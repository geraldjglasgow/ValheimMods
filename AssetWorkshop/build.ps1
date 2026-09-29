<#
.SYNOPSIS
    Builds workshop assets: Blender (model, bake, export, preview), then optionally a Unity asset bundle.
.EXAMPLE
    .\build.ps1 -Asset test_crate
    Blender only. Look at assets\test_crate\out\preview.png.
.EXAMPLE
    .\build.ps1 -Asset test_crate,other_prop -Bundle workshoptest
    Both assets, then one bundle with both prefabs, written to out\bundles\workshoptest.
.EXAMPLE
    .\build.ps1 -Asset test_crate -Bundle workshoptest -SkipBlender
    The bundle from what is already in assets\test_crate\out (built earlier, or by a script of the asset's own).
    An asset's <name>_icon.png, when there is one, goes into the bundle as a sprite of that name.
.EXAMPLE
    .\build.ps1 -Asset my_shield -Lineup
    Also renders assets\my_shield\out\lineup\ (the asset beside its codex category's game prefabs). After any build,
    a model that sets CATEGORY gets the style check (out\style_report.txt, printed here), and an asset folder with
    a variants.json gets its recoloured albedos and out\variants.png. -Asset also takes an asset folder's path.
#>
param(
    [Parameter(Mandatory = $true)][string[]]$Asset,
    [string]$Bundle,
    [switch]$NoPreview,
    [switch]$SkipBlender,
    [switch]$Lineup,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" }),
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" }),
    [string]$Python = $(if ($env:WORKSHOP_PYTHON) { $env:WORKSHOP_PYTHON } else { 'python' })
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Get-AssetDir([string]$Name) {
    if (Test-Path (Join-Path $Name 'model.py')) { return (Resolve-Path $Name).Path }
    if (Test-Path (Join-Path $root "$Name\model.py")) { return (Resolve-Path (Join-Path $root $Name)).Path }
    return Join-Path $root "assets\$Name"
}

function Build-Model([string]$Name) {
    $ErrorActionPreference = 'Continue'   # Blender writes warnings to stderr
    $dir = Get-AssetDir $Name
    if (-not (Test-Path "$dir\model.py")) { throw "no model.py in $dir" }
    $argv = @('--background', '--factory-startup', '--python-exit-code', '1',
              '--python', "$root\blender\run.py", '--', '--asset', $dir)
    if ($NoPreview) { $argv += '--no-preview' }
    Write-Host "Blender: $Name"
    $output = & $Blender @argv 2>&1 | ForEach-Object { "$_" }
    $failed = $LASTEXITCODE -ne 0
    $output | Where-Object { $_ -match '^WORKSHOP|Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    if ($failed) {
        $output | Select-Object -Last 25 | ForEach-Object { Write-Host "  $_" }
        throw "Blender failed for $Name"
    }
}

function Invoke-Codex([string]$Name) {
    # Opt in: only a model with CATEGORY gets the style check, only an asset with a variants.json its variants.
    $ErrorActionPreference = 'Continue'
    $dir = Get-AssetDir $Name
    $leaf = Split-Path $dir -Leaf
    $manifest = Get-Content (Join-Path $dir "out\$leaf.json") -Raw | ConvertFrom-Json
    $tools = Join-Path $root 'codex\tools'
    if (Test-Path "$dir\variants.json") {
        Write-Host "Recolour: $Name"
        & $Python "$tools\recolour.py" $dir --blender $Blender 2>&1 | ForEach-Object { Write-Host "  $_" }
        if ($LASTEXITCODE -ne 0) { Write-Warning "recolour failed for $Name" }
    }
    if ($manifest.category) {
        Write-Host "Style check: $Name"
        & $Python "$tools\stylecheck.py" $dir --blender $Blender 2>&1 | ForEach-Object { Write-Host "  $_" }
        if ($LASTEXITCODE -ne 0) { Write-Warning "style check failed for $Name" }
    }
}

function Build-Lineup([string]$Name) {
    $ErrorActionPreference = 'Continue'
    $argv = @('--background', '--factory-startup', '--python-exit-code', '1',
              '--python', "$root\blender\lineup.py", '--', '--asset', (Get-AssetDir $Name))
    Write-Host "Lineup: $Name"
    $output = & $Blender @argv 2>&1 | ForEach-Object { "$_" }
    $failed = $LASTEXITCODE -ne 0
    $output | Where-Object { $_ -match '^WORKSHOP|Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    if ($failed) {
        $output | Select-Object -Last 25 | ForEach-Object { Write-Host "  $_" }
        throw "Blender lineup failed for $Name"
    }
}

function Copy-ToUnity([string]$BundleName, [string[]]$Names) {
    $stage = Join-Path $root "unity\Assets\Bundles\$BundleName"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    foreach ($name in $Names) {
        $out = Join-Path $root "assets\$name\out"
        $dest = New-Item -ItemType Directory -Force (Join-Path $stage $name)
        Copy-Item "$out\$name.fbx", "$out\$name.json", "$out\${name}_*.png" -Destination $dest
    }
}

function Build-Bundle([string]$BundleName) {
    $out = Join-Path $root 'out\bundles'
    $log = Join-Path $root 'out\unity.log'
    New-Item -ItemType Directory -Force $out | Out-Null
    $argv = @('-batchmode', '-nographics', '-quit', '-projectPath', "$root\unity",
              '-executeMethod', 'Workshop.BundleBuild.Run', '-workshopBundle', $BundleName,
              '-workshopOut', $out, '-logFile', $log) | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
    Write-Host "Unity: bundle $BundleName"
    $process = Start-Process -FilePath $Unity -ArgumentList $argv -Wait -PassThru -NoNewWindow
    Get-Content $log | Where-Object { $_ -match 'WORKSHOP|error CS|licens|Licens' } | ForEach-Object { Write-Host "  $_" }
    if ($process.ExitCode -ne 0) { throw "Unity failed (exit $($process.ExitCode)); full log: $log" }
    Write-Host "Bundles: $out\$BundleName.windows, $out\$BundleName.linux"
}

if (-not $SkipBlender) { foreach ($name in $Asset) { Build-Model $name; Invoke-Codex $name } }
if ($Lineup) { foreach ($name in $Asset) { Build-Lineup $name } }
if ($Bundle) {
    $Bundle = $Bundle.ToLowerInvariant()   # Unity lower-cases bundle names
    Copy-ToUnity $Bundle $Asset
    Build-Bundle $Bundle
}

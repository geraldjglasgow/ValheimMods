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
#>
param(
    [Parameter(Mandatory = $true)][string[]]$Asset,
    [string]$Bundle,
    [switch]$NoPreview,
    [switch]$SkipBlender,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" }),
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" })
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Build-Model([string]$Name) {
    $ErrorActionPreference = 'Continue'   # Blender writes warnings to stderr
    $dir = Join-Path $root "assets\$Name"
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

if (-not $SkipBlender) { foreach ($name in $Asset) { Build-Model $name } }
if ($Bundle) {
    $Bundle = $Bundle.ToLowerInvariant()   # Unity lower-cases bundle names
    Copy-ToUnity $Bundle $Asset
    Build-Bundle $Bundle
}

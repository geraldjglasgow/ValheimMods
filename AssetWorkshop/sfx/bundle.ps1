<#
.SYNOPSIS
    Bundles a built sound set (sfx/out/<set>, from build.py) for Windows and Linux with Unity.
.EXAMPLE
    .\sfx\bundle.ps1 -Set rootling
    Stages sfx\out\rootling\*.wav and manifest.json in unity\Assets\Bundles\Sfx\ecp_rootling_sfx\ and writes
    out\bundles\ecp_rootling_sfx.windows and .linux (the bundle name is the manifest's set plus _sfx, lower case).
.NOTES
    Unity runs in batch mode only while this script holds out\unity.lock (a folder: creating it is atomic). When
    another build holds it, the script waits up to -WaitSeconds and then gives up. Unity must not have the project
    open in the editor.
#>
param(
    [Parameter(Mandatory = $true)][string]$Set,
    [string]$Bundle,
    [int]$WaitSeconds = 600,
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" })
)
$ErrorActionPreference = 'Stop'
$workshop = Split-Path $PSScriptRoot -Parent
$source = Join-Path $PSScriptRoot "out\$Set"
if (-not (Test-Path "$source\manifest.json")) { throw "no $source\manifest.json: run sfx\build.py $Set first" }
$manifest = Get-Content "$source\manifest.json" -Raw | ConvertFrom-Json
if (-not $Bundle) { $Bundle = "$($manifest.set)_sfx" }
$Bundle = $Bundle.ToLowerInvariant()   # Unity lower-cases bundle names

function Enter-Lock([string]$Path, [int]$Seconds) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ($true) {
        try { New-Item -ItemType Directory -Path $Path -ErrorAction Stop | Out-Null; return }
        catch { if ((Get-Date) -gt $deadline) { throw "$Path is held by another build; try again later" } }
        Start-Sleep -Seconds 10
    }
}

function Copy-Stage([string]$Folder) {
    if (Test-Path $Folder) { Remove-Item $Folder -Recurse -Force }
    New-Item -ItemType Directory -Force $Folder | Out-Null
    Copy-Item "$source\manifest.json" $Folder
    foreach ($sound in $manifest.sounds) {
        foreach ($clip in $sound.clips) { Copy-Item "$source\$clip.wav" $Folder }
    }
}

$lock = Join-Path $workshop 'out\unity.lock'
$out = Join-Path $workshop 'out\bundles'
$log = Join-Path $workshop "out\sfx_$Bundle.log"
New-Item -ItemType Directory -Force $out | Out-Null
Enter-Lock $lock $WaitSeconds
try {
    Copy-Stage (Join-Path $workshop "unity\Assets\Bundles\Sfx\$Bundle")
    $argv = @('-batchmode', '-nographics', '-quit', '-projectPath', "$workshop\unity",
              '-executeMethod', 'Workshop.Sfx.SfxBundle.Run', '-workshopBundle', $Bundle,
              '-workshopOut', $out, '-logFile', $log) | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
    Write-Host "Unity: sound bundle $Bundle"
    $process = Start-Process -FilePath $Unity -ArgumentList $argv -Wait -PassThru -NoNewWindow
    Get-Content $log | Where-Object { $_ -cmatch '^WORKSHOP|error CS' -or $_ -match 'another Unity instance|licen[cs]e.*(error|fail)' } |
        ForEach-Object { Write-Host "  $_" }
    if ($process.ExitCode -ne 0) { throw "Unity failed (exit $($process.ExitCode)); full log: $log" }
    Write-Host "Bundles: $out\$Bundle.windows, $out\$Bundle.linux"
}
finally {
    Remove-Item $lock -Force -Recurse -ErrorAction SilentlyContinue
}

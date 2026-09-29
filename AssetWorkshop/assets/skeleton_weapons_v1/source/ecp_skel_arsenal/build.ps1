<#
.SYNOPSIS
    Builds the skeleton arsenal (Elite Creatures Pack): the dagger, sword, axe, mace, spear, atgeir, bow and arrow the
    skeletons carry, each with a vertebra worked in, and the vertebra they drop; then the showcase: the game's Skeletons
    holding each weapon and doing its attack, baked in Unity and staged in Blender.
.EXAMPLE
    .\assets\ecp_skel_arsenal\build.ps1 -Open
    Everything, then Blender opens the showcase playing.
.EXAMPLE
    .\assets\ecp_skel_arsenal\build.ps1 -SkipBlender -SkipBundle
    Only the Unity bake and the .blend, from the models and prefabs already built.
.NOTES
    Models: assets\ecp_skel_*\model.py and assets\ecp_vertebra\model.py on the shared code in this folder (grave_*.py).
    Unity code: unity\Assets\Editor\SkelArsenal. The bundle is out\bundles\ecp_skel_arsenal.windows/.linux. The Skeleton
    and its clips come from the reference export (rip-reference.ps1) and never go into the bundle. Close the Unity
    editor first: batch mode cannot open a project the editor has open.
#>
param(
    [switch]$SkipBlender,
    [switch]$SkipBundle,
    [switch]$SkipBake,
    [switch]$Open,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" }),
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" })
)
$ErrorActionPreference = 'Stop'
$workshop = Resolve-Path "$PSScriptRoot\..\.."
$assets = 'ecp_skel_dagger', 'ecp_skel_sword', 'ecp_skel_axe', 'ecp_skel_mace', 'ecp_skel_spear', 'ecp_skel_atgeir',
          'ecp_skel_bow', 'ecp_skel_arrow', 'ecp_vertebra'
$bake = Join-Path $PSScriptRoot 'out\blender'

function Invoke-Blender([string[]]$Arguments) {
    $ErrorActionPreference = 'Continue'   # Blender writes warnings to stderr
    $output = & $Blender @Arguments 2>&1 | ForEach-Object { "$_" }
    $output | Where-Object { $_ -match '^WORKSHOP|Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { $output | Select-Object -Last 20 | ForEach-Object { Write-Host "  $_" }; throw "Blender failed" }
}

if (-not $SkipBlender) {
    & "$workshop\build.ps1" -Asset $assets
    Write-Host 'Blender: vertebra icon'
    Invoke-Blender @('--background', '--factory-startup', '--python-exit-code', '1', '--python', "$PSScriptRoot\icon.py")
}
if (-not $SkipBundle) {
    if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity is running; close it first (batch mode cannot share the project)' }
    & "$workshop\build.ps1" -Asset $assets -Bundle ecp_skel_arsenal -SkipBlender
}
if (-not $SkipBake) {
    if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity is running; close it first (batch mode cannot share the project)' }
    if (Test-Path $bake) { Remove-Item $bake -Recurse -Force }
    $log = Join-Path $workshop 'out\skel_arsenal_unity.log'
    $argv = @('-batchmode', '-projectPath', "$workshop\unity", '-executeMethod', 'Workshop.SkelArsenal.ArsenalBake.Run',
              '-workshopOut', $bake, '-logFile', $log) | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
    Write-Host 'Unity: skeleton arsenal bake'
    $process = Start-Process -FilePath $Unity -ArgumentList $argv -Wait -PassThru -NoNewWindow
    Get-Content $log | Where-Object { $_ -match 'WORKSHOP|error CS' } | ForEach-Object { Write-Host "  $_" }
    if ($process.ExitCode -ne 0) { throw "Unity failed (exit $($process.ExitCode)); full log: $log" }
}
Write-Host 'Blender: showcase scene'
Invoke-Blender @('--background', '--factory-startup', '--python-exit-code', '1', '--python', "$PSScriptRoot\blender_scene.py", '--', '--bake', $bake)
$blend = Join-Path $bake 'skeleton_arsenal.blend'
Write-Host "Showcase: $blend"
if ($Open) {
    Start-Process -FilePath $Blender -ArgumentList @("`"$blend`"", '--python', "`"$PSScriptRoot\blender_play.py`"")
}

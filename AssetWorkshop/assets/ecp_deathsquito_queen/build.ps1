<#
.SYNOPSIS
    Builds the Deathsquito Queen's workshop preview: her model and her eggs and needle (the workshop's build.ps1),
    her sounds (queen_sfx.py, the game's own clips), the rig on the game Deathsquito's skeleton (queen_rig.py) and the
    Blender scene of her whole fight (queen_preview.py). Workshop preview only: nothing goes into a mod.
.EXAMPLE
    .\assets\ecp_deathsquito_queen\build.ps1 -Open
    Everything, then opens the fight in Blender, playing with its sounds.
.EXAMPLE
    .\assets\ecp_deathsquito_queen\build.ps1 -SkipModels -Render
    Rig and scene from the models already built, then renders out\preview\queen_moves.mp4 (about 5 minutes).
.EXAMPLE
    .\assets\ecp_deathsquito_queen\build.ps1 -SkipModels -Stills
    Rig and scene, then three stills of every shot in out\preview\stills.
.NOTES
    The game's Deathsquito, the players, the Plains props and the sounds' source clips come from the local reference
    export (rip-reference.ps1) and are never exported.
#>
param(
    [switch]$SkipModels,
    [switch]$SkipSounds,
    [switch]$Stills,
    [switch]$Render,
    [switch]$Open,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" })
)
$ErrorActionPreference = 'Stop'
$workshop = Resolve-Path "$PSScriptRoot\..\.."

function Invoke-Blender([string]$script, [string[]]$more, [string]$what) {
    $ErrorActionPreference = 'Continue'
    $argv = @('--background', '--factory-startup', '--python-exit-code', '1', '--python', "$PSScriptRoot\$script", '--') + $more
    Write-Host "Blender: $what"
    $output = & $Blender @argv 2>&1 | ForEach-Object { "$_" }
    $output | Where-Object { $_ -match '^WORKSHOP|Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "Blender failed: $what" }
}

if (-not $SkipModels) {
    & "$workshop\build.ps1" -Asset ecp_deathsquito_queen, ecp_queen_egg, ecp_queen_egg_burst, ecp_queen_needle
}
if (-not $SkipSounds -and (Test-Path "$PSScriptRoot\queen_sfx.py")) {
    Invoke-Blender 'queen_sfx.py' @("$PSScriptRoot\out\sfx") 'sounds'
}
Invoke-Blender 'queen_rig.py' @() 'rig'
$more = @()
if ($Stills) { $more += '--stills' }
if ($Render) { $more += '--render' }
Invoke-Blender 'queen_preview.py' $more 'fight'
if ($Open) {
    Start-Process -FilePath $Blender -ArgumentList @("`"$PSScriptRoot\out\preview\queen_moves.blend`"", '--python', "`"$PSScriptRoot\queen_open.py`"")
}

<#
.SYNOPSIS
    Builds the Skeleton Crossbowman (Elite Creatures Pack): its four Blender parts, then in Unity its five clips
    (humanoid clips authored on the game's Skeleton), the kit and the bundle for Windows and Linux.
.EXAMPLE
    .\assets\ecp_crossbowman\build.ps1 -Preview -Install
    Everything, plus stills and a video in assets\ecp_crossbowman\out\preview, and the bundles copied into the mod.
.NOTES
    Parts: ecp_xbow_crossbow, ecp_xbow_string, ecp_xbow_quiver, ecp_xbow_bolt (each an assets\<name>\model.py). Unity code:
    unity\Assets\Editor\Crossbow. The Skeleton comes from the reference export (rip-reference.ps1) and never goes into
    the bundle. Close the Unity editor first: batch mode cannot open a project the editor has open.
#>
param(
    [switch]$Preview,
    [switch]$Install,
    [switch]$SkipBlender,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" }),
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" })
)
$ErrorActionPreference = 'Stop'
$workshop = Resolve-Path "$PSScriptRoot\..\.."
$parts = 'ecp_xbow_crossbow', 'ecp_xbow_string', 'ecp_xbow_quiver', 'ecp_xbow_bolt'
$out = Join-Path $workshop 'out\bundles\ecp_crossbowman'
$previewOut = Join-Path $PSScriptRoot 'out\preview'
$modBundles = Join-Path $workshop '..\EliteCreaturesPack\EliteCreaturesPack\assets\bundles'

if (-not $SkipBlender) { & "$workshop\build.ps1" -Asset $parts }
# The Bone Crossbow's inventory icon, rendered from the built crossbow (assets\ecp_xbow_crossbow\icon.py). Blender writes
# warnings to stderr, which 'Stop' would turn into a failure.
$ErrorActionPreference = 'Continue'
& $Blender --background "$workshop\assets\ecp_xbow_crossbow\out\ecp_xbow_crossbow.blend" --python-exit-code 1 --python "$workshop\assets\ecp_xbow_crossbow\icon.py" 2>&1 |
    ForEach-Object { "$_" } | Where-Object { $_ -match 'Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
if ($LASTEXITCODE -ne 0) { throw 'Blender failed rendering the Bone Crossbow icon' }
$ErrorActionPreference = 'Stop'

$stage = Join-Path $workshop 'unity\Assets\Bundles\ecp_crossbowman'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
foreach ($part in $parts) {
    $dest = New-Item -ItemType Directory -Force (Join-Path $stage $part)
    Copy-Item "$workshop\assets\$part\out\$part.fbx", "$workshop\assets\$part\out\$part.json", "$workshop\assets\$part\out\${part}_*.png" -Destination $dest
}

if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity is running; close it first (batch mode cannot share the project)' }
if (Test-Path $previewOut) { Remove-Item $previewOut -Recurse -Force }
$log = Join-Path $workshop 'out\crossbowman_unity.log'
$argv = @('-batchmode', '-projectPath', "$workshop\unity", '-executeMethod', 'Workshop.Crossbow.XbowBuild.Run',
          '-workshopOut', $out, '-logFile', $log)
if ($Preview) { $argv += @('-workshopPreview', $previewOut) }
$argv = $argv | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
Write-Host 'Unity: crossbowman'
$process = Start-Process -FilePath $Unity -ArgumentList $argv -Wait -PassThru -NoNewWindow
Get-Content $log | Where-Object { $_ -match 'WORKSHOP|error CS' } | ForEach-Object { Write-Host "  $_" }
if ($process.ExitCode -ne 0) { throw "Unity failed (exit $($process.ExitCode)); full log: $log" }

if ($Preview) {
    & $Blender --background --factory-startup --python "$workshop\blender\encode.py" -- "$previewOut\frames" "$previewOut\crossbowman_shot.mp4" 30 2>&1 |
        Where-Object { $_ -match 'Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    Write-Host "Preview: $previewOut"
}
if ($Install) {
    Copy-Item "$out\ecp_crossbowman.windows", "$out\ecp_crossbowman.linux" -Destination $modBundles
    Write-Host "Installed the bundles into $modBundles"
}

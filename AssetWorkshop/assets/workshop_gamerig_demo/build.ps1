<#
.SYNOPSIS
    Builds the Mossback, the workshop's proof of route (b): a new body on the game Skeleton's own skeleton, through
    Blender, Unity's checks against the game's creature, the bundle, and optionally stills, a video and a Blender scene.
.EXAMPLE
    .\assets\workshop_gamerig_demo\build.ps1 -Preview -Blend
    Everything: out\preview.png (Blender), the bundle and report in out\bundles\workshop_gamerig_demo, stills sheets
    and mossback.mp4 in assets\workshop_gamerig_demo\out\unity, and out\blender\mossback_animations.blend.
.NOTES
    1. Blender (blender\workshop\gamerig_run.py): the rig copied from the game prefab, the body, weights, bake, the
       contract out\workshop_gamerig_demo.json and the PNGs; then codex\tools\recolour.py makes the kinds in
       variants.json (out\variants.png).
    2. Unity (Workshop.GameRig.GameRigBuild): prefab, checks against the contract and the game's own Skeleton, the
       playback through the Skeleton's controller, the bundles for Windows and Linux, report.txt. Any failed check
       fails the build. Unity runs only while this holds out\unity.lock (another session may be using the project).
    3. -Preview: stills and video frames from the playback, stills sheets (gamerig_sheet.py) and an MP4 (encode.py).
    4. -Blend: our body baked frame by frame (point cache) and out\blender\mossback_animations.blend (gamerig_scene.py).
#>
param(
    [switch]$Preview,
    [switch]$Blend,
    [switch]$SkipBlender,
    [string]$Blender = $(if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE\tools\blender\blender.exe" }),
    [string]$Unity = $(if ($env:WORKSHOP_UNITY) { $env:WORKSHOP_UNITY } else { "$env:USERPROFILE\tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe" })
)
$ErrorActionPreference = 'Stop'
$asset = 'workshop_gamerig_demo'
$workshop = Resolve-Path "$PSScriptRoot\..\.."
$here = $PSScriptRoot
$out = Join-Path $workshop "out\bundles\$asset"
$stage = Join-Path $workshop "unity\Assets\Bundles\$asset"
$unityOut = Join-Path $here 'out\unity'
$blendOut = Join-Path $here 'out\blender'
$lock = Join-Path $workshop 'out\unity.lock'

function Invoke-Blender([string[]]$Arguments, [string]$What) {
    $ErrorActionPreference = 'Continue'   # Blender writes warnings to stderr
    Write-Host "Blender: $What"
    $output = & $Blender --background --factory-startup --python-exit-code 1 @Arguments 2>&1 | ForEach-Object { "$_" }
    $failed = $LASTEXITCODE -ne 0
    $output | Where-Object { $_ -match '^WORKSHOP|Error|Traceback' } | ForEach-Object { Write-Host "  $_" }
    if ($failed) {
        $output | Select-Object -Last 25 | ForEach-Object { Write-Host "  $_" }
        throw "Blender failed: $What"
    }
}

function Enter-Lock {
    for ($i = 0; $true; $i++) {
        cmd /c mkdir "`"$lock`"" 2>$null
        if ($LASTEXITCODE -eq 0) { return }
        if ($i -ge 120) { throw "out\unity.lock has been held for 30 minutes; another session is using Unity" }
        if ($i -eq 0) { Write-Host 'Unity is in use by another build (out\unity.lock); waiting' }
        Start-Sleep -Seconds 15
    }
}

function Invoke-Unity([string[]]$Arguments, [string]$Log) {
    Enter-Lock
    try {
        $argv = @('-batchmode', '-projectPath', "$workshop\unity", '-logFile', $Log) + $Arguments
        $argv = $argv | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
        Write-Host "Unity: Workshop.GameRig.GameRigBuild"
        $process = Start-Process -FilePath $Unity -ArgumentList $argv -Wait -PassThru -NoNewWindow
    }
    finally {
        Remove-Item $lock -Force -ErrorAction SilentlyContinue
    }
    Get-Content $Log | Where-Object { $_ -cmatch 'WORKSHOP|error CS|another Unity instance' } | ForEach-Object { Write-Host "  $_" }
    if ($process.ExitCode -eq 2) { return $false }   # checks failed, but the previews and the bundle were written
    if ($process.ExitCode -ne 0) { throw "Unity failed (exit $($process.ExitCode)); full log: $Log" }
    return $true
}

if (-not $SkipBlender) {
    Invoke-Blender @('--python', "$workshop\blender\workshop\gamerig_run.py", '--', '--asset', $here) 'the body on the game skeleton'
    & python "$workshop\codex\tools\recolour.py" $here   # the kinds in variants.json, recoloured by paint region
    if ($LASTEXITCODE -ne 0) { throw 'recolour.py failed' }
}

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
$staged = @("$asset.json") + (@('albedo', 'normal', 'regions') | ForEach-Object { "${asset}_$_.png" })
$staged | ForEach-Object { Copy-Item (Join-Path "$here\out" $_) -Destination $stage }

New-Item -ItemType Directory -Force $out | Out-Null
$arguments = @('-executeMethod', 'Workshop.GameRig.GameRigBuild.Run', '-workshopAsset', $asset, '-workshopOut', $out)
if ($Preview) { $arguments += @('-workshopPreview', $unityOut) }
if ($Blend) { $arguments += @('-workshopBlender', $blendOut) }
$passed = Invoke-Unity $arguments (Join-Path $workshop "out\${asset}_unity.log")
Write-Host "Report: $out\report.txt"

if ($Preview) {
    & python "$workshop\blender\workshop\gamerig_sheet.py" $unityOut
    if ($LASTEXITCODE -ne 0) { throw 'gamerig_sheet.py failed' }
    Invoke-Blender @('--python', "$workshop\blender\encode.py", '--', "$unityOut\frames", "$unityOut\mossback.mp4", '30') 'the video'
    Write-Host "Preview: $unityOut"
}
if ($Blend) {
    Invoke-Blender @('--python', "$workshop\blender\workshop\gamerig_scene.py", '--', '--bake', $blendOut, '--name', 'mossback', '--render') 'the Blender scene'
}
if (-not $passed) { throw "checks failed; see $out\report.txt (the previews above show what they measured)" }

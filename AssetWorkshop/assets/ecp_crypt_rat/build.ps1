param([switch]$Video)
$ErrorActionPreference = 'Stop'
$workshop = (Resolve-Path "$PSScriptRoot/../..").Path
$blender = if ($env:WORKSHOP_BLENDER) { $env:WORKSHOP_BLENDER } else { "$env:USERPROFILE/tools/blender/blender.exe" }
& "$workshop/build.ps1" -Asset ecp_crypt_rat
if ($LASTEXITCODE -ne 0) { throw 'Model build failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/animate.py" *> "$PSScriptRoot/out/animation.log"
if ($LASTEXITCODE -ne 0) { throw 'Rig build failed: out/animation.log' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/performance.py" *> "$PSScriptRoot/out/performance.log"
if ($LASTEXITCODE -ne 0) { throw 'Performance build failed: out/performance.log' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/validate_motion.py" *> "$PSScriptRoot/out/motion_checks.log"
if ($LASTEXITCODE -ne 0) { throw 'Motion validation failed: out/motion_checks.log' }
& $blender --background --factory-startup --python-exit-code 1 --python "$workshop/blender/lineup.py" -- --asset ecp_crypt_rat --refs Boar,Wolf,Draugr,Skeleton *> "$PSScriptRoot/out/lineup.log"
if ($LASTEXITCODE -ne 0) { throw 'Lineup failed: out/lineup.log' }
if ($Video) {
    & $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/render_performance.py" *> "$PSScriptRoot/out/performance_video.log"
    if ($LASTEXITCODE -ne 0) { throw 'Performance video failed: out/performance_video.log' }
    & $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/review.py" *> "$PSScriptRoot/out/review.log"
    if ($LASTEXITCODE -ne 0) { throw 'Animation review failed: out/review.log' }
    & $blender --background --factory-startup --python-exit-code 1 --python "$workshop/blender/encode.py" -- "$PSScriptRoot/out/lunge_frames" "$PSScriptRoot/out/crypt_rat_lunge.mp4" 30 *> "$PSScriptRoot/out/video.log"
    if ($LASTEXITCODE -ne 0) { throw 'Video encoding failed: out/video.log' }
}
Write-Host "Crypt Rat built: $PSScriptRoot/out"

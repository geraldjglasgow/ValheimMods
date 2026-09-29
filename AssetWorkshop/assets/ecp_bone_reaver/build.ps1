param([switch]$Preview, [switch]$Unity, [switch]$Open)
$ErrorActionPreference = 'Stop'
$blender = Join-Path $env:USERPROFILE 'tools/blender/blender.exe'
$workshop = (Resolve-Path "$PSScriptRoot/../..").Path
$argsBuild = @('--background','--factory-startup','--python-exit-code','1','--python',"$PSScriptRoot/build.py")
if ($Preview) { $argsBuild += @('--','--render') }
& $blender @argsBuild
if ($LASTEXITCODE -ne 0) { throw 'Creature build failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/validate.py"
if ($LASTEXITCODE -ne 0) { throw 'Rig validation failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/export_unity.py"
if ($LASTEXITCODE -ne 0) { throw 'Creature export failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/compare.py"
if ($LASTEXITCODE -ne 0) { throw 'Comparison failed' }
if ($Unity) {
    $kit = "$workshop/unity/Assets/Creatures/ecp_bone_reaver"
    $reference = "$workshop/unity/Assets/Reference/BoneReaver"
    New-Item -ItemType Directory -Force $kit,$reference | Out-Null
    Copy-Item "$PSScriptRoot/out/unity/*" $kit -Force
    Copy-Item "$PSScriptRoot/out/reference_unity/*" $reference -Force
    $editor = Join-Path $env:USERPROFILE 'tools/Unity/Editor/6000.0.75f1/Editor/Unity.exe'
    $unityArgs = "-batchmode -nographics -projectPath `"$workshop/unity`" -executeMethod Workshop.BoneReaverBuild.Run -logFile `"$PSScriptRoot/out/unity.log`""
    $buildProcess = Start-Process -FilePath $editor -ArgumentList $unityArgs -WindowStyle Hidden -Wait -PassThru
    if ($buildProcess.ExitCode -ne 0) { throw 'Unity import failed; see out/unity.log' }
}
if ($Open) {
    Start-Process -FilePath $blender -ArgumentList "`"$PSScriptRoot/out/compare_valheim_skeletons.blend`""
}

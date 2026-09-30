param()
$ErrorActionPreference = 'Stop'
$workshop = (Resolve-Path "$PSScriptRoot/../..").Path
$blender = "$env:USERPROFILE/tools/blender/blender.exe"
& "$workshop/build.ps1" -Asset workshop_havorn
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/finish.py"
if ($LASTEXITCODE -ne 0) { throw 'Havorn export validation failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$PSScriptRoot/showcase.py"
if ($LASTEXITCODE -ne 0) { throw 'Havorn colored presentation failed' }
& $blender --background --factory-startup --python-exit-code 1 --python "$workshop/blender/lineup.py" -- --asset "$PSScriptRoot" --refs Karve,VikingShip
if ($LASTEXITCODE -ne 0) { throw 'Havorn reference lineup failed' }
python "$PSScriptRoot/package.py"
if ($LASTEXITCODE -ne 0) { throw 'Havorn packaging failed' }

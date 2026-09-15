# Packages one mod or every mod of the workspace by calling the mod's own pack.ps1 (the standard script).
#
#   .\pack.ps1 -Mod Lockstep                  check the version, build, zip to Lockstep\thunderstore\
#   .\pack.ps1 -Mod Lockstep -Version 0.2.0   set the version everywhere first (CHANGELOG.md needs "## 0.2.0")
#   .\pack.ps1 -All                            pack every mod that has a thunderstore\icon.png
param(
    [string]$Mod = "",
    [string]$Version = "",
    [switch]$All,
    [string]$GamePath = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

if ($All -and $Version -ne "") { throw "-Version applies to a single mod, use -Mod" }
if (-not $All -and $Mod -eq "") { throw "Give -Mod <name> or -All" }

$mods = @()
if ($All) {
    $mods = Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName "pack.ps1") } | ForEach-Object { $_.Name }
} else {
    $mods = @($Mod)
}

$results = @()
foreach ($name in $mods) {
    $script = Join-Path $root "$name\pack.ps1"
    if (-not (Test-Path $script)) { throw "$name has no pack.ps1" }
    if ($All -and -not (Test-Path (Join-Path $root "$name\thunderstore\icon.png"))) {
        Write-Host "Skipping $name, no thunderstore\icon.png yet"
        continue
    }
    Write-Host "=== $name"
    $packArgs = @{}
    if ($Version -ne "") { $packArgs["Version"] = $Version }
    if ($GamePath -ne "") { $packArgs["GamePath"] = $GamePath }
    & $script @packArgs
    $zip = Get-ChildItem (Join-Path $root "$name\thunderstore") -Filter "$name-*.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $results += "$name -> $($zip.FullName)"
}

Write-Host ""
Write-Host "Packages:"
$results | ForEach-Object { Write-Host "  $_" }

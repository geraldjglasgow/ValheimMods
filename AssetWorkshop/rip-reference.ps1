<#
.SYNOPSIS
    Exports Valheim's own assets into a Unity project with AssetRipper, as a local reference: real sizes, how the
    game's prefabs are put together, and the property names of its shaders.
.DESCRIPTION
    The export stays outside the repository and nothing from it is ever shipped or committed: a mod uses the game's
    assets at runtime, by name. Scripts are exported as the game's DLLs, not decompiled; shaders as dummies that keep
    their property names. Loading takes about half a minute, the export a good while longer (4 GB of game data).
    AssetRipper's own log goes to <Out>.log.
.EXAMPLE
    .\rip-reference.ps1
#>
param(
    [string]$GameData = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data',
    [string]$Out = "$env:USERPROFILE\ValheimReference",
    [string]$AssetRipper = "$env:USERPROFILE\tools\AssetRipper\AssetRipper.GUI.Free.exe",
    [int]$Port = 51888
)
$ErrorActionPreference = 'Stop'
$base = "http://127.0.0.1:$Port"
$wanted = @{ ScriptExportMode = 'DllExportWithoutRenaming'; ShaderExportMode = 'Dummy'; ImageExportFormat = 'Png' }

function Start-Ripper {
    $process = Start-Process -FilePath $AssetRipper -ArgumentList '--headless', '--port', $Port -PassThru -NoNewWindow `
        -RedirectStandardOutput "$Out.log" -RedirectStandardError "$Out.err.log"
    for ($i = 0; $i -lt 60; $i++) {
        try { Invoke-WebRequest -UseBasicParsing "$base/" | Out-Null; return $process }
        catch { Start-Sleep -Seconds 1 }
    }
    throw "AssetRipper did not start; see $Out.log"
}

function Get-Settings {
    $html = (Invoke-WebRequest -UseBasicParsing "$base/Settings/Edit").Content
    $form = @{}
    foreach ($m in [regex]::Matches($html, '(?s)<select[^>]*name="([^"]+)".*?</select>')) {
        $form[$m.Groups[1].Value] = [regex]::Match($m.Value, '<option[^>]*value="([^"]*)"[^>]*selected').Groups[1].Value
    }
    foreach ($m in [regex]::Matches($html, '<input[^>]*type="text"[^>]*name="([^"]+)"[^>]*value="([^"]*)"')) {
        $form[$m.Groups[1].Value] = $m.Groups[2].Value
    }
    foreach ($m in [regex]::Matches($html, '<input[^>]*type="checkbox"[^>]*name="([^"]+)"[^>]*checked')) {
        $form[$m.Groups[1].Value] = 'true'
    }
    return $form
}

function Set-Settings {
    $form = Get-Settings
    foreach ($key in $wanted.Keys) { $form[$key] = $wanted[$key] }
    Invoke-WebRequest -UseBasicParsing -Method Post "$base/Settings/Update" -Body $form | Out-Null
    $now = Get-Settings
    foreach ($key in $wanted.Keys) {
        if ($now[$key] -ne $wanted[$key]) { throw "AssetRipper kept $key = $($now[$key])" }
    }
}

if (Test-Path $Out) { throw "$Out already exists; delete it first or pass -Out" }
$ripper = Start-Ripper
try {
    Set-Settings
    Write-Host "Loading $GameData"
    Invoke-WebRequest -UseBasicParsing -Method Post "$base/LoadFolder" -Body @{ Path = $GameData } -TimeoutSec 3600 | Out-Null
    Write-Host "Exporting to $Out"
    Invoke-WebRequest -UseBasicParsing -Method Post "$base/Export/UnityProject" -Body @{ Path = $Out } -TimeoutSec 14400 | Out-Null
    Write-Host "Done: $Out"
}
finally {
    Stop-Process -Id $ripper.Id -ErrorAction SilentlyContinue
}

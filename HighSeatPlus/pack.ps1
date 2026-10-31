# Standard packaging script of the ValheimMods workspace. Identical in every mod; the mod name is the folder name.
#
#   .\pack.ps1                       check that the version is consistent, build Release, zip to thunderstore\<Mod>-<version>.zip
#   .\pack.ps1 -Version 1.2.0        first write that version everywhere it is recorded, then pack
#   .\pack.ps1 -GamePath D:\Valheim  override the game folder passed to the build
#
# Version locations kept in sync: the plugin constant (PluginVersion or ModVersion in a .cs file of the project),
# <Version> in the csproj, version_number in thunderstore\manifest.json, and the top "## X.Y.Z" heading of
# CHANGELOG.md (written by hand first, the release notes). CLAUDE.md is updated too when it quotes the version.
param(
    [string]$Version = "",
    [string]$GamePath = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$name = Split-Path -Leaf $root
$project = Join-Path $root "$name\$name.csproj"
$manifestPath = Join-Path $root "thunderstore\manifest.json"
$changelogPath = Join-Path $root "CHANGELOG.md"
$iconPath = Join-Path $root "thunderstore\icon.png"
$readmePath = Join-Path $root "README.md"
$licensePath = Join-Path $root "LICENSE"
$claudePath = Join-Path $root "CLAUDE.md"

foreach ($required in @($project, $manifestPath, $changelogPath, $iconPath, $readmePath, $licensePath)) {
    if (-not (Test-Path $required)) { throw "Missing $required (standard layout: <Mod>\<Mod>.csproj, thunderstore\manifest.json, thunderstore\icon.png, README.md, CHANGELOG.md)" }
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
function Read-Text([string]$path) { return [System.IO.File]::ReadAllText($path) }
function Write-Text([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8) }

# ---- where the version lives
$constPattern = '(public const string (?:PluginVersion|ModVersion) = ")([^"]+)(")'
$csprojPattern = '(<Version>)([^<]+)(</Version>)'
$claudePattern = '(Loading \[[^\]]+ )(\d+\.\d+\.\d+)(\])'

$pluginFile = Get-ChildItem (Join-Path $root $name) -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and (Read-Text $_.FullName) -match $constPattern } |
    Select-Object -First 1
if ($null -eq $pluginFile) { throw "No 'public const string PluginVersion' (or ModVersion) found under $name\" }

function Get-Versions {
    $pluginText = Read-Text $pluginFile.FullName
    $csprojText = Read-Text $project
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $changelogMatch = [regex]::Match((Read-Text $changelogPath), '(?m)^## (\d+\.\d+\.\d+)')
    $changelogVersion = ""
    if ($changelogMatch.Success) { $changelogVersion = $changelogMatch.Groups[1].Value }
    return @{
        "plugin constant ($($pluginFile.Name))" = [regex]::Match($pluginText, $constPattern).Groups[2].Value
        "csproj <Version>" = [regex]::Match($csprojText, $csprojPattern).Groups[2].Value
        "thunderstore/manifest.json" = $manifest.version_number
        "CHANGELOG.md top section" = $changelogVersion
    }
}

# ---- optional bump
if ($Version -ne "") {
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 1.2.3, got '$Version'" }
    if ((Read-Text $changelogPath) -notmatch "(?m)^## $([regex]::Escape($Version))\s*$") {
        throw "CHANGELOG.md has no '## $Version' section. Write the release notes first, then run pack.ps1 -Version $Version again."
    }
    Write-Text $pluginFile.FullName ([regex]::Replace((Read-Text $pluginFile.FullName), $constPattern, ('${1}' + $Version + '${3}')))
    Write-Text $project ([regex]::Replace((Read-Text $project), $csprojPattern, ('${1}' + $Version + '${3}')))
    Write-Text $manifestPath ([regex]::Replace((Read-Text $manifestPath), '("version_number"\s*:\s*")([^"]+)(")', ('${1}' + $Version + '${3}')))
    if (Test-Path $claudePath) {
        Write-Text $claudePath ([regex]::Replace((Read-Text $claudePath), $claudePattern, ('${1}' + $Version + '${3}')))
    }
    Write-Host "Version set to $Version in $($pluginFile.Name), $name.csproj and thunderstore\manifest.json"
}

# ---- consistency check
$versions = Get-Versions
$distinct = @($versions.Values | Sort-Object -Unique)
if ($distinct.Count -ne 1 -or $distinct[0] -eq "") {
    $report = ($versions.GetEnumerator() | ForEach-Object { "  $($_.Key): $($_.Value)" }) -join "`n"
    throw "Version mismatch, fix it or run .\pack.ps1 -Version X.Y.Z:`n$report"
}
$version = $distinct[0]
Write-Host "$name $version"

# ---- icon
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
$iconSize = "$($icon.Width)x$($icon.Height)"
$icon.Dispose()
if ($iconSize -ne "256x256") { throw "thunderstore\icon.png is $iconSize, Thunderstore requires 256x256" }

# ---- build
$buildArgs = @("build", $project, "-c", "Release")
if ($GamePath -ne "") { $buildArgs += "-p:GamePath=$GamePath" }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
$dll = Join-Path $root "dist\$name.dll"
if (-not (Test-Path $dll)) { throw "Build did not produce $dll" }

# ---- package
$stage = Join-Path $root "thunderstore\stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage "plugins") | Out-Null
Copy-Item $dll (Join-Path $stage "plugins")
Copy-Item $manifestPath, $iconPath, $readmePath, $changelogPath $stage
Copy-Item $licensePath $stage

$zip = Join-Path $root "thunderstore\$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
Remove-Item $stage -Recurse -Force

Get-ChildItem (Join-Path $root "thunderstore") -Filter "$name-*.zip" | Where-Object { $_.FullName -ne $zip } | ForEach-Object {
    Write-Warning "Older package still present, delete it if it was uploaded or is wrong: $($_.Name)"
}
Write-Host "Created $zip"

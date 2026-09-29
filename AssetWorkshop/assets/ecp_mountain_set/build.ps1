param([switch]$SkipBlender, [switch]$Install)
$ErrorActionPreference = 'Stop'
$workshop = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$names = @('ecp_frostfang', 'ecp_rimeback', 'ecp_scree_wing', 'ecp_cairn_wight', 'ecp_ice_crawler')
$stage = [IO.Path]::GetFullPath((Join-Path $workshop 'unity\Assets\Bundles\ecp_mountain_set'))
$expected = Join-Path $workshop 'unity\Assets\Bundles\'
if (-not $stage.StartsWith($expected, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Bundle staging path is outside the workshop: $stage"
}
& (Join-Path $workshop 'build.ps1') -Asset $names -Bundle ecp_mountain_set -SkipBlender:$SkipBlender
if ($Install) {
    $dest = Join-Path $workshop '..\EliteCreaturesPack\EliteCreaturesPack\assets\bundles'
    foreach ($platform in @('windows', 'linux')) {
        Copy-Item -LiteralPath (Join-Path $workshop "out\bundles\ecp_mountain_set.$platform") -Destination $dest
    }
}

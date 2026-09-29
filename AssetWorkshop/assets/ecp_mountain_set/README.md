# Mountain creatures

Five original accessory meshes fitted to vanilla animated creatures. The game supplies bodies,
textures, rigs and movement at runtime; exported meshes and atlases contain only our new work.
The design follows the swamp reference study: coarse silhouettes, low-resolution textured surfaces,
muted stone, hide and bone, and sparse pale frost accents.

| Creature | Asset | Animated base | Relative size |
| --- | --- | --- | --- |
| Frostfang, wolf miniboss | `ecp_frostfang` | Wolf | 1.65 |
| Rimeback | `ecp_rimeback` | Lox | 0.65 |
| Scree Wing | `ecp_scree_wing` | Hatchling | 1.1 |
| Cairn Wight | `ecp_cairn_wight` | Fenring | 0.9 |
| Ice Crawler | `ecp_ice_crawler` | Neck | 2.0 |

Run `./build.ps1 -Install` in this directory to bake and build the Windows/Linux bundles, then
copy them into EliteCreaturesPack. `-SkipBlender` reuses the existing model outputs.
Build the mod after installing the bundle.

Run Blender headless with `--python lineup.py` to create `out/mountain_lineup.png` and
`out/mountain_review.blend`. This reference scene includes locally exported vanilla assets for
inspection only and must never be distributed. The lineup shows the rest-pose fit; it does not
validate attacks, animated attachment motion or in-game lighting.

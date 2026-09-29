# Building pieces

Everything the hammer's building tabs place: walls, gable ends, floors, roofs, beams, poles, stairs, doors, gates,
windows, fences, arches, blocks, defences and resource stacks. Furniture is in `furniture.md`, crafting and production
stations in `stations.md`; both build on the anatomy, damage, shader and icon sections of this page.

Measured on 2026-09-29 from the 398 prefabs the hammer's `_HammerPieceTable` lists (388 measured; ships, carts, siege
engines, the repair tool, the placeable rock and the training dummy left out) by `codex/measure/pieces.py`
(`codex/data/pieces.json`). Looks were read from renders and texture sheets in `codex/out/pieces/` (`renders/`,
`textures/`, `atlas/`, `icons/`), made by `pieces_render.py` (Blender), `pieces_sheets.py` and `pieces_icons.py`. All
paths below are under the reference export (`%USERPROFILE%\ValheimReference\ExportedProject\Assets`); every reference
prefab is `GameElements/Pieces/<name>.prefab`.

## The numbers

Triangles are the new look at LOD 0 (the renderers a freshly built piece shows up close). Texel density is texture
pixels per metre of surface over the piece's main textures, the material's tiling included. Longest is the visual
mesh's longest side in metres.

| Category | n | Triangles median (range) | Texture px | px per metre median (IQR) | Longest m median | References |
| --- | --- | --- | --- | --- | --- | --- |
| `piece.wall` | 27 | 264 (44 to 1,320) | 128 (32 to 128) | 35.8 (29.3 to 39.6) | 2.03 | woodwall, stone_wall_2x1, darkwood_decowall, iron_wall_2x2, Piece_grausten_wall_2x2, stave_wall_2x2 |
| `piece.gable` | 29 | 254 (88 to 349) | 128 | 32.3 (29.2 to 36.4) | 2.41 | wood_wall_roof_a, wood_wall_roof_45, ashwood_wall_roof_45, scale_wall_roof_45 |
| `piece.floor` | 13 | 196 (32 to 1,072) | 128 (32 to 256) | 35.2 (30.3 to 43.3) | 2.00 | wood_floor, stone_floor_2x2, blackmarble_floor, Piece_grausten_floor_2x2, ashwood_floor_2x2 |
| `piece.roof` | 27 | 416 (238 to 740) | 128 | 27.2 (24.9 to 38.7) | 2.87 | wood_roof, wood_roof_top, darkwood_roof, darkwood_roof_ocorner, piece_grausten_roof_45 |
| `piece.beam` | 38 | 108 (44 to 1,532) | 128 (128 to 256) | 36.2 (30.2 to 41.0) | 2.36 | wood_beam, wood_beam_45, darkwood_beam, woodiron_beam, stave_beam_2m, wood_log_45 |
| `piece.pole` | 20 | 219 (44 to 1,516) | 128 (128 to 256) | 37.5 (29.1 to 47.4) | 2.00 | wood_pole2, wood_pole_log, darkwood_pole, woodiron_pole, stone_pillar, blackmarble_column_2 |
| `piece.stair` | 9 | 220 (112 to 1,328) | 128 (128 to 256) | 29.0 (24.1 to 44.1) | 2.09 | wood_stair, stone_stair, blackmarble_stair, piece_dvergr_spiralstair, ashwood_stair |
| `piece.door` | 3 | 936 (308 to 3,536) | 128 | 37.7 | 3.01 | wood_door, ashwood_door, piece_hexagonal_door |
| `piece.gate` | 8 | 1,357 (202 to 5,938) | 128 (128 to 256) | 30.2 (22.5 to 48.6) | 4.73 | wood_gate, darkwood_gate, iron_grate, stave_gate, flametal_gate |
| `piece.window` | 3 | 292 (156 to 308) | 128 | 29.3 | 2.04 | wood_window, Piece_grausten_window_2x2 |
| `piece.fence` | 2 | 512 and 1,536 | 128 and 64 | 102.8 and 38.7 | 2.51 | wood_fence, stone_fence |
| `piece.arch` | 12 | 576 (84 to 1,412) | 128 (128 to 256) | 27.3 (23.4 to 35.1) | 2.14 | stone_arch, darkwood_arch, blackmarble_arch, ashwood_arch_big, Piece_grausten_pillar_arch |
| `piece.block` | 9 | 269 (28 to 420) | 256 | 46.9 (UV); 35.8 on screen (triplanar) | 2.00 | blackmarble_1x1, blackmarble_2x2x2, blackmarble_base_1, blackmarble_tip |
| `piece.defence` | 7 | 558 (352 to 2,684) | 128 (64 to 256) | 43.9 (28.9 to 49.0) | 3.81 | stake_wall, piece_sharpstakes, piece_stakewall_blackwood, piece_dvergr_stake_wall |
| `piece.stack` | 23 | 864 (328 to 3,600) | 64 (32 to 256) | 34.4 (27.5 to 64.1) | 1.49 | wood_stack, stone_pile, bar_iron_stack, coal_pile |

What these say, for a new building piece:

- **Low poly, low res, big texels.** A 2 m wall is 44 to 450 triangles; a whole roof slope 238 to 740. Textures are
  128 px (256 of 388 pieces), 256 px for a few modelled pieces (80), never more than 512 (one: the Frost Foundry).
  Building pieces sit at 27 to 38 px per metre, about one texel per 3 cm. Anything finer than 6 cm is paint, not
  geometry.
- **One renderer, one or two materials.** 323 of 388 pieces draw one renderer at LOD 0; the rest two to four (door
  leaves, lids, fire objects).
- **Aim at the median of the category, not the maximum.** The high ends are modelled showpieces (ashwood_door 3,536,
  darkwood_beam_67 1,532, piece_drawbridge 5,938), not the norm.

## Anatomy of a building piece

Every building piece is laid out the same way. `woodwall` (the 2x2 m wood wall) shows the full set:

```
woodwall                      root, layer piece (10), tag leaky or roof or none
  Piece, ZNetView, WearNTear  (and a BoxCollider on the root, or colliders on children)
  beam_2m_snow      (off)     snow cap: MeshRenderer with snow_flat.mat (SnowMesh shader), layer Default
  $hud_snappoint_top 1 (off)  snap points: empty, inactive, tag snappoint, layer piece
  ...
  New                         the new look (WearNTear.m_new), with a LODGroup
    _Combined Mesh [high]     LOD 0: one mesh, 264 triangles, woodwall.mat
    high (off)                the separate planks it was combined from: renderers disabled, the fragment root
    largelod                  LOD 1: Unity's cube scaled to the wall, woodwall_lowlod.mat
  Worn (off)                  WearNTear.m_worn: same layout, woodwall_worn.mat
  Broken (off)                WearNTear.m_broken: planks shortened and tilted, woodwall_worn.mat
```

- **Root.** `Piece`, `ZNetView`, `WearNTear` and usually one box collider. Layer 10 (`piece`) on 374 of 388 roots,
  `piece_nonsolid` (16) on 14 (hanging cloths, garlands, the candle, the cauldrons, the prep table, the cooking spit).
- **Tags on colliders decide what counts as a roof.** `WearNTear.RoofCheck` sphere-casts up from a piece and takes the
  first collider not tagged `leaky` as its roof (no rain wear, no wetness). Walls, poles, beams and furniture tag their
  colliders `leaky` (34 roots and most child colliders) so they never shelter what is under them; roofs and gables tag
  theirs `roof` (43 roots, 21 of 29 gables, 25 of 27 roofs). No game code reads `roof`, but follow the convention: a
  new roof's collider `roof`, anything else `leaky`.
- **Three looks as siblings.** `m_new`, `m_worn`, `m_broken` are child GameObjects the game switches with
  `SetActive` at health above 75 %, above 25 % and below (`WearNTear.SetHealthVisual`). The new one is active in the
  prefab, the others inactive. For furniture and small pieces all three often point at the same object (81 pieces).
- **LODs.** A `LODGroup` on each look (or one on the root). 277 pieces have two levels, 106 one, 4 three. LOD 0 hands
  over at about 10 % of screen height (median 0.10 for walls and roofs, 0.20 to 0.30 for furniture and stations) and
  the last level culls at 1.5 to 8 % (median 0.023 for walls, 0.05 to 0.08 for furniture). LOD 1 of a plank piece is a
  single scaled Unity cube (12 triangles) with a `_lowlod` material; of a modelled piece a reduced mesh at a median
  18 % of LOD 0's triangles (the workbench 1,252 to 256, the forge 1,748 to 406, darkwood_roof 488 to 160).
- **Snow cap.** An inactive child with a mesh from the game's snow library and a SnowMesh material (see Snow below).
- **Snap points.** Direct children of the root, inactive, tagged `snappoint` (only direct children count:
  `Piece.GetSnapPoints` reads `transform.GetChild`). They are named with the HUD tokens the builder sees when cycling
  snap points: `$hud_snappoint_top`, `_bottom`, `_edge`, `_center`, `_corner`, `_inner`, combined with a space
  (`$hud_snappoint_bottom $hud_snappoint_corner`).
- **Fragments.** What flies apart on destruction (see Damage).

### Pivots

- Walls, beams, poles, doors and the older stone and wood pieces are centred on their box (woodwall -1 to +1 m in
  height, wood_beam -0.2 to +0.2). The newer sets (ashwood, grausten, the scale walls) put the pivot at the bottom
  centre. The game snaps by snap points, so both work; **put a new piece's pivot at the bottom centre** so free
  placement on the ground sits it on the ground.
- Floors pivot at or just under the walking surface (wood_floor: top at +0.08 m, battens down to -0.14 m).
- Roofs pivot at the eave line: the slope rises from there (darkwood_roof -0.09 to +1.07 m).
- Banners hang from their pivot (piece_banner01: +0.1 to -2.93 m).

## The grid and snap points

The game builds on a 1 m grid with 2 m as the standard module. Every snap pattern measured, as points and the extent
they span:

| Kind | Snap points | Extent (m) | Examples |
| --- | --- | --- | --- |
| Wall panel | 4, at the panel's corners in its plane | 2x2x0, 2x1x0, 1x2x0, 1x1x0, 4x2x0 | woodwall, wood_wall_half, Piece_grausten_wall_1x2, iron_wall_1x1, Piece_grausten_wall_4x2 |
| Thick wall block | 8, at the box's corners | 2x1x1, 1x1x1, 4x2x1 | stone_wall_2x1, stone_wall_1x1, stone_wall_4x2 |
| Gable end | 3 (triangle) or 5 (with a ridge row) | 2x1x0 (26 degrees), 2x2x0 (45), 2x4x0 (67) | wood_wall_roof_a, wood_wall_roof_45, wood_wall_roof_top_67 |
| Floor | 4 corners in the plane, or 8 for a slab | 2x0x2, 1x0x1; 2x1x2 for stone and marble | wood_floor, wood_floor_1x1, stone_floor_2x2 |
| Roof slope | 4: two on the eave, two on the ridge | 2x1x2 (26), 2x2x2 (45), 2x4x2 (67) | darkwood_roof, wood_roof_45, darkwood_roof_67 |
| Roof ridge | 6 | 2x0.5x2 | wood_roof_top, darkwood_roof_top |
| Beam | 2, one at each end | 2x0x0, 1x0x0, 4x0x0; diagonals 2x1x0, 2x2x0, 2x4x0 | wood_beam, wood_beam_1, darkwood_beam4x4, wood_beam_26/45/67 |
| Pole | 2, top and bottom | 0x2x0, 0x1x0, 0x4x0 | wood_pole2, wood_pole, wood_pole_log_4 |
| Stair | 4 or 6 | 2x1x2 (one metre up over two) | wood_stair, stone_stair, blackmarble_stair |
| Block | 8 corners | 1x1x1, 2x1x1, 2x2x1, 2x2x2 | blackmarble_1x1, blackmarble_2x2x2 |
| Door | 4 (frame corners) or 8 (both faces) | 2x2x0, 2x2x0.53 | ashwood_door, wood_door |
| Gate | 4 or 5 | 2x3x0, 2x4x0, 1.5x4x0 | wood_gate, darkwood_gate, flametal_gate |
| Furniture, stations, stacks | none | | piece_chest_wood, piece_workbench, wood_stack |

- The three roof pitches are rise over a 2 m run: 1 m ("26", 26.6 degrees), 2 m ("45") and 4 m ("67", 63.4
  degrees). Beams, gables, roofs and crosses come in all three.
- **Snap points sit on the nominal box; the mesh overshoots it.** woodwall is exactly 2.00 x 2.00 m, but the stone
  wall 2x1 is 2.18 x 1.18 x 1.20 m on a 2 x 1 x 1 m snap box (9 cm proud on every side), ashwood walls 2.03 m, and the
  roof slopes overhang their snap box by 0.2 to 0.7 m at the eave (darkwood_roof: 2.38 x 1.16 x 2.70 m on 2 x 1 x 2).
  The overlap hides seams between neighbours. Overshoot a new piece by 1 to 5 % on walls and floors, 10 % on stone,
  and let roofs hang over.

## Materials and texture sharing

Pieces of one material set share one texture, usually 128 px, and differ only in UVs. A new piece in a set **uses the
set's texture and material** (borrowed from the game at runtime, never copied); a new set paints one texture to the
same rules. The main sets, with the texture's colour inside the pieces' UV islands (median sRGB, the material's
`_Color` tint applied; `data/pieces.json` `paint`):

| Set | Texture (px) | Shared by | Tint | Median colour new / worn | Luminance p10 to p90 |
| --- | --- | --- | --- | --- | --- |
| wood planks | `3rd party/ADG_Textures/Plank Textures/Planks5/textures/Planks5c_low.png` (128), tiled | 54 pieces | 1.0 | #5d4529 / #52513e | 0.26 to 0.31 |
| core wood logs | `GameElements/Pieces/_res/logwall/Pine_tree_log_wall.png` (128), atlas | 7 | 0.79 | #835c3c / #726750 | 0.29 to 0.47 |
| darkwood and shingles | `GameElements/Pieces/_res/DarkWood/Beams/model/DarkWoodBeams_d.png` (128), atlas | 41 (darkwood, stave, roof ornaments) | 1.0 | #564028 / #5d5347 | 0.11 to 0.30 |
| thatch | `world/textures/straw_roof.png` (64), tiled | 12 | 0.79 | #827257 / #7c7365 | 0.37 to 0.54 |
| stone | `GameElements/Pieces/_res/stone/stone.png` (128), atlas | 6 | 0.79 (worn 0.63) | #76736b / #6a6760 | 0.39 to 0.54 |
| iron cage | `GameElements/Pieces/_res/IronCage/metalwall.png` (32), tiled | 4 | 0.63 (worn rust 0.66, 0.49, 0.38) | #65645c / #685948 | 0.33 to 0.46 |
| iron-bound wood | `GameElements/Pieces/_res/IronBeam/model/Ironbeam_d.png` (256), atlas, metal mask | 5 | 1.0 | #594632 / #5d5139 | 0.22 to 0.49 |
| black marble | `GameElements/Pieces/_res/Marble/material/marble_d.png` (256), triplanar | 14 | 1.0 | #2b2c2b | 0.13 to 0.28 |
| grausten | `GameElements/Pieces/_res/Grausten/model/Grausten_d.png` (128), tiled, metal mask | 26 | 0.79 | #605f60 / #595759 | 0.35 to 0.38 |
| ashwood | `GameElements/Pieces/_res/BlackWood/walls/BlackWood_d.png` (128), atlas | 29 | 1.0 | #21201e | 0.12 to 0.22 |
| stave scales | `GameElements/Pieces/_res/Stavechurch/walls/scaled/Scaledwall2x2_d.png` (128), atlas | 16 | 1.0 | #4f3b23 | 0.08 to 0.29 |
| stakes | `GameElements/Pieces/_res/stakewall/stakewall_d.png` (128), atlas | 5 (stake wall, sharp stakes, fence, turret) | 1.0 | #736855 | 0.27 to 0.53 |

- **Tints darken.** Stone, logs, thatch and grausten multiply their texture by 0.79 grey; the iron cage by 0.63; the
  workbench by 0.62. Paint a new texture to the *tinted* colour or set the same tint.
- **Colours are low and brown.** Wood sits at luminance 0.26 to 0.47 and saturation 0.16 to 0.28; stone and metal at
  0.33 to 0.54 with saturation under 0.04; ashwood and black marble near black (0.12 to 0.17). Nothing is bright.
- **Tiled or atlas.** Plank walls, floors, thatch, the iron cage and grausten tile one texture (UVs run past 0 to 1,
  the material tiles it: woodwall.mat `_MainTex` scale -0.56 x 0.12). Log, darkwood, ashwood, stave, stone and iron
  pieces use a small atlas with strips or islands that every piece in the set maps into.

### Atlas layouts

Read off `codex/out/pieces/atlas/*.png` (the texture with the UV islands of up to eight pieces over it):

- **DarkWoodBeams_d (128 px): two vertical strips, grain running along V.** Left 42 %: near-black, fibrous, tarred
  bark (luminance about 0.11); right 58 %: planed mid-brown wood with fine vertical grain. Beams, poles, arches and
  stave pieces lay long thin islands down the right strip, following the grain; roof shingles, the decorated wall and
  the carved raven and wolf map into the dark strip. Both strips tile in V, so a 4 m beam repeats the strip. A new
  darkwood or stave piece maps timber faces down the right strip and dark, weathered faces down the left.
- **BlackWood_d (ashwood, 128 px): two vertical strips.** Left 42 %: brown plank; right 58 %: charcoal-black wood.
  Ashwood frames are black with brown panels, so frames map right, infill left.
- **Scaledwall2x2_d (stave scales, 128 px): top 63 % diamond-scale shingles beside plain vertical planks, bottom 37 %
  a near-black horizontal board strip** for the sill and top rail.
- **Pine_tree_log_wall (core wood) and stakewall_d (stakes) share one layout:** the left 60 % is the log's side
  (orange-brown split pine with long streaks for core wood; grey, deeply furrowed bark for stakes), the right column
  two small end-grain discs above one large one on a white unused ground. Log ends map to the discs, sides to the
  left block.
- **stone.png (128 px): islands on white.** Small cobbles (top), a mottled mid-grey rock face (bottom left), and a
  round weathered rock blob (right); 37 % of the texture is used. Stone pieces are one lumpy block, not coursed
  masonry.
- **Ironbeam_d (256 px):** a planed brown wood area in the middle, hammered grey iron with white highlights around it,
  flat grey patches for rivet heads; `Ironbeam_m.png` is white where the iron is. 21 % used by the pieces' islands.
- **Planks5c_low (128 px) and straw_roof (64 px)** are plain tiling textures: horizontal grain, low contrast (planks);
  a dense noisy straw weave (thatch).

### How each material is painted, up close

From the texture sheets (`codex/out/pieces/textures/*.png`, point-upscaled) and close renders (`renders/close_*.png`):

- **Planks** (woodwall, wood_floor): mid-brown (#5d4529), a fine horizontal grain of darker streaks, a few dark knot
  flecks, no painted edges or plank gaps: the gaps are geometry. Worn: the same grain gone grey-green (#52513e) with
  mossy green blotches. Normal map: faint horizontal grain only.
- **Logs** (wood_pole_log, wood_wall_log): warm orange-brown split pine with long diagonal pale streaks and darker
  seams; the ends are pale discs with dark rings and a dark rim. Worn: grey-brown.
- **Darkwood** (darkwood_beam, darkwood_pole): planed brown wood with fine vertical grain; carved twists and
  diamond cut-outs are modelled and take the dark strip. Worn: desaturated grey (#5d5347) with dark diagonal hash
  scratches.
- **Shingles** (darkwood_roof): big overlapping diamond shingles, about 0.5 m across, four to a 2 m row, three rows
  to a 26 degree slope, each a thin bent sheet; painted with the dark strip, so they read near-black brown.
- **Thatch** (wood_roof): alpha-cutout straw cards (`straw_roof_alpha.mat`, cutoff 0.69) over a plank deck; the lower
  edge is ragged with long straw strands hanging past the eave. Straw is noisy tan (#827257), pale flecks, a vertical
  strand pattern in the normal map. Worn: greyer (#7c7365).
- **Stone** (stone_wall_2x1): one lumpy block per piece, mottled grey (#76736b) with paler and darker blotches
  3 to 10 cm across; the worn look darkens the same texture with the tint (0.79 to 0.63) and a stronger normal.
- **Iron fittings** (woodiron_beam, iron_wall_2x2): straps about 10 cm wide wrapped round the timber and pyramid rivet
  heads about 5 cm across (read from the close render), all modelled; painted pale grey hammered metal with white
  highlights, marked metallic by the metal mask. Worn: the iron turns rust orange and the wood grey-green. The iron cage
  bars tile a 32 px grey-green mottle with a rusty patch (tint 0.63; worn tint turns it rust).
- **Black marble**: near-black (#2b2c2b) with thin white and gold veins, projected triplanar in world space so the
  veins run on across neighbouring blocks.
- **Grausten**: pale grey (#605f60 after tint) with fine diagonal streaks, almost flat (luminance 0.35 to 0.38); the
  broken look swaps in a cracked version with dark branching crack lines. The metal mask is a cloudy map that makes
  patches glossier.
- **Ashwood**: charcoal-black frames with brown plank infill; decorative panels (knots, trees) are cut-out geometry.
- **Cloth** (banners): dark grey cloth with a pale embroidered knot border, the pole strip beside it in the same
  128 px texture; see `furniture.md`.

### Proportions

Measured from the parts the simple wood pieces are assembled from (the game's bevelled unit cube, scaled per part;
`data/pieces.json` `sections`):

| Part | Section (m) | Length (m) | Where |
| --- | --- | --- | --- |
| Wall plank | 0.5 wide x 0.3 thick | 2.0 (1.0 in half walls) | woodwall: four side by side, 0.5 m apart |
| Wall batten | 0.13 x 0.18 | 2.0 | woodwall, back face, two across |
| Floor and roof plank | 0.5 x 0.1 | 2.0 to 2.34 | wood_floor (4 planks), wood_roof, wood_stair treads |
| Roof crossbeam | 0.13 x 0.18 | 2.02 | wood_roof |
| Beam and pole | 0.4 x 0.4 | 1, 2 | wood_beam, wood_pole2 |
| Log | 0.53 to 0.55 diameter | 2.2, 4.4 | wood_pole_log, wood_wall_log |
| Darkwood beam | 0.41 x 0.49 | 2, 4 | darkwood_beam |
| Stave beam | 0.71 x 0.71 | 2, 4 | stave_beam_2m |
| Iron-bound beam | 0.31 x 0.39 | 2 | woodiron_beam |
| Banner pole | 0.2 x 0.2 | 1.41 | piece_banner01 |

The game's timber is **oversized: a wall plank is 30 cm thick, a beam 40 cm square.** Thin boards read as flimsy
beside it. Most pieces are made of a handful of large parts: woodwall is six boxes, wood_floor five.

### Edges

The plank cube (`Misc/common_models/Cube_Cube_Material.asset`, 44 triangles) is a unit cube with one 45 degree
chamfer of 0.021 on every edge, so the bevel scales with the part: 6 mm on a 0.3 m face, 10 mm on 0.5 m, 4 cm along a
2 m length. Modelled pieces bevel the same way: **one chamfer per edge, 2 to 3 % of the part's section**, never
rounded multi-segment bevels and never sharp. Edges are not painted lighter; the bevel catches the light. Gaps
between planks are real gaps, not painted lines.

## The Custom/Piece shader

340 of 388 pieces draw with `Custom/Piece` (`Shaders/Piece.shader` in the export is a dummy that keeps the property
list). The others: Standard (41 pieces, mostly item-derived stacks and small props), Vegetation (15: banners and hanging
cloths, the frost wood stack), Rug (10), StaticRock (7: stone fence, portal, dvergr metal), Creature (7), FlowOpaque (7:
lava and eitr channels), StandardTwosided (4), Water (3). How the 152 Custom/Piece materials on the build-menu pieces
set its switches (`data/pieces.json` `piece_shader`):

| Switch | Setting (pieces) | What it does |
| --- | --- | --- |
| `_MainTex`, `_BumpMap` | set on 381 and 380 | albedo and tangent-space normal |
| `_Color` | 1.0 on most; 0.79 stone, logs, thatch, grausten; 0.62 to 0.63 workbench, forge, iron cage | multiplies albedo; the build highlight and the invalid-placement red write into it |
| `_NoiseTex`, `_ValueNoise` | the game's 64 px uniform RGB noise (`3rd party/SunShafts/Effects/ImageEffects/Textures/Noise.png`) on 339; strength 1.0 (150), 0.5 (105), 0 (71), 1.5 (12, thatch) | brightness variation from the noise so repeated pieces do not look identical; the placement ghost and destruction fragments set it to 0 |
| `_ValueNoiseVertex` | 1 on 160, 0 on 185 | per vertex (soft, large patches) or per pixel |
| `_RippleDistance`, `_RippleFreq` | 0 (200), 0.03 (82, darkwood), 0.05 (31, marble, beds), 0.01 (31, grausten), 0.1 (20, stone); freq 0 to 10 | a small vertex wobble so edges are not ruler-straight; the ghost zeroes it unless Left Ctrl is held, fragments zero it; worn materials double it (woodwall 0.03 to 0.06) |
| `_TriplanarMap` | on for 14 (black marble), scale 0.14; `_TriplanarLocalPos` 0 | world-space projection: 256 px x 0.14 = 36 px per metre, continuous across blocks; the ghost switches it to local space so the texture does not slide while aiming |
| `_AddRain` | on for 338 | wet darkening and gloss when it rains and the piece has no roof over it |
| `_AddSnow` (int) | on for 332 | snow on upward faces from the global snow level |
| `_MetallicTex`, `_Metallic`, `_MetallicAlphaGloss` | mask on 89 (grausten, iron beam, forge, chests); `_Metallic` 1 on 100 | metal mask: white is metal with `_MetalColor` and its own smoothness |
| `_Glossiness` | 0.1 to 0.5; wood 0.1 to 0.25, grausten 0.15, marble 0.5 | base smoothness; low everywhere |
| `_BumpScale` | 1.0 on 329; thatch 0.48, iron beam 0.6; worn 1.5 to 2 | normal strength |
| `_Cull` | back (308), off (45: garlands, the Yule tree, the maypole, the spice rack) | two-sided cards set `_Cull` 0; the darkwood shingles keep back culling but set `_TwoSidedNormals` 1 (28 pieces) |
| `_Cutoff` | 0.5 (321, unused unless alpha), 0.69 thatch, 0.33 braziers, 0.11 garlands | alpha cutout for cards |
| `_EmissionMap`, `_EmissionColor` | map on 11 (bonfire, lava lantern, eternal pyre, fairy lights, galdr table); most glow is a separate Standard material instead (`stations.md`) | glow; the support highlight writes `_EmissionColor` |
| `_MoveableObject` | 0 on all 340 | reserved for pieces that move (carts, ships) |

A new piece **wears the set's own game material** (`GameMaterials.Borrow` in BundlePrefabs) or, for a new texture, a
Custom/Piece material copied from a set material with only `_MainTex`, `_BumpMap` and `_MetallicTex` swapped, so the
noise, ripple, rain, snow and highlight behave like the game's. A Standard material shows none of it and does not
darken in rain.

## Damage: WearNTear

### Material types and support

`WearNTear.m_materialType` sets how far support carries (`WearNTear.GetMaterialProperties`), and with the material
comes the family of hit and destroy effects. Health is `m_health` (median over the pieces of that type).

| Type | Pieces | Max support | Min support | Vertical loss | Horizontal loss | Health median (range) |
| --- | --- | --- | --- | --- | --- | --- |
| Wood | 266 | 100 | 10 | 0.125 | 0.2 | 200 (5 to 10,000) |
| HardWood (core wood logs, heavy gates, dvergr pieces) | 15 | 140 | 10 | 0.1 | 0.167 | 500 (50 to 2,000) |
| Timberwood (stave) | 17 | 200 | 10 | 0.077 | 0.2 | 800 |
| Stone | 22 | 1,000 | 100 | 0.125 | 1.0 | 1,500 (100 to 2,000) |
| Marble (black marble) | 18 | 1,500 | 100 | 0.125 | 0.5 | 1,500 |
| Ashstone (grausten) | 26 | 2,000 | 100 | 0.1 | 0.333 | 800 (400 to 4,000) |
| Iron (iron and iron-bound wood) | 23 | 1,500 | 20 | 0.077 | 0.077 | 1,000 (200 to 3,000) |
| Ice | 1 | 1,000 | 100 | 0.125 | 0.333 | 10 |
| Ancient | 0 | 5,000 | 100 | 0.067 | 0.25 | (used by world pieces) |

Wood walls are 400 health (woodwall), roofs 400, floors 100 to 4,000, stone walls 1,500, black marble 1,500.

### How the look changes with damage

Across the building pieces (`damage` in the data):

- **Worn (below 75 %)** swaps the **material** on 170 building pieces, the mesh and material on 31. The worn material
  is the new one with: a worn albedo (wood grey-green, darkwood grey with scratches, iron rust, grausten cracked) or
  the same albedo under a darker or rust `_Color`; `_BumpScale` x1.5 to x2; `_Glossiness` down (woodwall 0.25 to
  0.1); `_ValueNoise` up 0.25 to 0.5; `_RippleDistance` doubled (0.03 to 0.06: the worn piece warps more).
- **Broken (below 25 %)** swaps the **mesh and material** on 155 building pieces: the same parts shortened, tilted a
  few degrees and some removed (woodwall's planks cut to 1.77 m and leaning 7 to 16 degrees; the darkwood roof with
  shingles missing; the workbench with planks fallen on the ground), in the worn material. 46 swap only the material.
- Furniture and stations mostly keep one object for all three looks (37 and 34 pieces).
- `m_switchEffect` plays when the look changes: `vfx_SawDust` on 61 pieces (the darkwood, stave and other modelled
  wood sets), a stack's or station's own place effect on a few, none on the rest. Give a new wooden piece
  `vfx_SawDust`.

For a new piece: model the new look; make the worn look the same mesh with the set's worn material; make the broken
look a copy with parts cut short, tilted 7 to 16 degrees and one or two removed, in the worn material.

### Fragments

On destruction every client runs `Destructible.CreateFragments`: each active `MeshRenderer` under the fragment roots
(or, without roots, every visible one) is copied as a rigid body with a box collider at 0.9 of its scale, pushed out
from the common centre at 4 m/s plus up to 1 m/s at random, and removed after 2 or 3 s. How the game provides them:

- **Pre-cut chunks** (223 pieces, the modelled ones): an inactive `<Name>Destruction` child holding the broken mesh
  split into 12 to 30 separate chunk meshes (12 to 184 triangles each; the workbench 28, the forge 28, the hearth 12),
  set as `m_fragmentRoots`.
- **Source parts** (27, the cube-built wood pieces): the `high` object under each look, holding the separate planks
  with disabled renderers that `SimpleMeshCombine` merged into `_Combined Mesh [high]`.
- **Visible renderers** (80, mostly furniture): no roots; the visible renderers fly as they are.

A new piece gives its destruction as a `Destruction` child of 8 to 30 chunk meshes in the worn material, or at least
splits its visible mesh into its main parts, so it does not leave as one block.

### Effects by material

The effects a piece plays, by WearNTear material (counts of pieces; reuse these by prefab name):

| Material | Hit (`m_hitEffect`) | Destroyed (`m_destroyedEffect`) | Place: sound |
| --- | --- | --- | --- |
| Wood, HardWood, Timberwood | `vfx_SawDust` (272 pieces); logs add `sfx_tree_hit` | `vfx_SawDust` + `sfx_wood_destroyed` (257) | `sfx_build_hammer_wood` (239) |
| Stone, Ashstone | `vfx_RockHit` + `sfx_rock_hit` | `sfx_rock_destroyed`, stone adds `vfx_RockDestroyed` | `sfx_build_hammer_stone` (76) |
| Marble | `vfx_MarbleHit` + `sfx_rock_hit` | `vfx_MarbleDestroyed` + `sfx_rock_destroyed` | `sfx_build_hammer_stone` |
| Iron | `vfx_HitSparks` (iron) or `vfx_SawDust` (iron-bound wood) | `vfx_HitSparks` + `sfx_metal_blocked`, or the wood pair | `sfx_build_hammer_metal` (38) |
| Ice, crystal | none | `vfx_icecube_destroyed` + `sfx_ice_destroyed`; `fx_crystal_destruction` | `sfx_build_hammer_crystal` |
| Pots, small props | | `sfx_clay_pot_break`, `sfx_build_hammer_default` | `sfx_build_hammer_default` (31) |

Stacks and a few pieces have destroy effects of their own (`vfx_wood_stack_destroyed`, `vfx_stone_wall_4x2_destroyed`,
`vfx_coin_pile_destroyed`); a new piece of an existing material uses the shared pair.

### Snow and rain

- **Snow cover.** A piece carries one (244 pieces) to five inactive snow caps: meshes from the game's snow library
  scaled to the piece's top faces, drawn with `GameElements/Pieces/_res/snow_flat.mat` (214 caps), `snow.mat` (49),
  `snow_flat_low.mat`, `snow_flat_low2.mat` or `snow_rooftop.mat`, all on the SnowMesh shader. Library meshes by use:
  `Snow2_Beam` (70), `Snow2_2x2` (60), `Snow2_1x1` (54), `Snow2_BeamTop` (35), `Snow2_Beam26/45/67`, `Snow2_Beam4`,
  `26/45/67_Shingles_Snow2`, `26/45/67_IC_Snow2` and `_OC_Snow2` for inner and outer roof corners, `Snow2_Spiral`.
  `WearNTear.m_snow` (and `m_snowWorn`, `m_snowBroken`) name the cap; the game shows it once the snow build-up passes
  25 % and drives `_SnowLevel` from 0 to 1 over 25 to 100 %. A new piece reuses a library cap (borrowed at runtime)
  scaled to its top: woodwall's is `Snow2_Beam` scaled (1, 1, 0.67) 0.7 m up.
- **Rain.** `_AddRain` darkens and glosses the material outdoors. `WearNTear.m_wet` (an object shown while wet) is used
  by only 4 pieces.

## Placement

- **Place effects.** Every piece plays a `vfx_Place_*` dust burst and a `sfx_build_hammer_*` sound
  (`Piece.m_placeEffect`). The vfx emit from a box sized to a footprint, so a new piece takes the one whose box
  matches its own (`data/pieces.json` `place_effects`): `vfx_Place_wood_wall` 2 x 2 x 0.2 m (93 pieces),
  `vfx_Place_wood_pole` 0.3 x 1 x 0.3 (90), `vfx_Place_stone_wall_2x1` 2 x 1 x 1 shell (51), `vfx_Place_wood_roof`
  2 x 0.32 x 2.2 (30), `vfx_Place_wood_beam` 1.5 x 0.4 x 0.4 (29), `vfx_Place_workbench` 3 x 1 x 1 (17),
  `vfx_Place_chest` 1 x 1 x 1 (16), `vfx_Place_stone_wall_4x2` 4 x 2 x 1 (11), `vfx_Place_wood_floor` 2 x 0.19 x 2,
  `vfx_Place_wood_wall_half` 2 x 0.97 x 0.2, `vfx_Place_wood_stair`, `vfx_Place_bed` 2.86 x 1 x 1.
- **The ghost.** `Player.SetupPlacementGhost` instantiates the prefab itself: joints, rigid bodies, lights, light
  flicker and LOD, guide points and terrain modifiers are removed, particle systems hidden, audio disabled, every
  transform moved to the `ghost` layer, colliders outside the placement ray mask disabled, shadows off, and each
  material copied with `_ValueNoise` 0 and `_TriplanarLocalPos` 1. A child named `_GhostOnly` is switched on (the
  hoe's terrain pieces use it for their outline). Invalid placement tints `_Color` red and `_EmissionColor` red x 0.7.
  So **the prefab is its own ghost**: nothing extra is modelled, and any helper that should only show while aiming
  goes under `_GhostOnly`.
- **The support highlight** (hammer, repair) tints `_Color` from red to green by support and adds 40 % of it to
  `_EmissionColor` for 0.2 s; materials without `_EmissionColor` show it weakly.

## Build menu icons

Read from the icon atlas (`Texture2D/sactx-0-4096x4096-BC7-IconAtlas-7390861a.png`; each `Piece.m_icon` is a sprite
in it) and the contact sheets `codex/out/pieces/icons/*.png`; numbers from 388 icons (`data/pieces.json` `icons`):

- **64 x 64 px** (386 of 388), transparent background (clear corners on 373), no frame, no outline, no drop shadow.
- **Framing:** the drawing is centred (centre at 0.50, 0.51 of the square) and its long side fills a median 88 % of the
  square (p10 70 %, p90 95 %); opaque pixels cover a median 27 % of it. **Within a family the scale is shared:** the
  2 m wall fills the frame, the 1 m quarter wall is drawn at half that size, the 1 m beam at half the 2 m beam, so a
  small piece gets a small icon.
- **Angle:** flat pieces (walls, gables, beams, poles, doors, windows) straight on from the front, orthographic, so a
  diagonal beam shows its slope; volumetric pieces (roofs, stairs, furniture, stations, stacks) in a high
  three-quarter view from the front-left, about 30 degrees down and 30 to 45 degrees round.
- **Lighting:** the game's own shading, a warm key from the upper left and front, the right and lower faces a step
  darker, no rim light; textures read at icon size. Median luminance 0.35, saturation 0.13: icons are as dark and
  muted as the pieces.
- **Edges:** anti-aliased, about 18 % of the drawing's pixels part-transparent.

## Per kind

### Walls (`piece.wall`)

Full 2x2, half 2x1, quarter 1x1 and 1x2 panels, 4x2 for stone and grausten. The wood wall is four 0.5 m planks with
two battens behind (264 triangles); newer sets are one slab with a frame (ashwood_wall_2x2 232, Piece_grausten_wall_2x2
44 triangles, the detail entirely in the tiling texture). Decorative walls cut their pattern as geometry
(darkwood_decowall 1,320, ashwood_decowall_2x2 1,072). Stone walls are a single lumpy block 1.2 m thick. Iron walls
are a grid of square bars 12 cm deep (iron_wall_2x2, 200 triangles, the 32 px iron cage texture).

### Gable ends (`piece.gable`)

Triangles and trapezoids that close a roof's end at 26, 45 and 67 degrees, 3 snap points (5 with a ridge row), an
upside-down twin for each. wood_wall_roof_a: planks under a 0.4 m capping beam along the slope (176 triangles).
Colliders on gables and roofs are tagged `roof` (21 of 29).

### Floors (`piece.floor`)

1x1 and 2x2 (4x4 in grausten). Wood floors are 0.1 m planks on battens (wood_floor 220 triangles, 51.7 px/m); stone
and marble floors are 1 m thick slabs with 8 snap points (stone_floor_2x2 80 triangles, 256 px).

### Roofs (`piece.roof`)

Slope, ridge (top), inner and outer corner at each pitch. The thatch roof is a plank deck with a crossbeam and three
alpha straw cards and an under-layer; the darkwood roof is modelled diamond shingles (488 triangles, 30.8 px/m); the
grausten roof is overlapping slate slabs with a second material. Roofs overhang their snap box; the ridge adds a
rounded capping beam. Every roof carries its own snow cap from the roof library.

### Beams and poles (`piece.beam`, `piece.pole`)

1, 2 and 4 m, and diagonal beams at the three pitches with snap points at both ends. Plain beams and poles are one
bevelled box, 44 triangles (wood_beam 0.4 m square). Darkwood beams are carved twists (524 to 1,532 triangles), stave
beams 0.71 m square with a carved band (100 triangles plain, 400 to 900 decorated), the iron-bound beam 532 triangles
with modelled straps and rivets. Logs are 8-sided with chamfered ends, 0.53 m across, 64 to 76 triangles. Poles centre
their pivot.

### Stairs (`piece.stair`)

One metre up over 2 m. wood_stair: four 0.5 m treads on two stringers (264 triangles); stone and marble stairs are
solid wedges with 5 to 7 steps (152 and 112 triangles); the dvergr spiral stair is 1,328.

### Doors, gates and windows (`piece.door`, `piece.gate`, `piece.window`)

- The moving leaf is a child named **`door`** with its origin on the hinge line; the `Animator` on the root plays a
  controller whose clips rotate `door` about Y (`GameElements/Pieces/_res/door/`). `Door` sets the animator's integer
  `state` to 1 or -1 by which side of the door's forward axis the player stands on (`opening_forward` turns the leaf +90
  degrees about Y, `opening_backward` -90), and to 0 to close.
- Clips: `door_animator` (wood_door, wood_gate, iron_grate): 0 to 90 degrees in 0.5 s; `darkwood_gate_animator`: 90
  degrees in 1.0 s; `Flametal_gate_animator`: 90 degrees in 2.0 s; `window_animator` (wood_window): 135 degrees in
  0.5 s; the drawbridges lower 79 degrees about Z in 1.5 s (down) and 2.2 s (up) with counterweights. Light doors 0.5
  s, heavy gates 1 to 2 s.
- The frame is part of the piece (wood_door: two 0.5 x 0.3 m posts and a threshold, the leaf 1.4 x 1.88 m).
- Sounds by weight (`Door.m_openEffects`, `m_closeEffects`): `sfx_door_open`/`_close` (wood), `sfx_darkwood_door_open`
  (darkwood, drawbridges), `sfx_metalgate_open` (iron grate), `sfx_flametalgate_door_open`, `sfx_window_open`.
- Grausten windows are wall slabs with the opening cut, no moving part.

### Fences, arches, blocks (`piece.fence`, `piece.arch`, `piece.block`)

Fences are 2 x 1 m with 4 snap points (wood_fence: stake-textured logs, 512 triangles). Arches are 2 x 2 m brackets
(darkwood_arch 1,280 triangles with a carved twist) or stone and marble voussoirs. Black marble blocks are 1 to 2 m
cubes and bases, 252 to 326 triangles, 8 snap points, triplanar.

### Defences (`piece.defence`)

Stake walls: five sharpened logs 0.4 to 0.5 m thick, 3.6 m tall, bark texture with pale cut tips (stake_wall 502
triangles); sharp stakes lean out at 45 degrees on crossed rails and carry an `Aoe` and a trigger collider on
`character_trigger`.

### Stacks (`piece.stack`)

Resource piles built from the item's own model and material (bars, coins, logs), no snap points, health 5 to 100,
LOD 1 only, pivot on the ground, each with a destroy effect named after it.

## Checklist for a new building piece

1. Category key and set chosen; the set's texture and material borrowed, not repainted.
2. Size on the 1 m grid (2 m module), mesh 1 to 5 % proud of the snap box (10 % for stone), roofs overhanging.
3. Pivot at the bottom centre (the eave for a roof, the top for anything that hangs).
4. Triangles at the category median; parts no finer than 6 cm; one chamfer per edge at 2 to 3 % of the section.
5. Texel density 27 to 38 px/m, the set's 128 px texture, islands laid into the set's atlas strips with the grain
   along the strip.
6. Children: `New` (with a LODGroup: LOD 0, and LOD 1 a box or an 18 % mesh, culled at 2 %), `Worn` (worn material),
   `Broken` (cut, tilted, worn material), `Destruction` (8 to 30 chunks) set as fragment roots, a snow cap from the
   library, snap points at the box corners (inactive, tag `snappoint`, `$hud_snappoint_*` names).
7. Root: `Piece` (category tab, place effects matching the footprint), `WearNTear` (material type of the set,
   health from the table, hit and destroy effects of the material), `ZNetView`, a box collider on layer `piece`
   tagged `roof` or `leaky`.
8. Icon: 64 px, transparent, front view for flat pieces or high three-quarter for volumes, long side at 88 % for the
   family's largest size and in proportion for smaller ones.

## Not measured

- The Custom/Piece shader's code is not in the export (the shader is a dummy with its properties), so what
  `_ValueNoise`, `_RippleDistance`, `_AddRain` and `_AddSnow` do on screen is read from their names, their values, and
  how the game's code sets them (the ghost, fragments, `WearNTear`), not seen in game. The Blender renders use plain
  Principled materials with the textures, so the renders show shapes, texture use and proportions, not the game's
  lighting.
- The shading of icons is described from the atlas; the icon render setup (camera, lights) is not in the export.
- Snow caps and destruction are measured from the prefabs, not watched in game.

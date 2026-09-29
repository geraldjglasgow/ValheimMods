# Furniture

Chairs, benches and thrones, tables, beds, chests and other containers, torches, braziers and lanterns, hearths and
fire pits, banners and hanging cloths, rugs, and decor (item and armour stands, roof ornaments, garlands, the sign).
Furniture is built with the hammer like building pieces and shares their anatomy, shader, damage, placement and icon
rules (`pieces.md`); this page gives what differs and the scripts that make furniture work.

Measured on 2026-09-29 from the hammer's piece table by `codex/measure/pieces.py` (`codex/data/pieces.json`, categories
`furniture.*`, 104 pieces). Looks read from `codex/out/pieces/renders/furniture.*.png`, `renders/close_*.png`,
`textures/*.png` and `icons/furniture.*.png`. Reference prefabs are `GameElements/Pieces/<name>.prefab` in the
reference export.

## The numbers

| Category | n | Triangles median (range) | Texture px | px per metre median (IQR) | Longest m median | References |
| --- | --- | --- | --- | --- | --- | --- |
| `furniture.chair` | 14 | 731 (80 to 14,240) | 128 (64 to 256) | 45.2 (35.9 to 63.3) | 2.52 | piece_chair, piece_chair02, piece_bench01, piece_throne01, piece_logbench01 |
| `furniture.table` | 6 | 498 (224 to 1,050) | 256 (128 to 256) | 49.4 (44.6 to 55.0) | 2.54 | piece_table, piece_table_oak, piece_table_round, piece_blackmarble_table |
| `furniture.bed` | 3 | 1,550 (378 to 1,578) | 128 to 256 | 27.3 (27.2 to 33.4) | 3.13 | bed, piece_bed02, ashwood_bed |
| `furniture.chest` | 14 | 460 (284 to 6,168) | 128 (64 to 256) | 45.0 (35.4 to 66.1) | 1.05 | piece_chest_wood, piece_chest, piece_chest_private, piece_chest_blackmetal, piece_chest_barrel |
| `furniture.light` | 16 | 621 (66 to 3,570) | 64 (32 to 128) | 57.3 (32.1 to 87.2) | 1.02 | piece_groundtorch_wood, piece_groundtorch, piece_walltorch, piece_brazierfloor01, piece_dvergr_lantern |
| `furniture.hearth` | 4 | 1,117 (450 to 1,730) | 64 to 256 | 42.0 (37.7 to 45.3) | 2.85 | fire_pit, hearth, bonfire, fire_pit_iron |
| `furniture.cloth` | 14 | 148 (148 to 452) | 128 | 51.8 | 3.03 | piece_banner01, piece_banner03, piece_cloth_hanging_door, piece_cloth_hanging_door_blue |
| `furniture.rug` | 11 | 52 (18 to 2,618) | 128 (128 to 256) | 42.7 (38.5 to 47.4) | 3.11 | rug_deer, rug_wolf, rug_fur, jute_carpet |
| `furniture.decor` | 17 | 782 (44 to 12,612) | 128 (128 to 256) | 52.5 (43.0 to 85.2) | 2.20 | itemstand, itemstandh, ArmorStand, wood_dragon1, darkwood_raven, sign |

How furniture differs from building pieces:

- **Denser texels.** Furniture sits at 42 to 57 px per metre against 27 to 38 for walls and roofs: it is seen from
  closer. Tables and pots use 256 px; torches and braziers 32 to 64 px tiled textures.
- **One unique atlas per piece**, not a shared set texture: `Table_d.png` (256), `BedSimple_d.png` (128),
  `woodchest_d.png` (128), `RavenThrone_d.png` (128), each with the piece's own islands. The few shared ones: the runed
  furniture texture (bench, chair, two tables), `Table_oak_d.png` (oak and round tables), the ceramic pots texture, the
  plank texture on banner poles.
- **No snap points** (except the hearth's 8, the bathtub's 8 and the roof ornaments' single point); pivot on the
  ground at the centre of the footprint for everything that stands (36 of 37 chairs, tables, beds and chests have
  their lowest vertex within 0.1 m of it).
- **Low health, one look.** Health 50 to 300 (chests 100 to 1,000, the private chest 10,000). 38 furniture pieces keep
  one object for new, worn and broken, 30 swap only the material; destruction flies the visible renderers apart (47)
  or pre-cut chunks (35).
- **Colliders** are a `leaky` box on layer `piece` for anything that blocks; `piece_nonsolid` boxes for what the player
  walks through (rugs, hanging cloths, garlands) and for benches' seats; a `character_trigger` sphere for fires and
  lights (the warmth and light areas).
- **Comfort.** `Piece.m_comfort` 1 to 3 with a group: chairs 1 (thrones 3), tables 1 to 2, beds 1 to 2, rugs 1 to 2,
  banners 1, lights 1 to 2, fires 1 to 2.

## Colours and paint

Measured inside each texture's UV islands with the material tint applied (`data/pieces.json` `paint`); read from
`codex/out/pieces/textures/`:

| Piece | Texture (px) | Tint | Median colour | Luminance p10 to p90 | What it looks like |
| --- | --- | --- | --- | --- | --- |
| table | `GameElements/Pieces/_res/Table/Model/Table_d.png` (256) | 0.71, 0.70, 0.61 | #997449 | 0.42 to 0.52 | pale golden planed wood, long wavy grain, a few dark crack strokes and nail slots; tiles on the top |
| bed (simple) | `GameElements/Pieces/_res/BedSimple/Model/BedSimple_d.png` (128) | 0.80, 0.78, 0.64 | #a27a4d | 0.45 to 0.56 | the same golden wood; a straw mattress island top right, pale tan with black gaps |
| raven throne | `GameElements/Pieces/_res/RavenThrone/Model/RavenThrone_d.png` (128) | 0.86, 0.83, 0.62 | #a2794e | 0.40 to 0.60 | pale wood with a dark green-black knotwork panel |
| wood chest | `world/Props/Chests/materials/woodchest_d.png` (128) | 1.0 | #634a2b | 0.30 to 0.35 | mid-brown planks with horizontal grain, a pale end-grain disc, a small rope ring |
| reinforced chest | `world/Props/Chests/materials/ironchest_d.png` (128) | 1.0 | #705a39 | 0.37 to 0.40 | brown planks, grey iron bands |
| torch | `world/textures/wood.png` (32), tiled | 0.76 | #a9764f | 0.44 to 0.53 | orange-tan wood with horizontal streaks, 32 px |
| brazier | `GameElements/Pieces/_res/Brazier/Model/Brazier02_d.png` (64) | 1.0 | #c09263 | 0.54 to 0.66 | pale bone-tan wood and dark horn |
| hearth | `GameElements/Pieces/_res/HearthNew/model/HeartNew_d.png` (256) | 1.0 | #52514c | 0.09 to 0.43 | grey fieldstones, dark mortar lines, soot-black blotches round the edges; the ash bed is its own material |

- **Tints warm and darken the wood.** Beds, tables and thrones multiply their texture by about (0.8, 0.78, 0.64): the
  wood reads golden brown, not orange. Paint furniture wood to the tinted value, or set the tint.
- Furniture wood is lighter than building wood (luminance 0.45 to 0.52 against 0.26 to 0.31 for the plank walls): it
  is planed and oiled, the walls weathered.
- Cloth, hide and straw are painted with light baked in and ragged, alpha-cut edges (see Rugs and Cloth).

## Chairs, benches and thrones (`furniture.chair`)

- **Seat heights:** the stool (piece_chair) is 0.59 m tall, the plain bench (piece_bench01) 0.54 m, the log bench
  0.59 m; chairs with backs 1.2 to 1.28 m (piece_chair02, piece_chair03), thrones 2.8 to 3.7 m (piece_throne01 2.79,
  piece_moose_throne 3.68). Benches are 2.0 to 2.7 m long (two sitters).
- **`Chair`:** `m_attachPoint` is a child at or near the root: on the ground under the seat (piece_chair 0, 0.1, 0;
  piece_bench01 0.03, -0.02, 0; the log bench 0.7 m to one side), thrones 0.05 to 0.22 m back from the centre. The
  animation carries the sitter up to the seat, so the seat must sit at the height the animation expects: 0.45 to
  0.55 m for `attach_chair`, the throne seat for `attach_throne` (piece_throne01's seat). `m_useDistance` 1.5 m,
  `m_detachOffset` (0, 0.5, 0).
- Parts are chunky: the plain bench is a 2.5 x 0.61 m top of two planks on two trestle legs, 168 triangles; the stool
  four splayed legs and a square top, 160 triangles on a 256 px texture. Thrones are carved showpieces (694 to 14,240
  triangles) with raven heads, knotwork panels, antlers.
- Colliders: a `leaky` box on `piece` for the frame and a `piece_nonsolid` box for the seat.
- Place with `vfx_Place_wood_pole` (benches, chairs) or `vfx_Place_throne02` (thrones); destroy with the material's
  pair.

## Tables (`furniture.table`)

- **Top at 0.78 to 0.83 m** (piece_table 0.83, piece_table_oak 0.82, piece_table_round 0.82, the runed tables 0.78).
- Sizes: 2.5 x 1.25 m (piece_table), 6.6 x 1.9 m (piece_table_oak, a feast table), 2.5 m round, 1.0 x 1.0 m (the small
  runed table).
- piece_table: a top of wide planks on two trestle legs with a stretcher, 224 triangles, 256 px, 43.7 px/m; the oak
  and round tables add iron nail heads (modelled) on each board end.
- Place with `vfx_Place_workbench` (3 x 1 x 1 m box).

## Beds (`furniture.bed`)

- **3.1 to 3.5 m long, 1.7 to 2.8 m wide** (bed 1.70 x 0.68 x 3.13 m; piece_bed02, the dragon bed, 2.79 x 1.75 x 3.49).
- **`Bed.m_spawnPoint`:** a child at (0, 0.44, 0), the mattress top at the centre; the player respawns there. A
  `GuidePoint` child at 0.47 m and a `PlayerBase` sphere (`EffectArea`) for the base effect.
- Parts: a wooden frame (sides, head and foot boards, corner posts) and the bedding as separate soft shapes: a straw
  mattress (bed), hides and furs thrown over (piece_bed02, ashwood_bed). The bedding is painted, not modelled in folds.
- Place with `vfx_Place_bed` (2.86 x 1 x 1 m box). Collider: a `leaky` box 1.2 x 0.4 x 2.8 m on `piece`.
- LOD: bed 378 triangles to 133; the fancy beds 1,550 to 395.

## Chests and containers (`furniture.chest`)

- **Sizes and grids** (`Container.m_width` x `m_height`): piece_chest_wood 1.65 x 0.79 x 0.75 m, 5 x 2; piece_chest
  (reinforced) 1.84 x 1.09 x 0.97 m, 6 x 4; piece_chest_private 0.92 x 0.54 x 0.49 m, 3 x 2; piece_chest_blackmetal
  2.31 x 1.04 x 1.52 m, 8 x 4; piece_chest_barrel 0.80 x 0.90 x 0.80 m, 6 x 2; piece_chest_grausten 8 x 5;
  piece_chest_warderobe 1.68 x 2.67 x 1.11 m, 5 x 10. Bigger grid, bigger chest.
- **The lid is two static copies**, `m_open` and `m_closed`, and the game swaps them with `SetActive`; there is no lid
  animation. The open copy is posed 30 degrees up about the back edge (piece_chest_blackmetal, piece_chest_private
  (0.259, 0, 0, 0.966)) to 33 degrees (piece_chest), the wood chest's slid 0.39 m back. Model the body and the lid
  as separate meshes, pose a second lid open at 30 degrees, parent both under the look.
- Sounds: `sfx_chest_open` and `sfx_chest_close` on every chest. Place with `vfx_Place_chest` (1 x 1 x 1 m).
- Bodies are boxy and flare out towards the lid (wood and reinforced chests), with modelled iron bands and a hasp on
  the reinforced ones; 408 to 584 triangles for wood chests, 1,826 to 6,168 for the late ones.
- Health is high for the iron chests (1,000) against 100 for the wood chest; the look never changes (one object for
  all three states).

## Torches, sconces, braziers and lanterns (`furniture.light`)

A light is a model plus a fire object the game switches on while it has fuel (`Fireplace`):

- **The model.** Tall thin shapes, low triangle counts on tiny tiled textures: piece_groundtorch_wood a 1.41 m stick,
  66 triangles, 128 px; piece_groundtorch (iron) 1.48 m, 622 triangles, a 32 px tiled texture; piece_walltorch
  0.95 m on a wall bracket, 817 triangles; braziers 1.0 x 1.0 m bowls on crossed legs, 882 triangles, 64 px. The
  standing torch's pivot is at mid height (-0.65 to +0.76 m): the piece is placed where the ray hits, and the stick
  goes into the ground.
- **`Fireplace.m_enabledObject` `_enabled`** (inactive in the prefab), holding:
  - `Point light`: colour (1.0, 0.621, 0.482), intensity 1.5, range 10 to 15 m, no shadows for torches;
    `LightFlicker` intensity 0.1, speed 10, movement 0.1; `LightLod` light distance 80 m, shadow distance 20 m.
    Blue and green torches: (0.48, 0.77, 1.0) and (0.63, 1.0, 0.48), range 10 m. Dvergr lanterns (1.0, 0.785, 0.288),
    range 6 m, soft shadows. Candles (1.0, 0.646, 0.222), intensity 2, range 1 m.
  - `fx_Torch_Basic` particles with a `flare` child at the flame.
  - `sfx_fire_loop` (`ZSFX` + `AudioSource` + `TimedDestruction`).
  - `FireWarmth`: a sphere trigger on `character_trigger` with an `EffectArea` (warmth).
- **Braziers** add `_enabled_low` (a dim light 1.5, range 2.5 m, colour 0.84, 0.53, 0.41, flicker 0.2 at 12) and
  `_enabled_high` (intensity 2, range 10 m, soft shadows) that the game swaps at half fuel, plus a `SmokeSpawner`
  and an `Aoe` burn box.
- **Fuel:** resin for torches (start 2, max 4 to 6, 10,000 to 20,000 s each), coal for braziers (max 5), greydwarf eye
  and guck for the coloured torches. Refuel effects: `vfx_groundtorch_addFuel` or `vfx_walltorch_addFuel` with
  `sfx_FireAddFuel`.
- Lights have no worn look (one object) and fly apart as their visible renderers.

## Hearths, fire pits and the bonfire (`furniture.hearth`)

- **Sizes:** fire_pit a 1.58 m ring of stones, 0.25 m high (1,730 triangles, 64 px); fire_pit_iron a 1.5 m iron bowl
  on legs (450 triangles, 128 px); hearth a 4.1 x 3.1 m stone kerb 0.76 m high with 8 snap points (1,198 triangles,
  256 px, stone, 1,500 health); bonfire 4.3 m across, a heap of charred logs (1,036 triangles, 256 px).
- **Three fire objects** (`Fireplace`): `_enabled` (always while lit: the fire loop sound and glowing ash meshes in a
  `HearthAshesBurn` material), `_enabled_low` (below half fuel: a dim point light 1.5 at 3 m, colour 0.84, 0.53, 0.41,
  flicker 0.2 at speed 12, small smoke), `_enabled_high` (above half: an `Aoe` burn box `FireBurn`, a `SmokeSpawner`,
  an ash bed mesh, the main light, `FireWarmth`, and particles: smoke, low flames, flames, flare).
- **Lights:** hearth (1.0, 0.621, 0.482) intensity 2, range 13 m, soft shadows; fire pits and bonfire (1.0, 0.504,
  0.324), range 10 m (fire pits) to 20 m (bonfire), flicker 0.1 at 10 to 15 with movement 0.1 to 0.2; `LightLod` light
  100 m, shadows 20 to 25 m.
- **Fuel:** wood, max 10 (fire pits, bonfire) or 20 (hearth), 5,000 s each. Refuel: `vfx_FireAddFuel`,
  `vfx_HearthAddFuel` or `vfx_bonfire_AddFuel`, with `sfx_FireAddFuel`. Placing a fire also plays
  `snow_decrease_firepit_placed_small` or `_large`, which melts snow round it.
- The stones are painted grey fieldstone with dark mortar and soot blotches (#52514c median); the ash bed and logs are
  separate materials so they can glow.

## Banners and hanging cloths (`furniture.cloth`)

- **piece_banner01 to 11:** a 1.41 m pole, 0.2 m square, at the top (the plank texture, 44 triangles) and a flat cloth
  2.93 m long and 1.18 m wide hanging from it (104 triangles). One mesh shape for all eleven; only the material changes
  (black and white border, red and white stripes, blue and yellow ...). The pivot is at the pole, the cloth hangs down.
- **The cloth uses the Vegetation shader**, so it sways in the wind: `_Cull` 0 (two-sided), alpha cutout 0.5 for the
  ragged lower edge and swallowtail, `_ValueNoise` 0.5, `_AddRain` 1. A new banner uses a copy of a banner material
  with its own texture.
- **Texture layout** (`GameElements/Pieces/_res/Banners/Model/Banner_1_borderBlackWhite_d.png`, 128 px): the cloth on
  the left 60 %, painted dark grey with a pale embroidered knot border and a pale line down each side, a ragged
  transparent edge; the pole's wood strip on the right. Median cloth colour #373737.
- Hanging cloths (piece_cloth_hanging_door, the dvergr curtains): 2.5 to 3.0 m cloths on a 1.1 to 1.4 m pole, collider
  on `piece_nonsolid` so the player walks through.
- Health 50, no worn look, comfort 1 (banners) or 2 (curtains).

## Rugs (`furniture.rug`)

- **Flat and cheap:** 18 to 200 triangles (rug_deer 50, rug_asksvin 18, rug_wolf 200), 3 x 3 to 3.4 x 4 m (rug_hare
  1.1 x 1.9 m), 1 to 9 cm thick, sitting 3 to 6 cm above the pivot so they do not flicker against the floor.
- **The Rug shader** (`Custom/Rug`): albedo, normal, alpha cutout (0.5 to 0.9) for the hide's outline, and a near/far
  depth fade (`_ZFadeDistance` 0.3) that keeps a rug lying on a floor from z-fighting. Hides use the animal's hide
  texture (rug_deer: `GameElements/Items/_res/Hide/hide01.png`, tint 0.74).
- Collider: a `piece_nonsolid` box 5 cm high the size of the rug.
- rug_Bjorn is the exception: a bear with a modelled head (2,618 triangles) that is also a `Chair`.

## Decor (`furniture.decor`)

- **Item stands:** `itemstand` (wall) is a 0.5 x 0.5 x 0.2 m board, 88 triangles; `itemstandh` (floor) a 0.2 m disc.
  `ItemStand.m_attachOther` is where the item hangs; the item's own model is shown there.
- **ArmorStand:** a skinned player mannequin (3,546 triangles, `PlayerMaterialArmorStand*`) on a wooden cross base,
  health 600.
- **Roof ornaments** (wood_dragon1, darkwood_raven, darkwood_wolf): carved heads 1.5 to 2 m tall, 3.1 to 3.3 m long,
  402 to 892 triangles, one snap point at the base so they sit on a ridge or beam end; the darkwood ones use the
  darkwood atlas (`pieces.md`).
- **Sign:** a 1.0 x 0.5 x 0.1 m board (44 triangles) with a `TextMeshPro` child for the text.
- **Garlands:** alpha-cut cards on `piece_nonsolid`, 4.1 to 4.2 m long, two-sided (the celebration garland's
  `_Cutoff` is 0.11).
- Seasonal pieces (the Yule tree, crown, garland, mistletoe, maypole) use their own textures and have no worn look.

## Icons

As for building pieces (`pieces.md`): 64 px, transparent, no outline. Furniture is drawn in the high three-quarter
view from the front-left; chests show the lid closed, lights unlit, beds with their bedding, thrones straight on so
the back panel reads.

## Checklist for new furniture

1. Category key; the reference to line up with (a chair beside piece_chair02, a chest beside piece_chest_wood).
2. Pivot on the ground at the footprint's centre (at the pole for anything that hangs; mid height for a staked torch).
3. Real-world heights: seats 0.45 to 0.6 m, table tops 0.8 m, beds 0.45 m to the mattress, chests 0.55 to 1.1 m.
4. 300 to 1,500 triangles, one 128 or 256 px atlas of its own, 42 to 57 px per metre; LOD 1 at 20 to 30 % of the
   triangles, handing over at 20 to 30 % of screen height.
5. The script's objects: a `Chair` attach point on the ground under the seat, a `Bed` spawn point at mattress height,
   an open and a closed lid for a `Container`, an `_enabled` (and for big fires `_enabled_low` and `_enabled_high`)
   fire object with the light, flicker, LOD, particles, sound and warmth for a `Fireplace`.
6. Collider on `piece` tagged `leaky`; `piece_nonsolid` for anything walked through.
7. One look for all three states, or a worn material; destruction as visible renderers split into parts.
8. Place effect by footprint (`vfx_Place_chest`, `vfx_Place_bed`, `vfx_Place_workbench`, `vfx_Place_wood_pole` for
   thin things) and the material's hammer sound.

## Not measured

- The height the `attach_chair` and `attach_throne` animations lift the sitter to is not measured (the clips belong to
  the player's controller); seat heights are taken from the game's chairs, whose attach points sit on the ground.
- Wind sway on banners is read from their shader (Vegetation), not watched.
- Light colours and ranges are the prefab values; how bright they look depends on the game's lighting, which the
  Blender renders do not reproduce.

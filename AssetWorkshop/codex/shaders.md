# Shaders

Which shaders the game's materials use, what their properties do, how the game sets them, and which one a new asset
is dressed in when a mod loads it. Written from `measure/shaders.py` (data in `data/shaders.json`): every `.mat` of the
reference export, the shader dummies AssetRipper exports into `Shaders/` (they keep every property with its inspector
label, type and default, not the shader code), and the prefab scan in `measure/shaders_usage.py`, which counts only the
materials the game's prefabs draw. The export also holds each model file's import materials (2,176 on Unity's Standard
shader), most of which the game never shows; counts below are of **drawn** materials: the 1,958 materials the
renderers of items, pieces, creatures, world objects, locations and effects use (3,630 counting the model prefabs'
own).

How a material's textures are painted is in `paint.md`; the recolour properties (`_Hue`, `_Style`, `_Color`) in
`recolour.md`; particle shaders in detail in `vfx/`.

## The shaders the game draws with

| Shader (file) | Drawn materials | Drawn on | Typical use |
| --- | --- | --- | --- |
| Unity **Standard** (built-in, fileID 46) | 560 | item 380, piece 156, env 99, vfx 69, creature 65 | food, fish, gems, ingots, eyes, potions, many Mistlands and Ashlands props; untextured tinted materials (`paint.md`) |
| **Custom/Creature** (`Creature.shader`) | 336 | item 268, creature 109, piece 53, vfx 32, env 26 | every weapon, tool, shield and armour item; creature bodies and their gear; some furniture (Dvergr beds, charred benches) |
| **Custom/Piece** (`Piece.shader`) | 332 | piece 305, item 91, env 71, creature 43 | building pieces, furniture, stations, chests; world copies of pieces in locations |
| **Custom/StaticRock** (`StaticRock.shader`) | 158 | env 121, piece 87, location 31 | rocks, cliffs, ore veins, runestones, stone location parts, ice |
| **Custom/Vegetation** (`Vegetation.shader`) | 150 | piece 93, env 78 | trees (bark and leaves), bushes, crops, banners, sails, tents, hanging cloth |
| Particles/Standard Unlit (`Shaders/BuiltInOverrides`) | 92 | vfx 70 | sparks, glows, additive and blended particles |
| Lux Lit Particles Bumped | 43 | vfx 39 | lit smoke, fog, dust |
| **Custom/Fallen Warrior** | 36 | item 30, creature 6 | the `FW_` and `SP_` copies of player armour worn by the Fallen Warrior, laid out like `Custom/Player` |
| Particles/Standard Surface | 32 | vfx 28 | lit particles, blood splats, webs |
| Custom/LitParticles | 24 | vfx 18 | rain, snow, portal fields, soft lit smoke |
| **Unlit/WeaponGlow** | 21 | item 21 | the glowing overlay mesh of the Ashlands weapons' blood, lightning and nature variants (AxeBerzerkr, MaceEldner, SpearSplitner, SwordNiedhogg, BowAshlands, CrossbowRipper) and the Ashlands staves |
| Standard TwoSided | 17 | item 11, vfx 7 | arrow fletching (cutoff 0.49), Dvergr banners, fireplace ash |
| **Custom/Rug** | 14 | piece 14 | rugs and floor cloth: cutout, fades into the floor (`_ZFadeDistance` 0.2 to 0.3) |
| Custom/Gradient Mapped Particle (Unlit) | 12 | vfx 9 | flames and candle flames coloured through a gradient |
| Custom/FlowOpaque | 11 | item 5, creature 4 | lava, ooze, Bonemass tears: scrolling texture and foam |
| Custom/Bonemass | 10 | creature 6 | Bonemass, its creep and mud: a flow layer over the albedo |
| **Custom/Player** | 9 (of 50) | player, armour stand | the player body: skin, chest and legs textures in one material |
| Custom/Blob, Custom/Grass, Custom/Decal, Custom/Trilinearmap, Custom/ShadowBlob, Valheim/Snow Mesh, Custom/Water | 8, 8, 7, 7, 6, 6, 5 | | slimes; crop and clutter cards; ground decals; rock insides; water rings and shadows; snow caps; small water surfaces |
| Standard (Specular setup), Autodesk Interactive, legacy particles (fileID 200, 203) | 4, 4, 8, 7 | | a coal lump, potions, old flame and glow particles |

58 shader dummies are exported; 53 are used by some material. `data/shaders.json` lists every one with its full
property block, texture slots filled, keywords and value ranges.

## Custom/Creature: items and creatures

The shader the workshop dresses most new assets in. Properties (label in the dummy, then what the materials do with
it; numbers over the 336 drawn materials):

| Property | Label | Measured use |
| --- | --- | --- |
| `_MainTex`, `_Color` | Albedo, Tint | albedo on 332; 44 set a tint other than white (greyscale bodies coloured by it: Skeleton_Poison (0.61, 1, 0.24), Fenring and Ulv 0.68 grey) |
| `_Hue`, `_Saturation`, `_Value` | Color Adjustment, -0.5..0.5, -1..1, -1..1 | 0 on all but 24 materials; set per star level by `LevelEffects` (`recolour.md`) |
| `_EmissionMap`, `_EmissionColor` (HDR) | Glow, Emissive color | glow map on 89; colours up to 28 (BonemawSerpent (0, 28.5, 10.2)), eyes and runes |
| `_MetallicGlossMap` | Metal Mask | on 185: red = metal (hard 0 or 1), alpha = smoothness when `_UseGlossmap` is on (37) |
| `_Metallic`, `_MetalColor` (HDR) | Metallic, Color | 158 metallic (mask texels at 1); `_MetalColor` white on 145 of them |
| `_Glossiness` | Base Smoothness | median 0.20 (10-90 %: 0.08 to 0.57): the non-metal parts are matte |
| `_MetalGloss` | Metal Smoothness | median 0.66 (0.40 to 0.83) on metallic materials, 0 elsewhere; metal weapons 0.75 |
| `_BumpMap`, `_BumpScale` | Normal Map, Normal strength (0..10) | normal map on 330; strength 1.0 on 158 of 158 metallic and most others (0.63 to 2.79) |
| `_TwoSidedNormals`, `_Cull` | Double-Sided, Culling Off/Back | cull off on 145 (capes, fur cards, leaves on creatures), double-sided normals on 42 |
| `_Cutoff` (+ `_ALPHATEST_ON`) | Alpha Cutoff | cutout on 55, cutoff median 0.5 (0.12 to 0.74): hair, fur, rags, feathers |
| `_AddRain`, `_SnowCover` | Add Rain, Snow Coverage | rain on 41; snow set at run time by `VisEquipment` |
| `_UseStyles`, `_StyleTex`, `_Style` | Styles | 12 materials, the variant shields and capes (`recolour.md`) |
| `NOISEGLOW`, `_NoiseGlowTex`... | Noise Glow | one material (the Fader) |
| `_SnapNoiseToPixel` | Snap to Pixel | 1 on all 336: the shader's noise snaps to texels, keeping the point-filtered look |

On creatures alone (the creatures agent, `data/creatures.json`): `_Glossiness` median 0.19 (0.12 to 0.30), `_Metallic`
0 on 64 of 87 materials, `_BumpScale` 1, `_Cull` off on 60 of the 102 that set it, `_ALPHATEST_ON` on 32 (cutoff about
0.5).

## Custom/Piece: building pieces

340 of 388 hammer pieces draw with it (the pieces agent, `data/pieces.json` key `piece_shader`).

| Property | Measured use |
| --- | --- |
| `_MainTex`, `_Color` | albedo on 329; tint on 97. Pieces are darkened by tint: stone, logs, thatch and grausten 0.79 grey; workbench, forge and iron cage about 0.62; beds and tables (0.80, 0.78, 0.64) |
| `_NoiseTex`, `_ValueNoise`, `_ValueNoiseVertex` | a 64 px uniform RGB noise (`3rd party/SunShafts/.../Noise.png`) on 321 materials; strength 1.0 on 150, 0.5 on 105, 0 on 71; per vertex on 160. It varies the value of each piece in the world so identical pieces do not repeat |
| `_RippleDistance`, `_RippleFreq` | vertex wobble 0 to 0.24 m; the placement ghost and destruction fragments set it to 0 |
| `_TriplanarMap`, `_TriplanarScale`, `_TriplanarLocalPos` | only black marble (scale 0.14, world space; the ghost switches to local space) |
| `_MetallicTex`, `_Metallic`, `_MetallicAlphaGloss`, `_MetalColor` | metal mask on 119, metallic 136; metal smoothness median 0.63 (0.32 to 0.80) |
| `_Glossiness` | median 0.19 on metal pieces, 0.18 elsewhere (0 to 0.33) |
| `_BumpScale` | 1.0; worn copies 1.5 to 2 |
| `_EmissionMap`, `_EmissionColor` | glow on 28 (eternal pyre (1, 0.53, 0), shield core (4.9, 0, 1.7)) |
| `_Cull`, `_TwoSidedNormals`, `_Cutoff` | cull off on 27 (cards, shingles), cutout on 13 |
| `_AddRain`, `_AddSnow` | rain on 338 of 340 hammer pieces, snow on 332 |
| `_MoveableObject` | 20 (carts, ships' moving parts) |

Worn copies of a piece's material (`*_worn`) raise `_BumpScale` 1.5 to 2 times and `_ValueNoise` by 0.25 to 0.5, lower
`_Glossiness` and double `_RippleDistance`, and use a `_worn_d` texture (greener, less saturated: `palette.md`). The
build highlight writes a colour into `_Color` and 0.4 times it into `_EmissionColor`; an invalid placement writes red
and
red times 0.7.

## Custom/StaticRock, Custom/Vegetation, Custom/Grass: the world

- **StaticRock** (158): `_MainTex`, `_BumpMap` (strength 0.15 to 2.4), `_MetalTex` for ore veins (27 metallic,
  metal smoothness 0.71), `_EmissiveTex` on 32 (runestones, unstable rock), `_Glossiness` median 0.23. `_MossTex` on 116
  is the biome's ground laid over surfaces facing up (`forest_d`, `stonemoss_heath`, `pillar_snow_d2`, `AshOnRocks_d`):
  a rock's top cover comes from the shader, never from its albedo, and the biome variants of a rock are the same mesh
  with only the moss texture swapped (Rock_4, Rock_4_plains, Rock_4_deepnorth). Two settings recur: `_MossAlpha` 1 with
  `_MossBlend` 0, or `_MossAlpha` 0 with `_MossBlend` 5 to 10. `_TriplanarMap` is for fracture insides; `_FRESNEL` edge
  glow for ice. Tinted: 85 of 156 (Morkhalla rock slabs (0.63, 0.63, 0.67)).
- **Vegetation** (150): albedo with cutout (`_Cutoff` 0.5, 47 cutout), cull off on 133, `_TwoSidedNormals` on 64,
  `_Glossiness` 0.1. Wind: `_SwaySpeed`, `_SwayDistance` and `_Height` are shared by the bark and leaves of one tree
  (beech 10 / 25 / 35, pine 15 / 120 / 100, bush 30 / 3 / 3); `_RippleSpeed` and `_RippleDistance` are leaf flutter (100
  to 200, 0.5 to 3); `_PushDistance` bends bushes away from the player (2 to 3 m). `_AddSnow`, `_AddRain` 1. Banners,
  sails and tents are Vegetation too (wind, cull off, cutout 0.5).
- **Grass** (8 drawn, the clutter and crops): `_TerrainColorTex` with `_TerrainColorScale` 0.01: the Meadows grass is
  white in its own texture and coloured by `grass_terrain_color.png`, repeating every 100 m; fades between
  `_FadeDistanceMin` and `_FadeDistanceMax` (15 to 20, up to 35 m).

The environment agent's numbers (`data/environment.json`) add: world texel density 25 to 45 px/m, big rocks and
Mistlands cliffs tiling the terrain's `gouacherock_big.png` 8 to 10 times, `Trilinearmap` repeating its texture
`_UVScale` times a metre (0.4 to 1.0).

## Custom/Player and Custom/Fallen Warrior: the player's body

One material draws the whole body: `_MainTex` (skin), `_SkinBumpMap`, `_SkinColor` (set by `VisEquipment` from the
character's skin colour; the hair uses the same property on its own material through a property block),
`_ChestTex`/`_ChestBumpMap`/`_ChestMetal` and `_LegsTex`/`_LegsBumpMap`/`_LegsMetal` (an armour item swaps its own
textures in: they use the top-left 128 px of a 256 px layout, and their alpha decides where armour covers skin, see
`models/armour.md`), `_Glossiness` 0.16, `_MetalGlossiness` 0.5, `_BumpScale` 0.53, `_SnowCover`. Fallen Warrior is the
same layout with `_TintColor`.

## Unlit/WeaponGlow

A second mesh over a glowing weapon: `_MainTex`, a curl-noise texture (`_CurlTex`, `_CurlSize` 2 to 4.9,
`_CurlStrength` 0.14 to 0.17) scrolling upwards (`_ScrollSpeed` 0.08 to 0.13), `_PixelSize` 32 to 128 (the glow is
snapped to that many pixels, so it stays blocky), `_VertPush` 0.008 m outwards.

## Standard and the untextured materials

Unity's Standard shader draws 560 materials, 85 of them metallic with smoothness median 0.65 (0.35 to 0.89), 193 with
emission (eyes, lanterns, glowing mushrooms), 30 cutout and 30 alpha-blended (gems, glass). It is the shader of the
game's flat-colour materials: ingots (tin (0.94 grey), copper (1, 0.66, 0.46), bronze (1, 0.76, 0.46), silver bar white:
all metallic 1, smoothness 0.8, a borrowed rock normal map), crystals (`crystal_exterior`: tint (0.77, 0.61, 1)
alpha 0.35, emission (0.34, 0.55, 1.04), metallic 1), eyes (`eye_red`: (1, 0, 0.19) with emission (2.96, 0.3, 0.3)), tar
(black, smoothness 0.83), fishing baits. `paint.md` lists them.

## Which shader a new asset wears

Nothing of the game's is in a bundle: `BundlePrefabs.GameMaterials` gives a bundled model the game's shader at run
time. `Borrow(prefab)` copies the first material of a game prefab; `Dress(game, placeholder)` copies a game material
and puts the placeholder's baked albedo and normal map into `_MainTex` and `_BumpMap` (white tint, tiling reset);
`Plain(material, gloss)` then clears the maps laid out for the game model's UVs (`_MetallicGlossMap`, `_EmissionMap`,
`_StyleTex`), sets `_Metallic` and `_MetalGloss` to 0, `_Glossiness` to the given gloss and switches emission off.

| New asset | Dress in (borrow from) | Then | Gloss |
| --- | --- | --- | --- |
| weapon, tool, shield, armour piece, trinket | Custom/Creature: a game item of the same kind (Battleaxe, ShieldWood) | `Plain` | 0.19 to 0.20 (the game's base smoothness) |
| creature body, creature gear | Custom/Creature: the creature it is built on, or a creature of the same build | `Plain`; cutout fur and rags need `_Cutoff` 0.5 and cull off | 0.19 |
| building piece, furniture, station, chest | Custom/Piece: a piece of the same material (`woodwall`, `stone_mat`) | clear `_MetallicTex` and `_EmissionMap` (see below); keep `_NoiseTex` and `_ValueNoise`, rain and snow | 0.18 |
| rock, cliff, ore, runestone, world prop | Custom/StaticRock: a rock of the same biome (its `_MossTex` is the biome's ground) | clear `_MetalTex`, `_EmissiveTex` | 0.2 |
| tree, bush, crop, banner, sail, hanging cloth | Custom/Vegetation, cutout 0.5, cull off | keep the tree's wind values for trees; banners as `Banner_*` | 0.1 |
| rug, floor cloth | Custom/Rug | | 0.1 |
| food, gem, ingot, potion | Standard, as the game does (or Creature) | ingots: metallic 1, smoothness 0.8, no albedo, a tint | 0.3 to 0.8 |
| glowing overlay | Unlit/WeaponGlow from a glowing weapon | | |

- **What `Plain` loses.** The game's metal gear reads as metal through its metal mask (red 1 on metal texels,
  `_Metallic` 1, `_MetalGloss` about 0.66 to 0.75); a dressed-and-plain workshop item is all matte, its metal only
  painted grey. A metal mask baked from the asset's `metal` paint region would bring that back (the workshop does not
  bake one yet).
- **Custom/Piece's maps are named differently.** `Plain` clears `_MetallicGlossMap` and `_EmissionMap`; a Piece material
  keeps its metal mask in `_MetallicTex` and StaticRock in `_MetalTex` and `_EmissiveTex`, which `Plain` leaves in
  place, laid out for the game piece's UVs. Clear them too when dressing in those shaders.
- **Keep what the world does to it.** Piece's value noise, rain and snow, StaticRock's moss and snow and Vegetation's
  wind are why a piece or plant sits in the world like the game's: dress, do not replace the shader.

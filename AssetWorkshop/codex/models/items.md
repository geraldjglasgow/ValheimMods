# Items

Everything that is not a weapon, tool, shield or armour: crafting materials, trophies, food, meads, feasts, gems,
coins and valuables, keys, fish, bait and the rest. Measured on 2026-09-29 by `measure/items.py`: `data/items.json`
categories `item.*` (materials also carry a `family`: metal, wood, hide, bone, stone, plant, thread, other). Renders:
`codex/out/items/sheets/item.*.png` (three-quarter views, each framed to fit, with its size written under it); paint:
`out/items/paint/item.*.png`; icons: `out/items/icons/item.*.png`.

## 1. Dropped items are drawn big

An item on the ground is one object standing for the whole stack, and the game draws it large so it reads from a
distance: a log of Wood is 2.07 m long, a DeerHide 2.45 m, a TrollHide 2.13 m, an arrow 1.76 m, a Flint 0.65 m, a
Stone 0.50 m, a bar of Iron 0.62 m. Build a new item to its kind's size here, not to its real size.

| Kind (key) | n | Longest side, min / p25 / median / p75 / max | Triangles | Texture | px per metre | Game example |
| --- | --- | --- | --- | --- | --- | --- |
| crafting material (`item.material`) | 253 | 0.10 / 0.59 / 0.81 / 1.55 / 4.09 m | 12 to 14,222 (144) | 32 to 1,024 (128) | 20 to 1,477 (49) | Wood 98 tris, 64 px, 2.07 m |
| trophy (`item.trophy`) | 70 | 0.24 / 0.59 / 1.12 / 2.02 / 5.87 m | 48 to 15,717 (1,093) | 32 to 1,024 (256) | 8 to 308 (57) | TrophyBoar 0.66 m; TrophyDeer 1,258 tris, 1.13 m |
| food, raw and cooked (`item.food`) | 120 | 0.22 / 0.47 / 0.57 / 0.73 / 2.75 m | 24 to 2,298 (282) | 32 to 1,024 (64) | 23 to 256 (53) | CookedMeat 0.43 m; Bread; LoxPie 322 tris, 64 px, 0.57 m |
| mead and mead base (`item.mead`) | 21 | 0.28 / 0.37 / 0.40 / 0.45 / 0.50 m | 208 to 2,382 (456) | 32 to 256 (128) | 49 to 124 (90) | MeadHealthMinor 320 tris, 0.28 m |
| feast (`item.feast`) | 9 | 0.73 / 1.64 / 2.19 / 2.32 / 3.49 m | 330 to 8,700 (1,452) | 32 to 128 (128) | 29 to 80 (40) | FeastMeadows 648 tris |
| gem (`item.gem`) | 11 | 0.15 / 0.24 / 0.36 / 0.48 / 0.48 m | 22 to 768 (48) | none or 32 to 64 | | Ruby 64 tris, flat colour, 0.36 m |
| coins, valuables (`item.valuable`) | 3 | 0.36 to 0.71 m | 52 to 624 | none | | Coins, SilverNecklace |
| key (`item.key`) | 8 | 0.41 / 0.51 / 0.55 / 0.69 / 0.83 m | 56 to 370 (302) | 32 to 64 (64) | 27 to 78 (33) | CryptKey, DvergrKey |
| fish (`item.fish`) | 12 | 0.64 / 0.91 / 1.09 / 1.49 / 1.90 m | 136 to 610 (200) | 32 to 128 (64) | 29 to 77 (50) | Fish1 136 tris |
| bait (`item.bait`) | 9 | 0.54 m (one mesh, nine colours) | 264 | none | | FishingBait |
| other (`item.misc`) | 38 | 0.20 / 0.42 / 0.57 / 1.24 / 1.85 m | 112 to 1,868 (580) | 64 to 256 (128) | 23 to 174 (111) | saddles, upgrade items, seeds, the dragon egg |

Crafting materials by family (median triangles, texture, longest side; from `samples[].family`):

| Family | n | Triangles (min, median, max) | Texture | Longest side (min, median, max) | Examples |
| --- | --- | --- | --- | --- | --- |
| metal: bars, ores, scrap, nails, moulds | 53 | 44, 228, 1,764 | 128 | 0.61, 0.78, 2.79 m | every bar is one 44-triangle ingot, 0.30 x 0.12 x 0.62 m (Bronze, Copper, Iron, BlackMetal, Flametal, Gold); ores 64 to 1,380; scrap 176 |
| wood | 6 | 64, 107, 536 | 96 | 1.70, 2.08, 3.04 m | Wood, FineWood, RoundLog, YggdrasilWood, ElderBark |
| hide | 12 | 32, 109, 436 | 128 | 0.75, 1.88, 4.09 m | DeerHide, TrollHide (a flat pelt 2.13 m across), LoxPelt |
| bone, tooth, shell | 17 | 12, 129, 2,204 | 256 | 0.20, 0.84, 1.98 m | BoneFragments, WolfFang, Chitin, HardAntler |
| stone | 11 | 38, 56, 192 | 64 | 0.53, 0.63, 1.02 m | Stone, Flint, Obsidian, Coal |
| plant, seed, resin | 22 | 12, 285, 1,380 | 64 | 0.16, 0.48, 1.80 m | seeds, Barley, Flax, Resin, Thistle |
| thread, sinew, bundle | 7 | 88, 376, 928 | 128 | 0.38, 0.63, 0.99 m | LinenThread, NornThread |

## 2. Frames an item carries

- **`attach`** (on 413 of the 578 items in these categories, variants included): the model the hand holds when the item
  is used (eating, drinking, throwing), the item stand shows, and the cooking station shows (it looks for `attach_cook`,
  then `attach`). Same frame as weapons (`weapons.md`, section 1): the item's grip at the origin. Food sits roughly
  centred on it (the fist at 0.47 of a food's length, 0.23 m to +Z and 0.27 m to -Z at the median); a bottle spans z
  -0.14 to +0.11 m.
- **`equipoffset`** (82 foods, 22 meads): a transform whose position and rotation `VisEquipment.AttachItem` adds to the
  held copy, to bring a dish or bottle to the mouth.
- Items without `attach` (materials that are only carried, arrows) are just the dropped model under the root.

## 3. What they look like

From the sheets (`sheets/item.*.png`) and texture close-ups (`paint/closeup_armour_things.png`):

- **Materials** are one simple, readable object: an ingot, a log with end-grain rings, a flat pelt, a heap of bone
  shards, a lumpy stone, a coil of root. Ores are a rock with the metal as blotches of colour (CopperOre: tan rock,
  green patina patches, 64 px, 1,380 triangles). Deep North moulds are one grey slab strapped with leather bands and a
  dark silhouette of the part pressed in (228 triangles, 128 px).
- **Trophies are the creature's own head or part on the creature's own texture**: TrophyGreydwarf wears
  `Characters/GreyDwarf/Materials/greydrawrf_diffuse.png`, TrophyDraugr `Draugr_d.png`, TrophySkeleton `Skeleton_d.tga`,
  TrophyBoar the boar's; 59 of 70 are on the `Creature` shader, many with emission (glowing eyes). Big creatures give
  big trophies (TrophySerpent 4.58 m, TrophyBonemass 3.08 m, TrophyDragonQueen 3.08 m).
- **Food** is plated: stews and pies in dark wooden bowls 0.5 to 0.6 m across, roasts and chops with a pale bone
  sticking out, berries as small clusters, jerky as strips. Colours are saturated: median saturation 0.5 to 0.8 on
  most (Sausages 0.77 orange, FishCooked 0.61, Bread 0.54 tan); CookedMeat, painted with a bark texture at 64 px, only
  0.17. Textures are small (32 or 64 px) and often borrowed: raw and cooked lox meat come from a creature pack.
- **Meads** are stoppered bottles 0.28 to 0.5 m tall: a round or square body in the colour of the brew, a pale cork, a
  leather strap or chain, a charm (berries, a bone, a paw print); mead bases are a wooden bowl of liquid (20 share
  `_res/stew/meadbase_d.png`).
- **Gems** are cut stones, most of 22 to 188 triangles, in one flat, saturated colour with no texture at all (Ruby, the
  AncientGemstone set, 5 of 11 carry no maps), or a 32 to 64 px mottled sheet (GemstoneRed, GemstoneBlue, Amber).
  Coins are a heap of discs with a normal map and no albedo: flat gold.
- **Keys** are chunky and oversized: 0.41 to 0.83 m. The Hildir keys are one 302-triangle key (a ring bow with a
  coloured band, a square shank, a two-toothed bit) in three colours; the CryptKey a thin white key with a cyan gem;
  the BloodGoldKey an orange openwork bow.
- **Fish** are low (136 to 610 triangles), 0.6 to 1.9 m, on the Standard shader with albedo and normal only.

## 4. How they are painted

Numbers over the uv-covered pixels of the references (`categories[item.*]["paint"]`):

| Item | Texture | Luma p5 / p50 / p95 | Saturation | Colours at 16 levels | Neighbour step |
| --- | --- | --- | --- | --- | --- |
| Wood | 64 px | 97 / 136 / 157 | 0.36 | 61 | 9.8 |
| Stone | 32 px | 103 / 124 / 151 | 0.10 | 24 | 8.4 |
| Flint | 64 px | 98 / 124 / 155 | 0.11 | 35 | 7.6 |
| CopperOre | 64 px | 124 / 134 / 141 | 0.42 | 51 | 1.9 |
| DeerHide | 64 px | 51 / 118 / 142 | 0.75 | 61 | 6.3 |
| TrollHide | 64 px | 67 / 90 / 160 | 0.83 | 70 | 3.5 |
| CookedMeat | 64 px | 75 / 133 / 163 | 0.17 | 43 | 17.1 |
| Bread | 32 px | 97 / 116 / 145 | 0.54 | 17 | 5.2 |
| Sausages | 128 px | 0 / 130 / 152 | 0.77 | 39 | 12.1 |

- Mid tones: medians 90 to 136 of 255; few near-blacks, no pure whites.
- Stone and flint are nearly grey (saturation 0.1); hides, food and berries are strongly coloured (0.5 to 0.8).
- 17 to 70 colours: flat regions with soft blotches; bark and hide show a few 1 px streaks.
- Normal maps on 9 of 10 materials, shallow (mean tilt 4 to 14 degrees on the references).

## 5. Shaders and materials

Unity's Standard shader (`builtin_46`) on 155 of 253 materials and 101 of 120 foods; the game's
`Creature` on 58 materials and 59 trophies; `Piece` on 18 materials (things that are also building parts); a few on
`StaticRock`, `Vegetation`, `Bonemass`, `FlowOpaque` (liquids). Standard also carries all gems and fish and 5 of 8
keys. Maps: albedo and normal on most; an emission map on 110 materials.

## 6. Effects and sounds

- **Food**: `equipEffect` is the eating: `sfx_eat` (84 of 98 designs) and `fx_Eat` (50) or a tinted `fx_Eat_Green`
  (15), `fx_Eat_Orange` (7), `fx_Eat_Blue` (5), `fx_Eat_Red`, `fx_Eat_Black`, `fx_Eat_Yellow`: pick the colour of the
  food.
- **Meads**: `startEffect` `vfx_MeadSplash` and `sfx_MeadBurp` (the drinking); a few use `sfx_drink`.
- **On the ground**: most items have a particle system on the root on `item_particle` (the dropped sparkle); 88 foods
  instead have an `fx_ItemSparkles` child, and 47 cooked foods also steam (`fx_FoodSteam_Small`, 4 with the larger
  `fx_FoodSteam`).
- Trophies, gems, keys and valuables have no effects of their own.

## 7. The dropped item

- Root: `ZNetView`, `ZSyncTransform`, `ItemDrop`, `Rigidbody` (mass 1; a few materials 1.5 to 20), and on 25
  materials `Floating` (it floats: wood, hides, feathers ...).
- Colliders: a `BoxCollider` round the model on almost everything (materials: 237 box, 26 convex mesh, 6 sphere, 4
  capsule; trophies: 51 box, 28 convex mesh).
- 88 foods and 22 meads also carry `Piece` (with `WearNTear`) on the root: they are building pieces as well, so the
  game can place them in the world as objects.

## 8. Checklist

- Size the dropped model to its kind (section 1): a material 0.5 to 2 m, food about 0.55 m, a bottle about 0.4 m, a
  trophy 0.6 to 2 m.
- 50 to 300 triangles for a material or food, 300 to 1,100 for a trophy; 64 px (128 for trophies and meads).
- One simple readable object; saturated colours for food, grey for stone, the creature's own texture for a trophy.
- `attach` (and `equipoffset` for food) if it is ever held; the root `Rigidbody` and a `BoxCollider`;
  `item_particle` or `fx_ItemSparkles`; `fx_Eat_<colour>` and `sfx_eat` for food.

## References

- Materials: `GameElements/Items/materials/Wood.prefab`, `Stone.prefab`, `Flint.prefab`, `CopperOre.prefab`,
  `Bronze.prefab`, `LeatherScraps.prefab`, `DeerHide.prefab`, `TrollHide.prefab`
- Trophies: `GameElements/Items/trophies/TrophyBoar.prefab`, `TrophyDeer.prefab`, `TrophyGreydwarf.prefab`,
  `TrophySkeleton.prefab`, `TrophyDraugr.prefab`, `TrophyWolf.prefab`
- Food and drink: `GameElements/Items/consumables/CookedMeat.prefab`, `Bread.prefab`, `Sausages.prefab`,
  `LoxPie.prefab`, `FishCooked.prefab`, `Raspberry.prefab`; `MeadHealthMinor.prefab`, `MeadStaminaMinor.prefab`,
  `MeadPoisonResist.prefab`, `MeadTasty.prefab`; feasts `GameElements/Pieces/FeastBlackforest.prefab`,
  `FeastAshlands.prefab`
- Gems, valuables, keys: `GameElements/Items/valuables/Ruby.prefab`, `Amber.prefab`, `AmberPearl.prefab`,
  `Coins.prefab`, `SilverNecklace.prefab`; `GameElements/Items/materials/GemstoneRed.prefab`, `GemstoneBlue.prefab`;
  `GameElements/Items/misc/CryptKey.prefab`, `DvergrKey.prefab`, `BloodGoldKey.prefab`
- Fish: `Characters/animals/fishes/Fish1.prefab`

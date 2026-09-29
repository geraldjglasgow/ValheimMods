# The look

What makes an asset read as Valheim, across every kind of asset. Read this first, every time; each rule points at the
page that measures it. The numbers are the game's own, measured from its files on 2026-09-29 (`data/*.json`).

## The rules

1. **Few big parts, low poly.** An asset is a handful of chunky, recognisable parts. Anything smaller than two texels
   is paint, not geometry: at the game's densities that is 3 to 6 cm. A 2 m wall is 44 to 450 triangles, a 2.3 m
   boulder 196, a two-handed axe 188 to 3,023 (median 1,080), a player-sized creature 1.5k to 4k.
2. **Texel density is the constant.** Held gear about 55 to 75 px per metre, building pieces 27 to 38, world props
   25 to 45, furniture and stations 42 to 57, creatures 45 to 73. Aim at the category's median, not its maximum.
3. **Small textures, point filtered.** 64 to 128 px for items, 128 px for pieces (usually a shared set atlas),
   128 to 256 px for creatures, 512 px only for bosses. 1,092 of 1,168 albedos are point filtered: the pixels show.
4. **Few close tones, soft blotches, a little pixel noise.** A material's texels sit between two close tones (value
   span median 0.22; planks 0.11). Blotches are 3 to 9 texels across, whatever the asset's size. No fine procedural
   noise, no modelled cracks, no scratches.
5. **The normal map is the albedo's own relief.** Light paint raised, dark paint sunk, 2 to 4 normal units per unit of
   value per texel (5 to 7 for bone, bark and stone). Normal maps are shallow: about 6 degrees of tilt on items.
6. **Muted colour.** Metals are neutral grey (saturation 0.02), stone 0.04, fur 0.17, bone 0.38, wood about 0.5,
   leather 0.64; only dyes, leather and bronze are saturated. Whatever a tint, star level or variant will colour is
   painted grey (the Wolf is painted at saturation 0.02).
7. **Light is painted on creatures, not on pieces.** Creature bodies are lighter on top (the Troll 0.54 against 0.38
   underneath); building pieces and world props are not (planks 0.44 against 0.42). Everywhere, hollows are painted
   dark, edges one to three texels lighter, gaps near black.
8. **Exaggerate one or two proportions.** Creatures push one or two ratios far from the player's (Greydwarf arms 0.48
   of its height against the player's 0.30; Troll, Stone Golem and Bonemass shoulders 0.42 to 0.65, short legs, huge
   palms). Timber is oversized (a wall plank 0.5 x 0.3 m, a beam 0.4 m square). Dropped items are drawn 1.5 to 3 times
   real size (a wood log 2.07 m).
9. **Wear the game's shaders and reuse the game's sets.** An asset is dressed at runtime in the game material its kind
   uses (below), a building piece borrows its set's material instead of being repainted, effects and sounds reuse the
   game's own where one fits. A plain Unity material loses rain, snow, moss, wind, star tints and the build highlight.
10. **Judge it beside the game's own, in the game's light.** Never alone: in a lineup with the category's references,
    point filtered, then in game under its biome's light (Meadows sun #ffc57b at 1.7; Swamp 0.6 with olive ambient
    and fog 0.02; Mistlands fog #37434c; Deep North 0.5 to 0.9 and cold). See "Checks".

## Budgets at a glance

| Kind | Triangles | Texture | px per metre | Game example | Page |
| --- | --- | --- | --- | --- | --- |
| weapon | 300 to 1,000 median, growing with tier (Meadows 264, Deep North 1,904) | 64 to 128 | 55 to 75 | Battleaxe 578 / 64 px | `models/weapons.md` |
| shield | 108 to 2,288 (median 871) | 128 | 50 to 102 (median 57) | ShieldBanded | `models/shields.md` |
| armour | chest and legs painted into the body layout (the top-left 128 px of 256); worn meshes about 770 | 256 | 63 on the body | ArmorIronChest | `models/armour.md` |
| building piece | 2 m wall 264 median (44 to 1,320), roof slope 238 to 740 | 128 set atlas | 27 to 38 | woodwall | `models/pieces.md` |
| furniture, station | a few hundred to a few thousand | 128 to 256 | 42 to 57 | piece_workbench | `models/furniture.md`, `stations.md` |
| rock | 196 (2.3 m) to 272 (28 m) | tiled terrain texture | 25 to 45 | Rock_4 | `models/environment.md` |
| tree | deciduous 344 to 578, conifers about 2,000 to 2,500; about 100 cards of 4.7 to 6.8 m | 256 px leaves | 25 to 45 | Beech1, Pinetree_01 | `models/vegetation.md` |
| grass | 36-triangle crossed cards | 32 to 128 | | | `models/vegetation.md` |
| creature, player-sized | 1.5k to 4k (median 2.9k) | 128 to 256 | 54 to 73 | Skeleton 4,077 / 128 px | `models/creatures.md` |
| creature, large / boss | about 6.7k / 10k to 22k | 256 / 512 | about 39 | Troll, Eikthyr | `models/creatures.md` |
| inventory icon | a 64 px render of the model, transparent, no outline or shadow, filling about 90 % | 64 | | | `models/icons.md` |
| build-menu icon | 64 px, transparent, the long side about 88 % of the square, one scale per family | 64 | | | `models/pieces.md` |
| effect | 2 to 4 systems, bursts of 10 to 100, particles live 1 to 3 s, removed after 3 to 10 s | 8 to 128, flipbooks 32 px frames | | vfx_HitSparks | `vfx/archetypes.md` |

## Shapes

- **Items** are built in the attach frame: the fist at the origin, a blade out of the thumb side (+Z), an axe's edge
  towards the knuckles (+X), the left hand mirrored in Y; the item keeps its `attach` node's scale
  (`models/weapons.md`). Tiers add modelled ornament, never finer paint.
- **Building pieces** snap on a 1 m grid with a 2 m module; edges get one chamfer of 2 to 3 % of the section; gaps
  between planks are geometry; meshes overshoot their snap box by 1 to 10 %. Damage is three children, `New`, `Worn`
  and `Broken`, and destruction throws 12 to 30 pre-cut chunks (`models/pieces.md`).
- **World props** are few big facets with no bevels (0.3 to 0.5 m on boulders, 2 to 3 m on big rocks), mostly buried,
  the pivot on the ground line. Moss, snow and ash on top come from the shader, never the texture. Trees are about 100
  huge cut-out branch cards round a low-sided trunk, with no billboard (`models/environment.md`, `vegetation.md`).
- **Creatures** keep one LOD, roughly one material, hair, fur and rags as alpha-cut cards, and the names the game's code
  looks for (`Visual`, `Head`, the `attack` tag, a `footstep` curve: `rigs/README.md`).

## Paint and colour

`paint.md` has the ten texture rules in full, `palette.md` the tones per material family and biome, and
`blender/workshop/paint.py` the recipes that reproduce them (`paint.make("wood.planks", ...)`, `paint.iron(...)`).
With the recipes, set `AO_STRENGTH = 0.2` (they paint their own hollows; the default 0.6 makes bone twice as dark as
the game's) and `NORMAL_FROM_ALBEDO` to the family's strength. Every recipe sets the material's paint region, so the
asset recolours by region (`recolour.md`).

Metal is grey paint made metal by a mask (red 1 on metal texels, smoothness about 0.66). The workshop's bake does not
write a metal mask yet, so a dressed workshop item renders its metal matte.

## Materials the game draws each kind with

| Kind | Game shader | How a mod gets it | Page |
| --- | --- | --- | --- |
| weapons, tools, shields, trophies | `Custom/Creature` (metal mask, gloss 0.19, metal gloss 0.75) | `GameMaterials.Dress` on a game item's material | `shaders.md` |
| food, gems, fish | Unity Standard | as built | `models/items.md` |
| armour on the body | `Custom/Player` (`_ChestTex`, `_LegsTex` in the body layout) | the item's armour textures | `models/armour.md` |
| building pieces | `Custom/Piece` (value noise, rain, snow, build highlight) | borrow the set's material | `models/pieces.md` |
| creatures | `Custom/Creature` (star tints on material 0 of the main renderer) | `GameMaterials.Dress`, `CreatureBody.Wear` | `models/creatures.md` |
| rocks | `StaticRock` (`_MossTex` is the biome's ground) | borrow a rock's material | `models/environment.md` |
| trees, bushes, banners | `Vegetation` (sway, flutter, push, cutout 0.5) | borrow | `models/vegetation.md` |
| effects | gradient-mapped or unlit additive for hot things, lit alpha for smoke and dust | `BundleEffects` | `vfx/shaders.md` |

## Rigs and motion

Three routes (`rigs/README.md`): a game creature with new parts or clips; a new body on an exact copy of a game
skeleton, so the game's controller and clips play it unchanged (`blender/workshop/gamerig.py`, the Mossback); or a rig
of its own (a humanoid avatar retargets every humanoid clip; generic rigs need their own clips). The game's code finds
`Visual`, `Head`, attack origins and attach points by exact name, drives logic by state tags (`attack`, `stagger`,
`freeze`) and plays footsteps from a `footstep` float curve. Attacks last 1.7 to 3.5 s with the hit at about 43 %.

## Effects

`vfx/README.md` has the ten rules. In short: colour comes from the particle system, the texture is a white or grey
shape; hot things are additive and unlit, smoke and dust are lit and alpha-blended, bits are flat lit squares; small
point-filtered textures (the most used is an 8 px blob); fade in fast and out slowly; the flash is a point light, not
a sprite. Reuse a game effect, recoloured (`vfx/catalogue.md`, `EffectTint`), before building one.

## Sounds

`sfx/README.md` has the rules, `sfx/mixing.md` the levels, `sfx/catalogue.md` the game sounds to reuse. In short:
a game sound is a networked prefab (ZSFX, AudioSource, TimedDestruction, ZNetView) registered on every peer; a
variant plays the same clips at another ZSFX pitch (the Greyling is the Greydwarf at 1.8 to 2.2); clips are dry
because the mixer adds the room (a 0.5 s reverb, a 3.5 s hall for big creatures); three to five variations with about
1.8 semitones of pitch spread; 44.1 kHz mono Vorbis. The mix puts threats loud and the player's own actions quiet:
creature death and attack about -13 LUFS played, alert -16, hurt -17, idle -20, creature steps -28, the player's own
steps -31 to -42, fire loops -32, ambience -35. Only metal rings; swings are dark whooshes 3 to 15 dB under the hit;
creature voices get darker with size (tiny 1.5 to 10 kHz centroid, bosses 70 to 600 Hz). Every shipped sound is a
real recording: the game's own clips at runtime (another pitch, trimmed, filtered, chosen for the creature's size:
troll-scale clips are far too bassy for a skeleton), CC0, or the user's own. Synthesised sounds were judged "cartoon
noises" by the user and are placeholders only. Cut ringing tones where no metal is involved.

## Checks

| Check | Command | Reads |
| --- | --- | --- |
| style check | `.\build.ps1 -Asset <name>` with `CATEGORY` set | `out/style_report.txt`: every line PASS or a reason in the brief |
| lineup | `.\build.ps1 -Asset <name> -Lineup` | `out/lineup/*.png`: the asset among the category's references |
| paint recipes | `python codex/tools/paint_check.py` | `codex/out/paint/check/` |
| recolour | `python codex/tools/recolour.py assets/<name>` | `out/variants.png` |
| creature on a game rig | `assets/<name>/build.ps1 -Preview` (route b) | `out/unity/sheet_pair.png`, the report |
| effect | `python vfx/build.py <effect> --preview` | `out/style_report.txt`, `out/sheet.png`, the MP4 |
| sound | `out/venv/Scripts/python sfx/build.py <set>` | `sfx/out/<set>/report.md` and the `*_compare.png` spectrograms beside the game's |
| in game | DevBridge's stage: `/bundle`, `/place`, `/lineup`, `/light`, `/frame`, `/screenshot` | `DevBridge/README.md` |

## What made earlier workshop assets look wrong

- 18k to 21k triangles and fine modelled detail (cracks, spirals, thin processes) where the game uses 600 to 4,000
  triangles of chunky parts.
- 1,024 px atlases of fine procedural noise where the game paints 64 to 256 px of soft blotches (the style check's
  blotch-size and local-contrast lines catch this).
- Light from above painted on everything, and the pipeline's default ambient occlusion on top of recipes that already
  paint their hollows.
- Big thin smooth plates, which read as cloth or banners whatever the paint.
- Judging an asset alone instead of beside the game's own.
- Smooth, blobby organic forms: the Mossback proves the rig route but its metaball body is softer than the game's
  faceted creatures.
- Synthesised sounds (damped sines, swept noise): "cartoon noises" to the user; and the game's troll-scale clips on a
  small creature, far too bassy.

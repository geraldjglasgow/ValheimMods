# Paint

How the game's textures are painted, per material family, measured over its own albedos, normal maps, metal masks and
meshes; and the Blender recipes (`blender/workshop/paint.py`) that reproduce it, checked against those numbers.

Measured by `measure/paint.py` (data in `data/paint.json`): 35 material families, 213 samples, each a game texture
(or the part of one that a prefab's meshes, a rectangle or the metal mask picks), always only the texels the game's
UVs cover and always times the material's tint, as the game draws it (linear colour space). Families and samples are
in `measure/paint_families.py` (tables in `paint_families_hard.py` and `paint_families_soft.py`); every number of every
sample is in `data/paint.json` under `families.<family>.samples`.
Sampled colours are in `palette.md`, shaders in `shaders.md`, recolouring in `recolour.md`. Contact sheets and 4x zooms
of every family's textures are written to `codex/out/paint/` by `measure/paint_sheets.py` and `measure/paint_zoom.py`.

## What decides whether a texture looks like the game's

1. **The normal map is the albedo's own relief.** The normal map's slope follows the albedo's value: the correlation
   of the normal's x and y with minus the value's slope is 0.7 to 0.93 (family medians) in 24 of 35 families, and 0.8
   to 0.99 on single textures (Skeleton 0.94 / 0.95, iron armour 0.97 / 0.99, planks 0.78 / 0.87, bark 0.78 / 0.91).
   Light paint is raised, dark paint sunk, at a strength of 2 to 4 normal units per unit of value per texel (5 to 7
   for bone, bark and stone). The game's normal maps were made from its albedos. A new asset gets its normal map the
   same way: `paint_normal.from_albedo` (below).
2. **Few tones, soft blotches, a little pixel noise.** A family's texels sit between two close tones: the value span
   (90th minus 10th percentile of HSV value) is 0.07 (grausten) to 0.58 (gold), median 0.22 over the families and 0.22
   over all 1,070 albedos the game draws; planks 0.11. The variation is spread over every size from one texel to the
   whole texture, with about equal share per octave, and 5 to 40 % of it in single texels (`fine`: planks 0.26, iron
   0.20, silver 0.45, thatch 0.65, teeth 0.05, fine wood 0.05).
3. **Blotches are sized to the texture, not the world.** The value stays alike over 3 to 9 texels (correlation
   length, 25 of 35 families; 12.5 on stone, 25 on dark wood): 0.06 to 0.6 m at the game's densities (0.02 m on rope,
   1.5 m on dark wood). A small item and a big wall show the same number of blotches per texture.
4. **Saturation is low except for dyes and bronze.** Median HSV saturation: metals 0.02 (iron, silver, black metal),
   stone 0.04, fur 0.17, bark 0.27, bone 0.38, wood 0.45 to 0.55, leather 0.64, bronze 0.54, dyed cloth up to 0.77.
5. **Pieces have no light from above; creatures do.** On building pieces and world objects the texels under faces
   looking up are no lighter than those under faces looking down (planks 0.44 against 0.42, stone 0.36 against 0.35,
   correlation with the face's up 0.0 to 0.1). On creature bodies they are: skin 0.44 against 0.37 (the Troll 0.54
   against 0.38, the Greydwarf 0.41 against 0.28, the player 0.75 against 0.62, the Seeker 0.38 against 0.30).
6. **Many textures are grey and coloured by the material.** 409 of 1,676 drawn materials multiply their texture by a
   tint (stone 0.79, the Wolf painted at saturation 0.02 and tinted per variant); 140 have no texture at all (ingots,
   crystals, gems, eyes: a colour, metal and gloss). Paint grey what a tint or variant will colour (`recolour.md`).
7. **Point filtered, mipmapped, compressed, small.** 1,092 of 1,168 albedos are point filtered, 1,141 mipmapped,
   1,148 DXT1, DXT5 or BC7; items 128 px median at 57 px/m, pieces 128 px at 38 px/m, creatures 256 px at 45 px/m.
8. **Metal is painted grey and made metal by a mask.** Iron, silver and black metal albedos are neutral grey (value
   0.13 to 0.56); the metal mask (red 1, alpha 1) makes them metallic at smoothness 0.66; a lighter 1 to 3 px line
   runs along edges, and there is little else (no scratches, no noise beyond the texels').
9. **Hollows are painted dark, edges painted light, gaps near black.** Value against the normal field's curvature
   0.3 to 0.6 in most families: bumps lighter, cracks darker (partly the same fact as 1).
10. **Detail smaller than two texels is paint, never geometry**, and it is sparse: a crack or grain line one texel
    wide, a lighter edge line one to three texels, pits of one to two texels on bone.

## How it was measured

- **Samples.** A family's samples are game textures chosen by looking at them (`measure/paint_families.py`). An atlas
  shared by several materials is narrowed by the UV coverage of one prefab (`HardAntler`, `PickaxeAntler`), a
  rectangle (the SpineSnap bow's bone and grip) or its material's metal mask (`metal` or `nonmetal`). Only covered
  texels count (the meshes' UV triangles are drawn into a mask by `measure/paint_uv.py`), cutout texels (alpha under
  0.5) are left out, and the material's `_Color` multiplies the texture in linear space.
- **Tones.** `value` is the HSV value of the sRGB colour (0 to 1) and `saturation` the HSV saturation; tones are the
  mean linear colour within 5 percentiles of the 10th, 50th and 90th luminance percentile (`palette.md`).
- **Blotches.** `bands`: the share of the value's variance in each octave of feature size (below 2 texels (`fine`),
  2-4, 4-8, 8-16, 16-32, 32-64, 64+), by differences of Gaussian blurs of sigma 1, 2, 4 ... 32 texels.
  `correlation_px`: the lag at which the value's autocorrelation falls to one half. `blotch_m` = 2 x correlation /
  texel density. `local_contrast`: the standard deviation of value minus its 2-texel blur, over the total.
- **Light.** Against the material's normal map: the normal's x and y against the value's slope (normal from albedo),
  the value against the normals' curvature and against the height they integrate to, and a least-squares light
  direction in texture space. Against the mesh (static pieces and world objects, and creatures through their bind
  pose): the median value of texels under faces whose normal points up (y above 0.5), sideways and down.
- **Texel density** of a use: texture pixels x sqrt(UV area / world area) / the mesh's scale in the game (for a
  skinned mesh its first bone's scale in the prefab times its bind pose's).

## Textures: size, density, import

| Kind | Albedo px (p25 / median / p75) | Texel density px/m (p25 / median / p75) | Example |
| --- | --- | --- | --- |
| item | 64 / 128 / 256 | 40 / 57 / 93 | Battleaxe 64 px at 35 px/m; helmet_iron 64 px at 109 px/m |
| piece | 128 / 128 / 256 | 26 / 38 / 57 | woodchest 128 px at 37 px/m; WorkBench 256 px at 40 px/m |
| creature | 128 / 256 / 256 | 30 / 45 / 72 | Skeleton 128 px at 59 px/m; Greydwarf 128 px at 33 px/m |
| env | 64 / 256 / 512 | 7 / 16 / 41 | rock_256 512 px at 46 px/m; big cliffs 5 to 10 px/m tiling |
| location | 128 / 256 / 256 | 23 / 34 / 45 | stone floors 512 px at 32 px/m |

Over distinct mesh uses (item 2,073, piece 5,343, creature 715, env 9,736, location 276). The other agents' finer
figures: held gear 55 to 75 px/m (median 62) on 64 to 128 px, player-sized creatures 54 to 73 px/m on 128 to 256 px,
bosses 512 px, world objects 25 to 45 px/m.

Import settings, from each texture's `.meta` (the runtime texture's own settings):

| Maps | Textures | Filter | Mipmaps | Format | Wrap |
| --- | --- | --- | --- | --- | --- |
| albedo | 1,168 | point 1,092, bilinear 70, trilinear 6 | on 1,141 | DXT5 520, DXT1 380, BC7 248, RGBA32 14 | repeat 1,154 |
| normal | 876 | point 858 | on 862 | DXT5 653 (DXT5nm), BC7 221 | repeat 874 |
| metal mask | 260 | point 133, bilinear 127 | all | DXT1 145, DXT5 71, BC7 44 | repeat |
| emission | 230 | point 161, bilinear 68 | 229 | DXT1 144, DXT5 60, BC7 23 | repeat |

Anisotropic filtering is 1 on 1,163 albedos. `maxTextureSize` is 2048 on 3,178 of 3,181 textures and the compression
fields are the same on all: AssetRipper's defaults, not the game's; the size that counts is the texture's own (the
export keeps the runtime size). Albedos are sRGB, normal maps linear, and the game renders in linear colour
space (`m_ActiveColorSpace` 1). A new texture is point filtered, mipmapped, sRGB for albedo, compressed DXT1 (no
alpha) or DXT5 (alpha), at the kind's size.

## The families

Numbers are medians over the family's samples (n); V = HSV value, S = HSV saturation, hue in degrees, span = V90 - V10,
corr = correlation length in texels, fine = share of variance in single texels, local = local contrast, nfa = the
normal map's correlation with the albedo's slope, k = its strength, tilt = mean tilt of the normals in degrees, gloss =
`_Glossiness`.

| Family | n | px | px/m | V10 / V50 / V90 | S | hue | span | corr | blotch m | fine | local | nfa | k | tilt | gloss |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| wood.planks | 14 | 128 | 37 | 0.35 / 0.44 / 0.52 | 0.55 | 31 | 0.11 | 4.5 | 0.17 | 0.26 | 0.46 | 0.88 | 2.6 | 5 | 0.18 |
| wood.dark | 5 | 128 | 31 | 0.13 / 0.34 / 0.39 | 0.45 | 27 | 0.25 | 25 | 1.47 | 0.04 | 0.18 | 0.86 | 3.2 | 5 | 0.18 |
| wood.logs | 6 | 128 | 53 | 0.34 / 0.45 / 0.57 | 0.48 | 35 | 0.22 | 8 | 0.34 | 0.22 | 0.45 | 0.88 | 3.9 | 8 | 0.20 |
| wood.bark | 8 | 512 | 31 | 0.26 / 0.40 / 0.54 | 0.27 | 21 | 0.21 | 10 | 0.62 | 0.12 | 0.39 | 0.91 | 6.3 | 14 | 0.09 |
| wood.fine | 4 | 192 | 74 | 0.36 / 0.47 / 0.59 | 0.48 | 29 | 0.24 | 16 | 0.86 | 0.05 | 0.24 | 0.86 | 3.7 | 5 | 0.18 |
| metal.iron | 8 | 96 | 52 | 0.34 / 0.43 / 0.65 | 0.02 | - | 0.26 | 3 | 0.12 | 0.20 | 0.49 | 0.93 | 2.3 | 8 | 0.13 |
| metal.bronze | 6 | 96 | 64 | 0.44 / 0.59 / 0.85 | 0.54 | 27 | 0.39 | 3 | 0.12 | 0.26 | 0.54 | 0.47 | 2.5 | 5 | 0.13 |
| metal.copper | 3 | 64 | 15 | 0.34 / 0.37 / 0.45 | 0.42 | 53 | 0.09 | 3 | 0.27 | 0.34 | 0.58 | 0.01 | - | 13 | 0.31 |
| metal.tin (ore) | 1 | 128 | 28 | 0.25 / 0.56 / 0.71 | 0.02 | - | 0.46 | 3 | 0.22 | 0.25 | 0.54 | 0.93 | 4.3 | 35 | 0.19 |
| metal.silver | 5 | 64 | 77 | 0.44 / 0.56 / 0.77 | 0.02 | - | 0.34 | 2 | 0.07 | 0.45 | 0.59 | 0.87 | 2.6 | 10 | 0.29 |
| metal.blackmetal | 6 | 96 | 87 | 0.10 / 0.13 / 0.28 | 0.03 | - | 0.18 | 3.5 | 0.08 | 0.18 | 0.42 | 0.39 | 0.5 | 2 | 0.15 |
| metal.gold | 3 | 128 | 36 | 0.42 / 0.84 / 0.91 | 0.59 | 45 | 0.58 | 2 | 0.28 | 0.25 | 0.54 | 0.77 | 2.8 | 32 | 0.50 |
| metal.flametal | 7 | 128 | 46 | 0.29 / 0.38 / 0.55 | 0.25 | 219 | 0.18 | 5 | 0.27 | 0.22 | 0.42 | 0.69 | 0.5 | 4 | 0.37 |
| bone.bone | 8 | 128 | 44 | 0.31 / 0.54 / 0.71 | 0.38 | 45 | 0.18 | 5.5 | 0.30 | 0.21 | 0.44 | 0.87 | 5.4 | 15 | 0.24 |
| bone.antler | 2 | 192 | 46 | 0.51 / 0.66 / 0.80 | 0.41 | 31 | 0.29 | 3.5 | 0.16 | 0.25 | 0.51 | 0.22 | 1.4 | 16 | 0.17 |
| bone.horn | 2 | 64 | 76 | 0.51 / 0.67 / 0.79 | 0.34 | 30 | 0.28 | 7 | 0.19 | 0.34 | 0.54 | 0.92 | 2.5 | 7 | 0.50 |
| bone.teeth | 3 | 64 | 63 | 0.38 / 0.57 / 0.66 | 0.38 | 47 | 0.32 | 6 | 0.19 | 0.05 | 0.25 | 0.79 | 1.9 | 4 | 0.20 |
| leather.leather | 7 | 64 | 58 | 0.26 / 0.29 / 0.42 | 0.64 | 26 | 0.17 | 8 | 0.19 | 0.16 | 0.38 | 0.01 | - | 3 | 0.11 |
| leather.hide | 8 | 128 | 40 | 0.35 / 0.46 / 0.53 | 0.44 | 33 | 0.18 | 8.5 | 0.44 | 0.11 | 0.29 | 0.67 | 2.4 | 6 | 0.18 |
| leather.fur | 6 | 128 | 37 | 0.27 / 0.39 / 0.51 | 0.17 | 30 | 0.23 | 9 | 0.36 | 0.10 | 0.31 | 0.75 | 2.8 | 5 | 0.12 |
| cloth.linen | 11 | 128 | 37 | 0.33 / 0.43 / 0.67 | 0.55 | 16 | 0.29 | 7 | 0.32 | 0.18 | 0.41 | 0.69 | 2.1 | 10 | 0.10 |
| cloth.rope | 2 | 80 | 128 | 0.48 / 0.60 / 0.68 | 0.11 | 34 | 0.20 | 1 | 0.02 | 0.75 | 0.86 | 0.89 | 2.3 | 11 | 0.17 |
| stone.stone | 12 | 256 | 35 | 0.28 / 0.37 / 0.47 | 0.04 | - | 0.18 | 12.5 | 0.55 | 0.10 | 0.31 | 0.91 | 7.5 | 10 | 0.23 |
| stone.marble | 4 | 128 | 35 | 0.14 / 0.18 / 0.29 | 0.03 | - | 0.15 | 3 | 0.17 | 0.40 | 0.60 | -0.00 | - | 7 | 0.50 |
| stone.grausten | 5 | 128 | 29 | 0.23 / 0.30 / 0.32 | 0.09 | 12 | 0.07 | 3 | 0.21 | 0.42 | 0.61 | 0.75 | 2.8 | 4 | 0.15 |
| thatch.straw | 6 | 96 | 45 | 0.36 / 0.53 / 0.61 | 0.40 | 37 | 0.18 | 1 | 0.06 | 0.65 | 0.77 | 0.71 | 3.3 | 15 | 0.21 |
| crystal.crystal | 3 | 64 | 45 | 0.34 / 0.49 / 0.84 | 0.74 | 335 | 0.44 | 8 | 0.36 | 0.06 | 0.24 | 0.80 | 1.1 | 5 | 0.58 |
| crystal.ice | 6 | 160 | 46 | 0.48 / 0.66 / 0.76 | 0.36 | 186 | 0.20 | 13.5 | 1.08 | 0.12 | 0.34 | 0.81 | 3.3 | 7 | 0.50 |
| crystal.obsidian | 2 | 160 | 72 | 0.09 / 0.22 / 0.29 | 0.03 | - | 0.20 | 3.5 | 0.13 | 0.29 | 0.55 | 0.46 | 1.0 | 11 | 0.25 |
| chitin.chitin | 9 | 128 | 47 | 0.21 / 0.32 / 0.57 | 0.39 | 43 | 0.30 | 5 | 0.26 | 0.13 | 0.41 | 0.83 | 4.1 | 11 | 0.50 |
| skin.skin | 12 | 256 | 47 | 0.27 / 0.41 / 0.56 | 0.42 | 64 | 0.22 | 6.5 | 0.27 | 0.08 | 0.33 | 0.85 | 4.3 | 14 | 0.16 |
| skin.flesh | 9 | 64 | 49 | 0.46 / 0.57 / 0.69 | 0.53 | 10 | 0.17 | 3 | 0.09 | 0.32 | 0.60 | 0.80 | 2.6 | 13 | 0.50 |
| veg.mushroom | 3 | 64 | 39 | 0.57 / 0.84 / 0.93 | 0.26 | 20 | 0.36 | 12 | 0.32 | 0.15 | 0.40 | 0.71 | 3.3 | 6 | 0.22 |
| veg.leaves | 11 | 128 | 37 | 0.33 / 0.45 / 0.61 | 0.47 | 78 | 0.22 | 8 | 0.25 | 0.20 | 0.46 | 0.84 | 3.7 | 13 | 0.12 |
| veg.moss | 6 | 192 | 130 | 0.36 / 0.39 / 0.42 | 0.50 | 66 | 0.07 | 5 | 0.06 | 0.24 | 0.49 | - | - | 5 | 0.30 |

Hues are left out (-) where the family is grey (saturation under 0.05). Where the game's normal map does not follow
the albedo (nfa under 0.5: bronze, copper, black metal, antler, leather, marble, moss) it is a borrowed or generic map
(copper items use `gouacherock_n`, leather helmets a cloth weave), and the family's own paint still follows rule 1 on
most of its textures.

### Wood

- **Planks** (Planks5c_low, woodchest, Bench, Chair, WorkBench, Table, spiral stair, barrel, cart, karve): warm orange
  brown (hue 31, saturation 0.55), a narrow value range (0.35 to 0.52). At 4x: long streaks of grain one to three
  texels wide running the length of each board, alternating a shade lighter and darker; soft washes along a board (one
  end darker); a darker line one texel wide between boards; knots rare, soft rings of four to eight texels. Boards are
  each a slightly different shade. 26 % of the variation is single texels. Worn copies (`*_worn_d`) are greyer and
  greener (Planks5c_worn: moss green).
- **Dark wood** (darkwood beams and gate, ashwood, frostwood): the planks' hue at a third of the value, big smooth
  washes (correlation 25 texels) with faint grain; 4 % in single texels. The darkest 10 % near black (0.13).
- **Logs** (log wall, log bench, drawbridge logs, poles, firewood): orange to brown streaks along the log, stronger than
  planks (span 0.22); end grain is loose concentric rings.
- **Bark** (beech, birch, oak, pine, swamp and acacia trunks, Yggdrasil shoots): greyer (saturation 0.27), long
  vertical fissures, dark in the cracks and lighter on the ridges; beech and oak bark are near white grey with painted
  moss, birch bark white with black marks (tint 0.69). Strongest normals of any wood (k 6.3, tilt 14 degrees).
- **Fine wood** (finewood, oak table, runed furniture, fine wood bow): smooth, few streaks, big soft washes (5 % single
  texels), paler (V50 0.47).

### Metal

- **Iron** (iron armour, helmet, buckler, mace, atgeir, battleaxe, iron beam, iron shields; metal mask texels only):
  neutral grey 0.34 to 0.65, a lighter line of one to three texels along edges and rims, darker rings round bosses and
  rivets, soft mottling of two to six texels; chain mail is a painted pattern of light links on dark (IronArmorChest).
  Metal mask red 1 on metal, alpha 1; `_Metallic` 1, metal smoothness 0.60 to 0.80, base smoothness 0.1 to 0.4.
- **Bronze**: flat orange tan (hue 27, saturation 0.54, V50 0.59), the lightest metal after gold; buckler faces
  lighter in the middle, darker to the rim.
- **Copper**: in the game mostly an ore (green and orange flecks on rock, `copper_ore_big_d`) and Dvergr fittings
  (`anvil.png`, a 32 px grey noise, tinted (1, 0.75, 0.57)); ingots are untextured (tint (1, 0.66, 0.46), metallic 1).
- **Tin**: an ore texture (dark rock with pale veins); the ingot is untextured (0.94 grey, metallic 1, smoothness 0.78).
- **Silver**: light neutral grey (V50 0.56), the most single-texel noise of any metal (0.45); the Silver warhammer is
  cool blue grey.
- **Black metal**: near black (V50 0.13), faint grey strokes along blades, one lighter edge line; the bar's texture is
  a light grey noise darkened by its material.
- **Gold**: saturated yellow (hue 45, V50 0.84), high contrast (span 0.58): the crown is flat ochre, the coin pile white
  discs tinted yellow, gold veins bright flecks on dark rock.
- **Flametal**: blue grey (hue 219, saturation 0.25) with orange, red and teal patches; the ore glows through an
  emission map.

### Bone, antler, horn, teeth

- **Bone** (Skeleton, bone tower shield, Spinesnap, bone fragments, Bonemass bones, troll skeleton, giant skull, bone
  throne): cream to ochre (hue 45, V 0.31 to 0.71), bones separated by near-black painted gaps; at 4x: pits of one to
  two texels, a darker brown towards joints, soft grime clouds on big bones (Bonemass). Normal maps strong (k 5.4, tilt
  15 degrees). Flat ivory swatches, one per UV island, on small bone items (the items agent).
- **Antler** (the Eikthyr antler item, the antler pickaxe): pale beige with red-brown streaks and blood.
- **Horn** (the Beta horn, the anniversary horn): pale, banded along the horn.
- **Teeth** (wolf fang spear, Draugr fang bow, ash fang): smooth ivory gradients darker to the root, almost no texel
  noise (5 %).

### Leather, hide, fur

- **Leather** (leather helmet, scraps, Lox armour, belt, grips, saddles): dark red brown (V50 0.29, saturation 0.64),
  flat with medium washes (16 to 32 texels), lighter where it bulges; wraps are saturated deep red (74 to 104, 0 to 22,
  5 to 8 sRGB) with a few darker diagonal stripes (the items agent).
- **Hide** (deer, bear, troll hides, rugs, tanning rack): orange brown with a darker spine and edges, cutout outline
  (alpha cut 0.36 to 0.76 of the texture).
- **Fur** (wolf and lox capes, fur rugs, wolf hide, bear rug): low saturation (0.17), big soft clouds, strands only as
  faint one-texel streaks; the Wolf and Fenring are grey tinted by the material; fur and rags are cutout cards.

### Cloth, rope, thatch

- **Linen and wool** (clothes, jute carpets, banners, sails, tents, capes, rugs): the colour is the dye (red jute
  #6d251f, blue jute #064b67, undyed sail #aeaaa5), the paint nearly flat (span 0.29 across dyes),
  a checker weave of two texels on jute, painted trims and borders; within one dye the span is 0.07 (undyed sail) to
  0.2 (jute); cutout ragged edges on capes and banners.
- **Rope** (cart rope, Norn thread): nearly all single-texel noise (75 %), light grey brown.
- **Thatch and straw** (roof, roof corner, straw floor, bird nest, straw hat): vertical strands one texel wide, light
  and dark straws alternating (65 % single texel), dark vertical gaps where the cutout edge falls.

### Stone

- **Stone** (stone wall piece, cobbles, stone walls and floors, rocks, pillars, runestones, stone chest): neutral grey
  (saturation 0.04), soft cloudy washes 12 texels and up, a few dark cracks one texel wide; runes and carvings painted
  as darker lines. The strongest normals (k 7.5). Rock tops get the biome's moss from the shader (`shaders.md`).
- **Black marble** (Mistlands): near black (V50 0.18) with thin pale veins; the normal map does not follow the paint.
- **Grausten** (Ashlands): warm dark grey (hue 12, saturation 0.09), the narrowest range of all (span 0.07), fine
  diagonal chisel strokes; the cracked version adds dark crack lines.

### Crystal, ice, obsidian

- **Crystal** (crystal battleaxe, gemstones, proustite): smooth gradients, no texel noise (6 %), saturated (0.74);
  mountain crystals and ingot-like gems are untextured, transparent and emissive (`crystal_exterior`: tint
  (0.77, 0.61, 1), alpha 0.35, emission (0.34, 0.55, 1.04)).
- **Ice** (cave ice, ice shelves, frozen ship, black ice, frost core): pale cyan (hue 186), large smooth patches
  (correlation 13.5 texels), white cracks; the ice textures are near white, coloured by tints and `_FRESNEL` edge glow.
- **Obsidian**: black glass with grey streaks; the item is the grey `iron.png` tinted 0.40.

### Chitin, skin, flesh

- **Chitin** (chitin, carapace armour and shields, Seekers, Seeker Queen, Deathsquito, Tick): dark (V50 0.32) with light
  edges and streaks radiating along plates; red, orange, teal hues; glossy (0.5).
- **Skin** (Troll, Draugr, Greydwarf, Goblin, Jotun, Neck, the player, Abomination, Morgen, Fenring): mottled blotches
  of 8 to 16 texels, painted muscle shading, lighter on the back (see "Light painted in"); little texel noise (8 %).
  Olive greens for Draugr and Goblins, blue for Trolls and Jotuns, peach for the player.
- **Flesh** (raw meats, entrails, soft tissue, hearts, blood bags): pink red with pale fat streaks one to two texels
  wide; glossy (0.5).

### Plants

- **Mushrooms**: bright caps (V50 0.84) with pale spots (the berserker toadstool) or smooth tan (Boletus), pale stems.
- **Leaves, needles, crops** (beech, birch, oak, bushes, pine needles, Yggdrasil leaves, kale, turnip, swamp plant):
  mid green (hue 78), each leaf a cutout shape (alpha cut 0.39 to 0.77 of the texture) with a lighter midrib and darker
  edge; clusters of leaves painted on one card.
- **Moss** (the moss textures rocks carry in `_MossTex`, the Mistlands creep, worn planks): yellow green, nearly flat
  (span 0.07) with fine noise.

## Light painted in

- **From above: creatures, not pieces.** Up / side / down medians of value on static meshes (n samples): planks 0.44 /
  0.42 / 0.42 (12), stone 0.36 / 0.35 / 0.35 (12), logs 0.54 / 0.44 / 0.51 (6), grausten 0.30 / 0.31 / 0.31 (5), thatch
  0.56 / 0.56 / 0.56 (4). On skinned creature bodies: skin 0.44 / 0.42 / 0.37 (11). A piece's texture is shared by faces
  facing every way (planks on floors, walls and roofs), so it cannot carry light from above; a creature's is laid out
  once, so its back is painted lighter. Paint new pieces and props without top light and new creatures with about 15 %
  darker undersides.
- **No light direction in texture space.** A least-squares fit of value on the normal's x and y gives a light direction
  that differs between samples by 60 to 130 degrees within every family: there is no consistent painted light
  direction in UV space.
- **Normal maps from the albedo.** Correlation of the normal map's slope with the albedo's (nfa above) 0.7 to 0.93 in 24
  families, at a strength k of 2 (metal, cloth, teeth) to 7.5 (stone). The mean tilt of the normals is small: 2 to 16
  degrees in most families (median 8; tin ore 35, gold 32; the items agent's weapons 3 to 15, median about 6). So the
  normal adds a pixel-sized emboss of the paint, not new detail: `detail_shared` (how much of the normal's fine detail
  is also in the albedo) 0.1 to 0.5.

## Material values

- **Smoothness.** Non-metal `_Glossiness` 0.1 to 0.25 (wood 0.18, bone 0.24, cloth 0.10, stone 0.23, skin 0.16); wet
  and glassy things higher (flesh 0.5, crystal 0.58, ice 0.5, chitin 0.5, marble 0.5).
- **Metal.** A metal mask with red 1 on metal texels and 0 elsewhere (hard edges), alpha 1 under the metal of iron,
  bronze and black metal (mean 0.98 to 1.0; silver 0.61); `_Metallic` 1, `_MetalGloss` 0.66 median (0.40 to 0.83),
  `_MetalColor` white on 145 of
  158. Pieces keep the mask in `_MetallicTex` with `_MetallicAlphaGloss` 0.63.
- **Emission.** 83 Creature and 193 Standard materials glow: eyes (skeleton eyes (1.53, 0.78, 0.29), Draugr eyes
  (0.29, 1.54, 0.97)), boss marks (BonemawSerpent (0, 28.5, 10.2)), embers; through a glow map, or a flat emissive
  colour on a small material of its own (every eye in the table below).
- **Cutout.** Fur cards, hides, rugs, leaves, capes, straw: alpha cut at 0.5 (0.12 to 0.74); the cut shape is painted
  ragged, never a straight edge.

## Untextured and tinted materials

140 of the drawn materials have no albedo at all: a colour, metallic and smoothness, often a borrowed normal map.

| Material | Colour (tint) | Metallic / smoothness | Other |
| --- | --- | --- | --- |
| tin ingot | 0.94 grey | 1 / 0.78 | |
| copper ingot | (1, 0.66, 0.46) | 1 / 0.81 | normal `gouacherock_n`, strength 0.3 |
| bronze ingot | (1, 0.76, 0.46) | 1 / 0.81 | same |
| silver bar | white | 1 / 0.82 | same |
| iron nails | 0.75 grey | 1 / 0.61 | |
| coin | (0.85, 0.82, 0.35) | 1 / 0.7 | `copper_n` |
| crystal (mountain) | (1, 0.67, 0.88), alpha 0.87 | 0 / 1 | emission (0.5, 0.71, 1.1) |
| ruby | (1, 0, 0), alpha 0.87 | 0.59 / 0.71 | |
| tar | black | 0 / 0.83 | |
| eyes | saturated colour | 0 / 1 | emission 1.5 to 6 |
| fishing baits | one colour per biome | 0 / 0.39 | |
| snow caps (`Valheim/Snow Mesh`) | (0.9, 0.96, 1) | 0.91 / 0.32 | |

Flametal and obsidian items reuse the grey `iron.png` with a tint ((1, 0.54, 0.37) and emission for flametal, 0.40 grey
for obsidian). 409 materials tint a texture (`recolour.md`).

## The recipes: `blender/workshop/paint.py`

```python
from workshop import paint
planks = paint.wood_planks("deck", axis="Y")            # grain along Y
blade = paint.iron("blade")
grip = paint.leather("grip", region="trim")
banner = paint.linen("banner", dye="red")                # or tint="#3a5a8a"
troll = paint.skin("hide", preset="troll")
rock = paint.make("stone.stone", "boulder", patches=[("moss", 0.2, 0.3)])
```

One function per family: `wood_planks`, `wood_dark`, `wood_logs`, `wood_bark`, `wood_fine`, `iron`, `bronze`,
`copper`, `tin`, `silver`, `blackmetal`, `gold`, `flametal`, `bone`, `antler`, `horn`, `teeth`, `leather`, `hide`,
`fur`, `linen`, `rope`, `stone`, `marble`, `grausten`, `thatch`, `crystal`, `ice`, `obsidian`, `chitin`, `skin`,
`flesh`, `mushroom`, `leaves`, `moss`; or `paint.make(family, name, **overrides)`. Each returns a Principled BSDF
material the pipeline's bake reads, with custom properties `region` (the family's default paint region) and
`paint_family`.

Each recipe stacks, in order (`paint_layers.py`, `paint_patterns.py`):

1. **Blotches** in the family's dark, mid and light tones (`palette.md`; `tones_from` in `paint_specs.py` names the
   samples a family takes them from). Blender's noise was measured by baking it: at detail 5 and roughness 0.7 its Fac
   has 10 / 50 / 90 % points 0.422 / 0.498 / 0.574 and its correlation length is 0.22 / scale metres; roughness 0.7
   gives each octave about the same share of the variation, as the game's paint has. The ramp puts the dark and light
   tones on the noise's 10 and 90 % points. The blotch size is the family's in texels (the variance-weighted mean of
   its bands from 4 texels up, capped at 24) turned into metres at the asset's density.
2. **Grain** (wood, iron, antler, obsidian): a noise drawn out along `axis`.
3. **The family's pattern**: plank seams with a shade per board (`seams`), bark fissures, horn bands, strands (fur,
   thatch), a two-texel weave (cloth), a rope's twist, cracks (stone dark, ice light), veins (marble, leaves), diagonal
   chisel hatching (grausten), fat marbling (flesh).
4. **Patches**: stains, rust, moss, flametal's ember and teal (`patches=[(colour, cover, size_m)]`).
5. **Bump** from the painted coat's value (luminance to the power 1/2.2), distance = k / density: the game's normal,
   as far as a bake's bump can see it.
6. **Texel jitter**: every cube of one texel a little lighter or darker, sized to the family's single-texel share.
7. **Worn edges**: hard convex edges lifted towards the light tone, found by the Bevel node's normal leaning from the
   true one and the occlusion saying the edge is in the open. A smooth-shaded thin tube has no hard edges, so it is
   not lifted all over, which Cycles' pointiness does (`README.md`, "Pointiness lifts a thin tube everywhere").
8. **Hollows**: the ambient-occlusion node within six texels darkens towards a near-black of the dark tone.
9. **Light from above** (`top`): skin 0.16 and fur 0.12, nothing else; the tones are raised to keep the mean.

Overrides on any recipe: `tones`, `tint` (keeps each tone's luminance), `preset` or `dye` (tones of one game texture:
`paint_specs.PRESETS`, such as skin `troll`, `draugr`, `greydwarf`, `goblin`, `human`, `jotun`; cloth `red`, `blue`,
`undyed`, `brown`, `tent`, `ship`; hide `deer`, `bear`, `troll`, `seal`, `moose`; fur `wolf`, `lox`, `bear`; crystal
`lilac`, `red`, `gem`; mushroom `boletus`, `toadstool`, `mistlands`; gold `crown`, `coins`; `palette.md` lists their
tones), `region`, `axis`, `blotch_m`,
`density` (the asset's px per metre), `jitter`, `relief`, `edges`, `hollows`, `top`, `patches`, `pattern`, `grain`,
`contrast`, `smooth`, `rough`, `metal`.

### Using them

- **Density.** Pass the asset's texel density (`density=`) when it differs from the family's (the swatch model
  computes it from `TEXTURE_SIZE` and the surface area); blotches, jitter and the bump follow it.
- **`AO_STRENGTH` 0.2.** The recipes paint their own hollows; the pipeline's default 0.6 on top of them darkens the
  darkest tenth to about twice what the game has (bone's 10th percentile value 0.24 at 0.6 against the game's 0.47;
  0.45 at 0.2).
- **The normal the game's way.** The bake's bump cannot see the texel jitter or the painted hollows and edges, so the
  baked normal follows the albedo at 0.4 to 0.8 where the game's does at 0.8 to 0.99. `paint_normal.image_from_albedo(
  albedo, normal, strength)` makes the normal from the baked albedo at the family's strength k (with the baked normal
  kept under it); the check's `hooked_*` lines show the result (correlation 1.0 against the game's 0.7 to 0.93, at
  the family's strength; the recipes cap it at 6). The pipeline does not call it yet.
- **Metal.** The bake keeps only colour and normal, and `GameMaterials.Plain` makes the dressed material all matte:
  a metal recipe's `metal` value shows only in Blender's preview (`shaders.md`).
- **Cutout.** The pipeline bakes no alpha: leaves, fur cards and ragged cloth edges need modelled outlines or a mask
  the pipeline does not make yet.

## The check: `codex/tools/paint_check.py`

`python codex/tools/paint_check.py` builds `codex/tools/paint_swatches` once per family through the pipeline (a
shape the size of a game thing of that family, grown until its atlas is 128 px at the family's density; texture and
density as the game's), measures the bake exactly as the game's textures were measured, and writes
`codex/out/paint/check/`: `paint_check.md` (every line), `paint_check.json`, `paint_check_<n>.png` (per family the
swatch beside three of the game's samples, 4x, point filtered) and, with `--lineup`, the preview of every swatch in a
row. A line is `ok` within 0.06 to 0.10 of the game's median (a ratio of 0.67 to 1.5 for blotch size and normal
strength), `range` within the game's spread of samples, `off` otherwise. Families whose recipe takes its tones from a
few samples are compared with those samples' numbers. The normal lines are skipped where the game's normal map does
not follow its albedo (nfa under 0.5).

Final run (2026-09-29): 331 of 415 lines ok, 40 within the game's range, 44 off. Each cell is swatch / game;
`~` within the game's range of samples, `x` off. `k (hook)` is the normal strength after `paint_normal.from_albedo`.

| Family | px | px/m | V10 | V50 | V90 | S | span | blotch m | local | fine | blotchy | k (hook) | ok / range / off |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| wood.planks | 128 | 37 | 0.34 / 0.35 | 0.42 / 0.44 | 0.49 / 0.52 | 0.56 / 0.55 | 0.14 / 0.11 | 0.16 / 0.17 | 0.56 / 0.46 | 0.46 / 0.48 | 0.34 / 0.34 | 2.57 / 2.59 | 13 / 0 / 0 |
| wood.dark | 128 | 31 | 0.14 / 0.13 | 0.25 / 0.34 ~ | 0.39 / 0.39 | 0.52 / 0.45 | 0.25 / 0.27 | 0.78 / 1.69 x | 0.23 / 0.18 | 0.18 / 0.36 x | 0.72 / 0.54 x | 3.18 / 3.34 | 7 / 1 / 5 |
| wood.logs | 128 | 63 | 0.35 / 0.34 | 0.42 / 0.45 | 0.55 / 0.57 | 0.45 / 0.48 | 0.20 / 0.22 | 0.25 / 0.34 | 0.41 / 0.45 | 0.41 / 0.43 | 0.48 / 0.41 | 3.81 / 3.92 | 11 / 0 / 2 |
| wood.bark | 128 | 35 | 0.20 / 0.25 | 0.28 / 0.35 ~ | 0.43 / 0.48 | 0.30 / 0.27 | 0.23 / 0.24 | 0.23 / 0.56 ~ | 0.53 / 0.53 | 0.43 / 0.69 x | 0.38 / 0.16 x | 5.39 / 4.67 | 9 / 2 / 2 |
| wood.fine | 128 | 91 | 0.39 / 0.36 | 0.47 / 0.47 | 0.58 / 0.59 | 0.46 / 0.48 | 0.19 / 0.24 | 0.20 / 0.86 ~ | 0.28 / 0.24 | 0.22 / 0.28 | 0.69 / 0.55 x | 3.64 / 3.67 | 9 / 2 / 2 |
| metal.iron | 128 | 55 | 0.34 / 0.34 | 0.43 / 0.43 | 0.62 / 0.65 | 0.00 / 0.02 | 0.28 / 0.26 | 0.26 / 0.12 ~ | 0.39 / 0.49 | 0.32 / 0.30 | 0.55 / 0.46 | 2.27 / 2.30 | 10 / 2 / 1 |
| metal.bronze | 128 | 64 | 0.47 / 0.44 | 0.62 / 0.59 | 0.82 / 0.85 | 0.47 / 0.54 | 0.35 / 0.39 | 0.22 / 0.12 ~ | 0.42 / 0.54 | 0.36 / 0.35 | 0.51 / 0.43 | - | 8 / 1 / 0 |
| metal.copper | 128 | 14 | 0.33 / 0.34 | 0.36 / 0.36 | 0.40 / 0.39 | 0.47 / 0.47 | 0.07 / 0.05 | 0.14 / 0.14 | 0.85 / 0.93 | 0.80 / 0.89 | 0.10 / 0.04 | - | 9 / 0 / 0 |
| metal.tin | 128 | 26 | 0.28 / 0.25 | 0.53 / 0.56 | 0.71 / 0.71 | 0.00 / 0.02 | 0.43 / 0.46 | 0.30 / 0.22 | 0.48 / 0.54 | 0.33 / 0.27 | 0.52 / 0.52 | 3.86 / 4.32 | 11 / 0 / 2 |
| metal.silver | 128 | 76 | 0.49 / 0.49 | 0.59 / 0.62 | 0.68 / 0.67 | 0.00 / 0.02 | 0.19 / 0.18 | 0.13 / 0.07 x | 0.58 / 0.66 | 0.62 / 0.57 | 0.27 / 0.29 | 2.60 / 2.87 | 10 / 0 / 3 |
| metal.blackmetal | 128 | 86 | 0.09 / 0.10 | 0.13 / 0.13 | 0.24 / 0.28 | 0.00 / 0.03 | 0.15 / 0.18 | 0.16 / 0.08 ~ | 0.39 / 0.42 | 0.28 / 0.27 | 0.56 / 0.53 | - | 8 / 1 / 0 |
| metal.gold | 128 | 38 | 0.71 / 0.65 ~ | 0.86 / 0.85 | 0.96 / 0.95 | 0.61 / 0.63 | 0.25 / 0.31 | 0.26 / 0.29 | 0.54 / 0.69 ~ | 0.52 / 0.49 | 0.36 / 0.33 | 2.78 / 2.23 | 8 / 2 / 3 |
| metal.flametal | 128 | 47 | 0.31 / 0.29 | 0.42 / 0.38 | 0.55 / 0.55 | 0.16 / 0.25 ~ | 0.24 / 0.18 | 0.13 / 0.27 ~ | 0.55 / 0.42 | 0.37 / 0.40 | 0.42 / 0.48 | 0.53 / 0.53 | 9 / 3 / 1 |
| bone.bone | 128 | 48 | 0.45 / 0.47 | 0.58 / 0.60 | 0.74 / 0.76 | 0.40 / 0.40 | 0.29 / 0.28 | 0.25 / 0.11 ~ | 0.49 / 0.53 | 0.44 / 0.44 | 0.41 / 0.39 | 4.84 / 4.28 | 10 / 2 / 1 |
| bone.antler | 128 | 39 | 0.54 / 0.51 | 0.65 / 0.66 | 0.79 / 0.80 | 0.43 / 0.41 | 0.26 / 0.29 | 0.20 / 0.16 | 0.51 / 0.51 | 0.36 / 0.45 | 0.48 / 0.33 x | - | 8 / 0 / 1 |
| bone.horn | 128 | 66 | 0.58 / 0.51 ~ | 0.69 / 0.67 | 0.80 / 0.79 | 0.33 / 0.34 | 0.22 / 0.28 ~ | 0.15 / 0.19 | 0.58 / 0.54 | 0.57 / 0.57 | 0.36 / 0.31 | 2.46 / 2.50 | 9 / 2 / 2 |
| bone.teeth | 128 | 54 | 0.44 / 0.38 | 0.58 / 0.57 | 0.69 / 0.66 | 0.42 / 0.38 | 0.24 / 0.32 ~ | 0.22 / 0.19 | 0.30 / 0.25 | 0.13 / 0.07 | 0.74 / 0.86 x | 1.92 / 1.95 | 10 / 1 / 2 |
| leather.leather | 128 | 59 | 0.26 / 0.26 | 0.29 / 0.29 | 0.40 / 0.42 | 0.58 / 0.64 | 0.14 / 0.17 | 0.34 / 0.19 ~ | 0.32 / 0.38 | 0.27 / 0.33 | 0.59 / 0.52 | - | 8 / 1 / 0 |
| leather.hide | 128 | 45 | 0.33 / 0.31 | 0.41 / 0.43 | 0.47 / 0.50 | 0.65 / 0.67 | 0.15 / 0.16 | 0.44 / 0.53 | 0.30 / 0.27 | 0.23 / 0.29 | 0.63 / 0.61 | 2.45 / 3.68 ~ | 11 / 1 / 1 |
| leather.fur | 128 | 38 | 0.28 / 0.27 | 0.38 / 0.39 | 0.51 / 0.51 | 0.16 / 0.17 | 0.23 / 0.23 | 0.52 / 0.36 | 0.25 / 0.31 | 0.16 / 0.23 | 0.74 / 0.64 | 2.74 / 2.76 | 12 / 1 / 0 |
| cloth.linen | 128 | 41 | 0.38 / 0.38 | 0.46 / 0.52 | 0.60 / 0.72 x | 0.43 / 0.38 | 0.22 / 0.29 ~ | 0.34 / 0.84 ~ | 0.40 / 0.41 | 0.51 / 0.54 | 0.41 / 0.31 x | 2.10 / 3.23 ~ | 6 / 5 / 2 |
| cloth.rope | 128 | 72 | 0.49 / 0.48 | 0.57 / 0.60 | 0.65 / 0.68 | 0.09 / 0.11 | 0.16 / 0.20 | 0.03 / 0.02 x | 0.72 / 0.86 | 0.71 / 0.76 | 0.18 / 0.10 | 2.25 / 2.27 | 10 / 0 / 3 |
| stone.stone | 128 | 35 | 0.28 / 0.28 | 0.35 / 0.37 | 0.47 / 0.47 | 0.05 / 0.04 | 0.19 / 0.18 | 0.52 / 0.55 | 0.36 / 0.31 | 0.29 / 0.27 | 0.58 / 0.61 | 5.48 / 7.47 | 12 / 1 / 0 |
| stone.marble | 128 | 40 | 0.14 / 0.14 | 0.20 / 0.18 | 0.29 / 0.29 | 0.00 / 0.03 | 0.14 / 0.15 | 0.20 / 0.17 | 0.58 / 0.60 | 0.59 / 0.50 | 0.28 / 0.32 | - | 9 / 0 / 0 |
| stone.grausten | 128 | 32 | 0.23 / 0.23 | 0.29 / 0.30 | 0.33 / 0.32 | 0.04 / 0.09 | 0.09 / 0.07 | 0.25 / 0.21 | 0.54 / 0.61 | 0.47 / 0.47 | 0.41 / 0.33 | 2.75 / 2.76 | 11 / 2 / 0 |
| thatch.straw | 128 | 47 | 0.34 / 0.36 | 0.47 / 0.53 ~ | 0.59 / 0.61 | 0.41 / 0.40 | 0.26 / 0.18 ~ | 0.09 / 0.06 ~ | 0.68 / 0.77 | 0.71 / 0.74 | 0.19 / 0.11 | 3.18 / 3.35 | 7 / 3 / 3 |
| crystal.crystal | 128 | 41 | 0.59 / 0.56 | 0.96 / 0.96 | 1.00 / 1.00 | 0.18 / 0.10 x | 0.40 / 0.44 | 0.63 / 0.84 | 0.10 / 0.08 | 0.03 / 0.10 | 0.93 / 0.83 | - | 8 / 0 / 1 |
| crystal.ice | 128 | 43 | 0.51 / 0.48 | 0.66 / 0.66 | 0.77 / 0.76 | 0.44 / 0.36 ~ | 0.26 / 0.20 ~ | 0.28 / 1.08 ~ | 0.45 / 0.34 | 0.34 / 0.38 | 0.49 / 0.43 | 3.06 / 3.29 | 10 / 3 / 0 |
| crystal.obsidian | 128 | 57 | 0.13 / 0.09 | 0.23 / 0.22 | 0.31 / 0.29 | 0.00 / 0.03 | 0.18 / 0.20 | 0.10 / 0.13 | 0.56 / 0.55 | 0.44 / 0.39 | 0.41 / 0.40 | - | 9 / 0 / 0 |
| chitin.chitin | 128 | 41 | 0.18 / 0.17 | 0.32 / 0.27 | 0.46 / 0.45 | 0.41 / 0.39 | 0.29 / 0.28 | 0.34 / 0.23 | 0.39 / 0.38 | 0.32 / 0.36 | 0.54 / 0.48 | 3.96 / 4.77 | 12 / 0 / 1 |
| skin.skin | 128 | 43 | 0.28 / 0.28 | 0.35 / 0.34 | 0.45 / 0.48 | 0.52 / 0.52 | 0.17 / 0.20 | 0.33 / 0.19 x | 0.35 / 0.42 | 0.28 / 0.34 | 0.61 / 0.51 x | 4.28 / 3.17 | 11 / 0 / 2 |
| skin.flesh | 128 | 46 | 0.47 / 0.46 | 0.58 / 0.57 | 0.69 / 0.69 | 0.47 / 0.53 | 0.21 / 0.17 | 0.18 / 0.09 ~ | 0.55 / 0.60 | 0.48 / 0.41 | 0.41 / 0.34 | 2.58 / 2.63 | 10 / 2 / 1 |
| veg.mushroom | 128 | 34 | 0.57 / 0.57 | 0.60 / 0.61 | 0.67 / 0.68 | 0.25 / 0.26 | 0.09 / 0.11 | 0.17 / 0.17 | 0.59 / 0.50 | 0.39 / 0.27 x | 0.42 / 0.57 x | - | 7 / 0 / 2 |
| veg.leaves | 128 | 36 | 0.36 / 0.33 | 0.47 / 0.45 | 0.59 / 0.61 | 0.45 / 0.47 | 0.23 / 0.22 | 0.34 / 0.25 | 0.49 / 0.46 | 0.53 / 0.39 x | 0.37 / 0.43 | 3.55 / 3.71 | 10 / 2 / 1 |
| veg.moss | 256 | 160 | 0.36 / 0.36 | 0.39 / 0.39 | 0.42 / 0.42 | 0.48 / 0.50 | 0.05 / 0.07 | 0.06 / 0.06 | 0.48 / 0.49 | 0.39 / 0.39 | 0.48 / 0.42 | - | 9 / 0 / 0 |

What stays off: blotches larger than a 128 px swatch holds (dark wood 1.7 m, fine wood 0.86 m, ice 1.1 m: `blotch m`
and `blotchy`), the single-texel share of bark, thatch and leaves (their fibres and cutout edges), crystal's and
ice's saturation (the game's crystal textures are pale and coloured by emission and tint), and linen's lightest tenth
(the undyed sail is lighter than the recipe's mixed default).

## Not measured, or not right

- **Brushwork is described, not generated.** The recipes are noise-based; a hand-painted texture's large shapes (a
  shield's painted planks, a creature's painted muscles, runes) are modelled or painted per asset, not by a family
  recipe.
- **Small families.** Antler, horn, tin, rope and copper rest on one to three samples; their numbers are indications.
- **Families measured as the game mixes them.** Skin, cloth and chitin mix many creatures' and dyes' colours; the
  recipes take their default tones from a few chosen samples (`tones_from`) and offer the others as presets.
- **Top light on items** cannot be measured (an item's texture has no fixed up); the recipes add none.
- **The Creature shader's HSV formula** is compiled into the shader, which the export does not keep (`recolour.md`).
- **Swatch statistics on small atlases.** Bands above 16 texels do not fit a 128 px swatch the way they fit the
  game's 256 to 512 px textures; the check compares the shares below 16 texels (`fine`, `blotchy`).

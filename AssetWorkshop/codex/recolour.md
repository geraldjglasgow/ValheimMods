# Recolour

How the game makes one asset come in several colours, measured from its files, and how the workshop makes every part of
a new asset recolourable without losing the painted light and grime. Numbers from `measure/shaders_recolour.py`
(data in `data/shaders.json` key `recolour`) and `measure/paint_materials.py` (`data/paint.json` key `materials`); the
game code read (`LevelEffects`, `ItemStyle`, `MaterialVariation`, `MaterialVariationWorld`, `RandomMaterialValues`,
`VisEquipment`) was decompiled into the scratch folder only.

## The game's own ways

### 1. Hue, saturation and value on the Creature shader

`Custom/Creature` has `_Hue` (-0.5 to 0.5: a turn of the colour wheel, 0.5 being half-way round), `_Saturation` and
`_Value` (-1 to 1, offsets). The shader code is not in the export, so the exact formula is not measured; what is
measured is how the game sets them.

- **In material files**: 0 on all but 24 of the 336 drawn Creature materials. The 24 are variants made from another
  material's texture without repainting it: `Skeleton_dark` (0, -0.22, -0.05), `Skeleton_burned` (-0.18, -0.68, -0.54),
  `greyling` (-0.03, +0.12, 0), `swampfish_cave` (0, -0.75, +0.5), Hildir's quest bosses `GoblinBrute_hildir`
  (+0.47, -0.37, -0.16) and `GoblinShaman_Hildir` (-0.18, +0.16, 0), `fenring_Leader_Hildir` (value +0.22), and
  Hildir's market cloth: `HildirFabrics3/4/5/7/9` turn one fabric texture into five colours (hue -0.42, -0.5, +0.16,
  -0.30, -0.30; saturation -0.22, -1, +0.13, -0.10, -1).
- **Per star level, `LevelEffects`** (89 creature prefabs). `SetupLevelVisualization` copies material 0 of
  `m_mainRender` (only that slot; the copy is cached per prefab name and level), writes the level's `_Hue`,
  `_Saturation`, `_Value` and optionally `_EmissionColor`, scales the creature, and can switch a child object on.

  | | 1 star (level 2) | 2 stars (level 3) |
  | --- | --- | --- |
  | scale | median 1.1 (0.75 to 1.3) | median 1.2 (0.8 to 1.5) |
  | hue set on | 66 of 89, -0.48 to +0.5 | 62, -0.5 to +0.5 (median -0.1) |
  | saturation set on | 55, -1.0 to +0.3 | 60, -1.0 to +0.47 |
  | value set on | 28, -0.42 to +0.13 | 40, -0.65 to +0.40 |
  | emissive colour | 12 | 9 |
  | object switched on | 21 (Boar tusks, Deer antlers, Surtling, Charred) | 21 |

  Examples: Boar (+0.05, -0.1, 0) then (+0.09, -0.5, -0.05); Greydwarf (-0.06, +0.1, +0.05) then (-0.5, +0.1, 0);
  Draugr (+0.27, 0, 0) then (-0.25, +0.04, 0); Troll (-0.14, +0.1, 0) then (+0.44, +0.2, 0); Neck hue +0.24 then +0.42;
  Asksvin saturation -1 (grey) at one star; Seeker (-0.28, -0.3, -0.1) with an emissive colour; Dvergr +0.1 hue then
  saturation -1 with emission; Bjorn scale 1.3 and 1.5 with an emissive colour (12.6, 0.9, 0) at one star.
  A star creature is the same paint turned: the hue swing is large (0.05 to 0.5 of a turn) and the value is left at 0
  on most (61 of 89 at one star, 49 at two), so its light and grime stay where they were painted.

### 2. A tint over a grey texture

409 of the 1,676 drawn materials on meshes (24 %) multiply their texture by a `_Color` that is not white (Piece 95,
StaticRock 81, Standard 79, Vegetation 44, Creature 41). Two uses:

- **Darkening a shared texture**: stone pieces 0.79 grey (`stone.png` median #a19d94 becomes #7e7b74), worn stone 0.63,
  logs and thatch 0.79, Morkhalla rock slabs (0.63, 0.63, 0.67). One light texture serves several brightnesses.
- **Colouring a grey paint**: creatures meant to come in colours are painted in grey and coloured by the tint: the
  Wolf's texture has saturation 0.02; Skeleton_Mountains (0, 0.79, 1), Skeleton_Poison (0.61, 1, 0.24), Fenring and Ulv
  0.68 grey (the creatures agent, `data/creatures.json`); `dvergr_metal` colours the grey `anvil.png` (saturation 0)
  into copper with (1, 0.75, 0.57). A tint is a multiply, so it can only darken and colour; the light, blotches and
  grime painted into the grey stay.

### 3. Swapping the material or the texture

- **`MaterialVariation`** (per placed object, stored in the ZDO as `MatVar<slot>`): the Hen picks one of 5 materials
  (weights 1 each); the pet rock `Placeable_HardRock` 9 faces (weight 1 on one).
- **`MaterialVariationWorld`** (237 prefabs: dungeon rooms, cave pieces, crypt walls): replaces a room's materials when
  its location's biome or dungeon theme matches.
- **Separate materials and prefabs**: `Greydwarf_Frozen` (`greydrawrf_diffuse_frozen.png`), `Skeleton_frozen_d`,
  `Barka_ice_d`, the worn texture of every building piece (`paint.md`).

### 4. Style textures (item variants)

`Custom/Creature`'s `_UseStyles`, `_StyleTex` and `_Style`: an item with `m_variants` in its shared data carries an
`ItemStyle`, whose `Setup(variant)` sets `_Style` through `MaterialMan` (a property block, so the material is shared).
14 drawn Creature materials carry a style texture and 12 switch `_UseStyles` on: the wood shields (`ShieldWood`,
`ShieldBanded`: 4 variants, `shieldwood_paint.png`), the iron, black metal, silver and wood tower shields (7 variants,
`NewShields/.../Shield_Designs_Decals.png`, 1,024 px), flametal
and gold shields (5, `Flametal_Shield_Designs_Decals.png`), the root shield (4), the linen cape (6,
`CapeLinen_styles.png`). Each variant has its own inventory icon in `m_icons`.

- **Painted overlays**: `shieldwood_paint.png` (512 px) is a 4 x 4 grid of 128 px cells, each a copy of the albedo's
  UV space with one design painted on the shield face island (red and white, blue and white, red and black quarters),
  alpha the paint mask (3.5 % of its texels opaque). The designs are painted like the rest: saturated but scuffed, with
  scratches and dirt through them.
- **Dye swatches**: `CapeLinen_styles.png` (128 px) is six flat 32 px squares (light grey, dark grey, red, blue, green,
  ochre), one per variant.

### 5. Other per-object variation

- **`RandomMaterialValues`** (24 prefabs, all grausten pieces and a few dev rooms): a random `_UVOffset` between -2 and
  2 per placed piece, stored as `RandMatSeed` in the ZDO. Not a colour change: it slides the texture so neighbouring
  pieces do not show the same blotches.
- **`Custom/Piece` value noise** (`shaders.md`): every piece's value varies by a 64 px noise in world space.
- **The player**: `_SkinColor` on the body material and on hair through a property block, from the ZDO's skin and hair
  colour vectors (`VisEquipment.UpdateColors`); the player picks them in the character screen.
- **Weather**: `_SnowCover` and `_AddRain` darken and whiten at run time; never paint them in.

## The workshop's way: paint regions

Every material of a new asset names its **paint region**, and the pipeline bakes the regions into an ID mask beside the
albedo, so a recolour tool or a mod can change one part and leave the rest.

- **Marking.** The paint recipes (`blender/workshop/paint.py`) set `region` and `paint_family` on every material they
  make: `paint.iron("blade")` is region `metal`, family `metal.iron`. Override with `region=`: `paint.linen("banner",
  region="primary")`. Any other material is marked with `regions.mark(material, "primary", family="cloth.linen")`.
- **Baking.** When any material has a region, the build also writes `out/<name>_regions.png`: each texel the flat colour
  of its region (`regions.COLOURS`: primary #e6194b, secondary #3cb44b, trim #4363d8, metal #42d4f4, leather #f58231,
  cloth #911eb4, skin #ffe119, glow #f032e6, bone #fabed4, wood #9a6324, stone #a9a9a9, fur #469990, rope #aaffc3, gem
  #dcbeff, base #808080), black where nothing is baked, with the albedo's own UVs and margin. The manifest lists each
  region's share of the texels, its median baked colour and its paint families; the style check compares each region
  with its family (`tools/stylecheck.py`).
- **Variants.** `python codex/tools/recolour.py assets/<name> --variant blue --region primary=hue:+0.6` writes
  `out/<name>_albedo_blue.png`; several at once from `assets/<name>/variants.json`; `tools/variants_render.py` renders a
  sheet of them. Operations: `hue:+0.1` (turns), `sat:0.8` and `val:1.1` (factors), `tint:#8a6d3b` with `keep:0.4` (how
  much of the painted colour stays).

### Choosing the regions

A region is one colour field a player would call one thing. Rules, from how the game splits its own variants:

1. **Material regions by default.** Each paint family has one: wood, metal, bone, leather, fur, cloth, rope, stone,
   skin, gem, primary (chitin, plants, thatch). A variant then changes what a player would repaint (the cloth, the wood)
   and leaves what they would not (the metal).
2. **Role regions for what a variant changes.** Rename the part that carries the colour to `primary` (a banner's field,
   a shield's face, a cape, a creature's hide), its second colour to `secondary` (the banner's border, a creature's
   belly or markings) and small edging to `trim` (bands, stitching, the shield's rim if it is painted, not metal). The
   game's shield variants recolour exactly the face; the cape's the whole cloth; star creatures the whole body.
3. **Metal stays metal.** The game's items never hue-shift metal: bronze, iron, silver and black metal are different
   paints (`palette.md`), and its shields keep the rim and boss unpainted in every style. Keep `metal` a region of its
   own and leave it out of variants, or tint it only to another metal's tones.
4. **Glow is its own region** (eyes, runes, embers): it is recoloured with the emission colour, as `LevelEffects` does.
5. **Whole islands, at least two texels.** A region smaller than two texels at the asset's density bleeds into its
   neighbour in the mask's margin; keep a region's parts whole UV islands where the model allows.
6. **Never split one painted gradient.** If light, a stain or a grime cloud runs across two parts, both parts in one
   region or both recoloured alike; otherwise the variant shows a seam in the painted light.

### Keeping the painted light and grime

The game recolours without repainting: a hue turn, a saturation factor, or a tint over a grey. Each keeps a texel's
value (its painted light, blotches, grime, dark gaps) and changes only its colour. The workshop does the same:

- **Hue and saturation** (`hue:`, `sat:`): turn and scale in HSV, value untouched. Right for a region painted in its
  default colour (a red banner to a blue one). A hue turn cannot colour a grey: saturation 0 has no hue to turn.
- **Tint in a recipe** (`paint.<family>(tint=...)`): the colour replaced, each tone's luminance kept
  (`paint_specs.tinted`: every tone becomes the tint scaled to that tone's luminance; a tone the tint cannot reach at
  its luminance, such as a light tone of a deep blue, is pulled towards grey until it fits, so the tones never clip
  into one flat colour). Right for painting a part in a colour the family does not have.
- **Tint of a baked region** (`recolour.py`, `tint:#rrggbb`): the region's median colour becomes the tint (its
  brightness too), every texel keeps its brightness against that median in linear light (the baked light, grime and
  occlusion), and `keep` (0 to 1, default 0.5) of its own departure in hue from the median. Right for a region painted
  grey or near grey on purpose, as the game paints its Wolf.
- **Never** fill a region with a flat colour or multiply a coloured paint by a second colour: both flatten the value
  (a multiply of a saturated red by a blue makes near black).
- **Paint recolourable regions for it.** A region meant for tints: paint it near grey (saturation 0.02 to 0.15) with
  the full value structure, as the game's Wolf (0.02) and Skeleton (tinted per biome). A region meant for hue turns:
  paint it in its default colour at the family's saturation.

### How a mod ships the variants

| Way | What ships | Mod code | Game precedent |
| --- | --- | --- | --- |
| HSV on the dressed material | one albedo | set `_Hue`, `_Saturation`, `_Value` on a copy of the `Custom/Creature` material (whole material: every region turns) | `LevelEffects`, Hildir's fabrics |
| baked variant albedos | `<name>_albedo_<variant>.png` per variant (the tool writes them) | swap `_MainTex` on a material copy per variant | `Greydwarf_Frozen`, worn pieces |
| regions mask at load | albedo + `<name>_regions.png` | recolour the albedo on the CPU per region when the prefab is made | none (the game has no ID masks) |
| style atlas | an atlas of variant overlays + `_UseStyles` | `ItemStyle` and `m_variants`; needs an atlas builder the workshop does not have yet | shields, linen cape |

Baked variant albedos are the simplest and cost texture memory per variant (a 256 px albedo is 32 KB in DXT1, 43 KB
with mipmaps); HSV costs nothing but moves every region at once; the mask costs mod code and load time.

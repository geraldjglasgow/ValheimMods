# Weapons

What the game's weapons are made of, measured from the reference export on 2026-09-29 by `measure/items.py` (every
prefab with an `ItemDrop`; 1,519 read, 882 kept in categories). Numbers are in `data/items.json` under
`categories["weapon.*"]` and `categories["ammo.*"]`; renders, texture close-ups and icon sheets are in
`codex/out/items/` (`sheets/weapon.*.png`, `paint/`, `icons/`). Shields are in `shields.md`, pickaxes and other tools
in `tools.md`, the inventory icon in `icons.md`.

A weapon counts once per design: an enchanted or elemental copy (`SwordNiedhoggBlood`, `AxeGold_FrostFire`) and a
creature copy that draws the same meshes in the same materials are listed as `variants` and left out of the numbers.

## 1. The attach frame: where the hand holds it

`VisEquipment.AttachItem` copies the prefab's child named `attach`, parents the copy to the hand bone's
`RightHand_Attach` (or `LeftHand_Attach`) at zero position and zero rotation, and keeps the copy's own scale: the
`attach` node's local scale is the size in the hand, the prefab root's scale is lost. Everything the player holds is
built in this frame. Measured from the Player prefab's bones, expressed in the attach frame (Unity axes, metres):

| Axis | In the right hand | Measured |
| --- | --- | --- |
| origin | the middle of the closed fist, in the palm, 3.3 cm below the knuckle line | knuckles (`RightHandIndex1..Pinky1`) at y +0.033, x +0.006 to +0.014 |
| +Z | out of the top of the fist, on the thumb and index side: where a sword blade goes | index knuckle z +0.038, little finger knuckle z -0.046, thumb root z +0.047 |
| -Z | out of the bottom of the fist, the little finger side: where a pommel goes | |
| +X | the way the knuckles and straight fingers point: the leading side of a punch, where an axe's edge faces | fingertips x +0.086; wrist -0.10, elbow -0.36 |
| +Y | from the palm to the back of the hand (right hand) | knuckles at +Y |
| scale | the attach bone's world scale is 0.95 (the Player's `Visual`), but the item keeps its own scale | |

The left hand's frame is the same except Y: its knuckles sit at -Y (y -0.033), so -Y is the back of the left hand.
In the Player's rest pose both attach points face +Z forward, so an item pointing +Z points where the player looks.

**In Blender** (the workshop maps Unity (x, y, z) to Blender (-x, -z, y)): the fist at the origin, the blade up
Blender -Y, the axe's edge towards Blender -X, the flat of a blade facing Blender ±Z. The sheets in `out/items/sheets/`
draw every held item from Unity +Y with +Z up and +X to the right, fist at the red cross, one scale per sheet.

Where each kind sits in the frame (medians over the designs; ranges min to max):

| Kind | Long axis | Fist along the length (0 = far end behind the hand) | Ahead of the fist | Behind the fist | Striking side |
| --- | --- | --- | --- | --- | --- |
| sword | +Z | 0.07 to 0.13 (0.11) | 0.82 to 1.50 m (1.18) | 0.08 to 0.22 m (0.14) | both edges ±X, flat faces ±Y |
| two-handed sword | +Z | 0.16 to 0.17 | 1.58 to 1.63 m | 0.30 to 0.33 m | ±X |
| knife | +Z | 0.12 to 0.24 (0.19) | 0.29 to 0.62 m (0.44) | 0.07 to 0.15 m (0.09) | single-edged knives lean the blade to +X (KnifeBlackMetal, KnifeSilver) |
| one-handed axe | +Z | 0.15 to 0.18 (0.17) | 0.70 to 0.84 m (0.75) | 0.13 to 0.17 m (0.16) | head reaches +X 0.19 to 0.43 m (0.24); the edge faces +X |
| two-handed axe | +Z | 0.12 to 0.20 (0.17) | 1.38 to 1.51 m (1.43) | 0.20 to 0.36 m (0.29) | head reaches +X 0.34 to 0.44 m; beards and back spikes to -X up to 0.42 m |
| paired axes | two hafts crossing at the fist, heads at -Z | 0.46 to 0.47 | 0.38 to 0.39 m | 0.34 to 0.42 m | the two heads spread to ±X |
| club, mace | +Z | 0.10 to 0.27 (0.14) | 0.68 to 1.11 m (0.91) | 0.11 to 0.34 m (0.13) | head round the axis, symmetric |
| sledge | +Z | 0.22 to 0.35 (0.26) | 0.93 to 1.16 m (1.00) | 0.27 to 0.54 m (0.36) | head across X, faces ±X |
| spear | Z, **head at -Z** | 0.38 to 0.50 (0.44) | head 0.88 to 1.49 m below the fist (1.06) | butt 1.25 to 1.48 m above (1.39) | point at -Z: the spear clips hold it overhand |
| atgeir | +Z, the shaft **tilted 21 to 25 degrees** (22) towards +X | 0.19 to 0.26 (0.26) | 1.89 to 2.09 m (1.97) | 0.46 to 0.71 m (0.69) | blade at +X 0.77 to 0.84 m, along +Z |
| bow | Z, symmetric about the fist | 0.46 to 0.50 (0.48) | 0.74 to 1.13 m each way | | string on -X (0.33 to 0.63 m out), limbs bow to +X; the bow leans 9 to 16 degrees (14), top towards -X |
| crossbow | Z, **prod at +Z** | 0.42 to 0.47 (0.43) | prod 0.72 to 0.88 m ahead | stock 0.98 m behind | prod spans ±X 0.64 to 0.71 m (1.27 to 1.42 m wide) |
| staff | +Z (head up) | 0.23 to 0.49 (0.44) | 0.10 to 1.28 m (0.94) | 0.11 to 1.16 m (0.56) | head at +Z |
| fist weapon | none: a gauntlet round the fist | 0.36 to 0.50 | 0.10 to 0.29 m | 0.26 to 0.29 m | claws point +X and +Z |

Build a new weapon with its grip centred on the origin and measure it against these before anything else: a haft
that does not pass through the origin along Z, or a head on the wrong side, is held wrongly by every clip.

## 2. Budgets per kind

Designs only (variants left out). Triangles are the held model's; texture is the albedo's longest side; texel density
is texture pixels per metre of surface in the hand (`measure/items_geometry.surface`).

| Kind (key) | n | Triangles | Texture | px per metre | Length | Game example |
| --- | --- | --- | --- | --- | --- | --- |
| sword (`weapon.sword`) | 9 | 166 to 1,980 (308) | 64 to 256 (128) | 52 to 106 (61) | 0.91 to 1.71 m (1.29) | SwordIron 304 tris, 256 px atlas, 1.27 m |
| two-handed sword (`weapon.greatsword`) | 4 | 166 to 1,904 (545) | 64 to 128 (64) | 40 to 109 (47) | 1.91 to 1.96 m | THSwordKrom 580, 64 px, 1.91 m |
| knife (`weapon.knife`) | 10 | 164 to 676 (232) | 32 to 256 (128) | 48 to 135 (74) | 0.37 to 0.78 m (0.52) | KnifeCopper 232, atlas, 0.37 m |
| one-handed axe (`weapon.axe_1h`) | 8 | 128 to 1,917 (395) | 32 to 256 (64) | 49 to 116 (60) | 0.83 to 1.00 m (0.91) | AxeIron 508, 32 px, 0.88 m |
| two-handed axe (`weapon.axe_2h`) | 6 | 188 to 3,023 (1,080) | 64 to 128 (96) | 36 to 82 (66) | 1.61 to 1.82 m (1.74) | Battleaxe 578, 64 px, 1.70 m |
| paired axes (`weapon.axe_dual`) | 2 | 512 to 672 | 32 to 64 | 39 to 81 | 0.73 to 0.80 m | AxeBerzerkr 512, 64 px |
| club, mace (`weapon.mace`) | 8 | 200 to 2,874 (549) | 64 (all) | 56 to 110 (72) | 0.81 to 1.27 m (1.09) | MaceIron 618, 64 px, 1.03 m |
| sledge (`weapon.sledge`) | 5 | 168 to 3,194 (1,432) | 64 to 128 (64) | 43 to 133 (58) | 1.20 to 1.54 m (1.36) | SledgeIron 584, 64 px, 1.36 m |
| spear (`weapon.spear`) | 9 | 106 to 1,365 (450) | 64 to 128 (128) | 46 to 89 (63) | 2.30 to 2.97 m (2.36) | SpearBronze 232, 128 px, 2.43 m |
| atgeir (`weapon.atgeir`) | 6 | 224 to 1,445 (495) | 64 to 128 (128) | 54 to 88 (62) | 2.39 to 2.81 m (2.66) | AtgeirIron 252, 128 px, 2.81 m |
| bow (`weapon.bow`) | 9 | 260 to 3,904 (904) | 32 to 256 (128) | 32 to 181 (101) | 1.47 to 2.34 m (1.69) | BowFineWood 544, 128 px, 1.52 m |
| crossbow (`weapon.crossbow`) | 3 | 620 to 5,404 (1,624) | 32 to 128 (128) | 23 to 79 (58) | 1.70 to 1.86 m | CrossbowArbalest 620, 128 px, 1.70 m |
| staff (`weapon.staff`) | 12 | 410 to 3,660 (1,259) | 64 to 256 (128) | 30 to 362 (88) | 0.32 to 2.33 m (1.39) | StaffFireball 1,202, 128 px, 2.03 m |
| fist weapon (`weapon.fist`) | 4 | 692 to 4,404 (1,270) | 32 to 256 (96) | 58 to 110 (89) | 0.43 to 0.69 m | FistFenrirClaw 1,810, 32 px |
| bomb, throwable (`weapon.bomb`) | 12 | 48 to 1,294 (490) | 32 to 256 (128) | 55 to 262 (92) | 0.20 to 0.48 m (0.38) | BombOoze 158; the blob bottles 490 |
| arrow (`ammo.arrow`) | 12 | 28 to 288 (76) | 32 to 128 (128) | 28 to 207 | dropped 1.64 to 2.17 m (1.76) | ArrowFlint 76 tris on the shared 128 px `arrow_white` |
| bolt (`ammo.bolt`) | 11 | 26 to 326 (86) | 64 to 256 (64) | 23 to 94 (53) | 0.56 to 0.78 m (turret bolts 1.2 to 4.2 m) | BoltIron 26 tris on the shared 64 px `bolts_d` |
| creature weapons (`weapon.creature`) | 21 | 48 to 1,372 (508) | 32 to 512 (128) | 42 to 432 (58) | 0.68 to 6.31 m | skeleton_sword, draugr_axe, GoblinClub |

- **Low.** Half the game's weapons are under 600 triangles and most carry 64 or 128 px. A sword is a flat lozenge
  blade on 4 to 8 sides, a bar guard, a cylinder grip and a knob. Nothing smaller than 2 cm is modelled; it is
  painted.
- **Density, not resolution, is the constant.** 50 to 110 px per metre across kinds (median about 62): a 1.7 m axe
  on 64 px and a 0.9 m sword on a quarter of a 256 px atlas land at the same density. Aim for 55 to 75 px per metre.
- **One material.** Over the 107 player weapon designs: 1 renderer on 98, 1 material on 92 (2 or 3 on the rest), 1
  submesh on 87. Maps: albedo, normal and metal on 59, plus emission on 13 (the glowing ones), albedo and normal only
  on 21.
- **Shader.** The game's `Creature` shader on 60 % of held weapons, Unity's Standard (`builtin_46` in the data) on
  most of the rest. Creature materials on metal weapons: `_Metallic` 1, `_MetalGloss` 0.75 (0 to 1), `_Glossiness`
  0.19 (0 to 0.87), `_BumpScale` 1. A mod dresses into the game's material at runtime; see `shaders.md`.
- **Shared and bought textures.** SwordBronze, SwordIron, SwordSilver, AxeFlint, AxeStone, KnifeCopper, KnifeFlint,
  the Hammer and four pickaxes paint on one 256 px atlas (`weapons/_res/weapons1/weapons.png`), each using about 1 %
  of it (a patch roughly 25 px across). The nine wooden practice weapons share `WoodenWapons_d.png`. AxeIron and
  AxeBronze wear a store asset's textures shrunk to 32 px. Each Deep North weapon has its own 64 or 128 px sheet
  (`nordaxe_d` 64 px for AxeGold). Low resolution is the look, not an accident.

## 3. From tier to tier

Tier is the latest biome among the recipe's materials (`items_classify.tier`); `data/items.json["tiers"]` holds the
table over weapons, shields and pickaxes (one design counts once).

| Tier | n | Triangles, median (p25 to p75) | Texture, median | What changes, from the renders |
| --- | --- | --- | --- | --- |
| meadows: wood, stone, flint, antler | 12 | 264 (228 to 334) | 192 (the shared atlas) | lashed stone and flint heads on raw sticks, bark and hide wraps; AxeFlint 214, Club 200 |
| black forest: bronze, practice wood | 24 | 242 (183 to 417) | 64 | the first cast shapes: plain bronze blades, pink-orange; the practice set is the same outlines in flat wood |
| swamp: iron | 15 | 572 (300 to 619) | 128 | grey iron, wider plain heads, iron bands and pommels; Battleaxe 578, MaceIron 618 |
| mountain: silver, wolf, crystal | 12 | 598 (448 to 1,117) | 128 | pale silver, fangs and crystal; first curls and flared guards (SwordSilver, BattleaxeCrystal 1,029) |
| plains: black metal | 11 | 731 (345 to 1,709) | 128 | near-black metal with red leather wraps, hooked and bearded blades (SwordBlackmetal 308, BattleaxeBlackmetal 1,130) |
| mistlands: carapace, eitr, Dvergr | 16 | 872 (578 to 1,300) | 128 | organic shells, glowing cores, Dvergr engineering; ornament grows (SpineSnap 3,904, SkullSplittur 1,468) |
| ashlands: flametal, charred bone | 19 | 956 (506 to 1,476) | 128 | blue-grey flametal with red or ember accents, jagged spiky outlines, enchanted glow variants |
| deep north: gold | 21 | 1,904 (764 to 2,620) | 64 | blue steel plates framed by orange-gold scrollwork and knotted bands; the most modelled ornament (SwordGold 1,980, BattleaxeGold 3,023) |

Triangles roughly double every two to three tiers (about 250, 590, 800, 950, then 1,900 in the Deep North); texture size
does not grow (64 to 128 px throughout). Later tiers get their richness from modelled ornament (curls, spikes, bands, a
second material colour), not from finer paint. Boss and special weapons sit with their tier: SwordDyrnwyn 448 tris, 128
px; SwordMistwalker 184, 64 px; AxeJotunBane 750, 128 px; SledgeStagbreaker 3,194 (antlers).

## 4. Parts and proportions

Measured from the median cross-section along the length (`outline` per category: width and thickness at 20 slices
from the end behind the fist to the tip) and from the renders.

- **Sword** (SwordIron): pommel 5 to 6 cm wide, grip 4 to 5 cm round and 12 to 18 cm long with the fist 9 cm above
  the pommel end, guard a straight bar 14 to 20 cm across and 4 to 6 cm thick at 12 to 17 % of the length, blade 7 to
  10 cm wide and 2 to 3 cm thick, straight-sided to 80 % and tapering to a point over the last 10 to 15 %. The blade
  is 75 to 80 % of the length. Guards curl on silver (SwordSilver) and Deep North (SwordGold); black metal adds a
  single-edged hooked tip (SwordBlackmetal).
- **Two-handed sword** (THSwordKrom): 1.91 to 1.96 m; the hilt reaches 30 to 33 cm behind the fist (room for the
  second hand), guard 17 to 24 cm across at 23 to 28 % of the length, blade 9 to 12 cm wide and 2 to 3 cm thick for
  the remaining 70 %.
- **Knife**: 0.37 to 0.78 m; the handle reaches 7 to 15 cm behind the fist, often no guard; blade 5 to 7 cm wide,
  1 cm thick; most single-edged blades curve towards +X (KnifeBlackMetal, KnifeButcher, KnifeSilver).
- **One-handed axe** (AxeIron): haft 3 to 5 cm round, 0.88 m; head in the last 22 % of the length, 25 to 28 cm deep
  towards +X, 3 to 7 cm thick; a small butt knob or bare haft end 15 cm behind the fist.
- **Two-handed axe** (Battleaxe): haft 6 to 7 cm round (with its wraps), head in the last 30 %: 34 to 49 cm deep at its
  widest (90 % of the length), a bearded blade on +X, often a back spike or second blade on -X; the haft end 20 to
  36 cm behind the lower hand.
- **Club and mace**: haft 6 to 7 cm round; head in the last 20 %: 12 to 20 cm across (MaceIron), up to 54 cm with
  spikes (MaceNeedle); pommel cap 9 to 10 cm. The Club is one tapering log, 16 cm at the head.
- **Sledge**: haft 6 to 9 cm; head in the last 25 %: 54 to 69 cm across X, 18 to 36 cm deep in Y; butt cap 11 to 21 cm.
- **Spear**: shaft 4 to 5 cm round, 2.3 to 3.0 m; head the last 12 to 18 % at -Z, 6 to 9 cm wide; wraps or rings at the
  grip.
- **Atgeir**: shaft 5 to 6 cm round along the tilted axis; blade 15 to 21 cm wide in the last 25 % (at 78 to 90 % of
  the length) on +X, tapering to a point; a butt spike or cap.
- **Bow**: the string stands 28 to 42 cm off the grip at the middle (Bow 41 cm), the gap closing towards the tips;
  limbs 5 to 11 cm thick; one straight string from tip to tip.
- **Crossbow**: stock 0.98 m behind the fist, prod at +Z 0.72 to 0.88 m ahead, 1.27 to 1.42 m wide; convex mesh
  colliders.
- **Staff**: a crooked or knotted shaft 3 to 6 cm round with the working head at +Z: a crystal or orb in wraps
  (StaffIceShards, StaffRedTroll), a nest of roots (StaffGreenRoots); some are short wands 0.3 to 1.1 m.
- **Fist weapon**: a gauntlet shell round the fist (the origin inside it), claws or spikes 20 to 40 cm long pointing
  +X and +Z.
- **Bomb**: 0.2 to 0.5 m: a stoppered bottle (the `BombBlob_*` family, 0.48 m, one mesh with seven textures), a sack
  (BombOoze), a bundle (BombBile), a lumpy ball (BombSmoke).
- **Arrow and bolt**: the dropped arrow is one arrow at 1.5x (the mesh is 1.14 m long; ArrowWood's `model` node scale
  1.5): 1.64 to 1.83 m on the ground; in flight the projectile prefab is 1.37 to 1.80 m. The fletching is flat planes
  cut out by alpha on `StandardTwosided` (cutoff 0.49). Bolts are 0.56 to 0.78 m.

## 5. How they are painted

Looked at pixel by pixel (`out/items/paint/closeup_metal.png`, `closeup_wood_leather.png`, the per-category
`paint/<category>.png` with the uv layout drawn over the albedo) and counted by `items_textures.albedo_stats` over the
pixels the uvs cover (`categories[...]["paint"]`).

- **Metal is a flat field with a painted edge.** The body of a blade or head is one flat value with soft 2 to 6 px
  mottling; along the cutting edges and outlines runs a lighter line 1 to 3 px wide, painted in (Battleaxe: grey
  head, median sRGB (82, 81, 76), rim about 40 levels lighter; SwordBlackmetal and AtgeirBlackmetal: near-black
  (26 to 33) with a 2 px grey line tracing the blade; AxeGold: flat blue-grey plate with one pale arc along the edge).
  No scratches, no fine noise, no photographic texture.
- **Metal colours by tier** (median sRGB of the albedo where the metal map is metal): bronze (206, 146, 115) to
  (240, 175, 94); iron (104, 104, 104) to (181, 181, 181); silver (157 to 186 grey, a touch of blue); black metal (24
  to 41 grey, almost black); flametal (74 to 95 grey-blue, red and ember accents); Deep North gold (156, 117, 71) with
  blue steel.
- **Wood hafts**: warm brown, median (98 to 148, 73 to 113, 49 to 82); 1 px darker and lighter streaks along the grain,
  a few soft round knots (AtgeirBlackmetal), scattered small dark-red spots on the swamp-tier sheet (Battleaxe,
  MaceIron share one layout: haft strip on the right, head blob in the middle, pommel ball bottom left).
- **Leather and wraps**: black-metal wraps are saturated deep red (74 to 104, 0 to 22, 5 to 8) with a few darker
  diagonal stripes; rawhide lashings are pale bands with a dark line between turns (SpearWolfFang); the wrap is a
  modelled sleeve, the turns are painted.
- **Bone, fang, chitin**: palette swatches, flat pale ivory or yellow ovals with soft shading, one per uv island
  (BowDraugrFang's 64 px sheet is a grid of such blobs).
- **Few colours, soft steps.** 16 to 100 colours at 16 levels a channel (SwordBlackmetal 16, Battleaxe 26, MaceSilver
  100); the mean step between neighbouring pixels 3 to 14 levels. Saturation stays low on metal (0 to 0.1) and wood
  (0.4 to 0.6); colour comes from the wraps, gems and ornament.
- **Normal maps are shallow.** Mean tilt 3 to 15 degrees (median about 6); they carry the outlines of the uv islands
  as a bevel, wood grain lines, a few dents and rivets. Never high-frequency noise.
- **Metal map**: red (and green, blue) is a hard-edged metal mask, white where metal, black on wood and leather; alpha
  is smoothness, mostly full (the material's `_MetalGloss` and `_Glossiness` scale it). Metal covers 30 to 95 % of a
  weapon's used pixels.
- **Little light is painted in.** Weapon albedos have no overall top-to-bottom gradient and little ambient
  occlusion; swatch islands shade softly from the middle to the rim (BowDraugrFang). The shape reads from the normal
  map and the engine's light. Paint the edge line, a slight darkening in hollows, nothing more.

## 6. Effects and sounds

Every effect below is a prefab the item's shared data lists (`categories[...]["effects"]` counts how many designs use
each); reuse them by name.

| Kind | hitEffect | blockEffect | trigger / trail start | Other |
| --- | --- | --- | --- | --- |
| sword | vfx_HitSparks, sfx_sword_hit, fx_hit_camshake | sfx_metal_blocked, vfx_blocked, fx_block_camshake | fx_swing_camshake; sfx_sword_swing | |
| two-handed sword | vfx_HitSparks, sfx_sword_hit, fx_hit_camshake | sfx_metal_blocked, vfx_blocked, fx_block_camshake | sfx_kromsword_swing | |
| knife | vfx_HitSparks, sfx_arrow_hit, fx_hit_camshake_knife | sfx_metal_blocked (wood: sfx_wood_blocked) | sfx_knife_swing | |
| one-handed axe | vfx_clubhit, sfx_axe_hit (flint: sfx_axe_flint_hit), fx_hit_camshake | sfx_metal_blocked | sfx_axe_swing | |
| two-handed axe | vfx_clubhit, sfx_battleaxe_hit, fx_hit_camshake | sfx_wood_blocked | sfx_battleaxe_swing_wosh | |
| club, mace | vfx_clubhit, sfx_club_hit, fx_hit_camshake | sfx_metal_blocked | sfx_club_swing | MaceSilver: vfx_silvermace_hit |
| sledge | (area hit on trigger) | sfx_metal_blocked | start sfx_sledge_swing; trigger vfx_sledge_hit, sfx_sledge_hit | Demolisher: fx_sledge_demolisher_hit |
| spear | vfx_arrowhit, sfx_spear_hit, fx_hit_camshake | sfx_wood_blocked | sfx_spear_poke; thrown: sfx_spear_throw | projectile `<name>_projectile` |
| atgeir | vfx_arrowhit, sfx_spear_hit, fx_hit_camshake | sfx_wood_blocked | sfx_atgeir_attack | |
| bow | | sfx_wood_blocked | hold sfx_bow_draw; trigger sfx_bow_fire, vfx_bow_fire | arrow's projectile |
| crossbow | | sfx_wood_blocked | hold sfx_bow_draw; trigger vfx_arbalest_fire, sfx_arbalest_fire | bolt's projectile |
| staff | | sfx_wood_blocked | start: its own cast sound (sfx_firestaff_launch, sfx_icestaff_start ...) | `staff_<name>_projectile` |
| fist | vfx_HitSparks, sfx_unarmed_hit, sfx_sword_hit | sfx_fist_metal_blocked | start sfx_unarmed_swing; trail sfx_claw_swing | |
| bomb | | sfx_wood_blocked | sfx_bomb_throw | `<name>_projectile` |

- **Swing trail.** Every melee weapon carries `attach/equiped/trail` with a `MeleeWeaponTrail`: lifetime 0.3 s,
  material `club_trail` (three sledges `red_trail`), 4 subdivisions, `base` and `tip` children marking the striking
  part in the attach frame (swords: base z 0.12 to 0.20, tip z 0.80 to 1.37 near the point; axes: base (0.21, 0, 0.51)
  to tip (0.24, 0, 0.77) along the edge; maces and sledges: across the head). The `equiped` child is switched on only
  in a hand. Spears, bows, crossbows, staffs and fist weapons have no trail.
- **Upgrade glow.** `attach/UpgraderGlow` (a `ParticleIntensityScaler`, max quality 4, colour up to 3x, light up to
  2.5) with a particle system on material `DraugrFangGlow`, a force field and a point light (colour (0.48, 0.80,
  1.00), intensity 0.4, range 3 m): on every sword, axe, club, sledge, knife, spear, atgeir, bow, crossbow, staff
  and pickaxe design (143 items with variants); not on shields, fist weapons, bombs or the other tools.
- **Burning and glowing weapons** add particles and a light under `attach`: SwordIronFire (flames, embers, smoke on
  `flame`, `gnista`, `fog`; light (1.0, 0.62, 0.48) 2.0, 5 m), SwordDyrnwyn (`flameball_flipbook_gradient`, light
  orange 1.0, 10 m), SwordMistwalker (`DraugrFangGlow`, light (0.48, 0.80, 1.0) 1.0, 6 m). Elemental variants swap in
  a `WeaponGlow` or `ShieldNoise` material and equip sounds (`sfx_weapons_nature_enable` ...).
- **On the ground** every item has a particle system on its root on `item_particle` (the dropped-item sparkle).

## 7. The dropped weapon

- Root: `ZNetView`, `ZSyncTransform`, `ItemDrop`, `Rigidbody` (mass 1, drag 0, angular drag 0.05, continuous
  collision detection), the `item_particle` system; a few add `Floating`.
- Colliders sit on the mesh node inside `attach` (a `BoxCollider` fitted to the mesh on about 80 % of weapons; a convex
  `MeshCollider` on crossbows, sledges and some maces and one-handed axes; spheres on some bombs).
  `VisEquipment.CleanupInstance` disables them in the hand.
- The dropped weapon is the held model lying where the root puts it; weapons are not scaled up on the ground (unlike
  materials, see `items.md`).

## 8. Weapons creatures hold

Creature weapons are items too (`Characters/<creature>/...`, 21 designs after dropping attack copies that share a
model). Most reuse a player weapon's mesh at a larger `attach` scale, in the creature's material: skeleton_sword is
SwordBronze's mesh at 1.17x (1.07 m), draugr_sword SwordIron's at 1.17x (1.48 m), draugr_axe AxeIron's at 1.29x
(1.13 m), skeleton_mace MaceIron's at 1.33x (1.37 m); GoblinClub and GoblinSpear are the Club and SpearFlint at 1x.
Giants get their own: JotunWarriorSword2h 2.75 m, 664 triangles; the troll's log 6.3 m, 64 triangles. A weapon for a
game creature is the player-sized design scaled in its `attach`, not a new, finer model.

## 9. Checklist

- Grip through the origin along Z, the right side forward (table in section 1); spears head down -Z, atgeirs tilted
  about 22 degrees to +X, bows leaning about 14 degrees with the string on -X.
- Length and fist position inside the kind's range; triangles near the kind's median, at most the tier's p75.
- 64 or 128 px, 55 to 75 px per metre; one material; albedo, normal, metal map.
- Flat metal with a painted light edge line, grained wood, painted wraps; few colours; a shallow normal map.
- `attach/equiped/trail` with `base` and `tip` along the striking part; `attach/UpgraderGlow` copied from a game
  weapon; the kind's hit, block and swing effects.
- Line it up against the references on one sheet (`python codex/measure/items_render.py` then `items_sheets.py`).

## References

- Swords: `GameElements/Items/weapons/SwordBronze.prefab`, `SwordIron.prefab`, `SwordSilver.prefab`,
  `SwordBlackmetal.prefab`, `SwordMistwalker.prefab`, `SwordNiedhogg.prefab`
- Axes: `AxeFlint.prefab`, `AxeBronze.prefab`, `AxeIron.prefab`, `AxeBlackMetal.prefab`, `AxeJotunBane.prefab`;
  `Battleaxe.prefab`, `BattleaxeCrystal.prefab`, `BattleaxeBlackmetal.prefab`, `BattleaxeSkullSplittur.prefab`;
  `AxeBerzerkr.prefab`
- Blunt: `Club.prefab`, `MaceBronze.prefab`, `MaceIron.prefab`, `MaceSilver.prefab`, `MaceNeedle.prefab`,
  `MaceEldner.prefab`; `SledgeStagbreaker.prefab`, `SledgeIron.prefab`, `SledgeDemolisher.prefab`
- Knives: `KnifeFlint.prefab`, `KnifeCopper.prefab`, `KnifeChitin.prefab`, `KnifeSilver.prefab`,
  `KnifeBlackMetal.prefab`
- Polearms: `SpearFlint.prefab`, `SpearBronze.prefab`, `SpearElderbark.prefab`, `SpearWolfFang.prefab`,
  `SpearCarapace.prefab`, `SpearSplitner.prefab`; `AtgeirBronze.prefab`, `AtgeirIron.prefab`,
  `AtgeirBlackmetal.prefab`, `AtgeirHimminAfl.prefab`
- Two-handed swords: `THSwordKrom.prefab`, `THSwordSlayer.prefab`, `THSwordGold.prefab`
- Ranged: `Bow.prefab`, `BowFineWood.prefab`, `BowHuntsman.prefab`, `BowDraugrFang.prefab`, `BowSpineSnap.prefab`,
  `BowAshlands.prefab`; `CrossbowArbalest.prefab`, `CrossbowRipper.prefab`; arrows `ArrowFlint.prefab`,
  `ArrowIron.prefab`, `ArrowNeedle.prefab`, `ArrowCarapace.prefab`, `ArrowCharred.prefab`; bolts `BoltIron.prefab`,
  `BoltBlackmetal.prefab`, `BoltBone.prefab`, `BoltCarapace.prefab`
- Magic and other: `StaffFireball.prefab`, `StaffIceShards.prefab`, `StaffShield.prefab`, `StaffRedTroll.prefab`,
  `StaffGreenRoots.prefab`; `FistFenrirClaw.prefab`, `FistBjornUndeadClaw.prefab`, `FistBjornClaw.prefab`;
  `BombOoze.prefab`, `BombBile.prefab`, `BombSmoke.prefab`, `BombBlob_Poison.prefab`
- Creature weapons: `Characters/Skeleton/weapons/skeleton_sword.prefab`, `skeleton_mace.prefab`,
  `Characters/Draugr/weapons/draugr_axe.prefab`, `draugr_sword.prefab`, `Characters/Goblin/misc/GoblinSword.prefab`,
  `GoblinClub.prefab`

All prefab paths without a folder are under `GameElements/Items/weapons/`.

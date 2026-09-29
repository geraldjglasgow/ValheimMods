# Creatures

Every enemy, animal, boss, NPC and the player, measured from the reference export: what a creature prefab is made of,
how big, how many triangles and texture pixels, how it is painted and shaded, and how the game makes variants and star
levels out of one model. How it is rigged and animated is in `rigs/` (start at `rigs/README.md`).

Measured on 2026-09-29 by `measure/creatures.py` over **184 prefabs** under `Characters/` whose root carries `Player`,
`Humanoid`, `Character`, `Trader`, `Odin`, `Valkyrie`, `Raven`, `Fish`, `Leviathan` or `RandomFlyingBird`; 95 of them
are distinct models with a mesh, the rest draw the same main mesh as another (a texture, colour or scale swap, marked
`variant_of` in `data/creatures.json` and left out of the numbers below). Sizes are in the pose saved in the prefab (the
bind pose: arms out in a T- or A-pose for the bipeds, so their width is the arm span), metres, at the prefab's own
scale. Renders:
`codex/out/creatures/` (`grid_<kind>.png`, `lineup_<kind>.png` at one scale beside a 1.8 m mark, `tex_*.png` for the
albedos), made by `creatures_render.py`, `creatures_sheet.py` and `creatures_colour.py`.

## Categories and budgets

LOD0 totals over the renderers a creature draws; texture and texel density of its main renderer (the one
`LevelEffects.m_mainRender` or `VisEquipment.m_bodyModel` names, else the largest).

| Key | Models | Triangles (LOD0) | Albedo px | Texel density px/m | Height m | Bones |
| --- | --- | --- | --- | --- | --- | --- |
| `creature.critter` | 15 | 136–2,164 (median 242) | 32–128 (64) | 20–90 (51) | 0.32–1.93 (0.64) | 0–30 (0) |
| `creature.humanoid_small` | 7 | 1,180–6,708 (2,342) | 32–256 (128) | 5.5–116 (62) | 1.18–1.56 (1.44) | 22–63 (53) |
| `creature.humanoid` | 13 | 806–6,708 (2,910) | 128–512 (256) | 39–86 (59) | 1.91–2.87 (2.17) | 21–61 (33) |
| `creature.humanoid_large` | 14 | 1,200–15,684 (6,734) | 128–512 (256) | 23–79 (39) | 3.07–23.0 (5.3) | 22–126 (57) |
| `creature.quadruped_small` | 2 | 2,210–2,238 | 64–128 | 42–49 | 0.89–0.98 | 30–34 |
| `creature.quadruped` | 9 | 1,754–6,250 (2,966) | 64–512 (256) | 30–100 (58) | 0.57–2.61 (1.79) | 23–61 (33) |
| `creature.quadruped_large` | 6 | 2,580–7,438 (4,189) | 256 | 29–52 (38) | 2.51–4.29 (3.48) | 31–66 (45) |
| `creature.flyer` | 6 | 812–17,000 (3,739) | 64–512 (320) | 25–78 (40) | 0.10–10.5 (3.0) | 13–68 (32) |
| `creature.swimmer` | 5 | 322–21,744 (9,234) | 128–1,024 (512) | 3–45 (26) | 0.54–45 (5.1) | 0–20 (8) |
| `creature.amorphous` | 4 | 204–784 (510) | 128–256 | 30–70 (38) | 0.96–6.77 (2.3) | 2–8 |
| `creature.boss` | 10 | 10,108–22,478 (15,841) | 256–1,024 (512) | 10–44 (26) | 5.6–60 (11.5) | 28–70 (53) |
| `creature.npc` | 7 | 1,126–6,316 (4,032) | 128–512 (256) | 29–71 (43) | 1.21–4.14 (1.67) | 50–69 (51) |

Heights in the bind pose: a bat is 0.10 m tall with its wings flat, a T-posed troll 7.5 m where its capsule is 6.6 m.

The game's examples (triangles / albedo px / height), all under `Characters/`:

- **Critters.** `Hare/Hare.prefab` 2,164 / 128 / 1.49 m (ears up); `Chicken/Hen.prefab` 848 / 64 / 0.82 m;
  `animals/birds/Crow.prefab` 280 / 64, six bones; fish `animals/fishes/Fish1.prefab` 136 / 128 / 0.46 m, no bones at
  all (the fish swim by moving their transform). Seagal and AshCrow are the Crow's mesh on materials of their own.
- **Small bipeds.** `Goblin/Goblin.prefab` 1,676 / 128 / 1.41 m; `GoblinShaman/GoblinShaman.prefab` 2,342 / 128;
  `Dverger/Dverger.prefab` 2,508 / 128 / 1.55 m; `Surtling/Surtling.prefab` 6,708 / **32** / 1.44 m (a palette texture,
  see Paint); `Neck/Neck.prefab` 1,444 / 64 / 1.18 m.
- **Player-sized bipeds.** `Player/Player.prefab` 1,514 / 256 / 1.91 m; `GreyDwarf/Greydwarf.prefab` 1,308 / 128 /
  1.95 m; `Draugr/Draugr.prefab` 2,910 / 256 / 2.17 m; `Skeleton/Skeleton.prefab` 4,077 / 128 / 1.99 m;
  `TheCharred/Charred_Melee.prefab` 4,629 / 256 / 2.63 m; `Fenring/Fenring_Cultist.prefab` 3,236 / 128 / 2.76 m.
- **Large bipeds.** `Troll/Troll.prefab` 6,206 / 256 / 7.5 m; `GoblinBrute/GoblinBrute.prefab` 4,350 / 256 / 3.94 m;
  `Jotnar/JotunWarrior.prefab` 15,684 / 256 / 3.63 m (10,696 of it armour); `StoneGolem/StoneGolem.prefab` 1,200 / 512 /
  4.54 m; `Fenring/Fenring.prefab` 3,348 / 128 / 3.45 m.
- **Legged animals.** `Boar/Boar.prefab` 2,238 / 128 / 0.98 m; `Wolf/Wolf.prefab` 1,962 / 256 / 1.40 m;
  `Deer/Deer.prefab` 1,754 / 512 / 1.79 m; `Seeker/Seeker.prefab` 5,353 / 256; `Asksvin/Asksvin.prefab` 4,764 / 128;
  `Lox/Lox.prefab` 3,350 / 256 / 4.29 m; `moose/Moose.prefab` 4,576 / 256 / 3.44 m; `Bjorn/Bjorn.prefab` 4,226 / 256.
- **Flyers.** `Bat/Bat.prefab` 812 / 64; `Deathsquito/Deathsquito.prefab` 3,880 / 128; `Hatchling/Hatchling.prefab`
  2,830 / 512; `Volture/Volture.prefab` 3,598 / 64; `Gjall/Gjall.prefab` 9,160 / 512 / 10.5 m.
- **Swimmers.** `Serpent/Serpent.prefab` 4,872 / 512, 30 m long; `BonemawSerpent/BonemawSerpent.prefab` 10,433 / 512;
  `Leech/Leech.prefab` 322 / 128, 4.6 m long.
- **Amorphous.** `Blob/Blob.prefab` 204 / 128, two bones; `Greydwarf_king/TentaRoot.prefab` 348 / 256, a chain of 8;
  `Kvastur/BogWitchKvastur.prefab` 784 / 128 (the Bog Witch's walking broom, five bones).
- **Bosses.** `Eikthyr/Eikthyr.prefab` 13,558 / 256 (9,408 of it chains); `Greydwarf_king/gd_king.prefab` 14,759 / 512 /
  13.3 m; `Bonemass/Bonemass.prefab` 18,340 / 256; `Dragon/Dragon.prefab` 14,344 / 512; `GoblinKing/GoblinKing.prefab`
  13,338 / 512; `Fader/Fader.prefab` 18,852 / 1,024 (the only 1,024 px creature).
- **NPCs.** `TraderHaldor/Haldor.prefab` 2,148 / 128 / 1.67 m; `Hildir/Hildir.prefab` 4,235 / 128;
  `BogWitch/BogWitch.prefab` 6,080 / 256 in eleven parts; `Odin/odin.prefab` 1,126 / 128 / 2.21 m.

**What to aim for.** A new creature takes its category's median and stays inside the range: a player-sized biped
2,000–4,000 triangles on one 128 or 256 px atlas at 55–75 px/m; a wolf-sized animal 2,000–3,000 on 128–256 px; a large
animal or biped 3,500–7,000 on 256 px at 30–40 px/m; a boss 10,000–20,000 on 512 px. Texel density falls as creatures
grow: the game keeps one atlas per creature and lets a troll's texels be twice the size of a greydwarf's (23 against
49 px/m). The
spine greataxe's lesson (workshop README) holds for creatures too: a model several times over the budget, with detail
smaller than a texel, reads as another game's art.

## What a creature prefab is

Every creature the game spawns has the same shape (the workshop's creatures must too; `rigs/README.md` has the parts
the code reads by name):

```
<Name>                    CapsuleCollider (the body), Rigidbody, Humanoid | Character, ZNetView, ZSyncTransform,
                          ZSyncAnimation, MonsterAI | AnimalAI, CharacterDrop, FootStep, [VisEquipment], [Tameable,
                          Procreation, Growup], [RandomAnimation], [Tail]
  EyePos                  the eye point AI and aiming use (Greydwarf 1.54 m, Draugr 1.78 m, Goblin 1.32 m)
  Visual                  Animator, CharacterAnimEvent, LODGroup, LevelEffects
    Armature              the skeleton (armature scale 100 for most; 50 Greydwarf, 130 Troll, 300 Dragon)
    <renderers>           SkinnedMeshRenderers, one per part
  [Attack collider ...]   extra colliders some creatures keep
```

- **Almost every monster is a `Humanoid`**, whatever its shape: the Wolf, Lox, Serpent, Blob and Bat carry `Humanoid`
  because it holds the attack items (`m_defaultItems`, `m_randomWeapon`, `m_randomSets` ...). Only passive animals
  (Deer, Hare, Seal, Goblin_Gem) are plain `Character` with `AnimalAI`.
- **The capsule** is upright (`m_Direction` y) for 69 of the 78 distinct models that have one: radius 0.4 m and height
  1.8–1.85 m for player-sized bipeds (Player 0.49 × 1.85, Greydwarf 0.40 × 1.80, Draugr and Skeleton 0.40 × 1.85), 0.4 ×
  1.45 for the small ones, 1.0 × 6.6 for the Troll. Long animals lie it along z: Asksvin 1.08 × 3.0, Bjorn 1.32 × 3.5,
  Moose 1.04 × 4.43, Seal 0.88 × 4.24, Leech 0.3 × 1.5. The capsule, not the mesh, is what collides and what the player
  hits; keep the mesh's torso inside it.
- **One LOD.** 79 of 95 distinct models have a LODGroup with a single level, culled at 3.7–59 % of screen height (median
  7.6 %). Only six have two levels (Abomination, Barka, Asksvin and its hatchling, both Leviathans). Build LOD0 only,
  on the category's budget, and give it a LODGroup that culls at about 7 %.
- **Death is a separate prefab.** `m_deathEffects` spawns a ragdoll prefab (plus vfx and sfx): the Greydwarf's
  `GreyDwarf/fx/Greydwarf_ragdoll.prefab` is the body mesh again on its own physics skeleton of 11 Rigidbodies and 10
  CharacterJoints (hips and chest boxes, capsule limbs, a sphere head; the ragdoll wizard's layout), a `Ragdoll`
  component (`m_ttl` 2 s, `m_float`, `m_dropItems`) and its own LODGroup. Its bones need not match the live rig (the
  Greydwarf's ragdoll bones are `mixamorig:*`). 43 of 95 models have one; skeletons, the Charred, bosses and blobs play
  a death clip or burst into effects instead. The ragdoll's `m_mainModel` receives the dead creature's star-level
  `_Hue`, `_Saturation`, `_Value`.
- **Eyes** are often separate: the Greydwarf's are two 2-triangle quads on the built-in Standard shader with emission
  and the eye colour is the kind: `eye_blue` on the Greydwarf, `eye_red` on the Elite, `eye_yellow` on the Greyling,
  `eye_shaman` on the Shaman; the Draugr has `eye_l`, `eye_r` renderers; the Charred's are an 8-triangle `Eyes` part. 45
  of 87 Creature-shader materials carry an `_EmissionMap`, where glowing eyes, runes and embers are painted.

## Silhouette and proportions

From the rigs' rest positions and the humanoid avatars' bone maps (`data/rigs.json`, `body`; fractions of height):

| Rig | Height m | Hips | Head bone | Shoulders wide | Arm (shoulder to wrist) | Leg (hip to ankle) | Palm |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Player | 1.91 | 0.48 | 0.88 | 0.23 | 0.30 | 0.47 | 0.060 |
| Greydwarf | 1.94 | ground | 0.83 | 0.22 | **0.48** | 0.58 | 0.070 |
| Goblin | 1.39 | ground | 0.87 | 0.29 | 0.43 | 0.43 | 0.058 |
| Dverger | 1.55 | 0.36 | 0.80 | 0.32 | 0.41 | **0.34** | 0.062 |
| Charred | 2.63 | 0.54 | 0.92 | 0.24 | 0.30 | 0.50 | 0.063 |
| Fenring | 3.44 | 0.50 | 0.89 | 0.28 | 0.36 | 0.51 | 0.090 |
| GoblinBrute | 3.93 | 0.49 | 0.94 | 0.27 | 0.42 | 0.46 | **0.101** |
| Troll | 7.47 | ground | 0.80 | **0.42** | 0.42 | **0.38** | 0.086 |
| StoneGolem | 4.53 | 0.37 | 0.86 | **0.45** | **0.51** | **0.34** | – |
| Bonemass | 7.74 | ground | 0.68 | **0.65** | 0.57 | 0.52 | 0.115 |

Read with the renders (`out/creatures/lineup_*.png`):

- **Monsters exaggerate one or two proportions against the player's.** The Greydwarf's arms are half its height, its
  hands reach its knees, its knees stay bent and its feet splay into three toes. The Troll, Stone Golem and Bonemass
  have shoulders 40–65 % of their height wide, short legs and huge hands (palm 0.09–0.12 of height against the player's
  0.06). The Dverger has a third of its height in its legs. A new creature picks its exaggeration and keeps the rest
  plain.
- **Big simple masses, few parts.** The Troll is one body mesh plus hair cards; the Wolf and Deer are one mesh each, the
  Boar one plus its tusks (five tusk meshes in the prefab, one shown, others switched on per star, the Deer's antlers
  likewise); the Greydwarf is a body and a stone on its back. Only bosses, the Jotnar and NPCs split into many parts
  (the BogWitch has eleven, FrozenKing_0 twelve).
- **Heads are big and readable.** Head bones sit at 0.80–0.94 of height (Bonemass 0.68, hunched); faces carry few, large
  features (the Troll's brow, nose and ears have their own bones; the Greydwarf's face is a bark knot with two glowing
  quads).
- **Hands:** the Greydwarf has three two-joint fingers and no thumb (`l_index`, `l_middle`, `l_pinky`), the Troll three
  three-joint fingers and a thumb, the player, Skeleton, Goblin and Dverger five; the Draugr, Stone Golem and Surtling
  have no finger bones at all (mitten hands).
- **Hips at the ground:** the Greydwarf (`root`), Troll (`Root`), Goblin (`Root`), Draugr (`Hips`), the Elder, Odin and
  Bonemass map
  Unity's Hips to a bone at the feet, not the pelvis. Their clips still play; a new rig should put Hips at the pelvis
  like the player's (0.91 m), which retargets more cleanly.

## How creatures are painted

Seen on the albedos (`out/creatures/tex_humanoid.png`, `tex_animals.png`, `tex_bosses.png`, point filtered) and the
renders. Numbers from `creatures_colour.py` (sRGB, main albedo, alpha-cut pixels left out; `colour` per creature in the
data). Other pages cover the families in depth (`paint.md`, `palette.md`); what is particular to creatures:

- **Hand-painted atlases, light baked in, no photo texture.** Every creature is one packed atlas of its unwrapped
  parts with soft painted shading: lighter on top of forms, darker in creases, big soft blotches of a second and third
  colour. Unused atlas space is left black or filled with a grey checker from the painting tool (48 % of the
  Greydwarf's 128 px atlas is black, so its mean colour [46, 39, 26] is not its look; its light tone is
  [122, 101, 68]).
- **Bark** (Greydwarf, Greyling, the Elder, Abomination): warm orange-brown bark painted as long strokes along each limb
  with dark crevice lines, patches of green moss, a pale grey stone. Moss and lichen are painted, not modelled.
- **Fur** (Troll, Wolf, Lox, Moose, Bjorn): painted as soft directional strokes on the body (the Troll blue-grey with
  near-black creases, contrast 146 of 255); long hair, manes and shaggy coats are **alpha-cut cards** on the same atlas
  (the Troll's yellow hair strands, the Lox's orange-brown clumps, the Moose's pale fringes; `_ALPHATEST_ON`, cutoff
  0.3–0.7, `_Cull` off). The cards take a real share of the atlas: 42 % of the Moose's albedo is cut out, 15 % of the
  Troll's, 19 % of the Bog Witch's.
- **Skin** (Goblin, Dverger, Player, Neck, Fenring face): smooth painted gradients with muscle forms shaded in, very
  little texture noise (Goblin olive [119, 104, 51] middle tone, contrast 57; Dverger flat teal). Faces are painted,
  not modelled beyond the big forms.
- **Bone** (Skeleton, Bonemass, Charred skulls): cream-beige bones each painted with its own shading on a dark brown
  ground (Skeleton light [159, 138, 116], dark [74, 36, 14]); Bonemass is smoky beige and brown at 256 px.
- **Rot, moss and rags** (Draugr): mottled green-brown camouflage blotches, rags with **alpha-cut holes and ragged
  edges** (6 % of its atlas is cut out).
- **Chitin and insects** (Seeker, Tick, Deathsquito, the Queen): mottled orange-brown with dark speckle (Seeker
  saturation 0.62, against 0.2–0.4 for most creatures); the Deathsquito's shell is green segments with orange wing
  membranes; the Tick is pale flesh with painted purple veins.
- **Stone and ice** (Stone Golem, Barka, the Rime Giant here): grey stone with white snow and crystal patches as
  flat shapes (Stone Golem 512 px).
- **Fire** (Surtling): a **32 px colour-swatch palette** (flat squares of browns, tans, greys, olive, yellow, red) with
  an emission colour of 73 in each channel. Flat-coloured creatures can use a palette atlas: the mesh's UVs sit inside
  one swatch per part.
- **Greyscale bodies tinted per kind.** The Wolf (`Wolf Pixel.png`, saturation 0.02), Fenring, Ulv and Hatchling
  paint in greys; the colour comes from the material's `_Color` and the star levels' HSV shift. A creature meant to come
  in several colours is painted this way.
- **Pixels show.** Albedos of 64 px and less are point filtered blocks at a player's distance (the Neck's 64 px shows
  its pixels on a 1.2 m body, the Bat's and Volture's 64 px on 1.4 and 7.8 m wingspans). Paint at the target size; do
  not paint at 2048 and scale down.
- **Biome belonging is colour first** (middle tones of the main albedo, sRGB). Meadows and Black Forest are warm
  browns and greens (Boar [104, 82, 64], Greydwarf bark and moss; the Troll's blue-grey is the exception); Swamp is
  olive, mud and dark red (Draugr [79, 81, 57], Blob [94, 85, 58], Leech [143, 48, 51]); Mountain is greys and white,
  colour left to the tint (Wolf [131, 131, 131] saturation 0.02, Fenring, Stone Golem [137, 136, 124], Hatchling);
  Plains is olive and ochre (Goblin [119, 104, 51], Deathsquito [39, 49, 31], Lox [65, 44, 10] fur); Mistlands is teal
  skin and purple-brown chitin (Dverger, Seeker Soldier [86, 57, 57], Gjall [125, 76, 61]); Ashlands is charcoal, dark
  teal and dull red with glow (Charred [34, 49, 53], Morgen [90, 60, 49], Volture [69, 73, 67]); Deep North is pale
  ice blue and blue-grey (Barka [112, 123, 124], JotunWarrior [85, 106, 115]). `palette.md` has the sampled colours.

## Materials and shader settings

Over the 87 materials of the distinct models on the game's `Creature` shader (`shaders.md` has the shader):

- **Slots:** `_MainTex` 87, `_BumpMap` 85, `_EmissionMap` 45, `_MetallicGlossMap` 31. Normal maps are on nearly every
  creature at `_BumpScale` 1 (0.46–2.79).
- **Smoothness is low:** `_Glossiness` median 0.19 (quartiles 0.12–0.30); `_Metallic` 0 on three-quarters of them,
  used only where a creature wears metal (Lox saddle 1.0, the Jotnar armour).
- **Most are two-sided:** `_Cull` 0 (off) on 60 of the 102 materials that set it, back-face culling on 42.
  `_TWOSIDEDNORMALS_ON` on 29. Cards and thin parts (hair, rags, wings, fins) need it.
- **Alpha cut-out:** `_ALPHATEST_ON` on 32, `_Cutoff` 0.11–1.0 (median 0.5).
- **Colour adjust** `_Hue` (−0.5 to 0.5), `_Saturation` (−1 to 1), `_Value` (−1 to 1) are 0 in every creature material;
  the game drives them at run time (Recolours below). **Styles** (`_UseStyles`, `_StyleTex`) are used by no creature.
  **Noise glow** only by the Fader. **Snow cover** `_SnowCover` is 0 in the materials; `VisEquipment` sets it at run
  time on creatures that have one.
- **Emission** is HDR: eyes and runes 2–10 (Draugr [2.1, 9.5, 7.4]), fire and magic up to 73 (Surtling), the Bonemaw
  Serpent's glow 28.5.
- **One material per creature** is the rule (humanoid median 1, maximum 2); bosses use 1–3; the JotunWarrior uses 4
  and GoblinBruteBros 6 where they wear armour sets.

## Recolours and variants: how the game makes kinds out of one model

Measured in `families` of `data/creatures.json` (each creature against the first of its rig):

1. **Star levels (LevelEffects).** Every level above the first applies one setup: `transform.localScale` of Visual,
   and `_Hue`, `_Saturation`, `_Value` (and optionally `_EmissionColor`) on a copy of **material 0 of the main
   renderer only**; it can also switch on an object and switch off a base object. The game's setups (level 2 | level 3):

   | Creature | ★ | ★★ |
   | --- | --- | --- |
   | Greydwarf, Greyling | ×1.1, hue −0.06, sat +0.1, val +0.05 | ×1.2, hue −0.5 |
   | Greydwarf_Shaman | ×1.1, hue +0.17 | ×1.2, hue +0.42 |
   | Greydwarf_Elite | ×1.1, hue −0.05 | ×1.2, hue −0.11 |
   | Skeleton (all kinds) | ×1.1, hue −0.03, sat +0.3 | ×1.2, hue −0.1, sat +0.3, val −0.1 |
   | Draugr, Draugr_Ranged | ×1.0, hue +0.27 | ×1.1, hue −0.25 |
   | Draugr_Elite | ×1.1, hue −0.1 | ×1.2, hue −0.2 |
   | Troll | ×1.0, hue −0.14, sat +0.1 | ×1.0, hue +0.44, sat +0.2 |
   | Goblin | ×1.1, hue −0.05 | ×1.2, hue −0.15 |
   | GoblinBrute | ×**0.75**, hue −0.1, sat −0.1 | ×0.8, hue −0.18 |
   | Wolf, Ulv | ×1.1, sat +0.1, val −0.03 | ×1.2, hue −0.1, sat +0.2, val −0.05 |
   | Boar | ×1.1, hue +0.05, sat −0.1, + `Fangs 005` | ×1.2, hue +0.09, sat −0.5, + `Fangs 006` |
   | Deer | ×1.1, sat −0.15, + `Antlers 04` | ×1.2, sat −0.4, + `Antlers 05` |
   | Moose | sat +0.07, + `HornsSmall` | sat +0.1, + `Horns` |
   | Seal | ×1.1, sat −0.15, + `Horns` | ×1.2, sat −0.4, + `Saber` |
   | Bjorn | ×1.3, emission [12.6, 0.9, 0], + `arrow` | ×1.5, hue +0.11, sat −0.63 |
   | Neck | ×1.2, hue +0.24 | ×1.3, hue +0.42 |
   | Leech | ×1.1, hue +0.23, sat +0.26 | ×1.2, hue −0.14, sat +0.47 |
   | Asksvin | ×1.1, hue −0.48, sat −1.0 | ×1.2, hue +0.5 |
   | Seeker | emission [0.04, 0, 0.16], hue −0.28 | emission [0.31, 0, 0], hue −0.1 |
   | Dverger (all kinds) | emission [0, 4.0, 3.3], hue +0.1 | emission [12.0, 0, 8.8], sat −1 |
   | Surtling | ×1.1, + `flames_world_BLUE` | ×1.2, + `flames_world_Purple` |
   | Charred (all kinds) | ×1.1, + `1-Star FX` | ×1.2, + `2-Star FX` |
   | Jotnar | ×1.1 | ×1.2 |
   | Hare | ×1.1, hue −0.14, val +0.13 | ×1.4, hue −0.5, val +0.4 |

   Scale steps of ×1.1 and ×1.2 with a hue swing of 0.05–0.5 are the norm. Bosses, NPCs, blobs, the Lox, Fenrings,
   Ghosts and most flyers have none. **Consequence for a new creature:** the part that should change colour with the
   stars must be material 0 of the renderer `m_mainRender` names; gear on other materials keeps its colour.
2. **Tint swaps (`_Color`) on one texture.** The Skeleton's kinds share `Skeleton_d.tga` and differ only in `_Color`:
   `Skeleton_Mountains` [0, 0.79, 1.0] (cyan), `Skeleton_Poison` [0.61, 1.0, 0.24] (green), plus a ×1.26 scale. The
   Fenring and Ulv tint their grey albedo by 0.68.
3. **Texture swaps.** `Greydwarf_Frozen` is the Greydwarf with `greydrawrf_diffuse_frozen.png`; `Deer_White` a 32 px
   white texture; `Seagal` the Crow's mesh with `seagull_d.png`. `AshCrow` and `Leech_cave` keep their base's texture
   on a material of their own; `Bat_Swamp` is the Bat unchanged (it differs only in its stats).
4. **Extra meshes and scale.** `Greydwarf_Elite` is the Greydwarf body at ×1.57 with two more parts; Greyling the body
   without the back stone and with its own controller; the young (`Boar_piggy`, `Wolf_cub`, `Lox_Calf`, `Moose_calf`,
   `Seal_Pup`, `Asksvin_hatchling`) are the adult's rig at 0.3–0.7 of its height (Wolf_cub 0.50, Moose_calf 0.73,
   Asksvin_hatchling 0.28), some with their own mesh.
5. **Other meshes on the same rig.** Draugr, Draugr_Elite and Draugr_Ranged are three meshes (2,910, 2,503, 2,832
   triangles) on one 21-bone skeleton; Troll, TrollFrost and Troll_Summoned share the Troll's 57 bones; the Jotun
   warrior and witch share 59.
6. **Gear sets from items** (the flexible way, used by NPC-like humanoids). A `Humanoid`'s `m_defaultItems`,
   `m_randomArmor`, `m_randomSets` and `m_randomItems` hand it items at spawn; their `attach_skin` children are skinned
   to the creature's own bones: the Goblin's `GoblinHelmet`, `GoblinArmband`, `GoblinLoin`, `GoblinShoulders`,
   `GoblinLegband`; the Dverger's `DvergerSuitFire`, `DvergerSuitIce`, `DvergerHairMale_Redbeard`; the Charred's
   `Charred_Breastplate`, `Charred_Helmet`. The Dverger mage's three `m_randomSets` (Fire, Ice, Healer) each swap suit
   and staff. `rigs/README.md` has the rule that makes this work (the bone order).
7. **Objects switched on per level** (antlers, fangs, horns, flame effects), above.

The workshop's region masks (`recolour.md`) are the bridge: paint a new creature greyscale where it should tint, mark
each recolourable part as its own region, keep the star-tinted body on material 0, and make each kind a variant.

## Parts to model, and what not to

- **One body mesh** skinned to the rig, closed where the camera sees it; interior faces left out.
- **Separate meshes** only for parts that are switched, swapped or animated apart: eyes (for their emissive shader),
  a star-level extra (antlers, fangs, horns), removable gear, cards (hair, mane) when they need their own cut-out
  material. The game keeps them on the same material wherever it can (the BogWitch's eleven parts and the Greydwarf's
  body and back stone each share one material).
- **Cards for hair, fur fringes, feathers, rags, fins and wing membranes**, cut out with alpha, two-sided.
- **No modelled detail below two texels**: at 60 px/m that is 3 cm. Scales, fur strands, scars, veins and stitching are
  paint.
- **No facial rig** beyond a jaw, and the eyes as bones or quads. Only the Troll has brow, cheek, lip and ear bones.

## Checklist

- Category chosen from the table; triangles, texture size and texel density at its median; height and capsule from
  the reference creatures beside it in a lineup (`creatures_sheet.py lineup`).
- The prefab shape above: Visual child with Animator, CharacterAnimEvent, LODGroup (one level, cull about 7 %),
  LevelEffects naming the main renderer; EyePos at the eyes; capsule upright (or along z for a long animal).
- One atlas, painted with light baked in, big soft shapes, black or transparent where unused; normal map; smoothness
  about 0.2; two-sided and alpha-cut where cards are.
- Star-level setups written (×1.1 and ×1.2, a hue swing), the tinted body on material 0.
- A ragdoll prefab (11 bodies) or a death clip with a `Die` event, and a death effect.
- The rig and clips from `rigs/README.md`.

# Brief: `ecp_deathsquito_queen`

The Deathsquito Queen: the game's Deathsquito grown 2.5 times, gravid and crowned. Model and paint only for now (the
user's request of 2026-09-29: "just the model, fully coloured"); no rig, clips, effects, sounds or mod code yet.

## 1. What it is and where it lives

- Name: `ecp_deathsquito_queen`.
- What it is: a Deathsquito 2.5 times the size of the game's, with a swollen egg-heavy abdomen glowing blood red
  underneath, a crown of five amber spikes on its thorax, banded legs, a long drooping proboscis, a stinger and old
  notched wings.
- Mod: Elite Creatures Pack, if the user releases it (the workshop rule: it stays here until then).
- Biome: Plains, where the Deathsquitos fly.
- Tier: 4 (Plains).
- Who uses it: a creature, a rare hovering mini-boss over the Plains.

## 2. Category

- Category key: `creature.flyer`.
- Label and sample count: flyers: bats, insects, drakes, birds of prey, floating things; 6 distinct models.
- Triangles (min, p25, median, p75, max): 812, 3,022, 3,739, 7,840, 17,000.
- Texture px: 64, 80, 320, 512, 512.
- Texel density (px per metre): 24.5, 33.1, 39.9, 48.6, 77.9 (the Deathsquito is the 77.9).
- Longest side (m): height 0.1, 0.8, 3.02, 6.62, 10.5 (bind-pose height; the flyers are measured by wingspan too).

## 3. Game references

| Prefab (path under the reference export) | What to take from it |
| --- | --- |
| `Characters/Deathsquito/Deathsquito.prefab` | the creature it is the queen of: parts, layout, pose, paint, scale x 2.5 (measured below) |
| `Characters/Bat/Bat.prefab` | a small flyer's budget and wing plates |
| `Characters/Hatchling/Hatchling.prefab` | a mid-sized flyer, 6.5 m span, painted wing membranes |
| `Characters/Volture/Volture.prefab` | a big flyer's texel size (64 px on 7.8 m) |
| `Characters/Gjall/Gjall.prefab` | a big floating creature's paint and 512 px budget |

The Deathsquito, measured from the reference export (`Deathsquito/model/Cube.asset`, posed by the prefab; Blender
axes, front -Y): 1.80 m wingspan, 1.19 m from proboscis tip to abdomen end, mesh 0.25 to 0.64 m above its root; 3,880
triangles, of which 1,920 are its two eye spheres; 128 px albedo at 78 px/m, `Deathsquito_mat` on `Custom/Creature`,
gloss 0.535, two-sided, an emission map for the belly and eyes.

| Deathsquito part | Centre (m) | Size (m) | Triangles | x 2.5 |
| --- | --- | --- | --- | --- |
| thorax (two halves) | (0, -0.02, 0.51) | 0.17 x 0.22 x 0.19 | 348 | 0.42 x 0.56 x 0.47 |
| head (two halves) | (0, -0.17, 0.46) | 0.09 x 0.09 x 0.07 | 236 | 0.22 x 0.22 x 0.19 |
| eyes | (+-0.016, -0.17, 0.48) | 0.04 round | 2 x 960 | 0.10 |
| abdomen plates | (+-0.05, 0.33, 0.49) | 0.10 x 0.49 x 0.10 each half | 416 | 0.50 wide, 1.23 long |
| abdomen belly (glows) | (+-0.05, 0.31, 0.41) | 0.10 x 0.48 x 0.17 each half | 252 | |
| wings | (+-0.47, 0.19, 0.60) | 0.86 x 0.29 x 0.09 | 2 x 56 | 2.16 x 0.73 |
| proboscis (two halves) | (0, -0.42, 0.46) | 0.41 long | 16 | 1.03 long |
| antennae | (+-0.08, -0.26, 0.51) | 0.15 x 0.11 x 0.08 | 24 | |
| legs, three a side | coxae under the thorax, femurs rising to a high knee, tibiae down, tarsi out to x +-0.37 (one bone chain a side) | 0.02 thick | about 400 | knee at (0.35, 1.44), feet at x +-0.9 |

Its paint (`Deathsquito_d.png`, sRGB): each part one lozenge island, lighter in the middle and dark at its rim; green
chitin #151813 / #283520 / #384e2c (10/50/90 %, saturation 0.38, hue 98); the belly's glowing stripes #ac2f0f /
#e33e1b / #fb7352 between near-black bands; wings orange-brown #4f361d / #72401d / #c3431f with red speckle and olive
washes, a dark ragged rim; eyes acid green #87b968 at the centre.

## 4. Silhouette

- A huge mosquito hanging in the air: 4.5 m of broad old wings, a heavy sagging abdomen glowing red from below, legs
  dangling in spider knees, a needle a metre long, and a crown of pale spikes on its hump. At 10 m it reads as
  "Deathsquito, but a queen" from the size, the swollen glowing abdomen and the crown.

## 5. Parts

- Origin and orientation: the root on the ground under the thorax, metres, Z up, facing -Y (Unity +Z); the body
  hovers where the Deathsquito's does at 2.5 times (legs' tips 0.62 m up, thorax centre about 1.3 m). The bind pose is
  the Deathsquito's: wings spread level with a little dihedral, legs hanging.

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| thorax | 0.46 x 0.63 x 0.52, humped | 8 % | chitin.chitin | primary | loft of 10-sided rings, faceted |
| head | 0.26 x 0.26 x 0.24 | 2 % | chitin.chitin | primary | |
| eyes | 0.12 x 0.15 x 0.14 each | 1 % | glow green | eyes | beads high on the head's front as the game's are, a little bigger at scale |
| proboscis and palps | 0.85 m needle, 0.06 at the root; palps 0.3 m | 1 % | chitin.chitin (dark) | primary | droops 0.1 m |
| antennae | 0.45 m | 1 % | chitin.chitin (dark) | primary | swept forward and up like the Deathsquito's |
| crown | five spikes 0.22 to 0.36 m on the thorax's front, bases 0.05 m | 2 % | bone.horn (amber) | trim | curled back; the queen's mark |
| abdomen | 0.66 x 1.18 x 0.72, six overlapping segments | 20 % | chitin.chitin plates, glow belly, near-black joints | primary, glow | the exaggerated proportion: 1.35 times the Deathsquito's girth at scale, sagging |
| stinger | 0.25 m | 0.5 % | bone.horn (amber) | trim | curled down from the abdomen's tip |
| wings | 2.14 x 0.88 each, 1.6 cm plate | 50 % | chitin.chitin in the wing's tones | secondary | a paddle swept back like the game's, one notch worn into the trailing edge; a 4.5 cm rim of green shell inset round it (the game's wings are edged green) |
| wing veins | the leading edge (costa, 6.4 to 2.6 cm thick) and one long vein behind it | 1 % | chitin.chitin | primary | modelled: veins painted a texel wide stepped across the 256 px atlas |
| legs | three a side on the game's leg chain at 2.5 (hip, coxa, high knee at 1.44 m, ankle, foot at x 0.8), 0.06 to 0.03 thick | 8 % | chitin.chitin, amber bands at each joint | primary, trim | banded like a real mosquito's |

## 6. Budget

- Triangles (target): 3,500 to 4,500 (the category's median; the Deathsquito's 3,880 with most of it in its eyes).
  Built: 3,715 (3,295 before the legs were re-laid on the game's leg chain with a coxa and a band more).
- `TEXTURE_SIZE`: 256 (the category's median is 320; the Deathsquito's 128 at 2.5 times would be 31 px/m).
- Texel density (target px per metre): 45 to 60 (between the category's p75 and the Deathsquito's 78, as texel
  density falls when a creature grows). Built: 38, the category's median; the pipeline's smart projection leaves
  two thirds of the atlas to margins, and the game's Deathsquito seen at 2.5 times shows the same texel size.
- `NORMAL_MAP` on / `AO_STRENGTH` 0.2 / `NORMAL_FROM_ALBEDO` 4.1 (chitin's k).

## 7. Materials

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| chitin.chitin | thorax, head, abdomen plates, legs, palps, wing rims and veins | `paint.make("chitin.chitin", tones=Deathsquito green, top=0.12, contrast=1.3)` | #1f2518 / #344428 / #56683a | the Deathsquito's green, a shade lighter (V50 0.22 against its 0.21); lighter on top |
| chitin.chitin (joints) | the belly's joints, needle, antennae | the same recipe, near-black green | #0c0e0a / #161a11 / #252b1b | the dark stripes across the glowing belly; on top the joints stay shell, a crease only, as on the game's |
| glow | the belly, below 100 degrees from the top | `paint.make("skin.flesh", tones=belly glow, pattern=None)` | #7a1a0a / #c8321a / #e85a30 | painted bright; in the mod the glow region becomes the emission map |
| glow (hot) | the belly's bottom, below 150 degrees | the same, lighter | #b02810 / #e84422 / #ff7a48 | the game's stripes are brightest along the bottom |
| chitin.chitin (wing) | wing membranes | `paint.make("chitin.chitin", tones=wing, patches=red 0.3 / olive 0.15, contrast=1.6, jitter_scale=0.6)` | #4a3a22 / #7a4c2a / #9c6a40 | soft salmon with red and olive washes, as the game's wing |
| bone.horn | crown, stinger, claws, knee and ankle bands | `paint.make("bone.horn", tones=amber, pattern=("bands", 0.07, 0.25), blotch_m=0.1)` | #5a4020 / #9a7a48 / #d4b878 | banded like a real mosquito's legs |
| glow green | eyes | `paint.make("crystal.crystal", tones=eye, hollows=0, edges=0)` | #4a9a2c / #88d058 / #c8f890 | the Deathsquito's eye colour, painted light since the bake has no emission |

Dressed in the mod as the Deathsquito's own material (`Custom/Creature`, gloss 0.535, two-sided) wearing the baked
textures (`GameMaterials.Dress`).

## 8. Paint regions

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| primary | chitin: body, legs, needle, antennae, joints, wing rims and veins | the kind's shell colour |
| secondary | wing membranes | wing colour |
| glow | the belly | the blood's glow colour |
| trim | crown, stinger, leg bands | the crown's colour |
| eyes | eyes | eye colour |

- Variants (`variants.json`): `pale` (a frost queen: blue-grey shell, pale wings, blue glow and eyes), `ember` (an
  Ashlands one: charcoal shell, rust wings, orange glow and crown), `venom` (a swamp one: olive shell and wings, acid
  green glow, yellow eyes).

## 9. Rig and animation (workshop preview, 2026-09-29)

- Route and source prefab: (b), built by `queen_rig.py`: an exact copy of the Deathsquito's own 17-bone skeleton
  (`Characters/Deathsquito/Deathsquito.prefab`, `workshop.gamerig`) scaled 2.5 and applied, so a mod can put the body on a
  copy of the Deathsquito with its Visual at 2.5 and the game's `deathsquito_animator` (buzzing idle, sting) plays it.
  Every part rides one bone whole (the build writes each part's bone on its faces, `queen_bones.py`): thorax and crown
  on `Thorax`; head, eyes, needle, palps and antennae on `Head`; abdomen and sting on `Abdomen`; each wing (with its
  veins) on `L.Wing` / `R.Wing`; each leg segment on its bone of the side's one chain, as the game's three legs a side
  share it (coxa `leg1`, femur rising to the high knee `leg2`, tibia `leg3`, tarsus and claw `leg4`; the legs were
  re-laid on that chain's joints at 2.5 for this).
- New bones (deforming; a mod adds them to the game creature by name, `BundlePrefabs.CreatureBody`): the front and hind
  legs' own chains `L.legF1`..`4`, `L.legH1`..`4` and the right side's, copies of the game chain's bones moved along her
  length to those legs' joints, so each leg swings on its own (the game's one chain a side carries the middle legs); and
  `Proboscis` under `Head` from the needle's root, so the needle droops and sways (`queen_sway.py`).
- Sockets (new, non-deforming): `Mouth` under `Proboscis` at the needle's tip (needles leave there; the dive and lunge
  strike with it: `m_attackOriginJoint`), `EggPoint` under `Abdomen` at its tip (the eggs).
- Moves: all seven of `MOVES.md` keyed in Blender (`queen_moves.py`, `queen_attacks.py`, `queen_ranged.py`: her state
  over time as pure maths; `queen_pose.py` turns it into the root's place and turn and rotations of the game's own
  bones: the wings about her long axis, `Head` and `Abdomen` about their joints, the leg chain for the tuck and dangle).
  The wings buzz a stroke a frame (15 a second) and motion blur makes the fan.
- In a mod: the game's idle and sting clips, the moves' extra poses (curl and pumps, head aim, the legs' intents) set in code
  after the animator each frame, the travel (dive line, whirl circle, rises) moved by the owner.

## 10. Effects

| When | Preview (`queen_fx.py`, `queen_hatch.py`) | In a mod (plan) |
| --- | --- | --- |
| a hit | a red flash at the struck point | the game's `vfx_deathsquito_hit`, scaled |
| egg lands, burst pieces crumble, needle strikes dirt | heath dust puffs | the game's dirt hit effects |
| egg throbs | the egg swells on each beat, 1 to 3 a second, a red light in it growing | the same in code, a light on the egg |
| egg bursts | five petals thrown out tumbling, crumbling away; the cup crumbling last; a game Deathsquito rising | the burst model's pieces (`ecp_queen_egg_burst`), the game's `fx_egg_splash`-style burst |
| her glow | belly and eyes emissive from the regions mask | the glow and eyes regions as `_EmissionMap` |

## 11. Sounds

All from the game's own recordings, trimmed, pitched and filtered as a mod can at run time: `queen_sfx.py` and
`SOUNDS.md` (17 cues: her buzz at 0.67 of the Deathsquito's wing loop, the dive's rush, the pierce, eggs, the
throb, the burst, needles from the arrow's hit, a beetle's growl with a
Seeker's screech for her alert). Nobody has listened to them yet.

## 12. Into the mod (plan only)

- Bundle name and mod folder: `ecp_deathsquito_queen`, Elite Creatures Pack, only when the user releases it.
- Game prefab to copy and build on: `Deathsquito`.
- Game material: the Deathsquito's `Deathsquito_mat` (`Custom/Creature`) dressed with the baked albedo and normal; the
  glow region's mask becomes its `_EmissionMap`.
- Colliders: the Deathsquito's capsule scaled 2.5.
- Icon, recipe, config: none yet.
- Multiplayer: a creature; the game's own sync (ZNetView, ZSyncTransform, ZSyncAnimation).

## 13. Checks

- [x] `.\build.ps1 -Asset ecp_deathsquito_queen -Lineup` builds (2 s); `out/preview.png` looked at, all four views.
      4.56 m span, 3.06 m from needle to sting, 1.21 m tall; 3,715 triangles; one 256 px atlas at 34 px/m.
- [x] Style check (`out/style_report.txt`): triangles, texture, density, size, whole albedo and the primary (shell) and
      secondary (wing) regions PASS. Outside the game's range, by design:
      - glow value, saturation and span HIGH: the belly is a glow, painted as bright as the Deathsquito's own glowing
        stripes (#e33e1b, V 0.87, S 0.86), and judged against flesh, the nearest family;
      - trim span HIGH, blotch LOW: amber horn banded along short spikes and joint rings, against the two pale horns
        the codex has;
      - eyes span, blotch LOW and local contrast HIGH: two small knobs of one bright green, judged against crystal.
- [x] Lineup (`out/lineup/`) and the review beside the game's Deathsquito (`review.py`: `out/review/turn.png`, `top`,
      `front`, `side`, `below` beside it at 2.5 times; `scale` beside it at 1 times and a 1.8 m player). What differed
      and was changed: the abdomen read as a red-striped barrel (glow up the flanks, wide dark joints all round), now
      the glow is on the belly only and the joints on top are creases, as on the game's; the wings were leaf-shaped,
      too green, then too red and busy, now paddles swept back in the game's soft salmon with red and olive washes and
      a green rim; painted veins a texel wide stepped across the atlas and were replaced by one modelled vein; the
      eyes were buried and dark, now two bright beads high on the head; the neck of the abdomen showed a dark cone.
- [x] Paint regions (`out/ecp_deathsquito_queen_regions.png`): primary 61 %, secondary 17 %, trim 15 %, glow 6 %,
      eyes 2 %; the variants sheet (`out/variants.png`) looks painted.
- [ ] In the game through DevBridge: not yet (model only, no rig, not in a bundle).

# Materials: hits, breaks, placing and footsteps

What each material sounds like in the game when it is struck, broken, placed or walked on, measured, and the layer
balance that makes a new impact read as that material. Roles come from `WearNTear` (pieces: `m_hitEffect`,
`m_destroyedEffect`), `Destructible`, `MineRock`/`MineRock5`, `TreeBase`/`TreeLog` (world objects), `Piece.m_placeEffect`,
shields' `m_blockEffect` and the player's `FootStep` list; the material comes from the sound's name (the pieces'
`WearNTear.m_materialType` is set per piece, but the sound prefabs are shared across materials by name). Played LUFS is
the in-game level before distance (see [archetypes.md](archetypes.md) for how every column is measured).

## The materials side by side

| material | struck (sound: length s, played LUFS, centroid Hz, flatness, low/mid/high) | broken | placed | walked on (walk / run / land, played LUFS) |
| --- | --- | --- | --- | --- |
| wood | `sfx_tree_hit` 1.38, -21.3, 512, 0.51, 0.86/0.10/0.05; `sfx_wood_hit` 1.19, -19.2, 307 | `sfx_wood_destroyed` 2.71, -14.2, 589, 0.52; `sfx_wood_break` 2.71, -14.2, 589 | `sfx_build_hammer_wood` 1.27, -21.4, 753, 0.50, 0.68/0.26/0.06 | -33.6 / -31.3 / -19.0, centroid 101 to 122 Hz |
| stone | `sfx_rock_hit` 0.78, -15.0, 846, 0.51, 0.58/0.35/0.07 | `sfx_rock_destroyed` 2.72, -16.9, 480, 0.54; world stone breaks 3.1 s, reaching 90 m | `sfx_build_hammer_stone` 1.26, -20.8, 335, 0.51, 0.86/0.12/0.03 | -33.4 / -35.4 / -20.9, centroid 104 to 130 Hz |
| metal | `sfx_metal_blocked` 0.88, -17.9, 2,618, 0.08; `sfx_metalbars_hit` 0.93, -17.7, 760, 0.40 | `sfx_metal_blocked` again; `sfx_metalbars_break` 2.20, -13.6, 1,066, 0.41 | `sfx_build_hammer_metal` 1.27, -22.3, 566, 0.42 | (metal floors: the default steps) |
| ice | `sfx_ice_hit` 1.28, -21.1, 502, 0.52, 0.81/0.15/0.03 | `sfx_ice_destroyed` 2.16, -16.8, 474, 0.54 | `sfx_build_hammer_ice` 0.86, -19.3, 818 (no piece uses it) | -35.1 / -33.5 / -34.7, centroid 629 to 1,204 Hz |
| crystal | (the ice hit) | `fx_crystal_destruction` 2.37, -18.2, 4,946, 0.43, 0.44/0.11/0.43 | `sfx_build_hammer_crystal` 0.86, -22.1, 818, 0.54 | - |
| bone | `sfx_skeleton_hit` 0.72, -18.2, 2,748, 0.41, 0.04/0.56/0.39 | `sfx_carrion_destroyed` 0.87, -10.2, 2,308, 0.44 | - | - |
| earth, grass | `sfx_bush_hit` 0.90, -27.2, 5,299, 0.51 (foliage) | `sfx_pickable_pick` 1.41, -30.1, 423 (foliage) | `sfx_build_hoe` 0.45, -24.9, 1,110; `sfx_build_cultivator` 0.75, -24.3, 3,043 | earth -41.9 / -37.4 / -27.5 (334 Hz at a run); grass -41.2 / -33.4 / -29.9 (walking 3.1 kHz) |
| snow, ash | snow walls: the snow run step, 0.65, -34.0, 1,013 | `fx_snow_wall_break` (the same step) | the snow shovel: `sfx_snow_hit_transient` 0.81, -13.7, 918 over `sfx_snow_hit_debris` 1.35, -24.7, 5,660 | -39.0 / -37.0 / -30.2, centroid 483 to 1,281 Hz |
| mud, tar, slime | `sfx_MudHit` (the same clips and settings as `sfx_blob_hit`, `sfx_Bonemass_Hit`, `sfx_draugrpile_hit`) 1.14, -14.9, 780, 0.54 | `sfx_MudDestroyed` (= `sfx_GuckSackDestroyed`, `sfx_draugrpile_destroyed`) 0.97, -14.4, 1,439 | - | mud run -33.8 (2.6 kHz), sneak -43.1 (3.5 kHz); tar land -17.7 |
| water | - | `vfx_watersplash_*` 2.72, -21.3, 779 | - | run -35.0 (2.7 kHz), land -20.5 (2.6 kHz), swim -26.0 (2.5 s) |
| clay | - | `sfx_clay_pot_break` 1.88, -19.0, 2,346, 0.45, 0.26/0.51/0.25 | - | - |
| coins | - | `sfx_coins_destroyed` 0.85, -14.4, 14,567, 0.23 | `sfx_coins_placed` 0.51, -14.1, 10,856, 0.44 | - |

And the falls and blocks: `sfx_tree_fall` 3.98 s at -15.5 with a 1.9 s creak before the crash (attack 1.94 s), reach
60 m; `sfx_wood_blocked` 0.58 s at -15.8 (333 Hz, a dull knock, pitch 0.7 to 0.8);
`sfx_metal_blocked` 0.88 s at -17.9 (2.6 kHz clang, flatness 0.08).

## What makes each material

Measured over the archetypes (`world.*`, `piece.*`, `build.place.*`, `footstep.*`, `combat.block.*`), in the order to
check them on a new sound: band split first, then flatness, then the envelope.

- **Wood**: a dark, noisy knock (centroid 300 to 750 Hz, flatness 0.50 to 0.53, 82 to 86 % under 300 Hz) with a short
  hollow body; strikes 0.6 to 1.6 s with an instant attack; breaks are splintering crashes of 2.5 to 2.9 s that stay
  loud for a second (decay about 1.8 s) with cracks scattered through them. Wooden floors under feet are the deepest
  steps in the game (100 to 120 Hz): a boot on boards is a thump, not a click.
- **Stone**: noisier and more gritty than wood (0.51 to 0.55), the strike brighter in the mids (`sfx_rock_hit` 35 %
  mid) with a short crunch; placing and footsteps are almost all low thump (86 to 98 % under 300 Hz); breaks are the
  longest impacts (2.7 to 3.7 s, a rumble with falling debris, -15 to -17 LUFS played; world rocks reach 90 m).
- **Metal** is the one material that rings: clean partials between 1 and 5 kHz (flatness 0.08 to 0.18, where every
  other material is 0.4 to 0.55), an instant attack and a 0.4 to 0.7 s ring, no body thump; bars and gates are duller
  (0.40). Coins are the brightest sound in the game (10.9 kHz): a jingle of tiny high partials.
- **Ice**: like stone in balance (81 % under 300 Hz, flatness 0.52) with a glassy upper crunch; breaks 2 to 2.2 s.
- **Crystal**: the break is a dense shower of glassy grit (the crunch, not ringing tones: flatness 0.42 to 0.50)
  over a steady sub band that lasts to the end: 45 % of the energy under 300 Hz, 45 % over 3 kHz, only 10 % between;
  -20 dB after 1.3 to 1.45 s, gone after 1.7 to 2.1 s, the peak 12.5 to 14 dB over the RMS.
- **Bone**: a dry, bright clack (2.3 to 2.7 kHz, 56 to 65 % mid, 28 to 39 % high), short (0.7 to 0.9 s); almost
  nothing under 300 Hz (4 to 6 %).
- **Earth, grass and foliage**: soft brushes and rustles; foliage the brightest (5.3 to 6.3 kHz) and quiet (-24 to
  -27 LUFS); grass under feet swishes at 3.1 kHz at a walk and thumps at a run (363 Hz).
- **Snow and ash**: a crunch in the low mids (0.5 to 1.3 kHz), quiet, noisy (0.53 to 0.57); shovelling snow adds a
  bright spray (5.7 kHz).
- **Mud, tar, slime and water**: wet and bright (2.5 to 3.5 kHz), mid-heavy (59 to 80 % between 300 Hz and 3 kHz)
  with a long, splashy decay (0.9 to 1.9 s).
- **Flesh** has no sound of its own in the game: a hit on a creature is the weapon's hit plus the creature's hurt
  vocal (and a blood particle). A new creature made of a material (stone, wood, ice) adds that material's hit.

## Making an impact

`recipes.impact.hit(material, seed, size)` and `recipes.impact.shatter(material, seed, size, seconds)` build in the
layers the game's sounds show, and the levels of each layer are the material's character:

| layer | what it is | crystal | ice | metal | stone | wood |
| --- | --- | --- | --- | --- | --- | --- |
| click | 4 ms of noise over 2.5 kHz: the contact | all | all | all | all | all |
| ring | modal partials (inharmonic ratios), decaying faster by mode | 0.06 | 0.08 | 1.0 | 0.1 | 0.35 |
| grit | dense tiny noise bursts, 250 Hz to 2.5 kHz, thinning over 0.1 s: the strike's body | yes | yes | yes | yes | yes |
| crunch | the material's grain in its own band (crystal 2.2 to 12 kHz, stone 0.4 to 3.5) | 1.0 | 0.8 | 0.12 | 0.9 | 0.6 |
| thud | a low sine falling 110 to 55 Hz, 60 dB down after 0.3 s: the weight | 1.0 | 1.4 | 0.35 | 1.3 | 1.0 |
| rumble | brown noise under 50 to 70 Hz for the whole event: big and broken things | 1.6 | 0.9 | 0 | 0.6 | 0.2 |

The recipe then compresses the mix (3:1 over a threshold 14 to 16 dB under its loudest moment, the first 20 ms let
through), limits the peaks to 10 to 16 dB over the loudest 400 ms and rounds them (tanh): synthetic strikes start with
a crest of 17 to 20 dB, the game's sit at 12 to 15. Two lessons from tuning it: a thud that dies in 30 ms lets a
later layer become the loudest moment (the attack then measures 0.1 s where the game's is 0.01 to 0.035), so the low
body must last (60 dB down after 0.3 s); and a compressor that catches the first milliseconds does the same.

Worked example: `sfx/sounds/crystal.py`. After the tuning passes its report reads: the break in the game's range for
length, played level, band split (0.47 / 0.07 / 0.46) and crest, near for centroid, with the attack (0.13 s) and the
-20 dB point (1.49 s against 1.25 to 1.46) late and the texture a little too noisy (flatness 0.53 against 0.41 to
0.50); the strike in range for level, attack, decay and tail, and brighter and more tonal than the game's ice hit
(centroid 1.5 kHz against 0.35 to 0.75), which suits crystal but is a choice nobody has listened to yet.

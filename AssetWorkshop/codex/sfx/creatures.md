# Creature sounds

What each creature plays, how its voice is built, and how to make a new one sit among them. Measured from the creature
prefabs' own fields (`BaseAI.m_idleSound`, `m_alertedEffects`, `Character.m_hitEffects`, `m_deathEffects`), their attack
items' effects, their `FootStep` lists and the `Effect` events in their animation clips; 67 creature folders under
`Characters/`. The role shapes (length, level, attack, spectrum) are in [archetypes.md](archetypes.md); contact sheets
per creature (a row per role) are in `codex/out/sfx/creatures/<folder>.png`.

## What every creature has

A fighting creature has, as separate sound prefabs:

| role | where it is set | variations | ZSFX pitch | ZSFX volume | reach | played LUFS |
| --- | --- | --- | --- | --- | --- | --- |
| idle | `BaseAI.m_idleSound`, tried every `m_idleSoundInterval` s (Greydwarf 10, Boar 15, Troll 5) and played with `m_idleSoundChance` (0.25 to 0.5) | 6 | 0.95-1.07 | 0.7-0.8 | 80 m | -20.3 |
| alert | `BaseAI.m_alertedEffects` | 4 | 0.95-1.07 | 0.8 | 60 m | -15.7 |
| attack | each attack item's `m_startEffect` / `m_triggerEffect` / `m_trailStartEffect` | 3 | 0.95-1.0 | 1.0 | 80 m | -13.3 |
| attack hit | each attack item's `m_hitEffect` | 4 | 0.9-1.0 | 0.5-0.55 | 50 m | -18.2 |
| hurt | `Humanoid.m_hitEffects` (with a blood or splinter particle prefab) | 5 | 0.97-1.0 | 0.5-0.6 | 40 m | -17.2 |
| death | `Humanoid.m_deathEffects` (with the ragdoll) | 3 | 0.95-1.0 | 1.0 | 50 m | -13.0 |
| footsteps | `FootStep.m_effects` per motion and ground | 9 | 0.88-1.0 | 0.65-0.8 | 40 m | -28.3 |

Most also have `m_critHitEffects` and `m_backstabHitEffects` pointing at the shared `fx_crit` and `fx_backstab`
(`sfx_crit` 1.4 s, `sfx_backstab` 1.0 s: bright stingers played on top of the hit), and tameable animals `m_petEffect` (`fx_*_pet`, 15 m) and
`m_tamedEffect` (`fx_creature_tamed`). All creature sounds are fully 3D, most in the SFX group with a custom roll-off
curve; big creatures and bosses move some into SFX_LARGE (3.5 s hall reverb) and reach 100 m. Captions: the
creature's `$enemy_*` token with `$caption_attacking`, `$caption_alerted`, `$caption_dying`, `$caption_hurt`
(caption type 2, enemy; 1 for wildlife and enemy idles; 3 for bosses).

The level order is the same for nearly every creature: death and attack loudest (about -13 LUFS played), alert 2.5 dB
under, hurt 4 dB under (it plays with the weapon's hit), idle 7 dB under (it repeats), footsteps 15 dB under.

## Variants are the same clips at another pitch

The game makes a creature's smaller, bigger or elite version by pointing a copy of its sound prefabs at the same clips
with another ZSFX pitch (Unity pitch: 2.0 is an octave up and half as long), and sometimes another volume and reach:

| family | clips shared | pitch (ZSFX min-max) |
| --- | --- | --- |
| Greydwarf, Greydwarf Elite, Greyling | idle (8), alert (3), attack (4), hurt (3), death (1) | Greydwarf idle 1.2-1.3, alert 1.1-1.3, attack 1.0-1.1, hurt and death 1.1-1.2; Elite 0.8-1.0 (a third lower); Greyling 1.8-2.2 (almost an octave up) |
| Goblin (Fuling), Goblin Brute | idle (4), alert (3), hurt (9), death (4) | Goblin 1.0-1.3; Brute 0.5-0.7 (an octave down, and louder: volume 1.0 against 0.6 for the idle) |
| Bear (Bjorn), Unbjorn | footsteps (10), claw slash (5) | both 1.0 |
| Bear bite, Writhan bite | 16 clips | Bear 1.0; Writhan 2.5 |
| Seeker, Seeker hatchling | idle (11) | Seeker 0.7-0.8; hatchling 0.9-1.0 |
| Skeleton, big Skeleton | death (3) | both 0.9-1.1 |
| Troll | alert and attacking (3) | both 0.9-1.0 |
| Wolf | alert and love (2) | alert 0.7, -19, 1056, 0.42; attack 1.2, -13, 1352, 0.46; hurt 1.0, -18, 990, 0.36; death 0.9, -10, 1396, 0.37 |
| many biters | attack hit (2): Greydwarf, wolf, neck, leech, serpent, wraith, fenring, dragon | 0.8-1.0, volume 0.5 to 1.0 |

So a new variant of a game creature (a bigger, elite or young version) should reuse its clips at a pitch that follows
its size: the bigger the creature, the lower (the small Greyling plays the Greydwarf's clips at 1.8 to 2.2, the
Elite at 0.8 to 1.0; the big Goblin Brute plays the Goblin's at 0.5 to 0.7, an octave down). That needs no new audio at all:
`SfxPrefabs.Variant(scene, "sfx_greydwarf_idle", "MyMod_giant_idle", new SfxSettings { MinPitch = 0.6f, MaxPitch =
0.65f, Caption = "$enemy_mymod_giant" })` copies the game prefab with its own clips and changes only what is set.

## Voice character by size

Clip centroid (Hz) of each creature's own idle and alert, as measured on the clips; what the player hears is that
times the ZSFX pitch (the Greyling's clips at 554 Hz play at about 1,050).

| size | creatures (idle / alert centroid, Hz) | character |
| --- | --- | --- |
| tiny (hare, chicken, neck, bat, tick, seeker) | Hare 3,034 / 5,994; Neck 3,516 / 1,363; Bat 10,835 / 2,619; Tick 2,452 / 2,608; Seeker 1,769 / 3,617 | squeaks, chitters and hisses; clicky (Tick, Seeker) or tonal (Chicken flatness 0.10, Hare 0.16 to 0.36); 0.6 to 2 s; quiet idles (-22 to -38 LUFS) |
| small (goblin, greydwarf, skeleton, draugr, wolf, boar, deer, leech) | Goblin 1,605 / 925; Greydwarf 554 / 463; Skeleton 1,942 / 1,749; Draugr 775 / 1,070; Boar 651 / 1,229; Deer 399 / 238; Leech 1,595 / 1,595 | the human range: grunts, groans, rasps; goblins speak (flatness 0.09 to 0.29: clear harmonics), undead rasp (0.33 to 0.48); 0.8 to 3.3 s |
| large (troll, lox, abomination, gjall, stone golem, fenring, seeker brute) | Troll 466 / 356; Lox 200 / 474; Abomination 70 / 120; Gjall 148 / 182; Stone golem 126 / 241; Seeker brute 541 / 643 | roars and rumbles with most energy under 300 Hz; long (idles 2 to 6 s, Gjall's alert 9 s); noisy (0.40 to 0.51) |
| bosses | Eikthyr 327 / 235; the Elder 196 / 309; Bonemass 136 / 616; Moder 422 / 562; the Queen 333 / 1,672; Fader 78 / 239 | as large creatures, louder (-8 to -16 LUFS) and longer, with a stinger for summoning and a long death sequence (the Frozen King's 15 s) |

Rules of thumb for a new creature, from the same table: halve the size, raise pitch and centroid about an octave and
shorten calls by a quarter; double it, lower them an octave and lengthen by a half; undead and constructs are
noisier (flatness 0.45 to 0.55) than beasts (0.35 to 0.45), which are noisier than speakers (0.1 to 0.3).

## Per creature

Each creature folder's roles, and for its own sounds (prefabs in its folder) the median length s, played LUFS,
centroid Hz and flatness per vocal role. A folder can hold several creatures (Troll holds the forest troll, the fire
troll's attacks and some of the frost troll's; GreyDwarf holds the Greydwarf, Elite, Shaman and Greyling); the full
list per sound, with the prefabs that make it (`makers`), is under `creatures` in `data/sfx.json`.

| creature | roles | idle / alert / attack / hurt / death: length s, played LUFS, centroid Hz, flatness |
| --- | --- | --- |
| Abomination | alert, attack, attack_hit, crit, death, footstep, hurt, idle, move, spawn | idle 2.9, -15, 70, 0.43; alert 2.5, -13, 120, 0.48; attack 1.9, -12, 86, 0.50; death 2.4, -13, 189, 0.51 |
| Asksvin | alert, anim, attack, attack_hit, breed, crit, death, eat, footstep, hurt, idle, move, tame | idle 2.5, -20, 169, 0.44; alert 1.8, -15, 290, 0.49; attack 1.3, -19, 455, 0.47; death 2.2, -17, 217, 0.46 |
| Barka | alert, attack, attack_hit, crit, death, hurt, idle, jump, move | idle 3.1, -17, 245, 0.49; alert 3.1, -17, 245, 0.49; attack 3.4, -16, 352, 0.55; hurt 2.0, -22, 318, 0.50; death 3.4, -14, 211, 0.52 |
| Bat | alert, attack, attack_hit, crit, death, hurt, idle | idle 1.7, -30, 10835, 0.42; alert 1.0, -14, 2619, 0.32; attack 0.3, -21, 2122, 0.47; hurt 0.6, -15, 1742, 0.46; death 1.0, -14, 1612, 0.50 |
| Bjorn | alert, attack, attack_hit, crit, death, eat, footstep, hurt, idle, tame | idle 1.7, -28, 700, 0.45; alert 1.7, -28, 700, 0.45; attack 2.2, -15, 714, 0.43; hurt 2.1, -15, 586, 0.40; death 1.7, -15, 514, 0.46 |
| Blob | alert, attack, attack_hit, crit, death, footstep, hurt, idle, jump | idle 1.9, -23, 239, 0.46; alert 2.1, -17, 380, 0.52; attack 0.8, -15, 1096, 0.52; hurt 1.1, -15, 780, 0.54; death 2.0, -12, 404, 0.51 |
| Boar | alert, attack, attack_hit, breed, crit, death, eat, footstep, hurt, idle, tame | idle 0.7, -23, 651, 0.41; alert 1.6, -14, 1229, 0.37; attack 0.6, -12, 1481, 0.51; hurt 0.9, -12, 1175, 0.35; death 1.5, -12, 1830, 0.27 |
| Bonemass | alert, attack, attack_hit, crit, death, hurt, idle | idle 4.7, -15, 136, 0.48; alert 1.2, -16, 616, 0.46; attack 1.6, -11, 439, 0.46; death 2.3, -11, 682, 0.41 |
| BonemawSerpent | alert, attack, attack_hit, crit, death, hurt, idle | attack 9.0, -10, 342, 0.52; death 4.8, -12, 149, 0.52 |
| Chicken | attack, attack_hit, breed, crit, death, eat, footstep, hurt, idle, tame | idle 1.1, -38, 1294, 0.10; hurt 0.6, -30, 2567, 0.16; death 0.4, -26, 1274, 0.10 |
| Deathsquito | attack, crit, death, hurt | attack 0.7, -22, 3837, 0.29; death 1.2, -14, 632, 0.54 |
| Deer | alert, crit, death, footstep, hurt, idle | idle 2.4, -19, 399, 0.22; alert 2.2, -14, 238, 0.21; death 1.5, -8, 293, 0.21 |
| Dragon (Moder) | alert, anim, attack, attack_hit, crit, death, footstep, hurt, idle, jump, move | idle 2.3, -17, 422, 0.49; alert 2.9, -13, 562, 0.51; attack 2.1, -11, 610, 0.48; hurt 1.0, -15, 1565, 0.51; death 2.1, -12, 515, 0.47 |
| Draugr | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.4, -23, 775, 0.33; alert 1.3, -15, 1070, 0.41; hurt 0.6, -21, 1050, 0.34; death 2.0, -15, 508, 0.42 |
| Dverger | alert, anim, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.1, -27, 809, 0.28; alert 0.5, -27, 967, 0.30; attack 1.9, -18, 822, 0.48; hurt 1.3, -22, 901, 0.36; death 0.9, -21, 1086, 0.23 |
| Eikthyr | alert, attack, attack_hit, crit, death, footstep, hurt, idle, spawn | idle 1.4, -14, 327, 0.46; alert 2.1, -8, 235, 0.29; attack 1.8, -10, 735, 0.49; hurt 1.5, -13, 272, 0.21; death 4.2, -9, 332, 0.38 |
| Elaking | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.8, -25, 1537, 0.31; alert 2.7, -29, 1469, 0.33; attack 2.6, -19, 2374, 0.45; hurt 2.8, -15, 2484, 0.46; death 2.0, -13, 1910, 0.26 |
| ElakingMole | alert, anim, attack, attack_hit, crit, death, footstep, hurt, idle, move | idle 2.8, -21, 495, 0.43; alert 2.1, -16, 512, 0.44; attack 2.6, -11, 1032, 0.49; hurt 1.8, -13, 1104, 0.46; death 2.9, -12, 863, 0.46 |
| Fader | alert, attack, attack_hit, crit, death, footstep, idle, jump | idle 3.0, -11, 78, 0.40; alert 10.9, -9, 239, 0.48; attack 2.4, -11, 209, 0.47; death 10.9, -8, 227, 0.54 |
| FallenValkyrie | alert, attack, attack_hit, crit, death, hurt, idle, move | idle 3.5, -24, 847, 0.46; alert 2.5, -12, 641, 0.41; attack 2.7, -17, 641, 0.42; death 4.9, -13, 480, 0.35 |
| FallenWarrior | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 4.7, -36, 1310, 0.33; alert 4.7, -36, 1310, 0.33; attack 3.4, -29, 1207, 0.40; hurt 3.0, -26, 853, 0.46; death 3.8, -20, 354, 0.45 |
| Fenring | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.6, -19, 168, 0.60; alert 0.8, -16, 304, 0.43; attack 1.0, -12, 838, 0.50; death 2.5, -16, 649, 0.46 |
| FrozenKing | alert, attack, attack_hit, crit, death, footstep, hurt, idle, jump, move | idle 5.5, -15, 470, 0.44; attack 2.0, -13, 666, 0.47; death 15.4, -16, 1073, 0.51 |
| Ghost | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 2.9, -26, 972, 0.44; alert 2.8, -26, 1548, 0.44; attack 2.4, -20, 2122, 0.46; hurt 1.5, -20, 2017, 0.44; death 2.3, -15, 737, 0.46 |
| Gjall | alert, attack, attack_hit, crit, death, hurt, idle | idle 3.5, -16, 148, 0.48; alert 9.1, -11, 182, 0.41; attack 4.9, -13, 112, 0.56; death 3.5, -15, 648, 0.56 |
| Goblin (Fuling) | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 0.8, -19, 1605, 0.28; alert 0.5, -15, 925, 0.27; hurt 0.6, -19, 962, 0.22; death 1.1, -10, 1036, 0.09 |
| GoblinBrute | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 0.8, -15, 1605, 0.28; alert 1.0, -13, 1152, 0.29; attack 0.9, -16, 839, 0.41; hurt 0.6, -14, 962, 0.22; death 1.1, -10, 1036, 0.09 |
| GoblinKing (Yagluth) | anim, attack, attack_hit, crit, death, footstep, hurt, spawn | attack 1.7, -9, 646, 0.47; death 6.3, -11, 500, 0.56 |
| GoblinShaman | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.6, -17, 1493, 0.22; alert 0.7, -11, 1310, 0.29; attack 1.4, -13, 401, 0.55; hurt 0.6, -12, 1453, 0.29; death 0.8, -12, 1310, 0.29 |
| GreyDwarf | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.8, -20, 554, 0.42; alert 3.3, -16, 463, 0.38; attack 0.8, -14, 505, 0.43; hurt 0.9, -17, 516, 0.36; death 1.6, -9, 531, 0.43 |
| Greydwarf_king (the Elder) | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 6.3, -13, 196, 0.51; alert 1.6, -8, 309, 0.47; attack 1.6, -11, 512, 0.49; death 3.3, -12, 1001, 0.51 |
| Hare | alert, crit, death, footstep, hurt, idle | idle 1.0, -30, 3034, 0.36; alert 0.9, -23, 5994, 0.25; hurt 0.9, -21, 3989, 0.16; death 0.9, -21, 3989, 0.16 |
| Hatchling | alert, anim, attack, attack_hit, crit, death, hurt, idle, move | idle 1.7, -17, 378, 0.46; alert 2.7, -14, 1749, 0.35; attack 1.1, -13, 1030, 0.53; hurt 1.8, -13, 1357, 0.39; death 2.1, -11, 1194, 0.44 |
| Jotnar | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 3.7, -33, 776, 0.37; alert 3.7, -33, 776, 0.37; attack 2.6, -18, 1059, 0.47; hurt 2.3, -17, 1097, 0.42; death 3.2, -19, 818, 0.41 |
| Leech | alert, attack, attack_hit, crit, death, hurt, idle | idle 2.5, -19, 1595, 0.48; alert 2.5, -18, 1595, 0.48; attack 0.9, -17, 2877, 0.41; hurt 0.6, -17, 2179, 0.50; death 0.8, -12, 486, 0.53 |
| Lox | alert, anim, attack, attack_hit, breed, crit, death, eat, footstep, hurt, idle, tame | idle 0.9, -17, 200, 0.47; alert 1.7, -12, 474, 0.47; attack 2.3, -10, 542, 0.46; death 1.4, -9, 538, 0.50 |
| Morgen | alert, attack, attack_hit, crit, death, footstep, idle, jump, move, spawn | idle 2.2, -14, 345, 0.44; alert 1.8, -12, 544, 0.48; attack 1.9, -13, 427, 0.49; death 2.1, -10, 454, 0.54 |
| Neck | alert, attack, attack_hit, crit, death, hurt, idle | idle 0.8, -22, 3516, 0.39; alert 1.1, -14, 1363, 0.38; attack 0.6, -16, 859, 0.46; hurt 0.6, -20, 1869, 0.42; death 0.8, -12, 2205, 0.43 |
| Seeker | alert, attack, attack_hit, crit, death, footstep, hurt, idle, jump, move | idle 2.0, -17, 1769, 0.48; alert 0.8, -13, 3617, 0.34; attack 1.5, -13, 2455, 0.51; hurt 0.8, -14, 9782, 0.38; death 1.4, -15, 1698, 0.52 |
| SeekerBrute | alert, attack, attack_hit, crit, death, footstep, idle, jump | idle 3.7, -18, 541, 0.51; alert 4.5, -12, 643, 0.46; attack 1.4, -13, 236, 0.54; death 2.2, -15, 380, 0.55 |
| SeekerQueen | alert, anim, attack, attack_hit, crit, death, footstep, idle, jump, move, spawn | idle 4.5, -11, 333, 0.41; alert 3.8, -10, 1672, 0.54; attack 3.6, -10, 381, 0.56 |
| Serpent | alert, attack, attack_hit, crit, death, hurt, idle | idle 2.5, -18, 1595, 0.48; alert 2.7, -14, 1180, 0.40; attack 2.5, -11, 779, 0.48 |
| Skeleton | alert, attack, attack_hit, crit, death, footstep, hurt, idle, tame | idle 1.7, -20, 1942, 0.44; alert 2.0, -18, 1749, 0.48; attack 1.9, -15, 1562, 0.46; hurt 0.7, -18, 2748, 0.41; death 1.7, -14, 1521, 0.43 |
| StoneGolem | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 4.0, -17, 126, 0.40; alert 1.5, -13, 241, 0.55; attack 1.5, -12, 512, 0.44; hurt 1.0, -10, 2022, 0.55; death 2.9, -12, 191, 0.45 |
| Surtling | alert, attack, attack_hit, crit, death, hurt | alert 1.3, -12, 1954, 0.35; attack 1.4, -9, 1394, 0.50; hurt 0.9, -17, 2173, 0.32; death 1.8, -8, 1437, 0.49 |
| TheCharred | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 3.4, -21, 472, 0.42; alert 2.5, -17, 696, 0.42; attack 1.7, -19, 1100, 0.51; hurt 1.8, -16, 1490, 0.40; death 2.9, -15, 662, 0.43 |
| Tick | alert, attack, attack_hit, crit, death, eat, footstep, hurt, idle | idle 1.4, -25, 2452, 0.51; alert 1.0, -19, 2608, 0.48; attack 1.1, -18, 2627, 0.46; hurt 0.8, -19, 2941, 0.49; death 1.7, -17, 1171, 0.50 |
| Troll | alert, attack, attack_hit, crit, death, footstep, hurt, idle | idle 1.9, -17, 466, 0.45; alert 1.8, -13, 356, 0.39; attack 1.6, -11, 135, 0.55; hurt 0.9, -16, 1023, 0.51; death 4.2, -9, 250, 0.44 |
| Volture | alert, anim, attack, attack_hit, crit, death, hurt, idle, move | idle 0.8, -31, 4388, 0.50; alert 1.4, -20, 1813, 0.44; attack 1.4, -20, 1525, 0.45; death 1.5, -12, 934, 0.47 |
| Wolf | alert, anim, attack, attack_hit, breed, crit, death, eat, footstep, hurt, tame | alert 0.7, -19, 1056, 0.42; attack 1.2, -13, 1352, 0.46; hurt 1.0, -18, 990, 0.36; death 0.9, -10, 1396, 0.37 |
| Wraith | alert, attack, attack_hit, crit, death, hurt, idle | idle 2.3, -22, 1229, 0.38; alert 2.0, -17, 1052, 0.51; attack 0.9, -12, 790, 0.47; hurt 0.5, -15, 1069, 0.36; death 1.6, -13, 1284, 0.50 |
| Writhan | attack, attack_hit, crit, death, footstep, hurt, idle, move | idle 2.4, -15, 314, 0.43; attack 3.2, -15, 570, 0.49; death 3.2, -17, 623, 0.54 |
| moose | alert, attack, attack_hit, crit, death, eat, footstep, hurt, idle, tame | idle 1.2, -28, 918, 0.27; alert 1.4, -25, 1147, 0.06; attack 1.5, -21, 419, 0.47; hurt 2.5, -18, 725, 0.06; death 2.5, -18, 389, 0.43 |
| seal | alert, crit, death, footstep, hurt, idle | idle 1.1, -35, 1331, 0.25; alert 1.0, -30, 1484, 0.22; hurt 1.0, -30, 1484, 0.22; death 1.1, -27, 1530, 0.09 |

Creatures without a voice cell (Frostwisp, Frysling, Hive, Ulv, Valkyrie, Raven, the traders) play only shared or
borrowed sounds, loops or animation-event foley; their rows are in `data/sfx.json`. The traders (Haldor, Hildir, the
Bog Witch) speak through `Trader` lists (`sfx_haldor_greet`, `sfx_hildir_hello`...): 0.8 to 1.6 s, very tonal
(flatness 0.07 to 0.25), pitch 1.07 to 1.1.

## Bosses

| boss | folder | voice (own sounds) | around the fight |
| --- | --- | --- | --- |
| Eikthyr | Eikthyr | deep bellows whose harmonics fall: alert 1.8 to 2.1 s at -8 LUFS, 235 Hz, fairly tonal (0.29); death 4.2 s | antler stomp `fx_eikthyr_stomp` (shockwave), altar `sfx_offering` / `sfx_spawn`; 9 of 10 sounds in SFX_LARGE |
| The Elder | Greydwarf_king | a long grinding creak in two parts for idle (6.3 s, 196 Hz, noisy 0.51), alert 1.5 s at -8 | root spawn `fx_gdking_rootspawn`, `sfx_gdking_stomp`, `sfx_gdking_shoot_start` |
| Bonemass | Bonemass | a bubbling low idle (3.9 s, 143 Hz, ending abruptly), alert 1.2 s at -16, attack 1.6 s at -11 | `sfx_Bonemass_aoe_start`, `sfx_Bonemass_spawn_draugr_start`, punches |
| Moder | Dragon | idle 2.3 s, alert 2.9 s at -13, 422 to 562 Hz, noisy (0.49 to 0.51) | cold breath and cold ball (`sfx_dragon_coldbreath_*`), wing `sfx_dragon_flap` (pitch 0.7 to 0.8, 100 m) |
| Yagluth | GoblinKing | attack 1.7 s at -9, death 6.3 s | voice lines as effects (`fx_goblinking_vo_*`), `sfx_goblinking_beam` loop, meteors and nova |
| The Queen | SeekerQueen | idle 4.5 s at -11, alert 3.8 s at -10 (1.7 kHz screech, 0.54) | burrow, pierce, rush, turn as animation events; 15 of 17 in SFX_LARGE |
| Fader | Fader | a short low idle growl (3 s, 78 Hz, -10.8 LUFS played); its loudest sounds are events, the meteor call (`sfx_fader_meteor_start`, 11 s, -8.8) and the death explosion (`sfx_fader_death_explosion`, 8 s, -7.6) | fissures, meteors, fire walls, breath, a bell (`sfx_fader_bell`) |
| The Frozen King | FrozenKing | idle 5.5 s, a 15.4 s death sequence; 53 sound nodes captioned `$sfx_frozenking` | chains, spike rain, tendrils, spirit summons, three looping idles by phase |

## Making a new creature's sounds

1. Pick the nearest game creature by size and kind from the tables above and open its sheet
   (`codex/out/sfx/creatures/<folder>.png`). A bigger, smaller or elite version of it: reuse its clips at another
   pitch (above) and stop there.
2. Otherwise make the five vocal roles (idle, alert, attack, hurt, death) with `recipes.voice.voice(role, size, seed,
   base_hz, grit)`, layered with what the creature is made of (wood: `recipes.creak`; stone: `recipes.impact`
   crunch; slime: filtered noise bursts), and variations as in the first table (idle 6, alert 4, attack 3, hurt 5,
   death 3). `sfx/sounds/rootling.py` is the worked example.
3. Play them through copies of the nearest creature's own sound prefabs (`sfx_greydwarf_idle` for a small forest
   creature), which keeps its reach, roll-off, reverb and concurrency; set the pitch back to about 0.95 to 1.05 when
   the original's is far from 1 (the Greydwarf's idle plays at 1.2 to 1.3), and always set the caption to the new
   creature's `$enemy_*` token.
4. Footsteps: borrow the player's surface steps (`fx_footstep_run`, `fx_footstep_mud_run`, as small creatures do) or
   a big creature's (`sfx_troll_footstep` at pitch 0.6 to 0.85) unless the creature walks on something unusual.

# Deathsquito Queen: sounds

The Queen's sound cues for the workshop preview, made by `queen_sfx.py` from the game's own recordings. **Nobody has
listened to them yet.** The numbers below say where they sit against the game's Deathsquito, not that they sound
right; ears have the last word. Preview only: nothing here goes into a mod, no bundle, no audio shipped.

- **Real recordings only.** Every cue is one to three of the game's clips from the local reference export, never
  synthesis. Where a clip comes from another creature or an item, the table says why it fits.
- **Sized for her.** She is the Deathsquito at 2.5 times, so her own sounds are the Deathsquito's clips pitched down
  (her wings 6.9 semitones, the sting 4). Nothing is at troll scale: the game's troll and sledge clips carry 70 to 93 %
  of their energy under 80 Hz, and these cues carry 0 to 14 % (most under 2 %). Every layer is high-passed at 100 to
  300 Hz.
- **Only what a mod can do at runtime** with the game's clips: volume (and a volume ramp), a start time into the clip,
  a delay, pitch (`AudioSource.pitch`, set every frame for the three glide cues), and high- and low-pass
  (`AudioHighPassFilter` / `AudioLowPassFilter`, 2nd order, which the script's filters match). A mod would play the same
  recipes with the game's clips by name.

```
blender --background --factory-startup --python queen_sfx.py            (writes out/sfx/build_<time>/<cue>_<n>.wav)
AssetWorkshop/out/venv/Scripts/python queen_sfx.py -- <out dir>         (the same with soundfile, for quick passes)
```

Each run writes a new `out/sfx/build_<time>/` folder and keeps the three before it. The preview's `_own(cue)` (as in
`ecp_headsman/blender_fx.py`) picks the newest folder.

## The cues

`peak` means the clip's start is cut so its loudest moment lands that many seconds after the cue. `hp`/`lp` are the
high- and low-pass in Hz. Pitch is Unity's: 0.8 is 4 semitones down and 1.25 times as long.

| Cue | When it plays (where to put the cue) | Game clips and processing | Why |
| --- | --- | --- | --- |
| `buzz_loop` | All the time she is in the air: one strip every 4.0 s (120 frames at 30 fps), back to back. 1 file | `Characters/Deathsquito/fx/wav/Insect_Wasp_WingsLoop3` (the Deathsquito's own wing loop, 2.69 s) played whole at pitch 0.6733 (exactly 4.0 s), looped, hp 110 | Her own species' buzz, 6.9 semitones deeper: fundamental 203 Hz becomes 136 Hz, a big insect and no sub-bass. The clip is a seamless loop and stays one (seam -6 dB) |
| `buzz_strain` | Dive tell: at the move's start (0.0). It rises in 0.45 s, holds with a tremble, ends at 1.0 s (the dive starts at 1.1). 3 files | The same wing loop from 0.3 / 1.1 / 1.9 s into it, pitch ramped from 0.672 to 0.97 / 1.03 / 0.92 over 0.45-0.5 s, then to 1.0 / 1.06 / 0.95 at 1.0 s; a 7 Hz, 1.5 % pitch tremble; volume from 0.35 to 1 as it rises; hp 120 | Her wings straining: her own buzz climbing back up to the small Deathsquito's pitch, a whine rising over the hover |
| `dive_whoosh` | At the dive's start (1.1): its rush peaks 0.2 s after the cue (1.3, the hit). 3 files | `Player_Movement_SpearThrow_M_01..03` (the player's spear throw) at pitch 0.8 / 0.76 / 0.84, peak 0.2, hp 150, 0.48 s | A long pointed thing rushing straight through the air. The game's swings are nearly all bass; the spear throw has almost none (under 2 % below 80 Hz) |
| `pierce` | At the hit (the dive's 1.3). Onset at the cue. 3 files | `Player_Movement_Spear_Hit_M_01/02/05` (the spear landing) at 0.9 / 0.88 / 0.92, peak 0, hp 140, 0.28 s; the Deathsquito's `Blood_Splat_Gib02/17` at 0.6, 0.9 / 0.85 / 0.95, hp 160, 0.35 s | A spear thrust striking flesh, and the Deathsquito's own flesh splat, a little lower for her weight |
| `lunge` | The lunge: its sting's thump lands 0.40-0.47 s after the cue, so put the cue at about 0.05 (hit 0.5). 3 files | The Deathsquito's own attack `sScorpionTailAttack01/02` at pitch 0.8 / 0.78 / 0.84, hp 100, 0.9 s, 0.08 s fade | Her species' own sting sound (a hissing rush and a thump), 4 semitones down. It carries its own steady whine at 4.9 kHz (3.9 kHz here), the Deathsquito's, kept |
| `egg_launch` | Each egg leaving the abdomen (0.6 + 0.25 k). Squelch at the cue, pop at +0.1. 3 files | `Foot_Mud_Run03/05/01` (a boot in mud) at 0.8, peak 0.02, gain 0.7, hp 200, 0.22 s; the Deathsquito's `Blood_Splat_Gib17/02/17` at 0.85 / 0.9 / 0.8, peak 0.1, hp 160, 0.3 s | A wet squeeze, then a wet pop as the egg comes free |
| `egg_land` | Each egg landing (0.8 s after its launch). Onset at the cue. 3 files | `Player_Footstep_Default_Land_M_01..03` (a body landing on earth) peak 0, hp 180, 0.35 s; `UI_Hoe_01/04/05` (the hoe biting into dirt) peak 0.01, gain 0.6, hp 180, 0.3 s; `Foot_Mud_Run08/03/05` at 0.85, +0.02 s, gain 0.45, hp 200, 0.3 s | A heavy thud into soil, dirt giving way (it sinks in half), a wet squelch. The landing thud is 57-60 % sub-bass as the game has it; hp 180 brings the cue to 3-10 % |
| `egg_pulse` | Each throb from 17 s (1 a second, then 3 a second in the last 3 s). 0.25 s, onset at the cue. 3 files | `Player_Movement_Unarmed_Hit_M_01/02/04` (a fist landing on a body) peak 0.02, hp 130, lp 700, 12 ms fade-in, 0.26 s; `Foot_Mud_Run03/05/01` at 0.8, peak 0.03, gain 0.4, hp 250, lp 2500, 0.2 s | A soft, wet heartbeat: a body thump with its crack taken off (lp 700) and a faint squelch. It is the lowest cue (centroid 190-290 Hz), still 6-14 % under 80 Hz |
| `egg_burst` | The shell bursting at 20 s. Onset at 0.02, crumbling from 0.15 to 0.8 s. 3 files | `Enemy_Tick_Death_01/02/05` at 0.8 (exactly how the game bursts its Seeker egg, `fx_egg_splash`), peak 0.02, hp 120, 0.55 s; `world/Props/BeeHive/fx/wav/Smash_TorchBreak1` (the beehive breaking) at 0.9 / 0.85 / 0.95, peak 0.01, gain 0.6, hp 200, 0.3 s; `Amb_WindingTunnel_OneShots_RockCrumble_06/07/05` +0.15 s, gain 0.45, hp 150, 0.65 s, volume falling to 0.4 | The game's own egg burst, the crack of a papery shell (the hive), and the pieces crumbling away |
| `hatch_buzz` | The hatchling rising out of the cup (0.8 s). It spins up in 0.35 s. 3 files | The Deathsquito's wing loop from 0.0 / 0.9 / 1.8 s into it, pitch ramped 0.8 to 1.0 over 0.35 s, volume 0 to 1 over 0.3 s, hp 110, 1.4 s, 0.35 s fade | The game's own Deathsquito, at its own pitch once its wings are up to speed |
| `needle_shot` | Each needle (volley 0.55 + 0.125 k; the single shot 0.3). 0.15-0.2 s. 4 files | `Characters/GreyDwarf/fx/audio/wav/Zombie04_Claw_Swipe2/3` (the swish eight creatures play when a bite lands) at 1.1 / 1.0 / 0.95 / 1.15, peak 0.03, hp 300, 0.2 s; `Characters/Neck/fx/wav/GiantMantis_Spit` at 1.0 / 1.1 / 0.9 / 1.05, peak 0.01, gain 0.5, hp 250, 0.12 s | A quick bright swish on the onset of a creature's spit: sharp, short, no ring (the bow's shot has a string twang, so it is not used) |
| `needle_ground` | Each needle sticking in the ground (its arrival). Onset at the cue. 4 files | `Weapons_Arrow_Hit_M_01/02/03/01` (what the game plays when an arrow sticks) at 1.15 / 1.1 / 1.2 / 1.05, peak 0.005, hp 150, 0.25 s; `UI_Hoe_02/03/05/01` at 1.1, peak 0.01, gain 0.35, hp 200, 0.15 s | A shaft thunking into soil, a little higher than an arrow for a thinner needle, with a pinch of dirt |
| `needle_flesh` | Each needle hitting a player. 3 files | `Weapons_Arrow_Hit_M_02/03/01` at 1.1 / 1.0 / 1.15, peak 0.005, hp 150, 0.25 s; the Deathsquito's `Blood_Splat_Gib02/17/02` at 1.1 / 1.0 / 1.2, peak 0.02, gain 0.55, hp 200, 0.3 s | The arrow's hit and the Deathsquito's flesh splat, smaller and higher than `pierce` |
| `zip_whir` | The whirl's start (0.3): 1.1 s, needle rushes at +0.35 and +0.75. 3 files | The Deathsquito's wing loop from 0.2 / 1.0 / 1.7 s into it, pitch ramped 0.8 to 0.9 over 0.3 s and swung +-5 % at 1.5 Hz (once per turn and a half), volume 0.4 to 1 and out by 1.1 s, hp 120; `SpearThrow_M_01/03/02` at 1.1, peak 0.35, gain 0.45, hp 200, 0.35 s; `SpearThrow_M_03/02/01` at 1.05, peak 0.75, gain 0.45, hp 200, 0.35 s | Her wings working harder, the pitch swinging as she goes round, and the proboscis rushing past twice |
| `alert` | When she engages (optional, once). 2 files | `Characters/Neck/fx/wav/GiantBeetle_Growl1/2` (the swamp's giant beetle) at 0.8 / 0.78, hp 200, 1.2 s; `Enemy_Seeker_Alerted_03/04` at 0.75 / 0.72, +0.05 s, gain 0.5, hp 300, 1.0 s | A buzzing cry: the beetle's growl rattles at about 20 Hz (17 Hz here), the Seeker's insect screech rides over it. The Deathsquito itself has no voice |

## For the preview

As in `ecp_headsman/blender_fx.py` (`OWN` = the cue's files from the newest `out/sfx/build_*`, variants in turn):

```python
SOUNDS = {
    'buzz_loop': [(OWN, 0.25, 4.0)],
    'buzz_strain': [(OWN, 0.35, 1.0)],
    'dive_whoosh': [(OWN, 0.45, 0.5)],
    'pierce': [(OWN, 0.5, 0.4)],
    'lunge': [(OWN, 0.45, 0.9)],
    'egg_launch': [(OWN, 0.4, 0.4)],
    'egg_land': [(OWN, 0.4, 0.4)],
    'egg_pulse': [(OWN, 0.25, 0.3)],
    'egg_burst': [(OWN, 0.5, 0.8)],
    'hatch_buzz': [(OWN, 0.3, 1.4)],
    'needle_shot': [(OWN, 0.3, 0.2)],
    'needle_ground': [(OWN, 0.25, 0.25)],
    'needle_flesh': [(OWN, 0.35, 0.3)],
    'zip_whir': [(OWN, 0.45, 1.1)],
    'alert': [(OWN, 0.5, 1.2)],
}
```

Every file is levelled the same way (its loudest 0.1 s to RMS 0.12, peak at most 0.9), so these volumes are the mix:
the game's order (creature attacks and hits loudest, the idle under them, whatever repeats quietest). The buzz loop
runs under everything at 0.25, so `buzz_strain`, `hatch_buzz` and `zip_whir` sit over it; the needles and the egg's
throb repeat many times and are the quietest.

## Measured (the Blender build of 2026-09-29)

Per cue over its files: length, peak and the share of energy per band, and the spectral centroid.

| Cue | Files | Length s | Peak dBFS | Under 80 Hz % | 80-160 | 160-300 | 300-600 | 0.6-1.5k | 1.5-4k | Over 4k | Centroid Hz |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `buzz_loop` | 1 | 4.00 | -3.7 | 0.0 | 6 | 34 | 29 | 20 | 7 | 4 | 813 |
| `buzz_strain` | 3 | 1.00 | -5.3 to -3.1 | 0.1 | 0-1 | 6-9 | 36-42 | 38-46 | 6-7 | 5-6 | 1,081-1,114 |
| `dive_whoosh` | 3 | 0.48 | -7.7 to -6.9 | 0.0 | 1 | 18-45 | 13-57 | 16-34 | 7-16 | 1-3 | 644-1,065 |
| `pierce` | 3 | 0.35 | -6.3 to -4.5 | 1.5-5.7 | 7-18 | 8-13 | 8-16 | 14-25 | 29-30 | 9-20 | 1,606-2,344 |
| `lunge` | 3 | 0.84-0.89 | -6.0 to -4.6 | 0.8-1.7 | 2-6 | 1-2 | 0-7 | 2-3 | 49-63 | 24-40 | 3,294-3,543 |
| `egg_launch` | 3 | 0.37-0.38 | -5.9 to -4.0 | 0.2-0.5 | 3-5 | 4-12 | 13 | 16-31 | 31-41 | 8-20 | 1,605-2,474 |
| `egg_land` | 3 | 0.34-0.35 | -5.9 to -3.8 | 3.4-10.2 | 7-13 | 11-15 | 11-21 | 16-31 | 15-23 | 13-14 | 1,598-1,915 |
| `egg_pulse` | 3 | 0.24-0.25 | -7.4 to -5.8 | 6.0-14.0 | 39-67 | 15-33 | 6-10 | 3-10 | 1-2 | 0 | 192-292 |
| `egg_burst` | 3 | 0.78-0.80 | -7.7 to -3.4 | 0.7-1.2 | 1 | 3-9 | 18-28 | 26-38 | 29-33 | 4-5 | 1,457-1,492 |
| `hatch_buzz` | 3 | 1.40 | -5.1 to -3.4 | 0.1-0.2 | 0 | 6-10 | 31-40 | 40-48 | 6-7 | 6-7 | 1,192-1,267 |
| `needle_shot` | 4 | 0.15-0.20 | -9.3 to -4.3 | 0.0 | 0 | 0-1 | 14-20 | 16-32 | 26-36 | 17-39 | 2,208-3,303 |
| `needle_ground` | 4 | 0.15-0.23 | -3.9 to -3.4 | 0.7-1.7 | 4-24 | 21-37 | 21-47 | 7-16 | 6-13 | 4-8 | 780-1,325 |
| `needle_flesh` | 3 | 0.30 | -5.3 to -3.0 | 0.4-0.7 | 4-18 | 19-22 | 21-27 | 8-21 | 16-24 | 11-17 | 1,619-1,944 |
| `zip_whir` | 3 | 1.10 | -7.8 to -3.7 | 0.1 | 0 | 6-7 | 34-44 | 35-43 | 8-10 | 5 | 1,069-1,110 |
| `alert` | 2 | 1.05-1.20 | -5.1 to -4.1 | 0.0 | 0 | 1 | 8-11 | 10-31 | 52-75 | 4-7 | 1,722-2,150 |

Against the game's Deathsquito (the codex's features, `sfx_features.measure`):

| Queen | Deathsquito | What changed |
| --- | --- | --- |
| `buzz_loop`: pitch 136 Hz, centroid 813 Hz, 38 % under 300 Hz, 0 % under 80 | wing loop: 202 Hz, 1,480 Hz, 15 %, 7 % (hiss) | a fifth deeper, more body, no sub-bass |
| `lunge`: centroid 3,300-3,540 Hz, 0.88 s | attack: 3,680-3,990 Hz, 0.71 s | lower and longer, the same sting |
| `pierce` / `needle_flesh`: centroid 1,520-2,340 Hz, 0.4-5.7 % under 80 | hit splats Gib02/17: 1,590-2,100 Hz, 11-20 % | the same splat, less sub-bass |

## Checks

- **Rings** (a narrow spectral line 12 dB over its third-octave neighbourhood for 60 ms or more, above 1 kHz): none
  in the impacts, eggs or needles. The wing cues show the buzz's own harmonics, as the Deathsquito's loop
  does; the alert shows the Seeker's screech. Two faint lines left in: a 6 kHz air whistle in `dive_whoosh_2`
  (21 dB under the frame's loudest, the spear's own) and a 15.4 kHz line in `pierce_2` (40 dB under). The lunge keeps
  the Deathsquito's attack whine (3.9 kHz here, 10 dB over its surroundings).
- **Loop seam**: `buzz_loop_1` -6.1 dB (seamless). Keep its strips exactly 120 frames apart.
- **Spectrograms** were compared with the Deathsquito's clips (the codex's `sfx_draw` tiles): the wing cues show the
  same harmonic stripes lower down; the glides and the whirl's swing show as rising and waving stripes; the burst as
  a sharp onset decaying over 0.8 s.
- **Not listened to.** Not tried in game.

## Not found in the game

- No egg-laying or hatching sound of its own: the burst borrows the game's Seeker-egg burst and the beehive's crack;
  the throb borrows a body punch.
- No heartbeat sound.
- No voice for the Deathsquito: the alert is a giant beetle's growl and a Seeker's screech.
- No needle or dart: the needles are the arrow's hit and the claw swipe.

## For a mod later

The same recipes with the game's clips by name, through copies of sound prefabs (`BundlePrefabs.SfxPrefabs`),
plus a small player for what ZSFX cannot do on its own: a start time into the clip, a delay per layer, per-frame pitch
ramps (`buzz_strain`, `hatch_buzz`, `zip_whir`), volume ramps (`egg_burst`, the glide cues), and
`AudioHighPassFilter`/`AudioLowPassFilter` on each layer. The loop is the Deathsquito's wing clip with
`AudioSource.loop` at pitch 0.672. `ecp_headsman/sfx_table.py` shows how the preview's recipes became a mod's table.

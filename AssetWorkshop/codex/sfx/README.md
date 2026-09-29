# Sounds

How Valheim's sounds are made, set up and mixed, measured from the game's own files, and how to make a new one that
sits beside them. Claude cannot listen, so every statement here comes from numbers (`measure/sfx*.py`, written to
`data/sfx.json`) and from looking at spectrograms and envelopes (`codex/out/sfx/`, gitignored). Nobody has listened to
the workshop's own sounds yet either: the numbers say they sit in the game's ranges, not that they sound right.

| Page | What it holds |
| --- | --- |
| [archetypes.md](archetypes.md) | every archetype (a creature's idle, a sword hit, a fire loop...): length, level, timing, spectrum, variations, pitch spread, reach, and its texture in words |
| [creatures.md](creatures.md) | creature voices: what each creature has, voice character by size, how variants reuse clips, bosses |
| [materials.md](materials.md) | hits, breaks, placing and footsteps by material, and the layer balance that makes each material |
| [mixing.md](mixing.md) | the mixer, the 3D settings, ZSFX's playback rules, the loudness order of everything, import settings, networking |
| [catalogue.md](catalogue.md) | which game sound prefab to reuse for which purpose, by name |

The pipeline that turns this into sounds is `AssetWorkshop/sfx/` (its README has the commands).

## What a game sound is

A sound in Valheim is a **sound prefab**: a GameObject with a `ZSFX` (the game's sound component), an `AudioSource`, a
`TimedDestruction` and a `ZNetView`. Something plays it by spawning the prefab through an `EffectList` (a creature's
`m_hitEffects`, an item's `m_triggerEffect`, a piece's `m_placeEffect`), a `FootStep` entry or an animation event
(`Effect` on the clip, with the prefab as its object parameter). Measured over the export: 974 `sfx_` prefabs,
4,665 sound nodes in 1,763 files, 1,292 distinct sounds once copies are merged (the same `sfx_fire_loop` sits in about
150 location and room prefabs), 4,468 clips, 7.37 hours.

- **ZSFX plays one clip at random** from `m_audioClips` (the variations), at a random pitch between `m_minPitch` and
  `m_maxPitch` and a random volume between `m_minVol` and `m_maxVol`. Pitch here is Unity's: 2.0 is an octave up and
  half as long. Median: 3 clips a sound (406 of 1,292 have one, loops nearly always), pitch 0.95 to 1.05 (a spread
  of 1.8 semitones; a quarter of sounds use none, a quarter 3.5 or more), volume 0.92.
- **It limits repeats.** `AudioMan` refuses a one-shot when `m_maxConcurrentSources` copies of the same sound (by
  `m_hash`) started within 0.1 s (`m_concurrencyThreshold` on the `_AudioManager` prefab) closer than
  max(1 m, min distance); 0 means no limit for one-shots (1,064 of 1,242 ZSFX), the rest use 1 to 3. For loops it
  keeps the nearest `m_maxConcurrentSources` + 1 copies audible (0 keeps only the nearest; -1 is unlimited).
- **It adds space with distance.** Every sound with `m_distanceReverb` (1,215 of 1,242) sends more to the reverb and
  spreads wider (45 to 120 degrees) as it gets further, up to 64 m (`m_customReverbDistance` on 8). The mixer's SFX
  group already reverberates everything (0.5 s). So the clips themselves are dry: bake no room into a new sound.
- **Captions.** `m_closedCaptionToken` names the source (`$enemy_greydwarf`), `m_secondaryCaptionToken` the action
  (`$caption_attacking`, `$caption_alerted`, `$caption_dying`, `$caption_hurt`...), `m_captionType` sorts them
  (0 misc, 1 wildlife and enemy idles, 2 enemy, 3 boss). A copy of a creature's sound keeps its caption: change it.
- **The AudioSource** holds the 3D side: fully 3D (spatial blend 1 on 1,235 of 1,292), roll-off (custom curve 641,
  logarithmic 498, linear 153), min and max distance (reach), the mixer group (SFX 1,112, SFX_LARGE 144 for big
  things), priority 128, Doppler 1. `m_PlayOnAwake` is off: ZSFX starts it.
- **It is networked.** The `ZNetView` means the machine that plays an effect creates a ZDO for the sound, and every
  peer near it instantiates the prefab from that ZDO by its name's hash: 790 of the 973 `sfx_` prefabs carry one and
  all 790 are listed in `ZNetScene.m_prefabs`; the other 183 (interface sounds, sounds nested inside bigger prefabs)
  play locally. A new networked sound prefab must be registered in ZNetScene on every peer, or other players get a
  missing-prefab error instead of the sound. `TimedDestruction` (3 to 5 s on most) removes it; the owner destroys
  the networked copy.

## Making a new sound

1. **Decide the archetype and look it up.** Find the row in [archetypes.md](archetypes.md) and read its texture
   words; for a creature, read [creatures.md](creatures.md) for the nearest creature of that size; for an impact,
   [materials.md](materials.md). Note the numbers to hit: length, played level, attack, tail, centroid, flatness,
   band split, variation count, pitch spread.
2. **Reuse first.** If a game sound already fits, play it by name ([catalogue.md](catalogue.md)): no bundle, no risk,
   exactly the game's sound. Next best: the game's clips at another pitch (the Greyling is the Greydwarf's clips at
   1.8 to 2.2; see creatures.md). Only then make new clips.
3. **Pick the game prefab to copy.** A new sound plays through a copy of a sound-only game prefab of the same
   archetype (one ZSFX, no particles): it keeps that prefab's reach, roll-off, mixer group, reverb and concurrency.
   Choose one whose reach and group suit; override pitch, volume and caption where the new sound differs.
4. **Write the sound set** in `AssetWorkshop/sfx/sounds/<set>.py` from the recipes (voice, impact, creak, swing,
   fire) or the kit, and build it: `AssetWorkshop/out/venv/Scripts/python AssetWorkshop/sfx/build.py <set>`. The build
   levels every clip so that, through the copied prefab, it plays at the archetype's median in-game level.
5. **Read the report and the sheets.** `sfx/out/<set>/report.md` lists every number against the archetype (ok inside
   the middle half, near inside the range); `<sound>_compare.png` shows the variations above the nearest game clips.
   Fix what is off in the recipe's parameters and build again; most sounds need two to four passes (a build takes
   seconds).
6. **Bundle and use it.** `.\sfx\bundle.ps1 -Set <set>` builds `out/bundles/<set>_sfx.windows` and `.linux`; the mod
   embeds both and calls `BundlePrefabs.SfxPrefabs.Copy` inside `NetPrefabs.OnSceneAwake` (see the BundlePrefabs
   README).

## Rules for a new sound

- **Licence-clean sources only**: the game's own clips played by name at runtime, recordings the user makes, or CC0.
  Never AI audio generators, never the game's audio in a bundle. Synthesis (this workshop's kit) is for placeholders
  only (next rule).
- **44.1 kHz** (4,187 of 4,468 game clips; 273 are 48 kHz), **16-bit WAV** as the bundle's source (Unity encodes
  Vorbis itself), **mono** for anything played in 3D (2,974 game clips are mono; the stereo ones are music, ambience,
  UI and big one-shots).
- **Dry.** No baked reverb beyond the sound's own body (a cave creature's echo is part of it; a room is not).
- **Variations like the game's.** Three to five clips for one-shots (creature idles six, footsteps eight to twenty),
  one for loops; different recordings (seeds), not pitch copies: ZSFX adds the pitch spread.
- **Level like the game's**: see the played levels in [mixing.md](mixing.md). A sound twice as loud as its archetype
  is the most common way a mod sound stands out.
- **Real recordings, not synthesis.** The user has listened: synthesised swings, impacts, cracks and bone clatter
  (damped sines and swept noise) sounded "comically bad ... like cartoon noises" that "don't fit Valheim at all"
  (2026-09-29, the headsman boss). A shipped sound is a real recording: the game's own clips played at runtime,
  chosen for the event and the creature's size and trimmed, pitched and filtered there (`SfxPrefabs.Variant` and
  `Copy` with game clips; clip start, delay, volume, pitch, AudioHighPassFilter/AudioLowPassFilter), a CC0 recording,
  or one the user makes. The synthesis kit in `AssetWorkshop/sfx` makes placeholders only.
- **Match the source to the creature's size.** The game's troll and sledge swings carry 70 to 93 % of their energy
  below 80 Hz, "way too much bass" on a skeleton-sized creature: measure a candidate's sub-80 Hz share and centroid
  (`codex/data/sfx.json`, `sfx/compare.py`) and pick from creatures of the same size (`creatures.md`).
- **Cut the rings.** A narrow spectral line lasting 60 ms or more is a ringing tone, and it reads as a metal "ting"
  where none belongs: the Stone Golem's spike wall rings at 4 kHz, and the Skeleton's melee swings end in a sword ring
  about 0.15 s after their peak (cut them short).
- **Numbers in range are not enough: compare the spectrograms.** The report's numbers (length, level, centroid,
  flatness) can all pass while the sound is plainly different: the synthesised Rootling deaths passed every number,
  yet their compare sheet shows two short clean bursts with evenly spaced harmonic stripes where the game's deaths are
  one long, dense, rough vocalisation that holds and then fades.

## How the numbers were measured

`measure/sfx_features.py` measures every clip the same way, the game's and the workshop's: loudness after ITU-R
BS.1770-4 (K-weighting, 400 ms blocks, gating), on the mid of stereo clips; timing from a 10 ms RMS envelope (attack
from the onset to within 3 dB of the peak, tail to 40 dB under it); spectrum as the energy-weighted mean over the loud
frames (centroid, bandwidth, low under 300 Hz / mid 300 to 3000 / high over 3000); flatness as octave-band Wiener
entropy (about 0.56 for any noise, near 0 for a pure tone, 0.1 to 0.3 for voices and ringing metal); pitch by YIN.
"Played" level is the clip's momentary maximum (loudest 400 ms) plus the gain its ZSFX volume and AudioSource volume
give it: the level the game plays it at before distance and the mixer. Stats are over clips unless a column says
sounds.

```
AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx.py [--rescan]    data/sfx.json (30 s from caches)
AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_sheets.py [key]  contact sheets in codex/out/sfx
AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_tables.py archetypes|creatures|mixer|references
```

The measuring scripts need the workshop's venv (`AssetWorkshop/out/venv`, `pip install -r AssetWorkshop/sfx/
requirements.txt`: numpy, Pillow, soundfile). scipy is not used: Windows Application Control blocks its compiled
modules on this machine, so IIR filters run as exact FFT convolutions with their impulse responses.

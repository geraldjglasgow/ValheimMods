# Mixing: how the game levels, places and processes its sounds

The mixer, the 3D settings on the AudioSources, ZSFX's playback rules, the loudness order of everything, how clips are
imported and how sounds travel between players. Measured from `Audio/MasterMixer.mixer`, every sound prefab, the
`_AudioManager` prefab and the decompiled `ZSFX`, `AudioMan`, `BaseAI` and `ZNetView`.
`measure/sfx_tables.py mixer` prints the mixer again.

## The loudness order

The level each archetype plays at before distance (median played LUFS: the clip's loudest 400 ms plus the gain of its
ZSFX volume, which ZSFX writes over the AudioSource's own and which Unity clamps at 1), loudest first. A new sound
belongs at its archetype's level; `sfx/build.py` puts it there.

| played LUFS | what |
| --- | --- |
| -7 to -10 | the loudest single sounds: boss calls and events (Eikthyr's and the Elder's alerts -7.8, Fader's death explosion -7.6 and meteor call -8.8), the lightning staff (-8.8), an exploding rock (-9.5), adding fuel to a fire (-10.0) |
| -10 to -14 | altar summons (-10.7), spawns (-11.0), the sledge's hit (-11.5), projectile impacts (-12.8), creature deaths (-13.0) and attacks (-13.3), staff casts (-13.2) |
| -14 to -18 | breaking wood (-14.2) and stone (-15.4 to -16.9), area attacks (-14.6), a wooden block (-15.2), weapon hits (club -15.4, sword -15.8, spear, battleaxe and greatsword -16.0), creature alerts (-15.7) and hurts (-17.2), metal blocks (-17.9) |
| -18 to -24 | music (-18.1), creature attack hits (-18.2), status effects (-18.5), doors (-19.1), wood hits (-19.2), creature idles (-20.3), placing pieces (-20.8 to -22.3), the axe's hit (-22.6), drinking (-22.9), equipping (-23.4), chests (-23.8), sword swings (-24.0) |
| -24 to -30 | creature footsteps (-28.3), light swings (unarmed -27.0, axe -27.9, polearm -28.1) |
| -30 to -43 | fire loops (-32.3), eating (-32.3), picking up (-34.2), random ambience (-34.8), the environment's bed (-35.3), the player's own footsteps (walk -34 to -42, run -31 to -37), the knife's swing (-39.1) |

Two rules fall out of it. The player's own actions are quiet (their footsteps, swings and pickups sit 10 to 25 dB
under the creatures around them), and what threatens is loud. Sounds that repeat are quieter than sounds that happen
once (idle under alert, footsteps under everything).

## The mixer

`Master` > `Effects` > `SFX` > `SFX_LARGE`, `Effects` > `Ambient`, and beside them `GUI`, `Music`, `Music_ontop`,
`Haptics`. Every group's volume is 0 dB in the Default snapshot except Ambient (-4 dB). A sound's AudioSource picks its
group: SFX for 1,112 of 1,292 distinct sounds, SFX_LARGE 144, Music_ontop 18 (location music), GUI 12. The effects
volume setting moves `Effects` and `GUI` together (`AudioMan.SetSFXVolume` sets both exposed volumes).

| group | chain (Default snapshot) |
| --- | --- |
| Master | high-pass 25 Hz; compressor threshold -1 dB, attack 50 ms, release 102 ms: a safety limiter |
| SFX | compressor threshold -9.8 dB, attack 10 ms, release 154 ms, make-up +1.6 dB; EQ +1.2 dB around 12 kHz (3 octaves wide), -1.8 dB at 1,584 Hz (0.65 octave), -1.9 dB at 401 Hz (1.2 octaves); SFX Reverb: dry 0 dB, room -7.1 dB, decay 0.5 s, highs decaying 0.16 as long, room under 250 Hz -46.5 dB (no low reverb), reflections -14.5 dB at 64 ms, late reverb +2.9 dB at 48 ms |
| SFX_LARGE (inside SFX) | EQ +1.6 dB around 8.4 kHz (3 octaves), -1.9 dB at 420 Hz (1 octave); compressor threshold -1.1 dB, 50/50 ms; SFX Reverb: room -10 dB (-5 dB on the highs), decay 3.5 s, highs 0.7 as long, reflections -12.3 dB, late reverb -11.3 dB at 29 ms |
| Ambient | EQ -2.0 dB at 223 Hz (2 octaves); low-pass 11 kHz; compressor threshold -10.4 dB, attack 72 ms, release 226 ms, +3 dB; low-pass 15 kHz |
| Music | duck volume (threshold -80 dB, ratio 4) keyed from Music_ontop's send: location music pushes the main music down |
| GUI, Haptics, Music_ontop | no processing (Music_ontop sends to the duck) |

What this means for a new sound:

- **Do not bake a room in.** Everything in SFX already gets a 0.5 s room with dark tails and no low reverb, and
  SFX_LARGE adds a 3.5 s hall on top (a SFX_LARGE sound passes through both groups). Keep clips dry; give a big
  creature or a boss its size by routing it through a SFX_LARGE prefab, not by adding reverb.
- **Do not be louder than the archetype.** The SFX compressor starts at -9.8 dB on the bus: a sound 6 dB hotter than
  its neighbours both sticks out and pulls the whole bus down (pumping) for 150 ms.
- **The EQ scoops 400 Hz and 1.6 kHz and lifts the air.** A new sound built to the archetype's spectrum gets the same
  treatment; do not pre-compensate.

### Snapshots

`AudioMan` switches between three snapshots over 1.5 s (`m_snapshotTransitionTime`):

- **Default** as above.
- **Indoor** (when AudioMan's indoor flag is set): both SFX reverbs are switched off (room -100 dB), the ambience is
  low-passed to 5 kHz and 8.2 kHz, and the music duck is stronger (threshold -65 dB, ratio 10).
- **Menu**: SFX and Ambient down 15 dB, the ambience low-passed to 5 kHz.

## Where a sound sits in 3D

Over the 1,292 distinct sounds' AudioSources:

- **Spatial blend 1** (fully 3D) on 1,235; 2D (0) on 39: the location music, the interface, and the player's own
  pickup, drop, level-up and achievement sounds (heard in the head, not placed); a few use curves.
- **Roll-off**: custom curve 641, logarithmic 498, linear 153. Two custom curves cover 406 of the 641 (times are
  distance over max distance):
  - `(0.1, 1) (0.2, 0.5) (0.4, 0.25) (0.8, 0.125) (1, 0)` on 233: full level to a tenth of the reach, then half per
    doubling of distance (inverse distance, -6 dB per doubling) and cut to silence at the reach. Creature sounds.
  - `(0, 1) (0.25, 0.47) (0.42, 0.2) (0.73, 0.06) (1, 0)` on 173: falling from the start, half by a quarter of the
    reach, a tenth by about 0.45 of it.
  Logarithmic sounds (Unity: min distance over distance, and they stop falling at max distance, never going silent)
  are the footsteps, placing and close interactions; linear ones (1 at min distance to 0 at max) swings, UI and blocks.
- **Min distance** 5 m (505) or 1 m (391); **max distance** (reach) 100 m (215), 30 (212), 50 (155), 20 (137),
  40 (125), 80 (74). Reach follows importance, not loudness: creature idles and alerts 60 to 100 m, attacks 80, deaths
  50, hurts 40, weapon swings 20, footsteps 20 to 40, fires 15, thunder 1,000, fireworks 400. Each sound's distances
  where it falls to -6, -20 and -40 dB are in `data/sfx.json` (`half_m`, `tenth_m`, `gone_m`).
- **Spread** 0 on 726 (a point), 90 or 120 degrees on 131 (big things, water, loops); ZSFX widens it with distance.
- **Doppler** 1 on 1,254; **priority** 128 on 1,273 (0, never culled, on the 18 location music sources).

## How ZSFX plays a sound

From the decompiled `ZSFX` and `AudioMan` (values from `Systems/_AudioManager.prefab`, which differ from the code's
defaults):

- One clip at random from `m_audioClips`; pitch uniform in [`m_minPitch`, `m_maxPitch`] (1,242 ZSFX: median spread
  1.8 semitones; 0 on a quarter, 3.5 or more on a quarter); volume uniform in [`m_minVol`, `m_maxVol`] (median 0.92;
  footsteps and swings 0.3 to 0.8, blocks up to 1.5). `m_randomPan` is never used. `m_minDelay`/`m_maxDelay` delay
  the start on 97 (0.1 to 3 s): layered effects are staggered this way.
- **Concurrency**: a one-shot is refused when `m_maxConcurrentSources` copies with the same `m_hash` started less than
  0.1 s ago (`m_concurrencyThreshold`) within max(1 m, min distance), or anywhere with `m_ignoreConcurrencyDistance`
  (82 sounds). 0 means unlimited (1,064); 1 to 3 on most of the rest. Loops: the nearest `m_maxConcurrentSources` + 1
  copies of the same loop stay audible, the rest fade out over 0.5 s (fires in pieces use 2 to 6; 0 keeps only the
  nearest). The hash is per prefab: a copied prefab that keeps the original's hash shares its limit.
- **Fades**: 52 fade in, 55 fade out, 19 fade out on their own after `m_fadeOutDelay` (charging and looping
  effects that end themselves).
- **Distance reverb** (1,215 of 1,242): the send to the scene's reverb zones rises with distance up to 64 m (or
  `m_customReverbDistance`), and the spread widens from 45 to 120 degrees; loops update it every second.
- **Captions**: `m_closedCaptionToken` (the source), `m_secondaryCaptionToken` (the action, over sound nodes:
  `$caption_attacking` 122, `$caption_dying` 48, `$caption_alerted` 47, `$caption_hurt` 10...), `m_captionType` (over
  distinct sounds: 0 misc 754, 1 wildlife and enemy idles 60, 2 enemy 306, 3 boss 122), shown when the sound reaches
  `m_minimumCaptionVolume` (0.3) after distance.
- **Vibration** (`m_useVibration`, 427): controller rumble from the clip on consoles; many sounds have a
  `_vibration_only` twin with the same timing and no audio (`sfx_hit_vibration_only`, `sfx_pickaxe_hit_vibration_only`).

## How sounds travel between players

- A sound prefab with a `ZNetView` (790 of the 973 `sfx_` prefabs; all 790 are in `ZNetScene.m_prefabs`) becomes a
  networked object when an `EffectList` spawns it: the machine that plays the effect creates its ZDO, and every peer in
  range instantiates the prefab from it by its name's hash. So an effect played on one machine (a creature's hit on its
  owner) is heard by everyone, and **a new sound prefab must be registered in ZNetScene on every peer**
  (`BundlePrefabs.SfxPrefabs` does this through `NetPrefabs`).
- `BaseAI` rolls idle sounds on every peer that has the creature loaded (`DoIdleSound` is not limited to the owner),
  and alerts come from the owner's AI.
- Interface sounds and the random ambience are local (no ZNetView, or instantiated by `AudioMan` on the local machine).
- A mod playing a sound on one machine only, for its own reasons, uses `LocalEffects.LocalEffect.Sound`, which strips
  the ZNetView before the copy wakes.

## How clips are imported

All 4,468 clips are Vorbis. Load type by length (from the export's .meta files):

| length | compressed in memory | decompress on load | streaming |
| --- | --- | --- | --- |
| under 1 s | 831 | 390 | 0 |
| 1 to 10 s | 2,821 | 166 | 67 |
| 10 s and over | 95 | 2 | 96 |

Sample rate: 44.1 kHz on 4,187, 48 kHz on 273, 32 kHz on 8. Channels: mono 2,974, stereo 1,494 (music, ambience,
interface, ships, big one-shots). The workshop's `Workshop.Sfx.SfxImport` imports new clips as Vorbis at quality 0.6,
decompressed on load under 1 s and compressed in memory otherwise (a bundle loaded from memory is not streamed), sample
rate kept, preloaded.

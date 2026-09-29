# Sounds

New sounds for the mods, made from scratch to sit beside Valheim's own: synthesised in numpy from recipes built on the
game's measured sounds, levelled to the game's mix, compared with the game's nearest clips, and bundled for Unity so a
mod plays them through copies of the game's own sound prefabs. The knowledge behind it is the codex's sound pages
(`AssetWorkshop/codex/sfx/`, start with its README); this folder is the pipeline.

Nobody has listened to anything made here yet, and nothing has been tried in the game. The numbers say the example
sounds sit in the game's ranges; ears have the last word.

**Synthesised sounds are placeholders, not shipped sounds.** The user listened to synthesised swings, impacts, cracks
and bone clatter made the same way for the headsman boss and called them "comically bad ... like cartoon noises"
(2026-09-29). A shipped sound is a real recording: the game's own clips at runtime (chosen for the event and the
creature's size, trimmed, pitched and filtered there: `BundlePrefabs.SfxPrefabs.Variant` / `Copy`, worked recipes in
`assets/ecp_headsman/sfx.py`), CC0, or the user's own. What stays useful here for that: the measuring (`codex/data/
sfx.json`), levelling to the archetype (`kit/level.py`, `kit/targets.py`), the compare sheets (`compare.py`) and the
bundling of recorded clips (`bundle.ps1`).

```
sfx/
  build.py           render a set's sounds, level them, write WAV and OGG, compare (python sfx/build.py <set>)
  compare.py         re-run the comparison of a built set (report and sheets)
  bundle.ps1         stage a built set in Unity and build its bundle for Windows and Linux
  requirements.txt   numpy, Pillow, soundfile for the venv
  kit/               the synthesis kit (plain numpy)
  recipes/           sound kinds with the game's shapes built in: voice, impact, creak, swing, fire
  sounds/            sound sets: one module per set (rootling, crystal, fire_loop are the worked examples)
  out/<set>/         build output (gitignored): clips, manifest.json, report.md, *_compare.png
```

## Setting up

```
python -m venv AssetWorkshop/out/venv
AssetWorkshop/out/venv/Scripts/python -m pip install -r AssetWorkshop/sfx/requirements.txt
```

Everything runs with that Python. soundfile (libsndfile) reads the game's OGG clips and writes OGG Vorbis; scipy is
not used, because Windows Application Control blocks its compiled modules on this machine (the import fails after a
minute). Filters therefore run as exact FFT convolutions with their impulse responses (`kit.filters.apply`), or
sample by sample where they must move (`kit.filters.svf`). The comparison reads the game's clips from the local
reference export (`%USERPROFILE%\ValheimReference`) and the codex's `data/sfx.json`.

## Making a sound

1. Read the codex first: the archetype's row and words (`codex/sfx/archetypes.md`), the creature or material page,
   and the catalogue (`codex/sfx/catalogue.md`) for the game prefab to play it through. Reuse a game sound or make a
   pitch variant when one fits: no new audio is best.
2. Write `sounds/<set>.py`:

   ```python
   from recipes import voice
   SET = "ecp_rootling"                     # the bundle is <SET>_sfx; clip names start with the sound names
   CAPTION = "$enemy_ecp_rootling"          # the caption token copies get (optional)
   SOUNDS = [
       {"name": "ecp_rootling_idle",        # unique across mods: prefix it
        "archetype": "sfx.creature.idle",   # the codex category it is measured against
        "prefab": "sfx_greydwarf_idle",     # the game sound prefab a mod copies to play it
        "pitch": [0.95, 1.05],              # ZSFX overrides for the copy (optional; also "volume")
        "variations": 6,                    # clips, as many as the archetype's median
        "make": lambda seed: voice.voice("idle", size=0.6, seed=seed)},
   ]
   ```

   Add `"loop": True` for a loop (levelled by integrated loudness, checked for a seam).
3. Build: `AssetWorkshop/out/venv/Scripts/python AssetWorkshop/sfx/build.py <set>` (`--only <sound,...>` rebuilds
   some, `--no-ogg` skips the OGG copies). Each variation is rendered with its own seed and levelled so that, played
   through a copy of `prefab` (with its ZSFX volume, or `volume`), it sits at the archetype's median played level.
4. Read `out/<set>/report.md`: every measured number of the sound (medians of its variations) against the
   archetype's middle half and range, `ok`, `near` (inside the range) or `low`/`high`; the nearest game clips by name;
   the loop seam for loops. Look at `out/<set>/<sound>_compare.png`: the variations' spectrograms and envelopes on top,
   the nearest game clips below, on one time scale.
5. Change the recipe's parameters and build again until the report and the picture agree with the game. A build takes
   seconds; expect two to four passes.
6. Bundle: `.\sfx\bundle.ps1 -Set <set>` (PowerShell). It waits for `out\unity.lock` (a folder made atomically; other
   workshop builds use it too), stages the WAVs and manifest in `unity\Assets\Bundles\Sfx\<bundle>\`, runs
   `Workshop.Sfx.SfxBundle.Run` in batch mode, releases the lock, and writes `out\bundles\<set>_sfx.windows` and
   `.linux`. Unity must not have the project open.
7. In the mod: embed both bundle files and copy the game prefab with the new clips (see the BundlePrefabs README):

   ```csharp
   NetPrefabs.OnSceneAwake(harmony, scene =>
   {
       AssetBundle sounds = EmbeddedBundle.Load(typeof(Plugin).Assembly, "ecp_rootling_sfx");
       GameObject idle = SfxPrefabs.Copy(scene, "sfx_greydwarf_idle", "ecp_rootling_idle", sounds,
           new[] { "ecp_rootling_idle_1", "ecp_rootling_idle_2", "ecp_rootling_idle_3",
                   "ecp_rootling_idle_4", "ecp_rootling_idle_5", "ecp_rootling_idle_6" },
           new SfxSettings { MinPitch = 0.95f, MaxPitch = 1.05f, Caption = "$enemy_ecp_rootling" });
       // then point the creature's BaseAI.m_idleSound at it
   });
   ```

   The manifest (`out/<set>/manifest.json`) lists every sound's clips, prefab, overrides and caption for this code.

## The kit

| module | what |
| --- | --- |
| `core` | the rate (44.1 kHz, as 94 % of the game's clips), time, gain, fades, mixing layers, placing, trimming, stereo |
| `osc` | sine, polyBLEP saw and square, triangle, a glottal pulse (Rosenberg derivative) for voices, FM, modal partials with detuned pairs |
| `noise` | white and coloured noise (exact slope in dB an octave), velvet noise, random events (Poisson, and thinning for showers), bursts, crackle |
| `filters` | RBJ biquads (low/high/band pass, notch, peak, shelves) applied exactly; a time-varying state-variable filter; a spectral filter with any response over time, formant banks and tilts |
| `env` | breakpoints, attack-release, ADSR, swells, glides, vibrato, slow random wander, decays |
| `shape` | tanh drive with bias, folding, bit crushing, roughness (20 to 70 Hz modulation), ring modulation |
| `grain` | scattering sounds in time, granular clouds, bouncing times |
| `reverb` | an eight-line feedback delay network (Householder matrix, per-line damping), early reflections, a room; mostly not needed, since the game's mixer adds the room |
| `resample` | windowed-sinc reading, pitch shift the way Unity's pitch does, varispeed, rate change |
| `level` | momentary and integrated loudness (the codex's BS.1770 code), a look-ahead limiter, a compressor, peak-to-loudness limiting, matching to a target |
| `io` | 16-bit WAV with dither, OGG Vorbis |
| `targets` | the codex's numbers: an archetype, a game sound's summary, the level a clip needs through a copied prefab, the report, the nearest game clips |

## The recipes

- `voice.voice(role, size, seed, base_hz, grit)`: idle, alert, attack, hurt, death. A glottal source with jitter,
  vibrato, a subharmonic growl, rasp and pulsed breath, through a chest resonance and four formants scaled by size,
  opening with the envelope; per-syllable pitch arches; legato idles. Size 1 is a Greydwarf-sized body.
- `impact.hit(material, seed, size)`, `impact.shatter(material, seed, size, seconds)`: crystal, ice, metal, stone,
  wood, in layers (click, ring, grit, crunch, thud, rumble) whose balance is the material (codex/sfx/materials.md),
  compressed and limited to the game's crest.
- `creak.creak(...)`: stick-slip friction through wooden resonances: doors, hulls, living wood.
- `swing.whoosh(seed, weight, whistle)`: a swing's band-passed air rising and falling with the blade's speed.
- `fire.fire_loop(seconds, seed, intensity)`: roar, flicker, hiss, crackle and pops, crossfaded end into start.

## The worked examples

| set | sounds | copied prefab | result (report.md after the last pass) |
| --- | --- | --- | --- |
| `rootling` | the Rootling's idle (6), alert (4), attack (3), hurt (5), death (3): a small wooden creature, a voice at size 0.6 under creaks that follow it | `sfx_greydwarf_idle`, `_alerted`, `_attack`, `_hit`, `_death`, pitch set back to about 1 | played level, centroid and flatness in range or near for every role; attacks and hurts shorter than the archetype's median (0.9 and 0.6 s); a small creature's band split (little under 300 Hz) |
| `crystal` | a crystal struck (5) and breaking (4) | `sfx_ice_hit`, `sfx_ice_destroyed` | the break in range for length, level, band split and crest, its attack and tail late and a little too noisy; the strike brighter than the game's ice hit (see codex/sfx/materials.md) |
| `fire_loop` | a 10 s fire loop (1) | `sfx_fire_loop` | in range for length, level, decay and band split, a touch bright and noisy; loop seam -11.7 dB against the clip's own sample steps (seamless) |

The Rootling set is bundled (`ecp_rootling_sfx`, 21 clips, about 214 kB a platform); no mod uses it yet.

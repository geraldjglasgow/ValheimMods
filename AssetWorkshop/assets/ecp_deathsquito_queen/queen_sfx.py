"""The Deathsquito Queen's sounds for the workshop preview: the game's own recordings, chosen for each move and sized
for her (the Deathsquito grown 2.5 times: deeper and heavier than the Deathsquito, but nowhere near a troll's or a
sledge's bass, whose clips carry 70-93 % of their energy under 80 Hz).

Every cue is layers of real Valheim clips (never synthesized: the user found synthesized sounds "like cartoon
noises"), started where their peak lands on the moment, trimmed, pitched and high-passed just enough to take the
sub-bass off. Only what a mod can do at runtime with the game's clips: a volume (and a volume ramp), a start time
into the clip and a delay, a pitch (AudioSource.pitch, and for a few cues a pitch ramped frame by frame), and high-
and low-pass filters (AudioHighPassFilter, AudioLowPassFilter). The files written here are a preview mix; a mod would
play the same recipes with the game's clips and never ship audio. Nobody has listened to them yet.

Run in Blender, whose sound library decodes the game's .ogg files:
    blender --background --factory-startup --python queen_sfx.py -- <out dir>
(the workshop venv's python runs it too, decoding with soundfile). Writes <out dir>/build_<time>/<cue>_<n>.wav
(default out dir: out/sfx next to this file), the preview places them by cue name (SOUNDS.md has the table), and
reports each with its length, peak and energy per band.
"""
import collections
import os
import shutil
import sys
import time
import wave

import numpy as np

SR = 48000
REFERENCE = os.path.join(os.path.expanduser('~'), 'ValheimReference', 'ExportedProject', 'Assets')
BANDS = (0, 80, 160, 300, 600, 1500, 4000, SR // 2)
KEEP = 3                       # older build folders kept
LOUDNESS, PEAK = 0.12, 0.9     # the loudest 0.1 s is brought to this RMS, unless the peak would pass PEAK

_DS = 'Characters/Deathsquito/fx/wav/'
WINGS = _DS + 'Insect_Wasp_WingsLoop3.ogg'     # the Deathsquito's own wing buzz, a 2.69 s loop, 203 Hz fundamental
GIB = _DS + 'Blood_Splat_Gib%02d.ogg'          # the Deathsquito's own hit splats (02, 17)
STING = _DS + 'sScorpionTailAttack0%d.ogg'     # the Deathsquito's own attack: a hissing whoosh and a thump
HOVER = 0.672                                  # her wings: the Deathsquito's loop 6.9 semitones down (136 Hz)
LOOPED = {WINGS}                               # clips a layer plays with AudioSource.loop on (read round and round)
SPEAR_THROW = 'Characters/Player/audio/wav/weapons/spear/Player_Movement_SpearThrow_M_0%d.ogg'
SPEAR_HIT = 'GameElements/Items/weapons/_res/spear/sfx/Player_Movement_Spear_Hit_M_0%d.ogg'
ARROW_HIT = 'Characters/Player/audio/wav/weapons/bow/Weapons_Arrow_Hit_M_0%d.ogg'
MUD = 'Characters/Player/audio/wav/movement/mud/Foot_Mud_Run%02d.ogg'
HOE = 'Audio/sfx/inventory_gui/wav/build/UI_Hoe_0%d.ogg'
LAND = 'Characters/Player/audio/wav/footsteps/default/Player_Footstep_Default_Land_M_0%d.ogg'

# One layer of a cue: `files` (a % pattern and its numbers: variant n takes the n-th, round and round), `peak`
# (seconds after the cue the clip's loudest moment lands: its start is cut to fit), `delay` (seconds after the cue it
# starts; with `peak`, nothing before it is kept), `gain`, `low` (high-pass Hz), `high` (low-pass Hz, 0 for none),
# `pitch` (one per variant, round and round), `longest` (seconds kept, from where the layer starts), `start` (seconds
# into the clip it starts, one per variant), `glide` (a pitch curve per variant, ((seconds, pitch), ...), set frame by
# frame; it replaces `pitch`), `wobble` ((Hz, depth): the pitch swung by that fraction), `env` (a volume curve,
# ((seconds, gain), ...)), `fade_in`, `fade_out` (seconds).
Layer = collections.namedtuple(
    'Layer', 'files numbers peak delay gain low high pitch longest start glide wobble env fade_in fade_out',
    defaults=(None, 0.0, 1.0, 100.0, 0.0, (1.0,), 2.0, (0.0,), None, None, None, 0.0, 0.03))

# Loops: one seamless file each, the clip played whole at the pitch that makes it `seconds` long (AudioSource.loop).
LOOPS = {
    # Her wings while she hovers: the Deathsquito's own wing loop 6.9 semitones down (fundamental 203 -> 136 Hz),
    # exactly 4.0 s (120 frames at 30 fps), its hiss under 110 Hz off.
    'buzz_loop': dict(file=WINGS, seconds=4.0, low=110.0, high=0.0),
}

RECIPES = {
    # The tell before the dive: her wing buzz pushed up from the hover pitch towards the Deathsquito's own (a strain),
    # swelling as it rises in 0.45 s, then held with a 7 Hz tremble; three starts into the loop, three heights.
    'buzz_strain': [Layer(WINGS, (0,), start=(0.3, 1.1, 1.9), low=120, longest=1.0, fade_in=0.05, fade_out=0.15,
                          glide=(((0, HOVER), (0.45, 0.97), (1.0, 1.0)), ((0, HOVER), (0.5, 1.03), (1.0, 1.06)),
                                 ((0, HOVER), (0.4, 0.92), (1.0, 0.95))),
                          wobble=(7.0, 0.015), env=((0, 0.35), (0.45, 1.0), (1.0, 1.0)))],
    # The straight dive: the player's spear throw (a weapon rushing through the air), four semitones lower for her
    # weight, its peak mid-dive.
    'dive_whoosh': [Layer(SPEAR_THROW, (1, 2, 3), peak=0.2, low=150, pitch=(0.8, 0.76, 0.84), longest=0.48)],
    # The proboscis driving into a player: the spear's own hit, lowered a little, and the Deathsquito's flesh splat.
    'pierce': [Layer(SPEAR_HIT, (1, 2, 5), peak=0.0, low=140, pitch=(0.9, 0.88, 0.92), longest=0.28),
               Layer(GIB, (2, 17, 2), peak=0.015, gain=0.6, low=160, pitch=(0.9, 0.85, 0.95), longest=0.35)],
    # The lunge: the Deathsquito's own sting attack (its hissing rush and the thump of the sting), 4 semitones down.
    'lunge': [Layer(STING, (1, 2, 1), low=100, pitch=(0.8, 0.78, 0.84), longest=0.9, fade_out=0.08)],
    # An egg squeezed out and flung: a wet squelch (a boot in mud) and the Deathsquito's splat popping after it.
    'egg_launch': [Layer(MUD, (3, 5, 1), peak=0.02, gain=0.7, low=200, pitch=(0.8,), longest=0.22),
                   Layer(GIB, (17, 2, 17), peak=0.1, low=160, pitch=(0.85, 0.9, 0.8), longest=0.3, fade_out=0.12)],
    # An egg landing in dirt and sinking in: a body landing on earth, the hoe biting into the ground, a wet squelch.
    'egg_land': [Layer(LAND, (1, 2, 3), peak=0.0, low=180, longest=0.35),
                 Layer(HOE, (1, 4, 5), peak=0.01, gain=0.6, low=180, longest=0.3),
                 Layer(MUD, (8, 3, 5), delay=0.02, gain=0.45, low=200, pitch=(0.85,), longest=0.3)],
    # The egg throbbing: a punch landing on a body, muffled (a thump with the sharpness taken off), with a faint wet
    # squelch; short enough to repeat three times a second.
    'egg_pulse': [Layer('Characters/Player/audio/wav/movement/melee/Player_Movement_Unarmed_Hit_M_0%d.ogg', (1, 2, 4),
                        peak=0.02, low=130, high=700, longest=0.26, fade_in=0.012, fade_out=0.08),
                  Layer(MUD, (3, 5, 1), peak=0.03, gain=0.4, low=250, high=2500, pitch=(0.8,), longest=0.2)],
    # The shell bursting: the game's own egg bursting (the Seeker egg plays the Tick's death at 0.8), the crack of
    # the beehive breaking (a papery shell) on its onset, and rock crumbling after it as the shell falls apart.
    'egg_burst': [Layer('Characters/Tick/SFX/Enemy_Tick_Death_0%d.ogg', (1, 2, 5), peak=0.02, low=120, pitch=(0.8,),
                        longest=0.55, fade_out=0.3),
                  Layer('world/Props/BeeHive/fx/wav/Smash_TorchBreak1.ogg', (0,), peak=0.01, gain=0.6, low=200,
                        pitch=(0.9, 0.85, 0.95), longest=0.3, fade_out=0.15),
                  Layer('Audio/Ambients/Winding Tunnel/OneShots/Amb_WindingTunnel_OneShots_RockCrumble_%02d.ogg',
                        (6, 7, 5), delay=0.15, gain=0.45, low=150, longest=0.65, fade_in=0.05, fade_out=0.3,
                        env=((0, 1.0), (0.65, 0.4)))],
    # A hatchling's first buzz: the game's own Deathsquito wing loop spinning up to its own pitch as it rises.
    'hatch_buzz': [Layer(WINGS, (0,), start=(0.0, 0.9, 1.8), low=110, longest=1.4, fade_out=0.35,
                         glide=(((0, 0.8), (0.35, 1.0), (1.4, 1.0)),), env=((0, 0.0), (0.3, 1.0), (1.4, 1.0)))],
    # A needle spat out: a quick bright swish (the claw swipe eight creatures play when a bite lands) on the onset
    # of the Neck's spit.
    'needle_shot': [Layer('Characters/GreyDwarf/fx/audio/wav/Zombie04_Claw_Swipe%d.ogg', (2, 3, 2, 3), peak=0.03,
                          low=300, pitch=(1.1, 1.0, 0.95, 1.15), longest=0.2, fade_out=0.05),
                    Layer('Characters/Neck/fx/wav/GiantMantis_Spit.ogg', (0,), peak=0.01, gain=0.5, low=250,
                          pitch=(1.0, 1.1, 0.9, 1.05), longest=0.12, fade_out=0.05)],
    # A needle sticking in the ground: the game's arrow hit (what an arrow plays when it sticks), a little higher for
    # a thinner shaft, and a pinch of the hoe's dirt.
    'needle_ground': [Layer(ARROW_HIT, (1, 2, 3, 1), peak=0.005, low=150, pitch=(1.15, 1.1, 1.2, 1.05), longest=0.25),
                      Layer(HOE, (2, 3, 5, 1), peak=0.01, gain=0.35, low=200, pitch=(1.1,), longest=0.15)],
    # A needle in a player: the arrow hit and the Deathsquito's flesh splat.
    'needle_flesh': [Layer(ARROW_HIT, (2, 3, 1), peak=0.005, low=150, pitch=(1.1, 1.0, 1.15), longest=0.25),
                     Layer(GIB, (2, 17, 2), peak=0.02, gain=0.55, low=200, pitch=(1.1, 1.0, 1.2), longest=0.3)],
    # The whirl: her wings working harder, the pitch swinging once and a half as she goes round (1.5 Hz), and the
    # spear throw's rush twice as the needle sweeps past.
    'zip_whir': [Layer(WINGS, (0,), start=(0.2, 1.0, 1.7), low=120, longest=1.1,
                       glide=(((0, 0.8), (0.3, 0.9), (1.1, 0.9)),), wobble=(1.5, 0.05),
                       env=((0, 0.4), (0.2, 1.0), (0.9, 1.0), (1.1, 0.0))),
                 Layer(SPEAR_THROW, (1, 3, 2), peak=0.35, gain=0.45, low=200, pitch=(1.1,), longest=0.35),
                 Layer(SPEAR_THROW, (3, 2, 1), peak=0.75, gain=0.45, low=200, pitch=(1.05,), longest=0.35)],
    # Her cry when she engages: the giant beetle's buzzing growl (the swamp's) lowered for her size, with the
    # Seeker's screech riding over it, quieter.
    'alert': [Layer('Characters/Neck/fx/wav/GiantBeetle_Growl%d.ogg', (1, 2), low=200, pitch=(0.8, 0.78), longest=1.2,
                    fade_out=0.15),
              Layer('Characters/Seeker/fx/wav/Enemy_Seeker_Alerted_0%d.ogg', (3, 4), delay=0.05, gain=0.5, low=300,
                    pitch=(0.75, 0.72), longest=1.0, fade_out=0.25)],
}


def samples(seconds):
    return int(round(seconds * SR))


def gains(freqs, low, high, order=2):
    """The magnitude of the mod's 2nd-order AudioHighPassFilter at `low` Hz and AudioLowPassFilter at `high`."""
    gain = 1.0 / np.sqrt(1.0 + (low / np.maximum(freqs, 1e-3)) ** (2 * order))
    if high:
        gain /= np.sqrt(1.0 + (freqs / high) ** (2 * order))
    return gain


def band(x, low, high=0.0):
    """High- and low-pass by FFT (zero-phase, zero-padded)."""
    size = 1 << (2 * len(x) - 1).bit_length()
    f = np.fft.rfftfreq(size, 1.0 / SR)
    return np.fft.irfft(np.fft.rfft(x, size) * gains(f, low, high), size)[:len(x)]


def band_loop(x, low, high=0.0):
    """The same filter applied round the loop (circular), so the loop stays seamless."""
    f = np.fft.rfftfreq(len(x), 1.0 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gains(f, low, high), len(x))


def game(rel):
    """A sound from the local reference export, mono at SR, peak 1 (Blender's aud, else soundfile)."""
    path = os.path.join(REFERENCE, rel)
    try:
        import aud
        sound = aud.Sound(path)
        rate, data = sound.specs[0], np.asarray(sound.data(), dtype=np.float64)
    except ImportError:
        import soundfile
        data, rate = soundfile.read(path, dtype='float64', always_2d=True)
    x = data.mean(axis=1) if data.ndim > 1 else data
    if rate != SR:
        x = np.interp(np.arange(int(len(x) * SR / rate)) * rate / SR, np.arange(len(x)), x)
    return x / (np.abs(x).max() + 1e-9)


def pitch_curve(layer, n, count):
    """The pitch at each output sample: the variant's pitch, or its glide curve, swung by the wobble."""
    t = np.arange(count) / SR
    if layer.glide:
        times, values = zip(*layer.glide[n % len(layer.glide)])
        curve = np.interp(t, times, values)
    else:
        curve = np.full(count, layer.pitch[n % len(layer.pitch)])
    if layer.wobble:
        rate, depth = layer.wobble
        curve = curve * (1.0 + depth * np.sin(2 * np.pi * rate * t))
    return curve


def play(x, start, curve):
    """The clip read from `start` (samples) at the pitch curve, as AudioSource.pitch plays it, to the clip's end."""
    at = start + np.concatenate(([0.0], np.cumsum(curve[:-1])))
    at = at[at < len(x) - 1]
    return np.interp(at, np.arange(len(x)), x)


def shape(x, layer):
    """The layer's volume: its curve, a fade in, a fade out at its end (AudioSource.volume ramped)."""
    if layer.env:
        times, values = zip(*layer.env)
        x = x * np.interp(np.arange(len(x)) / SR, times, values)
    fade = min(len(x), samples(layer.fade_in))
    if fade:
        x[:fade] *= np.linspace(0.0, 1.0, fade)
    fade = min(len(x), samples(layer.fade_out))
    if fade:
        x[-fade:] *= np.linspace(1.0, 0.0, fade)
    return x


def clip(layer, n):
    """Variant `n` of a layer and when it starts after the cue: the clip pitched, filtered, its start cut so its
    peak lands on `peak` (and nothing of it before `delay` when both are set), kept to `longest`, shaped."""
    rel = layer.files % layer.numbers[n % len(layer.numbers)] if '%' in layer.files else layer.files
    source = np.tile(game(rel), 3) if rel in LOOPED else game(rel)
    start = samples(layer.start[n % len(layer.start)])
    reach = int((len(source) - start) / 0.5) + 1      # enough output for the whole clip at any pitch over 0.5
    x = band(play(source, start, pitch_curve(layer, n, reach)), layer.low, layer.high)
    at = layer.delay
    if layer.peak is not None:
        top, lead = int(np.argmax(np.abs(x))), layer.peak - layer.delay
        cut = max(0, top - samples(lead))
        at = layer.delay + (max(0.0, lead - top / SR) if cut == 0 else 0.0)
        x = x[cut:]
        if cut:
            x[:samples(0.01)] *= np.linspace(0.0, 1.0, samples(0.01))[:len(x)]
    return shape(x[:samples(layer.longest)].copy(), layer), at


def mix(layers, n):
    parts = [(clip(layer, n), layer.gain) for layer in layers]
    out = np.zeros(max(samples(at) + len(x) for (x, at), _ in parts))
    for (x, at), gain in parts:
        out[samples(at):samples(at) + len(x)] += gain * x
    return out


def finish(x):
    """The silent tail cut, the end faded, brought to a common loudness."""
    quiet = np.nonzero(np.abs(x) > np.abs(x).max() * 0.003)[0]
    x = x[:quiet[-1] + samples(0.02)] if len(quiet) else x
    x[-samples(0.03):] *= np.linspace(1.0, 0.0, samples(0.03))
    return x * loudness(x)


def loudness(x):
    """The gain that brings the loudest 0.1 s to LOUDNESS, unless the peak would pass PEAK."""
    window = min(len(x), samples(0.1))
    rms = np.sqrt(np.convolve(x ** 2, np.ones(window) / window, 'valid').max())
    return min(LOUDNESS / (rms + 1e-9), PEAK / (np.abs(x).max() + 1e-9))


def loop(spec):
    """A loop: the whole clip read round and round at the pitch that makes it `seconds` long, filtered round the
    loop, levelled. Returns the samples and the pitch."""
    x = game(spec['file'])
    count = samples(spec['seconds'])
    pitch = len(x) / count
    wrapped = np.interp(np.arange(count) * pitch, np.arange(len(x) + 1), np.append(x, x[0]))
    wrapped = band_loop(wrapped, spec['low'], spec['high'])
    return wrapped * loudness(wrapped), pitch


def write(path, x):
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((np.clip(x, -1.0, 1.0) * 32767).astype('<i2').tobytes())


def report(name, x, extra=''):
    spec = np.abs(np.fft.rfft(x)) ** 2
    f = np.fft.rfftfreq(len(x), 1.0 / SR)
    total = spec.sum() + 1e-12
    shares = [spec[(f >= a) & (f < b)].sum() / total * 100 for a, b in zip(BANDS, BANDS[1:])]
    peak = 20 * np.log10(np.abs(x).max() + 1e-12)
    print('WORKSHOP sfx %-16s %.2f s, peak %5.1f dBFS, below 80 Hz %4.1f %%, centroid %5.0f Hz | bands %s%s' % (
        name, len(x) / SR, peak, shares[0], (spec * f).sum() / total, ' '.join('%4.1f' % s for s in shares), extra))


def seam(x):
    """The step where the loop wraps against its typical sample step, dB (near 0 or below is seamless)."""
    typical = max(float(np.median(np.abs(np.diff(x)))), 1e-9)
    return 20 * np.log10(max(abs(x[-1] - x[0]), 1e-9) / typical)


def variants(layers):
    return max(max(len(layer.numbers), len(layer.pitch), len(layer.start), len(layer.glide or ())) for layer in layers)


def prune(base, keep):
    """Removes the older build folders but the last `keep` (an open preview may hold its files: skipped then)."""
    builds = sorted(d for d in os.listdir(base) if d.startswith('build_') and os.path.join(base, d) != keep)
    for old in builds[:-KEEP]:
        shutil.rmtree(os.path.join(base, old), ignore_errors=True)


def main(base):
    """Writes a new folder under `base` each time and keeps the last KEEP older ones."""
    out = os.path.join(base, time.strftime('build_%Y%m%d_%H%M%S'))
    os.makedirs(out)
    prune(base, out)
    print('WORKSHOP sfx bands (%): <80, 80-160, 160-300, 300-600, 600-1.5k, 1.5-4k, >4k Hz')
    for cue, spec in LOOPS.items():
        x, pitch = loop(spec)
        write(os.path.join(out, '%s_1.wav' % cue), x)
        report(cue + '_1', x, ' | loop at pitch %.4f, seam %+.1f dB' % (pitch, seam(x)))
    for cue, layers in RECIPES.items():
        for n in range(variants(layers)):
            x = finish(mix(layers, n))
            write(os.path.join(out, '%s_%d.wav' % (cue, n + 1)), x)
            report('%s_%d' % (cue, n + 1), x)
    print('WORKSHOP sfx: %d files in %s' % (len(os.listdir(out)), out))


if __name__ == '__main__':
    main(sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1
         else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out', 'sfx'))

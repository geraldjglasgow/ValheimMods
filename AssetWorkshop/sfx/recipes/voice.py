"""Creature voices, built the way a throat makes them: a buzzing source (glottal pulses at a pitch that moves, with
jitter, a growl an octave under, rasp and breath pulsed with the buzz) through a vocal tract (a chest resonance and
formants that scale with the creature's size and open as it calls), then strained (saturation) and trimmed.

Roles follow the game's measured shapes (codex/sfx/creatures.md): idles chatter or murmur in several short syllables,
each with its own rise and fall of pitch (the game's harmonics curve; flat ones sound synthetic); alerts hold a loud
call; attacks bark hard with a breathy follow-through; hurt yelps jump up and fall; deaths groan down and trail off.

    voice("idle", size=0.6, seed=3)      # a small creature's murmur (size 1 is a Greydwarf-sized body)
"""
import numpy as np

from kit import core, env, filters, noise, osc, shape

ROLES = {   # length s, attack s, pitch path (share of the call, semitones), arch per syllable (st), effort, breath,
    # syllables (min, max)
    "idle": dict(length=(1.2, 2.0), attack=0.08, path=[(0, 0), (1, -2)], arch=4.0, effort=0.4, breath=0.5,
                 legato=1.6, syll=(3, 5)),
    "alert": dict(length=(1.5, 2.5), attack=0.09, path=[(0, -2), (0.25, 4), (0.8, 3), (1, -3)], arch=2.0,
                  effort=0.85, breath=0.4, syll=(1, 2)),
    "attack": dict(length=(1.2, 1.7), attack=0.03, path=[(0, 3), (0.2, 5), (1, -6)], arch=1.5, effort=1.0,
                   breath=0.7, syll=(1, 2)),
    "hurt": dict(length=(0.75, 1.15), attack=0.015, path=[(0, 7), (0.12, 9), (1, -4)], arch=1.0, effort=0.9,
                 breath=0.45, syll=(1, 1)),
    "death": dict(length=(1.6, 2.6), attack=0.06, path=[(0, 3), (0.2, 4), (0.7, -5), (1, -12)], arch=2.0,
                  effort=0.7, breath=0.75, syll=(2, 3)),
}
FORMANTS = ((280, 160, 2.0), (650, 110, 0.0), (1100, 140, -4.0), (2450, 220, -10.0), (3300, 300, -15.0))
LIFTS = (0.1, 0.35, 0.2, 0.05, 0.0)


def boundaries(role, seconds, seed):
    """Start times of the call's syllables (the first at 0)."""
    rng = np.random.default_rng(seed)
    count = int(rng.integers(ROLES[role]["syll"][0], ROLES[role]["syll"][1] + 1))
    if count <= 1:
        return np.array([0.0])
    gaps = rng.uniform(0.7, 1.3, count)
    return np.concatenate([[0.0], np.cumsum(gaps / gaps.sum() * seconds)[:-1]])


def envelope(role, seconds, starts, seed):
    """Loudness: one bump per syllable, each rising over the role's attack and falling away before the next."""
    rng, attack = np.random.default_rng(seed), ROLES[role]["attack"]
    total = np.zeros(core.count(seconds))
    for index, start in enumerate(starts):
        length = (starts[index + 1] if index + 1 < len(starts) else seconds) - start
        stretch = ROLES[role].get("legato", 0.95)
        bump = env.ar(length * stretch, min(attack, length * 0.3), length * 0.8 * stretch, power=1.3)
        bump *= rng.uniform(0.6, 1.0)
        total = core.mix(total, core.place(bump, start))[:len(total)]
    return total


def pitch_curve(role, seconds, starts, base_hz, seed):
    """Pitch in Hz per sample: the role's path over the call, an arch over every syllable, jitter and vibrato."""
    spec, rng = ROLES[role], np.random.default_rng(seed)
    t = np.arange(core.count(seconds)) / core.RATE
    path = np.interp(t / seconds, *zip(*spec["path"]))
    ends = np.concatenate([starts[1:], [seconds]])
    index = np.searchsorted(starts, t, side="right") - 1
    local = (t - starts[index]) / (ends[index] - starts[index])
    arches = rng.uniform(0.4, 1.0, len(starts)) * spec["arch"] * rng.choice([1, 1, -0.5], len(starts))
    semitones = path + arches[index] * np.sin(np.pi * np.clip(local, 0, 1))
    wobble = env.wander(seconds, 0.4, 11.0, seed) + np.log2(env.vibrato(seconds, 6.0, 0.25)) * 12
    return base_hz * 2 ** ((semitones + wobble) / 12)


def source(f0, seconds, effort, breath, seed):
    """The buzz: glottal pulses, a subharmonic growl under effort, rasp, and breath pulsed with the buzz."""
    buzz = osc.glottal(f0, seconds, open_share=0.65 - 0.25 * effort)
    growl = osc.glottal(f0 / 2, seconds, open_share=0.5) * (0.2 + 0.5 * effort)
    voiced = shape.roughness(buzz + growl, rate_hz=35 + 25 * effort, depth=0.25 + 0.5 * effort, seed=seed)
    pulse = 0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(f0) / core.RATE)
    air = filters.apply(noise.pink(seconds, seed + 1), filters.highpass(300)) * (0.4 + 0.6 * pulse)
    return voiced + air * breath * 4.0


def tract(x, size, opening, seed):
    """A chest resonance and formants scaled for a body size; F1 and F2 rise as the envelope opens the mouth."""
    rng, scale = np.random.default_rng(seed), size ** -0.85
    times = np.arange(len(opening)) / core.RATE

    def moving(centre, lift):
        return lambda t: centre * scale * (1 + lift * np.interp(t, times, opening))
    bank = [(moving(f * rng.uniform(0.93, 1.07), lift), w * scale, g) for (f, w, g), lift in zip(FORMANTS, LIFTS)]
    return filters.spectral(x, filters.formants(bank, floor=0.04))


def voice(role, size=1.0, seed=0, base_hz=None, grit=1.0):
    """One call: role in ROLES, size 1 = Greydwarf-sized (0.5 small, 2.5 troll), grit scales strain and rasp."""
    spec, rng = ROLES[role], np.random.default_rng(seed)
    seconds = rng.uniform(*spec["length"]) * size ** 0.25
    starts = boundaries(role, seconds, seed)
    base = (base_hz or 150.0 * size ** -0.9) * rng.uniform(0.94, 1.06)
    loudness = envelope(role, seconds, starts, seed)
    raw = source(pitch_curve(role, seconds, starts, base, seed), seconds, spec["effort"] * grit, spec["breath"], seed)
    shaped = tract(raw * loudness, size, loudness / max(loudness.max(), 1e-9), seed)
    strained = shape.drive(shaped, 1.0 + 3.0 * spec["effort"] * grit, bias=0.15)
    cleaned = filters.apply(strained, filters.highpass(60 / size ** 0.5), filters.lowpass(9500))
    return core.fade(core.trim(cleaned), 0.004, 0.05)

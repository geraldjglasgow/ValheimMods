"""Struck and broken things, by material, in layers: the contact click, the body's modes ringing (modal synthesis), the
grit of the strike (dense tiny noise bursts in the low mids), a crunch of the material's grain in its own band, the
thud of the weight and, for big or broken things, a sub rumble under it all.

Measured on the game's impacts (codex/sfx/materials.md), the layers' balance is what makes the material: metal rings
in clean partials for about a second (flatness 0.07 to 0.15, centroid 1.6 to 2.9 kHz); stone, wood and ice are noisy
(flatness about 0.5) and dark (ice and rock hits: 75 to 85 % of the energy under 300 Hz); a crystal or ice break is a
dense crunch over a steady sub band (crystal: about 45 % under 300 Hz, 45 % over 3 kHz, only 10 % between), decaying
over 2 to 3 s, its peak 12 to 15 dB over its RMS (the game's sounds are compressed and their peaks rounded).
"""
import numpy as np

from kit import core, env, filters, grain, level, noise, osc, shape

MATERIALS = {  # modes (ratios, Hz, decay s, fall per mode, detune st), grit and crunch bands Hz, layer levels, rumble
    "crystal": dict(ratios=(1.0, 2.32, 4.25, 6.63, 9.38), hz=1600, decay=0.7, fall=0.7, detune=0.08,
                    grit=(250, 2500), band=(2200, 12000), ring=0.08, crunch=1.0, thud=1.0, rumble=(55, 1.6)),
    "ice": dict(ratios=(1.0, 2.1, 3.7, 5.5), hz=900, decay=0.25, fall=0.7, detune=0.05,
                grit=(250, 2000), band=(1500, 7000), ring=0.08, crunch=0.8, thud=1.4, rumble=(60, 0.9)),
    "metal": dict(ratios=(1.0, 1.59, 2.14, 2.83, 3.61, 4.23, 5.4), hz=520, decay=1.0, fall=0.8, detune=0.05,
                  grit=(400, 3000), band=(2000, 8000), ring=1.0, crunch=0.12, thud=0.35, rumble=(70, 0.0)),
    "stone": dict(ratios=(1.0, 1.7, 2.6, 3.4, 4.9), hz=240, decay=0.12, fall=0.7, detune=0.0,
                  grit=(200, 2000), band=(400, 3500), ring=0.1, crunch=0.9, thud=1.3, rumble=(50, 0.6)),
    "wood": dict(ratios=(1.0, 2.3, 3.9, 5.1), hz=320, decay=0.14, fall=0.65, detune=0.0,
                 grit=(250, 2500), band=(500, 4000), ring=0.35, crunch=0.6, thud=1.0, rumble=(60, 0.2)),
}


def click(seed, bright_hz=2500.0):
    """The contact: 4 ms of noise over bright_hz."""
    return filters.apply(noise.burst(0.004, seed, decay_s=0.0008), filters.highpass(bright_hz))


def modes(material, seconds, seed, size=1.0):
    """The body ringing: the material's modes (their pitch falling with size), each decaying faster than the last."""
    spec, rng = MATERIALS[material], np.random.default_rng(seed)
    base = spec["hz"] / size * rng.uniform(0.9, 1.1)
    freqs = [base * r * rng.uniform(0.98, 1.02) for r in spec["ratios"]]
    decays = [spec["decay"] * size ** 0.5 * spec["fall"] ** i * rng.uniform(0.8, 1.2) for i in range(len(freqs))]
    amps = [0.8 ** i * rng.uniform(0.6, 1.0) for i in range(len(freqs))]
    return osc.partials(freqs, amps, decays, seconds, seed=seed, detune=spec["detune"])


def crunch(band, seconds, seed, start_hz=900.0, half_life_s=0.25, floor_hz=0.0, fade_s=None):
    """Grain: tiny noise bursts in a band (low, high Hz), dense at first and thinning, each quieter as it comes
    later."""
    rng = np.random.default_rng(seed)
    times = noise.thinning(seconds, start_hz, half_life_s, seed, floor_hz)
    fade_s = fade_s or max(half_life_s * 3, 0.05)

    def piece(index):
        return noise.burst(rng.uniform(0.0004, 0.004), seed + index) * np.exp(-times[index] / fade_s) \
            * rng.uniform(0.2, 1.0) ** 2
    return filters.apply(grain.scatter(seconds, times, piece), filters.highpass(band[0]), filters.lowpass(band[1]))


def thud(seconds, size, seed):
    """The weight: a low sine falling from 110 to 55 Hz (lower for bigger things), 60 dB down after 0.3 s."""
    rng = np.random.default_rng(seed)
    hz = env.glide(110 * size ** -0.5 * rng.uniform(0.9, 1.1), 55 * size ** -0.5, seconds, curve=0.3)
    return osc.sine(hz, seconds) * env.decay(seconds, 0.3 * size ** 0.5)


def rumble(material, seconds, seed, t60):
    """A sub band under the whole event (the game's breaks keep one to the end): brown noise under the material's
    rumble frequency, decaying over t60 seconds."""
    hz, amount = MATERIALS[material]["rumble"]
    band = filters.apply(noise.brown(seconds, seed + 11), filters.lowpass(hz, 0.9), filters.highpass(25))
    return band * env.ar(seconds, 0.06, t60) * amount * 3.0


def finish(x, threshold_db, fade_out_s, plr_db=11.0):
    """Compressed (the transient let through for 20 ms), peaks limited to plr_db over the loudest 400 ms and rounded
    by a gentle tanh (the game's crest is 12 to 15 dB), faded."""
    squeezed = level.peak_to_loudness(level.compress(x, threshold_db, 3.0, attack_s=0.02), plr_db)
    return core.fade(shape.drive(squeezed, 1.4), 0.0005, fade_out_s)


def hit(material, seed=0, size=1.0, seconds=None):
    """One strike on a whole object."""
    spec = MATERIALS[material]
    seconds = seconds or min(2.5, 0.45 + max(spec["decay"], 0.35) * 1.2 * size ** 0.5)
    layers = [click(seed) * 0.6, modes(material, seconds, seed, size) * spec["ring"] * 0.6,
              crunch(spec["grit"], seconds, seed + 2, 700, 0.08) * 3.0,
              crunch(spec["band"], seconds, seed + 3, 500, 0.06) * spec["crunch"] * 1.5,
              thud(seconds, size, seed) * spec["thud"] * 0.75, rumble(material, seconds, seed, seconds * 0.7) * 0.12]
    return finish(core.mix(*layers), -14, 0.08, 16.0)


def shatter(material, seed=0, size=1.0, seconds=2.6):
    """A break: the strike, a shower of the material's crunch thinning over a second or two, pieces ringing as they
    land, and the sub rumble to the end."""
    spec, rng = MATERIALS[material], np.random.default_rng(seed)
    strike = core.pad(hit(material, seed, size, seconds=0.8), core.count(seconds)) * 1.8
    shower = crunch(spec["band"], seconds, seed + 5, 1400, 0.3 * size, 60.0, 0.6 * size) * spec["crunch"] * 2.1
    shower += crunch(spec["grit"], seconds, seed + 6, 600, 0.2 * size, 20.0) * 1.4
    landing = grain.scatter(seconds, np.sort(rng.gamma(2.0, 0.22, 12)),
                            lambda i: modes(material, 0.4, seed + 60 + i, rng.uniform(0.25, 0.5)) * 0.08)
    low = rumble(material, seconds, seed, seconds * 0.6) * 0.75
    return finish(core.mix(strike, shower, landing * spec["ring"] * 8, low), -16, 0.25, 10.0)

"""A fire loop the way the game's sounds: steady broadband noise heaviest under 500 Hz (the game's sfx_fire_loop:
10 s, centroid 427 Hz, flatness 0.57, integrated about -33 LUFS), a slow breathing of the flames, a thin hiss, and
fine crackles all through it with an occasional pop. Rendered a second longer than asked and crossfaded end into
start, so it loops without a seam.
"""
import numpy as np

from kit import core, env, filters, noise, osc


def roar(seconds, seed):
    """The low body of the flames: brown noise under 200 Hz, swelling slowly."""
    body = filters.apply(noise.brown(seconds, seed), filters.lowpass(200), filters.highpass(35))
    return body * (1 + env.wander(seconds, 0.22, 0.6, seed))


def flicker(seconds, seed):
    """The flames licking: pink noise around 500 Hz with a quick, uneven flutter."""
    band = filters.apply(noise.pink(seconds, seed + 1), filters.bandpass(520, 0.6))
    return band * np.clip(1 + env.wander(seconds, 0.35, 7.0, seed + 2), 0.2, None) * 0.55


def hiss(seconds, seed):
    return filters.apply(noise.white(seconds, seed + 3), filters.highpass(3500)) * 0.16


def crackles(seconds, seed, rate_hz=16.0):
    """Fine crackles over 1.5 kHz and a pop every second or so: a crack and a small low knock."""
    fine = filters.apply(noise.crackle(seconds, rate_hz, seed + 4), filters.highpass(1500)) * 0.6
    rng = np.random.default_rng(seed)
    pops = np.zeros(core.count(seconds))
    for index, t in enumerate(noise.events(seconds, 1.1, seed + 5)):
        crack = filters.apply(noise.burst(0.012, seed + 50 + index, decay_s=0.002),
                              filters.bandpass(rng.uniform(1200, 3200), 1.5))
        knock = osc.sine(rng.uniform(70, 120), 0.05) * env.decay(0.05, 0.03) * 0.3
        piece = core.mix(crack * rng.uniform(0.4, 1.2), knock)
        start = int(t * core.RATE)
        pops[start:start + len(piece)] += piece[:len(pops) - start]
    return fine + pops


def loop_seam(x, overlap_s=1.0):
    """x made loopable: its last overlap_s crossfaded (equal power) into its start, and that tail removed."""
    n = core.count(overlap_s)
    body, tail = x[:-n].copy(), x[-n:]
    ramp = np.linspace(0, np.pi / 2, n)
    body[:n] = body[:n] * np.sin(ramp) + tail * np.cos(ramp)
    return body


def fire_loop(seconds=10.0, seed=0, intensity=1.0):
    """A burning fire, seconds long, looping seamlessly; intensity raises the crackle rate and the flutter."""
    total = seconds + 1.0
    layers = core.mix(roar(total, seed), flicker(total, seed) * intensity, hiss(total, seed),
                      crackles(total, seed, 12.0 + 8.0 * intensity))
    return loop_seam(layers, 1.0)

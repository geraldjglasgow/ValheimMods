"""Envelopes and control curves: breakpoints, attack-release shapes, decays, glides, vibrato and slow random wander.
Each returns one value per sample."""
import numpy as np

from kit.core import RATE, count


def breakpoints(points, seconds, rate=RATE, exponential=False):
    """A curve through [(time s, value), ...], held flat before the first and after the last point; exponential
    interpolates in the log domain (for frequencies and gains that should move evenly to the ear)."""
    t = np.arange(count(seconds, rate)) / rate
    times, values = zip(*points)
    if exponential:
        return np.exp(np.interp(t, times, np.log(np.maximum(values, 1e-9))))
    return np.interp(t, times, values)


def ar(seconds, attack_s, release_s=None, rate=RATE, power=1.0):
    """Rise over attack_s (a quarter sine), then an exponential fall that reaches -60 dB at the end (or after
    release_s); power > 1 makes the start of the fall steeper."""
    t = np.arange(count(seconds, rate)) / rate
    release = release_s or max(seconds - attack_s, 1e-3)
    rise = np.sin(0.5 * np.pi * np.clip(t / max(attack_s, 1e-4), 0, 1))
    fall = np.exp(-6.91 * np.clip(t - attack_s, 0, None) / release) ** power
    return rise * fall


def adsr(seconds, attack_s, decay_s, sustain, release_s, rate=RATE):
    """Attack, decay to sustain, hold, release at the end: the classic shape, linear segments."""
    hold = max(seconds - attack_s - decay_s - release_s, 0.0)
    marks = [(0.0, 0.0), (attack_s, 1.0), (attack_s + decay_s, sustain), (attack_s + decay_s + hold, sustain),
             (seconds, 0.0)]
    return breakpoints(marks, seconds, rate)


def swell(seconds, peak_at=0.5, sharpness=2.0, rate=RATE):
    """A bell that peaks at peak_at (share of the length): the loudness of a pass-by or a whoosh."""
    t = np.linspace(0, 1, count(seconds, rate))
    rise = np.clip(t / max(peak_at, 1e-3), 0, 1)
    fall = np.clip((1 - t) / max(1 - peak_at, 1e-3), 0, 1)
    return np.where(t < peak_at, rise, fall) ** sharpness * np.sin(np.pi * np.clip(t, 0, 1)) ** 0.25


def glide(start, end, seconds, rate=RATE, curve=1.0):
    """From start to end over seconds, even in pitch (exponential); curve > 1 lingers at the start, < 1 moves early."""
    t = np.linspace(0, 1, count(seconds, rate)) ** curve
    return start * (end / start) ** t


def vibrato(seconds, rate_hz, depth_st, rate=RATE):
    """A pitch factor that swings depth_st semitones either way rate_hz times a second."""
    t = np.arange(count(seconds, rate)) / rate
    return 2 ** (depth_st * np.sin(2 * np.pi * rate_hz * t) / 12)


def wander(seconds, depth, speed_hz, seed=0, rate=RATE):
    """A slow random curve around 0 (about +-depth), changing about speed_hz times a second: jitter in a voice's
    pitch (use as 2 ** (wander / 12)), the flicker of a flame's level."""
    rng = np.random.default_rng(seed)
    knots = max(4, int(seconds * speed_hz) + 3)
    values = rng.standard_normal(knots) * depth
    return np.interp(np.linspace(0, knots - 1, count(seconds, rate)), np.arange(knots), values)


def decay(seconds, t60, rate=RATE):
    """exp fall reaching -60 dB after t60 seconds."""
    return np.exp(-6.91 * np.arange(count(seconds, rate)) / rate / t60)

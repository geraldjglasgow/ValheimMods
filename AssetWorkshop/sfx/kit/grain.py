"""Layering many small sounds: scatter copies of a sound over time (debris, crackle, footsteps in gravel) and granular
clouds made of short windowed pieces of a source (shimmer, rumble, textures that never repeat)."""
import numpy as np

from kit import resample
from kit.core import RATE, count


def scatter(seconds, times, make, rate=RATE):
    """A track with make(index) placed at each time (seconds); make returns a mono array."""
    out = np.zeros(count(seconds, rate))
    for index, t in enumerate(times):
        piece = make(index)
        start = int(t * rate)
        if start < len(out):
            piece = piece[:len(out) - start]
            out[start:start + len(piece)] += piece
    return out


def cloud(source, seconds, density_hz=40.0, grain_s=0.06, pitch_st=0.0, seed=0, rate=RATE):
    """Grains grain_s long cut from random places of source, Hann windowed, pitched +-pitch_st semitones at random,
    density_hz of them a second on average."""
    rng = np.random.default_rng(seed)
    out = np.zeros(count(seconds, rate))
    size = count(grain_s, rate)
    for t in np.cumsum(rng.exponential(1 / density_hz, int(seconds * density_hz * 2) + 4)):
        if t >= seconds:
            break
        at = rng.integers(0, max(1, len(source) - size))
        piece = source[at:at + size] * np.hanning(min(size, len(source)))
        if pitch_st:
            piece = resample.pitch(piece, rng.uniform(-pitch_st, pitch_st))
        start = int(t * rate)
        piece = piece[:len(out) - start]
        out[start:start + len(piece)] += piece
    return out


def bounces(first_s, count_=6, spacing_s=0.12, shrink=0.7, seed=0):
    """(times, gains) of an object bouncing to rest: gaps and heights shrinking by shrink each time."""
    rng = np.random.default_rng(seed)
    times, gains, t, g, gap = [], [], first_s, 1.0, spacing_s
    for _ in range(count_):
        times.append(t)
        gains.append(g)
        gap *= shrink * rng.uniform(0.85, 1.1)
        t, g = t + gap, g * shrink * rng.uniform(0.8, 1.1)
    return times, gains

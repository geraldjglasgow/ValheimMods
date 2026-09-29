"""Resampling with a windowed sinc: change the sample rate, shift pitch the way Unity's AudioSource.pitch does (faster
and shorter), or play through a pitch curve."""
import numpy as np

from kit.core import RATE, curve

TAPS = 16


def read(x, positions, cutoff=1.0):
    """x read at fractional sample positions, band-limited to cutoff times Nyquist (under 1 when reading faster)."""
    x = np.asarray(x, dtype=float)
    base = np.floor(positions).astype(np.int64)
    frac = positions - base
    out = np.zeros(len(positions))
    padded = np.concatenate([np.zeros(TAPS), x, np.zeros(TAPS + 1)])
    for tap in range(-TAPS + 1, TAPS + 1):
        distance = frac - tap
        weight = cutoff * np.sinc(cutoff * distance) * (0.5 + 0.5 * np.cos(np.pi * np.clip(distance / TAPS, -1, 1)))
        index = np.clip(base + tap + TAPS, 0, len(padded) - 1)
        out += padded[index] * weight
    return out


def pitch(x, semitones):
    """x played 2 ** (semitones / 12) times as fast: higher and shorter, as a raised AudioSource.pitch does."""
    step = 2 ** (semitones / 12)
    positions = np.arange(0, len(x) - 1, step)
    return read(x, positions, min(1.0, 1 / step))


def varispeed(x, speed):
    """x played at a speed that changes per output sample (a curve of playback rates): a pitch glide on a sound."""
    steps = curve(np.asarray(speed, dtype=float), len(x))
    cumulative = np.concatenate([[0.0], np.cumsum(steps)])
    positions = cumulative[cumulative < len(x) - 1]
    return read(x, positions, min(1.0, 1 / max(steps.max(), 1e-3)))


def rate_change(x, source_rate, target_rate=RATE):
    """x resampled from source_rate to target_rate, same duration."""
    step = source_rate / target_rate
    return read(x, np.arange(0, len(x) - 1, step), min(1.0, 1 / step))

"""Loudness after ITU-R BS.1770-4, the measure every sound in the codex is compared by.

K-weighting (a high shelf of +4 dB above about 1.7 kHz, then a high pass at 38 Hz) is applied in the time domain (as
an FFT convolution with the two biquads' impulse response, which is exact), the weighted mean square is taken over
400 ms blocks overlapping by 75 %, and the integrated loudness is the mean over the blocks that pass two gates: an
absolute gate at -70 LUFS and a relative gate 10 LU under the mean of the blocks that passed the first. The
coefficients are computed for the file's own sample rate from the analogue prototype, so 32, 44.1 and 48 kHz clips are
measured alike. At 48 kHz they match the standard's published values.

Clips shorter than one block (400 ms) are padded with silence to one block, so a 100 ms click reads as the loudness of
that click spread over 400 ms: always compare a short sound with other short sounds, and prefer momentary_max for
one-shots, where it is the level a player hears at the loudest moment.
"""
import functools

import numpy as np

import sfx_audio

SHELF_HZ, SHELF_DB, SHELF_Q = 1681.974450955533, 3.999843853973347, 0.7071752369554196
HIGHPASS_HZ, HIGHPASS_Q = 38.13547087602444, 0.5003270373238773
ABSOLUTE_GATE = -70.0
RELATIVE_GATE = -10.0


@functools.lru_cache(maxsize=None)
def k_response(rate):
    """The K-weighting filter's impulse response at a sample rate."""
    return sfx_audio.impulse_response(k_weighting(rate))


def k_weighting(rate):
    """The two K-weighting biquads for a sample rate, as [b0, b1, b2, a0, a1, a2] rows."""
    k = np.tan(np.pi * SHELF_HZ / rate)
    vh = 10 ** (SHELF_DB / 20)
    vb = vh ** 0.4996667741545416
    a0 = 1 + k / SHELF_Q + k * k
    shelf = [(vh + vb * k / SHELF_Q + k * k) / a0, 2 * (k * k - vh) / a0, (vh - vb * k / SHELF_Q + k * k) / a0,
             1.0, 2 * (k * k - 1) / a0, (1 - k / SHELF_Q + k * k) / a0]
    k = np.tan(np.pi * HIGHPASS_HZ / rate)
    a0 = 1 + k / HIGHPASS_Q + k * k
    highpass = [1.0, -2.0, 1.0, 1.0, 2 * (k * k - 1) / a0, (1 - k / HIGHPASS_Q + k * k) / a0]
    return np.array([shelf, highpass])


def weighted_power(samples, rate, block_s, step_s):
    """K-weighted mean square of every block (block_s long, one every step_s), summed over the channels."""
    samples = samples if samples.ndim == 2 else samples[:, None]
    size, hop = int(round(block_s * rate)), int(round(step_s * rate))
    if len(samples) < size:
        samples = np.vstack([samples, np.zeros((size - len(samples), samples.shape[1]), samples.dtype)])
    weighted = sfx_audio.convolve(samples, k_response(rate))
    squares = np.concatenate([[0.0], np.cumsum((weighted ** 2).sum(axis=1))])
    starts = np.arange(0, len(weighted) - size + 1, hop)
    return (squares[starts + size] - squares[starts]) / size


def to_lufs(power):
    return -0.691 + 10 * np.log10(np.maximum(power, 1e-24))


def integrated(samples, rate):
    """Integrated loudness in LUFS (gated), or -70.0 when every block is under the absolute gate."""
    power = weighted_power(samples, rate, 0.4, 0.1)
    loud = power[to_lufs(power) > ABSOLUTE_GATE]
    if not len(loud):
        return ABSOLUTE_GATE
    gate = to_lufs(loud.mean()) + RELATIVE_GATE
    kept = loud[to_lufs(loud) > gate]
    return float(to_lufs(kept.mean()))


def momentary_max(samples, rate):
    """The loudest 400 ms block in LUFS: the level of a one-shot at its loudest moment."""
    return float(to_lufs(weighted_power(samples, rate, 0.4, 0.05).max()))


def short_term_max(samples, rate):
    """The loudest 3 s window in LUFS: for loops and long sounds."""
    return float(to_lufs(weighted_power(samples, rate, 3.0, 0.5).max()))


def loudness_range(samples, rate):
    """EBU R 128 loudness range in LU (3 s windows, gated, 10th to 95th percentile): how much a loop breathes."""
    power = weighted_power(samples, rate, 3.0, 1.0)
    loud = power[to_lufs(power) > ABSOLUTE_GATE]
    if len(loud) < 2:
        return 0.0
    kept = to_lufs(loud[to_lufs(loud) > to_lufs(loud.mean()) - 20.0])
    return float(np.percentile(kept, 95) - np.percentile(kept, 10))

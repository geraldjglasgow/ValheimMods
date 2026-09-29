"""Filters, three ways:

- apply(x, lowpass(800), peak(2500, 2, 6)...): fixed biquads (Robert Bristow-Johnson's cookbook), run as an exact
  FFT convolution with the chain's impulse response (scipy is blocked on this machine, and a Python sample loop per
  filter would be slow);
- svf(x, cutoff, q, 'band'): a state-variable filter whose cutoff and Q may change every sample (sweeps, whooshes);
- spectral(x, response): any response that changes over time, applied frame by frame to a short-time spectrum
  (formant banks for voices, tilts that open and close).
"""
import functools

import numpy as np

import sfx_audio
from kit.core import RATE, curve


def _rbj(kind, freq, q, gain_db, rate):
    w0 = 2 * np.pi * min(freq, 0.49 * rate) / rate
    cos, alpha, a = np.cos(w0), np.sin(w0) / (2 * q), 10 ** (gain_db / 40)
    root = 2 * np.sqrt(a) * alpha
    table = {
        "lowpass": ((1 - cos) / 2, 1 - cos, (1 - cos) / 2, 1 + alpha, -2 * cos, 1 - alpha),
        "highpass": ((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha),
        "bandpass": (alpha, 0.0, -alpha, 1 + alpha, -2 * cos, 1 - alpha),
        "notch": (1.0, -2 * cos, 1.0, 1 + alpha, -2 * cos, 1 - alpha),
        "peak": (1 + alpha * a, -2 * cos, 1 - alpha * a, 1 + alpha / a, -2 * cos, 1 - alpha / a),
        "lowshelf": (a * ((a + 1) - (a - 1) * cos + root), 2 * a * ((a - 1) - (a + 1) * cos),
                     a * ((a + 1) - (a - 1) * cos - root), (a + 1) + (a - 1) * cos + root,
                     -2 * ((a - 1) + (a + 1) * cos), (a + 1) + (a - 1) * cos - root),
        "highshelf": (a * ((a + 1) + (a - 1) * cos + root), -2 * a * ((a - 1) + (a + 1) * cos),
                      a * ((a + 1) + (a - 1) * cos - root), (a + 1) - (a - 1) * cos + root,
                      2 * ((a - 1) - (a + 1) * cos), (a + 1) - (a - 1) * cos - root)}
    return tuple(float(v) for v in table[kind])


def lowpass(freq, q=0.707, rate=RATE):
    return _rbj("lowpass", freq, q, 0.0, rate)


def highpass(freq, q=0.707, rate=RATE):
    return _rbj("highpass", freq, q, 0.0, rate)


def bandpass(freq, q=1.0, rate=RATE):
    """Unity gain at freq; q = freq / bandwidth."""
    return _rbj("bandpass", freq, q, 0.0, rate)


def notch(freq, q=1.0, rate=RATE):
    return _rbj("notch", freq, q, 0.0, rate)


def peak(freq, q, gain_db, rate=RATE):
    return _rbj("peak", freq, q, gain_db, rate)


def lowshelf(freq, gain_db, q=0.707, rate=RATE):
    return _rbj("lowshelf", freq, q, gain_db, rate)


def highshelf(freq, gain_db, q=0.707, rate=RATE):
    return _rbj("highshelf", freq, q, gain_db, rate)


@functools.lru_cache(maxsize=256)
def _response(sections):
    return sfx_audio.impulse_response(sections)


def apply(x, *sections):
    """x through a chain of fixed biquads (each a 6-tuple from the functions above), exactly, by convolution."""
    return sfx_audio.convolve(np.asarray(x, dtype=float), _response(tuple(sections)))


def svf(x, cutoff, q=0.707, mode="low", rate=RATE):
    """A state-variable filter (trapezoidal, stable under fast sweeps); cutoff and q per sample or fixed.
    mode: 'low', 'band', 'high', 'notch' or 'peak'."""
    n = len(x)
    g = np.tan(np.pi * np.clip(curve(cutoff, n), 10, 0.45 * rate) / rate)
    k = 1.0 / curve(q, n)
    a1 = 1.0 / (1.0 + g * (g + k))
    a2, a3 = g * a1, g * g * a1
    low, band = np.zeros(n), np.zeros(n)
    ic1 = ic2 = 0.0
    for i, (v0, c1, c2, c3) in enumerate(zip(x.tolist(), a1.tolist(), a2.tolist(), a3.tolist())):
        v3 = v0 - ic2
        v1 = c1 * ic1 + c2 * v3
        v2 = ic2 + c2 * ic1 + c3 * v3
        ic1, ic2 = 2 * v1 - ic1, 2 * v2 - ic2
        low[i], band[i] = v2, v1
    outputs = {"low": low, "band": band * k, "high": x - k * band - low, "notch": x - k * band,
               "peak": 2 * low - x + k * band}
    return outputs[mode]


def spectral(x, response, rate=RATE, size=1024, hop=256):
    """x filtered by response(freqs, times) -> linear gain, evaluated on a (frames, bins) grid: the frequencies in Hz
    as a row and each frame's centre time in seconds as a column. Hann analysis and synthesis, overlap-add."""
    window = np.hanning(size)
    padded = np.concatenate([np.zeros(size), np.asarray(x, float), np.zeros(size)])
    cut = sfx_audio.frames(padded, size, hop) * window
    spectrum = np.fft.rfft(cut, axis=1)
    times = (np.arange(len(cut)) * hop + size / 2 - size) / rate
    gains = response(np.fft.rfftfreq(size, 1 / rate)[None, :], times[:, None])
    pieces = np.fft.irfft(spectrum * gains, size, axis=1) * window
    out, norm = np.zeros(len(padded) + size), np.zeros(len(padded) + size)
    for index, piece in enumerate(pieces):
        out[index * hop:index * hop + size] += piece
        norm[index * hop:index * hop + size] += window ** 2
    return (out / np.maximum(norm, 1e-6))[size:size + len(x)]


def formants(bank, floor=0.02):
    """A response for spectral(): resonances [(centre Hz or f(t), bandwidth Hz, gain dB), ...] summed, over a floor.
    Centres given as functions of time move (a vowel changing, a throat opening)."""
    def response(freqs, times):
        total = np.full(np.broadcast(freqs, times).shape, floor)
        for centre, width, level in bank:
            hz = centre(times) if callable(centre) else centre
            total = total + 10 ** (level / 20) / (1 + ((freqs - hz) / (width / 2)) ** 2)
        return total
    return response


def tilt(db_per_octave, pivot_hz=1000.0):
    """A response for spectral(): a straight slope in dB per octave through 0 dB at pivot_hz (may be a function of
    time, for a sound that darkens as it fades)."""
    def response(freqs, times):
        slope = db_per_octave(times) if callable(db_per_octave) else db_per_octave
        return 10 ** (slope * np.log2(np.maximum(freqs, 20.0) / pivot_hz) / 20)
    return response

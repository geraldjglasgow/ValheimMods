"""Oscillators. Frequencies may be a number or an array per sample (a glide, a vibrato), so every oscillator runs
from a phase integrated sample by sample. Saw and square are band-limited with polyBLEP, so a 3 kHz whistle does not
fold back into the audible band.
"""
import numpy as np

from kit.core import RATE, count, curve


def phase(freq, seconds, rate=RATE, start=0.0):
    """Phase in cycles (not wrapped) of a frequency curve over seconds."""
    return start + np.cumsum(curve(freq, count(seconds, rate))) / rate


def sine(freq, seconds, rate=RATE, start=0.0):
    return np.sin(2 * np.pi * phase(freq, seconds, rate, start))


def _blep(t, dt):
    """polyBLEP: the correction that rounds a sawtooth's jump over one sample on each side."""
    out = np.zeros_like(t)
    rising = t < dt
    x = t[rising] / dt[rising]
    out[rising] = x + x - x * x - 1
    falling = t > 1 - dt
    x = (t[falling] - 1) / dt[falling]
    out[falling] = x * x + x + x + 1
    return out


def saw(freq, seconds, rate=RATE, start=0.0):
    """A band-limited rising sawtooth, -1 to 1."""
    cycles = phase(freq, seconds, rate, start)
    t, dt = cycles % 1.0, np.maximum(curve(freq, len(cycles)) / rate, 1e-9)
    return 2 * t - 1 - _blep(t, dt)


def square(freq, seconds, width=0.5, rate=RATE):
    """A band-limited pulse of the given duty cycle, the difference of two saws."""
    return (saw(freq, seconds, rate) - saw(freq, seconds, rate, start=width)) * 0.5


def triangle(freq, seconds, rate=RATE):
    """A triangle from the saw's phase (its harmonics fall at 12 dB an octave, so aliasing stays faint)."""
    t = phase(freq, seconds, rate) % 1.0
    return 4 * np.abs(t - 0.5) - 1


def glottal(freq, seconds, open_share=0.6, closing=0.35, rate=RATE):
    """A voice source: the derivative of a Rosenberg glottal pulse per cycle (opening for open_share of the open
    phase, closing fast over the rest, then shut). Its sharp closure gives the bright harmonic buzz that the vocal
    tract (a formant filter) then shapes; about -12 dB an octave above the fundamental."""
    t = phase(freq, seconds, rate) % 1.0
    opening, shutting = open_share * (1 - closing), open_share * closing
    flow = np.where(t < opening, 0.5 - 0.5 * np.cos(np.pi * t / opening), 0.0)
    ramp = (t >= opening) & (t < opening + shutting)
    flow[ramp] = np.cos(0.5 * np.pi * (t[ramp] - opening) / shutting)
    pulse = np.diff(flow, prepend=flow[0])
    return pulse / max(np.abs(pulse).max(), 1e-9)


def fm(carrier, modulator, index, seconds, rate=RATE):
    """Frequency modulation: a sine at carrier Hz whose phase a sine at modulator Hz moves by index radians."""
    return np.sin(2 * np.pi * phase(carrier, seconds, rate) + curve(index, count(seconds, rate))
                  * np.sin(2 * np.pi * phase(modulator, seconds, rate)))


def partials(freqs, amps, decays, seconds, rate=RATE, seed=0, detune=0.0):
    """Modal synthesis: damped sines, one per mode (freqs in Hz, amps linear, decays = time to fall 60 dB, in s).
    detune spreads each mode into two at +-detune semitones, which beat as struck glass and bells do."""
    t = np.arange(count(seconds, rate)) / rate
    rng = np.random.default_rng(seed)
    out = np.zeros_like(t)
    for f, a, d in zip(freqs, amps, decays):
        for shift in ((0.0,) if detune <= 0 else (-detune, detune)):
            hz = f * 2 ** (shift / 12)
            if hz < rate / 2:
                out += a * np.exp(-6.91 * t / d) * np.sin(2 * np.pi * hz * t + rng.uniform(0, 2 * np.pi))
    return out

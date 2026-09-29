"""Waveshaping and modulation: saturation that adds harmonics (a strained throat, a hot recording), folding,
roughness and ring modulation."""
import numpy as np

from kit.core import RATE


def drive(x, amount=2.0, bias=0.0):
    """tanh saturation; amount 1 is gentle, 5 is a snarl; bias > 0 adds even harmonics (an asymmetric growl).
    The output peaks near the input's peak."""
    top = max(np.abs(x).max(), 1e-12)
    y = np.tanh(amount * (x / top + bias)) - np.tanh(amount * bias)
    return y / max(np.abs(y).max(), 1e-12) * top


def fold(x, amount=1.5):
    """Wavefolding: what goes past +-1 folds back, a hard metallic edge."""
    y = x * amount
    return np.abs((y - 1) % 4 - 2) - 1


def crush(x, bits=8):
    """Fewer amplitude steps: a grainy, broken texture."""
    steps = 2 ** (bits - 1)
    return np.round(x * steps) / steps


def roughness(x, rate_hz=40.0, depth=0.5, seed=0, rate=RATE):
    """Amplitude modulation at 20 to 70 Hz, the rasp of a growl or a purr; the rate wobbles a little."""
    rng = np.random.default_rng(seed)
    t = np.arange(len(x)) / rate
    wobble = 1 + 0.15 * np.sin(2 * np.pi * rng.uniform(0.3, 1.2) * t + rng.uniform(0, 6.28))
    mod = 1 - depth * (0.5 + 0.5 * np.sin(2 * np.pi * rate_hz * np.cumsum(wobble) / rate))
    return x * mod


def ring(x, freq, rate=RATE):
    """Ring modulation: sum and difference tones, inharmonic, otherworldly."""
    return x * np.sin(2 * np.pi * freq * np.arange(len(x)) / rate)

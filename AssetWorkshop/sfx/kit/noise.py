"""Noise: white and its colours (shaped in the frequency domain to an exact slope), velvet noise (sparse clicks) and
crackle (random short bursts, the grain of fire, gravel and dry wood)."""
import numpy as np

from kit.core import RATE, count


def white(seconds, seed=0, rate=RATE):
    return np.random.default_rng(seed).standard_normal(count(seconds, rate)) * 0.3


def coloured(seconds, slope_db=-3.0, seed=0, rate=RATE, low_hz=20.0):
    """Noise whose power falls slope_db per octave (pink -3, brown -6, blue +3), flat under low_hz, RMS 0.3."""
    n = count(seconds, rate)
    spectrum = np.fft.rfft(np.random.default_rng(seed).standard_normal(n))
    freqs = np.maximum(np.fft.rfftfreq(n, 1 / rate), low_hz)
    spectrum *= (freqs / 1000.0) ** (slope_db / 6.0206)
    out = np.fft.irfft(spectrum, n)
    return out * (0.3 / max(out.std(), 1e-12))


def pink(seconds, seed=0, rate=RATE):
    return coloured(seconds, -3.0, seed, rate)


def brown(seconds, seed=0, rate=RATE):
    return coloured(seconds, -6.0, seed, rate)


def blue(seconds, seed=0, rate=RATE):
    return coloured(seconds, 3.0, seed, rate)


def velvet(seconds, density_hz=2000.0, seed=0, rate=RATE):
    """Sparse +-1 impulses, density_hz of them a second at random places: smooth-sounding noise at high densities,
    a crackle at low ones."""
    rng, n = np.random.default_rng(seed), count(seconds, rate)
    out = np.zeros(n)
    hits = rng.integers(0, n, int(density_hz * seconds))
    out[hits] = rng.choice([-1.0, 1.0], len(hits))
    return out


def events(seconds, rate_hz, seed=0, regularity=0.0):
    """Times in seconds of random events at rate_hz on average (Poisson; regularity 0..1 evens the gaps)."""
    rng = np.random.default_rng(seed)
    gaps = rng.exponential(1.0 / rate_hz, int(seconds * rate_hz * 3) + 8)
    gaps = gaps * (1 - regularity) + regularity / rate_hz
    times = np.cumsum(gaps)
    return times[times < seconds]


def thinning(seconds, start_hz, half_life_s, seed=0, floor_hz=0.0):
    """Times of events whose rate starts at start_hz and halves every half_life_s (never under floor_hz): the
    shower of pieces after a break, dense at first and petering out."""
    rng = np.random.default_rng(seed)
    candidates = events(seconds, start_hz + floor_hz, seed)
    rate_at = start_hz * 0.5 ** (candidates / half_life_s) + floor_hz
    return candidates[rng.uniform(0, start_hz + floor_hz, len(candidates)) < rate_at]


def burst(length_s, seed=0, rate=RATE, decay_s=None):
    """One short noise burst with an exponential decay (a crackle, a grain of gravel)."""
    n = count(length_s, rate)
    decay = decay_s or length_s / 4
    return np.random.default_rng(seed).standard_normal(n) * np.exp(-np.arange(n) / rate / decay)


def crackle(seconds, rate_hz=12.0, seed=0, rate=RATE, length_s=(0.0005, 0.004), loudness=(0.05, 1.0)):
    """Random short bursts at rate_hz: each 0.5 to 4 ms, its level drawn so most are small and a few are loud
    (squared uniform), the grain of burning wood and trickling gravel. Filter it for colour."""
    rng, n = np.random.default_rng(seed), count(seconds, rate)
    out = np.zeros(n)
    for number, t in enumerate(events(seconds, rate_hz, seed)):
        piece = burst(rng.uniform(*length_s), seed + number + 1, rate)
        start = int(t * rate)
        piece = piece[:n - start] * (loudness[0] + (loudness[1] - loudness[0]) * rng.uniform() ** 2)
        out[start:start + len(piece)] += piece
    return out

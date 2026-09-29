"""Time, gain and mixing: the small helpers every recipe uses."""
import numpy as np

RATE = 44100


def count(seconds, rate=RATE):
    """Samples in a duration."""
    return max(1, int(round(seconds * rate)))


def timeline(seconds, rate=RATE):
    """The time in seconds of every sample of a sound that long."""
    return np.arange(count(seconds, rate)) / rate


def gain(db):
    """Decibels to a linear factor."""
    return 10.0 ** (np.asarray(db, dtype=float) / 20.0)


def db(value):
    """A linear amplitude to decibels (-120 for silence)."""
    return 20 * np.log10(np.maximum(np.abs(value), 1e-6))


def curve(value, n):
    """A scalar or an array stretched to n samples (arrays are resampled linearly)."""
    value = np.asarray(value, dtype=float)
    if value.ndim == 0:
        return np.full(n, float(value))
    if len(value) == n:
        return value
    return np.interp(np.linspace(0, 1, n), np.linspace(0, 1, len(value)), value)


def fade(x, in_s=0.002, out_s=0.010, rate=RATE):
    """Raised-cosine fades at both ends, so no clip starts or stops with a click."""
    y = np.array(x, dtype=float, copy=True)
    rising, falling = min(count(in_s, rate), len(y)), min(count(out_s, rate), len(y))
    rise = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, rising))
    fall = 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, falling))
    y[:rising] *= rise if y.ndim == 1 else rise[:, None]
    y[len(y) - falling:] *= fall if y.ndim == 1 else fall[:, None]
    return y


def pad(x, length):
    """x padded with silence (or cut) to length samples."""
    if len(x) >= length:
        return x[:length]
    shape = (length - len(x),) + x.shape[1:]
    return np.concatenate([x, np.zeros(shape)])


def mix(*layers):
    """The sum of layers of any lengths (mono and stereo may be mixed: mono is spread to both sides)."""
    layers = [np.asarray(layer, dtype=float) for layer in layers if layer is not None and len(layer)]
    stereo = any(layer.ndim == 2 for layer in layers)
    length = max(len(layer) for layer in layers)
    out = np.zeros((length, 2) if stereo else length)
    for layer in layers:
        layer = np.repeat(layer[:, None], 2, axis=1) if stereo and layer.ndim == 1 else layer
        out[:len(layer)] += layer
    return out


def place(x, at_s, rate=RATE):
    """x delayed by at_s seconds (silence before it)."""
    return np.concatenate([np.zeros((count(at_s, rate),) + x.shape[1:]), x]) if at_s > 0 else x


def peak(x, peak_db=-1.0):
    """x scaled so its highest sample is at peak_db dBFS."""
    top = np.abs(x).max()
    return x * (gain(peak_db) / top) if top > 0 else x


def stereo(x, width=0.0, seed=0, rate=RATE):
    """A mono sound as stereo; width 0 keeps it centred, up to 1 decorrelates the sides with short delays."""
    if x.ndim == 2:
        return x
    if width <= 0:
        return np.stack([x, x], axis=1)
    rng = np.random.default_rng(seed)
    left, right = (count(d, rate) for d in rng.uniform(0.0003, 0.0012, 2) * width)
    return np.stack([np.concatenate([np.zeros(left), x])[:len(x)], np.concatenate([np.zeros(right), x])[:len(x)]], 1)


def trim(x, floor_db=-70.0, rate=RATE, keep_s=0.02):
    """x without its silent tail (under floor_db below its peak), keeping keep_s seconds of it."""
    level = np.abs(x) if x.ndim == 1 else np.abs(x).max(axis=1)
    loud = np.flatnonzero(level >= np.abs(x).max() * gain(floor_db))
    end = min(len(x), (loud[-1] if len(loud) else 0) + count(keep_s, rate))
    return x[:end]

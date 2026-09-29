"""A feedback delay network reverb: eight delay lines of mutually prime lengths, mixed back into each other through a
Householder matrix (energy preserving), each line's gain set so the tail falls 60 dB in rt60 seconds, and a two-tap
low-pass in every loop so the highs die first, as in a real room. Processed in blocks no longer than the shortest
line, so every block is plain numpy.

The game's mixer already puts a short room on every sound in the SFX group (SFX Reverb: 0.5 s decay, high
frequencies decaying at 0.16 of that) and a 3.5 s hall on SFX_LARGE, and ZSFX raises the reverb send with distance:
bake only the space that belongs to the sound itself (a cave creature's own echo, a boss's roar), and keep it short.
"""
import numpy as np

from kit.core import RATE, count

PRIMES = (1009, 1123, 1237, 1361, 1493, 1621, 1777, 1913, 2053, 2203, 2357, 2503, 2663, 2819, 2971, 3137, 3307)


def delays(size, rate=RATE, lines=8):
    """Delay lengths in samples for a space of relative size (1 = a hall about 10 m across)."""
    scale = size * rate / 44100
    chosen = [int(PRIMES[i * 2] * scale) | 1 for i in range(lines)]
    return np.array(sorted(set(max(64, d) for d in chosen)))


class _Network:
    """The delay lines' memory, written at time t + offset so a read t - length never falls before the start."""

    def __init__(self, lengths, total, rt60, damping, seed, rate):
        rng = np.random.default_rng(seed)
        self.lengths, self.offset, self.damping = lengths, int(lengths.max()), damping
        self.gains = 10 ** (-3 * lengths / (rt60 * rate))
        self.matrix = np.eye(len(lengths)) - 2.0 / len(lengths)
        self.taps = rng.choice([-1.0, 1.0], (2, len(lengths))) / np.sqrt(len(lengths))
        self.inputs = rng.choice([-1.0, 1.0], len(lengths))
        self.memory = np.zeros((len(lengths), total + self.offset))
        self.last = np.zeros(len(lengths))

    def step(self, start, feed):
        """One block: read every line, damp, mix through the matrix, write back with the input; returns the output."""
        stop, at = start + len(feed), start + self.offset
        reads = np.stack([self.memory[i, at - d:at - d + len(feed)] for i, d in enumerate(self.lengths)])
        damped = (1 - self.damping) * reads + self.damping * np.concatenate([self.last[:, None], reads[:, :-1]], 1)
        self.last = reads[:, -1]
        self.memory[:, at:stop + self.offset] = self.matrix @ (self.gains[:, None] * damped) \
            + self.inputs[:, None] * feed
        return (self.taps @ reads).T


def fdn(x, rt60=1.2, size=1.0, damping=0.35, seed=0, rate=RATE):
    """The wet signal only (stereo, frames x 2) of a dry mono x, with a tail of rt60 seconds added to its length."""
    lengths = delays(size, rate)
    block, total = int(lengths.min()), len(x) + count(rt60 * 1.1, rate)
    feed = np.zeros(total)
    feed[:len(x)] = x
    network, out = _Network(lengths, total, rt60, damping, seed, rate), np.zeros((total, 2))
    for start in range(0, total, block):
        out[start:start + block] = network.step(start, feed[start:start + block])
    return out


def early(x, size=1.0, seed=0, rate=RATE, count_=6):
    """A few discrete early reflections (5 to 40 ms scaled by size), falling in level: a sense of walls."""
    rng = np.random.default_rng(seed)
    times = np.sort(rng.uniform(0.005, 0.040, count_)) * size
    out = np.zeros(len(x) + count(times.max() + 0.01, rate))
    for number, t in enumerate(times):
        start = count(t, rate)
        out[start:start + len(x)] += x * (0.6 ** (number + 1)) * rng.choice([-1, 1])
    return out


def room(x, rt60=0.6, size=0.6, mix=0.2, damping=0.4, seed=0, rate=RATE):
    """x (mono) in a space: dry, early reflections and the FDN tail, as stereo; mix is the wet share (0..1)."""
    wet = fdn(x, rt60, size, damping, seed, rate)
    first = early(x, size, seed, rate)
    length = max(len(wet), len(first))
    stereo = np.zeros((length, 2))
    stereo[:len(x)] += (1 - mix) * x[:, None]
    stereo[:len(first)] += 0.5 * mix * first[:, None]
    stereo[:len(wet)] += mix * wet
    return stereo

"""Instruments for the trailer's score, synthesised with numpy and scipy (no samples): war drums, bone rattles, drones,
a throat-singer, horns, tremolo and staccato strings, a glassy pad, a plucked tagelharpa, risers and impacts, played into
a stereo bus with a convolution hall."""
import numpy as np
from scipy import signal

SR = 48000
RNG = np.random.default_rng(7)
_STEPS = {"C": -9, "C#": -8, "Db": -8, "D": -7, "Eb": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "Ab": -1, "A": 0, "Bb": 1, "B": 2}


def hz(name):
    """A note's frequency: "D3", "Bb2"."""
    return 440.0 * 2 ** ((_STEPS[name[:-1]] + 12 * (int(name[-1]) - 4)) / 12)


def tt(seconds):
    return np.arange(int(seconds * SR)) / SR


def noise(seconds):
    return RNG.standard_normal(int(seconds * SR))


def filt(x, kind, freq, order=2):
    return signal.sosfilt(signal.butter(order, freq, btype=kind, fs=SR, output="sos"), x)


def envelope(seconds, attack, release, curve=1.0):
    n = int(seconds * SR)
    e = np.ones(n)
    a = min(n, int(attack * SR))
    r = min(n - a, int(release * SR))
    if a:
        e[:a] = np.linspace(0, 1, a) ** curve
    if r:
        e[n - r:] *= np.linspace(1, 0, r) ** curve
    return e


def saw(freq, n):
    phase = np.cumsum(np.broadcast_to(freq, (n,))) / SR + RNG.random()
    return 2 * (phase % 1.0) - 1


def ensemble(freqs, seconds, detune=0.004, voices=3, vibrato=0.0):
    t = tt(seconds)
    out = np.zeros_like(t)
    for f in freqs:
        for v in range(voices):
            d = 1 + detune * (v - (voices - 1) / 2)
            out += saw(f * d * (1 + vibrato * np.sin(2 * np.pi * 5.1 * t + RNG.random() * 6)), len(t))
    return out / np.sqrt(len(freqs) * voices)


# ---------------------------------------------------------------- percussion
def drum(base, decay, drop=1.2, skin=0.8, click=0.5):
    length = max(0.35, decay * 4)
    t = tt(length)
    phase = 2 * np.pi * np.cumsum(base * (1 + drop * np.exp(-t / 0.025))) / SR
    body = np.sin(phase) * np.exp(-t / decay) + 0.3 * np.sin(1.59 * phase) * np.exp(-t / (decay * 0.35))
    n = noise(length)
    hide = filt(n, "bandpass", [base * 2, min(base * 9, 9000)]) * np.exp(-t / 0.06) * skin
    stick = filt(n, "lowpass", 3000) * np.exp(-t / 0.008) * click
    return np.tanh(1.3 * (body + hide + stick)) * 0.8


def taiko(pitch=1.0, decay=0.55):
    return drum(52 * pitch, decay)


def tom(f=105, decay=0.22):
    return drum(f, decay, skin=0.6)


def frame_drum(f=185):
    return drum(f, 0.09, skin=1.2, click=0.8)


def rattle():
    """Bones knocking together: a few dry clicks in 50 ms."""
    out = np.zeros(int(0.12 * SR))
    click = noise(0.02) * np.exp(-tt(0.02) / 0.003)
    for _ in range(6):
        i = int(RNG.uniform(0, 0.05) * SR)
        out[i:i + len(click)] += click * RNG.uniform(0.4, 1.0)
    return filt(out, "bandpass", [1800, 7000]) * 0.6


# ---------------------------------------------------------------- sustained
def drone(freqs, seconds, cutoff=500, attack=2.0, release=2.0):
    return filt(ensemble(freqs, seconds, 0.005, 3), "lowpass", cutoff) * envelope(seconds, attack, release)


def horn(freqs, seconds, attack=0.25, release=0.5):
    """Low brass: dark until the breath opens it, a little growl, a slow vibrato."""
    x = ensemble(freqs, seconds, 0.003, 3, vibrato=0.003)
    t = tt(seconds)
    bright = 0.2 + 0.8 * np.clip(t / max(attack, 0.02), 0, 1) * np.exp(-np.maximum(t - attack, 0) / 1.2)
    y = filt(x, "lowpass", 380) * (1 - bright) + filt(x, "lowpass", 2200) * bright
    return np.tanh(1.6 * y) * envelope(seconds, attack, release)


def tremolo_strings(freqs, seconds, attack=1.0, release=0.4, rate=11.0):
    x = filt(ensemble(freqs, seconds, 0.006, 5, vibrato=0.002), "bandpass", [250, 6000])
    bow = 0.35 + 0.65 * np.abs(np.sin(np.pi * rate * tt(seconds)))
    return x * bow * envelope(seconds, attack, release, curve=2)


def staccato(freq, seconds=0.2):
    x = filt(ensemble([freq], seconds, 0.006, 3), "lowpass", 1600)
    return x * np.exp(-tt(seconds) / 0.07) * envelope(seconds, 0.004, 0.02)


def glass(freqs, seconds):
    """A cold, glassy pad: detuned sines and their octaves, shimmering slowly."""
    t = tt(seconds)
    out = np.zeros_like(t)
    for f in freqs:
        for d in (0.997, 1.0, 1.003):
            out += np.sin(2 * np.pi * f * d * t + RNG.random() * 6)
        out += 0.3 * np.sin(2 * np.pi * 2 * f * t + RNG.random() * 6)
    shimmer = 0.75 + 0.25 * np.sin(2 * np.pi * 0.3 * t)
    return out / (3 * len(freqs)) * shimmer * envelope(seconds, 1.5, 1.5)


_VOWELS = {"u": (325, 700, 2530), "o": (450, 800, 2830), "a": (730, 1090, 2440), "e": (530, 1840, 2480)}


def throat(seconds, f0, vowels=("o", "a", "o"), whistle=None):
    """A Norse throat-singer: a low voice (additive harmonics shaped by moving vowel formants) and, if given, an overtone
    whistle that steps along those harmonic numbers."""
    t = tt(seconds)
    n, step = len(t), 240
    ctrl = np.arange(0, n + step, step)
    pos = np.linspace(0, len(vowels) - 1, len(ctrl))
    forms = np.array([[np.interp(pos, range(len(vowels)), [_VOWELS[v][i] for v in vowels])] for i in range(3)])[:, 0]
    if whistle:
        wpos = np.linspace(0, len(whistle) - 1, len(ctrl))
        wcenter = f0 * np.interp(np.round(wpos * 4) / 4, range(len(whistle)), whistle)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 4.8 * t) + 0.002 * np.sin(2 * np.pi * 0.7 * t)
    phase = 2 * np.pi * np.cumsum(f0 * vib) / SR
    out = np.zeros(n)
    for k in range(1, int(4800 / f0)):
        fk = k * f0
        amp = sum(w / (1 + ((fk - forms[i]) / b) ** 2) for i, (w, b) in enumerate(((1.0, 90), (0.7, 110), (0.25, 160))))
        if whistle:
            amp = amp + 2.5 / (1 + ((fk - wcenter) / 30) ** 2)
        out += np.interp(np.arange(n), ctrl, amp) / k ** 0.6 * np.sin(k * phase)
    breath = filt(noise(seconds), "bandpass", [300, 3000]) * 0.02
    return (out / np.max(np.abs(out)) + breath) * envelope(seconds, 0.8, 1.0)


def pluck(freq, seconds=1.8):
    """A plucked bowed-lyre string (Karplus-Strong) with a wooden body."""
    period = max(2, int(SR / freq))
    buf = RNG.uniform(-1, 1, period)
    out = np.empty(int(seconds * SR))
    for i in range(len(out)):
        j = i % period
        out[i] = buf[j]
        buf[j] = 0.497 * (buf[j] + buf[(j + 1) % period])
    return filt(out, "bandpass", [150, 3500])


# ---------------------------------------------------------------- effects
def sweep(seconds, f_from, f_to, width=0.35):
    """Noise whose band climbs from f_from to f_to, growing louder: a riser."""
    x = noise(seconds)
    f, tm, z = signal.stft(x, fs=SR, nperseg=2048)
    centers = f_from * (f_to / f_from) ** (tm / seconds)
    mask = np.exp(-0.5 * (np.log2((f[:, None] + 1) / centers[None, :]) / width) ** 2)
    y = signal.istft(z * mask, fs=SR, nperseg=2048)[1][:len(x)]
    return y / np.max(np.abs(y)) * np.linspace(0, 1, len(y)) ** 2


def reverse_cymbal(seconds):
    return filt(noise(seconds), "highpass", 3000) * (tt(seconds) / seconds) ** 3 * 0.5


def crash(decay=1.4):
    t = tt(decay * 3)
    metal = sum(np.sin(2 * np.pi * f * t) * np.exp(-t / (decay * r)) for f, r in ((3150, 0.6), (4270, 0.5), (5490, 0.4), (6820, 0.3)))
    wash = filt(noise(decay * 3), "highpass", 2500) * np.exp(-t / decay)
    return (wash * 0.6 + metal * 0.08) * envelope(decay * 3, 0.004, 0.2)


def sub_drop(seconds=2.5):
    t = tt(seconds)
    return np.sin(2 * np.pi * np.cumsum(28 + 42 * np.exp(-t / 0.4)) / SR) * np.exp(-t / 1.1)


# ---------------------------------------------------------------- the bus
class Bus:
    """The mix: every part placed at a time, panned, with its share sent to the hall."""

    def __init__(self, seconds):
        self.dry = np.zeros((2, int(seconds * SR)))
        self.send = np.zeros_like(self.dry)

    def add(self, sig, at, gain=1.0, pan=0.0, verb=0.25):
        i = int(at * SR)
        if i < 0:
            sig, i = sig[-i:], 0
        sig = sig[: max(0, self.dry.shape[1] - i)] * gain
        a = (pan + 1) * np.pi / 4
        for ch, w in ((0, np.cos(a) * 1.414), (1, np.sin(a) * 1.414)):
            self.dry[ch, i:i + len(sig)] += sig * w
            self.send[ch, i:i + len(sig)] += sig * w * verb

    def render(self, rt=2.6, wet=0.45):
        n = self.dry.shape[1]
        ir = _hall(rt)
        return self.dry + wet * np.stack([signal.fftconvolve(self.send[c], ir[c])[:n] for c in (0, 1)])


def _hall(rt):
    n = int((rt + 0.3) * SR)
    t = np.arange(n) / SR
    ir = RNG.standard_normal((2, n)) * np.exp(-6.9 * t / rt)
    ir = np.stack([filt(filt(ir[c], "lowpass", 5500), "highpass", 120) for c in (0, 1)])
    ir = np.concatenate([np.zeros((2, int(0.02 * SR))), ir], axis=1)
    return ir / np.sqrt(np.sum(ir ** 2, axis=1, keepdims=True))


def lowpass_curve(x, times, cutoffs):
    """The whole mix through a lowpass whose cutoff follows (times, cutoffs): underwater, dazed, opening up again."""
    out = np.empty_like(x)
    for c in range(x.shape[0]):
        f, tm, z = signal.stft(x[c], fs=SR, nperseg=2048)
        cut = np.exp(np.interp(tm, times, np.log(cutoffs)))
        z *= 1 / np.sqrt(1 + (f[:, None] / cut[None, :]) ** 4)
        out[c] = signal.istft(z, fs=SR, nperseg=2048)[1][: x.shape[1]]
    return out

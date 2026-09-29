"""Levels the game's way: loudness measured with the codex's own BS.1770 code (sfx_loudness), matched to a target,
and a peak limiter so nothing clips."""
import numpy as np

import sfx_audio
import sfx_loudness
from kit.core import RATE, count, gain


def momentary_max(x, rate=RATE):
    """The loudest 400 ms in LUFS, on the mid of a stereo sound (as the codex measures the game's one-shots)."""
    return sfx_loudness.momentary_max(sfx_audio.mid(np.atleast_2d(x.T).T), rate)


def integrated(x, rate=RATE):
    """Integrated loudness in LUFS on the mid (for loops)."""
    return sfx_loudness.integrated(sfx_audio.mid(np.atleast_2d(x.T).T), rate)


def limit(x, ceiling_db=-1.0, lookahead_s=0.003, release_s=0.06, rate=RATE):
    """A look-ahead peak limiter: gain falls before any peak over the ceiling and recovers over release_s."""
    level = np.abs(x) if x.ndim == 1 else np.abs(x).max(axis=1)
    need = np.minimum(1.0, gain(ceiling_db) / np.maximum(level, 1e-9))
    width = count(lookahead_s, rate)
    ahead = sfx_audio.frames(np.concatenate([need, np.ones(width)]), width + 1, 1)[:len(need)].min(axis=1)
    ramp = np.convolve(ahead, np.ones(width) / width, mode="full")[:len(need)]
    target, coef = np.minimum(ramp, ahead).tolist(), 1.0 - np.exp(-1.0 / count(release_s, rate))
    held, g = np.empty(len(target)), 1.0
    for i, t in enumerate(target):
        g = t if t < g else g + (t - g) * coef
        held[i] = g
    return x * (held if x.ndim == 1 else held[:, None])


def peak_to_loudness(x, plr_db=10.0, rate=RATE):
    """x with its peaks limited to plr_db over the RMS of its loudest 400 ms, whatever its level: the game's breaks
    and hits peak 10 to 17 dB over their momentary loudness (peak_dbfs minus momentary_max_lufs in the codex), a raw
    synthetic transient 15 to 20."""
    mono = sfx_audio.mid(np.atleast_2d(x.T).T)
    width = count(0.4, rate)
    loudest = np.sqrt(np.convolve(mono ** 2, np.ones(width) / width, mode="valid").max()) if len(mono) > width \
        else np.sqrt((mono ** 2).mean())
    scale = gain(-1.0) / max(np.abs(x).max(), 1e-12)
    ceiling = 20 * np.log10(max(loudest * scale, 1e-9)) + plr_db
    return limit(x * scale, min(ceiling, -1.0), rate=rate) / scale


def compress(x, threshold_db=-12.0, ratio=4.0, attack_s=0.005, release_s=0.12, rate=RATE):
    """A downward compressor on the 5 ms RMS level: above threshold_db under the sound's own loudest moment (so it
    works whatever the raw level), the level rises only 1/ratio as fast. The game's impacts and voices peak 12 to
    15 dB over their RMS (crest); a synthetic strike starts near 20, and this brings it down."""
    mono = np.abs(x) if x.ndim == 1 else np.abs(x).max(axis=1)
    width = count(0.005, rate)
    rms = 20 * np.log10(np.maximum(np.sqrt(np.convolve(mono ** 2, np.ones(width) / width, mode="same")), 1e-9))
    over = np.maximum(rms - (rms.max() + threshold_db), 0.0)
    target = (10 ** (-over * (1 - 1 / ratio) / 20)).tolist()
    down, up = 1.0 - np.exp(-1.0 / count(attack_s, rate)), 1.0 - np.exp(-1.0 / count(release_s, rate))
    held, g = np.empty(len(target)), 1.0
    for i, t in enumerate(target):
        g += (t - g) * (down if t < g else up)
        held[i] = g
    return x * (held if x.ndim == 1 else held[:, None])


def match(x, target_lufs, loop=False, rate=RATE, ceiling_db=-1.0):
    """x at target_lufs (momentary max for one-shots, integrated for loops), limited under ceiling_db."""
    y = x
    for _ in range(4):
        current = integrated(y, rate) if loop else momentary_max(y, rate)
        if abs(target_lufs - current) < 0.1:
            break
        y = y * gain(target_lufs - current)
        if np.abs(y).max() > gain(ceiling_db):
            y = limit(y, ceiling_db, rate=rate)
    return y

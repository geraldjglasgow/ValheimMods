"""The numbers the codex measures on every sound: level, timing, spectrum and pitch. The same function measures the
game's clips (sfx_clips.py) and the workshop's new sounds (sfx/compare.py), so the two are always comparable.

Conventions, stated once:
- A stereo clip is measured on its mid (the average of its channels), what a fully 3D AudioSource plays; `width` says
  how much of it is in the sides.
- The envelope is the RMS of 10 ms frames every 5 ms, in dB. The peak is its loudest frame. Onset: the first frame
  within 30 dB of the peak. Attack: onset to the first frame within 3 dB of the peak. Decay: from there to the last
  frame within 20 dB of the peak. Tail: to the last frame within 40 dB.
- The spectrum is the energy-weighted mean power spectrum (2048-point Hann frames, hop 512) over the frames within
  30 dB of the loudest frame. Bands: low under 300 Hz, mid 300 to 3000 Hz, high over 3000 Hz, as shares of the energy.
- Flatness is Wiener entropy (geometric over arithmetic mean power, 50 Hz to 12 kHz) averaged over those frames:
  near 0 for a pure tone, 0.1 to 0.3 for a voice or a ringing hit, 0.4 and up for hiss and wind.
- Pitch: YIN on the loud frames (50 to 1500 Hz, threshold 0.2); reported when at least 30 % of them are voiced.
"""
import numpy as np

import sfx_audio
import sfx_loudness

BANDS = {"low": (0, 300), "mid": (300, 3000), "high": (3000, 1e9)}
FFT, HOP = 2048, 512


def envelope(mono, rate, frame_s=0.010, hop_s=0.005):
    """(dB per frame, hop in seconds) of the RMS envelope."""
    size, hop = max(1, int(rate * frame_s)), max(1, int(rate * hop_s))
    cut = sfx_audio.frames(mono.astype(np.float32), size, hop)
    return sfx_audio.power_db((cut.astype(np.float64) ** 2).mean(axis=1)), hop / rate


def timing(env_db, hop_s):
    """Onset, attack, decay, tail and body (time within 20 dB of the peak), in seconds."""
    peak = env_db.max()

    def first(level):
        return int(np.argmax(env_db >= peak - level))

    def last(level):
        return len(env_db) - 1 - int(np.argmax(env_db[::-1] >= peak - level))

    onset, top = first(30), first(3)
    return {"onset_s": onset * hop_s, "attack_s": (top - onset) * hop_s, "decay_s": (last(20) - top) * hop_s,
            "tail_s": (last(40) - top) * hop_s, "body_s": float((env_db >= peak - 20).sum() * hop_s)}


def spectra(mono, rate):
    """(power spectra of the loud frames (frames, bins), bin frequencies in Hz)."""
    cut = sfx_audio.frames(mono.astype(np.float32), FFT, HOP) * np.hanning(FFT).astype(np.float32)
    power = np.abs(np.fft.rfft(cut, axis=1)) ** 2
    energy = power.sum(axis=1)
    loud = power[energy >= energy.max() * 1e-3]
    return loud, np.fft.rfftfreq(FFT, 1 / rate)


def spectral(mono, rate):
    """Centroid, bandwidth (spread), 85 % roll-off, dominant frequency, band shares and flatness."""
    power, freqs = spectra(mono, rate)
    mean = power.sum(axis=0)
    total = mean.sum() or 1e-24
    centroid = float((freqs * mean).sum() / total)
    result = {"centroid_hz": centroid, "bandwidth_hz": float(np.sqrt(((freqs - centroid) ** 2 * mean).sum() / total)),
              "rolloff_hz": float(freqs[min(len(freqs) - 1, np.searchsorted(np.cumsum(mean), 0.85 * total))]),
              "peak_hz": float(freqs[1 + np.argmax(mean[1:])]), "flatness": flatness(power, freqs)}
    for band, (low, high) in BANDS.items():
        result[band] = float(mean[(freqs >= low) & (freqs < high)].sum() / total)
    return result


OCTAVES = (125, 250, 500, 1000, 2000, 4000, 8000, 16000)


def flatness(power, freqs):
    """Noisiness: Wiener entropy (geometric over arithmetic mean power) inside each octave from 125 Hz to 16 kHz,
    averaged over the octaves weighted by their energy, then over the frames. Measured per octave so a spectrum's
    slope does not count: white, pink and brown noise all read about 0.56, a pure tone near 0."""
    ratios, weights = [], []
    for low, high in zip(OCTAVES, OCTAVES[1:]):
        band = power[:, (freqs >= low) & (freqs < high)] + 1e-24
        if band.shape[1] < 2:
            continue
        ratios.append(np.exp(np.log(band).mean(axis=1)) / band.mean(axis=1))
        weights.append(band.sum(axis=1))
    if not ratios:
        return 0.0
    ratios, weights = np.array(ratios), np.array(weights)
    per_frame = (ratios * weights).sum(axis=0) / np.maximum(weights.sum(axis=0), 1e-30)
    return float(per_frame.mean())


def pitch(mono, rate, low_hz=50.0, high_hz=1500.0, threshold=0.2):
    """(median f0 in Hz or None, voiced share, f0 spread p10..p90 in semitones) by YIN over the loud frames."""
    width = int(rate / low_hz) + 1
    cut = sfx_audio.frames(mono.astype(np.float64), 2 * width, width // 2)
    energy = (cut ** 2).sum(axis=1)
    cut = cut[energy >= energy.max() * 1e-2][:400]
    shortest = int(rate / high_hz)
    diff = yin_difference(cut, width)
    cumulative = diff[:, 1:] * np.arange(1, width) / np.maximum(np.cumsum(diff[:, 1:], axis=1), 1e-20)
    lags = [valley(row[shortest - 1:], threshold) for row in cumulative]
    found = np.array([lag + shortest for lag in lags if lag is not None])
    voiced = len(found) / max(1, len(lags))
    if voiced < 0.3:
        return None, float(voiced), 0.0
    f0 = rate / found
    spread = 12 * np.log2(np.percentile(f0, 90) / np.percentile(f0, 10))
    return float(np.median(f0)), float(voiced), float(spread)


def valley(row, threshold):
    """YIN's pick: the first dip under the threshold, followed down to its minimum, refined by a parabola through the
    minimum and its neighbours (a fractional lag); None when the row never dips under the threshold."""
    under = np.flatnonzero(row < threshold)
    if not len(under):
        return None
    at = int(under[0])
    while at + 1 < len(row) and row[at + 1] < row[at]:
        at += 1
    if 0 < at < len(row) - 1:
        left, mid, right = row[at - 1], row[at], row[at + 1]
        bend = left - 2 * mid + right
        return at + (0.5 * (left - right) / bend if bend > 0 else 0.0)
    return float(at)


def yin_difference(cut, width):
    """YIN's difference function d(lag) for lags 0..width-1 of every frame (frames of 2 * width samples)."""
    head = cut[:, :width]
    spectrum = np.fft.rfft(cut, 4 * width, axis=1)
    cross = np.fft.irfft(spectrum * np.conj(np.fft.rfft(head, 4 * width, axis=1)), axis=1)[:, :width]
    squares = np.concatenate([np.zeros((len(cut), 1)), np.cumsum(cut ** 2, axis=1)], axis=1)
    moving = squares[:, np.arange(width) + width] - squares[:, np.arange(width)]
    return (head ** 2).sum(axis=1, keepdims=True) + moving - 2 * cross


def level(samples, rate):
    """Peak, RMS over the body, crest, integrated and momentary-max loudness, and the stereo width."""
    mono = sfx_audio.mid(samples)
    env_db, _ = envelope(mono, rate)
    body = 10 ** (env_db[env_db >= env_db.max() - 20] / 10)
    rms = float(sfx_audio.power_db(body.mean()))
    peak = float(sfx_audio.db(np.abs(samples).max()))
    width = 0.0
    if samples.ndim == 2 and samples.shape[1] == 2:
        side = ((samples[:, 0] - samples[:, 1]) / 2).std()
        width = float(side / max(mono.std(), 1e-12))
    return {"peak_dbfs": peak, "rms_dbfs": rms, "crest_db": peak - rms, "width": width,
            "loudness_lufs": sfx_loudness.integrated(mono, rate),
            "momentary_max_lufs": sfx_loudness.momentary_max(mono, rate)}


def measure(samples, rate):
    """Every number above for one clip, rounded for the data file."""
    mono = sfx_audio.mid(samples)
    result = {"length_s": len(mono) / rate, "rate": int(rate), "channels": int(samples.shape[1])}
    result.update(level(samples, rate))
    result.update(timing(*envelope(mono, rate)))
    result.update(spectral(mono, rate))
    f0, voiced, spread = pitch(mono, rate)
    result.update({"pitch_hz": f0, "voiced": voiced, "pitch_spread_st": spread})
    if result["length_s"] >= 6.0:
        result["range_lu"] = sfx_loudness.loudness_range(mono, rate)
    return {k: (round(v, 4) if isinstance(v, float) else v) for k, v in result.items()}

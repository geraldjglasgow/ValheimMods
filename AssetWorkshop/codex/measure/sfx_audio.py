"""Reads audio for the sound measurements: the game's clips in the reference export (OGG Vorbis) and the workshop's own
(WAV, OGG). Decoding goes through soundfile (libsndfile), which is installed in the workshop's venv, not the system
Python; run every sfx script with it:

    python -m venv AssetWorkshop/out/venv
    AssetWorkshop/out/venv/Scripts/python -m pip install -r AssetWorkshop/sfx/requirements.txt
    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx.py

Nothing read here may be copied into the repository: the codex keeps measurements and words only.
"""
import os

import numpy as np

ROOT = os.path.join(os.environ.get("USERPROFILE", ""), "ValheimReference", "ExportedProject", "Assets")


def _soundfile():
    try:
        import soundfile
    except ImportError as error:
        raise SystemExit("soundfile is missing: run this with AssetWorkshop/out/venv/Scripts/python "
                         "(see AssetWorkshop/sfx/README.md)") from error
    return soundfile


def full_path(path):
    """An absolute path, or a path relative to the reference export's Assets folder."""
    return path if os.path.isabs(path) else os.path.join(ROOT, path)


def read(path):
    """(samples, rate): the file as float32 samples shaped (frames, channels), and its sample rate in Hz."""
    data, rate = _soundfile().read(full_path(path), dtype="float32", always_2d=True)
    return data, rate


def info(path):
    """(length in seconds, sample rate, channels) from the file's header, without decoding it."""
    header = _soundfile().info(full_path(path))
    return header.duration, header.samplerate, header.channels


def mid(samples):
    """The average of the channels: what a fully 3D AudioSource plays of a stereo clip, and a mono clip unchanged."""
    return samples.mean(axis=1) if samples.ndim == 2 else samples


def db(value, floor=-120.0):
    """Amplitude (or an array of them) in decibels, never below floor."""
    return np.maximum(20 * np.log10(np.maximum(np.abs(value), 1e-12)), floor)


def power_db(value, floor=-120.0):
    """Power (mean square) in decibels, never below floor."""
    return np.maximum(10 * np.log10(np.maximum(value, 1e-24)), floor)


def frames(signal, size, hop):
    """A strided (count, size) view of a 1-D signal cut into frames; the tail shorter than a frame is padded."""
    count = max(1, 1 + int(np.ceil((len(signal) - size) / hop)))
    padded = np.zeros((count - 1) * hop + size, dtype=signal.dtype)
    padded[:len(signal)] = signal[:len(padded)]
    strides = (padded.strides[0] * hop, padded.strides[0])
    return np.lib.stride_tricks.as_strided(padded, shape=(count, size), strides=strides, writeable=False)


def impulse_response(sections, limit=1 << 18, quiet=1e-10):
    """The impulse response of a chain of biquads [[b0, b1, b2, a0, a1, a2], ...], run sample by sample until its last
    1024 samples fall under quiet times its peak (or limit samples). scipy is not usable on this machine (Windows
    Application Control blocks its compiled modules), so IIR filters run as exact FFT convolutions with this."""
    response = np.zeros(1024)
    response[0] = 1.0
    for b0, b1, b2, a0, a1, a2 in sections:
        response = _biquad(response, b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0, limit, quiet)
    return response


def _biquad(x, b0, b1, b2, a1, a2, limit, quiet):
    """One biquad over x (direct form I), continued past x's end while it still rings."""
    out, x1, x2, y1, y2, peak = [], 0.0, 0.0, 0.0, 0.0, 1e-30
    source = list(x)
    for n in range(limit):
        x0 = source[n] if n < len(source) else 0.0
        y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        out.append(y0)
        x2, x1, y2, y1 = x1, x0, y1, y0
        peak = max(peak, abs(y0))
        if n >= len(source) and n % 1024 == 0 and max(map(abs, out[-1024:])) < quiet * peak:
            break
    return np.array(out)


def convolve(signal, response, block=1 << 16):
    """signal (frames, or frames x channels) convolved with response by FFT overlap-add, cut to signal's length."""
    flat = signal.ndim == 1
    x = signal[:, None] if flat else signal
    size = 1 << int(np.ceil(np.log2(block + len(response))))
    kernel = np.fft.rfft(response, size)
    out = np.zeros((len(x) + len(response), x.shape[1]))
    for start in range(0, len(x), block):
        chunk = np.fft.rfft(x[start:start + block].astype(np.float64), size, axis=0)
        piece = np.fft.irfft(chunk * kernel[:, None], size, axis=0)
        end = min(len(out), start + size)
        out[start:end] += piece[:end - start]
    out = out[:len(x)]
    return out[:, 0] if flat else out

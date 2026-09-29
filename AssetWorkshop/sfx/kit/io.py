"""Writing sounds: 16-bit WAV with triangular dither (the bundle's source; Unity encodes it to Vorbis itself, so no
sound is encoded twice) and OGG Vorbis through soundfile for listening and sharing."""
import os
import wave

import numpy as np

from kit.core import RATE


def pcm16(x, seed=0):
    """Float samples to int16 with TPDF dither."""
    rng = np.random.default_rng(seed)
    dither = (rng.uniform(-0.5, 0.5, x.shape) + rng.uniform(-0.5, 0.5, x.shape)) / 32768
    return np.clip(np.round((x + dither) * 32767), -32768, 32767).astype("<i2")


def write_wav(path, x, rate=RATE):
    """A 16-bit WAV, mono or stereo."""
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    data = pcm16(np.asarray(x, dtype=float))
    with wave.open(path, "wb") as handle:
        handle.setnchannels(1 if data.ndim == 1 else data.shape[1])
        handle.setsampwidth(2)
        handle.setframerate(rate)
        handle.writeframes(data.tobytes())
    return path


def write_ogg(path, x, rate=RATE, quality=0.6):
    """An OGG Vorbis file (quality 0..1, about the game's own bit rates at 0.6)."""
    import soundfile
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    soundfile.write(path, np.asarray(x, dtype=np.float32), rate, format="OGG", subtype="VORBIS",
                    compression_level=1.0 - quality)
    return path

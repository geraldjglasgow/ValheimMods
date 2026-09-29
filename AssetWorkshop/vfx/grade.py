"""The game's camera, approximately, for preview frames: Unity renders linear HDR (VfxPreview), this adds what the
game's post-processing does to it (Misc/posteffect_presets/ingame.asset in the reference export: bloom intensity 0.3,
threshold 0.7 in gamma, soft knee 0.7; ACES tonemapping; post exposure +1 EV; contrast 1.2; temperature -8) and
writes sRGB. Plain numpy.

    rgb8 = grade.frame(half_bytes, width, height)
"""
import numpy as np

BLOOM_INTENSITY, BLOOM_THRESHOLD, BLOOM_KNEE = 0.3, 0.7 ** 2.2, 0.7 * 0.7 ** 2.2
EXPOSURE, CONTRAST = 2.0, 1.2
WHITE_BALANCE = np.array([0.97, 1.0, 1.04])   # temperature -8: a touch cooler


def read(raw, width, height):
    """Unity's RGBAHalf bytes (bottom row first) -> float32 (height, width, 3), top row first."""
    image = np.frombuffer(raw, dtype=np.float16).reshape(height, width, 4)[::-1, :, :3]
    return np.nan_to_num(image.astype(np.float32), nan=0.0, posinf=64.0)


def bloom(image):
    """Soft-knee threshold, a blur pyramid of five levels, added back at the game's intensity."""
    brightness = image.max(axis=2, keepdims=True)
    soft = np.clip(brightness - BLOOM_THRESHOLD + BLOOM_KNEE, 0, 2 * BLOOM_KNEE) ** 2 / (4 * BLOOM_KNEE + 1e-5)
    bright = image * np.maximum(soft, brightness - BLOOM_THRESHOLD) / np.maximum(brightness, 1e-5)
    total, level = np.zeros_like(image), bright
    for _ in range(5):
        level = _half(_blur3(level))
        total += _grow(level, image.shape)
    return image + BLOOM_INTENSITY * total


def _blur3(image):
    padded = np.pad(image, ((1, 1), (1, 1), (0, 0)), mode="edge")
    rows = 0.25 * padded[:-2] + 0.5 * padded[1:-1] + 0.25 * padded[2:]
    return 0.25 * rows[:, :-2] + 0.5 * rows[:, 1:-1] + 0.25 * rows[:, 2:]


def _half(image):
    h, w = image.shape[0] // 2 * 2, image.shape[1] // 2 * 2
    image = image[:h, :w]
    return 0.25 * (image[0::2, 0::2] + image[1::2, 0::2] + image[0::2, 1::2] + image[1::2, 1::2])


def _grow(image, shape):
    """Bilinear upsampling to the full frame (nearest-neighbour would leave blocks round bright spots)."""
    def axis(size, target):
        pos = np.clip((np.arange(target) + 0.5) * size / target - 0.5, 0, size - 1)
        low = np.floor(pos).astype(int)
        return low, np.minimum(low + 1, size - 1), (pos - low)
    y0, y1, fy = axis(image.shape[0], shape[0])
    x0, x1, fx = axis(image.shape[1], shape[1])
    rows = image[y0] * (1 - fy)[:, None, None] + image[y1] * fy[:, None, None]
    return rows[:, x0] * (1 - fx)[None, :, None] + rows[:, x1] * fx[None, :, None]


def tonemap(image):
    """Exposure, contrast about middle grey, ACES (Narkowicz' fit), sRGB."""
    x = image * EXPOSURE * WHITE_BALANCE
    x = 0.18 * np.power(np.maximum(x, 0) / 0.18, CONTRAST)
    x = np.clip((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0, 1)
    return np.where(x <= 0.0031308, 12.92 * x, 1.055 * np.power(x, 1 / 2.4) - 0.055)


def frame(raw, width, height):
    """One preview frame graded like the game's camera, as uint8 RGB."""
    return (np.clip(tonemap(bloom(read(raw, width, height))), 0, 1) * 255 + 0.5).astype(np.uint8)

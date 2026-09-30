"""The building blocks of the particle texture generators (textures.py): a pixel grid, tileable value noise and fractal
noise, a separable gaussian blur, smoothstep, white-over-alpha and PNG output. numpy and Pillow only.
"""
import numpy as np
from PIL import Image


def grid(px):
    """(x, y) in -1..1 over a px square, pixel centres, y up."""
    c = (np.arange(px) + 0.5) / px * 2 - 1
    return np.meshgrid(c, -c)


def value_noise(px, cells, rng):
    """Smooth tileable value noise, 0..1, with `cells` lattice cells across."""
    lattice = rng.random((cells, cells))
    x = np.arange(px) / px * cells
    i0 = np.floor(x).astype(int) % cells
    i1 = (i0 + 1) % cells
    f = x - np.floor(x)
    f = f * f * (3 - 2 * f)
    rows = lattice[i0][:, i0] * (1 - f)[None, :] + lattice[i0][:, i1] * f[None, :]
    rows1 = lattice[i1][:, i0] * (1 - f)[None, :] + lattice[i1][:, i1] * f[None, :]
    return rows * (1 - f)[:, None] + rows1 * f[:, None]


def fbm(px, rng, cells=4, octaves=4, persistence=0.5):
    """Fractal value noise, 0..1."""
    total, weight, amplitude = np.zeros((px, px)), 0.0, 1.0
    for octave in range(octaves):
        total += amplitude * value_noise(px, cells * 2 ** octave, rng)
        weight += amplitude
        amplitude *= persistence
    return total / weight


def blur(image, sigma):
    """Separable gaussian blur (edges clamped), for 2D or 3D arrays."""
    if sigma <= 0:
        return image
    radius = max(1, int(3 * sigma))
    kernel = np.exp(-0.5 * (np.arange(-radius, radius + 1) / sigma) ** 2)
    kernel /= kernel.sum()
    out = image
    for axis in (0, 1):
        padded = np.pad(out, [(radius, radius) if a == axis else (0, 0) for a in range(out.ndim)], mode="edge")
        out = sum(k * np.take(padded, range(i, i + out.shape[axis]), axis=axis) for i, k in enumerate(kernel))
    return out


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def white(alpha, grey=None):
    """RGBA from an alpha mask: white RGB (or a grey level array), the game's way for tinted particles."""
    rgb = np.ones(alpha.shape + (3,)) if grey is None else np.repeat(grey[..., None], 3, axis=2)
    return np.concatenate([rgb, np.clip(alpha, 0, 1)[..., None]], axis=2)


def save(rgba, path):
    """Writes an RGBA float array as an 8-bit PNG."""
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path)
    return path

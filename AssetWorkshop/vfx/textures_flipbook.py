"""Flipbook textures in the game's layouts: boiling fireball frames for the gradient-mapped shader
(flameball_flipbook.png) and lit smoke frames packed normal, depth and alpha (slowwispysmokeloop.png). 8 x 8 frames,
row by row from the top left. Re-exported by textures.py.
"""
import math

import numpy as np

from textures_base import blur, fbm, grid, smoothstep
from textures_soft import puff


def flame_flipbook(frames=64, columns=8, cell=32, seed=0, lobes=6, size=0.92):
    """Boiling fireball frames (flameball_flipbook.png): a lobed blob about two thirds of its cell, bright with dark
    creases, turning over through a seamless loop. Greyscale in RGB, alpha 1: the game's gradient-mapped shader
    turns the grey into colour (bright = its first custom colour, dark = its second) and uses it as alpha."""
    rng = np.random.default_rng(seed)
    phases, speeds = rng.uniform(0, 2 * math.pi, lobes), rng.integers(1, 3, lobes)
    detail = [fbm(cell * 4, rng, cells=10, octaves=3), fbm(cell * 4, rng, cells=4, octaves=2)]
    sheet = np.zeros((cell * (frames // columns), cell * columns))
    for i in range(frames):
        t = 2 * math.pi * i / frames
        row, column = divmod(i, columns)
        sheet[row * cell:(row + 1) * cell, column * cell:(column + 1) * cell] = _flame_frame(cell, t, phases, speeds, detail, size)
    return np.concatenate([np.repeat(sheet[..., None], 3, axis=2), np.ones(sheet.shape + (1,))], axis=2)


def _flame_frame(cell, t, phases, speeds, detail, size):
    """One frame: a lobed, ragged-edged blob whose inner wisps and dark creases turn with t (one turn per loop)."""
    x, y = grid(cell)
    angle, r = np.arctan2(y, x), np.hypot(x, y)
    ragged = _turned(detail[1], x, y, t, 0.35)
    lobes = sum(0.18 / k * np.cos(k * angle + p + s * t) for k, (p, s) in enumerate(zip(phases, speeds), 2))
    edge = size * (0.78 + lobes + 0.3 * (ragged - 0.5))
    body = smoothstep(edge, edge * 0.55, r)
    inner = _turned(detail[0], x, y, t, 0.5)
    creases = 1 - smoothstep(0.04, 0.2, np.abs(inner * 2 - 1))
    return np.clip(body * (0.6 + 1.0 * inner) * (1 - 0.5 * creases), 0, 1)


def _turned(noise, x, y, turn, zoom):
    """A tileable noise image read at (x, y) turned by `turn` radians and scaled by `zoom`, wrapping at its edges."""
    size = noise.shape[0]
    u = (x * math.cos(turn) - y * math.sin(turn)) * zoom
    v = (x * math.sin(turn) + y * math.cos(turn)) * zoom
    return noise[((v * 0.5 + 0.5) * size).astype(int) % size, ((u * 0.5 + 0.5) * size).astype(int) % size]


def smoke_flipbook(frames=64, columns=8, cell=64, seed=0):
    """Lit smoke frames packed the way the game's lit smoke flipbooks are (slowwispysmokeloop.png): normal X and Y in
    red and green, depth in blue, coverage in alpha; a puff billowing out and thinning over the frames. For the
    game's lit particle shader, which reads the normal from red and green."""
    rng = np.random.default_rng(seed)
    base = [puff(cell, seed=seed * 97 + k, lumps=9)[..., 3] for k in range(4)]
    sheet = np.zeros((cell * (frames // columns), cell * columns, 4))
    for i in range(frames):
        f = i / (frames - 1)
        k = min(3, int(f * 3))
        mix = f * 3 - k
        height = base[k] * (1 - mix) + base[min(3, k + 1)] * mix
        height = blur(height, 1 + 2 * f) * (1 - 0.6 * f ** 2) + 0.05 * rng.random((cell, cell)) * height
        row, column = divmod(i, columns)
        sheet[row * cell:(row + 1) * cell, column * cell:(column + 1) * cell] = _normal_pack(height)
    return sheet


def _normal_pack(height):
    """Normal (red, green), depth (blue) and alpha of a height field."""
    gy, gx = np.gradient(height * 6)
    n = np.stack([-gx, gy, np.ones_like(height)], axis=2)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.concatenate([n[..., :2] * 0.5 + 0.5, height[..., None], np.clip(height, 0, 1)[..., None]], axis=2)


def sheet_of(images, columns):
    """Frames (same size) into a flipbook, row by row from the top left, as Unity's texture sheet animation reads."""
    rows = [np.concatenate(images[i:i + columns], axis=1) for i in range(0, len(images), columns)]
    return np.concatenate(rows, axis=0)

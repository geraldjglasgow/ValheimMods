"""Soft particle shapes in the game's style: the gaussian dot, the swirled glow, the clumpy puff and the mottled cloud
(point.png, glow.png, wildfire01.png, dirt.png and dust01_bw.png in the game). Re-exported by textures.py.
"""
import math

import numpy as np

from textures_base import blur, fbm, grid, smoothstep, value_noise, white


def point(px=128, sigma=0.36):
    """A soft round dot (the game's point.png: gaussian, sigma 0.36 of the radius, nearly 1 in the middle)."""
    x, y = grid(px)
    r = np.hypot(x, y)
    return white(np.exp(-0.5 * (r / sigma) ** 2) * smoothstep(1.0, 0.9, r))


def glow(px=128, seed=0, sigma=0.42, peak=0.85, swirl=0.4):
    """A soft glow with faint smeared streaks (glow.png): a wide gaussian, peak below 1, streaked by noise stretched
    along a slow spiral."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    r = np.hypot(x, y)
    turn = (np.arctan2(y, x) / (2 * math.pi) + 0.5 + 0.25 * r) % 1.0
    band = value_noise(px, 12, rng)
    streaks = band[(turn * (px - 1)).astype(int), (np.clip(r, 0, 0.999) * 3).astype(int)]
    alpha = peak * np.exp(-0.5 * (r / sigma) ** 2) * (1 - swirl + swirl * streaks)
    return white(blur(alpha, px / 96) * smoothstep(1.0, 0.85, r))


def puff(px=128, seed=0, lumps=16, spread=0.48, size=(0.05, 0.16), density=0.85):
    """A clumpy smoke or dust puff (wildfire01.png): separate soft lumps of mixed sizes with bright cores and thin gaps
    between them, inside a soft circle. With a lit shader for dust and smoke, additive for magic mist."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    top, total = np.zeros((px, px)), np.zeros((px, px))
    for _ in range(lumps):
        angle, dist = rng.random() * 2 * math.pi, spread * math.sqrt(rng.random())
        radius = rng.uniform(*size)
        cx, cy = dist * math.cos(angle), dist * math.sin(angle)
        lump = np.exp(-0.5 * ((x - cx) ** 2 + (y - cy) ** 2) / radius ** 2) * rng.uniform(0.55, 1.0)
        top, total = np.maximum(top, lump), total + lump
    haze = 0.2 * np.exp(-0.5 * (np.hypot(x, y) / 0.4) ** 2)
    alpha = density * (0.75 * top + 0.25 * np.tanh(total)) + haze
    return white(np.clip(alpha, 0, 1) * smoothstep(1.0, 0.8, np.hypot(x, y)))


def cloud(px=256, seed=0, radius=0.8, contrast=0.8, creases=0.45, level=0.45, grey=None):
    """A mottled round cloud with dark creases (dirt.png, dust01_bw.png): fractal noise with ridges, in a soft
    circle. grey (0..1) makes the RGB that grey instead of white, as dust01_bw does."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    body = fbm(px, rng, cells=4, octaves=5)
    ridges = 1 - np.abs(fbm(px, rng, cells=3, octaves=4) * 2 - 1)
    grain = fbm(px, rng, cells=16, octaves=3)
    alpha = np.clip((body - 0.5) * contrast + 0.5 + 0.25 * (grain - 0.5), 0, 1) * (1 - creases * blur(ridges, px / 256) ** 3)
    alpha *= smoothstep(radius + 0.2, radius - 0.3, np.hypot(x, y) + 0.15 * (body - 0.5))
    tone = None if grey is None else np.full((px, px), grey) * (0.85 + 0.3 * body)
    return white(2 * level * alpha, tone)

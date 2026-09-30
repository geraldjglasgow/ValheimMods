"""Particle textures in the game's style, made with numpy and Pillow: the shapes the game's own effects draw with
(codex/vfx/textures.md). Every generator returns an RGBA float array (0..1, rows top to bottom) and takes a seed, so
a texture is the same every build.

    import textures
    textures.save(textures.point(128), "out/my_point.png")
    textures.save(textures.flame_flipbook(seed=4), "out/my_flames.png")

The game's rules, which these follow: the shape is in the alpha channel over white RGB (the particle system's colour
tints it); sizes are small (8 to 128 px, flipbooks 8 x 8 frames of 32 or 64 px); edges are soft and gaussian, bodies
lumpy and mottled; the pixel pieces (8 px chunks, flipbooks) are drawn point-filtered so their pixels show.

The building blocks are in textures_base.py, the soft shapes in textures_soft.py, the flipbooks in textures_flipbook.py;
every generator is importable from here and listed in GENERATORS.
"""
import math

import numpy as np
from PIL import Image, ImageDraw

from textures_base import blur, fbm, grid, save, smoothstep, value_noise, white  # noqa: F401  (the public toolbox)
from textures_flipbook import flame_flipbook, sheet_of, smoke_flipbook  # noqa: F401
from textures_soft import cloud, glow, point, puff  # noqa: F401


# ---------------------------------------------------------------- sparks, rings, splats

def star(px=64, long=0.95, short=0.5, width=0.022, glow_sigma=0.035):
    """A thin four-point glint (starspark.png): a vertical ray longer than the horizontal one, a tiny bright core."""
    x, y = grid(px)
    vertical = np.exp(-0.5 * (x / width) ** 2) * np.clip(1 - np.abs(y) / long, 0, 1) ** 1.6
    horizontal = np.exp(-0.5 * (y / width) ** 2) * np.clip(1 - np.abs(x) / short, 0, 1) ** 1.6
    core = 0.6 * np.exp(-0.5 * (np.hypot(x, y) / glow_sigma) ** 2)
    return white(np.clip(0.75 * (vertical + horizontal) + core, 0, 1))


def spark_bolt(px=64, seed=0, sigma=0.42, bolt=0.45, segments=6):
    """A soft glow with a small bright zig-zag in the middle (spark 1.png): the game's cinder and ember sprite."""
    rng = np.random.default_rng(seed)
    base = np.exp(-0.5 * (np.hypot(*grid(px)) / sigma) ** 2) * 0.95
    canvas = Image.new("L", (px, px), 0)
    tilt = rng.uniform(0, math.pi)
    points = []
    for i in range(segments + 1):
        along, across = (i / segments - 0.5) * bolt * px, rng.uniform(-0.1, 0.1) * px
        points.append((px / 2 + along * math.cos(tilt) - across * math.sin(tilt), px / 2 + along * math.sin(tilt) + across * math.cos(tilt)))
    ImageDraw.Draw(canvas).line(points, fill=255, width=max(1, px // 32))
    line = blur(np.asarray(canvas, dtype=float) / 255, px / 160)
    return white(np.clip(base + line, 0, 1))


def streak(width=32, height=8, tail=0.85):
    """A falling drop or rain streak (drop_pixel.png): a horizontal bar bright at the head, fading along the tail.
    Stretched particles draw it along their velocity."""
    x = np.linspace(0, 1, width)
    along = np.clip(1 - x / tail, 0, 1) ** 1.3 * smoothstep(0.0, 0.08, x)
    across = np.exp(-0.5 * ((np.arange(height) + 0.5 - height / 2) / (height / 10)) ** 2)
    return white(across[:, None] * along[None, :])


def ring(px=128, inner=0.5, outer=0.95, edge=0.04):
    """A shockwave ring (shockwave.png): nothing in the middle, rising to its brightest just inside the rim."""
    r = np.hypot(*grid(px))
    return white(smoothstep(inner, outer, r) * smoothstep(1.0, 1.0 - edge, r))


def wobbly_ring(px=64, seed=0, radius=0.86, width=0.075, wobble=0.07):
    """A thin, slightly wobbly ring (water_ring.png): water and tar ripples, drawn flat on the surface."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    angle = np.arctan2(y, x)
    offset = sum(rng.uniform(-1, 1) * np.sin(k * angle + rng.uniform(0, 6.3)) for k in (3, 5, 7)) * wobble / 2
    thickness = width * (0.7 + 0.6 * value_noise(px, 5, rng)[(np.clip((angle / math.pi + 1) / 2, 0, 0.999) * px).astype(int), 0])
    distance = np.abs(np.hypot(x, y) - radius - offset)
    return white(smoothstep(thickness, thickness * 0.4, distance))


def speckle(px=64, seed=0, count=520, radius=0.42, cell=2):
    """A splat of small square speckles, dense in the middle (brains.png): blood splats and gore bits."""
    rng = np.random.default_rng(seed)
    alpha = np.zeros((px, px))
    for _ in range(count):
        r, a = radius * abs(rng.normal(0, 0.5)), rng.random() * 2 * math.pi
        cx, cy = int(px / 2 + r * px / 2 * math.cos(a)), int(px / 2 + r * px / 2 * math.sin(a))
        size = rng.integers(1, cell + 1)
        alpha[max(0, cy):cy + size, max(0, cx):cx + size] = np.maximum(alpha[max(0, cy):cy + size, max(0, cx):cx + size],
                                                                        rng.uniform(0.6, 1.0))
    return white(alpha)


def veins(px=64, seed=0, radius=0.72):
    """A splash of branching liquid threads (slime_splash.png): two layers of ridged noise kept to their thin crests,
    densest in a ring round an emptier middle."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    threads = sum(smoothstep(0.88, 0.97, 1 - np.abs(fbm(px, rng, cells=c, octaves=2) * 2 - 1)) for c in (6, 10))
    threads *= 0.45 + 0.55 * fbm(px, rng, cells=16, octaves=2)
    r = np.hypot(x, y)
    mask = smoothstep(radius + 0.25, radius, r) * (0.45 + 0.55 * smoothstep(0.1, 0.6, r))
    return white(np.clip(threads * mask, 0, 1))


def chunk(px=8, seed=0, fill=0.45):
    """An 8 px pixel chunk (leaf_low.png): an irregular blob of a few grey levels, drawn point-filtered. The game's
    most used particle texture: debris bits, drops and flecks tinted by the system's colour."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    shape = np.hypot(x, y) + rng.uniform(-0.35, 0.35, (px, px))
    alpha = (shape < fill + 0.35).astype(float)
    grey = np.clip(0.75 + rng.uniform(-0.2, 0.2, (px, px)), 0, 1)
    return white(alpha, grey)


def shard(px=32, seed=0, length=0.9, width=0.28):
    """A pointed crystal or ice shard sprite: a long diamond with a lit and a shaded half, white-grey over alpha."""
    rng = np.random.default_rng(seed)
    x, y = grid(px)
    tilt = rng.uniform(-0.2, 0.2)
    u, v = x * math.cos(tilt) - y * math.sin(tilt), x * math.sin(tilt) + y * math.cos(tilt)
    inside = np.abs(u) / width + np.abs(v) / length < 1
    grey = np.where(u < 0, 0.95, 0.7) - 0.15 * np.abs(v)
    return white(blur(inside.astype(float), 0.5), grey)


GENERATORS = {"point": point, "glow": glow, "puff": puff, "cloud": cloud, "star": star, "spark_bolt": spark_bolt,
              "streak": streak, "ring": ring, "wobbly_ring": wobbly_ring, "speckle": speckle, "veins": veins,
              "chunk": chunk, "shard": shard, "flame_flipbook": flame_flipbook, "smoke_flipbook": smoke_flipbook}

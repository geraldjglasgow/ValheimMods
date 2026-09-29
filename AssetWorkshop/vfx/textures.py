"""Particle textures in the game's style, made with numpy and Pillow: the shapes the game's own effects draw with
(codex/vfx/textures.md). Every generator returns an RGBA float array (0..1, rows top to bottom) and takes a seed, so
a texture is the same every build.

    import textures
    textures.save(textures.point(128), "out/my_point.png")
    textures.save(textures.flame_flipbook(seed=4), "out/my_flames.png")

The game's rules, which these follow: the shape is in the alpha channel over white RGB (the particle system's colour
tints it); sizes are small (8 to 128 px, flipbooks 8 x 8 frames of 32 or 64 px); edges are soft and gaussian, bodies
lumpy and mottled; the pixel pieces (8 px chunks, flipbooks) are drawn point-filtered so their pixels show.
"""
import math

import numpy as np
from PIL import Image, ImageDraw


# ---------------------------------------------------------------- building blocks

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


# ---------------------------------------------------------------- soft shapes

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


# ---------------------------------------------------------------- flipbooks

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


GENERATORS = {"point": point, "glow": glow, "puff": puff, "cloud": cloud, "star": star, "spark_bolt": spark_bolt,
              "streak": streak, "ring": ring, "wobbly_ring": wobbly_ring, "speckle": speckle, "veins": veins,
              "chunk": chunk, "shard": shard, "flame_flipbook": flame_flipbook, "smoke_flipbook": smoke_flipbook}

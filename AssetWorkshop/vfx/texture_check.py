"""Puts each generated particle texture beside the game texture it imitates and compares their numbers (coverage,
soft edge, radial alpha profile), so the generator can be tuned by eye and by measurement. Writes
vfx/out/texture_check.png (gitignored); the game's pixels never leave the out folder.

    python vfx/texture_check.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "codex", "measure"))

import textures  # noqa: E402
import game  # noqa: E402
import vfx_textures  # noqa: E402

PAIRS = [  # (generator call, game texture it imitates)
    (lambda: textures.point(128), "Effects/textures/point.png"),
    (lambda: textures.glow(128, seed=2), "Effects/textures/glow.png"),
    (lambda: textures.puff(128, seed=3), "Effects/textures/wildfire01.png"),
    (lambda: textures.cloud(256, seed=5), "Effects/textures/dirt.png"),
    (lambda: textures.cloud(256, seed=6, contrast=0.5, grey=0.4), "Effects/textures/dust01_bw.png"),
    (lambda: textures.star(64), "Effects/textures/starspark.png"),
    (lambda: textures.spark_bolt(64, seed=1), "Effects/textures/spark 1.png"),
    (lambda: textures.ring(128), "Effects/textures/shockwave.png"),
    (lambda: textures.wobbly_ring(64, seed=2), "Effects/textures/water_ring.png"),
    (lambda: textures.speckle(64, seed=4), "Effects/textures/brains.png"),
    (lambda: textures.veins(64, seed=7), "Effects/textures/slime_splash.png"),
    (lambda: textures.chunk(8, seed=3), "Effects/textures/leaf_low.png"),
    (lambda: textures.streak(32, 8), "Effects/textures/drop_pixel.png"),
    (lambda: textures.flame_flipbook(seed=4), "Effects/textures/flameball_flipbook.png"),
]
TILE = 192


def profile(alpha, bins=10):
    h, w = alpha.shape
    y, x = np.mgrid[:h, :w]
    r = np.hypot(x - (w - 1) / 2, y - (h - 1) / 2) / (w / 2)
    return [round(float(alpha[(r >= t) & (r < t + 1 / bins)].mean()), 2) for t in np.arange(0, 1, 1 / bins)]


def panel(rgba):
    """Colour times alpha over black, enlarged point-filtered to the tile."""
    image = np.clip(rgba[..., :3] * rgba[..., 3:4], 0, 1)
    return Image.fromarray((image * 255).astype(np.uint8)).resize((TILE, TILE), Image.NEAREST)


def main():
    out = os.path.join(HERE, "out")
    os.makedirs(out, exist_ok=True)
    sheet = Image.new("RGB", (TILE * 4 + 12, (TILE + 4) * ((len(PAIRS) + 1) // 2)), (40, 40, 40))
    for i, (make, reference) in enumerate(PAIRS):
        ours = make()
        theirs = vfx_textures.load_rgba(reference)
        x, y = (i % 2) * (TILE * 2 + 12), (i // 2) * (TILE + 4)
        sheet.paste(panel(ours), (x, y))
        sheet.paste(panel(theirs), (x + TILE, y))
        a, b = ours[..., 3] if ours[..., 3].min() < 1 else ours[..., 0], theirs[..., 3] if theirs[..., 3].min() < 1 else theirs[..., 0]
        print(f"{os.path.basename(reference):28s} ours {profile(a)}\n{'':28s} game {profile(b)}")
    path = os.path.join(out, "texture_check.png")
    sheet.save(path)
    print(path)


if __name__ == "__main__":
    main()

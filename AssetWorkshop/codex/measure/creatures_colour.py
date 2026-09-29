"""Colour numbers of a creature's main texture, and contact sheets of the textures for looking at them.

texture_colour(path) gives the texture's mean colour, its dark, middle and light tones (the mean colour of the pixels
around the 10th, 50th and 90th luminance percentile), mean saturation and value contrast, sRGB 0-255, leaving out
cut-out pixels (alpha under 0.5). creatures.py stores it per creature.

    python codex/measure/creatures_colour.py <sheet name> <creature> ...

writes codex/out/creatures/<sheet name>.png: each creature's main albedo, blown up with nearest-neighbour filtering to
show its pixels, labelled with its size (reads data/creatures.json).
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

import game

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "out", "creatures"))
DATA = os.path.normpath(os.path.join(HERE, "..", "data", "creatures.json"))


def texture_colour(path):
    """{mean, dark, middle, light, saturation, contrast} of an exported texture, or None when it cannot be read."""
    try:
        with Image.open(os.path.join(game.ROOT, path)) as image:
            pixels = np.asarray(image.convert("RGBA"), dtype=np.float64).reshape(-1, 4)
    except (OSError, ValueError):
        return None
    kept = pixels[pixels[:, 3] >= 128][:, :3] if (pixels[:, 3] >= 128).any() else pixels[:, :3]
    luminance = kept @ np.array([0.2126, 0.7152, 0.0722])
    order = kept[np.argsort(luminance)]
    tone = lambda q: [int(round(v)) for v in order[_band(len(order), q)].mean(axis=0)]
    top, bottom = kept.max(axis=1), kept.min(axis=1)
    saturation = np.where(top > 0, (top - bottom) / np.maximum(top, 1), 0).mean()
    ranked = np.sort(luminance)
    return {"mean": [int(round(v)) for v in kept.mean(axis=0)], "dark": tone(0.1), "middle": tone(0.5),
            "light": tone(0.9), "saturation": round(float(saturation), 3),
            "contrast": round(float(ranked[int(0.9 * (len(ranked) - 1))] - ranked[int(0.1 * (len(ranked) - 1))]), 1)}


def _band(count, quantile):
    """The slice of sorted pixels within two percent either side of a quantile."""
    middle, half = int(quantile * (count - 1)), max(int(0.02 * count), 1)
    return slice(max(middle - half, 0), min(middle + half + 1, count))


def main_texture(sample):
    """The export path of a creature's main albedo: its first material's _MainTex."""
    for material in sample["materials"]:
        path = game.material(material["path"])["textures"].get("_MainTex")
        if path:
            return path
    return None


def sheet(names, out, tile=256):
    """The named creatures' main albedos side by side, point filtered, labelled with name and size."""
    with open(DATA, encoding="utf-8") as handle:
        samples = {s["name"]: s for c in json.load(handle)["categories"].values() for s in c["samples"]}
    paths = [(n, main_texture(samples[n])) for n in names if n in samples]
    columns = min(len(paths), 6)
    rows = (len(paths) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * tile, rows * (tile + 16)), (60, 60, 60))
    draw = ImageDraw.Draw(canvas)
    for i, (name, path) in enumerate(paths):
        x, y = (i % columns) * tile, (i // columns) * (tile + 16)
        if path:
            with Image.open(os.path.join(game.ROOT, path)) as image:
                size = image.size
                canvas.paste(image.convert("RGB").resize((tile, tile), Image.NEAREST), (x, y))
            draw.text((x + 4, y + tile + 2), f"{name} {size[0]}x{size[1]}", fill=(255, 255, 255))
    os.makedirs(OUT, exist_ok=True)
    canvas.save(os.path.join(OUT, out + ".png"))


if __name__ == "__main__":
    sheet(sys.argv[2:], sys.argv[1])

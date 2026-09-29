"""Renders the game's effects through the workshop's effect preview (vfx/build.py --reference: staged under the
gitignored Assets/Reference, drawn in the preview copies of the game's shaders, graded like the game's camera) and
lays them out for looking at: codex/out/vfx/effects_NN.png, one row per effect, frames across its first seconds.

    python codex/measure/vfx_lineup.py                     the categories' reference effects (Unity, a few minutes)
    python codex/measure/vfx_lineup.py --compose           only lay out what vfx/out/reference already holds
    python codex/measure/vfx_lineup.py Effects/vfx_HitSparks.prefab ...
"""
import argparse
import json
import os
import subprocess
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, "..", ".."))
RENDERS = os.path.join(WORKSHOP, "vfx", "out", "reference")
OUT = os.path.join(WORKSHOP, "codex", "out", "vfx")
ROWS, FRAMES, TILE = 6, 5, (320, 180)


def references():
    """Each category's reference effects from data/vfx.json (prefab paths in the export)."""
    with open(os.path.join(WORKSHOP, "codex", "data", "vfx.json"), encoding="utf-8") as handle:
        data = json.load(handle)
    return [r for c in data["categories"].values() for r in c["references"][:2] if "#" not in r]


def render(paths, seconds=2.5):
    subprocess.run([sys.executable, os.path.join(WORKSHOP, "vfx", "build.py"), "--reference", *paths, "--preview",
                    "--seconds", str(seconds), "--lift", "0.3", "--distance", "4", "--wait"], check=True)


def row(name):
    """FRAMES frames of one rendered effect, spread over its preview, labelled."""
    frames = sorted(f for f in os.listdir(os.path.join(RENDERS, name, "frames")) if f.endswith(".png"))
    picks = [frames[int(i * (len(frames) - 1) / (FRAMES - 1))] for i in range(FRAMES)]
    strip = Image.new("RGB", (TILE[0] * FRAMES, TILE[1]))
    for i, frame in enumerate(picks):
        tile = Image.open(os.path.join(RENDERS, name, "frames", frame)).resize(TILE)
        ImageDraw.Draw(tile).text((5, 4), f"{name}  {frame[6:10]}", fill=(235, 235, 235))
        strip.paste(tile, (i * TILE[0], 0))
    return strip


def compose():
    names = sorted(n for n in os.listdir(RENDERS) if os.path.isdir(os.path.join(RENDERS, n, "frames")))
    os.makedirs(OUT, exist_ok=True)
    for start in range(0, len(names), ROWS):
        rows = [row(n) for n in names[start:start + ROWS]]
        sheet = Image.new("RGB", (rows[0].width, TILE[1] * len(rows)))
        for i, strip in enumerate(rows):
            sheet.paste(strip, (0, i * TILE[1]))
        path = os.path.join(OUT, f"effects_{start // ROWS + 1:02d}.png")
        sheet.save(path)
        print(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("prefabs", nargs="*")
    parser.add_argument("--compose", action="store_true")
    args = parser.parse_args()
    if not args.compose:
        render(args.prefabs or references())
    compose()


if __name__ == "__main__":
    main()

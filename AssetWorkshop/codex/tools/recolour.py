"""Recolours a built asset's baked albedo per paint region and renders the variants side by side.

    python codex/tools/recolour.py assets/<name> --variant <v> --region primary=hue:+0.1,sat:0.8,val:1.1
        --region trim=tint:#8a6d3b
    python codex/tools/recolour.py assets/<name>              every variant in assets/<name>/variants.json

Writes out/<name>_albedo_<variant>.png per variant, then out/variants.png: the asset as built and each variant on the
model in Blender, from the front three-quarters above and from behind below (--no-sheet skips it). The operations are
recolour_ops' (hue, sat, val, tint, keep). Only the texels of the named regions change (the regions mask,
<name>_regions.png, which the build bakes when materials name regions); the painted value structure (baked light,
grime, occlusion) stays. build.ps1 runs this after every build of an asset that has a variants.json:

    {"variants": {"red": {"primary": "hue:-0.04,sat:1.2", "metal": "tint:#6b5a48"},
                  "blue": {"primary": "hue:0.55"}}}
"""
import argparse
import json
import os
import re
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import recolour_ops  # noqa: E402
import style_measure  # noqa: E402

RENDER = os.path.join(HERE, "variants_render.py")
NAME = re.compile(r"^[A-Za-z0-9_-]+$")


def parse(argv=None):
    parser = argparse.ArgumentParser(prog="recolour.py", description=__doc__.splitlines()[0])
    parser.add_argument("asset", help="the asset folder (assets/<name>), built with paint regions")
    parser.add_argument("--variant", help="one variant's name (with --region); default: every one in variants.json")
    parser.add_argument("--region", action="append", default=[], help="region=operations, e.g. trim=tint:#8a6d3b")
    parser.add_argument("--no-sheet", action="store_true", help="write the textures only, no Blender render")
    parser.add_argument("--blender", help="blender.exe (default: WORKSHOP_BLENDER or tools)")
    return parser.parse_args(argv)


def variants(folder, args):
    """{variant: {region: operations text}} from the command line, or from the asset's variants.json."""
    if args.variant:
        pairs = [r.split("=", 1) for r in args.region]
        if not pairs or any(len(p) != 2 for p in pairs):
            raise SystemExit("recolour: --variant needs one or more --region <region>=<operations>")
        return {args.variant: dict(pairs)}
    path = os.path.join(folder, "variants.json")
    if not os.path.exists(path):
        raise SystemExit(f"recolour: no --variant given and no {path}")
    with open(path, encoding="utf-8") as handle:
        data = json.load(handle)
    return data.get("variants", data)


def recolour(folder, chosen):
    """Writes out/<name>_albedo_<variant>.png for every variant; returns [(variant, path)]."""
    name = os.path.basename(os.path.normpath(folder))
    out = os.path.join(folder, "out")
    with open(os.path.join(out, name + ".json"), encoding="utf-8") as handle:
        manifest = json.load(handle)
    colours = {r["name"]: r["colour"] for r in manifest.get("regions", [])}
    if not colours:
        raise SystemExit(f"recolour: {name} has no paint regions (mark materials with regions.mark and rebuild)")
    albedo = _rgb(os.path.join(out, manifest["albedo"]))
    ids = _rgb(os.path.join(out, manifest["regionMask"]))
    written = []
    for variant, regions in chosen.items():
        if not NAME.match(variant):
            raise SystemExit(f"recolour: variant names are letters, digits, - and _ ('{variant}')")
        pixels = albedo.copy()
        for region, spec in regions.items():
            _region(pixels, ids, colours, region, spec)
        path = os.path.join(out, f"{name}_albedo_{variant}.png")
        Image.fromarray(pixels.astype(np.uint8)).save(path)
        written.append((variant, path))
    return written


def _region(pixels, ids, colours, region, spec):
    if region not in colours:
        raise SystemExit(f"recolour: no region '{region}' (the asset has {', '.join(sorted(colours))})")
    try:
        ops = recolour_ops.parse(spec)
    except ValueError as error:
        raise SystemExit(f"recolour: {error}")
    colour = np.array(recolour_ops.hex_bytes(colours[region]))
    hit = np.all(ids == colour, axis=2)
    pixels[hit] = recolour_ops.apply(pixels[hit], ops)


def sheet(folder, written, blender=None):
    """Renders the asset as built and each variant side by side; stacks the two views into out/variants.png."""
    name = os.path.basename(os.path.normpath(folder))
    out = os.path.join(folder, "out")
    albedos = [os.path.join(out, f"{name}_albedo.png")] + [p for _, p in written]
    labels = ["as built"] + [v for v, _ in written]
    views = os.path.join(out, "variants")
    command = [style_measure.blender_path(blender), "--background", "--factory-startup", "--python", RENDER, "--",
               "--blend", os.path.join(out, name + ".blend"), "--name", name, "--albedos", ",".join(albedos),
               "--labels", ",".join(labels), "--out", views]
    result = subprocess.run(command, capture_output=True, text=True)
    if result.returncode != 0 or not os.path.exists(os.path.join(views, "back.png")):
        print(result.stdout[-2000:], result.stderr[-2000:])
        raise SystemExit("recolour: Blender could not render the variants")
    return _stack([os.path.join(views, v + ".png") for v in ("turn", "back")], os.path.join(out, "variants.png"))


def _stack(paths, target):
    images = [Image.open(p).convert("RGB") for p in paths]
    width = max(i.width for i in images)
    canvas = Image.new("RGB", (width, sum(i.height for i in images)), (0, 0, 0))
    y = 0
    for image, caption in zip(images, ("front three-quarters", "from behind (order mirrored)")):
        canvas.paste(image, (0, y))
        ImageDraw.Draw(canvas).text((12, y + 10), caption, fill=(235, 230, 215))
        y += image.height
    canvas.save(target)
    return target


def _rgb(path):
    with Image.open(path) as image:
        return np.asarray(image.convert("RGB"), dtype=np.int32).copy()


if __name__ == "__main__":
    arguments = parse()
    root = os.path.abspath(arguments.asset)
    done = recolour(root, variants(root, arguments))
    for variant_name, file in done:
        print(f"WORKSHOP variant {variant_name}: {file}")
    if not arguments.no_sheet:
        print(f"WORKSHOP variants sheet: {sheet(root, done, arguments.blender)}")

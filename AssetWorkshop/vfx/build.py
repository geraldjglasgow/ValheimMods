"""Builds effects: textures, the spec, the Unity prefab and bundle, and a preview (graded frames, a sheet, an MP4).

    python vfx/build.py <effect> [<effect> ...] [--bundle <name>] [--preview] [--install <folder>] [--wait]
    python vfx/build.py --reference Effects/Fire/fx_Torch_Green.prefab [--preview]    a game effect alone, for looking

An effect is vfx/effects/<effect>/effect.py with EFFECT (vfx/spec.py's words) and, optionally, textures() returning
{name: RGBA array} for textures no generator in vfx/textures.py makes. Output goes to vfx/effects/<effect>/out/
(gitignored): the textures, effect.json, frames/ (graded PNGs), sheet.png and <effect>.mp4; the bundle to
out/bundles/<name>.windows and .linux. Unity runs only while this holds out/unity.lock (--wait waits for it).
"""
import argparse
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import time

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, ".."))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(WORKSHOP, "codex", "measure"))

import check  # noqa: E402
import grade  # noqa: E402
import reference  # noqa: E402
import spec  # noqa: E402
import textures  # noqa: E402

UNITY = os.environ.get("WORKSHOP_UNITY", os.path.join(os.environ.get("USERPROFILE", ""), "tools", "Unity", "Editor",
                                                      "6000.0.75f1", "Editor", "Unity.exe"))
BLENDER = os.environ.get("WORKSHOP_BLENDER", os.path.join(os.environ.get("USERPROFILE", ""), "tools", "blender", "blender.exe"))
LOCK = os.path.join(WORKSHOP, "out", "unity.lock")


def load_effect(name):
    folder = name if os.path.isdir(name) else os.path.join(HERE, "effects", name)
    module_spec = importlib.util.spec_from_file_location("effect", os.path.join(folder, "effect.py"))
    module = importlib.util.module_from_spec(module_spec)
    module_spec.loader.exec_module(module)
    return folder, module


def write_effect(folder, module):
    """Textures and effect.json into <folder>/out; returns the out folder and the expanded spec."""
    out = os.path.join(folder, "out")
    os.makedirs(out, exist_ok=True)
    made = module.textures() if hasattr(module, "textures") else {}
    for name, t in module.EFFECT.get("textures", {}).items():
        image = made[name] if name in made else textures.GENERATORS[t["make"]](**t.get("args", {}))
        textures.save(image, os.path.join(out, f"{name}.png"))
    expanded = spec.expand(module.EFFECT)
    expanded["preview"]["references"] = [reference.stage(p) for p in expanded["preview"]["references"]]
    with open(os.path.join(out, "effect.json"), "w", encoding="utf-8") as handle:
        json.dump(expanded, handle, indent=1)
    print(check.report(out))
    return out, expanded


def reference_effect(path, seconds, lift=0.0, distance=4.0):
    """A spec with nothing of ours: the game effect alone, for looking at it."""
    name = os.path.splitext(os.path.basename(path))[0]
    out = os.path.join(HERE, "out", "reference", name)
    os.makedirs(out, exist_ok=True)
    expanded = spec.expand({"name": name, "preview": {"seconds": seconds, "references": [reference.stage(path)],
                                                      "reference_offset": [0, 0, 0], "figure": True, "lift": lift,
                                                      "distance": distance, "target_height": 0.8 + lift * 0.6}})
    with open(os.path.join(out, "effect.json"), "w", encoding="utf-8") as handle:
        json.dump(expanded, handle, indent=1)
    return out, expanded


def run_unity(folders, bundle, preview, lights, wait):
    """Unity in batch mode under the workshop's lock; when another session has the project open, waits and tries again
    (with --wait). Prints the log's WORKSHOP lines."""
    log = os.path.join(WORKSHOP, "out", "vfx_unity.log")
    args = [UNITY, "-batchmode", "-projectPath", os.path.join(WORKSHOP, "unity"), "-executeMethod",
            "Workshop.Vfx.VfxBuild.Run", "-workshopVfx", ";".join(folders), "-logFile", log]
    if bundle:
        args += ["-workshopBundle", bundle, "-workshopOut", os.path.join(WORKSHOP, "out", "bundles")]
    if preview:
        args += ["-workshopPreview", "-workshopReferenceLights", lights]
    else:
        args.insert(2, "-nographics")
    for attempt in range(60 if wait else 1):
        _lock(wait)
        try:
            result = subprocess.run(args)
        finally:
            os.rmdir(LOCK)
        text = open(log, encoding="utf-8", errors="replace").read()
        if "AlreadyOpenInAnotherInstance" not in text:
            break
        print("the Unity project is open in another session; waiting")
        time.sleep(30)
    for line in text.splitlines():
        if "WORKSHOP" in line or "error CS" in line:
            print(line)
    if result.returncode != 0:
        raise SystemExit(f"Unity failed ({result.returncode}); log: {log}")


def _lock(wait):
    started = time.time()
    while True:
        try:
            os.mkdir(LOCK)
            return
        except FileExistsError:
            if not wait or time.time() - started > 1800:
                raise SystemExit(f"Unity is in use ({LOCK} exists); try again later or pass --wait")
            time.sleep(20)


def finish_preview(out, name):
    """Grades the raw frames into PNGs, a sheet and an MP4, and deletes the raw frames."""
    frames = os.path.join(out, "frames")
    info = json.load(open(os.path.join(frames, "frames.json")))
    pngs = []
    for i in range(info["frames"]):
        raw_path = os.path.join(frames, f"frame_{i:04d}.half")
        rgb = grade.frame(open(raw_path, "rb").read(), info["width"], info["height"])
        pngs.append(os.path.join(frames, f"frame_{i:04d}.png"))
        Image.fromarray(rgb).save(pngs[-1])
        os.remove(raw_path)
    sheet(pngs, info["fps"], os.path.join(out, "sheet.png"))
    encode(frames, os.path.join(out, f"{name}.mp4"), info["fps"])


def sheet(pngs, fps, path, count=8, columns=4):
    """Evenly spaced frames in a grid, each labelled with its time."""
    picks = [pngs[int(i * (len(pngs) - 1) / (count - 1))] for i in range(count)]
    tiles = [Image.open(p) for p in picks]
    w, h = tiles[0].size
    out = Image.new("RGB", (w * columns, h * ((count + columns - 1) // columns)))
    for i, (tile, name) in enumerate(zip(tiles, picks)):
        ImageDraw.Draw(tile).text((6, 4), f"{int(name[-8:-4]) / fps:.2f} s", fill=(235, 235, 235))
        out.paste(tile, ((i % columns) * w, (i // columns) * h))
    out.save(path)


def encode(frames, mp4, fps):
    subprocess.run([BLENDER, "--background", "--factory-startup", "--python", os.path.join(WORKSHOP, "blender", "encode.py"),
                    "--", frames, mp4, str(fps)], capture_output=True)
    print(mp4 if os.path.exists(mp4) else f"encoding failed: {mp4}")


def install(bundle, folder):
    for suffix in ("windows", "linux"):
        shutil.copy(os.path.join(WORKSHOP, "out", "bundles", f"{bundle}.{suffix}"), folder)
    print(f"installed {bundle} into {folder}")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("effects", nargs="*")
    parser.add_argument("--bundle")
    parser.add_argument("--preview", action="store_true")
    parser.add_argument("--install")
    parser.add_argument("--wait", action="store_true")
    parser.add_argument("--reference", nargs="*", default=[])
    parser.add_argument("--seconds", type=float, default=3.0)
    parser.add_argument("--lift", type=float, default=0.0, help="raise a reference this many metres (a torch's flame)")
    parser.add_argument("--distance", type=float, default=4.0, help="camera distance for a reference")
    args = parser.parse_args()
    built = [write_effect(*load_effect(n)) for n in args.effects] + [reference_effect(p, args.seconds, args.lift, args.distance) for p in args.reference]
    for out, _ in built:
        shutil.rmtree(os.path.join(out, "frames"), ignore_errors=True)   # no frames left over from a longer run
    lights = reference.write_lights([p for _, e in built for p in e["preview"]["references"]])
    run_unity([out for out, _ in built], args.bundle.lower() if args.bundle else None, args.preview, lights, args.wait)
    if args.preview:
        for out, expanded in built:
            finish_preview(out, expanded["name"])
            print(os.path.join(out, "sheet.png"))
    if args.install and args.bundle:
        install(args.bundle.lower(), args.install)


if __name__ == "__main__":
    main()

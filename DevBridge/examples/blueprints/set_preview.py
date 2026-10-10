#!/usr/bin/env python3
"""Preview a closed build from inside, offline: preview.py's shapes and Blender scene, with fires drawn orange, iron
dark, bones pale, item stands green, the blueprint's "marks" as magenta posts, and the ceiling left out with --open.
Made for the film sets (crypt_hall, ship_hold), whose roofs hide everything from preview.py's named views.

  python set_preview.py crypt_hall --open 7.4 --views "top;oblique;aisle@1.6,1.5,-16:0,1.5,8" [--tag open]

--open Y   leave out every piece whose bottom is at or above Y (the ceiling, its logs, hanging fires)
views      ';' between: oblique, top, front, back, east, west, upper, or name@x,y,z:tx,ty,tz (a camera at the first
           point looking at the second, blueprint axes, y up). Writes <out>/<name>-<tag>-<view>.png.
"""
import argparse, json, os, subprocess, tempfile
import preview as pv

FIRE = ("brazier", "torch", "Candle", "fire_pit", "lantern")
COLOURS = {**pv.COLOURS, "mark": (0.95, 0.10, 0.85), "iron": (0.18, 0.18, 0.20), "bone": (0.90, 0.88, 0.78),
           "stand": (0.15, 0.75, 0.30)}
pv.CAT["_mark"] = {"snaps": [], "size": [0.3, 1.2, 0.3], "pivot_above_bottom": 0.0}
_base = pv.kind


def kind(name):
    if name == "_mark": return "mark"
    if any(k in name for k in FIRE): return "fire"
    if name.startswith("iron_"): return "iron"
    if name.startswith(("bone_", "skull_")): return "bone"
    if name.startswith("itemstand"): return "stand"
    return _base(name)


pv.kind = kind


def bottom(p):
    return p[2] - pv.CAT[p[0]]["pivot_above_bottom"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("name")
    ap.add_argument("--open", type=float)
    ap.add_argument("--views", default="top;oblique")
    ap.add_argument("--out", default=os.path.join(pv.HERE, "previews"))
    ap.add_argument("--tag", default="set")
    a = ap.parse_args()
    bp = json.load(open(os.path.join(pv.HERE, a.name + ".json")))
    pieces = [p for p in bp["pieces"] if a.open is None or bottom(p) < a.open]
    names, views, eyes = [], [], []
    for v in a.views.split(";"):
        label, _, spec = v.partition("@")
        names.append(label)
        views.append("cam=" + spec if spec else label)
        if spec:
            eyes.append([float(c) for c in spec.split(":")[0].split(",")])
    pieces += [["_mark", *m, 0] for m in bp.get("marks", {}).values()             # not the ones a camera stands in
               if all(sum((m[k] - e[k]) ** 2 for k in range(3)) > 1.5 for e in eyes)]
    out = os.path.join(os.path.abspath(a.out), f"{a.name}-{a.tag}")
    data = {"groups": pv.mesh(pieces), "cut": None, "colours": COLOURS, "site": bp.get("site", {}),
            "views": views, "out": out}
    path = os.path.join(tempfile.gettempdir(), a.name + "-set-preview.json")
    with open(path, "w") as f:
        json.dump(data, f)
    script = os.path.join(pv.HERE, "preview_blender.py")
    r = subprocess.run([pv.BLENDER, "-b", "--factory-startup", "-P", script, "--", path], capture_output=True, text=True)
    for i, (label, view) in enumerate(zip(names, views)):
        made = f"{out}-{view if not view.startswith('cam=') else f'cam{i}'}.png"
        want = f"{out}-{label}.png"
        if os.path.exists(made):
            os.replace(made, want)
            print("preview", want)
        else:
            print("missing", want, (r.stdout + r.stderr)[-1500:])


if __name__ == "__main__":
    main()

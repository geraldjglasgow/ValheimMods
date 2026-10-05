#!/usr/bin/env python3
"""Render a blueprint offline (no game needed): every piece becomes a simple shape built from its snap points in
pieces.json (slopes, gables, beams, boxes), coloured by kind, then Blender renders it headless.

  python preview.py compound [--out <folder>] [--views oblique,top,gate,canal]

Writes <name>-<view>.png. Shapes are approximations: they show layout, heights, gaps and overlaps, not textures.
"""
import argparse, json, math, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
BLENDER = os.path.expandvars(r"%USERPROFILE%\tools\blender\blender.exe")
CAT = json.load(open(os.path.join(HERE, "pieces.json")))

COLOURS = {"thatch": (0.78, 0.62, 0.30), "stone": (0.52, 0.52, 0.50), "log": (0.30, 0.20, 0.12),
           "wood": (0.55, 0.38, 0.22), "floor": (0.66, 0.50, 0.32), "prop": (0.70, 0.15, 0.12),
           "fire": (0.95, 0.45, 0.10), "banner": (0.05, 0.05, 0.05), "fence": (0.45, 0.33, 0.20)}


def kind(name):
    if name.startswith("wood_roof"): return "thatch"
    if name.startswith("stone"): return "stone"
    if "log" in name: return "log"
    if "floor" in name or "stair" in name: return "floor"
    if "fence" in name: return "fence"
    if name in ("fire_pit", "hearth"): return "fire"
    if "banner" in name: return "banner"
    if name.startswith("piece_") or name.startswith("portal"): return "prop"
    return "wood"


def box(x0, x1, y0, y1, z0, z1):
    v = [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    quads = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return v, quads


def slab(points, t=0.25):
    """A thick quad or triangle through the given points, pushed down by t."""
    n = len(points)
    v = list(points) + [(x, y - t, z) for x, y, z in points]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
    faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    return v, faces


def prism(points, t=0.2):
    """A vertical polygon (gable triangles) given in x, y, made t thick along z."""
    v = [(x, y, t / 2) for x, y in points] + [(x, y, -t / 2) for x, y in points]
    n = len(points)
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    return v, faces


def bar(a, b, w=0.3):
    """A square bar from a to b (beams and logs at an angle)."""
    (ax, ay, az), (bx, by, bz) = a, b
    dx, dy, dz = bx - ax, by - ay, bz - az
    ln = math.sqrt(dx * dx + dy * dy + dz * dz) or 1
    ux, uy, uz = dx / ln, dy / ln, dz / ln
    sx, sy, sz = (-uz, 0, ux) if abs(uy) < 0.99 else (1, 0, 0)          # side, horizontal
    s = math.sqrt(sx * sx + sz * sz) or 1
    sx, sz = sx / s * w / 2, sz / s * w / 2
    tx, ty, tz = uy * sz - uz * sy, uz * sx - ux * sz, ux * sy - uy * sx  # up-ish, across the bar
    t = math.sqrt(tx * tx + ty * ty + tz * tz) or 1
    tx, ty, tz = tx / t * w / 2, ty / t * w / 2, tz / t * w / 2
    v = [(p[0] + i * sx + j * tx, p[1] + j * ty, p[2] + i * sz + j * tz) for p in (a, b) for i in (-1, 1) for j in (-1, 1)]
    return v, [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]


def shape(name):
    c = CAT[name]
    sn, size, pab = c["snaps"], c["size"], c["pivot_above_bottom"]
    if name.startswith("wood_roof_top"):
        h = max(p[1] for p in sn)
        return [slab([(-1, 0, -1), (1, 0, -1), (1, h, 0), (-1, h, 0)]), slab([(-1, h, 0), (1, h, 0), (1, 0, 1), (-1, 0, 1)])]
    if name.startswith("wood_roof") and len(sn) == 4:
        ring = sorted(sn, key=lambda p: math.atan2(p[2], p[0]))
        return [slab([tuple(p) for p in ring])]
    if name.startswith("wood_wall_roof") and "top" not in name:
        pts = [(p[0], p[1]) for p in sn if abs(p[2]) < 0.01]
        return [prism(pts)] if len(pts) == 3 else []
    if len(sn) == 2 and abs(sn[0][1] - sn[1][1]) > 0.1 and abs(sn[0][0] - sn[1][0]) > 0.1:
        return [bar(tuple(sn[0]), tuple(sn[1]), 0.4 if "log" in name else 0.3)]
    if "stair" in name:
        return [slab([(-1, 0, 1), (1, 0, 1), (1, 1, -1), (-1, 1, -1)], 0.3)]
    ext = []
    for ax in range(3):
        vals = [p[ax] for p in sn]
        if sn and max(vals) - min(vals) > 0.05:
            ext.append((min(vals), max(vals)))
        elif ax == 1:
            ext.append((-pab, -pab + size[1]))
        else:
            half = min(size[ax], 0.3 if name.startswith("wood") and size[ax] < 0.7 else size[ax]) / 2
            ext.append((-half, half))
    return [box(ext[0][0], ext[0][1], ext[1][0], ext[1][1], ext[2][0], ext[2][1])]


def mesh(pieces):
    """Vertices and faces per colour, in Blender axes (X = x, Y = z, Z = y)."""
    out, cache = {}, {}
    for n, x, y, z, yaw in pieces:
        if n not in cache:
            cache[n] = shape(n)
        t = math.radians(yaw)
        c, s = math.cos(t), math.sin(t)
        group = out.setdefault(kind(n), {"v": [], "f": []})
        for verts, faces in cache[n]:
            base = len(group["v"])
            group["v"] += [(round(x + px * c + pz * s, 3), round(z - px * s + pz * c, 3), round(y + py, 3))
                           for px, py, pz in verts]
            group["f"] += [[base + i for i in f] for f in faces]
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("name")
    ap.add_argument("--out", default=tempfile.gettempdir())
    ap.add_argument("--views", default="oblique,top",
                    help="oblique, top, front, back, east, west, or cam=x,y,z:tx,ty,tz (blueprint axes, y up); ';' between")
    a = ap.parse_args()
    a.out = os.path.abspath(a.out)
    bp = json.load(open(os.path.join(HERE, a.name + ".json")))
    data = {"groups": mesh(bp["pieces"]), "colours": COLOURS, "site": bp.get("site", {}),
            "views": a.views.split(";") if ";" in a.views or "cam=" in a.views else a.views.split(","), "out": os.path.join(a.out, a.name)}
    path = os.path.join(a.out, a.name + "-preview.json")
    with open(path, "w") as f:
        json.dump(data, f)
    script = os.path.join(HERE, "preview_blender.py")
    r = subprocess.run([BLENDER, "-b", "--factory-startup", "-P", script, "--", path], capture_output=True, text=True)
    print("\n".join(l for l in r.stdout.splitlines() if l.startswith("preview")) or r.stdout[-2000:] + r.stderr[-2000:])


if __name__ == "__main__":
    main()

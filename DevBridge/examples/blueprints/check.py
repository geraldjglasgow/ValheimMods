#!/usr/bin/env python3
"""Check a saved build in one pass, offline (no game needed):

  python check.py <name> [--world W] [--before BOSS] [--no-gen] [--no-render]

  1. regenerate it from its design source (the JSON's "source"), unless --no-gen;
  2. pieces with no measurements in the catalogues (python measure_pieces.py <name> while the game runs);
  3. structural support with the world's limits (--world, else the site's "limits", its spot in sites.json, or
     build_limits.json's default_world): what would break and the weakest pieces;
  4. materials, each with the boss kill that opens it, and the pieces needing the world's "before" boss or later;
  5. pieces buried in others (furniture inside a wall, a block mostly inside another) and exact duplicates;
  6. comfort at every bed;
  7. previews oblique, front, upper and cut into previews/<name>-<view>.png, unless --no-render.
"""
import argparse, collections, json, math, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import blueprint as b  # noqa: E402
import support  # noqa: E402

CRUDE = ("roof", "_45", "_26", "stair", "ladder", "dragon", "raven", "wolf")      # sloped or odd shapes: boxes lie


def regenerate(bp):
    src = bp.get("source")
    if src and os.path.exists(os.path.join(HERE, src)):
        r = subprocess.run([sys.executable, src], cwd=HERE, capture_output=True, text=True)
        print(f"regenerated from {src}" if r.returncode == 0 else f"{src} failed:\n{r.stderr[-1500:]}")
        return r.returncode == 0
    print("no design source recorded: checking the saved JSON as it is")
    return True


def world_of(name, bp, a):
    limits = support.LIMITS
    w = a.world or bp["site"].get("limits") or b.load("sites").get(name, {}).get("world") or limits.get("default_world", "vanilla")
    return w if w in limits else "vanilla"


def check_support(pieces, world):
    support.use_world(world)
    sup = support.estimate(pieces)
    rows = sorted((sup[i] / support.MATS[support.material(p[0])][1], p) for i, p in enumerate(pieces))
    broken = [p for m, p in rows if m < 1]
    print(f"support ({world}): {len(broken)} of {len(pieces)} would break" +
          (f", e.g. {broken[:4]}" if broken else f"; weakest x{rows[0][0]:.2f} {rows[0][1][:4]}"))
    return not broken


def check_materials(pieces, before):
    stage, keys = b.stages()
    cat = {**b.load("pieces_offline"), **b.load("pieces"), **b.load("piece_costs")}
    total, late = collections.Counter(), collections.Counter()
    limit = keys.index(before) if before in keys else None
    for p in pieces:
        for item, n in cat.get(p[0], {}).get("cost", []):
            total[item] += n
            if limit is not None and stage.get(item, (99,))[0] >= limit:
                late[f"{p[0]} ({item})"] += 1
    need = max((stage[i] for i in total if i in stage), default=(0, "start", None))
    print("materials: " + ", ".join(f"{i} {n}" for i, n in total.most_common()))
    print(f"needs: {need[2] + ' killed' if need[2] else 'no boss'}" +
          (f"; before {before}: {'ok' if not late else 'too late: ' + str(dict(late))}" if limit is not None else ""))
    return not late


def overlap(a, c):
    d = [max(0.0, min(a[1], c[1]) - max(a[0], c[0])), max(0.0, min(a[3], c[3]) - max(a[2], c[2])),
         max(0.0, min(a[5], c[5]) - max(a[4], c[4]))]
    return d[0] * d[1] * d[2]


def clash_box(p):
    """The piece's box, a stone wall at its nominal size (the catalogue's includes the bevelled edges: 1.34 thick)."""
    c = support.CAT[p[0]]
    if not p[0].startswith("stone_wall"):
        return support.box(p)
    saved = c["size"]
    c["size"] = [round(saved[0]), round(saved[1]), 1.0]
    try:
        return support.box(p)
    finally:
        c["size"] = saved


TOGETHER = (("fire_pit", "cookingstation"), ("fire_pit", "cauldron"), ("hearth", "cookingstation"),
            ("cookingstation", "cookingstation"))


def meant(p, q, vp, vq, share):
    """Whether an overlap is a flaw: furniture in structure or in other furniture (but not a rug under something or
    a spit or cauldron on its fire), or one big block 60 % inside another (beams and poles set into walls are fine)."""
    fp, fq = (any(k in x[0] for k in support.HOLDS_NOTHING) or x[0].startswith("piece_") for x in (p, q))
    names = (p[0].lower(), q[0].lower())
    if any(a in names[0] and b in names[1] or a in names[1] and b in names[0] for a, b in TOGETHER):
        return False
    if p[0] == q[0] and math.dist(p[1:4], q[1:4]) < 0.05 and abs((p[4] - q[4]) % 360 - 180) < 1:
        return False                                                    # a mirrored pair: two shutters, two leaves
    if fp or fq:
        return not any(n.startswith("rug_") for n in names)
    return share >= 0.6 and min(vp, vq) > 0.5


def check_clashes(pieces):
    """Furniture a quarter or more inside another piece, a block 60 % or more inside another, exact duplicates."""
    found, seen, cells = [], set(), collections.defaultdict(list)
    boxes = [None if any(k in p[0] for k in CRUDE) or p[0] not in support.CAT else clash_box(p) for p in pieces]
    for i, bx in enumerate(boxes):
        if bx:
            cells[(math.floor((bx[0] + bx[1]) / 4), math.floor((bx[2] + bx[3]) / 4), math.floor((bx[4] + bx[5]) / 4))].append(i)
    vol = lambda bx: max((bx[1] - bx[0]) * (bx[3] - bx[2]) * (bx[5] - bx[4]), 1e-6)
    for (cx, cy, cz), members in cells.items():
        near = [j for dx in (-1, 0, 1) for dy in (-1, 0, 1) for dz in (-1, 0, 1) for j in cells.get((cx + dx, cy + dy, cz + dz), [])]
        for i in members:
            for j in near:
                if j <= i or (i, j) in seen:
                    continue
                seen.add((i, j))
                p, q = pieces[i], pieces[j]
                if p[0] == q[0] and math.dist(p[1:4], q[1:4]) < 0.05 and abs((p[4] - q[4]) % 360) < 1:
                    found.append(f"duplicate {p[:4]}")
                    continue
                share = overlap(boxes[i], boxes[j]) / min(vol(boxes[i]), vol(boxes[j]))
                if share >= 0.25 and meant(p, q, vol(boxes[i]), vol(boxes[j]), share):
                    found.append(f"{p[0]} {p[1:4]} and {q[0]} {q[1:4]} overlap {share:.0%}")
    print(f"clashes: {len(found)}" + ("" if not found else "\n  " + "\n  ".join(found[:12])))
    return not found


def check_comfort(pieces):
    cat = b.load("comfort")
    beds = [p for p in pieces if cat.get(p[0], {}).get("group") == "Bed"]
    if beds:
        totals = [b.comfort_at(pieces, cat, (p[1], p[2] + 0.5, p[3]))[0] for p in beds]
        print(f"comfort at {len(beds)} beds: {min(totals)}..{max(totals)}")


def render(name):
    out = os.path.join(HERE, "previews")
    r = subprocess.run([sys.executable, "preview.py", name, "--views", "oblique,front,upper,cut", "--out", out],
                       cwd=HERE, capture_output=True, text=True)
    shots = [l.split(" ", 1)[1] for l in r.stdout.splitlines() if l.startswith("preview ")]
    print("previews:\n  " + "\n  ".join(shots) if shots else "previews failed: " + (r.stdout + r.stderr)[-800:])


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("name")
    ap.add_argument("--world")
    ap.add_argument("--before", help="a boss: flag materials it or a later one opens (default: the world's)")
    ap.add_argument("--no-gen", action="store_true")
    ap.add_argument("--no-render", action="store_true")
    a = ap.parse_args()
    bp = b.load(a.name)
    if not a.no_gen and regenerate(bp):
        bp = b.load(a.name)
    pieces, world = bp["pieces"], world_of(a.name, bp, a)
    unknown = sorted({p[0] for p in pieces if p[0] not in support.CAT})
    print(f"{a.name}: {len(pieces)} pieces" + (f"; not measured: {unknown}" if unknown else ""))
    known = [p for p in pieces if p[0] in support.CAT]
    ok = check_support(known, world)
    ok &= check_materials(pieces, (a.before or support.LIMITS[world].get("before") or "").lower() or None)
    ok &= check_clashes(pieces)
    check_comfort(pieces)
    if not a.no_render:
        render(a.name)
    print("CHECK PASSED" if ok and not unknown else "CHECK FOUND PROBLEMS")


if __name__ == "__main__":
    main()

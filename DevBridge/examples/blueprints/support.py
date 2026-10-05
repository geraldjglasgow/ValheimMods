# Estimate the game's structural support for every piece of a blueprint, offline, and list the pieces that would
# break. It follows WearNTear.UpdateSupport with the catalogue's boxes in place of the real colliders:
#   - a piece touching the ground (box bottom at or below y 0.15: a hearth on a ground floor still counts in the
#     game) has its material's full support;
#   - otherwise from each touching piece j: s_j * (1 - loss * d), d = distance between centres + 0.1; loss is the
#     material's horizontal loss, or for a support point below the centre the mix of horizontal and vertical loss by
#     the angle down;
#   - two support points on opposite sides (100 degrees or more apart, seen from above) give their average, each
#     s_j * (1 - vertical loss * d): spans, lintels and arches;
#   - capped at the material's maximum; below its minimum the piece breaks.
# The material values come from build_limits.json, per world: the game's own (vanilla) or a server's, such as
# Ravenholt, whose Cornerstone mod changes them. Furniture (fires, torches, chests, stands) is held but holds nothing.
#   python support.py <name> [--world Ravenholt|vanilla] [--all]   worst margins, failures (default world: the
#                                                                   blueprint's site 'limits', its spot in sites.json, else vanilla)
#   python support.py --read <World>       read the synced Cornerstone values from the running game into build_limits.json
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
CAT = {**json.load(open(os.path.join(HERE, "pieces_offline.json"))), **json.load(open(os.path.join(HERE, "pieces.json")))}
LIMITS = json.load(open(os.path.join(HERE, "build_limits.json")))
MATS = {k: tuple(v) for k, v in LIMITS["vanilla"].items()}
GROUND = 0.15                                # a box bottom this low counts as on the ground
HOLDS_NOTHING = ("brazier", "torch", "chest", "Stand", "itemstand", "banner", "dragon", "bed", "rug", "fire", "lantern")


def material(n):
    if n.startswith("stone_") or n in ("hearth",):
        return "stone"
    if n.startswith(("wood_pole_log", "wood_wall_log", "wood_log_")):
        return "hard"
    if n.startswith(("woodiron", "iron_")):
        return "iron"
    return "wood"


def use_world(world):
    """Switch the material values to a world's (build_limits.json)."""
    MATS.clear()
    MATS.update({k: tuple(v) for k, v in LIMITS[world].items() if isinstance(v, list)})


def read_world(world):
    """Read Cornerstone's synced values from the running game (DevBridge) and save them under `world`."""
    sys.path.insert(0, HERE)
    import blueprint as b
    cfg = 'BepInEx.Bootstrap.Chainloader.PluginInfos["com.orianaventure.mod.Cornerstone"].Instance.Config'
    vals = {}
    for i in range(int(b.val(cfg + ".GetConfigEntries().Length"))):
        key = b.val(f"{cfg}.GetConfigEntries()[{i}].Definition").split(" = ", 1)[-1]
        vals[key] = b.val(f"{cfg}.GetConfigEntries()[{i}].BoxedValue").split(" = ", 1)[-1]
    names = {"stone": "Stone", "wood": "Wood", "hard": "HardWood", "iron": "Iron", "marble": "Marble"}
    keys = ("MaximumStability", "MinimumStability", "VerticalLoss", "HorizontalLoss")
    keep = {k: v for k, v in LIMITS.get(world, {}).items() if not isinstance(v, list)}       # its "before" etc.
    LIMITS[world] = {**keep, "read": "Cornerstone, synced from the server", **{
        m: [float(vals[f"{c}.{k}"]) for k in keys] if vals.get(f"{c}.EnableSection") == "True" else LIMITS["vanilla"][m]
        for m, c in names.items()}}
    json.dump(LIMITS, open(os.path.join(HERE, "build_limits.json"), "w"), indent=1)
    print(world, LIMITS[world])


def box(p):
    n, x, y, z, yaw = p[:5]
    c = CAT[n]
    sx, sy, sz = c["size"]
    ox, oy, oz = c.get("centre", [0, sy / 2 - c.get("pivot_above_bottom", 0), 0])
    t = math.radians(yaw)
    cx, cz = x + ox * math.cos(t) + oz * math.sin(t), z - ox * math.sin(t) + oz * math.cos(t)
    hx = abs(sx / 2 * math.cos(t)) + abs(sz / 2 * math.sin(t))
    hz = abs(sx / 2 * math.sin(t)) + abs(sz / 2 * math.cos(t))
    bottom = y - c.get("pivot_above_bottom", sy / 2)
    return (cx - hx, cx + hx, bottom, bottom + sy, cz - hz, cz + hz)


def touching(a, b, m=0.04):
    """Boxes within m of each other (furniture gets 0.08: the catalogue boxes leave it a few cm off the wall it rests
    on in the game)."""
    return a[0] < b[1] + m and a[1] > b[0] - m and a[2] < b[3] + m and a[3] > b[2] - m and a[4] < b[5] + m and a[5] > b[4] - m


def estimate(pieces):
    boxes = [box(p) for p in pieces]
    com = [((b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2) for b in boxes]
    holds = [not any(k in p[0] for k in HOLDS_NOTHING) for p in pieces]
    cells = {}
    for i, b in enumerate(boxes):                                     # 2 m grid for neighbour search
        for gx in range(math.floor(b[0] / 2), math.floor(b[1] / 2) + 1):
            for gy in range(math.floor(b[2] / 2), math.floor(b[3] / 2) + 1):
                for gz in range(math.floor(b[4] / 2), math.floor(b[5] / 2) + 1):
                    cells.setdefault((gx, gy, gz), set()).add(i)
    near = [set() for _ in pieces]
    for group in cells.values():
        for i in group:
            m = 0.04 if holds[i] else 0.08
            near[i] |= {j for j in group if j != i and holds[j] and touching(boxes[i], boxes[j], m)}
    sup = [MATS[material(p[0])][0] if boxes[i][2] <= GROUND else 0.0 for i, p in enumerate(pieces)]
    for _ in range(200):
        changed = False
        for i, p in enumerate(pieces):
            if boxes[i][2] <= GROUND:
                continue
            mx, _, vl, hl = MATS[material(p[0])]
            best, points = 0.0, []
            for j in near[i]:
                s = sup[j]
                if s <= 0:
                    continue
                d = math.dist(com[i], com[j]) + 0.1
                best = max(best, s - hl * d * s)
                b = boxes[j]
                q = [min(max(com[i][0], b[0]), b[1]), min(max(com[i][1], b[2]), b[3]), min(max(com[i][2], b[4]), b[5])]
                if q[1] < com[i][1] + 0.05:
                    v = [q[k] - com[i][k] for k in range(3)]
                    ln = math.sqrt(sum(c * c for c in v)) or 1.0
                    ny = v[1] / ln
                    if ny < 0:
                        t = math.acos(1 - abs(ny)) / (math.pi / 2)
                        best = max(best, s - (hl + (vl - hl) * t) * d * s)
                    points.append((v[0], v[2], s - vl * d * s))
            for a in range(len(points)):
                for c2 in range(a + 1, len(points)):
                    (ax, az, av), (bx, bz, bv) = points[a], points[c2]
                    if (ax * bx + az * bz) / ((math.hypot(ax, az) * math.hypot(bx, bz)) or 1) <= math.cos(math.radians(100)):
                        best = max(best, (av + bv) / 2)
            new = min(best, mx)
            if new > sup[i] + 0.01:
                sup[i], changed = new, True
        if not changed:
            break
    return sup


def main():
    if sys.argv[1] == "--read":
        return read_world(sys.argv[2])
    name = sys.argv[1]
    sites = json.load(open(os.path.join(HERE, "sites.json")))
    bp = json.load(open(os.path.join(HERE, name + ".json")))
    world = (sys.argv[sys.argv.index("--world") + 1] if "--world" in sys.argv
             else bp["site"].get("limits") or sites.get(name, {}).get("world", "vanilla"))
    use_world(world if world in LIMITS else "vanilla")
    print(f"support rules: {world if world in LIMITS else 'vanilla'}")
    pieces = bp["pieces"]
    sup = estimate(pieces)
    rows = sorted(((sup[i] / MATS[material(p[0])][1], sup[i], p) for i, p in enumerate(pieces)), key=lambda r: r[0])
    fail = [r for r in rows if r[0] < 1]
    for r in (rows if "--all" in sys.argv else rows[:15]):
        print(f"x{r[0]:6.2f}  support {r[1]:7.1f}  {r[2]}")
    print(f"{len(pieces)} pieces, {len(fail)} below their material's minimum (would break)")


if __name__ == "__main__":
    main()

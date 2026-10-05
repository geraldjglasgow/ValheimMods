#!/usr/bin/env python3
"""Copy the building the player stands in (or next to) into a blueprint, from the running game (DevBridge):

  python copy_build.py <name> [--floor 4x6] [--touching] [--radius 30] [--description "..."]

Only built pieces (made by a player, anyone's; no carts, boats or plants), and only the building's own: it finds the
floor under the player, follows the floor tiles at that level (a step up or down of 0.35 m at most) but never across
a wall, door or gate (so it stays in the room), and takes every
piece over that floor's outline (1.2 m past it for walls, 2.5 m for roofs), from the ground to the roof. A busy base
whose buildings touch each other stays out. --touching instead follows every piece whose box touches the last
(whole joined complexes). The frame is centred on the building, the ground is the terrain at its centre, and its front (-z) faces
its door (the door nearest the edge, snapped to 45 degrees; south when it has none). Writes <name>.json and prints
what it took.
"""
import argparse, json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import blueprint as b  # noqa: E402
import support  # noqa: E402

DOORS = ("door", "gate")
NOT_BUILDING = ("Pickable", "Cart", "Raft", "Karve", "VikingShip", "Trailership")   # plants and vehicles are not part of it


def gather(p, radius):
    seen = {}
    for dy in (-4, 4, 12, 20, 28):
        for o in b.nearby(radius, p[0], p[1] + dy, p[2]):
            if "piece" in o and o.get("creator") not in (0, None) and not o["prefab"].startswith(NOT_BUILDING):
                seen[o["id"]] = o
    return list(seen.values())


def boxed(o):
    view = f'ZNetScene.instance.FindInstance($zdo("{o["id"]}"))'
    o["yaw"] = float(b.val(view + ".transform.eulerAngles.y"))
    piece = [o["prefab"], o["position"][0], o["position"][1], o["position"][2], o["yaw"]]
    o["box"] = support.box(piece) if o["prefab"] in support.CAT else (
        piece[1] - 0.5, piece[1] + 0.5, piece[2] - 0.5, piece[2] + 0.5, piece[3] - 0.5, piece[3] + 0.5)
    return o


def seed(objs, p):
    under = [o for o in objs if o["box"][0] <= p[0] <= o["box"][1] and o["box"][4] <= p[2] <= o["box"][5]
             and p[1] - 2 <= o["box"][3] <= p[1] + 0.5]
    return min(under or objs, key=lambda o: abs(o["box"][3] - p[1]) if under else math.dist(o["position"], p))


def floor_outline(objs, p):
    """The x/z outline of the floor tiles joined to the one under the player at its level, or None."""
    floors = [o for o in objs if "floor" in o["prefab"]]
    under = [o for o in floors if o["box"][0] <= p[0] <= o["box"][1] and o["box"][4] <= p[2] <= o["box"][5]
             and p[1] - 1.5 <= o["box"][3] <= p[1] + 0.5]
    if not under:
        return None
    start = min(under, key=lambda o: abs(o["box"][3] - p[1]))
    walls = [o for o in objs if any(k in o["prefab"] for k in ("wall", "door", "gate", "stake"))
             and o["box"][2] < start["box"][3] + 1.8 and o["box"][3] > start["box"][3] + 0.2]
    room, todo = {start["id"]: start}, [start]
    while todo:
        a = todo.pop()
        for o in floors:
            if o["id"] not in room and abs(o["box"][3] - a["box"][3]) <= 0.35 and support.touching(a["box"], o["box"], 0.06)                     and not walled(a, o, walls):
                room[o["id"]] = o
                todo.append(o)
    bs = [o["box"] for o in room.values()]
    return min(b[0] for b in bs), max(b[1] for b in bs), min(b[4] for b in bs), max(b[5] for b in bs), min(b[2] for b in bs)


def walled(a, o, walls):
    """Whether a wall, door or gate stands on the edge between two floor tiles (the room ends there)."""
    m = ((a["box"][0] + a["box"][1] + o["box"][0] + o["box"][1]) / 4, (a["box"][4] + a["box"][5] + o["box"][4] + o["box"][5]) / 4)
    return any(w["box"][0] - 0.1 <= m[0] <= w["box"][1] + 0.1 and w["box"][4] - 0.1 <= m[1] <= w["box"][5] + 0.1 for w in walls)


def tiles_rect(objs, p, w, l):
    """The w x l block of 2 m floor tiles (either way round) that holds the tile under the player, is complete, and
    has the most walls on its edges: its outline in the tiles' own grid, as (yaw, centre, half sizes, base)."""
    floors = [o for o in objs if o["prefab"] == "wood_floor" or o["prefab"].startswith("stone_floor")]
    under = [o for o in floors if o["box"][0] <= p[0] <= o["box"][1] and o["box"][4] <= p[2] <= o["box"][5]
             and p[1] - 1.5 <= o["box"][3] <= p[1] + 0.5]
    if not under:
        return None
    s0 = min(under, key=lambda o: abs(o["box"][3] - p[1]))
    t = math.radians(s0["yaw"])
    local = lambda x, z: ((x - s0["position"][0]) * math.cos(t) - (z - s0["position"][2]) * math.sin(t),
                          (x - s0["position"][0]) * math.sin(t) + (z - s0["position"][2]) * math.cos(t))
    grid = {(round(local(o["position"][0], o["position"][2])[0] / 2), round(local(o["position"][0], o["position"][2])[1] / 2))
            for o in floors if abs(o["box"][3] - s0["box"][3]) <= 0.35}
    walls = [local(o["position"][0], o["position"][2]) for o in objs if any(k in o["prefab"] for k in ("wall", "door", "gate"))
             and o["box"][2] < s0["box"][3] + 1.8 and o["box"][3] > s0["box"][3] + 0.2]
    best = None
    for a, b2 in ((w, l), (l, w)):
        for i0 in range(-a + 1, 1):
            for j0 in range(-b2 + 1, 1):
                if all((i, j) in grid for i in range(i0, i0 + a) for j in range(j0, j0 + b2)):
                    x0, x1, z0, z1 = 2 * i0 - 1, 2 * (i0 + a) - 1, 2 * j0 - 1, 2 * (j0 + b2) - 1
                    edge = sum(1 for x, z in walls if (min(abs(x - x0), abs(x - x1)) < 0.4 and z0 - 0.5 <= z <= z1 + 0.5)
                               or (min(abs(z - z0), abs(z - z1)) < 0.4 and x0 - 0.5 <= x <= x1 + 0.5))
                    if not best or edge > best[0]:
                        best = (edge, x0, x1, z0, z1)
    if not best:
        return None
    _, x0, x1, z0, z1 = best
    return s0, local, (x0, x1, z0, z1), s0["box"][3]


def over_rect(objs, rect):
    """Everything over the tile block (walls and fittings 1.2 m past its edges, roofs 2 m), from its floor up, and
    the stairs that lead to it from outside (within 4 m of an edge, below the floor)."""
    s0, local, (x0, x1, z0, z1), top = rect
    out = []
    for o in objs:
        x, z = local((o["box"][0] + o["box"][1]) / 2, (o["box"][4] + o["box"][5]) / 2)
        n = o["prefab"]
        pad = (2.0 if "roof" in n else 4.0 if "stair" in n and o["box"][3] <= top + 0.3
               else 1.2 if any(k in n for k in ("wall", "pole", "beam", "log", "door", "gate")) else -0.1)
        if x0 - pad <= x <= x1 + pad and z0 - pad <= z <= z1 + pad and o["box"][3] >= top - 1.5:
            out.append(o)
    return out


def over(objs, outline):
    """Every piece whose centre is over the outline (walls 1.2 m past it, roofs 2.5 m), not below the floor's base."""
    x0, x1, z0, z1, base = outline
    out = []
    for o in objs:
        pad = 2.5 if "roof" in o["prefab"] else 1.2
        cx, cz = (o["box"][0] + o["box"][1]) / 2, (o["box"][4] + o["box"][5]) / 2
        if x0 - pad <= cx <= x1 + pad and z0 - pad <= cz <= z1 + pad and o["box"][3] >= base - 1.5:
            out.append(o)
    return out


def connected(objs, start):
    out, todo = {start["id"]}, [start]
    while todo:
        a = todo.pop()
        for o in objs:
            if o["id"] not in out and support.touching(a["box"], o["box"], 0.06):
                out.add(o["id"])
                todo.append(o)
    return [o for o in objs if o["id"] in out]


def frame(objs):
    xs = [v for o in objs for v in o["box"][:2]]
    zs = [v for o in objs for v in o["box"][4:]]
    c = ((min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2)
    ground = float(b.val(f"ZoneSystem.instance.GetGroundHeight($v3({c[0]:.2f},0,{c[1]:.2f}))"))
    doors = [o for o in objs if any(k in o["prefab"] for k in DOORS)]
    if not doors:
        return c, ground, 180.0
    d = max(doors, key=lambda o: math.hypot(o["position"][0] - c[0], o["position"][2] - c[1]))
    compass = math.degrees(math.atan2(d["position"][0] - c[0], d["position"][2] - c[1])) % 360
    return c, ground, round(compass / 45) * 45 % 360                     # 45 degree steps: diagonal buildings too


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("name"); ap.add_argument("--radius", type=float, default=30); ap.add_argument("--description")
    ap.add_argument("--touching", action="store_true", help="follow every touching piece (whole joined complexes)")
    ap.add_argument("--floor", help="WxL: the building stands on a block of W x L 2 m floor tiles (e.g. 4x6)")
    ap.add_argument("--at", help="x,z,y: start there instead of at the player (a point on the building's floor)")
    a = ap.parse_args()
    p = [float(v) for v in a.at.split(",")] if a.at else b.vec(b.val("$player.transform.position"))
    if a.at:
        p = [p[0], p[2], p[1]]                                          # given as x,z,y
    objs = [boxed(o) for o in gather(p, a.radius)]
    if not objs:
        sys.exit("no built pieces near the player")
    if a.floor:
        rect = tiles_rect(objs, p, *map(int, a.floor.lower().split("x")))
        if not rect:
            sys.exit(f"no complete {a.floor} block of floor tiles under the player")
        building = over_rect(objs, rect)
    else:
        outline = None if a.touching else floor_outline(objs, p)
        if not a.touching and not outline:
            sys.exit("no floor under the player: stand on the building's floor (or use --touching)")
        building = connected(objs, seed(objs, p)) if a.touching else over(objs, outline)
    c, ground, facing = frame(building)
    th = b.frame_yaw(facing)
    pieces = [b.capture(o, c, th, ground) for o in b.creation_order(building)]
    bp = {"name": a.name, "description": a.description or f"Copied from {b.status()['network']['world']} at "
          f"{c[0]:.1f},{c[1]:.1f}", "copied_from": {"at": [round(c[0], 2), round(c[1], 2)], "ground": round(ground, 2),
          "facing": facing}, "site": {**b.site_for(pieces), "limits": "Ravenholt"}, "pieces": pieces}
    b.write(bp, b.path_of(a.name))
    print(f"wrote {a.name}.json: {len(pieces)} pieces of {len(objs)} built pieces nearby; door faces {facing:.0f}; "
          f"{max(p[1] for p in pieces) - min(p[1] for p in pieces):.1f} x {max(p[3] for p in pieces) - min(p[3] for p in pieces):.1f} m, "
          f"{max(p[2] for p in pieces):.1f} m high")


if __name__ == "__main__":
    main()

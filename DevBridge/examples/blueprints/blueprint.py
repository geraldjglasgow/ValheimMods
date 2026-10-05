#!/usr/bin/env python3
"""Save and build piece blueprints in the running game through DevBridge (http://127.0.0.1:7780).

  python blueprint.py list
  python blueprint.py cost <name> [--before moder]                     materials, each with the boss that opens it
  python check.py <name>                                              the whole offline check of a design + previews
  python blueprint.py comfort <name> [--at x,y,z]     comfort at every bed (or the points), from comfort.json
  python blueprint.py build <name> [--facing west|north|east|south|<deg>] [--at x,z] [--ground y] [--dry] [--force]
  python blueprint.py ghost <name> [--facing ..] [--at x,z] [--ground y] [--outline] [--clear]
                                                      see it first: local copies only, and remember the spot
  python blueprint.py build <name> --saved            build on the spot the last ghost used
  python blueprint.py save <name> --center x,z --ground y --facing <door direction> [--radius 15] [--anyone]

build: where the player stands (or --at), the door facing where the player looks (snapped to 90 degrees) unless
--facing says otherwise. It refuses when anyone's pieces are already in the footprint (--replace removes them
first, --force builds over them), clears
trees, rocks and shrubs, levels a square at the player's feet (or --ground) with EarthWright's admin console
commands (a site's "terrain" steps: level and paint squares, e.g. dig a canal; "water" checks the ground suits it),
moves the player out in front of the door, places every piece in the saved order and counts them.
Preview a blueprint offline first with preview.py; designs are written with kit.py from pieces.json.
save: captures the pieces the local player built (all builders with --anyone) around a centre into <name>.json.

A blueprint's frame: x across the front, z from the front (the door, -z) to the back, y above the levelled ground,
yaw relative to the frame. Pieces are placed in list order, so the list must run in support order (lowest first,
what hangs after what holds it).
"""
import argparse, collections, json, math, os, sys, time, urllib.error, urllib.parse, urllib.request

BRIDGE = "http://127.0.0.1:7780"
HERE = os.path.dirname(os.path.abspath(__file__))
COMPASS = {"north": 0, "east": 90, "south": 180, "west": 270}


# ---------------------------------------------------------------- DevBridge calls
def get(path, **q):
    url = BRIDGE + path + ("?" + urllib.parse.urlencode(q) if q else "")
    return urllib.request.urlopen(url, timeout=120).read().decode()


def ev(expr):
    try:
        return get("/eval", expr=expr).strip()
    except urllib.error.HTTPError as x:
        return "ERR " + x.read().decode()[:200]


def val(expr):
    r = ev(expr)
    if r.startswith("ERR") or " = " not in r:
        raise RuntimeError(f"{expr}: {r}")
    return r.split(" = ", 1)[1].strip('"')


def vec(text):
    return [float(v) for v in text.strip("()").split(",")]


def console(cmd):
    return get("/console", cmd=cmd).strip()


def status():
    return json.loads(get("/status"))


def nearby(radius, x, y, z):
    return json.loads(get("/nearby", radius=str(radius), at=f"{x},{y},{z}", limit="5000"))


def player_id():
    return int(val("Game.instance.GetPlayerProfile().GetPlayerID()"))


# ---------------------------------------------------------------- frame maths
def frame_yaw(door):
    """World yaw of the frame whose door (-z) faces the compass direction `door`."""
    return (door + 180) % 360


def to_world(c, th, x, z):
    t = math.radians(th)
    return c[0] + x * math.cos(t) + z * math.sin(t), c[1] - x * math.sin(t) + z * math.cos(t)


def to_local(c, th, wx, wz):
    t, dx, dz = math.radians(th), wx - c[0], wz - c[1]
    return dx * math.cos(t) - dz * math.sin(t), dx * math.sin(t) + dz * math.cos(t)


def parse_facing(text):
    deg = COMPASS[text.lower()] if text.lower() in COMPASS else float(text)
    return round(deg / 90) * 90 % 360


def look_facing():
    fx, _, fz = vec(val("GameCamera.instance.transform.forward"))
    return round(math.degrees(math.atan2(fx, fz)) / 90) * 90 % 360


def bounds(bp, margin=1.0):
    xs = [p[1] for p in bp["pieces"]]
    zs = [p[3] for p in bp["pieces"]]
    return min(xs) - margin, max(xs) + margin, min(zs) - margin, max(zs) + margin


def inside(box, c, th, o):
    x, z = to_local(c, th, o["position"][0], o["position"][2])
    return box[0] <= x <= box[1] and box[2] <= z <= box[3]


def reach(box):
    return math.hypot(max(abs(box[0]), abs(box[1])), max(abs(box[2]), abs(box[3]))) + 1


# ---------------------------------------------------------------- blueprint files
def path_of(name):
    return os.path.join(HERE, name + ".json")


def load(name):
    with open(path_of(name)) as f:
        return json.load(f)


def write(bp, out):
    head = json.dumps({k: v for k, v in bp.items() if k != "pieces"}, indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in bp["pieces"])
    with open(out, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")


def cmd_list(_):
    for fn in sorted(os.listdir(HERE)):
        if fn.endswith(".json") and fn not in ("pieces.json", "sites.json", "comfort.json", "ships.json",
                                               "pieces_offline.json"):
            bp = load(fn[:-5])
            print(f"{fn[:-5]}: {len(bp['pieces'])} pieces - {bp.get('description', '')}")


def stages():
    """progression.json as {material: (stage index, stage key, boss)} and the ordered stage keys."""
    pr = load("progression")["stages"]
    return {m: (i, st["key"], st["boss"]) for i, st in enumerate(pr) for m in st["materials"]}, [st["key"] for st in pr]


def cmd_cost(a):
    """What a blueprint costs to build by hand (piece_costs.json from the game, then the catalogues), each material
    with the boss kill that opens it (progression.json); --before <boss> lists the pieces that need that boss or
    later."""
    bp = load(a.name)
    cat = {**load("pieces_offline"), **load("pieces"), **{k: v for k, v in load("piece_costs").items()}}
    stage, keys = stages()
    total, unknown, late = collections.Counter(), collections.Counter(), collections.Counter()
    limit = keys.index(a.before.lower()) if a.before else None
    for p in bp["pieces"]:
        if p[0] not in cat:
            unknown[p[0]] += 1
            continue
        for item, n in cat[p[0]]["cost"]:
            total[item] += n
            if limit is not None and stage.get(item, (99,))[0] >= limit:
                late[f"{p[0]} ({item})"] += 1
    print(f"{a.name}: {len(bp['pieces'])} pieces")
    for item, n in total.most_common():
        print(f"  {item}: {n}   {stage[item][1] if item in stage else '?'}")
    need = max((stage[i] for i in total if i in stage), default=(0, "start", None))
    print(f"needs: {'nothing beyond the start' if need[2] is None else need[2] + ' killed'}")
    if limit is not None:
        print(f"pieces needing {a.before} or later: {dict(late) if late else 'none'}")
    if unknown:
        print("no cost known (python piece_costs.py, or measure_pieces.py <name>):", dict(unknown))


def comfort_at(pieces, cat, at, shelter=True):
    """Comfort the game would give at a point (SE_Rested): 1, +1 in shelter, then within 10 m of the point the best
    piece of each comfort group and every distinct group-None name. Fires are counted as if lit."""
    best, extra = {}, {}
    for n, x, y, z, _ in pieces:
        c = cat.get(n)
        if not c or c["comfort"] <= 0 or math.dist(at, (x, y, z)) >= 10:
            continue
        if c["group"] == "None":
            extra[c["name"]] = c["comfort"]
        elif c["comfort"] > best.get(c["group"], (0, ""))[0]:
            best[c["group"]] = (c["comfort"], c["name"] + (" (lit)" if c.get("needs_lit") else ""))
    total = (2 if shelter else 1) + sum(v[0] for v in best.values()) + sum(extra.values())
    return total, best, extra


def cmd_comfort(a):
    """Comfort at every bed of a blueprint (or at --at x,y,z points, ';' between), from comfort.json."""
    bp, cat = load(a.name), load("comfort")
    beds = [p for p in bp["pieces"] if cat.get(p[0], {}).get("group") == "Bed"]
    points = [tuple(pair(t)) for t in a.at.split(";")] if a.at else [(p[1], p[2] + 0.5, p[3]) for p in beds]
    if not points:
        sys.exit("no beds in this blueprint; give points with --at x,y,z")
    for pt in points:
        total, best, extra = comfort_at(bp["pieces"], cat, pt)
        parts = ", ".join(f"{g} {v[0]} ({v[1]})" for g, v in sorted(best.items())) + "".join(
            f", {n} {c}" for n, c in extra.items())
        print(f"comfort {total} at {pt[0]:.1f},{pt[1]:.1f},{pt[2]:.1f}: 2 base+shelter, {parts}")


# ---------------------------------------------------------------- build
def blockers(box, c, th, ground):
    return [o for o in nearby(reach(box), c[0], ground, c[1])
            if o.get("creator") not in (0, None) and "tombstone" not in o.get("prefab", "").lower() and inside(box, c, th, o)]


def remove_pieces(found):
    """Take built pieces away without drops (the owner role first, so the removal reaches everyone)."""
    for o in found:
        view = f'ZNetScene.instance.FindInstance($zdo("{o["id"]}"))'
        ev(view + ".ClaimOwnership()")
        ev(f"ZNetScene.instance.Destroy({view}.gameObject)")
    print(f"removed {len(found)} built pieces from the footprint")


def require_earthwright():
    if not any(p.startswith("EarthWright ") for p in status()["plugins"]):
        sys.exit("EarthWright is not loaded: it clears and levels the site.")


def remove_shrubs(box, c, th, ground):
    for o in nearby(reach(box), c[0], ground, c[1]):
        name = o.get("prefab", "").lower()
        if o.get("creator") in (0, None) and ("shrub" in name or "bush" in name) and inside(box, c, th, o):
            view = f'ZNetScene.instance.FindInstance($zdo("{o["id"]}"))'
            ev(view + ".ClaimOwnership()")
            ev(f"ZNetScene.instance.Destroy({view}.gameObject)")


def prepare_site(bp, box, c, th, ground):
    """Clear around the centre (the player stands there), then run the site's terrain steps, each around the player
    moved to its centre: level (a square at ground + y) or paint. Without steps: one level square of level_half."""
    site = bp["site"]
    if site.get("clear") != "per_level":                              # else each level step clears its own area
        print(console(f"ew forestry {site['clear_radius']}"))
        print(console(f"ew debris {site['clear_radius']}"))
    remove_shrubs(box, c, th, ground)
    steps = site.get("terrain") or [{"op": "level", "at": [0, 0], "half": site["level_half"], "y": 0}]
    if abs((th + 45) % 90 - 45) > 0.01:                               # squares are world-aligned: cover with circles
        steps = [c2 for st in steps for c2 in as_circles(st)]
        print(f"the frame is turned {th:.1f} deg: {len(steps)} circle steps instead of squares")
    done = 0
    for i, step in enumerate(steps):
        if site.get("clear") == "per_level" and step["op"] == "level" and step.get("y", 0) >= 0:
            step = dict(step, clear_here=True)
        done += terrain_step(step, c, th, ground) is not False
        if i % 25 == 24:
            print(f"  terrain {i + 1} of {len(steps)}")
    print(f"terrain steps done {done} of {len(steps)}")
    time.sleep(4)


def as_circles(step):
    """Circles that together cover a frame square (corners included): a grid of radius r, centres within the square."""
    h = step["half"]
    dug = step["op"] == "level" and step.get("y", 0) < 0
    r = min(h, 3.0 if dug else 16.0 if step["op"] == "level" else 8.0)   # small circles keep a dug edge within 0.9 m
    inner = h - r / math.sqrt(2)
    n = max(1, math.ceil(2 * inner / (r * math.sqrt(2) * 0.95)) + 1) if inner > 0.01 else 1
    offs = [0.0] if n == 1 else [-inner + 2 * inner * i / (n - 1) for i in range(n)]
    return [dict(step, at=[step["at"][0] + dx, step["at"][1] + dz], half=r, shape="circle") for dx in offs for dz in offs]


def put_player(x, z, lift=3.0):
    """Set the (flying) player straight onto a point: no teleport, no loading screen, no cooldown. Over water the
    player is held above the surface, where swimming would drift them (terrain edits only use x and z)."""
    ground = float(val(f"ZoneSystem.instance.GetGroundHeight($v3({x:.3f},0,{z:.3f}))"))
    y = max(ground, float(val("ZoneSystem.instance.m_waterLevel")) + 1.5) + lift
    for part in ("transform.position", "m_body.position"):
        ev(f"$player.{part} = $v3({x:.3f},{y:.3f},{z:.3f})")
    ev("$player.m_body.linearVelocity = $v3(0,0,0)")


def at_point(x, z, tries=5):
    """Put the player on the point and make sure it is still there (the user may be moving); False if it never held."""
    for _ in range(tries):
        put_player(x, z)
        time.sleep(0.15)
        p = vec(val("$player.transform.position"))
        if math.hypot(p[0] - x, p[2] - z) < 0.3:
            return True
    return False


def terrain_step(step, c, th, ground):
    wx, wz = to_world(c, th, *step["at"])
    if not at_point(wx, wz):
        print(f"skipped {step['op']} at {wx:.2f},{wz:.2f}: the player would not stay there (hands off the controls)")
        return False
    shape = step.get("shape", "square")
    if step.get("clear_here"):
        reach = step["half"] * (1.42 if shape == "square" else 1.0)
        console(f"ew forestry {reach:.1f}")
        console(f"ew debris {reach:.1f}")
        at_point(wx, wz)                                              # clearing can nudge the player off the point
    if step["op"] == "level":
        cmd = f"ew terrain level {step['half']:.2f} {ground + step.get('y', 0):.3f} shape={shape}"
    else:
        cmd = f"ew terrain paint {step['half']:.2f} paint={step['paint']} shape={shape}"
    p = vec(val("$player.transform.position"))
    if math.hypot(p[0] - wx, p[2] - wz) >= 0.3:
        print(f"skipped {step['op']} at {wx:.2f},{wz:.2f}: the player moved before the edit")
        return False
    out = console(cmd)
    if "sent" not in out:
        sys.exit(f"'{cmd}' was refused (ew terrain needs an admin); nothing was placed.")
    time.sleep(0.3)
    return True


def check_water(site, ground, force):
    """A site with a canal needs ground just above sea level: dry land, but the dug floor deep enough under water."""
    w = site.get("water")
    if not w:
        return
    sea = float(val("ZoneSystem.instance.m_waterLevel"))
    top = ground + w["floor"] + w["depth"]
    if (ground < sea + 0.5 or top > sea) and not force:
        sys.exit(f"ground {ground:.2f} does not suit the canal: pick {sea + 0.5:.1f} to {sea - w['floor'] - w['depth']:.1f} "
                 f"(sea level {sea:.1f}) with --ground, or --force.")


def move_player(x, z, yaw, lift=0.3, near=2.0, strict=True):
    y = float(val(f"ZoneSystem.instance.GetGroundHeight($v3({x:.3f},0,{z:.3f}))")) + lift
    ev(f"$player.TeleportTo($v3({x:.3f},{y:.3f},{z:.3f}), Quaternion.Euler(0,{yaw},0), false)")
    for _ in range(30):
        time.sleep(0.5)
        p = status()["player"]["position"]
        if math.hypot(p[0] - x, p[2] - z) < near:
            return
    if strict:
        sys.exit(f"the player did not reach {x:.2f},{z:.2f}; stopped.")
    print(f"warning: the player stopped short of {x:.2f},{z:.2f}; carrying on")


def place_all(bp, c, th, ground):
    bad = 0
    for i, (n, x, y, z, yaw) in enumerate(bp["pieces"]):
        wx, wz = to_world(c, th, x, z)
        r = ev(f'$player.PlacePiece($prefab("{n}").Piece, $v3({wx:.3f},{ground + y:.3f},{wz:.3f}), '
               f'Quaternion.Euler(0,{(yaw + th) % 360},0), false)')
        if r.startswith("ERR") or "rror" in r:
            bad += 1
            print("failed", n, x, y, z, r[:120])
        if i % 500 == 499:
            print(f"  {i + 1} of {len(bp['pieces'])}")
    print(f"placed {len(bp['pieces']) - bad} of {len(bp['pieces'])}")


def verify(bp, box, c, th, ground, me):
    """Count what stands, searching spheres stacked every 8 m up to the build's top (one sphere at the ground misses
    the upper part of a tall build), so nothing is counted twice."""
    time.sleep(6)
    top, r, seen = max(p[2] for p in bp["pieces"]), reach(box) + 2, {}
    for y in range(0, int(top) + 8, 8):
        for o in nearby(math.hypot(r, 5), c[0], ground + y, c[1]):
            seen[o["id"]] = o
    objs = list(seen.values())
    got = collections.Counter(o["prefab"] for o in objs if o.get("creator") == me and inside(box, c, th, o))
    want = collections.Counter(p[0] for p in bp["pieces"])
    missing = {n: want[n] - got[n] for n in want if got[n] < want[n]}
    print(f"standing: {sum(got.values())} of {sum(want.values())}" + (f", missing {missing}" if missing else ""))
    drops = [o["prefab"] for o in objs if o.get("item") and inside(box, c, th, o)]
    if drops:
        print("item drops in the footprint (a piece broke?):", collections.Counter(drops))


def fit_ground(site, feet):
    """The player's feet, or for a canal site the nearest height that holds water (said when it differs)."""
    w = site.get("water")
    if not w:
        return feet
    sea = float(val("ZoneSystem.instance.m_waterLevel"))
    ground = min(max(feet, sea + 0.5), sea - w["floor"] - w["depth"])
    if abs(ground - feet) > 0.01:
        print(f"ground {ground:.2f} instead of your feet {feet:.2f}: the canal needs ground {sea + 0.5:.1f} to "
              f"{sea - w['floor'] - w['depth']:.1f} (levelling cuts or fills to it)")
    return ground


def site_of(a, bp):
    """Centre, ground and door direction: --saved (from ghost), else --at or the player, --ground or the feet,
    --facing or where the player looks."""
    if a.saved:
        s = load("sites").get(a.name) or sys.exit(f"no saved site for {a.name}: place it with ghost first")
        if s["world"] != status()["network"]["world"]:
            sys.exit(f"the saved site is in world {s['world']}")
        return tuple(s["at"]), a.ground if a.ground is not None else s["ground"], s["facing"]
    p = status()["player"]["position"]
    start = a.line[0] if a.line else a.at
    c = tuple(start) if start else (p[0], p[2])
    feet = float(val(f"ZoneSystem.instance.GetGroundHeight($v3({c[0]},0,{c[1]}))")) if start else p[1]
    ground = a.ground if a.ground is not None else fit_ground(bp["site"], feet)
    if a.line:
        (x1, z1), (x2, z2) = a.line
        return c, ground, round((math.degrees(math.atan2(x2 - x1, z2 - z1)) + 90) % 360, 2)   # door: right of the line
    return c, ground, parse_facing(a.facing) if a.facing else look_facing()


def compass(deg):
    names = {0: "north", 90: "east", 180: "south", 270: "west"}
    return names.get(round(deg, 2) % 360, f"{deg % 360:.0f} deg (0 north, 90 east)")


def cmd_build(a):
    bp = load(a.name)
    c, ground, door = site_of(a, bp)
    th, box = frame_yaw(door), bounds(bp)
    print(f"{a.name}: {len(bp['pieces'])} pieces at {c[0]:.2f},{c[1]:.2f}, ground {ground:.2f}, door faces {compass(door)}")
    found = blockers(box, c, th, ground)
    if found:
        kinds = ", ".join(f"{n} x{k}" for n, k in collections.Counter(o["prefab"] for o in found).most_common(5))
        print(f"{len(found)} built pieces already in the footprint ({kinds})")
        if not (a.force or a.replace):
            sys.exit("--replace removes them first (no drops), --force builds over them.")
    check_water(bp["site"], ground, a.force)
    if a.dry:
        return
    require_earthwright()
    if found and a.replace:
        remove_pieces(found)
    p = status()["player"]["position"]
    if math.hypot(p[0] - c[0], p[2] - c[1]) > 0.3:
        move_player(c[0], c[1], th, near=0.3)                         # clearing works around the player
    prepare_site(bp, box, c, th, ground)
    move_player(c[0], c[1], th, strict=False)                         # every piece within loading range
    place_all(bp, c, th, ground)
    verify(bp, box, c, th, ground, player_id())
    sx, sz = to_world(c, th, *bp["site"]["stand"])
    move_player(sx, sz, th, strict=False)


# ---------------------------------------------------------------- ghost
def cmd_ghost(a):
    """Show the blueprint where it would go as still local copies (DevBridge's stage: no collision, never saved,
    only on this machine), and remember the spot for build --saved. --outline: only what stands below 1.2 m."""
    bp = load(a.name)
    get("/clear", kind="game")
    if a.clear:
        print("ghost cleared")
        return
    c, ground, door = site_of(a, bp)
    th = frame_yaw(door)
    pieces = [p for p in bp["pieces"] if p[2] <= 1.2] if a.outline else bp["pieces"]
    canal = math.degrees(math.atan2(math.cos(math.radians(th)), -math.sin(math.radians(th))))
    print(f"{a.name}: {len(pieces)} ghost pieces at {c[0]:.2f},{c[1]:.2f}, ground {ground:.2f}, door/gate faces "
          f"{compass(door)}, frame +x (the canal side) toward {compass(canal % 360)}")
    bad = place_ghost(pieces, c, th, ground)
    sites = load("sites") if os.path.exists(path_of("sites")) else {}
    sites[a.name] = {"world": status()["network"]["world"], "at": [round(c[0], 2), round(c[1], 2)],
                     "ground": round(ground, 2), "facing": door, "saved": time.strftime("%Y-%m-%d %H:%M")}
    with open(path_of("sites"), "w") as f:
        json.dump(sites, f, indent=1)
    print(f"placed {len(pieces) - bad} ghost pieces; spot saved: python blueprint.py build {a.name} --saved; "
          f"python blueprint.py ghost {a.name} --clear removes them")


def place_ghost(pieces, c, th, ground):
    """A stage copy at an exact point is turned toward the player first, so each yaw is given relative to that."""
    bad, me = 0, None
    for i, (n, x, y, z, yaw) in enumerate(pieces):
        if i % 25 == 0:
            me = vec(val("$player.transform.position"))
        wx, wz = to_world(c, th, x, z)
        turn = math.degrees(math.atan2(me[0] - wx, me[2] - wz)) if math.hypot(me[0] - wx, me[2] - wz) > 0.05 else 0
        try:
            get("/lineup", prefabs=n, at=f"{wx:.3f},{ground + y:.3f},{wz:.3f}", yaw=f"{(yaw + th - turn) % 360:.2f}", sit="0")
        except urllib.error.HTTPError:
            bad += 1
        if i % 1000 == 999:
            print(f"  {i + 1} of {len(pieces)}")
    return bad


# ---------------------------------------------------------------- save
def capture(o, c, th, ground):
    view = f'ZNetScene.instance.FindInstance($zdo("{o["id"]}"))'
    wx, wy, wz = vec(val(view + ".transform.position"))
    yaw = float(val(view + ".transform.eulerAngles.y"))
    x, z = to_local(c, th, wx, wz)
    return [o["prefab"], round(x, 3), round(wy - ground, 3), round(z, 3), round((yaw - th) % 360, 1)]


def creation_order(objs):
    """ZDO ids count up as objects are made: the build order, when one session made them all; else lowest first."""
    if len({o["id"].split(":")[0] for o in objs}) == 1:
        return sorted(objs, key=lambda o: int(o["id"].split(":")[1]))
    return sorted(objs, key=lambda o: o["position"][1])


def site_for(pieces):
    low = [max(abs(p[1]), abs(p[3])) for p in pieces if p[2] < 1] or [5]
    far = max(math.hypot(p[1], p[3]) for p in pieces)
    return {"level_half": round(max(low) + 0.5, 1), "clear_radius": math.ceil(far + 1.5),
            "stand": [0, round(min(p[3] for p in pieces) - 3, 1)]}


def cmd_save(a):
    c, th, me = a.center, frame_yaw(parse_facing(a.facing)), player_id()
    objs = [o for o in nearby(a.radius, c[0], a.ground, c[1])
            if o.get("piece") and (o.get("creator") == me or (a.anyone and o.get("creator") not in (0, None)))]
    if not objs:
        sys.exit("no built pieces found there (is the area loaded?)")
    pieces = [capture(o, c, th, a.ground) for o in creation_order(objs)]
    bp = {"name": a.name, "description": a.description or "", "site": site_for(pieces), "pieces": pieces}
    out = a.out or path_of(a.name)
    write(bp, out)
    print(f"wrote {out}: {len(pieces)} pieces, site {bp['site']}")


def pair(text):
    return [float(v) for v in text.split(",")]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("list").set_defaults(fn=cmd_list)
    c = sub.add_parser("cost")
    c.add_argument("name"); c.add_argument("--before", help="a boss: list the pieces that need it or a later one")
    c.set_defaults(fn=cmd_cost)
    cf = sub.add_parser("comfort")
    cf.add_argument("name"); cf.add_argument("--at"); cf.set_defaults(fn=cmd_comfort)
    for verb, fn in (("build", cmd_build), ("ghost", cmd_ghost)):
        b = sub.add_parser(verb)
        b.add_argument("name"); b.add_argument("--facing"); b.add_argument("--at", type=pair)
        b.add_argument("--ground", type=float); b.add_argument("--saved", action="store_true")
        b.add_argument("--dry", action="store_true"); b.add_argument("--force", action="store_true")
        b.add_argument("--outline", action="store_true"); b.add_argument("--clear", action="store_true")
        b.add_argument("--replace", action="store_true")
        b.add_argument("--line", type=lambda t: [pair(v) for v in t.split(":")],
                       help="x1,z1:x2,z2 - centre at the first point, the frame's +x (a canal's way out) toward the second")
        b.set_defaults(fn=fn)
    s = sub.add_parser("save")
    s.add_argument("name"); s.add_argument("--center", type=pair, required=True)
    s.add_argument("--ground", type=float, required=True); s.add_argument("--facing", required=True)
    s.add_argument("--radius", type=float, default=15); s.add_argument("--anyone", action="store_true")
    s.add_argument("--description"); s.add_argument("--out"); s.set_defaults(fn=cmd_save)
    a = ap.parse_args()
    a.fn(a)


if __name__ == "__main__":
    main()

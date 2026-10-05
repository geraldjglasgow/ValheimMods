# Design source of compound.json (and the single-building blueprints great_hall, longhouse, smithy, barn): a walled
# compound with a boat canal, after a picture the user showed on 2026-10-05. Run it to write the JSON files again.
#
# Frame: x along the canal (it leaves through the east wall), z across, y above the levelled ground; the main gate
# is in the south (-z) wall. Walls: x +-41, z +-30, stone to the 4 m wall walk, wooden breastwork and merlons;
# four 10 m corner towers, two gate towers, two water-gate towers, three stairs up to the walk.
# Canal: 8 m of water from x -20 out past the east wall, stone sides, stone quays, one footbridge on the gate road.
# North: great hall (hearth, smoke vent), plain_wood_house, open smithy, fenced field. South: guard house
# (plain_wood_house), two longhouses, barn. Buildings are empty shells apart from the hall's hearth.
import json, os
from kit import Plan, Line, moved, spans, stone_run, curtain, wall_stairs, building, tower, fence_rect, WALK_IN

HERE = os.path.dirname(os.path.abspath(__file__))
XW, ZW = 41, 30                    # wall lines
GATE = (-14, -10)                  # main gate opening in the south wall
WATER = (-5, 5)                    # water gate opening in the east wall


def walls():
    p = Plan()
    south, north = Line(0, -ZW, "+x", "-z"), Line(0, ZW, "+x", "+z")
    west, east = Line(-XW, 0, "+z", "-x"), Line(XW, 0, "+z", "+x")
    curtain(p, south, -36, -20)
    curtain(p, south, *GATE, stone=False, posts="ends")
    curtain(p, south, -4, 36)
    curtain(p, north, -36, 36)
    curtain(p, west, -25, 25)
    curtain(p, east, -25, -11)
    curtain(p, east, *WATER, stone=False, posts="ends")
    curtain(p, east, 11, 25)
    for u in (GATE[0] + 1, GATE[1] - 1):
        south.put(p, 10, "wood_gate", u, 1.05)
    wall_stairs(p, west, -6)
    wall_stairs(p, north, -6)
    wall_stairs(p, east, 14)
    return p.pieces()


def towers():
    inner_x, inner_z = XW - WALK_IN, ZW - WALK_IN
    walks = [("x", inner_z, ZW - 0.5), ("x", -ZW + 0.5, -inner_z), ("z", inner_x, XW - 0.5), ("z", -XW + 0.5, -inner_x)]
    out = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            out += tower(sx * XW, sz * ZW, 5, walks, "z" if sx * sz > 0 else "x").pieces()
    for cx in (GATE[0] - 3, GATE[1] + 3):
        out += tower(cx, -ZW + 1.5, 3, walks, "x").pieces()
    for cz in (WATER[1] + 3, WATER[0] - 3):
        out += tower(XW - 1.5, cz, 3, walks, "z").pieces()
    return out


def canal():
    p = Plan()
    for zs in (-1, 1):
        stone_run(p, 0, Line(0, zs * 4.5, "+x", "+z" if zs > 0 else "-z"), -20, 46, -4, 2, False)
    stone_run(p, 0, Line(-20.5, 0, "+z", "-x"), -5, 5, -4, 2, False)
    for x in spans(-24, 36):
        for z in (-8, -6, 6, 8):
            p.add(1, "stone_floor_2x2", x, -0.4, z)                   # quays, top 0.1
    for x in (-23, -21):
        for z in spans(-5, 5):
            p.add(1, "stone_floor_2x2", x, -0.4, z)                   # quay across the canal head
    for x in (-4, 4, 12, 20, 28):
        for z in (-5.4, 5.4):
            p.add(2, "wood_pole_log", x, 0.91, z)                     # mooring posts
    bx = sum(GATE) / 2                                                # footbridge on the gate road
    for x in (bx - 1, bx + 1):
        for z in spans(-5, 5):
            p.add(2, "wood_floor", x, 0.1, z)
    for x in (bx - 1.9, bx + 1.9):
        for z in range(-5, 6, 2):
            p.add(3, "wood_pole", x, 0.68, z)
        for z in spans(-5, 5):
            p.add(4, "wood_beam", x, 1.18, z, 90)
    fence_rect(p, 26, 12, 34, 24, gate_at=29)                          # the field
    return p.pieces()


HALL_POSTS = [(n, sx, y, z, 0) for sx in (-3, 3) for z in (-10, -6, -2, 2, 6, 10)
              for n, y in (("wood_pole_log_4", 2.0), ("wood_pole_log", 4.6))]       # top 5.7, under the thatch


def designs():
    return {
        "great_hall": (building(10, 26, 4, {"F": "SWSWS", "K": "SWSWS", "Wst": "SWSWSWSWSWSWS", "E": "SWSWSWDWSWSWS"},
                                vents=(-1, 1), inside=[("hearth", 0, 0.13, 0, 90)] + HALL_POSTS),
                       "Great hall 10 x 26 m, 4 m walls, door in the middle of the long side (local +x), hearth "
                       "under a capped ridge vent, two rows of log posts."),
        "longhouse": (building(10, 14, 4, {"F": "SWDWS", "K": "SSWSS", "Wst": "SWSWSWS", "E": "SWSWSWS"}),
                      "Longhouse 10 x 14 m, 4 m walls, Wood Gate in the front gable, crossed gable horns."),
        "smithy": (building(10, 14, 4, {"F": "OOOOO", "K": "SSSSS", "Wst": "SOOOOOS", "E": "SOOOOOS"}, floor="stone"),
                   "Open smithy 10 x 14 m on a stone floor: open front and sides, solid back."),
        "barn": (building(10, 10, 4, {"F": "SSDSS", "K": "SSSSS", "Wst": "SWSWS", "E": "SWSWS"}),
                 "Barn 10 x 10 m, 4 m walls, Wood Gate in the front gable."),
    }


def house():
    with open(os.path.join(HERE, "plain_wood_house.json")) as f:
        return json.load(f)["pieces"]


def compound(parts):
    out = walls() + towers() + canal()
    out += moved(parts["great_hall"], -21, 17, 90)
    out += moved(house(), 3, 17, 0)
    out += moved(parts["smithy"], 18, 17, 0)
    out += moved(house(), -28, -17, 270)                               # guard house, door toward the gate road
    out += moved(parts["longhouse"], 0, -17, 180)
    out += moved(parts["longhouse"], 15, -17, 180)
    out += moved(parts["barn"], 29.5, -17, 90)
    return out


SITE = {
    "clear_radius": 60,
    "stand": [-12, -40],
    "water": {"floor": -4, "depth": 1.5},
    "terrain": [{"op": "level", "at": [x, 0], "half": 36, "y": 0} for x in (-11, 11)]
               + [{"op": "level", "at": [x, 0], "half": 4, "y": -4} for x in range(-16, 49, 8)]
               + [{"op": "paint", "at": [30, z], "half": 4, "paint": "cultivated"} for z in (16, 20)]
               + [{"op": "paint", "at": [-12, z], "half": 2, "paint": "paved"} for z in (-28, -24, -20, -16, -12, 10)],
}


def write(name, desc, site, pieces):
    head = json.dumps({"name": name, "description": desc, "site": site}, indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in pieces)
    path = os.path.join(HERE, name + ".json")
    with open(path, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
    print(f"wrote {path}: {len(pieces)} pieces")


def main():
    parts = {}
    for name, (plan, desc) in designs().items():
        parts[name] = plan.pieces()
        far = max(abs(p[3]) for p in parts[name])
        write(name, desc, {"level_half": max(abs(p[1]) for p in parts[name] if p[2] < 1) + 0.5,
                           "clear_radius": 12, "stand": [0, -(far + 3)]}, parts[name])
    write("compound", "Walled compound 82 x 60 m with a boat canal through the east wall (dig it below sea level), "
          "corner and gate towers, wall walk, great hall, two longhouses, two plain wood houses, smithy, barn and "
          "a fenced field. Pick ground 30.5 to 32.5 m (sea level 30) with open water east of it.", SITE, compound(parts))


if __name__ == "__main__":
    main()

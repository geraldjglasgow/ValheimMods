# Design source of compound.json, the walled village, and of the older single-building blueprints great_hall,
# longhouse, smithy and barn. Run it to write the JSON files again. Layout numbers: village_plan.py (and its plan
# image); buildings: village_buildings.py; furniture: furnish.py.
#
# Frame: x along the canal (west to east), z across (north +z), y above the levelled ground; the main gate is in the
# south wall. Walls x +-64, z +-53: stone to the 4 m wall walk, breastwork and merlons, ten towers (corners, the main
# gate pair, two pairs at the water gates), stairs up to the walk on four walls.
# Canal: 15 m of water (the user's rule), floor 5 m below the ground, straight through the village between water
# gates open to the sky (masts); stone sides and quays. The crossing on the gate road is a drawbridge (CROSSING).
# North: mead hall facing the plaza and the drawbridge, eight ornate houses in two blocks of four.
# South: kitchen and food store, blacksmith and smelting yard, barn and four animal pens. Outside the south gate:
# two fenced fields either side of the road. Standing iron torches along the streets, quays and gates; dirt under
# every building and pen.
import json, math, os
from kit import Plan, Line, moved, spans, stone_run, curtain, wall_stairs, building, tower, fence_rect, WALK_IN
import village_buildings as vb
from furnish import on

HERE = os.path.dirname(os.path.abspath(__file__))
XW, ZW = 64, 53                    # wall lines
GATE = (-3, 3)                     # main gate opening in the south wall: three wood gates
NGATE = (-3, 3)                    # the back gate in the north wall, behind the mead hall (the user asked for it)
WATER = (-8, 8)                    # water gate openings in the west and east walls, open to the sky
CW = 7.5                           # half the canal's water width
QUAY = (8.5, 12.5)                 # quays beside the canal, against the canal walls
CROSSING = "drawbridge"            # the user's choice: a drawbridge on the gate road ("footbridge" would block boats)
CORNER = 5                         # corner tower half size
FLOOR = -5                         # canal floor below the town ground: town 32.5 m keeps 2.5 m of water (sea level 30)

HOUSES = [(-46, 40), (-28, 40), (-46, 21), (-28, 21), (28, 40), (46, 40), (28, 21), (46, 21)]
PENS = [(37, -21, 0), (51, -21, 0), (37, -38, 180), (51, -38, 180)]
FIELDS = [(-33, -80, 270), (33, -80, 90)]          # 54 x 40 m each, the gate toward the road


def walls():
    p = Plan()
    south, north = Line(0, -ZW, "+x", "-z"), Line(0, ZW, "+x", "+z")
    west, east = Line(-XW, 0, "+z", "-x"), Line(XW, 0, "+z", "+x")
    ex, ez = XW - CORNER, ZW - CORNER                                 # where the corner towers start
    curtain(p, south, -ex, GATE[0] - 6)
    curtain(p, south, *GATE, stone=False, posts="ends")
    curtain(p, south, GATE[1] + 6, ex)
    curtain(p, north, -ex, NGATE[0] - 6)
    curtain(p, north, *NGATE, stone=False, posts="ends")
    curtain(p, north, NGATE[1] + 6, ex)
    for side in (west, east):
        curtain(p, side, -ez, WATER[0] - 6)
        curtain(p, side, WATER[1] + 6, ez)
    for u in spans(*GATE):
        south.put(p, 10, "wood_gate", u, 1.05)
    for u in spans(*NGATE):
        north.put(p, 10, "wood_gate", u, 1.05)
    wall_stairs(p, west, 17)
    wall_stairs(p, east, 17)
    wall_stairs(p, north, 10)
    wall_stairs(p, south, 11)
    return p.pieces()


def towers():
    inner_x, inner_z = XW - WALK_IN, ZW - WALK_IN
    walks = [("x", inner_z, ZW - 0.5), ("x", -ZW + 0.5, -inner_z), ("z", inner_x, XW - 0.5), ("z", -XW + 0.5, -inner_x)]
    out = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            out += tower(sx * XW, sz * ZW, CORNER, walks, "z" if sx * sz > 0 else "x").pieces()
    for cx in (GATE[0] - 3, GATE[1] + 3):
        out += tower(cx, -ZW + 1.5, 3, walks, "x").pieces()
    for cx in (NGATE[0] - 3, NGATE[1] + 3):
        out += tower(cx, ZW - 1.5, 3, walks, "x").pieces()
    for x in (XW - 1.5, -XW + 1.5):                                   # water-gate towers, shut toward the canal
        out += tower(x, WATER[1] + 3, 3, walks, "z", closed=("S",)).pieces()
        out += tower(x, WATER[0] - 3, 3, walks, "z", closed=("N",)).pieces()
    return out


def canal():
    p = Plan()
    reach = XW + 6                                                    # canal walls run 6 m past the water gates
    for zs in (-1, 1):                                                # sides, y -5..0, inner face on the water
        stone_run(p, 0, Line(0, zs * (CW + 0.5), "+x", "+z" if zs > 0 else "-z"), -reach, reach, FLOOR,
                  (-FLOOR - 1) // 2, True)
    for x in spans(-(XW - WALK_IN - 1), XW - WALK_IN - 1):
        for z in spans(*QUAY):
            for zs in (-1, 1):
                p.add(1, "stone_floor_2x2", x, -0.4, zs * z)          # quays, top 0.1
    for x in range(-52, 53, 8):
        if abs(x) > 4:                                                # leave the drawbridge landing clear
            for z in (-(CW + 0.5), CW + 0.5):
                p.add(2, "wood_pole_log", x, 0.81, z)                 # mooring posts on the canal wall
    if CROSSING == "footbridge":
        footbridge(p, 0)
    elif CROSSING == "drawbridge":
        drawbridge(p)
    return p.pieces()


def drawbridge(p):
    """Two Timberwood drawbridge leaves hinged on the quays, each 10.06 m of deck when lowered (along its local +x),
    meeting over the middle of the canal; raised they stand 79 degrees up beside the water, clear of any mast. The
    lowered deck top is 0.69 above the pivot, so the pivot sits 0.59 below the quay top."""
    for zs, yaw in ((-1, 270), (1, 90)):
        p.add(5, "piece_drawbridge", 0, 0.1 - 0.69, zs * 9.6, yaw)


def footbridge(p, bx):
    """A 4 m wide wooden bridge across the canal at x = bx, on log piers from the canal floor, with rails."""
    for x in (bx - 1, bx + 1):
        for z in spans(-9, 9):
            p.add(2, "wood_floor", x, 0.1, z)
        for z in (-4, 0, 4):
            p.add(1, "wood_pole_log_4", x, -2.8, z)
            p.add(1, "wood_pole_log", x, -1.2, z)
    for x in (bx - 1.9, bx + 1.9):
        for z in range(-9, 10, 2):
            p.add(3, "wood_pole", x, 0.68, z)
        for z in spans(-9, 9):
            p.add(4, "wood_beam", x, 1.18, z, 90)


def fields():
    """Two fenced fields outside the south wall, 54 x 40 m, each with its gate toward the road."""
    out = []
    for cx, cz, rot in FIELDS:
        p = Plan()
        fence_rect(p, -20, -27, 20, 27, gate_at=1)
        out += moved(p.pieces(), cx, cz, rot)
    return out


HALL_POSTS = [(n, sx, y, z, 0) for sx in (-3, 3) for z in (-10, -6, -2, 2, 6, 10)
              for n, y in (("wood_pole_log_4", 2.0), ("wood_pole_log", 4.6))]       # top 5.7, under the thatch


def designs():
    """The first compound's buildings, kept as blueprints of their own."""
    return {
        "great_hall": (building(10, 26, 4, {"F": "SWSWS", "K": "SWSWS", "Wst": "SWSWSWSWSWSWS", "E": "SWSWSWDWSWSWS"},
                                vents=(-1, 1), inside=[("hearth", 0, 0.13, 0, 90)] + HALL_POSTS),
                       "Great hall 10 x 26 m, 4 m walls, door in the middle of the long side (local +x), hearth "
                       "under a capped ridge vent, two rows of log posts."),
        "longhouse": (building(10, 12, 4, {"F": "SWDWS", "K": "SSWSS", "Wst": "SWSSWS", "E": "SWSSWS"}),
                      "Longhouse 10 x 12 m, 4 m walls, Wood Gate in the front gable, crossed gable horns."),
        "smithy": (building(10, 12, 4, {"F": "OOOOO", "K": "SSSSS", "Wst": "SOOOOS", "E": "SOOOOS"}, floor="stone"),
                   "Open smithy 10 x 12 m on a stone floor: open front and sides, solid back."),
        "barn": (building(10, 10, 4, {"F": "SSDSS", "K": "SSSSS", "Wst": "SWSWS", "E": "SWSWS"}),
                 "Barn 10 x 10 m, 4 m walls, Wood Gate in the front gable."),
    }


VILLAGE = {                                                           # name: (generator, x, z, rot)
    "mead_hall": (vb.mead_hall, 0, 33, 90),                           # door (local +x) toward the plaza
    "kitchen": (vb.kitchen, -15, -21, 270),                           # open front toward the road
    "food_store": (vb.food_store, -15, -38, 270),
    "blacksmith": (vb.blacksmith, -45, -22, 270),
    "smelting_yard": (vb.smelting_yard, -45, -40, 270),
    "big_barn": (vb.barn, 19, -30, 90),                               # gates toward the road
}


def torches():
    """Standing iron torches: both sides of the gate road, along both quays, at the plaza corners, at both gates
    (inside the back gate, outside both), in the side streets and between the houses; each spot clear of every piece."""
    spots = [(sx * 3.8, z, 0) for z in (-47, -39, -31, -23, -15) for sx in (-1, 1)]
    spots += [(sx * x, sz * 11.5, 0.1) for x in (8, 24, 40, 56) for sx in (-1, 1) for sz in (-1, 1)]    # quay tops
    spots += [(sx * 10, z, 0) for z in (14.5, 22) for sx in (-1, 1)]
    spots += [(sx * 4.5, z, 0) for z in (46.5, 56.5, -56.5) for sx in (-1, 1)]
    spots += [(-29, z, 0) for z in (-18, -30, -42)] + [(29.5, z, 0) for z in (-20, -44)]
    spots += [(sx * x, 30.5, 0) for x in (18.5, 37) for sx in (-1, 1)] + [(sx * 18.5, 45, 0) for sx in (-1, 1)]
    return [on("piece_groundtorch", x, y, z) for x, z, y in spots]


def footprints():
    """Each building's and pen's floor plan in the frame, from its pieces' positions: (x0, x1, z0, z1)."""
    out = []
    for fn, x, z, rot in (list(VILLAGE.values()) + [(vb.ornate_house, hx, hz, 0) for hx, hz in HOUSES]
                          + [(vb.pen, px, pz, prot) for px, pz, prot in PENS]):
        ps = moved(fn().pieces(), x, z, rot)
        out.append((min(p[1] for p in ps), max(p[1] for p in ps), min(p[3] for p in ps), max(p[3] for p in ps)))
    return out


def dirt():
    """Dirt paint under every building and pen (the user asked for it): squares as wide as the plan is narrow, side
    by side along its long side."""
    steps = []
    for x0, x1, z0, z1 in footprints():
        w, d = x1 - x0, z1 - z0
        n = math.ceil(max(w, d) / min(w, d))
        for i in range(n):
            f = (i + 0.5) / n
            at = [x0 + w * f, (z0 + z1) / 2] if w >= d else [(x0 + x1) / 2, z0 + d * f]
            steps.append({"op": "paint", "at": [round(v, 2) for v in at], "half": round(min(w, d) / 2, 2),
                          "paint": "dirt"})
    return steps


def village():
    out = walls() + towers() + canal() + fields()
    for name, (fn, x, z, rot) in VILLAGE.items():
        out += moved(fn().pieces(), x, z, rot)
    house = vb.ornate_house().pieces()
    for x, z in HOUSES:
        out += moved(house, x, z, 0)                                  # porches toward the canal side
    pen = vb.pen().pieces()
    for x, z, rot in PENS:
        out += moved(pen, x, z, rot)
    return out + torches()


def terrain():
    level = [{"op": "level", "at": [x, z], "half": 30, "y": 0} for x in (-46, 0, 46) for z in (-28, 28)]
    canal_dig = [{"op": "level", "at": [x, 0], "half": CW, "y": FLOOR} for x in range(-75, 76, 15)]
    field = [{"op": op, "at": [x, -80], "half": 20, **kw} for x in (-40, -26, 26, 40)
             for op, kw in (("level", {"y": 0}), ("paint", {"paint": "cultivated"}))]
    road = [{"op": "paint", "at": [0, z], "half": 3, "paint": "paved"} for z in range(-99, -14, 6)]
    return level + canal_dig + field + road + dirt()


SITE = {"clear": "per_level", "clear_radius": 30, "stand": [0, -58], "water": {"floor": FLOOR, "depth": 2.5},
        "terrain": terrain()}


def write(name, desc, site, pieces):
    head = json.dumps({"name": name, "description": desc, "source": "compound.py", "site": site}, indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in pieces)
    path = os.path.join(HERE, name + ".json")
    with open(path, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
    print(f"wrote {path}: {len(pieces)} pieces")


def single(name, pieces, desc, clear=14):
    far = max(abs(p[3]) for p in pieces)
    write(name, desc, {"level_half": max(abs(p[1]) for p in pieces if p[2] < 1) + 0.5, "clear_radius": clear,
                       "stand": [0, -(far + 3)]}, pieces)


def main():
    for name, (plan, desc) in designs().items():
        single(name, plan.pieces(), desc)
    for name, (fn, _, _, _) in list(VILLAGE.items()) + [("ornate_house", (vb.ornate_house, 0, 0, 0)),
                                                        ("pen", (vb.pen, 0, 0, 0))]:
        single(name, fn().pieces(), fn.__doc__.split("\n")[0].strip())
    write("compound", "Walled village 128 x 106 m: a 15 m boat canal straight through it between water gates open to "
          "the sky, a drawbridge on the gate road, wall walk and ten towers; mead hall, eight ornate two-storey houses, "
          "kitchen and food store, blacksmith and smelting yard, barn, four animal pens; two fenced fields outside the "
          "south gate. Pick ground 30.5 to 32.5 m (sea level 30) with open water at both canal ends.", SITE, village())


if __name__ == "__main__":
    main()

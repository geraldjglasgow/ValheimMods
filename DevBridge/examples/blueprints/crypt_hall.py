# Design source of crypt_hall.json: a torchlit burial hall, a film set for the Elite Creatures Pack trailer
# (2026-10-09). Run it to write the JSON again; python check.py crypt_hall --before fader checks it (skull piles are
# Ashlands pieces); python set_preview.py crypt_hall --open 7.4 shows the inside.
#
# Frame: door on -z, y from the levelled ground; the stone floor's top is 0.1 (clear of the terrain, like the village's
# stone floors). Interior 20 x 36 m (x -10..10, z -18..18), 1 m stone walls, a flat stone ceiling 8 m up with core-wood
# logs across it.
#
#   vestibule   z -25..-19: a 4 m wide, 5 m high stone passage with twin arches at its mouth; the march starts here
#   entrance    a 4 m wide door in the front wall (twin arches 4 m up)
#   arcade      two rows of stone pillars at x +-6 every 3 m (z -15..12) with an arch between each pair: the aisle
#               between the pillar faces is 10.9 m wide and nothing stands in it
#   fight floor x -5..5, z 0..10: kept empty; two hanging braziers 6 m above it
#   dais        x -5..5, z 12..18, 1 m high, a stone stair across its whole front; stone throne, braziers, treasure
#   gallery     a stone ledge 3 m up in the east arcade bay at z -4.5, reaching 1.5 m into the aisle, with a stone
#               stair up the east wall to it: the crossbowman's post
#   side aisles x +-(6.6..10): tombs (stone blocks), iron cages with skulls, bone heaps, banners, sconces
#   light       fire only, in pools along the aisle (vestibule, door, braziers at z -10.5, the gallery, the fight
#               floor, the dais) with dark bays between; sconces on the side walls light the tombs
import json, os
from kit import Plan, Line, stone_run
from furnish import on, hung
import support

HERE = os.path.dirname(os.path.abspath(__file__))
HW, HL, H = 10, 18, 8                      # interior half width, half length, ceiling underside
PX = 6                                     # pillar rows at x = +-PX
PZ = list(range(-15, 13, 3))               # pillar centres along z
FLOOR = 0.1                                # floor top (the dais top is FLOOR + 1)
LEDGE_Z, LEDGE_Y = -4.5, FLOOR + 3.0       # the crossbowman's ledge: bay centre and floor top
SKULLS = "skull_pile"                      # Ashlands; "bone_stack" keeps every piece before Moder
FRONT_YAW = {"+z": 0, "+x": 90, "-z": 180, "-x": 270}


def mount(n, face, at, bottom, normal, yaw=None):
    """A piece hung flat on a wall or pillar face: its box's back 2 cm into the face, its bottom at `bottom`. face is
    the face's x (normal +-x) or z (normal +-z), `at` the position along the face; normal points into the room. The
    piece's front is its local +z unless a yaw is given (banners: 0 on x faces, 90 on z faces)."""
    yaw = FRONT_YAW[normal] if yaw is None else yaw
    axis, sign = normal[1], 1 if normal[0] == "+" else -1
    x, z = (face, at) if axis == "x" else (at, face)
    y = round(bottom + support.CAT[n]["pivot_above_bottom"], 3)
    b = support.box([n, x, y, z, yaw])
    lo, hi = (b[0], b[1]) if axis == "x" else (b[4], b[5])
    shift = (face - sign * 0.02) - (lo if sign > 0 else hi)
    x, z = (x + shift, z) if axis == "x" else (x, z + shift)
    return (n, round(x, 3), y, round(z, 3), yaw)


def side(sx, z, n, bottom, yaw=None):
    """A piece on the inner face of the west (sx -1) or east (sx 1) wall."""
    return mount(n, sx * HW, z, bottom, "+x" if sx < 0 else "-x", yaw)


def pillar_face(sx, z, n, bottom):
    """A piece on the aisle face of the pillar at (sx * PX, z)."""
    return mount(n, sx * (PX - 0.57), z, bottom, "-x" if sx > 0 else "+x")


# ---------------------------------------------------------------- shell
def walls(p):
    """Side and back walls from y -1 to 8, the front wall with the 4 m door, the floor and the door threshold."""
    for line, u1 in ((Line(-HW - 0.5, -HL - 1, "+z", "-x"), 2 * HL + 2),
                     (Line(HW + 0.5, -HL - 1, "+z", "+x"), 2 * HL + 2),
                     (Line(-HW, HL + 0.5, "+x", "+z"), 2 * HW)):
        stone_run(p, 0, line, 0, u1, -1, rows=4)
    front = Line(-HW, -HL - 0.5, "+x", "-z")
    stone_run(p, 0, front, 0, 8, -1, rows=4)
    stone_run(p, 0, front, 12, 20, -1, rows=4)
    for u in (9, 11):
        front.put(p, 0, "stone_arch", u, 4.53)                          # twin arches over the door
        front.put(p, 0, "stone_wall_2x1", u, H - 0.5)
        p.add(0, "stone_wall_2x1", u - 10, FLOOR - 0.5, -HL - 0.5)       # threshold
    front.put(p, 0, "stone_wall_4x2", 10, 6)
    for x in range(-HW + 1, HW, 2):
        for z in range(-HL + 1, HL, 2):
            p.add(0, "stone_floor_2x2", x, FLOOR - 0.5, z)


def vestibule(p):
    """A 4 m wide, 5 m high passage in front of the door, roofed, twin arches at its open mouth."""
    for line in (Line(-2.5, -HL - 7, "+z", "-x"), Line(2.5, -HL - 7, "+z", "+x")):
        stone_run(p, 0, line, 0, 6, -1, rows=3, top=False)
    for z in (-HL - 6, -HL - 4, -HL - 2):
        for x in (-1, 1):
            p.add(0, "stone_floor_2x2", x, FLOOR - 0.5, z)
        for x in (-2, 0, 2):
            p.add(0, "stone_floor_2x2", x, 5.5, z)
    for x in (-1, 1):
        p.add(0, "stone_arch", x, 4.53, -HL - 6.5)


def arcade(p):
    """Pillars 0..8 m at x +-PX, an arch 6..7 m between each pair and a 2x1 block over it up to the ceiling."""
    for sx in (-1, 1):
        for z in PZ:
            for y in (1, 3, 5, 7):
                p.add(0, "stone_pillar", sx * PX, y, z)
        for z in PZ[:-1]:
            p.add(0, "stone_arch", sx * PX, 6.53, z + 1.5, 90)
            p.add(0, "stone_wall_2x1", sx * PX, 7.5, z + 1.5, 90)


def ceiling(p):
    """Stone floor tiles 8..9 m over the whole hall, laid from the walls and the arcade inward; core-wood logs across
    it on every second pillar line."""
    def reach(t):
        x, z = t
        d = min(HW - abs(x), HL - abs(z))
        return min(d, abs(abs(x) - PX)) if PZ[0] - 1 <= z <= PZ[-1] + 1 else d
    tiles = [(x, z) for x in range(-HW, HW + 1, 2) for z in range(-HL, HL + 1, 2)]
    for x, z in sorted(tiles, key=reach):
        p.add(Plan.KEEP, "stone_floor_2x2", x, H + 0.5, z)
    for z in PZ[::2]:
        for x in (-8, -4, 0, 4, 8):
            p.add(Plan.KEEP + 1, "wood_wall_log_4x0.5", x, H - 0.265, z)


def dais(p):
    """1 m high, x -5..5, z 12..18, with a stone stair across the whole front (z 10..12)."""
    for x in (-4, -2, 0, 2, 4):
        for z in (13, 15, 17):
            p.add(0, "stone_floor_2x2", x, FLOOR + 0.5, z)
        p.add(0, "stone_stair", x, FLOOR, 11, 180)                          # rises toward +z onto the dais


def ledge(p):
    """The crossbowman's post: a stone stair up the east wall (z -11.5..-5.5) onto a ledge 3 m up that crosses the
    arcade bay at z -4.5 and reaches 1.5 m into the aisle (x 4..10)."""
    for k, z in enumerate((-10.5, -8.5, -6.5)):
        p.add(0, "stone_stair", 9, FLOOR + k, z, 180)
        for y in range(k):
            p.add(0, "stone_floor_2x2", 9, FLOOR + y + 0.5, z)
    for x in (9, 7, 5):
        p.add(0, "stone_floor_2x2", x, LEDGE_Y - 0.5, LEDGE_Z)


# ---------------------------------------------------------------- side aisles: tombs, cages, bones (x +-8..10)
WEST = [(-16.5, "cage"), (-13.5, "tomb"), (-10.5, "bones"), (-7.5, "tomb"), (-4.5, "tomb"), (-1.5, "cage"),
        (1.5, "tomb"), (4.5, "bones"), (7.5, "tomb"), (10.5, "tomb"), (14.5, "tomb"), (16.8, "bones")]
EAST = [(-16.5, "bones"), (-13.5, "tomb"), (-4.5, "tomb"), (-1.5, "bones"), (1.5, "cage"), (4.5, "tomb"),
        (7.5, "tomb"), (10.5, "bones"), (14.5, "cage"), (17.0, "tomb")]                 # z -11.5..-5.5: the stair
CANDLES = {(-1, -13.5), (-1, 7.5), (-1, 14.5), (1, -13.5), (1, 4.5), (1, 17.0)}


def tomb(sx, z):
    """A stone tomb (a 2x1 block on the floor, head to the wall), a resin candle at its head on some."""
    out = [("stone_wall_2x1", sx * 9, FLOOR + 0.5, z, 0)]
    if (sx, z) in CANDLES:
        out.append(on("Candle_resin", sx * 9.6, FLOOR + 1.0, z))
    return out


def cage(sx, z):
    """A 2 x 2 iron cage against the wall with skulls in it."""
    return [("iron_wall_2x2", sx * 8, FLOOR + 1.09, z, 90), ("iron_wall_2x2", sx * 9, FLOOR + 1.09, z - 1, 0),
            ("iron_wall_2x2", sx * 9, FLOOR + 1.09, z + 1, 0), ("iron_floor_2x2", sx * 9, FLOOR + 2.19, z, 0),
            on(SKULLS, sx * 9.1, FLOOR, z)]


def bones(sx, z):
    return [on(SKULLS, sx * 9.2, FLOOR, z - 0.6, 30), on("bone_stack", sx * 9.0, FLOOR, z + 0.6, 75)]


def side_props():
    kinds = {"tomb": tomb, "cage": cage, "bones": bones}
    return [q for sx, bays in ((-1, WEST), (1, EAST)) for z, k in bays for q in kinds[k](sx, z)]


# ---------------------------------------------------------------- the dais and the hangings
def dais_props():
    """Stone throne facing the hall, coin pile and treasure chest beside it, skulls in the back corners; red banners
    behind the throne, black ones beside them and either side of the door."""
    top = FLOOR + 1.0
    out = [on("piece_throne02", 0, top, HL - 0.68, 180), on("treasure_pile", -3.4, top, 15.5),
           on("piece_chest_treasure", 3.4, top, 16.2, 200), on(SKULLS, -4.3, top, 17.3, 15),
           on(SKULLS, 4.3, top, 17.3, 160)]
    out += [mount(n, HL, x, 2.6, "-z", 90) for n, x in (("piece_banner04", -1.6), ("piece_banner04", 1.6),
                                                       ("piece_banner01", -4.2), ("piece_banner01", 4.2))]
    out += [mount("piece_banner01", -HL, x, 3.4, "+z", 90) for x in (-6, 6)]
    return out


def side_banners():
    """Black and red banners high on the side walls, over the dark bays."""
    rows = ((-1, -13.5, "piece_banner01"), (-1, -1.5, "piece_banner04"), (-1, 7.5, "piece_banner01"),
            (1, -16.5, "piece_banner01"), (1, 1.5, "piece_banner04"), (1, 7.5, "piece_banner01"))
    return [side(sx, z, n, 3.4, 0) for sx, z, n in rows]


# ---------------------------------------------------------------- fire
def lights():
    """Pools along the aisle, dark bays between: vestibule, door, standing braziers at z -10.5, two sconces at the
    ledge, the fight floor (pillar sconces at z 3 and 9, two hanging braziers, iron torches by the stair), the dais
    (four braziers). Side-wall sconces light the tombs."""
    out = [mount("piece_walltorch", sx * 2, -21.5, 2.2, "-x" if sx > 0 else "+x") for sx in (-1, 1)]
    out += [mount("piece_walltorch", -HL, sx * 3.2, 2.4, "+z") for sx in (-1, 1)]
    out += [on("piece_brazierfloor01", sx * 4.6, FLOOR, -10.5) for sx in (-1, 1)]
    out += [pillar_face(1, z, "piece_walltorch", LEDGE_Y + 0.5) for z in (LEDGE_Z - 1.5, LEDGE_Z + 1.5)]
    out += [pillar_face(sx, z, "piece_walltorch", 2.4) for sx in (-1, 1) for z in (3, 9)]
    out += [hung("piece_brazierceiling01", sx * 3, H - 1.93, 6) for sx in (-1, 1)]
    out += [on("piece_groundtorch", sx * 5.75, FLOOR, 10.5) for sx in (-1, 1)]
    out += [on("piece_brazierfloor01", sx * 4.2, FLOOR + 1.0, 12.7) for sx in (-1, 1)]
    out += [on("piece_brazierfloor01", sx * 1.8, FLOOR + 1.0, 16.6) for sx in (-1, 1)]
    walls_at = ((-1, -10.5), (1, -12.4), (-1, -4.5), (-1, 4.5), (1, 4.5), (-1, 15.6), (1, 15.6))
    out += [side(sx, z, "piece_walltorch", 2.4) for sx, z in walls_at]
    return out


# ---------------------------------------------------------------- marks for the director (blueprint frame, y = height)
MARKS = {
    "entrance": [0, FLOOR, -HL + 1],
    "march_start": [0, FLOOR, -HL - 5],
    "march_end": [0, FLOOR, -HL + 10],
    "fight_center": [0, FLOOR, 5],
    "dais_center": [0, FLOOR + 1.0, 15],
    "crossbow_ledge": [4.8, LEDGE_Y, LEDGE_Z],
    "camera_low": [1.6, FLOOR + 0.6, -HL + 1.6],
}
AIMS = {"camera_low": [0, FLOOR + 1.5, 6], "crossbow_ledge": [0, FLOOR + 1.0, 5]}
NOTES = {
    "entrance": "inside the door, aisle centre; the door is 4 m wide (x -2..2) and 4 m high",
    "_frame": "points in the blueprint frame like the pieces: y from the levelled ground, the floor top is 0.1",
    "march_start": "in the vestibule, 5 m outside the door; the march runs +z down the aisle",
    "march_end": "10 m inside the door; the aisle is clear for 10.9 m between the pillar faces",
    "fight_center": "centre of the empty fight floor x -5..5, z 0..10 (the dais stair starts at z 10)",
    "dais_center": "on the dais top (1 m above the floor); the throne is at its back, z 17.3",
    "crossbow_ledge": "front of the ledge 3 m above the floor (x 4..10, z -5.5..-3.5), clear of the pillars; aim: the "
                      "fight",
    "camera_low": "low near the entrance, looking down the aisle at its aim",
}


def crypt_hall():
    p = Plan()
    for part in (walls, vestibule, arcade, ceiling, dais, ledge):
        part(p)
    p.extend(Plan.KEEP + 10, side_props() + dais_props() + side_banners() + lights())
    return p.pieces()


def rounded(points):
    return {k: [round(c, 3) for c in v] for k, v in points.items()}


def write(name, desc, site, pieces):
    head = json.dumps({"name": name, "description": desc, "source": os.path.basename(__file__), "site": site,
                       "marks": rounded(MARKS), "aims": rounded(AIMS), "mark_notes": NOTES}, indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in pieces)
    path = os.path.join(HERE, name + ".json")
    with open(path, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
    print(f"wrote {path}: {len(pieces)} pieces")


LEVEL, PAINT = {"op": "level", "half": 13, "y": 0}, {"op": "paint", "half": 13, "paint": "dirt"}
SITE = {"terrain": [dict(step, at=[0, z]) for step in (LEVEL, PAINT) for z in (-12.5, 7.5)],
        "clear_radius": 28, "stand": [0, -28.5], "limits": "Ravenholt"}

if __name__ == "__main__":
    write("crypt_hall", "Film set: a dark stone burial hall, interior 20 x 36 m, 8 m ceiling, lit only by fire. A "
          "roofed vestibule and a 4 m door at the front (-z), two rows of pillars with arches leaving a 10.9 m aisle, "
          "an empty 10 x 10 m fight floor before a 1 m dais with a stone stair, throne, braziers and treasure; tombs, "
          "iron cages, skulls, bones, banners and sconces along the side walls; a crossbowman's ledge 3 m up on the "
          "east side with a stair. Director marks in 'marks'. Check the fires are lit (fuelled) before a take.",
          SITE, crypt_hall())

# Design source of ship_hold.json: the dark hold of a ship, a film set for the Elite Creatures Pack trailer's gear
# montage (2026-10-09), built on the ground: it only has to look like a hold from inside. Run it to write the JSON
# again; python check.py ship_hold checks it; python set_preview.py ship_hold --open 3.45 shows the inside.
#
# Frame: the near end (where the camera comes down) on -z, y from the levelled ground; the wood floor's top is 0.13
# (like plain_wood_house, clear of the terrain). Interior 8 x 16 m (x -4..4, z -8..8), wood walls 3 m with a log
# strake on top, the deck (wood floor tiles) 3.5 m up on beams across every 2 m, ribs (posts) up the walls at every
# beam, knees from the ribs up to every second beam.
#
#   near end    a 2 x 2 hatch in the deck (x -1..1, z -6..-4) with a ladder up through its west half
#   walls       weapon racks (item stands) on both side walls round the middle and on the far wall; a table with
#               horizontal item stands each side; barrels, chests, metal bars along the walls
#   middle      the parry spot, 4 x 4 m, empty
#   far end     the ballista spot, 3 x 3 m, empty, against the far wall
#   light       two hanging braziers from the deck beams (near east, far west), sconces on the ribs at z 0 and 6,
#               candles on the tables; daylight only through the hatch
import json, os
from kit import Plan
from furnish import on, hung
import support

HERE = os.path.dirname(os.path.abspath(__file__))
HW, HL = 4, 8                         # interior half width and half length (wall lines)
FLOOR = 0.13                          # floor top
WALL_TOP, DECK = 3.0, 3.5             # wall top and deck underside (the deck top is 3.72)
FACE = HW - 0.215                     # inner face of the side walls (wood walls 0.43 thick)
RIB = FACE - 0.2                      # rib (post) centres; their inner face is at RIB - 0.2
RIBS = range(-6, 7, 2)                # rib and deck beam lines along z
HATCH_Z = (-6, -4)                    # the hatch: x -1..1 between these
FRONT_YAW = {"+z": 0, "+x": 90, "-z": 180, "-x": 270}


def mount(n, face, at, bottom, normal):
    """A piece hung on a wall or post face (its local +z out): its back 2 cm into the face, its bottom at `bottom`.
    face: the face's x (normal +-x) or z (normal +-z); at: the position along it; normal points into the room."""
    yaw, axis, sign = FRONT_YAW[normal], normal[1], 1 if normal[0] == "+" else -1
    x, z = (face, at) if axis == "x" else (at, face)
    y = round(bottom + support.CAT[n]["pivot_above_bottom"], 3)
    b = support.box([n, x, y, z, yaw])
    lo, hi = (b[0], b[1]) if axis == "x" else (b[4], b[5])
    shift = (face - sign * 0.02) - (lo if sign > 0 else hi)
    x, z = (x + shift, z) if axis == "x" else (x, z + shift)
    return (n, round(x, 3), y, round(z, 3), yaw)


# ---------------------------------------------------------------- hull
def shell(p):
    """Floor (top at FLOOR), walls from the ground to 3 m, a 4 m log strake on top of them to 3.5 m."""
    for x in (-3, -1, 1, 3):
        for z in range(-HL + 1, HL, 2):
            p.add(0, "wood_floor", x, FLOOR - 0.08, z)
    for sx in (-1, 1):
        for z in range(-HL + 1, HL, 2):
            p.add(0, "woodwall", sx * HW, 1.0, z, 90)
            p.add(0, "wood_wall_half", sx * HW, 2.5, z, 90)
        for x in (-3, -1, 1, 3):
            p.add(0, "woodwall", x, 1.0, sx * HL)
            p.add(0, "wood_wall_half", x, 2.5, sx * HL)
        for z in (-6, -2, 2, 6):
            p.add(0, "wood_wall_log_4x0.5", sx * HW, WALL_TOP + 0.24, z, 90)
        for x in (-2, 2):
            p.add(0, "wood_wall_log_4x0.5", x, WALL_TOP + 0.24, sx * HL)


def frame(p):
    """Ribs up the side walls at every beam line, deck beams across under the deck (the hatch left open, framed by
    two beams along z), knees from the ribs up to the beams on every second line."""
    for sx in (-1, 1):
        for z in RIBS:
            p.add(1, "wood_pole2", sx * RIB, 1.0, z)
            p.add(1, "wood_pole2", sx * RIB, 2.1, z)                     # up to the beam's underside (3.1)
    for z in RIBS:
        if z in HATCH_Z:
            xs = [(-3, "wood_beam"), (-1.5, "wood_beam_1"), (1.5, "wood_beam_1"), (3, "wood_beam")]
        else:
            xs = [(x, "wood_beam") for x in (-3, -1, 1, 3)]
        for x, n in xs:
            p.add(2, n, x, DECK - 0.2, z)
    for sx in (-1, 1):
        p.add(2, "wood_beam", sx * 1.2, DECK - 0.2, sum(HATCH_Z) / 2, 90)
        for z in (-6, -2, 2, 6):                                         # low end on the rib, high end at the beam
            p.add(3, "wood_beam_26", sx * (RIB - 0.2 - 1.0), 2.1, z, 0 if sx < 0 else 180)


def deck(p):
    """Wood floor tiles 3.5..3.72 (the 2 x 2 hatch left open, 1x1 tiles beside it), a log gunwale round the edge and
    a beam coaming round the hatch on top (open on its +z side, where the ladder comes up)."""
    cells = [(x, z) for x in (-3, 3, -1, 1) for z in range(-HL + 1, HL, 2)]
    for x, z in cells:
        if abs(x) == 1 and z == sum(HATCH_Z) / 2:
            for dz in (-0.5, 0.5):
                p.add(Plan.KEEP, "wood_floor_1x1", x * 1.5, DECK + 0.14, z + dz)
        else:
            p.add(Plan.KEEP, "wood_floor", x, DECK + 0.14, z)
    top = DECK + 0.22
    for sx in (-1, 1):
        for z in (-6, -2, 2, 6):
            p.add(Plan.KEEP + 1, "wood_wall_log_4x0.5", sx * HW, top + 0.27, z, 90)
        for x in (-2, 2):
            p.add(Plan.KEEP + 1, "wood_wall_log_4x0.5", x, top + 0.27, sx * HL)
        p.add(Plan.KEEP + 1, "wood_beam", sx * 1.2, top + 0.2, sum(HATCH_Z) / 2, 90)
    p.add(Plan.KEEP + 1, "wood_beam", 0, top + 0.2, HATCH_Z[0] - 0.2)


def ladder(p):
    """A ladder from the near end wall up through the hatch's west half (x -1.07..-0.03), rising toward +z: 2 m a
    piece, it passes the deck at z -4."""
    for k in range(2):
        p.add(Plan.KEEP + 2, "wood_stepladder", -0.55, FLOOR + 2 * k, -HL + 1.3 + 2 * k, 180)


# ---------------------------------------------------------------- cargo, racks and fire
def side(sx, z, n, bottom):
    """A piece on the inner face of the west (sx -1) or east (sx 1) wall."""
    return mount(n, sx * FACE, z, bottom, "+x" if sx < 0 else "-x")


def rib(sx, z, n, bottom):
    """A piece on the room face of the rib at (sx * RIB, z)."""
    return mount(n, sx * (RIB - 0.2), z, bottom, "+x" if sx < 0 else "-x")


def cargo():
    """Along the walls, clear of the ribs (x from 3.36 inward), the aisle (x -1.5..1.5), the parry and ballista spots:
    barrels in the corners, a reinforced chest in the far west corner, a table for two horizontal stands on each
    side (west near, east far), metal bars under the hanging braziers."""
    out = [on("piece_chest_barrel", x, FLOOR, z) for x, z in ((-2.95, -7.3), (-2.15, -7.3), (-2.95, -6.5),
                                                          (2.95, -7.3), (2.95, -6.5), (2.15, -7.3),
                                                          (-2.95, 6.35))]
    out += [on("piece_table", -2.74, FLOOR, -5.0, 90), on("piece_table", 2.74, FLOOR, 6.0, 90)]
    out += [on("bar_iron_stack", 2.95, FLOOR, -5.45), on("bar_bronze_stack", 2.95, FLOOR, -4.8),       # clear of the
            on("bar_iron_stack", -2.95, FLOOR, 4.85), on("bar_tin_stack", -2.95, FLOOR, 5.5),         # braziers
            on("piece_chest", -2.6, FLOOR, 7.25, 180)]
    out += [on("Candle_resin", -2.95, TABLE, -5.0), on("Candle_resin", 2.95, TABLE, 6.0)]
    return out


STANDS_V = [(-1, z) for z in (-3, -1, 1, 3)] + [(1, z) for z in (-3, -1, 1, 3)]   # vertical stands on the side walls
STANDS_FAR = (-2.4, 2.4)                                                   # on the far wall, beside the ballista
TABLE = FLOOR + 0.82                                                                # table top, 1 cm down
STANDS_H = [(-2.74, -5.6), (-2.74, -4.4), (2.74, 5.4), (2.74, 6.6)]                  # horizontal stands on the tables


def stands():
    """Item stands where the bone weapons will hang, in the order of their marks stand_1, stand_2, ..."""
    out = [side(sx, z, "itemstand", FLOOR + 1.3) for sx, z in STANDS_V]
    out += [mount("itemstand", HL - 0.215, x, FLOOR + 1.5, "-z") for x in STANDS_FAR]
    out += [on("itemstandh", x, TABLE, z) for x, z in STANDS_H]
    return out


def fire():
    """Two hanging braziers from deck beams (near east over the bars, far west over the bars), sconces on the ribs
    at z 0 (over the parry spot, lighting the racks) and z 6 (either side of the ballista)."""
    out = [hung("piece_brazierceiling01", x, DECK - 0.4 - 1.93, z) for x, z in ((2.6, -4), (-2.6, 4))]
    out += [rib(sx, z, "piece_walltorch", FLOOR + 1.8) for sx in (-1, 1) for z in (0, 6)]
    return out


# ---------------------------------------------------------------- marks for the director (blueprint frame, y = height)
def marks(placed):
    m = {f"stand_{i + 1}": [s[1], s[2], s[3]] for i, s in enumerate(placed)}
    m.update({"ballista": [0, FLOOR, 5.5], "camera_end": [0, FLOOR + 1.5, -0.5], "parry_spot": [0, FLOOR, 0],
              "deck_hatch": [0, DECK + 0.22, sum(HATCH_Z) / 2]})
    return m


AIMS = {"camera_end": [0, FLOOR + 0.8, 5.5]}
NOTES = {
    "_frame": "points in the blueprint frame like the pieces: y from the levelled ground, the floor top is 0.13",
    "stand_1..stand_8": "vertical item stands on the side walls (west z -3..3, then east), 1.3..1.8 above the "
                        "floor, facing the aisle",
    "stand_9..stand_10": "vertical item stands on the far wall either side of the ballista, facing -z",
    "stand_11..stand_14": "horizontal item stands on the two tables (west near end, east far end)",
    "ballista": "centre of the empty 3 x 3 m spot (x -1.5..1.5, z 4..7) against the far wall; the ballista faces -z",
    "camera_end": "6 m from the ballista, looking at it (aim)",
    "parry_spot": "centre of the empty 4 x 4 m floor mid-hold (x -2..2, z -2..2)",
    "deck_hatch": "centre of the 2 x 2 hatch on the deck top (x -1..1, z -6..-4); the ladder fills its west half, so "
                  "come down through x 0.2..0.9",
}


def ship_hold():
    p = Plan()
    for part in (shell, frame, deck, ladder):
        part(p)
    placed = stands()
    p.extend(Plan.KEEP + 10, cargo() + placed + fire())
    return p.pieces(), marks(placed)


def rounded(points):
    return {k: [round(c, 3) for c in v] for k, v in points.items()}


def write(name, desc, site, pieces, mk):
    head = json.dumps({"name": name, "description": desc, "source": os.path.basename(__file__), "site": site,
                       "marks": rounded(mk), "aims": rounded(AIMS), "mark_notes": NOTES}, indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in pieces)
    path = os.path.join(HERE, name + ".json")
    with open(path, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
    print(f"wrote {path}: {len(pieces)} pieces")


SITE = {"terrain": [{"op": "level", "at": [0, z], "half": 6, "y": 0} for z in (-4.5, 4.5)],
        "clear_radius": 12, "stand": [0, -11], "limits": "Ravenholt"}

if __name__ == "__main__":
    pieces, mk = ship_hold()
    write("ship_hold", "Film set: the dark hold of a ship, built on the ground, interior 8 x 16 m. Wood walls with "
          "ribs and knees, a deck of wood floor tiles 3.5 m up on beams, a 2 x 2 hatch with a ladder at the near end "
          "(-z). Item stands for the bone weapons on both side walls, the far wall and two tables (marks stand_1..), "
          "barrels, a chest, metal bars; an empty 4 x 4 parry spot mid-hold and an empty 3 x 3 ballista spot at the far "
          "end. Two hanging braziers, four sconces, two candles: check they are lit (fuelled) before a take.",
          SITE, pieces, mk)

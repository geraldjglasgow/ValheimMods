# Design source of plain_wood_house.json: run it to write the blueprint again after a change.
# Two-storey plain wood house, 10 x 10 m (5 x 5 floor tiles), 3 m storeys, built 2026-10-05.
# Frame: x across the front, z from the front (-5, the door) to the back (+5), y above the levelled ground.
import json, os

O = 0.3                         # exterior timber offset from the wall line
FL0, FL1 = 0.05, 3.0            # floor pivots (top = pivot + 0.08)
PH = {}                         # phase -> pieces; phases are placed in order, lowest first within a phase


def add(ph, n, x, y, z, yaw=0):
    PH.setdefault(ph, []).append((n, x, y, z, yaw))


def wall_xy(side, a, off=0.0):
    """Point on a wall line: F/K (front/back, along x) or Wst/E (along z); a = position along it."""
    if side == "F": return a, -5 - off
    if side == "K": return a, 5 + off
    if side == "Wst": return -5 - off, a
    return 5 + off, a


def along(side, n, a, y, ph, off=0.0):
    x, z = wall_xy(side, a, off)
    add(ph, n, x, y, z, 0 if side in ("F", "K") else 90)


# ---------------------------------------------------------------- floors (fire pit and stairwell left open)
for i in range(-2, 3):
    for j in range(-2, 3):
        if (i, j) != (0, 0):
            add(0, "wood_floor", 2 * i, FL0, 2 * j)
        if (i, j) not in ((0, 0), (-2, -1), (-2, 0), (-2, 1)):
            add(2, "wood_floor", 2 * i, FL1, 2 * j)

# ---------------------------------------------------------------- walls: S solid, W window, D door; bays at -4..4
GROUND = {"F": "SWDWS", "K": "SWSWS", "Wst": "WSSSS", "E": "WSSSS"}
UPPER = {"F": "SWWWS", "K": "WSSSW", "Wst": "SWSWS", "E": "SWSWS"}
BAYS = [-4, -2, 0, 2, 4]


def shutters(side, a, yc, ph):
    """Two 1x1 shutters hinged at the bay's outer edges."""
    base = 270 if side in ("Wst", "E") else 0
    x, z = wall_xy(side, a - 1); add(ph, "wood_window", x, yc, z, base + 180)
    x, z = wall_xy(side, a + 1); add(ph, "wood_window", x, yc, z, base)


def brace(side, a, yc, ph):
    """Diagonal brace outside a corner bay, rising toward the wall's middle."""
    if side in ("F", "K"):
        yaw = 0 if a < 0 else 180
    else:
        yaw = 270 if a < 0 else 90
    x, z = wall_xy(side, a, O)
    add(ph, "wood_beam_45", x, yc, z, yaw)


def bay(side, a, k, y0, ph):
    if k == "S":
        along(side, "woodwall", a, y0 + 1, ph)
        along(side, "wood_wall_half", a, y0 + 2.5, ph)
        if abs(a) == 4: brace(side, a, y0 + 1, ph + 1)
    elif k == "W":
        along(side, "wood_wall_half", a, y0 + 0.5, ph)
        along(side, "wood_wall_half", a, y0 + 2.5, ph)
        shutters(side, a, y0 + 1.5, ph + 1)
        along(side, "wood_beam", a, y0 + 0.9, ph + 1, O)               # sill
        along(side, "wood_beam", a, y0 + 2.1, ph + 1, O)               # lintel
    elif k == "D":
        along(side, "wood_gate", a, 1.05, ph)                          # the 3 m Wood Gate


for kinds, y0, ph in ((GROUND, 0, 1), (UPPER, 3, 3)):
    for side, row in kinds.items():
        for a, k in zip(BAYS, row):
            bay(side, a, k, y0, ph)

# ---------------------------------------------------------------- exterior timber frame
posts = [(sx * (5 + O), sz * (5 + O)) for sx in (-1, 1) for sz in (-1, 1)]
posts += [wall_xy(side, a, O) for a in (-3, -1, 1, 3) for side in ("F", "K", "Wst", "E")]
for x, z in posts:
    for ph, y in ((1, 1), (2, 3), (3, 5)):
        add(ph, "wood_pole2", x, y, z)
for side in ("F", "K", "Wst", "E"):
    for a in BAYS:
        if not (side == "F" and a == 0):
            along(side, "wood_beam", a, 0.2, 0, O)                     # sill beam
        along(side, "wood_beam", a, 3.0, 2, O)                         # girt at the floor line
        if side in ("F", "K"):
            along(side, "wood_beam", a, 6.0, 4, O)                     # top plate under the gables

# ---------------------------------------------------------------- interior structure
for i in (-2, -1, 0, 1, 2):
    for z in (-3, -1, 1, 3):
        if not (i == -2 and z in (-1, 1, 3)):                          # stairwell headroom
            add(2, "wood_beam", 2 * i, FL1 - 0.34, z)                  # joists
for sx in (-1, 1):
    for sz in (-1, 1):
        add(1, "wood_pole2", sx, 1, sz)                                # posts around the fire
        add(1, "wood_pole", sx, 2.5, sz)
        add(3, "wood_pole", sx, 3.5, sz)                               # railing posts above
for x, z, yaw in ((0, -1, 0), (0, 1, 0), (-1, 0, 90), (1, 0, 90)):
    add(4, "wood_beam", x, 3.9, z, yaw)                                # rail round the opening over the fire
for k in range(3):                                                     # stairs up the west wall
    add(1, "wood_stair", -4, FL0 + 0.03 + k, -2 + 2 * k, 180)
    add(2, "wood_beam_26", -3, 1.0 + k, -2 + 2 * k, 270)               # handrail
add(1, "wood_pole", -3, 0.5, -3)                                       # newel
for z in (-3, -1, 1):
    add(3, "wood_pole", -3, 3.5, z)
add(4, "wood_beam", -3, 3.9, -2, 90)
add(4, "wood_beam", -3, 3.9, 0, 90)
add(4, "wood_beam", -4, 3.9, -3, 0)                                    # stairwell front rail

# ---------------------------------------------------------------- gables (the peak triangle stays open: smoke vent)
for side, sgn in (("F", -1), ("K", 1)):
    z = 5 * sgn
    add(5, "wood_wall_roof_45", -4, 6, z, 0)
    add(5, "wood_wall_roof_45", 4, 6, z, 180)
    for sx in (-1, 1):
        add(5, "woodwall", 2 * sx, 7, z, 0)
        add(6, "wood_wall_roof_45", 2 * sx, 8, z, 0 if sx < 0 else 180)
    add(5, "woodwall", 0, 7, z, 0)
    add(6, "woodwall", 0, 9, z, 0)
    zo = z + O * sgn
    for sx in (-3, 3):
        add(5, "wood_pole2", sx, 7, zo)
    for sx in (-1, 1):
        add(5, "wood_pole2", sx, 7, zo)
        add(6, "wood_pole2", sx, 9, zo)
    add(7, "wood_beam", 0, 10, zo, 0)                                  # collar under the vent

# ---------------------------------------------------------------- roof: ridge along z, 2 m eaves, 1 m gable overhang
ZS = (-5, -3, -1, 1, 3, 5)
for c in (4, 6, 2):                                                    # wall-top row first, then eaves, then upper
    for z in ZS:
        add(8, "wood_roof_45", -c, 11 - c, z, 270)
        add(8, "wood_roof_45", c, 11 - c, z, 90)
for z in ZS:
    if abs(z) != 1:
        add(9, "wood_roof_top_45", 0, 10, z, 90)
for sx in (-1, 1):
    for z in (-2, 0, 2):
        add(9, "wood_pole", sx, 10.5, z)                               # smoke louver posts
for z in (-3, 3):
    for x in (-2, 0, 2):
        add(9, "wood_beam", x, 8, z, 0)                                # collar ties
for z in (-1, 1):
    add(10, "wood_roof_top_45", 0, 11, z, 90)                          # raised cap over the fire

# ---------------------------------------------------------------- furniture
add(11, "piece_workbench", 4.1, 0.13, 2.5, 270)
add(11, "piece_workbench_ext2", 4.2, 0.13, -1.5, 270)                  # tanning rack
add(11, "piece_workbench_ext1", 0, 0.13, 4.3, 180)                     # chopping block
add(11, "portal_wood", 0, 3.08, 4.25, 180)                             # upstairs: 3.3 m tall
add(11, "fire_pit", 0, 0, 0, 0)
for yaw in (0, 60, 120):
    add(12, "piece_cookingstation", 0, 0, 0, yaw)
add(12, "piece_banner01", 0, 9.8, -5 - O, 90)                          # black banner under the front vent


def ordered():
    for ph in sorted(PH):
        items = PH[ph] if ph >= 8 else sorted(PH[ph], key=lambda p: p[2])
        for n, x, y, z, yaw in items:
            yield [n, round(x, 3), round(y, 3), round(z, 3), yaw % 360]


bp = {
    "name": "plain_wood_house",
    "description": "Two-storey plain wood house, 10 x 10 m, 3 m storeys, Wood Gate door. Ground floor: campfire with "
                   "three cooking stations under an opening to the roof, workbench, tanning rack, chopping block, stairs. "
                   "Upstairs: portal. Black banner on the front gable. Smoke leaves through a capped ridge vent.",
    "source": "plain_wood_house.py",
    "site": {"level_half": 5.5, "clear_radius": 9, "stand": [0, -9.5]},
    "pieces": list(ordered()),
}
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "plain_wood_house.json")
head = json.dumps({k: v for k, v in bp.items() if k != "pieces"}, indent=1)[:-2]
rows = ",\n  ".join(json.dumps(p) for p in bp["pieces"])                # one piece per line keeps diffs readable
with open(out, "w") as f:
    f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
print(f"wrote {out}: {len(bp['pieces'])} pieces")

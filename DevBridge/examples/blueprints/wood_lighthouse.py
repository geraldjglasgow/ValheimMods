# Design source of wood_lighthouse.json: a very simple tall lighthouse of basic wood pieces on 3 x 3 floor tiles
# (6 x 6 m). Plain wood walls 22 m high with a door, a stair winding up inside (parts.winding_stair), a floor at the
# top with a half-wall parapet, four posts carrying a small gabled roof, standing wood torches as the light. Only
# stands under Ravenholt's limits (wood loses 0.05 a metre there; under the game's own rules wood stops near 16 m).
import json, os
from kit import Plan, roof, moved
from furnish import on
import parts

HERE = os.path.dirname(os.path.abspath(__file__))
TOP = 22                                     # wall top; 2 more than a multiple of 4 for the winding stair


def wood_lighthouse():
    p = Plan()
    cells = [(x, z) for x in (-2, 0, 2) for z in (-2, 0, 2)]
    for x, z in cells:
        p.add(0, "wood_floor", x, 0.05, z)
    for y in range(1, TOP, 2):
        for u in (-2, 0, 2):
            p.add(0, "wood_door" if (y, u) == (1, 0) else "woodwall", u, y + (0.01 if (y, u) == (1, 0) else 0), -3)
            p.add(0, "woodwall", u, y, 3)
            p.add(0, "woodwall", -3, y, u, 90)
            p.add(0, "woodwall", 3, y, u, 90)
    parts.winding_stair(p, TOP)
    gf = TOP + 0.22
    for x, z in cells:
        if (x, z) not in parts.HATCH:
            p.add(2, "wood_floor", x, gf - 0.08, z)
    for u in (-2, 0, 2):
        for line in ((u, -3, 0), (u, 3, 0), (-3, u, 90), (3, u, 90)):
            p.add(3, "wood_wall_half", line[0], gf + 0.5, line[1], line[2])
    for sx in (-1, 1):
        for sz in (-1, 1):
            for y in (gf + 1.0, gf + 3.0):
                p.add(4, "wood_pole2", sx * 2.8, y, sz * 2.8)
    for u in (-2, 0, 2):
        for line in ((u, -2.8, 0), (u, 2.8, 0), (-2.8, u, 90), (2.8, u, 90)):
            p.add(5, "wood_beam", line[0], gf + 4.2, line[1], line[2])
    p.extend(Plan.KEEP, moved(roof(6, 6, 4, side_over=0, gable_over=(0, 0), horns=None).pieces(), 0, 0, 0, gf + 0.4))
    p.extend(Plan.KEEP + 10, [on("piece_groundtorch_wood", x, gf, z) for x, z in ((2.2, -2.2), (-2.2, -2.2), (2.2, 2.2), (0, 0))])
    return p.pieces()


if __name__ == "__main__":
    pieces = wood_lighthouse()
    bp = {"name": "wood_lighthouse", "description": "Very simple tall lighthouse of basic wood pieces, 3 x 3 floor tiles "
          "(6 x 6 m), 22 m walls with a door, stair winding up inside, parapet, small gabled roof on four posts, standing "
          "wood torches. Ravenholt limits only.", "source": "wood_lighthouse.py",
          "site": {"level_half": 3.5, "clear_radius": 5, "stand": [0, -6], "limits": "Ravenholt"}}
    head = json.dumps(bp, indent=1)[:-2]
    with open(os.path.join(HERE, "wood_lighthouse.json"), "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + ",\n  ".join(json.dumps(p) for p in pieces) + "\n ]\n}\n")
    print(f"wrote wood_lighthouse.json: {len(pieces)} pieces")

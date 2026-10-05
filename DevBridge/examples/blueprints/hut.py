# Design source of hut_2x2.json: a small wood hut on 2 x 2 floor tiles (4 x 4 m), the user's "2 by 2". 2 m walls, a
# wood door in the middle of the front (-z) between quarter-wall columns, gable triangles front and back, a 45 degree
# roof (ridge along z, 1 m past each gable) with a ridge cap. Run it to write the JSON again.
import json, os
from kit import Plan

HERE = os.path.dirname(os.path.abspath(__file__))


def hut():
    p = Plan()
    for x in (-1, 1):
        for z in (-1, 1):
            p.add(0, "wood_floor", x, 0.05, z)
    for u in (-1, 1):
        p.add(1, "woodwall", u, 1.0, 2)                                  # back
        p.add(1, "woodwall", -2, 1.0, u, 90)                             # sides
        p.add(1, "woodwall", 2, 1.0, u, 90)
    for sx in (-1, 1):
        for y in (0.0, 1.0):
            p.add(1, "wood_wall_quarter", sx * 1.5, y, -2)               # either side of the door
    for z in (-2, 2):
        p.add(2, "wood_wall_roof_45", -1, 2.0, z, 0)                     # gables, the tall sides meet in the middle
        p.add(2, "wood_wall_roof_45", 1, 2.0, z, 180)
    for z in (-2, 0, 2):
        p.add(3, "wood_roof_45", -1, 3.0, z, 270)
        p.add(3, "wood_roof_45", 1, 3.0, z, 90)
    for z in (-2, 0, 2):
        p.add(4, "wood_roof_top_45", 0, 3.0, z, 90)
    p.add(Plan.KEEP, "wood_door", 0, 1.01, -2, 0)
    return p.pieces()


if __name__ == "__main__":
    pieces = hut()
    bp = {"name": "hut_2x2", "description": "Small wood hut on 2 x 2 floor tiles (4 x 4 m): 2 m walls, a centred door, "
          "gables, 45 degree roof with a ridge cap.", "source": "hut.py",
          "site": {"level_half": 2.5, "clear_radius": 4, "stand": [0, -4.5]}}
    head = json.dumps(bp, indent=1)[:-2]
    with open(os.path.join(HERE, "hut_2x2.json"), "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + ",\n  ".join(json.dumps(p) for p in pieces) + "\n ]\n}\n")
    print(f"wrote hut_2x2.json: {len(pieces)} pieces")

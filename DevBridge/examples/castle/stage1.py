# Curtain walls, wall-walk, corner towers, NE spiral tower, gatehouse.
import sys
from collections import Counter
from castle_lib import *
door_walk = (8, 10)                           # rows for a 2 m doorway at walk level
door_ground = (0, 3)                          # ground archway (GY-0.44 .. GY+2.56)

# curtain walls: rows 0-7, parapet row 8, merlons row 9
wall(True, 23, -20, 20, range(0, 9), WB); merlons(True, 23, -20, 20, 9, WB)
wall(False, 23, -20, 20, range(0, 9), WB); merlons(False, 23, -20, 20, 9, WB)
wall(False, -24, -20, 20, range(0, 9), WB); merlons(False, -24, -20, 20, 9, WB)
for a0, a1 in ((-20, -8), (8, 20)):
    wall(True, -24, a0, a1, range(0, 9), WB); merlons(True, -24, a0, a1, 9, WB)
wall(True, -24, -2, 2, range(4, 8), WB)       # wall above the gate, up to the platform

# wall-walk: 2 m of wood floor inside each wall
for c in range(-19, 20, 2):
    add("wood_floor", c, WALK, 22); add("wood_floor", 22, WALK, c); add("wood_floor", -22, WALK, c)
    if abs(c) > 8: add("wood_floor", c, WALK, -22)

# corner towers (not NE): 8x8, parapet row 12, merlons row 13, floor at the walk, stone deck at WB+12
for sx, sz in ((-1, 1), (-1, -1), (1, -1)):
    tx, tz = 24 * sx, 24 * sz
    x0, z0 = tx - 4, tz - 4
    zband = (21, 23) if sz > 0 else (-23, -21)
    xband = (21, 23) if sx > 0 else (-23, -21)
    holes = {("E" if sx < 0 else "W"): [(*zband, *door_walk), (*zband, *door_ground)],
             ("N" if sz < 0 else "S"): [(*xband, *door_walk)]}
    box(x0, x0 + 8, z0, z0 + 8, range(0, 13), WB, holes, parapet_row=12)
    ts = [tx - 2, tx, tx + 2]
    floor("wood_floor", ts, [tz - 2, tz, tz + 2], WALK)
    floor("stone_floor_2x2", ts, [tz - 2, tz, tz + 2], WB + 12 - 0.5, skip={(tx + sx * 2, tz), (tx + sx * 2, tz + sz * 2)})
    yaw = 180 if sz > 0 else 0                 # the ladder rises toward the outer z wall
    add("wood_stepladder", tx + sx * 2.5, WALK, tz, yaw)
    add("wood_stepladder", tx + sx * 2.5, WALK + 2, tz + sz * 2, yaw)

# NE spiral tower: parapet row 19, merlons row 20, spiral stairs from the ground to the deck at GY+18.67
tx, tz = 24, 24
slits = [(23, 25, r, r + 1) for r in (4, 11, 15)]
box(20, 28, 20, 28, range(0, 20), WB,
    {"W": [(21, 23, *door_walk), (21, 23, *door_ground)], "S": [(21, 23, *door_walk)], "N": slits, "E": slits},
    parapet_row=19)
for pivot, yaw in ((GY + 0.46, 180), (GY + 4.01, 0), (GY + 8.02, 0), (GY + 11.57, 180), (GY + 15.12, 0)):
    add("piece_SprialStairsStoneV2", tx, pivot, tz, yaw)
for x, z in [(22, 22), (24, 22), (26, 22), (22, 24), (26, 24)]:
    add("wood_floor", x, WALK, z); add("wood_floor", x, GY + 18.67, z)

# gatehouse: two towers flanking the gate, a platform over the passage with a front parapet
for sx in (-1, 1):
    x0 = -8 if sx < 0 else 2
    outer, inner = ("W", "E") if sx < 0 else ("E", "W")
    box(x0, x0 + 6, -28, -20, range(0, 13), WB,
        {outer: [(-23, -21, *door_walk)], inner: [(-23, -21, *door_walk)], "N": [(x0 + 2, x0 + 4, *door_ground)]},
        parapet_row=12)
    xs = [x0 + 2, x0 + 4]
    lx = x0 + 1.5 if sx < 0 else x0 + 4.5    # ladder strip along the outer side wall
    floor("wood_floor", xs, [-26, -24, -22], WALK)
    open_x = xs[0] if sx < 0 else xs[1]
    floor("stone_floor_2x2", xs, [-26, -24, -22], WB + 12 - 0.5, skip={(open_x, -24), (open_x, -26)})
    add("wood_stepladder", lx, WALK, -24, 0)
    add("wood_stepladder", lx, WALK + 2, -26, 0)
wall(True, -28, -2, 2, range(4, 9), WB)          # front lintel over the passage
merlons(True, -28, -2, 2, 9, WB)
floor("wood_floor", [-1, 1], [-26, -24, -22], WALK)
add("wood_gate", -1, GY + 1, -23.5, 0); add("wood_gate", 1, GY + 1, -23.5, 0)

# stairs up to the wall-walk on the west and east walls: two stepladders wide, rising north, landing at z -3
for x in (-20.5, -19.5, 19.5, 20.5):
    for i in range(4):
        add("wood_stepladder", x, WB + 2 * i, -11 + 2 * i, 180)
add("wood_floor", -20, WALK, -3); add("wood_floor", 20, WALK, -3)

print(Counter(p[0] for p in P), len(P))
if "go" in sys.argv:
    if not status()["player"]["god"]: console("god")
    go(CX, GY + 1, CZ - 10)
    console("killall")
    build("stage 1")
    time.sleep(6)
    print("standing:", Counter(o["prefab"] for o in nearby(45) if str(o.get("creator")) == ME))

# The keep: a stone throne hall x[-8,8] z[2,20], 8 m walls, 45 degree wood roof with the ridge along z.
import sys
from collections import Counter
from castle_lib import *


def gable_roof(x0, x1, z0, z1, eave, overhang_s=0, overhang_n=0, vent=()):
    """Roof over cells x[x0,x1) z[z0,z1), ridge along z at the middle; (x1-x0) must be a multiple of 4."""
    half = (x1 - x0) // 2
    mid = (x0 + x1) / 2
    zs = range(z0 - 2 * overhang_s + 1, z1 + 2 * overhang_n, 2)
    for i in range(half // 2):
        for z in zs:
            add("wood_roof_45", x0 + 1 + 2 * i, eave + 1 + 2 * i, z, 270)
            add("wood_roof_45", x1 - 1 - 2 * i, eave + 1 + 2 * i, z, 90)
    for z in zs:
        if z not in vent: add("wood_roof_top_45", mid, eave + half - 1, z, 90)
    for zg in (z0 + 0.5, z1 - 0.5):            # gables in the plane of the end walls
        for j in range(half // 2):
            add("wood_wall_roof_45", x0 + 1 + 2 * j, eave + 2 * j, zg, 0)
            add("wood_wall_roof_45", x1 - 1 - 2 * j, eave + 2 * j, zg, 180)
            x = x0 + 2 * j + 3
            while x <= x1 - 2 * j - 3:
                add("woodwall", x, eave + 2 * j + 1, zg, 0); x += 2


if __name__ == "__main__":
    E = GY + 8
    windows = [(z, z + 1, 3, 6) for z in (6, 10, 14)]
    box(-8, 8, 2, 20, range(0, 8), GY,
        {"S": [(-2, 2, 0, 3), (-6, -5, 3, 6), (5, 6, 3, 6)], "W": windows, "E": windows, "N": [(-6, -5, 3, 6), (5, 6, 3, 6)]})
    gable_roof(-8, 8, 2, 20, E, overhang_s=1)
    add("wood_gate", -1, GY + 1, 2.5, 0); add("wood_gate", 1, GY + 1, 2.5, 0)
    # trusses at eave height, chandeliers hang from the middle beams
    for z in (6, 10, 14, 18):
        for x in range(-6, 7, 2): add("wood_beam", x, E - 0.1, z, 0)
    for z in (6, 10, 14):
        add("piece_brazierceiling01", 0, E - 0.1 - 1.95, z, 0)
    # stone floor, a stepped dais, the throne
    floor("stone_floor_2x2", range(-6, 7, 2), range(4, 19, 2), GY - 0.45)
    floor("stone_floor_2x2", [-3, -1, 1, 3], [12], GY - 0.2)
    floor("stone_floor_2x2", [-3, -1, 1, 3], [14, 16, 18], GY + 0.05)
    add("piece_throne01", 0, GY + 0.56, 17, 180)
    for x in (-3.3, 3.3):
        add("piece_brazierfloor01", x, GY + 1.06 + 0.55, 13.7)
    for x in (-4.6, 4.6):
        add("piece_brazierfloor01", x, GY + 1.06 + 0.05, 4.0)
    # red carpet up the aisle, fur rugs on the dais
    for z in (5, 9.5):
        add("jute_carpet", 0, GY + 0.08, z, 90)
    add("rug_wolf", 0, GY + 0.57, 15.5, 0)
    # banners on the side walls between the windows, behind the throne, and flanking the door outside
    for z in (4, 8, 12, 16):
        add("piece_banner01", -6.85, GY + 6.6, z, 90)
        add("piece_banner01", 6.85, GY + 6.6, z, 270)
    for x in (-3, 3):
        add("piece_banner02", x, GY + 7, 18.85, 0)
    for x in (-3.5, 3.5):
        add("piece_banner01", x, GY + 6.5, 1.85, 180)
    # wall torches beside the windows
    for z in (5, 9, 13, 17):
        add("piece_walltorch", -6.75, GY + 2.5, z + 0.5, 90)
        add("piece_walltorch", 6.75, GY + 2.5, z + 0.5, 270)
    # feast tables with benches along both sides of the hall
    for x in (-4.5, 4.5):
        for z in (6.5, 9.5):
            add("piece_table", x, GY + 0.05, z, 90)
            add("piece_bench01", x - 1.1, GY + 0.05, z, 90)
            add("piece_bench01", x + 1.1, GY + 0.05, z, 270)
    print(Counter(p[0] for p in P), len(P))
    if "go" in sys.argv:
        build("stage 2 keep")
        time.sleep(5)
        print("standing:", Counter(o["prefab"] for o in nearby(14, z=CZ + 11) if str(o.get("creator")) == ME))

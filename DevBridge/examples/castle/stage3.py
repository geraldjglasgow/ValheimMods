# Mead hall (bar), blacksmith, bakery, animal pen. Structure first, then props.
import sys
from collections import Counter
from castle_lib import *
from stage2 import gable_roof

DUMP = {}
for _l in open("pieces_dump2.tsv", encoding="utf-8"):
    _f = _l.rstrip("\n").split("\t")
    if _f[0] == "P":
        _d = {k: v for k, _, v in (x.partition("=") for x in _f[2:])}
        DUMP[_f[1]] = ([float(v) for v in _d["rb"].split(",")], [float(v) for v in _d["rbc"].split(",")])
VANILLA_BOTTOM = {"forge": 0, "forge_ext1": 0, "forge_ext2": 0, "forge_ext3": -0.16, "forge_ext4": 0.05, "forge_ext5": 0,
                  "forge_ext6": -0.69, "smelter": 0, "piece_oven": 0, "fermenter": 0, "piece_MeadCauldron": -0.16,
                  "piece_workbench": -0.02, "piece_workbench_ext1": 0, "piece_workbench_ext2": -0.04,
                  "piece_workbench_ext3": -0.35, "piece_preptable": -0.02, "piece_chest_wood": 0, "hearth": 0,
                  "piece_table": 0, "piece_bench01": 0, "wood_fence": -0.16, "piece_groundtorch_wood": -0.65,
                  "piece_walltorch": 0, "rug_fur": 0, "rug_deer": 0}


def prop(n, x, s, z, yaw=0):
    """Put a prop's mesh bottom on surface height s, its mesh centred on (x, z)."""
    if n in DUMP:
        size, c = DUMP[n]
        a = math.radians(yaw)
        ox, oz = c[0] * math.cos(a) + c[2] * math.sin(a), -c[0] * math.sin(a) + c[2] * math.cos(a)
        add(n, x - ox, s - (c[1] - size[1] / 2), z - oz, yaw)
    else:
        add(n, x, s - VANILLA_BOTTOM.get(n, 0), z, yaw)


TOP = GY + 0.83                                   # piece_table top
S = GY + 0.05                                     # stone floor top


def stone_floor(x0, x1, z0, z1):
    floor("stone_floor_2x2", range(x0 + 1, x1, 2), range(z0 + 1, z1, 2), GY - 0.45)


def structure():
    # mead hall x[-18,-6] z[-18,-4]: 4 m stone walls, door on the east side, windows west and south
    box(-18, -6, -18, -4, range(0, 4), GY,
        {"E": [(-12, -10, 0, 2)], "W": [(-15, -14, 1, 3), (-9, -8, 1, 3)], "S": [(-13, -11, 1, 3)]})
    gable_roof(-18, -6, -18, -4, GY + 4, overhang_s=1, overhang_n=1, vent=(-11,))
    stone_floor(-17, -7, -17, -5)
    add("wood_door", -6.5, GY + 1, -11, 90)
    for z in (-14, -8):
        for x in range(-16, -7, 2): add("wood_beam", x, GY + 3.9, z, 0)
    # blacksmith x[-18,-10] z[4,16]: open arcade toward the courtyard
    box(-18, -10, 4, 16, range(0, 4), GY, {"E": [(5, 9, 0, 4), (10, 15, 0, 4)]})
    gable_roof(-18, -10, 4, 16, GY + 4, overhang_s=1, overhang_n=1, vent=(9,))
    stone_floor(-17, -11, 5, 15)
    # bakery x[10,18] z[4,16]: open arcade toward the courtyard
    box(10, 18, 4, 16, range(0, 4), GY, {"W": [(5, 9, 0, 4), (10, 15, 0, 4)]})
    gable_roof(10, 18, 4, 16, GY + 4, overhang_s=1, overhang_n=1, vent=(9,))
    stone_floor(11, 17, 5, 15)
    for z in (8, 12):
        for x in (12, 14, 16): add("wood_beam", x, GY + 3.9, z, 0)
    # animal pen x[8,18] z[-18,-6]: fence with a gap on the west, a hay shelter on the east
    for x in range(9, 18, 2):
        add("wood_fence", x + 0.1, GY + 0.16, -18, 0); add("wood_fence", x + 0.1, GY + 0.16, -6, 0)
    for z in range(-17, -6, 2):
        add("wood_fence", 18, GY + 0.16, z - 0.1, 90)
        if z not in (-13, -11): add("wood_fence", 8, GY + 0.16, z - 0.1, 90)
    for x in (14.3, 17.7):
        for z in (-17.7, -12, -6.3):
            add("wood_pole2", x, GY + 1, z); add("wood_pole2", x, GY + 2, z)
    floor("wood_floor", [15, 17], range(-17, -6, 2), GY + 3)


def props():
    # --- mead hall: bar across the north end, kegs behind it, hearth, tables, fermenter, cauldron
    for x in (-14.5, -12, -9.5): prop("piece_table", x, S, -7.6, 0)
    for x in (-16, -14.6, -13.2, -10.8, -9.4, -8): prop("piece_KegStand", x, S, -5.6, 180); \
        add("piece_Keg", x - DUMP["piece_KegStand"][1][0], S + 0.51, -5.6 + DUMP["piece_KegStand"][1][2], 180)
    for i, x in enumerate((-15.3, -14.2, -13, -11.6, -10.3, -9.1)):
        prop("piece_Stein" if i % 3 else "piece_WoodenChag", x, TOP, -7.8, 0)
    prop("hearth", -12, S, -11, 0)
    prop("piece_bench01", -12, S, -8.9, 180); prop("piece_bench01", -12, S, -13.1, 0)
    for x in (-14.5, -9.5):
        prop("piece_table", x, S, -15.5, 0)
        prop("piece_bench01", x, S, -14.4, 180); prop("piece_bench01", x, S, -16.6, 0)
        prop("piece_Stein", x - 0.6, TOP, -15.4); prop("piece_Stein", x + 0.5, TOP, -15.6)
        prop("piece_ChickenPie", x, TOP, -15.5)
    prop("fermenter", -15.8, S, -11, 90)
    prop("piece_MeadCauldron", -8, S, -9.3, 270)
    for z in (-14, -8):
        for x in (-15, -9): prop("piece_HamHang", x, GY + 3.85 - 1.42, z)
    prop("piece_bannerBEE", -16.85, GY + 3.9 - 3.03, -11, 0)
    for z in (-15.5, -7.5): add("piece_walltorch", -16.75, GY + 2.3, z, 90)
    # --- blacksmith: forge and its upgrades, workbench, smelter, crates
    prop("forge", -15.8, S, 9.5, 90)
    prop("forge_ext1", -15.9, S, 11.6, 90)
    prop("forge_ext2", -12.6, S, 9.5, 270)
    prop("forge_ext3", -16.2, S, 7.4, 90)
    prop("forge_ext4", -12.6, S, 11.8, 270)
    prop("forge_ext5", -12.6, S, 7.3, 270)
    prop("forge_ext6", -14.2, S, 14.4, 180)
    prop("piece_workbench", -15.6, S, 5.6, 90)
    prop("smelter", -14, GY, 18.4, 0)
    prop("piece_ChestCrate", -12, S, 5.5); prop("piece_ChestBarrel", -11.6, S, 14.6)
    for z in (6.5, 12.5): add("piece_walltorch", -16.75, GY + 2.3, z, 90)
    # --- bakery: oven, prep table, bread on tables, sacks, shelves, hanging garlic and onions
    prop("piece_oven", 15.6, S, 9.5, 270)
    prop("piece_preptable", 15.8, S, 6, 270)
    prop("piece_table", 12.6, S, 12.5, 90)
    for dz, n in ((-0.9, "piece_Bread1"), (-0.35, "piece_Bread2"), (0.2, "piece_FruitTart"), (0.75, "piece_LemonMeringuePie")):
        prop(n, 12.6, TOP, 12.5 + dz)
    prop("piece_table", 12.6, S, 7, 90)
    for dz, n in ((-0.8, "piece_ChickenPie"), (0, "piece_Bread2"), (0.7, "piece_CheeseWheel")): prop(n, 12.6, TOP, 7 + dz)
    prop("piece_Sac1", 16.4, S, 14.4); prop("piece_Sac3", 15.1, S, 14.6); prop("piece_Sac1", 16.4, S, 13.2, 40)
    prop("piece_ButterChurnLazy", 13.3, S, 14.6)
    prop("piece_Shelves3", 16.6, S, 12, 270)
    for i, (x, z) in enumerate(((13, 8), (15, 8), (13, 12), (15, 12))):
        prop("piece_GarlicHang" if i % 2 else "piece_OnionHang", x, GY + 3.85 - (0.88 if i % 2 else 1.59), z)
    for z in (6.5, 12.5): add("piece_walltorch", 16.75, GY + 2.3, z, 270)
    # --- pen: troughs, hay, bucket, pitchfork, wheelbarrow
    prop("piece_FARMTroughH20", 11.5, GY, -17.1, 0)
    prop("piece_FARMTrough", 11.5, GY, -6.9, 180)
    for z in (-16, -14.5, -9): prop("piece_FARMHay3", 16, GY, z, 30 * (z % 3))
    prop("piece_FARMHay4", 16.2, GY + 1.27, -15.4, 10)
    prop("piece_MoreBucket", 9, GY, -16.8); prop("piece_Pitchfork", 14.6, GY, -11.6)
    prop("piece_Wheelbarrow", 6.5, GY, -15, 20)


if __name__ == "__main__":
    structure(); print("structure", Counter(p[0] for p in P), len(P))
    if "go" in sys.argv: build("stage 3 structure")
    else: P.clear()
    props(); print("props", Counter(p[0] for p in P), len(P))
    if "go" in sys.argv:
        build("stage 3 props")
        time.sleep(8)
        drops = [o for o in nearby(40) if o.get("item")]
        print("item drops (broken pieces):", Counter(o["prefab"] for o in drops))

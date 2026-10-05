# Furniture, stations and ornaments for the village buildings, each list in its building's own frame (door side -z,
# y from the levelled ground). A piece sits on a surface at surface + its pivot height above the mesh bottom, read
# from the catalogue (pieces.json, then pieces_offline.json). Comfort is checked with: blueprint.py comfort <name>.
import json, os

HERE = os.path.dirname(os.path.abspath(__file__))
CAT = {**json.load(open(os.path.join(HERE, "pieces_offline.json"))), **json.load(open(os.path.join(HERE, "pieces.json")))}
WOOD, STONE, UPPER = 0.13, 0.1, 3.08          # floor tops: wood ground floor, stone floor, the houses' upper floor
CHEST_H = 1.08                                 # a reinforced chest stacked on another: 1 cm into it, so it rests
# Only pieces the user's group can make (2026-10-05: Swamp done, forge and workbench at level 5, silver just started):
# nothing needing tar, lox, linen, black metal, jute, black marble, Mistlands or Ashlands materials, frostwood, gold.


def on(n, x, surface, z, yaw=0):
    """A piece standing on a surface: its mesh bottom on it."""
    return (n, x, round(surface + CAT[n]["pivot_above_bottom"], 3), z, yaw)


def hung(n, x, bottom, z, yaw=0):
    """A piece hanging on a wall or post with its mesh bottom at `bottom`."""
    return (n, x, round(bottom + CAT[n]["pivot_above_bottom"], 3), z, yaw)


# ---------------------------------------------------------------- ornate house: exactly 10 comfort at the bed
def house():
    """Living room: wood table, benches, deer rug, chests. Bedroom: a plain bed (the user's choice), an armour stand
    beside it, wolf rug, wood chair at a wood table, a black banner over the chest. At the bed: 1 + shelter 1 + bed 1
    + armour stand 1 + rug 2 + chair 2 + table 1 + banner 1 = 10 (the living room's table, benches and rug are in the
    same groups, so they add nothing)."""
    return [
        on("rug_deer", 1.0, WOOD, 0.5),
        on("piece_table", 1.0, WOOD, 0.5, 90),
        on("piece_bench01", 0.0, WOOD, 0.5, 90),
        on("piece_bench01", 2.0, WOOD, 0.5, 270),
        on("piece_chest_wood", 1.5, WOOD, 4.3, 180),
        on("piece_chest_wood", 3.4, WOOD, 4.3, 180),
        on("bed", 1.8, UPPER, 2.9, 180),
        on("ArmorStand", 3.95, UPPER, 2.2, 270),
        on("rug_wolf", 1.8, UPPER, -0.8),
        on("piece_table", 2.9, UPPER, -3.6),
        on("piece_chair02", 1.15, UPPER, -3.6, 90),
        on("piece_chest_wood", -1.5, UPPER, 4.3, 180),
        hung("piece_banner01", -1.5, UPPER + 0.9, 4.7, 90),
    ]


def house_outside():
    """No comfort: a carved dragon on the front ridge, sconces on the porch posts either side of the door."""
    return [
        ("wood_dragon1", 0, 11.0, -7.6, 180),
        hung("piece_walltorch", -1, 1.6, -5.65, 180),
        hung("piece_walltorch", 1, 1.6, -5.65, 180),
    ]


# ---------------------------------------------------------------- mead hall: high comfort, brewing at the -z end
def mead_hall():
    """Two hearths, wood tables and benches along both walls, the high seat (raven throne between stone thrones on a
    bearskin, blue banners, armour stands) at the +z end, a wolf rug between the hearths, red and blue banners on the
    posts, fey lights over the benches, item stands, wood chairs; brewing at the -z end: mead ketill and cauldron on
    fire pits, four fermenters, barrels."""
    out = [on("hearth", 0, WOOD, z, 90) for z in (-6, 6)]
    for sx in (-1, 1):
        for sz in (-1, 1):
            out += [on("piece_table", sx * 5.15, WOOD, sz * z, 90) for z in (5.7, 8.3)]
            out += [on("piece_bench01", sx * 6.35, WOOD, sz * z, 90 if sx < 0 else 270) for z in (4.75, 7.25, 9.75)]
            out.append(hung("piece_FairylightGarland", sx * 6.7, 2.6, sz * 7.25, 90))
        for sz in (-1, 1):
            out.append(hung("piece_banner04", sx * 3.63, 0.67, sz * 3))                  # red banners, mid posts
            out.append(hung("piece_banner02", sx * 3.63, 0.67, sz * 11))
            out.append(hung("itemstand", sx * 4, 1.4, sz * 2.63, 0 if sz < 0 else 180))  # on the mid posts
            out.append(on("piece_chair02", sx * 5.15, WOOD, sz * 3.2, 0 if sz > 0 else 180))   # at the table ends
        out.append(on("ArmorStand", sx * 4.8, WOOD, 13.6, 180))
        out.append(on("piece_throne02", sx * 2.6, WOOD, 14.1, 180))
        out += [on("piece_chest_barrel", sx * 6.3, WOOD, z) for z in (-12.3, -11.3)]      # clear of the bench ends
        out += [on("fermenter", sx * x, WOOD, -13.6) for x in (3.4, 5.4)]
    out.append(hung("piece_FairylightGarland", -6.7, 2.6, 0, 90))                        # over the west wall
    out += [on("piece_throne01", 0, WOOD, 14.0, 180), on("rug_Bjorn", 0, WOOD, 11.5),
            hung("piece_banner02", -1.3, WOOD + 0.6, 14.7, 90), hung("piece_banner02", 1.3, WOOD + 0.6, 14.7, 90),
            on("rug_wolf", 0, WOOD, 0)]
    for x, n in ((-1.2, "piece_MeadCauldron"), (1.4, "piece_cauldron")):
        out += [on("fire_pit", x, WOOD, -12.5), on(n, x, WOOD, -12.5)]
    return out


# ---------------------------------------------------------------- the working buildings
def kitchen():
    """Hearth with the iron cooking station over it, a campfire with three spits at the front, the cauldron on a
    fire pit with its spice rack and butcher's table, a stone oven, the preparation table, barrels."""
    out = [on("hearth", 0, STONE, 0), on("piece_cookingstation_iron", 0, STONE, 0),
           on("fire_pit", 0, STONE, -4.5)]
    out += [on("piece_cookingstation", 0, STONE, -4.5, yaw) for yaw in (0, 60, 120)]
    out += [on("fire_pit", -3.0, STONE, 4.6), on("piece_cauldron", -3.0, STONE, 4.6),       # extensions within 5 m
            hung("cauldron_ext1_spice", -3.0, 1.0, 6.55, 180), on("cauldron_ext3_butchertable", -4.1, STONE, 2.4),
            on("piece_oven", 3.2, STONE, 5.5, 180), on("piece_preptable", 3.8, STONE, 1.5, 90)]
    out += [on("piece_chest_barrel", sx * 4.3, STONE, -5.5) for sx in (-1, 1)]
    return out


def food_store():
    """Ten chests along the side walls, four reinforced chests along the back, six barrels in the middle."""
    out = [on("piece_chest_wood", sx * 4.3, WOOD, z, 90 if sx < 0 else 270)
           for sx in (-1, 1) for z in (-3.5, -1.7, 0.1, 1.9, 3.7)]
    out += [on("piece_chest", x, WOOD, 5.25, 180) for x in (-2.9, -0.95, 1.0, 2.95)]
    out += [on("piece_chest_barrel", x, WOOD, z) for x in (-1.2, 1.2) for z in (-1.5, 0, 1.5)]
    return out


def blacksmith():
    """Forge at level 5 (cooler, smith's anvil, grinding wheel, tool rack; no bellows or anvils yet), workbench at
    level 5 with its four, two reinforced chests."""
    return [
        on("forge", 0, STONE, 6.5, 180), on("forge_ext2", -1.2, STONE, 4.3),
        on("forge_ext3", 2.2, STONE, 6.6, 180), on("forge_ext5", 3.5, STONE, 6.6),
        hung("forge_ext6", 0, 1.3, 8.75, 180),                                           # touches the back wall
        on("piece_workbench", -5.6, STONE, 2.0, 90), on("piece_workbench_ext1", -5.8, STONE, -1.2),
        on("piece_workbench_ext2", -5.9, STONE, 5.3, 90), on("piece_workbench_ext3", -3.2, STONE, -1.4),
        on("piece_workbench_ext4", -3.2, STONE, 4.8),
        on("piece_chest", -5.2, STONE, 7.6, 180), on("piece_chest", 5.2, STONE, 7.6, 180),
    ]


def smelting_yard():
    """Charcoal kiln at the back, two smelters in the middle, two chests at the front."""
    return [on("charcoal_kiln", 0, STONE, 6.0), on("smelter", -2.5, STONE, 0), on("smelter", 2.5, STONE, 0),
            on("piece_chest", -3.9, STONE, -8.0, 90),
            on("piece_chest", 3.9, STONE, -8.0, 270)]


def barn():
    """The storage hall (the user's wish): reinforced chests two high, every one facing an aisle. Along both side
    walls (the side doors kept clear) and the back wall; two islands either side of a 3.6 m centre aisle from the
    gate, in the front half and under the loft, each a wood wall spine with chests backing onto both faces; a row each
    side up in the loft. Chests support nothing, so every upper chest also leans on a wall or a spine. A chest's
    front is +z at yaw 0, so a chest backing onto a wall at -x turns 90, at +x 270, at +z 180."""
    out = [on("woodwall", sx * 3, WOOD, z, 90) for sx in (-1, 1) for z in (-7.0, -5.0, -3.0, 3.0, 5.0, 7.0)]
    for level in (WOOD, WOOD + CHEST_H):
        for sx, yaw in ((-1, 90), (1, 270)):
            out += [on("piece_chest", sx * 6.2, level, z, yaw) for z in (-9.8, -7.85, -5.9, -3.95, 3.95, 5.9, 7.85, 9.8)]
            for z in (-7.0, -5.0, -3.0, 3.0, 5.0, 7.0):                       # islands at x +-3, 1 cm into the spine
                out += [on("piece_chest", sx * 3 + 0.69, level, z, 90), on("piece_chest", sx * 3 - 0.69, level, z, 270)]
        out += [on("piece_chest", x, level, 10.25, 180) for x in (-4.75, -2.85, -0.95, 0.95, 2.85, 4.75)]
    for sx, yaw in ((-1, 90), (1, 270)):
        out += [on("piece_chest", sx * 4.4, 4.08, z, yaw) for z in (3.0, 5.0, 7.0, 9.0)]   # loft, under the rafters
    return out

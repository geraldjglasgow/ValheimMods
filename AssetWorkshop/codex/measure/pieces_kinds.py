"""Which codex category and which material set each build-menu piece belongs to. The game files a piece under a menu
tab (Piece.m_category) and usage tags, which are too coarse for modelling (a door and a floor share a tab), so the
category comes from the prefab's name and scripts, checked in order: the first rule that matches wins.

    pieces_kinds.category("woodwall", {"Piece", "WearNTear"})     # 'piece.wall'
    pieces_kinds.material_set("woodiron_beam", "Iron")             # 'ironwood'
"""
import re

EXCLUDED = {"Cart": "vehicle", "Karve": "ship", "Raft": "ship", "VikingShip": "ship", "VikingShip_Ashlands": "ship",
            "BatteringRam": "siege engine", "Catapult": "siege engine", "piece_repair": "hammer tool",
            "Placeable_HardRock": "placeable item", "piece_TrainingDummy": "creature rig"}

# (category, scripts that decide it, name pattern), first match wins
RULES = (
    ("furniture.decor", set(), r"bathtub|ArcheryTarget|maypole"),
    ("station.extension", {"StationExtension"}, None),
    ("station.forge", set(), r"^(forge|blackforge)$"),
    ("station.cauldron", set(), r"^piece_(cauldron|MeadCauldron)$"),
    ("station.workbench", {"CraftingStation"}, None),
    ("station.production", {"Smelter", "Fermenter", "CookingStation", "Beehive", "SapCollector", "Windmill",
                            "Incinerator"}, None),
    ("station.other", {"TeleportWorld", "PrivateArea", "MapTable", "WispSpawner", "Barber", "ShieldGenerator"}, None),
    ("furniture.bed", {"Bed"}, None),
    ("furniture.hearth", set(), r"^(fire_pit|fire_pit_iron|hearth|bonfire)$"),
    ("furniture.light", {"Fireplace"}, None),
    ("furniture.light", set(), r"lantern|torch|brazier|candle"),
    ("furniture.chest", {"Container"}, r"chest|barrel"),
    ("furniture.chest", {"Container"}, None),
    ("furniture.chest", set(), r"chest"),
    ("furniture.chair", set(), r"chair|bench|throne|stool"),
    ("furniture.table", set(), r"table"),
    ("furniture.cloth", set(), r"banner|cloth_hanging"),
    ("furniture.rug", set(), r"^rug_|carpet"),
    ("piece.stack", set(), r"_stack$|_pile$"),
    ("piece.defence", {"Trap", "Turret"}, None),
    ("piece.defence", set(), r"stake"),
    ("piece.door", {"Door"}, r"door"),
    ("piece.window", {"Door"}, r"window"),
    ("piece.gate", {"Door"}, None),
    ("piece.window", set(), r"window"),
    ("piece.fence", set(), r"fence"),
    ("piece.stair", set(), r"stair|ladder"),
    ("piece.arch", set(), r"arch"),
    ("piece.gable", set(), r"wall_roof|wall_cross_67|_roof_(26|45|67)_upside|scale_wall_roof"),
    ("piece.roof", set(), r"roof"),
    ("piece.beam", set(), r"beam|_cross_|wood_log_\d"),
    ("piece.pole", set(), r"pole|pillar|column"),
    ("piece.floor", set(), r"floor"),
    ("piece.block", set(), r"^blackmarble_|icecube"),
    ("piece.wall", set(), r"wall"),
    ("furniture.decor", set(), r""),
)

LABELS = {
    "piece.wall": "walls: full, half and quarter panels, log walls, stone and iron walls",
    "piece.gable": "gable and roof-end walls: triangles and slopes that close a roof's end",
    "piece.floor": "floors: 1x1, 2x2 and 4x4 plates",
    "piece.roof": "roofs: slopes, ridges, inner and outer corners at 26, 45 and 67 degrees",
    "piece.beam": "beams: horizontal and diagonal beams, braces and crosses, diagonal logs",
    "piece.pole": "poles, pillars and columns",
    "piece.stair": "stairs and ladders",
    "piece.door": "doors: hinged single doors",
    "piece.gate": "gates: double doors, grates, drawbridges, fence gates",
    "piece.window": "windows and shutters",
    "piece.fence": "fences",
    "piece.arch": "arches",
    "piece.block": "solid blocks: black marble bricks, bases, tips; the ice cube",
    "piece.defence": "defences: stake walls, sharp stakes, traps, turrets",
    "piece.stack": "resource stacks and piles (wood, stone, bars, coins)",
    "furniture.chair": "chairs, stools, benches and thrones",
    "furniture.table": "tables",
    "furniture.bed": "beds",
    "furniture.chest": "chests, barrels, pots and other containers",
    "furniture.light": "torches, sconces, braziers, lanterns and candles",
    "furniture.hearth": "hearths, fire pits and the bonfire",
    "furniture.cloth": "banners and hanging cloths",
    "furniture.rug": "rugs and carpets",
    "furniture.decor": "decor: item and armour stands, figures, garlands, seasonal pieces, the sign",
    "station.workbench": "crafting tables: workbench, stonecutter, artisan table, galdr table, prep table",
    "station.forge": "forges: the forge and the black forge",
    "station.cauldron": "cauldrons: the cauldron and the mead cauldron",
    "station.extension": "station extensions: the upgrades placed next to a station",
    "station.production": "production stations: smelters, kilns, furnaces, fermenter, windmill, spinning wheel, "
                          "beehive, cooking stations, oven, sap collector, obliterator",
    "station.other": "other stations: portals, ward, cartography table, wisp lure, eternal pyre, barber, shield",
}

# (material set, name pattern), first match wins; unmatched pieces fall back to their WearNTear material
SETS = (
    ("ironwood", r"^woodiron_"), ("corewood", r"wood_pole_log|wood_wall_log|wood_log_|yggdrasil|wood_core"),
    ("darkwood", r"darkwood"), ("blackmarble", r"blackmarble"), ("grausten", r"grausten"),
    ("ashwood", r"ashwood"), ("dvergr", r"dvergr|hexagonal|blackforge"), ("stave", r"^stave_"),
    ("scale", r"^scale_"),
    ("flametal", r"flametal"), ("crystal", r"crystal_wall"), ("ice", r"icecube"), ("iron", r"^iron_"),
    ("stone", r"^stone_|hearth|stone_pile"), ("cloth", r"banner|cloth_hanging|rug_|carpet"),
    ("wood", r"^wood|^piece_chest_wood$|^piece_chest$"),
)


def category(name, scripts):
    """The codex category of a build-menu piece from its prefab name and the game scripts on it."""
    for key, needs, pattern in RULES:
        if needs and not needs & scripts:
            continue
        if pattern is None or re.search(pattern, name, re.IGNORECASE):
            return key
    return "furniture.decor"


def material_set(name, wear_material):
    """The piece's material set: the family of look it belongs to, named as the game's recipes name them."""
    for key, pattern in SETS:
        if re.search(pattern, name, re.IGNORECASE):
            return key
    return (wear_material or "none").lower()

# the prefabs that best show each category, for the lineup render (names; the data file gives their paths)
REFERENCES = {
    "piece.wall": ["woodwall", "stone_wall_2x1", "darkwood_decowall", "iron_wall_2x2", "Piece_grausten_wall_2x2",
                   "stave_wall_2x2"],
    "piece.gable": ["wood_wall_roof_a", "wood_wall_roof_45", "ashwood_wall_roof_45", "scale_wall_roof_45"],
    "piece.floor": ["wood_floor", "stone_floor_2x2", "blackmarble_floor", "Piece_grausten_floor_2x2",
                    "ashwood_floor_2x2"],
    "piece.roof": ["wood_roof", "wood_roof_top", "darkwood_roof", "darkwood_roof_ocorner", "piece_grausten_roof_45"],
    "piece.beam": ["wood_beam", "wood_beam_45", "darkwood_beam", "woodiron_beam", "stave_beam_2m", "wood_log_45"],
    "piece.pole": ["wood_pole2", "wood_pole_log", "darkwood_pole", "woodiron_pole", "stone_pillar",
                   "blackmarble_column_2"],
    "piece.stair": ["wood_stair", "stone_stair", "blackmarble_stair", "piece_dvergr_spiralstair", "ashwood_stair"],
    "piece.door": ["wood_door", "ashwood_door", "piece_hexagonal_door"],
    "piece.gate": ["wood_gate", "darkwood_gate", "iron_grate", "stave_gate", "flametal_gate"],
    "piece.window": ["wood_window", "Piece_grausten_window_2x2"],
    "piece.fence": ["wood_fence", "stone_fence"],
    "piece.arch": ["stone_arch", "darkwood_arch", "blackmarble_arch", "ashwood_arch_big", "Piece_grausten_pillar_arch"],
    "piece.block": ["blackmarble_1x1", "blackmarble_2x2x2", "blackmarble_base_1", "blackmarble_tip"],
    "piece.defence": ["stake_wall", "piece_sharpstakes", "piece_stakewall_blackwood", "piece_dvergr_stake_wall"],
    "piece.stack": ["wood_stack", "stone_pile", "bar_iron_stack", "coal_pile"],
    "furniture.chair": ["piece_chair", "piece_chair02", "piece_bench01", "piece_throne01", "piece_logbench01"],
    "furniture.table": ["piece_table", "piece_table_oak", "piece_table_round", "piece_blackmarble_table"],
    "furniture.bed": ["bed", "piece_bed02", "ashwood_bed"],
    "furniture.chest": ["piece_chest_wood", "piece_chest", "piece_chest_private", "piece_chest_blackmetal",
                        "piece_chest_barrel"],
    "furniture.light": ["piece_groundtorch_wood", "piece_groundtorch", "piece_walltorch", "piece_brazierfloor01",
                        "piece_dvergr_lantern"],
    "furniture.hearth": ["fire_pit", "hearth", "bonfire", "fire_pit_iron"],
    "furniture.cloth": ["piece_banner01", "piece_banner03", "piece_cloth_hanging_door",
                        "piece_cloth_hanging_door_blue"],
    "furniture.rug": ["rug_deer", "rug_wolf", "rug_fur", "jute_carpet"],
    "furniture.decor": ["itemstand", "itemstandh", "ArmorStand", "wood_dragon1", "darkwood_raven", "sign"],
    "station.workbench": ["piece_workbench", "piece_stonecutter", "piece_artisanstation", "piece_magetable",
                          "piece_preptable"],
    "station.forge": ["forge", "blackforge"],
    "station.cauldron": ["piece_cauldron", "piece_MeadCauldron"],
    "station.extension": ["piece_workbench_ext1", "piece_workbench_ext2", "piece_workbench_ext3", "forge_ext1",
                          "forge_ext2", "cauldron_ext3_butchertable"],
    "station.production": ["smelter", "charcoal_kiln", "fermenter", "piece_spinningwheel", "windmill",
                           "piece_cookingstation"],
    "station.other": ["portal_wood", "guard_stone", "piece_cartographytable", "piece_wisplure"],
}

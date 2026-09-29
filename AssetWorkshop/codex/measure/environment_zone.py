"""The game's world placement lists, read from the reference export: what the zone system scatters in each biome
(ZoneSystem.m_vegetation and each LocationList's), the locations it places (m_locations), the grass and small plants
the clutter system instances (ClutterSystem.m_clutter), and the weather every biome cycles through (EnvMan's
m_environments and m_biomes, plus each LocationList's own).

    import environment_zone as zone
    zone.vegetation()        [{name, prefab, biomes, scale, tilt, per_zone, group, altitude, in_forest, ...}]
    zone.locations()         [{name, prefab, biomes, quantity, exterior_radius, clear_area, ...}]
    zone.clutter()           [{name, prefab, biomes, amount, scale, ...}]
    zone.environments()      {name: {colour and density fields}}, zone.biome_weather() {biome: [(env, weight)]}

Lists are read as plain text: every '  - m_name:' item of a top-level list is one entry, its '    m_key: value' lines
its fields. Nothing here is copied into the repository; the codex keeps the numbers.
"""
import functools
import os
import re

import game

SYSTEMS = "Systems"
ZONE_SYSTEM = "Systems/_ZoneSystem.prefab"
GAME_MAIN = "Systems/_GameMain.prefab"
ENVIRONMENT = "Systems/_Environment.prefab"
BIOMES = {1: "Meadows", 2: "Swamp", 4: "Mountain", 8: "BlackForest", 16: "Plains", 32: "AshLands",
          64: "DeepNorth", 256: "Ocean", 512: "Mistlands"}


def biome_names(mask):
    """Heightmap.Biome flags -> ['Meadows', 'BlackForest']; bits the enum does not name are left out."""
    return [name for bit, name in BIOMES.items() if int(mask) & bit]


def location_lists():
    """Every LocationList prefab the zone system reads, the base list's file first."""
    return [ZONE_SYSTEM] + game.find("Systems/LocationLists/*.prefab")


def list_items(text, name):
    """The entries of the top-level list 'name' in a MonoBehaviour's text: one dict of raw field strings each."""
    match = re.search(rf"^  {name}:\n((?:  [ -].*\n)*)", text, re.MULTILINE)
    if not match:
        return []
    entries = re.split(r"^  - ", match.group(1), flags=re.MULTILINE)[1:]
    return [_fields("    " + entry) for entry in entries]


def _fields(entry):
    """'    m_key: value' lines of one list entry -> {m_key: value}; nested blocks keep their first line only."""
    found = {}
    for key, value in re.findall(r"^    (m_\w+):[ ]?(.*)$", entry, re.MULTILINE):
        found.setdefault(key, value.strip())
    return found


def _num(entry, key, default=0.0):
    try:
        return float(entry.get(key, default))
    except ValueError:
        return default


@functools.lru_cache(maxsize=None)
def vegetation():
    """What ZoneVegetation places, from every list, in file order: enabled entries only."""
    found = []
    for path in location_lists():
        for entry in list_items(game.unity.read(path), "m_vegetation"):
            if entry.get("m_enable") != "1":
                continue
            found.append(_vegetation_entry(entry, path))
    return found


def _vegetation_entry(entry, source):
    """One vegetation entry as the codex keeps it."""
    return {"name": entry.get("m_name", ""), "prefab": game.path_of(entry.get("m_prefab", "")),
            "list": os.path.basename(source), "biomes": biome_names(_num(entry, "m_biome")),
            "scale": [_num(entry, "m_scaleMin"), _num(entry, "m_scaleMax")],
            "rand_tilt_deg": _num(entry, "m_randTilt"), "ground_tilt_chance": _num(entry, "m_chanceToUseGroundTilt"),
            "per_zone": [_num(entry, "m_min"), _num(entry, "m_max")],
            "group": [_num(entry, "m_groupSizeMin"), _num(entry, "m_groupSizeMax"), _num(entry, "m_groupRadius")],
            "altitude": [_num(entry, "m_minAltitude"), _num(entry, "m_maxAltitude")],
            "slope_deg": [_num(entry, "m_minTilt"), _num(entry, "m_maxTilt")],
            "in_forest": entry.get("m_inForest") == "1", "block_check": entry.get("m_blockCheck") == "1",
            "ground_offset": _num(entry, "m_groundOffset"), "snap_to_water": entry.get("m_snapToWater") == "1"}


@functools.lru_cache(maxsize=None)
def locations():
    """What ZoneLocation places, from every list: enabled entries, each with its prefab found by name."""
    found = []
    for path in location_lists():
        for entry in list_items(game.unity.read(path), "m_locations"):
            if entry.get("m_enable") == "1":
                found.append(_location_entry(entry, path))
    return found


def _location_entry(entry, source):
    """One location entry. Its prefab is found by the entry's name first: some lists (Deep North) carry a stale
    m_prefabName copied from another entry, while the real reference is a SoftReference id the export cannot resolve."""
    prefab = location_prefab(entry.get("m_name", "")) or location_prefab(entry.get("m_prefabName", ""))
    return {"name": entry.get("m_name", ""), "prefab": prefab, "list": os.path.basename(source),
            "biomes": biome_names(_num(entry, "m_biome")), "quantity": int(_num(entry, "m_quantity")),
            "exterior_radius": _num(entry, "m_exteriorRadius"), "interior_radius": _num(entry, "m_interiorRadius"),
            "clear_area": entry.get("m_clearArea") == "1", "unique": entry.get("m_unique") == "1",
            "min_distance_from_similar": _num(entry, "m_minDistanceFromSimilar"),
            "max_terrain_delta": _num(entry, "m_maxTerrainDelta"), "in_forest": entry.get("m_inForest") == "1",
            "altitude": [_num(entry, "m_minAltitude"), _num(entry, "m_maxAltitude")],
            "random_rotation": entry.get("m_randomRotation") == "1", "snap_to_water": entry.get("m_snapToWater") == "1"}


@functools.lru_cache(maxsize=None)
def _prefab_names():
    """{lower-case file name without .prefab: path} over world/Locations, then the rest of world/ (sorted paths)."""
    names = {}
    for path in game.find("world/Locations/**/*.prefab") + game.find("world/**/*.prefab"):
        names.setdefault(os.path.splitext(os.path.basename(path))[0].lower(), path)
    return names


def location_prefab(name):
    """A location's prefab path from its name (any case), or None when the export does not hold it."""
    return _prefab_names().get(name.lower()) if name else None


@functools.lru_cache(maxsize=None)
def clutter():
    """What the clutter system instances (grass, small plants): the base list in _GameMain and each LocationList's."""
    found = []
    for path in [GAME_MAIN] + location_lists()[1:]:
        for entry in list_items(game.unity.read(path), "m_clutter"):
            if entry.get("m_enabled") == "1":
                found.append(_clutter_entry(entry, path))
    return found


def _clutter_entry(entry, source):
    return {"name": entry.get("m_name", ""), "prefab": game.path_of(entry.get("m_prefab", "")),
            "list": os.path.basename(source), "biomes": biome_names(_num(entry, "m_biome")),
            "instanced": entry.get("m_instanced") == "1", "amount": int(_num(entry, "m_amount")),
            "scale": [_num(entry, "m_scaleMin"), _num(entry, "m_scaleMax")],
            "slope_deg": [_num(entry, "m_minTilt"), _num(entry, "m_maxTilt")],
            "altitude": [_num(entry, "m_minAlt"), _num(entry, "m_maxAlt")],
            "terrain_tilt": entry.get("m_terrainTilt") == "1", "on_cleared": entry.get("m_onCleared") == "1",
            "on_uncleared": entry.get("m_onUncleared") == "1", "in_forest": entry.get("m_inForest") == "1",
            "fractal_scale": _num(entry, "m_fractalScale")}


ENV_COLOURS = ("m_ambColorNight", "m_ambColorDay", "m_fogColorNight", "m_fogColorMorning", "m_fogColorDay",
               "m_fogColorEvening", "m_fogColorSunNight", "m_fogColorSunMorning", "m_fogColorSunDay",
               "m_fogColorSunEvening", "m_sunColorNight", "m_sunColorMorning", "m_sunColorDay", "m_sunColorEvening",
               "m_ambientOcclusionColor")
ENV_NUMBERS = ("m_fogDensityNight", "m_fogDensityMorning", "m_fogDensityDay", "m_fogDensityEvening",
               "m_lightIntensityDay", "m_lightIntensityNight", "m_sunAngle", "m_windMin", "m_windMax",
               "m_rainCloudAlpha", "m_aoIntensityDay", "m_aoIntensityNight")
ENV_FLAGS = ("m_isWet", "m_isFreezing", "m_isFreezingAtNight", "m_isCold", "m_isColdAtNight", "m_alwaysDark",
             "m_snowBuildup")


@functools.lru_cache(maxsize=None)
def environments():
    """{env name: {'colours': {field: [r, g, b]}, 'numbers': {...}, 'flags': [...], 'list': file}} from EnvMan and
    every LocationList, colours as stored (0 to 1)."""
    found = {}
    for path in [ENVIRONMENT] + location_lists()[1:]:
        for entry in list_items(game.unity.read(path), "m_environments"):
            if "m_ambColorDay" in entry:
                found.setdefault(entry["m_name"], _environment_entry(entry, path))
    return found


def _environment_entry(entry, source):
    colours = {key[2:]: [round(v, 4) for v in game.unity.numbers(entry[key])[:3]] for key in ENV_COLOURS
               if key in entry}
    numbers = {key[2:]: _num(entry, key) for key in ENV_NUMBERS if key in entry}
    flags = [key[2:] for key in ENV_FLAGS if entry.get(key) == "1"]
    return {"colours": colours, "numbers": numbers, "flags": flags, "list": os.path.basename(source)}


@functools.lru_cache(maxsize=None)
def biome_weather():
    """{biome name: [(environment, weight)]} from EnvMan's m_biomes and each LocationList's m_biomeEnvironments."""
    found = {}
    for path, key in [(ENVIRONMENT, "m_biomes")] + [(p, "m_biomeEnvironments") for p in location_lists()[1:]]:
        text = game.unity.read(path)
        for block in _biome_blocks(text, key):
            names = biome_names(_num(block[0], "m_biome"))
            for name in names:
                found.setdefault(name, block[1])
    return found


def _biome_blocks(text, key):
    """[(fields, [(env, weight)])] of a biome list: each biome entry's own fields and its weighted environments."""
    match = re.search(rf"^  {key}:\n((?:  [ -].*\n)*)", text, re.MULTILINE)
    if not match:
        return []
    blocks = []
    for entry in re.split(r"^  - ", match.group(1), flags=re.MULTILINE)[1:]:
        weights = re.findall(r"m_environment: (.+)\n\s+m_weight: (" + game.unity.NUMBER + ")", entry)
        blocks.append((_fields("    " + entry), [(env.strip(), float(w)) for env, w in weights]))
    return blocks

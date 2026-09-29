"""Measures the game's locations (the Location prefabs the zone system places: ruins, camps, altars, towers, dungeon
entrances) and dungeon rooms (the Room prefabs a DungeonGenerator strings together), for environment.py.

A location in the export is one flattened hierarchy: the game prefabs its artists dropped in (walls, trees, rocks,
spawners, chests) appear as child GameObjects named after their prefab. reused_parts() finds them by name against
every prefab in the export, which is how the codex tells what a location is built from.
"""
import functools
import os
import re

import numpy as np

import environment_read as read
import environment_zone as zone
import game
from workshop import unity

THEMES = {1: "Crypt", 2: "SunkenCrypt", 4: "Cave", 8: "ForestCrypt", 16: "GoblinCamp", 32: "MeadowsVillage",
          64: "MeadowsFarm", 128: "DvergerTown", 256: "DvergerBoss", 512: "ForestCryptHildir", 1024: "CaveHildir",
          2048: "PlainsFortHildir", 4096: "AshlandRuins", 8192: "FortressRuins", 16384: "Hole",
          65536: "NorthVillage", 131072: "MorkHalla",
          262144: "HalfBurriedCrypt"}   # a bit Room.Theme does not name; DG_HalfBurried_ForestCrypt uses it
RULES = (  # (category, name pattern) in order; the first match wins
    ("location.altar", r"eikthyrnir|gdking|bonemass|dragonqueen|goblinking|fader|starttemple|runestone|"
                       r"lorestone|stonecircle|dolmen|stonehenge|placeofmystery|memorial|shipsetting"),
    ("location.natural", r"tarpit|firehole|sulfurarch|rockspire|infestedtree|drakenest|volturenest|leviathan|"
                         r"icepond|gammeltroll|bigrock|giant\d|swords\d"),
    ("location.camp", r"camp|woodhouse|woodfarm|woodvillage|swamphut|logcabin|vendor|hut\d|lumber|northvillage|"
                      r"charredstone_spawner|excavation"),
    ("location.ruin", r"ruin|stonehouse|grave|well\d|shipwreck|frozenship|fortress|stonetower\d"),
    ("location.structure", r"guardtower|lighthouse|harbour|viaduct|roadpost|statue|waymarker"),
)


def category(name, sample):
    """The location's category: a dungeon entrance when it has an interior, else by name."""
    if sample["location"].get("has_interior"):
        return "location.dungeon_entrance"
    for key, pattern in RULES:
        if re.search(pattern, name.lower()):
            return key
    return "location.structure"


@functools.lru_cache(maxsize=None)
def prefab_names():
    """Every prefab name in the export (world/, GameElements/, Characters/), lower case."""
    names = set()
    for pattern in ("world/**/*.prefab", "GameElements/**/*.prefab", "Characters/**/*.prefab", "Effects/**/*.prefab"):
        names |= {os.path.splitext(os.path.basename(p))[0].lower() for p in game.find(pattern)}
    return names


def base_name(name):
    """'stone_wall_4x2 (12)' -> 'stone_wall_4x2'; '(Clone)' and trailing numbers in brackets dropped."""
    return re.sub(r"\s*\(\d+\)$|\(Clone\)$", "", name).strip()


def reused_parts(pf):
    """{game prefab name: count} of the child GameObjects named after a game prefab, counting the outermost only."""
    names, counts, inside = prefab_names(), {}, set()
    for node in pf.nodes()[1:]:
        parent = unity.ref(unity.field(pf.docs[node][1], "m_Father"))[0]
        if parent in inside:
            inside.add(node)
            continue
        name = base_name(pf.name(node))
        if name.lower() in names:
            counts[name] = counts.get(name, 0) + 1
            inside.add(node)
    return dict(sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])))


def location_fields(pf):
    """The root Location component's values, or {}."""
    body = next((b for k, b in pf.components(pf.root()) if k == "Location"), None)
    if body is None:
        return {}
    number = lambda field: float(unity.field(body, field) or 0)   # noqa: E731
    return {"exterior_radius": number("m_exteriorRadius"), "interior_radius": number("m_interiorRadius"),
            "clear_area": unity.field(body, "m_clearArea") == "1", "no_build": unity.field(body, "m_noBuild") == "1",
            "has_interior": unity.field(body, "m_hasInterior") == "1",
            "random_damage": unity.field(body, "m_applyRandomDamage") == "1",
            "interior_environment": unity.field(body, "m_interiorEnvironment")}


def random_spawns(pf):
    """{count, chance: five numbers} of the RandomSpawn switches that make every copy of a location different."""
    chances = [float(unity.field(b, "m_chanceToSpawn") or 0) for _, k, b in pf.all_components() if k == "RandomSpawn"]
    return {"count": len(chances), "chance": five(chances)}


def terrain_ops(pf):
    """How the location reshapes the ground: TerrainModifier count and how many level, smooth and paint."""
    bodies = [b for _, k, b in pf.all_components() if k == "TerrainModifier"]
    flag = lambda b, f: unity.field(b, f) == "1"   # noqa: E731
    return {"modifiers": len(bodies), "level": sum(flag(b, "m_level") for b in bodies),
            "smooth": sum(flag(b, "m_smooth") for b in bodies),
            "paint_cleared": sum(flag(b, "m_paintCleared") for b in bodies)}


def five(values):
    """{n, min, p25, median, p75, max} rounded, or None."""
    if not values:
        return None
    q = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), "min": round(float(q[0]), 2), "p25": round(float(q[1]), 2),
            "median": round(float(q[2]), 2), "p75": round(float(q[3]), 2), "max": round(float(q[4]), 2)}


def measure(path):
    """One location or room: environment_read's summary without the per-material list, plus how it is composed."""
    pf = game.prefab(path)
    sample = read.summary(path)
    sample["materials"] = len(sample["materials"])
    sample["shaders"] = shader_share(sample.pop("parts"))
    sample["reused_parts"] = dict(list(reused_parts(pf).items())[:20])
    sample["random_spawn"] = random_spawns(pf)
    sample["terrain"] = terrain_ops(pf)
    sample["location"] = location_fields(pf)
    sample["generator"] = generator(pf)
    return sample


def generator(pf):
    """The DungeonGenerator a location or its interior runs: its themes, algorithm and room counts, or None."""
    body = next((b for _, k, b in pf.all_components() if k == "DungeonGenerator"), None)
    if body is None:
        return None
    themes = int(float(unity.field(body, "m_themes") or 0))
    number = lambda field: float(unity.field(body, field) or 0)   # noqa: E731
    return {"themes": [n for bit, n in THEMES.items() if themes & bit], "algorithm": unity.field(body, "m_algorithm"),
            "rooms": [number("m_minRooms"), number("m_maxRooms")], "tile_m": number("m_tileWidth"),
            "grid": number("m_gridSize"), "camp_radius_m": [number("m_campRadiusMin"), number("m_campRadiusMax")]}


def shader_share(rows):
    """{shader: share of the drawn surface} over part rows (world-projected parts counted by triangles)."""
    area = {}
    for row in rows:
        area[row["shader"]] = area.get(row["shader"], 0.0) + (row["area_m2"] or 0.0)
    total = sum(area.values())
    return {k: round(v / total, 3) for k, v in sorted(area.items(), key=lambda kv: -kv[1])} if total else {}


def locations():
    """{category: [samples]} over every location the zone system places, each prefab once."""
    found, seen = {}, set()
    for entry in zone.locations():
        path = entry["prefab"]
        if not path or path in seen or not path.startswith("world/Locations"):
            continue
        seen.add(path)
        sample = measure(path)
        sample["placement"] = {k: entry[k] for k in ("biomes", "quantity", "exterior_radius", "clear_area",
                                                      "min_distance_from_similar", "max_terrain_delta", "list")}
        found.setdefault(category(sample["name"], sample), []).append(sample)
    return found


def room_fields(pf):
    """The root Room component's values and its connections (RoomConnection children: type, entrance)."""
    body = next((b for k, b in pf.components(pf.root()) if k == "Room"), "")
    theme = int(float(unity.field(body, "m_theme") or 0))
    conns = [b for _, k, b in pf.all_components() if k == "RoomConnection"]
    return {"theme": [n for bit, n in THEMES.items() if theme & bit], "size": list(unity.numbers(
        unity.field(body, "m_size"))), "entrance": unity.field(body, "m_entrance") == "1",
            "end_cap": unity.field(body, "m_endCap") == "1", "divider": unity.field(body, "m_divider") == "1",
            "weight": float(unity.field(body, "m_weight") or 0),
            "connections": sorted(unity.field(b, "m_type") for b in conns)}


def rooms():
    """[samples] of every enabled Room prefab under world/Rooms (the dungeon kits)."""
    found = []
    for path in game.find("world/Rooms/**/*.prefab"):
        pf = game.prefab(path)
        body = next((b for k, b in pf.components(pf.root()) if k == "Room"), None)
        if body is None or unity.field(body, "m_enabled") == "0":
            continue
        sample = measure(path)
        sample["room"] = room_fields(pf)
        found.append(sample)
    return found

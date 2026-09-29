"""Writes codex/data/environment.json: the world's rocks, cliffs, ore deposits, trees, logs, bushes, grass, flowers,
pickables and debris (environment_props.py), the locations and dungeon rooms (environment_locations.py), and each
biome's terrain, prop palette, weather and light (environment_biomes.py).

    python codex/measure/environment.py

Every sample keeps its LOD triangles, size, texture size, texel density, shader, per-material rows, placement and
behaviour; every category its five-number stats and the three to six references a new asset lines up against.
"""
import json
import os

import numpy as np

import environment_biomes as biomes
import environment_locations as locations
import environment_props as props
import environment_zone as zone

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "data", "environment.json")
GENERATED = "2026-09-29"


def five(values):
    """{n, min, p25, median, p75, max} of the values given, None when there are none."""
    values = [v for v in values if v is not None]
    if not values:
        return None
    q = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), "min": round(float(q[0]), 2), "p25": round(float(q[1]), 2),
            "median": round(float(q[2]), 2), "p75": round(float(q[3]), 2), "max": round(float(q[4]), 2)}


def stats(samples):
    return {"triangles": five([s["triangles"] for s in samples]),
            "texture_px": five([s["texture_px"] for s in samples]),
            "texel_density": five([s["texel_density"] for s in samples]),
            "longest_m": five([max(s["size_m"]) for s in samples if s["size_m"]]),
            "above_ground_m": five([s["above_ground_m"] for s in samples]),
            "lod_count": five([len(s["lods"]) for s in samples if s["lods"]])}


REFERENCES = {
    "env.rock_small": ["Rock_4", "Rock_3", "Rock_7_deepnorth", "UnstableLavaRock"],
    "env.rock_large": ["rock4_forest", "rock1_mountain", "rock2_heath", "rock_mistlands1", "Ashlands_rock2"],
    "env.cliff": ["cliff_mistlands1", "cliff_ashlands4", "HeathRockPillar", "cliff_ashlands3_Arch_1"],
    "env.mineable": ["rock4_copper", "MineRock_Tin", "silvervein", "mudpile_beacon", "MineRock_Obsidian",
                     "giant_helmet1"],
    "env.tree_meadows": ["Beech1", "Oak1", "Birch1"], "env.tree_plains": ["Birch1_aut", "Birch2_aut"],
    "env.tree_blackforest": ["FirTree", "Pinetree_01"], "env.tree_swamp": ["SwampTree1", "SwampTree2"],
    "env.tree_mistlands": ["YggaShoot1", "YggdrasilRoot"], "env.tree_ashlands": ["AshlandsTree1", "AshlandsTree6"],
    "env.tree_deepnorth": ["SnowFirTree", "Pinetree_Snow"],
    "env.tree_small": ["Beech_small1", "FirTree_small", "YggaShoot_small1", "SnowFirTree_small"],
    "env.log_stump": ["Beech_Stub", "beech_log", "beech_log_half", "stubbe", "FirTree_oldLog", "OakStub"],
    "env.bush": ["Bush01", "shrub_2", "Bush02_en", "AshlandsBush1"],
    "env.grass": ["instanced_meadows_grass", "instanced_forest_groundcover", "instanced_heathgrass",
                  "instanced_swamp_grass", "instanced_ormbunke", "instanced_vass"],
    "env.flower": ["Pickable_Dandelion", "Pickable_Thistle", "instanced_heathflowers"],
    "env.pickable": ["Pickable_Flint", "Pickable_Stone", "Pickable_Mushroom", "RaspberryBush", "Pickable_Branch",
                     "Pickable_SeedCarrot"],
    "env.debris": ["StatueEvil", "FrozenGD", "ice1", "instanced_small_rock1"],
    "location.altar": ["Eikthyrnir", "GDKing", "Runestone_Meadows", "StoneHenge1", "Dolmen01"],
    "location.ruin": ["Ruin1", "StoneTowerRuins04", "SwampRuin1", "CharredRuins2", "Mistlands_GuardTower1_ruined_new"],
    "location.camp": ["WoodHouse1", "SwampHut1", "AbandonedLogCabin02", "Greydwarf_camp1", "Hildir_camp"],
    "location.structure": ["Waymarker01", "Mistlands_Lighthouse1_new", "Mistlands_Viaduct2", "Mistlands_Statue1"],
    "location.dungeon_entrance": ["Crypt2", "SunkenCrypt4", "TrollCave02", "MountainCave02",
                                  "Mistlands_DvergrTownEntrance1"],
    "location.natural": ["TarPit1", "DrakeNest01", "InfestedTree01", "Mistlands_Swords1", "SulfurArch"],
}
LOCATION_LABELS = {
    "location.altar": "boss altars, rune stones, stone circles, dolmens and henges",
    "location.ruin": "ruined stone houses, towers, wells, graves, wrecks and the charred ruins",
    "location.camp": "camps, huts, cabins, farms and villages (villages and camps are built from rooms at runtime)",
    "location.structure": "standing structures: Dvergr towers, lighthouse, harbour, viaducts, statues, waymarkers",
    "location.dungeon_entrance": "entrances with an interior 5000 m up: crypts, caves, the Dvergr town and boss halls",
    "location.natural": "natural sites: tar pits, fire holes, nests, the infested tree, giants' remains, sulfur arch",
    "location.dungeon_room": "dungeon rooms (Room prefabs) the DungeonGenerator strings together, every theme",
}
SAMPLE_KEYS = ("prefab", "name", "triangles", "lods", "texture_px", "texel_density", "size_m", "above_ground_m",
               "shader", "maps", "parts", "placement", "behaviour", "fractured", "instance", "billboard")
LOCATION_KEYS = ("prefab", "name", "triangles", "texture_px", "texel_density", "size_m", "materials", "shaders",
                 "reused_parts", "random_spawn", "terrain", "location", "generator", "placement", "room")


def compact(sample, keys):
    """The sample's kept fields, with its materials trimmed to what a page needs."""
    kept = {k: sample[k] for k in keys if k in sample and sample[k] not in (None, [], {})}
    if isinstance(sample.get("materials"), list):
        kept["materials_detail"] = [{k: m[k] for k in ("name", "shader", "main", "texture_px", "tiling", "moss",
                                                        "values", "tint")} for m in sample["materials"]]
    return kept


def prop_stats(samples):
    return {"triangles": five([s["triangles"] for s in samples]),
            "texture_px": five([s["texture_px"] for s in samples]),
            "texel_density": five([s["texel_density"] for s in samples]),
            "longest_m": five([max(s["size_m"]) for s in samples if s["size_m"]]),
            "above_ground_m": five([s.get("above_ground_m") for s in samples]),
            "lod_count": five([len(s["lods"]) for s in samples if s.get("lods")])}


def location_stats(samples):
    return {"triangles": five([s["triangles"] for s in samples if s["triangles"]]),
            "texel_density": five([s["texel_density"] for s in samples]),
            "longest_m": five([max(s["size_m"]) for s in samples if s["size_m"]]),
            "materials": five([s["materials"] for s in samples if s["materials"]]),
            "random_spawns": five([s["random_spawn"]["count"] for s in samples])}


def category_block(key, samples, label, keys, stats):
    names = {s["name"]: s["prefab"] for s in samples}
    refs = [names[n] for n in REFERENCES.get(key, []) if n in names] or [s["prefab"] for s in samples[:4]]
    return {"label": label, "samples": [compact(s, keys) for s in sorted(samples, key=lambda s: s["prefab"])],
            "stats": stats(samples), "references": refs}


def categories():
    """Every env.* and location.* category, keys sorted."""
    found = {}
    for key, samples in props.props().items():
        found[key] = category_block(key, samples, props.LABELS.get(key, key), SAMPLE_KEYS, prop_stats)
    for key, samples in locations.locations().items():
        found[key] = category_block(key, samples, LOCATION_LABELS[key], LOCATION_KEYS, location_stats)
    rooms = locations.rooms()
    found["location.dungeon_room"] = category_block("location.dungeon_room", rooms, LOCATION_LABELS[
        "location.dungeon_room"], LOCATION_KEYS, location_stats)
    found["location.dungeon_room"]["references"] = room_references(rooms)
    return dict(sorted(found.items()))


def room_references(rooms):
    """One room per theme family: the largest-triangle room of the five most common themes."""
    by_theme = {}
    for room in rooms:
        theme = "+".join(room["room"]["theme"]) or "none"
        by_theme.setdefault(theme, []).append(room)
    common = sorted(by_theme.values(), key=len, reverse=True)[:5]
    return [max(group, key=lambda r: r["triangles"])["prefab"] for group in common]


def main():
    data = {"topic": "environment", "generated": GENERATED, "script": "measure/environment.py",
            "categories": categories(), "terrain": biomes.terrain(), "biomes": biomes.biomes(),
            "environments": biomes.environments(), "clutter": zone.clutter(), "vegetation": zone.vegetation(),
            "location_list": zone.locations()}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1, sort_keys=False, default=float)
        handle.write("\n")
    print(f"wrote {OUT}: {len(data['categories'])} categories")


if __name__ == "__main__":
    main()

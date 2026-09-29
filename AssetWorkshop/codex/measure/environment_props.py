"""The world props the zone and clutter systems place (rocks, cliffs, ore, trees, logs, bushes, grass, flowers,
pickables, debris) and what felling and breaking them leaves, measured and sorted into the codex's env.* categories
for environment.py.

Categories come from the game's own data: the script a prefab carries (TreeBase, TreeLog, MineRock5, Pickable,
InstanceRenderer), its size at scale 1, its name for the few the rules cannot tell apart, and the biomes the placement
lists put it in. A rock's fragmented copy (the MineRock5 it swaps to on its first hit) is kept on the whole rock as
'fractured'.
"""
import environment_behaviour as behaviour
import environment_clutter as clutter
import environment_read as read
import environment_zone as zone
import game

ORE = ("copper", "tin", "silver", "mudpile", "obsidian", "meteorite", "leviathan", "giant_", "flametal", "goldvein")
DEBRIS = ("statue", "grave", "frozen", "pot", "ashlands_ruins", "ashlands_arch", "ashlands_pillar", "ice1",
          "iceshore", "skull", "shard")
BUSHES = ("bush", "shrub", "branch")
FLOWERS = ("dandelion", "thistle", "heathflowers")
TREE_BIOMES = {"Meadows": "env.tree_meadows", "Plains": "env.tree_plains", "BlackForest": "env.tree_blackforest",
               "Mountain": "env.tree_blackforest", "Swamp": "env.tree_swamp", "Mistlands": "env.tree_mistlands",
               "AshLands": "env.tree_ashlands", "DeepNorth": "env.tree_deepnorth"}
LABELS = {
    "env.rock_small": "boulders under 5 m that break in one go or into a few pieces",
    "env.rock_large": "big rocks 5 to 30 m across, mostly buried, that break into dozens of hit areas",
    "env.cliff": "cliffs, pillars and arches: rock standing over 20 m above ground or over 30 m across",
    "env.mineable": "ore deposits and mineable remains: copper, tin, silver, iron (mud piles, giant remains), obsidian",
    "env.tree_meadows": "Meadows trees: beech, oak, birch", "env.tree_plains": "Plains trees: autumn birch",
    "env.tree_blackforest": "Black Forest and Mountain conifers: fir and pine",
    "env.tree_swamp": "Swamp trees: the ancient tree and its giant form",
    "env.tree_mistlands": "Mistlands Yggdrasil shoots",
    "env.tree_ashlands": "Ashlands charred trees", "env.tree_deepnorth": "Deep North snow firs and pines",
    "env.tree_small": "saplings and small trees that break in one go (Destructible of type Tree)",
    "env.log_stump": "stumps, felled logs and their halves, old fallen logs",
    "env.bush": "bushes and shrubs that break in one go",
    "env.grass": "grass and ground cover the clutter system instances",
    "env.flower": "flowers: pickable ones and instanced ones", "env.pickable": "pickables: stones, flint, branches, "
    "mushrooms, seeds, berry bushes", "env.debris": "scattered non-plant props: statues, grave stones, frozen bodies, "
    "pots, ruin fragments, ice sheets",
}


def placements():
    """{prefab: merged placement} over every vegetation and clutter entry that places it."""
    merged = {}
    for entry in zone.vegetation() + zone.clutter():
        if not entry["prefab"]:
            continue
        m = merged.setdefault(entry["prefab"], {"biomes": [], "scale": [9e9, 0.0], "entries": 0, "per_zone": 0.0})
        m["biomes"] = sorted(set(m["biomes"]) | set(entry["biomes"]))
        m["scale"] = [min(m["scale"][0], entry["scale"][0]), max(m["scale"][1], entry["scale"][1])]
        m["entries"] += 1
        m["per_zone"] += entry.get("per_zone", [0, entry.get("amount", 0)])[1]
    return merged


def chained(prefabs):
    """The stubs, logs, half logs and fragmented copies the placed prefabs leave behind, followed to the end."""
    found, queue = [], list(prefabs)
    while queue:
        for link in behaviour.links(queue.pop(0)):
            if link not in found and link.startswith("world/") and link.endswith(".prefab"):
                found.append(link)
                queue.append(link)
    return found


def category(sample, placed):
    """The category key of one measured sample (see the module docstring)."""
    name, scripts = sample["name"].lower(), sample["scripts"]
    if "InstanceRenderer" in scripts:
        return "env.flower" if any(f in name for f in FLOWERS) else (
            "env.debris" if "rock" in name and "plant" not in name else "env.grass")
    if any(o in name for o in ORE) or "MineRock" in scripts:
        return "env.mineable"
    if "Pickable" in scripts:
        return "env.flower" if any(f in name for f in FLOWERS) else "env.pickable"
    if "TreeLog" in scripts or any(k in name for k in ("stub", "stump", "log")):
        return "env.log_stump"
    if "TreeBase" in scripts or name.startswith(("swamptree", "yggdrasilroot")):
        return TREE_BIOMES.get((placed.get("biomes") or ["Meadows"])[0], "env.tree_meadows")
    if any(d in name for d in DEBRIS) or sample["shader"] in ("Creature", "Piece", "Standard"):
        return "env.debris"
    if sample["shader"] == "Vegetation":
        small_tree = sample["behaviour"].get("Destructible", {}).get("type") == "Tree"
        return "env.tree_small" if small_tree and not any(b in name for b in BUSHES) else "env.bush"
    return rock_size(sample)


def rock_size(sample):
    """env.rock_small / env.rock_large / env.cliff from the size at scale 1: height above the pivot and footprint."""
    size, base = sample["size_m"] or [0, 0, 0], sample["base_y"] or 0.0
    top, footprint = base + size[1], max(size[0], size[2])
    if top > 20 or footprint > 30 or ("cliff" in sample["name"].lower() and top > 10):
        return "env.cliff"
    return "env.rock_large" if footprint >= 5 else "env.rock_small"


def measure(path, placed):
    """One sample: environment_read's summary (or the instanced reader), its behaviour and its placement."""
    pf_scripts = read.scripts(game.prefab(path))
    sample = clutter.instanced(path) if "InstanceRenderer" in pf_scripts else read.summary(path)
    sample["behaviour"] = behaviour.of(path)
    sample["placement"] = placed.get(path)
    sample["above_ground_m"] = round(sample["base_y"] + sample["size_m"][1], 2) if sample["base_y"] is not None \
        else None
    sample["maps"] = sorted({m for mat in sample["materials"] for m in mat["maps"]})
    return sample


def props():
    """{category: [samples]} over every placed prop and what felling it leaves, each prefab once. A fragmented copy
    (the MineRock5 a rock swaps to on its first hit) is kept on its whole rock as 'fractured', not as a sample."""
    placed = placements()
    paths = sorted(p for p in placed if p.startswith(("world/", "GameElements/Items/pickables")))
    paths += sorted(set(chained(paths)) - set(paths))
    measured = {path: measure(path, placed) for path in paths}
    by_name = {s["name"]: s for s in measured.values()}
    found = {}
    for path, sample in measured.items():
        whole = [s for s in measured.values()
                 if s["behaviour"].get("Destructible", {}).get("spawn_when_destroyed") == sample["name"]]
        if "MineRock5" in sample["behaviour"] and whole and "TreeLog" not in sample["behaviour"]:
            for parent in whole:
                parent["fractured"] = fractured(sample)
            continue
        if sample["triangles"] and sample["name"] in by_name:
            found.setdefault(category(sample, placed.get(path) or {}), []).append(sample)
    return found


def fractured(sample):
    """What the fragmented copy of a rock keeps: its triangles, parts, hit areas and effects."""
    rock = sample["behaviour"]["MineRock5"]
    return {"prefab": sample["prefab"], "triangles": sample["triangles"], "parts": sample["parts"],
            "hit_areas": rock["hit_areas"], "area_longest_m": rock["area_longest_m"],
            "health_per_area": rock["health_per_area"], "tool_tier": rock["tool_tier"], "drops": rock["drops"],
            "hit_effects": rock["hit_effects"], "destroyed_effects": rock["destroyed_effects"]}

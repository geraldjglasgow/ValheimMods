"""How a world prefab behaves when it is hit, felled, broken or picked, read from its game scripts in the reference
export: TreeBase (a standing tree: stub and log it leaves), TreeLog (a felled log and the halves it splits into),
Destructible (one-hit props and rocks that swap to a fragmented copy), MineRock5 (a rock of many hit areas that
crumble one by one), MineRock and Pickable (the model with and without its item, and when it grows back).

    import environment_behaviour as behaviour
    behaviour.of("world/Props/Beech/Beech1.prefab")
    -> {"TreeBase": {health, tool_tier, stub, log, hit_effects, destroyed_effects, drops}}

Effect and drop lists are kept as prefab names, which is what a new asset reuses.
"""
import os
import re

import numpy as np

import game
from workshop import unity

SCRIPTS = ("TreeBase", "TreeLog", "Destructible", "MineRock5", "MineRock", "Pickable")
DESTRUCTIBLE_TYPES = {0: "None", 1: "Default", 2: "Tree", 4: "Character"}


def name_of(value):
    """A reference's prefab name ('vfx_beech_cut'), or None."""
    path = game.path_of(value)
    return os.path.splitext(os.path.basename(path))[0] if path else None


def effects(body, field):
    """Prefab names of an EffectList field ('m_hitEffect'), enabled entries only."""
    match = re.search(rf"^  {field}:\n    m_effectPrefabs:(.*?)(?=^  \w)", body, re.MULTILINE | re.DOTALL)
    if not match:
        return []
    entries = re.findall(r"- m_prefab: (\{[^}]*\})\s+m_enabled: (\d)", match.group(1))
    return [n for n in (name_of(ref) for ref, on in entries if on == "1") if n]


def drops(body, field):
    """{items: [(name, min, max, weight)], count: [min, max], chance} of a DropTable field, or None."""
    match = re.search(rf"^  {field}:\n(.*?)(?=^  \w)", body, re.MULTILINE | re.DOTALL)
    if not match:
        return None
    block = match.group(1)
    items = re.findall(r"m_item: (\{[^}]*\})\s+m_stackMin: (\d+)\s+m_stackMax: (\d+)\s+m_weight: (" +
                       unity.NUMBER + ")", block)
    count = [int(v) for v in re.findall(r"m_drop(?:Min|Max): (\d+)", block)[:2]]
    chance = re.search(r"m_dropChance: (" + unity.NUMBER + ")", block)
    return {"items": [[name_of(ref), int(lo), int(hi), float(w)] for ref, lo, hi, w in items],
            "count": count, "chance": float(chance.group(1)) if chance else None}


def _number(body, field):
    value = unity.field(body, field)
    return float(value) if value else None


def tree_base(body, pf):
    return {"health": _number(body, "m_health"), "tool_tier": _number(body, "m_minToolTier"),
            "stub": name_of(unity.field(body, "m_stubPrefab")), "log": name_of(unity.field(body, "m_logPrefab")),
            "hit_effects": effects(body, "m_hitEffect"), "destroyed_effects": effects(body, "m_destroyedEffect"),
            "drops": drops(body, "m_dropWhenDestroyed")}


def tree_log(body, pf):
    return {"health": _number(body, "m_health"), "tool_tier": _number(body, "m_minToolTier"),
            "sub_log": name_of(unity.field(body, "m_subLogPrefab")),
            "sub_log_points": len(unity.items(body, "m_subLogPoints")),
            "hit_effects": effects(body, "m_hitEffect"), "destroyed_effects": effects(body, "m_destroyedEffect"),
            "drops": drops(body, "m_dropWhenDestroyed")}


def destructible(body, pf):
    kind = int(_number(body, "m_destructibleType") or 0)
    return {"type": DESTRUCTIBLE_TYPES.get(kind, str(kind)), "health": _number(body, "m_health"),
            "tool_tier": _number(body, "m_minToolTier"),
            "spawn_when_destroyed": name_of(unity.field(body, "m_spawnWhenDestroyed")),
            "hit_effects": effects(body, "m_hitEffect"), "destroyed_effects": effects(body, "m_destroyedEffect"),
            "auto_fragments": unity.field(body, "m_autoCreateFragments") == "1"}


def mine_rock5(body, pf):
    """A MineRock5: every collider under it is one hit area, a fragment with its own mesh."""
    areas = [n for n in pf.nodes() if any(k.endswith("Collider") for k, _ in pf.components(n))]
    sizes = []
    for node in areas:
        for kind, b in pf.components(node):
            mesh = game.renderer_mesh(pf, node, b) if kind == "MeshRenderer" else None
            if mesh:
                sizes.append(max(game.mesh_stats(mesh)["size"]) * pf.world_scale(node))
    return {"name": unity.field(body, "m_name"), "health_per_area": _number(body, "m_health"),
            "tool_tier": _number(body, "m_minToolTier"), "hit_areas": len(areas),
            "area_longest_m": _five(sizes), "hit_effects": effects(body, "m_hitEffect"),
            "destroyed_effects": effects(body, "m_destroyedEffect"), "drops": drops(body, "m_dropItems")}


def mine_rock(body, pf):
    return {"name": unity.field(body, "m_name"), "health": _number(body, "m_health"),
            "tool_tier": _number(body, "m_minToolTier"), "hit_effects": effects(body, "m_hitEffect"),
            "destroyed_effects": effects(body, "m_destroyedEffect"), "drops": drops(body, "m_dropItems")}


def pickable(body, pf):
    hide = unity.ref(unity.field(body, "m_hideWhenPicked"))[0]
    hidden = next((pf.path_to(n) for n in pf.nodes() if unity.ref(unity.field(pf.docs[n][1], "m_GameObject"))[0]
                   == hide), None)
    return {"item": name_of(unity.field(body, "m_itemPrefab")), "amount": _number(body, "m_amount"),
            "respawn_minutes": _number(body, "m_respawnTimeMinutes"), "hide_when_picked": hidden,
            "pick_effects": effects(body, "m_pickEffector"), "harvestable": unity.field(body, "m_harvestable") == "1",
            "picked_by_default": unity.field(body, "m_defaultPicked") == "1"}


READERS = {"TreeBase": tree_base, "TreeLog": tree_log, "Destructible": destructible, "MineRock5": mine_rock5,
           "MineRock": mine_rock, "Pickable": pickable}


def of(prefab_path):
    """{script: its behaviour} for the root's world scripts (and a Pickable anywhere, as bushes carry it low)."""
    pf, found = game.prefab(prefab_path), {}
    for node in pf.nodes():
        for kind, body in pf.components(node):
            if kind in READERS and (node == pf.root() or kind == "Pickable") and kind not in found:
                found[kind] = READERS[kind](body, pf)
    return found


def _five(values):
    """{n, min, p25, median, p75, max} rounded, or None."""
    if not values:
        return None
    q = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), "min": round(float(q[0]), 2), "p25": round(float(q[1]), 2),
            "median": round(float(q[2]), 2), "p75": round(float(q[3]), 2), "max": round(float(q[4]), 2)}


LINKS = ("m_stubPrefab", "m_logPrefab", "m_subLogPrefab", "m_spawnWhenDestroyed")


def links(prefab_path):
    """Paths of the prefabs the root's scripts leave behind: a tree's stub and log, a log's halves, a rock's
    fragmented copy. Read by reference, since several prefabs share a name in different folders."""
    pf = game.prefab(prefab_path)
    found = []
    for kind, body in pf.components(pf.root()):
        if kind in ("TreeBase", "TreeLog", "Destructible"):
            found += [p for p in (game.path_of(unity.field(body, f)) for f in LINKS) if p]
    return found

"""Who plays each sound prefab, and through which field: a creature's m_hitEffects, an item's attack m_triggerEffect,
a piece's m_placeEffect, a FootStep entry for grass at a run... Found by following references in the export: the files
that reference a sound prefab's GUID (sfx_scan), then the field path to the reference inside the referring component.
A creature's attacks are items of their own (Greydwarf_attack), so an item that is itself referenced by a creature is
followed one step further, to the creature.

The referring files are 3.2 GB (location prefabs reach 110 MB), so nothing here parses a whole file: the documents
holding a reference are cut out of the text around it, and an owner's components are read from its script references.
"""
import functools
import os
import re

import game  # noqa: I100  (first: it puts the workshop's helpers on the path)
import sfx_scan
from workshop import unity

CONTEXT_KEYS = ("m_name", "m_material", "m_motionType", "m_variant", "m_childTransform", "m_attach", "functionName",
                "time")
OWNER_KINDS = ("Player", "Humanoid", "Character", "ItemDrop", "Piece", "Location", "Ship", "Fish", "Destructible",
               "MineRock", "MineRock5", "TreeBase", "TreeLog", "Pickable", "Door", "Fireplace", "CookingStation",
               "Smelter", "Projectile", "Aoe", "SpawnAbility", "Container", "Bed", "Beehive", "Plant", "WearNTear",
               "BaseAI", "MonsterAI", "AnimalAI", "Tameable", "Procreation", "FootStep", "StatusEffect", "Room",
               "ObjectDB")
SCRIPT_REF = re.compile(r"m_Script: \{fileID: (-?\d+), guid: c1b78fa918b030faf1c1f6f6164daeb2")
HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)", re.MULTILINE)


@functools.lru_cache(maxsize=64)
def text_of(path):
    with open(os.path.join(unity.ROOT, path), encoding="utf-8", errors="replace") as handle:
        return handle.read()


def key_indent(line):
    """(indent of the line's key, key or None, value, starts a list item) for a YAML line."""
    match = re.match(r"^( *)(- )?(?:(\w+):)? ?(.*)$", line)
    spaces, dash, key, value = match.groups()
    return len(spaces) + (2 if dash else 0), key, value, bool(dash)


def chain_at(lines, index):
    """The field path from the component's top to line index, parents first
    ('m_hitEffects/m_effectPrefabs/m_prefab')."""
    indent, key, _, _ = key_indent(lines[index])
    path = [key] if key else []
    for at in range(index - 1, -1, -1):
        where, name, value, _ = key_indent(lines[at])
        if name and where < indent and not value:
            path.append(name)
            indent = where
        if indent <= 2:
            break
    return "/".join(reversed(path))


def item_context(lines, index):
    """The scalar siblings of the list item holding line index (a FootStep entry's m_name, m_material...)."""
    indent, context = key_indent(lines[index])[0], {}
    for step in (-1, 1):
        at = index
        while 0 <= at + step < len(lines) and abs(at - index) < 64:
            at += step
            where, key, value, dash = key_indent(lines[at])
            if where < indent - 2 or (where < indent and not dash) or (step > 0 and dash and where <= indent):
                break
            if key in CONTEXT_KEYS and value:
                context[key[2:] if key.startswith("m_") else key] = value
            if dash and where <= indent and step < 0:
                break
    return context


def documents_with(text, guid):
    """(class ID, body) of every YAML document in text that mentions guid, cut out around each mention."""
    found, at = {}, text.find(guid)
    while at >= 0:
        start = text.rfind("\n--- !u!", 0, at) + 1
        end = text.find("\n--- !u!", at)
        end = len(text) if end < 0 else end
        head = HEADER.match(text, start)
        if head and start not in found:
            found[start] = (int(head.group(1)), text[text.find("\n", start) + 1:end])
        at = text.find(guid, end)
    return list(found.values())


def uses_in(path, guid):
    """[{'file', 'component', 'field', 'context'}] for every reference to guid inside one file."""
    found = []
    for cls, body in documents_with(text_of(path), guid):
        if cls in (1, 4, 1001):
            continue
        component = game.script_class(body) if cls == 114 else {74: "AnimationClip"}.get(cls) or \
            game.NATIVE.get(cls, f"class{cls}")
        lines = body.splitlines()
        for index, line in enumerate(lines):
            if guid in line:
                context = step_entry(lines, index) if component == "FootStep" else item_context(lines, index)
                found.append({"file": path, "component": component, "field": chain_at(lines, index),
                              "context": context})
    return found


def step_entry(lines, index):
    """The FootStep.m_effects entry holding line index: its name, motion flags and ground material flags."""
    start = next((i for i in range(index, -1, -1) if lines[i].startswith("  - m_name:")), None)
    if start is None:
        return {}
    entry = {"name": lines[start].split(":", 1)[1].strip()}
    for line in lines[start + 1:index]:
        match = re.match(r"^    m_(motionType|material): (\d+)", line)
        if match:
            entry[match.group(1)] = int(match.group(2))
    return entry


def owner(path):
    """What a referring file is: its name and the game components in it that say what kind of thing it is."""
    name = path.rsplit("/", 1)[-1].rsplit(".", 1)[0]
    if not path.endswith(".prefab"):
        kind = {".unity": "scene", ".anim": "anim"}.get(os.path.splitext(path)[1], "asset")
        return {"name": name, "kinds": [kind]}
    text, classes = text_of(path), game._classes()
    kinds = sorted({classes.get(int(i), "") for i in set(SCRIPT_REF.findall(text))} & set(OWNER_KINDS))
    facts = {"name": name, "kinds": kinds}
    if "ItemDrop" in kinds:
        facts.update(item_facts(text))
    if {"Humanoid", "Character", "Player"} & set(kinds):
        facts["creature"] = _first(text, r"^  m_name: (\$\w+)")
    if "WearNTear" in kinds:
        facts["material"] = int(_first(text, r"^  m_materialType: (\d+)") or -1)
    return facts


def _first(text, pattern):
    match = re.search(pattern, text, re.MULTILINE)
    return match.group(1).strip() if match else ""


def item_facts(text):
    """An ItemDrop's item type, skill and display name token."""
    return {"item_type": int(_first(text, r"^\s+m_itemType: (\d+)") or 0),
            "skill": int(_first(text, r"^\s+m_skillType: (\d+)") or 0),
            "item": _first(text, r"^\s+m_name: (\$\w+)")}


def usage(scan, records):
    """{sound prefab path: [uses]}, each use with its owner, and for items used by creatures, those creatures."""
    referrers, owners = sfx_scan.referrers(scan), {}
    prefabs = {sfx_scan.guid_of(p): p for p in sorted({r["prefab"] for r in records})}
    table = {p: [] for p in prefabs.values()}
    for path, entry in sorted(scan["files"].items()):
        for guid in set(entry["refs"]) & set(prefabs):
            if path == prefabs[guid]:
                continue
            for use in uses_in(path, guid):
                use["owner"] = owner_of(path, owners)
                table[prefabs[guid]].append(use)
    attach_wielders(table, referrers, owners)
    return table


def owner_of(path, owners):
    """owner(path), worked out once per file."""
    if path not in owners:
        owners[path] = owner(path)
    return owners[path]


def attach_wielders(table, referrers, owners):
    """For every use by an item: the creatures (Humanoid or Character prefabs under Characters/) that carry it, and
    whether the player can have it (ObjectDB lists it)."""
    for uses in table.values():
        for use in uses:
            if "ItemDrop" not in use["owner"]["kinds"] or "wielders" in use["owner"]:
                continue
            wielders, player = [], False
            for path in referrers.get(sfx_scan.guid_of(use["file"]), []):
                facts = owner_of(path, owners)
                player = player or "ObjectDB" in facts["kinds"]
                if path.startswith("Characters/") and set(facts["kinds"]) & {"Humanoid", "Character"} \
                        and "Player" not in facts["kinds"] and path != use["file"]:
                    wielders.append(f"{path.split('/')[1]}/{facts['name']}")
            use["owner"]["wielders"] = sorted(set(wielders))
            use["owner"]["player_item"] = player

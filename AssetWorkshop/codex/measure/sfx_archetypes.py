"""Groups the game's sounds into archetypes. A sound is one set of clips with one set of settings: the same
sfx_fire_loop copied into 150 location prefabs is one sound, with every copy's uses merged. Each sound gets the roles
its uses give it (sfx_roles), and creature sounds are also filed under the creatures that make them.
"""
import math
import os
import re

import sfx_roles


def sound_key(record):
    """What makes two records the same sound: the clips, the random pitch and volume, the reach, the group, looping."""
    zsfx, source = record.get("zsfx") or {}, record.get("source") or {}
    return (tuple(sorted(record["clips"])), zsfx.get("minPitch"), zsfx.get("maxPitch"), zsfx.get("minVol"),
            zsfx.get("maxVol"), source.get("max_distance"), source.get("mixer"), source.get("loop"))


def representative(members, usage):
    """The record that best stands for a sound: its own sfx_ prefab if it has one, else the most used copy."""
    def rank(record):
        own = record["name"] == record["node"].split("/")[-1] and record["name"].lower().startswith(("sfx", "fx"))
        return (not own, -len(usage.get(record["prefab"], [])), len(record["prefab"]))
    return min(members, key=rank)


def merge_uses(members, usage):
    """Every use of every copy's prefab, each (file, component, field, context) once."""
    seen, merged = set(), []
    for record in members:
        for use in usage.get(record["prefab"], []):
            key = (use["file"], use["component"], use["field"], repr(sorted(use["context"].items())))
            if key not in seen:
                seen.add(key)
                merged.append(use)
    return merged


def sounds(records, usage):
    """[{'record', 'copies', 'uses', 'roles', 'creatures'}] for every distinct sound with clips."""
    groups = {}
    for record in records:
        if record["clips"]:
            groups.setdefault(sound_key(record), []).append(record)
    found = []
    for members in groups.values():
        record = representative(members, usage)
        sound = {"record": record, "copies": len(members), "uses": merge_uses(members, usage)}
        sound["roles"] = roles_of(sound)
        sound["creatures"] = creatures_of(sound)
        found.append(sound)
    return found


def label(record):
    """The name a sound goes by: its prefab, and the child holding the sound when that is not the root."""
    node = record["node"].split("/")[-1]
    return record["name"] if node == record["name"] else f"{record['name']}>{node}"


def roles_of(sound):
    """The sound's archetype keys, without the 'sfx.' prefix."""
    record = sound["record"]
    if record.get("source") and record["source"].get("loop"):
        return [sfx_roles.loop_role(record)]
    found = {sfx_roles.use_role(use, record) for use in sound["uses"]} - {None}
    return sorted(found) or [sfx_roles.name_role(record)]


CREATURE_PARTS = ("FootStep", "MonsterAI", "AnimalAI", "Tameable", "Procreation", "AnimationClip")


def creatures_of(sound):
    """'Folder/Prefab' of the creatures that make this sound: owners of creature fields and animation events,
    wielders of creature attack items, and failing those the creature folder the sound lives in ('Troll/*')."""
    names = set()
    for use in sound["uses"]:
        folder = use["file"].split("/")[1] if use["file"].startswith("Characters/") else None
        if sfx_roles.is_attack_item(use):
            names.update(use["owner"].get("wielders") or [f"{folder}/*"])
        elif folder and folder != "Player" and (sfx_roles.is_creature(use) or use["component"] in CREATURE_PARTS):
            names.add(f"{folder}/*" if use["component"] == "AnimationClip" else f"{folder}/{use['owner']['name']}")
    if not names and sound["record"]["prefab"].startswith("Characters/"):
        folder = sound["record"]["prefab"].split("/")[1]
        names.add(f"{folder}/*" if folder != "Player" else "Player/Player")
    return sorted(names)


def by_role(all_sounds):
    """{archetype key: [sounds]}."""
    table = {}
    for sound in all_sounds:
        for role in sound["roles"]:
            table.setdefault(role, []).append(sound)
    return table


def creature_uses(sound):
    """('Folder/Prefab', role) for every creature use of the sound, and a folder fallback for creature sounds nothing
    references directly (their role from the name)."""
    found = set()
    for use in sound["uses"]:
        role = sfx_roles.creature_role(use)
        if not role:
            continue
        folder = use["file"].split("/")[1]
        if sfx_roles.is_attack_item(use):
            found.update((w, role) for w in use["owner"].get("wielders") or [f"{folder}/*"])
        else:
            found.add((f"{folder}/*" if use["component"] == "AnimationClip" else
                       f"{folder}/{use['owner']['name']}", role))
    record = sound["record"]
    if not found and record["prefab"].startswith("Characters/") and not record["prefab"].startswith("Characters/Pl"):
        role = sound["roles"][0]
        found.add((f"{record['prefab'].split('/')[1]}/*", role.split(".", 1)[1] if "." in role else role))
    return found


def by_creature(all_sounds):
    """{creature folder: {role: [(sound, [prefabs making it])]}}: what each creature plays, own sounds and borrowed."""
    table = {}
    for sound in all_sounds:
        makers = {}
        for creature, role in creature_uses(sound):
            folder, prefab = creature.split("/", 1)
            makers.setdefault((folder, role), set()).add(prefab)
        for (folder, role), prefabs in makers.items():
            table.setdefault(folder, {}).setdefault(role, []).append((sound, sorted(prefabs)))
    return table


def played_db(record):
    """The gain in dB the game applies to the clip before distance. With a ZSFX: its random volume, which it writes
    over the AudioSource's own on every play (the mean after Unity clamps AudioSource.volume to 1: a range of 1.3 to
    1.5 plays at 1). Without one: the AudioSource's volume."""
    zsfx, source = record.get("zsfx") or {}, record.get("source") or {}
    if zsfx:
        volume = clamped_mean(zsfx.get("minVol"), zsfx.get("maxVol"))
    else:
        volume = min(source.get("volume") if source.get("volume") is not None else 1.0, 1.0)
    return 20 * _log10(max(volume, 1e-6))


def clamped_mean(low, high):
    """The mean of min(1, v) for v uniform between low and high (either order)."""
    low, high = sorted((low if low is not None else 1.0, high if high is not None else 1.0))
    if high <= 1.0 or high - low < 1e-9:
        return min((low + high) / 2, 1.0)
    if low >= 1.0:
        return 1.0
    return ((1.0 - low) * (1.0 + low) / 2 + (high - 1.0)) / (high - low)


def _log10(value):
    return math.log10(value)


def short(path):
    return os.path.splitext(os.path.basename(path))[0]


def creature_display(name):
    """'Greydwarf_Elite' stays; 'folder:Troll' becomes 'Troll (folder)'."""
    return re.sub(r"^folder:(.*)$", r"\1 (folder)", name)

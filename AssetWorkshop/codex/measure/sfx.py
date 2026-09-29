"""Measures the game's sounds and writes codex/data/sfx.json: every sound prefab's settings (sfx_prefabs), who plays it
(sfx_usage), every clip's numbers (sfx_clips), the mixer (sfx_mixer) and the managers' own clips (sfx_managers),
grouped into archetypes (sfx_archetypes, sfx_roles) and summarised (sfx_stats).

    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx.py [--rescan]

--rescan walks the export again (after a new rip); otherwise the scan, survey and usage caches in codex/out/sfx are
reused and only changed clips are measured again. About 4 minutes from nothing, 30 s from the caches.
"""
import datetime
import json
import os
import sys

import sfx_archetypes
import sfx_clips
import sfx_managers
import sfx_mixer
import sfx_prefabs
import sfx_scan
import sfx_stats
import sfx_usage

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "data", "sfx.json")
OUT = sfx_scan.OUT
WORDS = {"creature": "creature", "weapon": "weapon", "footstep": "player footsteps", "build": "building",
         "piece": "piece", "world": "world object", "combat": "combat", "item": "item", "loop": "loop",
         "music": "music", "ambient": "ambience", "ui": "interface", "projectile": "projectile",
         "status": "status effect",
         "player": "player", "craft": "crafting", "boss": "boss", "water": "water", "ship": "ship", "npc": "trader",
         "other": "other"}


def cached(name, build, rescan):
    """A JSON cache in codex/out/sfx, rebuilt when missing or when rescan is set."""
    path = os.path.join(OUT, name)
    if not rescan and os.path.exists(path):
        with open(path, encoding="utf-8") as handle:
            return json.load(handle)
    value = build()
    os.makedirs(OUT, exist_ok=True)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(value, handle)
    return value


def label(key):
    """'sfx.weapon.sword.swing' -> 'weapon: sword swing'."""
    parts = key.split(".")[1:]
    return f"{WORDS.get(parts[0], parts[0])}: {' '.join(parts[1:]).replace('_', ' ')}".rstrip(": ")


def pseudo(name, clips, volume, mixer, loop, where, base=None):
    """A sound record for clips a manager plays directly, so the same statistics apply: 2D in the given group, or,
    with base (the record of the prefab AudioMan spawns them through), that prefab's ZSFX and AudioSource."""
    record = {"prefab": where, "name": name, "node": name, "clips": [c for c in clips if c], "zsfx": None,
              "source": {"volume": volume, "mixer": mixer, "loop": int(loop), "rolloff": "2D", "spatial": 0.0},
              "timeout_s": None, "networked": False, "components": []}
    if base:
        record.update(zsfx=dict(base["zsfx"], clips=record["clips"]), source=base["source"])
    return record


def spawner(path):
    """The sound record of a prefab AudioMan instantiates and fills with clips (RandomAmbientBase: no clips of its
    own, so the survey skips it)."""
    prefab = sfx_prefabs.game.prefab(path)
    parts = {kind: body for _, kind, body in prefab.all_components() if kind in ("ZSFX", "AudioSource")}
    return {"zsfx": sfx_prefabs.zsfx(parts["ZSFX"]), "source": sfx_prefabs.source(parts["AudioSource"])}


def manager_sounds(scan):
    """{archetype: [sound]} for the music tracks, environment loops, random ambient one-shots, wind and ocean."""
    audio, table = sfx_managers.audio_man(), {}
    where = sfx_managers.AUDIO_MANAGER
    for track in sfx_managers.music():
        table.setdefault("music.track", []).append(
            pseudo(track["name"], track["clips"], track["volume"], "Master/Music", track["loop"], where))
    loops = {}
    for env in sfx_managers.environments(scan):
        loops.setdefault(env["loop"], pseudo(env["name"], [env["loop"]], env["volume"], "Master/Effects/Ambient",
                                             True, env["file"]))
    table["ambient.loop"] = list(loops.values())
    base = spawner(audio["random_prefab"])
    table["ambient.random"] = [pseudo(a["name"], a["clips"], 1.0, "", False, where, base)
                               for a in audio["random_ambients"]]
    table["ambient.wind"] = [pseudo("wind", [audio["wind"]], audio["windMaxVol"], "Master/Effects/Ambient", True,
                                    where)]
    table["ambient.ocean"] = [pseudo("ocean", [audio["ocean"]], audio["oceanVolumeMax"], "Master/Effects/Ambient",
                                     True, where)]
    table["ambient.lava"] = [pseudo("lava noises", audio["lava_noises"], 1.0, "", False, where, base)]
    return table, audio


def as_sounds(records, role):
    return [{"record": r, "copies": 1, "uses": [], "roles": [role], "creatures": []} for r in records]


CREATURE_KEYS = ("prefab", "name", "mixer", "clips", "pitch_min", "pitch_max", "vol_min", "vol_max", "max_distance",
                 "length_s", "played_lufs", "attack_s", "tail_s", "centroid_hz", "flatness", "pitch_hz")


def creature_table(all_sounds, clips):
    """{creature folder: {role: [short sound summaries, each with the prefabs making it]}} for creatures.md (the full
    summaries, clip files included, are in the categories)."""
    table = {}
    for folder, roles in sorted(sfx_archetypes.by_creature(all_sounds).items()):
        table[folder] = {role: [dict({k: v for k, v in sfx_stats.summary(sound, clips).items() if k in CREATURE_KEYS},
                                     makers=makers) for sound, makers in members]
                         for role, members in sorted(roles.items())}
    return table


def build(rescan=False):
    scan = sfx_scan.load(rescan)
    records = cached("prefabs.json", lambda: sfx_prefabs.survey(scan), rescan)
    usage = cached("usage.json", lambda: sfx_usage.usage(scan, records), rescan)
    clips = sfx_clips.measure_all()
    all_sounds = sfx_archetypes.sounds(records, usage)
    groups = sfx_archetypes.by_role(all_sounds)
    managed, audio = manager_sounds(scan)
    for role, members in managed.items():
        groups[role] = as_sounds(members, role)
    categories = {f"sfx.{role}": sfx_stats.category(f"sfx.{role}", members, clips, label(f"sfx.{role}"))
                  for role, members in sorted(groups.items())}
    return {"topic": "sfx", "generated": datetime.date.today().isoformat(), "script": "measure/sfx.py",
            "conventions": sfx_stats_conventions(), "counts": counts(records, all_sounds, clips),
            "mixer": {"groups": sfx_mixer.groups(), "exposed": sfx_mixer.exposed()},
            "managers": {"audio": {k: v for k, v in audio.items() if k != "random_ambients"},
                         "music": sfx_managers.music()},
            "categories": categories, "creatures": creature_table(all_sounds, clips)}


def counts(records, all_sounds, clips):
    return {"records": len(records), "sounds": len(all_sounds), "clips": len(clips),
            "clip_hours": round(sum(c.get("length_s", 0) for c in clips.values()) / 3600, 2),
            "import": import_counts(clips)}


def import_counts(clips):
    """How the game imports its clips, by length: {'<1 s': {'compressed_in_memory': n, ...}, ...}."""
    table = {}
    for numbers in clips.values():
        length = numbers.get("length_s", 0)
        band = "<1 s" if length < 1 else "1-10 s" if length < 10 else ">=10 s"
        table.setdefault(band, {}).setdefault(numbers.get("load"), 0)
        table[band][numbers.get("load")] += 1
    return table


def sfx_stats_conventions():
    import sfx_features
    return {"features": sfx_features.__doc__.strip(), "played_lufs": "momentary_max_lufs + 20 log10(mean ZSFX "
            "volume x AudioSource volume): the level the game plays a clip at before distance and the mixer"}


def dump(value, depth=0, flat_at=4):
    """JSON with one line per entry from depth flat_at down (a sample, a stats row), so the file reads and diffs."""
    if depth >= flat_at or not isinstance(value, (dict, list)) or not value:
        return json.dumps(value, separators=(", ", ": "))
    pad, inner = " " * depth, " " * (depth + 1)
    if isinstance(value, list):
        items = [inner + dump(v, depth + 1, flat_at) for v in value]
        return "[\n" + ",\n".join(items) + f"\n{pad}]"
    items = [f"{inner}{json.dumps(k)}: {dump(v, depth + 1, flat_at)}" for k, v in value.items()]
    return "{\n" + ",\n".join(items) + f"\n{pad}}}"


if __name__ == "__main__":
    result = build(rescan="--rescan" in sys.argv)
    with open(DATA, "w", encoding="utf-8") as handle:
        handle.write(dump(result) + "\n")
    print(f"{DATA}: {len(result['categories'])} archetypes, {result['counts']}")

"""Measures the game's creature rigs and writes codex/data/rigs.json: every distinct skeleton (bone paths, rest
positions in metres, the armature's rest scale, sockets), the avatars on it (humanoid bone maps or generic), the
animator controllers its creatures run (parameters, layers, states, tags, transitions), every clip those use (length,
loop, root settings, events, humanoid or generic) and the vocabulary the game's code relies on: parameter names,
state tags, event functions, attack triggers.

    python codex/measure/rigs.py      (after creatures.py: it reads data/creatures.json for each creature's rig)
"""
import collections
import datetime
import json
import os
import re
import zlib

import numpy as np

import game
import rigs_clips
import rigs_controller
import rigs_read
import rigs_summary
from creatures import five, write

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "data"))
FAMILIES = {
    "rig.player": ("the player's skeleton: Player, Skeleton, FallenWarrior, ShadowPerson", ["player"]),
    "rig.humanoid": ("bipeds on humanoid avatars: the game's humanoid clips retarget onto them",
                     ["greydwarf", "troll", "draugr", "goblin", "goblinshaman", "goblinbrute",
                      "goblinbrutebros", "dverger", "elaking", "fenring", "charred_melee", "surtling", "gd_king",
                      "goblinking", "hildir", "haldor", "bogwitch", "odin", "jotunwarrior", "morgen",
                      "neck", "stonegolem", "wraith", "bonemass", "barka"]),
    "rig.quadruped": ("legged animals on generic avatars (four legs, the insects six or eight)",
                      ["wolf", "boar", "deer", "lox", "moose", "bjorn", "asksvin", "seal", "hare", "chicken", "tick",
                       "seeker", "seekerbrute", "eikthyr"]),
    "rig.flyer": ("winged and hovering creatures", ["bat", "mistile", "hatchling", "volture", "gjall", "crow",
                                                   "hugin", "valkyrie", "fallenvalkyrie", "dragon"]),
    "rig.serpent": ("bone chains: serpents, leeches, roots", ["serpent", "leech", "tentaroot"]),
    "rig.other": ("rigs of their own: blobs, ghosts, a broom, the late bosses",
                  ["blob", "bogwitchkvastur", "ghost", "ghost_void", "writhan", "abomination", "fader",
                   "frozenking_0", "frozenking_p3", "seekerqueen"]),
}


NOTES = {"rest_m": "bone positions in the pose saved in the game prefab (the bind pose for most, a crouch for the "
                   "Greydwarf) at the prefab's own scale, metres, Unity axes (y up, z forward), the root at the origin",
         "armature_scale": "the armature transform's local scale in the game prefab (a Blender FBX in centimetres "
                           "gives 100; creatures scale it)"}


def main():
    with open(os.path.join(DATA, "creatures.json"), encoding="utf-8") as handle:
        creatures = [s for c in json.load(handle)["categories"].values() for s in c["samples"]]
    by_rig = collections.defaultdict(list)
    for s in creatures:
        if s["rig"] != "static":
            by_rig[s["rig"]].append(s)
    rigs = {key: rig(key, members) for key, members in sorted(by_rig.items())}
    controllers = {p: rigs_controller.read(p) for p in sorted({c for r in rigs.values() for c in r["controllers"]})}
    clips = {p: rigs_clips.clip(p) for p in sorted({c for ctl in controllers.values() for c in ctl["clips"]})}
    resolve_parameters(clips, controllers)
    data = {"topic": "rigs", "generated": datetime.date.today().isoformat(), "script": "measure/rigs.py",
            "categories": families(rigs), "controllers": {p: compact(c) for p, c in controllers.items()},
            "clips": clips, "vocabulary": vocabulary(creatures, controllers, clips),
            "attacks": attack_summary(rigs_summary.attacks(controllers, clips)),
            "locomotion": rigs_summary.locomotion(controllers), "notes": NOTES}
    data["vocabulary"]["footsteps"] = rigs_summary.footstep_coverage(data["locomotion"], clips)
    write(os.path.join(DATA, "rigs.json"), data)
    print(f"rigs: {len(rigs)} rigs, {len(controllers)} controllers, {len(clips)} clips")


def attack_summary(attacks):
    """Every attack state, and the numbers over the creatures' ones (the player's weapon attacks left out): how long
    they play, when the first hit lands, how fast they blend in."""
    creature = [a for a in attacks if a["controller"] != "Player_animator"]
    return {"states": attacks, "creature_states": len(creature),
            "played_s": five(a["played_s"] for a in creature),
            "hit_fraction": five(a["hit_fraction"] for a in creature),
            "first_hit_s": five(a["hits_s"][0] for a in creature if a["hits_s"]),
            "blend_in_s": five(a["blend_in_s"] for a in creature),
            "with_trail": sum(1 for a in creature if a["trail"])}


def rig(key, members):
    """One rig: its skeleton read from its namesake creature, and what its creatures run on it."""
    lead = next((s for s in members if s["name"].lower() == key), members[0])
    skeleton = rigs_read.skeleton(game.prefab(lead["prefab"]))
    avatars = [rigs_clips.avatar(a) for a in sorted({(s.get("animator") or {}).get("avatar") for s in members} -
                                                    {None})]
    human = next((a["map"] for a in avatars if a["humanoid"]), None)
    body = rigs_summary.proportions(skeleton["bones"], human, lead.get("top_m")) if human else None
    return {"rig": key, "prefab": lead["prefab"], "creatures": [s["name"] for s in members],
            "categories": sorted({s["category"] for s in members if s["category"]}),
            "controllers": sorted({(s.get("animator") or {}).get("controller") for s in members} - {None}),
            "avatars": avatars, "body": body,
            "attack_triggers": sorted({a for s in members for items in s["items"].values() for i in items
                                       for a in i["attacks"]}),
            "proportions": rigs_read.proportions(skeleton["bones"]), **skeleton}


def families(rigs):
    """The rig families as codex categories: their rigs and the numbers over them."""
    out = {}
    for key, (label, names) in FAMILIES.items():
        members = [rigs[n] for n in names if n in rigs]
        stats = {"bones": five(r["bone_count"] for r in members),
                 "sockets": five(len(r["sockets"]) for r in members),
                 "top_m": five(r["proportions"]["top_m"] for r in members)}
        out[key] = {"label": label, "samples": members, "stats": stats,
                    "references": [r["prefab"] for r in members[:6]]}
    missing = set(rigs) - {n for _, names in FAMILIES.values() for n in names}
    if missing:
        print("rigs: not in rigs.FAMILIES:", ", ".join(sorted(missing)))
    return out


def compact(controller):
    """A controller as the data keeps it: clip paths shortened to clip names inside the states."""
    layers = [{**layer, "states": [{**s, "motion": _motion_names(s["motion"])} for s in layer["states"]]}
              for layer in controller["layers"]]
    return {**controller, "layers": layers}


def _motion_names(motion):
    if isinstance(motion, str):
        return os.path.splitext(os.path.basename(motion))[0]
    if isinstance(motion, dict):
        return {**motion, "children": [{**c, "motion": _motion_names(c["motion"])} for c in motion["children"]]}
    return motion


def resolve_parameters(clips, controllers):
    """Names the parameter curves the export left as 'typetree_0x<crc32>_...' from the controllers' parameters."""
    names = {zlib.crc32(p["name"].encode()): p["name"] for c in controllers.values() for p in c["parameters"]}
    for clip in clips.values():
        found = [re.match(r"typetree_0x([0-9A-Fa-f]{8})", p) for p in clip["parameters"]]
        clip["parameters"] = sorted(names.get(int(m.group(1), 16), p) if m else p
                                    for m, p in zip(found, clip["parameters"]))


def vocabulary(creatures, controllers, clips):
    """What the game's code and clips agree on: parameters and tags by how many controllers use them, event
    functions by how many clips call them (with the strings they pass), parameter curves, attack triggers."""
    parameters, tags = collections.Counter(), collections.Counter()
    for c in controllers.values():
        parameters.update({f"{p['name']} ({p['type']})" for p in c["parameters"]})
        tags.update({s["tag"] for layer in c["layers"] for s in layer["states"] if s["tag"]})
    curves = collections.Counter(p for clip in clips.values() for p in clip["parameters"])
    return {"parameters": ranked(parameters), "tags": ranked(tags),
            "events": events(clips), "parameter_curves": ranked(curves),
            "attack_triggers": triggers(creatures), "clip_lengths_s": five(c["length_s"] for c in clips.values()),
            "humanoid_clips": int(np.sum([c["humanoid"] for c in clips.values()])), "clip_count": len(clips)}


def events(clips):
    """{function: {clips, data}}: how many clips call each event function and the strings passed most often."""
    found = collections.defaultdict(lambda: {"clips": 0, "data": collections.Counter()})
    for clip in clips.values():
        for function in {e["function"] for e in clip["events"]}:
            found[function]["clips"] += 1
        found_data = [(e["function"], e["data"]) for e in clip["events"] if e["data"]]
        for function, data in found_data:
            found[function]["data"][data] += 1
    ordered = sorted(found.items(), key=lambda kv: (-kv[1]["clips"], kv[0]))
    return {f: {"clips": v["clips"], "data": ranked(v["data"], 12)} for f, v in ordered}


def ranked(counter, limit=None):
    """A Counter as a dict, most common first, ties by name (so the file is the same on every run)."""
    return dict(sorted(counter.items(), key=lambda kv: (-kv[1], kv[0]))[:limit])


def triggers(creatures):
    """{attack trigger: [creatures]} from the attack items each creature carries."""
    found = collections.defaultdict(set)
    for s in creatures:
        for item in (i for items in s["items"].values() for i in items):
            for attack in item["attacks"]:
                found[attack].add(s["name"])
    return {t: sorted(n) for t, n in sorted(found.items())}


if __name__ == "__main__":
    main()

"""Builds a sound set (sfx/sounds/<set>.py): renders every sound's variations with its recipe, levels them for the game
prefab they will play through, writes them, measures them and compares them with the game.

    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/sfx/build.py rootling [--only ecp_rootling_idle] [--no-ogg]

Writes sfx/out/<set>/ (gitignored):
    <sound>_<n>.wav          16-bit 44.1 kHz, what goes into the bundle (Unity encodes it to Vorbis itself)
    <sound>_<n>.ogg          the same as OGG Vorbis, for listening and sharing
    manifest.json            per sound: its clips, the game prefab to copy, the ZSFX overrides, the archetype
    report.md                every sound's numbers against its archetype's measured ranges
    <sound>_compare.png      its variations' spectrograms above the nearest game clips
"""
import importlib
import json
import os
import sys

import numpy as np

import compare
from kit import io, level, targets

HERE = os.path.dirname(os.path.abspath(__file__))


def load_set(name):
    return importlib.import_module(f"sounds.{name}")


def render(sound):
    """The sound's variations, each levelled for the prefab it will play through."""
    loop = sound.get("loop", False)
    target = targets.clip_target(sound["archetype"], sound["prefab"], loop, sound.get("volume"))
    clips = []
    for seed in range(sound["variations"]):
        raw = np.asarray(sound["make"](seed * 7919 + 11), dtype=float)
        clips.append(level.match(raw, target, loop))
    return clips, target


def write(folder, sound, clips, ogg=True):
    """Writes the clips; returns their names (no extension), <sound>_<n> counted from 1."""
    names = []
    for number, clip in enumerate(clips, 1):
        name = f"{sound['name']}_{number}"
        io.write_wav(os.path.join(folder, f"{name}.wav"), clip)
        if ogg:
            io.write_ogg(os.path.join(folder, f"{name}.ogg"), clip)
        names.append(name)
    return names


def entry(sound, names, target, module):
    """What the bundle build and a mod need to know about one sound."""
    found = {"name": sound["name"], "archetype": sound["archetype"], "prefab": sound["prefab"], "clips": names,
             "target_lufs": round(target, 2), "loop": sound.get("loop", False),
             "caption": sound.get("caption", getattr(module, "CAPTION", None)), "source": "synthesised"}
    for key in ("pitch", "volume"):
        if key in sound:
            found[key] = sound[key]
    return found


def build(name, only=None, ogg=True):
    """Builds every sound of the set (or only the named ones, keeping the others' manifest entries), then writes the
    manifest and the report for the whole set."""
    module, folder = load_set(name), os.path.join(HERE, "out", name)
    os.makedirs(folder, exist_ok=True)
    built = previous(folder) if only else {}
    for sound in module.SOUNDS:
        if only and sound["name"] not in only:
            continue
        clips, target = render(sound)
        built[sound["name"]] = entry(sound, write(folder, sound, clips, ogg), target, module)
        print(f"{sound['name']}: {len(clips)} clips at {target:.1f} LUFS")
    order = [s["name"] for s in module.SOUNDS if s["name"] in built]
    manifest = {"set": module.SET, "module": name, "sounds": [built[n] for n in order]}
    with open(os.path.join(folder, "manifest.json"), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=1)
    compare.main(name)
    return folder


def previous(folder):
    """The sound entries of the set's last manifest, by name."""
    try:
        with open(os.path.join(folder, "manifest.json"), encoding="utf-8") as handle:
            return {s["name"]: s for s in json.load(handle)["sounds"]}
    except (OSError, ValueError, KeyError):
        return {}


if __name__ == "__main__":
    args = sys.argv[1:]
    only = set(args[args.index("--only") + 1].split(",")) if "--only" in args else None
    print(build(args[0], only, "--no-ogg" not in args))

"""Contact sheets of the game's sounds, for looking at them: one per archetype (codex/out/sfx/sheets/<key>.png: the
reference sounds' clips first, then a spread of the rest) and one per creature (codex/out/sfx/creatures/<folder>.png:
a row per role). Everything goes to the gitignored out folder; nothing here is committed.

    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_sheets.py [key-or-folder ...]
"""
import json
import os
import sys

import sfx_clips
import sfx_draw
import sfx_scan

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data", "sfx.json")
ROLES = ("idle", "alert", "attack", "attack_hit", "hurt", "death", "footstep", "move")


def load():
    with open(DATA, encoding="utf-8") as handle:
        return json.load(handle)


def pick(samples, references, limit):
    """Clips for an archetype sheet: the references' first, then one clip each from a spread of the other sounds."""
    order = [s for r in references for s in samples if s["prefab"] == r.split(":")[0]][:len(references)]
    rest = [s for s in samples if s not in order]
    step = max(1, len(rest) // max(1, limit - len(order)))
    order += rest[::step]
    files = []
    for sample in order:
        files.extend((f, sample["name"]) for f in sample.get("files", [])[:2 if sample in order[:6] else 1])
    return files[:limit]


def archetype_sheet(key, entry, clips, limit=36):
    files = pick(entry["samples"], entry.get("references", []), limit)
    span = sfx_draw.span_for([clips.get(f, {}).get("length_s", 1.0) for f, _ in files])
    tiles = [sfx_draw.clip_tile(f, span, clips.get(f), f"{name}") for f, name in files]
    title = f"{key}: {entry['sounds']} sounds, {entry['clips']} clips; span {span:.2f} s; references first"
    return sfx_draw.sheet(tiles, title)


def files_by_sound(data):
    """{(prefab, name): clip files} from the categories (the creature entries do not repeat them)."""
    return {(s["prefab"], s["name"]): s.get("files", []) for entry in data["categories"].values()
            for s in entry["samples"]}


def creature_sheet(folder, roles, clips, files_of, per_role=6):
    """A row per role, up to per_role clips each."""
    tiles, span_clips = [], []
    rows = [(role, roles[role]) for role in ROLES if role in roles]
    for _, sounds in rows:
        span_clips += [clips.get(f, {}).get("length_s", 1.0) for s in sounds
                       for f in files_of.get((s["prefab"], s["name"]), [])[:1]]
    span = sfx_draw.span_for(span_clips)
    for role, sounds in rows:
        files = [(f, s["name"]) for s in sounds for f in files_of.get((s["prefab"], s["name"]), [])[:2]][:per_role]
        row = [sfx_draw.clip_tile(f, span, clips.get(f), f"{role}: {name}") for f, name in files]
        if not row:
            continue
        tiles += row + [sfx_draw.Image.new("RGB", row[0].size, (0, 0, 0))] * (per_role - len(row))
    return sfx_draw.sheet(tiles, f"{folder}: rows {', '.join(r for r, _ in rows)}; span {span:.2f} s", per_role)


def main(only):
    data, clips = load(), sfx_clips.load_cache()
    for folder in ("sheets", "creatures"):
        os.makedirs(os.path.join(sfx_scan.OUT, folder), exist_ok=True)
    for key, entry in data["categories"].items():
        if not only or key in only:
            archetype_sheet(key, entry, clips).save(os.path.join(sfx_scan.OUT, "sheets", f"{key}.png"))
    files_of = files_by_sound(data)
    for folder, roles in data["creatures"].items():
        if (not only or folder in only) and any(r in roles for r in ROLES):
            creature_sheet(folder, roles, clips, files_of).save(os.path.join(sfx_scan.OUT, "creatures", f"{folder}.png"))


if __name__ == "__main__":
    main(set(sys.argv[1:]))

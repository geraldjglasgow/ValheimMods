"""Writes codex/data/shaders.json: every shader the game's materials use, its properties (from the exported dummies),
how many of the materials the game's prefabs draw use it and on what kinds of asset, which texture slots and keywords
those materials fill, the values they set, and the game's recolour mechanisms (shaders_recolour).

    python codex/measure/shaders.py
"""
import collections
import datetime
import json
import os

import numpy as np

import game
import shaders_props
import shaders_recolour
import shaders_usage

DATA = os.path.join(game.WORKSHOP, "codex", "data", "shaders.json")
BUILTIN = {"builtin_46": "Standard", "builtin_45": "Standard (Specular setup)", "builtin_47": "Autodesk Interactive",
           "builtin_0": "(missing shader)"}


def materials_by_shader():
    """{shader stem: [(material path, usage entry or None, material)]} over every .mat in the export."""
    use = shaders_usage.scan()
    grouped = collections.defaultdict(list)
    for path in game.find("**/*.mat"):
        try:
            info = game.material(path)
        except (OSError, ValueError):
            continue
        grouped[info["shader"]].append((path, use.get(path), info))
    return grouped


def describe(stem, entries, props):
    """One shader's entry: identity, counts, kinds, slots, keywords, values."""
    drawn = [(p, u, i) for p, u, i in entries if u and set(u["kinds"]) - {"other"}]
    kinds = collections.Counter(k for _, u, _ in drawn for k in set(u["kinds"]) - {"other"})
    return {"name": props.get("name") or BUILTIN.get(stem, stem), "file": props.get("file"),
            "materials_in_export": len(entries), "materials_drawn": len(drawn),
            "kinds": dict(kinds.most_common()),
            "renderers": dict(collections.Counter(r for _, u, _ in drawn for r in u["renderers"]).most_common()),
            "texture_slots": dict(collections.Counter(s for _, _, i in drawn for s, t in i["textures"].items() if t)
                                  .most_common()),
            "keywords": dict(collections.Counter(k for _, _, i in drawn for k in i["keywords"]).most_common()),
            "values": _values(drawn, props.get("properties", [])),
            "examples": [p for p, u, _ in sorted(drawn, key=lambda e: -e[1]["prefabs"])[:8]],
            "properties": props.get("properties", [])}


def _values(drawn, properties):
    """Median and range of each float property the drawn materials set, and how many set it away from default. A
    built-in shader has no exported dummy: its materials' own float names stand in for the property list."""
    if not properties:
        names = sorted({name for _, _, info in drawn for name in info["floats"]})
        properties = [{"name": n, "type": "Float", "default": "", "ui_only": False} for n in names]
    found = {}
    for prop in properties:
        if prop["ui_only"] or prop["type"] in ("2D", "3D", "Cube", "Vector", "Color"):
            continue
        values = [i["floats"][prop["name"]] for _, _, i in drawn if prop["name"] in i["floats"]]
        if values:
            default = _number(prop["default"])
            found[prop["name"]] = {"n": len(values), "median": round(float(np.median(values)), 3),
                                   "min": round(float(min(values)), 3), "max": round(float(max(values)), 3),
                                   "changed": sum(1 for v in values if default is None or abs(v - default) > 1e-4)}
    return found


def _number(text):
    try:
        return float(text)
    except ValueError:
        return None


def main():
    props = shaders_props.all_shaders()
    grouped = materials_by_shader()
    shaders = {stem: describe(stem, entries, props.get(stem, {})) for stem, entries in sorted(grouped.items())}
    unused = {stem: {"name": p["name"], "file": p["file"], "properties": p["properties"]}
              for stem, p in props.items() if stem not in shaders}
    data = {"topic": "shaders", "generated": datetime.date.today().isoformat(), "script": "measure/shaders.py",
            "shaders": dict(sorted(shaders.items(), key=lambda kv: -kv[1]["materials_drawn"])),
            "unused_dummies": unused, "recolour": shaders_recolour.survey()}
    with open(DATA, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1)
    print(DATA)


if __name__ == "__main__":
    main()

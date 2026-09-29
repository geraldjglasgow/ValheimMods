"""Measures every creature of the game (enemies, animals, bosses, NPCs, the player) from the reference export and
writes codex/data/creatures.json: per category the creatures with their triangles, renderers, materials, shaders and
colour settings, textures, texel density, size, capsule, LODs, ragdoll, star-level looks, carried items and animator
scripts; the numbers over each category's distinct models; and the families of creatures that share a rig, with how
each member differs from the family's first (texture swap, colour shift, scale, other meshes).

    python codex/measure/creatures.py

Also writes codex/out/creatures/render_list.json, the visually distinct prefabs for creatures_render.py.
"""
import datetime
import json
import os

import numpy as np

import creatures_colour
import creatures_kinds as kinds
import creatures_one
import game
import creatures_read as cr
import rigs_read

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "data", "creatures.json"))
RENDER_LIST = os.path.normpath(os.path.join(HERE, "..", "out", "creatures", "render_list.json"))
STATS = ("triangles", "texture_px", "texel_density", "height_m", "bones", "renderer_count", "material_count")


def main():
    raw = [creatures_one.read(p) for p in cr.creature_prefabs()]
    rigs = rigs_read.rig_keys({c["prefab"]: c["rig_signature"] for c in raw})
    bases = base_of(raw)
    samples = [sample(c, rigs[c["prefab"]], bases[c["name"]]) for c in raw]
    unknown = [s["name"] for s in samples if not s["category"]]
    if unknown:
        print("creatures: not in creatures_kinds.CATEGORIES:", ", ".join(unknown))
    data = {"topic": "creatures", "generated": datetime.date.today().isoformat(), "script": "measure/creatures.py",
            "categories": categories(samples), "families": families(samples),
            "notes": {"size": "bind pose (T-pose for most bipeds), metres, Unity axes (y up, z forward), at the "
                              "prefab's own scale; star levels scale further (level_effects)",
                      "variant_of": "a creature drawing the same main mesh as another (texture, colour or scale "
                                    "swap); left out of the category stats"}}
    write(DATA, data)
    write(RENDER_LIST, render_list(samples))
    print(f"creatures: {len(samples)} creatures in {len(data['categories'])} categories -> {DATA}")


def base_of(raw):
    """{name: name of the creature whose main mesh it shares, or None}: of the creatures drawing one main mesh, the
    base is the first in rigs_read.PREFERRED, else the shortest name."""
    by_mesh = {}
    for c in raw:
        mesh = next((r["mesh"] for r in c["renderers"] if r["node"] == c.get("main_renderer")), None)
        if mesh:
            by_mesh.setdefault(mesh, []).append(c["name"])
    bases = {c["name"]: None for c in raw}
    for names in by_mesh.values():
        rank = lambda n: rigs_read.PREFERRED.index(n) if n in rigs_read.PREFERRED else len(rigs_read.PREFERRED)
        first = min(names, key=lambda n: (rank(n), len(n), n))
        bases.update({n: first for n in names if n != first})
    return bases


def sample(c, rig, base):
    """One creature as the data keeps it."""
    character = c.get("character") or {}
    keep = ("triangles", "renderer_count", "material_count", "shaders", "texture_px", "texel_density", "height_m",
            "top_m", "bottom_m", "size_m", "bones", "lods", "capsule", "ragdoll", "level_effects", "animator",
            "anim_event", "footstep", "sync", "attach", "ai")
    out = {"prefab": c["prefab"], "name": c["name"], "category": kinds.category_of(c["name"]), "rig": rig,
           "variant_of": base, "scripts": c["scripts"],
           "character": {k: character.get(k) for k in ("name", "faction", "boss", "health", "walkSpeed",
                                                       "runSpeed", "turnSpeed", "flying", "canSwim")}}
    out.update({k: c.get(k) for k in keep})
    out["materials"] = materials(c)
    out["colour"] = colour(c)
    out["renderers"] = [{k: r[k] for k in ("node", "kind", "mesh", "triangles", "bones", "lod", "drawn",
                                           "texture_px", "texel_density")} |
                        {"materials": [m["material"] for m in r["materials"]]} for r in c["renderers"]]
    out["items"] = {k: [{f: i[f] for f in ("item", "name", "attacks", "animation_state", "attach")} for i in v]
                    for k, v in (c.get("items") or {}).items()}
    return out


def materials(c):
    """The creature's materials once each, first seen first."""
    seen = {}
    for r in c["renderers"]:
        for m in r["materials"]:
            seen.setdefault(m["material"], dict(m))
    return list(seen.values())


def colour(c):
    """The colour numbers of the main renderer's albedo (creatures_colour.texture_colour), or None."""
    main = next((r for r in c["renderers"] if r["node"] == c.get("main_renderer")), None)
    for material in (main or {}).get("materials", []):
        texture = game.material(material["path"])["textures"].get("_MainTex")
        if texture:
            return {"texture": texture} | (creatures_colour.texture_colour(texture) or {})
    return None


def five(values):
    """{n, min, p25, median, p75, max} of the numbers given (None left out), or None when there are none."""
    values = [v for v in values if v is not None]
    if not values:
        return None
    q = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), "min": _round(q[0]), "p25": _round(q[1]), "median": _round(q[2]),
            "p75": _round(q[3]), "max": _round(q[4])}


def _round(value):
    return int(value) if float(value).is_integer() else round(float(value), 2)


def categories(samples):
    """{key: {label, samples, stats, references}}; stats over the category's distinct models only."""
    out = {}
    for key, (label, _) in kinds.CATEGORIES.items():
        members = [s for s in samples if s["category"] == key]
        names = {s["name"] for s in members}
        models = [s for s in members if s["triangles"] and s["variant_of"] not in names]
        stats = {name: five(s.get(name) for s in models) for name in STATS}
        refs = [s["prefab"] for name in kinds.REFERENCES[key] for s in members if s["name"] == name]
        out[key] = {"label": label, "samples": members, "stats": stats, "references": refs}
    return out


def families(samples):
    """{rig: {creatures, base, differences}}: every rig with more than one creature, each member against the first
    that is not a variant (the family's base)."""
    out = {}
    for rig in sorted({s["rig"] for s in samples if s["rig"] != "static"}):
        members = [s for s in samples if s["rig"] == rig]
        if len(members) < 2:
            continue
        base = min(members, key=lambda s: (s["variant_of"] is not None, len(s["name"]), s["name"]))
        out[rig] = {"creatures": [s["name"] for s in members], "base": base["name"],
                    "differences": {s["name"]: difference(base, s) for s in members if s is not base}}
    return out


def difference(base, other):
    """How a creature differs from its family's base: scale, meshes, materials, main texture, colour settings."""
    meshes = lambda s: {os.path.basename(r["mesh"]) for r in s["renderers"] if r["drawn"] and r["lod"] == 0}
    mats = lambda s: {m["material"] for m in s["materials"]}
    textures = lambda s: {m["main_texture"] for m in s["materials"] if m["main_texture"]}
    hsv = lambda s: {m["material"]: {k: m["floats"][k] for k in ("_Hue", "_Saturation", "_Value")
                                     if m["floats"].get(k)} for m in s["materials"]}
    ratio = (other["height_m"] or 0) / base["height_m"] if base.get("height_m") else None
    return {"height_ratio": round(ratio, 3) if ratio else None,
            "meshes_added": sorted(meshes(other) - meshes(base)),
            "meshes_removed": sorted(meshes(base) - meshes(other)),
            "materials_added": sorted(mats(other) - mats(base)),
            "textures_added": sorted(textures(other) - textures(base)),
            "hsv": {k: v for k, v in hsv(other).items() if v}, "triangles": other["triangles"] - base["triangles"]}


def render_list(samples):
    """Prefabs worth a render: one per distinct look (drawn meshes and materials, height)."""
    seen = {}
    for s in samples:
        drawn = tuple(sorted((r["mesh"], tuple(r["materials"])) for r in s["renderers"] if r["drawn"] and not r["lod"]))
        if s["triangles"]:
            seen.setdefault((drawn, s["height_m"]), s["prefab"])
    return list(seen.values())


def write(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1)
        handle.write("\n")


if __name__ == "__main__":
    main()

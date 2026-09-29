"""Summaries across the build-menu pieces for the codex pages: the material sets, textures shared between pieces, the
Custom/Piece shader's switches and how many pieces turn each on, snap point patterns per category, and the
destruction, hit and placement effects per WearNTear material.

    pieces_summary.summaries(samples)     # {"sets": …, "textures": …, "piece_shader": …, "snap": …, "effects": …}
"""
import collections
import functools
import os
import re

import numpy as np

import game
from workshop import unity

PLAIN = {"Transform", "GameObject", "MeshRenderer", "MeshFilter", "BoxCollider", "MeshCollider", "SphereCollider",
         "CapsuleCollider", "Piece", "WearNTear", "ZNetView", "LODGroup", "Rigidbody", "ParticleSystem",
         "ParticleSystemRenderer", "Light", "AudioSource", "EffectArea"}
SWITCHES = ("_TriplanarMap", "_TriplanarLocalPos", "_TriplanarScale", "_ValueNoise", "_ValueNoiseVertex",
            "_RippleDistance", "_RippleFreq", "_AddRain", "_AddSnow", "_MoveableObject", "_TwoSidedNormals", "_Cull",
            "_Cutoff", "_Metallic", "_MetallicAlphaGloss", "_Glossiness", "_BumpScale")


def _median(values):
    values = [v for v in values if v is not None]
    return round(float(np.median(values)), 1) if values else None


def by_set(rows):
    """{material set: {n, triangles, texture_px, texel_density (medians), prefabs}} of a category's samples."""
    groups = collections.defaultdict(list)
    for row in rows:
        groups[row["set"]].append(row)
    return {key: {"n": len(group), "triangles": _median(r["triangles"] for r in group),
                  "texture_px": _median(r["texture_px"] for r in group),
                  "texel_density": _median(r["texel_density"] for r in group),
                  "prefabs": sorted(r["name"] for r in group)} for key, group in sorted(groups.items())}


def sets(samples):
    """{set: {n, material types, shaders, main textures with how many pieces use each, worn materials}}."""
    groups = collections.defaultdict(list)
    for row in samples:
        groups[row["set"]].append(row)
    found = {}
    for key, group in sorted(groups.items()):
        textures = collections.Counter(t for r in group for t in r["textures"])
        found[key] = {"n": len(group), "material_types": _count(r["material_type"] for r in group),
                      "shaders": _count(s for r in group for s in r["shaders"]),
                      "textures": dict(textures.most_common(8)),
                      "categories": _count(r["category"] for r in group)}
    return found


def _count(values):
    return dict(sorted(collections.Counter(v for v in values if v is not None).items(), key=lambda kv: -kv[1]))


def textures(samples):
    """Main textures drawn by more than one piece: {texture: {size, pieces}}, most shared first."""
    users = collections.defaultdict(set)
    for row in samples:
        for texture in row["textures"]:
            users[texture].add(row["name"])
    shared = sorted(((t, sorted(n)) for t, n in users.items() if len(n) > 1), key=lambda kv: (-len(kv[1]), kv[0]))
    return {t: {"size": list(game.texture_size(t) or ()), "pieces": len(n), "examples": n[:12]} for t, n in shared}


@functools.lru_cache(maxsize=None)
def switches(material):
    """The Custom/Piece switches a material sets: floats and ints by name, and which texture slots it fills."""
    text = unity.read(material)
    found = {}
    for section in ("m_Ints", "m_Floats"):
        block = re.search(rf"^    {section}:\n((?:      .*\n)*)", text, re.MULTILINE)
        for key, number in re.findall(r"^      (_\w+): (-?[\d.eE-]+)$", block.group(1) if block else "", re.MULTILINE):
            found[key] = float(number)
    info = game.material(material)
    found["slots"] = sorted(k for k, v in info["textures"].items() if v)
    found["emission"] = max(info["colors"].get("_EmissionColor", (0, 0, 0))[:3])
    return found


def piece_shader(samples):
    """How the build-menu pieces' Custom/Piece materials set each switch: {switch: {value: pieces}} with examples."""
    materials = collections.defaultdict(set)
    for row in samples:
        for material in _material_paths(row):
            if game.material(material)["shader"] == "Piece":
                materials[material].add(row["name"])
    table = collections.defaultdict(lambda: collections.defaultdict(set))
    for material, names in materials.items():
        values = switches(material)
        for key in SWITCHES:
            table[key][values.get(key)].update(names)
        for slot in ("_MetallicTex", "_EmissionMap"):
            table[slot + " set"][slot in values["slots"]].update(names)
        table["emissive"][values["emission"] > 0].update(names)
    return {"materials": len(materials),
            "switches": {k: {str(v): {"pieces": len(n), "examples": sorted(n)[:8]} for v, n in sorted(
                table[k].items(), key=lambda kv: -len(kv[1]))} for k in sorted(table)}}


def _material_paths(row):
    """The .mat paths behind a sample's material names (the scan keeps names only)."""
    return [m for m in (_find_material(row["prefab"], name) for name in row["materials"]) if m]


def _find_material(prefab, name):
    for path in _prefab_materials(prefab):
        if path and os.path.basename(path)[:-4] == name:
            return path
    return None


@functools.lru_cache(maxsize=None)
def _prefab_materials(prefab):
    p = game.prefab(prefab)
    return tuple(sorted({m for _, k, b in p.all_components() if k in ("MeshRenderer", "SkinnedMeshRenderer")
                         for m in game.renderer_materials(b) if m}))


def snap(samples):
    """Snap point patterns per category: {category: {'count x extent': pieces}} with examples."""
    found = collections.defaultdict(lambda: collections.defaultdict(list))
    for row in samples:
        extent = "x".join(f"{v:g}" for v in row["snap"]["extent_m"]) if row["snap"]["extent_m"] else "-"
        found[row["category"]][f"{row['snap_points']} points, extent {extent} m"].append(row["name"])
    return {k: {p: {"pieces": len(n), "examples": sorted(n)[:6]} for p, n in sorted(v.items(), key=lambda kv: -len(
        kv[1]))} for k, v in sorted(found.items())}


def effects(samples):
    """Per WearNTear material: the destroyed, hit, switch and place effects, each with how many pieces use it."""
    found = collections.defaultdict(lambda: collections.defaultdict(collections.Counter))
    for row in samples:
        for kind, names in row["effects"].items():
            found[row["material_type"] or "none"][kind][" + ".join(sorted(names)) or "(none)"] += 1
    return {m: {k: dict(c.most_common()) for k, c in sorted(kinds.items())} for m, kinds in sorted(found.items())}


def damage(samples):
    """How damage is drawn: {state change: pieces} for worn and broken, and fragment modes."""
    rows = [r for r in samples if r["states"]]
    return {"worn": _count(r["states"]["worn"] if isinstance(r["states"]["worn"], str) else "?" for r in rows),
            "broken": _count(r["states"]["broken"] for r in rows),
            "fragments": _count(r["fragments"]["mode"] for r in rows if r["fragments"]),
            "snow_meshes": _count(r["snow"] for r in rows),
            "lod_levels": _count(r["lod_levels"] for r in samples)}


def place_boxes(samples):
    """{placement effect: {shape, box_m, pieces}}: the vfx_Place_* prefabs emit dust from a shape sized to a
    footprint, so a new piece takes the one whose box matches its own."""
    users = collections.Counter(e for row in samples for e in row["effects"]["place"] if e.startswith("vfx_Place"))
    found = {}
    for name, count in users.most_common():
        hits = game.find(f"**/{name}.prefab")
        text = unity.read(hits[0]) if hits else ""
        shape = re.search(r"ShapeModule:\n(?:.*\n)*?    type: (\d+)\n(?:.*\n)*?    m_Scale: (\{[^}]*\})", text)
        found[name] = {"shape": SHAPES.get(shape.group(1), shape.group(1)) if shape else None,
                       "box_m": list(unity.numbers(shape.group(2))) if shape else None, "pieces": count}
    return found


SHAPES = {"0": "sphere", "4": "cone", "5": "box", "6": "mesh", "13": "mesh renderer", "15": "box shell",
          "16": "box edge", "10": "circle", "18": "rectangle"}


def icons(samples):
    """Medians and 10th/90th percentiles of the build-menu icons' measures (see pieces_icons.measure)."""
    looks = [r["icon_look"] for r in samples if r.get("icon_look")]
    found = {"n": len(looks), "px": _count(l["px"] for l in looks),
             "clear_corners": sum(1 for l in looks if l["clear_corners"])}
    for key in ("fill", "luminance", "saturation", "soft_edge"):
        values = [l[key] for l in looks]
        found[key] = {q: round(float(np.percentile(values, p)), 3) for q, p in (("p10", 10), ("median", 50),
                                                                              ("p90", 90))}
    for key, index in (("box_long_side", None), ("box_width", 0), ("box_height", 1), ("centre_x", 0),
                       ("centre_y", 1)):
        values = [max(l["box"]) if index is None else l["box" if key.startswith("box") else "centre"][index]
                  for l in looks]
        found[key] = {q: round(float(np.percentile(values, p)), 3) for q, p in (("p10", 10), ("median", 50),
                                                                              ("p90", 90))}
    return found


def summaries(samples):
    return {"sets": sets(samples), "textures": textures(samples), "piece_shader": piece_shader(samples),
            "snap": snap(samples), "effects_by_material": effects(samples), "place_effects": place_boxes(samples),
            "damage": damage(samples), "icons": icons(samples)}

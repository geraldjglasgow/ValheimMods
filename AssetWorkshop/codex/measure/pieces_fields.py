"""The game-script fields of a building piece that decide how it looks, snaps and breaks: snap points, colliders, LOD
groups, WearNTear (states, wear material, effects, fragments), the Piece's placement effect, lights and particles,
and the station, fire, door, bed and container scripts. Reads a pieces_scan.Scan.

    pieces_fields.snap_points(scan)     # {"count", "extent_m", "points"}
    pieces_fields.wear(scan)            # {"material", "health", "states", "effects", "fragments", ...}
"""
import functools
import os
import re

import numpy as np

import game
import pieces_mesh
from workshop import unity

MATERIAL_TYPES = ("Wood", "Stone", "Iron", "HardWood", "Marble", "Ashstone", "Ancient", "Ice", "Timberwood")
COLLIDERS = ("BoxCollider", "MeshCollider", "SphereCollider", "CapsuleCollider")
LIGHT_TYPES = {"0": "spot", "1": "directional", "2": "point", "3": "area"}


@functools.lru_cache(maxsize=None)
def layers():
    """The game's layer names by number, from the export's TagManager."""
    path = os.path.join(unity.ROOT, "..", "ProjectSettings", "TagManager.asset")
    with open(path, encoding="utf-8") as handle:
        text = handle.read()
    block = text[text.index("  layers:"):text.index("m_SortingLayers")]
    return [line[4:].strip() for line in block.splitlines()[1:] if line.startswith("  - ")]


def block(body, name):
    """The lines under a nested field ('  m_hitEffect:') up to the next field at its indentation."""
    match = re.search(rf"^( *){name}:\n", body, re.MULTILINE)
    if not match:
        return ""
    indent, lines = len(match.group(1)), []
    for line in body[match.end():].splitlines():
        if line.strip() and len(line) - len(line.lstrip()) <= indent and not line.lstrip().startswith("- "):
            break
        lines.append(line)
    return "\n".join(lines)


def effects(body, name):
    """The prefab names of an EffectList field's enabled entries, '+attach' marking ones parented to the piece."""
    found = []
    for entry in block(body, name).split("- m_prefab:")[1:]:
        path = game.path_of(entry.splitlines()[0])
        if path and re.search(r"m_enabled: 1", entry):
            attach = "+attach" if re.search(r"m_attach: 1", entry) else ""
            found.append(os.path.basename(path)[:-7] + attach)
    return found


def value(body, name):
    """A top-level scalar field as a number when it is one."""
    raw = unity.field(body, name)
    try:
        return float(raw) if "." in raw or "e" in raw.lower() else int(raw)
    except ValueError:
        return raw


def point(scan, node):
    """A node's origin in the piece's frame."""
    return [round(float(v), 3) for v in pieces_mesh.to_root(scan.prefab, node)[:3, 3]]


def snap_points(scan):
    """The root's children tagged snappoint (Piece.GetSnapPoints reads only those): positions and their extent."""
    root = scan.prefab.root()
    points = [point(scan, n) for n in scan.prefab.children(root)
              if unity.field(scan.prefab.game_object(n), "m_TagString") == "snappoint"]
    if not points:
        return {"count": 0, "extent_m": None, "points": []}
    array = np.array(points)
    extent = [round(float(v), 3) for v in array.max(axis=0) - array.min(axis=0)]
    return {"count": len(points), "extent_m": extent, "points": sorted(points)}


def colliders(scan):
    """[{kind, tag, layer, trigger}] of every collider, with a box's size in the piece's frame."""
    rows = []
    for node, _, kind, body in scan.all_components():
        if kind not in COLLIDERS:
            continue
        go = scan.prefab.game_object(node)
        layer = int(unity.field(go, "m_Layer") or 0)
        row = {"kind": kind, "node": scan.prefab.name(node), "tag": unity.field(go, "m_TagString"),
               "layer": layers()[layer] if layer < len(layers()) else layer,
               "trigger": unity.field(body, "m_IsTrigger") == "1", "active": scan.active(node)}
        if kind == "BoxCollider":
            size = np.array(unity.numbers(unity.field(body, "m_Size")))
            row["size_m"] = [round(float(v), 3) for v in np.abs(pieces_mesh.to_root(scan.prefab, node)[:3, :3] @ size)]
        if kind == "MeshCollider":
            row["convex"] = unity.field(body, "m_Convex") == "1"
        rows.append(row)
    return rows


def lods(scan):
    """[{levels, heights, fade}] of every LOD group: screen heights where each level ends (fractions of the screen)."""
    rows = []
    for node, _, kind, body in scan.all_components():
        if kind == "LODGroup":
            heights = [round(float(h), 4) for h in re.findall(r"screenRelativeHeight: ([\d.eE-]+)", body)]
            rows.append({"node": scan.prefab.name(node), "levels": len(heights), "heights": heights,
                         "crossfade": unity.field(body, "m_AnimateCrossFading") == "1"})
    return rows


def wear(scan):
    """WearNTear: material type, health, flags, the new/worn/broken setup, fragments and effects. None without one."""
    body = scan.first("WearNTear")
    if body is None:
        return None
    material = value(body, "m_materialType")
    return {"material": MATERIAL_TYPES[material] if isinstance(material, int) and material < 9 else material,
            "health": value(body, "m_health"), "supports": value(body, "m_supports"),
            "burnable": value(body, "m_burnable"), "no_roof_wear": value(body, "m_noRoofWear"),
            "ash_immune": value(body, "m_ashDamageImmune"), "min_tool_tier": value(body, "m_minToolTier"),
            "states": _states(scan), "snow": _snow(scan, body), "fragments": _fragments(scan, body),
            "effects": {key: effects(body, "m_" + key + "Effect") for key in ("destroyed", "hit", "switch")}}


def _states(scan):
    """How the look changes with damage: which states exist, whether worn/broken are separate objects, and whether
    each swaps the mesh, the material or both against the new look."""
    new, found = scan.state("new"), {}
    names = {key: scan.states.get(key) for key in ("new", "worn", "broken")}
    for key in ("worn", "broken"):
        rows = scan.state(key)
        if names[key] is None:
            found[key] = "none"
        elif names[key] == names["new"]:
            found[key] = "same object"
        else:
            meshes = sorted(str(r["mesh"]) for r in rows) != sorted(str(r["mesh"]) for r in new)
            materials = sorted(str(r["materials"]) for r in rows) != sorted(str(r["materials"]) for r in new)
            found[key] = {(True, True): "mesh and material", (True, False): "mesh", (False, True): "material",
                          (False, False): "identical"}[(meshes, materials)]
    found["wet"] = names.get("wet") is not None or scan.states.get("wet") is not None
    return found


def _snow(scan, body):
    """Snow cover: how many snow meshes (SnowMesh shader) the piece carries and whether WearNTear swaps them."""
    count = sum(1 for r in scan.renderers() if r["snow"])
    worn = unity.ref(unity.field(body, "m_snowWorn"))[0] != "0"
    return {"meshes": count, "wired": unity.ref(unity.field(body, "m_snow"))[0] != "0", "per_state": worn}


def _fragments(scan, body):
    """What flies apart when the piece is destroyed: its own fragment roots (pre-cut chunks or the planks it was
    combined from) or, without roots, every visible mesh renderer."""
    roots = scan.states["fragments"]
    if unity.field(body, "m_autoCreateFragments") != "1":
        return {"mode": "none", "roots": 0, "pieces": 0}
    if not roots:
        return {"mode": "visible renderers", "roots": 0, "pieces": len(scan.visual())}
    pieces = [r for r in scan.renderers() if any(scan.under(r["node"], root) for root in roots)]
    names = " ".join(scan.prefab.name(root) for root in roots).lower()
    mode = "pre-cut chunks" if "destruct" in names or "broken" in names else "source parts"
    return {"mode": mode, "roots": len(roots), "pieces": len(pieces)}


def placement(scan):
    """The Piece's own fields that matter for looks and placement."""
    body = scan.first("Piece")
    return {"place_effect": effects(body, "m_placeEffect"), "ground_piece": value(body, "m_groundPiece"),
            "clip_ground": value(body, "m_clipGround"), "clip_everything": value(body, "m_clipEverything"),
            "random_rotation": value(body, "m_randomInitBuildRotation"), "comfort": value(body, "m_comfort"),
            "comfort_group": value(body, "m_comfortGroup"), "usage": value(body, "m_usage"),
            "icon": game.path_of(unity.field(body, "m_icon"))}


def lights(scan):
    """[{type, colour, intensity, range, shadows, flicker, lod}] of every Light, active or waiting under a fire."""
    rows = []
    for node, _, kind, body in scan.all_components():
        if kind != "Light":
            continue
        colour = [round(v, 3) for v in unity.numbers(unity.field(body, "m_Color"))[:3]]
        shadows = re.search(r"m_Shadows:\n\s+m_Type: (\d)", body)
        row = {"node": scan.prefab.path_to(node).split("/", 1)[-1], "type": LIGHT_TYPES.get(
            unity.field(body, "m_Type"), "other"), "colour": colour, "intensity": value(body, "m_Intensity"),
            "range": value(body, "m_Range"), "shadows": ("none", "hard", "soft")[int(shadows.group(1))] if shadows
            else None}
        for _, kind2, extra in scan.components(node):
            if kind2 == "LightFlicker":
                row["flicker"] = {k: value(extra, "m_" + k) for k in ("flickerIntensity", "flickerSpeed", "movement")}
            if kind2 == "LightLod":
                row["lod"] = {k: value(extra, "m_" + k) for k in ("lightDistance", "shadowDistance")}
        rows.append(row)
    return rows


def particles(scan):
    """Names of the particle systems, with the path from the piece's root."""
    return sorted(scan.prefab.path_to(n).split("/", 1)[-1] for n, _, k, _ in scan.all_components()
                  if k == "ParticleSystem")

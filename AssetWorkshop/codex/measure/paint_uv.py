"""Where each of the game's albedo textures sits on its meshes: which prefab, mesh and submesh draw it, at what scale
in the game, the texels its UVs cover (so empty atlas space stays out of the statistics) and its texel density.

    import paint_uv
    uses = paint_uv.uses()                         # {texture path: [{"prefab", "kind", "mesh", "submesh", "scale"}]}
    mask = paint_uv.coverage(texture, (w, h))      # bool (h, w): texels some UV triangle covers
    paint_uv.density(use, 128)                     # pixels per metre of surface for that use

Skinned meshes are measured through their first bone: a vertex reaches the world as bone matrix x bind pose, so the
scale is the bone's scale in the prefab times the bind pose's.
"""
import functools
import json
import os
import re

import numpy as np
from PIL import Image, ImageDraw

import game
import shaders_usage

CACHE = os.path.join(game.WORKSHOP, "codex", "out", "paint", "texture_uses.json")
KINDS = ("item", "piece", "creature", "env", "location", "ship", "player")


def uses(fresh=False):
    """{albedo texture path: [use, ...]} over every prefab of a real kind (cached in codex/out/paint)."""
    if not fresh and os.path.exists(CACHE):
        with open(CACHE, encoding="utf-8") as handle:
            return json.load(handle)
    found = {}
    for path, kind in shaders_usage.prefab_kinds().items():
        if kind in KINDS:
            for use in _prefab_uses(path, kind):
                _merge(found.setdefault(use.pop("texture"), {}), use)
    found = {texture: list(merged.values()) for texture, merged in found.items()}
    with open(CACHE, "w", encoding="utf-8") as handle:
        json.dump(found, handle, indent=0, sort_keys=True)
    return found


def _merge(merged, use):
    """One entry per (mesh, submesh, scale): the first prefab, every kind, how many renderer slots draw it."""
    key = (use["mesh"], use["submesh"], round(use["scale"], 4))
    entry = merged.setdefault(key, dict(use, kind=[], count=0))
    if use["kind"] not in entry["kind"]:
        entry["kind"] = sorted(entry["kind"] + [use["kind"]])
    entry["count"] += 1


def _prefab_uses(path, kind):
    """One use per (renderer, material slot) of the prefab whose material has a main texture."""
    prefab, found = game.prefab(path), []
    for node, cls, body in prefab.all_components():
        if cls not in ("MeshRenderer", "SkinnedMeshRenderer"):
            continue
        mesh = game.renderer_mesh(prefab, node, body)
        if not mesh or not mesh.endswith(".asset"):
            continue
        scale = _scale(prefab, node, body, mesh, cls)
        for slot, material in enumerate(game.renderer_materials(body)):
            texture = _main_texture(material)
            if texture:
                found.append({"texture": texture, "prefab": path, "kind": kind, "mesh": mesh, "submesh": slot,
                              "scale": scale, "material": material})
    return found


def _main_texture(material):
    if not material:
        return None
    try:
        return game.material(material)["textures"].get("_MainTex")
    except (OSError, ValueError):
        return None


def _scale(prefab, node, body, mesh, cls):
    """The mesh's size in the game against its own units."""
    if cls == "MeshRenderer":
        return prefab.world_scale(node)
    bones = [game.unity.ref(item)[0] for item in game.unity.items(body, "m_Bones")]
    bind = _bind_scale(mesh)
    if not bones or bind is None or not prefab.is_local(bones[0]):
        return prefab.world_scale(node)
    return prefab.world_scale(bones[0]) * bind


@functools.lru_cache(maxsize=None)
def _bind_scale(mesh):
    """The mean column length of the first bind pose's 3x3 part, or None for an unskinned mesh."""
    text = game.unity.read(mesh)
    match = re.search(r"m_BindPose:\n  - ((?: *e\d\d: \S+\n)+)", text)
    if not match:
        return None
    values = dict(re.findall(r"e(\d\d): (\S+)", match.group(1)))
    columns = [np.linalg.norm([float(values[f"{row}{col}"]) for row in "012"]) for col in "012"]
    return float(np.mean(columns))


@functools.lru_cache(maxsize=None)
def submesh(mesh, index):
    """(positions, uv, triangles) of one submesh (the whole mesh when the index is past its submeshes)."""
    positions, uv, triangles = game.mesh_arrays(mesh)
    text = game.unity.read(mesh)
    block = text[text.index("m_SubMeshes:"):text.index("m_Shapes:")] if "m_Shapes:" in text else ""
    ranges = [(int(a), int(b), int(c)) for a, b, c in
              re.findall(r"firstByte: (\d+)\n\s+indexCount: (\d+)\n\s+topology: \d+\n\s+baseVertex: (\d+)", block)]
    if index >= len(ranges) or len(ranges) < 2:
        return positions, uv, triangles
    wide = re.search(r"m_IndexFormat: 1", text) is not None
    first, count, base = ranges[index]
    flat = triangles.ravel()
    start = first // (4 if wide else 2)
    return positions, uv, flat[start:start + count - count % 3].reshape(-1, 3) + base


def coverage(texture, size, texture_uses=None):
    """bool (h, w): the texels any use's UV triangles cover, wrapped into the unit square."""
    width, height = size
    canvas = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(canvas)
    for use in (texture_uses if texture_uses is not None else uses().get(texture, [])):
        _, uv, tris = submesh(use["mesh"], use["submesh"])
        if uv is None or not len(tris):
            continue
        for triangle in uv[tris]:
            _draw_wrapped(draw, triangle, width, height)
    return np.asarray(canvas) > 0


def _draw_wrapped(draw, triangle, width, height):
    """One UV triangle drawn into the unit square; a triangle wider than the square covers all of it."""
    local = triangle - np.floor(triangle.min(axis=0))
    if (local.max(axis=0) - local.min(axis=0) >= 1.0).any():
        draw.rectangle((0, 0, width, height), fill=255)
        return
    for du in (0.0, -1.0):
        for dv in (0.0, -1.0):
            points = [((u + du) * width, (1.0 - (v + dv)) * height) for u, v in local]
            draw.polygon(points, fill=255)


def density(use, texture_px):
    """Texture pixels per metre of surface for one use: sqrt(uv area x px^2 / world area) / scale."""
    positions, uv, tris = submesh(use["mesh"], use["submesh"])
    if uv is None or not len(tris) or not use["scale"]:
        return None
    edges = positions[tris[:, 1:]] - positions[tris[:, :1]]
    world = np.linalg.norm(np.cross(edges[:, 0], edges[:, 1]), axis=1).sum() / 2
    a, b = uv[tris[:, 1]] - uv[tris[:, 0]], uv[tris[:, 2]] - uv[tris[:, 0]]
    area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2
    return float(texture_px * np.sqrt(area / world) / use["scale"]) if world > 0 and area > 0 else None


if __name__ == "__main__":
    table = uses(fresh=True)
    print(len(table), "albedo textures on", sum(len(v) for v in table.values()), "renderer slots")

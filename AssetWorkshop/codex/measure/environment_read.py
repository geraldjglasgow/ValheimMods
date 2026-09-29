"""Measures one world prefab (a rock, tree, bush, pickable, location or dungeon room) from the reference export: what
it draws at each LOD, how big it stands in the game, its materials and how many texture pixels land on a metre of
its surface. Plain Python on top of game.py; environment.py and environment_locations.py call summary().

    import environment_read as read
    read.summary("world/Props/Beech/Beech1.prefab")
    -> {prefab, name, scripts, lods: [{screen_height, triangles}], triangles, size_m, materials, texel_density, ...}

Transforms are Unity's, the root's own position and rotation dropped and its scale kept, as prefab.load places a
prefab. Sizes come from each mesh's bounding box carried through its transforms; texel density is worked out per
submesh against the texture its material draws with, then weighted by surface area.
"""
import functools
import re

import numpy as np

import game
from workshop import unity

DRAWN = ("MeshRenderer", "SkinnedMeshRenderer")
INTERIOR_Y = 1000.0     # a location's dungeon interior sits 5000 m up; bounds keep the outside only


def component_ids(pf, node):
    """[(fileID, class name, body)] of a node's components: the ids a LODGroup names its renderers by."""
    found = []
    for item in unity.items(pf.game_object(node), "m_Component"):
        file_id = unity.ref(item)[0]
        if file_id in pf.docs:
            cls, body = pf.docs[file_id]
            name = game.script_class(body) if cls == 114 else game.NATIVE.get(cls, f"class{cls}")
            found.append((file_id, name, body))
    return found


def local_matrix(body, is_root):
    """4x4 local transform of a Transform body (Unity axes); the root keeps only its scale."""
    x, y, z, w = unity.numbers(unity.field(body, "m_LocalRotation")) or (0, 0, 0, 1)
    rotation = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                         [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                         [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    scale = np.array(unity.numbers(unity.field(body, "m_LocalScale")) or (1, 1, 1))
    matrix = np.eye(4)
    if is_root:
        matrix[:3, :3] = np.diag(scale)
        return matrix
    matrix[:3, :3] = rotation * scale
    matrix[:3, 3] = unity.numbers(unity.field(body, "m_LocalPosition")) or (0, 0, 0)
    return matrix


def world_matrices(pf):
    """{node: 4x4 matrix from the node's mesh space to the prefab's space}, parents before children."""
    root, found = pf.root(), {}
    for node in pf.nodes():
        parent = unity.ref(unity.field(pf.docs[node][1], "m_Father"))[0]
        own = local_matrix(pf.docs[node][1], node == root)
        found[node] = found[parent] @ own if parent in found else own
    return found


def active_nodes(pf):
    """Nodes whose GameObject and every parent's is active."""
    active, root = set(), pf.root()
    for node in pf.nodes():
        parent = unity.ref(unity.field(pf.docs[node][1], "m_Father"))[0]
        if pf.active(node) and (node == root or parent in active):
            active.add(node)
    return active


def renderers(pf):
    """[{id, node, kind, mesh, materials}] of every enabled mesh renderer on an active node."""
    active, found = active_nodes(pf), []
    for node in pf.nodes():
        for file_id, kind, body in component_ids(pf, node):
            if kind in DRAWN and node in active and unity.field(body, "m_Enabled") != "0":
                mesh = game.renderer_mesh(pf, node, body)
                if mesh:
                    found.append({"id": file_id, "node": node, "kind": kind, "mesh": mesh,
                                  "materials": game.renderer_materials(body)})
    return found


def lod_groups(pf):
    """[{fade_mode, billboard, size, lods: [{screen_height, renderer ids}]}] of every LODGroup in the prefab."""
    found = []
    for node in pf.nodes():
        for _, kind, body in component_ids(pf, node):
            if kind == "LODGroup":
                found.append(_lod_group(body))
    return found


def _lod_group(body):
    heights = [float(h) for h in re.findall(r"screenRelativeHeight: (" + unity.NUMBER + ")", body)]
    blocks = re.split(r"- screenRelativeHeight:", body)[1:]
    lods = [{"screen_height": round(h, 4), "renderers": re.findall(r"renderer: \{fileID: (-?\d+)", block)}
            for h, block in zip(heights, blocks)]
    return {"fade_mode": int(float(unity.field(body, "m_FadeMode") or 0)),
            "billboard": unity.field(body, "m_LastLODIsBillboard") == "1",
            "size": float(unity.field(body, "m_Size") or 0), "lods": lods}


@functools.lru_cache(maxsize=None)
def submeshes(mesh_path):
    """[(first index, index count, local AABB centre, extent)] of a mesh's triangle submeshes."""
    text = unity.read(mesh_path)
    body = text[text.index("m_SubMeshes:"):text.index("m_Shapes:")] if "m_Shapes:" in text else text
    found = []
    pattern = (r"firstByte: (\d+)\s+indexCount: (\d+)\s+topology: (\d+)[\s\S]*?m_Center: (\{[^}]*\})\s+"
               r"m_Extent: (\{[^}]*\})")
    wide = game._nested(text, "m_IndexFormat") == "1"
    for first, count, topology, centre, extent in re.findall(pattern, body):
        if topology == "0":
            found.append((int(first) // (4 if wide else 2), int(count), unity.numbers(centre), unity.numbers(extent)))
    return found


def corners(centre, extent, matrix):
    """The eight corners of a box (centre, half-size) carried through a 4x4 matrix."""
    signs = np.array([[x, y, z] for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)], dtype=float)
    points = np.array(centre) + signs * np.array(extent)
    return (matrix[:3, :3] @ points.T).T + matrix[:3, 3]


def bounds(parts, matrices):
    """(min, max) over the submesh boxes of the given renderers in the prefab's space, or None."""
    points = [corners(c, e, matrices[r["node"]]) for r in parts for _, _, c, e in submeshes(r["mesh"])]
    points = [p for p in points if p[:, 1].min() < INTERIOR_Y]
    if not points:
        return None
    stacked = np.vstack(points)
    return stacked.min(axis=0), stacked.max(axis=0)


def mean_scale(matrix):
    """The uniform scale a 4x4 matrix applies (cube root of its determinant)."""
    return abs(np.linalg.det(matrix[:3, :3])) ** (1 / 3)


def surface(mesh_path, sub, scale):
    """(world area m^2, uv area) of one submesh (first index, count) of a mesh, or None when it will not decode."""
    try:
        positions, uv, tris = game.mesh_arrays(mesh_path)
    except (ValueError, IndexError, KeyError):
        return None
    tris = tris[sub[0] // 3: (sub[0] + sub[1]) // 3]
    if uv is None or not len(tris) or tris.max() >= len(positions):
        return None
    cross = np.cross(positions[tris[:, 1]] - positions[tris[:, 0]], positions[tris[:, 2]] - positions[tris[:, 0]])
    world = np.linalg.norm(cross, axis=1).sum() / 2 * scale * scale
    a, b = uv[tris[:, 1]] - uv[tris[:, 0]], uv[tris[:, 2]] - uv[tris[:, 0]]
    return world, np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2


TRAIT_FLOATS = ("_Cutoff", "_Cull", "_Glossiness", "_MossAlpha", "_MossBlend", "_MossTransition", "_AddSnow",
                "_AddRain", "_SwaySpeed", "_SwayDistance", "_Height", "_RippleSpeed", "_RippleDistance",
                "_PushDistance", "_TriplanarMap", "_TriplanarScale", "_BumpScale", "_TwoSidedNormals", "_SphereNormals",
                "_TerrainColorScale", "_FadeDistanceMin", "_FadeDistanceMax", "_UseGlossMap", "_UV2Height", "_UVScale")


@functools.lru_cache(maxsize=None)
def material_info(path):
    """What the codex keeps of a material: name, shader, main texture size, maps set, and the world-shader values
    that decide the look (moss, snow, sway, cutout, culling, triplanar), plus its tint as stored."""
    mat = game.material(path)
    main = mat["textures"].get("_MainTex")
    size = game.texture_size(main) if main else None
    tiling = unity.material(path)["albedo"][1]
    return {"name": mat["name"], "shader": mat["shader"], "main": main, "texture_px": list(size) if size else None,
            "tiling": [round(t, 3) for t in tiling],
            "maps": sorted(slot for slot, tex in mat["textures"].items() if tex),
            "moss": mat["textures"].get("_MossTex"),
            "values": {k[1:]: v for k, v in mat["floats"].items() if k in TRAIT_FLOATS},
            "tint": [round(c, 3) for c in mat["colors"].get("_Color", (1, 1, 1, 1))],
            "moss_colour": [round(c, 3) for c in mat["colors"].get("_MossColor", ())],
            "keywords": mat["keywords"]}


def lod_split(pf, drawn):
    """(LOD groups, [renderers drawn at the closest view], [[renderers] per LOD of the first group])."""
    groups, in_lod = lod_groups(pf), {}
    for group in groups:
        for i, lod in enumerate(group["lods"]):
            for rid in lod["renderers"]:
                in_lod.setdefault(rid, i)      # a renderer listed in LOD0 and LOD1 is drawn from the closest view
    near = [r for r in drawn if in_lod.get(r["id"], 0) == 0]
    per_lod = [[r for r in drawn if r["id"] in lod["renderers"]] for lod in groups[0]["lods"]] if groups else []
    return groups, near, per_lod


def triangles(parts):
    """Triangles drawn by these renderers."""
    return sum(game.mesh_stats(r["mesh"])["triangles"] for r in parts)


def part_rows(parts, matrices):
    """One row per material over these renderers: {material, shader, triangles, area_m2, texel_density}, each
    submesh measured against its own material's main texture (tiling included)."""
    rows = {}
    for r in parts:
        scale = mean_scale(matrices[r["node"]])
        for sub, mat in zip(submeshes(r["mesh"]), r["materials"]):
            info = material_info(mat) if mat else None
            if not info:
                continue
            row = rows.setdefault(mat, {"material": info["name"], "shader": info["shader"], "triangles": 0,
                                        "area_m2": 0.0, "_texel": 0.0, "_area": 0.0})
            row["triangles"] += sub[1] // 3
            repeat = triplanar_repeat(info)
            if repeat:
                row.update(triplanar_repeat_m=round(repeat, 2),
                           texel_density=round(max(info["texture_px"]) / repeat, 1))
                continue
            _add_surface(row, info, surface(r["mesh"], sub, scale))
    return [_finish(row) for row in rows.values()]


def _add_surface(row, info, got):
    """Adds one submesh's (world area, uv area) to a part row, weighting its texel density by area."""
    if got and got[0]:
        row["area_m2"] += got[0]
        if info["texture_px"]:
            w, h = info["texture_px"][0] * info["tiling"][0], info["texture_px"][1] * info["tiling"][1]
            row["_texel"] += np.sqrt(abs(got[1] * w * h) / got[0]) * got[0]
            row["_area"] += got[0]


def triplanar_repeat(info):
    """Metres over which a world-projected (triplanar) material repeats its texture, or None for a UV-mapped one:
    Trilinearmap's _UVScale and StaticRock's _TriplanarScale are texture repeats per metre."""
    values = info["values"]
    per_metre = values.get("UVScale") if info["shader"] == "Trilinearmap" else (
        values.get("TriplanarScale") if values.get("TriplanarMap") == 1.0 else None)
    return 1.0 / per_metre if per_metre and info["texture_px"] else None


def _finish(row):
    texel, area = row.pop("_texel"), row.pop("_area")
    if "triplanar_repeat_m" in row:
        return row
    row["texel_density"] = round(float(texel / area), 1) if area else None
    row["area_m2"] = round(float(row["area_m2"]), 2)
    return row


LOD_BIAS, FOV_DEG = 2.0, 65.0      # the game's default LOD setting (GraphicsSettingsManager) and camera field of view


def lod_distance(size, screen_height):
    """Metres from the camera at which a LODGroup of this size (at scale 1) drops below a screen-relative height,
    at the game's default LOD bias: Unity switches when size / (2 d tan(fov/2)) * bias falls under the height."""
    if not size or not screen_height:
        return None
    return round(size * LOD_BIAS / (2 * screen_height * np.tan(np.radians(FOV_DEG / 2))), 1)


def weighted_texel(rows):
    """Area-weighted texel density over part rows, or None."""
    measured = [(r["texel_density"], r["area_m2"]) for r in rows
                if r["texel_density"] and r["area_m2"] and "triplanar_repeat_m" not in r]
    area = sum(a for _, a in measured)
    return round(sum(t * a for t, a in measured) / area, 1) if area else None


def summary(prefab_path):
    """The measurements of one world prefab (see the module docstring)."""
    pf = game.prefab(prefab_path)
    drawn, matrices = renderers(pf), world_matrices(pf)
    groups, near, per_lod = lod_split(pf, drawn)
    box, rows = bounds(near, matrices), part_rows(near, matrices)
    mats = sorted({m for r in near for m in r["materials"] if m})
    infos = [material_info(m) for m in mats]
    main = max(rows, key=lambda r: r["area_m2"]) if rows else None
    return {"prefab": prefab_path, "name": pf.name(pf.root()), "scripts": scripts(pf),
            "triangles": triangles(near), "renderers": len(near),
            "lods": [{"screen_height": g["screen_height"], "triangles": triangles(parts),
                      "until_m": lod_distance(groups[0]["size"], g["screen_height"])}
                     for g, parts in zip(groups[0]["lods"], per_lod)] if groups else [],
            "lod_groups": len(groups), "billboard": any(g["billboard"] for g in groups),
            "size_m": [round(float(v), 2) for v in (box[1] - box[0])] if box else None,
            "base_y": round(float(box[0][1]), 2) if box else None,
            "texel_density": weighted_texel(rows), "shader": main["shader"] if main else None,
            "texture_px": max((max(i["texture_px"]) for i in infos if i["texture_px"]), default=None),
            "parts": rows, "materials": [{"path": m, **i} for m, i in zip(mats, infos)]}


def scripts(pf):
    """{game script: count} over the prefab, the plain Unity components (Transform, renderers, colliders) left out."""
    plain = {"Transform", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer", "RectTransform"}
    counts = {}
    for _, kind, _ in pf.all_components():
        if kind not in plain:
            counts[kind] = counts.get(kind, 0) + 1
    return dict(sorted(counts.items()))


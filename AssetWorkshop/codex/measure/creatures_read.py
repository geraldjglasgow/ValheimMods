"""Reads one creature prefab of the reference export into plain numbers: its renderers (triangles, bones, materials,
textures, texel density), its size in the game, its capsule, LODs, star-level looks (LevelEffects), the items it
carries and the settings of the scripts that drive its animator. creatures.py calls read() on every creature.

Matrices are 4x4 numpy arrays in Unity's axes (y up, +z forward). The prefab root is put at the origin with no
rotation (the export leaves roots at odd places), its scale kept, as the game spawns it.
"""
import os
import re

import numpy as np

import game
from workshop import unity

ROOT_CLASSES = ("Player", "Humanoid", "Character", "Trader", "Odin", "Valkyrie", "Raven", "Fish", "Leviathan",
                "RandomFlyingBird")
HSV = ("_Hue", "_Saturation", "_Value")
KEPT_FLOATS = ("_Glossiness", "_Metallic", "_MetalGloss", "_BumpScale", "_Cutoff", "_Cull", "_UseStyles", "_Style",
               "_SnowCover", "_NoiseGlowEnabled", "_TwoSidedNormals", "_AddRain") + HSV


def creature_prefabs():
    """Every prefab under Characters/ whose root GameObject carries one of ROOT_CLASSES."""
    found = []
    for path in game.find("Characters/**/*.prefab"):
        text = unity.read(path)
        if "m_Script: {fileID: " not in text:
            continue
        p = game.prefab(path)
        kinds = {kind for kind, _ in p.components(p.root())}
        if kinds & set(ROOT_CLASSES):
            found.append(path)
    return found


def trs(body):
    """A transform body's local matrix."""
    px, py, pz = unity.numbers(unity.field(body, "m_LocalPosition"))
    x, y, z, w = unity.numbers(unity.field(body, "m_LocalRotation"))
    sx, sy, sz = unity.numbers(unity.field(body, "m_LocalScale"))
    rot = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                    [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                    [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    m = np.eye(4)
    m[:3, :3] = rot * np.array([sx, sy, sz])
    m[:3, 3] = (px, py, pz)
    return m


def world(p, node, cache=None):
    """The node's matrix in the prefab, the root at the origin with its scale only."""
    cache = {} if cache is None else cache
    if node not in cache:
        body = p.docs[node][1]
        father = unity.ref(unity.field(body, "m_Father"))[0]
        if father == "0" or not p.is_local(father):
            sx, sy, sz = unity.numbers(unity.field(body, "m_LocalScale"))
            cache[node] = np.diag([sx, sy, sz, 1.0])
        else:
            cache[node] = world(p, father, cache) @ trs(body)
    return cache[node]


def scale_of(matrix):
    """The mean length of a matrix's three axes: how much it scales."""
    return float(np.mean(np.linalg.norm(matrix[:3, :3], axis=0)))


def owners(p):
    """{component fileID: transform node} over the prefab."""
    table = {}
    for node in p.nodes():
        for item in unity.items(p.game_object(node), "m_Component"):
            table[unity.ref(item)[0]] = node
    return table


def active_path(p, node):
    """True when the node and every parent up to the root are active."""
    while node and p.is_local(node):
        if not p.active(node):
            return False
        node = unity.ref(unity.field(p.docs[node][1], "m_Father"))[0]
    return True


def lod_levels(p):
    """[(screen height, {renderer fileIDs})] of the prefab's first LODGroup, [] without one."""
    for _, kind, body in p.all_components():
        if kind == "LODGroup":
            levels = []
            for chunk in body.split("screenRelativeHeight: ")[1:]:
                height = float(re.match(unity.NUMBER, chunk).group(0))
                levels.append((round(height, 4), set(re.findall(r"renderer: \{fileID: (-?\d+)\}", chunk))))
            return levels
    return []


def bind_poses(mesh_path):
    """The mesh's bind poses as 4x4 matrices."""
    text = unity.read(mesh_path)
    block = text[text.index("m_BindPose:"):text.index("m_BoneNameHashes:")] if "m_BindPose:" in text else ""
    values = [float(v) for v in re.findall(rf"e\d\d: ({unity.NUMBER})", block)]
    return [np.array(values[i:i + 16]).reshape(4, 4) for i in range(0, len(values) - 15, 16)]


def bone_nodes(body):
    """The transform fileIDs a SkinnedMeshRenderer lists as its bones, in order."""
    return [unity.ref(item)[0] for item in unity.items(body, "m_Bones")]


def mesh_matrix(p, node, body, mesh, cache):
    """Mesh space -> prefab space: through the root bone and its bind pose for a skinned mesh (the bind pose is the
    rest pose), through the node for a static one."""
    if "m_Bones:" not in body:
        return world(p, node, cache)
    bones, poses = bone_nodes(body), bind_poses(mesh)
    root = unity.ref(unity.field(body, "m_RootBone"))[0]
    index = bones.index(root) if root in bones else 0
    if index >= len(poses) or not p.is_local(bones[index]):
        return world(p, node, cache)
    return world(p, bones[index], cache) @ poses[index]


def material_info(path):
    """What the codex keeps of a material: shader, textures and sizes, colour-adjust floats, emission, keywords."""
    mat = game.material(path)
    sizes = {slot: list(game.texture_size(t) or (0, 0)) for slot, t in mat["textures"].items() if t}
    emission = mat["colors"].get("_EmissionColor", (0, 0, 0, 1))
    return {"material": mat["name"], "path": path, "shader": mat["shader"], "textures": sizes,
            "main_texture": os.path.basename(mat["textures"].get("_MainTex") or "") or None,
            "floats": {k: round(v, 3) for k, v in mat["floats"].items() if k in KEPT_FLOATS},
            "emission": [round(c, 3) for c in emission[:3]],
            "color": [round(c, 3) for c in mat["colors"].get("_Color", (1, 1, 1, 1))],
            "style_texture": os.path.basename(mat["textures"].get("_StyleTex") or "") or None,
            "keywords": sorted(mat["keywords"])}


def renderer_rows(p):
    """Every mesh or skinned renderer: node, kind, body, drawn (active and enabled), LOD index (0 when no group)."""
    owner, levels, rows = owners(p), lod_levels(p), []
    for node, kind, body in p.all_components():
        if kind not in ("SkinnedMeshRenderer", "MeshRenderer"):
            continue
        file_id = next((f for f, n in owner.items() if n == node and p.docs.get(f, (0, ""))[1] is body), None)
        lod = next((i for i, (_, ids) in enumerate(levels) if file_id in ids), 0)
        drawn = active_path(p, node) and unity.field(body, "m_Enabled") != "0"
        rows.append({"node": node, "kind": kind, "body": body, "drawn": drawn,
                     "lod": lod, "file_id": file_id})
    return rows


def measure_renderer(p, row, cache):
    """Triangles, vertices, bones, materials, the mesh's bounds in the prefab and its texel density."""
    mesh = game.renderer_mesh(p, row["node"], row["body"])
    if not mesh or not mesh.endswith(".asset"):
        return None
    stats, mats = game.mesh_stats(mesh), [m for m in game.renderer_materials(row["body"]) if m]
    matrix = mesh_matrix(p, row["node"], row["body"], mesh, cache)
    infos = [material_info(m) for m in mats]
    px = next((max(i["textures"]["_MainTex"]) for i in infos if "_MainTex" in i["textures"]), None)
    low, high = _bounds(mesh, matrix)
    return {"node": p.path_to(row["node"]), "file_id": row["file_id"], "kind": row["kind"], "mesh": mesh,
            "triangles": stats["triangles"],
            "vertices": stats["vertices"], "bones": stats["bones"], "lod": row["lod"], "drawn": row["drawn"],
            "materials": infos, "texture_px": px, "low": low, "high": high,
            "texel_density": _density(mesh, px, scale_of(matrix)), "mesh_scale": round(scale_of(matrix), 4)}


def _bounds(mesh, matrix):
    """(min, max) corners of the mesh's vertices in the prefab, metres; the header's box when the vertices do not
    decode (channels carrying flag bits)."""
    try:
        positions = game.mesh_arrays(mesh)[0]
    except (KeyError, ValueError, IndexError):
        half = np.array(game.mesh_stats(mesh)["size"]) / 2
        positions = np.array([-half, half])
    placed = positions @ matrix[:3, :3].T + matrix[:3, 3]
    return placed.min(axis=0).tolist(), placed.max(axis=0).tolist()


def _density(mesh, px, scale):
    if not px:
        return None
    try:
        value = game.texel_density(mesh, px, scale)
    except (KeyError, ValueError, IndexError):
        return None
    return round(value, 1) if value else None

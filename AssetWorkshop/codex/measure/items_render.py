"""Renders the game's items one by one for the codex's contact sheets (Blender, headless). Held items are placed in
their attach frame (the fist at the origin, marked by a small red cross), turned so the long axis points up and the
width faces the camera, and drawn orthographic at one scale per category, so sizes compare along a sheet. Other items
get a three-quarter view each, framed to fit. Textures are point filtered, as the game samples them.

    blender --background --factory-startup --python codex/measure/items_render.py -- <category> [...] [--max N]

Reads codex/data/items.json and writes codex/out/items/tiles/<category>/<nn>_<name>.png plus tiles.json (what each
tile shows); items_sheets.py lays them out with labels. Local reference only: nothing here enters the repository.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402
from workshop import prefab, scene  # noqa: E402

DATA = os.path.join(HERE, "..", "data", "items.json")
OUT = os.path.join(HERE, "..", "out", "items", "tiles")
UPRIGHT = ("weapon.", "tool.", "shield.")
TILE = (320, 640)
OBJECT_TILE = (320, 320)
WORN = ("armour.chest", "armour.legs", "armour.cape")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    limit = int(argv[argv.index("--max") + 1]) if "--max" in argv else 40
    wanted = [a for a in argv if not a.startswith("--") and not a.isdigit()]
    with open(DATA, encoding="utf-8") as handle:
        categories = json.load(handle)["categories"]
    for key in wanted or list(categories):
        rows = sorted(categories[key]["samples"], key=lambda r: (r["tier"] is None, r["tier"] or 0, r["name"]))
        rows = [dict(r, category=key) for r in rows]
        render_category(key, rows[:limit])


def render_category(key, rows):
    folder = os.path.join(OUT, key)
    os.makedirs(folder, exist_ok=True)
    upright = key.startswith(UPRIGHT) and key != "weapon.creature" and all("attach" in r for r in rows)
    frame = held_frame(rows) if upright else None
    shown = []
    for index, row in enumerate(rows):
        path = os.path.join(folder, f"{index:02d}_{row['name']}.png")
        if render_one(row, path, frame):
            shown.append({"file": os.path.basename(path), "name": row["name"], "tier": row["tier"],
                          "triangles": row["triangles"], "texture_px": row["texture_px"],
                          "size_m": row["size_m"], "upright": bool(frame)})
    with open(os.path.join(folder, "tiles.json"), "w", encoding="utf-8") as handle:
        json.dump({"category": key, "frame": frame, "tiles": shown}, handle, indent=1)


def held_frame(rows):
    """One view for a whole category in the attach frame (Unity axes): x from -half to +half across, z from low to
    high up, so every fist sits at the same place on the sheet."""
    half = max(max(abs(r["attach"]["box_min"][0]), abs(r["attach"]["box_max"][0])) for r in rows) * 1.04
    low = min(r["attach"]["box_min"][2] for r in rows) - 0.03
    high = max(r["attach"]["box_max"][2] for r in rows) + 0.03
    width_px = int(min(max(TILE[1] * 2 * half / (high - low), 160), 1280))
    return {"half_x": half, "low_z": low, "high_z": high, "resolution": [width_px, TILE[1]]}


def render_one(row, path, frame):
    """Loads one prefab alone and renders it; False when it draws nothing."""
    clear()
    stage(frame["resolution"] if frame else OBJECT_TILE)
    worn = row.get("category") in WORN
    root = prefab.load(row["prefab"], include_inactive=worn)
    frames = {o.name.split(".")[0]: o for o in root.children}
    if worn and any(n.startswith("attach_") for n in frames):
        keep_only(root, *[o for n, o in frames.items() if n.startswith("attach_")])
    elif frame and "attach" in frames:
        keep_only(root, frames["attach"])
        into_attach_frame(root, frames["attach"])
    parts = prefab.meshes(root)
    if not parts:
        return False
    point_filter()
    if frame:
        upright_shot(frame, path)
    else:
        object_shot(parts, path, BEHIND if row.get("category") == "armour.cape" else FRONT)
    return True


def clear():
    """Empties the scene between items (the workshop's clear, plus the collections and worlds made here)."""
    scene.clear()
    for block in (bpy.data.collections, bpy.data.worlds):
        for item in list(block):
            block.remove(item)


def keep_only(root, *frames):
    """Removes every mesh outside the given frames (what the hand holds or the body wears; not the ground model)."""
    held = {o for frame in frames for o in frame.children_recursive} | set(frames)
    for obj in list(root.children_recursive):
        if obj not in held and obj.type == "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)


def into_attach_frame(root, attach):
    """Moves the prefab so the attach sits at the origin with no rotation, keeping only the attach's own scale, as
    VisEquipment.AttachItem places it in the hand."""
    bpy.context.view_layer.update()
    scale = attach.matrix_local.to_scale()
    root.matrix_world = Matrix.Diagonal((*scale, 1.0)) @ attach.matrix_local.inverted()
    bpy.context.view_layer.update()
    mark_origin()


def mark_origin():
    """A small red cross at the origin: where the fist closes."""
    mat = flat("fist_mark", (0.9, 0.05, 0.05), emit=True)
    for axis in range(3):
        size = [0.014, 0.014, 0.014]
        size[axis] = 0.16
        bpy.ops.mesh.primitive_cube_add(size=1.0)
        cube = bpy.context.active_object
        cube.scale = size
        cube.data.materials.append(mat)
        cube["fist_mark"] = True


def upright_shot(frame, path):
    """Orthographic in the attach frame: looking from Unity +Y, Unity +Z up the sheet and +X to the right (the
    workshop maps Unity (x, y, z) to Blender (-x, -z, y)); the same view for the whole category."""
    height = frame["high_z"] - frame["low_z"]
    camera = bpy.context.scene.camera
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(height, 2 * frame["half_x"])
    camera.location = Vector((0.0, -(frame["high_z"] + frame["low_z"]) / 2, 20.0))
    camera.rotation_euler = look_rotation(Vector((0, 0, -1)), Vector((0, -1, 0)))
    render(path)


def look_rotation(forward, up):
    """Camera rotation looking along forward with up as the screen's top."""
    forward, up = forward.normalized(), up.normalized()
    right = forward.cross(up).normalized()
    true_up = right.cross(forward)
    return Matrix((right, true_up, -forward)).transposed().to_euler()


FRONT, BEHIND = Vector((0.8, -1.2, 0.9)), Vector((0.7, 1.2, 0.5))


def object_shot(parts, path, direction):
    """Perspective three-quarter view framed to the object's bounds (the workshop's front faces -Y)."""
    low, high = prefab.bounds(parts)
    centre, radius = (low + high) / 2, max((high - low).length / 2, 0.02)
    camera = bpy.context.scene.camera
    camera.data.type, camera.data.lens = "PERSP", 50
    direction = direction.normalized()
    camera.location = centre + direction * radius / math.sin(camera.data.angle / 2) * 1.02
    camera.rotation_euler = look_rotation(-direction, Vector((0, 0, 1)))
    render(path)


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def point_filter():
    for mat in bpy.data.materials:
        for node in (mat.node_tree.nodes if mat.node_tree else []):
            if node.bl_idname == "ShaderNodeTexImage":
                node.interpolation = "Closest"


def stage(resolution):
    s = bpy.context.scene
    s.render.engine = "BLENDER_EEVEE"
    s.eevee.taa_render_samples = 16
    s.render.resolution_x, s.render.resolution_y = resolution
    s.render.film_transparent = False
    s.view_settings.view_transform = "Standard"
    s.world = bpy.data.worlds.new("items")
    background = next(n for n in s.world.node_tree.nodes if n.bl_idname == "ShaderNodeBackground")
    background.inputs["Color"].default_value = (0.42, 0.46, 0.44, 1.0)
    background.inputs["Strength"].default_value = 0.9
    sun = bpy.data.objects.new("items_sun", bpy.data.lights.new("items_sun", "SUN"))
    sun.data.energy = 2.4
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(25))
    s.collection.objects.link(sun)
    camera = bpy.data.objects.new("items_camera", bpy.data.cameras.new("items_camera"))
    s.collection.objects.link(camera)
    s.camera = camera


def flat(name, rgb, emit=False):
    mat = bpy.data.materials.new(name)
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    if emit:
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1)
        bsdf.inputs["Emission Strength"].default_value = 2.0
    return mat


main()

"""Renders the reference prefabs of data/environment.json in Blender, for looking at them (never committed): each on
its own point-filtered textures under the game's Meadows clear-day light (EnvSetup 'Clear': sun #ffc57b at 1.7,
ambient #7692b4), on a ground plane at the prefab's pivot so buried rock stays buried, beside a 1.8 m figure.
Props get a three-quarter view and the same view with every triangle edge drawn; grass is planted as a small patch
of its instanced mesh; locations and rooms get a high three-quarter view.

    blender --background --factory-startup --python codex/measure/environment_render.py -- [category ...]
                                                           [prefab:<path under the export> ...]

Writes codex/out/environment/render/<category>/<name>_<view>.png and render/index.json (for environment_sheets.py).
The shader's own moss, snow, wind and terrain tint are not drawn: see the codex pages for what they add.
"""
import json
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))
sys.path.insert(0, HERE)

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import materials, prefab, prefab_parts, scene, shapes, unity  # noqa: E402

DATA = os.path.join(HERE, "..", "data", "environment.json")
OUT = os.path.join(HERE, "..", "out", "environment", "render")
TILE = 480
SUN, AMBIENT, GROUND = (1.0, 0.772, 0.484), (0.463, 0.574, 0.706), (0.31, 0.44, 0.25)
PROP_VIEW, SITE_VIEW = Vector((1.0, -1.25, 0.55)), Vector((1.0, -1.1, 1.0))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    index = _load_index()
    extra = [a[len("prefab:"):] for a in argv if a.startswith("prefab:")]
    if extra:
        index["extra"] = [render("extra", path, {}) for path in extra]
    for key, category in data["categories"].items():
        if (argv or extra) and key not in argv:
            continue
        samples = {s["prefab"]: s for s in category["samples"]}
        index[key] = [render(key, path, samples.get(path, {})) for path in category["references"]]
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "index.json"), "w", encoding="utf-8") as handle:
        json.dump(index, handle, indent=1)


def _load_index():
    try:
        with open(os.path.join(OUT, "index.json"), encoding="utf-8") as handle:
            return json.load(handle)
    except (OSError, ValueError):
        return {}


def render(key, path, sample):
    """Renders one prefab; returns {name, prefab, tiles: [paths], note}."""
    scene.clear()
    stage()
    name = os.path.splitext(os.path.basename(path))[0]
    folder = os.path.join(OUT, key.replace(".", "_"))
    os.makedirs(folder, exist_ok=True)
    parts = plant(sample) if sample.get("instance") else load(path)
    if not parts:
        return {"name": name, "prefab": path, "tiles": [], "note": "nothing drawn"}
    site = key.startswith("location.")
    ground(parts)
    figure(parts)
    view = SITE_VIEW if site else PROP_VIEW
    tiles = [shot(parts, view, os.path.join(folder, f"{name}_view.png"))]
    if not site:
        wire(parts)
        tiles.append(shot(parts, view, os.path.join(folder, f"{name}_wire.png")))
    return {"name": name, "prefab": path, "tiles": tiles, "note": ""}


def load(path):
    """The prefab's drawn meshes, the dungeon interior (5000 m up) left out."""
    root = prefab.load(path)
    parts = prefab.meshes(root)
    full_cutoff(parts)
    bpy.context.view_layer.update()
    inside = [o for o in parts if (o.matrix_world @ Vector(o.bound_box[0])).z > 1000]
    for obj in inside:
        bpy.data.objects.remove(obj)
    return [o for o in parts if o not in inside]


def plant(sample):
    """A 3 m patch of an instanced clutter prefab: 24 copies of its mesh, random turn and a little random scale."""
    instance, mat_path = sample["instance"], _material_path(sample)
    material = prefab_parts.material(mat_path)
    terrain_tint(material, mat_path)
    data = prefab_parts.mesh(instance["mesh"], [material])
    rng, parts = random.Random(7), []
    for i in range(24):
        obj = bpy.data.objects.new(f"clutter_{i}", data)
        bpy.context.scene.collection.objects.link(obj)
        obj.location = (rng.uniform(-1.5, 1.5), rng.uniform(-1.5, 1.5), 0.0)
        obj.rotation_euler = (0.0, 0.0, rng.uniform(0, math.tau))
        k = rng.uniform(0.8, 1.2)
        sx, sy, sz = instance["scale"]
        obj.scale = (sx * k, sz * k, sy * k)       # Unity y (up) is Blender z
        parts.append(obj)
    full_cutoff(parts)
    return parts


def terrain_tint(material, mat_path):
    """Grass drawn white in its texture takes its colour from _TerrainColorTex in the game; the render multiplies it
    by that map's mean colour so the patch reads as it does there."""
    import game
    import numpy as np
    tex = game.material(mat_path)["textures"].get("_TerrainColorTex")
    if not tex:
        return
    image = bpy.data.images.load(os.path.join(game.ROOT, tex))
    pixels = np.empty(len(image.pixels), np.float32)
    image.pixels.foreach_get(pixels)
    mean = pixels.reshape(-1, 4)[:, :3].mean(axis=0)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
    albedo = next(n for n in nodes if n.bl_idname == 'ShaderNodeTexImage')
    mix = nodes.new('ShaderNodeVectorMath')
    mix.operation, mix.inputs[1].default_value = 'MULTIPLY', tuple(float(c) for c in mean)
    links.new(albedo.outputs['Color'], mix.inputs[0])
    links.new(mix.outputs['Vector'], bsdf.inputs['Base Color'])


def full_cutoff(parts):
    """Unity's alpha test keeps a texel when alpha - cutoff >= 0, so a cutoff of 1.0 still draws fully opaque texels;
    the loader's 'alpha greater than cutoff' test would hide them, so its threshold is eased to 0.99."""
    for mat in {m for o in parts for m in o.data.materials if m and m.node_tree}:
        for node in mat.node_tree.nodes:
            if node.bl_idname == 'ShaderNodeMath' and node.operation == 'GREATER_THAN' and (
                    node.inputs[1].default_value >= 1.0):
                node.inputs[1].default_value = 0.99


def _material_path(sample):
    """The clutter prefab's material path, read again from its InstanceRenderer."""
    import game  # plain Python reader, importable inside Blender
    pf = game.prefab(sample["prefab"])
    body = next(b for _, k, b in pf.all_components() if k == "InstanceRenderer")
    return game.path_of(unity.field(body, "m_material"))


def stage():
    """Eevee, the Meadows clear-day sun and ambient, a camera."""
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 16
    s.render.resolution_x = s.render.resolution_y = TILE
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new("env_world")
    bg = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    bg.inputs['Color'].default_value = (*AMBIENT, 1.0)
    bg.inputs['Strength'].default_value = 0.8
    light = bpy.data.lights.new("env_sun", 'SUN')
    light.energy, light.color, light.angle = 3.4, SUN, math.radians(2)
    sun = bpy.data.objects.new("env_sun", light)
    s.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(45), 0.0, math.radians(35))
    camera = bpy.data.objects.new("env_camera", bpy.data.cameras.new("env_camera"))
    s.collection.objects.link(camera)
    s.camera = camera


def ground(parts):
    """A plain terrain-green plane at the pivot (z = 0), wide enough to run out of frame."""
    low, high = prefab.bounds(parts)
    size = max((high - low).length * 4, 20.0)
    bpy.ops.mesh.primitive_plane_add(size=size, location=((low.x + high.x) / 2, (low.y + high.y) / 2, 0.0))
    bpy.context.active_object.data.materials.append(materials.flat("env_ground", GROUND))
    bpy.context.active_object.name = "env_ground"


def figure(parts):
    """A 1.8 m stand-in for the player beside the asset."""
    low, high = prefab.bounds(parts)
    skin = materials.flat("env_figure", (0.25, 0.3, 0.4))
    x, y = high.x + 0.6, (low.y + high.y) / 2
    shapes.cylinder("env_body", 0.2, 1.5, (x, y, 0.75), material=skin, vertices=16)
    shapes.sphere("env_head", 0.15, (x, y, 1.65), material=skin)


def wire(parts):
    """Every triangle edge drawn over the parts in near-black, so facet size and count read."""
    black = materials.flat("env_wire", (0.02, 0.02, 0.02))
    low, high = prefab.bounds(parts)
    thickness = max((high - low).length * 0.0012, 0.002)
    for obj in parts:
        copy = obj.copy()
        bpy.context.scene.collection.objects.link(copy)
        modifier = copy.modifiers.new("wire", 'WIREFRAME')
        modifier.thickness, modifier.use_replace = thickness, True
        modifier.use_even_offset = False          # even offset spikes at the thin, sharp corners of cards and shards
        copy.data = obj.data.copy()
        copy.data.materials.clear()
        copy.data.materials.append(black)


def shot(parts, direction, path):
    """Frames the parts above ground (and the figure) from a direction and renders to path."""
    low, high = prefab.bounds(parts)
    low.z = max(low.z, 0.0)
    high.z = max(high.z, 1.8)
    camera = bpy.context.scene.camera
    center, radius = (low + high) / 2, max((high - low).length / 2, 0.3)
    distance = radius / math.sin(camera.data.angle / 2) * 1.02
    camera.data.clip_start, camera.data.clip_end = distance * 0.01, distance * 10
    camera.location = center + direction.normalized() * distance
    camera.rotation_euler = (-direction.normalized()).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


main()

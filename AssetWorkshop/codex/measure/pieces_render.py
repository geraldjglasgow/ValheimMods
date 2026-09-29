"""Renders the build-menu pieces for looking at, in Blender, into codex/out/pieces/renders/ (never committed):

- <category>.png: the category's reference prefabs side by side on a 1 m grid beside a 1.8 m post (a player's
  height), orthographic three-quarter view, point-filtered textures, one sun.
- states_<n>.png: reference pieces new, worn and broken side by side (WearNTear's three looks).
- close_<name>.png: a close perspective view of one piece, to see the texel size and edge treatment.

    blender --background --factory-startup --python codex/measure/pieces_render.py -- [category ...]

Reads data/pieces.json (run measure/pieces.py first). Loads each prefab with workshop.prefab.load.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(WORKSHOP, "blender"))
sys.path.insert(0, HERE)

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import prefab  # noqa: E402

import pieces_scan  # noqa: E402

DATA = os.path.join(WORKSHOP, "codex", "data", "pieces.json")
OUT = os.path.join(WORKSHOP, "codex", "out", "pieces", "renders")
VIEW = Vector((-0.38, -1.0, 0.5)).normalized()        # from the front-left, above: the prefab's front faces -Y
STATES = ("woodwall", "stone_wall_2x1", "darkwood_roof", "wood_roof", "woodiron_beam", "iron_wall_2x2",
          "Piece_grausten_wall_2x2", "blackmarble_2x1x1", "piece_workbench", "forge", "piece_chest_wood", "bed")
CLOSE = ("woodwall", "wood_roof", "darkwood_roof", "stone_wall_2x1", "woodiron_beam", "wood_pole_log",
         "Piece_grausten_wall_2x2", "blackmarble_arch", "piece_workbench", "stave_wall_2x2", "ashwood_wall_2x2",
         "piece_chest_wood", "wood_door", "piece_banner01")


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    stage()


def stage():
    """Eevee, standard view transform, a sun from the front-left, a pale grey-green sky light, a dark floor."""
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 16
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new("pieces")
    s.world.use_nodes = True
    bg = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    bg.inputs['Color'].default_value = (0.42, 0.46, 0.44, 1.0)
    bg.inputs['Strength'].default_value = 0.6
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(-30))
    s.collection.objects.link(sun)


def floor(low, high):
    """A dark floor with 1 m grid lines under the lineup, and a 1.8 m post at its left end."""
    width, depth = high.x - low.x + 4, high.y - low.y + 4
    bpy.ops.mesh.primitive_plane_add(size=1, location=((low.x + high.x) / 2, (low.y + high.y) / 2, -0.002))
    plane = bpy.context.active_object
    plane.scale = (width, depth, 1)
    plane.data.materials.append(flat("floor", (0.12, 0.13, 0.11)))
    line = flat("grid", (0.3, 0.3, 0.27))
    for x in range(math.floor(low.x) - 2, math.ceil(high.x) + 3):
        box((x, (low.y + high.y) / 2, 0), (0.015, depth, 0.004), line)
    for y in range(math.floor(low.y) - 2, math.ceil(high.y) + 3):
        box(((low.x + high.x) / 2, y, 0), (width, 0.015, 0.004), line)
    box((low.x - 1.0, (low.y + high.y) / 2, 0.9), (0.3, 0.3, 1.8), flat("post", (0.55, 0.52, 0.47)))
    return Vector((low.x - 1.3, low.y, 0.0)), Vector((high.x, high.y, max(high.z, 1.8)))


def box(centre, size, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    obj = bpy.context.active_object
    obj.scale = size
    obj.data.materials.append(material)


def flat(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (*rgb, 1)
    return mat


def load(path, state="new"):
    """The prefab's look in one WearNTear state: new as saved, or worn/broken with the other states, fragment
    roots, snow, fire and marker objects left out."""
    scan = pieces_scan.Scan(path)
    names = {scan.prefab.name(n) for n in scan.states.get("fragments", [])}
    for key in ("new", "worn", "broken"):
        node = scan.states.get(key)
        if node is not None and key != state and node != scan.states.get(state):
            names.add(scan.prefab.name(node))
    loose = ("snow", "_enabled", "areamarker", "playerbase", "destruction")
    if state == "new":
        root = prefab.load(path, skip=lambda name, layer: "snow" in name.lower())
    else:
        root = prefab.load(path, include_inactive=True, skip=lambda name, layer: name in names or any(
            word in name.lower() for word in loose))
    forget_meshes()
    return root


def forget_meshes():
    """Drops the loader's mesh cache keys: within one session a mesh shared by two prefabs came back wearing the first
    prefab's material (banners and rugs), so each prefab decodes its meshes afresh."""
    for data in bpy.data.meshes:
        for key in ("reference_mesh", "reference_source"):
            if key in data:
                del data[key]


def ground(root, x):
    """Moves a loaded prefab so it stands on z = 0 with its left edge at x; returns its right edge."""
    bpy.context.view_layer.update()
    parts = prefab.meshes(root)
    if not parts:
        return x
    low, high = prefab.bounds(parts)
    root.location += Vector((x - low.x, -(low.y + high.y) / 2, -low.z))
    return x + (high.x - low.x)


def lineup(paths, states=("new",)):
    """Every path (in every state given) in a row, 0.8 m apart; returns the bounds of everything."""
    x = 0.0
    for path in paths:
        for state in states:
            x = ground(load(path, state), x) + 0.8
    bpy.context.view_layer.update()
    return prefab.bounds([o for o in bpy.data.objects if o.type == 'MESH'])


def point_filter():
    for mat in bpy.data.materials:
        for node in (mat.node_tree.nodes if mat.node_tree else []):
            if node.bl_idname == 'ShaderNodeTexImage':
                node.interpolation = 'Closest'


def shot(name, low, high, ortho=True, lens=50, width=1600):
    """Frames the box low-high from VIEW and renders it to OUT/name.png."""
    s = bpy.context.scene
    centre = (low + high) / 2
    camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    s.collection.objects.link(camera)
    camera.location = centre + VIEW * 60
    camera.rotation_euler = (-VIEW).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    right, up = camera.matrix_world.to_3x3() @ Vector((1, 0, 0)), camera.matrix_world.to_3x3() @ Vector((0, 1, 0))
    corners = [Vector((a, b, c)) for a in (low.x, high.x) for b in (low.y, high.y) for c in (low.z, high.z)]
    span_x = max((p - centre).dot(right) for p in corners) * 2
    span_y = max((p - centre).dot(up) for p in corners) * 2
    s.render.resolution_x, s.render.resolution_y = width, max(240, min(1000, int(width * span_y / span_x)))
    camera.data.type = 'ORTHO' if ortho else 'PERSP'
    camera.data.ortho_scale = max(span_x, span_y * width / s.render.resolution_y) * 1.06
    camera.data.lens = lens
    if not ortho:
        camera.location = centre + VIEW * (max(span_x, span_y * width / s.render.resolution_y) * 1.9)
    s.camera = camera
    s.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


def main(keys):
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    os.makedirs(OUT, exist_ok=True)
    for key in keys or data["categories"]:
        clear()
        low, high = floor(*lineup(data["categories"][key]["references"]))
        point_filter()
        shot(key, low, high)
    if not keys:
        names = {s["name"]: s["prefab"] for c in data["categories"].values() for s in c["samples"]}
        for n in range(0, len(STATES), 2):
            clear()
            low, high = floor(*lineup([names[p] for p in STATES[n:n + 2]], ("new", "worn", "broken")))
            point_filter()
            shot(f"states_{n // 2 + 1}", low, high)
        for name in CLOSE:
            clear()
            low, high = lineup([names[name]])
            point_filter()
            shot("close_" + name, low, high, ortho=False, lens=50, width=1200)


main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

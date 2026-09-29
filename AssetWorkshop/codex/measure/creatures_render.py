"""Renders the game's creatures one by one for looking at: a three-quarter view from the front and an orthographic
side view, on the game's own textures, in the bind pose (T-pose for most bipeds). Local reference only; the renders go
to codex/out/creatures/renders/ (gitignored) and creatures_sheet.py lays them out with names and sizes.

    blender --background --factory-startup --python codex/measure/creatures_render.py -- <list.json>
            [--size 384] [--force]

list.json is a list of prefab paths under the reference export (creatures.py writes out/creatures/render_list.json).
Each prefab is loaded into a cleared scene: a second load of the same skinned mesh in one scene reuses the first's
posed data (see the workshop README, the spine greataxe).
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import prefab, scene  # noqa: E402

OUT = os.path.normpath(os.path.join(HERE, "..", "out", "creatures", "renders"))
VIEWS = {"front": (Vector((0.75, -1.25, 0.45)), False), "side": (Vector((1.0, 0.0, 0.05)), True)}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    size = int(argv[argv.index("--size") + 1]) if "--size" in argv else 384
    with open(argv[0], encoding="utf-8") as handle:
        paths = json.load(handle)
    os.makedirs(OUT, exist_ok=True)
    for path in paths:
        name = os.path.splitext(os.path.basename(path))[0]
        if "--force" not in argv and all(os.path.exists(os.path.join(OUT, f"{name}_{v}.png")) for v in VIEWS):
            continue
        render(path, name, size)


def render(path, name, size):
    """Both views of one prefab, or a note when it draws nothing."""
    scene.clear()
    stage(size)
    root = prefab.load(path)
    parts = prefab.meshes(root)
    if not parts:
        print(f"creatures_render: {path} draws nothing")
        return
    for view, (direction, ortho) in VIEWS.items():
        shot(parts, direction, ortho, os.path.join(OUT, f"{name}_{view}.png"))


def stage(size):
    """Eevee, a soft sky and one sun from the front left, square frames."""
    s = bpy.context.scene
    s.render.engine = "BLENDER_EEVEE"
    s.eevee.taa_render_samples = 16
    s.render.resolution_x = s.render.resolution_y = size
    s.view_settings.view_transform = "Standard"
    s.world = bpy.data.worlds.new("creatures")
    background = next(n for n in s.world.node_tree.nodes if n.bl_idname == "ShaderNodeBackground")
    background.inputs["Color"].default_value = (0.42, 0.45, 0.47, 1.0)
    background.inputs["Strength"].default_value = 1.0
    sun = bpy.data.objects.new("creatures_sun", bpy.data.lights.new("creatures_sun", "SUN"))
    sun.data.energy = 2.5
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(-35))
    s.collection.objects.link(sun)
    camera = bpy.data.objects.new("creatures_camera", bpy.data.cameras.new("creatures_camera"))
    s.collection.objects.link(camera)
    s.camera = camera


def shot(parts, direction, ortho, path):
    """Frames every part from the direction given and renders to path; an orthographic shot along X frames only the
    height and length, and writes its framing beside the image (path.json) for creatures_sheet's lineups."""
    camera = bpy.context.scene.camera
    low, high = prefab.bounds(parts)
    center, radius = (low + high) / 2, max((high - low).length / 2, 0.05)
    direction = direction.normalized()
    camera.data.type = "ORTHO" if ortho else "PERSP"
    camera.data.ortho_scale = max(high.y - low.y, high.z - low.z, 0.1) * 1.08 if ortho else radius * 2.05
    if ortho:
        framing = {"center": list(center), "ortho_scale": camera.data.ortho_scale, "low": list(low),
                   "high": list(high)}
        with open(path[:-4] + ".json", "w", encoding="utf-8") as handle:
            json.dump(framing, handle)
    distance = radius * 4 if ortho else radius / math.sin(camera.data.angle / 2) * 1.02
    camera.data.clip_start, camera.data.clip_end = distance * 0.01, distance * 10
    camera.location = center + direction * distance
    camera.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


main()

"""The Rootbound Pack's inventory icon: out/icon.png, 128 x 128 with a transparent background, the pack alone in a
three-quarter view from behind and a little above (the root frame and X, guck seams and straps in sight), softly
lit like the game's item icons. Preview output, never part of the bundle; the final copy is kept as icon.png next to
model.py, since builds wipe out/.

    blender --background --factory-startup --python assets/packpanel_rootbound_pack/icon.py

Loads out/packpanel_rootbound_pack.blend (building it first when it is missing), renders at 512 with an
orthographic camera fitted to the pack's outline (small margin), then box-filters down to 128 in linear light with
premultiplied alpha, so the edges stay clean. The 512 render is kept as out/icon_512.png.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True
sys.path[:0] = [HERE, os.path.join(HERE, "..", "..", "blender")]

import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

from workshop import export, pipeline  # noqa: E402

NAME = os.path.basename(HERE)
OUT = os.path.join(HERE, "out")
RENDER, SIZE, MARGIN = 512, 128, 0.05
VIEW = Vector((0.9, 1.0, 0.4))        # towards the camera: behind the wearer (+Y), the vine side (+X), a little above


def main():
    pack = _pack()
    _stage()
    _frame(_camera(), pack)
    big = os.path.join(OUT, "icon_512.png")
    bpy.context.scene.render.filepath = big
    bpy.ops.render.render(write_still=True)
    _shrink(big, os.path.join(OUT, "icon.png"))
    print("ICON wrote", os.path.join(OUT, "icon.png"))


def _pack():
    blend = os.path.join(OUT, NAME + ".blend")
    if not os.path.exists(blend):
        pipeline.run(HERE, render_preview=False)
    bpy.ops.wm.open_mainfile(filepath=blend)
    pack = bpy.data.objects[NAME]
    for mat in pack.data.materials:     # matte leather, as the game's icons are
        next(n for n in mat.node_tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled').inputs[
            'Roughness'].default_value = 0.85
    return pack


def _stage():
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 64
    s.render.resolution_x = s.render.resolution_y = RENDER
    s.render.film_transparent = True
    s.render.image_settings.color_mode = 'RGBA'
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new("icon")
    background = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    background.inputs['Color'].default_value = (0.55, 0.58, 0.62, 1.0)
    background.inputs['Strength'].default_value = 0.45
    _light("key", 4.2, 12, (math.radians(38), 0.0, math.radians(200)))     # from above, over the camera's left
    _light("fill", 1.0, 30, (math.radians(70), 0.0, math.radians(100)))    # from the right, low
    _light("rim", 2.2, 8, (math.radians(-55), 0.0, math.radians(160)))    # from behind the pack, for its outline


def _light(name, energy, spread, rotation):
    light = bpy.data.lights.new(name, 'SUN')
    light.energy = energy
    light.angle = math.radians(spread)
    obj = bpy.data.objects.new(name, light)
    bpy.context.scene.collection.objects.link(obj)
    obj.rotation_euler = rotation


def _camera():
    camera = bpy.data.objects.new("icon_camera", bpy.data.cameras.new("icon_camera"))
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera.data.type = 'ORTHO'
    return camera


def _frame(camera, pack):
    """Points the camera along VIEW and fits the pack's outline into the frame with MARGIN on the longer side."""
    direction = VIEW.normalized()
    camera.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    turn = camera.rotation_euler.to_matrix()
    right, up = turn.col[0], turn.col[1]
    points = [pack.matrix_world @ v.co for v in pack.data.vertices]
    across, rise = [p.dot(right) for p in points], [p.dot(up) for p in points]
    middle = right * (max(across) + min(across)) / 2 + up * (max(rise) + min(rise)) / 2
    depth = sum((p.dot(direction) for p in points)) / len(points)
    camera.data.ortho_scale = max(max(across) - min(across), max(rise) - min(rise)) / (1 - 2 * MARGIN)
    camera.location = middle + direction * (depth + 3.0)
    camera.data.clip_start, camera.data.clip_end = 0.1, 10.0


def _shrink(path, out):
    """Box filter from RENDER to SIZE: colour to linear light and premultiplied by alpha, averaged, then back."""
    image = bpy.data.images.load(path)
    pixels = np.empty(RENDER * RENDER * 4, np.float32)
    image.pixels.foreach_get(pixels)
    pixels = pixels.reshape(RENDER, RENDER, 4)
    alpha = pixels[..., 3:4]
    colour = _linear(pixels[..., :3]) * alpha
    step = RENDER // SIZE
    colour = colour.reshape(SIZE, step, SIZE, step, 3).mean(axis=(1, 3))
    alpha = alpha.reshape(SIZE, step, SIZE, step, 1).mean(axis=(1, 3))
    colour = _encoded(np.where(alpha > 1e-4, colour / np.maximum(alpha, 1e-4), 0.0))
    small = bpy.data.images.new("icon", SIZE, SIZE, alpha=True)
    small.pixels.foreach_set(np.concatenate([colour, alpha], axis=2).astype(np.float32).ravel())
    export.png(small, out)


def _linear(srgb):
    return np.where(srgb <= 0.04045, srgb / 12.92, ((srgb + 0.055) / 1.055) ** 2.4)


def _encoded(linear):
    linear = np.clip(linear, 0.0, 1.0)
    return np.where(linear <= 0.0031308, linear * 12.92, 1.055 * linear ** (1 / 2.4) - 0.055)


main()

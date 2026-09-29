"""The vertebra drop's inventory icon, from the built item (build.ps1 runs it after the Blender builds):

    blender --background --factory-startup --python assets/ecp_skel_arsenal/icon.py

Opens assets/ecp_vertebra/out/ecp_vertebra.blend and writes out/ecp_vertebra_icon.png beside it: 256 x 256,
transparent, the vertebra seen mostly from above (its best-known view: the body, the ring round the canal, the wings
and the spine) tipped towards the viewer and turned a little, lit softly from the top left like the game's icons. The
workshop's Unity side packs it into the bundle as a sprite of the same name.
"""
import math
import os

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ITEM = os.path.join(HERE, '..', 'ecp_vertebra', 'out')
SIZE = 256


def main():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ITEM, 'ecp_vertebra.blend'))
    item = bpy.data.objects['ecp_vertebra']
    for obj in bpy.context.scene.objects:
        obj.hide_render = obj is not item
    item.rotation_euler = (math.radians(-35), 0.0, math.radians(-20))
    bpy.context.view_layer.update()
    _light()
    _camera(item)
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 64
    s.render.film_transparent = True
    s.render.resolution_x = s.render.resolution_y = SIZE
    s.render.image_settings.color_mode = 'RGBA'
    s.view_settings.view_transform = 'Standard'
    s.render.filepath = os.path.join(ITEM, 'ecp_vertebra_icon.png')
    bpy.ops.render.render(write_still=True)
    print('WORKSHOP icon', s.render.filepath)


def _light():
    world = bpy.data.worlds.new('icon')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.5, 0.5, 0.5, 1.0)
    world.node_tree.nodes['Background'].inputs[1].default_value = 0.8
    bpy.context.scene.world = world
    for obj in [o for o in bpy.context.scene.objects if o.type == 'LIGHT']:
        obj.hide_render = True
    sun = bpy.data.objects.new('icon_sun', bpy.data.lights.new('icon_sun', 'SUN'))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(30), math.radians(-30), math.radians(25))
    bpy.context.scene.collection.objects.link(sun)


def _camera(item):
    """Orthographic, straight down, framing the item with a small margin."""
    points = [item.matrix_world @ Vector(c) for c in item.bound_box]
    low = Vector([min(p[i] for p in points) for i in range(3)])
    high = Vector([max(p[i] for p in points) for i in range(3)])
    cam = bpy.data.objects.new('icon_camera', bpy.data.cameras.new('icon_camera'))
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = max(high.x - low.x, high.y - low.y) * 1.1
    cam.location = ((low.x + high.x) / 2, (low.y + high.y) / 2, high.z + 1.0)
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam


main()

"""The built axe beside the game's own Skeleton and battleaxes, all point filtered as the game samples textures, under
one light: the check that it looks like it belongs. Local reference only; run after build.ps1:

    blender --background --factory-startup --python assets/ecp_battleaxe_bone/lineup.py

Writes out/lineup/front.png, out/lineup/turn.png, out/lineup/close.png and out/lineup/lineup.blend.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'blender'))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import prefab  # noqa: E402

NAME = 'ecp_battleaxe_bone'
OUT = os.path.join(HERE, 'out', 'lineup')
WEAPONS = 'GameElements/Items/weapons/'
GAME_AXES = ('Battleaxe', 'BattleaxeSkullSplittur', 'BattleaxeBlackmetal', 'BattleaxeCrystal')
STAND_UP = (math.radians(-90), 0.0, math.radians(180))   # the item prefabs lie flat, head towards -Y


def main():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, 'out', NAME + '.blend'))
    for obj in [o for o in bpy.data.objects if o.name != NAME]:
        bpy.data.objects.remove(obj, do_unlink=True)
    axe = bpy.data.objects[NAME]
    ground(axe, 0.0)
    skeleton = prefab.load('Characters/Skeleton/Skeleton.prefab', 'game Skeleton')
    ground(skeleton, -1.5)
    for n, name in enumerate(GAME_AXES):
        root = prefab.load(WEAPONS + name + '.prefab', 'game ' + name)
        root.rotation_mode = 'XYZ'
        root.rotation_euler = STAND_UP
        ground(root, 0.95 + n * 0.85)
    point_filter()
    stage()
    os.makedirs(OUT, exist_ok=True)
    shot('front', Vector((0.9, -10, 1.0)), Vector((0.9, 0, 1.0)), ortho=6.4)
    shot('turn', Vector((3.2, -5.5, 2.4)), Vector((0.6, 0, 0.95)), lens=38)
    shot('close', Vector((0.95, -1.45, 1.8)), Vector((0.12, 0, 1.45)), lens=45)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'lineup.blend'))


def ground(root, x):
    """Moves a model so it stands on z = 0 with its bounds centred on x."""
    bpy.context.view_layer.update()
    parts = [root] if root.type == 'MESH' else prefab.meshes(root)
    low, high = prefab.bounds(parts)
    root.location += Vector((x - (low.x + high.x) / 2, -(low.y + high.y) / 2, -low.z))


def point_filter():
    for mat in bpy.data.materials:
        for node in (mat.node_tree.nodes if mat.node_tree else []):
            if node.bl_idname == 'ShaderNodeTexImage':
                node.interpolation = 'Closest'


def stage():
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 32
    s.render.resolution_x, s.render.resolution_y = 1600, 1000
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new('lineup')
    bg = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    bg.inputs['Color'].default_value = (0.42, 0.46, 0.44, 1.0)
    bg.inputs['Strength'].default_value = 0.9
    sun = bpy.data.objects.new('lineup_sun', bpy.data.lights.new('lineup_sun', 'SUN'))
    sun.data.energy = 2.4
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(25))
    s.collection.objects.link(sun)
    bpy.ops.mesh.primitive_plane_add(size=40)
    floor = bpy.context.active_object
    floor.data.materials.append(flat('lineup_floor', (0.16, 0.18, 0.13)))


def flat(name, rgb):
    mat = bpy.data.materials.new(name)
    next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (*rgb, 1)
    return mat


def shot(name, eye, target, ortho=None, lens=50):
    s = bpy.context.scene
    camera = bpy.data.objects.new('lineup_' + name, bpy.data.cameras.new('lineup_' + name))
    s.collection.objects.link(camera)
    camera.location = eye
    camera.rotation_euler = (target - eye).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO' if ortho else 'PERSP'
    camera.data.ortho_scale = ortho or 1.0
    camera.data.lens = lens
    s.camera = camera
    s.render.filepath = os.path.join(OUT, name + '.png')
    bpy.ops.render.render(write_still=True)


main()

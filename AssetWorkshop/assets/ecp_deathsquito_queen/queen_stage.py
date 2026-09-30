"""The preview's stage: Plains heath under the Plains' orange sun and grey sky (codex/biomes.md), a few of the game's
own Plains props round the arena and two players standing in for the ones she fights (the game's Player prefab, from
the local reference export: preview only, never exported), and the cameras, one per part of the fight."""
import math

import bpy
from mathutils import Vector
from workshop import materials, prefab

from queen_keys import shown

SUN, SUN_ENERGY = (1.0, 0.63, 0.39), 4.0        # the Plains sun #ffa163
SKY, SKY_STRENGTH = (0.42, 0.44, 0.45), 0.9     # grey ambient
HEATH = [(0.18, 0.16, 0.06), (0.23, 0.21, 0.09), (0.31, 0.28, 0.13)]   # #7b7347 / #817849 / #998f5a, linear
PROPS = [("world/Props/Birch/Birch1_aut.prefab", (-30.0, 40.0), 20), ("world/Props/Birch/Birch1_aut.prefab",
          (38.0, 52.0), 130), ("world/Props/Birch/Birch1_aut.prefab", (-40.0, -16.0), 250),
         ("world/Props/Bush01/Bush01_heath.prefab", (-12.0, 13.0), 0), ("world/Props/Bush01/Bush01_heath.prefab",
          (15.0, 15.0), 60), ("world/Props/Bush01/Bush01_heath.prefab", (-11.0, -12.0), 200),
         ("world/Props/Bush01/Bush01_heath.prefab", (13.0, -11.0), 300), ("world/Props/Rock_4_plains.prefab",
          (-48.0, 34.0), 40), ("world/Props/Rocks/rock4_heath.prefab", (46.0, -40.0), 100)]   # far out: backdrop
PLAYER = "Characters/Player/Player.prefab"


def build(players):
    """Ground, light, props, the players (each turned to face +Y, where she comes from); returns the player roots."""
    _ground()
    _light()
    for path, (x, y), turn in PROPS:
        root = prefab.load(path, "prop " + path.rsplit("/", 1)[-1])
        root.location, root.rotation_euler = (x, y, 0.0), (0.0, 0.0, math.radians(turn))
    stand = []
    for i, spot in enumerate(players):
        root = prefab.load(PLAYER, f"player {i + 1}")
        root.location, root.rotation_euler = spot, (0.0, 0.0, math.pi)
        stand.append(root)
    return stand


def _ground():
    bpy.ops.mesh.primitive_plane_add(size=120, location=(0, 4, 0))
    ground = bpy.context.active_object
    ground.name = "ground"
    mat = materials.flat("heath", HEATH[1], roughness=0.95)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value, noise.inputs['Detail'].default_value = 0.35, 6.0
    ramp = nodes.new('ShaderNodeValToRGB')
    for element, (position, colour) in zip(ramp.color_ramp.elements, [(0.4, HEATH[0]), (0.62, HEATH[2])]):
        element.position, element.color = position, (*colour, 1.0)
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], materials.principled(mat).inputs['Base Color'])
    ground.data.materials.append(mat)


def _light():
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN'))
    sun.data.energy, sun.data.color, sun.data.angle = SUN_ENERGY, SUN, math.radians(3)
    sun.rotation_euler = (math.radians(52), 0.0, math.radians(-35))
    bpy.context.scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("plains")
    world.use_nodes = True
    background = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
    background.inputs['Color'].default_value, background.inputs['Strength'].default_value = (*SKY, 1.0), SKY_STRENGTH
    bpy.context.scene.world = world


def camera(name, eye, look, lens=30):
    cam = bpy.data.objects.new("cam " + name, bpy.data.cameras.new("cam " + name))
    bpy.context.scene.collection.objects.link(cam)
    cam.location = Vector(eye)
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens, cam.data.clip_start, cam.data.clip_end = lens, 0.05, 400.0
    return cam


def caption(cam, text, first, last, keys, frames):
    """A caption in the camera's top left corner, shown from `first` to `last`."""
    curve = bpy.data.curves.new("caption " + text, 'FONT')
    curve.body, curve.size, curve.align_x = text, 0.055 * 30.0 / cam.data.lens, 'LEFT'   # the same on any lens
    label = bpy.data.objects.new("caption " + text, curve)
    bpy.context.scene.collection.objects.link(label)
    label.parent = cam
    width = 36.0 / cam.data.lens            # the view's half width at 1 m (36 mm sensor)
    label.location = (-width * 0.47, width * 0.24, -1.0)
    label.data.materials.append(_glow("caption", (0.95, 0.9, 0.78)))
    label.visible_shadow = False
    shown(keys, label, frames, first, last)
    return label


def _glow(name, colour):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = materials.flat(name, colour)
    bsdf = materials.principled(mat)
    bsdf.inputs['Emission Color'].default_value, bsdf.inputs['Emission Strength'].default_value = (*colour, 1.0), 1.5
    return mat

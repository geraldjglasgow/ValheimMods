"""Assemble the local, fully textured creature fittings into an interactive gallery."""
from pathlib import Path
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'blender'))
from workshop import scene, preview, materials

OUT = Path(__file__).resolve().parent / 'out'
KINDS = [('ecp_mire_jarl', 'Mire Jarl', 1.35),
         ('ecp_reed_stalker', 'Reed Stalker', 1.05),
         ('ecp_bog_maw', 'Bog Maw', 1.25),
         ('ecp_fen_crawler', 'Fen Crawler', 1.5),
         ('ecp_drowned_shade', 'Drowned Shade', 1.1)]
scene.clear()
creatures = []
for i, (asset, name, scale) in enumerate(KINDS):
    with bpy.data.libraries.load(str(OUT / (asset + '_review.blend')), link=False) as (src, dst):
        dst.objects = [name]
    obj = dst.objects[0]
    if obj is None:
        raise RuntimeError('Missing fitted creature: ' + name)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = ((i - 2) * 3.1, 0, 0)
    obj.scale = (scale,) * 3
    creatures.append(obj)
    for material in obj.data.materials:
        if material and material.use_nodes:
            for node in material.node_tree.nodes:
                if node.type == 'TEX_IMAGE':
                    node.interpolation = 'Closest'

bpy.context.view_layer.update()
preview._stage(creatures[0])
camera = preview._camera()
labelmat = materials.flat('Gallery lettering', (.55, .57, .42))
for i, (_, name, _) in enumerate(KINDS):
    curve = bpy.data.curves.new(name + ' label', 'FONT')
    curve.body = name
    curve.align_x = 'CENTER'
    curve.size = .23
    label = bpy.data.objects.new(name + ' label', curve)
    bpy.context.scene.collection.objects.link(label)
    label.location = ((i - 2) * 3.1, -1.15, .015)
    curve.materials.append(labelmat)

s = bpy.context.scene
s.render.resolution_x = 1900
s.render.resolution_y = 720
preview._render(camera, creatures, Vector((.07, -1, .3)), True,
                str(OUT / 'blender_gallery.png'))
# Open directly to a colored, lit view; textures remain packed for reopening.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.shading.type = 'MATERIAL'
            space.shading.use_scene_world = False
            space.shading.use_scene_lights = False
            space.overlay.show_overlays = False
            space.region_3d.view_perspective = 'CAMERA'
            space.region_3d.view_camera_zoom = 0
scene.select_only(creatures, creatures[0])
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Swamp_Creatures_Colored.blend'))

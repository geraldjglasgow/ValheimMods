"""Pack the rat's textures and save a framed material-preview Blender scene."""
import os
import bpy
from mathutils import Vector


def prepare():
    mesh = bpy.data.objects['ecp_crypt_rat']
    bpy.ops.object.select_all(action='DESELECT')
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    for material in mesh.data.materials:
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                node.interpolation = 'Closest'
                if not node.image.packed_file:
                    node.image.pack()
    direction = Vector((2.6, -3.0, 1.75))
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type != 'VIEW_3D':
                continue
            space = area.spaces.active
            space.shading.type = 'MATERIAL'
            space.shading.use_scene_world = False
            space.shading.use_scene_lights = False
            space.overlay.show_overlays = False
            space.region_3d.view_rotation = direction.to_track_quat('Z', 'Y')
            space.region_3d.view_location = (0, .18, .34)
            space.region_3d.view_distance = 3.5
            space.region_3d.view_perspective = 'PERSP'


if __name__ == '__main__':
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out')
    bpy.ops.wm.open_mainfile(filepath=os.path.join(out, 'ecp_crypt_rat_animated.blend'))
    prepare()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, 'ecp_crypt_rat_colored.blend'))
    print('WORKSHOP colored scene: packed textures and material preview enabled')

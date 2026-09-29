"""1.5 metre bone greataxe: articulated vertebral haft and a bearded scapula blade."""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
for folder in ('ecp_spine_greataxe', 'ecp_skel_arsenal'):
    sys.path.insert(0, os.path.join(HERE, '..', folder))

import bpy
from mathutils import Matrix, Vector
import axe_blade
import axe_mesh as geo
import axe_paint
import grave_vertebra
from workshop import scene

TEXTURE_SIZE = 256
NORMAL_MAP = False
AO_STRENGTH = 0.32


def spine_point(z):
    t = max(0, min(1, (z - .065) / 1.102))
    return Vector((.040 * math.sin(t * math.tau), .012 * math.sin(t * math.pi), z))


def spine_axis(z):
    return (spine_point(z + .002) - spine_point(z - .002)).normalized()


def build():
    # Linear colours matched to the cream/tan bones of Skeleton_d, not its iron islands.
    palette = ((0.085, 0.048, 0.023), (0.23, 0.145, 0.080), (0.48, 0.375, 0.245))
    bone = axe_paint.bone('Skeleton_matched_bone', palette, stain=0.35)
    edge = axe_paint.bone('Ground_bone_edge',
                          ((0.19, 0.13, 0.07), (0.43, 0.35, 0.23), (0.59, 0.50, 0.35)),
                          stain=0)
    dark = axe_paint.socket_dark('Old_bone_fissures')
    for mat in (bone, edge):
        bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        for link in list(bsdf.inputs['Normal'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Roughness'].default_value = 0.76

    # A slender, articulated S-curve; each vertebra follows the local spinal tangent.
    # Gradual changes in size and short processes avoid the old ladder silhouette.
    for i in range(22):
        z = .065 + i * 1.102 / 21
        t = i / 21
        radius = .029 + .004 * math.sin(math.pi * t) ** 2
        grip = abs(z - .34) < .13 or abs(z - .73) < .12
        frame = grave_vertebra.frame(spine_point(z), spine_axis(z), (-1, 0, 0))
        frame = frame @ Matrix.Diagonal((.86, .74 if grip else .86, 1.45, 1))
        grave_vertebra.build('Vertebra_%02d' % i, frame, radius, bone,
                             'neck' if grip else 'thoracic')
        if i < 21:
            points = [spine_point(z + dz) for dz in (.016, .026, .039)]
            geo.sweep('Interlocked_joint_%02d' % i, points,
                      [(.017, .015), (.016, .014), (.017, .015)], bone,
                      hint=(0, 1, 0), sides=8)
    geo.loft_z('Coccyx', [(0, .006, .006, 0, 0), (.04, .020, .017, 0, 0),
                          (.08, .024, .020, 0, 0)], bone, sides=7, smooth=False)
    # Keep the user's approved head geometry and painted texture exactly as authored.
    with bpy.data.libraries.load(os.path.join(HERE, 'approved_head.blend')) as (src, dst):
        dst.objects = ['Approved_axe_head']
    head = dst.objects[0]
    bpy.context.collection.objects.link(head)
    head.data.uv_layers.active.name = 'ApprovedUV'
    head.data.uv_layers.new(name='UVMap')
    head.data.uv_layers.active_index = 1
    head.data.uv_layers['UVMap'].active_render = True
    for mat in head.data.materials:
        uv = mat.node_tree.nodes.new('ShaderNodeUVMap')
        uv.uv_map = 'ApprovedUV'
        for node in list(mat.node_tree.nodes):
            if node.type == 'TEX_IMAGE':
                mat.node_tree.links.new(uv.outputs['UV'], node.inputs['Vector'])
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for obj in meshes:
        if not obj.data.uv_layers:
            obj.data.uv_layers.new(name='UVMap')
    # Bake component placement before normalising the overall height.
    bpy.context.view_layer.update()
    for obj in meshes:
        obj.data.transform(obj.matrix_world)
        obj.matrix_world = Matrix.Identity(4)
        obj.data.update()
    bpy.context.view_layer.update()
    low, high = scene.bounds(meshes)
    factor = 1.5 / (high.z - low.z)
    # Grounded origin; all dimensions are baked in metres, no object scaling.
    for obj in meshes:
        for v in obj.data.vertices:
            v.co.z -= low.z
            v.co *= factor
        obj.data.update()
    bpy.context.view_layer.update()
    low, high = scene.bounds(meshes)
    assert abs(high.z - low.z - 1.5) < 1e-5
    # FBX tangents require triangles/quads; preserve the authored shading.
    import bmesh
    for obj in meshes:
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.triangulate(mesh, faces=list(mesh.faces))
        mesh.to_mesh(obj.data)
        mesh.free()

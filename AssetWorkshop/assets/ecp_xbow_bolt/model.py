"""The Skeleton Crossbowman's bolt (Elite Creatures Pack): a thin bone shaft grimed like the skeleton itself, a blunt
knuckle of bone for a head (it does blunt damage; 2026-09-29, the user), bound on with sinew, and three short vanes of
dark, ragged feathers. One model for every bolt the
crossbowman shows: in the groove, in the fingers, in the quiver and in flight (the mod puts it on the bolt projectile).

The origin is the nock end on the shaft's line and the point is forward (-Y, Unity +Z), LENGTH away. The vanes stand
at 120 degrees, one straight up, and reach VANE_RADIUS from the line: small enough to clear the stock under a laid bolt
and to stand five in the quiver's mouth.
"""
import math

import bmesh
import bpy
from mathutils import Vector

from workshop import materials, shapes

TEXTURE_SIZE = 256
AO_STRENGTH = 0.6

LENGTH = 0.43
VANE_RADIUS = 0.016
SHAFT = [(0.000, 0.0048), (-0.030, 0.0056), (-0.160, 0.0052), (-0.300, 0.0050), (-0.360, 0.0060)]   # (y, radius)


def build():
    bone, point, feather, sinew = _bone(), _point_bone(), _feather(), _sinew()
    _shaft(bone)
    _knob("nock", (0.0, 0.002, 0.0), 0.0062, bone)
    _blunt(point)
    for i in range(3):
        _vane(f"vane_{i}", math.radians(90 + 120 * i), feather)
    for name, y0, y1 in (("bind_point", -0.364, -0.350), ("bind_tail", -0.112, -0.100), ("bind_nock", -0.016, -0.008)):
        _wrap(name, y0, y1, sinew)


def _shaft(material, sides=8):
    mesh = bmesh.new()
    rings = [[mesh.verts.new((r * math.cos(a), y, r * math.sin(a)))
              for a in (2 * math.pi * k / sides for k in range(sides))] for y, r in SHAFT]
    for a, b in zip(rings, rings[1:]):
        for i in range(sides):
            j = (i + 1) % sides
            mesh.faces.new((a[i], a[j], b[j], b[i]))
    mesh.faces.new(list(reversed(rings[0])))
    mesh.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    _object("shaft", mesh, material)


def _blunt(material):
    """A blunt head, for crushing rather than piercing: a knuckle of bone swelling out of a thick socket, rounded in front
    and a little flattened at the tip, like a hunter's blunt."""
    shapes.cylinder("socket", 0.0078, 0.034, (0.0, -0.372, 0.0), (math.radians(90), 0.0, 0.0), material, vertices=8)
    _knob("knuckle", (0.0, -0.405, 0.0), 0.0135, material, squash=(1.0, 1.25, 0.95))
    _knob("knuckle_face", (0.0, -0.421, 0.0), 0.0105, material, squash=(1.05, 0.55, 1.0))
    for side in (-1, 1):
        _knob(f"knuckle_lobe_{side}", (side * 0.008, -0.408, 0.002), 0.008, material)


def _vane(name, angle, material):
    """A ragged feather vane standing out from the shaft at `angle` (from +X round the line), thin but not flat."""
    out = Vector((math.cos(angle), 0.0, math.sin(angle)))
    across = Vector((-math.sin(angle), 0.0, math.cos(angle))) * 0.0008
    profile = [(-0.012, 0.004), (-0.020, VANE_RADIUS), (-0.060, VANE_RADIUS * 0.9), (-0.085, VANE_RADIUS * 0.65),
               (-0.105, 0.005)]
    mesh = bmesh.new()
    front = [mesh.verts.new(Vector((0, y, 0)) + out * r + across) for y, r in profile]
    back = [mesh.verts.new(Vector((0, y, 0)) + out * r - across) for y, r in profile]
    root = [mesh.verts.new(Vector((0, y, 0)) + out * 0.0035 + side) for y in (-0.012, -0.105) for side in (across, -across)]
    mesh.faces.new(front + [root[2], root[0]])
    mesh.faces.new(list(reversed(back)) + [root[1], root[3]])
    for i in range(len(profile) - 1):
        mesh.faces.new((front[i], back[i], back[i + 1], front[i + 1]))
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    _object(name, mesh, material)


def _wrap(name, y0, y1, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.0068, depth=y1 - y0, location=(0.0, (y0 + y1) / 2, 0.0),
                                        rotation=(math.radians(90), 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)


def _knob(name, location, radius, material, squash=(1.0, 0.7, 1.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = squash
    obj.data.materials.append(material)


def _object(name, mesh, material):
    data = bpy.data.meshes.new(name)
    mesh.to_mesh(data)
    mesh.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)


def _mottled(name, dark, light, scale, roughness, bump=0.0, stretch=(1.0, 1.0, 1.0)):
    """Old bone, grimed: a pale base mottled with brown, as the game paints its skeletons."""
    mat = bpy.data.materials.new(name)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = materials.principled(mat)
    bsdf.inputs['Roughness'].default_value = roughness
    coords = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = stretch
    links.new(coords.outputs['Object'], mapping.inputs['Vector'])
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = 7.0
    links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[1].position = 0.32, 0.68
    ramp.color_ramp.elements[0].color = (*dark, 1.0)
    ramp.color_ramp.elements[1].color = (*light, 1.0)
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    if bump > 0:
        node = nodes.new('ShaderNodeBump')
        node.inputs['Strength'].default_value = bump
        node.inputs['Distance'].default_value = 0.002
        links.new(noise.outputs['Fac'], node.inputs['Height'])
        links.new(node.outputs['Normal'], bsdf.inputs['Normal'])
    return mat


def _bone():
    """The crossbow's bone, grimed a little more: the skeleton's own beige with brown in it."""
    return _mottled("bolt_bone", (0.19, 0.135, 0.075), (0.50, 0.42, 0.29), 140.0, 0.75, bump=0.3, stretch=(1.0, 0.3, 1.0))


def _point_bone():
    return _mottled("bolt_point", (0.24, 0.18, 0.10), (0.58, 0.50, 0.36), 220.0, 0.6, bump=0.4)


def _feather():
    """Dark, dirty feathers, lighter towards the edges."""
    return _mottled("bolt_feather", (0.025, 0.022, 0.02), (0.13, 0.11, 0.09), 400.0, 0.8, stretch=(1.0, 3.0, 1.0))


def _sinew():
    return _mottled("bolt_sinew", (0.10, 0.075, 0.05), (0.27, 0.21, 0.14), 300.0, 0.7)

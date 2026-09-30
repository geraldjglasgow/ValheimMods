"""Original faceted rat parts; vertex groups survive the workshop atlas join."""
import math
import bpy
from mathutils import Vector


def finish(obj, name, mat, bone):
    obj.name = name
    obj.data.materials.append(mat)
    obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    return obj


def ellipsoid(name, pos, size, mat, bone, segments=12, rings=7):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=pos)
    obj = bpy.context.object
    obj.scale = size
    return finish(obj, name, mat, bone)


def tube(name, points, radii, mat, bone, sides=7):
    verts, faces = [], []
    for i, point in enumerate(points):
        tangent = Vector(points[min(i+1, len(points)-1)]) - Vector(points[max(i-1, 0)])
        tangent.normalize()
        u = tangent.cross(Vector((1, 0, 0)))
        if u.length < .01:
            u = tangent.cross(Vector((0, 0, 1)))
        u.normalize()
        v = tangent.cross(u)
        for j in range(sides):
            a = math.tau*j/sides
            verts.append(Vector(point) + radii[i]*(u*math.cos(a)+v*math.sin(a)))
    for i in range(len(points)-1):
        for j in range(sides):
            a, b = i*sides+j, i*sides+(j+1)%sides
            faces.append((a, b, b+sides, a+sides))
    faces.extend([tuple(reversed(range(sides))), tuple(range(len(verts)-sides, len(verts)))])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, bone)

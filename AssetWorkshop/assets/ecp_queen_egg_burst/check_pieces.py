"""Checks the built burst egg the way a mod script will use it: separates it by loose parts and reports each piece
(which one is the cup, each petal's angle), that there are six, and the closest any two pieces come (they must not
touch). Run after a build:

    blender --background --factory-startup --python assets/ecp_queen_egg_burst/check_pieces.py
"""
import itertools
import math
import os

import bmesh
import bpy
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
NAME = os.path.basename(HERE)


def pieces():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, "out", NAME + ".blend"))
    obj = bpy.data.objects[NAME]
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.separate(type='LOOSE')
    bpy.ops.object.mode_set(mode='OBJECT')
    return [o for o in bpy.data.objects if o.type == 'MESH']


def describe(obj):
    points = [obj.matrix_world @ v.co for v in obj.data.vertices]
    centre = sum(points, points[0] * 0) / len(points)
    low = min(p.z for p in points)
    angle = math.degrees(math.atan2(centre.y, centre.x))
    closed = all(len(e.link_faces) == 2 for e in _bm(obj).edges)
    return low, angle, len(obj.data.polygons), closed


def _bm(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    return bm


def closest(a, b):
    """The smallest distance from any vertex of one piece to the other's surface, and whether they overlap."""
    ta, tb = BVHTree.FromObject(a, bpy.context.evaluated_depsgraph_get()), BVHTree.FromObject(
        b, bpy.context.evaluated_depsgraph_get())
    overlap = bool(ta.overlap(tb))
    near = min(min(tb.find_nearest(v.co)[3] for v in a.data.vertices),
               min(ta.find_nearest(v.co)[3] for v in b.data.vertices))
    return near, overlap


def main():
    found = pieces()
    print(f"CHECK pieces: {len(found)}")
    for obj in sorted(found, key=lambda o: describe(o)[0]):
        low, angle, faces, closed = describe(obj)
        kind = "cup" if low < -0.1 else f"petal at {angle:+.0f} deg"
        print(f"CHECK {obj.name}: {kind}, lowest z {low:+.3f}, {faces} faces, closed {closed}")
    gaps = [(closest(a, b), a.name, b.name) for a, b in itertools.combinations(found, 2)]
    (near, overlap), a, b = min(gaps, key=lambda g: g[0][0])
    print(f"CHECK closest pieces: {a} and {b}, {near * 100:.2f} cm apart; any overlap: {any(g[0][1] for g in gaps)}")


main()

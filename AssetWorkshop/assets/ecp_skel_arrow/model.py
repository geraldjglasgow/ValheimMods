"""The skeleton arsenal's arrow (Elite Creatures Pack), v2, shot from the skeleton bow (ecp_skel_bow): its head is a small
vertebra lying flat, bound with sinew onto the end of a thin grimed bone shaft, the canal showing through, its wings
standing out as barbs and its spine ground into the point; three ragged dark feathers. Long like the game's own arrows
(its arrow projectile is 1.37 m).

The origin is the nock on the shaft's line and the point is forward (-Y, Unity +Z), LENGTH away; the vanes stand at
120 degrees, one straight up (+Z). v1 (a bone point with a vertebra bead) is archived in assets/skeleton_weapons_v1.
"""
import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402

import grave_blade as gb  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 256
AO_STRENGTH = 0.55

LENGTH = 1.16
VANE_RADIUS = 0.03
SHAFT_END, HEAD_R = 1.03, 0.0125
# (s from the spine's root, half width, half thickness, bevel, ridge)
POINT = [(0.0, 0.005, 0.0048, 0.3, 1.2), (0.028, 0.013, 0.0044, 0.42, 1.35), (0.065, 0.008, 0.0036, 0.48, 1.3)]


def build():
    bone, spine, feather, sinew = gp.bone("arrow_bone"), gp.vertebra("arrow_vertebra"), gp.feather(), gp.sinew("arrow_sinew")
    gs.loft("shaft", [(0.004, 0.0058, 0.0058), (0.6, 0.0064, 0.0064), (SHAFT_END, 0.006, 0.006)], bone, sides=6, roundness=1.0)
    gs.knob("nock", gs.at(0.0), 0.0078, bone, squash=(1.0, 1.3, 1.0))
    _, root = gv.flat("head", SHAFT_END - 0.003, HEAD_R, spine, pegs=False)
    sections = [(root + ds, -w, w, t, bevel, ridge) for ds, w, t, bevel, ridge in POINT]
    gb.blade("point", sections, gp.bone_blade("arrow_point"), tip=(LENGTH, 0.0))
    gs.wrap("head_binding", SHAFT_END - 0.03, SHAFT_END + 0.002, 0.0068, 0.0045, 0.0018, sinew)
    for i in range(3):
        _vane(f"vane_{i}", math.radians(90 + 120 * i), feather)
    gs.wrap("fletch_binding", 0.018, 0.028, 0.0065, 0.004, 0.0016, sinew)


def _vane(name, angle, material):
    """A ragged feather vane standing out from the shaft at `angle` (from +X round the line), thin but not flat."""
    out = Vector((math.cos(angle), 0.0, math.sin(angle)))
    across = Vector((-math.sin(angle), 0.0, math.cos(angle))) * 0.0011
    profile = [(-0.035, 0.005), (-0.055, VANE_RADIUS * 0.8), (-0.09, VANE_RADIUS), (-0.13, VANE_RADIUS * 0.85),
               (-0.16, VANE_RADIUS * 0.55), (-0.2, 0.006)]
    mesh = bmesh.new()
    front = [mesh.verts.new(Vector((0, y, 0)) + out * r + across) for y, r in profile]
    back = [mesh.verts.new(Vector((0, y, 0)) + out * r - across) for y, r in profile]
    root = [mesh.verts.new(Vector((0, y, 0)) + out * 0.004 + side) for y in (-0.035, -0.2) for side in (across, -across)]
    mesh.faces.new(front + [root[2], root[0]])
    mesh.faces.new(list(reversed(back)) + [root[1], root[3]])
    for i in range(len(profile) - 1):
        mesh.faces.new((front[i], back[i], back[i + 1], front[i + 1]))
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    return gs.mesh_object(name, mesh, material, smooth=False)

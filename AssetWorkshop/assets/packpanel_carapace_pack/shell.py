"""The Carapace Pack's shell: a domed shield over the top of the bag with its lower edge drawn to a point at the
middle, an eitr stone set in that point, and two wing cases down the back, each of three plates overlapping
downwards like a beetle's, parted by a narrow seam. Every plate is a grid on the bag's surface (body.net), lifted off
it more towards its lower edge, so each plate's lip stands over the top of the next.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import body
import forms

GAP = 0.008                  # half the seam between the wing cases, metres
WRAP = 0.8                   # how far the plates reach forward round the bag's sides (net columns past the back)
SHIELD_FRONT = 1.72          # the shield's front edge, on the bag's top (net row)
SHIELD_POINT, SHIELD_SIDE = 1.438, 1.505    # the shield's lower edge at the middle and at its sides (z)
# Wing plates: (top edge z, bottom edge z) at the middle and how much each edge rises at the sides (m); the last
# plate's lower edge wraps under the bag, given as net rows (middle, rise).
WINGS = [((1.458, 0.0), (1.338, 0.025)), ((1.352, 0.025), (1.238, 0.035)), ((1.252, 0.035), None)]
LAST_EDGE = (-1.2, 0.5)


def shield(material, count=13, rows=7):
    """The shield; returns where its eitr stone sits (centre, outward normal, up along the surface)."""
    columns = [(1 + WRAP) * (2 * i / (count - 1) - 1) for i in range(count)]
    points, normals = body.surface_grid(columns, lambda c: _shield_rows(c, rows))
    lifted, along, across = [], [], []
    for j, (row, nrm) in enumerate(zip(points, normals)):
        t = j / (rows - 1)
        lifted.append([p + n * _shield_lift(t, c) for p, n, c in zip(row, nrm, columns)])
        along.append([t] * count)
        across.append([c / (1 + WRAP) for c in columns])
    forms.plate("shield", lifted, normals, 0.009, material, {"along": along, "across": across})
    return _stone_seat(rows)


def _shield_rows(c, rows):
    point = min(abs(c) / 0.45, 1.0) ** 1.3
    end = body.row_for(c, SHIELD_POINT + (SHIELD_SIDE - SHIELD_POINT) * point)
    return [SHIELD_FRONT + (end - SHIELD_FRONT) * j / (rows - 1) for j in range(rows)]


def _shield_lift(t, c):
    """Standing off the bag more towards the lower edge, and crowned along the middle."""
    return 0.010 + 0.014 * t ** 1.5 + 0.004 * (1 - min(abs(c), 1.0)) ** 2


def _stone_seat(rows, t=0.8):
    row = SHIELD_FRONT + (body.row_for(0.0, SHIELD_POINT) - SHIELD_FRONT) * t
    points, normals = body.surface_grid([-0.02, 0.0, 0.02], lambda c: (row + 0.02, row, row - 0.02))
    normal = normals[1][1]
    up = (points[0][1] - points[2][1]).normalized()
    return points[1][1] + normal * _shield_lift(t, 0.0), normal, up


def stone(material, centre, normal, up, half=(0.015, 0.022, 0.009)):
    """An eitr stone, a flattened egg half sunk into the shield."""
    mesh = bmesh.new()
    bmesh.ops.create_uvsphere(mesh, u_segments=10, v_segments=7, radius=1.0)
    across = up.cross(normal).normalized()
    turn = Matrix((across * half[0], up * half[1], normal * half[2])).transposed()
    for vert in mesh.verts:
        vert.co = centre + turn @ vert.co
    data = bpy.data.meshes.new("eitr_stone")
    mesh.to_mesh(data)
    mesh.free()
    obj = bpy.data.objects.new("eitr_stone", data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(material)
    data.shade_smooth()
    return obj


def wings(material, count=8, rows=5):
    """Both wing cases, three plates each."""
    for side in (-1, 1):
        for k, (top, bottom) in enumerate(WINGS):
            _wing_plate(f"wing_{side}_{k}", material, side, top, bottom, count, rows)


def _wing_plate(name, material, side, top, bottom, count, rows):
    fractions = [i / (count - 1) for i in range(count)]
    start = GAP / 0.17
    columns = [side * (start + f * (1 + WRAP - start)) for f in fractions]
    edges = {c: (_edge(c, top, f), _edge(c, bottom, f)) for c, f in zip(columns, fractions)}
    grid = lambda c: [edges[c][0] + (edges[c][1] - edges[c][0]) * j / (rows - 1) for j in range(rows)]  # noqa: E731
    points, normals = body.surface_grid(columns, grid)
    lifted, along, across = [], [], []
    for j, (row, nrm) in enumerate(zip(points, normals)):
        t = j / (rows - 1)
        lifted.append([p + n * _wing_lift(t, f, bottom is None) for p, n, f in zip(row, nrm, fractions)])
        along.append([t] * count)
        across.append([max(-1.0, min(1.0, (f - 0.3) / 0.6)) for f in fractions])
    forms.plate(name, lifted, normals, 0.007, material, {"along": along, "across": across})


def _edge(c, edge, f):
    """A plate edge's net row at column c, fraction f of the way from the seam (0) to the side (1)."""
    if edge is None:
        return LAST_EDGE[0] + LAST_EDGE[1] * f * f
    z, rise = edge
    return body.row_for(c, z + rise * f * f)


def _wing_lift(t, f, last):
    """Standing off the bag more towards the lower edge (the last plate's lip turns in again under the bag), domed
    across each wing case (low at the seam)."""
    lip = 0.017 * t ** 1.6 * ((1 - 0.65 * t ** 4) if last else 1.0)
    return 0.003 + lip + 0.005 * math.sin(math.pi * min(f / 0.62, 1.0))

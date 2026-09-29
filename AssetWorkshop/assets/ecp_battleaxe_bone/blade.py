"""The blade: a giant's flat bone ground into a bearded crescent, in the XZ plane (edge towards +X, thin along Y).

One closed outline in three chains (upper back line, cutting edge, beard), each a Catmull-Rom curve through a few
control points. An inset copy of the outline is where the bone keeps its full thickness; between it and the outline
the edge is ground down (a wide paler bevel along the cutting edge) or rolls into a thicker rim (along the back lines,
as a bone's border does). Inside, a crown loop stands proud of the faces, so the blade is a thick faceted slab, not a
sheet. A natural hole goes through near the root.
"""
import math

import bmesh
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

import geo

ROOT_X, ROOT_Z = 0.018, 1.235        # the root line sits inside the haft, hidden by the lashing
SCALE = 0.85                         # the outline below, drawn large, is scaled about the root by this
UPPER = [(ROOT_X, 1.372), (0.09, 1.360), (0.18, 1.390), (0.29, 1.455), (0.385, 1.565)]
EDGE = [(0.385, 1.565), (0.450, 1.470), (0.497, 1.350), (0.515, 1.215), (0.500, 1.080), (0.455, 0.960),
        (0.380, 0.860), (0.280, 0.790), (0.195, 0.765)]
BEARD = [(0.195, 0.765), (0.212, 0.845), (0.180, 0.940), (0.115, 1.030), (ROOT_X, 1.090)]
BEVEL, RIM, TIP = 0.030, 0.012, 0.009    # inset along the cutting edge, the back lines' rim, at the two points
EDGE_HALF = 0.0012                   # half-thickness left at the cutting edge
RIM_SWELL = 1.30                     # the rim is this much thicker than the face inside it
KNAP = (0.002, -0.003, 0.002, -0.002, 0.001, -0.003, 0.002, 0.0, -0.016, 0.002, -0.003, 0.002, -0.001, 0.002,
        -0.002)
CROWN_CENTRE, CROWN_SIZE, CROWN_SWELL = (0.300, 1.190), 0.50, 1.45
HOLE = ((0.135, 1.115), 0.018, 0.028, math.radians(-25), 8)      # centre, rx, rz, tilt, points


def half_thickness(x):
    """The bone is thickest at the root (3 cm) and thins to 1.4 cm out at the edge."""
    return 0.015 - 0.008 * min(1.0, max(0.0, x / 0.40))


def build(bone, edge):
    outline, inset, kinds = _outline()
    hole, crown = _hole(), _crown(inset)
    loops = inset + hole + crown
    swell = [1.0] * (len(inset) + len(hole)) + [CROWN_SWELL] * len(crown)
    verts, faces, mats = [], [], []
    front, back = _face_loops(verts, loops, swell)
    outer_f, outer_b = _outer_loops(verts, outline, kinds)
    n, h = len(inset), len(hole)
    for a, b, strip_kinds in ((front[:n], outer_f, kinds), (outer_f, outer_b, kinds), (outer_b, back[:n], kinds),
                              (back[n:n + h], front[n:n + h], ['back'] * h)):
        _strip(faces, mats, a, b, strip_kinds, bone, edge)
    _fill(faces, mats, front, back, [inset, hole, crown], 0, bone)
    _fill(faces, mats, front, back, [crown], n + h, bone)
    obj = geo.mesh_object('blade', verts, faces, bone, smooth=False, face_materials=mats)
    _outward_normals(obj)
    return [obj]


def _outline():
    """(outline points, inset points, kind per point) round the closed outline; kinds 'edge', 'tip' or 'back'."""
    points, kinds, tips = [], [], []
    for control, kind in ((UPPER, 'back'), (EDGE, 'edge'), (BEARD, 'back')):
        curve = geo.catmull([Vector(_scaled(x, z) + (0.0,)) for x, z in control], 2)
        start = 1 if points else 0
        if points:
            tips.append(len(points) - 1)           # the corner where two chains meet: the horn, the beard's tip
        points += [(p.x, p.y) for p in curve[start:]]
        kinds += [kind] * (len(curve) - start)
    for tip in tips:
        kinds[tip] = 'tip'
    return _knapped(points, kinds), _inset(points, kinds), kinds


def _scaled(x, z):
    return ROOT_X + (x - ROOT_X) * SCALE, ROOT_Z + (z - ROOT_Z) * SCALE


def _inward(points, i):
    """The unit normal at point i pointing into the outline."""
    sign = 1.0 if _area(points) > 0 else -1.0
    (ax, az), (bx, bz) = points[i - 1], points[(i + 1) % len(points)]
    length = math.hypot(bx - ax, bz - az) or 1.0
    return -(bz - az) / length * sign, (bx - ax) / length * sign


def _inset(points, kinds):
    out = []
    for i, (x, z) in enumerate(points):
        nx, nz = _inward(points, i)
        depth = 0.006 if x < ROOT_X + 0.001 else {'edge': BEVEL, 'tip': TIP, 'back': RIM}[kinds[i]]
        out.append((x + nx * depth, z + nz * depth))
    return out


def _knapped(points, kinds):
    """The cutting edge is not a clean curve: small flakes knocked out of it, one deep chip."""
    out = list(points)
    for n, i in enumerate(i for i, k in enumerate(kinds) if k == 'edge'):
        nx, nz = _inward(points, i)
        cut = KNAP[n % len(KNAP)]
        out[i] = (points[i][0] - nx * cut, points[i][1] - nz * cut)
    return out


def _area(points):
    return sum(x0 * z1 - x1 * z0 for (x0, z0), (x1, z1) in zip(points, points[1:] + points[:1])) / 2


def _hole():
    (cx, cz), rx, rz, tilt, count = HOLE
    pts = []
    for k in range(count):
        a = 2 * math.pi * k / count
        x, z = rx * math.cos(a), rz * math.sin(a)
        pts.append((cx + x * math.cos(tilt) - z * math.sin(tilt), cz + x * math.sin(tilt) + z * math.cos(tilt)))
    return pts


def _crown(inset):
    """Every other inset point pulled towards the middle of the blade: the ridge line of the slab's thick middle."""
    cx, cz = CROWN_CENTRE
    return [(cx + (x - cx) * CROWN_SIZE, cz + (z - cz) * CROWN_SIZE) for x, z in inset[::2]]


def _face_loops(verts, points, swell):
    """Front (-Y) and back (+Y) vertices of the face loops, at the bone's thickness there times `swell`."""
    front, back = [], []
    for (x, z), s in zip(points, swell):
        t = half_thickness(x) * s
        front.append(len(verts))
        verts.append(Vector((x, -t, z)))
        back.append(len(verts))
        verts.append(Vector((x, t, z)))
    return front, back


def _outer_loops(verts, outline, kinds):
    front, back = [], []
    for (x, z), kind in zip(outline, kinds):
        t = half_thickness(x)
        e = EDGE_HALF if kind in ('edge', 'tip') else (t if x < ROOT_X + 0.001 else t * RIM_SWELL)
        front.append(len(verts))
        verts.append(Vector((x, -e, z)))
        back.append(len(verts))
        verts.append(Vector((x, e, z)))
    return front, back


def _strip(faces, mats, a, b, kinds, bone, edge):
    """Quads between two loops of equal length; ground (edge) material where both ends are on the cutting edge."""
    n = len(a)
    for i in range(n):
        j = (i + 1) % n
        faces.append((a[i], a[j], b[j], b[i]))
        mats.append(edge if kinds[i] != 'back' and kinds[j] != 'back' else bone)


def _fill(faces, mats, front, back, loops, offset, bone):
    """Triangulates the region the loops bound (the first outside, the rest holes) on both faces."""
    for tri in tessellate_polygon([[Vector((x, z, 0.0)) for x, z in loop] for loop in loops]):
        faces += [tuple(front[offset + i] for i in tri), tuple(back[offset + i] for i in reversed(tri))]
        mats += [bone, bone]


def _outward_normals(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()

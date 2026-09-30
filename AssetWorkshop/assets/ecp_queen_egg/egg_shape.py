"""The Queen's egg as one surface, shared by the whole egg (ecp_queen_egg) and the burst egg (ecp_queen_egg_burst), so
the burst's pieces sit exactly where the whole egg's shell is.

An ovoid 0.9 m long (Z) and 0.6 m across, narrower towards its top (+Z). Five lobes run from a waist groove up to the
top, with a V groove (a seam) between each two; the waist groove is wavy, low under each lobe's middle and high at the
seams, so each lobe is a tulip petal with a rounded foot. The grooves are where the shell is thin and the Queen's glow
shows through; they are also where the egg splits when it hatches: the lower cup stays in the ground and the five
petals burst outward. A few big lumps and a slow noise keep it from being a perfect egg.

Metres, Z up, the origin at the egg's centre. Petal k's middle faces the angle FRONT + k * 72 degrees (petal 0 faces
the front, -Y); seam k lies 36 degrees before it.
"""
import math

import bmesh
import bpy
from mathutils import Vector, noise

A, C = 0.286, 0.45            # half width, half length (m)
POINT = 0.12                  # the top end narrower than the bottom
PETALS = 5
HALF = math.pi / PETALS       # half a petal (36 degrees)
FRONT = -math.pi / 2          # petal 0's middle faces -Y
SEAM0 = FRONT - HALF          # seam 0, the first petal's left edge
EDGE = math.radians(13)       # a seam groove's wall, seam to lip
GROOVE, BULGE = 0.024, 0.02   # seam groove depth, lobe ridge height
RIM_Z, RIM_WAVE, RIM_W, RIM_DEPTH = -0.02, 0.05, 0.05, 0.02   # the waist groove: height, wave, half width, depth
ROWS = (-0.40, -0.30, -0.16, "rim_low", "rim", "rim_high", 0.14, 0.27, 0.37, 0.425)
CUT = ("rim",)                # the burst splits cup from petals along this row
LUMPS = [  # (angle from FRONT in degrees, z, radius m, height m): big soft bumps
    (18, 0.13, 0.12, 0.026), (118, 0.22, 0.10, 0.022), (215, 0.11, 0.11, 0.024), (292, 0.26, 0.09, 0.02),
    (60, -0.24, 0.13, 0.022), (250, -0.30, 0.12, 0.02), (160, -0.2, 0.1, 0.018)]
NOISE_M, NOISE_SCALE = 0.009, 4.0


def columns():
    """The egg's 20 column angles: per petal its seam, the seam groove's lip, the lobe's ridge, the far lip."""
    per = (0.0, EDGE, HALF, 2 * HALF - EDGE)
    return [SEAM0 + 2 * HALF * k + d for k in range(PETALS) for d in per]


def rim_z(u):
    """The waist groove's bottom at angle u: high at the seams, low under each lobe's middle."""
    return RIM_Z + RIM_WAVE * math.cos(PETALS * (u - SEAM0))


def row_z(row, u):
    """The base height of a row at angle u (the rim rows follow the wave)."""
    if row == "rim_low":
        return rim_z(u) - RIM_W
    if row == "rim":
        return rim_z(u)
    if row == "rim_high":
        return rim_z(u) + RIM_W
    return row


def radius(z):
    s = max(-1.0, min(1.0, z / C))
    return A * math.sqrt(max(0.0, 1.0 - s * s)) * (1.0 - POINT * s)


def base(u, z):
    """The smooth egg's point and outward normal at (u, z)."""
    r, e = radius(z), 1e-4
    slope = (radius(z + e) - radius(z - e)) / (2 * e)
    point = Vector((r * math.cos(u), r * math.sin(u), z))
    normal = Vector((math.cos(u), math.sin(u), -slope)).normalized()
    return point, normal


def seam_angle(u):
    """Angular distance from u to the nearest seam, 0 to HALF."""
    return abs((u - SEAM0 + HALF) % (2 * HALF) - HALF)


def _fades(u, z):
    """(above the waist 0..1, away from the top 0..1, away from the bottom 0..1)."""
    up = max(0.0, min(1.0, (z - rim_z(u)) / RIM_W))
    top = max(0.0, min(1.0, (C - z) / 0.06))
    bottom = max(0.0, min(1.0, (z + C) / 0.1))
    return up, top, bottom


def displacement(u, z, point):
    """How far the shell stands off the smooth egg: seam grooves, lobe ridges, the waist groove, lumps, noise."""
    theta = seam_angle(u)
    up, top, bottom = _fades(u, z)
    seam = -GROOVE * max(0.0, 1.0 - theta / EDGE) * (0.35 + 0.65 * up) * top * bottom
    ridge = math.sin(0.5 * math.pi * max(0.0, min(1.0, (theta - EDGE) / (HALF - EDGE))))
    lobe = BULGE * ridge * (0.6 + 0.4 * up) * top * bottom
    waist = -RIM_DEPTH * max(0.0, 1.0 - abs(z - rim_z(u)) / RIM_W)
    return seam + lobe + waist + _lumps(point) + NOISE_M * noise.noise(point * NOISE_SCALE) * top * bottom


def _lumps(point):
    total = 0.0
    for degrees, z, size, height in LUMPS:
        centre, _ = base(FRONT + math.radians(degrees), z)
        total += height * math.exp(-((point - centre).length / size) ** 2)
    return total


def surface(u, z):
    """(outer point, smooth normal) of the shell at (u, z)."""
    point, normal = base(u, z)
    return point + normal * displacement(u, z, point), normal


def glow(u, z):
    """How much of the Queen's glow shows through at (u, z), 0 to 1: 1 along the grooves' bottoms, 0 at their lips."""
    theta = seam_angle(u)
    waist = max(0.0, 1.0 - abs(z - rim_z(u)) / RIM_W)
    seam = max(0.0, 1.0 - theta / EDGE) * (1.0 if z >= rim_z(u) else 0.0) * (0.75 if z > 0.4 else 1.0)
    return min(1.0, max(waist, seam))


def apex(top):
    """The top (+Z) or bottom point of the egg."""
    return Vector((0.0, 0.0, C if top else -C))


def build_mesh(name, verts, faces, face_materials, glows):
    """A mesh object from vertices and faces, one material per face, a per-vertex `glow` attribute for the glow
    material, normals pointing out of each closed part; flat facets smoothed as the game's creatures are."""
    bm = bmesh.new()
    made = [bm.verts.new(v) for v in verts]
    mats = list(dict.fromkeys(face_materials))
    for face, mat in zip(faces, face_materials):
        bm.faces.new([made[i] for i in face]).material_index = mats.index(mat)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    data = bpy.data.meshes.new(name)
    bm.to_mesh(data)
    bm.free()
    for mat in mats:
        data.materials.append(mat)
    layer = data.attributes.new("glow", 'FLOAT', 'POINT')
    layer.data.foreach_set("value", glows)
    data.polygons.foreach_set('use_smooth', [True] * len(data.polygons))
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def shell_material(glows, face, shell, glowing):
    """The glow material where any corner of the face shows glow, the plain shell elsewhere."""
    return glowing if max(glows[i] for i in face) > 0.01 else shell


def whole(name, shell, glowing):
    """The whole egg: one closed mesh, a fan at each end and the rows of ROWS between, 20 columns round."""
    cols = columns()
    n = len(cols)
    verts, glows = [apex(False)], [0.0]
    for row in ROWS:
        for u in cols:
            z = row_z(row, u)
            verts.append(surface(u, z)[0])
            glows.append(glow(u, z))
    verts.append(apex(True))
    glows.append(0.0)
    faces = [(0, 1 + (i + 1) % n, 1 + i) for i in range(n)]
    for r in range(len(ROWS) - 1):
        a, b = 1 + r * n, 1 + (r + 1) * n
        faces += [(a + i, a + (i + 1) % n, b + (i + 1) % n, b + i) for i in range(n)]
    top, last = len(verts) - 1, 1 + (len(ROWS) - 1) * n
    faces += [(last + i, last + (i + 1) % n, top) for i in range(n)]
    mats = [shell_material(glows, f, shell, glowing) for f in faces]
    return build_mesh(name, verts, faces, mats, glows)

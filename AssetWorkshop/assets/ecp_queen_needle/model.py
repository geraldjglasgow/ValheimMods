"""The Deathsquito Queen's needle (BRIEF.md): the spike she spits from her proboscis, eight in a volley, the misses left
standing in the ground as a hazard (ecp_deathsquito_queen MOVES.md, moves 4 and 6). A bee sting grown big: a banded
amber horn shaft 0.6 m long, thickest in a bulb at its base, tapering to a sharp point; the last 14 cm dark red-brown
with two whorls of three small barbs hooked back.

Built to the codex's `ammo.bolt` (0.56 to 0.78 m bolts, 26 to 326 triangles, 64 px) on one 64 px atlas.
Metres, Z up. THE ORIGIN IS THE TIP and the needle points forward, -Y (Unity +Z): its body runs from the origin along
+Y to the base at y = +0.60. A projectile turned to its velocity (Unity LookRotation) flies it tip first; placed at a
hit point with the same rotation it stands in the ground tip first, its base trailing back along the flight.

    .\build.ps1 -Asset ecp_queen_needle -Lineup
"""
import math

import bmesh
import bpy
from mathutils import Vector

from workshop import paint, paint_specs

CATEGORY = "ammo.bolt"
TEXTURE_SIZE = 64
AO_STRENGTH = 0.2          # the paint recipes paint their own hollows (codex/paint.md)
NORMAL_FROM_ALBEDO = 2.5   # horn's normal strength k
DENSITY = 55.0             # px per metre the paint is drawn at: the game's bolts (ammo.bolt median 53)

LENGTH = 0.60
SHAFT = [  # (y, radius): the point, the dark sting to y = 0.14, the amber shaft, the base's bulb, hexagonal
    (0.0, 0.0), (0.035, 0.0045), (0.090, 0.0075), (0.140, 0.0095), (0.250, 0.0125), (0.370, 0.0155),
    (0.460, 0.0175), (0.515, 0.0215), (0.565, 0.0205), (0.590, 0.0150), (LENGTH, 0.0060)]
PIECES = [(0, 3), (3, 5), (5, 10)]    # the shaft in three pieces (ring ranges): the dark sting to y = 0.14, then two
                                      # amber lengths; short pieces pack bigger in the atlas, so the barbs get texels
BARBS = [  # (y of the root, angles round the shaft in degrees): two whorls, staggered
    (0.045, (90, 210, 330)), (0.095, (30, 150, 270))]
BARB_LENGTH, BARB_REACH = 0.026, 0.011     # back along the shaft, out from it


def build():
    horn, tip = _horn(), _tip()
    for first, last in PIECES:
        _ring_mesh(f"shaft{first}", SHAFT[first:last + 1], 6, tip if first == 0 else horn,
                   cap=last == len(SHAFT) - 1)
    for y, angles in BARBS:
        for degrees in angles:
            _barb(y, math.radians(degrees), tip)


def _tones(hexes):
    return tuple(paint_specs.hex_to_linear(h) for h in hexes)


def _horn():
    """The Queen's amber horn (her crown and stinger), banded along the needle: growth rings every 13 cm."""
    return paint.make("bone.horn", "needle_horn", tones=_tones(("#5a4020", "#9a7a48", "#d4b878")), region="trim",
                      axis="Y", pattern=("bands", 0.02, 0.15), blotch_m=0.1, density=DENSITY)


def _tip():
    """The sting's last 14 cm and its barbs: dark red-brown, the colour of old blood on the Queen's sting."""
    return paint.make("bone.horn", "needle_tip", tones=_tones(("#2a100a", "#4a1c10", "#6e3018")), region="secondary",
                      axis="Y", pattern=None, blotch_m=0.06, edges=0.0, density=DENSITY)


def _ring_mesh(name, profile, sides, material, cap):
    """A tube of `sides` points round the Y axis at each (y, radius), a point where the radius is 0, a flat cap on the
    last ring when `cap` (open ends meet the next piece's rings exactly)."""
    bm = bmesh.new()
    rings = []
    for y, r in profile:
        if r == 0.0:
            rings.append([bm.verts.new((0.0, y, 0.0))])
            continue
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / sides), y, r * math.sin(2 * math.pi * k / sides)))
                      for k in range(sides)])
    for a, b in zip(rings, rings[1:]):
        _band(bm, a, b)
    if cap:
        bm.faces.new(list(reversed(rings[-1])))
    return _object(name, bm, [material], smooth=True)


def _band(bm, a, b):
    """The faces between two rings (a ring of one point makes a fan)."""
    if len(a) == 1:
        return [bm.faces.new((a[0], b[(k + 1) % len(b)], b[k])) for k in range(len(b))]
    return [bm.faces.new((a[k], a[(k + 1) % len(a)], b[(k + 1) % len(b)], b[k])) for k in range(len(a))]


def _radius(y):
    """The shaft's radius at y (linear between the profile's rings)."""
    for (y0, r0), (y1, r1) in zip(SHAFT, SHAFT[1:]):
        if y0 <= y <= y1:
            return r0 + (r1 - r0) * (y - y0) / (y1 - y0)
    return SHAFT[-1][1]


def _barb(y, angle, material):
    """A small three-sided hook from inside the shaft at y, pointing back (+Y) and out at `angle` round the shaft."""
    out, side = Vector((math.cos(angle), 0.0, math.sin(angle))), Vector((-math.sin(angle), 0.0, math.cos(angle)))
    root, along = out * (_radius(y) * 0.5), Vector((0.0, 1.0, 0.0))
    bm = bmesh.new()
    base = [bm.verts.new(root + along * (y - 0.008)), bm.verts.new(root + along * (y + 0.006) + side * 0.004),
            bm.verts.new(root + along * (y + 0.006) - side * 0.004)]
    point = bm.verts.new(out * (_radius(y) + BARB_REACH) + along * (y + BARB_LENGTH))
    bm.faces.new(base)
    for a, b in zip(base, base[1:] + base[:1]):
        bm.faces.new((a, b, point))
    return _object(f"barb{y:.3f}_{angle:.2f}", bm, [material], smooth=False)


def _object(name, bm, materials, smooth):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    data = bpy.data.meshes.new(name)
    bm.to_mesh(data)
    bm.free()
    for mat in materials:
        data.materials.append(mat)
    data.polygons.foreach_set('use_smooth', [smooth] * len(data.polygons))
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    return obj

"""The satchel's smaller parts, in the player's frame like model.py: leather-scrap patches with crude sinew stitches,
the toggle closure (a wooden peg and the flap's leather loop), the knotted scrap shoulder straps and the bundle of
sticks tied to the wearer's left side (+X).
"""
import math
import random

from mathutils import Vector

import forms

# (material, a point inside the bag near the patch, the direction it is seen from, half sizes across and up, corners)
PATCHES = [("patch_red", Vector((-0.058, 0.28, 1.548)), Vector((0.0, 1.0, 0.45)), (0.046, 0.034), 5),
           ("patch_dark", Vector((0.072, 0.25, 1.333)), Vector((0.0, 1.0, -0.2)), (0.046, 0.036), 4),
           ("patch_grey", Vector((-0.10, 0.262, 1.43)), Vector((-1.0, 0.0, 0.0)), (0.036, 0.046), 5)]
PATCH_LIFT, PATCH_THICK = 0.0032, 0.0025
STITCH_GAP = 0.024

SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]
STRAP_IN, STRAP_OUT, KNOT, REACH = 0.080, 0.114, 3, 7

STICK_BASE, STICK_TOP = Vector((0.172, 0.250, 1.268)), Vector((0.176, 0.272, 1.628))


# --- patches and stitches --------------------------------------------------------------------------------------

def patches(kit, tree):
    for n, (material, inside, view, half, corners) in enumerate(PATCHES):
        centre, normal = forms.probe(tree, inside + view.normalized() * 0.5, -view)
        across = Vector((0.0, 0.0, 1.0)).cross(normal)
        across = across.normalized() if across.length > 1e-3 else Vector((1.0, 0.0, 0.0))
        frame = (centre, normal, across, normal.cross(across))
        outline = _outline(half, corners)
        _patch(f"patch_{n}", frame, outline, tree, kit[material])
        _stitches(f"patch_{n}", frame, outline, tree, kit["thread"])


def _outline(half, corners):
    """A crudely cut scrap: a few straight-ish sides at irregular angles, each side split once."""
    points = []
    start = random.uniform(0.0, 1.0)
    for k in range(corners):
        angle = 2 * math.pi * (k + start + random.uniform(-0.12, 0.12)) / corners
        scale = random.uniform(0.9, 1.15)
        points.append((math.cos(angle) * half[0] * scale, math.sin(angle) * half[1] * scale))
    outline = []
    for k in range(corners):
        (a0, b0), (a1, b1) = points[k], points[(k + 1) % corners]
        outline += [(a0, b0), ((a0 + a1) / 2 * 1.03, (b0 + b1) / 2 * 1.03)]
    return outline


def _onto(tree, frame, a, b, lift):
    """The surface point under (a, b) of the patch's tangent frame, lifted along its normal."""
    centre, normal, across, up = frame
    guess = centre + across * a + up * b
    hit, surface_normal = forms.probe(tree, guess + normal * 0.05, -normal)
    if hit is None:
        return guess + normal * lift, normal
    return hit + surface_normal * lift, surface_normal


def _patch(name, frame, outline, tree, material):
    centre = _onto(tree, frame, 0.0, 0.0, PATCH_LIFT)[0]
    inner = [_onto(tree, frame, a * 0.55, b * 0.55, PATCH_LIFT)[0] for a, b in outline]
    outer = [_onto(tree, frame, a, b, PATCH_LIFT)[0] for a, b in outline]
    count = len(outline)
    verts = [centre] + inner + outer
    faces = [(0, 1 + k, 1 + (k + 1) % count) for k in range(count)]
    faces += [(1 + k, 1 + count + k, 1 + count + (k + 1) % count, 1 + (k + 1) % count) for k in range(count)]
    patch = forms.mesh_object(name, verts, faces, material)
    forms.point_values(patch, "wear", [0.0] + [0.2] * count + [1.0] * count)
    forms.thicken(patch, PATCH_THICK)


def _stitches(name, frame, outline, tree, material):
    """Crude sinew stitches across the patch's edge, unevenly spaced and a little askew."""
    count = len(outline)
    ring = [Vector(p) for p in outline] + [Vector(outline[0])]
    for k in range(count):
        side = ring[k + 1] - ring[k]
        for step in range(max(1, round(side.length / STITCH_GAP))):
            at = ring[k] + side * ((step + 0.5 + random.uniform(-0.2, 0.2)) / max(1, round(side.length / STITCH_GAP)))
            out = Vector((side.y, -side.x)).normalized()
            out = out if out.dot(at) > 0 else -out
            out = _turn(out, random.uniform(-0.3, 0.3))
            _stitch(f"{name}_stitch_{k}_{step}", frame, at, out, tree, material)


def _turn(v, angle):
    c, s = math.cos(angle), math.sin(angle)
    return Vector((v.x * c - v.y * s, v.x * s + v.y * c))


def _stitch(name, frame, at, out, tree, material):
    length = random.uniform(0.0045, 0.006)
    inside = _onto(tree, frame, *(at - out * length), PATCH_LIFT + 0.0004)
    outside = _onto(tree, frame, *(at + out * length), 0.0006)
    along = outside[0] - inside[0]
    normal = (inside[1] + outside[1]).normalized()
    across = normal.cross(along).normalized()
    normal = along.cross(across).normalized()
    forms.oriented_box(name, (inside[0] + outside[0]) / 2, (along.normalized(), across, normal),
                       (along.length / 2 + 0.0012, 0.0014, 0.0011), material)


# --- the toggle closure ----------------------------------------------------------------------------------------

def toggle(kit, bag_tree, tree, flap_tip):
    """A spindle-shaped wooden peg on the bag's back under the flap's tip, and the flap's leather loop over it."""
    z = flap_tip - 0.032
    point, normal = forms.probe(bag_tree, Vector((0.0, 0.8, z)), Vector((0.0, -1.0, 0.0)))
    centre = point + normal * 0.0105
    xs = (-0.027, -0.021, -0.008, 0.0, 0.008, 0.021, 0.027)
    radii = (0.0042, 0.0062, 0.0074, 0.0078, 0.0074, 0.0062, 0.0042)
    forms.tube("toggle", [centre + Vector((x, 0.0, 0.0)) for x in xs], radii, 8, kit["wood"])
    _loop(kit["strap_dark"], tree, flap_tip + 0.028, z - 0.013)


def _loop(material, tree, top, bottom, width=0.0055, points=16):
    """A thin leather loop sewn to the flap's tip, lying on the flap and the bag, round under the peg."""
    mid, half_h, half_w = (top + bottom) / 2, (top - bottom) / 2, 0.0085
    left, right, normals = [], [], []
    for k in range(points):
        angle = 2 * math.pi * k / points
        for rail, grow in ((left, 0.0), (right, width)):
            x = math.cos(angle) * (half_w + grow)
            z = mid + math.sin(angle) * (half_h + grow)
            hit, normal = forms.probe(tree, Vector((x, 0.8, z)), Vector((0.0, -1.0, 0.0)))
            rail.append(hit + normal * 0.0008)
        normals.append(normal)
    forms.strap("toggle_loop", left, right, normals, 0.0026, material, closed=True)


# --- shoulder straps -------------------------------------------------------------------------------------------

def _rail(x, k):
    """A point on the shoulder's skin at x, step k (every 15 degrees over the shoulder from behind), lifted clear."""
    f = (x - 0.08) / 0.05
    (yi, zi), (yo, zo) = SHOULDER_IN[k], SHOULDER_OUT[k]
    return Vector((x, yi + (yo - yi) * f, zi + (zo - zi) * f)) + _out(15 + 15 * k) * 0.005


def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))


def shoulder_straps(kit):
    """Two short straps over the shoulders, each two scraps of leather knotted together behind the shoulder."""
    for side in (-1, 1):
        dark = _strap_piece(f"strap_{side}_back", side, STRAP_IN, STRAP_OUT, range(0, KNOT + 1), 0.0065)
        tan = _strap_piece(f"strap_{side}_front", side, STRAP_IN + 0.004, STRAP_OUT - 0.003,
                           range(KNOT, REACH + 1), 0.0060)
        forms.strap(*dark, kit["strap_dark"])
        forms.strap(*tan, kit["strap_tan"])
        _knot(f"knot_{side}", side, kit["strap_dark"])


def _strap_piece(name, side, x_in, x_out, steps, thickness):
    left = [_rail(x_in, k) for k in steps]
    right = [_rail(x_out, k) for k in steps]
    if steps[-1] == REACH:
        left.append(left[-1].lerp(_rail(x_in, REACH + 1), 0.45))
        right.append(right[-1].lerp(_rail(x_out, REACH + 1), 0.45))
    left, right = [Vector((side * p.x, p.y, p.z)) for p in left], [Vector((side * p.x, p.y, p.z)) for p in right]
    normals = []
    for i in range(len(left)):
        along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
        normal = (right[i] - left[i]).cross(along).normalized()
        normals.append(normal if normal.dot(_out(15 + 15 * min(steps[0] + i, REACH))) > 0 else -normal)
    return name, left, right, normals, thickness


def _knot(name, side, material):
    """The knot joining the two scraps, with its two cut tails sticking out."""
    mid = (_rail(STRAP_IN, KNOT) + _rail(STRAP_OUT, KNOT)) / 2
    mid.x *= side
    normal = _out(15 + 15 * KNOT)
    along = (_rail(0.1, KNOT + 1) - _rail(0.1, KNOT - 1)).normalized()
    across = along.cross(normal).normalized()
    forms.blob(name, mid + normal * 0.0085, (across, along, normal), (0.022, 0.014, 0.010), material)
    for tail, turn in ((0, -0.55), (1, 0.75)):
        direction = (-along * math.cos(turn) + across * math.sin(turn)).normalized()
        width = direction.cross(normal).normalized()
        forms.oriented_box(f"{name}_tail_{tail}", mid + normal * 0.009 + direction * 0.024,
                           (width, direction, normal), (0.0045, 0.014, 0.0016), material)


# --- the bundle of sticks --------------------------------------------------------------------------------------

STICKS = [(-0.011, -0.006, 0.0095, 0.008, 0.004), (0.004, -0.012, 0.0085, -0.030, -0.006),
          (0.012, 0.004, 0.0080, 0.016, 0.012), (-0.006, 0.010, 0.0090, -0.014, -0.016),
          (0.002, 0.000, 0.0070, 0.004, 0.002)]      # (offset x, offset y, radius, top shift, bottom shift)
TWIG = (2, 0.80, Vector((0.62, 0.18, 0.76)), 0.052, 0.0040)   # (stick, where along it, direction, length, radius)


def sticks(kit):
    """Five barky sticks of uneven length, a little bent, tied to the bag's side with two thongs."""
    axis = STICK_TOP - STICK_BASE
    for n, (dx, dy, radius, top, bottom) in enumerate(STICKS):
        offset = Vector((dx, dy, 0.0))
        low = STICK_BASE + offset + axis.normalized() * bottom
        high = STICK_TOP + offset + axis.normalized() * top
        bend = Vector((random.uniform(-0.004, 0.004), random.uniform(-0.004, 0.004), 0.0))
        centres = [low, low.lerp(high, 0.35) + bend, low.lerp(high, 0.7) - bend * 0.5, high]
        radii = [radius, radius * 0.97, radius * 0.92, radius * 0.86]
        forms.tube(f"stick_{n}", centres, radii, 6, kit["bark"], kit["cut"])
        if n == TWIG[0]:
            _twig(low.lerp(high, TWIG[1]), kit)
    for n, t in enumerate((0.22, 0.74)):
        _tie(f"tie_{n}", STICK_BASE.lerp(STICK_TOP, t), kit["strap_dark"])


def _twig(start, kit):
    """A snapped-off side twig, so the bundle reads as gathered sticks rather than poles."""
    _, _, direction, length, radius = TWIG
    end = start + direction.normalized() * length
    forms.tube("twig", [start, start.lerp(end, 0.5), end], [radius, radius * 0.85, radius * 0.7], 5, kit["bark"],
               kit["cut"])


def _tie(name, centre, material, points=12, width=0.0085):
    """A thong round the bundle, flattened against the bag and passing into its side."""
    left, right, normals = [], [], []
    for k in range(points):
        angle = 2 * math.pi * k / points
        radial = Vector((math.cos(angle), math.sin(angle), 0.0))
        ring = centre + Vector((radial.x * 0.027 - 0.006, radial.y * 0.0215, 0.0))
        left.append(ring - Vector((0.0, 0.0, width / 2)))
        right.append(ring + Vector((0.0, 0.0, width / 2)))
        normals.append(radial)
    forms.strap(name, left, right, normals, 0.0025, material, closed=True)

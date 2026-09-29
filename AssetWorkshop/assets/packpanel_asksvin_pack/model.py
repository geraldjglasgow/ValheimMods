"""PackPanel's Asksvin Pack, the Ashlands backpack, worn on the player's upper back. A stiff bag of dark red-brown
scaled asksvin hide that widens towards the shoulders, a lid of larger scales folded over the top whose lower edge
ends in three pointed lobes, a ridge of short curved spines along the lid's top edge, glowing flametal corner fittings
on the bottom corners, two straps holding the side lobes down with flametal buckles, a charred bone toggle closing
the middle lobe, and two short shoulder straps arching forward over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. This is the same convention as the
Trollhide Backpack; see fit.py for the check on the game's body.

The axes are the player's, not the bone's: in Player.prefab, Spine2 at rest leans back about 5.66 degrees and carries
a world scale of 95, so parented straight under the bone the pack needs a local rotation of about +5.66 degrees about
X and a local scale of 1/95 to sit as modelled.

The bag's front follows the body's back surface (measured on the player's body in its rest pose, in BACK below) and
presses 6 mm into it, so there is no gap and nothing shows through the chest or the sides of the torso.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import forms  # noqa: E402
import looks  # noqa: E402

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.55

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
BOTTOM, TOP = 1.13, 1.565               # the bag's height range (the bottom dips a little lower in the middle)
PRESS = 0.006                           # how far the bag's front sinks into the back
HALF_LOW, HALF_HIGH = 0.152, 0.228      # half widths at the bottom and the top: the pack widens upwards
BACK_LOW, BACK_HIGH = 0.305, 0.345      # y of the back face at the bottom and the top, on its sides
STRAP_X, STRAP_W, STRAP_T = 0.100, 0.026, 0.005
CENTRE = Vector((0.0, 0.25, 1.30))      # straps are laid by rays aimed at this line (along X) from outside

# The player's back at x = 0 in the rest pose: (height, y of the skin).
BACK = [(1.00, 0.166), (1.04, 0.157), (1.08, 0.149), (1.12, 0.144), (1.16, 0.141), (1.20, 0.143), (1.24, 0.145),
        (1.28, 0.157), (1.32, 0.170), (1.36, 0.182), (1.40, 0.192), (1.44, 0.201), (1.48, 0.203), (1.52, 0.196),
        (1.56, 0.189), (1.60, 0.173), (1.64, 0.148)]
# The shoulder's skin (y, z) at x = 0.08 and x = 0.13, every 15 degrees from 15 (behind) to 150 (in front), measured
# around (0.04, 1.48) in the side view: the rails the shoulder straps follow.
SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]
# The lid's course in the side view, outside the bag (it is laid on the bag's nearest surface), then how far along it
# the lid reaches between the lobes, and each lobe: (column u, how much further its tip reaches).
FLAP_PATH = [(0.215, 1.665), (0.31, 1.665), (0.44, 1.565), (0.46, 1.45), (0.46, 1.00)]
FLAP_EDGE, LOBE_HALF = 0.44, 0.314
LOBES = ((-0.628, 0.075), (0.0, 0.125), (0.628, 0.075))
COLUMNS, ROWS = 15, 10
# The spines along the lid's top edge: (x, length).
SPINES = [(-0.165, 0.046), (-0.110, 0.060), (-0.055, 0.072), (0.0, 0.082), (0.055, 0.072), (0.110, 0.060),
          (0.165, 0.046)]
TOGGLE_Z = 1.212


def build():
    kit = {"body": looks.scales(PIVOT, "pack_body", looks.BODY, 0.036, 0.5),
           "lid": looks.scales(PIVOT, "pack_lid", looks.LID, 0.060, 0.85), "strap": looks.strap_leather(PIVOT),
           "flametal": looks.flametal(PIVOT), "bone": looks.charred_bone(PIVOT)}
    kit["horn"] = looks.horn(PIVOT, looks.LID[0])
    bag = _bag(kit["body"])
    bag_tree = forms.surface([bag])
    flap = _flap(kit["lid"], bag_tree)
    _corner_caps(kit["flametal"], bag_tree)
    _spines(kit["horn"], forms.surface([flap]))
    toggle = _toggle(kit["bone"], bag_tree)
    _loop(kit, forms.surface([bag, flap, toggle]))
    _side_straps(kit, forms.surface([bag, flap]))
    _shoulder_straps(kit["strap"])
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


# --- the bag ---------------------------------------------------------------------------------------------------

def back_line(z):
    """The player's back (y of the skin at x = 0) at height z."""
    if z <= BACK[0][0]:
        return BACK[0][1]
    for (z0, y0), (z1, y1) in zip(BACK, BACK[1:]):
        if z <= z1:
            return y0 + (y1 - y0) * (z - z0) / (z1 - z0)
    return BACK[-1][1]


def front_y(x, z):
    """The bag's front: on the back's centre line, curling forward a little towards its sides."""
    wrap = 0.05 + (0.035 - 0.05) * min(1.0, max(0.0, (z - 1.26) / 0.10))
    return back_line(z) - wrap * (x / 0.19) ** 2 - PRESS


def half_width(v):
    """Half the bag's width at relative height v (0 bottom, 1 top)."""
    return HALF_LOW + (HALF_HIGH - HALF_LOW) * min(1.0, max(0.0, v))


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: a rounded wedge, wider at the top, the bottom dipping
    to a blunt point in the middle, the top and the back gently domed."""
    a, d, h = forms.rounded(param, 5.0)
    v = (h + 1) / 2
    x = a * half_width(v)
    z = BOTTOM + v * (TOP - BOTTOM) - 0.03 * (1 - v) ** 3 * (1 - a * a) + 0.008 * v ** 3 * (1 - a * a)
    front = front_y(x, z)
    back = BACK_LOW + (BACK_HIGH - BACK_LOW) * v + 0.016 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((10, 5, 10))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 8.0) * 0.003       # stiff hide, barely uneven
    forms.point_values(bag, "wear", [forms.edge_wear(p) for p in params])
    return bag


# --- the lid ---------------------------------------------------------------------------------------------------

def _path(s):
    """(point, outward normal) in the side view at arc length s along FLAP_PATH."""
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if s <= length or (y1, z1) == FLAP_PATH[-1]:
            t = s / length
            dy, dz = (y1 - y0) / length, (z1 - z0) / length
            return Vector((y0 + (y1 - y0) * t, z0 + (z1 - z0) * t)), Vector((-dz, dy))
        s -= length


def flap_length(u):
    """How far along FLAP_PATH column u reaches: FLAP_EDGE, plus a straight-sided point at each lobe."""
    return FLAP_EDGE + max(peak * max(0.0, 1.0 - abs(u - c) / LOBE_HALF) for c, peak in LOBES)


def _flap_target(u, s):
    """Where column u (-1.1 to 1.1) and arc length s aim: across the top and back, the outer columns pushed down
    over the bag's sides."""
    point, out = _path(s)
    half = half_width((point.y - BOTTOM) / (TOP - BOTTOM))
    if abs(u) <= 1.0:
        return Vector((u * half, point.x, point.y))
    inward = (abs(u) - 1.0) * 0.35
    return Vector((math.copysign(half + 0.08, u), point.x - out.x * inward, point.y - out.y * inward))


def _flap(material, bag_tree):
    """The lid: a sheet laid on the bag, one column per step of u, thickened towards the bag; wear on its edges."""
    verts, wear = [], []
    for i in range(COLUMNS):
        u = -1.1 + 2.2 * i / (COLUMNS - 1)
        end = flap_length(u)
        for j in range(ROWS):
            hit, normal, _, _ = bag_tree.find_nearest(_flap_target(u, end * j / (ROWS - 1)))
            verts.append(hit + normal * (0.011 + noise.noise(hit * 12.0) * 0.002))
            wear.append(1.0 if i in (0, COLUMNS - 1) or j in (0, ROWS - 1) else 0.0)
    faces = [(i * ROWS + j, (i + 1) * ROWS + j, (i + 1) * ROWS + j + 1, i * ROWS + j + 1)
             for i in range(COLUMNS - 1) for j in range(ROWS - 1)]
    flap = forms.mesh_object("flap", verts, faces, material)
    forms.point_values(flap, "wear", wear)
    _thicken(flap, 0.008)
    return flap


def _thicken(obj, thickness):
    """Gives the sheet its thickness on the bag's side of it (the pipeline applies the modifier)."""
    if _faces_inward(obj):
        obj.data.flip_normals()
    mod = obj.modifiers.new("thickness", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = -1.0
    mod.use_even_offset = True


def _faces_inward(obj):
    """True when the sheet's normals point into the bag (towards its middle) rather than away from it."""
    centre = Vector((0.0, 0.26, 1.36))
    poly = obj.data.polygons[len(obj.data.polygons) // 2]
    return poly.normal.dot(poly.center - centre) < 0


# --- flametal corners, spines, the toggle ----------------------------------------------------------------------

def _in_corner(param, side):
    """True for the part of the cube's surface round its bottom back corner on `side`."""
    return param[0] * side >= 0.66 and param[1] >= 0.2 and param[2] <= -0.58


def _corner_caps(material, bag_tree):
    """A three-armed flametal cap over each bottom back corner, laid on the bag."""
    params, faces = forms.box_grid((16, 8, 16))
    for side in (-1, 1):
        keep = [f for f in faces if all(_in_corner(params[i], side) for i in f)]
        used = sorted({i for f in keep for i in f})
        index = {i: k for k, i in enumerate(used)}
        verts = []
        for i in used:
            hit, normal, _, _ = bag_tree.find_nearest(bag_point(params[i]))
            verts.append(hit + normal * 0.0065)
        cap = forms.mesh_object(f"corner_{side}", verts, [[index[i] for i in f] for f in keep], material)
        uses = [sum(i in f for f in keep) for i in used]
        forms.point_values(cap, "wear", [1.0 if n < 3 else 0.0 for n in uses])     # the rim
        _thicken(cap, 0.005)


def _spines(material, lid_tree):
    """Short spines standing up and back from the lid's top edge, curving backwards, longest in the middle."""
    for i, (x, length) in enumerate(SPINES):
        hit, normal, _, _ = lid_tree.find_nearest(Vector((x, 0.40, 1.66)))
        up = (normal * 0.4 + Vector((0.0, 0.35, 1.0))).normalized()
        across = Vector((1.0, 0.0, 0.0))
        across = (across - up * across.dot(up)).normalized()
        base, radius = hit - up * 0.008, length * 0.29
        rings, tips = [(base, 0.0, 0.0)], [0.0]
        for k in range(4):
            t = k / 4
            centre = base + up * length * t + Vector((0.0, 1.0, 0.0)) * length * 0.35 * t * t
            rings.append((centre, radius * (1 - t) ** 0.85, radius * 0.72 * (1 - t) ** 0.85))
            tips.append(t)
        rings.append((base + up * length + Vector((0.0, 1.0, 0.0)) * length * 0.35, 0.0, 0.0))
        tips.append(1.0)
        forms.lathe(f"spine_{i}", rings, (across, up.cross(across)), 6, material, ("tip", tips))


def _toggle(material, bag_tree):
    """A charred bone toggle across the bag's back under the middle lobe: a short bone with knobbed ends."""
    hit, normal = forms.probe(bag_tree, Vector((0.0, 1.0, TOGGLE_Z)), Vector((0.0, -1.0, 0.0)))
    axis = Vector((1.0, 0.0, 0.0))
    axis = (axis - normal * axis.dot(normal)).normalized()
    centre = hit + normal * 0.012
    profile = [(-1.0, 0.0), (-1.0, 0.008), (-0.86, 0.0130), (-0.62, 0.0092), (0.0, 0.0080), (0.62, 0.0092),
               (0.86, 0.0130), (1.0, 0.008), (1.0, 0.0)]
    rings = [(centre + axis * t * 0.048 - normal * 0.004 * t * t, r, r) for t, r in profile]
    return forms.lathe("toggle", rings, (normal, normal.cross(axis)), 7, material)


# --- straps and buckles ----------------------------------------------------------------------------------------

def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))


def _sweep(tree, x, width, top, done, step=5.0):
    """Rails of a strap `width` wide lying on the outermost surface at x: rays aimed at CENTRE from further and further
    round below it, keeping the hits no higher than `top` until done(point) says the strap has ended."""
    left, right, normals = [], [], []
    angle = 70.0
    while angle > -140.0:
        out = _out(angle)
        angle -= step
        hits = [forms.probe(tree, Vector((x + dx, CENTRE.y, CENTRE.z)) + out, -out) for dx in (-width / 2, width / 2)]
        if hits[0][0] is None or hits[1][0] is None:
            break
        if max(hits[0][0].z, hits[1][0].z) > top:
            continue
        normal = (hits[0][1] + hits[1][1]).normalized()
        left.append(hits[0][0] + normal * 0.0015)
        right.append(hits[1][0] + normal * 0.0015)
        normals.append(normal)
        if done(left[-1]):
            break
    return left, right, normals


def _side_straps(kit, tree):
    """From each side lobe's tip down the back and under the bottom; a flametal plate on the lobe, a buckle low down."""
    for side in (-1, 1):
        left, right, normals = _sweep(tree, side * STRAP_X, STRAP_W, 1.338, lambda p: p.y < 0.215)
        forms.strap(f"side_strap_{side}", left, right, normals, STRAP_T, kit["strap"])
        _plate(f"lobe_plate_{side}", left[0], right[0], left[1] - left[0], normals[0], 0.016, kit["flametal"])
        at = min(range(1, len(left) - 1), key=lambda i: abs(left[i].z - 1.245))
        _buckle(f"buckle_{side}", left[at], right[at], left[at + 1] - left[at - 1], normals[at], kit["flametal"])


def _loop(kit, tree):
    """The middle lobe's strap loop, from its tip down over the bone toggle; a flametal plate where it starts."""
    left, right, normals = _sweep(tree, 0.0, 0.020, 1.288, lambda p: p.z < TOGGLE_Z - 0.004, step=3.0)
    forms.strap("loop", left, right, normals, 0.004, kit["strap"])
    _plate("loop_plate", left[0], right[0], left[1] - left[0], normals[0], 0.014, kit["flametal"])


def _frame(left, right, along, normal):
    """(across, along, normal) unit axes of a strap at one ring, the normal on the strap's outer side."""
    across = (right - left).normalized()
    along = (along - across * along.dot(across)).normalized()
    turned = across.cross(along)
    return across, along, (turned if turned.dot(normal) > 0 else -turned)


def _plate(name, left, right, along, normal, size, material):
    """A small flametal diamond riveted on a strap's end."""
    across, along, normal = _frame(left, right, along, normal)
    centre = (left + right) / 2 + along * size * 0.6 + normal * (STRAP_T + 0.0015)
    axes = ((across + along).normalized(), (along - across).normalized(), normal)
    forms.oriented_box(name, centre, axes, (size * 0.5, size * 0.5, 0.0022), material)


def _buckle(name, left, right, along, normal, material):
    """A flametal buckle lying on a strap: a rectangular frame, a middle bar and a tongue."""
    across, along, normal = _frame(left, right, along, normal)
    centre = (left + right) / 2 + normal * (STRAP_T + 0.002)
    w, h, t, bar = 0.024, 0.019, 0.0024, 0.0032
    axes = (across, along, normal)
    for part, offset, half in (("top", along * (h - bar), (w, bar, t)), ("bottom", -along * (h - bar), (w, bar, t)),
                               ("left", -across * (w - bar), (bar, h, t)), ("right", across * (w - bar), (bar, h, t)),
                               ("bar", Vector(), (w, bar * 0.8, t)),
                               ("tongue", along * h * 0.45 + normal * 0.001, (bar * 0.55, h * 0.5, t * 0.8))):
        forms.oriented_box(f"{name}_{part}", centre + offset, axes, half, material)


def _shoulder_straps(material, reach=8):
    """Short straps from the bag's top over each shoulder, ending just past its top."""
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in range(reach):
            out = _out(15 + 15 * k)
            inner, outer = SHOULDER_IN[k], SHOULDER_OUT[k]
            left.append(Vector((side * 0.08, inner[0], inner[1])) + out * 0.005)
            right.append(Vector((side * 0.13, outer[0], outer[1])) + out * 0.005)
        tip = 0.45
        left.append(left[-1].lerp(Vector((side * 0.08, *SHOULDER_IN[reach])) + _out(15 + 15 * reach) * 0.005, tip))
        right.append(right[-1].lerp(Vector((side * 0.13, *SHOULDER_OUT[reach])) + _out(15 + 15 * reach) * 0.005,
                                    tip))
        for i in range(len(left)):
            along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
            normal = (right[i] - left[i]).cross(along).normalized()
            normals.append(normal if normal.dot(_out(15 + 15 * min(i, reach))) > 0 else -normal)
        forms.strap(f"shoulder_strap_{side}", left, right, normals, 0.007, material)

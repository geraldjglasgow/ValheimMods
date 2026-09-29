"""PackPanel's Trollhide Backpack: a Viking rucksack worn on the player's upper back. A soft bag of dark teal troll
leather with stitched seams and scuffed edges, a ragged blue-grey troll-hide flap folded over the top and hanging
down the back, a hide bedroll strapped under it, two long straps with bronze buckles holding flap and bedroll, a
small side pouch, and two short shoulder straps arching forward over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. See fit.py for the check on the
game's body.

The axes are the player's, not the bone's: in Player.prefab, Spine2 at rest leans back about 5.66 degrees (a local
rotation of -5.66 about Unity X relative to the player; Spine and Spine1 tilt, Spine2 itself does not) and carries a
world scale of 95 (Armature 100 x Visual 0.95). Parented straight under the bone, the pack needs a local rotation of
about +5.66 degrees about X and a local scale of 1/95 to sit as modelled.

The bag's front follows the body's back surface (measured on the player's body in its rest pose, in BACK below)
and presses 6 mm into it, so there is no gap and nothing shows through the chest or the sides of the torso.
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import forms  # noqa: E402
import looks  # noqa: E402

TEXTURE_SIZE = 256   # all eight packs ship in one bundle embedded in PackPanel.dll
NORMAL_MAP = True
AO_STRENGTH = 0.6

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
BOTTOM, TOP = 1.15, 1.62                # the bag's height range
PRESS = 0.006                           # how far the bag's front sinks into the back
SEAM_X, SEAM_Z = 0.148, 1.21            # the leather's side seams (|x|) and bottom seam (z)
ROLL_Y, ROLL_Z, ROLL_R = 0.235, 1.08, 0.068    # the bedroll's axis (along X) and radius
STRAP_X, STRAP_W, STRAP_T = 0.10, 0.030, 0.005

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
# The flap's course in the side view, outside the bag: it is laid on the bag's nearest surface. FLAP_LENGTH is how
# far along it the flap reaches, TORN how much longer or shorter each column is: a torn edge with a few long tails.
FLAP_PATH = [(0.19, 1.72), (0.30, 1.72), (0.45, 1.60), (0.48, 1.47), (0.48, 1.14)]
FLAP_LENGTH = 0.54
TORN = [-0.04, 0.02, 0.07, 0.01, -0.01, 0.05, 0.11, 0.03, 0.0, 0.06, 0.015, 0.085, -0.03]


def build():
    random.seed(7)
    kit = {"leather": looks.leather(PIVOT, SEAM_X, SEAM_Z), "plain": looks.leather(PIVOT),
           "hide": looks.hide(PIVOT, "pack_hide", looks.HIDE, (95.0, 95.0, 8.0)),
           "roll": looks.hide(PIVOT, "pack_roll", looks.ROLL, (8.0, 110.0, 110.0)),
           "roll_end": looks.roll_end(PIVOT, ROLL_Y, ROLL_Z), "strap": looks.strap_leather(PIVOT),
           "bronze": looks.bronze(PIVOT)}
    bag = _bag(kit["leather"])
    flap = _flap(kit["hide"], forms.surface([bag]))
    roll = _bedroll(kit["roll"], kit["roll_end"])
    _roll_loops(kit)
    _long_straps(kit, forms.surface([bag, flap, roll]))
    _shoulder_straps(kit["strap"])
    _pouch(kit)
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


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: rounded, wider and fuller low down, the bottom sagging."""
    a, d, h = forms.rounded(param, 5.0)
    v = (h + 1) / 2
    x = a * (0.20 - 0.025 * v * v)
    z = BOTTOM + v * (TOP - BOTTOM) - 0.018 * (1 - v) ** 3 * (1 - a * a)
    front = front_y(x, z)
    back = 0.325 + 0.05 * math.sin(math.pi * (1 - v) * 0.85) + 0.012 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((10, 5, 10))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 9.0) * 0.004       # soft, uneven leather
    forms.point_values(bag, "wear", [forms.edge_wear(p) for p in params])
    return bag


# --- the flap --------------------------------------------------------------------------------------------------

def _path(s):
    """(point, outward normal) in the side view at arc length s along FLAP_PATH."""
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if s <= length or (y1, z1) == FLAP_PATH[-1]:
            t = s / length
            dy, dz = (y1 - y0) / length, (z1 - z0) / length
            return Vector((y0 + (y1 - y0) * t, z0 + (z1 - z0) * t)), Vector((-dz, dy))
        s -= length


def _flap_target(u, s):
    """Where column u (-1.15 to 1.15) and arc length s aim: across the top and back, the outer columns pushed down
    over the bag's sides."""
    point, out = _path(s)
    if abs(u) <= 1.0:
        return Vector((u * 0.16, point.x, point.y))
    inward = (abs(u) - 1.0) * 0.35
    return Vector((math.copysign(0.26, u), point.x - out.x * inward, point.y - out.y * inward))


def _flap_vertex(bag_tree, u, s, t):
    """A point of the flap: on the bag, lifted clear of it, the hanging part in soft vertical folds."""
    hit, normal, _, _ = bag_tree.find_nearest(_flap_target(u, s))
    hang = max(0.0, t - 0.35) / 0.65
    fold = 0.010 * hang * (math.sin(u * 7.5 + 0.8) + 1.0) / 2.0
    return hit + normal * (0.007 + 0.008 * hang * hang + fold + noise.noise(hit * 14.0) * 0.003)


def _flap(material, bag_tree, rows=12):
    """The hide flap, one column per entry of TORN; the tips of the torn edge are nudged sideways at random."""
    columns, verts, wear = len(TORN), [], []
    spacing = 2.3 / (columns - 1)
    for i, tail in enumerate(TORN):
        end = FLAP_LENGTH + tail + random.uniform(-0.008, 0.008)
        for j in range(rows):
            t = j / (rows - 1)
            u = -1.15 + spacing * (i + (random.uniform(-0.35, 0.35) if j == rows - 1 else 0.0))
            verts.append(_flap_vertex(bag_tree, u, end * t, t))
            wear.append(max(0.6 if i in (0, columns - 1) or j == 0 else 0.0, max(0.0, t - 0.75) / 0.25))
    faces = [(i * rows + j, (i + 1) * rows + j, (i + 1) * rows + j + 1, i * rows + j + 1)
             for i in range(columns - 1) for j in range(rows - 1)]
    flap = forms.mesh_object("flap", verts, faces, material)
    forms.point_values(flap, "wear", wear)
    _thicken(flap, 0.005)
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
    centre = Vector((0.0, 0.26, 1.40))
    poly = obj.data.polygons[len(obj.data.polygons) // 2]
    return poly.normal.dot(poly.center - centre) < 0


# --- the bedroll -----------------------------------------------------------------------------------------------

ROLL_SECTIONS = [(-0.22, 0.88), (-0.205, 0.97), (-0.13, 1.0), (-0.1, 0.93), (-0.07, 1.0), (0.0, 1.02),
                 (0.07, 1.0), (0.1, 0.93), (0.13, 1.0), (0.205, 0.97), (0.22, 0.88)]
ROLL_TURN = math.radians(25)     # where the outer layer ends, measured from +Y (behind) towards +Z


def roll_radius(k, around, scale):
    """Radius of the rolled hide at step k of `around`: a spiral, so the outer layer ends in a small step."""
    return ROLL_R * scale * (0.955 + 0.075 * k / around)


def roll_point(x, k, around, scale):
    angle = ROLL_TURN + 2 * math.pi * k / around
    r = roll_radius(k, around, scale)
    return Vector((x, ROLL_Y + r * math.cos(angle), ROLL_Z + r * math.sin(angle)))


def _bedroll(material, end_material, around=12):
    verts, faces = [], []
    ring = around + 1
    for x, scale in ROLL_SECTIONS:
        verts += [roll_point(x, k, around, scale * random.uniform(0.98, 1.02)) for k in range(ring)]
    for s in range(len(ROLL_SECTIONS) - 1):
        faces += [(s * ring + k, s * ring + (k + 1) % ring, (s + 1) * ring + (k + 1) % ring, (s + 1) * ring + k)
                  for k in range(ring)]
    caps = []
    for s, sign in ((0, -1), (len(ROLL_SECTIONS) - 1, 1)):
        verts.append(Vector((ROLL_SECTIONS[s][0] - sign * 0.008, ROLL_Y, ROLL_Z)))    # ends dished in a little
        centre = len(verts) - 1
        caps += [len(faces) + k for k in range(around)]
        fan = [(centre, s * ring + k + 1, s * ring + k) for k in range(around)]
        faces += fan if sign < 0 else [f[::-1] for f in fan]
    roll = forms.mesh_object("bedroll", verts, faces, material)
    forms.add_material(roll, end_material, caps)
    return roll


def _roll_out(k, around):
    angle = ROLL_TURN + 2 * math.pi * k / around
    return Vector((0.0, math.cos(angle), math.sin(angle)))


def _roll_loops(kit, around=12):
    """A strap round the roll under each long strap (over the outer layer's step), a small buckle at the back."""
    steps = [k + 0.5 for k in range(around)] + [float(around)]
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in steps:
            out = _roll_out(k, around)
            left.append(roll_point(side * STRAP_X - STRAP_W / 2, k, around, 0.95) + out * 0.004)
            right.append(roll_point(side * STRAP_X + STRAP_W / 2, k, around, 0.95) + out * 0.004)
            normals.append(out)
        forms.strap(f"roll_strap_{side}", left, right, normals, STRAP_T, kit["strap"], closed=True)
        _buckle(f"roll_buckle_{side}", left[10], right[10], left[11] - left[9], normals[10], kit["bronze"],
                small=True)


# --- straps and buckles ----------------------------------------------------------------------------------------

def _long_straps(kit, tree):
    """From the top of the flap down the back to the bedroll, a buckle where the flap ends."""
    for side in (-1, 1):
        left, right, normals = [], [], []
        for step in range(28):
            angle = math.radians(104 - step * 6.5)
            out = Vector((0.0, math.cos(angle), math.sin(angle)))
            hits = [forms.probe(tree, Vector((side * STRAP_X + dx, 0.25, 1.42)) + out, -out)
                    for dx in (-STRAP_W / 2, STRAP_W / 2)]
            if hits[0][0] is None or hits[1][0] is None or min(hits[0][0].z, hits[1][0].z) < 1.16:
                break
            normal = (hits[0][1] + hits[1][1]).normalized()
            left.append(hits[0][0] + normal * 0.0015)
            right.append(hits[1][0] + normal * 0.0015)
            normals.append(normal)
        forms.strap(f"long_strap_{side}", left, right, normals, STRAP_T, kit["strap"])
        at = min(range(1, len(left) - 1), key=lambda i: abs(left[i].z - 1.36))
        _buckle(f"buckle_{side}", left[at], right[at], left[at + 1] - left[at - 1], normals[at], kit["bronze"])


def _buckle(name, left, right, along, normal, material, small=False):
    """A bronze buckle lying on a strap: a rectangular frame, a middle bar and a tongue."""
    scale = 0.8 if small else 1.0
    across = (right - left).normalized()
    along = (along - across * along.dot(across)).normalized()
    normal = across.cross(along) if across.cross(along).dot(normal) > 0 else -across.cross(along)
    centre = (left + right) / 2 + normal * (STRAP_T + 0.002)
    w, h, t, bar = 0.023 * scale, 0.017 * scale, 0.0022, 0.0028 * scale
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
            angle = math.radians(15 + 15 * k)
            out = Vector((0.0, math.cos(angle), math.sin(angle)))
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


def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))


# --- the side pouch --------------------------------------------------------------------------------------------

def _pouch(kit):
    """A small pouch on the wearer's left side of the bag, its dark flap closed with a bronze toggle."""
    centre, half = Vector((0.224, 0.29, 1.265)), Vector((0.030, 0.050, 0.055))
    params, faces = forms.box_grid((3, 3, 3))
    verts = [centre + Vector([c * s for c, s in zip(forms.rounded(p, 3.0), half)]) for p in params]
    pouch = forms.mesh_object("pouch", verts, faces, kit["plain"])
    forms.point_values(pouch, "wear", [forms.edge_wear(p) for p in params])
    course = [(0.205, 1.322), (0.235, 1.323), (0.250, 1.316), (0.256, 1.300), (0.257, 1.275), (0.257, 1.258)]
    left = [Vector((x, 0.247, z)) for x, z in course]
    right = [Vector((x, 0.333, z)) for x, z in course]
    normals = [Vector((0.0, 0.0, 1.0)), Vector((0.2, 0.0, 1.0)).normalized(), Vector((0.7, 0.0, 0.7)).normalized(),
               Vector((1.0, 0.0, 0.3)).normalized(), Vector((1.0, 0.0, 0.0)), Vector((1.0, 0.0, 0.0))]
    forms.strap("pouch_flap", left, right, normals, 0.004, kit["strap"])
    forms.oriented_box("pouch_toggle", Vector((0.2635, 0.29, 1.266)),
                       (Vector((0, 1, 0)), Vector((0, 0, 1)), Vector((1, 0, 0))), (0.012, 0.004, 0.0035), kit["bronze"])

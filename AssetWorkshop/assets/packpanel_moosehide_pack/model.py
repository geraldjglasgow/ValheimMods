"""PackPanel's Moosehide Pack: the Deep North backpack, the largest and last pack of the line. A tall, heavy bag of
warm dark-brown moose hide with a darker base panel, a thick hide lid folded over the top and down the back with a
gold boss, a cream-white winter fur ruff all round the top edge, two antler toggles closing the lid through
orange sinew loops, a broad waist band with a gold buckle, gold caps on the two bottom back corners, a small
antler-tine charm hanging from the wearer's left side, and two short strap-leather shoulder straps with gold buckles
over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. This is the same contract as the
Trollhide Backpack: parented straight under the bone, the pack needs a local rotation of about +5.66 degrees about
X and a local scale of 1/95 to sit as modelled. See fit.py for the check on the game's body.

The bag's front follows the body's back surface, sampled from the player's body in its rest pose (SKIN below), and
presses a few millimetres into it, so there is no gap and nothing shows through the chest or the sides. Above the
shoulder blades the front stands back from the neck, and the pack rises only a little above the shoulders, well
behind the head.
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

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.6

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
BOTTOM, TOP = 1.09, 1.655               # the bag's height range
BASE_Z = 1.15                           # the darker base panel ends here
HALF_W = 0.205                          # the bag's half width
PRESS = 0.003                           # how far the bag's front sinks into the back
FLAP_LOW, FLAP_CURVE = 1.395, 0.075     # the lid's lowest point (middle) and how much higher its corners end
TOGGLE_X, TOGGLE_DROP = 0.105, 0.046    # the antler toggles: |x| of their middles, depth below the lid's edge

# The player's back in the rest pose: y of the skin (the most rearward within 12 mm) at |x| = SKIN_X, per height.
# None: the ray from behind misses the body there.
SKIN_X = [0.0, 0.03, 0.06, 0.09, 0.12, 0.15, 0.18, 0.21, 0.24, 0.27]
SKIN = [
    (1.02, [0.161, 0.161, 0.160, 0.158, 0.150, 0.109, 0.063, None, None, None]),
    (1.06, [0.153, 0.152, 0.150, 0.145, 0.132, 0.102, 0.053, None, None, None]),
    (1.10, [0.145, 0.142, 0.137, 0.130, 0.114, 0.095, 0.034, None, None, None]),
    (1.14, [0.142, 0.139, 0.133, 0.125, 0.109, 0.094, 0.034, None, None, None]),
    (1.18, [0.142, 0.138, 0.132, 0.126, 0.109, 0.094, 0.041, None, None, None]),
    (1.22, [0.144, 0.140, 0.137, 0.133, 0.114, 0.097, 0.043, None, None, None]),
    (1.26, [0.151, 0.150, 0.148, 0.145, 0.129, 0.109, 0.057, None, None, None]),
    (1.30, [0.164, 0.163, 0.163, 0.160, 0.148, 0.134, 0.098, None, None, 0.063]),
    (1.34, [0.176, 0.176, 0.176, 0.175, 0.164, 0.152, 0.140, 0.072, None, 0.115]),
    (1.38, [0.187, 0.187, 0.187, 0.187, 0.179, 0.167, 0.155, 0.111, 0.101, 0.164]),
    (1.42, [0.197, 0.197, 0.198, 0.197, 0.190, 0.182, 0.169, 0.148, 0.146, 0.159]),
    (1.46, [0.206, 0.207, 0.208, 0.208, 0.201, 0.194, 0.182, 0.160, 0.156, 0.153]),
    (1.50, [0.200, 0.200, 0.200, 0.201, 0.195, 0.188, 0.178, 0.163, 0.157, 0.152]),
    (1.54, [0.193, 0.193, 0.193, 0.193, 0.188, 0.179, 0.169, 0.156, 0.149, 0.147]),
    (1.58, [0.186, 0.186, 0.186, 0.186, 0.180, 0.171, 0.161, 0.147, 0.139, 0.119]),
    (1.62, [0.161, 0.162, 0.161, 0.157, 0.149, 0.127, 0.099, 0.078, None, None]),
    (1.66, [0.129, 0.127, 0.122, 0.108, 0.080, None, None, None, None, None]),
    (1.70, [0.086, 0.077, 0.061, None, None, None, None, None, None, None]),
]
# The shoulder's skin (y, z) at x = 0.08 and x = 0.13, every 15 degrees from 15 (behind) to 150 (in front), measured
# around (0.04, 1.48) in the side view: the rails the shoulder straps follow (the Trollhide Backpack's measurement).
SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]
# The lid's course in the side view, outside the bag: it is laid on the bag's nearest surface.
FLAP_PATH = [(0.170, TOP + 0.06), (0.30, TOP + 0.08), (0.45, TOP - 0.01), (0.49, 1.52), (0.49, 1.25)]


def build():
    random.seed(11)
    kit = {"hide": looks.moose_hide(PIVOT, BASE_Z), "flap": looks.flap_hide(PIVOT), "fur": looks.fur(PIVOT),
           "gold": looks.gold(PIVOT), "antler": looks.antler(PIVOT), "sinew": looks.sinew(PIVOT),
           "strap": looks.strap_leather(PIVOT)}
    bag = _bag(kit["hide"])
    bag_tree = forms.surface([bag])
    flap = _flap(kit["flap"], bag_tree)
    shell = forms.surface([bag, flap])
    _ruff(kit["fur"], shell)
    _boss(kit["gold"], forms.surface([flap]))
    for side in (-1, 1):
        _toggle(kit, shell, side)
        _corner_cap(kit["gold"], bag_tree, side)
    _charm(kit, shell)
    _band(kit, forms.surface([bag]))
    _shoulder_straps(kit)
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


# --- the bag ---------------------------------------------------------------------------------------------------

def _filled(row):
    """A SKIN row with its misses filled by falling away forwards from the last hit (no constraint there)."""
    out = []
    for y in row:
        out.append(y if y is not None else (out[-1] - 0.03 if out else 0.0))
    return out


SKIN_FILLED = [(z, _filled(row)) for z, row in SKIN]


def skin(x, z):
    """The player's back (y of the skin) at (x, z), bilinear in SKIN."""
    x = min(abs(x), SKIN_X[-1])
    i = min(int(x / 0.03), len(SKIN_X) - 2)
    fx = (x - SKIN_X[i]) / 0.03
    z = min(max(z, SKIN[0][0]), SKIN[-1][0])
    j = min(int((z - SKIN[0][0]) / 0.04), len(SKIN) - 2)
    fz = (z - SKIN[j][0]) / 0.04
    low, high = SKIN_FILLED[j][1], SKIN_FILLED[j + 1][1]
    a = low[i] + (low[i + 1] - low[i]) * fx
    b = high[i] + (high[i + 1] - high[i]) * fx
    return a + (b - a) * fz


def centre_y(z):
    """The bag's front on the centre line: on the skin, but standing back from the neck above the shoulders."""
    hold = 0.186 - 0.12 * max(0.0, z - 1.58) if z > 1.55 else 0.0
    return max(skin(0.0, z), hold)


def smooth_max(a, b, k):
    h = min(1.0, max(0.0, 0.5 + 0.5 * (a - b) / k))
    return b + (a - b) * h + k * h * (1 - h)


def front_y(x, z):
    """The bag's front: curling forward towards its sides (more round the waist), never behind the skin's reach."""
    wrap = 0.05 + (0.03 - 0.05) * min(1.0, max(0.0, (z - 1.28) / 0.10))
    natural = centre_y(z) - wrap * (x / 0.19) ** 2
    return smooth_max(natural, skin(x, z), 0.008) - PRESS


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: a tall rounded block, fuller in the middle of its back,
    the top domed (a full load under the lid, standing above the ruff), the bottom sagging a little."""
    a, d, h = forms.rounded(param, 5.5)
    v = (h + 1) / 2
    half = HALF_W + 0.006 * math.sin(math.pi * v) - 0.020 * v ** 2
    x = a * half
    z = BOTTOM + v * (TOP - BOTTOM) - 0.014 * (1 - v) ** 3 * (1 - a * a) + 0.034 * v ** 8 * (1 - a * a) * (1 - d * d)
    front = front_y(x, z)
    back = 0.383 + 0.030 * math.sin(math.pi * v) + 0.012 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((10, 5, 12))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 8.0) * 0.004       # thick, slightly uneven hide
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


def _arc_at(z):
    """Arc length along FLAP_PATH where it comes down the back to height z."""
    s = 0.0
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if z1 <= z <= z0:
            return s + length * (z0 - z) / (z0 - z1)
        s += length
    return s


def flap_edge_z(x):
    """Height of the lid's rounded lower edge at x."""
    u = min(1.0, abs(x) / 0.17)
    return FLAP_LOW + FLAP_CURVE * u * u


def _flap_target(u, s):
    """Where column u (-1.1 to 1.1) and arc length s aim: across the top and back, the outer columns pushed down
    over the bag's shoulders."""
    point, out = _path(s)
    if abs(u) <= 1.0:
        return Vector((u * 0.17, point.x, point.y))
    inward = (abs(u) - 1.0) * 0.5
    return Vector((math.copysign(0.25, u), point.x - out.x * inward, point.y - out.y * inward))


def _flap(material, bag_tree, columns=11, rows=10):
    """The lid: one sheet of thick hide over the top and down the back, its lower edge rounded."""
    verts, wear = [], []
    for i in range(columns):
        u = -1.1 + 2.2 * i / (columns - 1)
        end = _arc_at(flap_edge_z(min(abs(u), 1.0) * 0.17)) - (0.03 if abs(u) > 1.0 else 0.0)
        for j in range(rows):
            t = j / (rows - 1)
            hit, normal, _, _ = bag_tree.find_nearest(_flap_target(u, end * t))
            verts.append(hit + normal * (0.006 + noise.noise(hit * 12.0) * 0.002))
            wear.append(0.75 if i in (0, columns - 1) or j == rows - 1 else 0.0)
    faces = [(i * rows + j, (i + 1) * rows + j, (i + 1) * rows + j + 1, i * rows + j + 1)
             for i in range(columns - 1) for j in range(rows - 1)]
    flap = forms.mesh_object("flap", verts, faces, material)
    forms.point_values(flap, "wear", wear)
    _thicken(flap, 0.007)
    return flap


def _thicken(obj, thickness):
    """Gives the sheet its thickness on the bag's side of it (the pipeline applies the modifier)."""
    centre = Vector((0.0, 0.29, 1.40))
    poly = obj.data.polygons[len(obj.data.polygons) // 2]
    if poly.normal.dot(poly.center - centre) < 0:
        obj.data.flip_normals()
    mod = obj.modifiers.new("thickness", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = -1.0
    mod.use_even_offset = True


# --- the fur ruff ----------------------------------------------------------------------------------------------

RUFF_Z, RUFF_R = TOP - 0.020, 0.041


def _ruff_front(angle):
    """0 round the back and sides, rising to 1 in the middle of the front, where the ruff meets the wearer's back."""
    return max(0.0, (abs(angle) - 100.0) / 80.0)


def _ruff_radius(angle):
    """Thick round the back and sides, thin across the front."""
    return RUFF_R * (1.0 - 0.74 * _ruff_front(angle) ** 1.5)


def _ruff(material, shell, steps=36, sides=8):
    """A fat roll of winter fur all round the top edge, lumpy and tufted, its seam at the front."""
    centre, points, radii = Vector((0.0, 0.30, RUFF_Z)), [], []
    for n in range(steps):
        angle = -180.0 + 360.0 * n / steps
        out = Vector((math.sin(math.radians(angle)), math.cos(math.radians(angle)), 0.0))
        hit, normal = forms.probe(shell, centre + out * 0.6, -out)
        flat = Vector((normal.x, normal.y, 0.0)).normalized()
        r = _ruff_radius(angle)
        tuck = 0.45 - 0.85 * _ruff_front(angle)            # across the front the roll sinks into the bag's edge
        points.append(hit + flat * r * tuck + Vector((0.0, 0.0, 0.004 + 0.004 * math.cos(math.radians(angle)))))
        radii.append(r * random.uniform(0.92, 1.08))
    ruff = forms.tube("ruff", points, radii, material, sides=sides, closed=True, shape=_tuft)
    forms.point_values(ruff, "root", [_root(v.co, points, radii, n // sides) for n, v in enumerate(ruff.data.vertices)])
    return ruff


def _tuft(i, k, direction):
    """Fur clumps: every other vertex of a ring pushed out, varied by noise, drooping a little."""
    spike = (0.14 if (k + i) % 2 == 0 else -0.04) * (1.0 - 0.7 * max(0.0, direction.z))
    return max(0.8, 1.0 + spike + 0.22 * noise.noise(direction * 3.0 + Vector((i * 0.9, k * 0.3, 0.0))))


def _root(co, points, radii, i):
    """1 on the fur's underside and against the bag (the roots), 0 on its outer tips."""
    offset = co - points[i]
    return max(0.0, min(1.0, 0.5 - offset.z / radii[i]))


# --- gold: the boss and the corner caps -------------------------------------------------------------------------

BOSS_PROFILE = [(0.058, 0.0), (0.058, 0.005), (0.054, 0.010), (0.045, 0.011), (0.040, 0.008), (0.034, 0.009),
                (0.028, 0.016), (0.016, 0.025), (0.0, 0.028)]


def _boss(material, flap_tree):
    """A round gold boss in the middle of the lid: a raised rim, a groove, a dome."""
    hit, normal = forms.probe(flap_tree, Vector((0.0, 1.0, 1.515)), Vector((0.0, -1.0, 0.0)))
    return forms.lathe("boss", BOSS_PROFILE, hit - normal * 0.002, normal, material, segments=16,
                       across=Vector((1.0, 0.0, 0.0)))


def _corner_cap(material, bag_tree, side, counts=(16, 8, 20)):
    """A gold cap over the bag's bottom back corner: the corner's patch of the bag's surface, lifted and thickened."""
    params, faces = forms.box_grid(counts)

    def inside(p):
        return p[0] * side > 0.70 and p[1] > 0.40 and p[2] < -0.70

    keep = [f for f in faces if all(inside(params[i]) for i in f)]
    used = sorted({i for f in keep for i in f})
    index = {old: new for new, old in enumerate(used)}
    verts = []
    for old in used:
        hit, normal, _, _ = bag_tree.find_nearest(bag_point(params[old]))
        verts.append(hit + normal * 0.006)
    cap = forms.mesh_object(f"corner_cap_{side}", verts, [[index[i] for i in f] for f in keep], material)
    _thicken(cap, 0.004)
    return cap


# --- antler toggles and sinew loops ----------------------------------------------------------------------------

def _on_back(shell, x, z, lift):
    """The point `lift` off the pack's back surface at (x, z), seen from behind, and the surface normal there."""
    hit, normal = forms.probe(shell, Vector((x, 1.0, z)), Vector((0.0, -1.0, 0.0)))
    return hit + normal * lift, normal


def _toggle(kit, shell, side, count=7):
    """An antler tine lying across the bag below the lid's edge, thick cut end inwards, tip curving up and out,
    held by a sinew loop stitched to the lid."""
    edge = flap_edge_z(TOGGLE_X)
    z0 = edge - TOGGLE_DROP
    points, radii, grades = [], [], []
    for n in range(count):
        t = n / (count - 1)
        x = side * (TOGGLE_X - 0.040 + 0.082 * t)
        r = 0.0024 + 0.0074 * (1.0 - t ** 1.2)
        point, _ = _on_back(shell, x, z0 + 0.016 * t * t, 0.0105 + 0.004 * t)
        points.append(point)
        radii.append(r)
        grades.append(t)
    tine = forms.tube(f"toggle_{side}", points, radii, kit["antler"], sides=6)
    forms.point_values(tine, "grade", [grades[min(n // 6, count - 1)] for n in range(len(tine.data.vertices))])
    _sinew_loop(kit["sinew"], shell, side * TOGGLE_X, z0, edge, side)
    return tine


def _sinew_loop(material, shell, x, z_toggle, z_edge, side, steps=14):
    """A loop of sinew from under the lid's edge down round the toggle, arching over the tine where it crosses."""
    top, bottom = z_edge + 0.014, z_toggle - 0.013
    middle, half = (top + bottom) / 2, (top - bottom) / 2
    points = []
    for n in range(steps):
        phi = 2 * math.pi * n / steps
        z = middle + half * math.cos(phi)
        arch = 0.013 * math.exp(-((z - z_toggle) / 0.007) ** 2)
        point, _ = _on_back(shell, x + 0.011 * math.sin(phi), z, 0.0035 + arch)
        points.append(point)
    return forms.tube(f"toggle_loop_{side}", points, [0.0028] * steps, material, sides=4, closed=True)


# --- the antler charm ------------------------------------------------------------------------------------------

def _on_side(tree, y, z, lift):
    """The point `lift` off the bag's left side (+x) at (y, z), and the surface normal there."""
    hit, normal = forms.probe(tree, Vector((1.0, y, z)), Vector((-1.0, 0.0, 0.0)))
    return hit + normal * lift


def _charm(kit, tree):
    """A small forked antler tine on a sinew cord with a gold bead, hanging on the wearer's left side, towards the
    back, just under the ruff."""
    y, top = 0.345, RUFF_Z - 0.02
    cord = [_on_side(tree, y, top - 0.012 * n, 0.004) for n in range(5)]
    forms.tube("charm_cord", cord, [0.0022] * 5, kit["sinew"], sides=4)
    bead = cord[-1] + Vector((0.0, 0.0, -0.006))
    forms.lathe("charm_bead", [(0.0065, -0.004), (0.0075, 0.0), (0.0065, 0.004)], bead, Vector((0, 0, 1)),
                kit["gold"], segments=8)
    main = [_on_side(tree, y + 0.006 * t * t, top - 0.062 - 0.068 * t, 0.007 + 0.008 * t) for t in
            (0.0, 0.2, 0.4, 0.6, 0.8, 1.0)]
    _tine("charm_tine", main, 0.0055, kit["antler"])
    fork = [main[2] + Vector((0.004, -0.010 * t, -0.022 * t)) for t in (0.0, 0.33, 0.66, 1.0)]
    _tine("charm_fork", fork, 0.0032, kit["antler"])


def _tine(name, points, base, material):
    count = len(points)
    radii = [base * (1.0 - 0.8 * n / (count - 1)) for n in range(count)]
    tine = forms.tube(name, points, radii, material, sides=5)
    forms.point_values(tine, "grade", [min(1.0, (n // 5) / (count - 1)) for n in range(len(tine.data.vertices))])
    return tine


# --- the waist band --------------------------------------------------------------------------------------------

BAND_Z, BAND_W = 1.235, 0.042


def _band(kit, tree, steps=19):
    """A broad strap round the back and sides of the bag's lower half, a gold buckle in the middle of the back."""
    centre, left, right, normals = Vector((0.0, 0.29, BAND_Z)), [], [], []
    for n in range(steps):
        angle = math.radians(-122.0 + 244.0 * n / (steps - 1))
        out = Vector((math.sin(angle), math.cos(angle), 0.0))
        low, normal = forms.probe(tree, centre + out * 0.6 - Vector((0, 0, BAND_W / 2)), -out)
        high, _ = forms.probe(tree, centre + out * 0.6 + Vector((0, 0, BAND_W / 2)), -out)
        left.append(low + normal * 0.002)
        right.append(high + normal * 0.002)
        normals.append(normal)
    forms.strap("band", left, right, normals, 0.006, kit["strap"])
    mid = steps // 2
    _buckle("band_buckle", right[mid], left[mid], right[mid + 1] - right[mid - 1], normals[mid], kit["gold"], 0.006,
            size=(0.024, 0.030))


# --- shoulder straps -------------------------------------------------------------------------------------------

def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))


def _shoulder_straps(kit, reach=8):
    """Short straps from the bag's top over each shoulder, ending just past its top, a gold buckle on top."""
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in range(reach):
            inner, outer = SHOULDER_IN[k], SHOULDER_OUT[k]
            left.append(Vector((side * 0.08, inner[0], inner[1])) + _out(15 + 15 * k) * 0.005)
            right.append(Vector((side * 0.13, outer[0], outer[1])) + _out(15 + 15 * k) * 0.005)
        tip = 0.45
        left.append(left[-1].lerp(Vector((side * 0.08, *SHOULDER_IN[reach])) + _out(15 + 15 * reach) * 0.005, tip))
        right.append(right[-1].lerp(Vector((side * 0.13, *SHOULDER_OUT[reach])) + _out(15 + 15 * reach) * 0.005,
                                    tip))
        for i in range(len(left)):
            along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
            normal = (right[i] - left[i]).cross(along).normalized()
            normals.append(normal if normal.dot(_out(15 + 15 * min(i, reach))) > 0 else -normal)
        forms.strap(f"shoulder_strap_{side}", left, right, normals, 0.007, kit["strap"])
        _buckle(f"shoulder_buckle_{side}", left[4], right[4], left[5] - left[3], normals[4], kit["gold"], 0.007)


def _buckle(name, left, right, along, normal, material, lift, size=(0.029, 0.018)):
    """A gold buckle lying on a strap: a rectangular frame, a middle bar and a tongue. size: half extents across the
    strap and along it."""
    across = (right - left).normalized()
    along = (along - across * along.dot(across)).normalized()
    normal = across.cross(along) if across.cross(along).dot(normal) > 0 else -across.cross(along)
    centre = (left + right) / 2 + normal * (lift + 0.002)
    (w, h), t, bar = size, 0.0024, 0.0034
    axes = (across, along, normal)
    for part, offset, half in (("top", along * (h - bar), (w, bar, t)), ("bottom", -along * (h - bar), (w, bar, t)),
                               ("left", -across * (w - bar), (bar, h, t)), ("right", across * (w - bar), (bar, h, t)),
                               ("bar", Vector(), (w, bar * 0.8, t)),
                               ("tongue", along * h * 0.45 + normal * 0.001, (bar * 0.55, h * 0.5, t * 0.8))):
        forms.oriented_box(f"{name}_{part}", centre + offset, axes, half, material)

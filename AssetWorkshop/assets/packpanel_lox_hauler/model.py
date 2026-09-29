"""PackPanel's Lox Hauler, the Plains backpack: a frame pack. Two forged black-metal bars bent to the wearer's back,
joined by a crossbar behind the neck and turned back at the bottom into a load shelf with two more crossbars, carry
a bulky bundle wrapped in shaggy brown lox fur. Two off-white linen lashings cinch the bundle to the bars (one tied
off in a knot with hanging ends), a lox-pelt bedroll rolled fur side in (pale hide outside, fur drooping out of its
ends and along its edge) lies across the top under the crossbar, lashed to it with two linen loops, and two short
leather straps run over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the pack at -Z behind the spine. Same convention as the Trollhide
pack: parented straight under the bone it needs a local rotation of about +5.66 degrees about X and a local scale of
1/95 to sit as modelled. See fit.py for the check on the game's body.

The bundle's front and the bars follow the body's back surface, measured on the player's body in its rest pose (SKIN
below): the bundle presses 6 mm into the back, the bars clear the skin by a few millimetres, and the frame and
bedroll stay about 15 cm from the head (fit.py prints the distances).
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
BOTTOM, TOP = 1.05, 1.575               # the bundle's height range
PRESS = 0.006                           # how far the bundle's front sinks into the back
LOAD_X, LOAD_REAR, WRAP = 0.178, 0.345, 0.05
CENTRE_Y = 0.245                        # the bundle's middle, where the lashings are probed from
BAR_X, BAR_HX, BAR_HY = 0.195, 0.015, 0.013
BAR_TOP, TOPBAR_Z, SHELF_Z, SHELF_REAR, SHELF_FRONT, SHELF_TIP = 1.725, 1.695, 1.035, 0.365, 0.205, 0.392
STRAP_Z, STRAP_W, STRAP_T = (1.22, 1.44), 0.034, 0.004
ROLL_Y, ROLL_Z, ROLL_R, LOOP_X = 0.250, 1.626, 0.058, 0.13

# The player's back (y of the skin, cast from behind) in the rest pose: rows every 2.5 cm from z = 1.00, columns at
# SKIN_X; 0 where there is no body at that x (the waist is narrower than the shoulders).
SKIN_X = (0.0, 0.05, 0.10, 0.13, 0.15, 0.17, 0.19, 0.21)
SKIN_Z0, SKIN_DZ = 1.0, 0.025
SKIN = (
    (0.166, 0.164, 0.162, 0.125, 0.095, 0.065, 0, 0), (0.160, 0.158, 0.152, 0.120, 0.090, 0.059, 0, 0),
    (0.155, 0.153, 0.141, 0.116, 0.085, 0.053, 0, 0), (0.150, 0.144, 0.130, 0.112, 0.081, 0.042, 0, 0),
    (0.145, 0.137, 0.118, 0.102, 0.072, 0.030, 0, 0), (0.144, 0.135, 0.114, 0.098, 0.070, 0.027, 0, 0),
    (0.142, 0.132, 0.112, 0.097, 0.073, 0.033, 0, 0), (0.142, 0.131, 0.113, 0.098, 0.077, 0.037, 0, 0),
    (0.143, 0.134, 0.115, 0.099, 0.078, 0.038, 0, 0), (0.145, 0.138, 0.120, 0.101, 0.079, 0.040, 0, 0),
    (0.148, 0.144, 0.128, 0.108, 0.085, 0.044, 0, 0), (0.156, 0.153, 0.142, 0.124, 0.110, 0.067, 0, 0),
    (0.164, 0.163, 0.152, 0.140, 0.126, 0.093, 0.049, 0), (0.172, 0.171, 0.161, 0.150, 0.142, 0.123, 0.074, 0),
    (0.180, 0.179, 0.171, 0.159, 0.151, 0.144, 0.102, 0), (0.186, 0.186, 0.180, 0.168, 0.160, 0.153, 0.126, 0),
    (0.192, 0.192, 0.187, 0.177, 0.170, 0.161, 0.147, 0), (0.198, 0.199, 0.194, 0.186, 0.179, 0.169, 0.156, 0),
    (0.203, 0.205, 0.201, 0.193, 0.188, 0.177, 0.164, 0.140), (0.204, 0.205, 0.201, 0.194, 0.189, 0.182, 0.167, 0.153),
    (0.199, 0.200, 0.197, 0.190, 0.184, 0.177, 0.168, 0.155), (0.195, 0.196, 0.192, 0.185, 0.179, 0.172, 0.163, 0.154),
    (0.191, 0.191, 0.188, 0.180, 0.174, 0.167, 0.158, 0.148), (0.187, 0.187, 0.183, 0.174, 0.168, 0.161, 0.152, 0.142),
    (0.173, 0.176, 0.170, 0.161, 0.153, 0.138, 0.129, 0.116), (0.158, 0.157, 0.147, 0.125, 0.106, 0.089, 0.074, 0),
    (0.140, 0.133, 0.106, 0.078, 0, 0, 0, 0),
)
# The shoulder's skin (y, z) at x = 0.08 and x = 0.13, every 15 degrees from 15 (behind) to 150 (in front), measured
# around (0.04, 1.48) in the side view: the rails the shoulder straps follow (from the Trollhide pack's fit).
SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]
# The bundle's grid rows (heights): a row on each lashing and one either side, so the lashings cinch it.
LOAD_ROWS_Z = (1.05, 1.062, 1.09, 1.14, 1.195, 1.22, 1.245, 1.30, 1.36, 1.415, 1.44, 1.465, 1.515, 1.55, 1.566, 1.575)


def build():
    random.seed(11)
    kit = {"fur": looks.fur(PIVOT, "hauler_fur", looks.FUR, (24.0, 24.0, 10.0)),
           "roll": looks.pale_hide(PIVOT),
           "roll_end": looks.roll_end(PIVOT, ROLL_Y, ROLL_Z), "linen": looks.linen(PIVOT),
           "metal": looks.black_metal(PIVOT), "leather": looks.strap_leather(PIVOT)}
    load = _load(kit["fur"])
    bars = _frame(kit["metal"])
    _bedroll(kit["roll"], kit["roll_end"])
    _roll_fur(kit["fur"])
    _roll_loops(kit["linen"])
    _tufts(kit["fur"], forms.surface([load]))
    tree = forms.surface([load] + bars)
    lashings = [_lashing(kit["linen"], tree, z, f"lashing_{i}") for i, z in enumerate(STRAP_Z)]
    _knot(kit["linen"], tree, lashings[1])
    _shoulder_straps(kit["leather"])
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


# --- the body --------------------------------------------------------------------------------------------------

def skin(x, z):
    """The back's skin (y) at (x, z), bilinear in SKIN."""
    x = min(abs(x), SKIN_X[-1])
    fz = (min(max(z, SKIN_Z0), SKIN_Z0 + SKIN_DZ * (len(SKIN) - 1)) - SKIN_Z0) / SKIN_DZ
    i = min(int(fz), len(SKIN) - 2)
    j = max(k for k in range(len(SKIN_X) - 1) if SKIN_X[k] <= x)
    tz, tx = fz - i, (x - SKIN_X[j]) / (SKIN_X[j + 1] - SKIN_X[j])
    row = [SKIN[r][j] * (1 - tx) + SKIN[r][j + 1] * tx for r in (i, i + 1)]
    return row[0] * (1 - tz) + row[1] * tz


def front_y(x, z):
    """The bundle's front: on the skin, but curling forward no more than WRAP at its sides (low down the waist is
    narrower than the bundle, and the bundle must not wrap round it)."""
    return max(skin(x, z), skin(0.0, z) - WRAP * (x / LOAD_X) ** 2) - PRESS


def bar_y(z):
    """The middle of a frame bar at height z: clear of the skin under it (looking 2 cm up and down, so the bend is
    smooth), never ahead of the bundle's front corner; straight above the shoulder blades."""
    z = min(z, 1.60)
    under = max(skin(x, zz) for x in (BAR_X - BAR_HX, BAR_X, BAR_X + BAR_HX) for zz in (z - 0.02, z, z + 0.02))
    return max(under + 0.004, front_y(BAR_X - BAR_HX, z) - 0.004) + BAR_HY


# --- the fur bundle --------------------------------------------------------------------------------------------

def _cinch(z):
    return max(math.exp(-((z - s) / 0.018) ** 2) for s in STRAP_Z)


def load_point(param):
    """A point of the bundle for a point of the cube [-1, 1]^3, and how deep into the bundle it lies (0 front, 1
    rear): rounded, bulging behind the bars, fuller in the middle, squeezed where the lashings go round."""
    a, d, h = forms.rounded(param, 4.0)
    v, t = (h + 1) / 2, (d + 1) / 2
    z = BOTTOM + v * (TOP - BOTTOM)
    bulge = forms.smoothstep(0.25, 0.65, t) * math.sin(math.pi * min(1.0, max(0.0, v)))
    x = a * (LOAD_X + 0.010 * bulge)
    front = front_y(x, z)
    rear = LOAD_REAR + 0.035 * math.sin(math.pi * (0.12 + 0.8 * v)) + 0.012 * (1 - a * a)
    squeeze = _cinch(z)
    y = front + t * (rear - front) * (1 - 0.07 * squeeze)
    return Vector((x * (1 - 0.055 * squeeze), y, z)), t


def _load(material):
    rows = [forms.spread_rows(8), forms.spread_rows(5), [2 * (z - BOTTOM) / (TOP - BOTTOM) - 1 for z in LOAD_ROWS_Z]]
    params, faces = forms.box_grid(rows)
    points = [load_point(p) for p in params]
    load = forms.mesh_object("load", [p for p, _ in points], faces, material)
    normals = [v.normal.copy() for v in load.data.vertices]
    for vert, normal, (_, depth) in zip(load.data.vertices, normals, points):
        vert.co += normal * _shag(vert.co) * forms.smoothstep(0.06, 0.3, depth) * (1 - _cinch(vert.co.z))
    return load


def _shag(co):
    """How far the fur stands out here: a few big bulges (the load inside), lumps and long locks hanging down."""
    bulges = noise.noise(co * 2.6 + Vector((3.1, 0.0, 0.0))) * 0.016
    lumps = noise.noise(co * 5.0) * 0.010
    locks = noise.noise(Vector((co.x * 22.0, co.y * 22.0, co.z * 7.0))) * 0.009
    return bulges + lumps + locks + 0.008


def _tuft_spots():
    """(angle round the bundle from straight behind, height, length, hangs straight down) for every lock of fur: a
    fringe along the lower rim and rows down both back corners, none under a lashing."""
    spots = [(math.radians(a + random.uniform(-4, 4)), 1.085, random.uniform(0.04, 0.06), True)
             for a in range(-98, 99, 14)]
    for side in (-1, 1):
        spots += [(side * math.radians(random.uniform(62, 76)), z, random.uniform(0.048, 0.064), False)
                  for z in (1.13, 1.17, 1.29, 1.33, 1.37, 1.51, 1.55)]
    return spots


def _tufts(material, tree):
    """Locks of fur standing out of the bundle (they give it its shaggy outline), laid down along the surface; the
    fringe on the rim hangs straight down."""
    for i, (phi, z, length, hangs) in enumerate(_tuft_spots()):
        out = Vector((math.sin(phi), math.cos(phi), 0.0))
        hit, normal = forms.probe(tree, Vector((0.0, CENTRE_Y, z)) + out * 0.8, -out)
        if hit is None:
            continue
        if hangs:
            tip = hit + out * 0.016 + Vector((0.0, 0.0, -length))
        else:
            down = Vector((0.0, 0.0, -1.0))
            along = (down - normal * normal.dot(down)).normalized()
            tip = hit + normal * 0.028 + (along * 0.75 + down * 0.25).normalized() * length
        tip += Vector((random.uniform(-1, 1), random.uniform(-1, 1), 0.0)) * 0.006
        forms.tuft(f"tuft_{i}", hit, normal, tip, random.uniform(0.03, 0.04), material)


# --- the frame -------------------------------------------------------------------------------------------------

def _bar_path(side):
    """One bar, top to bottom: straight above the shoulder blades, bent to the back, turned back into a shelf arm
    (a knob caps its end)."""
    heights = (BAR_TOP, 1.62, 1.55, 1.50, 1.45, 1.40, 1.35, 1.30, 1.24, 1.16)
    ys = [bar_y(z) for z in heights]
    ys = [max(ys[i], (ys[max(i - 1, 0)] + 2 * ys[i] + ys[min(i + 1, len(ys) - 1)]) / 4) for i in range(len(ys))]
    low = bar_y(1.10)
    path = [(y, z) for y, z in zip(ys, heights)]
    path += [(low, 1.10), (low + 0.007, 1.068), (low + 0.025, 1.047), (low + 0.052, SHELF_Z + 0.001),
             (SHELF_TIP, SHELF_Z)]
    return [Vector((side * BAR_X, y, z)) for y, z in path]


def _frame(material):
    """The two bars, the crossbar behind the neck, the shelf's two crossbars, collars on the joints, knobs on top."""
    section = forms.bar_section(BAR_HX, BAR_HY)
    wobble = lambda i: Vector((random.uniform(-1, 1), random.uniform(-1, 1), 0.0)) * 0.0012  # noqa: E731
    bars = [forms.sweep(f"bar_{s}", _bar_path(s), section, Vector((1, 0, 0)), material, wobble) for s in (-1, 1)]
    top_y = bar_y(TOPBAR_Z)
    cross = forms.bar_section(0.012, 0.012)
    for name, y, z in (("top", top_y, TOPBAR_Z), ("shelf_rear", SHELF_REAR, SHELF_Z),
                       ("shelf_front", SHELF_FRONT, SHELF_Z)):
        path = [Vector((x, y, z)) for x in (-BAR_X, BAR_X)]
        forms.sweep(f"crossbar_{name}", path, cross, Vector((0, 0, 1)), material)
    axes = (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    for s in (-1, 1):
        forms.oriented_box(f"collar_top_{s}", Vector((s * BAR_X, top_y, TOPBAR_Z)), axes, (0.019, 0.018, 0.016),
                           material)
        forms.oriented_box(f"collar_shelf_{s}", Vector((s * BAR_X, SHELF_REAR, SHELF_Z)), axes,
                           (0.019, 0.017, 0.017), material)
        forms.ellipsoid(f"knob_{s}", Vector((s * BAR_X, bar_y(BAR_TOP), BAR_TOP + 0.006)), (0.021, 0.020, 0.018),
                        material)
        forms.ellipsoid(f"shelf_knob_{s}", Vector((s * BAR_X, SHELF_TIP + 0.004, SHELF_Z)), (0.018, 0.019, 0.019),
                        material, 8, 4)
    return bars


# --- the bedroll -----------------------------------------------------------------------------------------------

ROLL_SECTIONS = [(-0.208, 0.88), (-0.196, 0.97), (-0.165, 1.0), (-LOOP_X, 0.91), (-0.095, 1.0), (0.0, 1.03),
                 (0.095, 1.0), (LOOP_X, 0.91), (0.165, 1.0), (0.196, 0.97), (0.208, 0.88)]
ROLL_TURN = math.radians(-25)    # where the outer layer ends, measured from +Y (behind) towards +Z


def roll_point(x, k, around, scale):
    """The rolled pelt at step k of `around`: a spiral, so the outer layer ends in a small step."""
    angle = ROLL_TURN + 2 * math.pi * k / around
    r = ROLL_R * scale * (0.95 + 0.085 * k / around)
    return Vector((x, ROLL_Y + r * math.cos(angle), ROLL_Z + r * math.sin(angle)))


def _bedroll(material, end_material, around=12):
    verts, faces = [], []
    ring = around + 1
    for x, scale in ROLL_SECTIONS:
        verts += [roll_point(x, k, around, scale * random.uniform(0.97, 1.03)) for k in range(ring)]
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


def _roll_fur(material):
    """The fur lining, rolled inwards: locks drooping out of both ends and peeking out along the outer layer's
    edge."""
    for s in (-1, 1):
        for k in range(5):
            angle = math.radians(72 * k + random.uniform(10, 50))
            radial = Vector((0.0, math.cos(angle), math.sin(angle)))
            base = Vector((s * (ROLL_SECTIONS[-1][0] - 0.006), ROLL_Y, ROLL_Z)) + radial * ROLL_R * 0.66
            tip = base + Vector((s * random.uniform(0.012, 0.018), 0.0, -random.uniform(0.02, 0.03))) + radial * 0.01
            forms.tuft(f"roll_end_tuft_{s}_{k}", base, Vector((s, 0.0, 0.0)), tip, 0.03, material)
    _roll_lining(material)


def _roll_lining(material, count=15):
    """A strip of the fur lining showing along the outer layer's edge, its free edge ragged."""
    left, right, normals = [], [], []
    for i in range(count):
        x = ROLL_SECTIONS[0][0] + 0.012 + (ROLL_SECTIONS[-1][0] - ROLL_SECTIONS[0][0] - 0.024) * i / (count - 1)
        reach = math.radians(9 + (7 if i % 2 else 0) + random.uniform(-2, 2))
        for rail, angle, r in ((left, ROLL_TURN, 1.03), (right, ROLL_TURN + reach, 0.985)):
            rail.append(Vector((x, ROLL_Y + ROLL_R * r * math.cos(angle), ROLL_Z + ROLL_R * r * math.sin(angle))))
        normals.append(Vector((0.0, math.cos(ROLL_TURN + reach / 2), math.sin(ROLL_TURN + reach / 2))))
    forms.strap("roll_lining", left, right, normals, 0.004, material)


def _roll_loops(material, samples=16):
    """A linen loop round the bedroll and the crossbar above it at each LOOP_X: the hull of the two in side view."""
    circles = [(Vector((ROLL_Y, ROLL_Z)), ROLL_R * 0.93 + 0.003), (Vector((bar_y(TOPBAR_Z), TOPBAR_Z)), 0.017)]
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in range(samples):
            angle = 2 * math.pi * k / samples
            out = Vector((math.cos(angle), math.sin(angle)))
            centre, radius = max(circles, key=lambda c: c[0].dot(out) + c[1])
            point = centre + out * radius
            for rail, dx in ((left, -0.015), (right, 0.015)):
                rail.append(Vector((side * LOOP_X + dx, point.x, point.y)))
            normals.append(Vector((0.0, out.x, out.y)))
        forms.strap(f"roll_loop_{side}", left, right, normals, STRAP_T, material, closed=True)


# --- the lashings ----------------------------------------------------------------------------------------------

def _lashing(material, tree, z, name, steps=20):
    """A linen band round the bundle's back and sides at height z, over the bars and tucked round their fronts."""
    end = math.atan2(BAR_X, bar_y(z) - CENTRE_Y) + math.radians(12)
    left, right, normals = [], [], []
    for k in range(steps + 1):
        phi = -end + 2 * end * k / steps
        out = Vector((math.sin(phi), math.cos(phi), 0.0))
        hits = [forms.probe(tree, Vector((0.0, CENTRE_Y, z + dz)) + out * 0.8, -out)
                for dz in (-STRAP_W / 2, STRAP_W / 2)]
        if hits[0][0] is None or hits[1][0] is None:
            continue
        across = hits[0][1] + hits[1][1]
        normal = (Vector((across.x, across.y, 0.0)).normalized() + out).normalized()
        left.append(hits[0][0] + normal * 0.0015)
        right.append(hits[1][0] + normal * 0.0015)
        normals.append(normal)
    forms.strap(name, left, right, normals, STRAP_T, material)
    return left, right, normals


def _knot(material, tree, lashing, phi=0.32):
    """The upper lashing tied off behind the wearer's left: a lumpy knot and two short hanging ends."""
    left, right, normals = lashing
    at = min(range(len(left)), key=lambda i: abs(math.atan2(left[i].x, left[i].y - CENTRE_Y) - phi))
    centre = (left[at] + right[at]) / 2 + normals[at] * (STRAP_T + 0.006)
    along = (left[min(at + 1, len(left) - 1)] - left[max(at - 1, 0)]).normalized()
    up = Vector((0, 0, 1))
    forms.ellipsoid("knot", centre, (0.021, 0.012, 0.019), material, 8, 5, (along, normals[at], up))
    forms.ellipsoid("knot_lump", centre + along * 0.012 - up * 0.008 + normals[at] * 0.002, (0.013, 0.010, 0.012),
                    material, 6, 4, (along, normals[at], up))
    for tail, (spread, length) in enumerate(((-0.10, 0.085), (0.12, 0.11))):
        _tail(material, tree, f"knot_tail_{tail}", phi + spread * 0.3, spread, length)


def _tail(material, tree, name, phi, drift, length, steps=5, width=0.017):
    """A hanging end of linen laid on the fur below the knot, drifting sideways as it falls."""
    left, right, normals = [], [], []
    for s in range(steps):
        z = STRAP_Z[1] - 0.012 - length * s / (steps - 1)
        angle = phi + drift * s / (steps - 1) * 0.4
        out = Vector((math.sin(angle), math.cos(angle), 0.0))
        across = Vector((out.y, -out.x, 0.0)) * (width / 2)
        hits = [forms.probe(tree, Vector((0.0, CENTRE_Y, z)) + out * 0.8 + a, -out) for a in (-across, across)]
        if hits[0][0] is None or hits[1][0] is None:
            break
        normal = (hits[0][1] + hits[1][1]).normalized()
        lift = 0.003 + 0.004 * s / (steps - 1)
        left.append(hits[0][0] + normal * lift)
        right.append(hits[1][0] + normal * lift)
        normals.append(normal)
    forms.strap(name, left, right, normals, 0.0035, material)


# --- the shoulder straps ---------------------------------------------------------------------------------------

def _shoulder_straps(material, reach=6):
    """Short leather straps from the top of the bundle over each shoulder, ending just past its top."""
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in range(reach):
            out = _out(15 + 15 * k)
            inner, outer = SHOULDER_IN[k], SHOULDER_OUT[k]
            left.append(Vector((side * 0.08, inner[0], inner[1])) + out * 0.005)
            right.append(Vector((side * 0.13, outer[0], outer[1])) + out * 0.005)
        tip, out = 0.5, _out(15 + 15 * reach)
        left.append(left[-1].lerp(Vector((side * 0.08, *SHOULDER_IN[reach])) + out * 0.005, tip))
        right.append(right[-1].lerp(Vector((side * 0.13, *SHOULDER_OUT[reach])) + out * 0.005, tip))
        for i in range(len(left)):
            along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
            normal = (right[i] - left[i]).cross(along).normalized()
            normals.append(normal if normal.dot(_out(15 + 15 * min(i, reach))) > 0 else -normal)
        forms.strap(f"shoulder_strap_{side}", left, right, normals, 0.007, material)


def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))

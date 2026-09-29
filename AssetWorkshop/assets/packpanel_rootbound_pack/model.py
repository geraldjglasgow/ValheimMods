"""PackPanel's Rootbound Pack, the Swamp backpack, worn on the player's upper back. An upright bag of dark, damp bog
leather held in an external frame of twisted roots: a rail up each back edge that curls under the bottom and forward
over the top, a bar across the top and the bottom, two thinner roots woven in an X across the back, curling
tendrils at the corners and a thin root winding round one rail. Iron bands with rivets bind the rails where the bars
meet them, iron loops hold the two short shoulder straps, and an iron pull ring hangs from the flap. A leather flap
covers the top, and every seam (the flap's edge, the side seams, the bottom panel) is sealed with a bead of glossy
green guck, dripping in places.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. This is the same convention as the
Trollhide Backpack; parented straight under the bone, the pack needs a local rotation of about +5.66 degrees about
X and a local scale of 1/95 to sit as modelled. See fit.py for the check on the game's body.

The bag's front follows the body's back surface (measured on the player's body in its rest pose, BACK below, from
the Trollhide Backpack) and presses 6 mm into it, so there is no gap and nothing shows through the chest or the
sides of the torso.
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
BOTTOM, TOP, HALF_W = 1.175, 1.595, 0.172
PRESS = 0.006                           # how far the bag's front sinks into the back
ROUND = 4.0                             # the bag's superellipse power: higher is boxier
RAIL_R, BAR_R, CROSS_R, TENDRIL_R, VINE_R = 0.0195, 0.0152, 0.0115, 0.0068, 0.0042
STEP = 0.026                            # ring spacing along the roots
TWIST = 2 * math.pi / 0.30              # how fast the root strands turn, radians per metre

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
# The flap's course in the side view, outside the bag: it is laid on the bag's nearest surface.
FLAP_PATH = [(0.19, 1.70), (0.30, 1.70), (0.42, 1.60), (0.44, 1.50), (0.44, 1.30)]
FLAP_LENGTH, FLAP_HALF_W = 0.400, 0.140

# The frame, as paths over the bag's parameter cube (x across, y front to back, z bottom to top; see bag_point).
RAIL_D = 0.75                           # how far back the rails run up the bag's sides
RAIL = [(0.86, -0.3, -1.0), (0.97, 0.4, -1.0), (1.0, RAIL_D, -1.0), (1.0, RAIL_D, 1.0), (0.97, 0.4, 1.0),
        (0.9, -0.2, 1.0)]
TOP_BAR, BOTTOM_BAR = [(-1.0, 1.0, 1.0), (1.0, 1.0, 1.0)], [(-1.0, 1.0, -1.0), (1.0, 1.0, -1.0)]
CROSS = [(-0.95, 1.0, -0.95), (0.95, 1.0, 0.97)]


def build():
    random.seed(11)
    kit = {"leather": looks.bog_leather(PIVOT), "flap": looks.flap_leather(PIVOT), "root": looks.root(PIVOT),
           "guck": looks.guck(PIVOT), "iron": looks.iron(PIVOT), "strap": looks.strap_leather(PIVOT)}
    bag = _bag(kit["leather"])
    flap, edge = _flap(kit["flap"], forms.surface([bag]))
    skin = forms.surface([bag, flap])
    _seams(kit, skin, edge)
    rails = _frame(kit, skin)
    _bands(kit, rails)
    _pull_ring(kit, skin, edge)
    _shoulder_straps(kit)
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
    """A point of the bag for a point of the cube [-1, 1]^3: an upright, boxy sack, a little full in the belly,
    narrowing slightly to the top, its bottom sagging."""
    a, d, h = forms.rounded(param, ROUND)
    v = (h + 1) / 2
    x = a * (HALF_W + 0.012 * math.sin(math.pi * v) - 0.010 * v)
    z = BOTTOM + v * (TOP - BOTTOM) - 0.016 * (1 - v) ** 3 * (1 - a * a)
    front = front_y(x, z)
    back = 0.318 + 0.032 * math.sin(math.pi * (0.12 + 0.8 * v)) + 0.008 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((8, 4, 8))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 8.0) * 0.004       # soft, uneven leather
    forms.point_values(bag, "wear", [forms.edge_wear(p) for p in params])
    return bag


def outward(point):
    """Away from the bag's middle: the direction a point of its surface is seen from."""
    centre = Vector((0.0, 0.265, min(max(point.z, 1.27), 1.50)))
    return (point - centre).normalized()


def land(tree, guide):
    """(point, normal) of the surface under `guide`, seen from outside the bag, or (None, None)."""
    out = outward(guide)
    return forms.probe(tree, guide + out * 0.12, -out)


def surface_path(tree, guides, step):
    """The guides dropped onto the surface, smoothed and spaced `step` apart: (points, normals)."""
    hits = [land(tree, g)[0] for g in forms.resample(guides, 0.008)]
    points = forms.resample(forms.smooth([h for h in hits if h is not None], 4), step)
    landed = [land(tree, p) for p in points]
    points = [h if h is not None else p for p, (h, _) in zip(points, landed)]
    normals = [n if n is not None else outward(p) for p, (_, n) in zip(points, landed)]
    normals = [(a + b * 2 + c).normalized() for a, b, c in zip([normals[0]] + normals, normals, normals[1:] +
                                                                  [normals[-1]])]
    return points, normals


def over_cube(params, per_span=16):
    """Bag points along a path over the parameter cube (straight between its waypoints)."""
    points = []
    for a, b in zip(params, params[1:]):
        points += [bag_point(tuple(x + (y - x) * i / per_span for x, y in zip(a, b))) for i in range(per_span)]
    return points + [bag_point(params[-1])]


# --- the flap --------------------------------------------------------------------------------------------------

def _path(s):
    """(point, outward normal) in the side view at arc length s along FLAP_PATH."""
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if s <= length or (y1, z1) == FLAP_PATH[-1]:
            t = s / length
            return Vector((y0 + (y1 - y0) * t, z0 + (z1 - z0) * t))
        s -= length


def _flap(material, bag_tree, columns=9, rows=6):
    """The flap: from the middle of the top, over the back edge and down the back to a rounded lower edge.
    Returns the flap and its lower edge (outer surface) as points across it."""
    verts, wear = [], []
    for i in range(columns):
        u = -1.0 + 2.0 * i / (columns - 1)
        end = FLAP_LENGTH - 0.042 * u * u + random.uniform(-0.004, 0.004)
        for j in range(rows):
            t = j / (rows - 1)
            side = _path(end * t)
            hit, normal, _, _ = bag_tree.find_nearest(Vector((u * FLAP_HALF_W, side.x, side.y)))
            verts.append(hit + normal * (0.0065 + noise.noise(hit * 14.0) * 0.0015))
            wear.append(max(0.5 if i in (0, columns - 1) else 0.0, max(0.0, t - 0.7) / 0.3))
    faces = [(i * rows + j, (i + 1) * rows + j, (i + 1) * rows + j + 1, i * rows + j + 1)
             for i in range(columns - 1) for j in range(rows - 1)]
    flap = forms.mesh_object("flap", verts, faces, material)
    forms.point_values(flap, "wear", wear)
    edge = [flap.data.vertices[i * rows + rows - 1].co.copy() for i in range(columns)]
    _thicken(flap, 0.005)
    return flap, edge


def _thicken(obj, thickness):
    """Gives the sheet its thickness on the bag's side of it (the pipeline applies the modifier)."""
    poly = obj.data.polygons[len(obj.data.polygons) // 2]
    if poly.normal.dot(poly.center - Vector((0.0, 0.26, 1.40))) < 0:
        obj.data.flip_normals()
    mod = obj.modifiers.new("thickness", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = -1.0
    mod.use_even_offset = True


# --- guck ------------------------------------------------------------------------------------------------------

def _seams(kit, skin, flap_edge):
    """Guck beads along the flap's edge, down each side and across the bottom panel, with a few drips."""
    for side in (-1, 1):
        guides = over_cube([(side, 0.05, -0.9), (side, 0.05, 0.92)])
        _guck_line(f"guck_side_{side}", kit, skin, guides, drips=(0.18,) if side > 0 else ())
    guides = over_cube([(-0.88, 1.0, -0.5), (0.88, 1.0, -0.5)])
    _guck_line("guck_bottom", kit, skin, guides, drips=(0.3, 0.72))
    edge = [p + outward(p) * 0.002 for p in forms.resample(flap_edge, 0.01)]
    _guck_line("guck_flap", kit, skin, edge, drips=(0.25, 0.7), width=0.017)


def _guck_line(name, kit, tree, guides, drips=(), width=0.015, height=0.0055):
    path, normals = surface_path(tree, guides, 0.028)
    count = len(path)
    widths, heights = [], []
    for i, point in enumerate(path):
        fade = min(1.0, min(i, count - 1 - i) / 1.5) ** 0.7
        blob = 1.0 + 0.3 * noise.noise(point * 30.0)
        widths.append(width * blob * (0.4 + 0.6 * fade))
        heights.append(height * blob * fade)
    forms.bead(name, tree, path, normals, widths, heights, kit["guck"])
    for k, at in enumerate(drips):
        _drip(f"{name}_drip_{k}", kit, tree, path[round(at * (count - 1))])


def _drip(name, kit, tree, start):
    """A drop of guck run down the surface from a bead, fattening towards its end."""
    radii = [0.0034, 0.0042, 0.0056, 0.0052]
    points = []
    for k, r in enumerate(radii):
        hit, normal = land(tree, start + Vector((0.0015 * k * random.uniform(-1, 1), 0.0, -0.013 * k)))
        points.append(hit + normal * (r * 0.55) if hit is not None else start)
    drop = forms.tube(name, points, radii, kit["guck"], sides=5)
    forms.point_values(drop, "crest", [0.65] * len(drop.data.vertices))


# --- the root frame --------------------------------------------------------------------------------------------

def root_path(tree, params, radius, lift=0.75, wobble=0.004):
    """A root's centre line over the bag along a parameter path, sunk a quarter of its radius into the leather and
    wandering a little from side to side."""
    points, normals = surface_path(tree, over_cube(params), STEP)
    return forms.smooth([p + n * radius * lift + noise.noise_vector(p * 9.0) * wobble
                         for p, n in zip(points, normals)], 1)


def radii(points, radius, taper=0.07, knots=0.24):
    """Full radius along a root, knotty, tapering to a point over `taper` metres at each end."""
    along = [0.0]
    for a, b in zip(points, points[1:]):
        along.append(along[-1] + (b - a).length)
    result = []
    for s, point in zip(along, points):
        end = min(1.0, min(s, along[-1] - s) / taper)
        knot = 0.7 * noise.noise(point * 11.0) + 0.3 * noise.noise(point * 31.0)
        result.append(radius * max(0.25, end ** 0.6) * (1.0 + knots * knot))
    return result


def _root(name, kit, points, radius, sides=6, ridge=0.28):
    return forms.tube(name, points, radii(points, radius), kit["root"], sides, TWIST * random.uniform(0.85, 1.15),
                      ridge, wander=1.3)


def _frame(kit, skin):
    """Rails up the back edges, bars across the top and the bottom, the woven X, tendrils and a winding root.
    Returns the rails' centre lines."""
    rails = {}
    for side in (-1, 1):
        rails[side] = root_path(skin, [(side * a, d, h) for a, d, h in RAIL], RAIL_R)
        _root(f"rail_{side}", kit, rails[side], RAIL_R)
    _root("top_bar", kit, root_path(skin, TOP_BAR, BAR_R), BAR_R)
    _root("bottom_bar", kit, root_path(skin, BOTTOM_BAR, BAR_R), BAR_R)
    _cross(kit, skin)
    _tendrils(kit, rails)
    _vine(kit, rails[1])
    return rails


def _cross(kit, skin):
    """Two thinner roots in an X across the back, the first passing over the second where they cross."""
    over = root_path(skin, CROSS, CROSS_R)
    under = root_path(skin, [(-a, d, h) for a, d, h in CROSS], CROSS_R)
    middle = min(over, key=lambda p: min((p - q).length for q in under))
    lifted = []
    for point in over:
        distance = math.hypot(point.x - middle.x, point.z - middle.z)
        lifted.append(point + outward(point) * CROSS_R * 1.5 * math.exp(-(distance / 0.045) ** 2))
    _root("cross_over", kit, forms.smooth(lifted, 1), CROSS_R)
    _root("cross_under", kit, under, CROSS_R)


def _nearest(points, target):
    return min(range(len(points)), key=lambda i: (points[i] - target).length)


# Tendril shapes as offsets from where they leave a rail: (outwards along x, back along y, up along z).
TENDRIL_TOP = [(0.0, 0.0, 0.0), (0.014, 0.012, 0.018), (0.028, 0.022, 0.034), (0.040, 0.022, 0.048),
               (0.047, 0.010, 0.056), (0.044, -0.002, 0.052)]
TENDRIL_BOTTOM = [(0.0, 0.0, 0.0), (0.012, 0.010, -0.022), (0.020, 0.012, -0.046), (0.018, 0.002, -0.066),
                  (0.008, -0.012, -0.074)]


def _tendrils(kit, rails):
    """Thin root ends curling up from the top corners and down from the bottom corners."""
    for side, rail in rails.items():
        for shape, corner in ((TENDRIL_TOP, (side, RAIL_D, 0.93)), (TENDRIL_BOTTOM, (side, RAIL_D, -0.93))):
            start = rail[_nearest(rail, bag_point(corner))]
            guides = [start + Vector((side * x, y, z)) for x, y, z in shape]
            points = forms.smooth(forms.resample(guides, 0.016), 2)
            radius = [TENDRIL_R * (1.0 - 0.8 * i / (len(points) - 1)) for i in range(len(points))]
            forms.tube(f"tendril_{side}_{shape is TENDRIL_TOP}", points, radius, kit["root"], 5)


def _vine(kit, rail, period=0.11, low=1.25, high=1.47):
    """A thin root winding round the wearer's left rail over the middle of its height."""
    part = [p for p in rail if low <= p.z <= high]
    frames = forms.frames(part)
    along, points = 0.0, []
    for i, (point, (_, n, b)) in enumerate(zip(part, frames)):
        along += (point - part[i - 1]).length if i else 0.0
        angle = 2 * math.pi * along / period
        points.append(point + (n * math.cos(angle) + b * math.sin(angle)) * (RAIL_R * 1.05 + VINE_R * 0.5))
    points = forms.smooth(forms.resample(points, 0.02), 1)
    forms.tube("vine", points, radii(points, VINE_R, 0.04, 0.1), kit["root"], 5, 0.0, 0.0)


# --- iron ------------------------------------------------------------------------------------------------------

def _bands(kit, rails):
    """An iron band with a rivet round each rail just inside each corner, where the bars meet it."""
    for side, rail in rails.items():
        for h in (0.78, -0.8):
            i = _nearest(rail, bag_point((side, RAIL_D, h)))
            axis = rail[min(i + 1, len(rail) - 1)] - rail[max(i - 1, 0)]
            radius = RAIL_R * 1.42
            forms.band(f"band_{side}_{h}", rail[i], axis, radius, 0.028, kit["iron"])
            out = outward(rail[i])
            out = (out - axis.normalized() * out.dot(axis.normalized())).normalized()
            forms.rivet(f"band_rivet_{side}_{h}", rail[i] + out * radius, out, 0.0055, 0.0035, kit["iron"])


def _pull_ring(kit, skin, edge):
    """An iron ring hanging from a rivet at the middle of the flap's edge."""
    middle = edge[len(edge) // 2]
    hit, normal = land(skin, middle + Vector((0.0, 0.0, 0.012)))
    forms.rivet("pull_rivet", hit, normal, 0.0065, 0.0045, kit["iron"])
    down = (Vector((0.0, 0.0, -1.0)) - normal * -normal.z).normalized()
    across = down.cross(normal).normalized()
    centre = hit + down * 0.019 + normal * 0.006
    forms.ring("pull_ring", centre, (across, down), (0.016, 0.016), 0.0034, kit["iron"], 8, 4)


# --- shoulder straps -------------------------------------------------------------------------------------------

def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))


def _shoulder_straps(kit, reach=8):
    """Short straps from inside the bag's top over each shoulder, ending just past its top, each through an iron
    loop where it leaves the bag."""
    for side in (-1, 1):
        left, right = [], []
        for k in range(reach):
            left.append(Vector((side * 0.08, *SHOULDER_IN[k])) + _out(15 + 15 * k) * 0.005)
            right.append(Vector((side * 0.13, *SHOULDER_OUT[k])) + _out(15 + 15 * k) * 0.005)
        end = _out(15 + 15 * reach) * 0.005
        left.append(left[-1].lerp(Vector((side * 0.08, *SHOULDER_IN[reach])) + end, 0.45))
        right.append(right[-1].lerp(Vector((side * 0.13, *SHOULDER_OUT[reach])) + end, 0.45))
        normals = []
        for i in range(len(left)):
            along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
            normal = (right[i] - left[i]).cross(along).normalized()
            normals.append(normal if normal.dot(_out(15 + 15 * min(i, reach))) > 0 else -normal)
        forms.strap(f"shoulder_strap_{side}", left, right, normals, 0.007, kit["strap"])
        _strap_loop(kit, side, left[2], right[2], normals[2])


def _strap_loop(kit, side, left, right, normal):
    across = (right - left).normalized()
    centre = (left + right) / 2 + normal * 0.0035
    forms.ring(f"strap_loop_{side}", centre, (across, normal), (0.033, 0.0105), 0.0035, kit["iron"], 8, 4)

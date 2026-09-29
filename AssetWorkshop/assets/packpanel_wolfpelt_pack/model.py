"""PackPanel's Wolfpelt Pack, the Mountains backpack, worn on the player's upper back: a soft bag of dark grey leather
with stitched seams and scuffed edges under a big shaggy grey-white wolf fur mantle draped over its top and down its
back (fur.py: layers of broad, rounded fur locks laid like shingles, a darker saddle down the middle, a ragged hem
of soft tufts), two brown-black straps down the back and under the bag with silver buckles,
round silver clasps pinning the fur's hem to the straps, a wolf fang charm on a thong hanging from the wearer's
left buckle, and two short shoulder straps over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. See fit.py for the check on the
game's body.

The axes are the player's, not the bone's: in Player.prefab, Spine2 at rest leans back about 5.66 degrees (a local
rotation of -5.66 about Unity X relative to the player) and carries a world scale of 95 (Armature 100 x Visual 0.95).
Parented straight under the bone, the pack needs a local rotation of about +5.66 degrees about X and a local scale
of 1/95 to sit as modelled - the same as the Trollhide Backpack.

The bag's front follows the body's back surface (measured on the player's body in its rest pose, in SKIN below)
and presses 6 mm into it, so there is no gap and nothing shows through the chest or the sides of the torso; towards
the waist, where the torso narrows, the front curls forward at most CURL. The fur stays below about 1.64 m in the
player's frame, under the base of the neck and the tops of the shoulders.
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import forms  # noqa: E402
import fur  # noqa: E402
import looks  # noqa: E402
import trinkets  # noqa: E402

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.5

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
BOTTOM, TOP = 1.16, 1.585               # the bag's height range
PRESS = 0.006                           # how far the bag's front sinks into the back
SEAM_X, SEAM_Z = 0.132, 1.215           # the leather's side seams (|x|) and bottom seam (z)
STRAP_X, STRAP_W, STRAP_T = 0.092, 0.032, 0.005
BUCKLE_Z, CLASP_Z = 1.245, 1.405        # heights of the buckles and of the clasps on the fur's hem

CURL = 0.060                            # how far the bag's front may curl forward of the back's centre line

# The player's back in the rest pose, measured from behind: y of the skin at |x| = SKIN_X for each height, None
# where that column misses the body (the waist is narrower than the bag).
SKIN_X = (0.0, 0.05, 0.10, 0.15, 0.18, 0.20)
SKIN = [(1.10, (0.145, 0.137, 0.118, 0.072, None, None)), (1.14, (0.142, 0.133, 0.113, 0.072, None, None)),
        (1.18, (0.142, 0.131, 0.113, 0.077, None, None)), (1.22, (0.144, 0.137, 0.119, 0.079, None, None)),
        (1.26, (0.151, 0.147, 0.134, 0.095, 0.033, None)), (1.30, (0.164, 0.163, 0.152, 0.126, 0.069, None)),
        (1.34, (0.176, 0.176, 0.167, 0.148, 0.117, 0.068)), (1.38, (0.187, 0.187, 0.182, 0.162, 0.148, 0.106)),
        (1.42, (0.196, 0.198, 0.193, 0.177, 0.161, 0.143)), (1.46, (0.206, 0.207, 0.203, 0.191, 0.174, 0.159)),
        (1.50, (0.199, 0.200, 0.197, 0.184, 0.173, 0.161)), (1.54, (0.193, 0.193, 0.190, 0.176, 0.164, 0.155)),
        (1.58, (0.186, 0.186, 0.182, 0.167, 0.155, 0.146)), (1.62, (0.161, 0.161, 0.151, 0.116, 0.094, 0.076))]
# The shoulder's skin (y, z) at x = 0.08 and x = 0.13, every 15 degrees from 15 (behind) to 150 (in front), measured
# around (0.04, 1.48) in the side view: the rails the shoulder straps follow.
SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]


def build():
    random.seed(23)
    kit = {"leather": looks.leather(PIVOT, SEAM_X, SEAM_Z), "fur": looks.fur(PIVOT),
           "strap": looks.strap_leather(PIVOT), "silver": looks.silver(PIVOT), "ivory": looks.ivory(PIVOT)}
    bag = _bag(kit["leather"])
    bag_tree = forms.surface([bag])
    mantle = fur.Mantle(bag_tree)
    pelt = mantle.layers(kit["fur"])
    hooks = _back_straps(kit, bag_tree)
    _clasps(kit, forms.surface([bag] + pelt), bag_tree)
    trinkets.fang_charm(kit, *hooks[1])
    _shoulder_straps(kit["strap"])
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


# --- the bag ---------------------------------------------------------------------------------------------------

def skin(x, z):
    """y of the player's back at (x, z), bilinear in SKIN; where the body is missing, far forward."""
    x = min(abs(x), SKIN_X[-1])
    z = min(max(z, SKIN[0][0]), SKIN[-1][0])
    i = next(i for i in range(len(SKIN) - 1) if z <= SKIN[i + 1][0])
    j = next(j for j in range(len(SKIN_X) - 1) if x <= SKIN_X[j + 1])
    tz = (z - SKIN[i][0]) / (SKIN[i + 1][0] - SKIN[i][0])
    tx = (x - SKIN_X[j]) / (SKIN_X[j + 1] - SKIN_X[j])
    rows = [[-1.0 if y is None else y for y in SKIN[k][1][j:j + 2]] for k in (i, i + 1)]
    low = rows[0][0] + (rows[0][1] - rows[0][0]) * tx
    high = rows[1][0] + (rows[1][1] - rows[1][0]) * tx
    return low + (high - low) * tz


def front_y(x, z):
    """The bag's front: on the skin, curling forward towards its sides at most CURL, pressed in a little."""
    return max(skin(x, z), skin(0.0, z) - CURL) - PRESS


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: a rounded, upright sack, a touch narrower at the top,
    fullest a little below the middle, the bottom sagging."""
    a, d, h = forms.rounded(param, 4.5)
    v = (h + 1) / 2
    x = a * (0.186 - 0.018 * v * v)
    z = BOTTOM + v * (TOP - BOTTOM) - 0.014 * (1 - v) ** 3 * (1 - a * a)
    front = front_y(x, z)
    back = 0.300 + 0.042 * math.sin(math.pi * (1 - v) * 0.85) + 0.010 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((8, 4, 8))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 9.0) * 0.004       # soft, uneven leather
    forms.point_values(bag, "wear", [forms.edge_wear(p) for p in params])
    return bag


# --- straps ------------------------------------------------------------------------------------------------------

def _back_straps(kit, tree):
    """Two straps from under the fur down the back and under the bag, each with a silver buckle. Returns, per
    strap, the buckle's hook (the bottom of its frame) and the surface normal there."""
    hooks = []
    for side in (-1, 1):
        left, right, normals = _probe_strap(tree, side * STRAP_X)
        forms.strap(f"back_strap_{side}", left, right, normals, STRAP_T, kit["strap"])
        at = min(range(1, len(left) - 1), key=lambda i: abs(left[i].z - BUCKLE_Z))
        centre, along, normal = trinkets.buckle(f"buckle_{side}", left[at], right[at], left[at + 1] - left[at - 1],
                                                normals[at], kit["silver"], STRAP_T)
        hooks.append((centre + along * 0.017, normal))
    return hooks


def _probe_strap(tree, x):
    """The strap's two rails on the bag, probed around an axis along X behind the bag, from under the fur to under
    the bag's bottom."""
    left, right, normals = [], [], []
    for step in range(24):
        angle = math.radians(24 - step * 6.5)
        out = Vector((0.0, math.cos(angle), math.sin(angle)))
        hits = [forms.probe(tree, Vector((x + dx, 0.235, 1.33)) + out, -out) for dx in (-STRAP_W / 2, STRAP_W / 2)]
        if hits[0][0] is None or hits[1][0] is None or min(hits[0][0].y, hits[1][0].y) < 0.19:
            break
        normal = (hits[0][1] + hits[1][1]).normalized()
        left.append(hits[0][0] + normal * 0.0015)
        right.append(hits[1][0] + normal * 0.0015)
        normals.append(normal)
    return left, right, normals


def _clasps(kit, pelt_tree, bag_tree):
    """A round silver clasp where each strap goes in under the fur, sitting on whatever is outermost there."""
    batch = forms.Batch()
    for side in (-1, 1):
        at = Vector((side * STRAP_X, 0.30, CLASP_Z))
        normal = bag_tree.find_nearest(at + Vector((0.0, 0.2, 0.0)))[1]
        hit, _ = forms.probe(pelt_tree, at + normal * 0.5, -normal)
        trinkets.clasp(batch, hit + normal * 0.006, normal)
    batch.build("clasps", kit["silver"])


def _shoulder_straps(material, reach=8):
    """Short straps from under the fur over each shoulder, ending just past its top."""
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


def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))

"""PackPanel's Deerhide Satchel, the Meadows starter pack: a small, soft satchel of tan-brown deer hide with a paler
belly-hide underside, patched with darker leather scraps held on by crude sinew stitches, closed by a simple
rounded flap whose leather loop goes over a wooden toggle peg. Short shoulder straps of knotted leather scraps, and
a bundle of sticks tied to the wearer's left side. Humble and homemade.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. The same convention as the
Trollhide Backpack; see fit.py for the check on the game's body.

As for the Trollhide: parented straight under Spine2 (which leans back about 5.66 degrees at rest and carries a world
scale of 95), the pack needs a local rotation of about +5.66 degrees about X and a local scale of 1/95.

The bag sits between the shoulder blades, a little higher than the Trollhide's; its front follows the body's back
surface (BACK, measured on the player's body in its rest pose) and presses 6 mm into it.
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
import parts  # noqa: E402

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.55

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
BOTTOM, TOP = 1.29, 1.595               # the bag's height range
PRESS = 0.004                           # how far the bag's front sinks into the back
BELLY_Z = 1.335                         # where the tan hide gives way to the pale belly hide
FLAT = 0.08                             # the back is flat across to |x| = FLAT, then curves forward

# The player's back at x = 0 in the rest pose: (height, y of the skin), measured on the game's body.
BACK = [(1.00, 0.166), (1.04, 0.157), (1.08, 0.149), (1.12, 0.144), (1.16, 0.141), (1.20, 0.143), (1.24, 0.145),
        (1.26, 0.151), (1.28, 0.157), (1.30, 0.164), (1.32, 0.170), (1.34, 0.176), (1.36, 0.182), (1.38, 0.187),
        (1.40, 0.192), (1.42, 0.197), (1.44, 0.202), (1.46, 0.207), (1.48, 0.203), (1.50, 0.200), (1.52, 0.196),
        (1.54, 0.193), (1.56, 0.189), (1.58, 0.186), (1.60, 0.175), (1.62, 0.161), (1.64, 0.148)]
# The flap's course in the side view (y, z), outside the bag: it is laid on the bag's nearest surface.
FLAP_PATH = [(0.170, 1.622), (0.235, 1.640), (0.300, 1.628), (0.345, 1.580), (0.360, 1.52), (0.360, 1.20)]
FLAP_HALF = 0.150                       # half the flap's width
FLAP_TIP = 1.415                        # the height of the tongue's tip
COLUMNS, ROWS = 11, 10


def build():
    random.seed(11)
    kit = _kit()
    bag = _bag(kit["hide"])
    flap = _flap(kit["flap"], forms.surface([bag]))
    tree = forms.surface([bag, flap])
    parts.patches(kit, tree)
    parts.toggle(kit, forms.surface([bag]), tree, FLAP_TIP)
    parts.shoulder_straps(kit)
    parts.sticks(kit)
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


def _kit():
    return {"hide": looks.deer_hide(PIVOT, BELLY_Z), "flap": looks.flap_hide(PIVOT),
            "patch_dark": looks.patch_leather(PIVOT, "dark"), "patch_red": looks.patch_leather(PIVOT, "red"),
            "patch_grey": looks.patch_leather(PIVOT, "grey"), "thread": looks.thread(PIVOT),
            "strap_dark": looks.scrap_strap(PIVOT, "dark"), "strap_tan": looks.scrap_strap(PIVOT, "tan"),
            "wood": looks.wood(PIVOT), "bark": looks.bark(PIVOT), "cut": looks.cut_wood(PIVOT)}


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
    """The bag's front: on the back, flat across the shoulder blades, curling forward beyond them (more low down,
    where the back narrows towards the waist), as measured on the body."""
    bend = 5.0 + 5.0 * min(1.0, max(0.0, (1.40 - z) / 0.10))
    return back_line(z) - bend * max(0.0, abs(x) - FLAT) ** 2 - PRESS


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: a soft pillow, a little wider and fuller low down, the
    bottom sagging in the middle. The depth is eased towards the front, so the side pressed on the back stays flat
    and flush almost to its edges while the outer side keeps its round."""
    a, d, h = forms.rounded(param, 4.0)
    v = (h + 1) / 2
    x = a * (0.141 + 0.011 * (1 - v))
    z = BOTTOM + v * (TOP - BOTTOM) - 0.012 * (1 - v) ** 3 * (1 - a * a)
    front = front_y(x, z)
    depth = (0.090 + 0.045 * math.sin(math.pi * (0.15 + 0.7 * (1 - v)))) * (1 - 0.12 * a * a)
    back = back_line(z) - PRESS + depth
    return Vector((x, front + ((d + 1) / 2) ** 1.4 * (back - front), z))


def _bag(material):
    params, faces = forms.box_grid((9, 4, 9))
    bag = forms.mesh_object("bag", [bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 11.0) * 0.0035       # soft, uneven hide
    forms.point_values(bag, "wear", [forms.edge_wear(p) for p in params])
    return bag


# --- the flap --------------------------------------------------------------------------------------------------

def _path(s):
    """(y, z) in the side view at arc length s along FLAP_PATH."""
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if s <= length or (y1, z1) == FLAP_PATH[-1]:
            t = s / length
            return y0 + (y1 - y0) * t, z0 + (z1 - z0) * t
        s -= length


def _arc_to(z):
    """Arc length along FLAP_PATH to where it first comes down to height z."""
    s = 0.0
    for (y0, z0), (y1, z1) in zip(FLAP_PATH, FLAP_PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if z1 < z0 and z1 <= z <= z0:
            return s + length * (z0 - z) / (z0 - z1)
        s += length
    return s


def tip_z(u):
    """Where column u (-1 to 1) of the flap ends: a rounded tongue, lowest in the middle."""
    return FLAP_TIP + 0.065 * u * u + 0.03 * u ** 6


def _flap_vertex(bag_tree, u, s, hang):
    y, z = _path(s)
    hit, normal, _, _ = bag_tree.find_nearest(Vector((u * FLAP_HALF, y, z)))
    return hit + normal * (0.0055 + 0.004 * hang * hang + noise.noise(hit * 16.0) * 0.0015)


def _flap(material, bag_tree):
    """The flap, from the bag's top front edge over the top and down the back, one column per u."""
    verts, wear = [], []
    corner = _arc_to(1.57)
    for i in range(COLUMNS):
        u = -1.0 + 2.0 * i / (COLUMNS - 1)
        end = _arc_to(tip_z(u) + random.uniform(-0.004, 0.004))
        for j in range(ROWS):
            s = end * j / (ROWS - 1)
            jitter = random.uniform(-0.25, 0.25) / (COLUMNS - 1) if j == ROWS - 1 and 0 < i < COLUMNS - 1 else 0.0
            verts.append(_flap_vertex(bag_tree, u + jitter, s, max(0.0, s - corner) / max(end - corner, 1e-3)))
            wear.append(0.75 if i in (0, COLUMNS - 1) or j == ROWS - 1 else 0.0)
    faces = [(i * ROWS + j, (i + 1) * ROWS + j, (i + 1) * ROWS + j + 1, i * ROWS + j + 1)
             for i in range(COLUMNS - 1) for j in range(ROWS - 1)]
    flap = forms.mesh_object("flap", verts, faces, material)
    forms.point_values(flap, "wear", wear)
    forms.thicken(flap, 0.004)
    return flap


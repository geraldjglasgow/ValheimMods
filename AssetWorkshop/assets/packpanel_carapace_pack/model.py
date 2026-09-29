"""PackPanel's Carapace Pack, the Mistlands backpack, worn on the player's upper back. A soft bag of dark grey-blue
scale hide, armoured on its back like a beetle: a domed carapace shield over the top (its lower edge drawn to a point
at the middle, an eitr stone set in it) and two wing cases below, each of three glossy near-black purple plates
that overlap downwards, parted by a faint eitr-blue seam. A band of blue jute is tied round the bag under the shield,
and two short blue jute shoulder straps arch forward over the tops of the shoulders.

Pivot: the origin is the player's Spine2 bone in its rest pose, the bone the mod parents the pack to. The pack is
modelled in the player's own frame (feet at the origin, rendered size, Blender axes: Z up, the wearer faces -Y, so
the pack sits behind the wearer at +Y), where Spine2 is at Blender (0, +0.0433, 1.4524), which is Unity
(0, 1.4524, -0.0433). At the end every part is moved by (0, -0.0433, -1.4524), so that point becomes the origin.
Exported to Unity (Blender -Y becomes Unity +Z), the pack's local frame is Spine2's position in the player's frame
with the player's axes: +Z the wearer's forward, the bag at -Z behind the spine. This is the same convention as the
Trollhide Backpack; see fit.py for the check on the game's body.

The axes are the player's, not the bone's: in Player.prefab, Spine2 at rest leans back about 5.66 degrees (a local
rotation of -5.66 about Unity X relative to the player) and carries a world scale of 95 (Armature 100 x Visual 0.95).
Parented straight under the bone, the pack needs a local rotation of about +5.66 degrees about X and a local scale of
1/95 to sit as modelled.

The bag's front follows the body's back surface (measured on the player's body in its rest pose, in BACK below) and
presses 6 mm into it, so there is no gap and nothing shows through the chest or the sides of the torso. The shell
plates and the band are laid on the bag's own smooth surface (bag_point over an unfolded cube, see net()).
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import body  # noqa: E402
import forms  # noqa: E402
import looks  # noqa: E402
import shell  # noqa: E402

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.6

PIVOT = Vector((0.0, 0.0433, 1.4524))   # Spine2 at rest, player frame
SEAM_Y = 0.235                          # the eitr seam down each side of the bag
BAND_Z = (1.447, 1.488)                 # the jute band's lower and upper edge, at the back's centre
STRAP_W = 0.05

# The shoulder's skin (y, z) at x = 0.08 and x = 0.13, every 15 degrees from 15 (behind) to 150 (in front), measured
# around (0.04, 1.48) in the side view: the rails the shoulder straps follow.
SHOULDER_IN = [(0.197, 1.522), (0.189, 1.566), (0.168, 1.608), (0.135, 1.644), (0.090, 1.668), (0.040, 1.678),
               (-0.007, 1.656), (-0.040, 1.619), (-0.071, 1.591), (-0.094, 1.557)]
SHOULDER_OUT = [(0.186, 1.519), (0.178, 1.559), (0.161, 1.601), (0.124, 1.626), (0.085, 1.647), (0.040, 1.648),
                (0.000, 1.628), (-0.034, 1.608), (-0.061, 1.581), (-0.077, 1.548)]


def build():
    kit = {"hide": looks.scale_hide(PIVOT, SEAM_Y), "jute": looks.jute(PIVOT),
           "shield": looks.chitin(PIVOT, "pack_shield", grooves=False),
           "wing": looks.chitin(PIVOT, "pack_wing", grooves=True, glow_x=shell.GAP + 0.002)}
    _bag(kit["hide"])
    stone = shell.shield(kit["shield"])
    # Recipe uses shell and jute: the clasp is worn chitin, not a luminous gemstone.
    shell.stone(kit["shield"], *stone)
    shell.wings(kit["wing"])
    _band(kit["jute"])
    _shoulder_straps(kit["jute"])
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        obj.data.transform(Matrix.Translation(-PIVOT))       # Spine2 becomes the origin


# --- the bag ---------------------------------------------------------------------------------------------------

def _bag(material):
    params, faces = forms.box_grid((8, 4, 10))
    bag = forms.mesh_object("bag", [body.bag_point(p) for p in params], faces, material)
    normals = [v.normal.copy() for v in bag.data.vertices]
    for vert, normal in zip(bag.data.vertices, normals):
        vert.co += normal * noise.noise(vert.co * 8.0) * 0.003       # soft, uneven hide
    return bag


# --- straps ----------------------------------------------------------------------------------------------------

def _band(material, count=31, reach=1.85):
    """The jute band round the bag under the shield, from one front corner round the back to the other."""
    columns = [-(1 + reach) + 2 * (1 + reach) * i / (count - 1) for i in range(count)]
    points, normals = body.surface_grid(columns, lambda c: (body.row_for(c, BAND_Z[1]), body.row_for(c, BAND_Z[0])))
    lift = 0.007
    left = [p + n * lift for p, n in zip(points[0], normals[0])]
    right = [p + n * lift for p, n in zip(points[1], normals[1])]
    forms.strap("band", left, right, normals[0], 0.005, material)


def _shoulder_straps(material, reach=8):
    """Short straps from under the shield over each shoulder, ending just past its top."""
    for side in (-1, 1):
        left, right, normals = [], [], []
        for k in range(reach):
            out = _out(15 + 15 * k)
            inner, outer = SHOULDER_IN[k], SHOULDER_OUT[k]
            left.append(Vector((side * 0.08, inner[0], inner[1])) + out * 0.005)
            right.append(Vector((side * (0.08 + STRAP_W), outer[0], outer[1])) + out * 0.005)
        tip = 0.45
        end = _out(15 + 15 * reach) * 0.005
        left.append(left[-1].lerp(Vector((side * 0.08, *SHOULDER_IN[reach])) + end, tip))
        right.append(right[-1].lerp(Vector((side * (0.08 + STRAP_W), *SHOULDER_OUT[reach])) + end, tip))
        for i in range(len(left)):
            along = left[min(i + 1, len(left) - 1)] - left[max(i - 1, 0)]
            normal = (right[i] - left[i]).cross(along).normalized()
            normals.append(normal if normal.dot(_out(15 + 15 * min(i, reach))) > 0 else -normal)
        forms.strap(f"shoulder_strap_{side}", left, right, normals, 0.007, material)


def _out(degrees):
    angle = math.radians(degrees)
    return Vector((0.0, math.cos(angle), math.sin(angle)))

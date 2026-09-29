"""The skeleton arsenal's bow (Elite Creatures Pack), all bone: two great ribs for the limbs, their heads meeting at the
grip where they are bound together in dark hide, their natural bend giving the bow its shape (angular like the
skeleton's own, Characters/Skeleton/weapons/skeleton_bow); a vertebra threaded on above the grip for the arrow to ride
on; sinew lashings along the limbs; finger bones capping both tips for nocks. The string is not part of the model: the
bow's wielder draws it (the showcase stretches it from the tips to the drawing fingers).

Built upright (the top tip up -Y, the string side +X, the flats facing Z), then turned into the game's hold: the
skeletons hold their bow slanted in the left fist (skeleton_bow's attach frame: the stave through the fist, its string
0.35 m behind the grip along STRING, the limbs along LIMBS; measured from the game's mesh, Blender axes). The build
writes out/ecp_skel_bow_points.json: the tips, the rest (where the arrow passes the bow) and the string's middle at
rest, turned, in Blender and in Unity axes. About 1.34 m tip to tip.
"""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.6

STRING = Vector((0.751, -0.006, 0.659))         # from the grip towards the string's middle
LIMBS = Vector((0.492, -0.704, -0.512))         # along the stave
TOP = -1                                        # which way along LIMBS the top tip lies

# One rib, its head at the grip to its tip: (s, x, half width, half thickness); the other limb is its mirror.
LIMB = [(-0.035, -0.012, 0.024, 0.02), (0.11, -0.004, 0.025, 0.019), (0.33, 0.09, 0.022, 0.015), (0.585, 0.29, 0.016, 0.011),
        (0.645, 0.325, 0.012, 0.0085)]
TIP_S, TIP_X = 0.66, 0.34                       # the nock, where the string is tied
REST = (0.088, -0.02)                           # (s, x): the arrow passes over the vertebra's wing here
SHELF, SHELF_R = 0.088, 0.022


def build():
    bone, spine, sinew = gp.bone("bow_bone"), gp.vertebra("bow_vertebra"), gp.sinew()
    for side in (1, -1):
        bones.rib(f"limb_{side}", [((x, -side * s, 0.0), w, t) for s, x, w, t in LIMB], bone)
        gs.knob(f"nock_{side}", (TIP_X, -side * TIP_S, 0.0), 0.015, bone, squash=(1.0, 1.5, 1.0))
        gs.cone(f"nock_hook_{side}", (TIP_X - 0.004, -side * (TIP_S + 0.012), 0.0), (TIP_X + 0.018, -side * (TIP_S + 0.026), 0.0),
                0.006, bone, vertices=5)
        for s in (0.24, 0.47):
            gs.wrap(f"limb_lashing_{side}_{s}", side * s - 0.016, side * s + 0.016, 0.025, 0.0065, 0.003, sinew,
                    centre=(_limb_x(s), 0.0), squash=0.8)
    gs.wrap("grip", -0.075, 0.075, 0.0265, 0.012, 0.0038, gp.leather(), squash=0.85)
    gv.build("shelf", gv.frame(gs.at(SHELF, 0.0), (0, -1, 0), (1, 0, 0)), SHELF_R, spine, kind='thoracic')
    gs.rings("shelf_lashing", [SHELF + 0.028, SHELF + 0.035, SHELF - 0.028], 0.0245, 0.0034, sinew, squash=0.85)
    _points(_turn())


def _limb_x(s):
    """The limb's middle line across (x) at s along it."""
    for (a, xa, _, _), (b, xb, _, _) in zip(LIMB, LIMB[1:]):
        if a <= s <= b:
            return xa + (xb - xa) * (s - a) / (b - a)
    return LIMB[-1][1]


def _turn():
    """Upright (top -Y, string +X, flats Z) into the skeleton bow's hold."""
    back = STRING.normalized()
    top = (TOP * LIMBS - back * (TOP * LIMBS).dot(back)).normalized()
    flat = -back.cross(top)
    upright = Matrix((Vector((1, 0, 0)), Vector((0, -1, 0)), Vector((0, 0, 1)))).transposed()
    hold = Matrix((back, top, flat)).transposed()
    turn = (hold @ upright.inverted()).to_4x4()
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.matrix_world = turn @ obj.matrix_world
    return turn


def _points(turn):
    points = {"tip_top": (TIP_X, -TIP_S, 0.0), "tip_bottom": (TIP_X, TIP_S, 0.0), "rest": (REST[1], -REST[0], 0.0),
              "string_rest": (TIP_X, 0.0, 0.0)}
    blender = {k: list(turn @ Vector(v)) for k, v in points.items()}
    unity = {k: [-v[0], v[2], -v[1]] for k, v in blender.items()}
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "ecp_skel_bow_points.json"), "w", encoding="utf-8") as handle:
        json.dump({"blender": blender, "unity": unity}, handle, indent=2)

"""The skeleton arsenal's mace (Elite Creatures Pack), all bone: the haft is a long bone, knuckles for its foot; the head
is two great vertebrae off some beast, bored through their bodies and driven onto a bone core, their spines and wings the
flanges, three fangs set into each body's face and one great fang standing up out of a knuckle cap on top; sinew binds
the head on and dark hide the grip.

Weapon axes (see ecp_skel_arsenal): the fist round the grip at the origin, the haft up -Y (Unity +Z). The vertebrae's
bodies face -X (Unity +X) and their spines +X. About 1.08 m long and 0.25 m across the head, like MaceIron (1.03 m,
0.22 m) and the skeleton's own mace.
"""
import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

from mathutils import Vector  # noqa: E402

import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.7

HEAD = (0.705, 0.795)   # the two vertebrae's bodies, on the core
HEAD_R = 0.064


def build():
    bone, spine, teeth, sinew = gp.bone("mace_bone"), gp.vertebra("mace_vertebra"), gp.teeth("mace_teeth"), gp.sinew()
    bones.long_bone("haft", -0.13, 0.64, 0.021, bone, bottom=bones.KNUCKLE, top=None)
    gs.loft("core", [(0.6, 0.019, 0.019), (0.83, 0.017, 0.017)], bone, sides=8)
    for i, s in enumerate(HEAD):
        gv.build(f"head_{i}", gv.frame(gs.at(s), (0, -1, 0), (1, 0, 0)), HEAD_R, spine, kind='beast')
        for j, angle in enumerate((-50, 0, 50)):
            _fang(f"fang_{i}_{j}", s, math.radians(angle), teeth)
    gs.knob("cap", gs.at(0.842), 0.03, bone, squash=(1.0, 0.8, 1.0))
    bones.fang("cap_fang", gs.at(0.85), gs.at(0.96), 0.02, teeth, bend=Vector((0.012, 0.0, 0.0)))
    gs.rings("head_lashing", [0.625, 0.633, 0.641, 0.649], 0.0235, 0.0038, sinew)
    gs.wrap("grip", -0.095, 0.095, 0.0222, 0.015, 0.0037, gp.leather())


def _fang(name, s, angle, teeth):
    """A fang set into a body's face at `s`, `angle` round from straight ahead (-X) towards Z, curving up the weapon."""
    out = Vector((-math.cos(angle), 0.0, math.sin(angle)))
    base = gs.at(s) + out * HEAD_R * 0.78
    return bones.fang(name, base, base + out * 0.048 + Vector((0.0, -0.012, 0.0)), 0.0145, teeth, bend=Vector((0.0, -0.008, 0.0)))

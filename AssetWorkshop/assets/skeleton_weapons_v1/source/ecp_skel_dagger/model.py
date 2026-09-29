"""The skeleton arsenal's dagger (Elite Creatures Pack), all bone: a seax-shaped blade ground out of a slab of bone, its
back straight and then broken down to the point, its edge ground pale and chipped once; a vertebra threaded on for the
guard - the wings are the quillons and the spine a thumb spur lying back along the grip - bound to the blade with sinew;
a finger bone for the handle, its knuckle the pommel, wound in dark hide.

Weapon axes (see ecp_skel_arsenal): the fist round the handle's middle at the origin, the blade up -Y (Unity +Z), the
edge towards +X, the flats facing Z. About 0.39 m long, like KnifeCopper (0.37 m).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import grave_blade as gb  # noqa: E402
import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.65

# (s, back x, edge x, half thickness at the back, bevel): the back straight, then broken down to the point. Thick: bone.
BLADE = [(0.074, -0.026, 0.028, 0.0095, 0.5), (0.18, -0.028, 0.034, 0.009, 0.5), (0.24, -0.008, 0.033, 0.0078, 0.55)]
TIP = (0.300, 0.028)
# The handle, a finger bone: (s, half width, half height).
HANDLE = [(-0.058, 0.019, 0.018), (-0.045, 0.0158, 0.0148), (0.0, 0.0148, 0.0138), (0.045, 0.0158, 0.0148), (0.058, 0.018, 0.017)]
GUARD, GUARD_R = 0.068, 0.022


def build():
    bone, spine, blade, sinew = gp.bone("dagger_bone"), gp.vertebra("dagger_vertebra"), gp.bone_blade("dagger_blade"), gp.sinew()
    gb.single("blade", gb.notch(BLADE, 0.13, 0.005, side=1, width=0.01), blade, tip=TIP)
    bones.pores("pore", [gs.at(0.11, 0.0, side * 0.0092) for side in (-1, 1)] + [gs.at(0.16, -0.012, 0.0088)], 0.0032, gp.dark())
    gs.loft("handle", HANDLE, bone, sides=8)
    gs.knob("knuckle", gs.at(-0.068), 0.021, bone, squash=(1.25, 0.8, 1.0))
    for side in (-1, 1):
        gs.knob(f"condyle_{side}", gs.at(-0.075, side * 0.013), 0.0135, bone)
    gs.wrap("grip", -0.040, 0.040, 0.0158, 0.011, 0.0036, gp.leather(), squash=0.93)
    gv.build("guard", gv.frame(gs.at(GUARD), (0, -1, 0), (0, 0, 1)), GUARD_R, spine, kind='lumbar')
    gs.wrap("binding", 0.052, 0.088, 0.028, 0.0065, 0.0028, sinew, centre=(0.0015, 0.0), squash=0.42)

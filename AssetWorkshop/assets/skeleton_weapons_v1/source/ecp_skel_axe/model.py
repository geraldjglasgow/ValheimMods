"""The skeleton arsenal's axe (Elite Creatures Pack), all bone: the haft is a long bone, knuckles for its foot and its
ball head above the blade; the blade is a shoulder blade, its socket cupped round the haft and its broad flat ground to
a pale bearded edge, the ridge of the scapula standing out across both faces; sinew lashes it on, and a vertebra is
slid up under it as a collar, its spine lying back towards the poll. Dark hide binds the grip.

Weapon axes (see ecp_skel_arsenal): the fist round the grip at the origin, the haft up -Y (Unity +Z), the blade out to
-X (Unity +X, as AxeIron's), its flats facing Z. 0.9 m long, the blade 0.24 m out from the haft, like AxeIron.
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

EYE = 0.640                     # where the shoulder blade's socket cups the haft
# The scapula out from the socket along -X: (u, v low, v high, half thickness), v along the haft (+ up the weapon).
BLADE = [(0.012, -0.034, 0.034, 0.017), (0.045, -0.028, 0.03, 0.0125), (0.095, -0.07, 0.05, 0.0092),
         (0.155, -0.125, 0.064, 0.0068), (0.205, -0.155, 0.074, 0.0045), (0.218, -0.16, 0.077, 0.0012)]
COLLAR, COLLAR_R = 0.535, 0.03


def build():
    bone, spine, blade, sinew = gp.bone("axe_bone"), gp.vertebra("axe_vertebra"), gp.bone_blade("axe_blade"), gp.sinew()
    bones.long_bone("haft", -0.15, 0.715, 0.02, bone, bottom=bones.KNUCKLE, top=bones.BALL)
    gb.slab("blade", gs.at(EYE, -0.016), (-1, 0, 0), BLADE, blade)
    gs.knob("socket", gs.at(EYE, -0.012), 0.028, bone, squash=(0.8, 1.35, 0.95))
    for side in (-1, 1):
        gs.band(f"ridge_{side}", [(gs.at(EYE + 0.012, -0.04, side * 0.011), 0.006, 0.005),
                                  (gs.at(EYE + 0.05, -0.12, side * 0.008), 0.0055, 0.0042),
                                  (gs.at(EYE + 0.068, -0.19, side * 0.0058), 0.004, 0.003)], bone, sides=5)
    bones.pores("pore", [gs.at(EYE - 0.03, -0.11, side * 0.0085) for side in (-1, 1)], 0.0042, gp.dark())
    gs.wrap("lashing", EYE - 0.05, EYE + 0.045, 0.03, 0.009, 0.0032, sinew, centre=(-0.01, 0.0), squash=0.8)
    gv.build("collar", gv.frame(gs.at(COLLAR), (0, -1, 0), (1, 0, 0)), COLLAR_R, spine, kind='thoracic')
    gs.rings("collar_lashing", [COLLAR + 0.033, COLLAR + 0.04, COLLAR - 0.036], 0.0215, 0.0035, sinew)
    gs.wrap("grip", -0.12, 0.09, 0.0205, 0.016, 0.0038, gp.leather(), squash=0.95)

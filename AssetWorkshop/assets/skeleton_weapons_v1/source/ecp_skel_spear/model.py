"""The skeleton arsenal's spear (Elite Creatures Pack), all bone: the shaft is three long bones laid end to end, each joint
bound in sinew; the head is a broad leaf of bone ground to a pale edge with a ridge down its middle, lashed onto the
shaft's end; right under it a vertebra is slid up the shaft and lashed there, its wings standing out like the lugs of
the game's bronze spear; the bottom bone's knuckles are the butt, and dark hide binds the grip.

Weapon axes (see ecp_skel_arsenal), but reversed like the game's own spears: the fist at the origin, the head down +Y
(Unity -Z) 1.08 m away and the butt 1.35 m up -Y, since the spear clips hold it overhand (SpearBronze and SpearFlint
both). Built head up -Y first and turned half round Z at the end. The blade's flats face Z. 2.43 m long.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import bpy  # noqa: E402
from mathutils import Matrix  # noqa: E402

import grave_blade as gb  # noqa: E402
import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.65

JOINTS = (-0.55, 0.25)          # where one bone of the shaft meets the next
# (s, left x, right x, half thickness, bevel, ridge): a leaf, widest low, the ridge standing proud.
BLADE = [(0.77, -0.02, 0.02, 0.0105, 0.4, 1.3), (0.84, -0.045, 0.045, 0.0098, 0.42, 1.45), (0.93, -0.042, 0.042, 0.009, 0.44, 1.5),
         (1.01, -0.023, 0.023, 0.0075, 0.48, 1.4)]
TIP = (1.08, 0.0)
COLLAR, COLLAR_R = 0.668, 0.029


def build():
    bone, spine, blade, sinew = gp.bone("spear_bone"), gp.vertebra("spear_vertebra"), gp.bone_blade("spear_blade"), gp.sinew()
    bones.jointed_haft("shaft", -1.345, 0.8, JOINTS, 0.0185, bone, sinew)
    gb.blade("blade", gb.notch(BLADE, 0.89, 0.005, side=-1, width=0.012), blade, tip=TIP)
    bones.pores("pore", [gs.at(0.86, 0.018, side * 0.0115) for side in (-1, 1)], 0.0035, gp.dark())
    gs.wrap("head_lashing", 0.705, 0.79, 0.0225, 0.0075, 0.0028, sinew)
    gv.build("collar", gv.frame(gs.at(COLLAR), (0, -1, 0), (0, 0, 1)), COLLAR_R, spine, kind='thoracic')
    gs.rings("collar_lashing", [COLLAR - 0.036, COLLAR - 0.043, COLLAR - 0.05], 0.0205, 0.0034, sinew)
    gs.wrap("grip", -0.12, 0.12, 0.0195, 0.016, 0.0036, gp.leather())
    half_turn = Matrix.Rotation(3.14159265, 4, 'Z')
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.matrix_world = half_turn @ obj.matrix_world

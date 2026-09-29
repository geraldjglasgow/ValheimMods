"""The skeleton arsenal's atgeir (Elite Creatures Pack), all bone: the shaft is three long bones laid end to end, each
joint bound in sinew; the head is a great rib ground into a long single-edged blade, its natural curve sweeping the point
back, a fang curving up off its back for the hook; below the head a short column of three vertebrae threaded on the
shaft, spines all lying down the back like a living spine; a fang for a foot and dark hide where the fist holds.

Built along the weapon (-Y) and then turned into the game's hold: the game's atgeirs (AtgeirIron) lie in the fist at a
slant, their haft through the fist but 21 degrees off the attach frame's axis, so the two-handed clips put the other
hand on the haft. HOLD is that haft direction and BLADE_SIDE the way the blade's edge faces, measured from AtgeirIron's
mesh in its attach frame (Blender axes). About 3.0 m long with the fist 0.76 m from the foot, like AtgeirIron.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import grave_blade as gb  # noqa: E402
import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.65

HOLD = Vector((-0.355, -0.924, -0.14))          # up the haft, towards the blade
BLADE_SIDE = Vector((-0.925, 0.325, 0.198))     # across the blade, towards its edge

JOINTS = (0.35, 1.08)
# (s, back x, edge x, half thickness at the back, bevel): a rib's curve, the back sweeping to +X, the edge towards -X.
BLADE = [(1.84, 0.018, -0.026, 0.012, 0.5), (1.95, 0.02, -0.046, 0.0112, 0.5), (2.06, 0.024, -0.05, 0.0102, 0.5),
         (2.17, 0.032, -0.038, 0.0088, 0.52)]
TIP = (2.28, 0.05)
SPINE = [1.60, 1.665, 1.73]                     # the vertebrae up the shaft
SPINE_R = 0.028


def build():
    bone, spine, blade, teeth, sinew = gp.bone("atgeir_bone"), gp.vertebra("atgeir_vertebra"), gp.bone_blade("atgeir_blade"), \
        gp.teeth("atgeir_teeth"), gp.sinew()
    bones.jointed_haft("shaft", -0.68, 1.86, JOINTS, 0.0195, bone, sinew)
    gb.single("blade", gb.notch(BLADE, 2.0, 0.006, side=1, width=0.012), blade, tip=TIP)
    bones.pores("pore", [gs.at(1.97, 0.0, side * 0.0115) for side in (-1, 1)], 0.0036, gp.dark())
    bones.fang("hook", gs.at(1.9, 0.02), gs.at(2.02, 0.11), 0.017, teeth, bend=Vector((0.03, 0.0, 0.0)))
    gs.wrap("head_lashing", 1.78, 1.9, 0.024, 0.008, 0.003, sinew)
    for i, s in enumerate(SPINE):
        gv.build(f"spine_{i}", gv.frame(gs.at(s), (0, -1, 0), (1, 0, 0)), SPINE_R, spine, kind='thoracic')
    gs.rings("spine_lashing", [SPINE[0] - 0.04, SPINE[0] - 0.047, SPINE[-1] + 0.035, SPINE[-1] + 0.042], 0.0215, 0.0034, sinew)
    bones.fang("foot", gs.at(-0.69), gs.at(-0.79), 0.019, teeth)
    gs.wrap("grip", -0.11, 0.11, 0.0205, 0.016, 0.0036, gp.leather())
    _turn()


def _turn():
    """Canonical (haft along -Y, edge towards -X, flats facing Z) into the game's hold."""
    up = HOLD.normalized()
    edge = (BLADE_SIDE - up * BLADE_SIDE.dot(up)).normalized()
    flat = edge.cross(up)
    canonical = Matrix((Vector((0, -1, 0)), Vector((-1, 0, 0)), Vector((0, 0, 1)))).transposed()
    hold = Matrix((up, edge, flat)).transposed()
    turn = (hold @ canonical.inverted()).to_4x4()
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.matrix_world = turn @ obj.matrix_world

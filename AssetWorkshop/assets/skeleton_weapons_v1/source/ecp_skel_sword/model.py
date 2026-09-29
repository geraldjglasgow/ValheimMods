"""The skeleton arsenal's sword (Elite Creatures Pack), all bone: a broad straight blade ground out of a great flat bone,
a ridge down its middle, its edges ground pale with a couple of chips out of them and dark pores in its face; for the
guard a rib laid crosswise and lashed on with sinew, its ends curving up towards the blade; for the grip three
vertebrae threaded on a bone core, sinew between them; for the pommel the knuckle end of a long bone.

Weapon axes (see ecp_skel_arsenal): the fist round the grip's middle at the origin, the blade up -Y (Unity +Z), its flats
facing Z. About 1.07 m from pommel to point, like the skeleton's own sword (0.97 m past the grip) and SwordIron (1.18 m).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

from mathutils import Vector  # noqa: E402

import grave_blade as gb  # noqa: E402
import grave_bones as bones  # noqa: E402
import grave_paint as gp  # noqa: E402
import grave_shapes as gs  # noqa: E402
import grave_vertebra as gv  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.65

# (s, left x, right x, half thickness, bevel, ridge): broad at the guard, a raised ridge down the middle, a rounded point.
BLADE = [(0.080, -0.048, 0.048, 0.011, 0.34, 1.25), (0.22, -0.047, 0.047, 0.0105, 0.34, 1.25),
         (0.52, -0.043, 0.043, 0.0095, 0.36, 1.25), (0.78, -0.037, 0.037, 0.0085, 0.38, 1.2),
         (0.91, -0.025, 0.025, 0.007, 0.42, 1.15)]
TIP = (0.985, 0.0)
GRIP = [-0.033, 0.0, 0.033]
VERTEBRA_R = 0.0205
GUARD = 0.072


def build():
    bone, spine, blade, sinew = gp.bone("sword_bone"), gp.vertebra("sword_vertebra"), gp.bone_blade("sword_blade"), gp.sinew()
    gb.blade("blade", gb.notch(gb.notch(BLADE, 0.43, 0.007, side=1), 0.67, 0.006, side=-1), blade, tip=TIP)
    bones.pores("pore", [gs.at(s, x, side * 0.0135) for s, x in ((0.2, 0.012), (0.47, -0.01), (0.7, 0.008)) for side in (-1, 1)],
                0.0038, gp.dark())
    _guard(bone, sinew)
    gs.loft("core", [(-0.058, 0.0068, 0.0068), (0.06, 0.0078, 0.0078)], bone, sides=6)
    for i, s in enumerate(GRIP):
        gv.build(f"grip_{i}", gv.frame(gs.at(s), (0, -1, 0), (1, 0, 0)), VERTEBRA_R, spine, kind='grip')
    for s in (-0.0165, 0.0165):
        gs.rings(f"lashing_{s}", [s - 0.004, s + 0.004], 0.0135, 0.0035, sinew)
    bones.long_bone("pommel", -0.075, -0.05, 0.02, bone, bottom=bones.KNUCKLE, top=None)


def _guard(bone, sinew):
    """A rib laid across the blade's foot, curving up at both ends, lashed on with sinew crossing over the blade."""
    points = []
    for i in range(7):
        x = -0.11 + 0.22 * i / 6
        points.append((gs.at(GUARD + 0.02 * (x / 0.11) ** 2, x), 0.012, 0.0095))
    gs.band("guard", points, bone, sides=6, up=Vector((0, -1, 0)))
    for side in (-1, 1):
        gs.knob(f"guard_end_{side}", gs.at(GUARD + 0.021, side * 0.112), 0.0115, bone)
    gs.wrap("guard_lashing", GUARD - 0.012, GUARD + 0.02, 0.05, 0.007, 0.0028, sinew, squash=0.3)

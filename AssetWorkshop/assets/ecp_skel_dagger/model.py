"""The skeleton arsenal's dagger (Elite Creatures Pack), v2: a dagger grown out of a spine. The grip is the end of a
tail - three tail vertebrae on a bone core, smallest at the bottom, sinew between them, the core tapering on past
them to a point for a pommel; the guard is a vertebra lying flat in the blade's plane, its wings the quillons and the
canal showing through; and the blade is that vertebra's spine, drawn out and ground to a double edge.

Weapon axes (see ecp_skel_arsenal): the fist round the grip's middle at the origin, the blade up -Y (Unity +Z), the
flats facing Z. About 0.39 m long, like KnifeCopper (0.37 m). v1 (a bone seax with a vertebra guard) is archived in
assets/skeleton_weapons_v1.
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

TAIL = [(-0.052, 0.0135), (-0.021, 0.0155), (0.01, 0.0175)]     # (s, r): the grip's tail vertebrae, bottom to top
GUARD_FRONT, GUARD_R = 0.019, 0.03
# (s from the spine's root, half width, half thickness, bevel, ridge): narrow and thick where it leaves the arch.
BLADE = [(0.0, 0.011, 0.0105, 0.25, 1.15), (0.028, 0.021, 0.0095, 0.36, 1.3), (0.1, 0.027, 0.0082, 0.42, 1.35),
         (0.165, 0.018, 0.0068, 0.46, 1.3)]
LENGTH = 0.30                                                   # the point


def build():
    bone, spine, blade, sinew = gp.bone("dagger_bone"), gp.vertebra("dagger_vertebra"), gp.bone_blade("dagger_blade"), gp.sinew()
    gs.loft("core", [(-0.066, 0.0055, 0.0055), (0.03, 0.0072, 0.0072)], bone, sides=6)
    bones.fang("tail_tip", gs.at(-0.062), gs.at(-0.092), 0.0085, bone)
    for i, (s, r) in enumerate(TAIL):
        gv.build(f"grip_{i}", gv.frame(gs.at(s), (0, -1, 0), (1, 0, 0)), r, spine, kind='tail')
    for s in (-0.0365, -0.0055):
        gs.rings(f"grip_sinew_{s}", [s - 0.0025, s + 0.0025], 0.0085, 0.0028, sinew)
    _, root = gv.flat("guard", GUARD_FRONT, GUARD_R, spine)
    sections = [(root + ds, -w, w, t, bevel, ridge) for ds, w, t, bevel, ridge in BLADE]
    gb.blade("blade", gb.notch(sections, root + 0.07, 0.004, side=1, width=0.009), blade, tip=(LENGTH, 0.0))
    bones.pores("pore", [gs.at(root + 0.06, 0.004, side * 0.0092) for side in (-1, 1)], 0.003, gp.dark())

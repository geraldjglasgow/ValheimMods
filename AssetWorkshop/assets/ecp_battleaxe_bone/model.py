"""Bone battleaxe: a two-handed axe made the way the game makes its bone gear. A giant's leg bone for the haft, a flat
bone ground into a bearded crescent for the blade, rawhide lashing, a tusk for the back spike, a leather grip.

Sized and budgeted like the game's own battleaxes (Battleaxe, BattleaxeSkullSplittur: 1.70 to 1.79 m, a 0.4 to 0.7 m
head, 600 to 1500 triangles, 64 to 128 px textures): 1.79 m, head 0.76 m across with the spike. Z up, the blade
towards +X, thin along Y. The origin is the lower hand, 0.28 m above the butt, where the game's battleaxes put their
`attach` point.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import blade  # noqa: E402
import haft  # noqa: E402
import paint  # noqa: E402

TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.5


def build():
    bone = paint.bone()
    blade.build(paint.blade(), paint.ground_edge())
    haft.build(bone, paint.leather(), paint.rawhide(), paint.ivory())

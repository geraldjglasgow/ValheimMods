"""Deathsquito Queen: the game's Deathsquito grown 2.5 times, gravid and crowned (BRIEF.md). A 4.5 m wingspan of
old notched wings, a sagging abdomen of six overlapping plates glowing blood red underneath, a crown of five amber
spikes, banded spider legs, a needle a metre long and a stinger.

Built to its category (creature.flyer: 812 to 17,000 triangles, median 3,739; 64 to 512 px; 25 to 78 px/m) on one
256 px atlas, painted with the codex recipes in the game Deathsquito's own tones. Laid out on the Deathsquito's parts
at 2.5 times, in its bind pose (wings spread, legs hanging), so each part can later ride one bone of the
Deathsquito's skeleton scaled 2.5 (BRIEF.md section 9). Z up, front -Y, the root on the ground under the thorax.

    .\build.ps1 -Asset ecp_deathsquito_queen -Lineup
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import queen_body  # noqa: E402
import queen_bones  # noqa: E402
import queen_limbs  # noqa: E402
import queen_paint  # noqa: E402

CATEGORY = "creature.flyer"
TEXTURE_SIZE = 256
AO_STRENGTH = 0.2          # the paint recipes paint their own hollows (codex/paint.md)
NORMAL_FROM_ALBEDO = 4.1   # chitin's normal strength k


def build():
    m = {name: getattr(queen_paint, name)() for name in
         ("chitin", "joint", "glow", "glow_hot", "wing", "horn", "eye")}
    queen_body.build(m)
    queen_limbs.build(m)
    queen_bones.tag_scene()          # each part's bone of the game Deathsquito's skeleton, for queen_rig.py

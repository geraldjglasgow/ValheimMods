"""The Deathsquito Queen's egg, whole (BRIEF.md): a leathery ovoid of the Queen's olive chitin, 0.9 m long and 0.6 m
across, five lobes above a wavy waist, the Queen's blood-red glow showing through the grooves between them and in a
few soft windows. A game Deathsquito hatches out of it (MOVES.md of ecp_deathsquito_queen, move 2 "Brood").

Built to the codex's `item.misc` (the Dragon Egg's category: 112 to 1,868 triangles, 64 to 256 px) on one 128 px atlas.
Metres, Z up; the long axis is Z (the narrower end, where the lobes meet, at +Z) and the ORIGIN IS THE EGG'S CENTRE,
so a mod sinks it half into the ground by placing the origin on the ground. Petal 0's lobe faces the front (-Y).

    .\build.ps1 -Asset ecp_queen_egg -Lineup
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import egg_paint  # noqa: E402
import egg_shape  # noqa: E402

CATEGORY = "item.misc"
TEXTURE_SIZE = 128
AO_STRENGTH = 0.2          # the paint recipes paint their own hollows (codex/paint.md)
NORMAL_FROM_ALBEDO = 4.1   # chitin's normal strength k


def build():
    egg_shape.whole("egg", egg_paint.shell(), egg_paint.glowing())

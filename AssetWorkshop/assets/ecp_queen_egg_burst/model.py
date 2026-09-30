"""The Deathsquito Queen's egg, burst (BRIEF.md): the whole egg (ecp_queen_egg) split for hatching into a lower cup and
five petals, each a closed piece of shell 3.5 cm thick, 1 cm apart, fitting together into the whole egg's shape with
its paint; inside, the wet red-brown slime of the hatchling.

Same frame as the whole egg: metres, Z up, the long axis Z, the ORIGIN AT THE EGG'S CENTRE. The pieces share no vertex
and touch nowhere, so Blender's "separate by loose parts" gives six objects: the cup is the one reaching down to
z = -0.45 (the only piece below z = -0.1); petal k's middle faces the angle -90 + 72 k degrees from +X (petal 0 the
front, -Y), its foot on the waist groove and its tip at the egg's top.

    .\build.ps1 -Asset ecp_queen_egg_burst -Lineup
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "ecp_queen_egg"))      # the whole egg's surface and paint

import burst_shape  # noqa: E402
import egg_paint  # noqa: E402

CATEGORY = "item.misc"
TEXTURE_SIZE = 256         # twice the surface of the whole egg (inside, edges): 128 px would halve its texel size
AO_STRENGTH = 0.2          # the paint recipes paint their own hollows (codex/paint.md)
NORMAL_FROM_ALBEDO = 4.1   # chitin's normal strength k


def build():
    burst_shape.build({"shell": egg_paint.shell(), "glow": egg_paint.glowing(), "inside": egg_paint.inside(),
                       "section": egg_paint.section()})

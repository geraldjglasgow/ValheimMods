"""The Queen beside the game's own Deathsquito: scaled 2.5 (what the Queen was built to: same proportions, same
paint per pixel?) and at its true size, under the lineup's sun and sky, point filtered. Run after a build:

    blender --background --factory-startup --python assets/ecp_deathsquito_queen/review.py

Writes out/review/<view>.png (turn, top, front, side, below, scale) and out/review/review.blend.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import lineup, prefab  # noqa: E402

NAME = os.path.basename(HERE)
OUT = os.path.join(HERE, "out", "review")
GAME = "Characters/Deathsquito/Deathsquito.prefab"
VIEWS = {"turn": (0.55, -1.0, 0.45), "top": (0.0, -0.001, 1.0), "front": (0.0, -1.0, 0.06),
         "side": (1.0, 0.0, 0.06), "below": (0.45, -0.8, -0.55)}


def main():
    queen = lineup.asset_entry(os.path.join(HERE, "out", NAME + ".blend"), NAME, False)
    big = prefab.load(GAME, "Deathsquito x2.5")
    big.scale = (2.5, 2.5, 2.5)
    small = prefab.load(GAME, "Deathsquito")
    lineup.point_filter()
    lineup.stage(12.0)
    os.makedirs(OUT, exist_ok=True)
    pair = queen["meshes"] + prefab.meshes(big)
    floor = bpy.data.objects["lineup_floor"]
    for view, direction in VIEWS.items():
        big.location = Vector((0.0, 3.6, 0.0)) if view == "side" else Vector((5.2, 0.0, 0.0))
        small.location = Vector((0.0, 0.0, -50.0))
        floor.hide_render = view == "below"
        lineup.VIEWS[view] = Vector(direction)
        lineup.shot(view, pair, os.path.join(OUT, view + ".png"), lens=40)
    floor.hide_render = False
    big.location, small.location = Vector((0.0, 0.0, -50.0)), Vector((-3.2, 0.0, 0.0))
    marker = _player_marker(Vector((-4.6, 0.0, 0.0)))
    lineup.VIEWS["scale"] = Vector((0.3, -1.0, 0.25))
    lineup.shot("scale", queen["meshes"] + prefab.meshes(small) + [marker], os.path.join(OUT, "scale.png"), lens=40)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "review.blend"))


def _player_marker(at):
    """A 1.8 m capsule where a player would stand, for scale."""
    bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=1.8, location=at + Vector((0, 0, 0.9)))
    marker = bpy.context.active_object
    marker.name = "player 1.8 m"
    marker.data.materials.append(lineup.flat("marker", (0.35, 0.4, 0.55)))
    return marker


main()

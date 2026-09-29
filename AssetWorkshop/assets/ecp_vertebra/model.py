"""The vertebra the skeletons drop (Elite Creatures Pack): one big lumbar vertebra, the bone every weapon of the skeleton
arsenal (ecp_skel_*) carries, as an item on its own.

About 16 cm from wingtip to wingtip and 15 cm from the body's front to the spine's tip: oversized like the game's other
bone items (its skull trophy is 33 cm). It stands on its body with the spine pointing back (-Y is the front of the
body, facing the viewer), lifted so its lowest point touches the ground; the origin is on the ground under it. One box
collider round it all, for the item's Rigidbody.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ecp_skel_arsenal"))

import bpy  # noqa: E402

from workshop import scene, shapes  # noqa: E402

import grave_paint  # noqa: E402
import grave_vertebra  # noqa: E402

TEXTURE_SIZE = 512
AO_STRENGTH = 0.75
R = 0.043


def build():
    bone = grave_paint.vertebra("vertebra_bone")
    place = grave_vertebra.frame((0.0, -0.035, 0.0), (0, 0, 1), (0, 1, 0))
    parts = grave_vertebra.build("vertebra", place, R, bone, kind='lumbar')
    lowest = min((p.matrix_world @ v.co).z for p in parts for v in p.data.vertices)
    for part in parts:
        part.location.z -= lowest
    bpy.context.view_layer.update()
    low, high = scene.bounds(parts)
    shapes.collider_box("col_vertebra", tuple(high - low), tuple((low + high) / 2))
    return parts

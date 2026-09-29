"""The Mossback: the workshop's proof of route (b), a new body on a game skeleton (codex/rigs/README.md). A hulking
forest ogre (hunched mossy back, head sunk under a heavy brow, tusks, gorilla forearms, mitten hands, short heavy
legs) grown round the game Skeleton's own 53 bones, weighted to them in the Skeleton body's bone order, so the
Skeleton's avatar, controller and clips play it unchanged. Built by blender/workshop/gamerig_run.py (see build.ps1).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from workshop import gamerig  # noqa: E402

import body  # noqa: E402
import looks  # noqa: E402
import parts  # noqa: E402

SOURCE = "Characters/Skeleton/Skeleton.prefab"
CATEGORY = "creature.humanoid"
TEXTURE_SIZE = 256
AO_STRENGTH = 0.5
SMOOTHNESS = 0.18
BONE_ORDER = "game"


def build():
    """The rig (an exact copy of the Skeleton's), the weighted body and parts, and four sockets."""
    rig = gamerig.armature(SOURCE, name="mossback_rig")
    paint = looks.build()
    body.build(rig, paint)
    parts.build(rig, paint)
    sockets(rig)
    return rig


def sockets(rig):
    """Where a mod hangs things: the mouth (bite and breath origin), the eyes (CharacterAnimEvent's names), the hump."""
    gamerig.socket(rig, "Mouth", "Jaw", (0.0, -0.235, 1.735))
    gamerig.socket(rig, "LeftEye", "Head", (0.052, -0.2, 1.822))
    gamerig.socket(rig, "RightEye", "Head", (-0.052, -0.2, 1.822))
    gamerig.socket(rig, "fx_back", "Spine2", (0.0, 0.26, 1.7))

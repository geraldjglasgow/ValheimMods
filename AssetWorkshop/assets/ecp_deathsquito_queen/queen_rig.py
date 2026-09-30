"""The Queen on the game Deathsquito's own skeleton (route b of codex/rigs/README.md), at 2.5 times: every bone, name,
parent and axis as the game prefab has them (workshop.gamerig), the armature scaled 2.5 and applied, so the rest pose
is the Queen's. Each part rides one bone whole (weight 1, from the face attribute queen_bones writes in the build), so
the game's own clips or a pose keyed on these bones move her the way a mod will. Two sockets: `Mouth` at the needle's
tip under Head (where needles leave and the dive strikes) and `EggPoint` at the abdomen's tip under Abdomen.

    blender --background --factory-startup --python assets/ecp_deathsquito_queen/queen_rig.py [-- --check]

Run after a build (it opens out/ecp_deathsquito_queen.blend). Writes out/rig/queen_rig.blend; --check also renders a
test pose (wings up, abdomen curled, head down, knees bent) to out/rig/check_*.png.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))
sys.path.insert(0, HERE)

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import gamerig, lineup  # noqa: E402

import queen_bones  # noqa: E402
import queen_limbs  # noqa: E402
import queen_pose  # noqa: E402

NAME = os.path.basename(HERE)
SOURCE = "Characters/Deathsquito/Deathsquito.prefab"
SCALE = 2.5
OUT = os.path.join(HERE, "out", "rig")
SOCKETS = {"Mouth": ("Proboscis", (0.0, -1.44, 0.98)), "EggPoint": ("Abdomen", (0.0, 1.45, 1.03))}
PROBOSCIS = ((0.0, -0.56, 1.13), (0.0, -1.44, 0.98))     # the needle's root and tip


def build():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, "out", NAME + ".blend"))
    body = bpy.data.objects[NAME]
    rig = gamerig.armature(SOURCE, "queen_rig")
    _scale(rig)
    _proboscis(rig)
    for name, (parent, location) in SOCKETS.items():
        gamerig.socket(rig, name, parent, location)
    _leg_chains(rig)
    _weights(body)
    modifier = body.modifiers.new("rig", 'ARMATURE')
    modifier.object = rig
    body.parent = rig
    _report(body, rig)
    return rig, body


def _scale(rig):
    """The Deathsquito's skeleton grown 2.5 times, applied, so the rest pose sits in the Queen's metres."""
    rig.scale = (SCALE, SCALE, SCALE)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def _proboscis(rig):
    """A deforming bone under the Head from the needle's root towards its tip, so the needle can droop and sway on its
    own; a bone the game creature lacks (a mod adds it)."""
    bpy.ops.object.mode_set(mode='EDIT')
    bone = rig.data.edit_bones.new("Proboscis")
    bone.head, bone.tail = PROBOSCIS
    bone.parent, bone.use_deform, bone.use_connect = rig.data.edit_bones["Head"], True, False
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.bones["Proboscis"][gamerig.KIND], rig.data.bones["Proboscis"][gamerig.NEW] = "bone", True


def _leg_chains(rig):
    """The front and hind legs' chains: each bone a copy of the game chain's bone (its axes, its length, its joint's
    height and reach) moved along her length to that leg's joint (queen_limbs.LEG_Y), parented as the game's are, so
    the same local rotation bends every leg alike. Deforming bones the game creature lacks (a mod adds them)."""
    bpy.ops.object.mode_set(mode='EDIT')
    edit = rig.data.edit_bones
    for side in "LR":
        for leg in (0, 2):
            parent = edit[f"{side}.leg1"].parent
            for k in range(1, 5):
                game = edit[f"{side}.leg{k}"]
                bone = edit.new(queen_bones.leg_bone(side, leg, k))
                bone.head, bone.tail = (0, 0, 0), (0, game.length, 0)
                matrix = game.matrix.copy()
                matrix.translation.y = queen_limbs.LEG_Y[leg][k - 1]
                bone.matrix, bone.parent, bone.use_deform, bone.use_connect = matrix, parent, True, False
                parent = bone
    bpy.ops.object.mode_set(mode='OBJECT')
    for name in queen_bones.OWN:
        rig.data.bones[name][gamerig.KIND], rig.data.bones[name][gamerig.NEW] = "bone", True


def _weights(body):
    """A vertex group per bone; every vertex follows the bone written on its faces, whole."""
    values = [0] * len(body.data.polygons)
    body.data.attributes[queen_bones.ATTRIBUTE].data.foreach_get("value", values)
    untagged = sum(1 for v in values if v == 0)
    if untagged:
        raise SystemExit(f"queen_rig: {untagged} faces carry no bone")
    groups = {name: body.vertex_groups.new(name=name) for name in queen_bones.BONES}
    members = {}
    for polygon, value in zip(body.data.polygons, values):
        members.setdefault(queen_bones.BONES[value - 1], set()).update(polygon.vertices)
    for name, verts in members.items():
        groups[name].add(list(verts), 1.0, 'REPLACE')


def _report(body, rig):
    """Per bone: its vertices and how far their middle lies from the bone's head, to see each part rides the right
    joint."""
    for group in body.vertex_groups:
        points = [body.matrix_world @ v.co for v in body.data.vertices
                  if any(g.group == group.index for g in v.groups)]
        if points:
            middle = sum(points, Vector()) / len(points)
            gap = (middle - gamerig.head(rig, group.name)).length
            print(f"WORKSHOP rig: {group.name:9s} {len(points):5d} vertices, middle {gap:.2f} m from its joint")


TEST_POSE = {"L.Wing": ((0, 1, 0), -40), "R.Wing": ((0, 1, 0), 40), "Abdomen": ((1, 0, 0), -35),
             "Head": ((1, 0, 0), 20), "L.leg3": ((0, 1, 0), 25), "R.leg3": ((0, 1, 0), -25)}


def check(rig, body):
    """Renders the rest pose and a test pose from two sides."""
    for bone, (axis, degrees) in TEST_POSE.items():
        pose = rig.pose.bones[bone]
        pose.rotation_mode = 'QUATERNION'
        pose.rotation_quaternion = queen_pose.turn(rig, bone, axis, degrees)
    lineup.stage(10.0)
    for view, direction in {"turn": (0.6, -1.0, 0.4), "side": (1.0, 0.0, 0.05)}.items():
        lineup.VIEWS[view] = Vector(direction)
        lineup.shot(view, [body], os.path.join(OUT, f"check_{view}.png"), lens=40)


def main():
    os.makedirs(OUT, exist_ok=True)
    rig, body = build()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "queen_rig.blend"))
    if "--check" in sys.argv:
        check(rig, body)
    print(f"WORKSHOP rig: {os.path.join(OUT, 'queen_rig.blend')}", flush=True)


if __name__ == "__main__":
    main()

"""Which bone each part of the Queen rides. The parts are rigid, so each rides one bone whole: the thorax and crown the
Thorax, the head, eyes, palps and antennae the Head, the needle its own `Proboscis` under the Head (so it can droop and
sway), the abdomen and sting the Abdomen, each wing its Wing bone, and each leg segment its bone of that leg's chain.
The game Deathsquito has one leg chain a side (its three legs share it): the Queen's middle legs ride it, and her front
and hind legs ride chains of their own (`L.legF1`..`4`, `L.legH1`..`4` and the right side's), new bones under the
Thorax at their own joints with the game chain's axes, so every leg can swing on its own (queen_sway.py). The new bones
are ones BundlePrefabs.CreatureBody copies onto the game creature by name, for a mod to pose in code. The bone is kept
on every face (a face attribute, BONES index + 1) so it survives the pipeline's join and bake; queen_rig.py turns it
into vertex groups."""
import re

import bpy

GAME = ["Root", "Root2", "Thorax", "Bone.003", "Head", "Bone.005", "Abdomen", "L.leg1", "L.leg2", "L.leg3", "L.leg4",
        "L.Wing", "R.leg1", "R.leg2", "R.leg3", "R.leg4", "R.Wing"]     # the game body's bone order
LEGS = ("F", "", "H")               # front, middle (the game's chain), hind: queen_limbs.LEG_Y's order
OWN = [f"{side}.leg{kind}{k}" for side in "LR" for kind in ("F", "H") for k in range(1, 5)] + ["Proboscis"]
BONES = GAME + OWN
ATTRIBUTE = "queen_bone"
LEG_PART = {"coxa": 1, "femur": 2, "hipband": 2, "tibia": 3, "knee": 3, "tarsus": 4, "claw": 4, "ankle": 4}
HEAD_PARTS = ("head", "eye", "needle", "palp", "antenna")


def leg_bone(side, leg, k):
    """The bone of segment k (1 coxa .. 4 tarsus) of leg `leg` (0 front, 1 middle, 2 hind) on side 'L' or 'R'."""
    return f"{side}.leg{LEGS[leg]}{k}"


def bone_of(name):
    """The bone a part (by its object name) rides."""
    leg = re.match(r"leg(\d)([+-]1)_(\w+)", name)
    if leg:
        return leg_bone(_side(leg.group(2)), int(leg.group(1)), LEG_PART[leg.group(3)])
    wing = re.match(r"(wing|costa|vein)([+-]1)", name)
    if wing:
        return f"{_side(wing.group(2))}.Wing"
    if name.startswith("needle"):
        return "Proboscis"
    if name.startswith(HEAD_PARTS):
        return "Head"
    if name.startswith(("abdomen", "stinger")):
        return "Abdomen"
    if name.startswith(("thorax", "crown")):
        return "Thorax"
    raise ValueError(f"no bone for part {name}")


def _side(sign):
    """Side +1 is +X, the creature's left (it faces -Y), as the game's L.Wing is."""
    return "L" if sign == "+1" else "R"


def tag(objects):
    """Writes each mesh object's bone onto its faces."""
    for obj in objects:
        if obj.type != 'MESH':
            continue
        value = BONES.index(bone_of(obj.name)) + 1
        layer = obj.data.attributes.get(ATTRIBUTE) or obj.data.attributes.new(ATTRIBUTE, 'INT', 'FACE')
        layer.data.foreach_set("value", [value] * len(obj.data.polygons))


def tag_scene():
    tag(list(bpy.context.scene.objects))

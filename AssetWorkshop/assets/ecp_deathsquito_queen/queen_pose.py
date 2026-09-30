"""A queen_moves.State onto the rig: the body's place and turn on the root empty, the pose parameters as rotations of
the game Deathsquito's own bones (the ones a mod would pose) and of the Queen's own leg chains and proboscis: the wings
about her long axis, the head and abdomen about their joints, each leg's hip, knee and tarsus and the proboscis from
queen_sway's springs. The wings buzz one stroke a frame (15 a second at 30 frames), which motion blur turns into the
blurred fan an insect's wings are; each stroke also sweeps and twists them a little."""
import math

from mathutils import Quaternion, Vector

from queen_bones import leg_bone

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
STROKE_UP, STROKE_DOWN = 42.0, -14.0      # the buzz's two ends, degrees above level, at buzz 1
LEG_JOINTS = (1, 3, 4)                    # hip, knee, tarsus: the posed segments of each leg


def turns(rig, bone, steps):
    """The pose rotation that turns `bone` by each (axis, degrees) of her own rest frame in turn (the armature rests
    in her frame), whatever the bone's own axes; its children follow."""
    rest = rig.data.bones[bone].matrix_local.to_quaternion()
    world = Quaternion()
    for axis, degrees in steps:
        world = world @ Quaternion(Vector(axis), math.radians(degrees))
    return rest.inverted() @ world @ rest


def turn(rig, bone, axis, degrees):
    return turns(rig, bone, [(axis, degrees)])


def wing_angle(s, frame):
    stroke = STROKE_UP if frame % 2 == 0 else STROKE_DOWN
    return s.wing_lift + 14.0 + (stroke - 14.0) * s.buzz


def wings(rig, s, frame):
    """Both wings: the buzz's stroke about her long axis, sweeping a little forward and back and twisting about the
    wing's length (the leading edge down on the downstroke) with each stroke."""
    wing = wing_angle(s, frame)
    sweep, twist = (6.0 if frame % 2 == 0 else -6.0) * s.buzz, (-8.0 if frame % 2 == 0 else 8.0)
    return {"L.Wing": turns(rig, "L.Wing", [(Y, -wing), (Z, sweep), (X, twist)]),
            "R.Wing": turns(rig, "R.Wing", [(Y, wing), (Z, -sweep), (X, twist)])}


def bones(rig, s, frame, motion):
    """Each posed bone's rotation for state `s` at `frame`, what swings from `motion` (queen_sway.Sway.step)."""
    (droop, side), (_, _, curl, yaw) = motion.needle, motion.body
    out = wings(rig, s, frame)
    out.update({"Head": turn(rig, "Head", X, s.head),
                "Abdomen": turns(rig, "Abdomen", [(X, -(s.curl + curl)), (Z, yaw)]),
                "Proboscis": turns(rig, "Proboscis", [(X, droop), (Z, side)])})
    for (lr, leg), (back, outward, knee, tarsus) in motion.legs.items():
        sign = 1 if lr == "L" else -1        # her left legs fold and swing out the other way round Y
        out[leg_bone(lr, leg, 1)] = turns(rig, leg_bone(lr, leg, 1), [(X, back), (Y, -sign * outward)])
        out[leg_bone(lr, leg, 3)] = turn(rig, leg_bone(lr, leg, 3), Y, sign * knee)
        out[leg_bone(lr, leg, 4)] = turn(rig, leg_bone(lr, leg, 4), Y, sign * tarsus)
    return out


def key(keys, root, rig, s, frame, motion):
    """The root empty's place and turn (Euler YXZ: bank, then pitch, then yaw; her flying posture from `motion` added)
    and the bones, at `frame`."""
    pitch, bank = s.pitch + motion.body[0], s.bank + motion.body[1]
    keys.add(root, 'location', frame, s.pos)
    keys.add(root, 'rotation_euler', frame, (math.radians(pitch), math.radians(bank), s.yaw))
    for name, rotation in bones(rig, s, frame, motion).items():
        keys.add(rig, f'pose.bones["{name}"].rotation_quaternion', frame, rotation)


def prepare(rig, root):
    root.rotation_mode = 'YXZ'
    names = ["L.Wing", "R.Wing", "Head", "Abdomen", "Proboscis"]
    names += [leg_bone(lr, leg, k) for lr in "LR" for leg in range(3) for k in LEG_JOINTS]
    for name in names:
        rig.pose.bones[name].rotation_mode = 'QUATERNION'

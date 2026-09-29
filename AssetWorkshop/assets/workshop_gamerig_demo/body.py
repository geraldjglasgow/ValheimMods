"""The Mossback's body: one closed skin grown round the game Skeleton's own bones out of metaballs (every mass placed
from a bone's rest position, so the body follows the skeleton it will wear), turned into a mesh, cut down to the
game's budget, painted by region (skin, moss on the hump and shoulders, leather wraps at the wrists and shins) and
weighted with Blender's automatic weights, the head and jaw by hand.

Blender axes: metres, Z up, the creature looks down -Y, its left hand at +X (the Skeleton's T-pose).
"""
import bmesh
import bpy
from mathutils import Vector

from workshop import gamerig, gamerig_weights, scene

REACH = 0.575                     # a metaball's surface lies at 0.575 of its radius (stiffness 2, threshold 0.6)
RESOLUTION = 0.018                # metres between metaball samples
BODY_TRIANGLES = 2500


def build(rig, looks):
    """The weighted, painted body mesh."""
    field = bpy.data.metaballs.new("mossback_field")
    field.resolution = field.render_resolution = RESOLUTION
    field.threshold = 0.6
    shape = bpy.data.objects.new("mossback_field", field)
    bpy.context.scene.collection.objects.link(shape)
    _torso(field, rig)
    _head(field, rig)
    for side in ("Left", "Right"):
        _arm(field, rig, side)
        _leg(field, rig, side)
    body = _mesh(shape)
    _paint(body, looks)
    _weights(body, rig)
    return body


def mirror(point, side):
    """A left-side point (+X) on the given side."""
    return Vector((point[0] if side == "Left" else -point[0], point[1], point[2]))


def ellipsoid(field, centre, half):
    """A mass whose surface, alone, reaches `half` (x, y, z) metres from its centre."""
    element = field.elements.new(type='ELLIPSOID')
    element.co, element.radius, element.stiffness = Vector(centre), 1.0 / REACH, 2.0
    element.size_x, element.size_y, element.size_z = half
    return element


def capsule(field, a, b, radius):
    """A limb from a to b whose surface, alone, lies `radius` metres round the line."""
    a, b = Vector(a), Vector(b)
    element = field.elements.new(type='CAPSULE')
    element.co, element.radius, element.stiffness = (a + b) / 2, radius / REACH, 2.0
    element.size_x = (b - a).length / 2
    element.rotation = Vector((1, 0, 0)).rotation_difference((b - a).normalized())
    return element


def _torso(field, rig):
    """Belly, chest, a hump that rises behind the neck, heavy trapezius slopes up to the ears."""
    ellipsoid(field, (0, 0.03, 0.97), (0.23, 0.16, 0.13))            # pelvis
    ellipsoid(field, (0, -0.1, 1.13), (0.24, 0.2, 0.18))             # belly
    ellipsoid(field, (0, 0.0, 1.33), (0.27, 0.19, 0.19))             # ribcage
    ellipsoid(field, (0, -0.07, 1.45), (0.25, 0.13, 0.12))           # chest
    ellipsoid(field, (0, 0.13, 1.57), (0.26, 0.17, 0.17))            # hump
    for side in ("Left", "Right"):
        capsule(field, mirror((0.05, 0.07, 1.68), side), mirror((0.22, 0.06, 1.63), side), 0.1)   # trapezius
        ellipsoid(field, mirror((0.25, 0.05, 1.6), side), (0.12, 0.12, 0.12))                     # deltoid


def _head(field, rig):
    """A thick neck, the head thrust forward under a heavy brow, sunken eyes, a broad nose, an underslung jaw."""
    capsule(field, (0, 0.05, 1.6), (0, -0.04, 1.73), 0.11)            # neck
    ellipsoid(field, (0, -0.06, 1.83), (0.125, 0.13, 0.12))           # cranium
    ellipsoid(field, (0, -0.16, 1.862), (0.135, 0.05, 0.038))         # brow
    ellipsoid(field, (0, -0.205, 1.795), (0.048, 0.045, 0.05))        # nose
    ellipsoid(field, (0, -0.125, 1.705), (0.135, 0.11, 0.06))         # jaw, jutting past the lip
    hollow(field, (-0.07, -0.225, 1.748), (0.07, -0.225, 1.748), 0.018)   # the mouth's line
    for side in ("Left", "Right"):
        ellipsoid(field, mirror((0.1, -0.11, 1.77), side), (0.05, 0.065, 0.045))  # cheek
        ellipsoid(field, mirror((0.13, -0.03, 1.81), side), (0.03, 0.035, 0.05))  # ear
        socket = ellipsoid(field, mirror((0.052, -0.205, 1.822), side), (0.034, 0.03, 0.022))
        socket.use_negative = True                                               # eye socket


def hollow(field, a, b, radius):
    """A groove: a negative capsule the surface pulls in round."""
    element = capsule(field, a, b, radius)
    element.use_negative = True
    return element


def _arm(field, rig, side):
    """Upper arm, a gorilla forearm swelling to the wrist, a broad mitten hand with a thumb."""
    head = lambda bone: gamerig.head(rig, side + bone)
    capsule(field, head("Arm"), head("ForeArm"), 0.088)
    ellipsoid(field, head("ForeArm").lerp(head("Hand"), 0.55), (0.15, 0.11, 0.11))
    capsule(field, head("ForeArm"), head("Hand"), 0.075)
    palm = head("Hand").lerp(head("HandMiddle1"), 0.55)
    ellipsoid(field, palm + Vector((0, 0, -0.005)), (0.08, 0.075, 0.04))
    capsule(field, head("HandIndex1"), head("HandIndex3_end"), 0.028)
    middle = head("HandMiddle1").lerp(head("HandRing1"), 0.5)
    capsule(field, middle, head("HandMiddle3_end").lerp(head("HandRing3_end"), 0.5), 0.032)
    capsule(field, head("HandPinky1"), head("HandPinky3_end"), 0.024)
    capsule(field, head("HandThumb1"), head("HandThumb3_end"), 0.027)


def _leg(field, rig, side):
    """A heavy thigh, knee, a thick calf, a big flat foot."""
    head = lambda bone: gamerig.head(rig, side + bone)
    hip, knee, ankle = head("UpLeg"), head("Leg"), head("Foot")
    capsule(field, hip + mirror((0.02, 0, -0.06), side), knee, 0.1)
    ellipsoid(field, hip.lerp(knee, 0.35) + mirror((0.04, 0.0, 0.0), side), (0.11, 0.13, 0.15))
    capsule(field, knee, ankle, 0.075)
    ellipsoid(field, knee.lerp(ankle, 0.3) + Vector((0, 0.035, 0)), (0.09, 0.085, 0.13))     # calf
    ellipsoid(field, ankle + Vector((0, -0.065, -0.025)), (0.085, 0.16, 0.055))               # foot


def _mesh(shape):
    """The field as a closed mesh cut down to the budget, smooth shaded: the face and hands keep a third of the
    triangles, so the brow, the sockets and the fingers survive; the big masses share the rest."""
    scene.select_only([shape])
    bpy.ops.object.convert(target='MESH')
    body = bpy.context.view_layer.objects.active
    body.name = body.data.name = "mossback_body"
    detail = lambda c: (c.z > 1.66 and c.y < 0.0 and abs(c.x) < 0.2) or abs(c.x) > 0.8
    _decimate(body, lambda c: not detail(c), BODY_TRIANGLES * 2 // 3)
    _decimate(body, detail, BODY_TRIANGLES // 3)
    body.data.shade_smooth()
    return body


def _decimate(body, chosen, triangles):
    """Collapses the faces whose centres `chosen` accepts down to about `triangles`; the rest stay as they are."""
    scene.select_only([body])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_mode(type='FACE')
    bpy.ops.mesh.select_all(action='DESELECT')
    mesh = bmesh.from_edit_mesh(body.data)
    faces = [f for f in mesh.faces if chosen(f.calc_center_median())]
    for face in faces:
        face.select_set(True)
    bmesh.update_edit_mesh(body.data)
    ratio = min(1.0, triangles / max(1, sum(len(f.verts) - 2 for f in faces)))
    bpy.ops.mesh.decimate(ratio=ratio)
    bpy.ops.object.mode_set(mode='OBJECT')


def _paint(body, looks):
    """Materials by face: moss on top of the hump and shoulders, leather wraps at wrists and shins, skin elsewhere."""
    for key in ("skin", "moss", "leather"):
        body.data.materials.append(looks[key])
    for face in body.data.polygons:
        c, n = face.center, face.normal
        moss = c.z > 1.52 and n.z > 0.35 and c.y > -0.02 and abs(c.x) < 0.36
        wrap = 0.7 < abs(c.x) < 0.8 or (0.14 < c.z < 0.26 and abs(c.x) > 0.06)
        face.material_index = 1 if moss else (2 if wrap else 0)


def _weights(body, rig):
    """Heat weights from every deform bone, smoothed; then by hand: the skull and face on Head and the jaw below the
    mouth line on Jaw, each fading into the neck's weights at its edge, half and half along the lips, and the crotch
    on the hips (so it does not stretch between the thighs when the legs part)."""
    gamerig_weights.auto(body, rig)
    gamerig_weights.smooth(body, factor=0.5, repeat=3)
    gamerig_weights.region(body, (0, -0.07, 1.83), (0.17, 0.2, 0.15), {"Head": 1.0}, soft=0.35)
    below = lambda co: co.z <= 1.748
    gamerig_weights.region(body, (0, -0.135, 1.7), (0.16, 0.14, 0.075), {"Jaw": 1.0}, soft=0.4, where=below)
    lips = lambda co: 1.748 < co.z <= 1.762 and co.y < -0.12 and abs(co.x) < 0.16
    gamerig_weights.fixed(body, {"Head": 0.5, "Jaw": 0.5}, where=lips)
    gamerig_weights.region(body, (0, 0.0, 0.8), (0.1, 0.22, 0.09), {"Hips": 1.0}, soft=0.5)     # the crotch

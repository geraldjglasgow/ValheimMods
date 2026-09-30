"""The Queen's brood and needles beside the Queen and the game's own Deathsquito, for review: two whole eggs half sunk
at a slant (ecp_queen_egg), a burst egg opened like a flower with a game Deathsquito rising out of it
(ecp_queen_egg_burst, split by loose parts as a mod would), eight needles standing in the ground and one in flight
(ecp_queen_needle), a 1.8 m capsule for scale; on Plains heath, under the lineup's sun and sky, point filtered.
Eye-level views from 5 and 10 m on bare heath, then among the game's own heath grass. Run after building the three
assets and the Queen:

    blender --background --factory-startup --python assets/ecp_queen_egg/review.py

Writes out/review/<view>.png and out/review/review.blend.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ASSETS, "..", "blender"))

import bpy  # noqa: E402
from mathutils import Euler, Matrix, Vector  # noqa: E402
from workshop import lineup, prefab, prefab_parts  # noqa: E402

OUT = os.path.join(HERE, "out", "review")
QUEEN = "ecp_deathsquito_queen"
HEATH = (0.21, 0.19, 0.07)                  # Plains heath ground #817849, linear
NEEDLES_AT = Vector((1.0, -4.2, 0.0))       # the volley's landing spot, in front of the Queen
MOUTH = Vector((0.0, -1.42, 0.98))          # the Queen's needle tip
EGGS = [(Vector((-2.3, 1.5, 0.0)), Euler((math.radians(28), 0.0, math.radians(35)))),
        (Vector((-3.3, 0.3, 0.0)), Euler((math.radians(-22), math.radians(14), math.radians(-60))))]
BURST_AT, OPEN = Vector((2.7, 1.3, 0.06)), math.radians(72)
PLAYER_AT = Vector((-1.3, 0.5, 0.0))
GRASS_MESH = "world/Props/ground_clutter/models/default.asset"
GRASS_MAT = "world/Props/ground_clutter/models/materials/grasscross_heath.mat"


def built(name):
    return os.path.join(ASSETS, name, "out", name + ".blend")


def append(name):
    """The asset's baked visual mesh from its built .blend, linked into the scene."""
    with bpy.data.libraries.load(built(name), link=False) as (_, into):
        into.objects = [name]
    obj = into.objects[0]
    bpy.context.scene.collection.objects.link(obj)
    return obj


def eggs():
    egg = append("ecp_queen_egg")
    made = [egg] + [_copy(egg) for _ in EGGS[1:]]
    for obj, (where, turn) in zip(made, EGGS):
        obj.location, obj.rotation_euler = where, turn          # the origin (the egg's centre) on the ground
    return made


def _copy(obj):
    twin = obj.copy()
    bpy.context.scene.collection.objects.link(twin)
    return twin


def burst():
    """The burst egg split by loose parts; the cup is the piece reaching lowest, each petal turned out on its foot."""
    obj = append("ecp_queen_egg_burst")
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.separate(type='LOOSE')
    bpy.ops.object.mode_set(mode='OBJECT')
    parts = [o for o in bpy.context.scene.objects if o.name.startswith("ecp_queen_egg_burst")]
    for part in parts:
        _open(part)
        part.matrix_world = Matrix.Translation(BURST_AT) @ part.matrix_world
    return parts


def _open(part):
    """Turns a petal outward about the tangent at its foot (the cup stays)."""
    points = [v.co for v in part.data.vertices]
    if min(p.z for p in points) < -0.1:
        return
    centre = sum(points, Vector()) / len(points)
    out = Vector((centre.x, centre.y, 0.0)).normalized()
    foot = min(points, key=lambda p: p.z - 0.3 * p.dot(out))
    hinge = Vector((out.x, out.y, 0.0)) * Vector((foot.x, foot.y, 0.0)).length + Vector((0.0, 0.0, foot.z))
    axis = Vector((0.0, 0.0, 1.0)).cross(out)
    turn = Matrix.Translation(hinge) @ Matrix.Rotation(OPEN, 4, axis) @ Matrix.Translation(-hinge)
    part.matrix_world = turn @ part.matrix_world


def needles():
    """Eight needles standing in the ground round the landing spot, tip first; one in flight from the Queen."""
    needle = append("ecp_queen_needle")
    rng = random.Random(8)
    made = []
    for k in range(8):
        spot = NEEDLES_AT + Vector((rng.uniform(-1.2, 1.2), rng.uniform(-1.0, 1.0), 0.0))
        ahead, dip = Vector((spot.x - MOUTH.x, spot.y - MOUTH.y, 0.0)).normalized(), math.radians(rng.uniform(28, 55))
        direction = ahead * math.cos(dip) - Vector((0.0, 0.0, math.sin(dip)))
        made.append(_place(needle if k == 0 else _copy(needle), spot, direction, rng.uniform(0.04, 0.08)))
    flying = MOUTH + (NEEDLES_AT - MOUTH) * 0.45
    made.append(_place(_copy(needle), flying, NEEDLES_AT - MOUTH, 0.0))
    return made


def _place(obj, where, direction, bury):
    """The needle's tip (its origin) `bury` metres past `where` along its flight, its -Y along the flight."""
    d = direction.normalized()
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = Vector((0.0, -1.0, 0.0)).rotation_difference(d)
    obj.location = where + d * bury
    return obj


def player():
    """A 1.8 m capsule where a player would stand, for scale."""
    bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=1.8, location=PLAYER_AT + Vector((0, 0, 0.9)))
    marker = bpy.context.active_object
    marker.name = "player 1.8 m"
    marker.data.materials.append(lineup.flat("marker", (0.35, 0.4, 0.55)))
    return marker


def grass():
    """The game's heath grass round the needles as the ClutterSystem scatters it (eye-level views only): its
    InstanceRenderer mesh and material at the renderer's scale (1.3, 3.5, 1.3) times the clutter's 0.7 to 1.5, 200 a
    10 m patch times the system's 1.5, so 3 a square metre."""
    data = prefab_parts.mesh(GRASS_MESH, [prefab_parts.material(GRASS_MAT)])
    rng, made = random.Random(3), []
    for _ in range(int(3 * 12 * 16)):
        where = NEEDLES_AT + Vector((rng.uniform(-6, 6), rng.uniform(-11, 5), 0.0))
        size = rng.uniform(0.7, 1.5)
        card = bpy.data.objects.new("heath grass", data)
        bpy.context.scene.collection.objects.link(card)
        card.matrix_world = Matrix.LocRotScale(where, Euler((0, 0, rng.uniform(0, 6.3))),
                                               Vector((1.3, 1.3, 3.5)) * size)
        made.append(card)
    return made


def eye(name, distance, target, side=0.35):
    """A camera 1.8 m up, `distance` metres from the target on the ground, looking at it."""
    s = bpy.context.scene
    camera = bpy.data.objects.new("review_" + name, bpy.data.cameras.new("review_" + name))
    s.collection.objects.link(camera)
    flat = Vector((side, -1.0, 0.0)).normalized()
    camera.location = target + flat * distance + Vector((0.0, 0.0, 1.8))
    camera.rotation_euler = (target + Vector((0, 0, 0.2)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.lens, camera.data.clip_end = 30, 200
    s.camera = camera
    s.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


def shots(queen, laid, pieces, pins, marker, small):
    brood = laid + pieces + prefab.meshes(small) + [marker]
    views = {"overview": ((0.5, -1.0, 0.55), queen + brood + pins),
             "eggs": ((0.3, -1.0, 0.35), laid + [marker]),
             "burst": ((-0.45, -1.0, 0.6), pieces + prefab.meshes(small)),
             "needles": ((0.45, -1.0, 0.5), pins[:-1] + [marker]),
             "top": ((0.0, -0.001, 1.0), queen + brood + pins)}
    for view, (direction, framed) in views.items():
        lineup.VIEWS[view] = Vector(direction)
        lineup.shot(view, framed, os.path.join(OUT, view + ".png"), lens=40)


def main():
    queen = lineup.asset_entry(built(QUEEN), QUEEN, False)["meshes"]
    laid, pieces, pins, marker = eggs(), burst(), needles(), player()
    small = prefab.load("Characters/Deathsquito/Deathsquito.prefab", "Deathsquito")
    small.location = BURST_AT + Vector((0.15, -0.1, 0.35))           # rising out of the burst egg
    lineup.point_filter()
    lineup.stage(14.0)
    bpy.data.objects["lineup_floor"].data.materials[0] = lineup.flat("heath", HEATH, roughness=1.0)
    os.makedirs(OUT, exist_ok=True)
    shots(queen, laid, pieces, pins, marker, small)
    eye("eye_5m_bare", 5.0, NEEDLES_AT)
    eye("eye_10m_bare", 10.0, NEEDLES_AT)
    grass()
    lineup.point_filter()
    eye("eye_5m_grass", 5.0, NEEDLES_AT)
    eye("eye_10m_grass", 10.0, NEEDLES_AT)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "review.blend"))


main()

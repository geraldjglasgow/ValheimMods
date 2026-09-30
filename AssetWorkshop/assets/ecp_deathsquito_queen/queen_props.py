"""What the Queen throws into the fight, for the preview: needles (flying, vanishing in a hit, or stuck in the ground
where they stay), eggs (flung in an arc, burying themselves half deep), hit flashes and dust. The needle
and egg models are the workshop's own assets (ecp_queen_needle, ecp_queen_egg), appended from their builds; effects
are simple meshes whose fade is the object colour's alpha (a material reading Object Info), as the game's are
particles a mod would borrow and tint."""
import math
import os

import bpy
from mathutils import Euler, Vector

import queen_shots

ASSETS = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
NEEDLE_AXIS = Vector((0.0, -1.0, 0.0))    # the needle model's tip direction; its origin is at the tip
NEEDLE_BURY = 0.07                        # how deep a stuck needle's tip goes: its barbs at the ground line
HIT_RADIUS = 0.5                          # a needle at a chest hits a player standing this near its aim
NEEDLE_LIFE, CRUMBLE = 30.0, 0.6          # seconds a stuck needle stays (harmless), and its crumbling into the ground
EGG_ARC, EGG_SINK = 2.0, 0.15             # the egg's flight apex above the straight line; its sink time, seconds
_templates = {}


def template(name):
    """The asset's built visual mesh, appended once from assets/<name>/out/<name>.blend and hidden."""
    if name not in _templates:
        path = os.path.join(ASSETS, name, "out", name + ".blend")
        if not os.path.exists(path):
            print(f"WORKSHOP preview: {name} is not built yet, a stand-in shape takes its place", flush=True)
            obj = _stand_in(name)
        else:
            with bpy.data.libraries.load(path, link=False) as (data_from, data_to):
                data_to.objects = [n for n in data_from.objects if n == name]
            obj = data_to.objects[0]
            bpy.context.scene.collection.objects.link(obj)
            _point_filter(obj)
            regions = os.path.join(ASSETS, name, "out", name + "_regions.png")
            if "egg" in name and os.path.exists(regions):
                queen_shots.glow(obj, regions)          # the grooves glow as the Queen's belly does
        obj.hide_render = obj.hide_viewport = True
        _templates[name] = obj
    return _templates[name]


def _stand_in(name):
    """A plain shape the size of the missing asset: a needle 0.6 m long pointing -Y from its tip, an egg 0.9 m long
    along Z round its middle, or a burst egg of a cup and five petals."""
    if name == "ecp_queen_needle":
        bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=0.02, depth=0.6, location=(0, 0.3, 0),
                                        rotation=(math.radians(90), 0, 0))
    elif name == "ecp_queen_egg":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.3)
        bpy.context.active_object.scale = (1.0, 1.0, 1.5)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.28, location=(0, 0, -0.2))
        for k in range(5):
            a = 2 * math.pi * k / 5
            bpy.ops.mesh.primitive_uv_sphere_add(segments=6, ring_count=4, radius=0.12,
                                                 location=(0.2 * math.cos(a), 0.2 * math.sin(a), 0.2))
        bpy.ops.object.select_all(action='DESELECT')
        for obj in [o for o in bpy.context.scene.objects if o.name.startswith("Sphere")]:
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
        bpy.ops.object.join()
    obj = bpy.context.active_object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = name
    return obj


def _point_filter(obj):
    for slot in obj.material_slots:
        for node in (slot.material.node_tree.nodes if slot.material else []):
            if node.bl_idname == 'ShaderNodeTexImage':
                node.interpolation = 'Closest'


def copy(name, label):
    """A linked copy of a template, shown."""
    obj = template(name).copy()
    obj.name, obj.hide_render, obj.hide_viewport = label, False, False
    bpy.context.scene.collection.objects.link(obj)
    return obj


def needle(keys, event, frame0, fps, last_frame, flash, dust, where, gone):
    """One needle along the straight line it was fired on (it never turns after anyone): one aimed at a chest hits if a
    player (`where(frame)`: where each stands) is still there when it arrives, and vanishes in a flash; otherwise it
    flies on down its line and sticks in the ground, harmless, until `gone(arrive)` (the preview frame NEEDLE_LIFE
    seconds on in the story), when it crumbles into the ground in a puff of dust."""
    start, aim = Vector(event["from"]), Vector(event["to"])
    arrive = frame0 + max(1, round((aim - start).length / event["speed"] * fps))
    hit = aim.z > 0.5 and any((p.xy - aim.xy).length < HIT_RADIUS for p in where(arrive))
    if not hit and aim.z > 0.0:
        aim = start + (aim - start) * (start.z / (start.z - aim.z))      # on down the same line to the ground
        arrive = frame0 + max(1, round((aim - start).length / event["speed"] * fps))
    way = aim - start
    obj = copy("ecp_queen_needle", f"needle {frame0}")
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = NEEDLE_AXIS.rotation_difference(way.normalized())
    stuck, crumble = aim + way.normalized() * NEEDLE_BURY, gone(arrive) if not hit else last_frame + 1
    for f in range(frame0 - 1, last_frame + 1):
        at = start + way * min(max((f - frame0) / (arrive - frame0), 0.0), 1.0) if f < arrive else stuck
        sinking = min(max((f - crumble) / (CRUMBLE * fps), 0.0), 1.0)
        keys.add(obj, 'location', f, at + way.normalized() * 0.25 * sinking)
        visible = frame0 <= f and (f < arrive or not hit) and sinking < 1.0
        keys.add(obj, 'scale', f, (1 - sinking,) * 3 if visible else (0, 0, 0), constant=sinking == 0.0)
    if hit:
        flash(aim, arrive)
    else:
        dust(aim, arrive, 0.35)
        if crumble <= last_frame:
            dust(Vector((aim.x, aim.y, 0.04)), crumble + 3, 0.22)
    return arrive, ("needle_flesh" if hit else "needle_ground")


def egg_flight(keys, event, frame0, fps, rng):
    """An egg flung in an arc to its spot, tumbling; it lands slanted away from where it came from and sinks in
    until its middle is at the ground. Returns the egg, its landed frame and its resting transform."""
    start, spot = Vector(event["from"]), Vector((*event["to"][:2], 0.0))
    land = frame0 + round(event["flight"] * fps)
    settled = land + round(EGG_SINK * fps)
    away = (spot - start).xy.normalized()
    rest = Euler((-math.radians(35) * away.y, math.radians(35) * away.x, rng.uniform(0, 6.28)), 'ZXY')
    obj = copy("ecp_queen_egg", f"egg {event['index']}")
    obj.rotation_mode = 'ZXY'           # its own spin first, then the slant
    for f in range(frame0, settled + 1):
        tau = min((f - frame0) / (land - frame0), 1.0)
        at = start.lerp(spot + Vector((0, 0, 0.3)), tau) + Vector((0, 0, EGG_ARC * 4 * tau * (1 - tau)))
        if f > land:
            at = spot + Vector((0, 0, 0.3 * (1 - (f - land) / (settled - land))))
        spin = Euler((rest.x + (1 - tau) * 7.0, rest.y + (1 - tau) * 3.0, rest.z), 'ZXY')
        keys.add(obj, 'location', f, at)
        keys.add(obj, 'rotation_euler', f, spin)
    keys.add(obj, 'scale', frame0 - 1, (0, 0, 0), constant=True)
    keys.add(obj, 'scale', frame0, (1, 1, 1), constant=True)
    return obj, land, (spot, rest)


def fx_material(name, colour, emission=0.0):
    """A see-through material whose opacity is the object colour's alpha, so each effect object fades on its own."""
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = bpy.data.materials.new(name)
    tree = mat.node_tree
    bsdf = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*colour, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.9
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*colour, 1.0)
        bsdf.inputs['Emission Strength'].default_value = emission
    info = tree.nodes.new('ShaderNodeObjectInfo')
    tree.links.new(info.outputs['Alpha'], bsdf.inputs['Alpha'])
    mat.surface_render_method = 'BLENDED'
    return mat


def blob(label, radius, material):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius)
    obj = bpy.context.active_object
    obj.name = label
    obj.data.materials.append(material)
    obj.visible_shadow = False
    return obj


def fade(keys, obj, frames, alpha_at, scale_at, where_at):
    """Key an effect object over `frames`: its alpha, scale and place as functions of 0..1 through them."""
    frames = list(frames)
    keys.add(obj, 'color', frames[0] - 1, (1, 1, 1, 0.0), constant=True)
    for i, f in enumerate(frames):
        u = i / max(len(frames) - 1, 1)
        keys.add(obj, 'color', f, (1, 1, 1, alpha_at(u)))
        keys.add(obj, 'scale', f, (scale_at(u),) * 3)
        keys.add(obj, 'location', f, where_at(u))
    keys.add(obj, 'color', frames[-1] + 1, (1, 1, 1, 0.0), constant=True)
    keys.add(obj, 'scale', frames[-1] + 1, (0, 0, 0), constant=True)

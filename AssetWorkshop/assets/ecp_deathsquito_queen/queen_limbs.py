"""The Deathsquito Queen's wings and legs, laid out on the game Deathsquito's at 2.5 times: wings spread level with a
little dihedral, paddle-shaped, a notch worn into the trailing edge; three legs a side on the Deathsquito's one leg
chain per side (hip, coxa, a femur rising to a high knee, the tibia falling to the ankle, a long tarsus out to the
foot: its bones leg1 to leg4 at 2.5 times), banded amber at the joints. Metres, Z up, front -Y; side 1 is +X."""
import bmesh
import bpy
from mathutils import Vector

import queen_geo as geo

WING_Z, DIHEDRAL, WING_THICK, RIM = 1.47, 0.055, 0.016, 0.045
LEAD = [(0.14, 0.02), (0.5, 0.08), (0.9, 0.15), (1.3, 0.22), (1.7, 0.30), (2.0, 0.37), (2.2, 0.45), (2.28, 0.56)]
TRAIL = [(2.25, 0.68), (2.14, 0.79), (1.95, 0.87), (1.7, 0.90), (1.42, 0.86), (1.33, 0.76), (1.25, 0.83),
         (1.0, 0.72), (0.7, 0.54), (0.42, 0.34), (0.2, 0.18)]    # a paddle swept back, one notch worn in
VEIN = [(0.16, 0.08), (0.6, 0.2), (1.2, 0.38), (1.8, 0.54), (2.15, 0.62)]
CHAIN = [(0.08, 1.10), (0.1725, 0.965), (0.3525, 1.435), (0.495, 0.9175), (0.80, 0.66)]   # x, z: hip, the leg2,
#   leg3 and leg4 bones' heads (the Deathsquito's at 2.5 times), the foot past leg4's tail as the game's mesh reaches
LEG_Y = [(-0.20, -0.24, -0.30, -0.36, -0.46), (-0.06, -0.06, -0.06, -0.07, -0.08),   # y of each joint: front legs
         (0.08, 0.11, 0.16, 0.22, 0.32)]                                               # reach forward, hind legs back


def build(m):
    for side in (1, -1):
        wing(side, m["wing"], m["chitin"])
        _costa(side, m["chitin"])
        for i, ys in enumerate(LEG_Y):
            joints = [geo.mirrored((x, y, z), side) for (x, z), y in zip(CHAIN, ys)]
            _leg(f"leg{i}{side:+d}", side, joints, m["chitin"], m["horn"])


def _wing_point(x, y, side):
    return Vector((x * side, y, WING_Z + DIHEDRAL * (x - LEAD[0][0])))


def wing(side, membrane, rim):
    """The outline as one face, a rim of shell inset round it (the game's wings are edged green), the inside
    triangulated, given its thickness."""
    bm = bmesh.new()
    face = bm.faces.new([bm.verts.new(_wing_point(x, y, side)) for x, y in LEAD + TRAIL])
    bmesh.ops.recalc_face_normals(bm, faces=[face])
    if face.normal.z < 0:
        face.normal_flip()
    ring = bmesh.ops.inset_region(bm, faces=[face], thickness=RIM, use_even_offset=True)["faces"]
    for f in ring:
        f.material_index = 1
    bmesh.ops.triangulate(bm, faces=[face], quad_method='BEAUTY', ngon_method='BEAUTY')
    data = bpy.data.meshes.new(f"wing{side:+d}")
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(data.name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(membrane)
    data.materials.append(rim)
    solid = obj.modifiers.new("thickness", 'SOLIDIFY')
    solid.thickness, solid.offset = WING_THICK, 0.0          # the edge walls take the rim's material
    return obj


def _costa(side, material):
    """The thick leading vein and one long vein behind it, standing on the membrane."""
    lead = [_wing_point(x, y + 0.012, side) + Vector((0, 0, 0.004)) for x, y in LEAD[:-1]]
    geo.tube(f"costa{side:+d}", lead, geo.taper(lead, 0.032, 0.013), material, sides=6)
    vein = [_wing_point(x, y, side) + Vector((0, 0, WING_THICK * 0.6)) for x, y in VEIN]
    geo.tube(f"vein{side:+d}", vein, [(r, r * 0.6) for r in geo.taper(vein, 0.022, 0.008)], material, sides=5)


def _leg(name, side, joints, chitin, horn):
    """One segment per bone of the chain: the coxa (leg1), the femur rising to the knee (leg2), the tibia falling to
    the ankle (leg3), the tarsus and a hooked claw (leg4); amber bands at each joint, on the bone below it."""
    hip, coxa, knee, ankle, foot = joints
    geo.tube(name + "_coxa", [hip, coxa], [0.058, 0.05], chitin, sides=6)
    femur = geo.catmull([coxa, (coxa + knee) / 2 + Vector((0.03 * side, 0, 0.0)), knee], 2)
    geo.tube(name + "_femur", femur, geo.taper(femur, 0.05, 0.042), chitin, sides=6)
    tibia = geo.catmull([knee, (knee + ankle) / 2 + Vector((0.03 * side, 0, 0.02)), ankle], 1)
    geo.tube(name + "_tibia", tibia, geo.taper(tibia, 0.042, 0.032), chitin, sides=6)
    tarsus = geo.catmull([ankle, (ankle + foot) / 2 + Vector((0, 0, 0.03)), foot], 1)
    geo.tube(name + "_tarsus", tarsus, geo.taper(tarsus, 0.032, 0.015), chitin, sides=5)
    claw = foot + Vector((-0.05 * side, 0.0, -0.05))
    geo.tube(name + "_claw", [foot, claw], [0.015, 0.003], horn, sides=4, cap_start=False)
    geo.knob(name + "_hipband", coxa, (0.052, 0.052, 0.052), horn, segments=6, rings=4)
    geo.knob(name + "_knee", knee, (0.056, 0.056, 0.056), horn, segments=6, rings=4)
    geo.knob(name + "_ankle", ankle, (0.042, 0.042, 0.042), horn, segments=6, rings=4)

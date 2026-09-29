"""The Skeleton Crossbowman's crossbow (Elite Creatures Pack), made of bones like the game's Spinesnap bow and bone tower
shield: a femur for the stock, its knee knuckles the butt and its ball the seat of the nut; a row of vertebrae for the
fore-stock, bowed a little like a real back and threaded on a long bone through their middles, the bolt riding their
tops (a notch carved in each) and spanning the bow; two ribs lashed on with sinew for the prod; a vertebra for the nut; a long
finger bone for the trigger lever; a jawbone, teeth and all, for the stirrup. The string is its own asset
(ecp_xbow_string), because the mod stretches it from each prod tip to the nut, the drawing fingers or straight across;
the bolts are the game's own bone bolt, placed by the mod.

The origin is the middle of the grip, where the right fist closes round the stock's wrist; the stock points forward
(-Y, Unity +Z) and its groove faces up (+Z). The points the mod and the workshop's Unity side use (Blender axes; Unity:
x' = -x, y' = z, z' = -y) are the constants below; the fore-stock's middle at SUPPORT is where the left fist holds it.
Look: the game's bone items (Characters/Skeleton textures, GameElements/Items/misc/LargeBone, weapons/BowSpineSnap,
shields/ShieldBoneTower) - muted beige, darker brown in the hollows, big soft shapes.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

from workshop import materials, shapes

TEXTURE_SIZE = 512
AO_STRENGTH = 0.7

TIPS = ((-0.352, -0.398, 0.024), (0.352, -0.398, 0.024))   # where the string is tied, at the ribs' knobbed ends
NUT = (0.0, -0.100, 0.036)                                   # the string's notch when spanned; the bolt's nock end
MUZZLE = (0.0, -0.560, 0.032)                                # the front of the groove, where the bolt leaves
REST = (0.0, -0.398, 0.024)                                  # the string's middle when let go: straight between the tips
SUPPORT = (0.0, -0.220, -0.010)                              # the fore-stock's middle (on the spine's bow), in the left fist

# The femur, butt to ball: (y, width, height, centre height). The knee end is the butt and drops like a stock.
# Half as long behind the grip as it was (2026-09-29, the user): the butt 15 cm behind the grip instead of 30.
FEMUR = [(0.134, 0.052, 0.060, -0.030), (0.117, 0.036, 0.044, -0.026), (0.080, 0.029, 0.034, -0.017),
         (0.040, 0.027, 0.031, -0.009), (0.000, 0.027, 0.031, -0.004), (-0.060, 0.030, 0.033, -0.001),
         (-0.100, 0.026, 0.028, 0.002)]
VERTEBRAE = [-0.145 - 0.040 * i for i in range(11)]         # centres of the fore-stock's vertebrae, back to front
SAG = 0.022                                                  # how far the spine bows down in its middle
CORE = (-0.110, -0.580)                                      # the bone threaded through the vertebrae, back to front
RIB = [((0.018, -0.472, 0.011), 0.013, 0.0065), ((0.12, -0.462, 0.012), 0.012, 0.006), ((0.25, -0.437, 0.017), 0.010, 0.005),
       ((0.352, -0.398, 0.024), 0.007, 0.0045)]              # (point, half height, half thickness), centre to tip
LEVER = [((0.0, -0.085, -0.020), 0.0055), ((0.0, -0.060, -0.036), 0.005), ((0.0, 0.020, -0.044), 0.0045),
         ((0.0, 0.080, -0.052), 0.004)]
JAW = [((-0.040, -0.528, -0.004), 0.011, 0.006), ((-0.054, -0.595, -0.010), 0.012, 0.0065), ((0.0, -0.648, -0.014), 0.012, 0.007),
       ((0.054, -0.595, -0.010), 0.012, 0.0065), ((0.040, -0.528, -0.004), 0.011, 0.006)]


def build():
    bone, spine, rib, teeth, sinew, leather, groove = _bone(), _spine(), _rib(), _teeth(), _sinew(), _leather(), _groove()
    _femur(bone)
    for i, y in enumerate(VERTEBRAE):
        _vertebra(f"vertebra_{i}", y, spine, groove)
    _core(bone)
    _nut(spine, groove)
    _tube("lever", LEVER, bone)
    for end in (LEVER[0][0], LEVER[-1][0]):
        _knob(f"lever_knob_{end[1]}", end, 0.008, bone)
    for side in (-1, 1):
        _band(f"rib_{side}", [((side * p[0], p[1], p[2]), h, t) for p, h, t in RIB], rib)
        _knob(f"rib_head_{side}", TIPS[0 if side < 0 else 1], 0.011, rib)
    _band("jaw", JAW, bone)
    _teeth_row(teeth)
    _wrap("prod_lashing", -0.492, -0.450, (0.034, 0.028), _spine_z(-0.471), 0.0075, 0.0038, sinew)
    _wrap("spine_lashing", -0.150, -0.112, (0.030, 0.027), 0.002, 0.0070, 0.0036, sinew)
    _wrap("jaw_lashing", -0.545, -0.525, (0.029, 0.026), 0.002, 0.0060, 0.0032, sinew)
    _wrap("grip_wrap", -0.030, 0.060, (0.018, 0.021), -0.005, 0.011, 0.0034, leather)
    _to_meshes()


def _femur(material):
    """The shaft, the two knee knuckles at the butt, the ball and a knob of trochanter under the nut."""
    _loft("femur", FEMUR, material)
    for side in (-1, 1):
        _knob(f"condyle_{side}", (side * 0.020, 0.146, -0.034), 0.031, material, squash=(1.0, 0.9, 1.05))
    _knob("femur_ball", (0.0, -0.118, 0.004), 0.025, material)
    _knob("trochanter", (0.017, -0.082, 0.010), 0.013, material)


def _spine_z(y):
    """The spine's middle line: level with the stock at both ends, bowed SAG lower in the middle like a real back."""
    t = (y - VERTEBRAE[0]) / (VERTEBRAE[-1] - VERTEBRAE[0])
    return 0.002 - SAG * math.sin(math.pi * min(max(t, 0.0), 1.0))


def _spine_tilt(y):
    """How far a vertebra at y leans to follow the bow (radians about X)."""
    return math.atan2(_spine_z(y + 0.005) - _spine_z(y - 0.005), 0.01)


def _vertebra(name, y, material, notch):
    """A rounded body on the bowed line, leaning with it: a carved notch on top for the bolt, a spine pointing down and
    back, a nub to each side, all turned with the lean so the spines fan like a real back's."""
    centre, turn = Vector((0.0, y, _spine_z(y))), Matrix.Rotation(_spine_tilt(y), 3, 'X')
    at = lambda offset: centre + turn @ Vector(offset)
    body = _knob(name, centre, 0.022, material, squash=(1.05, 0.72, 0.97))
    body.rotation_euler = (_spine_tilt(y), 0.0, 0.0)
    shapes.box(name + "_notch", (0.006, 0.012, 0.002), at((0.0, 0.0, 0.0195)), (_spine_tilt(y), 0.0, 0.0), notch)
    _cone(name + "_spine", at((0.0, 0.0, -0.014)), at((0.0, 0.010, -0.038)), 0.008, material)
    for side in (-1, 1):
        _cone(f"{name}_side_{side}", at((side * 0.014, 0.0, -0.004)), at((side * 0.036, 0.004, -0.010)), 0.0065, material)


def _core(material):
    """The long bone the vertebrae are threaded on, following their bow, showing between them, knobbed at both ends."""
    ys = [CORE[0] + (CORE[1] - CORE[0]) * i / 16 for i in range(17)]
    _tube("core_bone", [((0.0, y, _spine_z(y)), 0.0085) for y in ys], material)
    for y in CORE:
        _knob(f"core_end_{y}", (0.0, y, _spine_z(y)), 0.0125, material, squash=(1.0, 0.85, 1.0))


def _nut(material, notch):
    """The nut: a vertebra sitting crosswise on the femur's ball, its notch on top holding the string."""
    shapes.cylinder("nut", 0.019, 0.036, (0.0, -0.100, 0.019), (0.0, math.radians(90), 0.0), material, vertices=10)
    _cone("nut_spine", (0.0, -0.094, 0.030), (0.0, -0.060, 0.046), 0.008, material)
    shapes.box("nut_notch", (0.038, 0.007, 0.008), (0.0, -0.097, 0.036), material=notch)


def _teeth_row(material):
    """Teeth along the jawbone's top, smaller towards the back."""
    for i in range(9):
        t = (i - 4) / 4.0
        angle = t * math.radians(80)
        x, y = 0.050 * math.sin(angle), -0.600 - 0.046 * math.cos(angle) + 0.012 * abs(t)
        size = 0.0075 - 0.002 * abs(t)
        shapes.box(f"tooth_{i}", (size, size * 0.9, size * 1.5), (x, y, -0.0005), (0.0, 0.0, -angle), material, bevel=0.0015)


def _knob(name, location, radius, material, squash=(1.0, 1.0, 1.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = squash
    obj.data.materials.append(material)
    return obj


def _cone(name, base, tip, radius, material):
    """A blunt tapering process from `base` to `tip`."""
    direction = Vector(tip) - Vector(base)
    bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=radius, radius2=radius * 0.35, depth=direction.length,
                                    location=Vector(base) + direction / 2)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    obj.data.materials.append(material)
    return obj


def _loft(name, stations, material, sides=12):
    """A solid through rounded sections along Y, capped at both ends."""
    mesh = bmesh.new()
    rings = []
    for y, width, height, centre in stations:
        ring = []
        for i in range(sides):
            angle = 2 * math.pi * i / sides
            c, s = math.cos(angle), math.sin(angle)
            ring.append(mesh.verts.new((width / 2 * _round(c), y, centre + height / 2 * _round(s))))
        rings.append(ring)
    _skin(mesh, rings, sides)
    return _object(name, mesh, material)


def _band(name, points, material, sides=8):
    """A flattened bone swept along the points: each (point, half height, half thickness), height kept upright."""
    mesh = bmesh.new()
    rings = []
    for i, (co, high, thick) in enumerate(points):
        ahead = Vector(points[min(i + 1, len(points) - 1)][0]) - Vector(points[max(i - 1, 0)][0])
        side = ahead.normalized().cross(Vector((0, 0, 1))).normalized()
        ring = []
        for k in range(sides):
            angle = 2 * math.pi * k / sides
            ring.append(mesh.verts.new(Vector(co) + side * thick * math.cos(angle) + Vector((0, 0, high * math.sin(angle)))))
        rings.append(ring)
    _skin(mesh, rings, sides)
    return _object(name, mesh, material)


def _skin(mesh, rings, sides):
    for a, b in zip(rings, rings[1:]):
        for i in range(sides):
            j = (i + 1) % sides
            mesh.faces.new((a[i], a[j], b[j], b[i]))
    mesh.faces.new(list(reversed(rings[0])))
    mesh.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)


def _object(name, mesh, material):
    data = bpy.data.meshes.new(name)
    mesh.to_mesh(data)
    mesh.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    return obj


def _round(v):
    """Circle towards a square, a little: bone shafts are rounded, not round."""
    return math.copysign(abs(v) ** 0.75, v)


def _tube(name, points, material, resolution=2):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = 1.0
    curve.bevel_resolution = 1
    curve.use_fill_caps = True
    curve.resolution_u = resolution
    spline = curve.splines.new('NURBS')
    spline.points.add(len(points) - 1)
    for point, (co, radius) in zip(spline.points, points):
        point.co = (*co, 1.0)
        point.radius = radius
    spline.use_endpoint_u = True
    spline.order_u = 3
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(material)
    return obj


def _wrap(name, y0, y1, radii, centre, pitch, thickness, material):
    """Turns of sinew or rawhide round the stock (along Y) from y0 to y1, `pitch` metres per turn, on an ellipse of
    `radii` (across, up) round the stock's centre height."""
    steps = max(10, int((y1 - y0) / pitch * 6))
    points = []
    for i in range(steps + 1):
        y = y0 + (y1 - y0) * i / steps
        angle = 2 * math.pi * (y - y0) / pitch
        points.append(((radii[0] * math.cos(angle), y, centre + radii[1] * math.sin(angle)), thickness))
    return _tube(name, points, material, resolution=1)


def _to_meshes():
    curves = [o for o in bpy.context.scene.objects if o.type == 'CURVE']
    for obj in bpy.context.scene.objects:
        obj.select_set(obj in curves)
    bpy.context.view_layer.objects.active = curves[0]
    bpy.ops.object.convert(target='MESH')


def _mottled(name, dark, light, scale, roughness, bump=0.0, stretch=None):
    """Old bone: a pale base mottled darker, as the game paints its skeletons and bone items."""
    mat = bpy.data.materials.new(name)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = materials.principled(mat)
    bsdf.inputs['Roughness'].default_value = roughness
    coords = nodes.new('ShaderNodeTexCoord')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = 6.0
    if stretch:
        mapping = nodes.new('ShaderNodeMapping')
        mapping.inputs['Scale'].default_value = stretch
        links.new(coords.outputs['Object'], mapping.inputs['Vector'])
        links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    else:
        links.new(coords.outputs['Object'], noise.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[1].position = 0.3, 0.7
    ramp.color_ramp.elements[0].color = (*dark, 1.0)
    ramp.color_ramp.elements[1].color = (*light, 1.0)
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    if bump > 0:
        node = nodes.new('ShaderNodeBump')
        node.inputs['Strength'].default_value = bump
        node.inputs['Distance'].default_value = 0.003
        links.new(noise.outputs['Fac'], node.inputs['Height'])
        links.new(node.outputs['Normal'], bsdf.inputs['Normal'])
    return mat


def _bone():
    """Femur, lever and jaw: beige, brown in the hollows, grain along the length."""
    return _mottled("xbow_bone", (0.20, 0.145, 0.085), (0.53, 0.45, 0.32), 55.0, 0.75, bump=0.35, stretch=(1.0, 0.25, 1.0))


def _spine():
    """The vertebrae, a little yellower, like the Spinesnap bow's."""
    return _mottled("xbow_spine", (0.22, 0.16, 0.07), (0.56, 0.47, 0.28), 90.0, 0.7, bump=0.45)


def _rib():
    return _mottled("xbow_rib", (0.26, 0.20, 0.12), (0.60, 0.53, 0.40), 70.0, 0.7, bump=0.3, stretch=(0.3, 1.0, 1.0))


def _teeth():
    return _mottled("xbow_teeth", (0.40, 0.34, 0.22), (0.72, 0.66, 0.52), 120.0, 0.55)


def _sinew():
    return _mottled("xbow_sinew", (0.10, 0.075, 0.05), (0.27, 0.21, 0.14), 300.0, 0.7)


def _leather():
    return _mottled("xbow_leather", (0.06, 0.03, 0.013), (0.17, 0.09, 0.04), 200.0, 0.65)


def _groove():
    return materials.flat("xbow_groove", (0.03, 0.02, 0.012), roughness=0.9)

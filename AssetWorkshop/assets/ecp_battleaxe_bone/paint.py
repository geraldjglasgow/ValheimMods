"""The battleaxe's materials: node recipes the pipeline bakes into one small atlas, painted the way the game paints.

The game's bone (Skeleton_d, Boneshield_d, SpineSnap_d: 64 to 128 px, point filtered) is flat colour in big soft
blotches: sand to cream, rusty-brown stains, dark painted crack lines, a paler worn crown on edges, no fine grain.
Colours below are linear RGB sampled from those textures; the leather from the Spinesnap's grip, the rawhide's red
from the blackmetal battleaxe's wrap. The baked ambient occlusion (AO_STRENGTH in model.py) darkens the hollows.
"""
import math

import bpy

BONE = ((0.225, 0.140, 0.080), (0.341, 0.242, 0.153), (0.512, 0.399, 0.270))       # Skeleton_d 10/50/90 %
BLADE = ((0.190, 0.112, 0.058), (0.300, 0.203, 0.118), (0.440, 0.330, 0.200))      # older, browner
GROUND = ((0.380, 0.300, 0.190), (0.540, 0.450, 0.310), (0.640, 0.560, 0.410))     # the ground edge: fresh bone
STAIN = (0.190, 0.090, 0.040)                                                        # rusty brown
JOINT = (0.150, 0.075, 0.035)                                                        # the brown at a bone's ends
CRACK = (0.050, 0.025, 0.012)
LEATHER = ((0.014, 0.007, 0.006), (0.040, 0.019, 0.015), (0.075, 0.038, 0.026))
RAWHIDE = ((0.040, 0.006, 0.003), (0.110, 0.018, 0.008), (0.175, 0.045, 0.020))
IVORY = ((0.230, 0.160, 0.090), (0.520, 0.460, 0.340), (0.700, 0.660, 0.540))
BUTT, TOP = -0.30, 1.52                                                              # the haft's ends, for JOINT


def bone():
    mat, out = _new('bone_haft', 0.8)
    contrast = ((0.190, 0.112, 0.060), BONE[1], (0.560, 0.440, 0.300))    # the Skeleton's spread on a thin part
    colour = _blotches(mat, contrast, (1.0, 1.0, 0.30), 15.0)  # blotches drawn out along the bone
    colour = _stains(mat, colour, 0.45, (1.0, 1.0, 0.35))
    colour = _joints(mat, colour)
    _finish(mat, out, _lift(mat, colour, BONE[2], edges=False))
    return mat


def blade():
    mat, out = _new('bone_blade', 0.8)
    colour = _blotches(mat, BLADE, (0.55, 1.0, 1.0), 16.0)     # grain from the root out to the edge
    colour = _stains(mat, colour, 0.75, (0.6, 1.0, 1.0))
    colour = _root_dirt(mat, colour)
    colour = _cracks(mat, colour)
    _finish(mat, out, _lift(mat, colour, BLADE[2]))
    return mat


def ground_edge():
    mat, out = _new('bone_ground_edge', 0.7)
    colour = _blotches(mat, GROUND, (0.5, 1.0, 1.0), 14.0)
    colour = _pores(mat, colour)
    _finish(mat, out, colour)
    return mat


def leather():
    mat, out = _new('grip_leather', 0.75)
    strap = _spiral(mat, 0.034)
    colour = _ramp(mat, strap, [(0.0, LEATHER[0]), (0.16, LEATHER[0]), (0.24, LEATHER[1]), (0.62, LEATHER[2]),
                                (1.0, LEATHER[1])])
    _finish(mat, out, _mix(mat, colour, _blotches(mat, LEATHER[1:], (1, 1, 1), 20.0, low=0.9), 'MULTIPLY', 0.35))
    return mat


def rawhide():
    """Turns of rawhide strap, each a little crooked: a dark seam between turns, the strap lighter in its middle."""
    mat, out = _new('lashing_rawhide', 0.7)
    turn = _spiral(mat, 0.026)
    strap = _ramp(mat, turn, [(0.0, RAWHIDE[0]), (0.13, RAWHIDE[0]), (0.22, RAWHIDE[1]), (0.55, RAWHIDE[2]),
                              (0.92, RAWHIDE[1]), (1.0, RAWHIDE[0])])
    _finish(mat, out, _mix(mat, strap, _blotches(mat, RAWHIDE, (1, 1, 1), 30.0, low=0.8), 'MULTIPLY', 1.0))
    return mat


def ivory():
    mat, out = _new('tusk_ivory', 0.55)
    along = _math(mat, 'MULTIPLY', _axis(mat, 'X'), -3.7)      # 0 at the lashing, 1 at the tip (x = -0.27)
    colour = _ramp(mat, along, [(0.0, IVORY[0]), (0.22, IVORY[1]), (0.75, IVORY[2]), (1.0, IVORY[2])])
    _finish(mat, out, _mix(mat, colour, _blotches(mat, IVORY, (1, 1, 1), 25.0, low=0.92), 'MULTIPLY', 0.5))
    return mat


# --- recipes -------------------------------------------------------------------------------------------------

def _blotches(mat, palette, stretch, scale, low=None):
    """Two-tone soft blotches between the palette's mid and light, with a few dark flecks; `stretch` scales the
    coordinates per axis (below 1: blotches drawn out along that axis)."""
    fac = _noise(mat, scale, 2.0, stretch)
    if low is not None:
        return _ramp(mat, fac, [(0.3, tuple(c * low for c in (1, 1, 1))), (0.7, (1.0, 1.0, 1.0))])
    dark, mid, light = palette
    return _ramp(mat, fac, [(0.37, dark), (0.46, mid), (0.53, mid), (0.62, light)])


def _stains(mat, colour, amount, stretch):
    """Rusty-brown stains over part of the bone (amount 0 to 1)."""
    fac = _noise(mat, 6.0, 1.5, stretch, seed_offset=(7.1, 3.3, 1.9))
    mask = _ramp(mat, fac, [(0.60 - amount * 0.10, (0, 0, 0)), (0.66 - amount * 0.08, (1, 1, 1))])
    return _mix(mat, colour, _rgb(mat, STAIN), 'MIX', mask, factor_socket=True, strength=0.65)


def _joints(mat, colour):
    """A long bone is brown towards its ends and cleanest in the middle of the shaft."""
    along = _math(mat, 'MULTIPLY', _math(mat, 'ADD', _axis(mat, 'Z'), -BUTT), 1 / (TOP - BUTT))
    mask = _ramp(mat, along, [(0.0, (1, 1, 1)), (0.12, (0, 0, 0)), (0.84, (0, 0, 0)), (1.0, (1, 1, 1))])
    return _mix(mat, colour, _rgb(mat, JOINT), 'MIX', mask, factor_socket=True, strength=0.6)


def _root_dirt(mat, colour):
    """Grime where the blade goes into the lashing, fading out towards the edge."""
    mask = _ramp(mat, _axis(mat, 'X'), [(0.02, (1, 1, 1)), (0.16, (0, 0, 0))])
    return _mix(mat, colour, _rgb(mat, JOINT), 'MIX', mask, factor_socket=True, strength=0.45)


def _cracks(mat, colour):
    """Thin dark crack lines, broken up so only some of the cell edges show."""
    coords = _coords(mat, (1.0, 1.0, 1.0))
    wobble = _noise_vec(mat, coords, 8.0)
    voronoi = _node(mat, 'ShaderNodeTexVoronoi')
    voronoi.feature = 'DISTANCE_TO_EDGE'
    voronoi.inputs['Scale'].default_value = 6.0
    _link(mat, wobble, voronoi.inputs['Vector'])
    line = _ramp(mat, voronoi.outputs['Distance'], [(0.020, (1, 1, 1)), (0.034, (0, 0, 0))])
    keep = _ramp(mat, _noise(mat, 3.0, 1.0, (1, 1, 1), seed_offset=(2.2, 9.1, 4.4)), [(0.50, (0, 0, 0)),
                                                                                   (0.56, (1, 1, 1))])
    mask = _mix(mat, line, keep, 'MULTIPLY', 1.0)
    return _mix(mat, colour, _rgb(mat, CRACK), 'MIX', mask, factor_socket=True, strength=0.85)


def _pores(mat, colour):
    voronoi = _node(mat, 'ShaderNodeTexVoronoi')
    voronoi.inputs['Scale'].default_value = 38.0
    _link(mat, _coords(mat, (1, 1, 1)), voronoi.inputs['Vector'])
    dots = _ramp(mat, voronoi.outputs['Distance'], [(0.10, (1, 1, 1)), (0.16, (0, 0, 0))])
    return _mix(mat, colour, _rgb(mat, GROUND[0]), 'MIX', dots, factor_socket=True, strength=0.6)


def _spiral(mat, pitch):
    """0 to 1 across each turn of a strap wound up the haft: the turn's fraction from the angle round the bone
    and the height."""
    x = _math(mat, 'ADD', _axis(mat, 'X'), 0.008)
    angle = _math(mat, 'ARCTAN2', _axis(mat, 'Y'), x)
    turns = _math(mat, 'ADD', _math(mat, 'MULTIPLY', angle, 1 / (2 * math.pi)),
                  _math(mat, 'MULTIPLY', _axis(mat, 'Z'), 1 / pitch))
    return _math(mat, 'FRACT', turns)


def _lift(mat, colour, light, edges=True):
    """Worn crowns: convex edges and upward faces a shade lighter, as the game paints its light in. Thin parts
    (the haft) skip the edges: every vertex of a thin tube is convex, so all of it would lift."""
    geometry = _node(mat, 'ShaderNodeNewGeometry')
    lit = colour
    if edges:
        crowns = _ramp(mat, geometry.outputs['Pointiness'], [(0.56, (0, 0, 0)), (0.64, (1, 1, 1))])
        lit = _mix(mat, colour, _rgb(mat, light), 'MIX', crowns, factor_socket=True, strength=0.4)
    facing = _math(mat, 'MULTIPLY_ADD', _separate(mat, geometry.outputs['Normal'], 'Z'), 0.5)   # -1..1 to 0..1
    facing.node.inputs[2].default_value = 0.5
    up = _ramp(mat, facing, [(0.2, (0.84, 0.84, 0.84)), (0.9, (1.0, 1.0, 1.0))])
    return _mix(mat, lit, up, 'MULTIPLY', 1.0)


# --- node plumbing -------------------------------------------------------------------------------------------

def _new(name, roughness):
    mat = bpy.data.materials.new(name)
    bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Roughness'].default_value = roughness
    return mat, bsdf.inputs['Base Color']


def _finish(mat, target, colour):
    _link(mat, colour, target)


def _node(mat, kind):
    return mat.node_tree.nodes.new(kind)


def _link(mat, source, target):
    mat.node_tree.links.new(source, target)


def _coords(mat, stretch, seed_offset=(0.0, 0.0, 0.0)):
    coords = _node(mat, 'ShaderNodeTexCoord')
    mapping = _node(mat, 'ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = stretch
    mapping.inputs['Location'].default_value = seed_offset
    _link(mat, coords.outputs['Object'], mapping.inputs['Vector'])
    return mapping.outputs['Vector']


def _noise(mat, scale, detail, stretch, seed_offset=(0.0, 0.0, 0.0)):
    noise = _node(mat, 'ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = detail
    noise.inputs['Roughness'].default_value = 0.45
    _link(mat, _coords(mat, stretch, seed_offset), noise.inputs['Vector'])
    return noise.outputs['Fac']


def _noise_vec(mat, coords, scale):
    """The coordinates pushed about by a noise, so cell edges wander like cracks."""
    noise = _node(mat, 'ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    _link(mat, coords, noise.inputs['Vector'])
    push = _node(mat, 'ShaderNodeVectorMath')
    push.operation = 'MULTIPLY_ADD'
    _link(mat, noise.outputs['Color'], push.inputs[0])
    push.inputs[1].default_value = (0.05, 0.05, 0.05)
    _link(mat, coords, push.inputs[2])
    return push.outputs['Vector']


def _ramp(mat, fac, stops):
    ramp = _node(mat, 'ShaderNodeValToRGB')
    elements = ramp.color_ramp.elements
    while len(elements) < len(stops):
        elements.new(0.5)
    for element, (position, rgb) in zip(elements, stops):
        element.position, element.color = position, (*rgb, 1.0)
    _link(mat, fac, ramp.inputs['Fac'])
    return ramp.outputs['Color']


def _rgb(mat, rgb):
    node = _node(mat, 'ShaderNodeRGB')
    node.outputs['Color'].default_value = (*rgb, 1.0)
    return node.outputs['Color']


def _mix(mat, a, b, blend, factor, factor_socket=False, strength=1.0):
    """a blended with b; factor is a number, or a colour socket (its brightness) when factor_socket, scaled by
    strength."""
    node = _node(mat, 'ShaderNodeMix')
    node.data_type, node.blend_type = 'RGBA', blend
    if factor_socket:
        _link(mat, _math(mat, 'MULTIPLY', _bw(mat, factor), strength), node.inputs['Factor'])
    else:
        node.inputs['Factor'].default_value = factor
    _link(mat, a, _socket(node, 'A'))
    _link(mat, b, _socket(node, 'B'))
    return next(s for s in node.outputs if s.type == 'RGBA')


def _socket(node, name):
    return next(s for s in node.inputs if s.name == name and s.type == 'RGBA')


def _bw(mat, colour):
    node = _node(mat, 'ShaderNodeRGBToBW')
    _link(mat, colour, node.inputs['Color'])
    return node.outputs['Val']


def _math(mat, operation, a, b=None):
    node = _node(mat, 'ShaderNodeMath')
    node.operation = operation
    for socket, value in zip(node.inputs, (a, b)):
        if value is None:
            continue
        if isinstance(value, (int, float)):
            socket.default_value = value
        else:
            _link(mat, value, socket)
    return node.outputs['Value']


def _axis(mat, axis):
    return _separate(mat, _node(mat, 'ShaderNodeTexCoord').outputs['Object'], axis)


def _separate(mat, vector, axis):
    node = _node(mat, 'ShaderNodeSeparateXYZ')
    _link(mat, vector, node.inputs['Vector'])
    return node.outputs[axis]

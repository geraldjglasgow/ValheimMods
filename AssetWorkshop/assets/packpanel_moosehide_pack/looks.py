"""The Moosehide Pack's own procedural materials, baked by the pipeline into one 256 atlas: warm dark-brown moose
hide with fine hair streaks for the bag (a darker base panel below a sinew seam), a darker, longer-haired hide for
the lid flap with pale worn edges, a cream-white winter fur for the ruff (grey-brown at the roots, bright at the
tips), gold, pale antler (brown at the cut base, ivory to the tip), orange-red moose sinew and near-black strap
leather. Nothing here reads a game texture.

The atlas is small (256 pixels, about 6 mm a pixel on this pack), so the patterns are broad: mottling, hair streaks
and worn edges, with the fine hair in the normal map. Patterns are laid out on the player-frame position (object
coordinates plus the pivot). Colours are linear RGB. The Graph writer is the Trollhide Backpack's.
"""
import bpy

HIDE = ((0.040, 0.019, 0.010), (0.118, 0.054, 0.024), (0.205, 0.105, 0.050), (0.300, 0.185, 0.100))
FLAP = ((0.026, 0.012, 0.007), (0.085, 0.038, 0.017), (0.165, 0.086, 0.040), (0.330, 0.215, 0.125))
BASE = (0.022, 0.012, 0.008)
FUR = ((0.170, 0.128, 0.090), (0.470, 0.395, 0.285), (0.760, 0.690, 0.545), (0.900, 0.865, 0.770))
GOLD_DARK, GOLD, GOLD_BRIGHT = (0.17, 0.065, 0.008), (0.56, 0.29, 0.032), (0.82, 0.58, 0.18)
ANTLER_BASE, ANTLER, ANTLER_TIP = (0.300, 0.185, 0.120), (0.700, 0.600, 0.425), (0.880, 0.835, 0.730)
SINEW_DARK, SINEW = (0.150, 0.028, 0.008), (0.440, 0.095, 0.026)
STRAP_DARK, STRAP_MID = (0.012, 0.007, 0.004), (0.048, 0.024, 0.011)


class Graph:
    """A small writer for one material's node graph, ending in its Principled BSDF."""

    def __init__(self, name, pivot, roughness=0.8):
        self.mat = bpy.data.materials.new(name)
        self.tree = self.mat.node_tree
        self.bsdf = next(n for n in self.tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
        self.bsdf.inputs['Roughness'].default_value = roughness
        self.pivot = tuple(pivot)

    def node(self, kind, **props):
        node = self.tree.nodes.new(kind)
        for key, value in props.items():
            setattr(node, key, value)
        return node

    def feed(self, socket, value):
        if isinstance(value, bpy.types.NodeSocket):
            self.tree.links.new(value, socket)
        else:
            socket.default_value = value

    def position(self):
        """Where a point sits in the player's frame, in metres."""
        coords = self.node('ShaderNodeTexCoord').outputs['Object']
        return self.vmath('ADD', coords, self.pivot)

    def vmath(self, op, a, b):
        node = self.node('ShaderNodeVectorMath', operation=op)
        self.feed(node.inputs[0], a)
        self.feed(node.inputs[1], b)
        return node.outputs['Value' if op in ('LENGTH', 'DOT_PRODUCT') else 'Vector']

    def math(self, op, a, b=0.0):
        node = self.node('ShaderNodeMath', operation=op)
        self.feed(node.inputs[0], a)
        self.feed(node.inputs[1], b)
        return node.outputs[0]

    def axis(self, vector, name):
        node = self.node('ShaderNodeSeparateXYZ')
        self.feed(node.inputs[0], vector)
        return node.outputs[name]

    def noise(self, vector, scale, detail=3.0, roughness=0.5):
        node = self.node('ShaderNodeTexNoise')
        self.feed(node.inputs['Vector'], vector)
        node.inputs['Scale'].default_value = scale
        node.inputs['Detail'].default_value = detail
        node.inputs['Roughness'].default_value = roughness
        return node.outputs['Fac']

    def voronoi(self, vector, scale):
        node = self.node('ShaderNodeTexVoronoi')
        self.feed(node.inputs['Vector'], vector)
        node.inputs['Scale'].default_value = scale
        return node.outputs['Distance']

    def attribute(self, name):
        return self.node('ShaderNodeAttribute', attribute_name=name, attribute_type='GEOMETRY').outputs['Fac']

    def smooth(self, value, low, high, invert=False):
        """0 below `low`, 1 above `high` (or the other way round), smoothstepped between."""
        node = self.node('ShaderNodeMapRange', interpolation_type='SMOOTHSTEP')
        self.feed(node.inputs['Value'], value)
        node.inputs['From Min'].default_value, node.inputs['From Max'].default_value = low, high
        node.inputs['To Min'].default_value, node.inputs['To Max'].default_value = (1.0, 0.0) if invert else (0.0, 1.0)
        return node.outputs['Result']

    def ramp(self, fac, stops):
        node = self.node('ShaderNodeValToRGB')
        elements = node.color_ramp.elements
        while len(elements) < len(stops):
            elements.new(0.5)
        for element, (position, colour) in zip(elements, stops):
            element.position, element.color = position, (*colour, 1.0)
        self.feed(node.inputs['Fac'], fac)
        return node.outputs['Color']

    def mix(self, fac, a, b):
        node = self.node('ShaderNodeMix', data_type='RGBA', blend_type='MIX')
        colours = [s for s in node.inputs if s.type == 'RGBA']
        self.feed(node.inputs[0], fac)
        self.feed(colours[0], a if isinstance(a, bpy.types.NodeSocket) else (*a, 1.0))
        self.feed(colours[1], b if isinstance(b, bpy.types.NodeSocket) else (*b, 1.0))
        return next(s for s in node.outputs if s.type == 'RGBA')

    def finish(self, colour, height, strength, distance=0.004):
        self.feed(self.bsdf.inputs['Base Color'], colour)
        bump = self.node('ShaderNodeBump')
        bump.inputs['Strength'].default_value = strength
        bump.inputs['Distance'].default_value = distance
        self.feed(bump.inputs['Height'], height)
        self.feed(self.bsdf.inputs['Normal'], bump.outputs['Normal'])
        return self.mat


def _hair(g, p, tones, stretch, name_wear="wear"):
    """Shared by both hides: mottled tones, hair streaks along the stretched axis, pale where worn.
    Returns (colour, height)."""
    dark, mid, streak, edge = tones
    colour = g.ramp(g.noise(p, 4.0, 4.0, 0.6), [(0.30, dark), (0.70, mid)])
    hair = g.noise(g.vmath('MULTIPLY', p, stretch), 1.0, 3.0)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.52, 0.72), 0.28), colour, streak)
    patches = g.smooth(g.voronoi(p, 7.0), 0.10, 0.30, True)
    colour = g.mix(g.math('MULTIPLY', patches, 0.30), colour, dark)
    worn = g.smooth(g.math('MULTIPLY', g.attribute(name_wear), g.math('ADD', g.noise(p, 24.0, 3.0), 0.35)), 0.45, 0.8)
    colour = g.mix(g.math('MULTIPLY', worn, 0.8), colour, edge)
    height = g.math('ADD', g.math('MULTIPLY', hair, 0.8), g.math('MULTIPLY', g.noise(p, 11.0, 3.0), 0.6))
    return colour, height


def moose_hide(pivot, base_z):
    """The bag: warm dark-brown moose hide, hair running down it; below base_z a darker base panel, joined by a
    band of orange sinew stitching."""
    g = Graph("pack_moose_hide", pivot, 0.85)
    p = g.position()
    colour, height = _hair(g, p, HIDE, (45.0, 45.0, 14.0))
    z = g.axis(p, "Z")
    below = g.smooth(z, base_z - 0.004, base_z - 0.001, invert=True)
    colour = g.mix(g.math('MULTIPLY', below, 0.85), colour, BASE)
    seam = g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', z, base_z + 0.006)), 0.003, 0.006, invert=True)
    dash = g.math('LESS_THAN', g.math('FRACT', g.math('DIVIDE', g.math('ADD', g.axis(p, "X"), g.axis(p, "Y")),
                                                     0.018)), 0.6)
    colour = g.mix(g.math('MULTIPLY', seam, dash), colour, SINEW)
    height = g.math('ADD', height, g.math('MULTIPLY', seam, 0.6))
    return g.finish(colour, height, 0.35)


def flap_hide(pivot):
    """The lid: darker hide, longer hair running down the back, pale where the edge is worn."""
    g = Graph("pack_flap_hide", pivot, 0.9)
    p = g.position()
    colour, height = _hair(g, p, FLAP, (40.0, 40.0, 10.0))
    return g.finish(colour, height, 0.45)


def fur(pivot):
    """Winter fur: cream-white, grey-brown at the roots (the 'root' attribute, 1 against the bag), clumped."""
    g = Graph("pack_fur", pivot, 0.95)
    p = g.position()
    root = g.attribute("root")
    clumps = g.noise(p, 38.0, 4.0, 0.65)
    tone = g.math('SUBTRACT', g.math('ADD', g.math('MULTIPLY', clumps, 0.9), 0.35), g.math('MULTIPLY', root, 0.75))
    colour = g.ramp(tone, [(0.05, FUR[0]), (0.35, FUR[1]), (0.62, FUR[2]), (0.85, FUR[3])])
    strands = g.noise(g.vmath('MULTIPLY', p, (140.0, 140.0, 140.0)), 1.0, 2.0)
    height = g.math('ADD', g.math('MULTIPLY', clumps, 1.0), g.math('MULTIPLY', strands, 0.5))
    return g.finish(colour, height, 0.6, distance=0.006)


def gold(pivot):
    """Worked gold: deep orange-gold with brighter hammered spots, darker in the grain."""
    g = Graph("pack_gold", pivot, 0.3)
    p = g.position()
    colour = g.ramp(g.noise(p, 55.0, 3.0), [(0.30, GOLD_DARK), (0.52, GOLD), (0.74, GOLD_BRIGHT)])
    return g.finish(colour, g.noise(p, 90.0, 2.0), 0.12)


def antler(pivot):
    """Antler: brown and rough at the cut base ('grade' 0), pale ivory along the tine, near white at the tip (1)."""
    g = Graph("pack_antler", pivot, 0.55)
    p = g.position()
    grade = g.math('ADD', g.attribute("grade"), g.math('MULTIPLY', g.math('SUBTRACT', g.noise(p, 60.0, 3.0), 0.5),
                                                       0.25))
    colour = g.ramp(grade, [(0.08, ANTLER_BASE), (0.40, ANTLER), (0.92, ANTLER_TIP)])
    ridges = g.noise(g.vmath('MULTIPLY', p, (260.0, 260.0, 260.0)), 1.0, 2.0)
    return g.finish(colour, ridges, 0.35)


def sinew(pivot):
    g = Graph("pack_sinew", pivot, 0.6)
    p = g.position()
    colour = g.ramp(g.noise(p, 70.0, 3.0), [(0.30, SINEW_DARK), (0.62, SINEW)])
    return g.finish(colour, g.noise(p, 200.0, 2.0), 0.3)


def strap_leather(pivot):
    g = Graph("pack_strap", pivot, 0.65)
    p = g.position()
    colour = g.ramp(g.noise(p, 35.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    return g.finish(colour, g.noise(p, 160.0, 2.0), 0.2)

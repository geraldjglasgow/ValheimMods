"""The Lox Hauler's own procedural materials, baked by the pipeline into one atlas: shaggy brown lox fur for the load
(locks flowing down, dark between them, tan at the tips), pale lox hide for the bedroll's outside (whose ends show
the rolled layers of hide and fur), off-white linen for the lashings, dark forged black metal for the frame, brown
strap leather. Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot). The atlas is small (256), so
every pattern is kept broad: locks of fur a couple of centimetres across, no fine weave. Colours are linear RGB.
"""
import math

import bpy

FUR = ((0.040, 0.014, 0.005), (0.262, 0.089, 0.019), (0.571, 0.296, 0.095))       # dark, mid, tips
HIDE_PALE, HIDE_MID, HIDE_CREASE = (0.507, 0.342, 0.181), (0.319, 0.181, 0.078), (0.107, 0.051, 0.019)
GAP = (0.012, 0.007, 0.004)
LINEN, LINEN_SHADE, GRIME = (0.49, 0.42, 0.285), (0.29, 0.23, 0.135), (0.13, 0.095, 0.05)
METAL_DARK, METAL_MID, METAL_WORN = (0.016, 0.019, 0.018), (0.046, 0.056, 0.051), (0.150, 0.165, 0.155)
STRAP_DARK, STRAP_MID = (0.028, 0.012, 0.005), (0.080, 0.034, 0.012)


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

    def smooth(self, value, low, high, invert=False):
        """0 below `low`, 1 above `high` (or the other way round), smoothstepped between."""
        node = self.node('ShaderNodeMapRange', interpolation_type='SMOOTHSTEP')
        self.feed(node.inputs['Value'], value)
        node.inputs['From Min'].default_value, node.inputs['From Max'].default_value = low, high
        node.inputs['To Min'].default_value, node.inputs['To Max'].default_value = (1.0, 0.0) if invert else (0.0, 1.0)
        return node.outputs['Result']

    def ramp(self, fac, stops, constant=False):
        node = self.node('ShaderNodeValToRGB')
        elements = node.color_ramp.elements
        while len(elements) < len(stops):
            elements.new(0.5)
        for element, (position, colour) in zip(elements, stops):
            element.position, element.color = position, (*colour, 1.0)
        node.color_ramp.interpolation = 'CONSTANT' if constant else 'LINEAR'
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


def fur(pivot, name, tones, stretch):
    """Shaggy fur in clumps: the surface is split into locks (Voronoi cells, stretched: small stretch = long locks
    along that axis, wavered by noise), each lock pale towards its lower tip and dark in the gaps between locks, over
    broad dark and mid patches, with fine hair streaks along the locks."""
    dark, mid, tips = tones
    g = Graph(name, pivot, 0.95)
    p = g.position()
    waver = g.vmath('MULTIPLY', _colour_noise(g, p, 9.0), (0.9, 0.9, 0.9))
    q = g.vmath('ADD', g.vmath('MULTIPLY', p, stretch), waver)
    cell = g.node('ShaderNodeTexVoronoi')
    g.feed(cell.inputs['Vector'], q)
    cell.inputs['Scale'].default_value = 1.0
    gap = g.smooth(cell.outputs['Distance'], 0.42, 0.78)
    below = g.math('SUBTRACT', g.axis(cell.outputs['Position'], "Z"), g.axis(q, "Z"))      # > 0: lower in the lock
    tip = g.math('MULTIPLY', g.smooth(below, -0.1, 0.5), g.axis(cell.outputs['Color'], "X"))
    colour = g.ramp(g.noise(p, 4.0, 3.0), [(0.30, dark), (0.62, mid)])
    colour = g.mix(g.math('MULTIPLY', tip, 0.85), colour, tips)
    hair = g.noise(g.vmath('MULTIPLY', p, (stretch[0] * 3.5, stretch[1] * 3.5, stretch[2] * 0.8)), 1.0, 2.0)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.55, 0.75), 0.35), colour, tips)
    colour = g.mix(g.math('MULTIPLY', gap, 0.7), colour, dark)
    height = g.math('ADD', g.math('SUBTRACT', 1.0, gap), g.math('MULTIPLY', hair, 0.35))
    return g.finish(colour, height, 0.8, 0.010)


def _colour_noise(g, p, scale):
    """A noise vector (colour output centred on 0), to waver a pattern."""
    node = g.node('ShaderNodeTexNoise')
    g.feed(node.inputs['Vector'], p)
    node.inputs['Scale'].default_value = scale
    node.inputs['Detail'].default_value = 2.0
    return g.vmath('SUBTRACT', node.outputs['Color'], (0.5, 0.5, 0.5))


def roll_end(pivot, centre_y, centre_z, pitch=0.019):
    """The bedroll's ends: a spiral of pale hide and brown fur with dark gaps between the layers, around the roll's
    axis (along X through centre_y, centre_z)."""
    g = Graph("hauler_roll_end", pivot, 0.9)
    p = g.position()
    dy = g.math('SUBTRACT', g.axis(p, "Y"), centre_y)
    dz = g.math('SUBTRACT', g.axis(p, "Z"), centre_z)
    radius = g.math('SQRT', g.math('ADD', g.math('MULTIPLY', dy, dy), g.math('MULTIPLY', dz, dz)))
    radius = g.math('ADD', radius, g.math('MULTIPLY', g.math('SUBTRACT', g.noise(p, 22.0, 2.0), 0.5), 0.006))
    turn = g.math('DIVIDE', g.math('ARCTAN2', dz, dy), 2 * math.pi)
    layer = g.math('FRACT', g.math('SUBTRACT', g.math('DIVIDE', radius, pitch), turn))
    colour = g.ramp(layer, [(0.0, GAP), (0.07, HIDE_PALE), (0.20, HIDE_MID), (0.28, FUR[2]), (0.62, FUR[1]),
                            (0.94, GAP)])
    height = g.math('ADD', g.smooth(layer, 0.0, 0.12), g.math('MULTIPLY', g.noise(p, 60.0, 2.0), 0.3))
    return g.finish(colour, height, 0.5)


def pale_hide(pivot):
    """The bedroll's outside, lox hide flesh side out: pale tan, mottled, creased round the roll, a few stains."""
    g = Graph("hauler_roll_hide", pivot, 0.8)
    p = g.position()
    colour = g.ramp(g.noise(p, 7.0, 4.0), [(0.32, HIDE_MID), (0.68, HIDE_PALE)])
    wrinkles = g.noise(g.vmath('MULTIPLY', p, (26.0, 3.0, 3.0)), 1.0, 2.0)
    crease = g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', wrinkles, 0.5)), 0.0, 0.05, True)
    colour = g.mix(g.math('MULTIPLY', crease, 0.55), colour, HIDE_CREASE)
    stain = g.smooth(g.noise(p, 5.0, 3.0), 0.62, 0.78)
    colour = g.mix(g.math('MULTIPLY', stain, 0.4), colour, HIDE_CREASE)
    height = g.math('ADD', g.math('MULTIPLY', crease, -0.6), g.math('MULTIPLY', g.noise(p, 40.0, 3.0), 0.4))
    return g.finish(colour, height, 0.35)


def linen(pivot):
    """Off-white linen webbing: soft tonal patches, grime where the noise gathers, a faint lengthwise weave."""
    g = Graph("hauler_linen", pivot, 0.85)
    p = g.position()
    colour = g.ramp(g.noise(p, 16.0, 3.0), [(0.30, LINEN_SHADE), (0.62, LINEN)])
    grime = g.smooth(g.noise(p, 6.0, 3.0), 0.58, 0.78)
    colour = g.mix(g.math('MULTIPLY', grime, 0.55), colour, GRIME)
    height = g.math('ADD', g.noise(p, 70.0, 2.0), g.math('MULTIPLY', g.noise(p, 16.0, 2.0), 0.5))
    return g.finish(colour, height, 0.25)


def black_metal(pivot):
    """Forged black metal: near-black with a cold green-grey cast, hammer facets in the bump, rubbed lighter in
    patches."""
    g = Graph("hauler_black_metal", pivot, 0.45)
    p = g.position()
    colour = g.ramp(g.noise(p, 14.0, 4.0), [(0.30, METAL_DARK), (0.70, METAL_MID)])
    worn = g.smooth(g.noise(p, 26.0, 4.0), 0.60, 0.74)
    colour = g.mix(g.math('MULTIPLY', worn, 0.7), colour, METAL_WORN)
    hammer = g.voronoi(p, 60.0)
    height = g.math('ADD', g.math('MULTIPLY', hammer, 0.8), g.math('MULTIPLY', g.noise(p, 35.0, 3.0), 0.4))
    return g.finish(colour, height, 0.35)


def strap_leather(pivot):
    g = Graph("hauler_strap", pivot, 0.65)
    p = g.position()
    colour = g.ramp(g.noise(p, 30.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    height = g.noise(p, 120.0, 2.0)
    return g.finish(colour, height, 0.2)

"""The backpack's own procedural materials, baked by the pipeline into one atlas: dark teal troll leather with seams,
stitches and scuffed edges for the bag, mottled blue-grey troll hide for the flap and the bedroll (whose ends show
the rolled layers), near-black strap leather and bronze. Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot), so seams sit where model.py
puts them. Colours are linear RGB.
"""
import math

import bpy

LEATHER_DARK, LEATHER_MID = (0.019, 0.048, 0.068), (0.052, 0.112, 0.145)
LEATHER_WORN, SEAM, THREAD = (0.140, 0.240, 0.265), (0.006, 0.013, 0.019), (0.20, 0.22, 0.20)
HIDE = ((0.026, 0.056, 0.108), (0.078, 0.148, 0.250), (0.150, 0.230, 0.335), (0.225, 0.285, 0.345))
ROLL = ((0.042, 0.060, 0.085), (0.118, 0.152, 0.198), (0.185, 0.220, 0.258), (0.250, 0.262, 0.275))
FLESH, GAP = (0.205, 0.192, 0.170), (0.016, 0.020, 0.026)
STRAP_DARK, STRAP_MID = (0.013, 0.009, 0.006), (0.052, 0.030, 0.016)
BRONZE_DARK, BRONZE, PATINA = (0.150, 0.070, 0.018), (0.540, 0.285, 0.075), (0.060, 0.090, 0.060)


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


def leather(pivot, seam_x=None, seam_z=None):
    """Troll leather: dark teal-blue, mottled, lighter where the wear attribute and noise agree; with seam_x/seam_z,
    stitched seams at |x| = seam_x (above seam_z) and at z = seam_z."""
    g = Graph("pack_leather" if seam_x else "pack_leather_plain", pivot, 0.7)
    p = g.position()
    colour = g.ramp(g.noise(p, 5.0, 3.0), [(0.3, LEATHER_DARK), (0.72, LEATHER_MID)])
    grain = g.noise(p, 140.0, 2.0)
    scuff = g.smooth(g.math('MULTIPLY', g.attribute("wear"), g.math('ADD', g.noise(p, 22.0, 4.0), 0.3)), 0.5, 0.8)
    colour = g.mix(g.math('MULTIPLY', scuff, 0.8), colour, LEATHER_WORN)
    height = g.math('ADD', g.math('MULTIPLY', grain, 0.35), g.math('MULTIPLY', g.noise(p, 9.0, 2.0), 0.6))
    if seam_x is not None:
        line, stitch = _seams(g, p, seam_x, seam_z)
        colour = g.mix(stitch, g.mix(line, colour, SEAM), THREAD)
        height = g.math('ADD', g.math('SUBTRACT', height, line), g.math('MULTIPLY', stitch, 0.5))
    return g.finish(colour, height, 0.3)


def _seams(g, p, seam_x, seam_z):
    """(line, stitch) masks: dark seam lines and a row of pale stitches beside each."""
    x, y, z = (g.axis(p, a) for a in "XYZ")
    side = g.math('ABSOLUTE', g.math('SUBTRACT', g.math('ABSOLUTE', x), seam_x))
    side = g.math('ADD', side, g.math('MULTIPLY', g.math('LESS_THAN', z, seam_z), 1.0))   # only above seam_z
    low = g.math('ABSOLUTE', g.math('SUBTRACT', z, seam_z))
    line = g.math('MAXIMUM', g.smooth(side, 0.0012, 0.0035, True), g.smooth(low, 0.0012, 0.0035, True))
    along_low = g.math('ADD', x, y)
    stitch = g.math('MAXIMUM', _stitches(g, side, z), _stitches(g, low, along_low))
    return line, stitch


def _stitches(g, distance, along):
    band = g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', distance, 0.0068)), 0.0012, 0.0024, True)
    dash = g.math('LESS_THAN', g.math('FRACT', g.math('DIVIDE', along, 0.011)), 0.55)
    return g.math('MULTIPLY', band, dash)


def hide(pivot, name, tones, stretch):
    """Troll hide: mottled blue-grey, hair streaks along the stretched axis, darker spots, pale worn edges.
    tones: (dark, mid, streak, edge); stretch: noise scale per axis (small = long streaks along that axis)."""
    dark, mid, streak, edge = tones
    g = Graph(name, pivot, 0.9)
    p = g.position()
    colour = g.ramp(g.noise(p, 3.5, 4.0, 0.6), [(0.28, dark), (0.72, mid)])
    hair = g.noise(g.vmath('MULTIPLY', p, stretch), 1.0, 3.0)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.48, 0.74), 0.42), colour, streak)
    spots = g.smooth(g.voronoi(p, 11.0), 0.06, 0.22, True)
    colour = g.mix(g.math('MULTIPLY', spots, 0.4), colour, dark)
    worn = g.smooth(g.math('MULTIPLY', g.attribute("wear"), g.math('ADD', g.noise(p, 30.0, 3.0), 0.4)), 0.45, 0.8)
    colour = g.mix(g.math('MULTIPLY', worn, 0.85), colour, edge)
    height = g.math('ADD', g.math('MULTIPLY', hair, 0.7), g.math('MULTIPLY', g.noise(p, 13.0, 3.0), 0.8))
    return g.finish(colour, height, 0.4)


def roll_end(pivot, centre_y, centre_z, pitch=0.016):
    """The ends of the rolled bedroll: a spiral of pale flesh side and blue-grey fur side with dark gaps between the
    layers, around the roll's axis (along X through centre_y, centre_z)."""
    g = Graph("pack_roll_end", pivot, 0.9)
    p = g.position()
    dy = g.math('SUBTRACT', g.axis(p, "Y"), centre_y)
    dz = g.math('SUBTRACT', g.axis(p, "Z"), centre_z)
    radius = g.math('SQRT', g.math('ADD', g.math('MULTIPLY', dy, dy), g.math('MULTIPLY', dz, dz)))
    radius = g.math('ADD', radius, g.math('MULTIPLY', g.math('SUBTRACT', g.noise(p, 22.0, 2.0), 0.5), 0.008))
    turn = g.math('DIVIDE', g.math('ARCTAN2', dz, dy), 2 * math.pi)
    layer = g.math('FRACT', g.math('SUBTRACT', g.math('DIVIDE', radius, pitch), turn))
    colour = g.ramp(layer, [(0.0, GAP), (0.14, FLESH), (0.5, ROLL[2]), (0.62, ROLL[1]), (0.97, GAP)])
    colour = g.mix(g.math('MULTIPLY', g.noise(p, 40.0, 3.0), 0.35), colour, ROLL[0])
    height = g.math('ADD', g.smooth(layer, 0.0, 0.14), g.math('MULTIPLY', g.noise(p, 60.0, 2.0), 0.3))
    return g.finish(colour, height, 0.5)


def strap_leather(pivot):
    g = Graph("pack_strap", pivot, 0.65)
    p = g.position()
    colour = g.ramp(g.noise(p, 35.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    height = g.noise(p, 160.0, 2.0)
    return g.finish(colour, height, 0.2)


def bronze(pivot):
    g = Graph("pack_bronze", pivot, 0.35)
    p = g.position()
    colour = g.ramp(g.noise(p, 45.0, 4.0), [(0.32, BRONZE_DARK), (0.62, BRONZE)])
    patina = g.smooth(g.noise(p, 18.0, 3.0), 0.6, 0.74)
    colour = g.mix(g.math('MULTIPLY', patina, 0.55), colour, PATINA)
    return g.finish(colour, g.noise(p, 120.0, 2.0), 0.15)

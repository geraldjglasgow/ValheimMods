"""The Rootbound Pack's own procedural materials, baked by the pipeline into one atlas: dark, damp bog leather
(olive-brown, mottled, with wet-looking sheen patches and a green bog tint), pale twisted roots with dark grooves
and moss, bright slimy guck with a pale glossy crest, rusty dark iron and near-black strap leather. Nothing here
reads a game texture.

The Graph writer is copied from the Trollhide Backpack's looks.py. Patterns are laid out on the player-frame
position (object coordinates plus the pivot). Colours are linear RGB.
"""
import bpy

LEATHER = ((0.030, 0.024, 0.009), (0.082, 0.066, 0.022))            # dark, mid
LEATHER_BOG, LEATHER_WET = (0.040, 0.056, 0.014), (0.150, 0.132, 0.052)
LEATHER_WORN, LEATHER_SOAKED = (0.120, 0.094, 0.040), (0.016, 0.024, 0.007)
FLAP = ((0.022, 0.017, 0.008), (0.060, 0.046, 0.019))
ROOT_DARK, ROOT_MID, ROOT_LIGHT = (0.022, 0.019, 0.009), (0.150, 0.132, 0.066), (0.340, 0.300, 0.160)
ROOT_MOSS = (0.090, 0.125, 0.028)
GUCK_EDGE, GUCK, GUCK_CREST = (0.018, 0.065, 0.003), (0.095, 0.25, 0.012), (0.24, 0.43, 0.065)
IRON_DARK, IRON, RUST = (0.020, 0.022, 0.026), (0.105, 0.112, 0.125), (0.120, 0.036, 0.010)
STRAP_DARK, STRAP_MID = (0.012, 0.010, 0.005), (0.050, 0.036, 0.016)


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


def bog_leather(pivot, name="pack_leather", tones=LEATHER):
    """Bog leather: dark olive-brown, mottled, a green bog tint in patches, lighter damp-looking sheen spots, and
    scuffed paler where the wear attribute and noise agree. Slightly glossy."""
    dark, mid = tones
    g = Graph(name, pivot, 0.42)
    p = g.position()
    colour = g.ramp(g.noise(p, 4.5, 4.0), [(0.3, dark), (0.72, mid)])
    colour = g.mix(g.math('MULTIPLY', g.smooth(g.noise(p, 2.2, 2.0), 0.5, 0.68), 0.6), colour, LEATHER_BOG)
    sheen = g.smooth(g.noise(p, 9.0, 3.0), 0.6, 0.74)
    colour = g.mix(g.math('MULTIPLY', sheen, 0.45), colour, LEATHER_WET)
    scuff = g.smooth(g.math('MULTIPLY', g.attribute("wear"), g.math('ADD', g.noise(p, 22.0, 4.0), 0.3)), 0.5, 0.8)
    colour = g.mix(g.math('MULTIPLY', scuff, 0.7), colour, LEATHER_WORN)
    soaked = g.smooth(g.math('ADD', g.axis(p, "Z"), g.math('MULTIPLY', g.noise(p, 12.0, 3.0), 0.06)), 1.24, 1.31,
                      invert=True)
    colour = g.mix(g.math('MULTIPLY', soaked, 0.75), colour, LEATHER_SOAKED)
    wrinkles = g.voronoi(g.vmath('MULTIPLY', p, (1.0, 1.0, 2.2)), 16.0)
    height = g.math('ADD', g.math('MULTIPLY', g.noise(p, 130.0, 2.0), 0.3), g.math('MULTIPLY', wrinkles, 0.8))
    return g.finish(colour, height, 0.35)


def flap_leather(pivot):
    return bog_leather(pivot, "pack_flap", FLAP)


def root(pivot):
    """Roots: pale grey-tan wood, darker in the grooves between the twisted strands (the ridge attribute), fine
    fibres, green moss in patches."""
    g = Graph("pack_root", pivot, 0.85)
    p = g.position()
    colour = g.ramp(g.noise(p, 14.0, 3.0), [(0.3, ROOT_MID), (0.7, ROOT_LIGHT)])
    groove = g.smooth(g.attribute("ridge"), 0.15, 0.7, invert=True)
    colour = g.mix(g.math('MULTIPLY', groove, 0.85), colour, ROOT_DARK)
    moss = g.smooth(g.math('ADD', g.noise(p, 6.0, 3.0), g.math('MULTIPLY', groove, 0.12)), 0.6, 0.7)
    colour = g.mix(g.math('MULTIPLY', moss, 0.7), colour, ROOT_MOSS)
    fibres = g.noise(g.vmath('MULTIPLY', p, (1.0, 1.0, 1.0)), 90.0, 2.0)
    height = g.math('ADD', g.math('MULTIPLY', g.attribute("ridge"), 0.8), g.math('MULTIPLY', fibres, 0.4))
    return g.finish(colour, height, 0.45)


def guck(pivot):
    """Guck: bright slimy green, a pale glossy line along the bead's crest, darker at its edges, small bubbles."""
    g = Graph("pack_guck", pivot, 0.12)
    p = g.position()
    crest = g.attribute("crest")
    colour = g.ramp(crest, [(0.0, GUCK_EDGE), (0.45, GUCK), (0.93, GUCK_CREST)])
    bubbles = g.smooth(g.voronoi(p, 90.0), 0.05, 0.16, invert=True)
    colour = g.mix(g.math('MULTIPLY', bubbles, 0.45), colour, GUCK_CREST)
    colour = g.mix(g.math('MULTIPLY', g.smooth(g.noise(p, 30.0, 2.0), 0.55, 0.75), 0.4), colour, GUCK_EDGE)
    height = g.math('ADD', g.math('MULTIPLY', bubbles, 0.5), g.math('MULTIPLY', g.noise(p, 45.0, 2.0), 0.4))
    return g.finish(colour, height, 0.25)


def iron(pivot):
    """Dark forged iron with rust blooms."""
    g = Graph("pack_iron", pivot, 0.45)
    p = g.position()
    colour = g.ramp(g.noise(p, 55.0, 4.0), [(0.3, IRON_DARK), (0.66, IRON)])
    rust = g.smooth(g.noise(p, 20.0, 3.0), 0.58, 0.72)
    colour = g.mix(g.math('MULTIPLY', rust, 0.7), colour, RUST)
    return g.finish(colour, g.noise(p, 140.0, 2.0), 0.25)


def strap_leather(pivot):
    g = Graph("pack_strap", pivot, 0.6)
    p = g.position()
    colour = g.ramp(g.noise(p, 35.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    return g.finish(colour, g.noise(p, 160.0, 2.0), 0.2)

"""The Carapace Pack's own procedural materials, baked by the pipeline into one atlas: dark grey-blue scale hide for
the bag, near-black purple chitin with a painted beetle-wing sheen for the shell plates, deep-blue woven jute for the
straps, and eitr: bright cyan-blue seam lines and a small glowing stone. Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot), so seams sit where model.py
puts them; the plates also read the per-vertex attributes "along" (0 at a plate's top edge, 1 at its bottom edge) and
"across" (-1 to 1 over its width). Colours are linear RGB. The Graph writer is the Trollhide Backpack's.
"""
import bpy

HIDE_DARK, HIDE_MID, HIDE_LIGHT = (0.013, 0.018, 0.026), (0.048, 0.062, 0.082), (0.100, 0.118, 0.145)
CHITIN_DARK, CHITIN = (0.007, 0.013, 0.011), (0.027, 0.055, 0.041)
SHEEN, IRIS, FRINGE = (0.16, 0.20, 0.11), (0.050, 0.100, 0.068), (0.075, 0.11, 0.051)
JUTE_DARK, JUTE, JUTE_LIGHT = (0.003, 0.016, 0.070), (0.010, 0.058, 0.190), (0.034, 0.120, 0.300)
EITR, EITR_CORE, EITR_DEEP = (0.13, 0.18, 0.075), (0.26, 0.29, 0.16), (0.029, 0.061, 0.034)


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

    def voronoi(self, vector, scale, feature='F1'):
        node = self.node('ShaderNodeTexVoronoi', feature=feature)
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


def scale_hide(pivot, seam_y, name="pack_scale_hide"):
    """Scale hide: dark grey-blue leather covered in small domed scales with dark borders, mottled; faint eitr seams
    down the back's centre line (seen between the shell halves) and down each side at y = seam_y."""
    g = Graph(name, pivot, 0.6)
    p = g.position()
    cells = g.vmath('MULTIPLY', p, (1.0, 1.0, 1.35))
    dome = g.smooth(g.voronoi(cells, 36.0), 0.05, 0.62, True)
    border = g.smooth(g.voronoi(cells, 36.0, 'DISTANCE_TO_EDGE'), 0.02, 0.11)
    colour = g.ramp(g.noise(p, 4.0, 3.0), [(0.3, HIDE_DARK), (0.75, HIDE_MID)])
    colour = g.mix(g.math('MULTIPLY', dome, 0.55), colour, HIDE_LIGHT)
    colour = g.mix(g.math('SUBTRACT', 1.0, border), colour, HIDE_DARK)
    height = g.math('ADD', g.math('MULTIPLY', dome, 0.6), g.math('MULTIPLY', border, 0.4))
    glow = g.math('MULTIPLY', _hide_seams(g, p, seam_y), 0.85)
    colour = g.mix(glow, colour, EITR)
    return g.finish(colour, g.math('SUBTRACT', height, glow), 0.45)


def _hide_seams(g, p, seam_y):
    """1 on the eitr seams: |x| < 5 mm behind the wearer, and |y - seam_y| < 4 mm on the bag's sides."""
    x, y = g.axis(p, "X"), g.axis(p, "Y")
    centre = g.math('MULTIPLY', g.smooth(g.math('ABSOLUTE', x), 0.003, 0.007, True), g.smooth(y, 0.28, 0.30))
    side = g.math('MULTIPLY', g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', y, seam_y)), 0.0015, 0.004, True),
                  g.smooth(g.math('ABSOLUTE', x), 0.10, 0.12))
    return g.math('MAXIMUM', centre, side)


def chitin(pivot, name, grooves, glow_x=None):
    """Carapace: near-black purple chitin with a painted sheen, a bright band across each plate and a lit lower lip,
    tinted towards teal where it catches most; with `grooves`, fine lengthwise striations like a wing case; with
    `glow_x`, an eitr line along the plate's edge nearest the centre (|x| below glow_x)."""
    g = Graph(name, pivot, 0.25)
    p = g.position()
    along, across = g.attribute("along"), g.attribute("across")
    colour = g.ramp(g.noise(p, 7.0, 3.0), [(0.35, CHITIN_DARK), (0.7, CHITIN)])
    fade = g.smooth(g.math('ABSOLUTE', across), 0.55, 1.0, True)
    offset = g.math('ABSOLUTE', g.math('SUBTRACT', along, 0.30))
    glow = g.math('MULTIPLY', g.smooth(offset, 0.04, 0.24, True), fade)
    colour = g.mix(g.math('MULTIPLY', glow, 0.8), colour, FRINGE)
    band = g.math('MULTIPLY', g.smooth(offset, 0.02, 0.11, True), fade)
    lip = g.math('MULTIPLY', g.smooth(along, 0.9, 0.99), 0.4)
    shine = g.math('MAXIMUM', g.math('MULTIPLY', band, g.math('ADD', g.noise(p, 11.0, 2.0), 0.35)), lip)
    tint = g.mix(g.smooth(g.noise(p, 3.0, 2.0), 0.4, 0.62), SHEEN, IRIS)
    colour = g.mix(g.math('MINIMUM', shine, 0.85), colour, tint)
    height = g.math('MULTIPLY', g.noise(p, 20.0, 2.0), 0.15)
    if grooves:
        line = g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', g.math('FRACT', g.math('MULTIPLY', across, 3.5)), 0.5)),
                        0.40, 0.48)
        colour = g.mix(g.math('MULTIPLY', line, 0.7), colour, CHITIN_DARK)
        height = g.math('SUBTRACT', height, g.math('MULTIPLY', line, 0.5))
    if glow_x is not None:
        edge = g.smooth(g.math('ABSOLUTE', g.axis(p, "X")), glow_x - 0.004, glow_x, True)
        colour = g.mix(edge, colour, EITR)
    return g.finish(colour, height, 0.25)


def jute(pivot):
    """Blue jute: deep-blue woven fabric, a fine crossed weave and lighter slubs in the yarn."""
    g = Graph("pack_jute", pivot, 0.9)
    p = g.position()
    colour = g.ramp(g.noise(p, 30.0, 3.0), [(0.3, JUTE_DARK), (0.72, JUTE)])
    weave = g.math('MULTIPLY', g.math('SINE', g.math('MULTIPLY', g.axis(p, "X"), 520.0)),
                   g.math('SINE', g.math('MULTIPLY', g.math('ADD', g.axis(p, "Y"), g.axis(p, "Z")), 520.0)))
    slub = g.smooth(g.noise(g.vmath('MULTIPLY', p, (40.0, 40.0, 160.0)), 1.0, 2.0), 0.55, 0.72)
    colour = g.mix(g.math('MULTIPLY', slub, 0.6), colour, JUTE_LIGHT)
    return g.finish(colour, g.math('ADD', weave, g.math('MULTIPLY', slub, 0.5)), 0.35, 0.002)


def eitr_stone(pivot, centre, radius):
    """Refined eitr set in the shell: white-cyan at its heart, deep blue at its rim."""
    g = Graph("pack_eitr", pivot, 0.1)
    p = g.position()
    distance = g.math('DIVIDE', g.vmath('LENGTH', g.vmath('SUBTRACT', p, tuple(centre)), (0.0, 0.0, 0.0)), radius)
    colour = g.ramp(distance, [(0.0, EITR_CORE), (0.45, EITR), (1.0, EITR_DEEP)])
    return g.finish(colour, g.math('MULTIPLY', g.noise(p, 90.0, 2.0), 0.2), 0.1)

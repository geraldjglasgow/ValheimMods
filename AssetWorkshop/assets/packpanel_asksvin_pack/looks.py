"""The Asksvin Pack's own procedural materials, baked by the pipeline into one atlas: dark red-brown asksvin hide in
domed reptilian scales (smaller on the body, larger on the lid), charred near-black strap leather, flametal glowing
orange-gold (the glow is simply a bright albedo), charred bone and horn spines that fade from the hide's colour to a
pale ash tip. Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot). Colours are linear RGB.
"""
import bpy

BODY = ((0.018, 0.025, 0.009), (0.075, 0.093, 0.026), (0.17, 0.19, 0.067))     # vanilla asksvin olive hide
LID = ((0.012, 0.021, 0.008), (0.06, 0.085, 0.022), (0.20, 0.22, 0.085))
CHAR, CREVICE, ASH = (0.012, 0.004, 0.003), (0.004, 0.0018, 0.0015), (0.190, 0.120, 0.090)
STRAP_DARK, STRAP_MID = (0.010, 0.006, 0.005), (0.050, 0.022, 0.014)
EMBER, FLAME, HOT = (0.380, 0.032, 0.003), (1.000, 0.200, 0.012), (1.000, 0.560, 0.090)
SCALE_DARK = (0.019, 0.032, 0.009)
BONE_BLACK, BONE_GREY, BONE_CRACK = (0.030, 0.027, 0.025), (0.170, 0.155, 0.140), (0.004, 0.003, 0.003)
HORN_DARK, HORN_TIP = (0.014, 0.007, 0.006), (0.420, 0.340, 0.260)


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

    def noise(self, vector, scale, detail=3.0, roughness=0.5, output='Fac'):
        node = self.node('ShaderNodeTexNoise')
        self.feed(node.inputs['Vector'], vector)
        node.inputs['Scale'].default_value = scale
        node.inputs['Detail'].default_value = detail
        node.inputs['Roughness'].default_value = roughness
        return node.outputs[output]

    def cells(self, vector, scale, feature='F1', randomness=0.85):
        """Voronoi cells: (distance, per-cell random colour, the cell's seed point in the input's space);
        DISTANCE_TO_EDGE gives (edge distance, None, None)."""
        node = self.node('ShaderNodeTexVoronoi', feature=feature)
        self.feed(node.inputs['Vector'], vector)
        node.inputs['Scale'].default_value = scale
        node.inputs['Randomness'].default_value = randomness
        if feature == 'DISTANCE_TO_EDGE':
            return node.outputs['Distance'], None, None
        return node.outputs['Distance'], node.outputs['Color'], node.outputs['Position']

    def axis(self, vector, name):
        node = self.node('ShaderNodeSeparateXYZ')
        self.feed(node.inputs[0], vector)
        return node.outputs[name]

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

    def first(self, colour):
        """The red channel of a colour, as a float (a cell's random value)."""
        node = self.node('ShaderNodeSeparateColor')
        self.feed(node.inputs['Color'], colour)
        return node.outputs[0]

    def finish(self, colour, height, strength, distance=0.004):
        self.feed(self.bsdf.inputs['Base Color'], colour)
        bump = self.node('ShaderNodeBump')
        bump.inputs['Strength'].default_value = strength
        bump.inputs['Distance'].default_value = distance
        self.feed(bump.inputs['Height'], height)
        self.feed(self.bsdf.inputs['Normal'], bump.outputs['Normal'])
        return self.mat


def scales(pivot, name, tones, size, contrast=0.75):
    """Asksvin hide: domed scales about `size` metres across (wider than tall), shingled like a lizard's back: each
    scale lighter and raised towards its lower edge, tucked under the one above at its top. Each has its own shade
    between charred and mid, near-black crevices a couple of texels wide between them, charred patches, and ash-pale
    scuffs where the wear attribute and noise agree. tones: (dark, mid, crown); contrast: how much the crowns show."""
    dark, mid, crown = tones
    g = Graph(name, pivot, 0.75)
    p = g.position()
    warp = g.vmath('MULTIPLY', g.vmath('SUBTRACT', g.noise(p, 7.0, 2.0, output='Color'), (0.5,) * 3), (0.03,) * 3)
    q = g.vmath('ADD', g.vmath('MULTIPLY', p, (1.0, 1.0, 1.3)), warp)
    _, shade, seed = g.cells(q, 1.0 / size)
    edge, _, _ = g.cells(q, 1.0 / size, 'DISTANCE_TO_EDGE')
    edge = g.math('DIVIDE', edge, size)                   # 0 on the crevice, about 0.5 in the scale's middle
    rise = g.math('DIVIDE', g.math('SUBTRACT', g.axis(q, "Z"), g.axis(seed, "Z")), size)   # up from the scale's seed
    lower = g.smooth(rise, -0.35, 0.25, True)             # 1 on the scale's lower, exposed part
    colour = g.ramp(g.first(shade), [(0.0, CHAR), (0.3, dark), (0.95, mid)])
    colour = g.mix(g.math('MULTIPLY', g.math('MULTIPLY', lower, g.smooth(edge, 0.08, 0.3)), contrast), colour, crown)
    colour = g.mix(g.math('MULTIPLY', g.smooth(g.noise(p, 4.0, 3.0), 0.52, 0.7), 0.55), colour, CHAR)   # charring
    colour = g.mix(g.smooth(edge, 0.05, 0.13, True), colour, CREVICE)
    worn = g.smooth(g.math('MULTIPLY', g.attribute("wear"), g.math('ADD', g.noise(p, 26.0, 3.0), 0.35)), 0.5, 0.8)
    colour = g.mix(g.math('MULTIPLY', worn, 0.65), colour, ASH)
    height = g.math('ADD', g.math('MULTIPLY', g.smooth(edge, 0.0, 0.3), 0.7), g.math('MULTIPLY', lower, 0.45))
    height = g.math('ADD', height, g.math('MULTIPLY', g.noise(p, 90.0, 2.0), 0.12))
    return g.finish(colour, height, 0.8, distance=0.006)


def strap_leather(pivot):
    """Charred strap leather: near black with a faint red-brown, fine grain."""
    g = Graph("pack_strap", pivot, 0.65)
    p = g.position()
    colour = g.ramp(g.noise(p, 35.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    return g.finish(colour, g.noise(p, 160.0, 2.0), 0.2)


def flametal(pivot):
    """Flametal, hot: a molten orange-gold, yellow-white where it glows most, darker ember-red in the hammer marks;
    where the wear attribute is set (the corner caps' rims) a cooled, dark red-black edge."""
    g = Graph("pack_flametal", pivot, 0.35)
    p = g.position()
    heat = g.math('ADD', g.math('MULTIPLY', g.noise(p, 30.0, 3.0), 0.7), g.math('MULTIPLY', g.noise(p, 90.0, 2.0), 0.3))
    colour = g.ramp(heat, [(0.30, EMBER), (0.46, FLAME), (0.66, HOT)])
    colour = g.mix(g.math('MULTIPLY', g.attribute("wear"), 0.85), colour, SCALE_DARK)    # a cooled rim on the caps
    return g.finish(colour, g.noise(p, 70.0, 3.0), 0.25)


def charred_bone(pivot):
    """Charred bone: grey-black with greyer ridges and a few black cracks along the bone."""
    g = Graph("pack_bone", pivot, 0.8)
    p = g.position()
    colour = g.ramp(g.noise(p, 60.0, 4.0), [(0.35, BONE_BLACK), (0.70, BONE_GREY)])
    stretched = g.vmath('MULTIPLY', p, (8.0, 60.0, 60.0))                 # cracks run along the bone (X)
    crack, _, _ = g.cells(stretched, 1.0, 'DISTANCE_TO_EDGE')
    colour = g.mix(g.smooth(crack, 0.0, 0.05, True), colour, BONE_CRACK)
    height = g.math('ADD', g.noise(p, 80.0, 3.0), g.smooth(crack, 0.0, 0.05))
    return g.finish(colour, height, 0.35)


def horn(pivot, base):
    """The spines: the hide's own colour at the base (the "tip" attribute 0), near-black horn, pale ash at the tip
    (1), with streaks along their length."""
    g = Graph("pack_horn", pivot, 0.55)
    p = g.position()
    tip = g.math('ADD', g.attribute("tip"), g.math('MULTIPLY', g.math('SUBTRACT', g.noise(p, 50.0, 2.0), 0.5), 0.2))
    colour = g.ramp(tip, [(0.05, base), (0.35, HORN_DARK), (0.95, HORN_TIP)])
    return g.finish(colour, g.noise(g.vmath('MULTIPLY', p, (140.0, 140.0, 25.0)), 1.0, 2.0), 0.2)

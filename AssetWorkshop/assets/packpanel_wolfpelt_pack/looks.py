"""The Wolfpelt Pack's own procedural materials, baked by the pipeline into one atlas: dark grey leather with seams,
stitches and scuffed edges for the bag, warm grey-white wolf fur for the mantle (darker roots, pale tips, a darker
saddle down the middle like a wolf's back), near-black brown strap leather, silver, and ivory for the fang.
Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot), so seams sit where model.py
puts them. The fur reads the per-vertex vector "fur" that fur.py writes: x runs across the mantle and y differs per
layer (so noise stretched along z makes strands down the locks), z runs from the root (0) to the tip (1). The fang reads
the float "tooth", root 0 to tip 1. Colours are linear RGB.
"""
import bpy

LEATHER_DARK, LEATHER_MID = (0.017, 0.018, 0.021), (0.046, 0.047, 0.053)
LEATHER_WORN, SEAM, THREAD = (0.118, 0.116, 0.114), (0.005, 0.005, 0.006), (0.135, 0.130, 0.122)
FUR_ROOT, FUR_DARK = (0.060, 0.052, 0.043), (0.150, 0.135, 0.115)
FUR_MID, FUR_TIP = (0.255, 0.267, 0.275), (0.52, 0.54, 0.55)
STRAND_DARK, STRAND_LIGHT, SADDLE = (0.095, 0.10, 0.11), (0.59, 0.61, 0.62), (0.065, 0.07, 0.08)
STRAP_DARK, STRAP_MID = (0.014, 0.009, 0.006), (0.058, 0.034, 0.019)
SILVER_DARK, SILVER, SILVER_BRIGHT = (0.160, 0.168, 0.185), (0.520, 0.540, 0.575), (0.820, 0.835, 0.860)
IVORY_ROOT, IVORY, IVORY_TIP = (0.210, 0.150, 0.080), (0.700, 0.620, 0.410), (0.860, 0.820, 0.660)


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

    def attribute(self, name, output='Fac'):
        node = self.node('ShaderNodeAttribute', attribute_name=name, attribute_type='GEOMETRY')
        return node.outputs[output]

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


def leather(pivot, seam_x=None, seam_z=None):
    """Dark grey leather, mottled, paler where the wear attribute and noise agree; with seam_x/seam_z, stitched
    seams at |x| = seam_x (above seam_z) and at z = seam_z."""
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
    line = g.math('MAXIMUM', g.smooth(side, 0.002, 0.005, True), g.smooth(low, 0.002, 0.005, True))
    stitch = g.math('MAXIMUM', _stitches(g, side, z), _stitches(g, low, g.math('ADD', x, y)))
    return line, stitch


def _stitches(g, distance, along):
    band = g.smooth(g.math('ABSOLUTE', g.math('SUBTRACT', distance, 0.010)), 0.0015, 0.0035, True)
    dash = g.math('LESS_THAN', g.math('FRACT', g.math('DIVIDE', along, 0.021)), 0.5)
    return g.math('MULTIPLY', band, dash)


def fur(pivot):
    """Wolf fur: warm grey-white with brownish-grey undertones, each lock darker at the root and paler at the tip,
    mottled lock to lock, faint strands along the locks and a darker saddle down the middle of the mantle. Low
    contrast and a weak bump, so it reads soft and matte."""
    g = Graph("pack_fur", pivot, 1.0)
    p = g.position()
    tuft = g.attribute("fur", 'Vector')
    along = g.axis(tuft, "Z")
    hair = g.noise(g.vmath('MULTIPLY', tuft, (4.5, 4.5, 0.5)), 1.0, 3.0, 0.55)
    tone = g.math('ADD', along, g.math('MULTIPLY', g.math('SUBTRACT', hair, 0.5), 0.45))
    colour = g.ramp(tone, [(0.0, FUR_ROOT), (0.35, FUR_DARK), (0.65, FUR_MID), (1.0, FUR_TIP)])
    middle = g.math('ADD', g.math('ABSOLUTE', g.axis(p, "X")), g.math('MULTIPLY', g.noise(p, 8.0, 3.0), 0.07))
    saddle = g.math('MULTIPLY', g.smooth(middle, 0.09, 0.16, True), g.smooth(along, 0.3, 0.6))
    colour = g.mix(g.math('MULTIPLY', saddle, 0.72), colour, SADDLE)
    mottle = g.noise(p, 11.0, 2.0)                  # clump to clump: some greyer, some whiter
    colour = g.mix(g.math('MULTIPLY', g.smooth(mottle, 0.52, 0.70), 0.30), colour, FUR_DARK)
    colour = g.mix(g.math('MULTIPLY', g.smooth(mottle, 0.30, 0.44, True), 0.25), colour, STRAND_LIGHT)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.30, 0.42, True), 0.3), colour, STRAND_DARK)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.60, 0.74), 0.2), colour, STRAND_LIGHT)
    height = g.math('ADD', hair, g.math('MULTIPLY', along, 0.4))
    return g.finish(colour, height, 0.1)


def strap_leather(pivot):
    g = Graph("pack_strap", pivot, 0.65)
    p = g.position()
    colour = g.ramp(g.noise(p, 35.0, 3.0), [(0.3, STRAP_DARK), (0.75, STRAP_MID)])
    return g.finish(colour, g.noise(p, 160.0, 2.0), 0.2)


def silver(pivot):
    """Pale silver with dark tarnish in patches: the albedo alone has to read as silver, as the game draws it."""
    g = Graph("pack_silver", pivot, 0.3)
    p = g.position()
    colour = g.ramp(g.noise(p, 55.0, 4.0), [(0.28, SILVER_DARK), (0.50, SILVER), (0.70, SILVER_BRIGHT)])
    return g.finish(colour, g.noise(p, 150.0, 2.0), 0.1)


def ivory(pivot):
    """The fang: brownish at the root, ivory along its length, paler at the tip, with faint growth lines."""
    g = Graph("pack_ivory", pivot, 0.45)
    p = g.position()
    along = g.attribute("tooth")
    colour = g.ramp(along, [(0.0, IVORY_ROOT), (0.22, IVORY), (1.0, IVORY_TIP)])
    lines = g.noise(g.vmath('MULTIPLY', p, (40.0, 40.0, 400.0)), 1.0, 2.0)
    colour = g.mix(g.math('MULTIPLY', g.smooth(lines, 0.55, 0.72), 0.3), colour, IVORY_ROOT)
    return g.finish(colour, lines, 0.1)

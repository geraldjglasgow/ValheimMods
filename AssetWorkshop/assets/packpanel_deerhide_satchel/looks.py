"""The satchel's own procedural materials, baked by the pipeline into one atlas: tan-brown deer hide with short hair
streaks and a paler belly-hide underside for the bag, a warmer brown flap with pale worn edges, darker leather-scrap
patches, pale sinew thread, knotted scrap straps in two browns, a pale wooden toggle and barky sticks with light cut
ends. Nothing here reads a game texture.

Patterns are laid out on the player-frame position (object coordinates plus the pivot). Colours are linear RGB.
"""
import bpy

HIDE = ((0.12, 0.061, 0.027), (0.27, 0.14, 0.065), (0.40, 0.235, 0.12))        # dark, mid, hair streak
BELLY = (0.500, 0.330, 0.160)
HIDE_WORN = (0.520, 0.360, 0.200)
FLAP = ((0.105, 0.040, 0.015), (0.235, 0.098, 0.036), (0.400, 0.200, 0.085))
FLAP_EDGE = (0.560, 0.390, 0.215)
PATCHES = {"dark": ((0.045, 0.022, 0.010), (0.105, 0.052, 0.024)),
           "red": ((0.070, 0.022, 0.010), (0.160, 0.056, 0.024)),
           "grey": ((0.050, 0.036, 0.024), (0.115, 0.085, 0.055))}
THREAD = ((0.250, 0.190, 0.100), (0.420, 0.330, 0.190))
SCRAPS = {"dark": ((0.040, 0.018, 0.008), (0.098, 0.048, 0.022)),
          "tan": ((0.170, 0.090, 0.040), (0.300, 0.175, 0.085))}
WOOD = ((0.260, 0.140, 0.055), (0.520, 0.330, 0.150))
BARK = ((0.075, 0.058, 0.040), (0.190, 0.150, 0.100), (0.280, 0.250, 0.200))
HEARTWOOD = ((0.330, 0.200, 0.085), (0.600, 0.450, 0.240))


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


def _hair(g, p, tones):
    """Mottled hide in (dark, mid) with short hair streaks running down (noise stretched along Z)."""
    dark, mid, streak = tones
    colour = g.ramp(g.noise(p, 4.0, 4.0, 0.6), [(0.30, dark), (0.70, mid)])
    hair = g.noise(g.vmath('MULTIPLY', p, (70.0, 70.0, 7.0)), 1.0, 3.0)
    colour = g.mix(g.math('MULTIPLY', g.smooth(hair, 0.50, 0.72), 0.45), colour, streak)
    return colour, hair


def _worn(g, p, colour, tone, amount=0.8):
    """Lighter where the wear attribute and a noise agree: scuffed edges and corners."""
    wear = g.math('MULTIPLY', g.attribute("wear"), g.math('ADD', g.noise(p, 26.0, 3.0), 0.35))
    return g.mix(g.math('MULTIPLY', g.smooth(wear, 0.42, 0.78), amount), colour, tone)


def deer_hide(pivot, belly_z):
    """The bag: tan-brown deer hide fading, over a ragged line near belly_z, into pale belly hide underneath."""
    g = Graph("satchel_hide", pivot, 0.8)
    p = g.position()
    colour, hair = _hair(g, p, HIDE)
    edge = g.math('ADD', g.axis(p, "Z"), g.math('MULTIPLY', g.math('SUBTRACT', g.noise(p, 9.0, 3.0), 0.5), 0.05))
    colour = g.mix(g.smooth(edge, belly_z + 0.018, belly_z - 0.018), colour, BELLY)
    colour = _worn(g, p, colour, HIDE_WORN, 0.6)
    height = g.math('ADD', g.math('MULTIPLY', hair, 0.6), g.math('MULTIPLY', g.noise(p, 11.0, 3.0), 0.7))
    return g.finish(colour, height, 0.35)


def flap_hide(pivot):
    """The flap: a warmer, darker piece of the same hide, paler along its cut edges."""
    g = Graph("satchel_flap", pivot, 0.85)
    p = g.position()
    colour, hair = _hair(g, p, FLAP)
    colour = _worn(g, p, colour, FLAP_EDGE, 0.9)
    height = g.math('ADD', g.math('MULTIPLY', hair, 0.6), g.math('MULTIPLY', g.noise(p, 13.0, 3.0), 0.7))
    return g.finish(colour, height, 0.35)


def patch_leather(pivot, kind):
    """A scrap of darker leather sewn on: mottled, creased, lighter where worn at its edge."""
    dark, mid = PATCHES[kind]
    g = Graph(f"satchel_patch_{kind}", pivot, 0.7)
    p = g.position()
    colour = g.ramp(g.noise(p, 16.0, 4.0), [(0.30, dark), (0.72, mid)])
    colour = _worn(g, p, colour, tuple(c * 2.2 for c in mid), 0.7)
    crease = g.smooth(g.voronoi(p, 30.0), 0.0, 0.18)
    height = g.math('ADD', crease, g.math('MULTIPLY', g.noise(p, 50.0, 2.0), 0.5))
    return g.finish(colour, height, 0.4)


def thread(pivot):
    g = Graph("satchel_thread", pivot, 0.9)
    p = g.position()
    colour = g.ramp(g.noise(p, 150.0, 2.0), [(0.35, THREAD[0]), (0.65, THREAD[1])])
    return g.finish(colour, g.noise(p, 300.0, 1.0), 0.2)


def scrap_strap(pivot, kind):
    dark, mid = SCRAPS[kind]
    g = Graph(f"satchel_strap_{kind}", pivot, 0.7)
    p = g.position()
    colour = g.ramp(g.noise(p, 30.0, 3.0), [(0.30, dark), (0.75, mid)])
    colour = _worn(g, p, colour, tuple(c * 1.8 for c in mid), 0.6)
    return g.finish(colour, g.noise(p, 140.0, 2.0), 0.25)


def wood(pivot):
    """The toggle: pale, smoothed wood with a faint grain along X."""
    g = Graph("satchel_wood", pivot, 0.6)
    p = g.position()
    grain = g.noise(g.vmath('MULTIPLY', p, (8.0, 160.0, 160.0)), 1.0, 3.0)
    colour = g.ramp(grain, [(0.30, WOOD[0]), (0.65, WOOD[1])])
    return g.finish(colour, grain, 0.15)


def bark(pivot):
    """The sticks: grey-brown bark in lengthwise streaks with lighter lichen flecks."""
    g = Graph("satchel_bark", pivot, 0.9)
    p = g.position()
    streak = g.noise(g.vmath('MULTIPLY', p, (90.0, 90.0, 9.0)), 1.0, 4.0)
    colour = g.ramp(streak, [(0.30, BARK[0]), (0.66, BARK[1])])
    flecks = g.smooth(g.noise(p, 60.0, 2.0), 0.64, 0.72)
    colour = g.mix(g.math('MULTIPLY', flecks, 0.6), colour, BARK[2])
    return g.finish(colour, streak, 0.5)


def cut_wood(pivot):
    """The sticks' cut ends: light heartwood."""
    g = Graph("satchel_cut", pivot, 0.8)
    p = g.position()
    colour = g.ramp(g.noise(p, 90.0, 2.0), [(0.3, HEARTWOOD[0]), (0.7, HEARTWOOD[1])])
    return g.finish(colour, g.noise(p, 200.0, 1.0), 0.2)

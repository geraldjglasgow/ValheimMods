"""Original low-resolution material recipes, based on visual study of the vanilla palettes.
No game textures are loaded or copied into the deliverables.
"""
import bpy
from workshop import materials


def mottled(name, colors, stretch=(1, 1, 1), scale=20, roughness=.8, metal=0):
    mat = materials.flat(name, colors[1], roughness, metal)
    tree = mat.node_tree
    coord = tree.nodes.new('ShaderNodeTexCoord')
    mult = tree.nodes.new('ShaderNodeVectorMath'); mult.operation = 'MULTIPLY'
    mult.inputs[1].default_value = stretch
    tree.links.new(coord.outputs['Object'], mult.inputs[0])
    noise = tree.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = 2
    tree.links.new(mult.outputs['Vector'], noise.inputs['Vector'])
    ramp = tree.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'EASE'
    ramp.color_ramp.elements[0].position = .25
    ramp.color_ramp.elements[0].color = (*colors[0], 1)
    ramp.color_ramp.elements[1].position = .75
    ramp.color_ramp.elements[1].color = (*colors[2], 1)
    ramp.color_ramp.elements.new(.5).color = (*colors[1], 1)
    tree.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    tree.links.new(ramp.outputs['Color'], materials.principled(mat).inputs['Base Color'])
    return mat


def make():
    return {
        'drift': mottled('salt_worn_wood',[(.09,.065,.037),(.22,.17,.105),(.34,.28,.18)],(.18,2,5),32),
        'fine': mottled('finewood',[(.10,.051,.016),(.32,.20,.081),(.47,.33,.155)],(.12,3,6),35),
        'ygg': mottled('yggdrasil_wood',[(.055,.04,.02),(.16,.12,.061),(.29,.225,.13)],(.2,2,5),25),
        'fur': mottled('deer_hide',[(.09,.048,.023),(.22,.14,.07),(.36,.27,.15)],(3,2,10),26),
        'troll': mottled('trollhide',[(.022,.044,.059),(.061,.11,.145),(.115,.18,.215)],scale=28),
        'ask': mottled('asksvin_hide',[(.045,.036,.024),(.125,.105,.064),(.235,.20,.125)],(2,2,4),22),
        'bronze': mottled('muted_bronze',[(.075,.054,.025),(.28,.20,.089),(.49,.37,.17)],scale=40,roughness=.55,metal=.6),
        'shell': mottled('carapace',[(.009,.022,.021),(.028,.067,.060),(.083,.135,.112)],scale=26,roughness=.55),
        'shell_edge': mottled('carapace_worn_edges',[(.055,.066,.036),(.12,.145,.078),(.23,.25,.14)],scale=30),
        'flametal': mottled('flametal',[(.008,.010,.013),(.028,.030,.034),(.075,.058,.046)],scale=22,roughness=.55,metal=.7),
        'flame_edge': mottled('flametal_worn_edges',[(.17,.07,.028),(.38,.20,.081),(.55,.35,.14)],scale=18,roughness=.6),
        'rope': materials.flat('flax_cord',(.33,.25,.135)),
        'seam': materials.flat('dark_joints',(.014,.011,.007)),
        'bone': materials.flat('bone_toggle',(.50,.42,.26)),
        'red': materials.flat('ochre_float',(.28,.039,.017)),
    }

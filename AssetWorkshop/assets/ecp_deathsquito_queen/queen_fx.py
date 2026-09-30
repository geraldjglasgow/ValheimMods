"""The preview's small effects: hit flashes and dust puffs. Their fade is the object colour's alpha
(queen_props.fx_material)."""
import math

from mathutils import Vector

from queen_props import blob, fade, fx_material

AIR, BLOOD, DUST = (0.86, 0.9, 0.93), (0.9, 0.12, 0.05), (0.36, 0.32, 0.2)


def _soft(mat):
    """Soft edges: see-through where the ball turns away from the camera, so a puff has no outline."""
    tree = mat.node_tree
    if "soft" in tree.nodes:
        return mat
    bsdf = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
    info = next(n for n in tree.nodes if n.type == 'OBJECT_INFO')
    facing = tree.nodes.new('ShaderNodeLayerWeight')
    facing.name = "soft"
    inside = tree.nodes.new('ShaderNodeMath')
    inside.operation, inside.inputs[0].default_value = 'SUBTRACT', 1.0
    tree.links.new(facing.outputs['Facing'], inside.inputs[1])
    soft = tree.nodes.new('ShaderNodeMath')
    soft.operation, soft.inputs[1].default_value = 'POWER', 2.0
    tree.links.new(inside.outputs['Value'], soft.inputs[0])
    alpha = tree.nodes.new('ShaderNodeMath')
    alpha.operation = 'MULTIPLY'
    tree.links.new(soft.outputs['Value'], alpha.inputs[0])
    tree.links.new(info.outputs['Alpha'], alpha.inputs[1])
    tree.links.new(alpha.outputs['Value'], bsdf.inputs['Alpha'])
    return mat


def flash(keys, at, frame, colour=BLOOD, label="flash"):
    """A burst of light where a hit lands: grows and fades in a sixth of a second."""
    obj = blob(f"{label} {frame}", 0.22, fx_material(f"fx {colour}", colour, emission=6.0))
    fade(keys, obj, range(frame, frame + 6), lambda u: 1 - u, lambda u: 0.4 + u, lambda u: Vector(at))


def dust(keys, at, frame, size=0.5, count=6, seed=0):
    """A puff of heath dust: a few soft balls rolling out and up from `at`, fading over 0.6 s."""
    mat = _soft(fx_material("fx dust", DUST))
    for k in range(count):
        a = 2 * math.pi * (k + 0.37 * seed) / count
        out = Vector((math.cos(a), math.sin(a), 0.0))
        obj = blob(f"dust {frame} {k}", 0.18 * size, mat)
        fade(keys, obj, range(frame, frame + 18), lambda u: 0.55 * (1 - u) ** 1.5, lambda u: 0.6 + 1.6 * u,
             lambda u, out=out: Vector(at) + out * size * 1.4 * (1 - (1 - u) ** 2) + Vector((0, 0, 0.35 * size * u)))

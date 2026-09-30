"""The paint of the Queen's eggs (ecp_queen_egg and ecp_queen_egg_burst): the codex recipes (workshop.paint) in the
Deathsquito Queen's own tones (assets/ecp_deathsquito_queen/queen_paint.py). A prop, so no light from above.

- shell: the Queen's olive chitin with soft darker mottling and dull red-brown bruises (region primary).
- glow: the Queen's blood-red glow showing through the grooves, by the mesh's `glow` attribute: bright at their bottoms,
  near-black blood at their lips (region glow, so a mod can make it emissive: only glow colours are in it, so the
  emission fades out at its edges).
- inside: the wet red-brown slime inside the burst shell (region secondary).
- section: the broken shell's edge, a paler olive (region primary).
"""
from workshop import paint, paint_layers, paint_nodes, paint_specs

DENSITY = 75.0                                        # px per metre the atlas lands at (the style check measures it)
SHELL = ("#1f2518", "#344428", "#56683a")             # the Queen's chitin
MOTTLE = ("#192012", 0.30, 0.12)                      # darker soft mottling: colour, cover, size (m)
BRUISE = ("#3a2416", 0.12, 0.1)                       # dull red-brown where the glow is felt under the shell
GLOW = ("#7a1a0a", "#c8321a", "#e85a30")              # the Queen's belly glow
EMBER = ("#1c120a", "#40120a")                        # where the glow fades out: near-black blood
SLIME = ("#240a05", "#44160b", "#6e2814")             # wet red-brown slime
SECTION = ("#3c3e26", "#5a5c3a", "#7c7c50")           # a broken edge of shell


def _tones(hexes):
    return tuple(paint_specs.hex_to_linear(h) for h in hexes)


def shell(name="egg_shell"):
    """The Queen's chitin, mottled darker, no light from above (a prop)."""
    return paint.make("chitin.chitin", name, tones=_tones(SHELL), patches=[MOTTLE, BRUISE], contrast=1.3, top=0.0,
                      edges=0.3, density=DENSITY)


def glowing():
    """The glow showing through the shell, by the mesh's `glow` attribute: near-black blood at the grooves' lips, the
    Queen's red deeper in, bright where the shell is thinnest, in soft noisy blotches. Only glow colours (no shell)
    are in this region, so a mod's emission from it fades to nothing at its edges. The bump is the shell's, so the
    relief runs on across the faces."""
    mat = shell("egg_glow")
    bsdf = next(n for n in mat.node_tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
    amount = paint_nodes.node(mat, 'ShaderNodeAttribute', attribute_name="glow").outputs['Fac']
    wobble = paint_nodes.noise(mat, paint_layers.scale_for(0.1), 2.0, paint_nodes.coords(mat, offset=(3.3, 1.7, 8.1)))
    swing = paint_nodes.math(mat, 'MULTIPLY_ADD', wobble, 3.0)        # 1 + 3 * (wobble - 0.5): about 0.5 to 1.5
    swing.node.inputs[2].default_value = -0.5
    level = paint_nodes.math(mat, 'MULTIPLY', amount, swing)
    dark, mid, light = _tones(GLOW)
    colour = paint_nodes.ramp(mat, level, [(0.0, _tones(EMBER)[0]), (0.18, _tones(EMBER)[1]), (0.38, dark),
                                           (0.68, mid), (0.95, light)])
    colour = paint_layers.jitter(mat, colour, 0.06, 1.0 / DENSITY)
    paint_nodes.link(mat, colour, bsdf.inputs['Base Color'])
    mat["region"] = "glow"
    mat["paint_family"] = "skin.flesh"
    return mat


def inside():
    """The slime inside the shell: dark wet red-brown with paler wet streaks."""
    return paint.make("skin.flesh", "egg_inside", tones=_tones(SLIME), pattern=("marbling", 0.07), hollows=0.3,
                      region="secondary", density=DENSITY)


def section():
    """The broken shell's edge: paler olive than the outside."""
    return paint.make("chitin.chitin", "egg_section", tones=_tones(SECTION), edges=0.0, hollows=0.0,
                      density=DENSITY)

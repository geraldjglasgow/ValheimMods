"""The Deathsquito Queen's paint: the codex recipes (workshop.paint) in the game Deathsquito's own tones, sampled from
Deathsquito_d.png (BRIEF.md section 3). Each material names its paint region."""
from workshop import paint, paint_specs

DENSITY = 55.0          # px per metre the atlas lands at (the style check measures it)


def _tones(dark, mid, light):
    return tuple(paint_specs.hex_to_linear(h) for h in (dark, mid, light))


def chitin():
    """The shell: the Deathsquito's green a shade lighter, lighter on top as the game paints its creatures."""
    return paint.make("chitin.chitin", "queen_chitin", tones=_tones("#1f2518", "#344428", "#56683a"), top=0.12,
                      contrast=1.3, density=DENSITY)


def joint():
    """Near-black green: the soft joints between the abdomen's plates, the wings' rim, the needle."""
    return paint.make("chitin.chitin", "queen_joint", tones=_tones("#0c0e0a", "#161a11", "#252b1b"), edges=0.2,
                      density=DENSITY)


def glow():
    """The engorged belly, painted as the glow it is in the game (the Deathsquito's stripes #e33e1b)."""
    return paint.make("skin.flesh", "queen_glow", tones=_tones("#7a1a0a", "#c8321a", "#e85a30"), pattern=None,
                      region="glow", density=DENSITY)


def glow_hot():
    """The middle of each belly segment, brightest as the game's stripes are."""
    return paint.make("skin.flesh", "queen_glow_hot", tones=_tones("#b02810", "#e84422", "#ff7a48"), pattern=None,
                      region="glow", hollows=0.2, density=DENSITY)


def wing():
    """The membrane: orange-brown with red speckle and olive washes, as the Deathsquito's wing (no painted veins: a
    line a texel wide steps across the atlas; the veins are modelled)."""
    return paint.make("chitin.chitin", "queen_wing", tones=_tones("#4a3a22", "#7a4c2a", "#9c6a40"),
                      patches=[("#a4442a", 0.3, 0.14), ("#5e5a32", 0.15, 0.4)], region="secondary", edges=0.0,
                      jitter_scale=0.6, contrast=1.6, density=DENSITY)


def horn():
    """The crown, the stinger and the leg bands: amber horn, banded along its length."""
    return paint.make("bone.horn", "queen_horn", tones=_tones("#5a4020", "#9a7a48", "#d4b878"), region="trim",
                      pattern=("bands", 0.07, 0.25), blotch_m=0.1, density=DENSITY)


def eye():
    """Acid green, the Deathsquito's eye colour: painted as the glow it is in the game."""
    return paint.make("crystal.crystal", "queen_eye", tones=_tones("#4a9a2c", "#88d058", "#c8f890"),
                      region="eyes", hollows=0.0, edges=0.0, density=DENSITY)

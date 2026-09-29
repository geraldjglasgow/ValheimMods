"""The Mossback's paint, one recipe per region (blender/workshop/paint.py, numbers from the codex): grey-green skin
painted light (the star levels' hue shift and a mod's _Color tint colour it: codex models/creatures.md), green moss
on the hump and shoulders, dark leather wraps and belt, a hide loincloth, ivory tusks and claws, pale yellow eyes.
Every material names its paint region, so the bake writes the regions mask a mod recolours by.
"""
from workshop import materials, paint, regions
from workshop.paint_specs import hex_to_linear

DENSITY = 70.0            # px per metre the atlas gives the body (a player-sized biped: 55 to 75)
SKIN = tuple(hex_to_linear(c) for c in ("#50564a", "#6b715f", "#838972"))   # grey-green, light: tints take it


def build():
    """{skin, moss, leather, hide, bone, glow}: the recipe materials the bake reads."""
    return {
        "skin": paint.skin("mossback_skin", tones=SKIN, density=DENSITY, region="skin", top=0.12, contrast=1.8,
                           relief=1.0, blotch_m=0.3, jitter=0.03),
        "moss": paint.moss("mossback_moss", density=DENSITY, region="fur"),
        "leather": paint.leather("mossback_leather", density=DENSITY, region="leather"),
        "hide": paint.hide("mossback_hide", density=DENSITY, region="cloth"),
        "bone": paint.teeth("mossback_bone", density=DENSITY, region="bone", tint="#ddd2b0"),
        "glow": regions.mark(materials.flat("mossback_eye", (0.95, 0.78, 0.25), roughness=0.4), "glow", family="glow"),
    }

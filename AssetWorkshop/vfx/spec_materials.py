"""The spec's textures and materials (spec.py): which of the game's particle shaders each material is dressed into, with
the game's settings, blends, keywords and vertex streams for it (codex/vfx/shaders.md), and the placeholder's nearest
mode in Unity's standard particle shader.
"""


# The game's shaders by the codex's short names: the shader a material is dressed into in the game, its settings,
# and the vertex streams the game's own systems feed it (codex/data/vfx.json "shaders").
SHADERS = {
    "gradient_mapped": {"game": "Custom/Gradient Mapped Particle (Unlit)",
                        "floats": {"_GradientChannel": 0, "_GradientAsAlpha": 1, "_Cull": 0, "_SoftParticles": 1,
                                   "_SoftFadeFactor": 12, "_SoftNearFade": 0, "_CameraFadeFactor": 1, "_FejdFog": 0,
                                   "_SkyMask": 0},
                        "keywords": ["_SOFTPARTICLES_ON", "_GRADIENTASALPHA_ON"],
                        "streams": ["Position", "Color", "UV", "Custom1XYZW", "Custom2XYZW"], "blend": "additive_soft"},
    "particle_unlit": {"game": "Custom/Particle (Unlit)",
                       "floats": {"_AlphaChannel": 3, "_Cull": 0, "_SoftParticles": 0, "_SoftFadeFactor": 1,
                                  "_SoftNearFade": 0.5, "_CameraFadeFactor": 0.2, "_FejdFog": 0, "_SkyMask": 0},
                       "keywords": [], "streams": ["Position", "Color", "UV"], "blend": "additive_soft"},
    "lit": {"game": "Lux Lit Particles/ Bumped",
            "floats": {"_InvFade": 1, "_WrappedDiffuse": 0.2, "_Translucency": 0.5, "_AlphaInfluence": 1,
                       "_EnableAlbedo": 0, "_EnableEmission": 0, "_EnableFlipbookBlending": 0},
            "colours": {"_Color": [1, 1, 1, 1], "_CamFadeDistance": [2, 150, 25, 0]},
            "keywords": [], "streams": [], "blend": "alpha"},
    "lit_custom": {"game": "Custom/LitParticles",
                   "floats": {"_ZFadeDistance": 0.3, "_CameraFadeDistanceMin": 0, "_CameraFadeDistanceMax": 0.1,
                              "_BumpScale": 1, "_LightNormalFactor": 0, "_Billboard": 0},
                   "colours": {"_Color": [1, 1, 1, 1], "_EmissionColor": [0, 0, 0, 1]},
                   "keywords": [], "streams": [], "blend": "alpha"},
    "standard_unlit": {"game": "Particles/Standard Unlit2", "floats": {"_Cull": 0, "_ColorMode": 0},
                       "colours": {"_Color": [1, 1, 1, 1]}, "keywords": [], "streams": ["Position", "Color", "UV"],
                       "blend": "alpha"},
    "standard_lit": {"game": "Particles/Standard Surface2", "floats": {"_Cull": 0, "_Glossiness": 0, "_Metallic": 0},
                     "colours": {"_Color": [1, 1, 1, 1]}, "keywords": [], "streams": [], "blend": "opaque"},
    "legacy_additive": {"game": "Legacy Shaders/Particles/Additive", "floats": {"_InvFade": 1},
                        "colours": {"_TintColor": [0.5, 0.5, 0.5, 0.5]}, "keywords": [], "streams": [],
                        "blend": "additive_alpha"},
    "legacy_alpha": {"game": "Legacy Shaders/Particles/Alpha Blended", "floats": {"_InvFade": 1},
                     "colours": {"_TintColor": [0.5, 0.5, 0.5, 0.5]}, "keywords": [], "streams": [], "blend": "alpha"},
    "decal": {"game": "Custom/ParticleDecal", "floats": {"_ZFadeDistance": 0.3, "_ZFadeDistanceNear": 0, "_FadePower": 1,
                                                         "_Glossiness": 0, "_Metallic": 0},
              "colours": {"_Color": [1, 1, 1, 1]}, "keywords": [],
              "streams": ["Position", "Normal", "Color", "UV", "Tangent", "Velocity"], "blend": "alpha"},
    "opaque_lit": {"game": "Standard", "floats": {"_Glossiness": 0.1, "_Metallic": 0}, "colours": {"_Color": [1, 1, 1, 1]},
                   "keywords": [], "streams": [], "blend": "opaque"},
}
BLENDS = {"additive_soft": (3, 1), "additive_alpha": (5, 1), "additive": (1, 1), "alpha": (5, 10),
          "premultiplied": (1, 10), "multiply": (2, 0), "opaque": (1, 0)}
# Unity's standard particle shader modes (the game's Particles/Standard Unlit2 and the bundle's placeholder).
STANDARD_MODES = {"opaque": (0, []), "alpha": (2, ["_ALPHABLEND_ON"]), "premultiplied": (3, ["_ALPHAPREMULTIPLY_ON"]),
                  "additive_alpha": (4, ["_ALPHABLEND_ON"]), "additive": (4, ["_ALPHABLEND_ON"]),
                  "additive_soft": (4, ["_ALPHABLEND_ON"]), "multiply": (6, ["_ALPHAMODULATE_ON"])}


def texture(name, t):
    return {"name": name, "file": f"{name}.png", "make": t.get("make", ""), "srgb": t.get("srgb", True), "point": t.get("point", False),
            "mips": t.get("mips", not t.get("point", False)), "wrap": t.get("wrap", "clamp"),
            "max_size": t.get("max_size", 512)}


def material(name, m):
    """A material: which game shader it dresses into, its blend and every float, colour and keyword for it."""
    shader = SHADERS[m.get("shader", "standard_unlit")]
    blend = m.get("blend", shader["blend"])
    floats = dict(shader["floats"])
    floats["_SrcBlend"], floats["_DstBlend"] = BLENDS[blend]
    floats["_ZWrite"] = 1 if blend == "opaque" else 0
    mode, mode_keywords = STANDARD_MODES[blend]
    if m.get("shader", "standard_unlit").startswith("standard"):
        floats["_Mode"] = mode
    floats.update(m.get("floats", {}))
    _soft_and_fades(m, floats)
    colours = dict(shader.get("colours", {}))
    colours.update({k: list(v) for k, v in m.get("colours", {}).items()})
    keywords = _keywords(shader["keywords"] + m.get("keywords", []) +
                         (mode_keywords if m.get("shader", "").startswith("standard") else []), floats)
    return {"name": name, "texture": m.get("texture", ""), "game_shader": shader["game"], "borrow": m.get("borrow", ""),
            "blend": blend, "keywords": keywords, "floats": [{"k": k, "v": float(v)} for k, v in sorted(floats.items())],
            "colours": [{"k": k, "v": [float(x) for x in v]} for k, v in sorted(colours.items())],
            "queue": int(m.get("queue", 2000 if blend == "opaque" else 3000)), "placeholder_mode": mode,
            "placeholder_keywords": mode_keywords, "streams": shader["streams"]}


def _keywords(keywords, floats):
    """The material's keywords, sorted, without those of [Toggle] properties that are off: the two must agree."""
    keywords = set(keywords)
    for toggle, keyword in (("_SoftParticles", "_SOFTPARTICLES_ON"), ("_GradientAsAlpha", "_GRADIENTASALPHA_ON")):
        if floats.get(toggle) == 0:
            keywords.discard(keyword)
    return sorted(keywords)


def _soft_and_fades(m, floats):
    """Short names for the fades every particle shader has, in the shader's own property names."""
    if "soft" in m:
        for key in ("_SoftFadeFactor", "_InvFade", "_ZFadeDistance"):
            if key in floats:
                floats[key] = m["soft"]
        if "_SoftParticles" in floats:
            floats["_SoftParticles"] = 1 if m["soft"] else 0
    if "camera_fade" in m and "_CameraFadeFactor" in floats:
        floats["_CameraFadeFactor"] = m["camera_fade"]

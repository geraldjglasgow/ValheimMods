"""How one of the game's particle systems is drawn, in the codex's words: its ParticleSystemRenderer (render mode,
sorting, size limits, stretching, vertex streams, meshes), its material (shader, texture, blend, softness, fades,
tints) and its transform. vfx_modules uses these for every system; vfx_effect for lights, trails and meshes.
"""
import math

import game
from vfx_read import ALIGNMENTS, BLENDS, RENDER_MODES, SORT_MODES, VERTEX_STREAMS, parse


def num(value, digits=4):
    return round(value, digits) if isinstance(value, float) else value


def vec(value):
    return [round(value[k], 4) for k in "xyz" if k in value] if isinstance(value, dict) else None


def ref_path(ref):
    """The export path a '{fileID, guid}' reference points at, or None."""
    if not isinstance(ref, dict) or not ref.get("guid"):
        return None
    return game.path_of("{fileID: %d, guid: %s}" % (int(ref.get("fileID", 0)), ref["guid"]))


def renderer(body):
    """A ParticleSystemRenderer: render mode, sorting, size limits, stretching, vertex streams, meshes, materials."""
    r = parse(body)
    materials = game.renderer_materials(body)
    meshes = [ref_path(r.get(k)) for k in ("m_Mesh", "m_Mesh1", "m_Mesh2", "m_Mesh3")]
    return {"enabled": r.get("m_Enabled") == 1.0, "mode": RENDER_MODES[int(r.get("m_RenderMode", 0))],
            "sort": SORT_MODES[int(r.get("m_SortMode", 0))], "fudge": num(r.get("m_SortingFudge")),
            "order": int(r.get("m_SortingOrder", 0)), "min_size": num(r.get("m_MinParticleSize")),
            "max_size": num(r.get("m_MaxParticleSize")), "length_scale": num(r.get("m_LengthScale")),
            "speed_scale": num(r.get("m_VelocityScale")), "camera_speed_scale": num(r.get("m_CameraVelocityScale")),
            "alignment": ALIGNMENTS[int(r.get("m_RenderAlignment", 0))], "pivot": vec(r.get("m_Pivot")),
            "flip": vec(r.get("m_Flip")), "shadows": r.get("m_CastShadows") not in (0.0, None),
            "streams": vertex_streams(r), "meshes": [m for m in meshes if m],
            "material": material_summary(materials[0]) if materials and materials[0] else None,
            "trail_material": material_summary(materials[1]) if len(materials) > 1 and materials[1] else None}


def vertex_streams(r):
    """The custom vertex streams a renderer feeds its shader, by name, or None when it uses the default set."""
    if r.get("m_UseCustomVertexStreams") != 1.0:
        return None
    blob = str(r.get("m_VertexStreams", "") or "")
    codes = [int(blob[i:i + 2], 16) for i in range(0, len(blob) - 1, 2)]
    return [VERTEX_STREAMS[c] if c < len(VERTEX_STREAMS) else f"#{c}" for c in codes]


BUILTIN_SHADERS = {"builtin_200": "Legacy Particles Additive", "builtin_203": "Legacy Particles Alpha Blended",
                   "builtin_46": "Standard", "builtin_45": "Standard (Specular setup)"}


def material_summary(path):
    """What decides a particle material's look: shader, texture and its size, blend, softness, fades and tints.
    Unity's built-in shaders are named from the game's own bundles (a material there points at the shader object)."""
    if not path:
        return None
    try:
        m = dict(game.material(path))
    except (OSError, ValueError):
        return {"path": path, "shader": "missing"}
    m["shader"] = BUILTIN_SHADERS.get(m["shader"], m["shader"])
    floats, colours = m["floats"], m["colors"]
    tex = m["textures"].get("_MainTex")
    return {"path": path, "name": m["name"], "shader": m["shader"], "texture": tex,
            "texture_px": list(game.texture_size(tex) or []) if tex else None,
            "normal": m["textures"].get("_NormalTex") or m["textures"].get("_BumpMap"),
            "blend": blend_of(m), "soft": _soft(m), "camera_fade": _camera_fade(m),
            "colour": _rgba(colours.get("_Color")), "tint": _rgba(colours.get("_TintColor")),
            "emission": _rgba(colours.get("_EmissionColor")),
            "alpha_channel": floats.get("_AlphaChannel"), "gradient_channel": floats.get("_GradientChannel"),
            "gradient_as_alpha": floats.get("_GradientAsAlpha"), "fog": floats.get("_FejdFog"),
            "cull": floats.get("_Cull"), "billboard": floats.get("_Billboard"), "sky_mask": floats.get("_SkyMask"),
            "light_normal": floats.get("_LightNormalFactor"), "keywords": m["keywords"]}


def _rgba(values):
    return [round(v, 3) for v in values] if values else None


def blend_of(m):
    """The blend a particle material draws with, in words: additive, alpha, premultiplied, multiply, ..."""
    shader, floats = m["shader"], m["floats"]
    if shader in ("ParticleUnlit", "ParticleGradientMapped_Unlit", "Particle Standard Unlit", "Particle Standard Surface"):
        pair = (int(floats.get("_SrcBlend", 1)), int(floats.get("_DstBlend", 0)))
        return BLENDS.get(pair, f"src{pair[0]}_dst{pair[1]}")
    if shader in ("LitParticles", "Lux Lit Particles Bumped", "Lux Lit Particles Tess Bumped"):
        return "alpha_lit"
    if shader in ("ParticleDecal", "Decal", "ShadowBlob", "Legacy Particles Alpha Blended"):
        return "alpha"
    if shader == "Legacy Particles Additive":
        return "additive_alpha"
    if shader in ("MeshFlipbook",):
        return "additive_soft"
    if shader == "Distortion":
        return "distortion"
    return "opaque_lit"


def _soft(m):
    f = m["floats"]
    if f.get("_SoftParticles") == 1.0:
        return {"factor": f.get("_SoftFadeFactor"), "near": f.get("_SoftNearFade")}
    if m["shader"] in ("LitParticles", "ShadowBlob", "ParticleDecal", "Decal"):
        return {"distance": f.get("_ZFadeDistance")}
    if "_InvFade" in f and m["shader"].startswith(("Legacy", "Lux")):
        return {"inv_fade": f.get("_InvFade")}
    if f.get("_SoftParticlesEnabled") == 1.0:
        return {"near": f.get("_SoftParticlesNearFadeDistance"), "far": f.get("_SoftParticlesFarFadeDistance")}
    return None


def _camera_fade(m):
    f = m["floats"]
    if m["shader"] == "LitParticles":
        return {"min": f.get("_CameraFadeDistanceMin"), "max": f.get("_CameraFadeDistanceMax"),
                "y": f.get("_CameraYFadeDistance")}
    if "_CameraFadeFactor" in f and m["shader"] in ("ParticleUnlit", "ParticleGradientMapped_Unlit"):
        return {"factor": f.get("_CameraFadeFactor")}
    if m["shader"].startswith("Lux") and "_CamFadeDistance" in m["colors"]:
        near, far, far_range = m["colors"]["_CamFadeDistance"][:3]
        return {"near": near, "far": far, "far_range": far_range}
    if f.get("_CameraFadingEnabled") == 1.0:
        return {"near": f.get("_CameraNearFadeDistance"), "far": f.get("_CameraFarFadeDistance")}
    return None


def transform(prefab, node):
    body = parse(prefab.docs[node][1])
    return {"position": vec(body.get("m_LocalPosition")), "euler": quaternion_euler(body.get("m_LocalRotation")),
            "scale": vec(body.get("m_LocalScale")), "world_scale": round(prefab.world_scale(node), 4)}


def quaternion_euler(q):
    """Unity's euler angles (degrees, applied Z, X, Y) of a {x, y, z, w} quaternion."""
    if not isinstance(q, dict):
        return None
    x, y, z, w = (q.get(k, 0.0) for k in "xyzw")
    ex = math.degrees(math.asin(max(-1.0, min(1.0, 2 * (w * x - y * z)))))
    ey = math.degrees(math.atan2(2 * (w * y + x * z), 1 - 2 * (x * x + y * y)))
    ez = math.degrees(math.atan2(2 * (w * z + x * y), 1 - 2 * (x * x + z * z)))
    return [round(ex, 2), round(ey, 2), round(ez, 2)]

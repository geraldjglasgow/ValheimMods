"""Materials whose look comes from settings more than paint: the ones the game leaves untextured (a flat colour,
metal and gloss, often a borrowed rock normal map: ingots, crystals, ice) and the ones that tint a texture with their
colour (a grey or pale texture made into many things)."""
import collections

import numpy as np

import game
import paint_image
import shaders_usage

REAL = {"item", "piece", "creature", "env", "location", "ship", "player"}


def survey():
    """{untextured: [...], tinted: {per shader counts, examples}} over materials the game's meshes draw."""
    untextured, tinted, counts = [], [], collections.Counter()
    for material, entry in shaders_usage.scan().items():
        if not set(entry["kinds"]) & REAL or not set(entry["renderers"]) & {"mesh", "skinned", "override"}:
            continue
        try:
            info = game.material(material)
        except (OSError, ValueError):
            continue
        counts[info["shader"]] += 1
        if not info["textures"].get("_MainTex"):
            untextured.append(_flat(material, info, entry))
        elif _is_tinted(info):
            tinted.append(_tinted(material, info, entry))
    return {"untextured": sorted(untextured, key=lambda m: m["material"]),
            "tinted": {"count": len(tinted), "of": sum(counts.values()),
                       "per_shader": dict(collections.Counter(t["shader"] for t in tinted).most_common()),
                       "examples": sorted(tinted, key=lambda t: -t["prefabs"])[:60]}}


def _colour(info, name, default=(1.0, 1.0, 1.0, 1.0)):
    return [round(c, 3) for c in info["colors"].get(name, default)]


def _flat(material, info, entry):
    floats = info["floats"]
    return {"material": material, "shader": info["shader"], "kinds": sorted(set(entry["kinds"]) & REAL),
            "color": _colour(info, "_Color"), "metallic": floats.get("_Metallic"),
            "glossiness": floats.get("_Glossiness"), "emission": _colour(info, "_EmissionColor", (0, 0, 0, 0)),
            "normal_map": info["textures"].get("_BumpMap"), "mode": floats.get("_Mode"), "prefabs": entry["prefabs"]}


def _is_tinted(info):
    colour = info["colors"].get("_Color", (1.0, 1.0, 1.0, 1.0))[:3]
    return max(abs(c - 1.0) for c in colour) > 0.05


def _tinted(material, info, entry):
    """A tinted material: its tint and the texture's median colour before and after it (sRGB hex)."""
    texture = info["textures"]["_MainTex"]
    rgba = paint_image.load(texture)
    before = np.median(paint_image.linear(rgba[..., :3]).reshape(-1, 3), axis=0)
    after = before * paint_image.linear(np.array(info["colors"]["_Color"][:3]))
    return {"material": material, "shader": info["shader"], "texture": texture,
            "kinds": sorted(set(entry["kinds"]) & REAL),
            "tint": _colour(info, "_Color")[:3], "texture_median": _hex(before), "tinted_median": _hex(after),
            "texture_saturation": round(float(paint_image.hsv(paint_image.to_srgb(before))[1]), 3),
            "prefabs": entry["prefabs"]}


def _hex(linear_rgb):
    srgb = paint_image.to_srgb(np.asarray(linear_rgb))
    return "#" + "".join(f"{int(round(c * 255)):02x}" for c in srgb)


if __name__ == "__main__":
    import json
    result = survey()
    print(len(result["untextured"]), "untextured;", json.dumps(result["tinted"], indent=1)[:3000])

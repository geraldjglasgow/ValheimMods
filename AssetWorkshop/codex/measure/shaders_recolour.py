"""The game's own recolour mechanisms, measured: Creature-shader hue/saturation/value on materials, LevelEffects per
star level, MaterialVariation swaps, RandomMaterialValues ranges, ItemStyle style textures, MaterialVariationWorld
swaps by biome or dungeon theme, and the player's skin/hair colour property.

    shaders_recolour.survey()     # the "recolour" section of data/shaders.json
"""
import os
import re

import game
import shaders_usage

SCRIPTS = {"LevelEffects": -1094038755, "MaterialVariation": -124776421, "RandomMaterialValues": 1815218749,
           "ItemStyle": -923427435, "MaterialVariationWorld": 901333681}
REAL = {"item", "piece", "creature", "env", "location", "ship", "player"}
_FLOAT = r"(-?[\d.]+(?:[eE][-+]?\d+)?)"


def survey():
    holders = _holders()
    return {"hsv_materials": hsv_materials(), "level_effects": [level_effects(p) for p in holders["LevelEffects"]],
            "material_variation": [material_variation(p) for p in holders["MaterialVariation"]],
            "random_material_values": [random_values(p) for p in holders["RandomMaterialValues"]],
            "item_style": item_styles(holders["ItemStyle"]),
            "material_variation_world": _world(holders["MaterialVariationWorld"])}


def _world(paths):
    return {"prefabs": len(paths), "examples": [os.path.basename(p) for p in paths[:12]]}


def _holders():
    """{script: [prefab paths whose text references it]} (plain text search, no parsing)."""
    found = {name: [] for name in SCRIPTS}
    for path in game.find("**/*.prefab"):
        text = game.unity.read(path)
        for name, file_id in SCRIPTS.items():
            if f"fileID: {file_id}, guid: " in text:
                found[name].append(path)
    return found


def hsv_materials():
    """Drawn materials whose _Hue, _Saturation or _Value is not zero."""
    found = []
    for material, entry in shaders_usage.scan().items():
        if not set(entry["kinds"]) & REAL:
            continue
        floats = game.material(material)["floats"]
        hsv = [round(floats.get(k, 0.0), 3) for k in ("_Hue", "_Saturation", "_Value")]
        if any(abs(v) > 1e-4 for v in hsv):
            found.append({"material": material, "hsv": hsv,
                          "texture": game.material(material)["textures"].get("_MainTex"),
                          "shader": game.material(material)["shader"], "kinds": sorted(set(entry["kinds"]) & REAL)})
    return found


def _bodies(path, script):
    """The YAML bodies of one script's components in a prefab."""
    text = game.unity.read(path)
    parts = re.split(r"^--- !u!", text, flags=re.M)
    return [p for p in parts if f"m_Script: {{fileID: {SCRIPTS[script]}, guid:" in p]


def level_effects(path):
    """Per star level: scale, hue, saturation, value, emissive colour, and whether an object is switched on."""
    prefab = os.path.splitext(os.path.basename(path))[0]
    setups = []
    for body in _bodies(path, "LevelEffects"):
        for block in re.split(r"^  - m_scale: ", body, flags=re.M)[1:]:
            number = lambda name: float(re.search(rf"m_{name}: {_FLOAT}", block).group(1))
            setups.append({"scale": float(block.split("\n")[0]), "hue": number("hue"),
                           "saturation": number("saturation"), "value": number("value"),
                           "emissive": bool(number("setEmissiveColor")),
                           "emissive_color": [float(v) for v in re.findall(rf"\w: {_FLOAT}",
                                              re.search(r"m_emissiveColor: \{([^}]*)\}", block).group(1))],
                           "enables_object": "m_enableObject: {fileID: 0}" not in block})
    return {"prefab": prefab, "levels": setups}


def material_variation(path):
    """The materials a MaterialVariation picks between, with weights."""
    entries = []
    for body in _bodies(path, "MaterialVariation"):
        index = re.search(r"m_materialIndex: (\d+)", body)
        pairs = re.findall(r"m_material: (\{[^}]*\})\s+m_weight: " + _FLOAT, body)
        entries.append({"slot": int(index.group(1)) if index else 0,
                        "materials": [{"material": game.path_of(m), "weight": float(w)} for m, w in pairs]})
    return {"prefab": os.path.splitext(os.path.basename(path))[0], "variations": entries}


def random_values(path):
    """The shader properties a RandomMaterialValues sets per placed object, and their ranges."""
    found = []
    for body in _bodies(path, "RandomMaterialValues"):
        for block in re.split(r"^  - m_propertyNames:", body, flags=re.M)[1:]:
            names = re.findall(r"^    - (\w+)$", block, flags=re.M)
            low = re.search(r"m_minimum: \{([^}]*)\}", block)
            high = re.search(r"m_maximum: \{([^}]*)\}", block)
            found.append({"properties": names, "min": game.unity.numbers(low.group(0)) if low else None,
                          "max": game.unity.numbers(high.group(0)) if high else None})
    return {"prefab": os.path.splitext(os.path.basename(path))[0], "properties": found}


def item_styles(paths):
    """Items with an ItemStyle (their material's _StyleTex holds one column per variant, _Style picks one)."""
    found = []
    for path in paths:
        text = game.unity.read(path)
        variants = re.search(r"m_variants: (\d+)", text)
        refs = re.findall(r"^  - (\{fileID: 2100000, guid: \w+, type: 2\})$", text, re.M)
        materials = {game.path_of(m) for m in refs}
        styles = sorted({game.material(m)["textures"].get("_StyleTex") for m in materials if m} - {None})
        found.append({"prefab": os.path.splitext(os.path.basename(path))[0],
                      "variants": int(variants.group(1)) if variants else None, "style_textures": styles})
    return found

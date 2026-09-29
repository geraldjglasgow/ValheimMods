"""Reads an item's shared data (ItemDrop.m_itemData.m_shared) from the ItemDrop body of a prefab: its type, skill,
animation state, icons, armour material, helmet flags, attacks and the effect lists the game plays for it. Plain text
parsing of the Unity YAML, no decoding.

    shared = items_shared.read(body)        # body: the ItemDrop MonoBehaviour's YAML text
    shared["item_type"], shared["effects"]["hit"], shared["attack"]["projectile"]
"""
import os
import re

import game

ITEM_TYPES = {0: "None", 1: "Material", 2: "Consumable", 3: "OneHandedWeapon", 4: "Bow", 5: "Shield", 6: "Helmet",
              7: "Chest", 9: "Ammo", 10: "Customization", 11: "Legs", 12: "Hands", 13: "Trophy",
              14: "TwoHandedWeapon", 15: "Torch", 16: "Misc", 17: "Shoulder", 18: "Utility", 19: "Tool",
              20: "Attach_Atgeir", 21: "Fish", 22: "TwoHandedWeaponLeft", 23: "AmmoNonEquipable", 24: "Trinket"}
ANIMATION_STATES = ["Unarmed", "OneHanded", "TwoHandedClub", "Bow", "Shield", "Torch", "LeftTorch", "Atgeir",
                    "TwoHandedAxe", "FishingRod", "Crossbow", "Knives", "Staves", "Greatsword", "MagicItem",
                    "DualAxes", "Feaster", "Scythe"]
SKILLS = {0: "None", 1: "Swords", 2: "Knives", 3: "Clubs", 4: "Polearms", 5: "Spears", 6: "Blocking", 7: "Axes",
          8: "Bows", 9: "ElementalMagic", 10: "BloodMagic", 11: "Unarmed", 12: "Pickaxes", 13: "WoodCutting",
          14: "Crossbows", 104: "Fishing", 105: "Cooking", 106: "Farming", 107: "Crafting"}
HAIR_SETTINGS = ["Default", "Hidden", "HiddenHat", "HiddenHood", "HiddenNeck", "HiddenScarf"]
SHARED_EFFECTS = ("hitEffect", "hitTerrainEffect", "blockEffect", "startEffect", "holdStartEffect", "equipEffect",
                  "unequipEffect", "triggerEffect", "trailStartEffect", "buildEffect", "destroyEffect")
ATTACK_EFFECTS = ("hitEffect", "hitTerrainEffect", "startEffect", "triggerEffect", "trailStartEffect", "burstEffect")
SHARED = 6      # indentation of m_shared's own fields
ATTACK = 8      # indentation of an attack's fields


def read(body):
    """The shared data of one ItemDrop body as a plain dict (names, enums as names, effect prefab names)."""
    shared = block(body, "m_shared", 4)
    found = _identity(shared)
    found.update(_numbers(shared))
    found.update(_looks(shared))
    return found


def _identity(shared):
    """What kind of item it is: the fields the codex classifies by."""
    number = lambda name: int(_number(value(shared, name)))
    return {"name": value(shared, "m_name"),
            "item_type": ITEM_TYPES.get(number("m_itemType"), "?"),
            "attach_override": ITEM_TYPES.get(number("m_attachOverride"), "?"),
            "animation_state": _enum(ANIMATION_STATES, number("m_animationState")),
            "skill": SKILLS.get(number("m_skillType"), "?"),
            "set_name": value(shared, "m_setName"),
            "ammo_type": value(shared, "m_ammoType"),
            "build_pieces": game.path_of(value(shared, "m_buildPieces"))}


def _numbers(shared):
    """Stack, quality, weight, food and armour values: they tell materials from food from gear."""
    number = lambda name: _number(value(shared, name))
    return {"tool_tier": int(number("m_toolTier")), "max_stack": int(number("m_maxStackSize")),
            "max_quality": int(number("m_maxQuality")), "weight": number("m_weight"), "value": number("m_value"),
            "variants": int(number("m_variants")), "food": number("m_food"),
            "food_stamina": number("m_foodStamina"), "food_eitr": number("m_foodEitr"),
            "is_drink": number("m_isDrink") == 1, "armor": number("m_armor"), "block_power": number("m_blockPower"),
            "damages": _damages(block(shared, "m_damages", SHARED))}


def _looks(shared):
    """What the item shows: icons, armour material, hair and beard flags, attacks and effect lists."""
    return {"icons": [game.path_of(item) for item in list_items(shared, "m_icons", SHARED)],
            "armor_material": game.path_of(value(shared, "m_armorMaterial")),
            "hide_hair": _enum(HAIR_SETTINGS, _number(value(shared, "m_helmetHideHair"))),
            "hide_beard": _enum(HAIR_SETTINGS, _number(value(shared, "m_helmetHideBeard"))),
            "hair_settings": _hair(shared, "m_helmetHairSettings"),
            "beard_settings": _hair(shared, "m_helmetBeardSettings"),
            "effects": {name: effect_names(block(shared, "m_" + name, SHARED)) for name in SHARED_EFFECTS},
            "attack": _attack(block(shared, "m_attack", SHARED)),
            "secondary": _attack(block(shared, "m_secondaryAttack", SHARED))}


def _attack(text):
    """The fields of one attack that say what it looks like: animation, projectile and effects."""
    if not text:
        return {}
    return {"type": int(_number(value(text, "m_attackType", ATTACK))),
            "animation": value(text, "m_attackAnimation", ATTACK),
            "projectile": _prefab_name(value(text, "m_attackProjectile", ATTACK)),
            "projectile_path": game.path_of(value(text, "m_attackProjectile", ATTACK) or "{}"),
            "bow_draw": value(text, "m_bowDraw", ATTACK) == "1",
            "reload": value(text, "m_requiresReload", ATTACK) == "1",
            "effects": {name: effect_names(block(text, "m_" + name, ATTACK)) for name in ATTACK_EFFECTS}}


def _damages(text):
    """{kind: amount} of the non-zero damages."""
    found = re.findall(rf"^ {{{SHARED + 2}}}m_(\w+): ({game.unity.NUMBER})$", text, re.MULTILINE)
    return {kind: float(amount) for kind, amount in found if float(amount)}


def _hair(text, name):
    return [_enum(HAIR_SETTINGS, _number(m)) for m in re.findall(r"m_setting: (\d+)", block(text, name, SHARED))]


def effect_names(text):
    """The prefab names in one EffectList block (m_effectPrefabs), enabled entries only, in order."""
    names = []
    for entry in re.split(r"^\s*- m_prefab: ", text, flags=re.MULTILINE)[1:]:
        enabled = re.search(r"m_enabled: (\d)", entry)
        name = _prefab_name(entry.splitlines()[0])
        if name and (not enabled or enabled.group(1) == "1"):
            names.append(name)
    return names


def _prefab_name(ref_text):
    path = game.path_of(ref_text) if ref_text else None
    return os.path.splitext(os.path.basename(path))[0] if path else None


def value(text, name, indent=SHARED):
    """The raw value of the field `name` written at exactly `indent` spaces, '' when absent."""
    match = re.search(rf"^ {{{indent}}}{name}: ?(.*)$", text, re.MULTILINE)
    return match.group(1).strip() if match else ""


def block(text, name, indent):
    """The lines under 'name:' at `indent`: every later line indented deeper, or a list dash at the same indent."""
    match = re.search(rf"^ {{{indent}}}{name}:.*$", text, re.MULTILINE)
    if not match:
        return ""
    lines = []
    for line in text[match.end() + 1:].splitlines():
        depth = len(line) - len(line.lstrip())
        if line.strip() and depth <= indent and not (depth == indent and line.lstrip().startswith("- ")):
            break
        lines.append(line)
    return "\n".join(lines)


def list_items(text, name, indent):
    """The '- {…}' entries of a list field written at `indent`."""
    return re.findall(rf"^ {{{indent}}}- (\{{.*\}})$", block(text, name, indent), re.MULTILINE)


def _number(text):
    try:
        return float(text)
    except ValueError:
        return 0.0


def _enum(names, number):
    index = int(number)
    return names[index] if 0 <= index < len(names) else str(index)

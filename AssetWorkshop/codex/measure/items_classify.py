"""Sorts the scanned items into the codex's categories (weapon.sword, armour.cape, item.trophy ...) from their shared
data: item type, skill, animation state and name, as the game itself tells its items apart. Also gives each item its
tier (the biome whose materials its recipe needs) and, for materials, a family (metal, wood, hide ...).

    items_classify.category(record)      # 'weapon.axe_2h', or None for what the codex leaves out
    items_classify.tier(record)          # 0 (meadows) .. 7 (deep north), or None
"""
import functools
import os
import re

import game
from workshop import unity

TIERS = ["meadows", "black forest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deep north"]
TIER_MATERIALS = [
    "Wood Stone Flint LeatherScraps DeerHide Resin Feathers HardAntler TrophyDeer Raspberry Honey",
    "Bronze Copper Tin FineWood RoundLog TrollHide Coal SurtlingCore GreydwarfEye BoneFragments BjornHide BjornPaw "
    "TrophyBjorn AncientSeed CryptKey",
    "Iron ElderBark Chain Root Guck WitheredBone Ooze Bloodbag TrophyDraugrElite Entrails",
    "Silver WolfPelt WolfFang WolfClaw Obsidian FreezeGland DragonTear Crystal TrophyHatchling SerpentScale Chitin "
    "WolfHairBundle TrophyFenring TrophyFrostTroll",
    "BlackMetal LinenThread Needle LoxPelt Tar Barley Cloudberry TrophyGoblin UndeadBjornRibcage TrophyBjornUndead",
    "Eitr Carapace YggdrasilWood BlackCore Bilebag Mandible ScaleHide BlackMarble Sap Wisp RoyalJelly TrophySeeker "
    "MushroomMagecap MushroomJotunPuffs TrophyGjall",
    "FlametalNew AskHide CharredBone GemstoneRed GemstoneBlue GemstoneGreen Blackwood Grausten CelestialFeather "
    "MoltenCore AskBladder MorgenSinew MorgenHeart Vineberry SulfurStone ProustitePowder BonemawSerpentTooth",
    "Gold Frostwood MooseHide MooseSinew NornThread ElakingHairBundle SealHide SealBlubber OrbThunderBlood OrbFrostFire",
]
NAME_TIERS = [("Flint|Stone|Wood|Antler|Leather|Rags|Deer", 0), ("Bronze|Troll|Copper", 1), ("Iron|Root|Elder", 2),
              ("Silver|Wolf|Fenring|Drake|Obsidian|Crystal|Chitin|Serpent", 3), ("Black[Mm]etal|Lox|Padded|Needle", 4),
              ("Carapace|Mage|Mistwalker|Krom|Demolisher|SkullSplittur|HimminAfl|Splitner|SpineSnap|Arbalest", 5),
              ("Flametal|Ashlands|Charred|Berzerkr|Eldner|Niedhogg|Slayer|Ripper|Dyrnwyn", 6),
              ("Gold|DeepNorth|DN", 7)]
VARIANT = re.compile(r"_?(BloodLightning|FrostFire|Blood|Lightning|Nature|Fire)$")
MATERIAL_FAMILIES = [("metal", r"Ore|Scrap|Bronze|Iron|Silver|Copper|Tin|BlackMetal|Flametal|Gold|Nails|Chain|Ingot"),
                     ("wood", r"Wood|Log|Bark|Stick"), ("hide", r"Hide|Pelt|Leather|Scale|Straps|Blubber"),
                     ("bone", r"Bone|Fang|Tooth|Skull|Antler|Claw|Horn|Mandible|Chitin|Carapace|Ribcage|Paw"),
                     ("stone", r"Stone|Flint|Obsidian|Marble|Grausten|Coal|Sulfur|Ice"),
                     ("plant", r"Seed|Flax|Barley|Root|Thistle|Dandelion|Sap|Vine|Oat|Flour|Resin|Tar|Feather"),
                     ("thread", r"Thread|Sinew|Bundle|Hair")]


def category(record):
    """The codex category of one scanned item, or None when it has no model of its own or is left out
    (hair and beards, the unarmed 'weapon', creature attacks that draw nothing)."""
    shared, path = record["shared"], record["prefab"]
    if not (record["held"] or record["worn"] or record["drop"]):
        return None
    if not path.startswith(("GameElements", "world")):
        return _creature(record)
    return _gear(shared, record["name"]) or _thing(shared, record["name"], path)


def _gear(shared, name):
    """Weapons, tools, shields and armour."""
    kind, skill, anim = shared["item_type"], shared["skill"], shared["animation_state"]
    if kind in ("Ammo", "AmmoNonEquipable"):
        return {"Bows": "ammo.arrow", "Crossbows": "ammo.bolt"}.get(skill, "item.bait")
    if kind == "Bow":
        return {"Bows": "weapon.bow", "Crossbows": "weapon.crossbow"}.get(skill, "tool.misc")
    if kind == "Shield":
        return "shield.tower" if "Tower" in name else "shield.buckler" if "Buckler" in name else "shield.round"
    if kind in ("Helmet", "Chest", "Legs", "Utility", "Trinket"):
        return "armour." + {"Chest": "chest", "Legs": "legs", "Helmet": "helmet"}.get(kind, kind.lower())
    if kind == "Shoulder":
        return "armour.cape" if "Cape" in name else "armour.shoulder"
    if kind == "Torch":
        return "tool.torch"
    if kind == "Tool" or skill == "Farming":
        return {"Hammer": "tool.hammer"}.get(name, "tool.farming" if name in ("Hoe", "Cultivator", "Shovel", "Scythe")
                                            else "tool.misc")
    if kind in ("OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft"):
        return _weapon(skill, anim, name)
    return None


def _weapon(skill, anim, name):
    if name in ("PlayerUnarmed",) or name.endswith("_attack"):
        return None
    if anim == "Torch":
        return "tool.tankard"
    if anim == "FishingRod":
        return "tool.fishing_rod"
    if skill == "Pickaxes":
        return "tool.pickaxe"
    if skill in ("ElementalMagic", "BloodMagic"):
        return "weapon.staff"
    by_anim = {"DualAxes": "weapon.axe_dual", "TwoHandedAxe": "weapon.axe_2h", "TwoHandedClub": "weapon.sledge",
               "Greatsword": "weapon.greatsword", "Atgeir": "weapon.atgeir"}
    if anim in by_anim:
        return by_anim[anim]
    return {"Swords": "weapon.sword", "Axes": "weapon.axe_1h", "Clubs": "weapon.mace", "Knives": "weapon.knife",
            "Spears": "weapon.spear", "Unarmed": "weapon.fist"}.get(skill, "weapon.bomb" if skill == "None" else None)


def _thing(shared, name, path):
    """Trophies, food, meads, gems, valuables, keys, materials and the rest."""
    kind = shared["item_type"]
    if kind == "Customization" or "/Upgrades/" in path:
        return None if kind == "Customization" else "item.misc"
    if kind == "Trophy":
        return "item.trophy"
    if "/Pieces/Feast" in path:
        return "item.feast"
    if shared["is_drink"] or name.startswith("MeadBase") or name == "BarleyWineBase":
        return "item.mead"
    if shared["food"] > 0 or kind == "Consumable":
        return "item.food"
    if re.search(r"Gem|Ruby|Amber|Jewel", name):
        return "item.gem"
    if "/valuables/" in path:
        return "item.valuable"
    if "Key" in name and "Mold" not in name:
        return "item.key"
    return "item.material" if kind == "Material" else "item.misc"


def _creature(record):
    """Weapons a creature holds that draw a model (the Fallen Valkyrie's copies of player gear left out)."""
    if record["prefab"].startswith("Characters/animals/fishes/"):
        return "item.fish"
    if "/FallenWarrior/" in record["prefab"] or not record["held"]:
        return None
    if record["shared"]["item_type"] in ("OneHandedWeapon", "TwoHandedWeapon", "Bow", "Shield"):
        return "weapon.creature"
    return None


def material_family(name):
    return next((family for family, pattern in MATERIAL_FAMILIES if re.search(pattern, name)), "other")


def tier(record):
    """The biome tier of an item: the latest biome among its recipe's materials, else a guess from its name."""
    resources = recipes().get(record["name"])
    if resources:
        found = [i for i, words in enumerate(TIER_MATERIALS) for r in resources if r in words.split()]
        if found:
            return max(found)
    return next((t for pattern, t in reversed(NAME_TIERS) if re.search(pattern, record["name"])), None)


def base_name(name):
    """The item an enchanted or elemental variant is made from ('SwordNiedhoggBlood' -> 'SwordNiedhogg')."""
    return VARIANT.sub("", name)


@functools.lru_cache(maxsize=None)
def recipes():
    """{item prefab name: [resource prefab names]} from every recipe in the export."""
    found = {}
    for path in game.find("GameElements/Recipes/**/*.asset"):
        text = unity.read(path)
        item = game.path_of(unity.field(text, "m_item"))
        if item:
            found[os.path.splitext(os.path.basename(item))[0]] = [
                os.path.splitext(os.path.basename(game.path_of(ref) or ""))[0]
                for ref in re.findall(r"m_resItem: (\{[^}]*\})", text)]
    return found

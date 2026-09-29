"""Writes codex/data/items.json: every item of the game (weapons, tools, shields, ammunition, armour, trophies, food,
meads, materials ...) measured from the reference export and summed up per category.

    python codex/measure/items.py            # uses the scan cache in codex/out/items/scan.json when present
    python codex/measure/items.py --rescan   # reads every item prefab again first

The per-item reading is in items_scan (shared data, models, colliders, trails, glow, cloth); the sorting in
items_classify; the numbers in items_stats; armour and icons add sections of their own (items_armour, items_icons).

The pictures the pages were written from go to codex/out/items/ (gitignored), after this script:

    blender --background --factory-startup --python codex/measure/items_render.py -- [category ...]
    python codex/measure/items_sheets.py        # contact sheets of the renders
    python codex/measure/items_textures.py      # albedo, uv, normal, metal close-ups
    python codex/measure/items_armour.py        # the body's uv layout over the armour textures
    python codex/measure/items_icons.py         # icon sheets
"""
import collections
import json
import os
import sys

import items_armour
import items_classify
import items_icons
import items_scan
import items_stats
import items_textures

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "data", "items.json")
GENERATED = "2026-09-29"
LABELS = {
    "weapon.sword": "one-handed swords", "weapon.axe_1h": "one-handed axes", "weapon.axe_2h": "two-handed axes",
    "weapon.axe_dual": "paired axes (dual wield)", "weapon.mace": "one-handed clubs and maces",
    "weapon.sledge": "sledges (two-handed hammers)", "weapon.knife": "knives and daggers", "weapon.spear": "spears",
    "weapon.atgeir": "atgeirs", "weapon.greatsword": "two-handed swords", "weapon.bow": "bows",
    "weapon.crossbow": "crossbows", "weapon.staff": "staffs", "weapon.fist": "fist weapons (claws)",
    "weapon.bomb": "bombs and throwables", "weapon.creature": "weapons creatures hold",
    "ammo.arrow": "arrows", "ammo.bolt": "crossbow bolts", "shield.round": "round and square shields",
    "shield.tower": "tower shields", "shield.buckler": "bucklers", "tool.pickaxe": "pickaxes",
    "tool.hammer": "the hammer", "tool.farming": "hoe, cultivator, shovel and scythe", "tool.fishing_rod": "fishing rod",
    "tool.torch": "torches and lanterns", "tool.tankard": "tankards", "tool.misc": "other tools",
    "armour.helmet": "helmets and hats", "armour.chest": "chest armour", "armour.legs": "leg armour",
    "armour.cape": "capes", "armour.utility": "utility items (belt, wishbone, shoes)",
    "armour.trinket": "trinkets", "item.material": "crafting materials", "item.trophy": "trophies",
    "item.food": "food (raw and cooked)", "item.mead": "meads and mead bases", "item.feast": "feasts",
    "item.gem": "gems", "item.valuable": "coins and valuables", "item.key": "keys", "item.bait": "fishing bait",
    "item.fish": "fish", "item.misc": "other items"}


def build(records):
    """{category: {label, samples, variants, stats, references, effects, outline}} over every classified item."""
    grouped = collections.defaultdict(list)
    for record in records:
        key = items_classify.category(record)
        if key:
            grouped[key].append(record)
    return {key: _category(key, grouped[key]) for key in sorted(grouped)}


def _category(key, records):
    originals, variants = _split_variants(records)
    rows = [items_stats.sample(r, key) for r in originals]
    references = _references(key, rows)
    found = {"label": LABELS.get(key, key), "samples": rows,
             "variants": sorted(r["name"] for r in variants),
             "stats": items_stats.summary(rows),
             "references": references, "paint": _paint(rows, references),
             "effects": items_stats.effects(originals),
             "trail": items_stats.tally(originals, lambda r: [r["trail"]["material"]] if r["trail"] else []),
             "glow": items_stats.tally(originals, lambda r: r["glow"]["materials"][:1] if r["glow"] else []),
             "ground_fx": items_stats.tally(originals, lambda r: [r["ground_fx"]] if r["ground_fx"] else [])}
    outline = items_stats.outline(originals)
    if outline:
        found["outline"] = outline
    return {k: v for k, v in found.items() if v not in ({}, [], None)}


def _split_variants(records):
    """Variants are kept by name and left out of the stats, so one design counts once: enchanted and elemental
    versions of an item in the same category (SwordNiedhoggBlood of SwordNiedhogg), and items drawing the same meshes
    in the same materials as another (creature attack copies)."""
    names = {r["name"] for r in records}
    seen, originals, variants = set(), [], []
    for record in sorted(records, key=lambda r: (len(r["name"]), r["name"])):
        model = items_stats.model_of(record)
        look = (tuple(model["meshes"]), tuple(m["path"] for m in model["materials"]))
        base = items_classify.base_name(record["name"])
        if look in seen or (base != record["name"] and base in names):
            variants.append(record)
        else:
            seen.add(look)
            originals.append(record)
    return sorted(originals, key=lambda r: r["name"]), variants


def _references(key, rows):
    """The hand-picked references when the category has them, else one sample per tier (lowest to highest)."""
    chosen = [r["prefab"] for r in rows if r["name"] in REFERENCES.get(key, ())]
    if chosen:
        return chosen
    by_tier = {}
    for row in sorted(rows, key=lambda r: (r["tier"] is None, r["tier"] or 0, r["name"])):
        by_tier.setdefault(row["tier"], row["prefab"])
    return list(by_tier.values())[:6]


def _tiers(categories):
    """How budgets grow from tier to tier, over the player's weapons, shields and pickaxes (one design counts once)."""
    rows = [r for key, c in categories.items() if key.startswith(("weapon.", "shield.", "tool.pickaxe"))
            and key != "weapon.creature" for r in c["samples"]]
    return {"names": items_classify.TIERS, "held_gear": items_stats.by_tier(rows)}


def _paint(rows, references):
    """How the references' main textures are painted (items_textures.texture_stats), by item name."""
    found = {r["name"]: items_textures.texture_stats(r["prefab"]) for r in rows if r["prefab"] in references}
    return {k: v for k, v in found.items() if v}


REFERENCES = {
    "weapon.sword": ("SwordBronze", "SwordIron", "SwordSilver", "SwordBlackmetal", "SwordMistwalker", "SwordNiedhogg"),
    "weapon.axe_1h": ("AxeFlint", "AxeBronze", "AxeIron", "AxeBlackMetal", "AxeJotunBane"),
    "weapon.axe_2h": ("Battleaxe", "BattleaxeCrystal", "BattleaxeBlackmetal", "BattleaxeSkullSplittur"),
    "weapon.axe_dual": ("AxeBerzerkr", "AxeEarly"),
    "weapon.mace": ("Club", "MaceBronze", "MaceIron", "MaceSilver", "MaceNeedle", "MaceEldner"),
    "weapon.sledge": ("SledgeStagbreaker", "SledgeIron", "SledgeDemolisher"),
    "weapon.knife": ("KnifeFlint", "KnifeCopper", "KnifeChitin", "KnifeSilver", "KnifeBlackMetal"),
    "weapon.spear": ("SpearFlint", "SpearBronze", "SpearElderbark", "SpearWolfFang", "SpearCarapace", "SpearSplitner"),
    "weapon.atgeir": ("AtgeirBronze", "AtgeirIron", "AtgeirBlackmetal", "AtgeirHimminAfl"),
    "weapon.greatsword": ("THSwordKrom", "THSwordSlayer", "THSwordGold"),
    "weapon.bow": ("Bow", "BowFineWood", "BowHuntsman", "BowDraugrFang", "BowSpineSnap", "BowAshlands"),
    "weapon.crossbow": ("CrossbowArbalest", "CrossbowRipper"),
    "weapon.staff": ("StaffFireball", "StaffIceShards", "StaffShield", "StaffRedTroll", "StaffGreenRoots"),
    "weapon.fist": ("FistFenrirClaw", "FistBjornUndeadClaw", "FistBjornClaw"),
    "weapon.bomb": ("BombOoze", "BombBile", "BombSmoke", "BombBlob_Poison"),
    "weapon.creature": ("skeleton_sword", "skeleton_mace", "draugr_axe", "draugr_sword", "GoblinSword", "GoblinClub"),
    "ammo.arrow": ("ArrowFlint", "ArrowIron", "ArrowNeedle", "ArrowCarapace", "ArrowCharred"),
    "ammo.bolt": ("BoltIron", "BoltBlackmetal", "BoltBone", "BoltCarapace"),
    "shield.round": ("ShieldWood", "ShieldBanded", "ShieldSilver", "ShieldBlackmetal", "ShieldCarapace", "ShieldFlametal"),
    "shield.tower": ("ShieldWoodTower", "ShieldBoneTower", "ShieldIronTower", "ShieldBlackmetalTower"),
    "shield.buckler": ("ShieldBronzeBuckler", "ShieldIronBuckler", "ShieldCarapaceBuckler"),
    "tool.pickaxe": ("PickaxeAntler", "PickaxeStone", "PickaxeBronze", "PickaxeIron", "PickaxeBlackMetal"),
    "tool.farming": ("Hoe", "Cultivator", "Scythe", "Shovel"),
    "tool.torch": ("Torch", "Lantern", "Lantern_DN"),
    "tool.tankard": ("Tankard", "TankardOdin", "TankardAnniversary"),
    "armour.helmet": ("HelmetLeather", "HelmetBronze", "HelmetIron", "HelmetDrake", "HelmetPadded", "HelmetCarapace"),
    "armour.chest": ("ArmorLeatherChest", "ArmorBronzeChest", "ArmorIronChest", "ArmorWolfChest",
                     "ArmorPaddedCuirass", "ArmorCarapaceChest"),
    "armour.legs": ("ArmorLeatherLegs", "ArmorBronzeLegs", "ArmorIronLegs", "ArmorWolfLegs", "ArmorPaddedGreaves",
                    "ArmorCarapaceLegs"),
    "armour.cape": ("CapeDeerHide", "CapeTrollHide", "CapeWolf", "CapeLox", "CapeFeather", "CapeAsh"),
    "armour.trinket": ("TrinketBronzeStamina", "TrinketIronHealth", "TrinketSilverDamage", "TrinketFlametalEitr"),
    "armour.utility": ("Wishbone", "IceShoes", "Demister"),
    "item.trophy": ("TrophyBoar", "TrophyDeer", "TrophyGreydwarf", "TrophySkeleton", "TrophyDraugr", "TrophyWolf"),
    "item.food": ("CookedMeat", "Bread", "Sausages", "LoxPie", "FishCooked", "Raspberry"),
    "item.mead": ("MeadHealthMinor", "MeadStaminaMinor", "MeadPoisonResist", "MeadBaseHealthMinor", "MeadTasty"),
    "item.material": ("Wood", "Stone", "CopperOre", "Bronze", "DeerHide", "TrollHide"),
    "item.fish": ("Fish1", "Fish2", "Fish5", "Fish9"),
    "item.misc": ("SaddleLox", "DragonEgg", "BarberKit", "AncientSeed"),
    "item.gem": ("Ruby", "Amber", "GemstoneRed", "GemstoneBlue", "AmberPearl"),
    "item.valuable": ("Coins", "SilverNecklace", "AncientCoin"),
    "item.key": ("CryptKey", "DvergrKey", "BloodGoldKey"),
}


def main():
    records = items_scan.scan_all() if "--rescan" in sys.argv else items_scan.load()
    categories = build(records)
    data = {"topic": "items", "generated": GENERATED, "script": "measure/items.py", "categories": categories,
            "tiers": _tiers(categories), "armour": items_armour.section(records), "icons": items_icons.section(records)}
    os.makedirs(os.path.dirname(DATA), exist_ok=True)
    with open(DATA, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1, sort_keys=False)
        handle.write("\n")
    counts = {k: len(v["samples"]) for k, v in data["categories"].items()}
    print(f"{sum(counts.values())} items in {len(counts)} categories -> {os.path.relpath(DATA)}")


if __name__ == "__main__":
    main()

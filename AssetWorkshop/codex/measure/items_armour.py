"""The armour section of data/items.json: the player body's texture layout, what every chest and leg armour paints
into it, the meshes armour adds on top (skinned to the body's bones or hung on a named bone), how helmets treat hair
and beards, and how capes move (MagicaCloth settings).

    items_armour.section(records)      # called by items.py
    python codex/measure/items_armour.py   # also draws the uv overlays into codex/out/items/armour/
"""
import collections
import os

import game
import items_body
import items_classify
import items_stats

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "out", "items", "armour")
BODY_SLOTS = {"Chest": ("_ChestTex", "_ChestBumpMap", "_ChestMetal"), "Legs": ("_LegsTex", "_LegsBumpMap", "_LegsMetal")}
OVERLAYS = ("ArmorLeatherChest", "ArmorBronzeChest", "ArmorIronChest", "ArmorWolfChest", "ArmorPaddedCuirass",
            "ArmorCarapaceChest", "ArmorFlametalChest", "ArmorBronzeLegs", "ArmorIronLegs", "ArmorWolfLegs",
            "ArmorPaddedGreaves", "ArmorMageLegs")


def section(records):
    armour = [r for r in records if (items_classify.category(r) or "").startswith("armour.")]
    return {"body_layout": body_layout(), "painted": [p for p in (_painted(r) for r in armour) if p],
            "coverage": _coverage_summary(armour), "worn_meshes": _worn(armour), "helmets": _helmets(armour, records),
            "capes": [dict(c, item=r["name"]) for r in armour if items_classify.category(r) == "armour.cape"
                      for c in r["cloth"]]}


def body_layout():
    """Where the body's parts sit on a 256 px armour texture, and the facts that constrain painting it."""
    return {"texture_px": items_body.TEXTURE, "regions": items_body.regions(),
            "used_quadrant": "top-left 128 x 128 px (u 0 to 0.5, v 0.5 to 1); the other three quarters are never "
                             "sampled", "mirrored": True, "body_mesh": items_body.BODY, "body_shader": "Player"}


def _painted(record):
    """One chest or legs item: its body textures (sizes) and which body regions they paint opaque."""
    kind = record["shared"]["item_type"]
    path = record["shared"]["armor_material"]
    if kind not in BODY_SLOTS or not path:
        return None
    textures = game.material(path)["textures"]
    colour, bump, metal = (textures.get(slot) for slot in BODY_SLOTS[kind])
    return {"item": record["name"], "kind": kind, "material": path, "tier": items_classify.tier(record),
            "texture": colour, "texture_px": list(game.texture_size(colour) or (0, 0)) if colour else None,
            "normal": bool(bump), "metal": bool(metal),
            "coverage": items_body.coverage(colour) if colour else {}}


def _coverage_summary(armour):
    """For chest and for legs: the median share of each region painted, over the game's items."""
    found = {}
    for kind in BODY_SLOTS:
        rows = [p["coverage"] for p in (_painted(r) for r in armour) if p and p["kind"] == kind and p["coverage"]]
        if rows:
            found[kind] = {region: items_stats.five([row.get(region, 0) for row in rows])["median"]
                           for region in items_body.REGIONS}
            found[kind + "_n"] = len(rows)
    return found


def _worn(armour):
    """Per armour category: how many items add a skinned mesh (attach_skin), which bones others hang parts on
    (attach_<bone>), and the triangle budget of the added meshes."""
    found = {}
    for key in ("armour.helmet", "armour.chest", "armour.legs", "armour.cape", "armour.utility", "armour.trinket"):
        rows = [r for r in armour if items_classify.category(r) == key]
        worn = [r["worn"] for r in rows if r["worn"]]
        found[key] = {"items": len(rows), "attach_skin": sum("attach_skin" in r["frames"] for r in rows),
                      "attach": sum("attach" in r["frames"] for r in rows),
                      "bones": dict(collections.Counter(b for r in rows for b in r["bones"]).most_common()),
                      "worn_triangles": items_stats.five([w["triangles"] for w in worn]),
                      "worn_texture_px": items_stats.five([w["texture_px"] for w in worn if w["texture_px"]])}
    return found


def _helmets(armour, records):
    """How helmets treat hair and beards: each helmet names a HelmetHairType for hair and for beard (Default keeps
    it, Hidden removes it, HiddenHat/Hood/Neck/Scarf swap in the variant the worn hair or beard item lists for that
    type in its own m_helmetHairSettings)."""
    rows = [r for r in armour if items_classify.category(r) == "armour.helmet"]
    styles = [r for r in records if r["shared"]["item_type"] == "Customization"]
    return {"n": len(rows), "hair": dict(collections.Counter(r["shared"]["hide_hair"] for r in rows)),
            "beard": dict(collections.Counter(r["shared"]["hide_beard"] for r in rows)),
            "hair_items_with_variants": dict(collections.Counter(
                s for r in styles for s in r["shared"]["hair_settings"] + r["shared"]["beard_settings"])),
            "items": {r["name"]: [r["shared"]["hide_hair"], r["shared"]["hide_beard"]] for r in rows}}


def draw_overlays(records):
    """The body uv layout over the skin and over the game's armour textures (codex/out/items/armour/)."""
    os.makedirs(OUT, exist_ok=True)
    items_body.overlay("Characters/Player/model/old_PlayerCharacter2/PlayerCharacter_01.png",
                       os.path.join(OUT, "layout_skin.png"))
    by_name = {r["name"]: r for r in records}
    for name in OVERLAYS:
        painted = _painted(by_name[name]) if name in by_name else None
        if painted and painted["texture"]:
            items_body.overlay(painted["texture"], os.path.join(OUT, f"layout_{name}.png"))


if __name__ == "__main__":
    import items_scan
    draw_overlays(items_scan.load())

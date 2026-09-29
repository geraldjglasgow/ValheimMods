"""The role a sound plays, from how the game uses it: the component and field that reference its prefab (a creature's
m_idleSound, an item's attack m_triggerEffect, a piece's m_placeEffect...), the owner (a creature, a player item, a
creature's attack item), and for impacts the material its name says. Loops are classed by where they live, and sounds
nothing references by their name and folder.

Every role is an archetype key of data/sfx.json ('sfx.creature.idle', 'sfx.weapon.sword.swing',
'sfx.world.hit.stone'...). One sound may play several roles (the game plays sfx_metal_blocked for a metal block and for
a metal piece breaking); it then counts in each.
"""
import re

SKILLS = {1: "sword", 2: "knife", 3: "club", 4: "polearm", 5: "spear", 6: "shield", 7: "axe", 8: "bow",
          9: "staff", 10: "staff", 11: "unarmed", 12: "pickaxe", 13: "axe", 14: "crossbow", 104: "fishing"}
TWO_HANDED = {"axe": "battleaxe", "club": "sledge", "sword": "greatsword"}
ACTIONS = {"m_startEffect": "swing", "m_triggerEffect": "swing", "m_trailStartEffect": "swing",
           "m_burstEffect": "swing", "m_holdStartEffect": "draw", "m_hitEffect": "hit", "m_hitTerrainEffect": "hit",
           "m_blockEffect": "block", "m_equipEffect": "equip", "m_unequipEffect": "equip"}
CREATURE_FIELDS = {"m_hitEffects": "hurt", "m_deathEffects": "death", "m_alertedEffects": "alert",
                   "m_idleSound": "idle", "m_critHitEffects": "crit", "m_backstabHitEffects": "crit",
                   "m_jumpEffects": "jump", "m_consumeItemEffects": "eat", "m_wakeupEffects": "alert"}
PLAYER_FIELDS = {"m_hitEffects": "player.hurt", "m_deathEffects": "player.death", "m_jumpEffects": "player.jump",
                 "m_consumeItemEffects": "item.eat", "m_pickupEffects": "item.pickup", "m_equipEffects": "item.equip",
                 "m_dropEffects": "item.drop", "m_perfectBlockEffect": "combat.perfect_block",
                 "m_buttonEffects": "ui", "m_critHitEffects": "combat.crit", "m_backstabHitEffects": "combat.crit"}
COMPONENTS = {
    "Piece": "build.place", "WearNTear": "piece", "Destructible": "world", "MineRock": "world", "MineRock5": "world",
    "TreeBase": "world", "TreeLog": "world", "ImpactEffect": "world", "Pickable": "world.pick",
    "PickableItem": "world.pick", "Container": "piece.container", "Door": "piece.door", "Fireplace": "piece.fire",
    "CraftingStation": "craft.station", "CookingStation": "piece.station", "Smelter": "piece.station",
    "Fermenter": "piece.station", "Beehive": "piece.station", "Feast": "item.eat", "Trap": "piece.trap",
    "Turret": "piece.trap", "PrivateArea": "piece.ward", "ItemStand": "piece.stand", "ArmorStand": "piece.stand",
    "OfferingBowl": "boss.summon", "BossStone": "boss.summon", "Projectile": "projectile.hit", "Aoe": "projectile.aoe",
    "SpawnAbility": "projectile.aoe", "Tameable": "creature.tame", "Procreation": "creature.breed",
    "EggHatch": "creature.breed", "SpawnArea": "creature.spawn", "TriggerSpawner": "creature.spawn",
    "Ragdoll": "creature.corpse", "RandomFlyingBird": "creature.idle", "RandomSpeak": "world.voice",
    "Trader": "npc.voice", "WaterTrigger": "water.splash", "Floating": "water.splash", "LiquidVolume": "water.splash",
    "Ship": "ship", "SnowDestruction": "world.destroy.snow", "InventoryGui": "ui", "TabHandler": "ui",
    "StoreGui": "ui", "Minimap": "ui", "MessageHud": "ui.stinger", "Script:assembly_guiutils.dll": "ui"}
IGNORED = {"ZNetScene", "ObjectDB", "PieceTable", "Recipe", "ItemSets", "Achievements", "DungeonGenerator", "Game",
           "CreatureSpawner", "Pickable.m_itemPrefab", "StationExtension"}
MATERIALS = (("crystal", r"crystal|glass|gem"), ("ice", r"ice|icicle|frost|rime"),
             ("metal", r"metal|iron|bars|flametal|coin|sledge_iron|trap|forge|anvil"),
             ("stone", r"rock|stone|marble|ashstone|crumble|boulder|tombstone|golem"),
             ("wood", r"wood|tree|log|branch|barley|hottub|door|window|chest|bow|arrow"),
             ("bone", r"bone|draugr|skeleton|carrion"), ("snow", r"snow"),
             ("mud", r"mud|guck|tar|slime|jelly|egg|ooze|blob"), ("foliage", r"bush|vines|leaves|grass|pickable"),
             ("clay", r"clay|pot|bowl"), ("flesh", r"flesh|meat|hit$|punch|unarmed|claw"))
SURFACES = ((16384, "ice"), (2048, "lava"), (1024, "ash"), (512, "tar"), (256, "metal"), (8192, "snow_deep"),
            (4096, "snow_deep"), (64, "grass"), (32, "mud"), (16, "snow"), (8, "wood"), (4, "stone"), (2, "water"),
            (128, "ground"), (1, "default"))


def material(name):
    """The material a sound's name speaks of ('sfx_rock_hit' -> 'stone'), 'misc' when none."""
    low = name.lower()
    return next((m for m, pattern in MATERIALS if re.search(pattern, low)), "misc")


def leaf(field):
    """The effect list's own field name: 'm_itemData/m_shared/m_attack/m_hitEffect/m_effectPrefabs/m_prefab' ->
    'm_hitEffect'."""
    parts = [p for p in field.split("/") if p not in ("m_effectPrefabs", "m_prefab")]
    return parts[-1] if parts else ""


def is_creature(use):
    owner = use["owner"]
    return use["file"].startswith("Characters/") and bool({"Humanoid", "Character"} & set(owner["kinds"])) \
        and "Player" not in owner["kinds"]


def is_attack_item(use):
    """A creature's attack item (Greydwarf_attack, troll_groundslam): an ItemDrop under Characters/. ObjectDB lists
    these too, so being in ObjectDB does not make an item the player's."""
    return use["component"] == "ItemDrop" and use["file"].startswith("Characters/")


def weapon_kind(owner):
    """'sword', 'battleaxe', 'staff'... of a player item, from its skill and hands."""
    kind = SKILLS.get(owner.get("skill"), "tool" if owner.get("item_type") == 19 else "item")
    if owner.get("item_type") in (14, 22):
        kind = TWO_HANDED.get(kind, kind)
    if owner.get("item_type") == 5:
        kind = "shield"
    return kind


def item_role(use, record):
    """The role of a use by an ItemDrop: a creature's attack, or a player weapon's swing, hit, block, draw... A
    creature attack that borrows a shared sound (a skeleton swinging sfx_sword_swing) gives that sound no role: it
    keeps its weapon archetype, and the creature is still credited with it."""
    action, owner = ACTIONS.get(leaf(use["field"])), use["owner"]
    sound = f"{record['name']} {record['node']}"
    if not action:
        return None
    if is_attack_item(use):
        if not record["prefab"].startswith("Characters/") or action in ("block", "equip"):
            return None
        return "creature.attack_hit" if action == "hit" else "creature.attack"
    if owner.get("item_type") == 2 or (action == "equip" and re.search(r"eat|drink|burp|mead", sound, re.I)):
        return "item.drink" if re.search(r"drink|mead|burp", sound, re.I) else "item.eat"
    if action == "equip":
        return "item.equip"
    if action == "block":
        return f"combat.block.{material(sound)}"
    return f"weapon.{weapon_kind(owner)}.{action}"


def creature_role(use):
    """What a use means for the creature making the sound ('idle', 'attack', 'footstep'...), whether or not the sound
    is the creature's own; None when the use is not a creature's."""
    if not use["file"].startswith("Characters/") or use["file"].startswith("Characters/Player"):
        return None
    component, name = use["component"], leaf(use["field"])
    if is_attack_item(use):
        action = ACTIONS.get(name)
        return None if action in (None, "block", "equip") else "attack_hit" if action == "hit" else "attack"
    if component == "AnimationClip":
        return anim_role(use).split(".", 1)[1]
    fixed = {"FootStep": "footstep", "Tameable": "tame", "Procreation": "breed", "EggHatch": "breed"}
    if component in fixed:
        return fixed[component]
    if component in ("Humanoid", "Character", "MonsterAI", "AnimalAI", "BaseAI"):
        return CREATURE_FIELDS.get(name, name[2:])
    return None


def use_role(use, record):
    """The archetype key (without 'sfx.') of one use of a sound prefab, or None when the use says nothing."""
    component, name = use["component"], leaf(use["field"])
    sound = f"{record['name']} {record['node']}"
    if component in IGNORED or f"{component}.{name}" in IGNORED:
        return None
    if component == "ItemDrop":
        return item_role(use, record)
    if component == "FootStep":
        return footstep_role(use)
    if component == "AnimationClip":
        return anim_role(use)
    if component == "Player":
        return PLAYER_FIELDS.get(name, f"player.{name[2:]}")
    if component in ("Humanoid", "Character", "MonsterAI", "AnimalAI", "BaseAI") and is_creature(use):
        role = CREATURE_FIELDS.get(name)
        return f"combat.{role}" if role == "crit" else f"creature.{role or name[2:]}"
    if component.startswith("SE_") or component == "StatusEffect":
        return "status.start" if "start" in name.lower() else "status.other"
    return component_role(component, name, sound)


def component_role(component, name, sound):
    """Roles of world objects, pieces and interfaces."""
    base = COMPONENTS.get(component)
    if base in ("piece", "world"):
        if "destroy" in name.lower():
            return f"{base}.destroy.{material(sound)}"
        return f"{base}.hit.{material(sound)}" if "hit" in name.lower() else f"{base}.other"
    if base == "build.place":
        return f"build.place.{material(sound)}" if name == "m_placeEffect" else None
    return base


ANIM_ROLES = (("death", r"death|die|dead"), ("hurt", r"stagger|hurt|hit_react|flinch"),
              ("attack", r"attack|bite|claw|slam|swipe|punch|stomp|spit|throw|shoot|cast|breath|smash|charge|"
                         r"spin|combo|slash|pierce|rush|flurry|beam|nova|leap|pounce|scream"),
              ("spawn", r"spawn|intro|rise|arise|wake|emerge|summon"), ("alert", r"taunt|roar|alert|howl"),
              ("move", r"walk|run|turn|step|move|fly|swim|jump|land|burrow|dodge|roll|crawl|sneak|takeoff"),
              ("idle", r"idle|sleep|eat|sit|rest"))


def anim_role(use):
    """An animation event's sound, by the clip's name: 'Burrow.anim' -> creature.move, 'attack_bite' -> attack."""
    clip = use["owner"]["name"].lower()
    kind = next((role for role, pattern in ANIM_ROLES if re.search(pattern, clip)), "anim")
    prefix = "player" if use["file"].startswith("Characters/Player") else "creature"
    return f"{prefix}.{kind}"


def footstep_role(use):
    """'footstep.<surface>.<motion>' for the player, 'creature.footstep' for everything else."""
    if "Player" not in use["owner"]["kinds"]:
        return "creature.footstep"
    flags, motion = use["context"].get("material", 1), use["context"].get("motionType", 1)
    surface = next((label for bit, label in SURFACES if flags & bit), "default")
    pace = "land" if motion & 32 else "run" if motion & 2 else "sneak" if motion & 4 else \
        "swim" if motion & 16 else "climb" if motion & 8 else "walk"
    return f"footstep.{surface}.{pace}"


LOOP_KINDS = (("music.location", r"^Audio/Music|music"), ("loop.fire", r"fire|torch|brazier|pyre|bonfire|hearth|"
              r"lantern|burn|candle|flame|kiln|oven|smelter|furnace|cookingstation|slot\d"),
              ("loop.ship", r"^GameElements/Ships|sail|creak|deck|fizzle"),
              ("loop.creature", r"^Characters/"), ("loop.magic", r"portal|shield|guard|mage|eitr|potential|frost|"
              r"glowworm|mistile|wisp|demister|lightning|jelly|spawner|prespawn|gate|ice|beam|orb|core|waystone"),
              ("loop.machine", r"windmill|grind|propeller|spinning|mill|forge|machinery|cart|ram|sled|catapult|"
              r"fermenter|bathtub|cauldron|sap|beehive|bird|refinery|foundry"))


def loop_role(record):
    """The archetype of a looping sound, from its prefab's folder and the names on the way to it."""
    where = f"{record['prefab']} {record['node']}".lower()
    if "MusicLocation" in record.get("components", []):
        return "music.location"
    for key, pattern in LOOP_KINDS:
        if re.search(pattern, record["prefab"]) or re.search(pattern, where):
            return key
    return "loop.ambient"


NAME_ROLES = (("creature.idle", r"idle|verse_idle|breath|chew|random"),
              ("creature.alert", r"alert|taunt|scream|roar|shout|howl|callout"),
              ("creature.death", r"death|die"), ("creature.hurt", r"hurt|_hit$|pain"),
              ("creature.attack_hit", r"impact|attack_hit|_hit_"),
              ("creature.attack", r"attack|bite|claw|slam|swing|whoosh|wosh|slash|stab|punch|stomp|spit|throw|melee"),
              ("creature.footstep", r"footstep|step|crawl|land|flap|wing|move|roll|jump"))


def name_role(record):
    """The archetype of a sound nothing references (or only other sound prefabs do), from its name and folder."""
    name = f"{record['name']} {record['node']}".lower()
    if record["prefab"].startswith("Characters/"):
        return next((key for key, pattern in NAME_ROLES if re.search(pattern, name)), "creature.other")
    if record.get("source") and record["source"]["mixer"].endswith("GUI"):
        return "ui"
    if re.search(r"explo|shockwave|blast|thunder", name):
        return "projectile.aoe"
    return "other.unused"

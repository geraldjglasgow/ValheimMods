"""Sorts the game's effects into the codex's categories (vfx.<kind>) and each particle system into a role (flame,
smoke, spark, ...), by name, texture, shader and numbers. vfx_survey measures each category and role.

    import vfx_classify
    vfx_classify.category(fx)          # 'vfx.hit_spark'
    vfx_classify.role(system)          # 'spark'
"""
import re

import vfx_read

# (key, label, name pattern) in order: the first match wins. Names are lower case.
CATEGORIES = [
    ("vfx.camshake", "camera shakes", r"camshake"),
    ("vfx.fireworks", "fireworks", r"firework|sparkler"),
    ("vfx.water", "water and lava splashes, wakes and surface rings", r"water|splash_|lavasplash|_hitwater|drown|bathtub|"
                                                                      r"leviathan|float_"),
    ("vfx.footstep", "footsteps, landings and slides", r"footstep|^fx_land|_land$|^fx_slide|snowwalk|^fx_introland|raven_land"),
    ("vfx.place", "building placement puffs", r"^vfx_place_"),
    ("vfx.fire_fuel", "fire flare-ups when fuel is added", r"addfuel|add_wood|addwood|add_fuel|candle_on|candle_off"),
    ("vfx.fire", "fires, torches, braziers and burning", r"torch_|flames|brazier|burning|burn$|cinderfire|bonfire|fire_pit|"
                                                        r"hearth|walltorch|groundtorch|candle|chestfire|fx_hotground"),
    ("vfx.status", "status effects on a character", r"^vfx_(poison|frost|cold|freezing|wet|smoked|slimed|tared|lightfoot|"
                                                    r"bugrepellent|harpooned|undeadburn|staffshield|goblinshield|frostorbs)$|"
                                                    r"^vfx_(mead|potion)|^fx_(potion|immobilize|adrenaline|gp_|puke)|"
                                                    r"^fx_lightning(_red)?$"),
    ("vfx.eat", "eating and drinking", r"^fx_eat|feast|foodsteam|meadsplash"),
    ("vfx.love", "petting, taming and love hearts", r"_pet$|love|soothed|tamed|lay_egg|_birth$"),
    ("vfx.levelup", "level-ups, upgrades and item sparkles", r"levelup|upgrade|itemsparkles|invupgrade|bonusyield|crit$|backstab|"
                                                            r"perfectdodge|wishbone"),
    ("vfx.pickup", "picking and pickups", r"pick$|pick_|pickup|_pick\b|lootspawn|offering"),
    ("vfx.spawn", "spawns, summons, despawns and teleports", r"spawn|summon|arise|wakeup|burrow|despawn|teleport|portal|"
                                                            r"changedcharacter|odin|raven_despawn|aspectspawn|tendrilspawn"),
    ("vfx.station", "crafting stations and machines", r"add(ore|item|food|tissue|flax|ammo)|_add$|additem|produce|forge_hammer|"
                                                     r"fermenter|mill_|kiln|cartograp|bowl_|cooking_station|foundry|refinery|"
                                                     r"_sw_|oven_|turret|catapult|lever|blastfurnace|artisanpress|hottub|"
                                                     r"archerytarget_bullseye|trap_|bossstone|guardstone|shieldgenerator"),
    ("vfx.explosion", "explosions and blasts", r"expl|blast|fireballhit|meteor_hit|meteorsmash|nova|bomb|clusterbomb"),
    ("vfx.shot", "bow, crossbow and siege shots", r"(bow|crossbow|arbalest|batteringram|turret)_[a-z]*_?fire$|^vfx_bow_fire"),
    ("vfx.aura", "auras and glows on creatures and things", r"glow|aura|pheromone|protect|_shield$|charred_eyeglow"),
    ("vfx.hit_plant", "plants rustling and breaking", r"leaf_puff|_puff$|bush[0-9]?_[a-z]*hit|shrub[a-z_0-9]*hit|vines_hit|creep"),
    ("vfx.ground_slam", "ground slams, stomps and shockwaves", r"groundslam|stomp|shockwave|punch_aoe|spikesmash|groundpunch|"
                                                               r"_aoe|dragon_land|pierceground|impact"),
    ("vfx.hit_creature", "creature hits and blood", r"blood|_hurt$|^(vfx|fx)_(boar|deer|wolf|troll|foresttroll|frosttroll|"
                                                    r"trollsnow|greydwarf|goblin|goblinbrute|draugr|skeleton|blob|bonemass|neck|"
                                                    r"leech|wraith|ghost|serpent|dragon|hatchling|lox|seal|stonegolem|fenring|"
                                                    r"ulv|bat|bjorn|unbjorn|moose|morgen|seeker|charred|dverger|fallenwarrior|"
                                                    r"shadowperson|tick|deathsquito|player|gjall|goblinking|himminafl|"
                                                    r"deathsquito|jotun|abomination|asksvin|foresttroll)[a-z_]*_?hit$"),
    ("vfx.hit_material", "impacts on things and projectile hits: sparks, chips, mud", r"hitsparks|gucksackhit|mudhit|rockhit|marblehit|obsidian|clubhit|sledge|mace_hit|"
                                                                 r"arrowhit|projectilehit|torch_hit|coalhit|ice_hit|iceshard_hit|"
                                                                 r"barnacle_hit|blocked|perfectblock|archerytarget_hit|dummy_hit|"
                                                                 r"shieldhit|staffshield_hit|weapon_hit|jotunbane_hit|_hit$"),
    ("vfx.tree", "tree chopping, falling logs and sawdust", r"treecut|_cut$|_cut_|stubbe|regrow|sawdust|tree_fall|hitground|"
                                                            r"logdestroyed|fir_oldlog|yggashoot|beech_small|small1_destroy"),
    ("vfx.death", "death poofs", r"death|_die$|ragdoll"),
    ("vfx.destruction", "destruction debris of pieces, props and rocks", r"destroy|destruction|gibs|broken|damaged|break"),
    ("vfx.magic", "magic and boss attacks: staffs, charges, beams, breath, lightning", r"staff|mage|lightning|beam|charge|mistile|shield|"
                                                                        r"launch|spit|breath|roar|magic|chain|whirl|spin|"
                                                                        r"fissure|swipe|swing|clawattack|attack|bite|glint|"
                                                                        r"trail|frost|aoe_start|protect|prespawn|pray|"
                                                                        r"connection|hand|bolt|dodge|flying"),
]
GROUP_CATEGORIES = {"weather": ("vfx.weather", "weather: rain, snow, ash, wind, fog around the player"),
                    "projectile": ("vfx.projectile", "projectiles in flight"),
                    "item": ("vfx.item_glow", "weapons and tools: glows, flames and embers on the item"),
                    "piece": ("vfx.fire", None)}
AMBIENT = ("vfx.ambient", "ambient: fireflies, flies, mist, bubbling pools, heat haze")
AMBIENT_NAMES = r"fireflies|^flies$|mist(?!ile)|fog|clouds|smoke_area|mistvolume|bubbl|heatdistortion|frostcloud|"\
                r"ceilingdrops|tar_surface|darkland"
WEATHER_PATHS = ("Effects/weather/", "Effects/thunder/")
LABELS = {key: label for key, label, _ in CATEGORIES}
LABELS.update({key: label for key, label in GROUP_CATEGORIES.values() if label})
LABELS[AMBIENT[0]] = AMBIENT[1]
LABELS["vfx.misc"] = "everything else"


def category(fx):
    """The codex category key of an effect."""
    name = fx["name"].lower()
    if fx.get("group") in GROUP_CATEGORIES:
        return GROUP_CATEGORIES[fx["group"]][0]
    if fx["prefab"].startswith(WEATHER_PATHS):
        return GROUP_CATEGORIES["weather"][0]
    if re.search(AMBIENT_NAMES, name) and not re.search(r"hit|death|destroy|break|attack|eat", name):
        return AMBIENT[0]
    for key, _, pattern in CATEGORIES:
        if re.search(pattern, name):
            return key
    return "vfx.misc"


# ---------------------------------------------------------------- roles of a single system

ROLES = {
    "flame": "flames: a flipbook or soft blob, additive or gradient-mapped, rising slowly",
    "smoke": "smoke and dust clouds: big soft lit puffs, alpha-blended",
    "spark": "sparks: tiny bright additive points or streaks, fast, often falling",
    "sparkle": "twinkles: star-shaped glints that pop and fade in place (dropped items, pickups)",
    "ember": "embers and cinders: small additive points drifting up for seconds",
    "glow": "glows and flares: one or a few big soft additive or alpha sprites",
    "flash": "flashes: a big bright sprite for a fraction of a second at the start",
    "debris": "mesh debris: chunks, splinters, drops rendered as small meshes",
    "pixel": "pixel bits: tiny flat-colour squares (8 px or untextured)",
    "blood": "blood: drops, splats and mist in dark red",
    "decal": "decals: splats projected on the ground",
    "ring": "rings and shockwaves: flat or billboard rings growing outwards",
    "water": "water: drops, foam, splashes, ripples",
    "slime": "slime, mud, sap, tar and lava: thick splashes and blobs",
    "snow": "snow: flakes, clumps and powder",
    "carrier": "no material: a parent that only carries sub-emitters, lights or sound",
    "leaves": "leaves, needles, feathers, hair: small textured flakes tumbling down",
    "distortion": "heat haze and refraction",
    "light": "light carriers: systems whose particles drive lights",
    "trail": "trails: ribbons behind particles or moving objects",
    "other": "anything else",
}
_T = {  # texture or material name patterns per role, checked in order
    "blood": r"blood|brains|entrails|bloodbag",
    "slime": r"slime|mud|sap_|(^|[ /_])tar|lava|goo|ooze|(^|[ /_])mead|puke|colorless",
    "snow": r"snow",
    "decal": r"decal|splat_decal",
    "distortion": r"distortion|heathaze",
    "water": r"water|drop_pixel|drops_pixel|foam|(^| )rain|splash01|bubble",
    "ring": r"shockwave|ring|wave|nova",
    "leaves": r"leaf|needle|feather|hair|birch|beech|oak|pine_tree|shrub|en_particle|acacia",
    "flame": r"flame|fire|wildfire06|candle",
    "smoke": r"smoke|dust|dirt|fog|mist|cloud|wispy|steam|puff",
    "sparkle": r"starspark|item_particle",
    "spark": r"spark|star|gnista|crystal|cinder|firework|sparc|lightning|bam",
    "glow": r"glow|point|light_|dot|nutty|orb",
}


def role(s):
    """The role of one particle system (a key of ROLES)."""
    r = s.get("renderer") or {}
    m = r.get("material") or {}
    texture = (m.get("texture") or "").rsplit("/", 1)[-1]
    names = f"{m.get('name', '')} {texture}".lower().replace("wildfire", "puff")
    return _by_kind(s, r, m, names) or _by_name(s, m, names) or _by_numbers(s, names, m)


def _by_kind(s, r, m, names):
    """Roles the renderer decides: lights, bare carriers, decals, meshes, trails; None for the rest."""
    if s.get("light_module"):
        return "light"
    if not m:
        return "carrier" if not s.get("trail") else "trail"
    if m.get("shader") in ("ParticleDecal", "Decal"):
        return "decal"
    if r.get("mode") == "mesh":
        return "blood" if re.search(_T["blood"], names) else "debris"
    if r.get("mode") == "none" and s.get("trail"):
        return "trail"
    return None


def _by_name(s, m, names):
    """Roles the material's and texture's names decide; None for the rest."""
    if re.search(r"flame|fire(?!work)|candle", (m.get("name") or "").lower()) and not re.search(r"smoke|dust", names):
        return "flame"   # the game's burning flames are often 8 px pixel chunks (leaf_low) drawn additive
    if re.search(r"pixel|leaf_low|heart_low", names) and not re.search(r"wildfire01_pixel|drop_pixel|drops_pixel|water_foam_pix", names):
        return "blood" if "blood" in names else "pixel"
    for key in ("blood", "decal", "distortion", "sparkle", "slime", "snow", "water", "ring", "leaves"):
        if re.search(_T[key], names):
            return "pixel" if key == "blood" and not _reddish(s) else key
    if not m.get("texture") and not (m.get("blend") or "").startswith("additive"):
        return "pixel"
    return None


def _by_numbers(s, names, m):
    """Flames, smoke, sparks, embers, glows and flashes, told apart by texture name, blend, size, speed and lifetime."""
    life = vfx_read.mean(s.get("lifetime")) or 0.0
    size = vfx_read.mean(s.get("size")) or 0.0
    additive = (m.get("blend") or "").startswith("additive")
    few = s.get("max_particles", 1000) <= 3 or (_burst_total(s) <= 3 and not _rate(s))
    if re.search(_T["flame"], names):
        return "flame"
    if re.search(_T["smoke"], names) and not additive:
        return "smoke"
    if few and (additive or re.search(_T["glow"], names)):
        return "flash" if life < 0.6 else "glow"
    if re.search(_T["spark"], names) or (additive and size < 0.15):
        return "ember" if life >= 1.0 and (vfx_read.mean(s.get("gravity")) or 0) <= 0.05 else "spark"
    if re.search(_T["glow"], names):
        return "glow"
    if re.search(_T["smoke"], names):
        return "smoke"
    return "other"


def _reddish(s):
    """True when the system's start colour is blood red (or unknown): the game reuses its blood materials, coloured
    by the particle system, for water drops, sap and other droplets."""
    c = s.get("colour") or {}
    rgb = c.get("colour") or (c.get("colours") or [None])[-1]
    if not rgb:
        return True
    r, g, b = rgb[:3]
    return r > 1.8 * g and r > 1.8 * b


def _burst_total(s):
    em = s.get("emission") or {}
    return sum((vfx_read.mean(b["count"]) or 0) * max(1, b["cycles"]) for b in em.get("bursts", []))


def _rate(s):
    em = s.get("emission") or {}
    return vfx_read.mean(em.get("rate")) or 0.0


def particles(s):
    """(particles in bursts, particles per second from the rate) of one system."""
    return _burst_total(s), _rate(s)

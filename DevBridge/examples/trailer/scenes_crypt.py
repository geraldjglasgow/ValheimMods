"""Writes the crypt run's scene file (crypt_sequence.json): one take in the crypt hall, from the skeletons' march to the
Executioner's axe shattering on the lens. Run it after changing the beats; DevBridge Studio's Scenes tab shows the result."""
import json
import math
import os

import director as d

PLAIN = {"ecr_resolved": True, "ecr_stars": 0, "ecr_mask": 0}
FILM_NIGHT = {"name": "FilmNight", "base": "Clear", "ambient": [0.03, 0.028, 0.03], "fogColor": [0.012, 0.011, 0.012],
              "sun": [0.02, 0.02, 0.022], "ao": [0, 0, 0]}
# each skeleton: where it starts in the doorway column, and where it ends in the fanned-out arc before the camera
ARMY = [("Cutthroat", (-0.8, -12.4), (-3.6, -6.6)), ("Swordsman", (0.8, -12.4), (3.6, -6.6)),
        ("Axeman", (-0.8, -13.8), (-2.4, -6.0)), ("Bonebreaker", (0.8, -13.8), (2.4, -6.0)),
        ("Spearman", (-0.8, -15.2), (-1.2, -5.6)), ("Halberdier", (0.8, -15.2), (1.2, -5.6)),
        ("Bowman", (0.0, -16.6), (0.0, -5.3))]
XB, BJ = [4.8, 4.35, -4.5], [-1.6, 1.35, 3.4]          # the crossbow and Bjorn's chest: the first bolt's line
BOLT, AXE = "ECP_SkeletonCrossbowman_bolt", "ECP_Headsman_axe_hurl"


def cast(name, x=0.0, y=0.0, z=0.0, local=False):
    spot = {"cast": name, "off": [x, y, z]}
    if local:
        spot["local"] = True
    return spot


def on_line(dist, aside=0.0):
    """A point on the bolt's line `dist` metres from the crossbow, stepped `aside` metres off it, level."""
    v = [BJ[i] - XB[i] for i in range(3)]
    n = math.sqrt(sum(c * c for c in v))
    u = [c / n for c in v]
    side = [-u[2], 0.0, u[0]]
    s = math.sqrt(side[0] ** 2 + side[2] ** 2)
    return [round(XB[i] + dist * u[i] + aside * side[i] / s, 2) for i in range(3)]


def cast_and_set():
    cues = [
        {"t": 0, "env": FILM_NIGHT}, {"t": 0, "light": {"tod": 0.0}}, {"t": 0, "world": {"spawns": False}},
        {"t": 0, "lights": {"radius": 40, "scale": 0.32, "range": 0.75}},
        {"t": 0, "lights": {"near": [0, 1, -14], "radius": 7, "scale": 0.75, "range": 1.0}},
        {"t": 0, "lights": {"near": [0, 1, 5], "radius": 7, "scale": 0.6, "range": 1.0}},
        {"t": 0, "prop": "Torch", "child": "attach", "on": "camera", "off": [0.3, 0.5, -0.8], "show": ["equiped"],
         "lamp": {"color": [1.0, 0.62, 0.35], "intensity": 1.2, "range": 14}, "until": 5.5,
         "path": [{"t": 0, "off": [0.3, 0.5, -0.8]}]},
        {"t": 0, "fade": {"to": 1, "over": 0, "color": [0, 0, 0]}}, {"t": 0.03, "fade": {"to": 0, "over": 1.2, "color": [0, 0, 0]}},
        {"t": 0, "player": {"at": [0, 0.2, 16.5], "hide": True, "god": True}},
        {"t": 0, "spawn": "ECP_Headsman", "as": "exe", "at": [0.2, 0.15, 5.6], "face": [-1.6, 0.15, 3.4], "hold": True, "zdo": PLAIN},
        {"t": 0, "extra": "bjorn", "at": [-1.6, 0.2, 3.4], "face": [0.2, 0.2, 5.6], "look": {"model": 0, "hair": "Hair5", "beard": "Beard5"},
         "equip": ["ArmorIronChest", "ArmorIronLegs", "EE_Boots_Iron", "HelmetIron", "SwordIron", "ShieldBanded"]},
        {"t": 0, "extra": "sigrid", "at": [2.0, 0.2, 4.2], "face": [0.2, 0.2, 5.6], "look": {"model": 1, "hair": "Hair14"},
         "equip": ["ArmorTrollLeatherChest", "ArmorTrollLeatherLegs", "EE_Boots_TrollLeather", "CapeTrollHide", "ECP_BoneAxe", "ECP_ShieldKraken"]},
        {"t": 0, "extra": "eirik", "at": [3.0, 0.2, 7.2], "face": [0.2, 0.2, 5.6], "look": {"model": 0, "hair": "Hair2", "beard": "Beard5"},
         "equip": ["ArmorWolfChest", "ArmorWolfLegs", "EE_Boots_Wolf", "HelmetDrake", "CapeWolf", "SwordIron", "ShieldSilver"]},
        {"t": 0, "spawn": "ECP_SkeletonCrossbowman", "as": "xbow", "at": [4.8, 3.15, -4.5], "face": [-1.6, 1.0, 3.4], "hold": True, "zdo": PLAIN},
    ]
    cues += [{"t": 0, "act": n, "look": cast("exe", 0, 1.6, 0)} for n in ("bjorn", "sigrid", "eirik")]
    cues.append({"t": 0.1, "eval": '$cast("exe").Character.m_staggerDamageFactor = 0'})
    for name, (x0, z0), (x1, z1) in ARMY:
        n = name.lower()
        cues.append({"t": 0, "spawn": f"ECP_Skeleton{name}", "as": n, "at": [x0, 0.15, z0], "face": [x1, 0.15, z1], "hold": True, "zdo": PLAIN})
        for field in ("m_walkSpeed", "m_speed", "m_runSpeed"):
            cues.append({"t": 0.1, "eval": f'$cast("{n}").Character.{field} = 2.1'})
        cues.append({"t": 0.1, "anim": n, "speed": 1.25, "for": 9.0})
        cues.append({"t": 0.2 + abs(z0 + 12.4) * 0.1, "ai": n, "moveTo": [x1, 0.15, z1], "straight": True, "run": False, "arrive": 0.3})
        cues.append({"t": 8.0, "ai": n, "face": [x1 * 0.6, 0.15, -1.0]})
    return cues


def march():
    """Low in the aisle, facing the door: they come on, the camera gives ground."""
    return [{"t": 0, "camera": {"keys": [
        {"t": 0, "pos": [0.4, 0.55, -2.4], "look": [0, 1.3, -15], "fov": 54},
        {"t": 6.0, "pos": [0.5, 0.6, 0.6], "look": [0, 1.35, -6], "fov": 56, "ease": "inout"}], "hand": [0.25, 0.35]}}]


def bolt():
    """Up into the crossbow's line of fire; the bolt in slow motion; aside, round behind it, and in with it."""
    loosed = {"exists": BOLT}
    return [
        {"t": 3.6, "camera": {"from": "here", "keys": [{"t": 2.2, "pos": on_line(2.6), "look": cast("xbow", 0, 1.25, 0), "fov": 46, "ease": "inout"}], "hand": [0.2, 0.3]}},
        {"t": 5.0, "attack": "xbow", "target": "bjorn"}, {"t": 5.0, "mark": "aim"},
        {"t": 5.0, "when": loosed, "adopt": BOLT, "as": "bolt", "newest": True},
        {"t": 5.0, "when": loosed, "time": {"scale": 0.12, "over": 0.08}},
        {"t": 5.0, "when": loosed, "mark": "loosed"},
        {"t": 5.0, "when": loosed, "delay": 0.05, "camera": {"from": "here", "keys": [
            {"t": 0.5, "pos": on_line(2.6, 0.9), "look": cast("bolt"), "fov": 46, "ease": "out"},
            {"t": 1.3, "pos": cast("bolt", 0, 0.25, -1.3, True), "look": cast("bolt", 0, 0, 6, True), "fov": 50, "ease": "inout"}],
            "hand": [0.15, 0.3]}},
    ]


HIT = {"near": ["bolt", "bjorn", 1.45]}


def hit(delay, cue):
    """A beat timed from the bolt reaching Bjorn."""
    return dict({"t": 5.0, "when": HIT, "delay": delay}, **cue)


SECOND = {"near": ["bolt2", "eirik", 2.0]}


def fight():
    """Bjorn turns to the first bolt and gets his shield up in time: it sticks in the shield; he turns back and strikes.
    The rear strike on Eirik, hits from the others, the spin through all three. As the Executioner winds up to hurl its
    axe at Bjorn, Eirik in the background raises his shield and a second bolt sticks in it; the axe shatters on the lens."""
    near = {"cam": ["axe", 1.3]}
    exe = cast("exe", 0, 1.6, 0)
    spin_hits = [hit(2.7 + i * 0.07, cue) for i, (who, away) in enumerate((("sigrid", [5.0, 3.6, -3.9]), ("eirik", [0.8, 2.0, 1.6]), ("bjorn", [-3.8, 3.6, -4.6])))
                 for cue in ({"fling": who, "vel": away}, {"vfx": "vfx_HitSparks", "at": cast(who, 0, 1.2, 0)},
                             {"sfx": "sfx_battleaxe_hit", "at": cast(who, 0, 1.2, 0)})]
    stuck = lambda delay, who, bolt, when, push=0.12: [dict({"t": when[0], "when": when[1], "delay": delay}, **cue) for cue in (
        {"prop": BOLT, "on": who, "bone": "LeftHand_Attach", "pose": bolt, "off": [0, 0, push]},
        {"hide": bolt}, {"eval": f'$cast("{bolt}").Projectile.m_vel = $v3(0, 0, 0)'},
        {"sfx": "sfx_metal_shield_blocked", "at": cast(who, 0, 1.3, 0)}, {"vfx": "vfx_HitSparks", "at": cast(bolt)},
        {"mark": f"{bolt}_hit"})]
    return [
        {"t": 5.2, "act": "bjorn", "look": cast("xbow", 0, 1.2, 0)},     # he sees it take aim and turns to it
        {"t": 5.8, "act": "bjorn", "block": True},                       # shield up before it looses
        *stuck(0, "bjorn", "bolt", (5.0, HIT)),
        hit(0, {"time": {"scale": 1.0, "over": 0.4}}),
        hit(0.2, {"camera": {"from": "here", "lead": 1.0, "orbit": {
            "center": [0.2, 0.0, 5.0], "radius": 5.0, "height": 3.3, "from": 215, "to": 300, "time": 6.5, "lift": 1.1, "fov": 50},
            "hand": [0.2, 0.3]}}),
        # he lowers the shield, turns back and strikes; the fight is on
        hit(0.45, {"act": "bjorn", "block": False}), hit(0.45, {"act": "bjorn", "look": exe}),
        hit(0.75, {"act": "bjorn", "attack": True}),
        hit(0, {"act": "sigrid", "look": exe}), hit(0, {"act": "eirik", "look": exe}),
        hit(0.5, {"attack": "exe", "item": "rear", "target": "eirik"}), hit(0.5, {"mark": "rear"}),
        hit(0.55, {"anim": "exe", "speed": 1.4, "for": 1.35}),
        hit(1.2, {"act": "sigrid", "attack": True}), hit(1.4, {"act": "bjorn", "attack": True}),
        hit(1.25, {"act": "eirik", "attack": True}), hit(1.75, {"act": "eirik", "attack": True}),
        hit(1.9, {"attack": "exe", "item": "spin"}), hit(1.9, {"mark": "spin"}),
        hit(1.95, {"anim": "exe", "speed": 1.5, "for": 2.4}),
        *spin_hits,
        hit(2.7, {"shake": 1.2, "decay": 0.3}),
        # the second bolt: Eirik, behind it, turns to the crossbow and blocks it as the Executioner winds up its throw
        hit(2.9, {"attack": "xbow", "target": "eirik"}),
        hit(3.5, {"act": "eirik", "look": cast("xbow", 0, 1.2, 0)}),
        hit(3.9, {"act": "eirik", "block": True}),
        {"t": 9.0, "when": {"exists": BOLT}, "adopt": BOLT, "as": "bolt2", "newest": True},
        *stuck(0, "eirik", "bolt2", (9.0, SECOND), push=0.75),
        {"t": 9.0, "when": SECOND, "delay": 0.35, "act": "eirik", "look": exe},
        # the moment the second bolt is loosed, the Executioner turns on Bjorn and throws
        *[dict({"t": 9.0, "when": {"exists": BOLT}, "delay": d}, **cue) for d, cue in (
            (0, {"ai": "exe", "face": cast("bjorn")}),
            (0.05, {"attack": "exe", "item": "hurl", "target": "bjorn", "skip": 0.4}), (0.05, {"mark": "hurl"}))],
        {"t": 9.0, "when": {"exists": BOLT}, "delay": 0.25, "camera": {"from": "here", "keys": [
            {"t": 0.4, "pos": {"cast": "bjorn", "off": [0.75, 1.55, 0.95], "snap": True}, "look": cast("exe", 0, 1.8, 0), "fov": 54, "ease": "inout"}],
            "hand": [0.3, 0.45]}},
        {"t": 5.0, "when": {"exists": AXE}, "adopt": AXE, "as": "axe", "newest": True},
        {"t": 5.0, "when": near, "mark": "shatter"},
        {"t": 5.0, "when": near, "vfx": "ECP_Headsman_shatter", "at": {"lens": [0, 0, 0.7]}},
        {"t": 5.0, "when": near, "sfx": "sfx_ice_destroyed", "at": {"lens": [0, 0, 0.5]}},
        {"t": 5.0, "when": near, "shake": 2.0, "decay": 0.3},
        {"t": 5.0, "when": near, "fade": {"to": 1, "over": 0.12, "color": [1, 1, 1]}},   # a quick cut to white
        {"t": 5.0, "when": near, "delay": 0.29, "end": True},                            # white held 0.17 s here, 0.16 s in the next
    ]


def scene():
    return {"name": "crypt_sequence", "title": "2 Crypt: march, bolt, Executioner", "order": 2, "status": "rehearsed",
            "notes": ("One take in the crypt hall (400,-1220). Seven bone skeletons march through the door at a low camera that "
                      "gives ground; it rises to the Skeleton Crossbowman on its ledge, into its line of fire. The bolt flies in "
                      "slow motion: the camera steps aside, swings round behind it and rides it into Bjorn. The camera circles "
                      "slowly above Bjorn, Sigrid and Eirik fighting the Crypt Executioner: its rear strike on Eirik, hits from the "
                      "other two, the spin throwing them back; it turns to Bjorn and hurls its axe; the camera is in the way and "
                      "the axe shatters on the lens: white (the Rime Giant's blizzard opens from it)."),
            "origin": [400, 32, -1220], "yaw": 0, "length": 26.0,
            "setup": [{"do": "reset"}, {"do": "spawns", "on": False}, {"do": "purge", "kinds": ["creatures", "extras", "items"]},
                      {"do": "teleport", "at": [14, 0]}, {"do": "path", "x": 0.4, "from": -5, "to": 2}, {"do": "clear", "x": 0.4, "from": -6, "to": 1, "radius": 2.5}, {"do": "cues", "cues": [{"env": FILM_NIGHT}, {"light": {"tod": 0.0}}]},
                      {"do": "wait", "seconds": 1.0}],
            "cues": sorted(cast_and_set() + march() + bolt() + fight(), key=lambda c: c["t"])}


if __name__ == "__main__":
    path = os.path.join(d.FOOTAGE, "scenes", "crypt_sequence.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(scene(), f, indent=1)
    print(path)

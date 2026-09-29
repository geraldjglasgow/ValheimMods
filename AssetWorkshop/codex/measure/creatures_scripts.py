"""Reads the game scripts on a creature prefab that decide how it looks and moves: Character/Humanoid settings,
LevelEffects (star looks), the items it carries and the attack triggers they fire, the ragdoll it leaves, footsteps,
CharacterAnimEvent, ZSyncAnimation, VisEquipment and the Animator. Values only; nothing is copied.
"""
import os
import re

import game
from workshop import unity

CHARACTER_FIELDS = ("m_name", "m_faction", "m_boss", "m_health", "m_speed", "m_walkSpeed", "m_runSpeed",
                    "m_turnSpeed", "m_runTurnSpeed", "m_flying", "m_flyFastSpeed", "m_canSwim", "m_swimDepth",
                    "m_swimSpeed", "m_deathAnimation", "m_jumpForce")
ITEM_LISTS = ("m_defaultItems", "m_randomWeapon", "m_randomArmor", "m_randomShield")


def value(body, name):
    """A field anywhere in the body (first match), as a number when it is one."""
    match = re.search(rf"^\s*{name}: ?(.*)$", body, re.MULTILINE)
    if not match:
        return None
    text = match.group(1).strip()
    return float(text) if re.fullmatch(unity.NUMBER, text) else text


def character(body):
    """The Character/Humanoid settings the codex keeps."""
    return {k[2:]: value(body, k) for k in CHARACTER_FIELDS}


def effect_list(body, name):
    """Paths of the prefabs in an EffectList field (m_deathEffects, m_hitEffects ...)."""
    match = re.search(rf"^  {name}:\n    m_effectPrefabs:\n((?:    .*\n)*)", body, re.MULTILINE)
    if not match:
        return []
    refs = re.findall(r"m_prefab: (\{[^}]*\})", match.group(1))
    return [p for p in (game.path_of(r) for r in refs) if p]


def level_effects(p, body):
    """The LevelEffects setups, one per star above the first: scale, HSV shift, emission, object switched on."""
    setups = []
    block = body[body.index("m_levelSetups:"):] if "m_levelSetups:" in body else ""
    for chunk in block.split("- m_scale: ")[1:]:
        enable = unity.ref(value(chunk, "m_enableObject") or "")[0]
        setups.append({"scale": float(re.match(unity.NUMBER, chunk).group(0)),
                       "hue": value(chunk, "m_hue"), "saturation": value(chunk, "m_saturation"),
                       "value": value(chunk, "m_value"), "set_emission": value(chunk, "m_setEmissiveColor") == 1,
                       "emission": [round(c, 3) for c in unity.numbers(value(chunk, "m_emissiveColor") or "")[:3]],
                       "enable_object": _node_path(p, enable)})
    base = unity.ref(unity.field(body, "m_baseEnableObject"))[0]
    return {"setups": setups, "base_object": _node_path(p, base)}


def _node_path(p, game_object_id):
    """The path of the transform whose GameObject has this fileID, None for {fileID: 0}."""
    if game_object_id in ("0", "", None):
        return None
    for node in p.nodes():
        if unity.ref(unity.field(p.docs[node][1], "m_GameObject"))[0] == game_object_id:
            return p.path_to(node)
    return None


def items(body):
    """{list name: [item prefab paths]} of a Humanoid's carried items; random sets by set name."""
    found = {}
    for name in ITEM_LISTS:
        paths = [game.path_of(i) for i in unity.items(body, name)]
        if any(paths):
            found[name[2:]] = [p for p in paths if p]
    sets = body[body.index("m_randomSets:"):body.index("m_randomItems:")] if "m_randomSets:" in body else ""
    for chunk in sets.split("- m_name: ")[1:]:
        found["set " + chunk.splitlines()[0].strip()] = [p for p in (game.path_of(r) for r in
                                                         re.findall(r"- (\{[^}]*\})", chunk)) if p]
    randoms = body[body.index("m_randomItems:"):body.index("m_unarmedWeapon:")] if "m_randomItems:" in body else ""
    picks = [game.path_of(r) for r in re.findall(r"m_prefab: (\{[^}]*\})", randoms)]
    if any(picks):
        found["randomItems"] = [p for p in picks if p]
    return found


def item_info(path):
    """An item's name, type, animator triggers (primary, secondary), animation state and attach children."""
    p = game.prefab(path)
    drop = next((b for k, b in p.components(p.root()) if k == "ItemDrop"), "")
    anims = re.findall(r"^\s+m_attackAnimation: ?(.*)$", drop, re.MULTILINE)
    chains = re.findall(r"^\s+m_attackChainLevels: ?(\d+)", drop, re.MULTILINE)
    randoms = re.findall(r"^\s+m_attackRandomAnimations: ?(\d+)", drop, re.MULTILINE)
    return {"item": os.path.splitext(os.path.basename(path))[0], "name": value(drop, "m_name"),
            "type": value(drop, "m_itemType"), "animation_state": value(drop, "m_animationState"),
            "attacks": [_trigger(a, c, r) for a, c, r in zip(anims, chains, randoms) if a.strip()],
            "attach": sorted(p.name(n) for n in p.children(p.root()) if p.name(n).startswith("attach"))}


def _trigger(animation, chain, random):
    """The trigger names Attack.Start sets: name, name0..nameN-1 for chains or random picks."""
    count = max(int(chain), int(random))
    return animation.strip() if count <= 1 else f"{animation.strip()}0..{count - 1}"


def ragdoll(paths):
    """The first death-effect prefab with a Ragdoll: its path, bodies, joints and whether it draws its own mesh."""
    for path in paths:
        p = game.prefab(path)
        kinds = [k for _, k, _ in p.all_components()]
        if "Ragdoll" in kinds:
            return {"prefab": path, "rigidbodies": kinds.count("Rigidbody"),
                    "joints": kinds.count("class144") + kinds.count("ConfigurableJoint"),
                    "colliders": sum(kinds.count(c) for c in ("CapsuleCollider", "BoxCollider", "SphereCollider")),
                    "skinned": kinds.count("SkinnedMeshRenderer")}
    return None


def anim_event(p, body):
    """CharacterAnimEvent: foot IK and head look settings, eyes and feet it names."""
    keep = ("m_footIK", "m_footDownMax", "m_footOffset", "m_headRotation", "m_lookWeight", "m_bodyLookWeight",
            "m_headLookWeight", "m_eyeLookWeight", "m_lookClamp", "m_femaleHack")
    out = {k[2:]: value(body, k) for k in keep}
    out["eyes"] = [p.name(unity.ref(i)[0]) for i in unity.items(body, "m_eyes") if p.is_local(unity.ref(i)[0])]
    feet = re.findall(r"m_transform: \{fileID: (-?\d+)\}", body)
    out["feet"] = [p.name(f) for f in feet if p.is_local(f)]
    return out


def footstep(p, body):
    """FootStep: the feet it listens to, footless or not, and the effect names by surface."""
    feet = [unity.ref(i)[0] for i in unity.items(body, "m_feet")]
    return {"feet": [p.name(f) for f in feet if p.is_local(f)], "footless": value(body, "m_footlessFootsteps") == 1,
            "effects": re.findall(r"- m_name: (.*)$", body, re.MULTILINE)}


def animator(body):
    """The Animator's controller and avatar files, root motion and culling."""
    return {"controller": game.path_of(unity.field(body, "m_Controller")),
            "avatar": game.path_of(unity.field(body, "m_Avatar")),
            "apply_root_motion": unity.field(body, "m_ApplyRootMotion") == "1",
            "culling_mode": int(unity.field(body, "m_CullingMode") or 0)}


def sync(body):
    """ZSyncAnimation: the animator parameters it keeps in step across the network."""
    return {k: re.findall(r"^  - (.*)$", _list(body, "m_" + k), re.MULTILINE)
            for k in ("syncBools", "syncFloats", "syncInts")}


def _list(body, name):
    match = re.search(rf"^  {name}:\n((?:  - .*\n)*)", body, re.MULTILINE)
    return match.group(1) if match else ""


def vis_equipment(p, body):
    """VisEquipment: the transforms items hang from, by slot."""
    slots = ("m_leftHand", "m_rightHand", "m_helmet", "m_backShield", "m_backMelee", "m_backTwohandedMelee",
             "m_backBow", "m_backTool", "m_backAtgeir")
    out = {}
    for slot in slots:
        node = unity.ref(unity.field(body, slot))[0]
        if p.is_local(node):
            out[slot[2:]] = p.path_to(node)
    return out

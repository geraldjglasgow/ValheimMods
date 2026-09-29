"""Reads the game's avatars and animation clips from the reference export for rigs.py.

An avatar is humanoid when its human description maps bones (m_Human); the mapping (Unity human name -> bone name),
the twist and stretch settings and the root-motion bone are kept. A clip's length, frame rate, loop and root settings,
its events (function, time, string, number) and what it animates (humanoid muscles, or the transforms of a generic
rig by path) are read from its YAML; the curves themselves are not kept.
"""
import os
import re

import game  # noqa: F401  (puts the workshop's blender folder on the path)
from workshop import unity

MUSCLE_ROOTS = ("RootT.", "RootQ.", "MotionT.", "MotionQ.", "LeftHandIK", "RightHandIK", "LeftFootIK",
                "RightFootIK")


def avatar(path):
    """{name, humanoid, map {human name: bone}, settings} of an Avatar .asset."""
    text = unity.read(path)
    description = text[text.index("m_HumanDescription:"):] if "m_HumanDescription:" in text else ""
    pairs = re.findall(r"- m_BoneName: (.*)\n\s+m_HumanName: (.*)", description)
    settings = {k: _number(description, k) for k in ("m_ArmTwist", "m_ForeArmTwist", "m_UpperLegTwist",
                                                      "m_LegTwist", "m_ArmStretch", "m_LegStretch", "m_FeetSpacing")}
    root = re.search(r"m_RootMotionBoneName: ?(.*)", description)
    return {"name": os.path.splitext(os.path.basename(path))[0], "path": path, "humanoid": bool(pairs),
            "map": {human.strip(): bone.strip() for bone, human in pairs},
            "settings": {k[2:]: v for k, v in settings.items() if v is not None},
            "root_motion_bone": (root.group(1).strip() if root else "") or None,
            "has_translation_dof": _number(description, "m_HasTranslationDoF") == 1}


def _number(text, name):
    match = re.search(rf"{name}: ({unity.NUMBER})", text)
    return float(match.group(1)) if match else None


def clip(path):
    """What the codex keeps of one .anim clip."""
    text = unity.read(path)
    tail = text[text.rindex("m_AnimationClipSettings:"):] if "m_AnimationClipSettings:" in text else ""
    start, stop = _number(tail, "m_StartTime") or 0.0, _number(tail, "m_StopTime") or 0.0
    attributes = set(re.findall(r"^    attribute: (.*)$", text, re.MULTILINE))
    muscles = [a for a in attributes if not a.startswith("m_") and not a.startswith(MUSCLE_ROOTS)]
    paths = set(re.findall(r"^    path: (.+)$", text, re.MULTILINE))
    return {"name": unity.field(text, "m_Name"), "path": path, "length_s": round(stop - start, 4),
            "fps": _number(text, "m_SampleRate"), "loop": _number(tail, "m_LoopTime") == 1,
            "humanoid": bool(_muscles(muscles)),
            "muscles": len(_muscles(muscles)), "parameters": sorted(_parameters(muscles)),
            "bones": len(paths), "root": _root_settings(tail), "events": _events(text)}


def _muscles(attributes):
    """Attributes that are humanoid muscles ('Spine Front-Back', 'LeftHand.Index.1 Stretched')."""
    return [a for a in attributes if " " in a or "." in a]


def _parameters(attributes):
    """Float curves that drive animator parameters (single words such as 'footstep'; the export names some only by
    their CRC32, 'typetree_0x995590F9_...', which rigs.py resolves against the controllers' parameter names)."""
    return {a for a in attributes if " " not in a and "." not in a}


def _root_settings(tail):
    keys = ("m_LoopBlend", "m_LoopBlendOrientation", "m_LoopBlendPositionY", "m_LoopBlendPositionXZ",
            "m_KeepOriginalOrientation", "m_KeepOriginalPositionY", "m_KeepOriginalPositionXZ",
            "m_HeightFromFeet", "m_Mirror")
    flags = {k[2:]: _number(tail, k) == 1 for k in keys}
    return flags | {"orientation_offset_y": _number(tail, "m_OrientationOffsetY"), "level": _number(tail, "m_Level")}


def _events(text):
    """[(time s, function, string parameter, float, int)] of the clip's animation events."""
    block = text[text.rindex("m_Events:"):] if "m_Events:" in text else ""
    found = re.findall(r"- time: (\S+)\n\s+functionName: (.*)\n\s+data: ?(.*)\n\s+objectReferenceParameter: "
                       r"(\{[^}]*\})\n\s+floatParameter: (\S+)\n\s+intParameter: (\S+)", block)
    events = []
    for time, function, data, obj, number, integer in found:
        ref = unity.ref(obj)
        events.append({"time": round(float(time), 4), "function": function.strip(), "data": data.strip(),
                       "float": float(number), "int": int(integer), "object": bool(ref[1]) and ref[0] != "0"})
    return events

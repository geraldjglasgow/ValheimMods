"""Reads the game's animator controllers (.controller) and override controllers (.overrideController) from the
reference export: parameters, layers, every state (in sub-state machines too) with its tag, speed and motion (a clip
or a blend tree), the transitions and their conditions, and the clips each uses. rigs.py writes them into data.
"""
import os
import re

import game
from workshop import unity

PARAMETER_TYPES = {1: "float", 3: "int", 4: "bool", 9: "trigger"}
CONDITIONS = {1: "{}", 2: "!{}", 3: "{} > {}", 4: "{} < {}", 6: "{} == {}", 7: "{} != {}"}
BLEND_TYPES = {0: "1D", 1: "2D simple directional", 2: "2D freeform directional", 3: "2D freeform cartesian",
               4: "direct"}


def read(path):
    """{name, path, parameters, layers, clips} of a controller; an override controller reads its base controller
    and lists which clips it swaps."""
    if path.endswith(".overrideController"):
        return _override(path)
    docs = unity.documents(path)
    head = next(body for cls, body in docs.values() if cls == 91)
    layers = [_layer(docs, chunk) for chunk in head.split("  - serializedVersion: 5\n    m_Name: ")[1:]]
    clips = sorted({c for layer in layers for s in layer["states"] for c in _state_clips(s["motion"])})
    return {"name": unity.field(head, "m_Name"), "path": path, "parameters": _parameters(head),
            "layers": layers, "clips": clips}


def _parameters(head):
    block = head[head.index("m_AnimatorParameters:"):head.index("m_AnimatorLayers:")]
    found = re.findall(r"- m_Name: (.*)\n    m_Type: (\d+)", block)
    return [{"name": n.strip(), "type": PARAMETER_TYPES.get(int(t), t)} for n, t in found]


def _layer(docs, chunk):
    """One layer: name, mask, blending, default weight, default state, its states and any-state transitions."""
    name = chunk.splitlines()[0].strip()
    machine = unity.ref(_value(chunk, "m_StateMachine"))[0]
    states, any_state = [], []
    _machine(docs, machine, "", states, any_state)
    default = unity.ref(unity.field(docs[machine][1], "m_DefaultState"))[0] if machine in docs else "0"
    return {"name": name, "mask": game.path_of(_value(chunk, "m_Mask") or "{fileID: 0}"),
            "blending": "additive" if _value(chunk, "m_BlendingMode") == "1" else "override",
            "weight": float(_value(chunk, "m_DefaultWeight") or 0), "ik_pass": _value(chunk, "m_IKPass") == "1",
            "synced_layer": int(_value(chunk, "m_SyncedLayerIndex") or -1),
            "default_state": unity.field(docs[default][1], "m_Name") if default in docs else None,
            "states": states, "any_state": any_state}


def _value(text, name):
    match = re.search(rf"^\s*{name}: ?(.*)$", text, re.MULTILINE)
    return match.group(1).strip() if match else ""


def _machine(docs, file_id, prefix, states, any_state):
    """Walks a state machine and its children, appending states (named with their machine path) and any-state
    transitions."""
    if file_id not in docs:
        return
    body = docs[file_id][1]
    for state_id in re.findall(r"- m_State: \{fileID: (-?\d+)\}", body):
        if state_id in docs:
            states.append(_state(docs, state_id, prefix))
    for item in unity.items(body, "m_AnyStateTransitions"):
        any_state.append(_transition(docs, unity.ref(item)[0]))
    for child in re.findall(r"- m_StateMachine: \{fileID: (-?\d+)\}", body):
        if child in docs:
            _machine(docs, child, prefix + unity.field(docs[child][1], "m_Name") + "/", states, any_state)


def _state(docs, state_id, path):
    body = docs[state_id][1]
    speed_parameter = unity.field(body, "m_SpeedParameter") if unity.field(body, "m_SpeedParameterActive") == "1" \
        else None
    return {"id": state_id, "name": path + unity.field(body, "m_Name"), "tag": unity.field(body, "m_Tag"),
            "speed": float(unity.field(body, "m_Speed") or 1), "speed_parameter": speed_parameter,
            "motion": _motion(docs, unity.field(body, "m_Motion")),
            "transitions": [_transition(docs, unity.ref(t)[0]) for t in unity.items(body, "m_Transitions")]}


def _motion(docs, value):
    """A clip's path, a blend tree as a dict, or None."""
    file_id, guid = unity.ref(value)
    if guid:
        return game.path_of(value)
    if file_id in docs and docs[file_id][0] == 206:
        return _blend_tree(docs, docs[file_id][1])
    return None


def _blend_tree(docs, body):
    children = []
    for chunk in body.split("  - serializedVersion: 2\n")[1:]:
        position = unity.numbers(_value(chunk, "m_Position"))
        children.append({"motion": _motion(docs, _value(chunk, "m_Motion")),
                         "threshold": float(_value(chunk, "m_Threshold") or 0),
                         "position": list(position), "time_scale": float(_value(chunk, "m_TimeScale") or 1)})
    kind = int(unity.field(body, "m_BlendType") or 0)
    return {"blend": BLEND_TYPES.get(kind, kind), "parameter": unity.field(body, "m_BlendParameter"),
            "parameter_y": unity.field(body, "m_BlendParameterY") if kind in (1, 2, 3) else None,
            "children": children}


def _transition(docs, file_id):
    """A transition: where it goes, its conditions as text, exit time and blend duration (seconds when fixed)."""
    if file_id not in docs:
        return {}
    body = docs[file_id][1]
    found = re.findall(r"m_ConditionMode: (\d+)\n\s+m_ConditionEvent: (.*)\n\s+m_EventTreshold: (.*)", body)
    conditions = [CONDITIONS.get(int(m), "{}?").format(e.strip(), _short(t)) for m, e, t in found]
    target = unity.ref(unity.field(body, "m_DstState"))[0]
    machine = unity.ref(unity.field(body, "m_DstStateMachine"))[0]
    to = unity.field(docs[target][1], "m_Name") if target in docs else (
        "machine " + unity.field(docs[machine][1], "m_Name") if machine in docs else
        "exit" if unity.field(body, "m_IsExit") == "1" else None)
    return {"to": to, "conditions": conditions, "duration": float(unity.field(body, "m_TransitionDuration") or 0),
            "exit_time": float(unity.field(body, "m_ExitTime") or 0) if unity.field(body, "m_HasExitTime") == "1"
            else None}


def _short(value):
    number = float(value)
    return str(int(number)) if number == int(number) else f"{number:g}"


def _state_clips(motion):
    """Every clip path a motion uses (through nested blend trees)."""
    if isinstance(motion, str):
        return [motion]
    if isinstance(motion, dict):
        return [c for child in motion["children"] for c in _state_clips(child["motion"])]
    return []


def _override(path):
    """An override controller: its base controller read whole, plus the clip swaps (original -> override)."""
    text = unity.read(path)
    base = game.path_of(unity.field(text, "m_Controller"))
    pairs = re.findall(r"m_OriginalClip: (\{[^}]*\})\n\s+m_OverrideClip: (\{[^}]*\})", text)
    swaps = [{"original": game.path_of(o), "override": game.path_of(v)} for o, v in pairs if game.path_of(v)]
    out = read(base) if base else {"parameters": [], "layers": [], "clips": []}
    return {**out, "name": os.path.splitext(os.path.basename(path))[0], "path": path, "base": base,
            "overrides": swaps, "clips": sorted(set(out["clips"]) | {s["override"] for s in swaps})}

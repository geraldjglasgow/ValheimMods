"""Summaries rigs.py adds to the rig data: every attack state with its clip's timing (when the hit lands, how long it
plays), the locomotion blend trees (which clip plays at which speed), and body proportions of the humanoid rigs from
their avatars' bone maps.
"""
import os

import numpy as np

HIT_EVENTS = ("Hit", "OnAttackTrigger")
LANDMARKS = ("Hips", "Spine", "Chest", "Neck", "Head", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightUpperArm",
             "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes", "Left Middle Proximal")


def attacks(controllers, clips):
    """[{controller, state, clip, length_s, speed, played_s, hits_s, hit_fraction, trail, exit_time, blend_in_s}]
    for every state tagged 'attack' on a controller's first layer that plays one clip."""
    rows = []
    for path, controller in controllers.items():
        layer = controller["layers"][0] if controller["layers"] else {"states": [], "any_state": []}
        blend_in = {t["to"]: t["duration"] for t in layer["any_state"] if t}
        for state in layer["states"]:
            if state["tag"] == "attack" and isinstance(state["motion"], str) and state["motion"] in clips:
                rows.append(_attack(controller["name"], state, clips[state["motion"]], blend_in))
    return rows


def _attack(controller, state, clip, blend_in):
    speed = state["speed"] or 1.0
    hits = [e["time"] for e in clip["events"] if e["function"] in HIT_EVENTS]
    length = clip["length_s"] or 0.0
    exits = [t["exit_time"] for t in state["transitions"] if t.get("exit_time")]
    return {"controller": controller, "state": state["name"], "clip": clip["name"], "length_s": length,
            "speed": speed, "played_s": round(length / speed, 3), "hits_s": [round(h / speed, 3) for h in hits],
            "hit_fraction": round(hits[0] / length, 3) if hits and length else None,
            "trail": any(e["function"] == "TrailOn" for e in clip["events"]),
            "exit_time": exits[0] if exits else None, "blend_in_s": blend_in.get(state["name"])}


def locomotion(controllers):
    """{controller: [(clip, x, y, time scale)]} of each first-layer state tagged 'idle' whose motion is a 2D blend
    tree: x and y are the blend parameters' values (turn or sideways speed, forward speed in m/s)."""
    out = {}
    for controller in controllers.values():
        layer = controller["layers"][0] if controller["layers"] else {"states": []}
        for state in layer["states"]:
            tree = state["motion"]
            if state["tag"] == "idle" and isinstance(tree, dict) and tree["parameter_y"]:
                children = [(_name(c["motion"]), *c["position"], c["time_scale"]) for c in tree["children"]]
                out[f"{controller['name']}/{state['name']}"] = {"x": tree["parameter"], "y": tree["parameter_y"],
                                                                "children": children}
    return out


def _name(motion):
    if isinstance(motion, str):
        return os.path.splitext(os.path.basename(motion))[0]
    return f"1D blend on {motion['parameter']}" if isinstance(motion, dict) else None


def proportions(bones, avatar_map, height):
    """Landmarks of a humanoid rig (rest heights and lengths in metres, and as fractions of the body's height):
    hips, neck and head heights, shoulder width, arm and leg lengths, hand length."""
    by_name = {b["bone"]: np.array(b["rest_m"]) for b in bones}
    at = {h: by_name[avatar_map[h]] for h in LANDMARKS if avatar_map.get(h) in by_name}
    if "Hips" not in at or "Head" not in at or not height:
        return None
    length = lambda *names: float(sum(np.linalg.norm(at[b] - at[a]) for a, b in zip(names, names[1:])))
    out = {"height_m": height, "hips_m": float(at["Hips"][1]), "head_m": float(at["Head"][1])}
    if "Neck" in at:
        out["neck_m"] = float(at["Neck"][1])
    if all(k in at for k in ("LeftUpperArm", "RightUpperArm")):
        out["shoulders_m"] = float(np.linalg.norm(at["LeftUpperArm"] - at["RightUpperArm"]))
    if all(k in at for k in ("LeftUpperArm", "LeftLowerArm", "LeftHand")):
        out["arm_m"] = length("LeftUpperArm", "LeftLowerArm", "LeftHand")
    if all(k in at for k in ("LeftUpperLeg", "LeftLowerLeg", "LeftFoot")):
        out["leg_m"] = length("LeftUpperLeg", "LeftLowerLeg", "LeftFoot")
    if all(k in at for k in ("LeftHand", "Left Middle Proximal")):
        out["palm_m"] = length("LeftHand", "Left Middle Proximal")
    ratios = {f"{k[:-2]}_ratio": round(v / height, 3) for k, v in out.items() if k != "height_m"}
    return {k: round(v, 3) for k, v in out.items()} | ratios


def footstep_coverage(locomotion, clips):
    """How many moving clips (off the blend trees' centre) in the locomotion trees carry a footstep curve or FootStep
    events, and the controllers whose moving clips carry none."""
    stepping = {c["name"] for c in clips.values()
                if "footstep" in c["parameters"] or any(e["function"] == "FootStep" for e in c["events"])}
    moving = [(key.split("/")[0], child[0]) for key, tree in locomotion.items() for child in tree["children"]
              if child[0] and not child[0].startswith("1D") and (child[1] or child[2])]
    silent = sorted({ctl for ctl, _ in moving} - {ctl for ctl, name in moving if name in stepping})
    return {"moving_clips": len(moving), "with_steps": sum(1 for _, name in moving if name in stepping),
            "silent_controllers": silent}

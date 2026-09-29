"""The skeleton of a creature prefab: which transforms its skinned meshes deform (the bones), how they hang together,
where they rest in the game (metres, Unity axes, bind pose) and which other transforms sit in the armature (sockets
such as RightHand_Attach, leaf ends from the exporter, effect and collider holders).

A rig is a set of deform-bone names; creatures whose skinned meshes use the same set share the rig, and the rig is
named after the first of them in path order (rig_keys).
"""
import os

import numpy as np

import game
import creatures_read as cr
from workshop import unity


def deform_bones(p):
    """{transform node: name} of every bone the prefab's active skinned renderers list (a renderer switched on only
    at a star level counts; a whole second model left inactive in the prefab does not)."""
    bones = {}
    for node, kind, body in p.all_components():
        if kind == "SkinnedMeshRenderer" and _active_parent(p, node):
            for node in cr.bone_nodes(body):
                if p.is_local(node):
                    bones[node] = p.name(node)
    return bones


PREFERRED = ("Player", "JotunWarrior", "Greydwarf", "Troll", "Draugr", "Skeleton", "Goblin", "GoblinShaman",
             "Charred_Melee", "Surtling", "gd_king", "GoblinKing", "SeekerQueen", "TentaRoot", "Crow", "Serpent",
             "Hatchling", "Fenring", "Bonemass", "Dragon", "FrozenKing_0", "Wolf", "Boar", "Lox", "Moose", "Deer",
             "Seeker", "Dverger", "Elaking", "Hugin", "Blob", "Leech", "Chicken", "Bjorn", "Eikthyr", "Fader")


def _active_parent(p, node):
    """True when every transform above the node is active (the node itself may be off: star-level parts)."""
    father = unity.ref(unity.field(p.docs[node][1], "m_Father"))[0]
    return cr.active_path(p, father) if p.is_local(father) else True


def signature(p):
    """The rig's identity: the paths of its deform bones below the armature, sorted and joined ('' when the prefab
    has no skinned mesh)."""
    bones = deform_bones(p)
    if not bones:
        return ""
    top = armature_top(p, bones)
    return "|".join(sorted(_relative(p, node, top) for node in bones))


def rig_keys(signatures):
    """{prefab path: rig key} from {prefab path: signature}: a rig is named after the first of its creatures in
    PREFERRED, else its shortest-named creature that is not a boss aspect, lower case; no skinned mesh: 'static'."""
    members = {}
    for path, sig in signatures.items():
        members.setdefault(sig, []).append(os.path.splitext(os.path.basename(path))[0])
    names = {sig: _rig_name(found) for sig, found in members.items()}
    return {path: names[sig] if sig else "static" for path, sig in signatures.items()}


def _rig_name(creatures):
    preferred = [n for n in PREFERRED if n in creatures]
    if preferred:
        return preferred[0].lower()
    plain = [n for n in creatures if not n.lower().startswith("aspect")] or creatures
    return min(plain, key=lambda n: (len(n), n)).lower()


def armature_top(p, bones):
    """The armature: the deepest transform above every deform bone that is not itself one (the parent of the top
    bones, Armature in most rigs)."""
    chains = [_chain(p, node) for node in bones]
    common = []
    for level in zip(*chains):
        if len(set(level)) > 1 or level[0] in bones:
            break
        common.append(level[0])
    return common[-1] if common else p.root()


def _chain(p, node):
    """The nodes from the prefab root down to this one."""
    chain = []
    while node and p.is_local(node):
        chain.append(node)
        node = unity.ref(unity.field(p.docs[node][1], "m_Father"))[0]
    return chain[::-1]


def skeleton(p, cache=None):
    """The rig as the codex keeps it: the armature, its rest scale, every bone's path and rest position, and the
    other transforms under the armature by kind; sockets also keep their offset from the bone they hang on."""
    cache = {} if cache is None else cache
    bones = deform_bones(p)
    if not bones:
        return None
    top = armature_top(p, bones)
    rows, others = [], {"sockets": [], "ends": [], "holders": []}
    for node in _below(p, top)[1:]:
        path = _relative(p, node, top)
        if node in bones:
            rows.append({"bone": p.name(node), "path": path, "rest_m": _position(p, node, cache)})
        elif _kind(p, node) == "sockets":
            others["sockets"].append(_socket(p, node, path, cache))
        else:
            others[_kind(p, node)].append(path)
    body = p.docs[top][1]
    return {"armature": p.name(top), "armature_scale": list(unity.numbers(unity.field(body, "m_LocalScale"))),
            "armature_rotation": list(unity.numbers(unity.field(body, "m_LocalRotation"))),
            "bone_count": len(rows), "bone_order": bone_order(p), "bones": rows, **others}


def bone_order(p):
    """The bone names, in order, of the active skinned renderer that lists the most bones: the order a skinned
    'attach_skin' item must share, since VisEquipment hands it the body's bone array as it is."""
    lists = [cr.bone_nodes(body) for node, kind, body in p.all_components()
             if kind == "SkinnedMeshRenderer" and _active_parent(p, node)]
    longest = max(lists, key=len) if lists else []
    return [p.name(n) if p.is_local(n) else None for n in longest]


def _position(p, node, cache):
    return [round(float(v), 3) for v in cr.world(p, node, cache)[:3, 3]]


def _socket(p, node, path, cache):
    """A socket: its path, rest position in the prefab and its local offset (metres in the prefab: the parent
    bone's scale applied) and rotation on its parent."""
    body = p.docs[node][1]
    father = unity.ref(unity.field(body, "m_Father"))[0]
    offset = cr.world(p, father, cache)[:3, :3] @ np.array(unity.numbers(unity.field(body, "m_LocalPosition")))
    return {"path": path, "rest_m": _position(p, node, cache), "offset_m": [round(float(v), 4) for v in offset],
            "rotation": [round(v, 4) for v in unity.numbers(unity.field(body, "m_LocalRotation"))]}


def _below(p, top):
    """The top node and all its descendants, parents first."""
    order, stack = [], [top]
    while stack:
        node = stack.pop()
        order.append(node)
        stack.extend(reversed(p.children(node)))
    return order


def _relative(p, node, top):
    full, base = p.path_to(node), p.path_to(top)
    return full[len(base) - len(p.name(top)):]


def _kind(p, node):
    """A non-deform transform in the armature: 'ends' (exporter leaf ends), 'holders' (carry components: effects,
    colliders, cloth) or 'sockets' (plain transforms: attach points, markers)."""
    name = p.name(node)
    if name.endswith("_end") or name.endswith("_End") or name.endswith(".end"):
        return "ends"
    if any(k != "Transform" for k, _ in p.components(node)):
        return "holders"
    return "sockets"


def proportions(rows):
    """A few landmarks from the rest positions: height of the highest bone, width of the widest pair, and the
    lowest bone's height (feet), metres."""
    points = np.array([r["rest_m"] for r in rows])
    return {"top_m": round(float(points[:, 1].max()), 3), "bottom_m": round(float(points[:, 1].min()), 3),
            "span_m": round(float(points[:, 0].max() - points[:, 0].min()), 3),
            "length_m": round(float(points[:, 2].max() - points[:, 2].min()), 3)}

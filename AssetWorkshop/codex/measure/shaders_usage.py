"""Which of the game's materials its prefabs actually draw, and on what kind of asset: the reference export also holds
every model file's import materials (most on Unity's Standard shader), which the game never shows, so counts of
shaders and textures start from the prefabs' renderers.

    import shaders_usage
    use = shaders_usage.scan()        # {material path: {"kinds": [...], "renderers": [...], "prefabs": n}}

A prefab's kind comes from the game scripts on it (ItemDrop: item, Piece: piece, Character: creature, ...), and a
nested prefab's materials count for the kind of the prefab that nests it. The scan reads every prefab as text (about
a minute) and is kept in codex/out/paint/usage.json; scan(fresh=True) reads the export again.
"""
import functools
import json
import os
import re

import game

CACHE = os.path.join(game.WORKSHOP, "codex", "out", "paint", "usage.json")
DLL_GUID = "c1b78fa918b030faf1c1f6f6164daeb2"
RENDERERS = {23: "mesh", 137: "skinned", 199: "particle", 96: "trail", 120: "line", 212: "sprite"}
KINDS = (                                     # first match wins
    ("player", ("Player",)),
    ("creature", ("Character", "Humanoid", "MonsterAI", "AnimalAI", "Fish")),
    ("item", ("ItemDrop",)),
    ("piece", ("Piece",)),
    ("ship", ("Ship", "Vagon")),
    ("env", ("TreeBase", "TreeLog", "MineRock", "MineRock5", "Destructible", "Pickable")),
    ("location", ("Location", "LocationProxy", "Room", "DungeonGenerator")),
    ("vfx", ("Projectile", "Aoe")),
)
_HEAD = re.compile(r"^--- !u!(\d+) &(-?\d+)", re.MULTILINE)
_REF = r"\{fileID: (-?\d+), guid: (\w+), type: \d\}"


def scan(fresh=False):
    """{material path: {"kinds": sorted kinds, "renderers": sorted renderer kinds, "prefabs": count}}."""
    if not fresh and os.path.exists(CACHE):
        with open(CACHE, encoding="utf-8") as handle:
            return json.load(handle)
    use = {}
    for info in _prefabs().values():
        for material, renderer in info["materials"]:
            entry = use.setdefault(material, {"kinds": set(), "renderers": set(), "prefabs": 0})
            entry["kinds"].add(info["kind"])
            entry["renderers"].add(renderer)
            entry["prefabs"] += 1
    use = {m: {"kinds": sorted(e["kinds"]), "renderers": sorted(e["renderers"]), "prefabs": e["prefabs"]}
           for m, e in sorted(use.items())}
    _save(use)
    return use


def prefab_kinds(fresh=False):
    """{prefab path: kind} for every prefab, nesting included (cached beside the material scan)."""
    path = CACHE.replace("usage.json", "prefab_kinds.json")
    if not fresh and os.path.exists(path):
        with open(path, encoding="utf-8") as handle:
            return json.load(handle)
    kinds = {p: info["kind"] for p, info in sorted(_prefabs().items())}
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(kinds, handle, indent=0)
    return kinds


@functools.lru_cache(maxsize=None)
def _prefabs():
    """Every prefab read once per run, with nested prefabs' kinds filled in."""
    prefabs = {path: _read(path) for path in game.find("**/*.prefab")}
    _inherit_kinds(prefabs)
    return prefabs


def _read(path):
    """One prefab as {kind, materials: [(material path, renderer kind)], nested: [prefab paths]}."""
    text = game.unity.read(path)
    heads = list(_HEAD.finditer(text))
    ends = [h.start() for h in heads[1:]] + [len(text)]
    materials, classes = [], set()
    for head, end in zip(heads, ends):
        body, cls = text[head.end():end], int(head.group(1))
        if cls in RENDERERS:
            materials += [(m, RENDERERS[cls]) for m in _materials(body)]
        elif cls == 114:
            classes.add(_script(body))
    materials += [(m, "override") for m in _overrides(text)]
    nested = [game.unity.asset_path(g) for g in re.findall(r"m_SourcePrefab: \{fileID: \d+, guid: (\w+)", text)]
    return {"kind": _kind(classes, materials), "materials": materials, "nested": [n for n in nested if n]}


def _materials(body):
    block = re.search(r"^  m_Materials:\n((?:  - .*\n)*)", body, re.MULTILINE)
    guids = re.findall(_REF, block.group(1)) if block else []
    return [p for p in (game.unity.asset_path(g) for _, g in guids if g != game.unity.BUILTIN_ASSETS) if p]


def _overrides(text):
    """Materials a nested prefab's instance swaps in (m_Modifications on m_Materials)."""
    found = re.findall(r"propertyPath: m_Materials\.Array\.data\[\d+\]\n\s+value: ?\n\s+objectReference: " + _REF,
                       text)
    return [p for p in (game.unity.asset_path(g) for _, g in found) if p and p.endswith(".mat")]


def _script(body):
    match = re.search(r"m_Script: " + _REF, body)
    if not match or match.group(2) != DLL_GUID:
        return ""
    return game._classes().get(int(match.group(1)), "")


def _kind(classes, materials):
    for kind, names in KINDS:
        if classes & set(names):
            return kind
    if materials and all(r in ("particle", "trail", "line") for _, r in materials):
        return "vfx"
    return "other"


def _inherit_kinds(prefabs):
    """A nested prefab with no kind of its own takes the kind of the prefab that nests it (three levels deep)."""
    for _ in range(3):
        for info in list(prefabs.values()):
            for child in info["nested"]:
                inner = prefabs.get(child)
                if inner and inner["kind"] in ("other", "vfx") and info["kind"] not in ("other", "vfx"):
                    inner["kind"] = info["kind"]


def _save(use):
    os.makedirs(os.path.dirname(CACHE), exist_ok=True)
    with open(CACHE, "w", encoding="utf-8") as handle:
        json.dump(use, handle, indent=0)


if __name__ == "__main__":
    result = scan(fresh=True)
    prefab_kinds(fresh=True)
    print(len(result), "materials drawn by prefabs")

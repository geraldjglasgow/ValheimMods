"""Finds every item prefab in the reference export (a prefab whose root carries an ItemDrop) and measures each one:
its shared data, held, worn and dropped models, colliders, trail, upgrade glow and cloth. The result is cached in
codex/out/items/scan.json, since reading 1,500 prefabs takes a few minutes.

    python codex/measure/items_scan.py          # rescans and rewrites the cache
"""
import json
import os
import subprocess

import game
import items_extras
import items_model
import items_shared

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "out", "items")
CACHE = os.path.join(OUT, "scan.json")
ITEMDROP_SCRIPT = "m_Script: {fileID: 825357479, guid: c1b78fa918b030faf1c1f6f6164daeb2"


def item_prefabs():
    """Paths of every prefab that has an ItemDrop anywhere in the export, sorted (grep when there is one, else a
    plain walk, which takes a few minutes)."""
    try:
        listing = subprocess.run(["grep", "-rl", "--include=*.prefab", ITEMDROP_SCRIPT, "."], cwd=game.ROOT,
                                 capture_output=True, text=True, check=True).stdout
        return sorted(p[2:] if p.startswith("./") else p for p in listing.splitlines() if p)
    except (OSError, subprocess.CalledProcessError):
        return [p for p in game.find("**/*.prefab") if _has_itemdrop(p)]


def _has_itemdrop(path):
    with open(os.path.join(game.ROOT, path), encoding="utf-8", errors="ignore") as handle:
        return ITEMDROP_SCRIPT in handle.read()


def scan_one(path):
    """Everything the codex keeps of one item prefab, or None when its ItemDrop is not on the root."""
    prefab = game.prefab(path)
    root = prefab.root()
    body = next((b for kind, b in prefab.components(root) if kind == "ItemDrop"), None)
    if body is None:
        return None
    found = items_model.frames(prefab)
    parts = items_model.parts(prefab)
    colliders, rigidbody = items_model.colliders(prefab)
    record = {"prefab": path, "name": prefab.name(root), "shared": items_shared.read(body),
              "frames": sorted(found), "root_scale": round(prefab.world_scale(root), 4),
              "colliders": colliders, "rigidbody": rigidbody}
    for role in ("held", "worn", "back", "drop"):
        frame = {"held": found.get("attach"), "back": found.get("attach_back")}.get(role, root)
        record[role] = items_model.measure(prefab, parts[role], frame)
    record.update(items_extras.extras(prefab, found))
    record["projectile"] = _projectile(record["shared"]["attack"].get("projectile_path"))
    return record


def _projectile(path):
    """The model of the projectile an item's attack fires (an arrow in flight), measured in its own frame."""
    if not path or not path.endswith(".prefab"):
        return None
    prefab = game.prefab(path)
    found = items_model._renderers(prefab, prefab.root(), True)
    measured = items_model.measure(prefab, found, prefab.root())
    return dict(measured, prefab=path) if measured else None


def scan_all():
    """Measures every item prefab and writes the cache; returns the records."""
    records, failures = [], []
    for path in item_prefabs():
        try:
            record = scan_one(path)
        except Exception as error:    # a prefab the reader cannot parse is noted, not fatal
            failures.append({"prefab": path, "error": f"{type(error).__name__}: {error}"})
            continue
        if record:
            records.append(record)
    os.makedirs(OUT, exist_ok=True)
    with open(CACHE, "w", encoding="utf-8") as handle:
        json.dump({"records": records, "failures": failures}, handle, indent=1)
    return records


def load():
    """The cached scan (scanning first when there is none)."""
    if not os.path.exists(CACHE):
        scan_all()
    with open(CACHE, encoding="utf-8") as handle:
        return json.load(handle)["records"]


if __name__ == "__main__":
    found = scan_all()
    print(f"{len(found)} items scanned into {os.path.relpath(CACHE)}")

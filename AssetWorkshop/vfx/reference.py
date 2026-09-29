"""Stages a game effect from the reference export into the Unity project for previews only: the prefab and every file
it references by GUID (nested prefabs, materials, the exported dummy shaders, textures, meshes), copied with their .meta
files (the game's GUIDs) into unity/Assets/Reference/Vfx/, which is gitignored and which the vfx bundle build refuses
to include. A file whose GUID the project already has anywhere is not copied again. Scripts stay behind (the prefab's
game components load as missing scripts, which the preview ignores).

    reference.stage("Effects/Fire/fx_Torch_Green.prefab")   -> "Assets/Reference/Vfx/Effects/Fire/fx_Torch_Green.prefab"
"""
import glob
import json
import os
import re
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, ".."))
sys.path.insert(0, os.path.join(WORKSHOP, "blender"))
sys.path.insert(0, os.path.join(WORKSHOP, "codex", "measure"))

from workshop import unity  # noqa: E402

PROJECT = os.path.join(WORKSHOP, "unity")
TARGET = "Assets/Reference/Vfx"
YAML = (".prefab", ".mat", ".asset", ".controller", ".anim")
SKIP = (".dll", ".cs")
_project_guids = None


def project_guids():
    """{guid: project path} of every asset already in the Unity project."""
    global _project_guids
    if _project_guids is None:
        _project_guids = {}
        for meta in glob.glob(os.path.join(PROJECT, "Assets", "**", "*.meta"), recursive=True):
            with open(meta, "rb") as handle:
                found = re.search(rb"guid: ([0-9a-f]{32})", handle.read(200))
            if found:
                _project_guids[found.group(1).decode()] = os.path.relpath(meta[:-5], PROJECT).replace("\\", "/")
    return _project_guids


def stage(export_path):
    """Copies a game prefab and its dependencies; returns the prefab's project path (a project path passes through)."""
    if export_path.startswith("Assets/"):
        return export_path
    guids, seen, todo = project_guids(), set(), [export_path]
    own = _guid(export_path)
    if own in guids and not guids[own].startswith(TARGET):
        return guids[own]
    while todo:
        path = todo.pop()
        if path in seen:
            continue
        seen.add(path)
        guid = _guid(path)
        if guid in guids and not guids[guid].startswith(TARGET):
            continue
        todo.extend(_references(path))
        _copy(path)
    return f"{TARGET}/{export_path}"


def _guid(path):
    with open(os.path.join(unity.ROOT, path + ".meta"), "rb") as handle:
        found = re.search(rb"guid: ([0-9a-f]{32})", handle.read(200))
    return found.group(1).decode() if found else None


def _references(path):
    """Export paths of the files a YAML asset references (not scripts, not Unity's built-in assets)."""
    if not path.endswith(YAML):
        return []
    text = unity.read(path)
    found = []
    for guid in set(re.findall(r"guid: ([0-9a-f]{32})", text)):
        if guid in (unity.BUILTIN_ASSETS, unity.BUILTIN_MESHES):
            continue
        target = unity.asset_path(guid)
        if target and not target.endswith(SKIP) and os.path.isfile(os.path.join(unity.ROOT, target)):
            found.append(target)
    return found


def _copy(path):
    destination = os.path.join(PROJECT, TARGET, path)
    os.makedirs(os.path.dirname(destination), exist_ok=True)
    for suffix in ("", ".meta"):
        source = os.path.join(unity.ROOT, path + suffix)
        if os.path.exists(source) and not os.path.exists(destination + suffix):
            shutil.copyfile(source, destination + suffix)


def write_lights(project_paths):
    """The game references' LightFlicker settings by light name, for the preview's lights (JSON path)."""
    import vfx_effect
    rows = []
    for project_path in project_paths:
        if not project_path.startswith(TARGET):
            continue
        export_path = project_path[len(TARGET) + 1:]
        for script in vfx_effect.all_of(vfx_effect.effect(export_path), "scripts"):
            if script["class"] == "LightFlicker":
                s = script["settings"]
                rows.append({"prefab": export_path, "light": script["node"].rsplit("/", 1)[-1],
                             "flicker": {"enabled": True, "intensity": s.get("m_flickerIntensity", 0.1),
                                         "speed": s.get("m_flickerSpeed", 10), "movement": s.get("m_movement", 0.1),
                                         "ttl": s.get("m_ttl", 0), "fade": s.get("m_fadeDuration", 0.2),
                                         "fade_in": s.get("m_fadeInDuration", 0)}})
    path = os.path.join(WORKSHOP, "out", "vfx_reference_lights.json")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump({"lights": rows}, handle, indent=1)
    return path

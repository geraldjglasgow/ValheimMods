"""The property blocks of the game's shaders, read from the dummies AssetRipper exports (Shaders/*.shader keep every
property: name, inspector label, type, default and attributes, though not the shader code).

    shaders_props.all_shaders()     # {"Creature": {"name": "Custom/Creature", "file": ..., "properties": [...]}}
"""
import glob
import os
import re

import game

FOLDERS = ("Shaders", "Shader", "Shaders/BuiltInOverrides")
_PROPERTY = re.compile(r'^\s*((?:\[[^\]]*\]\s*)*)(\w+)\s*\("((?:[^"\\]|\\.)*)",\s*'
                       r'(Range\([^)]*\)|\w+)\)\s*=\s*(.*?)\s*$')
UI_ONLY = re.compile(r"(Foldout|Header|Spacer)$")


def all_shaders():
    """{file stem: {name, file, properties}} for every exported shader dummy."""
    found = {}
    for folder in FOLDERS:
        for path in sorted(glob.glob(os.path.join(game.ROOT, folder, "*.shader"))):
            stem = os.path.splitext(os.path.basename(path))[0]
            with open(path, encoding="utf-8", errors="replace") as handle:
                text = handle.read()
            found[stem] = {"name": _name(text), "file": os.path.relpath(path, game.ROOT).replace("\\", "/"),
                           "properties": properties(text)}
    return found


def _name(text):
    match = re.search(r'Shader\s+"([^"]+)"', text)
    return match.group(1) if match else ""


def properties(text):
    """[{name, label, type, default, attributes, ui_only}] from a shader's Properties block."""
    block = re.search(r"Properties\s*\{(.*?)\n\t\}", text, re.S)
    found = []
    for line in (block.group(1).splitlines() if block else []):
        match = _PROPERTY.match(line)
        if not match:
            continue
        attributes, name, label, kind, default = match.groups()
        found.append({"name": name, "label": _clean(label), "type": kind, "default": default.replace(" {}", ""),
                      "attributes": re.findall(r"\[([^\]]*)\]", attributes), "ui_only": bool(UI_ONLY.search(name))})
    return found


def _clean(label):
    """The inspector label without the Markdown shader GUI's '#', '&' and '-' markers."""
    return re.sub(r"^[#\-\s]+|&+$", "", label).strip()

"""One pass over every prefab, scene, asset and animation clip of the reference export (about 27,000 files, 3.5 GB
of YAML): which files hold a sound (a ZSFX, or an AudioSource with a clip of its own) and which prefabs each file
references. Animation clips count because their events (functionName Effect) spawn sound prefabs. The result is
cached in codex/out/sfx/scan.json (seconds to rebuild; pass --rescan to sfx.py after the export is ripped again).
"""
import json
import os
import re
import sys
import time

import game  # noqa: I100  (first: it puts the workshop's helpers on the path)
from workshop import unity

OUT = os.path.join(game.WORKSHOP, "codex", "out", "sfx")
CACHE = os.path.join(OUT, "scan.json")
ZSFX_SCRIPT = "fileID: 313648218, guid: c1b78fa918b030faf1c1f6f6164daeb2"
CLIP_SOURCE = "m_Resource: {fileID: 8300000"
CLIP_REF = re.compile(r"fileID: 8300000, guid: ([0-9a-f]{32})")
GUID_REF = re.compile(r"guid: ([0-9a-f]{32})")
KINDS = (".prefab", ".unity", ".asset", ".anim")


def files():
    """Every prefab, scene and asset file under the export, relative, forward slashes."""
    found = []
    for folder, _, names in os.walk(unity.ROOT):
        for name in names:
            if name.endswith(KINDS):
                found.append(os.path.relpath(os.path.join(folder, name), unity.ROOT).replace("\\", "/"))
    return sorted(found)


def scan_file(path, prefab_guids):
    """{'zsfx', 'source', 'clips', 'refs'} of one file, or None when it references neither sounds nor prefabs."""
    with open(os.path.join(unity.ROOT, path), encoding="utf-8", errors="replace") as handle:
        text = handle.read()
    refs = sorted(set(GUID_REF.findall(text)) & prefab_guids)
    clips = sorted(set(CLIP_REF.findall(text)))
    if not refs and not clips:
        return None
    return {"zsfx": ZSFX_SCRIPT in text, "source": CLIP_SOURCE in text, "clips": clips, "refs": refs}


def prefab_guids():
    """{guid: path} of every prefab in the export, from the GUID index."""
    index = unity._load_index()
    return {guid: path for guid, path in index.items() if path.endswith(".prefab")}


def build():
    """Scans everything and writes the cache."""
    started, guids = time.time(), prefab_guids()
    wanted, table = set(guids), {}
    for number, path in enumerate(files()):
        entry = scan_file(path, wanted)
        if entry:
            table[path] = entry
        if number % 2000 == 0:
            print(f"scan: {number} files, {time.time() - started:.0f} s", file=sys.stderr)
    result = {"prefabs": guids, "files": table}
    os.makedirs(OUT, exist_ok=True)
    with open(CACHE, "w", encoding="utf-8") as handle:
        json.dump(result, handle)
    return result


def load(rescan=False):
    """The scan, from the cache unless rescan or there is none."""
    if not rescan and os.path.exists(CACHE):
        with open(CACHE, encoding="utf-8") as handle:
            return json.load(handle)
    return build()


def sound_files(scan):
    """Files holding a ZSFX or an AudioSource with its own clip."""
    return sorted(p for p, e in scan["files"].items() if e["zsfx"] or e["source"])


def referrers(scan):
    """{prefab guid: [files that reference it]}."""
    table = {}
    for path, entry in scan["files"].items():
        for guid in entry["refs"]:
            table.setdefault(guid, []).append(path)
    return table


def guid_of(path):
    """The GUID of an export file, from its .meta."""
    return unity._meta_guid(os.path.join(unity.ROOT, path + ".meta"))

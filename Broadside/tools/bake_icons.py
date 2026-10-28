"""Copy rendered icons from the game's icon cache into Broadside/assets/icons, gzipped, so the mod carries them.

Broadside renders a hammer or item icon only when it finds none (KwIconCache): first among the icons the mod carries
(assets/icons, embedded as Broadside.icons.<file>), then in BepInEx/cache/Broadside.Icons. Carrying the icons that are the same for every player (the ready-made ships, the pieces and
items that are rendered rather than painted) means nobody renders them.

Carried icons are found first, so the game renders them again only when the mod carries none: to make them anew (a
change to how ships, the ready-made ships or a rendered piece or item look), delete assets/icons/*.icon, build, launch
the LocalTesting profile and take up the hammer once (every ready-made ship's icon is then in the cache), run this, and
build again: the icons are embedded by Broadside.csproj.

A carried icon's file is named as KwIconCache names it, "<name>@<hash>.icon": for a ready-made ship the hash of the hash
of its file's text (FNV-1a, 64 bits, over UTF-16 code units), for a piece or item the hash of nothing; with KwIconCache.Look
above 1, "<look>|" in front of what is hashed. A ship's icon is
copied from the cache under that name, or under any older stamp of the same name (the pixels are the same ship).
Players' own designs are never carried.

    python Broadside/tools/bake_icons.py [--cache <folder>] [--dry-run]
"""
import argparse
import glob
import gzip
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.join(HERE, "..", "Broadside")
SHIPS = os.path.join(PROJECT, "assets", "ships")
ICONS = os.path.join(PROJECT, "assets", "icons")
SOURCE = os.path.join(PROJECT, "Pieces", "KwIconCache.cs")
PROFILE_CACHE = os.path.join(os.environ.get("APPDATA", ""), "r2modmanPlus-local", "Valheim", "profiles",
                             "LocalTesting", "BepInEx", "cache", "Broadside.Icons")


def fnv(text):
    """KwIconCache.Hash: FNV-1a 64 over UTF-16 code units, 16 hex digits."""
    h = 0xCBF29CE484222325
    data = text.encode("utf-16-le")
    for i in range(0, len(data), 2):
        h = ((h ^ (data[i] | (data[i + 1] << 8))) * 0x100000001B3) & 0xFFFFFFFFFFFFFFFF
    return f"{h:016x}"


def look():
    """KwIconCache.Look, read from its source."""
    with open(SOURCE, encoding="utf-8") as f:
        match = re.search(r"public const int Look = (\d+);", f.read())
    if not match:
        sys.exit(f"no Look number in {SOURCE}")
    return int(match.group(1))


def stamp(source):
    """KwIconCache.FileName's hash: look 1 is the source alone, a later look in front of it."""
    n = look()
    return fnv(source if n == 1 else f"{n}|{source}")


def safe(name):
    """KwIconCache.Safe: anything but letters, digits, _ and - becomes _."""
    return "".join(c if c.isalnum() or c in "_-" else "_" for c in name)


def newest_cache(root):
    """The cache folder itself; a folder per build or version inside it is from the cache's first builds."""
    if glob.glob(os.path.join(root, "*.icon")):
        return root
    folders = [f for f in glob.glob(os.path.join(root, "*")) if os.path.isdir(f)]
    if not folders:
        sys.exit(f"no icon cache in {root}: launch the game with this build and take up the hammer once")
    return max(folders, key=os.path.getmtime)


def ship_icons(cache):
    """Each ready-made ship: (target file, source file in the cache or None)."""
    found = []
    for path in sorted(glob.glob(os.path.join(SHIPS, "*.json"))):
        with open(path, encoding="utf-8-sig", newline="") as f:
            text = f.read()
        name = safe("KW_bp_Ready-made/" + os.path.splitext(os.path.basename(path))[0])
        target = f"{name}@{stamp(fnv(text))}.icon"
        exact = os.path.join(cache, target)
        older = sorted(glob.glob(os.path.join(cache, name + "@*.icon")), key=os.path.getmtime)
        found.append((target, exact if os.path.exists(exact) else (older[-1] if older else None)))
    return found


def piece_icons(cache):
    """Every piece or item icon in the cache (stamped with the hash of nothing); saved ships are not pieces."""
    nothing = stamp("")
    return [(os.path.basename(p), p) for p in sorted(glob.glob(os.path.join(cache, f"*@{nothing}.icon")))
            if not os.path.basename(p).startswith("KW_bp_")]


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--cache", help="the icon cache folder (default: LocalTesting's)")
    parser.add_argument("--dry-run", action="store_true", help="list what would be carried, change nothing")
    args = parser.parse_args()
    cache = args.cache or newest_cache(PROFILE_CACHE)
    icons = ship_icons(cache) + piece_icons(cache)
    missing = [t for t, s in icons if s is None]
    print(f"cache: {cache}")
    for target, source in icons:
        print(f"  {'MISSING' if source is None else 'carry  '} {target}")
    if missing:
        sys.exit(f"{len(missing)} icon(s) not in the cache: delete assets/icons/*.icon, build, take up the hammer in game "
                 "once, then run this again")
    if args.dry_run:
        return
    os.makedirs(ICONS, exist_ok=True)
    for old in glob.glob(os.path.join(ICONS, "*.icon")):
        os.remove(old)
    for target, source in icons:
        with open(source, "rb") as raw, open(os.path.join(ICONS, target), "wb") as packed:
            packed.write(gzip.compress(raw.read(), 9))
    print(f"{len(icons)} icons carried in {os.path.normpath(ICONS)}")


if __name__ == "__main__":
    main()

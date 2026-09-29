"""Measures every audio clip in the reference export (about 4,500 OGG files, 7.4 hours) with sfx_features, in
parallel, and caches the numbers in codex/out/sfx/clips.json keyed by path, size and modification time, so a re-run
only measures what changed. Also reads each clip's import settings from its .meta (load type, compression).

    python codex/measure/sfx_clips.py          (with the venv's Python; see sfx_audio.py)
"""
import concurrent.futures
import json
import os
import re
import sys
import time

import sfx_audio
import sfx_features

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "out", "sfx")
CACHE = os.path.join(OUT, "clips.json")
LOAD_TYPES = {0: "decompress_on_load", 1: "compressed_in_memory", 2: "streaming"}


def all_clips():
    """Every .ogg/.wav under the export, relative, forward slashes."""
    found = []
    for folder, _, names in os.walk(sfx_audio.ROOT):
        for name in names:
            if name.lower().endswith((".ogg", ".wav")):
                found.append(os.path.relpath(os.path.join(folder, name), sfx_audio.ROOT).replace("\\", "/"))
    return sorted(found)


def stamp(path):
    full = sfx_audio.full_path(path)
    return f"{os.path.getsize(full)}:{int(os.path.getmtime(full))}"


def import_settings(path):
    """{'load', 'preload', 'background'} of a clip from its .meta."""
    try:
        with open(sfx_audio.full_path(path) + ".meta", encoding="utf-8") as handle:
            text = handle.read()
    except OSError:
        return {}
    block = text[text.find("defaultSettings:"):]
    load = re.search(r"loadType: (\d)", block)
    preload = re.search(r"preloadAudioData: (\d)", block)
    background = re.search(r"loadInBackground: (\d)", text)
    return {"load": LOAD_TYPES.get(int(load.group(1)), "?") if load else None,
            "preload": bool(int(preload.group(1))) if preload else None,
            "background": bool(int(background.group(1))) if background else None}


def measure_one(path):
    """(path, numbers or {'error': ...}) for one clip; run in a worker process."""
    try:
        samples, rate = sfx_audio.read(path)
        numbers = sfx_features.measure(samples, rate)
    except Exception as error:  # a broken file must not stop the survey
        numbers = {"error": repr(error)}
    numbers.update(import_settings(path))
    numbers["stamp"] = stamp(path)
    return path, numbers


def load_cache():
    try:
        with open(CACHE, encoding="utf-8") as handle:
            return json.load(handle)
    except (OSError, ValueError):
        return {}


def save_cache(table):
    os.makedirs(OUT, exist_ok=True)
    temp = CACHE + ".tmp"
    with open(temp, "w", encoding="utf-8") as handle:
        json.dump(table, handle)
    os.replace(temp, CACHE)


def measure_all(paths=None, workers=None):
    """{clip path: numbers} for every clip, measuring only the ones the cache does not hold as they are now."""
    table, paths = load_cache(), paths or all_clips()
    todo = [p for p in paths if table.get(p, {}).get("stamp") != stamp(p)]
    started = time.time()
    with concurrent.futures.ProcessPoolExecutor(max_workers=workers or max(1, os.cpu_count() - 1)) as pool:
        for count, (path, numbers) in enumerate(pool.map(measure_one, todo, chunksize=8), 1):
            table[path] = numbers
            if count % 250 == 0:
                print(f"clips: {count}/{len(todo)} in {time.time() - started:.0f} s", file=sys.stderr)
                save_cache(table)
    save_cache(table)
    return {p: table[p] for p in paths}


if __name__ == "__main__":
    result = measure_all()
    errors = [p for p, n in result.items() if "error" in n]
    print(f"{len(result)} clips measured, {len(errors)} errors")

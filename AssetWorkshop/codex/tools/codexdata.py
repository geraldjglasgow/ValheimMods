"""Reads the codex's measurements (codex/data/*.json) for the tools: one category's label, stats, samples and
references, merged over every data file that has the category. Plain Python, so Blender's Python can import it too.

    cat = codexdata.category("weapon.axe_2h")                  # None when no data file has it
    cat["stats"]["triangles"]["median"], cat["references"], cat["samples"], cat["sources"]

Tolerant by design, since the data is written while the tools run: an unreadable file is skipped, a category may lack
any stat, a stat any of its five numbers. Extra folders (a fixture beside a test asset) are read only for the topics
codex/data does not have yet.
"""
import glob
import json
import os
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "data"))
NUMBERS = ("n", "min", "p25", "median", "p75", "max")


def load(extra=()):
    """{file name: parsed JSON} over codex/data, then any extra folder's files whose names codex/data lacks."""
    found = {}
    for folder in [DATA, *extra]:
        for path in sorted(glob.glob(os.path.join(folder, "*.json"))):
            name = os.path.basename(path)
            if name not in found:
                data = _read(path)
                if data is not None:
                    data["_path"] = path
                    found[name] = data
    return found


def category(key, extra=(), topics=None):
    """The category merged over every file that has it (only the files named in topics, e.g. ('paint',), when given),
    or None: label, stats (first file wins per stat), samples (merged by prefab), references (the first non-empty
    list), sources (the files it came from)."""
    merged = {"key": key, "label": "", "stats": {}, "samples": [], "references": [], "sources": []}
    for name, data in load(extra).items():
        entry = (data.get("categories") or {}).get(key)
        if not isinstance(entry, dict) or (topics and os.path.splitext(name)[0] not in topics):
            continue
        merged["sources"].append(os.path.relpath(data["_path"], os.path.join(HERE, "..")).replace("\\", "/"))
        merged["label"] = merged["label"] or entry.get("label", "")
        for stat, numbers in (entry.get("stats") or {}).items():
            if isinstance(numbers, dict):
                merged["stats"].setdefault(stat, numbers)
        _merge_samples(merged["samples"], entry.get("samples") or [])
        merged["references"] = merged["references"] or list(entry.get("references") or [])
    return merged if merged["sources"] else None


def keys(extra=()):
    """Every category key in the data, sorted."""
    return sorted({k for data in load(extra).values() for k in (data.get("categories") or {})})


def stat(cat, *names):
    """The first of these stats the category has, as a dict of the five numbers (plus n), or None."""
    for name in names:
        numbers = (cat or {}).get("stats", {}).get(name)
        if isinstance(numbers, dict) and any(isinstance(numbers.get(k), (int, float)) for k in NUMBERS[1:]):
            return numbers
    return None


def _merge_samples(into, samples):
    by_prefab = {s.get("prefab") or s.get("name"): s for s in into}
    for sample in samples:
        if not isinstance(sample, dict):
            continue
        key = sample.get("prefab") or sample.get("name")
        if key in by_prefab:
            for field, value in sample.items():
                by_prefab[key].setdefault(field, value)
        else:
            into.append(dict(sample))
            by_prefab[key] = into[-1]


def _read(path, tries=3):
    """The parsed file, or None; a file that does not parse is tried again a second later, since the measuring
    scripts rewrite the data files while the tools may be reading them."""
    for attempt in range(tries):
        try:
            with open(path, encoding="utf-8") as handle:
                data = json.load(handle)
            return data if isinstance(data, dict) else None
        except (OSError, ValueError):
            if attempt + 1 < tries:
                time.sleep(1.0)
    return None

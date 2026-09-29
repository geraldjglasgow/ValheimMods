"""What a new sound aims at, read from the codex (codex/data/sfx.json): an archetype's measured ranges, the game sound
prefab a mod will copy (its clips' loudness and its ZSFX settings), and the game clips nearest to a new clip."""
import functools
import json
import math
import os

from kit import MEASURE

DATA = os.path.normpath(os.path.join(MEASURE, "..", "data", "sfx.json"))
COMPARED = ("length_s", "played_lufs", "momentary_max_lufs", "loudness_lufs", "attack_s", "decay_s", "tail_s",
            "centroid_hz",
            "bandwidth_hz", "flatness", "low", "mid", "high", "pitch_hz", "peak_dbfs", "crest_db")


@functools.lru_cache(maxsize=1)
def data():
    with open(DATA, encoding="utf-8") as handle:
        return json.load(handle)


def archetype(key):
    """The data/sfx.json entry of an archetype ('sfx.creature.idle')."""
    try:
        return data()["categories"][key]
    except KeyError as error:
        raise SystemExit(f"no archetype {key} in {DATA}; see codex/sfx/archetypes.md") from error


def sample(name):
    """A game sound's summary by prefab name ('sfx_greydwarf_idle', 'fx_crit>sfx_crit'), from any archetype."""
    for entry in data()["categories"].values():
        for found in entry["samples"]:
            if found["name"] == name or found["name"].split(">")[0] == name:
                return found
    raise SystemExit(f"no game sound {name} in {DATA}; see codex/sfx/catalogue.md")


def played(key, loop=False):
    """The archetype's median in-game level before distance: each sound's clip level (momentary max, integrated for
    loops) plus the gain its ZSFX volume and AudioSource volume give it."""
    feature = "loudness_lufs" if loop else "momentary_max_lufs"
    levels = [s[feature] + s.get("played_db", 0.0) for s in archetype(key)["samples"] if feature in s]
    return float(sorted(levels)[len(levels) // 2])


def copy_gain_db(prefab, volume=None):
    """The gain a copy of the game prefab gives its clips: its own mean ZSFX volume times its AudioSource volume, or
    the override volume times that AudioSource volume."""
    found = sample(prefab)
    own = ((found.get("vol_min") or 1.0) + (found.get("vol_max") or 1.0)) / 2
    source_db = found.get("played_db", 0.0) - 20 * math.log10(max(own, 1e-3))
    return source_db + 20 * math.log10(max(volume if volume is not None else own, 1e-3))


def clip_target(key, prefab, loop=False, volume=None):
    """The level (LUFS) a new clip should have so that, played through a copy of the game prefab, it sits at the
    archetype's median in-game level: the game's own mix, whatever the copied prefab's volume."""
    return played(key, loop) - copy_gain_db(prefab, volume)


def verdict(value, stats, slack=0.0):
    """'ok' inside the middle half (widened by slack either way), 'near' inside the range, 'low'/'high' outside."""
    if value is None or not stats:
        return "-"
    if stats["p25"] - slack <= value <= stats["p75"] + slack:
        return "ok"
    if stats["min"] <= value <= stats["max"]:
        return "near"
    return "low" if value < stats["min"] else "high"


def report(numbers, key):
    """[(feature, value, p25..p75, min..max, verdict)] of a new clip's numbers against an archetype."""
    stats = archetype(key)["stats"]
    rows = []
    for feature in COMPARED:
        value, spread = numbers.get(feature), stats.get(feature)
        if spread:
            slack = 0.5 if feature.endswith("lufs") else 0.0
            rows.append((feature, value, (spread["p25"], spread["p75"]), (spread["min"], spread["max"]),
                         verdict(value, spread, slack)))
    return rows


def nearest(numbers, key, count=6, clips=None):
    """The archetype's game clips closest to a new clip in length, loudness, brightness and noisiness."""
    def distance(other):
        terms = [(math.log(max(numbers["length_s"], 1e-3) / max(other["length_s"], 1e-3)), 1.0),
                 ((numbers["momentary_max_lufs"] - other["momentary_max_lufs"]) / 6, 1.0),
                 (math.log(max(numbers["centroid_hz"], 1) / max(other["centroid_hz"], 1)), 1.0),
                 ((numbers["flatness"] - other["flatness"]) / 0.1, 0.7)]
        return sum(weight * term * term for term, weight in terms)
    files = [f for s in archetype(key)["samples"] for f in s.get("files", [])]
    known = [(f, clips[f]) for f in dict.fromkeys(files) if f in clips and "length_s" in clips[f]]
    return [f for f, _ in sorted(known, key=lambda item: distance(item[1]))[:count]]

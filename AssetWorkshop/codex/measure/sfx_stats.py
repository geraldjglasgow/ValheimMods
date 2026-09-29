"""The numbers data/sfx.json keeps per archetype: five-number summaries over the clips (length, loudness, timing,
spectrum, pitch) and over the sounds (variation count, random pitch and volume, reach), the settings the sounds share,
a short summary of every sound, and the three to six sounds that sit nearest the middle of the archetype.
"""
import math
from collections import Counter

import numpy as np

import sfx_archetypes

CLIP_FEATURES = ("length_s", "momentary_max_lufs", "loudness_lufs", "peak_dbfs", "crest_db", "attack_s", "decay_s",
                 "tail_s", "body_s", "centroid_hz", "bandwidth_hz", "rolloff_hz", "flatness", "low", "mid", "high",
                 "pitch_hz", "pitch_spread_st", "width")


def five(values):
    """{n, min, p25, median, p75, max} of the values that are numbers, or None when there are none."""
    values = [v for v in values if isinstance(v, (int, float)) and not isinstance(v, bool) and math.isfinite(v)]
    if not values:
        return None
    low, p25, median, p75, high = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), "min": _r(low), "p25": _r(p25), "median": _r(median), "p75": _r(p75), "max": _r(high)}


def _r(value):
    value = float(value)
    return round(value, 3) if abs(value) < 100 else round(value, 1)


def settings(sound):
    """The per-sound settings a new sound copies: variations, random pitch and volume, reach, concurrency, timer."""
    record = sound["record"]
    zsfx, source = record.get("zsfx") or {}, record.get("source") or {}
    low, high = zsfx.get("minPitch") or 1.0, zsfx.get("maxPitch") or 1.0
    return {"clips": len(record["clips"]), "pitch_min": low, "pitch_max": high,
            "pitch_spread_st": round(12 * math.log2(max(high, 1e-3) / max(low, 1e-3)), 2),
            "vol_min": zsfx.get("minVol"), "vol_max": zsfx.get("maxVol"),
            "played_db": round(sfx_archetypes.played_db(record), 2),
            "min_distance": source.get("min_distance"), "max_distance": source.get("max_distance"),
            "half_m": source.get("half_m"), "gone_m": source.get("gone_m"),
            "concurrency": zsfx.get("maxConcurrentSources"), "timeout_s": record.get("timeout_s"),
            "delay_max_s": zsfx.get("maxDelay"), "fade_in_s": zsfx.get("fadeInDuration"),
            "fade_out_s": zsfx.get("fadeOutDuration"), "priority": source.get("priority"),
            "spread_deg": source.get("spread_deg") if isinstance(source.get("spread_deg"), float) else None}


def clip_rows(sound, clips):
    return [clips[c] for c in sound["record"]["clips"] if c in clips and "error" not in clips[c]]


def summary(sound, clips):
    """One sound in a few numbers: its settings and the medians of its clips."""
    record, rows = sound["record"], clip_rows(sound, clips)
    source = record.get("source") or {}
    found = {"prefab": record["prefab"], "name": sfx_archetypes.label(record), "copies": sound["copies"],
             "uses": len(sound["uses"]), "mixer": source.get("mixer", "").replace("Master/", ""),
             "rolloff": source.get("rolloff"), "loop": bool(source.get("loop")),
             "spatial": source.get("spatial") if isinstance(source.get("spatial"), float) else None,
             "creatures": [sfx_archetypes.creature_display(c) for c in sound["creatures"]][:8]}
    found.update({k: v for k, v in settings(sound).items() if v is not None})
    for feature in ("length_s", "momentary_max_lufs", "loudness_lufs", "attack_s", "tail_s", "centroid_hz",
                    "flatness", "low", "high", "pitch_hz"):
        values = [r.get(feature) for r in rows if isinstance(r.get(feature), (int, float))]
        if values:
            found[feature] = _r(np.median(values))
    if "momentary_max_lufs" in found:
        found["played_lufs"] = _r(found["momentary_max_lufs"] + found["played_db"])
    found["files"] = record["clips"]
    return found


def category(key, members, clips, label):
    """The data/sfx.json entry of one archetype."""
    rows = [row for sound in members for row in clip_rows(sound, clips)]
    per_sound = [settings(sound) for sound in members]
    samples = sorted((summary(s, clips) for s in members), key=lambda s: (-s["uses"], s["name"]))
    stats = {f: five(r.get(f) for r in rows) for f in CLIP_FEATURES}
    stats.update({f: five(s[f] for s in per_sound) for f in per_sound[0] if f not in stats})
    stats["played_lufs"] = five(s.get("played_lufs") for s in samples)
    stats["voiced"] = five(r.get("voiced") for r in rows)
    return {"label": label, "sounds": len(members), "clips": len(rows),
            "stats": {k: v for k, v in stats.items() if v},
            "shared": shared(members), "references": references(samples),
            "samples": samples}


def shared(members):
    """How the sounds of an archetype are set up: mixer group, roll-off, looping, spatial blend, random pan, caption."""
    def count(values):
        return dict(Counter(str(v) for v in values).most_common(6))
    records = [s["record"] for s in members]
    sources = [r.get("source") or {} for r in records]
    zsfx = [r.get("zsfx") or {} for r in records]
    return {"mixer": count(s.get("mixer", "").replace("Master/", "") for s in sources),
            "rolloff": count(s.get("rolloff") for s in sources), "loop": count(bool(s.get("loop")) for s in sources),
            "spatial": count(s.get("spatial") if isinstance(s.get("spatial"), float) else "curve" for s in sources),
            "random_pan": count(z.get("randomPan") for z in zsfx if z),
            "distance_reverb": count(z.get("distanceReverb") for z in zsfx if z),
            "caption_type": count(z.get("captionType") for z in zsfx if z),
            "networked": count(r.get("networked") for r in records)}


def references(samples, count=6):
    """The sounds nearest the archetype's middle in length, loudness and brightness, most-used first among equals."""
    usable = [s for s in samples if all(isinstance(s.get(f), (int, float)) for f in ("length_s", "momentary_max_lufs",
                                                                                      "centroid_hz"))]
    if not usable:
        return [s["prefab"] for s in samples[:count]]
    def spread(feature, transform=lambda v: v):
        values = np.array([transform(s[feature]) for s in usable])
        middle, scale = np.median(values), max(np.subtract(*np.percentile(values, [75, 25])), 1e-6)
        return np.abs(values - middle) / scale
    score = spread("length_s", math.log) + spread("momentary_max_lufs") + spread("centroid_hz", math.log)
    score -= 0.1 * np.log1p([s["uses"] for s in usable])
    order, picked, names = np.argsort(score), [], set()
    for index in order:
        name = usable[index]["name"]
        if name not in names:
            picked.append(usable[index]["prefab"] + ("" if ">" not in name else ":" + name.split(">")[1]))
            names.add(name)
        if len(picked) == count:
            break
    return picked

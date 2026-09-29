"""The style check's paint lines: each paint region of the asset against the game's paint family it is painted as
(codex/data/paint.json "families", measured the same way: HSV of the sRGB texels the UVs cover).

A region is compared with the families its materials name (regions.mark(..., family="metal.iron")); a region that
names none is compared with every family whose default region is its name (all "metal" families for "metal"), their
ranges merged. Lines: median value (value_p50), median saturation (saturation), value span (p90 - p10), blotch size
in metres (twice the value's correlation length over the texel density: how big the painted patches are in the world)
and local contrast (the share of the value's spread within a few pixels: fine noise high, soft blotches low).
"""
import numpy as np

import codexdata
import style_rules

LINES = (("albedo_value", "value (median)", "value_p50"), ("albedo_saturation", "saturation (median)", "saturation"),
         ("albedo_contrast", "value span (p90-p10)", "value_span"), ("blotch_m", "blotch size m", "blotch_m"),
         ("local_contrast", "local contrast", "local_contrast"))


def families(extra=()):
    """{family: entry} from paint.json (codex/data, or a fixture folder's while codex/data has none)."""
    data = codexdata.load(extra).get("paint.json") or {}
    found = data.get("families")
    return found if isinstance(found, dict) else {}


def match(region, known):
    """(label, [family names]) the region is judged against; ('', []) when nothing fits."""
    named = [f for f in region.get("families", []) if f in known]
    if named:
        return ", ".join(named), named
    same = sorted(f for f, entry in known.items() if entry.get("region") == region["name"])
    if same:
        return f"every '{region['name']}' family ({', '.join(same)})", same
    return "", []


def merged(known, names):
    """One family's stats, or several merged: n summed, min of the mins, median of the medians, max of the maxes."""
    stats = {}
    for _, _, stat in LINES:
        found = [known[n].get("stats", {}).get(stat) for n in names]
        found = [f for f in found if isinstance(f, dict) and isinstance(f.get("median"), (int, float))]
        if found:
            stats[stat] = {"n": sum(f.get("n", 0) for f in found), "min": min(f.get("min", f["median"]) for f in found),
                           "median": float(np.median([f["median"] for f in found])),
                           "max": max(f.get("max", f["median"]) for f in found)}
    return stats


def region_lines(region, known):
    """(heading, [(label, shown, numbers, verdict, format)]) for one region."""
    label, names = match(region, known)
    head = f"{region['name']:<12}{region['colour']}  {region['share']:>6.1%}  median {region['albedo']}"
    if not names:
        return head + "  (no paint family: name one with regions.mark(..., family=...))", []
    stats = merged(known, names)
    rows = [style_rules.line(region.get(key), text, stats.get(stat), "{:.2f}") for key, text, stat in LINES]
    return head + f"  against {label}", rows

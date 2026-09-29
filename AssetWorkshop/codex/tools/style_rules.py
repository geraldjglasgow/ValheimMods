"""How the style check judges an asset against its codex category: which measurement meets which stat, the verdict
per line, and the game examples nearest to the asset.

A line is PASS when the asset's number lies within the game's range for the category (min to max), LOW below it and
HIGH above it; "--" when the category has no data for it. Stats are looked up under several names, so the data files
can name them their own way (the first name is the one the codex README uses).
"""
import math

import codexdata

# (measurement, label, stat names to look for, number format)
GEOMETRY = (
    ("triangles", "triangles", ("triangles", "tris"), "{:.0f}"),
    ("texture_px", "texture px", ("texture_px", "texture_size", "texture"), "{:.0f}"),
    ("texel_density", "texel density px/m", ("texel_density", "density_px_per_m"), "{:.1f}"),
    ("longest_m", "longest side m", ("longest_m", "length_m", "height_m"), "{:.2f}"),
)
ALBEDO = (   # named as paint.json names a family's stats (its bare "value" is the 10th percentile, so not here)
    ("albedo_value", "albedo value (median)", ("value_p50", "value_median", "albedo_value"), "{:.2f}"),
    ("albedo_saturation", "albedo saturation (median)",
     ("saturation", "saturation_p50", "saturation_median", "albedo_saturation"), "{:.2f}"),
    ("albedo_contrast", "albedo value span (p90-p10)", ("value_span", "contrast", "albedo_contrast"), "{:.2f}"),
)
PAINT_TOPICS = ("paint", "palette", "albedo")
KINDS = {"weapon": "item", "tool": "item", "shield": "item", "armour": "item", "item": "item", "piece": "piece",
         "furniture": "piece", "env": "env", "location": "location", "creature": "creature"}
SAMPLE_FIELDS = ("triangles", "texture_px", "texel_density", "longest_m")


def verdict(value, numbers):
    """PASS, LOW or HIGH against the stat's range (min to max, or p25 to p75 when those are all it has); '--'."""
    if value is None or not numbers:
        return "--"
    low = _first(numbers, "min", "p25")
    high = _first(numbers, "max", "p75")
    if low is not None and value < low:
        return "LOW"
    if high is not None and value > high:
        return "HIGH"
    return "PASS" if low is not None or high is not None else "--"


def lines(measured, cat, paint_cat):
    """[(label, asset text, stat numbers or None, verdict, format)] for every geometry and albedo measurement."""
    rows = []
    for key, label, names, form in GEOMETRY:
        rows.append(line(measured.get(key), label, codexdata.stat(cat, *names), form))
    for key, label, names, form in ALBEDO:
        numbers = codexdata.stat(paint_cat, *names) or codexdata.stat(cat, *names)
        rows.append(line(measured.get(key), label, numbers, form))
    return rows


def paint_category(key, extra=()):
    """The category-level albedo entry that fits best (paint, palette or a fixture's albedo file): the key itself,
    its family, its kind of asset, 'all'."""
    family = key.split(".")[0]
    for candidate in (key, family, KINDS.get(family, family), "all"):
        found = codexdata.category(candidate, extra, topics=PAINT_TOPICS)
        if found:
            return found
    return None


def nearest(measured, samples, count=3):
    """The game samples closest to the asset over triangles, texture size, texel density and longest side
    (squared log ratios over the fields both have); each with its distance."""
    scored = []
    for sample in samples:
        fields = [(measured.get(f), sample_value(sample, f)) for f in SAMPLE_FIELDS]
        pairs = [(a, b) for a, b in fields if _positive(a) and _positive(b)]
        if pairs:
            scored.append((sum(math.log(a / b) ** 2 for a, b in pairs) / len(pairs), sample))
    return sorted(scored, key=lambda item: item[0])[:count]


def sample_value(sample, field):
    if field == "longest_m" and field not in sample and isinstance(sample.get("size_m"), (list, tuple)):
        return max(sample["size_m"]) if sample["size_m"] else None
    value = sample.get(field)
    return value if isinstance(value, (int, float)) else None


def line(value, label, numbers, form):
    """(label, the asset's number as text, the stat's numbers, verdict, number format)."""
    shown = form.format(value) if isinstance(value, (int, float)) else "--"
    return label, shown, numbers, verdict(value, numbers), form


def _first(numbers, *names):
    return next((numbers[n] for n in names if isinstance(numbers.get(n), (int, float))), None)


def _positive(value):
    return isinstance(value, (int, float)) and value > 0

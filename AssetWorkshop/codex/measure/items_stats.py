"""Per-sample rows and per-category summaries for data/items.json: the five-number stats the codex format asks for,
the attach frame's numbers for held items, the median outline, and which effects a category's items use.

    items_stats.sample(record, category)     # the row data/items.json keeps for one item
    items_stats.summary(rows)                # {"triangles": {n, min, p25, median, p75, max}, ...}
"""
import collections

import numpy as np

import items_classify

WORN_SCALE = 0.95       # the Player's Visual transform: worn (skinned) parts are drawn at 0.95 of their own size
EFFECT_SLOTS = ("hitEffect", "hitTerrainEffect", "blockEffect", "startEffect", "holdStartEffect", "equipEffect",
                "triggerEffect", "trailStartEffect")


def model_of(record):
    """The model a player sees most: held, else worn, else the dropped one."""
    return record["held"] or record["worn"] or record["drop"]


def sample(record, category):
    """The row kept for one item: identity, budget, size in the game, shader and maps, attach frame, physics."""
    model = model_of(record)
    main = model["materials"][0] if model["materials"] else {}
    scale = WORN_SCALE if model is record["worn"] and model["skinned"] else 1.0
    row = {"prefab": record["prefab"], "name": record["name"], "tier": items_classify.tier(record),
           "triangles": model["triangles"], "texture_px": model["texture_px"] or None,
           "texel_density": model["texel_density"], "size_m": [round(s * scale, 3) for s in model["shape"]["size"]],
           "shader": main.get("shader"), "maps": sorted(main.get("maps", {})),
           "materials": len(model["materials"]), "submeshes": model["submeshes"], "renderers": model["renderers"]}
    if record["held"]:
        row["attach"] = _attach(record["held"]["shape"])
    if record["trail"]:
        row["trail"] = {k: record["trail"][k] for k in ("base", "tip", "life_s", "material")}
    if category.startswith("item.") and category != "item.fish":
        row["drop"] = _drop(record)
    if category == "item.material":
        row["family"] = items_classify.material_family(record["name"])
    return row


def _attach(shape):
    return {k: shape[k] for k in ("long_axis", "fist_at", "width_axis", "thin_axis", "head_side", "tilt_deg",
                                  "box_min", "box_max")}


def _drop(record):
    """How the item lies on the ground: its colliders and Rigidbody."""
    body = record["rigidbody"] or {}
    return {"colliders": sorted({c["kind"] for c in record["colliders"]}), "mass": body.get("mass")}


def five(values):
    values = [v for v in values if v is not None]
    if not values:
        return None
    q = np.percentile(values, [0, 25, 50, 75, 100])
    rounded = [round(float(v), 3 if max(values) < 10 else 1) for v in q]
    return dict(zip(("min", "p25", "median", "p75", "max"), rounded), n=len(values))


def summary(rows):
    """Stats over a category's rows (variants of the same model left out by the caller)."""
    stats = {"triangles": five([r["triangles"] for r in rows]),
             "texture_px": five([r["texture_px"] for r in rows]),
             "texel_density": five([r["texel_density"] for r in rows]),
             "longest_m": five([max(r["size_m"]) for r in rows])}
    held = [r["attach"] for r in rows if "attach" in r]
    if held:
        stats["fist_at"] = five([a["fist_at"] for a in held])
        stats["z_max_m"] = five([a["box_max"][2] for a in held])
        stats["z_min_m"] = five([a["box_min"][2] for a in held])
        stats["x_max_m"] = five([a["box_max"][0] for a in held])
        stats["x_min_m"] = five([a["box_min"][0] for a in held])
        stats["tilt_deg"] = five([a["tilt_deg"] for a in held])
    masses = [r["drop"]["mass"] for r in rows if r.get("drop") and r["drop"]["mass"]]
    if masses:
        stats["mass_kg"] = five(masses)
    return {k: v for k, v in stats.items() if v}


def by_tier(rows):
    """Median triangles and texture size, and the shaders used, of the given rows grouped by tier."""
    found = {}
    for tier in sorted({r["tier"] for r in rows if r["tier"] is not None}):
        picked = [r for r in rows if r["tier"] == tier]
        found[str(tier)] = {"n": len(picked), "triangles": five([r["triangles"] for r in picked]),
                            "texture_px": five([r["texture_px"] for r in picked]),
                            "shaders": dict(collections.Counter(r["shader"] for r in picked).most_common())}
    return found


def tally(records, key):
    """How often each value occurs among the records ({value: count}, most common first)."""
    counts = collections.Counter(v for r in records for v in key(r))
    return dict(counts.most_common())


def effects(records):
    """For each effect slot the prefabs the category's items play there, with how many items use each."""
    found = {}
    for slot in EFFECT_SLOTS:
        counts = tally(records, lambda r, s=slot: r["shared"]["effects"].get(s, []) +
                       r["shared"]["attack"].get("effects", {}).get(s, []))
        if counts:
            found[slot] = counts
    projectiles = tally(records, lambda r: [p for p in (r["shared"]["attack"].get("projectile"),
                                                        r["shared"]["secondary"].get("projectile")) if p])
    if projectiles:
        found["projectile"] = projectiles
    return found


def outline(records):
    """The median cross-section along the long axis of the held models: [at, width m, thickness m, centre m]."""
    profiles = [r["held"]["profile"] for r in records if r["held"] and r["held"].get("profile")]
    if not profiles:
        return None
    table = np.array(profiles)
    median = np.median(table, axis=0)
    return [[round(float(v), 3) for v in row] for row in median]

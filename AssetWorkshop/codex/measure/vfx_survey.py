"""Measures the game's effects for the codex and writes codex/data/vfx.json: one category per kind of effect
(vfx.hit_material, vfx.fire, ...), one role per kind of particle system (flame, smoke, spark, ...), the textures, the
shaders and their settings, the lights and the game scripts around them.

    python codex/measure/vfx_corpus.py      read the export once (out/vfx/effects.json)
    python codex/measure/vfx_survey.py      measure, write data/vfx.json and the texture sheets

Every range is {n, min, p25, median, p75, max}. Sizes and speeds are the systems' own numbers (metres, metres per
second) before any transform scale; lifetimes and durations are seconds.
"""
import collections
import datetime
import json
import math
import os

import vfx_classify
import vfx_corpus
import vfx_effect
import vfx_read
import vfx_textures

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "data", "vfx.json")
MODULES = ("sheet", "velocity", "limit", "force", "noise", "colour_life", "size_life", "rotation_life", "collision",
           "sub_emitters", "trail", "light_module", "custom", "inherit_velocity")


def five(values):
    """{n, min, p25, median, p75, max} of the finite numbers, or None."""
    values = sorted(v for v in values if isinstance(v, (int, float)) and math.isfinite(v) and abs(v) < 1e5)
    if not values:
        return None
    def at(q):
        i = q * (len(values) - 1)
        low = int(math.floor(i))
        high = min(low + 1, len(values) - 1)
        return round(values[low] + (values[high] - values[low]) * (i - low), 3)
    return {"n": len(values), "min": round(values[0], 3), "p25": at(0.25), "median": at(0.5), "p75": at(0.75),
            "max": round(values[-1], 3)}


def shares(counter, total, top=12):
    return {k: round(v / total, 3) for k, v in counter.most_common(top)} if total else {}


def material(s):
    return (s.get("renderer") or {}).get("material") or {}


def lifetime(s):
    return vfx_read.mean(s.get("lifetime"))


def colour_of(s):
    """A representative start colour (r, g, b, a): the middle of two colours, a gradient's middle, or the colour."""
    c = s.get("colour") or {}
    if "colour" in c:
        return c["colour"]
    if "colours" in c:
        return [round((a + b) / 2, 3) for a, b in zip(*c["colours"])]
    g = c.get("gradient") or c.get("random") or (c.get("gradients") or [None])[-1]
    return [round(v, 3) for v in vfx_read.gradient_at(g, 0.5)] if g else None


def alpha_curve(s, times=(0, 0.1, 0.25, 0.5, 0.75, 0.9, 1)):
    g = (s.get("colour_life") or {}).get("gradient")
    return [round(vfx_read.gradient_at(g, t)[3], 3) for t in times] if g else None


def size_curve(s, times=(0, 0.1, 0.25, 0.5, 0.75, 0.9, 1)):
    curve = (s.get("size_life") or {}).get("curve")
    if not curve:
        return None
    peak = max(abs(v) for _, v in curve) or 1.0
    keys = [(t, v / peak, 0.0, 0.0) for t, v in curve]
    return [round(vfx_read.evaluate(keys, t), 3) for t in times]


def median_rows(rows):
    rows = [r for r in rows if r]
    if not rows:
        return None
    return [round(sorted(col)[len(col) // 2], 3) for col in zip(*rows)]


# ---------------------------------------------------------------- effects

def effect_numbers(fx):
    systems = vfx_effect.all_systems(fx)
    bursts, rates = zip(*[vfx_classify.particles(s) for s in systems]) if systems else ((), ())
    scripts = vfx_effect.all_of(fx, "scripts")
    timeouts = [sc["settings"].get("m_timeout") for sc in scripts if sc["class"] == "TimedDestruction"]
    lights = vfx_effect.all_of(fx, "lights")
    return {"prefab": fx["prefab"] + (f"#{fx['subtree']}" if fx.get("subtree") else ""), "name": fx["name"],
            "systems": len(systems), "burst_particles": round(sum(bursts)), "rate": round(sum(rates), 1),
            "duration_s": max((vfx_read.number(s["duration"]) for s in systems), default=None),
            "loop": any(s["loop"] for s in systems), "lights": len(lights),
            "light_range_m": max((l["range"] for l in lights if l["range"]), default=None),
            "timeout_s": timeouts[0] if timeouts else None,
            "roles": sorted({vfx_classify.role(s) for s in systems}),
            "scripts": sorted({sc["class"] for sc in scripts})}


def category_entry(key, effects):
    rows = [effect_numbers(fx) for fx in effects]
    systems = [s for fx in effects for s in vfx_effect.all_systems(fx)]
    lights = [l for fx in effects for l in vfx_effect.all_of(fx, "lights")]
    n = len(effects)
    roles = collections.Counter(r for row in rows for r in row["roles"])
    scripts = collections.Counter(sc for row in rows for sc in row["scripts"])
    return {"label": vfx_classify.LABELS.get(key, key), "samples": rows,
            "stats": {"systems": five(r["systems"] for r in rows),
                      "burst_particles": five(r["burst_particles"] for r in rows if r["burst_particles"]),
                      "rate": five(r["rate"] for r in rows if r["rate"]),
                      "duration_s": five(r["duration_s"] for r in rows),
                      "timeout_s": five(r["timeout_s"] for r in rows),
                      "lifetime_s": five(lifetime(s) for s in systems),
                      "size_m": five(vfx_read.mean(s["size"]) for s in systems),
                      "speed_ms": five(vfx_read.mean(s["speed"]) for s in systems),
                      "gravity": five(vfx_read.mean(s["gravity"]) for s in systems),
                      "light_range_m": five(l["range"] for l in lights),
                      "light_intensity": five(l["intensity"] for l in lights)},
            "shares": {"loop": round(sum(r["loop"] for r in rows) / n, 3),
                       "with_light": round(sum(1 for r in rows if r["lights"]) / n, 3),
                       "roles": shares(roles, n, 20), "scripts": shares(scripts, n, 12),
                       **system_shares(systems)},
            "references": references(rows)}


def system_shares(systems):
    total = len(systems)
    count = lambda f: collections.Counter(f(s) for s in systems)
    modules = collections.Counter(m for s in systems for m in MODULES if s.get(m))
    return {"render": shares(count(lambda s: (s.get("renderer") or {}).get("mode", "none")), total),
            "shader": shares(count(lambda s: material(s).get("shader", "none")), total),
            "blend": shares(count(lambda s: material(s).get("blend", "none")), total),
            "space": shares(count(lambda s: s["space"]), total),
            "scaling": shares(count(lambda s: s["scaling"]), total),
            "modules": shares(modules, total, 20)}


def references(rows, count=6):
    """Up to six effects that show the category: near its median system count, standard names, varied."""
    if not rows:
        return []
    middle = sorted(r["systems"] for r in rows)[len(rows) // 2]
    ranked = sorted(rows, key=lambda r: (abs(r["systems"] - middle) + (0 if r["name"].lower().startswith(("vfx_", "fx_"))
                                                                      else 2), r["name"]))
    picked, stems = [], set()
    for r in ranked:
        stem = r["name"].lower().split("_")[1] if "_" in r["name"] else r["name"].lower()
        if stem not in stems:
            picked.append(r["prefab"])
            stems.add(stem)
        if len(picked) == count:
            break
    return picked


# ---------------------------------------------------------------- roles

def role_entry(role, items):
    """Numbers over every system of one role; items are (effect name, system)."""
    systems = [s for _, s in items]
    total = len(systems)
    return {"label": vfx_classify.ROLES[role], "systems": total, "stats": _role_stats(systems),
            "shares": _role_shares(systems), "colour": _role_colour(systems),
            "examples": [f"{name}: {s['node']}" for name, s in items[:: max(1, total // 8)][:8]]}


def _role_stats(systems):
    stretched = [s["renderer"] for s in systems if (s.get("renderer") or {}).get("mode") == "stretched"]
    return {"lifetime_s": five(lifetime(s) for s in systems),
            "size_m": five(vfx_read.mean(s["size"]) for s in systems),
            "speed_ms": five(vfx_read.mean(s["speed"]) for s in systems),
            "gravity": five(vfx_read.mean(s["gravity"]) for s in systems),
            "burst_particles": five(vfx_classify.particles(s)[0] for s in systems if vfx_classify.particles(s)[0]),
            "rate": five(vfx_classify.particles(s)[1] for s in systems if vfx_classify.particles(s)[1]),
            "max_particles": five(s["max_particles"] for s in systems),
            "duration_s": five(vfx_read.number(s["duration"]) for s in systems),
            "rotation_speed_deg": five(vfx_read.mean(s.get("rotation_life")) for s in systems if s.get("rotation_life")),
            "noise_strength": five(vfx_read.mean(s["noise"]["strength"]) for s in systems if s.get("noise")),
            "noise_frequency": five(s["noise"]["frequency"] for s in systems if s.get("noise")),
            "drag": five(vfx_read.mean(s["limit"]["drag"]) for s in systems if s.get("limit")),
            "length_scale": five(r.get("length_scale") for r in stretched),
            "speed_scale": five(r.get("speed_scale") for r in stretched)}


def _role_shares(systems):
    total = len(systems)
    textures = collections.Counter((material(s).get("texture") or "none").rsplit("/", 1)[-1] for s in systems)
    tiles = collections.Counter(tuple(s["sheet"]["tiles"]) for s in systems if s.get("sheet"))
    shapes = collections.Counter((s.get("shape") or {}).get("type", "none") for s in systems)
    return {**system_shares(systems), "texture": shares(textures, total, 10),
            "sheet_tiles": {f"{a}x{b}": round(v / total, 3) for (a, b), v in tiles.most_common(6)},
            "loop": round(sum(1 for s in systems if s["loop"]) / total, 3), "shape": shares(shapes, total, 8)}


def _role_colour(systems):
    additive = [s for s in systems if material(s).get("blend", "").startswith("additive")]
    return {"start_rgba_median": median_rows(colour_of(s) for s in systems),
            "start_rgba_additive_median": median_rows(colour_of(s) for s in additive),
            "alpha_over_life_median": median_rows(alpha_curve(s) for s in systems),
            "size_over_life_median": median_rows(size_curve(s) for s in systems),
            "curve_times": [0, 0.1, 0.25, 0.5, 0.75, 0.9, 1]}


# ---------------------------------------------------------------- lights, scripts, shaders

def lights_entry(effects):
    by_category = collections.defaultdict(list)
    for fx in effects:
        for light in vfx_effect.all_of(fx, "lights"):
            by_category[vfx_classify.category(fx)].append((fx["name"], light))
    everything = [l for rows in by_category.values() for _, l in rows]
    return {"all": {"n": len(everything), "range_m": five(l["range"] for l in everything),
                    "intensity": five(l["intensity"] for l in everything),
                    "types": dict(collections.Counter(l["type"] for l in everything)),
                    "shadows": dict(collections.Counter(l["shadows"] for l in everything))},
            "by_category": {k: {"n": len(v), "range_m": five(l["range"] for _, l in v),
                                "intensity": five(l["intensity"] for _, l in v),
                                "examples": [f"{n}: range {l['range']} m, intensity {l['intensity']}, colour {l['colour']}"
                                             for n, l in v[:: max(1, len(v) // 5)][:5]]}
                            for k, v in sorted(by_category.items(), key=lambda kv: -len(kv[1]))}}


def scripts_entry(effects):
    found = collections.defaultdict(list)
    for fx in effects:
        for sc in vfx_effect.all_of(fx, "scripts"):
            found[sc["class"]].append((fx["name"], sc["settings"]))
    out = {}
    for cls, rows in sorted(found.items(), key=lambda kv: -len(kv[1])):
        numeric = collections.defaultdict(list)
        for _, settings in rows:
            for k, v in settings.items():
                if isinstance(v, float):
                    numeric[k].append(v)
        out[cls] = {"effects": len(rows), "settings": {k: five(v) for k, v in numeric.items() if len(set(v)) > 1},
                    "constant": {k: v[0] for k, v in numeric.items() if len(set(v)) == 1},
                    "examples": [n for n, _ in rows[:: max(1, len(rows) // 6)][:6]]}
    return out


def shaders_entry(effects):
    """Per shader: systems, blends, render modes, vertex streams and softness settings used with it, and materials."""
    rows = collections.defaultdict(list)
    for fx in effects:
        for s in vfx_effect.all_systems(fx):
            m = material(s)
            if m:
                rows[m["shader"]].append((s, m))
    out = {}
    for shader, items in sorted(rows.items(), key=lambda kv: -len(kv[1])):
        streams = collections.Counter(" ".join(s["renderer"]["streams"] or ["default"]) for s, _ in items)
        out[shader] = {"systems": len(items),
                       "blend": dict(collections.Counter(m["blend"] for _, m in items).most_common(6)),
                       "render": dict(collections.Counter(s["renderer"]["mode"] for s, _ in items).most_common(6)),
                       "streams": dict(streams.most_common(5)),
                       "soft": [json.dumps(x) for x, _ in collections.Counter(json.dumps(m.get("soft")) for _, m in items).most_common(4)],
                       "camera_fade": [x for x, _ in collections.Counter(json.dumps(m.get("camera_fade")) for _, m in items).most_common(4)],
                       "fog": dict(collections.Counter(str(m.get("fog")) for _, m in items)),
                       "materials": [n for n, _ in collections.Counter(m["name"] for _, m in items).most_common(12)]}
    return out


def main():
    effects = vfx_corpus.load()
    groups = collections.defaultdict(list)
    for fx in effects:
        groups[vfx_classify.category(fx)].append(fx)
    by_role = collections.defaultdict(list)
    for fx in effects:
        for s in vfx_effect.all_systems(fx):
            by_role[vfx_classify.role(s)].append((fx["name"], s))
    data = {"topic": "vfx", "generated": datetime.date.today().isoformat(), "script": "measure/vfx_survey.py",
            "corpus": {"effects": len(effects), "systems": sum(len(v) for v in by_role.values())},
            "categories": {k: category_entry(k, v) for k, v in sorted(groups.items(), key=lambda kv: -len(kv[1]))},
            "roles": {k: role_entry(k, v) for k, v in sorted(by_role.items(), key=lambda kv: -len(kv[1]))},
            "textures": vfx_textures.main(), "shaders": shaders_entry(effects),
            "lights": lights_entry(effects), "scripts": scripts_entry(effects)}
    os.makedirs(os.path.dirname(DATA), exist_ok=True)
    with open(DATA, "w", encoding="utf-8") as handle:
        json.dump(vfx_corpus.clean(data), handle, indent=1)
    print(f"{len(effects)} effects in {len(groups)} categories, {len(by_role)} roles -> {os.path.relpath(DATA)}")


if __name__ == "__main__":
    main()

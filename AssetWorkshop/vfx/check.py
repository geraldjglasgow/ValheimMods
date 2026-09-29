"""The effect style check: an effect's spec against the game's own numbers (codex/data/vfx.json). Each particle system
is classified into a role the way the survey classifies the game's (flame, smoke, spark, ...) and its lifetime, size,
speed, gravity and counts compared with that role's range; the effect as a whole with its category (systems, particles,
light, removal time). Writes <effect>/out/style_report.txt, one line per check: PASS, or WARN with the game's range.

    python vfx/check.py <effect>          (vfx/build.py runs it on every build)

A value passes inside half the game's 25th percentile to twice its 75th: the game's own spread is wide, and the check
is for numbers that are off by a factor, not for taste. The preview beside the game's effect is the real test.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "codex", "measure"))

import vfx_classify  # noqa: E402
import vfx_read  # noqa: E402

DATA = os.path.join(HERE, "..", "codex", "data", "vfx.json")
SHADER_NAMES = {"Custom/Gradient Mapped Particle (Unlit)": "ParticleGradientMapped_Unlit",
                "Custom/Particle (Unlit)": "ParticleUnlit", "Lux Lit Particles/ Bumped": "Lux Lit Particles Bumped",
                "Custom/LitParticles": "LitParticles", "Particles/Standard Unlit2": "Particle Standard Unlit",
                "Particles/Standard Surface2": "Particle Standard Surface", "Custom/ParticleDecal": "ParticleDecal",
                "Legacy Shaders/Particles/Additive": "Legacy Particles Additive",
                "Legacy Shaders/Particles/Alpha Blended": "Legacy Particles Alpha Blended", "Standard": "Standard"}


def summary_curve(c):
    """The expanded spec's curve in the survey's words."""
    if c["mode"] == "const":
        return {"const": c["c"]}
    if c["mode"] == "range":
        return {"min": c["min"], "max": c["max"]}
    return {"curve": [[t, v] for t, v in zip(c["t"], c["v"])]}


def as_measured(s, materials, textures):
    """An expanded spec system in the shape vfx_classify reads."""
    m = materials.get(s["renderer"]["material"])
    material = None
    if m:
        texture = next((t for t in textures if t["name"] == m["texture"]), None)
        material = {"name": texture["make"] if texture else "", "texture": texture["make"] + ".png" if texture else None,
                    "shader": SHADER_NAMES.get(m["game_shader"], m["game_shader"]), "blend": m["blend"]}
    colour = s["colour"]
    return {"lifetime": summary_curve(s["lifetime"]), "size": summary_curve(s["size"]), "speed": summary_curve(s["speed"]),
            "gravity": summary_curve(s["gravity"]), "max_particles": s["max_particles"], "loop": s["loop"],
            "emission": {"rate": summary_curve(s["emission"]["rate"]),
                         "bursts": [{"count": summary_curve(b["count"]), "cycles": b["cycles"]} for b in s["emission"]["bursts"]]},
            "colour": {"colour": colour["a"]} if colour["mode"] == "colour" else {"colours": [colour["a"], colour["b"]]},
            "renderer": {"mode": s["renderer"]["mode"], "material": material},
            "light_module": None, "trail": s["trail"]["enabled"]}


def judge(label, value, stats):
    if value is None or not stats:
        return None
    low, high = stats["p25"] * 0.5 if stats["p25"] > 0 else stats["min"], stats["p75"] * 2 if stats["p75"] > 0 else stats["max"]
    ok = low - 1e-6 <= value <= high + 1e-6
    return f"{'PASS' if ok else 'WARN'} {label}: {value:g} (game {stats['p25']:g} to {stats['p75']:g}, median {stats['median']:g})"


def system_lines(s, data, materials, textures):
    measured = as_measured(s, materials, textures)
    role = s.get("role") or vfx_classify.role(measured)
    stats = data["roles"].get("glow" if role == "flash" else role, {}).get("stats", {})
    lines = [f"system {s['name']}: role {role}" + ("" if s.get("role") else " (classified; set 'role' in the spec)")]
    single = s["max_particles"] <= 3 or (vfx_classify.particles(measured)[0] <= 3 and not vfx_classify.particles(measured)[1])
    for label, key, value in (("lifetime s", "lifetime_s", None if single else vfx_read.mean(measured["lifetime"])),
                              ("start size m", "size_m", vfx_read.mean(measured["size"])),
                              ("speed m/s", "speed_ms", vfx_read.mean(measured["speed"])),
                              ("particles a burst", "burst_particles", None if single else vfx_classify.particles(measured)[0] or None),
                              ("per second", "rate", vfx_classify.particles(measured)[1] or None)):
        line = judge(f"  {label}", value, stats.get(key))
        if line:
            lines.append(line)
    return lines


def effect_lines(spec, data, category):
    entry = data["categories"].get(category)
    if not entry:
        return [f"WARN category {category or '(none)'} is not in codex/data/vfx.json: set EFFECT['category']"]
    st = entry["stats"]
    lines = [f"category {category}: {entry['label']}", judge("systems", len(spec["systems"]), st["systems"])]
    if spec["lights"]:
        lines.append(judge("light range m", spec["lights"][0]["range"], st.get("light_range_m")))
        lines.append(judge("light intensity", spec["lights"][0]["intensity"], st.get("light_intensity")))
    if spec["timeout"] > 0:
        lines.append(judge("removed after s", spec["timeout"], st.get("timeout_s")))
    return [line for line in lines if line]


def report(out_folder):
    with open(os.path.join(out_folder, "effect.json"), encoding="utf-8") as handle:
        spec = json.load(handle)
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    materials = {m["name"]: m for m in spec["materials"]}
    lines = effect_lines(spec, data, spec.get("category", ""))
    for s in spec["systems"]:
        lines += system_lines(s, data, materials, spec["textures"])
    text = "\n".join(lines) + "\n"
    with open(os.path.join(out_folder, "style_report.txt"), "w", encoding="utf-8") as handle:
        handle.write(text)
    return text


if __name__ == "__main__":
    name = sys.argv[1]
    folder = name if os.path.isdir(name) else os.path.join(HERE, "effects", name)
    print(report(os.path.join(folder, "out")))

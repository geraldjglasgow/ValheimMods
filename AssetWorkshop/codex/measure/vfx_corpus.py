"""Which of the game's prefabs the vfx pages are measured on, and every one of them read once into
codex/out/vfx/effects.json (vfx_effect's words), so the survey, the texture sheets and the catalogue share one read.

    python codex/measure/vfx_corpus.py          reads the export, writes out/vfx/effects.json (about a minute)

The corpus: every fx_* and vfx_* prefab; everything under Effects/ (weather, fireflies, flies, mist); the weather
systems of Systems/_Environment.prefab, one effect per group; the projectiles of creatures and staffs; the fire pieces
(campfire, hearth, bonfire, braziers, torches); and the weapons and tools that carry particles (their glows and
flames). A prefab is kept when it has a particle system, a light, a trail or a camera shake.
"""
import json
import math
import os
import sys
import time

import game
import vfx_effect

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "out", "vfx")
CACHE = os.path.join(OUT, "effects.json")
ENVIRONMENT = "Systems/_Environment.prefab"
FIRE_PIECES = ("fire_pit", "fire_pit_iron", "hearth", "bonfire", "piece_brazierceiling01", "piece_brazierfloor01",
               "piece_brazierfloor02", "piece_groundtorch", "piece_groundtorch_blue", "piece_groundtorch_green",
               "piece_groundtorch_mist", "piece_groundtorch_wood", "piece_walltorch", "piece_oven", "smelter",
               "charcoal_kiln", "forge", "blastfurnace", "eitrrefinery", "piece_bathtub",
               "Candle_resin", "CastleKit_braided_box01", "piece_Lavalantern", "piece_dvergr_lantern",
               "piece_dvergr_lantern_pole", "MountainKit_brazier", "MountainKit_brazier_blue", "MountainKit_brazier_purple",
               "piece_brazierfloor01")


def corpus():
    """[(path, subtree or None, group)] of every effect the codex measures."""
    named = game.find("**/fx_*.prefab") + game.find("**/vfx_*.prefab") + game.find("Effects/**/*.prefab")
    named += [p for p in game.find("Characters/**/*projectile*.prefab") + game.find("GameElements/**/*projectile*.prefab")]
    named += [p for p in game.find("GameElements/Pieces/**/*.prefab") if p.rsplit("/", 1)[-1][:-7] in FIRE_PIECES]
    named += game.find("GameElements/Items/weapons/*.prefab") + game.find("GameElements/Items/tools/*.prefab")
    seen, out = set(), []
    for path in named:
        if path not in seen:
            seen.add(path)
            out.append((path, None, _group(path)))
    return out + [(ENVIRONMENT, sub, "weather") for sub in weather_groups()]


def _group(path):
    name = path.rsplit("/", 1)[-1].lower()
    if name.startswith(("fx_", "vfx_")):
        return "effect"
    if "projectile" in name:
        return "projectile"
    if path.startswith("GameElements/Pieces"):
        return "piece"
    if path.startswith("GameElements/Items"):
        return "item"
    return "effect"


def weather_groups():
    """The weather and ambient groups of the environment prefab: each child of FollowPlayer, and Rain, Distant fog
    planes and OceanMist."""
    p = game.prefab(ENVIRONMENT)
    root = p.root()
    groups = []
    for child in p.children(root):
        name = p.name(child)
        if name == "FollowPlayer":
            groups += [p.path_to(c) for c in p.children(child)]
        elif any(k == "ParticleSystem" for n in _subtree(p, child) for k, _ in p.components(n)):
            groups.append(p.path_to(child))
    return groups


def _subtree(p, node):
    out, stack = [], [node]
    while stack:
        n = stack.pop()
        out.append(n)
        stack.extend(p.children(n))
    return out


def read_all(verbose=True):
    effects, started = [], time.time()
    for i, (path, sub, group) in enumerate(corpus()):
        try:
            fx = vfx_effect.effect_subtree(path, sub) if sub else vfx_effect.effect(path)
        except Exception as error:  # noqa: BLE001  (one odd prefab must not stop the survey)
            print(f"skip {path} {sub or ''}: {error}", file=sys.stderr)
            continue
        if keep(fx, group):
            fx["group"] = group
            effects.append(fx)
        if verbose and i % 200 == 0:
            print(f"{i} read, {len(effects)} kept, {time.time() - started:.0f} s")
    return effects


def keep(fx, group):
    systems = vfx_effect.all_systems(fx)
    shakes = [s for s in vfx_effect.all_of(fx, "scripts") if s["class"] == "CamShaker"]
    if group == "item":
        return any("attach" in s["node"] for s in systems)
    return bool(systems or vfx_effect.all_of(fx, "lights") or vfx_effect.all_of(fx, "trails") or shakes)


def clean(value):
    """JSON-safe: infinities as the string 'inf'."""
    if isinstance(value, float) and not math.isfinite(value):
        return "inf" if value > 0 else "-inf"
    if isinstance(value, dict):
        return {k: clean(v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return [clean(v) for v in value]
    return value


def load():
    """The cached corpus read (run this script first)."""
    with open(CACHE, encoding="utf-8") as handle:
        return json.load(handle)


def main():
    effects = read_all()
    os.makedirs(OUT, exist_ok=True)
    with open(CACHE, "w", encoding="utf-8") as handle:
        json.dump(clean(effects), handle)
    systems = sum(len(vfx_effect.all_systems(fx)) for fx in effects)
    print(f"{len(effects)} effects, {systems} particle systems -> {os.path.relpath(CACHE)}")


if __name__ == "__main__":
    main()

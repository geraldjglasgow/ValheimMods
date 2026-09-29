"""Checks the workshop's paint recipes (blender/workshop/paint.py) against the game: builds the paint_swatches asset
once per material family through the pipeline (its atlas sized to the family's measured texel density), measures the
bake the way codex/measure measures the game's textures, and writes a report and side-by-side sheets.

    python codex/tools/paint_check.py                    every family (about two minutes)
    python codex/tools/paint_check.py wood.planks iron   some families (keys, or the part after the dot)
    python codex/tools/paint_check.py --lineup           also the all-families preview sheet

Writes codex/out/paint/check/: <family>_albedo.png, _normal.png, .blend; paint_check.json (every number);
paint_check.md (the table); paint_check_<n>.png (sheets: swatch beside three game samples, 4x, point filtered).
"""
import json
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(HERE, "..", "measure"))
sys.path.insert(0, HERE)

import paint_aggregate  # noqa: E402
import paint_check_sheet  # noqa: E402
import paint_check_stats  # noqa: E402

sys.path.insert(0, os.path.join(WORKSHOP, "blender", "workshop"))
import paint_normal  # noqa: E402  (plain numpy, no bpy at import)
import paint_specs  # noqa: E402

ASSET = os.path.join(HERE, "paint_swatches")
OUT = os.path.join(WORKSHOP, "codex", "out", "paint", "check")
PAINT = os.path.join(WORKSHOP, "codex", "data", "paint.json")
BLENDER = os.environ.get("WORKSHOP_BLENDER", os.path.join(os.environ.get("USERPROFILE", ""), "tools", "blender",
                                                          "blender.exe"))


def families(args):
    """Family keys from the arguments (keys, or the part after the dot: 'iron'), or all of them."""
    with open(PAINT, encoding="utf-8") as handle:
        known = json.load(handle)["families"]
    names = {f.split(".")[1]: f for f in known}
    picked = [a if a in known else names.get(a) for a in args if not a.startswith("--")]
    return [p for p in picked if p] or list(known)


def build(family):
    """Runs the pipeline for one family and copies its outputs into OUT; returns the .blend path."""
    env = dict(os.environ, PAINT_SWATCH=family)
    command = [BLENDER, "--background", "--factory-startup", "--python-exit-code", "1", "--python",
               os.path.join(WORKSHOP, "blender", "run.py"), "--", "--asset", ASSET, "--no-preview"]
    result = subprocess.run(command, env=env, capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit(f"{family}: pipeline failed\n{result.stdout[-3000:]}\n{result.stderr[-2000:]}")
    stem = os.path.join(OUT, family.replace(".", "_"))
    for suffix in ("_albedo.png", "_normal.png", ".blend"):
        shutil.copyfile(os.path.join(ASSET, "out", "paint_swatches" + suffix), stem + suffix)
    paint_normal.png_from_albedo(stem + "_albedo.png", stem + "_normal_hooked.png", _strength(family))
    return stem + ".blend"


def _strength(family):
    """The family's measured normal strength, as the recipes use it (paint_specs)."""
    return paint_specs.spec(family)["relief"]


def uv_data(blends):
    """{blend path: {triangles, area}} from one Blender run of paint_check_uv.py."""
    target = os.path.join(OUT, "uv.json")
    command = [BLENDER, "--background", "--factory-startup", "--python-exit-code", "1", "--python",
               os.path.join(HERE, "paint_check_uv.py"), "--", target] + blends
    subprocess.run(command, capture_output=True, text=True, check=True)
    with open(target, encoding="utf-8") as handle:
        return json.load(handle)


def check(family, uv, game):
    stem = os.path.join(OUT, family.replace(".", "_"))
    swatch = paint_check_stats.measure(stem + "_albedo.png", stem + "_normal.png", uv)
    return swatch, paint_check_stats.compare(swatch, target(family, game))


def target(family, game):
    """The game's numbers the swatch should match: the family's, or those of the samples its recipe takes its tones
    from (paint_specs tones_from), measured the same way."""
    entry = game["families"][family]
    chosen = paint_specs.FAMILIES[family].get("tones_from")
    records = [r for r in entry["samples"] if chosen and os.path.basename(r["texture"]) in chosen]
    return paint_aggregate.family_stats(records) if records else entry["stats"]


def report(results):
    """The markdown table: per family, each check's swatch value, the game's median and range, and the verdict."""
    lines = ["# Paint check", "", "| family | check | swatch | game median | game range | verdict |",
             "| --- | --- | --- | --- | --- | --- |"]
    for family, (swatch, rows) in results.items():
        for key, mine, median, low, high, verdict in rows:
            spread = f"{low:.3f} to {high:.3f}" if low is not None else ""
            lines.append(f"| {family} | {key} | {mine:.3f} | {median:.3f} | {spread} | {verdict} |")
    counts = {v: sum(1 for _, rows in results.values() for r in rows if r[5] == v) for v in ("ok", "range", "off")}
    lines += ["", f"ok {counts['ok']}, within the game's range {counts['range']}, off {counts['off']}."]
    return "\n".join(lines) + "\n"


def lineup():
    """Every family in a row through the pipeline with its preview sheet, copied to OUT/lineup_preview.png."""
    env = {k: v for k, v in os.environ.items() if k != "PAINT_SWATCH"}
    command = [BLENDER, "--background", "--factory-startup", "--python-exit-code", "1", "--python",
               os.path.join(WORKSHOP, "blender", "run.py"), "--", "--asset", ASSET]
    subprocess.run(command, env=env, capture_output=True, text=True, check=True)
    shutil.copyfile(os.path.join(ASSET, "out", "preview.png"), os.path.join(OUT, "lineup_preview.png"))


def main(args):
    os.makedirs(OUT, exist_ok=True)
    picked = families(args)
    blends = {family: build(family) for family in picked}
    uvs = uv_data(list(blends.values()))
    with open(PAINT, encoding="utf-8") as handle:
        game = json.load(handle)
    results = {family: check(family, uvs[blends[family]], game) for family in picked}
    strips = [paint_check_sheet.row(f, os.path.join(OUT, f.replace(".", "_") + "_albedo.png"),
                                    uvs[blends[f]]["triangles"], rows) for f, (_, rows) in results.items()]
    pages = paint_check_sheet.sheet(strips, os.path.join(OUT, "paint_check.png"))
    with open(os.path.join(OUT, "paint_check.json"), "w", encoding="utf-8") as handle:
        json.dump({f: {"swatch": s, "checks": r} for f, (s, r) in results.items()}, handle, indent=1)
    with open(os.path.join(OUT, "paint_check.md"), "w", encoding="utf-8") as handle:
        handle.write(report(results))
    if "--lineup" in args:
        lineup()
    print(report(results).splitlines()[-1], *pages, sep="\n")


if __name__ == "__main__":
    main(sys.argv[1:])

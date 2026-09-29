"""Style check: compares a built asset with its codex category and writes out/style_report.txt.

    python codex/tools/stylecheck.py assets/<name> [--category <key>] [--blender <blender.exe>] [--data <folder>]

The category is the model's CATEGORY (the manifest's "category") unless --category names one. Each line gives the
asset's number, the game's five numbers for the category (min, p25, median, p75, max) and the sample count, and a
verdict: PASS inside the game's range, LOW or HIGH outside it, "--" where the codex has no data. The geometry comes from
the category's stats in codex/data; the whole albedo from a paint, palette or albedo file that has the category (or its
family, or its kind); each paint region's value, saturation and value span from the game's paint family it is painted
as (paint.json "families", see style_paint). Then the nearest game examples. A test asset may carry a fixture/ folder
of data files, read for topics codex/data does not have yet (--data adds more such folders). build.ps1 runs it after
every build of a model that sets CATEGORY and prints the report.
"""
import argparse
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import codexdata  # noqa: E402
import style_measure  # noqa: E402
import style_paint  # noqa: E402
import style_rules  # noqa: E402

REPORT = "style_report.txt"
HEAD = f"{'':<28}{'asset':>9}{'min':>9}{'p25':>9}{'median':>9}{'p75':>9}{'max':>9}{'n':>5}  verdict"


def parse(argv=None):
    parser = argparse.ArgumentParser(prog="stylecheck.py", description=__doc__.splitlines()[0])
    parser.add_argument("asset", help="the asset folder (assets/<name>), built")
    parser.add_argument("--category", help="codex category key; default: the model's CATEGORY")
    parser.add_argument("--blender", help="blender.exe for the mesh dump (default: WORKSHOP_BLENDER or tools)")
    parser.add_argument("--data", action="append", default=[], help="extra data folder for missing topics")
    return parser.parse_args(argv)


def run(folder, key=None, blender=None, data=()):
    """Measures, compares, writes out/style_report.txt; returns the report's text."""
    measured = style_measure.asset(folder, blender)
    key = key or measured["manifest"].get("category")
    extra = [d for d in [*data, os.path.join(folder, "fixture")] if os.path.isdir(d)]
    cat = codexdata.category(key, extra) if key else None
    paint_cat = style_rules.paint_category(key, extra) if key else None
    text = "\n".join(report(measured, key, cat, paint_cat, extra)) + "\n"
    path = os.path.join(folder, "out", REPORT)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
    return text


def report(measured, key, cat, paint_cat, extra):
    """The report's lines."""
    out = [_title(measured, key, cat), _sources(cat, paint_cat, extra), "", HEAD]
    rows = style_rules.lines(measured, cat, paint_cat)
    out += [_row(*row) for row in rows]
    region_text, region_rows = _regions(measured["regions"], style_paint.families(extra))
    out += region_text
    judged = [row for row in rows + region_rows if row[3] != "--"]
    flagged = [row[0].strip() for row in judged if row[3] in ("LOW", "HIGH")]
    summary = f"{len(flagged)} of {len(judged)} judged lines outside the game's range"
    out += ["", summary + (f": {', '.join(flagged)}" if flagged else "")]
    out += _measured_extras(measured)
    out += _nearest(measured, cat)
    out += [f"note: {note}" for note in measured["notes"]]
    return out


def _title(measured, key, cat):
    if not key:
        return f"Style check: {measured['name']}, no category (set CATEGORY in model.py or pass --category)"
    if not cat:
        return f"Style check: {measured['name']} against {key}: the codex has no data for this category yet"
    count = len(cat["samples"]) or (cat["stats"].get("triangles") or {}).get("n", "?")
    return f"Style check: {measured['name']} against {key} ({cat['label'] or 'no label'}, {count} game samples)"


def _sources(cat, paint_cat, extra):
    files = sorted(set((cat or {}).get("sources", []) + (paint_cat or {}).get("sources", [])))
    fixture = " (a fixture folder fills missing topics: " + ", ".join(extra) + ")" if extra else ""
    return "data: " + (", ".join(files) if files else "none") + fixture


def _row(label, shown, numbers, verdict, form):
    numbers = numbers or {}
    cells = [form.format(numbers[k]) if isinstance(numbers.get(k), (int, float)) else "--"
             for k in ("min", "p25", "median", "p75", "max")]
    count = numbers.get("n", "--")
    return f"{label:<28}{shown:>9}" + "".join(f"{c:>9}" for c in cells) + f"{count!s:>5}  {verdict}"


def _measured_extras(measured):
    size = measured.get("size_m") or []
    parts = [f"size {' x '.join(f'{s:.2f}' for s in size)} m (Unity x, y, z)" if size else ""]
    if measured.get("surface_m2") is not None:
        parts.append(f"surface {measured['surface_m2']:.3f} m2, UV area {measured['uv_area']:.3f}")
    parts.append(f"UV islands cover {measured.get('uv_fill', 0):.0%} of the atlas")
    return ["measured: " + "; ".join(p for p in parts if p)]


def _nearest(measured, cat):
    found = style_rules.nearest(measured, (cat or {}).get("samples", []))
    if not found:
        return []
    out = ["", "Nearest game examples (triangles, texture, texel density, longest side):"]
    for _, sample in found:
        numbers = [style_rules.sample_value(sample, f) for f in style_rules.SAMPLE_FIELDS]
        text = "  ".join(f"{v:g}" if v is not None else "--" for v in numbers)
        out.append(f"  {sample.get('name') or os.path.basename(sample.get('prefab', '?')):<26}{text}   "
                   f"{sample.get('prefab', '')}")
    return out


def _regions(regions, known):
    """The paint regions' lines under the table, and their rows (named after the region) for the summary."""
    if not regions:
        return [], []
    source = "paint.json families" if known else "no paint.json yet"
    out = ["", f"Paint regions: mask colour, share of texels, median albedo; against the game's paint ({source})"]
    judged = []
    for region in regions:
        head, rows = style_paint.region_lines(region, known)
        out.append(head)
        out += [_row("  " + row[0], *row[1:]) for row in rows]
        judged += [(f"{region['name']} {row[0]}", *row[1:]) for row in rows]
    return out, judged


if __name__ == "__main__":
    args = parse()
    print(run(os.path.abspath(args.asset), args.category, args.blender, args.data), end="")

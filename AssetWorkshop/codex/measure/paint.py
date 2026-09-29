"""Writes codex/data/paint.json: how the game paints each material family (paint_families), measured on its textures,
the same albedo numbers per kind of asset over every texture (categories: item, piece, creature, env, location, all),
how the game imports its textures, how big they are and how densely they cover each kind of asset, and the materials
it leaves untextured or tints.

    python codex/measure/paint.py            # about five minutes; the prefab scans are cached in codex/out/paint

Every sample is measured by paint_sample; family numbers are medians over samples (paint_aggregate).
"""
import datetime
import json
import os
import sys

import game
import paint_aggregate
import paint_families
import paint_import
import paint_kinds
import paint_materials
import paint_sample

DATA = os.path.join(game.WORKSHOP, "codex", "data", "paint.json")


def families():
    """{family: {label, region, stats, samples}} for every family in paint_families."""
    found = {}
    for name, spec in paint_families.FAMILIES.items():
        records = [r for r in (paint_sample.measure(s) for s in spec["samples"]) if r]
        print(f"{name}: {len(records)} samples", file=sys.stderr, flush=True)
        found[name] = {"label": spec["label"], "region": spec["region"],
                       "stats": paint_aggregate.family_stats(records) if records else {}, "samples": records}
    return found


def main():
    data = {"topic": "paint", "generated": datetime.date.today().isoformat(), "script": "measure/paint.py",
            "families": families(), "categories": paint_kinds.categories(),
            "textures": {"import": paint_import.survey(), "kinds": paint_import.per_kind()},
            "materials": paint_materials.survey()}
    os.makedirs(os.path.dirname(DATA), exist_ok=True)
    with open(DATA, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1)
    print(DATA)


if __name__ == "__main__":
    main()

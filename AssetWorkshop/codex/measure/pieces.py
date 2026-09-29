"""Measures every piece the hammer builds (the build menus: the _HammerPieceTable) and writes codex/data/pieces.json:
per codex category the samples, their stats and the reference prefabs, plus the summaries the pages are written from
(material sets, shared textures, Piece shader switches, snap patterns, effects per material).

    python codex/measure/pieces.py

Ships, carts, siege engines, the hammer's repair tool, the placeable rock and the training dummy are in the table too;
they are listed under "excluded" and measured elsewhere. Reads the reference export only, about a minute.
"""
import datetime
import json
import os
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import game  # noqa: E402
import pieces_extras  # noqa: E402
import pieces_fields  # noqa: E402
import pieces_icons  # noqa: E402
import pieces_kinds  # noqa: E402
import pieces_mesh  # noqa: E402
import pieces_paint  # noqa: E402
import pieces_scan  # noqa: E402
import pieces_summary  # noqa: E402
from workshop import unity  # noqa: E402

TABLE = "GameElements/Pieces/_HammerPieceTable.prefab"
OUT = os.path.join(game.WORKSHOP, "codex", "data", "pieces.json")
TABS = {0: "misc", 1: "crafting", 2: "building (workbench)", 3: "building (stonecutter)", 4: "furniture",
        5: "deep north", 100: "all"}
STATS = ("triangles", "texture_px", "texel_density", "longest_m", "snap_points", "health")


def table():
    """The prefab paths the hammer's piece table lists, in the order of the build menus."""
    body = next(b for _, k, b in game.prefab(TABLE).all_components() if k == "PieceTable")
    return [game.path_of(item) for item in unity.items(body, "m_pieces")]


def sample(path):
    """Everything measured on one piece, as the data file keeps it."""
    scan = pieces_scan.Scan(path)
    piece, wear = scan.first("Piece"), pieces_fields.wear(scan)
    look, name = scan.look(scan.visual()), os.path.basename(path)[:-7]
    scripts = {k for _, _, k, _ in scan.all_components()}
    station = game.path_of(unity.field(piece, "m_craftingStation"))
    row = {"prefab": path, "name": name, "token": unity.field(piece, "m_name"),
           "tab": TABS.get(int(unity.field(piece, "m_category")), "other"),
           "category": pieces_kinds.category(name, scripts),
           "set": pieces_kinds.material_set(name, wear and wear["material"]),
           "station": os.path.basename(station)[:-7] if station else None,
           "material_type": wear and wear["material"], "health": wear and wear["health"]}
    row.update({k: look[k] for k in ("triangles", "renderers", "texture_px", "texel_density", "size_m", "bounds_m",
                                     "shaders", "maps", "materials", "textures")})
    row["shader"] = "Piece" if "Piece" in look["shaders"] else (look["shaders"] or [None])[0]
    row.update(_structure(scan, wear))
    row["scripts"] = sorted(s for s in scripts if s not in pieces_summary.PLAIN)
    row.update(pieces_extras.all_of(scan))
    icon = pieces_icons.crop(row["icon"]) if row["icon"] else None
    row["icon_look"] = pieces_icons.measure(icon) if icon is not None else None
    return row


def _structure(scan, wear):
    """Snap points, colliders, LOD groups, damage states, fragments, snow, effects, lights and particles."""
    snap, lods, place = pieces_fields.snap_points(scan), pieces_fields.lods(scan), pieces_fields.placement(scan)
    worn, broken = scan.look(scan.state("worn")), scan.look(scan.state("broken"))
    return {"snap": snap, "snap_points": snap["count"], "colliders": _colliders(scan),
            "lod_levels": max((l["levels"] for l in lods), default=0),
            "lod_heights": max((l["heights"] for l in lods), key=len, default=[]),
            "lod1_triangles": sum(pieces_mesh.triangles(r["mesh"]) for r in scan.renderers()
                                  if r["lod"] == 1 and r["active"] and r["enabled"] and not r["snow"]),
            "states": wear and dict(wear["states"], worn_triangles=worn["triangles"],
                                    broken_triangles=broken["triangles"], worn_materials=worn["materials"]),
            "fragments": wear and wear["fragments"], "snow": wear and wear["snow"]["meshes"],
            "effects": dict(wear["effects"] if wear else {}, place=place["place_effect"]),
            "icon": place["icon"], "comfort": place["comfort"] or None,
            "flags": sorted(k for k in ("ground_piece", "clip_ground", "clip_everything", "random_rotation")
                            if place[k] == 1) + sorted(k for k, v in (wear or {}).items() if k in (
                                "ash_immune",) and v == 1) + (["no_support"] if wear and wear["supports"] == 0
                                                             else []),
            "lights": pieces_fields.lights(scan), "particles": pieces_fields.particles(scan)}


def _colliders(scan):
    """'kind tag layer' of every collider with how many there are, triggers marked."""
    counts = {}
    for c in pieces_fields.colliders(scan):
        key = " ".join(x for x in (c["kind"], c["tag"] if c["tag"] != "Untagged" else "", c["layer"],
                                   "trigger" if c["trigger"] else "") if x)
        counts[key] = counts.get(key, 0) + 1
    return dict(sorted(counts.items()))


def stats(values):
    """{n, min, p25, median, p75, max} of the numbers given (None left out)."""
    values = [float(v) for v in values if v is not None]
    if not values:
        return {"n": 0}
    q = np.percentile(values, [0, 25, 50, 75, 100])
    return {"n": len(values), **{k: round(float(v), 3) for k, v in zip(("min", "p25", "median", "p75", "max"), q)}}


def categories(samples):
    """{key: {label, samples, stats, references}} for every category with at least one sample."""
    found = {}
    for key, label in pieces_kinds.LABELS.items():
        rows = sorted((s for s in samples if s["category"] == key), key=lambda s: s["name"])
        if not rows:
            continue
        for row in rows:
            row["longest_m"] = max(row["size_m"]) if row["size_m"] else None
        names = {r["name"]: r["prefab"] for r in rows}
        references = [names[n] for n in pieces_kinds.REFERENCES.get(key, []) if n in names] or \
            [r["prefab"] for r in rows[:4]]
        found[key] = {"label": label, "samples": rows, "stats": {k: stats(r[k] for r in rows) for k in STATS},
                      "by_set": pieces_summary.by_set(rows), "references": references}
    return found


def dump(value, depth=0, fold=4):
    """JSON with one item per line down to fold levels, anything deeper on one line: a sample stays readable."""
    pad = " " * (depth + 1)
    if depth >= fold or not isinstance(value, (dict, list)) or not value:
        return json.dumps(value, ensure_ascii=False)
    if isinstance(value, dict):
        items = [f"{pad}{json.dumps(str(k))}: {dump(v, depth + 1, fold)}" for k, v in value.items()]
        return "{\n" + ",\n".join(items) + "\n" + " " * depth + "}"
    return "[\n" + ",\n".join(pad + dump(v, depth + 1, fold) for v in value) + "\n" + " " * depth + "]"


def main():
    paths = table()
    samples, excluded = [], {}
    for path in paths:
        name = os.path.basename(path)[:-7]
        if name in pieces_kinds.EXCLUDED:
            excluded[name] = {"prefab": path, "reason": pieces_kinds.EXCLUDED[name]}
            continue
        samples.append(sample(path))
    data = {"topic": "pieces", "generated": datetime.date.today().isoformat(), "script": "measure/pieces.py",
            "source": {"table": TABLE, "pieces": len(paths), "measured": len(samples)},
            "categories": categories(samples), "excluded": excluded}
    data.update(pieces_summary.summaries(samples))
    data["paint"] = pieces_paint.colours(samples)
    data["sections"] = pieces_paint.sections(samples)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as handle:
        handle.write(dump(data) + "\n")
    print(f"pieces: {len(samples)} measured, {len(excluded)} excluded -> {OUT}")


if __name__ == "__main__":
    main()

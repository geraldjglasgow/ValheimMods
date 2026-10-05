#!/usr/bin/env python3
"""Measure build pieces in the running game into pieces.json, the catalogue blueprint designs are written from.

  python measure_pieces.py wood_floor stone_wall_4x2 ...   measure these (merged into pieces.json)
  python measure_pieces.py --set building                  the building set below
  python measure_pieces.py --all                           every piece the Hammer builds

Per piece: the game's name, category, material, build cost, snap points (local, at yaw 0; the most reliable
footprint), the drawn size at yaw 0 and how far the pivot sits above the mesh bottom (both from a /lineup copy, so
they include trims and snow meshes), plus the hand-written notes in NOTES (facing, hanging, traps). Coordinates: x
right, y up, z forward (+z is the side a /lineup copy turns toward the player, its front).
"""
import json, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from blueprint import get, ev, val  # noqa: E402

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "pieces.json")

BUILDING = """wood_floor wood_floor_1x1 woodwall wood_wall_half wood_wall_quarter wood_wall_roof wood_wall_roof_45
wood_wall_roof_45_upsidedown wood_wall_roof_top_45 wood_beam wood_beam_1 wood_beam_26 wood_beam_45 wood_pole
wood_pole2 wood_stair wood_stepladder wood_ledge wood_roof wood_roof_top wood_roof_45 wood_roof_top_45 wood_roof_67
wood_roof_top_67 wood_roof_icorner_45 wood_roof_ocorner_45 wood_door wood_gate wood_window wood_fence wood_fence_gate
wood_wall_log wood_wall_log_4x0.5 wood_pole_log wood_pole_log_4 wood_log_26 wood_log_45 stake_wall stone_wall_4x2
stone_wall_2x1 stone_wall_1x1 stone_floor_2x2 stone_stair stone_pillar stone_arch darkwood_gate fire_pit hearth
piece_cookingstation piece_workbench piece_workbench_ext1 piece_workbench_ext2 piece_banner01 portal_wood""".split()

NOTES = {
    "wood_floor": "top = pivot + 0.08",
    "stone_floor_2x2": "1 m thick, top = pivot + 0.5",
    "wood_stair": "rises 1 toward local -z over 2 m; pivot at the bottom edge height. yaw 180 rises to +z",
    "stone_stair": "rises 1 toward local -z over 2 m; pivot at the bottom",
    "wood_stepladder": "1 wide, rises 2 toward local -z over 2 m (45 deg); pivot at the bottom centre",
    "wood_roof_45": "rises toward local -z; plane through the pivot. yaw 270 rises to +x, 90 to -x, 180 to +z",
    "wood_roof": "26 deg: rises 1 over 2 toward local -z; pivot at the low edge height",
    "wood_roof_top_45": "ridge along x, 1 above the pivot; eaves at z +-1 at pivot height. yaw 90: ridge along z",
    "wood_wall_roof_45": "gable triangle, right angle at +x (tall side x=+1), pivot at the bottom centre; yaw 180 mirrors",
    "wood_wall_roof_top_45": "not a gable peak: a V (valley) wall, bottom y 0 at x +-1 and top y 2 at x +-1",
    "wood_beam_26": "runs along x rising toward +x (1 over 2); pivot at the low end height, centred along x",
    "wood_beam_45": "runs along x rising toward +x (2 over 2), centred",
    "wood_log_45": "log version of wood_beam_45 (core wood); good for crossed gable horns",
    "wood_window": "1x1 shutter (opens like a door) hinged at the pivot (x 0), spans x -1..0; yaw 180 spans 0..1",
    "wood_gate": "the taller wood door: 2 wide, 3 tall, pivot 1 above the bottom",
    "darkwood_gate": "2 wide, 4 tall, pivot at the bottom (needs darkwood materials)",
    "wood_fence": "2 m along x but spans x -1.1..0.9: at yaw 0 the pivot is 0.1 east of the span centre",
    "wood_fence_gate": "spans x -1.2..0.8, pivot 1 above the bottom",
    "stake_wall": "2 wide, snaps 0..2 high but the points reach 3.5",
    "fire_pit": "a raycast straight up must hit a roof (underRoof) or rain puts it out; smoke needs a vent above",
    "hearth": "4 x 3 stone hearth; leave a ridge vent above it",
    "piece_cookingstation": "fire check point 0.2 above the pivot: put the pivot on the fire's centre; three at yaw "
                            "0/60/120 share one campfire",
    "piece_workbench": "front (work side) is +z; needs a roof above; extensions within 5 m",
    "piece_workbench_ext1": "chopping block; counts within 5 m of the workbench",
    "piece_workbench_ext2": "tanning rack; counts within 5 m of the workbench",
    "piece_banner01": "black banner, hangs below its pivot (top bar); thin along x, so it faces +-x at yaw 0; must touch "
                      "what holds it",
    "portal_wood": "3.3 m tall: does not fit under a 3 m ceiling; front +z",
}


def snaps(path):
    out = []
    for i in range(int(val(f"{path}.transform.childCount"))):
        child = f"{path}.transform.GetChild({i})"
        if val(f"{child}.gameObject.tag") == "snappoint":
            out.append([round(float(v), 3) for v in val(f"{child}.localPosition").strip("()").split(",")])
    return out


def cost(path):
    res = []
    for i in range(int(val(f"{path}.Piece.m_resources.Length"))):
        r = f"{path}.Piece.m_resources[{i}]"
        res.append([val(f"{r}.m_resItem.name"), int(val(f"{r}.m_amount"))])
    return res


def drawn(name):
    """Size at yaw 0 and pivot height from a /lineup copy (the first call tells the row's own turn)."""
    first = json.loads(get("/lineup", prefabs=name, new="1"))[0]
    get("/clear")
    r = json.loads(get("/lineup", prefabs=name, new="1", yaw=str(-first["yaw"])))[0]
    get("/clear")
    return r["size"], round(r["position"][1] - r["row"]["origin"][1], 2)


def measure(name):
    p = f'$prefab("{name}")'
    material = ev(f"{p}.WearNTear.m_materialType")
    size, pivot = drawn(name)
    entry = {
        "name": val(f"Localization.instance.Localize({p}.Piece.m_name)"),
        "category": val(f"{p}.Piece.m_category"),
        "material": material.split(" = ", 1)[1] if " = " in material else None,
        "cost": cost(p), "snaps": snaps(p), "size": size, "pivot_above_bottom": pivot,
    }
    if name in NOTES:
        entry["note"] = NOTES[name]
    return entry


def hammer_pieces():
    table = 'ObjectDB.instance.GetItemPrefab("Hammer").ItemDrop.m_itemData.m_shared.m_buildPieces.m_pieces'
    return [val(f"{table}[{i}].name") for i in range(int(val(f"{table}.Count")))]


def main():
    args = sys.argv[1:]
    names = BUILDING if args == ["--set", "building"] else hammer_pieces() if args == ["--all"] else args
    data = json.load(open(OUT)) if os.path.exists(OUT) else {}
    for n in names:
        try:
            data[n] = measure(n)
            print(n, data[n]["name"], data[n]["size"])
        except Exception as e:  # keep going; a piece without Piece or WearNTear is reported and skipped
            print("skipped", n, str(e)[:120])
    with open(OUT, "w") as f:
        json.dump(dict(sorted(data.items())), f, indent=1)
    print(f"{len(data)} pieces in {OUT}")


if __name__ == "__main__":
    main()

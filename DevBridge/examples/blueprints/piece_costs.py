# Read every piece the game's hammer can build, with its station and cost, from the running game (DevBridge):
#   python piece_costs.py            writes piece_costs.json: {prefab: {name, category, station, cost: [[item, n]]}}
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import blueprint as b

TABLE = 'ObjectDB.instance.GetItemPrefab("Hammer").GetComponent("ItemDrop").m_itemData.m_shared.m_buildPieces.m_pieces'


def text(expr):
    try:
        v = b.val(expr)
    except RuntimeError:                                              # a null on the way (no station, no item)
        return None
    return v.strip().strip('"') if v else None


def piece(i):
    p = f'{TABLE}[{i}].GetComponent("Piece")'
    n = int(text(f"{p}.m_resources.Length") or 0)
    cost = [[text(f"{p}.m_resources[{k}].m_resItem.name"), int(text(f"{p}.m_resources[{k}].m_amount") or 0)]
            for k in range(n)]
    station = text(f"{p}.m_craftingStation.m_name")
    return text(f"{TABLE}[{i}].name"), {
        "name": text(f'Localization.instance.Localize({p}.m_name)'), "category": text(f"{p}.m_category"),
        "station": text(f"Localization.instance.Localize({p}.m_craftingStation.m_name)") if station else None,
        "cost": cost}


def main():
    count = int(text(f"{TABLE}.Count"))
    out = {}
    for i in range(count):
        name, data = piece(i)
        out[name] = data
        if i % 50 == 49:
            print(f"  {i + 1} of {count}", flush=True)
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "piece_costs.json")
    json.dump(out, open(path, "w"), indent=1, sort_keys=True)
    print(f"wrote {path}: {len(out)} pieces")


if __name__ == "__main__":
    main()

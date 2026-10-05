# Design source of lighthouse.json (Ravenholt) and lighthouse_vanilla.json: an ornate stone lighthouse kept inside a
# world's support limits, from the tower parts in parts.py. Frame: door on -z, y from the levelled ground. Only
# materials a group has before Moder. Run it to write both JSON files; python check.py lighthouse checks one.
#
# The shaft's height is the one number that changes: K, the top 2 m course. Under the game's own rules stone loses
# about a quarter of its support per course and breaks below 100, so eight courses is the most that stands; the
# vanilla version stops at seven (top 14 m). Ravenholt's Cornerstone settings make stone lose only a tenth per course
# (21 courses would stand): its version goes to fifteen (top 30 m) with about twice the minimum left at the top. Wood
# resting on stone starts again at full support, so the gallery and lantern room are sound either way.
#
#   0-5 m    base 10 x 10 with 2 m walls (two rings), arched door behind a pillared portal, corner quoins
#   5 m      ledge with merlons where the shaft steps in
#   shaft    8 x 8 to 2K m, an arched window in every course (north and south faces, then east and west), corner
#            quoins, a 1 m band on top; the tall version has a double log band round the middle
#   inside   a stair winding up the walls, 4 m a turn, sconces
#   top      gallery on beam ends with a balustrade; lantern room with four blue braziers (no fuel), gabled roof with
#            dragon heads and a finial; the stair comes up inside it, a door opens east onto the gallery
#   decor    blue banners under the gallery, sconces at the portal, iron torches round the paving
import json, os
from kit import Plan
from furnish import on, hung
import parts

HERE = os.path.dirname(os.path.abspath(__file__))
DOOR = {(O, "S", k): "door" for O in (5, 4) for k in (1, 2)} | {(5, "S", 3): "lintel", (4, "S", 3): "lintel"}


def band_course(K):
    """The course the middle log band crosses (its windows are left out), or None when the shaft is short."""
    return (K + 5) // 2 if K >= 11 else None


def windows(K):
    return {(4, face, k): "window" for k in range(5, K + 1) if k != band_course(K)
            for face in (("S", "N") if k % 2 else ("W", "E"))}


def lighthouse(K):
    p = Plan()
    parts.stone_shaft(p, lambda k: (5, 4) if k <= 3 else (4,), 1, K, DOOR | windows(K), small_top=True)
    parts.stone_band(p, 4, 2 * K - 0.5)
    parts.paving(p, 7, 6, extra={(x, z) for x in (-2, 0, 2) for z in (-2, 0, 2)} | {(0, -4)})
    parts.quoins(p, 5, 0, 4)
    parts.quoins(p, 4, 5, 2 * K - 1)
    parts.merlons(p, 5, 5.5)
    parts.portal(p, -5.6)
    if band_course(K):
        parts.log_band(p, 4, 2 * band_course(K) - 3)
    parts.winding_stair(p, 2 * K)
    gf = parts.gallery(p, 2 * K)
    parts.lantern_room(p, gf, door=("E", 2))
    fittings = [("wood_gate", 0, 1.02, -4.5, 0)] + parts.wall_banners(4, 2 * K - 3.43) + parts.beacon(gf)
    fittings += [hung("piece_walltorch", sx * 1.5, 2.2, -6.27, 180) for sx in (-1, 1)] + parts.stair_sconces(2 * K)
    fittings += [on("piece_groundtorch", sx * 6.4, 0, sz * 6.4) for sx in (-1, 1) for sz in (-1, 1)]
    p.extend(Plan.KEEP + 10, fittings)
    return p.pieces()


def write(name, desc, site, pieces):
    head = json.dumps({"name": name, "description": desc, "source": os.path.basename(__file__), "site": site},
                      indent=1)[:-2]
    rows = ",\n  ".join(json.dumps(p) for p in pieces)
    path = os.path.join(HERE, name + ".json")
    with open(path, "w") as f:
        f.write(head + ',\n "pieces": [\n  ' + rows + "\n ]\n}\n")
    print(f"wrote {path}: {len(pieces)} pieces")


SHARED = ("10 x 10 base with 2 m walls and a pillared, arched door; 8 x 8 shaft with quoins, merlons on the ledge and "
          "an arched window in every course; a stair winding up inside; a gallery on beam ends with a balustrade; a "
          "log lantern room with four blue braziers (no fuel) under a gabled roof with dragon heads. Materials from "
          "before Moder.")

if __name__ == "__main__":
    write("lighthouse", "Ornate stone lighthouse for Ravenholt, 37 m to the ridge: a 30 m shaft that only stands under "
          "Ravenholt's Cornerstone limits (build_limits.json), with a double log band round its middle. " + SHARED,
          {"level_half": 7.5, "clear_radius": 16, "stand": [0, -14], "limits": "Ravenholt"}, lighthouse(15))
    write("lighthouse_vanilla", "Ornate stone lighthouse inside the game's own limits, 21 m to the ridge (14 m shaft). "
          + SHARED, {"level_half": 7.5, "clear_radius": 16, "stand": [0, -14], "limits": "vanilla"}, lighthouse(7))

# Building blocks for stone towers (lighthouse, watchtower, keep, belfry): each adds pieces to a kit.Plan in a frame
# centred on the tower, door on -z, y from the levelled ground. Courses are 2 m: course k spans y 2k-3..2k-1, so
# course 1 sits 1 m into the ground. Rings are square, 1 m walls, named by their outer half size O (O=4: 8 x 8, a
# 6 x 6 inside). lighthouse.py puts them together; python check.py <name> checks the result.
#
#   ring, course          the four wall lines of a ring; one course of a segment in 4x2 / 2x1 / 1x1 blocks
#   stone_shaft           courses of one or more rings with doors, lintels and arched windows
#   stone_band, quoins, merlons, paving, portal, log_band       trims
#   winding_stair         stair up the inside walls of an O=4 ring, 4 m a turn; the hatch it leaves at the top
#   gallery               floor over a shaft top and 1 m past it on beam ends, with a balustrade
#   lantern_room          log room on a floor: posts, parapet, door bay, hatch rail, gabled roof, dragons, finial
#   wall_banners, stair_sconces, beacon                          fittings (lists for Plan.KEEP + 10)
import math
from kit import Plan, Line, roof, moved
from furnish import on, hung

HATCH = {(-2, 0), (-2, 2), (0, 2)}       # floor cells an O=4 winding stair needs open above its top (2 mod 4 m)


def ring(O):
    """The four wall lines of a square ring of 1 m walls with outer half size O: (face, line, length)."""
    return [("S", Line(-O, -(O - 0.5), "+x", "-z"), 2 * O), ("N", Line(-O, O - 0.5, "+x", "+z"), 2 * O),
            ("W", Line(-(O - 0.5), -(O - 1), "+z", "-x"), 2 * O - 2), ("E", Line(O - 0.5, -(O - 1), "+z", "+x"), 2 * O - 2)]


def course(p, line, u0, u1, yc, from_end=False, small=False):
    """One 2 m course from u0 to u1 centred at height yc: 4x2 blocks, then 2x1 pairs, then 1x1 pairs; from_end lays
    them from u1 back, so the small blocks end up at u0 (beside an opening on the right). small: no 4x2, so every
    block of a band above stands right over one below (support is measured centre to centre)."""
    left, sign = (u1, -1) if from_end else (u0, 1)
    sizes = ((4, "stone_wall_4x2", (0,)), (2, "stone_wall_2x1", (-0.5, 0.5)), (1, "stone_wall_1x1", (-0.5, 0.5)))
    for size, name, rows in sizes[1:] if small else sizes:
        while abs(u1 - u0) - abs(left - (u1 if from_end else u0)) >= size - 0.01:
            for dy in rows:
                line.put(p, 0, name, left + sign * size / 2, yc + dy)
            left += sign * size


def stone_shaft(p, rings, k1, k2, openings=(), small_top=False):
    """Courses k1..k2 of the rings rings(k) (a list of O). openings: {(O, face, k): kind}, the middle 2 m of that face
    left open: "door" (nothing), "lintel" (an arch in the bottom half, stone over it), "window" (an arch in the top
    half). small_top: the last course in 2 m blocks, for a band on top."""
    openings = dict(openings)
    for k in range(k1, k2 + 1):
        yc, small = 2 * k - 2, small_top and k == k2
        for O in rings(k):
            for face, line, length in ring(O):
                kind, mid = openings.get((O, face, k)), length / 2
                if not kind:
                    course(p, line, 0, length, yc, small=small)
                    continue
                course(p, line, 0, mid - 1, yc, small=small)
                course(p, line, mid + 1, length, yc, from_end=True, small=small)
                if kind == "lintel":
                    line.put(p, 0, "stone_arch", mid, yc - 1 + 0.53)
                    line.put(p, 0, "stone_wall_2x1", mid, yc + 0.5)
                elif kind == "window":
                    line.put(p, 0, "stone_arch", mid, yc + 0.53)


def stone_band(p, O, y):
    """A 1 m course of 2x1 blocks round ring O, centred at y (on top of a course: y = its top + 0.5)."""
    for face, line, length in ring(O):
        for u in range(1, length, 2):
            line.put(p, 0, "stone_wall_2x1", u, y)


def quoins(p, O, y0, y1):
    """Stone pillars standing proud on the four corners of ring O from y0 to y1 (2 m each), a 1x1 cap above."""
    for sx in (-1, 1):
        for sz in (-1, 1):
            for y in range(int(y0) + 1, int(y1), 2):
                p.add(0, "stone_pillar", sx * O, y, sz * O)
            p.add(0, "stone_wall_1x1", sx * O, y1 + 0.58, sz * O)


def merlons(p, O, y):
    """Merlons on a ledge: 1x1 blocks round the line O - 0.5 at the corners and every 2.25 m, bottom at y - 0.5."""
    step = [u for u in (-2.25 * i for i in range(-3, 4)) if abs(u) <= O - 0.5] + [-(O - 0.5), O - 0.5]
    for u in sorted(set(round(v, 2) for v in step)):
        for s in (-1, 1):
            p.add(0, "stone_wall_1x1", u, y, s * (O - 0.5))
            if abs(u) < O - 1:
                p.add(0, "stone_wall_1x1", s * (O - 0.5), y, u, 90)


def paving(p, outer, inner, extra=()):
    """Stone floor (top at the ground) in the square ring between half sizes inner and outer (2 m tiles on even
    centres), plus the tiles in extra (centres) - e.g. inside the tower and in the doorway."""
    for x in range(-outer + 1, outer, 2):
        for z in range(-outer + 1, outer, 2):
            if max(abs(x), abs(z)) >= inner or (x, z) in extra:
                p.add(0, "stone_floor_2x2", x, -0.5, z)


def portal(p, z, height=4):
    """A door portal on the paving in front of the face at z (negative, the door side): two stone pillars either
    side of a 2 m door and an arch across their tops."""
    for sx in (-1, 1):
        for y in range(1, height, 2):
            p.add(0, "stone_pillar", sx * 1.5, y, z)
    p.add(0, "stone_arch", 0, height + 0.53, z)


def log_band(p, O, y, rows=2):
    """Courses of core-wood logs wrapped round ring O's faces from y up (0.53 m each), crossing at the corners."""
    n = math.ceil((2 * O + 0.66) / 4.66)                                 # logs end to end, past both corners
    us = [-n * 4.66 / 2 + 4.66 * (i + 0.5) for i in range(n)]
    for r in range(rows):
        for u in us:
            for s in (-1, 1):
                p.add(1, "wood_wall_log_4x0.5", u, y + 0.27 + 0.53 * r, s * (O + 0.45))
                p.add(1, "wood_wall_log_4x0.5", s * (O + 0.45), y + 0.27 + 0.53 * r, u, 90)


def winding_stair(p, top):
    """Inside an O=4 ring: 2 x 2 landings in the corners and one stair a side, winding up the walls 4 m a turn from
    the ground to `top` (2 more than a multiple of 4, so it arrives in the north-east corner); the floor at the top
    leaves HATCH open."""
    assert top % 4 == 2, "the stair arrives at the north-east corner only at 2 more than a multiple of 4"
    steps = [((-2, 0), 180), ((0, 2), 270), ((2, 0), 0), ((0, -2), 90)]           # each rises toward the next corner
    corners = [(-2, 2), (2, 2), (2, -2), (-2, -2)]
    for h in range(top):
        (x, z), yaw = steps[h % 4]
        p.add(1, "wood_stair", x, h, z, yaw)
        if h + 1 < top:
            cx, cz = corners[h % 4]
            p.add(1, "wood_floor", cx, h + 1 - 0.08, cz)


def gallery(p, y, O=4, hatch=HATCH):
    """The floor over a shaft whose stone top is y, and 1 m past ring O on beam ends (cells in hatch left open), with
    a two-rail balustrade. Returns the floor top."""
    gf, half = y + 0.22, O + 1
    cells = sorted(((x, z) for x in range(-O, O + 1, 2) for z in range(-O, O + 1, 2) if (x, z) not in hatch),
                   key=lambda c: -max(abs(c[0]), abs(c[1])))                     # over the walls first
    for u in [i + 0.5 for i in range(-O, O)]:
        for s in (-1, 1):
            p.add(2, "wood_beam_1", u, y - 0.2, s * (O + 0.45), 90)
            p.add(2, "wood_beam_1", s * (O + 0.45), y - 0.2, u, 0)
    for x, z in cells:
        p.add(2, "wood_floor", x, gf - 0.08, z)
    edge = half - 0.2
    for s in (-1, 1):
        for u in sorted({-edge, edge, *range(-O + 1, O, 2)}):
            p.add(3, "wood_pole", u, gf + 0.5, s * edge)
            if abs(u) < edge - 0.5:
                p.add(3, "wood_pole", s * edge, gf + 0.5, u)
        for u in range(-O, O + 1, 2):
            for yy in (gf + 0.45, gf + 1.0):
                p.add(4, "wood_beam", u, yy, s * edge)
                p.add(4, "wood_beam", s * edge, yy, u, 90)
    return gf


def lantern_room(p, gf, door=("E", 2), hatch_rail=True):
    """A 6 x 6 log room on the floor top gf: twelve log posts, a two-log parapet (the door bay open: face and u), a
    log plate under a gabled thatch roof with a dragon head at each ridge end and a finial; a rail round HATCH."""
    posts = {(x, s * 3) for x in (-3, -1, 1, 3) for s in (-1, 1)} | {(s * 3, z) for z in (-1, 1) for s in (-1, 1)}
    for x, z in sorted(posts):
        p.add(5, "wood_pole_log_4", x, gf + 2.12, z)
    for s, faces in ((-1, ("S", "W")), (1, ("N", "E"))):
        for u in (-2, 0, 2):
            for y, low in ((gf + 0.27, True), (gf + 0.8, True), (gf + 3.74, False)):
                if not (low and door == (faces[0], u)):
                    p.add(5, "wood_wall_log", u, y, s * 3)
                if not (low and door == (faces[1], u)):
                    p.add(5, "wood_wall_log", s * 3, y, u, 90)
    if hatch_rail:
        for x, z in ((-1, -1), (-1, 1), (1, 1)):
            p.add(6, "wood_pole", x, gf + 0.5, z)
        for x, z, yaw in ((-1, 0, 90), (-2, -1, 0), (0, 1, 0)):
            p.add(7, "wood_beam", x, gf + 1.0, z, yaw)
    p.extend(Plan.KEEP, moved(roof(6, 6, 4, side_over=0, gable_over=1, horns=None).pieces(), 0, 0, 0, gf))
    p.extend(Plan.KEEP + 9, [("wood_dragon1", 0, gf + 7.0, -3.6, 180), ("wood_dragon1", 0, gf + 7.0, 3.6, 0),
                             ("wood_pole_log", 0, gf + 8.0, 0, 0)])


def beacon(gf, cells=((0, 0), (2, 0), (0, -2), (2, -2)), name="piece_brazierfloor02"):
    """Braziers on the floor top gf (blue ones burn without fuel), clear of HATCH."""
    return [on(name, x, gf, z) for x, z in cells]


def wall_banners(O, bottom, us=(-2.5, 2.5), name="piece_banner02"):
    """Banners flat on all four faces of ring O, hanging down to `bottom` (keep them beside the windows)."""
    out = []
    for u in us:
        for s in (-1, 1):
            out += [hung(name, u, bottom, s * (O + 0.2), 90), hung(name, s * (O + 0.2), bottom, u, 0)]
    return out


def stair_sconces(top):
    """A wall torch over every west and east landing of a winding_stair, below the floor at the top."""
    out = []
    for h in range(1, top - 2, 2):
        x, z, yaw = (-2.88, 2, 90) if h % 4 == 1 else (2.88, -2, 270)
        out.append(hung("piece_walltorch", x, h + 1.3, z, yaw))
    return out

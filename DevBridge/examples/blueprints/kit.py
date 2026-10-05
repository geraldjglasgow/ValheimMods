"""Building blocks for blueprint designs (see pieces.json for every piece's snap points and quirks).

Frame: x right, z forward, y up from the levelled ground. A building's door side is -z, like plain_wood_house.
Each structure is a Plan; Plan.pieces() lists it in placement order (by phase; inside a phase lowest first, except
phases from Plan.KEEP on, which keep the order they were added in: roofs, where each row hangs from the one before).
"""
import math

O = 0.3            # exterior timber (posts, sill and plate beams, braces) sits this far outside a wall line
DIRS = {"+x": (1, 0), "-x": (-1, 0), "+z": (0, 1), "-z": (0, -1)}
ALONG = {"+x": 0, "+z": 270, "-x": 180, "-z": 90}   # yaw that turns a piece's local +x onto the direction


class Plan:
    KEEP = 60

    def __init__(self):
        self.ph = {}

    def add(self, ph, n, x, y, z, yaw=0):
        self.ph.setdefault(ph, []).append([n, x, y, z, yaw])

    def extend(self, ph, pieces):
        self.ph.setdefault(ph, []).extend(pieces)

    def pieces(self):
        out = []
        for ph in sorted(self.ph):
            items = self.ph[ph] if ph >= self.KEEP else sorted(self.ph[ph], key=lambda p: p[2])
            out += [[n, round(x, 3), round(y, 3), round(z, 3), round(yaw % 360, 1)] for n, x, y, z, yaw in items]
        return out


def moved(pieces, ox, oz, rot=0, oy=0.0):
    """Pieces of one frame turned by rot (degrees, like a yaw) and moved to (ox, oy, oz) of another."""
    t = math.radians(rot)
    c, s = math.cos(t), math.sin(t)
    return [[n, round(ox + x * c + z * s, 3), round(oy + y, 3), round(oz - x * s + z * c, 3), round((yaw + rot) % 360, 1)]
            for n, x, y, z, yaw in pieces]


class Line:
    """A straight wall line: u runs along `d` from (x0, z0), v points toward `n` (outward)."""

    def __init__(self, x0, z0, d, n):
        self.x0, self.z0, self.d, self.n, self.yaw = x0, z0, DIRS[d], DIRS[n], ALONG[d]

    def at(self, u, v=0.0):
        return self.x0 + u * self.d[0] + v * self.n[0], self.z0 + u * self.d[1] + v * self.n[1]

    def put(self, plan, ph, name, u, y, v=0.0, yaw_add=0):
        x, z = self.at(u, v)
        plan.add(ph, name, x, y, z, self.yaw + yaw_add)


def spans(u0, u1, step=2):
    """Centres of step-wide tiles from u0 to u1."""
    n = int(round((u1 - u0) / step))
    return [u0 + step * (i + 0.5) for i in range(n)]


# ---------------------------------------------------------------- stone
def stone_run(plan, ph, line, u0, u1, y0, rows=2, top=True, v=0.0):
    """1 m thick stone wall from u0 to u1 (an even length) and from y0 up: `rows` 2 m courses of 4x2 (a pair of 2x1
    for a 2 m remainder), then a 1 m course of 2x1 when top."""
    for r in range(rows):
        yc, u = y0 + 2 * r + 1, u0
        while u1 - u >= 3.99:
            line.put(plan, ph, "stone_wall_4x2", u + 2, yc, v)
            u += 4
        if u1 - u >= 1.99:
            line.put(plan, ph, "stone_wall_2x1", u + 1, yc - 0.5, v)
            line.put(plan, ph, "stone_wall_2x1", u + 1, yc + 0.5, v)
    if top:
        for u in spans(u0, u1):
            line.put(plan, ph, "stone_wall_2x1", u, y0 + 2 * rows + 0.5, v)


def log_run(plan, ph, line, u0, u1, y, v=0.0):
    """Horizontal log beams (4 m, then 2 m) from u0 to u1."""
    u = u0
    while u1 - u >= 3.99:
        line.put(plan, ph, "wood_wall_log_4x0.5", u + 2, y, v)
        u += 4
    if u1 - u >= 1.99:
        line.put(plan, ph, "wood_wall_log", u + 1, y, v)


# ---------------------------------------------------------------- curtain wall with a wall walk
WALK = 4.0         # wall-walk floor top
WALK_IN = 4.5      # the walk reaches this far inside the wall line


def curtain(plan, line, u0, u1, stone=True, posts="every"):
    """Stone wall (y -1..4) with a 4 m wooden walk inside, a breastwork and merlons on top, log posts and a log
    beam along the inner edge. stone=False bridges an opening (gate, water gate) on log lintels instead."""
    if stone:
        stone_run(plan, 0, line, u0, u1, -1)
    else:
        log_run(plan, 1, line, u0, u1, 3.3)                # just above a 3 m gate
        log_run(plan, 1, line, u0, u1, 3.75)
    for a in spans(u0, u1):
        for v in (-1.5, -3.5):
            line.put(plan, 2, "wood_floor", a, WALK - 0.08, v)
        line.put(plan, 3, "wood_wall_half", a, WALK + 0.5)
        line.put(plan, 3, "wood_wall_quarter", a - 0.5, WALK + 1)
    stops = [u0, u1] if posts == "ends" else sorted({*range(int(u0), int(u1), 4), u1})
    for u in stops:
        line.put(plan, 1, "wood_pole_log_4", u, WALK - 2.22, -WALK_IN)   # the log is 4.44 long
    log_run(plan, 1, line, u0, u1, WALK - 0.47, -WALK_IN)


def wall_stairs(plan, line, u0, rise=4):
    """Wooden stairs beside the walk's inner edge, rising along +u from u0 to the walk."""
    for k in range(rise):
        line.put(plan, 1, "wood_stair", u0 + 2 * k + 1, k, -(WALK_IN + 1.4), 270)


# ---------------------------------------------------------------- roofs and gables (ridge along z)
def roof(W, L, H, side_over=1, gable_over=1, vents=(), horns="both", kind="wood"):
    """45 degree roof over a W x L building (W/2 odd) with walls H high: ridge along z, side_over 2 m rows of eaves,
    gable_over metres past each gable (or (front, back)). Rows in `vents` (their z) get a raised ridge on posts.
    horns: crossed log horns at "both" gable ends, "front", "back" or none (None).
    kind "wood" is thatch; "darkwood" the darkwood roof (same shapes)."""
    p, hw = Plan(), W // 2
    yr = H + hw - 1                                   # ridge eaves height (peak yr + 1)
    front, back = gable_over if isinstance(gable_over, tuple) else (gable_over, gable_over)
    rows = spans(-(L / 2 + front), L / 2 + back)
    slope, top = f"{kind}_roof_45", f"{kind}_roof_top_45"
    for c in [hw - 1] + [hw - 1 + 2 * k for k in range(1, side_over + 1)] + list(range(hw - 3, 1, -2)):
        for z in rows:
            p.add(Plan.KEEP, slope, -c, yr - c + 1, z, 270)
            p.add(Plan.KEEP, slope, c, yr - c + 1, z, 90)
    for z in rows:
        if z not in vents:
            p.add(Plan.KEEP + 1, top, 0, yr, z, 90)
    for sx, zp in sorted({(sx, z + dz) for z in vents for sx in (-1, 1) for dz in (-1, 1)}):   # rows share posts
        p.add(Plan.KEEP + 1, "wood_pole", sx, yr + 0.5, zp)
    for z in vents:
        p.add(Plan.KEEP + 2, top, 0, yr + 1, z, 90)
    ends = {"both": (-1, 1), "front": (-1,), "back": (1,)}.get(horns, ())
    for ze in [-(L / 2 + front - 0.3) if e < 0 else L / 2 + back - 0.3 for e in ends]:
        p.add(Plan.KEEP + 3, "wood_log_45", 0, yr + 1.35, ze, 0)
        p.add(Plan.KEEP + 3, "wood_log_45", 0, yr + 1.35, ze, 180)
    return p


def gable(plan, ph, W, H, z):
    """Fill the gable triangle of a W wide roof on the wall plane z, from H to the ridge; the 2 x 1 peak stays open
    as a smoke vent."""
    hw = W // 2
    for k in range((hw - 1) // 2):
        x = hw - 1 - 2 * k
        for sx in (-1, 1):
            for j in range(k):
                plan.add(ph, "woodwall", sx * x, H + 1 + 2 * j, z)
            plan.add(ph + 1, "wood_wall_roof_45", sx * x, H + 2 * k, z, 0 if sx < 0 else 180)
    for j in range((hw - 1) // 2):
        plan.add(ph, "woodwall", 0, H + 1 + 2 * j, z)


# ---------------------------------------------------------------- one-storey timber buildings
def _bay(plan, line, a, kind, H, y0=0.0):
    """One 2 m bay of an H (3 or 4) m wall standing on y0: S solid, W window (shutters), D the 3 m wood gate,
    d the 2 m wood door (with a half wall over it), L low wall, O open."""
    put = lambda ph, n, y, v=0.0, yaw=0: line.put(plan, ph, n, a, y0 + y, v, yaw)
    if kind == "S":
        put(10, "woodwall", 1)
        put(10, "wood_wall_half" if H == 3 else "woodwall", 2.5 if H == 3 else 3)
    elif kind == "W":
        put(10, "wood_wall_half", 0.5)
        put(10, "wood_wall_half" if H == 3 else "woodwall", 2.5 if H == 3 else 3)
        line.put(plan, 11, "wood_window", a - 1, y0 + 1.5, 0, 180)
        line.put(plan, 11, "wood_window", a + 1, y0 + 1.5, 0, 0)
        put(11, "wood_beam", 0.9, O)
        put(11, "wood_beam", 2.1, O)
    elif kind == "D":
        put(10, "wood_gate", 1.05)
        if H == 4:
            put(10, "wood_wall_half", 3.5)
    elif kind == "d":
        put(10, "wood_door", 1.02)
        put(10, "wood_wall_half", 2.5)
        if H == 4:
            put(10, "wood_wall_half", 3.5)
    elif kind == "L":
        put(10, "wood_wall_half", 0.5)


def _sides(W, L):
    """The four wall lines of a W x L building centred on the origin, as (key, line, length); u runs left to right
    seen from outside the front (F) and back (K), front to back on the sides (Wst, E)."""
    hw, hl = W / 2, L / 2
    return [("F", Line(-hw, -hl, "+x", "-z"), W), ("K", Line(-hw, hl, "+x", "+z"), W),
            ("Wst", Line(-hw, -hl, "+z", "-x"), L), ("E", Line(hw, -hl, "+z", "+x"), L)]


def _frame(plan, W, L, H, bays):
    """Exterior timber: posts at every bay edge, sill beams, plates under the gables, braces on solid corner bays."""
    hw, hl = W / 2, L / 2
    poles = [("wood_pole2", 1), ("wood_pole", 2.5)] if H == 3 else [("wood_pole2", 1), ("wood_pole2", 3)]
    low = [("wood_pole2", 1)] if H == 3 else [("wood_pole2", 1), ("wood_pole", 2.5)]   # under the eaves: end below the thatch
    for sx in (-1, 1):
        for sz in (-1, 1):
            for n, y in low:
                plan.add(10, n, sx * (hw + O), y, sz * (hl + O))
    for key, line, length in _sides(W, L):
        for u in range(2, int(length) - 1, 2):
            for n, y in (poles if key in ("F", "K") else low):
                line.put(plan, 10, n, u, y, O)
        for i, kind in enumerate(bays[key]):
            a = 2 * i + 1
            if kind not in "DO":
                line.put(plan, 0, "wood_beam", a, 0.2, O)
            if key in ("F", "K"):
                line.put(plan, 12, "wood_beam", a, H, O)
            if kind == "S" and i in (0, len(bays[key]) - 1):
                line.put(plan, 12, "wood_beam_45", a, 1, O, 0 if i == 0 else 180)


def _gable_frame(plan, W, H, z, out):
    """Posts up the gable at every bay edge and a collar beam under the open peak, outside the gable wall."""
    hw = W // 2
    zo = z + O * out
    for x in range(1, hw, 2):
        for sx in (-1, 1):
            for j in range((hw - x) // 2):
                plan.add(13, "wood_pole2", sx * x, H + 1 + 2 * j, zo)
    plan.add(14, "wood_beam", 0, H + hw - 1, zo)


def building(W, L, H, bays, floor="wood", side_over=1, vents=(), inside=()):
    """A one-storey timber building, W across (W/2 odd) by L deep, walls H (3 or 4) m, door side -z, ridge along z.
    bays: {"F": "...", "K": "...", "Wst": "...", "E": "..."} one letter per 2 m bay (see _bay). inside: extra
    pieces (name, x, y, z, yaw) placed last, e.g. a hearth under `vents`."""
    p = Plan()
    if floor:
        name, y = ("wood_floor", 0.05) if floor == "wood" else ("stone_floor_2x2", -0.4)
        for x in spans(-W / 2, W / 2):
            for z in spans(-L / 2, L / 2):
                p.add(0, name, x, y, z)
    for key, line, _ in _sides(W, L):
        for i, kind in enumerate(bays[key]):
            _bay(p, line, 2 * i + 1, kind, H)
    _frame(p, W, L, H, bays)
    for zs in (-1, 1):
        gable(p, 15, W, H, zs * L / 2)
        _gable_frame(p, W, H, zs * L / 2, zs)
    p.extend(Plan.KEEP, roof(W, L, H, side_over, 1, vents).pieces())
    for n, x, y, z, yaw in inside:
        p.add(Plan.KEEP + 10, n, x, y, z, yaw)
    return p


# ---------------------------------------------------------------- towers on the wall
def tower(cx, cz, half, walks, ridge="z", closed=()):
    """A square stone tower 2*half wide centred at (cx, cz): stone up to the walk (y 4), a timber watch storey to
    y 7 under a gable roof. walks: (axis, lo, hi) bands of wall walk running through it; the bays they cross stay
    open, the others alternate arrow slits and solid wall. closed: faces ("S", "N", "W", "E") kept shut anyway,
    e.g. the side of a water-gate tower that looks over the open gate."""
    p, w = Plan(), 2 * half
    x0, z0 = cx - half, cz - half
    stone_run(p, 0, Line(x0, z0 + 0.5, "+x", "-z"), 0, w, -1)
    stone_run(p, 0, Line(x0, cz + half - 0.5, "+x", "+z"), 0, w, -1)
    stone_run(p, 0, Line(x0 + 0.5, z0 + 1, "+z", "-x"), 0, w - 2, -1)
    stone_run(p, 0, Line(cx + half - 0.5, z0 + 1, "+z", "+x"), 0, w - 2, -1)
    for x in spans(x0, x0 + w):
        for z in spans(z0, z0 + w):
            p.add(2, "wood_floor", x, WALK - 0.08, z)
    faces = [("S", Line(x0, z0 + 0.2, "+x", "-z"), "x"), ("N", Line(x0, cz + half - 0.2, "+x", "+z"), "x"),
             ("W", Line(x0 + 0.2, z0, "+z", "-x"), "z"), ("E", Line(cx + half - 0.2, z0, "+z", "+x"), "z")]
    for key, line, along in faces:
        for i, a in enumerate(spans(0, w)):
            mid = (x0 if along == "x" else z0) + a
            crossed = key not in closed and any(ax != along and min(mid + 1, hi) - max(mid - 1, lo) >= 1
                                                for ax, lo, hi in walks)
            if not crossed:
                line.put(p, 10, "wood_wall_half" if i % 2 == 0 else "woodwall", a, WALK + 0.5 if i % 2 == 0 else WALK + 1)
                line.put(p, 10, "wood_wall_half", a, WALK + 2.5)
    for sx in (-1, 1):
        for sz in (-1, 1):
            p.add(10, "wood_pole2", cx + sx * (half - 0.2), WALK + 1, cz + sz * (half - 0.2))
            p.add(10, "wood_pole", cx + sx * (half - 0.2), WALK + 2.5, cz + sz * (half - 0.2))
    top = Plan()
    for zs in (-1, 1):
        gable(top, 15, w, 3, zs * (half - 0.2))
    top.extend(Plan.KEEP, roof(w, w, 3, 0, 1).pieces())
    p.extend(Plan.KEEP + 5, moved(top.pieces(), cx, cz, 0 if ridge == "z" else 90, WALK))
    return p


# ---------------------------------------------------------------- small things
def fence_rect(plan, x0, z0, x1, z1, gate_at=None):
    """Roundpole fence round a rectangle (even sides); gate_at: the x of a 2 m gate in the -z side."""
    for x in spans(x0, x1):
        for z, side in ((z0, "S"), (z1, "N")):
            if side == "S" and gate_at is not None and abs(x - gate_at) < 0.5:
                plan.add(20, "wood_fence_gate", x + 0.2, 1.0, z)
            else:
                plan.add(20, "wood_fence", x + 0.1, 0, z)
    for z in spans(z0, z1):
        for x in (x0, x1):
            plan.add(20, "wood_fence", x, 0, z - 0.1, 90)

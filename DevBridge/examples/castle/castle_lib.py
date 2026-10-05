# Geometry helpers for the big castle. Coordinates are metres relative to the castle centre; x east, z north.
from cs import *
WB = GY - 0.44            # curtain walls and towers start 0.44 below ground so the walk is at stair-landing height
WALK = WB + 8             # wall-walk height, GY + 7.56
P = []                    # (prefab, x, y, z, yaw) relative


def add(n, x, y, z, yaw=0): P.append((n, x, y, z, yaw))


def fill(cells, along_x, line, base):
    """cells: set of (a, k) 1 m cells along a wall line; a = position along the wall, k = row. Greedy 4x2, 2x1, 1x1."""
    left = set(cells)

    def put(n, a, k, w, h):
        c, y = a + w / 2, base + k + h / 2
        if along_x: add(n, c, y, line + 0.5, 0)
        else: add(n, line + 0.5, y, c, 90)
        for da in range(w):
            for dk in range(h): left.discard((a + da, k + dk))
    if not cells: return
    k0 = min(k for _, k in cells)
    for k in sorted({k for _, k in cells}):
        if (k - k0) % 2: continue
        for a in sorted({a for a, kk in cells if kk == k}):
            if all((a + i, k + j) in left for i in range(4) for j in range(2)): put("stone_wall_4x2", a, k, 4, 2)
    for k in sorted({k for _, k in left}):
        for a in sorted({a for a, kk in left if kk == k}):
            if (a, k) in left and (a + 1, k) in left: put("stone_wall_2x1", a, k, 2, 1)
    for a, k in sorted(left): put("stone_wall_1x1", a, k, 1, 1)


def wall(along_x, line, a0, a1, rows, base, holes=()):
    """holes: (a0, a1, k0, k1) openings."""
    cells = {(a, k) for a in range(a0, a1) for k in rows
             if not any(h[0] <= a < h[1] and h[2] <= k < h[3] for h in holes)}
    fill(cells, along_x, line, base)


def merlons(along_x, line, a0, a1, row, base):
    for a in range(a0, a1):
        if (a - a0) % 2 == 0 or a == a1 - 1:
            fill({(a, row)}, along_x, line, base)


def box(x0, x1, z0, z1, rows, base, holes=None, parapet_row=None):
    """Hollow 1 m thick box on cells [x0,x1) x [z0,z1). holes: side (S N W E) -> list of (a0, a1, k0, k1)."""
    holes = holes or {}
    wall(True, z0, x0, x1, rows, base, holes.get("S", ()))
    wall(True, z1 - 1, x0, x1, rows, base, holes.get("N", ()))
    wall(False, x0, z0 + 1, z1 - 1, rows, base, holes.get("W", ()))
    wall(False, x1 - 1, z0 + 1, z1 - 1, rows, base, holes.get("E", ()))
    if parapet_row is not None:
        top = parapet_row + 1
        merlons(True, z0, x0, x1, top, base); merlons(True, z1 - 1, x0, x1, top, base)
        merlons(False, x0, z0 + 1, z1 - 1, top, base); merlons(False, x1 - 1, z0 + 1, z1 - 1, top, base)


def floor(n, xs, zs, y, skip=()):
    for x in xs:
        for z in zs:
            if (x, z) not in skip: add(n, x, y, z, 0)


def build(label, killall_every=80):
    """Place P in support order (lowest first), then clear P."""
    order = sorted(P, key=lambda p: (round(p[2], 2), p[1], p[3]))
    bad = 0
    for i, (n, x, y, z, yaw) in enumerate(order):
        r = place(n, CX + x, y, CZ + z, yaw)
        if r.startswith("ERR") or "rror" in r: bad += 1; print("  fail", n, x, y, z, r[:100])
        if i % killall_every == killall_every - 1: console("killall")
    print(f"{label}: placed {len(order) - bad} of {len(order)}")
    P.clear()
    return len(order) - bad

"""The burst egg's six pieces: the lower cup and five petals, each a closed shell SHELL thick, cut from the whole egg's
own surface (ecp_queen_egg/egg_shape.py) along its grooves, GAP apart so no two pieces touch.

- The cup: everything below the waist groove's bottom (less GAP / 2), a closed ring of 20 columns.
- Petal k (0 to 4): between seam k and seam k + 1 (GAP / 2 in from each), from the waist groove's bottom (plus GAP / 2)
  up to a tip GAP / 2 / sin(36 degrees) off the egg's top point, towards the petal's middle.

Outside: the whole egg's paint (shell, glow in the grooves); inside: the wet slime; the cut edges: the broken shell.
"""
import math

from mathutils import Vector

import egg_shape as egg

SHELL = 0.035              # shell thickness (m)
GAP = 0.01                 # between two pieces (m)


def build(mats):
    """mats: shell, glow, inside, section. Makes the cup and the five petals, each its own closed mesh object."""
    bottom = (egg.apex(False), egg.apex(False) + Vector((0.0, 0.0, SHELL)))
    _piece("cup", _cup_grid(), mats, wrap=True, end=bottom, fan_row=0)
    for k in range(egg.PETALS):
        grid = _petal_grid(k)
        _piece(f"petal{k}", grid, mats, wrap=False, end=_tip(k), fan_row=len(grid) - 1)


def _cup_grid():
    """Rows of (u, z) from the bottom up to the cut below the waist groove's bottom."""
    cols = egg.columns()
    rows = [[(u, egg.row_z(r, u)) for u in cols] for r in egg.ROWS[:egg.ROWS.index("rim")]]
    return rows + [[(u, egg.rim_z(u) - GAP / 2) for u in cols]]


def _petal_grid(k):
    """Rows of (u, z) from the cut above the waist groove's bottom to the last row under the top: per row the two
    cut sides (GAP / 2 in from the seams), the grooves' lips and the lobe's ridge."""
    seam = egg.SEAM0 + 2 * egg.HALF * k
    grid = []
    for row in ("cut",) + egg.ROWS[egg.ROWS.index("rim") + 1:]:
        height = (lambda u: egg.rim_z(u) + GAP / 2) if row == "cut" else (lambda u, r=row: egg.row_z(r, u))
        inset = (GAP / 2) / egg.radius(height(seam))
        us = (seam + inset, seam + egg.EDGE, seam + egg.HALF, seam + 2 * egg.HALF - egg.EDGE,
              seam + 2 * egg.HALF - inset)
        grid.append([(u, height(u)) for u in us])
    return grid


def _tip(k):
    """(outer, inner) tip points of petal k: just off the egg's top point towards the petal's middle."""
    middle = egg.FRONT + 2 * egg.HALF * k
    off = (GAP / 2) / math.sin(egg.HALF)
    outer = Vector((off * math.cos(middle), off * math.sin(middle), egg.C - 0.002))
    return outer, outer - Vector((0.0, 0.0, SHELL))


def _piece(name, grid, mats, wrap, end, fan_row):
    """A closed shell piece: the outer grid on the egg's surface, the inner one SHELL under it along the smooth
    normal, a fan from row fan_row to the end point (outer, inner), and the cut edges joining outer and inner."""
    rows, cols = len(grid), len(grid[0])
    verts, glows = [], []
    for side in (0, 1):
        for row in grid:
            for u, z in row:
                point, normal = egg.surface(u, z)
                verts.append(point - normal * SHELL * side)
                glows.append(egg.glow(u, z) if side == 0 else 0.0)
    verts.extend(end)
    glows.extend((0.0, 0.0))
    ends = (len(verts) - 2, len(verts) - 1)
    faces, kinds = _surfaces(rows, cols, wrap, ends, fan_row)
    cut = _cut_edges(_border(rows, cols, wrap, ends[0]), rows * cols, ends)
    materials = [_material(kind, f, glows, mats) for f, kind in zip(faces + cut, kinds + ["section"] * len(cut))]
    return egg.build_mesh(name, verts, faces + cut, materials, glows)


def _surfaces(rows, cols, wrap, ends, fan_row):
    """The outer and inner surfaces' quads and fans, each face with its kind ("outside", "inside")."""
    faces, kinds, span = [], [], cols if wrap else cols - 1
    for side, kind in ((0, "outside"), (1, "inside")):
        base = side * rows * cols
        for r in range(rows - 1):
            for c in range(span):
                a, b = base + r * cols + c, base + r * cols + (c + 1) % cols
                faces.append((a, b, b + cols, a + cols))
        for c in range(span):
            faces.append((base + fan_row * cols + c, base + fan_row * cols + (c + 1) % cols, ends[side]))
        kinds += [kind] * ((rows - 1) * span + span)
    return faces, kinds


def _border(rows, cols, wrap, tip):
    """The outer vertices round the piece's open border, in order (a closed loop)."""
    if wrap:
        return [(rows - 1) * cols + c for c in range(cols)]
    right = [r * cols + cols - 1 for r in range(1, rows)]
    left = [r * cols for r in range(rows - 1, 0, -1)]
    return list(range(cols)) + right + [tip] + left


def _cut_edges(loop, inner, ends):
    """Quads from each outer border edge to its inner twin (the tip's twin is the inner tip)."""
    twin = {ends[0]: ends[1]}
    return [(a, b, twin.get(b, b + inner), twin.get(a, a + inner)) for a, b in zip(loop, loop[1:] + loop[:1])]


def _material(kind, face, glows, mats):
    if kind == "outside":
        return egg.shell_material(glows, face, mats["shell"], mats["glow"])
    return mats[kind]

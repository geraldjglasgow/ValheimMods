"""The Carapace Pack's bag as a smooth surface: bag_point() maps a point of the cube [-1, 1]^3 onto the bag, fitted
to the player's back, and net() unfolds the cube round the bag's back so the shell plates and the band can be laid on
it as grids. Metres in the player's frame (Z up, the wearer faces -Y, the bag behind at +Y).
"""
import math

from mathutils import Vector

import forms

BOTTOM, TOP = 1.13, 1.605               # the bag's height range
PRESS = 0.006                           # how far the bag's front sinks into the back

# The player's back at x = 0 in the rest pose: (height, y of the skin).
BACK = [(1.00, 0.166), (1.04, 0.157), (1.08, 0.149), (1.12, 0.144), (1.16, 0.141), (1.20, 0.143), (1.24, 0.145),
        (1.28, 0.157), (1.32, 0.170), (1.36, 0.182), (1.40, 0.192), (1.44, 0.201), (1.48, 0.203), (1.52, 0.196),
        (1.56, 0.189), (1.60, 0.173), (1.64, 0.148)]


def back_line(z):
    """The player's back (y of the skin at x = 0) at height z."""
    if z <= BACK[0][0]:
        return BACK[0][1]
    for (z0, y0), (z1, y1) in zip(BACK, BACK[1:]):
        if z <= z1:
            return y0 + (y1 - y0) * (z - z0) / (z1 - z0)
    return BACK[-1][1]


def front_y(x, z):
    """The bag's front: on the back's centre line, curling forward a little towards its sides."""
    wrap = 0.05 + (0.035 - 0.05) * min(1.0, max(0.0, (z - 1.26) / 0.10))
    return back_line(z) - wrap * (x / 0.19) ** 2 - PRESS


def bag_point(param):
    """A point of the bag for a point of the cube [-1, 1]^3: a rounded body, broad under the shoulders and tapering
    to a narrower, rounded bottom like a beetle's abdomen, fullest a little below the middle."""
    a, d, h = forms.rounded(param, 4.0)
    v = (h + 1) / 2
    x = a * (0.185 - 0.07 * (1 - v) ** 2)
    z = BOTTOM + v * (TOP - BOTTOM)
    front = front_y(x, z)
    back = 0.335 + 0.03 * math.sin(math.pi * v) - 0.02 * v + 0.01 * (1 - a * a)
    return Vector((x, front + (d + 1) / 2 * (back - front), z))


def net(c, r):
    """The cube's surface unfolded round the back: column c across (|c| <= 1 on the back face, beyond it forward
    along the sides), row r up (|r| <= 1 on the back face, beyond it forward over the top or under the bottom)."""
    d = 1.0 - max(abs(c) - 1.0, 0.0) - max(abs(r) - 1.0, 0.0)
    return (max(-1.0, min(1.0, c)), max(-1.0, d), max(-1.0, min(1.0, r)))


def row_for(c, z):
    """The net's row (on the back and sides, |row| <= 1) where column c is at height z, so edges laid by height stay
    level all the way round the bag."""
    low, high = -1.0, 1.0
    for _ in range(40):
        middle = (low + high) / 2
        if bag_point(net(c, middle)).z < z:
            low = middle
        else:
            high = middle
    return (low + high) / 2


def surface_grid(columns, rows_of):
    """Points of the bag for a grid of the net: `rows_of(c)` gives each column's rows (top first); returns the grid
    (rows of points) and its outward normals."""
    points = [[bag_point(net(c, r)) for c, r in zip(columns, row)] for row in zip(*[rows_of(c) for c in columns])]
    centre = Vector((0.0, 0.25, 1.38))
    return points, forms.grid_normals(points, [[p - centre for p in row] for row in points])

"""The Deathsquito Queen's body: thorax, head, eyes, needle, palps, antennae, the crown of spikes, the swollen
abdomen of six overlapping plates with its glowing belly, and the stinger. Laid out on the game Deathsquito's parts at
2.5 times (BRIEF.md section 3), the abdomen fattened; metres, Z up, front -Y."""
import math

from mathutils import Vector

import queen_geo as geo

X, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, 1.0))
THORAX = [  # y, half width, half height above and below the middle, middle height: a hump highest before the wings
    (-0.35, 0.09, 0.08, 0.08, 1.22), (-0.29, 0.16, 0.16, 0.15, 1.26), (-0.19, 0.21, 0.24, 0.21, 1.30),
    (-0.07, 0.23, 0.27, 0.23, 1.32), (0.05, 0.22, 0.25, 0.22, 1.31), (0.15, 0.19, 0.20, 0.19, 1.28),
    (0.23, 0.13, 0.13, 0.13, 1.24), (0.29, 0.07, 0.07, 0.07, 1.21)]
HEAD = [(-0.31, 0.08, 0.08, 0.08, 1.21), (-0.38, 0.13, 0.12, 0.11, 1.20), (-0.46, 0.13, 0.115, 0.10, 1.18),
        (-0.53, 0.09, 0.08, 0.07, 1.15), (-0.585, 0.045, 0.045, 0.04, 1.13)]
EYE = ((0.08, -0.45, 1.245), (0.062, 0.075, 0.07))     # high on the head's front, as the game's beads are
CROWN = [(0.0, 0.36, 0.07), (0.5, 0.30, 0.062), (1.0, 0.22, 0.055)]   # angle from the top, length, base radius
ABDOMEN_Y = (0.26, 1.42)
ENVELOPE = [  # t along the abdomen, half width, half height above and below its centre line: an egg, fattest behind
    (0.0, 0.07, 0.07, 0.07), (0.08, 0.15, 0.15, 0.16), (0.2, 0.24, 0.24, 0.28), (0.4, 0.32, 0.31, 0.39),
    (0.58, 0.34, 0.31, 0.41), (0.75, 0.28, 0.25, 0.33), (0.88, 0.18, 0.16, 0.20), (0.96, 0.09, 0.08, 0.09),
    (1.0, 0.04, 0.04, 0.04)]
SEGMENTS, SIDES = 6, 12
PLATE_ANGLE, HOT_ANGLE = math.radians(100), math.radians(150)   # from the top: plates above, the hottest glow below


def build(m):
    """m: the materials by name (queen_paint)."""
    _lofted("thorax", THORAX, m["chitin"], sides=10)
    _lofted("head", HEAD, m["chitin"], sides=10)
    for side in (1, -1):
        geo.knob(f"eye{side}", geo.mirrored(EYE[0], side), EYE[1], m["eye"], segments=10, rings=6)
    _mouth(m)
    _crown(m["horn"])
    abdomen(m)
    stinger(m["horn"])


def _lofted(name, table, material, sides):
    """Rings across Y from a (y, half width, half height above, below, middle height) table, capped."""
    rings = [geo.ring(Vector((0.0, y, zc)), X, Z, rx, above, sides, phase=math.pi / 2, ry_below=below)
             for y, rx, above, below, zc in table]
    return geo.loft(name, rings, material)


def _mouth(m):
    """The needle drooping a metre ahead, two palps beside it and two antennae swept forward and up."""
    needle = geo.catmull([(0, -0.56, 1.13), (0, -0.85, 1.10), (0, -1.15, 1.05), (0, -1.42, 0.98)], 2)
    geo.tube("needle", needle, geo.taper(needle, 0.032, 0.004, 0.8), m["joint"], sides=6)
    for side in (1, -1):
        palp = [geo.mirrored(p, side) for p in ((0.03, -0.54, 1.11), (0.045, -0.68, 1.09), (0.055, -0.82, 1.05))]
        geo.tube(f"palp{side}", palp, geo.taper(palp, 0.024, 0.011), m["chitin"], sides=5)
        feeler = geo.catmull([geo.mirrored(p, side) for p in ((0.05, -0.53, 1.24), (0.13, -0.66, 1.33),
                                                               (0.22, -0.76, 1.43), (0.30, -0.82, 1.52))], 2)
        geo.tube(f"antenna{side}", feeler, geo.taper(feeler, 0.022, 0.005), m["joint"], sides=5)


def _crown(material):
    """Five spikes in an arc across the thorax's front, rising out of the shell and curling back."""
    y, rx, above, _, zc = THORAX[2]
    for angle, length, radius in CROWN:
        for side in ((1,) if angle == 0 else (1, -1)):
            a = angle * side
            root = Vector((rx * math.sin(a) * 0.8, y - 0.01, zc + above * math.cos(a) * 0.8))
            direction = Vector((math.sin(a) * 0.8, -0.35, math.cos(a)))
            curl = Vector((math.sin(a) * 0.04, length * 0.35, -length * 0.05))
            geo.spike(f"crown{a:+.1f}", root, direction, length, radius, material, curl=curl)


def abdomen(m):
    """Six plates, each overhanging the next's soft joint: green plates on top, the belly glowing below and hottest
    in the middle of each plate, near-black at the joints."""
    rows, kinds = _abdomen_rings()
    rings = [geo.ring(_spine(t), X, Z, rx * s, above * s, SIDES, phase=math.pi / 2 - math.pi / SIDES,
                      ry_below=below * s) for t, s, (rx, above, below) in rows]
    faces = []
    for kind in kinds:
        faces += [_face_material(m, kind, k) for k in range(SIDES)]
    return geo.loft("abdomen", rings, m["chitin"], face_materials=faces, cap_material=m["joint"])


def _abdomen_rings():
    """(t, scale, envelope) per ring and the kind of each band between rings: neck, joint, front, back."""
    starts = [0.04 + i * (0.97 - 0.04) / SEGMENTS for i in range(SEGMENTS + 1)]
    rows, kinds = [(0.0, 1.0), (0.025, 1.0)], ["neck"]
    for a, b in zip(starts, starts[1:]):
        rows += [(a + 0.01, 0.88), (a + 0.035, 0.98), ((a + b) / 2 + 0.01, 1.02), (b - 0.004, 1.03)]
        kinds += ["neck" if a == starts[0] else "joint"] * 2 + ["front", "back"]
    rows += [(0.985, 0.7), (1.0, 0.35)]
    kinds += ["joint", "joint"]
    return [(t, s, _envelope(t)) for t, s in rows], kinds


def _face_material(m, kind, k):
    """Plates on top, their joints only a crease as on the game's; below, the glow (hottest round the bottom) cut into
    stripes by near-black joints; shell on the neck."""
    centre = math.pi / 2 - math.pi / SIDES + 2 * math.pi * (k + 0.5) / SIDES
    from_top = abs((centre - math.pi / 2 + math.pi) % (2 * math.pi) - math.pi)
    if kind == "neck" or from_top <= PLATE_ANGLE:
        return m["chitin"]
    if kind == "joint":
        return m["joint"]
    return m["glow_hot"] if from_top >= HOT_ANGLE else m["glow"]


def _spine(t):
    """The abdomen's centre line: out of the thorax's back, sagging with the eggs' weight."""
    y = ABDOMEN_Y[0] + (ABDOMEN_Y[1] - ABDOMEN_Y[0]) * t
    s = min(t / 0.7, 1.0)
    return Vector((0.0, y, 1.21 - 0.16 * s * s * (3 - 2 * s)))


def _envelope(t):
    """(half width, half height above, below) at t, linear between the table's rows."""
    for (t0, *r0), (t1, *r1) in zip(ENVELOPE, ENVELOPE[1:]):
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            return tuple(a + (b - a) * f for a, b in zip(r0, r1))
    return tuple(ENVELOPE[-1][1:])


def stinger(material):
    """A curved amber sting out of the abdomen's tip, hooking down."""
    tip = _spine(1.0)
    points = geo.catmull([tip + Vector((0, -0.05, 0)), tip + Vector((0, 0.07, -0.02)),
                          tip + Vector((0, 0.16, -0.06)), tip + Vector((0, 0.22, -0.13))], 1)
    geo.tube("stinger", points, geo.taper(points, 0.05, 0.004, 0.9), material, sides=6, cap_start=False)

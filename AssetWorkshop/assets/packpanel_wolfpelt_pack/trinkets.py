"""The Wolfpelt Pack's small parts: silver buckles on the straps, round silver clasps pinning the fur's hem to the
straps, and the wolf fang charm hanging from a buckle on a leather thong with a silver cap. Coordinates are metres
in the player's frame (Z up, the wearer faces -Y).
"""
import math

from mathutils import Vector

import forms


def buckle(name, left, right, along, normal, material, thickness):
    """A silver buckle lying on a strap: a rectangular frame, a middle bar and a tongue. Returns its centre and its
    down-the-strap direction, for anything hung from it."""
    across = (right - left).normalized()
    along = (along - across * along.dot(across)).normalized()
    normal = across.cross(along) if across.cross(along).dot(normal) > 0 else -across.cross(along)
    centre = (left + right) / 2 + normal * (thickness + 0.002)
    w, h, t, bar = 0.024, 0.019, 0.0024, 0.0032
    axes = (across, along, normal)
    for part, offset, half in (("top", along * (h - bar), (w, bar, t)), ("bottom", -along * (h - bar), (w, bar, t)),
                               ("left", -across * (w - bar), (bar, h, t)), ("right", across * (w - bar), (bar, h, t)),
                               ("bar", Vector(), (w, bar * 0.8, t)),
                               ("tongue", along * h * 0.45 + normal * 0.001, (bar * 0.55, h * 0.5, t * 0.8))):
        forms.oriented_box(f"{name}_{part}", centre + offset, axes, half, material)
    return centre, along, normal


def disc(batch, centre, normal, radius, height, sides=8, turn=0.0):
    """A short cylinder standing on `centre` along `normal` (a flat plate, a boss, a cap)."""
    a = normal.orthogonal().normalized()
    b = normal.cross(a)
    rings = []
    for level in (0.0, height):
        rings.append([centre + normal * level + (a * math.cos(angle) + b * math.sin(angle)) * radius
                      for angle in (turn + 2 * math.pi * k / sides for k in range(sides))])
    batch.tube(rings)


def clasp(batch, centre, normal):
    """A round silver brooch: a plate with a bevelled rim, a raised boss and a small knob."""
    disc(batch, centre - normal * 0.004, normal, 0.030, 0.006, sides=10)
    disc(batch, centre + normal * 0.002, normal, 0.023, 0.003, sides=10, turn=math.pi / 10)
    disc(batch, centre + normal * 0.005, normal, 0.012, 0.005, sides=8)
    disc(batch, centre + normal * 0.010, normal, 0.005, 0.003, sides=6)


def fang_charm(kit, hook, out):
    """From `hook` (the bottom of a buckle) a short leather thong, a silver cap, then the fang, hanging straight
    down in the rest pose and curving out to the wearer's left like a claw. `out` is the surface's outward normal
    there, to keep the charm clear of the leather."""
    down, side = Vector((0.0, 0.0, -1.0)), Vector((1.0, 0.0, 0.0))
    away = (out - down * out.dot(down)).normalized()
    top = hook + away * 0.004
    thong = forms.Batch()
    loop = top + down * 0.034 + away * 0.004
    _cord(thong, top, loop)
    thong.build("fang_thong", kit["strap"])
    metal = forms.Batch()
    disc(metal, loop + down * 0.013, -down, 0.0095, 0.014, sides=8)
    disc(metal, loop + down * 0.001, -down, 0.0070, 0.004, sides=8)
    metal.build("fang_cap", kit["silver"])
    _fang(loop + down * 0.010, down, side, away, kit["ivory"])


def _cord(batch, start, end, radius=0.0028):
    """A thin four-sided cord from start to end."""
    along = (end - start).normalized()
    a = along.orthogonal().normalized()
    b = along.cross(a)
    ring = [a * radius, b * radius, -a * radius, -b * radius]
    batch.tube([[start + p for p in ring], [end + p for p in ring]])


def _fang(root, down, side, away, material, length=0.085, radius=0.0115, sides=7):
    """A curved, tapering fang: round at the root, flattened towards its point, curving out along `side`."""
    batch, rings, values = forms.Batch(), [], []
    steps = (0.0, 0.22, 0.45, 0.65, 0.82)
    for t in steps:
        spine = root + down * length * t + side * 0.028 * t * t + away * 0.006 * t
        tangent = (down * length + side * 0.056 * t + away * 0.006).normalized()
        a = (side - tangent * side.dot(tangent)).normalized()
        b = tangent.cross(a)
        r = radius * (1.0 - t) ** 0.75 * (1.0 + 0.25 * math.sin(math.pi * min(1.0, t * 1.6)))
        rings.append([spine + a * math.cos(k * 2 * math.pi / sides) * r
                      + b * math.sin(k * 2 * math.pi / sides) * r * 0.78 for k in range(sides)])
        values += [t] * sides
    tip = root + down * length + side * 0.028 + away * 0.006
    batch.tube(rings, tip, {"tooth": values + [1.0]})
    return batch.build("fang", material)

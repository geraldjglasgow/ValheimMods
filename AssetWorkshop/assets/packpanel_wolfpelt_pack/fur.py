"""The wolf fur mantle: a big shaggy grey-white pelt draped over the bag's top and down its back, built as layers of
fur laid like shingles. Each layer is a strip whose lower edge breaks into broad, rounded locks: domed across (a
valley, a shoulder, the crown, a shoulder, a valley), narrowing to a blunt, rounded end that curls out a little.
About half the locks part at the end into two soft prongs, so the edges read as clumped fur rather than leaves.
Every layer lies over the top of the next, so the locks of the upper layer rest on the layer below and overlap it
well. The last layer's locks are longer and make the ragged hem, soft tufts rather than points.

Mantle coordinates: u runs across (|u| = 1 is x = 0.17, beyond it the mantle wraps down over the bag's sides), s is
the arc length along PATH, from the top edge against the wearer's back, over the top and down the back. Every point
is found on the bag's nearest surface and lifted along its normal, so the mantle follows the leather.
"""
import math
import random

from mathutils import Vector, noise

import forms

# The mantle's centre line in the side view (y, z), a little outside the bag: laid on the bag's nearest surface.
PATH = [(0.15, 1.625), (0.26, 1.632), (0.35, 1.60), (0.378, 1.52), (0.386, 1.40), (0.386, 1.00)]
SIDE_X, UNIT = 0.21, 0.17          # where the side columns aim; metres per u across the top
EDGE = 1.55                        # |u| of the mantle's side edges
LAYERS, FIRST, LAST = 4, 0.075, 0.40   # layer count; lock bases of the first and last layer (middle of the back)
REACH = 0.15                       # how far above its lock bases a layer starts (under the layer above)
LIFT = 0.007                       # a layer's lift off the leather where it starts
# Across one lock, per column kind (valley, shoulder, crown): height of the lock's base above LIFT, and the "fur"
# attribute's root-to-tip value there.
BASE_HEIGHT = {"V": 0.010, "S": 0.024, "R": 0.030}
BASE_ALONG = {"V": 0.50, "S": 0.54, "R": 0.56}
KINDS = "VSRS"                     # column kinds repeating across a layer, ending in a valley
# Down a lock: (fraction of its length reached at the edge, the shoulders and the crown; width; lift above LIFT at
# the edge, the shoulders and the crown; root-to-tip value). The edges end sooner than the crown (a rounded end)
# and sit low, tucked into the fur below, so each lock is a dome rather than a loose sheet.
LOCK_ROWS = (((0.44, 0.46, 0.47), 0.84, (0.008, 0.026, 0.033), 0.74),
             ((0.80, 0.85, 0.88), 0.62, (0.010, 0.027, 0.034), 0.90))


def hem(u):
    """How far down the mantle reaches in column u, relative to the middle of the back: shorter over the sides."""
    k = min(1.0, abs(u) / EDGE)
    return 1.0 - 0.2 * k * k


def bulk(s):
    """Extra lift down the back, where the fur may be thick; kept low on the top, near the head."""
    return 0.020 * min(1.0, max(0.0, (s - 0.18) / 0.16))


def _path(s):
    """(point, outward normal) in the side view at arc length s along PATH."""
    for (y0, z0), (y1, z1) in zip(PATH, PATH[1:]):
        length = math.hypot(y1 - y0, z1 - z0)
        if s <= length or (y1, z1) == PATH[-1]:
            t = max(0.0, s) / length
            dy, dz = (y1 - y0) / length, (z1 - z0) / length
            return Vector((y0 + (y1 - y0) * t, z0 + (z1 - z0) * t)), Vector((-dz, dy))
        s -= length


class Mantle:
    def __init__(self, bag_tree):
        self.tree = bag_tree

    def target(self, u, s):
        point, out = _path(s)
        if abs(u) <= 1.0:
            return Vector((u * UNIT, point.x, point.y))
        drop = (abs(u) - 1.0) * UNIT * 1.25
        return Vector((math.copysign(SIDE_X, u), point.x - out.x * drop, point.y - out.y * drop))

    def lay(self, u, s, height):
        """The point `height` above the leather at mantle coordinates (u, s), plus a little lumpiness."""
        hit, normal, _, _ = self.tree.find_nearest(self.target(u, s))
        lump = noise.noise(hit * 11.0) * 0.004
        side = 1.0 - 0.75 * abs(normal.x)          # thinner where the leather faces sideways, towards the arms
        return hit + normal * ((height + bulk(s) + lump) * side)

    def layers(self, material):
        strips = []
        for r in range(LAYERS):
            base = FIRST + (LAST - FIRST) * r / (LAYERS - 1)
            last = r == LAYERS - 1
            strip = self._layer(f"fur_layer_{r}", base, 8 if r % 2 else 9, r, last, material)
            if last:
                _thicken(strip, 0.005)
            strips.append(strip)
        return strips

    def _layer(self, name, base, locks, r, last, material):
        """One strip: valley, shoulder, crown and shoulder columns across, three rows down to the lock bases, a
        rounded lock below each crown."""
        strip = _Strip(r)
        count = len(KINDS) * locks
        us = [-EDGE + 2 * EDGE * k / count for k in range(count + 1)]
        step = 2 * EDGE / count
        for k in range(2, count, 4):                     # crowns and the valleys between locks wander a little
            us[k] += random.uniform(-0.5, 0.5) * step
        for k in range(4, count, 4):                     # so the locks come in different widths
            us[k] += random.uniform(-1.1, 1.1) * step
        for k in range(1, count, 2):                     # shoulders stay halfway
            us[k] = (us[k - 1] + us[k + 1]) / 2
        columns = [self._column(strip, u, base, KINDS[k % 4]) for k, u in enumerate(us)]
        for k in range(count):
            strip.band(columns[k], columns[k + 1])
        for k in range(0, count, 4):
            self._lock(strip, us[k:k + 5], [c[-1] for c in columns[k:k + 5]], base * hem(us[k + 2]), r, last)
        return strip.build(name, material)

    def _column(self, strip, u, base, kind):
        """The strip's column at u, from its top edge (tucked under the layer above) down to the lock base."""
        end = base * hem(u) - (random.uniform(0.0, 0.012) if kind == "V" else 0.0)
        top = max(end - REACH, 0.0)
        rows = [(top, LIFT, 0.14), (max(end - REACH * 0.45, top + 0.02), LIFT + 0.011, 0.34),
                (end, LIFT + BASE_HEIGHT[kind], BASE_ALONG[kind])]
        return [strip.vertex(self, u, s, height, along) for s, height, along in rows]

    def _lock(self, strip, us, bases, start, r, last):
        """A broad lock below the crown us[2]: two rows narrowing towards a blunt end curling out a little, leaning
        into a soft S."""
        length = random.uniform(0.10, 0.17) if last else random.uniform(0.085, 0.145)
        shift, crown = random.uniform(-0.07, 0.07), us[2]
        bend = -shift * random.uniform(0.2, 0.5)
        rows = [bases]
        for (reach, width, lift, along), lean in zip(LOCK_ROWS, (bend, shift * 0.7)):
            rows.append([strip.vertex(self, crown + (u - crown) * width + lean, start + length * reach[_rank(k)],
                                      LIFT + lift[_rank(k)], along) for k, u in enumerate(us)])
        for a, b in zip(rows, rows[1:]):
            strip.band(b, a)                             # rows run across: swapped, to wind like the columns
        near = rows[-1]
        if random.random() < 0.5:
            tip = self._tip(strip, crown + shift, start + length, r)
            strip.faces += [(near[k], near[k + 1], tip) for k in range(len(near) - 1)]
            return
        prongs = [self._tip(strip, crown + (us[k] - crown) * 0.5 + shift, start + length * random.uniform(0.9, 1.0), r)
                  for k in (1, 3)]
        strip.faces += [(near[0], near[1], prongs[0]), (near[1], near[2], prongs[0]),
                        (near[2], near[3], prongs[1]), (near[3], near[4], prongs[1])]

    def _tip(self, strip, u, s, r):
        """A lock's end, curling out a little (less on the top, near the head)."""
        curl = random.uniform(0.004, 0.008) if r == 0 else random.uniform(0.008, 0.014)
        return strip.vertex(self, u, s, LIFT + 0.026 + curl, 1.0)


def _rank(k):
    """0 for a lock's edge columns, 1 for its shoulders, 2 for its crown (k = 0..4 across the lock)."""
    return (0, 1, 2, 1, 0)[k]


class _Strip:
    """Vertices, faces and the "fur" coordinates of one layer."""

    def __init__(self, r):
        self.verts, self.faces, self.values, self.r = [], [], [], r

    def vertex(self, mantle, u, s, height, along):
        self.verts.append(mantle.lay(u, s, height))
        self.values.append((u * 1.2 + self.r * 3.1, self.r * 1.7, along))
        return len(self.verts) - 1

    def band(self, a, b):
        """Quads between two neighbouring rows or columns of vertex indices."""
        self.faces += [(a[i], b[i], b[i + 1], a[i + 1]) for i in range(len(a) - 1)]

    def build(self, name, material):
        obj = forms.mesh_object(name, self.verts, self.faces, material)
        if _faces_inward(obj):
            obj.data.flip_normals()
        forms.point_vectors(obj, "fur", self.values)
        return obj


def _thicken(obj, thickness):
    """Gives the strip its thickness on the leather's side (the pipeline applies the modifier)."""
    mod = obj.modifiers.new("thickness", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = -1.0
    mod.use_even_offset = False                   # even thickness shoots thin prong corners far out


def _faces_inward(obj):
    """True when the strip's normals point into the bag (towards its middle) rather than away from it."""
    centre = Vector((0.0, 0.25, 1.37))
    inward = sum(1 for p in obj.data.polygons if p.normal.dot(p.center - centre) < 0)
    return inward > len(obj.data.polygons) / 2

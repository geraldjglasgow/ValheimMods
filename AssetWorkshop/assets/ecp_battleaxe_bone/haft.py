"""The haft and what is tied to it: a giant's leg bone (knuckle pommel, knob above the head, a slight bow), a leather
grip, the rawhide lashing that holds the blade's root against the bone, and a tusk through the lashing as the back
spike. Z up; the origin is the lower hand, BUTT metres above the butt, as on the game's battleaxes."""
import math

from mathutils import Vector

import geo

BUTT, TOP = -0.280, 1.465            # the shaft's ends; knobs reach a little past them
BOW = -0.011                         # the bone bows back, away from the blade, by this much at its middle
SHAFT = [(-0.240, 0.039, 0.034), (-0.200, 0.035, 0.031), (-0.140, 0.031, 0.028), (0.050, 0.029, 0.027),
         (0.350, 0.027, 0.025), (0.520, 0.028, 0.024), (0.700, 0.026, 0.025), (0.860, 0.027, 0.024),
         (1.000, 0.027, 0.026), (1.200, 0.030, 0.027), (1.330, 0.033, 0.030), (1.400, 0.037, 0.033),
         (1.440, 0.041, 0.036)]   # z, rx, ry: an hourglass, the knuckle and hip ends flared
WOBBLE = [(0.0, 0.0), (0.001, 0.0), (0.0, 0.001), (-0.001, 0.0), (0.0, 0.0), (0.0015, -0.001), (-0.001, 0.0015),
          (0.001, 0.0), (0.0, -0.001), (-0.001, 0.0), (0.0, 0.0), (0.0, 0.0), (0.0, 0.0)]   # a grown, not turned, line
GRIP = (-0.150, 0.345)               # leather from the pommel's flare up past the upper hand
COLLAR = [(1.098, 0.90), (1.111, 1.0), (1.165, 1.05), (1.290, 1.05), (1.347, 1.0), (1.360, 0.90)]  # z, girth
COLLAR_LOOP = [(-0.043, 0.0), (-0.032, -0.032), (0.0, -0.041), (0.032, -0.031), (0.062, -0.021), (0.090, -0.019),
               (0.101, 0.0), (0.090, 0.019), (0.062, 0.021), (0.032, 0.031), (0.0, 0.041), (-0.032, 0.032)]
TUSK = [(-0.025, 1.228), (-0.105, 1.236), (-0.180, 1.210), (-0.240, 1.155), (-0.270, 1.088)]
TUSK_R = (0.034, 0.028, 0.020, 0.011, 0.002)


def centre(z):
    f = (z - BUTT) / (TOP - BUTT)
    return Vector((BOW * math.sin(math.pi * f), 0.0, z))


def build(bone, leather, rawhide, ivory):
    return [_shaft(bone), *_pommel(bone), *_crown(bone), _grip(leather), *_lashing(rawhide), _tusk(ivory)]


def _shaft(bone):
    points = [centre(z) + Vector((dx, dy, 0.0)) for (z, _, _), (dx, dy) in zip(SHAFT, WOBBLE)]
    return geo.tube('haft', points, [(rx, ry) for _, rx, ry in SHAFT], bone, sides=8, phase=math.pi / 8,
                    cap_start=True, cap_end=True)


def _pommel(bone):
    """The knee end of the bone: two condyles side by side, a groove between them."""
    c = centre(BUTT)
    return [geo.knob('condyle_back', c + Vector((-0.021, 0.0, 0.028)), (0.030, 0.034, 0.036), bone),
            geo.knob('condyle_front', c + Vector((0.020, 0.002, 0.024)), (0.027, 0.031, 0.033), bone)]


def _crown(bone):
    """The hip end above the head: the ball leaning back on its neck, the trochanter knob in front of it."""
    c = centre(TOP)          # the shaft ends at 1.44, inside both knobs, so no cut end shows
    return [geo.knob('femur_head', c + Vector((-0.012, 0.0, -0.022)), (0.050, 0.046, 0.048), bone),
            geo.knob('trochanter', c + Vector((0.026, 0.0, -0.032)), (0.029, 0.031, 0.036), bone, segments=7)]


def _grip(leather):
    """A leather sleeve with a raised turn at each end; the wrap's spiral is painted."""
    low, high = GRIP
    stations = [(low - 0.012, -0.002), (low, 0.009), (low + 0.018, 0.009), (low + 0.024, 0.005),
                (high - 0.024, 0.005), (high - 0.018, 0.009), (high, 0.009), (high + 0.012, -0.002)]
    points = [centre(z) for z, _ in stations]
    radii = [(_radius(z)[0] + extra, _radius(z)[1] + extra) for z, extra in stations]
    return geo.tube('grip', points, radii, leather, sides=8, phase=math.pi / 8, cap_start=False, cap_end=False)


def _radius(z):
    """The shaft's (rx, ry) at height z, linear between stations."""
    for (z0, ax, ay), (z1, bx, by) in zip(SHAFT, SHAFT[1:]):
        if z0 <= z <= z1:
            f = (z - z0) / (z1 - z0)
            return ax + (bx - ax) * f, ay + (by - ay) * f
    return SHAFT[0][1:] if z < SHAFT[0][0] else SHAFT[-1][1:]


def _lashing(rawhide):
    """A thick wrap of rawhide round the bone and the blade's root, bulging where it was pulled tight; the turns of
    the wrap are painted."""
    rings = []
    for z, girth in COLLAR:
        c = centre(z)
        rings.append([c + Vector((x * girth, y * girth, 0.0)) for x, y in COLLAR_LOOP])
    return [geo.loft('lashing', rings, rawhide, smooth=True)]


def _tusk(ivory):
    """A boar's or troll's tusk pushed through the lashing behind the bone, curving down: the back spike."""
    points = [Vector((x, 0.0, z)) for x, z in TUSK]
    points = [points[0] + Vector((0.030, 0, 0))] + points
    radii = [(r * 0.85, r) for r in (TUSK_R[0],) + TUSK_R]
    return geo.tube('tusk', points, radii, ivory, sides=6, hint=Vector((0, 1, 0)), cap_start=True, cap_end=True)

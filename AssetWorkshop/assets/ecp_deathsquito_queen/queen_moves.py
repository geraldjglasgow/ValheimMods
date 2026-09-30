"""The Queen's moves as numbers over time (MOVES.md holds the same timings; change both together). Pure maths, no
Blender scene: each move turns a start state and its targets into her state at any moment of it, plus the events that
happen in it (sounds, needles, eggs, waves, hits). queen_preview.py samples them frame by frame.

A state is where the body's middle is (metres, Blender axes, Z up), which way she faces (yaw: 0 looks down -Y, the
workshop's front), her pitch (nose down positive), bank (her left, +X, wing down positive) and the pose parameters the
rig turns into bones (queen_pose.py): wing lift and buzz, the abdomen's curl, the head's pitch, the legs' tuck and
what they reach for, how still she holds her aim.
"""
import math
from dataclasses import dataclass, field, replace

from mathutils import Vector

HOVER_Z = 2.6                     # the body's middle above the ground while she hovers
BODY_Z = 1.28                     # the body's middle above the rig's root (the model's origin)
TIP = Vector((0.0, -1.44, -0.30))  # from the body's middle, her own frame: the needle's tip (Mouth socket)
EGG_POINT = Vector((0.0, 1.45, -0.25))       # the abdomen's tip (EggPoint socket)
HEAD_PIVOT = Vector((0.0, -0.315, -0.1125))  # the Head bone's joint: the head pitches about it
ABDOMEN_PIVOT = Vector((0.0, 0.2, -0.0675))  # the Abdomen bone's joint: the abdomen curls about it
DROOP = 10.0                      # degrees the needle hangs below her level at rest


@dataclass
class State:
    pos: Vector
    yaw: float = 0.0
    pitch: float = 0.0
    bank: float = 0.0
    wing_lift: float = 0.0        # degrees, both wings up
    buzz: float = 1.0             # the buzz's size, 1 as she hovers
    curl: float = 0.0             # degrees, abdomen tip down
    head: float = 0.0             # degrees, needle down
    tuck: float = 0.0             # 0 legs hanging, 1 drawn up and back
    steady: float = 0.0           # 1 holds the proboscis and her flying posture still on her aim (queen_sway)
    brace: float = 0.0            # legs drawn up under her for an effort (the lunge's draw-back, the volley, the whirl)
    reach: float = 0.0            # front legs thrust forward, hind trailing (the lunge, the rear before the volley)
    spread: float = 0.0           # legs splayed and clutching (laying eggs)

    def copy(self, **changes):
        changes["pos"] = changes["pos"].copy() if "pos" in changes else self.pos.copy()
        return replace(self, **changes)


@dataclass
class Event:
    time: float                   # seconds into the move
    kind: str                     # sound, needle, egg, wave, hit
    data: dict = field(default_factory=dict)


def smooth(x):
    x = min(max(x, 0.0), 1.0)
    return x * x * (3.0 - 2.0 * x)


def span(t, a, b):
    """How far t is through [a, b], eased, 0 before and 1 after."""
    return smooth((t - a) / (b - a)) if b > a else float(t >= b)


def lerp(a, b, f):
    return a + (b - a) * f


def yaw_to(pos, target):
    d = target - pos
    return math.atan2(d.x, -d.y)


def pitch_to(pos, target):
    d = target - pos
    return math.degrees(math.atan2(-d.z, d.xy.length))


def facing(yaw):
    return Vector((math.sin(yaw), -math.cos(yaw), 0.0))


def turn_toward(a, b, f):
    """An angle from a to b by the short way round."""
    return a + ((b - a + math.pi) % (2 * math.pi) - math.pi) * f


def bob(t, amount=0.06):
    return Vector((0.02 * math.sin(1.3 * t), 0.0, amount * math.sin(2 * math.pi * 0.7 * t)))


def rotate(v, s):
    """A vector in her frame (front -Y) to the world, by her yaw, pitch (nose down) and bank."""
    p, r, y = math.radians(s.pitch), math.radians(s.bank), s.yaw
    x1, z1 = v.x * math.cos(r) + v.z * math.sin(r), -v.x * math.sin(r) + v.z * math.cos(r)
    y2, z2 = v.y * math.cos(p) - z1 * math.sin(p), v.y * math.sin(p) + z1 * math.cos(p)
    return Vector((x1 * math.cos(y) - y2 * math.sin(y), x1 * math.sin(y) + y2 * math.cos(y), z2))


def unrotate(v, s):
    """A world vector in her own frame (front -Y, her left +X, her up +Z): rotate's inverse."""
    p, r, y = math.radians(s.pitch), math.radians(s.bank), s.yaw
    x1, y1 = v.x * math.cos(y) + v.y * math.sin(y), -v.x * math.sin(y) + v.y * math.cos(y)
    y2, z2 = y1 * math.cos(p) + v.z * math.sin(p), -y1 * math.sin(p) + v.z * math.cos(p)
    return Vector((x1 * math.cos(r) - z2 * math.sin(r), y2, x1 * math.sin(r) + z2 * math.cos(r)))


def pitch_about(v, pivot, degrees):
    """v turned about a pivot by `degrees` about her X axis (positive takes her front down)."""
    a, d = math.radians(degrees), v - pivot
    return pivot + Vector((d.x, d.y * math.cos(a) - d.z * math.sin(a), d.y * math.sin(a) + d.z * math.cos(a)))


def tip(s):
    """The needle's tip in the world, the head's pitch included."""
    return s.pos + rotate(pitch_about(TIP, HEAD_PIVOT, s.head), s)


def egg_point(s):
    """The abdomen's tip in the world, its curl included (a curl down turns the back end down)."""
    return s.pos + rotate(pitch_about(EGG_POINT, ABDOMEN_PIVOT, -s.curl), s)


class Move:
    """A stretch of the fight: `duration` seconds, `state(t)` at any moment, `events` in it, and `end` the state it
    leaves her in. Subclasses fill `_state` from `self.start` and their targets."""
    name, caption = "hover", ""

    def __init__(self, start, duration):
        self.start, self.duration, self.events = start, duration, []
        self.end = self.state(duration)

    def state(self, t):
        return self._state(min(max(t, 0.0), self.duration))

    def _state(self, t):
        s = self.start.copy()
        s.pos = s.pos + bob(t)
        return s


class Hover(Move):
    """Hanging in the air, turning to face `target`, drifting to `to` if given."""

    def __init__(self, start, duration, target, to=None, altitude=None):
        self.target, self.to, self.altitude = target, to, altitude
        super().__init__(start, duration)

    def _state(self, t):
        f = span(t, 0.0, min(self.duration, 1.0))
        s = self.start.copy(pitch=lerp(self.start.pitch, 0.0, f), bank=lerp(self.start.bank, 0.0, f),
                            curl=lerp(self.start.curl, 0.0, f), head=lerp(self.start.head, 0.0, f),
                            tuck=lerp(self.start.tuck, 0.0, f), wing_lift=lerp(self.start.wing_lift, 0.0, f),
                            steady=lerp(self.start.steady, 0.0, f), brace=lerp(self.start.brace, 0.0, f),
                            reach=lerp(self.start.reach, 0.0, f), spread=lerp(self.start.spread, 0.0, f),
                            buzz=1.0)
        goal = self.to if self.to is not None else self.start.pos
        if self.altitude is not None:
            goal = Vector((goal.x, goal.y, self.altitude))
        s.pos = self.start.pos.lerp(goal, span(t, 0.0, self.duration)) + bob(t)
        s.yaw = turn_toward(self.start.yaw, yaw_to(s.pos, self.target), span(t, 0.0, min(self.duration, 0.8)))
        return s

"""The Queen's seven moves (MOVES.md), each a queen_moves.Move: her state at any moment and the events in it."""
import math

from mathutils import Vector

from queen_moves import (Event, HOVER_Z, DROOP, Move, bob, facing, lerp, pitch_to, rotate, smooth, span, tip,
                         turn_toward, yaw_to, TIP)

CHEST = 1.3


def chest(player):
    return Vector((player.x, player.y, CHEST))


def tremble(t, amount=0.015):
    return Vector((amount * math.sin(97 * t), amount * math.sin(71 * t + 1), amount * math.sin(83 * t + 2)))


def pulse(t, at, length=0.12):
    """A quick bump from 0 up to 1 and back, `length` seconds long, starting at `at`."""
    x = (t - at) / length
    return math.sin(math.pi * x) if 0.0 <= x <= 1.0 else 0.0


class Dive(Move):
    """1: rise and back off, hold trembling on the line, then dart down it through the target and pull up."""
    name, caption = "dive", "1  Piercing dive"
    RISE, GO, ARRIVE, DURATION, OVERSHOOT = 0.5, 1.1, 1.45, 2.6, 3.0

    def __init__(self, start, target):
        self.target, aim = target, chest(target)
        self.hold = start.pos - facing(yaw_to(start.pos, aim)) * 0.5 + Vector((0, 0, 1.0))
        self.yaw, self.dive_pitch = yaw_to(self.hold, aim), 0.0
        for _ in range(4):        # the line the body runs so the needle's tip runs through the chest
            probe = start.copy(pos=self.hold, yaw=self.yaw, pitch=self.dive_pitch)
            self.dive_pitch = pitch_to(self.hold, aim - rotate(TIP, probe)) - DROOP
        line = aim - rotate(TIP, start.copy(pos=self.hold, yaw=self.yaw, pitch=self.dive_pitch)) - self.hold
        self.u, reach = line.normalized(), line.length
        self.length = reach + min(self.OVERSHOOT, max(0.0, (self.hold.z + self.u.z * reach - 1.3) / -self.u.z)
                                  if self.u.z < 0 else self.OVERSHOOT)
        self.hit_time = self.GO + (self.ARRIVE - self.GO) * reach / self.length
        super().__init__(start, self.DURATION)
        self.events = [Event(0.0, "sound", {"cue": "buzz_strain"}), Event(1.08, "sound", {"cue": "dive_whoosh"}),
                       Event(self.hit_time, "hit", {"at": aim, "cue": "pierce"})]

    def _state(self, t):
        s = self.start.copy(yaw=turn_toward(self.start.yaw, self.yaw, span(t, 0, self.RISE)))
        if t < self.GO:
            f = span(t, 0.0, self.RISE)
            s.pos = self.start.pos.lerp(self.hold, f) + tremble(t) * span(t, self.RISE, self.GO)
            s.pitch, s.tuck, s.buzz = self.dive_pitch * span(t, 0.2, 0.9), span(t, 0.2, 0.9), 1.0 + 0.6 * f
            s.steady = span(t, 0.3, 0.6)
            return s
        if t < self.ARRIVE:
            s.pos = self.hold + self.u * self.length * ((t - self.GO) / (self.ARRIVE - self.GO)) ** 1.15
            s.pitch, s.tuck, s.buzz, s.steady = self.dive_pitch, 1.0, 1.6, 1.0
            return s
        return self._recover(s, t)

    def _recover(self, s, t):
        end = self.hold + self.u * self.length
        back = end + Vector((self.u.x, self.u.y, 0)).normalized() * 1.4 + Vector((0, 0, HOVER_Z - end.z + 0.2))
        f = span(t, self.ARRIVE, self.DURATION)
        s.pos = end.lerp(back, 1 - (1 - f) ** 2) + bob(t) * f
        s.yaw = turn_toward(self.yaw, yaw_to(s.pos, self.target), span(t, self.ARRIVE + 0.15, 2.3))
        s.pitch = lerp(self.dive_pitch, -15.0, span(t, self.ARRIVE, 1.85)) * (1 - span(t, 1.85, self.DURATION))
        s.tuck, s.buzz = 1.0 - span(t, self.ARRIVE, 2.2), lerp(1.6, 1.0, f)
        s.steady = 1.0 - span(t, self.ARRIVE, self.ARRIVE + 0.4)
        return s


class Lunge(Move):
    """5: draw back with the head up, lunge 2 m with the needle, back off."""
    name, caption = "lunge", "5  Lunge"
    DURATION = 1.2

    def __init__(self, start, target):
        self.target = target
        super().__init__(start, self.DURATION)
        self.events = [Event(0.05, "sound", {"cue": "lunge"}),       # its sting's thump lands 0.4-0.47 s in
                       Event(0.5, "hit", {"at": chest(target), "cue": "pierce"})]

    def _state(self, t):
        ahead = facing(self.start.yaw)
        reach = -0.4 * span(t, 0.0, 0.35) + 2.4 * span(t, 0.35, 0.55) - 1.0 * span(t, 0.55, self.DURATION)
        s = self.start.copy()
        s.pos = self.start.pos + ahead * reach + bob(t, 0.03)
        s.pitch = -10 * span(t, 0, 0.35) + 25 * span(t, 0.35, 0.5) - 15 * span(t, 0.55, self.DURATION)
        s.head = -15 * span(t, 0, 0.35) + 25 * span(t, 0.35, 0.5) - 10 * span(t, 0.55, self.DURATION)
        s.buzz = 1.0 + 0.4 * span(t, 0.3, 0.5) - 0.4 * span(t, 0.6, 1.0)
        s.steady = span(t, 0.1, 0.3) * (1 - span(t, 0.6, 0.9))
        s.brace = span(t, 0.0, 0.3) * (1 - span(t, 0.35, 0.5))
        s.reach = span(t, 0.35, 0.5) * (1 - span(t, 0.65, 1.1))
        s.curl = 8 * span(t, 0.0, 0.35) - 14 * span(t, 0.35, 0.5) + 6 * span(t, 0.55, 1.0)   # the abdomen whips
        return s


class Whirl(Move):
    """7: dip and tilt, then zip 1.5 times round a 1.4 m circle, banked, yawed 50 degrees outward so the needle
    sweeps outside the circle; stop, wobble, face the target."""
    name, caption = "whirl", "7  Whirl"
    RADIUS, OUTWARD, BANK, DURATION = 1.4, math.radians(50), 35.0, 1.8

    def __init__(self, start, target):
        self.target = target
        self.centre = start.pos + facing(start.yaw) * 1.2 + Vector((0, 0, -0.3))
        side = start.pos - self.centre
        self.a0 = math.atan2(side.y, side.x)
        super().__init__(start, self.DURATION)
        self.events = [Event(0.3, "sound", {"cue": "zip_whir"})] + self._hits()

    def _angle(self, t):
        return self.a0 + 3 * math.pi * smooth((t - 0.3) / 1.0) + 0.3 * span(t, 1.3, 1.6)

    def _state(self, t):
        a = self._angle(t)
        on_circle = self.centre + Vector((math.cos(a), math.sin(a), 0)) * self.RADIUS
        s = self.start.copy(buzz=1.0 + 0.5 * span(t, 0, 0.3) - 0.5 * span(t, 1.3, 1.8), steady=0.0)
        s.pos = self.start.pos.lerp(on_circle, span(t, 0.0, 0.3)) + bob(t, 0.02)
        tangent_yaw = math.atan2(-math.sin(a), -math.cos(a))
        whirling = span(t, 0.1, 0.35) * (1 - span(t, 1.3, 1.7))
        s.yaw = turn_toward(self.start.yaw, tangent_yaw - self.OUTWARD, whirling)
        if t > 1.3:
            s.yaw = turn_toward(s.yaw, yaw_to(s.pos, self.target), span(t, 1.4, 1.8))
        s.bank = self.BANK * whirling + 6 * math.sin(20 * t) * span(t, 1.3, 1.5) * (1 - span(t, 1.5, 1.8))
        s.pitch, s.brace = 10 * whirling, 0.4 * whirling
        return s

    def _hits(self):
        """A hit where the needle's tip comes nearest the target (within 0.8 m)."""
        aim = chest(self.target)
        best = min((((tip(self.state(0.3 + i / 100)) - aim).length, 0.3 + i / 100) for i in range(101)))
        return [Event(best[1], "hit", {"at": aim, "cue": "pierce"})] if best[0] < 0.8 else []


class Spit(Move):
    """6: the head pulls back and aims, one fast needle at the target, a recoil."""
    name, caption = "spit", "6  Needle shot"
    DURATION, FIRE, SPEED = 0.8, 0.3, 45.0

    def __init__(self, start, target):
        self.target = target
        self.aim = pitch_to(tip(start), chest(target)) - DROOP
        super().__init__(start, self.DURATION)
        fired = self.state(self.FIRE)
        self.events = [Event(self.FIRE, "sound", {"cue": "needle_shot"}),
                       Event(self.FIRE, "needle", {"from": tip(fired), "to": chest(target), "speed": self.SPEED})]

    def _state(self, t):
        s = self.start.copy()
        back = 0.15 * span(t, 0, self.FIRE) + 0.2 * pulse(t, self.FIRE, 0.25)
        s.pos = self.start.pos - facing(self.start.yaw) * back + bob(t, 0.03)
        s.head = lerp(-20, self.aim, span(t, 0.1, self.FIRE)) - 12 * pulse(t, self.FIRE, 0.25)
        s.head *= 1 - span(t, 0.5, self.DURATION)
        s.steady = span(t, 0.05, 0.2) * (1 - span(t, 0.45, 0.7))
        s.brace, s.curl = 0.35 * span(t, 0, 0.25) * (1 - span(t, 0.5, 0.8)), -6 * pulse(t, self.FIRE, 0.25)
        return s

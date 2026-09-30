"""The living part of her motion: everything on her swings and flexes the way an insect's does, as damped springs driven
by her own motion in her own frame, on top of what each move poses.

- Legs, six chains (hip swung back and out, knee, tarsus), each with its own stiffness and idle sway so the six never
  move as one. Speeding up swings them back, braking forward past rest and back; turns and the whirl fling them out; a
  drop lets them fold, a climb lets them hang. What she is doing sets what they reach for: drawn up under her for an
  effort (`brace`: the lunge's draw-back, the volley, the whirl), forelegs thrust out (`reach`, the lunge, the rear
  before the volley), splayed
  and clutching (`spread`, laying eggs), tucked back (`tuck`, the dive); hanging idle, now and then one twitches.
- The abdomen, heavy and slow: it droops as she climbs and lifts as she drops, swings out of her turns, bounces with
  every wingbeat and, hovering, pumps slowly as an insect breathes.
- Her flying posture: she tilts nose down into speeding up and nose up into braking, and banks into her turns, as a
  flying insect does; held still (`steady`) while she aims.
- The proboscis: droops as she climbs, swings against a turn, searches slowly from side to side; held still on her aim.

A mod does the same after the animator each frame, from the creature's velocity. Degrees throughout: legs `back`,
`out`, `knee`, `tarsus` (folded in under her); proboscis `droop`, `side` (tip to her left); body `pitch` (nose down),
`bank` (her left wing down), abdomen `curl` (tip down) and `yaw` (tip to her right)."""
import math
from collections import namedtuple

from mathutils import Vector

from queen_moves import unrotate

G = 9.81
ACCEL_CAP, ACCEL_SMOOTH = 25.0, 0.08           # m/s2 per axis after smoothing (dives and trembles are violent)
HIP_HZ, KNEE_HZ, TARSUS_HZ, NEEDLE_HZ, BODY_HZ, ABDOMEN_HZ, DAMPING = 1.8, 1.4, 1.1, 2.4, 1.2, 1.3, 0.33
SUBSTEPS = 4
TUNING = [(1.08, 0.0), (1.0, 2.1), (0.93, 4.0)]  # per leg (front, middle, hind): stiffness factor, idle phase
INTENT = {                                      # per leg (front, middle, hind): (back, out, knee, tarsus) at full
    "brace": [(-12, -8, 38, 26), (6, -8, 38, 26), (20, -6, 34, 24)],
    "reach": [(-50, 6, -14, -12), (-18, 4, 0, 0), (20, 0, 12, 6)],
    "spread": [(-22, 20, 16, 12), (0, 22, 14, 10), (16, 18, 16, 12)],
}
Motion = namedtuple("Motion", "legs needle body")


class Spring:
    """A damped spring angle chasing a target."""

    def __init__(self, hz):
        self.omega, self.angle, self.speed = 2 * math.pi * hz, 0.0, 0.0

    def step(self, target, dt):
        accel = self.omega ** 2 * (target - self.angle) - 2 * DAMPING * self.omega * self.speed
        self.speed += accel * dt
        self.angle += self.speed * dt
        return self.angle


class Leg:
    def __init__(self, side, index):
        stiff, self.phase = TUNING[index]
        self.side, self.index = side, index
        self.hip_back, self.hip_out = Spring(HIP_HZ * stiff), Spring(HIP_HZ * stiff * 0.9)
        self.knee, self.tarsus = Spring(KNEE_HZ * stiff), Spring(TARSUS_HZ * stiff)

    def step(self, dt, accel, velocity, s, t):
        """accel, velocity: hers, in her frame; s: her state. Returns (back, out, knee, tarsus)."""
        forward, left, up = -accel.y, accel.x, accel.z
        sway = t * 2 * math.pi * (0.75 + 0.12 * self.index) + self.phase + (0.0 if self.side == "L" else 1.3)
        will = self._intent(s)
        idle = (1.0 - min(1.0, max(s.brace, s.reach, s.spread, s.tuck))) * self._twitch(t)
        lateral = left if self.side == "R" else -left      # a push to her left flings her right legs out
        drag = _clamp(22 * forward / G, -15, 35) + _clamp(1.6 * -velocity.y, -8, 25)   # braking or backing: gently
        back = self.hip_back.step(_clamp(drag, -18, 45) + 4 * math.sin(sway) + will[0] + 8 * idle, dt)
        out = self.hip_out.step(_clamp(20 * lateral / G, -12, 22) + 3 * math.sin(0.8 * sway + 1) + will[1], dt)
        trail = _clamp(math.degrees(0.45 * self.hip_back.speed / self.hip_back.omega), -8, 8)   # the knee lags the hip
        heavy = _clamp(-12 * up / G, -6, 14)       # climbing: hanging straighter; dropping: folding up
        knee = self.knee.step(heavy + trail + 5 * math.sin(sway + 0.9) + will[2] + 20 * idle, dt)
        tarsus = self.tarsus.step(0.6 * knee + 4 * math.sin(sway + 1.8) + will[3] + 10 * idle, dt)
        return min(back + 50 * s.tuck, 75.0), out, knee + 25 * s.tuck, tarsus + 15 * s.tuck

    def _intent(self, s):
        """What the move has the leg reach for: the sum of each intent's pose times how much of it she means."""
        total = [0.0, 0.0, 0.0, 0.0]
        for name, amount in (("brace", s.brace), ("reach", s.reach), ("spread", s.spread)):
            for k, value in enumerate(INTENT[name][self.index]):
                total[k] += value * amount
        return total

    def _twitch(self, t):
        """Now and then, on its own clock, the leg flexes and resettles, as an idle insect's legs do: 0 to 1."""
        every = 2.9 + 0.83 * self.index + (0.6 if self.side == "R" else 0.0)
        x = ((t + 1.37 * self.index + (2.1 if self.side == "R" else 0.0)) % every) / 0.45
        return math.sin(math.pi * x) if x < 1.0 else 0.0


class Proboscis:
    def __init__(self):
        self.droop, self.side = Spring(NEEDLE_HZ), Spring(NEEDLE_HZ * 0.85)

    def step(self, dt, accel, steady, t):
        """Returns (droop, side); `steady` 1 holds it still on her aim."""
        loose = 1.0 - steady
        droop = self.droop.step(loose * (_clamp(10 * accel.z / G, -8, 10) + 2.5 * math.sin(3.1 * t)), dt)
        side = self.side.step(loose * (_clamp(-9 * accel.x / G, -10, 10) + 3.5 * math.sin(2.3 * t + 0.7)), dt)
        return droop, side


class Body:
    def __init__(self):
        self.pitch, self.bank = Spring(BODY_HZ), Spring(BODY_HZ)
        self.curl, self.yaw = Spring(ABDOMEN_HZ), Spring(ABDOMEN_HZ * 0.85)

    def step(self, dt, accel, steady, t):
        """Returns (pitch, bank, abdomen curl, abdomen yaw) added to what the move poses."""
        forward, left, up, loose = -accel.y, accel.x, accel.z, 1.0 - steady
        pitch = self.pitch.step(loose * _clamp(9 * forward / G, -10, 12), dt)
        bank = self.bank.step(loose * _clamp(12 * left / G, -15, 15), dt)
        curl = self.curl.step(_clamp(10 * up / G, -10, 12) + 2.5 * math.sin(2 * math.pi * 0.35 * t), dt)
        yaw = self.yaw.step(_clamp(12 * left / G, -14, 14), dt)
        return pitch, bank, curl, yaw


class Sway:
    """Everything that swings, stepped once a frame from her states."""

    def __init__(self):
        self.legs = {(side, i): Leg(side, i) for side in "LR" for i in range(3)}
        self.proboscis, self.body = Proboscis(), Body()
        self.history, self.accel = [], Vector()

    def step(self, s, dt, t):
        """Her state this frame -> Motion(legs {(side, leg): angles}, needle (droop, side), body (4 angles))."""
        velocity, accel = self._motion(s.pos, dt)
        own_accel, own_velocity = unrotate(accel, s), unrotate(velocity, s)
        legs, needle, body = {}, (0.0, 0.0), (0.0, 0.0, 0.0, 0.0)
        for n in range(SUBSTEPS):
            now, step = t + n * dt / SUBSTEPS, dt / SUBSTEPS
            for key, leg in self.legs.items():
                legs[key] = leg.step(step, own_accel, own_velocity, s, now)
            needle = self.proboscis.step(step, own_accel, s.steady, now)
            body = self.body.step(step, own_accel, s.steady, now)
        return Motion(legs, needle, body)

    def _motion(self, pos, dt):
        """Her velocity and her smoothed, capped acceleration from the last three places."""
        self.history = (self.history + [pos.copy()])[-3:]
        velocity, accel = Vector(), Vector()
        if len(self.history) >= 2:
            velocity = (self.history[-1] - self.history[-2]) / dt
        if len(self.history) == 3:
            accel = (self.history[-1] - 2 * self.history[-2] + self.history[-3]) / (dt * dt)
        capped = Vector([_clamp(a, -ACCEL_CAP, ACCEL_CAP) for a in accel])
        self.accel = self.accel.lerp(capped, min(1.0, dt / ACCEL_SMOOTH))
        return velocity, self.accel


def _clamp(x, low, high):
    return min(max(x, low), high)

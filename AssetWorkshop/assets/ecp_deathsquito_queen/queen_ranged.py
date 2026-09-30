"""The Queen's moves at range (MOVES.md): the needle volley (4) and the brood of eggs (2)."""
from mathutils import Vector

from queen_attacks import chest, pulse
from queen_moves import Event, Move, bob, egg_point, facing, pitch_to, span, tip, turn_toward, yaw_to, DROOP


class Volley(Move):
    """4: back up and rear (the tell), then eight needles in a second at one target, a head recoil each. The aim is
    taken once, where the target stands as the volley begins: she does not turn and every needle keeps to its line, so
    a player who dodges leaves the rest to fly into the ground where they stood; those that miss stay stuck there."""
    name, caption = "volley", "4  Needle volley"
    FIRST, EVERY, SHOTS, DURATION, SPEED = 0.55, 0.125, 8, 2.1, 35.0
    # where each needle is aimed round the locked spot: (x, y) on the ground, or None for the chest
    SPREAD = [None, None, (-0.7, 0.9), None, (1.2, -0.4), None, None, (0.3, 1.3)]

    def __init__(self, start, target):
        self.target = target
        self.from_pos = start.pos - facing(start.yaw) * 0.5 + Vector((0, 0, 0.3))
        probe = start.copy(pos=self.from_pos)
        self.aim = pitch_to(tip(probe), chest(target)) - DROOP
        super().__init__(start, self.DURATION)
        self.events = [Event(0.05, "sound", {"cue": "buzz_strain"})]
        for k, spot in enumerate(self.SPREAD):
            self.events += self._shot(k, spot)

    def _shot(self, k, spot):
        at = self.FIRST + self.EVERY * k
        aim = chest(self.target) if spot is None else Vector((self.target.x + spot[0], self.target.y + spot[1], 0.0))
        needle = {"from": tip(self.state(at)), "to": aim, "speed": self.SPEED}
        return [Event(at, "sound", {"cue": "needle_shot"}), Event(at, "needle", needle)]

    def _state(self, t):
        s = self.start.copy()
        s.pos = self.start.pos.lerp(self.from_pos, span(t, 0, 0.5)).lerp(self.start.pos, span(t, 1.6, 2.1))
        s.pos = s.pos + bob(t, 0.03)
        rear, aiming = span(t, 0.0, 0.45) * (1 - span(t, 0.45, 0.55)), span(t, 0.45, 0.55) * (1 - span(t, 1.6, 2.1))
        recoil = sum(pulse(t, self.FIRST + self.EVERY * k, 0.1) for k in range(self.SHOTS))
        s.pos = s.pos - facing(self.start.yaw) * 0.06 * recoil                 # each shot kicks her back
        s.pitch = -25 * rear - 2.5 * recoil
        s.head = self.aim * aiming - 8 * recoil
        s.buzz, s.steady = 1.0 + 0.3 * aiming, aiming
        s.reach, s.brace = 0.8 * rear, 0.5 * aiming                           # forelegs up as she rears
        s.curl = 6 * rear - 4 * recoil                                         # abdomen tucks, jerks with the shots
        return s


class Brood(Move):
    """2: rise, curl the abdomen down and pump, fling one egg every quarter second to its spot; they land and bury
    themselves half deep; she uncurls and settles."""
    name, caption = "brood", "2  Brood"
    FIRST, EVERY, FLIGHT, DURATION, CURL = 0.6, 0.25, 0.8, 2.6, 40.0

    def __init__(self, start, spots, facing_point):
        self.spots, self.facing_point = spots, facing_point
        super().__init__(start, max(self.DURATION, self.FIRST + self.EVERY * len(spots) + 0.75))
        self.events = [Event(0.1, "sound", {"cue": "buzz_strain"})]
        for k, spot in enumerate(spots):
            at = self.FIRST + self.EVERY * k
            egg = {"from": egg_point(self.state(at + 0.06)), "to": spot, "flight": self.FLIGHT, "index": k}
            self.events += [Event(at, "sound", {"cue": "egg_launch"}), Event(at + 0.06, "egg", egg),
                            Event(at + 0.06 + self.FLIGHT, "sound", {"cue": "egg_land"})]

    def _state(self, t):
        s = self.start.copy(steady=0.0)
        done = self.FIRST + self.EVERY * len(self.spots)
        s.pos = self.start.pos + Vector((0, 0, 0.6)) * span(t, 0, 0.6) * (1 - span(t, done, self.duration))
        s.pos = s.pos + bob(t, 0.03)
        s.yaw = turn_toward(self.start.yaw, yaw_to(s.pos, self.facing_point), span(t, 0, 0.5))
        pumps = pulse(t, 0.2) + pulse(t, 0.4) + sum(pulse(t, self.FIRST + self.EVERY * k, 0.1)
                                                    for k in range(len(self.spots)))
        laying = span(t, 0, 0.6) * (1 - span(t, done, self.duration))
        s.curl = self.CURL * laying + 9 * pumps
        s.pos = s.pos + Vector((0, 0, 0.04 * pumps))                           # each squeeze heaves her
        s.pitch = -10 * laying - 4 * pumps
        s.buzz, s.spread = 1.2, 0.85 * laying
        return s

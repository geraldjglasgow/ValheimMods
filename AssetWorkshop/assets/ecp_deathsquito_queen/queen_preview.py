"""The Deathsquito Queen's fight in Blender: every move of MOVES.md in one sequence on the rigged Queen (queen_rig.py)
against two players on the Plains, with her needles, her eggs and their hatching, a camera and caption per
move, and the preview sounds (queen_sfx.py) on the timeline. Workshop preview only: nothing here is exported.

    blender --background --factory-startup --python assets/ecp_deathsquito_queen/queen_preview.py -- [--render] [--stills]

Needs out/rig/queen_rig.blend and the builds of ecp_queen_egg, ecp_queen_egg_burst and ecp_queen_needle. Writes
out/preview/queen_moves.blend (open it with queen_open.py to play it); --stills renders one frame of each move's key
moment to out/preview/stills/; --render renders out/preview/frames/ and encodes out/preview/queen_moves.mp4 with sound.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "blender"))
sys.path.insert(0, HERE)

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import queen_audio  # noqa: E402
import queen_fx  # noqa: E402
import queen_hatch  # noqa: E402
import queen_pose  # noqa: E402
import queen_props  # noqa: E402
import queen_shots  # noqa: E402
import queen_stage  # noqa: E402
import queen_sway  # noqa: E402
from queen_attacks import Dive, Lunge, Spit, Whirl  # noqa: E402
from queen_keys import Keys  # noqa: E402
from queen_moves import BODY_Z, HOVER_Z, Hover, State, yaw_to  # noqa: E402
from queen_ranged import Brood, Volley  # noqa: E402

FPS = 30
OUT = os.path.join(HERE, "out", "preview")
P1, P2 = Vector((0.0, 0.0, 0.0)), Vector((3.5, 1.5, 0.0))
ONLINE = 3                                  # players on the server in the preview's story; P1 and P2 are here
EGG_SPOTS = [(-3.0, 2.5), (1.5, 4.5), (5.5, 4.0), (-1.0, -3.5), (6.5, -1.0)]   # ONLINE + 2, 3 to 7 m round them


BROOD = f"2  Brood: {ONLINE} online + 2 = {ONLINE + 2} eggs"
SPIT = "6  Needle shot (nobody in reach)"
NEEDLES = "30 s after the volley: its needles crumble"
VOLLEY = "4  Needle volley: aim locked, dodge it"
DODGE, DODGE_TIME, DODGE_START = 1.8, 0.35, 2     # P2 steps 1.8 m aside in 0.35 s after the volley's second needle


def fight():
    """The sequence: (camera kind, caption, move, target, points to frame) in order, each move starting where the
    last left her, and the players' dodges: (part index, seconds into it, seconds long, player, from, to).
    Consecutive parts with the same camera and caption share one shot."""
    s = State(Vector((0.8, 8.5, HOVER_Z)), yaw=yaw_to(Vector((0.8, 8.5, 0)), P1))
    parts, eggs = [], [Vector((x, y, 0.0)) for x, y in EGG_SPOTS]

    def then(kind, caption, make, target=P1, extra=()):
        nonlocal s
        move = make(s)
        parts.append((kind, caption, move, target, list(extra)))
        s = move.end
        return move
    then("wide", "Deathsquito Queen", lambda s: Hover(s, 1.6, P1), extra=[P2])
    then("side", "1  Piercing dive", lambda s: Dive(s, P1))
    then("close", "5  Lunge", lambda s: Hover(s, 1.0, P1, to=_near(s.pos, P1, 3.3), altitude=1.85))
    then("close", "5  Lunge", lambda s: Lunge(s, P1))
    then("close", "7  Whirl", lambda s: Hover(s, 0.4, P1))
    then("close", "7  Whirl", lambda s: Whirl(s, P1))
    then("side", SPIT, lambda s: Hover(s, 1.3, P1, to=_near(s.pos, P1, 9.0), altitude=HOVER_Z))
    then("side", SPIT, lambda s: Spit(s, P1))
    then("high", VOLLEY, lambda s: Hover(s, 1.0, P2, to=_near(s.pos, P2, 6.0), altitude=HOVER_Z + 0.3), target=P2)
    volley = then("side", VOLLEY, lambda s: Volley(s, P2), target=P2)
    p2 = _dodged(volley.start.pos, P2)
    parts[-1][4].append(p2)                          # the shot holds where the dodge ends too
    dodges = [(len(parts) - 1, Volley.FIRST + DODGE_START * Volley.EVERY, DODGE_TIME, 1, P2, p2)]
    then("high", BROOD, lambda s: Hover(s, 0.7, P1), extra=eggs + [p2])
    then("high", BROOD, lambda s: Brood(s, EGG_SPOTS, (P1 + p2) / 2), extra=eggs + [p2])
    then("high", BROOD, lambda s: Hover(s, 1.2, P1), extra=eggs + [p2])
    then("eggs", "20 seconds later: the eggs hatch", lambda s: Hover(s, 4.2, P1), extra=eggs + [p2])
    field = [P2 + Vector((2.8 * math.cos(a), 2.8 * math.sin(a), 0.0)) for a in (0.0, 1.57, 3.14, 4.71)]
    then("needles", NEEDLES, lambda s: Hover(s, 2.0, P1), target=P2, extra=[p2] + field)   # the needle field, wide
    return parts, dodges


def _dodged(queen, player):
    """Where a player stepping DODGE metres aside, square to her line of fire, ends up."""
    line = Vector((player.x - queen.x, player.y - queen.y, 0.0)).normalized()
    return player + Vector((line.y, -line.x, 0.0)) * DODGE


def shots(placed):
    """Runs of parts with the same camera and caption: (kind, caption, first, last, points, queen, target)."""
    runs = []
    for kind, caption, move, target, extra, first, last in placed:
        points = _points(kind, move, target, extra)
        if runs and runs[-1][0] == kind and runs[-1][1] == caption:
            k, c, f, _, p, q, t = runs[-1]
            runs[-1] = (k, c, f, last, p + points, q, t)
        else:
            runs.append((kind, caption, first, last, points, move.start.pos, target))
    return runs


def _points(kind, move, target, extra):
    """What a shot must hold: her body along the move with her wingspan round it (not for the eggs' close shot), the
    target and the extras, each from the ground to head height."""
    out = []
    for i in range(0, round(move.duration * FPS) + 1, 6) if kind not in ("eggs", "needles") else ():
        at = move.state(i / FPS).pos
        out += [at + Vector((dx, dy, dz)) for dx, dy, dz in ((2.3, 0, 0), (-2.3, 0, 0), (0, 1.6, -1.0),
                                                              (0, -1.6, 0.3))]
    for spot in [target] + extra:
        out += [Vector((spot.x, spot.y, 0.0)), Vector((spot.x, spot.y, 1.9))]
    return out


def _near(pos, target, distance):
    """A point `distance` from the target on the line to `pos`."""
    away = Vector((pos.x - target.x, pos.y - target.y, 0.0)).normalized()
    return target + away * distance


def queen():
    """The rigged Queen from queen_rig.py under a root empty at her body's middle, her glow made to glow."""
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, "out", "rig", "queen_rig.blend"))
    rig, body = bpy.data.objects["queen_rig"], bpy.data.objects["ecp_deathsquito_queen"]
    root = bpy.data.objects.new("queen", None)
    bpy.context.scene.collection.objects.link(root)
    rig.parent, rig.location = root, (0.0, 0.0, -BODY_Z)
    rig.hide_viewport = rig.hide_render = False
    queen_shots.glow(body, os.path.join(HERE, "out", "ecp_deathsquito_queen_regions.png"))
    queen_pose.prepare(rig, root)
    return root, rig


def key_fight(keys, root, rig, parts):
    """Every frame of every move, her legs and proboscis swinging with her (queen_sway); returns the parts with their
    first frames and the events at their frames."""
    frame, placed, events, sway = 1, [], [], queen_sway.Sway()
    for kind, caption, move, target, extra in parts:
        count = round(move.duration * FPS)
        for i in range(count):
            s, t = move.state(i / FPS), (frame + i) / FPS
            queen_pose.key(keys, root, rig, s, frame + i, sway.step(s, 1.0 / FPS, t))
        events += [(frame + round(e.time * FPS), e) for e in move.events]
        placed.append((kind, caption, move, target, extra, frame, frame + count - 1))
        frame += count
    return placed, sorted(events, key=lambda pair: pair[0]), frame - 1


def players(keys, roots, dodges, placed):
    """The players' dodges keyed on their stand-ins (a quick side step with a little hop); returns where every player
    stands at any frame."""
    moves = []
    for part, at, length, who, start, end in dodges:
        first = placed[part][5] + round(at * FPS)
        last = first + round(length * FPS)
        for f in range(first - 1, last + 1):
            u = min(max((f - first) / (last - first), 0.0), 1.0)
            hop = Vector((0, 0, 0.15 * math.sin(math.pi * u)))
            keys.add(roots[who], 'location', f, start.lerp(end, u * u * (3 - 2 * u)) + hop)
        moves.append((who, first, last, start, end))

    def where(frame):
        spots = [P1.copy(), P2.copy()]
        for who, first, last, start, end in moves:
            u = min(max((frame - first) / (last - first), 0.0), 1.0)
            spots[who] = start.lerp(end, u)
        return spots
    return where


def props(keys, events, end, hatch_start, crumble_start, where):
    """Needles, eggs and hits at their frames; the eggs hatch in the hatching part, the stuck needles crumble in the
    last (both time skips: 20 s after the eggs land, 30 s after the needles stuck). Returns the sound cues."""
    cues, rng, eggs = [], random.Random(7), []
    flash = lambda at, f: queen_fx.flash(keys, at, f)                                       # noqa: E731
    dust = lambda at, f, size: queen_fx.dust(keys, at, f, size, seed=f)                      # noqa: E731
    for frame, e in events:
        if e.kind == "sound":
            cues.append((frame, e.data["cue"]))
        elif e.kind == "hit":
            flash(e.data["at"], frame)
            cues.append((frame, e.data["cue"]))
        elif e.kind == "needle":
            gone = lambda arrive: crumble_start + round(0.3 * FPS) + 3 * (arrive % 5)             # noqa: E731
            arrive, cue = queen_props.needle(keys, e.data, frame, FPS, end, flash, dust, where, gone)
            cues.append((arrive, cue))
        elif e.kind == "egg":
            egg, land, rest = queen_props.egg_flight(keys, e.data, frame, FPS, rng)
            dust(rest[0] + Vector((0, 0, 0.05)), land, 0.5)
            eggs.append((egg, rest))
    return cues + hatch(keys, eggs, hatch_start, end, dust, where(end))


def hatch(keys, eggs, start, end, dust, standing):
    """Each egg throbs from the last part's start, bursts THROB seconds in (a frame apart), and a Deathsquito rises."""
    cues = []
    for k, (egg, rest) in enumerate(eggs):
        burst = start + round(queen_hatch.THROB * FPS) + 2 * k
        cues += [(f, "egg_pulse") for f in queen_hatch.throb(keys, egg, rest, burst, FPS)[k % 2::2]]
        queen_hatch.burst(keys, rest, burst, FPS, end, dust, 10 * k)
        dust(rest[0] + Vector((0, 0, 0.1)), burst, 0.7)
        queen_hatch.hatchling(keys, rest[0], burst, FPS, end, standing[k % 2 == 0], k)
        cues += [(burst, "egg_burst"), (burst + 6, "hatch_buzz")]
    return cues


def main():
    root, rig = queen()
    roots = queen_stage.build([P1, P2])
    keys, (parts, dodges) = Keys(), fight()
    placed, events, end = key_fight(keys, root, rig, parts)
    where = players(keys, roots, dodges, placed)
    starts = {caption: first for _, caption, _, _, _, first, _ in reversed(placed)}
    cues = props(keys, events, end, starts["20 seconds later: the eggs hatch"], starts[NEEDLES], where)
    cues += queen_audio.buzz(end, FPS)
    runs = shots(placed)
    queen_shots.cameras(runs, keys, end)
    keys.flush()
    queen_shots.settings(end, FPS)
    queen_audio.sounds(cues, FPS)
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "queen_moves.blend"))
    print(f"WORKSHOP preview: {end} frames, {len(events)} events, {len(cues)} sound cues", flush=True)
    if "--stills" in sys.argv:
        queen_shots.stills(runs, os.path.join(OUT, "stills"))
    if "--render" in sys.argv:
        queen_shots.render(os.path.join(OUT, "frames"), os.path.join(OUT, "queen_moves.mp4"))


if __name__ == "__main__":
    main()

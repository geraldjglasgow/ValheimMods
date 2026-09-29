"""Swings and whooshes: air torn by a moving edge. Band-passed noise whose centre rises and falls with the speed of the
blade (a pass-by), loudest just after the middle; a heavy weapon is lower, longer and duller, a blade adds a thin
whistle. The game's own swings are quiet next to its hits (codex/sfx/archetypes.md: sword swing momentary max about
-16 LUFS against -15 for a hit, axe and polearm swings -24 to -27), 0.6 to 1.8 s long, most energy under 300 Hz.
"""
import numpy as np

from kit import core, env, filters, noise


def whoosh(seed=0, weight=1.0, seconds=None, whistle=0.0):
    """One swing: weight 0.5 a knife, 1 a sword, 2 a sledge; whistle 0..1 adds a blade's thin tone."""
    rng = np.random.default_rng(seed)
    seconds = seconds or 0.45 * weight ** 0.5 * rng.uniform(0.9, 1.15)
    peak = rng.uniform(0.5, 0.62)
    speed = env.swell(seconds, peak, 1.4)
    centre = 250 * weight ** -0.7 + 1400 * weight ** -0.5 * speed
    air = filters.svf(noise.pink(seconds, seed), centre, 1.2 + 0.6 * (1 - speed), "band")
    body = filters.svf(noise.brown(seconds, seed + 1), 90 + 200 * speed, 0.8, "low") * 0.6 * weight
    tone = filters.svf(noise.white(seconds, seed + 2), centre * 2.2, 18.0, "band") * whistle * 0.6
    out = (air + body + tone) * speed
    return core.fade(out, 0.01, 0.03)

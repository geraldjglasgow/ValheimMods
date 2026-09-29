"""Creaks: wood under strain. Stick-slip friction is a train of tiny catches (impulses) at an uneven 20 to 150 a second,
each ringing the wood's resonances; the rate slides as the strain changes, which is what the ear hears as the creak's
pitch. Doors, ship hulls, bending trees and anything made of living wood.
"""
import numpy as np

from kit import core, env, filters, noise


def catches(rate_hz, seconds, seed, roughness=0.3):
    """Impulses at a rate that follows rate_hz (a curve or a number), each gap jittered by roughness."""
    rng, n = np.random.default_rng(seed), core.count(seconds)
    rate = core.curve(rate_hz, n)
    out, at = np.zeros(n), 0.0
    while at < n:
        out[int(at)] = rng.uniform(0.5, 1.0) * rng.choice([-1.0, 1.0])
        at += core.RATE / max(rate[int(at)], 1.0) * rng.uniform(1 - roughness, 1 + roughness)
    return out


def creak(seconds=0.8, seed=0, low_hz=35.0, high_hz=110.0, body_hz=(420, 900, 1650), shape=None):
    """One creak whose catch rate slides from low_hz to high_hz and back, ringing three wooden resonances."""
    rng = np.random.default_rng(seed)
    path = [(0, low_hz), (seconds * rng.uniform(0.3, 0.6), high_hz), (seconds, low_hz * rng.uniform(0.8, 1.3))]
    train = catches(env.breakpoints(path, seconds, exponential=True), seconds, seed)
    wood = sum(filters.apply(train, filters.bandpass(hz * rng.uniform(0.9, 1.1), 6.0)) * (0.8 ** i)
               for i, hz in enumerate(body_hz))
    grit = filters.apply(noise.white(seconds, seed + 1), filters.bandpass(2500, 1.0)) * 0.05
    envelope = shape if shape is not None else env.ar(seconds, seconds * 0.2, seconds * 0.9, power=1.2)
    return core.fade((wood + grit) * core.curve(envelope, len(train)), 0.005, 0.03)

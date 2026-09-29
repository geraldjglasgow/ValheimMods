"""The Rootling's voice set, a worked example of a new creature's sounds: a small creature of living roots (the
Swamp creature planned for Elite Creatures Pack), about a Greyling's size, so it is measured against the Greydwarf
family (codex/sfx/creatures.md: the Greyling plays the Greydwarf's own clips at pitch 1.8 to 2.2).

Each call is a small throaty voice (recipes.voice, size 0.6, a little less rasp than a Greydwarf) under woody creaks
(recipes.creak) that follow the voice's envelope, so the creature sounds like it is made of wood. At runtime a mod
copies the Greydwarf's own sound prefab for each role (its reach, mixer group and variation settings) and swaps these
clips in (BundlePrefabs.SfxPrefabs.Copy), with the pitch range set back to the archetype's usual spread.
"""
import numpy as np

from kit import core, env
from recipes import creak, voice

SET = "ecp_rootling"
CAPTION = "$enemy_ecp_rootling"


def rootling(role, seed):
    """One Rootling call: the voice, and creaks shaped by its envelope."""
    rng = np.random.default_rng(seed + 1000)
    call = voice.voice(role, size=0.6, seed=seed, base_hz=235 * rng.uniform(0.95, 1.05), grit=0.8)
    seconds = len(call) / core.RATE
    level = np.abs(call)
    follow = np.convolve(level, np.ones(2205) / 2205, mode="same")
    wood = creak.creak(seconds, seed + 7, low_hz=40, high_hz=120 if role != "death" else 70, shape=follow)
    wood *= np.abs(call).max() / max(np.abs(wood).max(), 1e-9) * (0.55 if role in ("idle", "death") else 0.35)
    return core.mix(call, wood)


def maker(role):
    return lambda seed: rootling(role, seed)


SOUNDS = [
    {"name": "ecp_rootling_idle", "archetype": "sfx.creature.idle", "prefab": "sfx_greydwarf_idle",
     "pitch": [0.95, 1.05], "variations": 6, "make": maker("idle")},
    {"name": "ecp_rootling_alerted", "archetype": "sfx.creature.alert", "prefab": "sfx_greydwarf_alerted",
     "pitch": [0.95, 1.1], "variations": 4, "make": maker("alert")},
    {"name": "ecp_rootling_attack", "archetype": "sfx.creature.attack", "prefab": "sfx_greydwarf_attack",
     "pitch": [0.95, 1.05], "variations": 3, "make": maker("attack")},
    {"name": "ecp_rootling_hit", "archetype": "sfx.creature.hurt", "prefab": "sfx_greydwarf_hit",
     "pitch": [0.95, 1.05], "variations": 5, "make": maker("hurt")},
    {"name": "ecp_rootling_death", "archetype": "sfx.creature.death", "prefab": "sfx_greydwarf_death",
     "pitch": [0.95, 1.0], "variations": 3, "make": maker("death")},
]

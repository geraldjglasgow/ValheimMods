"""A crystal struck and a crystal breaking, a worked example of material sounds: a glassy body (six inharmonic modes
around 1.15 kHz with a slight detune, so they beat), a bright contact and a small thud, and for the break a spray of
fragments bouncing to rest over a fading dust hiss (recipes.impact).

Measured against the game's hard impacts (codex/sfx/materials.md). At runtime a mod copies the game's pure sound
prefabs for ice, the nearest material with its own sfx_ prefabs: sfx_ice_hit for the strike, sfx_ice_destroyed for
the break (fx_crystal_destruction carries particles too, so it is not a sound-only template).
"""
from recipes import impact

SET = "ecp_crystal"

SOUNDS = [
    {"name": "ecp_crystal_hit", "archetype": "sfx.piece.hit.ice", "prefab": "sfx_ice_hit", "variations": 5,
     "make": lambda seed: impact.hit("crystal", seed, size=1.3)},
    {"name": "ecp_crystal_break", "archetype": "sfx.world.destroy.crystal", "prefab": "sfx_ice_destroyed",
     "variations": 4, "make": lambda seed: impact.shatter("crystal", seed, size=1.2, seconds=2.6)},
]

"""A new fire loop, a worked example of a loop: ten seconds of roar, flicker, hiss and crackle that loops without a
seam (recipes.fire), levelled to the game's sfx_fire_loop and meant to play through a copy of it (a looping
AudioSource with a 15 m reach in the SFX group; ZSFX keeps at most a few of the same loop audible at once).
"""
from recipes import fire

SET = "ecp_fire"

SOUNDS = [
    {"name": "ecp_fire_loop", "archetype": "sfx.loop.fire", "prefab": "sfx_fire_loop", "variations": 1,
     "loop": True, "make": lambda seed: fire.fire_loop(10.0, seed, intensity=1.0)},
]

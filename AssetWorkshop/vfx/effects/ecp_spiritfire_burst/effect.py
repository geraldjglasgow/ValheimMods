"""A burst of green spirit fire: a ball of boiling green flames that rises and dies in a second and a half, a
flash, pale embers drifting up for two seconds, a thin lit smoke, and a green light that flares and fades. Built
to the game's explosion and fire numbers (codex/vfx/archetypes.md: vfx.explosion, roles flame, ember, smoke, flash)
and to the game's own flames: the flameball flipbook drawn gradient-mapped and additive (bright texels take the
first custom colour, dark ones the second), as fx_Torch_Green does in green.
"""

GREEN_HOT = {"colour": [[0, 0.55, 1.7, 0.85], [1, 0.08, 0.4, 0.2]], "alpha": [[0, 1], [1, 1]]}     # Custom1: bright texels
GREEN_DARK = {"colour": [[0, 0.06, 0.9, 0.35], [1, 0.02, 0.18, 0.1]], "alpha": [[0, 1], [1, 1]]}  # Custom2: dark texels

EFFECT = {
    "name": "ecp_spiritfire_burst",
    "category": "vfx.explosion",
    "timeout": 4.0,
    "preview": {"seconds": 2.5, "distance": 4.5, "target_height": 0.9, "repeat": 0,
                "references": ["Characters/Surtling/fx/vfx_FireballHit.prefab"], "reference_offset": [3.2, 0, 0]},
    "textures": {
        "ecp_spiritfire_flames": {"make": "flame_flipbook", "args": {"seed": 4}, "point": True, "mips": False},
        "ecp_spiritfire_ember": {"make": "spark_bolt", "args": {"px": 64, "seed": 2}},
        "ecp_spiritfire_glow": {"make": "point", "args": {"px": 128}},
        "ecp_spiritfire_smoke": {"make": "cloud", "args": {"px": 128, "seed": 8}},
    },
    "materials": {
        "ecp_spiritfire_flames": {"texture": "ecp_spiritfire_flames", "shader": "gradient_mapped", "blend": "additive_soft",
                                  "soft": 12},
        "ecp_spiritfire_ember": {"texture": "ecp_spiritfire_ember", "shader": "particle_unlit", "blend": "additive_soft"},
        "ecp_spiritfire_glow": {"texture": "ecp_spiritfire_glow", "shader": "legacy_additive"},
        "ecp_spiritfire_smoke": {"texture": "ecp_spiritfire_smoke", "shader": "lit", "soft": 1.0,
                                 "colours": {"_Color": [0.55, 0.62, 0.58, 1]}},
    },
    "systems": [
        {"name": "flames", "role": "flame", "position": [0, 0.35, 0], "duration": 1.0, "space": "world", "max_particles": 200,
         "lifetime": (0.5, 0.9), "speed": (0.6, 2.0), "size": (0.35, 0.65), "rotation": (0, 360), "flip_rotation": 0.5,
         "colour": ([0.63, 0.63, 0.63, 1], [1, 1, 1, 1]), "gravity": -0.25,
         "shape": {"type": "sphere", "radius": 0.25},
         "emission": {"rate": 0, "bursts": [(0.0, 18), (0.08, 8)]},
         "limit": {"speed": 0.6, "drag": 2.5},
         "noise": {"strength": 0.35, "frequency": 1.5, "scroll": 0.3},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 1, 1]], "alpha": [[0, 0], [0.08, 1], [0.45, 0.85], [1, 0]]},
         "size_life": [[0, 0.3], [0.12, 0.9], [0.6, 1.0], [1, 0.45]],
         "rotation_life": (-45, 45),
         "sheet": {"tiles": [8, 8], "time": "fps", "fps": 45, "start_frame": (0, 0.9999)},
         "custom": [{"colour": {"gradient": GREEN_HOT}}, {"colour": {"gradient": GREEN_DARK}}],
         "renderer": {"material": "ecp_spiritfire_flames", "max_size": 0.5}},
        {"name": "flash", "role": "flash", "position": [0, 0.5, 0], "duration": 0.5, "max_particles": 2,
         "lifetime": 0.3, "speed": 0, "size": 2.6, "colour": [0.3, 1.0, 0.55, 0.45],
         "shape": None, "emission": {"rate": 0, "bursts": [(0.0, 1)]},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 1, 1]], "alpha": [[0, 1], [1, 0]]},
         "size_life": [[0, 0.6], [1, 1.0]],
         "renderer": {"material": "ecp_spiritfire_glow", "max_size": 2.0}},
        {"name": "embers", "role": "ember", "position": [0, 0.4, 0], "duration": 1.0, "space": "world", "max_particles": 200,
         "lifetime": (1.2, 2.4), "speed": (1.5, 4.5), "size": (0.025, 0.06), "gravity": -0.02,
         "colour": ([0.5, 1.0, 0.7, 1], [0.8, 1.0, 0.85, 1]),
         "shape": {"type": "sphere", "radius": 0.2},
         "emission": {"rate": 0, "bursts": [(0.0, 60)]},
         "limit": {"speed": 0.4, "drag": 3.0},
         "noise": {"strength": 0.5, "frequency": 1.0, "scroll": 0.4},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 1, 1]], "alpha": [[0, 0], [0.1, 1], [0.7, 0.9], [1, 0]]},
         "size_life": [[0, 1], [1, 0.2]],
         "renderer": {"material": "ecp_spiritfire_ember", "max_size": 0.5}},
        {"name": "smoke", "role": "smoke", "position": [0, 0.6, 0], "duration": 1.0, "space": "world", "max_particles": 30,
         "lifetime": (1.6, 2.6), "speed": (0.3, 0.9), "size": (1.0, 1.6), "rotation": (0, 360), "gravity": -0.05,
         "colour": [0.7, 0.85, 0.78, 0.55],
         "shape": {"type": "sphere", "radius": 0.3},
         "emission": {"rate": 0, "bursts": [(0.05, 8)]},
         "limit": {"speed": 0.2, "drag": 1.0},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 1, 1]], "alpha": [[0, 0], [0.2, 0.6], [0.6, 0.4], [1, 0]]},
         "size_life": [[0, 0.4], [1, 1.0]],
         "rotation_life": (-15, 15),
         "renderer": {"material": "ecp_spiritfire_smoke", "max_size": 1.0}},
    ],
    "lights": [{"name": "light", "position": [0, 0.6, 0], "colour": [0.45, 1.0, 0.6], "intensity": 3.0, "range": 8.0,
                "flicker": {"intensity": 0.1, "speed": 10, "movement": 0.1, "ttl": 1.4, "fade": 0.9, "fade_in": 0.0},
                "lod": {"distance": 40}}],
}

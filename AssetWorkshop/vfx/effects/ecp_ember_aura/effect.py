"""A glowing ember aura, looping on a creature or player: hot embers drifting up round the body, a few flat orange
pixel motes, a faint warm halo and a flickering warm light. Built like the game's burning and glow effects (vfx_Burning,
fx_charred_chestglow; codex/vfx/archetypes.md: vfx.aura, vfx.status, roles ember, glow): embers are tiny additive
sparks on the game's cinder shape, living two to three seconds; the halo is one big alpha-blended soft sprite that
never dies; the light flickers the way LightFlicker does. The mod attaches it (LocalEffect.Attach, endless).
"""

EFFECT = {
    "name": "ecp_ember_aura",
    "category": "vfx.aura",
    "timeout": 0.0,
    "preview": {"seconds": 2.5, "warmup": 2.5, "distance": 4.0, "target_height": 1.0, "figure_offset": [0, 0, 0],
                "references": ["GameElements/StatusEffects/effects/vfx_Burning.prefab"], "reference_offset": [2.8, 0, 0]},
    "textures": {
        "ecp_ember_spark": {"make": "spark_bolt", "args": {"px": 64, "seed": 5}},
        "ecp_ember_halo": {"make": "point", "args": {"px": 128}},
    },
    "materials": {
        "ecp_ember_spark": {"texture": "ecp_ember_spark", "shader": "particle_unlit", "blend": "additive_soft"},
        "ecp_ember_mote": {"shader": "standard_unlit", "blend": "additive_alpha"},     # untextured: flat squares
        "ecp_ember_halo": {"texture": "ecp_ember_halo", "shader": "legacy_alpha", "soft": 0.3},
    },
    "systems": [
        {"name": "embers", "role": "ember", "position": [0, 0.9, 0], "duration": 5.0, "loop": True, "prewarm": True, "space": "world",
         "max_particles": 100, "lifetime": (1.6, 2.8), "speed": (0.05, 0.3), "size": (0.02, 0.05), "gravity": -0.04,
         "colour": ([1.0, 0.42, 0.08, 1], [1.0, 0.75, 0.3, 1]),
         "shape": {"type": "box", "scale": [0.6, 1.5, 0.4]},
         "emission": {"rate": 18},
         "limit": {"speed": 0.6, "drag": 1.0},
         "noise": {"strength": 0.45, "frequency": 1.0, "scroll": 0.3},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 0.6, 0.5]], "alpha": [[0, 0], [0.1, 1], [0.75, 0.9], [1, 0]]},
         "size_life": [[0, 1], [1, 0.3]],
         "renderer": {"material": "ecp_ember_spark", "max_size": 0.5}},
        {"name": "motes", "role": "ember", "position": [0, 0.8, 0], "duration": 5.0, "loop": True, "prewarm": True, "space": "world",
         "max_particles": 40, "lifetime": (1.0, 2.0), "speed": (0.1, 0.4), "size": (0.03, 0.045), "gravity": -0.02,
         "colour": [1.0, 0.5, 0.18, 1],
         "shape": {"type": "box", "scale": [0.5, 1.4, 0.35]},
         "emission": {"rate": 7},
         "noise": {"strength": 0.3, "frequency": 1.5},
         "colour_life": {"colour": [[0, 1, 1, 1], [1, 1, 1, 1]], "alpha": [[0, 0], [0.2, 1], [1, 0]]},
         "rotation_life": (-90, 90),
         "renderer": {"material": "ecp_ember_mote", "max_size": 0.5}},
        {"name": "halo", "role": "glow", "position": [0, 1.0, 0.35], "duration": 5.0, "loop": True, "prewarm": True, "max_particles": 1,
         "lifetime": 1000.0, "speed": 0, "size": 2.8, "colour": [1.0, 0.45, 0.15, 0.3], "shape": None,
         "emission": {"rate": 0, "bursts": [(0.0, 1)]},
         "renderer": {"material": "ecp_ember_halo", "max_size": 2.0}},
    ],
    "lights": [{"name": "light", "position": [0, 1.1, 0], "colour": [1.0, 0.55, 0.25], "intensity": 2.2, "range": 5.0,
                "flicker": {"intensity": 0.15, "speed": 8, "movement": 0.08},
                "lod": {"distance": 30}}],
}

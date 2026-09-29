"""The clutter system's grass, ferns, reeds, flowers and pebbles: each clutter prefab holds one InstanceRenderer (a
mesh, a material, a scale and LOD distances) that the game draws hundreds of times over every 10 m patch of ground
around the player. Measured with the same keys as environment_read.summary, plus 'instance'.
"""
import numpy as np

import environment_read as read
import game
from workshop import unity


def instanced(prefab_path):
    """The clutter system's grass and small plants: an InstanceRenderer (mesh, material, scale, LOD distances) the
    game draws thousands of times per patch. Same keys as summary(), plus 'instance'."""
    pf = game.prefab(prefab_path)
    body = next((b for _, k, b in pf.all_components() if k == "InstanceRenderer"), None)
    mesh, mat = game.path_of(unity.field(body, "m_mesh")), game.path_of(unity.field(body, "m_material"))
    scale = unity.numbers(unity.field(body, "m_scale")) or (1, 1, 1)
    stats, info = game.mesh_stats(mesh), read.material_info(mat)
    got = read.surface(mesh, (0, stats["triangles"] * 3), 1.0)
    texel = None
    if got and info["texture_px"] and got[0]:
        texel = np.sqrt(abs(got[1]) * info["texture_px"][0] * info["texture_px"][1] / got[0]) / (sum(scale) / 3)
    return {"prefab": prefab_path, "name": pf.name(pf.root()), "scripts": read.scripts(pf),
            "triangles": stats["triangles"],
            "renderers": 0, "lods": [], "lod_groups": 0, "billboard": False,
            "size_m": [round(s * k, 2) for s, k in zip(stats["size"], scale)], "base_y": None,
            "texel_density": round(float(texel), 1) if texel else None, "shader": info["shader"],
            "texture_px": max(info["texture_px"]) if info["texture_px"] else None,
            "materials": [{"path": mat, **info}],
            "instance": {"mesh": mesh, "scale": list(scale), "vertices": stats["vertices"],
                         "lod_distance_m": [float(unity.field(body, "m_lodMinDistance") or 0),
                                            float(unity.field(body, "m_lodMaxDistance") or 0)],
                         "shadows": unity.field(body, "m_shadowCasting") == "1"}}

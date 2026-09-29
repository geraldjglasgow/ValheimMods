"""One creature prefab measured whole: every renderer, the LOD0 totals, its size in the game, capsule, LODs, star
looks, carried items, ragdoll and animator scripts. creatures.py runs read() over every creature.
"""
import os

import game
import creatures_read as cr
import creatures_scripts as cs
import rigs_read
from workshop import unity

KEPT_SCRIPTS = ("Player", "Humanoid", "Character", "MonsterAI", "AnimalAI", "Tameable", "Procreation", "Growup",
                "Sadle", "CharacterDrop", "LevelEffects", "FootStep", "CharacterAnimEvent", "VisEquipment", "Trader",
                "Odin", "Valkyrie", "Raven", "Fish", "Leviathan", "RandomFlyingBird", "AnimationEffect", "Ragdoll",
                "Tail", "ZSyncAnimation", "RandomAnimation", "LodFadeInOut")


def read(path):
    """Everything the codex keeps of one creature prefab."""
    p, cache = game.prefab(path), {}
    kinds = [(node, kind, body) for node, kind, body in p.all_components()]
    out = {"prefab": path, "name": os.path.splitext(os.path.basename(path))[0],
           "scripts": sorted({k for _, k, _ in kinds if k in KEPT_SCRIPTS})}
    parts = [m for m in (cr.measure_renderer(p, row, cache) for row in cr.renderer_rows(p)) if m]
    out.update(_totals(parts, _main_ids(kinds)))
    out["renderers"] = [_renderer_row(m) for m in parts]
    out["lods"] = [h for h, _ in cr.lod_levels(p)]
    for node, kind, body in kinds:
        _script(out, p, node, kind, body)
    out["capsule"] = _capsule(p)
    skeleton = rigs_read.skeleton(p, cache)
    out["bones"] = skeleton["bone_count"] if skeleton else 0
    out["rig_signature"] = rigs_read.signature(p)
    return out


def _script(out, p, node, kind, body):
    """Fills the entries one component gives."""
    if kind in ("Humanoid", "Character", "Player"):
        out["character"] = cs.character(body)
        out["death_effects"] = cs.effect_list(body, "m_deathEffects")
        out["ragdoll"] = cs.ragdoll(out["death_effects"])
        if kind == "Humanoid":
            out["items"] = {k: [cs.item_info(i) for i in v] for k, v in cs.items(body).items()}
    elif kind == "LevelEffects":
        out["level_effects"] = cs.level_effects(p, body)
    elif kind == "Animator" and "animator" not in out:
        out["animator"] = cs.animator(body)
        out["animator"]["node"] = p.path_to(node)
    elif kind == "CharacterAnimEvent":
        out["anim_event"] = cs.anim_event(p, body)
    elif kind == "FootStep":
        out["footstep"] = cs.footstep(p, body)
    elif kind == "ZSyncAnimation":
        out["sync"] = cs.sync(body)
    elif kind == "VisEquipment":
        out["attach"] = cs.vis_equipment(p, body)
    elif kind in ("MonsterAI", "AnimalAI"):
        out["ai"] = {"kind": kind, "sleeping": cs.value(body, "m_sleeping") == 1}


def _renderer_row(m):
    """A renderer as kept in the data: no bounds corners, materials by name, shader and textures."""
    keep = ("node", "kind", "triangles", "vertices", "bones", "lod", "drawn", "texture_px", "texel_density",
            "mesh_scale")
    row = {k: m[k] for k in keep}
    row["mesh"] = m["mesh"]
    row["materials"] = m["materials"]
    return row


def _main_ids(kinds):
    """The renderer the game itself calls the body: LevelEffects.m_mainRender, else VisEquipment.m_bodyModel."""
    for field, owner in (("m_mainRender", "LevelEffects"), ("m_bodyModel", "VisEquipment")):
        ids = [unity.ref(unity.field(b, field))[0] for _, k, b in kinds if k == owner]
        if ids and ids[0] != "0":
            return ids[0]
    return None


def _totals(parts, main_id):
    """LOD0 totals over the drawn renderers: triangles, renderers, materials, size, and the main renderer's texture
    size and texel density (the body the game names, else the drawn renderer with the largest bounds)."""
    drawn = [m for m in parts if m["drawn"] and m["lod"] == 0]
    if not drawn:
        return {"triangles": 0, "renderer_count": 0}
    extent = lambda m: sum((h - l) ** 2 for l, h in zip(m["low"], m["high"]))
    main = next((m for m in drawn if m["file_id"] == main_id), None) or max(drawn, key=extent)
    low = [min(m["low"][i] for m in drawn) for i in range(3)]
    high = [max(m["high"][i] for m in drawn) for i in range(3)]
    shaders = sorted({i["shader"] for m in drawn for i in m["materials"]})
    return {"triangles": sum(m["triangles"] for m in drawn), "renderer_count": len(drawn),
            "material_count": len({i["material"] for m in drawn for i in m["materials"]}),
            "shaders": shaders, "texture_px": main["texture_px"], "texel_density": main["texel_density"],
            "main_renderer": main["node"], "size_m": [round(high[i] - low[i], 3) for i in range(3)],
            "height_m": round(high[1] - low[1], 3), "top_m": round(high[1], 3), "bottom_m": round(low[1], 3)}


def _capsule(p):
    """The root's CapsuleCollider (the body the game moves), metres with the root's scale: radius, height, centre."""
    scale = p.world_scale(p.root())
    for kind, body in p.components(p.root()):
        if kind == "CapsuleCollider":
            return {"radius": round(float(unity.field(body, "m_Radius")) * scale, 3),
                    "height": round(float(unity.field(body, "m_Height")) * scale, 3),
                    "axis": "xyz"[int(unity.field(body, "m_Direction") or 1)],
                    "center_y": round(unity.numbers(unity.field(body, "m_Center"))[1] * scale, 3)}
    return None

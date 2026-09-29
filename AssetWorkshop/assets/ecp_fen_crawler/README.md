# Fen Crawler accessory kit

Original overlapping peat/bark plates and an asymmetric ridge of snapped reeds. Fits the vanilla Neck's sloping back without replacing its body, limbs or animations. 442 triangles, 128 px albedo, no normal map. Muted yellow olive and wet dark brown are intended for the Swamp.

`model.py` uses full vanilla prefab rest coordinates, metres, Z up, front -Y. Place at root identity then parent to `Spine` retaining world transform; scale the creature afterward. The kit is one rigid piece; animated fitting should be reviewed in game across the Neck's crouch and attack poses. No colliders or game geometry in the shipped model.

Build: `powershell -ExecutionPolicy Bypass -File AssetWorkshop/build.ps1 -Asset ecp_fen_crawler`. Run the neighboring `ecp_bog_maw/review.py` in background Blender for local-only vanilla-body fitting sheets under `out/fitting/`.

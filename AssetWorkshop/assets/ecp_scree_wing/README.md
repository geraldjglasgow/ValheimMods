# Scree Wing

Original shale head armor, swept crag horns and serrated rock crest fitted to the
vanilla Hatchling. The game supplies its animated body and wings at runtime.
438 triangles, one 256 px point-filtered baked albedo, no emission or normal map.
Dark desaturated slate, chipped gray edges and weathered brown horn follow the
chunky shapes and muted palette of the local vanilla swamp reference board.

Build with `AssetWorkshop/build.ps1 -Asset ecp_scree_wing`.
Shared geometry/paint helpers come from `../ecp_cairn_wight/model.py`.
`../ecp_cairn_wight/review.py` renders the kit on the local vanilla base. Only
original attachment meshes and baked textures enter the runtime bundle.

Fit: unscaled vanilla prefab root rest coordinates; instantiate at root with
identity then parent to `Head` preserving world transform. Scale the complete
creature at its root. Do not zero the local attachment transform after parenting.
The existing thin pale vanilla horns remain visible between the new broad swept
horns, retaining the drake lineage. Rest fit is visually checked; runtime animation
validation is performed with the mod integration.

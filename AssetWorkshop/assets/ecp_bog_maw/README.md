# Bog Maw accessory kit

Original low polygon root plates, forked dead roots and stained bone tusks/teeth. Built against the locally exported vanilla Blob: 1.64 m wide, 1.76 m deep, 0.97 m high. Palette follows its dull peat/olive body and the Swamp's dead wood; bone stays yellow brown rather than bright white. 608 triangles, 128 px albedo, no normal map.

`model.py` uses full vanilla prefab rest coordinates, metres, Z up, front -Y. Place at the creature root with identity transform, then parent to `Bone` retaining world transform. Scale the whole creature afterward. No game meshes or textures are exported in the kit. Mesh has no colliders; the base creature retains its own collision and animation.

Build: `powershell -ExecutionPolicy Bypass -File AssetWorkshop/build.ps1 -Asset ecp_bog_maw` from repo root. `review.py` loads both built beast kits on locally available game reference bodies and writes `out/fitting/preview.png` in each folder; these previews and blend files are local only. The regular kit preview omits the vanilla body.

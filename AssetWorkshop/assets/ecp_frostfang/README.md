# Mountain creature attachments

Original low-poly meshes, fitted in the unscaled vanilla prefab root frame. Metres,
Z up, forward -Y in Blender. Vanilla meshes and textures are used only in local
reference previews and are never in an exported kit.

| Kit | Base | Animated anchor | Atlas |
| --- | --- | --- | --- |
| ecp_frostfang | Wolf | Spine2 | 128 |
| ecp_rimeback | Lox | Spine2 | 256 |
| ecp_ice_crawler | Neck | Spine1 | 128 |

Instantiate at creature root local identity, then reparent to the named bone with
world transform preserved. Apply the intended creature scale to its root. Runtime
materials retain the baked atlas with point sampling; keep original body textures.
Frostfang's coarse charcoal mane and old ivory shoulder spurs, Rimeback's broad slate
mantle, and Ice Crawler's opaque broken ice crest use muted, non-emissive colours.
Geometry and paint helpers follow the existing swamp kit's low polygon and atlas
conventions. Build with the workshop build.ps1 for those three asset names.

`review.py` assembles the baked meshes on local vanilla references and writes each
kit's `out/fitted/preview.png` and local-reference-only blend file. It never exports
the game meshes. `inspect_reference.py` creates local-only original body sheets.

# Drowned Shade

Original accessory kit for the vanilla Wraith: corroded burial yoke, polygonal alternating chain links, broken shackles, riveted shoulder plates and a hollow cracked bronze bell. Black bog iron and subdued verdigris retain the original Wraith shroud's charcoal palette. The vanilla body supplies its animated shroud, hands and face.

`model.py` uses creature-root rest coordinates, metres, Z up and front -Y. Place at the creature root with identity rotation/scale, then parent to the torso while preserving the world transform. The yoke is at Z 1.87 m and bell spans Z 1.025–1.496 m. Scale through the common creature parent.

Build with `AssetWorkshop/build.ps1 -Asset ecp_drowned_shade`. Output is 3,438 triangles with a 256 px albedo atlas and no colliders. No game geometry or textures are included in the export.

Run the neighboring `ecp_reed_stalker/preview_fit.py` for local reference fitting sheets. Static rest-pose fitting has been inspected; chain links follow the torso as one rigid mesh and have no separate physics or animation.

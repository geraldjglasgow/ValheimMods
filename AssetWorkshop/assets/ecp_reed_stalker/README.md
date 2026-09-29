# Reed Stalker

Original accessory kit for the vanilla Draugr: layered cut reeds, crossing flax lashings and an open wicker eel trap with bundled reeds. The runtime supplies the animated body and separately equipped spear. The muted olive/ochre/peat palette follows the local Draugr diffuse texture and the swamp reference board.

`model.py` uses creature-root rest coordinates, metres, Z up and front -Y. Place the imported kit at the creature root with identity rotation/scale, then parent it to the torso while preserving its world transform. The mantle begins at Z 1.84 m and the trap spans Z 0.96–1.61 m. Apply creature scale through the common parent.

Build with `AssetWorkshop/build.ps1 -Asset ecp_reed_stalker`. Output is 2,360 triangles with a 256 px albedo atlas and no colliders. No game geometry or textures are included in the asset export.

`preview_fit.py` separately loads the local vanilla reference and produces fitted views for both this kit and Drowned Shade. Those reference composites are local inspection images only. The fitting is checked in the exported rest pose; runtime animation verification remains necessary.

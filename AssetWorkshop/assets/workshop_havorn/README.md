# Havorn - decked cargo ship

Original Valheim-style ship with a complete upper deck, ten-step companionway, furnished cargo hold, mast ladder and lookout, captain's wheel, stern rudder and bow swivel cannon. All model and texture work is original.

## Current revision

- Downward iron hooks wrap over the handrail and support each shield; the old straight brackets and partly buried brown diamond decorations are removed.
- Fitted bow and stern caps replace the overlapping stems and prow carving. Deck boards extend to both ends, with continuous perimeter and closing end rails. End bulwarks now meet the deck edge below the railing.
- Cannon moved 0.89 m toward the bow and raised 0.36 m, with a taller pedestal. The full 180-degree forward aiming sweep clears the ship at 37 sampled angles.
- Both ladder rail openings remain clear. The lookout ladder ends evenly at the floor; redundant handholds are removed.
- Wheel and centreline stern rudder have separate pivots. Visual steering ropes connect the helm toward the stern.

Overall dimensions: 23.72 m long including rudder, 15.61 m across the yard, 20.85 m high. Hull beam approximately 7.18 m. Hold headroom 2.24 m, or 2.01 m under beams. Stair tread width 1.62 m; mast ladder clear width 0.59 m.

## Open in color

Open `out/Havorn_Finished_Ends.blend`. Packed textures, material preview, studio lights and eight saved cameras are included. `out/Havorn_Hold_Inspection.blend` is a separate cutaway view, not the exported geometry.

Previews: `colored_ship.png`, `colored_boarding.png` (hooks and access), `colored_cannon.png` (bow decking), `colored_helm.png` (stern decking), `colored_lookout.png`, `colored_deck.png`, and `colored_hold_cutaway.png` under `out/`.

## Portable files and validation

`out/havorn_parts.blend`, `.fbx`, and `out/havorn.glb` contain 19 separate visual objects. GLB embeds textures; FBX textures are supplied alongside. Blender/FBX include hidden deck, stair and nest collision markers. `havorn_construction.blend` retains original separate components and procedural materials.

18,313 triangles, point-filtered 2048 albedo, normal and region maps. `validation.json` records geometry, clearances, end floor/rail coverage, cannon sweep and FBX/GLB re-import checks. Shield meshes do not intersect the hull or deck rails; their sampled minimum vertex clearance is 0.146 m. `style_report.txt` records paint measurements; documented exceptions remain in `BRIEF.md`.

`CannonYaw` has a local-Z constraint of -90 to +90 degrees. The demonstration timeline shows port at frame 1, forward at 31 and starboard at 61, returning through frame 121. `interactions.json` contains pivots/anchors and the Unity-local interaction contract. Wheel and rudder are visual parts; they have no gameplay steering controller.

Rebuild from repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File AssetWorkshop/assets/workshop_havorn/build.ps1
```

`out/Havorn.zip` contains the asset, previews and reproducible sources. Vanilla comparison meshes are excluded. Earlier deliveries are archived under ignored `revisions/`.

This is an authoring asset. Valheim buoyancy, climbing, steering, firing, networking and runtime collision behavior still need integration and in-game testing. Nothing has been installed into a mod.

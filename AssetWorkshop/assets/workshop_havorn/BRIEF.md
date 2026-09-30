# Brief: Havorn decked cargo revision

1. **Identity:** `workshop_havorn`, standalone workshop asset, no release mod selected. Timber expedition/cargo ship for Meadows coasts and Ocean travel, Black Forest construction palette with iron fittings. User requested upper deck, stairs to a cargo hold, decoration and a colored presentation, and authorized increasing size.
2. **Category:** `vehicle.ship`; the shared codex has no surveyed ship population. The local Karve visual bounds are 7.805834 x 10.115770 x 9.342331 m (Blender XYZ). This revision is 15.611669 x 22.25 x 20.8477 m, larger than the initial exact-2x concept. Approximately 7.18 m hull beam provides room around a central companionway.
3. **References:** local `GameElements/Ships/Karve.prefab` for timber, scale and rigging; `GameElements/Ships/VikingShip.prefab` for large vessel silhouette and sail. Game assets remain comparison-only; no vanilla mesh or texture is shipped.
4. **Silhouette:** broad clinker hull, raised carved bird prow, one square red-and-linen sail, timber deck rail and painted shields. Recognizable large forms at game distance.
5. **Parts:** Blender metres, Z up, bow -Y, waterline origin. Seven overlapping strakes per side, closed keel, internal frames, lower cargo floor, full upper deck with real stair aperture, ten stair treads/risers/stringers/handrails, deck rails, mast and yard, sail, rigging, steering oar, boarding ladder, cargo and decoration. Hold standing height about 2.24 m, about 2.01 m under deck beams. No held-item frame, building snap grid, damage states or destruction chunks in this visual delivery.
6. **Budget:** approximately 16k triangles including complete interior and cargo; 2048 combined atlas, nearest filtering, AO 0.2 and normal relief 3.0. Large unique atlas is justified by 22 m ship size and both deck surfaces; density is measured in the report. No fictional ship-category limits are invented.
7. **Materials:** measured workshop wood.planks, wood.dark, wood.logs, cloth.linen, cloth.rope and metal.iron recipes. Muted oak and tarred trim, cream linen, oxide red dye, faded blue painted shields, ochre timber inlays. Original paint only. Runtime integration should use appropriate game ship hull/sail materials; Blender/GLB use baked preview materials.
8. **Regions:** wood structure, trim dark timber and ochre inlays, cloth undyed sail, primary oxide red, secondary faded blue, rope rigging, metal fittings. No extra recolour variants requested.
9. **Motion:** independent Hull, Sail, Rudder, Rigging, UpperDeck, HoldFloor, Stairs, Cargo, DeckRail, Decoration. Sail and rudder have meaningful pivots; no gameplay animation or controller implied. No creature skeleton, attacks, animation events or sounds.
10. **Effects:** none embedded; future mod should borrow Karve wake, splash, place, damage and destroy effects at runtime.
11. **Sounds:** none embedded; future mod should borrow ship creak, sail and water sounds at runtime. No synthesis or copied game audio.
12. **Integration:** editable Blender plus FBX/GLB and textures under `out/`. Deck collision boxes split around stair aperture; stair treads have boxes. Collision markers are preliminary, not game-tested. No crafting, flotation, ship controller, owner logic or ZDO state is added. Nothing installed in a mod. Original v1 archived under ignored revisions/.
13. **Checks:** verify actual dimensions, finite vertices, nonzero-area polygons and UVs; preserve the upper-deck hole; reimport GLB to verify all fifteen parts, dimensions, UVs and materials. Inspect colored exterior, upper deck and hold cutaway, plus vanilla reference lineup. Colored presentation opens with packed textures in Blender material preview. Cutaway modifies a separate inspection copy only.

## Style interpretation

The ship category has no survey statistics, so geometry budget and length are ungraded. The enlarged footprint and complete cargo interior justify the 16,125 triangles and large atlas. Colored paint regions deliberately differ from undyed timber samples. Thin rigging remains a silhouette exception to the codex two-texel geometry rule. Rope's larger blotches and lower contrast intentionally avoid harsh fine strand noise; cream linen has restrained value contrast because folds receive scene lighting. Final individual exceptions are listed below after measurement. Style exceptions are disclosed rather than treated as passes.

No in-game or dedicated-server result is claimed: gameplay integration remains a separate task.

## Decked-v2 measured exceptions (historical)

29 of 38 paint checks pass. The nine exceptions are: rope span 0.19 (reference 0.20), blotches 0.68 m (single reference 0.02 m) and local contrast 0.39 (0.80–0.93); red sail blotches 1.95 m (0.05–0.92) and local contrast 0.12 (0.23–0.82); metal blotches 0.68 m (0.04–0.41); undyed linen span 0.06 (0.07–0.53); blue shield value 0.31 (undyed wood 0.34–0.59) and saturation 0.34 (undyed wood 0.50–0.57). Rope and metal deliberately omit fine strand/scratch noise. Large, subdued patches on the red sail avoid the initial noisy cloth appearance. Shield paint is intentionally cooler and less saturated than raw wood. Linen receives fold shading from geometry and lighting.

The v2 atlas density was 23.6 px/m, slightly coarser than the general world-prop guide's 25–45 px/m. This is an accepted tradeoff for the added deck, interior and cargo without raising the existing 2048 atlas size. No ship-specific texture-density survey exists. The supplied close-up renders were checked for legibility.

## Lookout and bow swivel revision plan

Add a narrow rigid ladder aft of the mast with about 0.6 m between its stiles, closely spaced rungs, a clear approach from the upper deck, and an open access hatch through a 2.9 m lookout platform above the yard. Supply separate ladder/nest meshes, floor collision markers and entry, top and side-exit anchors. Reroute stays to clear the climbing lane. Preserve existing hull size and hold headroom; allow the lookout railing and short mast tip to increase overall height.

Add a small stylized timber-and-iron cannon on the bow centreline, with a fixed pedestal and separate rotating yoke and barrel. The yaw pivot rotates about ship-local up, limited to -90 through +90 degrees around bow-forward. Add a Blender limit constraint, a demonstration sweep animation, and portable interaction metadata documenting the same range. Keep the barrel above the bow rail throughout the sweep. This is fictional game art, not a mechanical firearm design. No firing simulation, damage, ammunition, networking or player controller is claimed.

Preview the full ship, ladder/nest close-up and cannon close-up in color. Validate all new parts survive GLB export, the yaw end stops clamp correctly, the climbing aperture remains open, and the original stairs and cargo hold keep their clearances. Preserve v2 in revisions/Havorn-decked-v2.zip before rebuilding.


## Lookout/cannon implementation

The new meshes add no copied vanilla art. The lookout is above the yard, its floor 17.21 m above the local waterline origin. The forward aiming arc is constrained in Blender and provided as runtime metadata; exporting does not implement game logic. The gun has a dark iron barrel and metal bands, a distinct fixed pedestal, yawing yoke, barrel pivot, operator and muzzle markers. The previous deck size and 2.24 m hold clearance are unchanged; overall height increases to about 20.85 m.

The modeled ladder is 0.59 m clear between stiles with 46 rungs about 0.31 m apart. The nest hatch and its floor collider markers are verified clear. The lower rim is interrupted at the ladder aperture so it does not block ascent. The short right stile ends below the landing; the taller left stile is a handhold. Ladder entry is in front of the stair opening, and its brackets actually reach the mast. A sampled authoring capsule checks clearance from upper deck, mast/rigging and lookout geometry, excluding the hand-contact ladder itself.

The cannon's requests at -130, -90, 0, 90 and 130 degrees are checked against the constrained -90, -90, 0, 90 and 90 result. GLB re-import checks the fifteen meshes, parent hierarchy, interaction anchors, and the animation's port/forward/starboard frames. The viewport opens in color. No Valheim player interaction, firing or multiplayer validation is claimed.


## Final v3 checks

The final mesh is 16,125 triangles with 23.2 px/m average atlas density. The same large-asset density tradeoff as v2 applies. 28 of 38 paint lines pass. Ten disclosed exceptions remain: metal blotches 0.60 m; red sail blotches 1.90 m and local contrast 0.15; rope value 0.53, span just below 0.20, blotches 0.69 m and contrast 0.37; cream cloth span 0.06; blue shield value 0.32 and saturation 0.34. The intentional muted-dye, broad-patch and low-noise choices explained above still apply. No ship-specific category geometry statistics exist.

The 1,510 capsule samples clear the tested deck/mast/rigging/nest surfaces. Nest and companionway floor geometry and collision markers leave their apertures open. Requested cannon rotations beyond both ends clamp to +/-90 degrees, and its exported GLB sweep hits port/forward/starboard correctly. FBX re-import also verifies dimensions, fifteen visual parts and cannon parent hierarchy. These checks validate the art/authoring rig only.


## Access and helm revision
Open the port boarding rail at the side ladder and all aft lookout rail levels at the mast ladder. Replace the side steering oar with a centreline stern rudder and add an eight-spoke captain's wheel, pedestal and visible steering ropes. Clear the helm standing space and reroute the aft stay. Rebuild shields as closed painted disks with integral rims on outward-facing mounts following the hull curvature, clear of both boarding access and hull surfaces. Preserve the original hull scale; the external stern rudder may extend overall length. Validate surface clearance and both rail openings, export separate helm/shield parts, and supply colored detail views. The prior version is archived in revisions/Havorn-lookout-v3.zip.


## Final access/helm revision checks
17,549 triangles, 19 visual parts, 23.3 px/m average atlas density. Overall length including the stern rudder is 23.97 m; original hull scaling and hold clearances are preserved. Both rail corridors pass sampled clearance checks; the shields have no surface intersections against hull, upper deck or deck rails, with minimum sampled vertex clearance 0.1455 m. The prior mast climbing, hatch, stair, cannon constraint and export checks pass. Color renders and all three vanilla lineup views were inspected.

30 of 38 paint checks pass. Eight measured exceptions remain: metal blotches 0.69 m; rope value 0.53, blotches 0.69 m and local contrast 0.36; red paint blotches 2.75 m and local contrast 0.15; linen value span 0.06; blue paint saturation 0.33. These retain the earlier documented muted paint, broad subdued patches, low-noise rope/metal and geometry-lit linen choices. The atlas-density exception remains intentional for this large furnished ship. Gameplay has not been tested.


## Low bow/stern and clean ladder revision
Remove the isolated nest exit handhold and floating hatch grip, and terminate both mast ladder stiles evenly at the lookout floor. Lower the raised bow and stern hull sheer, gunwale and carved stem details to the deck railing height (approximately 4.0 authoring units), preserving the lower hull, deck, rudder and overall horizontal dimensions. Retain the short original bird carving within the new bow height. Inspect colored nest and full ship renders and retain existing clearance/export checks. Archive the prior version as Havorn-helm-v4.zip.


Final low-end revision: 17,509 triangles; both new height checks, existing rail/shield/climb/stair checks, and FBX/GLB re-import checks pass. Colored full ship, helm and lookout views plus all vanilla lineup views inspected. The new UV packing measures 22.0 px/m, within the already accepted large-asset atlas tradeoff. 29 of 38 paint checks pass. Exceptions: metal blotches 0.82 m; rope value 0.53, span just under 0.20, blotches 0.73 m and contrast 0.37; red blotches 2.55 m and contrast 0.19; linen span 0.06; blue saturation 0.34. The muted-dye, broad-patch, low-noise and geometry-lit linen reasons from prior revisions still apply.


## Finished ends, hanging shields and forward cannon
Replace straight shield brackets with two solid iron straps per shield, wrapping over the handrail and returning downward on its inboard face. Remove the half-buried ochre diamond meshes beneath the shields. Rebuild the bow/stern with fitted end caps, deck boards to both tips and continuous end rails. Lower the end bulwark to the deck edge so it does not intersect the completed railing; retain the previous overall railing height. Remove the overlapping old keel stems and bird piece. Keep the established world scale fixed. Raise the cannon barrel/pivot by 0.35 authoring metres and move the entire assembly 0.8 metres toward the bow, extending its pedestal down to the existing deck. Update pivots, anchors, preview camera and validate clearance through its full forward aiming sweep.


Final finished-end revision: 18,313 triangles, 19 visual parts, 23.72 m overall length including rudder. End floor ray samples and closing handrail samples pass, alongside existing shield, rail, ladder and stair checks. Raised/forward cannon clears hull/deck/rails in 37 aim positions across 180 degrees. FBX/GLB re-import checks pass. Full ship, boarding hooks, bow cannon, stern helm and three vanilla lineup views inspected.

Atlas density is 22.9 px/m under the existing large-asset exception. 29 of 38 paint lines pass. Remaining measurements: metal blotches 0.78 m; rope value 0.53, span just under 0.20, blotches 0.61 m and contrast 0.39; red blotches 2.61 m and contrast 0.17; linen span 0.06; blue saturation 0.33. The previously stated muted paint, broad low-noise patches and geometry-lit cloth choices still explain these exceptions.

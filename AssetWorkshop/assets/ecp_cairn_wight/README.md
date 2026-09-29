# Cairn Wight

Original low-poly grave-bone mask, broken antlers, slate cairn and ragged burial hide,
fitted to the vanilla Fenring. The game supplies the animated body at runtime.
694 triangles, one 256 px point-filtered baked albedo, no emission or normal map.

The muted ochre bone, brown hide and charcoal stone follow the local vanilla swamp
reference board (`ecp_swamp_set/out/vanilla_reference.png`): broad tonal variation,
dark recesses and chunky silhouettes. All geometry and paint shipped here are original.

Build with `AssetWorkshop/build.ps1 -Asset ecp_cairn_wight`.
Run `review.py` with Blender to render both this kit and Scree Wing on local game
references. `inspect_reference.py` writes reference rest transforms and stills.
Reference previews are local only and must never be bundled.

Fit: geometry is in the **unscaled vanilla prefab root rest frame**, Blender Z up,
front -Y. Instantiate at the creature root with local identity, then parent to its
`Head` bone while preserving world transform. Apply creature scale at the root.
This preserves the game rig's inherited 100x scale correctly. Do not zero the
attachment transform after reparenting. Rest fit was visually checked; animated
gameplay validation remains part of the runtime integration.

# Skeleton arsenal

Eight custom weapon assets: dagger, mace, spear, atgeir, sword, axe, bow and arrow.
All blades, heads, shafts and bow limbs are bone, with hide grips and sinew bindings.
Every weapon incorporates anatomical vertebra geometry: dagger guard, sword grip,
mace head, axe and spear collars, atgeir spine, bow arrow shelf, and arrow bead.
Models are in the sibling `ecp_skel_<weapon>` folders, with editable Blender files,
FBX, albedo/normal textures and manifests in each `out` directory.

## Watch in Blender

```powershell
& "$env:USERPROFILE/tools/blender/blender.exe" ./out/blender/skeleton_arsenal.blend --python ./blender_play.py
```

Run the command from this folder.
Space pauses/resumes. The 50-second timeline tracks along the lineup, then cuts to
Dagger, Sword, Axe, Mace, Spear, Atgeir and Bow close-ups. Each attacks repeatedly.
The final shot shows the existing vertebra drop. Named cameras are in the Outliner.

The characters are the original game's Skeleton visual and skin, baked in Unity
through its humanoid Animator. Sword, mace and bow use the creature's game clips;
dagger, axe, spear and atgeir use the game's corresponding player weapon clips on
the Skeleton avatar. The bow has an animated string, nocked arrow and arrow flight.
These are vertex-cache animations in Blender; editable skeleton bone actions are
not included. Rebuild their motion through `unity/Assets/Editor/SkelArsenal`.

Keep the seven `out/blender/ecp_skel_*` cache folders beside the `.blend` when moving
the scene. Textures are packed. Original game content remains in local reference
and preview output; the weapon bundles contain the custom assets.

## Review and validation

- `out/arsenal_sheet.png`: all custom models and detail views.
- `out/attack_review.png`: three frames from every showcase shot.
- `out/reference_audit/inventory.json`: 284 files from the local exported Skeleton folder.
- `out/reference_audit/review.json`: original prefab render results, with source paths.
- `out/reference_audit/creatures.png`: 13 visible creature variants in manifest order.
- `out/reference_audit/weapons.png`: 13 original weapon variants in manifest order.
- `out/validation.json`: eight export checks, seven skeleton/weapon motion checks,
  finite cache coordinates, matching vertex/frame counts, texture/cache dependencies,
  and a bow release frame inside the attack loop.

`Skeleton_aspect.prefab` is inventoried but gives no visible mesh through the reference
importer. Particle effects and game shaders are not reproduced exactly by Blender.
The audit covers the local reference export, rather than asserting coverage of a
different installed game version.

The weapon models, Unity animation bake and original showcase already existed in
the workspace when reviewed. This pass audited the references, rendered attack
checks, corrected reversed detail-sheet framing, packed the scene textures, made
cache paths relative, and added playback confirmation and dependency validation.

`build.ps1 -Open` rebuilds the assets, bundles, animation bake and scene and opens
Blender. `reference_audit.py`, `present.py` and `validate.py` run through Blender's
`--background --python` option; validation takes the showcase `.blend` as input.

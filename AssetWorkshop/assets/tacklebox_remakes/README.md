# Tacklebox remakes

PackPanel's four tackleboxes, built from original geometry and material recipes. Since 2026-09-28 they are the
models in PackPanel's `packpanel_tackleboxes` bundle and its `tacklebox_<tier>.png` icons, in the asset folders
`../packpanel_<tier>_tacklebox/` (each `model.py` calls `designs.build(tier)`). The earlier tackleboxes and the
finewood study they replaced were deleted.

| Tier | Shape | Materials | Overall width | Triangles |
| --- | --- | --- | --- | --- |
| Driftwood | Small, flared creel with a low lid | Salt-worn wood, deerhide, flax bindings, bone toggle | 0.399 m | 1044 |
| Finewood | Rectangular chest with a raised, faceted lid | Finewood, blue-gray trollhide, bronze | 0.462 m | 1444 |
| Carapace | Rounded case with overlapping raised shell shields | Yggdrasil wood, dark teal carapace, worn shell edges | 0.540 m | 683 |
| Flametal | Chamfered strongbox with a sloped lid | Dark flametal, warm worn edges, Asksvin leather | 0.632 m | 1228 |

Driftwood keeps its original side float and hook. Finewood has a bronze fish plaque and a secured line spool;
carapace has a decorated fang clasp and shell spoon lure against the front; flametal has a forged flame emblem and
three hooks under a lid keeper. Their bodies are unchanged. Ground pivots, Blender Z-up, front -Y. Each asset has a body box collider,
256-pixel albedo and normal atlases, FBX export, packed Blender file, and four-view preview in its own `out/` folder.

Additional finish: driftwood hide stitching, tied bone charm and a carved wave; finewood bronze wave inlays and
trollhide stitches; carapace growth ridges; flametal corner diamonds, rivets and Asksvin-hide stitches.

## Review

Open `out/Tackleboxes_Review.blend`: all four in progression order, labelled and fully textured, with material preview
enabled. Textures are packed into the blend file. The cameras named `Driftwood close-up`, `Finewood close-up`,
`Carapace close-up`, and `Flametal close-up` give individual views; `All four - comparison` shows the lineup.
`out/tackleboxes_lineup.png` is the rendered comparison.

The individual editable files are in `../packpanel_<tier>_tacklebox/out/` and also open in material preview.

## Build

Run Blender in background mode with `--python AssetWorkshop/assets/tacklebox_remakes/present.py`.
This rebuilds all four assets, bakes original textures, exports them, packs textures, renders previews and creates the
review scene. `-- --gallery-only` recreates only the comparison from existing built models.

## Into PackPanel

From the workshop root, then copy the results and rebuild PackPanel:

```
.uild.ps1 -Asset packpanel_driftwood_tacklebox,packpanel_finewood_tacklebox,packpanel_carapace_tacklebox,packpanel_flametal_tacklebox -Bundle packpanel_tackleboxes
blender --background --factory-startup --python assets/packpanel_<tier>_tacklebox/icon.py    (each tier)
```

`out/bundles/packpanel_tackleboxes.windows` and `.linux` go to `PackPanel/PackPanel/assets/bundles/`; each
`assets/packpanel_<tier>_tacklebox/out/icon.png` goes to the asset folder's `icon.png` and to
`PackPanel/PackPanel/assets/tacklebox_<tier>.png`. The prefab names (`packpanel_<tier>_tacklebox`) are what PackPanel
loads; do not rename the folders.

## Art references

Visually inspected the locally exported vanilla `finewood_d`, `woodchest_d`, `TrollLeatherArmorChest_d`,
`bronzebuckler_d`, `Carapace_d`, `asksvinsaddle_d`, `yggbark_d`, and `FlametalArmor_d` textures.
The work uses their broad palette and low-resolution, faceted approach; none of those textures or game meshes
is embedded in these assets. Shown in Blender lighting; final Valheim lighting has not been visually tested.

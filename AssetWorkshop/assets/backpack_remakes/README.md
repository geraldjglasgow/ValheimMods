# PackPanel backpacks

The eight backpacks PackPanel hangs on the player's back, in `../packpanel_<pack>/` (deerhide satchel, trollhide
backpack, rootbound pack, wolfpelt pack, lox hauler, carapace pack, asksvin pack, moosehide pack). Since 2026-09-28
these are the models in PackPanel's `packpanel_backpacks` bundle and its `backpack_<word>.png` icons. The earlier
versions, the preserved original entry points and the rigid shared-shape experiments were deleted then.

The trollhide backpack is the reference: its source and palette are the original fitted construction (2,932
triangles). The other seven keep that soft-body, surface-fitted construction and their recipe details, with materials
refined to sit beside it:

- Deerhide: muted tan hide, scrap patches, broad wear and hand stitching.
- Rootbound: irregular twisted roots, dark iron and subdued guck seam seal.
- Wolfpelt: layered grey wolf fur, a darker saddle and silver closures.
- Lox: shaggy hide load on a blackmetal frame with weathered linen lashings.
- Carapace: green-black chitin and worn olive edges, blue jute, a chitin clasp.
- Asksvin: olive scaled hide informed by vanilla asksvin art, flametal hardware and a shaped lobed flap.
- Moosehide: winter fur and dark hide, subdued worn gold and sinew details.

Each asset folder's `model.py` and its local helpers (`fit.py`, `forms.py`, `looks.py`, ...) build it; `icon.py`
renders its 128 px inventory icon framed to the model's outline. The recipes are in
`PackPanel/PackPanel/config/PackPanel.Backpacks.yml`.

## Review

`present.py` (this folder) rebuilds all eight through the workshop pipeline, packs each `.blend` for material preview,
renders each `out/preview.png`, and writes `out/Backpacks_Review.blend` (outward), `out/Backpacks_Rear_Review.blend`
(wearer-facing) and the renders `out/backpacks_lineup.png` and `out/backpacks_rear_lineup.png`:

```
blender --background --factory-startup --python-exit-code 1 --python AssetWorkshop/assets/backpack_remakes/present.py
```

`-- --gallery-only` redoes only the comparisons; `-- --finish-only` repacks and renders the existing models.

## Into PackPanel

From the workshop root, then copy the results and rebuild PackPanel:

```
.\build.ps1 -Asset packpanel_deerhide_satchel,packpanel_trollhide_backpack,packpanel_rootbound_pack,packpanel_wolfpelt_pack,packpanel_lox_hauler,packpanel_carapace_pack,packpanel_asksvin_pack,packpanel_moosehide_pack -Bundle packpanel_backpacks
blender --background --factory-startup --python assets/packpanel_<pack>/icon.py      (each pack)
```

`out/bundles/packpanel_backpacks.windows` and `.linux` go to `PackPanel/PackPanel/assets/bundles/`; each
`assets/packpanel_<pack>/out/icon.png` goes to the asset folder's `icon.png` and to
`PackPanel/PackPanel/assets/backpack_<word>.png` (`deerhide`, `trollhide`, `rootbound`, `wolfpelt`, `lox`, `carapace`,
`asksvin`, `moosehide`). The prefab names (`packpanel_<pack>`) are what PackPanel loads; do not rename the folders.
Armour, cape and animation fit is checked in game.

Vanilla wolfhide, carapace and asksvin art and the original trollhide atlas were inspected as references. The textures
are procedurally generated originals, not copied vanilla artwork.

# Material-colored slot icons

Thirteen original item-hint illustrations replace the flat bronze slot art: helmet, leather tunic, trousers, trollhide cape, trollhide backpack, utility belt, roasted food, drinking horn, arrows, coins, keys, tacklebox and fishing tackle.

`../slot_icons.py` builds native Blender geometry and renders transparent RGBA PNGs with consistent lighting and safe margins. The backpack and tacklebox append the existing ValheimAssets trollhide backpack and driftwood tacklebox models. All other icon geometry and material colors are original. Vanilla cooked deer-meat art and the original trollhide model informed the material/shading direction; no vanilla bitmap is copied into the exports.

- `icon_*.png` here: 256px source renders.
- `../../PackPanel/assets/icon_*.png`: 64px runtime images embedded by the project.
- `comparison.png`: the actual 64px runtime images enlarged on a dark background for review.
- `lighting_reference.blend`: packed backpack scene documenting the icon lighting.

Rebuild from the workspace root:

```powershell
& 'C:/Users/gglasgow/tools/blender/blender.exe' --background --factory-startup --python-exit-code 1 --python PackPanel/artwork/slot_icons.py
```

The script replaces only the slot icon PNGs, not panels, buttons, wallpaper or item inventory icons. `-- --sheet-only` regenerates the comparison from the runtime PNGs.

SlotIcons now resolves the purse through `icon_purse.png`, just like the other slots. All hints retain their material colors with a uniform 0.85 alpha. The key-ring button uses the same hint tint. skin_art.py preserves these files when generating legacy panel artwork; its `--legacy-icons` option explicitly restores the former bronze set.

Validation: all 13 outputs are 64x64 RGBA with transparent margins; PackPanel Release build succeeds. Actual game UI appearance still needs a runtime review after deploying the rebuilt DLL.

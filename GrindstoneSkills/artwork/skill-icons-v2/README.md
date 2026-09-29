# Grindstone Skills — vanilla-inspired icon remakes

Nine original 2D icons for Defense, Sailing, Foraging, Husbandry, Cooking, Farming, Fishing,
Woodcutting and Pickaxes. Generated using the built-in imagegen tool, with the exact prompts and
generated source paths recorded in [generation.json](generation.json).

The art direction was checked against the installed game's original skill sprites, especially its
simple blocking shield and woodcutting tool. The replacements use isolated objects, matte painted
facets, muted wood/iron colors and clear silhouettes. The old gold wreaths, busy scenery and badge
surrounds have been removed. Native game artwork is not included in these replacement images.

## Review

Open `Grindstone_Skill_Icons_Review.blend`. It is a **2D review board**, not a set of 3D models.
Each card shows a large view, the previous icon at 64 pixels, and the replacement at 64 and 32 pixels.
The Blender file packs its image textures and font. Use the front camera view; zoom in to inspect.
`review-board.png` is the exported comparison at 1440 × 1600.

- `masters/`: generated 1254 × 1254 transparent PNGs, unaltered.
- `icons32/`, `icons64/`, `icons128/`: RGBA exports, same composition, bicubic downsampling.
- `previous/`: copies of the current mod icons for comparison and rollback.
- `build_review.py`: rebuilds the board using Blender; does not install anything.

Validation: all nine masters have transparent corners; every icon has 32, 64 and 128 pixel exports.
The rendered review board was inspected on a dark neutral background for readability and alpha edges.

**Deployed 2026-09-28:** the 64-pixel exports replaced the `skill_*.png` files under `GrindstoneSkills/assets`
(the size the game's own skill icons use). To roll back, copy `previous/` over them and rebuild.

Rebuild the review board from the repository root:

```powershell
& "$env:USERPROFILE/tools/blender/blender.exe" --background --factory-startup --python GrindstoneSkills/artwork/skill-icons-v2/build_review.py
```

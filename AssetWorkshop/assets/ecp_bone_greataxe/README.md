# Bone greataxe

A 1.500 m tall all-bone greataxe with twenty-two slim, interlocked vertebrae following a gentle S-curve,
an enlarged atlas socket, a bearded scapula blade and a hooked bone poll.
The two hand positions have shorter vertebral processes. Grounded pivot;
Blender Z is up, the blade extends along +X. Dimensions are baked in metres.

Reference inspected locally: `Characters/Skeleton/model/Texture/Skeleton_d.tga`
(128 x 128), its importer (point filtering), and `model/Materials/Skeleton.mat`
(white tint, metallic 0, glossiness 0.24, albedo plus normal texture).
The authored 256 x 256 atlas follows the reference's warm tan/cream bone,
painted brown recesses and broad shading. It contains no copied game texture.
The axe uses geometry and painted shading rather than a fine bump texture.

Build from the workspace root:

```powershell
& "$env:USERPROFILE/tools/blender/blender.exe" --background --factory-startup --python-exit-code 1 --python AssetWorkshop/assets/ecp_bone_greataxe/build.py
```

Uses the existing geometry/material helpers in `ecp_spine_greataxe` and
`ecp_skel_arsenal`. The comparison also needs the local ValheimReference export.
The approved head is preserved with its original UVs in `approved_head.blend`.
`original_held_source.blend` preserves the original finished scene.

Run `held_showcase.py` with Blender after building to make the original Valheim
Skeleton hold the axe and play a five-second idle loop (24 fps, frames 1–120;
frame 121 matches frame 1). Hand IK targets follow the axe and feet stay planted.
`check_idle.py` checks the seam, hand reach, foot drift and actual movement.
Open `out/skeleton_holding_axe.blend` with `--python open_idle.py` to start
textured playback automatically. Space pauses/resumes the animation.

Outputs in `out/`: editable packed-texture `.blend`, `.fbx`, albedo PNG,
dimensions/triangle manifest, four-view preview, and `skeleton_comparison.png`
with the actual game Skeleton under the same Blender lighting. The comparison
blend is local reference only; the exported axe contains only the new geometry.

For game integration, use point filtering, metallic 0, smoothness 0.24 and the
game Skeleton shader/material dressed with the new albedo. This delivery is
the model and texture; it has not been installed or checked inside Valheim.

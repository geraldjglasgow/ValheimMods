# Bone Reaver — model and animation kit

Working name for EliteCreaturesPack's two-handed bone-axe skeleton. Built against the locally installed
Valheim `Characters/Skeleton/Skeleton.prefab`, using the actual body's proportions and skin weights.
The normal skeleton's reference-pose height is 1.989042 m; this rig is exactly 1.25 times that, 2.486303 m.

## Open and compare

- `out/compare_valheim_skeletons.blend`: Rancid Remains on the left, ordinary vanilla Skeleton in the middle,
  the dressed Bone Reaver on the right, all at prefab-relative scale. The reference bodies are in their prefab
  T-pose; the Reaver is in its axe-dragging idle. These are actual extracted game meshes, not stand-ins.
- `out/ecp_bone_reaver.blend`: editable model, 76-bone rig, individual gear meshes and 16 baked actions.
  The reference body is named `REFERENCE_SkeletonBody`; original equipment has descriptive mesh names.
- `out/preview.png`, `out/attacks.png`, `out/comparison.png`: rendered inspections.
- `out/axe_detail.png`, `out/satchel_detail.png`: close-ups of the curved spine and belt-mounted knife roll.
- `out/unity/ecp_bone_reaver.fbx`: original gear, rig and clips, **without the game's body or textures**.
- `out/reference_unity/ecp_bone_reaver_preview.fbx`: full local reference preview; never distribute it.

In Blender select `BoneReaverRig`, change a Dope Sheet editor to **Action Editor**, select an action,
and play its frame range. The file starts on `idle_R`. Coordinates are metres, Z up, forward -Y.
The FBX exporter bakes the half-turn required for the workshop's Unity forward +Z convention.

## Model

The bone axe has 18 individually sculpted vertebrae following a gentle S-curve. Each has a flattened, kidney-shaped
body with bearing rims, connected pedicles around an open posterior canal, overlapping articular joints,
short transverse processes and a rearward dorsal process. The component forms are fused into one bone mesh
before binding. A forked bone socket joins the shaft to the head. Sinew wraps, a sharpened
scapula-shaped blade, a rear tusk and visible blood splashes complete the weapon. The three
dangling tooth ornaments and incised rune decorations were removed. The blade is one continuous mesh with a convex cutting edge, exposed
honed bevel and pointed ends. Both faces have real sculpted relief: a thick root at the socket, branching raised
bone ridges and a recessed central face, tapering to the cutting edge. Irregular surface relief, branching
fractures and small edge chips break up the plate. A packed painted texture adds aged ivory variation, pores,
fracture staining and dark blood along the cutting edge. Spiral rawhide strips and crossed sinew lash the socket. `out/axehead_detail.png` and `out/axehead_profile.png` show its depth.
Weathered brown burial cloth
covers one shoulder and the waist with folds, uneven overlapping panels, holes and torn hems. A flattened
leather knife roll follows the outside of the hip, supported by two belt loops, with a rolled opening,
stitched seams and angled handles. The drawing hand tracks the pouch's new position through the hip bone.
The current triangle count is recorded in `out/manifest.json`. Original textures are packed into the editable blend.

The `axe` and `daggers` bones control the held weapons independently. `eye_glow` controls the purple tell,
and `bag_daggers` controls the visible handles. The rest of the rig retains the vanilla bone names.
Equipment is skin-bound; the arm motion is baked from a two-bone IK solve with a common weapon transform.

## Actions and timing

All actions use 30 fps and have `_R` / `_L` variants for the incoming axe rest side. Frame zero is included.

| Action | Last frame | Behaviour / cues |
| --- | ---: | --- |
| `idle` | 60 | Loop, both hands on the trailing axe |
| `walk` | 48 | In-place dragging walk, alternating steps |
| `slam` | 108 | Raise overhead, `slam_impact` at 53; cue the rock burst at contact |
| `spin` | 192 | Circular two-handed swing; hit window 34–68; recovery 72–192 is exactly 4 s |
| `sweep` | 90 | Front sweep, hit window 30–51; ends in the **opposite** side's idle |
| `rear` | 108 | Head rotates backward; purple eyes on frames 12–26 (exactly 0.5 s); rear impact at 49 |
| `daggers` | 96 | Reach into the bag, fan three knives between the fingers, release at 49 |
| `axe_throw` | 192 | Circular windup; release at 75, yell at 120 (1.5 s later), axe materializes at 148 |

The Blender action markers and JSON manifest contain every cue. The Unity importer adds `ReaverCue(string)`
animation events. The controller returns a sweep to the opposite idle and every other attack to its starting side.
The release frames hide the held weapon; gameplay must create the moving projectile at that frame.

## Rebuild

From this directory: `./build.ps1 -Preview -Unity -Open`.

`body.py` imports and binds the body; `geometry.py` / `equipment.py` build the custom parts;
`spine_shape.py` curves the shaft while keeping both grip seats fixed; `satchel.py` fits the knife roll;
`rags.py` builds the brown draped cloth;
`vertebrae.py` sculpts the connected bones and `weapon_review.py` renders rapid front/back and joint close-ups;
`axe_blade.py` sculpts the sharpened blade; `bone_surface.py` paints aged bone, fractures and blood;
`bindings.py` builds the spiral strips, knots and crossed socket lacing;
`motion.py` solves and bakes the actions; `stage.py` renders them; `compare.py` stages the vanilla models;
`export_unity.py` exports separate kit/reference files. `validate.py` checks the saved blend, not just constants.

Unity output is staged under `Assets/Creatures/ecp_bone_reaver` and `Assets/Reference/BoneReaver`.
`Workshop.BoneReaverBuild.Run` imports both, builds controllers and prefabs, and creates the rock impact,
bone shatter, blood drip and materialization particle prefabs. Reference dependencies are rejected for the kit.

## Validation and integration boundary

Validated normalized weights on every deforming vertex, both directions of sweep-to-idle continuity,
all looping and attack endpoints, exact requested timing, weapon visibility, and the 1.25 scale.
The maximum baked two-hand wrist-target error was below 0.001 mm. Unity imported all 16 clips and both prefabs.
The reports are `out/validation.txt`, `out/manifest.json` and `out/unity.log`.

This task supplies the model/rig/animation assets. It does **not** install a spawning creature or implement
combat AI in EliteCreaturesPack. Runtime integration still needs to bind the game's body to this rig by bone name,
recompute its bind poses for these rest axes, register the prefab, select melee vs ranged attacks, remember rest side,
dispatch the cues, spawn/aim the three dagger and fast axe projectiles, perform damage and collision checks,
shatter the axe on impact, play the yell sound and replicate decisions over the network. The particle prefabs
are cosmetic building blocks; their runtime triggering and the optional continuous blood drip are not connected.
Do not publish the local reference FBX or blend as mod assets; the installed game supplies its skeleton at runtime.

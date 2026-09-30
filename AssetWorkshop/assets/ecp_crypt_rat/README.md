# Crypt Rat

Original undead rat model made entirely in AssetWorkshop. Faceted grey-green hide,
exposed ribs and spine, large incisors, bare articulated tail. 3370 triangles,
one 256 px albedo/normal atlas, 25 bones and seven authored animation clips.

The revised performance has a five-second idle with sniffing bursts, head turns,
breathing and independent ear flicks; an articulated scurry with a planted support
phase and lifted paws; and a crouch, leap, jaw snap, landing and recovery attack.
Paw placement is solved before baking ordinary animation keys, so no IK plugin is needed.
The 9.37-second preview follows seeded semi-random curved paths, stops to investigate,
lunges, returns home and loops. Gait phase follows distance travelled.
This is authored Blender preview motion, not live game navigation or AI.
Scurry travel is approximately three times faster than the original performance, with
short pauses and three-frame transitions. In-place run/scurry cycles take eight frames;
the moving preview continues to match foot cadence to distance travelled.
The tail is a continuous, blended skin over five joints. Its motion is baked from a
damped chain in world space, with gravity, momentum, floor contact and bend limits:
turns lead at the base and trail at the tip, and stops/landings produce a settling swing.
It has no constant idle wag. This bake needs no physics add-on to play in Blender.
The lunge uses one shared motion curve for the body, planted paws, tail simulation and
preview travel: hind-leg acceleration, a low ballistic arc at constant forward speed,
front-paw landing, braking, then hind-paw contact and a small recovery step.
Windup pulls the body 17 cm backward over planted paws. Forward travel takes 8.33 frames
(20% faster than the previous ten-frame spring), with the bite closing at frame 20.
Jaw opening is restrained. Paw-contact checks verify that
the hind paws do not slide during push-off or the front paws during landing.

Build from the workspace root:

```powershell
powershell -ExecutionPolicy Bypass -File AssetWorkshop/assets/ecp_crypt_rat/build.ps1 -Video
```

`WORKSHOP_BLENDER` can override the local Blender executable. Output stays under `out/`.

| Output | Purpose |
| --- | --- |
| `out/ecp_crypt_rat_performance.blend` | Textured 9.37-second idle/scurry/lunge scene with timeline markers |
| `out/crypt_rat_performance.mp4` | Full animation preview (`-Video`) |
| `out/performance_sheet.png` | Twelve samples from the roaming performance |
| `out/motion_checks.json` | Loop continuity, planted paws, scurry lift and travel checks |
| `out/tail_checks.json` | Tail segment lengths and floor-contact checks |
| `out/preview.png` | Four views including a 1.8 m scale figure |
| `out/crypt_rat_lunge.mp4` | Crouch, 0.95 m lunge, bite and recovery preview |
| `out/bite_sheet.png` | Six bite poses, left to right: frames 0, 14, 18, 20, 24, 54 |
| `out/animation_sheet.png` | Idle, walk, run, bite, stagger, death |
| `out/ecp_crypt_rat_animated.blend` | Editable rig and all seven animation actions |
| `out/ecp_crypt_rat_animated.fbx` | Skinned mesh and in-place actions, export facing Unity +Z |
| `out/animation_contract.json` | Clip ranges, tags, bite event, footsteps, runtime lunge movement |
| `out/ecp_crypt_rat.blend` / `.fbx` | Baked static model |
| `out/lineup/` | Local-only comparison beside Boar, Wolf, Draugr and Skeleton |
| `out/style_report.txt` | Measured style checks; eye-region exception explained in BRIEF.md |

The animation FBX is the creature asset. The workshop's ordinary static bundle builder
is not a creature controller builder. The contract specifies the Unity callbacks and
footstep curves to attach during integration. Preview travel is separate from the
in-place animation so the future character owner controls collision and movement.

Gameplay specification is in `gameplay.json`: Swamp and Sunken Crypt spawning only,
60 base health, 14 direct bite damage, five bleed stacks per landed bite, ticking
every 1.5 seconds for 5, 4, 3, 2, 1 damage before expiring. Drops bones and 0–1 IronOre.
Bone quantity, repeat-bite stacking policy and lunge distance are proposed defaults,
explicitly marked in that file. No spawn system, combat status effect, loot table,
Unity creature controller, mod changes, installation or release is included.

Validation: Blender builds and exports successfully, every vertex has normalized
weights to an existing bone, motion checks pass, reference lineup inspected.
The game and a dedicated server have not run this creature. Reference game assets
appear only in the local lineup; exported rat assets contain original geometry/paint.

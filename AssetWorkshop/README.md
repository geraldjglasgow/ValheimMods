# AssetWorkshop

3D assets for the mods, made from scripts. Blender builds and textures a model from a Python file, renders a
preview sheet to check it by eye, and exports it. Unity packs one or more models into an asset bundle that a mod
embeds, and ValheimModLibs' `BundlePrefabs` brings it into the game. Creatures go further: a rig, animation clips, an
animator controller built against the game's creature code, previews staged in Blender and replayed live in Unity.
Nothing here ships on its own.

```
AssetWorkshop/
  build.ps1            Blender for each asset, then optionally Unity for the bundle
  rip-reference.ps1    exports the game's own assets as a local reference project (never committed)
  blender/
    run.py             Blender entry point for one asset
    workshop/          helpers: shapes, materials, bake, export, preview, pipeline
  assets/<name>/
    model.py           the asset: build() makes the meshes
    out/               build output (gitignored)
  assets/crypt_mimic/  the first creature (see "Creatures")
  assets/ecr_slinger/  a game creature with new gear, the Greydwarf Slinger (see its section)
  assets/ecr_rimegiant/ gear fitted to a game creature's body, the Rime Giant (see its section)
  assets/ecp_kraken/   a creature of our own animated in code, the Kraken (see its section)
  unity/               the bundle project (Unity 6000.0.75f1); Assets/Editor holds the batch builds,
                       Assets/Preview/Scripts the runtime preview (fight replay, HUD)
  out/bundles/         built bundles (gitignored)
```

## Tools

Installed on 2026-09-27 into `%USERPROFILE%\tools`, so nothing needed admin rights.

| Tool | Where | For |
| --- | --- | --- |
| Blender 5.2.1 LTS, portable | `tools\blender\blender.exe` | modelling, baking, previews; run headless |
| Unity 6000.0.75f1 + Linux build support | `tools\Unity\Editor\6000.0.75f1\Editor\Unity.exe` | building bundles (Windows and Linux); run headless |
| Unity Hub 3.21 (winget, MSIX) | Start menu | installs editors |
| AssetRipper 2.0.0 | `tools\AssetRipper\AssetRipper.GUI.Free.exe` | the reference export |

`build.ps1` finds Blender and Unity there; override with `-Blender`/`-Unity` or the `WORKSHOP_BLENDER`/`WORKSHOP_UNITY`
environment variables.

- **Unity's version must match the game's exactly.** The game's is the ProductVersion of `UnityPlayer.dll` in the
  game folder (6000.0.75f1, changeset 26349cd2a5c8). When a game update changes it, install the new editor, then
  update `unity/ProjectSettings/ProjectVersion.txt` and the default in `build.ps1`.
- **Unity Hub from a VS Code terminal:** VS Code sets `ELECTRON_RUN_AS_NODE=1`, which makes the Hub run as plain
  Node. Clear it first:
  `Remove-Item Env:ELECTRON_RUN_AS_NODE; & "<Hub folder>\app\Unity Hub.exe" -- --headless install --version <v> --changeset <c>`
  (the Hub folder is `(Get-AppxPackage UnityTechnologies.UnityHub).InstallLocation`).
- **Licence:** the batch build ran without signing in. If it ever stops with a licensing error, sign in to Unity
  Hub once and add the free Personal licence (Preferences, Licenses).
- **Blender downloads:** download.blender.org refuses scripted downloads (403); the official mirrors work, for
  example `https://mirrors.ocf.berkeley.edu/blender/release/`. Check the `.sha256` file.

## Making an asset

1. Create `assets/<name>/model.py` with a `build()` function. The folder name becomes the prefab name the game
   registers, so it must be unique across every mod a player might run: prefix it with the mod
   (`openkeep_supply_crate`). `test_crate` is only the pipeline test.
2. `.\build.ps1 -Asset <name>`. This builds, bakes and exports in Blender and renders `assets/<name>/out/preview.png`.
   Look at the preview, adjust `model.py`, repeat. A build takes a few seconds.
3. `.\build.ps1 -Asset <name>,<other> -Bundle <bundle>`. This also builds the bundle
   `out/bundles/<bundle>.windows` and `out/bundles/<bundle>.linux`, one prefab per asset: a bundle only loads on the
   platform it was built for, and dedicated servers run the Linux player. Bundle names are lower-case.
   `-SkipBlender` bundles what is already in each `assets/<name>/out` (an asset built by a script of its own, such as
   `assets/kraken_beak_set` and `assets/kraken_food`, which write the `ecp_kraken_*` loot assets' `out` folders).

### Conventions

- Metres, Z up. The model's **front faces -Y** in Blender and becomes Unity's +Z (forward).
- The **world origin is the pivot**: put it on the ground under the object, where the game places it.
- Every mesh in the scene is joined into one visual mesh with one material. Meshes named `col_*` are colliders:
  `shapes.collider_box` gives a BoxCollider, and `shapes.collider_mesh(obj, convex=...)` a MeshCollider. Anything
  with a Rigidbody (a dropped item) needs box or convex colliders.
- A model may set `TEXTURE_SIZE` (default 512), `NORMAL_MAP` (default True) and `AO_STRENGTH` (0 to 1, default 0.6).

### Helpers

- `shapes`: `box`, `cylinder`, `sphere`, `collider_box`, `collider_mesh`. Sizes are in metres and scale is applied,
  so bevels stay even.
- `materials`: `flat`, `wood` (grain along an axis), `iron`, `stone`.
- Plain `bpy`/`bmesh` code works too: the pipeline takes whatever meshes are in the scene. Any Principled BSDF node
  setup is baked, including image textures (for example CC0 textures from ambientCG or Poly Haven).

### What the Blender step writes (`assets/<name>/out/`)

- `<name>.fbx`: the joined visual mesh plus the `col_*` colliders.
- `<name>_albedo.png`, `<name>_normal.png`: one baked atlas each. Albedo includes the baked ambient occlusion.
- `<name>.json`: the manifest the Unity step reads (files, colliders, size, triangles).
- `<name>.blend`: open it in Blender to look around or edit by hand.
- `preview.png`: four views in one sheet. Top left: three-quarter view. Top right: front. Bottom left: right side.
  Bottom right: next to a 1.8 m figure (a player) for scale. Each view is also kept as `view_*.png`.

### What the Unity step does

`unity/Assets/Editor/BundleBuild.cs` imports each staged asset and builds `<asset>.prefab`: the visual mesh with a
Standard-shader material carrying the baked textures, and colliders in place of the `col_*` meshes. It then builds
the bundle for StandaloneWindows64 and StandaloneLinux64. It logs each prefab's size and fails when it differs from Blender's, which
catches unit and axis mistakes. The full log is `out/unity.log`. An asset's `<name>_icon.png`, when its `out` has one,
goes into the bundle as a sprite named `<name>_icon` (an item's inventory icon: `bundle.LoadAsset<Sprite>(...)`).

## In a mod: BundlePrefabs

`ValheimModLibs/BundlePrefabs` loads the bundle a mod embeds (one per platform: a bundle only loads on the platform it
was built for, and dedicated servers run the Linux player), copies game prefabs to build on, registers new prefabs in
ZNetScene and ObjectDB on every peer, and swaps a model's bare placeholder materials for the game's own. Nothing of the
game's is ever in a bundle: the game's shaders and textures come from the running game.

From the reference export: building pieces (the wood chest's `woodchest.mat`) use `Custom/Piece`, which reads
`_MainTex` and `_BumpMap`; the simplest way to wear the game's look is to borrow a game material outright
(`GameMaterials.Borrow`), as the mimic does for its teeth (Bone Fragments) and gums (bear meat).

## Creatures

A creature is a rig with parts hung on its bones, animation clips, and an animator controller the game's creature code
can drive. `assets/crypt_mimic/` is the worked example:

| File | Does |
| --- | --- |
| `rig.py` | the skeleton, and the game's chest (reference, preview only) hung on its bones |
| `parts.py` | teeth, gums, tongue, eyes, throat, on the game's textures via `reference.material` |
| `poses.py` | named pose controls (lid, teeth, lean, lift, ...) written as complete keys |
| `clips.py` | the clips the game plays (sleep, ambush, idle, walk, lunge, stagger, death), with the bite frames |
| `showreel.py`, `build.py` | a staged fight from the clips; the `.blend` to open, `--render` for video and sheets |
| `actors.py`, `fight.py`, `combat.py`, `hud.py` | the full choreographed fight, rendered with a Valheim-style HUD |
| `export_unity.py` | the FBX (our parts only, rig turned to face Unity's +Z) and its manifest |
| `unity_fight.py` | the fight as JSON for the Unity replay |

```
blender --background --factory-startup --python assets/crypt_mimic/build.py -- --render     showreel + sheets
blender --background --factory-startup --python assets/crypt_mimic/export_unity.py           FBX for Unity
python assets/crypt_mimic/unity_fight.py                                                     fight for Unity
(copy out/unity/* to unity/Assets/Creatures/crypt_mimic/, then)
Unity -batchmode -projectPath unity -executeMethod Workshop.CreatureBuild.Run -workshopCreature crypt_mimic
Unity -batchmode -projectPath unity -executeMethod Workshop.CreatureBundle.Run -workshopCreature crypt_mimic
      -workshopBundle ecr_cryptmimic -workshopOut out/creature_bundles
Unity -batchmode -projectPath unity -executeMethod Workshop.PreviewShots.RunFight -workshopOut out/fightshots
Unity -projectPath unity -workshopOpenPreview      opens the preview scene and plays the fight
```

- The animator controller (`CreatureAnimator`) uses the parameters the game sets: `forward_speed`, `sideway_speed`,
  `turn_speed`, the `sleeping`, `dead`, `onGround` (and similar) flags, the `stagger` trigger and one trigger per attack
  clip. Attack states carry the tag `attack` and bite frames get an `OnAttackTrigger` event.
- Facing: an armature built facing -Y faces -Z in Unity, so `export_unity.py` bakes a half turn into the rig (bone rest
  poses and parts, not the armature object); `CreaturePrefab` fails the build if the front bone isn't on +Z.
- **No root motion from Blender rigs.** Clip travel came out backwards (a turned armature: Unity leaves the parent's
  rotation out), then downwards (Unity reads the travel in the moving node's own tilted frame; Blender's exporter gives
  the armature's contents and its motion opposite forwards). Clips are exported without travel and the mod moves the
  creature itself (the mimic's `MimicLeap` follows the clip's forward curve through `Character.AddRootMotion`).
  `RootMotionCheck` plays each attack through the animator and fails the build on any backwards, sideways or vertical
  travel.
- The model and prefab keep bare materials; `PreviewMaterials` textures only the preview scene's copy, and
  `CreatureBundle` fails if anything from `Assets/Reference` would go into the bundle.
- Batch mode cannot open the project while the editor has it open: close the editor first.
- Videos: `blender/encode.py` turns a folder of frames into an MP4 with Blender's own encoder (no ffmpeg needed).

## A game creature with new gear: the Greydwarf Slinger

When the new creature is one of the game's own with something added, the game's body, rig and animator stay and
only the additions come from here. `assets/ecr_slinger/build.ps1` is the worked example (Elite Creatures Pack's
Greydwarf Slinger):

| Piece | Made by |
| --- | --- |
| `assets/ecr_slingshot`, `ecr_sling_band`, `ecr_sling_pouch`, `ecr_slinger_satchel` | ordinary `model.py` assets (Blender) |
| the shot clip `ecr_sling_shot` | `unity/Assets/Editor/Slinger/SlingClip.cs`, authored on the game's Greydwarf |
| the kit `ecr_slinger_kit` | `SlingKit.cs`: the parts under mounts named after the Greydwarf's bones |
| stills and a video | `SlingerPreview.cs`: the game's Greydwarf (reference only) wearing the kit |

```
.\assets\ecr_slinger\build.ps1 [-Preview] [-Install] [-SkipBlender]
```

- **Humanoid clips on the game's skeleton.** The Greydwarf's avatar is humanoid, so a new clip is muscle curves the
  game plays through the creature's own avatar; the mod swaps it in for one of the controller's clips with an
  `AnimatorOverrideController`. `SlingPoser` poses the real skeleton (chest turn, two-bone IK for the arms, hand
  aim), `HumanPoseHandler.GetHumanPose` reads the pose back as muscles and `SlingTrack` keys them (RootT/RootQ are
  the pose's body position and rotation). Clip settings are copied from the controller's own clip (the Greydwarf's
  keep their original orientation) and the first and last keys are the idle's first frame, so the blends in and out
  are clean. Only bones the avatar maps can move (the Greydwarf's `spine3` is not one).
- **Game scale.** The reference model prefab is not always at the game's size: the Greydwarf's `Armature.001` is 100
  there and 50 in the game prefab. `SlingerBuild.GameSizedGreydwarf` sets the game's value, so offsets measured on
  the bones carry straight into the game.
- **Kit mounts.** The kit prefab's top-level children are named after bones (`l_hand`, `r_hand`, `root`); the mod
  parents each to the bone of that name with no offset, so every part keeps the offset measured here.
- **Batch mode skins a mesh once.** Rendering several poses in one batch run shows the first pose on every still
  unless each `SkinnedMeshRenderer` has `forceMatrixRecalculationPerRender` on. When a part seems to float, put
  marker spheres on the bones: here they showed the bones were right and the mesh stale.
- The bundle keeps Standard placeholder materials; the mod puts the game creature's own material, wearing the baked
  textures, on every part (`GameMaterials.Dress`).

## Gear fitted to the body: the Rime Giant

Elite Creatures Pack's Rime Giant is the game's forest Troll (the mod scales it 1.4x and tints it) wearing plates of
rime ice, a snow crust while it sleeps, and throwing an ice boulder. The parts are built to the Troll's own body:
Unity measures the body first, Blender builds each part to the measurements, Unity hangs them on the bones.

| Piece | Made by |
| --- | --- |
| the fit `out/rimegiant/fit.json` | `unity/Assets/Editor/RimeGiant/RimeFit.cs`, `RimeCrustFit.cs`: the skin under each plate (idle), the snow line and rime points over the sleeping troll |
| `assets/ecr_rime_plate_0`..`7` | `assets/ecr_rimegiant/rime_plate.py`: a slab whose back follows the measured skin, chunky crystals on top |
| `assets/ecr_rime_crust`, `ecr_rime_crust_1`..`6` | `rime_crust.py`: a low-poly snow blanket, rime crags and icicles, one piece per bone |
| `assets/ecr_rime_boulder` | its own `model.py` |
| the kit `ecr_rimegiant_kit` | `RimeKit.cs`: plates and crust pieces under mounts named after the Troll's bones |
| stills, a video, a clearance log | `RimePreview.cs`, `RimeFrames.cs`, `RimeClearance.cs`: the game's Troll (reference only) wearing the kit |

```
.\assets\ecr_rimegiant\build.ps1 [-Preview] [-Install] [-SkipBlender] [-Measure]
```

- **Pose through the Animator, not `SampleAnimation`.** `AnimationClip.SampleAnimation` writes a humanoid clip's raw
  root rotation (the troll's idle came out facing backwards); `RimePoser` plays each clip through a throwaway
  controller so the clip's own root settings apply, as in the game.
- **The game prefab, not the model prefab.** The Troll's model prefabs differ from `Troll.prefab` (other meshes, avatar,
  armature rotation, scale 100 against 130), so `RimeReference` instantiates the game prefab's `Visual` itself, from
  `Assets/Reference/Troll` (a subfolder: its `Throw.anim` would overwrite the Greydwarf's).
- **Fit, don't guess.** A plate's back is the skin's height under a 21x21 grid over its footprint, sunk 5 cm in; the
  crust's snow is a heightfield of the sleeping troll seen from above. The rays keep to the plate's own bones' skin, so a
  hand hanging in front of a thigh is not measured as the thigh.
- **The crust rides several bones.** The fit logs how far the snowed skin moves against each bone through the sleeping
  loop; each part of the crust hangs from the bone its skin moves least against, and the hands stay bare.
- **Look.** Muted pale cyan-grey ice with painted crack lines, ridge highlights and big facets; white snow in patches
  over grey rime, like the game's own ice and snowy rocks. Parts bake at 256 px; the bundle is about 2 MB per platform.
- `RimeProbe` renders every Troll clip and the developers' unused `FrostTroll` model for reference.

## A creature the mod animates itself: the Kraken

Elite Creatures Pack's Kraken is a sea boss whose code moves every bone itself (no Animator, no clips), so the bundle
is two skinned prefabs whose bones must sit exactly where the code expects them, plus four screen ink splats.

| Piece | Made by |
| --- | --- |
| `ecp_kraken_tentacle`: straight along +Z from 0 to 8 m, suckers on -Y, bones `kt_00`..`kt_15` every 0.5 m and `kt_end` | `assets/ecp_kraken/tentacle.py` |
| `ecp_kraken_head`: face +Z, beak halves, siphon, eyes on their own mesh and material, markers | `head.py`, `head_parts.py` |
| the head's body: mantle, face and a muscular column down to y -5 on a spine the mod bends (`kh_root` > `kh_body_1..3`; the head leans on it via `kh_neck`), the roots of the six long tentacles wrapped round it | `body.py`, `column.py` |
| the skin: mottling, scars, underside, suckers, beak, barnacles, baked onto the game mesh | `paint.py`, `bake_export.py` |
| `ecp_kraken_eye_albedo`, `ecp_kraken_ink_0`..`3` (512 px, straight alpha) | `textures.py` (plain Python, numpy and Pillow) |
| prefabs, the contract check, report, bundle, stills | `unity/Assets/Editor/Kraken` |

```
.\assets\ecp_kraken\build.ps1 [-Preview] [-Install] [-SkipBlender]
```

- **JSON, not FBX.** Blender writes each piece as JSON in Unity's axes (`(x, y, z) = (-bx, bz, -by)`, triangles
  rewound for the mirror): vertices split where normals or UVs differ, up to four bone weights, the rig and its markers
  at their rest positions. `KrakenPrefab` builds the bones as plain transforms with identity rotations at exactly those
  positions, binds the mesh to them and adds the SkinnedMeshRenderers; no importer settings stand between the contract
  and the prefab. Unity computes the tangents.
- **The contract is checked.** `KrakenContract` writes out what the mod relies on independently of the Blender side and
  `KrakenCheck` fails the build when a name, parent, rest position (1 mm for `kt_*`) or identity rest rotation is off,
  the tentacle does not lie along +Z with its suckers and pale underside on -Y (vertices standing out of the skin, the
  albedo under the downward normals), the beak and `kh_mouth` are not on +Z, a renderer has no bones or bindposes or a
  culling box too small for its mesh, or the skin does not follow the bones (it curls the tentacle, opens the jaws and
  leans the neck, then bakes the mesh), or the column below y -1 moves when `kh_neck` leans 40 degrees either way.
  `KrakenBodyCheck` bends the column 25 degrees at each spine bone about X and Z: no edge may stretch more than 0.3 m,
  the lower end must ride `kh_body_3`, nothing above y -0.8 may move, and the culling box must hold the column bent
  3 x 30 degrees any way with the neck at 0 or 40 degrees either way. Every
  run writes `out/bundles/ecp_kraken/report.txt`: the transform trees, bounds, triangles, radii, textures and sizes.
- **Baked from a detail mesh.** Each piece is built twice: the game mesh, and a bake mesh with every sucker (the
  tentacle models only the big ones; the column's tentacle roots none) and the paint as a node tree on corner attributes. Cycles bakes
  selected to active, colour with the mesh's own ambient occlusion and a normal map. The jaws are baked open, so their
  insides and the throat get their own paint. The tentacle unwraps along seams in four lengths; the head smart-projects,
  with the beak, lips, lids and siphon blown up first so they get more of the atlas, and the column (always half under
  water) shrunk so it gets less.
- **The neck and the spine.** Below y -0.8 the column hangs on `kh_root` > `kh_body_1` (y -1.0) > `kh_body_2` (-2.4) >
  `kh_body_3` (-3.8), blending linearly from one to the next (whole on `kh_root` at -0.8, `kh_body_1` at -1.7,
  `kh_body_2` at -2.9, `kh_body_3` from -3.8 down), so the mod can arc it from a head lunging over a ship's rail down to
  the water outside the hull. From -0.8 up the skin blends into `kh_neck` (whole at 0.6; the pivot is at 0.8), so the
  head bends on top of it. Every vertex takes the weights of its own height (the roots' rings are tilted), at most two
  bones each. The stills show `kh_neck` at +35 and -18, the spine at 25 degrees per bone, and the lunge.
- **Preview scenes.** The game's longship and Karve come from the reference export with every mesh, material and texture
  they reference (found by GUID in the export's .meta files, copied with their GUIDs into `Assets/Reference/KrakenShip`).
  The game's float box is centred on a ship's origin (`Ship.FixedUpdate`), so the stills float it there: longship deck
  floor 0.64 m and rail 1.33 m above the water, Karve 0.01 m and 0.73 m. The head is shown in the attack pose (base 0.6 m
  under water, raised 2.5 m) and merely surfaced. Tentacles are laid along curves bone by bone in world space, as the mod
  does. The ship prefabs sit at y 51 in the export: put them back at the origin.
- **Gotcha:** the ship's LODGroup lists the hull in LOD0 and LOD1; hiding every LOD1 renderer hides the hull.
- Look: murky crimson-brown to dark purple, darker blotches and cell lines, pale healed scars and sucker rings, a pale
  cream-pink underside, pale sucker rims round dark pits, a near-black chitin beak with an amber base, pale amber eyes
  with a black slit, barnacles on the mantle and the column. Head 10.6k triangles plus 0.8k for the eyes, tentacle 2.8k;
  about 3.6 MB per platform.
- History: the first version (2026-09-28) had a 12.8 m tentacle and eight short arm stubs round the crown; in game the stubs
  looked bad under the attacking head, so they gave way to the column, and the tentacles were halved.

## Reference export

`.\rip-reference.ps1` exports the game's assets with AssetRipper into `%USERPROFILE%\ValheimReference`, a Unity
project outside the repository. Use it to look up real sizes, how the game's prefabs are built, and the game's
shader property names (the shaders are exported as dummies that keep their properties). Scripts are copied as the
game's DLLs, not decompiled. Never commit or ship anything from it: a mod uses the game's assets at runtime, by name.

**A whole prefab in a Blender preview:** `workshop.prefab.load("GameElements/Ships/VikingShip.prefab")` builds what the
prefab draws under one empty at the origin, with an empty per GameObject on the way and a mesh object per renderer on
the game's textures (through `reference.mesh` and `reference.material`, plus the game's normals, one material per
submesh, tiling, tint and cutout). Transforms are Unity's in the workshop's axes, so the prefab's forward is -Y.
Skinned meshes are posed by the prefab's bones and kept static, Unity's built-in cube is drawn, and ropes
(LineRenderers, through their `LineAttach` points) become curves. Inactive GameObjects, disabled renderers, lower
LODs, the water mask, shadow blobs, particles and lights are left out (`include_inactive=True` and
`skip=lambda name, layer: ...` change that). Everything is tagged `reference.TAG`. GUIDs are looked up in
`out/reference_guids.json`, built from the export's .meta files on first use (about 3 s; a load then takes 0.2 s).
`blender/prefab_test.py` loads and renders the longship into `out/prefab_test/`: the hull is 21.5 m by 5.5 m, bow on
-Y, keel 0.71 m below the origin, deck floor 0.64 m and rail 1.33 m above it. A mesh whose vertex channels carry flag
bits (the Ashlands longship's hull) does not decode in `reference.mesh` yet; it is left out with a message.

## What works well

Hard-surface things: props, furniture, containers, building pieces, weapons and tools, and simple creatures whose
motion can be keyed from poses (a chest that bites, crawlers, floating things). Organic sculpting and natural walking
gaits for bipeds and quadrupeds do not.

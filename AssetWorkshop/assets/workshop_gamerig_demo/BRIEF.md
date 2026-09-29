# Brief: `workshop_gamerig_demo` (the Mossback)

The workshop's proof of route (b) of `codex/rigs/README.md`: a completely new creature body, modelled in Blender on an
exact copy of a game skeleton, so the game's own avatar, animator controller and clips play it unchanged. Not a mod
creature yet: no mod code, not seen in game.

## 1. What it is and where it lives

- Name: `workshop_gamerig_demo` (a mod would rename it with its prefix, e.g. `ecp_mossback`).
- What it is: a hulking forest ogre (hunched mossy back, head sunk under a heavy brow, tusks, gorilla forearms, mitten
  hands, short heavy legs) that fights, walks and staggers with the game Skeleton's own animations.
- Mod: none (workshop demo); Elite Creatures Pack would take it.
- Biome: Black Forest.
- Tier: 1 (Black Forest).
- Who uses it: a creature, spawned as a copy of the game's `Skeleton` wearing this body (`BundlePrefabs.CreatureBody`).

## 2. Category

- Category key: `creature.humanoid`.
- Label and sample count: bipeds of the player's size, 1.7 to 3 m; 13 distinct models (45 prefabs).
- Triangles (min, p25, median, p75, max): 806, 1,514, 2,910, 4,077, 6,708.
- Texture px: 128, 128, 256, 256, 512.
- Texel density (px per metre): 39.4, 54.3, 59.1, 73.1, 85.7.
- Longest side (m): height 1.91, 1.99, 2.17, 2.46, 2.87.

## 3. Game references

| Prefab (path under the reference export) | What to take from it |
| --- | --- |
| `Characters/Skeleton/Skeleton.prefab` | the skeleton itself (53 bones, T-pose, 1.99 m), its controller and clips, its sockets, the creature the mod copies |
| `Characters/Troll/Troll.prefab` | the exaggeration of a big brute: shoulders wide, big hands, short legs; fur painted as soft strokes |
| `Characters/GreyDwarf/Greydwarf.prefab` | Black Forest palette: warm browns and moss greens painted, not modelled |
| `Characters/Goblin/Goblin.prefab` | skin painted as smooth gradients with muscle forms shaded in, little noise |
| `Characters/Wolf/Wolf.prefab` | a greyscale body tinted per kind by `_Color` and the star levels |

## 4. Silhouette

- A stooped lump of a brute with a mossy hump rising behind a small forward-thrust head, arms hanging to the knees
  with fat forearms and fists; reads as "ogre" at 10 m, never as a skeleton or a troll.

## 5. Parts

- Origin and orientation: the creature's root on the ground between the feet, metres, Z up, facing -Y (Unity +Z).
  Every mass is placed from the Skeleton's rest bone positions (`gamerig.head`), so the body wears that skeleton.

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| body (metaball skin, decimated) | 2.14 x 1.95 x 0.60 (T-pose span, height, depth) | 67 % of the atlas | skin | skin | one closed mesh: hump, belly, head with sockets and brow, mitten hands |
| moss on hump and shoulders | faces of the body facing up behind the neck | 4 % | veg.moss | fur | paint only |
| wrist and shin wraps | faces of the body | part of leather | leather | leather | paint only |
| tusks | 0.12 long | 2 | bone.teeth | bone | rigid on `Jaw` |
| eyes | 0.019 radius | 2 | flat pale yellow | glow | rigid on `Head` |
| claws, toe claws | 0.03-0.05 | 16 | bone.teeth | bone | rigid on the last finger bone / `ToeBase` |
| belt and bone buckle | 0.45 across | 2 | leather, bone | leather, bone | rigid on `Hips` |
| loincloth front and back | 0.28 x 0.36, ragged hem | 6 % | leather.hide | cloth | hips at the top, both thighs share the hem |

## 6. Budget

- Triangles (target): 3,000 to 4,000 (built: 3,808, between the category's median and p75).
- `TEXTURE_SIZE`: 256.
- Texel density (target px per metre): 60 to 80 (built: 78).
- `NORMAL_MAP` / `AO_STRENGTH`: on / 0.5.

## 7. Materials

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| skin.skin | body | `paint.skin(tones=#50564a/#6b715f/#838972, top=0.12, contrast=1.8, blotch_m=0.3)` | grey-green, low saturation | painted grey-green so `_Color` and the star hue shift colour it (Wolf-style) |
| veg.moss | hump, shoulders | `paint.moss()` | the family's greens | |
| leather.leather | belt, wraps | `paint.leather()` | | |
| leather.hide | loincloth | `paint.hide()` | | |
| bone.teeth | tusks, claws, buckle | `paint.teeth(tint=#ddd2b0)` | ivory | |
| glow | eyes | `materials.flat((0.95, 0.78, 0.25))` | pale yellow | the game's Creature shader has no emission here: painted light |

Dressed in the mod as the game Skeleton's own material (`Custom/Creature`) wearing these baked textures
(`GameMaterials.Dress`, then `Plain(gloss 0.2)`), smoothness 0.18.

## 8. Paint regions

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| skin | the body | the kind's skin colour |
| fur | moss | moss, snow or ash on the back |
| leather | belt, wraps | |
| cloth | loincloth | |
| bone | tusks, claws, buckle | |
| glow | eyes | eye colour |

- Variants (`variants.json`, `out/variants.png`): `frost: skin=tint:#76828e, fur=tint:#b9c2c8, cloth=tint:#5d656e`;
  `swamp: skin=tint:#5e5c3f, fur=tint:#3d4a26, cloth=tint:#3a3629`; `ash: skin=tint:#524b46, fur=tint:#5a3024,
  glow=tint:#ff7a2a`.

## 9. Rig and animation

- Route and source prefab: (b), `Characters/Skeleton/Skeleton.prefab` (the game prefab, not a model prefab): its 53 bones
  (the player's names and order, the Skeleton's own lengths and T-pose), armature `Visual/_skeleton_base/Armature` at
  scale 100 under a 0.95, turned -90 degrees about X, body renderer `Visual/_skeleton_base/Skeleton`, root bone `Hips`.
- Bones and sockets: the Skeleton's own `RightHand_Attach`, `LeftHand_Attach`, `Helmet_attach`; new `Mouth` (under
  `Jaw`, the bite and breath origin), `LeftEye` and `RightEye` (under `Head`, CharacterAnimEvent's eye names),
  `fx_back` (under `Spine2`, on the hump).
- Clips: the Skeleton's controller unchanged: `Skeleton Rise` (wakeup), `Idle`, `Shield-Walk-Injured` (1 m/s),
  `Shield-Run-Forward` (3 m/s), `Standing Melee Attack Horizontal` (attack), `Bow Aim Idle 01` and `Bow Aim Recoil`
  (attack_bow), `Skeleton_mace`, `Skeleton Fire Attack`, `stagger2`; every player clip retargets too.
- Events: the game's clips' own (`Hit`, trails); nothing added.
- Animator controller: reused, `Characters/Skeleton/model/Skeleton_animator.controller`.

## 10. Effects

| When | Reuse (game prefab) or new |
| --- | --- |
| hit | `vfx_foresttroll_hit` |
| death | `vfx_troll_death` (the Skeleton's bone burst does not fit a fleshy body; no ragdoll made yet) |
| footstep | `vfx_troll_footstep` |

## 11. Sounds

| When | Reuse |
| --- | --- |
| idle, alerted | `sfx_troll_idle`, `sfx_troll_alerted` (pitched up by the mod) |
| attack, hit, death | `sfx_troll_attacking`, `sfx_troll_hit`, `sfx_troll_death` |
| footstep | `sfx_troll_footstep` |

## 12. Into the mod

- Bundle name and the mod folder: `workshop_gamerig_demo.windows` / `.linux` (412 / 540 KB) in
  `out/bundles/workshop_gamerig_demo`; a mod copies them into its `assets/bundles`.
- Game prefab to copy and build on: `Skeleton` (`PrefabBench.Copy`), then `CreatureBody.Wear(copy, bundle prefab)` and
  `CreatureBody.HideOthers(copy, body)` (the Skeleton's eye meshes).
- Game material to dress into: the Skeleton body's own (`Custom/Creature`), through `CreatureBody.Wear`.
- Textures: albedo and normal in the prefab's placeholder material; `workshop_gamerig_demo_regions` in the bundle for
  recolouring by region.
- Colliders and physics: the Skeleton's (capsule 0.4 x 1.85 m); the body stays inside it but for the hump and hands.
- Icon, recipe, config: none (a creature).
- Multiplayer: nothing new: the body is part of the registered prefab on every peer; the game's ZSyncAnimation already
  syncs the Skeleton's parameters.

## 13. Checks

- [x] Blender build (`gamerig_run.py`); `out/preview.png` looked at, all four views.
- [x] Style check (`codex/tools/stylecheck.py assets/workshop_gamerig_demo`, which reads the contract's manifest keys):
      triangles, texture, texel density, size, the albedo's value, saturation and span, and every skin line PASS.
      Outside: the blotch size of the small regions (leather, bone, cloth, fur: many small islands, measured as one
      wide blotch) and the bone's value and saturation (the tusks and claws are small and take the ambient occlusion
      of the face and fists).
- [x] Lineup against the game's Skeleton: the Unity stills show both side by side in every state (`out/unity/sheet_pair.png`).
- [x] Paint regions and the variants sheet (`out/variants.png`): skin, fur, leather, cloth, bone, glow recolour.
- [x] Unity: contract, game prefab, bone order, sockets, skin, placement, facing, playback (skeleton 0.000 mm from
      the game's, no tearing, feet within 1 cm of the game body's), bundle (report.txt).
- [ ] In the game through DevBridge: not done.

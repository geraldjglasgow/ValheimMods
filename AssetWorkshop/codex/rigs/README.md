# Rigs

The game's creature skeletons, avatars, animator controllers and clips, and how to build a rig that the game's creature
code (and, where wanted, the game's own clips) drives. Measured on 2026-09-29 by `measure/rigs.py` into
`data/rigs.json`: **63 skeletons** among the 184 creature prefabs of `models/creatures.md`, their **85 animator
controllers** and the **1,219 clips** those play (785 humanoid, 434 generic). Game code quoted by class and member name
from `assembly_valheim.dll` (decompiled into the scratch folder only).

| Page | Rig family | Rigs | Bones |
| --- | --- | --- | --- |
| `player.md` | the player's skeleton, shared by the Skeleton, FallenWarrior and ShadowPerson | 1 | 53 |
| `humanoid.md` | creature bipeds on humanoid avatars: the game's humanoid clips retarget onto them | 25 | 21–126 (median 53) |
| `quadruped.md` | legged animals and insects on generic avatars | 14 | 23–66 (median 33) |
| `flyer.md` | bats, insects, drakes, birds, the Valkyries, Moder | 10 | 6–70 |
| `serpent.md` | bone chains: sea serpents, leeches, root tentacles | 3 | 8–20 |
| `other.md` | rigs of their own: blobs, ghosts, the broom, the late bosses | 10 | 2–123 |

Names below are exact: the game finds most things by name, and a wrong case is a missing bone.

## What the game's code expects

A creature is driven through its `Animator` by parameters, reads back **state tags**, and is called back by **animation
events**. None of this depends on the skeleton; all of it must be right for a new creature to fight, walk and die.

### Hierarchy

- A child of the root named exactly **`Visual`**: `Character.Awake` does `transform.Find("Visual")` (no null check),
  `VisEquipment` too, and effects with `m_multiplyParentVisualScale` read its scale. The `Animator`,
  `CharacterAnimEvent`, `LODGroup` and `LevelEffects` sit on it; `LevelEffects` scales it per star.
- The `Animator` is found with `GetComponentInChildren<Animator>()` on the root: exactly one Animator under the
  creature, or the first one wins.
- **A bone named exactly `Head`** (case sensitive): `Utils.GetBoneTransform(animator, HumanBodyBones.Head)` is
  `Utils.FindChild(visual, "Head")`, a search by name, not by the avatar. It is `Character.m_head`
  (`GetHeadPoint`, where chat bubbles and the Talker speak from) and the head `CharacterAnimEvent` turns towards the
  target. 29 of the 63 rigs have none (the Greydwarf's and Goblin's are `head`, the Surtling's `mixamorig:Head`,
  Yagluth's `Bone.007`), so those never turn their heads to the player. Name the head bone `Head`.
- An **`EyePos`** child of the root (`Character.m_eye`), at the eyes.
- Foot transforms listed in `FootStep.m_feet` (any names; the game's: `LeftFoot`/`RightFoot`, `l_foot`/`r_foot`,
  `L Foot`/`L Hand` for four-legged ones) and, for foot IK, in `CharacterAnimEvent.m_feets`.
- Attack origins by name: an attack item's `m_attackOriginJoint` is found with `FindChild` under Visual: the game uses
  `Jaw`, `Hips`, `head`, `l_hand`, `LeftFoot`, `RightFoot`, `L_ShamanHand`, `BeamRoot` (Yagluth), `PukePoint` (the
  Seeker Queen). Projectiles spawn at that point plus the attack's height, range and offset.

### Parameters the code sets

Set through `ZSyncAnimation` (so other players see them) or straight on the Animator. A controller need only declare
the ones it uses; a parameter the code sets that the controller lacks is ignored.

| Parameter | Type | Set by | Meaning |
| --- | --- | --- | --- |
| `forward_speed` | float | `Character` every physics step | velocity along the creature's forward, m/s (walking, swimming, flying) |
| `sideway_speed` | float | `Character` | velocity to the right, m/s |
| `turn_speed` | float | `Character` | turn velocity, smoothed over 0.5 s (blend trees use ±0.5 to ±2) |
| `inWater`, `onGround`, `flying`, `encumbered`, `falling`, `slipping`, `skating`, `skategliding` | bool | `Character` | locomotion states |
| `tilt` | float | `Character` | the Visual's forward against up (flying and climbing lean) |
| `dead` | bool | `Character.OnDeath` | death |
| `stagger`, `jump` | trigger | `Character` | stagger when hit hard; jump |
| `fly_takeoff`, `fly_land` | trigger | `Character.TakeOff`, `Land` | flyers change mode |
| `alert` | bool | `BaseAI` | has seen a target |
| `sleeping` | bool | `MonsterAI` | sleeping creatures (`m_sleeping`) |
| `consume` | trigger | `MonsterAI` | eats an item from the ground (tameable animals) |
| `statef`, `statei` | float, int | `Humanoid` | the held item's `m_animationState` |
| `blocking` | bool | `Humanoid` | blocking |
| `interact`, `eat`, `equip_hip` | trigger | `Humanoid` | player-side actions |
| **attack triggers** | trigger | `Attack.Start` | the attack item's `m_attackAnimation`; with `m_attackChainLevels` > 1 the trigger is `name` + chain index (`attack_claw0`, `attack_claw1`), with `m_attackRandomAnimations` > 1 `name` + a random index (`base_attack0..3`, the Boar) |
| `attack_abort`, `detach`, `attach` | trigger | `Attack` | an attack cancelled; items that fly off and return |
| `idle` | int or float | `RandomAnimation` (`m_values` `idle`, 5 values every 3 s), `RandomFlyingBird` | which idle clip a 1D blend plays |
| `footstep` | float | **read** by `FootStep` | a curve in the walk and run clips, see Footsteps |

Of the 85 controllers, 77 declare `forward_speed` and `inWater`, 75 `sideway_speed`, `turn_speed` and `footstep`,
74 `jump`, 68 `dead`, 61 `stagger`. The attack triggers the game's creatures use are listed per creature in
`data/rigs.json` (`vocabulary.attack_triggers`); the common ones are `attack` (55 creatures), `attack_bite` (18),
`attack_bow`, `attack_cast`, `attack1..3`, `taunt`.

**Multiplayer:** bools, floats and ints reach other players only if they are named in the root's `ZSyncAnimation`
lists (`m_syncBools`, `m_syncFloats`, `m_syncInts`; the Greydwarf syncs `alert`, `inWater`, `forward_speed`,
`sideway_speed`, `turn_speed`). Triggers always travel as RPCs. A mod that sets a new bool or float must add it there.

### State tags

`Character` and `Humanoid` read the **tag** of the current and next state on layer 0 (`GetCurrentAnimatorStateInfo(0)
.tagHash`); a state's name does not matter. Tags in the game's controllers (count of controllers):

| Tag | Controllers | What the code does with it |
| --- | --- | --- |
| `idle` | 75 | nothing; the locomotion state carries it by convention |
| `attack` | 74 | `Humanoid.InAttack()`: the AI waits for the attack to end, movement slows by the attack's `m_speedFactor`, no head look |
| `stagger` | 59 | `IsStaggering()`: cannot move or attack |
| `freeze` | 43 | `CanMove()` false: spawning, waking, sleeping, eating, taunts |
| `sitting` | 1 (player) | `CanMove()` false, `IsSitting()` |
| `dodge`, `crouch`, `minoraction`, `minoraction_fast`, `emote`, `cutscene`, `knockeddown` | player only | player actions |

Every attack state must carry `attack` and every stagger state `stagger`, or the creature slides and attacks again
while its clip plays.

### Animation events

Functions on components of the Visual (`CharacterAnimEvent`, `AnimationEffect`) that clips call. How many of the 1,219
clips call each:

| Event | Clips | Component | Does |
| --- | --- | --- | --- |
| `Hit` | 237 | CharacterAnimEvent | `OnAttackTrigger`: the attack deals its damage or fires its projectile now |
| `Effect` | 202 | AnimationEffect | spawns the event's GameObject at the child named by its string (`Hip`, `R.Hand`, `RightHand_Attach`) or at the Visual |
| `TrailOn` / `TrailOff` | 84 / 63 | CharacterAnimEvent | weapon trails on and off (VisEquipment) |
| `Speed` | 62 | CharacterAnimEvent | sets `animator.speed` to the float (0.2–2.0; 41 of the events put it back to 1.0) to slow a wind-up or hasten a follow-through |
| `OnAttackTrigger` | 56 | CharacterAnimEvent | the same as `Hit` |
| `Stop` | 31 | CharacterAnimEvent | `OnStopMoving`: the creature stops sliding forward mid-attack |
| `Chain` | 21 | CharacterAnimEvent | from here the next attack of a chain may start |
| `FootStep` | 16 | CharacterAnimEvent | a footstep at the foot named by the string (`l.foot`, `Frontfoot.l`, `r.finger`) |
| `Attach` | 15 | AnimationEffect | parents the GameObject to the child named by the string (`head`, `r.hand`, `front_claws.L`) until the state ends |
| `Die` | 5 | CharacterAnimEvent | `OnDeath`: for creatures that play a death clip before their death effects |
| `RemoveAttachments`, `HideObject`/`ShowObject`, `Drop`, `Land`, `Jump`, `DodgeMortal`, `GPower` | 1–3 | various | rare |

Attack timing over the creatures' 389 attack states (the player's weapon attacks left out; `attacks` in the data):
a clip plays **1.7–3.5 s (median 2.5 s)** after its state speed; the **first `Hit` lands at 37–55 % of the clip
(median 43 %), 0.8–1.6 s in (median 1.1 s)**; attacks blend in from Any State over **0.2–0.25 s** and leave at an exit
time of 0.75–0.9 with a 0.25 s blend back to locomotion. The Greydwarf's `Zombie Attack` (1.97 s at speed 1.2) hits
at 0.90 s; the Wolf's bites (1.2 s) at 0.42 and 0.49 s; the Boar's (0.97–1.27 s) at 0.28–0.50 s; the Troll's log
swings (0.8 s clips at speed 0.25, 3.2 s played) at 1.26 and 1.48 s. Small fast animals hit early (25–40 %), big and
heavy ones late (50–65 %). 57 attack states have trails, all of them weapon swings.

### Footsteps

`FootStep` plays a step when the Animator's **`footstep` float changes sign** while `forward_speed` or `sideway_speed`
is above 0.2 m/s and 0.2 s have passed since the last; the step's position is the listed foot furthest forward. So a
walk or run clip carries a **`footstep` curve** that changes sign at each foot plant (positive while one foot is down,
negative for the other; the game's values are small, ±0.01 to ±0.06, only the sign counts). 333 of the game's clips
carry one (the export names it `typetree_0x995590F9`, its CRC32); 443 of the 540 moving clips in the game's locomotion
blend trees carry a curve or `FootStep` events (`vocabulary.footsteps`), and 14 controllers have none, so their
walking gives no steps: the four Greydwarf kinds, the Goblin, the Surtling, the Bat, Gjall, the Ghost, the Wraith,
Yagluth, the Jotun witch, the chickens. Controllers need a float parameter `footstep`. Clips with `FootStep` events (16)
name the foot instead. Footless creatures (blobs) set `m_footlessFootsteps` and step every `m_footlessTriggerDistance`
metres. The step effect is chosen from `FootStep.m_effects` by motion (walk, run, sneak, swim, land) and ground
(default, water, mud, snow, wood ...); the Greydwarf lists ten.

### Root motion, head look, foot IK

- `CharacterAnimEvent.OnAnimatorMove` adds `animator.deltaPosition` to the character (`Character.AddRootMotion`) on
  the owner, **whatever `applyRootMotion` says**: a clip that travels moves the creature (lunges, pounces, the Troll's
  jump). 63 of 172 creature Animators have `m_ApplyRootMotion` on (Charred, Jotnar, Moose, Bjorn, bosses), the rest
  off; both move.
  Humanoid clips carry their travel in `RootT`; generic ones need the avatar's root bone (`Canine AvatarAvatar` roots
  at `CG`, the Moose at `DEF-spine.001`). The workshop's own rigs export clips without travel and move the creature in
  code (workshop README, the crypt mimic): root motion from Blender came out backwards and downwards.
- **Head look** (`m_headRotation`, `m_lookWeight` 0.5, `m_headLookWeight` 1, `m_bodyLookWeight` 0.1, `m_lookClamp`
  0.5 on the Greydwarf) is humanoid IK (`SetLookAtPosition`): it needs a humanoid avatar and a bone named `Head`.
  It is off in attacks and while the creature cannot move.
- **Foot IK** (`m_footIK`, on 36 creatures: the player, Troll, Charred, Jotnar, Fenring, bosses) plants the listed feet
  on the ground through the humanoid avatar (`m_footDownMax` 0.2–0.4 m, `m_footOffset` 0.1–0.12 m). Generic rigs get
  none.

### Attach points and gear

- `VisEquipment` (on the root) names the transforms held items hang from: `m_rightHand`, `m_leftHand`, `m_helmet`,
  and on the player `m_backShield`, `m_backMelee`, `m_backTwohandedMelee`, `m_backBow`, `m_backTool`, `m_backAtgeir`.
  The game's creatures point them at sockets (`RightHand_Attach` 25, `LeftHand_Attach` 23, `Helmet_attach` 19,
  `HelmetAttach`, `RightAttach`, `LeftAttach` on the Draugr, `Attach.r`/`Attach.l` on the Charred) or straight at
  bones (`RightHand` on the Troll, `r.hand` on the Dverger, `Weapon.r` on the Goblin brute, `Wrist.r` on the Stone
  Golem, `head`/`Head` for helmets).
- An item prefab's children decide how it goes on (`VisEquipment.AttachItem`, `AttachArmor`):
  - `attach`: parented to the slot's transform at **zero position and rotation** (keeping its own scale), so the item
    is modelled in the socket's frame. An `equipoffset` child adds an offset.
  - `attach_skin`: **skinned to the body**. Its SkinnedMeshRenderers get `rootBone = body.rootBone` and
    `bones = body.bones`, the body renderer's bone array **as it is**: the gear mesh's bone indices must be in the
    same order as the body mesh's bones (`bone_order` in the data; the player's starts `Hips, Spine, Spine1, Spine2,
    Neck, Head, Jaw, LeftShoulder ...`). The Goblin's helmet, armband, loincloth, shoulders and leg band, the Dverger's
    suits and hair, the Charred's breastplate and helmet are all `attach_skin`.
  - `attach_<name>`: parented to the transform named `<name>` anywhere under Visual (the Dverger's staffs are
    `attach_r.hand`), zero offset.
  - `attach_back`: the back slots.
- `AnimationEffect.Attach` (event) and an attack's `m_attackOriginJoint` also find transforms by name.

## Three routes to a new creature

**(a) A game creature with new parts or clips.** Clone the game's prefab at run time (`BundlePrefabs`), hang new parts
under the bones by name, and swap clips with an `AnimatorOverrideController`. The body, rig, avatar and controller stay
the game's. The workshop has done this three times: the Greydwarf Slinger (kit under `l_hand`, `r_hand`, `root`; a
humanoid clip authored on the Greydwarf in Unity with `HumanPoseHandler`), the Rime Giant (plates fitted to the Troll's
skin, parts under the bone each moves least against) and the Skeleton Crossbowman (five clips authored on the
Skeleton's avatar). Use it when the creature is recognisably a game creature.

**(b) A new body on a game skeleton.** Model a new mesh and skin it to a copy of a game skeleton: the same bone names,
hierarchy, rest local transforms and armature scale (`bones` with `rest_m`, `armature_scale` in the data), so the
game's avatar, controller and clips play it unchanged, and `attach_skin` gear made for that skeleton fits it. The
bone order of the new body mesh must equal the game body's (`bone_order`) when it is to wear the game's gear.
Best skeletons to build on: the player's (53 bones, 30 finger bones, every player clip), the Greydwarf's (33, crouched
long-armed biped), the Troll's (57, big biped with face bones), the Wolf's or Boar's (34, quadruped with tail), the
Lox's (31, big quadruped with a mount point), the Bat's (13), the Serpent's (20).

The workshop builds this route (workshop README, "A new body on a game skeleton"; the Mossback in
`assets/workshop_gamerig_demo` is the worked example). `blender/workshop/gamerig.py` builds the armature from the game
prefab itself: every bone, socket and end with the game's names, parents, rest positions and axes, cross-checked
against `rigs.json`. The body goes out as JSON in Unity's axes, with the game's local transforms copied as numbers and
the game body's bone order. Unity rebuilds the skeleton from those numbers and checks it transform by transform
against the game prefab as Unity imports it (exact), then plays our skeleton and the game creature through the game's
own controller side by side (identical to 0.001 mm), measuring stretch and feet. The mod swaps the body renderer's
mesh with `BundlePrefabs.CreatureBody.Wear`. Build on the prefab the mod will copy, not on its rig's sample: the
Skeleton has the player's 53 names and bone order but its own lengths, standing in a T-pose (1.99 m, spine from
1.14 m, hips 28 cm wide) where the player stands in an A-pose (spine from 1.02 m, hips 20 cm).

**(c) A rig of its own.** For shapes no game skeleton fits.
- **A biped on a humanoid avatar.** Any skeleton Unity can map to its humanoid (Hips, Spine, Head, both upper arms,
  lower arms, hands, upper legs, lower legs, feet required; Chest, UpperChest, Neck, shoulders, toes, fingers, Jaw,
  eyes optional). Every humanoid clip in the game then retargets onto it: the player's 429, the Greydwarf's, the
  Troll's, the Draugr's (`humanoid.md` lists them). The game's creature bipeds are built this way with their own
  names (`l_arm1`, `Upperarm.l`, `L.Uarm`) and map 19–55 bones.
- **Generic** (quadrupeds, flyers, serpents, blobs): clips keyed on bone paths, authored for this rig. The crypt mimic
  is the workshop's worked example (`assets/crypt_mimic`: rig, poses, clips in Blender, controller `CreatureAnimator`
  built against the parameters above).
- **No Animator**, bones moved in code (the Kraken), for things that are not creatures in the game's sense.

## The workshop's rig conventions

For a rig of our own (route c), and for new parts on any rig. They keep rigs flexible (gear, variants, recolours) and
make the game's name-based lookups find what they need.

**Axes and scale.** Model in metres, Z up, front towards −Y (the workshop's convention; Unity's +Z after the half turn
`export_unity.py` bakes in). The armature object at scale 1 in Blender. When reusing a game skeleton, copy its armature
transform exactly: 100 for most, 50 for the Greydwarf, 130 for the Troll, 200 for the chickens, 300 for Moder, 320 for
the Frozen King, 30 for the Neck; the Wolf, Boar and Deer (Malbers `CG` rigs) have 1.

**Bind pose.** Bipeds standing straight, arms out and palms down, fingers straight: the player's arms hang about 60
degrees below level (an A-pose), the Draugr's 45, the Troll's 28, the Greydwarf's are level (a T-pose); any of these
maps to a humanoid avatar, but a rig built on a game skeleton keeps that skeleton's pose exactly. Quadrupeds stand
square on the ground, head forward; wings spread flat; tails and chains lie straight back along −Y. Every bone's local
axes follow the parent's where possible (roll 0), so humanoid mapping and IK are predictable.

**Bone names.**
- Bipeds: the player's names (`Hips`, `Spine`, `Spine1`, `Spine2`, `Neck`, `Head`, `Jaw`, `LeftShoulder`, `LeftArm`,
  `LeftForeArm`, `LeftHand`, `LeftHandIndex1..3`, `LeftHandMiddle1..3`, `LeftHandRing1..3`, `LeftHandPinky1..3`,
  `LeftHandThumb1..3`, `LeftUpLeg`, `LeftLeg`, `LeftFoot`, `LeftToeBase` and the `Right...` mirror). Leave out the
  fingers a creature does not have; keep the names of those it has.
- Quadrupeds: `Hips`, `Spine`, `Spine1`, `Spine2`, `Neck`, `Neck1`, `Head`, `Jaw`, `Tail1..n`; hind legs `LeftUpLeg`,
  `LeftLeg`, `LeftFoot`, `LeftToeBase`; front legs `LeftFrontUpLeg`, `LeftFrontLeg`, `LeftFootFront` (and `Right...`).
  `LeftFoot`, `RightFoot`, `LeftFootFront`, `RightFootFront` are the names `CharacterAnimEvent.FindJoints` knows.
- Always one bone named exactly `Head`, with `Hips` at the pelvis (not at the ground like the Greydwarf's `root`).
- Extra chains: `<Part>1..n` counting away from the body (`Tail1`, `LeftWing1`, `RightTentacle1`), left and right as
  words (`Left`, `Right`) as the player's bones have them (Blender flips these names when mirroring).

**Sockets.** Zero-weight bones (or plain transforms) for everything the code or a mod hangs on the creature, named
after the game's:
- `RightHand_Attach`, `LeftHand_Attach` under the hands, inside the closed fist (the player's sit 9.5 cm below the hand
  bone on the palm side and 4.2 cm further along the arm, in the T-pose); items are modelled in this frame.
- `Helmet_attach` under `Head`, at the crown (the player's: 10.6 cm up, 7 cm forward of the head bone).
- `BackShield_attach`, `BackOneHanded_attach`, `BackTwohanded_attach`, `BackBow_attach`, `BackAtgeir_attach` under
  `Spine1`, `BackTool_attach` under `Hips`, for anything carried on the back.
- `Mouth` under `Head` or `Jaw` (breath, spit and bite origins, `m_attackOriginJoint`), `LeftEye`/`RightEye` (the
  names `FindJoints` uses for `CharacterAnimEvent.m_eyes`), and `fx_<name>` for effect anchors (`AnimationEffect`
  strings).
- `Mount` / `attachpoint` on the back of a rideable animal (the Lox's `attachpoint` is 3.44 m up).
Sockets are cheap: the game's player has ten. A socket per part a mod may swap makes every piece of gear a separate
prefab that attaches by name.

**Parts and materials.**
- One body mesh on the whole skeleton; every removable, swappable or star-level part a separate mesh skinned to the same
  armature (so `attach_skin` can put it on and take it off), or rigid under one socket (`attach`).
- The body on **material 0 of the main renderer** (the one `LevelEffects.m_mainRender` names), painted greyscale where
  it should take the star-level and kind tints; gear on its own material so it keeps its colour.
- Recolourable regions marked as the workshop's regions (`recolour.md`): skin, fur, cloth, metal, bone, glow.

**Weights.** Up to four bones a vertex (Unity's skin quality default), weights summing to 1, smooth over two to three
edge loops at bends (elbows, knees, the spine), rigid on hard parts (horns, teeth, armour plates, weapons: one bone,
weight 1). Sockets and `_end` leaf bones carry no weight. Keep deform bones to what moves: the game's player-sized
bipeds use 21–63, animals 23–66.

**Budget of bones.** Body 20–35; add fingers only where they will be seen to move (15 or 30); a face only for a jaw
(1) and eyes (2). The game's largest (GoblinBruteBros 126, the Frozen King phase 3 123) are several creatures in one
prefab.

## Checklist for a rig

- Route chosen (a, b or c) and the reference skeleton or avatar named.
- `Visual` child with Animator (culling mode "cull update transforms", as 157 of the game's 184), CharacterAnimEvent,
  LODGroup, LevelEffects; `EyePos`; a bone named `Head`.
- Controller declares `forward_speed`, `sideway_speed`, `turn_speed`, `inWater`, `dead`, `footstep`, the attack
  triggers of the creature's items, `stagger`; attack states tagged `attack`, stagger `stagger`, spawn and sleep
  `freeze`, locomotion `idle`.
- Locomotion blend tree on (`turn_speed`, `forward_speed`) with idle at 0, the walk clip at the creature's `m_speed`
  and the run clip at its `m_runSpeed` (Greydwarf 2 and 5, Wolf 1.5 and 7, Boar 1–3 and 8, Troll 3 and 6).
- Walk and run clips with a `footstep` curve; attack clips with `Hit` at about 40–55 %, `Stop` before it for lunges,
  `TrailOn`/`TrailOff` round a weapon swing.
- `ZSyncAnimation` lists every bool and float the controller reads.
- Sockets named after the game's; gear skinned in the body's bone order.

# The player's rig

`Characters/Player/Player.prefab`: the skeleton, body meshes, attach points and animator every player uses, and that
the game's Skeletons, `FallenWarrior` and `ShadowPerson` share bone for bone. Armour texture layout (the `_ChestTex`,
`_LegsTex` regions the Player shader composites) is covered by the armour pages; this page is the skeleton, the body
and where things attach. Numbers from `data/rigs.json` (`rig.player`) and `data/creatures.json` (`Player`).

## The prefab

```
Player                    CapsuleCollider r 0.49 m, h 1.85 m; Rigidbody; PlayerController; Player; ZNetView;
                          ZSyncTransform; ZSyncAnimation; Talker; VisEquipment; Skills; FootStep (42 step effects)
  Visual                  Animator (player_maleAvatar, Player_animator), CharacterAnimEvent, LODGroup (one level,
                          culled at 4.2 % of screen height), AnimationEffect
    Armature              scale 100, rotated -90 degrees about X (a Blender FBX)
      Hips ...            53 deform bones, 10 sockets, 7 MagicaCloth colliders, the breath particles on Head
    body                  SkinnedMeshRenderer: body.asset (male) or bodyfem.asset, swapped by VisEquipment
  EyePos
```

- **Body meshes** (`VisEquipment.m_models`): `Characters/Player/model/body.asset` 1,514 triangles, 1,010 vertices, two
  submeshes (body and hair material), and `bodyfem.asset` 1,644 triangles; both skinned to the same 53 bones. They sit
  on `PlayerMaterial.mat` / `PlayerMaterial_Fem.mat` (the `Player` shader): `_MainTex` the 256 px skin
  (`PlayerCharacter_01.png`, only its top-left quarter painted: skin tones [144, 93, 70] to [195, 143, 114], a warm
  tan), `_SkinBumpMap`, and `_ChestTex`/`_LegsTex` with their bump and metal maps, which equipped armour replaces.
  1.91 m tall in the bind pose.
- **Hair and beards** are items (`attach_skin`), not part of the body.
- **Cloth**: `ClothCollider` capsules (MagicaCloth, a third-party library) on Hips, both upper and lower legs, Spine1,
  Spine2 and both arms, which capes and skirts collide with.

## Skeleton

53 deform bones under `Armature` (scale 100). Rest positions in the prefab, metres, Unity axes (x right... the left arm
is at negative x, y up, z forward); sockets marked:

```
Hips  0.91
  LeftUpLeg 1.02 > LeftLeg 0.57 > LeftFoot 0.12 > LeftToeBase 0.00        (x -0.10)
  RightUpLeg 1.02 > RightLeg 0.57 > RightFoot 0.12 > RightToeBase 0.00    (x +0.10)
  BackTool_attach (socket) 1.09
  Spine 1.02
    Spine1 1.31
      BackShield_attach, BackOneHanded_attach, BackTwohanded_attach, BackBow_attach, BackAtgeir_attach (sockets)
      Spine2 1.45
        Neck 1.61 > Head 1.67 > Jaw 1.71, Helmet_attach (socket) 1.78
        LeftShoulder 1.59 > LeftArm 1.54 (x -0.22) > LeftForeArm 1.27 (x -0.38) > LeftHand 1.05 (x -0.52)
          LeftHandThumb1..3, LeftHandIndex1..3, LeftHandMiddle1..3, LeftHandRing1..3, LeftHandPinky1..3
          LeftHand_Attach (socket) 0.95
        RightShoulder ... the mirror
```

The arms hang in an **A-pose, about 60 degrees below level** (upper arm from 1.54 m at 0.22 m out down to the wrist at
1.05 m, 0.52 m out), not a T-pose. Each finger has three bones plus an `_end` leaf; the toes one plus an end.

**Bone order** of the body renderer (the order an `attach_skin` item's bone weights must follow, since
`VisEquipment` hands the item `body.bones` as it is): `Hips, Spine, Spine1, Spine2, Neck, Head, Jaw, LeftShoulder,
LeftArm, LeftForeArm, LeftHand, LeftHandThumb1..3, LeftHandIndex1..3, LeftHandMiddle1..3, LeftHandRing1..3,
LeftHandPinky1..3, RightShoulder, RightArm, RightForeArm, RightHand, RightHandThumb1..3, RightHandIndex1..3,
RightHandMiddle1..3, RightHandRing1..3, RightHandPinky1..3, LeftUpLeg, LeftLeg, LeftFoot, LeftToeBase, RightUpLeg,
RightLeg, RightFoot, RightToeBase` (`bone_order` in the data). The Skeleton and FallenWarrior bodies use exactly this
order, so any player armour's `attach_skin` fits them; the Charred (56, a `Root` first), Jotnar (59, face bones after
`Jaw`) and Fenring (61) use player-like names in other orders and do not take player armour.

**Avatar** `player_maleAvatar` maps all 53: Hips `Hips`, Spine `Spine`, Chest `Spine1`, UpperChest `Spine2`, Neck,
Head, Jaw, both shoulders, `LeftArm`/`LeftForeArm`/`LeftHand` as upper arm, lower arm, hand, `LeftUpLeg`/`LeftLeg`/
`LeftFoot`/`LeftToeBase`, and 30 finger bones. Twist 0.5 on arms, forearms, thighs and legs, stretch 0.05, feet spacing
0. The Skeleton's `_skeleton_baseAvatar` maps the same bones the same way.

**Proportions** (`body` in the data): height 1.91 m; hips 0.91 m (0.48); neck 1.61 m; head bone 1.67 m (0.88);
shoulders 0.43 m apart (0.23); arm from shoulder to wrist 0.58 m (0.30); leg from hip to ankle 0.90 m (0.47); palm
0.11 m. Capsule 0.49 m radius, 1.85 m high.

## Attach points

`VisEquipment` slots and their sockets (offset from the parent bone in metres, in the rest pose):

| Slot | Socket | Parent | Offset (x, y, z) |
| --- | --- | --- | --- |
| `m_rightHand` | `RightHand_Attach` | RightHand | 0.042, −0.095, 0.000 |
| `m_leftHand` | `LeftHand_Attach` | LeftHand | −0.042, −0.095, 0.000 |
| `m_helmet` | `Helmet_attach` | Head | 0.004, 0.106, 0.070 |
| `m_backShield` | `BackShield_attach` | Spine1 | −0.016, −0.003, −0.258 |
| `m_backMelee` | `BackOneHanded_attach` | Spine1 | 0.225, 0.409, −0.219 |
| `m_backTwohandedMelee` | `BackTwohanded_attach` | Spine1 | 0.203, 0.360, −0.194 |
| `m_backBow` | `BackBow_attach` | Spine1 | −0.049, −0.135, −0.135 |
| `m_backAtgeir` | `BackAtgeir_attach` | Spine1 | 0.065, −0.450, −0.196 |
| `m_backTool` | `BackTool_attach` | Hips | 0.227, 0.178, 0.007 |

An item's `attach` child lands on the socket at zero position and rotation: build held items with the grip at the
origin in the socket's frame (the skeleton arsenal's weapons were measured against the game's in that frame, workshop
README). The hand sockets sit inside the closed fist, 9.5 cm below the hand bone on the palm side.

## Animator

`Characters/Player/animation/Player_animator.controller`: **150 parameters, two layers, 434 clips** (the most of any
controller; every humanoid creature can borrow them).

- **Base Layer** (149 states): locomotion is a 2D blend on (`sideway_speed`, `forward_speed`) whose 16 points (0, ±1,
  ±4, ±7 m/s sideways; −7 to 10.5 m/s forwards) are each a 1D blend on `statef`, the held item's animation state, so
  every weapon has its own walk and run; blocking, bow aim, encumbered, fishing, crossbow and staff states are their own
  2D blends. 61 attack states, 28 emotes, 8 sitting states (chairs, beds, ships, mounts).
- **upperbody** (23 states, weight 1, override, no avatar mask in the export): equipping, interacting, eating, hammer
  and hoe, reloads, staff loops, the shovel.
- **Attack triggers** are the weapon items' `m_attackAnimation` with a chain index: `swing_axe0..2`,
  `swing_longsword0..2`, `atgeir_attack0..2`, `battleaxe_attack0..2`, `knife_stab0..2`, `dual_knives0..2`,
  `dualaxes0..3`, `greatsword0..2`, `staff_fireball0..1`, `unarmed_attack0..1`, `shovel0..2`, plus `*_secondary`
  (`axe_secondary`, `sword_secondary`, `mace_secondary`, `atgeir_secondary`, `battleaxe_secondary`,
  `knife_secondary`, `greatsword_secondary`, `dualaxes_secondary`, `shovel_secondary`), `bow_fire`, `crossbow_fire`,
  `spear_throw`, `spear_poke`, `throw_bomb`, `swing_pickaxe`, `swing_sledge`, `swing_hammer`, `swing_hoe`,
  `scything`, `staff_*`.
- Tags used by the player code: `attack`, `dodge`, `crouch`, `minoraction`, `minoraction_fast`, `emote`, `sitting`,
  `cutscene`, `knockeddown`, `stagger`, `freeze`, `idle`.
- `ZSyncAnimation` syncs 24 bools (`inWater`, `falling`, `onGround`, `blocking`, `crouching`, `encumbered`,
  `equipping`, the `attach_*` seats, `bow_aim`, `reload_crossbow` ...) and `forward_speed`, `sideway_speed`, `statef`.
- `CharacterAnimEvent`: foot IK on (`m_footDownMax` 0.2 m, `m_footOffset` 0.12 m, `m_footStepHeight` 0.4 m, feet
  `LeftFoot` and `RightFoot`), head look (weights 0.5 look, 1 head, 0.1 body, clamp 0.5), `m_femaleHack` with the
  shoulders' offsets.

## Using the player's rig for a creature

The Skeleton does: its own 4,077-triangle mesh skinned to the player's 53 bones in the player's order, its own avatar
with the same mapping, its own controller (`Skeleton/model/Skeleton_animator.controller`) whose sword swing is the
player-style `Standing Melee Attack Horizontal` and whose bow states are the player's `Bow Aim Recoil` and `Bow Aim
Idle 01`. Route (b) of `README.md` on this skeleton gives a new humanoid every one of the player's 434 clips, the
player's sockets, foot IK, and player armour through `attach_skin`. The skeleton arsenal and the Skeleton Crossbowman
already play player attacks on the game's Skeleton this way (workshop README).

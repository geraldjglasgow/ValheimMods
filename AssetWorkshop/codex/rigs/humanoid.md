# Humanoid creature rigs

The game's creature bipeds: each has a skeleton of its own naming, mapped to Unity's humanoid avatar, so humanoid clips
(the player's among them) play on it through muscle space. 25 rigs, measured in `data/rigs.json` (`rig.humanoid`); the
player's skeleton is on `player.md`. How these fit the code's contract is on `README.md`.

## The rigs

Height in the rest pose (`body.height_m`), bones deformed, the armature's scale, how many bones the avatar maps
(of which fingers):

| Rig (prefab) | Creatures | Bones | Armature | Avatar maps | Height m | `Head`? |
| --- | --- | --- | --- | --- | --- | --- |
| greydwarf (`GreyDwarf/Greydwarf.prefab`) | Greydwarf, Greyling, Greydwarf_Elite, _Shaman, _Frozen | 33 | 50 | 31 (12) | 1.94 | `head` |
| troll (`Troll/Troll.prefab`) | Troll, TrollFrost, Troll_Summoned | 57 | 130 | 46 (24) | 7.47 | yes |
| draugr (`Draugr/Draugr.prefab`) | Draugr, _Elite, _Ranged, TrainingDummy | 21 | 100 | 19 (0) | 2.15 | yes |
| goblin (`Goblin/Goblin.prefab`) | Goblin, GoblinArcher, GoblinDeepNorth, Goblin_Gem | 53 | 100 | 49 (28) | 1.39 | `head` |
| goblinshaman (`GoblinShaman/GoblinShaman.prefab`) | GoblinShaman and Hildir's | 53 | 100 | 50 (27) | 1.43 | yes |
| goblinbrute (`GoblinBrute/GoblinBrute.prefab`) | GoblinBrute | 73 | 100 | 55 (30) | 3.93 | yes |
| goblinbrutebros | GoblinBruteBros (two brutes, one rig) | 126 | 100 | 55 (30) | 4.47 | yes |
| dverger (`Dverger/Dverger.prefab`) | seven Dvergr kinds | 63 | 100 | 55 (30) | 1.55 | `head` |
| hildir (`Hildir/Hildir.prefab`) | Hildir (the Dverger's layout, hair and mouth bones for its hat and toe bones) | 64 | 100 | 55 (30) | 1.57 | `head` |
| elaking (`Elaking/Elaking.prefab`) | Elaking, ElakingLantern, ElakingMole | 54 | 100 | 50 (30) | 1.49 | `head` |
| fenring (`Fenring/Fenring.prefab`) | Fenring, Fenring_Cultist, Ulv (on a generic avatar) | 61 | 100 | 53 (30) | 3.44 | yes |
| charred_melee (`TheCharred/Charred_Melee.prefab`) | eight Charred kinds | 56 | 100 | 53 (30) | 2.63 | yes |
| surtling (`Surtling/Surtling.prefab`) | Surtling, Frysling (`mixamorig:*` names) | 22 | 0.7 | 22 (0) | 1.33 | no |
| jotunwarrior (`Jotnar/JotunWarrior.prefab`) | JotunWarrior, DualWield, JotunWitch | 59 | 140 | 55 (30) | 3.61 | yes |
| stonegolem (`StoneGolem/StoneGolem.prefab`) | StoneGolem | 22 | 100 | 21 (0) | 4.53 | yes |
| neck (`Neck/Neck.prefab`) | Neck | 41 | 30 | 37 (18) | 1.18 | yes |
| morgen (`Morgen/Morgen.prefab`) | Morgen | 56 | 100 | 48 (28) | 10.46 | no |
| wraith (`Wraith/Wraith.prefab`) | Wraith | 50 | 100 | 48 (28) | 2.46 | `head` |
| barka (`Barka/Barka.prefab`) | Barka | 23 | 100 | 20 (0) | 12.63 | `head` |
| haldor, bogwitch, odin | the traders and Odin | 69, 50, 51 | 100 | 54, 49, 49 | 1.66, 4.74, 2.20 | yes, no, yes |
| gd_king, goblinking, bonemass | the Elder, Yagluth, Bonemass | 51, 55, 48 | 100 | 49, 53, 46 | 12.8, 7.9, 7.7 | no |

26 of the 28 humanoid avatars keep Unity's defaults: twist 0.5 on upper arm, forearm, upper leg and leg, arm and leg
stretch 0.05, feet spacing 0. The Stone Golem (arm stretch 0.45, twists 0.34–0.58) and Barka (arm stretch 0.78, arm
twist 0.1) were tuned for their long arms.

## Naming

Each studio artist named bones their own way; the avatar makes them interchangeable. The game's mappings for the
bones the humanoid needs:

| Unity | Player | Greydwarf | Troll | Draugr | Goblin | Dverger | Goblin brute | Haldor |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Hips | Hips | root | Root | Hips | Root | hip | Hip | Hip |
| Spine | Spine | spine1 | Spine0 | Spine0 | spine1 | spine1 | Spine1 | Spine0 |
| Chest | Spine1 | spine2 | Spine1 | Spine1 | spine2 | spine2 | Spine2 | Spine1 |
| UpperChest | Spine2 | spine3 | Spine2 | Spine2 | spine3 | spine3 | Spine3 | Spine2 |
| Neck | Neck | – | – | – | neck | neck | Neck | – |
| Head | Head | head | Head | Head | head | head | Head | Head |
| UpperArm | LeftArm | l_arm1 | LeftArm | LeftArm | l_upperarm | l.upperarm | Upperarm.l | Upperarm.l |
| LowerArm | LeftForeArm | l_arm2 | LeftForeArm | LeftForeArm | l_lowerarm | l.lowerarm | Lowerarm.l | Underarm.l |
| Hand | LeftHand | l_hand | LeftHand | LeftHand | l_hand | l.hand | Hand.l | Wrist.l |
| UpperLeg | LeftUpLeg | l_leg1 | LeftUpLeg | LeftUpLeg | l_leg0 | l.upperleg | uppoerleg.l | UpperLeg.l |
| LowerLeg | LeftLeg | l_leg2 | LeftLeg | LeftLeg | l_leg1 | l.lowerleg | lowerleg.l | LowerLeg.l |
| Foot | LeftFoot | l_foot | LeftFoot | LeftFoot | l_foot | l.foot | foot.l | Foot.l |
| Toes | LeftToeBase | – | – | – | – | l.toe | footfront.l | Foot2.l |
| Jaw | Jaw | – | Jaw | – | jaw | jaw | Jaw | Jaw |

The Greydwarf, Troll, Goblin, Draugr, Elder, Odin and Bonemass map Hips to a bone at the ground (`root`, `Root`, `Hips`)
and hang both hips and the spine from it; the others put Hips at the pelvis. The Elaking maps Hips to `spine1` and has
no UpperChest; the Morgen maps Head to its `Jaw`. The workshop names new bipeds as the player does (`README.md`,
conventions).

## Skeletons

Rest heights in metres in the game prefab (`rest_m`); the full lists with x and z are in the data.

**Greydwarf** (`Armature.001` at scale 50; 33 bones; saved in a crouch, knees forward):

```
root 0.00
  l_hip 0.88 > l_leg1 0.92 > l_leg2 0.56 > l_foot 0.07          (feet 0.33 m out, three toes painted)
  r_hip ...
  spine1 0.88 > spine2 1.21 > spine3 1.54
    head 1.61
    l_shoulder 1.61 > l_arm1 1.56 > l_arm2 1.50 > l_hand 1.47   (hand 1.14 m out: arms nearly level, very long)
      l_index1 > l_index2, l_middle1 > l_middle2, l_pinky1 > l_pinky2
    r_shoulder ...
```

A second armature `Armature` (no deform bones) sits beside it. No neck, no jaw, no thumb.

**Draugr** (`Armature` at 100; 21 bones, the smallest biped skeleton):

```
Hips -0.02
  LeftHip 1.09 > LeftUpLeg 1.07 > LeftLeg 0.55 > LeftFoot 0.01
  RightHip ...
  Spine0 1.09 > Spine1 1.33 > Spine2 1.54
    Head 1.72 > HelmetAttach (socket) 2.08
    LeftShoulder 1.72 > LeftArm 1.69 > LeftForeArm 1.46 > LeftHand 1.26 > LeftAttach (socket)
    RightShoulder ... > RightAttach (socket)
```

No fingers (mitten hands), no neck or jaw: a whole humanoid on 21 bones, which is enough for every humanoid clip.

**Troll** (`Armature` at 130; 57 bones; the only creature with a face rig):

```
Root 0.00
  LeftHip 2.64 > LeftUpLeg 2.97 > LeftLeg 1.50 > LeftFoot 0.17
  Spine0 2.64 > Spine1 4.11 > Spine2 5.18
    Head 5.95 > Jaw, Nose, LeftEye, RightEye, L.Brow, R.Brow, L.Cheek, R.Cheek, L.Ear, R.Ear, L.Ulip, R.Ulip
    LeftShoulder 5.95 > LeftArm 5.22 > LeftForeArm 4.53 > LeftHand 3.77
      LeftHandIndex1..3, LeftHandMiddle1..3, LeftHandRing1..3, LeftHandThumb1..3
```

**Goblin** (53): the player's layout in lower case (`Root`, `l_hip`, `l_leg0`, `l_leg1`, `l_foot`, `spine1..3`,
`neck`, `head`, `jaw`, `l_shoulder`, `l_upperarm`, `l_lowerarm`, `l_hand`, five three-bone fingers) with
`LeftHand_Attach` and `RightHand_Attach` sockets 6–7 cm into the fist.

## Controllers

Every creature biped's controller is the same template on one layer: a default state that checks spawning or sleep
(tag `freeze`), a **Movement** blend tree (tag `idle`), **In Water** and **Jump** states borrowed from the player's old
clips (`Treading Water` and `Swimming` are in 49 controllers, `Jump` in 34), **attack** states entered from Any State
on the item triggers (tag `attack`), a **Stagger** state (tag `stagger`) entered from Any State on `stagger`, and
**Wakeup** or **Sleeping** states (tag `freeze`). The game's:

- **Greydwarf** (`GreyDwarf/animation/greydwarf_animator.controller`): `wakeup?` (freeze) → `Wakeup` (`Spawn`, 0.93
  exit) or Movement; Movement 2D freeform cartesian on (`turn_speed`, `forward_speed`): `Idle` at 0, `Dwarf Walk` at 2,
  `Running` at 5 m/s; `attack` (`Zombie Attack`, speed 1.2), `throw` (`Throw`), `Stagger` (`stagger2`, speed 0.8, 1 s
  blend out), `George Vibing` (an idle dance, freeze).
- **Troll** (`Troll/misc/troll_animator.controller`): Movement with `Walk` at 3, `Walk Angry` at 6 (time scale 0.6),
  `Turn L`/`Turn R` at turn ±0.5 (time scale 2) and a 1D idle blend on `idle` (`Idle_Breathing`,
  `Idle_Smelling_Something`, `SlaveSquat`); attacks `attack`, `punch`, `throw`, `swing_logv`, `swing_logh` (the log
  swings play 0.8 s clips at speed 0.25, 3.2 s), `jump`; `Sleeping`, `Wakeup`, `Summon`.
- **Skeleton** and **Draugr** (`Skeleton/model/Skeleton_animator.controller`,
  `Draugr/model/Draugr_animator.controller`): Movement 2D freeform directional on (`sideway_speed`, `forward_speed`):
  `Idle`, `Shield-Walk-Injured` (Skeleton) or `Sad Walk` (Draugr) at 0.75–1 m/s and at ±0.75 sideways,
  `Shield-Run-Forward` or `Standing Run Forward` at 2.5–5; `attack_axe` (`Standing Melee Attack Horizontal`, speed 1.2,
  trail), `bow_idle` then `attack_bow` (`Bow Aim Idle 01`, `Bow Aim Recoil`), the Skeleton's `attack_mace` and
  `attack_fire`; sleep and rise states.
- **Goblin**: Movement with `Dwarf Walk` at 1 and 2 (time scale 1.6), `Run With Sword` at 5; `RandomAnimation` drives
  its `idle` (5 values every 3 s).

**Locomotion rule:** the walk clip sits at the creature's `Character.m_speed` and the run clip at `m_runSpeed` (m/s):
Greydwarf 2 and 5 (`m_runSpeed` 6), Troll 3 and 6, Goblin 2 and 5. Clip time scales 0.6–2.0 stretch a clip to the
speed; a new walk cycle is authored so its feet do not slide at that speed.

**Attack timing** on the reference bipeds (clip seconds after state speed; first hit):

| Controller / state | Clip | Plays | First hit | Notes |
| --- | --- | --- | --- | --- |
| greydwarf `attack` | Zombie Attack 1.97 s ×1.2 | 1.64 s | 0.90 s (55 %) | `Stop` at 0.65 s, `Hit` at 1.07 s of the clip |
| greydwarf `throw` | Throw 2.2 s | 2.2 s | 0.85 s (39 %) | the stone leaves |
| troll `attack` | Zombie Attack 2.13 s ×0.7 | 3.05 s | 1.28 s (42 %) | blend in 0.35 s |
| troll `punch` | Zombie Attack2 1.6 s | 1.6 s | 1.05 s (66 %) | |
| troll `swing log V` | Mace-Attack-R2 0.8 s ×0.25 | 3.2 s | 1.48 s (46 %) | trail |
| skeleton `attack_axe` | Standing Melee Attack Horizontal 1.9 s ×1.2 | 1.58 s | 0.79 s (50 %) | trail |
| skeleton `attack_mace` | Skeleton_mace 1.33 s | 1.33 s | 0.86 s (64 %) | trail |
| goblin `swing_longsword` | Sword And Shield Slash 1.5 s | 1.5 s | 0.64 s (43 %) | |
| jotun `Attack Cleave` | 2.67 s | 2.67 s | 0.85 s (32 %) | blend in 0.1 s |

**Shared clips.** Humanoid clips move between rigs freely: `stagger2` staggers 23 controllers (Draugr, Dvergr, Elaking,
Troll, Tick ...), `Zombie Attack` serves the Greydwarf and the Troll, `Throw` the Greydwarf, Troll and Neck, the
Charred's four controllers share 25 clips among all four, and many carry Mixamo names (`Zombie Attack`, `Mutant
Jumping`, `Sad Walk`, `Standing Melee Attack Horizontal`, `Sword And Shield Slash`, `Breathing Idle`). A new humanoid
creature can start from these and add clips of its own.

## Gear on creature bipeds

- **Held items** hang from the `VisEquipment` slots (`README.md`): `RightHand_Attach`/`LeftHand_Attach` (Goblin, Player
  rig), `RightAttach`/`LeftAttach`/`HelmetAttach` (Draugr), `Attach.r`/`Attach.l` (Charred), `r.hand`/`l.hand`/`head`
  (Dverger), `Weapon.r`/`Weapon.l`/`Head` (Goblin brute), `RightHand`/`LeftHand` (Troll), `Wrist.r`/`Wrist.l`/`Head`
  (Stone Golem). The Greydwarf has no VisEquipment: its "weapons" are invisible attack items.
- **Random loadouts:** the Draugr draws `m_randomWeapon` (`draugr_axe` three times in four, `draugr_bow` once) and
  `m_randomShield` (`ShieldWood`, `ShieldBanded`, none, none); the Dverger mage one of three `m_randomSets` (Fire, Ice,
  Healer: suit, hair and staff); the Charred archer `Charred_Helmet` and `Charred_Breastplate` from `m_randomItems` at
  50 % each.
- **Skinned gear** (`attach_skin`) follows the body's bone order: the Goblin's helmet, armband, loincloth, shoulders and
  leg band; the Dverger's `DvergerSuitArbalest`, `DvergerSuitFire`, `DvergerSuitIce`, `DvergerSuitSupport` and four hair
  styles; the Charred's breastplate, helmet, hip cloth and mage cloths; the Goblin brute's hip cloth and shoulder guard.
  This is the game's own modular-costume system and the workshop's model for recolourable, swappable gear: body and
  gear share one skeleton and one bone order, each piece is an item prefab, a kind is a set of items.

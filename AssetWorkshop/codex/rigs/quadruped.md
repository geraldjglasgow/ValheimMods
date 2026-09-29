# Quadruped rigs

Legged animals and insects: 14 rigs on **generic avatars** (clips keyed on bone paths, so a clip plays only on the
skeleton it was made for). Measured in `data/rigs.json` (`rig.quadruped`); the contract with the game's code is on
`README.md`, the models on `models/creatures.md`.

## The rigs

| Rig (prefab) | Creatures | Bones | Armature, root | Avatar root bone | Feet `FootStep` lists | Attack triggers |
| --- | --- | --- | --- | --- | --- | --- |
| wolf (`Wolf/Wolf.prefab`) | Wolf, Wolf_cub, spirit wolf | 34 | `CG` at 1 | `CG` | `L Foot`, `R Foot`, `L Hand`, `R Hand` | `attack1..3` |
| boar (`Boar/Boar.prefab`) | Boar, piglet, spirit boar | 34 | `CG` at 1 | `CG` | the same | `base_attack0..3` |
| deer (`Deer/Deer.prefab`) | Deer, Deer_White | 33 | `CG` at 1 | `CG` | the same | none |
| lox (`Lox/Lox.prefab`) | Lox, Lox_Calf | 31 | `Armature` 100 | – | `Frontfoot.l/.r`, `Backfoot.l/.r` | `attack_bite`, `attack_stomp` |
| moose (`moose/Moose.prefab`) | Moose, calf, spirit moose | 66 | `Moose Rig 2` 100 | `DEF-spine.001` | none | `attack_horn`, `attack_hooves`, `attack_horn_sweep` |
| bjorn (`Bjorn/Bjorn.prefab`) | Bjorn, Unbjorn | 36 | `Armature` 100 | `CoG` | `Back_Foot.L/.R`, `Front_Foot.L/.R` | `attack_bite`, `attack_claws`, `attack_slam`, `attack_swipe_l/_r/_combo` |
| asksvin (`Asksvin/Asksvin.prefab`) | Asksvin, hatchling | 32 | `Armature` 100 | `Hip` | `L.BackFoot`, `R.BackFoot`, `L.FrontFoot`, `R.FrontFoot` | `attack_bite`, `attack_headbutt`, `attack_pounce` |
| seal (`seal/Seal.prefab`) | Seal, Seal_Pup | 23 | `Armature` 100 | `Chest` | four flipper tips | none |
| hare (`Hare/Hare.prefab`) | Hare | 30 | `Armature` 100 | no avatar | `BackFoot.l`, `BackFoot.r` | none |
| chicken (`Chicken/Hen.prefab`) | Chicken, Hen | 30 | `Armature` 200 | `CG` (the Boar's avatar) | none | none |
| tick (`Tick/Tick.prefab`) | Tick | 30 | `Armature` 100 | no avatar | none | `attack` |
| seeker (`Seeker/Seeker.prefab`) | Seeker, SeekerBrood | 44 | `Armature` 100 | – | `l.frontarm3`, `r.frontarm3` | `attack_pincers`, `attack_slam`, `attack_claw_left/_right`, `attack_takeoff`, `attack_land`, `attack_flyingslam` |
| seekerbrute (`SeekerBrute/SeekerBrute.prefab`) | Seeker Soldier | 39 | `Armature` 100 | `root` | four feet | `attack_bite`, `attack_ram`, `attack_slam`, `taunt` |
| eikthyr (`Eikthyr/Eikthyr.prefab`) | Eikthyr | 30 | `Armature` 100 | `Root2` | `L_Foot`, `R_Foot`, `L_Hand`, `R_Hand` | `attack1`, `attack2`, `attack_stomp` |

The Ulv walks on all fours on the **Fenring's biped skeleton** with a generic avatar (`UlvAvatar`) and its own clips,
feet `LeftHand`, `RightHand`, `LeftFoot`, `RightFoot`: one skeleton, two body plans.

## Three generations of naming

- **The Malbers rigs** (Wolf, Boar, Deer): an asset-store animal pack (`3rd party/Malbers Animations/Animals Packs` in
  the export; 53 of its clips are in the game, 19 in the Wolf's controller, 20 in the Boar's, 14 in the Deer's). Names
  with spaces, a centre-of-gravity root, front legs as arms:

  ```
  CG (armature, scale 1)
    Pelvis 0.89
      Spine 0.93
        L Thigh 0.89 > L Calf 0.49 > L HorseLink 0.38 > L Foot 0.14          (hind leg, three joints below the hip)
        Spine1 0.95 > Spine2 0.98 > Neck 1.03 > Head 1.12
            Jaw > Tongue, Eye L, Eye R, L Ear, R Ear, L Cheek, R Cheek
          L Clavicle 0.93 > L UpperArm 0.94 > L Forearm 0.50 > L Hand 0.14    (front leg)
        Tail 0.92 > Tail1 > Tail2 > Tail3                                     (1.4 m behind: z -0.77 to -1.42)
  ```

  The Wolf is 2.4 m from nose to tail tip with its back at 0.95 m; the Boar and Deer add a toe (`L Toe0`, `L Finger0`).
- **The house rigs** (Lox, Bjorn, Asksvin, Hare, Seal, Tick, Chicken, Seekers): `Armature` at 100, names with `.l`/`.r`
  or `L.`/`R.` (`Frontupperleg.l`, `Back_UpperLeg.L`, `L.BackUpperLeg`, `BackUpperLeg.l`), a `Root` at the ground and
  a `Hip`. The Lox:

  ```
  Root -0.09
    Hip 1.81
      Mount 1.84 > Mount.001 3.15 > attachpoint (socket) 3.44                 (the saddle)
      spine1 1.84
        Shoulder.l 2.27 > Frontupperleg.l 2.15 > Frontlowerleg.l 0.58 > Frontfoot.l 0.16
        Spine2 2.27 > Neck 2.29 > Head 1.62 > Jaw > Lip.l, Lip.r; Toplip.l, Toplip.r, Waddle; Hair
    Hip.l 1.81 > Backupperleg.l 1.56 > Backlowerleg.l 0.60 > Backfoot.l 0.12
  ```

  Legs 2.8 m apart, 5.6 m nose to tail; the head hangs low in front (1.62 m) under a hump at the shoulders.
- **The Rigify rigs** (Moose): Blender's Rigify deform bones exported as they are, `DEF-` prefixed (`DEF-spine.001`
  to `.006`, `DEF-thigh.L`, `DEF-thigh.L.001`, `DEF-f_hoof.L`, `DEF-ear.L.001`, `DEF-jaw`, `DEF-tail.001`), 66 bones,
  two segments per long bone. The Bjorn and the Fader use Rigify-style centre bones (`Root`, `CoG`). The workshop's own
  quadrupeds use the names in `README.md` (the player's style, `Left`/`Right` words).

## Controllers

One layer, the same template for all: **Movement** (tag `idle`) a 2D freeform cartesian blend on (`turn_speed`,
`forward_speed`) with a **1D idle blend on `idle`** at its centre, walk and run clips forwards and turn clips at the
sides; **attack** states from Any State on the item triggers (tag `attack`); **Stagger** (tag `stagger`); **Consume**
(tag `freeze`) on the `consume` trigger for animals that eat (tameables); **In Water** a 2D blend of a swim idle and
swim; **Jump**. The Malbers controllers keep an `Idle` sub-state machine of five idles that no transition reaches.

| Controller | Idle blend (on `idle`) | Walk (m/s) | Run (m/s) | Turn (at `turn_speed`) |
| --- | --- | --- | --- | --- |
| wolf | `C_Idle01..04`, `C_Hawl` at 0–9 | `C_Walk` 1.5 | `C_Run` 7, `C_Run_Left/Right` at ±2 | `C_Walk Left/Right` ±2 |
| boar | `BIdle`, `BIdle SmallShake`, `BIdle Big Shake`, `BIdle Look`, `BDrink` | `BWalk` 1 (×1.5), 3 (×3) | `BRun` 8, `BRunL/R` ±4 | `BTurn L/R` ±2 (×2.2) |
| deer | `DIdle 1`, `DEat`, `DIdle Look`, `DIdle Head Shake`, `DIdle Scratch` | `DWalk` 1.5 | `DRun` 7 (×1.2) | `DTurn L/R` ±2 |
| lox | `Idle`, `Graze_ruminate`, `Shake off the Flies`, `Shoutout` | `Walk` 3 (×1.5) | `Canter` 6, 8.75 (×1.4) | `TurnLeft/Right` ±0.5, ±1 |
| asksvin | – | `Walk` 3 | `Canter` 7, `Run` 9 | ±0.5, ±1, and strafing walks at ±1 |

Speeds match the creatures' `Character` settings: Wolf `m_speed` 1.5, `m_runSpeed` 8; Boar 1.5 and 8; Deer 1.5 and 7;
Lox 4 and 7; Asksvin 7 and 9. Nothing in the game code sets `idle` on these animals (only `RandomAnimation`, which the
Goblin, Elaking and Eikthyr carry, and the birds' `RandomFlyingBird`): without it the Wolf plays only `C_Idle01`. A new
animal that wants idle variety carries a `RandomAnimation` with `idle`, 5 values, 3 s.

**Attack timing** (clip at state speed; first hit):

| State | Clip | Plays | First hit |
| --- | --- | --- | --- |
| wolf Attack1, Attack2 | C_Attack_Bite_Left, _Right | 1.2 s | 0.42 s, 0.49 s (35–41 %) |
| wolf Attack3 | C_Attack_Claws | 1.03 s | 0.50 s (49 %) |
| boar bite, fangs 1, fangs 2, fangs front | BAttack Bite, Fangs 1, 2, Front | 0.97–1.27 s | 0.28–0.50 s (25–42 %) |
| lox bite | Attack ×1.25 | 1.6 s | 0.58 s (36 %) |
| lox stomp | Stomp | 2.17 s | 1.37 s (63 %) |
| asksvin bite, headbutt, pounce | Attack Bite, Headbutt, Pounce | 1.67, 2.0, 3.0 s | 0.78, 1.04, 1.61 s (47–54 %) |
| seeker pincers | Attack Pincers ×1.3 | 1.03 s | 0.71 s (69 %) |
| seeker pierce left/right | Attack Pierce Left/Right | 1.5 s | 0.70 s (47 %) |

Small animals bite early and short (Boar, Wolf: about a second, the hit at a third); big ones wind up (the Lox's stomp
hits at 63 %). Stagger states leave at an exit time of 1.0–2.0 with a 0.5–1 s blend back to Movement.

## Footsteps and feet

All four feet are listed in `FootStep.m_feet` (the step plays at the one furthest forward), and walk and run clips carry
the `footstep` curve; the Lox and Asksvin list two step effects, the Wolf one. The Seeker lists only its front arms,
the Moose none (it steps from `FootStep` events or not at all). Four-legged creatures have no foot IK.

## Building a new quadruped

- **Route (b) on the Wolf or Boar skeleton** gives a new animal every Malbers clip of that animal (walk, trot, run,
  turns, idles, bites, swim, stagger, eat) with no animation work; keep the `CG` root at scale 1, the bone names with
  their spaces and the rest pose (back at 0.95 m on the Wolf, 0.8 m on the Boar), and scale the prefab, not the bones.
- **Route (c)**: a rig of our own (`README.md` names), clips authored in Blender for this rig (idle, walk, run, turn
  left and right, two or three attacks, stagger, eat, swim), exported without root travel; the controller built on the
  template above with the parameters the creature's items fire.
- Put a `Mount` chain and an `attachpoint` socket on the back of anything rideable (the Lox's saddle hangs there), a
  `Mouth` socket for bites and breath, `LeftEye`/`RightEye`.

# Flyer rigs

Winged and hovering creatures: bats, insects, drakes, birds of prey, the floating Gjall, the ambient birds, Odin's
ravens, the Valkyrie, the Fallen Valkyrie and Moder. Ten rigs, measured in `data/rigs.json` (`rig.flyer`). All are
generic avatars except Moder's. The code's contract is on `README.md`.

## How the game flies a creature

- `Character.m_flying` makes a creature a flyer: it moves at `m_flySlowSpeed` / `m_flyFastSpeed` (Bat 5 and 10 m/s,
  Hatchling 4 and 12, Seeker 12), and `MonsterAI` takes off and lands by `m_randomFly`, `m_chanceToTakeoff`,
  `m_chanceToLand`, `m_airDuration`, `m_groundDuration`, keeping `m_flyAltitudeMin`–`m_flyAltitudeMax` above ground.
- The code sets the bool **`flying`** and the triggers **`fly_takeoff`**, **`fly_land`** (on the Animator), and
  `forward_speed`, `turn_speed` as on the ground; **`tilt`** is the Visual's forward against up (pitch) for banking and
  diving. Clip events `TakeOff` and `Land` switch the character's mode from inside a clip.
- A flyer that never lands (Bat, Deathsquito, Gjall) needs only one locomotion state. One that lands (Hatchling,
  Dragon, Seeker, Volture) has two: **`Movement_Flying`** and **`Movement_Ground`** (both tag `idle`), switched by
  `flying`, with `Takeoff` and `Land` states in between.

## The rigs

| Rig (prefab) | Creatures | Bones | Avatar | Shape | Controller |
| --- | --- | --- | --- | --- | --- |
| bat (`Bat/Bat.prefab`) | Bat, Bat_Swamp | 13 | none | root, hip, spine, head, jaw; each wing an arm of shoulder, upper arm, lower arm, hand (0.67 m span between hands) | `bat_animator`: Movement (`Idle`, `Fly` at 1 m/s ×1.5), `attack` (`Bite Attack` 1.04 s, hit at 0.22 s), `Stagger` |
| mistile (`Deathsquito/Deathsquito.prefab`) | Deathsquito, Mistile | 17 | generic | thorax, head, abdomen, two wing bones, two legs of four | `deathsquito_animator`: `Idle` at speed 3 (the wings' buzz), `attack` ×2 (hit at 0.13 s) |
| hatchling (`Hatchling/Hatchling.prefab`) | Drake, TheHive | 28 | generic | spine 2, neck 3, head, jaw; wings upper, mid, lower plus three fold bones a side; tail 6 | `hatchling_animator`: flying (`IdleHover`, `FlyForward` at 4, `SoarForward` at 12) and ground (`Walk`) |
| volture (`Volture/Volture.prefab`) | Volture | 36 | generic, root `Hip` | bird: legs of upper, lower, foot and four toes; wings of two; neck 2 | `Volture_animator`: `flapping` 2D simple on (`tilt`, `forward_speed`), `Ground`, `Take Off`, `Landing`, `Attack Talons`, `Consume` |
| gjall (`Gjall/Gjall.prefab`) | Gjall | 50 | generic, root `Root` | a floating sack: 20 hull bones (`FrontHead_*`, `BackHead_*` top, side, front, back, bottom), 6 tentacles of 3, mouth bones | `Gjall_animator`: Movement_Flying (idle, turns ±1, `Fly` at 2), spit, shake, egg drop, taunt |
| crow (`animals/birds/Crow.prefab`) | Crow, Seagal, AshCrow | 6 | generic | two body bones, two-bone wings | `crow_flying`: `flapping` (speed 4) and `sailing` (speed 0.01, a frozen glide), on the bool `flapping` |
| hugin (`Raven/Hugin.prefab`) | Hugin, Munin | 51 | generic | a full bird: talons, toes, wings, neck, beak | `raven`: `Idle` (1D on `anglevel`), `talk`, `Flyin`, `Fly away`, `Poff` (tags `visible`, `away`, `flying`) |
| valkyrie (`Valkyrie/Valkyrie.prefab`) | the intro Valkyrie | 62 | generic, root `pelvis` | a woman with wings | `valkyrie`: `flax` and `glide` on `dropped` |
| fallenvalkyrie (`FallenValkyrie/FallenValkyrie.prefab`) | FallenValkyrie | 68 | generic, root `hip` | wings 16.9 m across | seven attack states, `Swooping` on the bool `swooping` |
| dragon (`Dragon/Dragon.prefab`) | Moder, Aspect_Moder | 70 | **humanoid** (22 bones mapped) | a biped-mapped dragon: the avatar's arms are its forelegs, wings separate; armature at 300 | `dragon_animator`: Movement_Flying (`Hover`, `FlyForward` at 10, `Glide` at 20), Movement_Ground (walk at 1, 4, 6), `Takeoff`/`Land` (tag `freeze`), bite, claws, taunt, ice ball, breath |

The ambient birds are not Characters: `RandomFlyingBird` flies them, sets `flapping` and `idle`, and they need no AI,
capsule or ZSyncAnimation. Odin's ravens are driven by `Raven` (the tutorial) through `anglevel` and triggers.

## Wings

- **Bat and drake wings are arm chains**: shoulder, upper arm, lower arm, hand (the Bat), plus separate fold bones on
  the Drake (`Uwingfold.l`, `Mwingfold.l`, `Lwingfold.l`) that pull the membrane in when the wing closes. The membrane
  is skinned between the arm bones; the hand bone spreads its tip.
- **Bird wings are two-bone chains** (`l.wing1`, `l.wing2` on the Volture, 1.7 m out each side; `l_wing1`, `l_wing2`
  on the Crow) with the feathers as cards weighted to them.
- **Insect wings are single rigid bones** (`L.Wing`, `R.Wing` on the Deathsquito) flapped by a fast loop (the idle at
  speed 3).
- Wing spans in the bind pose: Bat 1.4 m, Crow 1.3 m, Deathsquito 1.8 m, Hatchling 6.5 m, Volture 7.8 m, Moder 37 m.

## Building a new flyer

- A flyer that must land gets the two-mode template (Movement_Flying, Movement_Ground, Takeoff, Land) with the
  parameters `flying`, `fly_takeoff`, `fly_land`, `forward_speed`, `turn_speed`, `tilt`; a hoverer gets one mode.
- Author at least: hover or flap idle, fly forward (blend points at the creature's slow and fast fly speeds), glide,
  turn left and right, attack, stagger; with landing: walk, idle, take off, land (the Dragon's `TakeOff` plays at speed
  1.5 under tag `freeze`).
- Keep the flight loop short and fast (the Deathsquito's idle runs at three times speed; the Crow flaps at four) and
  the wing bones few.
- `m_headRotation` is off on the Deathsquito, Drake, Volture and Gjall: generic flyers do not turn their heads.

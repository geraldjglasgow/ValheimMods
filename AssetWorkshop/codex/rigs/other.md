# Rigs of their own

Creatures whose skeleton follows no family: blobs, a flying broom, ghosts, the Deep North's Writhan, the swamp's
Abomination and the late bosses. Ten rigs, measured in `data/rigs.json` (`rig.other`); all on generic avatars. They
show how little a rig needs, and how the game builds bosses. The code's contract is on `README.md`.

## Minimal rigs

**Blob** (`Blob/Blob.prefab` and its seven kinds): **two bones**, `Bone` at the base and `Bone.002` 0.5 m above it. The
body (204 triangles, 128 px, the `Blob` shader with flow textures for the ooze) squashes and stretches on those two;
everything else is the shader. Controller `blob_animator`: `Wakeup?`, `Wakeup`, `Jump`, Movement (`Idle`, `walk` at 1
and 2 m/s with time scales 2 and 4), `Attack fart` (×2.5, hit at 1.04 s of 1.33 s) and `Attack shoot` (×2.0). No
feet: `BlobLava` steps by distance (`m_footlessFootsteps`). No ragdoll; death is an effect. The kinds are material
swaps (`blob_tar`, a crystal Standard material for `BlobFrost`) and scale (`BlobElite` ×1.47, `BlobMorkMini` ×0.5).

**Kvastur** (`Kvastur/BogWitchKvastur.prefab`, the Bog Witch's broom): **five bones**, `Root`, `Shaft` (2.42 m), and
`Head_01..03` hanging down the bristles. 784 triangles, 128 px. It walks and runs (`Walk` at 1, `Run` at 6 m/s) and
slams (`Attack Slam`); its Stagger state holds the humanoid `stagger2` clip, which a generic avatar cannot play.

**TentaRoot** (8 bones in a line) is on `serpent.md`.

Lesson: a creature is as many bones as it has independent motions. A slime needs two; a walking object five.

## Ghosts

**Ghost** (`Ghost/Ghost.prefab`): a humanoid-shaped skeleton of 46 bones (`Root`, `Hips`, `Spine1`, `Spine2`, `Head`,
`Jaw`, full arms and five-finger hands, `Attach.l`/`Attach.r` in the hands) with **no legs** and a generic avatar
(root `Hips`), armature at 76.5. It floats: Movement is a 2D blend on (`idle`, `forward_speed`) between `Fly Forward`
and `Fly Forward Alerted` at 5 m/s; `Attack Swipe`, `Stagger`, `Sleeping`, `Spawn`. 3,819 triangles on 256 px, glowing
(emission 2.8). `Ghost_Void` hangs the Charred skeleton inside it (99 bones in one prefab).

## Deep North and swamp horrors

**Writhan** (`Writhan/Writhan.prefab`): 24 bones, a hunched thing with a **five-bone neck** (`Neck1..5`) between
`Chest` and `Head`, two arms and two legs (`Upperleg.l`, `Lowerleg.l`, `Foot.l`). 3,810 triangles in three parts on
256 px. Controller: Movement (`Walk` at 1, `Walk Fast` at 3, turns), `Attack Bite`, `Attack Explosion`, and a **death
state entered from Any State on `dead`** (`Death Explosion`, tag `freeze`): the creature plays its own death instead of
a ragdoll.

**Abomination** (`Abomination/Abomination.prefab`): 29 bones, a tree-root giant walking on legs and on its hands (its
`FootStep` lists `l.foot`, `r.foot`, `l.finger`, `r.finger`; `l.finger` and `r.finger` are sockets at the knuckles),
root chains (`root1.003` ...) for its hanging roots. 11,784 triangles on 512 px, the only non-boss creature with two
LOD levels (80 % and 40 % of screen height). `check sleeping` → `IdleSleep` → `RiseFromEarth` (`Arise`) → Movement
(walk at 1 and 5); swing ×2, slam ×1.3, ground slam.

## Bosses

Bosses are built like the rest: one rig, one controller, a boss component set (`m_boss` on the Character). What they
add is **more attacks, more parts and bigger numbers**: 10,000–22,500 triangles on 256–512 px (the Fader 1,024),
30–70 bones, 8–25 states.

| Boss (prefab) | Rig | Bones | Avatar | States | What its rig adds |
| --- | --- | --- | --- | --- | --- |
| Eikthyr (`Eikthyr/Eikthyr.prefab`) | eikthyr (a stag) | 30 | generic, root `Root2` | 11 | a 9,408-triangle `chains` part, a `mane` and `extrafur` of cards |
| The Elder (`Greydwarf_king/gd_king.prefab`) | gd_king | 51 | humanoid | 11 | the Greydwarf's naming at 13 m |
| Bonemass (`Bonemass/Bonemass.prefab`) | bonemass | 48 | humanoid | 8 | shoulders 0.65 of its height, the `Bonemass` shader |
| Moder (`Dragon/Dragon.prefab`) | dragon | 70 | humanoid (22) | 10 | wings on a biped-mapped body; `flyer.md` |
| Yagluth (`GoblinKing/GoblinKing.prefab`) | goblinking | 55 | humanoid | 9 | a torso and arms dragging itself; `BeamRoot` socket for its beam |
| The Queen (`SeekerQueen/SeekerQueen.prefab`) | seekerqueen | 44 | generic, root `Hip` | 18 | a 17-bone body chain named `1` to `17`, two pairs of arms, mandibles; `PukePoint` socket |
| Fader (`Fader/Fader.prefab`) | fader | 59 | generic, root `CoG` | 16 | a Rigify `metarig`: `CoG`, pelvis and breast rings, front legs to `front_claws`, four wing bones, 7 tail bones; `HornsBones` part 13,360 triangles |
| Frozen King (`FrozenKing/FrozenKing_0.prefab`) | frozenking_0 | 67 | generic, root `Hips` | 25 | the player's naming plus a seven-bone `ChainWhip` from each hand; armature at 320 |

- **Chains and whips are bone chains in the hands** (the Frozen King's `ChainWhip_Root.L` → `_A` ... `_F`) or modelled
  on the body (Eikthyr, the Frozen King's `Chains` part 13,248 triangles): chains are the most expensive detail the
  game allows itself.
- **Phases are prefabs.** `FrozenKing_p2` runs the Fader's controller and avatar on the Frozen King's skeleton (both
  generic, so the clips bind by bone path); `FrozenKing_p3` holds 123 bones for several bodies in one prefab and an
  override controller.
- **Boss aspects** (`FrozenKing/BossAspects/Aspect_*.prefab`) are the older bosses' meshes and rigs again with one
  material, `Aspect_mat` on the `Distortion` shader (a see-through shimmer; the preview renders nothing).
- **Sleep and wake**: most bosses start in a `freeze`-tagged sleep or chained state and break out on `!sleeping`
  (the Frozen King's `Idle Chained` → `Idle Break Free`).
- Attacks for bosses blend in over 0.1–0.25 s and are many (the Fader has 10 from Any State, the Queen 10, the Frozen
  King 10 plus chained doubles that run state to state).

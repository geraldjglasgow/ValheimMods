# Serpent and chain rigs

Creatures that are one long chain of bones: the sea serpents, the leeches and the Elder's root tentacles. Three rigs,
measured in `data/rigs.json` (`rig.serpent`); all generic avatars. The code's contract is on `README.md`.

## The rigs

**Serpent** (`Serpent/Serpent.prefab`, also `BonemawSerpent/BonemawSerpent.prefab`): 20 bones, `Armature` at 100, 30 m
long. The head leads (forward is +z) and the body trails back:

```
Root
  Main -0.14 (z -3.1)
    Tail1 (z -2.6) > Head (z -1.5) > Bone.004, Bone.005, l.fin, r.fin            (jaws and fins)
    Tail2, Tail3 (z -3.9) > Tail4 (z -5.9) > Tail5 (-7.7) > Tail6 (-10.2) > ... > Tail13 (z -29.0)
```

Segments are 1.9–3.0 m long. The Serpent's clips animate only the head end (`Slithering` at speed 0.5, `Bite` 2.0 s
with the hit at 1.12 s, `Rawr` 6.3 s); the **`Tail` component** moves the body: its eleven `m_tailJoints` (Tail3 to
Tail13) follow the head each frame, each keeping its distance to its parent, bending at most `m_maxAngle` 60 degrees,
sinking by `m_gravity` 5 m/s out of water and 0.1 m/s in it, held above the ground and at the surface
(`m_groundCheck`, `m_waterSurfaceCheck`), smoothed by `m_smoothness` 0.5, radius 0.5 m. `MonsterAI.m_serpentMovement`
steers it in wide turns (`m_serpentTurnRadius` 20 m). The root's capsule is small (0.8 m radius, 1.0 m); the body is
hit through a **capsule collider on every tail bone** (0.008 × 0.04 in bone units, 0.8 m radius and 4 m long at the
armature's 100) and one on the head, so the collision follows the `Tail` chain.

The Bonemaw Serpent's controller (`BoneMawSerpent_animator`) adds swimming under water (`underwater` bool, `Movement
Underwater`), a dive, a ram, a breath and an underwater bite; its locomotion is a 1D blend on `forward_speed`
(`Idle` at 2, `Slither` at 5 m/s).

**Leech** (`Leech/Leech.prefab`): 8 bones, `Armature` at 50, 3.2 m long:

```
root
  Head 0.25 > Jaw (z +0.31)
  Spine1 (z -0.28) > Spine2 (-0.81) > Spine3 (-1.46) > Spine4 (-2.17) > Spine5 (-2.94)
```

Also driven by `Tail` (`Spine2` to `Spine5_end`, `m_maxAngle` 80). Its controller
(`Leech/model/swampfish.controller`) has two states: `Idle` and `attack_bite` (`Bite` at speed 3) on the trigger
`attack`, and nothing else.

**TentaRoot** (`Greydwarf_king/TentaRoot.prefab`, the Elder's roots; also `Tendril`, `Tendril_back` of the Frozen
King): 8 bones standing straight up, `Root` 1.2 m below ground and `Bone1` to `Bone7` every 0.8–1.2 m to 5.5 m.
Three 116-triangle meshes on 256 px. Controller `tentaroot`: `Awake or not` → `wakeup` (rising, tag `freeze`),
`Movement` (an idle sway at speed 0.5), `punch` (`Attack` ×1.2).

## Building a chain creature

- Head at the front on +z (Blender −Y), the chain straight back along the body in the bind pose, segments of equal
  length (the Serpent's 2–3 m, the Leech's 0.5–0.8 m), names counting away from the head (`Tail1..n`, `Spine1..n`).
- Animate only what the code does not: the head, jaws and fins (idle, bite, taunt, stagger); list the rest in a `Tail`
  component and let it follow. A root chain (roots, tentacles, stalks) is animated whole: sway idle, rise, strike.
- Weight each ring of vertices to at most the two nearest chain bones, blending linearly between them, as the
  workshop's Kraken column does (workshop README).
- A long body culls late: its LODGroup and the renderer's bounds must hold the whole length (the Serpent's LODGroup size
  is 35.1 m for a 34.7 m body).
- Hitboxes follow the chain: a capsule collider on each segment bone, as the Serpent has.

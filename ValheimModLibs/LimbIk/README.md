# LimbIk

Two-bone IK for a character's limbs, posed after the animator.

- `TwoBoneIk.Solve(upper, lower, end, target, hint)`: bends a two-bone limb (shoulder, elbow, hand; or hip, knee,
  foot) so the end's origin reaches the target, as far as the limb stretches, and rolls it about the root-to-end line
  until the middle joint points at the hint, a point in the world. It writes the bones' rotations, so call it in a
  `LateUpdate`, after the animator; a target off the end bone's origin (a grip point in the hand) is reached by moving
  the target by the same offset, and a second pass settles it.

```csharp
Vector3 offset = grip.position - hand.position;
for (int pass = 0; pass < 2; pass++)
    TwoBoneIk.Solve(upperArm, foreArm, hand, target - offset, elbowHint);
```

Pure Unity transforms: no game types, no Harmony. Merged into the consuming mod with ILRepack like every library here.
Consumers: Broadside (the helmsman's hands on a tiller), MannedWeapons (an operator's fists on a weapon's grips) and
Elite Creatures Pack (the hands on the Bone Crossbow and the greataxe's haft; its own copy `Core/TwoBoneIk` went on
2026-10-26).

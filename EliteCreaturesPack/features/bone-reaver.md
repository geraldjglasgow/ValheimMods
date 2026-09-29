# Bone Reaver (working name)

Model and rig commissioned for a skeleton 25% taller than the ordinary Valheim skeleton, dressed in torn cloth,
dragging a giant decorated bone axe with both hands. The shaft is a vertebral column. A hip bag carries throwing
daggers. The art should sit alongside the game's own skeletons.

Art revision: the clothing uses weathered browns only, with irregular folded panels and torn edges. The knives
sit in a soft leather roll suspended from two visible belt loops. The axe's vertebral column follows an S-curve,
with varied bodies, neural arches and overlapping joints; the two hand-grip seats remain unchanged.

The model, editable Blender scene, side-by-side vanilla reference scene, original gear FBX and 16 animations
are in `../../AssetWorkshop/assets/ecp_bone_reaver/`. That directory's README describes the files and event contract.

Requested fight:

1. Lift the axe overhead and slam it down, scattering rock particles at contact.
2. Swing it through a circle, then wait four seconds.
3. Sweep across the front and finish with the axe resting on the other side. Subsequent attacks start on that side.
4. Turn the skull backward, glow purple for half a second, then swing the axe up and over to strike behind.
5. When nobody is in melee range at attack selection, draw three daggers between the fingers and throw all three.
6. Against kiting, use the circular windup to throw the giant axe at a player, fast enough to demand a dodge.
   After release wait 1.5 seconds, yell and summon another axe into the lowered hands. The projectile shatters
   into bone fragments when it hits something.

Art/animation assets are built and locally validated. Gameplay, spawn rules, stats, ranged attack selection,
projectile speed, network replication and in-game testing have not been implemented by this asset task.
The two ranged moves' selection policy remains a gameplay decision; their animation cues are already authored.

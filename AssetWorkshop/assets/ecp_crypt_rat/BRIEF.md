# Crypt Rat

## 1. Identity
`ecp_crypt_rat`: an undead rat for Swamp terrain and Sunken Crypt interiors only.
Workshop creature, reserved ECP prefix; no mod release or installation in this task.
Swamp tier 2 appearance, Greydwarf Shaman combat strength (interpretation of “greyling shaman”).

## 2. Category
`creature.quadruped`, nine reference models. Triangles min/p25/median/p75/max:
1754/2142/2966/4764/6250. Texture 64/128/256/256/512 px. Density
30/43.9/58.4/80.7/99.6 px/m. Height 0.57–2.61 m; longest side not measured.

## 3. References
Boar: small heavy quadruped silhouette and scale. Wolf: muzzle and articulated bite.
Draugr: desaturated dead skin and swamp readability. Skeleton: aged bone contrast.
Local reference prefabs are preview only; no game meshes or textures exported.

## 4. Silhouette
Low pear-shaped rump, narrow rat snout, round torn ears, large paired incisors,
exposed flank ribs and a tapering naked tail. Body roughly 1.2 m long and 0.65 m high;
tail adds about 1 m. Large enough to read in a crypt, below the player's waist.

## 5. Parts
Metres, ground origin, Z up, facing -Y. Not held, dropped, or a building piece.
Hunched torso 0.56 x 0.95 x 0.5 m; narrow head 0.38 x 0.5 x 0.3 m;
four crouched legs 0.3 m; ears 0.22 m; tail about 1 m, five joints.
Tail uses a continuous 21-ring mesh with blended joint weights for smooth bending.
Its baked motion uses a damped world-space chain, gravity, segment-length and bend
constraints, and floor contact. Movement leads from the base, with tip lag and settling.
Front shoulder masses narrowed by 28% and shortened front-to-back by 28%, with an
11% height reduction and slimmer upper foreleg taper; hindquarters retain their size.
Broad hide forms are faceted. Bone arcs and incisors are geometry; fur and decay are paint.

## 6. Budget
Target about 3000 triangles, one 256 px atlas, density 45–90 px/m.
AO_STRENGTH 0.2, NORMAL_MAP true, NORMAL_FROM_ALBEDO 3.

## 7. Materials
Codex recipes: skin.skin (draugr preset, grey olive tint), leather.fur (wolf preset,
mud brown tint), bone.bone (aged warm bone), skin.flesh (muted torn tissue).
Muted olive/brown swamp palette; bone kept brighter to reveal teeth and ribs.
Runtime dressing contract: Custom/Creature. Workshop preview uses baked Standard material.

## 8. Regions
fur: hide; skin: tail/ears/feet; bone: ribs/teeth/skull; secondary: torn tissue;
glow: small sickly eyes. No variants requested; one authored appearance.

## 9. Rig and clips
Route (c), original generic quadruped rig. Root, Body, Head, Jaw, four paired
upper/lower legs, five tail bones; mouth origin named BiteOrigin, eye socket EyePos.
Revised rig has 25 bones, including independently weighted ears and four paws.
Clips at 30 fps: idle 5 s, walk 1 s, run/scurry 8 frames, attack_bite 1.8 s,
stagger 0.6 s, death 1.2 s. Bite impact frame 20; attack tag `attack`.
Idle includes irregular sniffing, head turns, ear flicks and breathing with stationary paws.
Scurry uses analytic leg placement and separate support/swing phases; its preview cadence
is driven by travelled distance. Attack crouches, pushes off, snaps shut, lands and recovers.
The separate 9.37-second Blender performance follows seeded semi-random paths with brief sniffing
pauses; it does not implement runtime monster navigation. Loop and paw checks are in
out/motion_checks.json. Runtime lunge movement is 0.95 m over frames 14–22.33.
Windup shifts the body 17 cm backward and deepens the crouch. The lunge accelerates off
planted hind paws through frame 15.67, follows a low ballistic arc with constant forward
speed through frame 20.67, then brakes over planted front paws. Hind paws land at frame 21.5;
the body compresses and takes small recovery steps. The spring is 20% faster than the
previous ten-frame version; backward anticipation retains its timing and amplitude.
Motion, preview travel and tail simulation share lunge.py. Jaw/head motion is restrained.
Scurry travel is approximately 3x the previous preview, with foot cadence matched to
travel distance, shorter pauses and three-frame transitions. Bite timing is specified above.
In-place clips; forward lunge distance is a runtime movement contract, not root motion.
Locomotion requires forward_speed and footstep curves; dead bool and stagger trigger.

## 10. Effects
No new effects. Future integration can reuse local game blood hit effects.
No building placement effects.

## 11. Sounds
No audio authored or bundled. Select small creature recordings at runtime during integration;
no synthesized rat noises and no claim of auditioned sound.

## 12. Gameplay integration contract
Not implemented in a mod by the workshop model build. See gameplay.json for exact requested
behavior. Local reference Greydwarf_Shaman has 60 health; its default weapon GUID
50221819787faba40a31bee2055cbf08 resolves to Greydwarf_attack, which has 14 slash damage.
Use 60 health and 14 direct piercing bite damage, keeping the magnitude of that melee attack.
Successful damaging bite adds five bleed stacks; tick every 1.5 s, deal current stack count
as damage, then remove one. One bite: 5+4+3+2+1=15 damage over 7.5 s.
Proposed repeated-bite policy: add five without resetting the existing tick clock; no cap
specified by the user. No bleed on misses, dodges or fully blocked damage.
Drops BoneFragments (proposed 1–2; count unspecified), 0–1 IronOre.
Spawn only Swamp outdoors and Sunken Crypt rooms; no other dungeons or biomes.
Future physics: root capsule fitted to torso; no tail collision. Networking: owner decides
hit, movement, spawning and loot; victim owner ticks bleed; clients play synchronized animation.
Persist/transfer remaining stacks and tick timing across network ownership changes.
No icon, recipe or player placement. Textures: albedo, normal, region mask.

## 13. Validation
Build and review four views, in-place bite poses, skinning and exported actions.
Run codex style check and compare to Boar, Wolf, Draugr and Skeleton.
Measured: 3370 triangles, 256 px atlas, 51.3 px/m, 0.727 m high, 2.305 m nose-to-tail
extent. All geometry, texture and body paint checks pass. One accepted exception:
the eye region's blotch estimate is 4.99 m, outside skin-family ranges. These two tiny
isolated eye islands occupy only 1.1% of the atlas; that correlation estimate is not
a physical blotch on the model. They are intentionally one pale tone rather than
full-body skin patterning. Reviewed four model views and reference lineup.
Gameplay, Unity controller playback and dedicated-server validation are separate
from this workshop-only deliverable. Animation events and footstep curves are supplied
in the contract for a future Unity importer; FBX alone does not implement these callbacks.

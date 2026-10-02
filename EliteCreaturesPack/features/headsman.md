# Headsman (working name): greataxe skeleton boss

Status: **in the mod, untested in game** (2026-09-29): the Crypt Executioner (`ECP_Headsman`), its spawner in
burial chambers, the raised skeleton, the axehead and the players' Executioner's Greataxe, sections `25 - Crypt
Executioner` and `26 - Executioner Greataxe`. Code in `EliteCreaturesPack/Headsman`; workshop:
`../ValheimAssets/Assets/Creatures/Executioner/ecp_headsman` (`build.ps1 [-Open] [-Stills] [-SkipUnity] [-Player] [-Bundle] [-Install]`) and
`../ValheimAssets/Tools/Unity/Assets/Editor/Headsman`, `.../Greataxe`; the Blender previews are
`out/blender/headsman_moves.blend` and the player's `out/player/greataxe_player.blend`.

The game's Skeleton, 1.25 times its size, carrying the bone greataxe (`../ValheimAssets/Assets/Weapons/SkeletonArsenal/ecp_bone_greataxe`, made
with ChatGPT; the user's favourite) in both fists: the right fist on the upper grip seat, the left on the lower. It
stands, walks and runs with the axe at a ready hold across the front, head by the right shoulder, edge to the enemy.

## The moves (from the user)

1. **Slam.** Raises the axe over its head, cocks it far back, and comes down heavy on the target and into the
   ground in front; chunks thrown up where it lands.
2. **Ground scrape (the sweep).** Winds round to the right, puts the edge to the ground and drags it fast in an arc
   across the front, right to left. What the axe head passes through is hit. (Until 2026-09-29 night a rubble
   shockwave ran out from the arc and did the hitting; the user had it removed after the first fight: "remove the
   ground particles and just have it as a front sweep attack".)

**Only the axe head hits** in the slam, the sweep and the spin (user, 2026-09-29 night, from the first fight with the
hit shapes drawn): the game's sweep from the body reached nearly 3 m of empty air. A sphere of `Axe Head Radius`
(0.5 m) round the blade's edge, swept frame to frame through each move's cut window, hits once per swing: in the slam
only where the axe lands ("I only want the little circle as the hitbox"), in the spin the ring the head draws
("only the axehead circle should be doing damage"), so standing close in to its body is safe. The slam's wind-up
(raise and cock, clip 0 to 1.05 s) plays 1.35 times faster ("needs to be a bit faster"); knockback halved (slam 45,
the rest 30).
3. **Spin.** Winds up to the right and spins a full turn with the axe held out flat; the weight carries it past,
   it staggers to catch its balance and straightens.
4. **Overhead throw** (target out of melee range). Axe up over the head, cocked back, thrown overhand straight at the
   target, tumbling end over end. It shatters on impact; the bones gather and a skeleton unit rises there. The boss
   raises both hands for 2 s while a new axe forms in them; the skeleton forms in the same 2 s (user: "regenerate at
   the same rate/time").
5. **Spin throw** (target out of melee range). The spin's wind-up, the axe let go flat as it comes round at the
   target; same shatter, rising skeleton and re-forming axe.
6. **Rear strike** (several targets, one behind). The skull turns all the way round and the moment it is round the
   axe goes up and over the head to strike behind (the user asked for no wait and a faster swing after the first
   preview); then the feet step round until the body faces where the head looks, the head untwisting as it goes.

## Decisions made without the user (overturn freely)

- Rear strike hits with the hooked spike on the back of the axe head (the poll), so the fists never turn the axe
  over; it lands at a player's head and chest height.
- The spin's balance catch is a low stagger with the axe dragging; leaning on the axe like a crutch was out of reach.
- Re-forming axe (user, 2026-09-29): no swirling rocks. It starts small and ghostly - pale, whitish, greenish,
  bluish, half see-through - and stays so for 0.5 s (`HeadsmanGhost.Hold`); then, as it grows to full size, its pieces
  turn to their own colour one at a time and quickly, from the bottom vertebra of the haft up to the top of the head
  (`../ValheimAssets/Assets/Creatures/Executioner/ecp_headsman/blender_reveal.py`). A light at the grip and a ring on the ground stay.
- The thrown axe flies straight at 16 m/s (preview), the overhand throw tumbling forward (head over the top), the
  spin throw turning flat the way the boss spun.
- Summon (user, 2026-09-29): the axe breaks into its own pieces (22 vertebrae, blade shards, the hooked back piece in
  two) that scatter and lie; they lift, in their own colour (never recoloured), and swirl round the spot while the
  skeleton forms in the same 2 s window as the boss's new axe (`HeadsmanRanged.Forming`): after the same 0.5 s its
  bones (the game's Skeleton is 49 separate bones) fade into existence one at a time from the feet up, each through
  the axe's pale ghost colour to its own, and one at a time each piece darts into it where a bone is just appearing
  and is gone. Blender does the pieces and the reveal (`blender_shatter.py`, `blender_reveal.py`).
- Rear strike (user: "remember this is a skeleton ... not human"): the skull snaps round, then the whole upper body
  wrings 180 degrees round at the waist to follow it (both done in code, `HeadsmanMove.HeadSpin`/`TorsoSpin`), the
  hips and legs still facing the old way; a two-handed chop at the target behind; then the legs hop round under the
  still upper body (the creature's own turn) while the upper body unwinds, the left hand off the axe during the hop.
- Sounds (user: overdone, "lobster too buttery"; then "way too much bass for what is going on"; then, of sounds
  synthesized from scratch, "comically bad ... cartoon noises, they don't fit Valheim at all"): few, quiet, and real
  Valheim recordings chosen for the event's size, never synthesized. The game's troll/sledge swings and impacts are
  70-93 % of their energy below 80 Hz, so each cue is a recipe (`../ValheimAssets/Assets/Creatures/Executioner/ecp_headsman/sfx.py`): the
  Skeleton's own melee swing for the swings (cut short on the slam, ending as the blow lands), the player's spear
  throw for the throw, the game's axe hit with the hoe biting into dirt for the slam (no rubble: the user), the
  player's battleaxe hit for the rear strike, the hoe and cultivator in soil for the scrape, Fader's fissure pillars (two, the second further out) with rock
  crumble for the shockwave (the Stone Golem's spike wall rang like metal at 4 kHz: "a ping ... doesn't fit"), the Skeleton's bone shatter, bone hits and rattle for the axe breaking; each started so its peak lands on
  the moment and high-passed (90-150 Hz) to take the sub-bass off, every move under 3 % below 80 Hz. No footsteps (the
  user: "2 taps at the end ... not needed"). The pieces strike into the new skeleton in silence.
- The re-forming axe keeps the game's own sounds, which the user likes: a Dverger charge-up while it forms and a dark
  Fader thud when it is solid ("not sparkly, not like a buff").

## Into the mod (user, 2026-09-29)

- Shown to players as the **Crypt Executioner**; the prefab stays `ECP_Headsman`. Config section `25 - Crypt Executioner`.
- A mini boss of the Black Forest burial chambers: about 1 chamber in 4 gets one (rolled once when the chamber is
  generated, from its seed), placed by a spawner in one of its rooms. After it dies it **returns after a while** (the
  spawner's respawn time; the game's CreatureSpawner keeps and syncs it).
- About 900 health; hits about 50-60, the sweep about 35; at most 2 summoned skeletons alive at a time.
- Drops coins and bones, and **its axehead at 50 %** (`ECP_ExecutionerAxehead`).
- Players craft the **Executioner's Greataxe** (`ECP_ExecutionerGreataxe`), part of the skeleton arsenal (every bone
  weapon of the mod, `skeleton-arsenal.md`), from the axehead, spines
  (`ECP_Spine`, the arsenal's) and bone fragments. As strong as a fully upgraded Bronze Axe (slash 55, chop 49).
  A Battleaxe-type two-handed weapon: the game's Battleaxe stance, movement and block; its combo is the Battleaxe's
  first swing (a slash in front), the greatsword's whirling second swing (three quarters round, both fists together;
  the atgeir's 360 spin was tried first but holds a spear's shaft at arm's length, where the left fist cannot reach
  this haft), then an overhead, with a 0.75 s recovery after it (a Speed event slowing the overhead's clip after its
  hit), all the game's own player clips put into the Battleaxe's three combo states by an override while the greataxe
  is in hand. The spin hits all round (360).
- Held near the butt (the Battleaxe's clips put the left fist 0.77 m up the haft, on this axe's blade otherwise), the
  S-curved haft aimed through the clips' left fist; and the left fist is kept on the haft on every peer after each
  pose (user: "the left hand isn't really holding the axe"): it takes the haft point nearest where the clip has it
  among those the arm reaches, the arm following by two-bone IK (workshop `Greataxe/GreataxeGrip`, to port as is). Sounds: the Battleaxe's
  own, as the game plays them - its swing (`sfx_battleaxe_swing_wosh`) on all three steps, as the game plays it on the
  Battleaxe's three and on the wooden greatsword's whirl, and its hit. A first set (the Battleaxe's swing high-passed at
  240 Hz, a skeleton's sword swing for the spin, the sledge's swing for the overhead) was thin hiss with the body gone;
  user: "they don't sound like they belong in valheim" (2026-09-29).
- Preview before anything ships (user: "show me the player character in its states with the axe, and attacking"):
  `../ValheimAssets/Assets/Creatures/Executioner/ecp_headsman/build.ps1 -Player -Open` - every stance (idle, walk, jog, run, crouch, sneak,
  block, jump) and the combo from over the shoulder, the front and the side, with sounds; `-Player -Combo -Open` only
  the combo. The overhead is the user's pick, the Battleaxe's third swing (the sledge's smash was the other candidate).

## In the mod

`EliteCreaturesPack/Headsman`, by folder:

- `HeadsmanPrefabs` builds everything on each ZNetScene wake (the bundle `ecp_headsman` loaded once) and reapplies
  the settings; `HeadsmanSettings` (25), `Greataxe/GreataxeSettings` (26); `HeadsmanWords`; `HeadsmanChambers`.
- `Motion/`: the moves' table with their clip times and sound cues (`HeadsmanMoves`, the numbers of the workshop's
  `HeadsmanMelee`/`HeadsmanRanged`), the animator clock every peer reads the clip time from (`HeadsmanClock`), the
  world clock for ZDO moments (`HeadsmanTime`), the axe's shape (`HeadsmanAxe`), `TwoBoneIk`.
- `Build/`: the creature (a copy of the Skeleton at 1.25, `HeadsmanCreature`), its kit and animator (`HeadsmanKit`),
  its six attacks (copies of the skeleton's sword, `HeadsmanAttacks`), the two thrown axes (copies of the archer's
  arrow, `HeadsmanThrow`).
- `Fight/`: the owner's brain (the rear strike's turn, the axe head's hits `HeadsmanCut`, the slam's wind-up speed), the AI patches (the
  AI waits through the rear strike and a raised skeleton's forming; the rear strike only with two foes within 8 m).
- `Look/`: on every peer, the rig (`HeadsmanRig`: head and waist wrung round, the axe forming `HeadsmanForming`,
  rocks `HeadsmanRocks`, sound cues), the shatter (a networked, unsaved object the thrown axe spawns where it breaks;
  its pieces `HeadsmanShards`), the raised skeleton (`HeadsmanSummon`, `ECP_HeadsmanSkeleton`, a copy of
  `Skeleton_NoArcher` with no drops) and its forming (`HeadsmanRising`, bones split per mesh by `HeadsmanBones`).
- `Sound/`: the cues played from the game's clips (`HeadsmanSounds`, `HeadsmanVoice`) by the table
  `HeadsmanSoundTable.cs`, which `../ValheimAssets/Assets/Creatures/Executioner/ecp_headsman/sfx_table.py` writes from the preview's recipes
  (build.ps1 runs it; do not edit the table by hand).
- `Greataxe/`: the axehead and greataxe items (`GreataxeItems`, `GreataxeLook`), the recipe, the player's override
  and left-hand grip (`GreataxeAnimations`, `GreataxeGrip`,
  `GreataxeHold` on every player), the patches (`GreataxePatches`: Player.Awake, Attack.Start).

Multiplayer: the creature's animator is synced by the game, so every peer reads the move and its time from it and
draws the rest itself; damage, the rear strike's turn, the shockwave and summoning are the owner's. The moment an axe
broke and the moment a skeleton began to form are kept in ZDOs on the world clock (`ecp_hs_hit`, `ecp_hs_rise`), so a
peer arriving late sees the forming where it is and hears nothing. The player's greataxe swings on every peer from the
game's synced triggers and equipment; its swing and hit sounds are the Battleaxe's, which the game networks itself.

## Decisions made without the user, in the mod (overturn freely)

- Burial chambers: the dungeons of the game's ForestCrypt theme (not Hildir's crypt). The spawner stands in the
  chamber's largest room that is not the entrance, a dead end or a doorway, on its floor. Chambers generated before
  the mod roll too, once, on the first owner who loads them. `Respawn Days` 3 (a game day is 30 minutes). `Chambers` is a percentage (25).
- The spawner raises it when a player is within 25 m; it patrols back to its spawn point. It is not a boss (no boss
  bar, no global key); Elite Creatures Reborn may roll stars and mutations on it like any creature.
- The raised skeleton is the game's sword-and-shield skeleton, dropping nothing; at most `Summons` (2) stand within
  40 m of a new one. It faces back the way the axe flew. It plays the re-forming's own two sounds (charge-up as it
  begins, the thud when whole), which the preview only played at the boss; its eyes, smoke and weapon show once whole.
- Sound level: one factor for all cues, so the boss's voice is as loud as the game plays the skeleton's (0.8); the
  preview's relative levels are kept, capped at full volume (the throw and the re-forming's).
- The greataxe: tool tier 2 (a Bronze Axe's), the Battleaxe's durability, weight, stamina and block; not upgradable;
  recipe at workbench level 3: the axehead, 1 spine (8 up to 0.5.0; bone fragments when the arsenal's `ECP_Spine` is not in
  the game) and 10 bone fragments. Its secondary attack is the Battleaxe's.

## Test checklist

Build, the user restarts LocalTesting; `devcommands`, then:

- `spawn ECP_Headsman`: it stands with the axe at the ready; each of the six moves plays with its sounds (slam with
  chunks, scrape with slabs and the shockwave's damage, spin, rear strike only with two foes near, both throws from
  7-22 m). Health 900, damage per the section.
- A throw: the axe shatters where it hits, pieces scatter, lift and dart into a skeleton forming from the feet up in
  the same 2 s as the boss's new axe; the skeleton waits until whole, then fights; no more than two.
- With a second player (or a dedicated server): the other player sees and hears the same moves, the shatter, the
  forming skeleton and the new axe.
- Kill it: coins, bones, the axehead about half the time; `ECP_ExecutionerAxehead` has its model and icon.
- Burial chambers: find one with the spawner (log line "Crypt Executioner waits in ..."); it spawns when you come in;
  after its death it returns after the configured days.
- `spawn ECP_ExecutionerGreataxe` or craft it at the workbench: held with both hands in every stance; slash, spin
  (hits all round), overhead with the 0.75 s recovery; the left hand stays on the haft; each swing and hit sounds as a
  Battleaxe's; the other player sees and hears the same.
- `Enabled` off: no new spawners, the existing ones stop spawning. `Recipe` off: the greataxe is not craftable.

## Notes from before the mod


- Sounds: each cue is played from the game's own clips by the recipe in sfx.py (clip, delay or start time, volume,
  pitch, AudioHighPassFilter/AudioLowPassFilter); nothing of the game's audio ships. The .wav files sfx.py writes are
  only the preview's mix.
- The clips are humanoid clips on the game's Skeleton avatar, like the Skeleton Crossbowman's. What they cannot do is
  done in code at the clip's time: the head turn past 80 degrees (`HeadsmanMove.HeadSpin`), the rear strike's upper
  body wrung round at the waist (`HeadsmanMove.TorsoSpin`) and its legs' turn (`HeadsmanKeys.RootYaw`, 180 degrees
  over 0.95 to 1.35 s), the axe hidden from the throw until solid, the
  thrown axe as a projectile, the shatter and the summoned skeleton, the shockwave as the scrape's damage.
- Selection: melee moves in reach; the throws only when the target is out of melee range; the rear strike only with
  two or more targets and one behind.
- Open: spawn place and rules, health and damage, loot, whether it is a boss with a boss bar, the summoned unit's
  kind and cap, the shockwave's damage and reach at full size.

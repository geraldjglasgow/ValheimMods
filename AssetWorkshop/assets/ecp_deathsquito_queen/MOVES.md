# Deathsquito Queen: the fight

The behaviour spec for the Queen (`BRIEF.md` is the model). Workshop preview only for now (user, 2026-09-29): the rig,
the moves, the eggs, the needles, the hatching and the sounds are built and shown here in Blender; no mod
code until the user decides to release her. The mod is then built from this page and the timings in `queen_moves.py`.

## The moves (from the user, 2026-09-29)

1. **Piercing dive.** She hovers, pauses, then dives in a straight line with her proboscis. Heavy pierce damage; can
   be dodged.
2. **Brood.** Shoots eggs out of her abdomen, as many as the players on the server plus 2. They land on the ground
   half buried. After 20 seconds each egg hatches: it bursts open, the shell disintegrates and is removed, and one
   Deathsquito comes out of each egg.
3. ~~Wingbeat gale~~: removed by the user (2026-09-29, after five rounds on its air pulse). It was: fly up a bit,
   3 quick wing flaps sending an airwave burst at a player, once per player logged in, at most 3 times.
4. **Needle volley.** Shoots 8 needles in 1 second at one player. A needle that hits a player damages them and
   disappears; one that hits terrain stays stuck in it. If the player dodges, the volley's direction does not change
   (user, later the same day): she aims once, as the volley begins, and every needle keeps to that line. The stuck
   needles stay visible in the ground for 30 seconds but do no damage (user, later again; at first they stayed until
   she de-aggroed or the fight ended, hurting every 0.75 s and slowing 20 % whoever stood on them).
5. **Lunge.** A basic lunge attack.
6. **Needle shot.** When all players are ranged or out of melee distance, her basic attack is a fast needle shot at
   a player.
7. **Whirl.** She zips round in a very small circle, damaging anyone who did not dodge within reach of the proboscis.

Also from the user, on seeing the first preview: her legs looked stationary and unnatural, so they sway as she moves;
and the proboscis too. Then: in the wing attack she only dangled her feet "like a child dangling feet off a cliffside";
the whole body should move, bend and sway as a real insect's would doing these attacks. Then the airwave attack went.

Also from the user: a rare Plains mini-boss (like the Rime Giant), coming back after a few game days once killed;
about 2,500 health, no boss bar or global key; drops game items only (coins, needles and Deathsquito loot).

## Decisions made without the user (overturn freely)

- **Legs and proboscis** (`queen_sway.py`): each of the six legs is a chain of damped springs (hip swung back and out,
  knee, tarsus) driven by her own acceleration and speed in her frame, each leg with its own stiffness and idle sway so
  the six never move as one: speeding up swings them back, braking forward past rest and back, turns and the whirl
  fling them out, a drop lets them fold, a climb lets them hang. Braking and flying backwards swing them only gently
  (a hard turn at speed threw them up over her back). The proboscis droops on a climb, swings against a turn and
  searches slowly from side to side, held still while she aims (the dive, lunge, needle shot and volley).
- **The whole body in each move** (`queen_sway.py`, the moves' `brace`, `reach`, `spread`): her legs reach for what
  she is doing (drawn up under her as she draws back for the lunge, braced for the volley and the whirl, forelegs
  thrust out in the lunge and lifted as she rears for the volley, splayed and clutching while she lays), and twitch
  now and then while she hovers; her abdomen sways on a slow spring (drooping on a climb, swinging out of turns,
  pumping as she breathes); she tilts into speeding up and braking and banks into turns, held steady on her aim. Each
  volley shot kicks her back; each egg squeeze heaves her. The buzzing wings sweep and twist a little with each stroke.
- **The volley's dodge in the preview:** the second player steps 1.8 m aside after the second needle; the first two
  hit, the rest keep to their lines and stick in the ground where the player stood.
- **Where the needles come from:** the proboscis tip spits them (both the volley and the single shot); the eggs come
  out of the abdomen's tip. The volley's tell is a rear back (nose up) before the burst; the single shot has only a
  short head jerk.
- **Counting players:** "on the server" and "logged in" are every player online; "present" are the players within
  her fight range (40 m).
- **The dive** aims where the target is when the pause ends and holds that line (so a sidestep in the dive dodges
  it), 28 m/s, overshooting the target by about 3 m before she pulls up.
- **The whirl** is 1.5 turns of a 1.4 m circle in 1 s, the body banked and yawed 50 degrees outward so the needle
  sweeps outside the circle: it reaches about 2.5 m from the circle's centre.
- **Eggs** land 3 to 7 m round the present players, spread so no two are within 2 m; each is 0.9 m long, lies at a
  slant half sunk in the ground, glows through its shell and throbs faster in its last 3 seconds. The hatchling is the
  game's own Deathsquito.
- **Needles** fly at 35 m/s (the single shot 45 m/s); the volley spreads up to 1.2 m round the target, so a moving
  player takes some and the rest stick in the ground round them. A stuck needle (the volley's or a missed single shot)
  is harmless and stays 30 s, then crumbles into the ground in 0.6 s with a puff of dust.
- Numbers for the mod (first guesses, Plains tier; the Deathsquito's sting is 90 pierce): dive 110 pierce, lunge 70
  pierce, whirl 60 pierce, needle 25 pierce each (volley and single shot).

## Timings

Seconds from the move's start; `hit` is when damage is dealt. The preview keys them at 30 frames a second
(`queen_moves.py` holds the same numbers; change both together).

| Move | Tell | Action | Hit | Recover | Total |
| --- | --- | --- | --- | --- | --- |
| 1 dive | 0.0-0.5 rise 1 m, back 0.5 m, nose down 20 degrees; 0.5-1.1 pause, trembling, legs drawn up | 1.1-1.45 straight dive, 28 m/s | 1.3 (tip passes the target) | 1.45-2.6 pull up, turn back, climb | 2.6 |
| 2 brood | 0.0-0.6 rise 0.6 m, abdomen curls down 40 degrees, two pumps | 0.6 + 0.25 k: egg k launched, lands 0.8 s later | hatch 20 s after landing | 1.85-2.6 | 2.6 |
| 4 volley | 0.0-0.5 back up 0.5 m, rear nose up 25 degrees | 0.55 + 0.125 k, k = 0..7: a needle each, a head recoil each | each needle's arrival | 1.55-2.1 | 2.1 |
| 5 lunge | 0.0-0.35 draw back 0.4 m, head up | 0.35-0.55 lunge 2 m forward | 0.5 | 0.55-1.2 back 1 m | 1.2 |
| 6 needle shot | 0.0-0.3 head pulls back and aims | 0.3 one needle | its arrival | 0.3-0.8 | 0.8 |
| 7 whirl | 0.0-0.3 dip, tilt, buzz up | 0.3-1.3 1.5 turns round a 1.4 m circle | whenever the needle passes someone | 1.3-1.8 stop, wobble, face the target | 1.8 |

Hatching: from 17 s after landing the egg throbs (1.0 to 1.06 in size, 1 then 3 beats a second) and its glow grows; at
20 s the shell splits into five petals that burst outward and tumble, then crumble away over 0.6 s in a puff of dust,
and the Deathsquito rises out of the cup (0.8 s); the cup crumbles last.

## Sounds

Chosen from the game's own recordings, sized for her: `SOUNDS.md` (written with `queen_sfx.py`). Nobody has listened
to them yet.

## The preview

```
.\assets\ecp_deathsquito_queen\build.ps1 [-SkipModels] [-SkipSounds] [-Stills] [-Render] [-Open]
blender assets/ecp_deathsquito_queen/out/preview/queen_moves.blend --python assets/ecp_deathsquito_queen/queen_open.py
```

`build.ps1` builds her model and the eggs and needle (`ecp_queen_egg`, `ecp_queen_egg_burst`, `ecp_queen_needle`), the
sounds, the rig (`queen_rig.py`) and the fight (`queen_preview.py`): 25 s at 30 frames a second on Plains heath, two
players (the game's Player, from the reference export) and the moves in the order dive, lunge, whirl, needle shot,
needle volley, brood, then the eggs hatching "20 seconds later" and, "30 seconds after the volley", its needles
crumbling away. The story counts 3 players online and 2 present: 5 eggs. A camera per move, framed round where she goes and what she aims at, a caption
naming the move, her belly and eyes glowing as their emission map will, the sounds on the timeline. `-Stills` renders
three frames of every shot to `out/preview/stills/`, `-Render` the whole fight to `out/preview/queen_moves.mp4`.

| File | Does |
| --- | --- |
| `queen_moves.py` | her state (place, turn, pitch, bank, wing lift and buzz, abdomen curl, head pitch, the legs' tuck and intents, steadiness), the Move base and Hover, the needle tip and egg point in the world |
| `queen_attacks.py`, `queen_ranged.py` | the six moves: timings, travel, poses, events (sounds, hits, needles, eggs) |
| `queen_pose.py` | a state onto the rig: the root empty, the game bones' rotations, the legs' and proboscis' from the springs |
| `queen_sway.py` | the springs of her legs (with each move's intent and idle twitches), abdomen, posture and proboscis |
| `queen_keys.py` | keyframes in bulk (every frame collected, each curve filled at once) |
| `queen_props.py`, `queen_fx.py`, `queen_hatch.py` | needles, eggs, flashes, dust, the throb, the burst, the hatchlings |
| `queen_stage.py`, `queen_shots.py`, `queen_audio.py` | ground, light, props, players; cameras, captions, glow, render; sound strips |

## For the mod (later)

- A copy of the game's `Deathsquito` (its animator and 17-bone skeleton, the buzzing idle and the sting) with its
  Visual scaled 2.5 and the Queen's body skinned to it (`BundlePrefabs.CreatureBody.Wear`, which also adds her own
  bones by name: the front and hind leg chains `L.legF1`..`R.legH4` and `Proboscis`); the moves' poses on top in code
  (the abdomen's pumps, the head's aim, each move's leg intent) and the springs
  of her legs, abdomen, posture and proboscis from her velocity every frame, as the Kraken poses its bones.
- The owner decides every move, target, egg spot and needle; eggs and stuck needles are networked objects whose
  moments (landed, hatch time) live in their ZDOs on the world clock, so a late peer sees them where they are; every
  peer draws the throbbing and the burst itself.
- A stuck needle is scenery: a networked, unsaved object the needle spawns where it sticks, with no collider or
  damage, removed by its owner 30 s later (the moment it stuck in its ZDO on the world clock, so a late peer
  crumbles it on time).

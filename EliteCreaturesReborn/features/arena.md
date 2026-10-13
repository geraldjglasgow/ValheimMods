# Elite Creatures Reborn - specification: the Arena (Hel's Trial)

Status: specified 2026-10-12, being built. Not seen in game.

An endless arena in Hel's hall: players step into the hall, wake Hel on her throne, and fight waves of the dead that
grow stronger until every player has fallen or left. Requested on Discord (an "Infinite Arena"); the user chose Hel's
realm as its setting and ECR as its home. The hall's art is made in `..\ValheimAssets\Assets\Locations\HelHall` and
`Assets\Creatures\Hel`.

The entrance is decided (section 2): **Helgrind**, Hel's gate, a craftable endgame portal piece. Open, decided later
with the user (do not build): **rewards** (reward chest, leaderboard, any extra skill XP; until then arena creatures
drop nothing).

---

# 1. The hall

- **One hall per world**, at a fixed point high above the world centre: origin **(0, 5000, 0)**, unrotated, hall +Z
  toward the gate. At that height the game treats it as indoors (`Character.InInterior`), like its dungeons.
- **Always there, on every peer.** While `Arena` is on, every client and the dedicated server build the hall when the
  world loads, as local, non-networked scene objects (never ZDOs). A jump into the hall lands on a floor that already
  exists, so it needs no loading screen. When the setting turns off (server sync), the hall is removed; on, it is built.
- **Pieces** (bundles embedded in ECR, one per platform; prefabs in hall coordinates, origin at the floor centre):

  | Prefab | Where | What |
  | --- | --- | --- |
  | `ecr_helhall_floor` | origin | 52.5 m disc; 24 rune children `glow_rune_00` to `_23` (one shared emissive material) |
  | `ecr_helhall_walls` | origin | 7.5 m ring; gate at +Z with door PARTS `door_left`, `door_right`; four blind archways at 45/135/225/315 degrees |
  | `ecr_helhall_throne` | (0, 0, -14.731) | stepped dais and throne; ember glows `glow_brazier_0`, `_1` |
  | `ecr_helhall_ceiling` | origin | drum, dome, oculus glow `glow_oculus`, braided and hanging roots |
  | `ecr_hel` | the throne's `seat_root` | Hel, on the player's skeleton (scale on `Visual`) |

- **Points** (empty children of the pieces; read them from the built objects, never retype them): `spawn_0` to `_3`,
  `portal_0` to `_3`, `arrive`, `gate_inside`, `flame_0` to `_7` (walls); `seat_root`, `seat`, `spear_rest`,
  `spear_top`, `boss_stand`, `fire_0`, `_1` (throne); `light_oculus` (ceiling).
- **Materials:** stone pieces wear the game's building shader (`Custom/Piece`) borrowed from a stone piece's **body**
  renderer (never the snow overlay), tinted white, with the bundle's albedo and normal map; Hel wears the creature
  shader (`Custom/Creature`). Glow parts keep the bundle's own emissive material.
- **Colliders:** the bundles' `col_*` boxes, on `static_solid` (or the layer the bundle set), so players stand on the
  floor, walls and dome block them, and creatures path on the floor.
- **Light and mood:** the hall gets its own environment: a copy of the game's `Morkhalla` (cold blue) with its fog thinned
  so the far wall stays visible, through an environment zone round the hall. Blue fire (from the game's blue brazier) at
  the eight sconces and both throne braziers, a cold light at the oculus. Visuals only on clients.
- **The runes** in the floor light at random: one to three at a time fade in, hold one or two seconds, fade out. During a
  run more of them, faster as the waves rise; on a boss wave all of them flare. Client only.
- **Hel** sits on her throne, a still presence using the player's own throne-sitting animation (the player prefab's
  animator and avatar on her `Visual`, `attach_throne` set). Not a creature yet (a boss later). Hovering her shows
  "Hel" and, when no trial is running and the player is in the hall, `[E] Begin the trial`.
- **The gate doors** stand open between trials and close while one runs (on every client, from the run state).
- **No building** inside the hall: placing a piece within 40 m of the hall origin above 4900 m is refused with a
  message.

# 2. Getting in and out

- **Helgrind**, Hel's gate (piece `ECR_Helgrind`, "Helgrind": "Hel's gate, which leads to her hall and her trial"): a
  portal piece about the size of the game's wood portal (4.61 x 4.20 x 1.94 m, an arched opening 2.44 x 3.22 m), in the hammer's Misc tab while `Arena` is on, built at the **Black
  Forge** from **1 Sacrificial Blood** (`FrozenKingDrop`), **2 Frostcore**, **10 Bloodgold** (`Gold`) and **5 Memorial
  Coal**. Its art is `..\ValheimAssets\Assets\Props\HelGate\ecr_helgate` and the swirl `Assets\Effects\ecr_helgate_swirl`.
  - A copy of the game's `portal_stone` (placement, health, stone wear, sounds, its teleport trigger's box) **without
    its pairing**: no tag, no link with any other portal; the game's portal lists never see it.
  - **Walking into the opening** jumps you to the hall's `arrive` point at once (no loading screen) and keeps the gate's
    `exit` point (on the ground 2.25 m in front of it, facing away) as your way back. With `Arena` off or the hall not
    built, a centre message: "Hel's gate is closed." Carrying what the game will not let through a portal (ores, metals
    and the like), the game's own message, and you stay.
  - While `Arena` is on, its swirl turns in the opening with a cold blue-green light (#80ebe0, intensity 2, range 6)
    and the stone portal's hum; off, the gate stands dark but for its glowing Frostcores, coal and runes. Clients only.
  - With `Arena` off, Helgrind leaves the hammer; gates already built stay, closed.
- **The way out:** the wall that closes the hall's gate passage, behind `arrive`, carries Helgrind's swirl and light
  facing into the passage. Walk into it and you are jumped back, with the game's normal loading, to the gate you came
  in by (in front of it, facing away), or to where an admin stood for `elite arena`, or to your spawn point if nothing
  was kept.
- `elite arena` (admin, or devcommands in single player) stays: in the world, keeps where you stand and jumps you to
  `arrive`; in the hall, jumps you back as the way out does.
- Dying in the hall is the game's own death: your tombstone stays in the hall, skills drop as usual, and you go back in
  to fetch it.

# 3. The trial

- **Start:** a player in the hall interacts with Hel when no trial is running. Every player in the hall then is in it;
  players who arrive during it join it. A countdown of 10 seconds, the doors close, the first wave comes.
- **Waves** come out of the four archways (`spawn_0` to `_3`, in turn), each creature with a spawn effect, hunting the
  players at once. Never more than 20 arena creatures alive; the rest wait. The next wave comes when two or fewer are
  left, or after a wave timer (45 s, one second shorter each wave, never under 20 s).
- **Getting harder** (all fixed numbers in the code):
  - **Biome by wave:** waves 1-10 Meadows, 11-20 Black Forest, 21-30 Swamp, 31-40 Mountains, 41-50 Plains, 51-60
    Mistlands, 61-70 Ashlands, 71-80 Deep North; after 80 it stays in the Deep North and only the stars and mutations
    keep rising.
  - **Creatures:** each biome's hostile land creatures, read at runtime from the game's own spawn lists (so other mods'
    creatures can appear too), with no bosses, no water-only creatures and nothing passive.
  - **How many:** 4 in the first wave of a biome, one more per wave within it, a quarter more for each player past
    the first.
  - **Stars and mutations:** each creature rolls as ECR rolls any creature (`CreatureRoll.For`), plus extra stars
    (+1 at the 4th and +2 at the 8th wave of each biome, +1 more for every 10 waves after 80, up to ECR's maximum) and
    a mutation chance that rises from 5% by 3% a wave (at most 60%).
- **Bosses:** every 10th wave is a boss wave instead: the biome's boss alone on the rune circle (Eikthyr, the Elder,
  Bonemass, Moder, Yagluth, the Queen, Fader, Kall Fimbulbringer; after 80 the cycle starts again with more stars),
  rolling ECR's boss aspects as any boss does. The runes flare as it appears. The next wave comes when it dies.
- **Score:** each kill scores 10 x (stars + 1) x (biome number), a boss 500 x (biome number); the score, wave and
  creatures left show in the event bar under the minimap for every player in the hall:
  "Hel's Trial - wave 12 - 7 left - 3,420".
- **End:** when no player of the trial is alive in the hall (dead, left, logged out). The arena creatures left
  vanish; the doors open; every player of the trial reads "Hel's Trial ends at wave 14: 132 kills, score 18,420."
  `elite arena stop` (admin) ends it the same way.
- **Arena creatures drop nothing** (no loot, trophies or coins) until rewards are designed. Skills rise from the
  fighting as always.

# 4. Multiplayer

Built for a dedicated server first.

- **The trial lives in one networked object:** a hidden, persistent `ECR_ArenaCore` at the hall origin, placed by the
  server when the world has none. Its ZDO holds the trial: running or not, wave, score, kills, creatures left, the
  wave's spawn plan, end time of the wave timer (server clock), and the trial's id. Its owner runs the trial; if the
  owner leaves, the new owner carries on from the ZDO.
- **Starting** sends an RPC from the player's client to the core's owner.
- **Arena creatures** are tagged in their ZDO with the trial's id; a tagged creature with no running trial behind it
  vanishes when it loads. Their owners skip their drops.
- **Every client** draws the doors, the runes and the event bar from the core's ZDO.
- **Messages** (start, boss, end) go by RPC to the players of the trial only.

# 5. Configuration

One setting, synced from the server and lockable (the Charter clause, as raids):

- `12 - Arena` / `Arena` = `false`: the hall exists, Hel's Trial can be run and Helgrind is in the hammer. Off: no hall
  is built, Helgrind leaves the hammer and built gates are closed, `elite arena` says the arena is off, and arena
  creatures still in the world vanish.

Everything else is a fixed number in the code. Console commands: `elite arena`, `elite arena stop` (admin).

# 6. Decisions

Settled with the user on 2026-10-12:

1. The arena is in **Elite Creatures Reborn**, behind one setting `Arena`, off by default.
2. **Full arena** in the first build: the hall, Hel on her throne, endless waves, bosses, score.
3. **Normal death:** the tombstone stays in the hall.
4. **One hall, always loaded** on every peer, at a fixed point (not an instance per gate), so jumps in are instant.
5. **Rewards wait** for the user (see the top).
6. **The entrance is Helgrind** (decided 2026-10-12): a craftable endgame portal piece at the Black Forge (1 Sacrificial
   Blood, 2 Frostcore, 10 Bloodgold, 5 Memorial Coal), without the game's tag pairing; the way back is the wall at the
   end of the hall's gate passage, to the gate you came in by. `elite arena` stays for admins.

---

# Build checklist

`[ ]` not started, `[~]` partly, `[x]` built and seen working on a dedicated server.

## The hall (`Arena/Hall/`)
- [~] Bundles embedded, both platforms; prefabs loaded on every peer
- [~] Hall built at load when `Arena` is on, removed when it turns off; on the dedicated server too
- [~] Materials dressed (stone body material, creature material, glow kept); colliders on the right layer
- [~] Environment zone, blue fires, oculus light (clients only)
- [~] Runes lighting at random, more during a trial, all on a boss wave
- [~] Hel seated with the player's throne animation; hover and `[E] Begin the trial`
- [~] Doors open between trials, closed during one
- [~] Building refused in the hall

## The trial (`Arena/Trial/`)
- [~] `ECR_ArenaCore` registered and placed by the server; state in its ZDO; owner runs it
- [~] Start RPC from Hel; countdown; joiners
- [~] Waves: biome lists from the game's spawn lists, counts, stars, mutations, 20 alive, timers
- [~] Boss waves every 10th
- [~] Tagging, no drops, orphans vanish
- [~] Score, event bar line, messages
- [~] End conditions, cleanup, `elite arena stop`

## Getting in (`Arena/Travel/`, `Arena/Gate/`)
- [~] `elite arena` in and out, return point saved
- [~] Helgrind: bundles embedded, piece from `portal_stone` without its pairing, hammer and Black Forge while `Arena`
  is on, walk-in jump with the gate's exit kept, closed and no-teleport messages, swirl, light and hum on clients
- [~] The way out: swirl, light and trigger on the passage's back wall, back to the gate's exit

## Docs
- [~] `CLAUDE.md` Arena section, `console-commands.md`, `configuration.md` (the hall's part and the setting: done; the
  trial's part and both commands: done)

## Work log

- 2026-10-12, the hall: the four hall bundles and Hel's (`ecr_hel`) embedded for Windows and Linux (21.6 MB in all);
  the `Arena` setting bound and held by the server's charter; `Arena/Hall/` builds the hall on every peer from a host
  made in `ZNetScene.Awake`, removes and rebuilds it on the setting's change; stone in `Custom/Piece` from
  `stone_wall_4x2` (`New/stone`), Hel in `Custom/Creature` from the Draugr, glow parts their own; everything on
  `static_solid`; on clients the `ECR_HelHall` environment (Morkhalla, fog 0.015, not cold), blue fire and flickering
  lights from `MountainKit_brazier_blue` at the ten bowls (shadows on the throne's two), a cold spot in the oculus, the
  runes; Hel seated on the player's throne loop with her spear, her hover and `[E] Begin the trial`; the doors on every
  peer; building refused (the game's no-build status). Judgement calls: the hall's sky is not cold (no fire to warm
  by); the lights are tinted the runes' blue-green; Hel's hover reaches 3 m further than the game's. Builds clean;
  offline patch check passes; nothing seen in game. Code map and the lights in `CLAUDE.md`, "Arena: Hel's hall".
- 2026-10-12, the trial and the travel: `Arena/Trial/` and `Arena/Travel/`, `elite arena` and `elite arena stop`. The
  hidden persistent `ECR_ArenaCore` (added to `ZNetScene` in an `Awake` prefix on every peer; the server keeps exactly
  one at the origin) holds the trial in its ZDO (`ecr_arena_*`), its owner steps it twice a second; Hel's ask is an RPC
  to that owner. Creature lists from the game's spawn lists by each creature's home biome (the first a keyless entry
  names, else a single-biome keyed one), fixed fallback lists checked in game; bosses Eikthyr to FrozenKing, his later
  phases adopted as the wave's boss. Arena creatures tagged, rolled through the raids' `RaiderTraits.Roll` (a second
  overload: stars, mutation chance and barred mutations given; Gilded barred), hunting at once, allied by one group,
  dropping nothing (the drop list emptied last). Everything else that appears in the hall during a trial (a phase, a
  Summoner's adds, copies) is adopted on its owner; a tagged creature with no trial behind it vanishes. Raid files
  touched: `RaiderTraits.cs` (the overload; raids unchanged) and `RaidHudPatch.cs` (asks the arena's line first).
  Judgement calls: the boss comes on the rune circle at the floor's centre, not the dais's `boss_stand`; a wave lasts at
  least 5 s; the hall ends 5 s after its last living player; kinds tougher than the middle of their biome come less
  often (square root of the health ratio); an arena boss sets no world key, plays no dream, counts for neither `harder
  each kill` nor the boss hints; copies and a phase that brings the next score nothing; a tamed arena creature leaves
  the trial; `elite arena` lets a player out of the hall even with Arena off. Builds clean; offline patch check passes;
  nothing seen in game. Code map in `CLAUDE.md`, "Arena: Hel's Trial".
- 2026-10-12, the gate: Helgrind (`Arena/Gate/`). Bundles `ecr_helgate` and `ecr_helgatefx` embedded for Windows and
  Linux (2,139,736 bytes; the smaller revision the same day 1,114,847). `ECR_Helgrind` copied from `portal_stone` in a `ZNetScene.Awake` prefix on every peer; its
  `TeleportWorld` removed and its `TELEPORT` box's trigger swapped for ours; the swirl's particles and light removed,
  its hum kept under its `EffectFade`; the model, its mesh and floor colliders replaced by the gate's, dressed on
  clients in the hall's stone (`Custom/Piece` from `stone_wall_4x2`, gloss 0.15) with its Bloodgold metal mask at 0.76.
  In the hammer as the Raiders Chest is. Walking in: `ArenaTravel.Enter` with the gate's `exit` point, 1 m up, as the
  return point (now kept with its facing, `x,y,z,yaw`; an older `x,y,z` still reads). The way out: built from the hall
  by one call in `HallBuilder.Look`, 1.6 m behind `arrive` (the walls' back wall at r 29.5), the swirl's sill on the
  passage floor (centre 1.73 m up, read from the gate), its light 0.2475 m out, a 2.24 x 3.12 x 0.7 m trigger on
  `character_trigger`. Revised the same day to the smaller gate: the walk-in box moved to `portal_center` and sized
  (2.24, 3.12, 0.40), lights range 6, the points read from the gate's model. Judgement calls: the item rule is the game's
  own per-item one (`IsTeleportable(false)`), whatever the stone portal allows; the world's no-portals key does not
  close Helgrind (the way out comes back to the same gate, so it is no shortcut); the gate has a hover (name and open
  or closed). Builds clean; offline patch check passes; nothing seen in game. Code map in `CLAUDE.md`, "Arena:
  Helgrind".

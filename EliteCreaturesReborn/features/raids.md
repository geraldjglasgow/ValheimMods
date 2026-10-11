# Elite Creatures Reborn - specification: Raids

One feature of the mod, specified on its own. The game's own raids keep coming to bases as they always have. Beside
them, a **Raiders Chest**: players fill it with gold and sound a raid on their own base. The raid costs the gold, and the
raiders carry it: kill them all and it all comes back in their drops, with more. The more gold for how well geared the
players are, the harder the raid and the richer the drops. Raiders hunt players first, then the base: half
go for the Raiders Chest, half for the base's chests and then its crafting stations, breaking through walls to reach
them.

**Status: built, untested in game.** Every part is in the code (`Raids/`): the Raiders Chest, gear tier and heat, the
raid's state and clock on its host, the waves, the raiders' orders, coins and loot, every ending, the messages, the HUD
line, the setting and the two commands. Merged and reviewed on 2026-10-10; nothing has been seen working in game or on
a dedicated server yet, so the checklist holds no `[x]`.

---

# 1. How the game's raids work (what we build on)

From `assembly_valheim.dll` (RandEventSystem, RandomEvent, SpawnSystem, MonsterAI, Container):

- **The server decides.** On a timer it rolls a chance, then picks one raid from those the world qualifies for (boss
  keys) at a player standing in a base: at least 3 base pieces (workbench, fire, bed...) within 20 m
  (`EffectArea.GetBaseValue`).
- **A raid is a place and a clock**, 96 m across, with a start message, music and weather. Raiders trickle in from the
  normal spawner by chance, flagged as event creatures, and walk off when the clock runs out. There is no win and no
  reward beyond their own drops.
- **Raiders already break in.** `MonsterAI` attacks player pieces: it goes for a priority target
  (`FindClosestStaticPriorityTarget`), and when it is alerted and cannot reach anything it hits a piece within 10 m
  (`FindRandomStaticTarget`). That is the behaviour the raiders here steer, rather than a new one.
- **A destroyed container drops its contents** (`Container.OnDestroyed`).

**Base raids stay exactly the game's.** Nothing below changes them.

---

# 2. The Raiders Chest

A build piece with its **own model** (made in `../ValheimAssets`, never a copy of a game piece): an iron-bound chest with
a war horn on it, which sounds when a raid starts. Built near a workbench, in the hammer's Misc tab.

- **It holds only coins.** Open it and put gold in, like any chest.
- **Hover:**

  ```
  Raiders Chest
  1,000 coins - Deadly raid (coins back x2, drops x5)
  [E] Open   [Shift + E] Sound the raid
  ```

  The difficulty shown is worked out for the players near it now (section 3), so a group sees what they would get before
  they start.
- **Where:** only in a base (the game's base test at the chest). A raid needs a base to attack.
- **How often:** once per in-game day per chest; chests within 100 m share the cooldown.
- **One at a time:** no raid starts within 200 m of another raid, the game's or a chest's.

---

# 3. Gold against gear: the heat

A raid's **heat** is the gold in the chest divided by the **fair stake** for the players' gear.

**Gear tier** of a player: the higher of the best metal they have ever held (the game's known materials) and the tier of
the armour they wear. The raid uses the **strongest** player within 96 m of the chest, so a geared player cannot bring a
fresh character along to make the gold count for more.

| Tier | Gear | Fair stake |
| --- | --- | --- |
| 0 | Leather, bone, troll hide; no metal | 100 |
| 1 | Bronze | 200 |
| 2 | Iron | 400 |
| 3 | Silver, wolf | 600 |
| 4 | Black metal, padded | 900 |
| 5 | Mistlands (carapace, eitr) | 1,300 |
| 6 | Flametal | 1,800 |

**Heat decides both the fight and the pay:**

| Heat | Raid | Raiders | Extra stars | Mutated | Coins back | Drops |
| --- | --- | --- | --- | --- | --- | --- |
| under 0.5 | Trivial | fewer | 0 | as the biome rolls | x1 | x0.1 to x0.5 |
| 0.5 to 1.5 | Fair | normal | 0 | as the biome rolls | x1.1 | x1 |
| 1.5 to 3 | Hard | +50% | +1 | a third | x1.25 | x2 |
| 3 to 6 | Brutal | x2 | +2 | half | x1.5 | x3.5 |
| 6 and up | Deadly | x2.5 | +3 | nearly all (never Gilded) | x2 | x5 |

So a black metal player who puts in 100 coins (heat 0.11) gets a trivial raid: their 100 coins back and almost nothing
else. A player in leather who has never held bronze and puts in 1,000 coins (heat 10) gets a deadly raid: 2,000 coins
back and five times the raiders' loot. The numbers are judgement calls, here to be argued with.

## The gold

- **The raid costs it.** Sounding the raid takes every coin out of the chest.
- **The raiders carry it.** The stake times the heat's coins back is shared across every raider of every wave, the
  Warlord carrying a quarter. Each drops its share when it dies, however it dies (a player, a trap, another creature, a
  fall), except when the raid is stopped. Splintering copies carry none.
- **Guaranteed back on a win.** Kill every raider and every coin comes back, with the heat's extra; the raiders' own
  loot, times the heat's drops, is on top. A raider that leaves when a raid is lost takes its share with it: caught
  while it walks off, it drops only its own loot, unmultiplied.
- **Splintering copies** of a raider are raiders too, with no coins: they go for the base, die at a stop and walk off
  at a loss like the rest, and drop their own loot times the heat's drops. They are not counted in the waves, so the
  raid is won when the waves are beaten, and a copy still standing then walks off.

**Mid-raid:** heat is fixed when the raid starts. If a stronger player walks into the raid, the drops fall to what their
gear would have made of the gold, from then on; the coins carried and the fight stay as they were. Bringing in a geared
friend saves the base, not the loot.

---

# 4. The raid

1. **Sounding it.** The horn on the chest sounds; every player within 100 m reads "Raiders are coming for your gold!"
   with the difficulty, and a 20 second countdown starts. The coins leave the chest, and the chest stays locked until
   the raid ends.
2. **Who comes.** A raid the world has unlocked (the game's own boss rules), drawn at random and named in the message.
   It brings the creatures in that raid's own spawn list in the game (the Eikthyr army: boars and necks; the forest
   trolls: trolls; the Fulings: Fulings, berserkers and shamans...), so other mods' raids work too.
3. **Waves.** Three waves, each from one direction, 40 to 60 m from the chest on open ground, never in water. Each
   creature comes up to the game's own limit for it in that raid in the last wave, about half in the first, two thirds
   in the second, times the heat's raider count, and a quarter more for each player past the first. Never more than 20
   alive at once; the rest arrive as others die. 20 seconds between waves.
4. **Stars and mutations.** Raiders roll as any creature in the base's biome does (difficulty, world tier), plus the
   heat's extra stars and mutations. The last wave is led by a **Warlord**, the toughest creature in the list with two
   more stars again.
5. **Players see** one line under the minimap while within 96 m: "Fuling raid (Brutal) - wave 2 of 3 - 5 left - 8:40".

## What raiders go for

Every raider, in this order:

1. **Players.** A player it sees or hears, as any creature hunts. It turns back to the base only when it has no player.
2. **Its building target**, given when it spawns, alternately:
   - **Chest raiders** (half): the Raiders Chest.
   - **Plunderers** (half): the nearest chest of the base (any container piece), and when none is left within the
     raid's 96 m, the nearest crafting station or station upgrade.
3. **Whatever is in the way.** A raider that cannot reach its target breaks the wall, door or gate between, the game's
   own break-in, aimed at its target rather than at random.

A chest the plunderers break spills its contents on the ground, as the game does. A raider whose target is gone takes
the next one of its kind; a chest raider whose Raiders Chest is gone becomes a plunderer.

## How it ends

| End | When | Raiders left |
| --- | --- | --- |
| **Won** | The last wave is dead: every coin is back, with the extra | - |
| **Robbed** | The Raiders Chest is broken | Walk off and vanish with their coins |
| **Stopped** | A player stops it (below) | All die at once and drop nothing, coins included |
| **Abandoned** | No player within 96 m for 60 seconds, a logout or a server restart | Walk off and vanish with their coins |
| **Timed out** | 15 minutes from the start | Walk off and vanish with their coins |

Coins and loot already dropped by raiders killed before the end are the players' to keep in every case. Nothing else
is paid at the end.

**Stopping:** hold `E` on the Raiders Chest for 3 seconds while a raid is on, or an admin types `elite raid stop`. Every
raider dies where it stands, with its death effect, and drops nothing: the coins they carried are lost. It is a
surrender, so farming the easy first wave and then stopping costs most of the stake.

---

# 5. Multiplayer

Built for a dedicated server first, the workspace rule.

- **The raid lives in the chest's ZDO:** phase, wave, heat, the drop multiplier now, end time (server clock), the raid's
  name, raiders left, and the coins still to hand out to waves not yet spawned. The owner of the chest's ZDO runs the raid: it spawns the waves (as the Summoner aspect spawns its
  adds), counts the living raiders and decides how it ends. If ownership moves, the new owner carries on from the ZDO.
  Opening the chest is blocked during a raid, so the game's open-takes-ownership does not move it.
- **Gear tier is published by each player:** each client writes its own tier into its player's ZDO (the known materials
  and worn gear live only on that client); the chest's owner reads the players near it.
- **Raiders are tagged** with the chest's id and their role in their ZDO. Target choice runs on each raider's own owner,
  which may be a different client from the chest's. A raider that loads with no raid running behind it walks off.
- **Coins and drops:** each raider's coin share is written into its ZDO when it spawns, so whoever owns it when it dies
  drops exactly that; its loot is multiplied there too, from the multiplier in the chest's ZDO.
- **Stop:** the chest's owner sends one RPC to the owners of the tagged raiders; each kills its own with no drops.
- **Messages** go by RPC only to players within 100 m; the HUD line is drawn by each client from the chest's ZDO.
- **The piece** is registered on every peer (ECR is required on the server and every client).

---

# 6. Configuration

No rule file. One setting in the .cfg, synced from the server and lockable:

- `11 - Raids` / `Raiders Chest` = `true`: the chest is in the hammer. With `false` it is not, and a chest already built
  is an ordinary coin chest that cannot sound a raid.

Everything else is a fixed number in the code (the tables above, 3 waves, 20 s breaks, 40-60 m spawn ring, 15 minute
limit, 60 s empty to abandon, one raid per chest per in-game day, 20 alive at most).

Admin console commands (`console-commands.md`): `elite raid stop` (as the chest's stop), `elite raid start <coins>
[tier]` (a raid at your position with that stake, no chest, for testing).

---

# 7. Decisions

Settled with the user on 2026-10-09:

1. **No rule file.** One .cfg setting; everything else fixed.
2. **The chest has its own model**, not a copy of a game piece.
3. **Raiders attack players first, then the base**: half the Raiders Chest, half the chests and then the stations,
   breaking in to reach them.
4. **A raid costs its gold.** The raiders carry it, and a won raid guarantees it back in their drops, plus more.
5. **Stopping loses the gold** the remaining raiders carry; they die and drop nothing.
6. **The raid is random** among the unlocked ones.

Still open: only the numbers in the tables, which are judgement calls to tune in play.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## Game hooks to verify (decompile into the scratch folder)

- [~] `MonsterAI` static targets: setting a raider's target piece, and aiming the break-in at it (read in the decompiled game, built, untested)
- [~] The unlocked raids and their spawn lists (`RandEventSystem.m_events`, key checks, `m_spawn` max spawned) (built, untested)
- [~] The base test (`EffectArea.GetBaseValue`, gating the sounding); the walk-off is the raid's own - the game's flee from the host, not `MonsterAI.SetEventCreature`, whose flag sends a creature off whenever no game event runs (built, untested)
- [~] Known materials and worn armour for the gear tier (`Player.m_knownMaterial`, `Humanoid.SetupEquipment`); the robbery from the chest's `WearNTear.m_onDestroyed`, on its owner before its ZDO goes (built, untested)
- [~] Not starting the game's raid within 200 m of a chest raid (`RandEventSystem.GetValidEventPoints`; built, untested)

## The model

- [~] Raiders Chest model in `../ValheimAssets` (chest with a war horn, lid opening), built to the codex (`Assets/Props/RaidersChest`; not yet seen in game)
- [~] Tested in `AssetLab`; moved into ECR with BundlePrefabs (merged into ECR since 2026-10-10) in the release that ships it (moved in: bundle embedded, `Raids/Chest/`; not tested in AssetLab or in game)

## The raid (first, testable with `elite raid start`)

- [~] Gear tier published per player; heat and its table (built, untested)
- [~] Raid state in the chest's ZDO, run by its owner, carried on after an ownership change (built on the chest and the test marker, untested)
- [~] Waves from the raid's spawn list, sized by heat and players; 20 alive at most; Warlord (built, untested)
- [~] Spawn ring outside the base, one direction per wave, never in water (built, untested)
- [~] Targets: players first, chest raiders and plunderers, break-in toward the target (built, untested)
- [~] Coins taken at the start, shared across raiders (Warlord a quarter), dropped on any death but a stop; none from a raider caught walking off after a loss; Splintering copies tagged with none (built, untested)
- [~] Drops times the heat, lowered when a stronger player arrives (built, untested)
- [~] Every ending: won, robbed, stopped (all die, no drops, no coins), abandoned (also a dedicated server running the raid itself whose last player logs out), timed out (built, untested)
- [~] HUD line, messages and the horn to players within range only (built, untested)

## The chest

- [~] Coins only, locked during a raid, hover with the heat now, sound / stop (built, untested)
- [~] Cooldown per chest, shared within 100 m; base test; one raid within 200 m (built, untested; the base test gates sounding, not placing)

## Configuration and commands

- [~] `Raiders Chest` setting, synced and lockable; the hammer and the chest read it (built, untested)
- [~] `elite raid stop`, `elite raid start` (built, untested)

## Docs at release

- [~] `CLAUDE.md` raids section and console commands (written 2026-10-10); README line, changelog, wiki page at release

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-10-09 | Specified. Revised the same day: the War Horn and its rule file block replaced by the Raiders Chest (gold against gear sets difficulty and drops), raiders' targets, stopping a raid, one .cfg setting; base raids left as the game's. Then: a raid costs its gold, the raiders carry it and a win pays it back with more; a stop loses it; the raid is random. Nothing built. | - |
| 2026-10-10 | Foundation: `Raids/` - keys, the table and bands, gear tier published by each client and read by the host's owner, heat, the raid's state in the host's ZDO, the runner and its clock (countdown, waves, breaks, won, abandoned by absence, logout or server restart, timed out, stopped, robbed), presence reports from players the owner does not hold, the 200 m spacing, start and end messages to players within 100 m, the HUD line in the game's event bar, raider traits, the test marker (BundlePrefabs now merged), the `Raiders Chest` setting through Charter, `elite raid start/stop`. Stubs for raid-waves, raid-ai, raid-loot and raid-chest. Builds; untested in game. | - |
| 2026-10-10 | raid-loot: `Raids/Loot/` - a raider's coin share dropped as full stacks where it dies, however it dies, once, on its owner (nothing after a stop or with the stop's mark); its own loot times the raid's drops after the loot rules, random rounding below 1, cleared after a stop; the multiplier read from the host's ZDO, else the one the raider spawned with; drops lowered mid-raid when a stronger player arrives, never raised; the game's events kept 200 m from a running chest raid. Builds; untested in game. | - |
| 2026-10-10 | raid-waves: `Raids/Waves/` - the raid drawn from the game's events the world has unlocked (its global keys, or per player with player-based raids) and named after its commonest creature; three waves sized from the event's spawn list, heat, wave share and players, the last led by a Warlord of the toughest kind; one spot per wave on the 40-60 m ring (dry, outside the base, open sky, a new side, flat, clear way in); at most 20 alive, a few sent a tick; raiders levelled, rolled, alerted and tagged in the spawn frame; coins split as they spawn so exactly the coins back go out, the Warlord a quarter; the living counted from their ZDOIDs in the host's ZDO. Builds; untested in game. | - |
| 2026-10-10 | raid-chest: the Raiders Chest `ECR_RaidersChest` (`Raids/Chest/`): a copy of the reinforced chest wearing the workshop model (bundle embedded, lid turned on every client), in the hammer's Misc tab at a workbench for 10 wood, 4 leather scraps and 1 hard antler while the setting is on; coins only; hover with the heat now, why not, the raid and the hold; Shift + E sounds on the owner (base test, cooldown shared within 100 m, 200 m spacing; coins taken once the raid is on); locked during a raid; hold E 3 s to stop; robbed when broken; the horn (the lox's bellow, lowered). Builds; untested in game. | - |
| 2026-10-10 | raid-ai: `Raids/Orders/` - each tagged raider steered on its own owner, re-judged every 1.5 s from the host's ZDO: players first through the game's own hunt (its hunt-the-nearest-player and event-creature flags off, its own pick of priority pieces off), then chest raiders the Raiders Chest (plunderers on a test marker) and plunderers the nearest player-built chest of the base, then its stations, kept in the tag; the game's static target aimed at the piece on the line to the target when it is in reach or has no path. A lost raid walks raiders off (the game's flee, away from the host) to vanish with their coins once no player is within 40 m or after 30 s; a stop kills them where they stand (coin share -1 for raid-loot, Bloated, Splintering, Thieving and Gilded withheld). Orders by routed RPC to each raider's owner; a raider the order misses reads the end itself. Builds; untested in game. | - |
| 2026-10-10 | raid-review: the parts checked against each other, the spec and the game, and fixed: the horn went to every peer on the server, now only to the players within 100 m; a dedicated server running a raid itself (a base by the world's centre) never noticed its last player log out, its clock standing still - now abandoned; the test marker vanished the tick its raid ended, so raiders that missed a stop read a loss and walked off - it stays 10 s; a raider caught walking off after a loss paid its coins and multiplied loot, so a lost raid's passive walkers were free gold - now it leaves with its coins and drops its own loot unmultiplied ("nothing else is paid at the end"); a tamed raider held its wave open for good - it leaves the roster; Splintering copies take the raider tag with no coins, off the roster. Dead code dropped, summaries that named build agents rewritten, `CLAUDE.md` raids section and both console commands written. Open for the user: a biome roll can still make a raider Gilded. Builds; untested in game. | - |
| 2026-10-10 | lead (merging the parts): the raid is drawn among every unlocked raid wherever the base stands (decision 6), no longer preferring the base's biome; a stopped Thieving raider still drops what it stole (the players' own goods, never raid loot); raider rolls go through ECR's one creature roll, so a custom creature's fixed mutations hold in raids. Builds; untested in game. | - |

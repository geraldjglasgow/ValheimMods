# HighSeat - plan

Server-run creatures: the server simulates every creature near any player and pushes their state to everyone, so one
player's lag (a far-away player, a cloud PC) no longer freezes creatures for the whole group. Named after Hlidskjalf,
Odin's high seat that sees every world at once. First version written 2026-10-15 (milestone 2 below), built, never run in game.

Origin: a Discord report (Foetusbarbu, EU host, one player in the US, one on a Shadow cloud PC). In big fights and raids,
creatures froze for 20-30 seconds or moved "like they have ping" while players moved normally.

## How the game does it today (verified in assembly_valheim)

- `ZNetScene.CreateDestroyObjects` instantiates objects only around `ZNet.GetReferencePosition()`, one position. A
  dedicated server has no player, so it loads no creatures at all.
- `ZDOMan.ReleaseNearbyZDOS(refPos, uid)` runs per peer: a persistent ZDO with no owner, or whose owner's area no
  longer contains it, goes to that peer. A creature stays with its owner until the owner leaves the area.
- The owner runs the AI and physics and writes the ZDO; everyone else interpolates. Damage goes to the owner by RPC.
- Steam sockets are capped at 153600 bytes/s per connection (`ZSteamSocket.RegisterGlobalCallbacks`, SendRateMin/Max).

## Design

1. **Server loads every player's area.** On the server, `CreateDestroyObjects` uses the union of every peer's
   `m_refPos` (plus its own on a hosted game); `RemoveObjects` keeps anything inside any peer's area.
2. **Server owns creatures.** A `ReleaseNearbyZDOS` patch hands ZDOs whose prefab has `MonsterAI`/`AnimalAI` to the
   server's session id instead of the peer. Everything else (pieces, chests, doors, ships, carts, items) stays with
   players: interactions with those need no round trip.
3. **Players need nothing new to see it:** the game already syncs owned objects. Clients still run the mod, for the
   send rate and to never claim a creature (a client-side guard against `ClaimOwnership` on creatures).
4. **Bandwidth.** Raise the Steam send rate on the server and on clients (one setting, default about 4x the game's).
5. **CPU guard.** The game runs on one thread. If the server's frame time goes over budget, newly met creatures fall
   back to players (vanilla rules) until it recovers; creatures already held by the server stay put to avoid flip-flops.
6. **Headless needs.** On a dedicated server: animators set to always animate (ZSyncAnimation reads them), effects and
   audio skipped, pathfinding navmesh built around every peer (Pathfinding is built around the reference position
   today: patch it to cover all peer areas).

7. **Server spawns.** The ZDOs of `SpawnSystem` (one per zone), `CreatureSpawner` (spawn points, dungeons) and the
   event and raid spawners (the game's random events, ECR raids, Hel's Arena) go to the server, so it spawns and owns
   each creature from the start: no hand-over. Every roll that happens on spawn (ECR stars, mutations, aspects) then
   runs on the server with the server's rules, so a player's own config can never decide one.
8. **Building integrity (optional, its own setting, off by default).** `WearNTear` ZDOs of pieces in a player's area
   go to the server: support and damage are worked out there, so walls broken by creatures and raids look the same to
   everyone and big bases stop costing players' PCs. It adds server CPU, so the CPU guard covers it too.
9. **Stays with players:** the player's own character, the projectiles they fire, ships and carts they drive,
   chests and doors they open. Smelters, kilns, fermenters, beehives and plants stay vanilla: they catch up from
   timestamps when they load, so moving them gains nothing.

## Verify first (spikes, in this order)

1. A dedicated server can instantiate a creature far from its own position and run its AI: navmesh, ground, attack
   animations and hit detection all work headless.
2. Zones and terrain are loaded on the server around every peer (ZoneSystem, Heightmap colliders), or creatures fall
   through the world.
3. Ownership patch: a creature near two players stays on the server, and no client takes it back (Character, MonsterAI
   and tame/procreation code all call `ClaimOwnership` in places).
4. Frame time with four players spread across four biomes, about 100 creatures in total.
5. Hit feel at 150 ms ping: hits, staggers and knockback for the far player, and dodge and parry timing: a creature's
   attack is judged against the server's view of the player, so the far player may be hit after dodging.
6. Server spawns: wild spawns, spawn points and an ECR raid all spawn on the server and the creatures never change owner.

## Settings (one per feature)

- `Server runs creatures` (true/false, synced, server side).
- `Send rate KB/s` (the game's 150 by default; recommend 600).
- `Server frame budget ms` (the CPU guard's limit).
- `Server runs building integrity` (true/false, synced, off by default).

## Our mods

Every mod decides things on the creature's owner, so they work with the server as owner as long as the server runs
them (it already must). Check after spike 3: ECR (rolls, mutations, raids' host owner, Hel's Arena), EliteCreaturesPack
(mimic, custom AI), GrindstoneSkills Husbandry (taming, feeding: tame commands would go through the server).

## Decisions (user, 2026-10-15)

- Tamed creatures are server-run like any creature; follow and commands take the round trip.
- Dedicated servers and hosted games both: on a hosted game the host's PC runs every player's area (the CPU guard
  matters most there).
- Bosses are server-run like any creature.
- The name is HighSeat.

## Status (2026-10-15)

Written: ServerRun (what the server runs), Ownership (ReleaseNearbyZDOS), PlayerAreas (CreateDestroyObjects),
ZoneKeeping (UpdateTTL), AlwaysAnimate (BaseAI.Awake), FrameGuard, SendRate. Found while writing: no game code on a
player's machine claims a creature, so the server's ownership pass is enough and HighSeat is server only (no client
guard, no sync). Found in review: the game's SpawnSystem returns without a local player and takes raid spawns from the local player's
event, and SpawnArea checks the machine's own active area: ServerSpawning and ActiveArea make both work on the server.
Tested 2026-10-15 on the Ravenholt dedicated server, one player: creatures server-owned, moving, attacks landing. Not written: building integrity (design 8), lag compensation, the compatibility pass.

## Milestones

1. Spikes 1-3 with DevBridge on a local dedicated server plus two clients.
2. Ownership + area loading + send rate, behind the setting.
3. CPU guard, headless trims, compatibility pass over our mods.
4. Test with a real far-away player, then the standard layout (copy ShipConfig) and release.

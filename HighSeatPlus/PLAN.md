# HighSeatPlus - plan

Big servers: 100 players or more on one Valheim world. Two mods do it together. HighSeat (server only, players install
nothing) gets the server's half: the player cap, how the server sends the world to players, which creatures it runs.
HighSeatPlus (this mod, on players' PCs, optional) gets the players' half: big bases and crowds made cheap to draw and
run; a player without it plays as today. This plan covers both halves, because they are measured and released
together; `HighSeat/PLAN.md` points here. Faster jumps (kept places, kept portals) are Wayfare's: `Wayfare/PLAN.md`,
"Kept places". Started 2026-10-31: folder and plan only, nothing written.

## Why

The game caps a server at 10 players. Mods that raise the cap (ExpandedServer, up to 100) change only the cap and the
advertised capacity, not how the world is sent. On 2026-10-31 nobody had reported a stable count above about 20, both
ExpandedServer listings were deprecated and their repositories gone. Raising the cap is one line; the work is what
breaks after it.

## How the game does it today (verified in assembly_valheim, 2026-10-31)

- Player cap: `ZNet.RPC_PeerInfo` turns a peer away with error 9 ("server is full") once `GetNrOfPlayers() >= 10`
  (`ZNet.ServerPlayerLimit`).
- Sending the world: `ZDOMan.SendZDOToPeers2` serves one peer per server frame, in turn, then waits 0.05 s before the
  next round; `SendZDOs` fills at most about 10 KB per send (10240 bytes less the socket's queue). At 100 players a
  player is served once every 100 frames plus 0.05 s: about every 2 s at 50 frames a second, about 5 KB/s, whatever
  the Steam send rate. HighSeat's 500 KB/s does not lift this.
- What is sent: `ZDOMan.CreateSyncList` collects every ZDO in the peer's sectors (`FindSectorObjects` around its
  reference position), keeps those with a newer owner or data revision than the peer has (`ZDOPeer.ShouldSend`) and
  sorts them by distance (`ServerSortSendZDOS`). So the game does have interest management and sends only changed
  objects, but it re-checks every object in the area on every send, every unchanged building piece included.
- Force-send: `ZDOMan.ForceSendZDO(peer, id)` puts an object at the front of that player's next send. Wayfare's kept
  portals send far areas through it.
- Revisions: `ZDO.IncreaseDataRevision` is called from every setter; the owner revision changes in `SetOwner`.
- Building support: `WearNTearUpdater` calls `WearNTear.UpdateWear` on about 50 pieces a frame, adjusted so every piece
  comes round about once a second; on the piece's owner `UpdateSupport` runs an overlap box and raycasts each time,
  whether anything near the piece changed or not.
- Night skip: `Game.UpdateSleeping` (server only) skips the night only when `EverybodyIsTryingToSleep`: every player in
  bed.
- Raids: `RandEventSystem` runs one event at a time (`m_activeEvent`).
- World save: `ZDOMan.PrepareSave` copies every ZDO on the main thread.
- HighSeat's server builds the ground around a player one zone per player per pass, ten passes a second
  (`HighSeat/ZoneKeeping.cs`), and runs creatures only where the ground is in: about 2.5 s for a 5x5 area after a jump.

## Design

### HighSeat (server; players install nothing)

1. **Player cap.** The cap check takes the limit from a setting; error 9 stays for a full server.
2. **Send loop.** Every peer is served every round (0.05 s), each with its own byte budget from the send rate (500 KB/s
   is 25 KB a round), the socket queue check kept, the first peer rotating so no one always goes last. A time budget
   per frame stops a crowded round from stalling the server; the rest go next frame. The force-send list keeps working
   as the game's does (Wayfare uses it).
3. **Change-driven sync.** Instead of re-checking every ZDO in a peer's area, the server keeps the ZDOs changed since
   the last round (hooked where revisions change: `IncreaseDataRevision`, `SetOwner`, new, destroyed and received ZDOs)
   and sends each peer the changed ones near it. A sector that comes into a peer's area is checked in full once.
   Unchanged pieces then cost the server nothing.
4. **Update rate by distance.** Near objects every round, farther ones every few rounds. A player in a crowd of 100
   gets the players beside them often and the far side of the town less often.
5. **Creatures only where shared.** HighSeat exists because one player's lag froze creatures for everyone near them;
   that only happens when two or more players are near a creature. Near one player only, the creature goes to that
   player as the game does (no round trip for their hits); near two or more, to the server. A creature stays with the
   server until it has had one player near it for some seconds, so it does not flip. Spawners stay on the server, so
   every roll on spawn follows the server's rules. The server then runs only the shared areas, not all 100 players'.
6. **Night skip for a crowd.** A vote, not every player in bed: lying in bed or saying `!sleep` in chat is a yes, and
   the night passes once more than half of the players online say yes. Only the players in bed get the sleep screen;
   the server tells everyone the count with the game's own messages, so players need nothing installed.
7. **Kept places on the server.** The server keeps the ground and still objects of the busiest places (time spent
   there by every player, fading) and of fresh death spots (a new tombstone), creatures unloaded as today, so a player
   arriving there has creatures running at once instead of after the ground is built. The ground follows the rules of
   Wayfare's kept places (poked, let go only after its instances, never a terrain compiler without ground). No new
   messages: the server picks its places from what it already sees.

### HighSeatPlus (players' PCs; optional)

8. **Support checked on change.** A piece whose neighbourhood has not changed keeps its support value and skips
   `UpdateSupport`; a piece placed, removed or damaged nearby, or a terrain edit under it, marks the pieces within reach
   to check again. Same results as the game's, fewer checks. Rain wear and the rest of `UpdateWear` stay as they are.
   If HighSeat later runs building integrity on the server (its design 8), the server needs the same cache: then the
   code moves into a library in `ValheimModLibs`, used by both.
9. **Combined building meshes.** A settled building is drawn as a few combined meshes per material instead of one
   renderer per piece, split back when a piece in it changes (placed, removed, its worn or broken look). Colliders and
   ZDOs stay untouched, so the hammer, repair, comfort, roof checks, wards and other mods see what they see today.
10. **Crowds.** Players beyond the nearest few are drawn simpler (no cloth, a lower animation rate), never hidden.
    Party members and anyone near enough to fight are always drawn in full.
11. **Baking, last and only if the numbers ask for it.** The structural pieces of a settled room stored as one ZDO
    (each piece's prefab, position, rotation and health inside), one per room, so a hit resends a room and not a base.
    It needs the server and every player, an uninstall must never lose a base, and it hides pieces from everything that
    looks for them (OpenKeep's Copy building, comfort, wards, snap points). Only if, after 2 to 10, the number of ZDOs
    still limits the server (save time, loading, memory).

### Load testing (DevBridge, internal)

12. **Fake players.** DevBridge on the server adds N peers whose socket discards what it is sent and counts the bytes,
    each with a reference position that walks a path, spread across the map or crowded on one spot. Measured: the
    server's frame time, the time spent in sync and bytes per player, at 10, 25, 50 and 100.
13. **Fake characters.** Each fake player also owns a character ZDO that moves, so a real client sees a crowd and its
    frame rate can be measured.

## Verify first (spikes, in this order)

1. The server accepts a fake peer (its own `ISocket`, no connection), and the vanilla numbers at 10, 25, 50 and 100,
   spread and crowded: what breaks first.
2. Every way a ZDO changes passes through a point a patch can hook (design 3).
3. Players' PCs: frame time on a big base (about 5,000 pieces) split into support checks, rendering and scripts, before
   choosing between designs 8, 9 and 11.
4. A client's frame rate with 30, 60 and 100 fake characters around it.
5. Crossplay: how many PlayFab players can join. The IronLabs ExpandedServer page warns PlayFab Party may cap lower
   than the game's own limit; fake peers cannot test this.

## Settings

One on/off setting per feature; counts, distances and times are constants in the code.

- HighSeat, section `2 - Big servers` (server only, not synced): `Max players` (50), `Creatures only where shared`
  (off), `Night skip vote` (on), `Kept places` (on). The send loop, change-driven sync and update rates have no setting.
- HighSeatPlus, unsynced (each player decides): `Combine building meshes` (on), `Simpler far players` (on). Support
  checking on change has no setting: it changes no result.

## Decisions (user, 2026-10-31)

- The goal is 100 players or more.
- The players' half is its own mod, HighSeatPlus; the server half goes into HighSeat (option A). Players still install
  nothing for HighSeat; HighSeatPlus is optional.
- Kept places and kept portals are not here or in HighSeat: "the portal loading thing shouldn't be in highseat or
  highseatplus, maybe wayfare". They are planned in `Wayfare/PLAN.md`, "Kept places".
- `Max players` defaults to 50.
- `Creatures only where shared` is off by default: the server runs every creature near any player, as 0.2.3.
- The night is skipped by a vote, not a share in bed: lying in bed or saying `!sleep` in chat is a yes, and more than
  half of the players online skips the night.
- Far players are drawn simpler (no cloth, fewer animation updates) but never hidden, so PvP stays fair.
- Baking (design 11) is not built for now; raids stay one at a time; PlayFab (crossplay) limits stay as the game sets
  them until spike 5 is tested.

## Open questions

- Raids: more than one at a time with 100 players spread out? The game runs one, and players' PCs show one; it would
  need HighSeatPlus too.
- Lockstep's whole-group rule (the next boss only after everyone beat the last) does not suit 100 players: a setting
  there, or leave it to server owners?
- `Creatures only where shared` on by default, once measured?

## Status (2026-10-31)

Designs 1 to 10 and 12 to 13 written on 2026-10-31, built and checked offline (every Harmony target against the game
DLL, the size rules), never run in game and nothing measured; baking (11) not written. Nothing released.

- HighSeat (`HighSeat/HighSeat`): `PlayerCap/` (a transpiler on the cap check in `ZNet.RPC_PeerInfo`; the dedicated
  server's own build was not available to check its IL; the Steam lobby limit); `Sync/` (the send loop, change-driven
  sync and update rates, switch `Big server sync`); `Shared/` (creatures only where shared: a lone player's area keeps
  only its ground, spawners, creatures and base markers on the server, a fresh spawn goes to the lone player three
  frames after it was made); `NightSkip/` (the vote, chat heard on the server, sleep sent only to players in bed);
  `KeptPlaces/` (four busiest places, death spots for 10 minutes).
- HighSeatPlus: `Support/` (the game already caches most support checks; only pieces held by the ground or by rock
  and trees ran the full search every second, and those now skip it until something near them changes), `Meshes/`
  (16 m cells per material, the pieces' renderers hidden with forceRenderingOff and shown again the frame a piece
  changes; capped at 3M vertices), `Crowd/` (capes stilled, far animators stepped ten times a second; party members from
  Party's API).
- Combined meshes need measuring before release: the game's piece materials are already GPU-instanced (1008 of 1076
  hammer material slots), so combining cuts the renderers Unity culls about 4x but may not cut draw calls, and costs
  about 23 KB of video memory per combined piece (about 120 MB for a 6,000-piece base).
- DevBridge: `/load` (fake players, their bodies, `measure=`), in `DevBridge/REFERENCE.md`.

Found while building: a boss's one-per-player drop counts every player online (`CharacterDrop`, capped at 100); the
user chose players within 100 m instead (`PerPlayerDrops`, switch `Boss drops for players nearby`). The game sends the whole player list to every peer every 2 s (about 100 bytes a player). The game's own crossplay
(PlayFab) lobby and network stay at 10 players.

## After 0.3.0 (2026-10-31, unreleased, in the working tree)

Measured with DevBridge's fake players on a hosted world, then improved: crowd ranks and a server-wide upload ceiling;
packages written straight to the socket (no ZRpc copy; Steam send without the managed copy; broadcasts written once;
Jotunn's buffering prefix allowed beside them); round start spread over frames; held-back objects cost one lookup;
per-player preparation on 4 worker threads; deltas for players with HighSeatPlus (proven against the server with
DevBridge's verifying fakes: 311,384 objects, 0 mismatches); ownership and ground passes once per zone; objects
created to a time budget; player history sent with places kept (piece creatorIndex) and the 2 s list rebuild without
garbage; quiet server creatures skip bone writes; records written without ZDO.Serialize's garbage, made ahead on the
worker threads. Numbers so far (100 fakes crowded): 0.4 updates/s per player with the game's loop, 7.1 with the new
loop and deltas at 35 KB/s each; the 100-player join went from 36 to 41 fps before the last batch. Also fixed for
crowds: EliteEquipment's first fitted-leggings build (prepared behind the loading screen) and Broadside's first look
of newly loaded ships (spread over frames, cuts off the main thread). Still to run: the full check of the last batch
(correctness, the join profile, 10 and 25 players spread with quiet animation on and off), then a real Steam player.

## Milestones

1. The load harness in DevBridge (fake players and characters); vanilla numbers at 10, 25, 50 and 100.
2. HighSeat: player cap and send loop; numbers again.
3. HighSeat: change-driven sync, update rate by distance, creatures only where shared; numbers again.
4. HighSeatPlus: measure a big base and a crowd, then support on change, combined meshes and crowd drawing as the
   numbers say.
5. HighSeat: kept places on the server.
6. Night skip, a test with as many real players as can be gathered, then both released.
7. Baking, only if the numbers still call for it.

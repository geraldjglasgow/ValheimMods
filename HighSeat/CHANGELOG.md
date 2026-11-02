# Changelog

## 0.6.0

- `Open server`: a dedicated server asks nobody for a password, whatever password it was started with.
- A dedicated server shows its real player limit in the server list (0/50, not 0/10).
- A public dedicated server started without a password now starts instead of quitting.
- Fixed: the faster player history update failed on dedicated servers and fell back to the game's own code.

## 0.5.0

- `Creatures only where shared` is removed and always on: the server runs spawners and every creature players share; a creature near one player runs on that player's game.
- Much lighter with players spread out: a lone player's area loads only what the server needs there (50 spread out: about 24 server fps instead of 4).
- HighSeat's own statistics are only counted in debug builds.

## 0.4.3

- The faster send and player list paths now check the game version instead of reading the game's code; they stay off on any game version other than 1.0.21.

## 0.4.2

- Fixed: many players at the edge of explored land no longer freeze the server while new land generates.
- Smoother with players spread out: far players' areas are gathered less often, and waiting objects no longer slow every pass.
- Fixed: players leaving no longer cause one long frame; their areas unload a little at a time.

## 0.4.1

- Fixed: players spread over the world no longer freeze the server for 30-40 s while their ground loads.
- Fixed: a far player leaving a hosted game no longer unloads the host's area and leaves the host on a loading screen.
- No more short stutter every 2 s with many players: the ownership pass is spread over frames.

## 0.4.0

- Players with HighSeatPlus get only what changed in each object: about a fifth of the data in a crowd.
- Sends are prepared on several threads, with less garbage.
- Crowds: the nearest players update every round, the rest of a crowd less often.
- Smoother joins: objects are made to a time budget.
- Quiet creatures on the server skip bone animation until they fight or a player comes near.
- Joining stays under Steam's message limit however long the player history grows.
- Fixed: no frame budget warning while a host loads the world; the player cap is logged once.

## 0.3.0

- `Max players`: the server takes 50 players by default instead of the game's 10.
- `Big server sync`: every player is sent only what changed, 20 times a second, far things less often.
- `Night skip vote`: a bed or `!sleep` in chat is a vote; more than half online skips the night.
- `Kept places`: busy places and fresh death spots stay loaded on the server.
- `Creatures only where shared` (off by default): a creature near one player only runs on that player's game.
- `Boss drops for players nearby`: once-per-player boss items count players near the fight, not everyone online.

## 0.2.3

- Fixed: a world save or one slow frame no longer makes the server leave new creatures to players.

## 0.2.2

- Fixed: wild creatures spawn near players again; the server's spawner threw errors instead.

## 0.2.1

- Fixed: a dedicated server running 0.2.0 never accepted players (connections timed out).

## 0.2.0

- Fixed: creatures near players no longer float over dug-out ground or sink into raised ground.
- Send rate is now 500 KB/s with no setting, never lowered below what another mod set; `Send rate KB/s` removed.

## 0.1.0

- First release.
- The server runs every creature near any player, tamed creatures and bosses included.
- Wild spawns, the game's raids, nests, spawn points and boss altars spawn on the server.
- `Server frame budget ms`: a busy server leaves new creatures to players until it recovers.
- `Send rate KB/s`: optional higher send rate.

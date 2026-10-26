# Changelog

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

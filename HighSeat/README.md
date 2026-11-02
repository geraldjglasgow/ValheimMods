# HighSeat

In Valheim the nearest player's game runs every creature, and everyone else sees it through that player's connection.
One player far away, on a slow line or on a cloud PC, and creatures freeze or move "like they have ping" for the whole
group. HighSeat makes the server run every creature players share, the way most survival games do, and lets one server hold a crowd:
50 players by default instead of the game's 10.

## Features

- Server-run creatures: the server runs every creature two or more players share, monsters, animals, tamed creatures
  and bosses alike, so nobody's connection decides what the group sees. A player alone runs their own, as in the game,
  which keeps the server light when players spread out.
- Server-run spawners: wild spawns, the game's raids, nests, spawn points and boss altars spawn on the server, so every
  roll on spawn follows the server's rules. Only a console spawn begins on a player's game, handed over within two
  seconds.
- Big servers: up to 50 players by default, and every player is sent the world 20 times a second with their own share
  of the send rate. Only what changed is sent, and the far side of a crowded town less often than the players beside
  you; players with HighSeatPlus get only the changed parts of each object, about a fifth of the data in a crowd.
- Night skip vote: lying in bed or saying `!sleep` in chat is a vote, and more than half of the players online skips
  the night. Only those in bed get the sleep screen; everyone sees the count.
- Kept places: the busiest places and fresh death spots stay loaded on the server, so creatures there start at once
  when a player arrives.
- Boss drops for players nearby: a boss's once-per-player item drops for the players near the fight, not for everyone
  online.
- Open server: a dedicated server can run without a password, whatever its host panel passes, and lists its real
  player limit.
- Frame budget: a server that falls behind leaves new creatures to players, as the game does, until it recovers.
- Send rate: raised to 500 KB/s per player from the game's 150, never lowered below what another mod set.

## Install

Needed on the server only: a dedicated server, or the player who hosts. Players need nothing; HighSeatPlus, optional on
their PCs, gets them smaller updates and makes big bases and crowds cheaper to draw. Install with r2modman or the Thunderstore app, or put
`HighSeat.dll` in `BepInEx/plugins`. A network mod that sets a higher send rate (such as Better Networking) can stay:
HighSeat keeps the higher one.

The server does more work: give it a good CPU and plenty of memory, especially for big groups.
A crossplay server stays at the game's 10 players.

## Configuration

`BepInEx/config/com.HighSeat.cfg`: `Server runs creatures`, `Server frame budget ms`, and under Big servers
`Max players`, `Big server sync`, `Open server`, `Night skip vote`, `Kept places` and `Boss drops for players nearby`. All are described in the file and apply without a restart.

## Links

Discord: https://discord.gg/Q2sMPGQ4T3

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs

- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

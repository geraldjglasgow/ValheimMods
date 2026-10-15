# HighSeat

In Valheim the nearest player's game runs every creature, and everyone else sees it through that player's connection.
One player far away, on a slow line or on a cloud PC, and creatures freeze or move "like they have ping" for the whole
group. HighSeat makes the server run them instead, the way most survival games do.

## Features

- Server-run creatures: the server loads every player's area and runs every creature in it, monsters, animals, tamed
  creatures and bosses alike.
- Server-run spawners: wild spawns, the game's raids, nests, spawn points and boss altars spawn on the server, so a
  creature starts there and never changes hands. Only a console spawn begins on a player's game, handed over within
  two seconds.
- Frame budget: a server that falls behind leaves new creatures to players, as the game does, until it recovers.
- Send rate: raised to 500 KB/s per player from the game's 150, never lowered below what another mod set.

## Install

Needed on the server only: a dedicated server, or the player who hosts. Players need nothing. Install with r2modman
or the Thunderstore app, or put `HighSeat.dll` in `BepInEx/plugins`. A network mod that sets a higher send rate (such as
Better Networking) can stay: HighSeat keeps the higher one.

The server does more work: give it a good CPU, especially when players spread out across the map.

## Configuration

`BepInEx/config/com.HighSeat.cfg`: `Server runs creatures`, `Server frame budget ms`. Both are described in the file and
apply without a restart.

## Links

Discord: https://discord.gg/Q2sMPGQ4T3

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs

- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

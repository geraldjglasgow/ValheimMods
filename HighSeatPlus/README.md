# HighSeatPlus

The players' half of HighSeat, for big servers. HighSeat makes the server hold a big group; HighSeatPlus makes big bases
and crowds cheaper to draw and run on each player's PC. It changes only how your own PC does the work: what you and
everyone else see, and every building's support, stay exactly the game's.

## Features

- Building support on change: pieces standing on the ground or leaning on rock and trees search for their support
  again only when something near them changes (a piece placed, removed or damaged, the ground dug), not every second.
- Combined building meshes: a settled building is drawn as a few combined meshes instead of one per piece, split back
  out the moment a piece in it changes.
- Simpler far players: players beyond the nearest ten and out of fighting range are drawn with their capes still and
  fewer animation updates. Nobody is ever hidden, so PvP stays fair, and party members (with the Party mod) are always
  drawn in full.

## Install

Optional, on players' PCs. Made for servers running HighSeat, and works on any server; a player without it plays as
today. Install with r2modman or the Thunderstore app, or put `HighSeatPlus.dll` in `BepInEx/plugins`.

## Configuration

`BepInEx/config/com.HighSeatPlus.cfg`: `Combine building meshes` and `Simpler far players`, each your own choice and
described in the file. Support on change has no setting: it gives the game's own results.

## Links

Discord: https://discord.gg/Q2sMPGQ4T3

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs

- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

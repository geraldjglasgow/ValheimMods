# Getting Started

ShipConfig sets every ship's health, sail and paddle force, steering, drag, damage taken, weather wear and build cost,
per ship and with global multipliers on top. Ships from other mods get settings too. Changes apply while you play, to
new ships and ships already on the water.

This wiki describes version 1.3.1.

## Install

| Setup | Install on |
| --- | --- |
| Single player | your game |
| Hosting a world from your game | your game and every player who joins |
| Dedicated server | the server and every player |

Everyone needs the same version, or the player cannot join. Use r2modman or the Thunderstore app, or put
`ShipConfig.dll` in `BepInEx/plugins`. Needs BepInExPack for Valheim.

## The config file

`BepInEx/config/com.ShipConfig.cfg`, created on first start; every setting is described in it. Sections: `General`
(`Lock Configuration`), `Multipliers` (six global factors) and `Ship` (every setting of every ship, keyed
`<Ship>.<Setting>`, e.g. `VikingShip.Health`). Save it and the change applies within a few seconds, no restart. On a
server, the server's file applies to everyone while `Lock Configuration` is on (the default).

## Quick start

| To | Set |
| --- | --- |
| Make the Longship tougher | `VikingShip.Health = 2000` |
| Make every ship sail faster | `Sail Force Multiplier = 1.5` |
| Halve the damage every ship takes | `Damage Taken Multiplier = 0.5` |
| Make the Karve heel and capsize less under sail | lower `Karve.SailForceOffset`, e.g. to `0.5` |
| Make the Raft cheaper to build | `Raft.BuildCost = 0.5` |
| Stop a ship taking any damage | `Karve.Invulnerable = true` |
| Let the Longship sail the Ashlands ocean unharmed | `VikingShip.AshlandsOceanDamage = false` (skips the Drakkar's progression gate) |

## Pages

- [Ship Settings](wiki:Ship Settings) - every setting, what it does, its vanilla value
- [Multiplayer and Commands](wiki:Multiplayer and Commands) - servers, admins, the version check, other mods, the
  `charter` command

## Help

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues - name the mod and its version. Source:
https://github.com/geraldjglasgow/ValheimMods (GPL-3.0).

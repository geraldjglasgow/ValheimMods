# Getting Started

FeastMaster lets you configure every food and mead in Valheim, how health, stamina and eitr regenerate, every stamina
cost, cooking, brewing and feasts, and a few HUD options. Every setting starts at the game's behaviour, and while a
setting is at its default FeastMaster leaves that part of the game (and other mods) alone.

This wiki describes version 4.7.4.

## Install

- With r2modman or the Thunderstore app. By hand: install BepInExPack for Valheim, then put `FeastMaster.dll` in
  `BepInEx/plugins`.
- Multiplayer: on the dedicated server and every player's game, same version. Single player: your own game.

## The config file

`BepInEx/config/com.FeastMaster.cfg` (r2modman: the profile's Config editor). On a dedicated server edit the server's
copy; its values apply to every player.

- The per-food, per-mead, per-station and feast sections appear once a world has loaded, so load a world once to get
  the complete file. The numbered sections come first, then the item sections by name.
- The file describes every setting. Changes apply without a restart; a value out of range is brought back into it.

| Section | Covers | Page |
| --- | --- | --- |
| `0. Global Settings`, one per food and mead | food multipliers, degradation, eating again, food slots, auto eat, each item's values | [Foods and Meads](wiki:Foods and Meads) |
| `1.` to `3.` Regeneration | health, stamina and eitr regeneration, Vigor, Rested | [Regeneration and Stamina](wiki:Regeneration and Stamina) |
| `4.` to `7.` | stamina costs, drowning, base health and stamina, skills, world rates | [Regeneration and Stamina](wiki:Regeneration and Stamina) |
| `8. Display` | each player's HUD options and auto eat | [Foods and Meads](wiki:Foods and Meads) |
| `9. Kitchen`, one per cooking station | cook times, burning, fermenter, feasts | [Kitchen and Feasts](wiki:Kitchen and Feasts) |
| `Lock Configuration` | server binding, console command, other mods | [Multiplayer and Compatibility](wiki:Multiplayer and Compatibility) |

## Quick start

| You want | Set |
| --- | --- |
| Food that lasts twice as long | `Duration Modifier = 2` in `0. Global Settings` |
| Food that never fades | `Disable Food Degradation = true` in `0. Global Settings` |
| Four foods at once | `Food Slots = 4` in `0. Global Settings` |
| One food changed | its own section, for example `Health = 50` under `[CookedMeat]` |
| Faster stamina regeneration from food | `Vigor Per Stamina Point = 0.1` in `2. Stamina Regeneration` |
| Cheaper running | `Run Cost = 0.5` in `4. Stamina Costs` |
| Faster cooking | `Cook Time Multiplier = 0.5` in `9. Kitchen` |
| Shorter brewing | `Fermentation Time = 600` (ten minutes) in `9. Kitchen` |
| No stamina number on your HUD | `Hide Stamina Number = true` in `8. Display` |

## Upgrading from older versions

Renamed entries keep their values on the first start (if the new entry is already in the file, it wins).

| Old | New | Since |
| --- | --- | --- |
| `[General]` `Lock Configuration` | `[0. Global Settings]` `Lock Configuration` | 4.0.0 |
| `[0Meads_<prefab>]` | `[<prefab>]`, same keys | 4.0.0 |
| `Steady Regeneration` | `Continuous Food Healing` | 4.1.0 |
| `Extra Stamina From Food Only` | `Count Food Stamina Only` | 4.1.0 |
| `Regen Per 10 Extra Stamina` | `Regen Per Extra Stamina Point`, divided by 10 | 4.1.0 |
| `[9. Fermenter]` | `[9. Kitchen]`, same keys | 4.3.0 |

## Links

- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version, list your other
  mods, attach `BepInEx/LogOutput.log`).
- Source: https://github.com/geraldjglasgow/ValheimMods. Licence: GPL-3.0.

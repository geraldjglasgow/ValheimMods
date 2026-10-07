# Getting Started

OpenKeep is storage for Valheim in one mod: craft, build and feed stations from nearby chests; quick stack, sort, route and trash; salvage, batch crafting and craft speed; recipe search, favourite recipes, grid views and a recipe tracker; stack sizes, weights, chest sizes, station capacities and contents signs. Its Homestead section adds base tweaks: a choice of bed after death, quicker respawns, campfires on wood, honey rate, fires, smelters and pets fed from chests, night-only torches, quicker Rested and repairs. A build camera lets you build from a free camera near a crafting station.

This wiki describes version 2.1.0.

## Install

- Install with r2modman or the Thunderstore app, or put `OpenKeep.dll` in `BepInEx/plugins`. Needs BepInExPack for Valheim.
- Multiplayer: install it on the server and every client, same version.

Most features are on by default. Off by default: cart workbenches, contents signs, ground pickup, shared chests and the personal chest.

## Configuration

All files are in `BepInEx/config`, written on the first start. Edits apply without a restart.

| File | What it holds |
| --- | --- |
| `milkyteam.openkeep.cfg` | Every setting, each described in the file. |
| `OpenKeep.Reach.yml` | Which containers OpenKeep may use; what stations may take from them. |
| `OpenKeep.Salvage.yml` | Items never salvaged; return fractions. |
| `OpenKeep.Store.yml` | Item groups; rules for quick stack, routing and ground pickup. |
| `OpenKeep.Stacks.yml` | Stack sizes and weights. |
| `OpenKeep.Containers.yml` | Container sizes. |
| `OpenKeep.Stations.yml` | Item and fuel capacity of smelter-type stations. |
| `OpenKeep.Signs.yml` | Which containers get a contents sign, and where. |

- After a world loads, `OpenKeep.Items.txt`, `OpenKeep.Containers.txt` and `OpenKeep.Stations.txt` list every prefab name the YAML files need.
- A YAML file with an error is ignored and the previous rules stay; the log names the bad entry.
- With a configuration manager, each YAML file has an `Edit ...` entry.
- **Server** settings come from the server in multiplayer. **Player** settings (keys, display, colours, sort order) are always your own.

### Item names in YAML

| Entry | Matches |
| --- | --- |
| `Wood` | That prefab name (any case). |
| `$item_wood` | That name token. |
| `prefix:Trophy` / `suffix:Ore` | Prefab names starting / ending with the text. |
| `type:Material` | That game item type (`Material`, `Consumable`, `Trophy`, `Ammo`, `Fish`...). |
| `group:Ores` | A group from the file's `groups:` list. |
| `*` | Everything. |

### Keys

Every key can be rebound in the cfg, main key first (`R + LeftAlt`); empty switches it off. Keys never fire while you type, inventory keys work only with the inventory open, and a single key such as `F` does not fire while Shift, Ctrl or Alt is held. Each page lists its keys.

## Pages

- [Crafting and Salvage](wiki:Crafting and Salvage) - crafting from chests, station feeding, cart workbench, batch crafting, craft speed, recipe search and favourites, recipe tracker, salvage.
- [Store and Sort](wiki:Store and Sort) - quick stack, store, top up, route, favourites, trash, sorting, ground pickup.
- [Stacks and Containers](wiki:Stacks and Containers) - stack sizes, weights, chest sizes, station capacities, contents signs.
- [Homestead](wiki:Homestead) - beds and respawn, fires, torches, honey, self-feeding stations, pets, Rested, repairs.
- [Multiplayer and Commands](wiki:Multiplayer and Commands) - server settings, shared chests, other mods, console commands.
- [Build Camera](wiki:Build Camera) - build, remove and repair from a free camera near a crafting station.

## Links

- Source: https://github.com/geraldjglasgow/ValheimMods
- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version)
- Licence: GPL-3.0

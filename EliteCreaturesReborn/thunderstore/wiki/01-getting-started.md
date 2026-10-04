# Getting Started

Creatures roll more stars than the game's two, and some carry a **mutation** that changes the fight. Bosses take an
**aspect** instead, shown at their altar. Everything gets harder as bosses fall, and harder fights pay more loot.

This wiki describes version 3.15.0.

## Install

- Install on the server **and** every client, same version. Mismatched players are turned away at the join screen.
- Remove any other creature star or level mod first; they do not work together.
- Existing worlds are fine: bosses already killed count at once.

## Settings files

Both are in `BepInEx/config`, created on first run; edits apply without a restart.

| File | Holds |
| --- | --- |
| `creature_rules.yml` | Every gameplay rule. On a server, the server's copy applies to everyone while `lock to server: true` (default) |
| `gglasgow.elitecreaturesreborn.cfg` | Each player's own display preferences |

## Other mods

| Mod | Together |
| --- | --- |
| PackPanel | World tier shown on the inventory and under the minimap; Thieving never steals from PackPanel's slots |
| GrindstoneSkills | Husbandry's "better offspring" adds a star to a newborn |
| Elite Creatures Pack | Its creatures roll stars and mutations; a sleeping mimic hides them; the Kraken never rolls Twin, Phantom or Tethered |
| Epic Loot, other loot mods | Their drops pass through untouched |
| Modded creatures and biomes | Roll like any other; an unlisted biome uses the Meadows rules |

## Pages

- [Difficulty and World Tiers](wiki:Difficulty and World Tiers) - how many stars and mutations, what a star is worth
- [Mutations and Breeding](wiki:Mutations and Breeding) - the fifteen mutations, tamed creatures, breeding
- [Boss Aspects](wiki:Boss Aspects) - the sixteen aspects, boss stars, trophies, the damage board
- [Loot and Respawning](wiki:Loot and Respawning) - loot modes, drop rules, respawning camps and dungeons
- [Settings and Commands](wiki:Settings and Commands) - the rule file, display settings, console commands

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version, attach
`BepInEx/LogOutput.log`).

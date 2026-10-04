# EliteCrafting

**Work in progress:** significant changes will come, including to the config, the YAML files and item data.

Runes and magic gear: creatures and chests drop both, and a rune clicked onto an item changes its rarity or
inscriptions.

## Features
- Three rarities: Normal, Magic (1-2 inscriptions) and Rare (3-6).
- 162 inscriptions for every kind of gear, stronger the later the item's biome.
- Six runes, clicked onto an item: Awakening (Normal to Magic), Shaping (one more on
  Magic), Ascension (Magic to Rare), Consecrated (one more on Rare), Cleansing (back to Normal) and the Serpent
  (seals the item after a gamble).
- Drops from kills, bosses and world chests.
- Elite Creatures Reborn: `Synergy` (off by default) lets elite stars raise drops.
- Epic Loot: when installed, the runes work on Epic Loot's own magic items (not Cleansing) and only Epic Loot drops
  magic gear.
- Every rune, inscription, command and file:
  [CLAUDE.md](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCrafting/CLAUDE.md#player-reference).

## Install
Needed on the server and every client, same version. Install with r2modman or the Thunderstore app, or put
`EliteCrafting.dll` in `BepInEx/plugins`. Uninstalling deletes every rune; magic gear turns plain until reinstalled.
Mods that make weapons or armor stackable can merge magic items and lose inscriptions.

## Configuration
`BepInEx/config/com.EliteCrafting.cfg` and the `EliteCrafting_*.yml` files. Every setting is described in the file
and applies without a restart; the server's values bind every player (`Lock Configuration`). Console: `ecraft help`.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

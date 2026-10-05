# Getting Started

This wiki describes version 0.4.0. **Work in progress:** the config, the YAML files and the data stored on items will change.

## What it does

- Gear comes in three rarities: Normal (plain), Magic (1-2 inscriptions) and Rare (3-6).
- An inscription is a magic property such as +damage or faster movement. There are 162, stronger on later-biome items.
- Seven runes change an item's rarity and inscriptions.
- Creatures, bosses and world chests drop runes and ready-made magic gear.

## Install

Install on the server **and** every client, same version, with r2modman, the Thunderstore app, or `EliteCrafting.dll` in `BepInEx/plugins`. Needs BepInExPack Valheim.

## Quick start

1. Kill creatures and open dungeon chests to find runes and magic gear.
2. Click an **Awakening Rune** onto a plain weapon or armour piece in your inventory: it becomes Magic with one inscription.
3. Hover the item to read its inscriptions. Each shows its tier: T1 strongest, T7 weakest.
4. **Shaping** adds a second inscription, **Recasting** rerolls a Magic item you do not like, **Ascension** makes the item Rare, **Consecrated** adds more, up to six.
5. Type `ecraft help` in the console (F5) for commands.

## Other mods

- **Epic Loot**: the runes work on Epic Loot's magic items, and only Epic Loot drops magic gear.
- **Elite Creatures Reborn**: with `Synergy` on (off by default), elite stars and world tier raise the drops.
- **OpenKeep**: its salvage skips magic items by default.
- **Stackable gear**: if a mod makes weapons or armour stack, those items cannot become magic, and magic copies merged into a stack can lose their inscriptions.

## Pages

- [Runes and Magic Gear](wiki:Runes and Magic Gear): runes, rarities, tiers, Epic Loot
- [Inscription List](wiki:Inscription List): all 162 inscriptions
- [Drops and Loot](wiki:Drops and Loot): what drops where
- [Configuration and Commands](wiki:Configuration and Commands): settings, YAML files, commands

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version).

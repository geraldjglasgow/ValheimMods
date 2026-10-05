# Getting Started

This wiki describes version 0.6.0. **Work in progress:** the config, the YAML files and the data stored on items will change.

## What it does

- Gear comes in three rarities: Normal (plain), Magic (1-2 inscriptions) and Rare (3-6).
- An inscription is a magic property such as +damage or faster movement. There are 209. Each is a prefix (the item's core power: damage, health, armour, blocking, leech) or a suffix (everything else).
- Each kind of item, its class (swords, bows, helmets, trinkets...), rolls its own inscriptions, and gear from later biomes rolls stronger tiers.
- Seven runes change an item's rarity and inscriptions.
- Creatures, bosses and world chests drop runes and ready-made magic gear.

## Install

Install on the server **and** every client, same version, with r2modman, the Thunderstore app, or `EliteCrafting.dll` in `BepInEx/plugins`. Needs BepInExPack Valheim.

## Updating from 0.4.0

- Magic items keep their inscriptions and values. Their tiers are shown on each inscription's new ladder. A brand keeps its number, which now means flat damage: a 15% Emberbrand becomes +15 fire.
- `EliteCrafting_inscriptions.yml` and `EliteCrafting_economy.yml` are saved as `.v1.bak` and written fresh: redo your changes in the new files ([Configuration and Commands](wiki:Configuration and Commands)).

## Quick start

1. Kill creatures and open dungeon chests to find runes and magic gear.
2. Click an **Awakening Rune** onto a plain weapon, armour piece, trinket or tool in your inventory: it becomes Magic with one inscription.
3. Hover the item to read its inscriptions: prefixes first, then suffixes, each with its tier. T1 is the strongest.
4. **Shaping** adds a second inscription, **Recasting** rerolls a Magic item you do not like, **Ascension** makes the item Rare, **Consecrated** adds more, up to six.
5. Type `ecraft help` in the console (F5) for commands.

## Other mods

- **Elite Creatures Reborn**: with `Synergy` on (off by default), elite stars and world tier raise the drops.
- **PackPanel**: its backpacks take runes and roll inscriptions, including its own Deep Pockets (extra backpack slots).
- **Elite Creatures Pack**: its creatures drop runes and magic gear, its bosses more; its bone weapons and Kraken shield have item levels.
- **OpenKeep**: its salvage skips magic items by default.
- **Stackable gear**: if a mod makes weapons or armour stack, those items cannot become magic, and magic copies merged into a stack can lose their inscriptions.

## Pages

- [Runes and Magic Gear](wiki:Runes and Magic Gear): rarities, runes, item classes, item levels and tiers
- [Inscription List](wiki:Inscription List): all 209 inscriptions
- [Drops and Loot](wiki:Drops and Loot): what drops where
- [Configuration and Commands](wiki:Configuration and Commands): settings, YAML files, commands

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version).

# Getting Started

This wiki describes version 0.8.0. **Work in progress:** the config, the YAML files and the data stored on items will change.

## What it does

- Gear comes in three rarities: Normal (plain), Magic (2 inscriptions) and Rare (3); the Sealed Rune can add one more.
- An inscription is a magic property such as +damage or faster movement. There are 209. Each is a prefix (the item's core power: damage, health, armour, blocking, leech) or a suffix (everything else).
- Each kind of item, its class (swords, bows, helmets, trinkets...), rolls its own inscriptions, and gear from later biomes rolls stronger tiers.
- Five runes change an item's rarity or inscriptions; the Dvergr Chisel cuts sockets. Magic gear never drops: every Magic or Rare item is made with runes.
- The Rune Table stores runes, turns trophies into essence and, with the Ascension Rune, lets an essence choose the new inscription.
- Weapons, staves, armour, shields and backpacks can carry up to three sockets; eleven gems, dropped only by bosses, fill them, each adding a stat that depends on the item.
- Creatures, bosses and world chests drop runes.

## Install

Install on the server **and** every client, same version, with r2modman, the Thunderstore app, or `EliteCrafting.dll` in `BepInEx/plugins`. Needs BepInExPack Valheim.

## Updating from 0.7.0

- Magic and Rare items keep their inscriptions and values; their tiers are shown on the new ladders of at most 8 tiers (was 13).
- The Shaping and Consecrated Runes are gone: stacks you hold disappear. The Dvergr Chisel cuts sockets.
- Magic gear no longer drops; the `Magic item drops` setting is gone.
- `EliteCrafting_inscriptions.yml` and `EliteCrafting_economy.yml` are saved as `.v2.bak` (`.v1.bak` from 0.4.0 or older) and written fresh: redo your changes in the new files ([Configuration and Commands](wiki:Configuration and Commands)).

## Quick start

1. Kill creatures and open dungeon chests to find runes.
2. Click an **Awakening Rune** onto a plain weapon, armour piece, trinket or tool in your inventory: it becomes Magic with two inscriptions.
3. Hover the item to read its inscriptions: prefixes first, then suffixes, each with its tier. T1 is the strongest.
4. **Recasting** rerolls a Magic item you do not like, **Ascension** makes it Rare with a third inscription. The **Dvergr Chisel** cuts sockets for gems. At a [Rune Table](wiki:Rune Table) an essence can choose what Ascension adds.
5. Type `ecraft help` in the console (F5) for commands.

## Other mods

- **Elite Creatures Reborn**: with `Synergy` on (off by default), elite stars and world tier raise the drops.
- **PackPanel**: its backpacks take runes and roll inscriptions, including its own Deep Pockets (extra backpack slots).
- **Elite Creatures Pack**: its creatures drop runes, its bosses more; its bone weapons and Kraken shield have item levels.
- **OpenKeep**: salvaging a Magic item may give back an Awakening Rune, a Rare item an Ascension Rune, one time in four (`Runes from salvage`).
- **Stackable gear**: if a mod makes weapons or armour stack, those items cannot become magic, and magic copies merged into a stack can lose their inscriptions.

## Pages

- [Runes and Magic Gear](wiki:Runes and Magic Gear): rarities, runes, item classes, item levels and tiers
- [Inscription List](wiki:Inscription List): all 209 inscriptions
- [Drops and Loot](wiki:Drops and Loot): what drops where
- [Configuration and Commands](wiki:Configuration and Commands): settings, YAML files, commands
- [Gems and Sockets](wiki:Gems and Sockets): sockets, the Dvergr Chisel and the eleven gems
- [Rune Table](wiki:Rune Table): the table, its essences and trophy sacrifices

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version).

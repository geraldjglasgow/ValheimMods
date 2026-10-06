# Changelog

## 0.7.0

- Recasting Rune: every inscription is replaced by one or two new ones; the item stays Magic.
- Fixed: Elite Creatures Reborn's Phantom copies dropped EliteCrafting loot; they and Cloning decoys now drop none.
- Of a Tethered boss pair, only the last to fall drops EliteCrafting loot.
- With PackPanel 0.12.0+, its stat sheet lists your active inscriptions.
- Less work per hit, per frame and for dropped items; no stutter on the first kill after loading.
- Update the server and every client together.

## 0.6.1

- Fixed: Magic and Rare set armour shows its inscriptions again with Epic Loot installed.
- Fixed: mead bases and the dragon egg no longer drop or roll as magic items.

## 0.6.0

- Runes are stone tablets, each with its own glyph and icon.
- Runes work straight from the chest you have open; the item stays in your inventory.
- Magic and Rare items show a rarity background on every icon, with or without PackPanel.
- Dropped magic items shine a beam of light with rising sparks (`Loot beam`).
- Tooltips taller than the screen scroll with the mouse wheel or right stick.

## 0.5.0

- Item classes: each kind of item rolls its own inscriptions (`ecraft classes`).
- Tiers by item level: Meadows gear rolls the weakest tier, Deep North gear every tier; strong tiers are rare.
- Prefixes and suffixes: Magic holds 1 of each, Rare 3; totals are capped across your gear.
- Brands add flat damage (+X fire), scaled by weapon.
- 47 new inscriptions, including critical hits, chain lightning, throwing weapons, Glass Cannon and Heavy Hand.
- Deep North gear is item level 8.
- An API for other mods; PackPanel and Elite Creatures Pack use it.
- Removed: Epic Loot integration.
- Your inscriptions and economy files are saved as `.v1.bak` and replaced: redo your changes.
- Existing magic items keep their inscriptions.

## 0.4.0

- Creature drops appear with the creature's own loot when its body dissolves, not at the moment of death.

## 0.3.0

- Recasting Rune rerolls one to all of an item's inscriptions in place and never removes one.
- With Epic Loot: Recasting rerolls one to all effects in place; augments and sockets stay.
- PackPanel's backpacks never become magic gear; with Epic Loot, runes work on them.
- Faster loading: the config file is written once instead of once per setting.

## 0.2.0

- Recasting Rune, the seventh rune: rerolls every inscription on a Magic item, which stays Magic. Drops everywhere.
- Epic Loot: Recasting rerolls every effect of an Epic Loot Magic item; sockets stay.
- Existing `EliteCrafting_economy.yml` files get the new rune and its drops without editing.
- Sealed items show just "Sealed" in the tooltip (was "Sealed: Corrupted").
- Less work every frame keeping the config in sync.

## 0.1.0

- First release, a work in progress: significant changes will come.
- Three rarities: Normal, Magic (1-2 inscriptions) and Rare (3-6); 162 inscriptions, tiers by biome.
- Six runes clicked onto items: Awakening, Shaping, Ascension, Consecrated, Cleansing and the sealing Serpent.
- Kills and world chests drop runes and magic gear.
- Rarity-colored names, inscription tooltips and a ground glow on magic items.
- Elite Creatures Reborn `Synergy`: elite stars raise drops.
- Epic Loot: runes work on its magic items, and EliteCrafting drops no magic gear of its own.
- YAML rules over built-in defaults, server synced and reloaded live; console command `ecraft`.

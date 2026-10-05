# Changelog

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

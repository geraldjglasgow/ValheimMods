# Drops and Loot

Runes and magic gear drop beside a creature's normal loot. Every number here is a default the server can change in `EliteCrafting_economy.yml`.

## Who drops loot

- A creature drops loot only if a player or a player's pet hit it.
- Tamed and summoned creatures drop nothing, nor Elite Creatures Reborn's Cloven twins and Phantom husks.
- A creature's tier is the earliest biome it naturally lives in (else where it dies): Meadows 1, Black Forest 2, Swamp 3, Mountain and Ocean 4, Plains 5, Mistlands 6, Ashlands and Deep North 7.

## Chance per kill

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Rune | 4% | 5% | 6% | 7% | 8% | 9% | 10% |
| Magic item | 1% | 1.25% | 1.5% | 1.75% | 2% | 2.25% | 2.5% |

Stars multiply the chance: one star x2, two stars x3. At most 5 runes and 2 magic items per kill.

## Which rune drops

| Rune | T1 | T2 | T3 | T4 | T5 | T6 | T7 |
|---|---|---|---|---|---|---|---|
| Awakening | 69% | 50% | 40% | 33% | 28% | 25% | 21% |
| Shaping | 26% | 25% | 25% | 25% | 23% | 22% | 21% |
| Ascension | - | 20% | 22% | 23% | 23% | 22% | 21% |
| Consecrated | - | - | 4% | 8% | 12% | 15% | 18% |
| Cleansing | 5% | 5% | 5% | 6% | 7% | 7% | 8% |
| Serpent | - | - | 4% | 5% | 7% | 9% | 11% |

## Magic items

A dropped item is something a player can craft, from the creature's tier or one below. Its rarity:

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Magic / Rare | 80/20 | 70/30 | 60/40 | 50/50 | 45/55 | 40/60 | 35/65 |
| From bosses | 40/60 | 30/70 | 20/80 | 15/85 | 10/90 | 5/95 | 0/100 |

## Bosses

Bosses count as tiers 1-7 in this order. Each drops one magic item, some random runes and fixed bonus runes:

| Boss | Random runes | Bonus runes |
|---|---|---|
| Eikthyr | 2 | 2 Awakening, 1 Shaping |
| The Elder | 2 | 1 Ascension |
| Bonemass | 3 | 50%: 1 Serpent; 50%: 1 Consecrated |
| Moder | 3 | 1 Consecrated |
| Yagluth | 4 | 1 Consecrated; 25%: 1 Serpent |
| The Queen | 4 | 2 Consecrated |
| Fader | 5 | 2 Consecrated; 50%: 1 Serpent |

## World chests

Dungeon chests, ruin chests and buried treasure have a 30% rune chance and a 10% magic item chance, rolled when the game first fills them. Player-built chests never roll.

## Loot-find inscriptions

They work for the player who lands the killing blow, up to +200% each.

| Inscription | Raises |
|---|---|
| Norns' Favour | The chance a dropped magic item is Rare |
| Fateweaver | The rune chance |
| Trophy Taker | The creature's trophy chance |
| Hoardfinder | The creature's coin and treasure chance |

## Elite Creatures Reborn

With `Synergy` on (off by default), ECR's elite stars replace the game's stars (2 stars x1.5, 3 stars x2, up to 5 stars x3, +0.5 per star after), and ECR's world tier raises the rune chance (up to x1.7) and the Rare share (up to +35%). `ecraft ecr` shows what the creature you look at would pay.

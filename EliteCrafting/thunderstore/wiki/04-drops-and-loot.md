# Drops and Loot

Runes drop beside a creature's normal loot, at the same moment: when its body dissolves, or at once for a creature that leaves none. Magic gear never drops: every Magic or Rare item is made with runes. Every number here is a default the server can change in `EliteCrafting_economy.yml`.

## Who drops loot

- A creature drops loot only if a player or a player's pet hit it.
- Tamed and summoned creatures drop nothing, nor Elite Creatures Reborn's Phantom copies and Cloning decoys, nor the first of a Tethered pair to fall.
- A creature's tier is the earliest biome it naturally lives in (else where it dies): Meadows 1, Black Forest 2, Swamp 3, Mountain and Ocean 4, Plains 5, Mistlands 6, Ashlands 7, Deep North 8. Other mods can set their creatures' tiers (Elite Creatures Pack does).

## Chance per kill

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Rune | 35% | 40% | 45% | 50% | 55% | 60% | 65% | 70% |

Stars multiply the chance: one star x2, two stars x3. At most 5 runes per kill.

## Which rune drops

When a rune drops, the chance it is each rune (or a Dvergr Chisel), by tier:

| Rune | T1 | T2 | T3 | T4 | T5 | T6 | T7 | T8 |
|---|---|---|---|---|---|---|---|---|
| Awakening | 37% | 26% | 21% | 18% | 15% | 14% | 12% | 10% |
| Recasting | 50% | 50% | 50% | 50% | 50% | 50% | 50% | 50% |
| Ascension | - | 10% | 12% | 13% | 13% | 12% | 12% | 11% |
| Cleansing | 3% | 3% | 3% | 3% | 4% | 4% | 4% | 5% |
| Sealed | - | - | 2% | 3% | 4% | 5% | 6% | 7% |
| Dvergr Chisel | 10% | 11% | 12% | 13% | 14% | 15% | 16% | 17% |

The chisel drops only while `Gems and sockets` is on; while it is off, a rune drops in its place. Gems never drop from ordinary creatures or chests: only bosses drop them (below) ([Gems and Sockets](wiki:Gems and Sockets)).

## Bosses

Bosses count as tiers 1-7 in this order. Each drops some random runes, fixed bonus runes, the Dvergr Chisel 25% of the time and gems (any of the eleven, at random): 50% for one gem, plus 50% for one more for each star the boss has (a 4-star boss can drop five).

| Boss | Random runes | Bonus runes |
|---|---|---|
| Eikthyr | 2 | 2 Awakening, 1 Recasting |
| The Elder | 2 | 1 Ascension |
| Bonemass | 3 | 50%: 1 Sealed Rune; 50%: 1 Ascension |
| Moder | 3 | 1 Ascension |
| Yagluth | 4 | 1 Ascension; 25%: 1 Sealed Rune |
| The Queen | 4 | 2 Ascension |
| Fader | 5 | 2 Ascension; 50%: 1 Sealed Rune |

## World chests

Dungeon chests, ruin chests and buried treasure have a 50% rune chance, rolled when the game first fills them. Player-built chests never roll.

## Loot-find inscriptions

They work for the player who lands the killing blow, up to +200% each.

| Inscription | Raises |
|---|---|
| Norns' Favour | The chance a dropped magic item is Rare (nothing for now: magic gear does not drop) |
| Fateweaver | The rune chance |
| Trophy Taker | The creature's trophy chance |
| Hoardfinder | The creature's coin and treasure chance |

## Elite Creatures Reborn

With `Synergy` on (off by default), ECR's elite stars replace the game's stars (2 stars x1.5, 3 stars x2, up to 5 stars x3, +0.5 per star after), and ECR's world tier raises the rune chance (up to x1.7). `ecraft ecr` shows what the creature you look at would pay.

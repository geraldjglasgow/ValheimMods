# Drops and Loot

Runes drop beside a creature's normal loot, at the same moment: when its body dissolves, or at once for a creature that leaves none. Magic gear never drops: every Magic or Rare item is made with runes. Every number here is a default the server can change in `EliteCrafting_economy.yml`.

## Who drops loot

- A creature drops loot only if a player or a player's pet hit it.
- Tamed and summoned creatures drop nothing, nor Elite Creatures Reborn's Phantom copies and Cloning decoys, nor the first of a Tethered pair to fall.
- A creature's tier is the earliest biome it naturally lives in (else where it dies): Meadows 1, Black Forest 2, Swamp 3, Mountain and Ocean 4, Plains 5, Mistlands 6, Ashlands 7, Deep North 8. Other mods can set their creatures' tiers (Elite Creatures Pack does).

## Chance per kill

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Rune | 4% | 5% | 6% | 7% | 8% | 9% | 10% | 11% |

Stars multiply the chance: one star x2, two stars x3. At most 5 runes per kill.

## Which rune drops

When a rune drops, the chance it is each rune (or, rarely, a Dvergr Chisel or a gem), by tier:

| Rune | T1 | T2 | T3 | T4 | T5 | T6 | T7 | T8 |
|---|---|---|---|---|---|---|---|---|
| Awakening | 71% | 51% | 41% | 33% | 27% | 24% | 20% | 18% |
| Recasting | 21% | 22% | 22% | 23% | 23% | 22% | 20% | 19% |
| Ascension | - | 20% | 22% | 23% | 23% | 22% | 20% | 19% |
| Consecrated | - | - | 4% | 8% | 11% | 14% | 18% | 21% |
| Cleansing | 5% | 5% | 6% | 6% | 7% | 7% | 8% | 8% |
| Serpent | - | - | 4% | 5% | 7% | 8% | 10% | 12% |
| Dvergr Chisel | 0.9% | 0.8% | 0.9% | 1% | 1% | 1% | 1% | 1% |
| Any of the 11 gems | 1% | 0.9% | 1% | 1% | 1% | 1% | 1% | 1% |

The Consecrated Rune, the chisel and the gems drop only while `Gems and sockets` is on. That is one gem in about 2,500 kills at tier 1 and one in 640 at tier 8, before stars ([Gems and Sockets](wiki:Gems and Sockets)).

## Bosses

Bosses count as tiers 1-7 in this order. Each drops some random runes, fixed bonus runes, the Dvergr Chisel 25% of the time and its own gems:

| Boss | Random runes | Bonus runes | Gems |
|---|---|---|---|
| Eikthyr | 2 | 2 Awakening, 1 Recasting | Thor's 10%, Sleipnir's 10% |
| The Elder | 2 | 1 Ascension | Freyja's 15% |
| Bonemass | 3 | 50%: 1 Serpent; 50%: 1 Ascension | Nidhogg's 15% |
| Moder | 3 | 1 Ascension | Ymir's 20%, Skadi's 20% |
| Yagluth | 4 | 1 Ascension; 25%: 1 Serpent | Surtr's 20%, Tyr's 20% |
| The Queen | 4 | 2 Ascension | Odin's 20%, Heimdall's 20% |
| Fader | 5 | 2 Ascension; 50%: 1 Serpent | Hel's 25% |

## World chests

Dungeon chests, ruin chests and buried treasure have a 30% rune chance, rolled when the game first fills them. Player-built chests never roll.

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

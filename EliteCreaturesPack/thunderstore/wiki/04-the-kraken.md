# The Kraken

A deep-ocean boss that hunts ships. Outrun it under sail and it gives up. If it catches you it holds the ship and
attacks in two phases: tentacles slamming the deck, then its head at the rail.

## The hunt

- **Where:** rarely, in deep ocean (25 m or more), near a ship with a crew. A crew sailing the open sea meets one
  about every hour or two.
- It chases a ship within 90 m. **Escape** under sail with a fair wind, or into shallow water; rowing is too slow.
  Kept out of reach for a few seconds, it dives and is gone for good.
- **Caught:** it holds the ship from below. The ship cannot sail or turn until the kraken dies or everyone has been
  off and away from the ship for 12 seconds.
- While it holds the ship, **players cannot damage their own ship**, so swing over the rail freely. Its smashes still
  do: a karve has 500 health, a longship 1000.

## The fight

It makes one attack at a time, every 2.5 to 4 seconds (a little slower with one or two players aboard). It alternates
6 tentacle slams and 6 head attacks until it dies. Hits on tentacles and head count against one health bar.

**Tentacle phase:** six tentacles rise round the hull.

- **Slam:** a tentacle rears up, pauses (the tell), then whips across the deck along a line to the nearest player:
  60 blunt and knockback. Step out of the line, roll, or block. It lies on deck for a second: hit it.
- **Grab:** once per phase, warned by a deep bellow and churning water, a tentacle rears higher and trembles longer.
  Whoever it lands on is thrown into the sea. **Roll**; a shield does not help.

**Head phase:** the head rises at the rail, in melee reach, on the side with no ladder (you can always climb back
aboard).

| Attack | Who | Damage | Answer |
| --- | --- | --- | --- |
| Bite | Anyone within about 2.5 m of the rail in front of it | 80 pierce | Dodge, step back or block |
| Ink | Anyone further off; a straight stream after a one-second rear-back, at most every 6 s | 15 blunt, and ink covers your screen for 1.5 s | Step aside, roll, block or parry, or hide behind the mast |
| Head lunge | Anyone up to about 5 m in from the rail, while its ink recovers | 80 pierce | Roll, or get further away |
| Ship smash | Every 10 s or so, and when nobody is in reach of the head | 40 to the ship, 60 to anyone in its line | Step out of its line |

**Stagger:** enough damage at once cancels its coming blow and delays its next attack.

## Stats

- A boss with a boss health bar: 3000 health (Bonemass: 5000), more with more players nearby. Serpent resistances.
- A dodge that looks clear on your screen is clear.
- With Elite Creatures Reborn 3.12.0 or later it rolls boss stars and an aspect.

## Loot

On the held ship's deck, or on the water where it died if it held no ship: 20-29 Chitin, 5-7 Raw kraken tentacle,
150-299 Coins and exactly 1 Kraken beak. Loot other mods add (such as EliteCrafting's runes: every kill 3
runes and a Sealed Rune) lands there too, never on the sea floor.

| Item | What it is |
| --- | --- |
| Kraken beak | Material for the Kraken shield. Stacks to 10, weight 3. |
| Raw kraken tentacle | Not edible raw. Cooks like serpent meat, on the iron cooking station. Weight 10. |
| Cooked kraken tentacle | Food: 80 health, 28 stamina, 4 healing per tick, 30 minutes (cooked serpent meat: 70, 23, 3, 25 minutes). Weight 10. |

### Kraken shield

Made at the **forge** (the serpent scale shield's level) from **10 Fine wood, 5 Silver, 1 Kraken beak**; each upgrade
10 Fine wood and 3 Silver. Three quality levels.

| | Kraken shield | Serpent scale shield |
| --- | --- | --- |
| Block | 70 (+6 a level) | 60 (+6) |
| Deflection | 110 (+5 a level) | 100 (+5) |
| Parry | 1.5x | cannot parry |
| Movement | -5% | -10% |
| Durability | 300 (+50 a level) | 250 (+50) |
| Weight | 4 | 5 |

**Parry bite:** parry a creature's blow with it and the beak bites back: 60 pierce, +10 per quality level above 1.
Works on any creature, melee or ranged, never on players.

With EliteCrafting it is a round shield of item level 4 (Mountain and Ocean) and can become magic.

## Settings: `5 - Kraken`

| Key | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Krakens can appear |
| Chance | 0.3 | Percent chance per ocean zone roll near a crewed ship |
| Interval | 3600 | Seconds between a zone's rolls |
| Min Depth | 25 | Metres of water it needs; it never follows into shallower water |
| Health | 3000 | Its health |
| Swim Speed | 3.5 | Chase speed in m/s (rowing: about 2) |
| Hunt Range | 90 | Metres at which it notices a ship, and gives up beyond |
| Attack Gap Min | 2.5 | Least seconds between attacks |
| Attack Gap Max | 4 | Most seconds between attacks |
| Gap Factor Solo | 1.1 | Gap multiplier with one player aboard |
| Gap Factor Pair | 1.2 | Gap multiplier with two players aboard |
| Tentacle Gap Factor | 0.7 | Gap multiplier for tentacle slams |
| Phase Attacks | 6 | Attacks per phase before it switches |
| Slam Damage | 60 | Blunt damage of a tentacle slam |
| Ship Damage | 40 | Damage to the ship per smash |
| Ship Hit Interval | 10 | Seconds between ship smashes |
| Bite Damage | 80 | Pierce damage of the bite |
| Ink Damage | 15 | Blunt damage of the ink |
| Ink Blind | 1.5 | Seconds the ink covers your screen |
| Ink Interval | 6 | Least seconds between ink spits |
| Shield Parry Damage | 60 | Pierce damage of the Kraken shield's parry bite |
| Shield Parry Damage Per Level | 10 | Added per shield quality level above 1 |

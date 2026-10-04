# Foraging Skill

A new skill for wild picks (which the game counts for Farming): berries, mushrooms, herbs and things from the ground, as listed in `GrindstoneSkills.Forage.yml`. Crops, planted or wild, stay Farming's.

## Perks (34 - Forage Perks)

| Setting | Default | Meaning |
| --- | --- | --- |
| Extra Yield Chance At 100 | 50 | % chance a pick gives one more; replaces the game's 25%. |
| Sweep Level | 25 | From this level, picking a plant also picks the same kind around it (101 off). |
| Sweep Radius At 100 | 4 | Sweep reach in metres at level 100 (1 m at 25). |
| Best Time Levels | 20 | No effect since 0.13.0. |

**Best-time hints:** a plant's hover says when it is best picked ("Best picked at night") or "At its best now". Since 0.13.0 this is only a hint.

## Experience (33 - Foraging)

| Setting | Default | Meaning |
| --- | --- | --- |
| Foraging Enabled | true | Turns Foraging on or off; off, wild picks train Farming again. Levels are kept. |
| Show Callouts | true | Floating words like "Discovered Thistle!". Per player. |
| Show Hints | true | Best-time hints on hover. Per player. |
| Experience Per Pick | 3 | Experience per Meadows pick, times the item's factor. |
| Experience Per Biome Step | 25 | % more per biome step (Meadows 0 up to Ashlands and Deep North 6). |
| Discovery Multiplier | 3 | Multiplier for your first pick of each kind. |

## Default forage list

| Items | Best time | Experience factor |
| --- | --- | --- |
| Raspberry, Blueberries, Cloudberry, Lingonberry | dry day | 1 |
| Mushroom, MushroomYellow, MushroomSmokePuff | rain | 1 |
| MushroomBlue, Thistle | night | 1 |
| Dandelion, Fiddleheadfern, RoyalJelly | day | 1 |
| Flint, Stone, Grausten, Wood, Frostwood | - | 0.5 |
| wild Barley, Flax | - | 1 |

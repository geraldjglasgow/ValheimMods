# Configuration And Compatibility

## Files

In `BepInEx/config`, written at first start; edits apply without a restart.

| File | What it sets |
| --- | --- |
| `com.GrindstoneSkills.cfg` | every setting, by numbered section (see each skill's page) |
| `GrindstoneSkills.Finds.yml` | what felled trees can hide |
| `GrindstoneSkills.MineFinds.yml` | what broken rock can hide |
| `GrindstoneSkills.Forage.yml` | which picks count as foraging |
| `GrindstoneSkills.Snags.yml` | what a fishing line can snag |

## On a server

- **Lock Configuration** (`1 - General`, default `true`): the server's settings and YAML files apply to every player. Off, each player uses their own files.
- Admins can change server settings from their own game (for example with a configuration manager).
- Per player: `Stars On Icons`, the two keys, every `Show Callouts`, `Row Planting`, `Auto Replant`.

## Reading the numbers

- **"At 100"**: the value at level 100, growing evenly from 0 (50 at 100 is 25 at level 50). **"At 0" / "At 100"** pairs grow from one to the other.
- **"Level"**: unlocks at that level; 0 is everyone, 101 is off.
- Percents are plain numbers (25 = 25%). A chance or amount of 0 turns that feature off.
- Each skill but Cooking has an `Enabled` switch; levels are always kept.

| Sections | Page |
| --- | --- |
| 6, 7 | [Skill Book And Death](wiki:Skill Book And Death) |
| 4, 5 | [Cooking Skill](wiki:Cooking Skill) |
| 8, 9, 35 | [Sailing Skill](wiki:Sailing Skill) |
| 10-13 | [Woodcutting Skill](wiki:Woodcutting Skill) |
| 14-18 | [Pickaxes Skill](wiki:Pickaxes Skill) |
| 19-22 | [Defense Skill](wiki:Defense Skill) |
| 23-28 | [Husbandry Skill](wiki:Husbandry Skill) |
| 29-32 | [Farming Skill](wiki:Farming Skill) |
| 33, 34 | [Foraging Skill](wiki:Foraging Skill) |
| 50-54 | [Fishing Skill](wiki:Fishing Skill) |

## YAML files

Extra files named `GrindstoneSkills.Finds<anything>.yml` (likewise for the others) are read too, and replace same-named entries. A file with an error is ignored and the last good one stays in use.

**Finds, MineFinds and Snags:**

```
biomes:
  Meadows:
    - name: a queen bee's hive
      weight: 2
      items:
        - { prefab: Honey, min: 1, max: 3 }
```

| Field | Meaning |
| --- | --- |
| `biomes:` | a list per biome (`Meadows`, `BlackForest`, `Swamp`, `Mountain`, `Plains`, `Mistlands`, `AshLands`, `DeepNorth`, `Ocean`). |
| `trees:` / `deposits:` | Finds / MineFinds only: a list per tree or rock prefab, used instead of its biome's. |
| `name` | shown in the "Found ..." message. |
| `weight` | how often it is picked (default 1, 0 off). |
| `items` | `prefab`, `min`, `max` of each item that drops. |

**Forage:**

```
forage:
  Raspberry: {}
  Flint: { experience: 0.5 }
```

| Field | Meaning |
| --- | --- |
| `<ItemPrefab>:` | an item that counts as forage (crops never do). |
| `experience` | experience factor (default 1). |

## Console commands

| Command | What it does |
| --- | --- |
| `fishlog` / `/fishlog` | Your angler's log. |
| `raiseskill <skill> <n>` | Game cheat; also works for `sailing`, `foraging`, `husbandry`, `defense` and `all`. |
| `resetskill <skill>` | Game cheat; the same skills. |
| `charter` | Whether the server's settings bind you, and if you may change them. |
| `charter diff` | Your settings that differ from the server's. |
| `charter versions` | Mod versions on your side and the server's. |

## Other mods

- **Elite Creatures Reborn 3.10.0+:** decides newborns' stars; Better offspring adds one more, up to one above the stronger parent.
- **FeastMaster:** Defense food bonuses scale its food values; cooking speeds stack with its times.
- **OpenKeep:** crafting from chests and pets eating from chests work alongside; extra honey adds to its beehives.
- **ShipConfig:** its ship health replaces Sailing's health bonus; Sailing's speed still applies.
- Other mods' kitchens, rocks, trees, crops and skills work when they behave like the game's; add their items to the YAML files by prefab name.

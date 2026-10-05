# Loot and Respawning

## Loot modes

Set with `mode:` under `loot:`.

| Mode | What drops |
| --- | --- |
| Vanilla | The game's normal drops |
| Scaled | Normal drops, amounts multiplied by the star loot line (x1.5 at 2 stars, x3 at 5) |
| Rolled (default) | The drop table is rolled once more per star |
| Curated | Only the drops you list for that creature |

- Trophies are not multiplied unless `multiply trophies: true`. Bosses always drop one trophy for every player
  within 100 m, plus one per star plus one (twice that when Bountiful).
- Mutations do not add loot, except Gilded (3x loot plus coins). Thieving creatures drop what they stole.
- Boss aspect loot multipliers apply in every mode, Vanilla included.
- Other mods' drops are left alone.

| Key | Default | Meaning |
| --- | --- | --- |
| `mode` | Rolled | Vanilla, Scaled, Rolled or Curated |
| `extra roll chance` | [0, 100] | Rolled: % chance of an extra roll, by star count |
| `max extra rolls` | 5 | Rolled: most extra rolls (0 = no limit) |
| `global multiplier` | 1 | Multiplies every drop |
| `boss multiplier` | 1 | Multiplies boss drops on top |
| `multiply trophies` | false | Trophies follow the mode too |

## Drops for one creature

Add entries under `creatures:`, matched by prefab name:

```yaml
creatures:
  - match: Troll
    drops: [1, 1.5, 2, 3, 4, 5]      # its own star loot line
    multiply trophies: true
    drop overrides:                   # change its normal drops
      - item: TrollHide
        amount: [2, 5]
        chance: 100
      - item: Coins
        remove: true
    extra drops:                      # add drops (the whole list in Curated mode)
      - item: Ruby
        chance: 10
        amount: [1, 1]
        per star: true                # more rolls or amount with stars, like its normal drops
```

`amount` is `[min, max]`; `chance` is 0-100. `elite reference` writes `creature_reference.yml` with every creature's
prefab name, health and drops.

## Respawning

Most creatures guarding a place in Valheim are placed once and never come back after you kill them. These switches
bring them back. All off by default, all under `respawning:`. Times are in world days (30 real minutes each).

| Switch | Timer | What comes back |
| --- | --- | --- |
| `camps` | `camp days` 5 | Creatures guarding places out in the world: Fuling and Draugr villages, Greydwarf and skeleton ruins, Swamp huts, Mistlands dvergr outposts, Ashlands fortresses, drake nests, tar pits and the like |
| `dungeons` | `dungeon days` 7 | Creatures inside dungeons, the places you enter through a door with a loading screen: Burial Chambers, Sunken Crypts, Frost Caves, Infested Mines and the like |
| `dungeon loot` | `dungeon loot days` 14 | The contents of dungeon chests, refilled from their usual loot |

- **Only creatures (and dungeon chests) come back.** This is not a location reset: destroyed buildings, totems,
  spawner nests, rocks, ore, pickables and chests out in the world stay as you left them.
- Each creature comes back on its own: once the timer has passed since it was last seen alive, the next time a
  player comes near its spot. One still alive somewhere (it wandered off, or you tamed it) is not replaced.
- Never inside your base: a spot near your workbench, fire or other base pieces stays clear.
- Creatures the game already brings back on its own timer keep it.
- A dungeon chest refills only once it is completely empty, never topped up while you are still looting it. Its timer
  starts the first time it is seen with `dungeon loot` on.
- Works in existing worlds: places you cleared before turning it on come back too.
- Changes apply as each area loads; a place already loaded around you keeps its old timer until you come back.

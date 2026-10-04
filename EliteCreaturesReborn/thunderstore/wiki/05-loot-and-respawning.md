# Loot and Respawning

## Loot modes

Set with `mode:` under `loot:`.

| Mode | What drops |
| --- | --- |
| Vanilla | The game's normal drops |
| Scaled | Normal drops, amounts multiplied by the star loot line (x1.5 at 2 stars, x3 at 5) |
| Rolled (default) | The drop table is rolled once more per star |
| Curated | Only the drops you list for that creature |

- Trophies are not multiplied unless `multiply trophies: true`. Bosses always drop one trophy per star plus one.
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

All off by default. Times are in world days (30 real minutes each).

| Switch | Timer | What comes back |
| --- | --- | --- |
| `camps` | `camp days` 5 | Creatures in cleared camps |
| `dungeons` | `dungeon days` 7 | Creatures in cleared dungeons |
| `dungeon loot` | `dungeon loot days` 14 | Items in fully emptied dungeon chests |

All under `respawning:`.

# Difficulty and World Tiers

## Difficulty

`difficulty:` at the top of `creature_rules.yml`: `Easy`, `Medium` (default), `Hard`, `Very Hard` or `Extreme`. Without
the line, or with `Custom`, the file's own biome rows apply (see the end of this page).

The biome you are entering (Meadows with no bosses down, Black Forest after one, and so on) is always the gentlest.
Each boss killed after a biome makes that biome harder, so by the end the Meadows is the hardest place.

### Most stars a creature can have

| Difficulty | Biome you're entering | Each boss killed after a biome | Ocean |
| --- | --- | --- | --- |
| Easy | 1 | +1, up to 4 | 2 |
| Medium | 2 | +1, up to 5 | 2, then 3 from world tier 4 |
| Hard | 3 | +1, up to 5 | 3, then 4 from world tier 4 |
| Very Hard | 4 | +1, up to 5 | 4, then 5 from world tier 4 |
| Extreme | 5 at tiers 0-1, 6 at 2-3, 7 at 4-5, 8 at 6-7, in every biome reached | | One lower, at least 5 |

Most creatures stay plain and each extra star is rarer. Starting odds out of 100, by cap:

| Cap | Plain | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 90 | 10 | | | | | | | |
| 2 | 85 | 12 | 3 | | | | | | |
| 3 | 75 | 15 | 7 | 3 | | | | | |
| 4 | 65 | 17 | 10 | 5 | 3 | | | | |
| 5 | 55 | 18 | 12 | 8 | 5 | 2 | | | |
| 6 | 45 | 18 | 13 | 10 | 7 | 5 | 2 | | |
| 7 | 35 | 18 | 14 | 11 | 9 | 7 | 4 | 2 | |
| 8 | 28 | 17 | 14 | 12 | 10 | 8 | 6 | 3 | 2 |

Harder difficulties and each boss killed after a biome shift these odds towards more stars; Easy shifts them down.

### Mutation chance

The chance a plain creature is mutated. Each star adds a quarter of it. Each boss killed after a biome moves it a fifth
of the way to the ceiling.

| Difficulty | Biome you're entering | Ceiling | Ocean |
| --- | --- | --- | --- |
| Easy | 10% | 35% | 10% |
| Medium | 25% | 55% | 25%, then 30% from world tier 4 |
| Hard | 30% | 65% | 30%, then 35% from world tier 4 |
| Very Hard | 37.5%, +3.5 per boss killed | 81.5% | Halfway between Hard and Extreme |
| Extreme | 45%, +7 per boss killed | 98% | As the biome you're entering |

The biome decides which mutation (see [Mutations and Breeding](wiki:Mutations and Breeding)).

## What a star is worth

| Stars | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Size | +6% | +10% | +15% | +20% | +25% | +30% | +35% | +40% | +45% |
| Health | x1 | x1.4 | x1.95 | x2.6 | x3.3 | x4 | x4.7 | x5.4 | x6.1 |
| Damage | x1 | x1.2 | x1.45 | x1.75 | x2.1 | x2.5 | x2.9 | x3.3 | x3.7 |
| Attack speed | x1 | x1.02 | x1.05 | x1.08 | x1.12 | x1.16 | x1.19 | x1.22 | x1.25 |
| Move speed | x1 | x1 | x1.03 | x1.06 | x1.1 | x1.15 | x1.18 | x1.21 | x1.24 |
| Loot | x1 | x1 | x1.5 | x2 | x2.5 | x3 | x3.5 | x4 | x4.5 |

These are the `star power` lines: `growth`, `hp`, `attack`, `swing speed`, `speed`, `drops`. On the nameplate a large
star is worth five, and stars take the mutation's colour. Bosses use their own table ([Boss Aspects](wiki:Boss Aspects)).

## World tiers

The world tier is 0 in a new world and rises by one the first time each boss is killed, by anyone. Everyone sees "The
world hardens" when it rises. On a difficulty it drives the tables above; bosses and bred young ignore it.

- `elite tier` shows the tier and what it does where you stand. With PackPanel it also shows on the inventory and under
  the minimap.
- Admins can test a tier with the game's `setkey` / `removekey` on the `defeated_...` keys.

```yaml
world tiers:
  enabled: true      # false: always tier 0
  bosses: [defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader]
  star boost:     [1, 1.1,  1.2, 1.3,  1.4, 1.5,  1.6, 1.7]    # Custom only, by tier
  mutation boost: [1, 1.15, 1.3, 1.45, 1.6, 1.75, 1.9, 2]      # Custom only, by tier
```

Add a modded boss's key to `bosses` to make it count; `elite tier` lists every boss's key.

## Custom difficulty

Each biome uses its `biomes:` rows: `star chances` (% chance of 0 to 5 stars) and `mutation chance` (chance of each
mutation, by stars). Each mutation rolls separately, up to `max mutations` (1). The world tier multiplies them by
`star boost` and `mutation boost`. Defaults:

| Biome | Star chances (0-5 stars) | Mutation chance at 0 stars |
| --- | --- | --- |
| Meadows | 73, 10, 10, 5, 1, 1 | 2.5% |
| Black Forest | 62, 15, 12, 6, 3, 2 | 3.5% |
| Swamp | 52, 18, 14, 8, 5, 3 | 4% |
| Mountain | 42, 20, 17, 10, 7, 4 | 5% |
| Plains | 32, 22, 20, 13, 8, 5 | 6% |
| Mistlands | 22, 22, 22, 16, 11, 7 | 6.5% |
| Ashlands, Deep North | 16, 22, 26, 21, 11, 4 | 7.5% |
| Ocean | 68, 12, 10, 6, 3, 1 | 3% |

# Kitchen and Feasts

Cook times, burning, the fermenter and feasts: section `9. Kitchen` plus one section per cooking station.

## 9. Kitchen

| Key | Default | Range | Meaning |
| --- | --- | --- | --- |
| `Cook Time Multiplier` | 1 | 0.01 to 100 | multiplier on every recipe's cook time on every cooking station and oven; 0.5 cooks twice as fast |
| `Food Can Burn` | true | | true: cooked food left on a station burns at twice its cook time, as in the game; false: it never burns and waits until taken |
| `Fermentation Time` | 0 | 0 to 86400 | seconds a fermenter brews a batch; 0 keeps each barrel's own time (the game's: 2400, 40 minutes) |
| `Batch Yield` | 0 | 0 to 100 | meads tapped from one batch; 0 keeps each recipe's own yield |
| `Feast Servings` | 0 | 0 to 100 | servings a placed feast holds; 0 keeps each feast's own count |

## Cook times per station

Each cooking station has a section named after its prefab: `piece_cookingstation`, `piece_cookingstation_iron`,
`piece_oven`, and stations from other mods. It has one entry per recipe, named after the raw item: the seconds it
takes to cook (0.1 to 86400, default the game's time). The same food can cook on several stations, each with its own
time. The cauldron has no section: its recipes are crafted at once.

```
[piece_cookingstation]
RawMeat = 60
```

The time used is the entry times `Cook Time Multiplier`.

- Changes reach food already on the fire. Burnt food stays burnt; food already done stays done when the time is
  raised.
- With `Food Can Burn` switched back on, food on the fire for more than twice its cook time burns at once.

## Fermenter

`Fermentation Time` and `Batch Yield` are one value for every barrel and every mead.

- `Fermentation Time` also applies to barrels already brewing: a shorter time makes one that has brewed longer ready
  at once; a longer time can turn a ready, untapped barrel back to brewing.
- `Batch Yield` applies when a batch is tapped, so it covers batches already brewing.

## Feasts

- Feasts nobody has eaten from follow a `Feast Servings` change at once; a started feast keeps the servings it has
  left.
- Deconstructing a feast never gives back more than was placed, even after `Feast Servings` was lowered below what a
  started feast has left.
- The table's look (how full it is) catches up at the next serving or the next time the area loads.
- Each feast's food has its own food section, named after its prefab; its values and the global multipliers set what
  eating from the placed feast gives. See [Foods and Meads](wiki:Foods and Meads).

# Foods and Meads

Every food and mead, modded ones included, has its own section named after its prefab (`[CookedMeat]`,
`[MeadHealthMinor]`), defaulting to the item's own values. A food is any consumable that gives health, stamina or
eitr, plus each feast's food; a mead is any other consumable whose effect changes your stats.

Tooltips show the configured values. A change reaches items you carry, on the ground, in an open chest and foods
already eaten at once; a running mead keeps the same share of its effect left.

## Global multipliers

In `0. Global Settings`, default 1, applied to every food (not meads): `Health = 40` with `Health Modifier = 1.5`
gives 60.

| Key | Multiplies every food's |
| --- | --- |
| `Health Modifier` | health |
| `Stamina Modifier` | stamina |
| `Duration Modifier` | duration (`Food Rate` in `7. World Rates` also applies) |
| `Health Regen Modifier` | health healed per tick |
| `Eitr Modifier` | eitr |

## Food sections

| Key | Default | Meaning |
| --- | --- | --- |
| `Health`, `Stamina`, `Eitr` | the food's own | maximum health, stamina and eitr it adds |
| `Duration` | the food's own | seconds it lasts |
| `HealthRegen` | the food's own | health healed per tick (every 10 seconds) |
| `Vigor` | 0 | extra percent of stamina regeneration while active |
| `EitrVigor` | 0 | extra percent of eitr regeneration while active |

Vigor is explained in [Regeneration and Stamina](wiki:Regeneration and Stamina).

## Mead sections

| Key | Meaning |
| --- | --- |
| `Duration` | seconds the effect lasts, and so its cooldown |
| `HealthOverTime`, `StaminaOverTime`, `EitrOverTime` | total health, stamina, eitr restored over the effect |
| `HealthRegenMultiplier`, `StaminaRegenMultiplier`, `EitrRegenMultiplier` | regeneration multipliers while it lasts (1 = no change) |
| `RunStaminaModifier`, `JumpStaminaModifier` | added fraction of the running or jumping cost: -0.2 = 20% cheaper |

A mead cannot be drunk while its own effect, or one of its group (the healing meads, for example), still runs:
`Duration = 30` on a healing mead allows the next one after 30 seconds.

## Degradation and eating again

All in `0. Global Settings`.

| Key | Default | Meaning |
| --- | --- | --- |
| `Disable Food Degradation` | false | true: food keeps full strength until it runs out (overrides the curve) |
| `Degradation Curve` | 0.3 | strength = remaining fraction ^ curve; higher fades sooner |
| `Eat Again At` | 0.5 | fraction (0 to 1) of a food's duration below which it can be eaten again; 1 = any time, 0 = only once it ran out |

| Curve | Half the time left | A quarter left | A tenth left |
| --- | --- | --- | --- |
| 0 (no fading) | 100% | 100% | 100% |
| 0.3 (the game) | 81% | 66% | 50% |
| 1 (linear) | 50% | 25% | 10% |
| 2 (fades early) | 25% | 6% | 1% |

Your maximum values and the HUD food bars follow the curve.

## Food slots

`Food Slots` (default 3, range 1 to 5): how many foods a player can have active at once.

- The HUD shows that many slots, empty ones included.
- A new food takes a free slot. With every slot used it replaces the most depleted food if any can be eaten again;
  otherwise you are full.
- Health, stamina and eitr add up over all foods, so more slots mean much more of each; the global multipliers can
  balance that.
- Lowering the count keeps the extra foods until they run out; until then a new food can only replace one that can be
  eaten again.
- Feast food fills a slot like any other.
- Use only one mod that changes the number of food slots; with another, leave `Food Slots` at 3.

## Auto eat

When a food runs out, another of the same food is eaten from your inventory, with its sound and animation (never on
the station you are looking at). With none left nothing is eaten. Both switches must be on: `Allow Auto Eat`
(`0. Global Settings`, default false, the server's choice) and `Auto Eat` (`8. Display`, default true, each
player's own).

## HUD (8. Display)

Each player's own choice, never taken from the server.

| Key | Default | Meaning |
| --- | --- | --- |
| `Hide Health Number`, `Hide Stamina Number`, `Hide Eitr Number` | false | hide the number on that bar |
| `Food Timers` | Vanilla | time left under each food icon: Vanilla and Always show it (the game always does), Never hides it |
| `Auto Eat` | true | see Auto eat |

# Regeneration and Stamina

How health, stamina and eitr come back, what stamina costs, and the base values, skills and world rates behind them:
sections `1.` to `7.`. Every default is the game's behaviour.

## 1. Health Regeneration

`Continuous Food Healing` (default false). Off, the game heals your foods' total `HealthRegen` once every 10
seconds. On, the same amount is healed a little every frame, without floating heal numbers. Mead and Rested bonuses
still apply.

## 2. Stamina Regeneration

### Vigor

Vigor is a percentage added to stamina regeneration while a food is active. It does not fade with the food.

| Key | Default | Meaning |
| --- | --- | --- |
| `Vigor Per Stamina Point` | 0 | percent per point of a food's stamina (after `Stamina Modifier`) |
| `Vigor Multiplier` | 1 | scales every food's Vigor |
| `Vigor` (food section) | 0 | that food's extra Vigor |

A food's Vigor = (its `Vigor` + its stamina x `Vigor Per Stamina Point`) x `Vigor Multiplier`; all active foods add
up. Example: 0.1 makes a 60-stamina food +6%, three of them +18%. The tooltip shows `Vigor: +X% stamina regen`.

### Extra stamina

| Key | Default | Meaning |
| --- | --- | --- |
| `Regen Per Extra Stamina Point` | 0 | percent of regeneration per point of stamina above the base |
| `Count Food Stamina Only` | false | off: all maximum stamina above `Base Stamina` and skill stamina (food, meads, gear, other mods); on: only your foods' current stamina |

Example: 0.2 with 150 extra stamina gives +30%. Raising `Base Stamina` lowers the bonus. Vigor and this setting both
reward food stamina; with both on it counts twice, so use one.

### Curve, sneaking, encumbered, swimming

| Key | Default | Meaning |
| --- | --- | --- |
| `Regen Curve Strength` | 1 | 1 = off. Above 1: x strength on an empty bar, x1 at the pivot, divided by strength on a full bar, straight lines between. Below 1: the opposite |
| `Regen Curve Pivot` | 0.5 | bar fraction (0 to 1) where the curve is x1 |
| `Sneak Skill Regen Bonus` | 0 | percent added while crouched and standing still, at Sneak 100; scales with the skill |
| `Encumbered Regen Fraction` | 0 | regeneration while encumbered, as a fraction (0 to 1) of normal; 0 = none (game) |
| `Swimming Regen Fraction` | 0 | regeneration in the water, as a fraction (0 to 1) of normal; 0 = none (game) |
| `Swimming Regen Delay` | 3 | seconds of treading water or floating still before swimming regeneration starts |

- The curve stacks with the game's low-bar bonus and `Low Stamina Regen Bonus`. Strength 2, pivot 0.4: x2 at 0%,
  x1.5 at 20%, x1 at 40%, x0.75 at 70%, x0.5 at 100%.
- Encumbered: walking still drains; you regenerate standing still. Encumbered while swimming uses the encumbered
  fraction. Attacking, dodging, wall running and `Stamina Regen Delay` still stop regeneration.

### Basics and Rested

| Key | Default | Meaning |
| --- | --- | --- |
| `Stamina Regen Multiplier` | 1 | scales the base stamina regeneration |
| `Low Stamina Regen Bonus` | 1 | scales the game's bonus for a low bar; 0 removes it |
| `Stamina Regen Delay` | 1 | seconds after any stamina use before regeneration resumes |
| `Blocking Regen Factor` | 0.8 | regeneration factor while holding block (replaces the game's 0.8; 1 = no slowdown) |
| `Rested Duration` | game's | seconds Rested lasts at comfort 1 |
| `Rested Duration Per Comfort` | game's | seconds added per comfort level above 1 |
| `Rested Stamina Regen` | game's | stamina regeneration multiplier while rested (1 = no bonus) |
| `Rested Health Regen` | game's | health regeneration multiplier while rested (1 = no bonus) |
| `Rested Eitr Regen` | game's | eitr regeneration multiplier while rested (1 = no bonus) |

A new Rested duration applies from your next rest; the Rested multipliers apply at once, also to a running Rested,
and combine with the other rules like a mead's.

Order: (1) base regeneration and the low-bar bonus, scaled by `Stamina Regen Multiplier` and
`Low Stamina Regen Bonus`; (2) times the mead and Rested multiplier plus Vigor, the extra stamina bonus and the sneak
bonus; (3) times the curve, and `Blocking Regen Factor` while blocking; (4) times the world `Stamina Regen Rate`.

## 3. Eitr Regeneration

The same rules for eitr.

| Key | Default | Meaning |
| --- | --- | --- |
| `Eitr Vigor Per Eitr Point` | 0 | percent of eitr regeneration per point of a food's eitr |
| `Eitr Vigor Multiplier` | 1 | scales every food's Eitr Vigor |
| `EitrVigor` (food section) | 0 | that food's extra Eitr Vigor |
| `Eitr Regen Multiplier` | 1 | scales the base eitr regeneration |
| `Eitr Regen Delay` | 1 | seconds after any eitr use before regeneration resumes |
| `Eitr Regen Curve Strength` | 1 | 1 = off; the stamina curve, on the eitr bar |
| `Eitr Regen Curve Pivot` | 0.5 | eitr bar fraction (0 to 1) where the curve is x1 |
| `Blocking Eitr Regen Factor` | 0.8 | eitr regeneration factor while blocking; 1 = no slowdown |

Eitr Vigor is built like Vigor and shown as `Eitr Vigor: +X% eitr regen`. Your equipment's eitr regeneration bonus
is added afterwards, so the curve does not scale it.

## 4. Stamina Costs

Multipliers on the stamina players use: 1 = unchanged, 0 = free, 2 = double. Gear, meads and the game's own skill
discounts still apply, and every cost also multiplies with the world `Stamina Rate`.

| Key | Scales | Also multiplies with |
| --- | --- | --- |
| `Run Cost` | running | `Move Stamina Rate`, out of combat, a mead's `RunStaminaModifier` |
| `Jump Cost` | jumping | `Move Stamina Rate`, out of combat, `Skill Discount`, a mead's `JumpStaminaModifier` |
| `Dodge Cost` | dodging | out of combat, `Skill Discount` |
| `Block Cost` | normal and perfect blocks | `Skill Discount` |
| `Attack Cost` | attacks with any weapon or item but the hammer, hoe and cultivator | |
| `Sneak Cost` | sneaking | out of combat |
| `Swim Cost` | swimming | `Move Stamina Rate` |
| `Encumbered Cost` | walking while encumbered | |
| `Tool Cost` | hammer, hoe, cultivator: placing, repairing, removing, and their swings | |
| `Fishing Pull Cost` | reeling in, including the fish's pull | |
| `Fishing Hooked Cost` | holding a hooked fish | |
| `Harpoon Cost` | pulling a harpooned creature | |
| `Drowning Damage` | damage per second while swimming without stamina (0 = harmless) | |

All default to 1. A jump is checked against its reduced cost, so a cheap jump is never refused for lack of stamina.

| Key | Default | Meaning |
| --- | --- | --- |
| `Out Of Combat <X> Cost` for Run, Jump, Dodge, Sneak | 1 | extra multiplier while no enemy targets you or has noticed you and you are not attacking |
| `Free Sneaking Without Enemies` | false | sneaking is free while no enemy is in stealth range and none has noticed or targeted you; overrides both sneak costs |
| `Skill Discount` | 0 | percent off block, dodge and jump costs at Blocking, Dodge and Jump 100, scaling with the skill |

Examples: `Run Cost = 0.5` with `Out Of Combat Run Cost = 0.5` makes running half price in a fight and a quarter on
a quiet walk. `Skill Discount = 20` takes 10% off jumps at Jump 50, 20% at Jump 100; a high Dodge skill also keeps
the game's own dodge discount.

## 5. Base Values and 6. Skills

| Key | Default | Meaning |
| --- | --- | --- |
| `Base Health` | 25 | health with no food |
| `Base Stamina` | 75 | stamina with no food |
| `<Skill> Skill Stamina` for Run, Jump, Sneak, Swim, Fishing | 0 | stamina added at that skill's level 100, in a straight line (30 adds 15 at level 50) |
| `<Skill> Skill Gain` for Run, Jump, Sneak, Swim, Fishing | 1 | multiplier on the experience that skill earns (0 = none); the world's skill rate still applies |

Skill stamina shows on the HUD and does not count as extra stamina for `Regen Per Extra Stamina Point`.

## 7. World Rates

Each overrides one of the world's modifiers while above 0 (default 0 = the world's own). Changes apply at once; back
to 0 restores the world's value.

| Key | Overrides | Notes |
| --- | --- | --- |
| `Food Rate` | how fast food runs out | 2 = timers run twice as fast; applies with `Duration Modifier`; `Eat Again At` uses the result |
| `Stamina Rate` | all stamina use | multiplies with every cost above |
| `Move Stamina Rate` | running, jumping, swimming | multiplies with their costs and `Stamina Rate` |
| `Stamina Regen Rate` | stamina regeneration | multiplies with `Stamina Regen Multiplier` |

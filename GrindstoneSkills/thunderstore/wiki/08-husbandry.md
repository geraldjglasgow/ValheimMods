# Husbandry Skill

A new skill for taming, breeding and keeping animals. **Trained by** being near your animals as they tame, eat and give birth, and by petting, butchering and harvesting honey. Most perks use the **keeper**: the best Husbandry level among players near the animal (taming uses anyone within 60 m).

| Level | Milestone | What |
| --- | --- | --- |
| 20 | Animal Lore | an animal's or egg's hover shows its timers (fed, love, pregnancy, herd, growing up, hatching) |
| 25 | Animal Feeder | build a feeder (hammer, Misc) that hungry tamed animals eat from |
| 50 | Calm | a creature you are taming neither flees from nor attacks you |

Perks grow evenly from 0 at level 0 to the value at 100.

## General (23 - Husbandry)

| Setting | Default | Meaning |
| --- | --- | --- |
| Husbandry Enabled | true | Turns Husbandry on or off; levels are kept. |
| Keeper Range | 30 | Metres within which a player counts as an animal's keeper and earns experience from it. |
| Animal Lore Level | 20 | Level for Animal Lore. |
| Show Callouts | true | Floating words like Twins!. Per player. |

## Taming (24 - Taming)

| Setting | Default | Meaning |
| --- | --- | --- |
| Taming Speed At 100 | 100 | % faster taming (100 = half the time). |
| Fed Duration At 100 | 100 | % longer an animal stays fed (game: 10 min). |
| Taming Levels | (empty) | Creatures that need a keeper of a set level to tame, e.g. `Lox:30, Moose:60`. |
| Calm Level | 50 | Level for Calm. |
| Calm Break Time | 120 | Seconds a creature stays wary after you hurt it. |

## Breeding (25 - Breeding)

**Petting:** press E on a tamed animal you cannot command (not wolves): it becomes content and breeds faster.

| Setting | Default | Meaning |
| --- | --- | --- |
| Breeding Speed At 100 | 100 | % faster breeding. |
| Herd Size At 100 | 4 | More animals of a kind allowed nearby before breeding stops. |
| Growth Speed At 100 | 100 | % faster growing up and egg hatching. |
| Better Offspring At 100 | 25 | % chance a newborn or egg is one star above its parent. |
| Max Offspring Level | 3 | Highest level that reaches (3 = two stars). |
| Twins At 100 | 25 | % chance of a second birth or egg. |
| Content Duration | 600 | Seconds a petted animal stays content. |
| Content Breeding Bonus | 50 | % faster breeding while content. |

## Animal yield (26 - Animal Yield)

| Setting | Default | Meaning |
| --- | --- | --- |
| Butcher Yield At 100 | 50 | % more drops from tamed animals you kill (never trophies). |
| Prime Cuts | false | Meat from starred tamed animals carries their stars. |
| Produce Chance At 100 | 50 | % chance per interval that a fed tamed animal drops feathers, scraps, pelts or hides. |
| Produce Interval | 1200 | Seconds between produce rolls. |
| Extra Honey At 100 | 50 | % chance of one more honey per honey. |

## Companions (27 - Companions)

| Setting | Default | Meaning |
| --- | --- | --- |
| Pack Damage At 100 | 50 | % more damage by a tamed wolf following you. |
| Pack Toughness At 100 | 33 | % less damage taken by it. |
| Feeder Level | 25 | Level to build the Animal Feeder. |
| Feeder Range | 10 | Metres an animal walks to a feeder. |
| Feeder Recipe | Wood:10, LeatherScraps:4 | The feeder's cost, at a workbench. |

## Experience (28 - Husbandry Experience)

All but honey is times the creature's **tier**: boar and hen 1, wolf 2.5, lox, asksvin and moose 4.3.

| Setting | Default | Meaning |
| --- | --- | --- |
| Experience Multiplier | 1 | Multiplies all Husbandry experience. |
| Taming Experience | 20 | A whole taming near you. |
| Tamed Experience | 10 | A creature near you becomes tame. |
| Discovery Multiplier | 3 | Multiplier for your first tame of each kind. |
| Feeding Experience | 1 | An animal eats near you. |
| Birth Experience | 3 | A birth or egg near you. |
| Petting Experience | 1 | Petting an animal that is not content. |
| Butchering Experience | 5 | Killing a tamed animal. |
| Honey Experience | 0.5 | Per honey harvested. |

With Elite Creatures Reborn, see [Configuration And Compatibility](wiki:Configuration And Compatibility).

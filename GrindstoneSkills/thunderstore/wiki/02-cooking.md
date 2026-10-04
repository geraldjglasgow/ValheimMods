# Cooking Skill

The game's own skill: your level counts and its shorter crafting stays. **Kitchens:** cooking stations, oven, cauldron, mead cauldron, prep table, fermenter, and other mods' kitchens that use Cooking. **Trained by** what the game rewards (food on, dish off, crafting), scaled by the dish, plus tapping a fermenter. There is no on/off switch: set a perk to 0.

## Perks (4 - Kitchen)

Perks grow evenly from 0 at level 0 to the value at 100.

| Setting | Default | Meaning |
| --- | --- | --- |
| Cooking Speed At Level 100 | 50 | % faster cooking on stations and the oven, by the level of whoever put the food on. |
| Extra Burn Time At Level 100 | 100 | Extra time before a done dish burns, in % of its cook time (100: burns at 3× instead of 2×). |
| Fermenting Speed At Level 100 | 30 | % faster fermenting, by the level of whoever put the base in. |
| Extra Food Chance At Level 100 | 50 | % chance of extra dishes at every kitchen but the fermenter; replaces the game's 25%. |
| Extra Food Amount | 1 | Dishes per bonus. |
| Ingredient Save Chance At Level 100 | 10 | % chance per item crafted at the cauldron, mead cauldron or prep table to get one ingredient back. |
| Trash Filter | true | Kitchen trash filters on. Off, kitchens keep everything. |

## Trash filter

Shift + E on a kitchen (not the fermenter; on the oven, its add-food switch) cycles: all dishes, 1+, 2+, 3 stars. It is shared and needs ward access to change. Dishes below it are thrown away, into compost if a bin is within 20 m. While on, Shift + E at crafting stations cycles it instead of opening them. **Since 0.13.0 dishes have 0 stars, so anything but "all dishes" throws away every dish.**

## Experience (5 - Experience)

The game's amounts (0.4 food on, 0.6 dish off, 1 per crafted item) times the dish's **tier**: health + stamina + eitr ÷ 50, from 1 to 3. A trashed dish still pays its cook 0.6 × tier.

| Setting | Default | Meaning |
| --- | --- | --- |
| Experience Multiplier | 1 | Multiplies all Cooking experience. |
| Tier Scaling | true | Richer dishes teach more (the tier). |
| Tier Reference Food Value | 50 | Food value of tier 1. |
| Tier Maximum Multiplier | 3 | Highest tier. |
| Discovery Multiplier | 3 | Multiplier for a character's first make of each dish. |
| Fermenter Tap Experience | 1 | Per fermenter tap, times the mead's tier. |

Dishes starred before 0.13.0 keep their stars and eat bonus: see [Skill Book, Stars And Death](wiki:Skill Book, Stars And Death).

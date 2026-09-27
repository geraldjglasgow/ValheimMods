# Changelog

## 0.2.0

- New skill: Sailing, trained by steering a ship (the crew earn a share).
- Ships you build get more health, up to 50% at level 100.
- Ships you steer sail and row faster, up to 20% at level 100.
- You uncover more of the map while aboard a ship, up to twice the radius at level 100.
- Lookout, from Sailing 50: press O aboard a ship to send a pulse 100 m out; everyone aboard sees the name tags of the
  enemies it reached for 30 seconds. 60 second cooldown.
- The console's `raiseskill` and `resetskill` know Sailing.

## 0.1.0

- First version: Cooking. Built on the game's own Cooking skill, so existing levels count.
- Dishes get 0 to 3 stars, rolled from the cook's level when they finish on a cooking station or oven, or when they
  are crafted at the cauldron, mead cauldron or prep table. Meads take the stars of their base. Starred ingredients
  improve the odds.
- Stars boost a dish's health, stamina and eitr and make it last longer.
- Trash filter: Shift+E on a kitchen sets the fewest stars it keeps; dishes below are thrown away.
- Kitchen perks from the cook's level: faster cooking and fermenting, more time before food burns, extra food (up to
  50% at level 100), a chance to keep an ingredient.
- More Cooking experience for richer dishes and the first time you make each dish.
- Skill loss on death is configurable for every skill: the percent of each level lost (5 by default, as in the game)
  and whether progress toward the next level is lost (on by default).

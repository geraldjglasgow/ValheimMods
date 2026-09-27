# Changelog

## 0.3.0

- New: Woodcutting, built on the game's own skill, so existing levels count. Every perk grows from nothing at level
  0 to its full value at level 100.
- Timber!: a tree falls away from where you chopped it, pushed harder the better you are, and "Timber!" floats above
  it. Logs from your own trees hurt you less, not at all at level 100.
- Domino felling: a log from your tree hits other trees, logs and stumps up to three times harder, so one fell can
  knock over the next, up to five deep. Chain fells count as yours.
- Clean splits: up to one hit in five splits a log at once, and a cleanly split log gives 50% more wood.
- Old growth: the biggest trees of each kind give up to twice the wood.
- Finds: a falling tree sometimes hides a bird's nest, a wild hive, a lost purse or something rarer, from 2% of trees
  at level 0 to 15% at level 100. Server admins set the finds per biome and tree in `GrindstoneSkills.Finds.yml`.
- Swings that hit wood give back up to 30% of their stamina and wear the axe up to 50% less.
- Clean fell: the stump comes out with the tree. Replanting: up to half the time a sapling of the same kind takes root
  where the tree stood.
- More Woodcutting experience for harder wood, felled trees and broken logs, triple for the first tree of each kind;
  less for saplings and tiny trees.
- New store icon.

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

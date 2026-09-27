# Changelog

## 0.5.0

- New: Foraging, a skill of its own for what you pick in the wild: berries, mushrooms, thistle, dandelions,
  fiddleheads, royal jelly, and the flint, stones and branches lying about. In the game those picks trained Farming;
  now they train Foraging, and crops stay with Farming.
- Stars on what you pick: berries, mushrooms and herbs come off the plant with 0 to 3 stars, rolled from your
  Foraging level with the cooking odds. Starred forage gives more when eaten raw and is a starred ingredient in the
  kitchen. Flint, stones, branches, wild barley and wild flax never carry stars.
- Best time: every plant has its moment (berries on a dry day, mushrooms in the rain, thistles at night). Picked then,
  it rolls its stars as if you were 20 levels higher, and looking at a plant tells you when.
- Extra yield: up to a 50% chance of one more at level 100 (the game's own bonus stopped at 25%).
- Sweep picking, from level 25: picking a plant also picks every plant of the same kind around it, up to 4 m away at
  level 100.
- Foraging experience for every pick, more in later biomes, half for flint, stones and branches, and triple the first
  time you pick each kind.
- What counts as foraging is listed in `GrindstoneSkills.Forage.yml`: synced from the server, reloaded while the game
  runs, and open to items from other mods.
- Changed: berries and mushrooms can carry stars now, so a 0★ one counts toward a dish's ingredient stars where it
  used to be left out.
- `raiseskill` and `resetskill` work for Foraging as they do for Sailing.

## 0.4.0

- New: Pickaxes, built on the game's own skill, so existing levels count. A good miner learns to read stone.
- Seams: now and then a chunk next to the one you are mining glints gold for a few seconds, more often and for longer
  the better you are. Only you see your own seams.
- Clean strikes: hit the glinting chunk in time for double damage, a little experience and, on an ore deposit, an
  extra drop; the next seam opens at once. From level 100 (Unbroken) each clean strike in a chain hits harder, up to
  ×4.
- Splash: your swing also damages the chunks touching the one you hit, 15 at level 100 shared between them, growing
  every 10 levels.
- Rich veins: ore deposits have 0 to 3 stars, each worth 25% more drops, for everyone who mines them. From level 25
  (Read the rock) the hover shows a deposit's stars and how many chunks are left.
- Extra ore: up to a 30% chance of an extra drop from every chunk of an ore deposit. Pickaxes wear up to 50% less on
  rock.
- Echo, from level 50: a swing on rock pings the nearest ore deposit within 40 m and shows its name and distance.
  Buried silver stays the Wishbone's job.
- Finds: broken rock sometimes holds amber, an amber pearl or a ruby, richer further into the world. Server admins set
  the finds per biome and deposit in `GrindstoneSkills.MineFinds.yml`.
- More Pickaxes experience in later biomes and on ore deposits, plus experience for clean strikes and for the first
  hit on each kind of deposit.

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

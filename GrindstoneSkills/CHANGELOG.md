# Changelog

## 0.12.0

- **Wind Call, Sailing's level 25 active.** Press K aboard a ship: the wind turns to blow the way you look, for
  everyone aboard, for 60 seconds, then the weather's own wind returns. Look ahead for a wind at your back, or
  anywhere else. Only the direction changes; the weather still decides how strong it blows. Once every 3 minutes per
  player. While it blows it takes the place of Moder's tailwind; players ashore and on other ships keep the weather's
  wind. New section `35 - Wind Call`: `Wind Call Level` (25), `Wind Call Duration` (60), `Wind Call Cooldown` (180),
  synced, and `Wind Call Key` (K), each player's own.
- The Lookout key now fires while you hold W, and never while you type in chat.

## 0.11.0

- Changed: Riposte no longer staggers what it hits. A parry still powers up the attack you start within 2 seconds,
  and its melee hits still deal 25% more (`Riposte Damage`); the bigger hit staggers a creature only as any hit that
  size would. No setting was added or renamed.

## 0.10.0

- New: a skill book. The skills panel is wider and taller, and a page beside the list shows the skill you click: its
  icon, your level (a food or mead bonus in green) and how far you are to the next, then in a few short lines what the
  skill does for you at your level, with the numbers. Every skill has a page, the game's own (Swords, Run, Crafting...)
  as well as this mod's; a skill from another mod shows its description.
- Perks and milestones (Riposte, Shield Wall, Lookout...) are listed on the page with the level they need, or
  "unlocked". Hover a name, or any gold word such as Poise or Timber!, for what it does, in a bordered box beside it.
- The list shows about four more skills, the page you looked at last opens again next time, and with a gamepad the
  page follows the selected skill. The entries' own hover tooltips are gone: the page shows the same and more.
- Removed: the Defense plate in the inventory's stat column, with its `Show Plate` setting. Defense's page in the
  skills panel shows every bonus and each milestone instead. An old `Show Plate` line in the .cfg does nothing.
- Seams open where you can strike them: on the chunk you are mining, unless your swing broke it, or on another chunk
  within your pickaxe's reach of where you stand. They used to skip every chunk the swing touched and could open a
  step out of reach.
- Seams stay open longer: 2.6 seconds at level 0 and 5.6 at level 100 by default, instead of 2 and 5. A config written
  by an earlier version keeps the values it saved: set `Seam Window At 0` and `Seam Window At 100` there (on a
  server, in the server's config). The glint is smaller and softer, so it marks the chunk without hiding it.
- Fixed: rocks that break into chunks (copper and silver deposits, boulders, mud piles) were all taken for whichever of
  them the mod met first in a session. After a boulder, a copper deposit counted as plain stone: no "Rich vein" line,
  no extra ore, no discovery, and Echo passed it by; after a deposit, a boulder could count as ore. Each rock is now
  read for what it is.
- Every skill of the mod has a new icon in the skills panel and in its level-up messages, closer to the game's own:
  one plain object each, without the gold wreaths and the scenery.

## 0.9.1

- Woodcutting and Pickaxes have their own icons in the skills panel and in their level-up messages.

## 0.9.0

- New: Fishing, built on the game's own Fishing skill, so existing levels count. In the game it only makes you reel
  faster and tire less; here perks grow from nothing at level 0 to their full value at level 100, with milestones at
  levels 25, 50 and 75.
- The fight on the line:
  - Reeling while a hooked fish thrashes strains the line. A bar under the crosshair shows the tension, and when it is
    full the line snaps. Ease off while it thrashes, reel while it rests.
  - Every thrash is shorter than the last. After four (eight for a legendary) the fish is spent and comes in faster.
  - The strike window grows from the game's 0.5 seconds to 1 second. Start reeling the moment it nibbles for a perfect
    strike: the fish skips its first thrash.
  - From level 75, running out of stamina lets the fish take line for a few seconds instead of losing it, once per
    fish.
- Bites:
  - Up to twice as many at level 100, and more at dawn, at dusk and in the rain.
  - Bait crafted at the prep table carries Cooking stars like food. Starred bait bites more, hooks more big ones and
    comes back with its stars.
  - Entrails and blood bags dropped in the water are chum: fish bite twice as often near them until they dissolve a
    minute later.
- Big fish:
  - A hooked fish can grow a level on the line ("It's a big one!"), up to level 5, and more often at night. Everyone
    nearby sees it grow.
  - One fish in two hundred is born legendary: level 6, three times the size, glowing. Only anglers of level 50 or more
    can hook one, it always brings its bonus item, and the whole server hears when someone lands one.
- The angler's senses: from level 25 a nibble names the fish and a cast tells you the fishing conditions, from 50 it
  also tells the fish's size, and from 75 a cast tells you which fish in reach take your bait.
- The angler's log: every catch is weighed, and every species and size you land and your records are kept. Fish
  tooltips show them, and `/fishlog` in chat lists them.
- Catches:
  - A fish's bonus item comes up to twice as often at level 100, and from level 50 it can be two.
  - Up to 30% of catches give the bait back.
  - Casts go up to 30% further and the line is up to 50% longer.
  - A cast left in the water can snag coins, trinkets and whatever sinks in that biome. What each biome holds is in the
    new `GrindstoneSkills.Snags.yml`.
- Fish for the kitchen: each level of a fish above 1 improves the Cooking stars of the raw fish it cleans into.
- Fishing experience for every fish landed, more for later species and bigger fish, and a bonus for each new species
  and size in your log. Reeling in an empty line no longer trains the skill. Every setting is in the new sections 50
  to 54 of the .cfg.
- Fishing has its own icon in the skills panel and in its level-up messages.

## 0.8.2

- Cooking and Sailing have their own icons in the skills panel and in their level-up messages.

## 0.8.1

- Farming has its own icon in the skills panel and in its level-up messages.

## 0.8.0

- New: Farming, built on the game's own Farming skill, so existing levels count. The planter's level decides how crops
  grow and ripen, the picker's level decides the harvest. Perks grow from nothing at level 0 to their full value at
  level 100.
- Stars on crops: crops ripen with 0 to 3 stars from the planter's level, with the same odds as dishes.
  - Starred crops stand taller and show their stars. They count as starred ingredients in the kitchen and give more
    when eaten raw.
  - Seeds carry stars too: each star is worth 10 levels (heirloom seeds).
  - Each other kind of crop growing within 2 m adds 5 levels (companion planting).
  - Up to 2% of crops ripen into a giant: always 3★, six times the crop.
- Growing:
  - Everything you plant grows up to 40% faster, and half again as fast in the rain.
  - Once a day the use key on a growing plant tends it and its neighbours: they gain a tenth of their growing time.
  - Crops need up to 40% less room.
  - From level 75 crops grow in the Ashlands without a shield, at level 100 in the Mountains and the Deep North.
- Harvest:
  - Up to 50% chance of a bonus crop (the game's own bonus stops at 25%) and up to 30% chance of the seed back.
  - Row planting from level 25: three in a row, five from level 50. Shift plants one.
  - Auto-replant from level 50: a picked crop's plant goes back in with a seed from your inventory, scythe included.
- The almanac: a growing plant's hover shows when it will be ripe, and from level 20 the odds of its stars.
- The windmill keeps stars: starred barley makes starred flour, starred oats starred oat flour.
- Compost bin: a new barrel in the cultivator's menu.
  - It turns food scraps, spare crops, entrails and bone fragments into compost.
  - It feeds the growing crops within 12 m: they grow 25% faster and ripen with better stars.
  - Dishes a kitchen's trash filter throws away nearby go in too.
- Farming experience: more for richer crops, five times for a giant, triple for the first pick of each kind of crop,
  and a little for tending. Every setting is in the new sections 29 to 32 of the .cfg.
- Crops and flour now count toward a dish's ingredient stars, so 0★ vegetables pull the average down where they used
  to be left out.

## 0.7.0

- New: Husbandry, a skill of its own for taming, breeding and keeping animals (boars, wolves, lox, asksvin, moose and
  hens). It trains from taming near you, each tame (triple for your first of each kind), feeding, births, petting,
  butchering and honey, and bigger animals train it faster. Animals follow the best keeper within 30 m.
- Taming up to twice as fast. From level 50 a creature you have started taming neither flees from you nor attacks you,
  so taming goes on while you stand beside it. Animals stay fed up to twice as long.
- Breeding up to twice as fast and up to 4 more animals per pen; up to a 25% chance that a newborn or egg is one star
  above its parent (up to two stars) and up to a 25% chance of twins; young animals and warm eggs grow up to twice as
  fast.
- More from your animals: up to 50% more drops from a tamed animal you butcher (never trophies), produce from living
  animals (feathers, leather scraps, pelts, hides), up to 50% extra honey, eggs from starred hens carry their stars
  into the kitchen, and Prime cuts (starred meat from starred animals, off by default).
- Petting makes an animal content for 10 minutes, and a content animal breeds 50% faster. A wolf following you deals
  up to 50% more damage and takes up to a third less.
- Animal Feeder, from level 25: a new piece in the hammer's Misc tab. Hungry tamed animals, and animals being tamed,
  within 10 m walk to it and eat the food you put in it.
- Animal lore, from level 20: an animal's or egg's hover shows how long until it is tamed, how long it stays fed, its
  love and pregnancy, the herd's room, and when it grows up or hatches.
- Optional taming levels let a server make lox, asksvin or moose need a keeper of some level to tame.
- With Elite Creatures Reborn 3.10.0 or later, Better offspring gives its newborns one star more. `raiseskill` and
  `resetskill` work for Husbandry. Every setting is in the new sections 23 to 28 of the .cfg.

## 0.6.0

- New: Defense, a skill of its own for how much punishment you can take, trained by blocking hits from creatures
  (a parry counts double, a block with a weapon half, the first block against each kind of creature triple) and by
  the hits that get through. Bigger hits train it faster; falls, fire and poison don't train it.
- At level 100: +25 max health, 10% more health from food, 10% less damage from every source, up to 1% of max health
  back every 10 seconds out of combat, 25% more poise, a parry window of 0.35 instead of 0.25 seconds, and blocks and
  dodges 10% cheaper in stamina.
- Guard perks, growing with your level: Reflex (your shield sometimes blocks a hit you didn't block), Shield Bash (a
  block sometimes staggers the attacker), Thorns, more adrenaline from blocks, less shield wear and knockback, and
  Desperation (damage reduction doubled below 25% health).
- Milestones: Riposte at 25 (a parry powers up your next attack and staggers), Shield Wall at 50 (players behind your
  shield take 10% less damage), Hardened at 75 (each hit that gets through hardens you for 8 seconds, up to 5 times)
  and Last Stand at 100 (a killing blow leaves you at 1 health, once every 10 minutes).
- A Defense plate in the inventory's stat column shows your damage reduction, with every bonus in its tooltip; the
  milestones show as status icons while they last.
- `raiseskill` and `resetskill` work for Defense. Every setting is in the new sections 19 to 22 of the .cfg.

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

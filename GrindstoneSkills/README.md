# GrindstoneSkills

Deeper skills for Valheim: Cooking, Sailing, Woodcutting, Pickaxes, Foraging, Husbandry, Defense and Farming. A good
cook makes better food: every dish comes out with 0 to 3 stars, stars make food stronger and last longer, and every
kitchen can throw away dishes below the stars you want. Built on the game's own Cooking skill, so the levels you
already have count. A good sailor builds tougher ships, sails them faster, sees more of the map at sea, and from level
50 can send out a lookout pulse that marks every enemy around the ship. A good woodcutter aims where trees fall, knocks one tree
into the next, splits logs in one blow, finds what trees hide, and gets more wood from the giants of the forest. A
good miner reads stone: strikes the seams that glint in the rock, knows a rich vein at a glance, hears where the next
deposit lies, and turns up amber and rubies. A good forager picks starred berries, mushrooms and herbs for the
kitchen, knows when each plant is at its best, and clears a whole patch in one sweep. A good keeper tames faster,
walks among the animals being tamed without a fight, breeds stronger young, and gets more from the herd. A good
defender takes a beating: more health, less damage, cheaper blocks and dodges, and from level 25 on a parry that
powers the riposte, a shield wall for the friends behind, and one last stand against a killing blow. A good farmer
grows starred crops from heirloom seeds, plants whole rows at once, now and then pulls a giant turnip, and feeds the
fields from a compost bin.

### Cooking

- **Stars on dishes.** A dish's stars are rolled from the cook's Cooking level when it finishes: on a cooking station
  or the oven when it is done, at the cauldron, mead cauldron or prep table when it is crafted. 0 stars is the
  vanilla dish; every dish you already own is 0 stars.
- **Stars make food better.** By default a 1★ dish gives 10% more health, stamina and eitr and lasts 10% longer, 2★
  gives 20% and 20%, 3★ gives 35% and 30%. A dish stays at full strength for its extra time, then fades as usual.
- **Better cooks, better odds.** At level 0 almost everything is 0★; at level 100 half of all dishes are 3★. The odds
  table is fully configurable.
- **Starred ingredients help.** A stew made from 3★ meat has better odds than one made from plain meat; above level
  100 only starred ingredients raise the odds further. You choose whether recipes use your lowest or highest stars
  first (lowest by default, so your best food is kept).
- **Meads keep their stars.** A mead base's stars pass through the fermenter into every mead it makes.
- **Trash filter.** Shift+E on a cooking station, oven, cauldron, mead cauldron or prep table sets the fewest stars
  it keeps: Keep all, 1★ and up, 2★ and up, or 3★ only. Dishes below are thrown away, so a late-game kitchen can
  hand out 3★ food only. The station shows your own chance of keeping a dish.
- **Kitchen perks from your level:** food cooks up to 50% faster and takes twice as long to burn, meads ferment up
  to 30% faster, extra food comes up to 50% of the time (the game's own bonus stops at 25%), and crafting at a
  kitchen sometimes gives back an ingredient.
- **Cooking experience** as the game gives it, more for rich late-game dishes, and triple the first time you make
  each dish.
- **Stars stay apart.** 1★ and 3★ stacks never merge, in inventories, chests or on the ground.
- **Multiplayer.** Stars are decided on the machine that owns the station, the cook's level travels with the food,
  and experience reaches the cook even when someone else's filter throws the dish away.

### Sailing

A new skill in the skills panel. Every perk grows from nothing at level 0 to its full value at level 100.

- **Tougher ships.** A ship you build gets up to 50% more health, set by your Sailing level when you place it.
- **Faster ships.** A ship you steer is up to 20% faster, under sail and at the oars, whoever else is aboard.
- **Wider exploration at sea.** While you are aboard a ship you uncover the map up to twice as far around you.
- **Lookout, from level 50.** Press O aboard a ship: a pulse races 100 m out over the water, and everyone aboard sees
  the name tags of the enemies it reached (serpents, drakes, anything hostile, but not bosses) for 30 seconds. Once a
  minute. Players nearby see the pulse too.
- **Experience** comes from steering: about 40 per kilometre sailed at the helm, and a quarter of that for everyone
  else aboard. Level 50 takes a few hours at the helm.
- **Multiplayer.** A ship's health is the same on every machine, and the speed follows whoever is at the helm, even
  when the ship's physics runs on another player's machine.

### Woodcutting

Built on the game's own Woodcutting skill, so the levels you already have count. In the game it only makes axes hit
trees harder; here every perk grows from nothing at level 0 to its full value at level 100.

- **Timber!** A tree falls away from where you chopped it, pushed up to ten times harder than in the game, so you
  decide where it lands. "Timber!" floats above every falling tree for everyone nearby.
- **Log safety.** Logs from your own trees hurt you less, not at all at level 100. Other players, creatures and
  buildings still take the game's damage.
- **Domino felling.** A log from your tree hits other trees, logs and stumps up to three times harder, so one good
  fell can knock over the next tree, and that one the next, up to five deep. While you're nearby, every tree in the
  chain counts as yours, and you see "Chain ×3!".
- **Clean splits.** Up to one hit in five splits a log at once, with a crack and "Clean split!", and a cleanly split
  log gives 50% more wood.
- **Old growth.** The biggest trees of each kind give up to twice the wood: trees grow at random sizes, and the
  larger half of each kind earns a growing share of the bonus.
- **Finds.** A falling tree sometimes hides something: a bird's nest, a wild hive, a squirrel's stash, a lost purse,
  and rarer things in every biome. From 2% of trees at level 0 to 15% at level 100. Server admins set what each
  biome or tree can hide in `GrindstoneSkills.Finds.yml`.
- **Easier chopping.** A swing that hits wood gives back up to 30% of its stamina and wears your axe up to 50% less.
- **Clean fell.** The stump comes out with the tree and drops its wood.
- **Replanting.** Up to half the time, a sapling of the same kind takes root where the tree stood, if it has room to
  grow. A log lying on it only delays it. Beech, birch, oak, fir and pine have saplings; swamp, snow, Mistlands and
  Ashlands trees are never replanted.
- **Experience** as the game gives it per swing, more for harder wood (birch and oak twice, Yggdrasil three times),
  a quarter for saplings and tiny trees, plus a bonus for every tree felled and every log broken, and triple the
  first time you fell each kind of tree.
- **Multiplayer.** Your level travels with every hit, and the machine that owns a tree or a log decides. Experience
  for a tree felled, a chain or a split log reaches you wherever the tree's physics runs.

### Pickaxes

The miner learns to read stone. Built on the game's own Pickaxes skill, so the levels you already have count. In the
game it only makes pickaxes hit harder and cost less stamina; here the perks grow with your level to their full value
at level 100, and milestones unlock at levels 25, 50 and 100.

- **Seams.** Now and then a swing on a rock that breaks into chunks (copper and silver deposits, boulders, mud piles)
  makes a chunk near where you hit glint gold, with a soft clink. From 10% of swings at level 0 to 40% at level 100;
  the seam stays open 2 seconds at level 0, 5 at level 100. Only you see your seams.
- **Clean strikes.** Hit the glinting chunk before it closes: ×2 damage, a little experience, "Clean strike!", and
  on an ore deposit the chunk drops twice when it breaks. The next seam opens at once, so one clean strike leads to
  the next ("Clean strike ×3!"); hitting the rock elsewhere or letting a seam close ends the chain. Plain stone gets
  seams too, but no extra drops. A pickaxe too weak for the rock gets no seams.
- **Unbroken, at level 100.** Each clean strike after the first in a chain hits harder: ×2.4, ×2.8 and so on up to ×4.
- **Splash.** Your swing also hits the chunks touching the one you struck, shared between them, once per swing however
  many chunks it touches. It grows every 10 levels: 1.5 damage from level 10, 7.5 from 50, 15 at level 100. Chunks it breaks drop as usual. Splash gives no experience.
- **Rich veins.** Every ore deposit has 0 to 3 stars: 25% of them 1★, 11% 2★, 4% 3★. Each star gives 25% more drops
  from every chunk, to everyone who mines it, whatever their level. A deposit's stars are the same for every player
  and never change.
- **Read the rock, from level 25.** Looking at an ore deposit shows "Rich vein ★★" or "Plain vein", and on a rock of
  chunks how many are left.
- **Extra ore.** Up to 30% of the ore deposit chunks you break drop twice. Chunks that fall when you mine away what
  held them up count as yours, for rich veins, extra ore and finds.
- **Easier mining.** A swing that hits rock wears your pickaxe up to 50% less.
- **Echo, from level 50.** A swing that hits rock sounds the Wishbone's ping from the direction of the nearest ore
  deposit within 40 m, with its name and distance ("Copper deposit, 32 m"). Once every 10 seconds, for you alone. It
  skips anything the Wishbone rings for, so buried silver stays the Wishbone's job.
- **Finds.** Broken rock sometimes hides one of the game's own valuables: a lump of amber, an amber pearl or a ruby,
  with more pearls and rubies the further the biome is along the game. From 0.2% of chunks at level 0 to 1% at level
  100; smaller chunks get less (a mud pile's a tenth), so they are no find farm. Everyone nearby sees "Found a ruby!".
  Server admins set what each biome or deposit can hide in `GrindstoneSkills.MineFinds.yml`.
- **Experience** as the game gives it per swing, 25% more for each biome further along the game (Black Forest ×1.25
  up to Ashlands and Deep North ×2.5) and half again on ore deposits, plus a bonus for every clean strike and the
  first time you mine each kind of ore deposit. Rock too hard for your pickaxe earns only the game's amount.
- **Multiplayer.** Your level travels with every hit, and the machine that owns a rock decides its splash, extra drops
  and finds, so all of it works on a dedicated server. Seams, Echo and Read the rock are yours alone; the drops and
  finds everyone sees. Like the rest of the mod, it must be installed on the server and on every client.

### Foraging

A new skill in the skills panel, for what you pick in the wild: berries, mushrooms, thistle, dandelions, fiddleheads,
royal jelly, and the flint, stones and branches lying about. In the game those picks train Farming; with Foraging they
train Foraging instead, while crops stay with Farming.

- **Stars on what you pick.** Berries, mushrooms and herbs come off the plant with 0 to 3 stars, rolled from your
  Foraging level with the same odds as cooked dishes. Starred forage gives more when eaten raw, and in the kitchen it
  is a starred ingredient, so a good forager's raspberries make better jam. Flint, stones, branches, wild barley and
  wild flax never carry stars.
- **Pick at the right time.** Every plant has its best moment: berries on a dry day, mushrooms in the rain, thistles
  at night. Picked then, it rolls its stars as if you were 20 levels higher. Look at a plant to see when it is best
  picked, or that it is at its best now.
- **Extra yield.** Up to half your picks give one more at level 100 (the game's own bonus stops at 25%).
- **Sweep picking, from level 25.** Picking a plant also picks every plant of the same kind around it: 1 m around it
  at level 25, 2 m at 50, 4 m at 100.
- **Experience** for every pick, 25% more for each biome further along the game (Black Forest ×1.25 up to Ashlands
  and Deep North ×2.5), half for flint, stones and branches, and triple the first time you pick each kind
  ("Discovered Thistle!").
- **Multiplayer.** Your level travels with each pick and the machine that owns the plant rolls the stars, so it works
  on a dedicated server. Like the rest of the mod, it must be installed on the server and on every client.

### Defense

A new skill in the skills panel, for how much punishment you can take. The game's Blocking skill makes your shield
stronger; Defense makes you tougher. It trains when you block hits and when hits get through.

- **Tougher body.** At level 100: +25 max health (food or no food), 10% more health from food (after cooking stars)
  and 10% less damage from everything, after armour.
- **Recovery.** After 10 seconds without attacking, blocking or getting hurt, you heal up to 1% of your max health
  every 10 seconds. Resting and meads boost it as they boost food regeneration.
- **Poise.** Up to 25% more stagger damage before you stagger or your guard breaks.
- **Wider parry window.** The game's 0.25 seconds grows to 0.35 seconds at level 100.
- **Cheaper blocks and dodges.** Up to 10% less stamina for each, on top of the game's own Dodge skill.
- **Reflex.** Up to a 10% chance that your shield blocks a hit from the front although you weren't blocking
  ("Reflex!").
- **Shield bash.** Up to a 15% chance that an ordinary shield block staggers the attacker, like a parry ("Bash!").
- **Thorns.** A melee hit you block sends up to 10% of the blocked damage back.
- **Adrenaline.** Up to 25% more adrenaline from blocks and parries, and 25% less lost to hits you didn't block.
- **Shield care and stand firm.** Blocking wears your shield or weapon up to 50% less, and pushes you back up to 50%
  less.
- **Desperation.** Below 25% health your damage reduction is doubled.
- **Riposte, from level 25.** A parry powers up the attack you start within 2 seconds: its melee hits deal 25% more
  and stagger what they hit (not bosses).
- **Shield Wall, from level 50.** While you block with a shield, other players within 4 m behind you take 10% less
  damage.
- **Hardened, from level 75.** Each hit that gets through makes you take 3% less damage for 8 seconds, stacking up to
  5 times.
- **Last Stand, from level 100.** A blow that would kill you leaves you at 1 health and untouchable for 2 seconds.
  Once every 10 minutes.
- **See it all.** Your damage reduction is shown on a plate in the inventory, with every Defense bonus in its
  tooltip, and Riposte, Shield Wall, Hardened, Last Stand and Desperation show as icons in the status column while
  they last.
- **Experience** for every hit from a creature you block (1 for a shield, half for a weapon, double for a parry,
  triple the first time you block each kind of creature) and half that for a hit that hurts you. Bigger hits give
  more: a troll's club gives more than twice what a greydwarf's slap does. Falls, fire and poison give nothing.
- **Multiplayer.** Everything happens on your own machine, where the game handles your damage; allies read your level
  for Shield Wall. Like the rest of the mod, it must be installed on the server and on every client.

### Husbandry

A new skill in the skills panel, for taming, breeding and keeping animals: boars, wolves, lox, asksvin, moose and hens.
Every perk grows from nothing at level 0 to its full value at level 100. Animals are tended by the best keeper within
30 m, so everyone near a pen helps it.

- **Faster taming.** Up to twice as fast, on top of the game's own taming boost.
- **Calm, from level 50.** A creature you have started taming no longer runs from you or attacks you, and you no
  longer frighten it, so taming goes on while you stand right beside it. Hit it and it defends itself against you for
  two minutes. Players below 50 still scare it.
- **Stay fed longer.** Animals stay fed up to twice as long after a meal (20 minutes instead of 10), so they tame,
  breed and heal longer between feedings.
- **Faster breeding and bigger herds.** Pregnancy is up to twice as fast, and up to 4 more animals of a kind may stand
  in a pen before breeding stops.
- **Stronger offspring.** Up to a 25% chance that a newborn, or an egg, is one star above its parent (the game passes
  the parent's level on unchanged), up to two stars. With Elite Creatures Reborn, the newborn gets one star more than
  it rolled there.
- **Twins.** Up to a 25% chance of a second birth, or a second egg, half a minute after the first ("Twins!").
- **Faster growing up.** Piglets, cubs, calves and chicks grow up, and warm eggs hatch, up to twice as fast.
- **More at the butcher's.** A tamed animal you kill drops up to 50% more meat, hides and feathers (never more
  trophies).
- **Produce.** A fed tamed animal with a keeper near now and then drops its own materials without being killed:
  feathers from hens, leather scraps from boars, pelts from wolves and lox, hides from moose.
- **Extra honey.** Up to half your honey comes with one more when you harvest a beehive.
- **Starred eggs.** Eggs from starred hens carry the hen's stars: they stack apart, show their star, and are starred
  ingredients in the kitchen.
- **Prime cuts** (off by default). Meat from a starred tamed animal carries its stars into Cooking.
- **Petting.** Petting a tamed animal makes it content for 10 minutes: it breeds 50% faster.
- **Pack leader.** A tamed wolf following you deals up to 50% more damage and takes up to a third less.
- **Animal Feeder, from level 25.** A new piece in the hammer's Misc tab (a barrel, Wood 10 and Leather scraps 4 at a
  workbench). Hungry tamed animals, and animals being tamed, within 10 m walk to it and eat the food you put in it.
- **Animal lore, from level 20.** Look at an animal to see how long until it is tamed, how long it stays fed, its love
  and pregnancy, how much room the herd has and how long it stays content; look at a young animal or a warm egg to see
  when it grows up or hatches.
- **Taming levels** (off by default). Servers can make lox, asksvin or moose need a keeper of some level to tame.
- **Experience** from taming near you, each tame (triple for your first of each kind), feeding, births, petting,
  butchering and honey. Bigger animals give more: a lox about four times a boar.
- **Multiplayer.** Animals follow the keeper's level wherever their owner's machine is, so it all works on a dedicated
  server. Like the rest of the mod, it must be installed on the server and on every client.

### Farming

Built on the game's own Farming skill, so the levels you already have count. In the game it only gives a small chance
of an extra crop, cheaper cultivating and a wider scythe. Here the planter's level decides how crops grow and ripen,
and the picker's level decides the harvest. Perks grow from nothing at level 0 to their full value at level 100.

- **Stars on crops.** Crops ripen with 0 to 3 stars, rolled from the planter's Farming level with the same odds as
  cooked dishes. Starred crops stand a little taller in the field and show their stars when you look at them. In the
  kitchen they are starred ingredients, and eaten raw they give more.
- **Heirloom seeds.** Seeds carry stars too, and a starred seed ripens with better odds: 10 levels per star. Plant
  3★ carrots for 3★ seed carrots, and each generation of good seed breeds the next. Which seeds you plant first
  follows the Ingredient Order setting (lowest stars first by default; set it to highest to breed).
- **Companion planting.** Each other kind of crop growing within 2 m adds 5 levels to a crop's roll, up to three
  kinds.
- **Giant crops.** Up to 2% of crops ripen into a giant at level 100: two and a half times the size, always 3★, six
  times the crop.
- **Faster growth.** Everything you plant grows up to 40% faster, and half again as fast while it rains.
- **Closer rows.** Crops need up to 40% less room around them.
- **Tending.** Press the use key on a growing plant once a day. It and every growing plant within 2.5 m gain a tenth
  of their growing time.
- **The almanac.** Look at a growing plant to see when it will be ripe. From level 20 you also see the odds of the
  stars it will ripen with.
- **Better harvests.** Up to half your picks give an extra crop (the game's own bonus stops at 25%). Up to 30% give
  back the seed the plant grew from.
- **Row planting, from level 25.** Placing a seed plants three in a row, five from level 50. Each pays its own seed.
  Hold Shift to plant one.
- **Auto-replant, from level 50.** Picking a crop, by hand or with the scythe, puts its plant back in the same spot
  with a seed from your inventory.
- **Hardy crops.** From level 75 your crops grow in the Ashlands without a shield. At level 100, crops that grow in
  the Meadows also grow in the Mountains and the Deep North.
- **The windmill keeps stars.** Starred barley makes starred flour, starred oats starred oat flour, in separate stacks.
- **Compost bin.** A new barrel in the cultivator's menu (10 wood, 4 stone, next to a workbench). Put scraps in it:
  food, spare crops and seeds, entrails and bone fragments.
  - Every 30 seconds it turns one into compost, and it feeds the growing crops within 12 m.
  - Fertilized crops grow 25% faster and ripen with better stars.
  - Dishes a kitchen's trash filter throws away nearby go in too.
- **Experience** as the game gives it for planting and picking, more for richer crops, five times for a giant, triple
  the first time you pick each kind of crop, and a little for tending.
- **Multiplayer.** The planter's level travels with the plant, and the machine that owns a plant rolls its stars. The
  picker's level decides the harvest perks, so it all works on a dedicated server. Like the rest of the mod, it must
  be installed on the server and on every client.

### Every skill

- **Gentler deaths, if you want them.** Choose how much of every skill's level dying costs and whether you keep
  the progress toward the next level. The defaults are the game's own.

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod on every client and on the server.

For manual install, drag GrindstoneSkills.dll into the BepInEx/plugins folder.

### Settings

All in `BepInEx/config/com.GrindstoneSkills.cfg`, reloaded while the game runs.

- **Stars:** the food and duration bonus of each star.
- **Odds:** the chance of 0, 1, 2 and 3 stars at levels 0, 25, 50, 75, 100 and 130, and how many levels a star of the
  ingredients is worth.
- **Kitchen:** the trash filter switch and every perk's value at level 100 (perks grow from nothing at level 0).
- **Experience:** an overall multiplier, the tier scaling, the first-dish bonus and fermenter experience.
- **Death:** how much of every skill's level dying costs (5% by default, as in the game; 0 keeps every level) and
  whether the progress toward the next level is lost (on by default, as in the game).
- **Display** (each player's own): stars on icons, and which stars recipes use first.
- **Sailing:** a switch for the whole skill, each perk's value at level 100, and the helm and crew experience.
- **Lookout:** the level it unlocks at, its radius, how long tags stay and the cooldown; the key is each player's own.
- **Woodcutting:** a switch for the whole skill, the experience rates, and whether you see callouts (each player's
  own).
- **Felling:** the fall push, log safety, domino strength and depth, clean fell and replanting.
- **Chopping:** stamina refund, axe wear, clean split chance and bonus, and old growth.
- **Finds:** the chance of a find at level 0 and at level 100.
- **Pickaxes:** a switch for the whole skill, which items count as plain stone (a rock that drops anything else is an
  ore deposit), the experience rates, and whether you see callouts (each player's own).
- **Seams:** the seam chance and how long a seam stays open, at level 0 and at level 100, the clean strike damage,
  and Unbroken's level, bonus per link and number of links.
- **Veins:** the chance of 1, 2 and 3 stars, the bonus per star, the level of Read the rock, and Echo's level, radius
  and cooldown.
- **Pickaxe Perks:** extra ore, pickaxe wear and splash damage at level 100.
- **Mine Finds:** the chance of a find at level 0 and at level 100.
- **Foraging:** a switch for the whole skill, the experience per pick, per biome step and for discoveries, and whether
  you see callouts and plant hints (each player's own).
- **Forage Perks:** extra yield at level 100, the best-time bonus, and sweep picking's level and reach.
- **Defense:** a switch for the whole skill, each core perk at level 100 (health, food health, damage reduction,
  regeneration and its timing, poise, parry window, block and dodge stamina), and whether you see callouts and the
  plate (each player's own).
- **Defense Experience:** the multiplier, block, parry, weapon block and hit experience, how hit size is measured,
  the cooldown, the first-block bonus and whether PvP hits train.
- **Defense Milestones:** each milestone's level (above 100 turns it off) and its values.
- **Defense Guard:** reflex, shield bash, thorns, adrenaline, shield wear and knockback at level 100, and Desperation.
- **Husbandry:** a switch for the whole skill, the keeper range, the Animal lore level, and whether you see callouts
  (each player's own).
- **Taming:** taming speed and fed time at level 100, the taming levels per creature, and Calm's level and how long a
  hurt creature stays wary.
- **Breeding:** breeding speed, herd size, growth speed, better offspring, the highest offspring level and twins at
  level 100, and how long and how much petting's contentment helps.
- **Animal Yield:** butcher yield, produce chance and interval, extra honey, and the Prime Cuts switch.
- **Companions:** pack leader damage and toughness, and the feeder's level, range and recipe.
- **Husbandry Experience:** the multiplier, taming, tamed, first-tame, feeding, birth, petting, butchering and honey
  experience.

- **Farming:** a switch for the whole skill, crop stars, how many levels a seed's star and a companion are worth,
  giant crops, and each player's own callouts, row planting and auto-replant.
- **Farming Perks:** growth speed, grow space, bonus yield and seed return at level 100; the levels of auto-replant,
  rows, heat and cold tolerance and the almanac; rain and tending (above 100 turns a level off).
- **Farming Experience:** the multiplier, tier scaling, the first-pick and giant bonuses, and tending experience.
- **Compost:** a switch, how fast bins compost, how much they hold and how far they reach, what fertilized crops gain,
  kitchen trash, and more items that compost.

`GrindstoneSkills.Finds.yml` and `GrindstoneSkills.MineFinds.yml`, next to the .cfg, list what trees and broken rock
can hide, per biome and per kind of tree or deposit: named finds with a weight and their items. They come from the
server, reload while the game runs, and explain themselves in their comments. Extra files named
`GrindstoneSkills.Finds<anything>.yml` or `GrindstoneSkills.MineFinds<anything>.yml` next to them are read as well.

`GrindstoneSkills.Forage.yml` lists what counts as foraging: each item, whether its picks roll stars, when its plant is
at its best, and how much experience a pick gives. Items from other mods can be added by their prefab name.

### Console

With `devcommands` on, `raiseskill sailing 50` and `resetskill sailing` work like they do for the game's skills, and
`raiseskill all` includes Sailing. The same works for Foraging (`raiseskill foraging 50`), Defense
(`raiseskill defense 100`) and Husbandry (`raiseskill husbandry 50`).

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.

### Works with
- **FeastMaster:** FeastMaster sets each food's base values; GrindstoneSkills' stars add on top.
- **OpenKeep:** starred food stacks and quick-stacks by star count. OpenKeep's honey settings decide how much honey a
  hive makes; Husbandry's extra honey adds on top.
- **Elite Creatures Reborn:** its breeding decides a newborn's stars and mutation; Husbandry's stronger offspring adds
  one star on top (Elite Creatures Reborn 3.10.0 or later). Prime cuts gives no stars there, since Elite
  Creatures Reborn keeps its stars apart from the game's levels.
- **ShipConfig:** ShipConfig sets each ship's base health and speed; Sailing adds on top. After ShipConfig's ship
  health setting changes in game, ships already loaded lose the Sailing health bonus until they load again.

### Good to know
- Sailing is a skill of this mod's own (the game has none). If you remove GrindstoneSkills, the game forgets your
  Sailing level the next time it saves your character. The same goes for Foraging.
- Jotun puffs, magecap, seed carrots, turnips and onions, and vineberries are the same plants you can grow, so they
  stay with Farming even where they grow wild.
- Berries and mushrooms can carry stars now, so in a recipe a 0★ one counts toward the ingredients' average stars
  where it used to be left out. The same goes for eggs, for raw meat while Prime cuts is on, and for crops and flour.
- Husbandry is a skill of this mod's own too: removing GrindstoneSkills forgets it. Without the mod, Animal Feeders you
  built no longer load, and neither does the food in them: empty them and take them down before you remove it. The
  same goes for Compost bins.
- Crops planted before you installed Farming, and wild ones, ripen with 0 stars. Flax never carries stars, so linen
  is unchanged. Vines grow as in the game.
- Ships built before you installed GrindstoneSkills keep the game's health.
- Logs felled before you installed GrindstoneSkills get no domino, log safety or old growth bonus.
- The Thunderblood Axe and Greataxe don't train Woodcutting in the game, so they get no Woodcutting perks here either.
- Rocks from other mods count for Pickaxes too: any rock only a pickaxe can break, and it is an ore deposit when it
  drops anything but stone or grausten (the Plain Stone Items setting).

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` repository checked out next to this
one. `pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build GrindstoneSkills/GrindstoneSkills.csproj -c Release
```

### Bugs and feature requests
The source lives on [GitHub](https://github.com/geraldjglasgow/ValheimMods). Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.

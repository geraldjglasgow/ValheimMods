# GrindstoneSkills

Deeper skills for Valheim: Cooking, Sailing, Woodcutting, Pickaxes and Foraging. A good cook makes better food: every dish comes
out with 0 to 3 stars, stars make food stronger and last longer, and every kitchen can throw away dishes below the
stars you want. Built on the game's own Cooking skill, so the levels you already have count. A good sailor builds
tougher ships, sails them faster, sees more of the map at sea, and from level 50 can send out a lookout pulse that
marks every enemy around the ship. A good woodcutter aims where trees fall, knocks one tree into the next, splits logs
in one blow, finds what trees hide, and gets more wood from the giants of the forest. A good miner reads stone: strikes
the seams that glint in the rock, knows a rich vein at a glance, hears where the next deposit lies, and turns up amber
and rubies. A good forager picks starred berries, mushrooms and herbs for the kitchen, knows when each plant is at its
best, and clears a whole patch in one sweep.

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

`GrindstoneSkills.Finds.yml` and `GrindstoneSkills.MineFinds.yml`, next to the .cfg, list what trees and broken rock
can hide, per biome and per kind of tree or deposit: named finds with a weight and their items. They come from the
server, reload while the game runs, and explain themselves in their comments. Extra files named
`GrindstoneSkills.Finds<anything>.yml` or `GrindstoneSkills.MineFinds<anything>.yml` next to them are read as well.

`GrindstoneSkills.Forage.yml` lists what counts as foraging: each item, whether its picks roll stars, when its plant is
at its best, and how much experience a pick gives. Items from other mods can be added by their prefab name.

### Console

With `devcommands` on, `raiseskill sailing 50` and `resetskill sailing` work like they do for the game's skills, and
`raiseskill all` includes Sailing. The same works for Foraging (`raiseskill foraging 50`).

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.

### Works with
- **FeastMaster:** FeastMaster sets each food's base values; GrindstoneSkills' stars add on top.
- **OpenKeep:** starred food stacks and quick-stacks by star count.
- **ShipConfig:** ShipConfig sets each ship's base health and speed; Sailing adds on top. After ShipConfig's ship
  health setting changes in game, ships already loaded lose the Sailing health bonus until they load again.

### Good to know
- Sailing is a skill of this mod's own (the game has none). If you remove GrindstoneSkills, the game forgets your
  Sailing level the next time it saves your character. The same goes for Foraging.
- Jotun puffs, magecap, seed carrots, turnips and onions, and vineberries are the same plants you can grow, so they
  stay with Farming even where they grow wild.
- Berries and mushrooms can carry stars now, so in a recipe a 0★ one counts toward the ingredients' average stars
  where it used to be left out.
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

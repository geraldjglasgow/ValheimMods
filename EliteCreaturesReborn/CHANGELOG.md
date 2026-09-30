# Changelog

## 3.13.2

- The world tier boxes (in the inventory under the weight, and under the minimap) show only with PackPanel installed,
  in PackPanel's column of stat boxes. Without PackPanel, Elite Creatures Reborn adds nothing to the inventory or the
  HUD, and the game's armour and weight readouts stay exactly as the game draws them. Tier rises are still announced,
  and `elite tier` still shows the tier.

## 3.13.1

- Fixed: with Jewelcrafting, CurrencyPocket, OttoPay, TrashItems or Quick Stack Store Sort Trash Restock installed,
  the inventory broke (Jewelcrafting threw an error on every item picked up and every inventory close). The shared
  stat column moved the game's armour and weight boxes into a container of its own, and those mods look for them on
  the inventory panel by name. The game's boxes now stay where the game has them and are placed in the column from
  there, and the boxes those mods add join the column instead of covering one of its boxes; TrashItems' trash can
  takes dragged stacks there. OpenKeep 1.8.0 to 1.9.0 and PackPanel 0.1.0 to 0.2.0 carry the same column: update them
  too.

## 3.13.0

- **Devouring eats only what it can swallow.** A Devouring creature eats a creature only when that creature has at
  most 125% of its own current health. It does not hunt anything bigger, and a blow that lands on a bigger creature
  is an ordinary hit; toward one it behaves as its kind would. Both healths are read at the moment of the bite, so a
  wounded devourer's reach shrinks and a hurt creature comes within it. The new `max prey health` field sets the
  percent, and `0` lifts the limit.
- **Devouring eats one creature per star.** A 2-star devourer eats two creatures in its life, and an unstarred one
  still eats one (the new `min meals` field, 1 by default). Once it has eaten them all it is sated: it eats nothing
  more and behaves like any creature of its kind, and it keeps everything it absorbed. One that already hunts players
  goes on hunting them. The count stays with the creature through a reload and a hand-over to another player. A
  devourer already in your world counts only what it eats from now on.
- **You can see what a devourer has eaten.** Its nameplate shows one icon per creature it ate: that creature's
  trophy, or the game's horned monster head for one with no trophy, such as a greyling or a hen. A creature that is
  Thieving too shows its stolen items at the right and its meals just left of them, and a Cloaked one hides them with
  its nameplate. `Show devoured creatures` and `Devoured creature icon size` in the .cfg (on, 1.6, client side) govern
  them, and `elite inspect` lists what it ate against how many it may ("devoured 1 of 2: Boar").
- **The devour flash is a fifth of its size.** The burst when a creature is devoured covered the whole scene; now
  every part of it is drawn at a fifth of the size, and the screen shake it carries is felt only close by, and
  gently. The sound is unchanged.
- **Bloated's fuse is 1.7 seconds**, down from two. A `creature_rules.yml` written by an older version keeps its own
  `delay: 2.0`; change that line to have the shorter fuse. The two new Devouring fields work from their defaults
  either way.

## 3.12.0

- **The world tier shows under the minimap.** A small box with the globe and the tier sits right under the minimap,
  so the tier is in view without opening the inventory. It hides with the minimap and while world tiers are off, and
  the new `Show world tier under minimap` setting in the .cfg (on by default, client side) hides it for one player.
- **The world tier reads as one number.** The globe plate in the inventory and the box under the minimap show the
  tier alone ("3" instead of "3/7"); hovering the plate says how many tiers there are in total.
- **The inventory's stat plates are square boxes.** Armor, weight and the world tier stand in a column of small brown
  boxes, icon above number, down the outside of the inventory panel's right edge from its top, in place of the wooden
  plates. Each still names itself when hovered, and OpenKeep's trash plate still joins the column.
- **Thieving steals only with melee hits.** A thrown stone or spear, an arrow or a blast takes nothing, however much
  it hurts: the thief has to reach you. Before, a Greydwarf could rob you with a rock from across the clearing.
- **A parried or dodged blow steals nothing.** A parry still lets a sliver of damage through, and that sliver used to
  be enough for a thief to take something; so was a hit you rolled through. Now a parry, even in a parry window
  another mod has widened, or a dodge roll through the blow keeps your items. An ordinary block that lets damage
  through still steals.
- **Thieving leaves PackPanel's slots alone.** With PackPanel installed, a thief takes only from your main grid, never
  from PackPanel's slots: worn gear, the backpack, food, mead, ammo, the coin purse, the key ring and the tacklebox.
  What is inside a worn pack can be taken, the pack itself never. Should PackPanel be running but its grid unreadable,
  the thief takes nothing rather than risk a slot. Without PackPanel, or with it switched off, nothing changes.
- **The hotbar is the eight cells the 1-8 keys use.** A thief still takes from the hotbar only when nothing else is
  left. On a grid wider than eight, the top-row cells right of the hotbar are no longer spared with it: they go first,
  with the rest of the grid.
- **A sleeping crypt mimic gives nothing away.** With Elite Creatures Pack installed, a mimic rolls stars and
  mutations like any creature, but while it sleeps as a chest it keeps the chest's size, look and name: no star
  colour, no mutation name, glow or effect. It puts them all on the moment it wakes. Its health already counts its
  stars and mutations, so the hit that wakes it does not refill it.
- **The kraken never comes as a Twin or a Phantom.** With Elite Creatures Pack installed, its kraken rolls boss stars
  and an aspect like any boss, except those two: it holds one ship with one health bar, and a Twin put two krakens on
  one ship. The default rules list it under `per boss`, and a rule file written by an older version leaves those two
  out for it as well.
- **Warding's cap covers all your hits together.** `max reflect` (7.5% by default) is now the most Warding sends back
  to you in any one second, however many hits land and however many Warding creatures they land on. Before, each hit
  was capped on its own, so a bow with extra shots, split, chain and pierce, or a staff whose cast explodes, could add
  a dozen capped reflects together and still kill you. One sword swing a second reflects exactly what it did before.
- **A rule change reaches creatures already out in the world.** Editing `creature_rules.yml`, or a server pushing
  its rules, now changes how a loaded creature's mutations act at once, so lowering `max reflect` takes effect on
  the Warding creature in front of you. Before, only creatures that loaded after the change used it. A creature's
  stars and mutations, and the health, size and speed they gave it, stay as they were until it next loads.

## 3.11.0

- **`elite purge` takes a radius.** `elite purge 30` removes only the starred or mutated creatures within 30 m of
  you. Without a number it still removes every one loaded around you, as before.

## 3.10.0

- **Works with GrindstoneSkills' Husbandry.** With GrindstoneSkills 0.7.0 or later installed, its Husbandry skill's
  "Better offspring" chance gives a newborn (or the chick in an egg) one star more than it rolled here, up to one above
  its stronger parent. Without GrindstoneSkills nothing changes, and neither mod needs the other.

## 3.9.1

- **Starred creatures look like the game's starred creatures again.** A one-star creature wears the game's one-star
  colour and parts, and two stars or more the game's two-star look, so a two-star Neck is purple again instead of the
  plain green. The corpse keeps the look too. Size still comes from the `growth` line, so nothing grows twice.

## 3.9.0

**Three new mutations.**

- **Gilded**, the loot goblin. It glitters gold from well beyond nameplate range, never attacks a player, and runs
  from any player it can see within 30 m, so sneak up on it; a hit from any range sends it running too. Catch it and it
  drops three times its loot plus 20 coins for each star plus one, in every loot mode. It is the rarest mutation by
  design, and the only one with no downside. A tamed Gilded creature stays calm around its owner and drops ordinary loot.
- **Blinking**. Every 30 seconds of a fight it vanishes and reappears 4 m behind its target, facing it. Half a second
  before, the spot flashes and chimes, so you can turn in time. It never lands inside walls, off a ledge, in water it
  avoids or anywhere it could not have walked to. It has 25% less health.
- **Relentless**. Once it picks you, it keeps coming while you are within 150 m: it heads for where you are even out of
  sight, sneaking does not hide you from it, it swims after you, and it neither despawns at dawn nor wanders off after
  a raid while it hunts. It is never faster than its base speed, so outrun it past 150 m or it follows you home.

**Five new boss aspects.**

- **Adaptive** (loot x1.3). It takes 50% less of whichever damage type hurt it most in the last 15 seconds, and glows
  that type's colour so you can see what it is shrugging off. Swap weapons, or spread the group across damage types.
- **Fixated** (x1.3). It marks one player with a red eye over their head and a line in chat ("Bonemass fixes on
  Gerald!"), hits them 50% harder and everyone else 30% softer, and every 30 seconds moves the mark to whoever hurt it
  most in that time. Alone, you are always marked.
- **Stormbound** (x1.2). Every 20 seconds a glowing blue circle appears under each player near it, and two seconds later
  lightning strikes it: anyone still inside takes 8% of their maximum health as lightning damage and staggers. Step out
  or roll through.
- **Gravitic** (x1.3). Every 20 seconds it roars and drags every player within 30 m toward it for a second and a half,
  then slams: anyone within 6 m loses 10% of their maximum health and staggers unless they roll through it.
- **Colossal** (x1.2). 40% bigger, 15% more health, 15% slower, and its heavy attacks - stomps, slams, novas, spins,
  crushing blows - send a shockwave across the ground that knocks down every player within 8 m, at most once every 5
  seconds. Roll through it or jump it. Its corpse keeps its size.

About one boss fight in five is still plain: `none` now weighs 30 against 10 for each aspect. A rule file written by
an older version keeps its own `none: 20` and gives the new aspects 10 each.

**Mutation rules per creature.** Entries under `creatures:` in the rule file now accept `mutation chance`,
`mutation chances` and `mutation power`, laid over the rules of whatever biome the creature is in. `match: Deathsquito`
with `mutation chances: { Cloaked: [0] }` ends Cloaked mosquitoes without changing the rest of the Plains. Two entries
for the same creature now merge instead of the later one replacing the earlier.

**Cloaked** creatures show themselves at 10 m by default (was 6), and trolls and lox at 15 m, through the new default
`creatures:` entries. Drakes are never Cloaked.

**Warding reflects the base hit, capped.** The reflect now comes from the damage the creature actually took, without
sneak-attack or stagger bonuses and never more than the health it had left. A new `max reflect` field (default 7.5)
caps any single reflect at that percent of the attacker's maximum health, so a Warding creature can no longer one-shot
anyone. The Warding and Reflective flashes are 80% smaller.

**Bloated.** The fuse is two seconds by default, and the warning smoke stays at full strength until the blast, riding
the body wherever it rolls. The explosion is drawn 30% smaller (the damage radius is unchanged), and the corpse goes with
it: it vanishes in the blast and drops its loot there instead of lying on and puffing away afterwards.

**Thieving** creatures carry one stolen item per star, so a 3-star thief can rob you three times. A thief with no stars
still takes one; `max items` sets the minimum, and 8 is still the most. Every stolen item shows on the nameplate while
there is room beside the stars.

**Tamed Splintering** creatures split into tamed copies that keep the parent's name and follow the same player.

**Boss damage board.** It now sits at the far left of the screen, halfway down, out of the way. Type `/damage` in chat
(or `damage` in the F5 console) to see the latest boss's board again; any player can, and one who joined after the kill
gets it from the server. On a Phantom boss's board, damage dealt to its copies counts toward each player's total.

**Fixed**

- Small smoke clouds were left behind after a Bloated blast. Every effect the mod drew also made the game build a real,
  networked copy of it on every player's screen: a second explosion and bang for each blast, and warning smoke that
  never went away. Effects are now drawn once, locally, for every mutation and aspect, so some may look lighter than
  before.
- A Thieving creature despawning at dawn with a player nearby dropped its stolen goods again on every step it took
  while walking away, duplicating them. It now drops them once, when it actually goes.

**Upgrading**

- **Update the server as well as every player**: the Bloated blast, the boss board, the new mutations and the new
  aspects send new messages.
- A `creature_rules.yml` written by an older version keeps its own numbers: Bloated `delay: 1.0`, Cloaked
  `reveal distance: 6`, and no troll, lox or drake entries. Change those lines or delete the file to have the new defaults
  written. The new mutations work from their built-in defaults either way, and Gilded stays rare.
- Gilded has its own chance curve, like Devouring, so `mutation chance: [0]` in a biome does not silence it there. Use
  `mutations enabled: Gilded: false` to turn it off, or `mutation chances: Gilded: [0]` in that biome.

## 3.8.0

**Phantom reworked.** A Phantom boss no longer arrives with four copies. It splits off copies as it is hurt: at 66%
of its health and again at 33%, one copy for each player online, so a player alone faces one at a time.

- Each copy has 25 health for each world tier (tier 0 counts as 1) and still deals half the boss's damage.
- The boss keeps its big health bar. The copies' bars are small, in a row under it, instead of covering it.
- New Phantom keys in the rule file: `split at: [66, 33]` (the health marks; add or remove marks as you like),
  `per player: 1` and `health per tier: 25`. An older rule file's `copies` and `health` lines are no longer used:
  the log warns about them and the new defaults apply. Replace that Phantom line to tune the new ones.

**Bloated explosions can be seen and heard.** The blast now goes off with the game's dynamite explosion sound (new
`blast sound` field). The default blast and warning effects named prefabs the game does not have, and the search for
a stand-in looked in a list the game leaves empty, so the blast and its warning drew nothing. Both are fixed; a rule
file that still names the old effects gets a stand-in explosion and a note in the log. The same fix makes the
Warding, Devouring, Thieving, Summoner and Phantom effects show at all.

**World tier in the inventory.** A plate with a globe under the weight shows the tier ("3/7"). The armor and weight
plates move up to make room, and hovering any plate on that side names it. `Show world tier` in the .cfg hides the
plate for one player. With OpenKeep 1.5.0 its trash plate joins the same column, evenly spaced.

- **Update the server as well as every player**: the Bloated blast message changed, and a player on an older
  version sees no blast from a player on 3.8.0.

## 3.7.1

**Creatures spawn at full health alongside mods that raise creature health.** With a mod that multiplies creature
health (Path of Valheim, for one), mobs and bosses could spawn with only 10-15% of their health bar. A new creature
now counts as full health the way the game does it, so it stays full whatever another mod does to its maximum.
Creatures that already spawned part-hurt this way are restored to full the next time they load.

## 3.7.0

**Boss damage board.** When a boss dies, everyone on the server sees who fought it: the boss's name and every player
who hurt it, with the health each one took off it, most first, at the top of the screen for a minute.

- It counts what the boss actually lost - after its resistances, without the overkill - and only players count.
- Twin bosses are one fight and one board, with both twins' damage added together.
- Two new per-player settings in the .cfg: `Boss damage board` turns it off, `Boss damage board seconds` sets how
  long it stays.
- **Update the server as well as every player**: the tally is kept by whoever runs the boss, which can be any of them.

## 3.6.1

**Admin commands work for admins on a dedicated server.** A player on the server's `adminlist.txt` could be told that
`elite spawn`, `inspect`, `purge`, `effects` and `reference` need admin rights. The mod now asks the server, which
checks the player against its own admin list the same way the game checks `kick` and `ban`.

- **Update the server as well as every player**: the server answers the check. A player on 3.6.1 whose server still
  runs an older version is told the server did not answer.
- A refusal names the exact ID the server knows the player by, ready to add to `adminlist.txt`. The server reads the
  file again within 10 seconds, so no restart or reconnect is needed, and it logs every check.
- A mistyped `elite` subcommand now lists the subcommands for every player, admin or not.

## 3.6.0

**World tiers.** The world now hardens as bosses fall. It starts at tier 0 and goes up one tier the first time each
boss is defeated - by anyone, in any order - so the seven bosses take it to tier 7. Killing a boss again changes
nothing. Every rise is announced to everyone on the server ("The world hardens: tier 3 of 7").

- Each tier makes **stars and mutations more common in every biome**. A star boost leans each biome's star chances
  upward (the top end most), and a mutation boost multiplies every mutation chance. At the defaults, by tier 7 the
  unstarred share of the Meadows falls from 73 in 100 to 44, five-star Plains creatures rise from 5 in 100 to 22,
  and mutation chances double.
- **Bosses are unaffected**, and **creatures already alive keep what they rolled** - the tier decides what the next
  one rolls. A respawned camp comes back at the current tier.
- The tier is read from the game's own record of defeated bosses, so **a world that killed bosses before this
  version starts at that tier**, and an admin can try a tier out with the game's `setkey` and `removekey`.
- **`elite tier`** shows the tier, what it does to the rolls and which bosses count, and it is open to every player,
  not just admins. `elite inspect` now says which tier a creature was rolled at.
- A new `world tiers:` block in `creature_rules.yml` has the off switch, the list of bosses that count (add a modded
  boss's defeat key to count it; `elite tier` lists every boss's key) and the star and mutation boost for each tier.

**Breeding.** Tamed creatures now pass their traits on, where their young used to roll like wild creatures.

- A newborn - a pup, a piglet, a calf, or an egg and the chick that hatches from it - takes **one of its parents'
  mutations** whenever either has one, and **a star count from 0 up to its stronger parent's**, every count equally
  likely. Two plain parents have plain young, and nothing new is ever rolled, so a strong line stays strong only if
  you keep the good ones.
- **Young creatures keep their traits when they grow up.** They used to roll again as adults.
- **Eggs keep their traits** when carried or stored, and say what they will hatch - "Hatches with 2 stars,
  Leeching" - on their hover text and tooltip. An egg's stars are its quality, so eggs of different stars no longer
  stack together.
- A bred mutation behaves exactly as it does in the wild - a Miasmic pet still trails poison.
- A new `breeding:` block in `creature_rules.yml` has the off switch (newborns then roll like wild creatures, as
  before) and the chance a newborn takes a parent's mutation, 100 by default.

A rule file from an earlier version has neither block and takes the defaults, with both features on. To tune them,
move your `creature_rules.yml` aside, start the game once for a fresh documented copy, and carry the two blocks
across.

## 3.5.0

**Boss aspects.** A boss never takes a mutation; it now takes an **aspect** - one modifier that changes what kind of
fight it is, named before the boss ("Twin Bonemass"). Eight of them, plus a plain fight one time in five:

- **Reflective** - 15% of each hit you land comes back to you as true damage armour does not reduce. Burn and
  poison ticks are never returned.
- **Shielded** - 30% less damage from bows and crossbows.
- **Mending** - regenerates 0.3% of its health every second, in combat too.
- **Summoner** - each time it loses 33% of its health it calls two 2-star creatures of its own biome.
- **Elementalist** - 20% more fire, frost, lightning, poison and spirit damage.
- **Enraged** - 20% more physical damage.
- **Twin** - a second copy of the boss arrives with it; the two share one health pool, each has 25% less health and
  damage, they die together and both drop full loot.
- **Phantom** - four copies arrive with it, with 100 health and half its damage. They drop nothing, leave no body,
  never count as the boss's defeat, and vanish when the boss dies.

**Read it at the altar.** Hover the offering bowl to see the current aspect, what it does, what it pays and when it
shifts. Every altar shifts to a different aspect each in-game hour (75 real seconds), independently of the others,
and every player at a bowl reads the same thing, across restarts. The aspect on the bowl when you make the offering
is the one you fight. The Queen, and any boss spawned by console or another mod, rolls its aspect when it appears.

**Harder aspects pay more**: each has a loot multiplier (x1.1 Shielded to x1.5 Summoner), applied on top of the
boss's star drops and the boss multiplier - in Vanilla loot mode too, where it is the only thing that touches a
boss's drops.

**Boss stars now show** under the boss health bar, in the same row of small and large stars as a creature's
nameplate. They were rolled and scaled before but never drawn.

All of it lives in a new `aspects:` block under `bosses:` in `creature_rules.yml`: an off switch, the shift
interval (0 fixes each altar), each outcome's chance, each aspect's loot multiplier and numbers, and per boss what
Summoner calls and which aspects it may roll. A rule file from an earlier version has no such block and takes the
defaults. `stars: false` now turns off only boss stars; a boss is left exactly as the game ships it when aspects
are off too.

`elite spawn` makes bosses now, with one aspect word in place of mutations (`elite spawn Bonemass 2 Twin`), and
`elite inspect` reports a boss's aspect, its loot multiplier and its twin or phantom links.

## 3.4.0

**Loot has grown from one multiplier into a system.** A `loot:` block in `creature_rules.yml` picks one of four
server-wide modes:

- **Vanilla** - drops untouched; the loot feature's off switch, the rest of the mod still works.
- **Scaled** - the creature's own table, quantities raised by the star `drops` line: predictable abundance.
- **Rolled** (the default) - the creature's own table rolled once more per star, each roll independent, so a
  hard fight has a real *chance* at the rare drop instead of a guaranteed stack of the common one.
- **Curated** - the rule files decide entirely, for servers building their own economy.

The knobs around them:

- `extra roll chance` per star and `max extra rolls` (default 5, 0 = uncapped) tune Rolled.
- `global multiplier` over every kill and `boss multiplier` on top for bosses, applied after the mode.
- **Trophies are not multiplied** unless `multiply trophies` says so - twelve identical trophies from one kill
  is clutter, not a reward. A per-creature override exists for the server that disagrees about one creature.
- **Per-creature rules** in a `creatures:` section, matched by prefab name so modded creatures work exactly like
  vanilla ones: a `drops` line, `drop overrides` that change or remove rows of the creature's own table, and
  `extra drops` additions - which is also the whole of Curated's format, not a second one.
- Mutations and attunements still change the fight, not the reward; stars are what pays.

**`elite reference` writes `creature_reference.yml`**: every creature the running game knows, grouped by biome,
with prefab name, display name, base health and its vanilla drop table. Paste it and `creature_rules.yml` at an
assistant, describe the economy you want, and get back rules that use real names against real tables.

**Plays fair with other loot mods.** The engine only reworks rows it owns - the creature's own table and rows
your rule files name. Drops another mod injects into the same kill (EpicLoot's enchanting materials, say) pass
through untouched in every mode, whichever mod's patch happens to run first.

## 3.3.0

**Devouring reined in.** Out of the box it ate a camp in under two minutes and came out one-tapping players
through a parry; the defaults now leave it a situation you can respond to. A YAML file from an earlier version
pins the old numbers in its `mutation power:` block - edit its Devouring line (or delete it to take the new
defaults).

- `devour cooldown` default 10 → 60: one bite a minute, not six.
- `absorb health` default 100 → 50, `absorb damage` default 100 → 25: it keeps a share of a meal, not the whole
  animal; damage is cut hardest because damage is what kills.
- New `move` field: the devourer's base speed multiplier before its well-fed slow, default 1. Set `move: 0.5`
  to ship them at half speed from the first bite. Never enhancement-scaled.

**Joining a server no longer refuses a correct client under load.** The version check raced the config pushes over
the same connection and was judged by one 5-second deadline, so a valid reply that arrived slightly late was
reported as "you have no copy" and the join was refused. The check is now sent before the config pushes, and
retries every 5 seconds up to 20 before concluding a player is genuinely unmodded.

## 3.2.0

**Thieving** — a tenth mutation, and the only one whose threat is not damage. A Thieving creature takes one item
from your inventory when it lands a hit, carries it where you can see it, and drops every bit of it when it dies.

- **What it takes** — one whole stack from an unequipped slot, backpack first and the hotbar only as a last
  resort. **It never takes equipped gear** — not your weapon, shield, armour or tools — and a hit you fully
  block, parry or resist takes nothing. The item is preserved exactly: stack size, quality, durability, crafter's
  name and custom data all ride with it and come back unchanged.
- **You are told immediately.** A status message names what went — `Thieving Greydwarf stole Silver x14` — with
  a sound and an effect, and the creature's nameplate shows the item's own icon for as long as it holds it.
- **Nothing is ever destroyed.** Goods drop at the corpse when it dies, and also when it despawns or is cleared
  with `elite purge` — which otherwise drops nothing at all. Stolen items are **never** multiplied by `drops`,
  star count, loot mode, a boss aspect or `large star power`; they are your own items being handed back.
- **They drop for whoever lands the kill**, and the mod does not chase items back to their original owner.
- **Configured** by one field, `mutation power: Thieving: {{ max items: 1 }}`. One item is the default and what the
  mutation is designed around; it is hard-capped at 8 whatever you set. Suggested biome bumps for Black Forest,
  Plains and Mistlands are in the written rule file; Meadows deliberately gets none.
- Spawn one to look at with `elite spawn Greydwarf 0 Thieving`.

Bosses never take mutations, this one included. Existing rule files keep working untouched — an unlisted mutation
stays enabled and `max items` falls back to its default.

## 3.1.0

**Mutations enabled** — a new `mutations enabled` switch in the rule file turns any of the nine mutations off
everywhere, regardless of its chance curves. Old rule files keep working unchanged; an unlisted mutation stays
enabled.

**Rule file** — the generated `creature_rules.yml` comments are much shorter. The full field-by-field reference
for `mutation power` moved to the README's new "Mutation power fields" section instead of living inline.

**Leeching and Plated rebalanced** — both could make a creature nearly unkillable:

- Leeching's regen dropped from 2% to 0.5% max health per second, and lifesteal from 30% to 10%. Regen now pauses
  for `combat cooldown` (5s default) after the creature last took damage, is hard-capped at `regen cap` (20 hp/s
  default) so a high-health creature cannot out-heal a fight on regen alone, and is no longer large-star enhanced.
- Plated's `armour` field is now a damage-reduction *percentage* (40% default, was a flat 100 armour points fed
  into vanilla's armour curve, whose quadratic low end erased almost all of a weak hit's damage). A new
  `max reduction` field (55% default) hard-caps it so large-star enhancement cannot approach invulnerability.
  **This changes what an existing `armour` value in a customised rule file means** — a server that set its own
  number should revisit it.

**Attack animation speed fixed** — a starred or Mad creature's animator speed was multiplied every fixed step
during attacks, minor actions, emotes and stagger, because the game only resets it while idle or moving. It
compounded once per step and ran away. The scaled speed is now written absolutely and only inside the game's own
reset window, so an attack inherits it once for the whole swing. (Shipped in 3.1.0; missing from its notes.)

**Large stars toned down** — `large star power` defaults to `1` instead of `2`: a large star (already worth five
ordinary ones) no longer also doubles every mutation's bonus on top of that. `star power`'s `hp` line is flattened
at the top (`3.85`/`5.4` at 4/5 stars down to `3.3`/`4.0`) so a five-star creature's health climbs less steeply.
Ash Lands and Deep North's `star chances` trim the 4-5 star tail (`15, 9` down to `11, 4`) and pad the bulk of the
distribution instead, so their harshest creatures are rarer. `max mutations: 1` already capped how many of these
a single creature can stack, and stays unchanged.

## 3.0.0

Rebuilt from scratch. Not an update to earlier versions — none of the old code remains. This release covers
mutations only; the rest of the mod's features return over the next releases.

**Mutations** — nine, one per creature by default:

- Mad — far faster, half health
- Bloated — double health, explodes a second after it dies
- Cloaked — invisible beyond 6 metres
- Splintering — splits into two weaker copies when killed, which can split again
- Leeching — regenerates, and heals from damage it deals
- Warding — reflects damage and knocks you back
- Plated — armoured while healthy, hits harder as that armour goes
- Miasmic — trails poison clouds; poisons players, never creatures
- Devouring — kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you

**Stars** — beyond vanilla's two. Counted in fives on the nameplate: a small star is one, a large star is five.
A mutation on a large star is stronger.

**Configuration** — a settings file and a YAML rule file, both written and documented on first run, both
hot-reloaded. Star chances, mutation chances and mutation strength are set per biome. Servers can bind connected
players to their rules; display preferences stay per player.

**Multiplayer** — creature state travels in the world data the game already shares, so every player sees the same
creature. Untested across two real machines.

**Console** — `elite spawn`, `elite inspect`, `elite purge`, `elite effects`.

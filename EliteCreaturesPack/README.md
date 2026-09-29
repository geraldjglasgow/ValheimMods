# Elite Creatures Pack

New creatures for Valheim, each with its own way to fight. A crypt chest that bites whoever opens it. A greydwarf
with a slingshot that shoots stones. A giant crusted in ice, asleep on the mountainside until night falls. A kraken
that hunts ships on the open sea, and the shield you make from its beak. A skeleton with a crossbow, who has to
reload after every shot, and a crossbow of bones like its own for you to make. Seven skeletons armed with weapons
of bone and vertebrae, and the same weapons for you to make from the vertebrae they drop. And in some burial chambers, a
skeleton executioner with a greataxe of bone, whose axehead you can take to make the greataxe yourself. Out in the
wild, five new creatures haunt the Swamps and five roam the Mountains.

It works on its own. With **Elite Creatures Reborn** installed as well, every creature here rolls stars and mutations
like any other (the kraken, a boss, rolls stars and a boss aspect), and the mimic keeps its disguise until it wakes.

**The mod is young.** The numbers will be tuned in play, and settings may still change before 1.0.0; reports of
anything that feels off are welcome (see "Bugs and feature requests" below).

## Crypt Mimic

Some crypt chests aren't chests. In burial chambers and sunken crypts, a chest can be a **crypt mimic**: it looks,
reads and sits exactly like the crypt chest ("[E] Open") until you open it. Then the lid bursts open and bites you,
and there is no dodging that one. Awake, it hops after you and lunges: the lid gapes wide, it leaps about two and a
half metres and snaps shut. **Dodge the lunge** (blocking doesn't stop it), then hit it while it recovers with its
tongue out; it needs three seconds before it can bite again. Its bite does 20 slash. It has the Black Forest crypt
skeleton's health and resistances (weak to blunt and fire, resists pierce and frost, immune to poison), is undead so
the crypt's skeletons and draugr leave it alone, and drops the loot of the chest it was pretending to be.

About 15% of crypt chests are mimics, only in crypts generated after the mod is installed. The chest and every texture
on it are the game's own; the mimic's teeth, tongue and animations are embedded in the plugin.

Epic Loot adds magic items to a creature only when its loot table names that creature; add an entry for
`ECP_CryptMimic` to give the mimic some.

## Greydwarf Slinger

Some greydwarfs have learned to fight from a distance. In the Black Forest about one greydwarf in ten is a **Greydwarf
Slinger**: a greydwarf with a forked-branch slingshot in its fist and a satchel of stones on its hip. It fights like a
skeleton archer: it stands its ground, raises the slingshot, draws the pouch back to its cheek and lets fly, a
fist-sized stone (12 blunt), a little faster than a greydwarf throws, arcing onto you from up to 22 metres every few
seconds. It aims where you stand when it lets go, so a dodge, a raised shield or running across its line beats it.
It never claws: walk right up to it and it keeps shooting at point blank. It has the Greydwarf's health, resistances
and loot plus a few stones.

Slingers come from the wild and from greydwarf nests; camps keep their greydwarfs. The greydwarf and its materials are
the game's own; the slingshot, satchel and the shot animation are embedded in the plugin.

## Rime Giant

Some mountains have a sleeper. A **Rime Giant** is a troll half again as big, grey-white with frost and crusted in
eight plates of ice; about three mountains in five hold one, and only ever one. By day it sleeps where it stands under
a crust of snow, with no name or health bar: it looks like one more snowy outcrop, and walking past (or into it) won't
wake it or budge it. At night, in a snowstorm, or the moment you hit it, it wakes.

With every plate on, weapons barely hurt it (15% of the physical damage gets through), and each plate it loses lets
more through. **Fire breaks the plates**: each fire arrow knocks off about one, a torch's hit most of one, and it loses
one every few seconds near a campfire. At 40% health whatever is left shatters at once. Broken plates fall off and
tumble away and the last one staggers it. They never grow back mid-fight, only once it has given up on you. It is
immune to frost and weak to fire.

Its fist throws you back. Its ground slam sends an **avalanche** rolling towards you: far down the slope, barely a few
metres up it, knocking whoever it catches downhill. Don't stand below it; flank along the ridge or get above it. From
range it hurls ice boulders that burst into frost and slow you. It has 1500 health, and drops crystal, freeze glands
and silver ore.

A mountain here is one of the game's own connected Mountain regions at least about 150 m across; once its giant has
come it never gets another, even after it dies. The troll and its materials are the game's own; the plates, the crust
and the boulder are embedded in the plugin.

## Kraken

Far out on the deep ocean, something follows ships. A **Kraken** is a boss of the sea: a huge dark head with a chitin
beak and six long tentacles. When it finds a ship with a crew it roars and gives chase, its mantle breaking the water
behind its eyes. It is rare, and only ever over deep water: a crew sailing the open sea meets one about every hour or
two. **You can outrun it**: under sail with a fair wind any ship is faster, and once you're out of its reach for a few
seconds, or safe in water too shallow for it, it dives and is gone. Rowing won't save you, and a raft never will.

If it catches you, it dives under the ship and **holds it**: the sail comes down and the ship can't move until the
kraken dies or lets go (it lets go once nobody has been aboard for twelve seconds). Then it fights in two phases, round
and round: six attacks from under the ship, then six from the head. It attacks one thing at a time, every few seconds
(a little less often for a crew of two or a lone player). While it holds your ship, your own blows can't damage the
ship, so swing away over the rail:

- **Tentacles.** Six tentacles rise out of the sea round the hull. One at a time, a tentacle rears up, curls over, and
  whips down across the deck along a line through whoever it picked, lying across the rail and deck before it lifts
  again. They are thin towards the ends: **step out of the line or roll through it**. A tentacle on the deck is the
  moment to hit it. One of the six is **the grab**: listen for the deep bellow and watch for the tentacle that rears
  higher and trembles. Roll clear, because if it lands on you it hoists you up and tosses you into the sea a few
  metres off the hull. After six slams they sink.
- **The head.** The head rises right against the hull, facing the crew and close enough to hit from the rail, with
  two tentacles gripping the rail either side of it. It keeps off the ladder: on a ship with one ladder it comes up
  on the other side, so you can always climb back aboard. It **bites** anyone near the rail in front of it and **spits
  ink** from its mouth in a straight line at anyone further off, after a moment of warning as it rears back with its
  beak gaping. Between squirts, nowhere a few metres in from the rail is safe: the whole head throws itself over the
  rail and onto the deck to bite. Ink in the eyes blacks out your screen for a second and a half before it fades;
  dodge it, step aside, hide behind the mast, or block or parry it facing the kraken and it never reaches you. About
  every ten seconds a tentacle from the far side smashes across the ship instead, damaging it. After six attacks the
  head goes back under and the tentacles come up again.

It has 3000 health (Bonemass has 5000), and 30% more for every other player nearby, as for every creature, and a boss
health bar. Its smashes are measured against the ships: a karve (500 health) survives a fight only if you kill it
quickly, a longship (1000) holds out, a drakkar (3000) shrugs them off. Every part of it can be hit, head or tentacle.
It drops its loot onto the deck: chitin, coins, **its beak** and 5 to 7 cuts of **raw kraken tentacle**.

- **Cooked kraken tentacle.** Cook the raw tentacle on an iron cooking station, like serpent meat. It beats cooked
  serpent meat on every count: 80 health, 28 stamina, 4 healing a tick, for 30 minutes (serpent: 70, 23, 3, 25
  minutes).
- **Kraken shield.** At a level 3 forge, 10 fine wood, 5 silver and the kraken's beak make a finewood-and-silver
  shield with the beak set in its face. It beats the serpent scale shield on every count (block 70 against 60,
  deflection 110 against 100, -5% movement against -10%, durability 300 against 250, lighter, pierce resisted while
  blocking) and, unlike it, it parries. **Parry a creature's blow with it and the beak bites back**: 60 pierce damage
  to the creature, 10 more for each upgrade. Upgrades take more fine wood and silver, never another beak.

Its body is the game's sea serpent, hidden, wearing the kraken; the kraken's model, its ink and its loot's models are
embedded in the plugin, and everything it does is played by the mod, not by animations, so the tentacles strike
wherever the crew stands and lie on whatever deck your ship has: a raft, a karve, a longship, the Ashlands drakkar, or
a modded ship.

## Skeleton Crossbowman

Some of the dead kept their crossbows. The **Skeleton Crossbowman** is a new kind of skeleton unit: wherever the game
spawns one of its archer skeletons, about one spawn in seven is a crossbowman instead - in burial chambers, graves,
stone tower ruins, mountain cabins, the Meadows' ruins, from bone piles, and out in the night once Bonemass is dead.
Each is its own skeleton's kind: the Black Forest's, the Meadows', the Swamps' and the Mountains' crossbowmen have
that skeleton's health (40, 30, 60, 75), resistances, looks and loot, and hit as hard as that skeleton's archer (20,
15, 55 and 60), with blunt bolts.

It carries a crossbow made of bones in its fists (a femur for the stock, a gently bowed row of vertebrae threaded on a
bone along it, two ribs for the prod, a jawbone for the stirrup) and a quiver of grimy bone bolts on its hip. It fights
like the skeleton archer, standing its ground and shooting from up to 25 metres down to point blank, but every shot is
a crossbow's: it brings the stock up to its shoulder, lays its skull on it, holds the aim a moment and looses a bolt
with a blunt knuckle of bone for a head, flying straight and fast. Then it has to reload: it lowers the crossbow, bends
over it, hooks the string and draws it back into the nut, pulls a bolt from its quiver and lays it in the groove,
taking its time over it (about three and a half seconds). That's your moment to close in. It carries the crossbow at
the low ready in both hands wherever it walks or runs, and drops a few bone bolts now and then. The skeletons and their
materials are the game's own; the crossbow, its bolts, the quiver and the animations are embedded in the plugin.

## Bone Crossbow

The crossbowmen's weapon, for players: a **Bone Crossbow** made at the workbench. It handles like the game's own
crossbow (the same aim, reload and Crossbows skill, bolts as ammo, on your back when put away) and hits like a club:
30 blunt of its own (+4 an upgrade, three levels), and whatever bolt it looses adds its damage on top. A bone bolt,
which the crossbowmen drop, adds 32 pierce. It reloads in 4 seconds with no skill (the skill halves it), and it does
not chop trees. Its cost is a placeholder for now (see `Recipe` below).

## Skeleton Arsenal

The dead make their own weapons now, out of what they have: bone. Seven new skeleton units, each carrying one weapon
of bone with vertebrae worked into it:

| Skeleton | Weapon | Its blow |
| --- | --- | --- |
| Skeleton Cutthroat | bone dagger | a quick slash, slash and pierce, more often than the others |
| Skeleton Swordsman | bone sword | the skeleton's own sword swing |
| Skeleton Axeman | bone axe | a wide axe swing, a little harder |
| Skeleton Bonebreaker | bone mace | a heavy blunt blow |
| Skeleton Spearman | bone spear | a fast stab from further out |
| Skeleton Halberdier | bone atgeir | a two-handed blow from furthest out, the hardest and slowest |
| Skeleton Bowman | bone bow | shoots like the skeleton archer, bone arrows |

They are skeleton units like the game's own: wherever the game spawns a skeleton, about one spawn in four is an
arsenal skeleton instead - in burial chambers, graves, stone tower ruins, mountain cabins, the Meadows' ruins, from
bone piles, and out in the night once Bonemass is dead. Each has its own skeleton's health (40 in the Black Forest,
30 in the Meadows, 60 in the Swamps, 75 in the Mountains), resistances, looks and loot, and hits about as hard as that
skeleton's sword or bow. They strike one blow at a time, never a combo. The one-handed fighters keep the skeleton's
shield. Every one drops what a skeleton drops, and one in ten drops a **Vertebra** as well.

## Bone Weapons

The arsenal's weapons, for players, made at a **level 3 workbench** from bone fragments and a vertebra (the dagger
needs only bone fragments). Each handles like the bronze-age weapon of its kind (the same attacks, combos, skill,
durability and upgrades) and hits a little softer: 0.85 of its damage.

| Weapon | Handles like | Damage | Made from |
| --- | --- | --- | --- |
| Bone Dagger | copper knife | 10.2 slash, 10.2 pierce | 8 bone fragments |
| Bone Sword | bronze sword | 29.75 slash | a vertebra, 10 bone fragments |
| Bone Axe | bronze axe | 34 slash, 34 chop | a vertebra, 10 bone fragments |
| Bone Mace | bronze mace | 29.75 blunt | a vertebra, 12 bone fragments |
| Bone Spear | bronze spear | 29.75 pierce | a vertebra, 10 bone fragments |
| Bone Atgeir | bronze atgeir | 38.25 pierce | two vertebrae, 16 bone fragments |
| Bone Bow | fine bow | 27.2 pierce | a vertebra, 12 bone fragments |

Upgrades cost bone fragments only. **Bone Arrows**, 20 for 8 bone fragments at a level 2 workbench, hit for 24 pierce:
better than wood (22), not as good as flint (27).

## Crypt Executioner

A mini boss of the Black Forest: a skeleton headsman, a head taller than the others, with a greataxe whose haft is
strung from vertebrae. About one burial chamber in four has one waiting in its largest room. It has 900 health and
six moves:

- **Slam**: the axe raised high and brought down on you, stones flying where it lands.
- **Ground scrape**: the edge dragged through the ground in an arc; the shockwave that runs out from it is what hits.
- **Spin**: a full turn with the axe held out flat, hitting all round.
- **Overhead throw** and **spin throw**, when you keep your distance: the axe shatters where it hits, and its pieces
  gather into a skeleton that rises there, bone by bone from the feet up, while a new axe forms in the Executioner's
  hands. No more than two of its skeletons stand at a time.
- **Rear strike**, when two or more of you are near and one is behind it: its skull turns all the way round, and the
  axe comes down behind it.

It drops coins and bones, and its **axehead** half the time. Once killed, it returns to its chamber after three
days. Burial chambers from before you installed the mod can hold one too.

## Executioner's Greataxe

The Executioner's weapon, for players: made at a **level 3 workbench** from its axehead, 8 vertebrae and 10 bone
fragments. It is a two-handed axe that handles like the Battleaxe (the same stance, block and Axes skill) and hits
like a fully upgraded Bronze Axe: 55 slash, 49 chop. It cannot be upgraded. Its combo is its own: a slash in front, a
whirling spin that hits all round, then a heavy overhead blow that takes a moment to recover from.

## Swamp creatures

Five new creatures of the Swamps. They fight on the draugr's side, come one at a time in the swamp's interior (away
from its edges) beside its own creatures, and never replace them:

| Creature | Built on | | Health |
| --- | --- | --- | --- |
| **Mire Jarl** | Draugr Elite | a rare mini boss, a third bigger, in a corroded crown and burial armour: slower, heavier blows that knock you further, and hard to stagger. Only one in a wide area | 900 |
| **Reed Stalker** | Draugr | a draugr in a reed mantle with a spear, quicker than the rest: it circles you and stabs from 2.7 metres (pierce) | 160 |
| **Bog Maw** | Blob | a bigger blob crusted with roots and tusks, asleep in the mire until you come within 7 metres or make a noise | 200 |
| **Fen Crawler** | Neck | a neck half again as big, grown over with swamp: quick bites, resists pierce, weak to blunt, immune to poison | 100 |
| **Drowned Shade** | Wraith | a drowned revenant with a bell and a chain harness, flying at you only at night; its blows knock you back hard | 220 |

They drop what their swamp would give: the Jarl entrails and chains, the Stalker entrails and wood, the Maw ooze and
bone fragments, the Crawler neck tails and bone fragments, the Shade chains and coal. The Mire Jarl never comes
starred from the wild; the others can come with a star. It is a mini boss, not a boss: no altar, no boss bar, no
Forsaken power.

## Mountain creatures

Five new creatures of the Mountains. They come one at a time, away from the biome's edges; all are immune to frost and
weak to fire, and none can be tamed:

| Creature | Built on | | Health |
| --- | --- | --- | --- |
| **Frostfang** | Wolf | a rare wolf mini boss, two thirds bigger, with a mane; its bite carries frost and reaches further. At half health it rages for good: its coat pales, it runs a quarter faster and stops circling. Only one in a wide area | 1400 |
| **Rimeback** | Lox | a smaller lox under a slate carapace that comes straight at you; its blows shove you back | 650 |
| **Scree Wing** | Drake | a horned drake whose hail is more stone than ice | 240 |
| **Cairn Wight** | Fenring | a bone-masked fenring that comes only at night, leaping like any fenring | 450 |
| **Ice Crawler** | Neck | a neck twice the size with a crest of shale and a long frost bite | 180 |

Frostfang drops wolf fangs and pelts and sometimes a wolf trophy; the Rimeback stone, crystal and lox meat; the Scree
Wing freeze glands and stone; the Cairn Wight bone fragments and crystal; the Ice Crawler neck tails and sometimes a
freeze gland. Frostfang never comes starred from the wild and grants no Forsaken power; the others can come with a
star.

The swamp and mountain creatures' bodies, animations and sounds are the game's own; their gear (crowns, mantles,
crusts, plates, masks) is embedded in the plugin. For now the gear vanishes when they die.

## With Elite Creatures Reborn

Neither mod needs the other. With both installed:

- Every creature here rolls stars and mutations like any creature, scaled by the world tier, and Elite Creatures
  Reborn's loot rules apply to their drops.
- The kraken is a boss there: it rolls boss stars and a boss aspect, but never **Twin** or **Phantom**. Its fight is
  built around one ship and one health bar, and a Twin would put two krakens on your hull. That takes Elite Creatures
  Reborn 3.12.0 or later, whose rules know the kraken; with an older version the kraken rolls nothing at all.
- A dormant mimic shows nothing of it - no size change, star colour, mutation effect or decorated name - until it
  wakes; its health is already scaled, so the first hit counts.
- A mimic split off by the Splintering mutation is born awake.
- `elite spawn ECP_RimeGiant 2` makes a two-star giant to try.

## Configuration

One file, `BepInEx/config/com.EliteCreaturesPack.cfg`, written on first run and reloaded while the game runs when you
edit it. Every entry has a description in the file. Every setting changes gameplay, so every one is synced from the
server (see "Server settings" below). Most changes reach the creatures already loaded; a changed health applies to new
ones. Switching a creature off stops new ones from appearing; the ones already in the world stay until they die.

### 1 - General

| Setting | Default | |
| --- | --- | --- |
| `Lock Configuration` | on | server only: every player uses the server's values and cannot override them locally |

### 2 - Crypt Mimic

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | crypt chests can be mimics |
| `Chance` | 15 | percent of the listed chests that are mimics, in crypts generated from then on |
| `Chests` | `TreasureChest_forestcrypt, TreasureChest_sunkencrypt` | the chest prefabs that may be mimics, separated by commas. Hildir's crypt chest (her quest key) and `TreasureChest_fCrypt` (it also stands in fortresses and troll caves) are left out on purpose |
| `Bite Cooldown` | 3 | seconds after a lunge before it can bite again; meanwhile it hops after you |
| `Bite Damage` | 20 | slash, of the lunge and the ambush, before stars (the crypt skeleton's sword: 25) |
| `Run Speed` | 5 | how fast it bounds after you (the crypt skeleton runs at 4); it wanders at half this |

### 3 - Greydwarf Slinger

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | greydwarfs can come as slingers |
| `Share` | 10 | percent of the greydwarfs spawning in `Biomes` that come as slingers |
| `Nests` | on | greydwarf nests spawn them too, not only the wild |
| `Biomes` | BlackForest | where; several separated by commas, like `BlackForest, Meadows` |
| `Shot Interval` | 3.5 | seconds between its shots, at the least |
| `Stone Damage` | 12 | blunt, before stars (a greydwarf's thrown rock: 10) |
| `Stone Speed` | 16 | metres a second the stone flies (a greydwarf's thrown rock: 12) |
| `Range` | 22 | metres it shoots from |

### 4 - Rime Giant

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | giants can appear |
| `Mountains` | 60 | percent of mountains that hold a giant, fixed by the world seed |
| `Chance` | 25 | percent per zone roll, while someone roams a mountain whose giant has not come yet |
| `Interval` | 3600 | seconds between a zone's rolls |
| `Biomes` | Mountain | the biomes whose regions count as mountains; several separated by commas |
| `Health` | 1500 | before stars (a forest troll: 600) |
| `Plates` | 8 | plates of ice it wears, 0 to 8 |
| `Armoured Damage` | 15 | percent of a physical hit that gets through with every plate on; each plate lost lets an equal part more through |
| `Shatter At` | 40 | percent of its health at which the plates left all break off |
| `Fire Per Plate` | 30 | fire damage that breaks one plate (a fire arrow against its weakness: about 33) |
| `Regrow Delay` | 12 | seconds unhurt, out of the fight, before the plates grow back |
| `Regrow Interval` | 5 | seconds per plate as they grow back |
| `Sweep Damage` | 75 | blunt, its fist, with heavy knockback (a troll's punch: 60) |
| `Slam Damage` | 60 | blunt, where its fists land in the slam |
| `Avalanche Damage` | 45 | blunt plus half as much frost, where the slam's wave rolls over you |
| `Avalanche Length` | 24 | metres the wave rolls on flat ground (far further downhill, a few metres uphill); 0 turns the avalanche off |
| `Boulder Damage` | 40 | blunt, plus as much frost in the burst around it |

### 5 - Kraken

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | krakens can appear |
| `Chance` | 0.3 | percent per zone roll while a ship with a crew crosses deep ocean: about one kraken every hour or two at sea |
| `Interval` | 3600 | seconds between a zone's rolls |
| `Min Depth` | 25 | metres of water it needs: it only spawns over water this deep and never follows a ship into shallower water |
| `Health` | 3000 | the game adds 30% for every other player within 100 m, as for any creature |
| `Swim Speed` | 3.5 | metres a second as it chases (rowing: about 2) |
| `Hunt Range` | 90 | metres at which it notices a ship; it gives up once the ship is further than this |
| `Attack Gap Min` | 2.5 | seconds from one attack to the next, at the least |
| `Attack Gap Max` | 4 | seconds from one attack to the next, at the most; each gap is random between the two |
| `Gap Factor Solo` | 1.1 | the gap is this many times longer with one player aboard (three or more: the gap as set) |
| `Gap Factor Pair` | 1.2 | the gap is this many times longer with two players aboard |
| `Tentacle Gap Factor` | 0.7 | under the ship the slams come quicker: the gap between them is this many times the attack gap |
| `Phase Attacks` | 6 | tentacle slams under the ship, then as many attacks from the head, and round again |
| `Slam Damage` | 60 | blunt, a tentacle slamming across the deck |
| `Ship Damage` | 40 | to the ship, each time a tentacle smashes it in the head phase |
| `Ship Hit Interval` | 10 | seconds between those smashes; a smash takes the next attack's turn |
| `Bite Damage` | 80 | pierce, its beak, at players near the rail |
| `Ink Damage` | 15 | blunt, the ink it spits |
| `Ink Blind` | 1.5 | seconds the ink covers the screen of a player it hits, before it fades |
| `Ink Interval` | 6 | seconds between its ink spits, at the least |
| `Shield Parry Damage` | 60 | pierce, to a creature whose blow you parry with the Kraken shield; players are never bitten |
| `Shield Parry Damage Per Level` | 10 | added for each upgrade of the shield |

The kraken's meat and the Kraken shield's stats are fixed; a food mod such as FeastMaster can tune the meat.

### 6 - Skeleton Crossbowman

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | the archer skeletons can come as crossbowmen |
| `Share` | 15 | percent of those skeletons' spawns that come as crossbowmen, wherever and whenever they spawn |
| `Damage Factor` | 1 | a bolt's blunt damage against the bow of the skeleton it replaces (Black Forest 20, Meadows 15, Swamps 55, Mountains 60) |
| `Shot Interval` | 6 | seconds between its shots, at the least (the archer: 4); the whole shot, spanning and loading included, takes about 5.5 of them |
| `Bolt Speed` | 40 | metres a second the bolt flies, straight (the archer's arrow: 30) |
| `Range` | 25 | metres it shoots from (the archer: 20) |

The game's four archer skeletons come as crossbowmen; the no-archer, poison, Hildir and summoned skeletons never do.

### 7 - Bone Crossbow

| Setting | Default | |
| --- | --- | --- |
| `Craftable` | on | the Bone Crossbow can be made at the workbench |
| `Recipe` | Wood:10:5, BoneFragments:12:6, LeatherScraps:4:2 | the cost, as item:amount:amount per upgrade (a placeholder for now); item names are the game's prefab names |
| `Workbench Level` | 1 | the workbench level it needs |
| `Damage` | 30 | blunt damage of its own blow, before the bolt's (the Arbalest: 200 pierce) |
| `Damage Per Level` | 4 | blunt damage each upgrade adds |
| `Reload Time` | 4 | seconds to span and load it with no Crossbows skill; the skill halves it (the Arbalest: 3.5) |

### 8 - Skeleton Arsenal

`Enabled` (on) switches all of them on or off at once. Below it, one switch per skeleton, all on: `Skeleton Cutthroat`,
`Skeleton Swordsman`, `Skeleton Axeman`, `Skeleton Bonebreaker`, `Skeleton Spearman`, `Skeleton Halberdier`,
`Skeleton Bowman`. Off: that skeleton no longer spawns (ones already in the world stay), and the others come no more
often than before. Everything else about the arsenal and the bone
weapons is fixed as described above.

### 10 to 19 - the swamp and mountain creatures

A section per creature: `10 - Mire Jarl`, `11 - Reed Stalker`, `12 - Bog Maw`, `13 - Fen Crawler`,
`14 - Drowned Shade`, `15 - Frostfang`, `16 - Rimeback`, `17 - Scree Wing`, `18 - Cairn Wight`, `19 - Ice Crawler`.
Each has the same five settings:

| Setting | |
| --- | --- |
| `Enabled` | it spawns in the wild; off, no new ones come and the ones already in the world stay |
| `Health` | its health before stars; applies to new ones |
| `Damage Factor` | times the damage of the attacks it takes from the creature it is built on |
| `Spawn Chance` | percent chance of one at each spawn attempt, in a swamp or mountain zone where someone is |
| `Spawn Interval` | seconds between those attempts |

Their defaults:

| Creature | `Health` | `Damage Factor` | `Spawn Chance` | `Spawn Interval` |
| --- | --- | --- | --- | --- |
| Mire Jarl | 900 | 1.35 | 4 | 900 |
| Reed Stalker | 160 | 1 | 12 | 360 |
| Bog Maw | 200 | 1.2 | 10 | 420 |
| Fen Crawler | 100 | 3 | 18 | 300 |
| Drowned Shade | 220 | 1.15 | 10 | 480 |
| Frostfang | 1400 | 1.4 | 3 | 1200 |
| Rimeback | 650 | 0.65 | 7 | 600 |
| Scree Wing | 240 | 1 | 10 | 480 |
| Cairn Wight | 450 | 1.1 | 8 | 600 |
| Ice Crawler | 180 | 5 | 15 | 360 |

### 25 - Crypt Executioner

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | on | off: no chamber gets one, and the ones already placed raise none |
| `Chambers` | 25 | percent of burial chambers that hold one, decided once per chamber |
| `Respawn Days` | 3 | game days before it returns to its chamber after it died |
| `Health` | 900 | its health before stars (a troll: 600) |
| `Axehead Chance` | 50 | percent chance it drops its axehead |
| `Slam Damage` | 60 | slash damage of the slam |
| `Shockwave Damage` | 35 | blunt damage of the ground scrape's shockwave |
| `Spin Damage` | 50 | slash damage of the spin |
| `Throw Damage` | 55 | slash damage of a thrown axe |
| `Rear Strike Damage` | 55 | slash damage of the strike behind it |
| `Summons` | 2 | skeletons it may have raised at once; 0 raises none |

### 26 - Executioner's Greataxe

| Setting | Default | |
| --- | --- | --- |
| `Recipe` | on | the greataxe can be made at the workbench |
| `Slash` | 55 | slash damage (a fully upgraded Bronze Axe: 55) |
| `Chop` | 49 | chop damage, for trees (a fully upgraded Bronze Axe: 49) |
| `Vertebrae` | 8 | vertebrae the recipe takes |
| `Bone Fragments` | 10 | bone fragments the recipe takes |
| `Workbench Level` | 3 | the workbench level it needs |

## Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

## Trying them

With `devcommands` on, the game's `spawn` command makes any of them: `spawn ECP_CryptMimic`,
`spawn ECP_GreydwarfSlinger`, `spawn ECP_RimeGiant`, `spawn ECP_Kraken` (at sea, near a ship with a crew),
`spawn ECP_SkeletonCrossbowman` (and `_Meadows`, `_Swamps`, `_Mountains`), `spawn ECP_BoneCrossbow`,
`spawn ECP_SkeletonSwordsman` (Cutthroat, Axeman, Bonebreaker, Spearman, Halberdier, Bowman; each also `_Meadows`,
`_Swamps`, `_Mountains`), `spawn ECP_BoneSword` (Dagger, Axe, Mace, Spear, Atgeir, Bow), `spawn ECP_Vertebra`,
`spawn ECP_ArrowBone 20`, `spawn ECP_Headsman` (the Crypt Executioner), `spawn ECP_ExecutionerAxehead`,
`spawn ECP_ExecutionerGreataxe`, `spawn ECP_MireJarl` (ReedStalker, BogMaw, FenCrawler, DrownedShade),
`spawn ECP_Frostfang` (Rimeback, ScreeWing, CairnWight, IceCrawler). A mimic comes dormant, a giant and a Bog Maw
asleep. The kraken's loot: `spawn ECP_KrakenBeak`, `spawn ECP_KrakenMeat`,
`spawn ECP_KrakenMeatCooked`, `spawn ECP_ShieldKraken`.

## Install

1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod. On a server, install it on the server and on every client: the creatures and the kraken's items
   are new objects, and every machine has to know them.

By hand, drop `EliteCreaturesPack.dll` into `BepInEx/plugins`. The config file is created on the first run. Elite
Creatures Reborn is optional.

## Multiplayer

Every creature is built the same way on the server and every client, and everything a player needs to see - asleep or
awake, the giant's plates, the mimic's disguise - travels in the world data the game already shares. Spawning is
decided where the game decides every spawn. The plugin carries its models built for Windows and for Linux, so a
dedicated server on either runs them.

The kraken is decided by one machine (whichever the game gives it to) and drawn by every machine on its own view of the
ship, so the tentacles ride the ship's rolling without lag. Each player is judged hit or missed on their own machine,
against what they see: a dodge that looks clear is clear. The held ship stays put on whichever machine sails it.

## Warnings

- Removing the mod removes its creatures from the world, and the kraken's beak, meat and shield, the Bone Crossbow,
  the bone weapons, bone arrows, vertebrae and the Executioner's axehead and greataxe from every inventory and chest as
  it loads: the game drops items it does not know. A crypt chest that became a mimic stays gone.
- The models are built for Windows and Linux. On macOS the mod tries the Windows build, and its own
  parts (the mimic's teeth, the slingshot, the giant's plates, the kraken, the crossbow, the bone weapons, the
  Executioner's axe, the swamp and mountain creatures' gear) may not show.
- Names and texts are English in every language for now.

## Files

- `plugins/EliteCreaturesPack.dll` - the mod, a single merged assembly with its models inside
- `config/com.EliteCreaturesPack.cfg` - the settings, synced from the server

## Building

.NET SDK 8, with Valheim installed and the `ValheimModLibs` folder next to this one:

```
dotnet build EliteCreaturesPack/EliteCreaturesPack.csproj -c Release
```

Override `-p:GamePath=...`, `-p:BepInExCore=...` or `-p:ModLibsPath=...` if your layout differs. The build merges
the shared libraries into one DLL and writes it to `dist/`. The models are built in the workspace's `AssetWorkshop`.

## Bugs and feature requests

The source is open, at the repository linked from the store page (`website_url` in `thunderstore/manifest.json`):
https://github.com/geraldjglasgow/ValheimMods. Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

## Shout outs

- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you
distribute that is built from it must carry the same freedoms and be released under the same licence, with source.
See the `LICENSE` file for the full terms.

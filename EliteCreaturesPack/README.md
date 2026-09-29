# Elite Creatures Pack

New creatures for Valheim, each with its own way to fight. A crypt chest that bites whoever opens it. A greydwarf
with a slingshot that shoots stones. A giant crusted in ice, asleep on the mountainside until night falls. A kraken
that hunts ships on the open sea, and the shield you make from its beak.

It works on its own. With **Elite Creatures Reborn** installed as well, every creature here rolls stars and mutations
like any other (the kraken, a boss, rolls stars and a boss aspect), and the mimic keeps its disguise until it wakes.

**0.1.0 is the first release.** The numbers will be tuned in play, and settings may still change before 1.0.0;
reports of anything that feels off are welcome (see "Bugs and feature requests" below).

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

## Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

## Trying them

With `devcommands` on, the game's `spawn` command makes any of them: `spawn ECP_CryptMimic`,
`spawn ECP_GreydwarfSlinger`, `spawn ECP_RimeGiant`, `spawn ECP_Kraken` (at sea, near a ship with a crew). A mimic
comes dormant and a giant asleep. The kraken's loot: `spawn ECP_KrakenBeak`, `spawn ECP_KrakenMeat`,
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

- Removing the mod removes its creatures from the world, and the kraken's beak, meat and shield from every inventory
  and chest as it loads: the game drops items it does not know. A crypt chest that became a mimic stays gone.
- The models are built for Windows and Linux. On macOS the mod tries the Windows build, and its own
  parts (the mimic's teeth, the slingshot, the giant's plates, the kraken) may not show.
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

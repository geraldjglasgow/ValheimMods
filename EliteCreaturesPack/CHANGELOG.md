# Changelog

## 0.3.0

The swamp and mountain creatures are gone again. 0.2.0 shipped ten creatures that were never meant for this mod: the
Mire Jarl, Reed Stalker, Bog Maw, Fen Crawler and Drowned Shade of the Swamps, and Frostfang, the Rimeback, Scree
Wing, Cairn Wight and Ice Crawler of the Mountains. Everything else from 0.2.0 stays: the Skeleton Crossbowmen and
the Bone Crossbow, the skeleton arsenal and the bone weapons, the Crypt Executioner and its greataxe.

- Those ten no longer spawn, and any already in a world no longer appear. They dropped only the game's own items, so
  nothing in an inventory or chest is lost.
- Their config sections, `10 - Mire Jarl` to `19 - Ice Crawler`, do nothing now and can be deleted from
  `com.EliteCreaturesPack.cfg`.

## 0.2.0

Skeletons armed with crossbows and weapons of bone, and those weapons for you to make; a Crypt Executioner in the
burial chambers; five new creatures in the Swamps and five on the Mountains. The crypt mimic, the slinger, the Rime
Giant and the kraken are unchanged, and an existing `com.EliteCreaturesPack.cfg` keeps every value: the new sections
are added to it on the first run.

- **The Skeleton Crossbowman: a skeleton that has to reload.** About one spawn in seven of the game's four archer
  skeletons (Black Forest, Meadows, Swamps, Mountains) is a crossbowman instead, with that skeleton's health, looks
  and loot. It shoots a straight, fast bolt as hard as that skeleton's arrow (blunt) from up to 25 metres, then takes
  about three and a half seconds to span and load: that's the moment to close in. It drops a few bone bolts now and
  then. Section `6 - Skeleton Crossbowman`.
- **The Bone Crossbow, for players.** Made at the workbench, it handles like the game's crossbow and adds 30 blunt of
  its own (+4 an upgrade) to whatever bolt it looses, with a 4 second reload before skill. Its recipe is a placeholder
  for now and can be set in `7 - Bone Crossbow`.
- **The skeleton arsenal: seven skeletons with weapons of bone.** One skeleton spawn in four is now a Cutthroat
  (dagger), Swordsman, Axeman, Bonebreaker (mace), Spearman, Halberdier (atgeir) or Bowman, in each of the four
  skeleton kinds, striking one blow at a time, never a combo. They drop what a skeleton drops, and one in ten a
  **Vertebra** as well. `8 - Skeleton Arsenal` switches them all, or each one, on or off; their numbers are fixed.
- **Bone weapons and bone arrows.** At a level 3 workbench, bone fragments and a vertebra make the arsenal's seven
  weapons (the dagger takes bone fragments only). Each handles like the bronze-age weapon of its kind at 0.85 of its
  damage and upgrades with bone fragments. Bone arrows, 20 for 8 bone fragments at a level 2 workbench, hit for 24
  pierce: better than wood, not as good as flint.
- **The Crypt Executioner: a mini boss in the burial chambers.** About one Black Forest burial chamber in four, older
  ones included, holds a skeleton headsman a head taller than the rest, with 900 health and a bone greataxe: a slam,
  a ground scrape whose shockwave hits, a spin, a strike behind it when you surround it, and, if you keep your
  distance, a thrown axe that shatters into a skeleton rising where it lands (two at most) while a new axe forms in
  its hands. It returns three days after it dies. Section `25 - Crypt Executioner`.
- **The Executioner's Greataxe.** Half the time the Executioner drops its axehead; with 8 vertebrae and 10 bone
  fragments at a level 3 workbench it makes a two-handed axe that hits like a fully upgraded bronze axe (55 slash, 49
  chop), with a combo of its own: a slash, a spin, a heavy overhead blow. It cannot be upgraded. Section
  `26 - Executioner's Greataxe`.
- **Five swamp creatures.** The **Mire Jarl**, a rare crowned draugr elite (900 health, slow heavy blows); the **Reed
  Stalker**, a fast draugr spearman; the **Bog Maw**, a tusked blob asleep until you come close; the **Fen Crawler**,
  an armoured crawler with quick bites that shrugs off arrows but not clubs; and the **Drowned Shade**, a wraith with
  a drowned bell that comes only at night. They spawn in the swamp's interior beside its own creatures. Sections
  `10 - Mire Jarl` to `14 - Drowned Shade`.
- **Five mountain creatures.** **Frostfang**, a rare giant wolf with a frost bite that turns pale and faster at half
  health; the **Rimeback**, a slate-backed grazer whose blows shove you back; the **Scree Wing**, a horned drake whose
  hail is part stone; the **Cairn Wight**, a bone-masked fenring that leaps out of the night; and the **Ice Crawler**,
  a shale-crested crawler with a frost bite. All are immune to frost and weak to fire. Sections `15 - Frostfang` to
  `19 - Ice Crawler`.
- **The same five settings for each of those ten:** `Enabled`, `Health`, `Damage Factor`, `Spawn Chance` and
  `Spawn Interval`, synced from the server like the rest.
- With Elite Creatures Reborn, every new creature rolls stars and mutations like any other.

## 0.1.0

First release: four new creatures, each with its own fight.

- **The Crypt Mimic: some crypt chests bite whoever opens them.** About 15% of the chests in burial chambers and
  sunken crypts (in crypts generated after the mod is installed) are mimics that look and read exactly like the chest
  until someone opens or hits one. Awake, it hops after you and lunges: dodge the lunge (a shield won't stop it) and hit
  it while it recovers. It has the crypt skeleton's health and resistances and drops the loot of the chest it was
  pretending to be.
- **The Greydwarf Slinger: a greydwarf that shoots stones.** About one Black Forest greydwarf in ten, in the wild and
  from nests, carries a slingshot and fights like a skeleton archer: it stands its ground and lobs a stone (12 blunt)
  from up to 22 metres every few seconds, and never claws. It aims where you stand when it lets go, so a dodge, a
  shield or running across its line beats it.
- **The Rime Giant: a frost-plated troll asleep on the mountains.** A very rare giant, at most one per mountain ever,
  on about three mountains in five. By day it sleeps like a snowy outcrop; at night, in a snowstorm or when hit, it
  wakes. Eight plates of ice let only 15% of weapon damage through until fire breaks them, and whatever is left
  shatters at 40% health. Its slam sends an avalanche rolling downhill and it hurls ice boulders that slow. 1500
  health; drops crystal, freeze glands and silver ore.
- **The Kraken: a boss of the deep ocean that hunts ships.** Outrun it under sail and it gives up. Let it catch you and
  it holds the ship still and fights the crew in turns: six tentacles slamming across the deck one at a time (one of
  them grabs a player and throws them into the sea), then its head at the rail, biting, lunging onto the deck and
  spitting ink that blacks out the screen, while a tentacle smashes the ship. 3000 health and a boss bar; its smashes
  are measured so a karve is a risk and a longship holds out. It works on any ship, modded ones included.
- **The kraken's loot: its beak, tentacle meat and the Kraken shield.** Each kraken drops its beak and 5 to 7 raw
  kraken tentacles onto the deck, with chitin and coins. Cooked on an iron cooking station, the tentacle beats cooked
  serpent meat on every count (80 health, 28 stamina, 4 healing a tick, 30 minutes). At a level 3 forge the beak, 10
  fine wood and 5 silver make the Kraken shield, better than the serpent scale shield on every count and able to
  parry: a creature whose blow you parry with it is bitten for 60 pierce, 10 more for each upgrade.
- **A settings section per creature, synced from the server.** On or off, how common, and the numbers of its fight,
  in `com.EliteCreaturesPack.cfg`; reloaded while the game runs, and bound to the server's values while
  `Lock Configuration` is on. Install on the server and every client.
- **Works alone, or with Elite Creatures Reborn.** With it, every creature here rolls stars and mutations like any
  creature and a dormant mimic shows none of it until it wakes. The kraken rolls boss stars and an aspect, never Twin
  or Phantom, with Elite Creatures Reborn 3.12.0 or later; with an older version it rolls nothing.

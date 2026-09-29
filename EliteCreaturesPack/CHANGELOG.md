# Changelog

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

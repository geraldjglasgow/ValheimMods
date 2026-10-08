# Changelog

## 0.8.2

- Fixed: bone piles and greydwarf nests no longer fill rooms with arsenal skeletons and slingers; they stop at their usual count.
- Fixed: wild arsenal skeletons and slingers count toward the game's spawn limits.

## 0.8.1

- With EliteCrafting 0.8.0: the Rime Giant's bonus is an Ascension Rune (the Consecrated Rune is gone), so its loot loads again.

## 0.8.0

- Paws of the Bear: each punch is three quick swipes in the time of one, about a third of the damage each.
- `Triple Strike` (section `27 - Bear Claws`) turns it off for the game's single punch.

## 0.7.2

- Fixed: a Greydwarf Slinger could spawn where another mod had blocked the greydwarf's spawn.
- Lighter on frame rate, memory and dedicated servers; joining another world in the same session is quicker.

## 0.7.1

- Discord link on the store page.

## 0.7.0

- With EliteCrafting: every creature drops runes; the Kraken, Rime Giant and Crypt Executioner drop extra.
- With EliteCrafting: bone weapons and the Kraken shield have item levels.
- The Kraken's loot lands on the ship's deck or the water, not the sea floor.

## 0.6.2

- Faster loading: the config file is written once instead of once per setting.

## 0.6.1

- Rime Giant: the ice boulder's blunt damage is halved (20 blunt + 40 frost by default).
- Less work every frame keeping the server's settings in sync.

## 0.6.0

- Skeletons: the plain skeleton, each arsenal skeleton and the crossbowman are equally likely; one switched off drops out.
- Skeleton Crossbowman: now switched by `Skeleton Crossbowman` in section 8; section 6 loses `Enabled` and `Share`.
- Skeleton Crossbowman: drops a spine like the other arsenal skeletons.
- Every bone weapon, the Bone Crossbow, its bolts and the Executioner's Greataxe: new low-poly models and icons.
- Bone Crossbow: 2.3 s reload, bolts at 60 m/s, no recoil or smoke ring; its recipe takes a spine.
- Fixed: the Executioner's Greataxe asks for spines, not bone fragments; it takes 1 (was 8).
- A saved `Recipe`, `Reload Time` or `Spines` still at its old default moves to the new one.

## 0.5.0

- Crypt Executioner: only its axe head hits in the slam, sweep and spin.
- Crypt Executioner: the ground scrape loses its stones and shockwave, the slam winds up faster, knockback is halved.
- New `Axe Head Radius` and `Slam Windup Speed`; `Shockwave Damage` is now `Sweep Damage`: set it again if changed.
- The Executioner's Greataxe sounds like the battleaxe.
- The Vertebra is now a Spine with its own model, dropped and used in recipes as before.
- Vertebrae you have are lost.
- The greataxe's `Vertebrae` setting is now `Spines`: set it again if changed.

## 0.4.2

- Fixed: since 0.2.0 the mod loaded no creatures, items or recipes. The greataxe's section is now
  `26 - Executioner Greataxe` (no settings lost).
- Fixed: the Crypt Executioner, its axehead and the Executioner's Greataxe were missing; the Executioner now bursts
  into bones on death.
- Fixed: the Kraken shield was plain white.

## 0.4.1

- The bone weapons need a level 2 workbench (was 3).
- The Bone Crossbow and its bolts need a level 2 workbench (was 1); existing configs keep 1: set `Workbench Level`
  to 2.
- The Kraken shield has one handle, in the middle of the board.

## 0.4.0

- The crossbowman and arsenal skeletons replace only Black Forest skeletons; their other versions are gone.
- New Blunted Bone Bolts for any crossbow, dropped by crossbowmen; settings `Bolt Recipe`, `Bolts Per Craft`,
  `Bolt Damage`.
- The Bone Crossbow reloads in 3 seconds (was 4) with its own animation, your right hand on its stock.
- Its string snaps back on the shot, and the crossbowmen's bolts are now blunt with no feathers.
- The Bone Atgeir has its own thrust, second thrust and sweep, both hands on the haft.
- The Executioner's Greataxe keeps your left hand on its haft.

## 0.3.0

- Removed the ten Swamps and Mountains creatures 0.2.0 shipped by mistake; any already in a world no longer appear.
- Nothing in an inventory or chest is lost: they dropped only the game's own items.
- Their sections, `10 - Mire Jarl` to `19 - Ice Crawler`, do nothing now and can be deleted.

## 0.2.0

- Skeleton Crossbowman: one archer skeleton in seven shoots a fast blunt bolt, then must reload.
- Bone Crossbow for players: like the game's crossbow plus 30 blunt; its recipe is a placeholder.
- Skeleton arsenal: one skeleton in four carries a bone weapon, one in ten of them drops a Vertebra;
  `8 - Skeleton Arsenal` switches them.
- Bone weapons: the arsenal's seven at a level 3 workbench (0.85 of bronze-age damage); Bone Arrows at level 2.
- Crypt Executioner: a skeleton mini boss with a bone greataxe in about one burial chamber in four.
- Executioner's Greataxe: a two-handed axe made from its axehead, with a combo of its own.
- Five Swamps creatures: the Mire Jarl, Reed Stalker, Bog Maw, Fen Crawler and Drowned Shade.
- Five Mountains creatures: Frostfang, the Rimeback, Scree Wing, Cairn Wight and Ice Crawler.
- With Elite Creatures Reborn, every new creature rolls stars and mutations.
- An existing config keeps every value; the new sections are added.

## 0.1.0

- Crypt Mimic: about 15% of crypt chests, in crypts generated after install, bite whoever opens them.
- Greydwarf Slinger: about one Black Forest greydwarf in ten shoots stones from a distance and never claws.
- Rime Giant: a very rare ice-plated troll asleep on the mountains by day; fire breaks its plates.
- Kraken: a deep-ocean boss that hunts ships; outrun it under sail, or fight it while it holds your ship.
- Kraken loot: its beak, tentacle meat that beats serpent meat, and the Kraken shield, whose parry bites back.
- A settings section per creature, synced from the server; install on the server and every client.
- With Elite Creatures Reborn, every creature rolls stars and mutations; a dormant mimic shows none until it wakes.
- The kraken rolls boss stars and an aspect, never Twin or Phantom, with Elite Creatures Reborn 3.12.0 or later.

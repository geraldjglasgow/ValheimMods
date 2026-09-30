# Changelog

## 1.11.0
- OpenKeep leaves the inventory's look alone: the game's armour and weight readouts stay exactly as the game draws
  them (the brown boxes are PackPanel's), and the trash can is a plate in the game's own style under the armour,
  showing a bin and "Trash". It sits where trash can mods put theirs, so Jewelcrafting shows it while you drag a stack
  (its synergy box takes the spot otherwise), CurrencyPocket and OttoPay move it along with the armour, and TrashItems
  makes no second can. `Trash Can On Stat Column` off still puts the can in the button row.
- The trash can no longer stays red after a click.

## 1.10.0
- Fixed: since 1.8.0 OpenKeep broke Jewelcrafting and every other mod that adds a box beside the game's armour box
  (CurrencyPocket, OttoPay, TrashItems, Quick Stack Store Sort Trash Restock). The stat column moved the game's armour
  and weight boxes into a container of its own, and those mods look for them on the inventory panel by name.
  Jewelcrafting then threw an error on every item picked up, every inventory close and every input device change,
  and a chest closed that way stayed in use. The game's boxes now stay where the game has them and are placed in the
  column from there (GitHub #4, #5).
- The boxes those mods add join the column instead of covering one of its boxes: Jewelcrafting's synergy box right
  under the armour, a trash can next to OpenKeep's (TrashItems' takes dragged stacks there), the coin pocket between
  the armour and the weight.
- Stow: `Trash Can On Stat Column` (on, per player). Off puts the trash can at the end of the button row, and OpenKeep
  then leaves the game's armour and weight boxes exactly as the game has them, for inventory mods that place their own
  boxes there. Takes effect the next time you enter a world.

## 1.9.0
- Homestead: `Auto Repair` (on): opening a workbench, forge, black forge, galdr table, artisan table or any other
  station with the game's repair button repairs every item in your inventory that this station can repair at its
  current level, what you wear included, as if you pressed its repair button once for each: the same items (a forge
  item waits for a forge, an item needing a higher level waits for the extensions), the same Crafting skill gain, no
  cost. One repair sound and one message, `Repaired 3 items`; nothing when nothing needed repair. A cart with
  `Cart Workbench` on repairs like a workbench. Synced; off leaves only the game's button.
- Stow: a full chest no longer stops a stack. Ctrl + click and Store one (`V`) send what the chosen container has no
  room for on to the next nearest container that holds the item, and the next, until the stack is gone or none is
  left within `Nearby Range`: `Sent Wood to Chest and 1 more`. A stack routed to a chest only for its group or its
  `accept` list still stays in your inventory when that chest is full.
- Stow: with `Shared Chests = Full`, what a chest another player has open had no room for after a quick stack or dump
  goes on to the next nearest chest holding the item, with a line in the top left saying where; before, it stayed in
  your inventory.
- Stow: ground pickup leaves each drop to one chest: a chest already holding the item before one that only accepts
  it, then the one nearest to the drop, and the next when that one is full. Before, whichever chest swept first took
  it. A kiln's coal or a smelter's bars dropped at the station's output go into the nearest pickup chest holding them.
- Reach: a unit borrowed from a chest for a station that the station did not take goes back to the nearest chest
  holding that item, before other chests that accept it.

## 1.8.0
- Batch crafting: the Craft tab of every crafting station (and crafting by hand) has `-` and `+` buttons with the
  amount between them, left of the Craft button; Craft makes that many at once, in one bar, through the game's own
  multi-craft, so each item costs, earns skill and rolls the crafting bonus as if crafted alone. Click the number to
  type an amount. Shift steps by tens, Ctrl jumps to 1 or to as many as you can make, the D-pad steps on a gamepad.
  The amount stops at your materials (nearby chests count with Reach), your free space and `Max Amount` (100), and
  goes back to 1 when you pick another recipe. Not on the Upgrade or Salvage tab. It takes the place of the game's
  Shift + Craft (5 at once), which `Enabled = false` brings back. New section `10. Batch Crafting` (`Enabled`, on;
  `Max Amount`), both synced. Reach's Pull modifier + Craft pulls materials for the whole batch.
- Works with PackPanel, the new inventory mod (a bigger grid, labelled armour, utility, food, mead and ammo slots, a
  coin purse, a key ring and backpacks): with both installed, the button row and the trash can sit inside the
  inventory panel, quick stack, store all, dump, sort and a shared chest's stack all use PackPanel's whole grid and
  never touch its slots or key ring, and PackPanel's Key Stack is written with OpenKeep's stack sizes, so an
  `OpenKeep.Stacks.yml` entry for a key still wins. Neither mod needs the other.
- Stow: Shift + click on the trash can starts trash mode: the pointer becomes the trash can and every stack you click
  in the inventory or the open chest is destroyed at once, with no popup even with `Confirm Trash` on (favourites
  and worn items are kept); let go of Shift to stop. A click without Shift still destroys a dragged stack.
- Stow: the trash can's plate sits right under the armour plate, so a plate another mod adds to the column goes
  below it.
- Hotkeys with a modifier (`LeftAlt + D` and the like) now fire while you walk; before, holding any other key
  (such as W) blocked them.
- Homestead: tamed animals eat from chests (`Pets Eat From Chests`, on; `Pet Chest Range`, 10 m): a hungry tame
  walks to a container within range that holds food it eats and eats one item, as it would from the ground. Food on
  the ground still comes first, and animals still being tamed eat only from the ground. The `containers:` rules of
  the Reach YAML apply, as for fires.
- Homestead: `Auto Feed Leave` (1): stations that feed themselves leave that many of each item in every chest, so a
  chest keeps its last ore, wood or coal and quick stack still sends that item to it. 0 takes everything, as before;
  feeding a station by hand is not limited.
- Homestead: `Auto Feed Range` is 4 m by default (was 2 m), so chests a step away from a station feed it too. An
  existing .cfg keeps the value it has.

## 1.7.0
- Capacity: `OpenKeep.Stations.yml` sets how much each workstation holds: `items:` (ore, wood, flax, barley) and
  `fuel:` (coal, wood) for the smelter, blast furnace, charcoal kiln, eitr refinery, spinning wheel, windmill, hot tub
  and every modded station built like them, from 1 to 1000. The first world load lists every station with its
  vanilla values, commented out. Synced from the server, hot reloaded, editable in game, and switched off with
  Capacity's `Enabled`. Lowering a cap loses nothing: a fuller station keeps working and takes more once it is below
  the cap. `openkeep write docs` also writes `OpenKeep.Stations.txt`.
- Homestead: `Auto Feed Stations` (on): smelters, blast furnaces, charcoal kilns, eitr refineries, spinning wheels,
  windmills and hot tubs you built take what they work (ore, scrap, wood, soft tissue, flax, barley) and their fuel
  (coal, sap, wood) from the containers beside them, one item and one fuel a second while there is room.
  `Auto Feed Range` (2 m) runs from the station's outer edge to the middle of a container. `Auto Feed Skip` names
  items never taken (default `FineWood, RoundLog`, so the charcoal kiln burns only plain wood). The `stations:` and
  `containers:` rules of the Reach YAML apply, as for fires.
- Homestead: `Rested Delay` (5 s): the Rested buff comes after 5 seconds of resting by a fire instead of the game's
  20. How long it lasts, the comfort level and what counts as resting are unchanged; 20 keeps the game's wait.
- Homestead: `Area Repair` (on): repairing a piece with the hammer also repairs the damaged pieces touching it (only
  its direct neighbours, at most 64 a swing), each with the checks of a repair by hand (its crafting station in
  range, access to its ward). One swing's stamina and hammer wear pays for all of them.
- Homestead: `Torch Switch Key` (O, per player): look at a torch and press it to keep that torch lit day and night;
  press it again to put it back on the night schedule (by day it goes out at once). The hover shows the key and
  what it does, the choice is stored in the torch for every player, and a ward keeps out players without access.
- Homestead: `Torch Margin` (1 in-game hour, 0 to 4): torches light that long before nightfall and go out that long
  after daybreak. An in-game hour is 75 seconds of the game's 30 minute day; 0 keeps the game's own night.
- Homestead: `Auto Fuel` skips fuel from a lower world level (New Game+), which the game refuses by hand too.

## 1.6.0
- Homestead: new section `8. Homestead`, synced from the server.
- Homestead: `Nearest Bed Respawn` (on): every bed you own is a spawn bed. After death you wake in your own bed
  nearest to where you died, or the next nearest when that one is gone or no longer yours. Any of your beds lets you
  sleep (the hover says Sleep instead of Set spawn point). Beds claimed before this version count once you have
  been near them.
- Homestead: `Build On Wood` (default `fire_pit`): campfires can be built on wooden floors. The list takes the other
  pieces the game keeps off wood too (`bonfire`, `smelter`, `charcoal_kiln` and more); empty keeps the game's rule.
  In the Ashlands, or with the Fire world key, the campfire's embers can set the floor alight.
- Homestead: `Honey Per Day` sets how much honey each beehive makes per in-game day (0, the default, keeps the
  game's 1.5 a day); `Honey Per Player Online` makes it the number of players on the server. A hive still holds at
  most 4.
- Homestead: `Auto Fuel` (on): campfires, hearths, braziers, torches and every other fire you built refill themselves
  with their own fuel (wood, resin, coal, guck, greydwarf eyes) from containers within `Auto Fuel Range` (20 m) of
  the fire, a unit at a time as they burn; the `stations:` and `containers:` rules of the Reach YAML apply.
- Homestead: `Torches Night Only` (on): the standing torches and the sconce light at nightfall and go out at
  daybreak, saving their fuel; `Torch Pieces` lists which fires (braziers, lanterns and the resin candle can be
  added). A torch out for the day says `Lights at nightfall`. Turn it off before removing OpenKeep: the game has no
  switch to light them again.
- Stow: the tooltips of the armour, trash and weight plates (and Elite Creatures Reborn's world tier plate, once
  that mod is rebuilt with this version of the plate column) show in a dark box with a gold border, pinned just
  right of the plate instead of following the mouse.
- Stow: ground pickup's `Pickup Range` defaults to 2 m instead of 10. A config written by an earlier version keeps
  the value it saved: set it to 2 there.

## 1.5.0
- Stow: the armour, trash and weight plates on the right of the player panel each show a tooltip on hover saying
  what they are.
- Stow: the plates are one column other mods can add to. With Elite Creatures Reborn 3.8.0 its world tier plate
  sits under the weight, and all four move up and stay evenly spaced.

## 1.4.2
- Stow: quick stack, store all and dump leave the hotbar alone, as the sort always has. Items on the hotbar stay
  there; Store one, Ctrl + click routing and the trash still act on a hotbar item you pick, and top up still
  refills stacks there.

## 1.4.1
- The `openkeep signs` and `openkeep write docs` console commands call the Signs and Stacks code directly instead
  of looking it up by name through reflection. No change in behaviour.

## 1.4.0
- Stow: quick stack, store all, dump and sort only work on the rows of the player inventory that the game gives you
  (four, more once bought from the trader). Mods such as ExtraSlots keep their equipment, food and ammo slots in rows
  below those: these actions no longer empty them into chests, and the sort no longer pulls items out of them or
  fills them. Top up still refills the stacks there.
- Stow: new per-player setting `Main Inventory Rows` (0: the game's rows). Set a number when a mod adds ordinary
  inventory rows that should be stowed and sorted too.
- Stow: the trash can moved off the button row onto its own wood plate, with a new icon, between the armour and
  weight readouts on the right of the player panel. The row below the panel is now four buttons and re-centred.
- Stow: the armour and weight readouts show their icon larger and centred on the plate, with the number on top,
  and both stay where they are when the inventory grows with rows bought from the trader.
- Stow: `Confirm Trash` now defaults to off, so the Trash Key, the trash can and Destroy Junk act at once. A config
  written by an earlier version keeps the value it saved: set it to false there to lose the popup. Turn it on to
  get the game's popup back.
- Stow: the `Stow all` button is `Store all` again, next to `Store one`; `Stow All Key` is `Store All Key` and a
  cfg from 1.1.0 or 1.2.0 keeps its binding. The section `2. Stow` and `OpenKeep.Stow.yml` keep their names.
- Salvage: new synced settings `Skip Items With Mod Data` (on) and `Mod Data Prefixes` (`ecf_`). An item carrying
  custom item data under one of the prefixes — data another mod keeps on it, such as EliteCrafting's magic affixes —
  is no longer listed in the Salvage tab and the Salvage Key refuses it with a reason, so salvaging cannot destroy
  what that mod added. With EliteCrafting installed, OpenKeep salvages ordinary items into materials and
  EliteCrafting grinds its magic items into shards.

## 1.2.0

- Reach: new `stations:` map in `OpenKeep.Reach.yml` — per station prefab `enabled` and `allow` / `deny` item
  lists for station feeding (interact, Fill, Pull and the `From storage` hover line). Deny a meat and it is never
  pulled onto the cooking station; crafting and building still see it. Unlisted stations behave as before.
- Stow: new per-player setting `Sort Favourite Items` (on, the old behaviour). Off: the sort leaves favourite
  items where they are — for items kept in extra slots other mods add, which the sort used to pull into the main
  inventory.

## 1.1.1

- Multiplayer: joining a busy server no longer fails with "you have no copy" when every player runs the same
  version. The join check raced the config push over the same connection and gave up after a single 5 s deadline;
  it now greets first, retries every 5 s and only refuses after 20 s without any answer.
- Capacity: a container entry uncommented without its indentation (at the top of the file instead of inside
  `containers:`) is now applied anyway, with a warning explaining the indentation. Generated files comment
  entries as `  # piece_chest_wood: ...` so removing the `#` keeps the indentation.

## 1.1.0

- Stacks: config and YAML edits now reach items already picked up, items lying in the world and an open chest.
- Server sync moved to Charter, our own library. Existing config files work unchanged.
- Version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter`: `status`, `diff`, `versions`.
- Salvage: YAML key `exclude` renamed `deny`. A file still using `exclude:` warns once and its list is ignored.
- Stow: keys renamed — `Store All Key` → `Stow All Key` (`G`), `Restock Key` → `Top Up Key` (`R`),
  `Trash Flag Key` → `Junk Key` (`J`), `Trash Flagged Key` → `Destroy Junk Key` (`LeftShift + Delete`).
  `Store One Key` now `V`. Existing bindings carry over.
- Stow: junk marks move to `OpenKeep.junk` in character data. Marks from 1.0.0 are not read.
- Stow: buttons moved below their panels so they never cover the last item row; new `Button Row Offset`.
- Removed the unused `Hover Preview` and `Cart Upgrade Piece` settings.

## 1.0.0

- First full release: the 0.2.0 build under its final version number, no code changes.

## 0.2.0

- Signs: a vanilla sign above every player-built container naming its contents, most numerous first. Per-prefab
  switch, offset and rotation in `OpenKeep.Signs.yml`. Off by default. Edited signs keep your words; a sign
  removed with the hammer stays gone until reset. None on ships, carts, dungeon chests or spawned treasure.
- Console: `openkeep signs`, `signs reset`, `signs rewrite`.

## 0.1.0

- First release.
- Reach: crafting, upgrading, building and station feeding count and pay from nearby containers, ship holds and
  carts. Fill and Pull modifiers, per-character toggle, link lines, per-container rules.
- Stow: quick stack, stow all, top up and sort with hotkeys, favourites, junk marks and a trash can, routing by
  click, store one, find, dump, chest cycling, ground pickup by rule.
- Salvage: a Salvage tab and hotkey; return fraction, rounding, upgrade materials, recipe and station
  requirements, per-item fractions.
- Stacks: stack and weight multipliers, absolute values, ignore teleport restriction, merge into chests.
- Capacity: container sizes per prefab, fill and contents in the hover text.
- Carts: a workbench on every cart, with its own level and range.
- Shared chests: `View` shows a chest another player has open, read-only; `Full` lets two players work the same
  chest, every change sent to the owner's game to apply or refuse.
- Console command `openkeep`. Every gameplay setting synced and lockable, every YAML file synced and hot reloaded.

# Changelog

## 5.0.0

- Boots moved to the new mod EliteEquipment: section `16. Boots` is gone and boots made with OpenKeep disappear. Install EliteEquipment to wear boots.
- Blueprints: the Blueprint Bench (5 Wood) shares blueprints with everyone on the server; take theirs into your library.
- Blueprints: Shift + G + click picks every piece of that type in the building (Copy building, Site planner).
- `OpenKeep.Containers.yml` lists every container prefab itself, adding those of mods installed later; `OpenKeep.Containers.txt` is gone.

## 4.2.0

- Boots: each pair is the game's own footwear from its leggings, cut at the ankle, with new icons; Rag Shoes are cloth foot-wraps.
- Boots: early sets' boots cost three times their share (Padded twice), at least 2 of each material; upgrade kits are not split.
- Fixed: EliteCrafting inscriptions on worn boots now count.

## 4.1.0

- Boots (`Separate Boots`, off by default): leggings split into trousers and boots (80/20 stats and cost); boots give +5% speed.
- Recipe List: category buttons (Weapons, Armour, Food...) under the search filter the recipes (`Categories`).
- Salvage: EliteCrafting items show their rarity backdrop, seal and sockets.
- Fixed: without PackPanel, the trash can sits centred on its plate.

## 4.0.0

- Stow is now called Store: `2. Stow` settings and `OpenKeep.Stow.yml` carry over by themselves.
- Mímir's Chest (`Custom Storage Chests`, off by default): a chest that never fills, with search, filters, sorting and 9999 per stack.
- Salvage: with EliteCrafting, Magic and Rare items are salvaged and may give a rune back (chance shown).
- Store: the mouse wheel scrolls a chest taller than its panel instead of cycling chests.
- Homestead: a bed picked on the death map wakes you as soon as its land has loaded.
- Removed: `Bed Choice Seconds`, `Quick Respawn Range`, `Quick Respawn Seconds` and `Quick Area Loading` (now fixed). Nothing to do.
- Fixed: typing in OpenKeep's search or amount fields no longer walks your character.
- Fixed: after Alt + Tab, Shift, Ctrl or Alt no longer counts as held.
- Update the server and every client together.

## 3.0.0

- Removed: `Quick World Save`. The world saves with the game's own code again, which game 1.0.17 fixed. Nothing to do; the old setting is ignored.

## 2.4.0

- Quick World Save (on by default): the game stands still for a much shorter moment while the world saves. Server or host only.

## 2.3.0

- Reach: ship and cart storage counts only for the player sailing or pulling it.
- Reach: crafting from a chest another player used last may first say "Fetching the materials from storage, try again".
- Fixed: chests and drops another player's game held could lose or duplicate items.
- Shared chests and Blueprints: a moved stack or handed-over materials come back if no answer arrives.
- Fixed: hotkeys ignored after Shift, Ctrl or Alt was released in another window.
- Lighter on frame rate and server near many chests, signs and construction sites.
- Update the server and every client together.

## 2.2.1

- Blueprints: the build camera comes out with B, as in normal building, instead of by itself.
- Blueprints: ghost buildings are a little easier to see.

## 2.2.0

- Stow: `Auto Tidy` (off by default): nearby chests send stray items to the chest they belong in, learning where players put things.
- Blueprints (off by default): a Blueprints tab in the hammer saves buildings and places them as construction sites built as materials come in.
- Blueprints: Copy building, folders, Fix ground and a Site planner queue.
- Stow: chest cycling with the wheel waits while EliteCrafting scrolls a tooltip.
- Console: `openkeep tidy` and `openkeep blueprint`.

## 2.1.1

- Recipe Tracker: narrower, only as wide as its rows; `-`, `+` and the amount show with the inventory open.
- Recipe Tracker: an X at the left of each recipe removes it while the inventory is open.
- Fixed: the tracker's `-`, `+` and `X` buttons never showed.

## 2.1.0

- Salvage: the tab's list leaves out the hotbar and PackPanel's slots; the Salvage Key still works on them.
- Salvage: with Epic Loot, magic items show their rarity background on the tab.

## 2.0.2

- Fixed: with Epic Loot, recipe tile and button tooltips showed far from what they describe.

## 2.0.1

- Homestead: on the bed choice map a double click places a map pin, also on a bed.
- Reach: lighter on the frame rate with the crafting or build panel open near many chests.
- Faster loading: the config file is written once instead of once per setting.

## 2.0.0
- Quick portals moved to Wayfare: install it for quick jumps. `Quick Area Loading` now speeds respawns only.
- Build Camera: build from a free camera near a crafting station (B with a build tool).
- Recipe List: search (Ctrl+F, `@material`), favourite recipes, grid views.
- Recipe Tracker: right click a recipe to pin its materials on screen.
- Batch crafting: `Craft Speed` makes crafting faster or slower; the mouse wheel changes the amount.
- Homestead: on the bed choice map, beds sit above other icons; the nearest is pinged.
- Fixed: Ctrl + click stopped at a full chest instead of filling the open chest.

## 1.14.0
- Stow: Ctrl + click on an item no nearby chest holds puts it in the chest you have open.
- Reach: Epic Loot's enchanting table uses materials from nearby chests (the `Crafting` switch).

## 1.13.1
- Fixed: no more "LiberationSans SDF Font Asset was not found" warnings from chest link labels and the bed countdown.

## 1.13.0
- Stow: `Take All Key` (Shift+G) takes everything from the open chest, shared chests included.
- Stow: new `Sort Order` `Amount`: the item you have the most of comes first.
- Homestead: your bed icons are yellow on the minimap and the large map.
- Homestead: while you choose a bed after death, bed icons are twice the size and pulsing; the countdown sits upper
  left.

## 1.12.0
- Homestead: `Bed Choice Seconds`: dying opens the map to pick a bed; otherwise you wake in the nearest.
- Homestead: every bed you own shows on your map (`Beds On Map`).
- Homestead: `Quick Respawn`: the nearer your bed to where you died, the sooner you wake.
- Homestead: `Stand Up On Respawn`: you wake standing after a death.
- Homestead: `Quick Portals`: the nearer two portals are, the quicker the jump.
- Homestead: a jump within the loaded area skips the black screen (`Portal Screen Only When Loading`).
- Homestead: `Quick Area Loading`: land loads much faster after a long jump and while you respawn.

## 1.11.0
- The game's armour and weight readouts stay as the game draws them; the trash can is a plate in the game's style.
- The trash can sits where trash can mods expect one, so Jewelcrafting, CurrencyPocket, OttoPay and TrashItems fit
  around it.
- Fixed: the trash can stayed red after a click.

## 1.10.0
- Fixed: Jewelcrafting threw errors on every pickup and inventory close, and chests closed that way stayed in use
  (GitHub #4, #5).
- Fixed: mods that add a box beside the armour (CurrencyPocket, OttoPay, TrashItems and others) were broken since 1.8.0.
- Boxes other mods add join the stat column instead of covering one of its boxes.
- Stow: `Trash Can On Stat Column` (per player): off puts the trash can in the button row.

## 1.9.0
- Homestead: `Auto Repair`: opening a crafting station repairs every item it can, worn gear included, for free.
- Stow: Ctrl + click and Store one (`V`) go on to the next chest holding the item when one is full.
- Stow: with `Shared Chests = Full`, quick stack and dump send what a shared chest cannot take to the next chest.
- Stow: ground pickup gives each drop to one chest, one holding the item first, then the nearest.
- Reach: a unit a station did not take goes back to the nearest chest holding that item.

## 1.8.0
- Batch crafting: `-` and `+` beside the Craft button make many at once, in place of Shift + Craft.
- Batch crafting: Reach's Pull modifier + Craft pulls the materials for the whole batch.
- Works with PackPanel: stowing and sorting use its grid but never its slots.
- Stow: trash mode (Shift + click the trash can) destroys each stack you click; the can sits under the armour.
- Fixed: hotkeys with a modifier (`LeftAlt + D`) did not fire while W was held.
- Homestead: `Pets Eat From Chests`: hungry tamed animals eat from nearby chests.
- Homestead: `Auto Feed Leave` (1): self-feeding stations leave that many of each item in every chest.
- Homestead: `Auto Feed Range` defaults to 4 m (was 2 m); existing configs keep theirs.

## 1.7.0
- Capacity: `OpenKeep.Stations.yml` sets how many items and how much fuel each workstation holds.
- Homestead: `Auto Feed Stations`: smelters, kilns and other workstations take items and fuel from chests beside them.
- Homestead: `Rested Delay`: Rested comes after 5 seconds of resting instead of 20.
- Homestead: `Area Repair`: the hammer also repairs the damaged pieces touching the one you repair.
- Homestead: `Torch Switch Key` (O) keeps a torch lit day and night, or puts it back on schedule.
- Homestead: `Torch Margin`: torches light before nightfall and go out after daybreak by this many hours.
- Homestead: `Auto Fuel` skips fuel from a lower world level (New Game+).

## 1.6.0
- Homestead: new section `8. Homestead`, synced.
- Homestead: `Nearest Bed Respawn`: you wake in your own bed nearest to where you died.
- Homestead: `Build On Wood`: campfires and other listed pieces build on wooden floors.
- Homestead: `Honey Per Day` and `Honey Per Player Online` set each beehive's honey rate.
- Homestead: `Auto Fuel`: fires you built refill from nearby chests.
- Homestead: `Torches Night Only`: torches burn only at night; turn it off before removing OpenKeep.
- Stow: plate tooltips show in a gold-bordered box beside the plate.
- Stow: `Pickup Range` defaults to 2 m (was 10); existing configs keep theirs.

## 1.5.0
- Stow: the armour, trash and weight plates show a tooltip on hover.
- Stow: other mods can add plates to the column; Elite Creatures Reborn 3.8.0 adds its world tier plate.

## 1.4.2
- Stow: quick stack, store all and dump leave the hotbar alone; top up still refills it.

## 1.4.1
- Internal cleanup of the console commands; no behaviour change.

## 1.4.0
- Stow: quick stack, store all, dump and sort use only the game's own rows, leaving other mods' slots alone.
- Stow: new per-player `Main Inventory Rows` for mods that add ordinary rows.
- Stow: the trash can moved onto its own plate between the armour and weight readouts.
- Stow: armour and weight readouts show a larger icon and stay put as the inventory grows.
- Stow: `Confirm Trash` defaults to off; existing configs keep theirs.
- Stow: `Stow all` is `Store all` again; bindings carry over.
- Salvage: `Skip Items With Mod Data` keeps items with another mod's data, such as EliteCrafting's, out of salvage.

## 1.2.0
- Reach: new `stations:` map in `OpenKeep.Reach.yml`: per station `enabled` and `allow` / `deny` lists for feeding.
- Stow: new per-player `Sort Favourite Items` (on): off leaves favourite items where they are when sorting.

## 1.1.1
- Fixed: joining a busy server failed with "you have no copy" when everyone ran the same version.
- Capacity: a container entry uncommented without its indentation is applied anyway, with a warning.

## 1.1.0
- Stacks: edits reach items already picked up, lying in the world or in an open chest.
- Server sync moved to Charter, our own library; config files work unchanged.
- A version mismatch refuses the join with a named reason.
- New console command `charter` (`status`, `diff`, `versions`).
- Salvage: YAML key `exclude` renamed `deny`; old `exclude:` lists are ignored.
- Stow: keys renamed (`Stow All Key`, `Top Up Key`, `Junk Key`, `Destroy Junk Key`), `Store One Key` is `V`;
  bindings carry over.
- Stow: junk marks from 1.0.0 are not read.
- Stow: buttons moved below their panels; new `Button Row Offset`.
- Removed the unused `Hover Preview` and `Cart Upgrade Piece` settings.

## 1.0.0
- First full release: the 0.2.0 build under its final version number.

## 0.2.0
- Signs: a vanilla sign above every player-built container names its contents (off by default; rules in
  `OpenKeep.Signs.yml`).
- Console: `openkeep signs`, `signs reset`, `signs rewrite`.

## 0.1.0
- First release.
- Reach: crafting, building and station feeding count and pay from nearby containers, ship holds and carts.
- Stow: quick stack, stow all, top up, sort, favourites, junk, trash can, routing, find, dump, chest cycling, ground
  pickup.
- Salvage: a Salvage tab and hotkey, with per-item fractions.
- Stacks: stack and weight multipliers and per item values.
- Capacity: container sizes per prefab, fill and contents on hover.
- Carts: a workbench on every cart.
- Shared chests: `View` and `Full` modes for a chest another player has open.
- Console command `openkeep`; every gameplay setting synced and lockable, YAML files synced and hot reloaded.

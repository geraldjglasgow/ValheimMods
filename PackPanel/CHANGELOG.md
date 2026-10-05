# Changelog

## 0.8.1

- Fixed: bait can be dragged into the tacklebox's cells, and picked-up bait goes into the box.
- Picked-up bait no longer fills the Ammo slots.

## 0.8.0

- Backpacks are equipment: the worn pack shows as equipped, and Epic Loot can enchant it with utility effects.
- Less work every frame for the inventory, the HUD boxes, the key ring and item counts.
- Faster loading: the config file is written once instead of once per setting.

## 0.7.0

- New `Crafting Panel Width` and `Crafting Panel Height` (per player): a bigger crafting panel and recipe list.
- Crafting the next backpack or tacklebox upgrades the one in its slot: no free cell needed, contents kept.
- `Food Key` and `Mead Key` do nothing with a hammer, hoe or cultivator in hand.
- Fixed: Use on your grave with a full backpack takes everything instead of opening it.
- Fixed: with `Inventory Rows` 0, a backpack or tacklebox taken from your grave could miss its slot.
- Fixed: a stutter the first time the inventory opens.
- Less work every frame for the settings sync.

## 0.6.2

- Less work when any window or map pin appears (the Timber panel theme).
- Less work every frame for backpacks worn by other players, the hotkeys and the closed YAML editor.

## 0.6.1

- Fixed: the coin purse and key ring no longer take a copy of an item another mod already stored.
- Less work every frame for the key ring and the stat boxes beside the minimap.
- Removed the leftover art-test wallpaper file check.

## 0.6.0

- Works with BiomeLords: its Featherweight blessing's rows join your main grid and leave without touching your slots.

## 0.5.0

- `Inventory Rows` goes down to 0: just two cells for your hands (keys 1 and 2); a backpack's cells continue after them.
- The inventory panel follows its rows, so OpenKeep's buttons sit right under a small grid; the side panels keep their size.
- New `Night Shade`: the inventory panels darken at night and in dark weather, as the game's own do.
- Fixed: no more "LiberationSans SDF Font Asset was not found" warnings in the log when the inventory opens.

## 0.4.0

- Armour, weight and Elite Creatures Reborn's world tier now sit in full-size boxes in a column right of the minimap.
- The minimap and status effect icons move left to make room, and back when no box shows.
- The setting keeps its name, `Weight Under Minimap`.
- Fixed: on the Consumables tab, hovering an item shows its own tooltip and equips or drops that item.

## 0.3.0

- The Food and Mead bar is two squares: a food and a mead icon with their keys over them, nothing else.
- Fixed: the inventory broke with Jewelcrafting, CurrencyPocket, OttoPay, TrashItems or Quick Stack Store Sort Trash
  Restock installed.
- Other mods' stat boxes join the column instead of covering one; TrashItems' trash can takes dragged stacks there.
- OpenKeep 1.8.0 to 1.9.0 and Elite Creatures Reborn 3.12.0 to 3.13.0 carry the same column: update them too.

## 0.2.0

- New `Trinket Slot` under the utilities, for the one trinket the game lets you wear.
- A worn trinket moves in on first start; `Keep Slots On Death` keeps it, and your grave puts it back on.
- New `Food And Mead Bar` (per player) under your health: the two keys and your slots' food and meads.
- Arrows and bolts you craft, pick up, buy or loot go into the Ammo slots first.
- Capes stay under a worn backpack instead of flapping out through it.
- New Mead slot icon: a mead bottle instead of a drinking horn.

## 0.1.0

- First release; needed on the server and every client.
- A bigger grid, up to 12 x 10; existing characters keep their items.
- `Base Carry Weight`, `Keep Slots On Death`, and `Enabled` to bring back the game's inventory.
- Labelled slots on two tabs: armour, backpack, up to five utilities, food, mead and ammo.
- Ammo slots feed the bow; `Food Key` and `Mead Key` eat and drink from the slots.
- A stat sheet, with Epic Loot's magic effects.
- A coin purse and a key ring.
- Four craftable tackleboxes for bait, and eight craftable backpacks worn on your back.
- Stat boxes by the grid and the minimap; a brown or timber look.
- Pickups fill the grid, not the slots; taking all from your grave puts everything back.
- With OpenKeep 1.8.0 or later, its quick stack and sort skip the slots.
- Server synced; both YAML files hot reload.

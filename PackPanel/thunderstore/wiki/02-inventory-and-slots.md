# Inventory and Slots

The inventory shows, left to right, the grid, a column of stat boxes and the slot panel. Slot items are ordinary inventory items: they weigh, save and go to the grave like any other.

## The grid

- 8 x 5 by default, up to 12 x 10. Rows bought from a trader come on top.
- Hotbar keys 1 to 8 stay on the top row.
- `Inventory Rows = 0` leaves only your two hand cells (keys 1 and 2).
- A worn backpack adds its cells at the bottom.
- Carry weight is `Base Carry Weight` (300) plus Megingjord, other effects and a backpack.

## The slots

| Slot | Where | Takes |
| --- | --- | --- |
| Head, Chest, Legs, Back | Gear tab | Helmets, chest armour, leg armour, capes |
| Backpack | Gear tab | PackPanel's backpacks (other mods' backpacks fit but are not worn) |
| Utility (up to 5) | Gear tab | Belts, the wishbone, the wisplight... |
| Trinket | Gear tab | Trinkets |
| Food, Mead, Ammo (up to 5 each) | Consumables tab | Food; meads, potions and other consumables; arrows, bolts and bait |
| Coin purse, key ring, Tacklebox | Under both tabs | Coins; keys; a tacklebox |

- An empty slot shows its icon and name. Anything that does not suit a slot is refused: "That does not go in that slot".
- Items on the hidden tab still count: they weigh and stay worn.
- The Gear tab also shows the [stat sheet](wiki:Stat Sheet and Look).

## Worn gear

- An item in Head, Chest, Legs, Back, Utility or Trinket is worn.
- Wear: drop it on its slot, or right click it in the grid. Take off: drag it to the grid, or right click it.
- Every utility in the Utility slots is worn at once and all their effects count. Your character shows only one, and a second copy of the same utility is not worn.

## Eating and drinking

- `Food Key` (Z) eats every food in the Food slots that can be eaten now, left to right. `Mead Key` (B) drinks every mead in the Mead slots that can be drunk now.
- The game's rules apply: two of the same food eat one, two health meads drink one.
- The keys work like hotbar keys: outside the inventory, map, menus and chat.
- If nothing can be taken: "Nothing in your food slots can be eaten now" (or drunk, for meads).
- A key with a modifier, such as `LeftShift + Z`, needs exactly that modifier. `None` turns a key off.
- The Food and Mead bar, two squares under your health bar, shows both keys.

## Ammo

- The bow uses the Ammo slots first, left to right, then the grid. Right click a stack to use that one until it runs out.
- Arrows and bolts you craft, pick up, buy or loot go to the Ammo slots first.

## Coin purse and key ring

- The purse holds coins. Coins you pick up go into it first, and traders take coins from it.
- The key ring button, right of the purse, shows how many different keys you carry; hover it for the list. Click it to open the ring.
- Keys you pick up go onto the ring. Doors and gates still find them there. Drag a key onto the ring button to put it back on.
- Up to `Key Stack` (10) keys of one kind stack together (the game stacks 1).
- A key you never had before makes the button glow gold, with a note saying which key went onto the ring. Opening the ring clears it.

## Where new items go

- Pickups, crafting and purchases fill the grid, never a slot. "Inventory full" means the grid is full.
- Coins, keys, bait and arrows go to the purse, the ring, the tacklebox and the Ammo slots first, even when the grid is full.
- Items still join a matching stack in a slot (cooked meat joins the same meat in a Food slot).
- When a setting changes the layout, items keep their place if it still exists. What no longer fits drops at your feet: "No room for N items: dropped at your feet".

## Death and graves

- By default everything goes to the grave. Take all from your own grave and everything goes back where it was, armour, backpack and tacklebox included, worn again.
- `Keep Slots On Death` keeps your gear, backpack, utility, trinket, food, mead and ammo slots and your tacklebox when you die. The grid, the purse, the keys and the bait still go to the grave.

## Gamepad

- The gamepad moves over every cell. Moving onto a slot of the other tab switches tabs.
- On the shut key ring, A or X opens it. X on the tacklebox opens it. B shuts either before it closes the inventory.

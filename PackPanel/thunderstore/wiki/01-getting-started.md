# Getting Started

This wiki describes version 0.9.0.

PackPanel replaces the player's inventory screen:

- A bigger grid: 8 x 5 by default, up to 12 x 10, or down to the two cells for your hands.
- Labelled slots always on screen, on two tabs: Gear (armour, cape, backpack, up to five worn utilities, trinket, a stat sheet) and Consumables (food, mead and ammo, with keys to eat, drink everything or drink one mead slot). Under both: a coin purse, a key ring and a tacklebox.
- Eight craftable backpacks worn on your back, and four craftable tackleboxes for bait.
- Armour and weight boxes beside the grid and the minimap, a bigger crafting panel, a Timber or Brown look that darkens at night.
- A base carry weight setting and an option to keep your slot items when you die.

## Install

- Needs BepInExPack for Valheim (r2modman and the Thunderstore app install it). By hand: put `PackPanel.dll` in `BepInEx/plugins`.
- Multiplayer: install it on the server and every client, all on the same version. A player without it, or with another version, is refused with a message naming the mod; a player with it cannot join a server without it.
- On a server, the server's settings apply to everyone ([Configuration](wiki:Configuration)).
- An existing character keeps every item where it was; the armour you wear moves into its slots.

## Quick start

1. Open the inventory: the grid, a column of stat boxes, then the slot panel.
2. Drop up to three utilities (belts, the wishbone, the wisplight...) on the Utility slots. All are worn.
3. On the Consumables tab fill the Food, Mead and Ammo slots. Outside the inventory, Z eats from them and Left Alt + 1 to 5 drinks the mead in that one slot.
4. Craft a Deerhide Satchel (workbench level 2) and drop it on the Backpack slot: 4 more cells.
5. Craft a Driftwood Tacklebox (workbench level 2), drop it on the Tacklebox slot and right click it to open it.

## Pages

- [Inventory and Slots](wiki:Inventory and Slots): the grid, every slot, eating and drinking, ammo, purse, key ring, death.
- [Backpacks and Tackleboxes](wiki:Backpacks and Tackleboxes): recipes, sizes, upgrades, use, magic backpacks with EliteCrafting.
- [Stat Sheet and Look](wiki:Stat Sheet and Look): the stat sheet, stat boxes, the crafting panel, themes.
- [Configuration](wiki:Configuration): every setting, the YAML files, the console command.
- [Mod Compatibility](wiki:Mod Compatibility): OpenKeep, EliteCrafting, FeastMaster, Elite Creatures Reborn, Epic Loot, BiomeLords and others.

## Links

- Source: https://github.com/geraldjglasgow/ValheimMods
- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and its version)
- Licence: GPL-3.0

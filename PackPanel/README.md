# PackPanel

A bigger, better organised inventory, with labelled slots always on screen.

## Features
- A grid from your two hands up to 12 x 10, with `Base Carry Weight` and `Keep Slots On Death`.
- Labelled slots on two tabs, Gear and Consumables, plus a purse, key ring and tacklebox (below).
- Stat sheet: your stats as the game counts them; hover a line for its breakdown.
- Auto Equip: gear in its slot is always worn; right click gear in a chest to wear it.
- Eight backpacks and four tackleboxes; each tacklebox is crafted from the one before (below).
- Armour and weight by the minimap, a bigger crafting panel, a brown or timber look, darker at night.
- With OpenKeep 1.8.0+, its buttons join the inventory; quick stack and sort skip the slots.
- With EliteCrafting or Epic Loot, backpacks can be magic; the stat sheet lists their effects.

## Slots
- Head, Chest, Legs, Back: your armour and cape, worn.
- Backpack: wear one of the eight backpacks below.
- Utility: up to five belts, wishbones and the like, all worn at once.
- Trinket: your worn adrenaline trinket.
- Food: `Z` eats everything in them you can eat now.
- Mead: `Left Alt` + 1 to 5 drinks that slot's mead, shown under your health bar.
- Ammo: arrows and bolts go here first; the bow uses them left to right.
- Coin purse: coins go in first, and traders pay from it.
- Key ring: keys gather on a small round ring; doors still find them.
- Tacklebox: holds a tacklebox, whose cells keep your bait.

## Backpacks
Worn on your back for every player to see. Their cells join the bottom of your grid.
- Deerhide Satchel (Meadows): +4 slots.
- Trollhide Backpack (Black Forest): +4 slots, +50 carry weight.
- Rootbound Pack (Swamp): +8 slots, +50 carry weight.
- Wolfpelt Pack (Mountains): +8 slots, +100 carry weight.
- Lox Hauler (Plains): +12 slots, +100 carry weight.
- Carapace Pack (Mistlands): +12 slots, +150 carry weight.
- Asksvin Pack (Ashlands): +16 slots, +150 carry weight.
- Moosehide Pack (Deep North): +16 slots, +200 carry weight.

## Tackleboxes
- Driftwood Tacklebox (Meadows): 1 bait cell.
- Finewood Tacklebox (Black Forest): 2 bait cells.
- Carapace Tacklebox (Mistlands): 6 bait cells.
- Flametal Tacklebox (Ashlands): 8 bait cells.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `PackPanel.dll` in
`BepInEx/plugins`.
- Not compatible with mods that resize the inventory or add equipment slots, like ExtraSlots.
- Removing the mod loses what lies outside the game's grid, every backpack and tacklebox, and keys above
  one per stack: empty them first.
- Lowering `Inventory Width`, `Key Stack` or slot counts, or taking off a pack, drops what no longer fits.

## Configuration
`BepInEx/config/milkyteam.packpanel.cfg`, plus `PackPanel.Backpacks.yml` and `PackPanel.Tackleboxes.yml` (format:
https://github.com/geraldjglasgow/ValheimMods/blob/main/PackPanel/CLAUDE.md#backpack-and-tacklebox-yaml-files). Every
setting is described in the file and applies without a restart; the server's values bind every player, except your
look and keys.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

# PackPanel

A bigger, better organised inventory, with labelled slots always on screen.

## Features
- A grid from your two hands up to 12 x 10, with `Base Carry Weight` and `Keep Slots On Death`.
- Gear tab: armour, backpack, utilities, trinket and a stat sheet.
- Consumables tab: food, mead and ammo, with eat and drink keys.
- A coin purse, a key ring and four tackleboxes for bait.
- Eight backpacks for slots and carry weight, worn as equipment Epic Loot can enchant.
- Armour and weight by the minimap, a bigger crafting panel, a brown or timber look, darker at night.
- With OpenKeep 1.8.0+, its buttons join the inventory and its quick stack and sort skip the slots.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `PackPanel.dll` in
`BepInEx/plugins`.
- Not compatible with mods that resize the inventory or add equipment slots, like ExtraSlots.
- Removing the mod loses what lies outside the game's grid, every backpack and tacklebox, and keys above
  one per stack: empty them first.
- Lowering `Inventory Width` or `Key Stack` can lose items; fewer slots or taking off a pack drops what no longer fits.

## Configuration
`BepInEx/config/milkyteam.packpanel.cfg`, plus `PackPanel.Backpacks.yml` and `PackPanel.Tackleboxes.yml` (format:
https://github.com/geraldjglasgow/ValheimMods/blob/main/PackPanel/CLAUDE.md#backpack-and-tacklebox-yaml-files). Every
setting is described in the file and applies without a restart; the server's values bind every player, except your
look and keys.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

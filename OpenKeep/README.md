# OpenKeep

Storage for Valheim, plus a few base tweaks.

## Features
- Reach: craft, build and feed stations from nearby chests, ships and carts.
- Stow: quick stack, store all, top up, sort, route and trash, by button or hotkey.
- Crafting: a Salvage tab returns materials; craft many at once.
- Stacks and Capacity: stack sizes, weights, chest and station sizes, contents on hover.
- Off by default: cart workbenches, contents signs on chests, two players in one chest.
- Homestead: pick your bed after death, faster respawns and portals, campfires on wood, honey per day, fires,
  smelters and pets fed from chests, night-only torches, quicker Rested, area and auto repair.
- Works with PackPanel, the inventory mod; neither needs the other.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `OpenKeep.dll` in
`BepInEx/plugins`.
- Lowering a stack size, or removing the mod, loses items above the new limit.
- Items in an enlarged chest's extra rows are hidden without the mod.
- Before removing the mod, switch off Signs and `Torches Night Only`, then visit your bases by day.
- In the Ashlands (or with the Fire world key) a campfire sets its wooden floor alight.

## Configuration
`BepInEx/config/milkyteam.openkeep.cfg` and seven `OpenKeep.*.yml` files. Every setting is described in the file and
applies without a restart; the server's values bind every player. Console: `openkeep help`.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

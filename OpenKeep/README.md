# OpenKeep

Storage for Valheim, plus a few base tweaks.

## Features
- Reach: craft, build and feed stations from nearby chests and the ship or cart you are using.
- Stow: quick stack, store all, top up, sort, route and trash, by button or hotkey.
- Crafting: Salvage tab, batches, craft speed, recipe search, favourites, grid views, recipe tracker.
- Stacks and Capacity: stack sizes, weights, chest and station sizes, contents on hover.
- Build Camera: build from a free camera near a crafting station (B with the hammer).
- Off by default: cart workbenches, chest contents signs, shared chests, Auto Tidy (stray items go to the chest
  they belong in) and Blueprints (save buildings, place them as construction sites from the hammer).
- Homestead: choose your bed after death, quick respawns, campfires on wood, honey rate, fires, smelters and pets
  fed from chests, night-only torches, quicker Rested and auto repair.
- Works with PackPanel (inventory) and Wayfare (portals, quick jumps); neither is required.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `OpenKeep.dll` in
`BepInEx/plugins`.
- Lowering a stack size loses items above the new limit.
- Before removing the mod: split oversized stacks, empty enlarged chests' extra rows, switch off Signs and
  `Torches Night Only`, and visit your bases by day.
- Ashlands or the Fire world key: a campfire on wood sets the floor alight.

## Configuration
`BepInEx/config/milkyteam.openkeep.cfg` and seven `OpenKeep.*.yml` files. Every setting is described in the file and
applies without a restart; the server's values bind every player. Console: `openkeep help`.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

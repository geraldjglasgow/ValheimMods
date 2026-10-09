# OpenKeep

Storage for Valheim, plus a few base tweaks.

## Features
- Reach: craft, build and feed stations from nearby chests and the ship or cart you are using.
- Storing and sorting: quick stack, store all, sort and trash by button or hotkey (below).
- Crafting: salvage, batches, recipe search and a recipe tracker (below).
- Stacks and Capacity: stack sizes, weights, chest and station sizes, contents on hover.
- Build Camera: build from a free camera near a crafting station (B with the hammer).
- Homestead: beds, respawns, fires, honey, pets and repairs (below).
- Works with PackPanel (inventory) and Wayfare (portals, quick jumps); neither is required.

## Storing and sorting
- Quick stack: every stack to the nearby chests that already hold it.
- Store all / Take all: everything into or out of the open chest.
- Top up: refills your partial stacks from nearby chests.
- Route and Store one: a stack, or one item, to the nearest chest holding it.
- Find: points to every nearby chest holding the hovered item.
- Sort: your inventory or a chest, by category, name, weight, value or amount.
- Trash can and junk: destroy a stack, or all your junk at once; favourites are safe.
- Chest cycling: open the next chest around you with the arrows or the wheel.

## Crafting
- Station feeding: use a smelter, kiln, fire or oven empty-handed and it takes from chests.
- Salvage: a Salvage tab turns gear back into most of its materials.
- Batch crafting: make many at once with a - amount + stepper, at your chosen speed.
- Recipe search, categories and favourites: by name or material, category buttons (Weapons, Armour, Food...), favourites first, as rows or icon tiles.
- Recipe tracker: right click a recipe to pin its materials on screen.

## Homestead
- Bed choice: every bed you own counts; after a death, click one on the map.
- Quick respawn: the nearer you died to your bed, the sooner you wake.
- Campfires on wood: build them on wooden floors.
- Honey: set how much honey a hive makes per day.
- Self-feeding: fires, torches, smelters, kilns and windmills refill from nearby chests.
- Night-only torches: lit at nightfall, out at daybreak.
- Pets: hungry tame animals walk to a nearby chest and eat.
- Rested sooner: Rested after a few seconds of rest, not twenty.
- Repairs: the hammer also fixes touching pieces; opening a station repairs your gear.

## Off by default
- Cart workbench: every cart carries a workbench to craft and build from.
- Contents signs: a sign above each chest names what it holds.
- Ground pickup: chests pick up drops lying around them.
- Shared chests: view or use a chest another player has open.
- Auto Tidy: stray items go to the chest they belong in.
- Blueprints: save buildings, place them as construction sites from the hammer.
- Mímir's Chest: a chest that never fills, with search, filters and sorting.

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

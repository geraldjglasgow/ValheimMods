# Multiplayer and Compatibility

EarthWright is built for dedicated servers first; single player and hosted worlds work the same way.

## Installing on a server

- Install EarthWright on the server and on every client, in the same version (0.3.1). When the versions differ, or one side lacks it, the connection is refused and the reason is shown on the connection screen.
- Admins are the players on the server's admin list and the host. In single player you are an admin.
- Configure the server's `milkyteam.earthwright.cfg` and YAML files; players need not change anything.

## Whose settings apply

- Every gameplay setting is synced: while the server's `Lock Configuration` is on (default), the server's values bind every player and the server's YAML files replace theirs. Admins can still change synced settings from their own game (for example with a configuration manager or the F6 panel's protection section); the change goes to the server and from there to everyone.
- Keys, display options and personal preferences are never synced; each player keeps their own. The [Configuration Reference](wiki:Configuration Reference) marks every setting as Server or Player.
- With `Lock Configuration` off, every player uses their own settings and YAML files. Admin zones always come from the server.
- Changes to the server's .cfg or YAML files apply without a restart and reach connected players at once.

## How an edit travels

1. Your click is checked on your machine first: protection, costs, tool level, cooldown. A refused click costs nothing and shows its reason.
2. The edit goes to the machine that owns each piece of ground it touches (in Valheim that is usually the player who was there first, or the server), which checks protection and the height limits again and applies it.
3. The new ground is saved in the world and every player sees it. A refusal at that stage comes back to you as a message.
4. Edits that need an admin's rights (the Terraform entry, the limit override key, admins passing the lock or zones, `ew terrain`) travel through the server first, which checks the admin list.

So a player cannot get past the server's limits or protection with their own settings, and everyone sees the same ground.

## What stays with each player

- Your brush values, target height, ramp and road points, undo history and snapshots live on your machine and are gone when you log out.
- Costs are paid from your own stamina, tool and inventory. Free build is your own switch, if the server allows it.
- Uproot and clearing change world objects through the game's own objects, so drops and removals are seen by everyone.
- Your tool's light and the torch in your left hand are seen by every player; the light's colour is each viewer's choice.

## Removing EarthWright

- The terrain you changed stays in the world: it is stored the way the game stores all terrain edits.
- Ground raised or dug more than 8 m is drawn clamped to the game's own limits without EarthWright, so keep it installed (on every machine) once you have gone past 8 m.
- EarthWright's menu entries and custom entries disappear from the menus.

## Other mods

- Terrain pieces other mods add to the hoe's or cultivator's menu get the brush as well: their level and smooth pieces act as levelling entries, raise pieces as raise or lower, and their paint is kept. `Resize Modded Terrain Pieces`, `Modded Entries Need Stations` and the `modded` tool in the brush YAML cover them. EarthWright keeps its own entries in one block; pieces other mods add stay where they are in the menu.
- Only the game's Hoe and Cultivator items are terrain tools for EarthWright. Terrain work with other tools is left to the game, but it follows EarthWright's height limits, and the terrain lock refuses it.
- Pickaxe digging follows the dig limit and the terrain lock.
- EarthWright replaces the game's own terrain height limits and how edited terrain is drawn. Another mod that changes the same height limits does the same job; use one of them.
- EarthWright's console command is `ew`, so it does not clash with other terraforming mods' commands.

## Upgrading from 0.1.x

Version 0.2.0 removed the shovel of 0.1.x with its recipe, levels and Dig entry; shovels made with 0.1.x disappear from inventories. Custom entries take `tool: hoe` or `tool: cultivator`.

## Reporting problems

Set `Debug Log` (section 0) to true to write every terrain edit sent and applied to the BepInEx log. Report bugs and ideas at https://github.com/geraldjglasgow/ValheimMods/issues with the mod's name and version (the log line `Loading [EarthWright 0.3.1]` confirms it).

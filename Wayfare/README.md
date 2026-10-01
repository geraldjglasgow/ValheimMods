# Wayfare

Pick a portal's destination on the world map instead of matching tags. Any portal can reach any other portal you are
allowed to use.

## Features
- Walk into a portal and the map opens with every portal you may target; click one to go there. The game's carry
  rules, fade and cooldown still apply.
- A portal's tag is only a label next to its icon; it no longer decides where the portal leads.
- Access modes, cycled with Alt+Use on a portal: Public, Private (owner only) or Admin. Cycling makes you the owner;
  only the owner or an admin can change an owned portal's mode.
- Favourites: right-click a portal icon to add it to a panel on the map; saved with your character.
- `Toggle Icons Key` (P) shows portal icons on the ordinary map.
- Works with any mod's portals. A new or renamed portal can take a few seconds to appear.

## Install
Needed on the server and every client; a client-only install does not work. Install with r2modman or the Thunderstore
app, or put `Wayfare.dll` in `BepInEx/plugins`.

## Configuration
`BepInEx/config/com.Wayfare.cfg`. Every setting is described in the file and applies without a restart; the server's
values bind every player (`Lock Configuration`), except the map key, icon size and tag labels, which each player sets.
Console: `charter status` shows whether the server binds your settings.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

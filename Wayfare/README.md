# Wayfare

Walk into a portal and the world map opens on every portal you may use; click one and you travel there. No more
matching tags: every portal reaches every other, and a tag is only the name shown beside its icon. The server can
switch portals to a dropdown of named portals, or back to the game's own tag pairing.

## Features
- Map targeting: walk in, click a portal on the map, go. The game's carry rules, fade and cooldown still apply.
- `Teleport Mode`: Map, TargetTeleport (pick from a dropdown of every named portal) or Default (the game's portals).
- Access modes: Public, Private or Admin per portal, cycled with Shift+E (below).
- Favourites: right-click a portal icon to pin it to a panel on the map.
- `Toggle Icons Key` (P) shows portal and sea gate icons on the ordinary map.
- Works with any mod's portals, found by themselves.
- Sea gates: portals for ships, crew aboard (below). Experimental.
- Quick jumps: near portals are quicker, and no loading screen where nothing needs loading (below).

## Access modes
Changing a portal's mode makes you its owner. Portals you may not use never show on your map.
- Public: everyone may travel to it; every new portal starts Public.
- Private: only its owner (and server admins) may travel to it.
- Admin: only server admins may travel to it, and only an admin may set it.

## Sea gates
Two pillars with water between them open a portal for ships. Every sea gate reaches every other.
- Sea Gate Pillar: built from the hammer's Misc tab, on land or in shallow water.
- Pairing: two pillars 10 to 15 m apart across deep enough water open by themselves.
- Placement preview: green and red lines and posts show where a gate would open, or why not.
- Sailing through: the ship stops in the gate and the helmsman picks the destination on the map.
- Crew pointers: everyone aboard sees the map and each other's named pointers.
- Arrival: the ship comes out at the other gate with its crew on deck, its cargo and its speed.
- Safety: a few seconds without damage after arriving; a late ship sets its crew ashore by the gate.
- Name and access: E names a gate, Shift+E sets its access mode, as on a portal.
- Restricted cargo: ore and metal block the jump, as with portals.

## Quick jumps
- Near portals: the closer the two portals, the quicker the jump; the farthest take the game's 8 s.
- No loading screen: a jump into an area already loaded keeps the screen clear.
- Faster land loading: after a long jump the land around you loads as fast as your PC allows.
- Solid landings: on a server you land on your floors, not under them.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `Wayfare.dll` in
`BepInEx/plugins`.

## Configuration
`BepInEx/config/com.Wayfare.cfg`. Every setting is described in the file and applies without a restart; the server's
values bind every player, display settings stay your own. Console: `charter status` shows whether the server binds
your settings.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

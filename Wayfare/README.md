# Wayfare

Map-based portal targeting for Valheim: walk into any portal, pick your destination from the world map instead of
matching tag text. Any portal can connect to any other portal you're allowed to see.

**Required on the server as well as every client** - the server holds the authoritative access rules and grants or
refuses every teleport; a client-only install will not work.

### Features
- Entering a portal opens the world map in a targeting mode with an icon on every portal you may target. Click one
  to teleport there through the game's own teleport path: carry-item restriction, the fade effect, the 2-second
  cooldown and the world-blocking keys (`NoPortals`, `NoBossPortals`) all apply exactly as they do for a vanilla
  portal. Leave the map or press its own close/cancel binding to back out without teleporting.
- A portal's tag (set the normal way, `E`) is drawn next to its icon as a label - it no longer decides where the
  portal connects.
- Three access modes, cycled with **Alt+Use** on a portal (plain `E` still opens the tag box):
  - **Public** - any player may target it.
  - **Private** - only the owner may target it.
  - **Admin** - only server admins may target it.
  Cycling the mode makes the acting player the owner, every time, including cycling back to a mode it already had.
  A portal that already has an owner can only have its mode changed by that owner or an admin.
- The portal list refreshes roughly every 5 seconds - the same cadence the game's own portal reconnection runs at -
  so a portal built or renamed moments ago may take a few seconds to appear or update.
- Works with any mod's portal prefab: portals are found by their `TeleportWorld` component, never by a hardcoded
  prefab name list.
- Configurable hotkey (default `P`) toggles portal icons on the ordinary (non-targeting) large map - a display
  preference, set per player.
- Right-click a portal's icon to add or remove it from your favourites, shown as a panel on the map's left side;
  click a favourite to target it directly. Favourites are per player, saved with your character, and keyed to the
  portal itself, so renaming a favourited portal does not un-favourite it.

### Access enforcement
Portal mode and owner live in the portal's own ZDO under Wayfare-prefixed keys, so every client sees the same
values the server does. Every teleport request is validated by the server before it is granted - hiding an option
in one client's UI is never the only thing standing between a player and a portal they should not be able to use.
A mode change is applied only by whichever machine currently owns that specific portal's ZDO, the same authority
the game's own portal tag already uses.

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod - **on the server too**, not just connecting clients.

For manual install, drag `Wayfare.dll` into the `BepInEx/plugins` folder.

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. The hotkey, icon scale and the
show-tags toggle are always yours to set, since they only affect what you see.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` repository checked out next to
this one.

```
dotnet build Wayfare/Wayfare.csproj -c Release
```

### Bugs and feature requests
The source lives on [GitHub](https://github.com/geraldjglasgow/ValheimMods). Found a bug or want a feature? Open an
issue at https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you
distribute that is built from it must carry the same freedoms and be released under the same licence, with source.
See the `LICENSE` file for the full terms.

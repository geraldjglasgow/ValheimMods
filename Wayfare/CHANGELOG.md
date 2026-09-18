# Changelog

## 0.1.0

- Initial release. Walking into a portal opens the world map in a targeting mode showing every portal you may
  target; click one to teleport there through the game's own teleport path (carry rules, fade effect, cooldown).
- Portal tags are a label only, shown next to the icon; they no longer decide the destination.
- Three access modes, cycled with Alt+Use on a portal: Public, Private, Admin. Cycling makes the acting player the
  owner. Enforced by the server for targeting, by the portal's own ZDO owner for mode changes.
- Configurable hotkey (default `P`) toggles portal icons on the ordinary (non-targeting) large map.
- Right-click a portal icon to favourite it; favourites show in a left-side panel on the map and persist with the
  character across sessions.
- Works with any mod's portal prefab: discovered by its `TeleportWorld` component, not a hardcoded name list.
- Required on the server as well as every client.

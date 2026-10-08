# Changelog

## 0.4.0

- `Teleport Mode` (General): `Map` (as before), `TargetTeleport` or `Default` (the game's tag pairing).
- TargetTeleport: walking into a named portal opens a dropdown of every other named portal; pick one, press Teleport.
- TargetTeleport: unnamed portals stay dark and unconnected; favourites list first, each portal with its distance.
- Default: access modes and portal map icons are off; quick jumps and sea gates still work.

## 0.3.3

- Fixed: after Alt + Tab, `Toggle Icons Key` could stop working or act as if Shift, Ctrl or Alt were held.
- Store page: a fuller overview of what the mod does.

## 0.3.2

- A portal built moments ago shows on the map as soon as you walk into any portal.
- Fixed: `Toggle Icons Key` fired while typing a map pin name or in the console.
- Fixed: a `Toggle Icons Key` with Shift, Ctrl or Alt did nothing while walking.
- Lighter on frame rate and the server with the map open, near sea gates and with many portals.
- Update the server and every client together.

## 0.3.1

- Fixed: portal and sea gate icons wiggled while panning the map.

## 0.3.0

- Every portal reaches every other; a tag is only its name. Untagged portals are open again.
- Sea gates: every gate reaches every other. Sail in and the helmsman picks the destination on the map.
- Sea gates: the whole crew sees the map with each other's pointers; E on a pillar only names the gate.
- The map opens framed on every portal you may use.
- Access: only admins may set Admin; a Private portal answers only to its owner.
- Removed: `Unowned Portals Are Public`; a portal nobody has set is Public.
- Fixed: walking into a portal with the inventory open left both stuck open.

## 0.2.0

- Portals need a tag: untagged ones stay dark, closed and off the map; tagged ones glow as connected.
- Map icons are the game's portal icon in gold, ringed for favourites, easier to click.
- "You are here" marks the portal you stand in; it can no longer be picked.
- Double-click a portal icon to place a map pin; right-click removes a pin under it first.
- The favourites list hides portals you may no longer use.
- Fixed: favourites entries, and portal icons on the ordinary map, ignored left clicks.
- Faster loading: the config file is written once instead of once per setting.

## 0.1.0

- First release.
- Walk into a portal and pick its destination on the world map; portal tags are only labels.
- Access modes Public, Private and Admin, cycled with Shift+E.
- Favourites panel on the map; `Toggle Icons Key` (P) shows portals on the ordinary map.
- Works with any mod's portals.
- Sea gates: two pillars across water open a portal for ships, crew aboard. Experimental.
- Quick jumps, formerly in OpenKeep: near portals are quicker, no loading screen into a loaded area.
- Needed on the server and every client.

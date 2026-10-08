# Portals and Access

## Choosing a destination

- Walk into any portal: the large map opens with an icon on every portal you may travel to. The icons are larger and
  pulse while you choose. Click one to go there.
- Portals no longer pair: every portal reaches every other and glows as connected, tagged or not. A tag is only the
  name under the portal's icon; E on a portal opens the tag box.
- Double-click a portal icon to place a map pin there; a right click removes a pin under it first.
- Closing the map (Escape, the map key, the inventory) cancels. Walk out of the portal and back in to choose again.
- The game's carry rules apply: with ore, metal or other items portals refuse, you cannot travel, unless the portal
  you walked into takes everything (as the game's stone portal does) or a world modifier lets everything through.
  The fade and the 2 s wait between jumps are the game's.
- While a world modifier blocks portals (always, or during boss fights), nobody travels: "Portal travel is blocked
  here".
- A new or renamed portal appears on the map within a few seconds.

## Teleport modes

`Teleport Mode` (General) sets how a portal picks its destination, the same for every player on a server.

| Mode | Walking into a portal |
| --- | --- |
| Map (default) | the map opens, as above |
| TargetTeleport | a window opens with a dropdown of every other named portal |
| Default | the game's own portals: two portals with the same tag connect |

- TargetTeleport: pick a portal in the dropdown and press Teleport (X on a gamepad). Cancel, Escape or
  walking away from the portal closes the window.
- Only named portals are connected. An unnamed one stays dark: "Name this portal to connect it".
- The list holds the portals you may travel to (access modes below), your favourites first, then by name, each with
  its distance from you.
- Default: access modes, favourites and portal map icons are off. Quick jumps and sea gates work in every mode.

## Access modes

Every portal has a mode; its hover text shows which.

| Mode | Who may travel to it |
| --- | --- |
| Public | everyone |
| Private | its owner |
| Admin | server admins |

- **Shift+E** on a portal (the game's alternate use: hold the AltPlace key, Left Shift by default, and press Use)
  moves it to the next mode: Public, Private, then Public again. Only an admin may choose Admin: for them Shift+E
  goes Public, Private, Admin, then Public again.
- Changing the mode makes you the portal's owner. Anyone may change a portal nobody owns; an owned one only its owner
  or an admin ("You don't own this portal"). A Private portal only its owner, the player who made it private, not
  even an admin. Under a ward you need access to the ward.
- Every new portal is Public, and so is any portal nobody has set a mode on.
- The mode decides where you may travel to, not which portal you may enter: any portal you walk into lists every
  portal you may use.
- Portals you may not use never show on your map. A refusal says why: "Only the portal's owner may target it" or
  "Only a server admin may target this portal".
- Admins are the players in the server's `adminlist.txt` and the host of a hosted or single player world. They see,
  and may travel to, every portal.

## Favourites

- Right-click a portal icon on the map to add it to your favourites, or to remove it ("Added to favourites",
  "Removed from favourites"). A favourite shows a ring around its icon.
- While you choose a destination, your favourites are listed on the left of the map; click one to travel there. A
  long list scrolls with the mouse wheel. A portal you may no longer use leaves the list.
- Favourites are your own, kept with your character, and stay through renames and server restarts. Only portals you
  may still use are listed.

## Map icons

- `Toggle Icons Key` (P): with the large map open, shows or hides portal and sea gate icons on the ordinary map, so
  you can see your network without standing in a portal. It does not fire while Shift, Ctrl or Alt is held or while
  you type (chat, console, a map pin name).
- Icons draw above the game's pins. `Icon Scale` sets their size and `Show Tags` the names under portal icons; both
  are your own settings ([Quick Jumps and Settings](wiki:Quick Jumps and Settings)).

## Other mods' portals

Portals from other mods are found by themselves: any portal you walk into to travel, like the game's own, gets map
targeting, access modes and favourites, with no list to keep. The mod that adds it must be installed on the server
too.

Sea gates are separate: they never show while you choose a portal destination, and portals never show in the sea gate
picker ([Sea Gates](wiki:Sea Gates)).

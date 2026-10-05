# Quick Jumps and Settings

## Quick jumps

The game takes 8 seconds for every portal jump. With `Quick Portals` on, the closer the two portals, the quicker:

- Portals side by side take `Quick Portal Seconds` (0.5 s). The time grows with the distance on the map, up to the
  game's 8 s at `Quick Portal Range` (10000 m). With the defaults a 1 km jump takes 1.25 s, 5 km 4.25 s.
- The game still waits for a far area to load. On a server, the landing also waits until the buildings at the target
  have arrived, so you land on your floors, not under them; that wait never makes a jump longer than the game's.
- Every long jump counts: any portal, the console's `goto`, other mods' teleports. Dungeon doors keep the game's
  timing, and sea gates their own.
- `Screen Only When Loading` (your own setting): a jump to a place already loaded around you (about 100 to 150 m)
  keeps the screen clear, no black screen and no teleport swirl. A farther jump shows the game's teleport screen,
  which goes dark faster.
- `Quick Area Loading` (your own setting): after a long jump the land around you loads as fast as your PC allows,
  instead of the game's one 64 m square every 0.1 s. It saves several seconds with a high simulation distance.

OpenKeep had quick portals until 2.0.0: with Wayfare, use OpenKeep 2.0.0 or newer. OpenKeep keeps its quick respawn.

## The config file

`BepInEx/config/com.Wayfare.cfg`, written on first start; every setting is described in it. Changes apply without a
restart. Settings marked (Player) are always your own; the others come from the server while `Lock Configuration` is
on.

### General

| Key | Default | Meaning |
| --- | --- | --- |
| `Lock Configuration` | on | Read on the server only. On: every player uses the server's values. See Multiplayer below. |
| `Enabled` | on | Master switch. Off: portals pair by tag as in the game; no map targeting, access modes, sea gate jumps or quick jumps. |

### Access

| Key | Default | Meaning |
| --- | --- | --- |
| `Unowned Portals Are Public` | on | A portal or sea gate nobody has set a mode on is Public. Off: only admins may travel to it. |

### Map

| Key | Default | Meaning |
| --- | --- | --- |
| `Toggle Icons Key` (Player) | P | With the large map open: show or hide portal and sea gate icons on the ordinary map. |
| `Icon Scale` (Player) | 1 | Size of the map icons, relative to the default (0.25 at the least). |
| `Show Tags` (Player) | on | Show each portal's tag under its icon. |

### Sea Gates

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | on | Off: no ship jumps, and the pillar leaves the build menu; pillars already built stay. |
| `Pillar Recipe` | `Stone:10,FineWood:5,GreydwarfEye:5,SurtlingCore:1` | What a pillar costs: item prefab names and amounts, comma separated. Unknown items are skipped; with none usable, the default applies. |
| `Allow Restricted Cargo` | off | On: ships may carry ore, metal and other items portals refuse. |
| `Min Gate Width` | 10 | Closest two pillars may stand to pair, in metres. |
| `Max Gate Width` | 15 | Farthest two pillars may stand to pair, in metres. The placement preview looks for a pillar within twice this. |
| `Min Water Depth` | 1 | Metres of water a gate needs between its pillars and on a side ships come out. |
| `Protection Seconds` | 5 | After a jump, the crew and the ship take no damage for this long. |
| `Crew Wait Seconds` | 20 | How long the crew waits for the ship before being set ashore beside the destination gate (5 at the least). |

### Jump Speed

| Key | Default | Meaning |
| --- | --- | --- |
| `Quick Portals` | on | Nearer portals, quicker jumps. Off: the game's 8 s. |
| `Quick Portal Range` | 10000 | Metres on the map at which a jump takes the full 8 s (10 to 20000). Lower it (4000 to 5000) for a bigger difference between near and far portals. |
| `Quick Portal Seconds` | 0.5 | Seconds a jump takes between portals side by side (0 to 8). |
| `Screen Only When Loading` (Player) | on | No teleport screen for a jump into an area already loaded. Off: the screen on every jump. |
| `Quick Area Loading` (Player) | on | Load the land around you faster after a long jump. Off: the game's pace. |

## Multiplayer

- The server and every player need the same Wayfare version; otherwise the join is refused and the connection screen
  says which version to get. A player without Wayfare on a server that has it is disconnected about 20 seconds after
  joining.
- `Lock Configuration` on (the default): the server's values apply to every player while connected; their own return
  when they leave. Edits to the server's file reach players without rejoining.
- Admins (in the server's `adminlist.txt`) can change the server's values from their own game, for everyone.
  Configuration managers show them read-only to other players.
- Off: every player uses their own file. Leave it on unless every player has the same file.

## Console

Wayfare adds no command of its own. In the game's console (F5), no cheats needed, `charter` covers every MilkyTeam mod
that syncs its settings:

| Command | What it shows |
| --- | --- |
| `charter` or `charter status` | Whether the server's values apply to you, per mod |
| `charter diff Wayfare` | Every setting where your file differs from the server's |
| `charter versions` | Your mods and versions, and the server's; use it when you cannot join |

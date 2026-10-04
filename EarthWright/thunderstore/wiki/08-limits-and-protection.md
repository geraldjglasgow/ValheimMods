# Limits and Protection

How far the ground may be raised or dug, and who may change which ground. Everything here is a server rule (sections 6 and 11), except the admin override key. "Admins" means players on the server's admin list and the host; in single player you are one.

## Height limits

| Setting | Default | What it does |
| --- | --- | --- |
| `Raise Limit` | 8 m | How far the ground may rise above the world's original height (the unmodded game allows 8 m). Repeated raises add up to it. |
| `Dig Limit` | 8 m | How deep the ground may be dug below the world's original height. Also where the pickaxe stops digging and dropping stone. |
| `Admin Limit` | 256 m | The most an admin edit that ignores the limits may raise or dig. |

- Both limits range from 0.5 to 512 m, so they can be stricter or much looser than the game's 8 m.
- `EarthWright.Limits.yml` can set other limits per biome (Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth, Ocean). The biome is read for every point from the world's biome map, the same one that colours the ground. See [YAML Files](wiki:YAML Files).
- Limits apply to new edits. Ground that is already higher or deeper stays as it is and is never pushed back.
- The machine that owns the ground enforces the limits, so a player cannot get past them with their own settings. The preview marks points held back by a limit in red.
- The game's own terrain work follows the same limits: pickaxe digging, and terrain pieces EarthWright does not handle itself.
- Ground is drawn up to the largest limit in force. This is why every client needs EarthWright: without it, ground past 8 m is drawn clamped to the game's limit.
- `ew limits` prints the limits where you stand, whether the dig limit is lifted there, and how far the ground is from its original height.

## Admins past the limits

- Hold the admin limit override key (Right Alt, your own key) while changing terrain to ignore the height limits, up to `Admin Limit`. The edit travels through the server, which checks that you are an admin. With `Limit Override Needs God Mode` on it works only in god mode.
- The admin-only Terraform entry levels past the limits while `Terraform Ignores Height Limits` (section 12) is on.
- The admin command `ew terrain ... unlimited` ignores them too (see [Console Commands](wiki:Console Commands)).

## Gentle slopes

With `Gentle Slopes` on (off by default), digging also lowers the ground around the hole and raising also lifts the ground around the mound, so no slope next to an edit is steeper than `Gentle Slope Angle` (40 degrees), within `Gentle Slope Radius` (6 m). Natural cliffs nearby are left as they are, and so is ground under building pieces when the edit skips buildings. Smoothing, resets and painting do not trigger it.

## Strict dig exceptions

With `Strict Dig Exceptions` on (off by default), the dig limit does not apply:

- within `Dig Exception Radius` (8 m) of the objects in `Dig Exception Objects`: by default the game's copper, tin and iron deposits, mud piles, silver veins, obsidian, meteorites, gold veins and buried treasure;
- where tar covers the ground, while `Dig Exception In Tar` is on.

So a server can keep a strict dig limit and still let players dig out ore. A mined-out deposit ends its exception.

## Protection

Every terrain edit is checked on the player's machine before it is sent, and again by the machine that owns the ground before it is applied. A refused edit shows its reason in the middle of the screen, the preview turns red while a click would be refused, and nothing is charged.

| Setting | Default | What it does |
| --- | --- | --- |
| `Respect Wards` | on | An edit is refused when any part of the brush reaches into an active ward you are not permitted on, not only the aimed point. The wards flash as when the game refuses a placement. Admins are held by wards too. |
| `Respect No-Build Zones` | on | An edit is refused when the brush touches a place where the game forbids building, such as boss altars and traders. |
| `Refuse In Dungeons` | on | An edit is refused while you are inside a dungeon or another interior, so the ground above cannot be changed from below. |
| `Terrain Tools Allowed` | Everyone | Everyone, AdminsOnly, or Nobody (admins included). Covers the hoe, the cultivator and EarthWright's tools and commands; the pickaxe follows only the lock. |
| `Lock Terrain Editing` | off | No terrain changes at all: EarthWright's edits, the game's own hoe and cultivator entries, and pickaxe digging. |
| `Admins Bypass Lock` | on | Admins may still change terrain while it is locked. |
| `Exempt Tools` | empty | Item prefab names that keep working while terrain is locked, for example `Cultivator` or `Cultivator, PickaxeIron`. An exempt tool may also clear objects and uproot. |
| `Combat Lock` | off | Terrain tools are blocked while a hostile creature within `Combat Lock Radius` (30 m) is alerted and hunting you. |

The admin-only Terraform entry and custom entries marked `admin: true` are refused for everyone else, and their edits are checked by the server.

Protection applies to brush clicks, ramps, roads, resets, undo and redo, snapshot restores and the terrain console commands. Clearing objects and uprooting follow the same rules, checked per object: an object under someone else's ward is left alone while the rest is cleared.

## Admin zones

Admin zones are named circles on the map, kept by the server in `EarthWright.Zones.yml` and sent to every player. What they mean depends on `Admin Zone Mode`:

| Mode | Effect |
| --- | --- |
| Off (default) | Zones do nothing. |
| OnlyInsideZones | Terrain can only be changed inside a zone. A zone that names a player is that player's alone. |
| NeverInsideZones | Terrain can never be changed inside a zone, except by the player a zone names. |

- The whole brush counts: with OnlyInsideZones every part of it must be inside an allowed zone, with NeverInsideZones no part may reach into one.
- Admins pass every zone while `Admins Bypass Zones` is on (default).
- Admins manage zones in game: `ew zone add <name> <radius> [player]` (centred on you), `ew zone remove <name>`, `ew zone list`, or in the protection section of the F6 panel. The server checks the admin list, writes the file and sends the change to everyone.
- A zone has a name of letters, digits, `-` and `_` (at most 32), a radius of 5 to 200 m and an optional character name. At most 100 zones.

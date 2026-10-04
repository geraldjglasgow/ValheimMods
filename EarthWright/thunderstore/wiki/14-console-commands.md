# Console Commands

EarthWright has one console command, `ew` (also `earthwright`), with subcommands. Open the console with F5. `ew` or `ew help` lists them. Commands marked admin work only for players on the server's admin list and the host (in single player, you). Most commands act around your character, so they need you in a world.

## Everyone

| Command | What it does |
| --- | --- |
| `ew help` | Lists the subcommands. |
| `ew on` / `ew off` | Switches EarthWright on or off for you (the setting `Use EarthWright`). Off: your hoe and cultivator behave as in the unmodded game; height limits and protection still apply. |
| `ew limits` | Shows the height limits where you stand: biome, raise and dig limit, whether the dig limit is lifted there, the admin limit, and how far the ground is from its original height. |
| `ew reset [radius]` | Resets the ground in a circle around you to the world's original height and texture. Free. The radius defaults to `Reset Around Radius` (10 m); players may go up to `Command Max Radius` (50 m). |
| `ew undo [steps]` | Undoes your last terrain changes, newest first (1 to 50 steps, default 1). |
| `ew redo [steps]` | Brings back changes you undid. |
| `ew history` | Lists your undo and redo steps. |
| `ew history clear` | Forgets them. |
| `ew snapshot save <name> [radius]` | Records the ground around you (radius default `Snapshot Radius`, 32 m; 1 to 128 m). |
| `ew snapshot restore <name>` | Puts a snapshot back (undoable). |
| `ew snapshot list` | Lists your snapshots. |
| `ew snapshot delete <name>` | Forgets a snapshot. |
| `ew forestry [radius]` | Clears trees, logs and stumps around you (default 10 m). |
| `ew debris [radius]` | Clears rocks, logs, loose stones, branches and flint around you (default 10 m); ore only while `Clear Ore Deposits` is on. |
| `ew zone list` | Shows the admin zone mode and every zone. |
| `ew language` | Shows your game language and how many texts are translated. |
| `ew language write` | Writes the English texts to `BepInEx/config/EarthWright.Language.English.yml` as a template for translators. |
| `ew language reload` | Applies a changed translation file. |

`ew forestry` and `ew debris` need `Clearing Enabled` on the server and a hoe or cultivator in your hand, cost what a click of Clear objects costs, and follow the clearing mode. See [Clearing and Reset](wiki:Clearing and Reset).

Snapshots and the undo history are described in [Undo and Snapshots](wiki:Undo and Snapshots); translation files in [YAML Files](wiki:YAML Files).

## Admins

| Command | What it does |
| --- | --- |
| `ew reload` | Reloads the .cfg and every EarthWright YAML file. |
| `ew zone add <name> <radius> [player]` | Adds an admin zone centred on you (radius 5 to 200 m), optionally for one character. |
| `ew zone remove <name>` | Removes a zone. |
| `ew pieces [radius] [refund]` | Removes every player-built piece around you, wards included (default 10 m, up to 128 m). Containers spill their contents; `refund` drops the building materials. |
| `ew terrain <op> [radius] [value] [options]` | Terrain operations around you or at the brush, see below. `ew terrain help` lists them. |

Admins may also use `ew forestry` and `ew debris` without clearing being switched on, for free, without the survival tool check and up to 128 m, and `ew reset` past `Command Max Radius`.

## ew terrain

`ew terrain <op> [radius] [value] [options]` changes the ground in a circle around you. The radius defaults to 10 m and is limited by the server's `Max Radius` (section 3, 100 m at most). The edit is checked by the server, has a hard edge and no step limit, and can be undone like any other change.

| Operation | What it does |
| --- | --- |
| `level [r] [height]` | Flat at the height (default: the height of your feet). |
| `raise [r] [metres]` | Raises by the amount (default 1 m). |
| `lower [r] [metres]` | Lowers by the amount (default 1 m). |
| `min [r] [height]` | Lifts everything lower up to the height. |
| `max [r] [height]` | Cuts everything higher down to the height. |
| `band [r] <min:max>` | Lifts or cuts everything into the band, for example `ew terrain band 15 20:35`. |
| `offset [r] [metres]` | Sets the ground to its original height plus the metres. |
| `slope [width]` | A straight slope from the ground at your feet to the ground you aim at (width 4 m by default, up to 50 m). |
| `remove [r]` | Digs down to the dig limit. |
| `reset [r]` | Back to the original height and ground texture. |
| `paint [r] paint=<kind>` | Paints only; the height stays. |

Options (any order, after the numbers):

| Option | Meaning |
| --- | --- |
| `share=<0..1>` | Changes only a random share of the points. |
| `skip=pieces` | Leaves the ground under building pieces alone. |
| `band=<min>:<max>` | Changes only points whose height lies in the band. |
| `include=<prefab,...>` | Changes only the ground near those objects (`*` as wildcard). |
| `ignore=<prefab,...>` | Leaves the ground near those objects alone (`*` as wildcard). |
| `unlimited` | Ignores the height limits (up to `Admin Limit`). |
| `paint=<kind>` | Also paints: `dirt`, `paved`, `cultivated`, `grass`, `original`, `clearvegetation`. |
| `hardness=<0..1>` | The edge: 1 hard (default), lower values soften it. |
| `shape=<circle or square>` | The footprint's shape (circle by default). |
| `width=<m>` | The width of `slope`. |
| `at=brush` | Uses your brush (its centre, shape, size, rotation and target height) instead of a circle around you. Meant for custom menu entries. |

Examples:

```
ew terrain level 20 32.5 skip=pieces
ew terrain raise 8 2 share=0.3
ew terrain paint 15 paint=paved
ew terrain remove 6 include=rock4_copper*
ew terrain level {radius} {target} at=brush shape=square
```

The last one is the form a custom menu entry uses (see [YAML Files](wiki:YAML Files)).

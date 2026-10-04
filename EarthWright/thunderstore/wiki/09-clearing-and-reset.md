# Clearing and Reset

Resetting puts the ground back the way the world generated it. Clearing removes trees, rocks and other natural objects. Settings are in section 5 of the .cfg.

## Resetting the ground

| Way | Area | Cost |
| --- | --- | --- |
| Reset ground entry (hoe) | The brush, with its shape, size and soft edge. | A normal swing (stamina, tool wear); no volume stone. |
| Reset key (U) | The brush's footprint (shape, size, rotation), at full strength up to its edge. | Free, never held back by the cooldown. |
| Reset around key (Shift+U) | A circle of `Reset Around Radius` (10 m) around you. | Free. |
| `ew reset [radius]` | A circle around you; the radius defaults to `Reset Around Radius`. | Free. |

- A reset brings back the generated height and the original ground texture. It never touches buildings, trees or rocks.
- With `Reset Skips Ground Under Buildings` on (default), the reset keys and `ew reset` leave the ground under building pieces as it is, so a reset never pulls the floor out from under a house. The Reset ground entry resets everything inside its brush, under buildings too.
- A reset that touches a ward you are not permitted on is refused as a whole; the other protection rules apply too (see [Limits and Protection](wiki:Limits and Protection)).
- The reset keys work while the hoe or cultivator is out in build mode with its menu closed.
- `ew reset` radii: players up to `Command Max Radius` (50 m); admins further, but never past the server's `Max Radius` (section 3, 100 m at most).
- Every reset can be undone (see [Undo and Snapshots](wiki:Undo and Snapshots)).

## Clearing objects

Clearing is off by default. With `Clearing Enabled` on:

- the hoe lists the Clear objects entry: one click clears every object of the enabled kinds inside the brush;
- the Groundbreaker entry also clears before it levels and paves;
- every player may use `ew forestry` and `ew debris`.

With it off, only admins can clear, by console command. `Clearing Radius` (0) set above 0 makes the Clear objects and Groundbreaker entries clear a circle of that radius instead of the brush.

| Kind | Switch | Default |
| --- | --- | --- |
| Standing trees | `Clear Trees` | on |
| Tree stumps | `Clear Stumps` | on |
| Fallen logs | `Clear Logs` | on |
| Bushes and shrubs | `Clear Shrubs` | on |
| Rocks and boulders (not ore) | `Clear Rocks` | on |
| Natural pickables: berries, mushrooms, flowers, loose stones, branches, flint | `Clear Pickables` | on |
| Rocks and veins that drop ore | `Clear Ore Deposits` | off |

- A rock or pickable counts as ore when one of its drops has a word of `Ore Drops` in its item name (`Ore,Scrap,Obsidian,Softtissue,Tar,Sulfur`; capital letters matter).
- Never cleared: anything a player built, creature nests, containers, the Leviathan, crops on cultivated ground, and the pickables in `Keep Pickables` (boss offerings, quest items and treasure such as dragon eggs, core stands and dvergr loot).
- Each object is checked against the protection rules: objects under someone else's ward, in a no-build place or refused by a zone or the lock are left alone while the rest is cleared. A message says how many were cleared and why others were not.
- Clearing is charged once per click, and only when something will be cleared.
- Removed objects do not come back with undo.

## Clearing mode

| `Clearing Mode` | What happens |
| --- | --- |
| Remove (default) | Cleared objects vanish. Nothing drops and no tool is needed. |
| Survival | Trees, logs and stumps need an axe, rocks a pickaxe, in your inventory and of a tier high enough for the object (a broken tool does not count). Shrubs and pickables need no tool. Objects you lack the tool for are left, and the message says which tool was missing. |

In Survival mode, `Survival Drops` (on) has objects chopped and mined the normal way, so they drop their wood, stone and pickings; a felled tree's log and stump are cleared in a few passes after the first. With `Survival Drops` off they vanish once you have the right tool.

## Clearing commands

| Command | What it clears |
| --- | --- |
| `ew forestry [radius]` | Trees, logs and stumps around you. |
| `ew debris [radius]` | Rocks, logs, loose stones, branches and flint around you; ore deposits only while `Clear Ore Deposits` is on. |
| `ew pieces [radius] [refund]` (admin) | Every player-built piece around you, wards included. Containers spill their contents. With `refund` the building materials drop. |

- The radius defaults to 10 m. Players may go up to `Command Max Radius` (50 m), admins up to 128 m.
- Players need `Clearing Enabled`, a hoe or cultivator in hand, and pay what a click of Clear objects costs (cooldown included). The clearing mode and its tool check apply.
- Admins may always use them, for free and without the tool check.
- The commands clear their fixed kinds; the per-kind switches above apply to the Clear objects and Groundbreaker entries.

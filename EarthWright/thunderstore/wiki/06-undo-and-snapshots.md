# Undo and Snapshots

EarthWright keeps a history of your own terrain changes on your own machine, so you can take them back and bring them back again. Each player has their own history; all of its settings are your own (section 7).

## Undo and redo

- Ctrl+Z undoes your last terrain change, Ctrl+Y brings back the change you last undid. The keys work while the hoe or cultivator is out in build mode with its menu closed, also while you walk.
- `History Size` (15, at most 50) is how many steps each list holds; the oldest drops out.
- Changes made within `Group Window` (0.3 s) of each other are one step, and a stroke dragged with the button held is always one step. A whole ramp or road is one step.
- A new change clears the redo list.
- While a ramp or road is being placed, the undo key first removes its last point.
- The console commands `ew undo [steps]` and `ew redo [steps]` do the same, up to 50 steps at once; `ew history` lists the steps and `ew history clear` forgets them. The panel (F6) has undo and redo buttons too.

## What is recorded

- Every terrain change you send: brush clicks and repeats, hard levels, ramps, roads, resets (the Reset entry, the reset keys and `ew reset`), the level-and-pave part of Groundbreaker, admin `ew terrain` operations and snapshot restores. Height and paint are both put back.
- Objects are not: trees, rocks and plants removed by clearing or uprooting do not come back, and built pieces are never touched.
- Costs are not refunded. Undo and redo themselves cost nothing and are never held back by the cooldown.

## How it behaves with others

- An undo puts back only what your change did. If someone else changes the ground next to it afterwards, their work stays.
- Undo and redo go through the same protection as any terrain edit: wards, the terrain lock, admin zones and who may use terrain tools still apply. They put back exactly the heights that were there, so the height limits do not cut them. A step that is refused, or whose ground is not loaded (stand closer), stays on its list and can be tried again.
- If only part of a step could be put back, the rest stays on the list.

## When it is cleared

The history and the snapshots belong to one world session: they are dropped when you log out, disconnect or start another world. Dying keeps them.

## Snapshots

A snapshot is a named copy of the ground around you, kept in memory until you log out.

| Command | What it does |
| --- | --- |
| `ew snapshot save <name> [radius]` | Records the ground within the radius around you. The radius defaults to `Snapshot Radius` (32 m) and can be 1 to 128 m. Saving under an existing name replaces it. |
| `ew snapshot restore <name>` | Puts that ground back. The ground it replaces goes on the undo list, so a restore can be undone. |
| `ew snapshot list` | Lists the snapshots with their place, radius, size and age. |
| `ew snapshot delete <name>` | Forgets a snapshot. |

- At most 32 snapshots are kept.
- Only ground that is loaded around you is recorded; the command says so when part of the area was not.
- A restore follows the same rules as undo: protection applies and the ground must be loaded.

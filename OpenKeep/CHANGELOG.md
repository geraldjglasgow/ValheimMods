# Changelog

## 1.1.0

- Stacks: config and YAML edits now reach items already picked up, items lying in the world and an open chest.
- Server sync moved to Charter, our own library. Existing config files work unchanged.
- Version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter`: `status`, `diff`, `versions`.
- Salvage: YAML key `exclude` renamed `deny`. A file still using `exclude:` warns once and its list is ignored.
- Stow: keys renamed — `Store All Key` → `Stow All Key` (`G`), `Restock Key` → `Top Up Key` (`R`),
  `Trash Flag Key` → `Junk Key` (`J`), `Trash Flagged Key` → `Destroy Junk Key` (`LeftShift + Delete`).
  `Store One Key` now `V`. Existing bindings carry over.
- Stow: junk marks move to `OpenKeep.junk` in character data. Marks from 1.0.0 are not read.
- Stow: buttons moved below their panels so they never cover the last item row; new `Button Row Offset`.
- Removed the unused `Hover Preview` and `Cart Upgrade Piece` settings.

## 1.0.0

- First full release: the 0.2.0 build under its final version number, no code changes.

## 0.2.0

- Signs: a vanilla sign above every player-built container naming its contents, most numerous first. Per-prefab
  switch, offset and rotation in `OpenKeep.Signs.yml`. Off by default. Edited signs keep your words; a sign
  removed with the hammer stays gone until reset. None on ships, carts, dungeon chests or spawned treasure.
- Console: `openkeep signs`, `signs reset`, `signs rewrite`.

## 0.1.0

- First release.
- Reach: crafting, upgrading, building and station feeding count and pay from nearby containers, ship holds and
  carts. Fill and Pull modifiers, per-character toggle, link lines, per-container rules.
- Stow: quick stack, stow all, top up and sort with hotkeys, favourites, junk marks and a trash can, routing by
  click, store one, find, dump, chest cycling, ground pickup by rule.
- Salvage: a Salvage tab and hotkey; return fraction, rounding, upgrade materials, recipe and station
  requirements, per-item fractions.
- Stacks: stack and weight multipliers, absolute values, ignore teleport restriction, merge into chests.
- Capacity: container sizes per prefab, fill and contents in the hover text.
- Carts: a workbench on every cart, with its own level and range.
- Shared chests: `View` shows a chest another player has open, read-only; `Full` lets two players work the same
  chest, every change sent to the owner's game to apply or refuse.
- Console command `openkeep`. Every gameplay setting synced and lockable, every YAML file synced and hot reloaded.

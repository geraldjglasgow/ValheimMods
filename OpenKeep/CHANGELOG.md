# Changelog

## 1.3.0
- Stow: the trash can moved off the button row into the gap between the weight and armour readouts on the right
  of the player panel, on a copy of their plate. The row below the panel is now four buttons and re-centred.
- Stow: `Confirm Trash` now defaults to off, so the Trash Key, the trash can and Destroy Junk act at once. Turn it
  on to get the game's popup back.
- Stow: the `Stow all` button is `Store all` again, next to `Store one`; `Stow All Key` is `Store All Key` and a
  cfg from 1.1.0 or 1.2.0 keeps its binding. The section `2. Stow` and `OpenKeep.Stow.yml` keep their names.
- Salvage: new synced settings `Skip Items With Mod Data` (on) and `Mod Data Prefixes` (`ecf_`). An item carrying
  custom item data under one of the prefixes — data another mod keeps on it, such as EliteCrafting's magic affixes —
  is no longer listed in the Salvage tab and the Salvage Key refuses it with a reason, so salvaging cannot destroy
  what that mod added. With EliteCrafting installed, OpenKeep salvages ordinary items into materials and
  EliteCrafting grinds its magic items into shards.

## 1.2.0

- Reach: new `stations:` map in `OpenKeep.Reach.yml` — per station prefab `enabled` and `allow` / `deny` item
  lists for station feeding (interact, Fill, Pull and the `From storage` hover line). Deny a meat and it is never
  pulled onto the cooking station; crafting and building still see it. Unlisted stations behave as before.
- Stow: new per-player setting `Sort Favourite Items` (on, the old behaviour). Off: the sort leaves favourite
  items where they are — for items kept in extra slots other mods add, which the sort used to pull into the main
  inventory.

## 1.1.1

- Multiplayer: joining a busy server no longer fails with "you have no copy" when every player runs the same
  version. The join check raced the config push over the same connection and gave up after a single 5 s deadline;
  it now greets first, retries every 5 s and only refuses after 20 s without any answer.
- Capacity: a container entry uncommented without its indentation (at the top of the file instead of inside
  `containers:`) is now applied anyway, with a warning explaining the indentation. Generated files comment
  entries as `  # piece_chest_wood: ...` so removing the `#` keeps the indentation.

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

# Changelog

## 1.3.1

- Shorter store page and changelog; nothing changes in game.

## 1.3.0

- New per-ship `SailForceOffset`: lower it to make a ship heel and capsize less.
- Fixed: joining a busy server no longer refuses a correct client.

## 1.2.0

- Every ship gets a full set of settings beside `Health`: sail, paddle, steering, drag, damage, wear and build cost,
  defaulting to the ship's own values.
- New `Multipliers` section on top of every ship's values.
- A health change sets only the maximum; loaded ships keep their current health.
- Ships added by other mods after the world loads get settings too.
- Server sync moved to Charter, our own library; config files work unchanged.
- A version mismatch refuses the join with a named reason.
- New console command `charter` (`status`, `diff`, `versions`).

## 1.1.5

- New store icon.

## 1.1.4

- Store page links to the GitHub repository.

## 1.1.3

- Rebuilt on the rewritten shared config libraries; no behaviour change.
- MIT licence added.

## 1.1.2

- Fixed: a frame rate loss.

## 1.1.1

- Fixed: Linux dedicated servers hung in an endless config reload.

## 1.1.0

- Updated for the current Valheim version.
- Jotunn no longer required.
- Config written on first start and reloaded on edit.
- Health changes apply to ships already in the world.

## 1.0.0

- Initial release.

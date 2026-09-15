# Changelog

## 1.2.0

- Every ship gets a full set of settings beside `Health`: `SailForce`, `PaddleForce`, `RudderSpeed`,
  `TurnForceSailing`, `TurnForcePaddling`, `ForwardDrag`, `SidewaysDrag`, `AngularDamping`, `WaterImpactDamage`,
  `UpsideDownDamage`, `WeatherWear`, `AshlandsOceanDamage`, `DamageTaken`, `Invulnerable`, `BuildCost`. Defaults
  are read from the ship itself, so a fresh config changes nothing and existing `Health` values are kept.
- New `Multipliers` section applying on top of every ship's own value.
- Changing a ship's health changes only its maximum; a loaded ship keeps its current health.
- Ships registered by another mod after the world loads get their entries.
- Server sync moved to Charter, our own library. Existing config files work unchanged.
- Version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter`: `status`, `diff`, `versions`.

## 1.1.5

- New store icon.

## 1.1.4

- Store page links to the GitHub repository.

## 1.1.3

- Rebuilt on the rewritten shared configuration libraries. No behaviour change.
- MIT licence added.

## 1.1.2

- Fixed a frame rate loss from the shared library's exception-tagging finalizer.

## 1.1.1

- Config hot reload polls every five seconds instead of watching the file, which hung Linux dedicated servers in
  an endless reload loop.

## 1.1.0

- Updated for the current Valheim version.
- Jotunn no longer required.
- Config written on first start and reloaded on edit.
- Health changes apply to ships already in the world.
- Removed debug logging.

## 1.0.0

- Initial release.

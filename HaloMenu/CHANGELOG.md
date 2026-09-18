# Changelog

## 0.1.0

- First cut, built from `SPEC.md`. Hold and Toggle activation, angle-based selection with a dead zone and
  hysteresis, gamepad stick support, evenly spaced segments (2-16) with live-configurable radii/gap/start angle,
  center label, hover scale/brightness/rim feedback, disabled-entry dimming and refusal shake.
- `HaloMenu.API.dll`, versioned 1.0.0: `HaloMenuAPI.Register`/`CreateRing`, the `Ring` and `RingEntry` types, and
  the six ring events. Zero dependency on `HaloMenu.dll`.
- `Sample.HaloMenuDemo`: a small project referencing `HaloMenu.API.dll` alone, showing both integration levels.
- Not yet verified in a live game session - see `PLAN.md`.

# PLAN.md - HaloMenu

Written black-box from `SPEC.md` and the game's own decompiled assemblies (`assembly_valheim.dll`,
`assembly_utils.dll`, build 25253764), on the clean-room box (`homelab01`); the rules are under "Developing mods"
in `../CLAUDE.md`. `SPEC.md` is kept (not deleted) because this box cannot launch the game - see "Not yet
verified" below for what a test client still needs to confirm.

## Status

Everything in the acceptance checklist is implemented. Nothing has been run in a live game session: this build box
is `homelab01`, a headless clean room with no display and no running Valheim client (see `../CLAUDE.md`). The
project has not even been compiled yet on this box - do that first (`~/build.sh HaloMenu`, see below) before
trusting anything past "it typechecks."

## Layout

```
HaloMenu/
  HaloMenu.sln                three projects: HaloMenu.API, HaloMenu, Sample.HaloMenuDemo
  SPEC.md, PLAN.md, README.md, CHANGELOG.md, LICENSE
  HaloMenu.API/                the frozen 1.0 contract - no dependency on HaloMenu.csproj, ever
    HaloMenuAPI.cs             static facade: Register, CreateRing, Version, IsAvailable, Provide
    HaloMenuService.cs         abstract class the plugin implements; keeps this assembly from referencing HaloMenu
    Ring.cs, RingEntry.cs, RingEvents.cs, ActivationMode.cs, GamepadStick.cs
  HaloMenu/                    the plugin
    HaloMenuPlugin.cs          entry: config, HaloMenuServiceImpl, HaloMenuAPI.Provide, HaloMenuDriver, Harmony,
                                ConfigReloader.Setup, Guard.Install last
    HaloLog.cs                 ManualLogSource wrapper filtered by Debug/LogLevel
    Config/
      RingDefaults.cs          one ring's factory defaults (the feature sheet's table)
      RingSettings.cs          binds one ring's Input/Layout/Visual sections; used for the default ring and for
                                every API-created ring (its own subsection, named after the ring id)
      HaloLogLevel.cs, HaloMenuConfig.cs   the Debug section plus the default ring's RingSettings
    Runtime/
      RingState.cs, SelectionMath.cs, HysteresisSelector.cs   the angle math and the ~2 degree sticky band
      SlotAssignment.cs        which entry (if any) sits in each segment, resolved once per open
      InputSource.cs           hotkey polling, mouse offset, gamepad stick vector
      CursorLockState.cs       save/restore ZCursor's lock state and visibility
      BlockingUiWatcher.cs     inventory/map/console/chat/text-input/menu/death, checked at open and once per frame
      GameUiScale.cs           reads Hud's own root Canvas.scaleFactor so HaloMenu inherits Valheim's UI scale
      RingRegistry.cs          only one ring open at a time; opening a second cancels the first
      HaloMenuDriver.cs        the one MonoBehaviour; ticks every ring's input every frame, open or closed
    Layout/
      Geometry.cs               chordLimit/radialLimit/iconSize math, the hysteresis constant
      SegmentGeometry.cs, RingLayout.cs   the cached per-segment geometry, rebuilt only on the documented triggers
    Rendering/
      WedgeGraphic.cs           a generated annulus-wedge Graphic, ~2 degrees per vertex pair
      SegmentView.cs            one pooled segment: fill wedge, rim wedge, icon, hover tween, shake
      HoverVisualConfig.cs, RingView.cs   the Canvas, the segment pool, the center label
    Api/
      RingRuntime.cs (+ .ApiSurface.cs, .Input.cs, .Layout.cs)   implements HaloMenu.API.Ring directly - the FSM,
                                the Ring property/event surface, Hold/Toggle input, and layout caching, split
                                across four files for size, all one partial class
      HaloMenuServiceImpl.cs   the plugin's HaloMenuService: owns the default ring and every API-created ring
    Patches/
      MouseLookPatch.cs         Player.SetMouseLook prefix: zeroes the look vector while a ring is open
      CursorCapturePatch.cs     GameCamera.UpdateMouseCapture prefix: unlocks/shows the cursor, skips the original
  Sample.HaloMenuDemo/          references HaloMenu.API.csproj only, proves the two-assembly split
```

## Build

`~/build.sh HaloMenu` from this box (see `../CLAUDE.md` and `../../CLAUDE.md`). Output: `dist/HaloMenu.dll` and
`dist/HaloMenu.API.dll` - both must ship together; `HaloMenu.API.dll` is deliberately not merged into `HaloMenu.dll`
(see `HaloMenu/ILRepack.targets`), because dependent mods and the plugin must share the exact same assembly, not
two internalized copies of same-named types.

## Decisions the spec left open

- **Camera look and cursor, mechanism.** The game's own `Hud.InRadial()` already suspends look and unlocks the
  cursor for the vanilla hotbar radial menu (`Hud.m_radialMenu`, a `RadialBase`; see `PlayerController.LateUpdate`
  and `GameCamera.UpdateMouseCapture`), and it looked tempting to piggyback on it by making `Hud.InRadial()` return
  true. Rejected: `PlayerController.LateUpdate` also reads `Hud.instance.m_radialMenu.IsBlockingInput` directly
  (not through `InRadial()`) whenever `InRadial()` is true, so forcing it true while the vanilla radial is null or
  inactive would NullReferenceException. Instead: a prefix on `Player.SetMouseLook` zeroes the look vector while a
  ring is open (the same choke point vanilla's own radial uses, one step downstream), and a prefix on
  `GameCamera.UpdateMouseCapture` sets the cursor unlocked/visible and skips the original for that frame. Neither
  touches vanilla's own radial menu or any of its fields.
- **Cursor restore.** `CursorLockState` saves `ZCursor.LockState`/`IsVisible` once on open and restores them once
  on close, literally per the spec's "store the prior state, do not assume." Because `GameCamera.UpdateMouseCapture`
  runs unpatched again the very next frame after close, it also re-derives the correct state from current game
  state on its own - the explicit restore and the vanilla method's next tick should agree; if they ever visibly
  don't in testing, trust the vanilla method's frame-after value over `CursorLockState`'s snapshot.
- **StartAngleOffset = -90 "centers a segment at 12 o'clock".** Implemented the given formula exactly
  (`atan2(d.y, d.x)`, standard math convention, y-up screen space - i.e. Unity's own `Input.mousePosition` and
  RectTransform local space, no conversion needed). Under that formula, -90 places a segment *boundary*, not a
  center, at 12 o'clock whenever segmentCount is a multiple of 4 (4, 8, 12, 16 - i.e. most of the useful range).
  The numbered formula is called out in the spec as "the single most important behavioral requirement," so it was
  implemented as written rather than bent to make the prose line up for every segment count; the config default
  stays -90 as the table specifies.
- **Segment count vs. entry count.** `SegmentCount` is a fixed geometric slot count (config-driven, cached in
  `RingLayout`), never derived from how many entries are registered - `ring.SegmentCount = 6` in the Level 2
  example only makes sense as an author's explicit geometry choice otherwise. Visible entries fill slots by Order,
  densely (an invisible entry is skipped and does not consume a slot, so the next visible entry shifts up);
  entries beyond the slot count are dropped. A slot nobody filled renders as a plain wedge; releasing on it behaves
  like releasing in the dead zone (a cancel, not a selection of nothing).
- **What "an entry is registered/removed/icon changes" invalidates.** Under the fixed-slot-count model above,
  none of those three change segment *geometry* at all. `SlotAssignment` (which entry sits in which slot) is
  rebuilt fresh on every open regardless, which already covers registration, removal and a changed `Icon`
  reference for free - there is no separate cache to invalidate for that trigger. Visibility (`IsVisible`) is
  resolved once per open, not re-checked while the ring stays open, to honor the layout/slot stability the
  performance section asks for; `IsEnabled` (opacity, refusal-shake) is re-read live every frame, since the spec
  explicitly describes it changing the ring's *appearance* while open.
- **Visual settings need no cache at all.** `HoverScale`, `AnimationDuration`, `SegmentColor`, `HighlightColor`
  and `ShowCenterLabel` are read live every frame the ring is open and applied directly to each segment's Graphic
  color / RectTransform scale - there was never a baked copy of them to invalidate, so "changes without a restart"
  falls out for free rather than needing an explicit rebuild path.
- **UI scale.** Rather than re-deriving Valheim's own reference-resolution formula, `GameUiScale.Factor()` reads
  the scale factor of `Hud.instance.m_rootObject`'s own Canvas at layout-build time and multiplies it by the
  ring's own `UIScale` setting. This is what "respect Valheim's own UI scale setting" means here: whatever the
  game computes for its HUD, HaloMenu inherits, rather than a second, possibly-diverging formula. Falls back to
  `Screen.height / 1080` when `Hud.instance` is null (no local player yet).
- **Canvas sorting order.** `RingView` uses a fixed `sortingOrder = 100` on a Screen Space - Overlay canvas, guessed
  to sit above the HUD and below modal dialogs. Not verified against Valheim's own canvas sorting orders in a live
  session (this box cannot launch the game) - the first in-game test should confirm nothing vanilla draws over the
  ring, and nothing the ring draws obscures a real popup or dialog; adjust the constant if it does.
- **Hysteresis is a fixed constant**, `Geometry.HysteresisDegrees = 2f`, not a config entry - the spec's table has
  no key for it ("roughly 2 degrees" in prose only).
- **Blocking-UI polling vs. the per-frame budget.** The Performance section's "one atan2, one index comparison"
  budget is about the geometry/selection path specifically (the section is titled Performance and immediately
  precedes those two paragraphs with "Nothing in the geometry section is recomputed per frame"). `BlockingUiWatcher`
  (six-ish static property reads) and hotkey polling are a separate, unavoidable input/lifecycle concern and run
  every frame regardless of ring state; they were not folded into that budget.
- **Toggle mode's select gestures.** Left-click, or pressing the hotkey again, both select; right-click or Escape
  both cancel - matching the FSM diagram and the prose exactly. Escape is read with a plain `Input.GetKeyDown`, not
  gated behind `BlockingUiWatcher`, since a ring open by definition means no blocking UI is already up.
- **Colors are hex strings**, not `ConfigEntry<Color>` - matches how color settings are already bound elsewhere in
  this workspace (`Party.PartyColor`), parsed with `ColorUtility.TryParseHtmlString` at use time, defaulting back
  to the compiled-in default on an unparsable string rather than throwing.
- **Plugin GUID** `com.HaloMenu`, matching ShipConfig's naming rather than the `milkyteam.*` GUIDs OpenKeep uses;
  no strong reason either way, ShipConfig is the closer sibling in shape (small, single assembly plus libraries).
- **Packaging.** No `thunderstore/` folder yet (icon, manifest, `pack.ps1`): the acceptance checklist asks for a
  README, the GPLv3 text and a documented API example, not a store listing. Add Thunderstore packaging as a
  separate pass once there is real art for the icon and the mod has actually been tested in-game.

## Not yet verified (needs a real game session, not this box)

Everything in `SPEC.md`'s acceptance checklist that requires a running client or server - selection accuracy at
every listed segment count, no boundary flicker, gamepad parity, the hover cues together, `AnimationDuration = 0`
snapping cleanly, the center label, closing on every blocking UI, cursor/camera restore, the layout-rebuild-only-
on-invalidation claim (the spec suggests verifying with a counter log - none is wired up yet, add one if the
rebuild frequency ever looks wrong), zero-Instantiate-on-open/close in the profiler, correct proportions across
resolutions and aspect ratios, a vanilla dedicated server, single-player-has-the-mod, no network traffic, the
sample mod loading with and without HaloMenu present, two independent rings, and cancelling `OnSelecting`.

Test by copying `dist/HaloMenu.dll` and `dist/HaloMenu.API.dll` (and, to try the sample,
`Sample.HaloMenuDemo/dist/Sample.HaloMenuDemo.dll`) to a real client's `BepInEx/plugins`, per this workspace's
usual `LocalTesting` r2modman profile flow - not on this box.

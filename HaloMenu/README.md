# HaloMenu

A client-side radial menu framework for Valheim. Hold (or toggle) a hotkey, a ring of icons opens at screen
center, move the mouse (or a gamepad stick) toward one to highlight it, release to select it.

HaloMenu itself adds no gameplay: it is a framework other mods build on through its public API. With nothing
registered, it opens an empty ring and does nothing else.

### Features

- Hold or Toggle activation, per ring
- Selection by angle, not hit-testing: overshoot the ring's outer edge and it still selects; a gamepad's right
  (or left) analogue stick drives the same selection path as the mouse
- A dead zone at the center cancels; a small hysteresis band keeps the highlight from flickering on a boundary
- Evenly spaced segments, 2 to 16 of them, with configurable gaps, radii and start angle; the hovered entry's name
  shows in the ring's center
- Hover feedback: scale, a brightness lift and a rim highlight together, tunable duration (0 snaps instantly)
- Disabled entries dim and refuse selection with a shake; invisible entries do not take up a segment at all
- Every setting is live - changing it in the BepInEx Configuration Manager or the .cfg file takes effect the next
  time a ring opens, no restart
- Zero network traffic. HaloMenu never sends a packet, works on a fully vanilla server, and works with only one
  player in the session having it installed

### For players

Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/), then drop
`HaloMenu.dll` and `HaloMenu.API.dll` into `BepInEx/plugins`. Both files are required. By itself HaloMenu does
nothing visible; install a mod that uses it (or see `Sample.HaloMenuDemo` in this repository for what that looks
like) to get an actual menu.

Default hotkey: hold Left Alt.

### For mod authors: the API

Reference `HaloMenu.API.dll` only - never `HaloMenu.dll`. Your mod compiles and loads whether or not HaloMenu is
installed; call `HaloMenuAPI.IsAvailable` to check, or just call the API - `Register` on a missing HaloMenu is a
silent no-op, not an exception.

**Add an entry to the shared default ring:**

```csharp
using HaloMenu.API;

HaloMenuAPI.Register(new RingEntry
{
    Id = "mymod.teleport_home",
    Label = "Teleport Home",
    Icon = mySprite,
    Order = 100,
    IsVisible = () => Player.m_localPlayer != null,
    IsEnabled = () => !Player.m_localPlayer.IsEncumbered(),
    OnSelect = () => DoTeleport(),
});
```

`Id` is namespaced and must be unique; registering the same `Id` again replaces the prior entry. `Order` sorts
entries clockwise from the ring's start angle. `IsVisible` is checked once when the ring opens (an invisible entry
does not take up a segment); `IsEnabled` is checked live while the ring is open.

**Own an independent ring**, with its own hotkey, config subsection and layout - the better option once your mod
has more than a couple of entries, so you are not fighting other mods for slots on the shared ring:

```csharp
using HaloMenu.API;

Ring ring = HaloMenuAPI.CreateRing("mymod.buildmenu");
ring.Hotkey = myConfigEntry;      // a ConfigEntry<KeyboardShortcut> from your own ConfigFile
ring.SegmentCount = 6;
ring.Add(entryA);
ring.Add(entryB);
ring.OnSelected += (r, entry) => Logger.LogInfo($"{entry.Label} selected");
ring.OnCancelled += r => Logger.LogInfo("cancelled");
```

Only one ring across the whole game is ever open at once; opening a second closes whichever one was open, as a
cancel.

**Events**, all on `Ring`: `OnRingOpening` (return false to block), `OnRingOpened`, `OnHighlightChanged`,
`OnSelecting` (return false to turn a selection into a cancel), `OnSelected`, `OnCancelled`.

**Versioning:** the API surface is frozen at 1.0 - additive changes only within the 1.x line; a breaking change
ships as a separate `HaloMenu.API` 2.0 assembly alongside 1.x, never in place of it. `HaloMenuAPI.Version` is
queryable at runtime.

See `Sample.HaloMenuDemo/` in this repository for a complete, compiling example (Level 1 and Level 2 both), and
`SPEC.md` for the full behavioral specification this mod was built from.

### Building

Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` repository checked out next to
this one (see the workspace root `CLAUDE.md`).

```
"/c/Program Files/dotnet/dotnet" build HaloMenu/HaloMenu.sln -c Release
```

Output: `dist/HaloMenu.dll` and `dist/HaloMenu.API.dll` (both required). `Sample.HaloMenuDemo` builds to its own
`bin/` - it exists to prove the API compiles and loads standalone, not as a mod to ship.

### License

GPLv3. See `LICENSE`.

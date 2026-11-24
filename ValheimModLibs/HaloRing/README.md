# HaloRing

A radial ring a mod opens itself: icons round the screen centre, and the direction of the mouse (or the gamepad stick)
from the centre picks one. Taken out of the HaloMenu mod on 2026-11-02 so other mods can have the ring without it; the HaloMenu mod was then retired and deleted the same day.

- `HaloRingMenu.Install(harmony)`: once in the mod's `Awake`, with the mod's own Harmony. Installs this copy's
  WindowInput window (its patches once per merged copy).
- `new HaloRingMenu(owner, name, report)`: one ring. Its canvas is `"<owner> Ring [<name>]"`; `report` gets the text
  when an item's `IsEnabled` throws.
- `Style` (`HaloRingStyle`): radii, gap, start angle, dead zone, icon size, UI scale, hover scale and time, colours,
  centre label on or off, gamepad on or off and which stick. The retired HaloMenu's look by default. `HaloRingStyle.TopFirst(n)`
  is the start angle that puts segment 0's middle at twelve o'clock.
- `Open(items, segmentCount)`: opens now. `HaloRingItem(id, label, icon)` with `Sublabel` (a second line under the
  name, such as a cost), `Enabled` or `IsEnabled` (asked every frame; a disabled item is drawn dim), `Tag` (the
  caller's object). Segment 0 at the start angle, the rest counter-clockwise; a null leaves a segment empty. False when
  a game screen is up, the count is below 1, or another mod's ring is open.
- `Tick(deltaTime)` once a frame while open (false when it closed itself: a game screen opened, or Esc on the start
  screen). `CloseIfBlocked`, `CloseOnEscape` and `Track` are its three steps, for a caller that orders its own input
  between them.
- `Release(refuseDisabled = true)`: the pick, as a `HaloRingPick` (`Picked`: closed on the item; `Refused`: a disabled
  item shook and the ring stays open; `Cancelled`: nothing hovered, closed). `Cancel()`; `Cancelled` and
  `HighlightChanged` events; `IsOpen`, `Highlighted`, `HighlightedIndex`; `HaloRingMenu.Blocked`;
  `HaloRingMenu.HoldUntilReleased(button)` keeps input held back until a mouse button that closed a ring is let go.

While a ring is open:

- the cursor is free (its lock and visibility saved and put back on close), the camera does not follow the mouse, the
  player does not attack, block or use hotbar keys, walking stays (WindowInput),
- the hovered segment grows and brightens with a gold rim and its name (and sublabel) in the middle; a pointer resting
  on a boundary does not flicker (a 2 degree band),
- Esc cancels it instead of opening the game menu; the inventory, map, console, chat, a text input, the menu or death
  cancel it.

No config and no keys: the caller decides when the ring opens and closes. Only one ring is open at a time: opening one
cancels another of the same mod, and a ring does not open while another mod's is open (each copy leaves a check of its
own rings in the process-wide `AppDomain` slot `HaloRing.openRing`, a `Func<bool>`). Nothing else is shared between
copies. Client only; nothing is sent.

```csharp
HaloRingMenu.Install(harmony);
var ring = new HaloRingMenu("MyMod", "tools", message => Logger.LogError(message));
// on the mod's own button held:
ring.Open(items, items.Length);
// every frame while open:
if (ring.Tick(Time.unscaledDeltaTime) && !buttonHeld)
    Use(ring.Release());
```

Merged into the consuming mod with ILRepack like every library here, with WindowInput. Consumers:
EliteEquipment (the casters' spell ring on right mouse).

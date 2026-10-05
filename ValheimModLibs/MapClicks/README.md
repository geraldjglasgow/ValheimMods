# MapClicks

A click on a mod's own icon on the large map that leaves the game's pins working under it.

- `IconClick.Install(harmony)`: once in the mod's `Awake`. A prefix on `Minimap.OnMapDblClick` and a postfix on
  `Minimap.Update`, once per merged copy, by name.
- `IconClick.Hold(action)`: from a left click on the icon. The action runs once the game's double click window
  (`DoubleClickSeconds`, 0.3 s) has passed; a double click there drops it, so the game's own double click places a
  pin under the icon instead. It runs on a later frame: check again that what it acts on is still there.
- `IconClick.PinUnderPointer(map)`: true when one of the player's placed pins is under the pointer, as the game's own
  click and removal find it (its `GetClosestPin` within `PinInteractRadius`). A right click on the icon lets the
  game remove that pin first.
- `IconClick.Drop()`: forgets a held action.

```csharp
IconClick.Install(harmony);
// in a Minimap.OnMapLeftClick prefix, on a hit:
IconClick.Hold(() => { if (StillChoosing) Travel(target); });
// in a Minimap.RemovePinUnderPointer prefix, on a hit:
if (IconClick.PinUnderPointer(Minimap.instance)) return true;
```

Consumers: Wayfare (portal icons while choosing a destination), OpenKeep (bed icons during the choice of bed).

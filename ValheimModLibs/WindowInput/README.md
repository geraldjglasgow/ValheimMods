# WindowInput

A mod's own window treated like one of the game's while it is open.

- `GameWindow.Install(harmony)`: once in the mod's `Awake`. Installs the patches once per merged copy.
- `GameWindow.Add(isOpen, close)`: a window, with how to tell it is open and how to close it.
- `GameWindow.AnyOpen`, `GameWindow.CloseOpen()`.

While any registered window is open:

- the cursor is shown and free (unless the game menu is up),
- the player does not attack, block, use the hotbar or other player keys (`Player.TakeInput`),
- the camera does not follow the mouse (`PlayerController.InInventoryEtc`) and the wheel does not zoom
  (`ZInput.GetMouseScrollWheel`; the window's own scroll views still scroll),
- Esc closes the window instead of opening the game menu (`Menu.Update`).

Walking stays possible, as with the inventory.

```csharp
GameWindow.Install(harmony);
GameWindow.Add(() => RecapWindow.IsOpen, RecapWindow.Close);
```

Merged into the consuming mod with ILRepack like every library here. Consumer: EliteCreaturesReborn (the death recap
window). EarthWright's `Preview/PanelInputPatches` is the same rules and is to move onto it.

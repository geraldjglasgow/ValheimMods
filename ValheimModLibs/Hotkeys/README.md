# Hotkeys

A mod's hotkeys, read the same way in every mod of the workspace.

- `Hotkey.Pressed(entry)`: the shortcut's main key went down this frame. A shortcut with modifiers needs all of them
  held and no other Shift, Ctrl or Alt; any other key may be held, so `LeftAlt + D` fires while the player walks with W
  (BepInEx's own `KeyboardShortcut.IsDown` refuses a shortcut while any other key at all is held). A single key fires
  only with no Shift, Ctrl or Alt held, so `Z` never fires together with `LeftControl + Z`. `None` never fires.
- `Hotkey.Held(entry)`: the main key and its modifiers are held (for modifier settings such as `LeftShift`).
- `Typing.Active`: the player is typing, and neither of the above fires: the chat has focus, the console or the game's
  text input (signs, tame names) is open, a Unity `InputField` or `TMP_InputField` is selected, or a window the mod
  registered with `Typing.AddWindow(() => open)` is open (an IMGUI window such as the mod's YAML editor, which no input
  field reveals).

Where a key works (outside the inventory, only while a tool is held, ...) stays the mod's decision; for keys that act
in the world, the game's own `Player.TakeInput()` is the hotbar keys' rule.

```csharp
FoodKey = synced.Bind("2. Slots", "Food Key", new KeyboardShortcut(KeyCode.Z), "...", synced: false);
Typing.AddWindow(() => synced.YamlEditor.IsOpen);

if (Hotkey.Pressed(FoodKey) && player.TakeInput())
    EatEverything(player);
```

Merged into the consuming mod with ILRepack like every library here, so the registered windows are the mod's own.
Consumer: PackPanel (Food Key, Mead Key). OpenKeep's `Core/Keys` is the same rules and is to move onto it.

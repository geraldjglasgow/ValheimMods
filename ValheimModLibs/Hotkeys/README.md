# Hotkeys

A mod's hotkeys, read the same way in every mod of the workspace.

- `Hotkey.Pressed(entry)`: the shortcut's main key went down this frame. A shortcut with modifiers needs all of them
  held and no other Shift, Ctrl or Alt; any other key may be held, so `LeftAlt + D` fires while the player walks with W
  (BepInEx's own `KeyboardShortcut.IsDown` refuses a shortcut while any other key at all is held). A single key fires
  only with no Shift, Ctrl or Alt held, so `Z` never fires together with `LeftControl + Z`. `None` never fires.
  Mods read their keys many times a frame, so the main key is tested first; the modifiers and `Typing.Active` are
  read only on the frame it goes down (for `Held` and `ModifiersHeld`, only while the keys are held).
- `Hotkey.Held(entry)`: the main key and its modifiers are held (for modifier settings such as `LeftShift`).
- `Hotkey.ModifiersHeld(entry)`: a set shortcut's modifiers are all held, its main key or not; false without modifiers
  (PackPanel holds the game's hotbar back while Alt, its Mead Slot keys' modifier, is held).
- `Hotkey.KeyHeld(key)`: one key held now. Shift, Ctrl and Alt are read through the game's `ZInput` (Unity's input
  system, which lets go of every key when the window loses focus), never Unity's old `Input`, which keeps a modifier
  held after Alt + Tab out or an overlay's Alt + Z until it is pressed again (a stuck Alt made PackPanel's plain 1 to 3
  drink meads, 2026-10-05). Every modifier check above uses it.
- `KeyNames.Short(shortcut)`: the shortcut's name for a key cap on the screen, modifiers first without their side:
  `Z`, `Shift+Z`, `Ctrl+1`; empty for `None`.
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

- `Wheel`: the mouse wheel for a mod. `Wheel.Install(harmony)` once (one postfix on `ZInput.GetMouseScrollWheel`), then
  `Wheel.Claim(() => held)`. While any claim holds, `Wheel.Notches()` (once a frame) gives +1 (away from the player), -1
  or 0, small touchpad deltas adding up to the game's one-notch threshold, and the rest of the game reads the wheel as 0:
  the camera does not zoom and nothing else scrolls. A claim is asked on every read of the wheel, so keep it cheap.
  `Wheel.Raw()` reads the wheel past the block. EarthWright's brush has the same rule in its own `Brush/ScrollInput`.

Merged into the consuming mod with ILRepack like every library here, so the registered windows are the mod's own.
Consumer: PackPanel (Food Key, Mead Key, Mead Slot keys, and their caps on the Food and Mead bar); the EliteEquipment
casters bench in AssetLab (`Wheel`: Left Alt + wheel picks a wand's spell). OpenKeep's `Core/Keys` is the same rules and is to move onto it.

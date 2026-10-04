# Controls and Hotkeys

Every key is a setting of your own (never synced), so each player can rebind them in the .cfg. Most live in section `10. Controls`; the others sit in the section of their feature, named in the tables. Unless a table says otherwise, a key works while the hoe or cultivator is out in build mode, a terrain entry is selected, the build menu is closed and EarthWright is on for you. The selected entry's description in the build menu lists the keys that work with it, as you have bound them.

## Brush

| Default | Action | Setting |
| --- | --- | --- |
| Left Alt + mouse wheel | Change the selected brush value | `Adjust Modifier` |
| `]` / `[` | Increase / decrease the selected value (hold to repeat) | `Increase Key`, `Decrease Key` |
| hold Left Ctrl | Steps five times larger while changing a value; 2 m steps for the exact target height | `Fast Modifier` |
| B | Select the next value: size, amount, hardness, turn, depth, target | `Select Value Key` |
| N | Next shape; with the Ramp entry selected, the next ramp profile | `Shape Key` |
| Right / Left arrow | Turn a square, rectangle or frame by 22.5 degrees (hold to repeat) | `Rotate Right Key`, `Rotate Left Key` |
| Home | Turn back to north | `Reset Rotation Key` |
| hold Z | Snap the brush centre to the 1 m grid (not while Ctrl is held) | `Snap Hold Key` |
| I | Grid mode on or off | `Grid Mode Key` |
| O | Aim at the edge on or off | `Aim At Edge Key` |
| L | Next level style: Ease, Step, Instant | `Level Style Key` |
| P | Next paint | `Paint Key` |
| F9 | Hard level: one Instant, hard-edged level to the target (level or raise entry) | `Hard Level Key` |

## Target height

| Default | Action | Setting |
| --- | --- | --- |
| hold Left Shift | Level to the aimed ground instead of your feet (the game's own alternative placement) | the game's key |
| K | Lock the current target height; press again to release | `Lock Height Key` |
| PageUp / PageDown | Move the locked height by 0.1 m, five times that with Shift (hold to repeat) | `Height Up Key`, `Height Down Key` |
| End | Back to the ground under your feet | `Back To Feet Key` |
| Y | Next target mode: feet, aimed, continue the flat | `Target Mode Key` |
| middle mouse | Copy the height of the building piece under the crosshair and lock it | `Copy Floor Height Key` |

## Ramps and roads (section 4)

| Default | Action | Setting |
| --- | --- | --- |
| J | Quick ramp from your feet to the aimed point (any terrain entry selected) | `Quick Ramp Key` |
| Backspace | Remove the last ramp point or road waypoint | `Remove Last Point Key` |
| H | Carve the planned road with the current paint | `Carve Road Key` |
| Shift+H | Carve the planned road paved | `Carve Paved Road Key` |
| hold Left Ctrl | Put the ramp's whole width on the cursor's side | `One Side Modifier` |
| hold Left Alt while building | Blend the ramp's ends into the ground | `Blend Ends Modifier` |

## Reset, undo and costs

| Default | Action | Setting |
| --- | --- | --- |
| U | Reset the ground inside the brush (free) | `Reset Key` (section 5) |
| Shift+U | Reset the ground around you (free) | `Reset Around Key` (section 5) |
| Ctrl+Z | Undo your last terrain change (a ramp or road point first) | `Undo Key` (section 7) |
| Ctrl+Y | Redo | `Redo Key` (section 7) |
| F7 | Free build on or off, if the server allows it for you | `Free Build Key` (section 8) |
| hold Right Alt | Admins: ignore the height limits | `Admin Limit Override Key` (section 11) |

Shift+U, undo, redo and free build need only the hoe or cultivator out in build mode with its menu closed; U also needs the brush aimed at the ground.

## Anywhere

| Default | Action | Setting |
| --- | --- | --- |
| F6 | Open or close the EarthWright panel (also from the Esc menu); Esc closes it | `Panel Key` (section 9) |
| F8 | World grid on or off; shown while you build with any tool | `World Grid Key` (section 9) |
| I | Seed grid on or off, with the cultivator out and a seed or sapling selected | `Seed Grid Key` (section 14) |

## Mouse buttons and the wheel

- Holding the place button keeps applying a brush entry (`Hold To Repeat`, `Repeat Start Delay` 0.35 s, `Repeat Rate` 0.15 s). The size, rotation and height keys repeat with the same timing.
- `Plain Wheel Adjusts` (off): the wheel alone changes the selected value while a terrain entry is selected.
- `Camera Zoom Block`: WhileAdjusting (default) stops the camera zooming only while the wheel changes a value; Always stops it whenever a terrain entry is selected; Off lets it zoom regardless.

## Gamepad

With `Gamepad Controls` on (default), hold the gamepad modifier (`Gamepad Modifier`, the game's alternate-keys button `JoyAltKeys`, the left trigger in the default layout) and use:

| Button | Action | Setting |
| --- | --- | --- |
| D-pad up / down | Increase / decrease the selected value | `Gamepad Value Up`, `Gamepad Value Down` |
| D-pad right | Select the next value | `Gamepad Select Value` |
| D-pad left | Next shape | `Gamepad Shape` |

Buttons use the game's input names (`JoyDPadUp`, `JoyButtonX`, `JoyLBumper` and so on); an empty value turns one off. The camera does not zoom while the modifier is held with a terrain entry selected; the minimap still zooms one step on D-pad left and right. Everything else stays on the keyboard.

## How keys are read

- A single key does not fire while Shift, Ctrl or Alt is held, so U and Shift+U never both fire. The repeating keys (size, rotation, height) accept an extra Shift or Ctrl, which picks bigger steps.
- A key with modifiers needs exactly those modifiers. Other keys do not block it, so Ctrl+Z works while you walk.
- No key fires while you type: chat, the console, a sign, a text field, the EarthWright panel's fields.
- Keys are written in the .cfg as Unity key names, for example `LeftAlt`, `Mouse2` or `H + LeftShift`.
- While a terrain entry is selected, F9 does not also switch the gamepad layout, the middle mouse button does not remove the piece you copy a height from, and in the game's debug mode Z, B, K and L do not also trigger fly, no-cost, kill and remove-drops.

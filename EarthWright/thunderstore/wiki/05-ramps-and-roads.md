# Ramps and Roads

The hoe's Ramp and Road entries build a sloped surface between points you click, cutting into hills and filling hollows as needed. Nothing touches the ground until you build; until then the points are only yours, shown as a preview.

## Ramp

1. Select Ramp in the hoe's menu.
2. Click the start. Click the end (at least 1 m away).
3. Move the cursor and click a third time to build.

- The width follows the brush size: twice its radius, within `Min Width` (1 m) and `Max Width` (20 m). Change it with Left Alt + wheel or `[` / `]`. With `Width From Cursor` on, after the second click the width follows how far the cursor is to the side of the ramp's middle.
- Hold Left Ctrl (`One Side Modifier`) while setting the width or clicking to build: the whole width goes to the side the cursor is on.
- Hold Left Alt (`Blend Ends Modifier`) while clicking to build: both ends blend into the ground over `End Blend Length` (3 m).
- The shape key (N) cycles the ramp profile while the Ramp entry is selected. Your choice is kept in your own config (`Ramp Profile`).
- Backspace removes the end, then the start. The undo key does the same first, before it undoes any terrain.

| Profile | Shape seen from the side |
| --- | --- |
| Straight | One even slope from end to end. |
| SoftJoins | An even slope with short rounded joins where it meets the ground at both ends (`Soft Join Length`, 2 m). |
| SoftEnds | The slope builds up over the first part and eases off over the last: gentle landings. |
| SCurve | Flat at both ends and steepest in the middle. |

## Quick ramp

The quick ramp key (J) builds a ramp at once from your feet to the aimed point, with the current profile and the width of the selected entry's brush. It works while any terrain entry of the hoe or cultivator is selected. Its ends blend into the ground while `Quick Ramp Blends Ends` is on (your own setting, on). The server can switch it off (`Quick Ramp`); it also needs the Ramp entry to be enabled. With the Ramp entry selected and no point set, a faint preview shows the ramp the key would build (`Quick Ramp Preview`).

## Road

1. Select Road in the hoe's menu.
2. Click to place waypoints (each at least 1 m from the last). The road follows a smooth curve through them.
3. Press H to carve it with the current paint, or Shift+H to carve it paved.

- The width follows the brush size, as for the ramp.
- Backspace (or the undo key) removes the last waypoint.
- While `Road Blends Ends` is on (your own setting, on), the road's first and last metres blend into the ground past its ends.
- The waypoints are forgotten once the road is carved.

## Heights of the points

A clicked point sits on the ground there, unless you have set a height: a locked, exact or copied floor height (K, the Target value, middle mouse) becomes the height of every point you click. Lock a height, click the start, change the height, click the end, and the ramp runs between exactly those two heights. See [Brush and Target](wiki:Brush and Target).

## Paint and sides

- A ramp paints its surface with the server's `Ramp Paint`, a road carved with H with `Road Paint` (both dirt by default). The paint key (P) picks another paint, or Keep for none. Shift+H always paves.
- `Shoulder Width` (2 m) blends the ground beside a ramp or road into its surface, cutting or filling as needed. 0 leaves a sharp edge.

## Preview and HUD

- The centre line and both edges are drawn at the planned heights, with a post on every point and a line to where the next waypoint would go.
- Segments are coloured for carts: green up to `Cart Easy Slope` (20 degrees), yellow up to `Cart Hard Slope` (25 degrees), red beyond. All of it turns red while the ramp or road cannot be built.
- With `Show Changed Points` (section 4), a dot marks every ground point it changes: blue where the ground is filled, orange where it is cut, red past the height limit.
- `Preview Through Ground` keeps the lines visible where they cut into a hill.
- The HUD shows the length, rise, width, slope, the number of points, the paint, a set height, what the next click or key does, and a warning when it is steeper than `Warning Slope` (38 degrees; players slide down ground steeper than that).

## Limits

The server's rules (section 4) refuse a ramp or road that is:

| Check | Default |
| --- | --- |
| shorter than 1 m or longer than `Max Length`, measured on the map | 128 m |
| steeper anywhere along its middle than `Max Slope` (75 is the most allowed) | 75 degrees |
| changing more ground points than `Max Points` (one per square metre, shoulders included) | 1024 |
| raising or digging past the height limits, while `Refuse Past Height Limit` is on (off: it is built and the ground stops at the limit) | on |
| on ground that is not loaded, warded against you, or otherwise protected | - |

## Costs, undo and when points are forgotten

- Building a ramp or carving a road is charged once, as one use of the Ramp or Road entry: stamina, tool wear, the extra item and any volume stone (see [Costs and Stations](wiki:Costs and Stations)), and it starts the cooldown. The quick ramp is charged as the Ramp entry. A refused build costs nothing.
- A ramp or road is one undo step.
- With `Clear Points When Deselected` on (your own setting, on), points are forgotten when you select another entry or put the tool away. They are always forgotten when you die or log out.
- Tool levels can lock the Ramp and Road entries (`Level Unlocks`, features `ramp` and `road`).

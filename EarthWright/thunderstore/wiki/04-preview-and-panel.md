# Preview and Panel

Everything on this page is shown only to you and changes nothing in the world. All of its settings are your own (section 9, never synced).

## On the ground

| Preview | What it shows | Settings |
| --- | --- | --- |
| Outline | The brush's footprint on the ground: amber when the click would go through, red while it would be refused, grey while the aimed point is out of reach. A second, thinner outline shows where the paint reaches when that differs from the height footprint. | `Show Outline`, `Outline Width`, `Outline Colour`, `Paint Outline Colour`, `Blocked Colour`, `Out Of Reach Colour` |
| Changed points | A marker on every ground point the click would change: green where it would rise, orange where it would sink, red where a height limit holds it back. Paint-only entries mark every cell they would paint. Worked out with the same rules the owner of the ground uses, and refreshed when someone else edits the ground. | `Show Changed Points`, `Point Limit` (4000), `Point Size`, `Raise Colour`, `Lower Colour`, `Limited Colour`, `Paint Cell Colour` |
| Volume | A see-through cylinder or box between the ground and the height the click aims for (level, raise, lower and the admin height operations): blue within reach, orange beyond it, red while refused. | `Show Volume`, `Volume Colour`, `Volume Beyond Reach Colour`, `Volume Opacity` |
| The game's ghost ring | Resize: the ring follows the brush (hidden for shapes other than the circle). Hide: removed. Leave: as the game draws it. | `Ghost Visuals` (Resize) |
| Piece highlight | Building pieces standing inside the brush light up as the hammer shows them. | `Highlight Pieces` |
| World grid | Lines on the ground around the crosshair, on whole metres by default, with a brighter line every fifth. The world grid key (F8) shows or hides it while you build with any tool. It can also run through the last building piece you aimed at and turn with it. | `World Grid Key`, `World Grid Radius`, `World Grid Spacing`, `World Grid Line Width`, `World Grid Colour`, `World Grid Major Colour`, `World Grid Major Every`, `World Grid Anchor` |

Colours are written in the .cfg as hex `RRGGBBAA`; the last pair is the opacity.

Ramps and roads draw their own preview, see [Ramps and Roads](wiki:Ramps and Roads).

## Next to the crosshair

While a terrain tool is out and its menu is closed, a block of text sits next to the crosshair (`Show HUD`, `HUD Scale`, `HUD Offset X`, `HUD Offset Y`):

- the brush: size and soft rim, shape, rotation, hardness, the amount, level style, paint, grid and edge flags, with the selected value highlighted;
- the target height and where it comes from;
- the points the click changes and the volume it raises and lowers ("N points, +X m³ / −Y m³");
- the cost of one swing at the current size, have/need per item, red where you are short (`Show Costs`, section 8);
- the ramp or road being planned;
- a line with the ground tile under the crosshair, its height, how far it is from the world's original height, and how far the target is from it (`Show Cursor Readout`);
- in grey, "out of reach" with the distance and your reach, while you aim too far;
- in red, why the click would be refused (a ward, missing materials, a too steep ramp and so on).

A small note above it says when the server has locked EarthWright's settings (`Show Locked Badge`). The block hides while a menu, the inventory, the map, the console or a text field is open, and while you have hidden the game's HUD.

## Dust

`Remove Dust` (off by default) removes the dust and pebble effects of your terrain clicks; the sounds stay. Other players then see no dust from your tool either.

## The panel

The panel key (F6) opens and closes the EarthWright panel; the game's Esc menu also has an "EarthWright" button right below Settings. It is a window you can drag (its place is remembered, `Panel Position`) and scale (`Panel Scale`). Esc closes it. While you type in one of its fields no hotkey fires; Enter or a click on the window's background ends typing.

| Part | What it holds |
| --- | --- |
| Brush | Typed fields for the selected entry's values: size, depth, rotation, hardness (in percent) and its amount (metres, max step, strength or density). Then the target height as a typed field (typing it fixes the height), a lock toggle and the target mode, and choice grids for the shape, level style and paint, offering what the keys would. Select a terrain entry first; ramps and roads show only their size. |
| Presets | Buttons from `Raise Presets` ("1x2, 5x2, 5x3, 8x3" by default, each "amount x radius" in metres). A click sets the amount and the radius of a raise or lower entry at once. |
| Undo | Undo and redo buttons with the number of steps each can take. |
| Protection (admins only) | The terrain lock, who may use terrain tools and the admin zone mode, each with a button to change it; the list of admin zones with a remove button each; a row to add a zone at your position (name, radius, optional player). The setting buttons show only where the change reaches the server: on the server or host itself, or while the server binds its configuration. |

The panel's header says when the server has locked the settings; typed values are still limited by the server's ranges.

# Brush and Target

The brush is active while you hold the hoe or the cultivator in build mode, a terrain entry is selected, the build menu is closed and EarthWright is on (the server's `Enabled` and your own `Use EarthWright`). The keys below are the defaults; every one can be rebound, see [Controls and Hotkeys](wiki:Controls and Hotkeys).

## Brush values

The value selector key (B) picks which value the mouse wheel and the `[` / `]` keys change. Only the values that apply to the selected entry and shape are offered, and the HUD next to the crosshair highlights the selected one.

| Value | Meaning | Range |
| --- | --- | --- |
| Size | The radius: of a circle or ring, half the side of a square or frame, half the width of a rectangle. | `Minimum Radius` 0.5 m to `Maximum Radius` 20 m (see below) |
| Amount | Raise or lower: metres per click. Level in Ease or Step: the max step per click. Smooth: the strength. Vegetation paint: the grass density. | raise/lower: `Minimum Amount` 0.05 m to `Maximum Amount` 8 m; max step 0.1 to 1000 m; strength and density 0 to 1 |
| Hardness | The edge: 1 is a hard edge, 0 fades all the way from the centre. The HUD also shows the soft rim's width in metres. | 0 to 1 |
| Turn | The rotation of a square, rectangle or frame. | 0 to 360 degrees |
| Depth | The rectangle's half depth, the ring's inner radius or the frame's band width. | up to the shape's size |
| Target | An exact target height for levelling (see Target height below). | any height |

Changing a value:

- Hold Left Alt and turn the mouse wheel, or press `]` / `[` (hold to repeat).
- Hold Left Ctrl for steps five times larger (`Fast Step Multiplier`).
- Step sizes are your own settings (`Radius Step` 0.5 m, `Amount Step` 0.1, `Hardness Step` 0.05, `Rotation Step` 22.5 degrees, `Depth Step` 0.5 m). A negative step reverses the wheel.
- The level max step moves by the amount step below 2 m, by 1 m from 2 m and by 10 m from 20 m.
- `Plain Wheel Adjusts` lets the wheel change values without Alt while a terrain entry is selected. `Camera Zoom Block` decides when the camera ignores the wheel: only while it changes a value (default), always with a terrain entry selected, or never.
- `Announce Changes` also shows every change in the middle of the screen.

Values are remembered per entry until you log out, so Level ground and Raise ground each keep their own size. When the server changes the size settings or the brush YAML file, every entry starts again from its new defaults.

## Size limits

The largest radius you can dial in is the smallest of:

- `Maximum Radius` (20 m), or an entry's own `maxRadius` in `EarthWright.Brushes.yml`;
- the radius your tool's level allows (`Radius Per Level`, off by default);
- the skill cap (`Skill Cap`, off by default): below `Skill Cap Start Level` (25) an entry keeps its own size, and the cap grows to `Skill Cap Radius` (6 m) at `Skill Cap Full Level` (60) of the chosen skill (Crafting by default).

Whatever a player sends, the machine that owns the ground cuts a stroke to the server's `Max Radius` (100 m) and `Max Amount` (50 m) in section 3.

A brush smaller than the spacing of the ground's points (1 m) still changes the point nearest its centre, so a click never does nothing.

## Shapes and placement

- Shape key (N): cycles circle, square, rectangle, ring and frame, as far as the server's `Allowed Shapes` and your tool's level allow. Shapes are measured in world metres, so a square stays a square at any rotation.
- Rotate keys (Right and Left arrow): turn a square, rectangle or frame by the rotation step. Home turns it back to north.
- Grid mode (I): the centre and the size snap to whole metres, rotation is off and every covered point gets the full effect (no soft rim). Size steps are at least 1 m.
- Snap while held (Z): the brush centre snaps to the world's 1 m grid. Not while Ctrl is held, so Ctrl+Z still undoes.
- Aim at edge (O): the crosshair marks the near edge of the brush instead of its centre, handy for extending a flat area.
- Aim through objects (`Aim Through Objects`, your own setting, on): when the crosshair is on a rock, tree, cliff or water, the brush goes to the ground behind or under it instead of the game refusing the click. Building pieces are never looked through.

## Target height

Entries that level take their height from the target: Level ground, Paved road and Cultivate (while they level), Groundbreaker and Terraform. The HUD shows the target and where it comes from.

| Mode | Height | How to get it |
| --- | --- | --- |
| Feet | The ground under you, as the unmodded game does. Hold Shift (the game's alternative placement) to use the aimed ground instead. | the default; End returns to it |
| Aimed | The ground under the crosshair. | Target mode key (Y) |
| Continued | "Continue the flat": the height of earlier flat edits next to the crosshair, within `Continue Tolerance` (0.25 m). Without a clear flat it uses the crosshair. | Target mode key (Y) |
| Locked | A fixed height, used by every click until released. | Lock key (K) locks the current target; press again to release |
| Exact | A typed or stepped height. | select the Target value (B) and change it: 0.25 m steps, 2 m with Ctrl; or type it in the panel |
| Floor | The top of the building piece under the crosshair, locked. | Copy floor height (middle mouse) |

- Y cycles Feet, Aimed and Continued. End always goes back to Feet.
- PageUp and PageDown move a fixed height by 0.1 m (`Height Key Step`), five times that with Shift, and repeat while held. Pressed with a live mode, they first lock the current height.
- Releasing a lock returns to the live mode you used before.
- `Show Target Messages` announces locking, copying and releasing in the middle of the screen.
- The target is reset when you log in or change character; you start in `Default Target` (Feet).
- While a terrain entry is selected the middle mouse button copies a floor's height and never removes the piece.
- Ramps and roads use a locked, exact or copied height for the points you click (see [Ramps and Roads](wiki:Ramps and Roads)).

## Hard level

The hard level key (F9) arms an Instant, hard-edged level to the current target height and swings once, with a levelling entry or Raise ground selected. It is charged like a normal click. Your tool's level must allow the Instant style.

## Hold to repeat

With `Hold To Repeat` on (default), holding the place button keeps applying a brush entry: after `Repeat Start Delay` (0.35 s), then every `Repeat Rate` (0.15 s). Every repeat is a normal click with its costs, and never faster than the server's cooldown. Ramps, roads, Clear objects, Groundbreaker and Uproot do not repeat; custom entries repeat when their file says so. The size, rotation and height keys also repeat while held.

A stroke dragged with the button held is one undo step.

## Starting size and the size multiplier

- An entry starts at its own size (the game's sizes for the game's entries), times the server's `Size Multiplier`. The switches `Multiply Level`, `Multiply Raise` (raise and lower), `Multiply Smooth` and `Multiply Paint` (paint-only entries) decide which kinds of entry it applies to; Reset ground, ramps, roads, Groundbreaker, Clear objects, Uproot and custom entries keep their own size.
- `EarthWright.Brushes.yml` can give any entry, or a whole tool, its own starting size, limits, amount, hardness, style and shape, or stop it from being resized (see [YAML Files](wiki:YAML Files)).
- Terrain pieces that other mods add to the hoe or cultivator are resized too while `Resize Modded Terrain Pieces` is on.

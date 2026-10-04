# Inventory panel artwork

The approved Timber style uses `timber_background.png`: the warm vanilla-inspired wallpaper.
`TimberFrame.Contour` defines irregular cuts with lightly curved joins and a few deeper cuts.
`TimberBackground` draws the background and shaded wood edge as one continuous mesh. There is
no separate outer band or metal decoration. Icons, text, buttons and controls are not clipped.
Wallpaper UVs remain aligned to the canvas so growing an inventory reveals more of the texture.
The keyring retains its circular shape with the same wallpaper and continuous wood edge.

## Assets retained

- `timber_background.png`: embedded default; source and prompt in `PackPanel/artwork/background-vanilla-warm.*`.
- `timber_button.png`: approved blank button template, retained for button work.
- `panel.png`, `button*.png`, `cell.png`, `ring_panel.png`, `ring_line.png`: original Brown artwork.
- `icon_*.png`: material-colored slot illustrations; source and comparison in `PackPanel/artwork/slot-icons` and `slot_icons.py`.
- `trash.png`, `backpack_*.png` and backpack bundles: existing functional artwork.
- `skin_art.py`: source generator for the original procedural artwork.
- `PackPanel/artwork`: saved Minimal Plus, Vanilla Inspired and Vanilla Warm alternatives with their prompts.

The obsolete framed `timber_panel.png` and rejected charcoal-leather image/notes were removed.
The warm wallpaper falls back directly to the original Brown panel if unavailable.

## Live testing

The `PackPanel.ArtTest/background.png` override was removed in 0.6.1 (it polled the disk while a panel showed);
a new wallpaper is tried by embedding it in a local build.

`Timber Border Width` and `Timber Border Jaggedness` update the contour live through config reload.
New contour code still requires a restart. Brown and vanilla remain selectable.

Validation: preview rendered from the contour code; bounds and concave triangulation checked over
90 combinations of panel size, shape, bevel width and roughness. The deeper-cut preview was approved
before deployment. Coordinate checks are in `PackPanel/tests/TimberCoordinates.Check.csproj`.

## Button template reference

The built-in imagegen tool produced the background options and blank button art. Their background
prompts live beside the source images in `PackPanel/artwork`.
The button source is 1983 x 793; the content rectangle is x=57, y=105, width=1870, height=551 in
top-left coordinates. Corner slices occupy 25% of its cropped height at 5 UI units. Labels are separate.

## Inventory open/close performance

Contour points and concave fill triangles are cached per panel rectangle, shape and border-settings version.
Closing/reopening a panel, animating it, or replacing the wallpaper does not retriangulate it.
Already themed panels do not trigger global Image scans when re-enabled. New panels are queued individually;
full discovery is limited to initial/scene setup and theme switches. Reapplying the same active theme is a no-op.
The approved contour remains unchanged. Build/geometry checks pass; runtime latency needs checking after restart.

Approved background tint: RGB 0.92, alpha 1. The 8% darker live preview is now the default for every Timber surface, including newly created panels and the keyring. Icons and text are unaffected.

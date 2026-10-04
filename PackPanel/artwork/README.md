# Inventory background options

| Option | Image | Notes |
| --- | --- | --- |
| Minimal Plus | [background-minimal-plus.png](background-minimal-plus.png) | Minimal warm brown with slightly stronger soft shading; retained as a preferred option |
| Vanilla Inspired | [background-vanilla-inspired.png](background-vanilla-inspired.png) | Muted gray/taupe painted surface based on the vanilla crafting panel |
| Vanilla Warm | [background-vanilla-warm.png](background-vanilla-warm.png) | Same painted pattern recolored to the user's warm brown screenshot |

The companion Markdown files record the imagegen prompts. These are test artwork options, not new in-game theme settings.
The in-game preview file (`PackPanel.ArtTest/background.png`) was removed in 0.6.1: try an option by embedding it as
`timber_background.png` in a local build.

# Slot icons

The empty-slot icons in `PackPanel/assets/icon_<slot>.png` (head, chest, legs, back, backpack, utility, food, mead,
ammo, purse for coins, key, tacklebox, tackle) are the approved flat ochre set, `slot-icons-approved/64/` (256 px
sources in `slot-icons-approved/256/`, drawn by `slot-icons-silhouette-concept/draw.py`), installed 2026-09-28.
`slot_icons.py` renders the earlier 3D set and writes it straight into `PackPanel/assets`: running it replaces the
approved icons. `slot-icons/`, `slot-icons-2d-concept/` and `slot-icons-gold-concept/` are the other studies.


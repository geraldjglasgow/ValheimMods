# EarthWright

Terraforming for Valheim's hoe and cultivator. Resize the brush, pick its shape and edge, level to an exact
height, raise and lower by exact amounts, smooth, paint, reset, build ramps and curved roads and undo what you did,
all inside the game's own build menu. Costs, height limits, wards and admin rules are
set by the server. Every gameplay setting is synced and lockable, every key and display option is per player, and
the config file and five YAML files hot reload.

**Required on the server and on every client.** The machine that owns a piece of ground applies EarthWright's
edits to it, and ground raised or dug past the game's ±8 m shows correctly only where EarthWright is installed.

### The brush
- **Size**: Alt + mouse wheel, or `]` / `[` (hold Ctrl for bigger steps). 0.5 to 20 m by default; the server sets
  the range, per entry in `EarthWright.Brushes.yml` too. The game's own entries start at their own size and every
  entry remembers its size. Painting grows with the brush. A size multiplier, a cap that grows with a skill and a
  cap per tool level are optional.
- **Which value the wheel changes**: `B` steps through size, amount (raise and lower amount, level step or smooth
  strength), edge hardness, rotation, depth or inner size, and target height. The HUD highlights the one selected.
- **Shape**: `N` cycles circle, square, rectangle, ring and frame. The arrow keys turn it by 22.5°, `Home` turns
  it back to north. Squares are true squares in the world, never the game's diamond.
- **Edge**: from soft (the effect fades from the centre) to hard (full effect up to the rim). A soft edge on a
  square raise gives a bevel instead of a cliff.
- **Grid mode** (`I`): the brush snaps to whole metres with a hard edge; hold `Z` to snap only while held.
  **Aim at the edge** (`O`): the crosshair marks the near edge of the area instead of its centre.
- Hold the mouse button to keep applying (start delay and rate are settings). The camera does not zoom while you
  adjust with the wheel.

### Target height (what levelling levels to)
- Your feet by default, like the game; hold Shift for the ground under the crosshair.
- `K` locks the current height; `PageUp` / `PageDown` move it by 0.1 m (Shift: 0.5 m); `End` goes back to your
  feet. Choosing the target height with `B` sets an exact height in 0.25 m steps (Ctrl: 2 m).
- `Y` cycles feet, aimed ground and **continue the flat**: the height of the levelled ground next to the cursor,
  so a new stroke extends the platform you already made (falls back to the crosshair when two platforms disagree).
- Middle mouse copies the height of the floor piece you aim at.
- The HUD shows the target height and where it came from. Clicking a rock or cliff next to the ground still works:
  the brush lands on the terrain behind it.

### Operations
- **Level** in three styles, cycled with `L`: Ease (like the game, at most the step per click), Step (a larger
  step, up to 1000 m) and Instant (a flat plateau in one click). `F9` makes one instant, hard-edged plateau with
  Level or Raise selected.
- **Raise** and **Lower** by an exact amount (0.05 to 8 m by default); repeated raises add up to the raise limit.
- **Smooth** evens out ridges without flattening the ground.
- **Paint**: `P` cycles the entry's own paint, dirt, paved, cultivated, grass, the biome's original ground, grass
  density, clear grass, and keep (change the height, leave the paint).
- **Reset**: the Reset entry, `U` for the brush area or `Shift + U` around you puts the ground and its paint back
  as the world made it. Reset never removes buildings, trees or rocks and skips the ground under buildings.
- **Undo and redo**: `Ctrl + Z` / `Ctrl + Y`, 15 steps by default. A held stroke is one step; ramps, roads, resets
  and paint are covered. Costs are not refunded. The history is cleared when you log out.

### Build menu entries
The game's Level ground, Raise ground, Pathen, Paved road, Cultivate and Replant use the brush. EarthWright adds:
- **Hoe**: Lower ground, Smooth, Paint, Reset, Ramp, Road, Groundbreaker (clear, level and pave in one swing),
  Clear objects (off unless the server allows clearing) and Terraform (admins: level past the height limits).
- **Cultivator**: Till (cultivate without changing the height) and Uproot (removes wild berries, mushrooms,
  branches and stones in the brush).
- The hoe and cultivator menus get the game's full build menu with search, recent and favourites. Every entry can
  be switched off, and each entry's description lists every key that works with it, as you have bound them. Any console command can become a menu
  entry through `EarthWright.Entries.yml`.

### Ramps and roads
- **Ramp**: click the start, click the end, set the width with the brush size (or by moving the cursor sideways),
  click again to build. `N` cycles the profile: straight, soft joins, soft ends, S-curve. Hold Ctrl to widen one
  side only, Alt to blend the ends into the ground. `J` builds a ramp from your feet to the crosshair at once.
- **Road**: click waypoints, the road curves through them; `H` carves it, `Shift + H` carves it paved.
- The preview is coloured for carts (up to 20° green, up to 25° yellow, steeper red) and turns red when the ramp is
  too steep, too long or past the height limit. The HUD shows length, rise, width, slope and points changed.
- `Backspace`, or the undo key, removes the last point first.

### Preview and HUD
- The outline of the brush follows the ground; markers show every point that will change (raise, lower, held by a
  limit); a translucent volume shows the ground up to the target; buildings inside the area are highlighted.
- Next to the crosshair: size, shape, edge, amount, style, paint, target height and source, cost, and the tile and
  height under the cursor. A badge shows when the server locks the settings.
- `F8` draws a world grid on the ground. `F6` (or the EarthWright button in the Esc menu) opens a panel to type
  exact values, use raise presets (1×2, 5×2, 5×3, 8×3), undo and redo, and for admins the protection settings.
- Colours, sizes and every part of the display are personal settings. Dust from the hoe can be switched off.

### Costs
Vanilla by default. The server can set stamina (off, fixed, scaled with the brush), tool wear, materials (scaled
with the brush, per entry in `EarthWright.Costs.yml`, plus an item per swing), the stations needed (paving without
a stonecutter), stone per cubic metre raised and per square metre paved, and a cooldown. Free build (`F7`) is for
admins by default. The HUD shows what a stroke costs and what you have, in red when you are short. Resets and undo are
always free; the admin Terraform entry pays stamina and wear but no stone.

### Height limits
The game allows 8 m up and 8 m down from the world's ground. EarthWright makes both limits settings (0.5 to 512 m),
per biome in `EarthWright.Limits.yml`, and uses them for the pickaxe too. Admins may go further (Admin Limit, and
hold `Right Alt` while clicking). Optional: gentle slopes (digging also lowers the surroundings, raising lifts them),
and a strict mode where the dig limit lifts only near ore, buried treasure and tar. `ew limits` shows the limits
where you stand.

### Protection
- Wards are checked over the whole brush, on your machine and again on the machine that owns the ground.
- No terraforming in no-build places (traders, boss altars) or inside dungeons.
- The server can allow terrain tools to everyone, admins only or nobody, lock all terrain editing (admins bypass,
  tools can be exempt, the pickaxe included), refuse edits while hostile creatures are hunting you, and limit
  terraforming to admin zones (`ew zone add <name> <radius> [player]`, `ew zone list`, `ew zone remove <name>`).
- The same rules apply to clearing objects and uprooting.

### Tools
While a terrain tool is out: reach 20 m (5 to 50), a light on the tool that everyone sees (its colour is yours),
faster running, and a torch that stays in the left hand. The hoe and cultivator upgrade to level 6.
Sprinting on dirt roads is 10 % faster and cheaper, on paved roads 20 %. Seeds can snap to a grid (`I` with a seed
selected), and the cultivator can be allowed on ground the game forbids.

### Console commands
`ew help` lists them. `ew on` / `ew off` switch EarthWright for you (also typed in chat as `/ew off`). `ew reset
[radius]`, `ew undo`, `ew redo`, `ew history`, `ew snapshot save|restore|list|delete <name> [radius]`, `ew limits`,
`ew zone ...`, `ew language write|reload`, `ew reload` (admin). Clearing: `ew forestry [radius]`, `ew debris
[radius]`, and for admins `ew pieces [radius]` and `ew terrain <op> ...` (level, raise, lower, min, max, band,
offset, slope, remove, reset, paint; filters for a share of the points, skipping ground under buildings, and
including or ignoring objects). `ew terrain help` lists the options.

### Files
- `milkyteam.earthwright.cfg`: every setting, numbered by section as above. Hot reloaded.
- `EarthWright.Brushes.yml` (brush sizes per entry), `EarthWright.Costs.yml` (costs per entry),
  `EarthWright.Entries.yml` (custom menu entries), `EarthWright.Limits.yml` (limits per biome),
  `EarthWright.Zones.yml` (admin zones, kept by the server). Hot reloaded and synced from the server.
- `EarthWright.Language.<Language>.yml`: translations. `ew language write` writes the English template.

### Server settings
With `Lock Configuration` on (the default) every player uses the server's values and cannot change them; server
admins still can, in game. Keys, colours and display options are always your own.

### How to install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod on the server and on every client.

For a manual install, put `EarthWright.dll` into `BepInEx/plugins`. Keep EarthWright installed once you have
raised or dug past 8 m: without it the game draws that ground clamped to its own limits.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` folder next to this one.
`pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build EarthWright/EarthWright.csproj -c Release
```

### Bugs and feature requests
The source is open, at the repository linked from the store page (`website_url` in `thunderstore/manifest.json`):
https://github.com/geraldjglasgow/ValheimMods. Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you
distribute that is built from it must carry the same freedoms and be released under the same licence, with source.
See the `LICENSE` file for the full terms.

# YAML Files

Five YAML files sit next to the .cfg in `BepInEx/config`. Each is written with explanatory comments on the first start when it does not exist yet.

| File | What it holds |
| --- | --- |
| `EarthWright.Brushes.yml` | Starting sizes and limits per entry or per tool |
| `EarthWright.Costs.yml` | Costs per entry |
| `EarthWright.Entries.yml` | Custom menu entries that run console commands |
| `EarthWright.Limits.yml` | Height limits per biome |
| `EarthWright.Zones.yml` | Admin zones (kept by the server) |

How they behave:

- Saved changes are picked up within about five seconds; `ew reload` (admin) reloads every file at once.
- Extra files named like the main one with anything added before `.yml` (for example `EarthWright.Brushes.MyServer.yml`), in the config folder or next to `EarthWright.dll`, are read as well. This does not apply to the zones file, which is one file.
- A file with an error is rejected as a whole and the previous version stays in force. Errors and warnings name the exact place (for example `entries[2]: ...`) in the BepInEx log; unknown keys are warned about.
- While the server's `Lock Configuration` is on, players use the server's files and their own are ignored. With it off, every player uses their own. Admin zones always come from the server.
- Indent with spaces, two per level, as in the examples.

## EarthWright.Brushes.yml

Starting values and radius limits of the hoe and cultivator entries. Every key is optional. A missing value falls back to the entry's tool, then to the .cfg (`Minimum Radius`, `Maximum Radius`, `Default Level Style`), then to the entry's own values.

- `families:` by tool: `hoe`, `cultivator`, `modded` (terrain pieces other mods add).
- `entries:` by piece prefab name: the game's `mud_road_v2` (Level ground), `raise_v2` (Raise ground), `path_v2` (Pathen), `paved_road_v2` (Paved road), `cultivate_v2` (Cultivate), `replant_v2` (Replant); EarthWright's `ew_lower`, `ew_smooth`, `ew_paint`, `ew_reset`, `ew_ramp`, `ew_road`, `ew_groundbreaker`, `ew_clear`, `ew_terraform`, `ew_till`, `ew_uproot`; custom entries as `ew_custom_<id>`; and any modded terrain piece.

| Key | Meaning |
| --- | --- |
| `radius` | Starting radius in metres (a circle's radius, a square's half side), up to 100. Without it the entry starts at its own size times the .cfg `Size Multiplier`. |
| `minRadius` | Smallest radius for this entry, replacing the .cfg `Minimum Radius`. |
| `maxRadius` | Largest radius, replacing the .cfg `Maximum Radius` (tool levels and the skill cap may still lower it). |
| `amount` | Starting raise or lower amount per click in metres (the .cfg amount range still applies). |
| `maxStep` | Starting max step per click for the Ease and Step level styles, in metres. |
| `strength` | Starting smoothing strength, 0 to 1. |
| `hardness` | Starting edge hardness, 0 (soft) to 1 (hard). |
| `style` | Starting level style: `Ease`, `Step` or `Instant`. |
| `shape` | Starting shape: `Circle`, `Square`, `Rectangle`, `Ring` or `Frame`. |
| `resizable` | `false` keeps the entry at its starting radius; the size keys do not change it. |

```yaml
families:
  hoe:
  cultivator:
    maxRadius: 12
  modded:

entries:
  mud_road_v2:
    hardness: 0.5
    style: Step
  paved_road_v2:
    radius: 2
    shape: Square
  replant_v2:
    resizable: false
```

Saving the file makes every entry start again from its new values (what players changed in game is forgotten).

## EarthWright.Costs.yml

Costs of single entries; whatever an entry does not set follows section 8 of the .cfg. Under `entries:`, each key is an entry's prefab name (as above). Every value is optional:

| Key | Meaning |
| --- | --- |
| `resources` | The items one swing costs, `ItemPrefab: amount`. Replaces the entry's own materials; `resources: {}` makes it free. Still grows with the brush as `Material Radius Exponent` says, and `Charge Materials = false` still switches it off. |
| `stamina` | Stamina per swing, replacing `Stamina Mode` for this entry (the stamina skill still lowers it). |
| `durability` | Multiplies the entry's tool wear: 0 none, 0.5 half, 2 double. |
| `station` | Prefab name of the crafting station the entry needs instead of its own, for example `piece_workbench`, `piece_stonecutter`, `forge` or `blackforge`. |
| `stationRequired` | `true` or `false`: whether the entry needs a station at all, whatever the per-tool switches say. |

```yaml
entries:
  raise_v2:
    resources:
      Stone: 3
    stamina: 8
    station: piece_workbench
  paved_road_v2:
    stationRequired: false
  mud_road_v2:
    durability: 0.5
  ew_ramp:
    resources:
      Wood: 2
```

## EarthWright.Entries.yml

Custom menu entries: any console command as an entry in the hoe's or the cultivator's build menu. Only `id` and `command` are required.

| Key | Meaning |
| --- | --- |
| `id` | Letters, digits, `_` and `-`, unique across the files. The piece is named `ew_custom_<id>`. |
| `name` | The name in the menu, as written (default: the id). The menu's search finds it. |
| `description` | The text under the name, as written. Name and description may use the game's own `$words`. |
| `icon` | The icon of any piece or item by prefab name (`mud_road_v2`, `Stone`, `Hoe` ...), or one of EarthWright's: `lower`, `smooth`, `paint`, `reset`, `ramp`, `road`, `clear`, `groundbreaker`, `terraform`, `till`, `uproot`, `custom`. |
| `tool` | `hoe` or `cultivator` (default hoe). |
| `position` | The place in the tool's menu, 0 first (default: right after EarthWright's own entries). |
| `radius` | The brush radius the entry starts with (more than 0). |
| `height` | The brush amount in metres the entry starts with, for `{height}`. |
| `shape` | The shape the entry starts with: `circle`, `square`, `rectangle`, `ring` or `frame`. |
| `command` | The console command a click runs, as you would type it in the console (F5). Put it in quotes when it starts with `{` or contains `: `. |
| `repeat` | `true` runs the command again while the button is held, every `Custom Entry Repeat Interval` (0.25 s). |
| `admin` | `true` lists the entry only for admins and refuses it for everyone else. |

Placeholders in the command, filled from your brush (numbers with a dot, at most three decimals):

| Placeholder | Value |
| --- | --- |
| `{x}` `{y}` `{z}` | The brush centre |
| `{radius}` `{radius2}` | The brush radius and its second size (rectangle depth, ring inner radius, frame band) |
| `{height}` | The brush amount |
| `{target}` | The target height of levelling |
| `{rotation}` | The brush rotation in degrees |
| `{shape}` | `circle`, `square`, `rectangle`, `ring` or `frame` |
| `{player}` | Your character's name (may contain spaces) |

```yaml
entries:
  - id: level_big
    name: Big level
    description: Levels a large square at the brush to the target height.
    icon: terraform
    tool: hoe
    radius: 12
    command: ew terrain level {radius} {target} at=brush shape=square
    admin: true
  - id: reset_here
    name: Reset around me
    description: Resets the ground around you to the world's original height and texture.
    icon: reset
    tool: hoe
    radius: 6
    command: ew reset {radius}
```

- The command runs on your machine through the game's console, as if you had typed it, so the command's own rules apply: admin-only `ew` commands stay admin-only, and the game's cheat commands need what they always need.
- A click is charged like one swing of an EarthWright entry before the command runs, and tool levels can lock a custom entry (`custom_<id>` in `Level Unlocks`).
- The shipped file has no entries (`entries: []`); the two above are its commented examples.

## EarthWright.Limits.yml

Height limits per biome. A biome that is not listed, or a value that is left out, uses `Raise Limit` and `Dig Limit` from section 6. Values are metres from 0.5 to 512 (outside values are clamped with a warning).

Biomes: `Meadows`, `BlackForest`, `Swamp`, `Mountain`, `Plains`, `Mistlands`, `AshLands`, `DeepNorth`, `Ocean`.

```yaml
biomes:
  Swamp: { raise: 4, dig: 2 }
  Mountain: { raise: 16, dig: 8 }
  AshLands: { raise: 8, dig: 4 }
```

The shipped file lists every biome commented out. A biome uncommented without its two-space indent is still applied, with a warning to indent it.

## EarthWright.Zones.yml

The admin zones, kept by the server (see [Limits and Protection](wiki:Limits and Protection)). Admins normally change them in game with `ew zone add` and `ew zone remove`; the server then rewrites this file. It may also be edited by hand on the server; it is reloaded within five seconds and sent to every player.

```yaml
zones:
  - name: spawn
    x: 0
    z: 0
    radius: 60
  - name: bobs_farm
    x: 1250
    z: -340
    radius: 40
    player: "Bob"
```

- `name`: letters, digits, `-` and `_`, at most 32, unique (a second zone of the same name is skipped).
- `x`, `z`: the world position of the centre (required).
- `radius`: 5 to 200 m (clamped); 20 m when left out.
- `player`: optional character name; the zone is then that player's.
- At most 100 zones; any beyond are dropped with a warning.

## Translation files

EarthWright's texts are English by default. A file `EarthWright.Language.<Language>.yml`, where `<Language>` is the game's language name (German, French, Spanish, Russian, Polish, Portuguese_Brazilian, Chinese, Japanese ...), replaces them key by key while the game runs in that language. Files are read from the plugin's folder and then from `BepInEx/config`, which wins.

1. `ew language write` writes `EarthWright.Language.English.yml` with every text into `BepInEx/config`.
2. Copy it to `EarthWright.Language.<Language>.yml` and translate the texts on the right. Keep the keys, the `$1` `$2` placeholders and any `<color>` tags. Leave out what you do not translate; it stays English.
3. `ew language reload` applies the file without a restart.

While the game is in English the written English file itself replaces the built-in texts, so delete it when you are done. Custom entries' texts come from `EarthWright.Entries.yml` as written.

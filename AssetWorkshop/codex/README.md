# Codex

What Valheim's own assets look like, how they are built, rigged, painted, how their effects move and how they sound,
measured from the game's files and written down so that a new asset (an item, a creature, a building piece, a rock,
a particle effect, a sound) can be made from scratch in one pass and sit beside the game's own as if its artists had
made it.

The codex is words and numbers only. The game's meshes, textures and sounds stay in the local reference export
(`%USERPROFILE%\ValheimReference`, see the workshop README) and never enter the repository or a bundle.

## Layout

```
codex/
  README.md              this file: layout, rules, the data format
  BRIEF.md               the brief every new asset fills in before it is built
  look.md                the rules that hold across everything: what makes an asset read as Valheim
  paint.md               how the game's textures are painted, per material family, measured
  palette.md             sampled colours per material family and per biome
  shaders.md             the game's shaders, their properties, which assets use which, how a mod dresses into them
  recolour.md            the game's own recolour mechanisms and the workshop's region masks
  biomes.md              each biome's palette, materials, light and fog
  models/                one page per kind of asset: weapons, tools, shields, armour, items, icons, pieces,
                         furniture, stations, environment, vegetation, locations, creatures
  rigs/                  the game's skeletons, avatars, animator controllers and clips; how to build a rig the game's
                         clips play on
  vfx/                   the game's particle effects: archetypes, measured module values, textures, shaders, lights
  sfx/                   the game's sounds: archetypes, measured loudness, length, spectrum, variations, 3D settings
  data/                  the measurements the pages are written from, as JSON; the style check reads them
  measure/               the scripts that write data/ from the reference export; re-run them after a game update
  tools/                 tools that use the codex on a new asset: stylecheck.py, recolour.py, paint_check.py (the
                         paint recipes against the game), demo_asset/ (a shield that exercises them all)
  out/                   contact sheets, renders, spectrograms made while measuring (gitignored, never committed)
```

The pipelines that build to the codex live beside it: `blender/workshop/paint.py` (paint recipes),
`blender/workshop/regions.py` (paint regions), `blender/workshop/gamerig.py` (a new body on a game skeleton),
`blender/lineup.py` (the lineup), `vfx/` (particle effects) and `sfx/` (sounds). The local skill `valheim-asset`
(`.claude/skills/valheim-asset/SKILL.md`) is the order to use them in.

## Rules

- **New assets stay out of the mods.** What is built to the codex is kept in AssetWorkshop and goes into a mod only
  when the user decides to release it there (workspace rule, `CLAUDE.md` "Always").
- **Measurements and words only.** A page may quote a game asset's path, name, size, triangle count, texture size,
  shader property, particle value, sound length, loudness or a colour sampled from it. It never embeds or copies the
  game's pixels, meshes or audio. Renders and contact sheets of game assets go to `codex/out/` (gitignored).
- **Prescriptive and concrete.** Write for the one building the asset: ranges with the game's own examples beside
  them ("two-handed axes: 188 to 3,023 triangles, 64 to 128 px, 36 to 82 px per metre; Battleaxe 578 / 64 px"), the
  parts an asset of that kind is made of, what to do and what not to do. Name the reference prefabs to line up against.
- **Seen, not guessed.** Descriptions of looks come from looking at renders and textures (render them, then read the
  PNG), and numbers come from `measure/`. Say what was measured and on how many samples.
- **Re-runnable.** Every number in `data/` comes from a script in `measure/` that runs again after a game update.

## Data format

Each `data/<topic>.json`:

```json
{
  "topic": "items",
  "generated": "2026-09-29",
  "script": "measure/items.py",
  "categories": {
    "weapon.axe_2h": {
      "label": "two-handed axes",
      "samples": [
        {"prefab": "GameElements/Items/weapons/Battleaxe.prefab", "name": "Battleaxe", "triangles": 578,
         "texture_px": 64, "texel_density": 35.6, "size_m": [0.29, 1.70, 0.40], "shader": "Creature",
         "maps": ["_MainTex", "_BumpMap", "_MetallicGlossMap"]}
      ],
      "stats": {
        "triangles":     {"n": 9, "min": 188, "p25": 400, "median": 578, "p75": 1000, "max": 1468},
        "texture_px":    {"...": "same five numbers"},
        "texel_density": {"...": "pixels per metre of surface"},
        "longest_m":     {"...": "longest side in metres, in the game"}
      },
      "references": ["GameElements/Items/weapons/Battleaxe.prefab"]
    }
  }
}
```

- Category keys are `<family>.<kind>`: `weapon.*`, `tool.*`, `shield.*`, `armour.*`, `item.*`, `piece.*`,
  `furniture.*`, `station.*`, `ammo.*`, `env.*`, `location.*`, `creature.*`, `rig.*`, `vfx.*`, `sfx.*`. A new asset
  names its key in its brief and the style check compares it with that category.
- `rig.*` (`rigs.json`: `rig.player`, `rig.humanoid`, `rig.quadruped`, `rig.flyer`, `rig.serpent`, `rig.other`) has
  skeletons as samples; the file also holds `controllers`, `clips`, `vocabulary`, `attacks` and `locomotion`.
- `stats` hold whatever a family measures (a sound has `length_s`, `loudness_lufs`; an effect `particles`,
  `lifetime_s`), always as `{n, min, p25, median, p75, max}`.
- `references` are the three to six prefabs that best show the category, for the lineup render.
- Sizes are in the game's metres after every scale on the way (a prefab's transforms, a skinned mesh's armature).

## Measuring

`measure/game.py` reads the export with plain Python (numpy and Pillow): prefabs as nodes and components (game scripts
named from `assembly_valheim.dll`), mesh headers (`mesh_stats`) and arrays (`mesh_arrays`), materials (`material`),
texture sizes and texel density. Blender (`%USERPROFILE%\tools\blender\blender.exe --background --factory-startup`)
renders game prefabs with `workshop.prefab.load` for looking at them.

```
python codex/measure/<script>.py
```

One script per topic writes `data/<topic>.json`; `creatures.py` runs before `rigs.py`, which reads
`data/creatures.json`. Scripts that render or make contact sheets write only to `codex/out/`.

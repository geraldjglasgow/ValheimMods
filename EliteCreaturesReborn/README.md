# Elite Creatures Reborn

Creatures spawn with **mutations** — named, visible traits that change how a fight goes. A Mad greydwarf comes at
you twice as fast on half the health. A Bloated draugr takes twice the killing and then explodes in your face. A
Devouring wolf eats the boars around it and grows on what it eats until it comes looking for you.

Rebuilt from scratch. This release covers mutations and stars; the rest returns over the next releases.

## Mutations

Nine, one per creature by default, each with its own colour and its own name on the nameplate.

| Mutation | What it does |
| --- | --- |
| Mad | Far faster, half health |
| Bloated | Double health, explodes a second after it dies |
| Cloaked | Invisible beyond 6 metres |
| Splintering | Splits into two weaker copies when killed, which can split again |
| Leeching | Regenerates, and heals from the damage it deals |
| Warding | Reflects damage and knocks you back |
| Plated | Armoured while healthy, hits harder as that armour goes |
| Miasmic | Trails poison clouds; poisons players, never creatures |
| Devouring | Kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you |

## Stars

Beyond vanilla's two, counted in fives on the nameplate: a small star is one, a large star is five. A mutation on a
large star is stronger. Star chances and mutation chances are set per biome, so the Meadows stay the Meadows.

## Configuration

Two files, both written and documented on first run, both hot-reloaded while you play:

- `BepInEx/config/gglasgow.elitecreaturesreborn.cfg` — the settings: star chances, mutation chances and mutation
  strength per biome, colours, nameplate distance, and an off switch for everything.
- `BepInEx/config/creature_rules.yml` — the rule file, for anything too structured for a flat settings file.

A server binds connected players to its own rules. Display preferences — colours, tint strength, nameplate
distance, whether trait names show at all — stay with each player and are never locked.

## Console commands

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns exactly that creature, bypassing every roll, for testing |
| `elite inspect` | Prints the resolved stars, mutations and numbers for the creature under your crosshair |
| `elite purge` | Removes the loaded creatures this mod has marked, with no drops |
| `elite effects <text>` | Lists loaded effect prefabs matching the text and plays one, for building visuals |

## Install

With a mod manager, install it. By hand, drop `EliteCreaturesReborn.dll` into `BepInEx/plugins`. Requires
BepInEx. On a server, install it on the server and on every client.

## Multiplayer

Creature state travels in the world data the game already shares, so every player sees the same creature with the
same traits. Install it on the dedicated server as well as the clients.

## Files

- `plugins/EliteCreaturesReborn.dll` — the mod, a single merged assembly
- `config/gglasgow.elitecreaturesreborn.cfg` — settings
- `config/creature_rules.yml` — creature rules

## Building

.NET SDK 8, with Valheim installed:

```
dotnet build EliteCreaturesReborn/EliteCreaturesReborn.csproj -c Release
```

Override `-p:GamePath=...`, `-p:BepInExCore=...` or `-p:ModLibsPath=...` if your layout differs. The build merges
the shared libraries into one DLL and writes it to `dist/`.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you
distribute that is built from it must carry the same freedoms and be released under the same licence, with source.
See the `LICENSE` file for the full terms.

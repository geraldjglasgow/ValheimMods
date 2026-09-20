# Elite Creatures Reborn

Creatures spawn with **mutations** — named, visible traits that change how a fight goes. A Mad greydwarf comes at
you twice as fast on half the health. A Bloated draugr takes twice the killing and then explodes in your face. A
Devouring wolf eats the boars around it and grows on what it eats until it comes looking for you.

Rebuilt from scratch. This release covers mutations and stars; the rest returns over the next releases.

## Mutations

Ten, one per creature by default, each with its own colour and its own name on the nameplate.

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
| Thieving | Steals an item on its first landed hit and carries it on its nameplate; kill it to get everything back |

## Mutation power fields

`mutation power` in the rule file sets how strong each mutation is, in named fields rather than positional
numbers. A field marked (enhanced) is multiplied by `large star power` on a large star; a cost field never is.
Any mutation can also be switched off entirely with `mutations enabled`, regardless of its chance curves.

- **Mad** - `move`/`attack speed` multipliers (enhanced); `health` multiplier, its cost
- **Bloated** - `health` multiplier (enhanced); `delay` seconds from death to the blast, which goes off at the
  corpse's resting place; `damage` (enhanced) and `radius` (enhanced) of the blast; `blast effect`/`warning effect`
  vanilla prefabs for the explosion and the pre-blast warning
- **Cloaked** - `reveal distance` metres to become visible (enhanced); `fade time` seconds to phase, 0 snaps;
  `fade margin` extra metres before fading back out, to stop strobing
- **Splintering** - `damage` multiplier per split; `max generations` cascade-depth cap, 0 = unlimited;
  `max descendants` live-descendant cap, 0 = unlimited
- **Leeching** - `regen` percent max health per second, never enhanced; `regen cap` hard HP/s ceiling on a single
  tick, so a huge-health creature cannot out-heal a fight; `combat cooldown` seconds since its last damage taken
  before regen resumes; `lifesteal` percent of damage dealt returned as health (enhanced)
- **Warding** - `reflect` percent of incoming damage returned (enhanced); `knockback` force on a melee attacker
  (enhanced)
- **Plated** - `armour` percent of incoming damage cut at full health, to 0 hurt (enhanced); `max reduction` hard
  ceiling on that percent, so enhancement cannot approach invulnerability; `damage` percent bonus at zero health,
  to 0 full (enhanced)
- **Miasmic** - `cloud life` seconds a dropped cloud lasts; `cloud damage` strength of the vanilla Poison a cloud
  or hit applies, not direct damage (enhanced); `clouds per second` while moving (enhanced); `cloud radius` metres;
  `cloud effect`/`body effect` vanilla prefabs for the trail cloud and the permanent worn poison look (cosmetic)
- **Devouring** - `absorb health`/`absorb damage` percent kept permanently from a victim (enhanced; an instant
  kill always lands the killing blow, so the full amount is kept); `slow per 100 health`, its cost;
  `move` base speed multiplier before the slow (`0.5` halves it; not enhanced);
  `player threshold` fraction of a player's max health a hit must pass before it hunts players for good;
  `devour cooldown` seconds after a meal before it can eat again
- **Thieving** - `max items` the most it will ever hold (enhanced), hard-capped at 8; never takes equipped gear
  or more than one item per landed hit, and gives back everything it holds when it is killed

## Stars

Beyond vanilla's two, counted in fives on the nameplate: a small star is one, a large star is five. A mutation on a
large star is stronger. Star chances and mutation chances are set per biome, so the Meadows stay the Meadows.

## Loot

What a kill drops is governed by a `loot:` block in the rule file. Four modes, chosen for the whole world:
**Vanilla** (drops untouched), **Scaled** (quantities raised by the per-star `drops` line), **Rolled** (the
creature's own drop table rolled once more per star, each roll independent — the default, so a hard fight has a
real chance at the rare thing), and **Curated** (per-creature rules decide everything). An `extra roll chance`
line and a `max extra rolls` cap tune Rolled; a global multiplier and a separate boss multiplier scale everything
after the mode. Trophies are never multiplied unless you switch that on — one kill, one trophy.

Per-creature rules in the same file, matched by prefab name, override any of it: a creature's own drops line,
adjusted or removed rows of its drop table, extra drops with their own chance and amounts. `elite reference`
writes `creature_reference.yml` with every creature the game knows — modded ones included — under its exact
prefab name with its vanilla drop table; paste it and `creature_rules.yml` at an AI assistant, describe the
economy you want, and drop the rules it writes back into the file.

## Configuration

Two files, both written and documented on first run, both hot-reloaded while you play:

- `BepInEx/config/gglasgow.elitecreaturesreborn.cfg` — the settings: star chances, mutation chances and mutation
  strength per biome, colours, nameplate distance, and an off switch for everything.
- `BepInEx/config/creature_rules.yml` — the rule file, for anything too structured for a flat settings file,
  including a `mutations enabled` switch that turns any mutation off everywhere.

A server binds connected players to its own rules. Display preferences — colours, tint strength, nameplate
distance, whether trait names show at all — stay with each player and are never locked.

## Console commands

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns exactly that creature, bypassing every roll, for testing |
| `elite inspect` | Prints the resolved stars, mutations and numbers for the creature under your crosshair |
| `elite purge` | Removes the loaded creatures this mod has marked, with no drops (a Thieving creature's stolen goods drop first) |
| `elite effects <text>` | Lists loaded effect prefabs matching the text and plays one, for building visuals |
| `elite reference` | Writes `creature_reference.yml`: every creature the game knows, by biome, with its drop table |

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

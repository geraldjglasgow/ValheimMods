# Elite Creatures Reborn

Creatures spawn with more stars and with mutations, named traits that change how a fight goes. Bosses take an
aspect instead.

## Features
- Mutations: thirteen, shown on the nameplate, such as Mad, Bloated, Devouring and Thieving.
- Difficulty: Easy to Extreme in one line; new biomes start gentle and cleared ones keep hardening.
- Stars: beyond vanilla's two, counted in fives; up to eight on Extreme.
- Boss aspects: one modifier per fight (Twin, Phantom and eleven more), shown at the altar; harder ones pay more.
- World tiers: each boss's first defeat makes stars and mutations more common everywhere.
- Breeding: tamed creatures pass a mutation and stars on to their young.
- Loot: Vanilla, Scaled, Rolled or Curated drops, and drop rules per creature.
- Boss damage board: who hurt the boss and how much; `/damage` shows it again.
- Respawning: cleared camps, dungeons and dungeon chests can refill (off by default).
- Works with PackPanel, GrindstoneSkills' Husbandry and Elite Creatures Pack.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put
`EliteCreaturesReborn.dll` in `BepInEx/plugins`. Remove any other creature star mod and its configs first: this mod
reads neither.

## Configuration
`BepInEx/config/gglasgow.elitecreaturesreborn.cfg` (each player's display) and `BepInEx/config/creature_rules.yml`
(the rules, an off switch per feature). Both apply without a restart; the server's rules bind every player. Console:
`elite tier` and `damage` for everyone, the other `elite` commands for admins. Every mutation, aspect, rule field and
command in detail: [reference](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCreaturesReborn/CLAUDE.md).

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

# Elite Creatures Reborn

Creatures spawn with more stars and mutations, traits that change how a fight goes; bosses take an aspect instead.

## Features
- Difficulty: Easy to Extreme; new biomes start gentle, cleared ones harden.
- Stars: up to eight on Extreme, or off for another mod's.
- World tiers: each first boss defeat raises stars and mutations.
- Mutations: one per creature, named on the nameplate (below).
- Boss aspects: one per fight (below), or chosen at the altar with E; bosses gain stars each defeat.
- Taming: young inherit mutations and stars.
- Loot: four drop modes, per-creature rules; bosses pay every player.
- Boss damage board: who did how much.
- Boss hints: pointers to traders and the forge.
- Compendium: in-game pages on it all.
- Death recap: `F10` replays the seconds before each death.
- Respawning (off by default): cleared camps, dungeons and chests refill.
- Horde (off by default): more, weaker creatures in the wild.
- Raids: stake gold or trophies in the Raiders Chest, call a raid, win more.
- Rendering Table: trophies into ores or ingredients of their biome; boss trophies into Forge idols.
- Hel's Arena (off by default): through Helgrind, a biome's trial or Infinite; a boss every fifth wave, rewards by progress. Subject to change.
- Works with PackPanel, GrindstoneSkills and Elite Creatures Pack.

## Mutations
- Mad: far faster, half health.
- Bloated: double health, explodes after it dies.
- Cloaked: invisible beyond 10 m.
- Splintering: splits into weaker copies when killed.
- Leeching: regenerates and heals by hitting.
- Warding: reflects hits and knocks you back.
- Plated: armoured while healthy, hitting harder as it breaks.
- Miasmic: trails poison clouds.
- Devouring: eats weaker creatures and grows stronger.
- Thieving: steals items; kill it to get them back.
- Gilded: runs from you; triple loot.
- Blinking: reappears behind you.
- Relentless: chases you up to 150 m, sneaking or not.
- Juggernaut: never staggers.
- Screecher: shrieks when hit hard, deafening you and stopping magic.
- Frostbound: chilling aura, slippery ice trails; frost heals it.
- Mudbound: leaves slowing mud.
- Corrodent: wears armour down three times as fast, plus 4% of the piece per hit.
- Cloning: leaves a decoy and fights on unseen.
- Flamebound: leaves burning ground.
- Piercing, Howling, Binding (off by default): ignores 15% of armour; calls nearby creatures; roots you.

## Boss aspects
Each pays extra loot.
- Reflective: part of each hit comes back to you.
- Shielded: takes less damage from arrows and bolts.
- Mending: regenerates during the fight.
- Summoner: calls its biome's creatures as it loses health.
- Elementalist / Enraged: more elemental / physical damage.
- Twin: two bosses sharing one health pool.
- Tethered: two linked bosses, faster as their health drifts apart.
- Phantom: splits into copies and hides among them.
- Adaptive: resists the damage type hitting it most.
- Fixated: marks one player and hits them harder.
- Stormbound: lightning strikes under every player.
- Gravitic: drags everyone in, then slams.
- Colossal: bigger, tougher, with knockdown shockwaves.
- Brutal: heavy blows throw you 20 m.
- Nightfall: a stormy night with hunting tornadoes.
- Echoing: a ghost repeats its moves later.
- Portalbound: the Elder and Bonemass throw through portals.
- Bountiful: two more aspects, and all their loot.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put
`EliteCreaturesReborn.dll` in `BepInEx/plugins`. Remove other star mods, or set `Creature stars` to false.

## Configuration
`BepInEx/config/gglasgow.elitecreaturesreborn.cfg` (every switch, also in a configuration
manager) and `BepInEx/config/creature_rules.yml` (the tables). Both apply without a restart; the server's bind every player.
Console: `elite tier`, `damage`, `deaths` for everyone; other `elite` commands for admins. Details:
[reference](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCreaturesReborn/CLAUDE.md).
Translate: save a copy of [English.yml](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCreaturesReborn/EliteCreaturesReborn/translations/English.yml) as `EliteCreaturesReborn.translations.<Language>.yml` in `BepInEx/config`.

## Links
Discord: https://discord.gg/Q2sMPGQ4T3

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

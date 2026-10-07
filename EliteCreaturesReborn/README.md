# Elite Creatures Reborn

Creatures spawn with more stars and mutations, traits that change how a fight goes; bosses take an aspect instead.

## Features
- Difficulty: Easy to Extreme; new biomes start gentle, cleared ones keep hardening.
- Stars: beyond vanilla's two, up to eight on Extreme, or off for another mod's.
- World tiers: each boss's first defeat makes stars and mutations more common.
- Mutations: one per creature, named and coloured on the nameplate (below).
- Boss aspects: one per fight, shown at the altar with the boss's stars (below).
- Breeding: tamed young inherit mutations and stars.
- Loot: four drop modes, rules per creature; bosses drop a trophy for every player.
- Boss damage board: who did how much damage.
- Boss hints: four bosses point the way to the Bog Witch, the ancient forge, Haldor and Hildir until found.
- Death recap: `F10` replays the seconds before each death, hit by hit.
- Respawning: cleared camps, dungeons and dungeon chests can refill (off by default).
- Works with PackPanel, GrindstoneSkills' Husbandry and Elite Creatures Pack.

## Mutations
- Mad: far faster, half health.
- Bloated: double health, explodes after it dies.
- Cloaked: invisible until you are within 10 m.
- Splintering: splits into two weaker copies when killed.
- Leeching: regenerates and heals from the damage it deals.
- Warding: reflects part of each hit and knocks you back.
- Plated: armoured while healthy, hits harder as the armour breaks.
- Miasmic: trails poison clouds.
- Devouring: eats weaker creatures and keeps their health and damage.
- Thieving: steals an item with each hit; kill it to get them back.
- Gilded: runs from you; drops triple loot and a purse of coins.
- Blinking: reappears behind you every 30 seconds.
- Relentless: chases until you are 150 m away; sneaking does not hide you.
- Juggernaut: never staggers or is knocked back.
- Screecher: a big hit makes it shriek, deafening you and stopping magic.
- Frostbound: a chilling aura and slippery ice trails; frost heals it.
- Mudbound: leaves slowing mud where it walks.
- Corrodent: wears your armour down three times as fast.
- Cloning: leaves a harmless decoy and fights on unseen.

## Boss aspects
The altar shows the aspect before you offer and shifts every 15 seconds. About one fight in five has none; every
aspect pays extra loot.
- Reflective: part of each hit comes back to you.
- Shielded: takes less damage from arrows and bolts.
- Mending: regenerates during the fight.
- Summoner: calls creatures of its biome as it loses health.
- Elementalist / Enraged: more elemental / physical damage.
- Twin: two bosses sharing one health pool.
- Tethered: two linked bosses, faster the further apart their health is.
- Phantom: splits into copies and hides among them.
- Adaptive: resists whichever damage type hits it most.
- Fixated: marks one player and hits them harder.
- Stormbound: lightning strikes circles under every player.
- Gravitic: drags everyone in, then slams.
- Colossal: bigger and tougher; its shockwaves knock you down.
- Brutal: heavy blows throw you 20 m.
- Nightfall: a storming night with tornadoes that hunt you.
- Echoing: a ghost of the boss repeats its every move and blow 15 seconds later.
- Portalbound: the Elder and Bonemass throw through portals.
- Bountiful: two more aspects at once, and all their loot.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put
`EliteCreaturesReborn.dll` in `BepInEx/plugins`. Remove other star mods, or set `creature stars: false`: their
configs are not read.

## Configuration
`BepInEx/config/gglasgow.elitecreaturesreborn.cfg` (each player's own settings) and
`BepInEx/config/creature_rules.yml` (the rules). Both apply without a restart; the server's rules bind every player.
Console: `elite tier`, `damage` and `deaths` for everyone, the other `elite` commands for admins. In detail:
[reference](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCreaturesReborn/CLAUDE.md).

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

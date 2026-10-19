# GrindstoneSkills

Deeper skills for Valheim: every one of the game's skills does more, and four new skills join them.

## Features
- Skills: the game's keep your levels; perks grow from nothing at 0 to full at 100 (below).
- Milestones: abilities that unlock at set levels (below).
- Finds and snags: felled trees, broken rock and fishing lines can hide loot.
- Skill book: the Skills window shows each skill's page at your level.
- Skill loss on death: configurable for every skill; Catch Up trains a skill twice as fast back to its best.
- Icons: every skill has its own.
- Works with Elite Creatures Reborn, FeastMaster, OpenKeep and ShipConfig.

## Skills
- Cooking: faster cooking and fermenting, slower burning, extra dishes and ingredients back.
- Sailing (new): tougher ships you build, faster ships you steer, a wider map reveal aboard.
- Woodcutting: trees fall where you aim, domino felling, clean splits, more wood from old growth, replanting.
- Pickaxes: gold seams for clean strikes, rich veins of up to three stars, extra ore, splash damage.
- Foraging (new): extra berries, mushrooms and herbs from wild picks.
- Fishing: a tension fight on the line, more bites, bigger fish, chum, snags and an angler's log.
- Husbandry (new): faster taming and breeding, starred young, twins, produce, extra honey, tougher wolves.
- Defense (new): more health, less damage, regeneration, poise, longer parries; shield Reflex, Bash and Thorns.
- Farming: faster growth, closer planting, bonus crops, seeds back, giant crops, tending and a compost bin.
- Running: more carry weight, half the overloaded drain.
- Jumping: cheaper jumps, safer and softer falls.
- Sneaking: faster, quieter crouching and stronger backstabs.
- Swimming: dry off faster, longer breath, a wider pickup in water.
- Dodging: longer invulnerable rolls.
- Riding: a faster gallop and a tougher mount.
- Crafting: sturdier crafted gear and built pieces, a chance of a material back.
- Weapons: a skill behind your best weapon skill trains faster; a trait per weapon at 50.

## Milestones
- Sailing 25 and 50: Wind Call (`K` at the helm turns the wind), Lookout (`O` shows enemy name tags).
- Pickaxes 25 to 100: Read the Rock (a deposit's stars), Echo (points to ore), Unbroken (clean-strike streaks).
- Foraging 25: Sweep picks the same plants around you.
- Fishing 25 to 75: Species, Size and Water Sense name the fish; Legendary fish; Grace keeps a tired line.
- Husbandry 20 to 50: Animal Lore timers, the Animal Feeder, Calm while taming.
- Defense 25 to 100: Riposte, Shield Wall, Hardened, Last Stand (a killing blow leaves you at 1 health).
- Farming 25 to 100: Rows of three then five, Auto Replant, Heat and Cold Tolerance for crops.
- Running 75: Second Wind when you run dry. Jumping 50: a Roll on hard landings.
- Sneaking 75: Vanish from hunters. Dodging 50 and 75: Counter, Heavy Roll. Crafting 75: Journeyman stations.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `GrindstoneSkills.dll`
in `BepInEx/plugins`. Removing the mod forgets the new skills' levels; empty and take down Animal Feeders and
Compost bins first.

## Configuration
`BepInEx/config/com.GrindstoneSkills.cfg`, plus `GrindstoneSkills.Finds.yml`, `GrindstoneSkills.MineFinds.yml`,
`GrindstoneSkills.Forage.yml` and `GrindstoneSkills.Snags.yml` beside it. Every setting is described in its file and
applies without a restart; the server's values bind every player. Console: `fishlog` lists your catches.
Translate: save a copy of [English.yml](https://github.com/geraldjglasgow/ValheimMods/blob/main/GrindstoneSkills/GrindstoneSkills/translations/English.yml) as `GrindstoneSkills.translations.<Language>.yml` in `BepInEx/config`.

## Links
Discord: https://discord.gg/Q2sMPGQ4T3

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

# GrindstoneSkills

Deeper skills for Valheim: five of the game's skills do more, and four new skills join them.

## Features
- Skills: the game's keep your levels; perks grow from nothing at 0 to full at 100 (below).
- Milestones: abilities that unlock at set levels (below).
- Finds and snags: felled trees, broken rock and fishing lines can hide loot.
- Skill book: the Skills window shows each skill's page at your level.
- Skill loss on death: configurable for every skill.
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

## Milestones
- Wind Call (Sailing 25): `K` at the helm turns the wind to blow where you look.
- Lookout (Sailing 50): `O` aboard shows enemy name tags around the ship.
- Read the rock (Pickaxes 25): an ore deposit shows its vein stars and chunks left.
- Echo (Pickaxes 50): a swing on rock points to the nearest ore deposit.
- Unbroken (Pickaxes 100): clean strikes in a row hit harder.
- Sweep (Foraging 25): picking a plant also picks the same kind around it.
- Species, Size and Water Sense (Fishing 25 to 75): nibbles and casts name the fish and its size.
- Legendary fish (Fishing 50): glowing giants take your bait.
- Grace (Fishing 75): running out of stamina no longer loses the fish at once.
- Animal Lore (Husbandry 20): hovers show feeding, breeding, growing and hatching timers.
- Animal Feeder (Husbandry 25): a feeder hungry tamed animals eat from.
- Calm (Husbandry 50): a creature you are taming neither flees from nor attacks you.
- Riposte (Defense 25): after a parry, your next melee attack deals more damage.
- Shield Wall (Defense 50): players behind your raised shield take less damage.
- Hardened (Defense 75): each hit that hurts you adds damage reduction.
- Last Stand (Defense 100): a killing blow leaves you at 1 health instead.
- Rows (Farming 25 and 50): a seed plants a row of three, then five.
- Auto Replant (Farming 50): picking a crop replants it from your seeds.
- Heat and Cold Tolerance (Farming 75 and 100): crops grow in the Ashlands, then the Mountain and Deep North.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `GrindstoneSkills.dll`
in `BepInEx/plugins`. Removing the mod forgets the new skills' levels; empty and take down Animal Feeders and
Compost bins first.

## Configuration
`BepInEx/config/com.GrindstoneSkills.cfg`, plus `GrindstoneSkills.Finds.yml`, `GrindstoneSkills.MineFinds.yml`,
`GrindstoneSkills.Forage.yml` and `GrindstoneSkills.Snags.yml` beside it. Every setting is described in its file and
applies without a restart; the server's values bind every player. Console: `fishlog` lists your catches.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

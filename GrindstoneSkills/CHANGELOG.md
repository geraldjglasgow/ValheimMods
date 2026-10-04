# Changelog

## 0.13.2

- Sailing: Wind Call (K) works only while you steer a ship; elsewhere the key does nothing.
- Less work every frame keeping the config in sync with the server.

## 0.13.1

- Less work every frame reading the hotkeys.

## 0.13.0

- Removed: stars on dishes, wild picks, crops and seeds. Nothing rolls a star any more, so items stack like vanilla.
- Giant crops are no longer 3-star. The star settings stay in the config but do nothing.
- Items that are already starred keep their stars.

## 0.12.1

- Fixed: no more "LiberationSans SDF Font Asset was not found" warnings from the skill book and the fishing tension bar.

## 0.12.0

- Sailing: Wind Call (level 25): K aboard turns the ship's wind to blow where you look, for 60 s.
- Cooldown 3 minutes; settings in the new section `35 - Wind Call`, `Wind Call Key` per player.
- The Lookout key now fires while you hold W, and never while you type in chat.

## 0.11.0

- Changed: Riposte no longer staggers what it hits; its 25% bonus damage (`Riposte Damage`) stays.

## 0.10.0

- New: a skill book. Click a skill to see what it does at your level, perks and milestones included.
- Hover a name or gold word for what it does; the list's old tooltips are gone.
- Removed: the Defense plate and `Show Plate`; Defense's page shows the same.
- Pickaxes: seams open within reach, stay open longer and glint softer.
- Old configs keep the old seam times: set `Seam Window At 0` and `Seam Window At 100` to 2.6 and 5.6.
- Fixed: copper and silver deposits could count as plain stone, and boulders as ore.
- New skill icons, closer to the game's own.

## 0.9.1

- Woodcutting and Pickaxes have their own skill icons.

## 0.9.0

- New: Fishing, on the game's own skill, so existing levels count.
- Reeling while a fish thrashes strains the line until it snaps; fish tire after four thrashes.
- Longer strike window; a perfect strike skips the first thrash.
- From level 75, running out of stamina no longer loses the fish at once.
- More bites with level, at dawn, dusk and in rain; starred bait and chum help.
- Fish can grow to level 5 on the hook; one in 200 is legendary, for anglers of 50+.
- Nibbles name the fish and its size; casts read the water.
- An angler's log keeps your catches and records; `/fishlog` lists it.
- More bonus items, bait back, longer casts and line, and snags (`GrindstoneSkills.Snags.yml`).
- Bigger fish clean into better-starred raw fish.
- Reeling an empty line no longer trains Fishing. Settings in sections 50 to 54; own skill icon.

## 0.8.2

- Cooking and Sailing have their own skill icons.

## 0.8.1

- Farming has its own skill icon.

## 0.8.0

- New: Farming. The planter's level decides how crops grow, the picker's level the harvest.
- Crops ripen with 0 to 3 stars, raised by starred seeds and companion crops; a few grow giant.
- Faster growth (more in rain), closer rows, and daily tending.
- From level 75 crops grow unshielded in the Ashlands, at 100 in the Mountains and Deep North.
- Bonus crops, seeds back, row planting from level 25 and auto-replant from 50.
- The almanac shows when a plant ripens and, from level 20, its star odds.
- The windmill keeps stars. New compost bin: feeds nearby crops for faster growth and better stars.
- Settings in sections 29 to 32. Changed: 0★ crops and flour now lower a dish's ingredient stars.

## 0.7.0

- New: Husbandry, a skill of its own for taming, breeding and keeping animals.
- Faster taming, longer fed time, and from level 50 animals being tamed neither flee nor attack you.
- Faster breeding, bigger herds, stronger young, twins and faster growing up.
- More butcher drops, produce from living animals, extra honey, starred eggs, optional Prime cuts.
- Petting makes animals content and breed faster; a following wolf hits harder and takes less damage.
- Animal Feeder from level 25; Animal lore on hover from level 20; optional taming levels.
- Elite Creatures Reborn 3.10.0 or later: stronger young get a star there too.
- `raiseskill` and `resetskill` work for Husbandry. Settings in sections 23 to 28.

## 0.6.0

- New: Defense, a skill of its own, trained by blocking hits and taking them.
- At level 100: +25 max health, more food health, 10% less damage and regeneration out of combat.
- More poise, a wider parry window, and blocks and dodges 10% cheaper.
- Guard perks: Reflex, Shield Bash, Thorns, more adrenaline, less shield wear and knockback, Desperation.
- Milestones: Riposte at 25, Shield Wall at 50, Hardened at 75, Last Stand at 100.
- A Defense plate shows your damage reduction; milestones show as status icons.
- `raiseskill` and `resetskill` work for Defense. Settings in sections 19 to 22.

## 0.5.0

- New: Foraging, a skill of its own for wild picks, which used to train Farming.
- Berries, mushrooms and herbs come off with 0 to 3 stars, for the kitchen and eating raw.
- Each plant has a best time to pick (dry, rain, night) for better stars; its hover says when.
- Up to 50% extra yield, and sweep picking from level 25.
- What counts as foraging is in `GrindstoneSkills.Forage.yml`, open to other mods' items.
- Changed: 0★ berries and mushrooms now count toward a dish's ingredient stars.
- `raiseskill` and `resetskill` work for Foraging.

## 0.4.0

- New: Pickaxes, on the game's own skill, so existing levels count.
- Seams: hit a glinting chunk in time for double damage and, on ore, an extra drop.
- Clean strikes chain; from level 100 (Unbroken) each hits harder, up to ×4.
- Splash damages neighbouring chunks. Rich veins: ore deposits with 0 to 3 stars drop more.
- Read the rock (25) shows a deposit's stars; Echo (50) points to the nearest ore deposit.
- Extra ore and less pickaxe wear. Broken rock can hold amber, amber pearls and rubies
  (`GrindstoneSkills.MineFinds.yml`).
- More experience in later biomes and on ore.

## 0.3.0

- New: Woodcutting, on the game's own skill, so existing levels count.
- Timber!: trees fall away from where you chop; your own logs hurt you less.
- Domino felling: a falling log can knock over the next tree, up to five deep.
- Clean splits and old growth give more wood. Falling trees can hide finds (`GrindstoneSkills.Finds.yml`).
- Swings give back stamina and wear the axe less; the stump comes out; a sapling may take root.
- More experience for harder wood, felled trees and broken logs. New store icon.

## 0.2.0

- New skill: Sailing, trained by steering a ship (the crew earn a share).
- Ships you build get up to 50% more health; ships you steer go up to 20% faster.
- Up to twice the map reveal radius aboard a ship.
- Lookout (level 50): O aboard sends a 100 m pulse; everyone aboard sees enemy name tags for 30 s.
- `raiseskill` and `resetskill` know Sailing.

## 0.1.0

- First version: Cooking, on the game's own skill, so existing levels count.
- Dishes get 0 to 3 stars from the cook's level; meads keep their base's stars; starred ingredients help.
- Stars boost a dish's health, stamina and eitr and make it last longer.
- Trash filter: Shift+E on a kitchen sets the fewest stars it keeps.
- Kitchen perks: faster cooking and fermenting, slower burning, extra food, a chance to keep an ingredient.
- More experience for richer dishes and the first time you make each.
- Skill loss on death is configurable for every skill.

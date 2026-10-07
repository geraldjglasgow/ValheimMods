# FeastMaster

Configure every food and mead, and how health, stamina and eitr regenerate and drain. Every setting starts at the
game's own value, and only what you change is patched, so it sits beside other mods that tune stamina, fishing or
skills.

## Features
- Foods and meads, modded ones too: every value, per item or for all at once (below).
- Food slots: one to five foods active at once, all on the HUD.
- Auto eat: a food that runs out is replaced from your inventory, if the server allows.
- Regeneration: health, stamina and eitr recovery reshaped (below).
- Stamina: every cost and the body's base values (below).
- Kitchen: cook and brew times, burning and feast servings (below).
- HUD: hide the health, stamina or eitr number and the food timers, per player.

## Foods and meads
- Global multipliers: health, stamina, duration, healing and eitr of every food at once.
- Foods: health, stamina, eitr, duration and healing each, shown on the tooltip.
- Meads: duration (also the cooldown), what they restore, regen boosts, run and jump costs.
- Degradation: food keeps full strength, or fades on a curve you pick.
- Eating again: how soon a food can be eaten again.

## Regeneration
- Continuous healing: food heals smoothly instead of every 10 seconds.
- Vigor: food stamina also speeds stamina regeneration; Eitr Vigor does the same for eitr.
- Extra stamina: regeneration grows with stamina above your base; use this or Vigor, not both.
- Regen curve: refill faster on an empty bar and slower on a full one, or the reverse.
- Basics: regen speed, the low-bar bonus, the pause after use, the slowdown while blocking.
- Sneaking, encumbered, swimming: crouch still to recover faster; recover while overloaded or in the water.
- Rested: how long it lasts and how much faster you recover.

## Stamina and body
- Costs: running, jumping, dodging, blocking, attacks, sneaking, swimming, tools, fishing and the harpoon.
- Out of combat: cheaper running, jumping, dodging and sneaking when no enemy is on you.
- Free sneaking: no drain while no enemy is near.
- Skill discount: high Blocking, Dodge and Jump skills cut their costs.
- Base values: health and stamina with no food.
- Skills: Run, Jump, Sneak, Swim and Fishing add stamina and learn faster or slower.
- Drowning and world rates: drowning damage; the world's food, stamina and regen rates.

## Kitchen and feasts
- Cook times: per station and recipe, or one multiplier for all, food on the fire included.
- Burning: food left on the fire can stay cooked until taken.
- Fermenter: brew time and meads per batch, barrels already brewing included.
- Feasts: servings per placed feast; each feast's food has its own values.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `FeastMaster.dll` in
`BepInEx/plugins`. Another mod that changes food slots conflicts with `Food Slots`; use one.

## Configuration
`BepInEx/config/com.FeastMaster.cfg`. Every setting is described in the file and applies without a restart; the
server's values bind every player, display settings stay your own. Console: `charter status` shows whether the server
binds your settings.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

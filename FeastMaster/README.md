# FeastMaster

Configure every food and mead in Valheim, and how stamina and eitr regenerate and drain. Untouched until you change
a setting: only what you change is patched, so it sits beside other mods that tune stamina, fishing or skills.

## Features
- Foods and meads, modded ones too: global multipliers and a section per item, shown on tooltips.
- Degradation: off or a fade curve; set when food can be eaten again.
- Food slots: one to five; auto eat replaces a food that runs out, if the server allows.
- Kitchen: fermenter time and yield, cook times, food that never burns, feast servings.
- Recovery: continuous food healing, Vigor (regeneration from food), regen rules and curves, Rested.
- Vigor and `Regen Per Extra Stamina Point` both reward food stamina; use one, not both.
- Stamina costs: a multiplier per drain, cheaper out of combat, free sneaking, skill discounts.
- Base health and stamina, stamina and gain from skills, drowning damage, world rates.
- HUD: hide the health, stamina or eitr number and food timers, per player.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `FeastMaster.dll` in
`BepInEx/plugins`. A mod that also changes the number of food slots conflicts with `Food Slots`; use one of them.

## Configuration
`BepInEx/config/com.FeastMaster.cfg`. Every setting is described in the file and applies without a restart; the
server's values bind every player, display settings stay your own. Console: `charter status` shows whether the server
binds your settings.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

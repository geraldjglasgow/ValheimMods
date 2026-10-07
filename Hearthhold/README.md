# Hearthhold

Stardew Valley style quality for Valheim's food. What you gather, harvest, hunt and cook comes plain or with one, two
or three stars (bronze, silver, gold). Better ingredients and a better cook make better food, and starred food is
worth more when you eat it, sell it or give it away.

Requires GrindstoneSkills: its Foraging, Husbandry, Farming, Fishing and Cooking levels decide the stars.

## Features
- Stars on wild picks, honey and sap (Foraging), crops (Farming), meat (Husbandry and the creature's own stars) and
  fish (the catch's size, when it is cleaned).
- Dishes and meads roll their stars from the cook's Cooking level and the stars of the ingredients; your best
  ingredients go in first.
- Meads keep their base's stars from the fermenter.
- Eating: each star adds 10% health, stamina and eitr and makes the food last 10% longer.
- Meads: each star makes healing and restoring meads stronger and lasting meads last longer.
- Stars are shown on icons, tooltips and items on the ground in bronze, silver and gold, and starred items stack
  apart.
- Everything is decided on the server or the object's owner, so it works the same for everyone in multiplayer.

## Content
- Daily Fortune: each day the Norns grant Ill-omened, Poor, Fair, Good or Blessed fortune, the same for everyone,
  nudging every star roll. Type `fortune` in the console to see today's.
- Professions at level 100: Botanist (wild picks always gold), Tiller (crops at least silver), Rancher (meat at least
  silver), Gourmet (dishes and meads at least silver), Angler (cleaned fish at least silver).
- Shipping Crate: a chest that buys your starred goods at dawn for coins; more stars pay more.
- Trader friendship: give Haldor, Hildir or the Bog Witch a starred dish once a day for up to ten hearts and a
  discount of up to 10%.
- Aging Cask: meads and wines left in it gain a star every two days, up to gold.

## Possible later feature
- Bundle Hall: a hall of bundles to fill with starred goods ("Meadows forage: bronze raspberries, mushrooms and
  dandelions"), each finished bundle unlocking a lasting reward. Not in the mod yet.

## Install
Needed on the server and every client, with GrindstoneSkills. Install with r2modman or the Thunderstore app, or put
`Hearthhold.dll` in `BepInEx/plugins`.

## Configuration
`BepInEx/config/com.Hearthhold.cfg` holds three switches: Daily Fortune, Shipping Crate and Trader Friendship. They
apply without a restart, and the server's values bind every player (`Lock Configuration`).

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- ConcernedApe, for Stardew Valley, which inspired this mod.
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

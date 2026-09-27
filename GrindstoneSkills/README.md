# GrindstoneSkills

Deeper skills for Valheim, starting with Cooking. A good cook makes better food: every dish comes out with 0 to 3
stars, stars make food stronger and last longer, and every kitchen can throw away dishes below the stars you want.
Built on the game's own Cooking skill, so the levels you already have count.

### Features

- **Stars on dishes.** A dish's stars are rolled from the cook's Cooking level when it finishes: on a cooking station
  or the oven when it is done, at the cauldron, mead cauldron or prep table when it is crafted. 0 stars is the
  vanilla dish; every dish you already own is 0 stars.
- **Stars make food better.** By default a 1★ dish gives 10% more health, stamina and eitr and lasts 10% longer, 2★
  gives 20% and 20%, 3★ gives 35% and 30%. A dish stays at full strength for its extra time, then fades as usual.
- **Better cooks, better odds.** At level 0 almost everything is 0★; at level 100 half of all dishes are 3★. The odds
  table is fully configurable.
- **Starred ingredients help.** A stew made from 3★ meat has better odds than one made from plain meat; above level
  100 only starred ingredients raise the odds further. You choose whether recipes use your lowest or highest stars
  first (lowest by default, so your best food is kept).
- **Meads keep their stars.** A mead base's stars pass through the fermenter into every mead it makes.
- **Trash filter.** Shift+E on a cooking station, oven, cauldron, mead cauldron or prep table sets the fewest stars
  it keeps: Keep all, 1★ and up, 2★ and up, or 3★ only. Dishes below are thrown away, so a late-game kitchen can
  hand out 3★ food only. The station shows your own chance of keeping a dish.
- **Kitchen perks from your level:** food cooks up to 50% faster and takes twice as long to burn, meads ferment up
  to 30% faster, extra food comes up to 50% of the time (the game's own bonus stops at 25%), and crafting at a
  kitchen sometimes gives back an ingredient.
- **Cooking experience** as the game gives it, more for rich late-game dishes, and triple the first time you make
  each dish.
- **Stars stay apart.** 1★ and 3★ stacks never merge, in inventories, chests or on the ground.
- **Gentler deaths, if you want them.** Choose how much of every skill's level dying costs and whether you keep
  the progress toward the next level. The defaults are the game's own.
- **Multiplayer.** Stars are decided on the machine that owns the station, the cook's level travels with the food,
  and experience reaches the cook even when someone else's filter throws the dish away.

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod on every client and on the server.

For manual install, drag GrindstoneSkills.dll into the BepInEx/plugins folder.

### Settings

All in `BepInEx/config/com.GrindstoneSkills.cfg`, reloaded while the game runs.

- **Stars:** the food and duration bonus of each star.
- **Odds:** the chance of 0, 1, 2 and 3 stars at levels 0, 25, 50, 75, 100 and 130, and how many levels a star of the
  ingredients is worth.
- **Kitchen:** the trash filter switch and every perk's value at level 100 (perks grow from nothing at level 0).
- **Experience:** an overall multiplier, the tier scaling, the first-dish bonus and fermenter experience.
- **Death:** how much of every skill's level dying costs (5% by default, as in the game; 0 keeps every level) and
  whether the progress toward the next level is lost (on by default, as in the game).
- **Display** (each player's own): stars on icons, and which stars recipes use first.

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.

### Works with
- **FeastMaster:** FeastMaster sets each food's base values; GrindstoneSkills' stars add on top.
- **OpenKeep:** starred food stacks and quick-stacks by star count.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` repository checked out next to this
one. `pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build GrindstoneSkills/GrindstoneSkills.csproj -c Release
```

### Bugs and feature requests
The source lives on [GitHub](https://github.com/geraldjglasgow/ValheimMods). Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.

# CLAUDE.md - Hearthhold

Stardew Valley style quality for Valheim's food. Food, its ingredients and meads carry 0 to 3 stars (plain, bronze,
silver, gold); better ingredients and a better cook make better food; starred food is worth more when eaten, sold or
given. Requires GrindstoneSkills (hard dependency): it owns item stars (the quality field, stacking apart, recipe
counting, the bronze/silver/gold display) and the Foraging and Husbandry skills; Hearthhold decides when an item gets
stars and what they do, through GrindstoneSkills' public API (`Core/GrindstoneLink.cs`). Required on the server and
every client. Version 0.1.0, started 2026-10-06; first in-game test 2026-10-06 (single player, see "Tested").

## User decisions (2026-10-06)

- Stars on forage (berries, mushrooms, dandelion, thistle, honey, sap...) from the Foraging level; on animal meat from
  the creature's own stars and the killer's Husbandry level; on farmed crops from the Farming level; on fish (the
  catch's level) when it is cleaned.
- Higher star ingredients raise the odds of higher star dishes and meads; the cook's Cooking level counts too.
- Ideas taken: bronze/silver/gold star colours, Daily Fortune, fish, professions, trader friendship (small discount
  only), aging cask, shipping crate. The bundle hall is a possible later feature (README says so).
- The .cfg holds only switches: Daily Fortune, Shipping Crate and Trader Friendship. Everything else is fixed numbers
  in code (below).
- GrindstoneSkills' old star cleanup (`OldStarCleanup`) was removed so it never strips Hearthhold's stars, and it got
  a public API (`SkillsApi`, `StarsApi`).

## Fixed numbers

- **Odds** (`Core/StarOdds.cs`): effective level = skill level (0-100) + source bonus + fortune, 0 to 150, chances
  interpolated between: 0 -> 90/9/1/0 %, 50 -> 55/30/12/3, 100 -> 25/38/26/11, 150 -> 5/35/35/25 (plain/bronze/
  silver/gold).
- **Ingredient floor (user, 2026-10-06):** a dish or mead base is never below its ingredients' average stars rounded
  down (silver meat always cooks to at least silver); the cook's level only raises it.
- **Source bonus:** ingredients +20 per average star of the star items used; a creature +20 per creature star (max
  +60); a fish +15 per level above 1 (levels 1-5).
- **Daily Fortune** (`Fortune/`): one per in-game day, the same for everyone (hash of world seed and day): Ill-omened
  -10 (10%), Poor -5 (20%), Fair 0 (40%), Good +5 (20%), Blessed +10 (10%). Told top-left on arrival and each new day;
  `fortune` console command.
- **Professions** (level 100, `Core/Professions.cs`): Botanist (Foraging) wild picks, honey and sap always gold;
  Tiller (Farming) crops at least silver; Rancher (Husbandry) meat at least silver; Gourmet (Cooking) dishes, mead bases
  and meads at least silver; Angler (Fishing) raw fish you clean at least silver.
- **Eating** (`Eating/`): per star +10% health, stamina and eitr and +10% duration.
- **Meads** (`Eating/`): per star, stronger or longer (see that folder's notes).
- **Shipping Crate** (`Shipping/`): price per unit = max(1, round(food value / 5)) coins, x1 / x1.25 / x1.5 / x2 by
  stars; items with no food value 2 coins base. Sold at dawn.
- **Trader friendship** (`Friendship/`): one starred dish per trader per day; 0-10 hearts; 1% discount per heart, at
  most 10%.
- **Aging Cask** (`Cask/`): meads and wines gain a star every 2 in-game days, up to gold.

## How a star is decided (multiplayer)

Skill levels live on the actor's client; items spawn on the object's owner. The actor sends a mark (`Core/Marks.cs`,
RPC `hearthhold_mark`: level, input stars) on the object's ZNetView just before the game's own RPC; routed RPCs from
one peer arrive in order, so the owner holds it when the game's RPC runs. The owner opens a spawn scope
(`Core/SpawnStars.cs`) around the game's spawning call; a postfix on `ItemDrop.Awake` gives each new star item its
rolled stars and saves it. Without a mark (a vanilla client) the roll is at level 0. Crafting at a cauldron or prep
table is local to the crafter, so it rolls there.

## Built details (2026-10-06, from the feature work)

- **Meat:** most creatures drop through their death ragdoll seconds later (`Ragdoll.Setup` stores the drop list, the
  ragdoll's owner spawns it in `SpawnLoot`): the killer's Husbandry level is written to the ragdoll's ZDO
  (`hearthhold_killer_husbandry`) and read there; the creature level comes from the game's own key.
- **Crafting:** the inventory list is sorted most stars first for the craft (best ingredients used), then restored.
  Raw food dropped from a broken station keeps its input stars. Meads copy their base's stars exactly (the Gourmet
  floor applies when the base is crafted).
- **Meads:** restoring meads (health, stamina, eitr) restore +10% per star, ttl untouched (it is the re-drink
  cooldown); other effects last +15% per star.
- **Shipping Crate:** copy of `piece_chest_wood`, 6 x 3, 10 Wood + 4 Resin; sells at the dawn day change (15% into the
  day), once however many days were missed; skips a stack whose coins would not fit; tells players within 30 m.
- **Trader friendship:** gifts by using a dish from the hotbar on the trader (`Trader.UseItem`); every kitchen product
  is taken over there, so a refused dish is not eaten; quest items stay the game's. No heart glyph (font not
  confirmed): "Friendship 3/10, 3% off". The discount lowers trade prices only inside `StoreGui.FillList` and
  `BuySelectedItem`.
- **Aging Cask:** copy of `piece_chest_barrel`, 10 FineWood + 10 BronzeNails + 5 Resin; records per slot in the ZDO;
  ages every 2 x `EnvMan.m_dayLengthSec` of game time, sleeping included.

## Which items carry stars (`Catalog/`)

- Kitchen products: outputs of cooking stations, fermenters and kitchen recipes that make food (a food value) or an
  intermediate a station or barrel takes (mead bases, dough, raw fish); bait and feast materials are left out.
- Forage: wild picks that can be eaten, plus `Sources.ForageExtras` (dandelion, thistle, fiddlehead, smoke puffs, royal
  jelly, sap); hive and sap collector output by the same rule. Crops: any crop a kitchen uses or that can be eaten.
- Meat: creature drops that can be eaten or a cooking station takes, plus `Sources.MeatExtras` (entrails, blood bags).
  Feathers, greydwarf eyes, coal and freeze glands go into some meads but stay plain (they have other uses).
- Items with a Piece are not feasts (this game lets ordinary items be placed); only a Feast component marks a feast.

## Tested in game 2026-10-06 (single player, DevBridge)

Fortune command; forage stars at Foraging 60/90 (all four tiers, colours right) and Botanist at 100; meat stars from
2-star boars (+40 bonus seen in the rolls); Shipping Crate sale and prices (10 plain raspberries 50 coins, 10 gold 100);
Aging Cask (minor healing meads plain -> bronze after a 2-day time skip); Haldor gift (silver mead +60 points, second
gift that day refused). Cooking station: silver raw meat -> silver cooked meat (ingredient floor). Not yet: cauldron crafting, fermenter, eating bonuses, meads, the trader
discount, the killer's Husbandry level, the dawn trigger itself, a dedicated server.

## Code map

- `Core/`: plugin settings, keys, `GrindstoneLink` (API by reflection), `Stars`, `StarSource`, `StarOdds`,
  `Professions`, `SpawnStars`, `Marks`, `ClonedPiece(s)` (build pieces copied from the game's), `HookGuard`.
- `Catalog/`: `Discovery` (after ZNetScene.Awake and ObjectDB.Awake), `Kitchen` (kitchens, products, ingredients,
  food values), `Sources` (forage, crop, meat items), `StarItems` (registers every product and source item with
  GrindstoneSkills, clears quality scaling).
- `Fortune/`, `Sources/` (picks, hives, sap, creature drops), `Kitchen/` (stations, crafting, fermenter), `Eating/`,
  `Shipping/`, `Friendship/`, `Cask/`.

## Rules

Methods at most 24 lines, classes at most 300 (workspace rule). Every key starts with `hearthhold_`. Check the game's
own code (decompiled into the scratch folder) before patching; prefer prefixes and postfixes; never break another mod's
patch on the same method (GrindstoneSkills patches cooking stations, fermenters, crafting and pickables too).

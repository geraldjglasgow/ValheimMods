# EliteCrafting

Crafting stones and magic gear for Valheim. Creatures drop **stones**, and sometimes a piece of gear whose name is
colored by its rarity. Click a stone onto an item in your inventory to raise it up the ladder (Common, Uncommon,
Rare, Epic, Legendary, Mythic), add an affix, swap one, reroll them, lock one, corrupt the item for a gamble, or copy
it. Biome **essences** reshape an item around their family, and magic items you do not want grind into **shards** that
fuse back into stones. Magic gear glows its rarity color on the ground. Rarities, affixes, stones and drop tables are
all YAML, synced from the server and reloaded live.

**Status: 0.2.0 in development. Built, not yet tested in game.**

## Features

- **Six rarities.** Common (white, never glows), Uncommon (green), Rare (blue), Epic (purple), Legendary (orange) and
  Mythic (red). Rarity is how many affixes an item carries: 1-2 on Uncommon up to 6 on Mythic. Mythic never drops; it
  is made with the Stone of Apotheosis.
- **162 affixes** across weapons, shields, armor, capes, utility items and tools, each a plain sentence in the tooltip
  ("You move 6% faster", "Adds 12% of this weapon's damage as fire"). Slot sanity: armor stats never roll on weapons,
  weapon stats never on armor, skill bonuses only on items of that skill.
- **Affix tiers 1-7**, one per biome from the Meadows to the Ashlands. An item can roll at most the tier of the
  biome its materials come from, so an Ashlands sword rolls stronger affixes than a bronze one.
- **27 stones, 16 essences** (tables below). A stone that cannot work on an item says why and is not used up.
- **Sigils** steer the next stone; **Honing and Tempering** make a weapon hit or an armor piece protect a little
  better, and that survives even a Stone of Unmaking.
- **Salvage**: grind a magic item you do not want into shards of the stone that made its rarity, and fuse shards back
  into stones.
- **Drops**: every kill a player took part in may drop stones and, more rarely, a pre-rolled Uncommon to Legendary item
  from the gear of its biome. Stars raise the odds; bosses guarantee drops and their biome's essences. Dungeon and ruin
  chests, buried treasure and other world containers roll once, when the game fills them. Tamed creatures and kills
  no player was involved in drop nothing.
- **Loot-find affixes** (Norns' Favour, Fateweaver, Trophy Taker, Hoardfinder) count for the player who lands the
  killing blow, whoever's machine rolls the loot.
- **Tooltip block** with the rarity, each affix and its tier (Compact, Standard or Full detail, your choice), and
  **rarity-colored names** in the inventory, on the ground, in the pickup message, in the workbench's upgrade tab and
  on item and armor stands.
- **Ground glow**: magic items lying in the world glow in their rarity color. Only the nearest 25 hold a light, so a
  pile of loot never turns into a pile of lights.
- **Fully configurable, server synced**: two YAML families layered over complete built-in defaults, edited while the
  game runs (checked every five seconds, no restart). On a server every player rolls under the server's odds.
- **Elite Creatures Reborn synergy** (optional, off by default): its elite stars raise stone and gear drops.

## Required on the server and on every client

The stones are items of their own, so every machine has to know them: install the mod on the dedicated server and on
every player's game, same version everywhere. A player without it, or with a different version, is refused at join
with a message naming the mod and both versions.

## Install

1. Install [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install EliteCrafting, on the server and on every client.
3. Start the game (or the server) once: `com.EliteCrafting.cfg`, `EliteCrafting_affixes.yml` and
   `EliteCrafting_economy.yml` are written to `BepInEx/config`.

For a manual install, drop `EliteCrafting.dll` into `BepInEx/plugins`.

## Stones

Pick up a stack of stones in the inventory and click it onto an item in your own inventory (not in an open chest).
One stone is used per success. A stone dropped on another stone stacks or swaps as usual. The Stone of Unmaking and the
Serpent Stone cannot be undone and ask first: hold Shift while you click (or switch `Confirm destructive stones` to a
dialog).

| Stone | Works on | Does | Drops from |
|---|---|---|---|
| Stone of Awakening | Common | Makes the item Uncommon with one affix | everywhere; Eikthyr |
| Stone of Ascension | Uncommon | Makes it Rare, keeps its affixes and adds to Rare's minimum | Black Forest and later; the Elder |
| Stone of Exaltation | Rare | Makes it Epic, keeping its affixes | Mountain and later; Moder, Yagluth |
| Stone of Transcendence | Epic | Makes it Legendary, keeping its affixes | Mistlands and later; the Queen, the Fader |
| Stone of Apotheosis | Legendary | Makes it Mythic, keeping its affixes | Ashlands, very rare; sometimes the Fader |
| Lesser / Greater Stone of Growth | Uncommon, Rare / Epic and up | Adds one affix, if the rarity has room | Black Forest / Mountain and later |
| Lesser / Greater Stone of Turmoil | Uncommon, Rare / Epic and up | Removes one random affix and rolls a new, different one | everywhere / Mountain and later |
| Lesser / Greater Stone of Upheaval | Uncommon, Rare / Epic and up | Rerolls every affix, keeping the rarity | Black Forest / Mountain and later |
| Lesser / Greater Stone of Perfection | Uncommon, Rare / Epic and up | Rerolls the numbers, keeping the affixes | Swamp / Plains and later |
| Lesser / Greater Stone of Severing | Uncommon, Rare / Epic and up | Removes one random affix (not below the rarity's minimum) | Black Forest / Mountain and later |
| Stone of Unmaking | any magic item | Strips it back to Common; Honing, Tempering and a pending sigil stay | everywhere |
| Serpent Stone | any magic item | Corrupts it and **seals** it for good, with one of five outcomes: nothing more, an extra affix past the cap, a chaotic reroll that ignores tier limits, one rarity up (a 7th affix on a Mythic) or one rarity down | Swamp and later; Bonemass |
| Stone of Binding | any magic item | Locks one random affix: it survives Turmoil, Upheaval, Perfection and Severing. One at a time | Plains and later; Yagluth |
| Stone of Chance | Common | Turns it into a random rarity: 50% Uncommon, 30% Rare, 15% Epic, 5% Legendary, never Mythic | everywhere |
| Stone of Reflection | any magic item | Makes a copy, affixes and bonuses included; the copy is sealed | astronomically rare, Mistlands and later |
| Honing Stone | weapons | +1% damage per use, up to +10% | everywhere |
| Tempering Stone | armor, capes, shields | +1% armor (block on a shield) per use, up to +10% | everywhere |
| Sigil of Preservation | any item | The next reroll-type stone leaves the highest-tier affix untouched | Mountain and later |
| Sigils of War, Warding, Fortune | any item | The next added affix comes from offense, defense or utility | Swamp and later |
| Sigil of Culling | any item | The next removal takes the lowest-tier affix instead of a random one | Swamp and later |

A sigil sits on the item as "Pending" until a stone it steers uses it; one at a time. A sealed item takes no stone,
essence or sigil again. Every stone's odds, costs and the rarities it accepts are in `EliteCrafting_economy.yml`.

A refusal says why, for example "The Stone of Ascension does not work on Rare items", "This item cannot hold another
affix", "No affix can roll on this item", or "Unequip this item first" when the server does not allow changing
equipped items.

## Essences

Sixteen essences, a Lesser and a Greater for each of eight families. An essence rerolls a magic item like a Stone of
Upheaval (a bound affix stays), and **one of the new affixes always comes from its family**. A Lesser essence works on
Uncommon and Rare items; a Greater one works on every magic rarity and rolls its family affix at the highest tier the
item allows. The essence's tooltip lists the affixes it can guarantee.

| Family | Biome | Drops from | Its affixes lean toward |
|---|---|---|---|
| Storm | Meadows | Meadows creatures, Eikthyr | lightning, speed, jumping, parries |
| Grove | Black Forest | Black Forest creatures, the Elder | blunt damage, woodcutting, regeneration, thorns, standing firm |
| Venom | Swamp | Swamp creatures, Bonemass | poison, leeching, cleansing, wading |
| Frost | Mountain | Mountain creatures, Moder | frost, cold, climbing, falling, stamina |
| Battle | Plains | Plains creatures, Yagluth | raw damage, armor, blocking, carrying |
| Seidr | Mistlands | Mistlands creatures, the Queen | eitr, magic, runes, the mist |
| Ember | Ashlands | Ashlands creatures, the Fader | fire, heat, light |
| Tide | Ocean | serpents | the sea: sailing, swimming, fishing, sea creatures |

Each boss always drops a Lesser essence of its family and sometimes a Greater one. Families are configurable in
`essence_families`.

## Salvage

Hover a magic item in your own inventory and press **Shift + End** (`8 - Salvage / Salvage key`; without Shift it only
asks, or it follows your `Confirm destructive stones` mode). The item is ground into **two shards** of the ascension
stone that made its rarity: an Uncommon into Shards of Awakening, a Rare into Shards of Ascension, up to Mythic and the
Shards of Apotheosis. **Right-click** five shards to fuse a stone (ten for Apotheosis); **Shift + right-click** fuses
every full set. The loop always loses: a ground item returns at most 40% of one stone.

Equipped items and items with a pending sigil are refused; a sealed item grinds like any other. A server can switch
grinding off (`Salvage`, synced), require a crafting station nearby and change every number in the `salvage` section.

## Affixes

| Where | Affixes |
|---|---|
| Weapons (damage) | Honed Might, Primal Fury, Nightstalker; the brands Emberbrand, Rimebrand, Stormbrand, Venombrand, Spiritbrand, Bonebreaker, Keen Edge, Needlepoint; Undead, Beast and Sea Slayer, Godslayer; Ambusher, Cruel Opening, Press the Advantage, Deathblow; Berserkergang (while health-critical) |
| Weapons (on hit and kill) | Reaper, Soul Reaper, Blood Drinker, Cornered Thirst, Seidr Siphon, Evader's Fury, Hamstring, Staggering Blows, Dazing Blows, Fafnir's Greed |
| Melee weapons | Balanced Grip, Long Reach, Sweeping Arc, Blood Price, Rune-Edged, Lone Blade, Steel Rhythm, Heartwood; Blade, Axe, Club, Knife, Spear, Polearm, Fist and Woodcutter's Mastery |
| Bows and crossbows | Easy Draw, Quick Windlass, Swift String, True Flight, Volley, Thrifty Quiver, Skirmisher; Bow and Crossbow Mastery |
| Staves | Seidr Thrift, Blood Thrift, Twincast, Grave-Lord's Command, Grave Vigor; Elemental and Blood Mastery |
| Shields | Stalwart, Perfect Guard, Repelling Guard, Tireless Guard, Keen Guard, Anchored Guard, Seidr Riposte, Shield Mastery |
| Armor: health and regeneration | Vigor, Endurance, Wellspring, Troll Blood, Second Wind, Seidr Flow, Mending, Stout Heart, Restless Mind, Purity, Resolute, Quick Recovery, Valhalla's Edge |
| Armor: protection | Hardened, Padded, Mailed, Riveted, Ironclad, Arrowward, Flameward, Frostward, Stormward, Venomward, Elemental Ward, the Fire, Frost, Lightning and Poison Bulwarks, Mist Veil, Bramblehide, Runic Ward, Ironroot, Coldblood, Ashen Skin; the Cornered Blood, Hide and Veil variants |
| Movement | Fleetfoot, Stride, Momentum, Pathfinder, Mountain Goat, Marshstrider, Spring-Heeled, Light Leap, Nimble, Soft Landing, Long Wind, Strong Swimmer, Raven's Glide, Ghostwalk, Soft Tread, Pack Mule, Cornered Flight, Wanderer's Mastery |
| Weather and world | Emberheart, Winterborn, Oilskin, Sealegs, Shadowmeld, Hearthlight (a light everyone sees), Mistbane, Fair Winds, Beast Whisperer, Hearthbound |
| Utility items and helmets | Broad Back, Magpie, Huginn's Eye, Mimir's Insight, Artisan's Mastery, Gourmand, Soulbound, Brewer's Haste, Forsaken Favour, Reflex Draught, Swift Draught, Harvester; loot find: Norns' Favour, Fateweaver, Trophy Taker, Hoardfinder |
| Tools | Builder's Reach, Tireless Hands, Green Thumb, Miner's Mastery, Angler's Mastery, Deep Vein |
| Most gear | Well-Forged and Everlasting (durability), Lightened and Gossamer (weight), Supple Fit (no movement penalty) |

`ecraft list affixes` in the console prints the full list with slots and tiers. Evader's Fury, Steel Rhythm and a
charged Runic Ward show an icon on the HUD while they are active.

## Console commands

Open the console with F5. Everything is under one command, `ecraft`. Output is English.

| Command | Who | Does |
|---|---|---|
| `ecraft help` | everyone | Lists the sub-commands you may run |
| `ecraft inspect [cursor\|hover\|ground\|<slot>]` | everyone* | An item's EliteCrafting data and what it means |
| `ecraft stats` | everyone* | Your summed affix totals and the effects active right now |
| `ecraft list affixes\|stones\|rarities [<filter>]` | everyone* | The configuration in force, filtered by slot, category, rarity or id; `stones` also lists the essence families and the shards |
| `ecraft give <stone>\|<shard>\|all [count]` | admin | Stones, essences or shards into your inventory |
| `ecraft roll <rarity> <prefab\|slot> [tier]` | admin | A rolled magic item into your inventory |
| `ecraft reroll [cursor\|hover\|<slot>]` | admin | Rerolls an item's affixes, keeping its rarity |
| `ecraft affix <affix> [tier] [value] [cursor\|hover\|<slot>]` | admin | Adds or replaces one affix, for testing |
| `ecraft reload` | admin, on the machine whose files are in force | Re-reads the YAML, the translations and the `.cfg` now |
| `ecraft dump affixes\|economy\|items` | admin | Writes the merged configuration in force, or a survey of every item, to the config folder |
| `ecraft tiers` | admin | Writes `EliteCrafting_item_tiers_reference.yml`: every magic base with its tier and why |
| `ecraft ecr` | everyone* | The Elite Creatures Reborn synergy: installed or not, the switch, and what the creature you look at would pay |

\* unless the server turns `Read-only commands for everyone` off. `<slot>` is an equipment slot: `right`, `left`,
`head`, `chest`, `legs`, `cape`, `utility`.

## Files

| File | What |
|---|---|
| `BepInEx/config/com.EliteCrafting.cfg` | Switches and preferences. Gameplay keys (affix effects, modifying equipped items, stone and gear drops, command access, salvage, the Elite Creatures Reborn synergy) follow the server; display, ground glow, the confirm mode, the Salvage key and diagnostics are per player |
| `BepInEx/config/EliteCrafting_affixes.yml` | Every affix: effect, slots, category, tiers, weights, caps |
| `BepInEx/config/EliteCrafting_economy.yml` | Rarities and colors, rolling rules, stones, sigils, essence families, salvage, item tiers, biomes and drop tables (creatures, bosses, chests, Elite Creatures Reborn) |
| `EliteCrafting_affixes_<anything>.yml`, `EliteCrafting_economy_<anything>.yml` | Your own additions, read after the main file in name order; they change only what they name |
| `EliteCrafting.translations.<Language>.yml` | Your own words for a language, key to text, over the built-in English |

The main YAML files are written once with the full defaults and never rewritten. The built-in defaults always sit
underneath, so a later release's new affixes reach your server without editing anything; `use_defaults: false` in a
main file makes the files the whole configuration. A file with an error is reported in the log with file and line,
and the previous rules stay in force. Turn an affix off with `enabled: false` (items that have it keep it, greyed and
inert, and get it back when you turn it on) or stop it rolling with `weight: 0`.

**Upgrading a server that ran 0.1.0:** its main `EliteCrafting_economy.yml` was written by 0.1.0 and still names the
stones that were not ready then with `enabled: false`, and its boss `bonus` lists lack the essences. A main file wins
over the built-in defaults for everything it names, so those stones stay off and bosses drop no essences. Delete or
rename the main file (the next start writes the 0.2.0 one) and move your own changes into an
`EliteCrafting_economy_<anything>.yml`. New affixes, essences and drop-table rows your file does not name arrive on
their own.

## With OpenKeep

OpenKeep's Salvage tab turns ordinary crafted items back into materials and, by default, leaves magic items alone;
EliteCrafting's Salvage key grinds magic items into shards.

## With Elite Creatures Reborn

Optional and off by default (`9 - Elite Creatures Reborn / Synergy`, synced). With both mods installed and the switch
on, an elite's stars raise EliteCrafting's stone and gear drops on their own table (`drops.ecr`; a three-star elite
pays double). Whenever Elite Creatures Reborn is installed, its Cloven twin and Phantom husks drop nothing of ours, so
a split boss never pays twice. Without it nothing changes. `ecraft ecr` shows what it sees.

## Uninstalling

Removing the mod **deletes every stone, essence and shard** from inventories, chests and the ground the next time they load, because the
game drops items it no longer knows. Magic gear keeps its data: it turns back into its plain vanilla item while the mod
is gone, and gets its rarity and affixes back when the mod is reinstalled.

If another mod makes weapons or armor stackable, those items stop taking stones (a warning is logged); items that are
already magic keep their affixes, but the game may merge two of them into one stack and lose one's affixes.

## Building

Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` folder next to this one.

```
dotnet build EliteCrafting/EliteCrafting.csproj -c Release
```

The build merges Charter, ConfigReload, PatchGuard and YamlDotNet into `dist/EliteCrafting.dll`. Override
`-p:GamePath=...`, `-p:BepInExCore=...` or `-p:ModLibsPath=...` when your layout differs.

## License

GNU General Public License v3.0, see `LICENSE`.

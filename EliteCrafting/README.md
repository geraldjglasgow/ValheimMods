# EliteCrafting

**Work in progress:** significant changes will come, including to the config, the YAML files and item data.

Runes and magic gear: creatures and chests drop runes; a rune clicked onto an item changes its rarity,
inscriptions or sockets.

## Features
- Rarities: Normal, Magic and Rare, named and coloured (below).
- Inscriptions: 209 magic properties; each kind of item rolls its own (below).
- Tiers: later biomes roll stronger inscriptions; T1 is the strongest.
- Runes: six, clicked onto an item from your inventory or an open chest (below).
- Rune Table: stores runes and turns trophies into essence, which chooses what Ascension adds.
- Sockets: up to three, cut by the Dvergr Chisel, filled with gems that only bosses drop (below).
- Drops: creatures, bosses and chests drop runes, more with stars; magic gear never drops.
- Salvage: with OpenKeep, salvaged Magic or Rare gear may give a rune back.
- Magic items on the ground glow under a beam of light.
- Elite Creatures Reborn: `Synergy` lets elite stars raise drops.
- An API for other mods: PackPanel and Elite Creatures Pack join in.
- Every rune, inscription, command and file:
  [CLAUDE.md](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCrafting/CLAUDE.md#player-reference).

## Rarities
- Normal: a plain item, no inscriptions.
- Magic (green): two inscriptions, one prefix and one suffix.
- Rare (blue): three inscriptions, at most two prefixes and two suffixes.

## Runes
- Awakening: makes a Normal item Magic with two inscriptions.
- Recasting: replaces both inscriptions on a Magic item with two new ones.
- Ascension: makes a Magic item Rare, keeping both and adding a third.
- Cleansing: strips a Magic or Rare item back to Normal.
- Sealed: a gamble (nothing, one more inscription, or a wild reroll), then sealed for good.

## Gems
A gem's stat depends on the item (weapon, staff, or armour and shield); a full item's gem can be replaced.
- Surtr's, Ymir's, Thor's, Nidhogg's, Hel's: an element's damage, staff bonus or resistance.
- Tyr's: physical damage, summon damage, less stagger.
- Freyja's, Odin's, Skadi's: health, eitr or stamina.
- Heimdall's: critical hits, cast speed, avoiding hits.
- Sleipnir's: movement speed, armour, shields and backpacks only.

## Inscriptions
A prefix is the item's core power; a suffix is everything else.
- Weapon damage (18): more damage, critical hits, slayers.
- Elemental brands (6): flat fire, frost, lightning, poison or spirit damage.
- On hit (10): chain lightning, paralysis, stagger, hamstring.
- Attack speed and cost (10): faster, cheaper swings.
- Leech (6): health, eitr or stamina from hits and kills.
- Ranged and magic (12): draw speed, Volley, Twincast, summons.
- Throwing (3): throw a melee weapon, recall it, leap to it.
- Blocking and parry (14): stronger blocks and parries, avoiding hits, thorns.
- Health, stamina and eitr (14): more of each, faster regeneration.
- Armour and resistances (25): armour, less damage by type, less stagger.
- Movement (23): run, sprint and swim faster, double jump, glide, sail.
- Weather, food and meads (16): never Cold or Wet, longer food, a light.
- Fortune and insight (15): carry more; more runes, coins and trophies.
- Gathering and building (15): extra ore, wood and harvest; free builds.
- Skill masteries (18): extra skill levels.
- Item properties (4): durability, weight.

## Install
Needed on the server and every client, same version. Install with r2modman or the Thunderstore app, or put
`EliteCrafting.dll` in `BepInEx/plugins`. Uninstalling deletes runes and gems; magic gear turns plain until reinstalled.
Mods that make weapons or armor stackable can merge magic items and lose inscriptions.

## Configuration
`BepInEx/config/com.EliteCrafting.cfg` and the `EliteCrafting_*.yml` files. Every setting is described in the file
and applies without a restart; the server's values bind every player (`Lock Configuration`). Console: `ecraft help`.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

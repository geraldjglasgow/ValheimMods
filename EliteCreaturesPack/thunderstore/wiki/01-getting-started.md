# Getting Started

New creatures for Valheim, each with its own fight, and bone weapons made from what they drop.

This wiki describes version 0.7.2.

| Creature | Where | In short |
| --- | --- | --- |
| [Crypt Mimic](wiki:Crypt Mimic and Greydwarf Slinger) | Burial chambers, sunken crypts | A crypt chest that bites its opener |
| [Greydwarf Slinger](wiki:Crypt Mimic and Greydwarf Slinger) | Black Forest | A greydwarf that shoots stones |
| [Rime Giant](wiki:Rime Giant) | Mountains, very rare | An ice-plated troll; fire breaks its plates |
| [Kraken](wiki:The Kraken) | Deep ocean, rare | A sea boss that hunts ships |
| [Arsenal Skeletons](wiki:Skeletons) | Black Forest | Skeletons with bone weapons |
| [Skeleton Crossbowman](wiki:Skeletons) | Black Forest | Reloads after every shot |
| [Crypt Executioner](wiki:Skeletons) | Burial chambers | A mini boss with a bone greataxe |

Players can make the [Bone Weapons](wiki:Bone Weapons) and the kraken's food and shield.

## Install

- Install on the **server and every client, at the same version**. A player with another version, or without the
  mod, is refused on joining with a message saying what to update.
- Use r2modman or the Thunderstore app, or put `EliteCreaturesPack.dll` in `BepInEx/plugins` (needs BepInExPack
  Valheim).
- Models may not show on macOS. Texts are English only.

## Settings

- `BepInEx/config/com.EliteCreaturesPack.cfg` describes every setting and its allowed range. Changes apply while the
  game runs. Each page lists its settings with their defaults.
- **The server's settings apply to everyone** while `Lock Configuration` (section `1 - General`, default `true`) is
  on. Admins can change them from their own game.
- Turning a creature off stops new ones; those already out stay.
- Biome lists are comma-separated game names (`BlackForest`, `Mountain`...). Recipes are comma-separated
  `item:amount:amount per upgrade` with game prefab names (`BoneFragments`, `Wood`...; the spine is `ECP_Spine`).

## Console commands

| Command | Shows |
| --- | --- |
| `charter status` | Whether the server's settings apply to you, and whether you may change them |
| `charter diff` | Settings where the server's value differs from your .cfg |
| `charter versions` | Mod versions on your side and on the server |

With `devcommands`, `spawn <prefab> [amount] [level]` makes any of these (level 2 is one star):

- Creatures: `ECP_CryptMimic`, `ECP_GreydwarfSlinger`, `ECP_RimeGiant`, `ECP_Kraken` (at sea near a crewed ship),
  `ECP_Headsman` (Crypt Executioner), `ECP_SkeletonCutthroat` (also `Swordsman`, `Axeman`, `Bonebreaker`,
  `Spearman`, `Halberdier`, `Bowman`, `Crossbowman`).
- Items: `ECP_Spine`, `ECP_BoneDagger` (also `Sword`, `Axe`, `Mace`, `Spear`, `Atgeir`, `Bow`), `ECP_ArrowBone`,
  `ECP_BoneCrossbow`, `ECP_BoltBoneBlunt`, `ECP_ExecutionerGreataxe`, `ECP_ExecutionerAxehead`, `ECP_KrakenBeak`,
  `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`.

## Other mods

- **Elite Creatures Reborn** (optional): these creatures roll stars and mutations, and its
  `elite spawn <prefab> <stars>` takes these prefabs.
- **EliteCrafting** (optional): every creature here drops its runes and magic gear, the bosses more (below). The
  bone weapons, the Bone Crossbow and the Executioner's Greataxe are item level 2 (Black Forest) and the Kraken shield
  level 4, so they roll inscriptions like the game's gear of those biomes.
- **Epic Loot:** add an `ECP_CryptMimic` entry to its loot tables to give mimics magic items.
- Loot mods, spawn mods and other mods' ships work with these creatures.

## With EliteCrafting

Runes and magic gear come on top of each creature's own loot. Boss magic items roll Rare more often, as from the game's
bosses: 70% at level 2, 85% at level 4.

| Creature | Level | Drops |
| --- | --- | --- |
| Kraken | 4 (Ocean) | Every kill: 3 runes, 1 magic item and a Serpent Rune |
| Rime Giant | 4 (Mountain) | Every kill: 3 runes and 1 magic item; 50%: a Consecrated Rune |
| Crypt Executioner | 2 (Black Forest) | Every kill: 2 runes and 1 magic item; 25%: an Ascension Rune |
| Its raised skeletons | - | Nothing |
| Crypt Mimic | The crypt's: 2 burial chamber, 3 sunken crypt | A crypt chest's odds: 30% a rune and 10% a magic item (36% and 12% in a sunken crypt) |
| Greydwarf Slinger, arsenal skeletons, Skeleton Crossbowman | 2 (Black Forest) | A Black Forest creature's odds: 5% a rune, 1.25% a magic item |

A server's EliteCrafting files can change any of these by prefab name. `ecraft tiers` lists the bone weapons' levels.

## Links

- Source: https://github.com/geraldjglasgow/ValheimMods
- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and its version)
- Licence: GPL-3.0

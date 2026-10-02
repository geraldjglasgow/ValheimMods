# EliteCrafting

Crafting stones and magic gear: creatures and chests drop both, and a stone clicked onto an item changes its rarity
or inscriptions.

## Features
- Six rarities, Common to Mythic, by inscription count; magic gear glows on the ground.
- 162 inscriptions for every kind of gear, stronger the later the item's biome.
- 27 stones: click one onto an item to promote, reroll, add, remove, lock, corrupt or copy it; a refused stone is kept.
- 16 biome essences reroll an item around their family; sigils steer the next stone.
- Sockets: drops carry up to 4, a Jeweller's Chisel cuts up to 2; 8 family gems fill them, 8 catalysts strengthen a family.
- Salvage: Shift + End grinds a magic item into shards; five shards fuse a stone.
- Drops from kills (more with stars, sure from bosses) and world chests.
- Elite Creatures Reborn: `Synergy` (off by default) lets elite stars raise drops.
- OpenKeep's Salvage tab leaves magic items to this mod's Salvage.
- Every stone, essence, inscription, command and file:
  [CLAUDE.md](https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCrafting/CLAUDE.md#player-reference).

## Install
Needed on the server and every client, same version. Install with r2modman or the Thunderstore app, or put
`EliteCrafting.dll` in `BepInEx/plugins`. Uninstalling deletes every stone, essence and shard; magic gear turns
plain until reinstalled. Mods that make weapons or armor stackable can merge magic items and lose inscriptions.

## Configuration
`BepInEx/config/com.EliteCrafting.cfg` and the `EliteCrafting_*.yml` files. Every setting is described in the file
and applies without a restart; the server's values bind every player (`Lock Configuration`). Console: `ecraft help`.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

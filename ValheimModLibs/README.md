# ValheimModLibs

Small, focused libraries for Valheim BepInEx mods, extracted from Elite Creatures Reborn, the reference consumer. Each one builds to a DLL that you merge into your plugin with ILRepack, so players never install them separately.

| Library | What it does | Depends on |
| --- | --- | --- |
| [ConfigReload](ConfigReload/) | Writes the .cfg at startup, hot reloads it on edit | BepInEx |
| [TraitSets](TraitSets/) | Enum sets in ZDOs (one bit-mask key per set), weighted multi rolls | game, Unity |
| [YamlConfig](YamlConfig/) | YAML documents with parse helpers and validation, hot reload, Charter push, write-back, in-game editor, Valheim hooks | Charter, YamlDotNet, BepInEx, Harmony, game |
| [SyncedConfig](SyncedConfig/) | Facade: Charter + ConfigReload + YamlConfig in one object with `Bind`, `BindLocking`, `AddYaml`, `Finish` | all of the above |
| [PatchGuard](PatchGuard/) | Logs exceptions from the mod's own code under the mod's name (`Guard.Run` / `Guard.Wrap` around entry points), then rethrows; `Profiler` times patched methods | BepInEx, Harmony |
| [ItemCopies](ItemCopies/) | Writes item values into the prefab and into every live copy of its shared data (world drops, inventory, eaten foods, the open container) and into new copies as they appear | BepInEx, Harmony, game, Unity |
| [PlateColumn](PlateColumn/) | The stat readouts on the inventory's player panel as one column of small brown boxes any mod adds a box to (rank, icon, optional number, tooltip), stacked right of the panel, with tooltips on the game's own armour and weight too, the same tip box on words of any text (`LinkTips`), and a row of small boxes under the minimap for readouts kept in view; the brown panel, cell and box sprites (`Skin`); loads an icon from an embedded PNG | game, Unity |
| [Charter](Charter/) | Our own server-to-player binding: the server's config values bind every player, stewards (admins) may amend them, articles carry other values, one join-check screen for all our mods, `charter` console command | BepInEx, Harmony, game, Unity |
| [BundlePrefabs](BundlePrefabs/) | A mod's own models in the game: loads its embedded asset bundle for the running platform, copies game prefabs to build on, registers prefabs with ZNetScene and ObjectDB on every peer, dresses models in the game's own materials | BepInEx, Harmony, game, Unity |
| [LocalEffects](LocalEffects/) | Local, cosmetic copies of the game's effect prefabs (bursts, sounds, lasting glows): seen on one machine only, never networked, never a damage source, thinned by a density | game, Unity |
| [PlayerGrid](PlayerGrid/) | Which cells of the local player's inventory are the main grid and which are PackPanel's slots, as PackPanel publishes it in the character's custom data; the writer and the readers in one place, trusted only while PackPanel is loaded and on | BepInEx, game |

## Building

Requires .NET SDK 8 and an installed Valheim (assemblies are referenced from the game folder, override with `-p:GamePath=...`). BepInEx core DLLs are taken from the game folder if BepInEx is installed there, otherwise from `lib/BepInEx/core` (copy them from BepInExPack).

```
dotnet build -c Release
```

Outputs land in each project's `bin/Release`. To use a library from a mod, add a `ProjectReference` (or reference the DLL) and list it in your ILRepack inputs. The mods next to this folder show the setup; EliteCreaturesReborn is the reference consumer, ShipConfig the smallest.

## Design rules

- No plugin of its own, no runtime dependency for players. Everything is merged into the consuming mod.
- Game specific code is limited to what the game forces (ZDO storage, the three Valheim hook points). Everything else is plain .NET.
- Namespaces are one word each (`Charter`, `ConfigReload`, `ItemCopies`, `TraitSets`, `YamlConfig`, `SyncedConfig`, `BundlePrefabs`, `LocalEffects`), public surface kept small and documented in the source.
- Every library is our own code, GPL-3.0 like the rest, written from its own specification; no third-party library is copied into the workspace.

GPL-3.0 licensed. See the `LICENSE` file.

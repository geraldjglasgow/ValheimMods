# CLAUDE.md - ValheimModLibs

Shared libraries for Valheim BepInEx mods, extracted from Elite Creatures Reborn, the reference consumer (`../EliteCreaturesReborn`, 2026-09-12). FeastMaster, Lockstep, OpenKeep and ShipConfig consume them as well.

## Layout

- `Directory.Build.props`: net48, nullable, game and BepInEx paths. All projects inherit it.
- `Charter/`: our own server-to-player configuration binding, written clean-room from `Charter/SPEC.md`. `Charter` (the per-mod object: `Clause`, `Binding`, `IsBound`, `IsAuthor`, `MayAmend`, `IsSteward`, `Install`), `Clause<T>` (a registered ConfigEntry with `AuthorValue`/`OwnValue`), `Article<T>` (a pushed value that is not an entry: `Assign`, `Changed`; ordinary articles travel only while bound, `standing: true` ones always). Internals, one class each: `Ledger` (registration), `PushBody`/`Fragmenter`/`FragmentAssembler` (wire), `Publisher`/`Courier` (author side), `Receiver` (player side), `ChangeRouter` (what an entry edit means on each side), `Amendments` (steward edits), `Stewardship` (admin list), `ReadOnlyTags` (configuration manager tag), `Family`/`FamilyEntry`/`FamilyCheck`/`FamilyRpc` (the one-screen join check across all our mods), `RefusalScreen`, `ConsoleCommands` (`charter`), `GameHooks`/`Ticker`. See `Charter/README.md` for the decisions.
- `ConfigReload/`: single file. `ConfigReloader.Setup(config, log)` saves the .cfg and polls it every five seconds for edits (no FileSystemWatcher: Mono on Linux fed it with the mod's own reads).
- `ItemCopies/`: `Copies.Apply(prefabName, write)` and `Copies.ApplyAll(write)` call the mod's write for the prefab's `SharedData` and for every live copy of it (`ItemDrop.s_instances`, the local inventory, the player's foods, the open container; each object once); `Copies.HookSpawns(harmony, onCopy)` runs the callback for new copies (postfixes on `ItemDrop.Awake` and `Inventory.AddItem(ItemData)`, installed once per merged copy). `CopyScan` reaches the private game members by reflection, `SpawnHooks` keeps the callbacks.
- `TraitSets/`: `TraitStore<T>` (bit mask + primary key in a ZDO), `TraitRoller` (weighted roll, roll many with declining chance).
- `YamlConfig/`: `YamlModel` is the base a mod derives from (`Read(YamlNode root)`, `Verify()`; `Errors` reject the files, `Warnings` do not; `ReadGroups`/`ResolveGroups` via `YamlGroupTable` for the common `groups:` map). `YamlNode` is one value of the parsed tree: `Get` chains through missing keys, the typed `Try*` readers report with the YAML path, `WarnUnknownKeys` flags keys nobody asked for. `YamlFileSet` describes one `Name*.yml` family (model factory, apply callback, default content, sync key). `YamlFileHub` owns the sets: registration, one Charter `Article<List<string>>` per set for the server push, build and apply, write-back on the author, `HookGame` (load at menu or dedicated start, apply at first spawn). Its internals are split into `YamlFileStore` (discovery in the search folders, reading, default content, atomic write-back), `YamlFileWatcher` (five-second poll reload on a timer thread, handed to the main thread), `YamlGameHooks` (the three Harmony patches, installed once per assembly) and `YamlFileList` (the path/content list shape of the article). `YamlEditorWindow` is the IMGUI editor with live validation and save/apply/discard.
- `SyncedConfig/`: `SyncedConfiguration` facade over Charter, ConfigReload and YamlConfig.
- `PatchGuard/`: `Guard.Install(harmony, log, assembly)` records the mod's logger and assembly (it patches nothing and returns 0). `Guard.Run` / `Guard.Wrap` run an entry point and, when it throws, walk the exception's stack; when the mod assembly (or one of its root namespaces, from the trace text) is on it, the exception is logged under the mod's logger with the given context, then rethrown. `Guard.Report` only logs. `Profiler.Install` times every method the mod patched and reports the most expensive ones. Call Install last in Awake.

## Rules

- Public API changes must keep every consuming mod compiling (EliteCreaturesReborn, FeastMaster, Lockstep, OpenKeep, ShipConfig). Build the mods after touching a library.
- Keep game coupling minimal and explicit. `YamlGameHooks` (behind `YamlFileHub.HookGame`), `TraitStore` and `ItemCopies` are the only places that touch game types on purpose; use reflection (`AccessTools.Field`) instead of publicized assemblies so consumers do not need a publicizer for the library's sake.
- `YamlNode` readers are generic (scalars, lists, maps, enums): no mod vocabulary in the library, neither a consumer's domain nouns nor its setting names such as "world level"; that belongs in the consumer's `YamlModel`. Docs and examples use neutral words (rules, tags, tools).
- `YamlModel` reports problems through `Errors` (files are rejected, the previous configuration stays) and `Warnings` (files are applied). Use the right one.
- Consumers merge these DLLs with ILRepack `Internalize=true`; do not rely on public types being visible across mods.

## Build

`"/c/Program Files/dotnet/dotnet" build -c Release` from the repo root (there is a solution file). `dotnet` is not on PATH in Git Bash on this machine.

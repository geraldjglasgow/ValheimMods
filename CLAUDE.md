# CLAUDE.md - ValheimMods workspace

A workspace of Valheim BepInEx mods and the shared libraries they are built on, in one git repository. The mods
expect `ValheimModLibs` next to them, which the layout guarantees. This file is the workspace guide: what is
here, the standard layout every mod follows, how to build, and how to release. The release steps live only here;
the larger mods also have a `CLAUDE.md` or `PLAN.md` of their own.

## What is here

```
ValheimMods/
  CLAUDE.md          this file
  pack.ps1           packages one mod or all of them (calls each mod's own pack.ps1)
  EliteCreaturesReborn/ creature stars, mutations, attunements, boss aspects, loot rules, world tiers
  FeastMaster/       food and mead values
  ShipConfig/        ship health
  Lockstep/          boss progression gated on the whole group
  OpenKeep/          storage: craft from chests, quick stack, salvage, stacks, container sizes, signs
  Party/             shared parties: membership, chat, health bars, map visibility, friendly-fire protection
  Wayfare/           map-based portal targeting: access modes, favourites, no more tag pairing
  ValheimModLibs/    shared libraries, merged into each mod DLL by ILRepack, never shipped alone
```

### The mods

**Elite Creatures Reborn** (EliteCreaturesReborn): creature stars, mutations, elemental attunements, boss aspects, loot rules, world tier
progression, retaliation zones and multiplayer scaling. Everything is tunable through the .cfg
and two YAML rule files, synced from the server and hot reloaded. It was written black-box (see "Developing mods"
below) and is deliberately not compatible with any other mod's config, save keys or API. Its `CLAUDE.md` has the
architecture and the test checklist.

**FeastMaster**: configure every food and mead. Global multipliers, a section per food and per mead, and a switch
that stops food from degrading. Foods and meads are discovered from the item database.

**ShipConfig**: one health setting per ship prefab, applied to new and already loaded ships. The minimal example
of the shared libraries.

**Lockstep** (published as OathBound up to 0.1.2, renamed to avoid confusion with LionAndOtter's unrelated Oathbound): the next boss stays sealed until every player on the server has defeated the previous one. Per player
credit, a self-maintaining roster with ignore and inactivity rules, altar gate with a spawn guard, admin console
commands, a YAML chain. Design and status in `Lockstep/PLAN.md`.

**OpenKeep**: storage and inventory in one mod: craft from containers, quick stack, sort, salvage, stack sizes,
container sizes, carts as stations, contents signs and shared chests. Design in `OpenKeep/CLAUDE.md`.

**Party**: shared parties - a server-owned roster with one leader, invites, party chat, a draggable health panel,
colored floating names, always-on map pins, friendly-fire protection, a party-only map ping, and a public API for
other mods. Design and the judgement calls the spec left open in `Party/PLAN.md`.

**Wayfare**: replaces portal tag-pairing with map-based targeting - walk into a portal, pick the destination from
the world map, click to teleport through the game's own teleport path. Public/Private/Admin access modes owned by
the acting player, a favourites panel, any mod's portal prefab discovered by component rather than a name list.
Required on the server as well as every client. Design and the judgement calls the spec left open in
`Wayfare/PLAN.md`.

### The libraries (ValheimModLibs)

Small, single-purpose libraries. None is a plugin and players never install them: each mod merges the DLLs it uses
into its own plugin with ILRepack (`Internalize=true`), so mods stay self-contained and cannot conflict on library
versions. `ValheimModLibs/CLAUDE.md` carries the design rules and per-library docs.

| Library | Purpose |
| --- | --- |
| Charter | our own server-to-player binding: the server's values bind every player, stewards amend, articles, join check, `charter` command |
| ConfigReload | write the .cfg at startup, hot reload it on edit |
| YamlConfig | YAML config files with validation, reload, sync, write-back and an in-game editor |
| SyncedConfig | facade over the three above: `Bind`, `BindLocking`, `AddYaml`, `Finish` |
| TraitSets | enum sets in ZDOs, weighted rolls |
| PatchGuard | attribute exceptions from the mod's own code to the mod in the log, then rethrow |
| ItemCopies | write item values into the prefab and every live copy of its shared data, and into new copies |

Using a library from a new mod: add a `ProjectReference` to the library project, list its DLL (and its
dependencies' DLLs) in the mod's `ILRepack.targets`, and follow the pattern in ShipConfig. Build ValheimModLibs
first when a library changed.

## Standard mod layout

Every mod has the same shape. New mods copy it from ShipConfig (the smallest) and rename.

```
<Mod>/
  <Mod>/                the project
    <Mod>.csproj        references the game DLLs, publicizes them, merges the libraries
    <Mod>.cs            plugin entry with the PluginVersion constant
    ILRepack.targets    merge list, copies the DLL to dist/ and to the test profile
    config/             embedded default YAML files, if any
  thunderstore/         everything for the mod store
    manifest.json       name, version_number, description, dependencies
    icon.png            exactly 256x256; larger sources live next to it as icon_source.png (gitignored)
    *.png               header and gallery images for the store page
    <Mod>-X.Y.Z.zip     the release package, written by pack.ps1 (gitignored)
    thunderstore.toml   tcli publish settings: team, community, categories (no secrets)
  dist/                 build output, the merged <Mod>.dll (gitignored)
  README.md             the store page: features, install, console commands, files, building
  CHANGELOG.md          the store changelog: one "## X.Y.Z" section per release, newest first
  PLAN.md               design and roadmap while the mod is unfinished
  pack.ps1              the standard packaging script, identical in every mod (source of truth: any mod's copy)
```

Rules that keep the layout standard:

- The mod name is the folder name, the project folder name, the csproj name, the DLL name and the manifest name.
- Release notes go in `CHANGELOG.md`, never in the README.
- Store assets go in `thunderstore/`, nothing store-related at the mod root.
- Client-only display preferences are bound unsynced so every player decides for themselves; everything that
  changes gameplay is synced and lockable.
- When `pack.ps1` changes, copy the new version into every mod.

## Developing mods

**`CLEANROOM.md` at the root is the boundary every agent and every session works inside: what may be read, what may never be, and how behaviour gets decided. Read it before writing code.** The rest of this section is the same rules in brief.

Mods here are developed black-box, the way Elite Creatures Reborn was rewritten. That means:

- **Behaviour first, in our own words.** Before writing code for a mod that replaces, mirrors or is inspired by
  another mod, write a behaviour specification: what the player sees, every
  setting and what it does, edge cases. The spec is written from playing, from the other mod's public store page
  and documentation, and from the user's requirements. It is a working document; it is deleted once the mod is
  verified against it (the README and the mod's `CLAUDE.md` then carry the behaviour).
- **Never read the other implementation.** No source, decompiled DLL, config file, YAML, save data or API of any
  other mod is read, grepped, quoted or copied. The only code that may be decompiled is the game's own
  `assembly_valheim.dll`, into the scratch folder, to verify signatures.
- **Own names everywhere.** New plugin GUID, config file name, config keys, YAML keys, ZDO keys, console command,
  localization keys and vocabulary. No compatibility layer, migration or reader for another mod's files, even when
  it would be convenient for players switching over.
- **Implement from the spec, verify against the spec.** Every feature is built from the specification and the game
  code alone, then tested in the `LocalTesting` profile against the spec's checklist. A behaviour the spec did not
  cover is decided with the user and written into the mod's `CLAUDE.md`, not guessed from how another mod does it.
- **Multiplayer is not optional.** Every feature in every mod must work on a dedicated server, not only for a
  host or in single player, and it must work when it is first built rather than in a later pass. Decide world
  changes on the owner, draw on every client, keep persistent state in the ZDO so the game replicates it, and
  scope transient RPCs to the clients that could see them. Anything that affects a player but is invisible to
  them is a bug.
- **Shared code lives in the libraries.** Anything two mods need goes into `ValheimModLibs`, written the same way.
- **Small units.** A method is at most 24 lines from brace to brace and does one thing; lambdas and local functions
  count too. A class is at most 300 lines and has one responsibility; split by feature before it gets there. This
  applies to the mods and to `ValheimModLibs`.

## Building

- .NET SDK 8. In Git Bash `dotnet` may not be on PATH: use `"/c/Program Files/dotnet/dotnet"`.
- `dotnet build <Mod>/<Mod>/<Mod>.csproj -c Release`. Override `-p:GamePath=...`, `-p:BepInExCore=...`,
  `-p:ModLibsPath=...` when the layout differs.
- The build merges the libraries into one DLL, writes it to the mod's `dist/`, and copies it to the r2modman profile
  `LocalTesting` (`%APPDATA%\r2modmanPlus-local\Valheim\profiles\LocalTesting\BepInEx\plugins`). Test by launching
  that profile from r2modman; starting the Steam executable directly does not inject BepInEx on this machine. Never
  launch or kill the game from a script.
- Prefer prefix/postfix patches over transpilers. Verify game signatures by decompiling `assembly_valheim.dll` with
  `ilspycmd` into the scratch folder, never into a repository.

## Releasing

The standard process, the same for every mod. Thunderstore rejects a version that already exists, so every upload
needs a new version, and the number must match everywhere the mod records it. `pack.ps1` enforces that.

1. **Write the release notes.** Add a `## X.Y.Z` section at the top of the mod's `CHANGELOG.md`. Versions follow
   semantic versioning (https://semver.org/): `MAJOR.MINOR.PATCH`, three numbers, no prefix or suffix. Bump PATCH
   for fixes that change no setting or file format, MINOR for new features, settings or YAML keys that keep old
   configs working, MAJOR for anything that breaks an existing install: renamed or removed config keys, a changed
   YAML layout, a new plugin GUID, changed save or ZDO keys. `0.y.z` means the mod is still unfinished and anything
   may change; `1.0.0` is the first version whose config and files are promised to stay compatible. A bump resets
   the numbers to its right. Check the git tags or Thunderstore for the last released version and never reuse it.
   Deciding the version is the first step, not the last.
2. **Pack.** From PowerShell, in the mod folder:

   ```
   .\pack.ps1 -Version X.Y.Z
   ```

   or from the workspace root, `.\pack.ps1 -Mod <Mod> -Version X.Y.Z`. The script writes the version into the
   plugin constant, the csproj and `thunderstore/manifest.json` (and the mod's `CLAUDE.md` where it quotes the
   version), refuses to continue if the changelog has no section for it, checks that all locations agree, checks
   that the icon is 256x256, builds Release and writes `thunderstore/<Mod>-X.Y.Z.zip` containing
   `plugins/<Mod>.dll`, `manifest.json`, `icon.png`, `README.md` and `CHANGELOG.md`. Without `-Version` it only
   checks and packs what is already set. It warns about older zips still lying in `thunderstore/`; delete those.
3. **Upload** the zip to Thunderstore with `tcli`, the Thunderstore CLI. See "Uploading" below.
4. **Commit and tag** when the user asks: `git tag <Mod>-vX.Y.Z` (one repository holds all mods, so the tag names the
   mod), so the released versions are discoverable next time.

`.\pack.ps1 -All` at the root packs every mod that has an icon, as a consistency check across the workspace.

### Uploading

The mods are published by the Thunderstore team **MilkyTeam** in the `valheim` community. Uploads go through
`tcli` (installed once with `dotnet tool install -g tcli`, lands in `%USERPROFILE%\.dotnet\tools`). An upload
cannot be undone: a version can be deprecated on the site but never deleted or reused.

1. **Check what the store has** before choosing a version, since nothing is tagged yet for older releases:

   ```
   curl -s https://thunderstore.io/c/valheim/api/v1/package/ | python -c "import sys,json; [print(p['owner'], p['name'], p['versions'][0]['version_number']) for p in json.load(sys.stdin) if p['owner']=='MilkyTeam']"
   ```

   (On 2026-09-14 the store had FeastMaster 4.0.0, ShipConfig 1.1.5, Lockstep 0.2.1, EliteCreaturesReborn 1.2.1, OpenKeep 0.1.0
   and the deprecated OathBound 0.1.2.)
2. **Token.** A service account token (`tss_...`) is created on thunderstore.io under Settings, Teams, MilkyTeam,
   Service Accounts. It lives in the user environment variable `TCLI_AUTH_TOKEN` (`setx TCLI_AUTH_TOKEN "tss_..."`
   in PowerShell, then open a new shell). Never paste it into the chat, a script or the repository. If it is not
   set, ask the user to set it; there is no other way to upload.
3. **Publish config.** Each mod keeps `thunderstore/thunderstore.toml` with the team, the community and the
   categories the package already has on the store (`mods`, `utility` or `tweaks`, `client-side`, `server-side`;
   slugs from `https://thunderstore.io/api/experimental/community/valheim/category/`). Copy FeastMaster's file
   for a mod that has none yet and change the package name and categories. `namespace = "MilkyTeam"` must sit under
   `[package]`: under `[general]` tcli ignores it, submits the package as "AuthorName" and the store rejects it with
   a 400 after the file has already been uploaded.
4. **Upload**, from the workspace root:

   ```
   tcli publish --config-path <Mod>/thunderstore/thunderstore.toml --file <Mod>/thunderstore/<Mod>-X.Y.Z.zip
   ```

   The token comes from the environment variable; `--token` overrides it. Confirm with the user before running it.
5. Afterwards the store page shows the new version within a minute. Then commit and tag as in step 4 above.

### Nexus Mods

The same zips go to Nexus Mods, where the pages belong to the user's Nexus account. Nexus' upload API cannot create a
mod page: for a new mod the user creates the page on nexusmods.com and uploads the first file by hand, once. After
that `nexus-upload.py` at the workspace root adds each new version:

```
python nexus-upload.py --mod <Mod> --list-files    show the page's files, to pick the file_id
python nexus-upload.py --mod <Mod> --dry-run       check the zip, version and changelog
python nexus-upload.py --mod <Mod>                 upload, set the mod version, add the changelog entry
```

It reads the version and zip from `thunderstore/manifest.json`, the changelog text from the top section of
`CHANGELOG.md`, and the page IDs from `<Mod>/thunderstore/nexus.json` (`{"mod_id": N, "file_id": N}`; mod_id is the
number in the page URL). The personal API key from nexusmods.com (Settings, API keys) lives in the user environment
variable `NEXUS_API_KEY`; never in a file or the chat. Confirm with the user before uploading.

The page text (summary and BBCode description) cannot be set through the API. Each mod keeps the current text in
`<Mod>/thunderstore/nexus-description.txt`; when the README changes, update that file too and the user pastes it into
the page's edit form.

The raw API behind tcli is documented at `https://thunderstore.io/api/docs/` (initiate-upload, finish-upload,
submit under `/api/experimental/`), only needed if tcli stops working.

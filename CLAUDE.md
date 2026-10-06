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
  EliteCreaturesPack/ new creatures: the crypt mimic, the Greydwarf Slinger, the Rime Giant
  FeastMaster/       food and mead values
  ShipConfig/        ship health
  Lockstep/          boss progression gated on the whole group
  OpenKeep/          storage: craft from chests, quick stack, salvage, stacks, container sizes, signs; bed respawn, honey
  PackPanel/         the player's inventory: bigger grid, labelled armour/utility/food/ammo slots, key ring, backpacks, look
  Party/             shared parties: membership, chat, health bars, map visibility, friendly-fire protection
  Wayfare/           map-based portal targeting: access modes, favourites, no more tag pairing
  EarthWright/       terraforming: brush size/shape/edge, exact heights, ramps and roads, undo, height limits
  GrindstoneSkills/  deeper skills: Cooking (starred dishes, trash filter, kitchen perks) and a new Sailing skill
  PrestigeWorldwide/ entertainment: proximity voice chat and TVs that play a video link for everyone in sync
  ValheimModLibs/    shared libraries, merged into each mod DLL by ILRepack, never shipped alone
  DevBridge/         test bridge: drive the running game over localhost HTTP (for agents), on Thunderstore for dev profiles
  AssetLab/          dev-only mod, gitignored: unreleased workshop assets tested and balanced in game, never shipped
```

Beside this repository, in `..\ValheimAssets` (its own git repository, moved out of here on 2026-10-02 when it was
`AssetWorkshop`): the asset workshop - 3D assets, effects and sounds from scripts, built to its codex (the game's art,
measured). Its assets sit in `Assets\<category>\<family>\<name>` (Creatures, Weapons, Gear, ...), its tools in `Tools\`,
the codex in `Reference\Codex`; its `README.md` has the layout.

### The mods

**Elite Creatures Reborn** (EliteCreaturesReborn): creature stars, mutations, elemental attunements, boss aspects, loot rules, world tier
progression, retaliation zones and multiplayer scaling. Everything is tunable through the .cfg
and two YAML rule files, synced from the server and hot reloaded. It was written black-box (see "Developing mods"
below) and is deliberately not compatible with any other mod's config, save keys or API. Its `CLAUDE.md` is the
player reference the README links to: mutations, boss aspects, rule file fields and console commands.

**Elite Creatures Pack** (EliteCreaturesPack): new creatures, each with its own fight - the crypt mimic (a crypt
chest that bites its opener), the Greydwarf Slinger (shoots stones, keeps its distance) and the Rime Giant (a rare
frost-plated troll asleep on the mountains, whose plates only fire breaks). Models from `ValheimAssets`, embedded
per platform through BundlePrefabs; a .cfg section per creature, synced. Split out of Elite Creatures Reborn on
2026-09-28 before release: new creatures go here, not into ECR. Neither mod needs the other; with both, ECR rolls
stars and mutations on them and holds back a dormant mimic's looks (key names only: `ecp_disguised`, `ecr_gen`).
"The skeleton arsenal" is every bone weapon in it: the bone dagger, sword, axe, mace, spear, atgeir, both bows, the
arrow, the spine, the Bone Crossbow and its blunted bolt, and the Executioner's Greataxe (three bundles; one build,
`../ValheimAssets/Assets/Weapons/SkeletonArsenal/ecp_skel_arsenal/build.ps1`). Design in `EliteCreaturesPack/CLAUDE.md` and `features/`.

**FeastMaster**: configure every food and mead. Global multipliers, a section per food and per mead, and a switch
that stops food from degrading. Foods and meads are discovered from the item database.

**ShipConfig**: one health setting per ship prefab, applied to new and already loaded ships. The minimal example
of the shared libraries. Its ship panel (HUD, under the minimap while aboard) reads GrindstoneSkills' public
`GrindstoneSkills.Api.SailingApi` by reflection (`Panel/GrindstoneLink.cs`) for the Sailing bonuses and abilities;
changing that API's endpoints needs a matching ShipConfig change.

**Lockstep** (published as OathBound up to 0.1.2, renamed to avoid confusion with LionAndOtter's unrelated Oathbound): the next boss stays sealed until every player on the server has defeated the previous one. Per player
credit, a self-maintaining roster with ignore and inactivity rules, altar gate with a spawn guard, admin console
commands, a YAML chain. Design and status in `Lockstep/PLAN.md`.

**OpenKeep**: storage in one mod: craft from containers, quick stack, sort, salvage, stack sizes,
container sizes, carts as stations, contents signs and shared chests; plus a Homestead section outside storage
(respawn at the nearest owned bed, campfires on wooden floors, honey per day, fires refuelling from nearby chests,
torches lit only at night, a shorter world save freeze), a build camera that flies free near a crafting station, and blueprints (section 14, off
by default): a Blueprints tab in the game's hammer lists saved builds (DevBridge's blueprint JSON, in folders,
renamed with F2) and places one as a ghost construction site with the ground shaped to fit, built as materials are
handed over; plus Fix ground, a Site planner (build queue, one house at a time) and Copy building (select buildings
in the world, save them as a blueprint). Design in `OpenKeep/CLAUDE.md` and
`OpenKeep/SPEC-Blueprints.md`.

**PackPanel**: the player's own inventory and its UI, kept separate from the storage mod: a bigger grid, labelled
slots always on screen (armour, a backpack, worn utilities, food, meads, ammo, a coin purse), a key ring, eight
craftable backpacks worn on the back, the stat boxes beside the grid and under the minimap, and the brown or timber
look. Built as OpenKeep's section 10 and moved out before release (2026-09-28). New inventory and inventory UI work
goes here, not into OpenKeep. Works alone; with OpenKeep the two cooperate through published data only (a custom data
key, a GameObject name, config entries read through the chainloader), never a reference. Design in
`PackPanel/CLAUDE.md`.

**Party**: shared parties - a server-owned roster with one leader, invites, party chat, a draggable health panel,
colored floating names, always-on map pins, friendly-fire protection, a party-only map ping, and a public API for
other mods. Design and the judgement calls the spec left open in `Party/PLAN.md`.

**Wayfare**: replaces portal tag-pairing with map-based targeting - walk into a portal, pick the destination from
the world map, click to teleport through the game's own teleport path. Public/Private/Admin access modes owned by
the acting player, a favourites panel, any mod's portal prefab discovered by component rather than a name list,
and quick jumps (near portals quicker, no loading screen into a loaded area; moved from OpenKeep on 2026-10-04).
Required on the server as well as every client. Design and the judgement calls the spec left open in
`Wayfare/PLAN.md`.

**GrindstoneSkills**: deeper versions of the game's own skills, Cooking first (Farming and Crafting later). Dishes get
0 to 3 stars rolled from the cook's level (stored in the item's quality field), stars boost food, a per-kitchen trash
filter keeps only the stars you want, and the cook's level speeds cooking and fermenting and gives extra food.
Sailing is a skill of the mod's own (the game has none): ship health from the builder's level, speed from the
helmsman's, a wider map reveal aboard, and a level 50 lookout pulse that shows enemy name tags around the ship.
Husbandry is another own skill: taming, calm creatures, breeding (better offspring and twins), animal yield and an
Animal Feeder piece; its "star up" key on a pregnant parent is read by Elite Creatures Reborn (`Breeding/KeeperBonus.cs`).
Skill loss on death is configurable for every skill. Design in `GrindstoneSkills/PLAN.md`.

**EarthWright**: terraforming for the hoe and cultivator - brush size, shapes, edges and exact target heights,
level/raise/lower/smooth/paint/reset, ramps and curved roads, undo, costs, height limits per biome, ward and zone
protection and new menu entries. Required on the server and every client: edits travel as EarthWright's
own RPC to the owner of each terrain compiler. Design, module map and decisions in `EarthWright/PLAN.md`; code map
in `EarthWright/CLAUDE.md`.

**PrestigeWorldwide**: entertainment, two features and nothing else (the user's scope): proximity voice chat (talk,
shout, whisper, party radio with Party; Opus through the merged Concentus library, relayed by the server to players in
range) and TV pieces (a slim white rectangle for now, three sizes) that play a direct video link (.webm/.mp4, no
YouTube) for everyone nearby in sync on the mod's own server clock, with a remote copied from the game's text input
window. Required on the server and every client. Design and status in `PrestigeWorldwide/PLAN.md`.

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
| PatchGuard | attribute exceptions from the mod's own code to the mod in the log, then rethrow |
| ItemCopies | write item values into the prefab and every live copy of its shared data, and into new copies |
| PlateColumn | the inventory's stat plates as one column any mod adds a plate to, each with a tooltip; embedded PNG icons |
| BundlePrefabs | a mod's own models: embedded asset bundle per platform, prefab copies, ZNetScene/ObjectDB registration on every peer, game materials |
| LocalEffects | local, cosmetic copies of the game's effect prefabs: never networked, never a damage source, thinned by a density |
| PlayerGrid | PackPanel's published main grid (`PackPanel.mainGrid`): which player inventory cells are slots, written and read in one place |
| Hotkeys | hotkeys read one way: shortcuts fire while W is held, only their own modifiers count, nothing fires while the player types |
| AreaLoading | arriving somewhere far: land and objects loaded fast during a jump or respawn, the server-objects settle check, distance-scaled waits |
| WindowInput | a mod's own window treated like the game's while open: free cursor, no attacks, mouse look or zoom, Esc closes it |
| MapClicks | a click on a mod's own map icon waits out the double click window; the game's pins under it can still be placed and removed |
| EliteCraftingLink | EliteCrafting's public API by reflection: typed wrappers (classes, inscriptions, items, hooks) that do nothing without EliteCrafting |

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
    wiki/               the store's Wiki tab, one NN-title.md per page (see "Thunderstore wiki")
  dist/                 build output, the merged <Mod>.dll (gitignored)
  README.md             the store page (Thunderstore Details tab), very short: what it does, features, install,
                        configuration, links, shout outs; no Building section
  CHANGELOG.md          the store changelog: one "## X.Y.Z" section per release, newest first, one short line per change
  PLAN.md               design and roadmap while the mod is unfinished
  pack.ps1              the standard packaging script, identical in every mod (source of truth: any mod's copy)
```

Rules that keep the layout standard:

- The mod name is the folder name, the project folder name, the csproj name, the DLL name and the manifest name.
- Release notes go in `CHANGELOG.md`, never in the README.
- The README and the changelog are very short and concise: a README of about 250 words besides the standard shout
  outs (one line per feature, no setting-by-setting lists, no Building section; the .cfg describes every setting), a
  changelog section of about 100 words (one line per change, what changed and not how). The `valheim-release` skill
  has the README shape and checks both.
- Store assets go in `thunderstore/`, nothing store-related at the mod root.
- Client-only display preferences are bound unsynced so every player decides for themselves; everything that
  changes gameplay is synced and lockable.
- When `pack.ps1` changes, copy the new version into every mod.

## Developing mods

### Clean room: only while `CLEANROOM.md` exists

**When `CLEANROOM.md` is at the root, it is the boundary every agent and every session works inside: what may be
read, what may never be, and how behaviour gets decided. Read it before writing code and follow it. When there is
no `CLEANROOM.md`, nothing in this subsection applies:** another mod's DLL, config or documentation may be read or
decompiled (into the scratch folder, never into a repository) to understand how it does something.

While it exists, mods are developed black-box, the way Elite Creatures Reborn was rewritten. In brief:

- **Behaviour first, in our own words.** Before writing code for a mod that replaces, mirrors or is inspired by
  another mod, write a behaviour specification: what the player sees, every
  setting and what it does, edge cases. The spec is written from playing, from the other mod's public store page
  and documentation, and from the user's requirements. It is a working document; it is deleted once the mod is
  verified against it (the mod's `CLAUDE.md` then carries the behaviour; the README only summarises it).
- **Never read the other implementation.** No source, decompiled DLL, config file, YAML, save data or API of any
  other mod is read, grepped, quoted or copied. The only code that may be decompiled is the game's own
  `assembly_valheim.dll`, into the scratch folder, to verify signatures. One exception: an integration API a
  mod's author published for other mods to compile against may be staged by the user into `~/thirdpartyapis/`
  (outside the repo) and read when the user asks for an integration - `CLEANROOM.md` has the limits.
- **Own names everywhere.** New plugin GUID, config file name, config keys, YAML keys, ZDO keys, console command,
  localization keys and vocabulary. No compatibility layer, migration or reader for another mod's files, even when
  it would be convenient for players switching over.
- **Implement from the spec, verify against the spec.** Every feature is built from the specification and the game
  code alone, then tested in the `LocalTesting` profile against the spec's checklist. A behaviour the spec did not
  cover is decided with the user, not guessed from how another mod does it.

### Always

- **Multiplayer is not optional.** Every feature in every mod must work on a dedicated server, not only for a
  host or in single player, and it must work when it is first built rather than in a later pass. Decide world
  changes on the owner, draw on every client, keep persistent state in the ZDO so the game replicates it, and
  scope transient RPCs to the clients that could see them. Anything that affects a player but is invisible to
  them is a bug.
- **Shared code lives in the libraries.** Anything two mods need goes into `ValheimModLibs`, written the same way.
- **Small units.** A method is at most 24 lines from brace to brace and does one thing; lambdas and local functions
  count too. A class is at most 300 lines and has one responsibility; split by feature before it gets there. This
  applies to the mods and to `ValheimModLibs`.
- **New assets live outside the mods until they ship.** Every model, creature, effect, sound, texture and bundle is
  made and kept in `../ValheimAssets` (its source in `Assets/<category>/<family>/<name>/`, `Assets/Effects/`,
  `Assets/Sounds/`; its builds in gitignored `out/` folders), never in a mod's folder. It goes into a mod (the bundle copied into
  `<Mod>/.../assets/bundles`, the mod code that uses it) only when the user decides to release it in that mod, and in
  the same change as that release. Concepts, trials, demos and rejected versions never enter a mod, so nothing
  unreleased is pushed with one or left in it as dead code or dead assets; a mod that stops using an asset or its code
  has both removed. Build and install flags (`-Install`, `--install`) are for that release step only. Until then an
  asset is tried and balanced in the game through `AssetLab`, a gitignored dev-only mod installed into `LocalTesting`
  only: its bench embeds the bundle straight from the workshop's `out/` folders and is written like the mod code it
  will become, so it moves over unchanged when it ships (`AssetLab/README.md`, "Graduating a bench").

## Building

- **Blender viewing preference:** whenever the user asks to open, show or preview something in Blender, show the
  model with its colours and textures visible. Set the viewport to Material Preview (or Rendered with suitable
  lighting), ensure its textures load, and frame the model. Apply this to the window being opened, not just a PNG
  render; use the detailed viewing instructions in `.claude/skills/valheim-asset/SKILL.md`.
- .NET SDK 8. In Git Bash `dotnet` may not be on PATH: use `"/c/Program Files/dotnet/dotnet"`.
- `dotnet build <Mod>/<Mod>/<Mod>.csproj -c Release`. Override `-p:GamePath=...`, `-p:BepInExCore=...`,
  `-p:ModLibsPath=...` when the layout differs.
- The build merges the libraries into one DLL, writes it to the mod's `dist/`, and copies it to the r2modman profile
  `LocalTesting` (`%APPDATA%\r2modmanPlus-local\Valheim\profiles\LocalTesting\BepInEx\plugins`). Test by launching
  that profile from r2modman; starting the Steam executable directly does not inject BepInEx on this machine. Never
  launch or kill the game from a script.
- Prefer prefix/postfix patches over transpilers. Verify game signatures by decompiling `assembly_valheim.dll` with
  `ilspycmd` into the scratch folder, never into a repository.
- 3D models, textures, particle effects, sounds and asset bundles come from `../ValheimAssets` (Blender and Unity
  6000.0.75f1 in `%USERPROFILE%\tools`, both run headless). A new asset is built to its codex,
  `Reference/Codex/` there (the game's own art measured: budgets, paint, palettes, shaders, rigs, effects, sounds;
  `look.md` first) from a filled-in `Reference/Codex/BRIEF.md`, then checked with the style check and a lineup beside
  the game's own. Build with `..\ValheimAssets\build.ps1 -Asset <name>` (an asset is found by its folder name wherever
  it sits under `Assets`) and check a model by reading its `out\preview.png` and `out\lineup\`. The workshop's
  `README.md` has the conventions and what is not built yet.
- Test in the running game through `DevBridge` (here installed in `LocalTesting` only): once the user has started the
  profile, `curl -s http://127.0.0.1:7780/help` lists endpoints for screenshots, the UI tree, clicks, keys, mouse,
  console commands, the log, reflection and ZDOs. `DevBridge/REFERENCE.md` has the test loop and the limits. A rebuilt
  mod loads only after the user restarts the game. Before building, editing terrain or taking screenshots through it,
  read `DevBridge/AGENT-NOTES.md`: placing real pieces, piece measurements, support, and the traps (`/frame` moves the
  player), with working scripts in `DevBridge/examples/castle/`.

## Releasing

The standard process, the same for every mod. Thunderstore rejects a version that already exists, so every upload
needs a new version, and the number must match everywhere the mod records it. `pack.ps1` enforces that. The
`valheim-release` skill walks these steps; its `release_status.py` shows which mods have changes since their upload.

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
4. **Commit, tag and push**, as part of every release (otherwise only when the user asks): `git tag <Mod>-vX.Y.Z`
   (one repository holds all mods, so the tag names the mod), so the released versions are discoverable next time.

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

   The token comes from the environment variable; `--token` overrides it. A release request is the confirmation
   (the `valheim-release` skill, "A release request is the go-ahead"); confirm only an upload nobody asked for.
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
variable `NEXUS_API_KEY`; never in a file or the chat. A release uploads to Nexus without asking for every mod with a
`nexus.json`.

The page text (summary and BBCode description) cannot be set through the API. Each mod keeps the current text in
`<Mod>/thunderstore/nexus-description.txt`; when the README changes, update that file too and the user pastes it into
the page's edit form. Nexus accepts a summary of at most 350 characters.

The raw API behind tcli is documented at `https://thunderstore.io/api/docs/` (initiate-upload, finish-upload,
submit under `/api/experimental/`), only needed if tcli stops working.

### Thunderstore wiki

Each released mod has a player wiki (the store page's Wiki tab) kept in `<Mod>/thunderstore/wiki/`: one Markdown file
per page, `NN-title.md` (NN orders them, `01-` is the home page), first line `# Page Title`, links to another page of
the same wiki written `[text](wiki:Page Title)`. It describes the released version only, for players and server admins.
When a release changes what a page says, update the page in the same release. `wiki-upload.py` posts them:

```
python wiki-upload.py --mod <Mod> --dry-run   check the pages and links, show what would be created or updated
python wiki-upload.py --mod <Mod>             create and update the pages (matched by title), rewrite the links
python wiki-upload.py --all                   every mod with a wiki folder; --prune deletes store pages with no file
```

Thunderstore lets only a team member's own login edit a wiki: the `TCLI_AUTH_TOKEN` service account is refused (403).
The script uses the user's browser session instead, from the user environment variable `THUNDERSTORE_SESSION` (the
`sessionid` cookie of thunderstore.io, set with `setx`); never in a file or the chat. It stops working when the user
logs out. Confirm with the user before uploading.

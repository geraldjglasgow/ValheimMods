# OpenKeep build plan

The behaviour is in `SPEC.md`. This file is the engineering plan: the module layout, the shared core contract every
module codes against, the rules every implementing agent follows, and the build order.

## Rules for every agent

- **Clean room.** Read only: `SPEC.md`, this file, `../CLAUDE.md`, `../ValheimModLibs/` (the shared libraries and
  their docs), the other mods of this workspace (`../ShipConfig`, `../FeastMaster`, `../EliteCreaturesReborn`,
  `../Lockstep`) as style references, and the decompiled game in the scratch folder named in your prompt. No
  other mod's source, DLL, config or documentation is read; only this workspace, the shared libraries and the
  game's own decompiled assembly. No store pages, no wikis about other mods, no web search for how another mod
  does it. If a question cannot be answered from the spec and the game code,
  decide it yourself in the spirit of the spec and record the decision in `CLAUDE.md` under "Decisions".
- **Own vocabulary.** Names from the spec only; no compatibility with any other mod's keys, files or API.
- **Code rules.** Functions up to 24 lines from brace to brace (lambdas and local functions count), classes up
  to 300 lines, one responsibility per class, one class per file unless a patch class belongs to a small helper.
  Prefix, postfix and finalizer patches only; no transpilers. Zero compiler warnings from the mod's code.
  Settings are read at use time (`.Value`), never cached. Nullable is off in this project.
- **Verify every game signature** against the decompiled sources in the scratch folder before patching it, and
  read the game method's body so the patch fits how the game really behaves (ownership, RPCs, save paths).
- **Write complete files** (the Write tool), not partial edits, so the tree is never half-written for the other
  agents building at the same time. Stay inside your module folder(s) plus the files this plan assigns to you.
  Never edit `Plugin.cs`, the csproj or another module's folder; if you need something there, say so in your
  final report.
- **Build** from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build OpenKeep/OpenKeep/OpenKeep.csproj -c Release` (Git Bash). Other agents
  build the same project concurrently: a failure caused by a locked file or by a compile error in a folder that
  is not yours is not yours to fix; wait a minute and retry, up to five times, then report it. Your module must
  compile with zero warnings before you finish.
- **Report** at the end: what is built, what is not, every decision the spec left open, the game methods patched,
  and what the tester has to look at. Do not launch or kill the game.

## Layout

```
OpenKeep/OpenKeep/src/
  Plugin.cs                 entry (do not edit): calls <Module>Module.Initialize for every module, patches per class
  Core/                     shared by every module (contract below)
    CoreModule.cs           section 0 settings (Ships, Carts, Player Chests, Honour Wards), console command
    Language.cs             $ok_ words (exists)
    ContainerScan.cs        nearby container discovery, usability, ownership claim, save
    ContainerRules.cs       per prefab enabled flag registry (filled by Reach's YAML), the section 0 switches
    ItemMatcher.cs          the item vocabulary: one matcher
    ItemMatchSet.cs         a list of matchers with group resolution
    ItemGroups.cs           the groups: map of a YAML file
    ItemNames.cs            prefab name and display name of an item
    CharacterData.cs        string sets in the player's custom data (OpenKeep.<key>)
    Keys.cs                 KeyboardShortcut helpers, inventory-open and text-input checks
    Messages.cs             centre and top-left messages
    Command.cs              the openkeep console command
  Reach/                    section 1
  Stow/                     section 2
  Salvage/                  section 3
  Stacks/                   section 4
  Capacity/                 section 5
  Carts/                    section 6
  Shared/                   section 9 (several players in one chest; built after the others)
OpenKeep/OpenKeep/config/   embedded default YAML files (OpenKeep.Reach.yml, OpenKeep.Stow.yml, OpenKeep.Salvage.yml,
                            OpenKeep.Stacks.yml, OpenKeep.Containers.yml); embedded automatically by the csproj as
                            resource "OpenKeep.config.<file>"
```

Each module has `<Module>Module.Initialize(SyncedConfiguration synced)` (already stubbed) that binds the module's
settings (a `<Module>Settings` static class with `ConfigEntry` properties), registers its YAML set with
`synced.AddYaml(new YamlFileSet("OpenKeep.<Module>*.yml", "openkeep_<module>", () => new <Module>Model(), Apply)
{ DefaultContent = SyncedConfiguration.EmbeddedResource(assembly, "OpenKeep.config.OpenKeep.<Module>.yml") })`,
and registers its words with `Language.Add("ok_<word>", "English")`. Patches are `[HarmonyPatch]` classes anywhere
in the module folder; `Plugin.PatchEverything` finds them.

## Core contract (namespace `OpenKeep.Core`)

Built first by the Core agent; every other module calls it and nothing else outside its own folder.

```csharp
public enum ContainerUse { Reach, Stow }

public static class ContainerScan
{
    // Every usable container within range of a position, nearest first. Never throws; unusable ones are skipped.
    public static List<Container> Nearby(Vector3 position, float range, ContainerUse use);
    // Usable: Container component; valid ZNetView; inventory read from the ZDO at least once; not in use by another
    // player (the container the local player has open counts as usable); the game's privacy check for the local
    // player; the ward check when CoreSettings.HonourWards and the container checks guard stones; the prefab is
    // enabled (ContainerRules.IsEnabled); the section 0 switches for ships, carts and the private chest.
    public static bool IsUsable(Container container, ContainerUse use);
    // Every loaded container (tracked by Container.Awake / OnDestroy patches), no usability filter.
    public static IReadOnlyCollection<Container> All();
    // Claims ownership of the container's ZDO for the local client the way the game does when a chest is opened.
    // True when the local client owns it afterwards. Call before changing the inventory.
    public static bool Claim(Container container);
    // Persists a changed inventory (the game's save path) so every client sees it.
    public static void Save(Container container);
    public static string PrefabName(Container container);       // prefab name without "(Clone)"
    public static bool IsShip(Container c); public static bool IsCart(Container c); public static bool IsPrivateChest(Container c);
}

public static class ContainerRules
{
    // Reach's YAML fills this; default is enabled for every prefab. Stow reads it too.
    public static void SetEnabled(IReadOnlyDictionary<string, bool> byPrefab);
    public static bool IsEnabled(string prefabName);
}

public static class CoreSettings   // section "0. Containers", all synced
{
    public static ConfigEntry<bool> Ships, Carts, PlayerChests, HonourWards;
}

public sealed class ItemGroups      // the groups: map of one YAML file
{
    public static ItemGroups Empty { get; }
    public void Read(YamlNode groupsNode);             // name -> list of matcher strings; unknown shapes are Errors on the model
    public bool Matches(string group, ItemDrop.ItemData item);   // recursive through group: members, cycle safe
    public IEnumerable<string> Names { get; }
}

public sealed class ItemMatcher     // one entry of the vocabulary
{
    public static ItemMatcher Parse(string text, ItemGroups groups);   // never throws; "" matches nothing
    public bool Matches(ItemDrop.ItemData item);
    public bool Matches(string prefabName, ItemDrop.ItemData.SharedData shared);
    public bool IsExactName { get; }                    // a plain prefab or $token name (specific wins over patterns)
    public override string ToString();
}

public sealed class ItemMatchSet
{
    public static ItemMatchSet Parse(IEnumerable<string> entries, ItemGroups groups);
    public static ItemMatchSet Empty { get; }
    public bool IsEmpty { get; }
    public bool Matches(ItemDrop.ItemData item);
    public bool Matches(string prefabName, ItemDrop.ItemData.SharedData shared);
}

public static class ItemNames
{
    public static string PrefabName(ItemDrop.ItemData item);    // m_dropPrefab name, else the shared name token
    public static string DisplayName(ItemDrop.ItemData item);   // localized m_shared.m_name
    public static bool SameItem(ItemDrop.ItemData a, ItemDrop.ItemData b);  // same shared name (stacking identity)
}

public static class CharacterData   // Player.m_localPlayer.m_customData under "OpenKeep.<key>"
{
    public static HashSet<string> GetSet(string key);
    public static void SetSet(string key, IEnumerable<string> values);
    public static bool GetFlag(string key); public static void SetFlag(string key, bool value);
}

public static class Keys
{
    public static bool Pressed(ConfigEntry<KeyboardShortcut> key);   // this frame, modifiers honoured, no text input active
    public static bool Held(ConfigEntry<KeyboardShortcut> key);      // main key held (for modifiers such as LeftShift)
    public static bool InventoryOpen { get; }                        // InventoryGui visible
    public static bool TextInputActive { get; }                      // chat, console or any input field focused
}

public static class Messages
{
    public static void Center(string text); public static void TopLeft(string text);   // localized through Language
}
```

## Build order and agents

1. **Core** (this contract, `CoreModule` settings, the `openkeep containers` and `openkeep reload` commands;
   `openkeep write docs` calls `OpenKeep.Stacks.Documentation.Write()` if that type exists, via reflection).
2. In parallel with 1, since they do not need the core: **Stacks + Capacity** (one agent), **Salvage** (one agent;
   uses `ItemMatchSet`/`ItemGroups` for its YAML, so it codes against the contract above and builds after Core
   lands, or ships its YAML model last).
3. After 1: **Reach + Carts** (one agent) and **Stow** (one agent).
4. **Integration**: one agent runs the whole build, checks every module against the spec's checklist by reading,
   writes `CLAUDE.md` (code map, patched methods, decisions, test checklist) and `README.md` (the store page), and
   fills `thunderstore/nexus-description.txt`. Then the user tests in game.

Version 0.1.0 until the in-game checklist passes.

# YamlConfig

YAML configuration files for Valheim mods, done once: parsing with path-qualified messages, unknown key warnings, groups, server to client sync, write-back, five-second reload and an in-game editor.

## YamlModel and YamlNode

Derive a model, read what you need from the root map, and add checks that span entries in `Verify`. Errors reject the files (the previous configuration stays); warnings are logged and the files still apply. Keys a model never asks for are warned about automatically.

```csharp
public sealed class RuleModel : YamlModel
{
    public readonly Dictionary<string, float> chances = new();

    protected override void Read(YamlNode root)
    {
        ReadGroups(root);                                   // groups: Tools: [Hammer, Hoe]
        foreach (YamlNode rule in root.Get("rules").Items)
        {
            if (!rule.Get("name").TryString(out string name))
                rule.Error("a rule needs a name");            // "rules[2]: a rule needs a name"
            if (rule.Get("chance").TryFloat(out float chance))
                chances[name] = chance;
            rule.Get("limits").Get("Count").TryInt(out int count);  // chains: nothing is reported for missing keys
        }
    }

    protected override void Verify()
    {
        if (chances.Count == 0) Errors.Add("no rules");
    }
}
```

`YamlNode`: `Kind` (Scalar, List, Map, Null, Missing), `Path`, `Text`, `Get(key)` (case-insensitive), `Entries`, `Items`, `Count`, `Is("inherit")`, `TryInt`, `TryFloat`, `TryBool` (true/false, yes/no, on/off, 1/0), `TryString`, `TryEnum<T>`, `TryList<T>`, `TryStringList` (one scalar counts as a one-item list), `Line`, `Error`, `Warn`, `WarnUnknownKeys`, `ErrorUnknownKeys`. A `Missing` node reads nothing and reports nothing; a `Null` node (key without value) is an error when read.

`ResolveGroups("Hammer")` returns the groups a name belongs to, innermost first (`Tools`, then a group containing `Tools`), safe against cycles.

### Lines, one entry at a time, misspelt keys

For a file of many independent entries (one creature, one rule each), three opt-in tools let a model leave out only the
entry with a mistake and say where it is:

```csharp
public sealed class EntryModel : YamlModel
{
    protected override bool TrackLines => true;               // messages carry the line: "rules[3].chance (line 12): ..."

    protected override void Read(YamlNode root)
    {
        foreach (YamlNode entry in root.Get("rules").Items)
        {
            List<string> problems = CollectErrors(() =>           // errors inside go here, not to Errors
            {
                ReadRule(entry, CurrentFile);                       // CurrentFile: the file being read (name only)
                entry.ErrorUnknownKeys();                           // a misspelt key is an error at its own path
            });
            problems.ForEach(problem => entry.Warn("rule left out: " + problem));   // the rest of the files still apply
        }
    }
}
```

- `TrackLines` (off by default: each file is parsed a second time, through YamlDotNet's representation model, for
  positions; `YamlLineIndex`): `YamlNode.Line` answers (a map entry's key line, a list item's first line; 0 when unknown)
  and every `Error`/`Warn` names it.
- `CollectErrors(read)`: errors reported while `read` runs (typed readers, shape checks, `Error`, `ErrorUnknownKeys`)
  are returned instead of rejecting the files; they carry the path and line but not the file, so report them again
  through a node's `Warn` or `Error`, which adds it. Warnings are unaffected; calls nest, the innermost collects.
- `ErrorUnknownKeys()`: like `WarnUnknownKeys`, but each unknown key in the visited maps under the node is an error at
  its own path; maps reported this way are not warned about again by the automatic pass.
- `YamlFileSet.SearchSubfolders`: also find the set's files in the subfolders, at any depth, of the first search folder
  (the config folder for `SyncedConfiguration`); the other search folders are still searched without theirs. A subfolder
  that cannot be listed leaves the search at the folder itself. The five-second watcher searches the same way.

## YamlFileSet and YamlFileHub

```csharp
// Awake
hub = new YamlFileHub("MyMod", Logger, new[] { Paths.ConfigPath, pluginFolder }, charter)
{
    CanApply = () => ZNetScene.instance != null,
};
rules = hub.Register(new YamlFileSet("MyMod.Rules*.yml", "mymod.rules",
    () => new RuleModel(), model => Apply((RuleModel)model))
{
    DefaultContent = () => ReadEmbedded("MyMod.Rules.yml"),   // written when no file exists
    Enabled = () => useYaml.Value,
    EditorLabel = () => "Edit rules",
});
hub.HookGame(harmony, Priority.Normal);   // load at the main menu or dedicated server start, apply at first spawn
```

The hub finds `MyMod.Rules*.yml` in every search folder (main file first), parses all files into one model, and publishes the contents through a Charter article named by the sync key (an ordinary article: it travels only while the server's binding is on). Every side reacts to that article: players receive the server's files while bound and keep their own files otherwise, the server applies its own. Files are parsed once per load, reload or save: the model built to validate them is the one applied (only files received from the server are parsed on arrival). Edits on disk are picked up every five seconds and published without being written back; the editor's save is written back atomically on the author. `Applied` fires after each apply; `ApplyAll()` re-applies the current models.

With `SyncedConfiguration` (the `SyncedConfig` library) all of this is `config.AddYaml(new YamlFileSet(...))` and `config.Finish(harmony)`.

## YamlEditorWindow

```csharp
editor = new YamlEditorWindow(hub, "MyMod YAML Editor") { Translate = Localization.instance.Localize };
YamlEditorHost.Add(gameObject, editor);     // draws it and keeps the cursor free, only while it is open
// ConfigurationManager: new ConfigurationManagerAttributes { CustomDrawer = _ => editor.DrawButtons() }
```

Unity runs an IMGUI pass every frame for every enabled behaviour with an `OnGUI`, even one that draws nothing, so the
window is drawn by a `YamlEditorHost` component that `Open` enables and `Close` disables; a plugin needs no `OnGUI`
or `Update` for it. `SyncedConfiguration` adds the host for its `YamlEditor` to the plugin's object. Without a host,
call `editor.Update()` from Update and LateUpdate and `editor.OnGUI()` from OnGUI; with one, `OnGUI()` does nothing,
so a plugin that still calls it does not draw the window twice.

Full screen, one text area per file (Tab indents two spaces, Enter keeps the indentation), a validation panel that re-parses on every change, and `Save and apply`, `Apply without saving`, `Discard`. Saving and applying are only enabled for the author while there are no errors.

Depends on Charter (this repo), YamlDotNet 13.7.1, BepInEx, Harmony, `assembly_valheim` and UnityEngine IMGUI. Merge `YamlConfig.dll`, `Charter.dll` and `YamlDotNet.dll` into your plugin with ILRepack.

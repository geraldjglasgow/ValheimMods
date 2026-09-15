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

`YamlNode`: `Kind` (Scalar, List, Map, Null, Missing), `Path`, `Text`, `Get(key)` (case-insensitive), `Entries`, `Items`, `Count`, `Is("inherit")`, `TryInt`, `TryFloat`, `TryBool` (true/false, yes/no, on/off, 1/0), `TryString`, `TryEnum<T>`, `TryList<T>`, `TryStringList` (one scalar counts as a one-item list), `Error`, `Warn`. A `Missing` node reads nothing and reports nothing; a `Null` node (key without value) is an error when read.

`ResolveGroups("Hammer")` returns the groups a name belongs to, innermost first (`Tools`, then a group containing `Tools`), safe against cycles.

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

The hub finds `MyMod.Rules*.yml` in every search folder (main file first), parses all files into one model, and publishes the contents through a Charter article named by the sync key (an ordinary article: it travels only while the server's binding is on). Every side reacts to that article: players receive the server's files while bound and keep their own files otherwise, the server applies its own. Edits on disk are picked up every five seconds and published without being written back; the editor's save is written back atomically on the author. `Applied` fires after each apply; `ApplyAll()` re-applies the current models.

With `SyncedConfiguration` (the `SyncedConfig` library) all of this is `config.AddYaml(new YamlFileSet(...))` and `config.Finish(harmony)`.

## YamlEditorWindow

```csharp
editor = new YamlEditorWindow(hub, "MyMod YAML Editor") { Translate = Localization.instance.Localize };

void Update()     => editor.Update();       // also from LateUpdate: keeps the cursor free
void OnGUI()      => editor.OnGUI();
// ConfigurationManager: new ConfigurationManagerAttributes { CustomDrawer = _ => editor.DrawButtons() }
```

Full screen, one text area per file (Tab indents two spaces, Enter keeps the indentation), a validation panel that re-parses on every change, and `Save and apply`, `Apply without saving`, `Discard`. Saving and applying are only enabled for the author while there are no errors.

Depends on Charter (this repo), YamlDotNet 13.7.1, BepInEx, Harmony, `assembly_valheim` and UnityEngine IMGUI. Merge `YamlConfig.dll`, `Charter.dll` and `YamlDotNet.dll` into your plugin with ILRepack.

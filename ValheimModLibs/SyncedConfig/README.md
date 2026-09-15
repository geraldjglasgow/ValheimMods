# SyncedConfig

The one object most mods actually want: Charter plus everything that is missing around it.

```csharp
[BepInPlugin(GUID, Name, Version)]
public class MyPlugin : BaseUnityPlugin
{
    SyncedConfiguration config = null!;

    void Awake()
    {
        config = new SyncedConfiguration(this, Logger, Name, Version);   // also: oldestAccepted (default: Version), mandatory (default: true)

        // synced by default, bound for players while the locking entry is true on the server
        var locked    = config.BindLocking("General", "Bind Players", true, "Server only. When on, every player uses the server's values.");   // bool only
        var chance    = config.Bind("Feature", "Example chance", 10f, "Percent.", acceptableValues: new AcceptableValueRange<float>(0f, 100f));
        var localOnly = config.Bind("Display", "Example toggle", true, "Client side.", synced: false);

        // optional YAML files, synced and hot reloaded the same way; RuleModel derives from YamlModel (see YamlConfig)
        var rules = config.AddYaml(new YamlFileSet("MyMod.Rules*.yml", "mymod.rules", () => new RuleModel(), model => Apply((RuleModel)model))
        {
            DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(MyPlugin).Assembly, "MyMod.Rules.yml"),   // logical resource name
        });

        Harmony harmony = new(GUID);
        harmony.PatchAll();
        config.Finish(harmony);   // writes the .cfg, hot reloads it, installs the YAML hooks
    }

    void Update()  => config.YamlEditor.Update();
    void OnGUI()   => config.YamlEditor.OnGUI();
    // ConfigurationManager button: new ConfigurationManagerAttributes { CustomDrawer = _ => config.YamlEditor.DrawButtons() }
}
```

What you get without further code:

- The .cfg exists after the first start and reloads on edit, with `SettingChanged` firing so Charter pushes changes.
- Server values win on players while the server's locking entry is on, only stewards (admins, the host) can change them then, version mismatch is refused at connect with one screen for every Charter mod and a refusal code.
- YAML files are found in the config folder and next to the DLL, validated with exact error paths, hot reloaded, pushed to players while the locking entry is on, and editable in game.

`Add(entry, synced)` registers an entry bound elsewhere and returns its `Clause<T>`; `Sync` (the `Charter`), `Config`, `Yaml` and `YamlEditor` expose the underlying objects; `IsLocked`, `IsAdmin` and `IsAuthor` map to `IsBound && !MayAmend`, `IsSteward` and `IsAuthor` of the charter.

Merge `SyncedConfig.dll`, `YamlConfig.dll`, `ConfigReload.dll`, `Charter.dll` and `YamlDotNet.dll` into your plugin with ILRepack (see the `ILRepack.targets` of any mod next to this folder for a working example).

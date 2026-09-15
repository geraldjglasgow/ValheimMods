# ConfigReload

Writes your BepInEx `.cfg` at startup and hot reloads it when it changes on disk.

```csharp
public void Awake()
{
    myEntry = Config.Bind(...);
    // ... all your binds ...
    ConfigReload.ConfigReloader.Setup(Config, Logger);
}
```

`SettingChanged` fires for every changed entry, so Charter pushes the new values to players and your own handlers run. Changes are picked up by a five-second poll of the file's write time and size (no file watcher: on Linux servers it reported the mod's own reads as changes); the reload itself runs on the main thread, and only when the file's values differ from what the ConfigFile holds: the mod's own saves (the startup write, a save after binding entries later, Charter's amendments) rewrite the file with the loaded values and are never reported as a change.

Ship it by merging `ConfigReload.dll` into your plugin with ILRepack, or copy `ConfigReload.cs` into your project. No dependencies besides BepInEx.

# PatchGuard

Puts your mod's name on exceptions thrown by your mod's code, then rethrows them. Comes with a profiler that shows
which patched method eats frame time.

Unity logs an exception with the game method that was running. With Harmony that is a dynamic method such as
`Hud.DMD<Hud::UpdateBuild>`, which says nothing about which of a dozen mods patching it actually failed. PatchGuard
walks the stack of an exception and its inner exceptions (the frames, with the text of the trace as a fallback) and,
when your assembly or one of its root namespaces is on it, logs the exception through your logger, so the line reads
`[Error :MyMod] Exception in Update (thrown by this mod's code): ...` with the full trace. The exception is then
rethrown unchanged; exceptions thrown anywhere else are not logged here.

```csharp
void Awake()
{
    Harmony harmony = new(GUID);
    harmony.PatchAll();
    synced.Finish(harmony);
    Guard.Install(harmony, Logger, typeof(MyPlugin).Assembly);   // records logger and assembly, patches nothing
}
```

Wrap the entry points the game calls directly, and the body of a patch when the game method name in Unity's log is
not enough:

```csharp
void Update() => Guard.Run("Update", () => editor.Update());
entry.SettingChanged += Guard.Wrap("apply settings", (_, _) => Apply());
static void Postfix(Player __instance) => Guard.Run("Player patch", () => Adjust(__instance));
```

`Guard.Run` (with a `Func<T>` overload for code that returns a value) and `Guard.Wrap` (for `Action` and
`EventHandler`) log and rethrow the same way; `Guard.Report(exception, context)` only logs, for places that swallow
exceptions on purpose. An exception is logged once even when it passes through several guarded methods. `Install`
returns 0 and adds no patch of its own: a patch on every method a mod touches proved to cost milliseconds per call
with a large mod list.

## Profiler

```csharp
IDisposable report = Profiler.Install(harmony, Logger, typeof(MyPlugin).Assembly, reportEverySeconds: 5, top: 12);
```

Adds a timing prefix and postfix to every game method that carries one of your patches, and to your own patch methods
on them, then logs the most expensive ones at the interval (inclusive time: the game method plus every patch on it,
so it names the hot method, not the exact line). Dispose the result to stop the reports; the timing patches stay.
Meant for diagnosis behind a config switch, not for release play.

Merge `PatchGuard.dll` into your plugin with ILRepack like the other libraries. Depends on BepInEx and Harmony only.

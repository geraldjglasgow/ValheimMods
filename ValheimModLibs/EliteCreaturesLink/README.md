# EliteCreaturesLink

Elite Creatures Reborn's public API (`EliteCreaturesReborn.Api.EliteCreaturesApi`, spec in
`EliteCreaturesReborn/features/api.md`) for other mods, bound by reflection. A mod that merges this library never
references Elite Creatures Reborn: every wrapper finds the API on first use and does nothing (answers `false`, `null` or
an empty array) when Elite Creatures Reborn is absent, older than API version 1, or lacks an endpoint. Our mods merge it
with ILRepack like the other libraries; other authors may bundle it the same way.

## Use

1. Add a `ProjectReference` to `EliteCreaturesLink.csproj` and list `EliteCreaturesLink.dll` in the mod's
   `ILRepack.targets`.
2. Load after Elite Creatures Reborn:
   `[BepInDependency(EliteLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]`. Until it is in BepInEx's
   chainloader the link answers as if it were absent (and looks again on the next call).
3. Register on every peer (server and clients alike): registrations are code, not synced data. Only a creature's owner
   reads them, when the creature is first rolled; a creature already rolled keeps what it has.
4. Register once the creature prefab is complete: the checks that need the prefab (its attack items) are made at the
   call, and the body (large or not) is measured at the creature's roll.

```csharp
if (EliteLink.Present)
{
    foreach (string problem in EliteTraits.SetMutations("ECP_Warlord", new[] { "Juggernaut", "Mad" }))
        Logger.LogWarning(problem);
    EliteTraits.SetAspects("ECP_Warlord", new[] { "Enraged", "Summoner", "Portalbound" });
    EliteTraits.SetPortalAttacks("ECP_Warlord", new[] { "ECP_Warlord_throw" });
    EliteTraits.SetSummons("ECP_Warlord", new[] { "Draugr", "Draugr_Elite" }, new[] { 1, -1 });
}
bool known = Array.IndexOf(EliteTraits.MutationNames, "Gilded") >= 0;
```

Each registration answers the problems Elite Creatures Reborn found, one sentence each naming the endpoint and the
prefab (an unknown name, left out; a mutation its limits refuse now; an attack item the creature does not hold). Only
unknown names and empty names are left out; the rest stay and are checked again when used. An empty list clears that
part, a later call replaces an earlier one, and `Clear` forgets all four.

## Classes

| Class | Endpoints |
|---|---|
| `EliteLink` | `Guid`, `Present`, `ApiVersion`, `PluginVersion`, `HasEndpoint`, `EndpointNames` |
| `EliteTraits` | `MutationNames`, `AspectNames`, `SetMutations`, `SetAspects`, `SetPortalAttacks`, `SetSummons`, `Clear` |

## Internals

`ApiBinding` finds the plugin by GUID (`gglasgow.elitecreaturesreborn`) in the chainloader, the type by name in its
assembly, and checks `GetApiVersion()` against `Required` (1). `Endpoint<T>` turns one endpoint into a typed delegate
(`Delegate.CreateDelegate`, matched by name, parameter types and return type), so a call costs a delegate call; a
missing endpoint is logged once and stays null. `Safe.Call` answers the fallback when the delegate is null or throws.
Only BCL types cross the boundary. Main thread only.

References: BepInEx, assembly_valheim, UnityEngine, UnityEngine.CoreModule; no publicizer. Renaming an API method needs
a matching change here and in Elite Creatures Reborn.

# EliteCraftingLink

EliteCrafting's public API (`EliteCrafting.Api.EliteCraftingApi`, spec in `EliteCrafting/features/api.md`) for other
mods, bound by reflection. A mod that merges this library never references EliteCrafting: every wrapper finds the API on
first use and does nothing (answers `false`, `null` or `0`) when EliteCrafting is absent, older than API version 1, or
lacks an endpoint. Our mods merge it with ILRepack like the other libraries; other authors may bundle it the same way.

## Use

1. Add a `ProjectReference` to `EliteCraftingLink.csproj` and list `EliteCraftingLink.dll` in the mod's
   `ILRepack.targets`.
2. Load after EliteCrafting, so the registrations made in Awake reach it:
   `[BepInDependency(CraftingLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]`. Until EliteCrafting is in
   BepInEx's chainloader the link answers as if it were absent (and looks again on the next call).
3. Register on every peer (server and clients alike): registrations are code, not synced data. A server's YAML files
   still override what code registers.

```csharp
if (CraftingLink.Present)
{
    CraftingClasses.RegisterItemClass("{ \"id\": \"backpack\", \"group\": \"armour\", \"drop_weight\": 0.5 }");
    CraftingClasses.ClaimItems("backpack", "PP_BackpackDeer", "PP_BackpackMoose");
    CraftingClasses.SetItemLevel("PP_BackpackMoose", 8);
    CraftingInscriptions.RegisterExternalEffect("{ \"id\": \"packpanel_slots\", \"scope\": \"item\", \"value\": \"flat\", \"cap\": 4 }");
    CraftingInscriptions.RegisterInscription("{ \"id\": \"packpanel_roomy\", \"effect\": \"packpanel_slots\", \"value\": \"flat\", "
        + "\"affix\": \"suffix\", \"category\": \"utility\", \"classes\": { \"best\": [\"backpack\"] }, "
        + "\"tiers\": { \"count\": 4, \"from\": 1, \"min\": 1, \"max\": 4 } }");
    CraftingInscriptions.AddToPool("backpack", "carry_weight", bestFit: true);
    CraftingHooks.RegisterEquipmentProvider("packpanel", player => WornPack(player));
}
float extra = CraftingInscriptions.GetItemTotal(pack, "packpanel_slots");
```

Definitions are JSON objects with the YAML format 2 field names (`classes-and-tiers.md`): a class takes the economy
YAML's `classes` fields, an inscription the inscription YAML's fields (complete; its effect registered first), a creature
loot profile the `drops.creatures` / `drops.bosses` fields plus `prefab` and `boss`. Errors are logged by EliteCrafting
under the endpoint's name and the call answers `false`.

## Classes

| Class | Endpoints |
|---|---|
| `CraftingLink` | `Guid`, `Present`, `ApiVersion`, `PluginVersion`, `HasEndpoint`, `EndpointNames` |
| `CraftingClasses` | `RegisterItemClass`, `ClaimItems`, `RegisterClassifier` / `UnregisterClassifier`, `SetItemLevel`, `GetItemClass`, `GetItemLevel` |
| `CraftingInscriptions` | `RegisterInscription`, `AddToPool`, `RegisterExternalEffect`, `GetPlayerTotal`, `GetItemTotal`, `GetPlayerInscriptionsJson` (EliteCrafting 0.7.0+: ask `CraftingLink.HasEndpoint` first) |
| `CraftingItems` | `IsMagic`, `GetRarity`, `GetRarityColor`, `GetInscriptionsJson`, `GetDecoratedName`, `CanBeMagic`, `RollMagic`, `Cleanse`, `GetSalvageRune` / `GetSalvageRuneChance`, `DecorateIcon` (EliteCrafting 0.8.0+; OpenKeep's Salvage) |
| `CraftingHooks` | `RegisterEquipmentProvider` / `Unregister...`, `InvalidatePlayer`, `RegisterMagicBaseFilter` / `Unregister...`, `Add/RemoveItemChangedListener`, `Add/RemoveLootGeneratedListener`, `SetCreatureLoot` |

## Internals

`ApiBinding` finds the plugin by GUID (`com.EliteCrafting`) in the chainloader, the type by name in its assembly, and
checks `GetApiVersion()` against `Required` (1). `Endpoint<T>` turns one endpoint into a typed delegate
(`Delegate.CreateDelegate`, matched by name, parameter types and return type), so a call costs a delegate call; a
missing endpoint is logged once and stays null. `Safe.Call` answers the fallback when the delegate is null or throws.
Delegates handed in (classifiers, providers, filters, listeners) use game and BCL types only and pass through unchanged;
EliteCrafting runs them guarded. Main thread only.

References: BepInEx, assembly_valheim, assembly_utils, UnityEngine, UnityEngine.CoreModule (the set PlayerGrid uses);
no publicizer.

# ItemCopies

Writes your mod's numbers into an item, everywhere that item currently exists.

The game does not keep one shared object per item type despite the name `SharedData`: outside the editor, every
world drop gets its own deep copy the moment its `ItemDrop` wakes up, and every item loaded into an inventory (from
a save, or freshly spawned) goes through the same path. Writing into the registered prefab's own `SharedData`
changes what new copies inherit, but does nothing for a copy that already exists. ItemCopies finds every copy that
matters right now and runs your write against each of them once.

```csharp
Copies.Apply("Wood", shared => shared.m_weight = configValue.Value);
```

`Apply` writes into the named prefab and into every live copy of it: the world's spawned `ItemDrop` instances, the
local player's inventory, the player's eaten foods, and whichever container the player has open. `ApplyAll` does the
same for every registered prefab and every live copy of anything, naming the prefab each one belongs to so you can
filter inside your own callback:

```csharp
private static void ApplyNamed(string prefabName, ItemDrop.ItemData.SharedData shared)
{
    if (FoodConfigs.TryGetValue(prefabName, out var configs))
    {
        Apply(shared, configs);
    }
}

ObjectDB.instance.Awake // your own hook
    => Copies.ApplyAll(ApplyNamed);
```

Neither call reaches a copy twice, even when it is reachable through more than one source (an item held by the
local player that is also the one loaded in an open container, say).

## New copies

A value applied now says nothing about an item picked up, crafted or dropped a minute later. `HookSpawns` runs your
callback for those as they appear, for as long as the game runs:

```csharp
Copies.HookSpawns(harmony, item => ApplyNamed(item.m_dropPrefab.name, item.m_shared));
```

This patches `ItemDrop.Awake` (a new world drop) and `Inventory.AddItem(ItemData)` (an item entering any
inventory), once per mod however many times `HookSpawns` is called; every registered callback runs off the same
pair of patches.

Merge `ItemCopies.dll` into your plugin with ILRepack like the other libraries. Depends on BepInEx, Harmony, the
game and Unity; private game members (the world's item list, the currently open container) are reached through
reflection, so your mod does not need a publicized game assembly for this library's sake.

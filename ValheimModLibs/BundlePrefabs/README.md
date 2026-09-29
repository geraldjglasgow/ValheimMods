# BundlePrefabs

Brings a mod's own models into the game: loads the asset bundle the mod embeds, copies game prefabs to build on,
registers new prefabs with the game on every peer, and dresses a model in the game's own materials. The bundles come
from the workspace's `AssetWorkshop` (Blender, then Unity); see its README.

## Use

```csharp
// Plugin.Awake
NetPrefabs.OnSceneAwake(harmony, scene =>
{
    AssetBundle bundle = EmbeddedBundle.Load(typeof(Plugin).Assembly, "my_bundle");
    GameObject body = EmbeddedBundle.Prefab(bundle, "my_model");
    GameObject creature = PrefabBench.Copy(scene.GetPrefab("Skeleton"), "MyMod_Creature");
    // ... swap the visual, set fields ...
    NetPrefabs.Register(scene, creature);
});
```

- `EmbeddedBundle.Load(assembly, name)` loads `<name>.windows` or `<name>.linux` (a dedicated server runs the Linux
  player; macOS tries `<name>.osx`, then the Windows build) from the assembly's embedded resources, once per process.
  A bundle only loads on the platform it was built for, so embed one per platform.
- `PrefabBench.Copy(prefab, name)` makes an inactive copy under a never-destroyed, inactive parent: no Awake runs, so
  no ZDO is created and no Character starts, until the game instantiates it. The name is what the game hashes, so it
  must be unique across mods (prefix it with the mod).
- `NetPrefabs.OnSceneAwake(harmony, build)` runs `build` after every `ZNetScene.Awake`, when the game's prefabs are
  available to copy; `NetPrefabs.Register(scene, prefab)` adds the prefab to `m_prefabs` and the name-hash table. Every
  peer runs the same code, so the hash in a ZDO means the same prefab everywhere.
- `ItemPrefabs.Register(harmony, item)` keeps an item prefab in the live ObjectDB and in every one built or copied
  later (`Awake`, `CopyOtherDB`).
- `GameMaterials.Borrow(prefab, child)` copies a game prefab's material (its textures, shader and settings);
  `GameMaterials.Apply(model, pick)` replaces a model's placeholder materials by name;
  `GameMaterials.Dress(game, placeholder)` copies a game material (shader, lighting) and puts the placeholder's own
  baked albedo and normal map on it, for a workshop-textured part lit like the thing it belongs to;
  `GameMaterials.Plain(material, gloss)` then takes off the game maps it kept that fit only the game model's UVs
  (metal and gloss, glow, style variants).
- `ModelBounds.In(model, space)` measures a model's meshes in another transform's axes (renderer bounds are empty on
  the inactive bench), for centring a bundle model in a copied game prefab.

## Rules

- Nothing of the game's goes into a bundle: models reference the game's materials at runtime (`GameMaterials`).
- Private game members are reached by reflection (`AccessTools`), so consumers need no publicizer for this library.
- Patches are installed once per merged copy of the library; each consuming mod merges its own copy (ILRepack,
  `Internalize=true`), so two mods registering prefabs each patch `ZNetScene.Awake` once.

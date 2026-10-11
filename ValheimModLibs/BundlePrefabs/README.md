# BundlePrefabs

Brings a mod's own models into the game: loads the asset bundle the mod embeds, copies game prefabs to build on,
registers new prefabs with the game on every peer, and dresses a model in the game's own materials. The bundles come
from the asset workshop `../ValheimAssets` (Blender, then Unity); see its README.

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
  A bundle only loads on the platform it was built for, so embed one per platform. It is read straight from the
  resource stream (`AssetBundle.LoadFromStream`, no byte-array copy left as garbage), which stays open with the bundle.
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
  (metal and gloss, glow, style variants); `GameMaterials.Metal(material, placeholder, metalGloss)` puts the shine of
  metal back from the workshop's own metal mask (the placeholder's `_MetallicGlossMap`, red 1 on metal), when it has one;
  `GameMaterials.Unstyled(material)` only switches a shield's painted style designs off, for a workshop model whose UVs
  are laid on that game item's own atlas and wears its material as it is.
- `ModelBounds.In(model, space)` measures a model's meshes in another transform's axes (renderer bounds are empty on
  the inactive bench), for centring a bundle model in a copied game prefab.
- `CreatureBody.Wear(creature, bundleBody, gloss)` puts a workshop body on a bench copy of a game creature: a body made
  on an exact copy of the creature's own skeleton (ValheimAssets, "A new body on a game skeleton"). The main body
  renderer (the one `LevelEffects.m_mainRender` names, else `VisEquipment.m_bodyModel`) takes the bundle mesh, its bind
  poses and bounds, and the creature's own bones by name in the mesh's order; sockets the creature lacks are added under
  the bones of the same names; material 0 becomes the game body's material dressed in the bundle's baked textures
  (`Dress`, then `Plain(gloss)`), and LevelEffects is pointed at the body so star tints still apply. The creature keeps
  its avatar, controller, clips, attacks, AI, death effect and ragdoll. `CreatureBody.HideOthers(creature, body)`
  switches off the game body's other meshes (the Skeleton's eyes); `MainRenderer`, `Rebind`, `AddSockets` and `Bones`
  are the steps, public.

  ```csharp
  GameObject creature = PrefabBench.Copy(scene.GetPrefab("Skeleton"), "MyMod_Mossback");
  SkinnedMeshRenderer body = CreatureBody.Wear(creature, EmbeddedBundle.Prefab(bundle, "workshop_gamerig_demo"), 0.18f);
  CreatureBody.HideOthers(creature, body);
  NetPrefabs.Register(scene, creature);
  ```
- `BundleEffects.Prepare(bundle, name, copyName)` loads an effect built in ../ValheimAssets/Tools/Vfx: an inactive copy under
  the bench with its placeholder materials dressed in the game's own particle shaders (the placeholder's `VfxShader` tag
  names one; `VfxFloats`, `VfxColours`, `VfxKeywords` hold its settings; `VfxBorrow` copies a game prefab's material
  instead) and the game components its `<name>_parts` recipe lists (LightFlicker, LightLod, TimedDestruction,
  CamShaker). Call it inside `NetPrefabs.OnSceneAwake`; pass the copy to LocalEffects (`Flash`, `FlashScaled`, `Attach`)
  on every peer. An undressed placeholder still draws, in Unity's standard particle shader.
- `BundleEffects.Networked(scene, effect)` is for an effect that goes into a game EffectList (a hit or death effect),
  which only one peer runs: it adds a ZNetView and registers the prefab on every peer.
- `EffectTint.Shift(copy, hue, saturation, value)` recolours a copy of any effect: particle start colours, colour over
  lifetime, the gradient-mapped fire's custom colours, trails, lights and material tints (materials are copied first).
  With `PrefabBench.Copy` it derives a new effect from a game one without a bundle. Particle systems are reached by
  reflection (this library does not reference Unity's particle module).
- `SfxPrefabs.Copy(scene, "sfx_greydwarf_idle", "MyMod_beast_idle", bundle, clipNames, settings)` makes a mod's own
  sound the game's way: a copy of a game sound prefab (its ZSFX random clip, pitch and volume, concurrency and
  captions; its AudioSource reach, roll-off curve, mixer group and reverb; TimedDestruction and ZNetView) playing the
  bundle's AudioClips, with its own concurrency hash, registered in ZNetScene. Sounds played through an EffectList are
  networked objects, so every peer must register them (call it inside `NetPrefabs.OnSceneAwake`). A layered game sound
  (`sfx_sword_swing` and its overlay) keeps only its first layer; `CopyLayers` takes one clip set per layer.
  `SfxPrefabs.Variant(scene, gamePrefab, name, settings)` keeps the game's own clips and changes only the settings,
  the way the game makes the Greyling from the Greydwarf (pitch 1.8 to 2.2). `SfxSettings` overrides MinPitch,
  MaxPitch, MinVolume, MaxVolume, MaxConcurrent and Caption; set Caption for a creature's sounds, or the copy captions
  it with the original creature's name. AudioClip lives in UnityEngine.AudioModule, which this library does not
  reference: clips travel as an Array and are set by reflection (`SfxPrefabs.Load(bundle, names)`). The clips come
  from `../ValheimAssets/Tools/Sfx`; which prefab to copy: `../ValheimAssets/Reference/Codex/sfx/catalogue.md`.
- `TexturePixels.Read(texture[, region])` reads a game texture's pixels back through the GPU (game textures are not CPU
  readable): for a mod's own runtime copies of game art, never shipped. Null on a dedicated server.
  `TexturePixels.ReadLinear(texture)` reads a data texture (normal, metal or gloss map) as stored, without the sRGB
  conversion colours get (EliteEquipment's iron mail, 2026-10-08).
- `SpriteCrop.Fit(source, part, name)` cuts a new square item icon out of part of a game icon at runtime (fractions of
  the icon, y from the bottom): the sprite's region read back through the GPU, the opaque pixels of the part found and
  fitted into a square with a margin. No game art goes into a bundle this way (OpenKeep's boots: the bottom of each
  leggings' icon). Null on a dedicated server or any failure; keep the source icon then.
- Not yet run in the game: `SpriteCrop` (2026-10-07), `CreatureBody`, `BundleEffects`, `EffectTint` and `SfxPrefabs` (2026-09-29); their previews
  and checks ran in the workshop's Unity project only.

## Rules

- Nothing of the game's goes into a bundle: models reference the game's materials at runtime (`GameMaterials`).
- Private game members are reached by reflection (`AccessTools`), so consumers need no publicizer for this library.
- Patches are installed once per merged copy of the library; each consuming mod merges its own copy (ILRepack,
  `Internalize=true`), so two mods registering prefabs each patch `ZNetScene.Awake` once.

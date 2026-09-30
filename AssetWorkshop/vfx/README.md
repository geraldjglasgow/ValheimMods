# Effects pipeline

Builds new particle effects that look like the game's own: an effect written in the codex's words
(`codex/vfx/`), textures generated in the game's style, a Unity prefab and bundle for Windows and Linux, a style check
against the game's numbers, and a preview rendered the way the game's camera shows it, beside the game's nearest
effect. The mod dresses the bundle's placeholder materials in the game's own particle shaders at runtime
(`ValheimModLibs/BundlePrefabs`: `BundleEffects`, `EffectTint`). Before building, check `codex/vfx/catalogue.md`:
a game effect cloned and recoloured is often the better answer.

```
vfx/
  build.py              textures, spec, style check, Unity (prefab, bundle, preview), grading, sheet and MP4
  spec.py               the effect spec: short forms in, the complete JSON the Unity build reads out
  spec_materials.py     the game shaders a material dresses into, their settings, blends, keywords and streams
  textures.py           particle texture generators (numpy, Pillow); textures_base.py, textures_soft.py and
                        textures_flipbook.py hold the building blocks, soft shapes and flipbooks it re-exports
  texture_check.py      the generators beside the game's textures, with radial profiles (out/texture_check.png)
  check.py              the style check: each system against its role's numbers, the effect against its category
  grade.py              the game camera for preview frames: bloom, exposure, ACES, contrast, sRGB
  reference.py          stages a game effect in unity/Assets/Reference/Vfx for previews only
  effects/<name>/effect.py   an effect: EFFECT, and textures() when a texture needs code of its own
unity/Assets/Editor/Vfx/
  VfxBuild.cs           the batch entry point
  VfxSpec.cs            the JSON as classes
  VfxPrefab.cs, VfxMain.cs, VfxMotion.cs, VfxLife.cs, VfxRender.cs, VfxCurves.cs   the prefab from the spec
  VfxMaterials.cs, VfxMeshes.cs     textures, placeholder materials with their dressing tags, particle meshes
  VfxBundle.cs          one bundle per platform; fails if anything from Assets/Reference or Assets/Editor goes in
  VfxPreview.cs, VfxStage.cs, VfxLighting.cs, VfxEmulation.cs, Shaders/   the preview
```

## Making an effect

1. Read `codex/vfx/README.md` and pick the category in `codex/vfx/archetypes.md`; take its layers.
2. Write `vfx/effects/<mod prefix>_<name>/effect.py`: `EFFECT` with `name`, `category`, `textures`, `materials`,
   `meshes`, `systems` (each with its `role`), `lights`, `timeout`, `shake` and `preview` (with `references`: the game
   effects to stand beside it). The three examples are the templates: `ecp_spiritfire_burst` (a one-shot fire burst),
   `ecp_frost_impact` (a hit with mesh shards and bouncing bits), `ecp_ember_aura` (a loop attached to a body).
3. Build and look:

   ```
   python vfx/build.py <effect> --preview --wait                   prefab, style report, preview
   python vfx/build.py <effect> <effect> --bundle <bundle> --wait  also the bundle, out/bundles/<bundle>.windows/.linux
   python vfx/build.py <effect> --bundle <bundle> --install <mod>/assets/bundles --wait   release step only (below)
   python vfx/build.py --reference Effects/vfx_HitSparks.prefab --preview --seconds 2   a game effect alone
   ```

   Read `effects/<name>/out/style_report.txt` (every line PASS, or a reason), then `out/sheet.png` (eight frames over
   the preview, ours on the left, the stand-in player, the game's reference on the right) and `out/<name>.mp4`.
   Adjust and build again; a build with a preview takes about 30 s once Unity has the project imported.

`--install` copies the bundle into a mod: use it only when the user has decided to release the effect in that mod, in
the same change as the release. Until then effects stay here (source in `vfx/effects/`, bundles in `out/`) and are
seen in game through DevBridge's stage, never by copying them into a mod (workspace rule, `CLAUDE.md` "Always").

`--wait` waits for `out/unity.lock` (other sessions share the Unity project) and retries when the project is open
elsewhere. Unity runs in batch mode with a GPU (previews need it); the log is `out/vfx_unity.log`.

## The spec

Keys are the codex's (the same ones `codex/measure/vfx_modules.py` measures the game's systems with), so a measured
game system can be pasted into a spec and changed. Values take the measured forms or short ones:

| Form | Examples |
| --- | --- |
| curve | `2.5`, `(1, 3)` a random range, `[[0, 1], [1, 0.3]]` a curve over the life, `{"min": 1, "max": 3}` |
| colour | `[r, g, b, a]`, `([..], [..])` random between two, `{"colour": [[t, r, g, b]], "alpha": [[t, a]]}` a gradient |
| burst | `(time, count)` or `{"time", "count", "cycles", "interval", "probability"}` |

A system: `name`, `role`, `parent` (another system's name, listed before it), `position`, `euler`, `duration`, `loop`,
`prewarm`, `space` (`local` or `world`), `scaling` (`local`, `hierarchy`, `shape`), `max_particles`, `seed`, `lifetime`,
`speed`, `size`, `rotation` (degrees), `flip_rotation`, `colour`, `gravity`, `shape` (`type` sphere, hemisphere, cone,
cone_volume, box, circle, edge, donut, rectangle, mesh; `radius`, `angle`, `arc`, `thickness`, `scale`, `rotation`;
`None` for none), `emission` (`rate`, `rate_distance`, `bursts`), `velocity`, `limit` (`speed`, `drag`), `force`, `noise`
(`strength`, `frequency`, `scroll`, `octaves`), `colour_life`, `size_life`, `rotation_life` (degrees a second),
`sheet` (`tiles`, `time` lifetime or fps, `fps`, `start_frame`), `trail`, `collision` (`type` world, `bounce`,
`dampen`, `lifetime_loss`), `sub_emitters` (`{"type": "death", "emitter": "sparks"}`), `custom` (two streams: the
gradient-mapped shader's colours), `renderer` (`mode` billboard, stretched, horizontal, vertical, mesh, none;
`material`, `mesh`, `max_size`, `length_scale`, `speed_scale`, `sort`, `streams`).

A material names the game shader it dresses into (`shader`): `gradient_mapped`, `particle_unlit`, `lit` (Lux),
`lit_custom`, `standard_unlit`, `standard_lit`, `legacy_additive`, `legacy_alpha`, `decal`, `opaque_lit`; a `blend`
(`additive_soft` = SrcColor One, `additive_alpha`, `additive`, `alpha`, `premultiplied`, `multiply`, `opaque`); `soft`
and `camera_fade` in the shader's own units; `floats`, `colours`, `keywords` to override; `borrow` (`"prefab"` or
`"prefab/child"`) to copy a game material instead of making one (mesh debris in the game's rock or bark). Each shader
brings the game's settings and vertex streams for it (`spec.SHADERS`, from `codex/vfx/shaders.md`).

A light: `name`, `parent`, `position`, `colour`, `intensity`, `range`, `flicker` (the game's LightFlicker: `intensity`,
`speed`, `movement`, `ttl`, `fade`, `fade_in`), `lod` (LightLod `distance`). `timeout` adds a TimedDestruction, `shake` a
CamShaker. Meshes for mesh particles: `{"kind": "chunk" | "shard" | "splinter", "seed", "size", "detail"}`.

Textures come from `textures.py` by name (`make`: point, glow, puff, cloud, star, spark_bolt, streak, ring,
wobbly_ring, speckle, veins, chunk, shard, flame_flipbook, smoke_flipbook; `args` for the generator) with the game's
import settings (`point`, `mips`, `srgb`). `textures.py` keeps the game's rules: white RGB with the shape in alpha,
8 to 128 px sprites, 256 px flipbooks of 8 x 8 frames, point filtering where pixels should show.

## What goes in the bundle

Each effect's prefab (an empty root, one child per system, the lights), our textures and meshes, placeholder materials
and `<name>_parts.txt`. A placeholder is Unity's own `Particles/Standard Unlit` (or `Standard Surface` for lit shaders)
with our texture and the nearest blend, so an undressed effect still draws; its override tags hold the dressing:
`VfxShader` (the game shader's name), `VfxFloats`, `VfxColours`, `VfxKeywords`, `VfxBlend`, `VfxBorrow`. Particle
renderers keep their colours unconverted, as the game's do. Nothing of the game's is in the bundle; the build fails if
anything under `Assets/Reference` or `Assets/Editor` would go in. The three examples make a 162 KB Windows and 279 KB
Linux bundle.

## In the mod

```csharp
// inside NetPrefabs.OnSceneAwake (the game's shaders are loaded by then)
AssetBundle bundle = EmbeddedBundle.Load(typeof(Plugin).Assembly, "ecp_vfx");
GameObject frostHit = BundleEffects.Prepare(bundle, "ecp_frost_impact");   // dressed, with its game components
// then on every peer, from state it already has (a ZDO value, an RPC it received):
LocalEffect.Flash(frostHit, hitPoint, radius: 4f);                         // local, cleans itself up
LocalEffect.Attach(auraPrefab, creature.transform, creature.transform.position, endless: true);
```

- **Local by default.** The prefab has no ZNetView; every peer draws its own copy, so nothing travels and nothing can
  desync (see `codex/vfx/parts.md`, "Network").
- **In a game EffectList** (an item's hit effect, a creature's death effect), which only one peer runs:
  `BundleEffects.Networked(scene, effect)` adds a ZNetView and registers the prefab on every peer; the recipe's
  TimedDestruction removes it on the owner.
- **The derive route:** a game effect copied and recoloured, no bundle at all:
  `var copy = PrefabBench.Copy(scene.GetPrefab("vfx_Burning"), "mymod_spirit_burning"); EffectTint.Shift(copy, 0.33f);`
  (see `codex/vfx/catalogue.md` for what to start from and how to reach the fires pasted into pieces).
- `LocalEffect.FlashScaled` resizes every part, lights and camera shake included; `density` thins particles and dims
  lights for a per-player setting.

## The preview

`VfxPreview` renders without Play mode: our prefab at the origin, each reference 3 m to its right (the camera backs off
to fit), a 1.8 m stand-in player, dark earth, a low warm evening sun and cool ambient. The particle systems step with
`Simulate`, 30 frames a second. The workshop project renders in gamma colour space, so every material is drawn in a
preview copy of its game shader (`Shaders/`, used only here) that does the game's linear maths itself: the
gradient-mapped and mask shaders exactly as the game's compiled code does them, the lit ones approximately (lit by the
preview's sun, ambient and the effects' own point lights, with LightFlicker's flicker and fade played). Frames are linear
HDR; `grade.py` adds the game camera: bloom 0.3 over a 0.7 gamma threshold, +1 EV, contrast 1.2, ACES. Decals and
refraction are not drawn. Game references are staged from the reference export under the gitignored
`unity/Assets/Reference/Vfx` (their game scripts load as missing scripts and do nothing) and never bundled.

## Limits

- The preview's lit particles and its grade are approximations: judge colour and brightness beside the game's
  reference in the same preview, and finally in game (DevBridge).
- Particle collision in the preview hits only the ground plane. Sub-emitters work; the game's own scripts
  (GlobalWind, VortexParticles, ParticleDecal) do not run in the preview.
- Not yet seen in game: the dressing (BundleEffects), the recolour (EffectTint) and the three examples have been built
  and previewed only.

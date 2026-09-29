# The game's particle shaders

What each shader the game's effects draw with does, measured from the game's own files: the shader objects in the
game's SoftRef bundles (`valheim_Data/StreamingAssets/SoftRef/Bundles/c4210710` and `2c2cce25`, read with UnityPy, their
Direct3D 11 programs disassembled with `d3dcompiler_47`: `codex/measure/vfx_shaders.py --disassemble`, output in the
gitignored `codex/out/vfx/shaders/`), the exported dummies' property blocks
(`Shaders/*.shader` in the reference export) and the 4,128 particle systems of the codex corpus
(`codex/data/vfx.json`, key `shaders`). Nothing here is copied from the shaders; it describes what they do.

A mod never ships these shaders. A workshop effect's bundle carries placeholder materials in Unity's standard particle
shader, and the mod dresses them into the game's shader by name at runtime (`BundlePrefabs.BundleEffects`, see
`AssetWorkshop/vfx/README.md`). The names below are the names `Shader.Find` takes.

## Which shader for what

| Use | Shader (runtime name) | Blend | Systems in the corpus | Game example |
| --- | --- | --- | --- | --- |
| flames, fire balls, hot cores, magic fire, ring flashes | `Custom/Gradient Mapped Particle (Unlit)` | SrcColor One | 336 | `fx_Torch_Basic`, `vfx_Burning` flames (1) |
| cinders, sparks, embers, rain streaks, ribbons | `Custom/Particle (Unlit)` | SrcColor One | 136 | `ashrain_cinder` in `vfx_Burning`, `fx_ember_rain` |
| smoke, dust, mist, fog, lit splashes, pixel bits | `Lux Lit Particles/ Bumped` | SrcAlpha OneMinusSrcAlpha, lit | 985 | `vfx_Place_*`, `vfx_SawDust`, fire smoke |
| rain drops, snow, wet drops, tar surface | `Custom/LitParticles` | SrcAlpha OneMinusSrcAlpha, lit per pixel | 90 | `vfx_Wet`, weather `Snow` |
| glows, upgrade glows, pixel bits, shockwave rings | `Particles/Standard Unlit2` | per material | 751 | `DraugrFangGlow` on every weapon, `pixel_additive` |
| blood drops, rock chips, sawdust, hearts (opaque lit bits) | `Particles/Standard Surface2` | per material, mostly opaque | 603 | `blood_splat`, `rocks`, `love` |
| halos round flames, item sparkles | `Legacy Shaders/Particles/Alpha Blended` | SrcAlpha OneMinusSrcAlpha | 305 | `light_glow` flare on torches, `item_particle` |
| old flames and trails | `Legacy Shaders/Particles/Additive` | SrcAlpha One | 136 | `flame` in `vfx_Burning`, `trail` |
| blood and footprint splats on the ground | `Custom/ParticleDecal` | SrcAlpha OneMinusSrcAlpha, projected | 120 | `splat_decal_blend` in `vfx_BloodHit` |
| mesh debris (chunks, splinters, gibs) | `Standard`, `Custom/StaticRock`, `Custom/Creature`, `Custom/Piece` | opaque | 347 | `stone` in `vfx_RockHit` |
| water rings and blobs on a surface | `Custom/ShadowBlob` | SrcAlpha OneMinusSrcAlpha | 44 | `water_ring` |
| heat haze, shockwave refraction | `Particles/Standard Unlit2` with distortion on, `Custom/Distortion` | refraction | 48 | `shockwave_distortion` |

## Custom/Gradient Mapped Particle (Unlit)

The game's fire. Measured on 336 systems; 320 of them additive, the rest alpha-blended (lava splashes).

- **What it computes** (from its compiled fragment program): the texture channel `_GradientChannel` (red in every game
  material) gives a value `g`. The colour is `lerp(Custom2, Custom1, g)`: bright texels take the first custom data
  colour, dark texels the second. It is multiplied by the particle colour. With `_GradientAsAlpha` 1 (every game
  material) `g` is also the alpha, and with `_SrcBlend` 3 (SrcColor) the colour is premultiplied by that alpha, so
  the blend `SrcColor One` adds the colour squared: dim parts vanish, bright parts burn. A soft-particle fade
  (`_SoftFadeFactor`, 1 to 12, `fx_Torch_Basic` 12 = 8 cm) and a camera fade (`_CameraFadeFactor`: alpha times eye
  depth times the factor, 1 = fully visible from 1 m) multiply the alpha. `_FejdFog` 1 blends into the game's fog.
- **The colours live in the particle system, not the material.** Custom data stream 1 and 2 are colours
  (`CustomDataModule`, mode Color), usually gradients over the particle's life, often HDR: `fx_Torch_Basic` Custom1 runs
  from (2.0, 1.22, 0.0) to (0.25, 0.16, 0.0), Custom2 from (2.0, 0, 0) to (1.0, 0.3, 0.0): yellow-white cores over red
  edges, dimming to dark orange. `fx_Torch_Green` does the same in green. Recolouring a gradient-mapped fire means
  changing those two custom colours (EffectTint does), not the start colour.
- **Vertex streams:** Position, Color, UV, Custom1XYZW, Custom2XYZW (331 of 336 systems). Without them the custom
  colours are zero and nothing draws. The workshop's spec sets them for this shader.
- **Texture:** a greyscale flipbook in RGB with no alpha: `flameball_flipbook.png` (256 px, 8 x 8 frames of 32 px,
  point-filtered, no mipmaps), `flameball_big_flipbook.png` (512 px, 8 x 8 of 64 px), `CandleFlame_Flipbook.png`,
  `flames_large_flipbook.png` (16 x 8); also `water_ring.png` for ring flashes and `slowwispysmokeloop.png` for glowing smoke.
- **Settings the game uses:** `_SrcBlend` 3, `_DstBlend` 1, `_GradientChannel` 0, `_GradientAsAlpha` 1, `_Cull` 0,
  `_SoftParticles` 1 with `_SoftFadeFactor` 12 (or 1, 2), `_SoftNearFade` 0, `_CameraFadeFactor` 1 (or 5), `_FejdFog` 0
  (4 systems use 1), keywords `_SOFTPARTICLES_ON`, `_GRADIENTASALPHA_ON`. Queue Transparent, ZWrite off, both faces.

## Custom/Particle (Unlit)

A mask shader. One material in the game uses it: `ashrain_cinder` (136 systems: cinders on burning things, the
Ashlands' ember rain, sparks round the Fader).

- **What it computes:** the texture channel `_AlphaChannel` (alpha) is a mask; all colour is the particle colour. The
  alpha is mask times particle alpha times the camera fade (`_CameraFadeFactor` 0.2: fully visible from 5 m, fading
  in closer) and, when `_SoftParticles` is on, a depth fade. With `_SrcBlend` 3 the colour is premultiplied; the game's
  `SrcColor One` again adds it squared.
- **Streams:** Position, Color, UV. **Texture:** `spark 1.png` (64 px: a soft round glow with a small bright zig-zag).
- Use it for anything small and hot whose colour the system decides: cinders, sparks, rain of embers, magic motes.

## Lux Lit Particles/ Bumped

The most used shader in the corpus: 985 systems (smoke 95 % of the smoke role, dust, mist, lit pixel bits, slime).
A third-party lit particle shader (Lux Lit Particles) in the game's build.

- **Blend** SrcAlpha OneMinusSrcAlpha, ZWrite off, both faces, light mode `Vertex`: lit per vertex by the sun, ambient
  and nearby point lights, so smoke turns orange by a fire and grey-blue at night.
- **Texture channels:** `_MainTex` is "Normal (RG), Depth (B), Alpha (A)": the lit smoke flipbooks
  (`slowwispysmokeloop.png`, `WispySmoke_Flipbook.png`, `RisingSmoke.png`) carry a normal in red and green; the
  greyscale puffs (`dirt.png`, `dust01_bw.png`, `wildfire01.png`) are read the same way, so their grey tilts the normal
  and gives the puff its lumpy shading. The colour is `_Color` times the particle colour (albedo off: `_EnableAlbedo`
  0 in the game's materials).
- **Properties:** `_InvFade` (soft particles, 0.66 to 3), `_CamFadeDistance` (near, far, far range: (4, 150, 25) and
  (2, 150, 25) are the usual), `_WrappedDiffuse` 0.2, `_Translucency` 0.5, `_AlphaInfluence` 1, `_EnableFlipbookBlending`
  (141 systems stream UV2 and AnimBlend for it).
- Use it for everything that should sit in the world's light: smoke, dust, steam, mist, splashes of mud or slime, lit
  pixel bits (`pixel_lit`: untextured, a flat lit square).

## Custom/LitParticles

Per-pixel lit particles (forward base plus an additive pass per light). 90 systems: rain drops (`drop_pixel.png`
stretched), snow flakes (untextured), wraith smoke, the tar and water surfaces, heavy mist. `_Color`, `_EmissionColor`,
`_NormalTex`, `_ZFadeDistance` (soft, 0.3 to 2 m), `_CameraFadeDistanceMin`/`Max` (0/0.1, 0.5/1, 5/20 m), `_Billboard`.

## Particles/Standard Unlit2 and Particles/Standard Surface2

The game's copies of Unity's standard particle shaders (751 and 603 systems): texture times `_Color` times the particle
colour, blend by `_Mode` (0 opaque, 1 cutout, 2 fade, 3 transparent/premultiplied, 4 additive, 6 modulate) with
`_SrcBlend`/`_DstBlend`/`_ZWrite`, optional soft particles (`_SoftParticlesFarFadeDistance` 0.1 to 0.42 m) and camera
fading (near/far 1/2 m, 5/10 m). Surface2 is lit (standard lighting), used opaque for blood drops, rock chips and
sawdust (`blood_splat`, `rocks`, `sawdust`: untextured or the 8 px `leaf_low.png`, coloured by the particle). Unlit2
draws the upgrade glow on every weapon (`DraugrFangGlow`: `glow.png`, alpha-blended, 147 systems), untextured pixel
bits (`pixel_additive`, `pixel_unlit`), spark sprites (`gnista` on the Crystal item's icon, additive), shockwave rings
(`block_wave` on `shockwave.png`) and heat haze (`shockwave_distortion`, distortion on). A workshop placeholder is Unity's
own `Particles/Standard Unlit`, which behaves the same, so an effect meant for Unlit2 looks right even undressed.

## The legacy particle shaders

`Legacy Shaders/Particles/Alpha Blended` (305 systems) and `.../Additive` (136): colour is 2 x `_TintColor` x particle
colour x texture; soft particles by `_InvFade` (0.66 to 3). The game uses Alpha Blended for its big soft halos: the
`light_glow` flare on every torch and fire (`point.png`, 2 to 4 m, particle colour (1.0, 0.49, 0.21) at alpha 0.1:
a faint warm haze, not a bright bloom) and the dropped-item sparkle `item_particle` (`starspark.png`). Additive draws old
flames (`flame`, `flame_small` on 8 px `leaf_low.png`: burning is a stream of stretched additive pixels) and trails.

## Decals, blobs and distortion

- `Custom/ParticleDecal` (120) and `Custom/Decal` (20): alpha-blended splats projected onto whatever is under them (ZTest
  off, fading by `_ZFadeDistance` 0.2 to 0.3 m). Blood (`brains.png`, deep red (0.85, 0.02, 0.02)), footprints in snow
  and ash, ice cracks. Placed by the game's `ParticleDecal` script where another system's particles collide (see
  `parts.md`). Streams Position, Normal, Color, UV, Tangent, Velocity.
- `Custom/ShadowBlob` (44): soft blobs and rings lying on a surface (water rings, the ship's wake blob).
- `Custom/Distortion` (3, the staff shield's shards) and Standard Unlit2 with `_DistortionEnabled` (45 systems of
  `shockwave_distortion` and `heathaze_distortion`): refraction for heat and shockwaves, 0.3 s, 5 to 15 m wide.

## Opaque debris

Mesh particles (385 systems) mostly wear an ordinary lit material of the thing they come from: `Standard` (bark, ice,
snow lumps, blood drops), `Custom/StaticRock` (rock and ore chips: `stone` on `rock_low.png` 32 px), `Custom/Creature`
(a creature's own texture for its gibs), `Custom/Piece` (building materials). Borrow the game material outright
(`VfxBorrow`), so the chips match what broke.

## Colour space and the camera

The game renders in linear colour space with HDR, then bloom (intensity 0.3, threshold 0.7 in gamma, soft knee 0.7),
ACES tonemapping, +1 EV post exposure, contrast 1.2 and a slightly cool white balance (`Misc/posteffect_presets/ingame.asset`).
Its particle renderers leave the particle colours unconverted (`m_ApplyActiveColorSpace` 0), so a start colour of
(1.0, 0.5, 0.2) reaches the shader as those linear numbers: stronger and more saturated than the same numbers in a
colour picker. Textures are sRGB and linearised on sampling (a mid-grey 0.5 in a mask becomes 0.21), which sharpens
soft masks. HDR particle colours (custom data up to 2.0, lights up to 10) are what make fire glow through the bloom.
The workshop's previews reproduce all of this (see `AssetWorkshop/vfx/README.md`).

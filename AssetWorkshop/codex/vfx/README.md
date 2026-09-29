# Effects: particles, lights, trails, decals, shakes

How the game's own effects are built, measured on 1,201 effect prefabs and their 4,128 particle systems (every
`fx_*` and `vfx_*` prefab, everything under `Effects/`, the weather groups of `Systems/_Environment.prefab`, the
projectiles, the fire pieces and the weapons that carry particles). The numbers are in `codex/data/vfx.json`, written
by `codex/measure/vfx_survey.py`; renders of the game's effects are in `codex/out/vfx/effects_*.png` (gitignored,
`codex/measure/vfx_lineup.py`). The effect pipeline that builds new ones from these pages is `AssetWorkshop/vfx/`.

| Page | What it holds |
| --- | --- |
| `archetypes.md` | Per kind of effect (hit, death, fire, explosion, spawn, ...): its layers, numbers and the game's examples; per layer (flame, smoke, spark, ember, debris, ...): lifetime, size, speed, gravity, counts, shader, curves |
| `textures.md` | The game's particle textures: sizes, channels, filtering, flipbooks, what each looks like, which generator imitates it |
| `shaders.md` | What each particle shader does (from the game's compiled shaders), its settings, streams and when to use it |
| `parts.md` | Lights, LightFlicker, LightLod, TimedDestruction, CamShaker, ParticleDecal, GlobalWind, trails, the network model |
| `catalogue.md` | Which game effect to reuse for what, cloned locally and recoloured, with names checked in the export |

## How the game builds an effect

- **A prefab of a few systems, each one layer.** Two to six particle systems (median 3; death poofs 6, ground slams 5),
  each doing one thing: a flame flipbook, a smoke puff, a spray of pixel bits, a few mesh chips, a halo. In 489 of
  1,031 effects the root is a system itself; children carry the other layers, often a copy of another effect pasted
  in whole (`fx_iceshard_hit` has children `vfx_RockHit (1)` and `vfx_ice_hit`).
- **Short.** One-shot effects burst at time 0 (bursts of 10 to 100 particles a layer), run systems of 3 to 5 s and are
  removed by `TimedDestruction` (779 of 1,041 effects) after 3 to 10 s (median 5 s). Most particles live 1 to 3 s.
- **Local space, local scaling.** 63 % of systems simulate in local space, 85 % scale by their own transform only
  (`scalingMode` Local): scaling the effect's root does not resize its children (LocalEffects' `FlashScaled` exists
  for this).
- **Colour from the system, shape from the texture.** Textures are small white or greyscale shapes (8 to 256 px);
  the particle's start colour, colour over lifetime and, for fire, two custom-data colours give every tint.
- **Three looks.** Hot things (fire, sparks, magic) are additive and unlit, often HDR, blended `SrcColor One` so only
  the bright parts show. Air things (smoke, dust, mist, steam) are big, soft, alpha-blended and lit by the scene.
  Solid bits (chips, drops, blood, splinters) are tiny flat squares or low-poly meshes, lit and opaque.
- **Pixels on purpose.** The most used particle texture is an 8 px chunk (`leaf_low.png`, 298 systems); 563 of 756
  pixel-bit systems use no texture at all (flat squares 4 to 15 cm); the fire flipbook is 32 px frames drawn
  point-filtered. Seen up close, the game's fire and debris are visibly pixelated.
- **Lights carry the flash.** Half the explosions and 71 % of fires have a point light (range 4 to 10 m, intensity 2,
  explosions 4); one-shot lights fade out through `LightFlicker`'s time to live (median 0.5 s). The game seldom draws a
  separate flash sprite.
- **Networked, not local.** 867 of 1,041 effects carry a `ZNetView` (831 non-persistent): the game makes an effect on one
  peer (usually the owner of what was hit) and the ZDO spawns it on every nearby peer. A mod's own effect is local
  unless it is registered the same way (`parts.md`, "Network").

## Ten rules for a new effect

1. Pick the category in `archetypes.md` and copy its layer list; stay inside its p25 to p75 ranges.
2. Colour through the particle system. Keep textures white or grey; tint with start colour and colour over lifetime,
   and for fire with the two custom colours of the gradient-mapped shader.
3. Hot = additive and unlit (`gradient_mapped`, `particle_unlit`, `legacy_additive`); air = lit and alpha (`lit`); bits
   = lit squares or meshes. Never draw smoke additive or sparks lit.
4. Fade in fast and out slowly: alpha 0 at birth, full by 10 % of the life, down from about 50 %, 0 at death (the
   median over the 3,193 systems with colour over lifetime: 0, 0.9, 0.99, 0.96, 0.52, 0.21, 0 at 0, 0.1, 0.25, 0.5,
   0.75, 0.9 and 1 of the life; flames 0, 0.67, 0.99, 0.91, 0.46, 0.18, 0).
5. Smoke grows (size over life from 0.3 to 1), sparks and bits shrink to 0, flames swell quickly and shrink at the end.
6. Keep textures small: 32 to 128 px shapes, 8 px bits, 256 px flipbooks of 8 x 8 frames, point-filtered where the pixel
   should show. Soft edges are gaussian, bodies lumpy.
7. Put a light on anything hot: the colour of the effect, range 3 to 10 m, intensity 2, a `LightFlicker` (0.1, 10, 0.1)
   and a time to live for one-shots; `LightLod` 15 to 40 m.
8. Give every one-shot a `TimedDestruction` a little longer than its longest particle (the game: 3 to 10 s).
9. Keep it local: the effect's prefab has no `ZNetView`; every peer draws it from state it already has (LocalEffects),
   unless it must go into a game `EffectList` (then register it, `parts.md`).
10. Preview it beside the game's nearest effect (`vfx/build.py --preview`, the spec's `references`) and match size,
    brightness and timing before shipping.

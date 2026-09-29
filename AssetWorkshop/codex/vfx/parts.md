# Lights, game scripts and the network

What sits round the particle systems in the game's effects, measured on the codex corpus (`codex/data/vfx.json`, keys
`lights` and `scripts`) and read from the game's code (`assembly_valheim.dll`, decompiled into the scratch folder only).
A workshop bundle cannot carry the game's scripts, so the effect spec lists them and BundlePrefabs' `BundleEffects`
adds them at runtime from the effect's `_parts` recipe.

## Lights

510 lights in the corpus: 503 point, 3 directional (lightning flashes), 4 spot. 470 cast no shadow.

| Kind | Range m | Intensity | Colour | Example |
| --- | --- | --- | --- | --- |
| torch | 10 to 15 (carried 10, wall 12, standing 15) | 1.5 | (1.0, 0.62, 0.48) | `Torch`, `piece_walltorch` |
| fire pit, bonfire, hearth | 10, 20, 13 | 2 (bonfire's second light 1.75) | (1.0, 0.5, 0.32); hearth (1.0, 0.62, 0.48) | `fire_pit`, `bonfire`, `hearth` |
| glowing embers of a fire | 2.5 to 3 | 1.5 | (0.84, 0.53, 0.41), flicker 0.2 at speed 12, moving 0.15 m | second light of `fire_pit`, `hearth` |
| burning status | 8 | 2 | (1.0, 0.69, 0.46) | `vfx_Burning` |
| upgrade glow on weapons | 3 | 0.4 | (0.48, 0.80, 1.0) | every weapon's `UpgraderGlow` |
| magic hit, projectile | 3 to 5 | 2 to 3 | the magic's colour: frost (0.455, 0.842, 1.0), lightning (0.23, 0.78, 1.0) | `fx_iceshard_hit`, `fx_chainlightning_hit` |
| explosion | 6 to 20 (median 9) | 3 to 4 | fire orange, boss magenta | `fx_goblinking_meteor_hit` 20 m, 4 |
| ground slam | 10 to 20 | 2 to 4 | blue for Eikthyr (0.23, 0.78, 1.0) | `fx_eikthyr_stomp` 20 m, 3 |
| spawn, death | 5 to 20 (median 10) | 2 to 3 | the creature's colour | `vfx_spawn` 15 m, 3; `vfx_ghost_death` 4 m, 3 |
| fireworks | 125 to 200 | 3.6 to 10 | the rocket's colour | `vfx_Firework_Rocket` 200 m, 10 |
| lightning flash | 1,000 to 5,000, directional | 1.5 | pale blue (0.74, 0.90, 1.0) | `MistlandsFlash`, `Thor` |

Rules: range 3 m for a glow on an item, 4 to 6 m for a hit or status, 10 m for a fire or a spawn, 20 m for a boss blast.
Intensity 2 almost always (median over all lights 2.0; p25 0.4, the item glows). Colour saturated and bright, the
effect's hottest colour. No shadows (92 %).

## LightFlicker (301 lights)

What it does (from its code): every frame the light's intensity is its starting intensity times
`1 + sin(n) · sin(0.564 n) · cos(0.758 n) · m_flickerIntensity`, with `n = offset + time · m_flickerSpeed` and a random
offset per light, and the light moves by up to `m_movement` metres on a similar wave. `m_fadeInDuration` ramps it up
from 0; with `m_ttl` above 0 it fades out over the last `m_fadeDuration` seconds and destroys its GameObject at `m_ttl`.
The game's accessibility setting "reduce flashing lights" swaps the flicker for a steady or smoothed light
(`m_flashingLightsSetting`).

| Field | Game values (p25, median, p75; range) | Use |
| --- | --- | --- |
| `m_flickerIntensity` | 0.1, 0.1, 0.1 (0 to 0.5) | 0.1 for fire; 0.3 to 0.5 for crackling magic |
| `m_flickerSpeed` | 0.1, 10, 10 (0 to 100) | 10 for fire; 0.1 for a slow pulse |
| `m_movement` | 0, 0.1, 0.1 (0 to 2) m | 0.1 for fire; 0 for a steady glow |
| `m_ttl` | 0, 0.5, 2 (0 to 20) s | 0 for loops; 0.3 to 1 s for a hit's flash, 2 s for an explosion |
| `m_fadeDuration` | 0.2, 0.2, 1 (0 to 10) s | how long the one-shot light takes to die |
| `m_fadeInDuration` | 0, 0.1, 0.5 (0 to 4) s | 0 for impacts, 0.5 for a summon building up |

The flash of a hit or explosion is this light, not a sprite: a light of intensity 2 to 4 with `m_ttl` 0.5 and
`m_fadeDuration` 0.2 to 0.5.

## LightLod (265 lights)

Switches a light off beyond `m_lightDistance` from the player (fading its range over a second, checked once a
second) and its shadows beyond `m_shadowDistance`; the game's light limit setting also culls the furthest. Values:
`m_lightDistance` 15 (item glows), 40 (effects, magic, the bonfire), 80 (torches), 100 (fire pit and hearth main
lights); `m_shadowDistance` 20. LightLod reads
the light's range when it wakes, so a mod that rescales a light after instantiating must rescale LightLod's remembered
range too (LocalEffects' `ScaleLights` does).

## TimedDestruction (779 of 1,041 effects)

Destroys the effect after `m_timeout` seconds (p25 3, median 5, p75 8; 1 to 60) when `m_triggerOnAwake` is on (807 of
845). With a `ZNetView` it destroys through `ZNetScene.Destroy` on the owner only, so the removal travels to every
peer; without one it calls `Object.Destroy` locally. Set it a little longer than the longest particle (duration plus
the longest lifetime): a hit 3 to 5 s, a death with decals 10 to 15 s (decals live 10 s).

## CamShaker (161 effects)

Shakes the game camera when the effect starts: `GameCamera.AddShake(position, m_range, m_strength, m_continous)`,
optionally after `m_delay`, continuously for `m_continousDuration`, or for the owner only (`m_localOnly`). Values:
`m_strength` 1 (0.1 to 3), `m_range` 30 to 50 m (5 to 100). Ground slams shake on 63 %, explosions 35 %, water splashes 39 %,
footsteps of big creatures 33 %. The camera shake prefabs (`fx_hit_camshake`, `fx_swing_camshake`, `fx_block_camshake`,
`fx_damage_camshake`) are only a CamShaker and a TimedDestruction of 3 s.

## ParticleDecal (146 effects)

Blood and footprints on whatever is hit: a system with world collision calls `OnParticleCollision`; with
`m_chance` percent (40 to 100) the script emits one particle from `m_decalSystem` (a ParticleDecal-shader system) at the
collision point, turned to the surface normal with a random spin. So a decal needs two systems: the colliding drops
and the decal system (`splat_decal_blend`, 1 to 3 m, 10 s).

## GlobalWind (80 systems)

Every 2 s (or every 0.01 s smoothed) sets the system's velocity over lifetime (or force) in world space to the game's
wind force times `m_multiplier` (0.08 for bonfire flames, 1 to 5 for smoke and dust, up to 20 for weather), and can
scale emission with wind intensity. Fires, smoke and weather lean with the wind because of it.

## ParticleIntensityScaler (145 weapons)

Scales the upgrade glow with the item's quality: emission times up to 0.95 more, alpha up to 1, start and lifetime
colours up to 3 times, light intensity up to 2.5 (`maxQuality` 4). A glow on a new weapon copies the game's
`UpgraderGlow` child rather than rebuilding it.

## Trails

- **Swing trails** are the game's `MeleeWeaponTrail` under `attach/equiped/trail` (93 weapons): 0.3 s long, 4
  subdivisions, material `club_trail` (`red_trail` on three sledges).
- **Particle trails** (227 systems): the trail module on small particles (2 to 25 cm), mostly on systems that draw
  nothing themselves (render mode none), textured `spark 1.png` or the flame flipbook, additive `SrcColor One`; 81 % add
  noise so the ribbons wiggle (strength 0.5 to 2). Lightning is a `Lighting` material ribbon.
- No TrailRenderer or LineRenderer is used for effects except chains and beams.

## Other game scripts round effects

`ZSFX` (430: the effect's sound; see `codex/sfx`), `Gibber` (60: throws a destruction's pre-cut mesh chunks 2 to 5 m/s,
rotating up to 8, gone after 3 s), `SmokeSpawner` (13 fire pieces: spawns physical `Smoke` balls every 0.5 s that rise
and gather under roofs, not particles), `VortexParticles` (summons: pulls particles round an axis), `WrapParticles`
(weather round the camera), `EffectArea` (the warmth of a fire), `Aoe` (damage; never in a cosmetic copy).

## Network

- **The game's way.** `EffectList.Create` (hit effects, death effects, footsteps, on every creature, item and piece)
  instantiates the prefab on the peer that runs it: for a hit, the owner of the creature hit. The effect prefab carries
  a non-persistent `ZNetView` (831 of 1,041 effects), so its instance makes a ZDO that every nearby peer spawns as its
  own copy; `TimedDestruction` on the owner removes it everywhere. Effects without a ZNetView (footsteps: 1 of 58 has
  one) are seen only where they are made, which is why footsteps are created on every peer from the animation.
- **A mod's own effect.** Bundle effects are local: no ZNetView, never in ZNetScene. Every peer draws its own copy from
  state it already has (a ZDO value, an RPC), through LocalEffects (`Flash`, `FlashScaled`, `Attach`), which also strips
  any ZNetView, Aoe and projectile from a cloned game effect. This costs no network traffic and cannot desync.
- **Into a game EffectList.** An effect added to a creature's or item's EffectList (`m_hitEffect`, `m_deathEffects`)
  is created on one peer only, so it must be networked like the game's: `BundleEffects.Networked(scene, effect)` inside
  `NetPrefabs.OnSceneAwake` adds the ZNetView and registers the prefab on every peer, and the recipe's TimedDestruction
  removes it. An unregistered prefab with a ZNetView makes a ZDO no other peer can spawn.

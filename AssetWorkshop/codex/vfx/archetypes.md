# Archetypes: what each kind of effect is made of

Two views of the same 4,128 particle systems (`codex/data/vfx.json`): **layers** (the role one system plays: a flame, a
smoke puff, a spray of pixel bits) and **categories** (the kind of effect: a hit, a death, a fire). A new effect picks
its category, takes that category's layers, and sets each layer from the layer's numbers. Ranges read "p25 to p75
(median)"; n is how many systems or effects were measured. Sizes and speeds are the systems' own numbers in metres and
metres per second; gravity is Unity's gravity modifier (1 = 9.81 m/s² down, negative rises). The renders these
descriptions come from are `codex/out/vfx/effects_*.png`.

## Layers

| role | systems | lifetime s | start size m | speed m/s | gravity | particles a burst | per second | drawn as |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| pixel | 756 | 1-2.5 (2) | 0.045-0.15 (0.062) | 1.5-5.5 (3) | 0-0.3 (0.1) | 20-100 (50) | 20-100 (50) | Particle Standard Surface 40 %, Particle Standard Unlit 29 % |
| smoke | 666 | 1.5-4 (2.25) | 1-3 (1.75) | 0.225-4.5 (1.5) | 0-0 (0) | 10-100 (30) | 4-20 (5) | Lux Lit Particles Bumped 95 %, LitParticles 3 % |
| flame | 389 | 0.65-1.5 (1) | 0.29-1.5 (0.4) | 0-2 (0.3) | -0.08-0 (0) | 10-50 (30) | 20-50 (30) | ParticleGradientMapped_Unlit 60 %, Legacy Particles Additive 20 % |
| debris | 357 | 2.5-5.5 (3.25) | 0.075-1 (0.25) | 1-4.5 (2.25) | 0.1-0.5 (0.3) | 4.5-30 (13.5) | 1-47.5 (12.5) | Standard 34 %, StaticRock 24 % |
| blood | 303 | 1-2 (1) | 0.2-1 (0.5) | 2.5-3.5 (3) | 0.1-0.5 (0.3) | 6-50 (15) | 20-50 (35) | Particle Standard Surface 73 %, Standard 16 % |
| glow | 289 | 2.5-2.5 (2.5) | 1.15-2 (1.15) | 0-0 (0) | 0-0 (0) | 1-1 (1) | 50-50 (50) | Particle Standard Unlit 53 %, Legacy Particles Alpha Blended 44 % |
| slime | 240 | 0.75-2 (1.25) | 0.5-1.5 (0.75) | 0.5-4.5 (2) | 0.05-0.2 (0.2) | 30-50 (30) | 1-30 (1) | Lux Lit Particles Bumped 67 %, Particle Standard Surface 18 % |
| trail | 227 | 0.65-2 (1.5) | 0.02-0.25 (0.05) | 0-2.5 (0.6) | 0-0 (0) | 3-50 (20) | 2.5-34.75 (22) | none 34 %, ParticleUnlit 29 % |
| sparkle | 170 | 2-2 (2) | 0.4-0.4 (0.4) | 0-0 (0) | 0-0 (0) | 20-130 (30) | 1-1 (1) | Legacy Particles Alpha Blended 100 % |
| decal | 140 | 6-10 (10) | 2-3.5 (2) | 0-0 (0) | 0-0 (0) | 1-1 (1) | - | ParticleDecal 86 %, Decal 14 % |
| ember | 123 | 2-3.5 (3) | 0.03-0.066 (0.05) | 0-3 (0.4) | -0.01-0 (0) | 40-225 (100) | 5-36.25 (7.5) | ParticleUnlit 42 %, Particle Standard Unlit 39 % |
| water | 109 | 0.5-2 (1) | 0.125-6 (2) | 0-4 (0) | 0-0.25 (0) | 1-22.5 (3) | 3-95 (10) | LitParticles 38 %, ShadowBlob 23 % |
| spark | 85 | 0.45-2 (0.5) | 0.04-0.2 (0.1) | 0-7.5 (1.5) | 0-0.5 (0.2) | 2-50 (20) | 2-40 (5) | Particle Standard Unlit 74 %, ParticleUnlit 24 % |
| distortion | 45 | 0.3-0.4 (0.3) | 5-15 (10.3) | 0-0 (0) | 0-0 (0) | 1-1 (1) | 150-150 (150) | Particle Standard Unlit 100 % |
| ring | 40 | 0.5-1 (0.8) | 4-20 (10) | 0-0 (0) | 0-0 (0) | 1-1 (1) | - | Particle Standard Unlit 82 %, Legacy Particles Alpha Blended 18 % |
| leaves | 27 | 1.25-3 (2.5) | 0.275-0.75 (0.525) | 2.5-4.25 (2.5) | 0.1-0.3 (0.2) | 20-100 (50) | - | Particle Standard Surface 67 %, AlphaParticle 30 % |
| snow | 23 | 3-10 (10) | 0.04-0.11 (0.06) | 0-1.25 (0.5) | 0-0.03 (0) | - | 10-150 (100) | LitParticles 83 %, Lux Lit Particles Bumped 17 % |

Curves below are sampled at 0, 0.1, 0.25, 0.5, 0.75, 0.9 and 1 of a particle's life, medians over the role.

### flame
Rolling fire blobs. 60 % draw the 8 x 8 flameball flipbook gradient-mapped and additive (`shaders.md`), 20 % are 8 px
pixel chunks in the legacy additive shader stretched along their motion (the burning status: `flame` on
`leaf_low.png`), the rest additive flipbooks in Unlit2. Fire loops at 20 to 50 particles a second (bonfire 200 on a
mesh-shaped emitter); a burst of fire is 10 to 50. Life 0.65 to 1.5 s, barely rising (speed 0.3, gravity -0.08 to 0,
the rise comes from a limit-velocity drag of 0.5 to 4 and noise 0.1 to 0.7 at frequency 1.5). Rotation random 0 to
360 degrees, spinning 45 to 120 degrees a second, flip 0.5. Alpha 0, 0.67, 0.99, 0.91, 0.46, 0.18, 0; size starts
full and ends at a third to a half. Colour: the torch's custom colours, HDR yellow-white (2.0, 1.22, 0) to dark
orange (0.25, 0.16, 0) over red (2, 0, 0) to (1, 0.3, 0). Seen: small bright boiling blobs with visible pixels,
yellow-white in the middle, red at the rim, a big faint orange halo round them (the `light_glow` layer).

### smoke
Big soft lit puffs: 95 % Lux lit, alpha-blended. Life 1.5 to 4 s, start size 1 to 3 m growing (size over life 0.01,
0.47, 0.74, 0.91, 0.99, 0.99, 1), alpha 0 then about 0.95 until half life, then fading (0, 0.95, 0.98, 0.96, 0.55,
0.22, 0). Bursts of 10 to 100 for poofs, 4 to 20 a second for chimneys. Speed 1.5 with a limit-velocity drag (75 %
use it) so puffs shoot out and stop; 37 % add a force (rising or wind). Textures `dirt.png`, `wildfire01.png`,
`dust01_bw.png`, the lit flipbooks `slowwispysmokeloop.png` and `WispySmoke_Flipbook.png` (22 % of smoke systems, 8 x 8).
Colours are pale earth and grey (median start (0.59, 0.63, 0.56)); dust poofs beige (0.6, 0.51, 0.41) to (1, 0.94, 0.8),
fire smoke grey (0.47) darkening to (0.1). Seen: soft cauliflower clouds lit by the sun on top and by fires beside them.

### pixel
The game's signature: flat squares, 4.5 to 15 cm (median 6 cm), 20 to 100 in a burst, 1.5 to 5.5 m/s, gravity 0 to 0.3,
tumbling (rotation over life 70 to 300 degrees a second), life 1 to 2.5 s, shrinking to nothing at the end (size 1,
0.95, 0.94, 0.74, 0.79, 0.39, 0). 74 % untextured: `rocks` and `blood_splat` (lit opaque, Standard Surface2),
`pixel_lit` (Lux), `pixel_additive` and `pixel_unlit` (Unlit2). The rest use `leaf_low.png` (8 px chunk), `brains.png`,
`wildfire01_pixel.png`. Every chip, splinter, crumb, spore and spark-speck in the game is one of these. 19 % stretched
(sparks: `pixel_additive` at 15 to 45 m/s in the dynamite). Seen: little square flecks that catch the light.

### debris
Mesh particles: chunks of rock, bone, ice, wood, gibs. 4.5 to 30 in a burst (median 13.5), 1 to 4.5 m/s, gravity 0.1 to
0.5, life 2.5 to 5.5 s, spinning (up to 300 degrees a second), 61 % colliding with the world. Sizes are mesh scales:
0.075 to 1. Materials of the thing broken: `stone` (StaticRock on `rock_low.png`), `Standard` bark and ice, a
creature's own material for its gibs (`Skeleton` for the skull in `vfx_skeleton_death`). Size stays full and drops to
0 in the last 10 % (0.99, 1, 1, 0.97, 0.81, 0.39, 0). Seen: dark faceted low-poly rocks bouncing and shrinking away.

### blood
Opaque lit blobs and drops in deep red: start colour median (0.69, 0.03, 0.01). Three parts in `vfx_BloodHit`: 6 big
blobs (`blood_splat`, 1 m, 0.4 s), 50 drops (`blood_drop`, 5 cm, 1 to 6 m/s, gravity 0.5, 2 s), 5 chunks (0.2 to 0.3 m),
plus 30 lit splashes (`slime_green` tinted red) and a decal. 30 % collide. The game colours the same materials for
other liquids: greydwarf deaths bleed yellow-brown sap, ghosts teal, eating splashes orange.

### glow
One or a few big soft sprites that sit still: 1.15 to 2 m, speed 0, alpha-blended (97 %), never additive. Two kinds:
the upgrade glow on weapons (`DraugrFangGlow`: 50 a second emitted from the weapon's mesh, 2.5 s life, alpha about 0.02
each, so a haze of 125 faint overlapping sprites; 147 systems) and the flare round fires (`light_glow`: one particle,
infinite life, 2 to 4 m, colour (1.0, 0.49, 0.21) at alpha 0.1). Local space (99.7 %).

### ember
Tiny hot points that drift for seconds: 3 to 6.6 cm, life 2 to 3.5 s, speed 0 to 3, gravity about 0 (the rise comes
from noise: 81 % use it, strength 0.3 to 1, frequency 0.5 to 2), additive (`ashrain_cinder` on `spark 1.png`, or the
Crystal-icon `gnista` sprite). Loops at 5 to 36 a second, bursts of 40 to 225. Colour hot yellow-white (1, 0.88, 0.5);
the bonfire's sparks go from (1, 0.77, 0) to white. Alpha 0, 1, 0.99, 0.94, 0.51, 0.2, 0; size shrinks to nothing.

### spark
Short fast streaks: life 0.45 to 2 (median 0.5), 4 to 20 cm, 1.5 to 7.5 m/s, gravity 0.2 to 0.5, 27 % stretched
(length scale 1 to 10). `vfx_HitSparks`: 30 stretched `gnista` sprites, 3 cm, 5 to 10 m/s, gravity 0.5, 0.3 to 0.5 s,
plus a small yellow dust puff. Colour white to pale pink-gold.

### sparkle
The dropped-item twinkle (`item_particle`, on every weapon and 88 foods): `starspark.png`, 0.3 to 0.5 m, 1 or 2 a second,
life 2 s, still, rotating 90 degrees a second, alpha 1 fading to 0, colour gold (1, 0.89, 0.25) to rose (1, 0.4, 0.4),
legacy alpha-blended, world space.

### slime
Thick splashes: `slime_splash.png` on Lux lit (67 %), 0.5 to 1.5 m, 30 to 50 in a burst, life 0.75 to 2 s, gravity 0.2,
spinning -50 degrees a second, growing from 0.08 to full. Tinted per liquid: mud, tar, poison, lava, blood.

### decal
Splats projected on the ground: one particle a collision (placed by `ParticleDecal`, see `parts.md`), 2 to 3.5 m,
living 6 to 10 s, growing in over the first quarter, alpha fading in the last quarter; `brains.png` in blood red
(0.85, 0.02, 0.02) 93 % of the time.

### water, snow, leaves, rings, distortion
- **water:** rain and drips are `drop_pixel.png` stretched (LitParticles, gravity 0.15, `vfx_Wet` 20 a second, 7 to 10 cm);
  surface rings `water_ring.png` horizontal on ShadowBlob; splashes are big flat foam sheets (`ship_water`, 1.5 to 2 m,
  horizontal, 100 in a burst) with 100 lit pixel drops.
- **snow:** untextured LitParticles flakes 4 to 11 cm, pale blue-white (0.69, 0.91, 1.0), 10 to 150 a second, living 10 s.
- **leaves:** hair, feathers, leaf sprites 0.3 to 0.75 m, 20 to 100, 2.5 m/s, tumbling 200 degrees a second, gravity 0.2.
- **ring:** a `shockwave.png` ring 4 to 20 m wide growing from 0 in 0.5 to 1 s, alpha falling linearly; flat on the
  ground (horizontal) or facing the camera.
- **distortion:** one 5 to 15 m refraction sprite for 0.3 s at the centre of blasts and slams.

## Categories

| category | effects | systems | particles a burst | per second (loops) | longest system s | removed after s | light share, range m, intensity | loops |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| vfx.item_glow | 160 | 2-5 (2) | 1-7 (1.5) | 51-106 (51) | 5-5 (5) | 5-5 (5) | 93 %, 3 m, 0.4 | 100 % |
| vfx.destruction | 125 | 1-3 (2) | 100-250 (125) | 100-100 (100) | 3-3 (3) | 4-8 (5) | 8 %, 4 m, 2 | 2 % |
| vfx.projectile | 118 | 1-3 (2) | 1-500 (26) | 25-100 (50) | 5-5 (5) | 5-7 (7) | 38 %, 4 m, 2 | 98 % |
| vfx.death | 70 | 5-8 (6) | 250-480 (350) | 90-425.05 (268.35) | 3-4.75 (3) | 10-15 (10) | 29 %, 10 m, 2 | 4 % |
| vfx.hit_creature | 62 | 4-6 (5) | 55-111 (91) | 120-120 (120) | 3-3 (3) | 5-10 (8) | 10 %, 7.5 m, 2.5 | 0 % |
| vfx.magic | 61 | 2-4 (3) | 22-140 (50) | 66.25-350 (183.3) | 3-5 (5) | 4-8 (5) | 41 %, 10 m, 2 | 15 % |
| vfx.footstep | 58 | 1-4 (2) | 5-76 (30) | 40-200 (120) | 3-3 (3) | 1-5 (3) | 0 % | 2 % |
| vfx.station | 55 | 2-4 (3) | 29-55 (32) | 82-100 (93) | 3-5 (3) | 4-7 (6) | 18 %, 10 m, 3 | 4 % |
| vfx.hit_material | 45 | 2-4 (3) | 26-130 (69) | 38.75-46.25 (42.5) | 3-3 (3) | 3-7 (5) | 29 %, 4 m, 2 | 0 % |
| vfx.fire | 45 | 2-7 (3) | 1-4 (1) | 20-133 (94) | 5-5 (5) | 5-5 (5) | 71 %, 6 m, 2 | 87 % |
| vfx.place | 42 | 1-1 (1) | 100-200 (100) | - | 3-3 (3) | 3-3 (3) | 0 % | 0 % |
| vfx.spawn | 40 | 2-6 (3) | 80-325.5 (130) | 21.15-199.4 (40.55) | 3-5 (5) | 5-10 (8) | 48 %, 10 m, 2 | 12 % |
| vfx.tree | 39 | 2-3 (3) | 207.5-650 (220) | - | 3-3 (3) | 5-10 (5) | 0 % | 0 % |
| vfx.explosion | 34 | 3.25-6 (4.5) | 123.5-346.5 (206.5) | 46.425-166.25 (85) | 3.25-5 (5) | 4.5-10 (6) | 50 %, 9.157 m, 4 | 9 % |
| vfx.status | 32 | 2-4 (4) | 35.25-51.5 (45.5) | 9.25-58.05 (20) | 4.5-10 (5) | 4-15 (5) | 66 %, 4 m, 2 | 44 % |
| vfx.weather | 30 | 2-4 (3) | 2-6 (3) | 20.75-620 (132.5) | 5-5 (5) | 7.5-10 (10) | 13 %, 1000 m, 1.5 | 87 % |
| vfx.water | 23 | 2-4 (3) | 26.5-106 (50) | 7-411 (20) | 3.5-5 (5) | 3-6 (5) | 0 % | 17 % |
| vfx.love | 20 | 1-1 (1) | 5-30 (20) | - | 3-3 (3) | 5-5 (5) | 0 % | 0 % |
| vfx.ground_slam | 19 | 4-9 (5) | 497-775 (726) | 192.5-200 (200) | 3-5 (3) | 5-10 (5.5) | 47 %, 20 m, 3 | 0 % |
| vfx.eat | 19 | 4-4 (4) | 66-112 (98) | 2.625-2.875 (2.75) | 3-3 (3) | 6-6 (6) | 0 % | 10 % |
| vfx.ambient | 16 | 1-2 (1) | 2.5-12.25 (3) | 1.5-12.5 (10) | 5-5 (5) | 3.5-7.5 (5) | 6 %, 4 m, 1 | 88 % |
| vfx.fire_fuel | 15 | 2.5-3 (3) | 17-34 (29) | - | 3-5 (3) | 5-7 (7) | 0 % | 0 % |
| vfx.levelup | 12 | 2-7 (3.5) | 40-536 (211) | 10-53.325 (20) | 4.5-5 (5) | 4-10 (10) | 58 %, 6 m, 3 | 25 % |
| vfx.hit_plant | 11 | 1-1 (1) | 20-100 (20) | - | 3-3 (3) | 4-5 (5) | 0 % | 0 % |
| vfx.fireworks | 11 | 4-4 (4) | 202-202 (202) | 126.25-245 (160) | 5-6 (6) | 20-27.5 (20) | 91 %, 200 m, 10 | 100 % |
| vfx.shot | 9 | 2-4 (3) | 41-51 (50) | - | 3-3 (3) | 3-3 (3) | 0 % | 0 % |
| vfx.pickup | 9 | 1-2 (2) | 7.75-51.25 (20) | 300-300 (300) | 3-3 (3) | 1-3 (3) | 11 %, 10 m, 2 | 0 % |
| vfx.camshake | 5 | 0 | - | - | - | 3-3 (3) | 0 % | 0 % |
| vfx.aura | 4 | 1-4.75 (2.5) | 333.5-912.5 (623) | 15-20 (15) | 4.5-5 (5) | 8-8 (8) | 50 %, 8 m, 3.25 | 75 % |

"Longest system" is the longest `duration` of the effect's systems (their emission window; particles live on after it).

### Impacts on things (vfx.hit_material; plants vfx.hit_plant; trees vfx.tree)
Two or three layers: a spray of pixel chips in the material's colour and a small dust puff, with mesh chips for stone.
- `vfx_RockHit`: 100 lit squares 5 to 6 cm, grey (0.16) to (0.56), 1 to 3 m/s, gravity 0.2, 2 s; 8 `stone` mesh chips
  1 to 3 cm, 1 to 4 m/s, gravity 0.3, 3 s. No light. Removed at 4 s.
- `vfx_HitSparks` (metal): 30 stretched additive `gnista` sparks, 3 cm, 5 to 10 m/s, gravity 0.5, 0.3 to 0.5 s; one small
  gold-to-white `fog` puff (0.8 to 1 m, 0.5 s).
- `vfx_SawDust` (wood): 200 lit squares 5 to 10 cm in sawdust yellow-brown (0.72, 0.45, 0.21) to (0.78, 0.69, 0.21), 1 to
  6 m/s, gravity 0.2 to 0.5, 3 s; one beige puff of 5 `dust_particle` 0.5 to 1 m.
- Plants (`vfx_bush_leaf_puff`, `vfx_shrub_2_hit`): one layer of 20 to 100 leaf bits or leaf meshes.
- Tree chops and falling logs: 200 to 650 particles: pixel chips, bark and branch meshes, a dust cloud.
Match the chip colour to the material hit; keep it to 2 to 4 s; no light unless the hit is magic (then 3 to 4 m,
intensity 2 to 3 in the magic's colour, `fx_iceshard_hit` (0.455, 0.842, 1.0)).

### Creature hits and blood (vfx.hit_creature)
Four to six layers, 55 to 111 particles, no light: blood blobs, drops, chunks, a lit splash and a ground decal (see the
blood layer). 60 % place a decal through `ParticleDecal`. Creatures of other stuff swap the colour, not the recipe:
`vfx_blob_hit` green slime, skeletons bone dust and chips, ghosts teal glow. Removed after 5 to 10 s (the decal lives 10).

### Death poofs (vfx.death)
The busiest one-shots: 5 to 8 systems, 250 to 480 particles, removed after 10 to 15 s. The body's material turned to
stuff: `vfx_skeleton_death` is a pale bone-dust column (50 lumpy `dust_particle_pixel` puffs 0.4 to 0.6 m), 100 grey
chips, 15 bone meshes and 10 skull meshes wearing the Skeleton's own material; `vfx_greydwarf_death` is yellow-brown sap:
blobs, drops, 30 soft clouds, decals; `vfx_ghost_death` 100 black smoke puffs shot out at 7 to 10 m/s, 300 slow smoke,
30 white sparks, 50 teal additive pixels and a teal light (0.56, 1, 0.89), 4 m, intensity 3. 29 % have a light
(10 m, intensity 2); 17 % shake the camera.

### Footsteps and landings (vfx.footstep)
One to four systems, no light, a sound (86 % carry ZSFX), removed after 1 to 5 s. `fx_footstep_run`: 2 beige dust puffs
(`dust_footstep`, 0.7 m, 1 s, 0.5 to 1 m/s from a hemisphere). Snow and mud add lit pixel bits and a footprint decal;
big creatures add mesh chips and a camera shake (33 %).

### Building placement (vfx.place)
One system: a burst of 100 to 200 `build_fog_lowres` lit puffs (1 to 1.5 m, 1.5 s, 3 to 6 m/s) from a box the size of
the piece's footprint (`vfx_Place_wood_wall` 2 x 2 x 0.2 m), grey (0.44) to white, removed at 3 s. Seen: a billowing
pale dust cloud along the piece that settles in a second and a half. A new piece picks the effect whose box matches
its footprint (`codex/data/pieces.json` place_effects).

### Fires, torches, burning (vfx.fire; flare-ups vfx.fire_fuel)
Loops of 2 to 7 systems: flames (20 to 200 a second), a halo (`light_glow`, one particle), embers (5 to 10 a second
living 5 to 10 s) and smoke (3 a second, 2 to 4 s, grey darkening), with a point light on 71 % (6 m, intensity 2,
flickering 0.1 at speed 10, moving 0.1 m). `fx_BonfireFlames`: 200 flames a second (`fireball_large`, 1.5 to 2.3 m,
0.8 to 1.2 s, gravity -1) from the fire's mesh, 10 sparks, 3 smoke. `vfx_Burning` (status): pixel flames (100 and 50 a
second of stretched additive 8 px chunks), a 0.8 m gradient-mapped flame layer at 100 a second, 5 cinders a second and
a 4 m halo; light (1.0, 0.69, 0.46), 8 m, intensity 2. A flare-up (`vfx_FireAddFuel`) is 20 stretched pixel flames
1 to 4 m/s, 10 flipbook flames 0.6 to 1 m, 4 smoke puffs 1 to 2 m living 4 to 6 s. Fires bend in the wind through
`GlobalWind` (22 %).

### Explosions (vfx.explosion; ground slams vfx.ground_slam)
Three to six systems, 120 to 350 particles, a light on half (9 m, intensity 4), a camera shake on 35 %, removed after 5
to 10 s. `fx_dynamite_explosion`: 50 dark smoke puffs 4 to 5 m stretched at 8 to 17 m/s, 10 flame spikes 3 to 6 m for
0.1 to 0.3 s, 30 flipbook flames 3 to 4 m, 50 additive pixel sparks at 15 to 45 m/s, 50 glowing bits hanging 3 to 5 s,
one 15 m distortion ring for 0.3 s. Seen: a screen-filling pixelated yellow-red fireball for a quarter second, then
brown smoke and drifting gold sparks. Ground slams: 500 to 775 particles: a ring of 500 dust puffs shot out at 12 to 15
m/s low along the ground, 50 rising puffs, 200 grey chips, 25 rock meshes; a light of 20 m on half and a shake on 63 %.

### Spawns, summons, despawns (vfx.spawn)
Two to six systems, 80 to 325 particles, a light on half (10 m, intensity 2), removed after 5 to 10 s. `vfx_spawn`:
200 lumpy pixel puffs 1 to 2 m in magenta to violet (0.99, 0, 0.39) to (0.88, 0.14, 1.0) and 400 cinders, a magenta
light (1.0, 0.45, 0.89), 15 m. Summons swirl (`VortexParticles`) and rise.

### Status effects (vfx.status) and auras (vfx.aura)
Looping on a character (44 %), 2 to 4 systems, 20 a second, a light on two thirds (4 m, intensity 2). `vfx_Poison`: green
smoke (0.78, 1, 0) to (0.48, 0.85, 0.06) at 10 and 20 a second; `vfx_Wet`: 20 drips a second (stretched `water_drop`,
7 to 10 cm, gravity 0.15); `vfx_Frost`: pale blue mist and cold glints; `vfx_Burning`: see fires. Auras and shields:
`fx_shaman_protect` bursts 1,000 blue glowing dots on a sphere shell, a 6 m horizontal shockwave and a 12 m mesh dome,
light (0.3, 0.51, 1.0), 10 m, intensity 4.

### Magic and boss attacks (vfx.magic), projectiles (vfx.projectile)
Three systems, 50 particles in bursts or 180 a second, lit on 41 % (10 m, intensity 2), trails on 21 %. Projectiles
loop (98 %) with a flame or glow core, a trail and a light (4 m, intensity 2 to 3); their hit is a separate effect.
Staff charges gather particles inward (negative speed, radial velocity).

### Level-ups, upgrades, pickups, love
- `vfx_skilllevelup`: a 1.5 m gold `gloria` glow, 200 cinders at 3 to 6 m/s rising (gravity -0.1), 10 ribbon trails,
  sparks; light (1.0, 0.87, 0.32), 4 m, intensity 2; removed at 10 s. Seen: a sparse drifting cloud of tiny pale motes.
- `vfx_HealthUpgrade` swirls red streaks and dots round the player for about two seconds.
- `vfx_pickable_pick`: 4 beige puffs 0.5 to 1 m for 1 s. `fx_creature_tamed`: 30 gold hearts (`heart_low.png`, 8 px)
  0.2 to 0.3 m rising (gravity -0.1) for 2 to 3 s.

### Water (vfx.water), weather (vfx.weather), ambient (vfx.ambient)
- Splashes: big horizontal foam sheets (100, 1.5 to 2 m, from a circle), 8 standing mist sprites 5 m, 100 lit drops;
  removed at 4 s; a camera shake on 39 %.
- Weather follows the player (`_Environment/FollowPlayer`): `Rain` streams 500 stretched streaks a second (1 m sprites
  of several drops, 1.5 s) plus 20 distant 15 m rain curtains and a carrier throwing 100 invisible particles a second
  down at 20 m/s whose collisions with the world spawn the ground splashes (collision sub-emitter; 5 cm, 0.7 s); `LightRain` 80 drops a second; `Snow` 150 flakes a second (4 to
  8 cm, 10 s); `Ashlands_AshRain` 300 ash flakes and 100 cinders a second with heat haze; wind dust 50 a second. Wind
  bends them all (`GlobalWind`); `WrapParticles` keeps the snowstorm's particles round the camera.
- Ambient loops: `FireFlies` 10 a second of blue specks (0, 0.38, 1.0) living 10 s with a light (0.55, 0.72, 1.0), 4 m;
  mists are a few huge lit sprites living 5 to 12 s.

### Eating (vfx.eat), stations (vfx.station), shots (vfx.shot)
- `fx_Eat_*`: the blood-hit recipe in the food's colour (orange, green, ...), smaller: 6 blobs, 30 splashes, 25 drops.
- Stations: 2 to 4 systems of smoke and sparks at the machine's mouth, a light on 18 % (10 m, intensity 3).
- `vfx_bow_fire`: 10 pale blue-grey smoke puffs thrown forward 2 to 8 m/s and 5 stretched pixel spikes at 10 to 35 m/s.

### Glows on items (vfx.item_glow)
Every weapon carries the upgrade glow (`UpgraderGlow`: DraugrFangGlow haze, light (0.48, 0.80, 1.0), 3 m, intensity 0.4,
scaled with the item's quality by `ParticleIntensityScaler`) and its swing trail (`MeleeWeaponTrail`, 0.3 s). Fire
weapons add flames, embers and a light (1.0, 0.62, 0.48), 5 m, intensity 2.

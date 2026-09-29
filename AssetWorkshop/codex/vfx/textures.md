# Particle textures

The textures the game's particle systems draw with: 121 of them over the 4,128 systems (`codex/data/vfx.json`, key
`textures`: size, channels, coverage, edge softness, import settings, blends, examples). Contact sheets in
`codex/out/vfx/textures_*.png` (`codex/measure/vfx_textures.py`: colour over grey, colour added onto black, alpha);
the generator's imitations beside the game's in `AssetWorkshop/vfx/out/texture_check.png` (`vfx/texture_check.py`).

## The style

- **Shape in alpha, colour white.** Nearly every particle texture is a white (or grey) RGB with the shape in its alpha
  channel: `point.png`, `glow.png`, `spark 1.png`, `wildfire01.png`, `dirt.png`, `shockwave.png` are pure white RGB. The
  particle system tints them. Two exceptions: the fire flipbooks are greyscale RGB with an opaque alpha (the
  gradient-mapped shader reads the red channel as both colour position and alpha), and the lit smoke flipbooks pack a
  normal in red and green, depth in blue and coverage in alpha.
- **Small.** 8 px bits (`leaf_low.png`, `heart_low.png`), 32 px (`wildfire01_pixel.png`, `rock_low.png`, `drop_pixel.png`
  32 x 8), 64 px sparks and splats, 128 px soft shapes, 256 px clouds and flipbooks, 512 px only for the big glow and the
  large flipbooks. No particle texture needs more than 256 px; 1024 px ones are borrowed sky and ice textures.
- **Two kinds of edge.** Soft shapes are gaussian: `point.png` is a gaussian of sigma 0.36 of the radius (alpha 0.99,
  0.92, 0.80, 0.62, 0.45, 0.30, 0.18, 0.10, 0.04, 0.01 from centre to rim), `glow.png` a wider one peaking at 0.8 with
  faint swirled streaks, `spark 1.png` a sigma-0.4 glow with a small bright zig-zag in the middle. Pixel shapes are hard
  and point-filtered: the 8 px chunk, the 8 px heart, the pixel versions of the puffs.
- **Lumpy bodies.** Smoke and dust are not smooth discs: `wildfire01.png` is a cluster of soft round lumps of mixed
  size with bright cores and dark gaps; `dirt.png` a mottled round cloud with dark creases and fine grain, alpha about
  0.37 flat across the middle falling to 0 at the rim; `dust01_bw.png` a fainter, flatter cloud with grey (0.35 to 0.43)
  RGB. Seen lit in game they read as cauliflower puffs.
- **Point-filtered where pixels should show.** The fire flipbook (32 px frames, no mipmaps), `leaf_low.png`,
  `heart_low.png`, `brains.png`, `slime_splash.png`, `rock_low.png`, the `*_pixel` textures and the lit smoke flipbooks are
  point-filtered in the game's import settings; the soft glows, clouds and sparks are bilinear with mipmaps.
- **sRGB.** Every particle texture but the normal-packed flipbooks is sRGB and linearised when sampled (a mid-grey 0.5
  becomes 0.21), which hardens soft masks in the game's linear rendering.

## The textures to know

| Texture | Size | Used | What it is | Drawn as | Generator |
| --- | --- | --- | --- | --- | --- |
| `leaf_low.png` | 8 | 290 | an irregular 8 px blob in a few greys, point | opaque lit bits, additive pixel flames | `chunk` |
| `flameball_flipbook.png` | 256, 8 x 8 of 32 px | 247 | boiling fireball frames, greyscale, alpha 1, point, no mipmaps | gradient-mapped fire | `flame_flipbook` |
| `dirt.png` | 256 | 205 | mottled round cloud with dark creases | lit smoke, dust | `cloud` |
| `brains.png` | 64 | 202 | a dense speckle of small squares, point | blood decals, gore bits | `speckle` |
| `slime_splash.png` | 64 | 181 | a disc of fine branching threads | lit slime, mud, blood splashes | `veins` |
| `wildfire01.png` | 128 | 171 | clumpy cluster of round lumps | lit dust and smoke puffs | `puff` |
| `starspark.png` | 64 | 170 | thin four-point glint, vertical ray longer | item sparkle | `star` |
| `spark 1.png` | 64 | 166 | soft glow with a tiny zig-zag | cinders, embers | `spark_bolt` |
| `point.png` | 128 | 154 | gaussian dot | halos, glows, flashes | `point` |
| `glow.png` | 512 | 153 | wide soft glow, faint swirl | upgrade glow haze | `glow` |
| `dust01_bw.png` | 256 | 144 | faint flat cloud, grey RGB | lit smoke | `cloud(grey=0.4)` |
| `slowwispysmokeloop.png` | 512, 8 x 8 of 64 px | 98 | wispy smoke frames, normal RG, depth B, alpha A | lit smoke loops | `smoke_flipbook` |
| `crystal.png` (the Crystal item icon) | 64 | 78 | a pale blue crystal | additive spark sprite (`gnista`) | `shard` |
| `shockwave.png` | 128 | 77 | ring brightest just inside the rim, empty middle | shockwaves, block waves, refraction | `ring` |
| `rock_low.png` | 32 | 65 | rock surface, point | mesh chips (StaticRock) | borrow the game material |
| `wildfire01_pixel.png` | 32 | 50 | `wildfire01` at 32 px, point | lit pixel puffs (spawns, bone dust) | `puff(px=32)`, point |
| `WispySmoke_Flipbook.png` | 256, 8 x 8 of 32 px | 45 | small wispy smoke frames, normal-packed | lit smoke | `smoke_flipbook(cell=32)` |
| `wildfire06.png` | 128 | 45 | a wispy smoke puff | large additive flames, lit smoke | `cloud` with lower contrast |
| `drop_pixel.png` | 32 x 8 | 33 | a two-pixel bar bright at one end | stretched rain and drips | `streak` |
| `Firework.png` | 256 | 32 | a tiny point in a large empty square | firework sparks | `point(sigma=0.03)` |
| `flameball_big_flipbook.png` | 512, 8 x 8 of 64 px | 30 | larger fireball frames | big gradient-mapped fire | `flame_flipbook(cell=64)` |
| `water_foam.png` | 128 | 26 | speckled foam patch | splashes, puke | `speckle` + `cloud` |
| `spark.png` | 256 | 23 | a thick zig-zag bolt | lightning sparks | `spark_bolt` with a bigger bolt |
| `water_ring.png` | 64 | 21 | thin wobbly ring, point | water and tar ripples, ring flashes | `wobbly_ring` |
| `heart_low.png` | 8 | 18 | an 8 px heart, point | taming and love hearts | draw by hand |

Flipbooks are 8 x 8 frames laid out from the top left row by row (Unity's texture sheet order), always a seamless loop:
the fire turns over through its 64 frames and the particle starts at a random frame (`start_frame` 0 to 0.9999) and
plays at 45 frames a second (`fx_Torch_Basic`) or over its life.

## Making new ones

`AssetWorkshop/vfx/textures.py` makes each of these shapes from a seed with numpy and Pillow, tuned against the game's
radial profiles (`vfx/texture_check.py` prints them side by side: `point` matches within 0.02 at every radius, the
flame flipbook's frames within 0.05). Keep the rules: white RGB and shape in alpha; 8 to 128 px for sprites, 256 px
8 x 8 flipbooks; point filter for pixel shapes and flipbooks (the spec's texture `point: True, mips: False`); no
colour in the texture unless it is a borrowed material for mesh debris. Never copy or trace a game texture: generate,
compare the numbers, adjust the generator.

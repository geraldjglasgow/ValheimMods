# Brief: `ecp_queen_egg`

The Deathsquito Queen's egg, whole: what she shoots out of her abdomen in move 2, "Brood"
(`assets/ecp_deathsquito_queen/MOVES.md`). It lands, lies at a slant half sunk in the ground for 20 seconds, throbs,
and hatches a game Deathsquito; at the hatch it is swapped for the burst egg (`ecp_queen_egg_burst`, same frame, same
paint). Workshop preview only (the user, 2026-09-29): nothing goes into a mod until she is released.

## 1. What it is and where it lives

- Name: `ecp_queen_egg`.
- What it is: a leathery ovoid of the Queen's olive chitin, 0.9 m long, five lobes above a wavy waist, her blood-red
  glow showing through the grooves between them.
- Mod: Elite Creatures Pack, with the Queen, if the user releases her.
- Biome: Plains (where she flies).
- Tier: 4 (Plains).
- Who uses it: placed in the world by the Queen's brood attack; a networked object for 20 s, never picked up.

## 2. Category

The nearest category is `item.misc`: it holds the game's own big egg, the Dragon Egg (112 triangles, 128 px, 75 px/m,
1.0 x 1.31 m, an emission map), which is the closest thing the game has to a creature egg that lies in the world.
`env.pickable` (stones, mushrooms) and `env.debris` (statues, frozen bodies) are further off in size and paint.

- Category key: `item.misc`.
- Label and sample count: other items (saddles, upgrade items, seeds, the dragon egg); 38 samples.
- Triangles (min, p25, median, p75, max): 112, 541, 580, 611, 1,868.
- Texture px: 64, 96, 128, 128, 256.
- Texel density (px per metre): 23.0, 36.4, 111.3, 165.1, 174.2.
- Longest side (m): 0.20, 0.42, 0.57, 1.24, 1.85.

## 3. Game references

| Prefab (path under the reference export) | What to take from it |
| --- | --- |
| `GameElements/Items/misc/DragonEgg.prefab` | the game's big egg: 112 triangles, 128 px at 75 px/m, a smooth low-poly ovoid, a glowing pattern (albedo = emission) |
| `GameElements/Items/consumables/AsksvinEgg.prefab` | a 0.9 m egg: 108 triangles, 64 px, soft blotches with darker cracks |
| `GameElements/Items/consumables/VoltureEgg.prefab` | a 0.53 m egg, 124 triangles, 64 px, dark speckles |
| `world/Props/Dvergr/SeekerEgg/model/SeekerEgg.prefab` | the Seekers' brood sac: an insect's egg lying in the world |
| `Characters/Deathsquito/Deathsquito.prefab` | what hatches (1.2 m long, 1.8 m wings) and the paint family: green chitin, orange-red glow |
| `assets/ecp_deathsquito_queen` (workshop) | the mother: her chitin and glow tones, her abdomen's glowing belly between near-black joints |

## 4. Silhouette

- A dark olive egg half sunk at a slant, five glowing red seams running up to its top like a closed bud and a glowing
  wavy waist: at 10 m it reads "the Queen's egg, about to open" from the red lines on the dark shell.

## 5. Parts

- Origin and orientation: metres, Blender Z up. The long axis is **+Z** (the narrower end, where the lobes meet, at
  z = +0.45; the blunter end at z = -0.45). **The origin is the egg's centre**, so the mod sinks it half into the
  ground by putting the origin on the ground (any slant). Petal k's lobe faces the angle -90 + 72 k degrees from +X
  (lobe 0 faces the front, -Y). In Unity (FBX -Z forward, Y up): the long axis is +Y, lobe 0 faces +Z.
- Dropped items: not an item; built to the game's big egg's size class (0.9 x 0.6 m, the Dragon Egg 1.3 x 1.0 m).

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| shell | 0.58 x 0.60 x 0.90 | 73.5 % of the texels | chitin.chitin | primary | one closed mesh, 20 columns x 10 rows and a fan at each end; narrower at the top (egg profile, 12 % taper); lobes 2 cm proud, seven big soft lumps (1.8 to 2.6 cm), a slow noise of 0.9 cm |
| seam grooves | 5 V grooves 2.4 cm deep, walls 13 degrees (6.5 cm) each side, from the waist to the top | with the waist, 26.5 % | glow | glow | where the petals split in the burst egg |
| waist groove | a V groove 2 cm deep, 10 cm tall, wavy: 5 cm high at the seams, 5 cm low under each lobe (z -0.07 to +0.03) | (above) | glow | glow | where the cup splits from the petals |

## 6. Budget

- Triangles (target): a few hundred, as the game's eggs (108 to 124) grown to the category's median (580). Built: 400.
- `TEXTURE_SIZE`: 128 (the Dragon Egg's).
- Texel density (target px per metre): the Dragon Egg's 75. Built: 74.1.
- `NORMAL_MAP` on / `AO_STRENGTH` 0.2 / `NORMAL_FROM_ALBEDO` 4.1 (chitin's k).

## 7. Materials

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| chitin.chitin | the shell | `paint.make("chitin.chitin", tones=Queen, patches=[mottle, bruise], contrast=1.3, top=0, edges=0.3)` (`egg_paint.shell`) | #1f2518 / #344428 / #56683a; mottle #192012 (30 %, 12 cm); bruise #3a2416 (12 %, 10 cm) | the Queen's chitin (`queen_paint.chitin`) without light from above: a prop |
| glow | the grooves | the shell recipe (its bump) with the Base Color replaced by a ramp over the mesh's `glow` attribute (1 at a groove's bottom, 0 at its lips) times a 10 cm noise (`egg_paint.glowing`) | #1c120a / #40120a at the lips, #7a1a0a / #c8321a / #e85a30 (the Queen's belly glow) deeper in | only glow colours are in this region, so an emission from it fades to nothing at the lips |

Dressed in the mod as the Deathsquito's own material (`Custom/Creature`, the Queen's plan) or a Standard material with
emission, as the Dragon Egg is (`builtin_46`, `_EmissionMap`); either way the glow region is its emission (below).

## 8. Paint regions

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| primary (#e6194b) | the shell, 73.5 % of the texels | the shell's colour |
| glow (#f032e6) | the grooves, 26.5 % | the glow's colour; the mod's `_EmissionMap` = albedo x this mask |

- Variants (`variants.json`, matching the Queen's): `pale` (blue-grey shell, blue glow), `ember` (charcoal shell,
  orange glow), `venom` (olive shell, acid green glow). `out/variants.png`.

## 9. Rig and animation

- None: a rigid prop. The throb before hatching (1.0 to 1.06 in scale, 1 then 3 beats a second from 17 s,
  MOVES.md) is the mod's transform scale and emission strength.

## 10. Effects

| When | Reuse (game prefab) or new |
| --- | --- |
| lands | a dust puff (`vfx_*` ground hit, not chosen yet) |
| hatches | swapped for `ecp_queen_egg_burst`; `fx_egg_splash` (the Seeker egg's) or `fx_gjall_egg_splat`, not chosen yet |

## 11. Sounds

| When | Reuse (game prefab) or new |
| --- | --- |
| lands, hatches | the game's egg sounds (`fx_egg_splash` SFX, `fx_gjall_egg_splat` SFX), not chosen; nobody has listened |

## 12. Into the mod (plan only)

- Bundle name and mod folder: the Queen's bundle, Elite Creatures Pack, only when the user releases her.
- Game prefab to copy and build on: none needed; a new networked prefab (ZNetView) whose ZDO holds its landing time on
  the world clock, placed by the owner (MOVES.md "For the mod").
- Game material: `Deathsquito_mat` (`Custom/Creature`) dressed with the baked albedo and normal
  (`GameMaterials.Dress`), the glow mask times the albedo as `_EmissionMap`.
- Textures: `out/ecp_queen_egg_albedo.png`, `_normal.png`, `_regions.png` (glow = #f032e6).
- Colliders: none built; the mod adds a CapsuleCollider (radius 0.3, height 0.9, along Unity Y).
- Icon, recipe, config: none.
- Multiplayer: the owner spawns it and removes it at the hatch; every peer draws the throb and the burst.

## 13. Checks

- [x] `.\build.ps1 -Asset ecp_queen_egg -Lineup` builds (2 s); `out/preview.png` looked at: 0.58 x 0.60 x 0.90 m,
      400 triangles, one 128 px atlas.
- [x] Style check (`out/style_report.txt`): triangles, texture, density, size, the whole albedo and the shell (primary)
      PASS. Outside the game's range, by design: the glow's saturation and value span HIGH and its blotch size LOW - a
      glow, painted as bright as the Queen's belly and the Deathsquito's stripes (#e33e1b) and fading to near-black at
      the lips, judged against flesh, the nearest family (the Queen has the same three lines).
- [x] Lineup (`out/lineup/`, with `--refs +AsksvinEgg,VoltureEgg,SeekerEgg,Deathsquito`): the same pixel size as the
      Dragon Egg, the size class of the game's big eggs, the Deathsquito's palette. What differed and was changed: the
      first glow was one-texel neon lines standing proud like piping (the albedo relief raises bright paint), now broad
      grooves bright in the middle and near-black at the lips; soft glowing "windows" round single vertices turned
      whole lobes dark red (the face is the region), dropped for dull red bruises painted in the shell; the dark
      mottling read as holes, now a shade over the dark tone; the cup and the petals were two different shapes at the
      waist (a lid), now the ribs run on down the cup.
- [x] Paint regions (`out/ecp_queen_egg_regions.png`) cover shell and grooves; the variants sheet looks painted.
- [x] Beside the Queen and the game's Deathsquito (`review.py`, `out/review/`): half sunk at a slant on Plains heath,
      the glowing seams read from above as a five-pointed star.
- [ ] In the game through DevBridge: not yet (no bundle, no mod code).

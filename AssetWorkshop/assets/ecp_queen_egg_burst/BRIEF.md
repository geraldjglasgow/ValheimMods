# Brief: `ecp_queen_egg_burst`

The Deathsquito Queen's egg, split for hatching (`assets/ecp_deathsquito_queen/MOVES.md`, move 2 "Brood": at 20 s
"the shell splits into five petals that burst outward and tumble, then crumble away over 0.6 s in a puff of dust, and
the Deathsquito rises out of the cup; the cup crumbles last"). The mod swaps the whole egg (`ecp_queen_egg`) for this
at the same transform, separates it into its pieces and throws the petals. Workshop preview only.

## 1. What it is and where it lives

- Name: `ecp_queen_egg_burst`.
- What it is: the whole egg cut along its grooves into a lower cup and five petals, each a closed piece of shell 3.5 cm
  thick, 1 cm apart, wet red-brown slime inside.
- Mod: Elite Creatures Pack, with the Queen, if the user releases her.
- Biome: Plains. Tier: 4.
- Who uses it: a cosmetic burst every peer draws for itself (not networked), removed after the crumble.

## 2. Category

`item.misc`, as the whole egg (`ecp_queen_egg/BRIEF.md` section 2): the same object, cut.

- Category key: `item.misc`; other items, 38 samples.
- Triangles (min, p25, median, p75, max): 112, 541, 580, 611, 1,868.
- Texture px: 64, 96, 128, 128, 256.
- Texel density (px per metre): 23.0, 36.4, 111.3, 165.1, 174.2.
- Longest side (m): 0.20, 0.42, 0.57, 1.24, 1.85.

## 3. Game references

| Prefab (path under the reference export) | What to take from it |
| --- | --- |
| `assets/ecp_queen_egg` (workshop) | the shape, the frame and the paint it must match exactly when closed |
| `world/Props/Dvergr/SeekerEgg/model/SeekerEgg.prefab` and its `fx_egg_splash` | the game's insect egg that bursts |
| `Characters/Gjall/gibs/Egg_gib.prefab` | the game's own pre-cut pieces of an egg (gibs), thrown at a death |
| `Characters/Deathsquito/Deathsquito.prefab` | what climbs out: 1.8 m wings over a 0.6 m cup |

## 4. Silhouette

- Closed: the whole egg with fine dark cracks along its glowing grooves. Opened: a five-petalled flower of shell,
  dark olive outside, wet dark red inside, round a cup in the ground.

## 5. Parts

- Origin and orientation: **exactly the whole egg's**: metres, Blender Z up, the long axis +Z (narrow end up), the
  origin at the egg's centre; 0.58 x 0.60 x 0.898 m (the petals' tips stop 2 mm under the egg's top point).
- Pieces: six loose parts that share no vertex and touch nowhere (closest 0.69 cm, near the petals' tips; checked by
  `check_pieces.py`, which separates the built mesh by loose parts as the mod will and measures every pair). Blender's
  `bpy.ops.mesh.separate(type='LOOSE')` gives six objects (their names are Blender's, so find them by geometry):
  - **the cup**: the only piece reaching below z = -0.1 (its lowest point is the egg's bottom, z = -0.45); 400
    triangles; its top edge is the waist groove's bottom less 0.5 cm, wavy from z -0.075 (under each petal's middle)
    to +0.025 (at the seams).
  - **petal k** (k = 0 to 4): its lowest point about z = -0.065, its centre of mass at the angle -90 + 72 k degrees
    from +X towards +Y (petal 0 at -90, the front, -Y; then -18, +54, +126, +198); 120 triangles each. Its foot is the
    waist groove's bottom plus 0.5 cm, its sides the seams k and k + 1 moved 0.5 cm in, its tip 8.5 mm off the egg's
    top point towards its middle. A petal opens (or is thrown) outward about the tangent at the middle of its foot,
    the horizontal axis Z x (its middle direction).
  - In Unity (FBX -Z forward, Y up, X mirrored): the long axis is +Y and petal k's middle is at yaw -72 k degrees
    from +Z (0, -72, -144, +144, +72).
- Shell thickness 3.5 cm (the inner surface is the outer one moved 3.5 cm in along the smooth egg's normal); cuts 1 cm
  wide.

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| outside | the whole egg's surface | 15.0 % glow, the rest of primary's 45.9 % | chitin.chitin, glow | primary, glow | sampled from the same surface function as the whole egg (`ecp_queen_egg/egg_shape.py`) |
| inside | 3.5 cm under it | 39.1 % | skin.flesh | secondary | the hatchling's slime |
| cut edges | 3.5 cm wide strips round each piece | (in primary) | chitin.chitin | primary | a paler olive, so a thrown petal shows its thickness |

## 6. Budget

- Triangles (target): the whole egg's 400 outside, as many inside, the cut edges: about 1,000. Built: 1,000 (cup 400,
  petals 5 x 120), inside the category (max 1,868).
- `TEXTURE_SIZE`: 256. The burst has twice the whole egg's surface (inside and edges), so the whole egg's 128 px would
  halve its texel size; at 256 the outside keeps about the whole egg's texel size.
- Texel density (target px per metre): the whole egg's 74. Built: 94.1 (the paint is drawn at the whole egg's 75 px/m,
  so blotches and per-texel noise match it).
- `NORMAL_MAP` on / `AO_STRENGTH` 0.2 / `NORMAL_FROM_ALBEDO` 4.1 (chitin's k).

## 7. Materials

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| chitin.chitin | outside shell | `egg_paint.shell` (the whole egg's) | #1f2518 / #344428 / #56683a, mottle, bruise | identical recipe and world-space noise, so the closed burst egg matches the whole egg |
| glow | outside grooves | `egg_paint.glowing` (the whole egg's) | #1c120a / #40120a / #7a1a0a / #c8321a / #e85a30 | the grooves' bright bottoms are in the 1 cm cuts, so each edge glows up to the crack |
| skin.flesh | inside | `paint.make("skin.flesh", tones=slime, pattern=("marbling", 0.07), hollows=0.3, region="secondary")` | #240a05 / #44160b / #6e2814 | dark wet red-brown with paler wet streaks |
| chitin.chitin | cut edges | `paint.make("chitin.chitin", tones=section, edges=0, hollows=0)` | #3c3e26 / #5a5c3a / #7c7c50 | the broken shell, paler than the outside |

## 8. Paint regions

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| primary (#e6194b) | outside shell and cut edges, 45.9 % | the shell's colour |
| glow (#f032e6) | outside grooves, 15.0 % | the glow's colour; `_EmissionMap` = albedo x this mask |
| secondary (#3cb44b) | inside slime, 39.1 % | the slime's colour |

- Variants (`variants.json`, the whole egg's plus the slime): `pale`, `ember`, `venom`. `out/variants.png`.

## 9. Rig and animation

- None: rigid pieces the mod moves. Suggested burst (MOVES.md): each petal thrown out along its middle direction and
  up, spinning about its foot's tangent, then scaled down or dissolved over 0.6 s; the cup last.

## 10. Effects

| When | Reuse (game prefab) or new |
| --- | --- |
| the burst | `fx_egg_splash` (the Seeker egg's, with a light) or `fx_gjall_egg_splat`, recoloured red; not chosen |
| the crumble | a dust puff; not chosen |

## 11. Sounds

| When | Reuse (game prefab) or new |
| --- | --- |
| the burst | the game's egg splash sounds (`fx_egg_splash` SFX, `fx_gjall_egg_splat` SFX); nobody has listened |

## 12. Into the mod (plan only)

- Bundle name and mod folder: the Queen's bundle, Elite Creatures Pack, only when the user releases her.
- Game prefab to copy: none; a plain (not networked) prefab of six child meshes. The split can happen in the export
  (Blender, `separate(type='LOOSE')`, the cup found as the piece below z = -0.1) or in Unity at load (split the mesh
  by connected triangles); either way the pieces arrive already in place in the egg's frame.
- Game material: as the whole egg (`Deathsquito_mat`, `Custom/Creature`, glow as emission).
- Colliders: none; the pieces are thrown by the mod (no physics needed for 0.6 s).
- Multiplayer: every peer spawns and plays the burst itself when the egg's ZDO says it hatched.

## 13. Checks

- [x] `.\build.ps1 -Asset ecp_queen_egg_burst -Lineup` builds (2 s); `out/preview.png` looked at: closed, it is the
      whole egg with 1 cm cracks along the grooves.
- [x] `check_pieces.py`: 6 loose parts, each closed (every edge has two faces), the cup and five petals where section
      5 says; no two pieces overlap, the closest are 0.69 cm apart.
- [x] Style check (`out/style_report.txt`): triangles, texture, density, size, the whole albedo and the shell PASS.
      Outside the game's range, by design: the glow's saturation and value span HIGH and blotch size LOW (as the
      whole egg); the slime's value LOW and saturation HIGH against flesh (raw meat is pink; the slime is darker, the
      "darker and wet red-brown" asked for).
- [x] Lineup (`out/lineup/`, with the game's eggs) and the review (`ecp_queen_egg/out/review/burst.png`, `overview.png`,
      `top.png`): opened like a flower beside the Queen with a game Deathsquito rising out of it.
- [ ] In the game through DevBridge: not yet.

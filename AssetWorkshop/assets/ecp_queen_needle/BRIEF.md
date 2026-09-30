# Brief: `ecp_queen_needle`

The needle the Deathsquito Queen spits from her proboscis (`assets/ecp_deathsquito_queen/MOVES.md`): eight in a one
second volley (move 4) and a single fast shot (move 6). A needle that hits a player is removed; one that hits the
ground stays standing in it as a hazard (0.6 m across, 8 pierce every 0.75 s, 20 % slow) until the fight ends. Workshop
preview only.

## 1. What it is and where it lives

- Name: `ecp_queen_needle`.
- What it is: a bee sting grown big: a banded amber horn shaft 0.6 m long, thickest in a bulb at its base, tapering to
  a sharp point; its last 14 cm dark red-brown with two whorls of three small barbs hooked back.
- Mod: Elite Creatures Pack, with the Queen, if the user releases her.
- Biome: Plains. Tier: 4.
- Who uses it: a creature's projectile, then a networked hazard standing in the ground.

## 2. Category

The nearest category is `ammo.bolt` (crossbow bolts, 0.56 to 0.78 m): a short thick projectile flown point first,
which the needle is. Arrows (`ammo.arrow`) are drawn 1.64 to 2.17 m long when dropped and have feathers.

- Category key: `ammo.bolt`.
- Label and sample count: crossbow bolts; 11 samples.
- Triangles (min, p25, median, p75, max): 26, 55, 86, 142, 326.
- Texture px: 64, 64, 64, 96, 256.
- Texel density (px per metre): 23.1, 32.9, 53.0, 55.0, 94.2.
- Longest side (m): 0.56, 0.58, 0.78, 2.95, 4.21.

## 3. Game references

| Prefab (path under the reference export) | What to take from it |
| --- | --- |
| `GameElements/Items/weapons/BoltBone.prefab` | a 0.57 m bolt: 86 triangles, 64 px at 54 px/m, plain paint |
| `GameElements/Items/weapons/BoltCarapace.prefab` | an insect-part bolt, 326 triangles, barbs modelled |
| `GameElements/Items/weapons/BoltIron.prefab`, `BoltBlackmetal.prefab` | the plainest bolts: 26 and 38 triangles |
| `GameElements/Items/materials/Needle.prefab` | the Deathsquito's own needle (the drop): 24 triangles, 1.05 m, flat teal with a normal map |
| `assets/ecp_deathsquito_queen` (workshop) | her amber horn crown and stinger (`queen_paint.horn`), her proboscis the needle comes from |

## 4. Silhouette

- A pale amber spike with a dark barbed point: standing in the ground it reads as a thorn stuck in the heath, the
  pale shaft against the olive grass, the dark point at the ground line.

## 5. Parts

- Origin and orientation: metres, Blender Z up. **The origin is the needle's tip.** The needle points forward, **-Y**,
  its body running from the origin along **+Y** to the base at y = +0.60. In Unity (FBX -Z forward, Y up): the tip at
  the origin, the needle pointing **+Z**, the base at z = -0.60. So a projectile turned to its velocity
  (`Quaternion.LookRotation(velocity)`, as the game's `Projectile` does) flies it tip first; placed at a hit point with
  the same rotation it stands in the ground tip first, its base trailing back up the flight. Sink it 0.04 to 0.08 m
  along its forward axis (move it forward by that much) so the barbs sit at the ground line and the dark point shows.
- Held items: not held. Cross-section: hexagonal, a flat face up (+Z) and down.

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| point and sting | y 0 to 0.14, radius 0 to 0.95 cm | 29 % of the texels (with the barbs) | bone.horn, dark | secondary | a separate short piece of the shaft so it gets its own texels |
| barbs | six, 2.6 cm long, 1.1 cm out; roots at y 0.045 (at 90, 210, 330 degrees round the shaft) and y 0.095 (30, 150, 270) | (above) | bone.horn, dark | secondary | three-sided hooks pointing back (+Y) and out |
| shaft | y 0.14 to 0.46, radius 0.95 to 1.75 cm | 71 % (with the base) | bone.horn, amber | trim | two pieces, meeting exactly at y = 0.37 |
| base bulb | y 0.46 to 0.60, radius 2.15 cm at y 0.515 (4.3 cm thick), rounded to a flat end | (above) | bone.horn, amber | trim | the thicker base a sting grows from |

## 6. Budget

- Triangles (target): the bolts' 55 to 142 plus the barbs; under 200. Built: 142.
- `TEXTURE_SIZE`: 64 (the bolts').
- Texel density (target px per metre): the bolts' 53 to 55. Built: 225 by the style check, and the paint is drawn at
  55 px/m (`DENSITY`), so its blotches and per-texel noise are the bolts' size. Why the UV density is high: the game's
  bolts share one 64 px sheet between several bolts, and the pipeline's unwrap fills the whole atlas with this one
  needle. With one long 0.6 m strip it reached 87 px/m (inside the range), but then the barbs and the point were under
  a texel each and took their neighbours' amber: pale barbs and a pale point. Cut into three short pieces the islands
  pack 2.6 times bigger and the point and barbs keep their own dark red.
- `NORMAL_MAP` on / `AO_STRENGTH` 0.2 / `NORMAL_FROM_ALBEDO` 2.5 (horn's k).

## 7. Materials

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| bone.horn | shaft and base | `paint.make("bone.horn", tones=amber, axis="Y", pattern=("bands", 0.02, 0.15), blotch_m=0.1, region="trim")` | #5a4020 / #9a7a48 / #d4b878 | the Queen's crown and stinger (`queen_paint.horn`), faint growth rings every 13 cm |
| bone.horn | point, sting and barbs | `paint.make("bone.horn", tones=blood, axis="Y", pattern=None, edges=0, region="secondary")` | #2a100a / #4a1c10 / #6e3018 | old blood; no worn-edge lightening, so the point stays dark |

Dressed in the mod like the game's bolts (Standard `builtin_46`, or `GameMaterials.Dress` on a bolt's material) with
the baked albedo and normal. Point filtered.

## 8. Paint regions

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| trim (#4363d8) | the amber shaft and base, 70.6 % | the needle's colour, with the Queen's crown (her `trim`) |
| secondary (#3cb44b) | the point, sting and barbs, 29.4 % | the point's colour (a mod could make it glow to mark the hazard) |

- Variants: none yet; the Queen's `ember` variant would tint `trim` #c8762a here as on her crown.

## 9. Rig and animation

- None: a rigid projectile and prop.

## 10. Effects

| When | Reuse (game prefab) or new |
| --- | --- |
| flight | a thin trail (the arrows' `trail`), not chosen |
| hit, stuck | `vfx_arrowhit` / the game's arrow-in-ground dust, not chosen |

## 11. Sounds

| When | Reuse (game prefab) or new |
| --- | --- |
| spit, hit | the game's arrow and bolt sounds (`sfx_arrow_hit`), not chosen; nobody has listened |

## 12. Into the mod (plan only)

- Bundle name and mod folder: the Queen's bundle, Elite Creatures Pack, only when the user releases her.
- Game prefab to copy and build on: a bolt's projectile (the arbalest's), its visual swapped for this mesh; the stuck
  needle a new networked prefab (ZNetView) with a 0.6 m trigger for the hazard, spawned by the owner where a needle
  hits the ground, removed by the owner when the fight ends.
- Game material: a bolt's (Standard), dressed with the baked textures.
- Colliders: none built; the projectile's own sphere, the hazard's trigger.
- Multiplayer: the owner decides every needle; stuck needles are networked so a late peer sees them.

## 13. Checks

- [x] `.\build.ps1 -Asset ecp_queen_needle -Lineup` builds (2 s); `out/preview.png` looked at: 0.043 x 0.037 x 0.60 m,
      142 triangles, one 64 px atlas.
- [x] Style check (`out/style_report.txt`): triangles, texture, size, the whole albedo and the amber's value,
      saturation, span and contrast PASS. Outside the game's range: texel density HIGH (section 6); the amber's
      blotch size LOW (bands and blotches on a shaft 2 to 4 cm thick, against the two pale horns the codex has); the
      dark point's value LOW, saturation HIGH and span and blotch LOW - a small dark red-brown point judged against
      pale horn, the only family near it.
- [x] Lineup (`out/lineup/`, with `--refs +Needle,BoltBloodGold`): the size and budget of the game's bolts, a little over
      half the Deathsquito needle item's 1.05 m. What differed and was changed: the first head was a big square arrowhead
      (it read as an arrow), now a sting tapering to a point with small barbs; strong growth bands read as a
      segmented larva, now faint; the point and barbs sampled the amber (see section 6).
- [x] In the review (`ecp_queen_egg/out/review/needles.png`, `overview.png`, `eye_5m_bare.png`, `eye_10m_bare.png`):
      eight standing in Plains heath read clearly from 5 and 10 m on bare ground. Among the game's heath grass at the
      clutter's own numbers (`eye_5m_grass.png`, `eye_10m_grass.png`: 3 clumps a square metre, 0.7 to 1.45 m tall) they
      are hidden, like anything under half a metre in that grass; if the hazard must be seen there, the mod needs a
      marker (a glow on the `secondary` point, a decal or a small effect), since the needle is 0.6 m by design.
- [ ] In the game through DevBridge: not yet.

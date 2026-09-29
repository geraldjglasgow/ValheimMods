# Shields

The game's 21 shields measured on 2026-09-29 by `measure/items.py`: `data/items.json` categories `shield.round`
(11), `shield.tower` (6) and `shield.buckler` (4); renders in `codex/out/items/sheets/shield.*.png`, texture sheets in
`out/items/paint/shield.*.png`. A shield is `m_itemType` Shield, skill Blocking, animation state Shield; the name
decides the kind (Tower, Buckler, the rest round or square).

## 1. The attach frame

A shield is held in the left hand: `LeftHand_Attach`, same axes as the right hand's except Y (see `weapons.md`,
section 1). The left hand's back faces -Y, so:

| | Where | Measured |
| --- | --- | --- |
| origin | the fist, at the grip behind the middle of the board | fist at 0.44 to 0.49 of the height on round shields and bucklers |
| -Y | the front face, the boss and paint (away from the body) | renders from -Y show the boss (ShieldBanded) and the boss hole (ShieldIronTower); from +Y the grips |
| +Y | the back: grips, straps | grips 3 to 8 cm behind the board |
| ±Z | up and down the board (+Z up in the player's hand) | |
| ±X | across the board | |

Round shields sit centred on the fist: z +0.46 / -0.49 m, x ±0.42 to 0.44 (medians). Tower shields hang below the
hand: z +0.43 to +0.82 (0.68) above and -0.77 to -1.02 (-0.84) below. The board is dished: 0.29 to 0.34 m deep in Y
including the grip on round shields (ShieldWood 0.29, ShieldBanded 0.34). The shield's long axis tilts a few degrees
off Z (median 9 degrees round, 11 tower): the board is set slightly slanted on the forearm.

## 2. Budgets

| Kind | n | Triangles | Texture | px per metre | Size | Game example |
| --- | --- | --- | --- | --- | --- | --- |
| round, square (`shield.round`) | 11 | 108 to 2,288 (871) | 128 to 256 (128) | 50 to 102 (62) | 0.74 to 1.44 m tall (1.00) | ShieldWood 178 tris, 128 px, 0.81 m; ShieldBanded 274, 0.97 m |
| tower (`shield.tower`) | 6 | 268 to 4,604 (1,886) | 128 to 256 (128) | 44 to 101 (67) | 1.44 to 1.72 m tall (1.49) | ShieldWoodTower 268, 256 px, 1.53 m; ShieldIronTower 752 |
| buckler (`shield.buckler`) | 4 | 326 to 1,476 (613) | 32 to 64 (64) | 30 to 50 (44) | 0.56 to 0.74 m (0.64) | ShieldBronzeBuckler 326, ShieldIronBuckler |

- Shader: the game's `Creature` on all but one. Maps: albedo, normal, metal and `_StyleTex` on 7 of 11 round and 5 of
  6 tower shields (section 4).
- Material values: `_Metallic` 1, `_MetalGloss` 0.7, `_Glossiness` 0.21 (0.1 to 0.5), `_BumpScale` 1 (0.37 to 1.67).
- ShieldWood and ShieldBanded share one 128 px sheet (`shield_wood/shieldwood_d.png`); ShieldIronTower and the later
  iron and black-metal shields share `NewShields/model/IronShields/Shield_Designs_d.png` (256 px).
- Silhouettes by tier (from the sheets): a plain plank disc (ShieldWood), the same with an iron rim and boss
  (ShieldBanded), a flat iron-framed door of planks (ShieldIronTower), a kite of scale (ShieldSerpentscale), a
  silver-rimmed wood disc with a notch (ShieldSilver), a black-metal frame over pale sheet with chain hooks
  (ShieldBlackmetal, ShieldBlackmetalTower), a carapace kite (ShieldCarapace), an oval of planks in a blue-red flametal
  rim (ShieldFlametal, ShieldFlametalTower), a gold-rimmed disc with a round boss (ShieldGold). Bone gear goes organic:
  a leather board fringed with bones (ShieldBoneTower 2,872 tris), a coil of roots (ShieldRoots 1,406).

## 3. How they are painted

From `out/items/paint/closeup_wood_leather.png` and `paint/shield.*.png`, numbers over the used pixels:

- **Planks**: warm brown fields, median sRGB (132, 89, 51) to (148, 93, 57), each plank a flat colour with 1 px dark
  lines for the gaps and a few thin darker grain strokes; a dark diamond or disc for the boss hole or nail.
- **Rims and bosses**: grey metal (99 to 157 grey), soft, a bright soft highlight on the curve; black-metal boards
  are grey with pale scratch strokes (ShieldBlackmetal).
- **Few colours**: 25 to 60 colours at 16 levels a channel; neighbouring pixels step 3 to 8 levels.
- **Normal maps work harder than on weapons**: mean tilt 6 to 14 degrees, 19 to 48 % of pixels tilted over 10
  degrees: plank grooves, the rim bevel, rivets.
- **Metal map**: the rim and boss metal, the planks not (metal 15 to 20 % of used pixels on the wood shields, 93 to
  100 % on the black-metal ones).

## 4. Styles: the painted face

Round and tower shields carry `m_variants` 4, 5 or 7 and a material with `_UseStyles` 1, `_Style` and a `_StyleTex`: a
decal atlas of heraldic designs laid over the face (ShieldWood: `shieldwood_paint.png` 512 px, 4 designs; iron and
black-metal shields: `NewShields/model/Shield_Designs_Decals.png` 1,024 px, up to 7 designs). The designs: quartered,
pinwheel, saltire, bends and stripes in strong red, blue, white, yellow, green and black, with ragged, scratched paint
edges over the plank texture. The player picks the variant; how the shader chooses the tile is in `recolour.md`.
A new round or tower shield should keep a plain face in its albedo and put its designs in a style atlas.

## 5. Effects and sounds

| Slot | Round and tower | Bucklers |
| --- | --- | --- |
| blockEffect | sfx_wood_blocked, vfx_blocked, fx_block_camshake | sfx_metal_shield_blocked (3 of 4), vfx_blocked, fx_block_camshake |
| hitEffect (shield bash) | vfx_clubhit, sfx_club_hit | vfx_clubhit, sfx_club_hit (1 of 4) |
| startEffect | sfx_club_swing | sfx_club_swing |

No swing trail and no upgrade glow on any shield.

## 6. The dropped shield

Root `Rigidbody` mass 1, continuous collision; colliders on the board mesh: `BoxCollider` on 6 round shields, convex
`MeshCollider` on 5 round and 4 tower shields. Disabled in the hand. On the back the shield hangs on
`BackShield_attach` (Spine1) in the same attach frame.

## 7. Checklist

- Grip at the origin; face towards -Y, grips towards +Y; round shields centred on the fist, towers hanging 0.8 m
  below it and 0.7 m above.
- 0.8 to 1.0 m round, 1.45 to 1.7 m tower, 0.55 to 0.75 m buckler; the board dished, 3 to 8 cm thick with grips.
- 170 to 900 triangles for a round shield, up to 1,900 for a tower; 128 px; 50 to 100 px per metre.
- Flat planks with painted gaps, a metal rim with a soft highlight, a stronger normal map than a weapon's.
- A style atlas for the face designs; `sfx_wood_blocked`, `vfx_blocked`, `fx_block_camshake`.

## References

`GameElements/Items/shields/ShieldWood.prefab`, `ShieldBanded.prefab`, `ShieldSilver.prefab`, `ShieldBlackmetal.prefab`,
`ShieldCarapace.prefab`, `ShieldFlametal.prefab`; towers `ShieldWoodTower.prefab`, `ShieldBoneTower.prefab`,
`ShieldIronTower.prefab`, `ShieldBlackmetalTower.prefab`; bucklers `ShieldBronzeBuckler.prefab`,
`ShieldIronBuckler.prefab`, `ShieldCarapaceBuckler.prefab`.

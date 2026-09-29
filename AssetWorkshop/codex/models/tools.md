# Tools

Pickaxes, the hammer, farming tools, the fishing rod, torches and lanterns, tankards and the odd tools, measured on
2026-09-29 by `measure/items.py`: `data/items.json` categories `tool.*`; renders `codex/out/items/sheets/tool.*.png`,
textures `out/items/paint/tool.*.png`. Tools are held like weapons: the `attach` child in the hand's attach frame, fist
at the origin, +Z out of the thumb side, +X the knuckles' side (the frame is measured in `weapons.md`, section 1).

The game sorts them by shared data, not by folder: pickaxes are `TwoHandedWeapon` with skill Pickaxes and animation
state TwoHandedClub; the Hammer, Hoe and Cultivator are `Tool` (the Hoe and Cultivator animate as Atgeir); the Shovel
and Scythe are two-handed Farming items (animation Atgeir and Scythe); the fishing rod is `TwoHandedWeapon` with state
FishingRod; torches and lanterns are `Torch`; tankards are one-handed "weapons" that animate as Torch.

## Budgets

| Tool (key) | n | Triangles | Texture | px per metre | Size | Frame | Game example |
| --- | --- | --- | --- | --- | --- | --- | --- |
| pickaxe (`tool.pickaxe`) | 5 | 282 to 422 (296) | 32 to 256 (256: the shared weapon atlas) | 42 to 76 (61) | 0.90 to 0.99 m long | fist at 0.14 to 0.16 of the haft; head across X at z +0.70 to +0.83; the pick points +X (0.34 to 0.45 m), a back pick to -X on bronze, iron and black metal (to -0.45 m) | PickaxeIron 296 tris, 0.90 m across the head |
| hammer (`tool.hammer`) | 1 | 362 | 256 (shared atlas) | 136 | 0.47 m | fist at 0.22; head at z +0.37, 0.27 m across X | Hammer |
| hoe, cultivator, shovel, scythe (`tool.farming`) | 4 | 208 to 640 | 32 to 128 | 35 to 134 | 1.17 to 1.81 m | a pole along +Z tilted 18 to 24 degrees towards +X, the working head at the top on +X (x +0.46 to +0.63); the Scythe's snath runs along Y (-1.0 to +0.8 m) with the blade up +Z | Hoe 208, Cultivator 332, Shovel 640 |
| fishing rod (`tool.fishing_rod`) | 1 | 2,524 | 64 | 18 | 3.69 m | fist at 0.12; the rod runs +Z to 3.26 m, the butt 0.43 m behind | FishingRod |
| torch, lantern (`tool.torch`) | 5 | 48 to 2,860 (164) | 32 to 256 (64) | 32 to 118 | torch 0.86 m, lantern 0.64 to 0.66 m tall | torch: fist at 0.16, burning head at z +0.55 to +0.72, 10 cm across | Torch 160 tris, 32 px |
| tankard (`tool.tankard`) | 4 | 128 to 810 (469) | 32 to 256 (64) | 11 to 85 | 0.26 to 0.53 m | the handle in the fist, the cup towards +X (x +0.24 to +0.31) | Tankard 236, TankardOdin 810 |
| grappling hook, feaster (`tool.misc`) | 2 | 760 to 1,420 | 64 | 44 to 71 | 0.86 to 1.70 m | | GrapplingHook 760 |

- **Pickaxes** are the axes' twins: a round haft 6 to 7 cm thick with a wedge-ended head across it at 82 to 88 % of the
  length, 34 to 42 cm wide in the median outline (PickaxeIron 82 cm). Tiers: antler and stone lashed to a stick
  (PickaxeAntler 282, PickaxeStone 282), a bronze then an iron double pick on a plain haft (296 each, the shared
  256 px atlas), a green-black black-metal pick with a red leather wrap (PickaxeBlackMetal 422, 32 px).
- **The Hammer** is short and dense: 0.47 m, a wooden mallet, its barrel head 0.27 m across X and 0.13 m deep on a
  handle that swells towards the butt, 136 px per metre (the atlas again). It has no effects of its own: it opens the
  building menu (`m_buildPieces`).
- **The Hoe** is a rock mesh (`3rd party/A_piece_of_nature`, on the `StaticRock` shader) on Unity's built-in cube
  stretched to 4 by 4 cm by 1.23 m for the handle; the Cultivator is a pole ending in a claw of fingers; the Shovel
  (Ashlands) a dark pole with a blue-grey blade trimmed in orange on the `Piece` shader.
- **Torches**: a stick 2 to 4 cm thick widening into a 10 cm wrapped head. Lanterns hang: their `equiped` child holds a
  `Rigidbody`, a `ConfigurableJoint` and a `CapsuleCollider`, so the lamp swings from the hand; they also carry an
  `attach_back` child, the model hung on the back when put away (Lantern, Lantern_DN, Lantern_hooded).
- **Tankards** are cups on a handle; drinking is their attack: startEffect `vfx_MeadSplash` and `sfx_MeadBurp`.

## How they are painted

Four of the five pickaxes, the Hammer, the flint axe and knives and the first swords are painted on one 256 px atlas
(`weapons/_res/weapons1/weapons.png`): little pictures of each part (a grey wedge, a log of haft, a stone) on white,
each using about 1 % of the sheet. Haft colour median (148, 109, 66) to (164, 124, 93); iron pick (138, 137, 138);
shading and grain in a few soft strokes. Later tools have their own 32 to 128 px sheets in the weapon style (flat
fields, a light line on edges, painted wraps); see `weapons.md`, section 5.

## Effects and sounds

| Tool | Effects (from the item's shared data) |
| --- | --- |
| pickaxe | hit: vfx_HitSparks, fx_hit_camshake, sfx_pickaxe_hit_vibration_only, sfx_pickaxe_hit (antler: sfx_axe_flint_hit); block: sfx_metal_blocked (antler: sfx_wood_blocked), vfx_blocked, fx_block_camshake; start: sfx_pickaxe_swing; trigger: fx_swing_camshake; trail `club_trail`; the upgrade glow |
| hoe, cultivator | trigger: sfx_build_hoe, sfx_build_cultivator (they place terrain "pieces") |
| scythe, shovel | start: sfx_woosh_scythe; trail `club_trail` |
| fishing rod | start: sfx_fishingrod_swing; projectile FishingRodFloatProjectile (the float); block: sfx_wood_blocked |
| torch | hit: sfx_club_hit, vfx_torch_hit, fx_hit_camshake; block: sfx_wood_blocked; start: sfx_torch_swing |
| tankard | start: vfx_MeadSplash, sfx_MeadBurp |
| grappling hook | start: sfx_grapplinghook_fire; hit: sfx_grapplinghook_hit; projectiles Projectile_GrapplingHook, Projectile_GrapplingHook_secondary |

**Fire on a torch**, all under `attach/equiped` so it burns only in a hand: `fx_Torch_Carried` (particle systems on
`flameball_flipbook_gradient` for the flames, `embers`, `fog` for smoke, `light_glow` for the flare), a `Point light`
colour (1.0, 0.62, 0.48), intensity 1.5, range 10 m with `LightFlicker`, an `SFX` `AudioSource` (the fire loop) and
`FireWarmth` (a `SphereCollider` with an `EffectArea`: the warmth that keeps the player from freezing). TorchMist has a
blue light (0.48, 0.80, 1.0) and an `AntiMist` sphere. The Lantern: light (1.0, 0.79, 0.29), intensity 1.5, 8 m, and a
`light_glow` flare.

## The dropped tool

`Rigidbody` mass 1 to 1.5 (the Hammer and farming tools 1.5), a `BoxCollider` on the mesh. The torches' sphere
colliders are the `FireWarmth` area and the lanterns' capsules the swinging lamp, both under `equiped`. Like weapons,
tools are not enlarged on the ground.

## Checklist

- Haft through the origin along Z; the working head at +Z, pointing or cutting towards +X.
- Pickaxe about 0.95 m, 280 to 420 triangles; hammer about 0.5 m; torch about 0.85 m with the flame head 10 cm across.
- Fire, light and sound under `attach/equiped`; a lantern's swing is a joint under `equiped`; `attach_back` when it
  should hang on the back differently.
- Reuse the kind's effects by name (table above).

## References

`GameElements/Items/weapons/PickaxeAntler.prefab`, `PickaxeStone.prefab`, `PickaxeBronze.prefab`, `PickaxeIron.prefab`,
`PickaxeBlackMetal.prefab`; `GameElements/Items/tools/Hammer.prefab`, `Hoe.prefab`, `Cultivator.prefab`,
`Scythe.prefab`, `Shovel.prefab`, `FishingRod.prefab`, `Feaster.prefab`; `GameElements/Items/weapons/Torch.prefab`,
`TorchMist.prefab`, `Lantern.prefab`, `Lantern_DN.prefab`; `GameElements/Items/misc/Tankard.prefab`,
`TankardOdin.prefab`; `GameElements/Items/weapons/GrapplingHook.prefab`.

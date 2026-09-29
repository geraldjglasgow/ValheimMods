# Armour

Helmets, chest and leg armour, capes, utility items and trinkets, measured on 2026-09-29 by `measure/items.py` and
`measure/items_armour.py`: `data/items.json` categories `armour.*` and the `armour` section (body layout, what each
armour paints, worn meshes, helmet hair rules, cape cloth). Renders: `codex/out/items/sheets/armour.*.png`; the body's
uv layout over the skin and over the game's armour textures: `codex/out/items/armour/layout_*.png`.

## 1. How the game puts armour on the player

From `VisEquipment` (decompiled on 2026-09-29):

- **Chest and legs are two things.** (a) Textures painted into the player body's own uv layout: the item's
  `m_armorMaterial` (a material on the `Player` shader) supplies `_ChestTex`, `_ChestBumpMap`, `_ChestMetal` (or
  `_LegsTex`, `_LegsBumpMap`, `_LegsMetal`), which `SetChestEquipped` / `SetLegEquipped` copy onto the body's
  material; the texture's alpha decides where armour covers the skin. (b) Extra meshes from the prefab's children
  named `attach_skin` (a skinned mesh whose bones and root bone are replaced by the body's, so it deforms with the
  player) and `attach_<BoneName>` (instantiated under the Player's bone of that name at zero position and rotation).
  No player armour in this game version uses `attach_<BoneName>`; creature gear does (`r.hand`, `L_Hand`).
- **Helmets** attach their `attach` child to `Helmet_attach` (a child of `Head`), or use `attach_skin` (16 of 40).
- **Capes** (item type Shoulder) are `attach_skin` meshes simulated by MagicaCloth 2 (section 6).
- **Utility items and trinkets** use `attach_skin` (worn on the body) plus `attach` (the dropped look).
- A child `attach_cook` is not a bone: the cooking station shows it (`CookingStation` looks for `attach_cook`, then
  `attach`); the Deep North armours carry one. `VisEquipment` logs a missing joint for it and skips it.
- The inactive `attach_skin` stays in the prefab with a full copy of the Player armature (53 bones) under it; the
  dropped item shows a separate model (`dropmodel`, `log`): for most a folded bundle (chest: 36 to 3,092 triangles,
  median 299; capes: a rolled bundle of 36 to 126).

## 2. The body's texture layout

The player body (`Characters/Player/model/body.asset`, 1,514 triangles, 53 bones; the female `bodyfem.asset`, 1,644,
shares the layout: 88 % of their uv area overlaps) is drawn on the `Player` shader: skin `_MainTex`
(`old_PlayerCharacter2/PlayerCharacter_01.png`, 256 px) with the chest and legs textures laid over it.

- **Only the top-left quarter is used.** The uvs run from u 0.01 to 0.50 and v 0.51 to 0.99: on a 256 px armour
  texture the body lives in the top-left 128 x 128 px; the other three quarters are never sampled. The body covers
  9.1 % of the whole texture (36 % of that quarter).
- **Mirrored.** Left and right halves share one set of islands (pairs of islands of 308, 290 and 125 triangles lie on
  top of each other): armour textures are symmetric left to right; a badge on one breast appears on both.
- **Density**: 63 px per metre with a 256 px texture over the 1.9 m body (`game.texel_density`, scale 95). The torso
  strip is about 33 px wide and 38 px tall.

Where each part sits, as boxes in pixels on a 256 px texture (x to the right, y down from the top edge), from each
body vertex's strongest bone (`measure/items_body.regions`):

| Region (bones) | Box x0, y0 to x1, y1 | Share of the body's texture area |
| --- | --- | --- |
| neck and shoulders (LeftShoulder, RightShoulder; the neck's strip) | 35, 5 to 68, 29 | 7 % |
| torso (Spine, Spine1, Spine2) | 35, 14 to 68, 52 | 14 % |
| hips (Hips) | 35, 51 to 64, 63 | 2 % |
| thigh (LeftUpLeg, RightUpLeg) | 36, 44 to 64, 77 | 12 % |
| shin (LeftLeg, RightLeg) | 37, 73 to 64, 102 | 10 % |
| foot (LeftFoot, LeftToeBase ...) | 36, 94 to 65, 126 (plus the sole, a small island near 92, 2 to 113, 10) | 12 % |
| head (Neck, Head, Jaw: the face island) | 97, 26 to 125, 63 | 20 % |
| upper arm (LeftArm, RightArm) | 95, 66 to 121, 95 | 11 % |
| forearm (LeftForeArm, RightForeArm) | 97, 90 to 119, 110 | 7 % |
| hand (LeftHand and fingers) | 100, 109 to 127, 126 | 6 % |

So the texture reads as two columns: on the left, the body from neck to toe (shoulders at the top, then chest and back,
the waist near y 51 where the torso meets the hips, then thighs, shins, the foot at the bottom); on the right, the face
at the top, then the arm from shoulder to fingertips. `layout_skin.png` draws it over the skin; `layout_Armor*.png` over
the game's armour.

**What the game's armour paints** (share of each region's texture area with alpha over half, medians):

| Region | Chest textures (43) | Legs textures (20) |
| --- | --- | --- |
| torso | 0.94 | 0.17 (the waistband) |
| neck and shoulders | 0.88 | 0 |
| upper arm | 1.00 | 0 |
| forearm | 0.97 | 0 |
| hand | 0.02 (gloves on some: Wolf, Fenring, Flametal, Ashlands, Deep North 1.0) | 0 |
| hips | 0 | 1.00 |
| thigh | 0.12 (a tunic's hem) | 1.00 |
| shin | 0 | 1.00 |
| foot | 0 | 0.75 (boots; the sole island is left bare) |
| head | 0.05 | 0 |

- **Sizes**: 256 x 256 for every chest and legs body texture except ArmorBronzeLegs and ArmorIronLegs (128). Normal map
  on 59 of 64; metal map on 13.
- **Alpha is the cut**: 0 where the skin shows, 255 where armour covers; edges are hard (sleeves end in a straight or
  ragged line). The default empty chest is `player_armor_none.png` (32 px, fully transparent); the default legs are
  the underwear (`PlayerCharacter_MaleUnderwearBottoms.png`).
- **Some armour paints nothing on the body.** ArmorIronLegs' `_LegsTex` has its content outside the body's quarter:
  the iron legs are all mesh. ArmorBerserkerUndeadChest paints no region over 5 %.

**How the body textures are painted** (from the overlays): chain mail as rows of rounded pale-grey rings 3 to 4 px
each, lit from the top, over dark gaps (ArmorIronChest); bronze plates as orange squares with a darker rim
(ArmorBronzeChest); quilting as a diamond grid of dark blue with orange studs (ArmorPaddedCuirass); leather tunics as
flat brown with a darker belt band at the waist and soft folds; fur as blue-grey mottling (ArmorWolfChest); hex
scale mail on grey (ArmorFlametalChest). Patterns are tiled at 3 to 6 px a unit; the painting is soft, low-contrast,
and follows the seams of the islands.

## 3. The meshes armour adds

| Category | Items | attach_skin | attach | Triangles of the worn mesh | Its texture |
| --- | --- | --- | --- | --- | --- |
| chest (`armour.chest`) | 44 | 44 | 0 | 60 to 4,984 (770) | 64 to 256 (128) |
| legs (`armour.legs`) | 21 | 17 | 0 | 220 to 5,388 (1,134) | 64 to 256 (128) |
| helmet (`armour.helmet`) | 40 | 16 | 32 | 72 to 2,586 (449) held; 118 to 5,172 (548) skinned | 32 to 256 (96) |
| cape (`armour.cape`) | 12 | 12 | 0 | 132 to 1,313 (210) | 64 to 256 (128) |
| utility (`armour.utility`) | 5 | 3 | 3 | 124 to 768 | 32 to 128 |
| trinket (`armour.trinket`) | 15 | 15 | 15 | 224 to 1,448 (310) | 128 (all share `Trinkets/Models/Trinkets_d.png`) |

- Chest meshes are the parts that stand off the body: shoulder pads and pauldrons, belts and buckles, tunic and dress
  skirts, necklaces, sleeve cuffs (sheet `armour.chest.png`: ArmorBronzeChest is two shoulder cups and a gorget,
  ArmorIronChest a mail shirt's skirt and shoulders, the dresses a full skirt). Leg meshes are boots, greaves, skirts
  and loincloths. The worn mesh has its own material on the `Creature` shader (IronArmorChest_mat, 128 px) separate
  from the body texture (IronArmorChestPlayer_d, 256 px).
- Worn meshes are measured in their bind pose (the prefab's own armature, the Player's 53 bones at 100x scale under a
  -90 degree X rotation); in the game they are drawn at the Player's 0.95.

## 4. Helmets

- **Frame.** `Helmet_attach` sits 1.78 m above the feet in the rest pose, 5 cm in front of the head bone; +Y up, +Z
  forward (the face), +X the character's right; the item keeps its own scale. HelmetIron spans x ±0.105, y -0.056 to
  +0.183 (the rim 6 cm below the attach point), z -0.169 (the neck guard at the back) to +0.121 (the brow).
- **Budgets**: 72 to 2,586 triangles (449), 32 to 256 px (96), 40 to 361 px per metre (79); 0.18 to 0.56 m for
  helmets and hats (HelmetIron 690 tris, 64 px, 0.21 x 0.24 x 0.29 m; HelmetBronze 360; HelmetHat1 172, 64 px).
  Horns, crests and crowns reach 0.46 to 0.8 m (HelmetDrake 660, HelmetFenring 452, HelmetDNMage 1.12 m wide).
- **Hair and beard** (`m_helmetHideHair`, `m_helmetHideBeard`, enum `HelmetHairType`): `Default` keeps the player's
  hair or beard; `Hidden` removes it; `HiddenHat`, `HiddenHood`, `HiddenNeck`, `HiddenScarf` swap the current hair or
  beard item for its variant of that type (each hair and beard item lists them in its own `m_helmetHairSettings` /
  `m_helmetBeardSettings`: 64 variants for HiddenNeck, 38 each for Hat, Hood, Scarf). Over the 40 helmets, hair:
  HiddenHat 12, Default 8, HiddenNeck 7, HiddenScarf 6, HiddenHood 5, Hidden 2; beard: Default 32, Hidden 6,
  HiddenNeck 2. A cap or hat takes HiddenHat, a hood HiddenHood, a helmet with a neck guard HiddenNeck, a mask or
  full helm Hidden (HelmetFenring, HelmetPadded).
- Colliders on the dropped helmet: `BoxCollider` (23) or convex `MeshCollider` (21), under `attach` (disabled when
  worn).

## 5. Budgets, painting and look of chest and legs as a whole

- The body texture carries the look; the mesh carries the silhouette. Early sets are mostly texture (ArmorLeatherChest:
  a 60-triangle stub mesh, the tunic painted), later ones add more mesh (ArmorRootChest 4,746, ArmorBerserkerUndeadLegs
  5,388).
- Worn mesh textures are painted like weapons (flat fields, soft mottling, a painted light edge on metal):
  IronArmorChest's 128 px sheet is orange-brown leather with darker blotches, rows of pale grey mail rings on black
  and a round buckle; see `weapons.md`, section 5.
- Tiers follow the weapons': leather and rags, bronze plates, iron mail, wolf fur and silver, padded black metal and
  lox fur, carapace and eitr, flametal and ask hide, Deep North furs with gold trim.

## 6. Capes

Every cape is an `attach_skin` skinned mesh with a `MagicaCloth` (MagicaCloth 2, `Plugins/MagicaClothV2.dll`) and a
`PlayerClothWindShelter` on the same GameObject; `VisEquipment.SetupCloth` adds the Player's cloth colliders
(`ClothCollider` spheres on Hips, both thighs and shins, Spine1, Spine2, and capsule colliders on both upper arms),
remaps the bones and builds the simulation.

| Setting (serializeData) | Value on the game's capes | Exceptions |
| --- | --- | --- |
| clothType | 0 (mesh cloth) | |
| gravity | 7 | CapeAsksvin 9 |
| damping | 0.1 | CapeAsksvin 0.2 |
| radius (particle) | 0.085 m | CapeAsksvin 0.071 |
| reduction (simple distance) | 0.061 to 0.087 m | |
| distance stiffness | 1 | |
| angle restoration stiffness | 0.15, scaled by a curve from 1 at the collar to 0.1 at the hem | CapeAsksvin 0.05 |
| angle limit | 45 degrees, scaled by a curve from 0 at the collar to 1 at the hem | |
| world inertia | 1 | |
| collider collision mode | 1 | |

- Mesh: 132 to 1,313 triangles (210), 64 to 256 px (128), 30 to 120 px per metre (72); hangs 1.5 to 1.9 m from the
  collar to the hem in the bind pose, 0.7 to 1.4 m wide (CapeDeerHide 135 tris, 0.73 x 1.61 m; CapeLox 608, 1.39 m
  wide with a fur collar; CapeFeather 825 with cut-out feather hem).
- Look: one hide or cloth panel with a ragged or cut hem, often a fur or feather collar at the neck; painted with the
  animal's own texture (CapeTrollHide wears `Characters/Troll/model/material/troll_diffuse.png`).
- Which vertices stay pinned (the collar) is stored in MagicaCloth's own selection data, not read here.

## 7. Utility items and trinkets

- Utility: the megingjord belt, wishbone, ice shoes, skates, demister: 124 to 768 triangles, 32 to 128 px, worn by
  `attach_skin` (the Demister has no worn mesh of its own).
- Trinkets (15): necklaces, bracelets, rings and charms 0.09 to 0.53 m, 224 to 1,448 triangles (310), all on one
  shared 128 px sheet (`Trinkets_d.png`), Standard shader; four have emission.

## 8. Checklist

- Chest or legs: paint a 256 px texture whose alpha covers the regions in section 2, only inside the top-left
  128 px, symmetric; torso strip 35 to 68 px across; give it a normal map; put it on a `Player` material as
  `m_armorMaterial`.
- Add the silhouette as an `attach_skin` mesh weighted to the Player's bones (shoulder pads, skirt, boots): chest 400
  to 1,500 triangles, legs 700 to 1,500, 128 px, `Creature` material.
- Helmet: `attach` in the `Helmet_attach` frame (+Y up, +Z face), 0.2 to 0.3 m, 350 to 900 triangles, 64 to 128 px;
  choose the hair and beard types.
- Cape: `attach_skin` with MagicaCloth set as in section 6; 150 to 600 triangles, 128 px.
- Dropped look: a small folded bundle, a `BoxCollider`, `Rigidbody` mass 10 (chest, legs, capes) or 1 (helmets,
  trinkets).

## References

`GameElements/Items/armor/ArmorLeatherChest.prefab`, `ArmorBronzeChest.prefab`, `ArmorIronChest.prefab`,
`ArmorWolfChest.prefab`, `ArmorPaddedCuirass.prefab`, `ArmorCarapaceChest.prefab`; legs `ArmorLeatherLegs.prefab`,
`ArmorBronzeLegs.prefab`, `ArmorIronLegs.prefab`, `ArmorWolfLegs.prefab`, `ArmorPaddedGreaves.prefab`,
`ArmorCarapaceLegs.prefab`; capes `CapeDeerHide.prefab`, `CapeTrollHide.prefab`, `CapeWolf.prefab`, `CapeLox.prefab`,
`CapeFeather.prefab`, `CapeAsh.prefab`; helmets `GameElements/Items/helmets/HelmetLeather.prefab`,
`HelmetBronze.prefab`, `HelmetIron.prefab`, `HelmetDrake.prefab`, `HelmetPadded.prefab`, `HelmetCarapace.prefab`;
trinkets `GameElements/Items/Trinkets/TrinketBronzeStamina.prefab`, `TrinketIronHealth.prefab`; utility
`GameElements/Items/utility/Wishbone.prefab`, `IceShoes.prefab`, `Demister.prefab`; the body
`Characters/Player/Player.prefab`, `Characters/Player/Materials/PlayerMaterial.mat`.

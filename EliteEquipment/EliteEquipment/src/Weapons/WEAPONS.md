# Weapons (EliteEquipment section 3)

Fills the weapon gaps in `EliteEquipment/features/weapon_gaps.txt` (asked 2026-10-10): 22 new weapons, models by Opus
agents in `ValheimAssets/Assets/Weapons/<Family>/` (one per family, no bundle), then a Sonnet pass for every icon (ours
and the game's weapons of these 11 categories remade) and the one bundle. Written as EliteEquipment's section 3
(`3. Weapons` / `Weapons`, one switch). Built as an AssetLab bench, moved into EliteEquipment on 2026-10-12. Code follows Shields: a copy of the base game weapon (attacks,
animations, stamina, speed, effects, trails), the workshop model in place of the game's under `attach` (transform from
the agent's report), its own name, icon and damage, a recipe beside a same-tier game weapon (station, level, upgrade kit).

## Starting numbers (read from the game 2026-10-10; check with the valheim-weapon-dps skill once in game)

Damage sits on the line between the game's same-type weapons either side; per-level damage is the base weapon's scaled
by the same ratio. Silver weapons add a little spirit as the game's silver gear does.

| Weapon | Asset | Copy | Damage | Beside (recipe) | Cost (+ a level) |
| --- | --- | --- | --- | --- | --- |
| Bronze Greatsword | `ee_greatsword_bronze` | THSwordKrom | slash 46 | SwordBronze, forge 1 | Wood 4 (+2), Bronze 12 (+6), Leather scraps 3 (+2) |
| Iron Greatsword | `ee_greatsword_iron` | THSwordKrom | slash 72 | SwordIron, forge 2 | Wood 4 (+2), Iron 30 (+15), Leather scraps 4 (+2) |
| Silver Greatsword | `ee_greatsword_silver` | THSwordKrom | slash 98, spirit 30 | SwordSilver, forge 3 | Wood 4 (+2), Silver 60 (+30), Iron 8 (+4), Leather scraps 4 (+2) |
| Black Metal Greatsword | `ee_greatsword_blackmetal` | THSwordKrom | slash 124 | SwordBlackmetal, forge 4 | Fine wood 4, Black metal 30 (+15), Linen thread 8 (+5) |
| Bronze Dual Axes | `ee_dualaxes_bronze` | AxeBerzerkr | slash 48, chop 42 | AxeBronze, forge 1 | Wood 8, Bronze 12 (+6), Leather scraps 4 (+2) |
| Iron Dual Axes | `ee_dualaxes_iron` | AxeBerzerkr | slash 67, chop 50 | AxeIron, forge 2 | Wood 8, Iron 30 (+15), Leather scraps 4 (+2) |
| Silver Dual Axes | `ee_dualaxes_silver` | AxeBerzerkr | slash 85, chop 57, spirit 15 | SwordSilver, forge 3 | Wood 6, Silver 40 (+20), Leather scraps 4 (+2) |
| Black Metal Dual Axes | `ee_dualaxes_blackmetal` | AxeBerzerkr | slash 103, chop 65 | AxeBlackMetal, forge 4 | Fine wood 8, Black metal 30 (+15), Linen thread 6 (+4) |
| Carapace Dual Axes | `ee_dualaxes_carapace` | AxeBerzerkr | slash 122, chop 72 | SpearCarapace, black forge 1 | Yggdrasil wood 8, Carapace 16 (+8), Mandible 2 (+1) |
| Bloodgold Dual Axes | `ee_dualaxes_bloodgold` | AxeBerzerkr | slash 158, chop 88 | AxeGold, black forge 4 | Gold 30 (+15), Frostwood 15 (+5), Axe mould 1 |
| Flint Knuckles | `ee_fist_flint` | FistFenrirClaw | blunt 14 | AxeFlint, workbench | Wood 4 (+2), Flint 6 (+3), Leather scraps 4 (+2) |
| Iron Claws | `ee_fist_iron` | FistFenrirClaw | slash 42 | SwordIron, forge 2 | Wood 2, Iron 20 (+10), Leather scraps 6 (+3) |
| Flametal Claws | `ee_fist_flametal` | FistFenrirClaw | slash 100 | THSwordSlayer, black forge 3 | Flametal 20 (+10), Asksvin hide 4 (+2), Morgen sinew 2 (+1) |
| Iron Dagger | `ee_knife_iron` | KnifeSilver | slash 18, pierce 18 | SwordIron, forge 2 | Wood 2 (+1), Iron 10 (+5), Leather scraps 2 (+1) |
| Flametal Dagger | `ee_knife_flametal` | KnifeBlackMetal | slash 60, pierce 60 | SpearSplitner, black forge 3 | Blackwood 4, Flametal 10 (+5), Asksvin hide 2 (+1) |
| Silver Atgeir | `ee_atgeir_silver` | AtgeirBlackmetal | pierce 85, spirit 15 | SwordSilver, forge 3 | Wood 10, Silver 30 (+15), Iron 4 (+2), Leather scraps 3 (+1) |
| Flametal Atgeir | `ee_atgeir_flametal` | AtgeirBlackmetal | pierce 150 | SpearSplitner, black forge 3 | Blackwood 10, Flametal 30 (+15), Asksvin hide 4 (+2) |
| Silver Axe | `ee_axe_silver` | AxeBlackMetal | slash 80, chop 55, spirit 15 | SwordSilver, forge 3 | Wood 4, Silver 20 (+10), Iron 2 (+1), Leather scraps 2 (+1) |
| Flametal Axe | `ee_axe_flametal` | AxeBlackMetal | slash 140, chop 80 | SpearSplitner, black forge 3 | Blackwood 4, Flametal 20 (+10), Asksvin hide 2 (+1) |
| Flametal Greataxe | `ee_battleaxe_flametal` | BattleaxeBlackmetal | slash 160, chop 60 | THSwordSlayer, black forge 3 | Blackwood 10, Flametal 30 (+15), Asksvin hide 5 (+3) |
| Black Metal Bow | `ee_bow_blackmetal` | BowDraugrFang | pierce 60 | SwordBlackmetal, forge 4 | Fine wood 10 (+5), Black metal 15 (+8), Linen thread 10 (+5) |
| Black Metal Spear | `ee_spear_blackmetal` | SpearWolfFang | pierce 95 | SwordBlackmetal, forge 4 | Fine wood 10 (+5), Black metal 10 (+5), Linen thread 4 (+2) |

The game's neighbours (base damage): swords Bronze 35, Iron 55, Silver 75 + spirit 30, Black Metal 95; greatswords Krom
150, Slayer 170 (so the greatsword line runs 46, 72, 98, 124, 150); dual axes Early 30 / chop 35 (Meadows), Berserkir
140 / 80 (Ashlands); fists Bear 25, Fenrir 60, Vile 20 + pierce 60, Gold blunt 114; knives Copper 12/12, Silver 25/25 +
spirit 12, Skoll and Hati 45/45, Gold 75/75; atgeirs Iron 65, Black Metal 105, Himminafl 85 + lightning 40, Gold 182;
axes Iron 60/50, Black Metal 100/60, Jotun Bane 80/70 + poison 40, Gold 176/90; greataxes Skull Splittur 130/70, Gold
188/40; bows Draugr Fang 47 + poison 5, Spine Snap 72 + spirit 5; spears Fang 75, Carapace 115.

## Model placement (from the workshop reports)

- **Black Metal Spear** (`Assets/Weapons/Spears`, 436 triangles, 2.40 m): `GAME_ATLAS` on `AtgeirBlackmetal`
  (`attach/default`, `blackmetalatgeir`, metal gloss 0.845). Copy `SpearWolfFang` (`attach/{UpgraderGlow, fangspear
  (scale 0.1), collider}`): remove `attach/fangspear`, our `model` under `attach` at identity (built in the attach frame:
  fist at the origin, tip at z -1.00, butt +1.40); keep `UpgraderGlow`; collider (0.12, 0.06, 2.40) at (0, 0, 0.20) or
  the game's. Thrown: `projectile_wolffang` - its `fangspear` out, our `model` at (0, 0, -0.854), rotation (0, 180, 0).
- **Silver and Flametal Axe** (`Assets/Weapons/Axes`, 618 / 694 triangles, 0.98 / 0.99 m): `GAME_ATLAS` on `ShieldSilver`
  (`attach/model`, `SilverShield_Mat`) and `ShieldFlametal` (`attach/default`, `Shield_Flametal_mat`), styles off. Copy
  `AxeBlackMetal` for both (`attach/{UpgraderGlow, Model (mesh + MeshCollider, scale 1.15), equiped/trail/{base, tip}}`):
  remove `Model`, our `model` and `col_box_<asset>` under `attach` at identity (attach frame: fist at the origin, haft +Z,
  edge +X). Trail: keep the game's (0.223, 0, 0.506)-(0.237, 0, 0.770) or silver (0.24, 0, 0.50)-(0.25, 0, 0.78), flametal
  (0.26, 0, 0.48)-(0.27, 0, 0.80). `UpgraderGlow/Particle System` emits from the game mesh: point it at ours.
- **Silver and Flametal Atgeir** (`Assets/Weapons/Atgeirs`, 732 / 740 triangles, 2.74 / 2.80 m): `GAME_ATLAS` on
  `SwordSilver` (`attach/Viking_Sword (1)`, `weapons1`, 256 px, metal gloss 0.75) and `SpearSplitner` (`attach/default`,
  `Splitner_mat`, metal gloss 0.846). Copy `AtgeirBlackmetal` for both: switch off `attach/default` (it also carries the
  box collider), our `model` under `attach` at identity (the prefab root is the attach frame; haft along (0.344, -0.136,
  0.929)), our `col_box_<asset>`. The game's trail (0.621, -0.205, 1.389)-(0.752, -0.309, 1.928) fits.
- **Black Metal Bow** (`Assets/Weapons/Bows`, 808 triangles, 1.71 m): `GAME_ATLAS` on `AtgeirBlackmetal` (`attach/default`,
  `blackmetalatgeir`). No game bow bends (one rigid mesh each; the draw only animates the player), so it is rigid too.
  Copy `BowDraugrFang` (`attach/{UpgraderGlow, DraugrFangBow (mesh, BoxCollider, poison particles), Point light}`, root
  `LODGroup`): remove `DraugrFangBow` and `Point light` (or keep DraugrFangBow's rotated BoxCollider, removing only its
  renderer, filter and particle child), our `model` under `attach` at identity (the game's exact hold baked in), our two
  `col_box_bow_*` under `attach`; point the `LODGroup` at our renderer or remove it. Points: `grip`, `string_top`,
  `string_bottom`, `nock`.
- **Iron and Flametal Dagger** (`Assets/Weapons/Daggers`, 286 / 368 triangles, 0.53 / 0.58 m): `GAME_ATLAS` on `SwordIron`
  (`attach/Viking_Sword (1)`, `weapons1`, 256 px, metal gloss 0.75) and `THSwordSlayer` (`attach/default`,
  `NiedhoggSlayer_mat`, Unity Standard, 64 px, gloss scale 0.819). Iron copies `KnifeSilver`, flametal `KnifeBlackMetal`:
  replace `attach/default` (renderer, mesh, BoxCollider) with our `model` at identity (attach frame, 12 degree lean baked
  in) and `col_box_<asset>`; the game's trail (0.061, 0, 0.14)-(0.043, 0, 0.434) fits; `UpgraderGlow` at our mesh.
- **Bronze, Iron, Silver and Black Metal Greatsword** (`Assets/Weapons/Greatswords`, 330 / 490 / 476 / 450 triangles,
  1.94 to 1.96 m): `GAME_ATLAS` on the tier's one-handed sword: `SwordBronze`, `SwordIron`, `SwordSilver`
  (`attach/Viking_Sword (1)`, `weapons1`, 256 px, metal gloss 0.75) and `SwordBlackmetal` (`attach/default`,
  `blackmetalsword`, 128 px, metal gloss 0.772). Copy `THSwordKrom` for all four (`attach` at (0, 0.01, 0.5) turned 180
  about Y; `attach/Krom` renderer + BoxCollider): hide `attach/Krom`, our `model` under `attach` at identity (fist at the
  origin, blade +Z, edges +-X), our `col_box_<asset>`. Krom's trail and UpgraderGlow under `attach` stay.
- **Flametal Greataxe** (`Assets/Weapons/Greataxes`, 716 triangles, 1.82 m): `GAME_ATLAS` on `AxeBerzerkr`
  (`attach/default`, `berzerkr_mat`, 64 px, metal gloss 0.37: raise on our copy if it should shine more). Copy
  `BattleaxeBlackmetal` (`attach/{UpgraderGlow, battleaxe (mesh + a ~5 m BoxCollider), equiped/trail/{base, tip}}`):
  switch off `attach/battleaxe`, our `model` under `attach` at identity, our `col_box_ee_battleaxe_flametal` (needed for
  the dropped item); trail base (0.20, 0, 1.00), tip (0.43, 0, 1.40) (the game's (0.116, -0.009, 0.874)-(0.311, 0, 1.397)).
- **Flint Knuckles, Iron and Flametal Claws** (`Assets/Weapons/Fists`, 436 / 876 / 1,076 triangles per pair):
  `GAME_ATLAS` on `KnifeFlint` (`attach/knife`, `weapons1`), `SwordIron` (`attach/Viking_Sword (1)`, `weapons1`) and
  `ShieldFlametal` (`attach/default`, `Shield_Flametal_mat`, styles off). Copy `FistFenrirClaw` for all three. The game's
  fists: `attach_skin` (inactive, first) holds a SkinnedMeshRenderer on the player's 53 bones (`VisEquipment.AttachItem`
  takes the first `attach`/`attach_skin` and gives it the body's bones), `attach` is the dropped look (renderer + box).
  Dropped: our `model` mesh on `attach` at identity, the material copy, our box. In hand: the SkinnedMeshRenderer gets a
  copy of our mesh weighted 1.0 to bone 29 (RightHand) where z > -0.10, else bone 10 (LeftHand), the game claw's 53
  bindposes with [29] = S_R and [10] = S_L * Euler(180, 0, 0) * Translate(0, 0, 0.2) (S_R / S_L the TRS of
  `m_rightHand` / `m_leftHand`); exact numbers in the family README.
- **Six Dual Axes** (`Assets/Weapons/DualAxes`, 584 to 1,004 triangles a pair, 0.78 to 0.84 m an axe): `GAME_ATLAS` on
  `AxeBronze` (`attach/Viking_Axe`, `Viking_Axe_bronze`), `AxeIron` (`attach/Viking_Axe`, `Viking_Axe`), `KnifeSilver`
  (`attach/default`, `silverknife`), `AxeBlackMetal` (`attach/Model`, `blackmetalaxe`), `SpearCarapace`
  (`attach/carapacespear`, `carapacespear_mat`), `AxeGold` (`attach/Model`, `nordaxe_mat`). Copy `AxeBerzerkr`:
  `attach_skin` (first, inactive) holds one SkinnedMeshRenderer with both axes on the body's 53 bones, `attach` the
  dropped pair (two `default` renderers + boxes), trail at `berzerkr/equiped/trail`. Our prefab: `model/<asset>` the right
  axe in the RightHand_Attach frame, `left` (shown at (-0.42, 0, 0)) the left axe in the LeftHand_Attach frame (the right
  mirrored in Y). Held mesh built at runtime: right vertices `inverse(bindposes[29]) * A_R * v`, left
  `inverse(bindposes[10]) * A_L * v`, weight 1 on bone 29 / 10, bindposes from the game mesh; A_R / A_L the hand attach
  matrices (position (0, 0.00105, 0.0003), scale 0.01, rotation (-0.5, -0.5, 0.5, 0.5) / (0.5, 0.5, 0.5, 0.5)). Dropped:
  right at (0.0591, 0.0066, 0.1622) rot (-0.0364, -0.9718, 0.0410, 0.2292), left at (-0.2084, -0.0119, 0.1941) rot
  (0.9676, 0.1477, 0.2044, 0.0116). Trail kept (heads a little lower than Berzerkr's: check in game).

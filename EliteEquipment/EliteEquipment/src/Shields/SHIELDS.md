# Shields (EliteEquipment section 2)

A shield of each kind for every biome the game has none for, and the game's own shields' icons drawn the same way. The
game has the Bronze (Black Forest), Iron (Swamp), Carapace (Mistlands) and Nord Buckler (Deep North); this adds bucklers
for Meadows, Mountains, Plains and Ashlands (asked 2026-10-09, new workshop models) and the Bronze Shield, the Black
Forest's round shield (asked 2026-10-10, `EliteEquipment/features/weapon_gaps.txt`). Was the Bucklers bench until
2026-10-10. Section 2 (`2. Shields` / `Shields`); built as an AssetLab bench, moved into EliteEquipment on
2026-10-12.

| Shield | Prefab | Model | Copied from | Beside (recipe) | Block | Deflection | Weight | Durability | Cost (+ a level) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Wood Buckler | `EE_BucklerWood` | `ee_buckler_wood` | ShieldWood | ShieldWood, workbench 1 | 4 | 20 | 3 | 200 | Wood 8 (+4), Leather scraps 4 (+2), Resin 2 (+1) |
| Silver Buckler | `EE_BucklerSilver` | `ee_buckler_silver` | ShieldIronBuckler | ShieldSilver, forge 3 | 45 | 30 (+5) | 4 | 200 | Silver 10 (+5), Fine wood 4 (+1) |
| Black Metal Buckler | `EE_BucklerBlackmetal` | `ee_buckler_blackmetal` | ShieldIronBuckler | ShieldBlackmetal, forge 3 | 61 | 40 (+5) | 4 | 200 | Black metal 10 (+5), Fine wood 4 (+1), Chain 2 (+1) |
| Flametal Buckler | `EE_BucklerFlametal` | `ee_buckler_flametal` | ShieldIronBuckler | ShieldFlametal, black forge 3 | 83 | 40 (+5) | 4 | 300 | Flametal 10 (+5), Blackwood 4 (+1), Asksvin hide 2 (+1) |
| Bronze Shield (round) | `EE_ShieldBronze` | `ee_shield_bronze` | ShieldBanded | ShieldBronzeBuckler, forge 1 | 30 | 30 (+5) | 5 | 200 | Wood 10 (+5), Bronze 8 (+4) |
| Silver Tower Shield | `EE_TowerSilver` | `ee_tower_silver` | ShieldIronTower | ShieldSilver, forge 3 | 78 | 125 (+5) | 5 | 200 | Fine wood 15 (+10), Silver 10 (+5) |
| Carapace Tower Shield | `EE_TowerCarapace` | `ee_tower_carapace` | ShieldBlackmetalTower | ShieldCarapace, black forge 1 | 122 | 150 (+5) | 5 | 250 | Carapace 25 (+12), Scale hide 5 (+3), Eitr 10 (+3) |

Bucklers parry 2.5x, round shields 1.5x, towers none and -10% movement (`ShieldKind`; the tower hold is the Iron
Tower Shield's, (0.23, -0.97, -0.06)); all +6 block a level, -5% movement, +50 durability a level, the biome shield's upgrade kit. The game's own,
read in game 2026-10-09: Bronze Buckler 16 / 30 / 3 kg, Iron 28 / 30, Carapace 78 / 50 (+5), Nord 88 / 30 / 400.

- **Code.** `ShieldSets` (the numbers), `ShieldItems` (prefix on ZNetScene.Awake, ObjectDB.Awake, CopyOtherDB: makes the
  items once, on every peer, and adds them to the lists), `ShieldItem` (copy, name, icon, stats), `ShieldModel` (the
  workshop model under `attach`, the game item's material, the `grip` point on the hand), `ShieldRecipes` (beside the
  biome's shield), `ShieldIcons` (the game's bucklers and round shields show the workshop's remade `ee_icon_<prefab>[_<variant>]` icons while
  the switch is on, their own when off; ItemCopies), `ShieldsSwitch` (ObjectDB postfixes and the switch: recipes and
  icons), `ShieldsSettings` (`2. Shields` / `Shields`, on, synced), `ShieldKind` (parry and hold per kind). Words: the asset
  name (`$ee_buckler_wood`, `$ee_shield_bronze`) and `_desc`.
- **Icons** (user 2026-10-10): all eight bucklers' icons, ours and the game's four remade, rendered alike and about 57 %
  of the slot (10 % under the game's Bronze and Iron Buckler); every round shield's icon, ours and the game's nine
  craftable ones remade (one per style variant), at the game's Flametal Shield's 83 %.
- **The hold.** The workshop model is front +Z, up +Y, with a `grip` point; `ShieldKind.Hold` turns it as the game's
  shields of its kind lie in their attach (bucklers: boss out along (0.49, -0.87, -0.05); round shields, the Wood and
  Banded Shield: (0.30, -0.96, -0.02); measured 2026-10-10) and `ShieldModel` moves it so the grip sits on the attach point. The first try used the silver shield's turn (12 degrees): the board faced too far
  forward (user: "doesn't look right when equipped").
- **The models.** Workshop family `Assets/Gear/Shields` (was `Bucklers`; `build.ps1` builds all, `BRIEF.md`), bundle
  `ValheimAssets/out/bundles/ee_shields.windows` / `.linux`: per prefab `model/<name>`, `grip`, one `col_box_<name>`, a
  `<name>_icon` sprite; 516 to 848 triangles, 64 px. All 0.556 m across, the Bronze Buckler's size (user 2026-10-10;
  first built 0.60 to 0.66 m): one number, `SIZE` in `buckler.py`, scales the whole family (each model is designed on its
  own board and scaled as one). Icons fill about 63 % of the slot, as the Bronze and Iron Buckler's (`MARGIN` in
  `icons.py`; first 93 %).
- **Metal shine.** The workshop bakes a metal mask (`METAL` in a model.py; red 1 on metal) onto the placeholder's
  `_MetallicGlossMap`; `ShieldModel.Look` puts it on the game material with `GameMaterials.Metal` (BundlePrefabs) at the
  biome shield's metal gloss (`MetalGloss`: Silver and Black Metal 0.65, Flametal 0.77; the Wood Buckler has none).
- **The game shields' own material** (user 2026-10-10): the Silver and Black Metal Bucklers' UVs lie on the Silver and
  Black Metal Shields' own 128 px atlases (workshop `GAME_ATLAS`; the bundle carries no texture of theirs). `Look` names
  the shield; `ShieldModel.GameLook` borrows its body material (`attach/model`, `attach/default`) as it is, styles off
  (`GameMaterials.Unstyled`), never `Plain`. A game update that repaints those atlases changes these two bucklers.
- **Icons** render the real material (metal mask, the game's gloss, normal map, a sun from the upper left, a sky to
  reflect), so the metal shines as on the game's shield icons.
- **Silver Buckler** (user 2026-10-10): silver rim (notched), fine-wood planks, 12 silver studs and a raised low-poly
  silver wolf head in the middle (after the game's TrophyWolf) in place of the boss and bar; 668 triangles.

## In-game checklist

- [ ] `spawn EE_BucklerWood` (and the other three, and `EE_ShieldBronze`): name, icon, tooltip numbers; no error in the log.
- [ ] Equipped: in the hand (blocking, walking, running, parry), on the back when sheathed, beside the game's Iron Buckler.
- [ ] Dropped on the ground (colliders, it rests flat), on an item stand.
- [ ] Recipes beside their shields at the right station and level; upgrade to quality 2 and 3.
- [ ] Block and parry sound (metal for three, wood for the Wood Buckler).
- [ ] The switch off: no recipes; bucklers already made still load and work.
- [ ] A second player sees it held (multiplayer: the prefab is registered on every peer).

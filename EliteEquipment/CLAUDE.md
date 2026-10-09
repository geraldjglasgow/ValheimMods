# CLAUDE.md - EliteEquipment

More from the game's own equipment, version 0.1.0. Its one feature so far is Boots: the game's 20 player leggings
split into trousers and boots worn on the feet, 80/20 stats and cost. Boots were built as OpenKeep's section 16
(2026-10-07) and moved here on 2026-10-08 (see "Why its own mod"). This file is the code map, the patched methods, the
decisions and the in-game test checklist; the README is the store page.

Rules that still apply to every change:

- Own names everywhere: GUID `milkyteam.eliteequipment`, config `milkyteam.eliteequipment.cfg`, words `$ee_*`, prefabs
  `EE_*`, ZDO keys `EliteEquipment.<name>`, embedded resources `EliteEquipment.assets.<path>`.
- Verify every patched signature against a fresh `ilspycmd` decompile of the game's `assembly_valheim.dll` in the
  scratchpad, never in a repository, and read the method body.
- Functions at most 24 lines from brace to brace (lambdas and local functions count), classes at most 300 lines, one
  responsibility per class. Prefix, postfix and finalizer patches only; no transpilers. Every patch class is applied on
  its own in `Plugin.PatchEverything`, so one failure is logged and the others still apply.
- Settings are read at use time (`ConfigEntry.Value`), never cached. Gameplay settings are synced and covered by
  `Lock Configuration`.
- Nullable is off. Zero compiler warnings. Build from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build EliteEquipment/EliteEquipment/EliteEquipment.csproj -c Release`; the build
  copies the merged DLL to `dist/` and to the r2modman profile `LocalTesting`. Never launch or kill the game from a script.

## Layout

```
EliteEquipment/EliteEquipment/src/
  Plugin.cs                 entry: Lock Configuration, BootsModule.Initialize, patches per class, Synced.Finish,
                            Guard.Install last
  Core/
    Language.cs             the ee_* words, handed to the game's localization (Localization.SetupLanguage postfix)
    InventoryChanges.cs     the count of every Inventory.Changed (prefix); WornBoots keeps its answer on it
  Boots/                    section 1, Boots: the game's 20 leggings split into leggings and boots
    BootsModule.cs, BootsSettings.cs   1. Boots / Separate Boots (on by default), the ee_boots_* words
    BootSet.cs, BootSets.cs the 20 sets: key (the split's too), game leggings, EE_Boots_<Key>, name token; lookups
                            (the boots by token, prefab hash; the leggings by prefab name, hash and name token)
    BootsItems.cs           prefix ZNetScene.Awake, ObjectDB.Awake, CopyOtherDB: builds once, adds the boots to the lists
    NativeSplit.cs          assets/boots/native_split.txt: per set the boots triangles, the paint's ankle row, the trousers' paint edits
                            (Bronze, Leather, Troll leather, Caller, Ask, Embla and Eitr-weave carried down to the ankle),
                            the boots triangles the trousers draw too (Lox's calf fur)
    MeshCut.cs              a game skinned mesh cut in two by triangles (some shared), its buffers copied back from the GPU (most are not readable)
    SkinSplit.cs            a leggings' attach_skin meshes cut into trousers [0] and boots [1]; none headless or when the mesh differs
    BootsItem.cs            one boots item: a bench copy of its leggings, its skin cut to the boots, name, icon, a fifth of the stats and weight
    BootsDrop.cs            the dropped look: the boots mesh unskinned, turned into the body's space at the left foot's bind pose
    LegsLook.cs             VisEquipment.AttachArmor postfix: a player's split leggings draw their trousers parts while on
    PaintCut.cs, Paint.cs   the leggings' _LegsTex cut at the ankle row into trousers and boots paint, the edits applied (rags: own boots paint); texels in memory
    BootsIcons.cs           the workshop's boots and trousers icons (assets/boots), decoded once, none on a dedicated server
    LegsIcons.cs            the leggings show the trousers icon while the switch is on, their own (remembered) when off
    BodyPaint.cs            the body's _LegsTex for what is worn: trousers paint (or the game's) with the boots' laid over, made once per mix;
                            the edited leg metal and normal maps beside it
    PaintMaps.cs            the leggings' _LegsMetal and _LegsBumpMap with the trousers' paint edits copied in too (every channel)
    BootsStats.cs           the 80/20 split of armour, eitr, modifiers (boots: flat +5% movement); set sizes + 1; from remembered originals
    BootsWeight.cs          the leggings' weight share, taken where the game reads weight (GetWeight, GetNonStackedWeight, GetEquipmentWeight)
    BootsRecipes.cs         a boots recipe beside each leggings', the cost split a fifth / the rest, priced up for early sets
    BootsSwitch.cs          ObjectDB.Awake/CopyOtherDB postfix and the switch: stats, recipes, icons, look, take off, inventory weight
    WornBoots.cs            the worn pair (the equipped flag), cached for the local player; Wear, TakeOffAll
    BootsEquipPatches.cs    EquipItem prefix, IsItemEquiped, IsItemTypeEquiped, UnequipItem, UnequipAllItems postfixes
    BootsStatPatches.cs     armour, resistances, hit durability, set count, weight, modifiers, eitr, breaking, armour difference
    BootsShow.cs            Humanoid.SetupVisEquipment postfix (ZDO EliteEquipment.boots), VisEquipment.UpdateEquipmentVisuals postfix (and the paint)
    WornBootsLink.cs        the worn pair as an EliteCrafting equipment provider, so inscriptions on worn boots count (soft dependency)
    BootsLevels.cs          with EliteCrafting, each pair of boots gets its leggings' item level (SetItemLevel), when the database wakes
  Fitted/                   leggings made from the wearer's own body, in their set's look (part of the Boots look)
    FittedLook.cs           SetLegEquipped postfix and BootsShow's check: fitted leggings on, the game's hidden, the body trimmed
    LegStyle.cs, LegStyles.cs   a look's data (chest to copy, atlas recipe, density, thickness, under the boots or into
                            them, bands, buckle); Iron (mail), Bronze (plates), Wolf (silver mail, fur), Carapace (scales),
                            Flametal (heat-tinted mail), Protector (leather, flame bands), Vanguard (teal, gold vine)
    FittedBuild.cs          per look, body mesh and lower end, made once: the leggings' mesh and the body without what they cover
    BodySurface.cs          a body mesh read for it: rest positions (metres), welded outward normals, the arm/head vertices
    LegRegion.cs            the covered body triangles (waist 1.12 m to the ankle, or into the boots), the lower end
    BootTop.cs              how high a pair of boots reaches: its cut meshes' top or its paint's on the body
    Unwrap.cs               the texture wrapped round the hips and each leg, seams at the back and the inner legs
    FittedShell.cs          the covered triangles again, pushed out the look's thickness with the body's weights (sunk to
                            2 mm inside boots worn over them); the lips at its edges
    FittedBands.cs          the look's bands (belt, straps, trims, ankle cuff) and the buckle
    FittedCorner.cs, FittedMesh.cs   a clipped band's corners (weights mixed); the mesh as it is made
    FittedAtlas.cs, FittedMaterial.cs   a look's atlas copied from its chest's textures and body paint (the albedo always
                            opaque); its material (a chest's glow map cleared)
    ChestFit.cs             SetChestEquipped postfix and FittedLook's calls: a chest that lay on baggy trousers drawn in while
                            fitted leggings are on, its front cloth attached; its own mesh back when they come off
    ChestShape.cs           which chests (the Protector breastplate), where their hanging cloth is on the texture, how short
    ChestVertices.cs        a chest's vertices while drawn in: rest positions, weights, what each is below the waist (arm,
                            plate, cloth), how far it stands out beyond the leggings and a gap (wider behind)
    ChestPull.cs            the drawn-in chest: plates lean in by a smoothed rate per direction, hand-weighted fringes reweighted,
                            plates follow the thigh more the lower they hang (less behind); ChestParts
    ChestCloth.cs           the front cloth hung clear of the body and plates, shortened, split into a mesh of its own; the back
                            cloth dropped
    ChestClothSim.cs        the front cloth drawn on the body's bones and moved by MagicaCloth (the linen cape's settings, wind
                            0.05), fixed at the belt by a paint map, pushed by a collider round each thigh placed from the bind pose
    ClothColliders.cs       destroys the colliders a cloth made on the bones with its holder
    BodyEnvelope.cs         the body's hips and legs as seen from the hips' axis: reach per height and direction
    BoneBlend.cs            skin weights mixed, the four strongest kept
EliteEquipment/EliteEquipment/assets/boots/
                            native_split.txt: where the game's leggings are cut into trousers and boots (ValheimAssets
                            Assets/Gear/SeparatedLegArmor/NativeSplit_v001, coordinates only); the trousers' and boots'
                            icons pants_<set>.png and boots_<set>.png (64 px, NativeSplit_v001/Icons/v001/64; pants_iron.png,
                            pants_bronze.png, pants_wolf.png, pants_carapace.png, pants_flametal.png, pants_protector.png and
                            pants_vanguard.png are the fitted looks', pants_caller.png, pants_ask.png, pants_embla.png,
                            pants_eitrweave.png and pants_lox.png the v002 revisions',
                            Assets/Gear/FittedLegs/ee_fitted_legs/Icons/64) and the
                            rag shoes' paint paint_boots_rag.png (256 px, Revisions/Rag_v002); embedded as
                            EliteEquipment.assets.boots.<file>
```

Libraries merged: SyncedConfig (with Charter, ConfigReload, YamlConfig, YamlDotNet), PatchGuard, ItemCopies (the split
stats and icons into the prefab and every live copy), PlateColumn (`EmbeddedSprite`), BundlePrefabs (`PrefabBench`,
`SpriteCrop`, `TexturePixels`: `ReadLinear` added for the fitted looks' normal and metal maps) and EliteCraftingLink.
The game's `MagicaClothV2.dll` is referenced (not merged) for the chest cloth's simulation.

## Patched game methods

Prefix `ZNetScene.Awake`, `ObjectDB.Awake` and `ObjectDB.CopyOtherDB` (the boots built once and added to the lists before
the game indexes them); postfix `ObjectDB.Awake` and `CopyOtherDB` (recipes, stats, icons, look); prefix
`Humanoid.EquipItem` (`Priority.High`: boots worn by EliteEquipment, the game's method skipped); postfix
`Humanoid.IsItemEquiped`, `IsItemTypeEquiped`, `UnequipItem`, `UnequipAllItems`; postfix `Player.GetBodyArmor`,
`Player.ApplyArmorDamageMods`, `Humanoid.GetSetCount` (private), `Humanoid.GetEquipmentWeight` (the pair's weight in,
the leggings' fifth out), `Player.UpdateModifiers` (private), `Player.GetEquipmentEitrRegenModifier`,
`Humanoid.UpdateEquipment` (private: a broken pair comes off); prefix `Player.DamageArmorDurability` (the pair is one of
the pieces a hit may wear down; the original skipped only while a pair is worn) and `Player.TryGetArmorDifference`
(boots only); postfix `ItemDrop.ItemData.GetWeight` and `GetNonStackedWeight` (a split leggings at 80% while on);
postfix `VisEquipment.AttachArmor` (private: the 20 leggings' skins on players draw their trousers parts, while on);
postfix `Humanoid.SetupVisEquipment` and `VisEquipment.UpdateEquipmentVisuals` (private: the worn pair, the body's
leg paint and the fitted leggings' check on every client); postfix `VisEquipment.SetLegEquipped` (private: fitted
leggings put on or taken off when the leggings change); postfix `VisEquipment.SetChestEquipped` (private: a new chest
drawn in over fitted leggings at once). Core: prefix `Inventory.Changed` (private: the change count), postfix
`Localization.SetupLanguage` (the words).

## Config sections and keys

`General` (`Lock Configuration` true), `1. Boots` (`Separate Boots` true, synced; PackPanel reads this entry by section
and key). Meanings are in each entry's description in the .cfg.

## Network and file names

- Item prefabs `EE_Boots_<Key>` (20, networked items, copies of the game's leggings; PackPanel knows boots by this
  prefix), words `ee_boots_<key>`, `ee_boots_desc`, `ee_boots_off`; recipes `Recipe_EE_Boots_<Key>`; the split data
  `EliteEquipment.assets.boots.native_split.txt` (no bundle: the meshes, materials and paint are the game's own, cut at
  runtime), icons `EliteEquipment.assets.boots.boots_<set>.png` and `pants_<set>.png`, the rag shoes' paint
  `EliteEquipment.assets.boots.paint_boots_rag.png`; ZDO key `EliteEquipment.boots` (int, the worn boots prefab's hash, 0
  for none) on the player and its ragdoll, written by the owner when it changes. No RPC.
- Fitted leggings: a child `EE_FittedLegs` (a SkinnedMeshRenderer on the body's bones) under the player's `Visual`,
  meshes `EE_Fitted<Key>_<body>` and `<body>_ee_fitted` (the trimmed body), material `EE_Fitted<Key>`, textures
  `EE_Fitted<Key>_<0 albedo, 1 normal, 2 metal>`; all made per client, nothing networked or saved.
- EliteCrafting equipment provider id `EliteEquipment.Boots` (through EliteCraftingLink, only with EliteCrafting); item
  levels set through its `SetItemLevel` for the 20 `EE_Boots_*` prefabs.
- Drawn-in chests: the chest renderer's mesh swapped for `<chest mesh>_ee_fitted`; the front cloth a child `EE_ChestCloth`
  (a SkinnedMeshRenderer, mesh `<chest mesh>_ee_cloth`) under the player's `Visual`, its MagicaCloth on a child
  `EE_ChestCloth_Cloth`, its colliders `EE_ChestClothThigh` under the thigh bones, paint map `EE_ChestClothPaint`; all per
  client, nothing networked or saved.

## Decisions

### Why its own mod

- A GitHub comment on OpenKeep (2026-10-08): "adjusting inventory/crafting/etc does not belong in OpenKeep ... a Mod
  designed around managing the base shouldn't be modifying equipment, especially when it splits out the functionality
  of existing equipment into new slots/gear". The user agreed for the boots and picked a standalone mod, named
  EliteEquipment, so only players who want split armour install it. OpenKeep 4.2.0 was the last version with boots.
- Nothing carries over from OpenKeep: new prefab names (`OpenKeep_Boots_*` became `EE_Boots_*`, so boots crafted under
  OpenKeep do not load; the switch was off there by default), new words, ZDO key and config. No migration, as for any
  removed feature.
- The switch is on by default: installing the mod is the choice (in OpenKeep it was off, since storage players should
  not find their armour split without asking).
- Weight no longer goes through OpenKeep's Stacks, which wrote the leggings' 80% inside its own apply. The leggings'
  `m_weight` is never written: `BootsWeight` takes the share where the game reads weight (`GetWeight`,
  `GetNonStackedWeight`; `GetEquipmentWeight` reads the field, so its postfix takes the fifth off). So OpenKeep's Stacks,
  or any mod that writes item weights, keeps its value and the share applies on top. The boots are made at a fifth of
  their leggings' weight at build time (the game's own, before any such mod applies) and are an ordinary item to them.
  The switch recounts the local player's inventory weight (`Inventory.UpdateTotalWeight`), since nothing else changed.
- Every patch is applied whether the switch is on or off; each returns early while off (as OpenKeep's features do).

### Boots

- Asked for on 2026-10-07 (to OpenKeep): "chatgpt just separated the vanilla valheim armor into boot and legs, we need
  the boots to be an equipable item that goes on feet ... take the stats and split them between the boots and legs, 80%
  legs 20% boots. We need recipies for the boots ... packpanel needs a boots slot if this boots feature is enabled."
- One switch, `1. Boots / Separate Boots`. The boots items always exist, so boots already made survive the switch going
  off; off, they cannot be put on and a worn pair comes off.
- Boots are Legs items (tooltips, armour displays, Epic Loot, EliteCrafting treat them as armour) worn by EliteEquipment,
  never in `m_legItem`: the equipped flag marks the worn pair, `EquipItem` is answered for boots, the game's
  `UnequipItem` takes them off. A pair flagged in a save stays on at load even while the switch is off, because a
  client loads its character before the server's value may have arrived; the switch going off takes it off.
- The split (judgement calls; the user said only "80% legs 20% boots"): armour, armour per level, eitr regen and every
  equipment modifier the game sums, and weight; movement is the exception on the boots: every pair gives a flat +5%
  (user, 2026-10-07: "instead of taking away movement speed"), while the leggings keep 80% of their own. Resistances and
  equip effects cannot be split, so they stay on the leggings; durability stays whole on both (a hit wears down one
  random piece, boots included). The boots join the leggings' set and every piece of that set needs one more, so the
  full set is what it was before, in one more piece. The recipe is split the same way: a fifth of each material
  (rounded half up) to the boots, the rest kept; boots always cost at least one material and at least one per upgrade
  level when the leggings do. Then the boots are priced up (2026-10-08, user: "triple crafting cost for rag, leather,
  troll, bronze, bear, roots, vilebone, lox. double for padded ... each upgrade needs to get more expensive, also
  everything should cost at least 2"): those sets' boots craft for three times their share (Padded twice), and no boots
  material costs less than two, to craft or per upgrade level; the leggings keep their four fifths, so a pair costs
  more than the leggings did. "More expensive each upgrade" is the game's own rule (the per-level amount times 1, 2 and
  4 for quality 2, 3 and 4), so nothing was added for it. Upgrade kits (the game's upgrader resources,
  `m_upgraderResource`, used only at an upgrader station) are not split: both pieces keep the leggings' own.
- Look (2026-10-08, the user: "chatgpt just fixed all the pants/boots ... remove the old way of the pants boots being
  split and use these new assets"): the workshop's NativeSplit_v001, the game's own leggings cut in two and nothing
  added. Each leggings mesh is divided by connected pieces (a piece whose rest height tops out at 0.60 m or below is
  boots; Lox's one fur shell is cut along its calf edges at 0.43 m); the body paint at an ankle row (49 of 128 for most
  sets, Leather 76 of 256, Troll 84 of 256). The trousers paint of Bronze (v004), Leather and Troll leather (v002) is
  carried down to the ankle with texels taken from the same paint near the knee, and lies under the boots' paint. The
  Caller's (v002, 2026-10-09, user: "just make that pattern go down to the ankle") repeats its own criss-cross from row
  83 to the ankle, row 97 (each texel from the same column one 15-row repeat higher; the workshop's
  `NativeSplit_v001/Tools/extend_trousers_down.py`); the body's ankle edge loop is on row 98 and the foot below it
  stays bare (a first cut to row 127 painted the feet). Its trousers icon was rendered again in game from the body's legs
  wearing the new paint (approved: "perfect ... create icon and put in EE"). Ask's (v002, the same day, user: "just
  extend pants down to ankles") carries its grey-teal cloth the same way, by the same rule and rows; its icon is the
  workshop's v001 render with the brown shins recoloured from the cloth above (approved: "perfect ... create icon and
  put in EE"). Embla's (v002, user: "this one is easy, extend pants down to ankle") carries its mail from row 81 (where
  it blends into the brown) by its own 11-row repeat (rows 70-80). Its mail is metal: with only the colour carried, the
  new rows kept the brown leather's metal and normal maps and showed flat white ("the bottom is like pure white"), so
  every set's paint edits are now applied to its leggings' `_LegsMetal` and `_LegsBumpMap` as well (`PaintMaps`, every
  channel, at the map's size) and laid on the body beside the paint; the game's own maps come back when they no longer
  fit. Texture names `EE_LegsMap_<set><property>`. Its icon was rendered again in game from the legs wearing it: the
  workshop's v001 icon, the colour alone under flat light, showed the mail pale blue-white. Eitr-weave's (v002, user:
  "yeah extend these to ankle") carries its laced cloth from row 89 by two repeats of the lacing (rows 79-88); its icon is the workshop's v001 with
  the brown shins recoloured from the cloth above, as Ask's (a render in game lost the belt and sheath, not readable).
  Lox's (v002, user: "work on the lox armor legs needs to go down to ankle"; approved: "looks good. make an icon, and put
  in EE") is a mesh, not paint: its one fur shell is cut at 0.43 m, so the trousers ended at mid-calf over bare shins. The
  cut stays and the boots keep their whole look; the trousers also draw the boots' triangles standing wholly at or above
  0.07 m (the calf shell from its ankle ring, the fur cards, the tusks: a `legs Lox` line in `native_split.txt`, the
  workshop's `NativeSplit_v001/Tools/extend_lox_legs.py`), not the shoes. With the Lox boots on, those triangles draw
  twice in the same place: the game's whole leggings, no flicker seen in game. Other boots under them have their shafts
  hidden by the fur (a judgement call). Its icon is the trousers alone in the bind pose, rendered in game
  (`Tools/IconRest.cs`: Lox's mesh has its own scale, so it is moved into the body's space through the bind poses) and
  colour-matched to the v001 icon. Padded,
  Root and Fenris were looked at and kept as they are ("these are good"), then Rag, Leather, Troll leather, Bear and
  Vilebone ("I think the rest of the pants are good too"). The export's check now skips the rag shoes' paint, the workshop's
  own drawing, which it had been reporting as a mismatch. No
  game art ships: the mod embeds only coordinates (`native_split.txt`: the boots triangles per mesh, the row, each
  edit's target and source texel, checked against the workshop's PNGs on 2026-10-08, every texel equal; rebuilt by the
  workshop's `NativeSplit_v001/Tools/export_openkeep_split.py`, which now writes into this mod) and cuts at runtime. Most
  game leggings meshes are not readable, so `MeshCut` copies the index and vertex buffers back from the GPU once per
  mesh at load (checked in game: the face order and vertex data match the workshop's reference); the paint is read
  through the GPU the first time a client needs it. Both parts keep the game's materials, bones and bind poses: the
  leggings' skin is the game's own attach with its meshes swapped for the trousers part (players only; armour stands
  keep whole leggings), and the boots item carries the same skin cut to the boots. The body's `_LegsTex` is the
  trousers paint (or the game's, for other leggings or bare legs) with the worn boots' paint laid over it, one texture
  per mix, kept.
- Rag shoes (2026-10-08, user: "the ragged boots and icon were just made"): the game's rags have no footwear, so the
  workshop painted cloth foot-wraps in the rags' colours (Rag_v002: wraps, stitched patches, dark soles; no mesh). Being
  the workshop's own drawing, not the game's art, it ships as a PNG and is the rag shoes' boots paint (`PaintCut.OwnBoots`).
- Icons: the workshop's renders of the cut pieces (`NativeSplit_v001/Icons/v001/64`, a trousers and a boots icon per
  set, embedded PNGs). The boots wear theirs; while the switch is on the leggings wear the trousers icon (`LegsIcons`,
  their own remembered and given back when off, prefab and live copies). Without an icon the boots fall back to a cut of
  the leggings' own (`BundlePrefabs.SpriteCrop`); a dedicated server decodes none. Boots names follow the game's
  leggings names (Iron Greaves: Iron Boots; Trousers of Ask: Boots of Ask; the cloth sets wear shoes).
- Iron mail (2026-10-08). The user saw the butt glitch through the back of the iron trousers when walking and running.
  Measured in game: the workshop's iron variant and the game's own trousers attach identically (the same 248 triangles,
  weights and bind poses); the game's trousers clip the same way on the same body. Their own skin weights lag the
  body's when the thigh swings, and the iron legs paint nothing on the body, so the skin shows. Copying the body's
  weights and hiding covered body triangles reduced it but could not stop it (184 trouser vertices cannot follow the
  body), so the user asked for new iron pants that "100% not glitch skin through" in the boots' and chest's style. The
  mail is made from the wearer's own body triangles (waist to ankle, the body's 1.12 m and 0.094 m edges), pushed out
  1 cm with the same skin weights, and the body stops drawing those triangles, so there is no skin under it at all. It
  is snug by nature (mail leggings); the vanilla trousers' bagginess cannot keep that promise. The chest's mail block is
  retiled at runtime so its rings repeat down the legs, its leather makes the belt, straps (above and below the knee,
  tilted like the chest's arm wraps) and ankle cuffs, its iron disc the buckle. Male and female bodies each get their
  own, cached per body and lower end. Judgement calls the user did not make: with boots on, the mail ends inside them
  (the highest body edge loop at least 1 cm below the boots' top, from the boots' cut mesh or their paint on the body)
  so the boots' paint and meshes stay on top, while the texture is wrapped from the full length either way (it moved
  and shrank when boots went on: each leg's axis and mean radius came from the shorter part); the ankle cuff only without boots; tied to `Separate Boots` (no setting of
  its own; off, the game's whole leggings come back); players only. The user approved the look in game on the
  prototype ("I like the iron pants") and asked for it in EliteEquipment and a new icon (rendered in game in the bind
  pose, neutral light, the pants alone). Arms swinging into the thighs can still pass through, as with any armour.
- Bronze plates (2026-10-08, user: "checkout the bronze legs too, I want something similar done. they don't look very
  bronze to me. they look like normal pants"). The game's bronze trousers are paint only (tan cloth, a painted buckle).
  The mail code became one system with looks (`Fitted/`, `LegStyle`): Bronze copies the bronze chest's worn material.
  The chest's own bronze plates (from its body paint, 4 x 4 texels in offset rows, at the paint's 63 texels a metre so
  they are the chest's size) on its dark leather cover the leggings from the hips to the ankles (the user: "have bronze
  material for the bottom piece as well"; first only the thighs, leather below). A bronze trim from the chest's straight
  strip runs round each thigh above the knee (0.56-0.6 m), a bronze disc from the chest's mesh is the belt's buckle,
  and a leather ankle cuff shows without boots. Bronze discs on the knees were tried and taken off ("i kinda dont like
  the kneecap things and they strick through the boots"). My choices: plates all round (the chest has them in front
  only), the trim.
  Checked in the running game on staged copies (standing, running, from behind: no skin); the user wore it in game on 2026-10-08, and with boots the texture stays put since the fix.
- Wolf silver (2026-10-08, user: "fix the wolf pants so they are not extremely poofy, and so it looks uniform with the
  boots on or off"). The game's wolf trousers are a balloon fur shell to mid-calf with blue-grey shins painted below. A
  fur look in the trousers' own pelt came first; the user asked for silver ("they cost silver to make", then "make it
  the silver material and a little thicker"). The look, as the user approved it ("This is good"): the wolf chest's
  silver mail (its atlas is shared with the trousers; a 15 x 21 block of its mail patch, repeating every 15 x 3 texels)
  at 100 texels a metre, 1.2 cm off the skin (2 cm and 1.4 cm were each "a bit thinner"); the chest's pale grey fur
  (what shows white on its shoulders; the atlas has no white fur) behind the belt, as a ruff below it and as a cuff round
  each knee ("white fir on the straps of the pants? and under the belt"); a leather belt, a strap over each knee's fur
  and an ankle cuff without boots, from the leather strap the boots' wraps wear; the chest's silver disc as the buckle.
  Unlike Iron and Bronze it stays full length with boots on ("don't remove any of the pants under the legs",
  `LegStyle.UnderBoots`): the shell sinks to 2 mm off the skin from 1 cm above the boots' top over 5 cm, so the boots'
  wraps cover it (at full thickness the mail showed through the back of the wraps), and the ankle cuff goes. The
  ruff is mostly under the wolf chest's tunic from the front. Checked in game on the user's character with the
  wolf chest, standing, front and back, with and without boots; not yet running, a female body, a second player or a
  dedicated server, and only on hot-reloaded builds, not a fresh start. Icon rendered in game like the others.
- Boots item level (2026-10-09, user: "all boots we make need to have an item level similar to that of the set they
  belong to"). EliteCrafting's item level (1 Meadows to 8 Deep North) already came out equal from the boots' recipes (a
  fifth of the leggings' materials, all kept) for all 20 sets, checked in game; it is now set outright through
  EliteCrafting's `SetItemLevel` to the leggings' own level when the database wakes, so a rounding, a recipe change or a
  YAML override on the leggings cannot leave the boots behind. A server's YAML arriving later is not followed until the
  next database load (edge case).
- Carapace scales (2026-10-09, user: "carapace pants need to be redone like how we have been doing with the
  bronze/silver/iron legs"; approved: "good make an icon and integrate"). The game's carapace trousers are a baggy brown
  shell with shiny streaks over black shin paint. The carapace chest's 128 px mesh atlas (shared with the trousers; its
  body paint covers only the neck): its dark round scales (a 48 x 16 block at 38, 26, eight by two of their 6 x 8
  repeat in offset rows) at 110 texels a metre (the iron mail's density, both on 128 px atlases); a blue chitin cuff round
  each knee from the opaque part of the plates the boots wear (88, 96; the block above it is partly transparent, which the
  shader's alpha test would cut); the chest's mottled leather (2, 2, mirrored) under and as the belt and as the ankle
  cuff without boots; its steel strip as the buckle. Thickness and boots as the wolf silver (1.2 cm, under the boots,
  sunk inside them), the user's last word on thickness. My choices: the chitin cuff (the knee cuffs read dark; red claw
  marks or a steel trim were offered). Checked on the user's character standing, front, without boots, on a
  hot-reloaded build.
- Protector (2026-10-09, user: "fix these 1700 style poofy pants, but doing so, we will need to fix the chestplate, since
  the bottom part lays on the poofy pants ... keep with the theme from the chest"; approved: "put this one in EE and make
  new icon"). Leggings: the trousers' own charcoal leather (46, 136 of the shared 256 px atlas, at their 82 texels a
  metre), a band of the breastplate's steel with its orange flame motifs above each knee edged in its ember orange, a
  brown leather belt with a steel buckle, its pale pelt as the ankle cuff without boots; 1.2 cm, under the boots.
  The breastplate (`ChestShape`, `ChestPull`): its plates and tabards were shaped round the balloon trousers. Moving
  each vertex by its own distance crimped them and dragged the tabards between the legs ("kinda crimped", "scrunched");
  the shape was then worked out offline against the body (`ValheimAssets/.../ee_fitted_legs/Tools/chest_study.py` on
  meshes dumped with `MeshDump.cs`): each direction round the body gets one lean rate (the lean all but a tenth of its
  plate vertices take before passing the leggings and a gap; smoothed round the body), so plates lean in whole. The gap
  is 2.5 cm in front, 4 at the sides and 11 behind ("shouldn't be suctioned so bad to the leg, especially on the back").
  Plate fringes weighted to the hands (why they stuck out sideways) take the nearest plate's weights; plates follow
  the thigh under them more the lower they hang, up to 0.75 in front and 0.2 behind ("legs glitching through", then
  "dangle a bit more"). The front tabard hangs straight from the belt clear of body and plates, shortened to 0.62 m, and
  moves as cloth ("kinda like natural fabric"): its own mesh with MagicaCloth (the game's cape cloth), settings from the
  linen cape, wind 0.05 (0.15 blew it aside), fixed at the belt by a paint map on a second UV channel. Valheim's
  `SetupCloth` cannot set up a cloth made at runtime (its bone remap needs pre-built data), so the build is done
  directly; the game's cape colliders are 22 cm round and placed alike on both mirrored thigh bones (the left reached the
  middle and pushed the tabard right), so the tabard has its own collider round each thigh from the bind pose (9 cm). The
  back tabard was removed at the user's word ("remove the back blue dangling cloth"), and with it its seat collider.
  Drawn in only while fitted leggings are worn; any other leggings keep the breastplate's own mesh. Checked on the
  user's character on hot-reloaded builds, standing and moving (the user's reports); not on a female body, a second
  player or a dedicated server.
- Vanguard (2026-10-09, user: "try to have the pants taper down to the ankles, extend that gold looking pattern down";
  approved: "make icon for the trousers of the vanguard and put in EE"). The game's trousers are a teal balloon with a
  gold vine forming a diamond on each leg's front. The trousers' own panel (the shared 256 px atlas, 102-191, 2-51) at
  their 91 texels a metre: a strip half a leg's round wide (136-160, 25 texels; the male body's mean leg round is 49.7 at
  this density) centred on the vine's stem, mirrored both ways, so the stem runs down the front and back of each leg and
  the vine's arms meet their mirrors in diamonds to the ankles. First laid from the waist, the vine fell under the belt
  and the chest's skirt ("where is the gold that was on the top of the pants?"); it now starts 12 cm below the waist,
  plain teal from the same panel above. The chest's gold braid (flat at 140-196, 236-240) on brown leather as the belt
  and ankle cuff, its gold brooch as the buckle; 1.2 cm, under the boots. Checked on the user's character after a
  fresh start (the first one with every look), from the side.
- Flametal mail (2026-10-09, user: "fix these flametal pants"; approved: "good make icon and put in EE"). The game's
  flametal trousers are a baggy grey shell with shiny streaks. The flametal chest's 128 px mesh atlas (shared with the
  trousers): its iridescent mail (a 24 x 35 block at 104, 0, six by five of its 4 x 7 repeat) at 110 texels a metre; a
  cuff of its heat-tinted plate (2, 74) round each knee; its mauve leather (72, 72, mirrored) under and as the belt and
  as the ankle cuff without boots; its steel (40, 62) as the buckle; 1.2 cm, under the boots, as the wolf silver. Two
  things the earlier looks never met, both now handled for every look: the mail's holes are cut out in its alpha, so the
  first build showed the ground through the legs ("i can see through the holes in these pants") and the atlas albedo is
  now always opaque (`FittedAtlas.Opaque`); and the chest has a glow map (`_EmissionMap`, colour 4, laid out for its own
  mesh: only a dot in a corner glows), which `FittedMaterial` clears rather than let the atlas sample it. Checked on the
  user's character standing, from behind, without boots, on a hot-reloaded build. Its icon was redone (user: "it looks
  blue red, but the actual legs are not blue red"): the material is fully metallic, so worn the mail is near-black with
  white glints, while the icon light showed its albedo's blue and red. The new render (metallic 1) has its mail, found
  by a render with the atlas's mail rows as a white mask, graded near-black with glints (`ee_fitted_legs/Tools/
  IconMask.cs`, `flametal_icon_grade.py`).
- PackPanel gives the boots a Feet slot under Legs while `Separate Boots` is on (its `EliteEquipmentLink`: the GUID in
  the chainloader, this entry by section and key, the boots by the `EE_Boots_` prefix). Without PackPanel a pair is worn
  from the grid (right click) and shows the grid's equipped mark.

## Test checklist (LocalTesting profile)

Never run in game as EliteEquipment (the boots ran in game as OpenKeep's section 16). With PackPanel for the Feet slot:

1. Log shows `Loading [EliteEquipment 0.1.0]` without failed patches; `milkyteam.eliteequipment.cfg` has `1. Boots /
   Separate Boots = true`. With OpenKeep too, neither log mentions the other's boots.
2. At a workbench Leather Boots sit beside Leather Trousers; Iron Greaves cost 16 iron and Iron Boots 4 at the forge (the
   game: 20); upgrades split the same. Iron Greaves show 80% of their armour, the boots 20%.
3. Weight: Iron Greaves weigh 80% of the game's in the tooltip and the inventory total; the boots a fifth. With
   OpenKeep's `Weight Multiplier = 0.5`: both halve again. Switch off: the greaves' full weight is back in the total at
   once.
4. Craft Iron Boots and wear them (right click): they go into PackPanel's Feet slot, the armour total rises by their
   part, the body shows iron boots, the greaves end at the ankle. A second player sees the same; relog: still worn.
5. Male and female characters each: the right trousers and boots, no gaps at the ankle walking, running, crouching.
6. Icons: the boots show the workshop's boot icon, the leggings the trousers icon while `Separate Boots` is on and their
   own when off (inventory, crafting list, tooltip).
7. Wear Root helmet, harnesk and leggings without boots: no set bonus; add Root boots: the set bonus comes back.
8. Get hit until the boots break: they come off with the game's broken message and stay in the Feet slot.
9. Die: the boots go to the grave with the rest; the ragdoll shows what was worn.
10. Drop boots: they lie on the ground as a pair of boots. Without PackPanel: right click wears them, the grid marks them.
11. With EliteCrafting: an inscription on worn boots counts (its log has no refusal line).
12. Iron Greaves on a male and a female character: mail from the waist to the ankle with a belt, buckle, knee straps and
    ankle cuffs; walk, run, crouch and look at the back: no skin anywhere under the mail. Put on Iron Boots: the mail ends
    inside them, no skin between. Swap to other leggings and back: the body is whole again without them. The icon is the
    mail's. A second player sees the same. Armour stands keep the game's iron trousers. Bronze Plate Leggings the
    same: bronze plates from the hips to the ankles, a bronze trim above the knees, the plates icon; Bronze Plate Boots on: the leggings end inside them.
    Wolf Hide Trousers: silver mail with fur under the belt and at the knees, the silver mail icon; Wolf Hide Boots on:
    the mail stays full length under them and nothing pokes through the wraps walking and running, front and back.
    Carapace Greaves the same: scales with chitin knee cuffs, the scales icon; Carapace Boots over them, nothing poking
    through. Flametal Greaves the same: heat-tinted mail, solid (nothing seen through its rings), no glow, the flametal
    icon; Flametal Boots over them. Trousers of the Protector: dark leather with steel flame bands; with the Protector
    breastplate the plates sit close (loose behind) and follow the legs walking and running, the front tabard swings and is
    pushed by the thighs, no back tabard; other trousers with the breastplate: its own look; take the trousers off: its own
    look back, no tabard left behind. Trousers of the Vanguard: teal with the gold vine's diamonds from the thighs to the
    ankles on both legs alike, the braid belt and brooch. Lox Fur Trousers without boots: the fur to the ankles, bare
    feet; Lox Fur Boots on: the game's whole leggings, no flicker; the Lox fur icon.
13. With EliteCrafting: every pair of boots shows its leggings' item level in the tooltip (Iron Boots 3, Boots of the
    Protector 8).
14. `Separate Boots = false` while worn: the boots come off, the greaves show the game's look, armour, weight and recipe
    again, the boots recipes leave the workbench, PackPanel's Feet slot goes. Dedicated server with the switch on:
    every client the same; no errors.

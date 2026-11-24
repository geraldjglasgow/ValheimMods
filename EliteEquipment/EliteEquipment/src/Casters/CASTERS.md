# Casters (EliteEquipment section 4)

The Meadows tier of the healer and caster line designed in `EliteEquipment/features/casters.md` (the design, the
decisions and the build contract with every name). Asked for 2026-10-09: "everything will be custom made, I want to
start with medows first completely". Built as an AssetLab bench (section 3 there) and moved into EliteEquipment on
2026-10-12 as section 4.

## What is in it

| Item | Prefab | Copy of | Model (bundle) |
| --- | --- | --- | --- |
| Gnarled Wand | `EE_Wand_Meadows` | `Club` | `ee_wand_meadows` |
| Greywood Wand (Black Forest; heal power 25, +15 eitr, a thrown stone blunt 7 + spirit 7 instead of a bolt (see "The Greywood Wand's rock"); forge beside the bronze mace: Fine wood 4, Bronze 2, Greydwarf eye 6; model by ChatGPT 2026-10-11) | `EE_Wand_BlackForest` | `Club` | `ee_wand_blackforest` |
| Rootbound Wand (Swamp; heal power 35, +20 eitr, roots blunt 11 + spirit 11 instead of a bolt (40% of the iron sword's 55; see "The Rootbound Wand's roots"); forge beside the iron mace: Ancient bark 6, Iron 3, Root 2; 2026-11-02) | `EE_Wand_Swamp` | `Club` | `ee_wand_swamp` |
| Frostsilver Wand (Mountains; heal power 45, +25 eitr, ice shards instead of a bolt, each frost 15 + spirit 15 (40% of the silver sword's 75; frost slows by the game's rule; see "The Frostsilver Wand's ice shards"); forge beside the silver mace: Fine wood 4, Silver 4, Freeze gland 3; modelled from scratch 2026-11-02) | `EE_Wand_Mountains` | `Club` | `ee_wand_mountains` |
| Blackmetal Wand (Plains; heal power 60, +30 eitr, bolt pierce 19 + spirit 19 (40% of the black metal sword's 95); forge beside the Porcupine: Black metal 4, Fine wood 4, Needle 5; modelled from scratch 2026-11-02) | `EE_Wand_Plains` | `Club` | `ee_wand_plains` |
| Yggdrasil Wand (Mistlands; heal power 75, +30 eitr and +25% eitr regen while wielded (GearPiece.EitrRegen), bolt pierce 22 + spirit 22; galdr table beside the Staff of Embers: Yggdrasil wood 6, Refined eitr 4, Royal jelly 3; modelled from scratch 2026-11-02) | `EE_Wand_Mistlands` | `Club` | `ee_wand_mistlands` |
| Ashwood Wand (Ashlands; heal power 90, +30 eitr and +40% eitr regen while wielded, a firebolt fire 27 + spirit 27 instead of a bolt (40% of the Nidhogg sword's 135; fire burns by the game's rule; see "The Ashwood Wand's firebolt"); galdr table beside the Staff of Embers: Blackwood 6, Flametal 3, Celestial feather 2; 2026-11-02, its model being built: the club's look until its bundle is in) | `EE_Wand_Ashlands` | `Club` | `ee_wand_ashlands` |
| Leatherbound Spellbook (Meadows; 2 pages (4 until 2026-11-02), block 2, eitr regen +10%, wand heal power +3; workbench 1 beside the wood shield: Deer hide 3, Leather scraps 4, Resin 2, Feathers 2; the Raven Fetish's stats, weight 1; replaced the fetish `EE_Focus_Meadows` on 2026-11-02) | `EE_Book_Meadows` | `ShieldWood` | `ee_book_meadows` (prefab `ee_book_meadows`: `model`, `grip` at the middle of the back; open `ee_book_meadows_open`; icon `ee_book_meadows_icon`) |
| Barkbound Grimoire (Swamp; 4 pages, block 4, eitr regen +15%, wand heal power +6; forge beside the banded shield (level 2): Ancient bark 4, Root 3, Iron 2; 2026-11-02, its model being built: the wood shield's look until its bundle is in) | `EE_Book_Swamp` | `ShieldWood` | `ee_book_swamp` (`ee_book_swamp`, `ee_book_swamp_open`, `ee_book_swamp_icon`: the Meadows pair's contracts) |
| Cinderbound Tome (Ashlands; 8 pages, block 8, eitr regen +25%, wand heal power +12; galdr table beside Dundr `StaffLightning` (level 2): Asksvin hide 4, Flametal 3, Charred bone 4; 2026-11-02, its model being built) | `EE_Book_Ashlands` | `ShieldWood` | `ee_book_ashlands` (`ee_book_ashlands`, `ee_book_ashlands_open`, `ee_book_ashlands_icon`) |
| Deerhide Hood, Robe, Trousers | `EE_Seer_Meadows_Hood`, `_Robe`, `_Trousers` | `HelmetMage`, `ArmorMageChest`, `ArmorLeatherLegs` (recipes beside the leather set's) | `ee_seer_meadows` v2, the long seer's robes (`RobeLook`, `RobeBody`) |
| Rimegild Hat, Robe, Trousers (Deep North, tier 8, 2026-11-02; armour 22 (+2 a level), +15 eitr a piece, -2% movement on robe and trousers; set bonus frost resistant and Blood Magic +25 (`EE_SE_SeerDeepNorthSet`); beside the game's Deep North mage set `HelmetDNMage`, `ArmorDeepNorthMageChest`, `ArmorDeepNorthMagelegs` at its station and level: hat Norn thread 6 (+3), Seal hide 4 (+2), Moose sinew 2 (+1); robe 12 (+6), 6 (+3), 3 (+1); trousers 9 (+4), 5 (+2), 2 (+1); hat HiddenHat, beards shown; boots `EE_Boots_SeerDeepNorth` "Rimegild Boots") | `EE_Seer_DeepNorth_Hood`, `_Robe`, `_Trousers` | `HelmetMage`, `ArmorMageChest`, `ArmorLeatherLegs` | `ee_seer_deepnorth` (the robe's skirt as cloth like every set's) |
| Page: Mend, Page: Hearthsong, Page: Stag's Swiftness, Page: Elder's Volley, Page: Cleanse, Page: Eir's Spring, Page: Second Wind, Page: Chain Mend, Page: Revive, Page: Ward, Page: Sanctuary, Page: War Chant, Page: Feather Fall (found, never crafted; the Volley from the Elder only, Sanctuary from The Queen only, Revive from dungeon chests only) | `EE_Spell_Mend`, `EE_Spell_Hearthsong`, `EE_Spell_Swiftness`, `EE_Spell_Volley`, `EE_Spell_Cleanse`, `EE_Spell_Spring`, `EE_Spell_SecondWind`, `EE_Spell_ChainMend`, `EE_Spell_Revive`, `EE_Spell_Ward`, `EE_Spell_Sanctuary`, `EE_Spell_WarChant`, `EE_Spell_FeatherFall` | `Raspberry` | `ee_spells_meadows` (`ee_spell_page`, the old `ee_spell_scroll` while the bundle has no page; icons `ee_spell_<key>_icon`, Mend's Hearthsong's until it is built) |
| The wand's bolt | `EE_WandBolt` | `staff_iceshard_projectile` | `ee_casterfx_meadows` (`ee_fx_bolt`) |
| The Rootbound Wand's roots: the spawner (not networked) and a root | `EE_WandRoots`, `EE_WandRoot`, `EE_fx_wandroot_spawn` (the rising effect, no camera shake) | the Elder's `spawn_roots` (from `gd_king`'s `gd_king_rootspawn`), `TentaRoot`, `fx_gdking_rootspawn` (a `CamShaker`, range 50, removed in the copy) | the game's own (no bundle) |
| Elder's Volley's seed (networked) | `EE_VolleySeed` | the Elder's `gdking_root_projectile` (from `gd_king`'s `gd_king_shoot`) | the game's own (no bundle) |
| Moder's Breath's breath (networked) | `EE_SpellBreath` | Moder's `vfx_dragon_coldbreath` (the trail start effect of the `Dragon`'s `dragon_coldbreath`) | the game's own (no bundle) |
| The Frostsilver Wand's ice shard (networked) | `EE_WandIceShard` | the Staff of Frost's `staff_iceshard_projectile`, its look kept | the game's own (no bundle) |
| The Greywood Wand's rock (networked; a charged one breaks into up to 15 small copies of itself) | `EE_WandRock` | a Greydwarf's thrown stone, `Greydwarf_throw_projectile` (fired by `Greydwarf`'s `Greydwarf_throw`), its look kept | the game's own; its local trail `ee_fx_rock_trail`, burst `ee_fx_rock_break` and charge `ee_fx_charge_rock` in `ee_casterfx_meadows` |
| The Ashwood Wand's firebolt (networked; from 0.9 of a full charge it explodes where it lands) | `EE_WandFirebolt` | the Staff of Embers' `staff_fireball_projectile`, its look kept; its burst the Surtling fireball's `vfx_FireballHit` and `sfx_imp_fireball_explode`, the explosion the staff's `fx_fireball_staff_explosion` | the game's own (no bundle) |
| Dandelion Mash (eitr food: 12 / 6 / 35, 25 min, workbench: Dandelion 3, Honey 1, Mushroom 1) | `EE_Food_DandelionMash` | `CarrotSoup` (steam removed) | `ee_food_meadows` (`ee_food_dandelionmash`) |
| Candied Thistle (15 / 15 / 15, 15 min, makes 2), Glowcap Broth (15 / 8 / 40, 25 min), Seer's Porridge (15 / 8 / 45, 30 min); cauldron level 1 | `EE_Food_CandiedThistle`, `_GlowcapBroth`, `_SeersPorridge` | `BoarJerky`, `CarrotSoup`, `TurnipStew` | `ee_food_blackforest` (made 2026-10-11 by a Sonnet agent) |
| Turnip Crisps, Blood Draught, Mire Stew (Swamp, cauldron beside Turnip Stew); Glacier Tea, Wolfheart Stew (Mountains, beside Onion Soup); Candied Cloudberries, Lox Marrow Broth, Sunbarley Porridge (Plains, beside Blood Pudding): the features table's numbers | `EE_Food_<Name>` | snacks `BoarJerky`, drinks and broths `CarrotSoup`, stews and porridge `TurnipStew` | `ee_food_swamp`, `ee_food_mountains`, `ee_food_plains` (models by ChatGPT, bundled 2026-10-11) |

Status effects `EE_SE_Hearthsong`, `EE_SE_Swiftness`, `EE_SE_SeerMeadowsSet`. Words `$ee_wand_*`, `$ee_book_*`,
`$ee_seer_*`, `$ee_spell*`, `$ee_page_*`, `$ee_food_*`, all in `translations/English.yml` (the mod's CLAUDE.md, "Words").
Item custom data `EliteEquipment.pages` on each spellbook (its pages; a book without it is new and empty) and
`EliteEquipment.lastSpell` on each wand (the spell last picked on the spell ring; a wand from before keeps the first of
its two `EliteEquipment.loadout` slots, the old key removed at its next pick); player custom data `EliteEquipment.spells` (spells learned before the
spellbook) is turned into pages once and removed. Routed RPC `EliteEquipment_CasterFx` (effects to
the players near). Setting `4. Casters` / `Wands and Spells`, **off by default** (user, 2026-10-09): to test, set
`Wands and Spells = true` under `[4. Casters]` in `milkyteam.eliteequipment.cfg` (it hot reloads; on in LocalTesting).
Off: no recipes, no pages drop, pages go in no book, wands neither cast nor fire the bolt, no gear eitr, no spell
ring, spell bar, Spellbook, highlight or tooltip lines; items already made stay.

## Code map

- `CastersModule` (Initialize, Install), `CastersSettings`, `CasterSafe` (log once, never throw).
- Data: `Spell`, `Spells` (Mend, Hearthsong, Stag's Swiftness, Elder's Volley, Cleanse, Ward, Sanctuary, War Chant, Feather Fall, Eir's Spring, Second Wind, Chain Mend, Revive and the rest; each with its tier and the boss that drops it),
  `CasterGear` (items, numbers, recipes; the books with their page counts).
- Food: `CasterFood` (data), `FoodItems` (the dish copy).
- Prefabs: `CasterItems` (ZNetScene.Awake / ObjectDB.Awake / CopyOtherDB prefixes; scene postfix: bolt look, boss
  pages), `CasterBundles` (a missing bundle or asset a warning, the fallbacks `PrefabOr`, `SpriteOr`), `HeldModel` (the
  wand in the fist: model length +Y turned 90 degrees about X onto the attach's +Z; the book on the hand as a buckler;
  grip on the attach point), `WandItem` (both attacks on the Staff of Embers' `staff_fireball` cast, the
  one cast motion that plays on a one-handed weapon), `BookItem` + `BookOpen` (the hidden open book beside the closed
  one), `RobeItems` + `RobeLook`, `RobeCloth` + `RobeClothFixed` + `RobeClothColliders` + `RobeClothBuild` +
  `RobeClothPose` (the skirts as cloth, see "The robes' skirts as cloth"), `SpellItems` (the pages),
  `WandBolt`, `WandRoots` (the Rootbound Wand's spawner and root), `IceShards` (the Frostsilver Wand's shard), `WandRock` (the Greywood Wand's rock), `Firebolt` (the Ashwood Wand's firebolt), `VolleySeeds` (Elder's Volley's seed), `BreathLook` (Moder's Breath's breath), `CasterItemLook`, `CasterStatus`, `CasterRecipes`.
- Pages (see "The spellbook and its pages"): `BookPages`, `PageUse`, `PageDrops`, `BossPages`, `CreaturePages`,
  `ChestPages`, `PageMigration`.
- Casting: `SpellCast` (StartAttack prefix fixes the spell; `Attack.DoNonAttack` postfix casts it), `SpellCost`
  (`Attack.GetAttackEitr` postfix: the bolt costs no eitr, the cast its spell's), `FriendTarget`, `HealPower`,
  `HealTarget` (the highlight and the lock), `HealFlight` (the heal homing on the friend), `VolleyCast` + `VolleyHit`
  (Elder's Volley's seeds and their hit, fired from `SpellCast`'s cast; no patch of their own), `BreathCast` + `BreathCone` + `BreathHit` (Moder's Breath's bursts, who they hit and the hit, from `SpellCast` and `SpellHits`; no patch of their own), `Cleanse` + `CleanseEffect` +
  `CleanseList` (Cleanse's cast and release, its carrier status effect, the harmful effects it takes; no patch), `Ward` + `WardEffect` + `WardBubble` (Ward's cast, its `SE_Shield` and its bubble; no patch), `Sanctuary` + `SanctuaryEffect` (the dome, its shelter and cooldown; no patch), `WarChant`, `FeatherFall` (their chants' stats; Feather Fall's feathers), `ReleaseDraws` (these four releases, from `SpellFx.Draw`), `GameBorrow` (the game's shield and feather cape values), `HealSpells` + `HealReleases` (where Eir's Spring, Chain Mend, Second Wind and Revive join `SpellCast`, `SpellFx` and `CasterStatus`), `Spring` + `SpringPool` + `GroundPoint`, `ChainMend` + `ChainMendRun`, `SecondWind`, `Revive` + `ReviveGrave` + `ReviveArrival` + `ReviveCooldown` (see "Eir's Spring, Chain Mend, Second Wind and Revive"; one patch, Revive's message), `SpellMotion` + `SpellClips`
  + `SpellHits` + `PlayerControllers` (each spell's own motion, see "Spell motions and effects"), `CasterGearWorn` (wand, book,
  robes worn), `GearEitr` (`Player.GetTotalFoodValue` postfix), `LastSpell` (the wand's last picked spell; it casts
  only while its page is in the held book).
- Effects: `CasterFx` (routed RPC, local copies; the cast flash and chant ring turned to each spell's colour, the
  workshop's hue numbers), `BoltHit` (`Projectile.OnHit` postfix on the owner), `RootCast` (`SpawnAbility.Setup` and
  `SpawnAbility.SetupAoe` prefixes: count, place and hit of the Rootbound Wand's roots) + `WandRoot` + `RootHit` (a root
  rising, lashing and striking), `IceVolley` (`Attack.FireProjectileBurst` prefix and finalizer, `Projectile.Setup`
  prefix: count and fan of the Frostsilver Wand's shards), `RockShot` (`Projectile.Setup` prefix and postfix: the
  Greywood Wand's rock thrown straight at the crosshair, sized, pushed and armed to break) + `RockBreak` (a charged rock breaking
  into its pieces 5 m out) + `RockBurst` (its own routed message: the break's burst on every machine near) + `RockTrail`
  (the trail on every rock and piece, on every machine) + `ChargeStone` (the stone growing at the tip while charging), `FireboltShot` (`Projectile.Setup` postfix: the Ashwood Wand's firebolt sized by the charge, a full
  charge armed to explode), `SpellFx` (the spells' own charges and releases), `BoltTint` (the bolt charge in the wand's stone),
  `StreamStart` (the charge stream leaving the open book), `NearbyCast`, `CasterSounds` (game clips).
- Interface: `CasterHud` (Hud.Update postfix), `SpellRing` + `SpellRingItems` (see "The spell ring"),
  `CasterControls` (`Player.SetControls`: the mouse buttons below),
  `CasterBlink`, `WindUp` + `ChargeState` + `ChargeOrb` + `WandAnimations` + `ChargeFeet` + `TipSpawn` + `BoltSize`, `SpellBar` (one slot under the hotbar, labelled RMB: the last picked spell), `Spellbook` + `SpellbookUi` + `SpellbookLayout` + `SpellbookCover` + `SpellbookPages` + `SpellbookDetail` + `CasterUiCopies` (see "The Spellbook window"), `CasterTooltip`.

Patches checked offline against the game's DLLs (15 classes, 0 problems, 2026-10-09). All five bundles embedded
(2026-10-09). The robes follow the workshop BRIEF's runtime steps: a robe mesh per body (`RobeBody`, `AttachArmor`
postfix, which also starts the skirt's cloth: `RobeCloth`, see "The robes' skirts as cloth"), body paint on copies of the leather armour materials, the head piece hiding hair by its set's rule
(`RobeSet.Hair`: the Deerhide hood HiddenHood, the hats and the Mossweave branch headdress HiddenHat; beards shown). The
Mossweave head piece was a hood like the Deerhide's until 2026-11-01 (user: "needs to be completely different"); its
prefab and bundle names stay `EE_Seer_BlackForest_hood` and `ee_seer_blackforest_hood`.

## The spellbook and its pages (user, 2026-11-02)

The design is features/casters.md, "The spellbook". The Leatherbound Spellbook replaced the Raven Fetish (prefab, item,
recipe and words gone; the fetish's bundle files `assets/bundles/ee_focus_meadows.*` are no longer loaded). A wand casts
a spell only while that spell's page is in the book in the left hand (`LastSpell.In` through
`BookPages.Castable`); without a book the cast is refused with a message and only the bolt fires; the heal highlight
needs a Mend page in the held book. Pages are item custom data on the book (`BookPages`), so they leave and arrive with the book.

**The books** (user, 2026-11-02: "first book holds 2 spells, then 4, then 8"): `CasterGear.Books`, the Leatherbound
Spellbook (Meadows, 2 pages), the Barkbound Grimoire (Swamp, 4) and the Cinderbound Tome (Ashlands, 8), all copies of
the wood shield. Each `GearPiece` carries its own numbers: `Pages`, `Block` (block power at quality 1, +1 a level, set by
`BookItem`), `EitrRegen` (the item's `m_eitrRegenModifier`, summed by the game's
`Player.GetEquipmentEitrRegenModifier`, which counts `m_leftItem`) and `HealPower` (what `HealPower.Of` adds to the
wand's while the book is held, `CasterGearWorn.HeldBook`; the tooltip's "+N"). Parry bonus 1.5x, weight 1 and durability
100 (+50 a level) are the same for all three. `BookPose` finds every one of them by `CasterGear.Books`.

**Room** (`BookPages.Room`, the book's `Pages`): a page goes in only while the book holds fewer than its room. A book
already holding more (a Meadows book filled to 3 or 4 before its room became 2) keeps every page, and every one of them
still casts; it takes no new page until pages taken out bring it under its room. Nothing removes a page on its own. The
window says so honestly ("4 / 2 pages"), the tooltip too ("Pages (4/2)"), and a refused page says "full (4 / 2 pages)".

| Code | Game method | What it does |
| --- | --- | --- |
| `PageUse` | `Player.ConsumeItem` prefix | a page item used while holding a book goes in it (refused: no book, full, already in); never eaten; `TakeOut` for the Spellbook's button |
| `CreaturePages` | `CharacterDrop.GenerateDropList` postfix | on the creature's owner, once per death (dropped at once or kept in the ragdoll): a kill by a player (the last hit's attacker), 5% with 2+ stars (the game's level, or ECR's `ecr_stars` ZDO int when higher), 0.5% otherwise; no boss, tame or player |
| `ChestPages` | `Container.AddDefaultItems` postfix | on the chest's owner when its loot is first made: a chest with default loot above y 3000 (a dungeon), 10%, if it has room |
| `BossPages` | (prefab, from the `ZNetScene.Awake` postfix and the switch) | each `Spell.Boss` gets its page as a one-per-player drop (Eikthyr: Swiftness, the Elder `gd_king`: Elder's Volley, Moder `Dragon`: Moder's Breath) |
| `PageMigration` | (HUD tick, local player once) | player custom data `EliteEquipment.spells` becomes one page each (Mend never was one) in the inventory or at the feet, then is removed |

`PageDrops` picks the page: loot pages (`Spell.Loot`: no boss page; Mend is a loot page since 2026-11-02) of the source's tier and below, its own
tier's three times as likely; the tier from `WorldGenerator.GetBiome` at the source's x and z (Meadows 1, Black Forest
2, Swamp 3, Mountains and Ocean 4, Plains 5, Mistlands 6, Ashlands and Deep North 7). Judgement calls beyond the spec:
the elite chance also needs a player's kill; the sea counts as tier 4; a full chest gets no page; the old record's
pages go to the feet when the inventory is full, with one message; a page taken out leaves a wand's last spell naming it
(greyed on the spell bar, a tap refused); the book's tooltip lists its pages.

### Held open (user, 2026-11-02)

While `Wands and Spells` is on and a spellbook is in a player's left hand (the game's own shared left item,
`VisEquipment.m_currentLeftItemHash`), every machine that draws (not a dedicated server: no graphics device) poses that
player's left arm after the animator, within 60 m of the camera. Off, the book is closed in the shield hold as before.

| Code | Game method | What it does |
| --- | --- | --- |
| `BookPose` | `Player.LateUpdate` postfix (private), `Player.OnDestroy` postfix | per player: a book in hand starts a `BookHold`; no book costs one int compare; the hold goes once the arm has blended back |
| `BookHold` | | the weight, 0 to 1 over 0.2 s, eased (smoothstep); the left hand's copy shown open while above 0 |
| `BookHand` | | the arm by LimbIk's `TwoBoneIk` and the hand's turn; the hand's frame measured once from its bones |
| `BookFingers` | | the 15 finger segments turned onto open ways in the hand's frame |
| `BookYield` | | when the game's left arm wins |
| `BookOpen` | | `model_open` (each book's own `<asset>_open` from its own bundle, `BookOpen.OpenAsset`) or `model_flat` (its own closed one laid flat, a warning) under the item's attach, hidden; shown on the hand's copy only. A book whose bundle is not in has neither: `BookHold` keeps the shield hold for it (`BookOpen.Opens`, asked once per copy) |

- **The hand's frame** (measured in game 2026-11-02 on the live player; the rig's hand bone: fingers +Y, palm +Z, thumb
  side +X, lossy scale 95 on a 0.95 attach): the fingers' way is the four knuckles' (`LeftHandIndex1`, `Middle1`,
  `Ring1`, `Pinky1`) middle from the wrist; the palm's normal is index-minus-little-finger crossed with it; the palm's
  middle is 0.6 of the way from the wrist to the knuckles and 2 cm out on the palm's side.
- **The arm**: the palm put 0.10 m across the body, 0.33 m ahead and 0.25 m below the shoulder (`LeftArm`), scaled by
  the player's arm length against the game's 0.572 m (upper arm 0.313, forearm 0.259), so the elbow bends about 90
  degrees and the hand sits front-left at about waist-to-chest height (about 1.23 m on the game's player); the elbow
  turned toward a point 0.35 m below, 0.10 m behind and 0.12 m outside the shoulder.
- **The hand**: the fingers forward, turned 20 degrees across the body and tipped 20 degrees up; the palm up, rolled a
  quarter toward the body's right (toward the face), so the pages face the reader.
- **The fingers**: each segment from the knuckle out turned onto its way (`Quaternion.FromToRotation` from its joint
  to the next one or the tip bone, by the weight): the four fingers along the hand keeping 0.4 of their knuckles' fan,
  curled 4, 8 and 12 degrees toward the palm (gently cupped); the thumb out, 0.6 along and 0.8 across, curled 0, 4 and 8.
- **The book**: the open prefab's `palm` point (the grip of the closed fallback) on the palm's middle; the open book's
  spine (Z) along the fingers and its pages (+Y) along the palm's normal; the closed fallback on its back cover, its top
  toward the fingers. Placed once per opening in world space: the copy hangs from the hand, so it stays.
- **It yields** (the weight goes to 0, the book closes in the shield hold): dead, in bed, attached (`IsAttached`, and
  every `attach*` animator bool for other players: chairs, thrones, ship controls, saddles), an emote, swimming
  (`IsSwimming`, the synced `inWater` bool), building (`InPlaceMode`), blocking (`IsBlocking`, the ZDO for others), and
  the layer-0 state tags `sitting`, `emote`, `dodge` (the roll), `cutscene` (current or next). It holds through idle,
  walking, running, jumping, crouching and every wand attack, charge and cast: the right arm keeps the wand's animations,
  the left arm the book. A ragdoll is another object and never posed; the blink is not a yield.
- Judgement calls: blocking yields (the block is the game's left-arm use); beyond 60 m the arm is left to the game; the
  open book shows from the first frame of the blend and closes on its last.

## The Spellbook window (user, 2026-11-02: "the spellbook ui looks so bad")

Until then the window was drawn at fixed places over the crafting panel, which showed through. Now it takes the crafting
panel's place: it gets the panel's rect each frame it is open (`SpellbookLayout.Fit`; measured 2026-11-02 with PackPanel
and OpenKeep: anchored top right at (-40, -200), 670 x 740 units; the game's parts 10 in, its title centred 33 down,
the recipe list 236 wide with rows 227 x 32 every 30, hotbar slots 64), and every part is anchored to its edges or to a
share of its width, so it fits any resolution, UI scale or panel size. Copies of the game's parts only (`CasterUiCopies`).

- Hidden while open (`SpellbookCover`): the crafting panel, through a CanvasGroup (its own, or one added and removed
  again) at alpha 0 that takes no clicks; none of its parts is switched off, so the game and other mods (OpenKeep's
  search and categories) run it as usual. Put back exactly when the window closes: its button again, the inventory
  closed (Esc included), no book in hand, the switch off. It opens with a book in hand; a wand is not needed.
- Header (`SpellbookUi`): copies of the panel's darkening, background (PackPanel skins the copy) and title ("Spellbook"
  in the title's place), the held book's name (item name font) and "1 / 2 pages" under it (the held book's room; "4 / 2" for one over it), the crafting tabs' separator.
- Left, 40% of the width (`SpellbookPages`): the pages as recipe row copies (icon, name shrunk to fit, eitr cost at the
  right), the picked one lit by the row's own selected frame; "The book has no pages" in the column instead.
- Right (`SpellbookDetail`): the picked page's icon (64), name, kind and cost, description (wrapped, shrunk down to 65%
  when long), and Take out (a craft button copy) along the bottom.
- Both columns reach down to the window's bottom pad. Until 2026-11-02 a bottom strip "Prepared on the wand" held the
  wand's two slots; the spells are chosen on the spell ring now (below).
- Clicks (`Spellbook`): a page is picked and shown on the right. Refreshed only when the book, its pages, a pick or
  the language change; rows made again only when the pages change.
- Judgement calls: no scrolling (a book holds at most 8 pages; the column has room for about 15 rows); opened only by its button
  and closed with the inventory, as before; no gamepad navigation in the window.

## Controls (user, 2026-10-11)

With a wand drawn: left mouse casts the bolt on the press, as a sword swings (user, 2026-10-11: "the player must click to
cast, like the same with swords"; 4 eitr, up to a third less from Elemental Magic; `WindUp`): the next move of a three-move
combo (`WandMoves`): 1 the wand up with a flick, 2 by the left of the head then pointed, 3 back by the right of the head
then forward. The combo is the game's own sword combo (user: "whatever system they use"): the bolt is a three-level
chained attack, so the game picks the level as for a sword (the next one for a click during a move once its `Chain`
point, added to the clip at load just after the bolt, has passed, or within 0.2 s of its end; the first again after that),
and the level is the move. The game's numbered triggers for it are held back (`WandAnimations`). Still held after 0.2 s, before that move's bolt flies, the cast
turns into a charge: the wand goes behind the caster (move 1's furthest-back pose: the game's charged-staff hold, the bool
`staff_charging`), the caster walking at the cast's own slow speed (`ChargeStride`) and turning to aim (`ChargeFeet`); let
go, move 1 plays from the back and throws the charged bolt (as strong as the button was held, full at 3 s; free, the
replaced cast paid), and the combo goes on with move 2. The bolt flies in a straight line from the wand's tip to what the
crosshair is on (`TipSpawn`: the camera ray; the game's own aim for a player is the look direction, parallel to the
crosshair from the tip; the staff's 5 degree lift and spread are gone, no gravity). The animations are the workshop's `ee_casteranim`, swapped in for the charged staff's
three clips on every player (`WandAnimations`): a 0.22 s raise, a 1.6 s breathing hold, and one 5.2 s clip whose segments
are the four casts (moves at 0, 0.97, 1.94 s, the charged release at 3.05 s; Hits at 0.42, 1.39, 2.34, 3.27 s). The
caster's machine tells every player near which segment (a routed RPC) and each machine cross-fades the upper body into it
and out at its end; the game's trigger for that state is held back, since it would start the clip from move 1. The
raise is cross-faded into the hold at its end, and a hold let go without a cast back to `None` (the game's controller
has neither). Known: casting while walking turns the arm about 40 degrees left of the aim (the clips sit on the walking
hips); the bolt itself flies true. Until 2026-10-11 a press cast at once and the hold froze the body, then a creature's
1.4 s charged-staff raise played; both rejected by the user. A little white spell shows at the wand's `tip` from
0.3 s into the hold, grows to 2.2 times its size by full charge (3 s, the user's reference) and then gets a larger glow and a flash (`ChargeState` in the caster's
ZDO on the shared world clock, `ChargeOrb` drawing it on every machine; workshop effects `ee_fx_charge`,
`ee_fx_charge_full`, `ee_fx_charge_ready`). The bolt takes the charge at the trigger: up to +30% damage, and 0.8 to 2
times its size with its hit width (`BoltSize`; the size shared by the game's initial scale sync).

### The Rootbound Wand's roots (user, 2026-11-02)

No camera shake (user, 2026-11-02: "swamp wand shouldn't shake players screen"): the spawner's one `m_spawnEffects` entry, the
Elder's `fx_gdking_rootspawn` (networked, a `CamShaker` of range 50 on its root; its children `wetsplsh` and `SFX` have none; the
root's death effect `fx_tentaroot_death` has none), is replaced in `EE_WandRoots` by `EE_fx_wandroot_spawn`, a copy without the
`CamShaker`, registered in the scene with the root (`CasterItems.Made`). The game's Elder and root are untouched.

"The root wand attack should be the elder like root attack, but a tap should only have 1 root come out. full charge
should have like 5 come out." The Rootbound Wand's left mouse is the same click, combo, charge and eitr as the bolt (4
eitr a cast however many roots; the charged release paid by the cast it replaced), but its attack fires a copy of the
Elder's root attack instead of the bolt: `gd_king`'s `gd_king_rootspawn` fires `spawn_roots`, a `SpawnAbility` (not
networked) that raises 15 `TentaRoot` creatures within 15 m of its target. Ours (`EE_WandRoots`) raises 1 + 4 x the
charge roots, rounded (1 tapped, 5 at full charge), where the crosshair is (`TipSpawn.AimPoint`, kept 2 to 30 m out):
one on the spot, more at random within 3 m, on the ground under the point, 0.1 s apart, the game's `fx_gdking_rootspawn`
at each. A root (`EE_WandRoot`, not saved) is the game's root with the creature taken out (no AI, health, collider or
body): it comes up (the root's wake-up clip), faces away from the caster, lashes 0.4 s after it appeared (the `attack`
trigger, set on each machine's own animator), all clips 1.5 times the Elder's speed, and goes after 3.5 s with the game's
root fade. On the caster's machine (the owner) the strike lands at the clip's `Hit`, or at 2.6 s at the latest: the game's
`Aoe`, 2 m round, 1 m ahead of the root, set up from the attack's hit as the game sets one up (the wand's damage with
its quality, the charge's +30% and the skill; the skill not applied twice), the caster's enemies only (not the caster,
other players, tames or buildings), the damage the caster's; a root that hit someone raises Elemental Magic once. Damage
is blunt 11 + spirit 11 (+1 each a quality): the pierce half made blunt, as the Elder's roots hit blunt (55 blunt, 20
chop and pickaxe). Not yet seen in game.

### The Frostsilver Wand's ice shards (user, 2026-11-02)

A fan of sharp ice shards. The Frostsilver Wand's left mouse is the same click, combo, charge and eitr as the bolt (4
eitr a cast however many shards, up to a third less with Elemental Magic; the charged release paid by the cast it
replaced), but its attack fires a copy of the Staff of Frost's shard, `staff_iceshard_projectile` (the bolt's base),
with what the bolt strips kept: its look (a sphere stretched to 0.1 x 0.1 x 1 m along its flight, a pointed sliver, in
cold flames, sparks and a light) and its hit effect (`fx_iceshard_hit`, networked). Read live, the game's shard has no
damage, area, status effect or spawn on hit of its own; ours (`EE_WandIceShard`) is held to that, so only the attack's
hit lands. It flies straight for 3 s at the wand's 20 m/s (no gravity; the game's has 1 and 6 s), hits 0.1 m wide
(the game's own, the bolt's base), gives 1 adrenaline a hit, and keeps its own size (`BoltSize` leaves it alone: the charge buys shards, not size). As the
game fires the attack (`Attack.FireProjectileBurst`) the charge sets `m_projectiles` to 1 + 4 x the charge, rounded (1
tapped, 5 at full charge, 3 s); each leaves the wand's tip toward the crosshair (`TipSpawn`) and, as the game sets it up
(`Projectile.Setup`), turns into an even horizontal fan 8 degrees apart centred on the aim (one alone flies straight
at it; five span 32 degrees). Each carries the whole hit the game built for it: frost 15 + spirit 15 (+1 each a
quality), the charge's +30% and the skill, so each slows by the game's frost rule, and each that hits a character raises
Elemental Magic, as a bolt does. Hits as the game's projectiles hit: never the caster, of other characters only the
caster's enemies (other players only with PvP on, never a tame). The caster's machine owns the shards and decides their
hits; every player sees them by the game's own sync. Not yet seen in game.

### The Greywood Wand's rock (user, 2026-11-02)

"black forest left click spell wand should be like a rock", then "the charged version should be like a large fragmented
rock fires and the pieces start coming apart into an attack cone, but it's a tight cone", then (the second version) "more
greenish aspects ... the light at the end of the wand should be more greenish with little rock ... when charging the rock
gets bigger ... dark greenish spell particle ... thrown like a spell and shoot straight ahead ... shoot the large rock for
5 meters, then break up ... into like 15 pieces and keep going forward ... keep a tight cone." The Greywood Wand's left
mouse is the same click, combo, charge and eitr as the bolt (4 eitr a cast however many pieces, up to a third less with
Elemental Magic; the charged release paid by the cast it replaced), but it throws a copy of a Greydwarf's stone:
`Greydwarf`'s default item `Greydwarf_throw` fires `Greydwarf_throw_projectile`, a scene prefab (read live 2026-11-02: no
damage, area or status effect of its own, gravity 10, 0.5 m wide, its visual spinning; hit effects
`sfx_greydwarf_stone_hit` and `vfx_arrowhit`, both networked). Ours (`EE_WandRock`, `WandRock`) keeps the look and the hit
effects, flies like a spell (no gravity, no drag) for at most 4 s, hits 0.15 m wide at the stone's size, gives 1
adrenaline a hit, and shares its size with every machine.

- **The charge.** While charging, the little spell at the tip is the Greywood's own `ee_fx_charge_rock` (a mossy green glow
  with tiny stone chips round it; `BoltTint`, the pale blue `ee_fx_charge_tint_paleblue` while the bundle lacks it), and a
  real stone sits in it (`ChargeStone`: a local copy of the rock's visual meshes and materials, no collider), there from the
  charge's start, growing with the charge from the size a tap throws to the size a full charge throws and turning slowly,
  gone when the rock flies or the charge is broken. Both are drawn from `ChargeState`, so on every machine near.
- **The throw.** As the game sets it up (`Projectile.Setup`, `RockShot`) it goes straight from the wand's tip at the
  crosshair (`TipSpawn.AimPoint`), no arc, and the charge (`WindUp.Thrown`) makes it 1 (a tap) to 2.5 times the stone's
  size, its width with it (`BoltSize` leaves it alone), and adds up to 30 push to the wand's 10 (the Club's is 30, the
  bronze mace's 80). Every rock and piece wears a dark green spell trail (`RockTrail`, a component on the prefab: each
  machine that has the networked rock attaches a local `ee_fx_rock_trail` at its centre as it appears, scaled with it,
  gone with it; none while the bundle lacks it).
- **The break.** A tap or a hold under 0.15 of a full charge throws one whole rock. Above that the rock breaks into
  15 x the charge pieces, rounded, at least 2 (five a second held: 2 at half a second, 5 at 1 s, 10 at 2 s, 15 at the full
  3 s; user, 2026-11-03: "a little more proportional to how long it took to charge the shot"), once it has flown 4.5 to
  5.5 m from the tip (random each throw), on the caster's machine (`RockBreak`): its own routed message (`RockBurst`, `EliteEquipment_RockBreak`, to every player within
  60 m) draws a local `ee_fx_rock_break` there on each machine, its +Z along the flight, with the game's `sfx_rock_hit`
  (the game's `vfx_RockHit` while the bundle lacks the burst); the rock goes and its pieces fly on, straight, each
  from a point of its own within the rock and nothing laid out (user, the same day: "it also just shouldn't look like
  some uniform pre planned distribution"): a random heading in a 4.5 degree cone round the flight (spread evenly over the
  cone's disc), a random speed (the rock's, give or take 15%) and a random size (0.3 to 0.6 times the stone's; every piece
  0.15 m wide to hit). Each piece carries the rock's hit (blunt 7 + spirit 7, +1 each a quality, the charge's +30%, the
  skill and the push) times 1.5 by its share of the pieces' sizes: all of them landing is 1.5 times the rock, a bigger
  piece hitting harder (all 15 at full charge together 1.95 times a tap). A rock that hits something within
  5 m hits whole.

Hits as the game's projectiles hit: never the caster, of other characters only the caster's enemies (other players only
with PvP on, never a tame); each rock or piece that hits a character raises Elemental Magic. The caster's machine owns the
rock and its pieces and decides their hits; every player sees them and their hit effects by the game's own sync. No
cracked look: the big rock is the stone scaled (a cracked one needs new art). A stone that cannot be made leaves the wand
firing the bolt. Not yet seen in game.

### The Ashwood Wand's firebolt (user, 2026-11-02)

"need ashlands wand with firebolt as main lmb." The Ashwood Wand's left mouse is the same click, combo, charge and eitr as
the bolt (4 eitr a cast, up to a third less with Elemental Magic; the charged release paid by the cast it replaced), but
it fires a copy of the Staff of Embers' fireball: `StaffFireball` fires `staff_fireball_projectile`, a scene prefab (read
live 2026-11-02: no damage of its own, a 3 m area (`m_aoe`), gravity 4, 20 s, 0.1 m wide, no spawn on hit or status
effect; its look a burning `Sphere`, `flames`, `flames_world` and a `Point light`; its hit effect
`fx_fireball_staff_explosion`, networked). Ours (`EE_WandFirebolt`, `Firebolt`) keeps the look (its particles made to
follow its scale), flies straight (no gravity) at the wand's 20 m/s for 3 s, gives 1 adrenaline a hit and shares its size
with every machine. A tap is a single-target bolt: no area, and the Surtling fireball's small burst where it lands
(`vfx_FireballHit`, `sfx_imp_fireball_explode`, both networked) instead of the staff's explosion. As the game sets it up
(`Projectile.Setup` postfix, `FireboltShot`) the charge (`WindUp.Thrown`) makes it 0.5 (a tap) to 1 times (full charge,
3 s) the staff's fireball, its hit width 0.1 to 0.2 m with it (`BoltSize` leaves it alone). From 0.9 of a full charge
(2.7 s held) it explodes where it lands: the game's own area (`Projectile.DoAOE`, read 2026-11-02) cut to 2 m, with the
firebolt's own hit, buildings skipped (`m_aoeSkipWearNTear`), and the staff's explosion. Damage is fire 27 + spirit 27
(+1 each a quality), the charge's +30% and the skill; the fire sets burning by the game's own rule. Hits as the game's
projectiles hit, the explosion too: never the caster, of other characters only the caster's enemies (other players only
with PvP on, never a tame); a firebolt that hits a character, itself or by its explosion, raises Elemental Magic. The
caster's machine owns the firebolt and decides its hit and explosion; every player sees it, its size and its burst or
explosion by the game's own sync. A fireball that cannot be made leaves the wand firing the bolt. Its charge at the tip
stays white (`BoltTint` has no tint for it: none built yet). Known: the light keeps the staff's 4 m range at any size;
the explosion looks like the staff's 3 m one over a 2 m hit; the area hits other world objects (trees, rocks) as a direct
hit would. Not yet seen in game.

### Elder's Volley (user, 2026-11-02)

"Make another spell page where the Elder's attack where things shoot fast out of his hand... that is the spell." Spell 7
of features/casters.md: key `volley`, tier 2, the first **strike** (`SpellKind.Strike`: cast where the caster aims, never
at a friend, so no heal lock and no turn), 20 eitr, up to a third less from Elemental Magic (a strike's skill,
`Spell.Skill`; heals and chants keep Blood Magic, and a strike's cast raises no Blood Magic). Its page `EE_Spell_Volley`
("Page: Elder's Volley", icons `ee_spell_volley_icon` and `ee_spellicon_volley` in `ee_spells_meadows`) is a boss page:
the Elder (`gd_king`) drops one per player present (`BossPages`, as Eikthyr's), never loot. Cast from the ring, a right
mouse tap or middle mouse like every spell; when the cast lands (`SpellCast`, the `Attack.DoNonAttack` postfix; the
shared cast motion and flash, tinted green) `VolleyCast` fires 6 seeds from the wand's `tip` on the caster's machine, the
first at once and one every 0.1 s (the last at 0.5 s), each toward what the crosshair is on at that moment
(`TipSpawn.AimPoint`), turned at random up to 1.5 degrees across and up, at 30 m/s.

The seed `EE_VolleySeed` (`VolleySeeds`) is a copy of the Elder's own shot. `gd_king`'s four attack items are
`gd_king_rootspawn` (the roots), `gd_king_scream`, `gd_king_shoot` and `gd_king_stomp`; `gd_king_shoot` (the `shoot`
clip; pierce 35, chop 20, pickaxe 20; 25 bursts 0.1 s apart at 30 m/s, 10 degrees of spread) fires
`gdking_root_projectile`, a plain networked projectile (no gravity, no area, nothing spawned on a hit; 6 s, 0.5 m wide;
it stays 1 s where it strikes the ground or a wall; spores; `vfx_gdking_projectile_hit` and `sfx_gdking_projectile_hit`
where it lands), read live 2026-11-02. Ours keeps the look, the hit effects and the stay, flies 3 s, hits 0.1 m wide as
the bolt does, and gives the bolt's adrenaline (1 a hit). Each seed's hit (`VolleyHit`) is built as the game builds the
bolt's (`Attack.FireProjectileBurst`): the held wand's damage with its quality, times 0.6, the bolt attack's multiplier
and the caster's random Elemental Magic factor, then the caster's status effects (`SEMan.ModifyAttack`); so its types are
the wand's: Gnarled pierce 3.6 + spirit 3.6, Greywood blunt 4.2 + spirit 4.2, Rootbound blunt 6.6 + spirit 6.6, Frostsilver frost 9 +
spirit 9, Blackmetal pierce 11.4 + spirit 11.4 (+0.6 each a quality), before the skill. Each seed that hits a character
raises Elemental Magic by the bolt's amount. It hits as the bolt does: never the caster, of other characters only the
caster's enemies (other players only with PvP on), and no damage to a friend even then (`m_noDamageFriendly`: a tame).
The caster's machine owns the seeds and decides their hits; every player sees them by the game's own sync; a dedicated
server only relays them, and the page drop is decided on the Elder's owner. The rest are not fired once the caster can
no longer cast: dead, the wand or the book put away (or another wand drawn), swimming, teleporting, or the switch off.

Its own motion and effects (see "Spell motions and effects": the wand thrust forward and held, a recoil per seed; green
seeds spiralling into the tip; `ee_fx_cast_volley` at the tip with every seed) pace the seeds by the clip's six `Hit`s
(0.30 to 0.80 s, one a recoil) instead of the 0.1 s timer, which stays for a machine without the clip. `BoltSize`'s
`Projectile.Setup` postfix leaves the seeds their size (a spell is never charged).
Not yet seen in game.

### Cleanse (user, 2026-11-02)

"Make another spell that removes all debuffs from ally or self." Spell 8 of features/casters.md: key `cleanse`, tier 3
(Swamp), a **heal** (`SpellKind.Heal`), 12 eitr (up to a third less with Blood Magic; the Deerhide discount is Mend's
only), aimed exactly like Mend: the friend marked within 30 m (`HealTarget`, the gold marker shows; the lock and the turn
as Mend's), or the caster when none is, or that friend died or went out of reach. Its page `EE_Spell_Cleanse` ("Page:
Cleanse", icons `ee_spell_cleanse_icon` and `ee_spellicon_cleanse` in `ee_spells_meadows`) is a loot page: creatures and
dungeon chests of the Swamp and every later tier give it by `PageDrops`' rule (three times as likely in the Swamp).

It lands at once at the motion's `Hit` (`SpellCast` hands it to `Cleanse.Cast`; no flight: it has no bolt of its own, and
the gold mend bolt would read as Mend). Two things reach the target's owner, both by the game's own messages, so nothing
new is sent and a dedicated server or a tame owned by another machine works: `SEMan.AddStatusEffect(EE_SE_Cleanse)` (the
game sends a name hash to the owner, `RPC_AddStatusEffect`, read 2026-11-02) puts on `CleanseEffect`, a status effect with
no icon, words or stats in every item database (`CasterStatus`), whose `Setup` on the owner takes the harmful effects off
(`CleanseList.Strip`, quiet; `Setup` runs right after the game added the copy, not while `SEMan.Update` walks the list, so
removing there is safe) and which goes at the next update (`IsDone`); and `Character.Heal` for 0.2 of the wand's heal power
(`HealPower.Of`: the book and Blood Magic count; no Sunweave stamina). The release goes to every player near
(`CasterFx.Send`, the target's id with it; `SpellFx` hands it to `Cleanse.Show`), drawn on the target at its feet, with the
frost resistance mead's sound (the game has no water splash); its own release drops the shared cast flash at the tip.

What it takes (`CleanseList`, the game's names read from its item database 2026-11-02): `Burning`, `Spirit`, `Poison`,
`Frost`, `Lightning`, `Smoked`, `Tared`, `Wet`, `Slimed`, `Cold`, `Freezing`, `Harpooned`, `Immobilized`,
`ImmobilizedLong`, `ImmobilizedAshlands`; our own mods' published harmful ones (`ECP_Mired`, `ECP_Bleeding`,
`ECP_KrakenInk`, `ecr_batfiend_screech`, `ecr_batfiend_chill`, `ECF_Hamstring`, `ECF_Paralyze`; nothing when those mods
are absent); and any effect of the game's harm-only classes `SE_Burning`, `SE_Poison`, `SE_Frost`, `SE_Smoke`,
`SE_Harpooned` (another mod's fire or poison over time). Everything else stays: `Rested`, `Resting`, `Shelter`,
`CampFire`, `Warm`, food, every mead and potion, the chants, set bonuses, the Guardian powers, `CorpseRun`, `SoftDeath`,
`Encumbered` (a state, not an ailment) and the bukeberries' `Puke` (the player's own choice). Cold, freezing, wet and smoke
come straight back while their cause lasts. Not yet seen in game.

### Moder's Breath (user, 2026-11-02)

Spell `breath` of features/casters.md, "Nine more spells": tier 4, a **strike** (Elemental Magic, as Elder's Volley), 25
eitr. Its page `EE_Spell_Breath` ("Page: Moder's Breath", icons `ee_spell_breath_icon` and `ee_spellicon_breath` in
`ee_spells_meadows`, Hearthsong's until the bundle has them) is a boss page: Moder (`Dragon`) drops one per player present
(`BossPages`), never loot. When the cast lands (`SpellCast`) `BreathCast` breathes six bursts from the wand's `tip` on the
caster's machine, one per `Hit` of the thrust clip (the first at the cast, the next five from `SpellHits`; one every 0.1 s
without the clip), each toward what the crosshair is on at that moment; Moder's breath sound
(`sfx_dragon_coldbreath_trailon`, the game's own networked prefab) once with the first.

Moder's breath, read live 2026-11-03: the Dragon's six attack items are `dragon_taunt`, `dragon_bite`, `dragon_claw_left`,
`dragon_claw_right`, `dragon_spit_shotgun` and `dragon_coldbreath`; the breath (`attack_breath`) is a melee sweep from the
jaw, 30 m, 10 degrees, rays 2 m wide, through walls, frost 200, chop 200, pickaxe 200, and has no projectile: its look is
its trail start effect `vfx_dragon_coldbreath`, a networked object (ZSyncTransform, 10 s TimedDestruction) of three
particle cones along +Z (clouds, pixel flakes, spray with a splat; 1 s each at 5 to 40 m/s) and a point light, with no
camera shake. Ours, `EE_SpellBreath` (`BreathLook`, registered in the scene on every peer, no game art shipped), is that
copy shrunk to a caster's cone: a fifth of the speed (about 7 m), half as wide again (about 30 degrees), 0.4 the size,
a quarter second of half the particles per burst, the light 2.5 m out at 0.4 its range, gone after 4.5 s; any CamShaker
stripped as `WandRoots` does. The caster's machine spawns and owns each burst's object; every player sees it by the
game's own sync; a dedicated server only relays it.

Each burst hits (`BreathCone`, on the caster's machine) every character whose body reaches into the cone (7 m from the
tip, 15 degrees each side of the aim, widened by the body's radius) with no ground, rock or building between the tip and
its middle, and only the caster's enemies, by the bolt projectile's rule (`Projectile.IsValidTarget`): never the caster,
never a tame, other players only with PvP on for both, no one dodging. The hit (`BreathHit`, sent to the target's owner
by `Character.Damage`) is built as `VolleyHit` builds a seed's: the held wand's damage with its quality turned to frost
and spirit (its spirit stays spirit, every other type's amount becomes frost), times 0.5, the bolt attack's multiplier
and the caster's random Elemental Magic factor, then the caster's status effects; no push. The frost slows by the game's
own rule (the target's owner adds the Frost status, `SE_Frost`, for any frost damage). A burst that hits anyone raises
Elemental Magic by the bolt's amount and gives 1 adrenaline, once. A caster who can no longer cast breathes no more.
Not yet seen in game.

### Eir's Spring, Chain Mend, Second Wind and Revive (user, 2026-11-02)

Four heals of features/casters.md, "Nine more spells" (keys `spring`, `chainmend`, `secondwind`, `revive`; all
`SpellKind.Heal`, so their casts raise Blood Magic and their cost is cut by it). Pages `EE_Spell_Spring`,
`EE_Spell_ChainMend`, `EE_Spell_SecondWind`, `EE_Spell_Revive` (icons `ee_spell_<key>_icon` and `ee_spellicon_<key>`
in `ee_spells_meadows`; the shared fallbacks while the bundle lacks them). `SpellCast` hands their landing to
`HealSpells.Cast` (one line in its dispatch), asks `HealSpells.Ready` as a cast starts (only Revive can refuse) and
locks the marked friend only for Chain Mend (`HealSpells.Unmarked`: the spring, the wind and Revive neither lock nor
turn the caster). Their releases are drawn by `HealReleases.Draw`, called from `SpellFx.PointRelease` before the shared
flash path (which would tint them). Every heal is `Character.Heal` and every stamina or effect a status effect, both
of which the game sends to the target's owner, so a dedicated server and a tame owned by another machine work; the
effects are local copies on each machine near (`CasterFx`'s routed message, as every spell's).

- **Eir's Spring** (`Spring`, `SpringPool`, `GroundPoint`). Tier 3 loot page, 25 eitr. The circle's centre is the aim
  (`TipSpawn.AimPoint`) dropped to the ground under it (`GroundPoint.Below`) when within 20 m of the caster, else the
  caster's feet. A `SpringPool` object on the caster's machine heals every friend inside (players and tames,
  `FriendTarget.Friendly`; inside is 4 m across and 2.5 m up or down) by 0.1 of the wand's heal power (`HealPower.Of`
  at the cast) once a second, 8 times (at 1 to 8 s), and ends early if the caster leaves or the switch goes off. The ring
  `ee_fx_cast_spring` is sent once (`CasterFx`, no duration in the message: the spring's 8 s is the spell's own) and drawn
  looping at the centre, as authored, destroyed after 8 s; without it the gold chant ring flashes at 4 m. Sound: the
  health mead's.
- **Chain Mend** (`ChainMend`, `ChainMendRun`). Tier 6 loot page, 20 eitr. The first link is the friend locked as the
  cast started (`HealTarget`, as Mend's), or the caster; it is healed at once at the `Hit` (no flying bolt: the chain's
  arcs are its look) for the full heal power. Then every 0.15 s `ChainMendRun` jumps from the last one healed to the
  nearest friend within 8 m who is hurt (more than 0.5 under their most health) and not yet healed (the caster can be
  one), up to 4 in all, each heal 0.8 of the one before (1, 0.8, 0.64, 0.51). A Sunweave wearer's heals give their stamina
  as Mend's. Each link sends `ee_fx_chainmend_arc` (from the wand's tip or the last one's middle, with the healed one's
  id; none when the first is the caster) and Mend's `ee_fx_mend` motes. The arc is drawn from its start, its +Z turned
  to the healed one's middle and its transform's Z scaled by the distance (authored along +Z over 1 m; a mesh or
  hierarchy-scaled beam stretches); without it only the motes show.
- **Second Wind** (`SecondWind`). Tier 5 loot page, 20 eitr. Every player within 10 m, the caster too, gets 50 stamina
  at once: the caster adds `EE_SE_SecondWind` (an `SE_Stats` with no icon or words, 0.1 s long, `m_staminaUpFront` 50) by
  name hash; the game sends it to each player's owner (`SEMan.AddStatusEffect`), where `SE_Stats.Setup` calls
  `Character.AddStamina` (read 2026-11-03), the Sunweave heal stamina's route. Tames get none. The ring
  `ee_fx_cast_secondwind` is drawn at the caster's feet by `SpellFx.Ground` (turned to the caster's facing, rolled out to
  10 m as the chants'), the gold chant ring without it; the stamina mead's sound.
- **Revive** (`Revive`, `ReviveGrave`, `ReviveArrival`, `ReviveCooldown`). 50 eitr. Its page is the only one found in
  dungeon chests alone: `Spell.ChestOnly` 0.02, rolled by `ChestPages` in every dungeon chest as its loot is first made
  (any biome, beside the 10% biome page), and kept out of `PageDrops` (`Spell.Loot` is false), so no creature or boss
  drops it. The grave is the game's `TombStone` (read 2026-11-03: `Player.CreateTombStone` calls `TombStone.Setup` with
  the profile's name and player id, `ZDOVars.s_ownerName` and `s_owner`; `TombStone.Awake` stamps `s_timeOfDeath` with
  `ZNet.GetTime().Ticks` on its owner), found by a 0.75 m sphere cast along the camera's view, any layer, within 30 m of
  the caster (`ReviveGrave.Aimed`). It can be revived when its owner is not the caster, died within 3 min and is in the
  server's player list (by name: the list has no player id). As the cast starts the grave is checked (`Revive.Refusal`,
  before any eitr is spent: refused with a message with no such grave, an own grave, too late, owner gone, or while the
  caster's `EE_SE_ReviveCooldown` runs, a 300 s `SE_Stats` with the spell's icon, Mend's without it) and kept; at the
  `Hit` it is checked again, then one routed message goes to everybody (`EliteEquipment_Revive`: owner id, grave
  position, grave id, caster name), the cooldown is put on the caster, the pillar `ee_fx_cast_revive` shows at the
  grave's ground and the caster is told. Only the machine whose profile's player id is the grave's owner acts
  (`Revive.Receive`; a dedicated server never does): `ReviveArrival` waits up to 30 s for the player to be alive in the
  world (still on the death screen, or in the first 2 s after a spawn when `Player.TeleportTo` refuses, its
  `m_teleportCooldown`), then calls `Player.TeleportTo(grave + 0.5 m up, distantTeleport: true)` (the game's own: the
  area loads, the floor is found) and, once the teleport is over, heals them up to half their most health
  (`Character.Heal`, never lowering it), tells them who called them and shows the pillar on them. A second call to the
  same grave is ignored on that machine. The pillar without its effect: Mend's motes.
- **Patches**: postfix `ZNet.Awake` (`Revive.NetAwake`, registers `EliteEquipment_Revive`; `ZNet.Awake` makes
  `ZRoutedRpc` first, read 2026-11-03). Nothing else is patched; `Humanoid.StartAttack` and `Attack.DoNonAttack` are
  `SpellCast`'s, `Container.AddDefaultItems` is `ChestPages`'. Methods read 2026-11-03: `TombStone.Awake`/`Setup`,
  `Player.CreateTombStone`, `Player.TeleportTo`/`UpdateTeleport`/`IsTeleporting`, `Player.AddStamina`,
  `SE_Stats.Setup`, `Character.Heal`/`RPC_Heal`/`SetHealth`, `ZRoutedRpc.InvokeRoutedRPC` (target 0: every peer, the
  sender too), `ZNet.PlayerInfo`.

Not yet seen in game.

### Ward (features/casters.md, "Nine more spells")

Spell `ward`: tier 4 (Mountains), a **ward** (`SpellKind.Ward`, new: the Spellbook says Ward; Blood Magic as a heal), 20
eitr. Its page `EE_Spell_Ward` ("Page: Ward", icons `ee_spell_ward_icon` and `ee_spellicon_ward`) is a loot page by
`PageDrops`' rule. Aimed exactly like Mend: the friend marked within 30 m (`HealTarget` marks with a Ward page in the book
as with a heal page; the lock and the turn as Mend's), or the caster when none is, or that friend died or went out of
reach. It lands at once at the motion's `Hit` (`SpellCast` hands it to `Ward.Cast`): `SEMan.AddStatusEffect(EE_SE_Ward)`
with resetTime and 2 x `HealPower.Of` as its skill level (the game sends the hash and the level to the target's owner,
`RPC_AddStatusEffect`, and calls `SetLevel` there; read 2026-11-03) puts on `WardEffect`, our own `SE_Shield`, the Staff of
Protection's class. The game's `Staff_shield` (read live 2026-11-03): 60 s per item level, 200 + 5 per skill level + 300
per world level, Blood Magic raised as it breaks, its bubble `vfx_StaffShield` a networked start effect. Ours: 20 s
whatever the item level, 0 + 1 x the skill level (the caster's amount), nothing per world level, no skill raised; the game
shield's hit and break effects (`fx_StaffShield_Hit`, `fx_StaffShield_Break`) and, while ours is missing, its icon
borrowed (`GameBorrow`, out of each item database as `CasterStatus` prepares it). It takes all of every hit until the
damage it took passes the amount (the game's `OnDamaged` and `IsDone`); cast again on a warded friend it starts whole
(`SetLevel` clears the damage taken). Its bubble is our own, not the networked start effect: `ee_fx_cast_ward` goes to
every player near with the target's id (`CasterFx.Send`; `SpellFx.Draw` hands it to `ReleaseDraws`, then `WardBubble`),
drawn on the target at its feet, sized to the body, following it, for 20 s at most; as the ward ends on the owner (broken,
run out, removed, its wearer dead: `WardEffect.Stop`) the owner sends `ee_ward_end` the same way and every machine takes
the bubble off. Without `ee_fx_cast_ward` in the bundle the bubble is a local copy of the game's `vfx_StaffShield`. Sound:
the frost staff's shield forming (`sfx_stafffrostorbs_shield_spawn`). Not yet seen in game.

### Sanctuary (features/casters.md, "Nine more spells")

Spell `sanctuary`: tier 6 (Mistlands), a **ward**, 40 eitr. Its page `EE_Spell_Sanctuary` ("Page: Sanctuary", icons
`ee_spell_sanctuary_icon` and `ee_spellicon_sanctuary`) is a boss page: The Queen (`SeekerQueen`) drops one per player
present (`BossPages`), never loot. No cast starts while its cooldown is on: `SpellCast`'s StartAttack prefix refuses it
before any eitr is paid (`Sanctuary.Waits`, "Sanctuary is not ready again yet"). When the cast lands (`Sanctuary.Cast`)
the caster gets `EE_SE_SanctuaryCooldown` (`SanctuaryEffect.MakeCooldown`: 120 s, the spell's icon and the game's
countdown; a status effect, so a death clears it) and the dome stands at the caster's feet for 10 s: a coroutine on the
caster's machine gives, every 0.5 s, each friend within 6 m of its centre (players and tames, `FriendTarget.Friendly`; a
sphere, as `Character.GetCharactersInRange` measures) `EE_SE_Sanctuary` (`SanctuaryEffect`, 1 s, refreshed) through
`SEMan.AddStatusEffect`, sent to each one's owner. Its `OnDamaged` (the game's `SEMan.OnDamaged`, called from
`Character.RPC_Damage` on the owner before the hit is applied; read 2026-11-03) leaves 0.7 of every hit. It ends within
1 s of stepping out or of the dome's end; the dome ends early should the caster die or leave (the coroutine is the
caster's). `ee_fx_cast_sanctuary` goes to every player near and is drawn where it stands for 10 s (`Sanctuary.Show`; the
shared chant ring in its colour without it). Charge `ee_fx_charge_ward` at the tip; sound as Ward's. Not yet seen in game.

### War Chant (features/casters.md, "Nine more spells")

Spell `warchant`: tier 5 (Plains), a **chant**, 25 eitr, a loot page (`EE_Spell_WarChant`, "Page: War Chant"). Cast as
Hearthsong (`SpellCast`: every friend within 10 m, the caster too, by `SEMan.AddStatusEffect`); its status effect
`EE_SE_WarChant` (`EE_SE_WarChant_Long` for a Mossweave wearer) is the game's `SE_Stats` with `m_modifyAttackSkill` All
and `m_damageModifier` 1.1 (`WarChant.Stats`; `SE_Stats.ModifyAttack` multiplies the hit's damage for any skill when the
skill is All, run by `SEMan.ModifyAttack` on the attacker's machine; read 2026-11-03): 10% more damage of every kind for
60 s (90 s). Charge `ee_fx_charge_war` under the feet; release `ee_fx_cast_warchant` at the feet, turned to the caster's
facing and scaled to the chant's reach as Hearthsong's (`ReleaseDraws`, `SpellFx.Ground`). The two-chant limit is still
not built. Not yet seen in game.

### Feather Fall (features/casters.md, "Nine more spells")

Spell `featherfall`: tier 4 (Mountains), a **chant**, 15 eitr, a loot page (`EE_Spell_FeatherFall`, "Page: Feather
Fall"). Cast as Hearthsong; its status effect `EE_SE_FeatherFall` (and `_Long`) is the feather cape's: the game's
`SE_Stats` `SlowFall`, `CapeFeather`'s equip effect, read live 2026-11-03: `m_maxMaxFallSpeed` 5 m/s (the game's
`ModifyWalkVelocity` caps the fall) and `m_fallDamageModifier` -1 (no fall damage). `FeatherFall.Stats` sets those two
(constants), `GameBorrow` copies the cape's own out of each item database as it is prepared (and its icon while ours is
missing); 30 s (45 s for a Mossweave wearer: the chant's 60 and 90 halved). Charge `ee_fx_charge_feather` under the feet;
release `ee_fx_cast_featherfall`, one shot, on every friend of the caster within 10 m of where it was cast, on each
machine that draws (`FeatherFall.Show`; the shared chant ring without it). Not yet seen in game.

## Spell motions and effects (user, 2026-11-02)

"These spells shouldn't just have that base white animation when charging/casting, they need their own animations that
make sense." Each spell has its own upper-body clip in `ee_casteranim` (`Spell.Motion`, `SpellClips`, the workshop's
`out/spells.json`), its own charge in `ee_casterfx_meadows` shown from the cast's start to the clip's first `Hit`
(`Spell.ChargeEffect`) and its own release where the cast lands (`Spell.ReleaseEffect`, `SpellFx`):

| Spell | Motion (clip, motion end, `Hit`) | Charge (cast start to the `Hit`) | Release (at the `Hit`) |
| --- | --- | --- | --- |
| Mend | `ee_spell_reach`, 0.90 s, 0.40 s | `ee_fx_charge_mend` at the tip | its mend bolt to the friend, or the motes on the caster (as before) |
| Hearthsong | `ee_spell_raise`, 1.30 s, 0.92 s | `ee_fx_charge_hearth` on the ground under the feet, following the caster | `ee_fx_cast_hearth` at the feet, scaled to the chant's reach (10 m / 10) |
| Stag's Swiftness | `ee_spell_sweep`, 0.80 s, 0.42 s | `ee_fx_charge_swift` under the feet | `ee_fx_cast_swift` at the feet, its +Z (the wisp's dash) the caster's facing |
| Elder's Volley | `ee_spell_thrust`, 1.30 s, six 0.30 to 0.80 s | `ee_fx_charge_volley` at the tip | one seed and `ee_fx_cast_volley` at the tip per `Hit` |
| Moder's Breath | `ee_spell_thrust` (the volley's), 1.30 s, six 0.30 to 0.80 s | `ee_fx_charge_frost` at the tip (the Frostsilver bolt's) | one burst of Moder's breath (`EE_SpellBreath`, networked) per `Hit`, Moder's breath sound with the first |
| Cleanse | `ee_spell_reach` (Mend's), 0.90 s, 0.40 s | `ee_fx_charge_cleanse` at the tip | `ee_fx_cast_cleanse` on the target at its feet, following it (Mend's gold motes without it) |
| Ward | `ee_spell_reach` (Mend's), 0.90 s, 0.40 s | `ee_fx_charge_ward` at the tip | `ee_fx_cast_ward` on the target while the ward lasts (the game shield's bubble without it) |
| Sanctuary | `ee_spell_raise` (Hearthsong's), 1.30 s, 0.92 s | `ee_fx_charge_ward` at the tip | `ee_fx_cast_sanctuary` at the caster's feet, ended at 10 s (the shared ring without it) |
| War Chant | `ee_spell_raise` (Hearthsong's), 1.30 s, 0.92 s | `ee_fx_charge_war` under the feet | `ee_fx_cast_warchant` at the feet, scaled to the chant's reach |
| Feather Fall | `ee_spell_sweep` (Swiftness's), 0.80 s, 0.42 s | `ee_fx_charge_feather` under the feet | `ee_fx_cast_featherfall` on every friend within 10 m, one shot |
| Eir's Spring | `ee_spell_raise`, 1.30 s, 0.92 s | `ee_fx_charge_cleanse` at the tip | `ee_fx_cast_spring` on the ground at the circle, looping, ended at 8 s (the gold chant ring at 4 m without it) |
| Second Wind | `ee_spell_sweep`, 0.80 s, 0.42 s | `ee_fx_charge_swift` under the feet | `ee_fx_cast_secondwind` at the feet, to 10 m, the caster's facing (the gold chant ring without it) |
| Chain Mend | `ee_spell_reach`, 0.90 s, 0.40 s | `ee_fx_charge_mend` at the tip | `ee_fx_chainmend_arc` stretched between each pair and `ee_fx_mend` on each |
| Revive | `ee_spell_reach`, 0.90 s, 0.40 s | `ee_fx_charge_mend` at the tip | `ee_fx_cast_revive` at the grave, and on the revived player as they arrive (Mend's motes without it) |

- **How a motion plays.** On the caster's machine, before the game clones the wand's secondary attack, `SpellMotion.Choose`
  names the charged staff's cast state (`staff_charge_attack`, whose trigger `WandAnimations` holds back) when the
  spell's clip is there and the caster plays their own controller copy, else the game's `staff_fireball` as before.
  Once the game has started the cast, its spell goes to every player near on the bolt's own move message
  (`EliteEquipment_WandMove`, numbers from `SpellMotion.Base` = 5 on: the spell's place in `Spells.All`; no new RPC).
  Each machine that draws puts the clip in that player's `Charge Staff Attack 2` slot, cross-fades the upper body in
  (0.05 s, from 0), shows the charge until the first `Hit`, fades out where the motion ends (0.15 s) and puts
  `ee_anim_wand_casts` back; a bolt's move or charge for the same player ends a spell's motion at once. The spells play
  at the animator's own speed (the bolt's 0.8 is not applied).
- **One controller per player.** The swapped controller is copied for each player as they wake (`PlayerControllers`,
  destroyed with them), so one player's spell in the slot never changes another's bolt. A slot change saves each
  layer's state and time and plays a layer back only if it has reset.
- **The cast at the `Hit`.** The clip's first `Hit` is the game's attack trigger (`DoNonAttack`, where `SpellCast` casts,
  the wand wearing once); later ones never cast again, except each of the volley's next five fires its next seed
  (`SpellHits`, `VolleyCast.Seed`). A `Hit` more than 0.15 s before the clip's first after the motion began is the bolt's
  fading cast state playing the spell clip for a moment, and is ignored.
- **Effects** are local copies on each machine near (LocalEffects), never networked, none on a dedicated server: the
  charge from the motion message, the release from `CasterFx`'s message (the chants now send their own release with the
  caster's id, for the facing). A spell with its own effects drops the shared cast flash but keeps its sound; the chant
  releases keep the chant ring's sound; the volley's per-seed burst has none of its own. The shared white charge (at the
  tip), flash and chant ring are the fallback when the bundle lacks an effect.
- **The bolt's charge in the stone** (`BoltTint`, read from the wand the game shows in the hand, so on every machine):
  Gnarled `ee_fx_charge_tint_amber`, Greywood its own `ee_fx_charge_rock` with a stone growing in it (`_paleblue` while
  the bundle lacks it; see "The Greywood Wand's rock"), Frostsilver `_ice`, Rootbound `_earth`; the Blackmetal Wand
  keeps the white `ee_fx_charge` (no stone colour of its own). `ee_fx_charge_full`, `_ready` and `_stream` stay white.
- **The book.** The open-palm pose (`BookPose`/`BookHold`) runs after the animator and yields only to layer 0's tags, so
  it holds the left arm through every spell motion. With a book open on the palm the charge stream leaves the middle of
  its pages (`StreamStart`: the open book's top along +Y, the flat one's front cover along +Z, measured once per stream
  from its renderers), not the palm under it.
- **Patches**: prefix `Attack.OnAttackTrigger` (`SpellHits`); postfix `Player.OnDestroy` (protected, `PlayerControllers`);
  `SpellCast`'s `Humanoid.StartAttack` prefix and postfix now choose and send the motion. Checked against a fresh decompile
  (2026-11-02): `Humanoid.StartAttack` clones the secondary attack after the prefix; `Attack.Start` sets the trigger named
  by `m_attackAnimation`; `Attack.Update` starts and stops on `InAttack`, which counts any layer's `attack` tag;
  `Humanoid.OnAttackTrigger` runs only on the owner; `VisEquipment.m_rightItem` is the prefab name's hash.

Not yet seen in game.

## The spell ring (user, 2026-11-02)

"Right clicking and holding brings up the halo menu and you drag the mouse to the spell you want (more the direction,
the angle from the center selects the spell)"; "Keep the spell icon for RMB, but get rid of RMB+LMB. You can click RMB
quick to recast the last spell you selected." The ring is the HaloRing library (ValheimModLibs, merged; HaloMenu's ring
taken out of it), so players install nothing more. `SpellRing` reads the game's raw `Block` / `JoyBlock` buttons once a
frame from `CasterHud` (not `SetControls`: while the ring is open the library's WindowInput makes
`PlayerController.InInventoryEtc` true, so the player's controls get no block, attack or mouse look):

- Right mouse held for `SpellRing.HoldSeconds` (0.2 s, no setting) with a wand drawn and the player taking input: the
  ring opens at the screen centre with every page of the book in the left hand (`SpellRingItems`: icon, name, eitr
  cost under the name as the cast would take it, the first page at twelve o'clock and the rest clockwise; a page whose
  cost is above the caster's eitr drawn dim). The mouse direction from the centre picks (even past the rim; the gamepad's
  right stick the same); letting go casts the spell under it at once and saves it on the wand (`LastSpell`); letting go
  in the centre, Esc, or a game screen opening casts nothing. A dim page let go on is still picked and goes to the cast,
  where the game refuses it for eitr (its eitr bar flashes).
- Right mouse let go before 0.2 s (a tap): the wand's last spell again. Middle mouse (the game's secondary attack)
  casts it too. A last spell whose page is not in the held book, or none yet: the cast's own "no page" message.
- Without a book: no ring, "Hold a spellbook in the left hand to cast spells" once per hold; a book with no pages: "The
  book has no pages" once. The bolt (left mouse), the blink (dodge) and the Rootbound Wand's roots are unchanged.
- The cast goes the old way: `SpellRing` asks for it (valid 0.5 s), `CasterControls` turns it into the wand's secondary
  attack on the next `SetControls`, `SpellCast` casts the wand's `LastSpell`. The heal target (`HealTarget.Current`) is
  frozen from the ring opening until that cast starts (`SpellRing.Aiming`): a heal goes to the friend marked as the ring
  opened, or to the caster. The camera does not turn while the ring is open; walking works; attacks, the bolt and the
  hotbar are held back.
- Right mouse held + jump within the 0.2 s: the blink (as the game's block-dodge), and that press opens no ring and
  casts nothing. Left mouse does nothing while right mouse is held. Casters do not block (the block button never blocks
  with a wand).
- Local UI only: the ring's canvas `EliteEquipment Ring [spells]`, the library's patches installed with EliteEquipment's
  own Harmony (`Plugin.Awake`, `HaloRingMenu.Install`), one ring open at a time across every mod's copy of the library.
  Nothing on a dedicated server (`CasterHud` runs on a HUD only).

Until 2026-11-02 a wand held two spells in slots (user, 2026-10-11: "2 extra spells only"), set in the Spellbook: right
mouse let go cast the first, right mouse held + left mouse the second; until 2026-10-11 three, and Left Alt + wheel
picked one.
A dodge with a wand drawn is a blink (`CasterBlink`): up to 5 m along the dodge (back when standing), stopped short of
anything solid, set on the ground there, the dodge's own conditions, a flash at both ends, 0.8 s cooldown. It costs the
roll's stamina and half that in eitr (user, 2026-10-11: "as much stamina as dodging, and 1/2 that value of eitr"), and from the blink until 0.5 s after
nothing a roll dodges hits the caster ("when you cast to .5 seconds after you blink you are invulnerable": the game's own
dodge flag held up, on the owner and in the ZDO for the attackers' machines).

Robe armour follows the game's mage sets (half the biome's heavy armour, +2 per level; `CasterGear.ArmourPerLevel`): the
Meadows pieces 1 armour, +2 a level, no movement cost (leather has none). The eitr foods stay in EliteEquipment (user,
2026-10-11).

## The robes' skirts as cloth (user, 2026-11-02)

"The back foot glitches through the robe bottom, also when running the legs and feet completely glitch through the robe
bottom." Skinned to the shins the skirt creased hard at the knee; moved onto the thighs (the bundles' SHIN_HANG, every
set) the shins and feet came through the back standing with a foot back and running. No skin weights avoid both, so the
skirt of every seer's robe (8 sets, both bodies; the bundles unchanged) is moved by the game's own cloth, MagicaCloth
(`MagicaClothV2.dll`, the capes' simulation), on each client that draws the wearer:

- **What moves** (`RobeClothFixed`, once per robe mesh, from the bind pose: metres above the feet, the mesh's z up, 95 m
  a unit): below the hips line, 0.92 m (the seat; the hip joints are at 1.02, the crotch 0.887), every vertex the legs'
  bones carry (at least 5% of its weight) moves: the skirt, its lining, over-robes, fur rolls, lobe bands and tabs. A
  band 0.92 to 1.02 m is fixed (the cloth hangs from it). Everything else (bodice, sleeves, mantle, collar, sash, belt,
  charms, medallions; any vertex with 2% of its weight on an arm, hand, shoulder, neck, head or jaw bone) is left out of
  the cloth and skinned as before. A vertex below the line the legs do not carry would stay fixed, and a moving vertex in
  a triangle with a left-out one is held fixed instead, so no triangle links the cloth to a part skinned as before (none
  of either in any set today: no vertex below the line carries any arm, hand or head weight in any of the 16 meshes).
  Checked offline on the workshop's meshes of all 16: 695 (Swamp, to 0.52 m) to 1167 (Plains) moving vertices, 87 to
  210 fixed; the game's log gives the same counts. The ease from held to free: each shape setting runs on a curve over
  the skirt's depth (0 at the fixed band, 1 at the hem), changing most over the top 0.15 (about 10 cm).
- **The skirt's own mesh** (`RobeClothMesh`, once per robe mesh): the triangles with a moving vertex and their vertices
  (the skirt and the fixed band it hangs from) are copied into a mesh of their own, drawn by `EE_RobeSkirt`, a skinned
  renderer under the robe's with its bones, materials, property block and shadows; the robe's renderer draws a copy of
  its mesh without them. The cloth is built on the skirt's renderer with the list as MagicaCloth's per-vertex attributes
  (`vertexAttributeList`), no paint map. Why (user, 2026-11-02: "the bottom robe is like somehow attached to the top
  robe", a flap of the skirt held up at the left hand): MagicaCloth takes a renderer's vertices for its cloth by place,
  not by index (`VirtualMesh.SelectionMesh`): every vertex within 1.5 times the larger of the mean edge and the reduction
  distance (about 8 cm) of a listed vertex joins it, with every triangle touching it (in game 1747 of the Plains robe's
  2461 vertices for 1377 listed: the belt, rope, knots and upper skirt), and its reduction then merges vertices within a
  few centimetres whatever part they belong to, adding their bone weights together (`StepReductionBase`). Built on the
  whole robe in a pose with a sleeve's cuff beside the skirt, the cuff was merged into the skirt's particles and the skirt
  hung from the hand for as long as the cloth lived. The two meshes overlap by a row (user, 2026-11-03: "random horizontal cuts"): the robe's
  held triangles along the skirt's top edge (all three vertices held, one on the edge both meshes draw) are drawn by both,
  the skirt's copy 2 mm behind, so the robe's band always covers the join and the skirt's copy closes any gap behind it.
  The skirt's own renderer joins the wearer's level of detail group
  (the game's `VisEquipment.UpdateLodgroup`) and leaves it when the cloth goes.
- **Layers over the skirt** (`RobeClothTies`, once per robe mesh; user, 2026-11-03, Sunweave: the tan over-robe
  "glitching through" the white skirt when running): an over-robe, tunic, lobe band, leaf tabs, tabard, sash or rope tails
  hung over the skirt are particles of their own 0.5 to 6 cm outside the skirt's, and swung and were pushed apart from it,
  so the two passed through each other. Every moving vertex whose line in along its own normal meets a triangle of another
  layer facing the same way, 0.5 to 6 cm under it, is tied to that triangle's corners by three triangles in a submesh after
  the drawn ones: MagicaCloth reads every submesh (distance and bending constraints, a tetrahedron), the renderer has no
  material for it and draws nothing (the skirt's renderer is given only as many of the robe's materials as it draws). No
  tie onto a corner within the reduction distance (1.5 cm) of the vertex or of another layer outside it: the reduction
  merges those, and a tie across a merge turned inside out (the Rootweave's tunic seated: 2 cm through the skirt in every
  frame). Tried and dropped: one tie to the nearest vertex (the Mossweave's tabs lie 6 mm out but 3 cm from any skirt
  vertex), fans, ties both ways, point collision with the tied layer left out of it (legs worse, crossing the same), and
  not drawing the skirt under an over-layer (holes when the layers parted). Ties a robe gets (male): Sunweave 495,
  Mossweave 546, Rimegild 45, Rootweave 18; the Wolfwool and Mistweave have no layer over the skirt. Measured in the cloth
  test (`layers.py`): how far the skirt's outside comes through the layer over it, along its own normal (% of frames over
  1 cm / mean deepest cm, either body, before -> after):

  | Robe | walk | jog | run | sit | on the ground |
  | --- | --- | --- | --- | --- | --- |
  | Sunweave | 56/1.4 -> 0/0 | 73/1.8 -> 0/0 | 81/1.9 -> 16/0.5 | 100/2.5 -> 0/0 | 31/1.2 -> 0/0 |
  | Rootweave | 19/0.6 -> 17/0.5 | 81/2.0 -> 54/1.4 | 92/2.4 -> 70/1.8 | 0/0.3 -> 0/0.3 | 8/0.4 -> 4/0.2 |
  | Mossweave | 100/3.8 -> 72/1.6 | 99/4.9 -> 72/1.4 | 98/4.7 -> 88/2.0 | 100/4.7 -> 100/1.3 | 100/2.3 -> 73/2.2 |
  | Rimegild | 100/3.4 -> 100/2.7 | 59/1.4 -> 32/0.7 | 24/0.6 -> 10/0.3 | 0 -> 0 | 0 -> 0 |

  The Emberweave (lobe band) and Deerhide (sash tails), measured with the ties before the merge check: jog 19/0.6 -> 1/0.0
  and 34/1.2 -> 11/0.3, run 28/0.8 -> 8/0.2 and 29/0.8 -> 26/0.8. The legs, the join (0.0 mm) and standing (0% pokes)
  as before within the runs' spread (walk and jog legs a few points either way). Still off: the Mossweave's tabs and the
  Rimegild's tabard lie within a centimetre of the skirt even at rest, so a little of either shows through standing in
  the test; the Rootweave's tunic keeps only 18 ties (its skirt's top is merged into the tunic's lining) and still crosses
  running.
- **Colliders** (`RobeClothColliders`, `RobeClothWorn`; 2026-11-03): six tapered capsules on the leg bones, sized from
  what the wearer has on: every skinned renderer on the body (the body itself, which trousers paint, leggings, boots, any
  other mod's) read in the bind pose, each vertex given to the thigh, shin or foot of its heaviest bone, below the hips
  line. Per limb the capsule's line runs through the middle of the worn shape in each half of the limb (the box round it
  across the bone) with a round at each end, grown until every worn point is inside, then by 1 cm less the particle
  (2.5 cm): the skirt's outside (the particles' middles) stays 1 cm off the bulkiest of body, trousers and boots. Why
  (user, 2026-11-02, Sunweave with its trousers and boots: "feet/legs are still glitching through"): the old capsules were
  round on the bone lines, thighs 8.5, shins 6, feet 5 cm, but the body's calves stand up to 14 cm off the shin bone, the
  seer boots' fur cuffs 13 cm and their heels 16 cm off the foot line (measured, `worn_radii.py` in the workshop's cloth
  test); standing with a foot back a heel or cuff stood 1-2 cm outside the back of the skirt in every frame. Fitted, a
  Sunweave wearer's are about thighs 10/11, shins 12/12, feet 14/13 cm (top/bottom round). A worn mesh the game will not
  let us read that reaches below the hips adds 1.5 cm to every limb; a limb with nothing worn keeps its old round on its
  bone line. Each round is still cut to the nearest moving skirt vertex beside its half less the particle and 5 mm (to no
  less than 60%). Other leggings put on (the leg item's hash changes) drop the cloth; it is built again round them when the
  wearer next stands still. The log gives one skirt's radii once.
- **Feel** (`RobeClothBuild`; the linen cape's settings first, then): gravity 8 (the cape's 7), gravity falloff 0.5 (no
  sag or jitter at rest), damping 0.2 (the cape's 0.1), particles 2.5 cm (room for the lining and hem, which follow the
  particles a few centimetres off them), wind 0.05, reduction 1.5 cm on every skirt (MagicaCloth takes a share of the
  mesh's longest side, worked out per skirt: the lining, 0.8 cm in, and the hem's rim merge with the outside; the Mountain
  fur's lining, 3.6 cm in, is its own particles, drawn from inside only). It was 3.5 cm until 2026-11-03: that merged the
  held vertices along the skirt's top with their neighbours (their bone weights added, their places averaged), and the
  skirt's top drifted up to 5 cm off the robe's band, the dark "horizontal cuts" the user saw (Rootweave); at 1.5 cm the
  held vertices stay where the robe's own skinning puts them (0.0 mm measured offline on every set), fabric lengths from the build pose (`animationPoseRatio` 0.1: the skinned skirt, which the
  shins pass through, barely pulls it; only the legs widen it), angle restoration 0.3 (x1 at the band, 0.6 at 0.15 deep,
  0.35 at the hem), no angle limit, distance and bending stiffness 1, tether compression 0.4, at most 80 cm off the
  skinned skirt (4 cm at the band, 40 cm at 0.15 deep; 40 cm with a 60 degree angle limit until 2026-11-03, when the
  workshop's cloth test over the eight robes, both bodies, showed walking pokes falling from up to 24% of frames to
  mostly 0-11% (the Rootweave's male 20% either way) and jogging from 37-100% to 30-58%; sneaking better on six robes,
  worse on the Deerhide and the Rimegild's male; running stays at 75-98%, the game's run kicking the heel up behind to
  seat height), edge collision with friction 0.3 (a shin cannot slip between two
  particles), world inertia 0.4 (it trails a running body less, so a stride has less to push), movement limit 4 m/s,
  particle limit 5 m/s (as fast as a swinging foot pushes it), a jump over 1 m or 90 degrees in a frame resets it. The
  legs' push against the limits, measured offline in the workshop's idle, walk, run and stand poses: the legs reach at
  most 7 cm into the skinned skirt, always within the 80 cm limit, which the cloth's solver applies after the colliders.
- **When it runs** (`RobeCloth`, a component on the robe's worn instance, four checks a second, no allocation): players
  only (not armour stands or ragdolls), never without a graphics device (a dedicated server); only while `4. Casters /
  Wands and Spells` is on (off: the cloth and colliders go, the plain skirt is back). Built once per worn robe, when the
  wearer is within 40 m of the camera and has stood still and upright on the ground for two checks (the cloth keeps the
  lengths of the pose it is built in; a remote player running past is built when he stops). Then kept: beyond 40 m the
  game's cloth rests and the plain skinned skirt is drawn (its distance culling), off screen it rests and starts again
  from the skinned skirt. Pinned to the skinned skirt (blend 0) while lying, riding or at a ship's bench or mast
  (`attach_bed`, `_sitship`, `_dragon`, `_mast`, `_lox`, `_asksvin`, `lying_down`) or swimming (`inWater`); reset from
  the skinned skirt when that ends. Seated on a chair, a throne or the ground (`attach_chair`, `_throne`, a state tagged
  `sitting`) the cloth keeps moving and drapes over the knees, where the skinned skirt stood out like a drum (the cloth
  test's `--sit`, 2026-11-03); it is never built while seated. These are animator values the game sends for every player.
- **Tested offline** (2026-11-03): the workshop's cloth test (`ValheimAssets/Assets/Gear/SeerRobes/Tools/clothtest`,
  `run.py`) builds a Windows player of the workshop's Unity project with the game's own MagicaClothV2, Burst, Collections
  and Mathematics DLLs (the editor refuses the game's release Collections in jobs), wears each set's robe and boots (the
  mod's bundles) on the game's player and body, both bodies, with this cloth ported as it is here, plays the game's own
  controller through idle, walk (1.6 m/s), jog (4), run (7), sneak (2) and crouch moving through the world at 60 frames a
  second, and measures every frame how far any leg or boot vertex stands outside the skirt (out from the hips' upright
  line, past the skirt's outermost layer; an opening or the hem is not counted), the held vertices' drift off the robe's
  skinning (the join), and the skirt's jitter and stretch. `table.py` and `sheet.py` give the numbers and a contact sheet.
  Running: the back foot kicks up behind a skirt that reaches the shins (every set but Swamp) and comes out under and
  behind the hem in most frames whatever the settings (stiffer, freer, more distance, 150 Hz, point collision: none
  changed it); the cloth only keeps the shins from cutting through. Measured on the robes with their openings; the closed
  skirts are being made.
- **Fallback**: a robe mesh whose cloth will not build (an exception, or MagicaCloth's build reporting failure) keeps
  its skinned skirt for the session, logged once per mesh.
- Names: the cloth `EE_RobeCloth` (a child of the robe's `attach_skin(Clone)`), the skirt's renderer `EE_RobeSkirt` (a
  child of the robe's renderer), the colliders `EE_RobeClothLeg` under the leg bones; per client, nothing sent or saved.
  The thin bright vertical streaks seen down the front of a skirt in the rain (the screenshot of 2026-11-02 shows the
  wearer Wet) are most likely rain in front of the dark cloth, not the cloth: they run straight down across its folds,
  which no edge of the mesh does. No patch of its own: `RobeBody`'s `VisEquipment.AttachArmor` postfix
  calls `RobeCloth.Watch`. No setting of its own.

## EliteEquipment's boots

With EliteEquipment's `1. Boots / Separate Boots` on, the Deerhide Trousers split like every leggings: `CasterBoots`
registers them with its boots (`BootSets.AddOwn`) as a pair that brings its own
parts; the boots item `EE_Boots_SeerMeadows` ("Deerhide Foot Wraps", PackPanel's feet slot) is made by EliteEquipment
from the trousers' `boots` mesh, the wraps hide on the trousers, stats, weight and recipe go 80/20, the set grows to four
pieces. The trousers' +5 eitr goes 4 / 1 (`CasterGearWorn`, reading EliteEquipment's setting through the chainloader).
The workshop adds the boots icon, the trousers-without-wraps icon and the ankle-cut legs paint (missing: the boots keep a
cut of the trousers' icon and the trousers their whole paint). Without EliteEquipment's split the trousers keep their
wraps.

## Judgement calls (not asked)

- The spell ring: a 0.2 s hold before it opens, no setting; a dim (unaffordable) page let go on is picked and refused by the
  game's eitr check, not shaken; a book's first page at the top and the rest clockwise; the ring keeps HaloMenu's size
  and look; middle mouse casts the last spell.
- The spell bar sits under the hotbar (the hotbar is at the top of the screen, so "above" had no room).
- The healer's highlight (user, 2026-10-09): a friend counts as aimed at inside a 1.5 m bubble round their middle (4 to
  30 degrees, wider up close), the picked one kept until the crosshair leaves a bubble 1.3 times larger; looked for 10
  times a second; the marker (`ee_fx_heal_target`) is seen by the healer only. Lock-on: the friend highlighted as the
  cast starts gets the heal; it flies at 25 m/s (`ee_fx_mendbolt`, shown to players near) and heals where it lands. A
  lock that died or went more than 35 m away, or no friend highlighted, heals the caster (user, 2026-10-11: "allow spell
  to heal yourself if its not targeting an ally"; for a day before that Mend refused to cast without a friend).
- No facing rule (user, 2026-10-09: tried a 45-degree one, then "remove the FOV requirement ... points the wand just
  toward the target no matter the players actual looking position"): any friend in the aim bubble can be picked; the
  cast turns the body to the locked friend at 720 degrees a second until the cast is over (`CastTurn`), the camera left
  alone, the cast's own rotation speed 0 so the game does not turn it back meanwhile.
- The two-chant limit is not built: the Meadows have two chants.
- Ward and Sanctuary are a new kind, Ward ("ward" in features/casters.md): Ward is aimed like a heal (the marker, the lock, the turn), Sanctuary is not.
- Ward: cast again on a warded friend it starts whole (the game's shield keeps the damage it took); no Blood Magic raised as it breaks (the game's raises the wearer's); nothing per world level.
- Sanctuary shelters tames as well as players (the casters' friends), measures 6 m as a sphere, ends when its caster dies or leaves, and its cooldown is a status effect a death clears.
- Feather Fall's 30 s is the chant's duration halved, so a Mossweave wearer's lasts 45 s as every chant gets half again.
- Bolt: 4 eitr, pierce 6 + spirit 6 (+1 each a quality); heal power 15 (+1.5 a quality, +3 with the spellbook, up to
  +50% from Blood Magic); spell eitr up to a third cheaper from Blood Magic, as the game does for staves.

## Still to do from the move

`NearbyCast` (GrindstoneSkills has the same `NearbyRpc`) and `CasterUiCopies` (OpenKeep's `BenchCopies`, since diverged)
are to become shared libraries.

## In-game checklist

- [ ] Recipes at the workbench: wand, spellbook, three robe pieces (no page recipe); names, icons, tooltips (heal power, eitr, the book's pages).
- [ ] Wand in hand: held right, on the back when sheathed, on the ground, on an item stand; each spellbook in the left hand (the wood shield's look, with a warning, until its `ee_book_<tier>` is embedded).
- [ ] The three books: Leatherbound at the workbench, Barkbound at the forge (level 2, beside the banded shield), Cinderbound at the galdr table (level 2, beside Dundr); tooltips: block 2/4/8, eitr regen +10/+15/+25%, heal power +3/+6/+12, "Pages (n/2)", "(n/4)", "(n/8)"; a heal with each book is stronger by its own number.
- [ ] Room: a 3rd page refused in a Leatherbound ("full (2 / 2 pages)"); an old Leatherbound holding 4 shows "4 / 2 pages", casts all four, refuses a 5th and a page after taking one out (3 / 2), takes one again at 1 / 2.
- [ ] Spellbook held open: the left forearm forward, elbow about 90 degrees, palm up and open in front-left, the book open on it (each book its own `ee_book_<tier>_open`; its closed one laid flat, with a warning, until that is embedded; a book whose bundle is not in at all stays in the shield hold); closed on the back, the ground, an item stand and in the icon; a second player sees it the same; blends (no snap) into and out of swimming, a chair, a bed, the ship's controls, an emote, a dodge, blocking and the hammer; held through walking, running, jumping and wand casts; switch off: the old shield hold.
- [ ] Eitr bar appears with the wand (10) and each robe piece (+5); goes down when they come off.
- [ ] Spell motions: Mend reaches out (gold motes at the tip until the bolt leaves at full reach), Hearthsong lifts the wand high and sweeps it down (embers at the feet, then the firelight ring to 10 m), Stag's Swiftness sweeps low to high (wind at the feet, then the gust ring and the wisp dashing ahead), Elder's Volley thrusts and recoils once per seed (six seeds on the recoils, a green burst at the tip each); the book stays open on the palm; a second player sees the same; a bolt right after a spell and a spell right after a bolt play their own motions (no early cast); the layers do not reset when the slot changes.
- [ ] Bolt charges in the wand's stone: Gnarled amber, Greywood mossy green with stone chips and a stone growing at the tip, Frostsilver ice, Rootbound earth, Blackmetal white; the full glow, flash and stream white; with a book held the stream rises from its pages.
- [ ] Mend: heals the highlighted friend (second player, a tame), or yourself with no friend highlighted; costs 8 (6 with the set).
- [ ] Hearthsong and Stag's Swiftness: everyone within 10 m gets the status (icon, 60 s), a second player too.
- [ ] Left mouse: the bolt flies, hits, costs eitr; spirit hurts skeletons more. Middle mouse casts the last spell.
- [ ] Rootbound Wand: a tap raises one root where aimed, a full charge five spread round it; each rises, lashes and hits enemies only (never the caster, a second player or a tame), 4 eitr a cast, Elemental Magic raised; a second player and a dedicated server see the same roots; the other wands still fire the bolt.
- [ ] Frostsilver Wand: a tap fires one ice shard straight at the aim, a full charge five in an even fan; each the Staff of Frost's ice sliver look and hit burst, slows what it hits, hits enemies only (never the caster, a second player or a tame), 4 eitr a cast, Elemental Magic raised; a second player and a dedicated server see the same shards; the other wands still fire the bolt.
- [ ] Greywood Wand: charging shows a mossy green glow with stone chips at the tip and a stone in it growing to the size the release throws (a second player sees the same), gone on release or a broken charge; a tap throws one stone straight at the crosshair (no arc) with a dark green trail; a full charge throws a bigger one that bursts 5 m out (green burst, rock crack) into 15 small stones flying on in a tight cone, each with a trail, the middle one on the crosshair; a rock hitting within 5 m hits whole; enemies only, 4 eitr a cast, Elemental Magic per hit; a second player sees the rock, the burst and the pieces; the dedicated server logs nothing.
- [ ] Ashwood Wand: crafted at the galdr table beside the Staff of Embers (Blackwood 6, Flametal 3, Celestial feather 2), the club's look until its model is in; a tap fires a small fireball (half the staff's) straight at the aim, fire 27 + spirit 27, sets burning, a small burst and no area; held, it grows (white charge at the tip); from 2.7 s it explodes where it lands in the staff's explosion, hitting enemies within 2 m (never the caster, a second player without PvP, a tame or a building); Elemental Magic rises; a second player sees the fireball, its size and its explosion; dedicated server fine.
- [ ] Cleanse: on the ring at 12 eitr, the Spellbook says Heal; poisoned, burning, frosted or tarred, cast at a marked friend (second player, a tame) or yourself: the effects go (the HUD icons with them), rested, food, meads and chants stay, a small heal; Mend's reach motion, the pale charge at the tip, the splash on the target at its feet, seen by a second player; works on a dedicated server; its page drops in the Swamp and later biomes, never in the Meadows or Black Forest.
- [ ] Ward: on the ring at 20 eitr, the Spellbook says Ward; the gold marker shows with only a Ward page in the book; cast at a marked friend (second player, a tame) or yourself: the shield's icon, hits absorbed until twice the heal power is taken, then the game's break effect; gone at 20 s; cast again it is whole; the bubble on the target seen by a second player and gone as it breaks; works on a dedicated server; its page drops in the Mountains and later biomes.
- [ ] Sanctuary: The Queen drops Page: Sanctuary per player; 40 eitr; the dome at the feet for 10 s, seen by a second player; a friend inside takes 30% less from a hit (compare inside and out), the icon gone about 1 s after stepping out; the cooldown's icon counts 2 min and a cast before then is refused with the message and no eitr spent; works on a dedicated server.
- [ ] War Chant: everyone within 10 m, the caster too, gets its icon for 60 s (90 s in Mossweave); a weapon's hit on a training dummy 10% higher; the ring at the feet.
- [ ] Feather Fall: everyone within 10 m gets its icon for 30 s (45 s in Mossweave); a jump from a cliff falls slowly and hurts nothing; the feathers on each body, seen by a second player.
- [ ] Elder's Volley: in the book it is on the ring at 20 eitr (less with Elemental Magic) and the Spellbook says Strike; cast, six seeds fly from the wand's tip toward the crosshair 0.1 s apart, a little spread, the Elder's look and hit burst; each hurts enemies with 60% of the wand's bolt damage in the wand's types (the Rootbound blunt, the Frostsilver frost), never the caster, a second player or a tame, Elemental Magic raised; dying, drawing another wand, putting the book away or swimming mid-volley stops the rest; a second player and a dedicated server see the same seeds.
- [ ] Moder's Breath: Moder drops Page: Moder's Breath per player; in the book it is on the ring at 25 eitr (less with Elemental Magic); cast, the thrust breathes six bursts of Moder's frost from the wand's tip toward the crosshair, about 7 m long and 30 degrees wide, with Moder's breath sound once and no screen shake; enemies in the cone take frost and spirit (half the bolt each burst) and slow, never through a wall, never the caster, a second player without PvP or a tame; Elemental Magic raised; a second player and a dedicated server see the same breath.
- [ ] Click: one bolt per click; quick clicks run the combo 1, 2, 3 like a sword; a pause starts it again at 1.
- [ ] Left mouse held: the cast turns into the charge pose of the user's reference (wand forward, other hand open below it), white energy streaming from the other hand into the wand until full charge at 3 s; the white spell on the tip grows, gets its glow and flash at full; walking slowly and turning to aim; let go, the charged release and a bigger bolt (seen the same by a second player).
- [ ] Dodge with a wand: a blink, the roll's stamina and half that in eitr spent, no hit taken from a swing during the blink and 0.5 s after; with too little of either its bar flashes and nothing happens.
- [ ] Right mouse held 0.2 s opens the ring of the book's pages (icon, name, cost; unaffordable ones dim); the direction picks, let go casts it and the RMB slot shows it; centre or Esc casts nothing; the camera stays still and walking works while open; a tap casts the last spell again; right mouse held + jump blinks and casts nothing; left mouse alone charges and fires the bolt; Mend picked on the ring heals the friend marked as it opened. With HaloMenu installed its ring and this one never show together.
- [ ] Spellbook: the button shows with a book and the inventory open (no wand needed); the window opens in the crafting panel's place with nothing of that panel showing, and the panel comes back as it was on closing (the button, the inventory closed, Esc), also at a crafting station; header, pages (icon, name, cost; the picked one lit) and the picked page on the right (kind, cost, description, Take out) line up; take out puts the page in the inventory (refused when full); fits at another resolution and UI scale, and in Russian; with PackPanel the copies are skinned.
- [ ] Without a book: the bolt fires, holding right mouse opens no ring and says so once; a last spell whose page was taken out shows greyed and a tap refuses with a message. A wand saved with the old two slots shows its first slot's spell.
- [ ] Using Page: Hearthsong while holding the book puts it in (message), again says already in; a fifth page says full; without a book it says to hold one.
- [ ] Eikthyr drops Page: Stag's Swiftness and the Elder Page: Elder's Volley per player; a 2-star creature, a dungeon chest and plain kills sometimes give Page: Hearthsong (a second player sees the same drop).
- [ ] A character with the old `EliteEquipment.spells` record gets its pages once.
- [ ] Effects seen by a second player: cast flash, mend motes, chant ring, bolt and its hit.
- [ ] Robe skirts as cloth (each of the 8 robes, a man and a woman): the log's `as cloth:` and `leg colliders` lines
  once per robe mesh, no warning; stand still a moment, then walk, run, crouch, stand with a foot back, jump, walk up and
  down a slope: the hem swings heavily and settles, no shin or foot through the back, no flutter, no jitter at rest, the
  bodice, sleeves and sash as before; no part of the skirt held at a hand or sleeve, with a wand and without, also when
  the cloth was built with a hand by the hip; no gap or seam where the skirt meets the band above it. Sit in a chair and on the ground: the skirt drapes over the knees as cloth, no
  twitching; on a ship's bench, in bed, ride, swim: the plain skirt, then cloth again. A second player in a robe the same; beyond 40 m the plain skirt. Switch off: the plain skirt at once.
- [ ] With EliteEquipment's Separate Boots on: Deerhide Foot Wraps craftable beside the trousers, worn in the feet slot,
  the wraps gone from the trousers, eitr 4 + 1, set of four; off: the trousers keep their wraps.
- [ ] Eir's Spring aimed at the ground 10 m away: the ring there for 8 s, a hurt friend (a player, a wolf) standing in it
  healed once a second (the game's green numbers), one outside not; aimed at the sky: the ring at your feet. A second
  player sees the ring; a friend on a dedicated server is healed.
- [ ] Chain Mend on a marked friend with three hurt friends in a line 5 m apart: four heals 0.15 s apart, each smaller,
  an arc between each pair and the gold motes on each; a full-health friend skipped; a friend 9 m on not reached.
- [ ] Second Wind after sprinting, a second player near you also drained: both get 50 stamina at once; the ring at your feet.
- [ ] Revive: a second player dies; aim at their grave within 3 min: they are moved to the grave (from the death screen
  or after respawning) with half health, both told; the pillar at the grave and on them; the cooldown icon on you for
  5 min, a second cast refused. Your own grave, a grave older than 3 min, an empty aim: refused with the message and no
  eitr spent. Page: Revive only ever in dungeon chests.
- [ ] The switch off (the default): no recipes, no pages drop, pages do nothing, the wand neither casts nor fires,
  no eitr from gear, no spell bar or Spellbook.

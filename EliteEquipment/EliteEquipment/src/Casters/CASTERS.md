# Casters (EliteEquipment section 4)

The Meadows tier of the healer and caster line designed in `EliteEquipment/features/casters.md` (the design, the
decisions and the build contract with every name). Asked for 2026-10-09: "everything will be custom made, I want to
start with medows first completely". Built as an AssetLab bench (section 3 there) and moved into EliteEquipment on
2026-10-12 as section 4.

## What is in it

| Item | Prefab | Copy of | Model (bundle) |
| --- | --- | --- | --- |
| Gnarled Wand | `EE_Wand_Meadows` | `Club` | `ee_wand_meadows` |
| Greywood Wand (Black Forest; heal power 25, +15 eitr, bolt pierce 7 + spirit 7; forge beside the bronze mace: Fine wood 4, Bronze 2, Greydwarf eye 6; model by ChatGPT 2026-10-11) | `EE_Wand_BlackForest` | `Club` | `ee_wand_blackforest` |
| Raven Fetish | `EE_Focus_Meadows` | `ShieldWood` | `ee_focus_meadows` |
| Deerhide Hood, Robe, Trousers | `EE_Seer_Meadows_Hood`, `_Robe`, `_Trousers` | `HelmetMage`, `ArmorMageChest`, `ArmorLeatherLegs` (recipes beside the leather set's) | `ee_seer_meadows` v2, the long seer's robes (`RobeLook`, `RobeBody`) |
| Spell: Hearthsong, Spell: Stag's Swiftness | `EE_Spell_Hearthsong`, `EE_Spell_Swiftness` | `Raspberry` | `ee_spells_meadows` (`ee_spell_scroll`) |
| The wand's bolt | `EE_WandBolt` | `staff_iceshard_projectile` | `ee_casterfx_meadows` (`ee_fx_bolt`) |
| Dandelion Mash (eitr food: 12 / 6 / 35, 25 min, workbench: Dandelion 3, Honey 1, Mushroom 1) | `EE_Food_DandelionMash` | `CarrotSoup` (steam removed) | `ee_food_meadows` (`ee_food_dandelionmash`) |
| Candied Thistle (15 / 15 / 15, 15 min, makes 2), Glowcap Broth (15 / 8 / 40, 25 min), Seer's Porridge (15 / 8 / 45, 30 min); cauldron level 1 | `EE_Food_CandiedThistle`, `_GlowcapBroth`, `_SeersPorridge` | `BoarJerky`, `CarrotSoup`, `TurnipStew` | `ee_food_blackforest` (made 2026-10-11 by a Sonnet agent) |
| Turnip Crisps, Blood Draught, Mire Stew (Swamp, cauldron beside Turnip Stew); Glacier Tea, Wolfheart Stew (Mountains, beside Onion Soup); Candied Cloudberries, Lox Marrow Broth, Sunbarley Porridge (Plains, beside Blood Pudding): the features table's numbers | `EE_Food_<Name>` | snacks `BoarJerky`, drinks and broths `CarrotSoup`, stews and porridge `TurnipStew` | `ee_food_swamp`, `ee_food_mountains`, `ee_food_plains` (models by ChatGPT, bundled 2026-10-11) |

Status effects `EE_SE_Hearthsong`, `EE_SE_Swiftness`, `EE_SE_SeerMeadowsSet`. Words `$ee_wand_*`, `$ee_focus_*`,
`$ee_seer_*`, `$ee_spell*`, `$ee_food_*`, all in `translations/English.yml` (the mod's CLAUDE.md, "Words"). Player custom data `EliteEquipment.spells` (known spells); item custom data
`EliteEquipment.loadout` on each wand (two slots). Routed RPC `EliteEquipment_CasterFx` (effects to
the players near). Setting `4. Casters` / `Wands and Spells`, **off by default** (user, 2026-10-09): to test, set
`Wands and Spells = true` under `[4. Casters]` in `milkyteam.eliteequipment.cfg` (it hot reloads; on in LocalTesting).
Off: no recipes, no Eikthyr drop, scrolls teach nothing, wands neither cast nor fire the bolt, no gear eitr, no spell
bar, Spellbook, highlight or tooltip lines; items already made stay.

## Code map

- `CastersModule` (Initialize, Install), `CastersSettings`, `CasterWords` (the slot buttons' words), `CasterSafe` (log
  once, never throw).
- Data: `Spell`, `Spells` (Mend, Hearthsong, Stag's Swiftness), `CasterGear` (items, numbers, recipes).
- Food: `CasterFood` (data), `FoodItems` (the dish copy).
- Prefabs: `CasterItems` (ZNetScene.Awake / ObjectDB.Awake / CopyOtherDB prefixes; scene postfix: bolt look, Eikthyr
  drop), `CasterBundles`, `HeldModel` (wand and focus in the fist: model length +Y turned 90 degrees about X onto the
  attach's +Z, grip on the attach point), `WandItem` (both attacks on the Staff of Embers' `staff_fireball` cast, the
  one cast motion that plays on a one-handed weapon), `FocusItem`, `RobeItems` + `RobeLook`, `SpellItems`, `WandBolt`,
  `CasterItemLook`, `CasterStatus`, `CasterRecipes`, `EikthyrDrop`.
- Casting: `SpellCast` (StartAttack prefix fixes the spell; `Attack.DoNonAttack` postfix casts it), `SpellCost`
  (`Attack.GetAttackEitr` postfix: the bolt costs no eitr, the cast its spell's), `FriendTarget`, `HealPower`,
  `HealTarget` (the highlight and the lock), `HealFlight` (the heal homing on the friend), `CasterGearWorn`, `GearEitr` (`Player.GetTotalFoodValue` postfix), `KnownSpells`, `Loadout`, `SpellLearning`
  (`Player.ConsumeItem` prefix).
- Effects: `CasterFx` (routed RPC, local copies; the cast flash and chant ring turned to each spell's colour, the
  workshop's hue numbers), `BoltHit` (`Projectile.OnHit` postfix on the owner), `NearbyCast`, `CasterSounds` (game clips).
- Interface: `CasterHud` (Hud.Update postfix), `CasterControls` (`Player.SetControls`: the mouse buttons below),
  `CasterBlink`, `WindUp` + `ChargeState` + `ChargeOrb` + `WandAnimations` + `ChargeFeet` + `TipSpawn` + `BoltSize`, `SpellBar` (the two slots under the hotbar, each with its button: RMB, RMB+LMB), `Spellbook` + `SpellbookUi` + `CasterUiCopies`, `CasterTooltip`.

Patches checked offline against the game's DLLs (15 classes, 0 problems, 2026-10-09). All five bundles embedded
(2026-10-09). The robes follow the workshop BRIEF's runtime steps: a robe mesh per body (`RobeBody`, `AttachArmor`
postfix), body paint on copies of the leather armour materials, the hood a hood (hair HiddenHood, beards shown).

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
times its size with its hit width (`BoltSize`; the size shared by the game's initial scale sync). A wand holds two spells (user, 2026-10-11: "2 extra spells only"): right mouse casts the first slot when let go;
right mouse held + left mouse casts the second at once, and the release then casts nothing; right mouse held + jump
dodges as the game's block-dodge does, and casts nothing either. Middle mouse (the game's secondary attack) casts the
first slot. Casters do not block (the block button never blocks with a wand). No spell picking: until 2026-10-11 a wand
held three and Left Alt + wheel picked one.
A dodge with a wand drawn is a blink (`CasterBlink`): up to 5 m along the dodge (back when standing), stopped short of
anything solid, set on the ground there, the dodge's own conditions, a flash at both ends, 0.8 s cooldown. It costs the
roll's stamina and half that in eitr (user, 2026-10-11: "as much stamina as dodging, and 1/2 that value of eitr"), and from the blink until 0.5 s after
nothing a roll dodges hits the caster ("when you cast to .5 seconds after you blink you are invulnerable": the game's own
dodge flag held up, on the owner and in the ZDO for the attackers' machines).

Robe armour follows the game's mage sets (half the biome's heavy armour, +2 per level; `CasterGear.ArmourPerLevel`): the
Meadows pieces 1 armour, +2 a level, no movement cost (leather has none). The eitr foods stay in EliteEquipment (user,
2026-10-11).

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

- The Spellbook assigns by click (slot, then spell; the picked slot clicked again empties it), not by drag.
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
- Bolt: 4 eitr, pierce 6 + spirit 6 (+1 each a quality); heal power 15 (+1.5 a quality, +3 with the fetish, up to
  +50% from Blood Magic); spell eitr up to a third cheaper from Blood Magic, as the game does for staves.

## Still to do from the move

`NearbyCast` (GrindstoneSkills has the same `NearbyRpc`) and `CasterUiCopies` (OpenKeep's `BenchCopies`, since diverged)
are to become shared libraries.

## In-game checklist

- [ ] Recipes at the workbench: wand, fetish, three robe pieces, Spell: Hearthsong; names, icons, tooltips (heal power, eitr).
- [ ] Wand in hand: held right, on the back when sheathed, on the ground, on an item stand; the fetish in the left hand.
- [ ] Eitr bar appears with the wand (10) and each robe piece (+5); goes down when they come off.
- [ ] Mend: heals the highlighted friend (second player, a tame), or yourself with no friend highlighted; costs 8 (6 with the set).
- [ ] Hearthsong and Stag's Swiftness: everyone within 10 m gets the status (icon, 60 s), a second player too.
- [ ] Left mouse: the bolt flies, hits, costs eitr; spirit hurts skeletons more. Middle mouse casts slot 1.
- [ ] Click: one bolt per click; quick clicks run the combo 1, 2, 3 like a sword; a pause starts it again at 1.
- [ ] Left mouse held: the cast turns into the charge pose of the user's reference (wand forward, other hand open below it), white energy streaming from the other hand into the wand until full charge at 3 s; the white spell on the tip grows, gets its glow and flash at full; walking slowly and turning to aim; let go, the charged release and a bigger bolt (seen the same by a second player).
- [ ] Dodge with a wand: a blink, the roll's stamina and half that in eitr spent, no hit taken from a swing during the blink and 0.5 s after; with too little of either its bar flashes and nothing happens.
- [ ] Right mouse casts slot 1 on release; right mouse held + left mouse casts slot 2 (no slot 1 after); right mouse held + jump blinks and casts nothing; left mouse alone charges and fires the bolt.
- [ ] Spellbook: the button shows with a wand and the inventory open; slots and spells assign; closes with the inventory.
- [ ] Using Spell: Hearthsong teaches it (message), a second use says already known; Eikthyr drops Stag's Swiftness.
- [ ] Effects seen by a second player: cast flash, mend motes, chant ring, bolt and its hit.
- [ ] With EliteEquipment's Separate Boots on: Deerhide Foot Wraps craftable beside the trousers, worn in the feet slot,
  the wraps gone from the trousers, eitr 4 + 1, set of four; off: the trousers keep their wraps.
- [ ] The switch off (the default): no recipes, no Eikthyr drop, scrolls do nothing, the wand neither casts nor fires,
  no eitr from gear, no spell bar or Spellbook.

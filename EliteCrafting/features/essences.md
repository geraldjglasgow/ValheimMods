# EliteCrafting - specification: Essences

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers **essences**: per-biome stones that reroll a magic item and guarantee that one of the new affixes
comes from the biome's **family**. What the affixes themselves do is `affixes.md`; how an affix is drawn (tier
window, two-stage draw, exclusion) is `rarity.md` section 4; the stone pipeline an essence goes through is
`applying-stones.md`; the stone YAML schema it extends is `economy-yaml.md` section 4; where stones drop is
`drops.md`.

Numbers are defaults and all of them are configurable in `EliteCrafting_economy*.yml`. **Almost every number and
every family list here is a judgement call**, made at a desk; they are collected in `../DECISIONS.md` ESS-1 to ESS-17.

**Status: built 2026-09-24, not tested in game.** Ships in **Phase 2** (0.2.0). The schema additions (section 11) are
in `economy-yaml.md` sections 4, 9 and 10. Code: `Stones/ImbueVerb`, `Rolling/ImbueOps` (`ItemRoller.Imbue`),
`Rules/Parsing/EssenceFamilyParser` and `EssenceMemberChecks`, prefabs and tints in `Items/`. The tooltip line of
section 12 is part of the essence's item description (DECISIONS.md IMP-102).

---

# 1. What the player sees

A Swamp draugr drops a **Lesser Venom Essence**, a small yellow-green shard. Clicked onto a Rare sword, it reshapes
the sword like a Stone of Upheaval - every affix but a bound one is rolled again - except that one of the new affixes
is **always** of the Venom family: on a sword, Venombrand or Blood Drinker. A **Greater** essence does the same and
rolls that guaranteed affix at the highest tier the item allows.

There is one family per biome, named for what the biome does to a player: the Meadows' storm (Eikthyr), the Black
Forest's grove, the Swamp's venom, the Mountain's frost, the Plains' battle, the Mistlands' seidr, the Ashlands'
ember, and the Ocean's tide. Each biome's creatures drop its own essences, and each biome's boss always drops a
Lesser one and sometimes a Greater, so an Ashlands player has a reason to go back to the Swamp.

---

# 2. The families

A family is a list of affix ids. Eight ship: one per land biome tier 1-7 (`drops.md` section 3), plus the Ocean.
The biome's tier is where its essences drop (section 10); the family's content is its theme. Nothing in the family
itself depends on the biome.

**Design rules** the default lists follow (and an owner's lists should, but need not):

1. **Themed, tight.** An element family holds its element's brand, ward and bulwark, plus two or three affixes about
   living in that biome. A broad family would make the guarantee meaningless.
2. **Every family covers weapons** (`melee_weapon` and `ranged_weapon`) and at least three armor slots.
3. **No build-changing affix is ever the only member for a slot.** Affixes of weight 30 or less (`affixes.md`
   section 1, "Weights") may sit in a family only beside a heavier member for every slot they roll on - otherwise the
   essence would *guarantee* what the catalog made rare by design. So `thors_arc`, `twincast`, `volley`,
   `gungnir_path` and the other build-changers are in no family.
4. **No Mythic-only affix** (`mythic_only: true`) is ever a member (validation, section 11).
5. **No affix in two families** by default. The schema allows it; the defaults avoid it so each essence reads clearly.

Columns: slot abbreviations as `affixes.md` section 2 (melee, ranged, magic, shield, head, chest, legs, cape,
utility, tool); **cat** = category; **w** = affix weight (the chance of the guaranteed draw picking it, relative to
the family's other eligible members for the slot); **P** = the phase its effect is built in. A member whose effect is
not registered yet is skipped exactly like a disabled affix (section 4, step 3), so a family grows as Phase 2's
effects land.

## `storm` - Meadows (tier 1): Eikthyr's lightning and the stag's speed

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `stormbrand` | melee, ranged | offense | `brand_elemental` | 100 | 1 |
| `stormward` | head, cape | defense | - | 100 | 2 |
| `bulwark_lightning` | cape (gate T3) | defense | `elemental_bulwark` | 30 | 2 |
| `fleetfoot` | legs | utility | `move_speed` | 100 | 1 |
| `stride` | legs | utility | `move_speed` | 100 | 2 |
| `spring_heeled` | legs | utility | - | 100 | 1 |
| `light_leap` | legs | utility | - | 100 | 1 |
| `nimble` | legs, cape | defense | - | 100 | 1 |
| `perfect_guard` | shield | defense | - | 100 | 1 |
| `keen_guard` | shield | defense | - | 60 | 2 |

## `grove` - Black Forest (tier 2): troll blood, root and bark

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `bonebreaker` | melee, ranged | offense | `brand_physical` | 100 | 1 |
| `heartwood` | melee (axes) | utility | - | 70 | 2 |
| `woodcutting_mastery` | melee (axes) | utility | `skill_level` | 100 | 1 |
| `troll_blood` | chest, legs | defense | `health_recovery` | 100 | 1 |
| `mending` | head, chest | defense | - | 100 | 2 |
| `bramblehide` | chest | defense | - | 70 | 2 |
| `ironroot` | legs | defense | - | 100 | 2 |
| `repelling_guard` | shield | defense | - | 100 | 1 |
| `anchored_guard` | shield | defense | - | 30 | 2 |
| `harvester` | utility | utility | - | 100 | 2 |
| `farming_mastery` | tool (cultivator) | utility | `skill_level` | 60 | 1 |

## `venom` - Swamp (tier 3): poison, leeches and rain

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `venombrand` | melee, ranged | offense | `brand_elemental` | 100 | 1 |
| `blood_drinker` | melee, ranged | offense | `leech_health` | 60 | 2 |
| `blood_thrift` | magic (Blood Magic) | offense | `attack_cost_health` | 100 | 2 |
| `venomward` | legs, cape | defense | - | 100 | 2 |
| `bulwark_poison` | cape (gate T3) | defense | `elemental_bulwark` | 30 | 2 |
| `purity` | head, chest | defense | - | 70 | 2 |
| `marshstrider` | legs | utility | - | 60 | 2 |
| `oilskin` | head, cape (gate T2) | utility | `wet` | 40 | 2 |

## `frost` - Mountain (tier 4): cold, cliffs and long climbs

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `rimebrand` | melee, ranged | offense | `brand_elemental` | 100 | 1 |
| `frostward` | head, cape | defense | - | 100 | 2 |
| `bulwark_frost` | cape (gate T3) | defense | `elemental_bulwark` | 30 | 2 |
| `coldblood` | legs, cape | defense | - | 70 | 2 |
| `winterborn` | chest, cape (gate T5) | defense | - | 25 | 2 |
| `mountain_goat` | legs | utility | - | 60 | 2 |
| `long_wind` | legs | utility | - | 100 | 1 |
| `endurance` | chest, legs | defense | - | 100 | 1 |
| `soft_landing` | legs, cape | defense | `fall` | 100 | 1 |
| `ravens_glide` | cape (gate T4) | utility | `fall` | 30 | 2 |

`endurance` is in the family for rule 3 as much as for the climb: without it `winterborn` (weight 25) would be the
only chest member, and a frost essence on a chest piece would guarantee freeze immunity.

## `battle` - Plains (tier 5): Fuling warbands, deathsquitos and the long fight

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `honed_might` | melee, ranged | offense | `damage_physical` | 100 | 1 |
| `keen_edge` | melee, ranged | offense | `brand_physical` | 100 | 1 |
| `needlepoint` | melee, ranged | offense | `brand_physical` | 100 | 1 |
| `berserkergang` | melee, ranged, magic | offense | `damage_critical` | 60 | 2 |
| `padded` | chest, legs | defense | - | 100 | 2 |
| `mailed` | chest, legs | defense | - | 100 | 2 |
| `riveted` | chest, legs | defense | - | 100 | 2 |
| `ironclad` | chest | defense | - | 60 | 2 |
| `arrowward` | chest | defense | - | 70 | 2 |
| `valhallas_edge` | head | defense | - | 50 | 2 |
| `stalwart` | shield | defense | - | 100 | 1 |
| `tireless_guard` | shield | defense | - | 100 | 1 |
| `broad_back` | chest, utility | utility | - | 100 | 1 |
| `trophy_taker` | utility | utility | - | 70 | 2 |

## `seidr` - Mistlands (tier 6): eitr, the Dvergr's runes and the mist

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `primal_fury` | melee, ranged, magic | offense | `damage_elemental` | 100 | 1 |
| `seidr_siphon` | melee, magic | offense | - | 60 | 2 |
| `rune_edge` | melee | offense | `attack_cost` | 30 | 2 |
| `seidr_thrift` | magic | offense | `attack_cost` | 100 | 1 |
| `soul_reaper` | magic | offense | `on_kill` | 100 | 2 |
| `elemental_mastery` | magic (Elemental Magic) | offense | `skill_level` | 100 | 1 |
| `wellspring` | head, cape (T5+) | defense | `max_eitr` | 100 | 1 |
| `seidr_flow` | head, cape (T5+) | defense | `eitr_recovery` | 100 | 1 |
| `restless_mind` | head (T5+) | defense | `eitr_recovery` | 60 | 2 |
| `runic_ward` | chest | defense | - | 70 | 2 |
| `mist_veil` | legs, cape | defense | `avoid_hit` | 60 | 2 |
| `mistbane` | head, utility (T6+) | utility | - | 70 | 2 |
| `seidr_riposte` | shield | defense | - | 60 | 2 |

## `ember` - Ashlands (tier 7): fire, heat and the last light

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `emberbrand` | melee, ranged | offense | `brand_elemental` | 100 | 1 |
| `flameward` | chest, cape | defense | - | 100 | 2 |
| `bulwark_fire` | cape (gate T3) | defense | `elemental_bulwark` | 30 | 2 |
| `ashen_skin` | chest, cape (T5+) | defense | - | 100 | 2 |
| `emberheart` | chest, cape (gate T2) | defense | - | 40 | 2 |
| `hearthlight` | head | utility | - | 40 | 2 |

`hearthlight` (weight 40) is the only head member: above rule 3's threshold of 30, so allowed. A Greater Ember
Essence on a helmet therefore always gives Hearthlight - a harmless light, judged fine (ESS-2).

## `tide` - Ocean (no tier of its own): serpents, sails and salt water

| affix | slots | cat | group | w | P |
|---|---|---|---|---|---|
| `slayer_sea` | melee, ranged, magic | offense | `slayer` | 100 | 2 |
| `sealegs` | chest (gate T3) | utility | `wet` | 40 | 2 |
| `strong_swimmer` | cape | utility | - | 70 | 2 |
| `fair_winds` | cape, utility | utility | - | 70 | 2 |
| `huginns_eye` | head, utility | utility | - | 100 | 1 |
| `fishing_mastery` | tool (fishing rod) | utility | `skill_level` | 100 | 1 |

## Coverage

`x` = at least one default member rolls on that slot (before tier gates and `requires`); `(P2)` = only through a
Phase 2 effect, so empty until that effect is built; `-` = the family has nothing for that slot, and an essence of it
is refused there (`essence_wrong_item`, section 6).

| family | melee | ranged | magic | shield | head | chest | legs | cape | utility | tool |
|---|---|---|---|---|---|---|---|---|---|---|
| storm | x | x | - | x | (P2) | - | x | x | - | - |
| grove | x | x | - | x | (P2) | x | x | - | (P2) | x |
| venom | x | x | (P2) | - | (P2) | (P2) | (P2) | (P2) | - | - |
| frost | x | x | - | - | (P2) | x | x | x | - | - |
| battle | x | x | (P2) | x | (P2) | x | (P2) | - | x | - |
| seidr | x | x | x | (P2) | x | (P2) | (P2) | x | (P2) | - |
| ember | x | x | - | - | (P2) | (P2) | - | (P2) | - | - |
| tide | (P2) | (P2) | (P2) | - | x | (P2) | - | (P2) | x | x |

Every family has something for melee and ranged weapons and at least three armor slots once Phase 2's effects are
built. Magic weapons, tools and shields are thin, as they are in the whole catalog (`../DECISIONS.md` AFX-4). In a
Phase-1-effects-only build every family but Tide already has a live weapon member, which is what makes essences
testable before the rest of Phase 2's effects land.

**Deep North** (tier 8, reserved) has no family and no essence prefabs. When tier 8 ships, its family needs two new
built-in prefabs in that release (section 9) - an owner cannot add a biome family with new prefabs, only with the
reserved pool.

---

# 3. Grades

| | Lesser | Greater |
|---|---|---|
| `applies_to` | uncommon, rare | uncommon, rare, epic, legendary, mythic |
| `tier_floor` | none | 7 |
| Guaranteed affix tier | anywhere in the item's normal window | **the item's ceiling tier** |
| Cost | 1 | 1 |
| Scale (world model) | x0.85 | x1.15 |

- **The grades overlap on purpose.** The manipulation stones split their grades by rarity (Lesser Uncommon/Rare,
  Greater Epic and up, `stones.md` section 5) so each rarity has exactly one grade; essences follow PLAN.md instead -
  "reroll an Uncommon+ item ... Greater essences carry a `tier_floor`" - so a Greater essence is strictly the better
  stone and works wherever a Lesser one does. The Lesser grade still stops at Rare, so a cheap, farmable essence is
  not a Greater Stone of Upheaval for Legendaries (ESS-3).
- **`tier_floor: 7` means "at the ceiling".** The floor is clamped down to the item's ceiling (`item-tier.md` section
  6, `RollMath.EffectiveFloor`), so 7 rolls the guaranteed affix at exactly the highest tier the item allows, on
  every item: a Greater essence on a Swamp sword rolls it at tier 3, on an Ashlands sword at tier 7. This is why the
  biome of the essence does not limit its strength (ESS-4).
- **The floor applies to the guaranteed affix only.** For verb `imbue`, `tier_floor` is read for the family draw; the
  other rerolled affixes use the ordinary window with no floor. Every other verb keeps the field's meaning in
  `economy-yaml.md` ("lowest affix tier this stone rolls").
- A family member that stopped scaling below the ceiling (an affix whose top tier is lower than the item's ceiling)
  is eligible for a Lesser essence at its highest tier (`rarity.md` section 4), but **not** for a Greater one, because
  that tier is below the clamped floor. No default family member stops early, so this matters only for owners'
  affixes.

---

# 4. Behaviour (verb `imbue`)

An essence is a stone with verb `imbue`: it goes through every step of `applying-stones.md` section 2, and the verb
is "Upheaval with one guaranteed affix".

1. **Checks** 1-10 as for every stone (own inventory, magic base, format, enabled, `slots` filter, not sealed,
   equipped setting, known rarity, `applies_to`, cost).
2. **Verb precondition** (step 11), known before rolling:
   - **The family has something for this item.** At least one member is defined, enabled, has a registered effect,
     is not `mythic_only`, lists the item's slot and passes its `requires` (`affixes.md` section 1) - ignoring tiers
     and exclusion, which depend on the roll. Otherwise `essence_wrong_item`.
   - **There is room for the guarantee.** The **kept set** is the bound affix, plus the protected affix when a
     Sigil of Preservation is pending (`sigils.md` section 3). If `kept + 1` exceeds the rarity's maximum, refused
     with `affixes_full`. (Only possible with custom ladders, or an Uncommon with a bound and a preserved affix.)
3. **Dry run** on a copy of the item's record (step 12):
   1. Remove every affix not in the kept set - dormant ones included, as Upheaval does (`stones.md` section 10).
   2. **The count**: draw it as a fresh roll does (`rarity.md` section 4: uniform in `[min, max]`, or
      `rolling.count_weights`), then raise it to at least `kept + 1`.
   3. **The guaranteed draw, first**: one affix from the family, by the ordinary two-stage draw restricted to family
      members - eligibility as `rarity.md` section 4 (enabled and registered, slot, `requires`, regular pool, not the
      same id or exclusion group as a kept affix, at least one eligible tier), tiers from `[max(window low,
      floor), ceiling]` with the floor clamped to the ceiling. No candidate: refused with `essence_no_match`. This
      happens when every member for the slot is gated above the item's ceiling (Mistbane on a Plains belt), or when a
      kept affix already holds every member's id or group (a bound Venombrand on a sword leaves Blood Drinker; a bound
      Blood Drinker leaves Venombrand; both kept leaves nothing).
   4. **Mythic**: if the item's rarity has `mythic_affixes > 0` and no kept affix fills the Mythic-only slot, draw it
      next from the Mythic-only pool (regular pool until Phase 3, `rarity.md` section 5).
   5. **Fill** to the count from the regular pool with the ordinary window and **no floor**. Family members are not
      excluded from the fill: an item can end with two family affixes if their groups allow (ESS-15).
   6. The pool cannot reach the rarity's minimum: refused with `no_eligible_affix`.
4. **Confirm gate** (step 13): essences are not gated (`confirm: false`); like Upheaval, a reroll is the stone's
   everyday purpose.
5. **Commit** (step 14): one write, the cost, the equipment rebuild if equipped, the feedback message
   `essence_applied` naming the guaranteed affix.

What stays: rarity, the bound affix and its binding, a Preservation-protected affix, quality (Honing/Tempering),
vanilla upgrade level, durability, crafter, variant. What goes: every other affix, dormant ones included.

**The guarantee is always a new roll** (ESS-6). A kept affix that happens to be a family member does not count as the
guaranteed one: the essence always writes one fresh family affix, and the kept one blocks its own id and group. The
player paid for "a new venom affix", and that is what they get or the stone is refused.

**Success** is the dry run producing a valid item. An essence never refuses with `nothing_to_change`: with everything
kept, it still adds the guaranteed affix, and that is a change.

---

# 5. Sigils

Matrix entry for `sigils.md` section 2 (to be added there when built):

| Stone (verb) | Preservation | War / Warding / Fortune | Culling |
| --- | --- | --- | --- |
| Essences (`imbue`) | S | - | - |

- **Preservation steers** (the essence is a reroll-type stone, the kind PLAN.md gives Preservation to): the protected
  affix is kept, counts toward the count and toward `kept + 1`, and the sigil is consumed on success. If there is no
  live unbound affix to protect: refused `sigil_no_match`, as for Upheaval.
- **War, Warding and Fortune do not steer** an essence: the essence already steers its roll, and two steers on one
  stone would leave the player guessing which one bound the guaranteed draw. The sigil stays pending (SIG-1) for the
  next stone it does steer (ESS-7). A family's own categories are in section 2 for players who want to combine them:
  apply the essence, then a category sigil and a Stone of Turmoil.
- **Culling does not steer** (an essence removes every non-kept affix at once, as Upheaval does; SIG-5).
- **Serpent** is unaffected; an essence is never refused because a sigil is pending.

---

# 6. Refusals and feedback

The shared refusals (`stones.md` section 2) apply unchanged: `not_own_inventory`, `not_magic_base`, `newer_format`,
`unknown_rarity`, `stone_disabled`, `wrong_item_type` (only with an owner's `slots` filter), `sealed`, `equipped`,
`wrong_rarity`, `not_enough_stones`, `affixes_full`, `no_eligible_affix`, `sigil_no_match`. This file adds:

| Message id | English text | When |
| --- | --- | --- |
| `$ecf_msg_essence_wrong_item` | The $1 holds nothing this kind of item can take. | step 11: no family member for the item's slot and `requires` |
| `$ecf_msg_essence_no_match` | No affix of the $1 can roll on this item. | dry run: members exist for the slot, but none is eligible (tier gates, kept affixes) |

`$1` is the essence's display name. Feedback on success:

| Message id | English text |
| --- | --- |
| `$ecf_msg_essence_applied` | $1 is reshaped around $2. |

`$1` the item's name, `$2` the guaranteed affix's display name.

---

# 7. Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Common item | not in `applies_to` (Awaken it first) | `wrong_rarity` |
| Epic, Legendary or Mythic with a Lesser essence | not in `applies_to` | `wrong_rarity` |
| Mythic with a Greater essence | works: six again, one Mythic-only, one family affix | `essence_applied` |
| Family has no member for the slot (Venom on a shield, Storm on a staff) | refused before rolling | `essence_wrong_item` |
| Members list the slot but every one fails `requires` (Grove on a hammer: its only tool member needs the Farming skill) | refused before rolling | `essence_wrong_item` |
| Members exist but all are gated above the ceiling (Seidr on a Meadows belt: only Mistbane, T6+) | refused in the dry run | `essence_no_match` |
| Bound affix is a family member, another member remains for the slot | bound kept; the other member is guaranteed | `essence_applied` |
| Bound affix blocks every member for the slot (id or group) | refused | `essence_no_match` |
| Uncommon (max 2) with a bound affix and a pending Preservation | `kept + 1` = 3 > 2 | `affixes_full` |
| Only the bound affix on the item | everything else re-rolled around it; the guarantee is added | `essence_applied` |
| Dormant affixes | removed and replaced | - |
| Sealed item (Serpent, Reflection copy) | refused | `sealed` |
| Equipped | allowed with `Modify equipped items` on; rebuild on commit | - |
| Pending Preservation | protected affix kept; sigil consumed | `sigil_spent` |
| Pending War / Warding / Fortune / Culling | not steered; stays pending | - |
| Item over its maximum (Serpent's 7th affix) | unreachable - the item is sealed | `sealed` |
| Greater essence, member whose top tier is below the item's ceiling | that member is not eligible (section 3) | - |
| Pool cannot fill the rarity's minimum after the guarantee | refused | `no_eligible_affix` |
| Family list names an unknown or disabled affix | that member is skipped; the rest work | - |
| Family list empty after skipping | refused on every item | `essence_wrong_item` |
| Essence disabled, or its family removed from the YAML | refused | `stone_disabled` |
| Quality, upgrade level, crafter, variant | untouched | - |

---

# 8. The catalog

Sixteen built-in essences: eight families, two grades.

| id | Display name (English) | Prefab | Family | Grade | Home tier |
|---|---|---|---|---|---|
| `essence_storm_lesser` | Lesser Storm Essence | `ECF_EssenceStormLesser` | storm | lesser | 1 |
| `essence_storm_greater` | Greater Storm Essence | `ECF_EssenceStormGreater` | storm | greater | 1 |
| `essence_grove_lesser` | Lesser Grove Essence | `ECF_EssenceGroveLesser` | grove | lesser | 2 |
| `essence_grove_greater` | Greater Grove Essence | `ECF_EssenceGroveGreater` | grove | greater | 2 |
| `essence_venom_lesser` | Lesser Venom Essence | `ECF_EssenceVenomLesser` | venom | lesser | 3 |
| `essence_venom_greater` | Greater Venom Essence | `ECF_EssenceVenomGreater` | venom | greater | 3 |
| `essence_frost_lesser` | Lesser Frost Essence | `ECF_EssenceFrostLesser` | frost | lesser | 4 |
| `essence_frost_greater` | Greater Frost Essence | `ECF_EssenceFrostGreater` | frost | greater | 4 |
| `essence_battle_lesser` | Lesser Battle Essence | `ECF_EssenceBattleLesser` | battle | lesser | 5 |
| `essence_battle_greater` | Greater Battle Essence | `ECF_EssenceBattleGreater` | battle | greater | 5 |
| `essence_seidr_lesser` | Lesser Seidr Essence | `ECF_EssenceSeidrLesser` | seidr | lesser | 6 |
| `essence_seidr_greater` | Greater Seidr Essence | `ECF_EssenceSeidrGreater` | seidr | greater | 6 |
| `essence_ember_lesser` | Lesser Ember Essence | `ECF_EssenceEmberLesser` | ember | lesser | 7 |
| `essence_ember_greater` | Greater Ember Essence | `ECF_EssenceEmberGreater` | ember | greater | 7 |
| `essence_tide_lesser` | Lesser Tide Essence | `ECF_EssenceTideLesser` | tide | lesser | ocean (bonus rows) |
| `essence_tide_greater` | Greater Tide Essence | `ECF_EssenceTideGreater` | tide | greater | ocean (bonus rows) |

Localization: `$ecf_stone_essence_<family>_<grade>` and `..._desc` (the stone keys of `localization.md`), plus
`$ecf_family_<id>` for the family's own name ("Storm", "Grove", "Venom", "Frost", "Battle", "Seidr", "Ember", "Tide").
English descriptions, for example `$ecf_stone_essence_venom_lesser_desc`: "Rerolls an Uncommon or Rare item. One of
its new affixes is always of the Venom family." and for Greater: "Rerolls a magic item. One of its new affixes is
always of the Venom family, at the highest tier the item allows." The names are adopted defaults (ESS-14), like the
stone names before the user blessed them.

---

# 9. Prefabs and visuals

- **The 16 prefabs are a fixed built-in list registered from code**, like the 27 stones (`prefabs.md` sections 1-2):
  on every peer, identically, before any inventory or ZDO, whatever the YAML says. They join `StoneCatalog`'s
  built-in list (27 + 16 = 43 built-in stone ids). A disabled essence keeps its prefab (`../DECISIONS.md` RC-3).
- **Owner-defined essences** (a ninth family, a modded biome's family, a "Perfect" grade) bind to the reserved pool
  `ECF_Custom01`-`ECF_Custom16` through `prefab`, like any owner stone (`prefabs.md` section 5). No new reserved
  prefabs are added.
- **Base**: one new group, `essence`, on `Thunderstone` (believed vanilla; alternates `DragonTear`, `AmberPearl`) -
  **verify with `ecraft dump items` in game** exactly as PRF-1 does for the other groups (ESS-13).
- **Tint per family** (world model and icon, `prefabs.md` section 4 (b)); overridable per essence with `tint`:

| family | tint |
|---|---|
| storm | `#FFE45C` lightning yellow |
| grove | `#6B8E23` olive bark |
| venom | `#9ACD32` swamp yellow-green (distinct from the Serpent's `#3F7F2A`) |
| frost | `#9FE7FF` ice blue |
| battle | `#B0413E` rust red |
| seidr | `#6A5ACD` slate violet |
| ember | `#FF5A1F` flame orange |
| tide | `#1F8FA8` sea teal |

- **Scale per grade** as the other stones: lesser x0.85, greater x1.15 (PRF-3).
- Icons: runtime-tinted copies of the base's icon, the greater one lifted toward white (IMP-13), as `prefabs.md`
  section 6.
- Stack 50, item weight 0.2, value 0, teleportable - the stone defaults.

---

# 10. Drops

Essences are stones, so they drop through the existing stone machinery (`drops.md` sections 4-5, 9): no new roll,
no new hook.

- **Home tier only.** Each land family's two essences have weight at their biome's tier and 0 elsewhere. The biome
  is the source; an Ashlands player comes back to the Swamp for venom.
- **The stone chance per kill is unchanged**; essences share the tier's draw with the other stones.

| essence | T1 | T2 | T3 | T4 | T5 | T6 | T7 |
|---|---|---|---|---|---|---|---|
| `essence_storm_lesser` | 80 | 0 | 0 | 0 | 0 | 0 | 0 |
| `essence_storm_greater` | 20 | 0 | 0 | 0 | 0 | 0 | 0 |
| `essence_grove_lesser` | 0 | 80 | 0 | 0 | 0 | 0 | 0 |
| `essence_grove_greater` | 0 | 20 | 0 | 0 | 0 | 0 | 0 |
| `essence_venom_lesser` | 0 | 0 | 80 | 0 | 0 | 0 | 0 |
| `essence_venom_greater` | 0 | 0 | 20 | 0 | 0 | 0 | 0 |
| `essence_frost_lesser` | 0 | 0 | 0 | 80 | 0 | 0 | 0 |
| `essence_frost_greater` | 0 | 0 | 0 | 20 | 0 | 0 | 0 |
| `essence_battle_lesser` | 0 | 0 | 0 | 0 | 80 | 0 | 0 |
| `essence_battle_greater` | 0 | 0 | 0 | 0 | 20 | 0 | 0 |
| `essence_seidr_lesser` | 0 | 0 | 0 | 0 | 0 | 80 | 0 |
| `essence_seidr_greater` | 0 | 0 | 0 | 0 | 0 | 20 | 0 |
| `essence_ember_lesser` | 0 | 0 | 0 | 0 | 0 | 0 | 80 |
| `essence_ember_greater` | 0 | 0 | 0 | 0 | 0 | 0 | 20 |

What that means (all stones enabled, unstarred, non-boss): the Swamp's tier-3 table weighs about 1,005 without
essences; a Lesser Venom Essence is `80 / 1105` of the 6% stone chance, **about 1 in 230 Swamp kills**, and a Greater
one about 1 in 920. That is roughly half as common as an Ascension stone there. Starred creatures pay double or
triple as usual; Fateweaver applies.

**Boss bonus rows**, appended to each boss's `bonus` list in `drops.bosses` (`drops.md` section 9):

| Boss | Added rows |
|---|---|
| `Eikthyr` | `essence_storm_lesser` 100% x1, `essence_storm_greater` 15% x1 |
| `gd_king` | `essence_grove_lesser` 100% x1, `essence_grove_greater` 15% x1 |
| `Bonemass` | `essence_venom_lesser` 100% x1, `essence_venom_greater` 15% x1 |
| `Dragon` | `essence_frost_lesser` 100% x1, `essence_frost_greater` 15% x1 |
| `GoblinKing` | `essence_battle_lesser` 100% x1, `essence_battle_greater` 15% x1 |
| `SeekerQueen` | `essence_seidr_lesser` 100% x1, `essence_seidr_greater` 15% x1 |
| `Fader` | `essence_ember_lesser` 100% x1, `essence_ember_greater` 15% x1 |

**The Ocean has no tier of its own** (ocean creatures are tier 4 by `biomes.ocean`, which is the Mountain's table), so
tide essences come only from **creature bonus rows** (`drops.creatures`, `drops.md` section 7):

| Creature | Added rows |
|---|---|
| `Serpent` | `essence_tide_lesser` 25% x1, `essence_tide_greater` 5% x1 |
| `BonemawSerpent` *(verify name)* | `essence_tide_lesser` 25% x1, `essence_tide_greater` 5% x1 |

Bonus rows are not scaled by stars and not capped (IMP-44). An unknown creature key is a load warning, not an error
(`economy-yaml.md` section 9), so a wrong guess at the Ashlands serpent's name costs nothing but a warning.

Chests (Phase 2, `drops.md` section 11) roll the same tier tables, so a Swamp chest can hold a Venom essence.

**Judgement calls** (ESS-10): the 80/20 weights, home-tier-only, the boss rows (Eikthyr is cheap to re-summon, so its
Greater chance is the one to watch), and the tide rows.

---

# 11. YAML

Two additions to `EliteCrafting_economy*.yml`: the stone verb `imbue` with a `family` field, and a new top-level
section `essence_families`. Essences are ordinary entries in `stones:`; every common stone field of
`economy-yaml.md` section 4 applies unchanged.

## Stone fields for verb `imbue`

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `family` | family id | yes (for `imbue`) | - | the `essence_families` entry whose members the guaranteed draw uses |
| `tier_floor` | int 1-7 | no | none | for `imbue`: the lowest tier of the **guaranteed** affix only, clamped to the ceiling (section 3) |

## `essence_families`

A map, family id -> entry. Merges by key like the other economy maps (`economy-yaml.md` section 1); an entry's
`affixes` list is replaced whole.

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `affixes` | affix ids | yes | - | the members, from `EliteCrafting_affixes*.yml` |
| `name` | loc key or literal text | no | `$ecf_family_<id>` | the family's name in tooltips and `ecraft list` |

## Validation (additions to `economy-yaml.md` section 9)

| Check | Level |
| --- | --- |
| `imbue` stone without `family`, or `family` naming no `essence_families` entry | error |
| `family` on a stone whose verb is not `imbue` | warning (ignored) |
| Family id not snake_case; empty `affixes` list | error |
| A member id not defined in the **affix** family's rules in force | warning (skipped at runtime). A warning, not an error, because the two YAML families load, sync and reload separately; an error would let an edit to one family reject the other |
| A member with `mythic_only: true` | warning (skipped) |
| A member of weight 30 or less that is the only member of its family for one of its slots | warning (design rule 3) |
| An `imbue` stone whose `applies_to` includes the base rarity | error (nothing to reroll on a Common) |

The cross-family checks run whenever either family applies, against the other family's rules in force.

## The defaults

```yaml
essence_families:
  storm:  { affixes: [stormbrand, stormward, bulwark_lightning, fleetfoot, stride, spring_heeled, light_leap, nimble, perfect_guard, keen_guard] }
  grove:  { affixes: [bonebreaker, heartwood, woodcutting_mastery, troll_blood, mending, bramblehide, ironroot, repelling_guard, anchored_guard, harvester, farming_mastery] }
  venom:  { affixes: [venombrand, blood_drinker, blood_thrift, venomward, bulwark_poison, purity, marshstrider, oilskin] }
  frost:  { affixes: [rimebrand, frostward, bulwark_frost, coldblood, winterborn, mountain_goat, long_wind, endurance, soft_landing, ravens_glide] }
  battle: { affixes: [honed_might, keen_edge, needlepoint, berserkergang, padded, mailed, riveted, ironclad, arrowward, valhallas_edge, stalwart, tireless_guard, broad_back, trophy_taker] }
  seidr:  { affixes: [primal_fury, seidr_siphon, rune_edge, seidr_thrift, soul_reaper, elemental_mastery, wellspring, seidr_flow, restless_mind, runic_ward, mist_veil, mistbane, seidr_riposte] }
  ember:  { affixes: [emberbrand, flameward, bulwark_fire, ashen_skin, emberheart, hearthlight] }
  tide:   { affixes: [slayer_sea, sealegs, strong_swimmer, fair_winds, huginns_eye, fishing_mastery] }

stones:
  # --- essences (Phase 2) ---
  - { id: essence_storm_lesser,   verb: imbue, family: storm,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_storm_greater,  verb: imbue, family: storm,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_grove_lesser,   verb: imbue, family: grove,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_grove_greater,  verb: imbue, family: grove,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_venom_lesser,   verb: imbue, family: venom,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_venom_greater,  verb: imbue, family: venom,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_frost_lesser,   verb: imbue, family: frost,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_frost_greater,  verb: imbue, family: frost,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_battle_lesser,  verb: imbue, family: battle, grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_battle_greater, verb: imbue, family: battle, grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_seidr_lesser,   verb: imbue, family: seidr,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_seidr_greater,  verb: imbue, family: seidr,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_ember_lesser,   verb: imbue, family: ember,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_ember_greater,  verb: imbue, family: ember,  grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  - { id: essence_tide_lesser,    verb: imbue, family: tide,   grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_tide_greater,   verb: imbue, family: tide,   grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }

drops:
  stones:                 # T1  T2  T3  T4  T5  T6  T7
    essence_storm_lesser:   [80,  0,  0,  0,  0,  0,  0]
    essence_storm_greater:  [20,  0,  0,  0,  0,  0,  0]
    # ... one pair per land family at its own tier, as section 10
  bosses:
    Bonemass:
      bonus:              # replaces the list whole (ECO-1): the existing rows are repeated
        - { stone: serpent, chance: 50, amount: 1 }
        - { stone: essence_venom_lesser, chance: 100, amount: 1 }
        - { stone: essence_venom_greater, chance: 15, amount: 1 }
    # ... every boss likewise
  creatures:
    Serpent:        { bonus: [ { stone: essence_tide_lesser, chance: 25, amount: 1 }, { stone: essence_tide_greater, chance: 5, amount: 1 } ] }
    BonemawSerpent: { bonus: [ { stone: essence_tide_lesser, chance: 25, amount: 1 }, { stone: essence_tide_greater, chance: 5, amount: 1 } ] }
```

The built-in default layer carries these entries, so existing servers get essences when the release that builds them
lands, without editing a file (`configuration.md` section 3, CFG-7). Tints default per family (section 9).

## Worked example: an owner's own family

A server with a modded biome adds a family and one essence bound to a reserved prefab, in
`EliteCrafting_economy_server.yml`:

```yaml
essence_families:
  hunt:
    name: "Hunt"
    affixes: [slayer_beasts, ambusher, ghostwalk, shadowmeld, soft_tread, bow_mastery]

stones:
  - id: essence_hunt
    name: "Hunter's Essence"
    description: "Rerolls a magic item. One new affix is always of the Hunt family, at the item's top tier."
    prefab: ECF_Custom07
    verb: imbue
    family: hunt
    applies_to: [uncommon, rare, epic, legendary]
    tier_floor: 7
    tint: "#8B5A2B"

drops:
  creatures:
    Deer: { bonus: [ { stone: essence_hunt, chance: 2, amount: 1 } ] }
```

And an owner who thinks Venom should also cover shields adds one member without restating the others' fields - but
`affixes` is a list, so it is replaced whole and restated:

```yaml
essence_families:
  venom:
    affixes: [venombrand, blood_drinker, blood_thrift, venomward, bulwark_poison, purity, marshstrider, oilskin, tireless_guard]
```

---

# 12. Display and commands

- **Tooltip of an essence** (Display area): after the vanilla description, one line `$ecf_ui_essence_family`
  ("Always adds one of: $1"), `$1` the localized names of the family's live members, comma-separated. Built on
  demand, cached until `ActiveRules.RulesChanged` or `Words.Changed`, like every tooltip block.
- **Tooltip of a magic item** does not record which essence shaped it: the item data has no essence key. An essence
  writes ordinary affixes.
- **`ecraft list`** shows the families and their live members under the stones; **`ecraft give essence_venom_lesser
  3`** works as for any stone. Both belong to `console-commands.md` when built; no new sub-command.

---

# 13. Multiplayer

- Essences are stones: applied only in the player's own inventory, on the client that owns it, under the server's
  synced rules (`multiplayer.md` section 2). The family lists are part of the economy family, so a bound player uses
  the server's families; the affix definitions they point at are the affix family's, also the server's.
- The result travels in the item's own custom data - no netcode.
- The 16 prefabs are code-registered on every peer (section 9), so a join-in-progress client and a dedicated server
  agree on their names and hashes before any world data arrives. The mod is already required on every peer.
- Drops roll on the dying creature's ZDO owner from the synced tables, as for every stone.
- Transient feedback (the Phase 3 stone effect) follows the stone family's rule; no RPC in Phase 2 beyond what every
  stone has.

---

# 14. Performance

- Per family, at every rules apply: the member list resolved to definitions, and per slot the members that list it
  (a `family x slot` table of definition arrays). The step-11 check is one table lookup plus `requires` checks on a
  handful of members.
- The guaranteed draw reuses the roller's candidate filter with a family restriction; nothing new on any hot path.
  Essences do nothing per frame.

---

# Build checklist

- [x] Verb `imbue` in the roller: kept set, count raised to `kept + 1`, family draw first with the floor, Mythic-only
      slot, fill without floor
- [x] Step-11 precondition: family member for slot and `requires`; room for the guarantee
- [x] `essence_wrong_item`, `essence_no_match`, `essence_applied` localized
- [x] Preservation steers; War/Warding/Fortune/Culling stay pending
- [x] `essence_families` section read, merged by key, validated (cross-family warnings)
- [x] 16 prefabs on the `essence` base, tinted per family, scaled per grade; in `StoneCatalog`
- [x] Drop rows at home tier; boss and serpent bonus rows
- [x] Essence tooltip line (in the item description, IMP-102); `ecraft list` shows families
- [ ] Seen working on a dedicated server with two clients (`multiplayer.md` section 6 gains an essence step)

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified (Phase 2 spec pass). | pending |
| 2026-09-24 | Built: verb `imbue`, `essence_families`, 16 prefabs, drop rows, tooltip line, `ecraft list`/`give`. Checked with a scratch harness against the real defaults (Lesser/Greater rolls, ceiling tier, bound and preserved affixes, Mythic, refusals). Not tested in game. | pending |

---

# Decisions

Every judgement call in this file is in `../DECISIONS.md` (Essences: ESS-1 to ESS-17). None is blocking.

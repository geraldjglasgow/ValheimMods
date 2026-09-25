# EliteCrafting - specification: Affixes

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index. The Mythic-only
pool is in `affixes-mythic.md`, which uses everything defined here.

This file is the **affix catalog**: what an affix is, which slots each one rolls on, the effect registry the code
implements, and one entry per affix with its tiers and weights. How an affix becomes a change in the game is
`effects-runtime.md`; how affixes are drawn onto an item (tier window, two-stage draw, counts per rarity) is
`rarity.md`; how an item's tier ceiling is set is `item-tier.md`; how the data sits on the item is `item-data.md`.

**Every number in this file is a default, and every one is a judgement call** made at a desk, not in play. They are
written so the YAML defaults can be generated from them, and so they can be argued with. Numbers that are
especially arguable carry a *Judgement* line. The display names are adopted defaults (`../DECISIONS.md` AFX-13); any
of them can be renamed in the English file without touching an id.

**Status: specified; the Phase 1 set (section 6) built, not tested in game** (2026-09-23). 169 regular affixes, 13 Mythic-only affixes (`affixes-mythic.md`),
131 effects. Phase 1 builds the set in section 6.

Contents:

1. What an affix is - fields, value types, tiers, weights, exclusion, requirements, health-critical, dedupe
2. Slot sanity matrix
3. The effect registry
4. The catalog
5. The Mythic-only pool (pointer)
6. Phase 1 set
7. Decisions

---

# 1. What an affix is

An **affix** is one rollable line on a magic item: "+14 maximum health", "attacks cost 9% less stamina". It is a YAML
entry in the `EliteCrafting_affixes*.yml` family with its own id, name, slots, tiers and weights.

An **effect** is the code that makes an affix do something. It is registered in code under its own id and may take
a **parameter**. Many affixes share one effect: `blade_mastery` and `axe_mastery` are two affixes on effect
`skill_level` with params `Swords` and `Axes`; the eight brands are eight affixes on `brand_damage`. YAML can add
new affixes over registered effects, never new effects (`effects-runtime.md` section 1).

## The fields of an affix

The complete schema, with types and validation, is `configuration.md` section 6.

| Field | Meaning |
|---|---|
| `id` | snake_case, stable forever: it is written into items (`item-data.md`). Renaming an id orphans every item carrying it |
| `name` | localization key `$ecf_affix_<id>`; the English default is the display name in this file |
| `effect`, `param` | the registered effect and its parameter (section 3) |
| `value` | `percent`, `flat` or `flag` |
| `slots` | slot ids it may roll on (section 2) |
| `requires` | narrower item filter: governing skill, hands, item traits (below) |
| `category` | `offense`, `defense` or `utility`: what the War, Warding and Fortune sigils steer by |
| `mythic_only` | `true` only in the Mythic pool |
| `condition` | `none` or `health_critical` |
| `exclusion_group` | near-duplicates share a group; one per item |
| `weight` | how likely the affix is to be picked (the first stage of `rarity.md`'s two-stage draw) |
| `tiers` | `[{tier, min, max, weight}]`, one row per biome tier the affix exists at |
| `unit` | display unit for flat values: `ms`, `deg`, `m`, `min` |
| `enabled` | owner switch |
| `hook` | `easy`, `medium`, `hard`: documentation and validation only |

## Value types

- **percent**: a percentage of something the effect names ("+X% armor on this piece").
- **flat**: an absolute amount in the stat's own unit (health points, skill levels, degrees, milliseconds).
- **flag**: on or off. A flag has no value; it is stored as `1` in the item's `id:tier:value` list.
- **Values are stored positive.** Whether the stat goes up or down is the effect's `polarity` (a cost reduction of
  9 is stored as `9`, the effect subtracts). The tooltip sign comes from the polarity (`display.md`).
- Two effects are **better when lower** (`undying` cooldown minutes, `hit_cap` percent). The registry marks them so
  the tooltip does not color a low roll as bad.
- **Rounding.** A value is drawn uniformly in the tier's `[min, max]` and rounded to the decimals of the tier's
  bounds: integer bounds give integers, `1.5` gives one decimal. A value is never outside its tier's bounds.
- **Composite values.** Three affixes use their value twice (`stout_heart`, `restless_mind`, `lone_blade`): "+X, and
  -X/2 %". One stored number drives both halves, so Perfection rerolls them together.

## Tiers and biome gates

An affix tier is its strength step, gated by biome. Tier 1 is the Meadows, tier 7 the Ashlands.

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Biome | meadows | black_forest | swamp | mountain | plains | mistlands | ashlands |

(Tier 8, deep_north, is reserved and not shipped.) Every item has a **tier ceiling** (`item-tier.md`) and rolls its
affix tiers from a window just below it (`rarity.md`: default window 3, so an Ashlands item rolls tiers 5-7).

**Notation in the catalog.** `T1–T7: 3–5 / 5–8 / 8–11 / ...` lists `min–max` for each tier, left to right. A single
number means `min = max`. An affix that skips early tiers starts later (`T5–T7: ...`) and cannot roll on an item
whose ceiling is below its first tier. A **flag** has a single **gate tier**: `gate T4` means the affix exists at
tiers 4-7 with no value, so it is eligible on any item of ceiling 4 or more and its recorded tier still counts for
the sigils that care about tier.

**Tier weights are flat.** Every tier row carries `weight: 100` unless an entry says otherwise, so inside the window
each tier is equally likely. Rarity lives in the affix `weight` instead (below), which is easier to reason about.
Judgement call: a descending profile (higher tiers rarer) would slow progression inside a window; the owner can set
per-tier weights in YAML if they want it.

**Why most affixes span all seven tiers.** Rarity is affix count and tier is strength, so a Meadows Mythic should
still find six affixes. Affixes start late only when an early roll would be dead: eitr affixes (no eitr before
Mistlands food, so tiers 5-7), staff affixes (staves are Mistlands items), heat (Ashlands only), mist (Mistlands
only), bosses (Godslayer from tier 3).

## Weights and rarity by design

The affix `weight` is its share of the first-stage draw (`rarity.md` section 4). **Default 100**; a lower
weight makes an affix rarer by design.

Everything not listed here weighs 100. Roughly: 15-30 are build-changing behaviours, 40-60 strong or niche
effects and the health-critical variants, 70 situational ones.

- **15**: `gungnir_path`
- **20**: `faithful_throw`, `gossamer`, `twincast`, `volley`
- **25**: `everlasting`, `hurler`, `shatterhead`, `winterborn`
- **30**: `anchored_guard`, `blood_price`, `bulwark_fire`, `bulwark_frost`, `bulwark_lightning`, `bulwark_poison`, `ravens_glide`, `reflex_draught`, `rune_edge`, `swift_draught`, `thors_arc`
- **40**: `emberheart`, `fafnirs_greed`, `hearthlight`, `lone_blade`, `oilskin`, `sealegs`, `steel_rhythm`, `supple_fit`
- **50**: `blood_drinker_hc`, `fateweaver`, `flowing_seidr`, `godslayer`, `mist_veil_hc`, `norns_favour`, `quickened_blows`, `valhallas_edge`
- **60**: `berserkergang`, `blood_drinker`, `cruel_opening`, `elemental_ward`, `evaders_fury`, `farming_mastery`, `fleetfoot_hc`, `hardened_hc`, `hearthbound`, `ironclad`, `keen_guard`, `marshstrider`, `mist_veil`, `mountain_goat`, `pathfinder`, `restless_mind`, `seidr_riposte`, `seidr_siphon`, `stout_heart`, `swift_string`, `troll_blood_hc`
- **70**: `arrowward`, `beast_whisperer`, `bramblehide`, `brewers_haste`, `coldblood`, `dazing_blows`, `deep_vein`, `fair_winds`, `forsaken_favour`, `hamstring`, `heartwood`, `hoardfinder`, `mistbane`, `momentum`, `pack_mule`, `purity`, `quick_recovery`, `runic_ward`, `skirmisher`, `soulbound`, `spiritbrand`, `strong_swimmer`, `thrifty_quiver`, `trophy_taker`

Mythic-only weights are relative inside the Mythic pool (`affixes-mythic.md`).

## Exclusion

- **The same id never appears twice on one item.** A dormant copy still holds its id (`rarity.md`).
- **An exclusion group blocks near-duplicates**: one member per item. Across items they stack normally (a helmet
  with Vigor and a chest with Stout Heart both count). The groups:

| group | members |
|---|---|
| `damage_physical` | `honed_might` |
| `damage_elemental` | `primal_fury` |
| `damage_critical` | `berserkergang` |
| `slayer` | `slayer_undead`, `slayer_beasts`, `slayer_sea` |
| `on_kill` | `reaper`, `soul_reaper` |
| `leech_health` | `blood_drinker`, `blood_drinker_hc` |
| `brand_elemental` | `emberbrand`, `rimebrand`, `stormbrand`, `venombrand`, `spiritbrand` |
| `brand_physical` | `bonebreaker`, `keen_edge`, `needlepoint` |
| `swing_speed` | `quickened_blows`, `flowing_seidr` |
| `attack_cost` | `balanced_grip`, `blood_price`, `rune_edge`, `seidr_thrift` |
| `throw_follow` | `faithful_throw`, `gungnir_path` |
| `attack_cost_health` | `blood_thrift` |
| `skill_level` | `blade_mastery`, `axe_mastery`, `club_mastery`, `knife_mastery`, `spear_mastery`, `polearm_mastery`, `fist_mastery`, `bow_mastery`, `crossbow_mastery`, `elemental_mastery`, `blood_mastery`, `woodcutting_mastery`, `pick_mastery`, `fishing_mastery`, `shield_mastery`, `wanderer_mastery`, `artisan_mastery`, `farming_mastery` |
| `item_wear` | `well_forged`, `everlasting` |
| `item_lighten` | `lightened`, `gossamer` |
| `max_health` | `vigor`, `stout_heart` |
| `max_eitr` | `wellspring` |
| `health_recovery` | `troll_blood`, `troll_blood_hc` |
| `eitr_recovery` | `seidr_flow`, `restless_mind` |
| `item_armor` | `hardened`, `hardened_hc` |
| `elemental_bulwark` | `bulwark_fire`, `bulwark_frost`, `bulwark_lightning`, `bulwark_poison` |
| `avoid_hit` | `mist_veil`, `mist_veil_hc` |
| `move_speed` | `fleetfoot`, `fleetfoot_hc`, `stride` |
| `fall` | `soft_landing`, `ravens_glide` |
| `wet` | `oilskin`, `sealegs` |
| `mead_critical` | `reflex_draught`, `swift_draught` |
| `party_aura` | `warbanner`, `aegis` |
| `element_pact` | `pact_fire`, `pact_frost`, `pact_lightning`, `pact_poison` |

Groups are only for near-duplicates. Two affixes that touch the same stat in different situations (Stride while
sprinting, Ghostwalk while sneaking) are not grouped: both on one item is a fine item.

## Requirements: which items inside a slot

`slots` says which kind of item; `requires` narrows it. All present keys must hold.

- **`skill`** (list, any of): the item must be **governed** by one of these skills. An item's governing skills are
  its own skill type (`m_shared.m_skillType`), plus **Woodcutting** when its base damage includes chop, plus
  **Pickaxes** when it includes pickaxe damage. So an axe is governed by Axes and Woodcutting, a shield by Blocking,
  a fishing rod by Fishing. This is how "skill affixes roll only on items governed by that skill" is expressed.
- **`hands`**: `one` (one-handed weapon types) or `two` (two-handed types, atgeirs, bows).
- **`traits`** (list, all of): properties read from the item's shared data at load:
  `wears_out` (uses durability), `movement_penalty` (negative movement modifier), `builds` (has a build-piece table:
  hammer, hoe, cultivator), `projectile` (its primary attack fires projectiles), `ammo` (uses ammunition),
  `can_parry` (perfect-block bonus above 1: tower shields fail it).

An affix with no eligible item in any of its slots is a load **warning**, not an error: a mod might add such items.

**Two affixes sit on armor or utility items because no item governs their skills**: Wanderer's Mastery (Run, Jump,
Swim, Sneak) on legs and capes, Artisan's Mastery (Crafting, Cooking) on helmets and utility items.

**Skill levels past 100 do nothing in vanilla.** The game clamps its skill factor at 100 (`Skills.GetSkillFactor`,
verified in the decompile), so a skill affix on a player already at 100 shows a higher number and changes nothing.
The pool asked for "may exceed 100"; this file does not patch the clamp (`../DECISIONS.md` AFX-5).

## Health-critical

**Health-critical** is a condition: current health at or below the **threshold** share of maximum health.

- **Default threshold 30%.** Valhalla's Edge adds percentage points **additively** (two pieces at +5 make 40%).
- **The threshold cannot pass 50%** (the `hc_threshold` channel caps at +20). Past half health the condition stops
  being a crisis and the conditional affixes become flat stat boosts with a bigger number.
- Evaluated on the player's own client from its own health. The aggregate status effect reads it every tick (one
  comparison); hooks read the same cached flag. Nothing extra is synced.
- A health-critical affix and its unconditional twin share an exclusion group (one per item) but feed **separate
  channels**: the conditional channel is summed and capped on its own and switched on only while critical. The
  channel key therefore includes the condition (`configuration.md` section 6, `caps`).

**Which stats have a health-critical variant - a sensible handful, judgement call** (`../DECISIONS.md` AFX-1). The pool listed eleven stat variants and two mead behaviours. Kept:

| Affix | What it does when critical | Twin |
|---|---|---|
| `berserkergang` | +X% all damage dealt | none (the uncritical versions are split by type) |
| `blood_drinker_hc` | heal X% of damage dealt | `blood_drinker` |
| `troll_blood_hc` | health regenerates X% faster | `troll_blood` |
| `hardened_hc` | +X% armor on this piece | `hardened` |
| `mist_veil_hc` | X% chance to avoid a hit | `mist_veil` |
| `fleetfoot_hc` | move X% faster | `fleetfoot` |

Plus two affixes that exist only because of the condition: `reflex_draught` (drink a mead on becoming critical) and
`swift_draught` (meads heal at once while critical), and the threshold affix `valhallas_edge`.

Dropped: swing speed (two hard hooks for one line), block and parry (a player blocking at low health is covered by
the armor variant), stamina and eitr regeneration (overlap the health one; a critical player needs health). That is
six variants, one per role: hit harder, heal, regenerate, soak, dodge, run.

## Dedupe decisions from the pool

1. **Slow fall versus rollable fall damage.** Both kept, one group (`fall`). `soft_landing` is the rollable one,
   5-40% by tier, and the `fall_damage_taken` channel caps at 80%, so stacking legs and cape never reaches zero.
   `ravens_glide` is the flag: slow descent and no fall damage at all, cape only, gate tier 4, weight 30. The flag
   is the only way to fall safely from any height, and it takes a cape slot to get it.
2. **Percent elemental resistance versus the game's resistant modifier.** Both kept; they are different layers.
   The percent affixes (`flameward`, ..., `elemental_ward`) scale the element's part of an incoming hit, stack
   additively with each other up to the channel cap (60%), and multiply with everything else. The `bulwark_<element>`
   flags give the game's own **Resistant** step for one element through `ModifyDamageMods`: never better than
   Resistant, no stacking with a cape or mead that already gives it (the game takes the best modifier), cape only,
   gate tier 3, weight 30. One id per element in group `elemental_bulwark` instead of one affix with a rolled
   element: a param fixed per id keeps ids stable and the tooltip honest.
3. **All-physical and all-elemental resistance versus single types.** Kept, on a flatter curve (about 60% of the
   single-type numbers) and fewer slots (chest, cape).
4. **Weight reduction versus zero weight**: group `item_lighten`. **Durability versus never breaking**: group
   `item_wear`.
5. **The movement-speed variants PLAN.md names**: `fleetfoot`, `fleetfoot_hc` and `stride` share group
   `move_speed`. The situational ones (sneaking, encumbered, after a dodge, on roads) stay ungrouped.
6. **Attack cost rewrites**: `balanced_grip`, `blood_price` and `rune_edge` all rewrite the same stamina cost, so
   one per item (group `attack_cost`, which also holds the staff's `seidr_thrift`).
7. **Axes and woodcutting**: two affixes (`axe_mastery`, `woodcutting_mastery`), both in group `skill_level`, so an
   axe carries one of them. Lumber yield (`heartwood`) is a separate, non-skill affix.
8. **Cold and Freezing**: `emberheart` (never Cold) and `winterborn` (never Freezing) cover different game statuses
   and stay separate; together they make the wearer cold-proof. **Rain and Wet**: `oilskin` (rain never wets) and
   `sealegs` (Wet does not hurt) are redundant on one item, so they share group `wet`.
9. **Knockback**: `ironroot` (always, percent, legs) and `anchored_guard` (while blocking with that shield, flag,
   also stops the stagger) stay separate. **Stagger**: `resolute` (you build it up slower) and `quick_recovery` (you
   recover sooner) stay separate. **Stealth**: `soft_tread` (sound) and `shadowmeld` (sight) stay separate.
10. **Spirit**: a spirit brand exists (it hurts the undead) at weight 70; no spirit resistance (PLAN.md decision).

## What counts

Only equipped items count: weapons and tools in hand, shields equipped, armor, capes and utility items worn
(`effects-runtime.md` section 2). A weapon's player-global affix (Nightstalker, Reaper) counts while that weapon is
in hand.

**Item-local** affixes describe the item they sit on (its armor, block, durability, weight, brand, reach, draw).
They change that item only, show in the vanilla tooltip where the game computes the number, and are never summed
across items. **Player-global** affixes sum into channels (`effects-runtime.md` section 5).

---

# 2. Slot sanity matrix

PLAN.md's rules, applied (user directive 2026-09-23: no armor stats on weapons, no weapon stats on armor):

- Item-local stats stay on the item they describe: swing speed, brands, reach, arc, draw, reload → weapons; block and
  parry → shields; armor percent → armor pieces; durability and weight → every item class that has them.
- Player-global offense (family damage, sneak-attack bonus, the low-health opener, on-kill and on-hit effects) →
  weapons only.
- Player-global defense, regeneration, resistances and movement → armor, spread over head, chest, legs and cape so
  that stacking one stat costs slot choices. No armor affix rolls on all four slots except the item-local
  `hardened`, `well_forged` and `lightened`.
- Utility (carry, pickup, discovery, comfort, the find affixes) → armor and utility items.
- Tool effects (build range, gathering yield) → tools only. Lumber yield rolls on axes (slot `melee_weapon`,
  `requires.skill: [WoodCutting]`) because axes are weapons in `item-data.md`'s slot table and the chop is theirs.

Slot ids and how an item maps to one are `item-data.md` section 2 (pickaxes are `tool`, axes `melee_weapon`, trinkets
and torches never eligible). Columns: melee, ranged, magic = `melee_weapon`, `ranged_weapon`, `magic_weapon`;
utility = `utility_item`.

<div style="overflow-x:auto">

| affix | melee | ranged | magic | shield | head | chest | legs | cape | utility | tool | requires |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `honed_might` | x | x |  |  |  |  |  |  |  |  |  |
| `primal_fury` | x | x | x |  |  |  |  |  |  |  |  |
| `berserkergang` | x | x | x |  |  |  |  |  |  |  |  |
| `slayer_undead` | x | x | x |  |  |  |  |  |  |  |  |
| `slayer_beasts` | x | x | x |  |  |  |  |  |  |  |  |
| `slayer_sea` | x | x | x |  |  |  |  |  |  |  |  |
| `godslayer` | x | x | x |  |  |  |  |  |  |  |  |
| `nightstalker` | x | x | x |  |  |  |  |  |  |  |  |
| `ambusher` | x | x |  |  |  |  |  |  |  |  |  |
| `deathblow` | x | x | x |  |  |  |  |  |  |  |  |
| `cruel_opening` | x | x |  |  |  |  |  |  |  |  |  |
| `press_the_advantage` | x | x |  |  |  |  |  |  |  |  |  |
| `reaper` | x | x |  |  |  |  |  |  |  |  |  |
| `blood_drinker` | x | x |  |  |  |  |  |  |  |  |  |
| `blood_drinker_hc` | x | x |  |  |  |  |  |  |  |  |  |
| `evaders_fury` | x | x |  |  |  |  |  |  |  |  |  |
| `fafnirs_greed` | x |  |  |  |  |  |  |  |  |  |  |
| `emberbrand` | x | x |  |  |  |  |  |  |  |  |  |
| `rimebrand` | x | x |  |  |  |  |  |  |  |  |  |
| `stormbrand` | x | x |  |  |  |  |  |  |  |  |  |
| `venombrand` | x | x |  |  |  |  |  |  |  |  |  |
| `spiritbrand` | x | x |  |  |  |  |  |  |  |  |  |
| `bonebreaker` | x | x |  |  |  |  |  |  |  |  |  |
| `keen_edge` | x | x |  |  |  |  |  |  |  |  |  |
| `needlepoint` | x | x |  |  |  |  |  |  |  |  |  |
| `staggering_blows` | x |  |  |  |  |  |  |  |  |  |  |
| `dazing_blows` | x |  |  |  |  |  |  |  |  |  |  |
| `hamstring` | x | x |  |  |  |  |  |  |  |  |  |
| `thors_arc` | x |  | x |  |  |  |  |  |  |  |  |
| `quickened_blows` | x |  |  |  |  |  |  |  |  |  |  |
| `long_reach` | x |  |  |  |  |  |  |  |  |  |  |
| `sweeping_arc` | x |  |  |  |  |  |  |  |  |  |  |
| `balanced_grip` | x |  |  |  |  |  |  |  |  |  |  |
| `blood_price` | x |  |  |  |  |  |  |  |  |  |  |
| `rune_edge` | x |  |  |  |  |  |  |  |  |  |  |
| `lone_blade` | x |  |  |  |  |  |  |  |  |  | one-handed |
| `steel_rhythm` | x |  |  |  |  |  |  |  |  |  |  |
| `hurler` | x |  |  |  |  |  |  |  |  |  | skill Knives/Axes/Clubs; one-handed |
| `faithful_throw` | x |  |  |  |  |  |  |  |  |  | skill Spears |
| `gungnir_path` | x |  |  |  |  |  |  |  |  |  | skill Spears |
| `heartwood` | x |  |  |  |  |  |  |  |  |  | skill WoodCutting |
| `easy_draw` |  | x |  |  |  |  |  |  |  |  | skill Bows |
| `swift_string` |  | x |  |  |  |  |  |  |  |  | skill Bows |
| `quick_windlass` |  | x |  |  |  |  |  |  |  |  | skill Crossbows |
| `true_flight` |  | x | x |  |  |  |  |  |  |  | traits projectile |
| `thrifty_quiver` |  | x |  |  |  |  |  |  |  |  |  |
| `volley` |  | x |  |  |  |  |  |  |  |  | skill Bows |
| `shatterhead` |  | x |  |  |  |  |  |  |  |  |  |
| `skirmisher` |  | x |  |  |  |  |  |  |  |  |  |
| `seidr_thrift` |  |  | x |  |  |  |  |  |  |  |  |
| `blood_thrift` |  |  | x |  |  |  |  |  |  |  | skill BloodMagic |
| `twincast` |  |  | x |  |  |  |  |  |  |  | skill ElementalMagic |
| `flowing_seidr` |  |  | x |  |  |  |  |  |  |  | skill ElementalMagic |
| `soul_reaper` |  |  | x |  |  |  |  |  |  |  |  |
| `seidr_siphon` | x |  | x |  |  |  |  |  |  |  |  |
| `grave_command` |  |  | x |  |  |  |  |  |  |  | skill BloodMagic |
| `grave_vigor` |  |  | x |  |  |  |  |  |  |  | skill BloodMagic |
| `blade_mastery` | x |  |  |  |  |  |  |  |  |  | skill Swords |
| `axe_mastery` | x |  |  |  |  |  |  |  |  |  | skill Axes |
| `club_mastery` | x |  |  |  |  |  |  |  |  |  | skill Clubs |
| `knife_mastery` | x |  |  |  |  |  |  |  |  |  | skill Knives |
| `spear_mastery` | x |  |  |  |  |  |  |  |  |  | skill Spears |
| `polearm_mastery` | x |  |  |  |  |  |  |  |  |  | skill Polearms |
| `fist_mastery` | x |  |  |  |  |  |  |  |  |  | skill Unarmed |
| `bow_mastery` |  | x |  |  |  |  |  |  |  |  | skill Bows |
| `crossbow_mastery` |  | x |  |  |  |  |  |  |  |  | skill Crossbows |
| `elemental_mastery` |  |  | x |  |  |  |  |  |  |  | skill ElementalMagic |
| `blood_mastery` |  |  | x |  |  |  |  |  |  |  | skill BloodMagic |
| `woodcutting_mastery` | x |  |  |  |  |  |  |  |  |  | skill WoodCutting |
| `pick_mastery` |  |  |  |  |  |  |  |  |  | x | skill Pickaxes |
| `fishing_mastery` |  |  |  |  |  |  |  |  |  | x | skill Fishing |
| `shield_mastery` |  |  |  | x |  |  |  |  |  |  | skill Blocking |
| `wanderer_mastery` |  |  |  |  |  |  | x | x |  |  |  |
| `artisan_mastery` |  |  |  |  | x |  |  |  | x |  |  |
| `farming_mastery` |  |  |  |  |  |  |  |  |  | x | skill Farming |
| `well_forged` | x | x | x | x | x | x | x | x |  | x | traits wears_out |
| `everlasting` | x | x | x | x | x | x | x | x |  | x | traits wears_out |
| `lightened` | x | x | x | x | x | x | x | x | x | x |  |
| `gossamer` | x | x | x | x | x | x | x | x | x | x |  |
| `supple_fit` | x |  |  | x | x | x | x | x |  |  | traits movement_penalty |
| `stalwart` |  |  |  | x |  |  |  |  |  |  |  |
| `perfect_guard` |  |  |  | x |  |  |  |  |  |  | traits can_parry |
| `keen_guard` |  |  |  | x |  |  |  |  |  |  | traits can_parry |
| `repelling_guard` |  |  |  | x |  |  |  |  |  |  |  |
| `tireless_guard` |  |  |  | x |  |  |  |  |  |  |  |
| `anchored_guard` |  |  |  | x |  |  |  |  |  |  |  |
| `seidr_riposte` |  |  |  | x |  |  |  |  |  |  | traits can_parry |
| `vigor` |  |  |  |  | x | x |  |  |  |  |  |
| `endurance` |  |  |  |  |  | x | x |  |  |  |  |
| `wellspring` |  |  |  |  | x |  |  | x |  |  |  |
| `troll_blood` |  |  |  |  |  | x | x |  |  |  |  |
| `troll_blood_hc` |  |  |  |  |  | x |  |  |  |  |  |
| `mending` |  |  |  |  | x | x |  |  |  |  |  |
| `stout_heart` |  |  |  |  |  | x |  |  |  |  |  |
| `second_wind` |  |  |  |  | x |  | x |  |  |  |  |
| `seidr_flow` |  |  |  |  | x |  |  | x |  |  |  |
| `restless_mind` |  |  |  |  | x |  |  |  |  |  |  |
| `valhallas_edge` |  |  |  |  | x |  |  |  |  |  |  |
| `hardened` |  |  |  |  | x | x | x | x |  |  |  |
| `hardened_hc` |  |  |  |  |  | x | x |  |  |  |  |
| `padded` |  |  |  |  |  | x | x |  |  |  |  |
| `mailed` |  |  |  |  |  | x | x |  |  |  |  |
| `riveted` |  |  |  |  |  | x | x |  |  |  |  |
| `ironclad` |  |  |  |  |  | x |  |  |  |  |  |
| `flameward` |  |  |  |  |  | x |  | x |  |  |  |
| `frostward` |  |  |  |  | x |  |  | x |  |  |  |
| `stormward` |  |  |  |  | x |  |  | x |  |  |  |
| `venomward` |  |  |  |  |  |  | x | x |  |  |  |
| `elemental_ward` |  |  |  |  |  |  |  | x |  |  |  |
| `bulwark_fire` |  |  |  |  |  |  |  | x |  |  |  |
| `bulwark_frost` |  |  |  |  |  |  |  | x |  |  |  |
| `bulwark_lightning` |  |  |  |  |  |  |  | x |  |  |  |
| `bulwark_poison` |  |  |  |  |  |  |  | x |  |  |  |
| `arrowward` |  |  |  |  |  | x |  |  |  |  |  |
| `mist_veil` |  |  |  |  |  |  | x | x |  |  |  |
| `mist_veil_hc` |  |  |  |  |  |  |  | x |  |  |  |
| `bramblehide` |  |  |  |  |  | x |  |  |  |  |  |
| `runic_ward` |  |  |  |  |  | x |  |  |  |  |  |
| `resolute` |  |  |  |  |  | x | x |  |  |  |  |
| `ironroot` |  |  |  |  |  |  | x |  |  |  |  |
| `quick_recovery` |  |  |  |  | x |  |  |  |  |  |  |
| `purity` |  |  |  |  | x | x |  |  |  |  |  |
| `coldblood` |  |  |  |  |  |  | x | x |  |  |  |
| `fleetfoot` |  |  |  |  |  |  | x |  |  |  |  |
| `fleetfoot_hc` |  |  |  |  |  |  | x |  |  |  |  |
| `stride` |  |  |  |  |  |  | x |  |  |  |  |
| `ghostwalk` |  |  |  |  |  |  |  | x |  |  |  |
| `pack_mule` |  |  |  |  |  |  | x | x |  |  |  |
| `momentum` |  |  |  |  |  |  | x |  |  |  |  |
| `pathfinder` |  |  |  |  |  |  | x |  |  |  |  |
| `mountain_goat` |  |  |  |  |  |  | x |  |  |  |  |
| `marshstrider` |  |  |  |  |  |  | x |  |  |  |  |
| `strong_swimmer` |  |  |  |  |  |  |  | x |  |  |  |
| `spring_heeled` |  |  |  |  |  |  | x |  |  |  |  |
| `soft_landing` |  |  |  |  |  |  | x | x |  |  |  |
| `ravens_glide` |  |  |  |  |  |  |  | x |  |  |  |
| `long_wind` |  |  |  |  |  |  | x |  |  |  |  |
| `light_leap` |  |  |  |  |  |  | x |  |  |  |  |
| `nimble` |  |  |  |  |  |  | x | x |  |  |  |
| `soft_tread` |  |  |  |  |  |  | x | x |  |  |  |
| `shadowmeld` |  |  |  |  | x |  |  | x |  |  |  |
| `ashen_skin` |  |  |  |  |  | x |  | x |  |  |  |
| `emberheart` |  |  |  |  |  | x |  | x |  |  |  |
| `winterborn` |  |  |  |  |  | x |  | x |  |  |  |
| `oilskin` |  |  |  |  | x |  |  | x |  |  |  |
| `sealegs` |  |  |  |  |  | x |  |  |  |  |  |
| `hearthlight` |  |  |  |  | x |  |  |  |  |  |  |
| `broad_back` |  |  |  |  |  | x |  |  | x |  |  |
| `magpie` |  |  |  |  |  |  |  | x | x |  |  |
| `huginns_eye` |  |  |  |  | x |  |  |  | x |  |  |
| `mistbane` |  |  |  |  | x |  |  |  | x |  |  |
| `hearthbound` |  |  |  |  |  | x |  | x | x |  |  |
| `gourmand` |  |  |  |  |  | x |  |  | x |  |  |
| `soulbound` |  |  |  |  | x |  |  |  | x |  |  |
| `mimirs_insight` |  |  |  |  | x |  |  |  | x |  |  |
| `brewers_haste` |  |  |  |  | x |  |  |  | x |  |  |
| `forsaken_favour` |  |  |  |  | x |  |  |  | x |  |  |
| `reflex_draught` |  |  |  |  |  | x |  |  | x |  |  |
| `swift_draught` |  |  |  |  |  |  |  |  | x |  |  |
| `norns_favour` |  |  |  |  | x |  |  |  | x |  |  |
| `fateweaver` |  |  |  |  | x |  |  |  | x |  |  |
| `trophy_taker` |  |  |  |  |  |  |  |  | x |  |  |
| `hoardfinder` |  |  |  |  |  |  |  |  | x |  |  |
| `harvester` |  |  |  |  |  |  |  |  | x |  |  |
| `beast_whisperer` |  |  |  |  |  |  |  | x | x |  |  |
| `fair_winds` |  |  |  |  |  |  |  | x | x |  |  |
| `builders_reach` |  |  |  |  |  |  |  |  |  | x | traits builds |
| `tireless_hands` |  |  |  |  |  |  |  |  |  | x | traits builds |
| `deep_vein` |  |  |  |  |  |  |  |  |  | x | skill Pickaxes |

</div>

The Mythic-only rows are in `affixes-mythic.md`.

## Pool size per slot

| slot | regular affixes | of which Phase 1 | mythic_only |
|---|---|---|---|
| melee (`melee_weapon`) | 55 | 22 | 1 |
| ranged (`ranged_weapon`) | 39 | 17 | 1 |
| magic (`magic_weapon`) | 24 | 7 | 1 |
| shield (`shield`) | 13 | 7 | 3 |
| head (`head`) | 29 | 10 | 1 |
| chest (`chest`) | 31 | 7 | 7 |
| legs (`legs`) | 33 | 13 | 2 |
| cape (`cape`) | 37 | 10 | 7 |
| utility (`utility_item`) | 22 | 6 | 1 |
| tool (`tool`) | 10 | 7 | 1 |

## How many affixes an item can carry

The most affixes one item can hold (distinct groups plus ungrouped affixes, after `requires`), with only Phase 1
affixes enabled and with the full catalog, for an item whose ceiling is tier 1 and tier 7. A rarity whose minimum is
above this number cannot be reached on that item: stones refuse and pre-rolled drops fall back (`rarity.md`).
Legendary needs 4-5 and Mythic 6.

| item | Phase 1, Meadows item | Phase 1, Ashlands item | full catalog, Meadows | full catalog, Ashlands |
|---|---|---|---|---|
| sword | 9 | 9 | 28 | 30 |
| axe (one-handed) | 9 | 9 | 29 | 32 |
| spear | 9 | 9 | 28 | 31 |
| atgeir | 9 | 9 | 27 | 29 |
| bow | 9 | 9 | 24 | 26 |
| crossbow | 9 | 9 | 23 | 24 |
| elemental staff | 5 | 6 | 10 | 16 |
| blood staff | 5 | 6 | 9 | 16 |
| round shield | 7 | 7 | 8 | 10 |
| tower shield | 6 | 6 | 6 | 8 |
| helmet | 8 | 10 | 21 | 25 |
| chest piece | 7 | 7 | 19 | 25 |
| leg piece | 13 | 13 | 27 | 27 |
| cape | 8 | 10 | 21 | 29 |
| belt (utility) | 6 | 6 | 17 | 20 |
| hammer / hoe / cultivator | 4 | 4 | 4 | 4 |
| pickaxe | 3 | 3 | 4 | 4 |
| fishing rod | 3 | 3 | 3 | 3 |

Tools are the thin class: sane tool affixes are few; accepted (`../DECISIONS.md` AFX-4). Staves at tier 1 are theoretical (staves
are Mistlands items).

---

# 3. The effect registry

Every effect id the code must implement. This is the **authoritative list** (`effects-runtime.md` defers to it).
Columns follow `effects-runtime.md`'s registry fields; `hook` is the difficulty tag; `phase` is the earliest phase
any affix needs it. Signatures named here were read in a decompile of `assembly_valheim.dll` on 2026-09-23 but are
**to be re-verified against a fresh decompile when each hook is built** (PLAN.md).

**Routes.** `aggregate`: a channel on the one hidden status effect (`ECF_Aggregate`, `effects-runtime.md` section 3);
the named `StatusEffect` virtual reads the channel total. `hook`: a targeted prefix or postfix, or a field written on
rebuild.

**Param kinds.** `none`; `skill` (a `Skills.SkillType` name, a comma list of them, or `All`); `damage_type` (`blunt`,
`slash`, `pierce`, `fire`, `frost`, `lightning`, `poison`, `spirit`, and the groups `physical` = blunt+slash+pierce,
`elemental` = fire+frost+lightning+poison, `all`); `element` (`fire`, `frost`, `lightning`, `poison`);
`creature_family` (below); `resource` (`health`, `stamina`, `eitr`); `aura` (`damage_dealt`, `damage_taken`).

**Creature families** (`creature_family` param, by the game's `Character.Faction`): `undead` = Undead, `beasts` =
AnimalsVeg, `sea` = SeaMonsters, `boss` = Boss faction or any character the game flags as a boss. Which vanilla
creatures sit in which faction is the game's data, not ours; no further families in v1 (`../DECISIONS.md` AFX-7).

**Caps** are on the channel sum (all equipped items), per `effects-runtime.md` section 5. `-` means none; item-local
effects are never summed, so they have none. The cap values are judgement calls.

<div style="overflow-x:auto">

| effect | param | values | route | polarity | cap | hook | phase | what the code hooks | used by |
|---|---|---|---|---|---|---|---|---|---|
| `health_recovery` | none | percent | aggregate | raise | 150 | easy | 1 | aggregate SE `ModifyHealthRegen`: multiplier on health regeneration | `troll_blood`, `troll_blood_hc` |
| `stamina_recovery` | none | percent | aggregate | raise | 100 | easy | 1 | aggregate SE `ModifyStaminaRegen` | `second_wind` |
| `eitr_recovery` | none | percent | aggregate | raise | 100 | easy | 1 | aggregate SE `ModifyEitrRegen` | `seidr_flow` |
| `health_recovery_flat` | none | flat | aggregate | raise | - | easy | 2 | aggregate SE `UpdateStatusEffect`: heal X every 10 s on its own timer, food or not | `mending` |
| `move_speed` | none | percent | aggregate | raise | 25 | easy | 1 | aggregate SE `ModifySpeed`: all ground movement | `fleetfoot`, `fleetfoot_hc` |
| `move_speed_sprint` | none | percent | aggregate | raise | 30 | easy | 2 | aggregate SE `ModifySpeed`, only while running | `stride` |
| `move_speed_sneak` | none | percent | aggregate | raise | 50 | easy | 1 | aggregate SE `ModifySpeed`, only while crouched | `ghostwalk` |
| `move_speed_encumbered` | none | percent | aggregate | raise | 50 | easy | 2 | aggregate SE `ModifySpeed`, only while encumbered | `pack_mule` |
| `move_speed_after_dodge` | none | percent | aggregate | raise | 40 | easy | 2 | aggregate SE `ModifySpeed` for 5 s; timer started by a postfix on `Player.Dodge` | `momentum` |
| `move_speed_paved` | none | percent | aggregate | raise | 30 | medium | 2 | aggregate SE `ModifySpeed` when the ground under the player is a paved/path material (ground detection is the work) | `pathfinder` |
| `slope_penalty` | none | percent | aggregate | lower | 75 | medium | 2 | reduce the uphill steep-slope slowdown (find where the game applies it; may not be an SE path) | `mountain_goat` |
| `terrain_slow` | none | percent | aggregate | lower | 75 | medium | 2 | reduce tar and shallow-water slowdown (status/ground slows; per-source check) | `marshstrider` |
| `swimmer` | none | percent | aggregate | raise | 50 | easy | 2 | aggregate SE `ModifySpeed` while swimming (+X% swim speed) and `ModifySwimStaminaUsage` (-X% swim stamina) | `strong_swimmer` |
| `jump_height` | none | percent | aggregate | raise | 50 | easy | 1 | aggregate SE `ModifyJump` | `spring_heeled` |
| `fall_damage_taken` | none | percent | aggregate | lower | 80 | easy | 1 | aggregate SE `ModifyFallDamage` | `soft_landing` |
| `slow_fall` | none | flag | aggregate | raise | - | easy | 2 | aggregate SE `ModifyWalkVelocity` (clamp fall speed) + `ModifyFallDamage` to 0 | `ravens_glide` |
| `carry_capacity` | none | flat | aggregate | raise | - | easy | 1 | aggregate SE `ModifyMaxCarryWeight` | `broad_back` |
| `noise_made` | none | percent | aggregate | lower | 75 | easy | 2 | aggregate SE `ModifyNoise` | `soft_tread` |
| `stealth` | none | percent | aggregate | raise | 75 | easy | 2 | aggregate SE `ModifyStealth` (how hard you are to see while sneaking) | `shadowmeld` |
| `run_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyRunStaminaDrain` | `long_wind` |
| `jump_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyJumpStaminaUsage` | `light_leap` |
| `attack_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyAttackStaminaUsage` | `balanced_grip` |
| `block_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyBlockStaminaUsage` | `tireless_guard` |
| `dodge_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyDodgeStaminaUsage` | `nimble` |
| `home_item_stamina_cost` | none | percent | aggregate | lower | 60 | easy | 1 | aggregate SE `ModifyHomeItemStaminaUsage` | `tireless_hands` |
| `skill_level` | skill | flat | aggregate | raise | - | easy | 1 | aggregate SE `ModifySkillLevel` for the param skill(s) | 18 affixes (`blade_mastery`, `axe_mastery`, ...) |
| `skill_gain` | skill | percent | aggregate | raise | 100 | easy | 1 | aggregate SE `ModifyRaiseSkill` (param `All` = every skill) | `mimirs_insight` |
| `parry_bonus` | none | percent | aggregate | raise | - | easy | 1 | aggregate SE `ModifyTimedBlockBonus` | `perfect_guard` |
| `stagger_taken` | none | percent | aggregate | lower | 75 | easy | 2 | aggregate SE `ModifyStagger` (the stagger you build up when hit) | `resolute` |
| `damage_dealt` | damage_type | percent | aggregate | raise | - | easy | 1 | aggregate SE `ModifyAttack(skill, ref HitData)`: scale the param damage group of every hit you make | `honed_might`, `primal_fury`, `berserkergang` |
| `night_damage` | none | percent | aggregate | raise | - | easy | 1 | aggregate SE `ModifyAttack`, only while the game says it is night | `nightstalker` |
| `surprise_bonus` | none | percent | aggregate | raise | - | easy | 2 | aggregate SE `ModifyAttack`: raise `HitData.m_backstabBonus` (the sneak-attack multiplier) | `ambusher` |
| `stagger_power` | none | percent | aggregate | raise | - | easy | 2 | aggregate SE `ModifyAttack`: raise `HitData.m_staggerMultiplier` | `staggering_blows` |
| `coin_damage` | none | percent | aggregate | raise | - | easy | 2 | aggregate SE `ModifyAttack`; coin count cached on inventory change, never counted per hit | `fafnirs_greed` |
| `resist_modifier` | element | flag | aggregate | raise | - | easy | 2 | aggregate SE `ModifyDamageMods`: the game's own Resistant modifier for the param element (best-of, like capes) | `bulwark_fire`, `bulwark_frost`, `bulwark_lightning`, `bulwark_poison` |
| `freeze_immunity` | none | flag | aggregate | raise | - | easy | 2 | aggregate SE carries the game's `ColdResistance` status attribute (the frost-mead flag) | `winterborn` |
| `hc_threshold` | none | percent | aggregate | raise | 20 | easy | 2 | read by the health-critical condition evaluator; no game hook | `valhallas_edge` |
| `max_health` | none | flat | hook | raise | - | easy | 1 | postfix `Player.GetTotalFoodValue` (hp) | `vigor` |
| `max_stamina` | none | flat | hook | raise | - | easy | 1 | postfix `Player.GetTotalFoodValue` (stamina) | `endurance` |
| `max_eitr` | none | flat | hook | raise | - | easy | 1 | postfix `Player.GetTotalFoodValue` (eitr) | `wellspring` |
| `health_for_regen` | none | flat | hook | raise | - | easy | 2 | composite: +X on the `max_health` channel and -X on the `health_recovery` channel | `stout_heart` |
| `eitr_for_regen` | none | percent | hook | raise | - | easy | 2 | composite: +X on `eitr_recovery`, and max eitr -X/2 % (postfix `GetTotalFoodValue`, eitr) | `restless_mind` |
| `item_armor` | none | percent | hook | raise | - | easy | 1 | postfix `ItemData.GetArmor(int, float)` on this item | `hardened`, `hardened_hc` |
| `item_block` | none | percent | hook | raise | - | easy | 1 | postfix `ItemData.GetBaseBlockPower(int)` on this item | `stalwart` |
| `item_deflection` | none | percent | hook | raise | - | easy | 1 | postfix `ItemData.GetDeflectionForce(int)` on this item | `repelling_guard` |
| `item_durability` | none | percent | hook | raise | - | easy | 1 | postfix `ItemData.GetMaxDurability(int)` on this item | `well_forged` |
| `item_unbreakable` | none | flag | hook | raise | - | medium | 2 | prefix on every durability-drain site for this item (attack, block, armor hit, tool use): several call sites | `everlasting` |
| `item_lighten` | none | percent | hook | lower | - | easy | 1 | postfix `ItemData.GetWeight(int)` / `GetNonStackedWeight` on this item | `lightened` |
| `item_zero_weight` | none | flag | hook | raise | - | easy | 2 | same postfix as `item_lighten`, returns 0 | `gossamer` |
| `item_no_move_penalty` | none | flag | hook | raise | - | easy | 2 | postfix `Player.GetEquipmentMovementModifier`: add back this item's negative modifier | `supple_fit` |
| `brand_damage` | damage_type | percent | hook | raise | - | easy | 1 | postfix `ItemData.GetDamage(int, float)`: add X% of the item's own base damage as the param type (hot path: cached record only) | 8 affixes (`emberbrand`, `rimebrand`, ...) |
| `attack_eitr_cost` | none | percent | hook | lower | 60 | easy | 1 | postfix `Attack.GetAttackEitr(Character, ItemData)` | `seidr_thrift` |
| `attack_health_cost` | none | percent | hook | lower | 60 | easy | 2 | postfix `Attack.GetAttackHealth` (private instance method) | `blood_thrift` |
| `draw_stamina_cost` | none | percent | hook | lower | 60 | easy | 1 | postfix `ItemData.GetDrawStaminaDrain` | `easy_draw` |
| `reload_speed` | none | percent | hook | raise | 50 | easy | 1 | postfix `ItemData.GetWeaponLoadingTime` (shorter) | `quick_windlass` |
| `draw_speed` | none | percent | hook | raise | 50 | medium | 2 | bow draw time: the draw percentage reads the shared attack's minimum draw duration, not a per-swing copy | `swift_string` |
| `attack_reach` | none | percent | hook | raise | - | easy | 2 | prefix `Attack.Start` on the per-swing clone (Humanoid.StartAttack clones the item's Attack each swing): `m_attackRange` | `long_reach` |
| `attack_arc` | none | flat | hook | raise | - | easy | 2 | same clone: `m_attackAngle` (degrees) | `sweeping_arc` |
| `projectile_velocity` | none | percent | hook | raise | - | easy | 2 | same clone: projectile velocity | `true_flight` |
| `draw_move_penalty` | none | percent | hook | lower | - | medium | 2 | same clone: the attack's movement slowdown while drawing/aiming (`m_speedFactor`); verify it is what slows the draw | `skirmisher` |
| `heat_resist` | none | percent | hook | raise | - | easy | 2 | postfix `Player.GetEquipmentHeatResistanceModifier` | `ashen_skin` |
| `pickup_radius` | none | percent | hook | raise | 100 | easy | 1 | written into `Player.m_autoPickupRange` on aggregate rebuild (base kept) | `magpie` |
| `build_range` | none | percent | hook | raise | 100 | easy | 1 | written into `Player.m_maxPlaceDistance` on rebuild while the tool is in hand | `builders_reach` |
| `explore_radius` | none | percent | hook | raise | 100 | easy | 1 | written into the local minimap's `m_exploreRadius` on rebuild | `huginns_eye` |
| `rest_comfort` | none | flat | hook | raise | 3 | easy | 2 | postfix `SE_Rested.CalculateComfortLevel(Player)` | `hearthbound` |
| `skill_loss` | none | percent | hook | lower | 75 | easy | 2 | prefix `Skills.LowerAllSkills(float factor)`: scale the factor | `soulbound` |
| `food_duration` | none | percent | hook | raise | 100 | easy | 2 | postfix on the eat-food path: extend the new food's remaining time | `gourmand` |
| `damage_taken` | damage_type | percent | hook | lower | 60 | easy | 2 | prefix `Character.RPC_Damage` on the local player: scale the param damage types of the incoming hit (verify ordering vs armor) | 9 affixes (`padded`, `mailed`, ...) |
| `ranged_damage_taken` | none | percent | hook | lower | 60 | easy | 2 | same prefix, only when `HitData.m_ranged` | `arrowward` |
| `avoid_hit` | none | percent | hook | raise | 25 | easy | 2 | same prefix: chance to discard the whole hit | `mist_veil`, `mist_veil_hc` |
| `knockback_taken` | none | percent | hook | lower | 75 | easy | 2 | prefix `Character.ApplyPushback(HitData)` on the local player | `ironroot` |
| `forsaken_cooldown` | none | percent | hook | lower | 50 | easy | 2 | postfix on forsaken power activation: shorten the cooldown just set | `forsaken_favour` |
| `slayer` | creature_family | percent | hook | raise | - | easy | 2 | prefix `Character.Damage(HitData)` on the attacking client (target known): scale when the target is in the param family | `slayer_undead`, `slayer_beasts`, `slayer_sea`, `godslayer` |
| `staggered_target_damage` | none | percent | hook | raise | - | easy | 2 | same attacker-side prefix, when the target is staggered | `press_the_advantage` |
| `low_health_opener` | none | percent | hook | raise | - | medium | 2 | same attacker-side prefix, target below 20% health, first hit only (per-target memory) | `deathblow` |
| `exploit_stagger` | none | percent | hook | raise | - | medium | 2 | same attacker-side prefix: chance that a hit on a staggered target is treated as a sneak attack | `cruel_opening` |
| `on_hit_slow` | none | percent | hook | raise | - | medium | 2 | own status effect on the hit (`HitData.m_statusEffectHash`), applied by the target's owner; bosses refuse it | `hamstring` |
| `stagger_duration_dealt` | none | percent | hook | raise | - | medium | 2 | target-side: lengthen stagger you cause (needs the attacker's value carried in the hit) | `dazing_blows` |
| `chain_arc` | none | percent | hook | raise | - | hard | 3 | on hit, chance to spawn secondary lightning hits on nearby enemies (spawning, targeting, ownership) | `thors_arc` |
| `impact_burst` | none | percent | hook | raise | - | hard | 3 | on projectile impact, area damage around the point | `shatterhead` |
| `swing_speed` | none | percent | hook | raise | - | hard | 3 | animator speed per swing; must stay in sync for other clients watching | `quickened_blows` |
| `cast_rate` | none | percent | hook | raise | - | hard | 3 | staff projectile cadence (burst interval / animation) | `flowing_seidr` |
| `multishot` | none | flag | hook | raise | - | medium | 2 | per-shot clone: three projectiles in a spread, three ammo consumed | `volley` |
| `twincast` | none | flag | hook | raise | - | medium | 2 | per-cast clone: projectile count x2, eitr cost x2 | `twincast` |
| `ammo_save` | none | percent | hook | raise | 50 | medium | 2 | prefix on ammo consumption: chance to refund | `thrifty_quiver` |
| `rune_edge` | none | percent | hook | raise | - | medium | 2 | per-swing clone: half the stamina cost becomes eitr cost; +X% damage via `ModifyAttack` while it applies | `rune_edge` |
| `blood_price` | none | flag | hook | raise | - | medium | 2 | per-swing clone: the stamina cost becomes an equal health cost; refused when it would kill you | `blood_price` |
| `combo_finisher` | none | percent | hook | raise | - | medium | 2 | third hit of a combo chain landed in rhythm grants a short stagger-immune, -X% damage-taken window | `steel_rhythm` |
| `lone_blade` | none | percent | hook | raise | - | medium | 2 | with the off-hand empty: block armor +X% of the weapon's attack power, parry force +X/2 % | `lone_blade` |
| `dodge_fury` | none | percent | hook | raise | - | medium | 2 | dodging through a melee hit (i-frames) grants +X% damage for 10 s, no refresh while active | `evaders_fury` |
| `on_kill_restore` | resource | flat | hook | raise | - | medium | 2 | kill attribution: restore X of the param resource to the killer (kill credited on the attacker's client) | `reaper`, `soul_reaper` |
| `leech` | resource | percent | hook | raise | - | medium | 2 | heal/restore X% of damage dealt, attacker-side estimate from the outgoing hit (armor not known there) | `blood_drinker`, `blood_drinker_hc`, `seidr_siphon` |
| `thorns` | none | percent | hook | raise | - | medium | 2 | on being hit in melee, return X% of the hit to the attacker (routed damage RPC to the attacker's owner) | `bramblehide` |
| `calm_ward` | none | flat | hook | raise | - | medium | 2 | after 10 s without taking damage, a ward absorbs the next X damage (own status effect) | `runic_ward` |
| `debuff_decay` | none | percent | hook | raise | 75 | medium | 2 | burning/poison/frost status effects on you expire X% faster (shorten their ttl on add) | `purity` |
| `frost_slow_taken` | none | percent | hook | lower | 100 | medium | 2 | reduce the movement slow from the frost status effect on you | `coldblood` |
| `stagger_recovery` | none | percent | hook | lower | 75 | medium | 2 | shorten your own stagger animation/lockout | `quick_recovery` |
| `block_steadfast` | none | flag | hook | raise | - | medium | 2 | while blocking with this shield: no knockback and no stagger from blocked hits | `anchored_guard` |
| `perfect_block_window` | none | flat | hook | raise | 150 | medium | 2 | widen the perfect-block timing window by X ms (block timer read in `Humanoid.BlockAttack`) | `keen_guard` |
| `parry_restore` | resource | flat | hook | raise | - | medium | 2 | postfix `Humanoid.BlockAttack` on a perfect block: restore X of the param resource | `seidr_riposte` |
| `summon_damage` | none | percent | hook | raise | - | medium | 2 | creatures summoned by this staff deal +X% (tag them at spawn) | `grave_command` |
| `summon_health` | none | percent | hook | raise | - | medium | 2 | creatures summoned by this staff get +X% health | `grave_vigor` |
| `auto_mead` | none | flag | hook | raise | - | medium | 2 | entering health-critical drinks the best healing mead from your inventory (respects the game's mead cooldown) | `reflex_draught` |
| `mead_burst` | none | flag | hook | raise | - | medium | 2 | while health-critical, healing meads deliver their whole heal at once | `swift_draught` |
| `mead_cooldown` | none | percent | hook | lower | 50 | medium | 2 | shorten the shared mead cooldown after drinking | `brewers_haste` |
| `light_aura` | none | flag | hook | raise | - | medium | 2 | a soft light on the player; visible to others needs a synced player flag (other clients do not see your items' custom data) | `hearthlight` |
| `rain_shield` | none | flag | hook | raise | - | medium | 2 | rain never applies Wet; water immersion still does (environment status update) | `oilskin` |
| `ignore_wet` | none | flag | hook | raise | - | medium | 2 | the Wet status's regen penalties do not apply to you | `sealegs` |
| `cold_immunity` | none | flag | hook | raise | - | medium | 2 | you never get the Cold status (night/wet chill); Freezing is separate | `emberheart` |
| `demist_radius` | none | percent | hook | raise | 100 | medium | 2 | larger mist-clearing radius while a mist-clearing item is worn | `mistbane` |
| `taming_speed` | none | percent | hook | raise | 100 | medium | 2 | creature-owner side: tame progress faster when you are near; your value travels as a small player stat | `beast_whisperer` |
| `sail_speed` | none | percent | hook | raise | 50 | medium | 2 | ship sail force +X% while you are the one steering | `fair_winds` |
| `find_rarity` | none | percent | hook | raise | 200 | medium | 2 | killer-stat plumbing: magic-gear drops from your kills roll a higher rarity more often | `norns_favour` |
| `find_stones` | none | percent | hook | raise | 200 | medium | 2 | killer-stat plumbing: extra stone-drop roll chance from your kills | `fateweaver` |
| `find_trophy` | none | percent | hook | raise | 200 | medium | 2 | killer-stat plumbing: trophy drop chance x(1+X%) (creature owner rolls the drop) | `trophy_taker` |
| `find_coins` | none | percent | hook | raise | 200 | medium | 2 | killer-stat plumbing: coin and treasure drop chance x(1+X%) | `hoardfinder` |
| `yield_mining` | none | flat | hook | raise | - | medium | 2 | rock/ore destroyed by your hit drops X extra of its resource (object owner rolls; attacker in the hit) | `deep_vein` |
| `yield_lumber` | none | flat | hook | raise | - | medium | 2 | tree/log destroyed by your hit drops X extra wood | `heartwood` |
| `yield_pickable` | none | percent | hook | raise | 100 | medium | 2 | chance to double the yield of plants you pick (pick RPC on the pickable's owner) | `harvester` |
| `throw_secondary` | none | flag | hook | raise | - | hard | 3 | the weapon's secondary attack becomes a throw (needs a projectile attack cloned onto a weapon that has none) | `hurler` |
| `throw_return` | none | flag | hook | raise | - | hard | 3 | a thrown weapon comes back to your inventory after it lands or hits | `faithful_throw` |
| `throw_teleport` | none | flag | hook | raise | - | hard | 3 | when your thrown weapon hits a creature you are moved to it | `gungnir_path` |
| `portal_any_item` | none | flag | hook | raise | - | easy | 3 | postfix the teleportable check on the player's inventory: true while worn | `bifrost_blessing` |
| `undying` | none | flat | hook | raise | - | medium | 3 | a killing blow leaves you at 1 health instead; cooldown X minutes, shown as a status icon | `undying` |
| `parry_shockwave` | none | flat | hook | raise | - | medium | 3 | a perfect block staggers every enemy within X m (routed stagger to each target's owner) | `thunderclap` |
| `party_aura` | aura | percent | hook | raise | - | medium | 3 | nearby party members get +X% damage dealt or -X% damage taken; each client applies it for itself from a synced player flag | `warbanner`, `aegis` |
| `blink_dodge` | none | flag | hook | raise | - | hard | 3 | the dodge roll becomes a short teleport in the dodge direction (collision-safe placement) | `blink` |
| `hit_cap` | none | percent | hook | lower | - | medium | 3 | no single hit takes more than X% of your maximum health (after armor, on the local player) | `allfathers_bulwark` |
| `element_absorb` | element | percent | hook | raise | - | medium | 3 | damage of the param element is negated and X% of it heals you | `pact_fire`, `pact_frost`, `pact_lightning`, `pact_poison` |
| `stationless_build` | none | flag | hook | raise | - | easy | 3 | the build-station-in-range check passes while this tool is in hand | `master_builder` |
| `extra_jump` | none | flag | hook | raise | - | medium | 3 | one extra jump while airborne, normal jump stamina; reset on landing | `valkyrie_leap` |

</div>

**These effect ids are authoritative**; every other file uses them (`../DECISIONS.md` RC-5).

**Hook counts**: 70 easy, 53 medium, 8 hard.

---

# 4. The catalog

One entry per affix:

- **`id`** Name — behaviour, with X the rolled value.
  effect `param` · value type (unit) · slots · category · [condition] · exclusion group · hook · phase · affix weight
  tier table · requirements

`P1`, `P2`, `P3` is the phase that builds the affix: P1 is section 6; P2 is every other easy or medium effect;
P3 is hard hooks and the Mythic pool.

## Weapons: player-global offense

Offense that follows the player, not the item, lives on weapons only (PLAN.md slot sanity). It counts while the weapon is in hand.

- **`honed_might`** Honed Might — The blunt, slash and pierce parts of your hits are +X%, including parts added by other affixes.  
  `damage_dealt` `physical` · percent · melee, ranged · offense · group `damage_physical` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15
- **`primal_fury`** Primal Fury — The fire, frost, lightning and poison parts of your hits are +X%, including parts added by other affixes.  
  `damage_dealt` `elemental` · percent · melee, ranged, magic · offense · group `damage_elemental` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15
- **`berserkergang`** Berserkergang — While health-critical, all damage you deal is +X%.  
  `damage_dealt` `all` · percent · melee, ranged, magic · offense · **health-critical** · group `damage_critical` · easy · P2 · w 60  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`slayer_undead`** Undead Slayer — +X% damage against the undead family.  
  `slayer` `undead` · percent · melee, ranged, magic · offense · group `slayer` · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`slayer_beasts`** Beast Slayer — +X% damage against the beasts family.  
  `slayer` `beasts` · percent · melee, ranged, magic · offense · group `slayer` · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`slayer_sea`** Sea Slayer — +X% damage against the sea family.  
  `slayer` `sea` · percent · melee, ranged, magic · offense · group `slayer` · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`godslayer`** Godslayer — +X% damage against bosses.  
  `slayer` `boss` · percent · melee, ranged, magic · offense · no group · easy · P2 · w 50  
  T3–T7: 9–12 / 12–15 / 15–18 / 18–22 / 22–26  
  *Note:* Tiers 3-7 only: there is one boss per biome, so an early roll is nearly dead.
- **`nightstalker`** Nightstalker — +X% damage while it is night.  
  `night_damage` · percent · melee, ranged, magic · offense · no group · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15
- **`ambusher`** Ambusher — Your sneak-attack multiplier (hits on unaware enemies) is +X%.  
  `surprise_bonus` · percent · melee, ranged · offense · no group · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`deathblow`** Deathblow — Your first hit on an enemy below 20% health deals +X%.  
  `low_health_opener` · percent · melee, ranged, magic · offense · no group · medium · P2 · w 100  
  T1–T7: 10–15 / 15–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–75
- **`cruel_opening`** Cruel Opening — X% chance that a hit on a staggered enemy counts as a sneak attack.  
  `exploit_stagger` · percent · melee, ranged · offense · no group · medium · P2 · w 60  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`press_the_advantage`** Press the Advantage — +X% damage against staggered enemies.  
  `staggered_target_damage` · percent · melee, ranged · offense · no group · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`reaper`** Reaper — Killing an enemy restores X stamina.  
  `on_kill_restore` `stamina` · flat · melee, ranged · offense · group `on_kill` · medium · P2 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`blood_drinker`** Blood Drinker — Heal X% of the damage your hits deal.  
  `leech` `health` · percent · melee, ranged · offense · group `leech_health` · medium · P2 · w 60  
  T1–T7: 1–1.5 / 1.5–2 / 2–2.5 / 2.5–3 / 3–3.5 / 3.5–4 / 4–5
- **`blood_drinker_hc`** Cornered Thirst — While health-critical, heal X% of the damage your hits deal.  
  `leech` `health` · percent · melee, ranged · offense · **health-critical** · group `leech_health` · medium · P2 · w 50  
  T1–T7: 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–9 / 9–10
- **`evaders_fury`** Evader's Fury — Dodging through a melee attack grants +X% damage for 10 s; does not refresh while active.  
  `dodge_fury` · percent · melee, ranged · offense · no group · medium · P2 · w 60  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`fafnirs_greed`** Fafnir's Greed — +X% damage per full 999 coins carried; each stack past the second counts half.  
  `coin_damage` · percent · melee · offense · no group · easy · P2 · w 40  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8  
  *Note:* Stack formula and a cap of 5 stacks are judgement calls (`../DECISIONS.md` AFX-15).

## Melee weapons (brands also roll on ranged weapons)

Brands add a share of the weapon's own base damage as a new type. One elemental and one physical brand at most per item (groups `brand_elemental`, `brand_physical`).

- **`emberbrand`** Emberbrand — Adds X% of this weapon's own base damage again, as fire. **[Phase 1 pick]**  
  `brand_damage` `fire` · percent · melee, ranged · offense · group `brand_elemental` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`rimebrand`** Rimebrand — Adds X% of this weapon's own base damage again, as frost.  
  `brand_damage` `frost` · percent · melee, ranged · offense · group `brand_elemental` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`stormbrand`** Stormbrand — Adds X% of this weapon's own base damage again, as lightning.  
  `brand_damage` `lightning` · percent · melee, ranged · offense · group `brand_elemental` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`venombrand`** Venombrand — Adds X% of this weapon's own base damage again, as poison.  
  `brand_damage` `poison` · percent · melee, ranged · offense · group `brand_elemental` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`spiritbrand`** Spiritbrand — Adds X% of this weapon's own base damage again, as spirit. Spirit only hurts the undead family; the roll is still useful.  
  `brand_damage` `spirit` · percent · melee, ranged · offense · group `brand_elemental` · easy · P1 · w 70  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`bonebreaker`** Bonebreaker — Adds X% of this weapon's own base damage again, as blunt.  
  `brand_damage` `blunt` · percent · melee, ranged · offense · group `brand_physical` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`keen_edge`** Keen Edge — Adds X% of this weapon's own base damage again, as slash. Axes also gain the same share as chop.  
  `brand_damage` `slash` · percent · melee, ranged · offense · group `brand_physical` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`needlepoint`** Needlepoint — Adds X% of this weapon's own base damage again, as pierce. (Pickaxes are tools and do not roll brands.)  
  `brand_damage` `pierce` · percent · melee, ranged · offense · group `brand_physical` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`staggering_blows`** Staggering Blows — Hits with this weapon build +X% stagger on the target.  
  `stagger_power` · percent · melee · offense · no group · easy · P2 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`dazing_blows`** Dazing Blows — Enemies you stagger stay staggered X% longer.  
  `stagger_duration_dealt` · percent · melee · offense · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`hamstring`** Hamstring — Hit enemies move and attack X% slower for 2 s. Bosses are immune.  
  `on_hit_slow` · percent · melee, ranged · offense · no group · medium · P2 · w 70  
  T1–T7: 5–8 / 8–10 / 10–12 / 12–15 / 15–18 / 18–21 / 21–25
- **`thors_arc`** Thor's Arc — X% chance on hit to arc lightning to up to 3 nearby enemies for half the hit's damage.  
  `chain_arc` · percent · melee, magic · offense · no group · hard · P3 · w 30  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18  
  *Note:* Arc count and the half-damage share are judgement calls.
- **`quickened_blows`** Quickened Blows — This weapon attacks X% faster.  
  `swing_speed` · percent · melee · offense · group `swing_speed` · hard · P3 · w 50  
  T1–T7: 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–10
- **`long_reach`** Long Reach — This weapon's melee range is +X%.  
  `attack_reach` · percent · melee · offense · no group · easy · P2 · w 100  
  T1–T7: 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–10  
  *Note:* Arguable: reach is strong in Valheim; the curve stops at 10%.
- **`sweeping_arc`** Sweeping Arc — This weapon's swing arc is X degrees wider.  
  `attack_arc` · flat (deg) · melee · offense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`balanced_grip`** Balanced Grip — Attacks with this weapon cost X% less stamina. **[Phase 1 pick]**  
  `attack_stamina_cost` · percent · melee · offense · group `attack_cost` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`blood_price`** Blood Price — Attacks cost health instead of stamina, one for one. Refused when the cost would kill you.  
  `blood_price` · flag · melee · offense · group `attack_cost` · medium · P2 · w 30  
  gate T4 (rolls at T4–T7, no value)
- **`rune_edge`** Rune-Edged — Half of each attack's stamina cost is paid in eitr instead; while it is, the attack deals +X%.  
  `rune_edge` · percent · melee · offense · group `attack_cost` · medium · P2 · w 30  
  T5–T7: 9–11 / 11–13 / 13–15  
  *Note:* Tiers 5-7: needs an eitr pool.
- **`lone_blade`** Lone Blade — With the off-hand empty: block armor +X% of this weapon's attack power, parry force +X/2 %.  
  `lone_blade` · percent · melee · offense · no group · medium · P2 · w 40  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35 · requires one-handed
- **`steel_rhythm`** Steel Rhythm — Landing the third hit of a combo in rhythm makes you stagger-immune and take X% less damage for 2 s.  
  `combo_finisher` · percent · melee · defense · no group · medium · P2 · w 40  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35  
  *Note:* Window length (2 s) is a judgement call.
- **`hurler`** Hurler — This weapon's secondary attack becomes a throw.  
  `throw_secondary` · flag · melee · utility · no group · hard · P3 · w 25  
  gate T2 (rolls at T2–T7, no value) · requires skill Knives/Axes/Clubs; one-handed
- **`faithful_throw`** Faithful Throw — A thrown spear returns to your inventory after it lands or hits.  
  `throw_return` · flag · melee · utility · group `throw_follow` · hard · P3 · w 20  
  gate T2 (rolls at T2–T7, no value) · requires skill Spears
- **`gungnir_path`** Gungnir's Path — When your thrown spear hits a creature you are carried to it.  
  `throw_teleport` · flag · melee · utility · group `throw_follow` · hard · P3 · w 15  
  gate T5 (rolls at T5–T7, no value) · requires skill Spears
- **`heartwood`** Heartwood — Trees and logs you fell drop X extra wood.  
  `yield_lumber` · flat · melee · utility · no group · medium · P2 · w 70  
  T1–T7: 1 / 1 / 1–2 / 1–2 / 1–2 / 2 / 2–3 · requires skill WoodCutting

## Ranged weapons

Bows and crossbows. Draw affixes carry `requires.skill: [Bows]`, the reload affix `[Crossbows]`.

- **`easy_draw`** Easy Draw — Holding this bow drawn drains X% less stamina. **[Phase 1 pick]**  
  `draw_stamina_cost` · percent · ranged · offense · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18 · requires skill Bows
- **`swift_string`** Swift String — This bow draws X% faster.  
  `draw_speed` · percent · ranged · offense · no group · medium · P2 · w 60  
  T1–T7: 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–10 · requires skill Bows
- **`quick_windlass`** Quick Windlass — This crossbow reloads X% faster.  
  `reload_speed` · percent · ranged · offense · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18 · requires skill Crossbows
- **`true_flight`** True Flight — Projectiles from this weapon fly X% faster.  
  `projectile_velocity` · percent · ranged, magic · offense · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50 · requires traits projectile
- **`thrifty_quiver`** Thrifty Quiver — X% chance that a shot does not use up its ammunition.  
  `ammo_save` · percent · ranged · utility · no group · medium · P2 · w 70  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`volley`** Volley — This bow looses three arrows in a spread and uses three.  
  `multishot` · flag · ranged · offense · no group · medium · P2 · w 20  
  gate T4 (rolls at T4–T7, no value) · requires skill Bows
- **`shatterhead`** Shatterhead — Projectiles burst on impact, dealing X% of the hit to enemies within 2 m.  
  `impact_burst` · percent · ranged · offense · no group · hard · P3 · w 25  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–18 / 18–22 / 22–26
- **`skirmisher`** Skirmisher — Drawing or aiming this weapon slows you X% less.  
  `draw_move_penalty` · percent · ranged · utility · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50  
  *Note:* Movement-flavoured but weapon-local: it only matters with this weapon drawn, so it lives on the weapon, not the legs.

## Magic weapons

Staves. Most scale from tier 5: staves arrive with the Mistlands, and an eitr pool before then is a curiosity.

- **`seidr_thrift`** Seidr Thrift — Attacks with this staff cost X% less eitr. **[Phase 1 pick]**  
  `attack_eitr_cost` · percent · magic · offense · group `attack_cost` · easy · P1 · w 100  
  T5–T7: 11–13 / 13–15 / 15–18
- **`blood_thrift`** Blood Thrift — Attacks with this staff cost X% less health.  
  `attack_health_cost` · percent · magic · offense · group `attack_cost_health` · easy · P2 · w 100  
  T5–T7: 11–13 / 13–15 / 15–18 · requires skill BloodMagic
- **`twincast`** Twincast — This staff casts every projectile twice, for twice the eitr.  
  `twincast` · flag · magic · offense · no group · medium · P2 · w 20  
  gate T6 (rolls at T6–T7, no value) · requires skill ElementalMagic
- **`flowing_seidr`** Flowing Seidr — This staff fires X% faster.  
  `cast_rate` · percent · magic · offense · group `swing_speed` · hard · P3 · w 50  
  T5–T7: 6–7 / 7–8 / 8–10 · requires skill ElementalMagic
- **`soul_reaper`** Soul Reaper — Killing an enemy restores X eitr.  
  `on_kill_restore` `eitr` · flat · magic · offense · group `on_kill` · medium · P2 · w 100  
  T5–T7: 11–13 / 13–15 / 15–18
- **`seidr_siphon`** Seidr Siphon — Restore eitr equal to X% of the damage your hits deal.  
  `leech` `eitr` · percent · melee, magic · offense · no group · medium · P2 · w 60  
  T5–T7: 3–3.5 / 3.5–4 / 4–5
- **`grave_command`** Grave-Lord's Command — Creatures summoned with this staff deal +X% damage.  
  `summon_damage` · percent · magic · offense · no group · medium · P2 · w 100  
  T5–T7: 15–18 / 18–22 / 22–26 · requires skill BloodMagic
- **`grave_vigor`** Grave Vigor — Creatures summoned with this staff have +X% health.  
  `summon_health` · percent · magic · defense · no group · medium · P2 · w 100  
  T5–T7: 15–18 / 18–22 / 22–26 · requires skill BloodMagic

## Skill affixes

One affix per skill, all on effect `skill_level`, all in group `skill_level` (one skill affix per item). Each rolls only on items governed by its skill (`requires.skill`); the two body-skill affixes, which no item governs, sit on armor and utility items instead.

- **`blade_mastery`** Blade Mastery — +X skill levels in Swords while equipped.  
  `skill_level` `Swords` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Swords
- **`axe_mastery`** Axe Mastery — +X skill levels in Axes while equipped.  
  `skill_level` `Axes` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Axes
- **`club_mastery`** Club Mastery — +X skill levels in Clubs while equipped.  
  `skill_level` `Clubs` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Clubs
- **`knife_mastery`** Knife Mastery — +X skill levels in Knives while equipped.  
  `skill_level` `Knives` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Knives
- **`spear_mastery`** Spear Mastery — +X skill levels in Spears while equipped.  
  `skill_level` `Spears` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Spears
- **`polearm_mastery`** Polearm Mastery — +X skill levels in Polearms while equipped.  
  `skill_level` `Polearms` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Polearms
- **`fist_mastery`** Fist Mastery — +X skill levels in Unarmed while equipped.  
  `skill_level` `Unarmed` · flat · melee · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Unarmed
- **`bow_mastery`** Bow Mastery — +X skill levels in Bows while equipped.  
  `skill_level` `Bows` · flat · ranged · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Bows
- **`crossbow_mastery`** Crossbow Mastery — +X skill levels in Crossbows while equipped.  
  `skill_level` `Crossbows` · flat · ranged · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Crossbows
- **`elemental_mastery`** Elemental Mastery — +X skill levels in Elemental Magic while equipped.  
  `skill_level` `ElementalMagic` · flat · magic · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill ElementalMagic
- **`blood_mastery`** Blood Mastery — +X skill levels in Blood Magic while equipped.  
  `skill_level` `BloodMagic` · flat · magic · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill BloodMagic
- **`woodcutting_mastery`** Woodcutter's Mastery — +X skill levels in Woodcutting while equipped.  
  `skill_level` `WoodCutting` · flat · melee · utility · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill WoodCutting
- **`pick_mastery`** Miner's Mastery — +X skill levels in Pickaxes while equipped.  
  `skill_level` `Pickaxes` · flat · tool · utility · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Pickaxes
- **`fishing_mastery`** Angler's Mastery — +X skill levels in Fishing while equipped.  
  `skill_level` `Fishing` · flat · tool · utility · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Fishing
- **`shield_mastery`** Shield Mastery — +X skill levels in Blocking while equipped.  
  `skill_level` `Blocking` · flat · shield · offense · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Blocking
- **`wanderer_mastery`** Wanderer's Mastery — +X skill levels in Run, Jump, Swim and Sneak while equipped.  
  `skill_level` `Run,Jump,Swim,Sneak` · flat · legs, cape · utility · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15
- **`artisan_mastery`** Artisan's Mastery — +X skill levels in Crafting and Cooking while equipped.  
  `skill_level` `Crafting,Cooking` · flat · head, utility · utility · group `skill_level` · easy · P1 · w 100  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15
- **`farming_mastery`** Green Thumb — +X skill levels in Farming while equipped.  
  `skill_level` `Farming` · flat · tool · utility · group `skill_level` · easy · P1 · w 60  
  T1–T7: 2–3 / 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 · requires skill Farming  
  *Note:* Ours, not in the pool: added so the cultivator has a governed skill. Assumes the cultivator's own skill is Farming; verify.

## Item-local, any equipment

Stats that describe the item itself. They roll on any slot whose items have the stat (`requires.traits`).

- **`well_forged`** Well-Forged — This item's maximum durability is +X%.  
  `item_durability` · percent · melee, ranged, magic, shield, head, chest, legs, cape, tool · utility · group `item_wear` · easy · P1 · w 100  
  T1–T7: 10–15 / 15–20 / 20–30 / 30–40 / 40–50 / 50–65 / 65–80 · requires traits wears_out
- **`everlasting`** Everlasting — This item never loses durability.  
  `item_unbreakable` · flag · melee, ranged, magic, shield, head, chest, legs, cape, tool · utility · group `item_wear` · medium · P2 · w 25  
  gate T4 (rolls at T4–T7, no value) · requires traits wears_out
- **`lightened`** Lightened — This item weighs X% less.  
  `item_lighten` · percent · melee, ranged, magic, shield, head, chest, legs, cape, tool, utility · utility · group `item_lighten` · easy · P1 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`gossamer`** Gossamer — This item weighs nothing.  
  `item_zero_weight` · flag · melee, ranged, magic, shield, head, chest, legs, cape, tool, utility · utility · group `item_lighten` · easy · P2 · w 20  
  gate T4 (rolls at T4–T7, no value)
- **`supple_fit`** Supple Fit — This item no longer slows your movement.  
  `item_no_move_penalty` · flag · melee, shield, head, chest, legs, cape · utility · no group · easy · P2 · w 40  
  gate T2 (rolls at T2–T7, no value) · requires traits movement_penalty

## Shields

Block and parry stats stay on shields in v1 (`../DECISIONS.md` AFX-3). Parry affixes require a shield that can parry (trait `can_parry`: tower shields cannot).

- **`stalwart`** Stalwart — This shield's block armor is +X%. **[Phase 1 pick]**  
  `item_block` · percent · shield · defense · no group · easy · P1 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–16 / 16–20 / 20–25 / 25–30
- **`perfect_guard`** Perfect Guard — This shield's perfect-block bonus is +X%.  
  `parry_bonus` · percent · shield · defense · no group · easy · P1 · w 100  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35 · requires traits can_parry
- **`keen_guard`** Keen Guard — The perfect-block timing window is X ms longer.  
  `perfect_block_window` · flat (ms) · shield · defense · no group · medium · P2 · w 60  
  T1–T7: 10–15 / 15–20 / 20–30 / 30–40 / 40–50 / 50–55 / 55–60 · requires traits can_parry  
  *Note:* Arguable: the vanilla window is about a quarter second, so +60 ms is a large change.
- **`repelling_guard`** Repelling Guard — This shield's block knockback force is +X%.  
  `item_deflection` · percent · shield · defense · no group · easy · P1 · w 100  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`tireless_guard`** Tireless Guard — Blocking costs X% less stamina.  
  `block_stamina_cost` · percent · shield · defense · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`anchored_guard`** Anchored Guard — While blocking with this shield you are never knocked back or staggered by a blocked hit.  
  `block_steadfast` · flag · shield · defense · no group · medium · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`seidr_riposte`** Seidr Riposte — A perfect block restores X eitr.  
  `parry_restore` `eitr` · flat · shield · defense · no group · medium · P2 · w 60  
  T5–T7: 11–13 / 13–15 / 15–18 · requires traits can_parry

## Armor: health, stamina, eitr and recovery

- **`vigor`** Vigor — +X maximum health. **[Phase 1 pick]**  
  `max_health` · flat · head, chest · defense · group `max_health` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–8 / 8–11 / 11–14 / 14–18 / 18–22 / 22–26
- **`endurance`** Endurance — +X maximum stamina.  
  `max_stamina` · flat · chest, legs · defense · no group · easy · P1 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–15 / 15–19 / 19–23 / 23–28
- **`wellspring`** Wellspring — +X maximum eitr.  
  `max_eitr` · flat · head, cape · defense · group `max_eitr` · easy · P1 · w 100  
  T5–T7: 8–12 / 12–18 / 18–25  
  *Note:* Tiers 5-7: there is no eitr before Mistlands food.
- **`troll_blood`** Troll Blood — Health regenerates X% faster.  
  `health_recovery` · percent · chest, legs · defense · group `health_recovery` · easy · P1 · w 100  
  T1–T7: 3–5 / 5–8 / 8–11 / 11–14 / 14–17 / 17–20 / 20–25
- **`troll_blood_hc`** Cornered Blood — While health-critical, health regenerates X% faster.  
  `health_recovery` · percent · chest · defense · **health-critical** · group `health_recovery` · easy · P2 · w 60  
  T1–T7: 10–15 / 15–20 / 20–28 / 28–36 / 36–44 / 44–52 / 52–60
- **`mending`** Mending — Heal X every 10 seconds, with or without food.  
  `health_recovery_flat` · flat · head, chest · defense · no group · easy · P2 · w 100  
  T1–T7: 1 / 1–2 / 2 / 2–3 / 3 / 3–4 / 4–5  
  *Note:* The 10 s interval is a judgement call.
- **`stout_heart`** Stout Heart — +X maximum health, but health regenerates X% slower.  
  `health_for_regen` · flat · chest · defense · group `max_health` · easy · P2 · w 60  
  T1–T7: 6–10 / 10–16 / 16–22 / 22–28 / 28–36 / 36–44 / 44–52  
  *Note:* Trade ratio (health doubled vs the Vigor curve, regen loss equal to the health gained) is a judgement call.
- **`second_wind`** Second Wind — Stamina regenerates X% faster. **[Phase 1 pick]**  
  `stamina_recovery` · percent · head, legs · defense · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–8 / 8–11 / 11–14 / 14–17 / 17–20 / 20–25
- **`seidr_flow`** Seidr Flow — Eitr regenerates X% faster.  
  `eitr_recovery` · percent · head, cape · defense · group `eitr_recovery` · easy · P1 · w 100  
  T5–T7: 14–17 / 17–20 / 20–25
- **`restless_mind`** Restless Mind — Eitr regenerates X% faster, but maximum eitr is X/2 % lower.  
  `eitr_for_regen` · percent · head · defense · group `eitr_recovery` · easy · P2 · w 60  
  T5–T7: 28–34 / 34–40 / 40–50
- **`valhallas_edge`** Valhalla's Edge — Health-critical starts X percentage points higher (adds to the 30% default).  
  `hc_threshold` · percent · head · defense · no group · easy · P2 · w 50  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8

## Armor: protection

Resistances are spread so that covering every element takes several pieces. Spirit has no resistance affix (PLAN.md decision: spirit damage does not reach players).

- **`hardened`** Hardened — This piece's armor is +X%. **[Phase 1 pick]**  
  `item_armor` · percent · head, chest, legs, cape · defense · group `item_armor` · easy · P1 · w 100  
  T1–T7: 4–6 / 6–9 / 9–12 / 12–16 / 16–20 / 20–25 / 25–30
- **`hardened_hc`** Cornered Hide — While health-critical, this piece's armor is +X%.  
  `item_armor` · percent · chest, legs · defense · **health-critical** · group `item_armor` · easy · P2 · w 60  
  T1–T7: 10–15 / 15–22 / 22–30 / 30–38 / 38–46 / 46–54 / 54–62
- **`padded`** Padded — Reduces the blunt part of every hit you take by X%.  
  `damage_taken` `blunt` · percent · chest, legs · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`mailed`** Mailed — Reduces the slash part of every hit you take by X%.  
  `damage_taken` `slash` · percent · chest, legs · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`riveted`** Riveted — Reduces the pierce part of every hit you take by X%.  
  `damage_taken` `pierce` · percent · chest, legs · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`ironclad`** Ironclad — Reduces the blunt, slash and pierce parts of every hit you take by X%.  
  `damage_taken` `physical` · percent · chest · defense · no group · easy · P2 · w 60  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–8 / 8–10
- **`flameward`** Flameward — Reduces the fire part of every hit you take by X%.  
  `damage_taken` `fire` · percent · chest, cape · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`frostward`** Frostward — Reduces the frost part of every hit you take by X%.  
  `damage_taken` `frost` · percent · head, cape · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`stormward`** Stormward — Reduces the lightning part of every hit you take by X%.  
  `damage_taken` `lightning` · percent · head, cape · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`venomward`** Venomward — Reduces the poison part of every hit you take by X%.  
  `damage_taken` `poison` · percent · legs, cape · defense · no group · easy · P2 · w 100  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`elemental_ward`** Elemental Ward — Reduces the fire, frost, lightning and poison parts of every hit you take by X%.  
  `damage_taken` `elemental` · percent · cape · defense · no group · easy · P2 · w 60  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–8 / 8–10
- **`bulwark_fire`** Fire Bulwark — You are Resistant to fire damage, the game's own modifier. Does not stack with other sources of the same resistance.  
  `resist_modifier` `fire` · flag · cape · defense · group `elemental_bulwark` · easy · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`bulwark_frost`** Frost Bulwark — You are Resistant to frost damage, the game's own modifier. Does not stack with other sources of the same resistance.  
  `resist_modifier` `frost` · flag · cape · defense · group `elemental_bulwark` · easy · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`bulwark_lightning`** Lightning Bulwark — You are Resistant to lightning damage, the game's own modifier. Does not stack with other sources of the same resistance.  
  `resist_modifier` `lightning` · flag · cape · defense · group `elemental_bulwark` · easy · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`bulwark_poison`** Poison Bulwark — You are Resistant to poison damage, the game's own modifier. Does not stack with other sources of the same resistance.  
  `resist_modifier` `poison` · flag · cape · defense · group `elemental_bulwark` · easy · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`arrowward`** Arrowward — Damage from projectiles is X% lower, whatever its type.  
  `ranged_damage_taken` · percent · chest · defense · no group · easy · P2 · w 70  
  T1–T7: 2–4 / 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16
- **`mist_veil`** Mist Veil — X% chance to avoid all damage from a hit.  
  `avoid_hit` · percent · legs, cape · defense · group `avoid_hit` · easy · P2 · w 60  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8
- **`mist_veil_hc`** Cornered Veil — While health-critical, X% chance to avoid all damage from a hit.  
  `avoid_hit` · percent · cape · defense · **health-critical** · group `avoid_hit` · easy · P2 · w 50  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`bramblehide`** Bramblehide — Melee attackers take X% of the damage they deal to you.  
  `thorns` · percent · chest · defense · no group · medium · P2 · w 70  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`runic_ward`** Runic Ward — After 10 s without taking damage, a ward absorbs the next X damage.  
  `calm_ward` · flat · chest · defense · no group · medium · P2 · w 70  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–22 / 22–28 / 28–35 / 35–45
- **`resolute`** Resolute — You build up X% less stagger when hit.  
  `stagger_taken` · percent · chest, legs · defense · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`ironroot`** Ironroot — You are knocked back X% less by any hit.  
  `knockback_taken` · percent · legs · defense · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`quick_recovery`** Quick Recovery — When you are staggered you recover X% sooner.  
  `stagger_recovery` · percent · head · defense · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`purity`** Purity — Burning, poison and frost effects on you wear off X% faster.  
  `debuff_decay` · percent · head, chest · defense · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`coldblood`** Coldblood — The frost effect slows you X% less.  
  `frost_slow_taken` · percent · legs, cape · defense · no group · medium · P2 · w 70  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100

## Armor: movement

Legs are the movement slot; capes take the situational ones so the legs do not hoard every movement roll.

- **`fleetfoot`** Fleetfoot — You move X% faster. **[Phase 1 pick]**  
  `move_speed` · percent · legs · utility · group `move_speed` · easy · P1 · w 100  
  T1–T7: 1–2 / 2–3 / 3–4 / 4–5 / 5–6 / 6–7 / 7–8
- **`fleetfoot_hc`** Cornered Flight — While health-critical, you move X% faster.  
  `move_speed` · percent · legs · utility · **health-critical** · group `move_speed` · easy · P2 · w 60  
  T1–T7: 4–6 / 6–8 / 8–10 / 10–12 / 12–14 / 14–16 / 16–20
- **`stride`** Stride — You sprint X% faster (sprinting only).  
  `move_speed_sprint` · percent · legs · utility · group `move_speed` · easy · P2 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`ghostwalk`** Ghostwalk — You move X% faster while sneaking.  
  `move_speed_sneak` · percent · cape · utility · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`pack_mule`** Pack Mule — You move X% faster while encumbered.  
  `move_speed_encumbered` · percent · legs, cape · utility · no group · easy · P2 · w 70  
  T1–T7: 6–10 / 10–14 / 14–18 / 18–22 / 22–26 / 26–30 / 30–36
- **`momentum`** Momentum — For 5 s after a dodge roll you move X% faster.  
  `move_speed_after_dodge` · percent · legs · utility · no group · easy · P2 · w 70  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`pathfinder`** Pathfinder — You move X% faster on paved roads and paths.  
  `move_speed_paved` · percent · legs · utility · no group · medium · P2 · w 60  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`mountain_goat`** Mountain Goat — Steep slopes slow you X% less.  
  `slope_penalty` · percent · legs · utility · no group · medium · P2 · w 60  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100
- **`marshstrider`** Marshstrider — Tar and shallow water slow you X% less.  
  `terrain_slow` · percent · legs · utility · no group · medium · P2 · w 60  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100
- **`strong_swimmer`** Strong Swimmer — You swim X% faster and swimming costs X% less stamina.  
  `swimmer` · percent · cape · utility · no group · easy · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`spring_heeled`** Spring-Heeled — You jump X% higher.  
  `jump_height` · percent · legs · utility · no group · easy · P1 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`soft_landing`** Soft Landing — Fall damage you take is X% lower. **[Phase 1 pick]**  
  `fall_damage_taken` · percent · legs, cape · defense · group `fall` · easy · P1 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–35 / 35–40
- **`ravens_glide`** Raven's Glide — You fall slowly and take no fall damage.  
  `slow_fall` · flag · cape · utility · group `fall` · easy · P2 · w 30  
  gate T4 (rolls at T4–T7, no value)
- **`long_wind`** Long Wind — Sprinting costs X% less stamina.  
  `run_stamina_cost` · percent · legs · utility · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`light_leap`** Light Leap — Jumping costs X% less stamina.  
  `jump_stamina_cost` · percent · legs · utility · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18
- **`nimble`** Nimble — Dodge rolls cost X% less stamina.  
  `dodge_stamina_cost` · percent · legs, cape · defense · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–7 / 7–9 / 9–11 / 11–13 / 13–15 / 15–18

## Armor: stealth and environment

- **`soft_tread`** Soft Tread — The noise you make is X% quieter.  
  `noise_made` · percent · legs, cape · utility · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`shadowmeld`** Shadowmeld — While sneaking you are X% harder to see.  
  `stealth` · percent · head, cape · utility · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`ashen_skin`** Ashen Skin — Heat builds up on you X% slower (Ashlands heat).  
  `heat_resist` · percent · chest, cape · defense · no group · easy · P2 · w 100  
  T5–T7: 20–25 / 25–30 / 30–35  
  *Note:* Tiers 5-7: heat only matters in the Ashlands; the tier-7 top is the point of it.
- **`emberheart`** Emberheart — You never become Cold. Freezing still applies.  
  `cold_immunity` · flag · chest, cape · defense · no group · medium · P2 · w 40  
  gate T2 (rolls at T2–T7, no value)
- **`winterborn`** Winterborn — You never become Freezing, as if a frost resistance mead were always active.  
  `freeze_immunity` · flag · chest, cape · defense · no group · easy · P2 · w 25  
  gate T5 (rolls at T5–T7, no value)  
  *Note:* Regular pool, tiers 5-7 (Plains onward, after the Mountain is done). The pool also floated it as a Mythic candidate; it stays regular (`../DECISIONS.md` AFX-6).
- **`oilskin`** Oilskin — Rain never makes you Wet. Going into water still does.  
  `rain_shield` · flag · head, cape · utility · group `wet` · medium · P2 · w 40  
  gate T2 (rolls at T2–T7, no value)
- **`sealegs`** Sealegs — Being Wet does not slow your regeneration.  
  `ignore_wet` · flag · chest · utility · group `wet` · medium · P2 · w 40  
  gate T3 (rolls at T3–T7, no value)
- **`hearthlight`** Hearthlight — You give off a soft light that others can see.  
  `light_aura` · flag · head · utility · no group · medium · P2 · w 40  
  gate T1 (rolls at T1–T7, no value)

## Utility: armor and utility items

Carry, pickup, discovery, comfort and the economy affixes. Utility items (belts and the like) have no durability, so the item-local durability affixes skip them.

- **`broad_back`** Broad Back — +X carrying capacity. **[Phase 1 pick]**  
  `carry_capacity` · flat · chest, utility · utility · no group · easy · P1 · w 100  
  T1–T7: 10–15 / 15–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–75
- **`magpie`** Magpie — Items are picked up from X% farther away.  
  `pickup_radius` · percent · cape, utility · utility · no group · easy · P1 · w 100  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100
- **`huginns_eye`** Huginn's Eye — The map reveals X% farther around you.  
  `explore_radius` · percent · head, utility · utility · no group · easy · P1 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`mistbane`** Mistbane — Your mist-clearing light clears X% farther.  
  `demist_radius` · percent · head, utility · utility · no group · medium · P2 · w 70  
  T6–T7: 30–40 / 40–50  
  *Note:* Tiers 6-7: the mist exists only in the Mistlands.
- **`hearthbound`** Hearthbound — +X comfort while you rest.  
  `rest_comfort` · flat · chest, cape, utility · utility · no group · easy · P2 · w 60  
  T3–T7: 1 / 1 / 1–2 / 1–2 / 2
- **`gourmand`** Gourmand — Food you eat lasts X% longer.  
  `food_duration` · percent · chest, utility · utility · no group · easy · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`soulbound`** Soulbound — Skills lose X% less on death.  
  `skill_loss` · percent · head, utility · utility · no group · easy · P2 · w 70  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100
- **`mimirs_insight`** Mimir's Insight — All skills level up X% faster.  
  `skill_gain` `All` · percent · head, utility · utility · no group · easy · P1 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`brewers_haste`** Brewer's Haste — The cooldown after drinking a mead is X% shorter.  
  `mead_cooldown` · percent · head, utility · utility · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`forsaken_favour`** Forsaken Favour — Your forsaken power recharges X% faster.  
  `forsaken_cooldown` · percent · head, utility · utility · no group · easy · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`reflex_draught`** Reflex Draught — When you become health-critical you drink your best healing mead automatically.  
  `auto_mead` · flag · chest, utility · defense · **health-critical** · group `mead_critical` · medium · P2 · w 30  
  gate T2 (rolls at T2–T7, no value)
- **`swift_draught`** Swift Draught — While health-critical, healing meads heal all at once instead of over time.  
  `mead_burst` · flag · utility · defense · **health-critical** · group `mead_critical` · medium · P2 · w 30  
  gate T3 (rolls at T3–T7, no value)
- **`norns_favour`** Norns' Favour — Magic gear dropped by enemies you kill rolls a higher rarity X% more often.  
  `find_rarity` · percent · head, utility · utility · no group · medium · P2 · w 50  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`fateweaver`** Fateweaver — Enemies you kill are X% more likely to drop an extra stone.  
  `find_stones` · percent · head, utility · utility · no group · medium · P2 · w 50  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`trophy_taker`** Trophy Taker — Enemies you kill drop their trophy X% more often.  
  `find_trophy` · percent · utility · utility · no group · medium · P2 · w 70  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`hoardfinder`** Hoardfinder — Enemies you kill drop coins and treasure X% more often.  
  `find_coins` · percent · utility · utility · no group · medium · P2 · w 70  
  T1–T7: 5–8 / 8–12 / 12–16 / 16–20 / 20–25 / 25–30 / 30–35
- **`harvester`** Harvester — X% chance for plants you pick to yield double.  
  `yield_pickable` · percent · utility · utility · no group · medium · P2 · w 100  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50
- **`beast_whisperer`** Beast Whisperer — Creatures you are taming tame X% faster.  
  `taming_speed` · percent · cape, utility · utility · no group · medium · P2 · w 70  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100
- **`fair_winds`** Fair Winds — A ship you steer sails X% faster.  
  `sail_speed` · percent · cape, utility · utility · no group · medium · P2 · w 70  
  T1–T7: 5–10 / 10–15 / 15–20 / 20–25 / 25–30 / 30–40 / 40–50

## Tools

Build, repair and gathering. The tool skill affixes (Miner's, Angler's Mastery, Green Thumb) are listed with the skills above.

- **`builders_reach`** Builder's Reach — While this tool is in hand you can build and repair X% farther away. **[Phase 1 pick]**  
  `build_range` · percent · tool · utility · no group · easy · P1 · w 100  
  T1–T7: 10–20 / 20–30 / 30–40 / 40–50 / 50–60 / 60–80 / 80–100 · requires traits builds
- **`tireless_hands`** Tireless Hands — Building, repairing, tilling and planting cost X% less stamina.  
  `home_item_stamina_cost` · percent · tool · utility · no group · easy · P1 · w 100  
  T1–T7: 3–5 / 5–8 / 8–11 / 11–14 / 14–17 / 17–20 / 20–25 · requires traits builds  
  *Note:* Ours, not in the pool: added because tools otherwise have too few sane affixes to fill a Rare.
- **`deep_vein`** Deep Vein — Rock and ore you break drop X extra of their resource.  
  `yield_mining` · flat · tool · utility · no group · medium · P2 · w 70  
  T1–T7: 1 / 1 / 1–2 / 1–2 / 1–2 / 2 / 2–3 · requires skill Pickaxes

---

# 5. The Mythic-only pool

`affixes-mythic.md`: 13 affixes, Phase 3. A Mythic item draws one of them (`rarity.md` section 5).

---

# 6. Phase 1 set

PLAN.md Phase 1 asks for "~12 easy effects across slots". Twelve **picks** carry that intent, one per required slot
family and chosen so each is a single postfix or a single aggregate override:

| affix | slot | effect | why it was picked |
|---|---|---|---|
| `emberbrand` | melee, ranged | `brand_damage` | Melee. One postfix on `ItemData.GetDamage`, and the vanilla tooltip shows the boosted numbers for free. The other seven brands ride the same postfix. |
| `balanced_grip` | melee | `attack_stamina_cost` | Melee. One aggregate override (`ModifyAttackStaminaUsage`); felt on every swing, easy to verify. |
| `easy_draw` | ranged | `draw_stamina_cost` | Ranged. One postfix on `ItemData.GetDrawStaminaDrain`; bows are the ranged weapon every player has from the Meadows. |
| `seidr_thrift` | magic | `attack_eitr_cost` | Magic. One postfix on `Attack.GetAttackEitr(Character, ItemData)`; eitr cost is the stat a staff user notices first. |
| `stalwart` | shield | `item_block` | Shield. One postfix on `ItemData.GetBaseBlockPower`; the tooltip shows it. |
| `vigor` | head, chest | `max_health` | Head, chest. One postfix on `Player.GetTotalFoodValue`; max stamina and max eitr share it. |
| `hardened` | head, chest, legs, cape | `item_armor` | Every armor piece. One postfix on `ItemData.GetArmor`; tooltip shows it; the armor pieces all get a roll from day one. |
| `second_wind` | head, legs | `stamina_recovery` | Head, legs. One aggregate override (`ModifyStaminaRegen`); the whole regen family shares the shape. |
| `fleetfoot` | legs | `move_speed` | Legs. One aggregate override (`ModifySpeed`); the most visible stat in the game and the first cap to test. |
| `soft_landing` | legs, cape | `fall_damage_taken` | Cape, legs. One aggregate override (`ModifyFallDamage`); trivially tested by jumping off something. |
| `broad_back` | chest, utility | `carry_capacity` | Utility item, chest. One aggregate override (`ModifyMaxCarryWeight`); belts are the utility slot's identity. |
| `builders_reach` | tool | `build_range` | Tool. A field write on rebuild while the hammer is in hand; no patch at all. |

**Twelve are not enough to make items.** `rarity.md` fills a rarity's affix count from the eligible pool; with only
the twelve picks, every item class has one to three candidates, so nothing past Rare could be made and a Legendary
drop could never roll. Phase 1 therefore also enables a **fill-out**: affixes whose effect rides a patch point the
picks already need (the same postfix or the same aggregate override with another channel), so they cost channel
entries rather than new hooks. Effects in the fill-out, with their affixes:

- `brand_damage`: `rimebrand`, `stormbrand`, `venombrand`, `spiritbrand`, `bonebreaker`, `keen_edge`, `needlepoint`
- `item_durability`: `well_forged`
- `item_lighten`: `lightened`
- `damage_dealt`: `honed_might`, `primal_fury`
- `night_damage`: `nightstalker`
- `max_stamina`: `endurance`
- `max_eitr`: `wellspring`
- `health_recovery`: `troll_blood`
- `eitr_recovery`: `seidr_flow`
- `run_stamina_cost`: `long_wind`
- `jump_stamina_cost`: `light_leap`
- `dodge_stamina_cost`: `nimble`
- `block_stamina_cost`: `tireless_guard`
- `home_item_stamina_cost`: `tireless_hands`
- `parry_bonus`: `perfect_guard`
- `item_deflection`: `repelling_guard`
- `jump_height`: `spring_heeled`
- `skill_gain`: `mimirs_insight`
- `move_speed_sneak`: `ghostwalk`
- `pickup_radius`: `magpie`
- `explore_radius`: `huginns_eye`
- `reload_speed`: `quick_windlass`
- `skill_level`: `blade_mastery`, `axe_mastery`, `club_mastery`, `knife_mastery`, `spear_mastery`, `polearm_mastery`, `fist_mastery`, `bow_mastery`, `crossbow_mastery`, `elemental_mastery`, `blood_mastery`, `woodcutting_mastery`, `pick_mastery`, `fishing_mastery`, `shield_mastery`, `wanderer_mastery`, `artisan_mastery`, `farming_mastery`

That is **59 affixes on 34 effect ids, the Phase 1 set** (`../DECISIONS.md` RC-8), but only these **patch points**: the aggregate status
effect (its `ModifyAttack`, `ModifySpeed`, `ModifySkillLevel`, `ModifyRaiseSkill`, `ModifyJump`, `ModifyFallDamage`,
`ModifyMaxCarryWeight`, regen and stamina-usage overrides, and `ModifyTimedBlockBonus`), the `ItemData` postfixes
(`GetDamage`, `GetArmor`, `GetBaseBlockPower`, `GetDeflectionForce`, `GetMaxDurability`, `GetWeight`,
`GetDrawStaminaDrain`, `GetWeaponLoadingTime`), `Player.GetTotalFoodValue`, `Attack.GetAttackEitr`, and three field
writes on rebuild (build range, pickup radius, explore radius). The coverage table in section 2 shows every weapon,
shield and armor class reaching Mythic's six in Phase 1; tools stay short (`../DECISIONS.md` AFX-4).

No Phase 1 affix is health-critical: the condition evaluator arrives with Phase 2.

---

# 7. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Affixes: AFX-1 to AFX-15). AFX-8, party auras
without the Party mod, is **BLOCKING for Phase 3**. The schema additions this catalog needed are folded into
`configuration.md` section 6; the `$ecf_affix_<id>_line` key is `localization.md` section 1.

---

# Build checklist

- [ ] Effect registry holds all 34 Phase 1 effects with the fields of `effects-runtime.md` section 1
- [ ] The 0.1.0 built-in affix defaults hold exactly the 59 Phase 1 affixes of section 6, generated from the catalog
- [ ] `requires` (governing skill, hands, traits) evaluated from shared data at load
- [ ] Exclusion by id and by group; caps from the registry, overridable in `caps:`
- [ ] Every Phase 1 affix seen working in game on its slot (`ecraft affix` to force one)

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0: 169 regular affixes, 13 Mythic, 131 effects, Phase 1 set. | pending |
| 2026-09-23 | Reconciled: open questions moved to `../DECISIONS.md`; schema additions folded into `configuration.md`. | pending |

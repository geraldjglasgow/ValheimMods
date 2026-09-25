# EliteCrafting - specification: Stones

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the 27 stones: how a stone is applied, every refusal, and what each stone does, step by step, with
its edge cases. **Rarity counts, promote/demote and the affix roll** are `rarity.md`; **tier ceilings** are
`item-tier.md`; **sigil steering** in detail is `sigils.md`; **Honing and Tempering** in detail are `quality.md`;
**where stones drop** is `drops.md`; **the YAML fields** are `economy-yaml.md`.

Numbers are defaults and all of them are configurable in the `stones:` section of `EliteCrafting_economy*.yml`.
Display names are the user-approved catalog names (2026-09-23).

**Status: all 27 stones built, not tested in game** (Phase 1 and Phase 2, 2026-09-23); judgement calls in `../DECISIONS.md` IMP-16–22 and IMP-65–72. **Phase 1** ships the five ascension stones, Lesser Growth, Lesser
Turmoil and Lesser Perfection. Everything else is **Phase 2**. All 27 are specified fully here.

---

# 1. Applying a stone

The mechanism - gesture, pipeline, refusal display, confirm gate, cost payment - is owned by `applying-stones.md`.
This section restates it from the stones' side; where the two differ, `applying-stones.md` is the mechanism and this
file is the per-stone content.

## The gesture

The player picks up a stone stack in the inventory and clicks it onto the target item (InventoryGui patch). One use
per click. Costs above 1 are taken from **the held stack only** - stones in other stacks are not pooled (judgement
call: the player sees exactly which stack pays). A stone dropped onto **another stone** is not a use at all: the
game merges or swaps them as usual (`applying-stones.md` section 1).

## The check order

Every check runs **before** anything changes. The first failing check refuses the stone with its message, and **a
refusal never consumes anything** - not the stone, not a pending sigil.

1. Both the stone and the target are in the player's **own inventory** (not an open container, not the ground).
2. The target is a **magic base** (`item-data.md` section 2: max stack 1, resolves to a slot, not a stone) - sigils
   and quality stones included.
3. The target's data format is not newer than this build's (`item-data.md` section 8).
4. The stone is **enabled**.
5. The target's slot is in the stone's `slots` filter, if the stone has one (quality stones), and the stone's
   per-item precondition on the base holds (Honing needs damage, Tempering needs armor or block).
6. The target is **not sealed**.
7. The target is **not equipped**, only when the synced "modify equipped items" setting is off (default on).
8. The target's rarity is **known** (an owner has not removed it from the YAML - `item-data.md` section 6).
9. The target's rarity is in the stone's **`applies_to`**.
10. The held stack holds at least the stone's **cost** for that rarity (the rarity *before* the stone acts).
11. The stone's own **precondition** (full, at minimum, already bound, capped, sigil already pending, free slot for
    Reflection, sigil would be stranded by the Serpent).
12. The **dry run**: the stone's action is carried out on a copy of the item's parsed record. If the result is
    invalid - the pool could not supply an affix, nothing was changeable, a pending sigil found nothing to steer -
    the stone is refused with that reason. The dry-run record is exactly what commits; nothing is rolled twice.
13. The **confirm gate**, for stones with `confirm: true` (section 6). Last on purpose: the player is only ever asked
    to confirm a stone that would work.

## Commit

On success, in this order, all on the owning client in one frame:

1. The dry-run result is written to the item's custom data (one write; the parse cache is invalidated).
2. A pending sigil that steered this stone is cleared (`sigils.md`).
3. The cost is removed from the held stack.
4. If the item is equipped, the equipment-change rebuild runs (aggregated status effect and item-local caches).
5. A feedback message (section 3) and the transient effect for this stone family are shown; the effect is sent as an
   RPC only to peers that can see the player (PLAN.md).

**Transactional by design.** Because every stone acts on a copy and commits only on success, there is no state in
which a stone was consumed and the item half-changed, and every "cannot" is discovered before the player pays.

**What "success" means**, for every stone: the dry run produced a valid new item state. Some stones succeed with a
random result that happens to equal the old one (Perfection can reroll a value to the same number); that is still a
success and still costs the stone. Refusals are only for outcomes known *before* rolling.

---

# 2. Refusals

Every refusal: stone kept, sigil kept, item unchanged, message shown in the center of the screen. English text is the
default for the localization key; `$1`, `$2`, `$3` are the game's own placeholders, filled by the code
(`applying-stones.md` section 2).

| Message id | English text | Raised by |
| --- | --- | --- |
| `$ecf_msg_not_own_inventory` | Stones work only on items in your own inventory. | all |
| `$ecf_msg_not_magic_base` | This item cannot hold affixes. | all (includes stackable items) |
| `$ecf_msg_newer_format` | *(owned by `item-data.md`)* | all |
| `$ecf_msg_unknown_rarity` | *(owned by `item-data.md`)* | all |
| `$ecf_msg_stone_disabled` | The $1 is disabled on this server. | all |
| `$ecf_msg_wrong_item_type` | The $1 does not work on this kind of item. | Honing, Tempering (and any stone with `slots`) |
| `$ecf_msg_sealed` | This item is sealed. No stone can change it. | all |
| `$ecf_msg_equipped` | Unequip this item first. | all, only when modifying equipped items is off |
| `$ecf_msg_wrong_rarity` | The $1 does not work on $2 items. | all |
| `$ecf_msg_not_enough_stones` | You need $1 $2 for this. | all with cost above 1 |
| `$ecf_msg_affixes_full` | This item cannot hold another affix. | Growth |
| `$ecf_msg_affixes_minimum` | This item cannot lose another affix. | Severing |
| `$ecf_msg_already_bound` | This item already has a bound affix. | Binding |
| `$ecf_msg_nothing_to_bind` | This item has no affix that can be bound. | Binding |
| `$ecf_msg_quality_capped` | This item cannot be improved further by the $1. | Honing, Tempering |
| `$ecf_msg_sigil_pending` | A sigil already waits on this item. | all sigils |
| `$ecf_msg_sigil_would_strand` | The Serpent Stone would seal the pending sigil away. Use a stone the sigil steers first. | Serpent |
| `$ecf_msg_inventory_full` | You need a free inventory slot for the copy. | Reflection |
| `$ecf_msg_no_eligible_affix` | No affix can roll on this item. | every stone that adds affixes (dry run) |
| `$ecf_msg_nothing_to_change` | Nothing on this item can be changed by the $1. | Turmoil, Upheaval, Perfection, Severing (dry run) |
| `$ecf_msg_sigil_no_match` | The pending $1 finds nothing to steer toward on this item. | stones a sigil steers (dry run) |
| `$ecf_msg_confirm_required` | Hold Shift to use the $1. | Serpent, Unmaking (hold-Shift mode) |

The confirm dialog (dialog mode) uses `$ecf_ui_confirm_title` ("Use the $1?") and `$ecf_ui_confirm_body` ("$2
cannot be undone."); they are labels, not refusals.

---

# 3. Feedback messages

Shown on success. Owned here so every stone has one; styling is the display file's.

| Message id | English text |
| --- | --- |
| `$ecf_msg_promoted` | $1 rises to $2. |
| `$ecf_msg_affix_added` | $1 gains $2. |
| `$ecf_msg_affix_swapped` | $1 loses $2 and gains $3. |
| `$ecf_msg_affixes_rerolled` | $1 is reshaped. |
| `$ecf_msg_values_rerolled` | $1's values shift. |
| `$ecf_msg_affix_removed` | $1 loses $2. |
| `$ecf_msg_stripped` | $1 is unmade. |
| `$ecf_msg_corrupt_seal` | The Serpent seals $1. Nothing else changes. |
| `$ecf_msg_corrupt_add` | The Serpent seals $1 and grants $2. |
| `$ecf_msg_corrupt_chaos` | The Serpent seals $1 and twists every affix. |
| `$ecf_msg_corrupt_promote` | The Serpent seals $1 and raises it to $2. |
| `$ecf_msg_corrupt_demote` | The Serpent seals $1 and drags it down to $2. |
| `$ecf_msg_bound` | $2 is bound to $1. |
| `$ecf_msg_gambled` | $1 becomes $2. |
| `$ecf_msg_reflected` | A sealed copy of $1 appears. |
| `$ecf_msg_honed` | $1 is honed to +$2%. |
| `$ecf_msg_tempered` | $1 is tempered to +$2%. |
| `$ecf_msg_sigil_set` | The $2 waits on $1. |
| `$ecf_msg_sigil_spent` | The $2 steers the stone. |

---

# 4. Rules every stone shares

These hold for every stone unless its own section says otherwise; the per-stone tables below repeat only what
differs or needs stating.

- **Sealed** items refuse every stone, sigils and quality stones included (`$ecf_msg_sealed`). Sealed is permanent:
  not even Unmaking lifts it. (Judgement call for the quality stones, `../DECISIONS.md` STN-1.)
- **Equipped** items may be modified by default (synced setting, default on). When on, the equipment rebuild runs on
  commit. When off, `$ecf_msg_equipped`.
- **Stacks**: no magic base is stackable, so a stone can never meet a stack of magic items. A stone dropped on any
  stackable item (arrows, food, materials) is `$ecf_msg_not_magic_base`; dropped on another stone, the game merges
  or swaps them as usual (`applying-stones.md`).
- **Dormant (orphaned) affixes** - affixes whose id is no longer defined, or is disabled (user decision: kept, shown
  greyed, inert):
  - they **count** toward the rarity's minimum and maximum (they occupy space until removed);
  - they **may be removed** by the removal verbs (Severing, Turmoil, Upheaval, Unmaking, Serpent demote/chaos) -
    that is how a player clears them;
  - they are **never** rerolled by Perfection (their range is unknown), never bound, never preserved;
  - Sigil of Culling takes them first (`sigils.md`).
- **Bound affix** (Binding): never removed, rerolled or revalued by Turmoil, Upheaval, Perfection or Severing. Only
  Unmaking and the Serpent's chaotic reroll and demote remove it.
- **Pending sigil**: consumed only by a stone it steers, and only on that stone's success. A stone the sigil does
  not steer applies normally and **leaves the sigil pending** (`../DECISIONS.md` SIG-1, `sigils.md` section 4). The
  Serpent is refused while a sigil is pending (SIG-2).
- **Quality** (Honing/Tempering level) and the item's **vanilla upgrade level**, durability, crafter name and
  variant are untouched by every affix stone. Reflection copies all of them.
- **Tier ceiling and window** apply to every roll except the Serpent's chaotic reroll (`item-tier.md` section 6).
- **Mythic**: always six affixes with one from the Mythic-only pool (`rarity.md` section 5). Stones that remove
  and add keep that composition: a removed Mythic-only affix is replaced from the Mythic-only pool.
- **Cost** defaults to 1 for every stone at every rarity.
- **Tier floor** defaults to none for every stone. The knob is there for owner-made stones and Phase 2 essences.

---

# 5. The catalog

| id | Display name | Verb | Grade | applies_to | Cost | tier_floor | Confirm | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `awakening` | Stone of Awakening | promote | - | common | 1 | - | no | 1 |
| `ascension` | Stone of Ascension | promote | - | uncommon | 1 | - | no | 1 |
| `exaltation` | Stone of Exaltation | promote | - | rare | 1 | - | no | 1 |
| `transcendence` | Stone of Transcendence | promote | - | epic | 1 | - | no | 1 |
| `apotheosis` | Stone of Apotheosis | promote | - | legendary | 1 | - | no | 1 |
| `growth_lesser` | Lesser Stone of Growth | add | lesser | uncommon, rare | 1 | - | no | 1 |
| `growth_greater` | Greater Stone of Growth | add | greater | epic, legendary, mythic | 1 | - | no | 2 |
| `turmoil_lesser` | Lesser Stone of Turmoil | swap | lesser | uncommon, rare | 1 | - | no | 1 |
| `turmoil_greater` | Greater Stone of Turmoil | swap | greater | epic, legendary, mythic | 1 | - | no | 2 |
| `upheaval_lesser` | Lesser Stone of Upheaval | reroll_affixes | lesser | uncommon, rare | 1 | - | no | 2 |
| `upheaval_greater` | Greater Stone of Upheaval | reroll_affixes | greater | epic, legendary, mythic | 1 | - | no | 2 |
| `perfection_lesser` | Lesser Stone of Perfection | reroll_values | lesser | uncommon, rare | 1 | - | no | 1 |
| `perfection_greater` | Greater Stone of Perfection | reroll_values | greater | epic, legendary, mythic | 1 | - | no | 2 |
| `severing_lesser` | Lesser Stone of Severing | remove | lesser | uncommon, rare | 1 | - | no | 2 |
| `severing_greater` | Greater Stone of Severing | remove | greater | epic, legendary, mythic | 1 | - | no | 2 |
| `unmaking` | Stone of Unmaking | strip | - | uncommon ... mythic | 1 | - | **yes** | 2 |
| `serpent` | Serpent Stone | corrupt | - | uncommon ... mythic | 1 | - | **yes** | 2 |
| `binding` | Stone of Binding | lock | - | uncommon ... mythic | 1 | - | no | 2 |
| `chance` | Stone of Chance | gamble | - | common | 1 | - | no | 2 |
| `reflection` | Stone of Reflection | duplicate | - | uncommon ... mythic | 1 | - | no | 2 |
| `honing` | Honing Stone | quality | - | all six | 1 | - | no | 2 |
| `tempering` | Tempering Stone | quality | - | all six | 1 | - | no | 2 |
| `sigil_preservation` | Sigil of Preservation | sigil | - | all six | 1 | - | no | 2 |
| `sigil_war` | Sigil of War | sigil | - | all six | 1 | - | no | 2 |
| `sigil_warding` | Sigil of Warding | sigil | - | all six | 1 | - | no | 2 |
| `sigil_fortune` | Sigil of Fortune | sigil | - | all six | 1 | - | no | 2 |
| `sigil_culling` | Sigil of Culling | sigil | - | all six | 1 | - | no | 2 |

Display-name localization: `$ecf_stone_<id>`, description `$ecf_stone_<id>_desc`. Prefabs: `ECF_` + PascalCase id
(`ECF_Awakening`, `ECF_GrowthLesser`, `ECF_SigilWar`).

**Mythic in the Greater `applies_to` lists.** A Mythic is always full and always at its minimum, so Greater Growth
and Greater Severing always refuse on one - but with "cannot hold another affix" rather than "does not work on
Mythic items", which tells the player *why*. Turmoil, Upheaval and Perfection work on Mythics normally.

---

# 6. The confirm gate

Serpent and Unmaking (`confirm: true`) cannot be undone. Reflection has no downside and is not gated.

- **Hold-Shift (default):** clicking without Shift held refuses with `$ecf_msg_confirm_required`; clicking with Shift
  applies.
- **Dialog:** clicking opens a yes/no dialog naming the stone and item; yes applies (all checks re-run at that
  moment - the item may have changed), no or closing does nothing.
- **Off:** applies immediately.

The mode is a **client preference** in the `.cfg`, unsynced: it protects the player from their own mouse, not the
server from the player. Which stones are gated is **synced**, in the stone entry's `confirm` field.

---

# 7. Ascension stones (verb `promote`) - Phase 1

Awakening, Ascension, Exaltation, Transcendence, Apotheosis. Each moves an item exactly **one rarity up** from the
rarity in its `applies_to`.

## Behaviour

1. Checks (section 1).
2. Promote per `rarity.md` section 3: rarity + 1; keep every affix; add `max(new.min - count, 1)` affixes, capped at
   the new maximum; into Mythic, the Mythic-only affix first.
3. A pending War/Warding/Fortune sigil forces **one** of the added affixes into its category.
4. Success: the rarity changed and at least one affix was added (unless the item was already at or above the new
   maximum - only possible with a custom ladder - in which case the rarity change alone is the success).

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Wrong rarity (Ascension on a Common, Awakening on an Uncommon, ...) | refused | `wrong_rarity` |
| Item already at the new rarity's maximum (custom ladders only) | rarity changes, nothing added | `promoted` |
| Pool cannot supply the affixes promotion needs | refused | `no_eligible_affix` |
| Sealed | refused | `sealed` |
| Bound affix present | kept, counts toward the new count | - |
| Dormant affixes present | kept, count toward the new count | - |
| Equipped | allowed (setting on); rebuild on commit | - |
| Pending War/Warding/Fortune | one added affix from the category; sigil consumed | `sigil_spent` |
| Pending War/Warding/Fortune, no eligible affix in that category | refused | `sigil_no_match` |
| Pending Preservation or Culling | not steered; sigil stays pending | - |
| Apotheosis, Mythic-only pool empty for this slot (always, before Phase 3) | the special affix comes from the regular pool | - |
| Apotheosis on a Legendary with 4 affixes | +1 Mythic-only, +1 regular = 6 | - |
| Quality / upgrade level | untouched | - |

---

# 8. Stone of Growth (verb `add`) - Lesser Phase 1, Greater Phase 2

Adds one affix, up to the rarity's maximum.

## Behaviour

1. Checks. Refused when the item already holds its rarity's maximum (dormant affixes counted).
2. Roll one affix (`rarity.md` section 4) under the item's ceiling and window.
3. Success: exactly one affix added.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Item full (count >= rarity maximum) | refused | `affixes_full` |
| Mythic (always 6/6) | refused | `affixes_full` |
| Item full only because of dormant affixes | refused - remove them with Turmoil or Severing first | `affixes_full` |
| Every candidate excluded (same id / exclusion group / slot) | refused | `no_eligible_affix` |
| Sealed | refused | `sealed` |
| Bound affix present | kept | - |
| Equipped | allowed; rebuild | - |
| Pending War/Warding/Fortune | the added affix is from the category; consumed | `sigil_spent` |
| ... with no candidate in the category | refused | `sigil_no_match` |
| Pending Preservation or Culling | not steered; stays pending | - |
| Quality | untouched | - |

---

# 9. Stone of Turmoil (verb `swap`) - Lesser Phase 1, Greater Phase 2

Removes one random affix and adds one new one. The count never changes.

## Behaviour

1. Checks.
2. **Removable set**: every affix except the bound one and, with a pending Sigil of Preservation, the preserved one.
   Dormant affixes are removable. Empty set -> refused (`nothing_to_change`).
3. Remove one from the removable set: at random, or with a pending Sigil of Culling, the one Culling selects.
4. Roll one replacement from the **same pool class** as the removed affix (a Mythic-only affix is replaced from the
   Mythic-only pool). The removed id is excluded from the draw (so the swap always changes the affix, not just its
   value); its exclusion group is freed.
5. Success: one affix out, a different one in.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Only the bound affix on the item (e.g. a one-affix Uncommon, bound) | refused | `nothing_to_change` |
| Only bound + preserved affixes | refused | `nothing_to_change` |
| No replacement candidate other than the removed id | refused | `no_eligible_affix` |
| Dormant affix picked for removal | removed, replaced by a live affix - the intended way to clear dead weight | `affix_swapped` |
| Mythic | works; a removed Mythic-only affix is replaced from the Mythic-only pool (regular pool if that pool is empty) | - |
| Item over its maximum (Serpent 7th affix) | unreachable - the item is sealed | `sealed` |
| At rarity minimum | works (count unchanged) | - |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending Preservation | highest-tier affix cannot be the one removed; consumed | `sigil_spent` |
| Pending Culling | lowest-tier (dormant first) removed instead of random; consumed | `sigil_spent` |
| Pending War/Warding/Fortune | replacement from the category; consumed | `sigil_spent` |
| ... no candidate in the category | refused | `sigil_no_match` |
| Quality | untouched | - |

---

# 10. Stone of Upheaval (verb `reroll_affixes`) - Phase 2

Rerolls every affix, keeping the rarity.

## Behaviour

1. Checks.
2. **Kept set**: the bound affix, and with a pending Sigil of Preservation the preserved one. Everything else is
   removed, dormant affixes included. If nothing is removable -> refused (`nothing_to_change`).
3. Draw the new count uniformly in the rarity's `[min, max]` (`rarity.md` section 4). Kept affixes count toward it.
4. Fill to the count under the ceiling and window. On a Mythic, the Mythic-only slot is filled from the Mythic-only
   pool unless a kept affix already fills it.
5. Success: every non-kept affix replaced.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Every affix kept (bound and/or preserved) | refused | `nothing_to_change` |
| Pool cannot reach the rarity's minimum | refused | `no_eligible_affix` |
| Dormant affixes | removed and replaced | - |
| Kept affixes exceed the drawn count | nothing added beyond them; still a success if anything was removed | `affixes_rerolled` |
| Mythic | six again, one Mythic-only | - |
| Common | not in `applies_to` | `wrong_rarity` |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending Preservation | highest-tier affix kept; consumed | `sigil_spent` |
| Pending War/Warding/Fortune | one of the new affixes from the category; consumed | `sigil_spent` |
| ... no candidate in the category | refused | `sigil_no_match` |
| Pending Culling | not steered (Upheaval removes everything, it does not pick); stays pending | - |
| Quality | untouched | - |

---

# 11. Stone of Perfection (verb `reroll_values`) - Lesser Phase 1, Greater Phase 2

Rerolls the numbers, never the affixes.

## Behaviour

1. Checks.
2. **Rerollable set**: every live affix with a numeric value (`value: percent` or `flat`), except the bound one and,
   with a pending Sigil of Preservation, the preserved one. Flag affixes have no number; dormant affixes have no
   known range. Empty set -> refused (`nothing_to_change`).
3. Each rerollable affix gets a new value uniformly within **its stored tier's** current `[min, max]`. The tier never
   changes.
4. Success: every rerollable affix rerolled (a value may land on its old number).

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Only flag, dormant, bound or preserved affixes | refused | `nothing_to_change` |
| Affix tier above the item's current ceiling (YAML lowered it) | rerolled within its stored tier; stays above the ceiling | - |
| Stored tier no longer defined for that affix (YAML removed the tier) | treated like dormant for this stone: skipped | - |
| Bound affix | value never changes | - |
| Mythic | all rerollable affixes, Mythic-only included | - |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending Preservation | the preserved affix's value is not rerolled; consumed | `sigil_spent` |
| Pending War/Warding/Fortune/Culling | not steered; stays pending | - |
| Quality | untouched (Perfection is not a quality stone) | - |

---

# 12. Stone of Severing (verb `remove`) - Phase 2

Removes one random affix, never below the rarity's minimum.

## Behaviour

1. Checks. Refused when the item holds its rarity's minimum or fewer (dormant affixes counted).
2. **Removable set**: every affix except the bound one. Dormant affixes are removable. Empty -> refused.
3. Remove one: at random, or with a pending Sigil of Culling, the one Culling selects.
4. Success: exactly one affix removed.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| At rarity minimum (e.g. Uncommon with 1) | refused - Unmaking is the way to Common | `affixes_minimum` |
| Mythic (always at 6) | refused | `affixes_minimum` |
| The only affix above the minimum is the bound one (possible only on a custom ladder with minimum 0) | refused | `nothing_to_change` |
| Dormant affixes | eligible for removal | - |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending Culling | lowest-tier (dormant first) removed; consumed | `sigil_spent` |
| Pending Preservation | not steered (Severing is not a reroll); stays pending | - |
| Pending War/Warding/Fortune | not steered; stays pending | - |
| Quality | untouched | - |

---

# 13. Stone of Unmaking (verb `strip`) - Phase 2, confirm gate

Strips an item back to Common.

## Behaviour

1. Checks, including the confirm gate.
2. Remove every affix, bound and dormant included; clear the binding; rarity becomes the base rarity.
3. **Kept**: quality (Honing/Tempering), vanilla upgrade level, durability, crafter, variant, and a pending sigil.
4. Success: the item is Common with no affixes. It may be Awakened or Chanced again, and bound again.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Common | not in `applies_to` | `wrong_rarity` |
| Sealed | refused (sealed is permanent) | `sealed` |
| Bound affix | removed; binding cleared | - |
| Mythic | stripped to Common like any other | - |
| Equipped | allowed; rebuild | - |
| Pending sigil | kept, stays pending (it steers the next stone, not this one) | - |
| Quality | **survives** (PLAN.md) | - |
| No Shift held (hold-Shift mode) | refused | `confirm_required` |

---

# 14. Serpent Stone (verb `corrupt`) - Phase 2, confirm gate

Corrupts an item: one random outcome from a weighted table, then the item is **sealed** - no stone of any kind
works on it again.

## Outcome table (defaults from PLAN.md)

| Outcome id | Weight | What happens | If it cannot be carried out |
| --- | --- | --- | --- |
| `seal_only` | 25 | Nothing but the seal | - |
| `add_affix` | 25 | Add one affix (normal roll, ceiling and window); may exceed the rarity's maximum by `overflow` (default 1) | falls back to `seal_only` |
| `chaotic_reroll` | 20 | Remove **every** affix (bound and dormant included; binding cleared); draw the count in the rarity's range; roll each with **any tier the affix defines, uniformly**, ignoring ceiling, window and floor (`item-tier.md` section 6) | if the pool cannot reach the rarity's minimum: fill what it can; if it can fill none, `seal_only` |
| `promote` | 15 | One rarity up (`rarity.md` section 3). On the top rarity (Mythic): add one regular affix beyond the cap (a 7th) | falls back to `seal_only` |
| `demote` | 15 | One rarity down and lose an affix (`rarity.md` section 3; bound affix eligible) | always possible within `applies_to` |

Weights are relative; any outcome can be given weight 0.

## Behaviour

1. Checks, including the confirm gate. Refused while a sigil is pending (`sigil_would_strand` - sealing would leave
   it on the item forever).
2. Draw the outcome by weight. Carry it out on the copy; fall back as the table says.
3. Set sealed. Commit. The corruption effect plays for every peer that can see the player.
4. Success: always, once the checks pass - `seal_only` is a success.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Common | not in `applies_to` (nothing to corrupt) | `wrong_rarity` |
| Already sealed | refused | `sealed` |
| Item full + `add_affix` | may exceed by one (e.g. a Legendary with 6) | `corrupt_add` |
| Mythic + `add_affix` or `promote` | both give a 7th affix (regular pool) | `corrupt_add` / `corrupt_promote` |
| Legendary + `promote` | becomes a sealed Mythic (one Mythic-only affix added, filled to 6) | `corrupt_promote` |
| Uncommon + `demote` | becomes a sealed Common with no affixes | `corrupt_demote` |
| Mythic + `demote` | Mythic-only affix removed, becomes a Legendary of 5 | `corrupt_demote` |
| Bound affix + `chaotic_reroll` | rerolled away; binding cleared | - |
| Bound affix + `demote` | may be the one removed | - |
| Bound affix + `seal_only`/`add_affix`/`promote` | stays | - |
| Dormant affixes | removed by `chaotic_reroll`; may be removed by `demote`; untouched otherwise | - |
| Pending sigil | refused | `sigil_would_strand` |
| Equipped | allowed; rebuild | - |
| Quality | untouched by every outcome | - |
| No Shift held | refused | `confirm_required` |

---

# 15. Stone of Binding (verb `lock`) - Phase 2

Binds one random affix permanently.

## Behaviour

1. Checks. Refused if the item already has a bound affix (`already_bound`).
2. **Bindable set**: every live (non-dormant) affix. Empty -> refused (`nothing_to_bind`).
3. Pick one at random; mark it bound.
4. Success: one affix bound.

**Once per item** means **one bound affix at a time**. The only ways a binding ends are Unmaking (after which the
Common item may be crafted up and bound again) and the Serpent's chaotic reroll or demote (after which the item is
sealed anyway). PLAN.md's "once per item" could also be read as once in the item's whole life, surviving
Unmaking; the one-at-a-time reading is the adopted default (`../DECISIONS.md` ITD-3).

What the binding protects: `stones.md` section 4 - the affix and its value survive Turmoil, Upheaval, Perfection and
Severing.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Already has a bound affix | refused | `already_bound` |
| Only dormant affixes | refused | `nothing_to_bind` |
| Common | not in `applies_to` | `wrong_rarity` |
| Mythic | any live affix, Mythic-only included | - |
| Sealed | refused | `sealed` |
| Equipped | allowed (nothing changes for effects; no rebuild needed but harmless) | - |
| Pending sigil | not steered; stays pending | - |
| Quality | untouched | - |

---

# 16. Stone of Chance (verb `gamble`) - Phase 2

Consumed on a Common: turns it into a random rarity. The item is never destroyed.

## Weights (defaults)

| Rarity | Weight | Chance |
| --- | --- | --- |
| common | 0 | 0% |
| uncommon | 50 | 50% |
| rare | 30 | 30% |
| epic | 15 | 15% |
| legendary | 5 | 5% |
| mythic | 0 | 0% (craft-only) |

Note: the Mythic weight here is independent of the rarity's `drop_weight` - `drop_weight` governs world drops,
Chance is crafting. Mythic stays unreachable by Chance because its weight is 0. A `common` weight above 0 adds a
"fizzle" outcome (the stone is spent, nothing happens); default 0, and it exists only so an owner can make Chance
riskier.

## Behaviour

1. Checks.
2. Draw the rarity by weight.
3. Roll a fresh item of that rarity: count uniform in the range, ceiling and window as usual (a new Mythic would get
   its Mythic-only affix).
4. A pending War/Warding/Fortune forces one of the rolled affixes into its category.
5. Success: the item has the drawn rarity (or, on a fizzle, the stone is spent).

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Not Common | refused | `wrong_rarity` |
| Pool cannot fill the drawn rarity's minimum | the rarity is lowered to the highest one the pool can fill; if none, refused | `no_eligible_affix` |
| All weights 0 | YAML validation disables the stone with a warning; at runtime | `stone_disabled` |
| Sealed Common (Serpent-demoted) | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending War/Warding/Fortune | one affix from the category; consumed | `sigil_spent` |
| ... no candidate in the category | refused | `sigil_no_match` |
| Pending Preservation or Culling | not steered; stays pending | - |
| Quality | untouched (a honed Common keeps its honing) | - |

---

# 17. Stone of Reflection (verb `duplicate`) - Phase 2

Duplicates an item; the copy is sealed. Astronomically rare by default (`drops.md`); enabled.

## Behaviour

1. Checks. Needs a free inventory slot (`inventory_full`).
2. Make the copy: the game's own item clone (custom data copied), then **clear the copy's equipped flag** (the clone
   copies it - verified in the decompile) and let the inventory place it in a free slot.
3. The copy carries rarity, every affix (tiers and values, bound and dormant ones included), quality, vanilla upgrade
   level, durability, crafter and variant. It does **not** carry a pending sigil. It is sealed.
4. The original is unchanged and stays unsealed (it can be reflected again with another stone).
5. Success: a sealed copy is in the inventory.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| No free slot | refused | `inventory_full` |
| Common | not in `applies_to` (duplicating vanilla items is not this stone's purpose) | `wrong_rarity` |
| Sealed original (a Serpent item or a copy) | refused - copies cannot be copied | `sealed` |
| Equipped original | allowed; the copy is not equipped | - |
| Pending sigil on the original | stays on the original; the copy has none | - |
| Mythic, or an item over its cap | copied as-is | - |
| Tiers above the ceiling | copied as-is (not a roll) | - |
| Quality | copied | - |

---

# 18. Honing Stone and Tempering Stone (verb `quality`) - Phase 2

Summary; the full rules are `quality.md`.

- **Honing**: weapons (`melee_weapon`, `ranged_weapon`, `magic_weapon`), +1% damage per use, cap +10%.
- **Tempering**: armor (`head`, `chest`, `legs`, `cape`) +1% armor, `shield` +1% block, per use, cap +10%.
- Any rarity, Common included. Independent of affixes. Survives Unmaking.

| Situation | Result | Message |
| --- | --- | --- |
| At cap | refused | `quality_capped` |
| Wrong slot (Honing on armor, Tempering on a sword) | refused | `wrong_item_type` |
| Weapon with no damage (a summoning staff) / armor piece with 0 armor | refused | `wrong_item_type` |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Pending sigil | not steered; stays pending | - |
| Affixes, bound, dormant | untouched | - |

---

# 19. Sigils (verb `sigil`) - Phase 2

Summary; the full rules are `sigils.md`.

- Applied to any magic base of any rarity. Sets the item's pending sigil. One pending sigil per item.
- Consumed by the next stone it steers, on that stone's success.

| Situation | Result | Message |
| --- | --- | --- |
| A sigil already pending (same or other) | refused | `sigil_pending` |
| Sealed | refused | `sealed` |
| Equipped | allowed (no rebuild needed) | - |
| Stackable / not a magic base | refused | `not_magic_base` |

---

# 20. The stone items themselves

- Stackable (default 50), light (default 0.2), tradeable, teleportable, drop as world items. `stack` and
  `item_weight` in each stone entry (`economy-yaml.md` section 4).
- **Prefabs come from code, not from the YAML** (`configuration.md` section 5, `prefabs.md`): all 27 are registered
  on every peer, identically, whatever the YAML says. A stone the YAML disables must still exist as a prefab, or the
  game would delete stacks of it from chests and inventories on load.
- `enabled: false` (or no live definition at all) means: never drops, cannot be applied (`stone_disabled`), still
  exists as an item that can be stored and traded (`../DECISIONS.md` RC-3).
- Visuals (base mesh, tint, scale per grade) are the core spec's.

---

# 21. Owner-defined stones

An owner can add a stone in YAML with a new id and one of the fixed verbs (`promote`, `add`, `swap`,
`reroll_affixes`, `reroll_values`, `remove`, `strip`, `corrupt`, `lock`, `duplicate`, `gamble`, `quality`,
`sigil`), its own `applies_to`, costs, `tier_floor`, verb keys and drop weights. Examples: a "Stone of Deep Growth"
(`add`, `tier_floor: 6`) or a second corruption stone with gentler outcomes. Prefabs are never created from YAML:
an owner-defined stone binds to one of the **reserved custom prefabs** `ECF_Custom01`-`ECF_Custom16` the mod
registers in code (`prefabs.md` section 5, `../DECISIONS.md` RC-4) through its `prefab` field. Its `name` and
`description` may be literal text; with neither, localization falls back to the id. An example entry is in
`economy-yaml.md` section 4.

---

# 22. Multiplayer

- Stones apply **only in the player's own inventory**, on the client that owns it. No container is ever edited
  remotely, so there are no ownership races.
- The rules (applies_to, costs, weights, outcome tables, confirm list, enabled) are server-synced and lockable. The
  dice roll on the client under the server's odds.
- The changed item travels in the game's own item serialization: inventory saves, containers, the ground,
  tombstones. No netcode for the result.
- Refusals are decided identically for host-less clients on a dedicated server, because every check reads either the
  item or the synced rules.
- The only RPC is the transient feedback effect, scoped to peers who can see the player.

---

# Build checklist

- [ ] Click-to-apply in the inventory; cost from the held stack
- [ ] Check order 1-11; every refusal keeps the stone and the sigil
- [ ] Dry run on a copy, single commit, cost after commit
- [ ] Every refusal and feedback message id localized
- [ ] Ascension family (Phase 1)
- [ ] Lesser Growth, Lesser Turmoil, Lesser Perfection (Phase 1)
- [ ] Greater grades, Upheaval, Severing, Unmaking (Phase 2)
- [ ] Serpent outcomes and fallbacks, seal, `sigil_would_strand` (Phase 2)
- [ ] Binding one-at-a-time, protection across verbs (Phase 2)
- [ ] Chance weights, Mythic 0 (Phase 2)
- [ ] Reflection: free slot, equipped flag cleared, sealed copy (Phase 2)
- [ ] Confirm gate: hold-Shift / dialog / off, client preference
- [ ] Prefabs registered for disabled stones

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: `item_weight`, reserved pool, open questions moved to `../DECISIONS.md`. | pending |
| 2026-09-23 | Phase 2 verbs built (`reroll_affixes`, `remove`, `strip`, `corrupt`, `lock`, `duplicate`, `gamble`, `quality`, `sigil`); all 19 remaining stones enabled in the defaults. Not tested in game. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Stones: STN-1 to STN-9; Binding is ITD-3, the
confirm mode APP-4, the Serpent with a pending sigil SIG-2; disabled built-in stones RC-3, owner prefabs RC-4).

# EliteCrafting - specification: Runes

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the seven runes: how a rune is applied, every refusal, and what each rune does, step by step, with its
edge cases. Players see **rune**; the code keeps the internal word **stone** (`StoneDef`, `StoneVerb`, the `Stones/`
folder, this file's name, the localization keys `$ecf_stone_<id>`, the message ids `stone_disabled` and
`not_enough_stones`), as it keeps **affix** where players see **inscription**. **Rarity counts, promotion and the
inscription roll** are `rarity.md`; **tier ceilings** are `item-tier.md`; **where runes drop** is `drops.md`; **the
YAML fields** are `economy-yaml.md`.

Numbers are defaults and all of them are configurable in the `runes:` section of `EliteCrafting_economy*.yml`. The
YAML can tune or disable a rune; it cannot add one.

**Status: the six runes built 2026-10-02 (user decision), the Recasting Rune added 2026-10-04 (user decision); not
tested in game.**

---

# 1. Applying a rune

The mechanism - gesture, pipeline, refusal display, confirm gate, cost payment - is owned by `applying-stones.md`.
This section restates it from the runes' side; where the two differ, `applying-stones.md` is the mechanism and this
file is the per-rune content.

## The gesture

The player picks up a rune stack in the inventory and clicks it onto the target item (InventoryGui patch). One use
per click. Costs above 1 are taken from **the held stack only** - runes in other stacks are not pooled (judgement
call: the player sees exactly which stack pays). A rune dropped onto **another rune** is not a use at all: the game
merges or swaps them as usual (`applying-stones.md` section 1).

## The check order

Every check runs **before** anything changes. The first failing check refuses the rune with its message, and **a
refusal never consumes anything**.

1. Both the rune and the target are in the player's **own inventory** (not an open container, not the ground).
2. The target is a **magic base** (`item-data.md` section 2: max stack 1, resolves to a slot, not a rune).
3. The target's data format is not newer than this build's (`item-data.md` section 8).
4. The rune has a **live definition** in the economy YAML.
5. That definition is **enabled** and its verb is one this build performs.
6. The target is **not sealed**.
7. The target is **not equipped**, only when the synced "modify equipped items" setting is off (default on).
8. The target's rarity is **known** (an owner has not removed it from the YAML - `item-data.md` section 6).
9. The target's rarity is in the rune's **`applies_to`**.
10. The held stack holds at least the rune's **cost** for that rarity (the rarity *before* the rune acts).
11. The rune's own **precondition** (the item is full, for Shaping and Consecrated).
12. The **dry run**: the rune's action is carried out on a copy of the item's parsed record. If the result is
    invalid - the pool could not supply an inscription - the rune is refused with that reason. The dry-run record is
    exactly what commits; nothing is rolled twice.
13. The **confirm gate**, for runes with `confirm: true` (section 6). Last on purpose: the player is only ever asked
    to confirm a rune that would work.

## Commit

On success, in this order, all on the owning client in one frame:

1. The dry-run result is written to the item's custom data (one write; the parse cache entry is replaced).
2. The cost is removed from the held stack; the rest of the stack stays on the cursor.
3. If the item is equipped, the equipment rebuild runs (aggregated status effect and item-local caches).
4. A feedback message (section 3) and the vanilla item-move sound. Phase 3 adds a transient effect, sent only to the
   peers that can see the player (`applying-stones.md` section 6).

**Transactional by design.** Because every rune acts on a copy and commits only on success, there is no state in
which a rune was consumed and the item half-changed, and every "cannot" is discovered before the player pays.

**What "success" means**, for every rune: the dry run produced a valid new item state. The Serpent Rune's
`seal_only` outcome changes nothing but the seal; that is still a success and still costs the rune. Refusals are only
for outcomes known *before* rolling.

---

# 2. Refusals

Every refusal: rune kept, item unchanged, message shown in the center of the screen. English text is the default for
the localization key; `$1`, `$2`, `$3` are the game's own placeholders, filled by the code (`applying-stones.md`
section 2).

| Message id | English text | Raised by |
| --- | --- | --- |
| `$ecf_msg_not_own_inventory` | Runes work only on items in your own inventory. | all |
| `$ecf_msg_not_magic_base` | This item cannot hold inscriptions. | all (includes stackable items) |
| `$ecf_msg_newer_format` | *(owned by `item-data.md`)* | all |
| `$ecf_msg_unknown_rarity` | *(owned by `item-data.md`)* | all |
| `$ecf_msg_stone_disabled` | The $1 is disabled on this server. | all |
| `$ecf_msg_sealed` | This item is sealed. No rune can change it. | all |
| `$ecf_msg_equipped` | Unequip this item first. | all, only when modifying equipped items is off |
| `$ecf_msg_wrong_rarity` | The $1 does not work on $2 items. | all |
| `$ecf_msg_not_enough_stones` | You need $1 $2 for this. | all with cost above 1 |
| `$ecf_msg_affixes_full` | This item cannot hold another inscription. | Shaping, Consecrated |
| `$ecf_msg_no_eligible_affix` | No inscription can roll on this item. | Awakening, Shaping, Recasting, Ascension, Consecrated (dry run) |
| `$ecf_msg_confirm_required` | Hold Shift to use the $1. | Cleansing, Serpent (hold-Shift mode) |

The confirm dialog (dialog mode) uses `$ecf_ui_confirm_title` ("Use the $1?") and `$ecf_ui_confirm_body` ("$2
cannot be undone."); they are labels, not refusals.

---

# 3. Feedback messages

Shown on success. Owned here so every rune has one; styling is the display file's.

| Message id | English text | Rune |
| --- | --- | --- |
| `$ecf_msg_promoted` | $1 rises to $2. | Awakening, Ascension |
| `$ecf_msg_affix_added` | $1 gains $2. | Shaping, Consecrated |
| `$ecf_msg_rerolled` | $1 is recast with new inscriptions. | Recasting |
| `$ecf_msg_epic_rerolled` | $1 is recast with new enchantments. | Recasting, on an Epic Loot item |
| `$ecf_msg_stripped` | $1 is cleansed. | Cleansing |
| `$ecf_msg_corrupt_seal` | The Serpent seals $1. Nothing else changes. | Serpent, `seal_only` |
| `$ecf_msg_corrupt_add` | The Serpent seals $1 and grants $2. | Serpent, `add_inscription` |
| `$ecf_msg_corrupt_chaos` | The Serpent seals $1 and twists every inscription. | Serpent, `chaotic_reroll` |

---

# 4. Rules every rune shares

These hold for every rune unless its own section says otherwise; the per-rune tables below repeat only what differs
or needs stating.

- **Sealed** items refuse every rune (`$ecf_msg_sealed`). Sealed is permanent: not even Cleansing lifts it
  (`../DECISIONS.md` STN-1).
- **Equipped** items may be modified by default (synced setting, default on). When on, the equipment rebuild runs on
  commit. When off, `$ecf_msg_equipped`.
- **Stacks**: no magic base is stackable, so a rune can never meet a stack of magic items. A rune dropped on any
  stackable item (arrows, food, materials) is `$ecf_msg_not_magic_base`; dropped on another rune, the game merges or
  swaps them as usual (`applying-stones.md`).
- **Dormant (orphaned) inscriptions** - inscriptions whose id is no longer defined, or is disabled (user decision:
  kept, shown greyed, inert):
  - they **count** toward the rarity's minimum and maximum (they occupy space until removed), so they can make an
    item full for Shaping and Consecrated;
  - promotion, Shaping and Consecrated keep them;
  - Recasting, Cleansing and the Serpent's `chaotic_reroll` remove them - that is how a player clears them.
- The item's **vanilla upgrade level**, durability, crafter name and variant are untouched by every rune.
- **Tier ceiling and window** apply to every roll except the Serpent's `chaotic_reroll` (`item-tier.md` section 6).
- **The Serpent never changes the rarity.** Promotion (Awakening, Ascension) and Cleansing are the only ways an item
  changes rarity.
- **Cost** defaults to 1 for every rune at every rarity; 0 makes a rune free.
- **Tier floor** (`tier_floor`) defaults to none for every rune. Set, it is the weakest inscription tier the rune's
  added inscriptions may roll.

---

# 5. The catalog

| id | Display name | Prefab | Verb | applies_to | Cost | Confirm | Does |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `awakening` | Awakening Rune | `ECF_Awakening` | promote | normal | 1 | no | Normal -> Magic, one inscription |
| `shaping` | Shaping Rune | `ECF_Shaping` | add | magic | 1 | no | one more inscription on a Magic item (up to 2) |
| `recasting` | Recasting Rune | `ECF_Recasting` | reroll | magic | 1 | no | every inscription rolled again, 1-2, still Magic |
| `ascension` | Ascension Rune | `ECF_Ascension` | promote | magic | 1 | no | Magic -> Rare, inscriptions kept, filled to at least three |
| `consecrated` | Consecrated Rune | `ECF_Consecrated` | add | rare | 1 | no | one more inscription on a Rare item (up to 6) |
| `cleansing` | Cleansing Rune | `ECF_Cleansing` | strip | magic, rare | 1 | **yes** | back to Normal, every inscription gone |
| `serpent` | Serpent Rune | `ECF_Serpent` | corrupt | magic, rare | 1 | **yes** | one of three outcomes, then sealed for good |

**Only these seven ids exist.** The list is fixed in code (`StoneCatalog`); a `runes:` entry with any other id is an
error, and an entry may name no prefab but its own. Display-name localization: `$ecf_stone_<id>`, description
`$ecf_stone_<id>_desc`.

**The path of an item:** Normal -> Awakening -> Magic (1) -> Shaping -> Magic (2) -> Ascension -> Rare (3) ->
Consecrated, three times -> Rare (6). Shaping is optional: Ascension on a one-inscription Magic item adds two.
Recasting rerolls a Magic item the player does not like, as often as they have runes, before it goes on.
Cleansing returns any unsealed Magic or Rare item to Normal; the Serpent ends the path.

---

# 6. The confirm gate

Cleansing and the Serpent Rune (`confirm: true`) cannot be undone.

- **Hold-Shift (default):** clicking without Shift held refuses with `$ecf_msg_confirm_required`; clicking with Shift
  applies.
- **Dialog:** clicking opens a yes/no dialog naming the rune and item; yes applies (all checks re-run at that
  moment - the item may have changed), no or closing does nothing.
- **Off:** applies immediately.

The mode is a **client preference** in the `.cfg` (`Confirm destructive runes`), unsynced: it protects the player
from their own mouse, not the server from the player. Which runes are gated is **synced**, in the rune entry's
`confirm` field.

---

# 7. Awakening and Ascension (verb `promote`)

Each moves an item exactly **one rarity up** from the rarity in its `applies_to`: Awakening Normal -> Magic,
Ascension Magic -> Rare.

## Behaviour

1. Checks (section 1).
2. Promote per `rarity.md` section 3: rarity + 1; keep every inscription; add `max(new.min - count,
   rolling.promote_adds_at_least)` inscriptions, capped at the new maximum.
3. Success: the rarity changed and the inscriptions were added.

| From | Rune | To | Added |
| --- | --- | --- | --- |
| Normal (0) | Awakening | Magic | 1 |
| Magic (1) | Ascension | Rare | 2 (to Rare's minimum of 3) |
| Magic (2) | Ascension | Rare | 1 |

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Wrong rarity (Ascension on a Normal item, Awakening on a Magic item) | refused | `wrong_rarity` |
| Pool cannot supply the inscriptions promotion needs | refused | `no_eligible_affix` |
| Sealed | refused | `sealed` |
| Dormant inscriptions present | kept, count toward the new count | - |
| Equipped | allowed (setting on); rebuild on commit | - |
| Item already at the new rarity's maximum (an owner changed the counts) | rarity changes, nothing added | `promoted` |
| An owner put the top rarity in a promote rune's `applies_to` | refused: there is no rarity above it | `wrong_rarity` |
| Upgrade level | untouched | - |

---

# 8. Shaping and Consecrated (verb `add`)

Adds one inscription, up to the rarity's maximum: Shaping on a Magic item (up to 2), Consecrated on a Rare item (up
to 6).

## Behaviour

1. Checks. Refused when the item already holds its rarity's maximum (dormant inscriptions counted).
2. Roll one inscription (`rarity.md` section 4) under the item's ceiling and window.
3. Success: exactly one inscription added, at the end of the list.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Item full (count >= rarity maximum) | refused | `affixes_full` |
| Item full only because of dormant inscriptions | refused - Cleansing clears them, with everything else | `affixes_full` |
| Every candidate excluded (same id / exclusion group / slot / requirements) | refused | `no_eligible_affix` |
| Wrong rarity (Shaping on a Rare item, Consecrated on a Magic item) | refused | `wrong_rarity` |
| Sealed (including a Serpent item past its maximum) | refused | `sealed` |
| Equipped | allowed; rebuild | - |

---

# 8a. Recasting Rune (verb `reroll`)

Rerolls a Magic item (user decision 2026-10-04: the "alteration" currency, renamed). Every inscription goes and the
item is rolled fresh at its own rarity, as a dropped item is (`rarity.md` section 4): the count drawn again in the
rarity's range (1-2 on Magic; `rolling.count_weights` if set), each inscription under the item's ceiling and window
and the rune's `tier_floor`. The rarity never changes.

## Behaviour

1. Checks (section 1). No precondition of its own beyond the rarity: a Magic item with one inscription or two.
2. Roll fresh at the item's rarity on a copy. The same inscriptions may come back: it is a new draw, not an exclusion.
3. Success: the new set replaces the old one.

Judgement calls: **no confirm gate** by default (it is the rune a player spends again and again; an owner can set
`confirm: true`); about as common as Shaping in the drop tables (`drops.md` section 5).

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Dormant inscriptions present | removed with the rest; they no longer count | `rerolled` |
| The pool cannot reach the rarity's minimum (every candidate excluded) | refused, nothing changes | `no_eligible_affix` |
| An owner put the base rarity (Normal) in its `applies_to` | refused: a Normal item holds no inscriptions | `wrong_rarity` |
| An owner put `rare` in its `applies_to` | works: 3-6 rolled fresh on the Rare item | `rerolled` |
| Sealed | refused | `sealed` |
| Equipped | allowed; rebuild | - |
| Epic Loot installed | every effect replaced by a fresh Epic Loot roll of the item's rarity, renamed, sockets kept | `epic_rerolled` |

---

# 9. Cleansing Rune (verb `strip`), confirm gate

Strips an item back to Normal.

## Behaviour

1. Checks, including the confirm gate.
2. Remove every inscription, dormant ones included; the rarity becomes the base rarity.
3. **Kept**: vanilla upgrade level, durability, crafter, variant (none of them live in our keys), and unreadable
   segments (`item-data.md` section 4: never destroyed).
4. Success: the item is Normal with no inscriptions. It may be awakened again.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Normal | not in `applies_to` | `wrong_rarity` |
| Sealed | refused (sealed is permanent) | `sealed` |
| Rare with six inscriptions | stripped to Normal like any other | - |
| Equipped | allowed; rebuild | - |
| No Shift held (hold-Shift mode) | refused | `confirm_required` |

---

# 10. Serpent Rune (verb `corrupt`), confirm gate

Corrupts an item: one random outcome from a weighted table, then the item is **sealed** for good - no rune works on
it again. The rarity never changes.

## Outcome table (defaults)

| Outcome id | Weight | What happens | If it cannot be carried out |
| --- | --- | --- | --- |
| `seal_only` | 35 | Nothing but the seal | - |
| `add_inscription` | 35 | Add one inscription (normal roll, ceiling and window); may exceed the rarity's maximum by `overflow` (default 1): a 3rd on a full Magic item, a 7th on a full Rare | falls back to `seal_only` |
| `chaotic_reroll` | 30 | Remove **every** inscription (dormant ones included); draw the count in the rarity's range; roll each with **any tier the inscription defines, uniformly**, ignoring ceiling, window and floor (`item-tier.md` section 6) | if the pool cannot reach the count, fill what it can; if it can fill none, `seal_only` |

Weights are relative; any outcome can be given weight 0. A table whose every weight is 0 disables the rune with a
load warning (`economy-yaml.md` section 9).

## Behaviour

1. Checks, including the confirm gate.
2. Draw the outcome by weight. Carry it out on the copy; fall back as the table says.
3. Set sealed (`ecf_sealed = serpent`). Commit.
4. Success: always, once the checks pass - `seal_only` is a success.

The outcome is drawn in the dry run, and that dry run is what commits. In Dialog mode the Yes re-runs the checks and
so draws afresh; the player never saw the first draw.

## Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| Normal | not in `applies_to` (nothing to corrupt) | `wrong_rarity` |
| Already sealed | refused | `sealed` |
| Rare with 6 + `add_inscription` | a 7th inscription | `corrupt_add` |
| Magic with 2 + `add_inscription` | a 3rd inscription; still Magic | `corrupt_add` |
| `add_inscription` with no candidate left | sealed, nothing else changes | `corrupt_seal` |
| An owner put `normal` in `applies_to` + `add_inscription` | sealed, nothing else changes (a Normal item holds no inscriptions) | `corrupt_seal` |
| Dormant inscriptions | removed by `chaotic_reroll`; untouched otherwise | - |
| Equipped | allowed; rebuild | - |
| No Shift held | refused | `confirm_required` |

---

# 11. The rune items themselves

- Stackable (default 50), light (default 0.2), tradeable, teleportable, no trade value, drop as world items. `stack`
  and `item_weight` in each rune entry (`economy-yaml.md` section 4).
- **Prefabs come from code, not from the YAML** (`configuration.md` section 5, `prefabs.md`): all seven are registered
  on every peer, identically, whatever the YAML says. A rune the YAML disables must still exist as a prefab, or the
  game would delete stacks of it from chests and inventories on load.
- `enabled: false` (or no live definition at all) means: never drops, cannot be applied (`stone_disabled`), still
  exists as an item that can be stored and traded (`../DECISIONS.md` RC-3).
- Visuals (base mesh per group, tint) are `prefabs.md`'s.

---

# 12. Multiplayer

- Runes apply **only in the player's own inventory**, on the client that owns it. No container is ever edited
  remotely, so there are no ownership races.
- The rules (applies_to, costs, outcome tables, confirm list, enabled) are server-synced and lockable. The dice roll
  on the client under the server's odds.
- The changed item travels in the game's own item serialization: inventory saves, containers, the ground,
  tombstones. No netcode for the result.
- Refusals are decided identically for host-less clients on a dedicated server, because every check reads either the
  item or the synced rules.
- The only RPC is the Phase 3 transient feedback effect, scoped to peers who can see the player.

---

# Build checklist

- [ ] Click-to-apply in the inventory; cost from the held stack
- [ ] Check order 1-12; every refusal keeps the rune
- [ ] Dry run on a copy, single commit, cost after commit
- [ ] Every refusal and feedback message id localized
- [ ] Awakening and Ascension: one rarity up, the counts of section 7
- [ ] Shaping and Consecrated: one more inscription, refused when full
- [ ] Recasting: every inscription rerolled, 1-2, still Magic; refused on Normal and Rare
- [ ] Cleansing: back to Normal, confirm gate
- [ ] Serpent: three outcomes and their fallbacks, seal, rarity unchanged
- [ ] Confirm gate: hold-Shift / dialog / off, client preference
- [ ] Prefabs registered for disabled runes

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0 (the earlier stone catalog). | pending |
| 2026-10-02 | Rewritten for the six runes and three rarities (user decision); built. Not tested in game. | pending |
| 2026-10-04 | The Recasting Rune, verb `reroll` (user decision); built. Not tested in game. | pending |

---

# Decisions

Judgement calls are in `../DECISIONS.md` (sealed refuses every rune: STN-1; the confirm mode: APP-4; disabled runes
keep their prefab: RC-3) and the 2026-10-02 entry of `../PLAN.md`'s Decisions log.

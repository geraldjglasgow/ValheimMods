# EliteCrafting - specification: Rarity

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the three rarities: what each one is, how many inscriptions (affixes, in the code's word) it
carries, what it means to move an item up the ladder, and how a rarity's affixes are rolled. **Which tier an affix
may reach** on a given item is `item-tier.md`. **The runes that move items along the ladder** are `stones.md`. **What
each affix does** is `affixes.md`. **Where the rarity is stored on the item** is the core item-data file
(`item-data.md`).

Numbers are defaults and all of them are configurable in the `rarities:` and `rolling:` sections of
`EliteCrafting_economy*.yml` (`economy-yaml.md`).

**Status: built, not tested in game.** Cut to three rarities on 2026-10-02 (user decision, `../PLAN.md` Decisions
log).

---

# 1. The ladder

Rarity is **affix count**. Strength is a separate axis - an affix's tier - capped by the item's tier
(`item-tier.md`). A Meadows Rare rolls many weak affixes; an Ashlands Magic item rolls few strong ones.

| Order | id | Name (English) | Affixes | Color | Glows on the ground | Default acquisition |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | `normal` | Normal | 0 | `#FFFFFF` | never | vanilla items; never drops as magic gear |
| 1 | `magic` | Magic | 1-2 | `#1EFF00` | yes | drops + the Awakening Rune |
| 2 | `rare` | Rare | 3-6 | `#0070DD` | yes | drops + the Ascension Rune |

- The colors are the single source for every rarity-colored surface: item names, tooltip accents, ground glow. They
  live in the rarity entries of the economy YAML so an owner rethemes in one place.
- Localization keys: `$ecf_rarity_normal`, `$ecf_rarity_magic`, `$ecf_rarity_rare`.
- **Normal is the vanilla state.** An item with no EliteCrafting data at all *is* Normal; so is an item that was
  stripped by the Cleansing Rune. Normal is the *absence* of the rarity key (`item-data.md` section 3), so the two are
  indistinguishable.
- **Ladder order is the order of the entries in `rarities:`.** "One rarity up" means the next entry. Code never
  hard-codes the three ids except in one place: the first entry is the base rarity (see validation, section 7).
  Everything else - names, colors, counts, even the number of rarities - is data.
- **Old item data** written before 2026-10-02 carries rarity ids that no longer exist; they are renamed on read
  (`item-data.md` section 8): `uncommon` is Magic, `epic`, `legendary` and `mythic` are Rare, `common` is Normal.

---

# 2. Which items can have a rarity at all

An item can carry a rarity above Normal only if it is a **magic base** (defined in `item-data.md` section 2, which
owns the item-type-to-slot map; restated here):

1. Its item type resolves to one of the ten slots (`melee_weapon`, `ranged_weapon`, `magic_weapon`, `shield`,
   `head`, `chest`, `legs`, `cape`, `utility_item`, `tool`). The item-type-to-slot map is `item-data.md`'s.
2. **It is not stackable**: its shared data's max stack size is exactly 1.
3. It is not one of our own runes.

**Why magic items must be non-stackable** - verified in the decompile: the game merges a picked-up item into an
existing stack when the *name, upgrade level and world level* match, and never looks at custom data. Two magic
arrows with different affixes would silently become one stack with one set of affixes. There is no safe way for
a stackable item to carry per-copy state, so the rule is absolute.

How it is enforced:

- Every rune checks rule 2 at application time and refuses with `$ecf_msg_not_magic_base` (`stones.md`).
- The item-data writer refuses to write to a stackable item at all (`item-data.md` section 7).
- The pre-rolled drop pool never contains a stackable base (`drops.md`), and `ecraft roll` refuses one.
- **At load, a warning is logged for every slot-resolving item whose max stack size is above 1** - this is what
  catches another mod (or OpenKeep's stack settings) making weapons or armor stackable. Existing magic copies of
  such a base keep working, but they can lose their affixes if the game merges them; the warning says so
  (`../DECISIONS.md` RAR-6).

---

# 3. Promotion

The one move up the ladder, used by the Awakening and Ascension Runes.

1. The item's rarity becomes the next rarity on the ladder. Every existing affix is kept, dormant ones included,
   with its tier and value unchanged.
2. New affixes are rolled (section 4) until the item holds **the new rarity's minimum, and at least
   `rolling.promote_adds_at_least` (default 1) more than before** - capped at the new rarity's maximum.
   - `added = clamp(max(new.min - count, promote_adds_at_least), 0, new.max - count)`

| From (affixes) | Rune | To | Added | Result |
| --- | --- | --- | --- | --- |
| Normal (0) | Awakening | Magic | 1 | 1 |
| Magic (1) | Ascension | Rare | 2 | 3 |
| Magic (2) | Ascension | Rare | 1 | 3 |

**The "at least one" rule** makes every promotion visibly add power even when the item already meets the new
minimum (RAR-1); with the default ladder it never has to, since Rare's minimum is above Magic's maximum. The knob is
`rolling.promote_adds_at_least` (0 adds only what the minimum needs).

If the pool cannot supply the affixes promotion needs (section 4, "the pool is exhausted"), **the promotion is
refused, not partially applied** (`$ecf_msg_no_eligible_affix`). A rune never leaves an item below its rarity's
minimum.

There is **no demotion**. Nothing moves an item one rarity down; the Cleansing Rune takes it all the way back to
Normal (section 5).

---

# 4. How affixes are rolled

Every rune that adds affixes, the Serpent Rune's outcomes and every pre-rolled drop use this one procedure. It runs on
the peer that owns the item (the client, for a rune; the creature's owner, for a drop) against the server-synced
rules, so every peer rolls under the same odds.

## Inputs

- The item's **slot** (from its type) and **tier ceiling** (`item-tier.md`).
- The **tier floor** of the rune, if any (`tier_floor` on the rune; drops have none).
- The affixes already on the item.

## The tier window

An item rolls affix tiers from a window just below its ceiling, not from the weakest tier upward. This section
counts in strength grades, the code's numbering (grade 1 = Meadows strength ... 7 = Ashlands strength); players and
the YAML see tier = 8 - grade, so grades 5-7 are tooltip tiers 3-1 (`affixes.md`, "Tiers and biome gates"):

- `low = max(ceiling - rolling.tier_window + 1, tier_floor, 1)`, `high = ceiling` (grades; `tier_floor` converted).
- Default `tier_window: 3`. An Ashlands item (ceiling 7) rolls grades 5-7, shown as tiers 3-1; a Swamp item
  (ceiling 3) rolls grades 1-3, shown as tiers 7-5; a Meadows item rolls only grade 1, shown as tier 7.
- **Judgement call.** Without a window, an Ashlands weapon could roll Meadows-strength affixes, which reads as a
  bug to a player. Window 7 (off) gives PoE-style "item level only gates the top"; window 1 makes every roll
  exactly the ceiling tier (RAR-2).
- An affix that defines **no tier inside the window** but does define tiers below it (an affix that stops scaling
  early, e.g. a flat bonus that exists only at tiers 1-2) is eligible at **its highest tier at or below the
  ceiling**, provided that tier is at least the rune's `tier_floor`. It never becomes ineligible just because the
  item outgrew it.
- The window and the ceiling are ignored by exactly one roll in the mod: the Serpent Rune's chaotic reroll
  (`stones.md`, `item-tier.md` section 6).

## Eligibility of an affix

An affix is a candidate when **all** hold:

1. `enabled: true`, a `weight` above 0, and its effect id is registered in this build.
2. The item's slot is in its `slots`, and the item satisfies its `requires`.
3. No affix with the same id is on the item (dormant ones included - a dormant copy still occupies the id).
4. No affix of the same `exclusion_group` is on the item (dormant affixes whose definition is gone have no known
   group and block nothing).
5. It has at least one eligible tier (the window rules above).

## The two-stage draw

1. **Pick the affix**, weighted by the affix's `weight` (default 100; schema in `configuration.md` section 6,
   values in `affixes.md`).
2. **Pick its tier** among its eligible tiers, weighted by the per-tier `weight` in its `tiers` list.
3. **Pick its value** uniformly in that tier's `[min, max]` (rounding rules per value type are `affixes.md`'s).
4. Remove the chosen affix and its exclusion group from the candidate set; repeat for the next affix.

Two stages rather than one flat draw over (affix, tier) pairs, so that an affix with seven tiers is not seven times
likelier than one with a single tier. **Judgement call** (RAR-3).

## How many affixes

- A **promotion** adds the count in section 3; **Shaping**, **Consecrated** and the Serpent's `add_inscription` add
  exactly one.
- A **fresh roll** - a pre-rolled drop, `ecraft roll`, the Serpent's `chaotic_reroll` - draws the count
  **uniformly** in `[min, max]` of the rarity.
- `rolling.count_weights` (optional, per rarity) can bias the draw, e.g. `rare: {3: 4, 4: 3, 5: 2, 6: 1}`. Absent means
  uniform.

## The pool is exhausted

If fewer candidates exist than the roll needs:

- **Runes** detect it before anything happens (every rune works on a copy and commits only on success -
  `stones.md` section 1) and refuse with `$ecf_msg_no_eligible_affix`. The rune is kept. The Serpent never refuses:
  it fills what it can, or only seals (`stones.md` section 10).
- **Pre-rolled drops** fall back to the next lower rarity that may drop and whose minimum the pool can fill; a base
  whose pool cannot fill even the lowest magic rarity is left out of the drop pool at load (`drops.md` section 8).

---

# 5. Down the ladder, and items outside their range

- **Cleansing** is the only way down: every affix goes and the item becomes the base rarity (Normal), whatever rarity
  it had (`stones.md` section 9).
- **The Serpent Rune never changes the rarity.** Its `add_inscription` outcome may leave an item `overflow` (default
  1) past its rarity's maximum - a Magic item with three, a Rare with seven. The item is sealed, so nothing reads it
  as full or tries to fill it again.
- **An owner who edits the counts** can leave existing items with more or fewer affixes than their rarity now allows.
  Nothing corrects them automatically - values on items are fixed (user decision). The runes read the *current*
  ranges: Shaping and Consecrated are refused at or above the maximum, and a promotion fills to the new rarity's
  current minimum.

---

# 6. Multiplayer

- The rarity, affixes and every other part of an item's state live in the item's own custom data, which the game
  serializes and replicates everywhere. No netcode.
- The rarity *definitions* (names, colors, counts, window) are server-synced and lockable with the rest of the
  economy YAML. A client with a different local file sees the server's colors while bound.
- Rolls happen on the item's owning peer under the synced rules, so the odds cannot be changed client-side even
  though the dice are local. (Client trust is Valheim's own inventory model; this mod does not try to do better.)

---

# 7. Validation (economy YAML load)

Errors reject the files (previous configuration stays); warnings are logged and the files apply.

- Error: fewer than two rarities; the same id twice in one file; the first entry's `inscriptions.max` is not 0.
- Error: `inscriptions` or `color` missing; `min > max`; negative counts; `color` not `#RRGGBB`.
- Error: a rarity id referenced by a rune's `applies_to` or `cost`, or by a drop table, does not exist.
- Warning: `glow: true` on the base rarity (ignored - Normal never glows, user decision).
- Warning (when the gear pool is built): bases whose slot pool cannot fill the lowest magic rarity's minimum at their
  tier, listed by name; they never drop (`drops.md` section 8).

---

# Build checklist

`[ ]` not started, `[~]` partly, `[x]` built and seen working on a dedicated server.

- [ ] Three rarities from YAML, ladder order from entry order
- [ ] Old rarity ids read as their new rarity (`item-data.md` section 8)
- [ ] Magic-base rule: slot resolves, max stack 1, not a rune; load warning for stackable slot items
- [ ] Promotion adds to the new minimum, at least one; refused when the pool cannot fill
- [ ] Tier window, highest-tier fallback for early-stopping affixes
- [ ] Two-stage draw: affix by weight, tier by tier weight, value uniform
- [ ] `drop_weight: 0` on Normal keeps it out of every drop table

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: open questions moved to `../DECISIONS.md`. | pending |
| 2026-10-02 | Three rarities, promotion only (user decision); old ids renamed on read. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Rarity: RAR-1 to RAR-6) and the 2026-10-02 entry
of `../PLAN.md`'s Decisions log.

# EliteCrafting - specification: Rarity

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the six rarities: what each one is, how many affixes it carries, what it means to move an item up
or down the ladder, and how a rarity's affixes are rolled. **Which tier an affix may reach** on a given item is
`item-tier.md`. **The stones that move items along the ladder** are `stones.md`. **What each affix does** is
`affixes.md`. **Where the rarity is stored on the item** is the core item-data file (`item-data.md`).

Numbers are defaults and all of them are configurable in the `rarities:` and `rolling:` sections of
`EliteCrafting_economy*.yml` (`economy-yaml.md`).

**Status: Phase 1 built, not tested in game** (2026-09-23). Ships in **Phase 1** (0.1.0), except the Mythic-only affix pool, which
is Phase 3 - until then a Mythic's special affix is drawn from the regular pool (section 5).

---

# 1. The ladder

Rarity is **affix count**. Strength is a separate axis - an affix's tier - capped by the item's tier
(`item-tier.md`). A Meadows Mythic rolls many weak affixes; an Ashlands Uncommon rolls few strong ones.

| Order | id | Name (English) | Affixes | Color | Glows on the ground | Default acquisition |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | `common` | Common | 0 | `#FFFFFF` | never | vanilla items |
| 1 | `uncommon` | Uncommon | 1-2 | `#1EFF00` | yes | drops + crafting |
| 2 | `rare` | Rare | 2-3 | `#0070DD` | yes | drops + crafting |
| 3 | `epic` | Epic | 3-4 | `#A335EE` | yes | drops + crafting |
| 4 | `legendary` | Legendary | 4-5 | `#FF8000` | yes | rare drops + crafting |
| 5 | `mythic` | Mythic | 6, of which 1 from the Mythic-only pool | `#E6262E` | yes | **craft-only** (Stone of Apotheosis) |

- The colors are canonical (user decision 2026-09-23) and are the single source for every rarity-colored surface:
  item names, tooltip accents, ground glow. They live in the rarity entries of the economy YAML so an owner
  rethemes in one place.
- Localization keys: `$ecf_rarity_common` ... `$ecf_rarity_mythic`.
- **Common is the vanilla state.** An item with no EliteCrafting data at all *is* Common; so is an item that was
  stripped by the Stone of Unmaking. Common is the *absence* of the rarity key (`item-data.md` section 3), so the two
  are indistinguishable. A Common item may still carry its honing/tempering bonus.
- **Ladder order is the order of the entries in `rarities:`.** "One rarity up" and "one rarity down" mean the next
  and previous entry. Code never hard-codes the six ids except in two places: the first entry is the base rarity
  (see validation, section 7), and the rarity whose entry has `mythic_affixes > 0` gets the special-pool behaviour
  of section 5. Everything else - names, colors, counts, even the number of rarities - is data.

---

# 2. Which items can have a rarity at all

An item can carry a rarity above Common only if it is a **magic base** (defined in `item-data.md` section 2, which
owns the item-type-to-slot map; restated here):

1. Its item type resolves to one of the ten slots (`melee_weapon`, `ranged_weapon`, `magic_weapon`, `shield`,
   `head`, `chest`, `legs`, `cape`, `utility_item`, `tool`). The item-type-to-slot map is `item-data.md`'s.
2. **It is not stackable**: its shared data's max stack size is exactly 1.
3. It is not one of our own stones or sigils.

**Why magic items must be non-stackable** - verified in the decompile: the game merges a picked-up item into an
existing stack when the *name, upgrade level and world level* match, and never looks at custom data. Two magic
arrows with different affixes would silently become one stack with one set of affixes. There is no safe way for
a stackable item to carry per-copy state, so the rule is absolute.

How it is enforced:

- Every stone checks rule 2 at application time and refuses with `$ecf_msg_not_magic_base` (`stones.md`).
- The item-data writer refuses to write to a stackable item at all (`item-data.md` section 7).
- The pre-rolled drop pool never contains a stackable base (`drops.md`), and `ecraft roll` refuses one.
- **At load, a warning is logged for every slot-resolving item whose max stack size is above 1** - this is what
  catches another mod (or OpenKeep's stack settings) making weapons or armor stackable. Existing magic copies of
  such a base keep working, but they can lose their affixes if the game merges them; the warning says so
  (`../DECISIONS.md` RAR-6).

---

# 3. Promote and demote

Two ladder moves, used by the ascension stones, the Stone of Chance and the Serpent Stone.

## Promote (one rarity up)

1. The item's rarity becomes the next rarity on the ladder. Every existing affix is kept, bound and dormant ones
   included, with its tier and value unchanged.
2. New affixes are rolled (section 4) until the item holds **the new rarity's minimum, and at least one more than
   before** - capped at the new rarity's maximum.
   - `added = clamp(max(new.min - count, 1), 0, new.max - count)`
3. Promoting into a rarity with `mythic_affixes > 0` first adds the special affix(es) from the Mythic-only pool
   (section 5), then fills with regular affixes to the count above.

| From (affixes) | To | Added | Result |
| --- | --- | --- | --- |
| Common (0) | Uncommon | 1 | 1 |
| Uncommon (1) | Rare | 1 | 2 |
| Uncommon (2) | Rare | 1 | 3 |
| Rare (2) | Epic | 1 | 3 |
| Rare (3) | Epic | 1 | 4 |
| Epic (3) | Legendary | 1 | 4 |
| Epic (4) | Legendary | 1 | 5 |
| Legendary (4) | Mythic | 2 (1 Mythic-only + 1 regular) | 6 |
| Legendary (5) | Mythic | 1 (Mythic-only) | 6 |

**The "at least one" rule is a clarification of PLAN.md** ("rolls new ones up to the new tier's minimum"). Read
literally, a two-affix Uncommon promoted to Rare (minimum 2) would gain nothing but a color - a stone spent on a
visual change. With the rule, every promotion visibly adds power (RAR-1); the knob is
`rolling.promote_adds_at_least` (default 1; 0 restores the literal reading).

If the pool cannot supply the affixes promotion needs (section 4, "the pool is exhausted"), **the promotion is
refused, not partially applied** (`$ecf_msg_no_eligible_affix`). A stone never leaves an item below its rarity's
minimum.

## Demote (one rarity down) - Serpent Stone only by default

1. One affix is removed at random. **Bound affixes are eligible** - the Serpent is one of the two things that can
   remove a bound affix (`stones.md`, Binding).
2. The rarity becomes the previous one on the ladder.
3. Leaving a rarity with `mythic_affixes > 0`: every affix drawn from the Mythic-only pool is removed (a Legendary
   cannot carry Mythic-only effects), before the random removal of step 1 is counted - so a Mythic losing its
   Mythic-only affix has already "lost an affix" and step 1 is skipped.
4. If the item still holds more affixes than the new rarity's maximum, random affixes are removed until it fits.
5. If the new rarity is the base rarity (Common, 0 affixes), every affix is removed.

| From (affixes) | To | Result |
| --- | --- | --- |
| Uncommon (1 or 2) | Common | 0 |
| Rare (3) | Uncommon | 2 |
| Rare (2) | Uncommon | 1 |
| Epic (4) | Rare | 3 |
| Epic (3) | Rare | 2 |
| Legendary (5) | Epic | 4 |
| Legendary (4) | Epic | 3 |
| Mythic (6) | Legendary | 5 (the Mythic-only affix is removed) |

With the default ladder every demotion lands inside the lower rarity's range. With a custom ladder a demotion can
land **below** the new minimum; the item is then simply under-filled, which is legal - counts are checked when a
stone acts, not continuously.

## Items outside their rarity's range

An owner who edits the counts can leave existing items with more or fewer affixes than their rarity now allows.
Nothing corrects them automatically - values on items are fixed (user decision). The stones read the *current*
ranges: Growth is refused at or above the maximum, Severing at or below the minimum, and Upheaval rolls the new
count from the current range.

---

# 4. How affixes are rolled

Every stone that adds affixes, the Stone of Chance and every pre-rolled drop use this one procedure. It runs on the
peer that owns the item (the client, for a stone; the creature's owner, for a drop) against the server-synced
rules, so every peer rolls under the same odds.

## Inputs

- The item's **slot** (from its type) and **tier ceiling** (`item-tier.md`).
- The **tier floor** of the stone, if any (`tier_floor` on the stone; drops have none).
- The affixes already on the item.
- A **category filter**, only when a War/Warding/Fortune sigil is steering this roll (`sigils.md`).
- Which **pool**: regular, or Mythic-only (section 5).

## The tier window

An item rolls affix tiers from a window just below its ceiling, not from tier 1 upward:

- `low = max(ceiling - rolling.tier_window + 1, tier_floor, 1)`, `high = ceiling`.
- Default `tier_window: 3`. An Ashlands item (ceiling 7) rolls tiers 5-7; a Swamp item (ceiling 3) rolls 1-3; a
  Meadows item rolls only tier 1.
- **Judgement call.** Without a window, an Ashlands weapon could roll Meadows-strength affixes, which reads as a
  bug to a player. Window 7 (off) gives PoE-style "item level only gates the top"; window 1 makes every roll
  exactly the ceiling tier (RAR-2).
- An affix that defines **no tier inside the window** but does define tiers below it (an affix that stops scaling
  early, e.g. a flat bonus that exists only at tiers 1-2) is eligible at **its highest tier at or below the
  ceiling**, provided that tier is at least the stone's `tier_floor`. It never becomes ineligible just because the
  item outgrew it.
- The window and the ceiling are ignored by exactly one roll in the mod: the Serpent Stone's chaotic reroll
  (`stones.md`, `item-tier.md` section 6).

## Eligibility of an affix

An affix is a candidate when **all** hold:

1. `enabled: true`, and its effect id is registered in this build.
2. The item's slot is in its `slots`.
3. It belongs to the pool being drawn from (`mythic_only: true` only when drawing the Mythic-only pool, `false`
   otherwise).
4. No affix with the same id is on the item (dormant ones included - a dormant copy still occupies the id).
5. No affix of the same `exclusion_group` is on the item (dormant affixes whose definition is gone have no known
   group and block nothing).
6. It has at least one eligible tier (the window rules above).
7. It passes the category filter, when one applies.
8. It is not the id the current Turmoil swap just removed (`stones.md`).

## The two-stage draw

1. **Pick the affix**, weighted by the affix's `weight` (default 100; schema in `configuration.md` section 6,
   values in `affixes.md`).
2. **Pick its tier** among its eligible tiers, weighted by the per-tier `weight` in its `tiers` list.
3. **Pick its value** uniformly in that tier's `[min, max]` (rounding rules per value type are `affixes.md`'s).
4. Remove the chosen affix and its exclusion group from the candidate set; repeat for the next affix.

Two stages rather than one flat draw over (affix, tier) pairs, so that an affix with seven tiers is not seven times
likelier than one with a single tier. **Judgement call** (RAR-3).

## How many affixes

- A **promotion** adds the count in section 3.
- A **fresh roll** - a pre-rolled drop, the Stone of Chance, the Stone of Upheaval's count - draws the count
  **uniformly** in `[min, max]` of the rarity. Kept affixes (bound, preserved by a sigil) count toward it; if they
  already exceed the drawn count, nothing more is added.
- `rolling.count_weights` (optional, per rarity) can bias the draw, e.g. `legendary: {4: 3, 5: 1}`. Absent means
  uniform.

## The pool is exhausted

If fewer candidates exist than the roll needs:

- **Stones** detect it before anything happens (every stone works on a copy and commits only on success -
  `stones.md` section 1) and refuse with `$ecf_msg_no_eligible_affix`. The stone is kept.
- **Pre-rolled drops** fall back to the highest rarity whose minimum the pool can fill; a base whose pool cannot
  fill even the lowest magic rarity is left out of the drop pool at load (`drops.md`).

---

# 5. Mythic

- **Six affixes exactly** (`affixes: {min: 6, max: 6}`), of which `mythic_affixes: 1` comes from the Mythic-only
  pool (affixes with `mythic_only: true`) and the rest from the regular pool.
- **Craft-only by default.** `drop_weight: 0` on the Mythic rarity is the master switch: at 0, no drop table and no
  Stone of Chance weight can ever produce a Mythic, whatever the per-biome rows say. An owner who wants Mythic drops
  sets it above 0 *and* gives Mythic a weight in the drop tables (`drops.md`).
- **Ways in:** the Stone of Apotheosis (Legendary -> Mythic), and the Serpent Stone's promote outcome on a Legendary
  (which also seals the item).
- **Until the Mythic-only pool exists (Phase 3)**, or whenever it has no eligible candidate for the item's slot,
  the special affix is drawn from the regular pool instead and the item is still a full Mythic of six. The
  alternative - refusing Apotheosis until Phase 3 - would make the chase stone dead weight for two releases
  (RAR-5).
- Stone behaviour on a Mythic (always full, always at minimum, swaps keep the pool class, 7th affix from the
  Serpent) is in `stones.md`.

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

- Error: fewer than two rarities; duplicate id; the first entry's `affixes.max` is not 0.
- Error: `min > max`, negative counts, `color` not `#RRGGBB`.
- Error: more than one rarity with `mythic_affixes > 0`, or `mythic_affixes > affixes.min`.
- Error: a rarity id referenced by a stone's `applies_to`/`cost`/`weights` or by a drop table does not exist.
- Warning: `glow: true` on the base rarity (ignored - Common never glows, user decision).
- Warning: a rarity's minimum exceeds what the regular pool can supply for some slot (listed per slot).

---

# Build checklist

`[ ]` not started, `[~]` partly, `[x]` built and seen working on a dedicated server.

- [ ] Six rarities from YAML, ladder order from entry order
- [ ] Magic-base rule: slot resolves, max stack 1, not a stone; load warning for stackable slot items
- [ ] Promote adds to the new minimum, at least one; refused when the pool cannot fill
- [ ] Demote removes one (bound eligible), Mythic-only affixes stripped on leaving Mythic
- [ ] Tier window, highest-tier fallback for early-stopping affixes
- [ ] Two-stage draw: affix by weight, tier by tier weight, value uniform
- [ ] Mythic: six, one special, regular-pool fallback until Phase 3
- [ ] `drop_weight: 0` on Mythic blocks every drop and Chance path

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: open questions moved to `../DECISIONS.md`. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Rarity: RAR-1 to RAR-6).

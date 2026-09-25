# EliteCrafting - specification: Sigils

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the five sigils: meta-currency applied to an item *before* a stone, that steers the randomness of
the next stone it applies to. What each stone does on its own is `stones.md`; how affixes are rolled, including the
category filter this file uses, is `rarity.md` section 4.

Numbers are defaults and all of them are configurable in the `stones:` section of `EliteCrafting_economy*.yml`
(sigils are stone entries with verb `sigil`).

**Status: built (Phase 2, 2026-09-23), not tested in game.** Ships in **Phase 2**.

---

# 1. The pending state

- A sigil is applied like a stone: pick up the stack, click it onto an item in your own inventory. One sigil is
  consumed and the item gains a **pending sigil**.
- **One pending sigil per item.** Applying a second one - the same kind or another - is refused
  (`$ecf_msg_sigil_pending`, "A sigil already waits on this item.").
- The pending sigil is stored in the item's custom data (key owned by `item-data.md`) **by stone id**, so an
  owner-defined sigil works the same way. It travels with the item everywhere the item goes: chest, ground,
  tombstone, portal, trade.
- The tooltip shows it on its own line (`$ecf_ui_pending_sigil`: "Pending: $1"), in the display file's style.
- Applying a sigil changes nothing about the item's effects; no rebuild is needed if it is equipped.

## Refusals when applying a sigil

The ordinary check order of `stones.md` section 1 applies. The ones a sigil can meet:

| Situation | Message |
| --- | --- |
| Not in own inventory | `$ecf_msg_not_own_inventory` |
| Not a magic base (stackable, no slot, a stone) | `$ecf_msg_not_magic_base` |
| Sigil disabled | `$ecf_msg_stone_disabled` |
| Item sealed | `$ecf_msg_sealed` |
| Item equipped, modifying equipped items off | `$ecf_msg_equipped` |
| A sigil already pending | `$ecf_msg_sigil_pending` |

Rarity: sigils apply to all six rarities by default, Common included - a War sigil on a Common steers the Stone of
Awakening's or Chance's affix.

---

# 2. What each sigil steers

**A sigil is consumed by the next stone it steers, and only when that stone succeeds.**

| Sigil | Steers these verbs | What "steer" means |
| --- | --- | --- |
| Sigil of Preservation | `swap` (Turmoil), `reroll_affixes` (Upheaval), `reroll_values` (Perfection), `imbue` (essences) | The **protected affix** (section 3) is left untouched: Turmoil cannot remove it, Upheaval and an essence keep it, Perfection does not reroll its value |
| Sigil of War | every verb that adds an affix: `promote`, `add`, `swap`, `reroll_affixes`, `gamble` | **Exactly one** affix the stone adds is drawn from the `offense` category |
| Sigil of Warding | same as War | ... from `defense` |
| Sigil of Fortune | same as War | ... from `utility` |
| Sigil of Culling | every verb that removes one chosen affix: `remove` (Severing), `swap` (Turmoil) | The removed affix is the **culled affix** (section 3) instead of a random one |

Every other pairing is "not steered" (section 4).

## The full matrix

`S` steered and consumed on success; `-` not steered, sigil stays pending; `R` stone refused while any sigil is
pending.

| Stone (verb) | Preservation | War / Warding / Fortune | Culling |
| --- | --- | --- | --- |
| Ascension family (`promote`) | - | S | - |
| Growth (`add`) | - | S | - |
| Turmoil (`swap`) | S | S | S |
| Upheaval (`reroll_affixes`) | S | S | - |
| Perfection (`reroll_values`) | S | - | - |
| Severing (`remove`) | - | - | S |
| Unmaking (`strip`) | - | - | - |
| Serpent (`corrupt`) | R | R | R |
| Binding (`lock`) | - | - | - |
| Chance (`gamble`) | - | S | - |
| Reflection (`duplicate`) | - (the copy gets no sigil) | - | - |
| Honing, Tempering (`quality`) | - | - | - |
| Essences (`imbue`) | S | - | - |
| Another sigil (`sigil`) | refused: `sigil_pending` | refused | refused |

Why Upheaval is not steered by Culling: Upheaval removes every non-kept affix at once; there is no "which one" to
steer. Why Severing is not steered by Preservation: PLAN.md limits Preservation to reroll-type stones, and Severing
already has Culling for choosing what goes. Both are arguable (`../DECISIONS.md` SIG-4, SIG-5).

Why the Serpent refuses: it seals the item, and a sealed item accepts no further stone - the sigil would be stranded
on it for ever. Refusing (`$ecf_msg_sigil_would_strand`) tells the player to spend the sigil first (SIG-2; the
alternative was that the Serpent silently destroys the sigil).

---

# 3. The precise selections

## The protected affix (Preservation)

Among the item's **live, unbound** affixes (dormant affixes have no known strength; the bound one is protected
already):

1. the highest tier;
2. ties broken by the best roll - the value's position within its tier's range, `(value - min) / (max - min)`, with
   flag affixes and single-value tiers counting as 1.0;
3. then the earliest on the item.

Protecting the best of the highest tier is what a player means by "keep my good affix". If there is no live unbound
affix, Preservation finds nothing to protect and the stone is refused (`$ecf_msg_sigil_no_match`).

## The culled affix (Culling)

Among the item's **unbound** affixes:

1. any **dormant** affix first (it does nothing; removing it is always what the player wants), earliest first;
2. otherwise the lowest tier;
3. ties broken by the worst roll (same measure as above, lowest first);
4. then the earliest on the item.

## The category filter (War, Warding, Fortune)

- While the stone rolls its affixes, the category filter applies to **the first draw for which the category leaves
  at least one candidate**. That affix is from the category; every later draw of the same stone is unfiltered.
- For a promotion into Mythic, the Mythic-only draw comes first; if the Mythic-only pool has a candidate in the
  category, that is the steered affix, otherwise the first regular draw is.
- If **no** draw of the stone had a candidate in the category, the stone is refused (`$ecf_msg_sigil_no_match`) - a
  sigil is never spent without steering.
- Mythic-only affixes carry a category like every other affix (conventions).

---

# 4. When the next stone does not use the sigil

Two different cases, with two different answers.

**The next stone is refused** (any refusal in `stones.md` section 2): nothing changes. The stone is kept, and the
sigil stays pending. This is fixed, not a proposal - refusals never consume.

**The next stone is one the sigil does not steer** (Awakening with a Culling pending, Binding with anything pending,
Honing with anything pending): **the stone applies normally and the sigil stays pending** (adopted default,
`../DECISIONS.md` SIG-1).

- For: a sigil is an investment in the *next relevant* craft. Losing it to an unrelated Honing Stone would punish a
  player for doing their crafting in the wrong order, and the tooltip line makes the pending sigil hard to forget.
- Against: PLAN.md reads "consumed by the **next** stone used on that item". Under that reading any stone spends the
  sigil, steered or not, which makes sigils a sharper commitment ("apply it, then use the stone it is for").
- The rule is one switch in code either way; the YAML knob `sigils.consume_on_unsteered` (default `false`,
  `economy-yaml.md` section 5) lets an owner choose the other reading.

---

# 5. Other interactions

| Situation | Result |
| --- | --- |
| Unmaking with a sigil pending | Sigil stays pending (the item is still the same item) |
| Reflection with a sigil pending | Original keeps it; the copy has none |
| Serpent with a sigil pending | Refused, `$ecf_msg_sigil_would_strand` |
| Item with a pending sigil dropped, stored, traded, carried through a portal | Sigil travels with it |
| Pending sigil whose id was removed from the YAML or disabled | **Dormant**: shown greyed, never steers, and a newly applied sigil replaces it without refusal (SIG-8) |
| Equipped item | Sigils may be applied and consumed like any stone under the equipped-items setting |
| Essences | Preservation steers (the protected affix is kept beside the bound one and counts toward `kept + 1`); War, Warding, Fortune and Culling do not and stay pending - the essence already steers its roll (`essences.md` section 5, ESS-7) |
| Grinding a magic item (salvage) | Refused while a live sigil is pending, `$ecf_msg_salvage_sigil_pending` (SAL-7); a dormant one does not block (`salvage.md` section 3) |

There is **no way to remove a pending sigil** other than spending it. (Unmaking deliberately does not clear it;
SIG-3.)

---

# 6. Owner-defined sigils

A sigil is a stone entry with `verb: sigil` and a `steer` key:

| `steer` | Extra key | Behaves like |
| --- | --- | --- |
| `preserve` | - | Preservation |
| `category` | `category: offense \| defense \| utility` | War / Warding / Fortune |
| `cull` | - | Culling |

So "Sigil of the Mythic" (steer a Mythic-only draw) is not expressible without code - a new `steer` value would be
one. The `steer` values are listed with the stone schema in `economy-yaml.md` section 4.

---

# 7. Multiplayer

Nothing new: the pending sigil is item custom data, so it replicates with the item; steering happens on the client
that applies the stone, under synced rules. No RPC.

---

# Build checklist

- [ ] Sigil verb sets the pending sigil; one per item; `sigil_pending` refusal
- [ ] Tooltip line
- [ ] Preservation on Turmoil, Upheaval, Perfection with the protected-affix rule
- [ ] War / Warding / Fortune on every adding verb, first-eligible draw, `sigil_no_match`
- [ ] Culling on Severing and Turmoil, dormant first
- [ ] Not-steered stones leave the sigil pending (or the owner's choice)
- [ ] Serpent refused while pending; Reflection copy has none; Unmaking keeps it
- [ ] Dormant pending sigil replaced by a new one

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: open questions moved to `../DECISIONS.md`. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Sigils: SIG-1 to SIG-8).

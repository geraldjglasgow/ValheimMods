# EliteCrafting - specification: Affixes, the Mythic-only pool

The Mythic-only half of the affix catalog. Everything this file relies on - fields, value types, tier notation,
exclusion, requirements, the effect registry - is defined once in `affixes.md`; `../SPEC.md` is the whole-mod index,
and its questions are answered in `../DECISIONS.md` (AFX entries).

Numbers are defaults and judgement calls; display names are adopted defaults (`../DECISIONS.md` AFX-13). **Phase 3.**

**Status: specified, not built.** 13 affixes.

---

# 1. What is different about the pool

- Entries carry `mythic_only: true`. A Mythic item has six affixes, of which **one** is drawn from this pool and the
  rest from the regular pool (`rarity.md` section 5). Until Phase 3 builds these effects, or when none fits the
  item's slot, that sixth affix comes from the regular pool instead.
- The pool is **build-defining behaviours**: things that change how a character plays, not bigger numbers.
- Slot sanity applies exactly as in the regular pool (`affixes.md` section 2).
- **Tiers still apply.** A Mythic item keeps its tier ceiling, so a Meadows Mythic's Undying recharges slowly and an
  Ashlands Mythic's quickly. Flags gate at tier 1, so every Mythic can roll them.
- **Weights are relative inside this pool.** Default 100; the four Elementalist's Pacts are 25 each so together they
  weigh as one entry.
- Two behaviours the pool marked as Mythic candidates join here: building without a station (`master_builder`) and
  the extra jump in the air (`valkyrie_leap`).

# 2. Slot matrix (Mythic-only)

<div style="overflow-x:auto">

| affix | melee | ranged | magic | shield | head | chest | legs | cape | utility | tool | requires |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `bifrost_blessing` |  |  |  |  |  |  |  | x | x |  |  |
| `undying` |  |  |  |  | x | x |  |  |  |  |  |
| `thunderclap` |  |  |  | x |  |  |  |  |  |  | traits can_parry |
| `warbanner` | x | x | x |  |  |  |  |  |  |  |  |
| `aegis` |  |  |  | x |  | x |  |  |  |  |  |
| `blink` |  |  |  |  |  |  | x | x |  |  |  |
| `allfathers_bulwark` |  |  |  | x |  | x |  |  |  |  |  |
| `pact_fire` |  |  |  |  |  | x |  | x |  |  |  |
| `pact_frost` |  |  |  |  |  | x |  | x |  |  |  |
| `pact_lightning` |  |  |  |  |  | x |  | x |  |  |  |
| `pact_poison` |  |  |  |  |  | x |  | x |  |  |  |
| `master_builder` |  |  |  |  |  |  |  |  |  | x | traits builds |
| `valkyrie_leap` |  |  |  |  |  |  | x | x |  |  |  |

</div>

# 3. The catalog

- **`bifrost_blessing`** Bifrost Blessing — While worn, portals let you through with ores, metals and every other item the game forbids.  
  `portal_any_item` · flag · utility, cape · utility · **mythic_only** · no group · easy · P3 · w 100  
  gate T1 (rolls at T1–T7, no value)
- **`undying`** Undying — A killing blow leaves you at 1 health instead. Recharges in X minutes.  
  `undying` · flat (min) · chest, head · defense · **mythic_only** · no group · medium · P3 · w 100 · lower is better  
  T1–T7: 18–20 / 16–18 / 14–16 / 12–14 / 11–12 / 10–11 / 9–10
- **`thunderclap`** Thunderclap — A perfect block staggers every enemy within X m.  
  `parry_shockwave` · flat (m) · shield · defense · **mythic_only** · no group · medium · P3 · w 100  
  T1–T7: 3–4 / 3.5–4.5 / 4–5 / 4.5–5.5 / 5–6 / 5.5–6.5 / 6–7 · requires traits can_parry
- **`warbanner`** Warbanner — Party members within 20 m deal +X% damage (you included).  
  `party_aura` `damage_dealt` · percent · melee, ranged, magic · offense · **mythic_only** · group `party_aura` · medium · P3 · w 100  
  T1–T7: 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–9 / 9–10
- **`aegis`** Aegis — Party members within 20 m take X% less damage (you included).  
  `party_aura` `damage_taken` · percent · shield, chest · defense · **mythic_only** · group `party_aura` · medium · P3 · w 100  
  T1–T7: 3–4 / 4–5 / 5–6 / 6–7 / 7–8 / 8–9 / 9–10
- **`blink`** Blink — Your dodge roll becomes a short teleport in the dodge direction.  
  `blink_dodge` · flag · legs, cape · utility · **mythic_only** · no group · hard · P3 · w 100  
  gate T1 (rolls at T1–T7, no value)
- **`allfathers_bulwark`** Allfather's Bulwark — No single hit can take more than X% of your maximum health.  
  `hit_cap` · percent · chest, shield · defense · **mythic_only** · no group · medium · P3 · w 100 · lower is better  
  T1–T7: 55–60 / 50–55 / 45–50 / 42–45 / 38–42 / 35–38 / 30–35
- **`pact_fire`** Elementalist's Pact: Fire — Fire damage no longer harms you; X% of it heals you instead.  
  `element_absorb` `fire` · percent · cape, chest · defense · **mythic_only** · group `element_pact` · medium · P3 · w 25  
  T1–T7: 20–25 / 25–30 / 30–35 / 35–40 / 40–45 / 45–50 / 50–60
- **`pact_frost`** Elementalist's Pact: Frost — Frost damage no longer harms you; X% of it heals you instead.  
  `element_absorb` `frost` · percent · cape, chest · defense · **mythic_only** · group `element_pact` · medium · P3 · w 25  
  T1–T7: 20–25 / 25–30 / 30–35 / 35–40 / 40–45 / 45–50 / 50–60
- **`pact_lightning`** Elementalist's Pact: Lightning — Lightning damage no longer harms you; X% of it heals you instead.  
  `element_absorb` `lightning` · percent · cape, chest · defense · **mythic_only** · group `element_pact` · medium · P3 · w 25  
  T1–T7: 20–25 / 25–30 / 30–35 / 35–40 / 40–45 / 45–50 / 50–60
- **`pact_poison`** Elementalist's Pact: Poison — Poison damage no longer harms you; X% of it heals you instead.  
  `element_absorb` `poison` · percent · cape, chest · defense · **mythic_only** · group `element_pact` · medium · P3 · w 25  
  T1–T7: 20–25 / 25–30 / 30–35 / 35–40 / 40–45 / 45–50 / 50–60
- **`master_builder`** Master Builder — While this tool is in hand you can build without a crafting station nearby.  
  `stationless_build` · flag · tool · utility · **mythic_only** · no group · easy · P3 · w 100  
  gate T1 (rolls at T1–T7, no value) · requires traits builds
- **`valkyrie_leap`** Valkyrie's Leap — You can jump once more while in the air.  
  `extra_jump` · flag · legs, cape · utility · **mythic_only** · no group · medium · P3 · w 100  
  gate T1 (rolls at T1–T7, no value)

# 4. Notes per entry

- **`undying`** and **`allfathers_bulwark`** are better when lower (recharge minutes, the share of max health one
  hit may take). Tiers still run 1 to 7 in increasing strength.
- **`warbanner`** and **`aegis`** need to know who is nearby and in your party. Each client applies the aura to its
  own player from a small synced flag on the wearer (other clients never see your items' custom data,
  `item-data.md`). Pairing with our Party mod is the plan (PLAN.md). Behaviour without it is **not decided**:
  `../DECISIONS.md` AFX-8, BLOCKING for Phase 3.
- **`pact_<element>`**: full immunity plus 20-60% healing, kept even in the Ashlands (`../DECISIONS.md` AFX-9).
- **`bifrost_blessing`** lets the wearer carry anything through a portal; the portal check is on the traveller's own
  client, so it works on a dedicated server with no extra netcode.
- **`thunderclap`** routes a stagger to each nearby enemy's owner, like any other hit.
- **`blink`** is the one hard hook here (safe placement at the end of the blink).

# Build checklist

- [ ] Phase 3: the 13 effects registered and their affixes added to the built-in defaults
- [ ] AFX-8 decided before `party_aura` is built

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |

# Item classes, item levels and tier ladders

User decisions 2026-10-05, designed on the planning page https://claude.ai/artifact/VmVJDaDhHFrCP6dhKt1LoM (its database
holds the user's later edits: `pools/<inscription>`, `moves/<prefab>`, `cats/<class>`, `tiers/<inscription>`). This spec
replaces the slot taxonomy (`ItemSlot`, affixes.md section 2), the seven-tier window (item-tier.md section 6, rarity.md
section 4) and the `slots` / `tier_window` / `slot_weights` YAML keys. Where it disagrees with an older spec, this one wins.

## 1. Item classes

An **item class** is what an item is for inscriptions: each inscription lists the classes it rolls on, and gear drops pick
bases by class. Classes are data, not an enum: the built-in defaults are the `classes:` section of
`EliteCrafting_economy.yml` (33 classes), YAML files can add or change them, and other mods add classes and claim items
through the API (api.md).

Fields: `id` (snake_case, unique), `group` (display grouping: onehand, twohand, ranged, magic, offhand, armour, jewel,
tools, none), `name` (`$ecf_class_<id>` by default, English in `translations/English.classes.yml`), `rolls` (default
true; false = items of the class never become magic), `damage_scale` (default 1; multiplies `scaled` inscriptions when
they roll), `drop_weight` (default 1; gear drops), `match` (a list of rules, any of them), `items` (explicit prefabs).

**Classification** of an item (cached per `SharedData`, dropped on rules change and registry change):

1. A class whose `items` lists the item's prefab (first such class in order).
2. A class claimed for the item by an API classifier callback (registration order; the first non-null answer wins).
3. The first class, in file order, with a `match` rule the item meets. A rule meets when every key it has holds:
   `types` (the item type is one of these `ItemDrop.ItemData.ItemType` names), `skills` (`Skills.SkillType` names),
   `two_handed` (item type TwoHandedWeapon or TwoHandedWeaponLeft), `stackable` (max stack above 1), `prefixes` (the prefab
   name starts with one), `name_contains` (the prefab name contains one).
4. Otherwise none: never magic.

PackPanel's backpacks (shared name prefix `$packpanel_backpack_`) never meet a `match` rule (steps 1 and 2 only), and a
rune is never classified. An item is a **magic base** when it has a class with `rolls: true`, a max stack of 1 and is not
a rune.

`ItemTraits`, `Hands` and the governing skills stay (they serve `requires`); they are computed beside the class.

## 2. Item level

The item level is the biome of the item's materials, 1 Meadows to **8 Deep North** (was 1-7). Same derivation as
item-tier.md (explicit `item_tiers.items`, else the highest material tier of its recipe, else the station, else
`fallback_tier`), clamped 1-8, with the Deep North materials (`Gold`, `Frostwood`, `MooseHide`, ...) at 8 and
`biomes.deep_north: 8`. Every economy list indexed by tier takes eight entries; a list of seven still loads, its last
value repeated for tier 8. ECR's world-tier lists are unrelated and unchanged.

## 3. Inscriptions (format 2)

`EliteCrafting_inscriptions.yml` starts with `format: 2`. New and changed fields per inscription:

- `affix: prefix | suffix` (required). Prefixes are the item's core power (damage, health, armour, block, leech);
  suffixes everything else.
- `family` (snake_case, display grouping) and `category` (offense, defense, utility; as before).
- `classes: { best: [...], allowed: [...] }` replaces `slots`. Unknown class ids: a warning, the id ignored. An
  inscription with neither list never rolls.
- `scaled: true`: the rolled value is multiplied by the item class's `damage_scale` and rounded to the tier's
  decimals, at roll time (what is stored is final).
- `tiers` in one of two forms:
  - a **ladder** map `{ count, from, min, max }` (flags: `{ count, from }`), generated as in section 4;
  - explicit **rows** `[ { tier, level, min, max, weight } ]`: `tier` counts down (1 strongest), `level` is the item
    level that unlocks it, `weight` defaults to the tier number.
- Removed: `slots`. A user file still carrying it is not format 2 (section 7).

Tier count per inscription is free (1-16); the tooltip shows `T<n>` counted down within that inscription's ladder.

## 4. Ladder generation (must match the planning page exactly)

For `count = k`, `from = f` (1-8):

- **Unlock levels**, weakest tier first (index j = 0 is T`k`). Let `n = 9 - f`.
  - If `k <= n`: level_j = round(f + j * (8 - f) / (k - 1)) (k = 1: level = f).
  - Else if `n <= 1`: every tier at 8.
  - Else: T`k` at f; the remaining k - 1 tiers spread over levels f+1 .. 8: `lv = n - 1`, `base = (k-1) / lv`,
    `rem = (k-1) % lv`; level f+1+i gets `base + (i < rem ? 1 : 0)` tiers, in order.
  - Rounding is half away from zero (JavaScript `Math.round` rounds .5 up; for positive values that is the same).
- **Values** (valued ladders only): `span = max - min`, `step = span / k`.
  - Whole-number flat ladders (`value: flat`, no unit, `min` and `max` integers) with `span < 2k`: each tier is one
    integer, T at index j = round(min + span * j / (k - 1)) (k = 1: max).
  - Otherwise decimals `d` = 0 for whole-number flats, else max(step >= 1 ? 0 : step >= 0.1 ? 1 : 2, decimals written in
    min, decimals written in max), at most 2. Boundaries b_j = round_d(min + span * (j / k)^1.25), j = 0..k. Tier at
    index j: low = (j == 0 ? b_0 : b_j + 10^-d), high = b_(j+1); if low > high then low = high.
- **Weights**: a tier's weight is its tier number (T13 weight 13, T1 weight 1).

Example, `{ count: 13, from: 1, min: 3, max: 40 }` (vigor): T13 3-4 @1, T12 5-7 @2, T11 8-9 @2, T10 10-11 @3, T9 12-14
@3, T8 15-17 @4, T7 18-20 @4, T6 21-23 @5, T5 24-26 @5, T4 27-30 @6, T3 31-33 @6, T2 34-36 @7, T1 37-40 @8.

## 5. Rolling

Inputs: the item's class and level, the rarity, the rune's floor, chaotic or not.

- **Candidates**: enabled, weight above 0, not on the item (dormant copies included), no shared exclusion group, the
  item's class in `best` or `allowed`, `requires` met, the item's prefix or suffix count below the rarity's limit for
  the inscription's `affix` (section 6), and at least one eligible tier.
- **Eligible tiers** (ordinary roll): rows with `level <= item level`, weight above 0, and, where the class is only
  `allowed`, `tier >= 1 + floor(k * rolling.allowed_closed_fraction)` (default 0.334: 13 tiers stop at T5, 8 at T3,
  4 and 3 at T2, 2 and 1 not closed).
- **Draw**: inscription by `weight`, then tier by tier weight, then value uniformly in [min, max] rounded to the tier's
  decimals; `scaled` multiplies by the class's damage_scale and rounds again; flags store 1.
- **Rune floor** (`tier_floor`, unused by the defaults): counted from the best eligible tier, 1 = only the best.
- **Chaotic** (Serpent's reroll): every tier the inscription defines, uniformly, ignoring level, the allowed cap and
  the floor; the class pool still applies.
- `rolling.tier_window` is gone (ignored with a warning when a user file has it).

## 6. Rarity limits

Rarities gain `prefixes` and `suffixes` (economy file): Magic 1 and 1, Rare 3 and 3, Normal 0. Every roll respects them:
fresh rolls, Shaping and Consecrated, promotions, Recasting (a fresh roll at the item's rarity since 2026-10-05). The Serpent's "one past the cap" outcome may also pass the prefix or
suffix limit by its `overflow`. Counts (`inscriptions: { min, max }`) work as before, so a Rare with 3+3 limits and max 6
is full at 6.

## 7. Stored items and config formats

- **Item data**: a stored roll keeps its *grade* (strength from the weakest, 1 = T`k`); the tooltip shows
  `T(k + 1 - grade)` with that inscription's own k. `ItemKeys.CurrentFormat` becomes 2. Format-1 items (grades 1-7 over
  seven tiers) migrate on read: grade' = clamp(round(grade * k / 7), 1, k) for every roll whose id is defined; values are
  kept. Ids are unchanged: every old inscription id is still in the catalog (the brands are now flat added damage).
- **Config files**: both YAML families carry `format: 2`. On load, a file on disk without `format: 2` (a format-1 copy
  written by an older version) is renamed to `<name>.v1.bak` and the new default written in its place, with one
  warning in the log; extra `_<anything>.yml` files without it are skipped with a warning (not renamed). The server's
  synced texts follow the same rule.

## 8. Caps

`caps:` keys apply per channel: `effect` caps every channel of that effect (each `effect:param` separately), `effect:param`
one channel. The defaults follow the user's limits: resistance (`damage_taken`) 50 per damage type, `move_speed` and
`attack_speed` 15, `avoid_hit` 15, `leech` 5, stamina, eitr and health costs 30, cooldowns 30, `crit_chance` 25.

## 9. Display and commands

- Tooltip: prefixes first, then suffixes; each line keeps `T<n>` (shown per its ladder). Full detail adds the item's
  class and level (`Swords · item level 4`).
- `ecraft dump items` writes class and level columns; `ecraft classes` lists every class with its item count, level
  range and pool size; `ecraft roll` takes a class id where it took a slot; `ecraft affix <id>` shows the tier rows with
  their unlock levels.

## 10. Gear drops

Bases are items of classes with `rolls: true` (and the existing recipe / exclude / include rules), weighted by the
class's `drop_weight` (replaces `gear.slot_weights`), at drop tiers 1-8.

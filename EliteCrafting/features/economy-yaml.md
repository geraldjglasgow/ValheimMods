# EliteCrafting - specification: Economy YAML

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file is the **field-by-field reference** for the `EliteCrafting_economy*.yml` family: rarities, rolling rules,
stones (with the Serpent outcomes and Chance weights inside their stone entries), sigil knobs, item-tier maps, biome
tiers and drop tables. What the fields *do* is in the feature files each section names; this file is the shape.
The affix definitions live in the other family, `EliteCrafting_affixes*.yml` (`affixes.md`).

This is the **canonical economy schema**: the conventions' baseline field names plus every field the economy
features need. The affix schema is `configuration.md` section 6.

**Status: Phase 1 built, not tested in game** (2026-09-23). Ships in **Phase 1** with every section; the Phase 2 sections
and fields are read but inert until their feature is built (`drops.chests` is live since 0.2.0, except
`chests.containers`, which awaits the reader).

---

# 1. The file family

- **Main file** `EliteCrafting_economy.yml`, written on first run as a full, commented copy of the defaults below.
  Additional files `EliteCrafting_economy_<anything>.yml` in the config folder are read after it, in file-name order.
  Layering, `use_defaults` and the built-in default layer are `configuration.md` section 3.
- **Merge rule** (`configuration.md` section 3 owns it): list entries with an `id` (`rarities`, `stones`) merge
  **by id, field by field** - a later file names only the fields it changes; list-valued fields (`applies_to`,
  `slots`, `outcomes`) are replaced whole; map-valued fields (`cost`, `weights`) merge by key. For the sections this
  file adds, which are maps rather than id lists, the same idea applies: **maps merge by key** (`item_tiers.items`,
  `item_tiers.materials`, `item_tiers.stations`, `biomes`, `drops.stones`, `drops.rarity_weights`,
  `drops.boss_rarity_weights`, `drops.gear.slot_weights`, `drops.gear.include`), **tier-indexed lists are replaced
  whole** (a stone's seven weights), **boss and creature entries merge field by field** with their `bonus` list
  replaced whole, and scalars take the last value read (`../DECISIONS.md` ECO-1). So a server owner keeps the
  shipped defaults and writes only their changes in `EliteCrafting_economy_server.yml`.
- **Synced, lockable, hot-reloaded** by the mod's own rule plumbing (`configuration.md` sections 4-5): the server's
  files bind every player while locked; edits are picked up within five seconds or by `ecraft reload`. Errors reject
  the whole family (the previous configuration stays); warnings apply.
- **Percent convention:** every chance is a percent number (`4` means 4%), as in Elite Creatures Reborn's rule files
  (ECO-2). Weights are relative, any non-negative number.
- **Tier-indexed lists** have exactly seven entries, tier 1 (meadows) first (ECO-3). A list of another length is an
  error.

Top-level sections:

| Section | Feature file | Phase |
| --- | --- | --- |
| `rarities` | `rarity.md` | 1 |
| `rolling` | `rarity.md` section 4 | 1 |
| `stones` | `stones.md`, `sigils.md`, `quality.md` | 1 (Phase 2 entries inert until built) |
| `sigils` | `sigils.md` | 2 |
| `item_tiers` | `item-tier.md` | 1 |
| `biomes` | `drops.md` section 3 | 1 |
| `drops` | `drops.md` | 1 (chests Phase 2, built) |

---

# 2. `rarities`

A list, **in ladder order** (lowest first).

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `id` | id | yes | - | `common` ... `mythic`; referenced everywhere else |
| `name` | loc key | no | `$ecf_rarity_<id>` | display name |
| `color` | `#RRGGBB` | yes | - | the single source for every rarity-colored surface |
| `glow` | bool | no | true (false on the first entry) | ground glow; ignored (warning) on the base rarity |
| `affixes` | `{min, max}` | yes | - | affix count range; first entry must be `{0, 0}` |
| `mythic_affixes` | int | no | 0 | how many of the count come from the `mythic_only` pool; at most one rarity may set it |
| `drop_weight` | number | no | 1 | multiplier on this rarity's weight in every drop table; **0 blocks world drops entirely** |

```yaml
rarities:
  - id: common
    color: "#FFFFFF"
    glow: false
    affixes: { min: 0, max: 0 }
    drop_weight: 0          # Common is never a "magic drop"; vanilla items stay vanilla
  - id: uncommon
    color: "#1EFF00"
    affixes: { min: 1, max: 2 }
  - id: rare
    color: "#0070DD"
    affixes: { min: 2, max: 3 }
  - id: epic
    color: "#A335EE"
    affixes: { min: 3, max: 4 }
  - id: legendary
    color: "#FF8000"
    affixes: { min: 4, max: 5 }
  - id: mythic
    color: "#E6262E"
    affixes: { min: 6, max: 6 }
    mythic_affixes: 1
    drop_weight: 0          # craft-only (user decision); raise it AND give Mythic drop weights to open drops
```

---

# 3. `rolling`

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `tier_window` | int 1-7 | 3 | an item rolls tiers `ceiling - window + 1` to `ceiling` |
| `promote_adds_at_least` | int | 1 | a promotion always adds at least this many affixes (0 = PLAN.md's literal reading) |
| `count_weights` | map rarity -> `{count: weight}` | none (uniform) | bias fresh rolls' affix count |

```yaml
rolling:
  tier_window: 3
  promote_adds_at_least: 1
  # count_weights:
  #   legendary: { 4: 3, 5: 1 }
```

---

# 4. `stones`

A list. Common fields for every stone:

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `id` | id | yes | - | one of the 43 built-in ids (27 stones and sigils, 16 essences), or an owner's own |
| `name` | loc key or literal text | no | `$ecf_stone_<id>` | the item name (`localization.md` section 4); literal text shows as written in every language |
| `description` | loc key or literal text | no | `$ecf_stone_<id>_desc` | the item description in the vanilla tooltip |
| `prefab` | name | shipped stones: fixed | `ECF_` + PascalCase id | the code-registered prefab this entry defines. A built-in stone must name its own prefab; an owner-defined stone must name one of the reserved `ECF_Custom01`-`ECF_Custom16` (`prefabs.md` section 5); two entries may not share one |
| `verb` | enum | yes | - | `promote`, `add`, `swap`, `reroll_affixes`, `reroll_values`, `remove`, `strip`, `corrupt`, `lock`, `duplicate`, `gamble`, `quality`, `sigil`, `imbue` |
| `grade` | `lesser` / `greater` / none | no | none | informational: display, sorting, the `ecraft` listing |
| `applies_to` | rarity ids | yes | - | rarities the stone accepts |
| `cost` | map rarity -> int | no | 1 for every rarity | stones consumed per use, from the held stack |
| `tier_floor` | int 1-7 | no | none | lowest affix tier this stone rolls (clamped to the ceiling); for `imbue`, of the guaranteed affix only (`essences.md` section 3) |
| `enabled` | bool | no | true | false: never drops, refuses with `stone_disabled`; the prefab still exists (prefabs come from code) |
| `confirm` | bool | no | false | requires the client's confirm gate (Serpent, Unmaking) |
| `stack` | int | no | 50 | max stack size of the stone item |
| `item_weight` | number | no | 0.2 | item weight of one stone. Never called plain `weight` on a stone, so it cannot be confused with drop or gamble weights |
| `tint` | `#RRGGBB` | no | the stone's default tint (`prefabs.md` section 4) | world-model and icon tint; ascension stones default to the rarity they produce |

Verb-specific fields:

| Verb | Field | Type | Default | Meaning |
| --- | --- | --- | --- | --- |
| `corrupt` | `outcomes` | list of `{outcome, weight}` | Serpent table | `outcome` in `seal_only`, `add_affix`, `chaotic_reroll`, `promote`, `demote` |
| `corrupt` | `overflow` | int | 1 | how far `add_affix` may exceed the rarity maximum |
| `gamble` | `weights` | map rarity -> weight | Chance table | a `common` weight is a fizzle |
| `duplicate` | `seal_copy` | bool | true | whether the copy is sealed |
| `lock` | `max_bound` | int | 1 | bound affixes allowed at a time |
| `quality` | `slots` | slot ids | - | where the stone works |
| `quality` | `step` | int (percent points) | 1 | per use |
| `quality` | `cap` | int (percent) | 10 | maximum |
| `sigil` | `steer` | `preserve` / `category` / `cull` | - | what the sigil steers |
| `sigil` | `category` | `offense` / `defense` / `utility` | - | with `steer: category` |
| `imbue` | `family` | family id | - (required) | the `essence_families` entry (section 10) the guaranteed affix is drawn from |

`slots` is also accepted on any other verb, as a filter (`wrong_item_type` outside it).

## Complete default list

Final defaults. **The Phase 1 release ships every Phase 2 entry with `enabled: false`** (marked `# P2`); their
prefabs are registered anyway.

```yaml
stones:
  # --- ascension (Phase 1) ---
  - { id: awakening,     verb: promote, applies_to: [common] }
  - { id: ascension,     verb: promote, applies_to: [uncommon] }
  - { id: exaltation,    verb: promote, applies_to: [rare] }
  - { id: transcendence, verb: promote, applies_to: [epic] }
  - { id: apotheosis,    verb: promote, applies_to: [legendary] }

  # --- manipulation ---
  - { id: growth_lesser,      verb: add,            grade: lesser,  applies_to: [uncommon, rare] }
  - { id: growth_greater,     verb: add,            grade: greater, applies_to: [epic, legendary, mythic] }        # P2
  - { id: turmoil_lesser,     verb: swap,           grade: lesser,  applies_to: [uncommon, rare] }
  - { id: turmoil_greater,    verb: swap,           grade: greater, applies_to: [epic, legendary, mythic] }        # P2
  - { id: upheaval_lesser,    verb: reroll_affixes, grade: lesser,  applies_to: [uncommon, rare] }                 # P2
  - { id: upheaval_greater,   verb: reroll_affixes, grade: greater, applies_to: [epic, legendary, mythic] }        # P2
  - { id: perfection_lesser,  verb: reroll_values,  grade: lesser,  applies_to: [uncommon, rare] }
  - { id: perfection_greater, verb: reroll_values,  grade: greater, applies_to: [epic, legendary, mythic] }        # P2
  - { id: severing_lesser,    verb: remove,         grade: lesser,  applies_to: [uncommon, rare] }                 # P2
  - { id: severing_greater,   verb: remove,         grade: greater, applies_to: [epic, legendary, mythic] }        # P2
  - id: unmaking                                                                                                  # P2
    verb: strip
    applies_to: [uncommon, rare, epic, legendary, mythic]
    confirm: true

  # --- risk and endgame (Phase 2) ---
  - id: serpent                                                                                                   # P2
    verb: corrupt
    applies_to: [uncommon, rare, epic, legendary, mythic]
    confirm: true
    overflow: 1
    outcomes:
      - { outcome: seal_only,      weight: 25 }
      - { outcome: add_affix,      weight: 25 }
      - { outcome: chaotic_reroll, weight: 20 }
      - { outcome: promote,        weight: 15 }
      - { outcome: demote,         weight: 15 }
  - id: binding                                                                                                   # P2
    verb: lock
    applies_to: [uncommon, rare, epic, legendary, mythic]
    max_bound: 1
  - id: chance                                                                                                    # P2
    verb: gamble
    applies_to: [common]
    weights: { common: 0, uncommon: 50, rare: 30, epic: 15, legendary: 5, mythic: 0 }
  - id: reflection                                                                                                # P2
    verb: duplicate
    applies_to: [uncommon, rare, epic, legendary, mythic]
    seal_copy: true

  # --- quality (Phase 2) ---
  - id: honing                                                                                                    # P2
    verb: quality
    applies_to: [common, uncommon, rare, epic, legendary, mythic]
    slots: [melee_weapon, ranged_weapon, magic_weapon]
    step: 1
    cap: 10
  - id: tempering                                                                                                 # P2
    verb: quality
    applies_to: [common, uncommon, rare, epic, legendary, mythic]
    slots: [head, chest, legs, cape, shield]
    step: 1
    cap: 10

  # --- sigils (Phase 2) ---
  - { id: sigil_preservation, verb: sigil, steer: preserve, applies_to: [common, uncommon, rare, epic, legendary, mythic] }  # P2
  - { id: sigil_war,     verb: sigil, steer: category, category: offense, applies_to: [common, uncommon, rare, epic, legendary, mythic] }  # P2
  - { id: sigil_warding, verb: sigil, steer: category, category: defense, applies_to: [common, uncommon, rare, epic, legendary, mythic] }  # P2
  - { id: sigil_fortune, verb: sigil, steer: category, category: utility, applies_to: [common, uncommon, rare, epic, legendary, mythic] }  # P2
  - { id: sigil_culling, verb: sigil, steer: cull, applies_to: [common, uncommon, rare, epic, legendary, mythic] }  # P2

  # --- essences (Phase 2, essences.md section 11): one pair per family ---
  - { id: essence_venom_lesser,  verb: imbue, family: venom, grade: lesser,  applies_to: [uncommon, rare] }
  - { id: essence_venom_greater, verb: imbue, family: venom, grade: greater, applies_to: [uncommon, rare, epic, legendary, mythic], tier_floor: 7 }
  # ... storm, grove, frost, battle, seidr, ember, tide likewise (16 entries)
```

Every entry above takes the common defaults: `name: $ecf_stone_<id>`, `description: $ecf_stone_<id>_desc`,
`prefab: ECF_<PascalCase id>`, `cost` 1 everywhere, no `tier_floor`, `enabled: true`, `confirm: false` unless set,
`stack: 50`, `item_weight: 0.2`, the default tint. Written out in full, one entry looks like:

```yaml
  - id: growth_lesser
    name: $ecf_stone_growth_lesser
    description: $ecf_stone_growth_lesser_desc
    prefab: ECF_GrowthLesser
    verb: add
    grade: lesser
    applies_to: [uncommon, rare]
    cost: { uncommon: 1, rare: 1 }
    # tier_floor: 4          # e.g. an owner's "deep growth" variant
    enabled: true
    confirm: false
    stack: 50
    item_weight: 0.2
    tint: "#2EC4B6"
```

An owner-defined stone, bound to a reserved prefab (`stones.md` section 21):

```yaml
  - id: deep_growth
    name: "Stone of Deep Growth"
    description: "Adds one affix of at least tier 6."
    prefab: ECF_Custom06
    verb: add
    applies_to: [epic, legendary]
    tier_floor: 6
    tint: "#0B6E4F"
```

---

# 5. `sigils`

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `consume_on_unsteered` | bool | false | true: any successful stone consumes a pending sigil, steered or not (`sigils.md` section 4) |

```yaml
sigils:
  consume_on_unsteered: false
```

---

# 6. `item_tiers` (the item tier model, `item-tier.md`)

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `items` | map item prefab -> tier | the no-recipe list | explicit override, wins over everything |
| `materials` | map material prefab -> tier | the material map | recipe-material rule |
| `material_depth` | int | 3 | recursion depth for crafted intermediate materials |
| `stations` | map station prefab -> tier | small map | fallback when no material resolves |
| `station_levels` | map station -> `{level: tier}` | none | optional per-level refinement |
| `fallback_tier` | int 1-7 | 1 | last resort |

```yaml
item_tiers:
  fallback_tier: 1
  material_depth: 3

  items:                       # no-recipe vanilla items (verify names in game)
    BeltStrength: 3
    HelmetDverger: 3
    FishingRod: 1
    Wishbone: 3

  materials:
    # 1 meadows
    Wood: 1
    Stone: 1
    Flint: 1
    Resin: 1
    LeatherScraps: 1
    DeerHide: 1
    Feathers: 1
    HardAntler: 1
    # 2 black_forest
    RoundLog: 2
    FineWood: 2
    Copper: 2
    CopperOre: 2
    CopperScrap: 2             # verify
    Tin: 2
    TinOre: 2
    Bronze: 2
    BronzeNails: 2
    TrollHide: 2
    GreydwarfEye: 2
    SurtlingCore: 2
    BoneFragments: 2
    AncientSeed: 2
    Thistle: 2
    Coal: 2
    CryptKey: 2                # verify
    # 3 swamp
    Iron: 3
    IronScrap: 3
    IronOre: 3                 # verify
    IronNails: 3
    Chain: 3
    ElderBark: 3
    Guck: 3
    Ooze: 3
    Bloodbag: 3
    Entrails: 3
    WitheredBone: 3
    Root: 3
    Wishbone: 3
    # 4 mountain (and ocean)
    Silver: 4
    SilverOre: 4
    WolfPelt: 4
    WolfFang: 4
    WolfClaw: 4                # verify
    Obsidian: 4
    FreezeGland: 4
    DragonTear: 4
    Crystal: 4
    JuteRed: 4                 # verify
    YmirRemains: 4             # verify
    SerpentScale: 4            # ocean, verify
    Chitin: 4                  # ocean, verify
    # 5 plains
    BlackMetal: 5
    BlackMetalScrap: 5
    LinenThread: 5
    Flax: 5
    Needle: 5
    LoxPelt: 5
    Tar: 5
    YagluthDrop: 5             # verify
    # 6 mistlands
    Eitr: 6
    Sap: 6
    Softtissue: 6
    BlackCore: 6
    Carapace: 6
    ScaleHide: 6
    Mandible: 6
    BlackMarble: 6
    YggdrasilWood: 6
    JuteBlue: 6
    Wisp: 6
    Bilebag: 6
    RoyalJelly: 6
    QueenDrop: 6
    Iolite: 6                  # verify
    DvergrNeedle: 6            # verify
    DvergrKeyFragment: 6       # verify
    # 7 ashlands
    FlametalNew: 7
    FlametalOreNew: 7
    Flametal: 7                # legacy name, verify
    FlametalOre: 7             # legacy name, verify
    CharredBone: 7
    AskHide: 7
    AskBladder: 7
    CelestialFeather: 7
    MoltenCore: 7
    GemstoneRed: 7
    GemstoneBlue: 7
    GemstoneGreen: 7
    Grausten: 7
    Blackwood: 7
    CharcoalResin: 7
    CharredCogwheel: 7
    MorgenSinew: 7
    MorgenHeart: 7
    ProustitePowder: 7
    SulfurStone: 7
    BellFragment: 7
    FaderDrop: 7               # verify

  stations:                    # fallback only; all names verify in game
    piece_workbench: 1
    forge: 2
    piece_artisanstation: 5
    blackforge: 6
    piece_magetable: 6
  # station_levels:
  #   forge: { 1: 2, 3: 3, 5: 4, 7: 5 }
```

---

# 7. `biomes`

Biome id -> tier. Used for the death-position fallback and chest tiers.

```yaml
biomes:
  meadows: 1
  black_forest: 2
  swamp: 3
  mountain: 4
  plains: 5
  mistlands: 6
  ashlands: 7
  ocean: 4          # judgement call
  deep_north: 7     # reserved; clamped until tier 8 exists
```

---

# 8. `drops`

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `tamed` | bool | false | tamed creatures drop our loot |
| `require_player` | bool | true | a player or a player's tamed creature must have hit it |
| `max_stones_per_kill` | int | 5 | cap on stones from one non-boss kill |
| `max_gear_per_kill` | int | 2 | cap on gear from one non-boss kill |
| `star_multipliers` | list | `[1, 2, 3]` | by stars (vanilla level - 1); index 0 = no stars |
| `star_step` | number | 1 | added per star beyond the list |
| `chances.stone` | 7 percents | `[4, 5, 6, 7, 8, 9, 10]` | base stone chance per tier |
| `chances.gear` | 7 percents | `[1, 1.25, 1.5, 1.75, 2, 2.25, 2.5]` | base gear chance per tier |
| `stones` | map stone id -> 7 weights | table below | stone draw per tier; missing id = never drops |
| `rarity_weights` | map rarity id -> 7 weights | table below | gear rarity draw per tier |
| `boss_rarity_weights` | map rarity id -> 7 weights | table below | the same, for boss gear |
| `gear.require_recipe` | bool | true | exclude bases with no recipe |
| `gear.tiers_below` | int | 1 | bases this many tiers below the drop tier are eligible |
| `gear.same_tier_weight` | number | 3 | weight of a base at the drop tier |
| `gear.lower_tier_weight` | number | 1 | weight of a base below it |
| `gear.slot_weights` | map slot -> number | all 1, tool 0.5 | multiplies base weights |
| `gear.exclude` | item prefabs | `[]` | never drop |
| `gear.include` | map item prefab -> tier | `{}` | always in the pool at that tier |
| `bosses` | map prefab -> boss entry | table below | boss guarantees |
| `creatures` | map prefab -> creature entry | `{}` | per-creature overrides |
| `chests.stone_chance` | percent | 30 | stone chance per world container (Phase 2, drops.md section 11) |
| `chests.gear_chance` | percent | 10 | gear chance per world container |
| `chests.containers` | map container prefab -> container entry | `{}` | per-container overrides (parsing pending, see below) |
| `ecr.star_multipliers` | list, index = Elite Creatures Reborn stars | `[1, 1, 1.5, 2, 2.5, 3]` | replaces `star_multipliers` for ECR-resolved creatures while the `.cfg` `Synergy` is on (`ecr-integration.md` 4) |
| `ecr.star_step` | number | 0.5 | added per ECR star beyond the list |
| `ecr.star_rarity_bonus` | percent per ECR star | 0 | gear rarity shift above Uncommon, summed with Norns' Favour |
| `ecr.tier_stone_multipliers` | list, index = ECR world tier 0-7 | `[1, 1.1, ... 1.7]` | stone chance factor; inert until ECR records a tier (`ecr-integration.md` 5, DECISIONS ECR-6) |
| `ecr.tier_rarity_bonus` | list of percents, index = ECR world tier | `[0, 5, ... 35]` | gear rarity shift; inert likewise |
| `ecr.skip_worthless` | bool | true | ECR's Cloven twin and Phantom husks drop nothing from us, whenever ECR is installed (`ecr-integration.md` 6) |

Boss entry: `tier`, `stone_rolls`, `gear_rolls`, `bonus: [{stone, chance, amount}]`.
Creature entry: `tier`, `multiplier`, `stone_multiplier`, `gear_multiplier`, `bonus` (same shape).
Container entry (`chests.containers`, Phase 2, `../DECISIONS.md` IMP-79): the creature entry's shape - `tier` (overrides
the biome at the container), `multiplier` (0 = the container never rolls), `stone_multiplier`, `gear_multiplier`,
`bonus` - merged by key and field by field like `creatures`. Chests share the stone and rarity tables and the
`max_stones_per_kill` / `max_gear_per_kill` caps with creatures; they have no stars. **The rules reader does not parse
`containers` yet** (spine request); until it does, the key is an unknown-key warning and every container uses its
biome's tier and the flat chances.

```yaml
drops:                    # on/off: the .cfg switches `Stone drops` and `Magic item drops` (configuration.md)
  tamed: false
  require_player: true
  max_stones_per_kill: 5
  max_gear_per_kill: 2
  star_multipliers: [1, 2, 3]
  star_step: 1

  chances:
    stone: [4, 5, 6, 7, 8, 9, 10]
    gear:  [1, 1.25, 1.5, 1.75, 2, 2.25, 2.5]

  stones:                 # T1   T2   T3   T4   T5   T6    T7
    awakening:          [400, 300, 220, 160, 120, 100,   80]
    ascension:          [  0, 120, 120, 110, 100,  90,   80]
    exaltation:         [  0,   0,   0,  60,  70,  70,   70]
    transcendence:      [  0,   0,   0,   0,   0,  30,   40]
    apotheosis:         [  0,   0,   0,   0,   0,   0,    1]
    growth_lesser:      [  0,  80,  90,  90,  80,  70,   60]
    growth_greater:     [  0,   0,   0,  20,  30,  40,   50]
    turmoil_lesser:     [150, 150, 140, 120, 100,  90,   80]
    turmoil_greater:    [  0,   0,   0,  30,  40,  50,   60]
    upheaval_lesser:    [  0,  40,  50,  50,  50,  45,   40]
    upheaval_greater:   [  0,   0,   0,  15,  20,  25,   30]
    perfection_lesser:  [  0,   0,  30,  40,  40,  40,   40]
    perfection_greater: [  0,   0,   0,   0,  10,  15,   20]
    severing_lesser:    [  0,  50,  60,  60,  55,  50,   45]
    severing_greater:   [  0,   0,   0,  15,  20,  25,   30]
    unmaking:           [ 30,  30,  30,  30,  30,  30,   30]
    serpent:            [  0,   0,  20,  25,  30,  35,   40]
    binding:            [  0,   0,   0,   0,   5,   8,   10]
    chance:             [ 80,  80,  70,  60,  50,  40,   30]
    reflection:         [  0,   0,   0,   0,   0, 0.02, 0.05]
    honing:             [ 60,  60,  60,  50,  50,  40,   40]
    tempering:          [ 60,  60,  60,  50,  50,  40,   40]
    sigil_preservation: [  0,   0,   0,  10,  12,  15,   18]
    sigil_war:          [  0,   0,  15,  20,  20,  20,   20]
    sigil_warding:      [  0,   0,  15,  20,  20,  20,   20]
    sigil_fortune:      [  0,   0,  15,  20,  20,  20,   20]
    sigil_culling:      [  0,   0,  10,  15,  18,  20,   22]

  rarity_weights:         # T1  T2  T3  T4  T5  T6  T7
    uncommon:           [ 80, 70, 60, 50, 45, 40, 35]
    rare:               [ 18, 24, 28, 32, 33, 34, 35]
    epic:               [  2,  5, 10, 14, 17, 20, 22]
    legendary:          [  0,  1,  2,  4,  5,  6,  8]
    mythic:             [  0,  0,  0,  0,  0,  0,  0]

  boss_rarity_weights:
    uncommon:           [ 40, 30, 20, 15, 10,  5,  0]
    rare:               [ 40, 42, 42, 40, 38, 35, 30]
    epic:               [ 17, 22, 30, 35, 40, 45, 50]
    legendary:          [  3,  6,  8, 10, 12, 15, 20]
    mythic:             [  0,  0,  0,  0,  0,  0,  0]

  gear:
    require_recipe: true
    tiers_below: 1
    same_tier_weight: 3
    lower_tier_weight: 1
    slot_weights:
      melee_weapon: 1
      ranged_weapon: 1
      magic_weapon: 1
      shield: 1
      head: 1
      chest: 1
      legs: 1
      cape: 1
      utility_item: 1
      tool: 0.5
    exclude: []
    include: {}

  bosses:
    Eikthyr:     { tier: 1, stone_rolls: 2, gear_rolls: 1, bonus: [ { stone: awakening, chance: 100, amount: 2 } ] }
    gd_king:     { tier: 2, stone_rolls: 2, gear_rolls: 1, bonus: [ { stone: ascension, chance: 100, amount: 1 } ] }
    Bonemass:    { tier: 3, stone_rolls: 3, gear_rolls: 1, bonus: [ { stone: serpent, chance: 50, amount: 1 } ] }
    Dragon:      { tier: 4, stone_rolls: 3, gear_rolls: 1, bonus: [ { stone: exaltation, chance: 100, amount: 1 } ] }
    GoblinKing:
      tier: 5
      stone_rolls: 4
      gear_rolls: 1
      bonus:
        - { stone: exaltation, chance: 100, amount: 1 }
        - { stone: binding, chance: 25, amount: 1 }
    SeekerQueen: { tier: 6, stone_rolls: 4, gear_rolls: 1, bonus: [ { stone: transcendence, chance: 100, amount: 1 } ] }
    Fader:                                   # verify prefab name
      tier: 7
      stone_rolls: 5
      gear_rolls: 1
      bonus:
        - { stone: transcendence, chance: 100, amount: 1 }
        - { stone: apotheosis, chance: 10, amount: 1 }

  creatures: {}
  # creatures:
  #   Troll:        { stone_multiplier: 2 }          # trolls pay double stones
  #   Hen:          { multiplier: 0 }                # never drops our loot
  #   Serpent:      { tier: 4, bonus: [ { stone: serpent, chance: 20, amount: 1 } ] }

  chests:                  # world containers with default loot, rolled once when the game fills them (Phase 2)
    stone_chance: 30
    gear_chance: 10
    # containers:            # parsing pending (see above)
    #   TreasureChest_meadows: { tier: 2 }
    #   SomeModdedCrate:       { multiplier: 0 }

  ecr:                     # read only with Elite Creatures Reborn installed (Phase 2, ecr-integration.md)
    star_multipliers: [1, 1, 1.5, 2, 2.5, 3]
    star_step: 0.5
    star_rarity_bonus: 0
    tier_stone_multipliers: [1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7]
    tier_rarity_bonus:      [0, 5, 10, 15, 20, 25, 30, 35]
    skip_worthless: true
```

Essences add rows to this section (`essences.md` section 10): each land family's pair at its home tier in
`drops.stones` (Lesser 80, Greater 20, 0 elsewhere), a Lesser (100%) and a Greater (15%) row in each biome boss's
`bonus` list, and `drops.creatures` entries for `Serpent` and `BonemawSerpent` with the tide pair (25% / 5%).

---

# 9. Validation

Errors reject the family; warnings apply it.

| Check | Level |
| --- | --- |
| Rarity rules (`rarity.md` section 7) | error / warning as listed there |
| Duplicate stone id or prefab | error |
| A built-in stone id with a `prefab` other than its own; an owner-defined id with a `prefab` outside `ECF_Custom01`-`ECF_Custom16` | error |
| `tint` or a rarity `color` not `#RRGGBB`; `item_weight` below 0; `stack` below 1 | error |
| Unknown verb, unknown `outcome`, unknown `steer`, `steer: category` without a valid `category` | error |
| `applies_to`, `cost`, `weights` naming an unknown rarity | error |
| `tier_floor` outside 1-7 | error |
| `quality` stone without `slots`, `step` < 1, `cap` < `step` | error |
| Tier-indexed list not of length 7; negative weight or chance | error |
| A stone id in `drops.stones` or a boss/creature `bonus` that is not defined | error |
| Every `outcomes` weight 0 / every `gamble` weight 0 | warning; the stone is treated as disabled |
| A shipped stone id with no live definition (only possible with `use_defaults: false`) | warning (the stone becomes inert; its stacks stay) |
| A stone whose verb is not built in this version | warning; treated as disabled |
| `item_tiers` / `gear` / `bosses` / `creatures` key matching no prefab | warning (serves modpacks with optional content) |
| Mythic weight above 0 in a drop row while Mythic `drop_weight` is 0 | warning (the row cannot take effect) |
| A base that cannot fill Uncommon at its tier | warning; left out of the gear pool |
| A `drops.ecr` list empty or longer than 64 entries; a negative number in it | error |
| A `drops.ecr.tier_*` list longer than 8 (ECR's world tiers stop at 7) | warning |
| An `imbue` stone without `family`, or a `family` naming no `essence_families` entry | error |
| `family` on a stone whose verb is not `imbue` | warning (ignored) |
| An `imbue` stone whose `applies_to` includes the base rarity | error |
| An `essence_families` id not snake_case; an empty `affixes` list | error |
| A family member not defined in the affix family in force; a Mythic-only member | warning at every apply of either family, one line per family (skipped at roll time) |
| A member of weight 30 or less that is a slot's only member in its family | warning (design rule 3) |
| A `salvage.fragments` id that is not one of the five shards; a `stone` naming no defined stone | error |
| `fuse`, `amount` outside 1-999; `chance` outside 0-100 | error |
| A `salvage.yields` key naming no rarity; a row naming an undefined fragment | error |
| A yield row on the base rarity | warning, ignored |
| A yield row's `amount` above its fragment's `stack` | warning |
| A fragment `name` equal to a stone's or another fragment's name | error (they would stack together, IMP-56) |
| A `salvage.stations` name matching no station prefab | warning, logged in game when first needed (the server knows no prefabs at load) |

---

# 10. `essence_families`

Family id → `{ affixes, name }`, merged by key; an entry's `affixes` list is replaced whole. The field table, the
default lists and the design rules are `essences.md` sections 2 and 11. `name` defaults to `$ecf_family_<id>`.

# 11. `salvage`

`confirm`, `stations`, `yields` (rarity → rows `{ fragment, amount, chance }`, each rarity's list replaced whole)
and `fragments` (the five shards, merged by `id`, field by field: `stone`, `fuse`, `name`, `description`, `stack`,
`item_weight`, `tint`). The field tables and defaults are `salvage.md` section 7; the on/off switch is the `.cfg`'s
`8 - Salvage / Salvage`. `salvage.fragments` is the only id list below the root: the loader merges it by the dotted
path, so an overlay naming `{ id: shard_awakening, fuse: 4 }` changes that one field.

---

# Build checklist

- [ ] Main file written with these defaults on first run; extra files merged by key
- [ ] Every section read with file-and-line errors and unknown-key warnings
- [ ] Validation table implemented
- [ ] Synced, lockable, hot-reloaded (`configuration.md` sections 4-5)
- [ ] Stone `name`, `description`, `stack`, `item_weight`, `tint` reach the prefabs and live copies (shared `SharedData`, DECISIONS.md IMP-1)

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Economy YAML: ECO-1 to ECO-5; the `item_weight`
name is RC-6).

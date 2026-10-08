# EliteCrafting - specification: Economy YAML

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file is the **field-by-field reference** for the `EliteCrafting_economy*.yml` family: rarities, rolling rules,
runes (with the Serpent Rune's outcomes inside its entry), item-tier maps, biome tiers and drop tables. What the
fields *do* is in the feature files each section names; this file is the shape. The affix definitions live in the
other family, `EliteCrafting_inscriptions*.yml` (`affixes.md`).

This is the **canonical economy schema**: the conventions' baseline field names plus every field the economy
features need. The affix schema is `configuration.md` section 6.

**Status: built, not tested in game** (2026-09-23; cut to three rarities and six runes on 2026-10-02, when the rune keys
were renamed from `stone` to `rune`).

---

# 1. The file family

- **Main file** `EliteCrafting_economy.yml`, written on first run as a full, commented copy of the defaults below.
  Additional files `EliteCrafting_economy_<anything>.yml` in the config folder are read after it, in file-name order.
  Layering, `use_defaults` and the built-in default layer are `configuration.md` section 3.
- **Merge rule** (`configuration.md` section 3 owns it): list entries with an `id` (`rarities`, `runes`) merge
  **by id, field by field** - a later file names only the fields it changes; list-valued fields (`applies_to`,
  `outcomes`) are replaced whole; map-valued fields (`cost`) merge by key. For the sections this file adds, which
  are maps rather than id lists, the same idea applies: **maps merge by key** (`item_tiers.items`,
  `item_tiers.materials`, `item_tiers.stations`, `biomes`, `drops.runes`, `drops.rarity_weights`,
  `drops.boss_rarity_weights`, `drops.gear.slot_weights`, `drops.gear.include`, `drops.bosses`, `drops.creatures`),
  and a key set to `null` is removed; **tier-indexed lists are replaced whole** (a rune's seven weights), **boss and
  creature entries merge field by field** with their `bonus` list replaced whole, and scalars take the last value read
  (`../DECISIONS.md` ECO-1). So a server owner keeps the shipped defaults and writes only their changes in
  `EliteCrafting_economy_server.yml`.
- **Synced, lockable, hot-reloaded** by the mod's own rule plumbing (`configuration.md` sections 4-5): the server's
  files bind every player while locked; edits are picked up within five seconds or by `ecraft reload`. Errors reject
  the whole family (the previous configuration stays); warnings apply.
- **Percent convention:** every chance is a percent number (`4` means 4%), as in Elite Creatures Reborn's rule files
  (ECO-2). Weights are relative, any non-negative number.
- **Tier-indexed lists** have exactly seven entries, tier 1 (meadows) first (ECO-3). A list of another length is an
  error.

Top-level keys: `use_defaults` (main file only) and these sections:

| Section | Feature file |
| --- | --- |
| `rarities` | `rarity.md` |
| `rolling` | `rarity.md` section 4 |
| `runes` | `stones.md` |
| `item_tiers` | `item-tier.md` |
| `biomes` | `drops.md` section 3 |
| `drops` | `drops.md` |

---

# 2. `rarities`

A list, **in ladder order** (lowest first).

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `id` | id | yes | - | `normal`, `magic`, `rare`; referenced everywhere else |
| `name` | loc key | no | `$ecf_rarity_<id>` | display name |
| `color` | `#RRGGBB` | yes | - | the single source for every rarity-colored surface |
| `glow` | bool | no | true (false on the first entry) | ground glow; ignored (warning) on the base rarity |
| `inscriptions` | `{min, max}` | yes | - | affix count range; first entry must have `max: 0` |
| `drop_weight` | number | no | 1 | multiplier on this rarity's weight in every drop table; **0 blocks world drops entirely** |

```yaml
rarities:
  - id: normal
    color: "#FFFFFF"
    glow: false
    inscriptions: { min: 0, max: 0 }
    drop_weight: 0          # Normal is never a "magic drop"; vanilla items stay vanilla
  - id: magic
    color: "#1EFF00"
    inscriptions: { min: 1, max: 2 }
  - id: rare
    color: "#0070DD"
    inscriptions: { min: 3, max: 6 }
```

---

# 3. `rolling`

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `tier_window` | int 1-7 | 3 | an item rolls tiers `ceiling - window + 1` to `ceiling` |
| `promote_adds_at_least` | int | 1 | a promotion always adds at least this many affixes (0 = only what the new minimum needs) |
| `count_weights` | map rarity -> `{count: weight}` | none (uniform) | bias fresh rolls' affix count |

```yaml
rolling:
  tier_window: 3
  promote_adds_at_least: 1
  # count_weights:
  #   rare: { 3: 4, 4: 3, 5: 2, 6: 1 }
```

---

# 4. `runes`

A list of the seven runes (`stones.md`). The ids are fixed in code: an entry tunes or disables a rune, it never adds
one. Fields:

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `id` | id | yes | - | one of `awakening`, `shaping`, `recasting`, `ascension`, `consecrated`, `cleansing`, `serpent`; any other id is an error |
| `name` | loc key or literal text | no | `$ecf_stone_<id>` | the item name (`localization.md` section 4); literal text shows as written in every language |
| `description` | loc key or literal text | no | `$ecf_stone_<id>_desc` | the item description in the vanilla tooltip |
| `prefab` | name | no | `ECF_` + PascalCase id | the code-registered prefab this entry defines; if given it must be the rune's own |
| `verb` | enum | yes | - | `promote`, `add`, `reroll`, `strip`, `corrupt` |
| `applies_to` | rarity ids | yes | - | rarities the rune accepts |
| `cost` | map rarity -> int 0-999 | no | 1 for every rarity | runes consumed per use, from the held stack; 0 = free |
| `tier_floor` | int 1-7 | no | none | weakest affix tier this rune rolls, counted down like the tooltip (1 = the best tier the item allows; clamped to the ceiling) |
| `enabled` | bool | no | true | false: never drops, refuses with `stone_disabled`; the prefab still exists (prefabs come from code) |
| `confirm` | bool | no | false | requires the client's confirm gate (Cleansing, Serpent) |
| `stack` | int 1-9999 | no | 50 | max stack size of the rune item |
| `item_weight` | number | no | 0.2 | item weight of one rune. Never called plain `weight` on a rune, so it cannot be confused with drop weights |
| `tint` | `#RRGGBB` | no | the rune's default tint (`prefabs.md` section 4) | world-model and icon tint; Awakening and Ascension default to the color of the rarity they produce |

Verb-specific fields:

| Verb | Field | Type | Default | Meaning |
| --- | --- | --- | --- | --- |
| `corrupt` | `outcomes` | list of `{outcome, weight}` | - (required) | `outcome` in `seal_only`, `add_inscription`, `chaotic_reroll` |
| `corrupt` | `overflow` | int | 1 | how far `add_inscription` may exceed the rarity maximum |

---

# 5. The seven runes: defaults

```yaml
runes:
  - { id: awakening,   verb: promote, applies_to: [normal] }       # Normal -> Magic, one inscription
  - { id: shaping,     verb: add,     applies_to: [magic] }        # one more on a Magic item (up to 2)
  - { id: recasting,   verb: reroll,  applies_to: [magic] }        # all inscriptions replaced by 1-2 new
  - { id: ascension,   verb: promote, applies_to: [magic] }        # Magic -> Rare, one more (to Rare's 3 at least)
  - { id: consecrated, verb: add,     applies_to: [rare] }         # one more on a Rare item (up to 6)
  - id: cleansing                                                  # back to Normal
    verb: strip
    applies_to: [magic, rare]
    confirm: true
  - id: serpent                                                    # sealed for good, one of three outcomes
    verb: corrupt
    applies_to: [magic, rare]
    confirm: true
    overflow: 1                # add_inscription may go this many past the rarity's maximum
    outcomes:
      - { outcome: seal_only,       weight: 35 }   # sealed as it is
      - { outcome: add_inscription, weight: 35 }   # one more inscription, past the cap
      - { outcome: chaotic_reroll,  weight: 30 }   # every inscription rerolled at any tier
```

Every entry above takes the common defaults: `name: $ecf_stone_<id>`, `description: $ecf_stone_<id>_desc`,
`prefab: ECF_<PascalCase id>`, `cost` 1 everywhere, no `tier_floor`, `enabled: true`, `confirm: false` unless set,
`stack: 50`, `item_weight: 0.2`, the default tint. Written out in full, one entry looks like:

```yaml
  - id: consecrated
    name: $ecf_stone_consecrated
    description: $ecf_stone_consecrated_desc
    prefab: ECF_Consecrated
    verb: add
    applies_to: [rare]
    cost: { rare: 1 }
    # tier_floor: 3          # e.g. only tier 3 or better
    enabled: true
    confirm: false
    stack: 50
    item_weight: 0.2
    tint: "#E6C35C"
```

An owner's override file names only what it changes:

```yaml
runes:
  - id: serpent
    enabled: false           # no corruption on this server
  - id: consecrated
    cost: { rare: 2 }
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
| `max_runes_per_kill` | int | 5 | cap on runes from one non-boss kill |
| `max_gear_per_kill` | int | 2 | cap on gear from one non-boss kill |
| `star_multipliers` | list | `[1, 2, 3]` | by stars (vanilla level - 1); index 0 = no stars |
| `star_step` | number | 1 | added per star beyond the list |
| `chances.rune` | 7 percents | `[35, 40, 45, 50, 55, 60, 65]` | base rune chance per tier |
| `chances.gear` | 7 percents | `[1, 1.25, 1.5, 1.75, 2, 2.25, 2.5]` | base gear chance per tier |
| `runes` | map rune id -> 7 weights | table below | rune draw per tier; missing id = never drops |
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
| `chests.rune_chance` | percent | 50 | rune chance per world container (Phase 2, `drops.md` section 11) |
| `chests.gear_chance` | percent | 10 | gear chance per world container |
| `chests.containers` | map container prefab -> container entry | `{}` | per-container overrides |
| `ecr.star_multipliers` | list, index = Elite Creatures Reborn stars | `[1, 1, 1.5, 2, 2.5, 3]` | replaces `star_multipliers` for ECR-resolved creatures while the `.cfg` `Synergy` is on (`ecr-integration.md` 4) |
| `ecr.star_step` | number | 0.5 | added per ECR star beyond the list |
| `ecr.star_rarity_bonus` | percent per ECR star | 0 | gear rarity shift above Magic, summed with Norns' Favour |
| `ecr.tier_rune_multipliers` | list, index = ECR world tier 0-7 | `[1, 1.1, ... 1.7]` | rune chance factor; inert until ECR records a tier (`ecr-integration.md` 5, DECISIONS ECR-6) |
| `ecr.tier_rarity_bonus` | list of percents, index = ECR world tier | `[0, 5, ... 35]` | gear rarity shift; inert likewise |
| `ecr.skip_worthless` | bool | true | ECR's Cloven twin and Phantom husks drop nothing from us, whenever ECR is installed (`ecr-integration.md` 6) |

Boss entry: `tier`, `rune_rolls`, `gear_rolls`, `bonus: [{rune, chance, amount}]`.
Creature entry: `tier`, `multiplier`, `rune_multiplier`, `gear_multiplier`, `bonus` (same shape).
Container entry (`chests.containers`, Phase 2, `../DECISIONS.md` IMP-79): the creature entry's shape - `tier` (overrides
the biome at the container), `multiplier` (0 = the container never rolls), `rune_multiplier`, `gear_multiplier`,
`bonus` - merged by key and field by field like `creatures`. Chests share the rune and rarity tables and the
`max_runes_per_kill` / `max_gear_per_kill` caps with creatures; they have no stars. A container with no entry uses its
biome's tier and the flat chances.

```yaml
drops:                    # on/off: the .cfg switches `Rune drops` and `Magic item drops` (configuration.md)
  tamed: false
  require_player: true
  max_runes_per_kill: 5
  max_gear_per_kill: 2
  star_multipliers: [1, 2, 3]
  star_step: 1

  chances:
    rune: [35, 40, 45, 50, 55, 60, 65]
    gear: [1, 1.25, 1.5, 1.75, 2, 2.25, 2.5]

  runes:                  # T1  T2  T3  T4  T5  T6  T7  T8
    awakening:          [37, 26, 21, 18, 15, 14, 12, 10]
    recasting:          [50, 50, 50, 50, 50, 50, 50, 50]
    ascension:          [ 0, 10, 12, 13, 13, 12, 12, 11]
    cleansing:          [ 3,  3,  3,  3,  4,  4,  4,  5]
    serpent:            [ 0,  0,  2,  3,  4,  5,  6,  7]
    dvergr_chisel:      [10, 11, 12, 13, 14, 15, 16, 17]

  rarity_weights:         # T1  T2  T3  T4  T5  T6  T7
    magic:              [ 80, 70, 60, 50, 45, 40, 35]
    rare:               [ 20, 30, 40, 50, 55, 60, 65]

  boss_rarity_weights:
    magic:              [ 40, 30, 20, 15, 10,  5,   0]
    rare:               [ 60, 70, 80, 85, 90, 95, 100]

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
    Eikthyr:
      tier: 1
      rune_rolls: 2
      gear_rolls: 1
      bonus:
        - { rune: awakening, chance: 100, amount: 2 }
        - { rune: shaping, chance: 100, amount: 1 }
    gd_king:     { tier: 2, rune_rolls: 2, gear_rolls: 1, bonus: [ { rune: ascension, chance: 100, amount: 1 } ] }
    Bonemass:
      tier: 3
      rune_rolls: 3
      gear_rolls: 1
      bonus:
        - { rune: serpent, chance: 50, amount: 1 }
        - { rune: consecrated, chance: 50, amount: 1 }
    Dragon:      { tier: 4, rune_rolls: 3, gear_rolls: 1, bonus: [ { rune: consecrated, chance: 100, amount: 1 } ] }
    GoblinKing:
      tier: 5
      rune_rolls: 4
      gear_rolls: 1
      bonus:
        - { rune: consecrated, chance: 100, amount: 1 }
        - { rune: serpent, chance: 25, amount: 1 }
    SeekerQueen: { tier: 6, rune_rolls: 4, gear_rolls: 1, bonus: [ { rune: consecrated, chance: 100, amount: 2 } ] }
    Fader:                                   # verify prefab name
      tier: 7
      rune_rolls: 5
      gear_rolls: 1
      bonus:
        - { rune: consecrated, chance: 100, amount: 2 }
        - { rune: serpent, chance: 50, amount: 1 }

  creatures: {}
  # creatures:
  #   Troll:        { rune_multiplier: 2 }           # trolls pay double runes
  #   Hen:          { multiplier: 0 }                # never drops our loot
  #   Wolf:         { tier: 4, bonus: [ { rune: serpent, chance: 20, amount: 1 } ] }

  chests:                  # world containers with default loot, rolled once when the game fills them (Phase 2)
    rune_chance: 30
    gear_chance: 10
    # containers:
    #   TreasureChest_meadows: { tier: 2 }
    #   SomeModdedCrate:       { multiplier: 0 }

  ecr:                     # read only with Elite Creatures Reborn installed (Phase 2, ecr-integration.md)
    star_multipliers: [1, 1, 1.5, 2, 2.5, 3]
    star_step: 0.5
    star_rarity_bonus: 0
    tier_rune_multipliers: [1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7]
    tier_rarity_bonus:     [0, 5, 10, 15, 20, 25, 30, 35]
    skip_worthless: true
```

---

# 9. Validation

Errors reject the family; warnings apply it.

| Check | Level |
| --- | --- |
| Rarity rules (`rarity.md` section 7) | error / warning as listed there |
| The same rune id twice in one file; two runes on one prefab | error |
| A rune id that is not one of the seven; a `prefab` other than the rune's own | error |
| A rune without `verb` or `applies_to`; a `corrupt` rune without `outcomes` | error |
| `tint` or a rarity `color` not `#RRGGBB`; `item_weight` below 0; `stack` outside 1-9999 | error |
| Unknown verb, unknown `outcome` | error |
| `applies_to` or `cost` naming an unknown rarity | error |
| `tier_floor` outside 1-7 | error |
| Tier-indexed list not of length 7; negative weight or chance | error |
| A rune id in `drops.runes` or a boss, creature or container `bonus` that is not defined | error |
| Two runes with the same `name` | error (they would stack together, IMP-56) |
| A rune `name` that is a game item's name (`$item_...`) | warning (the rune would stack with that item) |
| Every `outcomes` weight 0 | warning; the rune is treated as disabled |
| A rune with no live definition (only possible with `use_defaults: false`) | warning (the rune becomes inert; its stacks stay) |
| A rune whose verb is not built in this version | warning; treated as disabled |
| `item_tiers` / `gear` / `bosses` / `creatures` key matching no prefab | warning (serves modpacks with optional content) |
| A weight above 0 in a drop row for a rarity whose `drop_weight` is 0 (Normal, by default) | warning (the row cannot take effect) |
| A base that cannot fill Magic at its tier | warning; left out of the gear pool |
| A `drops.ecr` list empty or longer than 64 entries; a negative number in it | error |
| A `drops.ecr.tier_*` list longer than 8 (ECR's world tiers stop at 7) | warning |

---

# Build checklist

- [ ] Main file written with these defaults on first run; extra files merged by key
- [ ] Every section read with file-and-line errors and unknown-key warnings
- [ ] Validation table implemented
- [ ] Synced, lockable, hot-reloaded (`configuration.md` sections 4-5)
- [ ] Rune `name`, `description`, `stack`, `item_weight`, `tint` reach the prefabs and live copies (shared `SharedData`, DECISIONS.md IMP-1)

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-10-02 | Three rarities, the six runes, `stone` keys renamed to `rune` (user decision). | pending |
| 2026-10-04 | The Recasting Rune (`recasting`, verb `reroll`) and its drop row (user decision). | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Economy YAML: ECO-1 to ECO-5; the `item_weight`
name is RC-6) and the 2026-10-02 entry of `../PLAN.md`'s Decisions log.

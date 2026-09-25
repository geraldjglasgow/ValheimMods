# EliteCrafting - specification: Item tier

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers **how strong an affix an item may carry**: every magic base has a *tier ceiling* from 1 to 7, and
no ordinary roll on that item produces an affix tier above it. How the ceiling combines with a stone's floor and
the tier window is `rarity.md` section 4; the affix tiers themselves are `affixes.md`.

**The model is decided** (user, 2026-09-23, `../DECISIONS.md` U-13): explicit item map, then highest recipe-material
tier, then crafting-station fallback, then tier 1. Section 2 keeps the alternatives that were weighed.

**Status: Phase 1 built, not tested in game** (2026-09-23). Ships in **Phase 1** - every roll needs a ceiling.

---

# 1. Tiers

One tier per biome gate, fixed ids (conventions):

| Tier | Biome |
| --- | --- |
| 1 | meadows |
| 2 | black_forest |
| 3 | swamp |
| 4 | mountain |
| 5 | plains |
| 6 | mistlands |
| 7 | ashlands |
| (8) | deep_north - reserved, not shipped; nothing may map to 8 until it is |

The ceiling is **computed, never stored on the item.** It is a property of the base (a Bronze Sword is always tier
2), derived at load. Each affix on an item stores its own tier (item-data), so changing the tier model later never
rewrites an existing item. There is no tier key on the item (`item-data.md` section 3): a drop does not remember
the biome it fell in, because its base already says how strong it may be.

---

# 2. The four candidate approaches, and the decision

| Approach | For | Against |
| --- | --- | --- |
| (a) **Recipe materials** - a YAML map of material prefab -> tier; item tier = the highest tier among its recipe's materials | Tracks the game's own progression exactly (bronze = Black Forest, black metal = Plains, flametal = Ashlands); works for modded items that use vanilla materials with no configuration; one short map covers hundreds of items | Items with no recipe need a fallback; modded materials need a map entry or a derivation |
| (b) **Crafting station + level** | Tiny map | Cannot separate Mistlands from Ashlands (Ashlands adds no new station, it upgrades existing ones); station levels span several biomes (the forge covers Black Forest to Plains); upgrade level is not the same thing as biome |
| (c) **World progression** (boss keys) | Zero per-item data | Tier would belong to the world, not the item: a Meadows club crafted after Yagluth would roll Plains-strength affixes, and the same item would change strength over time. Contradicts PLAN ("a Meadows Mythic rolls many weak affixes") |
| (d) **Explicit item -> tier map** with a fallback | Exact where written | Hundreds of lines to write and maintain by hand; rots with every game update |

**Decision: (d) as an override layer over (a), with (b) and a fixed default as the last two fallbacks.**
Materials do the work for nearly every item, vanilla or modded; the explicit map exists for the handful of items
the material rule gets wrong or cannot see (vendor items, recipe-less drops, deliberate rebalancing).

---

# 3. The derivation, step by step

For each magic base (`rarity.md` section 2), the first step that yields a tier wins:

1. **Explicit override.** `item_tiers.items: {<item prefab>: <tier>}`. Wins over everything.
2. **Recipe materials.** Find the item's recipes in the object database (recipes whose result is this item and that
   are enabled).
   - For each recipe, take every resource with a **craft amount above 0** (the requirement's base amount). Resources
     that appear only as upgrade costs are ignored - affix values never scale with upgrade level (user decision),
     so the item's tier is what it takes to *make* it.
   - Each resource's tier: its entry in `item_tiers.materials`; if absent and the resource is itself crafted by a
     recipe, **that intermediate's derived tier** (recursively, depth at most `item_tiers.material_depth`, default
     3, cycle-safe); otherwise unknown and skipped.
   - The recipe's tier = the highest known resource tier. **If an item has several recipes, the lowest recipe tier
     wins** - the tier reflects the earliest point a player can own the item. (Judgement call, `../DECISIONS.md`
     TIR-1.)
   - If no resource of any recipe resolves, fall through.
3. **Crafting station.** `item_tiers.stations: {<station prefab>: <tier>}`, from the recipe's crafting station
   (lowest across recipes). Optional per-level refinement: `item_tiers.station_levels: {<station>: {<min level>:
   <tier>}}`, where the highest listed level not above the recipe's minimum station level applies.
4. **Fixed fallback.** `item_tiers.fallback_tier` (default 1). Covers recipe-less modded items nobody mapped.

The result is clamped to 1..7. Every step is logged at debug level with the reason, and `ecraft tiers` writes it
out (section 7). The derivation needs item prefabs, recipes and the material map, all of which are verified in game
with `ecraft dump items` and `ecraft tiers` during Phase 1.

**Why the highest material and not an average:** a recipe mixes cheap and defining materials (a black metal sword
also needs wood and leather). The defining material is always the latest one; averaging would pull every late item
down by its wood.

---

# 4. Default material map (`item_tiers.materials`)

Vanilla prefab names from general game knowledge of the game's own items. **Every row must be checked against the
live object database before release** - `ecraft tiers` lists any material used by a magic base's recipe that is not
in the map, and any map key that matches no prefab (section 7). Rows marked *verify* are ones I am least sure of
(name or biome).

Only materials that appear in equipment recipes matter; the map does not need food, building or cosmetic
materials.

| Tier | Materials (prefab names) | Verify |
| --- | --- | --- |
| 1 meadows | `Wood`, `Stone`, `Flint`, `Resin`, `LeatherScraps`, `DeerHide`, `Feathers`, `HardAntler` | - |
| 2 black_forest | `RoundLog`, `FineWood`, `Copper`, `CopperOre`, `Tin`, `TinOre`, `Bronze`, `BronzeNails`, `TrollHide`, `GreydwarfEye`, `SurtlingCore`, `BoneFragments`, `AncientSeed`, `Thistle`, `Coal` | `CopperScrap`, `CryptKey` |
| 3 swamp | `Iron`, `IronScrap`, `IronNails`, `Chain`, `ElderBark`, `Guck`, `Ooze`, `Bloodbag`, `Entrails`, `WitheredBone`, `Root`, `Wishbone` | `IronOre` |
| 4 mountain | `Silver`, `SilverOre`, `WolfPelt`, `WolfFang`, `Obsidian`, `FreezeGland`, `DragonTear`, `Crystal` | `WolfClaw`, `JuteRed`, `YmirRemains` |
| 4 (ocean, see note) | `SerpentScale`, `Chitin` | both |
| 5 plains | `BlackMetal`, `BlackMetalScrap`, `LinenThread`, `Flax`, `Needle`, `LoxPelt`, `Tar` | `YagluthDrop` |
| 6 mistlands | `Eitr`, `Sap`, `Softtissue`, `BlackCore`, `Carapace`, `ScaleHide`, `Mandible`, `BlackMarble`, `YggdrasilWood`, `JuteBlue`, `Wisp`, `Bilebag`, `RoyalJelly`, `QueenDrop` | `Iolite`, `DvergrNeedle`, `DvergrKeyFragment` |
| 7 ashlands | `FlametalNew`, `FlametalOreNew`, `CharredBone`, `AskHide`, `AskBladder`, `CelestialFeather`, `MoltenCore`, `GemstoneRed`, `GemstoneBlue`, `GemstoneGreen`, `Grausten`, `Blackwood`, `CharcoalResin`, `CharredCogwheel`, `MorgenSinew`, `MorgenHeart`, `ProustitePowder`, `SulfurStone`, `BellFragment` | `Flametal`, `FlametalOre` (legacy names), `FaderDrop` |

Notes:

- **Ocean** has no tier of its own. Ocean materials sit at tier 4 because a player meets sea serpents and the
  leviathan's chitin around the Swamp-to-Mountain stretch. Judgement call (`../DECISIONS.md` TIR-3).
- **Boss drops** sit at the boss's own biome (`HardAntler` is Eikthyr's, so tier 1, even though the antler pickaxe
  is used in the Black Forest) - the tier says where the material comes from, not where the item is used.
- Map keys that match no prefab (a name that changed in a game update, or a mod that is not installed) are a
  **warning, not an error**, so one map can serve modpacks with and without optional content.

## Worked examples (expected results; verify with `ecraft tiers`)

| Item | Defining material | Tier |
| --- | --- | --- |
| Club, Hammer, Torch, Leather armor | Wood / LeatherScraps / DeerHide | 1 |
| Antler pickaxe | HardAntler | 1 |
| Bronze sword, Troll armor, Cultivator | Bronze / TrollHide / Copper | 2 |
| Iron mace, Root armor, Iron pickaxe | Iron / Root / Iron | 3 |
| Frostner, Wolf armor, Wolf cape | Silver or FreezeGland / WolfPelt | 4 |
| Porcupine, Padded armor, Lox cape | Needle / LinenThread / LoxPelt | 5 |
| Mistlands staffs, Carapace armor and shield, Arbalest | Eitr / Carapace / YggdrasilWood | 6 |
| Ashlands weapons and armor | FlametalNew / AskHide / ... | 7 |

---

# 5. Items with no recipe

The material rule cannot see items that are bought, found or dropped rather than crafted. Default overrides for the
vanilla ones that resolve to a slot:

| Item (prefab) | Source | Tier | Verify |
| --- | --- | --- | --- |
| `BeltStrength` (Megingjord) | vendor | 3 | name, and whether it resolves to `utility_item` |
| `HelmetDverger` (Dverger circlet) | vendor | 3 | name |
| `FishingRod` | vendor | 1 | name, slot |
| `Wishbone` | Bonemass drop | 3 | slot (it is also a material) |

Hildir's cosmetic clothing, tankards, and other vendor/event items are left to `fallback_tier` (1) and are **excluded
from the pre-rolled drop pool** by the no-recipe rule in `drops.md`. They are magic bases when their item type maps
to a slot (`item-data.md` section 2, `../DECISIONS.md` ITD-2).

The alternative fallback for no-recipe items - the world's progression (approach c) - was considered and rejected
as the default for the reason in section 2 (TIR-2); it would be the answer if the user ever wants "a bought item is
as strong as the world is far".

---

# 6. Where the ceiling applies, and the one place it does not

**Every ordinary roll is capped by the ceiling**: promotions, Growth, Turmoil's added affix, Upheaval, the Stone of
Chance, and pre-rolled drops. A stone's `tier_floor` raises the bottom of the window but never the top: a floor
above the ceiling is clamped down to the ceiling (`rarity.md` section 4).

**Drops use the base's own tier**, not the biome's. A bronze sword that drops from a Plains creature (the drop pool
allows one tier below the biome - `drops.md`) rolls with ceiling 2, not 5. A drop's biome decides *which bases* can
appear; the base decides *how strong*.

**The only roll that can exceed the ceiling is the Serpent Stone's chaotic reroll.** Exactly:

- Every affix is rerolled (count within the rarity's range, bound affixes included and unbound - `stones.md`).
- For each new affix, the eligible tiers are **every tier the affix defines**, ignoring the item's ceiling, the tier
  window and any `tier_floor`.
- The tier is drawn **uniformly** among those tiers - the per-tier weights are ignored too, which is what makes it
  chaotic: a Meadows club is as likely to get a tier-7 affix as a tier-1 one, and an Ashlands blade is as likely to
  get a tier-1 one. (Judgement call, `../DECISIONS.md` TIR-4 - "ignores biome tier floors, any tiers" in PLAN.md
  could also be read as "weighted as usual, just uncapped".)
- The item is sealed afterwards, so nothing can later "fix" an out-of-ceiling affix.

Everything else that leaves an item holding a tier above its ceiling is **not a roll**:

- The Stone of Reflection copies affixes as they are.
- An owner lowering an item's tier in YAML leaves existing affixes untouched (values are fixed on items; TIR-6). Perfection
  on such an affix rerolls its value **within its stored tier**, so it stays above the new ceiling; Turmoil and
  Upheaval replace affixes under the new ceiling.

---

# 7. Precomputation, multiplayer, and the reference dump

- The tier table (item prefab -> tier, source) is computed **once per economy-YAML apply and once when the object
  database is complete**, on every peer, from the synced YAML and the peer's own object database. Recipes other
  mods add after ours are covered by recomputing on the first spawn, when every mod's recipes are in the object
  database. Nothing is computed per roll.
- Every peer derives the same table, because the mod is required on all peers and the YAML is the server's. A
  client with an extra content mod the server lacks derives tiers for items the server never sees; harmless.
- **`ecraft tiers`** (Phase 1, `console-commands.md`; `../DECISIONS.md` TIR-5) writes
  `EliteCrafting_item_tiers_reference.yml` next to the config - every magic base with its slot, tier and the step
  that decided it (`override`, `material: Bronze`, `station: forge`, `fallback`), then two lists: **materials used by magic-base recipes that are not in the map**, and **map keys
  that match no prefab**. The same delegation idea as ECR's `elite reference`: a server owner running a modpack
  pastes it at an assistant and gets back the missing map lines. The grammar is in `console-commands.md`.

---

# Build checklist

- [ ] Override -> materials (craft amounts only, recursive intermediates, lowest recipe) -> station -> fallback
- [ ] Default material map and no-recipe overrides, every name verified in game
- [ ] Unmatched map keys warn, never error
- [ ] Table computed per YAML apply and on first spawn, never per roll
- [ ] Serpent chaotic reroll the only uncapped roll
- [ ] `ecraft tiers` reference file with unmapped materials

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Proposed in Phase 0. | pending |
| 2026-09-23 | Model confirmed by the user; open questions moved to `../DECISIONS.md`. | pending |

---

# Decisions

The model is the user's (`../DECISIONS.md` U-13); its tuning questions are TIR-1 to TIR-6.

# EliteCrafting - specification: Drops

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what creatures drop *from this mod*: **stones**, and **pre-rolled magic gear** (Uncommon to
Legendary; Mythic never, by default - user decision 2026-09-23). A creature's vanilla loot is not touched. How a
drop's affixes are rolled is `rarity.md` section 4; how strong they may be is `item-tier.md`; the stones themselves
are `stones.md`.

Numbers are defaults and all of them are configurable in the `drops:` and `biomes:` sections of
`EliteCrafting_economy*.yml`. The two on/off switches - `Stone drops` and `Magic item drops` - are synced `.cfg`
settings (`configuration.md`); each roll below is skipped when its switch is off. **Almost every number here is a judgement call** meant to be tuned in play; the tables
say so once rather than on every row.

**Status: Phase 1 built, not tested in game** (2026-09-23). Creature stone drops and pre-rolled gear drops ship in **Phase 1**.
**Phase 2** (0.2.0): the loot-find stats (section 10) and chests (section 11) are built, not tested in game; the
find affixes reach the default YAML once their effect ids are registered. The Elite Creatures Reborn hook (section 12)
is not started.

---

# 1. Who rolls, and how the drops get into the world

- **The dying creature's ZDO owner rolls** - on a dedicated server that is often a nearby client, not the server
  (`multiplayer.md` section 2); the server rolls only for creatures no player is near. The game itself only runs
  death handling on the owner; our hook follows the game's own death handling on that same peer and does nothing
  anywhere else. One roll per death, never one per client, always under the server's synced tables
  (`../DECISIONS.md` RC-7).
- **Our drops do not go through the creature's vanilla drop list.** Verified in the decompile: when a creature
  leaves a ragdoll, the game stores its loot on the ragdoll as *prefab and amount only* and spawns it later - any
  custom data on a pre-rolled item would be lost there. So the mod builds its own small list and spawns it directly,
  using the game's own "drop this item data" path, which saves the item's full data (custom data included) to the
  new world object.
- **Our drops appear at the moment of death**, at the creature's center point with the same small scatter the game
  uses. The creature's vanilla loot may appear a few seconds later when its ragdoll dissolves. (Judgement call,
  `../DECISIONS.md` DRP-6; delaying ours to match would mean storing our rolls on the ragdoll.)
- Building a gear item: clone the base prefab's item data, set its drop prefab, upgrade level 1, full durability,
  the world's world level, and our keys, then hand it to the game's drop-item-data call, which saves the ZDO before
  any other peer can see the object (recipe in `~/scratch/specs/ec-game-notes.md` section 15). Stones are spawned the
  same way without keys.
- **Alternative considered for stones** (game notes section 15): append them to the creature's vanilla drop list in
  a postfix on its drop-list generator, so they ride the ragdoll and appear together with vanilla loot. Not chosen,
  because that generator is public and can be called for reasons other than a death (every call would roll our
  loot), other mods' patches on that list could multiply our stones, and one death hook for both stones and gear
  keeps the rules (tamed, involvement, tier) in one place (`../DECISIONS.md` RC-12).
- **Other loot systems never see our rows**, because our rows are not in the creature's drop list. Elite Creatures
  Reborn's loot modes, other mods' drop multipliers and the game's pseudo-random drop smoothing do not touch them;
  our stones are not multiplied by vanilla's level multiplier either (our own star multiplier, section 4, replaces
  it).
- A creature killed while the game marks it as cheated (dev commands) marks our drops cheated too, as vanilla does.

---

# 2. Which deaths drop

A death rolls our drops only when **all** hold:

1. It is a creature, not a player.
2. **It is not tamed** (`../DECISIONS.md` DRP-1). Otherwise a breeding pen of boars or a chicken coop becomes a stone farm with no
   risk. Vanilla still drops meat from butchered animals; only our drops are withheld. `drops.tamed: false`.
3. **A player was involved** (DRP-2, `drops.require_player: true`): at least one player hit the creature during
   its life, **or** a tamed creature did.
   - Players: the game already records, in the creature's ZDO, every player who damaged it (verified in the
     decompile - the same record that credits kills). The mod reads that record; no new data.
   - Tamed allies (a wolf pack, summoned skeletons): the game's record ignores them, so the mod sets its own flag on
     the creature's ZDO when a tamed attacker lands a hit: bool **`ecf_ally_hit`**, written by the creature's owner
     in the damage path (`SPEC.md` section 3). Without this, players who fight with tamed wolves would never see a
     stone.
   - So: **a creature a player knocked off a cliff drops**; one that fell, drowned, burned in lava or was killed by
     other wild creatures with no player or pet ever touching it **does not**. That keeps unattended farms (creature
     infighting, spawner traps) from paying out.
4. Its per-creature multiplier is not 0 (section 7).

---

# 3. The creature's tier

Every drop roll needs a tier 1..7 (the same scale as `item-tier.md`). For a creature, the first rule that answers:

1. **Boss map** (`drops.bosses`), by prefab name.
2. **Creature override** (`drops.creatures.<prefab>.tier`).
3. **Home biome from spawn data**: the lowest-tier biome among the creature's open-world spawn entries. Precomputed
   at load from the game's spawn lists, the same source Elite Creatures Reborn's reference file uses. A troll that
   chases a player into the Meadows still drops Black Forest loot; a Fuling raid at a Meadows base drops Plains
   loot.
4. **Biome at the death position**, for creatures no open-world spawn list names (dungeon and camp spawners,
   summons, event creatures). Uses the world generator's biome lookup, which works on a dedicated server without
   loaded terrain.

Biome to tier (`biomes:`):

| Biome | Tier |
| --- | --- |
| meadows | 1 |
| black_forest | 2 |
| swamp | 3 |
| mountain | 4 |
| plains | 5 |
| mistlands | 6 |
| ashlands | 7 |
| ocean | 4 (judgement call, matches `item-tier.md`) |
| deep_north | 7 (reserved biome; clamped until tier 8 exists) |

---

# 4. The chance model

Every qualifying death makes two independent rolls: **stones** and **gear**.

```
p = base_chance[tier] * star_multiplier[stars] * creature_multiplier      (* Fateweaver, Phase 2 - stones only)
count = floor(p) + (random < frac(p) ? 1 : 0), capped at max_per_kill
```

A probability above 1 therefore means "guaranteed, plus a chance of one more", so big multipliers stay meaningful
instead of saturating at one drop.

- **Stars** are the vanilla level minus one (0, 1 or 2 in vanilla; more with mods that raise it). With Elite
  Creatures Reborn installed, creatures stay at vanilla level 1 and ECR's own stars are invisible to this mod until
  the Phase 2 hook (section 12).
- Each stone drawn picks one stone from the tier's stone table (section 5), amount 1.
- Each gear item drawn picks a rarity from the tier's rarity weights, then a base from the tier's gear pool
  (sections 6 and 8).

## Default base chances and multipliers

| Tier | Stone chance | Gear chance |
| --- | --- | --- |
| 1 meadows | 4% | 1.00% |
| 2 black_forest | 5% | 1.25% |
| 3 swamp | 6% | 1.50% |
| 4 mountain | 7% | 1.75% |
| 5 plains | 8% | 2.00% |
| 6 mistlands | 9% | 2.25% |
| 7 ashlands | 10% | 2.50% |

| Stars | Multiplier |
| --- | --- |
| 0 | x1 |
| 1 | x2 |
| 2 | x3 |
| each star beyond | +1 |

| Cap | Default |
| --- | --- |
| `max_stones_per_kill` | 5 |
| `max_gear_per_kill` | 2 |

Rough feel at 40 kills an hour in the Black Forest, all unstarred: two stones an hour, one magic item every two
hours. Starred creatures pay double or triple. Bosses pay far more (section 9).

---

# 5. Which stones drop where

Relative weights per tier. 0 means the stone never drops at that tier. **A disabled stone is removed from every
table**, so in Phase 1 (only the Phase 1 stones enabled) the per-kill stone chance is unchanged and the draw is
shared among the Phase 1 stones.

The ranges follow the catalog: Awakening Meadows+, Ascension Black Forest+, Exaltation Mountain+, Transcendence
Mistlands+, Apotheosis Ashlands only and very rare. The rest are adopted defaults (DRP-13): basic currency (Awakening, Turmoil,
Chance, quality) everywhere; power currency (Greater grades, Binding, Preservation) late; the Serpent from the Swamp
on, where corruption is at home.

| Stone | T1 | T2 | T3 | T4 | T5 | T6 | T7 | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| awakening | 400 | 300 | 220 | 160 | 120 | 100 | 80 | 1 |
| ascension | 0 | 120 | 120 | 110 | 100 | 90 | 80 | 1 |
| exaltation | 0 | 0 | 0 | 60 | 70 | 70 | 70 | 1 |
| transcendence | 0 | 0 | 0 | 0 | 0 | 30 | 40 | 1 |
| apotheosis | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 1 |
| growth_lesser | 0 | 80 | 90 | 90 | 80 | 70 | 60 | 1 |
| growth_greater | 0 | 0 | 0 | 20 | 30 | 40 | 50 | 2 |
| turmoil_lesser | 150 | 150 | 140 | 120 | 100 | 90 | 80 | 1 |
| turmoil_greater | 0 | 0 | 0 | 30 | 40 | 50 | 60 | 2 |
| upheaval_lesser | 0 | 40 | 50 | 50 | 50 | 45 | 40 | 2 |
| upheaval_greater | 0 | 0 | 0 | 15 | 20 | 25 | 30 | 2 |
| perfection_lesser | 0 | 0 | 30 | 40 | 40 | 40 | 40 | 1 |
| perfection_greater | 0 | 0 | 0 | 0 | 10 | 15 | 20 | 2 |
| severing_lesser | 0 | 50 | 60 | 60 | 55 | 50 | 45 | 2 |
| severing_greater | 0 | 0 | 0 | 15 | 20 | 25 | 30 | 2 |
| unmaking | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 2 |
| serpent | 0 | 0 | 20 | 25 | 30 | 35 | 40 | 2 |
| binding | 0 | 0 | 0 | 0 | 5 | 8 | 10 | 2 |
| chance | 80 | 80 | 70 | 60 | 50 | 40 | 30 | 2 |
| reflection | 0 | 0 | 0 | 0 | 0 | 0.02 | 0.05 | 2 |
| honing | 60 | 60 | 60 | 50 | 50 | 40 | 40 | 2 |
| tempering | 60 | 60 | 60 | 50 | 50 | 40 | 40 | 2 |
| sigil_preservation | 0 | 0 | 0 | 10 | 12 | 15 | 18 | 2 |
| sigil_war | 0 | 0 | 15 | 20 | 20 | 20 | 20 | 2 |
| sigil_warding | 0 | 0 | 15 | 20 | 20 | 20 | 20 | 2 |
| sigil_fortune | 0 | 0 | 15 | 20 | 20 | 20 | 20 | 2 |
| sigil_culling | 0 | 0 | 10 | 15 | 18 | 20 | 22 | 2 |

What the numbers mean in play (all stones enabled, unstarred, non-boss):

- **Apotheosis** in the Ashlands: 10% x 1/1016 = about **1 in 10,000 kills**, plus the Fader's bonus row (section 9).
  Craft-only Mythic stays the chase. In Phase 1, with only eight stones sharing the table, it is about 1 in 4,500.
- **Reflection** (enabled, astronomically rare - plan default): in the Ashlands about **1 in 200,000 kills**; in the
  Mistlands about 1 in 570,000. No boss bonus. A server that wants it gone sets `enabled: false`.
- **Phase 1 only** (8 stones enabled), Black Forest: Awakening 46%, Turmoil 23%, Ascension 18%, Growth 12%.

---

# 6. Gear rarity weights

Relative weights per tier for a pre-rolled drop, then multiplied by the rarity's `drop_weight` (default 1; **Mythic
0, which blocks Mythic whatever the table says** - `rarity.md` section 5).

| Tier | Uncommon | Rare | Epic | Legendary | Mythic |
| --- | --- | --- | --- | --- | --- |
| 1 | 80 | 18 | 2 | 0 | 0 |
| 2 | 70 | 24 | 5 | 1 | 0 |
| 3 | 60 | 28 | 10 | 2 | 0 |
| 4 | 50 | 32 | 14 | 4 | 0 |
| 5 | 45 | 33 | 17 | 5 | 0 |
| 6 | 40 | 34 | 20 | 6 | 0 |
| 7 | 35 | 35 | 22 | 8 | 0 |

Boss gear uses its own row (section 9). Norns' Favour shifts these in Phase 2 (section 10).

---

# 7. Per-creature rules

`drops.creatures`, keyed by creature prefab name - the same names the game uses, so a modded creature works like a
vanilla one.

| Field | Meaning | Default |
| --- | --- | --- |
| `tier` | override the derived tier | derived |
| `multiplier` | scales both chances; 0 = this creature never drops our loot | 1 |
| `stone_multiplier`, `gear_multiplier` | scale one roll only | 1 |
| `bonus` | extra rows, section 9 | none |

No vanilla creature is excluded by default. The tamed and player-involvement rules already cover summons, pets and
farms; an owner who finds a spawn-farmable creature sets its multiplier to 0.

---

# 8. The pre-rolled gear pool

Built per tier at load, from the magic bases (`rarity.md` section 2) and their derived tiers (`item-tier.md`):

- **Which bases**: every magic base whose tier is the drop tier (weight 3) or one below it (weight 1)
  (`gear.tiers_below: 1`, `gear.same_tier_weight: 3`, `gear.lower_tier_weight: 1`). A Plains troll-equivalent can
  drop an iron mace now and then, never a flametal sword.
- **Excluded**: items with **no recipe** (vendor items, event rewards, dev items: Megingjord should not fall out of a
  greyling); items the game marks as **DLC** or **quest** items; anything in `gear.exclude`. Items in
  `gear.include: {<prefab>: <tier>}` are added regardless.
- **Slot weights** multiply the base weight (`gear.slot_weights`), so an owner can make armor commoner than tools.
  Defaults: every slot 1, **tool 0.5** (a magic hammer from a troll is odd, but a magic pickaxe is a fine find).
- **A base whose slot pool cannot fill Uncommon's minimum** at its tier is left out, with a load warning.
- **The item's affixes use the base's own tier** as their ceiling (`item-tier.md` section 6) - never the biome's.

The rolled item:

| Property | Value |
| --- | --- |
| Rarity | drawn from the tier's weights; lowered to the highest rarity the pool can fill if needed |
| Affixes | count uniform in the rarity's range; ceiling = base tier; the usual window; no tier floor |
| Upgrade level | 1 |
| Durability | full |
| World level | the world's, as vanilla drops get |
| Crafter | none |
| Variant (shields, capes with styles) | random among the base's variants (judgement call) |
| Quality (Honing/Tempering) | 0 |
| Sealed, bound, pending sigil | none |

Expected contents of the default pools (derived, not listed in YAML; verify with the reference command):
tier 1 - flint and stone tools and weapons, crude bow, wooden shields, leather and rag armor, deer-hide cape;
tier 2 - bronze and bone weapons, bronze and troll armor, the antler/bronze pickaxes, finewood bow; and so on up to
tier 7's flametal and ask-hide gear.

---

# 9. Bosses

A creature is a boss when it is in the boss map, or when the game flags it as a boss (then with its derived tier).

Every boss kill, independent of the chance model:

- **Guaranteed stones**: `stone_rolls` stones from its tier's table.
- **Guaranteed gear**: `gear_rolls` items, rarity from the **boss rarity row** of its tier.
- **Bonus rows**: `{stone, chance, amount}` rolled independently.
- Stars and the player-involvement rule still apply (a boss that somehow dies with no player involved drops
  nothing); stars multiply the guaranteed counts.

| Boss (prefab) | Tier | Stone rolls | Gear rolls | Bonus rows |
| --- | --- | --- | --- | --- |
| `Eikthyr` | 1 | 2 | 1 | awakening 100% x2 |
| `gd_king` (The Elder) | 2 | 2 | 1 | ascension 100% x1 |
| `Bonemass` | 3 | 3 | 1 | serpent 50% x1 |
| `Dragon` (Moder) | 4 | 3 | 1 | exaltation 100% x1 |
| `GoblinKing` (Yagluth) | 5 | 4 | 1 | exaltation 100% x1, binding 25% x1 |
| `SeekerQueen` (The Queen) | 6 | 4 | 1 | transcendence 100% x1 |
| `Fader` *(verify name)* | 7 | 5 | 1 | transcendence 100% x1, apotheosis 10% x1 |

A bonus row naming a disabled stone is skipped. In Phase 1 the Bonemass and Yagluth Phase-2 rows are simply inert.

Boss rarity rows:

| Tier | Uncommon | Rare | Epic | Legendary | Mythic |
| --- | --- | --- | --- | --- | --- |
| 1 | 40 | 40 | 17 | 3 | 0 |
| 2 | 30 | 42 | 22 | 6 | 0 |
| 3 | 20 | 42 | 30 | 8 | 0 |
| 4 | 15 | 40 | 35 | 10 | 0 |
| 5 | 10 | 38 | 40 | 12 | 0 |
| 6 | 5 | 35 | 45 | 15 | 0 |
| 7 | 0 | 30 | 50 | 20 | 0 |

Bosses can be summoned again, so the Fader's 10% Apotheosis is the realistic route to a Mythic: about ten Fader
kills. That is the number to argue with.

---

# 10. The loot-find affixes - Phase 2

Four affixes feed the economy (`affixes.md`): **Norns' Favour** (`find_rarity`), **Fateweaver** (`find_stones`),
**Trophy Taker** (`find_trophy`) and **Hoardfinder** (`find_coins`). All four read the **killer's** gear.

- **The killer** is the creature's last attacker if that is a player. If the last hit was not a player's (a fall, a
  pet), there is no killer bonus. (Judgement call, DRP-12; "best of all involved players" is the alternative.) The
  game records the last hit on the creature's owner for every hit that lands while the creature is alive, the killing
  blow included (verified in the decompile).
- The drop runs on the creature's owner, which usually cannot see the killer's inventory. So each player's totals
  travel as **a small synced player stat**: the player's own client writes its current capped totals to its player
  ZDO (`ecf_find_rarity`, `ecf_find_stones`, `ecf_find_trophy`, `ecf_find_coins`; `effects-runtime.md` section 7)
  whenever its equipment changes, it spawns, an equipped item's state is written, the rules change or the `Affix
  effects` switch flips - and only when a value changed, so normal play sends nothing. The owner reads four floats.
  No inventory inspection, no RPC.
- **The owner trusts the rules, not the number**: each value read is clamped to 0 .. the running rules' cap for its
  channel, and a stat that no affix in the running rules feeds reads as 0. Only unconditional affixes count (a
  health-critical find affix would depend on the killer's health at the kill). With the `Affix effects` switch off
  there is no bonus. (`../DECISIONS.md` IMP-73.)
- **Fateweaver +F%**: the stone chance `p` is multiplied by `1 + F/100`. Gear chance untouched.
- **Norns' Favour +L%**: every rarity weight above Uncommon (above the first magic rung of the ladder, IMP-74) is
  multiplied by `1 + L/100` before the draw. Mythic stays at 0 (0 times anything). It does not change whether gear
  drops, only how good it is.
- **Trophy Taker +T%** and **Hoardfinder +C%** act on the creature's **vanilla** loot, not ours: for that one death,
  the chance of each row of the creature's own drop table that is a trophy (item type Trophy), or coins and treasure
  (the `Coins` item, or any item the trader buys: item value above 0), is multiplied by `1 + X/100` inside the game's
  own drop roll and restored right after. Amounts, the level multiplier and the game's pseudo-random drop smoothing
  stay the game's. A tamed or summoned creature is not an enemy: no bonus. (IMP-75.)
- None of them touches boss guarantees' counts; Norns' Favour does shift the boss rarity row. Chests have no killer
  and get none of them.

---

# 11. Chests and world containers - Phase 2

- **Which containers**: every container the game fills with default loot - it has a non-empty default drop table -
  and that no player built (the `creator` of a placed piece is 0): dungeon and ruin chests, buried treasure, and a
  modded container of the same kind. Player-built chests, carts, ships and tombstones never roll. (IMP-77.)
- **When and where**: once, at the moment the game fills it with its default items (verified in the decompile:
  `Container.AddDefaultItems`, called from the container's `Awake` on its ZDO owner while the game's own "default items
  added" flag is unset) - on that owner, usually the client of the player whose area generated it, else the server,
  under the server's synced tables. The container ZDO gets our bool **`ecf_filled`** before anything goes in, so it
  never rolls twice. Containers the game generated before the mod was installed were filled then and **do not** roll;
  rolling them on their next open would also pay out chests that were already looted. (IMP-76.)
- **Tier**: the container entry's `tier` (below), else the biome at the container's position (dungeons report their
  surface biome; the `biomes:` map).
- **Chances**: `drops.chests.stone_chance` (default 30) and `gear_chance` (default 10), percent per container, the
  same floor/fraction rule as creatures (section 4), times the container entry's multipliers; the same stone table
  (section 5) and gear rarity rows (section 6) at the chest's tier; capped by `max_stones_per_kill` /
  `max_gear_per_kill`. No stars, no killer, no loot-find affixes. The `.cfg` switches `Stone drops` and `Magic item
  drops` apply.
- **Per-container rules** (`drops.chests.containers`, IMP-79): keyed by container prefab name, the creature entry's
  shape - `tier`, `multiplier` (0 = this container never rolls), `stone_multiplier`, `gear_multiplier`, `bonus` rows.
  Not parsed yet (a spine request); until then every container uses its biome's tier and the flat chances.
- **Into the inventory**: pre-rolled gear (built exactly as creature gear, section 8) and stones in stacks of the
  stone's stack size go straight into the container, with their full data; the game's container save replicates
  them. What does not fit is lost, as the game's own default items are. (IMP-78.)
- Loot flung from a destroyed chest glows on the ground like any drop (display file); items inside a container never
  glow.

---

# 12. Elite Creatures Reborn - Phase 2, off by default

Ours-to-ours, config-gated, no hard dependency: when enabled and ECR is present, ECR's own star count replaces the
vanilla level in the star multiplier, and an ECR world tier can add a multiplier. **Specified in
`ecr-integration.md`** (2026-09-23), including one rule that applies whenever ECR is present, switch or not: ECR's
worthless creatures (the Cloven twin, Phantom husks) drop nothing from this mod. Built 2026-09-23, not tested in game;
the world-tier term is inert until ECR records a tier (DECISIONS ECR-6).

---

# 13. Precomputation (performance invariant)

At every economy-YAML apply, and once when the object database is complete on first spawn:

- per tier: the stone table (enabled stones only) as a cumulative weight array; the rarity rows (normal and boss)
  times `drop_weight`; the gear pool as a cumulative weight array of bases;
- per creature prefab: tier, boss flag, multipliers, bonus rows (from the boss map, overrides and spawn data);
- per base: its eligible affix pool per rarity at its ceiling, or at least whether it can fill each rarity.

At a kill: one dictionary lookup, a handful of random numbers, a binary search per drawn item, and the affix roll for
each gear item. Nothing reflective, nothing that allocates per kill beyond the spawned items.

---

# 14. Multiplayer

- Rolled on the creature's ZDO owner, once (often a nearby client on a dedicated server). Spawned as ordinary world items; the item's full data is in its ZDO the moment
  it exists, so a drop from a creature player A killed is intact when player B picks it up.
- Stone and gear tables are server-synced and lockable. A client that owns a creature (no dedicated server, or a
  zone it owns) rolls under the server's tables.
- Ground glow is spawned locally by each client from the item's replicated rarity (display file); no netcode here.

---

# Build checklist

- [ ] Owner-only death hook; own spawn path via the game's item-data drop; our rows never in the vanilla list
- [ ] Tamed deaths drop nothing; player-or-pet involvement required; pet hit flag on the creature ZDO
- [ ] Creature tier: boss map, override, spawn data, death-position biome
- [ ] Chance model with floor/fraction and caps; star multipliers
- [ ] Stone tables per tier, disabled stones removed
- [ ] Rarity rows times `drop_weight`; Mythic blocked
- [ ] Gear pool: tier window, no-recipe/DLC/quest exclusion, slot weights, base state
- [ ] Bosses: guaranteed stones and gear, bonus rows, boss rarity rows
- [ ] Everything precomputed at YAML apply and first spawn
- [x] Phase 2: loot-find stats (publish, killer read, Fateweaver, Norns' Favour, Trophy Taker, Hoardfinder) - built, needs in-game test
- [x] Phase 2: chests and world containers - built, needs in-game test (`containers` map pending the parser)
- 🔧 Phase 2: ECR hook (`ecr-integration.md`; world-tier term waiting on ECR-6)

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: owner wording, `ecf_ally_hit` key, open questions moved to `../DECISIONS.md`. | pending |
| 2026-09-23 | Phase 2: sections 10 (all four find affixes, owner-side clamp) and 11 (chest hook verified, `ecf_filled`, `containers` schema) specified and built. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Drops: DRP-1 to DRP-13; where the roll runs is
RC-7, the own death hook RC-12).

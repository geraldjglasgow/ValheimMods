# EliteCrafting - specification: Effects runtime

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **how an affix on an item becomes a change in the game**: the effect registry that YAML points
into, which items count, the one hidden aggregate status effect, the targeted patches, where values are summed and
capped, the performance rules, and which machine computes what. The affix catalog (which affixes exist, their
slots, tiers and which effect each uses) is `affixes.md`; the data it reads is `item-data.md`.

**Status: Phase 1 and Phase 2 built, not tested in game** (2026-09-24): the 34 Phase 1 effects, the aggregate and the item hooks;
Phase 2 added 77 effects (every easy and medium non-Mythic effect), kill attribution, two routed RPCs and the
per-player ZDO values in section 7 (`../DECISIONS.md` IMP-85-101, IMP-116-119).

---

# 1. Affix and effect

- An **affix** is a YAML entry: its own id, name, slots, tiers, weights (`configuration.md` has the schema).
- An **effect** is a code-registered implementation with its own id. An affix names exactly one effect and may give
  it a **parameter**: affixes `blade_mastery` and `axe_mastery` both use effect `skill_level`, with param `Swords`
  and `Axes`.
- Many affixes may share one effect. YAML can add new affixes freely, but **only over registered effects**: YAML
  never adds behaviour, it adds rollable combinations of behaviour the code already has.

## The registry

One static table in code, built at plugin Awake, one entry per effect:

| Field | Meaning |
|---|---|
| `id` | snake_case effect id, what YAML's `effect:` names |
| `route` | `aggregate` (summed into the status effect, section 3) or `hook` (a targeted patch, section 4) |
| `values` | which affix value types it accepts: any of `percent`, `flat`, `flag` |
| `param` | what the parameter is: `none`, `skill` (a `Skills.SkillType` name, a comma list, or `All`), `damage_type` (a type or the groups `physical`, `elemental`, `all`), `element`, `creature_family`, `resource`, `aura` (kinds and members defined in `affixes.md` section 3) |
| `polarity` | `raise` or `lower`: which way a positive stored value moves the stat; drives the sign in the tooltip (`display.md`) |
| `better` | `higher` (default) or `lower`: for the two effects where a low roll is the good one (`undying`, `hit_cap`), so display never marks it as weak |
| `cap` | default cap on the summed channel (section 5), or none |
| `phase` | 1, 2 or 3: effects of a later phase are registered but refuse to load in YAML until built (they are a validation error with the message "not implemented in this version") |

**YAML validation against the registry** (all are errors, so the affix file is rejected and the previous
configuration stays, `configuration.md`):

- `effect` is not a registered id, or is registered for a later phase.
- The affix's `value` type is not one the effect accepts.
- `param` missing when the effect needs one, present when it takes none, or not a valid member of its kind (a
  misspelled skill name).

The **authoritative list of effect ids** is the effect column of `affixes.md`. This file defines the mechanism and
the routing; where an id in this file's tables differs from `affixes.md`, `affixes.md` wins and this file is
corrected.

---

# 2. Which items count

- **Only equipped items contribute.** An affix in a backpack does nothing. Weapons and tools count while in hand,
  shields while equipped, armor, capes and utility items while worn. (Judgement call: it stops a bag of charms from
  stacking and matches how the vanilla stats of gear work.)
- **Only active affixes**: resolved to a live, enabled definition (`item-data.md` section 6). Dormant and unreadable
  entries are skipped.
- **Only the local player.** The aggregate is built for `Player.m_localPlayer` and nobody else. Other players'
  `Player` objects on this machine never get one: their stats run on their own machine (section 7).
- The `.cfg` master switch `Affix effects` (`configuration.md`) off means nothing counts: no aggregate status effect
  is added and every hook returns immediately. Display is unaffected.

---

# 3. The aggregate status effect

**One hidden status effect per local player carries every player-global stat.** The game's own status effect loop
then applies it at native cost, tick after tick, with no code of ours running per frame beyond a field read.

- A subclass of the game's `SE_Stats` (game notes Q7), created with `ScriptableObject.CreateInstance`. Its
  ScriptableObject `.name` is `ECF_Aggregate` (the game's `NameHash()` hashes `.name`, not `m_name`). No icon,
  `m_hidden = true`, `m_ttl = 0` (never expires), empty start/stop messages, so the HUD never lists it.
- Channels that `SE_Stats` already has a native field for (speed, carry weight, regen multipliers, the stamina-use
  modifiers, fall damage, noise, stealth, jump, parry bonus, stagger) are written into those fields on rebuild, so
  the game's own code applies them with its own semantics (for example half the speed bonus while swimming).
- `SE_Stats` has room for only two skill modifiers and categorical resist steps, so the subclass **overrides**
  `ModifySkillLevel`, `ModifyRaiseSkill` and `ModifyDamageMods` to read the mod's own per-skill and per-type tables.
  Each override is a table read; no allocation, no lookups by name.
- Added with `SEMan.AddStatusEffect(StatusEffect)`, which adds a shallow clone locally with no owner check. The mod
  keeps the clone and writes new values into it; lists are replaced, never mutated (the clone shares them with the
  template). It is never registered in `ObjectDB`.
- **Presence is maintained**, not assumed: `AddStatusEffect` returns null when one with the same hash exists, so
  the rebuild looks it up with `GetStatusEffect(hash)` and re-adds it only if missing. `Player.OnDeath` removes all
  status effects and respawn creates a new `Player`, so a postfix on `Player.OnSpawned` re-adds it.
- **Health-critical channels**: the rebuild prepares two sets of field values, without and with the conditional
  channels. The subclass's per-tick `UpdateStatusEffect` compares the player's health with the threshold and swaps
  sets only when the state flips: one comparison per tick, no allocation.

## When it is rebuilt

Rebuild = walk the local player's equipped items, sum active affixes into channels, clamp, write the totals.
A few items and a few affixes each, so it is cheap; it still runs **at most once per frame**, from a dirty flag.
When a rebuild changes a flat max-health, max-stamina or max-eitr total, it calls `Player.UpdateFood(0, true)` so the
pools resize at once instead of on the next food tick (at most once per second).

The flag is set by:

- A postfix on `Humanoid.SetupEquipment`, local player only. Both `EquipItem` and `UnequipItem` end there, and so
  does `HideHandItems` (swimming, eating), so weapon affixes correctly switch off while the hands are hidden.
- The local player's inventory `m_onChanged` (the game skips its own handler while the player is loading). Covers
  an equipped item leaving the inventory by any path (drop, death, another mod).
- `ItemRecord` writes to an item the local player has equipped (a stone applied to worn gear).
- `Player.OnSpawned` (first spawn, respawn after death).
- A successful apply of either YAML family, and a change of the `Affix effects` switch.

## Channels routed through the aggregate (Phase 1 core)

Consumers verified in the decompile (game notes Q7, 2026-09-23). Effect ids are `affixes.md` section 3's. "Field" means the native `SE_Stats` field the rebuild writes;
"override" means the subclass's own method.

| Effect id | Written to | Value written |
|---|---|---|
| `move_speed` | field `m_speedModifier` | total/100 |
| `carry_capacity` | field `m_addMaxCarryWeight` | total |
| `health_recovery` | field `m_healthRegenMultiplier` | 1 + total/100 |
| `stamina_recovery` | field `m_staminaRegenMultiplier` | 1 + total/100 |
| `eitr_recovery` | field `m_eitrRegenMultiplier` | 1 + total/100 |
| `run_stamina_cost` | field `m_runStaminaDrainModifier` (not the dead `m_runStaminaUseModifier`) | −total/100 |
| `jump_stamina_cost` | field `m_jumpStaminaUseModifier` | −total/100 |
| `attack_stamina_cost` | field `m_attackStaminaUseModifier` | −total/100 |
| `block_stamina_cost` | field `m_blockStaminaUseModifier` | −total/100 |
| `dodge_stamina_cost` | field `m_dodgeStaminaUseModifier` | −total/100 |
| `home_item_stamina_cost` | field `m_homeItemStaminaUseModifier` | −total/100 |
| `fall_damage_taken` | field `m_fallDamageModifier` | −total/100 |
| `noise_made` | field `m_noiseModifier` | −total/100 |
| `stealth` | field `m_stealthModifier` | total/100 |
| `jump_height` | field `m_jumpModifier` (y) | total/100 |
| `parry_bonus` | field `m_timedBlockBonus` | total/100 |
| `stagger_taken` | field `m_staggerModifier` | −total/100 |
| `skill_level` (param skill) | override `ModifySkillLevel` | level + total, per skill |
| `skill_gain` (param skill or `All`) | override `ModifyRaiseSkill` | value × (1 + total/100), per skill |
| `resist_modifier` (param element) | override `ModifyDamageMods` | one categorical step (the game's own "best wins") |
| `max_health`, `max_stamina`, `max_eitr` | postfix `Player.GetTotalFoodValue` (section 4) | + total |

This is the Phase 1-2 core of the aggregate route; `affixes.md` section 3 lists every effect and its route, and the
rest of its `aggregate` rows follow the same pattern (a native field where one exists, an override otherwise).
The exact sign and scale each field expects is checked against the decompile when built; the table records intent.

The overrides are a handful of arithmetic on cached floats; no allocation, no lookups.

Note for the catalog: the game clamps a skill's *factor* at level 100 (`Skills.GetSkillFactor`), so skill bonuses
above 100 raise the displayed level but change nothing that scales by factor. `affixes.md` should know this when
setting skill affix ranges.

---

# 4. Effects that need targeted patches

Anything that is **item-local** (describes the item itself) or **event-driven** (on hit, on kill, on block) does not
fit a status effect and goes through a prefix or postfix on the game method that computes it. Every such patch:

- reads the item's cached `ItemRecord` (`item-data.md` section 5), or the aggregate's totals, and nothing else;
- returns immediately when the item has no active affix of its concern (the common case: one CWT lookup);
- is a postfix unless a prefix is the only way; no transpilers in Phases 1-2 (workspace rule);
- is wrapped by `PatchGuard` so an exception names this mod in the log.

| Concern | Patch point (verified signature) | Notes |
|---|---|---|
| Max health / stamina / eitr (flat) | postfix `Player.GetTotalFoodValue(out hp, out stamina, out eitr)` | Aggregate totals; the game calls it every food tick |
| Item damage % and imbues, Honing | postfix `ItemData.GetDamage(int quality, float worldLevel)` | Also makes the vanilla tooltip numbers show the boost |
| Item armor %, Tempering | postfix `ItemData.GetArmor(int quality, float worldLevel)` | Also every frame while the inventory is open (character stats panel) |
| Block power %, Tempering on shields | postfix `ItemData.GetBaseBlockPower(int quality)` | |
| Durability % | postfix `ItemData.GetMaxDurability(int quality)` | **Hot**: the grid calls it every frame for every visible item |
| Item weight % | postfix `ItemData.GetWeight(int stackOverride)` | Hot-ish (encumbrance); CWT lookup only |
| Percent damage resist by type | prefix `Character.RPC_Damage(long sender, HitData hit)` for the local player, scaling `hit.m_damage` per type before the game's resist step | The game's own resists are categorical steps ("best wins"), so a percentage needs this; runs on the victim's own client |
| Attack fields (reach, arc), projectile, on-hit, on-kill, loot | `Attack` and drop-roll methods | Phase 2+, per `affixes.md` hook tags |

Honing and Tempering are not affixes but read the same record (`ecf_refine`, `quality.md`) through the same
patches, so there is one damage postfix and one armor postfix, not two of each.

---

# 5. Summing and capping

- **Channel** = `(effect id, param, condition)`. Two affixes with different ids but the same effect, param and
  condition feed the same channel and sum. A `health_critical` affix feeds its own channel, summed and capped apart
  from the unconditional one and switched on only while the player is at or below the threshold (`affixes.md`).
- **Sum** every active affix value in the channel across all counted items. Flag channels are OR (any copy on).
- **Cap** the sum: the YAML `caps:` map in the affix family (keys `effect`, `effect:param`, with `@health_critical`
  for the conditional channel) overrides the registry default; no entry and no default
  means uncapped. A cap is on the **sum**, never per item. Caps are for the effects that break the game past a point
  (movement speed, stamina cost reductions near 100%, fall damage). Default caps are judgement calls set with the
  catalog in `affixes.md`.
- **Order against vanilla**: the aggregate's `Modify*` calls are one status effect among the player's others; the
  game applies status effects in list order and each multiplies or adds on the current value. Percent channels are
  therefore multiplicative with other status effects and additive within the channel. Stated once so nobody
  "fixes" it per effect.
- **Item-local effects are not summed across items**: a weapon's damage affix changes that weapon's damage only.
- Totals are recomputed only on rebuild (section 3). Nothing sums per frame.

`ecraft stats` prints every non-zero channel with its raw sum, its cap and the applied value
(`console-commands.md`).

---

# 6. Performance invariants

From PLAN.md, made concrete. Each is an acceptance criterion; PatchGuard's Profiler run checks them before a
release.

1. **No per-frame parsing, reflection or allocation** in any patch or override. Records are cached; totals live in
   native fields and small index-addressed tables; overrides read them.
2. **Aggregation rebuild only on the events in section 3**, at most once per frame.
3. **Hot patches** (`GetDamage`, `GetWeight`, `GetArmor`, `GetMaxDurability`) do one CWT lookup, then return. They
   never touch YAML models, dictionaries or strings. Skill bonuses go through the aggregate's `ModifySkillLevel`,
   never a postfix on `Skills.GetSkillLevel` (called every frame for running).
4. **Channel indexes are assigned at YAML apply**, not looked up by name at runtime.
5. **Nothing runs on a dedicated server** for effects: no local player, no aggregate, and the item-local patches
   return on the empty record for anything the server touches.
6. The Profiler shows no EliteCrafting method among the top offenders in normal play (MP checklist,
   `multiplayer.md`).

---

# 7. Where each computation runs

| What | Runs on | Why it is correct |
|---|---|---|
| Aggregate stats (speed, regen, carry, stamina, skills) | the item holder's own client | The game computes a player's movement, regen and stamina on the machine that owns that player |
| Damage dealt (weapon affixes, Honing) | the attacker's client | The game builds `HitData` on the attacker and sends it; the boosted numbers travel inside it |
| Damage taken (armor, resists, Tempering) | the victim player's own client | Player damage is resolved on the player's owner |
| Loot-find effects (Phase 2) | the dying creature's ZDO owner (often a nearby client on a dedicated server) | Reads the killer's totals from a small per-player ZDO value the killer publishes (`drops.md`); the owner cannot inspect a remote inventory |
| Kill restore (Reaper, Soul Reaper, Phase 2) | seen on the dying creature's owner, applied on the killer's client | one routed RPC, `ECF_KillRestore`, to the killer's peer only (IMP-85) |
| Leech (Blood Drinker, Seidr Siphon, Cornered Thirst, Phase 2) | the attacker's client | computed from the outgoing hit, capped by the target's current health (IMP-86) |
| Evader's Fury trigger (Phase 2) | the attacker's peer decides the dodge; the dodger's client starts the window | routed RPC `ECF_MeleeDodged` (IMP-117) |
| Hamstring, stagger length, taming, sailing, gathering, summons (Phase 2) | the owner of the creature, ship, rock, tree or pickable | reads the acting player's published ZDO value below, or the status the hit carries (IMP-94-97) |
| Hearthlight, Mistbane (Phase 2) | every client | draws from the player's published ZDO value |
| Transient feedback (Phase 3 flashes, sounds) | object RPC to the peers that have the player instantiated | `multiplayer.md` |

Every peer computes the same result from the same replicated item data and the same server-synced YAML, so there is
nothing to reconcile and nothing to drift. The one thing a peer does **not** have is another player's equipped
items (`item-data.md` section 10); no Phase 1-2 effect needs them.

**ZDO keys.** Everything the mod writes into ZDOs, all small, all `ecf_`-prefixed (item data rides the item and is
not listed here; `item-data.md`):

| ZDO key | On | Type | Written by | Read by |
|---|---|---|---|---|
| `ecf_ally_hit` | creature | bool | the creature's owner, when a tamed or summoned ally hits it (Phase 1, `drops.md` section 2) | the creature's owner at death |
| `ecf_filled` | world container | bool | the container's owner, when the game fills it with default loot (Phase 2, `drops.md` section 11) | the same, so a container rolls once |
| `ecf_find_rarity`, `ecf_find_stones`, `ecf_find_trophy`, `ecf_find_coins` | player | float | the player's own client: capped loot-find totals (Norns' Favour, Fateweaver, Trophy Taker, Hoardfinder) | the dying creature's owner (`drops.md` section 10) |
| `ecf_daze` | player | float | the player's own client: Dazing Blows total | a staggered creature's owner (IMP-94) |
| `ecf_light` | player | float | the player's own client: Hearthlight on/radius | every client, which draws the light (IMP-100) |
| `ecf_demist` | player | float | the player's own client: Mistbane total | every client, which widens that player's demister |
| `ecf_taming` | player | float | the player's own client: Beast Whisperer total | the owner of a tameable creature in range (IMP-97) |
| `ecf_sail` | player | float | the player's own client: Fair Winds total | the ship's owner, for its helmsman |
| `ecf_yield_mining`, `ecf_yield_lumber` | player | float | the player's own client: Deep Vein, Heartwood | the owner of the rock or tree hit |
| `ecf_harvest` | player | float | the player's own client: Harvester | the owner of the picked pickable |
| `ecf_summon_damage`, `ecf_summon_health` | summoned creature | float | the summoner's client, at spawn | the summon's owner (its own damage and health) |

Player values are written by the player's own client (it owns its player ZDO) at the end of a rebuild, only when a
value changed, so normal play sends nothing. They are unconditional totals; each reader clamps to the running rules'
caps (IMP-73, IMP-96). A player without a key reads as 0.

**Routed RPCs** (registered with `ZRoutedRpc` at `ZNet.Awake` on every peer, argument-less, sent to one peer only):

| RPC | Sent by | To | Does |
|---|---|---|---|
| `ECF_KillRestore` | the dying creature's owner | the peer owning the killer's player ZDO (handled locally when that is itself) | the killer's client restores health / eitr / stamina from its own Reaper and Soul Reaper totals (IMP-85) |
| `ECF_MeleeDodged` | the attacker's peer, while a melee or area attack resolves against a dodging player | the dodger's peer | starts Evader's Fury (IMP-117) |

Neither carries data or is validated beyond "the local player is alive" (IMP-119).

**Status effects**: `ECF_Aggregate` (hidden, the local player only, never in ObjectDB); `ECF_Hamstring` (registered in
ObjectDB on every peer so a hit can carry its hash; the target's owner adds it, IMP-95); the HUD indicators
`ECF_Indicator_Fury`, `ECF_Indicator_Rhythm`, `ECF_Indicator_Ward` (local player only, never in ObjectDB, never sent;
IMP-88).

---

# 8. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Effects runtime: EFF-1 to EFF-4). Effect ids
follow `affixes.md` section 3 (RC-5).

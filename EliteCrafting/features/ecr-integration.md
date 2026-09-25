# EliteCrafting - specification: Elite Creatures Reborn integration

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the **optional synergy with Elite Creatures Reborn** (ECR), our own creature mod in this repository
(`../EliteCreaturesReborn/`): with it switched on, ECR's elite stars - and, once ECR builds them, its world tiers -
raise what EliteCrafting's drops pay. It is ours-to-ours (CLEANROOM.md: everything in the repository may be read),
**off by default, config-gated, and without a hard dependency**: EliteCrafting builds, loads and plays identically
with ECR absent. The drop model it modifies is `drops.md`; what ECR's stars and tiers are is ECR's
`features/scaling.md`, `features/pressure.md` and `features/world-tiers.md`.

Numbers are defaults and all of them are configurable in `drops.ecr` of `EliteCrafting_economy*.yml`. The judgement
calls are `../DECISIONS.md` ECR-1 to ECR-12.

**Status: built 2026-09-23, not tested in game** (code in `EliteCrafting/Loot/Ecr*.cs`, `Rules/Parsing/EcrDropParser.cs`,
`Commands/EcrCommand.cs`; DECISIONS.md IMP-80 to IMP-84). The world-tier terms are built but inert until ECR writes a
per-creature tier (ECR-6, waiting on the user). Ships in **Phase 2**. The key names below were read from ECR 3.4.0's
source in this repository on 2026-09-23 (`Traits/TraitKeys.cs`, `Traits/AspectStore.cs`; the aspect keys are in
ECR's uncommitted working tree that day and are re-checked when ECR commits them). Replaces the placeholder in
`drops.md` section 12.

---

# 1. What it does, and what it does not

Without this hook, ECR and EliteCrafting coexist but do not see each other's creatures: ECR keeps every creature at
the game's level 1 and scales it itself (ECR `scaling.md` section 1), so EliteCrafting's star multiplier, which reads
the game level, sees **0 stars on every ECR elite** and pays a five-star troll like a plain one (`drops.md` section 4).

With the synergy switched on and ECR present:

1. **ECR's star count replaces the game level** as the star input of EliteCrafting's chance model, on its own
   multiplier table (section 4). Both the stone roll and the gear roll use it, exactly where the vanilla star
   multiplier sits today; bosses' guaranteed counts use it too.
2. **ECR's world tier** multiplies the stone chance and shifts gear rarity (section 5) - **inert until ECR records a
   tier**, which it does not yet: ECR's world tiers are specified but not built (ECR `world-tiers.md`, "Status:
   specified, not built").
3. Optionally, stars shift gear **rarity** too (`star_rarity_bonus`, default 0).

Whether or not the synergy is on, when ECR is present:

4. **Creatures ECR marks worthless drop nothing from EliteCrafting** (section 6): ECR's Phantom husks and the Cloven
   twin, the boss copies ECR itself strips of loot. Without this a Cloven boss would pay our boss guarantees twice.

It does **not**: read ECR's mutations, attunements or boss aspects for drops (ECR's own principle, ECR `loot.md`
section 3: stars are the channel that says "this was harder"); change what ECR drops; touch ECR's creatures, config or
files; reference ECR's assembly or types.

---

# 2. Detection

- **By plugin GUID, through BepInEx's chainloader**: ECR is present when
  `BepInEx.Bootstrap.Chainloader.PluginInfos` contains **`gglasgow.elitecreaturesreborn`** (ECR `PluginInfo.Guid`,
  documented there as permanent). The version is read from the same entry for the log and `ecraft ecr`.
- **When**: once, lazily, at the first `ZNetScene.Awake` of the process - by then the chainloader has loaded every
  plugin, so load order between the two mods does not matter. The result is cached for the process.
- **No `BepInDependency` attribute**, not even a soft one, and no mention in the Thunderstore manifest: the hook needs
  no load order, and a manifest dependency would make the store install ECR alongside.
- **No reflection into ECR and no ECR types**: everything the hook needs is on the creature's ZDO, read by string key
  (section 3). A renamed ECR class can never break EliteCrafting; a renamed key degrades to "no ECR data" (section 8).
- **Local, per peer.** Each peer checks its own install. ECR is itself required on the server and every client (ECR
  README, "Install it on the dedicated server as well as the clients"), so in a correct install every peer agrees; a
  peer without ECR simply rolls as if it were absent (section 9).
- Logged once at startup of the hook: "Elite Creatures Reborn <version> found; synergy on/off".

---

# 3. What EliteCrafting reads

All from the **dying creature's own ZDO**, on the peer that rolls its loot (the creature's ZDO owner, RC-7). ECR's
owner wrote them when it rolled the creature; the game replicates them to every peer.

| Key (exact) | Type | Written by ECR | EliteCrafting uses it for |
|---|---|---|---|
| `ecr_resolved` | bool | `TraitStore.Save`, once the creature's traits are rolled | the creature has ECR data at all; without it the ECR star term is not used |
| `ecr_stars` | int, 0 to ECR's ceiling (5 by default, no enforced maximum) | `TraitStore.Save` | the star input (section 4), when the synergy is on |
| `ecr_asp_worthless` | bool | `AspectStore.MarkTwin` (Cloven) and `AspectStore.MarkHusk` (Phantom) | drop nothing (section 6), whenever ECR is present |
| `ecr_tier` | int 0-7 | **not written by ECR yet - proposed** (section 5) | the world-tier terms, when the synergy is on |

Read nowhere: `ecr_mask` (mutations), `ecr_biome` (ECR's roll biome; our tier comes from our own spawn data,
`drops.md` section 3), `ecr_gen` (splinter generation, section 7), `ecr_aspect` and the other aspect keys, the
devour and thieving keys.

All reads are the game's string-keyed ZDO getters: two to four lookups per qualifying death. No per-frame cost, no
cache needed.

---

# 4. Stars

```
stars = synergy on and ECR present and ecr_resolved ? ecr_stars : (game level - 1)     (as today)
S     = synergy on and ECR data used ? drops.ecr.star_multipliers[stars] : drops.star_multipliers[stars]
p     = base_chance[tier] * S * creature_multiplier (* Fateweaver, stones) (* T_stone, stones - section 5)
```

- **Our own table for ECR stars**, not the vanilla one: ECR elites are far commoner than vanilla stars (ECR's fallback
  star chances are 73% none, 10% one, 10% two, 5% three, 1% four, 1% five), so the vanilla `[1, 2, 3]` would pay about
  1.5x on average everywhere. The default follows ECR's own reasoning that one star is too common to pay for (ECR
  `scaling.md` section 1, "Loot does not pay until the second star"):

| ECR stars | 0 | 1 | 2 | 3 | 4 | 5 | each beyond |
|---|---|---|---|---|---|---|---|
| multiplier | 1 | 1 | 1.5 | 2 | 2.5 | 3 | +0.5 |

  Averaged over ECR's fallback star chances that is about **x1.14** - a nudge toward fighting elites, not a second
  economy (ECR-4).
- **Both rolls** - stones and gear chance - take `S`, as the vanilla star multiplier does (`drops.md` section 4). The
  per-kill caps (`max_stones_per_kill`, `max_gear_per_kill`) still apply, so a twenty-star creature on a server with
  no star ceiling pays at most the caps.
- **Bosses**: ECR rolls boss stars on its own boss table (90/6/3/1). The boss's `ecr_stars` feeds the same `S`, which
  multiplies the boss's guaranteed counts as stars already do (`drops.md` section 9). Bonus rows stay unscaled
  (IMP-44).
- **Star rarity bonus** (`star_rarity_bonus`, percent per ECR star, **default 0**): when above 0, every gear rarity
  weight above Uncommon is multiplied by `1 + (bonus x stars)/100`, added to Norns' Favour's percentage before the
  multiply (`drops.md` section 10), so the two bonuses sum rather than compound. Default 0 because ECR's design pays
  stars in quantity, not quality; it is here because PLAN.md names gear rarity as a possible synergy (ECR-5).
- **ECR data missing** on a creature (`ecr_resolved` false - it died in the frame it spawned, or it is a creature ECR
  does not manage): the vanilla path, `game level - 1`, which with ECR installed is 0.

---

# 5. World tier - inert until ECR records one

ECR's world tier (0 to 7, one number for the world, ECR `world-tiers.md`) is **not built in ECR 3.4.0**: no key, no
value, no API. The synergy is specified now so it needs no second design pass, and ships inert.

**Proposed contract (ECR-6, BLOCKING for this term only)**: when ECR builds world tiers, its `TraitStore.Save` also
writes **`ecr_tier`** (int, the world tier the creature was rolled at) on the creature's ZDO, next to `ecr_stars`.

Why on the creature rather than a world-wide value:

- ECR's own rule is that **creatures alive are not re-rolled** when the tier advances - the tier decides what the
  *next* creature rolls (ECR `world-tiers.md` section 5). The tier a creature was rolled at is therefore the honest
  measure of how hard it is.
- Under ECR's **Personal** source each client has its own tier; a creature rolled by one owner and killed on another
  peer would otherwise read the wrong player's tier. A value on the creature is the same on every peer.
- It costs ECR one int per creature and EliteCrafting one ZDO read per death; no RPC, no global key.

With `ecr_tier` present and the synergy on:

```
T_stone = drops.ecr.tier_stone_multipliers[tier]          multiplies the stone chance only
R_tier  = drops.ecr.tier_rarity_bonus[tier]               percent, added to Norns' Favour and the star bonus
```

| ECR world tier | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|---|
| `tier_stone_multipliers` | 1 | 1.1 | 1.2 | 1.3 | 1.4 | 1.5 | 1.6 | 1.7 |
| `tier_rarity_bonus` (%) | 0 | 5 | 10 | 15 | 20 | 25 | 30 | 35 |

- PLAN.md: "ECR elites/world tiers multiply stone drop chance". The gear chance is left alone - a mature world pays
  more currency and better gear, not more gear (ECR-6).
- Lists may be shorter than 8; an index past the end reuses the last entry (ECR's own convention for its lines).
- **Until ECR writes the key**, `ecr_tier` reads as absent and both terms are 1 and 0. Nothing to switch on later: the
  day an ECR release writes it, the terms start working on creatures rolled from then on.
- **If ECR picks another name** or a world-wide value instead, only the reader in section 3 changes; the YAML and
  the math do not.

---

# 6. Worthless creatures

ECR marks some creatures as worth nothing: the second half of a **Cloven** boss (a copy of the boss prefab) and
**Phantom husks** (`ecr_asp_worthless`, ECR `boss-aspects.md`). ECR clears their vanilla drop list itself
(`Patches/LootPatch.cs`); our drops are spawned by our own death hook (RC-12), so ECR's clear does not reach them.

- **Whenever ECR is present, a creature whose ZDO has `ecr_asp_worthless` true drops nothing from EliteCrafting** -
  no stones, no gear, no bonus rows, no boss guarantees - **whether or not the synergy is switched on** (ECR-7). This
  is not a bonus; it prevents a Cloven Moder from paying our boss guarantees twice. The check sits with the other
  "which deaths drop" rules (`drops.md` section 2) as a fifth rule.
- `drops.ecr.skip_worthless` (default true) turns it off for an owner who wants the twin to pay.
- ECR absent: never read (a stale key on a creature from an old ECR install is ignored, section 8).

---

# 7. Edge cases

| Situation | Result |
|---|---|
| ECR absent | nothing in this file runs; drops exactly as `drops.md` |
| ECR present, synergy off (default) | stars 0 on every ECR creature (game level 1), as today; worthless creatures drop nothing |
| ECR present, synergy on | ECR stars through `drops.ecr.star_multipliers`; tier terms if `ecr_tier` exists |
| Creature killed before ECR resolved it (`ecr_resolved` false) | vanilla star path (0 stars) |
| `ecr_stars` negative or unreadable | treated as 0 |
| `ecr_stars` above the table | last entry plus `star_step` per extra star; the per-kill caps bound the result |
| Splintering copies (`ecr_gen` above 0) | ordinary creatures with their own stars; each copy killed rolls our drops like any creature (ECR-9). A five-star cascade makes about nine copies, rarely (ECR `mutations.md`); the caps and the player-involvement rule apply to each |
| A devoured creature (killed by an ECR devourer, no player hit) | no drops: the player-involvement rule already refuses (`drops.md` section 2) |
| Tamed ECR creature | no drops (`drops.tamed: false`) |
| Thieving pouch | ECR's own; never touched |
| Boss aspects other than worthless (loot multipliers in ECR) | not read; our boss rows are our own (ECR-8) |
| ECR mutations and attunements | not read (ECR-8) |
| A creature ECR does not manage in a world with ECR | no `ecr_resolved`: vanilla path |
| Chest loot (Phase 2) | no creature, no stars; unaffected. The world-tier term does not apply to chests either (ECR-10) |

---

# 8. When ECR is absent, removed, or changes its keys

- **Absent at startup**: the hook never reads a key. Nothing is logged beyond one line at debug level.
- **Removed from a world that had it**: creatures in the saved world still carry `ecr_*` keys, but ECR's scaling is
  gone with the plugin, so those stars mean nothing now. The detection gate (section 2) is the plugin, not the keys,
  so the stale keys are ignored.
- **Added to an existing world**: works from the next creature ECR rolls.
- **ECR renames or drops a key**: reads return the getter defaults (false, 0), so creatures look unresolved and the
  vanilla path runs - drops stay correct, the synergy just stops paying. To make that visible: after **20 qualifying
  deaths in a row** on this peer with ECR present, the synergy on and no `ecr_resolved`, one warning is logged
  ("Elite Creatures Reborn is loaded but no creature carries its star data; its keys may have changed - the synergy
  is paying nothing"), once per session (ECR-11). `ecraft ecr` shows the same.
- **Compatibility is by key, not by version**: no version range is enforced. The keys above are the contract; the
  version is logged for bug reports. When ECR changes a key, both mods are in this repository and the change is made
  in both (a comment on ECR's `TraitKeys` naming EliteCrafting as a reader is part of building this hook).

---

# 9. Multiplayer

- The roll runs on the **dying creature's ZDO owner**, once (RC-7) - the same peer where ECR rolled or last owned the
  creature. The ECR keys are on that creature's ZDO and replicated, so the owner always has them; no RPC, no request
  to the server.
- The **synergy switch is synced** (a Charter clause): every owner rolls with the server's choice. The `drops.ecr`
  numbers are part of the economy YAML, synced and lockable like the rest.
- **ECR presence is local**: a peer without ECR rolls as if ECR were absent (vanilla path, no worthless check). That
  only happens in an install ECR itself does not support (ECR must be on every peer), so it is logged, not prevented.
- Nothing is written by this hook: no keys of ours on ECR's creatures beyond the existing `ecf_ally_hit`.
- Works the same on a dedicated server, a hosted game and single player.

---

# 10. Configuration

## `.cfg` (addition to `configuration.md` section 2)

| Section | Key | Type | Default | Synced | Meaning |
|---|---|---|---|---|---|
| `9 - Elite Creatures Reborn` | `Synergy` | bool | `false` | synced | With Elite Creatures Reborn installed: its elite stars (and later its world tier) raise EliteCrafting's drops. No effect without it |

The switch is the gate PLAN.md asks for; the numbers are YAML (CFG-6).

## YAML: `drops.ecr`

| Field | Type | Default | Meaning |
|---|---|---|---|
| `star_multipliers` | list of numbers, index = ECR stars | `[1, 1, 1.5, 2, 2.5, 3]` | replaces `drops.star_multipliers` for ECR-resolved creatures |
| `star_step` | number | 0.5 | added per star beyond the list |
| `star_rarity_bonus` | percent per star | 0 | gear rarity shift per ECR star (section 4) |
| `tier_stone_multipliers` | list, index = ECR world tier 0-7 | `[1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7]` | stone chance multiplier (section 5) |
| `tier_rarity_bonus` | list of percents, index = world tier | `[0, 5, 10, 15, 20, 25, 30, 35]` | gear rarity shift (section 5) |
| `skip_worthless` | bool | true | ECR's worthless creatures drop nothing from us (section 6); applies whenever ECR is present |

Merging: scalars take the last value read; the lists are replaced whole (ECO-1). Validation: every number at least 0;
lists non-empty and at most 64 entries (error); `tier_*` lists longer than 8 (warning - ECR's tiers stop at 7).

```yaml
drops:
  ecr:                        # read only with Elite Creatures Reborn installed
    star_multipliers: [1, 1, 1.5, 2, 2.5, 3]
    star_step: 0.5
    star_rarity_bonus: 0
    tier_stone_multipliers: [1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7]
    tier_rarity_bonus:      [0, 5, 10, 15, 20, 25, 30, 35]
    skip_worthless: true
```

Worked example: a server that wants elites to pay as vanilla stars do, and better gear from three stars up:

```yaml
drops:
  ecr:
    star_multipliers: [1, 2, 3]
    star_step: 1
    star_rarity_bonus: 5          # a five-star elite: +25% on every rarity above Uncommon
```

---

# 11. `ecraft ecr`

A read-only sub-command (Phase 2, for `console-commands.md` when built; read-only commands follow `Read-only commands
for everyone`):

```
ecraft ecr
  Elite Creatures Reborn 3.4.0 found (gglasgow.elitecreaturesreborn); synergy ON (server)
  world tier: not recorded by this ECR version
  hovered: Troll  ecr_resolved=true  ecr_stars=3  worthless=no  ecr_tier=-
  stars 3 -> x2.0 (drops.ecr); tier term x1.0; rarity bonus +0%
  last 20 deaths: 17 with ECR data
```

With nothing hovered, the first three lines. It uses `LootPreview.Explain`'s existing path for the hovered creature,
extended with the ECR terms, so `ecraft ecr` and the drop roll can never disagree.

---

# 12. Performance

At a death with ECR present: one extra bool read (worthless); with the synergy on, two or three more ZDO reads and a
table index. The multiplier tables are precomputed at each economy apply with the rest of the drop tables
(`drops.md` section 13). Nothing per frame.

---

# Build checklist

(🔧 = code done and checked by build and a throwaway harness over the real defaults, not yet seen in game.)

- 🔧 Detection by GUID at the first `ZNetScene.Awake`, cached; version logged; no attribute, no reflection
- 🔧 Worthless check as a "which deaths drop" rule whenever ECR is present (`skip_worthless`)
- 🔧 ECR stars replace the game level for resolved creatures when the synergy is on; own table and step
- 🔧 Star rarity bonus summed with Norns' Favour
- 🔧 `ecr_tier` reader and the two tier terms, inert while the key is absent (the key name waits on ECR-6)
- 🔧 Missing-data warning after 20 deaths; `ecraft ecr`
- 🔧 `.cfg` `9 - Elite Creatures Reborn / Synergy`; `drops.ecr` read, merged, validated
- [ ] A comment on ECR's `Traits/TraitKeys.cs` (and `AspectStore`'s worthless key) naming EliteCrafting as a reader -
      an ECR-side edit, left for a session that works on ECR
- [ ] Seen on a dedicated server with both mods: a 3-star ECR troll pays x2 with the synergy on, x1 off; a Cloven twin
      drops nothing either way; with ECR removed, nothing changes

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified (Phase 2 spec pass); key names read from ECR 3.4.0 source. | pending |
| 2026-09-23 | Built: detection, worthless rule, ECR star table, rarity bonus, inert tier terms, missing-data warning, `ecraft ecr`, `.cfg` switch, `drops.ecr`. `ecr_asp_worthless` re-checked in ECR's working tree (`AspectStore`, still uncommitted). | pending |

---

# Decisions

Every judgement call in this file is in `../DECISIONS.md` (Elite Creatures Reborn: ECR-1 to ECR-12). ECR-6 - the
`ecr_tier` key ECR would write when it builds world tiers - needs the user and blocks only the world-tier term.

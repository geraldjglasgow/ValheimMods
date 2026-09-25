# EliteCrafting — PLAN.md

Path-of-Exile-2-style currency crafting and ARPG item affixes for Valheim. Creatures drop
crafting stones; stones are clicked onto gear to grant, reroll, corrupt, and perfect magical
affixes across six rarities. Everything data-driven, server-synced, hot-reloaded.

- **Status (2026-09-24):** Phase 0 ✅ done. Phase 1 (0.1.0) and Phase 2 (0.2.0) 🔧 **code complete, integrated and
  reviewed, not yet tested in game** — what remains is in-game verification and packaging (👤, listed under each
  phase). Phase 3 ☐ not started, blocked on AFX-8.
  The **Phases** section below is the live checklist: ✅ done · 🔧 code done, needs in-game test · ⏳ in
  progress · ☐ not started. Where this file and `SPEC.md`/`features/` differ, the spec wins.
- **First release target:** 0.1.0 (Phase 1 below).
- **Identity:** folder/project/DLL `EliteCrafting`, GUID `com.EliteCrafting`, item/custom-data key
  prefix `ecf_`, console command `ecraft`, YAML families `EliteCrafting_affixes*.yml` and
  `EliteCrafting_economy*.yml`. Own names everywhere, per CLEANROOM.md.

## Read first (implementing agents)

1. `/CLEANROOM.md` — the boundary. Non-negotiable. Never read another mod's code, config, or data.
2. `/CLAUDE.md` (workspace) — layout, libraries, build, release, small-units rule (methods ≤ 24 lines,
   classes ≤ 300), multiplayer-first rule.
3. This file, then `SPEC.md` + `features/` once Phase 0 writes them.
4. Raw affix behaviour pool: `~/scratch/specs/affix-pool-draft.md` — **outside the repo on purpose.**
   It contains behaviour descriptions gathered from public documentation and brainstorming. Import
   behaviours from it, but every effect gets our own id and display name during Phase 0; the pool's
   batch-1 names are placeholders and must not appear in code, YAML, or localization.
5. Game signatures: decompile only `assembly_valheim.dll` with ilspycmd into `~/scratch/decomp/`.
   Build with `~/build.sh EliteCrafting` on homelab01.

## Pillars

1. **Fully multiplayer.** Dedicated server first. Item state rides the game's own item
   serialization; rolls happen on the authoritative peer; rules are server-synced and lockable.
2. **Very configurable.** Rarities, affixes, tiers, weights, stones, drop tables, costs — all YAML.
   Server owners can rebalance, disable, or extend without code changes.
3. **Performant.** Nothing per-frame. Parse once per item instance, aggregate on equip change,
   let the game's own status-effect system do the per-tick work.
4. **Slot sanity.** Affixes only roll where they make sense: no armor stats on weapons, no weapon
   stats on armor (user directive 2026-09-23).

## Rarity ladder (user decision 2026-09-23: six tiers)

| Rarity | Affixes | Color (canonical, user 2026-09-23) | Acquisition (default) |
|---|---|---|---|
| Common | 0 | white `#FFFFFF`, **never glows** | vanilla |
| Uncommon | 1–2 | green `#1EFF00` | drops + crafting |
| Rare | 2–3 | blue `#0070DD` | drops + crafting |
| Epic | 3–4 | purple `#A335EE` | drops + crafting |
| Legendary | 4–5 | orange `#FF8000` | rare drops + crafting |
| Mythic | 6 + Mythic-only affix pool | red `#E6262E` | **craft-only** (Stone of Apotheosis) |

- The hex palette is the single source for every rarity-colored surface: item names, tooltip
  accents, and the ground glow below. It lives in the rarity definitions in
  `EliteCrafting_economy*.yml`, so owners can retheme it in one place.

- Rarity = affix **count**. Affix **tier** (strength) is a separate axis gated by biome/world
  progression: a Meadows Mythic rolls many weak affixes; an Ashlands Uncommon rolls few strong ones.
- Mythic is different in kind: exclusive access to a small pool of build-defining effects
  (auras, Undying, Bifrost Blessing, Blink, …). Craft-only by default; YAML can open drops.
- Ladder is data: tier count, names, colors, affix ranges all live in `EliteCrafting_economy*.yml`.

## Affix system

- Pool of ~170 effects (see scratch draft; grows over time). Each affix definition carries:
  own `id`, display name, **slots**, category (`offense` / `defense` / `utility`), value type,
  tiers (range + biome gate + weight per tier), exclusion group, hook-difficulty tag, and for
  conditional variants the condition (e.g. health-critical, default threshold 30%).
- **Slot taxonomy:** `melee_weapon`, `ranged_weapon`, `magic_weapon`, `shield`, `head`, `chest`,
  `legs`, `cape`, `utility_item`, `tool`. An affix lists the slots it can roll on.
- **Slot sanity rules** (Phase 0 assigns the full matrix from these):
  - Item-local stats stay on the item they describe: attack speed, imbues, reach, arc, draw
    speed, durability → weapons/tools; block/parry → shields (and dual-purpose weapons that block);
    armor % → armor pieces.
  - Player-global offense (damage vs family, sneak attacks, low-health openers) → weapons only.
  - Player-global defense, regen, resists, movement → armor pieces (spread across head/chest/legs/cape
    so stacking requires slot choices).
  - Utility (carrying capacity, pickup, discovery, comfort) → armor + utility items.
  - Tool effects (build range, stationless building, mining/lumber yield) → tools only.
  - Skill affixes roll only on items governed by that skill (sword skill on swords, …).
- No prefix/suffix split (decision, arguable later): exclusion groups + per-slot pools give the
  same anti-stacking control with less player-facing complexity; Sigils steer by category instead.
- Multiple copies of the same affix never roll on one item (exclusion by id), and exclusion
  groups block near-duplicates (e.g. the three movement-speed variants).

## Currency catalog — stones (PoE2-informed; "stone" concept locked 2026-09-23)

All stones: stackable (default 50), light (default 0.2), tradeable, drop as world items, defined
in `EliteCrafting_economy*.yml`. Every stone declares `applies_to` (rarities), per-rarity cost
(default 1), optional `tier_floor` for what it rolls, enable flag, and drop-table entries.
Server owners can add new stones in YAML without code (behaviors compose from a fixed verb set:
promote / add / swap / reroll_affixes / reroll_values / remove / strip / corrupt / lock / duplicate
/ gamble / quality).

### Ascension stones — the ladder backbone

| Stone | Effect | PoE2 analog | Default drop range |
|---|---|---|---|
| Stone of Awakening | Common → Uncommon | Transmutation | Meadows+ |
| Stone of Ascension | Uncommon → Rare | Regal (generalized) | Black Forest+ |
| Stone of Exaltation | Rare → Epic | — (ladder extension) | Mountain+ |
| Stone of Transcendence | Epic → Legendary | — (ladder extension) | Mistlands+ |
| Stone of Apotheosis | Legendary → Mythic | — (the chase item) | Ashlands, very rare |

Promotion keeps existing affixes and rolls new ones up to the new tier's minimum.

### Manipulation stones — two grades each (Lesser: Uncommon/Rare; Greater: Epic/Legendary/Mythic)

| Stone | Effect | PoE2 analog |
|---|---|---|
| Stone of Growth | add one affix, up to tier max | Exalted / Augmentation |
| Stone of Turmoil | remove one random affix, add one new | Chaos (PoE2 swap behavior) |
| Stone of Upheaval | reroll **all** affixes, keep rarity | PoE1 Chaos (kept: our ladder rewards it) |
| Stone of Perfection | reroll numeric values only | Divine |
| Stone of Severing | remove one random affix | Annulment |
| Stone of Unmaking | strip to Common (one grade, universal) | — (recraft QoL; quality survives) |

### Risk and endgame stones

| Stone | Effect | PoE2 analog |
|---|---|---|
| Serpent Stone | corrupt: random outcome from a weighted table, then the item is **sealed** (no further stones ever). Defaults: 25% seal only, 25% add affix (may exceed cap by one), 20% chaotic full reroll (ignores biome tier floors, any tiers), 15% promote one rarity (on Mythic: adds a 7th affix), 15% demote one rarity and lose an affix. Weights in YAML. | Vaal Orb |
| Stone of Binding | lock one random affix permanently: survives Turmoil/Upheaval/Severing/Perfection; only Unmaking or the Serpent remove it. Once per item. | Fracturing Orb |
| Stone of Chance | consume on a Common: transforms it to a random rarity, weighted (default weights favor Uncommon/Rare; Mythic weight 0, consistent with craft-only Mythic). Item is never destroyed. | Orb of Chance (adapted — no uniques yet) |
| Stone of Reflection | duplicate an item, affixes and quality included; the copy is sealed. Astronomically rare by default; YAML can disable or retune. | Mirror of Kalandra |

### Sigils — meta-currency (PoE2 Omens)

Applied to an item first; shown in the tooltip as pending; consumed by the **next** stone used on
that item, steering its randomness. One pending sigil per item. Ship five:

| Sigil | Steers |
|---|---|
| Sigil of Preservation | reroll-type stones leave the highest-tier affix untouched |
| Sigil of War | the next added affix rolls from the offense category |
| Sigil of Warding | the next added affix rolls from the defense category |
| Sigil of Fortune | the next added affix rolls from the utility category |
| Sigil of Culling | the next removal removes the lowest-tier affix instead of random |

### Quality stones (independent of the affix system; survive Unmaking)

| Stone | Effect | PoE2 analog |
|---|---|---|
| Honing Stone | weapon: +1% damage per use, default cap +10% | Blacksmith's Whetstone |
| Tempering Stone | armor/shield: +1% armor (block) per use, default cap +10% | Armourer's Scrap |

### Essences — Phase 2

Per-biome families (e.g. swamp → poison-family, mountain → frost-family). Behavior: reroll an
Uncommon+ item with one guaranteed affix from the essence's family; Greater essences carry a
`tier_floor`. The `tier_floor` knob is the general home for PoE2's Greater/Perfect currency idea —
no separate "Flawless" item variants in v1.

Prefab count v1–v2: 5 + 11 + 4 + 5 + 2 = **27 stones**, plus essences in Phase 2.

## Acquisition and economy

- Stones drop from creatures and (Phase 2) chests: weighted by biome, creature type, and star
  level; bosses guarantee drops. All in `EliteCrafting_economy*.yml`.
- Stones are drop/trade-only in v1 — no crafting recipes for stones themselves (a YAML recipe
  knob can come later if wanted).
- **Drop model (user-confirmed 2026-09-23):** creatures/chests also drop pre-rolled magic
  gear, Uncommon–Legendary, weights per biome; Mythic never drops. Alternatives recorded in
  Decisions log (currency-only; uncapped).
- Affixes that feed the economy: Norns' Favour (higher-rarity find) and Fateweaver (extra stone drops)
  read the **killer's** gear — drop rolls run on the dying creature's ZDO owner, so the killer's
  relevant totals travel as a small synced player stat, not by remote inventory inspection.
- Phase 2: salvage — grind a magic item into stone fragments so dead drops have value
  (our own mechanic; no overlap with OpenKeep's vanilla-item salvage).
- Optional, config-gated, Phase 2+: Elite Creatures Reborn synergy — ECR elites/world tiers
  multiply stone drop chance. Ours-to-ours integration, off by default, no hard dependency.

## Applying stones — UX

- Pick up the stone stack, click it onto the target item in the inventory (InventoryGui patch).
  One stone consumed per use (per-rarity costs may consume more).
- Stones apply only to items in the player's **own inventory** — not to items sitting in an open
  container (avoids container-ownership races in multiplayer; move it over first).
- Refusals never consume: wrong rarity band, affixes full (Growth), at tier minimum (Severing),
  sealed item, pending-sigil conflicts, disabled stone, target not in own inventory. Each refusal
  shows a short message.
- Destructive stones (Serpent, Unmaking, Reflection has no downside but is precious — Serpent and
  Unmaking only) require a confirm: default hold-Shift to apply; `.cfg` can switch to a dialog or off.
- Display: rarity-colored item names, tooltip affix block (affix name, value, tier), quality line,
  sealed marker, pending sigil line. Verbosity/toggles are client-side unsynced preferences; the
  rarity palette itself comes from the synced rarity definitions.
- **Ground glow (Phase 1):** a magic item lying in the world glows in its rarity color — a small
  non-shadow point light attached to the item's world object, spawned by each client locally from
  the item's replicated rarity (no netcode). Common never glows. Applies to creature drops,
  player-dropped items, and loot flung from chests alike. On/off, intensity, and range are client
  preferences; an optional soft loot-beam variant is Phase 3 polish. Stones don't need it — their
  tinted emissive models carry their visibility — but a config can opt them into the same system.
- Equipped items may be modified (default on, synced setting).

## Architecture

- **Item state** lives in `ItemDrop.ItemData.m_customData` under `ecf_` keys (format version,
  rarity, affix list as `id:tier:value`, quality, sealed, binding, pending sigil). The game
  serializes and replicates custom data everywhere (inventory, containers, dropped items,
  tombstones, world saves) — item state needs **no custom netcode**. The format-version key lets
  later releases migrate old items safely instead of corrupting them.
- **Parse cache:** custom data parsed once per ItemData into an affix record, cached in a
  ConditionalWeakTable; invalidated on write.
- **Effect application:**
  - Aggregate player stats (regen, max pools, speed, carry weight, resists, fall damage, …) into
    one hidden always-on status effect per player, rebuilt on equip/unequip/inventory change —
    the game's own SE machinery applies it per tick at native cost.
  - Item-local and event effects use targeted prefix/postfix patches (GetDamage, attack fields,
    block calc, projectile spawn, skill level query, loot roll, …), each reading the cached parse.
  - An **effect registry** maps affix id → implementation; YAML can only reference registered
    effect ids (unknown ids are a YAML validation error, so typos surface at load, not in combat).
  - Every effect entry in the pool draft carries a hook-difficulty tag (easy/medium/hard);
    implement easy first, verify each hook signature against a fresh decompile.
- **Where things happen (multiplayer):**
  - Creature loot rolls: on the dying creature's ZDO owner (often a nearby client on a dedicated
    server; DECISIONS.md RC-7).
  - Stone application: on the client that owns the item (Valheim's inventory trust model);
    the *rules* it rolls under are server-synced and lockable, so clients cannot change outcomes' odds.
  - Effects: computed locally on every client from the item's replicated data — deterministic,
    everyone agrees, no drift.
  - Transient feedback (corruption flash, sounds): RPCs scoped to peers who can see the player.
- **Configuration:** Charter + ConfigReload for the `.cfg`; the two YAML families through the mod's
  own rule reader on YamlDotNet (overlay over built-in defaults, reload poll, one Charter article per
  family), Elite Creatures Reborn's pattern (features/configuration.md). Synced, lockable,
  hot-reloaded; no in-game editor in v1. Client display preferences unsynced.
- **Stone prefabs and world models:** cloned vanilla item bases registered into ObjectDB +
  ZNetScene identically on every peer (patched at DB load so join-in-progress works). **Mod
  required on server and all clients** (manifest + README say so).
  - **3D models v1 — no asset authoring needed.** A cloned prefab keeps its base's mesh, so
    dropped stones are visible in the world immediately. Distinction comes from: (a) different
    vanilla bases per stone group (gem-like for ascension, core-like glowing bases for risk
    stones, flat rune-like for sigils — the final base list is a Phase 0 task, chosen by
    surveying prefab names in the decompile, never another mod), (b) runtime material re-tint
    per stone family (color + emission on the clone's own material instance), (c) size scale
    per grade, (d) reused vanilla glow/particle effects for the rare ones. Nothing of Iron
    Gate's ships in our zip — the game supplies mesh and texture at runtime on every client.
  - The dedicated server renders nothing; each client instantiates its local registered clone,
    so visuals stay consistent across peers for free once registration is deterministic.
  - **Icons v1:** runtime-tinted vanilla sprites, matching the world-model tint. Original icon
    art later.
  - **Phase 3 upgrade path:** our own meshes, either authored in Unity/Blender on the Mac and
    shipped as our own AssetBundle embedded in the DLL (our art — clean-room fine), or generated
    procedurally in code (low-poly crystals). Either slots in without data or key changes.
- **Libraries:** Charter, ConfigReload, PatchGuard, YamlDotNet (+ Profiler in dev) — no YamlConfig/SyncedConfig (decision 2026-09-23).
  TraitSets not used (items carry custom data, not ZDO trait masks). Prefab registration is a
  ValheimModLibs extraction candidate once a second mod needs it — keep it in-mod until then.
- **Layout:** standard mod layout from workspace CLAUDE.md (copy ShipConfig skeleton, rename).
  Suggested source folders: `Items/` (prefab cloning, registration), `Affixes/` (definitions,
  registry, parse/serialize), `Effects/` (hook implementations, aggregation SE), `Stones/`
  (behaviors, application, refusals), `Loot/` (drop rolls), `Rules/` (YAML models), `Display/`
  (tooltip, names, colors), `Commands/`, `Patches/`. Small units: methods ≤ 24 lines, classes ≤ 300.

## Performance invariants

- No per-frame parsing, reflection, or allocation in any patch on a hot path (GetDamage is hot:
  read the cached record only).
- Aggregation rebuild only on equip/inventory-change events.
- Tooltips built on demand only.
- Drop rolls precompute weight tables per (biome, creature) at YAML load, not per kill.
- Ground-glow lights never cast shadows, keep a small radius, and are capped: only the nearest N
  glowing items to the camera hold a live light (default 25), re-evaluated on a timer — a loot
  explosion must not become a light explosion.
- PatchGuard's Profiler run is part of every phase's acceptance before release.

## Multiplayer test checklist (dedicated server + 2 clients, every phase)

- Stone applied on client A: result visible to client B (tooltip via trade/chest, colored name).
- Magic item round-trips: chest, dropped to ground, tombstone after death, portal, logout/login.
- Pre-rolled drop from a creature killed by A is intact when picked up by B.
- Join-in-progress client sees stone prefabs and existing magic items correctly.
- YAML hot reload on server propagates to clients without relog; locked settings refuse client edits.
- Band/sealed/full refusals behave identically for host-less clients.
- Aggregation SE recomputes on equip change for the right player only; no drift after teleport/death.
- Profiler shows no EliteCrafting method in the top offenders during normal play.

## Console commands (sketch — finalized in `features/console-commands.md`)

`ecraft give <stone_id> [count]` · `ecraft roll <rarity> [slot]` (spawn a rolled test item) ·
`ecraft inspect` (dump hovered/held item's ecf_ data) · `ecraft reroll` (dev reroll held item) ·
`ecraft stats` (dump own aggregated totals). Admin-gated like ECR's commands.

## Phases

Legend: ✅ done · 🔧 code done, needs in-game test · ⏳ in progress · ☐ not started · 👤 needs the user.
Detail for Phase 1 lives in `SPEC.md` §10 (the build checklist); decisions in `DECISIONS.md`.

### Phase 0 — SPEC (no code) — ✅ DONE 2026-09-23

- ✅ `SPEC.md` + 18 `features/` files; every batch-1 effect renamed to our vocabulary (182 affixes on 131
  effects: `features/affixes.md`, `affixes-mythic.md`); slot matrix; tier tables per biome.
- ✅ Per-stone behaviour, refusals and edge-case tables for all 27 stones (`features/stones.md`).
- ✅ YAML schemas with examples (`configuration.md`, `economy-yaml.md`); command grammar; localization keys.
- ✅ Game signature survey (`~/scratch/specs/ec-game-notes.md`, outside the repo).
- ✅ Open questions resolved with the user, or adopted as defaults the user may override (`DECISIONS.md`).
- ☐ 👤 One question still open, blocking **Phase 3 only**: AFX-8 — what the party auras (`warbanner`,
  `aegis`) do without the Party mod.

### Phase 1 — playable core (release 0.1.0) — 🔧 CODE COMPLETE, NOT TESTED IN GAME

Code (all build clean; nothing has run in game yet):
- 🔧 Project skeleton, standard mod layout, libraries Charter + ConfigReload + PatchGuard + YamlDotNet.
- 🔧 Item data model in `m_customData` + parse cache; orphaned affixes kept dormant.
- 🔧 Both YAML families: embedded defaults, overlays, validation, Charter sync, lock, hot reload.
- 🔧 Stone prefabs: all 27 + 16 reserved `ECF_Custom01–16`, registered on every peer; tint, grade scale, tinted icons.
- 🔧 Stone stacks load whole when larger than the current max stack (join before the server's rules, a lowered
  `stack`): `Items/StoneStackGuard`, DECISIONS.md IMP-7.
- 🔧 Workbench-upgrade carry-over of affixes; non-stackable magic gear warning.
- 🔧 Rolling (rarity ladder, tier window, exclusions, weights, Mythic pool).
- 🔧 Stone application gesture, refusals, confirm gate (HoldShift/Dialog/Off).
- 🔧 The 8 Phase 1 stones: Awakening, Ascension, Exaltation, Transcendence, Apotheosis, Lesser
  Growth, Lesser Turmoil, Lesser Perfection.
- 🔧 Effects: hidden aggregate status effect + item-local patches — **34 effects / 59 affixes** (widened
  from the planned ~12 so every gear type can reach Mythic's 6 affixes).
- 🔧 Display: rarity-colored names, tooltip affix block, ground glow (nearest-25 cap).
- 🔧 Creature stone drops + pre-rolled Uncommon–Legendary gear drops (on the creature's owner).
- 🔧 `ecraft` commands: help, inspect, stats, list, give, roll, reroll, affix, reload, tiers, dump (incl. `dump items`).

Remaining before 0.1.0 ships:
- ✅ Integration pass (2026-09-23): every contract request resolved (independent roll RNG, `item_weight`, stone
  `description`, stone stacks kept whole on load, rules-in-force for `list`/`dump`, reload result, `Words.Changed`,
  `ItemTier.RefreshRecipes`); cross-area review; judgement calls recorded as DECISIONS.md IMP-1–54 (ItemCopies
  dropped, IMP-1); README store draft, CHANGELOG 0.1.0, nexus text, CLAUDE.md contracts, feature status lines;
  size, clean-room, library and hot-path audits clean; consolidated MP test plan in `features/multiplayer.md` 6.
- ✅ Code review pass (2026-09-23): two reviewers checked every Harmony target against the decompile; 11 defects
  fixed (none a crash or dupe in normal play) — isolated patch/feature startup so one broken hook can't delete
  stones, zero-weight tiers, `cost: 0`, unbuilt verbs disabled at load, unbound reserved stones merging by name,
  duplicate stone names, durability after upgrade, stones into chest slots (IMP-55–64 in DECISIONS.md).
- ☐ 👤 Run `ecraft dump items` + `ecraft tiers` in game; verify stone base prefabs, item classification and
  the material → tier map against the dump (all currently "believed vanilla").
- ☐ 👤 Full multiplayer checklist green on a dedicated server + 2 clients (`features/multiplayer.md`).
- ☐ 👤 PatchGuard Profiler run: no EliteCrafting method among the top offenders.
- ☐ 👤 `thunderstore/icon.png` (256×256) and store images.
- ☐ 👤 `pack.ps1 -Version 0.1.0` (needs PowerShell — not on homelab01), Thunderstore upload, commit + tag `EliteCrafting-v0.1.0`.

### Phase 2 — the economy (release 0.2.0) — 🔧 CODE COMPLETE, NOT TESTED IN GAME

All 27 stones, 16 essences, sigils, quality, salvage and fusing, chests, loot-find stats, 103 new affixes (162 in all,
115 effects), the Elite Creatures Reborn hook and the display leftovers are built, reviewed and build clean; the
default YAML loads with 0 errors and 0 warnings; SPEC.md section 11 is the build checklist, `features/multiplayer.md`
section 6 steps 22-38 the in-game test plan.

- 🔧 Remaining manipulation stones: Greater Growth/Turmoil/Perfection, Upheaval (both), Severing (both), Unmaking.
- 🔧 Risk stones: Serpent, Binding, Chance, Reflection (Serpent's corruption flash for nearby peers is Phase 3; message only).
- 🔧 Sigils: the `sigil` verb applies one; every verb steered or not per sigils.md (DECISIONS.md IMP-65–72).
- 🔧 Quality stones: Honing, Tempering (`ecf_refine`; the stat getters and the tooltip line already read it).
- 🔧 Essences (`features/essences.md`): 8 biome families × Lesser/Greater = 16 prefabs on the `essence` base (👤 verify
  `Thunderstone` with `ecraft dump items`), verb `imbue` + `essence_families`, home-tier/boss/serpent drop rows, family
  line in the tooltip, `ecraft list`/`give` (DECISIONS.md ESS-1–17, IMP-102–104, 108).
- 🔧 Phase 2 effects: every easy and medium non-Mythic effect of the registry — 77 effects, 99 new default affixes
  (with the four loot-find affixes on their four effects: 103 new, 162 in all, on 115 effects), incl. the
  health-critical variants; nothing left for the hard-hook list (DECISIONS.md IMP-85–101).
  - 🔧 Kill attribution (Reaper, Soul Reaper): seen on the creature's owner, one routed RPC to the killer's peer only;
    leeches attacker-side from the outgoing hit (IMP-85–86).
  - 🔧 Killer-stat plumbing (Loot): each player publishes its capped `ecf_find_*` totals to its own ZDO, the
    creature's owner reads the last hitter's; Fateweaver, Norns' Favour, Trophy Taker, Hoardfinder applied
    (DECISIONS.md IMP-73–75). 🔧 Their effect ids are registered and the four affixes are in the default YAML.
- 🔧 Chest/world drops: world containers roll once when the game fills them, on the container's owner (`ecf_filled`,
  IMP-76–79); 🔧 `drops.chests.containers` per-prefab overrides parsed and live.
- 🔧 Salvage (`features/salvage.md`): the Salvage key (End, Shift to confirm; IMP-111) grinds a magic item into shards of its
  ascension stone, right-click fuses shards (Shift: all sets); 5 shard prefabs, `salvage:` section with `fragments`
  merged by id, `.cfg` `8 - Salvage` (DECISIONS.md SAL-1–16, IMP-102, 105–110). SAL-14 decided by the user 2026-09-24: OpenKeep's Salvage skips
  items carrying `ecf_` data (OpenKeep-side change, tracked in OpenKeep).
- 🔧 ECR soft hook (config-gated, default off; `features/ecr-integration.md`): detection by GUID, ECR stars replace the
  game level on their own table (`drops.ecr`), worthless ECR creatures drop nothing, `ecraft ecr` (DECISIONS.md ECR-1–12,
  IMP-80–84). The world-tier terms are built but inert until ECR writes `ecr_tier` per creature (ECR-6 decided by the
  user 2026-09-24; an ECR-side task for when ECR builds world tiers).
- 🔧 Display leftovers: the crafting panel's upgrade tab shows the target's colored name and affix block, upgrade
  entries in the recipe list are rarity-colored; item and armor stand hovers show the colored name and the block
  (DECISIONS.md IMP-121-123). The essence family and shard fuse lines stay in the item description (IMP-124).
- ✅ Wrap-up (2026-09-24): contracts, SPEC identity table and section 11, effects-runtime section 7, the multiplayer
  test plan, README, CHANGELOG 0.2.0 and nexus text brought up to date; `ecraft stats` prints the active Phase 2
  states; size, clean-room, library, translation and default-YAML audits clean.
- ✅ Phase 2 review pass (2026-09-24): two reviewers + an OpenKeep-side change. Fixed: Evader's Fury never firing
  (melee dodges are decided attacker-side — new `ECF_MeleeDodged` RPC), leech healing off friends/pets and chop damage,
  permanent Momentum from dodge presses, a Runic Ward crash on burn ticks, sheathed-weapon grinding, grinding under
  open panels; Salvage key moved to End (Delete clashed with OpenKeep's Trash/Destroy Junk). No dupe/loss path found
  in Reflection, salvage or fusing (IMP-111–119). Open: 👤 Deep Vein/Heartwood yield scaling (IMP-120, measure in game).

Remaining before 0.2.0 ships (all 👤, in order):
- ☐ 👤 `ecraft dump items` in game: confirm the essence base `Thunderstone` (else the logged fallback is used), and the
  prefab names `BonemawSerpent` (serpent Tide essence row) and `Fader` (boss row), both marked `# verify` in the
  economy defaults; fix the defaults if a name is wrong. (Also Phase 1's stone base and tier checks above.)
- ☐ 👤 Phase 2 in-game verification on a dedicated server + 2 clients: `features/multiplayer.md` section 6, steps 22-38
  (incl. the join-timing check for chests, step 35, and ECR with both mods, step 36); tick SPEC.md section 11.
- ☐ 👤 IMP-120: measure Deep Vein / Heartwood extra yield in game (step 33) and retune if it multiplies too far.
- ☐ 👤 PatchGuard Profiler run with Phase 2 affixes equipped (step 38).
- ☐ 👤 `thunderstore/icon.png` (256×256) and store images, if 0.1.0 has not shipped them.
- ☐ 👤 `pack.ps1 -Version 0.2.0` (PowerShell, not on homelab01), Thunderstore upload (tcli) and Nexus, then commit and
  tag `EliteCrafting-v0.2.0` (0.1.0 has not shipped: decide whether it ships first or 0.2.0 is the first release).
- ☐ ECR-side (another session): ECR writes `ecr_tier` per creature when it builds world tiers (ECR-6); a comment on
  ECR's `TraitKeys` naming EliteCrafting as a reader.

### Phase 3 — endgame polish — ☐ NOT STARTED (blocked: AFX-8, party auras without the Party mod, needs the user)

- ☐ Mythic-only pool (auras via Party pairing, Undying, Bifrost Blessing, Thunderclap, Blink, Allfather's
  Bulwark, Elementalist's Pact) — needs AFX-8 answered first.
- ☐ Per-rarity glow/particles on equipped gear; sounds; slot flash on stone use.
- ☐ Original icon art and optional custom stone meshes (our own AssetBundle, or procedural).
- ☐ Hard hooks last (attack speed, projectile pierce); gamble vendor idea evaluated.
- ☐ Optional in-game YAML editor (not in v1 — YamlConfig is not used).

Versioning: 0.y.z while unfinished; MINOR for new stones/affixes/keys, MAJOR for breaking key or
YAML changes, per workspace CLAUDE.md. Release via `pack.ps1` + tcli, same as every mod.

## Decisions log

- 2026-09-23 — Six rarities: Common/Uncommon/Rare/Epic/Legendary/Mythic (user).
- 2026-09-23 — Currency concept: stones (user: "I like the stone concept for now"; display names
  below are placeholders until the user blesses them in Phase 0).
- 2026-09-23 — Slot sanity: affixes only on item types where they make sense (user).
- 2026-09-23 — Name: EliteCrafting, from the folder the user created (user).
- 2026-09-23 — Rarity palette and ground glow (user): dropped magic items glow their rarity color;
  Common never glows. Uncommon `#1EFF00`, Rare `#0070DD`, Epic `#A335EE`, Legendary `#FF8000`,
  Mythic `#E6262E`. Ships in Phase 1.
- 2026-09-23 — Mythic is craft-only by default; Stone of Chance cannot yield Mythic by default (Claude, YAML-reversible).
- 2026-09-23 — Turmoil = PoE2 swap-one; Upheaval kept as PoE1 full reroll (Claude, arguable).
- 2026-09-23 — Categories instead of prefix/suffix (Claude, arguable — revisit if sigil steering feels shallow).
- 2026-09-23 — No spirit-resist affix: spirit damage does not affect players in vanilla (Claude).
- 2026-09-23 — Health-critical threshold default 30%, threshold affix additive (from pool draft).
- 2026-09-23 — Drop model (user confirmed): pre-rolled Uncommon–Legendary world drops alongside
  stones; Mythic never drops by default.
- 2026-09-23 — Stone display names (user): the catalog names above ship as-is.
- 2026-09-23 — Orphaned affixes (user): the item keeps the data; the tooltip shows it dormant
  (greyed), the effect is inert; restoring the id in YAML revives it.
- 2026-09-23 — Vanilla upgrade levels (user): affix values are fixed and do not scale with the
  item's workbench quality level.

- 2026-09-23 — YAML plumbing (user): follow Elite Creatures Reborn's pattern — Charter + ConfigReload +
  PatchGuard + YamlDotNet with our own rule reader/sync; **no YamlConfig / SyncedConfig** (ECR
  server-enforcement.md §7 provenance note). No in-game YAML editor in v1.
- 2026-09-23 — `assembly_guiutils.dll` may be decompiled as game code (user); added to CLEANROOM.md.
- 2026-09-23 — Item tier model (user): explicit YAML item map → highest recipe-material tier →
  crafting-station fallback → tier 1 (features/item-tier.md).
- 2026-09-23 — Remaining Phase 0 tuning questions (user): build on the proposed defaults; every
  question and its default is collected in DECISIONS.md for later override.

## Open questions

None open for Phase 1. Every question is answered in `DECISIONS.md` (user decisions, reconciliation, and
adopted defaults the user may override); AFX-8 (party auras without the Party mod) is BLOCKING for Phase 3.
The workbench-upgrade question (old item 8) is answered by the decompile: upgrades drop custom data, so
the carry-over patch is a Phase 1 requirement (features/item-data.md section 1).

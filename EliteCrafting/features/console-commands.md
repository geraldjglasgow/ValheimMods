# EliteCrafting - specification: Console commands

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the `ecraft` console command**: its sub-commands, grammar, who may run each, argument validation
and output format.

**Status: Phase 1 and Phase 2 built, not tested in game** (2026-09-24): Phase 2 added `ecr`, shard ids for `give`, essence
families and shards in `list stones`, and the active states in `stats`.

---

# 1. The shape

**One game console command, `ecraft`, with sub-commands.** One name in a console shared with the game and every
other mod. Registered once in a postfix on `Terminal.InitTerminal` (idempotent, like ECR's `elite`), never with
the game's `isCheat` flag (it marks the player's profile as cheated) and not relying on `onlyAdmin` (stored but never
checked by the game), with a tab-completion options
fetcher for the sub-command names and, per sub-command, for stone ids, rarity ids, slot ids, affix ids and family
names from the running configuration.

Grammar notation: `<required>`, `[optional]`, `a|b` alternatives. Sub-commands and ids are case-insensitive.

| Sub-command | Grammar | Access | Does |
|---|---|---|---|
| `help` | `ecraft [help]` | everyone | Lists the sub-commands this player may run, one line each |
| `inspect` | `ecraft inspect [cursor|hover|ground|<slot>]` | read-only | Dumps an item's `ecf_` data and its parse (section 3) |
| `stats` | `ecraft stats` | read-only | Own aggregated effect totals (section 3) |
| `list` | `ecraft list affixes|stones|rarities [<filter>]` | read-only | The running configuration, filtered by slot, category, rarity or id prefix; `stones` also lists the essence families and the salvage shards |
| `give` | `ecraft give <stone_id>|<shard_id>|all [count]` | admin | Puts stones, essences or salvage shards into own inventory |
| `roll` | `ecraft roll <rarity> <prefab|slot> [tier]` | admin | Creates a rolled magic item in own inventory |
| `reroll` | `ecraft reroll [cursor|hover|<slot>]` | admin | Rerolls every affix of an item in own inventory, keeping rarity |
| `affix` | `ecraft affix <affix_id> [tier] [value] [cursor|hover|<slot>]` | admin | Adds, or replaces, one specific affix on an item, for testing an effect |
| `reload` | `ecraft reload` | admin, author side | Re-reads both YAML families, the translation files and the `.cfg` now, instead of on the next poll (`configuration.md` section 5) |
| `dump` | `ecraft dump affixes|economy|items` | admin | `affixes`/`economy`: writes the effective merged configuration to `EliteCrafting_effective_<family>.yml.txt` in the config folder. `items`: the item survey, section 3 |
| `tiers` | `ecraft tiers` | admin | Writes `EliteCrafting_item_tiers_reference.yml` as `item-tier.md` specifies (every magic base with slot, tier and the rule that decided it, plus unmapped materials) |
| `ecr` | `ecraft ecr` | read-only | The Elite Creatures Reborn synergy (Phase 2): whether ECR is installed here, the `Synergy` switch, the hovered creature's ECR keys and the drop terms they give (`ecr-integration.md` section 11) |

`<slot>` here means an **equipment slot** to pick an equipped item: `right`, `left`, `head`, `chest`, `legs`,
`cape`, `utility`. It is not the affix slot taxonomy (`roll` takes that one).

---

# 2. Access

- **Read-only** (`inspect`, `stats`, `list`, `help`): every player, unless the synced `.cfg` key
  `Read-only commands for everyone` is off, which makes them admin-only. They tell a player about items they
  already hold and rules they already play under.
- **Admin** (`give`, `roll`, `reroll`, `affix`, `reload`, `dump`, `tiers`): the host, single player, or a player on the
  server's admin list (`ZNet.LocalPlayerIsAdminOrHost()`, as ECR). Anyone else: `ecraft: <sub> needs admin rights on
  this server.`
- **Author side** (`reload`): only where this side's files are in force (single player, the host, a player on an
  unlocked server). A player connected to a locked server gets `ecraft: reload changes the server's files; run it
  on the server.` A dedicated server's own console runs it. There is no in-game editor in v1 (`../DECISIONS.md`
  U-11).
- **Where the check runs**: on the client. Every admin command here changes only the caller's own inventory or
  files, and Valheim trusts a client with its own inventory; a server-side check would add a round trip and still
  not stop a modified client. This is a deliberate difference from ECR's world-changing commands, which do run on
  the server. (`../DECISIONS.md` CMD-1.)

---

# 3. Behaviour and output

Output goes to the console that ran the command (`args.Context.AddString`). Every line starts with
`ecraft <sub>:`. Numbers are printed in invariant culture so they can be pasted into YAML. Errors print the problem,
then the grammar line.

**Target resolution** for `inspect`, `reroll`, `affix` when no target is given, first match wins:

1. `cursor`: the item on the mouse cursor (being dragged).
2. `hover`: the inventory or container item under the mouse, when the inventory is open.
3. `ground`: the dropped item under the crosshair (`inspect` only; it is not in the inventory).
4. `right`: the item in the right hand.

`reroll` and `affix` refuse a target that is not in the caller's own inventory, is not a magic base, or (for `reroll`)
has no rarity. They ignore seals, costs, `Modify equipped items` and the rarity's affix count (they are test tools),
but never write an item whose format is newer (`item-data.md` section 8).

**`inspect`** prints, for the target:

```
ecraft inspect: Bronze sword [2] (SwordBronze), slot melee_weapon, tier ceiling 2, magic base
  raw: ecf_v=1 | ecf_rarity=rare | ecf_affixes=balanced_grip:2:7;long_reach:2:5;old_affix:1:3 | ecf_refine=3
  rarity rare (#0070DD), format v1
  affix balanced_grip T2 7 -> effect attack_stamina_cost, active
  affix long_reach T2 5 -> effect attack_reach, active
  affix old_affix T1 3 -> dormant (not in configuration)
  refine +3%, not sealed, no sigil, not bound
```

**`stats`** prints every non-zero aggregate channel and the equipped items that feed it:

```
ecraft stats: 4 magic items equipped, 9 active affixes, rebuilt 2.3 s ago
  move_speed          sum 11   cap 25   applied 11   (legs, cape)
  carry_capacity      sum 85   cap -    applied 85   (chest, utility)
  skill_level:Swords  sum 6    cap -    applied 6    (right)
  health critical: no (threshold 30%)
  active now: ward 12 left, momentum
```

The last line (Phase 2) lists the runtime states in force (`EffectSnapshot.States`: a Runic Ward charge, Evader's
Fury, Steel Rhythm, Momentum, being on a path, Fafnir's Greed coin stacks) and is left out when none is.

**`list`** prints one line per entry: id, localized name, value type, slots, category, tier range, enabled, and the
last file that touched it (`builtin` for the embedded layer). An essence's line adds `family <id>`. Under the stones,
`list stones` prints each essence family with its members - a member the roll skips (not defined, disabled,
Mythic-only) in brackets - and each salvage shard with its fuse count and target stone (`essences.md` section 12,
`salvage.md` section 6). The filter `imbue` or a family id shows only the families; `shard` only the shards.

```
ecraft list: 16 of 43 stones matching 'imbue'
  essence_venom_lesser "Lesser Venom Essence" imbue/lesser on [uncommon,rare] family venom prefab ECF_EssenceVenomLesser enabled (builtin)
  ...
  family venom "Venom": venombrand, blood_drinker, blood_thrift, venomward, bulwark_poison, purity, marshstrider, oilskin
```

**`give`**: `count` default 1, 1-9999. Spawned stones get the world's `m_worldLevel`, as vanilla spawns do, or they
would not stack with dropped ones. Adds up to what fits; the rest is dropped at the caller's feet (vanilla
`spawn` behaviour), and the output says how many went where. `all` gives `count` of every enabled stone (essences
included, shards not). An essence id works like any stone id; a shard id (`shard_ascension`) gives shards and adds a
line saying what they fuse into, or that they do not (no `salvage.fragments` entry, or a disabled target stone). An
unknown id prints the closest ids, shard ids among them.

**`roll`**: `<prefab>` is an item prefab name; `<slot>` is an affix slot id and picks a random magic-base item prefab
of that slot. `tier` (1-7) overrides the item's tier ceiling for this roll; without it the ceiling is the item's own
(`item-tier.md`). The roll uses the same code path and rules as a drop (`drops.md`) so it is a true sample. The new
item's `inspect` output is printed. `roll mythic` is allowed (admin tool), which is the only way besides the Stone of
Apotheosis.

**`affix`**: `tier` defaults to the highest tier the affix defines; `value` defaults to a normal roll in that tier's
range and may be outside the range (a test value). An existing affix of the same id is replaced in place; otherwise
it is appended, even past the rarity's maximum. Exclusion groups are ignored, with a warning line.

**`ecr`** (Phase 2, `ecr-integration.md` section 11, DECISIONS ECR-12): read-only, English. The first line says
whether Elite Creatures Reborn is installed on this machine (version and GUID) and the `Synergy` switch with whose
value it is (`server` while the server binds this player, else this machine's `.cfg`); then whether ECR records a
world tier. With a creature under the crosshair it adds the creature's ECR keys as stored and the terms the drop roll
would use for it - computed by the roll's own code, so the two never disagree. Last, how many of the last 20 qualifying
deaths rolled on this machine carried ECR data (counted while the synergy is on).

```
ecraft ecr: Elite Creatures Reborn 3.4.0 found (gglasgow.elitecreaturesreborn); synergy ON (server)
  world tier: not recorded by this ECR version (the tier terms stay inert, DECISIONS ECR-6)
  hovered: Troll  ecr_resolved=true  ecr_stars=3  worthless=no  ecr_tier=-
  stars 3 -> x2.0 (drops.ecr); tier term x1.0; rarity bonus +0.0%
  last 20 deaths: 17 with ECR data
```

**`reload`**: prints what each family did (`reloaded, 214 affixes` / `errors, previous configuration kept, see log`).

**`dump affixes|economy`**: writes the effective merged configuration (all layers applied) in the schema of
`configuration.md`, with a header comment naming the files that contributed. The extension `.yml.txt` keeps it out of
both file families.

**`dump items`** (Phase 1, `../DECISIONS.md` RC-10): the in-game survey that base prefabs, item classification, the
slot map and the material map are verified with, since prefab data lives in asset files and not in code. Writes
`EliteCrafting_items.txt` to the config folder, one line per `ObjectDB` item, tab-separated so it pastes into a
spreadsheet:

| Column | Source |
|---|---|
| prefab | the prefab name |
| display name | `m_shared.m_name` localized, plus the raw token |
| item type | `m_shared.m_itemType` |
| skill | `m_shared.m_skillType` |
| stack, weight, teleportable, value | `m_maxStackSize`, `m_weight`, `m_teleportable`, `m_value` |
| recipe materials | every enabled recipe producing the item: each resource prefab and its craft amount (upgrade-only amounts marked) |
| station | each recipe's crafting station prefab and minimum station level |
| resolved slot | the slot id from `item-data.md` section 2, or `-` |
| tier | the derived tier ceiling and the rule that decided it (`item-tier.md`) |
| visuals | whether the prefab has `Light`, `ParticleSystem` or `LightLod` children (for `prefabs.md` section 4) |

Every command runs inside `PatchGuard`'s `Guard.Run`, so an exception is logged under the mod's name and the console
gets `ecraft <sub>: failed, see the log.`

---

# 4. Multiplayer

- Every sub-command runs on the caller's machine and changes at most the caller's own inventory or the author's
  files. Nothing is sent to the server by the command itself; an item created or changed by `give`, `roll`,
  `reroll` or `affix` reaches others the ordinary way (`item-data.md`).
- `reload` on the server re-publishes to every connected player, which is why it is admin-only.
- `inspect ground` reads the replicated item data the client already has; it needs no ownership.

---

# 5. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Console commands: CMD-1 to CMD-4; `dump items` in
Phase 1 is RC-10; `tiers` is TIR-5).

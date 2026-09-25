# EliteCrafting - specification: Configuration

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the settings file and the two YAML families**: every `.cfg` key, the files of a family and how they
combine over the built-in defaults, validation and error reporting, server sync, hot reload, write-back of the
defaults, and the **complete affix YAML schema**. The economy family's schema and content (rarities, rolling, stones,
sigils, item tiers, biomes, drop tables) are `economy-yaml.md`; its file mechanics are here.

**Status: Phase 1 built, not tested in game** (2026-09-23). The plumbing follows Elite Creatures Reborn's (user decision, `../DECISIONS.md`
U-11); section 1 says which parts carry over and what changes for two families of several files.

---

# 1. The pieces

| File | Holds | Synced |
|---|---|---|
| `BepInEx/config/com.EliteCrafting.cfg` | global switches, confirm gate, display and glow preferences, diagnostics | gameplay keys yes and lockable; display, glow, confirm, diagnostics never |
| `EliteCrafting_affixes*.yml` | every affix, the effect caps, the health-critical threshold | yes |
| `EliteCrafting_economy*.yml` | rarities (palette, glow, affix counts), rolling rules, stones, sigils, item tiers, biomes, drop tables (`economy-yaml.md`) | yes |

**Libraries.** `Charter` (server binding, version check, the pushed file texts), `ConfigReload` (the `.cfg`: save and
five-second poll), `PatchGuard` (every entry point guarded) and `YamlDotNet` (its representation model only, no
object deserializer). **No `YamlConfig`, no `SyncedConfig`, no in-game editor** (U-11): the YAML side is the mod's own
code in `Rules/`, built the way Elite Creatures Reborn's `Rules/` folder is. Charter title `EliteCrafting`,
`mandatory: true` (the mod is required on both sides, `multiplayer.md`).

**The rule plumbing, one responsibility per class** (names are suggestions; the ECR class each one mirrors is in
brackets, read it for the pattern):

| Piece | Does | Pattern |
|---|---|---|
| Default text | the complete, commented default file of each family, embedded in the DLL as a resource; data, not logic | ECR `RuleText` |
| Rule files | per family: finds the files on disk, writes the default main file on first run, reads each file's text verbatim, remembers a write-time-and-size stamp per file, reports "changed on disk" | ECR `RuleFile` |
| YAML reads | the low-level reads over a YamlDotNet node tree: child by key, typed scalars, lists; every failure recorded against its file and line, never thrown | ECR `YamlRead` |
| Readers | one per family: turns one file's text into a partial model (only the keys the file names), collecting errors and warnings | ECR `RuleParser` |
| Overlay | lays one partial model over the model built so far: by id, field by field (section 3) | ECR `RuleOverlay` |
| Verify | post-merge checks on the finished model: completeness, cross-references, the effect registry | - |
| Reload | a slow poll on the main thread: every five seconds, compare each family's stamps; on a change, rebuild that family | ECR `RuleReload` |
| Server lock | Charter binding and one article per family; decides which texts a machine builds from | ECR `ServerLock` |
| Rule state | the one active model per family that the rest of the mod reads, and a `Changed` event | ECR `RuleState` |

What changes against ECR: ECR has one file per machine; EliteCrafting has two families of any number of files each,
laid over built-in defaults. So the article carries a **list of file texts**, the overlay runs once per file, and
the lock switch is the `.cfg` key `Lock Configuration` itself rather than a key mirrored out of the YAML.

**The mod runs with no YAML files at all**: the built-in defaults (section 3) are complete. A player who never opens
a file gets the designed game.

---

# 2. The `.cfg`

Sections are numbered so they sort in configuration UIs. "Synced" entries are Charter clauses and take the server's
value while the server's `Lock Configuration` is on; "local" entries are ordinary entries, never registered with
Charter, and never leave the machine.

| Section | Key | Type | Default | Synced | Meaning |
|---|---|---|---|---|---|
| `1 - General` | `Lock Configuration` | bool | `true` | binding | Charter's binding switch; the server's value governs. When on, every player uses the server's gameplay values and YAML and their own are ignored until they disconnect |
| `1 - General` | `Affix effects` | bool | `true` | synced | Master switch for all affix effects. Off: items keep and show their affixes, nothing applies (`effects-runtime.md`) |
| `2 - Stones` | `Modify equipped items` | bool | `true` | synced | Stones may be used on equipped items (`applying-stones.md` section 5) |
| `2 - Stones` | `Confirm destructive stones` | enum `HoldShift` / `Dialog` / `Off` | `HoldShift` | local | How stones marked `confirm: true` ask first (`applying-stones.md` section 4) |
| `3 - Drops` | `Stone drops` | bool | `true` | synced | Creatures drop stones per the economy drop tables |
| `3 - Drops` | `Magic item drops` | bool | `true` | synced | Creatures drop pre-rolled magic gear per the economy drop tables |
| `4 - Commands` | `Read-only commands for everyone` | bool | `true` | synced | `ecraft inspect`, `stats`, `list`, `help` for every player; off makes them admin-only too (`console-commands.md`) |
| `5 - Display (per player)` | `Colored item names` | bool | `true` | local | `display.md` section 2 |
| `5 - Display (per player)` | `Tooltip detail` | enum `Compact` / `Standard` / `Full` | `Standard` | local | `display.md` section 3 |
| `5 - Display (per player)` | `Show dormant affixes` | bool | `true` | local | `display.md` section 4 |
| `6 - Ground glow (per player)` | `Ground glow` | bool | `true` | local | `display.md` section 5 |
| `6 - Ground glow (per player)` | `Glow intensity` | float 0-3 | `1.0` | local | |
| `6 - Ground glow (per player)` | `Glow range` | float 0.5-6 | `2.0` | local | metres |
| `6 - Ground glow (per player)` | `Glow max lights` | int 0-100 | `25` | local | nearest-N cap |
| `6 - Ground glow (per player)` | `Glow refresh seconds` | float 0.25-5 | `1.0` | local | |
| `6 - Ground glow (per player)` | `Glow stones` | bool | `false` | local | |
| `7 - Diagnostics` | `Log rolls` | bool | `false` | local | Log every roll (stone, drop, command) with its inputs and result |
| `7 - Diagnostics` | `Log effect rebuilds` | bool | `false` | local | Log each aggregate rebuild with the channel totals |
| `9 - Elite Creatures Reborn` | `Synergy` | bool | `false` | synced | With Elite Creatures Reborn installed: its elite stars (and later its world tier) raise EliteCrafting's drops. No effect without it (`ecr-integration.md`; its numbers are `drops.ecr`) |

- Every value is hot-reloaded (ConfigReload's five-second poll) and takes effect without a restart: the display keys
  on the next tooltip and glow tick, `Affix effects` on the next rebuild, the drop switches on the next kill.
- Numeric ranges are `AcceptableValueRange`; configuration UIs show them as sliders.
- Everything that decides *what* rolls (rarities, affixes, tiers, weights, costs, drop tables and their multipliers)
  is YAML, not `.cfg`. The `.cfg` has only switches a server owner wants without opening a YAML file (`../DECISIONS.md`
  CFG-6).

---

# 3. YAML families: files, layers and merging

## Files

- Two families: `EliteCrafting_affixes*.yml` and `EliteCrafting_economy*.yml`. The patterns do not overlap, and the
  translation files (`EliteCrafting.translations.<Language>.yml`, `localization.md`) match neither.
- Searched in the BepInEx config folder only. Within a family: **the main file** (`EliteCrafting_affixes.yml` /
  `EliteCrafting_economy.yml`) **first, then every other matching file in file-name order** (ordinal,
  case-insensitive).
- **On first run** (the family's main file does not exist) the mod writes the main file from the embedded default
  text: a full, commented copy of the built-in defaults, every value at its default and every comment in place, so an
  owner always edits a complete, documented file rather than a blank one (`../DECISIONS.md` CFG-2). A write failure
  is logged and changes nothing else; the defaults still apply.
- Extra files are the owner's: `EliteCrafting_affixes_custom.yml`, `EliteCrafting_economy_zz_testing.yml`. They
  survive every update because the mod never writes them.

## Layers

The model for a family is built from layers, in this order:

1. **Built-in defaults**: the embedded default text, parsed once at startup. Identical on every peer (same mod
   version, enforced by Charter's version check), so it is never transmitted. A unit test asserts that it parses
   with no error and no warning.
2. **Each file** in the order above, overlaid one after another.

The main file may set the root key `use_defaults: false`; then layer 1 is skipped and the files alone are the whole
configuration. (`use_defaults` in any other file is a warning and ignored.) Default `true` (`../DECISIONS.md` CFG-1).

Why layered: a release that adds a new affix or stone reaches servers whose main file was written by an older
release, without the owner merging anything. The flip side, accepted: a main file that is a full copy of an older
release's defaults keeps that release's numbers for every entry it names, so a later **re-balance** of an existing
entry does not reach it. An owner who wants the new numbers deletes or renames the main file; the next start writes
the current one.

**What the built-in defaults contain, per release**: only the affixes whose effects that release implements (0.1.0:
the Phase 1 set, `affixes.md` section 6), because a later-phase effect in YAML is a validation error
(`effects-runtime.md` section 1). Later affixes join the defaults in the release that builds them, and layering
delivers them to existing servers. The economy defaults were complete from 0.1.0, with the Phase 2 stones present and
`enabled: false`; 0.2.0 enables them and adds the essences, `essence_families`, `salvage`, `drops.chests` and
`drops.ecr` (`economy-yaml.md` section 4). **Consequence for a server whose main economy file was written by 0.1.0**
(verified with the loader, 2026-09-24): that file names the 19 Phase 2 stones with `enabled: false` and the bosses with
their 0.1.0 `bonus` lists, so those stones stay disabled and the bosses' essence rows are replaced; new ids (the
essences, new `drops.stones` keys, new affixes) still arrive. The fix is the one above: delete or rename the main file
(README and CHANGELOG 0.2.0 say so).

## Merging entries

The overlay changes only what a file names, and leaves everything else as the earlier layers made it (the same rule
as ECR's per-biome overlay). Every list of entries with an `id` (affixes; rarities and stones in the economy family)
merges **by id, field by field** (`../DECISIONS.md` CFG-3):

- An entry whose id is **new** is added. After all layers it must be complete (every required field present) or it
  is an error.
- An entry whose id **already exists** from an earlier layer changes only the fields it names. A two-line entry is a
  valid override:

  ```yaml
  affixes:
    - id: fleetfoot
      enabled: false
  ```

- **List-valued fields** (`slots`, `tiers`, `applies_to`, `outcomes`) are replaced whole, never merged item by item.
- **Map-valued fields** (`caps`, `health_critical`, a stone's `cost` and `weights`) merge by key.
- The **same id twice in one file** is an error (almost always a copy-paste mistake).
- An override in a later file is logged at info level ("affix `fleetfoot` overridden by
  `EliteCrafting_affixes_custom.yml`: enabled"), so an owner can see what their files did.
- **Removing** an entry: `enabled: false`. Deleting an entry from a file removes only that layer's contribution;
  the built-in default is still there unless `use_defaults: false`.
- The economy family's map sections (item tiers, biomes, drop tables) merge as `economy-yaml.md` section 1 says.

---

# 4. Validation, errors and the fallback

Reading never throws: every problem is collected with its file name and line (`EliteCrafting_affixes_custom.yml:
line 42: 'ten' is not a number`), as ECR's reader does.

- **Errors reject the whole family**: the previous configuration stays in force, every error is logged, and nothing
  is published. A typo never silently disables a server's rules and never takes the server down.
- **Warnings are logged and the family is applied**: unknown keys, an affix that can never roll (weight 0, all tier
  weights 0, or no item can satisfy its slots and `requires`), an override that changes nothing.
- **Post-merge checks** (completeness, cross-references to rarities, slots and effects) run once on the finished
  model and name the id and every file that touched it, because a merged entry has no single line.
- The affix family checks every effect reference against the effect registry (`effects-runtime.md` section 1). The
  economy family checks references into the affix family (affix ids named by drop tables or essences) only as
  warnings, because the two families reload independently.
- **Fallback at startup**: if a family fails validation before any configuration was ever loaded, there is no
  "previous" to keep. The author (server, host, single player) then logs the errors, runs on the **built-in defaults
  alone** and publishes an **empty file list** for that family (section 5), so every peer builds the same defaults.
  Nothing is written to disk. The next successful reload replaces it. A typo never leaves a server with no affixes,
  and never leaves server and clients on different rules.
- **An empty file list** (the owner deleted every file, or the author published the fallback) builds the built-in
  defaults. Deleting every file mid-session keeps the loaded configuration and warns; the next start writes the main
  file again.

---

# 5. Sync, reload, write-back

## Sync (server lock)

- **One Charter article per family**, `ecf_affixes` and `ecf_economy`, an ordinary (not standing) `List<string>`
  article whose value is the family's files **as text**, name and verbatim content in load order. The author sends the
  texts it built its own model from, comments and all, never a parsed model, so every peer runs the same reader.
- **The author** (dedicated server, host, single player, or any player while unbound) builds from its own files and
  assigns the article after every successful build.
- **A bound player** ignores its own files: when the article arrives or changes, it builds built-in defaults plus the
  received texts, and adopts the result if it validates. **Validation runs on both sides**: a player that cannot
  build what it received keeps its previous configuration and logs the received file names, rather than falling back
  to defaults and quietly playing a different game.
- **Binding** is the `.cfg` key `Lock Configuration`, registered as Charter's binding switch (the author's value
  binds). While the server's lock is off, each player uses their own files and `.cfg`. When a player disconnects or
  the binding turns off, it rebuilds from its own files at once.
- Until the first push arrives during a join, a player runs on its own files; Charter delivers the push as part of
  the join, before the player can act.

## Hot reload

- A small `MonoBehaviour` on a `DontDestroyOnLoad` object polls every five seconds on the main thread (no file
  watcher; ConfigReload made the same choice for Mono on Linux). For each family it compares every file's write time
  and size with the stamp from the last read, and notices files added or removed. On any change it re-reads and
  rebuilds **that family only**.
- On the author a successful rebuild is adopted and re-published; every connected player rebuilds from the new texts,
  so one admin saving a file re-tunes the whole server without a relog. On a bound player a local edit is read but
  not adopted (its own files are not in force) until it unbinds.
- `ecraft reload` (`console-commands.md`) runs the same rebuild at once, for both families and the translation files.
- Each poll and each rebuild runs inside `PatchGuard`'s `Guard.Run`, so a bug is logged under the mod's name and the
  last good rules stay in force.

## Apply

- A successful build replaces the family's active model in one assignment and raises its `Changed` event. It also
  bumps the configuration generation the item parse cache checks (`item-data.md` section 5), marks the aggregate
  dirty (`effects-runtime.md` section 3), and rebuilds the precomputed tables (tier table, drop tables, colour strings).
- **Stone prefabs are never created at apply time**: they come from code only (`prefabs.md` section 2), and an
  owner-defined stone binds to one of the reserved custom prefabs. The economy family's stone name, description, stack
  size, item weight and tint need `ObjectDB`; they are written to the prefabs once they are built, and again on every
  later apply. Every live stone shares its prefab's `SharedData` (linked when it wakes), so that reaches every
  existing stack (`prefabs.md` section 2; ItemCopies is not used, `../DECISIONS.md` IMP-1).

## Write-back

- The only file the mod writes into a family is **the default main file on first run** (section 3). It never
  rewrites, reformats or "repairs" an owner's file.
- `ecraft dump affixes|economy` writes the effective merged model to `EliteCrafting_effective_<family>.yml.txt` in
  the config folder, outside both family patterns (`console-commands.md`), so an owner can see what the layers add up
  to.

## No in-game editor in v1

Owners edit the files; a remote admin without file access edits them through their host's file manager, and the
poll or `ecraft reload` on the server picks the change up (`../DECISIONS.md` CFG-4). An in-game editor may come in a
later phase as our own code over the same readers; it is not specified.

---

# 6. The affix YAML schema

This is the **canonical affix schema**: the conventions' baseline fields plus everything the catalog needs
(`weight`, `requires`, `unit`, flag tiers, skill lists, condition-aware caps, `health_critical`). `affixes.md`
supplies every default value; this section is the format and its validation. What lives in code rather than YAML
(an effect's param kind, polarity, `better: lower`, default cap and phase) is the effect registry's
(`effects-runtime.md` section 1). An affix's tooltip sentence is the localization key `$ecf_affix_<id>_line`
(`localization.md` section 1), not a YAML field.

## Root keys of an `EliteCrafting_affixes*.yml` file

| Key | Type | Required | Meaning |
|---|---|---|---|
| `use_defaults` | bool | no, default `true` | Main file only (section 3) |
| `health_critical` | map `{threshold, max_threshold}` | no, defaults 30 / 50 | Percent of max health at or below which `condition: health_critical` affixes apply; the threshold affix raises it up to `max_threshold`. `0 < threshold <= max_threshold <= 100` or error. Merges by key |
| `caps` | map: channel key → number | no | Cap on a channel's sum (`effects-runtime.md` section 5). A channel is `(effect, param, condition)`, so the keys are `effect`, `effect:param`, `effect@health_critical`, `effect:param@health_critical`. Merges by key across layers; a key naming no registered effect, or a cap `<= 0`, is an error; `null` removes a default cap |
| `affixes` | list of affix entries | no | Merged by `id` (section 3) |

## Affix entry

| Field | Type | Required | Default | Validation |
|---|---|---|---|---|
| `id` | string | yes | | `^[a-z][a-z0-9_]{1,47}$`; unique per file; our own words. Stable forever: it is written into items |
| `name` | string | no | `$ecf_affix_<id>` | A `$` token is a localization key; anything else is literal text shown in every language (`localization.md` section 4) |
| `effect` | string | yes | | A registered effect id (`affixes.md` section 3) of this or an earlier phase. Error otherwise |
| `param` | string | per effect | | Required iff the effect takes one; must be valid for the effect's param kind. The `skill` kind accepts one `Skills.SkillType` name, a comma list (`Run,Jump,Swim,Sneak`) or `All`; `damage_type` accepts the groups `physical`, `elemental`, `all` |
| `value` | enum `percent` / `flat` / `flag` | yes | | Must be a value type the effect accepts |
| `unit` | enum `ms` / `deg` / `m` / `min` | no | none | Display unit for `flat` values only; error on `percent` or `flag` |
| `slots` | list of slot ids (one id accepted) | yes | | Non-empty; each one of `melee_weapon`, `ranged_weapon`, `magic_weapon`, `shield`, `head`, `chest`, `legs`, `cape`, `utility_item`, `tool` |
| `requires` | map | no | none | Narrower item filter, every key must hold: `skill: [SkillType...]` (any of, against the item's governing skills), `hands: one|two`, `traits: [wears_out|movement_penalty|builds|projectile|ammo|can_parry...]` (all of). Unknown names are errors; an affix no item in its slots can satisfy is a warning |
| `category` | enum `offense` / `defense` / `utility` | yes | | What the War / Warding / Fortune sigils steer by |
| `mythic_only` | bool | no | `false` | Only in the Mythic pool (`rarity.md`, `affixes-mythic.md`) |
| `condition` | enum `none` / `health_critical` | no | `none` | `health_critical`: applies only at or below the threshold; its channel sums and caps apart from the unconditional one |
| `exclusion_group` | string | no | none | snake_case. At most one affix of a group per item; an affix is always exclusive with itself |
| `weight` | number | no | `100` | `>= 0`. The affix's share of the first-stage draw (`rarity.md`). 0 stops new rolls; existing copies keep working |
| `enabled` | bool | no | `true` | `false`: never rolls, and existing copies are dormant (`item-data.md` section 6) |
| `hook` | enum `easy` / `medium` / `hard` | no | none | Documentation only; validated so a typo is caught |
| `tiers` | list of tier entries | yes | | At least one; each `tier` at most once; sorted by tier on load |

## Tier entry

| Field | Type | Required | Validation |
|---|---|---|---|
| `tier` | int 1-7 | yes | 1 meadows ... 7 ashlands. 8 (deep_north) is reserved and an error until shipped |
| `min`, `max` | number | yes for `percent` / `flat`; **error if present on a `flag`** | `min <= max`. A rolled value is rounded to the most decimals either bound is written with (at most 2): `3` and `5` roll integers, `1.5` rolls one decimal |
| `weight` | number | no, default `100` | `>= 0`. The tier's share of the second-stage draw inside the item's tier window (`rarity.md`) |

A **flag** affix lists one row per tier it exists at, `tier` and `weight` only. Warnings (applied, logged): every
weight 0 on the affix or all its tiers (it never rolls); tiers whose ranges go down as the tier goes up (usually a
typo); a `caps` key for a channel no enabled affix feeds.

## Worked examples

Shapes from the catalog (`affixes.md` section 4); the YAML defaults are generated from it.

A **percent** affix, capped through `caps`:

```yaml
caps:
  move_speed: 25

affixes:
  - id: fleetfoot
    effect: move_speed
    value: percent
    slots: [legs]
    category: utility
    exclusion_group: move_speed
    hook: easy
    tiers:
      - { tier: 1, min: 1, max: 2 }
      - { tier: 2, min: 2, max: 3 }
      - { tier: 3, min: 3, max: 4 }
      - { tier: 4, min: 4, max: 5 }
      - { tier: 5, min: 5, max: 6 }
      - { tier: 6, min: 6, max: 7 }
      - { tier: 7, min: 7, max: 8 }
```

A **flat** affix:

```yaml
  - id: broad_back
    effect: carry_capacity
    value: flat
    slots: [chest, utility_item]
    category: utility
    hook: easy
    tiers:
      - { tier: 1, min: 10, max: 15 }
      - { tier: 2, min: 15, max: 20 }
      - { tier: 3, min: 20, max: 30 }
      - { tier: 4, min: 30, max: 40 }
      - { tier: 5, min: 40, max: 50 }
      - { tier: 6, min: 50, max: 60 }
      - { tier: 7, min: 60, max: 75 }
```

A **flag** affix, from tier 4, rarer by design:

```yaml
  - id: ravens_glide
    effect: slow_fall
    value: flag
    slots: [cape]
    category: utility
    exclusion_group: fall
    weight: 30
    hook: easy
    tiers:
      - { tier: 4 }
      - { tier: 5 }
      - { tier: 6 }
      - { tier: 7 }
```

A **skill** affix: the `param` picks the skill, `requires` keeps it on items that skill governs:

```yaml
  - id: blade_mastery
    effect: skill_level
    param: Swords
    value: flat
    slots: [melee_weapon]
    requires: { skill: [Swords] }
    category: offense
    exclusion_group: skill_level
    hook: easy
    tiers:
      - { tier: 1, min: 2, max: 3 }
      - { tier: 2, min: 3, max: 5 }
      - { tier: 3, min: 5, max: 7 }
      - { tier: 4, min: 7, max: 9 }
      - { tier: 5, min: 9, max: 11 }
      - { tier: 6, min: 11, max: 13 }
      - { tier: 7, min: 13, max: 15 }
```

An owner's **override file**, `EliteCrafting_affixes_custom.yml`, changing two fields and adding one affix with a
literal name:

```yaml
caps:
  move_speed: 15

affixes:
  - id: fleetfoot
    weight: 50
  - id: myserver_trailblazer
    name: "Trailblazer"
    effect: move_speed_sprint
    value: percent
    slots: [legs]
    category: utility
    tiers:
      - { tier: 5, min: 4, max: 6 }
      - { tier: 7, min: 6, max: 9 }
```

---

# 7. Multiplayer

- Gameplay `.cfg` keys and both YAML families are server-owned while `Lock Configuration` is on and reach clients on
  join and on every change (Charter clauses and articles).
- The display, glow, confirm and diagnostics keys never sync, on any server, in any lock state.
- Rolls happen on different machines (drops on the creature's ZDO owner, stones on the item owner's client), so they
  must all use the same rules: that is what the lock guarantees, why every peer builds from the same texts with the
  same reader, and why the startup fallback publishes an empty file list rather than letting each peer fall back on
  its own.
- This works on a dedicated server the first time it is built: the server is the author, has no local player, and
  still reads, validates, publishes and hot-reloads both families.

---

# 8. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Configuration: CFG-1 to CFG-7; the plumbing
itself is U-11).

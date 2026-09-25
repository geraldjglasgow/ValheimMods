# EliteCrafting - specification: Item data

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **where a magic item's state lives and exactly how it is written**: the keys in the game's own
per-item custom data, how each value is encoded, which items may carry it, the parse cache every other feature
reads through, what happens to data the running configuration no longer knows, and how later versions migrate old
items. What the values *mean* belongs to other files: rarities `rarity.md`, affixes `affixes.md`, quality
`quality.md`, sealing and binding `stones.md`, pending sigils `sigils.md`, the tier ceiling `item-tier.md`.

**Status: Phase 1 built, not tested in game** (2026-09-23).

---

# 1. Where the state lives

**All item state is in `ItemDrop.ItemData.m_customData`, a `Dictionary<string, string>` the game already owns.**

Verified in the decompile (`assembly_valheim.dll`, 2026-09-23):

- `ItemData.Save(ZPackage)` writes every custom data pair; `ItemData.Load` reads them back. That one pair of
  methods serves the player's inventory (character file), containers, carts, ships, tombstones, item stands and
  armor stands (`ItemDrop.SaveToZDO`/`LoadFromZDO`) and dropped world items (the `ItemDrop`'s ZDO).
- `ItemData.Clone()` copies the dictionary (`new Dictionary<string,string>(m_customData)`), so dropping, picking up,
  moving and splitting keep it.
- `ItemData.Load` **replaces** the dictionary with a new instance on every load. The parse cache relies on this
  (section 5).

Consequence: **item state needs no custom netcode, no ZDO keys of its own and no save file of its own.** Whatever
the game does with an item, the state goes along.

## One exception the game gets wrong for us: workbench upgrades

`InventoryGui.DoCrafting`, on an upgrade, **removes the old item and adds a brand-new one** from the prefab
(`Inventory.AddItem(name, stack, quality, variant, crafterID, crafterName, position, cheated)`), which copies no
custom data. Unpatched, **upgrading a magic item at a workbench silently turns it Common.**

The mod therefore carries its keys across an upgrade (hook points from `~/scratch/specs/ec-game-notes.md` Q2):

- **Prefix** on `InventoryGui.DoCrafting`: when `m_craftUpgradeItem` has `ecf_` keys, copy them (only ours) and the
  recipe's prefab name into a static "pending upgrade" slot.
- **Postfix** on the 10-argument `Inventory.AddItem(string name, int stack, int quality, int variant, long
  crafterID, string crafterName, Vector2i position, bool cheated, bool pickedUp, bool dropIfFullInv)`: when the
  pending slot is set, the result is not null and `name` equals the pending prefab name, write the saved pairs into
  the result. The name check keeps the ingredients an upgrader "break" returns from receiving affixes.
- **Finalizer** on `DoCrafting` clears the slot, whatever happened.
- Fallback if the `AddItem` postfix proves unreliable: a `DoCrafting` postfix that writes into the item at the old
  grid position when its shared name matches.
- Upgrader stations: success and "failed, level reduced" both recreate the item and both get the pairs back;
  "broke" destroys it and there is nothing to write.
- Durability reset, the new crafter name, the new upgrade level and the item not being re-equipped are vanilla
  behaviour and stay. The unequip fires the aggregate rebuild (`effects-runtime.md`).

This is a Phase 1 requirement, not polish: without it the first workbench visit destroys the player's work.
Affix values do not scale with the upgrade level (user decision 2026-09-23); the upgrade only preserves them.

---

# 2. Which items may carry state: magic bases

An item is a **magic base** (the term `rarity.md` and `stones.md` use) when all of these hold:

1. `m_shared.m_maxStackSize == 1`. This is also what makes magic items non-stackable (section 7).
2. It maps to one slot id of the taxonomy (table below).
3. It is not one of our own stones.

The slot map is owned here (`rarity.md` section 2 defers to it). Slot classification from the item's shared data
(`../DECISIONS.md` ITD-1, ITD-2; `affixes.md` assigns affixes to these slot ids):

| Slot id | Rule | Examples |
|---|---|---|
| `melee_weapon` | `OneHandedWeapon`, `TwoHandedWeapon`, `TwoHandedWeaponLeft`, `Attach_Atgeir` with a melee skill (Swords, Knives, Clubs, Polearms, Spears, Axes, Unarmed) | swords, axes, atgeirs, knives, fists |
| `ranged_weapon` | `Bow` type, or skill Bows / Crossbows | bows, crossbows |
| `magic_weapon` | skill ElementalMagic or BloodMagic | staffs |
| `shield` | `Shield` | all shields |
| `head` | `Helmet` | |
| `chest` | `Chest` | |
| `legs` | `Legs` | |
| `cape` | `Shoulder` | capes |
| `utility_item` | `Utility` | belts, wishbone-like items |
| `tool` | `Tool`, and weapons whose skill is Pickaxes; fishing rods (skill Fishing) | hammer, hoe, cultivator, pickaxes, fishing rod |

Items outside the table (materials, food, ammo, trophies, torches, `Trinket`, `Hands`, `Customization`, `Misc`)
are never magic bases. A stone clicked onto one is refused with `$ecf_msg_not_magic_base` (`applying-stones.md`).
The game-notes survey suggested torches as `tool` and `Trinket` as `utility_item`; both stay excluded (ITD-2).
Hildir's cosmetic clothing follows its item type like any other armor (tier 1 by fallback, never in the drop pool,
since it has no recipe). Which prefabs actually fall where is checked in game with `ecraft dump items`
(`console-commands.md`), since prefab data lives in asset files, not code: a Phase 1 task.

Axes are `melee_weapon` even though they also chop: affix slot sanity (PLAN.md) puts tool effects on tools only,
and an axe is bought as a weapon. Pickaxes are `tool`. Both are judgement calls (ITD-1).

---

# 3. The keys

Every key starts with `ecf_`. The mod **only ever reads, writes or removes its own keys** and never clears the
dictionary: other mods' keys on the same item are left exactly as they were.

| Key | Present when | Value | Example |
|---|---|---|---|
| `ecf_v` | any other `ecf_` key is present | format version, integer | `1` |
| `ecf_rarity` | the item is Uncommon or better | rarity id | `rare` |
| `ecf_affixes` | the item has at least one affix | affix list (section 4) | `fleetfoot:3:4;broad_back:4:35` |
| `ecf_bound` | an affix is locked by the Stone of Binding | affix id of the locked affix | `broad_back` |
| `ecf_refine` | a Honing or Tempering Stone was used | the honed/tempered bonus in percent points, number (`quality.md`) | `7` |
| `ecf_sealed` | the item is sealed (no further stones) | reason id: `serpent` or `reflection` | `serpent` |
| `ecf_sigil` | a sigil is pending on the item | the sigil's stone id | `sigil_war` |

There is **no tier key**: the item's tier ceiling is computed from its base and never stored (user decision,
`../DECISIONS.md` U-13; `item-tier.md` section 1). Each affix stores its own tier inside `ecf_affixes`.

Rules that keep the set tight:

- **Common is the absence of `ecf_rarity`.** The mod never writes `ecf_rarity=common`. A Common item can still carry
  `ecf_v` and `ecf_refine` (the honed/tempered bonus survives Unmaking, `quality.md`).
- **A key whose value would be empty is removed**, not written empty. An item with no `ecf_` keys at all is a plain
  vanilla item and costs nothing anywhere.
- `ecf_v` is written whenever any other key is written, and removed when the last other key goes.
- `ecf_sealed` is a reason rather than a flag so the tooltip can say *why* ("Corrupted" / "Mirrored"). Any
  non-empty value means sealed; an unknown reason id displays as the generic sealed text.
- `ecf_bound` names an affix that must also be in `ecf_affixes`. If it is not (the affix was removed by a path that
  forgot to clear it), the key is ignored on read and dropped on the next write.

---

# 4. Encodings

**The affix list** (`ecf_affixes`): entries separated by `;`, fields by `:`, in display order (the order they were
rolled; a reroll that replaces an affix puts the new one at the end).

```
<affix id>:<tier>:<value>[;<affix id>:<tier>:<value>...]
```

- **Affix id**: the YAML id, matching `^[a-z][a-z0-9_]{1,47}$` (enforced at YAML load, `configuration.md`), so it
  can never contain a separator.
- **Tier**: integer 1..7, decimal digits.
- **Value**: the rolled value, already rounded at roll time (to the decimals of the tier's bounds,
  `configuration.md` section 6), so what is stored is exactly what is displayed and applied. Written with `CultureInfo.InvariantCulture` and the format `0.##`: no exponent, no
  group separator, `.` as decimal point, leading `-` for negatives, no `+`. Read with
  `float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)`. Never `ToString()` without a culture: a
  German client would write `6,5` and a second client would read it as garbage.
- **Flag affixes** store the value `1`.
- **Magnitudes are stored, not signs of intent**: a "costs less stamina" affix stores `8`, and its effect knows it
  reduces (`effects-runtime.md`). Negative values are legal (a YAML owner may define a drawback affix) and pass
  through unchanged.
- Duplicate ids in one list are invalid (PLAN.md: never two copies of one affix); on read the first is kept and the
  rest are treated as unreadable segments (below).

**Other values:** rarity id, sealed reason, sigil id: the lowercase snake_case ids from `rarity.md`, `stones.md`,
`sigils.md`. Refine percent: the same number format as affix values. Version: an invariant integer.

**Unreadable data is never destroyed.** A segment of `ecf_affixes` that does not parse (wrong field count, bad
number, duplicate) is kept verbatim in the parse record and written back unchanged, in its original position,
whenever the item is written. It has no effect and does not show in the tooltip except at the Full detail level
(`display.md`). An `ecf_rarity` value that is not a known rarity id is handled like an orphaned affix (section 6).

---

# 5. The parse cache

Every reader (tooltip, effects, stones, glow, loot, commands) goes through one cache; nothing parses
`m_customData` on its own.

- **`ItemRecord Read(ItemData item)`**: returns the parsed record, from a
  `ConditionalWeakTable<ItemData, ItemRecord>` cache. A record holds: format version, rarity (definition or
  orphaned id), the affix entries (id, tier, value, resolved definition or orphaned), unreadable segments, bound
  id, refine percent, sealed reason, sigil id.
- **Validity check on every hit**: the record remembers the dictionary instance it was parsed from. If
  `item.m_customData` is a different instance (the game's `Load` replaced it, or a clone), the record is rebuilt.
  One reference compare; no string work on a hit.
- **Invalidation on write**: `Write(ItemData item, ItemRecord record)` serializes the record into the dictionary and
  replaces the cache entry in the same call. There is no other write path.
- **Configuration change**: a successful affix or economy YAML apply bumps a global generation number; a record from
  an older generation re-resolves its definitions (orphaned or live) on its next read, without re-parsing strings.
- **Non-magic-base and plain items** get a shared empty record, so hot paths (a `GetDamage` postfix) can call `Read`
  on every item without allocating.
- **Records are immutable** to readers. Stones build a new record and hand it to `Write`.
- **ItemData identity is not stable**: the game clones an item on every slot move, container move, drop and
  pickup merge (game notes, pitfall 3), so cache misses are normal and nothing may hold a long-lived reference to an
  `ItemData` (the aggregate rebuilds from the equipped slots each time). A miss costs one parse of a short string.
  Allowed optimization: because `Clone` copies the dictionary but not the value strings, a second small cache keyed
  by the `ecf_affixes` string instance lets a clone reuse its original's parsed affix list.
- Reading **never writes**. Migration (section 8) and orphan handling happen in the record, and reach the
  dictionary only when something writes the item for its own reasons.

Performance invariant (PLAN.md): no parsing, reflection or allocation per frame. A cache hit is one CWT lookup and
one reference compare.

---

# 6. Orphaned data (user decision 2026-09-23)

An affix whose **id is not in the running affix configuration**, or whose definition has `enabled: false`, is
**orphaned**:

- The item **keeps the data**. Nothing strips, rewrites or "repairs" it.
- The tooltip shows it **dormant**: greyed, with its stored tier and value (`display.md`). Its name comes from
  `$ecf_affix_<id>` if a translation exists, otherwise the raw id.
- Its **effect is inert**: the effects layer skips it (`effects-runtime.md`).
- It **still counts** toward the item's affix count, so Growth cannot fill the item past its rarity maximum by
  ignoring dormant affixes. Stones treat it as an ordinary affix (Severing may remove it, Upheaval replaces it).
- **Restoring the id in YAML revives it** with its stored tier and value. No migration, no reroll.

`enabled: false` makes an affix dormant everywhere; an owner who only wants it to **stop rolling** sets its `weight` to 0
instead and existing copies keep working (`configuration.md`). That split is a judgement call (ITD-4).

A **tier that no longer exists** in the affix's definition is not an orphan: the value is stored, so the effect
applies as stored and the tooltip shows the stored tier. Only value rerolls need a range; `stones.md` defines which
tier they fall back to.

An **unknown rarity id** (an owner removed a rarity): the item keeps it, its name draws in the default text color,
the tooltip shows the raw id greyed, its affixes stay active, and every stone refuses it (`$ecf_msg_unknown_rarity`)
until the rarity returns.

---

# 7. Magic items never stack

The game merges items only when `m_maxStackSize > 1` (`Inventory.AddItem` → `FindFreeStackItem`, which compares
shared name, upgrade level and world level but **not** custom data). A magic base requires `m_maxStackSize == 1`, so a
magic item can never be merged into another, on the ground or in any inventory.

Two guards keep it that way:

- `Write` refuses (logs an error, writes nothing) an item with `m_maxStackSize > 1`. This catches another mod
  raising gear stack sizes after the fact.
- **Stones never carry `ecf_` data**; a stone's identity is its prefab. Stones stack by prefab only.

**Removing the mod** leaves every `ecf_` key on gear untouched (the game round-trips unknown pairs), so reinstalling
restores everything; stones, being our prefabs, are deleted by the game on the next load (game notes, pitfall 6).

If a server raises stack sizes of weapons or armor (OpenKeep's stack rules can), those items stop being magic bases:
the ones already magic stay magic (the data is kept and read) but no stone applies to them. Stated in the README.

---

# 8. Format versions and migration

- **Current version: 1.** `ecf_v` absent while other `ecf_` keys exist is read as version 1 (defensive; the mod
  always writes it).
- **Older version on read**: the parser runs the chain of migration steps v1→v2→...→current in memory, each a pure
  function over the item's `ecf_` pairs. The record is in current form; the dictionary is untouched until the item
  is next written, which then writes current form and the current `ecf_v`. Reading a chest of old items therefore
  causes no writes and no ZDO traffic.
- **Newer version on read** (a client with an older mod saw an item from a newer one; normally prevented by the
  version check, but save files travel): the record parses what it can, the effects of parseable affixes apply,
  the tooltip adds `$ecf_ui_newer_format`, and **every stone refuses** (`$ecf_msg_newer_format`), because writing it
  back would lose whatever the newer format added.
- **When a new version is needed**: any change to the meaning or encoding of an existing key. Adding a new optional
  key is **not** a version bump (older readers ignore keys they do not know and never delete them).
- A version bump is a MAJOR release per workspace CLAUDE.md only if old items can no longer be read; a migration
  step that reads them keeps it MINOR.
- Every migration step ships with a unit test holding a frozen v(n) dictionary and its expected v(n+1) form.

---

# 9. Worked examples

A plain vanilla item: `m_customData = {}`. Nothing of ours, zero cost.

An Uncommon bronze sword, one affix:

```
{
  "ecf_v":       "1",
  "ecf_rarity":  "uncommon",
  "ecf_affixes": "balanced_grip:2:7"
}
```

A Legendary chest piece: four affixes, one bound, tempered, a sigil pending; another mod's key alongside:

```
{
  "ecf_v":       "1",
  "ecf_rarity":  "legendary",
  "ecf_affixes": "broad_back:6:52;troll_blood:5:12.5;well_forged:6:18;lightened:4:30",
  "ecf_bound":   "troll_blood",
  "ecf_refine": "6",
  "ecf_sigil":   "sigil_warding",
  "othermod_x":  "left alone"
}
```

A Mythic sealed by the Serpent Stone, seven affixes (the Serpent's "add one on Mythic" outcome), one flag affix, one
dormant (`storm_ward`, an affix the server's YAML no longer has):

```
{
  "ecf_v":       "1",
  "ecf_rarity":  "mythic",
  "ecf_affixes": "ravens_glide:7:1;well_forged:7:40;storm_ward:6:20;lightened:7:45;everlasting:7:1;broad_back:7:70;gossamer:7:1",
  "ecf_sealed":  "serpent"
}
```

A Common hammer after Unmaking, its refine bonus kept:

```
{
  "ecf_v":       "1",
  "ecf_refine": "4"
}
```

A mirrored copy from the Stone of Reflection (the original is unchanged and unsealed):

```
{
  "ecf_v":       "1",
  "ecf_rarity":  "rare",
  "ecf_affixes": "long_reach:5:8;staggering_blows:5:11;balanced_grip:3:9",
  "ecf_sealed":  "reflection"
}
```

A damaged list (hand-edited save): `"fleetfoot:3:4;garbage;broad_back:4:35"` parses to two affixes plus one
unreadable segment `garbage`, which is written back in place on the next write.

Affix ids are catalog ids (`affixes.md`) used to show the shape; the combinations are not checked for slot legality.

---

# 10. Multiplayer

- The state is replicated by the game wherever the item goes (section 1). A value written on the client that owns
  the item reaches every other peer the moment the item is next serialized (dropped, put in a chest, traded).
- **Items equipped by another player are not visible to this client.** Their data lives in that player's
  inventory, which the game does not replicate; `VisEquipment` carries prefab hashes only. Nothing in Phases 1-2
  needs it. Phase 3's per-rarity glow on equipped gear will need a small per-player ZDO summary, specified then.
- Writes happen only where the item is owned: the local player's inventory (stones, upgrades), or the ZDO owner of a
  freshly spawned world drop (pre-rolled loot, `drops.md`). No peer ever writes an item it does not own.

---

# 11. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Item data: ITD-1 to ITD-5; the tier model and the
dropped `ecf_tier` key are U-13). Binding "once per item" means one bound affix at a time, so no lifetime marker key
exists (ITD-3).

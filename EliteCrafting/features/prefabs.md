# EliteCrafting - specification: Prefabs

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the rune items as game objects**: how the prefabs are cloned from vanilla items and registered
on every peer, their names, how they are told apart in the world and in the inventory (base mesh per group, tint,
the base's own lights), and their icons. What each rune does is `stones.md` (the code's word for a rune is stone);
its YAML entry is `economy-yaml.md`.

**Status: Phase 1 built, not tested in game** (2026-09-23). Base prefab names are **believed vanilla, not yet verified in game** (section 4); fallbacks per group are coded (DECISIONS.md IMP-15). ItemCopies is not used (IMP-1).

---

# 1. What exists

- **Six rune prefabs**, one per rune id (table in section 3).
- Nothing else: no prefab for magic items (they are vanilla items with data, `item-data.md`), none for effects.

All of them exist on every peer, always, whatever the YAML says. A disabled or undefined rune still has its
prefab, so stacks of it in chests and inventories are never deleted by the game (an unknown prefab is dropped from an
inventory on load and a world ZDO with an unknown prefab is destroyed by the server; `multiplayer.md` section 5).

---

# 2. Cloning and registration

From the game-notes survey (`~/scratch/specs/ec-game-notes.md` Q18), verified against the decompile:

**Building a clone** (once per process, under a `DontDestroyOnLoad` holder):

1. Find the base prefab by name in whichever database is ready first (`ObjectDB.m_items` or `ZNetScene.m_prefabs`).
2. Instantiate it **under an inactive parent** so no `Awake` runs: no ZDO is created and the game's live item list is
   not touched. (`ZNetView.m_forceDisableInit` is wrong here: it destroys the prefab's `ZNetView`.) Instances later
   spawned from the clone are active, because instantiation does not copy the parent.
3. Name it `ECF_<PascalId>`: no spaces or parentheses, since the game's prefab hash cuts the name at the first one.
4. Configure its own `SharedData` copy (instantiation gives the clone its own): `m_name` `$ecf_stone_<id>`,
   `m_description` `$ecf_stone_<id>_desc`, `m_icons` (section 6), `m_maxStackSize` 50, `m_weight` 0.2,
   `m_teleportable` true, `m_itemType` Material, **`m_value` 0** (the Ruby and Amber bases sell to the trader), and
   cleared `m_subtitle`, `m_dlc`, `m_questItem`, consume/equip/set status effects, `m_appendToolTip` and food values.
5. Set `m_itemData.m_dropPrefab` to the clone.
6. Give every renderer its own material instances before tinting (section 4), so the vanilla base item never changes.

**Registering** (every peer: server, host, clients; identical because it is code only):

- Postfix on `ObjectDB.Awake` **and** `ObjectDB.CopyOtherDB`: add each clone to `m_items` only if no item of that
  name is present, then `UpdateRegisters()`. Both registries use `Dictionary.Add` and throw on a duplicate, and the
  main-menu database shares the prefab asset's list, so the name check runs every time.
- Prefix on `ZNetScene.Awake`: append each clone to `m_prefabs` if absent, so the game's own loop registers it.
- This covers the main menu (character preview, profile load), single player, dedicated server and clients, and it
  happens before any inventory loads or any ZDO arrives, which is what join-in-progress needs.

**YAML on top**: the economy family's rune `name`, `description`, `stack`, `item_weight` and `tint`
(`economy-yaml.md` section 4) are written to the registered prefabs each time the family applies
(`configuration.md` section 5). Every live copy reaches them because each rune item is re-linked to its prefab's one
`SharedData` when it wakes (`ItemDrop.Awake` postfix); the ItemCopies library is not used (`../DECISIONS.md` IMP-1,
superseding RC-11). A rune stack that loads larger than the current max stack is kept whole (IMP-7). Spawned runes
get the world's `m_worldLevel`, like vanilla spawns, or they would not stack with dropped ones.

**Dedicated server**: registers every prefab (it needs them for ZDOs and inventories) but skips material tinting
and icon generation (no graphics device).

---

# 3. The names

| Rune id | Prefab | Group |
|---|---|---|
| `awakening` | `ECF_Awakening` | ascension |
| `shaping` | `ECF_Shaping` | manipulation |
| `ascension` | `ECF_Ascension` | ascension |
| `consecrated` | `ECF_Consecrated` | manipulation |
| `cleansing` | `ECF_Cleansing` | manipulation |
| `serpent` | `ECF_Serpent` | risk |

Six in total: `ECF_` + the PascalCase id (`StoneCatalog.PrefabFor`).

---

# 4. Telling them apart

**v1 needs no asset authoring.** A clone keeps its base's mesh and texture, supplied by the game on every client;
nothing of Iron Gate's ships in our zip. Three levers:

**(a) One vanilla base per group.** From the game-notes survey of the decompile's item code; the prefab names live
in asset bundles, so **every row must be verified in game during Phase 1** (`ecraft dump items` lists every item with
its type, weight, stack, value and whether it has light or particle children; `../DECISIONS.md` PRF-1).

| Group | Runes | Base (verify in game) | Alternates | Why |
|---|---|---|---|---|
| ascension | Awakening, Ascension | `Ruby` | `Amber`, `AmberPearl` | precious-looking: the two runes that raise the rarity |
| manipulation | Shaping, Consecrated, Cleansing | `Crystal` | `DragonTear`, `Thunderstone` | crystal: the everyday crafting currency |
| risk | Serpent | `SurtlingCore` | `BlackCore` | glowing core: visibly dangerous |

A group whose base and alternates are all missing uses any other group's base, with a warning; a missing base never
throws. The Ruby and Amber bases carry a trade value in vanilla; the clone overrides it (section 2).

**(b) Runtime tint per rune**: the clone's own material instances get a color and, where the shader has it,
`_EmissionColor` (the property the game's own code tints; `_Color` also appears; both checked with `HasProperty`).
Defaults (judgement calls, PRF-3, overridable by the rune entry's `tint`, `economy-yaml.md` section 4):

| Rune | Tint |
|---|---|
| Awakening, Ascension | the color of the rarity each one **produces**, read from the rarity palette at apply time: Awakening green `#1EFF00` (Magic), Ascension blue `#0070DD` (Rare) |
| Shaping | teal `#2EC4B6` |
| Consecrated | gold `#E6C35C` |
| Cleansing | pale silver `#D8E4EE` |
| Serpent | venom green `#3F7F2A` |

Deriving the promote runes' tints from the rarity palette means a server that rethemes its rarities rethemes the
runes that make them, with no second edit.

**(c) The base's own lights**: a clone keeps whatever light or particle children its base has (the risk base
`SurtlingCore` is believed to carry one, which suits the Serpent Rune); its lights take the rune's tint. Nothing is
added.

**Ground glow**: runes do not use the magic-item ground glow by default (their tint and emission carry them); the
per-player preference `Glow runes` opts them in, in their tint (`display.md` section 5).

---

# 5. A fixed set: no runes from YAML

The six runes are the whole set. The YAML tunes or disables them; it cannot add one (`stones.md` section 5):

- A prefab cannot come from YAML: prefabs must exist on every peer **before** any world data arrives, and the
  server's YAML reaches a joining client only after its scene (and `ZNetScene`) is already up.
- Validation (economy family): a `runes:` entry whose id is not one of the six is an error; an entry may name no
  `prefab` but its own.
- A stack of a rune whose definition was disabled or removed survives (the prefab still exists) and is refused with
  `stone_disabled` until the definition returns.

---

# 6. Icons

**v1: runtime-tinted vanilla sprites**, matching the world model's tint.

- At registration on a client (not on a dedicated server), the base item's icon sprite is copied and tinted once,
  and the clone's `m_icons` set to the tinted copy. Game textures are usually not CPU-readable, so the copy goes
  through the GPU (a blit into a render texture with a tint, then read back into a new texture) rather than
  `GetPixels`.
- Awakening and Ascension icons re-tint when the rarity palette changes (economy YAML apply), like their models.
- A base icon the GPU copy cannot read keeps its untinted icon, with a warning.
- Original icon art: Phase 3.

---

# 7. Phase 3 upgrade path

Our own meshes, either authored on the Mac and shipped as our own AssetBundle embedded in the DLL (our art, fine for
the clean room), or generated procedurally in code (low-poly crystals). Either is swapped in at step 2 of the clone
build; prefab names, hashes, YAML and item data do not change, so existing stacks carry over.

---

# 8. Multiplayer

- Registration is code-only and identical on every peer, so every peer agrees on every prefab name and hash before
  any ZDO or inventory arrives; a join-in-progress client needs nothing extra.
- Tint, icons and lights are drawn locally on each client from the registered clone; the dedicated server renders
  nothing.
- The mod is required on every peer (`multiplayer.md` section 5) because of these prefabs.

---

# 9. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Prefabs: PRF-1 to PRF-4; surviving stacks of
disabled runes RC-3) and the 2026-10-02 entry of `../PLAN.md`'s Decisions log (six runes only). PRF-1 is a
verification task for Phase 1, not an open choice.

# EliteCrafting - specification: Prefabs

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the stone items as game objects**: how the prefabs are cloned from vanilla items and registered
on every peer, their names, how they are told apart in the world and in the inventory (base mesh per group, tint,
scale, reused effects), and their icons. What each stone does is `stones.md`; its YAML entry is `economy-yaml.md`.

**Status: Phase 1 built, not tested in game** (2026-09-23). Base prefab names are **believed vanilla, not yet verified in game** (section 4); fallbacks per group are coded (DECISIONS.md IMP-15). ItemCopies is not used (IMP-1).

---

# 1. What exists

- **27 built-in stone prefabs**, one per stone id of the conventions (table in section 3).
- **16 reserved custom prefabs**, `ECF_Custom01` ... `ECF_Custom16`, for stones a server owner defines in YAML
  (section 5).
- Nothing else: no prefab for magic items (they are vanilla items with data, `item-data.md`), none for effects.

All of them exist on every peer, always, whatever the YAML says. A disabled or undefined stone still has its
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
   `m_teleportable` true, `m_itemType` Material, **`m_value` 0** (gem bases sell to the trader), and cleared
   `m_subtitle`, `m_dlc`, `m_questItem`, consume/equip/set status effects, `m_appendToolTip` and food values.
5. Set `m_itemData.m_dropPrefab` to the clone.
6. Give every renderer its own material instances before tinting (section 4), so the vanilla base item never changes.

**Registering** (every peer: server, host, clients; identical because it is code only):

- Postfix on `ObjectDB.Awake` **and** `ObjectDB.CopyOtherDB`: add each clone to `m_items` only if no item of that
  name is present, then `UpdateRegisters()`. Both registries use `Dictionary.Add` and throw on a duplicate, and the
  main-menu database shares the prefab asset's list, so the name check runs every time.
- Prefix on `ZNetScene.Awake`: append each clone to `m_prefabs` if absent, so the game's own loop registers it.
- This covers the main menu (character preview, profile load), single player, dedicated server and clients, and it
  happens before any inventory loads or any ZDO arrives, which is what join-in-progress needs.

**YAML on top**: the economy family's stone `name`, `description`, `stack`, `item_weight` and `tint`
(`economy-yaml.md` section 4) are written to the registered prefabs each time the family applies
(`configuration.md` section 5). Every live copy reaches them because each stone item is re-linked to its prefab's one
`SharedData` when it wakes (`ItemDrop.Awake` postfix); the ItemCopies library is not used (`../DECISIONS.md` IMP-1,
superseding RC-11). A stone stack that loads larger than the current max stack is kept whole (IMP-7). Spawned stones get the world's `m_worldLevel`, like vanilla spawns, or they
would not stack with dropped ones.

**Dedicated server**: registers every prefab (it needs them for ZDOs and inventories) but skips material tinting
and icon generation (no graphics device).

---

# 3. The names

| Stone id | Prefab | Group | Grade |
|---|---|---|---|
| `awakening` | `ECF_Awakening` | ascension | |
| `ascension` | `ECF_Ascension` | ascension | |
| `exaltation` | `ECF_Exaltation` | ascension | |
| `transcendence` | `ECF_Transcendence` | ascension | |
| `apotheosis` | `ECF_Apotheosis` | ascension | |
| `growth_lesser` | `ECF_GrowthLesser` | manipulation | lesser |
| `growth_greater` | `ECF_GrowthGreater` | manipulation | greater |
| `turmoil_lesser` | `ECF_TurmoilLesser` | manipulation | lesser |
| `turmoil_greater` | `ECF_TurmoilGreater` | manipulation | greater |
| `upheaval_lesser` | `ECF_UpheavalLesser` | manipulation | lesser |
| `upheaval_greater` | `ECF_UpheavalGreater` | manipulation | greater |
| `perfection_lesser` | `ECF_PerfectionLesser` | manipulation | lesser |
| `perfection_greater` | `ECF_PerfectionGreater` | manipulation | greater |
| `severing_lesser` | `ECF_SeveringLesser` | manipulation | lesser |
| `severing_greater` | `ECF_SeveringGreater` | manipulation | greater |
| `unmaking` | `ECF_Unmaking` | manipulation | |
| `serpent` | `ECF_Serpent` | risk | |
| `binding` | `ECF_Binding` | risk | |
| `chance` | `ECF_Chance` | risk | |
| `reflection` | `ECF_Reflection` | risk | |
| `honing` | `ECF_Honing` | quality | |
| `tempering` | `ECF_Tempering` | quality | |
| `sigil_preservation` | `ECF_SigilPreservation` | sigil | |
| `sigil_war` | `ECF_SigilWar` | sigil | |
| `sigil_warding` | `ECF_SigilWarding` | sigil | |
| `sigil_fortune` | `ECF_SigilFortune` | sigil | |
| `sigil_culling` | `ECF_SigilCulling` | sigil | |

27 in total. Plus `ECF_Custom01`-`ECF_Custom16` (section 5).

---

# 4. Telling them apart

**v1 needs no asset authoring.** A clone keeps its base's mesh and texture, supplied by the game on every client;
nothing of Iron Gate's ships in our zip. Four levers:

**(a) One vanilla base per group.** From the game-notes survey of the decompile's item code; the prefab names live
in asset bundles, so **every row must be verified in game during Phase 1** (`ecraft dump items` lists every item with
its type, weight, stack, value and whether it has light or particle children; `../DECISIONS.md` PRF-1).

| Group | Base (verify in game) | Alternates | Why |
|---|---|---|---|
| ascension | `Ruby` | `Amber`, `AmberPearl` | gem-like: the ladder backbone reads as precious |
| manipulation | `Crystal` | `DragonTear`, `Thunderstone` | crystal shard: the everyday crafting currency |
| risk | `SurtlingCore` | `BlackCore` | glowing core: visibly dangerous |
| sigil | `Flint` | `BlackMarble` (`Coins` rejected: reads as money) | flat, rune-like |
| quality | `Stone` (Honing), `IronScrap` (Tempering) | `IronNails`, `CopperScrap` | whetstone and metal scrap |
| custom pool | 4 prefabs on each of ascension, manipulation, risk, sigil bases | | section 5 |

Ore and scrap bases are believed non-teleportable and gems carry a trade value in vanilla; the clone overrides both
(section 2).

**(b) Runtime tint per stone**: the clone's own material instances get a color and, where the shader has it,
`_EmissionColor` (the property the game's own code tints; `_Color` also appears; both checked with `HasProperty`).
Defaults (judgement calls, PRF-3, overridable by the stone entry's `tint`, `economy-yaml.md` section 4):

| Stones | Tint |
|---|---|
| Ascension stones | the color of the rarity each one **produces**, read from the rarity palette at apply time: Awakening green `#1EFF00`, Ascension blue `#0070DD`, Exaltation purple `#A335EE`, Transcendence orange `#FF8000`, Apotheosis red `#E6262E` |
| Growth | teal `#2EC4B6` |
| Turmoil | amber `#FFB000` |
| Upheaval | magenta `#D1307A` |
| Perfection | gold `#FFD700` |
| Severing | steel `#9FB4C7` |
| Unmaking | ash `#5A5A5A` |
| Serpent | venom green `#3F7F2A` |
| Binding | silver `#C0C0C0` |
| Chance | pink `#FF69B4` |
| Reflection | pale cyan `#E0FFFF` |
| Honing / Tempering | none (the base's own look) |
| Sigils | War `#C0392B`, Warding `#2E86DE`, Fortune `#27AE60`, Preservation `#8E44AD`, Culling `#7F8C8D` |

Deriving the ascension tints from the rarity palette means a server that rethemes its rarities rethemes the stones
that make them, with no second edit.

**(c) Scale per grade**: lesser × 0.85, greater × 1.15, ungraded × 1.0 of the base's scale (judgement call).

**(d) Reused vanilla effects for the rare ones**: Apotheosis, Reflection and the Serpent Stone keep (or gain) a
vanilla glow or particle child where their base has one (the risk base `SurtlingCore` is believed to carry one).
Which vanilla effect to reuse for Apotheosis and Reflection is **TBD - see `ecraft dump items` in game**; nothing is
invented until the survey shows what exists.

**Ground glow**: stones do not use the magic-item ground glow by default (their tint and emission carry them); the
per-player preference `Glow stones` opts them in, in their tint (`display.md` section 5).

---

# 5. Owner-defined stones: the reserved pool

`stones.md` section 21 lets a server owner add a stone in YAML with one of the fixed verbs. Such a stone cannot get a
prefab of its own name: prefabs must exist on every peer **before** any world data arrives, and the server's YAML
reaches a joining client only after its scene (and `ZNetScene`) is already up. A server whose YAML failed validation
at startup would even destroy those stones' world ZDOs.

So owner-defined stones bind to the **reserved pool**:

- `ECF_Custom01`-`04` on the ascension base, `05`-`08` manipulation, `09`-`12` risk, `13`-`16` sigil.
- The stone entry names one: `prefab: ECF_Custom06` (and usually a `tint` and a literal `name`; example in
  `economy-yaml.md` section 4).
- Validation (economy family): a built-in stone id must use its own prefab; an owner-defined id must use a reserved
  one; two stones naming the same prefab is an error.
- An unused reserved prefab never drops and cannot be obtained except by `ecraft give`, which refuses it. A stack
  of a reserved prefab whose stone definition was removed survives (the prefab still exists) and is refused with
  `stone_disabled` until a definition returns.

Reconciled with `stones.md` section 21 (`../DECISIONS.md` RC-4).

---

# 6. Icons

**v1: runtime-tinted vanilla sprites**, matching the world model's tint.

- At registration on a client (not on a dedicated server), the base item's icon sprite is copied and tinted once,
  and the clone's `m_icons` set to the tinted copy. Game textures are usually not CPU-readable, so the copy goes
  through the GPU (a blit into a render texture with a tint, then read back into a new texture) rather than
  `GetPixels`.
- Grades: the greater stone's icon gets a brighter tint than the lesser one of the same family (judgement call).
- Ascension icons re-tint when the rarity palette changes (economy YAML apply), like their models.
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
- Tint, scale, icons and effects are drawn locally on each client from the registered clone; the dedicated server
  renders nothing.
- The mod is required on every peer (`multiplayer.md` section 5) because of these prefabs.

---

# 9. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Prefabs: PRF-1 to PRF-4; the reserved pool is
RC-4, surviving stacks of disabled stones RC-3). PRF-1 is a verification task for Phase 1, not an open choice.

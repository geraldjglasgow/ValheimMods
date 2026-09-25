# EliteCrafting - specification: Localization

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **every player-facing word**: the key scheme, where the English defaults live, how the game's
localization receives them, and how translators and server owners override them.

**Status: Phase 1 built, not tested in game** (2026-09-23).

---

# 1. The key scheme

All keys start with `ecf_` (written `$ecf_...` in texts, as the game does). Ids are the snake_case ids from the
conventions, so a key can be derived from data without a lookup table.

| Pattern | For | Example |
|---|---|---|
| `$ecf_affix_<affix id>` | affix name | `$ecf_affix_fleetfoot` = "Fleetfoot" |
| `$ecf_affix_<affix id>_line` | the affix's tooltip sentence, `$1` the value, `$2` a composite second half (optional; `display.md`) | `$ecf_affix_fleetfoot_line` = "You move $1% faster" |
| `$ecf_stone_<stone id>` | stone (and sigil) item name | `$ecf_stone_growth_lesser` = "Lesser Stone of Growth" |
| `$ecf_stone_<stone id>_desc` | stone item description (vanilla tooltip body) | |
| `$ecf_rarity_<rarity id>` | rarity word | `$ecf_rarity_legendary` = "Legendary" |
| `$ecf_family_<family id>` | essence family name (`essences.md` section 8) | `$ecf_family_venom` = "Venom" |
| `$ecf_fragment_<shard id>` | salvage shard item name (`salvage.md` section 5) | `$ecf_fragment_shard_ascension` = "Shard of Ascension" |
| `$ecf_fragment_<shard id>_desc` | shard item description | |
| `$ecf_msg_<id>` | refusals and feedback messages | `$ecf_msg_sealed` |
| `$ecf_ui_<id>` | tooltip labels, console-facing words, category and slot names | `$ecf_ui_dormant` |

Sub-schemes inside `$ecf_ui_`, so that data ids map to words mechanically:

- `$ecf_ui_category_<offense|defense|utility>`
- `$ecf_ui_slot_<slot id>`
- `$ecf_ui_sealed_<reason id>`: `serpent`, `reflection`

Essences are stones, so their names are stone keys: `$ecf_stone_essence_<family>_<grade>` and `..._desc`. The English
words of essences and salvage ship in their own files, `English.essences.yml` and `English.salvage.yml` (the
translations folder holds one file per area; the loader reads every `English.*.yml`).

Rules:

- **Our own words only.** No key, and no English text, is taken from another mod (CLEANROOM.md).
- Placeholders are the game's: `$1`, `$2`, `$3`, filled by `Localization.Localize(text, words)`, in every feature
  file and in the shipped English text; never `{0}` (`../DECISIONS.md` RC-1).
- A key is never reused for a different meaning. Renaming a key is a breaking change for translators; add a new one
  instead.
- Every id a YAML entry introduces gets its key automatically (`$ecf_affix_<id>`); an entry may instead carry a
  literal `name` (section 4).

---

# 2. English defaults

- Shipped **embedded in the DLL** as one flat YAML map, `EliteCrafting.translations.English.yml` (key without `$` →
  text). One file rather than code constants because the affix catalog alone is well over a hundred names, and a
  translator starts by copying it.
- Loaded once at plugin Awake into a dictionary; nothing reads it from disk.
- Handed to the game with `Localization.AddWord` (key without `$`) in a postfix on `Localization.SetupLanguage`
  (every language change) and at once in plugin Awake if the game's localization already exists, the pattern
  OpenKeep's `Core/Language.cs` uses. After adding, the game's localization cache (`m_cache`) is evicted: it stores
  misses too, so a tooltip drawn before the words arrived would otherwise keep showing `[ecf_...]`.
- English is always installed first, then the current language's words over it, so **a missing translation falls
  back to English**, never to a raw `$key`.

The general keys this file owns (the other feature files list their own `$ecf_msg_` and `$ecf_ui_` keys):

| Key | English |
|---|---|
| `ecf_ui_affix_line` | `$1 $2` (value, name) |
| `ecf_ui_tier` | `T$1` |
| `ecf_ui_range` | `[$1-$2]` |
| `ecf_ui_bound` | `[Bound]` |
| `ecf_ui_dormant` | `(dormant)` |
| `ecf_ui_unreadable` | `Unreadable: $1` |
| `ecf_ui_newer_format` | `Changed by a newer version of EliteCrafting` |
| `ecf_ui_sealed` | `Sealed: $1` |
| `ecf_ui_sealed_serpent` | `Corrupted` |
| `ecf_ui_sealed_reflection` | `Mirrored` |
| `ecf_ui_tier_ceiling` | `Tier ceiling $1` |
| `ecf_ui_category_offense` / `_defense` / `_utility` | `Offense` / `Defense` / `Utility` |
| `ecf_ui_slot_melee_weapon` ... `ecf_ui_slot_tool` | `Melee weapon`, `Ranged weapon`, `Magic weapon`, `Shield`, `Head`, `Chest`, `Legs`, `Cape`, `Utility item`, `Tool` |
| `ecf_rarity_common` ... `ecf_rarity_mythic` | `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythic` |
| `ecf_msg_newer_format` | `This item was changed by a newer version of EliteCrafting.` |
| `ecf_msg_unknown_rarity` | `This item's rarity is not known on this server.` |

The other refusal and feedback texts (`not_own_inventory`, `not_magic_base`, `stone_disabled`, `sealed`,
`equipped`, `wrong_rarity`, `not_enough_stones`, `confirm_required`, `$ecf_ui_confirm_title` / `_body`, the per-stone
ones) are listed with their English in `stones.md` sections 2-3, `sigils.md` and `quality.md`, already in the
game's `$1`, `$2` placeholder form (`applying-stones.md` section 2).

Console output (`console-commands.md`) is **not** localized: it is a diagnostic tool, its output is pasted into bug
reports and YAML, and it must read the same for everyone (`../DECISIONS.md` LOC-3).

---

# 3. Translations and player overrides

- A translation is a flat YAML map in the config folder named `EliteCrafting.translations.<Language>.yml`, where
  `<Language>` is the game's language name (`German`, `French`, `Portuguese_Brazilian`...). Keys with or without the
  leading `$`. The name does not match either YAML family pattern, so the file is never synced or merged with them.
- Loaded when the game sets up that language, and again on `ecraft reload`. Invalid YAML or a non-map is one warning
  line; individual bad entries are skipped with a warning.
- **Per player, never synced.** Language is the player's choice. A server does not push translations.
- Order of precedence for a key: the player's translation file for the current language > a translation embedded in
  the DLL for that language (future releases may ship some) > the embedded English.
- The mod writes no translation file. A translator copies the English map, whose source is in the repository at
  `EliteCrafting/EliteCrafting/translations/English.yml` (the file the embedded resource is built from) and is
  linked from the README.

---

# 4. Names server owners add

An owner who adds an affix or a stone in YAML has no translation file on every player's machine. So:

- An affix's `name` (and a stone's `name`, `economy-yaml.md`) is either a `$key` or **literal text**. Literal text is
  shown as written, in every language, and arrives with the synced YAML.
- With no `name`, the key `$ecf_affix_<id>` is used; if no translation provides it, the tooltip shows the id itself,
  so an unnamed custom affix is still identifiable.
- A `$key` of the owner's own (say `$myserver_affix_frostbite`) works only for players whose translation file
  provides it, and falls back to the raw key otherwise. Documented as the advanced route.

---

# 5. Multiplayer

- Keys and English defaults are in the DLL, identical on every peer.
- Translation files are local and per player; two players on one server may read the same tooltip in two languages.
- Literal names come with the server's synced YAML, so every player sees the owner's words for custom content.
- The dedicated server needs no words at all (it draws nothing); it loads the dictionary anyway for `inspect`-style
  log output, which is cheap.

---

# 6. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Localization: LOC-1 to LOC-3). Decompiling the
game's `Localization` and `UITooltip` classes from `assembly_guiutils.dll` is allowed (user decision U-12,
`../../CLEANROOM.md`).

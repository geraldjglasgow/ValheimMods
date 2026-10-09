# Configuration and Commands

Files are in `BepInEx/config` and apply without a restart:

- `com.EliteCrafting.cfg`: the settings below.
- `EliteCrafting_inscriptions.yml`: every inscription (prefix or suffix, item classes, tier ladder, weight) and the caps.
- `EliteCrafting_economy.yml`: rarities and their prefix and suffix limits, rolling rules, runes, item classes, item levels and drop tables.

## Multiplayer

- Server and every client need the same version.
- With `Lock Configuration` on, the server's synced settings and YAML files apply to everyone.
- Admins can change the server's synced settings from their own game, for example with a configuration manager.
- `charter status` shows whether the server binds your settings, `charter diff` which of your values differ, `charter versions` your mod versions beside the server's.

## Settings

Synced settings come from the server; player settings are your own.

| Setting | Default | Scope | Meaning |
|---|---|---|---|
| Lock Configuration | On | Server | Everyone uses the server's settings and YAML files |
| Inscription effects | On | Synced | Off: inscriptions stay on items but do nothing |
| Modify equipped items | On | Synced | Runes work on equipped items |
| Runes from salvage | On | Synced | With OpenKeep, salvaging Magic gear may give back an Awakening Rune, Rare gear an Ascension Rune (25%) |
| Rune Table | On | Synced | The [Rune Table](wiki:Rune Table) is in the hammer and can be used. Off: it leaves the hammer and built tables cannot be used; what they hold is kept |
| Gems and sockets | On | Synced | Sockets on weapons, staves, armour, shields and backpacks; the Dvergr Chisel and the eleven gems drop and work; the Rune Table's Sockets tab. Off: none of it; gems already set keep their stats |
| Confirm destructive runes | HoldShift | Player | How Cleansing and the Sealed Rune ask first: `HoldShift`, `Dialog` (yes/no) or `Off` |
| Rune drops | On | Synced | Creatures, bosses and chests drop runes |
| Read-only commands for everyone | On | Synced | Off: `help`, `inspect`, `stats`, `list`, `inscription`, `classes` and `ecr` become admin-only |
| Colored item names | On | Player | Magic item names in their rarity colour |
| Tooltip detail | Standard | Player | `Compact` (no tiers), `Standard` (tiers) or `Full` (also value ranges, the item's class and level) |
| Show dormant inscriptions | On | Player | Show switched-off inscriptions, greyed |
| Ground glow | On | Player | Magic items on the ground glow |
| Glow intensity | 1 | Player | Brightness, 0-3 |
| Glow range | 2 | Player | Radius in metres, 0.5-6 |
| Glow max lights | 25 | Player | Only the nearest this many items glow, 0-100 |
| Glow refresh seconds | 1 | Player | How often the nearest are re-chosen, 0.25-5 |
| Glow runes | Off | Player | Runes on the ground glow too |
| Loot beam | On | Player | Glowing items also show a beam of light with rising sparks |
| Log rolls | Off | Player | Write every roll to the log |
| Log effect rebuilds | Off | Player | Write inscription totals to the log when they change |
| Synergy | Off | Synced | Elite Creatures Reborn's stars and world tier raise drops |

## YAML files

Both are written on first start with every default and a comment for each field. Put your changes in an extra file such as `EliteCrafting_economy_myserver.yml`: it is read after the main file and changes only what it names.

```yaml
format: 3
drops:
  chances:
    rune: [20, 25, 30, 35, 40, 45, 50, 55] # rune chance per tier, percent, Meadows to Deep North
  creatures:
    Troll: { rune_multiplier: 2 }          # double runes from trolls
    Hen: { multiplier: 0 }                 # hens drop nothing
runes:
  - { id: serpent, cost: { rare: 2 } }     # two Sealed Runes on Rare items
  - { id: cleansing, enabled: false }      # switch a rune off
classes:
  - { id: light, rolls: false }            # torches and lanterns never become magic
```

```yaml
format: 3
inscriptions:
  - { id: fleetfoot, weight: 50 }          # half as often
  - { id: godslayer, enabled: false }      # switched off
  - { id: vigor, tiers: { count: 8, from: 1, min: 5, max: 50 } }  # a stronger health ladder
```

- **Format 3.** Both files start with `format: 3`. A main file from an older version is renamed `<name>.v2.bak` (from 0.5.0 to 0.7.0; `.v1.bak` from 0.4.0 or older) and written fresh, with a warning in the log; copy your changes into the new file. An extra file without `format: 3` is skipped with a warning.
- **Inscription fields**: `affix` (`prefix` or `suffix`), `family`, `classes: { best: [...], allowed: [...] }` (class ids; the top third of tiers stays closed on `allowed`), `scaled: true` (multiplied by the weapon's damage scale), `weight` and `enabled`. `tiers` is a ladder `{ count, from, min, max }` (1-16 tiers, the weakest unlocked at item level `from`, the strongest at 8, values from `min` to `max`) or explicit rows `[ { tier, level, min, max, weight } ]`.
- **Economy fields**: each rarity's `prefixes` and `suffixes`; `rolling.allowed_closed_fraction` (0.334, the closed top of `allowed` classes); `classes` with `id`, `rolls` (false: never magic), `damage_scale`, `match` rules and `items` (prefab names); `item_tiers` (item levels: `items` by prefab, `materials`, `stations`). Lists by tier take eight values, Meadows to Deep North; a list of seven still loads, its last value counting for Deep North too.
- Deleting an entry does nothing (it comes back from the defaults); use `enabled: false`, or `weight: 0` to stop it rolling while existing copies keep working.
- The built-in defaults always sit under your files, so runes, drops and inscriptions a new version adds reach your server without editing. `use_defaults: false` in a main file turns that off: the files are then the whole configuration.
- A file with a mistake is named in the log, and the previous rules stay.
- `ecraft dump` writes the rules in force to a file.

## Commands

Open the console with F5. Commands marked * work for everyone unless the server turns them off; the rest need admin (single player, host or server admin list).

| Command | Does |
|---|---|
| `ecraft help` * | Lists the commands you may use |
| `ecraft inspect` * | Shows an item's rarity, inscriptions, sockets and gems |
| `ecraft stats` * | Shows your inscription totals and caps |
| `ecraft list inscriptions\|runes\|rarities [filter]` * | Lists the rules in force; filter by item class (`legs`, `sword_1h`), `prefix` or `suffix`, category (`offense`, `defense`, `utility`) or id |
| `ecraft inscription <id>` * | One inscription: its classes and every tier with its range and the item level that unlocks it |
| `ecraft classes` * | Every item class: its items, their item levels and how many inscriptions it rolls |
| `ecraft ecr` * | Shows the Elite Creatures Reborn link and what the creature you look at would drop |
| `ecraft give <rune>\|all [count]` | Gives runes: `awakening`, `recasting`, `ascension`, `cleansing`, `serpent`; the chisel `dvergr_chisel`; gems `gem_surtr`, `gem_ymir`, `gem_thor`, `gem_nidhogg`, `gem_hel`, `gem_tyr`, `gem_freyja`, `gem_odin`, `gem_skadi`, `gem_heimdall`, `gem_sleipnir` |
| `ecraft roll magic\|rare <item or class> [level]` | Gives a rolled magic item, for example `ecraft roll rare SwordIron` or `ecraft roll magic legs 8`; a class picks a random item of it, `level` (1-8) overrides its item level |
| `ecraft reroll` | Rerolls an item's inscriptions |
| `ecraft inscribe <inscription> [tier] [value]` | Adds one chosen inscription, for testing |
| `ecraft dump inscriptions\|economy\|items` | Writes the rules in force, or a list of every item with its class and level, to the config folder |
| `ecraft tiers` | Writes every item's class, item level and why to the config folder |
| `ecraft reload` | Re-reads the files now (on the server or in single player) |

`inspect`, `reroll` and `inscribe` act on the item on your cursor, under the mouse, or in your right hand; add `head`, `chest`, `legs`, `cape`, `utility` or `left` for other equipped items (`ground` for `inspect`).

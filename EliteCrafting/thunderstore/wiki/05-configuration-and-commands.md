# Configuration and Commands

Files are in `BepInEx/config` and apply without a restart:

- `com.EliteCrafting.cfg`: the settings below.
- `EliteCrafting_inscriptions.yml`: every inscription, its values and the caps.
- `EliteCrafting_economy.yml`: rarities, runes, item tiers and drop tables.

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
| Confirm destructive runes | HoldShift | Player | How Cleansing and Serpent ask first: `HoldShift`, `Dialog` (yes/no) or `Off` |
| Rune drops | On | Synced | Creatures, bosses and chests drop runes |
| Magic item drops | On | Synced | They drop magic gear (always off with Epic Loot) |
| Read-only commands for everyone | On | Synced | Off: `help`, `inspect`, `stats`, `list` and `ecr` become admin-only |
| Colored item names | On | Player | Magic item names in their rarity colour |
| Tooltip detail | Standard | Player | `Compact` (no tiers), `Standard` (tiers) or `Full` (also value ranges) |
| Show dormant inscriptions | On | Player | Show switched-off inscriptions, greyed |
| Ground glow | On | Player | Magic items on the ground glow |
| Glow intensity | 1 | Player | Brightness, 0-3 |
| Glow range | 2 | Player | Radius in metres, 0.5-6 |
| Glow max lights | 25 | Player | Only the nearest this many items glow, 0-100 |
| Glow refresh seconds | 1 | Player | How often the nearest are re-chosen, 0.25-5 |
| Glow runes | Off | Player | Runes on the ground glow too |
| Log rolls | Off | Player | Write every roll to the log |
| Log effect rebuilds | Off | Player | Write inscription totals to the log when they change |
| Synergy | Off | Synced | Elite Creatures Reborn's stars and world tier raise drops |

## YAML files

Both are written on first start with every default and a comment for each field. Put your changes in an extra file such as `EliteCrafting_economy_myserver.yml`: it is read after the main file and changes only what it names.

```yaml
drops:
  chances:
    rune: [8, 10, 12, 14, 16, 18, 20]   # rune chance per tier, percent
  creatures:
    Troll: { rune_multiplier: 2 }       # double runes from trolls
    Hen: { multiplier: 0 }              # hens drop nothing
runes:
  - { id: serpent, cost: { rare: 2 } }  # two Serpent Runes on Rare items
  - { id: cleansing, enabled: false }   # switch a rune off
```

- Switch an inscription off with `enabled: false`, or make it rarer with a lower `weight`, in `EliteCrafting_inscriptions_myserver.yml`.
- Deleting an entry does nothing (it comes back from the defaults); use `enabled: false`.
- The built-in defaults always sit under your files, so runes, drops and inscriptions a new version adds reach your server without editing. `use_defaults: false` in a main file turns that off: the files are then the whole configuration.
- A file with a mistake is named in the log, and the previous rules stay.
- `ecraft dump` writes the rules in force to a file.

## Commands

Open the console with F5. Commands marked * work for everyone unless the server turns them off; the rest need admin (single player, host or server admin list).

| Command | Does |
|---|---|
| `ecraft help` * | Lists the commands you may use |
| `ecraft inspect` * | Shows an item's rarity and inscriptions |
| `ecraft stats` * | Shows your inscription totals and caps |
| `ecraft list inscriptions\|runes\|rarities [filter]` * | Lists the rules in force; filter by slot, such as `legs` |
| `ecraft ecr` * | Shows the Elite Creatures Reborn link and what the creature you look at would drop |
| `ecraft give <rune>\|all [count]` | Gives runes: `awakening`, `shaping`, `recasting`, `ascension`, `consecrated`, `cleansing`, `serpent` |
| `ecraft roll magic\|rare <item or slot> [tier]` | Gives a rolled magic item, for example `ecraft roll rare SwordIron` or `ecraft roll magic legs` |
| `ecraft reroll` | Rerolls an item's inscriptions |
| `ecraft inscribe <inscription> [tier] [value]` | Adds one chosen inscription, for testing |
| `ecraft dump inscriptions\|economy\|items` | Writes the rules in force, or a list of every item, to the config folder |
| `ecraft tiers` | Writes every item's tier and why to the config folder |
| `ecraft reload` | Re-reads the files now (on the server or in single player) |

`inspect`, `reroll` and `inscribe` act on the item on your cursor, under the mouse, or in your right hand; add `head`, `chest`, `legs`, `cape`, `utility` or `left` for other equipped items (`ground` for `inspect`).

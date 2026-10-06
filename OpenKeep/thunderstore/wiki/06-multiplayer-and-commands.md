# Multiplayer and Commands

## Multiplayer

- Install OpenKeep on the server and every client, in the same version, or the player cannot join.
- `General / Lock Configuration` (on): every player uses the server's settings and YAML files. Off: everyone uses their own.
- Admins and the host can change the server's settings in game; the change reaches everyone.
- Player settings (keys, display, colours, sort order) are always your own.

## Shared chests

`0. Containers / Shared Chests` (Server) decides what happens with a chest another player has open.

| Mode | What you get |
| --- | --- |
| `Off` (default) | The chest is skipped, as in the game. |
| `View` | You can see its contents but not change them (`Viewing only`). |
| `Full` | Both of you can use it at once. If you both grab the same stack, one gets it (`Someone else got there first`). |

Crafting and station feeding never use a chest another player has open. A chest another player used last is fetched first: the first craft may say "Fetching the materials from storage, try again".

| Setting (`9. Shared`) | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Request Timeout` | 2 s | Server | How long a change to a shared chest waits for an answer (0.5 to 30). |
| `Touch Seconds` | 5 s | Server | How long a slot stays marked after another player pressed on it (0.5 to 60). |
| `Show Touches` | on | Player | Tint the slots another player is moving. |
| `Touch Colour` | #ffb347 | Player | Colour of that tint. |

## Other mods

- **PackPanel:** the buttons and trash can move into its panel, and its slots are never moved or sorted.
- **Epic Loot:** its enchanting table takes materials from nearby chests.
- **EliteCrafting:** its magic items are never salvaged.
- **GrindstoneSkills:** batch-cooked dishes each roll their own stars.
- **Wayfare:** quick portal jumps (near portals quicker, no loading screen into a loaded area) are Wayfare's; OpenKeep keeps the quick respawn.
- **EarthWright:** with the [Build Camera](wiki:Build Camera) out, its terrain edits still reach only from where you stand.
- **Trash can mods:** if one already put a trash can under the armour, OpenKeep's goes into the button row.

## Console commands

Type these in the game's console (F5).

| Command | Meaning |
| --- | --- |
| `openkeep` | List the commands. |
| `openkeep reload` | Reload the cfg and YAML files (admin or host on a server). |
| `openkeep containers` | List the containers within 20 m that OpenKeep can use, with their prefab names. |
| `openkeep write docs` | Write the item, container and station lists now. |
| `openkeep signs` | List every loaded chest and its sign state. |
| `openkeep signs reset` | Bring back the signs removed with the hammer (admin or host on a server). |
| `openkeep signs rewrite` | Rewrite all signs now. |
| `charter` | Each mod's version and whether the server's settings apply to you. |
| `charter diff OpenKeep` | The settings where your cfg differs from the server's. |
| `charter versions` | Your mods and versions and the server's; use it when you cannot join. |

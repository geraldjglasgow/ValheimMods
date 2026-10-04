# Multiplayer and Compatibility

## Multiplayer

- Install FeastMaster on the dedicated server and every player's game, all on the same version.
- A player without FeastMaster, or on another version, is refused at join with a message naming the server's version.
- A player with FeastMaster cannot join a server that runs other MilkyTeam mods but not FeastMaster.
- `Lock Configuration` (`0. Global Settings`, default true, read from the server only): while on, the server's values
  apply to every player, every food, mead and station section included, and players' own edits are undone. Off, each
  player plays by their own file.
- Section `8. Display` is always each player's own. Admins on the server's admin list, and the host, can change the
  server's values from their own game; edits to the server's file reach everyone without a restart.

## Console command

FeastMaster brings the `charter` command, shared by the MilkyTeam mods. Open the console with F5 (if nothing opens,
add `-console` to the game's launch options); no cheats needed.

| Command | Shows |
| --- | --- |
| `charter` or `charter status` | per mod, whether the server's values apply to you |
| `charter diff [mod]` | settings where your file differs from the server's (optionally one mod, any part of its name) |
| `charter versions` | your mods and versions, and the server's |

## Other mods

- **A default leaves the game alone.** Mods that change fishing, movement, skill experience, base values or
  regeneration keep working while FeastMaster's matching settings are at their defaults. A default cannot undo another
  mod's change: `Base Stamina = 75` while another mod sets 90 leaves the 90.
- **Foods, meads and Rested** keep their own (or another mod's) values until one of their entries (for foods, also a
  global food multiplier) is changed. From then on FeastMaster writes all of that item's values, even if set back to
  the defaults, until the next restart.
- **Modded items:** foods, meads, cooking stations and feasts from other mods get their own sections automatically.

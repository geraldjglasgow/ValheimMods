# Multiplayer and Commands

## Joining

- The server and every player need the same ShipConfig version ([Getting Started](wiki:Getting Started) has where to
  install it). Otherwise the join is refused and the connection screen says which version to get.
- A player without ShipConfig on a server that has it is disconnected about 20 seconds after joining.

## Lock Configuration

`Lock Configuration` (`[General]`, default on) is read on the server only.

- **On:** the server's values apply to every player while connected; their own values return when they leave. Edits to
  the server's file reach players without rejoining.
- **Admins** (in the server's `adminlist.txt`) can change the server's values from their own game, for everyone.
- **Off:** every player uses their own file, so ships handle, take damage and cost differently depending on whose game
  controls them. Leave it on unless every player has the same file.

## Other mods

- **Modded ships** built on the game's own ship parts get the full set of settings.
- **Mods that change the same values** (ship health, sail force, drag, damage, build cost) conflict. Use one mod per
  value.
- Only ships are changed; carts and building pieces are left alone.
- **Configuration managers** can edit the settings in game; while the server's values apply and you are not an admin,
  they show them read-only.

## The charter command

Type it in the game's console (F5); no cheats needed. It covers every MilkyTeam mod that syncs its settings.

| Command | What it shows |
| --- | --- |
| `charter` | Whether the server's values apply to you, per mod |
| `charter diff [name]` | Every setting where your file differs from the server's; a name narrows it to matching mods |
| `charter versions` | Your synced mods and versions, and the server's |

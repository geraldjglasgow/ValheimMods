# Console Commands

Open the console with F5 in a world. Neither command needs `devcommands`.

## lockstep

Works the same on a dedicated server and a hosted world.

| Command | Who | Effect |
| --- | --- | --- |
| `lockstep` or `lockstep status` | Everyone | Each stage: open, or who it waits for; then every player on the roster. On a server without Lockstep, says so |
| `lockstep grant <player> <stage>` | Admins | Gives the player credit for that boss |
| `lockstep revoke <player> <stage>` | Admins | Takes that credit away |
| `lockstep ignore <player>` | Admins | The player never holds the group back |
| `lockstep unignore <player>` | Admins | The player counts again |
| `lockstep forget <player>` | Admins | Removes the player from the roster; joining again makes them a new player (see `Late Joiners`) |

- Admins: the server's `adminlist.txt` and the host of a hosted world. Others are refused.
- `<player>`: a name from `lockstep status` (any case), or the player ID in brackets there, needed when two characters
  share a name. The player must have joined at least once.
- `<stage>`: a stage name from the [chain](wiki:Configuration) (`Eikthyr`, `TheElder`, `Bonemass`, `Moder`, `Yagluth`,
  `TheQueen`, `Fader` by default), its key (`defeated_gdking`) or its boss prefab (`gd_king`).
- `grant` and `revoke` change only Lockstep's credit, not the world's boss records or the character.
- Changes are saved to the roster file at once, and altars update.

In `lockstep status` each player is `online` (counted), `counted` (offline, still counted), `inactive` (offline and
not counted: away longer than `Inactive Days`, or `Count Only Online` is on) or `ignored` (never counted), with the
stages they have cleared and their last seen time in UTC.

## charter

Shared with other mods by the same team; runs in your own game, anyone may use it.

| Command | Shows |
| --- | --- |
| `charter` or `charter status` | Whether the server's settings apply to you, and whether you are an admin there |
| `charter diff [mod]` | Where your own settings differ from the server's |
| `charter versions` | These mods' versions in your game and on the server |

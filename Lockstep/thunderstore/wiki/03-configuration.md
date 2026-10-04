# Configuration

Three files in `BepInEx/config`: the `.cfg` and chain file, written on first start, and the roster, written when the
first player joins a world. Edits apply without a restart.

## com.Lockstep.cfg

| Section | Setting | Default | Effect |
| --- | --- | --- | --- |
| `General` | `Lock Configuration` | `true` | Read on the server. On: every player uses the server's settings and chain while connected; their own files are untouched and their values return when they leave |
| `Credit` | `Credit Radius` | `200` | Players within this many metres of a boss when it dies get credit without hitting it. 0: off. Hitting the boss always gives credit |
| `Credit` | `Credit Everyone Online` | `false` | Every online player gets credit when a boss dies, wherever they are |
| `Group` | `Inactive Days` | `14` | Offline players not seen for more than this many days are not counted. 0: off |
| `Group` | `Count Only Online` | `false` | Only online players are counted. Off: everyone on the roster, minus ignored and inactive players |
| `Group` | `Late Joiners` | `Catchup` | `Catchup`: a character's first join credits every boss the world has defeated. `Earn`: it fights every boss. Affects only first joins after the change |
| `Gate` | `Spawn Guard` | `true` | Also blocks the boss spawn itself while its stage is closed |
| `Gate` | `Name Missing Players` | `true` | The altar message names the players still missing the previous boss. Off: "the group" |

While `Lock Configuration` is on, a player's own change is reverted, while an admin (admin list or host) can change a
setting in their own game and it is saved on the server for everyone; `charter diff` lists where your file differs
from the server's. With it off, each player's game uses its own `Spawn Guard`, `Name Missing Players` and chain file.

Typical setups: a group that trusts each other, `Credit Everyone Online = true`; players come and go,
`Count Only Online = true` or a lower `Inactive Days`; strict (only fighters get credit, newcomers start over),
`Credit Radius = 0` and `Late Joiners = Earn`.

## LockstepChain.yml

The gated bosses, in order. Each top-level entry is a stage; its name is shown at the altar and in `lockstep status`
and typed in commands, so keep it free of spaces (commands also accept the `key` or `boss`).

| Field | Meaning |
| --- | --- |
| `order` | Whole number. The lowest is the first stage, always open. Gaps are fine; missing counts as 0 |
| `key` | The global key the game sets when this boss dies; credit is given by it |
| `boss` | Prefab name of the boss the altar spawns; altars are matched by it |

| `order` | Stage | `key` | `boss` |
| --- | --- | --- | --- |
| 1 | Eikthyr | `defeated_eikthyr` | `Eikthyr` |
| 2 | TheElder | `defeated_gdking` | `gd_king` |
| 3 | Bonemass | `defeated_bonemass` | `Bonemass` |
| 4 | Moder | `defeated_dragon` | `Dragon` |
| 5 | Yagluth | `defeated_goblinking` | `GoblinKing` |
| 6 | TheQueen | `defeated_queen` | `SeekerQueen` |
| 7 | Fader | `defeated_fader` | `Fader` |

**Adding a modded boss** needs its global key (kill it once and compare the game's `listkeys` list; needs
`devcommands`) and its prefab name (usually in the boss mod's documentation):

```yaml
ExampleBoss:
  order: 8
  key: defeated_exampleboss
  boss: ExampleBoss
```

- To fit a boss between two others, space the orders (10, 20, 30...) and give it 25.
- A modded boss is gated only when summoned at an altar, and gives credit only if it sets a global key on death.
- Change `order` to reorder: each altar waits for whichever stage now comes before it.
- Delete a stage to stop gating that boss: it gives no credit, and the next stage waits for the one before it.
- Every `LockstepChain*.yml` in `BepInEx/config` or next to `Lockstep.dll` joins the chain (e.g.
  `LockstepChain.MyBosses.yml`). The default file is written only when none exists.
- With `Lock Configuration` off, give every player the same chain file, or kills of a boss missing from a player's
  copy can go uncredited.

Problems are logged with the file and entry:

| Problem | Result |
| --- | --- |
| A stage without `key` or `boss`; an `order` that is not a whole number; no stages; invalid YAML | Error: no chain file is applied, the previous chain stays (the default if none loaded yet) |
| Two stages with the same `order` | Warning: applied, their relative order is undefined |
| An unknown field | Warning: applied, the field is ignored |

## Lockstep.<world>.roster.yml

Every character, one file per world, on the server only (the host's `BepInEx/config` on a hosted world). Copy it
along when you move the world to another server. The [console commands](wiki:Console Commands) make the common edits.
It has `installedKeys` (a list) and `players` (a list of entries with the other fields):

| Field | Meaning |
| --- | --- |
| `installedKeys` | Bosses the world had defeated when the roster was created; the stage after each is open to all |
| `id` | The character's player ID |
| `name` | The character's name, updated on every join |
| `lastSeen` | Last join, leave or credit, UTC, `yyyy-MM-dd HH:mm`. Another format counts as never seen: inactive while offline, unless `Inactive Days` is 0 |
| `ignored` | `true`: never holds the group back |
| `cleared` | Global keys of the bosses the character has credit for |
| `lastSeenUtc` | Copy of `lastSeen` for reading; editing it does nothing |

Keep a copy before editing by hand:

- An edit that does not parse is logged and ignored; the server keeps its roster and overwrites your edit at the next
  change.
- A file the server cannot read when loading the world counts as empty and is overwritten at the first join.
- Deleting the file while the server is stopped starts over: at the next join, every boss the world has defeated
  counts as cleared for everyone.

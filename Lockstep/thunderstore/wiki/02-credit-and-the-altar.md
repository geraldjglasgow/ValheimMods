# Credit and the Altar

## Earning credit

When a boss from the [boss chain](wiki:Configuration) dies, each player **online at that moment** gets credit if:

| Rule | Setting (default) |
| --- | --- |
| They hit the boss at any point, even if they died and respawned since | Always on |
| They were within this many metres of where it died | `Credit Radius` (200; 0 turns it off) |
| They were online at all, anywhere | `Credit Everyone Online` (off) |

A player who logged out before the boss died gets no credit (an admin can `lockstep grant` it). Credit never expires.
A boss not in the chain gives no credit. Credit counts only once the server sees the boss die; a kill report it
cannot confirm gives no credit and is logged on the server.

## Who counts

The server keeps a roster of every character that has joined: player ID (kept on rename), name, last seen time,
ignore flag and credit. Each character is its own entry with its own credit, so two characters appear twice.

An altar only waits for **counted** players:

| Player | Counted |
| --- | --- |
| Ignored by an admin | Never |
| Online | Always, unless ignored |
| Offline, with `Count Only Online` on | No |
| Offline, last seen more than `Inactive Days` ago (default 14; 0 turns this off) | No |
| Any other offline player | Yes |

- Ignored players still play and earn credit; they never hold the group back (`lockstep ignore` / `unignore`).
- An inactive player counts again as soon as they log in.
- `lockstep forget` removes a character; if it joins again, it is a new player.

**New players.** `Late Joiners` decides what a character gets the first time it joins. `Catchup` (default): credit
for every boss in the chain the world has already defeated. `Earn`: none. The newcomer then counts like everyone
else, so altars the group had opened close again until the newcomer catches up or an admin grants the credit.

**Installing on a world in progress.** The first time a player joins after install, every boss in the chain the world
has already defeated counts as cleared for everyone, including later joiners, whatever `Late Joiners` says (listed
under `installedKeys` in the roster file).

## The altar

A boss can be summoned only when every counted player has credit for the boss directly before it in the chain.

- The first boss (Eikthyr by default) is always open, as is any boss whose previous boss the world had already
  defeated when Lockstep was installed.
- Only the boss directly before counts: the Moder altar asks about Bonemass, not Eikthyr.
- It holds for every summon: a boss the group already beat cannot be summoned again either while a counted player
  lacks the one before it.
- Altars update at once when players join or leave, a boss dies, or the roster, `Group` settings or chain change.

Hovering an altar shows its gate under the game's own text:

```
Open: the first boss
Open: everyone has defeated Eikthyr
Sealed: waiting for Bjorn, Sigrid to defeat Eikthyr
```

Making the offering at a closed altar, or using a closed altar with item stands, shows instead:

```
TheElder will not answer. Waiting for Bjorn, Sigrid to defeat Eikthyr.
```

- The names are the counted players who lack the previous boss; with `Name Missing Players` off it says "the group".
  Stage names appear as written in the chain (`TheElder`).
- Nothing is used up: the offering stays in your inventory, items stay on their stands.
- Altars are matched by the boss they spawn: a modded altar spawning a boss in the chain is gated too; an altar whose
  boss is not in the chain works as normal.
- `Spawn Guard` (on) also stops the boss itself from spawning while its stage is closed, catching a summon that gets
  past the altar, for example from another mod using it. The offering is kept.

## Not gated, and other mods

- Bosses spawned with the `spawn` command or a mod's own spawner, or met away from an altar. Their kills still give
  credit for the next stage.
- Biomes, portals, dungeons, crafting and materials: a fast player can go anywhere and bring things back.
- The game's own progress: Lockstep never changes the world's boss records (global keys), so raids, traders and mods
  that read them (world level, raid mods) see normal progress.

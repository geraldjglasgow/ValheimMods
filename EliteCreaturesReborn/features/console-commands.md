# Elite Creatures Reborn - specification: Console commands

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the mod's console commands and who is allowed to run them. The settings they read and reload are
`configuration.md`; the access levels are enforced through `server-enforcement.md`.

**Status: partly built.** Seven sub-commands exist: `spawn`, `inspect`, `purge`, `effects`, `reference`, `tier` and
`raid` (`raids.md`, built 2026-10-10, untested), plus the top-level `damage` (3.9.0, section 1). Three specified ones do not: `pressure`, `zones` and `reload` -
each blocked on its own feature. **The built names and the specified names disagree**, which is the open decision
at the end of this file and should be settled before anything else is added.

---

# 1. The shape

**Nine sub-commands under one mod command**, plus a separate settings command with three of its own.

One command with sub-commands rather than eight top-level commands, because the console is shared with the game
and every other mod, and a mod that claims eight names in it is a bad neighbour. Tab completion makes the
sub-commands discoverable, and a `help` sub-command lists them.

| Sub-command | Does |
| --- | --- |
| **Pressure** | Prints the pressure where you stand, and what contributed to it |
| **Inspect** | Prints the resolved traits and values for the creature you are looking at, including a Thieving creature's pouch (`thieving.md`), what a Devouring creature has eaten against how many it may ("devoured 1 of 2: Boar", `mutations.md`) and a boss's aspect, its loot multiplier, its twin or phantom link, Summoner's waves, the damage type an Adaptive boss resists now ("resisting now: fire") and whom a Fixated boss has marked ("marked: Gerald (12 s ago)") (`boss-aspects.md`) |
| **Summon** | Spawns a creature with chosen stars, mutation and attunement, for testing. A boss takes one aspect word instead (`elite spawn Bonemass 2 Twin`), any of the thirteen - Adaptive, Fixated, Stormbound, Gravitic and Colossal included - and brings its twin, or splits off its phantom copies, as it would from the altar |
| **Purge** | Removes loaded modified creatures, or with a radius in metres only those that close to you - dropping any stolen goods first, the one documented exception to "no drops" (`thieving.md`) |
| **Zones** | Lists retaliation zones, their level and their decay |
| **Tier** | Shows the world tier, what it does to the rolls right now, and which bosses count toward it. Read-only |
| **Reload** | Re-reads the settings and rule files from disk |
| **Reference** | Writes `creature_reference.yml` - every registered creature, grouped by biome, with its vanilla drop table (`loot.md`) |
| **Raid** | `elite raid start <coins> [tier]` sounds a raid where the admin stands with that stake and no Raiders Chest, the heat weighed against the tier given (0-6) or else the strongest player within 96 m - the chest's base test and daily wait skipped, the 200 m between raids kept; `elite raid stop` stops the nearest raid within 200 m as holding E on its chest does: every raider dies and drops nothing (`raids.md`) |

The separate settings command is `charter`, provided by the workspace library of the same name, with `status`,
`diff` and `versions` (`server-enforcement.md`). It is not redesigned here.

## One top-level player command: `damage`

**`damage` shows the latest boss damage board again**, for the full `Boss damage board seconds`, whether it is
still on screen or has faded. It is typed as `/damage` in chat, or `damage` in the F5 console.

- **It is its own word, not an `elite` sub-command**, and the one exception to the one-name rule above. It is for
  players rather than admins, it is typed in chat mid-session, and `/damage` is what a player reaches for. The game
  has no `damage` command of its own, so the name is free.
- **It shows the board even for a player who turned the board off.** They asked for it by name.
- **With the board already up, it starts the full time again.**
- **Which board.** Every machine remembers the latest board it received, the server included. A player who joined
  after the kill has none, so their machine asks the server, which answers that player alone. With no board
  anywhere it says `damage: no boss has fallen since the world was loaded.` The server's memory lasts until it
  restarts; nothing is saved with the world.

## Two of these are the mod explaining itself

**Pressure** and **Inspect** are not developer tools that happen to be shipped. They are how the mod keeps the
promise it makes in `../SPEC.md`:

> Difficulty should be *legible*. A player who dies should be able to say what killed them and what they would do
> differently next time.

**Pressure** answers "why is it harder here?" by printing the number *and what contributed to it* - biome, world
tier, hearth distance, any retaliation zone. A player who learns that walking out from their base raises the
danger has learned a mechanic (`pressure.md`). A player who just meets harder creatures has learned nothing.

**Inspect** answers "what exactly was that?" for a creature the player is looking at, including which rule set
resolved it. It is what turns "that troll hit strangely hard" into a fact somebody can act on or report.

---

# 2. Access on a locked server

**Three levels decide who may use them:**

| Level | Who |
| --- | --- |
| Admin only | Admins |
| Admins and listed players | Admins, plus named players |
| Anyone | Everyone connected |

The split follows one line: **read-only or world-changing.**

- **Inspect and Pressure are read-only and safe to open up.** They tell a player about a world they are already
  standing in. Opening them costs a server nothing and makes the mod explain itself to everyone rather than to
  admins.
- **Summon, Purge and Raid change the world and are not.** Summon creates creatures, Purge deletes them and Raid
  sends or stops a raid. These are admin tools on any server that locks anything.
- **Tier is read-only too.** It was once meant to set the tier under a Manual source; the tier is now always
  derived from the world's boss defeats (`world-tiers.md`), so it only reports, and it is open to every player.

- **`damage` is read-only and open to everyone, always.** It only shows again what the server already sent to
  every player. It is not a cheat, needs no `devcommands` and no admin rights, and is not one of the access levels
  above.

Zones is read-only. Reload re-reads files and so changes what everyone is playing, which puts it with the second
group. Reference changes nothing in the world - it writes one fixed-name report next to the config files - so it
sits with the read-only group; a server that dislikes even that can restrict it, since access is a setting.

---

# 3. Multiplayer

- **Read-only commands are answered locally** wherever possible. Pressure is computed from the same terms the
  spawn roll uses and needs no message; Inspect reads the traits already replicated in the creature's ZDO.
- **World-changing commands are executed on the server** and their effects replicate the ordinary way. Summon
  creates a real creature whose owner rolls nothing - the traits are the ones the command specified - and Purge
  removes loaded creatures on every machine, not just the caller's screen.
- **Access is checked on the server**, never on the client that typed the command. A client-side check is a
  suggestion.
- **Tier never sets.** The tier is derived from the boss defeat keys the server already shares (`world-tiers.md`),
  and a command that overrode it would make the world's difficulty disagree with its own history. An admin who
  wants to try a tier out uses the game's own `setkey` and `removekey`.
- **`elite raid` runs on the admin's own machine.** `start` places the test raid's invisible marker there, so that
  machine owns it and runs the raid exactly as a Raiders Chest's owner runs its own (`raids.md` section 5); the raid
  then lives in the marker's ZDO and carries on with whoever owns it next. `stop` finds the raid by its host's ZDO and
  asks that host's owner, wherever it is.
- **Reload re-reads the server's files and re-sends them**, so one admin's reload reaches every connected player -
  which is the point of it, and also why it is not a read-only command.
- **`damage` is answered locally** from the latest board this machine received. Only a machine with none - a
  player who joined after the kill - asks the server, and the answer goes to that player alone. A board that
  arrives from a new boss death while the question is out is not overwritten by the older answer.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 4. Configuration

- **Command access**, one of the three levels, in the main settings file.
- **The list of permitted players**, for the middle level.

---

# 5. Open decisions

**The built names and the specified names disagree, and one built command is not in the specification at all.**

| Specified | Built | |
| --- | --- | --- |
| Summon | `spawn` | Different word for the same thing |
| Inspect | `inspect` | Agrees |
| Purge | `purge` | Agrees |
| Pressure | - | Blocked on `pressure.md` |
| Zones | - | Blocked on `retaliation-zones.md` |
| Tier | `tier` | Agrees; read-only, open to every player |
| Reload | - | Not built |
| - | `effects` | Built, unspecified |
| - | `damage` | Built in 3.9.0 at the user's request; top level, not under `elite`, open to every player |
| - | `raid` | Built 2026-10-10 from `raids.md` (start and stop); admin only |

Two things to settle:

1. **`spawn` or `summon`.** They are the same command. `summon` is the specification's word; `spawn` is what
   shipped in 3.0.0. Renaming it is a breaking change to a published command, so it is a **major version**
   decision either way, and the longer it waits the more it costs.
2. **What `effects` is, and whether it stays.** It exists in the build and appears nowhere in `../SPEC.md`. Per
   `../CLEANROOM.md`, a behaviour the spec does not cover is decided with the user and written down - so it either
   gets a row in the table above and a line in this file, or it goes.

Until this is settled the command list is in two places and they do not match, which is exactly the situation the
feature files exist to prevent.

**Whether `help` counts as one of the seven.** The table lists seven functional sub-commands and the shape
description mentions a `help` that lists them. Whether that is an eighth or simply the bare command with no
argument is not stated.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The commands

- [~] `spawn`, `inspect`, `purge` and `effects` exist
- [~] `tier` exists and is open to every player - built in 3.6.0, not tested in game
- [~] `damage` (and `/damage` in chat) shows the latest boss board again, for any player, even with the board off;
  a late joiner gets it from the server - built in 3.9.0, not tested in game
- [~] `raid start <coins> [tier]` and `raid stop` (`raids.md`), admin only - built 2026-10-10, not tested in game
- [ ] The two specified but missing sub-commands (Pressure, Zones), and Reload
- [ ] Inspect, Pressure and Tier are read-only; Summon and Purge change the world

## Multiplayer

- [ ] Read-only commands answered locally where possible
- [ ] World-changing commands executed on the server, replicating the ordinary way
- [ ] Access checked on the server, never on the client that typed it
- [~] Tier never sets; it reports the tier derived from the server's keys - built, not tested in game
- [ ] Reload re-reads the server's files and re-sends them to every player

## Configuration

- [ ] Command access level
- [ ] The permitted-players list for the middle level

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-20 | `Reference` added as the eighth sub-command, read-only group (`loot.md` section 7). | pending |
| 2026-09-26 | `tier` built: read-only, open to every player; the Manual source it was to set is dropped (`world-tiers.md`). | EliteCreaturesReborn-v3.6.0 |
| 2026-09-27 | `damage` built as a top-level, player-facing command (`/damage` in chat): replays the latest boss board, open to everyone, late joiners served by the server. `elite inspect` lists a Thieving creature's whole pouch. | - |
| 2026-09-27 | `elite spawn <boss> <stars> <aspect>` takes the five new aspect words (Adaptive, Fixated, Stormbound, Gravitic, Colossal); `elite inspect` shows an Adaptive boss's resisted type and a Fixated boss's mark. | - |
| 2026-10-10 | `elite raid start <coins> [tier]` and `elite raid stop` added with the raids (`raids.md`): admin only, run on the admin's machine, which owns the test raid's marker. | - |

# Elite Creatures Reborn - specification: Console commands

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the mod's console commands and who is allowed to run them. The settings they read and reload are
`configuration.md`; the access levels are enforced through `server-enforcement.md`.

**Status: partly built.** Four sub-commands exist: `spawn`, `inspect`, `purge` and `effects`. Three specified ones
do not: `pressure`, `zones` and `reload` - each blocked on its own feature. **The built names and the specified
names disagree**, which is the open decision at the end of this file and should be settled before anything else is
added.

---

# 1. The shape

**Eight sub-commands under one mod command**, plus a separate settings command with three of its own.

One command with sub-commands rather than eight top-level commands, because the console is shared with the game
and every other mod, and a mod that claims eight names in it is a bad neighbour. Tab completion makes the
sub-commands discoverable, and a `help` sub-command lists them.

| Sub-command | Does |
| --- | --- |
| **Pressure** | Prints the pressure where you stand, and what contributed to it |
| **Inspect** | Prints the resolved traits and values for the creature you are looking at, including a Thieving creature's pouch (`thieving.md`) and a boss's aspect, its loot multiplier, its twin or phantom link and Summoner's waves (`boss-aspects.md`) |
| **Summon** | Spawns a creature with chosen stars, mutation and attunement, for testing. A boss takes one aspect word instead (`elite spawn Bonemass 2 Twin`), and brings its twin or phantom copies as it would from the altar |
| **Purge** | Removes loaded modified creatures - dropping any stolen goods first, the one documented exception to "no drops" (`thieving.md`) |
| **Zones** | Lists retaliation zones, their level and their decay |
| **Tier** | Shows the world tier and its source, and sets it where the source is manual |
| **Reload** | Re-reads the settings and rule files from disk |
| **Reference** | Writes `creature_reference.yml` - every registered creature, grouped by biome, with its vanilla drop table (`loot.md`) |

The separate settings command is `charter`, provided by the workspace library of the same name, with `status`,
`diff` and `versions` (`server-enforcement.md`). It is not redesigned here.

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
- **Summon, Purge and Tier change the world and are not.** Summon creates creatures, Purge deletes them, and Tier
  can move the difficulty of the entire server. These are admin tools on any server that locks anything.

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
- **Tier only sets where the source is Manual** (`world-tiers.md`). Under Server or Elapsed the tier is derived,
  and letting a command override a derived value would mean the world's difficulty disagreed with its own
  history.
- **Reload re-reads the server's files and re-sends them**, so one admin's reload reaches every connected player -
  which is the point of it, and also why it is not a read-only command.

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
| Tier | - | Blocked on `world-tiers.md` |
| Reload | - | Not built |
| - | `effects` | Built, unspecified |

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
- [ ] The three specified but missing sub-commands
- [ ] Inspect and Pressure are read-only; Summon, Purge and Tier change the world

## Multiplayer

- [ ] Read-only commands answered locally where possible
- [ ] World-changing commands executed on the server, replicating the ordinary way
- [ ] Access checked on the server, never on the client that typed it
- [ ] Tier only sets where the source is Manual
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

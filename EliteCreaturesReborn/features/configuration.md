# Elite Creatures Reborn - specification: Configuration

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the files themselves, how they behave, and the one hard rule that binds every other feature file
in this folder: **everything can be turned off.** How the server binds these values to players is
`server-enforcement.md`; the settings that are deliberately exempt are `display-preferences.md`.

**Status: partly built.** The settings file, `creature_rules.yml`, validation and hot reload are built and
shipping. The **item rule file** (`item-rules.md`), **supplementary rule files** and the **in-game editor** are
not, and several of the thirteen settings sections describe features that do not exist yet.

---

# 1. The files

**A main settings file with thirteen sections, one per topic:**

general, stars, scaling, mutations, attunements, boss aspects, loot, world tiers, respawning, retaliation,
multiplayer, display, diagnostics.

One section per feature, matching one file per feature in this folder. A player looking for a setting should be
able to guess which section it is in from what they want to change, and an implementer should never have to ask
where a new setting goes.

**Two optional rule files** - one for creatures, one for items - for anything too structured for a flat settings
file. The creature file is `creature_rules.yml` and holds the per-biome, per-tier and per-creature tables.

**Neither is required. The mod runs on the main file alone.** A player who wants stars and mutations at their
default tuning should never have to open a YAML file, and a player who deletes one should get the shipped defaults
back rather than an error.

**Supplementary rule files can be dropped alongside and are merged into the main ones**, so a server keeps its own
additions in a separate file that survives an update. This is the difference between a server's customisations
being a thing they maintain and a thing they re-apply by hand every release.

---

# 2. Behaviour

**Validation.** A rule file with errors is reported **line by line in the log**, and **the previously loaded rules
stay in force.**

Both halves matter. Line by line, because "invalid YAML" in a four-hundred-line file is not a diagnosis. Previous
rules staying in force, because the alternative is that an admin's typo silently reverts a server's entire
ruleset to defaults mid-session, and nobody notices until a player asks why the trolls got easy. **A typo never
silently disables a server's rules and never takes the server down.**

**Hot reload.** The settings and rule files re-read on edit, while playing, without a restart. Tuning a mod like
this is a loop of change-something-and-look-at-it, and a restart per iteration makes that loop useless.

**Sync.** Server values and rules reach clients on join and on change - `server-enforcement.md`.

**In-game editor.** Rule files can be edited from inside the game, for admins without file access to the server.
A rented dedicated server is the normal case for a Valheim group, and many of them offer no convenient file
access at all.

**Config UI.** The mod presents cleanly in third-party configuration UIs, **including hiding settings that do not
apply given other choices** - the custom star table is not shown while a preset is selected (`pressure.md`), and
days-per-tier is not shown while the tier source is Server (`world-tiers.md`). A settings screen that shows every
option including the inert ones teaches the player nothing about which ones matter.

---

# 3. Everything can be turned off

**Every feature in this folder has an off switch, and turning one off leaves the rest working.**

Stars without mutations. Mutations without attunements. Loot rules and nothing else. The whole trait system off
and only the multiplayer scaling running. All of these are valid installations.

**This is a hard rule, not a nicety**, and it has two teeth:

1. **No feature may assume another is enabled.** A mutation that reads a world tier must work at tier 0 with tiers
   switched off. An aspect that scales loot must work with loot rules off.
2. **A disabled feature costs nothing at runtime** rather than running and discarding its result. Off means not
   executed, not executed-and-ignored.

The reason is a player, not an architecture principle: **a player should be able to install this mod for one thing
they want and get exactly that.** Somebody who wants nothing but slower dungeon respawns should be able to have
them without a single starred creature appearing anywhere in their world.

---

# 4. Diagnostics

**Off by default**: timing of the mod's own work, and a log of what was applied to each creature.

For diagnosing a performance complaint on a busy server, not for normal play. A mod that logs every creature it
touches will fill a dedicated server's log on a populated world, which is why this is opt-in and why it is
described here as a tool with a specific purpose rather than a verbosity level.

`../SPEC.md` names the thing most likely to need it: the **Splintering cascade** in `mutations.md`. Its caps are
off by default and its tail is long, so if a server starts stuttering that is the first place to look, and the
diagnostics are how you confirm it before changing anything.

---

# 5. Multiplayer

- **The settings file and both rule files are server-owned** while lock-to-server is true, and reach clients on
  join and on every edit.
- **A hot reload on the server propagates.** One admin saving a file re-tunes every connected player, which is the
  intended behaviour and is why `reload` is not treated as a read-only command (`console-commands.md`).
- **Validation runs on both sides.** A server must not send rules a client will reject, and a client that cannot
  parse what it was sent must keep its previous rules rather than falling back to defaults and quietly playing a
  different game from everyone else.
- **The display section never syncs**, on any server, in any lock state (`display-preferences.md`).

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 6. Open decisions

**How supplementary files merge.** The behaviour is specified - dropped alongside, merged into the main ones - and
the merge *rule* is not. Whether a supplementary file's row replaces a main file's row, or is combined with it,
and what happens when two supplementary files touch the same key, all need answering before the feature is built.
This is the kind of thing that works fine until two servers do it differently and one of them reports a bug that
cannot be reproduced.

**Whether the thirteen sections should include an item rules section.** The list has thirteen entries and the item
rule file is a whole feature (`item-rules.md`) with no section of its own - its settings would presumably live in
the rule file entirely, but the off switch and the OpenKeep stack-size interaction are settings-file matters.

**What the in-game editor can edit.** "Rule files can be edited from inside the game" does not say whether that is
free-text YAML with validation on save, or a structured editor over the known keys. The first is far easier and
hands an admin a way to break their server from a text box; the second is a lot of work. Not decided.

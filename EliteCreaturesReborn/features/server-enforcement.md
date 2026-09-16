# Elite Creatures Reborn - specification: Server enforcement

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers how the server's settings bind every player: what is locked, what is never locked, and what
happens to a player who joins without the mod. The settings themselves are `configuration.md`; the preferences
that are deliberately exempt from all of this are `display-preferences.md`.

**Status: built, not tested in game.** The mod merges the workspace's `Charter` library and binds its gameplay
settings through it. It has never been run against a real dedicated server with a second machine.

---

# 1. The rule

**The server's settings bind every player.**

- Values are **sent to clients on join and on every change**.
- Clients **cannot override what the server locks**. A player's own file is ignored while they are connected, and
  restored when they leave - so a player who plays on a server and then plays alone gets their own settings back
  rather than being permanently overwritten by somebody else's.
- Server admins can still edit the values in game, and the edit reaches every connected player.

This is provided by the workspace's own **`Charter`** library and is **not redesigned here**. Charter is the
clean-room replacement written for this repository; every mod in the workspace binds through it, and its own
documentation in `ValheimModLibs/` is the place its behaviour is specified.

---

# 2. What is never locked

**Display preferences are never bound to the server**, because they change only what one player sees. Colours,
tint strength, flame brightness, nameplate distance and whether trait names show at all stay that player's
decision on any server.

The full list and the reasoning are in `display-preferences.md`. The line is drawn at the same place everywhere in
this workspace: **anything that changes gameplay is synced and lockable; anything that changes only what one
player looks at is theirs.** A server has a legitimate interest in how much health a two-star troll has. It has no
legitimate interest in whether one player finds the orange tint garish.

---

# 3. Joining without the mod

**Players joining without the mod, or with an incompatible version, are refused at the join screen** with a reason
and a short code logged on both sides, so an admin can match the two.

The code is the part that matters in practice. A player reports "it says I can't join"; an admin has a server log
full of connections. One code that appears in both, naming the mod and both version numbers, turns that into a
thirty-second diagnosis instead of a conversation.

Charter also provides the `charter` console command, which answers the three questions a player actually has:
whether the server is binding their settings, where the server's values differ from their own file, and what
versions each side is running.

---

# 4. What the rule files do

The two optional rule files - creatures and items - sync the same way the settings file does: **to clients on join
and on every edit**, with the server's copy in force while lock-to-server is true.

Validation is the safety net, and it is specified in `configuration.md`: a rule file with errors is reported line
by line in the log and **the previously loaded rules stay in force**. The failure this prevents is the important
one - an admin's typo propagating to every connected player and silently disabling a server's entire ruleset
mid-session.

---

# 5. Multiplayer

This feature *is* the multiplayer layer, so the usual section is the other way round: what it guarantees to
everything else.

- Every other feature can assume **every connected player has the same gameplay values**, without checking.
- Every other feature can assume **a value change reaches every client**, without writing its own message.
- No feature may assume a client has the same *display* preferences as any other, because they deliberately do
  not.
- Trait state is separate from all of this. Traits live in creature ZDOs and are replicated by the game; Charter
  carries settings and rules, not creatures. A feature that confuses the two will work in single player and fail
  on a server.

---

# 6. Configuration

- **Lock configuration**, on by default. With it off, each player uses their own file and the server enforces
  nothing.
- **Admin editing in game**, so a server's values can be changed without file access.
- **Command access levels**, which decide who may run which console commands on a locked server - specified in
  `console-commands.md`.

---

# 7. A provenance note, now resolved

`../SPEC.md` carries a warning worth recording as closed, because it shaped what this mod actually merges.

It flagged that **`YamlConfig` and `TraitSets` carried the same provenance problem this rebuild exists to fix** -
both were rewritten from specifications that had themselves been written from a decompile - and said they needed
auditing or replacing before the new mod depended on them.

**Elite Creatures Reborn depends on neither.** It merges `Charter`, `ConfigReload`, `PatchGuard` and `YamlDotNet`
only, and reads its rule files through its own `Rules/YamlRead.cs` and stores traits through its own `Traits/`
code rather than using either flagged library. That is the reason this mod's set of merged libraries differs from
the other mods in the workspace, and it should stay that way unless those two libraries are audited.

`ItemCopies` is the one library ECR will need later and does not yet merge: it is what reaches items that already
exist in the world and in open containers, for `item-rules.md`. `../SPEC.md` records it as independent of the
provenance problem.

---

# 8. Open decisions

**What a player's own file does while they are connected** is specified - ignored, then restored - but **what
happens to a hot edit made while connected** is not. A player who edits their file on a locked server has made a
change that cannot take effect now and may surprise them when they next play alone.

**Whether a version mismatch should refuse or warn.** Refusing is correct while the mod's ZDO keys or rule format
can change between versions. Once the mod is at 1.0.0 and promising compatibility, a patch-level difference
refusing a join is stricter than it needs to be. Not decided, and not urgent until 1.0.0.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The rule

- [ ] Gameplay settings bound to the server's copy; display settings never locked
- [ ] Joining without the mod behaves as section 3 says
- [ ] The rule files do what section 4 says

## Configuration

- [ ] Lock configuration, on by default
- [ ] Admin editing in game, without file access
- [ ] Command access levels

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | _pending_ |

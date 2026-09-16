# Elite Creatures Reborn - specification: Multiplayer scaling

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers scaling a creature by **how many players are nearby**. It is not about the mod working in
multiplayer - every feature does that, and each feature file says how. This is one specific mechanic: a group
should not trivialise content a solo player finds hard.

Star scaling is `scaling.md` and is a different axis entirely; the two multiply together.

**Status: specified, not built.** Nothing in this file exists in the mod.

---

# 1. What it does

**Creature health and damage scale with how many players are nearby.**

Valheim's creatures are built for the player in front of them, so a party of five meets the same troll a solo
player does and removes it in a fifth of the time. This restores the fight.

## Health scales considerably harder than damage

The important asymmetry in the feature, and the reason it is written down here rather than left as two equal
sliders:

> **More players should mean a longer fight, not a fight where one person is deleted in a single hit.**

Health is the number that makes a group fight last. Damage is the number that decides whether an individual player
survives a mistake, and it does not care how many friends they brought - a troll that hits five times as hard
because five people are present kills the one who stepped forward, every time, and the answer becomes nobody ever
steps forward.

So health climbs steeply with the player count and damage climbs gently. It is the same stance `scaling.md` takes
between stars and health, pointed the other way, and for the same reason: the fight should get *longer and more
demanding*, not more arbitrary.

---

# 2. Phantom players

**A small group can tell the server to treat them as larger.** Two friends who want the fights of a party of five
just say so.

This exists because the scaling is otherwise a tax that only ever goes one way. A duo who find the game too easy
have no lever at all - they cannot recruit three more people to get harder fights - and the numbers this feature
computes are exactly the ones they want. Phantom players hand them the dial.

---

# 3. The ceiling

Settings cover the radius that counts as nearby, the amount added per player, **and a ceiling** - so a ten-player
raid does not produce something unkillable.

The ceiling matters more than it looks. Without it the feature is an unbounded multiplication that a large server
discovers by accident the first time ten people happen to stand in one place, and what they discover is a troll
nobody can hurt.

---

# 4. Multiplayer

Unlike every other feature in the mod, this one **cannot be rolled once and stored**. The player count near a
creature changes while the creature is alive - that is the entire mechanic - so it is a continuously evaluated
value rather than a trait.

- **The creature's owner counts nearby players and applies the scaling.** One machine decides, so there is no
  disagreement about how many players are present.
- **It is re-evaluated as players arrive and leave**, which is what makes reinforcements meaningful.
- **Health must be handled carefully when the count drops.** A creature scaled up for five players and then left
  with one must not be healed back to full or instantly killed by the maximum falling below its current health.
  The rule is that the *proportion* of health remaining is preserved across a rescale: a creature at half health
  stays at half health.
- **Nothing about this is written to the ZDO as a trait**, because it is not one. It is derived from the world
  each time.
- **The damage multiplier is applied where the game resolves damage**, on the same machine that already decides
  it, so no extra message is sent per hit.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In the main settings file's multiplayer section:

- **An off switch** for the whole feature. With it off, creatures are the same whether one player is present or
  ten, and every other feature keeps working.
- **Radius** that counts as nearby.
- **Health added per player**, and **damage added per player**, separately - they are deliberately not one
  setting.
- **The ceiling**, as a maximum player count that counts, a maximum multiplier, or both.
- **Phantom players**: how many extra players the server pretends are present.

---

# 6. Open decisions

**Every number.** `../SPEC.md` specifies the shape of this feature and none of its quantities: there is no radius,
no per-player health figure, no per-player damage figure and no ceiling written down. "Health scales considerably
harder than damage" is a direction, not a table. These want proposing before the code starts.

**Whether phantom players are a server setting or a group's own.** The description - "two friends who want the
fights of a party of five just say so" - reads like something the group controls, but a value that any player can
raise is also a value any player can raise for *everyone nearby*, including people who did not ask for it. Whether
it is admin-only, per-server, or somehow scoped to a party is not decided, and it is the difference between a
feature and an argument.

**Whether tamed creatures and phantom players interact.** A player with five tamed wolves is not five players, but
the count has to be defined against something. Tamed creatures presumably do not count - see `tamed-creatures.md` -
but it is not written down.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## The mechanic

- [ ] Health and damage scale with nearby player count
- [ ] Health climbs steeply, damage gently - deliberately not one setting
- [ ] A ceiling, so a large group cannot produce something unkillable
- [ ] Phantom players: a small group can ask to be treated as larger

## Multiplayer

- [ ] The creature's owner counts nearby players and applies the scaling
- [ ] Re-evaluated as players arrive and leave
- [ ] The proportion of health remaining is preserved across a rescale
- [ ] Nothing written to the ZDO as a trait - it is derived from the world each time
- [ ] The damage multiplier applied where the game resolves damage; no extra message per hit

## Configuration

- [ ] Off switch, radius, health per player, damage per player, ceiling, phantom players

## Blocked on a decision

- [ ] Every number - radius, per-player figures, ceiling - is unspecified.
- [ ] Whether phantom players are a server setting or a group's own.
- [ ] Whether tamed creatures count toward the player count.

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |

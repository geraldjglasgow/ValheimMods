# Elite Creatures Reborn - specification: Loot

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers how a kill decides what it drops: the four modes, how stars raise quantities, and why trophies
are handled on their own. **Boss loot** is multiplied further by the boss's aspect - that table is in
`boss-aspects.md`. **Dungeon chests refilling over time** is a different feature entirely and lives in
`scaling.md`. Changing an item once it exists - weight, stack size, what survives death - is `item-rules.md`.

Numbers are defaults and all of them are configurable.

**Status: partly built.** The per-star quantity multiplier is built and shipping: a creature's drop quantities are
scaled by its star count, which is the Scaled mode below. The **mode switch does not exist** - there is no Vanilla,
Rolled or Curated - and **trophies are not yet handled separately**, so they currently scale with everything else.
Rolled is meant to be the default, so what ships today is not the intended out-of-the-box behaviour.

---

# 1. Four modes

How drops are decided, chosen server-wide:

| Mode | Behaviour |
| --- | --- |
| Vanilla | Untouched. The mod's other features still work |
| Scaled | The creature's own drop table, with quantities raised by star level |
| Rolled | The creature's own drop table, rolled once more per star, each roll independent |
| Curated | The rule files decide entirely, ignoring the creature's own table |

**Rolled is the default.** Scaled gives predictable abundance - a three-star kill is reliably three-star worth of
resin - and predictable is the less interesting feel. Rolled gives the *chance* of something rare out of a hard
fight, because each extra roll is independent and each one could come up with the thing that only drops five
percent of the time. A hard fight that might pay out unusually is a better story than one that pays out exactly as
arithmetic predicted.

**Vanilla** exists so the mod can be installed for its other features alone, the same way the Vanilla star preset
does in `pressure.md`. **Curated** exists for servers building a custom economy, where the creature's own table is
the wrong starting point rather than a thing to be multiplied.

The mode is one server-wide choice, not a per-creature one. Per-creature adjustment is what the rule files are
for.

---

# 2. Trophies are handled separately

**Trophies are not multiplied by default, whatever the mode.**

Twelve identical trophies from one kill is clutter, not a reward. A trophy is usually wanted once, for a wall, and
every copy after the first is inventory weight the player carries to a chest and then never opens again.

A server that disagrees can switch trophy multiplication on, and then trophies follow the same mode as everything
else. It is a **single setting, off by default**, because the default should suit the player who has not thought
about it yet - and the player who has thought about it will find the setting.

---

# 3. Mutations and attunements do not change loot

Not by default. A mutation is a **change to the fight, not to the reward**: Cloaked makes a greydwarf harder to
find and Plated makes it harder to hurt, and neither is a reason for it to carry more resin.

Stars are the channel that says "this was harder, here is more". Letting mutations pay as well would mean every
trait in the mod eventually became an economic decision rather than a tactical one, and a player would farm the
mutation that paid best rather than fighting whatever the world produced.

A server can make them pay through the rule files if it wants to. The default is that they do not.

**Thieving's pouch is the one exception, and it is not really an exception.** What it drops on death is the
player's own property being handed back, never anything the creature's own loot table produced - so it is never
multiplied by mode, by `drops`, by star count, or by a boss aspect. See `thieving.md` for the full rule; this is
not a mutation "paying" in the sense the paragraph above is about.

Boss aspects are the deliberate exception, and `boss-aspects.md` explains why: an aspect is **visible and
rerollable before you commit**, so without a loot difference a group would simply wait for the easiest one every
time. That argument does not apply to a mutation, which you meet rather than choose.

---

# 4. Multiplayer

- **The drop list is built on the dying creature's owner**, which is where the game builds it, and read from the
  traits in the creature's ZDO. Those traits were rolled once by that same owner, so the multiplier is identical
  wherever the creature was rolled and no message is needed to agree on it.
- **The extra rolls in Rolled mode happen once, on the owner**, not once per client. Every player then sees one
  drop pile, which is the same rule the game already follows for ordinary loot.
- Because the mod keeps creatures at **vanilla level 1** and scales them itself (`scaling.md`), the game applies
  no level loot bonus of its own. The mod's multiplier is the whole story, and there is no second multiplier
  hiding underneath it.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In the main settings file's loot section, with the tables in the creature rule file:

- **Mode**: Vanilla, Scaled, Rolled or Curated.
- **The per-star quantity line** for Scaled, one entry per star count. Already built.
- **Trophy multiplication**: off by default; on makes trophies follow the mode.
- **Per-creature and per-group drop rules** for Curated, and for overriding individual drops in the other modes.
- **Per-tier loot quantity**, so a server can make a mature world pay differently (`world-tiers.md`).

An off switch for the whole feature leaves every other feature working, as `configuration.md` requires of all of
them - the mod with loot off still stars, mutates and scales creatures and simply never touches what they drop.

---

# 6. Open decisions

**What Curated mode's rule file actually looks like** is not specified. Every other mode reuses the creature's own
drop table, so the rule file only has to adjust it; Curated replaces it, which means the rule file needs a way to
express a whole drop table - item, quantity range, chance, and per-star variation - that nothing else in the mod
currently needs. That format wants designing before Curated is built, and the other three modes do not depend on
it.

**Whether Rolled's extra rolls are capped.** Each star adds an independent roll, and `pressure.md` sets no maximum
star ceiling. A twenty-star creature on a server that has raised the ceiling would roll its whole drop table
twenty-one times. That is arguably the point, but it is the same shape of problem as the Splintering cascade in
`mutations.md`, and that one has caps.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The modes

- [ ] Vanilla, Scaled, Rolled and Curated all selectable
- [~] Per-star quantity multiplier for Scaled - built and shipping
- [ ] Trophies handled separately, off by default
- [ ] Mutations and attunements do not change loot

## Multiplayer

- [ ] Drop list built on the dying creature's owner, where the game builds it
- [ ] Rolled mode's extra rolls happen once on the owner, not once per client

## Configuration

- [ ] Mode selection
- [ ] Per-creature and per-group drop rules for Curated, and per-drop overrides elsewhere
- [ ] Per-tier loot quantity

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |

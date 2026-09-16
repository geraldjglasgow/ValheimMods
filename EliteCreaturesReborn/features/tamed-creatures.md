# Elite Creatures Reborn - specification: Tamed creatures and breeding

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what happens to traits when a creature is tamed, bred, and raised. What the traits themselves do
is `mutations.md` and `attunements.md`; what stars are worth is `scaling.md`.

**Status: specified, not built.** Nothing in this file exists in the mod. Tamed creatures currently keep whatever
they were rolled with and pass nothing on.

---

# 1. Traits survive taming, and are inherited

**Tamed creatures keep their traits, and pass them on.** Offspring inherit from their parents, and a young
creature keeps what it had when it grows up.

Three separate guarantees, and all three have to hold or the feature does not work:

- **Taming does not strip traits.** A two-star Leeching wolf you tamed is still a two-star Leeching wolf.
- **Offspring inherit from both parents**, by the rule in section 2.
- **Growing up preserves what a juvenile had.** A trait that vanished at adulthood would make every breeding
  project unverifiable until the moment it failed.

---

# 2. Why this matters more than it looks

This is the most interesting consequence of the whole trait system, and it is worth protecting deliberately:

> **A bred line of Packbound wolves is a project.**

Everything else in the mod happens *to* the player - the world rolls a creature and the player deals with it.
Breeding is the one place where the player acts on the trait system rather than reacting to it. They choose which
creature to tame, which pair to breed, which offspring to keep, and over many generations they get something that
the world would never have handed them.

That makes inheritance a feature to design carefully rather than a detail of taming. If traits were stripped on
taming, the entire mod would be something that only ever happened to you.

---

# 3. The inheritance roll

**Each parent's mutation passes to the offspring with a 40% chance, rolled separately.**

Both parents may pass, one may, or neither. The outcomes are therefore ordinary and varied rather than
deterministic - a pair of Leeching wolves produces a Leeching pup a little over half the time, and produces
nothing special a little under half.

**Where both pass and they share a colour, one of the two is taken at random**, since a creature never carries two
of a colour (`mutations.md`).

The 40% is chosen to make breeding **a gamble that rewards persistence rather than a guarantee that rewards one
lucky capture.** A higher number would mean the first good pair a player tames settles the question permanently. A
much lower one would mean nobody ever finishes a line. Forty percent means a player who wants a stable
double-trait line has to breed through several generations and cull, which is a project with a middle rather than
a transaction with an outcome.

---

# 4. Multiplayer

Traits follow the same rule as everywhere else - **rolled once by the owner, stored in the ZDO, read by everyone** -
with one addition specific to this feature.

- **The inheritance roll is performed by the owner of the offspring** at the moment it is created, reading both
  parents' traits from their ZDOs, and written to the offspring's ZDO once. It is never re-rolled.
- **A breeding project survives everything the game survives.** Traits live in the ZDO, so they persist across
  logouts, server restarts and the creature being unloaded and reloaded. A player who spent four evenings on a
  wolf line must not lose it to a restart, and putting the roll anywhere but the ZDO would do exactly that.
- **Growing up must not re-roll.** The juvenile-to-adult transition replaces the creature, and the traits have to
  be carried across that transition explicitly. This is the case most likely to be got wrong, and it is invisible
  until someone has bred for an hour.
- **Every client sees the same pup**, because every client reads the same ZDO and draws the name and colours
  locally (`creature-naming.md`).

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In the creature rule file, under the conditions the file already supports - `tamed` is one of them:

- **An off switch** for inheritance, leaving taming itself untouched: tamed creatures keep their traits, offspring
  get nothing.
- **The pass-on chance**, defaulting to 40%.
- **Whether stars are inherited**, and how - see the open decision below.
- **Per-trait rules under the `tamed` condition**, so a server can change or disable what a mutation does once its
  carrier is tamed.

---

# 6. Open decisions

**Whether stars are inherited, and how.** This is the significant gap. The specification is explicit about
mutations - 40%, per parent, independently rolled - and says only that offspring "inherit from their parents"
about everything else. Stars are the mod's primary power axis, and an uncontrolled star inheritance is the one
thing here that could genuinely break a server: a player who breeds five-star wolves has an army the world cannot
answer.

At least three plausible rules, and they give very different games:

1. **Stars are not inherited.** Offspring roll from local pressure like anything else. Safest, and it makes
   breeding purely about mutations.
2. **Stars average the parents**, possibly rounding down. Predictable, and it means a line converges rather than
   climbing.
3. **Stars inherit with a small chance of one more or one less.** The most interesting to play with and the one
   that needs a ceiling, because it is the rule under which a patient player eventually reaches the star cap.

Not decided. It should be, before the code starts.

**Whether an attunement inherits on the same 40%.** The rule is written for mutations. An attunement is an
independent channel and the file does not say whether it follows the same roll, a different one, or none.

**Whether tamed creatures count for multiplayer scaling.** They presumably do not - see
`multiplayer-scaling.md` - but a player with a bred pack of eight wolves is the case where it would matter, and
nothing says either way.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## Inheritance

- [ ] Taming does not strip traits
- [ ] Offspring inherit from both parents by the section 3 roll
- [ ] Growing up preserves what a juvenile had

## Multiplayer

- [ ] The inheritance roll is performed by the owner of the offspring, once, at creation
- [ ] Traits live in the ZDO and survive everything the game survives
- [ ] The juvenile-to-adult transition must not re-roll
- [ ] Every client sees the same pup

## Configuration

- [ ] Off switch for inheritance, leaving taming itself untouched
- [ ] Pass-on chance, default 40%
- [ ] Per-trait rules under the `tamed` condition

## Blocked on a decision

- [ ] Whether stars are inherited, and how - see Open decisions.

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | _pending_ |

# Elite Creatures Reborn - specification: Tamed creatures and breeding

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what happens to traits when a creature is tamed, bred, and raised. What the traits themselves do
is `mutations.md` and `attunements.md`; what stars are worth is `scaling.md`.

**Status: built, not tested in game.** Inheritance, eggs and growing up are in the mod as of 3.6.0.
Per-trait rules under a `tamed` condition are not built (section 6).

---

# 1. Traits survive taming, and are inherited

**Tamed creatures keep their traits, and pass them on.** Offspring inherit from their parents, and a young
creature keeps what it had when it grows up.

Three separate guarantees, and all three have to hold or the feature does not work:

- **Taming does not strip traits.** A two-star Leeching wolf you tamed is still a two-star Leeching wolf. Taming
  changes nothing about the creature object or its stored traits, so this holds without code of its own.
- **Offspring inherit from both parents**, by the rule in section 3.
- **Growing up preserves what a young creature had.** The game replaces a grown cub, piglet, calf or chick with a new
  adult; the mod carries the traits across that replacement explicitly. A trait that vanished at adulthood would
  make every breeding project unverifiable until the moment it failed.

**One per-trait exception, specified in `thieving.md`: a tamed Thieving creature never steals**, from its owner or
from anyone. It keeps the mutation - it still shows on the nameplate, and it is bred and inherited by this feature's
own rules - but the theft itself stops the moment it is tamed.

---

# 2. Why this matters more than it looks

This is the most interesting consequence of the whole trait system, and it is worth protecting deliberately:

> **A bred line of Packbound wolves is a project.**

Everything else in the mod happens *to* the player - the world rolls a creature and the player deals with it.
Breeding is the one place where the player acts on the trait system rather than reacting to it. They choose which
creature to tame, which pair to breed, which offspring to keep, and over many generations they get something that
the world would never have handed them.

---

# 3. The inheritance roll

Decided with the user on 2026-09-26, replacing the earlier 40%-per-parent draft.

**Mutations: one of the parents', whenever either has one.** The newborn takes exactly one mutation, picked at random
from both parents' mutations together, whenever either parent carries one. Two Leeching wolves always have a Leeching
pup; a Leeching wolf and a Mad wolf have a pup that is one or the other, even odds. A mutation switched off in
`mutations enabled` is never passed on. The chance is a rule-file setting, 100 by default.

**Stars: equal odds from 0 up to the stronger parent's.** A 4-star and a 1-star can have a pup of 0, 1, 2, 3 or 4
stars, 20% each. A newborn never has more stars than its stronger parent, so a line cannot climb; it drifts down
unless the player keeps the strong ones and culls the rest. The user chose the stronger parent as the cap (so
breeding a strong animal with a weak one is still worth doing) and equal odds below it (so a line decays unless
tended), over the lower parent as the cap and over "usually the cap".

**Two plain parents have plain young.** A newborn never rolls anything new - no wild star roll, no mutation roll,
no world tier (`world-tiers.md`).

**The second parent** is the nearest tamed partner of the right species within the game's own partner range at the
moment the pregnancy starts. The game only counts that a partner is close, it never names one, so the mod finds it
then and remembers its traits until the birth - the partner may have wandered off by the time the young arrive. A
creature that breeds without a partner, or a pregnancy that began before 3.6.0, has one parent: its own traits alone.

**Attunements** do not exist in the mod yet; when they do, whether they inherit is still open (section 6).

---

# 4. Eggs

Hens lay eggs rather than bearing young, as does any creature whose offspring is an egg that hatches. **The egg
carries the traits it was laid with**, and the chick that hatches from it takes them.

- The traits are written into the egg item's own data, which the game keeps with the item wherever it goes - on the
  ground, in an inventory, in a chest - so carrying an egg to a warm spot does not lose them.
- The egg's stars are also written as its **quality**, the way the game already passes a parent's level to an egg,
  so eggs of different stars do not stack.
- **An egg says what it will hatch**: its hover text on the ground and its tooltip in an inventory end with a line
  such as "Hatches with 2 stars, Leeching", the mutation in its star colour, or "Hatches plain". Otherwise the
  inheritance would be invisible until the chick appeared.
- **A stack keeps one set of traits** - those of the egg it was stacked onto. The game will not hatch a stack anyway,
  so this only matters for eggs of the same stars and different mutations carried together.
- An egg with no traits from this mod (one from before 3.6.0, or from anywhere else) hatches a chick that rolls
  like a wild creature.

---

# 5. Multiplayer

Traits follow the same rule as everywhere else - **rolled once by the owner, stored in the ZDO, read by everyone** -
and every step of breeding runs on one machine: the owner of the parent, which is where the game runs its own
breeding.

- **Conception** runs on the pregnant parent's owner: it finds the partner nearby and writes the partner's traits
  onto the parent's ZDO, so a change of owner before the birth loses nothing.
- **The birth** runs on the same owner, which instantiates and therefore owns the newborn. The inheritance roll is
  made once, at that moment, and written to the newborn's ZDO as its traits. It is never re-rolled.
- **Hatching and growing up** run on the owner of the egg or the young creature, which also owns what replaces it.
- **A breeding project survives everything the game survives.** Traits live in the ZDO and the egg's item data, so
  they persist across logouts, server restarts and the creature being unloaded and reloaded.
- **Every client sees the same pup**, because every client reads the same ZDO and draws the name and colours
  locally (`creature-naming.md`).

---

# 6. Configuration

A top-level `breeding:` block in `creature_rules.yml`, written with its comments on first run, hot reloaded, bound
to the server's copy while `lock to server` is true. A rule file from before 3.6.0 has no such block and takes the
defaults - breeding on.

```yaml
breeding:
  enabled: true
  mutation chance: 100
```

- **`enabled: false`** lets newborns roll their stars and mutations from their biome like wild creatures, the
  behaviour before 3.6.0. Taming is untouched either way, and **growing up always keeps traits**, whatever the switch
  says - that is not inheritance, it is the same creature changing shape.
- **`mutation chance`** is the percent chance a newborn takes one of its parents' mutations when either has one,
  0 to 100.

## Open decisions

**Per-trait rules under a `tamed` condition**, so a server can change what a mutation does once its carrier is
tamed. Not built, and it matters more now that breeding makes mutated pets common: a tamed Miasmic creature still
trails poison that hurts players, a tamed Bloated one still explodes when it dies, and a tamed Devouring one still
eats other creatures. Whether those should stop, soften or stay is not decided.

**Whether an attunement inherits.** Attunements are not built.

**Whether tamed creatures count for multiplayer scaling.** They presumably do not - see `multiplayer-scaling.md` -
but a player with a bred pack of eight wolves is the case where it would matter, and nothing says either way.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## Inheritance

- [~] Taming does not strip traits - holds by construction, not tested in game
- [~] Offspring inherit from both parents by the section 3 roll - built, not tested in game
- [~] The partner is found at conception and remembered until the birth - built, not tested in game
- [~] Growing up preserves what a young creature had - built, not tested in game

## Eggs

- [~] A laid egg carries its traits in its item data and its stars as its quality - built, not tested in game
- [~] The chick takes the egg's traits; an egg without them hatches wild - built, not tested in game
- [~] The traits survive the egg being carried and put down again - by the game's own item saving, not tested
- [~] The egg's hover text and tooltip say what it will hatch - built, not tested in game

## Multiplayer

- [~] The inheritance roll is performed by the owner of the offspring, once, at creation - built, not tested on a
  dedicated server
- [~] Traits live in the ZDO and survive everything the game survives - built, not tested in game
- [~] The juvenile-to-adult transition does not re-roll - built, not tested in game
- [~] Every client sees the same pup - follows from the ZDO, not tested in game

## Configuration

- [~] Off switch for inheritance, leaving taming and growing up untouched - built, not tested in game
- [~] Mutation chance, default 100 - built; parser exercised outside the game

## Not built

- [ ] Per-trait rules under the `tamed` condition - see Open decisions

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-26 | User settled the roll: one parent mutation always, stars 0 to the stronger parent's at equal odds. Built conception, birth, eggs, hatching and growing up, and the `breeding:` block. Rule parser exercised outside the game. | EliteCreaturesReborn-v3.6.0 |

# Elite Creatures Reborn - specification: World tiers

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the world tier: what it is, what raises it, and what it changes. The tier is an **input** to the
spawn roll - it decides nothing on its own, it leans the rolls every biome already makes.

**Status: built, not tested in game.** Everything in sections 1 to 6 is in the mod as of 3.6.0. Map
rings are not built (section 7).

---

# 1. What a tier is

**A single number for the whole world**, 0 for an untouched world and one higher for each boss that has fallen. With
the seven vanilla bosses that is **tier 0 to tier 7**: tier 7 is a world where everything has fallen and nowhere is
safe.

It is one number for the whole map, not a per-biome or per-region value. The map already varies by place - that is
what biomes are for. The tier is the axis that varies by **progress**, so that a Meadows at tier 6 is not the Meadows
you started in.

---

# 2. What raises it

**The first defeat of each listed boss, by anyone on the server, in any order.** Killing a boss again changes
nothing. Decided with the user on 2026-09-26, over the alternative of counting every kill (which would let a group
farming Eikthyr for trophies reach tier 7 in an evening).

The tier is read from the game's own record of defeated bosses: every boss sets a world key when it dies
(`defeated_eikthyr`, `defeated_gdking`, `defeated_bonemass`, `defeated_dragon`, `defeated_goblinking`,
`defeated_queen`, `defeated_fader`). The tier is the number of listed keys the world carries. That has three
consequences worth knowing:

- **A world that already killed bosses before the mod was installed starts at that tier.** The world has progressed,
  and the tier says so.
- **A modded boss counts once its key is added to the list.** `elite tier` prints every boss the game knows with its
  key, so the key never has to be guessed.
- **An admin can try a tier out with the game's own `setkey` and `removekey`.** No command of the mod's own sets the
  tier; the tier always agrees with the world's history.

Earlier drafts of this file offered four sources - Personal, Server, Elapsed and Manual. The user settled on boss
defeats, server-wide, and the other three are not planned. That also retires the old open question of how Personal
could work when a creature is rolled once by its owner.

---

# 3. What varies by tier

**Star and mutation chances, in every biome**, through two lines in the rule file with one entry per tier (index 0 =
no boss down), the last entry repeating past the end - the same convention as the star lines.

- **Star boost.** Each star count's weight in a biome's `star chances` is multiplied by the boost once per star, and
  the row is scaled back to 100. At 1.5 a one-star creature becomes 1.5 times as likely against an unstarred one, a
  two-star 2.25 times, a five-star 7.6 times. Every biome keeps its own character; the whole row moves upward, and
  the top end moves most.
- **Mutation boost.** Multiplies every mutation chance, capped at 100. `max mutations` still caps how many one
  creature carries.

**Defaults, a judgement call** (tunable, arguable): star boost `[1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7]`, mutation
boost `[1, 1.15, 1.3, 1.45, 1.6, 1.75, 1.9, 2]`. What the star boost does to the default rows:

| Biome | Tier 0 unstarred / 5-star | Tier 2 | Tier 4 | Tier 7 |
| --- | --- | --- | --- | --- |
| Meadows | 73% / 1% | 65% / 2.2% | 56% / 4.2% | 44% / 8.6% |
| Black Forest | 62% / 2% | 52% / 4.2% | 43% / 7.4% | 30% / 13.8% |
| Plains | 32% / 5% | 23% / 9% | 16% / 13.8% | 10% / 21.6% |
| Ashlands | 16% / 4% | 11% / 6.7% | 7% / 9.8% | 4% / 14.9% |

Two things deliberately do **not** read the tier:

- **Bosses.** A boss rolls on its own table, the same at every tier - a boss is a set-piece a group prepares for.
- **Newborns of tamed parents.** They inherit from their parents (`tamed-creatures.md`); the world does not reach
  into a breeding pen.

**Respawning** needs no special case: a respawned camp's creatures are new creatures, so they roll at the world's
**current** tier, not the tier the camp was cleared at (`scaling.md`).

**Pressure** (`pressure.md`) is not built. When it is, the tier becomes one of its terms; until then the two boost
lines are how the tier reaches the roll.

---

# 4. What a player sees

- **A tier rise is announced** to everyone, in the middle of the screen: "The world hardens: tier 3 of 7". A world
  that silently got harder looks like bad luck. A tier that falls (an admin's `removekey`) is not announced.
- **`elite tier`** prints the tier, what the two boosts are right now, every listed boss with whether it is down,
  and any boss the game knows that the list leaves out. It only reads, so it is open to every player, admin or not
  (`console-commands.md`).
- **`elite inspect`** says which tier a wild creature was rolled at, when it was above 0.

---

# 5. Multiplayer

The tier is **world state**, not creature state, and it is the one value every machine must agree on continuously.

- **The server owns it, through the game's own keys.** A boss's death sends its key to the server, which stores it
  and pushes the key list to every client on join and on every change. The tier is derived from that list, so the
  client that owns and rolls a creature reads the same tier as the server with no message of the mod's own. Nothing
  about the tier is saved by the mod.
- **The rules are the server's.** The `world tiers:` block is part of the rule file the server pushes, so every
  machine uses the same key list and boost lines.
- **The announcement comes from the server.** It compares the tier before and after each new key and broadcasts a
  rise to everybody, the host included; each client shows it locally. A dedicated server has no screen and skips it.
- **Creatures already alive are not re-rolled** when the tier advances. Traits are rolled once by the owner and
  stored; the tier decides what the *next* creature rolls. The rolled tier is stored with the traits (only when above
  0) so `elite inspect` can report it on any machine.

---

# 6. Configuration

A top-level `world tiers:` block in `creature_rules.yml`, written with its comments on first run, hot reloaded, bound
to the server's copy while `lock to server` is true. A rule file from before 3.6.0 has no such block and takes the
defaults - tiers on.

```yaml
world tiers:
  enabled: true
  bosses: [defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader]
  star boost:     [1, 1.1,  1.2, 1.3,  1.4, 1.5,  1.6, 1.7]
  mutation boost: [1, 1.15, 1.3, 1.45, 1.6, 1.75, 1.9, 2]
```

- **`enabled: false`** holds the world at tier 0: both lines use their first entry, nothing is announced, and every
  other feature keeps working.
- **`bosses`** is the list of keys that count, one tier each. Keys are matched without regard to case; a key listed
  twice counts once; an empty list keeps the world at tier 0. A listed key that no boss the game knows sets is logged
  as a warning when the world starts.
- **A negative boost entry** is an error on its line and is read as 0.

---

# 7. Open decisions

**Map rings.** Earlier drafts wanted tier boundaries drawn on the map, off by default. With one tier for the whole
world there are no boundaries to draw; if rings come back they would have to show pressure, not the tier. Not built,
and waiting on `pressure.md`.

**Whether bosses should read the tier.** Deliberately not, for now (section 3). A server that wants later bosses
harder can already raise the boss star table.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## The tier

- [~] The tier counts the listed bosses' defeat keys, each once - built, not tested in game
- [~] Star boost and mutation boost lean every biome's roll by tier - built; parser and weighting exercised outside
  the game, rolls not seen in game
- [~] Bosses and newborns ignore the tier - built, not tested in game
- [~] Respawning brings a cleared camp back at the world's current tier - follows from the roll, not tested in game
- [ ] Pressure takes the tier as one of its terms - waits on `pressure.md`

## What a player sees

- [~] A tier rise is announced to everyone - built, not tested in game
- [~] `elite tier` reports tier, boosts and bosses, open to every player - built, not tested in game
- [~] `elite inspect` shows the rolled tier - built, not tested in game

## Multiplayer

- [~] The tier is derived from the server's global keys on every machine - built, not tested on a dedicated server
- [~] The announcement is decided on the server and shown on every client - built, not tested on a dedicated server
- [~] Creatures already alive are not re-rolled when the tier advances - by construction, not tested in game

## Configuration

- [~] `world tiers:` block: switch, boss key list, star boost, mutation boost - built; parser exercised outside the
  game including bad values and an old file without the block
- [~] Off switch - with tiers off the world sits permanently at tier 0 - built, not tested in game
- [~] A listed key no boss sets is logged at world start - built, not tested in game

## Not planned or waiting

- [ ] Map rings - see Open decisions

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-26 | User settled the source: first defeat of each boss, server-wide; Personal, Elapsed and Manual dropped. Built the tier from the game's defeat keys, the two boost lines, the announcement, `elite tier` open to all, the rolled tier in `elite inspect`. Rule parser exercised outside the game. | EliteCreaturesReborn-v3.6.0 |

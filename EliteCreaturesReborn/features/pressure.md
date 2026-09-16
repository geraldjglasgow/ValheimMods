# Elite Creatures Reborn - specification: Pressure and star counts

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers **how many stars a creature gets**: the pressure value that decides it, the range stars run over,
the presets, and the creatures the game itself refuses to level. What a star is *worth* once a creature has one -
health, damage, speed, size, loot - is in `scaling.md`, and bosses roll their stars on their own table there
rather than from pressure at all.

Numbers are defaults and all of them are configurable. Where a number is a judgement call it says so.

**Status: specified, not built.** The mod currently rolls star counts from a per-biome chance table. Pressure,
the distance term and the six presets do not exist in the code yet. Building this replaces how the roll is
decided; it changes nothing about what a star then does.

---

# 1. Pressure, not a flat roll

A creature's star count is drawn from a **pressure** value assigned at spawn, rather than from a fixed percentage
table. Three things raise pressure:

- **Biome.** Meadows is the floor, Ashlands and Deep North the ceiling.
- **World tier.** Bosses that have fallen push pressure up everywhere. See `world-tiers.md`.
- **Distance from the nearest player-built hearth.** Far from anyone's fire, the world is harder.

The distance term is the one that matters most, and it is there for a specific reason: **it gives the player a
lever they can pull with a hammer instead of a config file.** Wilderness is genuinely wild, settled land is
calmer, and building an outpost is rewarded rather than punished. A player who wants harder fights walks further
out; a player who wants a quiet base builds one.

Pressure runs **0 to 100** and is a single number the player can read - `elite pressure` prints it where you
stand, and what contributed to it (`console-commands.md`). That is deliberate: the system is meant to be legible
rather than mysterious. A player who notices the fights getting harder should be able to find out why in one
command.

Retaliation zones (`retaliation-zones.md`) raise pressure locally on top of all this, which is why they are
expressed in the same units.

---

# 2. Range and scaling

Stars run **0 to 5 by default**. The ceiling is configurable with **no enforced maximum** - a server that wants
twenty-star creatures may have them.

One caveat, and it is the only one: the **Splintering** cascade in `mutations.md` produces copies from a killed
creature, and its length grows with the star ceiling. A high ceiling and Splintering together are the case where
the cascade caps - off by default - are worth turning on. Nothing else in the mod cares how high the ceiling goes.

Bosses scale on their own separate settings and never on these. See `scaling.md` for the boss table and
`boss-aspects.md` for what else a boss carries.

---

# 3. Creatures the game never levels

Valheim withholds stars from some creatures entirely: they have no starred form and never spawn with one. The mod
can grant them stars anyway, which is how a server makes the whole world dangerous rather than the two thirds of
it the game happens to have built star art for.

This is **off by default and set per creature** in the creature rule file, never a blanket switch. Some of these
creatures are withheld for good reason - a starred version may look wrong, sit badly in its animations, or
trivially break a quest or a boss fight. Turning one on is a decision a server makes deliberately, one creature at
a time, having looked at it.

---

# 4. Presets and custom tables

Six ready-made presets set the whole pressure-to-stars relationship in one choice, for players who do not want to
tune tables:

**Vanilla, Gentle, Settled, Harsh, Savage, Merciless.**

Each preset is a curve giving the **average** star count at five points along the pressure scale. A creature's
actual count is drawn from a small spread around that average, so no two clearings feel identical.

| Preset | P0 | P25 | P50 | P75 | P100 |
| --- | --- | --- | --- | --- | --- |
| Vanilla | 0.1 | 0.3 | 0.6 | 0.9 | 1.2 |
| Gentle | 0.1 | 0.4 | 0.9 | 1.4 | 2.0 |
| Settled | 0.2 | 0.7 | 1.4 | 2.2 | 3.0 |
| Harsh | 0.3 | 1.0 | 2.0 | 3.0 | 4.0 |
| Savage | 0.5 | 1.4 | 2.6 | 3.8 | 4.6 |
| Merciless | 0.8 | 2.0 | 3.3 | 4.4 | 5.0 |

**Settled is the default.**

**Vanilla reproduces the game's own 0-2 star behaviour**, so the mod can be installed for its other features
alone - somebody who wants mutations and nothing else gets exactly that. It is additionally **hard-capped at 2
stars** whatever the curve says, because reproducing the game's own behaviour is the entire point of it.

A player wanting finer control switches to a **custom table** and writes chance rows per world tier directly. The
preset is then ignored, and configuration UIs hide the preset setting while a custom table is selected
(`configuration.md`).

These curves are a starting point to be adjusted after play, **not a claim to be balanced**. They are six curves
invented before the pressure formula has ever run, and `../SPEC.md` lists them third among the judgement calls
most likely to be wrong.

---

# 5. Multiplayer

The rule everything in this mod follows: **rolled once by the owner, stored in the ZDO, read by everyone.**

- Pressure is evaluated by the **owner** at the moment the creature is first resolved, and the resulting star
  count is written to the ZDO under this mod's own key prefix. It is never re-rolled.
- Pressure itself is **not** stored. It is an input to one roll, not creature state, and a creature does not
  become weaker because somebody later built a hearth nearby.
- `elite pressure` computes the value locally for the player who ran it, from the same terms, so it needs no
  message to the server.
- The **hearth distance** term reads structures the game already replicates, so every machine would arrive at the
  same answer - but only the owner's answer is ever used.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 6. Configuration

- **Preset**, one of the six, or **custom**, which switches to per-tier chance rows written directly.
- **Star ceiling**, no enforced maximum.
- **Per-biome pressure floor and ceiling.**
- **The weight of each pressure term** - biome, world tier, hearth distance - so a server that dislikes the
  distance mechanic can set it to zero and keep the rest.
- **Per creature: may this creature take stars at all**, for the creatures the game never levels. Off by default,
  one creature at a time.

All of it is bound to the server's copy while lock-to-server is true, and hot-reloaded on edit.

---

# 7. Open decisions

**The shape of the spread.** This file says a creature's count is drawn from "a small spread" around the preset's
average. How wide that spread is, and whether it is symmetric, is not specified. It wants deciding before the code
starts, because it is the difference between a preset being a promise and a preset being a suggestion.

**What counts as a hearth.** "The nearest player-built hearth" needs one concrete answer: a fire, a workbench, a
bed, or any player-built piece at all. Each gives a different game - a fire is a deliberate act, any piece at all
means a dropped workbench pacifies a region. Not yet chosen.

# Elite Creatures Reborn - specification: World tiers

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the world tier: what it is, the four things it can follow, and what it changes. The tier is an
**input** to almost everything else - it is one of the three terms that raise pressure in `pressure.md`, and every
rule table in the mod can vary by it. It decides nothing on its own.

**Status: specified, not built.** Nothing in this file exists in the mod. The rule files already have the shape
for per-tier rows; nothing fills them in yet.

---

# 1. What a tier is

**A single number for the whole world, 0 to 7**, that raises pressure everywhere as the world progresses.

Tier 0 is an untouched world. Tier 7 is a world where everything has fallen and nowhere is safe.

It is one number for the whole map, not a per-biome or per-region value. The map already varies by place - that is
what biomes and the hearth-distance term in `pressure.md` are for. The tier is the axis that varies by **time**,
so that a Meadows at tier 6 is not the Meadows you started in.

---

# 2. Four sources

How the tier advances, chosen per server:

| Source | The tier follows |
| --- | --- |
| Personal | Bosses that *this player* has killed |
| Server | Bosses killed by anyone on the server |
| Elapsed | Days the world has been alive |
| Manual | Only what an admin sets |

**Server is the default.** It makes the world one shared place, which is what a group playing together wants: the
world hardened when Eikthyr fell, for everyone, and that is a thing the group did together.

**Personal** exists for one specific problem: a new player joining a mature server and being flattened on arrival.
With Personal, their world is at the tier their own progress has earned. The cost is that two players standing in
the same field are fighting different creatures, which is strange in a different way - so it is a choice a server
makes knowing both halves.

**Elapsed** suits a long-running world where nobody is rushing bosses, and where the danger should grow because
time passed rather than because someone finally got round to Moder.

**Manual** hands the whole thing to an admin and the `elite tier` command (`console-commands.md`), which is also
the only source where that command can *set* the tier rather than just report it.

---

# 3. What varies by tier

**Every chance and value in the mod can differ by tier**: star chances, mutation frequency, attunement frequency,
loot quantity, item rules. This is where a server expresses its difficulty curve - not as one multiplier, but as
eight rows of whatever it wants each stage of the world to feel like.

Two features read the tier for something other than a table lookup:

- **Pressure** takes it as one of three terms, so the tier raises star counts everywhere without anyone writing a
  per-tier star table at all (`pressure.md`).
- **Respawning** brings a cleared camp back at the world's **current** tier, not the tier it was cleared at
  (`scaling.md`). The world moves on, and a camp cleared in the Meadows era should not still be a Meadows-era camp
  once everything else has hardened.

---

# 4. What a player sees

**Tier boundaries can be drawn on the map as rings**, so progression is something you can look at rather than read
out of a config file.

**Optional and off by default** - some players want the world unlabelled, and a map with rings on it is a map that
has told you where the game gets harder before you have been there.

`elite tier` prints the current tier and its source at any time, and is one of the read-only commands that is safe
to leave open to everyone on a locked server.

---

# 5. Multiplayer

The tier is **world state**, not creature state, and it is the one value in the mod that every machine must agree
on continuously rather than once.

- **Under Server, Elapsed and Manual the server owns the tier** and sends it to clients on join and on every
  change. A client never computes it.
- **Under Personal each client's tier is its own**, computed from that player's own boss kills. The server still
  owns the *rules*; only the index into them differs per player.
- **A tier change is announced**, because a world that silently got harder is indistinguishable from bad luck -
  the same argument `retaliation-zones.md` makes about zones.
- **Creatures already alive are not re-rolled** when the tier advances. Traits are rolled once by the owner and
  stored, and that does not change here: the tier decides what the *next* creature rolls. A world that hardened
  ten minutes ago is populated by creatures that hardened as they spawned.
- **The map rings are drawn locally** by each client from the tier and the boundary table. Nothing is sent for
  them.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 6. Configuration

In the main settings file's world tiers section:

- **Source**: Personal, Server, Elapsed or Manual.
- **The tier ceiling**, if a server wants to stop below 7.
- **For Server and Personal**: which bosses advance the tier, and by how much.
- **For Elapsed**: how many days per tier.
- **Map rings**: off by default, with their colours as a per-player display preference
  (`display-preferences.md`).
- **An off switch** for the whole feature. With tiers off the world sits permanently at tier 0, every per-tier
  table uses its first row, and every other feature keeps working.

Configuration UIs hide the settings that do not apply to the chosen source - days-per-tier is not shown while the
source is Server (`configuration.md`).

---

# 7. Open decisions

**What the eight tiers map to under Server.** Valheim has five bosses and the mod has eight tiers, so the mapping
is not one boss per tier. Whether the remaining three come from the Queen and Fader and a final "all of them", or
whether some bosses advance the tier by two, is not written down anywhere. It needs settling before the feature is
built, because the default table is the thing almost every server will run unedited.

**Whether Personal is compatible with shared creatures at all.** Under Personal, two players in one field index
different rule rows - but a creature is rolled **once, by its owner**, using that owner's tier. So the creature a
new player meets was rolled at the *veteran's* tier if the veteran happened to be the one whose machine owned it.
That undoes the entire reason Personal exists. Either the roll has to consider the nearest player rather than the
owner, or Personal has to be documented as single-player-only. This is the decision that matters most in this
file.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## The tier

- [ ] Four sources: Personal, Server, Elapsed, Manual
- [ ] Pressure takes the tier as one of its three terms
- [ ] Respawning brings a cleared camp back at the world's current tier

## What a player sees

- [ ] A tier change is announced
- [ ] Map rings, off by default, colours a per-player display preference

## Multiplayer

- [ ] Under Server, Elapsed and Manual the server owns the tier and sends it on join and on change
- [ ] Under Personal each client computes its own from that player's boss kills
- [ ] Creatures already alive are not re-rolled when the tier advances
- [ ] Map rings drawn locally; nothing sent for them

## Configuration

- [ ] Source, tier ceiling, which bosses advance it and by how much, days per tier
- [ ] Off switch - with tiers off the world sits permanently at tier 0

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | _pending_ |

# Elite Creatures Reborn - specification: Stars, scaling and respawning

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what a star is worth, the separate boss table, and how the world repopulates. What a mutation is
and does is in `mutations.md`.

Numbers are defaults and all of them are configurable through `creature_rules.yml`.

**Status:** built and shipping. Verified by build, by reading, and by parsing the written rule file; **not yet
tested in game**. The checklist is at the end.

---

# 1. What a star is worth

A creature is kept at vanilla level 1 and scaled by this mod instead, so the game applies no level bonus of its
own and the table below is the whole story. Six lines, one entry per star count, so index 0 is an unstarred
creature and index 5 a five-star one. An index past the end of a line reuses its last entry, which is what lets a
server raise the ceiling without rewriting every line.

| Line | What it multiplies | Default at 0-5 stars |
| --- | --- | --- |
| `growth` | Size bonus, added to 1 | 0.06, 0.10, 0.15, 0.20, 0.25, 0.30 |
| `hp` | Maximum health | 1, 1.4, 1.95, 2.6, 3.3, 4.0 |
| `attack` | Everything it deals | 1, 1.2, 1.45, 1.75, 2.1, 2.5 |
| `swing speed` | Attack and animation speed | 1, 1.02, 1.05, 1.08, 1.12, 1.16 |
| `speed` | Movement speed | 1, 1, 1.03, 1.06, 1.1, 1.15 |
| `drops` | Loot quantity | 1, 1, 1.5, 2, 2.5, 3 |

**Damage climbs faster than health deliberately.** A high-star creature should be a threat to respect, not a
sponge to chew through.

**Swing speed and movement are deliberately gentle.** Both are what makes a fight readable: a creature whose
animations run hot is not harder, it is unfair, and one that outruns the player removes the option of leaving.

**Loot does not pay until the second star.** A single star is common enough that paying for it would inflate the
whole economy quietly.

## Floors and clamps

Three, and only three:

- **Health** never falls below 5% of base however the multipliers land. Below that a creature is a crash, not a
  fight.
- **Movement** never falls below 5%, so a very well-fed Devouring creature crawls rather than freezing or
  reversing.
- **Run speed** is clamped so no creature outruns an unburdened player. When the clamp bites it is logged once,
  naming the creature. This one is obeyed rather than argued with: a creature you cannot disengage from is not a
  difficulty setting.

Nothing else is clamped. Stacked multipliers are allowed to produce absurd creatures, because a server that sets
absurd numbers asked for them.

## Additive, never multiplicative

Every multiplier is read as a bonus above 1, the bonuses are summed, and the total is applied once. Two modifiers
pulling opposite ways therefore cancel in proportion rather than compounding. A mutation's cost is never
multiplied by the large-star enhancement; only its gain is.

---

# 2. Bosses, on their own table

Bosses scale on a table entirely separate from the creature lines, so a server can make the world brutal and
leave bosses alone, or the reverse.

**Bosses take stars. They never take mutations.** Stars say how hard; a mutation is a change to the *kind* of
fight and belongs to the creatures you meet by surprise.

**Boss stars do not follow the world.** They are drawn from one distribution for the whole world, not from the
biome the altar happens to sit in and not from any measure of what the players have done. A boss is a set-piece
you choose to walk into, so its difficulty is a server's decision rather than a consequence of geography.

| Setting | Default | Why |
| --- | --- | --- |
| `stars` | `true` | `false` leaves every boss exactly as the game ships it, with the rest of the mod still working |
| `star chances` | 90, 6, 3, 1 | Nine bosses in ten stay plain. A surprise five-star Bonemass is a wasted evening for a group that prepared for the ordinary one |
| `hp` | 1, 1.5, 2.25, 3.4, 5.1, 7.6 | Climbs harder per star than a creature's - a boss is a prepared fight, and the preparation should matter |
| `attack` | 1, 1.25, 1.55, 1.9, 2.3, 2.75 | As above |
| `growth` | 0, 0.05, 0.10, 0.15, 0.20, 0.20 | Capped at +20%, lower than a creature's +30%: a boss already fills its arena and one scaled much past that clips through the terrain it stands on |
| `swing speed` | 1 | Left alone. A faster boss breaks its own telegraphs, which is exactly what makes the fight readable |
| `speed` | 1 | As above |
| `drops` | 1, 1.5, 2, 2.5, 3, 3.5 | A harder boss pays better, from the first star - unlike a creature, every boss star was earned |

A boss travels the same scaling path a creature does; the boss table is simply presented in place of the biome's.
Mutation chance is zero throughout for a boss, which keeps bosses free of mutations by construction rather than by
a check every caller has to remember.

**The Queen keeps her vanilla summon.** Every other boss has an offering altar and she does not - she is placed in
the Infested Citadel behind the Sealbreaker-locked gate, and the game has no Queen-specific code to adjust. Giving
her an altar would mean inventing a buildable piece or repurposing her gate or boss stone, all of which change
vanilla progression rather than scale it. Decided 2026-09-15; what would change it is a reason to fight her
outside the Citadel, at which point a buildable altar is the honest way to do it.

---

# 3. Respawning

Camps and dungeons repopulate over time rather than staying cleared forever, so a cleared draugr village becomes a
place you can return to rather than a dead landmark. Dungeon loot chests regenerate on their own, slower timer.

**All three are off by default.** This feature can quietly undo a player's sense of progress if it runs too fast,
so the defaults are conservative: a camp is worth clearing again after real days, not hours.

| Setting | Default | Meaning |
| --- | --- | --- |
| `camps` / `camp days` | off / 5 | Cleared camp spawners start spawning again |
| `dungeons` / `dungeon days` | off / 7 | Cleared dungeon spawners start spawning again |
| `dungeon loot` / `dungeon loot days` | off / 14 | Emptied dungeon chests refill from their own table |

Timers are in **world days** - one world day is 30 real minutes at default speed - and count in the same unit the
game's own spawner timer does.

## How the spawners work

The game's spawners already know how to respawn: each keeps the time of its last live creature and waits a set
number of world minutes. Camp and dungeon spawners simply ship with that timer set to zero, which is what makes a
cleared landmark stay dead. Writing a positive timer hands the work back to the game, so this mod never tracks a
creature or spawns one itself.

- **A spawner the game already gave a timer of its own is never touched.** That one respawns as its designer
  intended, and overriding it would be this mod deciding it knows better about a spawner it did not write.
- A spawner counts as a **dungeon** one when it sits inside a generated room, and a **camp** one otherwise.
- The timer is a local value the owner reads. Nothing is written and there is nothing to replicate.

## How loot regeneration works

A chest refills from its own drop table, exactly as the game fills a fresh one.

- **Only when emptied.** A chest a player is still working through is never topped up under them.
- **Only inside a generated room**, so a player's own chests and world-surface containers are never touched.
- **Only on the owner**, at the moment the chest loads - the same point the game itself fills a fresh chest, so a
  returning player finds it already stocked rather than watching items appear in front of them.
- The fill time lives in the chest's own ZDO, which the game replicates and saves, so the clock survives a restart
  and cannot be reset by another machine.
- A chest seen for the first time has its clock started rather than being refilled, so installing the mod does not
  immediately re-stock every crypt in the world.

## Known gap

A respawned camp should come back at the world's **current** tier rather than the tier it was cleared at - the
world moves on, and a camp cleared in the Meadows era should not still be a Meadows-era camp once everything else
has hardened. World tiers are a separate feature and do not exist yet, so respawn is tier-blind for now. This is a
deliberate omission, not an oversight, and it wires up when tiers land.

---

# 4. Configuration

Two top-level blocks in `creature_rules.yml`, both documented in the file the mod writes on first run, both
hot-reloaded, both bound to the server's copy while `lock to server` is true:

```yaml
respawning:
  camps: false
  camp days: 5
  dungeons: false
  dungeon days: 7
  dungeon loot: false
  dungeon loot days: 14

bosses:
  stars: true
  star chances: [90, 6, 3, 1]
  star power:
    growth:      [0,   0.05, 0.10, 0.15, 0.20, 0.20]
    hp:          [1,   1.5,  2.25, 3.4,  5.1,  7.6]
    attack:      [1,   1.25, 1.55, 1.9,  2.3,  2.75]
    swing speed: [1]
    speed:       [1]
    drops:       [1,   1.5,  2,    2.5,  3,    3.5]
```

The creature lines live per biome under `defaults` and `biomes`, as described in `mutations.md`.

Everything here can be turned off and the rest keeps working: bosses plain with creatures scaling, the world
repopulating with nothing scaled, or any other combination.

---

# 5. Testing it honestly

Not yet done. In the `LocalTesting` profile:

1. **Creature scaling** - `elite spawn Greydwarf 3` then `elite inspect`: health, damage, size, speeds and drops
   match the table at three stars.
2. **The run clamp** - spawn something fast at a high star count, confirm it can still be outrun and that the
   clamp logs once naming the creature.
3. **A starred boss** - raise `star chances` to force one; confirm its health bar, size and damage follow the boss
   table and not the creature one, and that it carries no mutation.
4. **`stars: false`** - confirm a boss is then identical to vanilla.
5. **Camp respawn** - set `camps: true` with a low `camp days`, clear a camp, wait, confirm it repopulates; confirm
   a spawner that already had a timer of its own is unaffected.
6. **Dungeon loot** - set `dungeon loot: true` with a low timer, empty a crypt chest, leave, return after the
   timer, confirm it refilled once and not twice, and that a half-emptied chest is untouched.
7. **Dedicated server** - every one of the above with a server and two clients, confirming both clients see the
   same boss stars and the same chest contents.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## Stars

- [ ] What a star is worth, as section 1 says
- [ ] Health never falls below 5% of base
- [ ] Movement never falls below 5%
- [ ] Run speed clamped so no creature outruns an unburdened player, logged once when the clamp bites
- [ ] Additive, never multiplicative
- [ ] Bosses on their own table

## Respawning

- [ ] Spawner behaviour as section 3 says
- [ ] A spawner the game already gave a timer of its own is never touched
- [ ] Loot regeneration only when emptied, only inside a generated room, only on the owner
- [ ] The known gap in section 3 closed or re-stated

## Verification

- [ ] Tested in a live multiplayer session, not only by build and by reading

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |

# Elite Creatures Reborn - specification: Boss aspects

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what an aspect is, the set of them, how an aspect is read at the altar before you summon, and how
loot pays for the harder ones. **Boss stars** - how a boss scales once it has them - are in `scaling.md`, on a
table entirely separate from ordinary creatures. Bosses take no mutations (`mutations.md`) and no attunements
(`attunements.md`), with one exception noted under Shifting below.

Numbers are defaults and all of them are configurable. Where a number is a judgement call it says so.

**Status: specified, not built.** None of this exists in the mod. Boss stars are built and shipping; aspects are
not. **The set is not final** - see the last section, which is the thing to settle before any code starts.

---

# 1. What an aspect is

Bosses do not take mutations. They take an **aspect**: a single modifier that changes the shape of the fight.

A boss carries stars and an aspect at the same time, and they are independent: **stars say how hard, the aspect
says what kind.** Stars scale a boss's numbers on the boss table; an aspect changes what the fight asks of you.

Several aspects change at phase boundaries rather than staying constant. Waning, Waxing, Legion and Shifting all
key off remaining health, so the fight has a **shape** rather than a difficulty number - which is the point of
having aspects at all rather than a boss star slider.

## The set

| Aspect | What changes |
| --- | --- |
| Waning | Opens with overwhelming speed and fast attacks, and weakens as the fight goes on. Survive the first minute and you win |
| Waxing | Opens manageable, then grows stronger, attacks faster and gains movement speed. Kill it fast or lose |
| Shrouded | Darkens the arena, so you fight by sound and silhouette |
| Legion | Calls reinforcements in waves at set health thresholds |
| Shifting | Changes its attunement at each phase, so one resistance set will not carry the whole fight |
| Bulwark | Takes greatly reduced damage from the front; you have to flank it |
| Echoing | Each attack repeats once, a moment later, in the same place |
| Draining | Regenerates over time, heals faster near other creatures and drains their health to do it - against a much lower maximum health |

Two further names, **Sundering** and **Unbound**, appear in the loot ranking below but have no behaviour written
for them anywhere. They are the open decision at the end of this file.

**Shifting is the one aspect that touches attunements.** It cycles a boss through elements at each phase. That is
a boss reading the attunement table rather than a boss being attuned, and it is the reason `attunements.md`
reserves the word `Shifting` and proposes `Wyrd` for its own shifting element instead - the two must not collide.

---

# 2. Reading the altar, and waiting for the fight you want

**The aspect is visible at the altar before you summon.** Standing at the offering bowl tells you which aspect is
currently on it and what that aspect does, in a line of plain text.

This is not a convenience. An aspect changes what gear you should bring, and an aspect revealed *after* you have
committed is simply unfair. Bulwark tells you to bring a group that can flank. Shifting tells you to bring more
than one damage type. Neither is any use discovered thirty seconds into the fight.

**And it shifts.** The aspect on an altar rerolls every **in-game hour**. A group that does not like what is on
the bowl can wait, go and do something else, and come back to a different fight. A group hunting one aspect
specifically can camp the altar until it comes up. The altar also shows **how long is left** before the next
shift, so waiting is an informed decision rather than standing around hoping.

The rules that make this work:

- **A reroll never repeats the current aspect**, so every shift is a visible change rather than a possible
  non-event.
- **Summoning locks the aspect in.** Once the offering is made, that is the fight, whatever the clock does next.
  An aspect that changed mid-fight would undo the whole point of showing it in advance.
- **Every altar rolls independently.** Each boss has its own current aspect, so you can check one while waiting on
  another.
- **Every player sees the same aspect at the same altar at the same moment**, and it survives a server restart
  without shuffling. Two people standing at one bowl must never read different things.
- **One outcome is "no aspect"** - an ordinary vanilla boss fight - so the plain version of each boss stays part
  of the rotation rather than being lost to the mod.

The reroll interval is configurable, **including to zero**, which fixes an altar's aspect permanently for servers
that want the choice taken away.

## The Queen keeps her vanilla summon

Decided 2026-09-15, and it supersedes an earlier line in `../SPEC.md` about giving every boss a matching altar.

Every other boss has an `OfferingBowl` altar and the Queen does not: she is placed in the Infested Citadel behind
the Sealbreaker-locked gate, and no Queen-specific class exists in the game's assembly. Giving her an altar "like
every other boss" would mean either inventing a buildable altar piece or repurposing her gate or boss stone, all
of which **change vanilla progression rather than scale it**. She is left exactly as the game ships her.

The consequence for this feature: the Queen can carry stars and an aspect like any other boss, but there is no
bowl to read it from in advance. Her aspect is therefore either fixed by configuration or announced on the
Sealbreaker gate - not yet decided, and only worth deciding once the rest of this is built.

What would change the whole decision: a reason to fight her outside the Citadel, at which point the buildable
altar is the honest way to do it, not a repurposed stone.

---

# 3. Loot scales with the aspect

**A harder aspect pays better.**

This is what stops a visible, rerolling aspect from becoming an easy-mode button. Without it, a group simply waits
for whichever aspect is gentlest on their build and the whole mechanic collapses into a free pass. With it,
waiting has two directions - wait for the aspect that suits you, or wait for the one that pays - and both are
legitimate play.

The plain "no aspect" outcome pays the vanilla amount, and **every aspect pays at or above it**. An aspect is
never a punishment for turning up.

The ranking, gentlest first, with the multiplier applied to everything the boss drops:

| Aspect | Pays |
| --- | --- |
| No aspect | 1.0x |
| Waning | 1.1x |
| Sundering | 1.2x |
| Bulwark | 1.3x |
| Shrouded | 1.4x |
| Echoing | 1.5x |
| Waxing | 1.6x |
| Legion | 1.8x |
| Shifting | 1.9x |
| Unbound | 2.0x |

**This ranking is a judgement made at a desk and is the single thing in `../SPEC.md` most likely to be wrong** -
nine fights ranked by someone who has fought none of them. It is one editable table, so reordering it after a few
real fights costs nothing, and it is first on the list of things to revisit.

---

# 4. Multiplayer

An aspect is server state, not creature state, for as long as it sits on an altar - which makes it the one trait
in the mod that does not simply follow the owner-rolls-once rule.

- **The current aspect on each altar lives in the altar's own ZDO**, with the time of its next reroll. The game
  replicates it, every client reads the same value, and it survives a restart without shuffling - which is what
  makes "two people at one bowl read the same thing" true rather than hoped for.
- **The reroll is performed by the altar's owner** on the in-game hour. Nobody else rolls, so there is no race
  between two clients standing at one bowl.
- **On summoning, the locked-in aspect is written to the spawned boss's own ZDO**, under this mod's key prefix,
  and from that point it behaves like every other trait: rolled once, stored, read by everyone.
- **The altar text is drawn locally** by each client from the altar's ZDO. No message is sent for it.
- **Phase changes are decided by the boss's owner** - it is the machine that knows its health - and the visible
  consequences are drawn by every client from the aspect they already have. Only the phase transition itself needs
  an RPC, scoped to players who could see the fight.
- **Loot is multiplied on the owner**, where the game builds the drop list, from the aspect in the boss's ZDO.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In the main settings file's boss aspects section, and the creature rule file for the tables:

- An **off switch** for the whole feature. With aspects off, boss stars and everything else keep working.
- **Reroll interval**, in in-game hours, including zero to fix every altar permanently.
- **The chance of "no aspect"**, so a server can make plain fights common or eliminate them.
- **Which aspects are in the rotation**, per boss, so a server can retire one it dislikes.
- **The loot multiplier per aspect**, as the table above.
- **Each aspect's own numbers** - Bulwark's frontal reduction, Echoing's delay, Legion's thresholds and wave size,
  Draining's regeneration and its health penalty.

`elite inspect` reports a boss's resolved aspect and stars alongside everything it already reports.

---

# 6. Open decisions

**The set is inconsistent in `../SPEC.md` and must be settled first.** Section 4 there is headed "The nine",
describes eight, and ranks nine:

- **Described but unranked:** Draining. It has behaviour written and no loot multiplier.
- **Ranked but undescribed:** Sundering and Unbound. They have loot multipliers at 1.2x and 2.0x - Unbound being
  the top of the entire table - and no behaviour anywhere in the specification.

So there are ten candidate names for nine slots, and the hardest-paying aspect in the mod is one nobody has
written down. Three things need deciding, in this order:

1. **Which nine.** Either Draining joins the ranking and one of Sundering or Unbound is dropped, or Draining is
   the name that goes and both others are written.
2. **What Sundering and Unbound do**, if they stay. Unbound in particular pays 2.0x, so it is the fight the whole
   ranking is anchored against.
3. **Draining's loot multiplier**, if it stays. Its description trades much lower maximum health for
   regeneration, which could land anywhere in the ranking depending on how the two balance.

Until this is settled the feature cannot be built, because the aspect list is the one thing every other part of it
indexes - the rotation, the altar text, the loot table and the ZDO encoding all need the same nine names.

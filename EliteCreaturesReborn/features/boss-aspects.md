# Elite Creatures Reborn - specification: Boss aspects

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

**Confirmed for `thieving.md`: a boss cannot carry a Thieving pouch, and its stolen-goods drop path is
unreachable from a boss death.** Bosses never take mutations at all (see below), so there is nothing here for
that mutation's death handling to reach.

This file covers what an aspect is, the set of them, how an aspect is read at the altar before you summon, and how
loot pays for the harder ones. **Boss stars** - how a boss scales once it has them - are in `scaling.md`, on a
table entirely separate from ordinary creatures. Bosses take no mutations (`mutations.md`) and no attunements
(`attunements.md`).

Numbers are defaults and all of them are configurable. Where a number is a judgement call it says so.

**Status: built, not tested in game.** The set was settled on 2026-09-26 and the whole feature built the same day.
Nothing is ticked below until it has been seen working on a dedicated server.

---

# 1. What an aspect is

Bosses do not take mutations. They take an **aspect**: a single modifier that changes the shape of the fight.

A boss carries stars and an aspect at the same time, and they are independent: **stars say how hard, the aspect
says what kind.** Stars scale a boss's numbers on the boss table; an aspect changes what the fight asks of you.

An aspect's percentages are taken from the boss **as its stars left it**: "25% less health" means three quarters of
the starred boss's health, not a bonus summed with the star line. Stars and mutations combine additively
(`scaling.md`); an aspect is a separate layer on top, so its number means exactly what its description says.

**The aspect is in the boss's name** - "Enraged Eikthyr" - on its health bar and wherever the game shows the name,
the same way a mutation names a creature.

## The set

Settled on 2026-09-26, replacing the earlier draft set (Waning, Waxing, Shrouded, Legion, Shifting, Bulwark,
Echoing, Draining, Sundering, Unbound), none of which was ever built.

| Aspect | What changes |
| --- | --- |
| Reflective | Direct damage you deal to the boss is dealt to you as well |
| Shielded | Takes 30% less damage from arrows and bolts |
| Mending | Regenerates its health far faster than the game's own trickle |
| Summoner | Calls strong creatures each time it loses 33% of its maximum health |
| Elementalist | Deals 20% more elemental damage |
| Enraged | Deals 20% more physical damage |
| Twin | Comes as two bosses sharing one health pool, each with 25% less health and damage |
| Phantom | Comes with four weak copies of itself: half its damage, 100 health, no drops, no body |

## How each aspect works

**Reflective.** Every hit that lands on the boss sends a share of the damage it actually dealt back to whoever
dealt it - **15%** by default (a judgement call; the request gave no number). "Direct" means a hit: a swing, an
arrow, a bolt, a spell, a thrown weapon. The burn and poison ticks that follow a hit are not direct and are never
returned. The returned damage is **true damage**: armour and resistances do not reduce it, so no gear makes the
aspect a non-event - it asks you to watch your own health as you deal damage. It cannot be blocked, parried or
dodged. It is returned to any attacker, a player's tame included. A flash at the attacker shows it happening.

**Shielded.** Hits from bows and crossbows deal **30%** less. Melee, magic, thrown weapons and every damage-over-time
tick are untouched. Shielded tells an archer to bring a melee weapon, which is exactly what the altar text is for.

**Mending.** Heals **0.3% of its maximum health every second**, always, in combat or out (a judgement call: over a
three-minute fight that is roughly half its health again). The game's own slow regeneration still runs underneath.
Mending is a damage check: the group that cannot out-damage it cannot win.

**Summoner.** Each time the boss loses **33%** of its maximum health it calls a wave of **2 creatures at 2 stars**
(count and stars are judgement calls; the request said "strong creatures"). With the default 33 the waves come at
67%, 34% and 1% health remaining. A single hit that crosses two thresholds calls two waves. Healing back above a
threshold never re-arms it. The creatures come from a per-boss list, all from the boss's own biome:

| Boss | Calls |
| --- | --- |
| Eikthyr | Boar, Neck |
| The Elder (`gd_king`) | Greydwarf Brute, Greydwarf Shaman |
| Bonemass | Draugr Elite, Oozer |
| Moder (`Dragon`) | Drake |
| Yagluth (`GoblinKing`) | Fuling Berserker, Fuling Shaman |
| The Queen (`SeekerQueen`) | Seeker Soldier, Seeker |
| Fader | Charred Warrior, Charred Marksman |

A boss with no list - a modded boss - never rolls Summoner rather than rolling it and doing nothing. Summoned
creatures appear alerted, a few metres from the boss, on the ground beneath it. They are real creatures: they
drop their own loot and stay in the world if the boss dies first. A name the game does not know is skipped and
logged once.

**Elementalist.** The fire, frost, lightning, poison and spirit parts of every hit the boss deals are **20%**
larger. Poison and fire are raised before the game turns them into their ticking effects, so the ticks are larger
too.

**Enraged.** The blunt, slash and pierce parts of every hit the boss deals are **20%** larger.

**Twin.** The moment the boss appears, a second copy of it appears beside it with the same stars. Both have **25%
less** health and **25% less** damage, and **their health is one pool**: damage to either comes off both, whatever
it was - a hit, a burn tick, lava. When the pool runs out both die together, and **both drop full loot**, each
its own trophy included (decided 2026-09-26). Each shows its own boss health bar; the two bars always read the
same. The twin never brings a twin of its own.

**Phantom.** The moment the boss appears, **4** copies of it appear in a ring around it: same prefab, same stars,
same size, same name. Each copy deals **50% less** damage and has **100 health**, however many stars the boss
has. A copy drops nothing and leaves no body - it vanishes where it falls, with a puff of smoke. Its death never
counts as the boss's: no boss-defeated key, no progression, no trophy. **When the boss dies its remaining copies
vanish with it** (a judgement call: they are the boss's phantoms, not creatures of their own). The copies never
bring copies of their own.

---

# 2. Reading the altar, and waiting for the fight you want

**The aspect is visible at the altar before you summon.** Standing at the offering bowl tells you which aspect is
currently on it and what that aspect does, in a line of plain text with the live numbers.

This is not a convenience. An aspect changes what gear you should bring, and an aspect revealed *after* you have
committed is simply unfair. Shielded tells an archer to bring a sword. Reflective tells you to bring healing.
Neither is any use discovered thirty seconds into the fight.

**And it shifts.** The aspect on an altar rerolls every **in-game hour** - one twenty-fourth of the game's day, 75
real seconds on the default 30-minute day. Confirmed on 2026-09-26 knowing it is short: a group that does not like
what is on the bowl can wait a minute or two, and a group hunting one aspect can camp the altar until it comes
up. The loot table in section 3 is what keeps that from being a free pass. The altar also shows **how long is
left** before the next shift, so waiting is an informed decision rather than standing around hoping.

The rules that make this work:

- **A reroll never repeats the current aspect**, so every shift is a visible change rather than a possible
  non-event. (When the rotation leaves nothing else to pick, the current one stays.)
- **Summoning locks the aspect in.** The aspect on the bowl at the moment of the offering is the fight, even if the
  altar shifts during the few seconds before the boss appears.
- **Every altar rolls independently.** Each boss has its own current aspect, so you can check one while waiting on
  another.
- **Every player sees the same aspect at the same altar at the same moment**, and it survives a server restart
  without shuffling. Two people standing at one bowl must never read different things.
- **One outcome is "no aspect"** - an ordinary vanilla boss fight - so the plain version of each boss stays part
  of the rotation rather than being lost to the mod.

The reroll interval is configurable, **including to zero**, which fixes an altar's aspect permanently for servers
that want the choice taken away.

## Bosses without an altar

Decided 2026-09-26. **A boss that appears without an offering rolls its aspect the moment it first exists**, from
the same rotation an altar uses, and keeps it. That covers the Queen, a boss spawned from the console, and a boss
another mod spawns. The aspect is in the boss's name, so it is read at the door rather than at a bowl.

## The Queen keeps her vanilla summon

Decided 2026-09-15, and it supersedes an earlier line in `../SPEC.md` about giving every boss a matching altar.

Every other boss has an `OfferingBowl` altar and the Queen does not: she is placed in the Infested Citadel behind
the Sealbreaker-locked gate, and no Queen-specific class exists in the game's assembly. Giving her an altar "like
every other boss" would mean either inventing a buildable altar piece or repurposing her gate or boss stone, all
of which **change vanilla progression rather than scale it**. She is left exactly as the game ships her, and takes
her aspect under "Bosses without an altar" above.

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

The ranking, gentlest first, with the multiplier applied to everything the boss drops (trophies follow the loot
rules' trophy switch, as every other multiplier does):

| Aspect | Pays |
| --- | --- |
| No aspect | 1.0x |
| Twin | 1.0x per boss - both drop, so the fight pays double |
| Shielded | 1.1x |
| Enraged | 1.2x |
| Elementalist | 1.2x |
| Mending | 1.3x |
| Phantom | 1.3x |
| Reflective | 1.4x |
| Summoner | 1.5x |

**This ranking is a judgement made at a desk** - eight fights ranked by someone who has fought none of them. It is
one editable table, so reordering it after a few real fights costs nothing, and it is first on the list of things
to revisit.

The multiplier stacks on the boss star `drops` line and the loot rules' boss multiplier. **It applies even with the
loot rules in Vanilla mode** (`configuration.md`: "an aspect that scales loot must work with loot rules off") -
there it is the only thing that touches a boss's drops. Phantom copies drop nothing at all, whatever the table says.

---

# 4. Multiplayer

An aspect is server state, not creature state, for as long as it sits on an altar - which makes it the one trait
in the mod that does not simply follow the owner-rolls-once rule.

- **The current aspect on each altar lives in the altar's own ZDO**, with the world time of its next reroll. The game
  replicates it, every client reads the same value, and it survives a restart without shuffling - which is what
  makes "two people at one bowl read the same thing" true rather than hoped for.
- **The reroll is performed by the altar's owner** when the world clock passes the stored time. Nobody else rolls,
  so there is no race between two clients standing at one bowl.
- **On summoning, the locked-in aspect is written to the spawned boss's own ZDO** on the machine that summons it,
  under this mod's key prefix, and from that point it behaves like every other trait: rolled once, stored, read by
  everyone.
- **The altar text is drawn locally** by each client from the altar's ZDO. No message is sent for it.
- **The boss's owner decides everything that follows**: Summoner's thresholds (the waves already called are counted
  in the boss's ZDO, so a hand-over neither repeats nor skips one), Mending's healing, the Twin and Phantom spawns
  at the moment it is first rolled, and Reflective's returned hit.
- **Twin's shared pool** is kept by each twin's owner: health it loses is sent to its partner's owner through the
  partner's own network view, which takes the same amount off. A twin's death tells its partner to fall. The two
  may have different owners; each only ever writes its own health.
- **Phantom copies are marked in their own ZDO**, so every machine strips their drops, body and boss key the moment
  it meets one. The boss's owner sends the vanish to each copy's owner when the boss dies.
- **Damage changes** (Enraged, Elementalist, Shielded, Twin's and Phantom's reduced damage) are applied where every
  hit is resolved - on the victim's owner - from the aspect in the attacker's or victim's ZDO.
- **Loot is multiplied on the owner**, where the game builds the drop list, from the aspect in the boss's ZDO.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

All in `creature_rules.yml`, in an `aspects:` block inside `bosses:`, server-synced and hot-reloaded like the rest of
the file. A boss that already exists keeps the aspect it rolled; the numbers are read live.

```yaml
bosses:
  stars: true
  ...
  aspects:
    enabled: true            # off: no aspects anywhere; boss stars keep working
    shift hours: 1           # in-game hours between altar shifts; 0 fixes each altar for good
    chances:                 # relative weights; `none` is a plain fight
      none: 20
      Reflective: 10
      ...
    loot:                    # drop multiplier per aspect
      none: 1
      Summoner: 1.5
      ...
    power:
      Reflective:   { reflect: 15 }
      Shielded:     { arrow reduction: 30 }
      Mending:      { regen: 0.3 }
      Summoner:     { every: 33, count: 2, stars: 2 }
      Elementalist: { elemental bonus: 20 }
      Enraged:      { physical bonus: 20 }
      Twin:         { less health: 25, less damage: 25 }
      Phantom:      { copies: 4, health: 100, less damage: 50 }
    per boss:                # matched by prefab name
      - match: Eikthyr
        summons: [Boar, Neck]
      - match: Bonemass
        aspects: [none, Reflective, Twin]   # optional: this boss's rotation
        summons: [Draugr_Elite, BlobElite]
```

- `enabled` is the feature's off switch. `stars` and `aspects` are independent: stars off with aspects on gives
  unstarred bosses with aspects, and both off leaves every boss exactly as the game ships it.
- `chances` are weights, not percentages; the defaults happen to sum to 100 (20 plain, 10 each). An aspect missing
  from the list never rolls.
- `per boss` narrows one boss's rotation (`none` stays in unless its weight is 0) and sets what Summoner calls.
- `elite inspect` reports a boss's aspect, what it does with the live numbers, its loot multiplier, and for a Twin or
  a Phantom copy whom it is tied to. `elite spawn <boss> <stars> <aspect>` makes exactly that fight.

---

# 6. Decisions

Settled on 2026-09-26 with the user:

1. **The set** is the eight in section 1. The earlier ten candidate names are retired, which also frees the word
   `Shifting` that `attunements.md` had stepped around.
2. **Twin: both bosses drop full loot**, trophy included. Twin's multiplier is therefore 1.0 per boss.
3. **Altars shift every in-game hour** (75 real seconds by default), as first specified.
4. **Bosses without an altar roll their aspect when they first appear.**

Judgement calls made while building, each a default in the rule file:

- Reflective returns 15% as true damage; Mending heals 0.3% per second; Summoner calls 2 two-star creatures; the
  per-boss summon lists; the loot ranking. None of these numbers came with the request.
- Aspect percentages multiply the starred boss rather than adding to the star line (section 1).
- "Arrows" means bows and crossbows.
- Phantom copies vanish when the boss dies; summoned creatures do not.

Still open:

- **Translation.** `creature-naming.md` asks for every on-screen string to come from a replaceable file. The mod has
  no translation file yet for any of its text; the altar lines and aspect names are English in code alongside the
  mutation names, and move when that feature is built.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## The aspects

- [ ] Reflective: returns true damage to the attacker, never burn or poison ticks
- [ ] Shielded: bows and crossbows only
- [ ] Mending: regenerates in and out of combat
- [ ] Summoner: waves at each threshold, counted in the ZDO, list per boss
- [ ] Elementalist: fire, frost, lightning, poison, spirit
- [ ] Enraged: blunt, slash, pierce
- [ ] Twin: second boss, shared pool, both die together, both drop
- [ ] Phantom: four copies, 100 health, half damage, no drops, no body, vanish with the boss
- [ ] The aspect is in the boss's name
- [ ] Boss stars show on the boss health bar

## The altar

- [ ] Hover text shows the aspect, what it does, and the time to the next shift
- [ ] One outcome is "no aspect" - a plain vanilla boss fight
- [ ] A reroll never repeats the current aspect
- [ ] Summoning locks the aspect in
- [ ] Every altar rolls independently
- [ ] A boss without an altar (the Queen, a console spawn) rolls when it appears
- [ ] Loot scales with the aspect, Vanilla loot mode included

## Multiplayer

- [ ] Current aspect and next-shift time live in the altar's own ZDO
- [ ] The reroll is performed by the altar's owner
- [ ] On summoning, the locked-in aspect is written to the boss's own ZDO
- [ ] Altar text drawn locally; no message sent for it
- [ ] Summoner waves, Mending, Twin and Phantom spawns decided by the boss's owner
- [ ] Twin pool holds with the two twins owned by different machines
- [ ] Phantom copies stripped on every machine; vanish reaches each copy's owner
- [ ] Loot multiplied on the owner
- [ ] Every player sees the same aspect at the same altar, surviving a restart

## Configuration

- [ ] `enabled`, independent of `stars`
- [ ] `shift hours`, including zero to fix every altar
- [ ] `chances`, `none` included
- [ ] `per boss` rotation and summon lists
- [ ] `loot` multiplier per aspect, and each aspect's `power` numbers, hot-reloaded

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-26 | Set replaced with the user's eight; open decisions settled; whole feature built, untested; boss stars drawn on the boss health bar. | uncommitted |

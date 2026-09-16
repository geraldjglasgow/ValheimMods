# Elite Creatures Reborn - specification: Mutations

One feature of the mod, specified on its own. The other feature files sit beside it; `SPEC.md` is the whole-mod
behaviour document that all of them are drawn from.

Stars and per-star scaling are **not** specified here - see `SPEC-scaling.md`. This file assumes a creature has a
star count and specifies only what a mutation is and what each one does.

Numbers are defaults and all of them are configurable through `creature_rules.yml`. Where a number is a judgement
call it says so.

**Status:** built and shipping in 3.0.0. Verified by build and by reading; not yet tested in a live multiplayer
session against the checklist in "Testing it honestly" below.

**Bosses never take mutations.** They take stars on their own separate table - see `SPEC-scaling.md`.

---

# Mutations

## What a mutation is

A mutation is a **named, visible character** a creature can carry. Where stars make a creature
bigger, a mutation makes it *different* - a fight you approach differently once you have learned to
recognise it.

**Not every mutation is a drawback-balanced trade.** Four of the nine below cost the creature
something; five are pure gains with no downside. That is deliberate. The balancing lever for this
feature is **how often mutations appear**, not making each one internally fair. A player who meets
a Miasmic Cloaked troll is supposed to be in trouble.

Mutations are never elemental. Elemental behaviour belongs to a later slice and must not appear
here.

## The nine

| Mutation | Gains | Costs |
| --- | --- | --- |
| Mad | Moves and attacks far faster - +60% movement, +50% attack speed | Half health |
| Bloated | Double health. On death it pauses a second, then explodes | None |
| Cloaked | Invisible at more than 6 metres, nameplate included | None |
| Splintering | Splits into two copies when killed, each a star weaker or more | Deals 40% less damage |
| Leeching | Regenerates 2% of max health per second, and heals 30% of damage it deals | None |
| Warding | Reflects 30% of damage taken back at the attacker, and knocks them back on any melee hit | None |
| Plated | Heavily armoured at full health | Sheds that armour as it is hurt, and its damage rises as it does |
| Miasmic | Leaves a trail of poison clouds as it moves; each cloud lingers 6 seconds then fades | None |
| Devouring | Eats other creatures and keeps what it takes. See below | Grows slower the more it has eaten |

Judgement calls in that table, all tunable: the Mad percentages; Bloated's one-second fuse and its explosion doing 40 damage in a 4 metre radius, scaled by its star count; Splintering's 40% damage reduction; Leeching's two
rates; Warding's 30% reflection; Plated running from +100% armour at full health to none at zero
while its damage climbs from nothing to +60%; Miasmic's clouds doing poison damage on a par with a
Blob's and appearing about one per second of movement.

## Devouring, in full

Devouring is the most involved of the nine and the one most worth getting right. It is meant to be a *situation*
rather than an encounter: you come over a rise and find something in the middle of eating a camp, and you decide
what to do about it.

### It eats in one bite

When a Devouring creature lands an attack on **another non-boss creature, that creature dies instantly.** No
chewing, no health bar, no fight - it is simply gone, and the devourer takes what it was.

On that kill it gains, permanently:

- `absorb health` percent of the victim's maximum health, added to its own.
- `absorb damage` percent of the victim's damage, added to its own.

Both default to 100. These accumulate with every meal, so one left alone in a busy area becomes genuinely
enormous - and visibly so, because it grows with what it has eaten.

An instant kill needs an unmistakable tell: a sound and an effect at the moment it happens, so a player watching
from a distance sees a creature simply cease rather than wondering what became of it.

### Then it has to wait

After a meal it **cannot devour again for `devour cooldown` seconds** - 10 by default. It carries on as an
ordinary creature during that time: it can move, it can be fought, it can be killed. It just cannot eat.

The cooldown is what makes the mutation a decision rather than a disaster. A camp is not consumed in a second;
it goes one creature every ten, and you can watch it happen and choose whether to intervene.

### It ignores you, until it does not

A Devouring creature pays the player no attention at all while it is feeding. Two things change that:

- **You attack it.** It turns on you immediately and fights you like any other creature. If you break off and it
  loses you, it goes back to eating.
- **It grows big enough.** Once its damage per hit reaches `player threshold` - **one third of your maximum
  health**, by default - it stops caring about creatures and hunts players from then on. That change is
  permanent.

So there are two ways to deal with one, and they cost different things. Interrupt it early, while it is small
and you are choosing the fight. Or leave it, and meet it later on its terms, three bites stronger.

### It never knocks anything back

Not creatures, not players. A Devouring creature that has grown big enough to hunt you does not fling you away -
it closes and stays closed. Knockback would turn every exchange into a chase and rob the mutation of its weight.

### Devouring in multiplayer

Every part of this has to work on a dedicated server, and several pieces will quietly not unless they are built
for it:

- **The instant kill can cross owners.** A devourer owned by one machine will attack prey owned by another. The
  kill must be routed to the prey's owner rather than skipped or applied locally, and the absorb banked on the
  devourer's owner. Skipping it means Devouring silently stops working on a busy server, which is exactly where
  it matters.
- **The cooldown lives on the ZDO, not in local memory.** If it is a field on a component, a creature handed
  from the server to an approaching player forgets it has just eaten and can eat again immediately.
- **So does the "hunts players now" flag.** It is described as permanent, so it must survive a handover, a
  reload and a player logging out. A local bool resets and the creature goes back to ignoring everyone.
- **So does everything it has absorbed.** Its accumulated health and damage are already ZDO state; keep them
  there.
- **The kill tell is drawn by every nearby client**, not only the owner. A creature vanishing in silence on
  someone else's screen is the invisible-effect failure this spec keeps naming.

**Which player's health does `player threshold` measure against?** On a server they differ. The rule: compare
against the **nearest player** at the moment of the check, and the first time its damage per hit reaches that
player's threshold, set the flag and hunt *every* player from then on. Do not evaluate it per player - a creature
that is hostile to one player and indifferent to their companion standing beside them is incomprehensible to both
of them.

### It grows slower as it grows

Movement speed falls as its accumulated health rises (`slow per 100 health`). A well-fed one is a slow-moving
disaster you can see coming and outrun, rather than something that is simply unfair. This is its only cost, and
it is what makes leaving one alone a survivable mistake.

## Bloated, in full

**The blast goes off at the corpse, not at the place of death.** A creature that dies turns into a ragdoll, and
in the second before it detonates that ragdoll can slide, tumble or roll down a hill. Exploding at the spot
where it died would leave the bang somewhere the corpse visibly is not, which reads as a bug even to a player
who could not say why.

So the fuse follows the body:

- The warning tell **rides the corpse**, wherever it goes, for the whole fuse. The thing that is about to
  explode is the thing that looks like it is about to explode.
- The blast happens at **wherever the corpse has come to rest** when the fuse ends, and the damage radius is
  measured from there.
- If there is no ragdoll - a creature that simply vanishes, or one whose corpse is already gone - it falls back
  to the place of death. Better a blast in the right general area than none.

### Which corpse, in multiplayer

Ragdoll physics is not synchronised: every machine simulates its own copy, so the corpse settles in a slightly
different spot on each, and on a slope that can be metres apart.

- **The warning rides each client's own local corpse.** It is cosmetic, it looks right on every screen, and the
  small divergence costs nothing.
- **The blast position is the owner's corpse**, sent when the fuse ends rather than when the creature died. Every
  client draws the blast there, and the owner deals the damage there, so what everyone sees and what actually
  hurts are the same place.

This means the explosion is announced at detonation, not at death - the warning is what goes out at death.

## Miasmic, in full

**Miasmic uses the game's own poison, not damage of its own.** Everything below is the vanilla Poison status
effect - the same debuff, the same icon, the same tick a player already knows from a blob or a leech. Nothing
here invents a new kind of harm, and a player who has been poisoned before knows exactly what has happened to
them.

### It looks poisoned, permanently

A Miasmic creature **carries the poison cloud visual on its body at all times** - the same wisp a poisoned
character trails. It is the creature's whole tell: you can see one coming and know what it is before the
nameplate resolves.

It is **not** poisoned and takes no damage from it. The visual is worn, not suffered.

### Two ways it poisons a player

- **It hits you.** A Miasmic creature's attacks apply Poison on top of their normal damage.
- **You walk through its trail.** Standing in a cloud it has left applies the same Poison.

Both use the identical status effect, so the two are indistinguishable once applied - you are poisoned, and how
you got there does not change the cure.

### The trail falls behind it

Clouds drop at the creature's **back**, offset behind its facing by roughly its own body width, never at its
centre. Fighting one head-on should not poison you through the trail: you are poisoned because it hit you, or
because you chased it, circled it, or backed onto ground it already crossed.

A creature standing still lays no meaningful new trail - the clouds it has already dropped are enough, and a
stationary one must not slowly wall itself in.

### It never harms anything but players

Not other creatures, not tamed animals, not itself. Three real problems this avoids:

- A Miasmic pack would poison itself to death - funny once, broken thereafter.
- A player's tamed wolves would die to a fight the player never chose, with no way to protect them.
- A Devouring creature feeding near a Miasmic one would have its meals poisoned out from under it, entangling
  two mutations that should stay independent.

If it is poisoning something, it is poisoning you. That is what makes the trail readable.

### Numbers

A cloud lasts `cloud life` seconds - 6 by default - and is visible for exactly that long, fading as it expires.
`cloud damage` becomes the **strength of the poison applied** rather than damage dealt directly, since the status
effect now does the harming.

## Splintering, in full

Killing one produces two copies, and **each copy rolls its own star count** independently. It is
not a guaranteed step of one - most copies come back several stars weaker, so a chain normally dies
within a generation or two. Occasionally a copy drops by only one star, and very occasionally that
happens again, and then you have a story.

| Parent | 1 less | 2 less | 3 less | 4 less | Straight to 0 |
| --- | --- | --- | --- | --- | --- |
| 5 stars | 15% | 35% | 30% | 15% | 5% |
| 4 stars | 15% | 35% | 30% | - | 20% |
| 3 stars | 15% | 35% | - | - | 50% |
| 2 stars | 15% | - | - | - | 85% |
| 1 star | - | - | - | - | 100% |

A copy landing on 1 or more stars still carries Splintering and splits in turn. A copy landing on 0
does not carry it and never splits, which is what terminates every branch. A 0-star Splintering
creature breaks straight into two plain ones.

Copies keep the parent's other mutations. They are the same creature, weaker, so their health and
damage come from ordinary star scaling rather than any special rule.

In practice a 5-star Splintering kill produces about **nine** extra creatures over two or three
short generations. The full unbroken chain still reaches 62, but a single lineage running 5 to 4 to
3 to 2 to 1 is roughly a **1 in 2,000** event. That is the intent: the big cascade should happen to
a server once, be talked about, and not be the norm.

Two safety knobs exist and are **off by default**: a cap on how many generations deep a cascade may
go, and a cap on how many live descendants one cascade may have at once. Turning either on
truncates a cascade rather than preventing the mutation.

## Console commands

**Nothing in this mod can be tested without these.** Mutations are rare by design - Splintering is about one
creature in forty in the Meadows - so verifying that any of it works by wandering around waiting to meet one is
not testing, it is luck. Every rule in this document needs a way to be produced on demand.

Three commands, all admin-gated on a server, all no-ops for a player without rights:

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns that creature with exactly those stars and mutations, ignoring every chance roll. Named mutations are applied whether or not the rules would ever have given them. |
| `elite inspect` | Prints the resolved mark of the creature under the crosshair: its stars, its mutations, the biome it was rolled in, and the values those produced after star power and any large-star enhancement. |
| `elite purge` | Removes loaded creatures that this mod has marked, without drops. For clearing the mess after testing. |
| `elite effects <text>` | Lists the names of loaded effect prefabs containing that text, and plays the one nearest the player so it can be judged by eye. |

`elite spawn` is the important one and it must bypass **everything**: the star distribution, the mutation
chances, `max mutations`, and the mutations-shown-on-glyphs limit. `elite spawn Greydwarf 9 Splintering Mad
Miasmic` must produce exactly that creature, so a rule can be checked in ten seconds rather than an hour.

`elite inspect` must show the *resolved* numbers, not the configured ones - what this creature actually ended up
with after the additive model and any enhancement. The gap between what a rule file says and what a creature
became is where bugs live.

`elite effects` exists because **prefab names are not discoverable from the game's code** - they are Unity asset
references, so no amount of reading the decompile produces a list. The only way to choose a good explosion or a
good cloud is to see what the loaded game actually has and look at it. Without this, every effect name in the
rule file is a guess that fails silently into a keyword fallback.

When an effect name does not resolve, say so in the log once, name the setting it came from, and say which
fallback was used instead. A silently substituted effect is how you end up shipping the wrong explosion.

Prefab names come from the game, so `elite spawn` should suggest near matches rather than failing silently on a
typo.

## Visuals

**No mutation may create an invisible hazard.** A player who takes damage must be able to see what did it, and
to see it *before* walking into it. The whole design stance of this mod is that difficulty is legible; a damage
volume with no visual breaks that more badly than any number could.

**Use the game's own effects. Do not require art assets.** Valheim already ships everything needed here. Look up
an existing prefab through `ZNetScene` by name, clone it, and retune the clone from config. The game's `Aoe`
component is purpose-built for a lingering ground hazard - it carries damage, a status effect, ground-conforming
placement, a hit interval, a lifetime, knockback and multi-spawn - so a cloud should *be* one of those rather
than a hand-rolled volume with a bare `GameObject`.

**Every prefab name is a config setting.** A server that wants a different look, or a modded biome that needs a
different effect, changes a string rather than waiting for a new build. If a named prefab cannot be found, log
it once, fall back to the nearest vanilla effect, and never crash or silently spawn nothing.

### Miasmic clouds must be visible for their whole life

This is the one most likely to be got wrong. Most vanilla effect prefabs are **one-shot**: they play for a
second or two and destroy themselves. Cloned naively for a six-second cloud, the particles vanish after a
second while the poison keeps ticking - which is *worse than no visual at all*, because the player learns "the
cloud is gone, I am safe" and that lesson is false.

So:

- **The visual must last exactly as long as the hazard**, driven by the same `cloud life` value from the rule
  file. A server setting `cloud life: 12` gets twelve seconds of both, with no code change.
- Achieve that however the chosen prefab allows - looping its particle systems, extending its duration, or
  re-triggering it on a timer shorter than its own runtime. Re-triggering must not stack into an
  ever-brightening pile; one cloud should look like one cloud however it is kept alive.
- **It must fade as it expires**, in the last second or so, so a player can see a cloud is about to become safe
  and time a move through it. A hazard that disappears instantly is a coin flip; one that visibly thins is a
  decision.
- The visible radius must match the damage radius. A cloud that looks smaller than it hurts is a bug report;
  one that looks larger is a player standing further away than they need to, forever.

### Bloated needs a tell during its delay

The spec calls the second between death and detonation "the window a player has to get clear". A window
nobody can see is not a window. The corpse must visibly and audibly announce what is about to happen for the
whole delay - swelling, glowing, hissing, whatever the chosen effect supports - and the blast itself should
reuse an existing explosion effect at the configured radius.

A player who has seen one Bloated creature die should recognise the second one instantly. That is the entire
purpose of the delay.

### The others

- **Cloaked phases, it does not snap.** Crossing `reveal distance` fades the creature in over `fade time`
  (0.5 seconds by default) and fades it back out over the same time when it leaves. A hard cut looks like a
  rendering fault; a fade looks deliberate, and it costs nothing in surprise because it happens at the boundary
  either way. The nameplate fades in step with the body.

  **This needs a hysteresis band or it will strobe.** A player standing exactly at the reveal distance would
  otherwise flicker in and out every frame as the distance jitters. Fade *in* at `reveal distance` and fade
  *out* only at `reveal distance + fade margin` (1 metre by default), so the two thresholds never coincide.

  **Do not assume alpha fading works.** Valheim creature materials use opaque shaders, and lowering alpha on an
  opaque material does nothing at all. Use whatever the creature shader actually exposes; if the only way to get
  a true fade is swapping render mode, weigh that against how it looks - a render-mode swap can break z-sorting
  and lighting and end up worse than no fade. If no clean fade is available, **snap, and write that down in
  DECISIONS.md**. A correct snap beats a fade that renders wrong.

  Beyond the fade, Cloaked gets no shimmer, outline or marker - being unable to see it *is* the mutation.
- **Devouring** should make a held creature obviously held, and should show that it is feeding. A player needs
  to be able to read "that thing is getting stronger right now" from a distance, because deciding whether to
  interrupt it is the whole encounter.
- **Warding** should show something at the moment it reflects, at the attacker, so the damage that just came
  back is attributable rather than mysterious.
- **Mad**, **Plated**, **Leeching** and **Splintering** need no effect of their own. Their star colours and
  names carry them, and adding more would clutter a creature that may be wearing four mutations at once.

### Rules for all of them

- Effects are **client-side and cosmetic**. They never decide damage, and a client with them turned off must
  take exactly the same damage as one with them on.
- Effects must clean themselves up. A cloud that expires leaves nothing behind, and unloading an area destroys
  its effects with it.
- Brightness and density follow the existing per-player display settings, down to off, for players who need a
  quieter screen or are running on weaker hardware. Turning visuals off does **not** turn the hazard off - it
  makes the hazard unfair, which is the player's choice to make and must be worded that way in the setting's
  description.

## How many a creature gets, and which

**Every mutation rolls independently**, with its own chance, and that chance depends on the creature's star
count and the biome it spawned in. There is no slot table and no fixed ceiling: a creature that fails every roll
is plain, and one that passes several carries several.

Rolling each mutation separately rather than rolling "how many, then pick one" is what lets a server say
"Miasmic is a Swamp thing, Plated is a Mountain thing" - which a count-then-pick model cannot express at all.

`max mutations` caps how many one creature may carry. **Default `1`**: a creature is Mad, or it is Cloaked,
but not both. Where more roll than the cap allows, the survivors are picked at random from those that rolled,
so a mutation that is common in a biome is also the one most likely to win the cap there. `0` removes the cap.

---

# Multiplayer

**This mod is multiplayer-capable and that is not negotiable.** Dedicated servers are how most people play
Valheim together, and a mutation that only exists on one machine is worse than no mutation at all: it makes the
game lie to everyone else.

Nothing in this slice may be deferred to "a later multiplayer pass".

## The rule everything follows

**Rolled once by the owner, stored in the ZDO, read by everyone.**

- A creature's stars and mutations are rolled **exactly once**, by whichever machine owns it at the time, and
  written to the creature's ZDO.
- Every other machine **reads** that state. No machine ever rolls for a creature it does not own, and no
  creature is ever rolled twice.
- ZDO data is replicated by the game itself, so this is the whole synchronisation mechanism. Do not build a
  custom RPC layer for creature state; the ZDO already is one.

## Keeping the wire quiet

Creature state rides the ZDO. A handful of things cannot - a blast, a reflect flash, a cloud appearing - because
they happen in an instant rather than being a property of the creature. Those go over the game's own RPC bus,
under two rules.

**Send to the people who could see it, not to everyone.** A per-creature effect goes through *that creature's
own* `ZNetView`, which the engine already scopes to clients holding that creature. The global
`ZRoutedRpc.Everybody` bus is for genuinely world-wide events, and a reflect flash in the Black Forest is not one
- a player fishing three biomes away should never receive that packet at all.

**One packet per event, never per frame and never per tick.** A cloud is announced once when it appears, with
its position, radius and lifetime, and every client then runs its own six-second visual locally. Do not stream a
cloud's state, do not re-announce it, and do not send a packet per damage tick.

The one to watch is **Warding**, because it fires on every melee hit rather than once per creature: a player
hitting twice a second generates two of these a second for the length of the fight. Scoped to the creature's own
`ZNetView` that is negligible; broadcast to everybody on a full server it is pure waste.

Cost is `frequency x recipients`. Keep both small and this costs nothing measurable.

## What every client must see

Given the same creature, every player looking at it sees the same thing:

- The same name on the nameplate, with the same mutations in the same order.
- The same number of stars, drawn the same way, in the same colours.
- The same hazards in the world - clouds where clouds are, a swelling corpse where one is swelling.

A player must never be able to tell, from what they can see, whether they happen to own a creature.

## Damage is the owner's decision; drawing is everyone's

This is the split that makes it work, and getting it backwards is the usual way this goes wrong.

- **Only the owner applies damage, heals, absorbs or splits.** A client never decides that a poison cloud hurt
  someone. Two machines both applying a cloud's damage is double damage; neither applying it is none.
- **Every client draws what it can see**, from the synced state, including clients that do not own the creature.
  A cloud's *existence* and position must be known to every client near it so each can render it locally.

**An invisible hazard that still hurts is the specific failure to avoid.** If a poison cloud damages a remote
player who cannot see it, that is a bug of the worst kind - unlearnable, unavoidable, and indistinguishable from
the mod being broken.

## Cases that must work

- **A dedicated server.** The server owns creatures no player is near, and hands ownership over as players
  approach. Nothing may depend on a player owning a creature.
- **Ownership changing hands.** A creature whose owner disconnects, or which is handed to another player, keeps
  its stars and mutations exactly. It is never re-rolled, never reset, never upgraded.
- **A player arriving late.** Someone who joins and walks up to a creature rolled an hour ago sees it correctly,
  with no re-roll.
- **A creature meeting a player before its owner has rolled it.** It shows as plain until the state arrives, then
  updates once, cleanly. It must not flicker, and it must not stay wrong.
- **Devouring across owners.** A Devouring creature that kills something owned by another machine still absorbs
  it. Route the work to the owner rather than skipping it: skipping means the mutation quietly stops working on
  a busy server, which is exactly where it matters most.
- **A rule file change mid-session.** Covered by `lock to server`: the server pushes its rules, clients adopt
  them. Creatures already rolled keep the mark they were born with; new creatures use the new rules.

## Testing it honestly

This cannot be verified inside the clean room - there is no game here, let alone two machines. So:

- Write the ownership assumption at every site that reads or writes creature state, as a comment, naming which
  machine is expected to run it.
- Where a decision could only be checked by running two clients, say so in `DECISIONS.md` under a heading that
  makes it findable, rather than presenting it as verified.

An honest list of what has not been proven is worth more than a confident claim that it works.


## Configuration

Two files. The `.cfg` holds switches and per-player display preferences. **A YAML rule file holds everything
that varies by biome**, because a flat config file cannot express a table of nine mutations across nine biomes
without becoming unreadable.

### The rule file

```yaml
# Elite Creatures Reborn - creature rules
#
# Percentages are 0-100. Anything a biome does not set falls back to `defaults`.
# Biome names must match the game's own: Meadows, BlackForest, Swamp, Mountain,
# Plains, Mistlands, AshLands, DeepNorth, Ocean.

# When true, players on a server use the server's copy of this file and cannot
# override it. Their own file is ignored while connected and restored when they
# leave. Display preferences in the .cfg (colours, nameplate distance) are never
# locked - they change only what that player sees.
lock to server: true

# The most mutations one creature may carry. 1 by default: a creature is Mad, or
# it is Cloaked, but not both. Raise it for stacked monsters; 0 means no cap at
# all. Where more roll than the cap allows, the survivors are picked at random.
max mutations: 1

# How much stronger a mutation is when it sits on a large star (worth 5 stars)
# rather than a small one. Multiplies the mutation's BONUS, never its cost: a Mad
# creature on a large star is much faster but still has half health. 5 - matching
# the star's worth - is available but produces absurdities like a creature
# outrunning the player, so the default is deliberately lower.
large star power: 2

defaults:
  # What a star is worth. One entry per star count, so index 0 is an unstarred
  # creature and index 5 a five-star one. Every line is documented below.
  star power:
    growth:      [0.06, 0.10, 0.15, 0.20, 0.25, 0.30]
    hp:          [1,    1.4,  1.95, 2.75, 3.85, 5.4]
    attack:      [1,    1.2,  1.45, 1.75, 2.1,  2.5]
    swing speed: [1,    1.02, 1.05, 1.08, 1.12, 1.16]
    speed:       [1,    1,    1.03, 1.06, 1.1,  1.15]
    drops:       [1,    1,    1.5,  2,    2.5,  3]

  # The chance that ANY ONE mutation appears, indexed by the creature's star
  # count. Applied to every mutation that has no entry of its own below. Each
  # mutation is rolled separately, so these do not sum to anything.
  mutation chance: [2.5, 3.5, 5, 6, 7.5, 10]

  # Per-mutation overrides. Anything not listed here uses `mutation chance` above.
  # Devouring is deliberately rarer everywhere: one of them changes a whole area.
  mutation chances:
    Devouring:   [0.6, 0.9, 1.2, 1.5, 1.8, 2.4]

  # How strong each mutation is. Named fields, not positional numbers - a row of
  # four bare decimals is exactly the thing server admins get wrong. Every field
  # is explained in the table below this code block.
  mutation power:
    Mad:         { move: 1.6, attack speed: 1.5, health: 0.5 }
    Bloated:     { health: 2.0, delay: 1.0, damage: 40, radius: 4 }
    Cloaked:     { reveal distance: 6, fade time: 0.5, fade margin: 1 }
    Splintering: { damage: 0.6, max generations: 0, max descendants: 0 }
    Leeching:    { regen: 2, lifesteal: 30 }
    Warding:     { reflect: 30, knockback: 4 }
    Plated:      { armour: 100, damage: 60 }
    Miasmic:     { cloud life: 6, cloud damage: 5, clouds per second: 1, cloud radius: 4 }
    Devouring:   { absorb health: 100, absorb damage: 100, slow per 100 health: 2, player threshold: 0.333, devour cooldown: 10 }

# Every biome below overrides only what it names. Delete a line to fall back to
# `defaults`; delete a whole biome to make it behave like the defaults entirely.
# The per-mutation entries are suggestions with a reason, not rules - they are
# here to be argued with and edited.
biomes:
  # `star chances` is per-biome ONLY - there is deliberately no global default,
  # because how starry a biome is is the main thing that separates one from the
  # next. One entry per star count, and they should add up to 100. This is a
  # straight distribution, not a chain: [73, 10, 10, 5, 1, 1] means 73 creatures
  # in 100 have no stars, 10 have one, 10 have two, 5 have three, 1 has four and
  # 1 has five. Even the gentlest biome keeps a sliver at the top - that is the
  # rare large-star creature, and it should exist everywhere, just barely.
  # Adding a seventh entry raises that biome's ceiling to 6 stars, and so on.
  - match: Meadows
    star chances:    [73, 10, 10, 5, 1, 1]
    mutation chance: [2.5,  3.5,  5,    6,    7.5,  10]

  - match: BlackForest
    star chances:    [62, 15, 12, 6, 3, 2]
    mutation chance: [3.5,  4.5,  6,    8,    10,   12.5]

  - match: Swamp
    star chances:    [52, 18, 14, 8, 5, 3]
    mutation chance: [4,    6,    8,    10,   12.5, 16]
    mutation chances:
      Miasmic:     [11, 16, 22, 28, 34, 42]   # rot belongs here
      Leeching:    [8,  12, 16, 21, 25, 31]

  - match: Mountain
    star chances:    [42, 20, 17, 10, 7, 4]
    mutation chance: [5,    7,    9,    12,   15,   18.5]
    mutation chances:
      Plated:      [16, 22, 30, 38, 46, 56]
      Cloaked:     [2,  3,  4,  5,  6,  7]   # little cover up there

  - match: Plains
    star chances:    [32, 22, 20, 13, 8, 5]
    mutation chance: [6,    8,    11,   14,   17,   21.5]
    mutation chances:
      Mad:         [22, 30, 40, 50, 60, 72]
      Devouring:   [1.5, 2, 3, 4, 5, 6]

  - match: Mistlands
    star chances:    [22, 22, 22, 16, 11, 7]
    mutation chance: [6.5,  9,    12.5, 16,   20,   25]
    mutation chances:
      Cloaked:     [30, 40, 52, 64, 76, 90]  # the mist hides things already
      Devouring:   [2, 3, 4, 5, 6, 8]

  - match: AshLands
    star chances:    [12, 20, 24, 20, 15, 9]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Bloated:     [38, 50, 64, 78, 92, 100]
      Splintering: [28, 37, 48, 58, 69, 82]

  - match: DeepNorth
    star chances:    [12, 20, 24, 20, 15, 9]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Plated:      [38, 50, 64, 78, 92, 100]

  - match: Ocean
    star chances:    [68, 12, 10, 6, 3, 1]
    mutation chance: [3,    4,    5.5,  7,    8.5,  11]
```

### What `star power` means

Every line is one entry per star count, index 0 through 5.

| Line | Meaning |
| --- | --- |
| `growth` | Size **bonus**, added to 1. `0.20` at 3 stars means a 3-star creature is 20% larger. |
| `hp` | Max health **multiplier**. `2.75` at 3 stars means 2.75x an ordinary one of its kind. |
| `attack` | Damage multiplier, applied to everything it deals. |
| `swing speed` | Attack and animation speed multiplier. Deliberately gentle - a fast-swinging creature gets unfair long before a hard-hitting one does. |
| `speed` | Movement speed multiplier. Also gentle: a creature you cannot outrun is a different kind of problem. |
| `drops` | Loot quantity multiplier. `1` for the first two entries means stars do not pay until the second one. |

These replace vanilla's own level scaling rather than stacking on top of it, so the numbers here are the whole
story. `hp` at index 0 must be `1` unless you intend unstarred creatures to differ from vanilla.

`growth` at index 0 is `0.06`, which makes even an unstarred creature slightly larger than vanilla. Set it to
`0` if that is not what you want.

### How star power and mutation power combine

**Additively, never multiplicatively.** Every multiplier in this mod is read as a *bonus above 1*, the bonuses
are added together, and the total is applied once:

```
final = 1 + (star value - 1) + sum of (mutation value - 1) for every mutation it carries
```

So a 4-star Bloated creature, with `hp[4] = 3.85` and Bloated `health: 2.0`:

```
1 + (3.85 - 1) + (2.0 - 1) = 4.85x health
```

not `3.85 x 2.0 = 7.7x`. Multiplying is what makes a four-mutation creature absurd rather than memorable, and
it is why the stacked extremes in this spec are survivable at all.

**A note on the arithmetic.** Adding the multipliers outright - `3.85 + 2.0` - looks simpler but double-counts
the creature it started as: an unstarred Bloated would come out at `1.0 + 2.0 = 3x` health when Bloated alone
should give `2x`. Subtracting the 1 from each term fixes that, at the cost of the headline number being slightly
lower than a straight sum: "3x stars plus 3x mutation" lands on 5x, not 6x. If you want the 6x, raise the values
in the file - the point of this rule is that the two sources add rather than compound, and that holds either way.

**This applies to every stacking value**: health, attack, movement speed, swing speed and size. A mutation that
*reduces* a stat contributes a negative bonus, so Mad on a 4-star creature is `1 + 2.85 - 0.5 = 3.35x` health -
still a big creature, still visibly frailer than its unmutated neighbour, which is the point of Mad.

Two mutations pulling opposite ways simply cancel in proportion: Bloated and Mad together on a 4-star creature
give `1 + 2.85 + 1.0 - 0.5 = 4.35x`.

**One hard floor, for correctness rather than balance:** a combined health multiplier below `0.05` is clamped
to `0.05`. Nothing else is clamped. A creature with zero or negative maximum health is a crash, not a fight.

`drops` is the exception - it is a quantity multiplier with no mutation contributing to it, so it is used as
written.

### What every mutation power field means

The implementation must repeat this table as comments inside the generated file. A server owner editing
`mutation power` should never have to guess what a number does.

| Mutation | Field | Meaning |
| --- | --- | --- |
| Mad | `move` | Movement speed multiplier. `1.6` = 60% faster. |
| Mad | `attack speed` | Attack and animation speed multiplier. |
| Mad | `health` | Max health multiplier. `0.5` = half health. This is its cost. |
| Bloated | `health` | Max health multiplier. |
| Bloated | `delay` | Seconds between death and the blast - the window a player has to get clear. |
| Bloated | `damage` | Blunt damage at 0 stars, multiplied by `(1 + stars)`. |
| Bloated | `radius` | Blast radius in metres. |
| Cloaked | `reveal distance` | Metres at which it becomes visible. The nameplate hides in step. |
| Cloaked | `fade time` | Seconds to phase in or out. `0` snaps. |
| Cloaked | `fade margin` | Extra metres before it fades back out, so it cannot strobe at the boundary. |
| Splintering | `damage` | Damage multiplier for a splintering creature. `0.6` = 40% weaker. |
| Splintering | `max generations` | Safety cap on cascade depth. `0` = unlimited, the default. |
| Splintering | `max descendants` | Safety cap on live descendants at once. `0` = unlimited. |
| Leeching | `regen` | Percent of max health regained per second. |
| Leeching | `lifesteal` | Percent of damage dealt returned to it as health. |
| Warding | `reflect` | Percent of incoming damage returned to the attacker. |
| Warding | `knockback` | Force applied to whoever lands a melee hit on it. |
| Plated | `armour` | Percent armour bonus at full health, falling to zero as it is hurt. |
| Plated | `damage` | Percent damage bonus at zero health, rising as it is hurt. |
| Miasmic | `cloud life` | Seconds a dropped cloud lasts before fading. |
| Miasmic | `cloud damage` | Strength of the Poison applied to a player standing in one. |
| Miasmic | `clouds per second` | How often it drops one while moving. |
| Miasmic | `cloud radius` | Metres. The visible cloud must match this exactly. |
| Miasmic | `cloud effect` | Name of the vanilla prefab cloned for the cloud. |
| Miasmic | `body effect` | Name of the vanilla poison visual worn on the creature itself, at all times. |
| Bloated | `blast effect` | Name of the vanilla prefab cloned for the explosion. |
| Bloated | `warning effect` | Name of the vanilla prefab played on the corpse during the delay. |
| Devouring | `absorb health` | Percent of a victim's max health added to its own. |
| Devouring | `absorb damage` | Percent of a victim's damage added to its own. |
| Devouring | `slow per 100 health` | Percent movement speed lost per 100 absorbed health. Its cost. |
| Devouring | `player threshold` | Fraction of a player's max health its per-hit damage must reach before it hunts players for good. `0.333` = a third. |
| Devouring | `devour cooldown` | Seconds before it can devour again after a meal. `10` by default. |

### What these defaults actually produce

Two things decide whether a creature is mutated: its **star count**, and a per-mutation **chance** roll. The
defaults are tuned so a quarter of Meadows creatures are something, rising to three quarters in the Ash Lands:

| Biome | Mutated | Has a large star |
| --- | --- | --- |
| Meadows | 25% | 1% |
| Black Forest | 34% | 2% |
| Swamp | 42% | 3% |
| Mountain | 50% | 4% |
| Plains | 59% | 5% |
| Mistlands | 67% | 7% |
| Ash Lands | 75% | 9% |
| Ocean | 29% | 1% |

**It tops out at three quarters on purpose.** An unmutated creature has to stay a real possibility even in the
worst biome, or "mutated" stops meaning anything and the ordinary ones stop being a relief.

**Large stars exist everywhere, barely.** One Meadows creature in a hundred has five stars, rising to nine in a
hundred in the Ash Lands. Even the gentlest biome can produce one, so the enhanced-mutation rule is a rare
event a new player might meet once rather than something gated behind progression.

Lower the `mutation chance` curve if you want mutations rarer, or raise `star chances` at the top end if you
want big creatures commoner. The two dials are independent.

### Rules the file obeys

- **A biome block overrides only what it names.** Everything else comes from `defaults`. A biome that sets only
  `star chances` still gets every mutation chance and power value from the defaults.
- **An unknown biome falls back to `defaults`** for everything except `star chances`, which has no global
  default. An unlisted or modded biome takes the **Meadows** row for stars, and that is logged once so it is
  visible rather than mysterious. Gentle is the right failure: a modded biome should not silently become the
  hardest place in the world because nobody wrote an entry for it.
- **`star chances` that do not sum to 100** are normalised rather than rejected, and a warning is logged. A
  server owner editing one number should not have to rebalance the rest by hand.
- **A bad file never takes the server down.** Errors are reported line by line in the log and the previously
  loaded rules stay in force.
- **The file reloads on edit**, while playing, without a restart.
- **The file is written on first run** with every value at its default and every comment in place, so a server
  owner always has a complete, documented file to edit rather than a blank one.

### Locking clients to the server

`lock to server: true` means a connected player plays by the server's rules: the server sends its file on join
and on change, and the client's own copy is ignored until they disconnect, then restored. This covers star
chances, star power, mutation chances, mutation power and `max mutations` - everything that changes what the
game does.

It never covers the display preferences in the `.cfg`: colours, nameplate distance, whether mutation names show. It is
also not how creature state travels - that is the ZDO, see Multiplayer above.
Those change only what one player sees, so each player keeps their own.

A player without the mod, or with an incompatible version, is refused at the join screen with a reason.

## What a player sees

**Names first.** A creature's mutations are part of its name, in the table's order, before its own
name: "Mad Greydwarf", or "Bloated Warding Miasmic Troll" for one carrying three. Names are the
authoritative tell and are always readable.

**Stars are coloured by mutation.** Each mutation has its own colour, and the creature's stars take
those colours one for one - a 3-star creature with two mutations shows two coloured stars and one
plain:

| Mutation | Star colour |
| --- | --- |
| Mad | Red |
| Bloated | Brown |
| Cloaked | Blue |
| Splintering | Bright white |
| Leeching | Green |
| Warding | Dark blue |
| Plated | Yellow |
| Miasmic | Dark green |
| Devouring | Dark red |

**Cloaked is blue and Warding is dark blue**, so those two must be clearly separable on a small star: keep
Cloaked a bright, light blue and Warding genuinely dark, near navy. Judge it at a small star's size against
snow and against night, not on a colour swatch.

Where a creature has more mutations than stars - an unstarred one with a mutation, or a 1-star that
rolled two - the surplus shows in the name only. The name is always complete; the stars are as
complete as they can be.

Colour is always redundant with the name and never the only carrier of information, so a
colourblind player loses nothing by it. The palette is a per-player setting and editable; the
server never locks it, because it changes only what that player sees.

### How the star row is drawn

**Terms.** A creature's **star count** is a number - how strong it is. A **glyph** is one star symbol actually
drawn on its nameplate. They are not the same once stars count in fives: a small glyph is worth one star, a
large glyph is worth five, so a 9-star creature draws five glyphs (one large, four small). Everything about
colour and mutations below counts *glyphs*, because a glyph is what a colour can be painted on.

**Stars count in fives.** A small star is worth one, a **large star is worth five**. A creature shows
`stars / 5` large stars followed by `stars % 5` small ones:

| Stars | Shown as |
| --- | --- |
| 1 to 4 | that many small stars |
| 5 | one large star |
| 6 | one large, one small |
| 9 | one large, four small |
| 10 | two large |
| 13 | two large, three small |
| 25 | five large |
| 50 | ten large |

This keeps the row short without ever hiding the count: five stars reads as one big star rather than a smear of
five small ones, and a player learns "big means five" in a single encounter. **No numeral is ever used** - the
row is always countable.

**Sizes**, both given against the size vanilla draws a star at:

| Glyph | Size |
| --- | --- |
| Small star | **1.3x vanilla** - 30% larger, so a single star is not a speck |
| Large star | **2.2x vanilla**, unchanged |

That leaves a large star about 1.7x the small one - still unmistakably the bigger glyph, but the two now sit
closer in scale so a mixed row looks like one row rather than a boulder with pebbles after it. Both numbers are
settings.

The widest it can get is **13 glyphs, at 49 stars** (nine large and four small). Every count below that is
narrower, so 13 is the layout budget.

**The ceiling is 50 stars.** Not because anyone will use it, but because a bound has to exist somewhere and an
unbounded one eventually produces a row wider than the screen. A `star chances` list longer than 51 entries is
truncated with a warning.

### Where the star row sits

Directly **below the health bar**, **left-justified** to the bar's left edge. It must never overlap the
creature's name or the bar itself - an earlier build placed it above the plate where it covered the name, which
is worse than not drawing it at all, because the name is the authoritative tell and the stars are only a
shortcut.

- Below the health bar, not above it and not on it.
- Left edge of the first glyph aligns with the left edge of the health bar. The row grows rightward; it does
  not centre, because a centred row shifts every star sideways whenever the count changes and makes two
  creatures harder to compare at a glance.
- **Every glyph is anchored top-left, and the row is anchored top-left.** All glyphs share the same *top* edge,
  whatever their size - they are not centred on a line and not sitting on a common baseline. A large star is
  bigger, so from that shared top edge it simply extends further **down** than the small stars beside it. Its
  top does not rise above them and its middle does not line up with theirs.

  This is what makes a mixed row read as one row: the eye follows the straight top edge and the large stars hang
  below it. Centring them instead makes the small stars appear to float, and baseline-aligning them makes the
  large ones tower.
- The whole plate - name, bar and stars - must stay within vanilla's existing screen footprint. Nothing the mod
  adds may make a nameplate taller or wider than vanilla's, or plates start colliding with each other in a
  crowd, which is exactly when you most need to read them.

### What colour the stars are

A creature with **no** mutation keeps vanilla's own star colour, so an ordinary starred creature still looks
ordinary.

Otherwise **every glyph is coloured - none are ever left plain** - and the glyphs are divided between the
creature's mutations in table order, left to right, as evenly as they go, with any remainder to the leftmost.
Each glyph is drawn solid: nothing is wedged, blended or half-shaded, so a small star stays legible.

| Glyphs | Mutations | Result |
| --- | --- | --- |
| 4 | **1** | **all four in that mutation's colour** |
| 1 | 1 | the one glyph in that colour |
| 4 | 2 | two and two |
| 5 | 2 | three then two |
| 4 | 4 | one colour each |

**The single-mutation case is the one that matters**, because `max mutations` is 1 by default and that is what
nearly every player will ever see. A 4-star Mad creature shows **four red stars**, not one red and three grey.
The whole row being one colour is the loudest, most readable signal the nameplate can give, and at the default
configuration it is the normal case rather than an edge case.

Colour is always redundant with the name, so a colourblind player loses nothing by it.

### One mutation per glyph, and the rest in the name

**Each glyph shows one mutation, drawn solid in that mutation's colour**, assigned in table order left to
right. No glyph is ever split, so a small star a few pixels across stays legible.

This is a rule about **display**, not a cap. A creature may carry more mutations than it has glyphs; the surplus
simply **shows in the name only**. The name is always complete, the stars are as complete as they can be.

So a **starless creature can be mutated** - it has no glyphs, so its name carries everything. It is called "Mad
Greydwarf" and looks like an ordinary greydwarf until you read the plate. That is the one case in the mod where
a mutation has no visual tell, and it is accepted deliberately: the creature's body colour is promised to elemental
attunement in a later slice, so borrowing it now would only mean taking it back.

How many a row can show, for reference:

| Stars | Row | Mutations shown on stars |
| --- | --- | --- |
| 0 | nothing | 0 - all in the name |
| 1 to 4 | that many small | 1 to 4 |
| 5 | one large | 1 |
| 6 | one large, one small | 2 |
| 9 | one large, four small | 5 |
| 29 | five large, four small | 9 - all of them |

Showing all nine on the stars needs 29 stars. That is far outside any sane configuration and does not matter: a
server that sets a 29-star ceiling has asked for a screen-wide nameplate and can have one. At the defaults the
cap is 1 and the question never arises.

### A mutation on a large star is enhanced

A large star is worth five. A mutation sitting on one is correspondingly **stronger** - that is what makes a
high-star creature different in kind rather than just carrying more labels.

**`large star power` multiplies the mutation's gain, and only its gain.** Default `2`.

```yaml
  # How much stronger a mutation is when it sits on a large star (worth 5 stars)
  # rather than a small one. Multiplies the mutation's BONUS, never its cost:
  # a Mad creature on a large star is much faster but still has half health.
  # 5 - matching the star's worth - is available but produces absurdities like a
  # creature outrunning the player, so the default is deliberately lower.
  large star power: 2
```

Read against the additive model: a mutation's bonus is `value - 1`, that bonus is multiplied by
`large star power` when it sits on a large star, and the result joins the sum as usual. Bloated's `health: 2.0`
is a bonus of `+1.0`; on a large star at the default it contributes `+2.0`.

**Costs are never multiplied.** Mad's `health: 0.5` is a bonus of `-0.5` and stays `-0.5` wherever it sits.
Scaling a penalty alongside a gain would push a creature's health multiplier negative at a high enough setting,
and "enhanced" should mean better at being itself, not worse.

**Fields that are not stat bonuses do not scale either** unless it makes sense for them to:

| Mutation | Enhanced on a large star | Left alone |
| --- | --- | --- |
| Mad | `move`, `attack speed` | `health` (a cost) |
| Bloated | `health`, `damage`, `radius` | `delay` - a longer fuse helps the player, not the creature |
| Cloaked | `reveal distance` | `fade time`, `fade margin` |
| Splintering | - | all of it; the split table already keys off stars |
| Leeching | `regen`, `lifesteal` | - |
| Warding | `reflect`, `knockback` | - |
| Plated | `armour`, `damage` | - |
| Miasmic | `cloud damage`, `clouds per second` | `cloud life`, `cloud radius` |
| Devouring | `absorb health`, `absorb damage` | `slow per 100 health` (a cost), `player threshold` |

**Movement is clamped regardless.** However the numbers land, a creature may not end up faster than an
unburdened player - a fight you cannot disengage from is not a fight. Clamp and log rather than obey.

`large star power` is a rule-file value like any other: it sits in `defaults` and a biome may override it, so a
server can make Ash Lands large stars ferocious and leave the Meadows alone.

Mutations are not readable before the nameplate resolves. Walking into a surprise is part of it -
except for Cloaked, which is the opposite problem and is the point of that mutation.

## Where this document is silent

Decide, build it, and write the decision and your reasoning in `DECISIONS.md` alongside the source.
Do not guess at how another mod might have done it - you have no way to know what those are, and
that is deliberate.

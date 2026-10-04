# Elite Creatures Reborn - specification: Mutations

One feature of the mod, specified on its own. The other feature files sit beside it; `SPEC.md` is the whole-mod
behaviour document that all of them are drawn from.

Stars and per-star scaling are **not** specified here - see `SPEC-scaling.md`. This file assumes a creature has a
star count and specifies only what a mutation is and what each one does.

Numbers are defaults and all of them are configurable through `creature_rules.yml`. Where a number is a judgement
call it says so.

**Status:** built and shipping in 3.0.0. Verified by build and by reading; not yet tested in a live multiplayer
session against the checklist in "Testing it honestly" below. Gilded, Blinking and Relentless, per-creature
mutation rules and the 3.9.0 changes to Bloated, Cloaked, Warding and Splintering are built and not yet tested in
game. So are Devouring's two limits (prey no bigger than 125% of its own health, one meal per star), the eaten
creatures on its nameplate, its devour tell at a fifth of its old size and Bloated's 1.7-second fuse (2026-09-28),
and the devour and Warding tells heard with nothing drawn (2026-09-30).

**Bosses never take mutations.** They take stars on their own separate table - see `SPEC-scaling.md`.

---

# Mutations

## What a mutation is

A mutation is a **named, visible character** a creature can carry. Where stars make a creature
bigger, a mutation makes it *different* - a fight you approach differently once you have learned to
recognise it.

**Not every mutation is a drawback-balanced trade.** Six of the thirteen below cost the creature
something; six are pure gains with no downside; and one, Gilded, is good news for the player - it never
fights back and pays several times over for the chase. That is deliberate. The balancing lever for this
feature is **how often mutations appear**, not making each one internally fair. A player who meets
a Miasmic Cloaked troll is supposed to be in trouble.

Mutations are never elemental. Elemental behaviour belongs to a later slice and must not appear
here.

## The thirteen

| Mutation | Gains | Costs |
| --- | --- | --- |
| Mad | Moves and attacks far faster - +60% movement, +50% attack speed | Half health |
| Bloated | Double health. On death it smokes for 1.5 seconds, then explodes | None |
| Cloaked | Invisible at more than 10 metres (15 for trolls and lox), nameplate included. Never rolled by a drake | None |
| Splintering | Splits into two copies when killed, each a star weaker or more | Deals 40% less damage |
| Leeching | Regenerates 0.5% of max health per second once it has taken no hit for 5 seconds, hard-capped at 20 hp/s, and heals 10% of damage it deals | None |
| Warding | Reflects 30% of each hit's base damage back at the attacker, never more than 7.5% of the attacker's maximum health in any one second, and knocks them back on any melee hit | None |
| Plated | Cuts incoming damage by a flat, capped percentage at full health | Sheds that cut as it is hurt, and its damage rises as it does |
| Miasmic | Leaves a trail of poison clouds as it moves; each cloud lingers 6 seconds then fades | None |
| Devouring | Eats other creatures and keeps what it takes - one per star, none with more health than it has, never a boss or a large creature - and wears what it ate on its nameplate. See below | Grows slower the more it has eaten |
| Thieving | Takes one item from your inventory on each strike that lands, until it holds one per star (never fewer than `max items`, never more than 8), and carries them. See `thieving.md` | None |
| Gilded | Glitters gold; never attacks a player and runs from any it sees. Drops three times its loot plus a purse of coins. See below | None - it is the player's good luck |
| Blinking | Every 30 seconds of combat it vanishes and reappears 4 metres behind its target, after a half-second tell at the spot. See below | 25% less health |
| Relentless | Keeps the target it picked while that target is within 150 metres, seen or not; sneaking does not hide you from it. See below | Never faster than its base speed |

Judgement calls in that table, all tunable: the Mad percentages; Devouring's one meal for an unstarred devourer
(`min meals`); Bloated's 1.5-second fuse and its explosion doing 40 damage in a 4 metre radius, scaled by its star count; Cloaked's 10 metres, 15 for trolls and lox, and none at all for drakes - an invisible flyer spitting frost from above is no fight; Splintering's 40% damage reduction; Leeching's regen
and lifesteal rates, its 5-second combat cooldown and its 20 hp/s regen cap - a high-health creature must be
beatable on regen alone; Warding's 30% reflection and its 7.5% ceiling; Plated running from a 40% damage cut at full health to none at
zero, hard-capped at 55% so a large star cannot approach invulnerability, while its damage climbs from nothing to
+60%; Miasmic's clouds doing poison damage on a par with a
Blob's and appearing about one per second of movement; Thieving's no-theft-on-a-blocked-hit rule, its one item per
star and its `max items: 1` floor - see `thieving.md`; Gilded's rarity, its x3 loot and 20 coins per star plus one,
and its 30-metre flee distance; Blinking's 30-second rhythm, 4 metres and half-second tell; Relentless's 150 metres.

## Devouring, in full

Devouring is one of the most involved of the thirteen and the one most worth getting right. It is meant to be a *situation*
rather than an encounter: you come over a rise and find something in the middle of eating a camp, and you decide
what to do about it.

### It eats in one bite

When a Devouring creature lands an attack on **another non-boss creature, that creature dies instantly** - if it
is small enough and the devourer still has appetite left (the two limits below). No chewing, no health bar, no
fight - it is simply gone, and the devourer takes what it was.

On that kill it gains, permanently:

- `absorb health` percent of the victim's maximum health, added to its own.
- `absorb damage` percent of the victim's damage, added to its own.

Health defaults to 50, damage to 25 - a meal makes it stronger, several meals make it a problem, but only a long
unattended feast makes it lethal (100/100 shipped originally and a camp-fed greydwarf one-tapped players through
a parry; damage is cut hardest because damage is what kills). These accumulate with every meal, so one left alone
in a busy area still becomes genuinely enormous - and visibly so, because it grows with what it has eaten.

An instant kill needs an unmistakable tell at the moment it happens, so a player nearby knows a creature was
eaten rather than wondering what became of it. **The tell is a sound only, since 2026-09-30:** the sound of the
game's `fx_aspect_death` (a boss-sized burst of sixteen particle systems and a light), at the game's own volume,
with nothing of it drawn - no particles, no light, no camera shake. The user asked for the particles to go: first
they swallowed the scene, and cut to a fifth of their size on 2026-09-28 they still cost frames. The prey vanishing
is what the player sees. Each client plays its own copy of the tell, so every client nearby hears it.

### Only what it can swallow

It eats only a creature whose **current health is at most `max prey health` percent of its own current health** -
100 by default since 2026-10-03 (125 before), so no more than its own, at the user's request - and never a boss or a
large creature (a body a metre or more wide or 3.5 m or more long, with 300 or more health; `Traits/BodySize.cs`). Anything bigger is not prey: it does not hunt it, and a blow that lands on one is an ordinary hit.
Toward a bigger creature it keeps the game's own manners - it fights what its kind fights and fights back when
attacked - rather than standing defenceless while its blows pass through. Both healths are read at the moment it
matters, so a wounded devourer's reach shrinks and a creature already hurt comes within it. `0` lifts the limit.

### One creature per star

It eats **one creature per star in its whole life** - a 2-star devourer eats two - and never fewer than `min meals`,
1 by default, so an unstarred one still eats once (the same floor Thieving's pouch has). The count lives in the
creature's ZDO with what it ate, so a reload or a hand-over never gives it a fresh appetite.

Once it has eaten them all it is **sated**: it devours nothing more, and it stops treating creatures as prey and
players as beneath notice - the game's own rules of who fights whom take over, so it behaves like any creature of its
kind. Whatever it has eaten, it keeps. If it has already grown big enough to hunt players, it hunts them as before.

Judgement calls, made 2026-09-28 where the request was silent: the floor of one meal (a Devouring creature that can
never eat would wear the name for nothing); what a sated one does (it has nothing left to feed on, so the feeding
behaviour - ignoring players - ends with it); and that a creature too big to eat gets the game's own treatment
rather than being ignored outright.

### You can see what it ate

Its nameplate shows **one icon per creature it has eaten**, the way a Thieving creature's shows what it carries: on
the star row's line, right-justified, oldest leftmost, shrinking to vanilla star size and then dropping the oldest
when they do not all fit. The icon is the eaten creature's **trophy**, the first trophy in its own drop table; a
creature with no trophy (a greyling, a hen, many modded creatures) shows the **game's horned monster head**, the
map's boss-pin sprite. Both come from the game at runtime; nothing is shipped. A creature that is Thieving as well
shows its pouch at the right edge and its meals just left of it. The icons hide with a Cloaked creature's plate.
`Show devoured creatures` and `Devoured creature icon size` (1.6) in the display settings govern them.

### Then it has to wait

After a meal it **cannot devour again for `devour cooldown` seconds** - 60 by default (10 originally, which
consumed a camp faster than a player could reasonably respond to). It carries on as an ordinary creature during
that time: it can move, it can be fought, it can be killed. It just cannot eat.

The cooldown is what makes the mutation a decision rather than a disaster. A camp is not consumed in a second;
it goes one creature a minute, and you can watch it happen and choose whether to intervene.

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
- **So does what it has eaten.** The meal list (`ecr_dev_meals`, the eaten creatures' prefab hashes) is written by
  the devourer's owner as it banks a meal and read everywhere: the prey's owner checks the one-per-star limit against
  it at the bite, and every client draws the nameplate icons from it. Both limits are checked where the bite
  resolves, from the devourer's replicated ZDO, so they need no routing. Because one swing can bite two creatures
  before the first meal is banked, the owner checks the limit again as it banks, and a meal past it is not kept.
- **The kill tell is heard by every nearby client**, not only the owner. A creature vanishing in silence on
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

Its base speed - before the slow - is the `move` multiplier, `1` by default: a fresh devourer moves like its
base creature. Admins who find that too hot can ship it slower from the first bite (`move: 0.5` halves it). The
knob is deliberately not enhancement-scaled: it is tuning, not power.

## Bloated, in full

**The fuse is 1.5 seconds** (`delay: 1.5`, shortened to 1.7 on 2026-09-28 and to 1.5 on 2026-10-03, both at the
user's request; two seconds from 3.9.0, one before that) - the window a player has to see the smoke and get clear.

**The blast goes off at the corpse, not at the place of death.** A creature that dies turns into a ragdoll, and
in the seconds before it detonates that ragdoll can slide, tumble or roll down a hill. Exploding at the spot
where it died would leave the bang somewhere the corpse visibly is not, which reads as a bug even to a player
who could not say why.

So the fuse follows the body:

- The warning smoke **rides the corpse's body**, wherever it goes, for the whole fuse. It follows the body itself,
  not the point the corpse was spawned at, which stays at the death spot while the limbs roll away. The thing that
  is about to explode is the thing that looks like it is about to explode.
- The smoke **stays at full strength until the blast.** It never thins in its last second the way a poison cloud
  does, because a thinning warning says the danger is passing, and here it is about to arrive.
- The blast happens at **wherever the corpse has come to rest** when the fuse ends, and the damage radius is
  measured from there.
- If there is no ragdoll - a creature that simply vanishes, or one whose corpse is already gone - it falls back
  to the place of death. Better a blast in the right general area than none.

### The corpse goes with the blast

The explosion is the corpse's end. At the blast the corpse vanishes and **its loot drops there**, exactly the loot
it would have dropped anyway, with no corpse smoke afterwards: no body lying on after the bang, and no white puff
a moment later.

- The game removes a corpse on its own timer - two seconds for most creatures, longer for the biggest - which would
  race the fuse or outlive the blast. So each machine moves its own corpse's timer to **one second past the fuse**.
- **If the blast never comes** - the machine that owned the creature left mid-fuse - that moved timer removes the
  corpse the game's ordinary way, loot and smoke, a second after the fuse. A corpse is never left lying and its loot
  is never lost.

### How big it looks

**The burst is drawn at 70% of its old size**, every part of it, not only its core. At full size it read as far
bigger than the blast it stood for. The damage still reaches the whole `radius`; only the picture is smaller.

### Which corpse, in multiplayer

Ragdoll physics is not synchronised: every machine simulates its own copy, so the corpse settles in a slightly
different spot on each, and on a slope that can be metres apart.

- **The warning rides each client's own local corpse.** It is cosmetic, it looks right on every screen, and the
  small divergence costs nothing.
- **The blast position is the owner's corpse**, sent when the fuse ends rather than when the creature died. Every
  client draws the blast there, and the owner deals the damage there, so what everyone sees and what actually
  hurts are the same place.
- **The blast names the corpse.** The corpse itself is a shared object, and only the machine that owns it can
  remove it for everyone - normally the same machine, but ownership can move. Every machine is told which corpse
  it was; only its owner removes it, and the removal replicates.
- **Each machine draws the blast once, locally**, and hears the bang once.

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

**A Thieving parent drops its pouch at the moment it splits, exactly as any other death, and the copies are born
carrying nothing.** A copy inheriting or sharing the pouch would duplicate stolen items; see `thieving.md`.

**A tamed parent splits into tamed copies**, rather than into two wild creatures at its owner's feet. Both copies
are tame, keep the name the player gave the parent (and who gave it), and fall in behind the player the parent was
following. A wild parent still makes wild copies. What is not carried over: the
feeding timer (the copies start hungry), a saddle, and any summon limit.

In practice a 5-star Splintering kill produces about **nine** extra creatures over two or three
short generations. The full unbroken chain still reaches 62, but a single lineage running 5 to 4 to
3 to 2 to 1 is roughly a **1 in 2,000** event. That is the intent: the big cascade should happen to
a server once, be talked about, and not be the norm.

Two safety knobs exist and are **off by default**: a cap on how many generations deep a cascade may
go, and a cap on how many live descendants one cascade may have at once. Turning either on
truncates a cascade rather than preventing the mutation.

### Investigated: "copies despawn when kited" (Roman6Migrish) - not reproduced, no defect found

Reported: a Splintering copy vanishes if the player leaves the area before killing it. Not reproduced in testing.
Checked `Mutations/Splitter.cs`'s `SpawnCopy`/`Configure` against the game's own `ZNetView`, `ZNetScene` and
`ZDOMan` (decompiled to `~/scratch/decomp` for signatures only, per `CLEANROOM.md`):

- **ZDO persistence.** `ZNetView.Awake()` sets `zdo.Persistent = m_persistent` straight from the prefab's own
  component, whatever GameObject creation path triggered `Awake()`. A copy uses
  `ZNetScene.instance.GetPrefab(prefabHash)` - the identical prefab reference a normal spawn of that creature
  uses - so its persistence is whatever that species already ships with, unchanged by splitting.
- **`Object.Instantiate` vs `ZNetScene`.** Vanilla's own networked spawn RPC, `ZNetScene.SpawnObject` /
  `RPC_SpawnObject`, does nothing more than `UnityEngine.Object.Instantiate(prefab, pos, rot)` - the exact call
  `SpawnCopy` makes. `ZNetView.Awake()` creates the ZDO and registers it with `ZNetScene` on its own, triggered by
  Unity's component lifecycle; there is no separate registration step a direct `Instantiate` call skips.
- **Ownership.** `ZDOMan.CreateNewZDO` assigns the creating machine's own session as owner immediately, and
  `Splitter.Split` only ever runs from the local machine's own creature dying (`DeathPatch.Capture` requires
  `nview.IsOwner()`), so a copy is owned exactly the way any owner-side spawn is. Ownership moving to another peer
  as it leaves range is the same automatic, distance-based reassignment every `ZNetView` object goes through -
  nothing here singles a copy out.
- **`despawnInDay`/`eventCreature`.** Per-prefab `MonsterAI` flags, read from the ZDO with the prefab's own field
  as fallback. A copy inherits whatever the species already carries; if this were the cause, the un-split parent
  of the same species would despawn on the same schedule regardless of Splintering.
- **Zone unload.** Destroying and later recreating a GameObject when its zone falls out of range - and, for a
  non-persistent creature, dropping the ZDO itself rather than just the GameObject - is standard Valheim behaviour
  for the species, not something `SpawnCopy` opts a copy into or out of.

**No defect found.** `SpawnCopy` sets up a copy exactly the way vanilla's own spawn path does, using the same
prefab as the parent, so a copy cannot be more or less persistent than an ordinary member of its species. The
likeliest explanation is standard despawn-on-zone-unload behaviour for whichever creature was split, made
noticeable only because the player was watching the copies chase and then vanish - not something Splintering
introduces. One thing this could not check: the prefab's actual `m_persistent` value is Unity asset data, not
code, so it cannot be read from the assembly - but it is shared by parent and copy either way, so it cannot explain
a copy-only symptom. Not fixed, because nothing here is broken; reopen only with a reproduction that names the
exact species, single-player vs dedicated server, and whether the unsplit parent shows the same behaviour under
the same conditions.

## Gilded, in full

Gilded is the loot goblin, and the one mutation that is good news for the player. It never fights a player: it
runs, and a player who catches it is paid several times over. The chase is the fight.

### Rare, and seen from far off

- **It is the rarest mutation.** It has its own chance curve under `defaults`, `Gilded: [0.2, 0.25, 0.35, 0.45,
  0.55, 0.7]` (halved on 2026-10-03 at the user's request) - one creature in five hundred at no stars, 0.7 in a hundred
  at five. A biome's `mutation chance` line does not raise it, because it has a curve of its own; the world tier still
  does.
- **Never on a large creature** (2026-10-03, at the user's request): a troll, bear, lox or anything else
  `Traits/BodySize.cs` measures as large never rolls Gilded - or Relentless - and never inherits either.
- **It glitters gold.** Gold flakes, spark trails and a softly pulsing gold halo on its body - the stamina mead's
  sparkle (`glitter effect`, `vfx_Potion_stamina_medium`), made steady and renewed every 2.5 seconds - and a warm
  gold light around it, 8 metres across. Spotting it first is how a player gets a chance at it, so it reads well
  beyond nameplate range: a distant sparkle stays a few pixels on screen instead of shrinking to nothing.
- The glitter is sized to the creature's body and thinned by the player's `Effect density`; at 0 nothing is drawn,
  the light included.
- **A Gilded creature that is also Cloaked hides its glitter while it is hidden.** Otherwise the glitter would give
  the cloak away.

### It never attacks a player

- **Players are never its enemies.** It never targets, chases or strikes one, and its swings and shots pass through
  players the way they pass through its own kind. It is one-sided: players can still hit it, and its health bar
  still reads as an enemy's. Nor are players its friends: a Gilded creature with a friend-helping ability, such as
  a Greydwarf shaman's heal, never walks up to a hurt player to use it.
- **It runs from any player it can see** within `flee distance` (30 metres). "Can see" is the game's own sight
  check - line of sight, its view cone until it is alerted, the player's stealth, mist - so sneaking up on one works.
  Crouch, come from behind, and it may not notice you until you are close.
- When it sees one it gives the alert and bolts, using the game's own flee, the same one a creature uses to run from
  fire: a reachable point about 25 metres away from the nearest player it can see, picked again every couple of
  seconds.
- **A hit from any range sends it running too**, away from whoever hit it, seen or not. An arrow out of the dark
  startles it.
- **It keeps running for 3 seconds** after it last saw a player or was hit, so a tree trunk between you does not
  stop it dead. Then it goes back to what it was doing.
- **It stays outrunnable.** It has no speed bonus of its own, and the movement clamp every creature has keeps it no
  faster than an unburdened player.
- While it runs it does not fight other creatures or eat. With no player in sight it behaves as its kind does,
  except that it still never picks a player to fight.
- **Only monsters run this way.** An animal - a deer, say - already runs from whatever threatens it and never
  attacks, so it keeps its own behaviour. It still glitters and still pays.
- `flee distance: 0` stops it running from what it sees; a hit still sends it running.

### It pays

- **Its drops are multiplied by `loot`** (3), after the loot mode, on exactly the rows the global loot multiplier
  touches: its own drop table and any row the rule file names for it. Trophies only when the trophy switch is on;
  rows another mod added, never.
- **It pays in every loot mode**, Vanilla and Curated included. In Vanilla, where the loot rules otherwise stand
  aside, the multiplier scales the creature's own table alone.
- **Plus a purse**: `bonus amount` (20) times (1 + stars) of `bonus item` (Coins) - 20 at no stars, 60 at two, 100
  at four. The purse comes after every multiplier and is never multiplied by any of them. It is capped at 100, the
  most one drop row can hold; if the creature already drops Coins, the purse joins that row, which stays capped at
  100.
- **An unknown `bonus item`** logs one warning and pays no purse; the multiplier still applies.
- `loot` and `bonus amount` are enhanced on a large star. `loot` is read as a stat, so `loot: 3` is a bonus of +2:
  at `large star power: 2` a large-star Gilded creature drops five times over.
- The numbers are read at its death from the rules in force then, so a rule edit re-tunes a Gilded creature already
  walking about, the same as every other loot setting.
- Why this mutation pays when no other does is in `loot.md`, section 3.

### Tamed

A Gilded creature can be bred and a newborn can inherit it. A tamed one flees no one - its owner and the other
players are not its enemies - and it still glitters. It **drops ordinary loot, with no multiplier and no purse**: it
never ran, so there was no chase to reward, and a paying one would turn a breeding pen into an endless purse.

### Gilded in multiplayer

- **The flee is decided on the creature's owner**, where its AI runs. The movement and the alert replicate through
  the game's own sync, so every player sees it bolt, and a hand-over simply moves the decision.
- **The glitter is drawn locally on every client.** A dedicated server draws nothing.
- **The pay-out is decided once, on the dying creature's owner**, with the rest of its loot, and every player sees
  the one pile.

### Settings

`loot`, `bonus item`, `bonus amount`, `flee distance`, `glitter effect`. There is no cost field, because there is no
cost.

## Blinking, in full

Blinking makes a fight you have to watch. Every so often it vanishes and reappears behind its target, and a player
paying attention gets half a second's warning to turn round.

### When it blinks

- **Only in combat**: while its AI holds a living enemy as its target and is alerted.
- **The first blink of a fight comes after a random 25% to 75% of `every`** (30 seconds): somewhere between 7.5
  and 22.5 seconds in. It lands inside an ordinary fight rather than after it, and a pack of blinkers never blinks
  in unison.
- **Then one every `every` seconds of combat.** Only combat counts: a lull - a target switch, a moment out of
  sight - pauses the count. **Ten seconds out of combat end the fight**, and the next fight starts with a fresh first
  draw.
- **Never sooner than `every` after its last tell**, whichever machine sent it, so a creature that changes hands
  mid-fight never blinks twice in a row.
- **Only when it could**: not mid-attack, not staggered, not ridden, not latched on to anything, and only when it
  can see or hear its target. Otherwise it tries again half a second later.
- **A tell that fizzles (below) still counts as its blink**: the next one comes a full interval later.

### Where it lands

`distance` (4 metres) behind its target - opposite the way the target is facing - and if that spot will not do,
the same distance behind-left or behind-right, 40 degrees round. A spot must be:

- **Real ground**, solid and still, within 2 metres of the height of the target's feet. Never a roof over the
  target, never off a ledge or down a drop, never the deck of a moving ship.
- **Room for its body.** No wall, building piece, ship or other character where it would stand, and never on top
  of its target: a short `distance` is stretched until it clears the target's body.
- **In plain sight of the target.** Never the far side of a wall or a hill.
- **Somewhere its own AI would go.** Not water it avoids, not lava, not a fire it fears; a wild one never lands in
  an area the game keeps monsters out of.
- **Reachable on foot**, joined by walkable ground to where it stands, so it never reaches a roof or a walled base
  it could not have walked into.
- **A flyer keeps its height** above the ground or the water, capped at 12 metres, so one circling high never lands
  out of reach.

If none of the three spots will do, it tries again 3 seconds later, when the fight has moved.

### The tell

- `tell time` (0.5 seconds) before it arrives, the destination is marked: the `tell effect` (a burst with cyan
  trails and a cyan light, `vfx_WishbonePing`) and a chime at the same spot (`tell sound`,
  `sfx_WishbonePing_near`). The spot is behind the target, which for a player is at or under their own camera, so
  the marker alone is easy to miss. The chime tells them where to turn.
- **The tell fizzles** - the marker shows and nothing arrives - if, during it, the target dies, the creature's AI
  lets the target go, the target gets more than `distance` + 6 metres from the spot, or something steps onto the
  spot. A creature changing hands mid-tell fizzles too.

### The blink

- A puff of `blink effect` (`vfx_ghost_spawn`) where it vanishes and another where it appears.
- **It arrives standing still, facing its target** - not still running the way it was going, and not setting off
  back to where it came from.
- **It never visibly slides.** Other machines learn of the jump a moment after the puff, and a short jump would be
  eased across rather than snapped, so every machine hides its body and nameplate from the moment it vanishes until
  its position has landed at the destination: at least 0.15 seconds, so it reads as a vanishing, and never more
  than 1.5, so a lost update can never leave it invisible.

### Its cost

25% less health (`health: 0.75`). A cost, so never enhanced.

### Tamed ones and animals

- **A tamed Blinking creature blinks behind its enemies** - what it fights for its owner - never behind a player,
  and never while it is ridden.
- An animal's AI never holds a target, so an animal never blinks. It still pays the health cost.

### Blinking in multiplayer

- **Decided on the owner**, which holds the AI and the position: the combat clock, the spot, the tell, the blink and
  the move.
- **Drawn on every machine that holds the creature**, from one message on the creature's own network view - the
  tell, the two puffs and the hiding. A player three biomes away receives nothing.
- **The time of the last tell is on the creature's ZDO** (`ecr_blink_at`), on the shared clock, which is what stops
  a new owner blinking early.

### Settings

`health`, `every` (0 turns blinking off), `distance` (never less than 1 metre), `tell time`, `blink effect`,
`tell effect`, `tell sound`. None is enhanced on a large star.

## Relentless, in full

Relentless does not give up. Once it has a target it keeps that target, and the only ways out are distance and
killing it.

### It keeps its quarry

- **The first target the game gives it becomes its quarry.** It keeps it while the quarry is alive, still an enemy,
  targetable at all, and within `chase distance` (150 metres) of it.
- **It ignores everything else meanwhile.** The game normally re-picks a monster's target every few seconds, taking
  the nearest enemy it senses, so a closer player or one who just hit it would pull it away. A Relentless creature
  stays on its quarry: the friend who hits it to draw it off gets nothing.
- **It heads for where the quarry is now**, even out of sight and out of earshot, instead of walking to where it
  last saw the quarry and searching there. It still attacks only what it can see.
- **It loses the quarry** only when the quarry gets beyond `chase distance`, dies, stops being an enemy (tamed,
  befriended), becomes invisible to monsters (ghost or debug-fly mode), or leaves the world. The next target the
  game gives it becomes its new quarry.
- A hunted player's own stealth indicator shows that it has them.

### Every way of giving up, switched off

While it holds a quarry, none of the ways the game lets a hunter lose interest apply:

- 30 seconds without sensing its target;
- the leash that pulls it back once it has chased far from where it spawned;
- 60 seconds without attacking;
- running away when it is being hurt and cannot reach its target;
- the periodic target re-pick;
- a tame's leash to the player it follows or the spot it guards;
- falling asleep.

### What still works against it

Relentless, not unstoppable. Each of these still works, and each only interrupts the hunt - none loses it the
quarry, and it comes back for you afterwards:

- a fire it fears;
- pheromones;
- fleeing at low health, as its kind does;
- lava - a quarry standing in lava is waited out, for a creature that avoids lava;
- areas the game keeps monsters out of;
- the game's "cannot reach you, smash the building" switch: after 15 seconds unable to reach you, it breaks in.

### Sneaking does not hide you

While a Relentless creature's AI is thinking, every player counts as standing up: sneaking does not shorten how far
it sees you or how close you get before it turns alert, crouching behind low cover does not hide you from its eyes,
and moving while crouched is heard as walking is, at 15 metres. Running and fighting are as loud as they always
are. Noise-dampening gear does not lower that 15 metres. Every other creature, and the player's own stealth display,
see the real values.

### Water

- While it hunts, a creature that can swim and that water does not hurt **takes the swimming path of its own
  size**, so it follows its quarry into the water instead of stopping at the shore.
- One whose attacks do not work in water **keeps closing on its quarry** instead of wandering off.
- A creature the game never lets swim keeps walking the bottom where the ground allows, or waits at the shore.

### Despawning

While it hunts it does not leave at dawn, does not walk off when a raid is over, and a summoned one is not
unsummoned for being far from the player who summoned it. Once the hunt ends each of those applies again.
Everything else is unchanged: its zone unloading, a summoner logging out, summon limits, growing up, admin
commands.

### Its cost

**It is never faster than its base speed.** Its movement multiplier is capped at 1, so star speed and Mad's bonus
are cancelled, and a player can always outrun it past 150 metres. Anything that slows it, such as Devouring's bulk,
still does.

### Tamed ones and animals

- **A tamed Relentless creature** hunts its enemies - never a friend - wherever they run, up to 150 metres from
  itself, and **cannot be called back mid-hunt**: its leash to the player or its guard spot is off while it hunts.
- **Only a creature with the game's monster AI hunts.** An animal that only ever flees has no target to hold on to.
  The speed cap still applies to it.

### Relentless in multiplayer

- **Decided on the owner**, where the AI runs.
- **The quarry is on the creature's ZDO** (`ecr_quarry`). A creature that changes hands mid-chase has no target on
  its new owner; the new owner reads the quarry back at its next target re-pick - within about 2 seconds, up to 6
  when no player is within 50 metres - without the creature having to notice the player again.
- **Sneaking is judged on the owner**, from each player's crouch, movement and noise, which the game already shares
  with every machine.

### Settings

`chase distance` (150). `0` turns the hold off; the speed cap stays. Not enhanced.

## Console commands

**Nothing in this mod can be tested without these.** Mutations are rare by design - Splintering is about one
creature in forty in the Meadows - so verifying that any of it works by wandering around waiting to meet one is
not testing, it is luck. Every rule in this document needs a way to be produced on demand.

Three commands, all admin-gated on a server, all no-ops for a player without rights:

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns that creature with exactly those stars and mutations, ignoring every chance roll. Named mutations are applied whether or not the rules would ever have given them. |
| `elite inspect` | Prints the resolved mark of the creature under the crosshair: its stars, its mutations, the biome it was rolled in, and the values those produced after star power and any large-star enhancement. |
| `elite purge [radius]` | Removes loaded creatures that this mod has marked, without drops; with a radius in metres, only those within it. For clearing the mess after testing. |
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

The spec calls the 1.5 seconds between death and detonation "the window a player has to get clear". A window
nobody can see is not a window. The corpse must visibly and audibly announce what is about to happen for the
whole delay, at full strength to the end - swelling, glowing, hissing, whatever the chosen effect supports - and
the blast itself should reuse an existing explosion effect sized to the configured radius (drawn at 70% of that
size, see "Bloated, in full"), and be heard: it plays the game's own explosion sound (added 2026-09-26 at the
user's request).

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
  interrupt it is the whole encounter. Its kill tell is a sound with nothing drawn, and its
  nameplate carries an icon for each creature it has eaten - see "Devouring, in full".
- **Warding** should mark the moment it reflects, at the attacker, so the damage that just came back is
  attributable rather than mysterious. It is a sound only, and only when something was actually reflected: the
  Staff of Protection's bubble taking a blow, at 30% of the game's loudness, with nothing drawn (the Reflective
  aspect's tell is the same). It used to be a spark as well, but on every hit of a long fight the particles cost
  frames and the sound was too loud (2026-09-30, at the user's request).
- **Gilded** glitters gold at all times and lights the ground around it, readable well beyond nameplate range -
  see "Gilded, in full".
- **Blinking** marks its destination with a flash and a chime before it arrives, puffs where it vanishes and where
  it appears, and is hidden for the moment in between - see "Blinking, in full".
- **Mad**, **Plated**, **Leeching**, **Splintering** and **Relentless** need no effect of their own. Their star
  colours and names carry them, and adding more would clutter a creature that may be wearing four mutations at
  once.

### Rules for all of them

- Effects are **client-side and cosmetic**. They never decide damage, and a client with them turned off must
  take exactly the same damage as one with them on.
- **Each effect is drawn once, on each machine, and never networked.** A cosmetic copy must not register with
  the network in any way. Until 3.9.0 each one quietly did, and the game then built a second, full-size, networked
  copy of every effect on every client: every blast and bang came twice, and Bloated's warning smoke was left
  puffing at the death spot for good, once per machine. Effects drawn once can look lighter than those doubled
  ones did; that is the correct look.
- Effects must clean themselves up. A cloud that expires leaves nothing behind, and unloading an area destroys
  its effects with it.
- Brightness and density follow the existing per-player display settings, down to off, for players who need a
  quieter screen or are running on weaker hardware. Turning visuals off does **not** turn the hazard off - it
  makes the hazard unfair, which is the player's choice to make and must be worded that way in the setting's
  description.

## How many a creature gets, and which

**Every mutation rolls independently**, with its own chance, and that chance depends on the creature's star
count and the biome it spawned in - and on the kind of creature it is, when the rule file gives that kind rules of
its own (see "Rules for one creature" below). There is no slot table and no fixed ceiling: a creature that fails
every roll is plain, and one that passes several carries several.

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
they happen in an instant rather than being a property of the creature (the reflect tell is only heard now, but it
travels the same way). Those go over the game's own RPC bus,
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
  a busy server, which is exactly where it matters most. Its meal count and what it ate travel in its ZDO, so a new
  owner never gives it a fresh appetite, and every player sees the same eaten-creature icons.
- **Thieving's two-authority steal.** The robbed player's own client decides and removes the item (their
  Inventory is authoritative nowhere else); the creature's owner decides whether there is room and banks it. See
  `thieving.md` for the full split and the accepted risk window it documents.
- **A Blinking creature changing hands mid-fight.** A tell in flight fizzles, and the new owner reads the last
  tell's time from the ZDO, so it never blinks twice in a row.
- **A Relentless creature changing hands mid-chase.** The new owner reads the quarry from the ZDO and carries on
  the hunt, without the creature having to notice the player again.
- **A Bloated corpse owned by another machine.** The blast names the corpse and only its owner removes it; if the
  blast never comes, each machine's own corpse timer removes it a second after the fuse, loot and all.
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
that varies by biome**, because a flat config file cannot express a table of thirteen mutations across nine biomes
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

# Turn a mutation off everywhere, regardless of its chance curves below. No biome
# block can turn it back on. Creatures already spawned keep whatever they rolled -
# this only changes what the next one rolls.
mutations enabled:
  Mad: true
  Bloated: true
  Cloaked: true
  Splintering: true
  Leeching: true
  Warding: true
  Plated: true
  Miasmic: true
  Devouring: true
  Thieving: true
  Gilded: true
  Blinking: true
  Relentless: true

# How much stronger a mutation is when it sits on a large star (worth 5 stars)
# rather than a small one. Multiplies the mutation's BONUS, never its cost: a Mad
# creature on a large star is much faster but still has half health. 5 - matching
# the star's worth - is available but produces absurdities like a creature
# outrunning the player, so the default is deliberately lower.
large star power: 1

defaults:
  # What a star is worth. One entry per star count, so index 0 is an unstarred
  # creature and index 5 a five-star one. Every line is documented below.
  star power:
    growth:      [0.06, 0.10, 0.15, 0.20, 0.25, 0.30]
    hp:          [1,    1.4,  1.95, 2.6,  3.3,  4.0]
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
  # Gilded is the rarest of all: the loot goblin is rare on purpose.
  mutation chances:
    Devouring:   [0.6, 0.9, 1.2, 1.5, 1.8, 2.4]
    Gilded:      [0.4, 0.5, 0.7, 0.9, 1.1, 1.4]

  # How strong each mutation is. Named fields, not positional numbers - a row of
  # four bare decimals is exactly the thing server admins get wrong. Every field
  # is explained in the table below this code block.
  mutation power:
    Mad:         { move: 1.6, attack speed: 1.5, health: 0.5 }
    Bloated:     { health: 2.0, delay: 1.5, damage: 40, radius: 4, blast effect: fx_dynamite_explosion, blast sound: sfx_bombdynamite_explosion, warning effect: vfx_Smoked }
    Cloaked:     { reveal distance: 10, fade time: 0.5, fade margin: 1 }
    Splintering: { damage: 0.6, max generations: 0, max descendants: 0 }
    Leeching:    { regen: 0.5, lifesteal: 10, regen cap: 20, combat cooldown: 5 }
    Warding:     { reflect: 30, knockback: 4, max reflect: 7.5 }
    Plated:      { armour: 40, damage: 60, max reduction: 55 }
    Miasmic:     { cloud life: 6, cloud damage: 5, clouds per second: 1, cloud radius: 4, cloud effect: vfx_blob_death, body effect: vfx_blob_death }
    Devouring:   { move: 1, absorb health: 50, absorb damage: 25, slow per 100 health: 2, player threshold: 0.333, devour cooldown: 60, max prey health: 125, min meals: 1 }
    Thieving:    { max items: 1 }
    Gilded:      { loot: 3, bonus item: Coins, bonus amount: 20, flee distance: 30, glitter effect: vfx_Potion_stamina_medium }
    Blinking:    { health: 0.75, every: 30, distance: 4, tell time: 0.5, blink effect: vfx_ghost_spawn, tell effect: vfx_WishbonePing, tell sound: sfx_WishbonePing_near }
    Relentless:  { chase distance: 150 }

# Rules for one kind of creature, matched by prefab name. Besides the loot keys
# (`loot.md`), an entry takes `mutation chance`, `mutation chances` and
# `mutation power`, laid over the rules of whatever biome the creature is in.
creatures:
  # Big bodies are hard to hide: trolls and lox show themselves from further out.
  - match: Troll
    mutation power:
      Cloaked:     { reveal distance: 15 }
  - match: Lox
    mutation power:
      Cloaked:     { reveal distance: 15 }
  # Drakes are never Cloaked: an invisible flyer spitting frost from above is no fight.
  - match: Hatchling
    mutation chances:
      Cloaked:     [0]
#  - match: Deathsquito
#    mutation chances:
#      Cloaked:     [0]

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
    mutation chances:
      Thieving:    [6,  8,  11, 14, 17, 21]   # greydwarves already take things that are not theirs

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
      Thieving:    [8,  11, 14, 18, 22, 27]  # fulings, and a biome where you are carrying something worth taking

  - match: Mistlands
    star chances:    [22, 22, 22, 16, 11, 7]
    mutation chance: [6.5,  9,    12.5, 16,   20,   25]
    mutation chances:
      Cloaked:     [30, 40, 52, 64, 76, 90]  # the mist hides things already
      Devouring:   [2, 3, 4, 5, 6, 8]
      Thieving:    [7,  10, 13, 17, 21, 26]  # a thief you cannot see is the encounter this mutation is for

  - match: AshLands
    star chances:    [16, 22, 26, 21, 11, 4]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Bloated:     [38, 50, 64, 78, 92, 100]
      Splintering: [28, 37, 48, 58, 69, 82]

  - match: DeepNorth
    star chances:    [16, 22, 26, 21, 11, 4]
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

So a 4-star Bloated creature, with `hp[4] = 3.3` and Bloated `health: 2.0`:

```
1 + (3.3 - 1) + (2.0 - 1) = 4.3x health
```

not `3.3 x 2.0 = 6.6x`. Multiplying is what makes a four-mutation creature absurd rather than memorable, and
it is why the stacked extremes in this spec are survivable at all.

**A note on the arithmetic.** Adding the multipliers outright - `3.3 + 2.0` - looks simpler but double-counts
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
| Bloated | `delay` | Seconds between death and the blast - the window a player has to get clear. `1.7` by default. |
| Bloated | `damage` | Blunt damage at 0 stars, multiplied by `(1 + stars)`. |
| Bloated | `radius` | Blast radius in metres. |
| Bloated | `blast sound` | The vanilla sound prefab the blast goes off with (added 2026-09-26). |
| Cloaked | `reveal distance` | Metres at which it becomes visible. The nameplate hides in step. `10` by default; the default `creatures:` entries make it `15` for trolls and lox. |
| Cloaked | `fade time` | Seconds to phase in or out. `0` snaps. |
| Cloaked | `fade margin` | Extra metres before it fades back out, so it cannot strobe at the boundary. |
| Splintering | `damage` | Damage multiplier for a splintering creature. `0.6` = 40% weaker. |
| Splintering | `max generations` | Safety cap on cascade depth. `0` = unlimited, the default. |
| Splintering | `max descendants` | Safety cap on live descendants at once. `0` = unlimited. |
| Leeching | `regen` | Percent of max health regained per second. Never large-star enhanced. |
| Leeching | `regen cap` | Hard HP/s ceiling on a single regen tick, regardless of max health. Its cost. |
| Leeching | `combat cooldown` | Seconds since its last damage taken before regen resumes. |
| Leeching | `lifesteal` | Percent of damage dealt returned to it as health. |
| Warding | `reflect` | Percent of each hit's base damage returned to the attacker as blunt damage. The base is the health the hit actually took, after the creature's resistances and armour, with the sneak-attack and stagger bonuses taken back out, and never more than the health it had left. A hit that took no health reflects nothing. |
| Warding | `max reflect` | Ceiling on everything Warding reflects to one attacker in any one second (a sliding window), as a percent of the attacker's maximum health, applied before the attacker's own armour. It covers every hit and every Warding creature together, so many projectiles, chains or an explosion cost no more than one sword swing. Kept on each creature's owner, so creatures owned by different players each keep their own count. `7.5` by default; `0` = no cap. Never enhanced: it is a ceiling. |
| Warding | `knockback` | Force applied to whoever lands a melee hit on it. |
| Plated | `armour` | Percent of incoming damage cut at full health, falling to zero as it is hurt. |
| Plated | `max reduction` | Hard ceiling on that percent, so a large star's enhancement cannot approach invulnerability. |
| Plated | `damage` | Percent damage bonus at zero health, rising as it is hurt. |
| Miasmic | `cloud life` | Seconds a dropped cloud lasts before fading. |
| Miasmic | `cloud damage` | Strength of the Poison applied to a player standing in one. |
| Miasmic | `clouds per second` | How often it drops one while moving. |
| Miasmic | `cloud radius` | Metres. The visible cloud must match this exactly. |
| Miasmic | `cloud effect` | Name of the vanilla prefab cloned for the cloud. |
| Miasmic | `body effect` | Name of the vanilla poison visual worn on the creature itself, at all times. |
| Bloated | `blast effect` | Name of the vanilla prefab cloned for the explosion. |
| Bloated | `warning effect` | Name of the vanilla prefab played on the corpse during the delay. |
| Devouring | `move` | Base movement multiplier, before the slow. `1` by default; a tuning knob, never enhancement-scaled. |
| Devouring | `absorb health` | Percent of a victim's max health added to its own. `50` by default. |
| Devouring | `absorb damage` | Percent of a victim's damage added to its own. `25` by default. |
| Devouring | `slow per 100 health` | Percent movement speed lost per 100 absorbed health. Its cost. |
| Devouring | `player threshold` | Fraction of a player's max health its per-hit damage must reach before it hunts players for good. `0.333` = a third. |
| Devouring | `devour cooldown` | Seconds before it can devour again after a meal. `60` by default. |
| Devouring | `max prey health` | The most current health a creature may have for it to hunt and eat it, as a percent of its own current health. `125` by default; `0` lifts the limit. |
| Devouring | `min meals` | The fewest creatures it eats in its life, whatever its stars: it eats one per star, never fewer than this, then is sated. `1` by default. |
| Thieving | `max items` | The fewest items one creature holds, whatever its stars: it holds one per star, never fewer than this and never more than 8. `1` by default. See `thieving.md`. |
| Gilded | `loot` | Multiplier on its drops, after the loot mode, in every mode. `3` by default. Read as a stat, so on a large star its bonus above 1 is enhanced. |
| Gilded | `bonus item` | Item prefab of the purse it drops on top. `Coins` by default. An unknown name pays no purse and logs once. |
| Gilded | `bonus amount` | How many of the bonus item per star plus one: `20` gives 20 at no stars and 100 at four. Never multiplied by anything else; capped at 100. |
| Gilded | `flee distance` | Metres within which it runs from a player it can see. `30` by default. `0` stops it running from sight; a hit still sends it running. |
| Gilded | `glitter effect` | Name of the vanilla sparkle it wears. `vfx_Potion_stamina_medium` by default. |
| Blinking | `health` | Max health multiplier. `0.75` = 25% less. This is its cost. |
| Blinking | `every` | Seconds of combat between blinks; the first of a fight comes after a random 25-75% of it. `30` by default; `0` turns blinking off. |
| Blinking | `distance` | Metres behind its target it lands. `4` by default, never less than 1. |
| Blinking | `tell time` | Seconds the destination is marked before it arrives. `0.5` by default. |
| Blinking | `blink effect` | Name of the vanilla puff drawn where it vanishes and where it appears. |
| Blinking | `tell effect` | Name of the vanilla effect that marks the destination. |
| Blinking | `tell sound` | Name of the vanilla sound played at the destination with the tell. |
| Relentless | `chase distance` | Metres within which it keeps its quarry. `150` by default; `0` turns the hold off (its speed cap stays). |

### What these defaults actually produce

Two things decide whether a creature is mutated: its **star count**, and a per-mutation **chance** roll. With
thirteen mutations, about three Meadows creatures in ten are something, rising to about four in five in the Ash
Lands, at world tier 0:

| Biome | Mutated | Has a large star |
| --- | --- | --- |
| Meadows | 30% | 1% |
| Black Forest | 40% | 2% |
| Swamp | 49% | 3% |
| Mountain | 58% | 4% |
| Plains | 66% | 5% |
| Mistlands | 73% | 7% |
| Ash Lands | 79% | 4% |
| Ocean | 35% | 1% |

These count each biome's `mutation chance` curve for every mutation, with Devouring and Gilded on their own rarer
curves. The per-mutation curves a biome adds on top - Miasmic in the Swamp, Bloated in the Ash Lands - raise its
figure further. With ten mutations, before 3.9.0, the same count gave a quarter in the Meadows and about three
quarters in the Ash Lands.

**It stops short of everything on purpose.** An unmutated creature has to stay a real possibility even in the
worst biome, or "mutated" stops meaning anything and the ordinary ones stop being a relief.

**Large stars exist everywhere, barely.** One Meadows creature in a hundred has five stars, rising to four in a
hundred in the Ash Lands. Even the gentlest biome can produce one, and now that `large star power` defaults to
`1` a large star is a size-and-damage event rather than a mutation-runaway one.

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
- **`mutations enabled: <name>: false` turns a mutation off everywhere.** It is read once at the root, before any
  `defaults` or biome overlay runs, so no biome block has a key that could turn it back on.
- **The file reloads on edit**, while playing, without a restart.
- **A rule change, this switch included, never touches a creature already spawned.** Traits are rolled once by
  the owner and stored in the ZDO; a reload only changes what the *next* creature rolls, the same rule
  `world-tiers.md` states for tier changes.
- **The file is written on first run** with every value at its default and every comment in place, so a server
  owner always has a complete, documented file to edit rather than a blank one.
- **An existing file keeps its own numbers.** A file written by an older version is never rewritten: one from
  before 3.9.0 still says `delay: 1.0` and `reveal distance: 6` and has no `creatures:` entries for trolls, lox or
  drakes until someone edits it or deletes it so a fresh one is written; one from 3.9.0 to 3.12.0 still says
  `delay: 2.0`. A mutation the file does not mention at all - Gilded, Blinking and Relentless in such a file - takes
  its built-in defaults, and Gilded stays rare because its curve is part of the built-in `defaults`. A field the file
  does not mention does the same: an older file with no `max prey health` or `min meals` gets 125 and 1.

### Rules for one creature

A `creatures:` entry, matched by **prefab name** in any case, may carry the three mutation keys a biome block
takes - `mutation chance`, `mutation chances` and `mutation power` - with the same syntax and the same validation.
The same entries carry the per-creature loot rules (`loot.md`), and one entry may hold both.

- **They change that kind of creature only, in whatever biome it is in.** They are laid over the rules of the biome
  it spawned in, and everything the entry does not name still comes from that biome.
- **`mutation power` merges field by field.** `Cloaked: { reveal distance: 15 }` changes the reveal distance and
  keeps the biome's fade time and margin.
- **`mutation chances` replaces each named mutation's curve**, and `[0]` stops that mutation for the creature.
- **`mutation chance` replaces only the creature's default curve.** Any mutation with its own curve - in the biome
  block, or in `defaults` like Devouring and Gilded - keeps that curve. To stop one mutation, name it under
  `mutation chances`.
- **`mutations enabled` still wins**, and the world tier's mutation boost still multiplies the creature's curves
  (a `0` stays `0`).
- **Bosses ignore these entries**; they roll on the boss table. **Breeding ignores them too**: a newborn inherits
  by the breeding rules, not by its kind's chances.
- **Two entries with the same `match` merge** rather than the later silently replacing the earlier: later keys win
  and drop rows add up. Uncommenting an example for a creature that already has an entry never loses the one above
  it.
- **The shipped file has three**: Troll and Lox with `mutation power: Cloaked: { reveal distance: 15 }`, because
  big bodies are hard to hide, and Hatchling (the drake) with `mutation chances: Cloaked: [0]`, because an invisible
  flyer spitting frost from above is no fight. A commented Deathsquito example shows how to end Cloaked mosquitoes
  in the Plains.

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
| Thieving | Violet |
| Gilded | Pale gold `#FFE066` |
| Blinking | Cyan `#29E0E0` |
| Relentless | Orange `#FF7F24` |

**Cloaked is blue and Warding is dark blue**, so those two must be clearly separable on a small star: keep
Cloaked a bright, light blue and Warding genuinely dark, near navy. Judge it at a small star's size against
snow and against night, not on a colour swatch. The same care holds for the three added in 3.9.0: Gilded's pale
gold is lighter than Plated's deep yellow, and Blinking's cyan is greener than Cloaked's sky blue.

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
| 29 | five large, four small | 9 |
| 49 | nine large, four small | 13 - all of them |

Showing all thirteen on the stars needs 49 stars - the widest row there is. That is far outside any sane
configuration and does not matter: a server that sets a 49-star ceiling has asked for a screen-wide nameplate and
can have one. At the defaults the cap is 1 and the question never arises.

### A mutation on a large star is enhanced

A large star is worth five. A mutation sitting on one is correspondingly **stronger** - that is what makes a
high-star creature different in kind rather than just carrying more labels.

**`large star power` multiplies the mutation's gain, and only its gain.** Default `1` - no extra multiplier at
all. A large star is already worth five ordinary ones through `star power` alone; multiplying mutation gains on
top of that as well is what let a large-star creature become unkillable. Raise it if a server wants large stars
to hit harder on their mutations too, understanding that is a deliberate escalation above the shipped default.

```yaml
  # How much stronger a mutation is when it sits on a large star (worth 5 stars)
  # rather than a small one. Multiplies the mutation's BONUS, never its cost:
  # a Mad creature on a large star is much faster but still has half health.
  # 1 is the default - a large star's own star power is already the reward: no
  # extra multiplier on top of it. Raising this stacks another escalation on
  # creatures that are already the rarest and biggest a biome produces.
  large star power: 1
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
| Leeching | `lifesteal` | `regen` and `regen cap` (a cost-like ceiling) - a huge-health creature must stay beatable on regen alone; `combat cooldown` |
| Warding | `reflect`, `knockback` | `max reflect` - a ceiling, like Plated's, must not itself scale |
| Plated | `armour`, `damage` | `max reduction` - the hard cap must not itself scale, or it stops being a cap |
| Miasmic | `cloud damage`, `clouds per second` | `cloud life`, `cloud radius` |
| Devouring | `absorb health`, `absorb damage` | `slow per 100 health` (a cost), `player threshold`, `max prey health`, `min meals` (limits; the one meal per star is not enhanced, stars already count) |
| Thieving | `max items` | - (the one item per star is not enhanced; stars already count) |
| Gilded | `loot`, `bonus amount` | `flee distance`, `bonus item`, `glitter effect` |
| Blinking | - | all of it; `health` is a cost |
| Relentless | - | `chase distance`, and its speed cap is a cost |

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

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The set

- [ ] The thirteen, as specified
- [ ] Devouring in full, including its multiplayer section
- [ ] Devouring's size limit: it neither hunts as prey nor devours a creature with more than `max prey health`
      (125%) of its own current health; a blow that lands on one is an ordinary hit; attacked by one, it fights back;
      a wounded devourer's reach shrinks
- [ ] Devouring's appetite: a 2-star devourer eats two and then no more, an unstarred one eats one; once sated it
      fights like any creature of its kind (attacks players on sight) unless it already hunts players; the count
      survives a reload, a relog and an ownership hand-over (`elite inspect` shows "devoured N of M")
- [ ] One swing that catches two prey at once still banks no more meals than the allowance
- [ ] Bloated in full, including which corpse in multiplayer and the corpse going with the blast
- [ ] Bloated's fuse is 1.7 seconds on a fresh rule file (an older file keeps its own `delay`)
- [ ] Miasmic in full - both poison paths, the trail, and harming players only
- [ ] Splintering in full, including tamed parents splitting into tamed copies
- [ ] Gilded in full - the glitter, never attacking a player, the flee, the pay-out, tamed ones paying nothing extra
- [ ] Blinking in full - the combat clock, the spot rules, the tell, the hidden landing, hand-over
- [ ] Relentless in full - the quarry, the give-ups switched off, sneaking, water, despawning, hand-over
- [ ] How many a creature gets, and which
- [ ] Rules for one creature: per-creature mutation keys, merging entries, the three shipped entries

## Visuals

- [ ] Miasmic clouds visible for their whole life
- [ ] Bloated has a tell during its delay
- [ ] Devouring's kill tell and Warding's reflect tell are heard with nothing drawn (no particles, light or camera
      shake), on the host and on a second client watching; the reflect tell clearly quieter than before
- [ ] A Devouring creature's nameplate shows one icon per creature eaten: the trophy, or the horned monster head for
      one with no trophy (greyling, hen); the same icons on every client; meals sit left of a Thieving pouch; hidden
      with a Cloaked plate; `Show devoured creatures` and `Devoured creature icon size` honoured
- [ ] The rules for all of them in section "Rules for all of them"

## Multiplayer

- [ ] Rolled once by the owner, stored in the ZDO, read by everyone
- [ ] The wire stays quiet
- [ ] Every client sees what section "What every client must see" lists
- [ ] Damage is the owner's decision; drawing is everyone's
- [ ] Every case in "Cases that must work"

## Verification

- [ ] Tested in a live multiplayer session

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-17 | Thieving added as the tenth mutation: table, star colour, large-star enhancement, `mutation power` block, Splintering interaction, and a "cases that must work" line for its two-authority steal. See `thieving.md`. | - |
| 2026-09-27 | 3.9.0: Gilded, Blinking and Relentless added with their "in full" sections, colours, fields and enhancement rows; per-creature mutation rules and the shipped Troll, Lox and Hatchling entries; Bloated's two-second fuse, full-strength smoke on the body, smaller blast and corpse bursting with it; Cloaked at 10 m; Warding's base hit and `max reflect`; tamed Splintering copies; effects drawn once, locally; the embedded rule file and the mutated-share table brought up to date. Built, not tested in game. | - |
| 2026-09-28 | Devouring eats only prey with at most `max prey health` (125%) of its own current health and one creature per star (`min meals` 1 as the floor), then is sated; what it ate is kept on its ZDO (`ecr_dev_meals`) and drawn on its nameplate as trophies or the game's monster head, beside a Thieving pouch through a shared icon row; its kill tell is drawn at a fifth of the size, every part (LocalEffects `FlashScaled`); Bloated's fuse 1.7 s. Built, not tested in game. | - |
| 2026-09-30 | The Warding and Reflective reflect tell and Devouring's kill tell are only heard, nothing drawn (LocalEffects `SoundOnly`): the reflect tell pinned to `fx_StaffShield_Hit` at 30% volume, the kill tell `fx_aspect_death`'s sound at full volume. At the user's request: the particles cost frames and the reflect sound was too loud. Built, not tested in game. | - |
| 2026-10-03 | Juggernaut and Screecher added at the user's request (13-14; colours iron grey, pink); Howling was built too and removed the same evening after a try in game ("way too crazy"); Gilded's curve halved; large creatures (`Traits/BodySize.cs`: capsule radius >= 1 m or length >= 3.5 m, and 300+ base health) never roll or inherit Gilded or Relentless and are never prey; bosses never prey; `max prey health` 125 -> 100; Bloated's fuse 1.5 s. Judgement calls: Juggernaut shows "Unstoppable" where it would have staggered and keeps its own attack recoil; Screecher triggers on one hit taking 15% of its max health from any source, deafens only players it is hostile to (AudioListener at 4%, a faint ringing, a "Ringing ears" status), blocks Elemental and Blood Magic weapons. Built by parallel agents, not tested in game. | - |

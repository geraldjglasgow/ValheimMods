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
Five more aspects - Adaptive, Fixated, Stormbound, Gravitic and Colossal - were added at the user's request for
3.9.0, built and not tested in game. Nothing is ticked below until it has been seen working on a dedicated server.

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
Echoing, Draining, Sundering, Unbound), none of which was ever built. The last five rows were added on 2026-09-27
at the user's request, for 3.9.0; the words in their rows are the user's.

| Aspect | What changes |
| --- | --- |
| Reflective | Direct damage you deal to the boss is dealt to you as well |
| Shielded | Takes 30% less damage from arrows and bolts |
| Mending | Regenerates its health far faster than the game's own trickle |
| Summoner | Calls strong creatures each time it loses 33% of its maximum health |
| Elementalist | Deals 20% more elemental damage |
| Enraged | Deals 20% more physical damage |
| Twin | Comes as two bosses sharing one health pool, each with 25% less health and damage |
| Phantom | Splits off weak copies of itself at 66% and 33% health, one per player online: half its damage, 25 health per world tier, no drops, no body |
| Adaptive | Takes 50% less of whichever damage type hit it most in the last 15 seconds, and its glow changes colour to show which one. You have to swap weapons, or the group has to spread across damage types |
| Fixated | Marks one player, with an icon over their head and a chat line. Hits the marked player 50% harder and everyone else 30% softer. Every 30 seconds the mark moves to whoever hurt it most. Solo, you are always marked |
| Stormbound | Every 20 seconds a glowing circle appears under each player, and 2 seconds later lightning strikes it, lightly staggers you and does a little damage |
| Gravitic | Every 20 seconds it roars and pulls every player within 30 m toward it for 1.5 seconds, then slams. Melee players get a free gap-closer and archers lose their distance |
| Colossal | 40% bigger, 15% more health, 15% slower. Its heavy attacks send out a shockwave that knocks players down |

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

**Phantom.** Each time the boss's health falls past a split mark - **66%** and then **33%** of its maximum by
default - copies of it appear in a ring around it: same prefab, same stars, same size, same name, **one for each
player online** (so a player alone faces one at each split, a group of four faces four). Each copy deals **50%
less** damage and has **25 health for each world tier**, tier 0 counting as 1, however many stars the boss has. A
hit that crosses both marks brings both splits at once, and healing back above a mark never re-arms it. A copy
drops nothing and leaves no body - it vanishes where it falls, with a puff of smoke. Its death never counts as the
boss's: no boss-defeated key, no progression, no trophy. **When the boss dies its remaining copies vanish with it**
(a judgement call: they are the boss's phantoms, not creatures of their own). The copies never split themselves.

The game draws every boss health bar in the same place, so the copies' bars would sit on top of the boss's and only
one would show. **The boss keeps its full-size bar; each copy's bar is a quarter of its width and a sliver of its
height, in a row under it** - four copies together are as long as the boss bar, a fifth starts a second row - each
with its name in small type above it.

Changed on 2026-09-26 at the user's request. Until then the four copies (100 health each) came the moment the boss
appeared, and their bars covered the boss's.

**Adaptive.** It learns what you are hitting it with. The boss takes **50% less** (`resist`) of whichever damage
type hit it most in the last **15 seconds** (`window`), and **glows that type's colour**, so the answer is readable:
swap weapons, or spread the group across damage types so no one type dominates.

- **What it counts.** The eight types a player chooses between by picking a weapon: blunt, slash, pierce, fire,
  frost, lightning, poison and spirit. Chop and pickaxe (tool damage) and untyped true damage are ignored. Each hit
  is counted as the boss's own resistances leave it and before the sneak-attack and stagger bonuses, so a type the
  boss already shrugs off can never become its adapted type. Only hits from players and their tames teach it.
- **What it cuts.** That type's part of every hit, from any source, by `resist` percent. Fire, poison and spirit are
  cut before they turn into burning and poison ticks, so the ticks are smaller too. The other types in the same hit
  land untouched.
- **When it changes.** A hit is counted before its own cut, and a change of type takes effect from the next hit, so
  no hit is cut by the type it tipped the balance to. A tie keeps the type it already resists, so the colour never
  flickers between two equal types. With nothing in the window - nobody has hit it for 15 seconds - it resists
  nothing and goes dark. `window: 0` means it never adapts.
- **The glow**, on every client: the game's health-potion aura, recoloured, around the boss, and a point light
  pooling the same colour over the boss and the ground, wider for a bigger boss. Blunt yellow, slash pink, pierce
  teal, fire orange-red, frost ice blue, lightning violet, poison green, spirit pale white. A change blends across in
  about half a second. It follows the player's effect density; at 0 nothing is drawn, and `elite inspect` still
  names the type ("resisting now: fire").
- *Multiplayer.* The boss's owner keeps the window, decides the type and writes it to the boss's ZDO
  (`ecr_adaptive`) only when it changes; every client colours the glow from there. The cut happens on the boss's
  owner, where hits on it resolve. The window itself is never sent: a machine that takes the boss over starts a
  fresh one holding a token of the stored type, so the boss keeps resisting and glowing as it was until the fight
  says otherwise.

**Fixated.** It picks one player to hate. The marked player takes **50% more** (`marked bonus`) from its hits, and
everyone else on the players' side - other players and tames - takes **30% less** (`others less`). Wild creatures
caught in its attacks, and every hit while nobody is marked, are unchanged.

- **Who is in the fight**: living players within 60 m of the boss.
- **Alone**, that player is marked at once and stays marked.
- **In a group**, the first mark goes to whoever has hurt it most, else the player it is targeting; until one of
  them exists nobody is marked. Every `every` (30) seconds the mark moves to whoever hurt it most **in those 30
  seconds alone**, the marked player included, so players can take turns carrying it. If nobody hurt it in that
  time, the mark stays.
- **It moves at once** when the marked player dies, logs out or goes beyond 60 m: to whoever hurt it most so far in
  the current period, else its target, else the nearest player in the fight. With nobody left in the fight the mark
  clears. A player who dies is never still marked when they come back.
- **The damage** it goes by is the boss damage board's own count: health actually taken off the boss, per player,
  players only.
- **A red eye** - the game's own "it has noticed you" eye, turned red - floats over the marked player's head for
  every player within 100 m of the boss, the marked player included. It hides with the HUD.
- **A chat line** when the mark lands or moves: "Bonemass fixes on Gerald!", or "Bonemass fixes on you!" for the
  marked player, for players within 100 m. It is not repeated for someone who walks up to a fight already under way;
  the eye shows them who is marked.
- `elite inspect` prints "marked: Gerald (12 s ago)", or "marked: nobody yet".
- *Multiplayer.* The boss's owner decides the mark and writes the marked player's character and when it landed to
  the boss's ZDO (`ecr_fixated`, `ecr_fixated_at`). The hit is scaled on the victim's owner - a player's own machine
  - from that mark. Every client draws the eye and says the line from the same ZDO, so nothing else is sent. A new
  owner starts a fresh period from the damage count it finds.

**Stormbound.** It calls lightning down on each player at once, and gives them two seconds to move.

- **When**: while the boss is awake and alerted and a living player is within `range` (40 m, measured along the
  ground, so a flying Moder still counts the players below). The first storm comes a full `every` (20 s) into the
  fight, then one every `every` seconds of fight. A lull pauses the count; 10 seconds out of the fight resets it.
- **The tell**: a glowing blue ring of `radius` (2.5 m) with a pulsing blue light appears on whatever each player in
  range stands on - the ground, a floor, a ship's deck, or the water's surface for a swimmer. It does not follow
  them. It is the warning, so it is drawn even at effect density 0.
- **The strike**: `tell time` (2 s) later each ring goes out and a blue bolt (the Himminafl axe's lightning, with a
  mild camera shake within 7 m) lands on it, with one thunderclap for the whole storm.
- **Who is hit**: a player still inside a circle - within its radius across the ground, not far above or below it -
  takes `damage` (8%) of their own maximum health as lightning, and staggers. Lightning resistance and a protection
  bubble reduce or absorb it; armour does not. A strike that deals nothing brings no stagger, and there is none while
  swimming or seated. A dodge roll's invincible moment avoids it; a raised shield does not, because the bolt comes
  from above. At most one strike per player per storm, however many circles overlap. Only players: tames and
  creatures are never struck.
- **Edge cases**: the boss dying before the lightning falls breaks the storm, and nothing falls. A player who
  arrives mid-storm sees the rest of it, but one with less than about a third of a second of warning left sits it
  out: nobody is struck by a circle they had no time to see. A ghost, a debug flyer or a teleporting player gets no
  circle.
- *Multiplayer.* The boss's owner decides when and where, and writes the storm - when the lightning falls, on the
  shared clock, and each circle's centre - to the boss's ZDO (`ecr_storm_at`, `ecr_storm`). Every client draws the
  circles and the strikes from there, so a late arrival sees a storm under way, and each player is judged on their
  own machine against where they see themselves, so a dodge on their screen is a dodge. A new owner reads when the
  last storm fell and keeps the rhythm.
- *Limits*: the tell is never under 0.5 s, `every` is always at least a second longer than the tell (0 turns the
  storms off), the radius is at least 0.5 m and the damage 0-100%.

**Gravitic.** It drags you in: melee players get a free gap-closer, and archers lose their distance.

- **When**: while the boss is alert and a player is within `range` (30 m). Every `every` (20) seconds of fight, the
  first a full interval in; a lull pauses the count and 10 seconds out resets it. `every` is never shorter than the
  pull time plus a second.
- **The roar**: the boss's own alert cry, a pulse at its feet and a slight tremble of the camera.
- **The pull**: every player within `range` of the boss as it roars is dragged toward it at `pull speed` (6 m/s) for
  `pull time` (1.5 s) - about 9 m - stopping a step short of its body, however big it is. Across the ground only:
  under a flying Moder it draws players to the ground beneath her. It moves each player through the game's own
  knockback, so walls and rocks stop them; a dodge roll pauses it, a jump briefly escapes it, and running toward the
  boss is never slowed. Never pulled: seated, in bed, at a helm, riding, standing on a ship's deck, teleporting,
  dead.
- **The slam**: when the pull ends, a ground-slam burst, a thud and a harder shake. Anyone within `slam radius`
  (6 m) of its body loses `slam damage` (10%) of their maximum health and staggers, turned to face it. The damage is
  untyped: armour and resistances do not reduce it and blocking does not stop it. A dodge roll timed through the
  slam avoids the damage and the stagger both. The slam has no attacker, so a death from it names no killer.
- **Edge cases**: the boss dying mid-pull stops the pull, and there is no slam. A player who meets the boss mid-cycle
  never heard that roar and sits the cycle out.
- *Multiplayer.* The owner keeps only the rhythm and sends one message, the roar, through the boss's own network
  view (`ecr_gravitic`), carrying the cycle's numbers so every machine pulls and slams by the same ones. Each machine
  then runs the cycle for itself - the roar, the pull on its own player, the slam and the judgement of its own player
  - because a player's body lives on their own client, and at the end of a pull the network delay is exactly what
  would decide inside or out. The last roar's time is on the boss's ZDO (`ecr_gravity_at`), so a hand-over neither
  roars twice nor starts the count over.
- *Limits*: the pull time is kept between 0.1 and 10 s and the slam damage between 0 and 100%.

**Colossal.** Bigger, tougher, slower, and its heaviest blows shake the ground.

- **40% bigger** (`bigger`), on top of its star growth, on every machine, growing from its feet; its body and
  collider grow with it. **15% more health** (`more health`), on its starred maximum. **15% slower** (`slower`):
  its movement, acceleration, turning and flight; its attacks are not slowed. Its attack reach does not grow - the game
  measures a blow's reach in the world, not on the body - so its reach past its own body is a little shorter than a
  normal boss's, as with any starred creature.
- **Its corpse keeps its size.** A grown boss's ragdoll would otherwise shrink back to normal as it fell. Eikthyr,
  the Elder, Yagluth and the Fader leave ragdolls; Bonemass, Moder and the Queen leave only a death effect.
- **Its heavy blows send a shockwave.** Heavy means an area attack that harms players, or a melee blow whose largest
  damage type is blunt (a tie counts; chop and pickaxe are ignored). Projectiles, taunts, breath, summons and spit
  never are. Among the vanilla bosses: Eikthyr's stomp, the Elder's stomp, Bonemass's punch, Yagluth's nova, the
  Queen's burst of stabbing legs, the Fader's tail spin, the Hive's punch, and nearly every swing of the Frozen King's
  chains in phases 1 and 3 (slams, whirl, punch burst, sweeps, flurry, rush); Moder and TheHive have none, so for
  them Colossal is only bigger and slower. A modded boss follows the same rule, and a boss in the air sends none.
- **At most one shockwave every 5 seconds per boss**, so a boss that swings heavily every few seconds cannot keep
  players on the floor.
- **The shockwave**: at the blow's impact point (an area attack's centre, or the end of a melee sweep, at the height
  of its feet), the Elder's stomp wave, grown so its edge lands on the radius, and a deep crash of splitting rock.
  Every player on the ground within `shockwave radius` (8 m) across the ground, and within half that up or down, is
  knocked down: the game's stagger, turned to face the blow, and a shove of about 2.5 m. No damage - the blow itself
  does that. A dodge roll through it avoids it and counts as a perfect dodge, and a player in the air as it passes is
  missed. Blocking does not help. Exempt: swimming, seated, riding, on a ship, teleporting, dead, already staggering.
- *Multiplayer.* The game runs a boss's attacks on its owner, so that is where each landed blow is weighed; a heavy
  one sends one message through the boss's own network view (`ecr_colossal_shock`: the impact point and the radius).
  Each machine draws the wave and judges its own player. A swing cut short by a hand-over hits nothing and shakes
  nothing. The corpse is grown on the owner and its size written on the ragdoll's own ZDO (`ecr_corpse_scale`), so
  every other machine grows its copy as it appears.
- *Limits*: `shockwave radius` is capped at 30 m, and 0 turns the shockwave off.

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
| Stormbound | 1.2x |
| Colossal | 1.2x |
| Adaptive | 1.3x |
| Fixated | 1.3x |
| Gravitic | 1.3x |

**This ranking is a judgement made at a desk** - thirteen fights ranked by someone who has fought none of them. It
is one editable table, so reordering it after a few real fights costs nothing, and it is first on the list of things
to revisit. Of the five added in 3.9.0 only Gravitic's 1.3x came from the user; Stormbound's and Colossal's 1.2x and
Adaptive's and Fixated's 1.3x are judgement calls.

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
  it meets one, and draws its small health bar. The splits already made are counted in the boss's ZDO, like
  Summoner's waves, and the copy count comes from the player list the server sends every client, so whichever
  machine owns the boss spawns the same number. The boss's owner sends the vanish to each copy's owner when the
  boss dies.
- **Damage changes** (Enraged, Elementalist, Shielded, Twin's and Phantom's reduced damage, Fixated's mark,
  Adaptive's cut) are applied where every hit is resolved - on the victim's owner - from the aspect in the attacker's
  or victim's ZDO.
- **Adaptive's resisted type, Fixated's mark and Stormbound's storm live in the boss's ZDO**, written by its owner
  and read by every client, so no message is sent for them and a hand-over or a late arrival reads the same state.
- **Stormbound's strike, Gravitic's pull and slam and Colossal's knockdown are judged on each player's own
  machine**, which is the only one that can move that player's body and the one that knows exactly where they are and
  whether they are mid-roll. Gravitic's roar and Colossal's shockwave are single messages through the boss's own
  network view, so only machines that hold the boss receive them.
- **The five new aspects never act on a Phantom copy**; Colossal's size, health and slowness come from the boss's own
  stats on every machine, and its corpse's size from the ragdoll's ZDO.
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
      none: 30
      Reflective: 10
      ...
      Colossal: 10
    loot:                    # drop multiplier per aspect
      none: 1
      Summoner: 1.5
      ...
      Stormbound: 1.2
      Colossal: 1.2
      Adaptive: 1.3
      Fixated: 1.3
      Gravitic: 1.3
    power:
      Reflective:   { reflect: 15 }
      Shielded:     { arrow reduction: 30 }
      Mending:      { regen: 0.3 }
      Summoner:     { every: 33, count: 2, stars: 2 }
      Elementalist: { elemental bonus: 20 }
      Enraged:      { physical bonus: 20 }
      Twin:         { less health: 25, less damage: 25 }
      Phantom:      { split at: [66, 33], per player: 1, health per tier: 25, less damage: 50 }
      Adaptive:     { resist: 50, window: 15 }
      Fixated:      { marked bonus: 50, others less: 30, every: 30 }
      Stormbound:   { every: 20, tell time: 2, radius: 2.5, damage: 8, range: 40 }
      Gravitic:     { every: 20, range: 30, pull time: 1.5, pull speed: 6, slam radius: 6, slam damage: 10 }
      Colossal:     { bigger: 40, more health: 15, slower: 15, shockwave radius: 8 }
    per boss:                # matched by prefab name
      - match: Eikthyr
        summons: [Boar, Neck]
      - match: Bonemass
        aspects: [none, Reflective, Twin]   # optional: this boss's rotation
        summons: [Draugr_Elite, BlobElite]
```

- `enabled` is the feature's off switch. `stars` and `aspects` are independent: stars off with aspects on gives
  unstarred bosses with aspects, and both off leaves every boss exactly as the game ships it.
- `chances` are weights, not percentages: by default 30 for the plain fight and 10 for each of the thirteen aspects,
  160 in all, so about one fight in five (30 in 160) is plain. `none` was 20 until 3.9.0 and was raised to keep that
  share with five more aspects. An aspect missing from the list keeps its built-in weight of 10; set it to `0` to
  take it out of the rotation. So an older rule file still rolls the five new aspects at 10 each, and keeps its own
  `none: 20`.
- `per boss` narrows one boss's rotation (`none` stays in unless its weight is 0) and sets what Summoner calls.
- **Elite Creatures Pack's kraken** (`ECP_Kraken`) never rolls Twin or Phantom (decided with the user, 2026-09-28: it
  holds one ship with one health bar, a Twin put two on one Karve in the first test, and Phantom copies would swarm the
  deck). It is a built-in `per boss` entry, so a rule file written before it still has it, and a new rule file lists
  it; an entry of the server's own for `ECP_Kraken` with `aspects:` replaces it. Only the prefab name crosses: without
  that mod the entry is never matched.
- `elite inspect` reports a boss's aspect, what it does with the live numbers, its loot multiplier, for a Twin or a
  Phantom copy whom it is tied to, for an Adaptive boss the type it resists now ("resisting now: fire"), and for a
  Fixated boss whom it has marked and how long ago ("marked: Gerald (12 s ago)"). `elite spawn <boss> <stars>
  <aspect>` makes exactly that fight, any of the thirteen included.
- The five new aspects' effect and sound prefabs are code constants rather than rule fields: the `aspects:` block
  holds numbers only.

---

# 6. Decisions

Settled on 2026-09-26 with the user:

1. **The set** is the eight in section 1 (thirteen since 3.9.0, below). The earlier ten candidate names are retired,
   which also frees the word `Shifting` that `attunements.md` had stepped around.
2. **Twin: both bosses drop full loot**, trophy included. Twin's multiplier is therefore 1.0 per boss.
3. **Altars shift every in-game hour** (75 real seconds by default), as first specified.
4. **Bosses without an altar roll their aspect when they first appear.**

Judgement calls made while building, each a default in the rule file:

- Reflective returns 15% as true damage; Mending heals 0.3% per second; Summoner calls 2 two-star creatures; the
  per-boss summon lists; the loot ranking. None of these numbers came with the request.
- Aspect percentages multiply the starred boss rather than adding to the star line (section 1).
- "Arrows" means bows and crossbows.
- Phantom copies vanish when the boss dies; summoned creatures do not.
- Phantom (changed with the user on 2026-09-26): splits at 66% and 33% (a list, so a server can add or remove
  marks), one copy per player online at each split, 25 health per world tier with tier 0 counting as 1, and small
  copy health bars under the boss's. An older rule file's `copies` and `health` lines are warned about and ignored.

Added with the user on 2026-09-27, for 3.9.0: **Adaptive, Fixated, Stormbound, Gravitic and Colossal**, as the user
worded them in section 1, with Adaptive's 50% and 15 s, Fixated's 50%, 30% and 30 s, Stormbound's 20 s and 2 s,
Gravitic's 20 s, 30 m, 1.5 s and x1.3, and Colossal's 40%, 15% and 15%. Judgement calls made while building them,
each a default in the rule file or a fixed rule in the code:

- **Loot**: Stormbound and Colossal 1.2x, Adaptive and Fixated 1.3x. Only Gravitic's came from the user.
- **`none` raised from 20 to 30**, so a plain fight is still about one in five (30 of 160) with thirteen aspects.
- **Adaptive counts after the boss's own resistances** and before the sneak-attack and stagger bonuses, so a type
  it already shrugs off can never become the one it adapts to. Only players' and tames' hits teach it; its cut
  applies to every hit. Ties keep the current type; a change applies from the next hit; an empty window resists
  nothing. The glow is the health-potion aura recoloured plus a light, one colour per type.
- **Fixated's fight is everyone within 60 m**, and the mark goes by **damage in the current period only**, read off
  the boss board's tally, so players can take turns; an empty period keeps the mark. A lost mark moves at once, to
  the period's top damage, else the boss's target, else the nearest. Tames on the players' side take the 30% less;
  wild creatures are untouched. The eye and the chat line reach players within 100 m, and the line is said only for
  a fresh mark.
- **Stormbound's strike is 8% of max health as lightning, the circle's radius 2.5 m, the range 40 m** measured
  along the ground. Lightning resistance and a protection bubble apply, armour does not; a roll avoids it, a shield
  does not; players only. The first storm comes a full interval in, a lull pauses, 10 s out of the fight resets. The
  circle draws even at effect density 0, because it is the warning.
- **Gravitic pulls at 6 m/s, slams within 6 m of its body for 10% of max health.** The pull is planar and stops a
  step short of the body; walls stop it, a roll pauses it, a jump escapes it. The slam is untyped (armour and
  blocking do not help) and dodgeable. Players who are seated, riding, at a helm, on a ship's deck or teleporting are
  never pulled.
- **Colossal's shockwave: 8 m, at most one every 5 s per boss, and only from heavy attacks** - an area attack that
  harms players, or a melee blow whose largest damage type is blunt - so it follows each boss's own weightiest
  attacks and works for modded bosses without a list. It knocks down without damage; a roll or a jump avoids it. The
  corpse keeps the boss's size. Attack reach is not grown.
- Stormbound and Gravitic share one rhythm rule: only seconds of fight count, a lull pauses, 10 s out of the fight
  resets, and the last storm's or roar's time is in the boss's ZDO so a hand-over never doubles it.

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
- [ ] Phantom: splits at 66% and 33%, one copy per player online, 25 health per tier (1 at tier 0), half damage,
  no drops, no body, vanish with the boss
- [ ] Phantom copies' health bars in a small row under the boss's bar
- [ ] Adaptive: resists the type players and tames dealt most in the last 15 s, counted after its resistances; ties
  keep the type; a change applies from the next hit; dark and resisting nothing with an empty window
- [ ] Adaptive: the glow in the type's colour on every client, blending on a change; nothing at effect density 0;
  `elite inspect` names the type
- [ ] Adaptive: a hand-over keeps the resisted type and the glow
- [ ] Fixated: solo player marked at once; in a group the mark moves every 30 s to the period's top damage, and at
  once when the marked player dies, logs out or leaves 60 m
- [ ] Fixated: +50% on the marked player, -30% on other players and tames, wild creatures untouched
- [ ] Fixated: red eye over the marked player and the chat line within 100 m; no line for a late arrival
- [ ] Stormbound: a circle under each player within 40 m every 20 s of fight; lightning 2 s later; 8% of max health
  and a stagger inside; a roll avoids it, a shield does not; one strike per player per storm
- [ ] Stormbound: circle drawn at effect density 0; boss death mid-tell breaks the storm; a late arrival sees it
- [ ] Gravitic: roar, pull toward the boss for 1.5 s at 6 m/s, stopping short of the body; walls stop it, a roll
  pauses it, a jump escapes it; seated, riding, on a ship and teleporting players are never pulled
- [ ] Gravitic: slam for 10% of max health and a stagger within 6 m of the body; a roll avoids it; no slam if the
  boss dies mid-pull
- [ ] Colossal: 40% bigger, 15% more health, 15% slower, on every machine; its corpse keeps its size
- [ ] Colossal: heavy blows only, at most one shockwave per 5 s, none from a boss in the air; knocked down within
  8 m with no damage; a roll or a jump avoids it
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
- [ ] Summoner waves, Mending, Twin spawns and Phantom splits decided by the boss's owner
- [ ] Twin pool holds with the two twins owned by different machines
- [ ] Phantom copies stripped on every machine; vanish reaches each copy's owner
- [ ] Adaptive's type, Fixated's mark and Stormbound's storm read from the boss's ZDO by every client and kept
  across a hand-over
- [ ] Stormbound, Gravitic and Colossal judged on each player's own machine; Gravitic's roar and Colossal's
  shockwave reach only machines holding the boss; a hand-over never doubles a storm or a roar
- [ ] Colossal's corpse grown on every machine from the ragdoll's ZDO
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
| 2026-09-26 | Set replaced with the user's eight; open decisions settled; whole feature built, untested; boss stars drawn on the boss health bar. | 61a0c3e |
| 2026-09-26 | Phantom reworked with the user: splits at health marks, copies per player online, health per world tier, small copy bars under the boss bar. Untested. | EliteCreaturesReborn-v3.8.0 |
| 2026-09-27 | 3.9.0: Adaptive, Fixated, Stormbound, Gravitic and Colossal added at the user's request, with their loot, chances (`none` 20 -> 30), rule-file fields and judgement calls; `elite inspect` shows Adaptive's type and Fixated's mark. Built, not tested in game. | - |

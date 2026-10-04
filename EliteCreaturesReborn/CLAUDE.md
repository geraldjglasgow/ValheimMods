# Elite Creatures Reborn - reference

The store page (`README.md`) keeps one line per feature and links here. The sections below were moved from it
unchanged on 2026-09-30: what each mutation and boss aspect does, the fields of the rule file `creature_rules.yml`,
respawning, and the console commands with their arguments. Design notes are in `features/`.

## Difficulty

The first setting in `creature_rules.yml`, `difficulty:`, picks how many stars and mutations creatures get as the
world's bosses fall: `Easy`, `Medium`, `Hard`, `Very Hard` or `Extreme`. A new rule file starts on Medium. A file
without the line (any file written before 3.14.0) is `Custom`: the biomes' own `star chances` and `mutation chance`
rows with the world tier's `star boost` and `mutation boost`, exactly as before. `elite tier` shows the difficulty
and what it gives the biome you stand in.

On every difficulty the biome you're entering is the gentlest. A biome you've cleared gets harder with every boss
killed after it, so by the end of the game the Meadows is the hardest place, not the Deep North. Biomes you haven't
reached match the one you're entering. Each difficulty is harder than the one below it in every biome at every
world tier.

**Stars.** The most stars a creature can have:

| Difficulty | Biome you're entering | Each boss killed after a biome | Ocean |
|---|---|---|---|
| Easy | 1 | +1, up to 4 | 2 |
| Medium | 2 | +1, up to 5 | 2, then 3 once Moder is dead |
| Hard | 3 | +1, up to 5 | 3, then 4 once Moder is dead |
| Very Hard | 4 | +1, up to 5 | 4, then 5 once Moder is dead |
| Extreme | 5 at world tiers 0-1, 6 at 2-3, 7 at 4-5, 8 at 6-7, in every biome you've reached | | one lower than that, never below 5 (also biomes not reached yet) |

Under that cap most creatures are plain and each extra star is rarer than the one before it (at cap 3: 75 in 100
plain, 15 one star, 7 two, 3 three). Harder difficulties lean toward more stars, every boss killed after a biome
leans it 5% further, and Very Hard and Extreme lean further with every boss killed anywhere.

**Mutations.** The share of creatures with no stars that carry a mutation; each star adds a quarter of it, never above
100%. Every boss killed after a biome closes a fifth of the gap from the start to the ceiling.

| Difficulty | Biome you're entering | Ceiling | Ocean |
|---|---|---|---|
| Easy | 10% | 35% | 10% |
| Medium | 25% | 55% | 25%, then 30% once Moder is dead |
| Hard | 30% | 65% | 30%, then 35% once Moder is dead |
| Very Hard | halfway between Hard and Extreme everywhere: 37.5%, +3.5 per boss killed | 81.5% | halfway too |
| Extreme | 45%, +7 per boss killed | 98% | the biome you're entering |

A mutated creature carries one mutation, picked by its biome's leanings: the biome's `mutation chance` and
`mutation chances` rows (and the creature's own entry under `creatures:`), so the Swamp still breeds Miasmic and the
Ashlands Bloated, and `mutations enabled` still wins. `max mutations` and the world tier boosts apply to Custom only.

**6 to 8 stars** happen only on Extreme. The `star power` lines have nine entries; a file whose lines stop at five
stars continues them by the built-in steps (an 8-star creature has about 1.5 times a 5-star's health and damage).

## Mutations

Fifteen, one per creature by default, each with its own colour and its own name on the nameplate.

| Mutation | What it does |
| --- | --- |
| Mad | Far faster, half health |
| Bloated | Double health, explodes 1.5 seconds after it dies |
| Cloaked | Invisible beyond 10 metres (15 for trolls and lox); drakes are never Cloaked |
| Splintering | Splits into two weaker copies when killed, which can split again |
| Leeching | Regenerates, and heals from the damage it deals |
| Warding | Reflects part of each hit back, never more than 7.5% of your maximum health in any second, and knocks you back |
| Plated | Armoured while healthy, hits harder as that armour goes |
| Miasmic | Trails poison clouds; poisons players, never creatures |
| Devouring | Kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you. It eats only creatures with no more health than it has at that moment, never a boss or a large creature (trolls, bears, lox and the like), one per star in its life (at least one), and shows each one it ate on its nameplate |
| Thieving | Steals an item with each melee hit that lands (not thrown stones, not a parried or dodged blow), up to one per star (at least one), and carries them on its nameplate; kill it to get everything back |
| Gilded | Glitters gold, never attacks a player and runs from any it sees; drops three times its loot plus a purse of coins. Never on large creatures (trolls, bears, lox and the like) |
| Blinking | Every 30 seconds of a fight it reappears behind its target, after a flash and a chime at the spot; 25% less health |
| Relentless | Once it picks you it keeps coming, seen or not, until you are 150 m away; sneaking does not hide you; never faster than its base speed. Never on large creatures |
| Juggernaut | Never staggers or is knocked back: hits, parries, blasts and traps don't stop it; where it would have staggered, "Unstoppable" shows over it instead |
| Screecher | When one hit takes 15% of its health it shrieks: players within 20 m are deafened for 4 seconds - the world goes near-silent under a ringing, and Elemental and Blood Magic weapons will not cast. At most once every 15 seconds; a killing blow makes no shriek |

Gilded is the rarest, on purpose, and the only one in your favour: it runs, and pays well if you catch it.

Large creatures are measured, not listed: a body a metre or more wide or 3.5 m or more long, with 300 or more health -
trolls, bears, lox, golems, Fuling berserkers, Seeker soldiers, Morgen, Gjall and the like, modded ones included.

## Mutation power fields

`mutation power` in the rule file sets how strong each mutation is, in named fields rather than positional
numbers. A field marked (enhanced) is multiplied by `large star power` on a large star; a cost field never is.
Any mutation can also be switched off entirely with `mutations enabled`, regardless of its chance curves.

- **Mad** - `move`/`attack speed` multipliers (enhanced); `health` multiplier, its cost
- **Bloated** - `health` multiplier (enhanced); `delay` seconds from death to the blast, 1.5 by default; the blast goes
  off at the corpse's resting place and the corpse goes with it, dropping its loot there; `damage` (enhanced) and
  `radius` (enhanced) of the blast; `blast effect`/`warning effect` vanilla prefabs for the explosion and the smoke
  that rides the corpse until it blows, `blast sound` the vanilla sound it goes off with (`elite effects <text>`
  lists and plays the game's effect and sound prefabs)
- **Cloaked** - `reveal distance` metres to become visible (enhanced), 10 by default and 15 for trolls and lox
  through their `creatures:` entries; `fade time` seconds to phase, 0 snaps; `fade margin` extra metres before
  fading back out, to stop strobing
- **Splintering** - `damage` multiplier per split; `max generations` cascade-depth cap, 0 = unlimited;
  `max descendants` live-descendant cap, 0 = unlimited
- **Leeching** - `regen` percent max health per second, never enhanced; `regen cap` hard HP/s ceiling on a single
  tick, so a huge-health creature cannot out-heal a fight; `combat cooldown` seconds since its last damage taken
  before regen resumes; `lifesteal` percent of damage dealt returned as health (enhanced)
- **Warding** - `reflect` percent of the base hit returned (enhanced). The base hit is the health the hit actually
  took off the creature, after its resistances and armour, without the sneak-attack or stagger bonus; a hit that
  took no health reflects nothing. `max reflect` caps everything reflected to one attacker in any one second at
  that percent of the attacker's maximum health, however many hits land and however many Warding creatures they land
  on, before the attacker's own armour (never enhanced; 0 removes the cap); `knockback` force on a melee
  attacker (enhanced)
- **Plated** - `armour` percent of incoming damage cut at full health, to 0 hurt (enhanced); `max reduction` hard
  ceiling on that percent, so enhancement cannot approach invulnerability; `damage` percent bonus at zero health,
  to 0 full (enhanced)
- **Miasmic** - `cloud life` seconds a dropped cloud lasts; `cloud damage` strength of the vanilla Poison a cloud
  or hit applies, not direct damage (enhanced); `clouds per second` while moving (enhanced); `cloud radius` metres;
  `cloud effect`/`body effect` vanilla prefabs for the trail cloud and the permanent worn poison look (cosmetic)
- **Devouring** - `absorb health`/`absorb damage` percent kept permanently from a victim (enhanced; an instant
  kill always lands the killing blow, so the full amount is kept); `slow per 100 health`, its cost;
  `move` base speed multiplier before the slow (`0.5` halves it; not enhanced);
  `player threshold` fraction of a player's max health a hit must pass before it hunts players for good;
  `devour cooldown` seconds after a meal before it can eat again; `max prey health` the most current health a creature
  may have for it to hunt and eat it, as a percent of its own current health, 100 by default, so no more than its own
  (0 lifts the limit; not enhanced). A boss or a large creature is never prey, whatever this says; `min meals` the fewest creatures it eats in its life whatever its stars, 1 by default (not enhanced) - it
  eats one per star, and once it has eaten them all it is sated: it eats nothing more and behaves like any creature of
  its kind. Its nameplate shows each creature it ate, by that creature's trophy (a horned monster head for one with no
  trophy)
- **Thieving** - it holds one item per star; `max items` is the fewest it holds whatever its stars (enhanced),
  and 8 is the most. It never takes equipped gear or more than one item per landed melee hit, and gives back everything
  it holds when it is killed. With PackPanel it takes only from your main grid, never from PackPanel's slots (gear,
  backpack, food, mead, ammo, coin purse, key ring, tacklebox)
- **Gilded** - `loot` multiplier on its drops (enhanced), applied in every loot mode; `bonus item` the item prefab
  of its purse and `bonus amount` how many per star plus one (enhanced), never multiplied and capped at 100;
  `flee distance` metres within which it runs from a player it can see; `glitter effect` the vanilla prefab it
  glitters with. A tamed Gilded creature flees no one and drops ordinary loot
- **Blinking** - `health` multiplier, its cost; `every` seconds of combat between blinks, 0 turns them off;
  `distance` metres behind its target it lands; `tell time` seconds of warning at the spot before it arrives;
  `blink effect`/`tell effect` vanilla prefabs for the puff and the marker, `tell sound` the chime. None is enhanced
- **Relentless** - `chase distance` metres within which it keeps its target, 0 turns the hold off (not enhanced).
  Its cost is fixed: it is never faster than its base speed
- **Juggernaut** - no fields: it never staggers and is never knocked back
- **Screecher** - `threshold` percent of its max health one hit must take (health actually lost) to make it shriek;
  `radius` metres the shriek reaches (enhanced); `mute time` seconds a player stays deafened and unable to cast
  Elemental or Blood Magic (enhanced) - a second shriek extends it, never stacks; `cooldown` seconds between shrieks;
  `shriek sound` the vanilla sound it shrieks with. Only players it is hostile to are deafened, so a tamed Screecher
  deafens no one

## Boss aspects

A boss never takes a mutation. It takes an **aspect**, which is in its name - "Enraged Eikthyr" - and on its altar
before you commit: hover the offering bowl to see the current aspect, what it does, what it pays, and how long
until it shifts. Every altar shifts to a different aspect each in-game hour (75 real seconds), so a group can wait
for the fight it wants. The aspect on the bowl when you make the offering is the one you fight. A boss with no
altar - the Queen, or a console spawn - rolls its aspect when it first appears.

| Aspect | What it does | Loot |
| --- | --- | --- |
| none | The fight as the game ships it (about one fight in five) | x1 |
| Reflective | 15% of each hit you land comes back to you as true damage | x1.4 |
| Shielded | 30% less damage from arrows and bolts | x1.1 |
| Mending | Regenerates 0.3% of its health every second, in combat too | x1.3 |
| Summoner | Calls two 2-star creatures of its own biome each time it loses 33% of its health | x1.5 |
| Elementalist | 20% more fire, frost, lightning, poison and spirit damage | x1.2 |
| Enraged | 20% more physical damage | x1.2 |
| Twin | A second copy of the boss; the two share one health pool, 25% less health and damage each, and both drop full loot | x1 each |
| Phantom | At 66% and again at 33% health it splits off one copy per player online, each with 25 health per world tier (tier 0 counts as 1) and half its damage; copies drop nothing, leave no body, and vanish when the boss dies. Their small health bars sit in a row under the boss's own | x1.3 |
| Adaptive | Takes 50% less of whichever damage type hit it most in the last 15 seconds, and glows that type's colour. Swap weapons, or spread the group across damage types | x1.3 |
| Fixated | Marks one player with a red eye over their head and a line in chat, hits them 50% harder and everyone else 30% softer. Every 30 seconds the mark moves to whoever hurt it most in those seconds; alone, you are always marked | x1.3 |
| Stormbound | Every 20 seconds a glowing circle appears under each player within 40 m, and 2 seconds later lightning strikes it: 8% of your maximum health and a stagger if you are still inside. A roll through it is safe; a shield is not | x1.2 |
| Gravitic | Every 20 seconds it roars and drags every player within 30 m toward it for 1.5 seconds, then slams: 10% of your maximum health and a stagger within 6 m of its body. A roll dodges the slam | x1.3 |
| Colossal | 40% bigger, 15% more health, 15% slower. Its heavy blows send out a shockwave that knocks players within 8 m down, no damage; roll through it or jump it | x1.2 |
| Tethered | Comes as two bosses joined by a faint tether, each with 25% less health and damage; they keep their own health and die apart. The further apart their health, the tauter and redder the tether, the faster both attack (up to 50%) and the less damage the one with less health left takes (up to 50%), both at their most once the gap reaches 50 points. Kill one first and the other fights on at full speed. Both health bars show, one under the other; only the last to fall drops loot | x1 |
| Bountiful | Carries two more aspects at once, drawn from that boss's own rotation (never two of Twin, Tethered and Phantom). The altar shows all of them before you offer, and the name carries every word: "Bountiful Enraged Mending Eikthyr". Its twin, tethered partner or Phantom copies carry its other aspects too (each copy calls its own Summoner waves and marks its own Fixated player). Pays every aspect's loot multiplied together | x2 (times each extra's) |
| Portalbound | Elder only. As it winds up its vine throw a portal opens 5 to 8 m up within 20 m of its target, in sight of them; as it throws, a second portal opens on its hand and the vines fly out of the far portal at you. No clear spot: it throws as usual | x1.2 |

The chances are weights: 38 for the plain fight and 10 for each of the sixteen aspects (Portalbound only for the Elder), so about one boss fight in
five stays as the game ships it. Stormbound's lightning, Gravitic's slam and Colossal's shockwave are dodged, not
blocked: a roll timed through them avoids them, and a raised shield does not.

Everything is in the `aspects:` block under `bosses:` in the rule file: the off switch, the shift interval (0 fixes
each altar), the chance of each outcome, the loot multiplier, every aspect's numbers, and per boss the creatures
Summoner calls and, optionally, which aspects that boss may roll.

**Boss trophies.** A boss drops one trophy per star plus one (five heads from a four-star boss) in every loot mode,
Vanilla included. Nothing else changes that count: not the `drops` line, extra rolls, the global or boss multiplier,
an aspect's loot or `multiply trophies`. Each Twin drops its own; of a Tethered pair only the last to fall drops anything; Phantom copies drop nothing.
Only a `drop overrides` row naming the trophy in the boss's `creatures:` entry replaces it, and an `extra drops` row
adds on top.

## Boss aspect power fields

`power` under `aspects:` sets each aspect's numbers. Percentages are of the boss as its stars left it.

- **Reflective** - `reflect` % of each hit you land that comes back to you, as true damage your armour does not
  reduce (burn and poison ticks never reflect)
- **Shielded** - `arrow reduction` % less damage from bows and crossbows
- **Mending** - `regen` % of max health healed every second, in combat too
- **Summoner** - `every` % of max health lost per wave; `count` creatures per wave; `stars` each summoned creature has
- **Elementalist** - `elemental bonus` % more fire, frost, lightning, poison and spirit damage
- **Enraged** - `physical bonus` % more blunt, slash and pierce damage
- **Twin** - `less health` and `less damage` % less for each of the two; they share one health pool and die together
- **Phantom** - `split at` the % of health left at which it splits off copies, `[]` for never; `per player` copies
  per player online at each split; `health per tier` each copy's max health per world tier (tier 0 counts as 1);
  `less damage` % less than the boss deals
- **Adaptive** - `resist` % less of whichever damage type hit it most in the last `window` seconds
- **Fixated** - `marked bonus` % harder it hits the marked player; `others less` % softer it hits everyone else;
  `every` seconds before the mark moves to whoever hurt it most
- **Stormbound** - `every` seconds between strikes; `tell time` seconds the circle shows under each player within
  `range` m before lightning hits it; `radius` the circle's size in m; `damage` % of a struck player's max health
- **Gravitic** - `every` seconds between pulls; `range` m it pulls players from; `pull time` seconds; `pull speed`
  m/s toward it; `slam radius` m and `slam damage` % of each player's max health for the slam after the pull
- **Colossal** - `bigger`, `more health` and `slower` in %; `shockwave radius` m its heavy attacks knock players
  down in
- **Tethered** - `less health` and `less damage` % less for each of the two (separate health, they die apart);
  `attack speed` % faster both attack and `armour` % less damage the one with less health left takes, each reached once
  their health is `full gap` percentage points apart and scaled down evenly below that; a dead or unloaded partner
  counts as empty
- **Bountiful** - `extra aspects` how many more aspects it carries, drawn by the chances from that boss's rotation
- **Portalbound** - `min height` m the far portal hangs at least above whatever is under it (ground, building,
  treetop or water); `clearance` m it keeps from anything solid and any creature; `range` m from the boss's target it
  opens within, with a clear line to it (0: never). Only the Elder rolls it

## Per-creature rules

Each entry under `creatures:` in the rule file is matched by prefab name (`elite reference` lists every one) and may
set any of these keys:

- `drops` - replaces the star `drops` line for this creature
- `multiply trophies` - this creature's own trophy switch (a boss's own trophies ignore it: always one per star plus one)
- `drop overrides` - changes rows of its own drop table: `item`, then `amount: [min, max]` (inclusive), `chance`
  0-100, or `remove: true` to delete the row
- `extra drops` - adds rows with `item`, `amount` and `chance`; `per star: true` makes a row follow the loot mode
  like the creature's own rows. In Curated mode these rows are the whole table
- `mutation chance`, `mutation chances`, `mutation power` - the same keys a biome block takes, for this creature only

A creature can have mutation rules of its own. An entry under `creatures:` in the rule file, matched by prefab name,
takes `mutation chance`, `mutation chances` and `mutation power` exactly as a biome block does. They change that
creature only, in whatever biome it is in, and everything they do not name still comes from the biome. This ends
Cloaked mosquitoes and leaves the rest of the Plains alone:

```yaml
creatures:
  - match: Deathsquito
    mutation chances:
      Cloaked: [0]
```

`mutation power` changes only the fields it names. `mutation chances` replaces the named mutation's curve.
`mutation chance` replaces the creature's default curve, but any mutation with its own curve in the biome (or in
`defaults`, like Devouring and Gilded) keeps it, so set that mutation to `[0]` under `mutation chances` to stop it.
`mutations enabled` still wins, the world tier still raises the chances (on Custom; on a difficulty the entry decides which mutation the creature gets), and bosses ignore these entries. Two
entries for the same creature merge: later keys win and drop rows add up. A new rule file ships with three: Troll
and Lox raise Cloaked's `reveal distance` to 15, and Hatchling (the drake) sets Cloaked to `[0]`, so drakes are
never Cloaked.

## Respawning

Cleared camps and dungeons can fill up again, and emptied dungeon chests can refill, each on its own timer in world
days (a world day is 30 real minutes at the game's default speed). All three are off by default, so clearing a camp
or a dungeon stays worth doing. A spawner the game already times on its own is left alone, and a chest refills only
once it has been fully emptied.

## Console commands

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns exactly that creature, bypassing every roll, for testing. A boss takes one aspect instead: `elite spawn Bonemass 2 Twin`; Bountiful takes its extras after it, or rolls them when none follow: `elite spawn gd_king 2 Bountiful Enraged Mending` |
| `elite inspect` | Prints the resolved stars, mutations or aspect, and numbers for the creature under your crosshair, with what a Thieving creature carries and what a Devouring creature has eaten ("devoured 1 of 2: Boar") |
| `elite purge [radius]` | Removes the loaded creatures this mod has marked, with no drops (a Thieving creature's stolen goods drop first). With a radius in metres, only those that close to you: `elite purge 30` |
| `elite effects <text>` | Lists loaded effect prefabs matching the text and plays one, for building visuals |
| `elite reference` | Writes `creature_reference.yml`: every creature the game knows, by biome, with its drop table |
| `elite tier` | Shows the world tier, what it does to the rolls, and which bosses count. Open to every player; the other `elite` commands are admin only |
| `damage` | Shows the latest boss damage board again. Open to every player, and typed as `/damage` in chat |

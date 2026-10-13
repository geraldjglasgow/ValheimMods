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

**The game's World Difficulty** (its combat setting, Very Easy to Very Hard) is separate and stacks: it still changes
how hard enemies hit and how hard players hit them, but its extra star chance (`Game.m_enemyLevelUpRate`) does nothing,
because creatures are kept at the game's level 1 and roll this mod's stars. With `creature stars: false` the game's
stars, that star chance included, apply as usual.

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
stars continues them by the built-in steps (an 8-star creature has about 1.5 times a 5-star's health and 1.4 times its damage).

## Creature stars off

`creature stars: false` at the top of `creature_rules.yml` (a file from before 3.17.0 has no line: add it) turns this
mod's creature stars off, for playing beside another mod that gives creatures stars, such as Path of Valheim:

- Creatures roll no stars here and keep the stars the game or the other mod gave them, with the game's own health,
  damage, loot, look and nameplate stars for them. The `star power` lines, the star `drops` line and the extra rolls
  do not apply to them.
- They still roll mutations: on a difficulty at the biome's rate for a creature with no stars, on Custom at each
  mutation's 0-star chance. A mutation's own numbers (Bloated's health, Mad's speed...) apply on top of the other mod's.
- Newborns inherit no stars from this mod (the game passes on the parent's level), and GrindstoneSkills' extra star
  for a newborn is declined.
- A Summoner boss's adds come at the game's level for their stars (`stars: 2` makes them level 3). `elite spawn`
  still gives the stars it names.

Bosses have their own switch, `stars:` under `bosses:`; with it off a boss keeps its level the same way. `elite tier`
says when creature stars are off, and `elite inspect` shows a kept level.

## Loaded crossbows

A crossbow put away loaded - a weapon swap, a swim - is still loaded when drawn again, with no reload. Firing empties
it as before. It lasts until the game closes or the player logs out.

## Mutations

Twenty-three, one per creature by default, each with its own colour and its own name on the nameplate. Piercing,
Howling and Binding start switched off: set them to `true` under `mutations enabled` (a rule file written before them
has no line for them and leaves them off; Flamebound, which starts on, rolls in such a file too).

| Mutation | What it does |
| --- | --- |
| Mad | Far faster, half health; never on bats |
| Bloated | Double health, explodes 1.5 seconds after it dies |
| Cloaked | Invisible beyond 10 metres (15 for trolls, lox and the Rime Giant); drakes, bats and deathsquitos are never Cloaked |
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
| Frostbound | A freezing aura: players within 6 m recover stamina half as fast ("Chilled"). Leaves slick ice where it walks: on it you slide - slow to start, turn and stop - and move 15% slower; each patch lasts 10 seconds. Frost heals it instead of hurting it and never slows it (a green "+N frost") |
| Mudbound | Leaves thick mud where it walks: in it you move 40% slower ("Deep mud"), and for a second after stepping out. Each patch lasts 10 seconds |
| Corrodent | Your armour loses durability three times as fast under its hits, melee, thrown or shot; a line names the piece that corroded and how much is left. Shields wear as usual |
| Cloning | When it squares up to you it leaves a decoy in its place and fights on unseen. The decoy looks exactly like it but its blows do nothing; the first blow of the unseen one that lands on you shows it, and the decoy vanishes in a puff. Kill the decoy first and it shows itself too; after 20 seconds it shows itself anyway. Once in its life |
| Piercing | Each of its hits - melee, thrown or shot - ignores 15% of your armour. Block, parry, resistances and armour wear are as usual. Off by default |
| Howling | When it turns on you it howls (a wolf's howl), and the 4 nearest creatures on its side within 30 m that are hostile to you come for you; again every 45 seconds while it fights you. It calls creatures already there, never new ones, and never a boss. Off by default |
| Binding | Leaves patches of tangled roots where it walks, smaller and further apart than ice or mud (one every 3 m walked, each reaching 1.2 m), so you can step between them, each lasting 10 seconds. Step into one and the roots burst up and hold you for a second ("Rooted": no walking, running, jumping or rolling; you can still turn, block and strike), then let you go for 3 seconds, long enough to walk clear. Off by default |
| Flamebound | Leaves burning ground where it walks: flames on the ground, 2 m wide, a patch every 1.5 m, each lasting 7 seconds (with effects off, glowing embers instead). Standing in it sets you on fire, the game's own Burning: 6 fire damage a second, more with stars (its star `attack` line); fire resistance, armour and being wet help. Its heat clears mist around it as a Wisplight does |

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
- **Cloaked** - `reveal distance` metres to become visible (enhanced), 10 by default and 15 for trolls, lox and the Rime Giant
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
  `player threshold` fraction of a player's max health (the highest among the players within 64 m of it) a hit must
  pass before it hunts players for good;
  `devour cooldown` seconds after a meal before it can eat again; `max prey health` the most current health a creature
  may have for it to hunt and eat it, as a percent of its own current health, 100 by default, so no more than its own
  (0 lifts the limit; not enhanced). A boss or a large creature is never prey, whatever this says; `min meals` the fewest creatures it eats in its life whatever its stars, 1 by default (not enhanced) - it
  eats one per star, and once it has eaten them all it is sated: it eats nothing more and behaves like any creature of
  its kind. Its nameplate shows each creature it ate, by that creature's trophy (a horned monster head for one with no
  trophy)
- **Thieving** - it holds one item per star; `max items` is the fewest it holds whatever its stars (enhanced),
  and 8 is the most. It never takes equipped gear, the Wishbone or more than one item per landed melee hit, and gives back everything
  it holds when it is killed. A stolen item it cannot keep (its pouch filled meanwhile, it died or went out of reach
  before the theft reached it) comes back to you within seconds, into your inventory or at your feet. With PackPanel it takes only from your main grid, never from PackPanel's slots (gear,
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
- **Frostbound** - the aura: `aura radius` metres it reaches (enhanced); `stamina regen` percent less stamina
  regeneration for a player inside it that it counts as an enemy (enhanced, at most 100), so a tamed one chills no
  friend; two auras never stack; `aura effect` the vanilla prefab it wears (cosmetic). `frost heal` percent of a hit's
  frost it heals instead of taking (enhanced; 0 makes frost do nothing to it) - the frost as the attacker sent it,
  before its own frost resistance; frost never slows it. The trail: `trail life` seconds an ice patch lasts, 0 for no
  trail; `patch radius` metres it reaches; `patch spacing` metres it walks between patches; `grip` percent of their
  footing a player keeps on the ice (100 is normal ground; at 5 a running player slides about 3 m before stopping);
  `slow` percent slower on the ice (enhanced, at most 90); `trail effect` the vanilla effect whose ground decal draws
  the patches. Life, radius, spacing and grip are never enhanced. A tamed one lays no trail
- **Mudbound** - `trail life` seconds a mud patch lasts, 0 for no trail; `patch radius` metres it reaches;
  `patch spacing` metres it walks between patches; `slow` percent slower in the mud, and for a second after stepping
  out (enhanced, at most 90); `trail effect` the vanilla effect whose ground decal draws the patches. A tamed one lays
  no trail
- **Corrodent** - `durability` how many times as fast a player's armour wears under its hits (enhanced, as a
  `1 + bonus` multiplier); 1 is the game's own rate, 0 stops its hits wearing armour. Durability never goes below 0;
  shields wear as usual
- **Cloning** - `times` how many times in its life it hides behind a decoy (enhanced; 0 turns it off); `range` metres
  from the player it is fighting at which it does it; `decoy life` seconds before it shows itself anyway when no blow
  has landed, 0 for no limit; `cooldown` seconds after it shows itself before it can do it again (only with `times`
  above 1); `reveal effect`/`reveal sound` the vanilla effect and sound it shows itself with, `vanish effect` the puff
  the decoy goes in. The decoy has its name, stars, gear, size, speed and current health but none of its mutations'
  powers: its blows do no damage, push, stagger or poison to anything, it drops nothing, leaves no body and is never a
  Devouring creature's prey. A blocked blow from the unseen one shows it, a dodged one does not. A tamed Cloning
  creature never does it
- **Piercing, Howling, Binding, Flamebound** - no fields: their numbers are fixed (above). Piercing's armour share,
  Binding's hold and Flamebound's fire are enhanced on a large star (the hold at most 3 seconds); Howling's reach,
  count and cooldown never are. Binding and Flamebound lay nothing when tamed, swimming or off the ground, and their
  patches act on players only, never creatures

## Boss aspects

A boss never takes a mutation. It takes an **aspect**, which is in its name - "Enraged Eikthyr" - and on its altar
before you commit: hover the offering bowl to see the boss's stars ("Stars: ★★", or none), the current aspect, what
it does, what it pays, and how long until it shifts. Every 15 seconds the altar shifts: the stars roll again and the
aspect changes to a different one, so a group can wait for the fight it wants. The stars and aspect on the bowl when
you make the offering are the ones you fight. A boss with no altar - the Queen, or a console spawn - rolls its stars
and aspect when it first appears.

| Aspect | What it does | Loot |
| --- | --- | --- |
| none | The fight as the game ships it (about one fight in five) | x1 |
| Reflective | 15% of each hit you land comes back to you as true damage | x1.4 |
| Shielded | 30% less damage from arrows and bolts | x1.1 |
| Mending | Regenerates 0.2% of its current health every second, in combat too, so less as it falls | x1.3 |
| Summoner | Calls two 2-star creatures of its own biome each time it loses 33% of its health | x1.5 |
| Elementalist | 20% more fire, frost, lightning, poison and spirit damage | x1.2 |
| Enraged | 20% more physical damage | x1.2 |
| Twin | A second copy of the boss; the two share one health pool, 25% less health and damage each, and both drop full loot | x1 each |
| Phantom | At 66% and again at 33% health it splits off one copy per player online, each with 25 health per world tier (tier 0 counts as 1) and half its damage; copies drop nothing, leave no body, and vanish when the boss dies. At each split the boss hides among them: in a puff it takes a random place in a ring with its copies, which carry its name and its share of health left, and all their bars, the boss's too, sit small in one row in random order. Hit them to find the boss | x1.3 |
| Adaptive | Takes 50% less of whichever damage type hit it most in the last 15 seconds, and glows that type's colour. Swap weapons, or spread the group across damage types | x1.3 |
| Fixated | Marks one player with a red eye over their head and a line in chat, hits them 50% harder and everyone else 30% softer. Every 30 seconds the mark moves to whoever hurt it most in those seconds; alone, you are always marked | x1.3 |
| Stormbound | Every 20 seconds a glowing circle appears under each player within 40 m, and 2 seconds later lightning strikes it: 8% of your maximum health and a stagger if you are still inside. A roll through it is safe; a shield is not | x1.2 |
| Gravitic | Every 20 seconds it roars and drags every player within 30 m toward it for 1.5 seconds, then slams: 10% of your maximum health and a stagger within 6 m of its body. A roll dodges the slam | x1.3 |
| Colossal | 40% bigger, 15% more health, 15% slower. Its heavy blows send out a shockwave that knocks players within 8 m down, no damage; roll through it or jump it | x1.2 |
| Tethered | Comes as two bosses joined by a faint tether, each with 25% less health and damage; they keep their own health and die apart. The further apart their health, the tauter and redder the tether, the faster both attack (up to 50%) and the less damage the one with less health left takes (up to 50%), both at their most once the gap reaches 50 points. Kill one first and the other fights on at full speed. Both health bars show, one under the other; only the last to fall drops loot, and the damage board shows once, when the last falls, counting both | x1 |
| Bountiful | Carries two more aspects at once, drawn from that boss's own rotation (never two of Twin, Tethered and Phantom). The altar shows all of them before you offer, and the name carries every word: "Bountiful Enraged Mending Eikthyr". Its twin, tethered partner or Phantom copies carry its other aspects too (each copy calls its own Summoner waves and marks its own Fixated player). Pays every aspect's loot multiplied together, and twice the boss trophies | x2 (times each extra's) |
| Portalbound | The Elder and Bonemass only. As it winds up its throw (the Elder's vines, Bonemass's slime ball) a portal opens 5 to 8 m up within 20 m of its target, in sight of them; as it throws, a second portal opens on its hand and the vines fly, or the slime arcs, out of the far portal at you. No clear spot: it throws as usual | x1.2 |
| Nightfall | Night falls on the fight: every player within 60 m of it sees a storming midnight - rain, lightning and thunder - and turns Wet and Cold as under a real storm at night (a roof, a fire or frost resistance still help); the day comes back over a few seconds once it dies or you go 10 m past that range. Every 18 to 28 seconds of fight a tornado whirls up 6 to 9 m in front of each player: for 1.5 seconds it forms, harmless, then hunts that player at 40% of a player's run speed until 10 seconds after it rose, and anyone its funnel touches (1.5 m wide at the ground, 9 m at its 14 m top) takes 25 lightning damage a second; armour does not help. One tornado per player at a time. A tornado that passes over one of the Elder's roots picks it up and throws it in a random direction, 6 to 10 m away. Only what each player sees and feels changes: the world's time and weather never do | x1.3 |
| Brutal | Its heavy blows throw every player they hit 20 m away, 3 m up at the peak; the hit deals its damage as usual, the landing none. A roll through the blow, or a block that holds (a parry included), keeps you on your feet; a broken guard or a blow from behind throws you. Never while swimming, seated, riding or on a ship's deck | x1.2 |
| Echoing | 15 seconds into the fight a white, nearly clear ghost of the boss rises where the boss stood then, and from then on does everything the boss did 15 seconds before - walks its path, turns, swings, throws, staggers - until the boss dies, when it is simply gone. Its blows, throws and breaths hurt as the boss's did (its stars and its aspects' damage), aimed where the boss's target stood then; anything the boss's attacks call up (the Elder's roots) the ghost's call up too. It cannot be hit, no creature goes for it, and it shows no health bar. Each boss of a Twin or Tethered pair has its own; Phantom copies have none. The Kraken never rolls it | x1.3 |

The chances are weights: 42 for the plain fight and 10 for each of the nineteen aspects (Portalbound only for the Elder and Bonemass), so about one boss fight in
five stays as the game ships it. Stormbound's lightning, Gravitic's slam, Colossal's shockwave and Nightfall's tornadoes
are dodged, not blocked: a roll timed through them avoids them, and a raised shield does not. Brutal's throw is the one
a shield stops.

Everything is in the `aspects:` block under `bosses:` in the rule file: the off switch, the shift interval in real
seconds (`shift seconds`; 0 fixes each altar; an older file's `shift hours` still works), the chance of each outcome, the loot multiplier, every aspect's numbers, and per boss the creatures
Summoner calls and, optionally, which aspects that boss may roll.

**Boss trophies.** A boss drops one trophy for every player within 100 m of it, plus one per star plus one (six heads
from a two-star boss killed by three players) in every loot mode, Vanilla included, whatever another mod (Epic Loot's
boss trophy mode) sets. Only Bountiful changes that count, times its own loot (twelve heads for that group);
nothing else does: not the `drops` line, extra rolls, the global or boss multiplier, another aspect's loot or
`multiply trophies`. Each Twin drops its own; of a Tethered pair only the last to fall drops anything; Phantom copies drop nothing.
Only a `drop overrides` row naming the trophy in the boss's `creatures:` entry replaces it, and an `extra drops` row
adds on top.

**Boss stars never multiply its other drops** (since 2026-10-05; Wishbones piled up): the heads are what stars pay. The
rest of a boss's table drops once - no extra rolls, no star `drops` line (a `drops` line under `bosses: star power:` in
an older file only warns) - times its aspect's loot and the global and boss multipliers. A `drops` line in the boss's
own `creatures:` entry still applies.

**Boss rewards** (`rewards: true` under `bosses:`; a GitHub player's suggestion, 2026-10-10): each boss also drops its
biome's material, a fixed table in `Loot/BossRewards.cs` - Eikthyr 2-4 Flint (20-40 Coins before 3.24.1; a player
asked for flint, 2026-10-12), the Elder 5-10 CopperOre, Bonemass 5-10 IronScrap, Moder 5-10 SilverOre, Yagluth 5-10 BlackMetalScrap, the Queen 5-10 Carapace, Fader 5-10 FlametalOreNew. One
roll for the plain fight plus one per star, for every player within 100 m of the boss (since 3.24.1, a player's
question, 2026-10-12: each earns a solo share, as with the trophies), times its aspects' loot and (loot rules on) the
global and boss multipliers, capped at the game's 100 per drop; paid in every loot mode, Vanilla included. Each Twin
pays its own; of a Tethered pair only the last to fall; a Phantom copy never. The altar shows one player's range for
the fight on the bowl ("Reward: 10-20 Iron Scrap per player").

**Harder each kill** (`harder each kill: true` under `bosses:`): each defeat leans the boss's next star roll like the
world tier's star boost, each star count's weight times (1 + 0.5 x defeats)^stars, counting at most 10 defeats (90/6/3/1
becomes 83/8/6/3 after one, 20/8/24/48 after ten). The count is a world global key, `ecr_kills_<prefab> N`, set by the
dying boss's owner through the game's SetGlobalKey (the server ignores a line it holds, so a Twin pair counts once; the
first of a Tethered pair and Phantom copies never count). The altar shows "Defeated N times"; `removekey
ecr_kills_<prefab>` resets one. It counts with the switch off too, so turning it on uses the real history.

## Boss aspect power fields

`shift seconds` (15) under `aspects:` is the real seconds between altar shifts. Each shift rolls the boss's stars and
its aspect again (never the same aspect twice in a row; the stars may repeat); 0 fixes each altar once it is first
rolled. It applies to the stars even with `enabled: false`. An older file's `shift hours` (in-game hours, 75 s each on
the default day) still works when the file names only that; if both are named, `shift seconds` wins.

`power` under `aspects:` sets each aspect's numbers. Percentages are of the boss as its stars left it.

- **Reflective** - `reflect` % of each hit you land that comes back to you, as true damage your armour does not
  reduce (burn and poison ticks never reflect)
- **Shielded** - `arrow reduction` % less damage from bows and crossbows
- **Mending** - `regen` % of its current health healed every second, in combat too
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
  opens within, with a clear line to it (0: never). Only the Elder and Bonemass roll it
- **Nightfall** - `range` m from the boss, along the ground, within which a player's sky turns to a storming
  midnight (kept until 10 m past it) and each player gets a tornado; `every` and `every max` the least and most seconds
  of fight between waves, drawn each time (never shorter than `life`; `every` 0 turns them off); `life` seconds from a
  tornado's rising to its breaking up; `form time` seconds it whirls into existence, harmless; `tornado speed` % of a
  player's run speed it hunts at; `damage` a second, as lightning, to a player its funnel touches (armour does not
  reduce it, lightning resistance does); `base width` and `top width` m across at the ground and at its top; `height`
  m tall; `toss distance` m at most that a tornado throws one of the Elder's roots it passes over (at least 60% of that;
  0: never)
- **Brutal** - `launch` m a thrown player flies across flat ground, away from the boss (0: never throws; at most
  40); `lift` m the throw peaks above where they left the ground (kept between 1 and 15). A throw never crosses the
  ground faster than 30 m/s, so a long `launch` with a low `lift` falls short
- **Echoing** - `delay` seconds the ghost runs behind its boss (kept between 1 and 60): it rises that long into the
  fight and replays what the boss did that long before

## Per-creature rules

Each entry under `creatures:` in the rule file is matched by prefab name (`elite reference` lists every one) and may
set any of these keys:

- `drops` - replaces the star `drops` line for this creature
- `multiply trophies` - this creature's own trophy switch (a boss's own trophies ignore it: always one per player near it plus one per star plus one, times Bountiful's loot)
- `drop overrides` - changes rows of its own drop table: `item`, then `amount: [min, max]` (inclusive), `chance`
  0-100, or `remove: true` to delete the row
- `extra drops` - adds rows with `item`, `amount` and `chance`; `per star: true` makes a row follow the loot mode
  like the creature's own rows. In Curated mode these rows are the whole table
- `mutation chance`, `mutation chances`, `mutation power` - the same keys a biome block takes, for this creature only

A creature can have mutation rules of its own. An entry under `creatures:` in the rule file, matched by prefab name,
takes `mutation chance`, `mutation chances` and `mutation power` exactly as a biome block does. They change that
creature only, in whatever biome it is in, and everything they do not name still comes from the biome. This ends
Cloaked wolves and leaves the rest of the Mountain alone:

```yaml
creatures:
  - match: Wolf
    mutation chances:
      Cloaked: [0]
```

`mutation power` changes only the fields it names. `mutation chances` replaces the named mutation's curve.
`mutation chance` replaces the creature's default curve, but any mutation with its own curve in the biome (or in
`defaults`, like Devouring and Gilded) keeps it, so set that mutation to `[0]` under `mutation chances` to stop it.
`mutations enabled` still wins, the world tier still raises the chances (on Custom; on a difficulty the entry decides which mutation the creature gets), and bosses ignore these entries. Two
entries for the same creature merge: later keys win and drop rows add up. A new rule file ships with five: Troll,
Lox and ECP_RimeGiant (Elite Creatures Pack's Rime Giant) raise Cloaked's `reveal distance` to 15, Hatchling (the drake) sets Cloaked to `[0]`, so drakes are
never Cloaked, and Bat sets Mad and Cloaked to `[0]`, so bats are never either. A Deathsquito is never Cloaked
whatever the file says (a rule in the mod, not the file), and one a world saved Cloaked loads without it.

## Respawning

Most creatures guarding a place are placed by the game once, through a spawner that never fires again. Under
`respawning:` in the rule file, cleared camps and dungeons can fill up again, and emptied dungeon chests can refill,
each on its own timer in world days (a world day is 30 real minutes at the game's default speed). All three are off
by default, so clearing a camp or a dungeon stays worth doing.

- **Camps** (`camps`, `camp days` 5) are every such place out in the world: Fuling and Draugr villages, Greydwarf and
  skeleton ruins, Swamp huts, Mistlands dvergr outposts, Ashlands fortresses, drake nests, tar pits and the like.
- **Dungeons** (`dungeons`, `dungeon days` 7) are the interiors entered through a door with a loading screen: Burial
  Chambers, Sunken Crypts, Frost Caves, Infested Mines and the like.
- **Dungeon loot** (`dungeon loot`, `dungeon loot days` 14) refills a dungeon chest from its own loot table once it
  has been fully emptied, never topping up one still being looted. Its clock starts the first time it is seen with
  the switch on. Chests out in the world are never refilled.

Only creatures and dungeon chest contents come back; this is not a location reset, so destroyed buildings, totems,
spawner nests, rocks, ore and pickables stay as they were left. Each spawner works on its own, the game's way: once
the timer has passed since its creature was last seen alive, it spawns again when a player comes near. A creature
still alive elsewhere (wandered off, tamed) is not replaced, a spot inside a player base (near a workbench, a fire)
stays clear, and a spawner the game already times on its own keeps its timer. Places cleared before the switch was
turned on come back too. A change applies as each area loads.

## Death recap

Every death can be watched again. While you play, the mod keeps a small video of the last seconds of your own
screen in memory, and logs every hit you take. When you die, nothing changes about dying or respawning; two seconds
later a line at the top left names the killer and the key. `F10` (or `/deaths`) opens the recap window: your deaths
since the game started (five by default), each with the video of the last 15 seconds and two after, playable at
0.25×, 0.5×, 1× or 2× with a timeline you can drag (hover it for a preview, marks at every hit and at the death),
the killer with its mutations and stars, and every hit with its damage by type and the health left. Space plays or
pauses, the arrow keys step a frame, clicking a hit plays from just before it. The video is recorded and kept only
on your machine, and only with `Record deaths` on (off by default; off keeps the hit list). Settings are in
`10 - Death Recap (per player)`. Design notes in `features/death-recap.md`.

## Boss hints

When Bonemass, Moder, Yagluth or the Elder dies, every player within 100 m of it reads, six seconds later, "You feel
a presence pulling you..." and a compass direction (north, northeast, ... northwest). It is the direction from the
altar that boss was summoned at to a place worth finding next: Bonemass points to the Bog Witch, Moder to the ancient
forge (the Forge of Potential), Yagluth to Haldor and the Elder to Hildir. Where the world has several possible spots
for that place, it is the one nearest the altar; a boss that fell far from any altar of its own (spawned by command)
points from where it fell. A player who has already found that place (its spot explored on their map, by their own
travels or a cartography table) gets no hint. A Twin or Tethered pair gives one hint, a Phantom copy none, and a world
without the place says nothing. Each player turns it off with `Boss hints` in `8 - Display (per player)`.

## Compendium

The game's compendium (the inventory's Compendium button) gets four pages after its own Active effects and Message
log, written each time it opens from the rules the server sent and the world's keys (`Compendium/`, one
`TextsDialog.UpdateTextsList` postfix, `Patches/CompendiumPatch.cs`; a player request for an in-game bestiary,
2026-10-13). Each player turns them off with `Compendium pages` in `8 - Display (per player)`.

- **Elite: World difficulty** - the difficulty and world tier, every counted boss as defeated or not yet, what the
  difficulty means, a table of every biome at this tier (most stars, the share with stars, the share of creatures with
  no stars that are mutated, the player's biome in orange; on Custom from the biome's rows and the tier's boosts), the
  five difficulties side by side, and the game's own World Difficulty: its enemy and player damage on this world and
  that it stacks.
- **Elite: Stars** - health, damage, attack speed, move speed, size and loot for each star count, up to the most this
  difficulty can give anywhere, for the biome the player stands in and any biome whose `star power` differs; then the
  bosses' table with each star count's chance.
- **Elite: Mutations** - each mutation that rolls on this server, its star in the player's colour for it, what it does
  with the numbers of the player's biome for a creature with no stars; the ones switched off by name.
- **Elite: Boss aspects** - each aspect with its loot and the altar's own line for it (`AspectText.Describe`).

## Raids

The game's own raids still come to bases as they always have. Beside them, the **Raiders Chest** (the hammer's Misc
tab, at a workbench: 10 wood, 4 leather scraps, 1 hard antler) sounds a raid on your own base for gold. It holds only
coins, and its hover shows what the coins in it would make of a raid now, for the players near it: "1,000 coins -
Deadly raid (coins back x2, drops x5)". `Shift + E` sounds the raid. It needs a base (3 base pieces - workbench, fire,
bed - within 20 m of the chest), one raid a day per chest (chests within 100 m share the wait), and no other raid,
the game's or a chest's, within 200 m.

**Gold against gear.** Sounding takes every coin out of the chest. The raid's heat is those coins over the fair stake
for the gear of the strongest player within 96 m: the higher of the best metal they have ever held and the armour
they wear.

| Tier | Gear | Fair stake |
| --- | --- | --- |
| 0 | Leather, bone, troll hide; no metal | 100 |
| 1 | Bronze (and the Berserker set) | 200 |
| 2 | Iron, root | 400 |
| 3 | Silver, wolf, fenris | 600 |
| 4 | Black metal, padded, lox | 900 |
| 5 | Mistlands: carapace, eitr | 1,300 |
| 6 | Flametal (and the Deep North) | 1,800 |

| Heat | Raid | Raiders | Extra stars | Mutated | Coins back | Drops |
| --- | --- | --- | --- | --- | --- | --- |
| under 0.5 | Trivial | x0.6 | 0 | as the biome rolls | x1 | x0.1 to x0.5 |
| 0.5 to 1.5 | Fair | x1 | 0 | as the biome rolls | x1.1 | x1 |
| 1.5 to 3 | Hard | x1.5 | +1 | a third | x1.25 | x2 |
| 3 to 6 | Brutal | x2 | +2 | half | x1.5 | x3.5 |
| 6 and up | Deadly | x2.5 | +3 | 9 in 10 (never Gilded) | x2 | x5 |

**The raid.** A 20 second countdown, then three waves of one of the game's raids the world has unlocked by its own
boss rules (those the game sends to the base's biome first), each from one side 40 to 60 m out, never in water:
about half the game's own count of each creature in the first, two thirds in the second, all of it in the third,
times the heat's raiders and a quarter more for each player past the first. At most 20 alive at once, 20 seconds
between waves. Raiders roll stars and mutations as the biome does, plus the heat's; the last wave is led by a
Warlord, its toughest creature with two more stars. Players within 96 m see the raid in the event bar under the
minimap: "Fuling raid (Brutal) - wave 2 of 3 - 5 left - 8:40". Raiders hunt players first; with none, half go for the
Raiders Chest and half for the base's chests and then its crafting stations, breaking through what stands between.

**The gold comes back.** The coins back are shared among every raider of every wave, the Warlord carrying a quarter,
and each drops its share as it dies, however it dies, with its own loot times the heat's drops. A stronger player
arriving mid-raid lowers the drops to what their gear would make of the gold, from then on. Splintering copies carry
no coins.

| End | When | Raiders left |
| --- | --- | --- |
| Won | The last wave is dead: every coin is back, with the extra | - |
| Robbed | The Raiders Chest is broken | Walk off and vanish with their coins |
| Stopped | Hold `E` on the chest for 3 seconds, or `elite raid stop` | All die at once and drop nothing, coins included |
| Abandoned | No player within 96 m for 60 seconds, a logout or a server restart | Walk off and vanish with their coins |
| Timed out | 15 minutes from the start | Walk off and vanish with their coins |

The chest stays locked while the raid is on. What raiders dropped before the end is the players' to keep; one caught
while walking off drops only its own loot. `11 - Raids` / `Raiders Chest` (on) puts the chest in the hammer; off, a
chest already built is an ordinary coin chest. It is synced from the server and held by the rule file's
`lock to server`. Design in `features/raids.md`.

## Horde

More creatures in the wild, each weaker. With `13 - Horde` / `Horde` on (off by default), the world's own spawns
come in groups `Spawn Multiplier` times larger (2, from 1 to 3; fractions round at random), with that many more of
each kind allowed around the players. Every wild creature has `Creature Health Multiplier` (0.65) on its health and
`Creature Attack Multiplier` (0.75) on its damage, and drops 1 / the spawn multiplier of its own loot (half at 2).
Each still rolls its own stars and mutations. Wild means made by the world's spawning (or split from such a creature
by Splintering), tagged in its ZDO whatever the setting, so turning the horde on or off reaches creatures already out
there: health on their next load, attack and loot at once. Bosses, the game's raids, this mod's raids and arena,
creature spawners in camps and dungeons, command spawns and tamed creatures are left as they are. All four settings
are synced from the server and held by the rule file's `lock to server`. Design in `features/horde.md`.

## Arena: Hel's hall

Built 2026-10-12, not seen in game. The hall half of the Arena (Hel's Trial): `features/arena.md` section 1 and the
`Arena` setting (section 5); the trial and the travel have their own code in `Arena/Trial` and `Arena/Travel`. The two
halves meet only in `Arena/ArenaHall.cs` (the hall's origin, `Built`, `TryPoint`, `Contains`) and `Arena/ArenaTrial.cs`
(`Running`, `Wave`, `BossWave`, `RequestStart`), which the hall reads for its doors, runes and Hel's hover.

`12 - Arena` / `Arena` (false) is a Charter clause like the raids' switch (`Config/ArenaSettings.cs`, `Rules/ServerLock.cs`).
While it is on, every peer, the dedicated server included, builds the hall at (0, 5000, 0) as plain local scene objects
(no ZNetView, no ZDO), on the first frame after the world's scene wakes, and removes or rebuilds it when the setting
changes (the server's value through Charter, or the .cfg's hot reload). Everything is on `static_solid`, so players
stand on the floor and the game's pathfinding tiles (y -500 to 5,500) take it. The server builds the colliders and the
doors; the look (materials, runes, fire, lights, the environment, Hel's animation and hover) is built on clients only.

| Bundle (both platforms, `assets/bundles`) | Prefab, where | Bytes (Windows + Linux) |
| --- | --- | --- |
| `ecr_helhall_floor` | the floor, origin; runes `glow_rune_00`..`23` | 2,832,430 + 2,978,572 |
| `ecr_helhall_walls` | the wall ring, origin; doors `door_left`, `door_right`; the points spawn, portal, arrive, gate_inside, flame | 2,475,826 + 2,636,279 |
| `ecr_helhall_throne` | the throne, (0, 0, -14.731); `glow_brazier_0`, `_1`; the points seat_root ... fire_1 | 2,201,082 + 2,370,992 |
| `ecr_helhall_ceiling` | the drum and dome, origin; `glow_oculus`; the point light_oculus | 2,528,776 + 2,672,239 |
| `ecr_helhall_cover` | four broken pillars wrapped in roots and four low walls (cover from the archways), origin; 70 box colliders, no glow, no points (added 2026-10-13) | 1,053,057 + 1,187,308 |
| `ecr_hel` | `ecr_hel` on seat_root; `ecr_hel_spear` from spear_rest to spear_top | 905,905 + 1,077,353 |

Together 24,919,819 bytes (23.8 MB) added to the DLL.

| File (`Arena/Hall/`) | Does |
| --- | --- |
| `HallHost.cs` | One host per world, made by a `ZNetScene.Awake` postfix (`HallScenePatch`) in the world's scene; builds on its first frame, removes and rebuilds on the setting's change; clears `Built` and the hall's forced sky when it goes |
| `HallBuilder.cs` | The four pieces under the host, every part on `static_solid`, the points indexed, the cover (stone-dressed, on every peer for its colliders; the hall stands without it if it fails to load), the doors, Hel, and on clients the runes, fires and environment |
| `HallBundles.cs` | The five embedded bundles through BundlePrefabs' `EmbeddedBundle`; a failure logged once and no hall |
| `HallPoints.cs` | Every part of the four pieces by name, read by `ArenaHall.TryPoint` |
| `HallMaterials.cs` | Stone: `Custom/Piece` from `stone_wall_4x2`'s body `New/stone` (`stone_mat_4x2`; never its first child, the snow overlay), white, gloss 0.15, the pieces' own albedo and normal map. Hel and the spear: `Custom/Creature` from the Draugr's body (`Draugr_mat`), gloss 0.15, her metal mask at 0.65. Anything under a `glow*` child keeps the bundle's emissive material, `_EMISSION` on |
| `HallEnvironment.cs` | `ECR_HelHall`: a copy of `Morkhalla`, fog 0.015 (was 0.1) so the far wall shows from the gate, not cold, freezing or wet; forced by the game's `EnvZone` on a 62 x 34 x 62 m trigger on `character_trigger` |
| `HallFires.cs` | Blue fire and flickering light from `MountainKit_brazier_blue` at the sconces and the throne's braziers, local copies through LocalEffects; a cold spot light in the oculus (table below) |
| `HallRunes.cs`, `RuneLight.cs` | The floor's runes, each its own material: one to three at a time between trials and in the breaks between waves (`ArenaTrial.Break`), more and faster while a wave is fought, all flaring on a boss wave; only changing runes are touched |
| `HallDoors.cs` | The gate's leaves, every peer: open (left -90, right +90 about Y) between trials, shut in one, eased over 1.5 s |
| `HelSeat.cs` | Hel on `seat_root` with the player's `Player_animator` and avatar (`attach_throne`, `onGround`: Sit_Throne_Idle), a 3 x 5.2 x 2.6 m box round her seated body, her spear |
| `HelHover.cs` | "Hel"; "[E] Begin the trial" while no trial runs and you stand in the hall, calling `ArenaTrial.RequestStart`; 3 m more reach than usual |
| `HallWords.cs` | `ecr_arena_hel` "Hel", `hud_ecr_arena_begin` "Begin the trial" |
| `HallNoBuild.cs` | Placing a piece within 40 m (across) of the origin above 4,900 m: the game's no-build status and message (`$msg_nobuildzone`) |
| `TrialView.cs` | `ArenaTrial`'s values (`Running`, `Wave`, `BossWave`, `Break`, giving `Fighting`) read at most four times a second, shared by the doors, the runes and the hover |

Lights and fire (clients only; the brazier's own light is `(0.75, 1, 1)`, intensity 2, range 5, with LightLod (50 m)
and LightFlicker):

| What | Where | Source | Colour | Range | Intensity | Shadows |
| --- | --- | --- | --- | --- | --- | --- |
| Sconce fire | `flame_0`..`7` | `MountainKit_brazier_blue` `_enabled_high/fx_Brazier_flames`, x0.55 | the game's blue flames | | | |
| Sconce light | 0.22 m over each | its `_enabled_high/Point light`, x1.4 | (0.5, 0.92, 0.88) | 7 m | 2, flickering | none |
| Brazier fire | `fire_0`, `_1` | the same flames, x1.35 | the game's blue flames | | | |
| Brazier light | 0.54 m over each | the same light, x1.8 | (0.5, 0.92, 0.88) | 9 m | 2, flickering | soft, 0.7, within 25 m (LightLod) |
| Oculus light | `light_oculus`, pointing down | made in code, spot 70 degrees, with a LightLod (80 m) | (0.62, 0.8, 1) | 34 m | 1.4 | soft, 0.7 |
| Ember glow | `glow_brazier_0`, `_1` | the throne bundle's emissive material | as built (#3aa89a x 0.6) | | | |
| Oculus glow | `glow_oculus` | the ceiling bundle's emissive material | as built (#2a978b x 1.6) | | | |
| Runes | `glow_rune_00`..`23` | the floor bundle's rune material, one copy each | its emission x2.5 lit, x3.5 flaring | | | |

Patches: `ZNetScene.Awake` postfix (`HallScenePatch`), `Localization.SetupLanguage` postfix (`HallWords`),
`Player.UpdatePlacementGhost` postfix (`HallNoBuild`); all three passed the offline patch check.

Not seen yet: the materials and their gloss, the fog, every light's colour and reach, the runes' brightness, Hel on the
throne and her spear's lean, her hover box's size and reach, the doors' swing, the colliders and the pathfinding on the
floor, and a dedicated server building the colliders.

## Arena: Hel's Trial

Built 2026-10-12, released in 3.25.0; rounds with breaks, archway portals, no skill loss in the hall and rewards
added 2026-10-13 after the user's first games, not seen in game. The trial and travel half of the Arena: `features/arena.md`
sections 2 to 4, the `elite arena` and `elite arena stop` commands. It reads the hall only through `ArenaHall` and
fills `ArenaTrial` (`Running`, `Break`, `Wave`, `BossWave`, `RequestStart`).

**The core.** The trial lives in one hidden, persistent networked object, `ECR_ArenaCore`, at the hall's origin: its
prefab is added to `ZNetScene`'s list in a `ZNetScene.Awake` prefix on every peer whatever the setting says, and the
server places one when the world has none (it holds every ZDO: one lookup of the core it knows, else a search of the
one zone at the world's centre; an extra is removed). Its ZDO holds the trial (`ecr_arena_*`, `Arena/Trial/ArenaKeys.cs`):
phase (idle, countdown, fighting, break), trial id (server ms at the start), wave, the last wave cleared or swept,
score, kills, creatures left, the end of the countdown, the wave's 3 minutes or the break, the last time a player was in
the hall, the archway the next wave starts at, the creatures still to come by archway, the archways with portals open,
and the trial's players. Its owner steps the trial four times a second; on a dedicated server that is the server itself (its reference point is the world's centre, where the hall
lies), elsewhere whoever holds the centre zone, and a new owner carries on from the ZDO. Every client reads
`ArenaTrial` from `ArenaCore.Current`, kept as the core wakes: one or two ZDO lookups.

**The trial.** Hel's hover calls `ArenaTrial.RequestStart`, an RPC to the core's owner (a direct call when it is that
machine): with Arena on, the hall built there, no trial running and a living player in the hall, the countdown starts
and every player in the hall is in it; whoever enters later joins. Waves, numbers fixed in `ArenaTable.cs`:

| What | Number |
| --- | --- |
| Countdown | 10 s |
| Biomes | waves 1-10 Meadows, 11-20 Black Forest ... 71-80 Deep North, then the Deep North |
| Creatures in a wave | 4 in the first of a biome, +1 each wave in it, x(1 + 0.25 per player past the first) |
| Extra stars | +1 from a biome's 4th wave, +2 from its 8th, +1 more per ten waves past 80; at most 8 (ECR's maximum) |
| Mutation chance | 5% in wave 1, +3% a wave, at most 60%, for a creature that rolled none; never Gilded (stripped from the base roll too) |
| Archways | each wave's creatures given to the 4 archways in turn; each sends one at a time, 0.8-1.2 s apart, the first 0.6-1.0 s after its portal opens; out of its recess, 0.8 m off the `portal_N` back face, on the recess floor, with `sfx_spawn` |
| Alive at once | 20; the archways wait while 20 live |
| Next wave | only when the wave is cleared (nothing alive or to come, held 2 s), after a break of 15 s (20 s after a boss wave) |
| Swept | a wave not cleared in 180 s: its creatures vanish (no drops, no score) and the break begins |
| Boss wave | every 10th: the boss alone on the rune circle at the floor's centre, with `vfx_spawn` and `sfx_spawn`, no portal |
| Score | a kill 10 x (stars + 1) x biome number (1-8), a boss 500 x biome number |
| End | no living player in the hall for 5 s (or a dedicated server with nobody connected) |

The creatures of a biome are read from the game's own spawn lists once a session (`ArenaSpawnLists.cs`): each
creature's home is the first biome of the trial that a keyless entry names it in, or else a single-biome keyed entry
(the Ticks and Brood after the Queen, the Jotun); entries naming every biome are skipped, so the late invasions of the
Meadows stay in their own biome. Fit: a monster AI, no boss, not water-only (`m_avoidLand`), not provoked-only
(`m_aggravatable`), of a faction hostile to players. A kind as tough as the middle of its biome or weaker comes as often
as any; a tougher one by the square root of the health ratio. When the lists give a biome nothing, the fixed list in
`ArenaFallback.cs` (checked in game 2026-10-12) is used. By that rule this game's lists give (worked out from them on
2026-10-12; the log prints each biome's list at the first wave): Meadows Boar, Neck, Greyling; Black
Forest Greydwarf, Elite, Shaman, Troll, Bjorn; Swamp Draugr, Draugr Elite, Skeleton_Swamps, Blob, BlobElite, Wraith,
Abomination, Writhan; Mountains Fenring, Wolf, Hatchling, StoneGolem; Plains Goblin, GoblinBrute, Lox, Deathsquito,
Unbjorn; Mistlands Seeker, SeekerBrute, Gjall, SeekerBrood, Tick; Ashlands Charred (melee, archer, twitcher), Asksvin,
Morgen_NonSleeping, FallenValkyrie, Volture, BlobLava; Deep North Greydwarf_Frozen, its Shaman, Skeleton_DeepNorth,
Elaking, ElakingLantern, ShadowPerson, Barka, Moose, JotunWarrior, JotunWitch. Bosses: Eikthyr, gd_king, Bonemass,
Dragon, GoblinKing, SeekerQueen, Fader, FrozenKing, then again from wave 90.

**Arena creatures.** Tagged in their ZDO (`ecr_arena_of` trial id, `ecr_arena_wave_of` wave, `ecr_arena_boss`,
`ecr_arena_tier` biome number) in the frame they are made, rolled through `CreatureRoll.For` (via the raids' `RaiderTraits.Roll`, with the wave's stars,
mutation chance and Gilded barred; a boss through its own draw and aspects, plus the stars past 80), set hunting
(`SetHuntPlayer`, no despawn in daylight, target the nearest player in the hall). Every creature that starts inside the
hall gets an `ArenaFighter` on every machine (`MonsterAI.Start` postfix); on its owner, every second: an untagged one
appearing in the hall while a trial runs is adopted (Kall's `FrozenKing_p2` and `_p3`, which his death effects make,
become the wave's boss this way; so do a Summoner's adds, Splintering copies, Twin, Phantom and Echo copies; in the
countdown or a break one joins the coming wave), a tamed one loses its tag, a tagged one whose trial is over (ended,
another, Arena off, no core for 10 s) or whose wave is cleared or swept (`ecr_arena_swept`) vanishes with no death, dropping only what a Thieving one stole, and one with no target is set on the nearest player. Once tagged, on
every machine: the group `ecr_arena` (so one wave's factions never fight each other), and no `m_defeatSetGlobalKey` or
dream (an arena boss sets no world key). Drops: the drop list is emptied last in `LootPatch` (the game's table, the
loot rules, boss rewards and trophies, Gilded's purse, a corpse's loot). A death on its owner sends the score to the
core's owner (an RPC): a copy (Phantom, Echo, decoy) and a boss phase whose death brings the next score nothing. An
arena boss counts for neither `harder each kill` nor the boss hints.

**Portals.** While an archway has creatures to send, the core's ZDO (`ecr_arena_portals`, a bit per archway) says so,
and every client opens a portal there (`ArenaPortals` on the core's object, switched off without a screen;
`ArenaPortal` per archway): the workshop's `ecr_helhall_portal` (bundle `ecr_helhall_portalfx`, both platforms, embedded
by the csproj's `ecr_helhall_*` line) as a local copy through LocalEffects on the walls' `portal_N` point, facing the
hall, with a point light #80ebe0 intensity 1.8, range 7, no shadows, 0.8 m in front, faded in over 0.5 s and out over
1.5 s. A second after the bit clears (the last creature out) the loop's emission stops (it fades by itself, destroyed
after 3 s) and the one-shot `ecr_helhall_portal_close` plays. The effect is one line in `ArenaPortalLook.cs`.

**Death.** A player who dies inside the built hall loses no skill levels: a `Player.OnDeath` prefix on the dying
player's own machine sets the game's time since the last death to 0, so the game gives its own soft death (no
`Skills.OnDeath`, "The Gods are merciful"), and a `Skills.Clear` prefix skips a world's reset-on-death during it.
GrindstoneSkills applies its own skill loss inside `Skills.OnDeath`, so it takes nothing either, with no change there.
After the respawn (`Player.OnSpawned` postfix): "Hel spares your skills." The tombstone stays in the hall as ever.

**Rewards** (`Arena/Rewards/`, `features/arena.md` section 3, "Rewards"). As the trial ends, the core's owner plans one
reward from its ZDO (`RewardPlan`: the waves cleared `ecr_arena_swept`, the boss waves beaten `ecr_arena_bosses`, a bit
per boss wave whose boss fell, the score) and sends it to every player of the trial, dead or alive, one routed RPC
`ecr_arena_reward` each (`RewardSend`; a player offline then gets nothing). Numbers in `RewardTable.cs`:

| Reward | How much |
| --- | --- |
| Idols | per boss wave beaten, the k-th boss's `Upgrader<min(k-1,7)>` `Weapon` (k odd) or `Armor` (k even): Eikthyr Wooden Battle, the Elder Bronze Protection ... Kall Bloodgold, then Bloodgold again |
| Coins | score / 10 (`Coins`) |
| Skill XP | each skill's raises in the hall during the trial again, x (waves cleared / 20), at most x2 |
| Runes (EliteCrafting) | per regular wave cleared 1, 2, 2, 2, 2, 2, 3, 3 by biome; per boss wave beaten 5, 3, 4, 4, 5, 6, 7, 7; drawn by EliteCrafting's default `drops.runes` weights at the wave's tier, chisel left out |
| Essence (EliteCrafting) | per regular wave cleared 2, 4, 6, 8, 10, 12, 14, 16; per boss wave beaten 20, 30, 40, 50, 60, 70, 80, 90 |

EliteCrafting is detected by ValheimModLibs' EliteCraftingLink (`CraftingLink.Present`: its plugin `com.EliteCrafting` in
the chainloader with API version 1 or later; nothing referenced), now merged into ECR; its item names are the link's
`CraftingPrefabs` constants. Every item is looked up in `ObjectDB` and left out when missing. On the player's machine
(`RewardGrant`) items go into the inventory, what does not fit falls at their feet; a dead player's reward waits for the
respawn (`ArenaDeath`'s `Player.OnSpawned` postfix). The XP bonus (`SkillBonus`) comes from what that machine recorded
(`SkillLedger`, a `Skills.RaiseSkill` prefix at `Priority.First`, before any mod scales the factor: each skill's factors
summed and its raises counted while the local player was in the hall during the running trial) and goes back through
`Skills.RaiseSkill` in steps of the trial's average raise (at most 2000 per skill), since the game drops what one raise
earns past the next level. The message `ecr_arena_reward` "Hel rewards you: $1." with the game's item names and
`ecr_arena_reward_coins`, `_runes`, `_essence`, `_xp` ("$1 coins", "$1 runes", "$1 essence", "+$1% skill XP") comes
four seconds after the end message (`RewardText`).

**Messages and HUD.** Start, boss, cleared and end go by routed RPC to the trial's players only, as a word key and up
to three words (`ArenaWords.cs`: `ecr_arena_started` "Hel wakes. Her trial begins!", `ecr_arena_boss` "Hel sends $1!",
`ecr_arena_cleared` "Wave $1 cleared. The next comes in $2 seconds.", `ecr_arena_ended` "Hel's Trial ends at wave $1:
$2 kills, score $3.", `ecr_arena_line` "Hel's Trial - wave $1 - $2 left - $3", `ecr_arena_break_line` "Hel's Trial -
wave $1 cleared - next wave in $2 - $3", `ecr_arena_countdown` "Hel's Trial - the first wave in $1", `ecr_arena_spared`
"Hel spares your skills.", `ecr_arena_name` "Hel's Trial"). The line is the game's event bar for a living player in the
hall, drawn from the core's ZDO by the raids' one `Hud.UpdateEvent` hook, which asks the trial first.

**Travel.** `ArenaTravel.Enter` and `Leave`, for Helgrind (below) and `elite arena` (admin; the host in single player):
in, the return point goes into the player's custom data `ecr_arena_return` (`x,y,z,yaw`; an older `x,y,z` still reads)
and the player is jumped to `arrive` (`TeleportTo`, not distant: no loading screen); out, back there facing as kept with
the normal loading, or to the bed or the world's spawn. The way in is refused while Arena is off or the
hall is not built here; the way out never is. `elite arena stop`: to the core's owner, or through the server when the
core is not loaded here; it ends the trial as an empty hall does.

| File (`Arena/Trial/`, `Arena/Travel/`) | Does |
| --- | --- |
| `ArenaKeys.cs`, `ArenaTable.cs`, `ArenaPhase.cs` | Keys and RPC names; every fixed number and the sums from them; Idle, Countdown, Fighting, Break |
| `ArenaState.cs` | The trial in the core's ZDO (as `RaidState`) |
| `ArenaCore.cs`, `ArenaCorePrefab.cs`, `ArenaCoreKeeper.cs` | The core (RPCs, the owner's step, `Current`); its prefab and `ZNetScene.Awake` prefix; the server keeping exactly one (`ZoneSystem.Start` postfix) |
| `ArenaStart.cs`, `ArenaRun.cs`, `ArenaWaves.cs`, `ArenaEnd.cs` | Hel wakes; one step (presence, ends, countdown and breaks, a wave and its clear); a wave planned, a wave over (cleared or swept) and the break; the end |
| `ArenaQueue.cs`, `TrialPlayers.cs`, `HallPlayers.cs` | The creatures to come by archway and the trial's players in the ZDO (decoded once per write); the living players in the hall |
| `ArenaCreatures.cs`, `ArenaSpawnLists.cs`, `ArenaFallback.cs`, `ArenaBosses.cs` | Biome lists and weights; homes from the spawn lists; the fixed lists; the bosses and Kall's next phase |
| `ArenaSpawn.cs`, `ArenaSpots.cs`, `ArenaRoll.cs`, `ArenaHunt.cs`, `ArenaTag.cs` | Each archway's next creature, a boss (spot, sound or effect, tag, roll, hunt); the recess and rune circle spots; the roll; hunting; the tag |
| `ArenaPortals.cs`, `ArenaPortal.cs`, `ArenaPortalLook.cs` | The clients' portals from the core's ZDO; one archway's portal and light; the effect, its close and the light |
| `ArenaDeath.cs` | No skill loss in the hall: `Player.OnDeath` prefix and finalizer, `Skills.Clear` prefix, `Player.OnSpawned` postfix (also pays a reward waiting for the respawn) |
| `Arena/Rewards/RewardTable.cs`, `RewardList.cs`, `RewardPlan.cs`, `RewardRunes.cs`, `RewardSend.cs` | The numbers; the reward as a package; planning it on the core's owner; EliteCrafting's runes and essence; sending it |
| `Arena/Rewards/RewardGrant.cs`, `RewardText.cs`, `SkillLedger.cs`, `SkillBonus.cs` | On the player's machine: adding it, its message, the XP recorded in the trial (`Skills.RaiseSkill` prefix), the XP bonus |
| `ArenaFighter.cs`, `ArenaFighters.cs`, `ArenaOrders.cs`, `ArenaAttachPatch.cs` | The hall's hold on a creature; the list the owner counts; adopt, release, vanish, keep hunting; the `MonsterAI.Start` postfix |
| `ArenaScore.cs`, `ArenaLoot.cs` | The score at death (`Character.OnDeath` prefix `ArenaDeathPatch`); the emptied drop list |
| `ArenaNet.cs`, `ArenaWords.cs`, `ArenaHudLine.cs` | Messages and the stop relay (`ZoneSystem.Start` postfix); the words (`Localization.SetupLanguage` postfix); the HUD line |
| `Arena/Travel/ArenaTravel.cs`, `Commands/ArenaCommand.cs` | In and out; `elite arena [stop]` |

Outside `Arena/`: `Raids/Traits/RaiderTraits.cs` has a second `Roll` (extra stars, mutated share, barred mutations
given outright; the raid's own calls it unchanged), `Raids/Hud/RaidHudPatch.cs` asks `ArenaHudLine` first,
`Patches/LootPatch.cs` calls `ArenaLoot` last, `Runtime/BossKills.cs` and `Patches/BossHintPatch.cs` skip arena bosses,
`Commands/EliteCommands.cs` routes `arena`. New patches (all passed the offline patch check): `ZNetScene.Awake`
prefix, `ZoneSystem.Start` postfixes (keeper, messages), `MonsterAI.Start` postfix, `Character.OnDeath` prefix,
`Localization.SetupLanguage` postfix, and (2026-10-13) `Player.OnDeath` prefix and finalizer, `Skills.Clear` prefix,
`Player.OnSpawned` postfix, `Skills.RaiseSkill` prefix. The csproj and `ILRepack.targets` merge EliteCraftingLink.

Not seen yet: everything. In particular a dedicated server owning and running the trial, the creatures' paths on the
hall floor, Moder flying under the dome, the Queen and Kall's phases being adopted in time, the spawn effect, the HUD
line, the messages, the travel's no-loading jump in and the return; and from 2026-10-13: the breaks and their line,
the 3-minute sweep, large creatures (Troll, Lox, Morgen, Gjall) made in a 1.4 m deep recess, the portals' look, light
and timing against the creatures, and an arena death keeping every skill level (GrindstoneSkills' too); the rewards
(the idols' names in the message, the items given or dropped, the reward at the respawn, the XP bonus's size with
GrindstoneSkills' rates, EliteCrafting's runes and essence present and absent).

## Arena: Helgrind

Built 2026-10-12, not seen in game. The entrance (`features/arena.md` section 2): **Helgrind**, Hel's gate, piece
`ECR_Helgrind`, a copy of the game's `portal_stone` without its pairing, in the hammer's Misc tab while `Arena` is on,
built at the Black Forge (`blackforge`) from `FrozenKingDrop` 1, `FrostCore` 2, `Gold` 10, `MemorialCoal` 5. Walking
into its opening jumps the local player into the hall through `ArenaTravel.Enter`, keeping the gate's `exit` point (1 m
up, facing away) as the way back. The gate is about the game's wood portal's size: 4.61 x 4.20 x 1.94 m, 4.03 m above
the ground, an arched opening 2.44 x 3.22 m whose centre is 1.73 m over its sill; the wall at the end of the hall's gate passage is the way out (`ArenaTravel.Leave`).

| Bundle (both platforms, `assets/bundles`) | What | Bytes (Windows + Linux) |
| --- | --- | --- |
| `ecr_helgate` | prefab `ecr_helgate`: `model`, glow `glow_frostcore_0`, `_1`, `glow_coal`, `glow_runes`, points `portal_center` (0, 1.73, 0) facing +Z, `light` (0, 1.73, 0.2475), `exit` (0, 0, 2.254) facing away, eight `col_box_*` (`post_l`, `post_r`, `lintel`, `keystone`, `base`, `step_front`, `brazier_l`, `brazier_r`); sprite `ecr_helgate_icon` | 352,344 + 521,960 |
| `ecr_helgatefx` | effect `ecr_helgate_swirl` (fills the arched opening round its origin, facing +Z, loops, local space, no light) | 114,306 + 126,237 |

| File (`Arena/Gate/`) | Does |
| --- | --- |
| `GatePrefab.cs` | `ZNetScene.Awake` prefix, every peer: `portal_stone` copied on the bench; `TeleportWorld` removed (no tag, no link; the name is in none of the game's portal lists), `TELEPORT`'s `TeleportWorldTrigger` swapped for `GateTrigger` and its box moved to `portal_center`, unscaled, size (2.24, 3.12, 0.40) (`GateModel.Opening`), `_target_found_red` stripped to its `SFX` hum under its `EffectFade`; Misc tab, name, icon |
| `GateModel.cs` | The bundles (EmbeddedBundle, a failure logged once); `New`, `Mesh collider` and `floor collider` replaced by the gate on the piece's layer; one object for every wear state; the point names, their values as built (used only without the model) and `PointIn`, which reads a point from a piece |
| `GateMaterials.cs` | Clients: the hall's stone (`Custom/Piece` from `stone_wall_4x2`'s `New`, white, gloss 0.15) with the Bloodgold metal mask at 0.76 (the Nord Buckler's); glow parts keep the bundle's emissive material |
| `GateHammer.cs` | Hammer, cost and the Black Forge in every ObjectDB, in or out with `Arena` (as `ChestHammer`) |
| `GateTrigger.cs` | Walk-in, the walker's machine: closed ("Hel's gate is closed.") while Arena is off or the hall not built; `$msg_noteleport` when `IsTeleportable(false)` fails (the game's per-item rule); else `ArenaTravel.Enter` with the exit point |
| `GateLook.cs` | Clients, not on a ghost: while Arena is on, the swirl at `portal_center`, the light at `light`, the hum faded in |
| `GateSwirl.cs` | The swirl, dressed once (BundleEffects) and copied local-only (LocalEffects), facing its anchor; its point light #80ebe0, intensity 2, range 6, soft shadows, with a LightLod |
| `GateWayOut.cs`, `WayOutTrigger.cs` | The way out, clients only, built with the hall: at 1.6 m behind `arrive` (the walls' back wall, r 29.5; no point in the bundle), facing into the passage, the swirl seated as in the gate with its sill on the passage floor: centre at `portal_center`'s height (1.73 m) and 0.15 m off the wall, its light `light`'s 0.2475 m further out (both read from the gate prefab), range 6; a 2.24 x 3.12 x 0.7 m trigger on `character_trigger` on the same centre (0.9 m clear of `arrive`) calling `ArenaTravel.Leave` |
| `GateHover.cs` | "Helgrind" and "Walk through into Hel's hall" or "Hel's gate is closed." |
| `GateWords.cs` | `piece_ecr_helgrind`, `_description`, `ecr_helgrind_open`, `ecr_helgrind_closed` (`Localization.SetupLanguage` postfix) |

Outside `Arena/Gate/`: `Arena/Hall/HallBuilder.cs` calls `GateWayOut.Build` in `Look` (one line);
`Arena/Travel/ArenaTravel.cs` has `Enter` and `Leave`; `Arena/Trial/ArenaKeys.cs` documents the return point's yaw;
`Config/ArenaSettings.cs` names Helgrind. Patches: `ZNetScene.Awake` prefix (`GatePrefab`), `ObjectDB.Awake`,
`ObjectDB.CopyOtherDB` and `ZNetScene.Awake` postfixes (`GateHammer`, the Raiders Chest's targets), and
`Localization.SetupLanguage` postfix (`GateWords`); the offline patch check passes.

Not seen yet: the gate's look and stone, its glow, the swirl's size and direction in the opening and on the passage's
back wall, the trigger boxes, the jump in and the return to the gate, the closed and no-teleport messages.

## Console commands

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns exactly that creature, bypassing every roll, for testing. A boss takes one aspect instead: `elite spawn Bonemass 2 Twin`; Bountiful takes its extras after it, or rolls them when none follow: `elite spawn gd_king 2 Bountiful Enraged Mending` |
| `elite inspect` | Prints the resolved stars, mutations or aspect, and numbers for the creature under your crosshair, with what a Thieving creature carries and what a Devouring creature has eaten ("devoured 1 of 2: Boar"). Open to every player |
| `elite purge [radius]` | Removes the loaded creatures this mod has marked, with no drops (a Thieving creature's stolen goods drop first). With a radius in metres, only those that close to you: `elite purge 30` |
| `elite effects <text>` | Lists loaded effect prefabs matching the text and plays one, for building visuals |
| `elite reference` | Writes `creature_reference.yml`: every creature the game knows, by biome, with its drop table |
| `elite tier` | Shows the world tier, what it does to the rolls, and which bosses count. Open to every player; the other `elite` commands but `inspect` are admin only |
| `elite raid start <coins> [tier]` | Sounds a raid where you stand with that many coins and no chest, for testing: the heat is weighed against the gear tier given (0-6), or else the strongest player within 96 m. Skips the chest's base test and daily wait, not the 200 m between raids |
| `elite raid stop` | Stops the nearest raid within 200 m, as holding `E` on its chest does: every raider dies and drops nothing |
| `damage` | Shows the latest boss damage board again. Open to every player, and typed as `/damage` in chat |
| `deaths` | Opens the death recap window, like its key. Open to every player, and typed as `/deaths` in chat |

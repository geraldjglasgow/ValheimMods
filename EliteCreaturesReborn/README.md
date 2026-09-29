# Elite Creatures Reborn

Creatures spawn with **mutations** — named, visible traits that change how a fight goes. A Mad greydwarf comes at
you twice as fast on half the health. A Bloated draugr takes twice the killing and then explodes in your face. A
Devouring wolf eats the boars around it and grows on what it eats until it comes looking for you.

Bosses take an **aspect** instead: one modifier, shown at the altar before you summon, that changes what kind of
fight it is. A Twin Bonemass comes as two sharing one health pool; a Reflective Moder hurts you with every hit you
land.

The world hardens as bosses fall: every boss's first defeat raises the **world tier**, and each tier makes stars and
mutations more common everywhere. Tamed creatures **pass their traits on**: a newborn inherits a mutation and stars
from its parents.

Rebuilt from scratch. This release covers mutations, stars, world tiers, breeding, loot and boss aspects; the rest
returns over the next releases.

## Mutations

Thirteen, one per creature by default, each with its own colour and its own name on the nameplate.

| Mutation | What it does |
| --- | --- |
| Mad | Far faster, half health |
| Bloated | Double health, explodes 1.7 seconds after it dies |
| Cloaked | Invisible beyond 10 metres (15 for trolls and lox); drakes are never Cloaked |
| Splintering | Splits into two weaker copies when killed, which can split again |
| Leeching | Regenerates, and heals from the damage it deals |
| Warding | Reflects part of each hit back, never more than 7.5% of your maximum health in any second, and knocks you back |
| Plated | Armoured while healthy, hits harder as that armour goes |
| Miasmic | Trails poison clouds; poisons players, never creatures |
| Devouring | Kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you. It eats only creatures with at most 125% of its own health, one per star in its life (at least one), and shows each one it ate on its nameplate |
| Thieving | Steals an item with each melee hit that lands (not thrown stones, not a parried or dodged blow), up to one per star (at least one), and carries them on its nameplate; kill it to get everything back |
| Gilded | Glitters gold, never attacks a player and runs from any it sees; drops three times its loot plus a purse of coins |
| Blinking | Every 30 seconds of a fight it reappears behind its target, after a flash and a chime at the spot; 25% less health |
| Relentless | Once it picks you it keeps coming, seen or not, until you are 150 m away; sneaking does not hide you; never faster than its base speed |

Gilded is the rarest, on purpose, and the only one in your favour: it runs, and pays well if you catch it.

## Mutation power fields

`mutation power` in the rule file sets how strong each mutation is, in named fields rather than positional
numbers. A field marked (enhanced) is multiplied by `large star power` on a large star; a cost field never is.
Any mutation can also be switched off entirely with `mutations enabled`, regardless of its chance curves.

- **Mad** - `move`/`attack speed` multipliers (enhanced); `health` multiplier, its cost
- **Bloated** - `health` multiplier (enhanced); `delay` seconds from death to the blast, 1.7 by default; the blast goes
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
  may have for it to hunt and eat it, as a percent of its own current health, 125 by default (0 lifts the limit; not
  enhanced); `min meals` the fewest creatures it eats in its life whatever its stars, 1 by default (not enhanced) - it
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

## Stars

Beyond vanilla's two, counted in fives on the nameplate: a small star is one, a large star is five. A mutation on a
large star is stronger. Star chances and mutation chances are set per biome, so the Meadows stay the Meadows.

Bosses have their own star table, the same for the whole world, and nine in ten stay plain by default. A boss's
stars are drawn under its health bar at the top of the screen.

## World tiers

The world starts at tier 0 and goes up one tier the first time each boss is defeated - by anyone, in any order -
so the seven bosses take it to tier 7. Killing a boss again changes nothing. Every rise is announced to everyone
on the server.

Each tier makes stars and mutations more common in every biome while keeping each biome's character. At the default
settings, by tier 7 the share of unstarred Meadows creatures falls from 73 in 100 to 44, five-star Plains creatures
rise from 5 in 100 to 22, and every mutation chance doubles. Bosses are unaffected, and creatures already alive keep
what they rolled - the tier decides what the next one rolls. A world that killed bosses before the mod was installed
starts at that tier.

The inventory shows the tier in a box with a globe ("3"), under the armor and weight in a column of small boxes down
the outside of the inventory panel's right edge. Every box in the column names itself when hovered, and the tier's
says how many tiers there are in total. With OpenKeep installed its trash plate joins the same column. The tier also
shows in a small box under the minimap, so it is in view without opening the inventory. `Show world tier` and
`Show world tier under minimap` in the .cfg hide the one or the other for one player.

`elite tier` shows any player the current tier, what it is doing to the rolls, and which bosses count. The
`world tiers:` block in the rule file has the off switch, the list of bosses that count (a modded boss counts once
its defeat key is listed; `elite tier` shows every boss's key), and the star and mutation boost for each tier.

## Breeding

Tamed creatures keep their traits and pass them on. A newborn - a pup, a piglet, a calf, or an egg and the chick
that hatches from it - takes one of its parents' mutations whenever either has one, and a star count from 0 up to
its stronger parent's, every count equally likely. Two plain parents have plain young and nothing new is ever
rolled, so a strong line stays strong only if you keep the good ones. With GrindstoneSkills installed, its Husbandry
skill's "Better offspring" chance gives a newborn one star more than it rolled, up to one above its stronger parent.

Young creatures keep their traits when they grow up. Eggs keep theirs when carried, and say what they will hatch
("Hatches with 2 stars, Leeching") on their hover text and tooltip; an egg's stars are its quality, so eggs of
different stars do not stack. A bred mutation behaves exactly as it does in the wild - a Miasmic pet
still trails poison. The `breeding:` block in the rule file has an off switch and the mutation chance (100 by
default).

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

The chances are weights: 30 for the plain fight and 10 for each of the thirteen aspects, so about one boss fight in
five stays as the game ships it. Stormbound's lightning, Gravitic's slam and Colossal's shockwave are dodged, not
blocked: a roll timed through them avoids them, and a raised shield does not.

Everything is in the `aspects:` block under `bosses:` in the rule file: the off switch, the shift interval (0 fixes
each altar), the chance of each outcome, the loot multiplier, every aspect's numbers, and per boss the creatures
Summoner calls and, optionally, which aspects that boss may roll.

## Boss damage board

When a boss dies, everyone on the server sees who fought it: the boss's name and every player who hurt it, with the
health each one took off it, most first, at the far left of the screen, halfway down, for a minute. It counts what
the boss actually lost - after its resistances, without the overkill - and only players count, not tames or summons.
Twin bosses are one fight and one board. On a Phantom boss's board, the health players took off its copies counts
too, including copies killed early. Each player can turn it off (`Boss damage board`) or change how long it stays
(`Boss damage board seconds`) in the .cfg.

Type `/damage` in chat, or `damage` in the F5 console, to see the latest board again for the full time, even if it
has faded or you turned the board off. Any player can. A player who joined after the kill gets the board from the
server, which remembers the latest one until it restarts.

## New creatures

The crypt mimic, the Greydwarf Slinger, the Rime Giant and the Kraken are a separate mod, **Elite Creatures Pack**.
Neither needs the other; with both installed they roll stars and mutations like any creature, and a mimic shows none
of it until it wakes. The Kraken is a boss here, with boss stars and an aspect, but never Twin or Phantom: it holds
one ship with one health bar.

## Loot

What a kill drops is governed by a `loot:` block in the rule file. Four modes, chosen for the whole world:
**Vanilla** (drops untouched), **Scaled** (quantities raised by the per-star `drops` line), **Rolled** (the
creature's own drop table rolled once more per star, each roll independent — the default, so a hard fight has a
real chance at the rare thing), and **Curated** (per-creature rules decide everything). An `extra roll chance`
line and a `max extra rolls` cap tune Rolled; a global multiplier and a separate boss multiplier scale everything
after the mode, and a boss's aspect multiplies its drops on top - even in Vanilla mode. A wild Gilded creature is
the one mutation that pays: its `loot` multiplier and coin purse apply in every mode, Vanilla included. Trophies are
never multiplied unless you switch that on — one kill, one trophy.

Per-creature rules in the same file, matched by prefab name, override any of it: a creature's own drops line,
adjusted or removed rows of its drop table, extra drops with their own chance and amounts. `elite reference`
writes `creature_reference.yml` with every creature the game knows — modded ones included — under its exact
prefab name with its vanilla drop table; paste it and `creature_rules.yml` at an AI assistant, describe the
economy you want, and drop the rules it writes back into the file.

## Configuration

Two files, both written and documented on first run, both hot-reloaded while you play:

- `BepInEx/config/gglasgow.elitecreaturesreborn.cfg` — each player's display preferences: star colours and sizes,
  whether trait names show, nameplate distance, effect density, stolen-item and devoured-creature icons, the boss
  damage board, the world tier plate and the tier box under the minimap, and a diagnostics switch.
- `BepInEx/config/creature_rules.yml` — the rules: star chances, star power, mutation chances and strength per biome
  and per creature, a `mutations enabled` switch that turns any mutation off everywhere, and the boss stars and
  aspects, world tiers, breeding, loot and respawning blocks, each with its own off switch.

A saved rule change reaches creatures already in the world as they act (a mutation's strength, Warding's cap); their
stars, mutations, health, size and speed stay as they were until they next load.

A server binds connected players to its rule file. Display preferences stay with each player and are never locked.

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
`mutations enabled` still wins, the world tier still raises the chances, and bosses ignore these entries. Two
entries for the same creature merge: later keys win and drop rows add up. A new rule file ships with three: Troll
and Lox raise Cloaked's `reveal distance` to 15, and Hatchling (the drake) sets Cloaked to `[0]`, so drakes are
never Cloaked.

## Console commands

| Command | Does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation...]` | Spawns exactly that creature, bypassing every roll, for testing. A boss takes one aspect instead: `elite spawn Bonemass 2 Twin` |
| `elite inspect` | Prints the resolved stars, mutations or aspect, and numbers for the creature under your crosshair, with what a Thieving creature carries and what a Devouring creature has eaten ("devoured 1 of 2: Boar") |
| `elite purge [radius]` | Removes the loaded creatures this mod has marked, with no drops (a Thieving creature's stolen goods drop first). With a radius in metres, only those that close to you: `elite purge 30` |
| `elite effects <text>` | Lists loaded effect prefabs matching the text and plays one, for building visuals |
| `elite reference` | Writes `creature_reference.yml`: every creature the game knows, by biome, with its drop table |
| `elite tier` | Shows the world tier, what it does to the rolls, and which bosses count. Open to every player; the other `elite` commands are admin only |
| `damage` | Shows the latest boss damage board again. Open to every player, and typed as `/damage` in chat |

## Install

With a mod manager, install it. By hand, drop `EliteCreaturesReborn.dll` into `BepInEx/plugins`. Requires
BepInEx. On a server, install it on the server and on every client.

## Multiplayer

Creature state travels in the world data the game already shares, so every player sees the same creature with the
same traits. Install it on the dedicated server as well as the clients.

## Files

- `plugins/EliteCreaturesReborn.dll` — the mod, a single merged assembly
- `config/gglasgow.elitecreaturesreborn.cfg` — display preferences, per player
- `config/creature_rules.yml` — creature rules

## Building

.NET SDK 8, with Valheim installed:

```
dotnet build EliteCreaturesReborn/EliteCreaturesReborn.csproj -c Release
```

Override `-p:GamePath=...`, `-p:BepInExCore=...` or `-p:ModLibsPath=...` if your layout differs. The build merges
the shared libraries into one DLL and writes it to `dist/`.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you
distribute that is built from it must carry the same freedoms and be released under the same licence, with source.
See the `LICENSE` file for the full terms.

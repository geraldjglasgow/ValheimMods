# Boss Aspects

Bosses never mutate. Each takes an **aspect**, a modifier for the whole fight, shown in its name ("Enraged Eikthyr").

## At the altar

Hover the offering bowl to see the boss's stars, its aspect, what that does, what it pays and when it shifts. Every 15
seconds the altar shifts: the stars roll again and the aspect changes to a different one, so you can wait for the fight
you want. What the bowl shows when you make the offering is what you fight. Bosses without an altar (the Queen, a
console spawn) roll both when they appear.

## The aspects

Settings are under `bosses:` > `aspects:` > `power:` in `creature_rules.yml`.

| Aspect | What it does | Settings (defaults) | Loot |
| --- | --- | --- | --- |
| none | The normal fight. | | x1 |
| Reflective | Part of each hit you land comes back to you. | `reflect` 15% | x1.4 |
| Shielded | Takes less damage from arrows and bolts. | `arrow reduction` 30% | x1.1 |
| Mending | Regenerates health, even in combat. | `regen` 0.3%/s | x1.3 |
| Summoner | Calls 2-star helpers from its biome as it loses health. | `every` 33% lost, `count` 2, `stars` 2 | x1.5 |
| Elementalist | More fire, frost, lightning, poison and spirit damage. | `elemental bonus` 20% | x1.2 |
| Enraged | More physical damage. | `physical bonus` 20% | x1.2 |
| Twin | Two copies sharing one health pool, each weaker; both drop loot. | `less health` 25%, `less damage` 25% | x1 each |
| Phantom | Splits off weak copies (one per player) at set health points and hides among them: it swaps to a random place in their ring, the copies carry its name and its share of health, and all the bars sit small in one row in random order. Hit them to find it. Copies drop nothing. | `split at` [66, 33], `per player` 1, `health per tier` 25, `less damage` 50% | x1.3 |
| Adaptive | Resists the damage type that hurt it most lately and glows that colour. | `resist` 50%, `window` 15 s | x1.3 |
| Fixated | Marks one player and hits them harder, everyone else softer. | `marked bonus` 50%, `others less` 30%, `every` 30 s | x1.3 |
| Stormbound | Lightning strikes a circle under each player; roll out of it. A player inside a dungeon is safe from a boss outside it. | `every` 20 s, `tell time` 2 s, `range` 40 m, `radius` 2.5 m, `damage` 8% of max health | x1.2 |
| Gravitic | Pulls players in, then slams; roll to dodge. | `every` 20 s, `range` 30 m, `pull time` 1.5 s, `pull speed` 6, `slam radius` 6 m, `slam damage` 10% | x1.3 |
| Colossal | Bigger, tougher, slower; heavy blows knock nearby players down (roll or jump). | `bigger` 40%, `more health` 15%, `slower` 15%, `shockwave radius` 8 m | x1.2 |
| Tethered | Two linked bosses with separate health; the further apart their health, the faster they attack and the tougher the weaker one gets. Only the last to die drops loot, and the damage board shows then, counting both. | `less health` 25%, `less damage` 25%, `attack speed` 50%, `armour` 50%, `full gap` 50 | x1 |
| Bountiful | Carries two extra aspects at once, and drops twice the boss trophies. | `extra aspects` 2 | x2 times the extras' |
| Portalbound | The Elder and Bonemass only: the Elder's vines fly, Bonemass's slime ball arcs, at you out of a portal. | `min height` 5 m, `clearance` 2 m, `range` 20 m | x1.2 |
| Nightfall | Storming midnight for every player within 60 m: rain, thunder, Wet and Cold as in a real night storm (shelter, fire and frost resistance help). The day returns once it dies or you go 10 m past that range; the world's time and weather never change. Every 18-28 s of fight a tornado rises 6-9 m in front of each player, harmless for 1.5 s, then hunts them at 40% of run speed until 10 s after it rose. Its funnel deals 25 lightning damage a second; armour does not help. It tosses the Elder's roots. | `range` 60 m, `every` 18 s, `every max` 28 s (older rule files keep their own), `life` 10 s, `form time` 1.5 s, `tornado speed` 40%, `damage` 25, `base width` 1.5 m, `top width` 9 m, `height` 14 m, `toss distance` 10 m | x1.3 |
| Brutal | Its heavy blows throw the players they hit far away; the hit does its damage, the landing none. A roll, or a block or parry that holds, keeps you on your feet. Never while swimming, seated, riding or on a ship's deck. | `launch` 20 m (0 = never, at most 40), `lift` 3 m high (1 to 15) | x1.2 |
| Echoing | 15 s into the fight a white, nearly clear ghost of the boss rises where the boss stood then, and from then on does everything the boss did 15 s before: walks its path, turns, swings, throws, staggers. Its blows hurt as the boss's did, aimed where the boss's target stood then. It cannot be hit, shows no health bar, and goes when the boss dies. | `delay` 15 s (1 to 60) | x1.3 |

The loot multiplier applies in every loot mode. About one fight in five has no aspect. Stormbound, Gravitic, Colossal
and Nightfall are dodged with a roll, not blocked; Brutal's throw is the one a shield stops. Echoing's ghost is beaten
by moving: it strikes where you stood 15 seconds ago.

## What Summoner calls

| Boss | Summons |
| --- | --- |
| Eikthyr | Boar, Neck |
| The Elder | Greydwarf Brute, Greydwarf Shaman |
| Bonemass | Draugr Elite, Oozer |
| Moder | Drake |
| Yagluth | Fuling Berserker, Fuling Shaman |
| The Queen | Seeker Soldier, Seeker |
| Fader | Charred Warrior, Charred Marksman |

## Boss stars

Bosses have their own star table, the same on every difficulty and tier, rolled at the altar with the aspect. Their
speed never changes.

| Stars | 0 | 1 | 2 | 3 |
| --- | --- | --- | --- | --- |
| Chance | 90% | 6% | 3% | 1% |
| Health | x1 | x1.5 | x2.25 | x3.4 |
| Damage | x1 | x1.25 | x1.55 | x1.9 |
| Size | - | +5% | +10% | +15% |

Stars add trophies only; the rest of a boss's drops is never multiplied by stars. A boss drops one trophy for every player within 100 m of it, plus one per star plus one, whatever the loot settings; Bountiful doubles that. Three players and a two-star Bountiful boss: twelve trophies.

## Damage board

When a boss dies, a board on the left lists how much damage each player did. A Twin or Tethered pair gets one board
for both. `/damage` in chat shows it again.

## Settings

Under `bosses:`:

| Key | Default | Meaning |
| --- | --- | --- |
| `stars` | true | false: bosses never get stars, and the altar shows none |
| `star chances` | [90, 6, 3, 1] | % chance of 0, 1, 2, 3 stars |
| `star power` | the table above | Boss star multipliers (no `drops` line; an old one only warns) |
| `aspects:` `enabled` | true | false: no aspects |
| `shift seconds` | 15 | Real seconds between altar shifts (stars and aspect); 0 = never changes. An older file's `shift hours` (in-game hours, 75 s each) still works while `shift seconds` is absent |
| `chances` | none 42, each aspect 10 | Weight of each aspect; 0 removes it |
| `loot` | the Loot column | Loot multiplier per aspect |
| `per boss` | | Per boss: `summons` (what Summoner calls) and optional `aspects` (the only ones it may roll) |

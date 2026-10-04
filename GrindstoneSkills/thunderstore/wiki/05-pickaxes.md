# Pickaxes Skill

The game's own skill (mining damage and swing stamina); your level counts. **Trained by** swings on rock, plus clean strikes and discovering each kind of ore deposit. **Ore deposits** are rocks that drop anything besides plain stone; only they get vein stars, extra ore, Echo pings and the ore experience bonus.

| Level | Milestone | What |
| --- | --- | --- |
| 25 | Read the rock | a deposit's hover shows its vein stars and chunks left |
| 50 | Echo | a swing on rock pings the nearest ore deposit not left to the Wishbone (name and distance) |
| 100 | Unbroken | clean strikes in a row hit harder, up to ×4 |

## Seams (15 - Seams)

A swing on a many-chunk rock may open a **seam**: a nearby chunk glints gold, for you only. Hit it in time for a **clean strike**: double damage, a second drop roll on ore, and the next seam opens at once. Missing ends the chain.

| Setting | Default | Meaning |
| --- | --- | --- |
| Seam Chance At 0 | 10 | % of swings that open a seam, at level 0. |
| Seam Chance At 100 | 40 | The same at level 100. |
| Seam Window At 0 | 2.6 | Seconds a seam stays open, at level 0. |
| Seam Window At 100 | 5.6 | The same at level 100 (configs older than 0.10.0 keep shorter windows). |
| Clean Strike Damage | 2 | Damage multiplier of a clean strike. |
| Unbroken Level | 100 | Level that unlocks Unbroken. |
| Unbroken Bonus Per Link | 20 | % added per clean strike after the first in a chain. |
| Unbroken Max Links | 5 | Links that add it. |

## Veins (16 - Veins)

Every ore deposit has 0 to 3 stars, the same for every player; each star means more drops for everyone who mines it.

| Setting | Default | Meaning |
| --- | --- | --- |
| Vein Chance 1 Star | 25 | % of deposits with 1 star. |
| Vein Chance 2 Stars | 11 | % with 2 stars. |
| Vein Chance 3 Stars | 4 | % with 3 stars. |
| Vein Bonus Per Star | 25 | % more drop rolls per star. |
| Read The Rock Level | 25 | Level for Read the rock. |
| Echo Level | 50 | Level for Echo. |
| Echo Radius | 40 | Echo reach, in metres. |
| Echo Cooldown | 10 | Seconds between Echoes. |

## Perks (17 - Pickaxe Perks)

Perks grow evenly from 0 at level 0 to the value at 100.

| Setting | Default | Meaning |
| --- | --- | --- |
| Extra Ore Chance At 100 | 30 | % chance a broken ore chunk drops its loot again. |
| Pickaxe Wear Reduction At 100 | 50 | % less pickaxe wear on rock (100 = none). |
| Splash Damage At 100 | 15 | **Splash:** damage a swing also spreads to the chunks touching the one it hits (grows every 10 levels). |

## Finds (18 - Mine Finds)

A broken chunk may hide amber, an amber pearl or a ruby ("Found a ruby!"); tiny chunks get less chance. Contents: `GrindstoneSkills.MineFinds.yml`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Find Chance At 0 | 0.2 | % chance per broken chunk at level 0. |
| Find Chance At 100 | 1 | The same at level 100. |

Default weights (amber / pearl / ruby): Meadows 85/12/3, Black Forest 75/20/5, Swamp 65/27/8, Mountain 55/33/12, Plains 45/38/17, Mistlands 35/42/23, Ashlands and Deep North 25/45/30, Ocean 40/50/10.

## Experience (14 - Pickaxes)

Swings earn the game's +1, times +25% per biome step (Meadows 0 up to Ashlands and Deep North 6), then ×1.5 on ore.

| Setting | Default | Meaning |
| --- | --- | --- |
| Pickaxes Enabled | true | Turns Pickaxes' features on or off; levels are kept. |
| Show Callouts | true | Floating words like Clean strike!. Per player. |
| Plain Stone Items | Stone, Grausten | Items that count as plain stone; anything else makes a rock an ore deposit. |
| Experience Multiplier | 1 | Multiplies all Pickaxes experience. |
| Experience Per Biome Step | 25 | % more per biome step. |
| Ore Experience Bonus | 50 | % more on ore deposits. |
| Clean Strike Experience | 1 | Per clean strike. |
| Discovery Experience | 10 | First hit on each kind of ore deposit. |

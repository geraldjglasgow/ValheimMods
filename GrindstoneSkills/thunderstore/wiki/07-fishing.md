# Fishing Skill

The game's own skill (reel speed, stamina); your level counts. **Trained by** fighting and landing fish; reeling an empty line no longer trains.

| Level | Milestone | What |
| --- | --- | --- |
| 25 | Species Sense | a nibble names the fish; a cast tells the fishing conditions |
| 50 | Size Sense | a nibble and a fish in the water show its level |
| 50 | Legendary fish | legendary fish take your bait |
| 50 | Double Bonus | a bonus roll can bring two items |
| 75 | Water Sense | a cast tells which fish take your bait and if something big lurks |
| 75 | Grace | running out of stamina does not lose the fish at once |

## The fight (51 - Fishing Fight)

Reeling while a fish thrashes fills a tension bar; full, the line snaps. Ease off while it thrashes, reel while it rests. Reeling right after a nibble is a **perfect strike** (it skips its first thrash). After a few thrashes the fish is **spent** and comes in faster.

| Setting | Default | Meaning |
| --- | --- | --- |
| Line Tension | true | Tension on; off, the line breaks as in the game. |
| Tension Build At 0 | 60 | % of line strength built per second reeling a thrashing fish, at level 0. |
| Tension Build At 100 | 30 | The same at level 100. |
| Tension Ease | 50 | % eased per second while not reeling. |
| Strike Window At 0 | 0.5 | Seconds after a nibble to set the hook, at level 0. |
| Strike Window At 100 | 1 | The same at level 100. |
| Perfect Strike Window | 0.2 | Seconds after a nibble for a perfect strike. |
| Tiring Per Thrash | 15 | % shorter each thrash than the first. |
| Thrashes To Tire | 4 | Thrashes before a fish is spent. |
| Spent Reel Speed | 50 | % faster reeling once spent. |
| Grace Level | 75 | Level for Grace. |
| Grace Seconds | 4 | Seconds the fish takes line while your stamina recovers. |

## Bites (52 - Bites)

Bite bonuses multiply. **Chum** dropped in the water draws fish to nearby floats.

| Setting | Default | Meaning |
| --- | --- | --- |
| Bite Chance At 100 | 100 | % more bites at level 100. |
| Dawn And Dusk Bite Bonus | 50 | % more bites at dawn and dusk. |
| Rain Bite Bonus | 25 | % more bites in rain. |
| Bait Bite Bonus Per Star | 20 | % more bites per star of bait starred before 0.13.0. |
| Chum Items | Entrails, Bloodbag | Items that work as chum (empty: off). |
| Chum Bite Bonus | 100 | % more bites with chum near the float. |
| Chum Radius | 12 | Metres chum reaches. |
| Chum Duration | 60 | Seconds chum lasts. |
| Species Sense Level | 25 | Level for Species Sense. |
| Size Sense Level | 50 | Level for Size Sense. |
| Water Sense Level | 75 | Level for Water Sense. |

## Big and legendary fish (53 - Big Fish)

A hooked fish may grow a level ("It's a big one!"), up to level 5. Legendary fish are level 6, three times the size and glowing; they always bring a bonus item and the server hears of the catch.

| Setting | Default | Meaning |
| --- | --- | --- |
| Big One Chance At 100 | 25 | % chance a hooked fish grows a level. |
| Night Big One Bonus | 50 | % more big-one chance at night. |
| Bait Big One Bonus Per Star | 5 | Points added per bait star. |
| Legendary Chance | 0.5 | % of spawned fish that are legendary. |
| Legendary Level | 50 | Level to hook them. |
| Legendary Thrashes | 8 | Thrashes before a legendary fish is spent. |
| Announce Legendary Catches | true | Tell every player about a legendary catch. |

## The catch (54 - Catch And Tackle)

A cast left in the water without a fish may **snag** something: a heavy reel, then the find ("Found ..."). Contents: `GrindstoneSkills.Snags.yml`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Bonus Item Chance At 100 | 40 | % chance of the fish's bonus item at level 100 (game: 20). |
| Double Bonus Level | 50 | Level for two-item bonus rolls. |
| Bait Saver At 100 | 30 | % chance a landed fish gives the bait back. |
| Snag Chance At 0 | 2 | % chance a cast snags, at level 0. |
| Snag Chance At 100 | 6 | The same at level 100. |
| Snag Wait | 8 | Seconds without a fish before a cast can snag. |
| Cast Distance At 100 | 30 | % farther casts. |
| Line Length At 100 | 50 | % longer line (game: 30 m). |
| Fillet Levels Per Fish Level | 10 | No effect since 0.13.0. |

Default snags (weight):

| Biome | Snags |
| --- | --- |
| Meadows | coins 30, resin 25, leather scraps 20, flint 20, amber 5 |
| Black Forest | coins 30, greydwarf eyes 25, bronze nails 20, troll hide 15, amber 10 |
| Swamp | coins 25, iron scrap 25, withered bones 20, chain 15, amber pearl 15 |
| Mountain | coins 30, obsidian 25, silver ore 20, wolf pelt 15, amber pearl 10 |
| Plains | coins 30, black metal scrap 20, barley 20, flax 20, silver necklace 10 |
| Ocean | sailor's purse 30, chitin 20, serpent scale 15, silver necklace 15, amber pearl 12, ruby 8 |
| Mistlands | coins 25, soft tissue 20, yggdrasil wood 20, sap 15, blue jute 15, ruby 5 |
| Ashlands | coins 25, grausten 25, charred bone 20, surtling core 15, flametal ore 15 |
| Deep North | coins 40, amber pearl 30, ruby 20, silver necklace 10 |

## Angler's log and experience (50 - Fishing)

Every species and level you land is logged with your heaviest catch, per character; `/fishlog` lists it. Experience scales with a fish's **species** (how hard it pulls: Perch 1 up to Northern salmon 2.6) and level.

| Setting | Default | Meaning |
| --- | --- | --- |
| Fishing Enabled | true | Turns Fishing's features on or off; levels are kept. |
| Show Callouts | true | Floating words like "It's a big one!". Per player. |
| Experience Multiplier | 1 | Multiplies all Fishing experience. |
| Empty Reel Experience | 0 | % of the game's reel experience with no fish on (game: 100). |
| Fight Experience | 100 | % of the game's reel experience with a fish on. |
| Catch Experience | 10 | Per fish landed, times species and size. |
| Size Experience Bonus | 50 | % more per fish level above 1. |
| Discovery Experience | 30 | First catch of each species. |
| New Size Experience | 10 | First catch of a known species at a new level. |

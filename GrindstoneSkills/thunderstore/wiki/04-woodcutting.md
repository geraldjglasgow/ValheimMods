# Woodcutting Skill

The game's own skill (axe damage to wood); your level counts. **Trained by** axe swings on wood, plus felling trees and breaking logs. The Thunderblood Axe and Greataxe do not use Woodcutting on wood (a game quirk).

Perks grow evenly from 0 at level 0 to the value at 100.

## Felling (11 - Felling)

These use the feller's level. A tree knocked over by your falling log counts as your fell.

| Setting | Default | Meaning |
| --- | --- | --- |
| Fall Push At 100 | 900 | **Timber!** % more push away from where you chopped, so trees fall where you aim. |
| Log Safety At 100 | 100 | % less damage to you from your own trees' logs. |
| Domino Impact At 100 | 200 | **Domino felling:** % more damage your falling log deals to trees, logs and stumps. |
| Domino Max Chain | 5 | Trees down a chain the extra damage reaches. |
| Clean Fell At 100 | 100 | % chance the stump comes out with the tree. |
| Replanting At 100 | 50 | % chance a free sapling takes root where the tree stood (trees the game has saplings for). |

## Chopping (12 - Chopping)

| Setting | Default | Meaning |
| --- | --- | --- |
| Stamina Refund At 100 | 30 | % of a swing's stamina back when it hits wood. |
| Axe Wear Reduction At 100 | 50 | % less axe wear on wood (100 = none). |
| Clean Split Chance At 100 | 20 | % of hits that split a log at once. |
| Clean Split Bonus | 50 | % more wood from a cleanly split log. |
| Old Growth Bonus | 100 | **Old growth:** % more wood from logs of the biggest trees of their kind. |
| Old Growth Start | 50 | Tree size, in % of its kind's range, where the bonus starts. |
| Old Growth Full | 100 | Tree size giving the whole bonus. |

## Finds (13 - Finds)

A tree you fell may drop a find from its trunk ("Found a bird's nest!"). Contents: `GrindstoneSkills.Finds.yml`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Find Chance At 0 | 2 | % chance per felled tree at level 0. |
| Find Chance At 100 | 15 | The same at level 100. |

Default finds (weight):

| Biome | Finds |
| --- | --- |
| Meadows | bird's nest 40, wild hive 20, raspberry stash 15, resin 10, coins 8, amber 5, queen bee's hive 2 |
| Black Forest | bird's nest 35, greydwarf's hoard 20, mushroom hollow 15, blueberry stash 15, wild hive 8, coins 8, amber 5 |
| Swamp | leech's leavings 25, guck 25, bird's nest 20, draugr's stash 15, ooze 10, amber pearl 4 |
| Mountain | eagle's nest 35, frozen hollow 12, wolf's cache 12, obsidian 12, lost purse 8, crystals 6, amber 5 |
| Plains | bird's nest 35, cloudberry stash 15, wild hive 12, deathsquito's nest 12, fuling's stash 8, ruby 3, queen bee's hive 2 |
| Mistlands | bird's nest 25, sap blister 15, seeker's cache 15, magecaps 12, Jotun puffs 12, dvergr's purse 8, wisp 4 |
| Ashlands | charred hollow 20, scorched nest 20, vineberries 15, fiddleheads 12, warrior's remains 10, sulfur 10, smoke puffs 10 |
| Deep North | frozen nest 30, lingonberry stash 20, ice hollow 12, timberwood cone 12, frozen purse 8, amber pearl 4 |
| Oak trees | owl's nest 35, acorn hoard 20, wild hive 15, coins and amber 8, queen bee's hive 3 |

## Experience (10 - Woodcutting)

Swings earn the game's +1; all of it is times the wood's **tier**: +50% per tool tier the wood needs (birch and oak ×2, Yggdrasil ×3).

| Setting | Default | Meaning |
| --- | --- | --- |
| Woodcutting Enabled | true | Turns Woodcutting's features on or off; levels are kept. |
| Show Callouts | true | Floating words like Timber! and Clean split!. Per player. |
| Experience Multiplier | 1 | Multiplies all Woodcutting experience. |
| Tier Scaling | true | Harder wood teaches more (the tier). |
| Tier Experience Per Tool Tier | 50 | % more per tool tier. |
| Small Wood Health | 30 | Wood under this health counts as small wood. |
| Small Wood Experience | 25 | % experience for a swing on small wood (saplings). |
| Fell Experience | 5 | Per tree you fell. |
| Split Experience | 2 | Per log you break into wood. |
| Discovery Multiplier | 3 | Multiplier for your first fell of each kind of tree. |

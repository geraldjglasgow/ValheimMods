# Homestead

Section `8. Homestead`: base tweaks outside storage. All are Server settings except those marked Player.

## Beds and respawn

| Setting | Default | Meaning |
| --- | --- | --- |
| `Nearest Bed Respawn` | on | Every bed you own is a spawn bed; you wake in the one nearest where you died. |
| `Bed Choice Seconds` | 30 | After a death the map opens this long so you can click a bed (0 to 60). 0: no map. |
| `Quick Respawn` | on | The nearer you died to your bed, the sooner you wake. |
| `Quick Respawn Seconds` | 1 | The wait when you died beside the bed (0 to 18). |
| `Quick Respawn Range` | 1000 m | The distance at which the game's full 18 s wait applies (10 to 20000). |
| `Stand Up On Respawn` | on | Wake standing instead of the getting-up animation. |
| `Quick Area Loading` (Player) | on | The land around your bed loads faster while you wait to respawn. |
| `Beds On Map` (Player) | on | Show all your beds on the map, in yellow. |

- Any bed you own lets you sleep. A bed counts once you have claimed, slept in or visited it.
- If your nearest bed is gone, you wake in the next one; with none left, at the world start.
- On the bed map, click a bed to wake there; the map key or Escape picks the nearest at once.

## Building and honey

| Setting | Default | Meaning |
| --- | --- | --- |
| `Build On Wood` | fire_pit | Pieces that may be built on wooden floors, comma separated. You can add `bonfire`, `smelter`, `charcoal_kiln`, `blastfurnace`, `eitrrefinery`, `piece_FrostKiln` or `windmill`. |
| `Honey Per Day` | 0 | Honey per beehive per game day (0 to 1000). 0: the game's 1.5. |
| `Honey Per Player Online` | off | Each hive makes as much honey per day as there are players online. |

A hive still holds at most 4 honey. **Hazard:** in the Ashlands, or with the Fire world key, a campfire on wood can set the floor alight.

## Fires and torches

| Setting | Default | Meaning |
| --- | --- | --- |
| `Auto Fuel` | on | Fires you built refill themselves from nearby chests with their own fuel. |
| `Auto Fuel Range` | 20 m | How far a fire looks for fuel (1 to 50). |
| `Torches Night Only` | on | The torches in `Torch Pieces` light at nightfall and go out at daybreak, saving fuel. |
| `Torch Pieces` | the standing torches and the sconce | Which fires are switched, comma separated prefab names. |
| `Torch Margin` | 1 | Game hours the torches light early and stay on late (0 to 4). |
| `Torch Switch Key` (Player) | O | Look at a torch and press it to keep it lit day and night, or put it back on the schedule. |

Fires with endless fuel, the resin candle and fires no player built are not refuelled.

## Stations and pets

| Setting | Default | Meaning |
| --- | --- | --- |
| `Auto Feed Stations` | on | Smelters, blast furnaces, kilns, refineries, spinning wheels, windmills and the hot tub take ore, wood and fuel from nearby chests by themselves. |
| `Auto Feed Range` | 4 m | Distance from the station's edge to a chest (0.5 to 20). |
| `Auto Feed Skip` | FineWood, RoundLog | Items stations never take by themselves (keeps fine and core wood out of the kiln). |
| `Auto Feed Leave` | 1 | How many of each item stay in every chest (0 to 1000), so quick stack still finds it. |
| `Pets Eat From Chests` | on | Hungry tame animals walk to a nearby chest and eat food from it. |
| `Pet Chest Range` | 10 m | How far an animal walks to a chest (1 to 30). |

Cooking stations, ovens and fermenters are not fed. Food on the ground is eaten first. These features use the chest rules of `OpenKeep.Reach.yml` (see [Crafting and Salvage](wiki:Crafting and Salvage)) and work only while a player is near.

## Rest and repair

| Setting | Default | Meaning |
| --- | --- | --- |
| `Rested Delay` | 5 | Seconds of resting before Rested (0 to 60). The game's is 20. |
| `Area Repair` | on | Repairing a piece with the hammer also repairs the damaged pieces touching it, for free. |
| `Auto Repair` | on | Opening a crafting station repairs all the gear it can repair, as its repair button does. |

Area repair still needs each piece's crafting station nearby and ward access.

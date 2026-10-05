# Build Camera

Section `11. Build Camera`: with a hammer, hoe, cultivator or any other build tool in hand near a crafting station, press `B` and the camera leaves your character. Fly it around the base and build, remove and repair from it as usual. Your character stays where it stands.

## Using it

- **Out:** `B` with a build tool in hand, standing within a crafting station's build range times `Range Multiplier` (workbench, forge, stonecutter...). Elsewhere you get `The build camera works only near a crafting station`.
- **Fly:** the movement keys move it level along the view (W never dives), Jump raises it, Crouch lowers it, Run makes it `Run Multiplier` times faster. The mouse looks around. On a gamepad: left stick to move, right trigger up, left trigger down; the triggers also place and rotate, so the camera moves a little while you place.
- **Where:** it stays within each station's build range times `Range Multiplier`, as far above and below the station. The ground, rocks, cliffs and cave walls stop it; building pieces, trees, creatures and water do not, so it passes through walls into a house.
- **Building:** you reach your own build distance (5 m) plus `Extra Reach`, measured from the camera. Pieces still need a station in its real range. As in the game, removing a piece needs its station near where you stand, and the hoe's level ground levels to the height where you stand.
- **Back:** `B` again, or at once when you put the tool away, die, teleport, sit at a ship's helm, or no station's area holds you any more (the station was destroyed).
- **Pickup:** items lying near the camera come into your inventory, as the game's auto pickup does around you (only with auto pickup on, room and weight allowing).
- **Light:** a light worn on the head (the Dvergr circlet, also one another mod puts in an extra slot) shines from the camera too.
- **Comfort:** a server can ask for the Resting effect (by a fire, under a roof) or a comfort level where you stand, to bring the camera out or to pick up with it. A missing need is named on screen, such as `The build camera needs: Resting, comfort 5 (you have 2)`. The entry needs are checked when the camera comes out, not while it is out.

## Settings

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | The build camera works. |
| `Range Multiplier` | 1 | Server | The camera's area is this many times each station's build range (0.25 to 5). Only the camera's: pieces still need the real range, and raids and comfort keep the station's area. |
| `Extra Reach` | 5 m | Server | How much farther than your own build distance the camera places, removes and repairs (0 to 45). |
| `Camera Pickup` | on | Server | Items near the camera come into your inventory. |
| `Entry Needs Resting` | off | Server | The camera comes out only while you have the Resting effect. |
| `Entry Min Comfort` | 0 | Server | Comfort level you need to bring it out (0 to 50). 0: none. |
| `Pickup Needs Resting` | off | Server | Camera Pickup works only while you have the Resting effect. |
| `Pickup Min Comfort` | 0 | Server | Comfort level you need for Camera Pickup (0 to 50). 0: none. |
| `Toggle Key` | B | Player | Bring the camera out and back. |
| `Gamepad Toggle` | JoyAltKeys + JoyRStick | Player | The same on a gamepad: hold the left trigger and click the right stick. The game's button names joined with `+`, the last one pressed while the others are held. Empty: none. |
| `Speed` | 10 | Player | Metres per second the camera flies (1 to 100). |
| `Run Multiplier` | 3 | Player | How much faster it flies while Run is held (1 to 10). |
| `Circlet Light` | on | Player | A light worn on the head shines from the camera too. |
| `Circlet Intensity` | 0 | Player | Brightness of that light (0 to 10). 0: the circlet's own. |
| `Circlet Range` | 0 | Player | Metres it reaches (0 to 100). 0: the circlet's own. |
| `Circlet Spot Angle` | 0 | Player | Width of its beam in degrees (0 to 179). 0: the circlet's own. |
| `Pickup Panel` | on | Player | While items lie by the camera and Camera Pickup's needs are not met, a panel near the top of the screen says what is missing. |
| `Pickup Panel Position` | 0, -120 | Player | Where that panel sits: pixels from the top centre of the screen (x right, y up). |

With EarthWright, terrain edits still reach only from where you stand, not from the camera.

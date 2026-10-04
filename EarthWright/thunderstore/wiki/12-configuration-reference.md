# Configuration Reference

Every setting of `BepInEx/config/milkyteam.earthwright.cfg`. The file is written on the first start with a description of each setting, and is reloaded when you save it; no restart is needed. BepInEx sorts the sections by name, so `10. Controls` to `15. Road Travel` follow `1. Brush` in the file.

The Scope column:

- **Server**: synced. While the server's `Lock Configuration` is on, the server's value binds every player and players cannot change it locally; admins can still change it from their game, and the change goes to the server. With `Lock Configuration` off, every player uses their own value.
- **Player**: your own, never synced: keys, display and personal preferences.

The YAML files are described in [YAML Files](wiki:YAML Files).

## 0. General

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Lock Configuration | true | | Server | When on, every player uses the server's values for the synced settings and YAML files; admins can still change them. |
| Enabled | true | | Server | Master switch. Off: the hoe and cultivator behave as in the unmodded game and EarthWright's entries are hidden. |
| Use EarthWright | true | | Player | Your own switch (also `ew on` / `ew off`). Off: your hoe and cultivator behave as unmodded; height limits and protection still apply. |
| Debug Log | false | | Player | Writes every terrain edit sent and applied to the BepInEx log. |

## 1. Brush

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Minimum Radius | 0.5 | 0.5 to 100 | Server | The smallest brush radius in metres. An entry in the brush YAML may set its own. |
| Maximum Radius | 20 | 0.5 to 100 | Server | The largest brush radius in metres. An entry in the brush YAML may set its own; tool levels and the skill cap may lower it. |
| Minimum Amount | 0.05 | 0.01 to 50 | Server | The smallest raise or lower per click, in metres. |
| Maximum Amount | 8 | 0.05 to 50 | Server | The largest raise or lower per click, in metres. |
| Size Multiplier | 1 | 0.1 to 10 | Server | Multiplies the starting radius of the terrain entries. Entries with a radius in the brush YAML keep it. |
| Multiply Level | true | | Server | The multiplier applies to levelling entries. |
| Multiply Raise | true | | Server | The multiplier applies to raise and lower entries. |
| Multiply Smooth | true | | Server | The multiplier applies to smoothing entries. |
| Multiply Paint | true | | Server | The multiplier applies to paint-only entries (Pathen, Replant, Paint, Till). |
| Resize Modded Terrain Pieces | true | | Server | The size keys also resize terrain pieces other mods add. Off: they keep their own size. |
| Default Level Style | Ease | Ease, Step, Instant | Server | The level style every player starts with; also applies to the game's Level ground. |
| Allowed Shapes | Circle, Square, Rectangle, Ring, Frame | | Server | The shapes the shape key cycles through, comma separated. |
| Allowed Paints | Dirt, Paved, Cultivated, Grass, Original, Vegetation, ClearVegetation, Keep | | Server | The paints the paint key cycles through besides the entry's own, comma separated. |
| Aim Through Objects | true | | Player | When the crosshair is on a rock, tree, cliff or water, the brush goes to the ground behind or under it. Buildings are never looked through. |
| Radius Step | 0.5 | -10 to 10 | Player | Metres per wheel notch or key press. Negative reverses the wheel. |
| Amount Step | 0.1 | -5 to 5 | Player | Step of the raise/lower amount, strength, density, and the level max step below 2 m. |
| Hardness Step | 0.05 | -1 to 1 | Player | Step of the edge hardness. |
| Rotation Step | 22.5 | -180 to 180 | Player | Degrees per step and per arrow key press. |
| Depth Step | 0.5 | -10 to 10 | Player | Step of the rectangle depth, ring inner radius and frame band width. |
| Fast Step Multiplier | 5 | 1 to 20 | Player | Steps are this much larger with the fast modifier (Ctrl); the height keys use it with Shift. |
| Announce Changes | false | | Player | Also shows every brush value change in the middle of the screen. |
| Skill Cap | false | | Server | The largest radius grows with a skill (see the next four settings). The cap never goes below an entry's own size. |
| Skill Cap Skill | Crafting | | Server | The skill, by its name in the game's code (Crafting, WoodCutting, Pickaxes, Run, ...). |
| Skill Cap Start Level | 25 | 0 to 100 | Server | Below this level entries keep their own size. |
| Skill Cap Full Level | 60 | 1 to 100 | Server | At this level the cap reaches the full radius. |
| Skill Cap Radius | 6 | 0.5 to 100 | Server | The largest radius at the full level, in metres. |

## 2. Target Height

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Default Target | Feet | Feet, Aimed, Continued | Player | Where levelling takes its height from when you log in. |
| Continue Tolerance | 0.25 | 0.01 to 5 | Player | Continue the flat: earlier edits within this many metres of each other count as one flat. |
| Height Key Step | 0.1 | 0.01 to 10 | Player | Metres per press of the height up/down keys (Shift: times the fast step multiplier). |
| Exact Height Step | 0.25 | 0.01 to 10 | Player | Metres per step of the exact target height. |
| Exact Height Fast Step | 2 | 0.01 to 50 | Player | The same with the fast modifier held. |
| Show Target Messages | true | | Player | A message when the target is locked, copied from a floor or released. |

## 3. Operations

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Max Radius | 100 | 1 to 100 | Server | The largest radius (also ring, frame and rectangle sizes) the owner of the ground accepts from a player; larger strokes are cut down. Also the limit of the `ew reset` and `ew terrain` radius. |
| Max Amount | 50 | 0.1 to 512 | Server | The largest raise, lower or offset of one stroke, in metres. Approved admin edits may apply up to 512 m. |
| Max Step | 1000 | 1 to 1000 | Server | The largest height change per point of one Ease or Step levelling stroke, in metres. |
| Ease Power | 1 | 0.25 to 4 | Server | How Ease fades toward a soft edge: 1 follows the hardness; higher eases less near the rim, lower more. |
| Paint Edge Hardness | 0.8 | 0 to 1 | Server | The least edge hardness used for painting, so paint keeps a crisp edge. 0 paints with the brush's own hardness. |

## 4. Ramps and Roads

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Max Points | 1024 | 16 to 8192 | Server | The most ground points (one per square metre, shoulders included) one ramp or road may change. |
| Max Slope | 75 | 5 to 75 | Server | The steepest slope in degrees along the middle. 75 is the absolute cap. |
| Min Width | 1 | 0.5 to 20 | Server | The narrowest ramp or road in metres. The width is twice the brush radius within these limits. |
| Max Width | 20 | 1 to 50 | Server | The widest ramp or road in metres. |
| Max Length | 128 | 4 to 1024 | Server | The longest ramp or road in metres, measured on the map. |
| Refuse Past Height Limit | true | | Server | On: a ramp or road past the height limits is refused. Off: it is built and the ground stops at the limit. |
| Quick Ramp | true | | Server | Allows the quick ramp key. |
| Shoulder Width | 2 | 0 to 10 | Server | Metres beside a ramp or road over which it blends into the ground. 0: a sharp edge. |
| End Blend Length | 3 | 0 to 20 | Server | Metres past each end over which blended ends fade into the ground. |
| Soft Join Length | 2 | 0.5 to 10 | Server | Length of the rounded joins of the SoftJoins profile. |
| Ramp Paint | Dirt | None, Dirt, Paved, Cultivated, Grass | Server | The paint of a ramp while your paint choice is the entry's own. |
| Road Paint | Dirt | None, Dirt, Paved, Cultivated, Grass | Server | The paint of a road carved with the carve key while your paint choice is the entry's own. |
| Ramp Profile | Straight | Straight, SoftJoins, SoftEnds, SCurve | Player | Your ramp profile; the shape key cycles it with the Ramp entry selected. |
| Width From Cursor | false | | Player | After the second ramp click, the width follows the cursor's distance from the middle. |
| Clear Points When Deselected | true | | Player | Points are forgotten when you select another entry or put the tool away. Always on death and logout. |
| Quick Ramp Blends Ends | true | | Player | The quick ramp blends its ends into the ground. |
| Road Blends Ends | true | | Player | A carved road blends its first and last metres into the ground. |
| Warning Slope | 38 | 1 to 75 | Player | The HUD warns above this slope in degrees. Players slide down ground steeper than 38. |
| Cart Easy Slope | 20 | 1 to 75 | Player | Preview segments up to this slope are green. |
| Cart Hard Slope | 25 | 1 to 75 | Player | Preview segments up to this slope are yellow; steeper ones red. |
| Show Changed Points | true | | Player | A dot on every point the ramp or road changes: blue filled, orange cut, red past the limit. |
| Preview Through Ground | true | | Player | Draws the preview through the ground. |
| Quick Ramp Preview | true | | Player | With the Ramp entry selected and no point set, faintly shows the quick ramp. |
| Quick Ramp Key | J | | Player | Builds a ramp from your feet to the aimed point. |
| Carve Road Key | H | | Player | Carves the planned road with the current paint. |
| Carve Paved Road Key | H + LeftShift | | Player | Carves the planned road paved. |
| Remove Last Point Key | Backspace | | Player | Removes the last ramp point or road waypoint. |
| One Side Modifier | LeftControl | | Player | Hold to put a ramp's whole width on the cursor's side. |
| Blend Ends Modifier | LeftAlt | | Player | Hold while building a ramp to blend its ends. |

## 5. Reset and Clearing

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Reset Around Radius | 10 | 1 to 50 | Server | Radius of the reset-around key and of `ew reset` without a radius. |
| Reset Skips Ground Under Buildings | true | | Server | The reset keys and `ew reset` leave the ground under building pieces alone. |
| Reset Key | U | | Player | Resets the ground inside the brush. Free. |
| Reset Around Key | U + LeftShift | | Player | Resets a circle around you. Free. |
| Command Max Radius | 50 | 1 to 128 | Server | The largest radius a non-admin may give `ew reset`, `ew forestry` and `ew debris`. |
| Clearing Enabled | false | | Server | Lists the Clear objects entry, lets Groundbreaker clear, and lets players use `ew forestry` and `ew debris`. Off: only admins clear, by command. |
| Clearing Mode | Remove | Remove, Survival | Server | Remove: objects vanish, no tool needed. Survival: an axe or pickaxe of a high enough tier is needed. |
| Survival Drops | true | | Server | In Survival mode, objects are chopped and mined normally and drop their items. Off: they vanish. |
| Clearing Radius | 0 | 0 to 50 | Server | Radius cleared by Clear objects and Groundbreaker. 0: the brush itself. |
| Clear Trees | true | | Server | Clearing entries take standing trees. |
| Clear Stumps | true | | Server | Clearing entries take tree stumps. |
| Clear Logs | true | | Server | Clearing entries take fallen logs. |
| Clear Shrubs | true | | Server | Clearing entries take bushes and shrubs. |
| Clear Rocks | true | | Server | Clearing entries take rocks and boulders (not ore). |
| Clear Pickables | true | | Server | Clearing entries take natural pickables (never crops on cultivated ground). |
| Clear Ore Deposits | false | | Server | Rocks and veins that drop ore are cleared too (Clear objects and `ew debris`). |
| Ore Drops | Ore,Scrap,Obsidian,Softtissue,Tar,Sulfur | | Server | A rock or pickable is ore when a drop's item name contains one of these (case matters). |
| Keep Pickables | (boss offerings, quest items, treasure) | | Server | Pickable prefab names clearing never takes, comma separated, `*` as wildcard. The default lists `Pickable_DragonEgg`, `goblin_totempole`, `Pickable_*CoreStand`, `Pickable_Swordpiece*`, `Pickable_VoltureEgg`, `Pickable_Charredskull`, `Pickable_FrostCoreHanger`, `Morkhalla_Eye*`, `Pickable_Dvergr*`, `Lured*`, `Pickable_Fishingrod`, `Pickable_RoyalJelly`, `Pickable_ForestCryptRemains*`, `Pickable_MountainRemains*`. |

## 6. Height Limits

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Raise Limit | 8 | 0.5 to 512 | Server | How far the ground may rise above the world's original height, in metres. |
| Dig Limit | 8 | 0.5 to 512 | Server | How deep the ground may be dug, in metres. Also where the pickaxe stops dropping stone. |
| Admin Limit | 256 | 0.5 to 512 | Server | The most an admin edit that ignores the limits may raise or dig. |
| Gentle Slopes | false | | Server | Digging and raising also reshape the ground around, so no slope beside an edit is steeper than the angle below. |
| Gentle Slope Angle | 40 | 10 to 80 | Server | The steepest slope in degrees gentle slopes leave. |
| Gentle Slope Radius | 6 | 1 to 20 | Server | How far around an edit, in metres, gentle slopes may reshape. |

## 7. Undo

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| History Size | 15 | 1 to 50 | Player | How many of your changes undo can take back. Cleared on logout or world change, not on death. |
| Group Window | 0.3 | 0 to 5 | Player | Changes within this many seconds are one step. A dragged stroke is always one step. |
| Undo Key | Z + LeftControl | | Player | Takes back your last terrain change (a ramp or road point first). Costs are not refunded. |
| Redo Key | Y + LeftControl | | Player | Brings back the change you last undid. |
| Snapshot Radius | 32 | 4 to 128 | Player | Radius `ew snapshot save` records when no radius is given. |

## 8. Costs

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Stamina Mode | Vanilla | Vanilla, Off, Fixed, Scaled | Server | Stamina per terrain swing: the game's, none, a fixed amount, or the game's scaled with the brush. A longer reach never lowers it. |
| Stamina Per Use | 5 | 0 to 100 | Server | Stamina per swing in Fixed mode. |
| Stamina Factor | 1 | 0 to 10 | Server | Scaled mode: the game's cost is multiplied by this. |
| Stamina Radius Exponent | 1 | 0 to 3 | Server | Scaled mode: (radius / normal radius) to this power. 0: size does not matter; 2: grows with the area. |
| Stamina Skill | None | None or a game skill | Server | A skill that lowers terrain stamina. |
| Stamina Skill Reduction | 50 | 0 to 100 | Server | Percent the skill takes off at level 100. |
| Tool Wear | true | | Server | Terrain swings wear the tool as in the game. |
| Tool Wear Factor | 1 | 0 to 10 | Server | Multiplies the game's wear per swing. |
| Tool Wear Radius Exponent | 0 | 0 to 3 | Server | Wear grows with (radius / normal radius) to this power. |
| Cooldown | 0 | 0 to 30 | Server | Seconds between two terrain uses. 0: none. |
| Allow Free Build | Admins | Nobody, Admins, Everyone | Server | Who may switch free build on. |
| Free Build Key | F7 | | Player | Switches free build while a terrain tool is out. |
| Show Costs | true | | Player | Shows the cost of one swing next to the crosshair. |
| Charge Materials | true | | Server | Entries cost their materials (the game's, or the costs YAML list). |
| Material Radius Exponent | 0 | 0 to 3 | Server | Material amounts grow with (radius / normal radius) to this power, rounded up, at least 1. |
| Extra Item | (empty) | | Server | Prefab name of an item every terrain swing also costs. |
| Extra Item Amount | 1 | 1 to 100 | Server | How many of the extra item. |
| Hoe Needs Stations | true | | Server | Hoe entries need their crafting station nearby. |
| Cultivator Needs Stations | true | | Server | Cultivator entries need their crafting station nearby. |
| Modded Entries Need Stations | true | | Server | Other mods' terrain entries need their station nearby. |
| Paved Road Needs Stonecutter | true | | Server | Paving with any entry needs a stonecutter nearby (Groundbreaker excepted). |
| Groundbreaker Needs Stonecutter | false | | Server | Groundbreaker needs a stonecutter when it paves. |
| Stone Per Cubic Metre Raised | 0 | 0 to 10 | Server | Volume Cost Item per cubic metre raised; fractions carry over. Lowering is free. 0: off. |
| Stone Per Square Metre Paved | 0 | 0 to 10 | Server | Volume Cost Item per square metre newly paved. 0: off. |
| Volume Cost Item | Stone | | Server | The item volume costs are paid in. |

## 9. Preview and HUD

All Player.

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| Show Outline | true | | Outline of the brush on the ground. |
| Outline Width | 0.08 | 0.01 to 0.5 | Line width in metres. |
| Outline Colour | FFD14CE6 | | Outline when the click would go through (RRGGBBAA). |
| Paint Outline Colour | C7A16EB2 | | Second outline where the paint reaches. |
| Blocked Colour | FF3B30E6 | | Outline and volume while the click would be refused. |
| Out Of Reach Colour | 9E9E9EB2 | | Outline while the aimed point is out of reach. |
| Ghost Visuals | Resize | Resize, Hide, Leave | The game's ghost ring: follows the brush, hidden, or as the game draws it. |
| Show Changed Points | true | | Marks every point the click would change. |
| Point Limit | 4000 | 100 to 20000 | Most points marked at once. |
| Point Size | 0.15 | 0.03 to 0.5 | Marker size in metres. |
| Raise Colour | 4CE659E6 | | A point that would rise. |
| Lower Colour | FF9933E6 | | A point that would sink. |
| Limited Colour | FF3354E6 | | A point held back by a height limit. |
| Paint Cell Colour | D9B280B2 | | A cell a paint-only entry would paint. |
| Show Volume | true | | See-through body between the ground and the target. |
| Volume Colour | 4CA6FFFF | | Volume within reach. |
| Volume Beyond Reach Colour | FF6633FF | | Volume beyond reach. |
| Volume Opacity | 0.25 | 0.02 to 1 | Opacity of the volume. |
| Show HUD | true | | EarthWright's lines next to the crosshair. |
| HUD Scale | 1 | 0.5 to 3 | Size of the HUD text. |
| HUD Offset X | 40 | -2000 to 2000 | Pixels from the crosshair (negative: left). |
| HUD Offset Y | 24 | -2000 to 2000 | Pixels from the crosshair (negative: up). |
| Show Cursor Readout | true | | The line with the tile, ground height and target distance. |
| Show Locked Badge | true | | A note while the server has locked the settings. |
| World Grid Key | F8 | | Shows or hides the world grid while building. |
| World Grid Radius | 10 | 2 to 50 | How far around the crosshair the grid reaches. |
| World Grid Spacing | 1 | 0.25 to 10 | Metres between grid lines. |
| World Grid Line Width | 0.04 | 0.01 to 0.3 | Line width in metres. |
| World Grid Colour | FFFFFF59 | | Grid lines. |
| World Grid Major Colour | FFD14C99 | | Every major line. |
| World Grid Major Every | 5 | 0 to 50 | Every how many lines a major line is drawn; 0: none. |
| World Grid Anchor | World | World, AimedPiece | The world's axes, or through the last building piece aimed at. |
| Highlight Pieces | true | | Highlights building pieces inside the brush. |
| Remove Dust | false | | Removes the dust of your terrain clicks (sounds stay; others see none either). |
| Panel Key | F6 | | Opens or closes the panel. |
| Panel Position | {"x":60.0,"y":120.0} | | Where the panel was last dragged to. |
| Panel Scale | 1 | 0.5 to 3 | Size of the panel. |
| Raise Presets | 1x2, 5x2, 5x3, 8x3 | | Preset buttons, each "amount x radius" in metres. |

## 10. Controls

All Player. See [Controls and Hotkeys](wiki:Controls and Hotkeys) for what each key does.

| Setting | Default | Range |
| --- | --- | --- |
| Adjust Modifier | LeftAlt | None lets the plain wheel change values |
| Fast Modifier | LeftControl | |
| Plain Wheel Adjusts | false | |
| Camera Zoom Block | WhileAdjusting | WhileAdjusting, Always, Off |
| Increase Key | RightBracket | |
| Decrease Key | LeftBracket | |
| Select Value Key | B | |
| Shape Key | N | |
| Rotate Right Key | RightArrow | |
| Rotate Left Key | LeftArrow | |
| Reset Rotation Key | Home | |
| Snap Hold Key | Z | |
| Grid Mode Key | I | |
| Aim At Edge Key | O | |
| Level Style Key | L | |
| Paint Key | P | |
| Hard Level Key | F9 | |
| Hold To Repeat | true | |
| Repeat Start Delay | 0.35 | 0.05 to 3 seconds |
| Repeat Rate | 0.15 | 0.05 to 3 seconds |
| Lock Height Key | K | |
| Height Up Key | PageUp | |
| Height Down Key | PageDown | |
| Back To Feet Key | End | |
| Target Mode Key | Y | |
| Copy Floor Height Key | Mouse2 | |
| Gamepad Controls | true | |
| Gamepad Modifier | JoyAltKeys | game input name; empty: off |
| Gamepad Value Up | JoyDPadUp | |
| Gamepad Value Down | JoyDPadDown | |
| Gamepad Select Value | JoyDPadRight | |
| Gamepad Shape | JoyDPadLeft | |

## 11. Protection

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Respect Wards | true | | Server | Refuses edits when any part of the brush reaches into a ward you are not permitted on; checked by you and by the owner of the ground. |
| Respect No-Build Zones | true | | Server | Refuses edits when the brush touches a place where the game forbids building. |
| Refuse In Dungeons | true | | Server | Refuses edits while you are inside a dungeon or interior. |
| Terrain Tools Allowed | Everyone | Everyone, AdminsOnly, Nobody | Server | Who may change terrain with the hoe, cultivator and EarthWright's tools. |
| Lock Terrain Editing | false | | Server | Locks all terrain editing, including the game's own hoe and cultivator entries and pickaxe digging. |
| Admins Bypass Lock | true | | Server | Admins may still edit while locked; their edits are checked by the server. |
| Exempt Tools | (empty) | | Server | Item prefab names that keep working while locked, comma separated (`Cultivator`, `PickaxeIron`). |
| Admin Zone Mode | Off | Off, OnlyInsideZones, NeverInsideZones | Server | What admin zones mean. |
| Admins Bypass Zones | true | | Server | Admins may edit regardless of zones; checked by the server. |
| Combat Lock | false | | Server | Blocks terrain tools while an alerted hostile creature within the radius hunts you. |
| Combat Lock Radius | 30 | 5 to 100 | Server | The combat lock's radius in metres. |
| Admin Limit Override Key | RightAlt | | Player | Admins: hold to ignore the height limits. Does nothing for other players. |
| Limit Override Needs God Mode | false | | Server | The override key works only in god mode. |
| Strict Dig Exceptions | false | | Server | Lifts the dig limit near the objects below and in tar. |
| Dig Exception Objects | (ore deposits, mud piles, buried treasure) | | Server | Prefab names, comma separated. Default: `rock4_copper, rock4_copper_frac, MineRock_Copper, MineRock_Tin, MineRock_Iron, mudpile, mudpile_beacon, mudpile_old, mudpile_frac, mudpile2, mudpile2_frac, silvervein, silvervein_frac, rock3_silver, rock3_silver_frac, MineRock_Obsidian, MineRock_Meteorite, goldvein, goldvein_frac, TreasureChest_meadows_buried, TreasureChest_memorial_buried, Pickable_MountainRemains01_buried`. |
| Dig Exception Radius | 8 | 1 to 32 | Server | How far from such an object the dig limit is lifted, in metres. |
| Dig Exception In Tar | true | | Server | Lifts the dig limit where tar covers the ground. |

## 12. Menu

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Enable Level Ground | true | | Server | Lists the game's Level ground entry. Off removes it for every player. |
| Enable Raise Ground | true | | Server | The same for Raise ground. |
| Enable Pathen | true | | Server | The same for Pathen. |
| Enable Paved Road | true | | Server | The same for Paved road. |
| Enable Cultivate | true | | Server | The same for the cultivator's Cultivate. |
| Enable Replant | true | | Server | The same for the cultivator's Replant. |
| Enable Lower | true | | Server | Lists EarthWright's Lower ground entry. |
| Enable Smooth | true | | Server | Lists Smooth ground. |
| Enable Paint | true | | Server | Lists Paint ground. |
| Enable Reset | true | | Server | Lists Reset ground. |
| Enable Ramp | true | | Server | Lists Ramp (the quick ramp needs it too). |
| Enable Road | true | | Server | Lists Road. |
| Enable Groundbreaker | true | | Server | Lists Groundbreaker. |
| Enable Clear Objects | true | | Server | Lists Clear objects; it also needs `Clearing Enabled` in section 5. |
| Enable Terraform | true | | Server | Lists Terraform; only admins see it. |
| Enable Till | true | | Server | Lists the cultivator's Till. |
| Enable Uproot | true | | Server | Lists the cultivator's Uproot. |
| Paved Road Levels | true | | Server | Paved road levels and paves as in the game. Off: it only paves. |
| Cultivate Levels | true | | Server | Cultivate evens out the ground as it tills. Off: it only tills. |
| Terraform Ignores Height Limits | true | | Server | Terraform levels past the raise and dig limits. Off: it obeys them. |
| Custom Entry Repeat Interval | 0.25 | 0.05 to 5 | Server | Seconds between runs of a repeating custom entry while held. |
| Show Key Hints In Descriptions | true | | Player | Entry descriptions end with the keys that work with them. |
| Full Build Menu | true | | Player | The hoe and cultivator use the full build menu: search, recent pieces, favourites. |

## 13. Tools

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Hoe Max Level | 6 | 1 to 10 | Server | The hoe's highest upgrade level. |
| Hoe Upgrade Cost | (empty) | | Server | One upgrade's cost as `Item:Amount` pairs. Empty: the game's own. |
| Hoe Durability Per Level | 200 | 0 to 10000 | Server | Durability gained per level. |
| Cultivator Max Level | 6 | 1 to 10 | Server | The cultivator's highest upgrade level. |
| Cultivator Upgrade Cost | (empty) | | Server | As for the hoe. |
| Cultivator Durability Per Level | 200 | 0 to 10000 | Server | As for the hoe. |
| Upgrade Station Levels | 1, 2, 3, 4, 5, 5 | | Server | The station level each tool level needs (crafting first, then each upgrade). Empty: the game's rule. |
| Radius Per Level | (empty) | | Server | The largest brush radius per tool level, comma separated. Empty: no limit. |
| Level Unlocks | (empty) | | Server | `feature:level` pairs locking entries, shapes and styles until a level. Empty: nothing locked. |
| Reach | 20 | 5 to 50 | Server | How far away a terrain tool works while held, in metres. |
| Movement Speed | 1.1 | 0.5 to 3 | Server | Running and sprinting speed while a terrain tool is held. |
| Torch In Left Hand | true | | Server | A torch stays in the left hand next to a terrain tool. |
| Tool Light | true | | Server | Terrain tools give off light, seen by every player. |
| Light Range | 8 | 1 to 30 | Server | How far the light reaches, in metres. |
| Light Intensity | 1.2 | 0 to 5 | Server | How bright the light is. |
| Light Colour | FFD999FF | | Player | The colour you see the tools' light in. |

## 14. Cultivator

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Seed Grid | false | | Player | Seeds and saplings snap to a grid while placed; switched with the key below. |
| Seed Grid Key | I | | Player | Turns the seed grid on or off while a seed or sapling is selected. |
| Seed Grid Spacing | 0 | 0 to 10 | Player | Metres between grid points. 0: each plant's own spacing. |
| Cultivate Anywhere | false | | Server | The cultivator's ground entries also work where the game says "needs dirt". |
| Uproot Picks Items | true | | Server | Uprooting picks a plant first, so its items drop. Off: nothing drops. |

## 15. Road Travel

| Setting | Default | Range | Scope | What it does |
| --- | --- | --- | --- | --- |
| Dirt Road Bonus | 10 | 0 to 100 | Server | Percent faster sprinting and less sprint stamina on dirt. |
| Paved Road Bonus | 20 | 0 to 100 | Server | Percent faster sprinting and less sprint stamina on paved roads. |

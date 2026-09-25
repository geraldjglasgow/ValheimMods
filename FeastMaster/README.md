# FeastMaster

A comprehensive food and mead configuration mod for Valheim; the server's settings bind every player.

### Features

**What you eat**
- **Global Stat Multipliers** - Scale health, stamina, eitr, duration and health regen of every food at once
- **Per-Item Configuration** - Set each food's values on its own; the configured numbers show on tooltips right away
- **Mead Customization** - Duration (which is also the mead's cooldown), the health, stamina and eitr restored over time, regeneration multipliers and run/jump stamina modifiers, per mead
- **Food Degradation** - Keep food at full strength or pick your own fade curve (linear, faster, slower)
- **Eat Again** - Decide how far a food must have run down before it can be eaten again
- **Auto Eat** - When a food runs out, eat another of the same from the inventory; the server allows it, each player can opt out
- **Fermenter** - Set how long a batch of mead brews and how many meads it gives
- **Cooking** - Cook time per recipe on every cooking station and oven, one multiplier for all of them, and a switch that stops food from burning
- **Feasts** - Set how many servings a placed feast holds; every feast's food has its own section like any other food

**How you recover**
- **Continuous Food Healing** - The food's regen is healed a little every frame over the 10 seconds instead of once
- **Vigor** - A stamina regeneration bonus on every food that lasts as long as the food does; Eitr Vigor does the same for eitr
- **Stamina and Eitr Regeneration** - Base regen, regen delay, blocking factor, regen from extra stamina, a sneak skill bonus, and regeneration while encumbered or swimming
- **Regen Curves** - Faster regeneration when the bar is low and slower when it is high, for stamina and for eitr
- **Rested** - How long Rested lasts, how much each comfort level adds, and its stamina, health and eitr regeneration

**What actions cost**
- **Stamina Costs** - A multiplier for every drain: run, jump, dodge, block, attack, sneak, swim, encumbered, tools, fishing, harpoon
- **Out of Combat** - Cheaper running, jumping, dodging and sneaking when nothing targets or has noticed you
- **Free Sneaking** - Sneaking drains nothing while no enemy is near
- **Skill Discount** - Block, dodge and jump get cheaper as the matching skill rises
- **Drowning Damage** - Scale the damage taken when stamina runs out in the water

**Display**
- **Hidden Numbers** - Hide the health, stamina or eitr number on the HUD, per player
- **Food Timers** - Show or hide the timers under the food icons, per player

Also: base health and stamina without food, extra stamina from the Run, Jump, Sneak, Swim and Fishing skill levels,
skill gain multipliers, and overrides for the world's food, stamina, move stamina and stamina regen rates. Every
setting defaults to vanilla behaviour, nothing changes until you opt in; changes apply immediately without
restarting the game, and on a server the server's configuration applies to every connected player.

### Settings
The config file is `BepInEx/config/com.FeastMaster.cfg`, written on first start. The numbered sections hold the global settings, in order; below them every food, mead and cooking station has a section named after its prefab. Foods and meads are discovered from the game, so modded consumables get sections too.

| Section | What it does |
| --- | --- |
| `0. Global Settings` | Multipliers for health, stamina, eitr, duration and health regen of all foods; `Disable Food Degradation`; `Degradation Curve` (0.3 is the game's curve, 1 linear, higher fades sooner, 0 never fades); `Eat Again At` (fraction of the duration below which food can be re-eaten, game 0.5); `Allow Auto Eat` (a food that runs out is replaced by another of the same from the inventory); `Lock Configuration` (the server's values are enforced on every client) |
| `1. Health Regeneration` | `Continuous Food Healing`: the food regen amount is spread over the 10 seconds and healed a little every frame, no floating numbers |
| `2. Stamina Regeneration` | `Stamina Regen Multiplier`, `Low Stamina Regen Bonus`, `Stamina Regen Delay`, `Blocking Regen Factor` (game 0.8); `Vigor Per Stamina Point` and `Vigor Multiplier`; `Regen Curve Strength` and `Regen Curve Pivot` (faster regen when the bar is low, slower when high); `Regen Per Extra Stamina Point` (percent per point of stamina above the base) and `Count Food Stamina Only`; `Sneak Skill Regen Bonus` while crouched and still; `Encumbered Regen Fraction`, `Swimming Regen Fraction` and `Swimming Regen Delay`; `Rested Duration` and `Rested Duration Per Comfort` (seconds at comfort 1 and per level above it, the game's values by default, applying from the next rest) and `Rested Stamina/Health/Eitr Regen` |
| `3. Eitr Regeneration` | `Eitr Vigor Per Eitr Point` and `Eitr Vigor Multiplier`; `Eitr Regen Multiplier`, `Eitr Regen Delay`; `Eitr Regen Curve Strength` and `Eitr Regen Curve Pivot`; `Blocking Eitr Regen Factor` |
| `4. Stamina Costs` | One multiplier per drain: `Run Cost`, `Jump Cost`, `Dodge Cost`, `Block Cost`, `Attack Cost`, `Sneak Cost`, `Swim Cost`, `Encumbered Cost`, `Tool Cost`, `Fishing Pull Cost`, `Fishing Hooked Cost`, `Harpoon Cost` (1 = vanilla, 0 = free); `Out Of Combat Run/Jump/Dodge/Sneak Cost` on top when nothing targets or has noticed you; `Free Sneaking Without Enemies`; `Skill Discount` on block, dodge and jump at skill 100; `Drowning Damage` |
| `5. Base Values` | `Base Health` (game 25) and `Base Stamina` (game 75) with no food; `Run/Jump/Sneak/Swim/Fishing Skill Stamina`: stamina added at skill 100, scaling with the skill |
| `6. Skills` | `Run/Jump/Sneak/Swim/Fishing Skill Gain`: experience multipliers, on top of the world's skill gain rate |
| `7. World Rates` | `Food Rate`, `Stamina Rate`, `Move Stamina Rate`, `Stamina Regen Rate`: above 0 overrides the world modifier, 0 leaves the world's value |
| `8. Display` | `Hide Health Number`, `Hide Stamina Number`, `Hide Eitr Number`, `Food Timers` (Vanilla, Always, Never); `Auto Eat` (this player's own choice while the server allows it); per player and not synced |
| `9. Kitchen` | `Fermentation Time` (seconds a batch brews; 0 keeps the barrel's own, the game's is 2400) and `Batch Yield` (meads per batch; 0 keeps each recipe's own), both applying to barrels already brewing; `Cook Time Multiplier` (on every recipe of every cooking station and oven, 0.5 cooks twice as fast) and `Food Can Burn` (off: cooked food waits on the station forever); `Feast Servings` (servings a placed feast holds; 0 keeps each feast's own; a feast already eaten from keeps the servings it has left) |
| one per food | `Health`, `Stamina`, `Eitr`, `Duration`, `HealthRegen`, `Vigor` (extra percent of stamina regeneration while the food is active, added to the food's stamina times `Vigor Per Stamina Point`) and `EitrVigor` (the same for eitr) |
| one per cooking station | Named after the station's prefab; one entry per recipe, named after the raw item: seconds it takes to cook, defaulting to the game's time. `Cook Time Multiplier` applies on top. Modded stations get a section too |
| one per mead | `Duration` (while it runs, neither this mead nor another of its group, such as the healing meads, can be drunk, so it is also the cooldown), `HealthOverTime`, `StaminaOverTime`, `EitrOverTime`, `HealthRegenMultiplier`, `StaminaRegenMultiplier`, `EitrRegenMultiplier`, `RunStaminaModifier`, `JumpStaminaModifier` (-0.2 makes running 20% cheaper while the mead lasts) |

Everything except the Display section is synced from the server and hot reloads when the file is edited (in game or with ConfigurationManager). A config file from 3.3.x, 4.0.0 or 4.2.0 keeps working: the moved `Lock Configuration`, the renamed mead sections, the settings renamed in 4.1.0 and section `9. Fermenter` (renamed `9. Kitchen` in 4.3.0) are carried over on the first start.

Vigor and `Regen Per Extra Stamina Point` both reward stamina from food, so with both switched on food stamina counts twice. Pick one: Vigor for a per-food value shown on the tooltip, extra stamina for a rule that also counts meads and gear.

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod.

For manual install, drag FeastMaster.dll into the BepInEx/plugins folder.

### Bugs and feature requests
The source lives on [GitHub](https://github.com/geraldjglasgow/ValheimMods). Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.

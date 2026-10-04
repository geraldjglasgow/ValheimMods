# GrindstoneSkills - PLAN

A skills mod. Cooking (the game's own skill) came first and Sailing (a skill of the mod's own, see "Sailing" near the
end) second; Farming and Crafting are the likely next ones. This document is the design
and the roadmap while the mod is unfinished, written from the user's decisions (2026-09-27) and the game's decompiled
assembly (`assembly_valheim.dll`, build 25527674, decompiled into the scratch folder, never into this repo).

## Decisions so far (user, 2026-09-27)

- **Whose skill counts: the cook's.** The dish carries a grade rolled from the cook's level, and anyone who eats it
  gets the bonus. The cook's level also improves the kitchen itself (speed, burning, yield).
- **Grades are stars, 0 to 3.** 0 stars is exactly vanilla, so every existing dish and anything made without the
  skill is 0 stars. Stars are rolled, not fixed by level; without the roll the trash filter would be pointless.
- **Trash filter on every station:** a minimum star count per station; dishes below it are thrown away.
- **Meads carry stars:** the mead base's star passes through the fermenter into every mead it makes.
- **Ingredient stars improve the odds** of the dish made from them.
- **Name: GrindstoneSkills** (first Grindstone, renamed the same day). No Thunderstore package uses either name (all
  12,047 Valheim packages checked 2026-09-27).
- **Skill loss on death is configurable (user, 2026-09-27):** a percent of each level lost (default 5, the game's
  own) and whether progress toward the next level is lost (default on, as in the game), for every skill, hot
  reloaded.

## The skill: the game's own Cooking

GrindstoneSkills builds on the game's own Cooking skill (`Skills.SkillType.Cooking = 105`) rather than adding a cooking
skill of its own. The game already defines it and shows it in the skills panel, so a second "Cooking" would only confuse. Players'
existing levels count from the first day. No registration, load patch, skill icon or localization is needed. The
death penalty applies as for every skill.

## What the game's Cooking skill already does

Read from the game's asset data (prefab bundles, with UnityPy) and the decompiled assembly, 2026-09-27:

- **Skill definition:** the Player prefab's `Skills.m_skills` includes 105 (and Farming 106, Crafting 107, Dodge
  108), increase step 0.25, icon `meat_cooked`.
  - English text: "Cooking", described as "Cooking speed, serving tray wear and chance for bonus yields".
  - Nothing in the code reads the skill for cooking-station speed or for any serving tray; the description is ahead
    of the code.
- **Kitchen stations:** `CookingStation.m_skill` is Cooking on `piece_cookingstation`, `piece_cookingstation_iron`
  and `piece_oven`, each with `m_canGiveBonusYield`.
  - There are no spit prefabs. `piece_FrostFoundry` is a `CookingStation` with no skill and is not a kitchen.
  - XP: placing raw food raises the skill by 0.4 and taking a dish off by 0.6, both for the player at the station.
  - Bonus yield: taking a dish off gives +1 dish with chance `skillFactor * 0.25`, from the taker's level.
- **Kitchen crafting stations:** `CraftingStation.m_craftingSkill` is Cooking on `piece_cauldron`,
  `piece_MeadCauldron` and `piece_preptable`.
  - XP: 1 per item crafted.
  - Craft time: shortened by `skillFactor * m_craftDurationSkillMaxDecrease`.
  - Bonus: for stackable outputs, +1 per craft with chance `skillFactor * 0.25`.
- **Fermenter:** no skill field; nothing is skill-based.

GrindstoneSkills keeps all of this and adds to it.

**Extra food (user, 2026-09-27):** the chance of extra food grows with level, from 0 up to a configurable cap
(default 50% at level 100), with a configurable number of extra dishes. The game's own chance is 25% at level 100.
It comes from `InventoryGui.m_craftBonusChance`/`m_craftBonusAmount`, which the workbench's Crafting bonus shares.
So at kitchens both fields are replaced for the one call (`OnInteract`, `DoCrafting`) and restored afterwards
(`Core/ExtraFood.cs`).

## Which items carry stars

- **"Kitchen items" are discovered, not listed; the game marks its kitchens itself:**
  - the outputs of every `CookingStation` whose `m_skill` is Cooking;
  - the outputs of every recipe at a `CraftingStation` whose `m_craftingSkill` is Cooking;
  - the outputs of every `Fermenter` conversion.
  - A modded kitchen that sets the same fields is picked up too.
- **This includes intermediates:** mead bases, dough and unbaked pies get a star when crafted. The star only changes
  food values on edible items; on an intermediate it feeds into the next step (see "Ingredient stars").
- **Discovery:** done once when `ObjectDB` is ready, like FeastMaster discovers foods.

## Stars

- **Storage:** the item's own quality field, `m_quality = stars + 1`, so 1 means 0 stars.
  - Stacks already separate by quality: `Inventory.FindFreeStackItem` matches name, quality and world level. Every
    mod that stacks through the game's inventory therefore keeps star counts apart, OpenKeep included.
  - Quality is saved with the item, in inventories and on the ground (the item's ZDO).
  - The tooltip hides quality when `m_maxQuality` is 1, which is true for all food. The upgrade tab never offers a
    kitchen item, since its quality already exceeds its maximum.
  - **Checked in the game code 2026-09-27:**
    - Recipes consume with `itemQuality = -1` (any quality), and stations and fermenters accept items by name.
      Nothing clamps quality to `m_maxQuality`.
    - Two things needed patches:
      - `Player.HaveRequirementItems` counts each ingredient as the highest count of any single quality from 1 to
        `m_maxQuality`. For food that is quality 1 only, so starred ingredients would not count and a stew could
        not be made from 1★ meat. A scope makes quality-1 counts of kitchen items count every quality.
      - `ItemData.IsSameType` compares quality only when `m_maxQuality > 1`, so dragging a 1★ stack onto a 3★ one
        would merge them. It is patched for kitchen items, and `InventoryGrid.DropItem` swaps them instead.
    - The alternative, stars in item custom data, would count ingredients correctly with no patch, but every merge
      path would then have to be patched instead, including other mods' own.
- **Display:** the game's star image in a corner of the item icon (inventory, containers, hotbar), a tooltip line
  with the stars and the boosted values, and stars in the hover name on the ground.
- **Rolled once, where the dish is finished:**
  - cauldron, mead cauldron, prep table: at crafting;
  - cooking stations and oven: when the dish turns done;
  - fermenter: no roll; every mead takes its base's star.

### Bonuses (defaults, synced and lockable)

| Stars | Health, stamina, eitr | Duration |
| --- | --- | --- |
| 0 | vanilla | vanilla |
| 1 | +10% | +10% |
| 2 | +20% | +20% |
| 3 | +35% | +30% |

### Odds by effective level (defaults, synced and lockable)

Effective level is the cook's level plus the ingredient bonus. Odds blend linearly between rows. Rows above 100
are reachable only with starred ingredients.

| Effective level | 0 | 1 | 2 | 3 |
| --- | --- | --- | --- | --- |
| 0 | 90% | 10% | 0% | 0% |
| 25 | 45% | 40% | 15% | 0% |
| 50 | 15% | 40% | 35% | 10% |
| 75 | 5% | 20% | 45% | 30% |
| 100 | 0% | 10% | 40% | 50% |
| 130 | 0% | 0% | 25% | 75% |

### Ingredient stars

- **Bonus:** the average stars of the consumed ingredients that can carry stars adds 10 effective levels per star.
  Ingredients that never carry stars (raw meat, mushrooms) are left out of the average instead of counting as 0,
  so a stew is not diluted by its vegetables.
- **At kitchen crafting stations:** the consumed stars are recorded while `DoCrafting` runs. A prefix sets a
  recording scope, the removal patch notes the stars it takes, and a postfix rolls.
- **At stations:** raw inputs carry no stars. Inputs that do (dough, unbaked pies) count the same way, one input
  per slot.
- **Which stacks a recipe takes:** a per-player setting (unsynced), "lowest stars first" (default, protects your best
  food) or "highest stars first" (best result). It applies to `Inventory.RemoveItem` by name during crafting.
  **To check:** OpenKeep's craft-from-containers pull follows the same order.

## Cooking stations and oven

- **Placing:** the cook's client adds the raw item through our own RPC on the station's `ZNetView`, carrying the
  item, the cheated flag, the input's stars and the cook's level. The owner runs the vanilla `RPC_AddItem`, finds
  the slot it filled, and writes our per-slot keys: cook player ID, cook level, input stars. A vanilla client's
  plain add still works and counts as level 0 with no input stars.
- **Finishing (on the owner):** when a slot turns Done, roll the star from the stored level and input stars, then
  write it to the slot.
  - At or above the station's minimum: the dish stays.
  - Below the minimum: the slot is cleared with a small effect for everyone and nothing drops.
  - A trashed dish is never taken off, so the game's 0.6 take-off XP would be lost. Instead the cook gets that
    share by routed RPC. If the cook is offline, it is lost.
- **Taking off:** vanilla `RPC_RemoveDoneItem` calls `SpawnItem(name, slot, ...)` on the owner; a patch sets the
  spawned item's quality from that slot's star. The game's bonus-yield dish comes from the same slot, so it gets the
  same star. Burnt items (coal) get none. Our slot keys clear when the slot does.
- **Vanilla bug:** `SpawnItem` records "crafted by" from the owner's `Player.m_localPlayer`, which is wrong in
  multiplayer. Nothing here depends on it.
- **Cooking speed and burn window:** per slot, from the stored cook level.
  - The skill's own description promises cooking speed, but the code doesn't do it yet. If a game update adds it,
    turn ours off by default.
  - Vanilla burns at `cookTime * 2`; the perk moves that per slot.
  - **How:** a prefix on `UpdateCooking` runs after FeastMaster's, which replaces the cook times for that call. It
    computes the delta `GetDeltaTime` is about to add and pre-adjusts each slot's cooked time. Vanilla's own
    done and burnt thresholds then see the sped-up time before done and the slowed-down time after done.
  - This is exact even when a station catches up a long absence in one tick, and it never swaps a field
    FeastMaster swaps.

## Kitchen crafting stations: cauldron, mead cauldron, prep table

- **Crafting runs on the cook's own client** (`InventoryGui.DoCrafting`), so everything is local: level, roll,
  filter, XP.
- **Every crafted item rolls on its own**, including the game's bonus item. Vanilla multi-crafting adds the whole
  amount in one `AddItem`, so the postfix takes the crafted stack back out and re-adds it split by star, dropping
  trashed ones.
- **Trashing** uses up the ingredients and shows a short message such as "Trashed: 1★ Deer stew".
- **The ingredient-save perk** rolls here (see "Kitchen perks").

## Fermenter

- **Adding the base:** the cook's client adds it through our own RPC carrying the base's stars and the cook's level.
  The owner runs the vanilla add and stores both keys on the fermenter.
- **Tapping:** `DelayedTap` spawns `m_producedItems` meads on the owner; a patch sets their quality from the stored
  star, then the keys clear.
- **No filter here.** The cauldron's filter already covers bases.
- **Speed:** fermenting speed comes from the stored level of whoever added the base.

## Trash filter

- **Stored on the piece:** a minimum star count 0 to 3 in the station's ZDO, on every kitchen (cooking stations,
  oven, cauldron, mead cauldron, prep table). Shared by everyone and persistent. New stations default to "keep all".
- **Setting it:** the game's alternative interact (Shift+E by default, as the player bound it) cycles Keep all,
  1★ and up, 2★ and up, 3★ only.
  - Vanilla `CookingStation.Interact` ignores `alt` today, so Shift+E is free there.
  - Stations that use an add-food switch take the key through the switch's hover.
  - The change goes to the owner by RPC and is checked against ward access (`PrivateArea.CheckAccess`).
- **Hover text:** the setting, plus the hovering player's own chance to keep a dish, for example
  "Keeps 3★ only (your chance 42%)".
- **Server switch:** turns the filter off entirely; stations then keep everything.

## Eating

- **Remembering the stars:** the game saves only each active food's name and time left, and on load rebuilds it
  from the item prefab (`Player.Load`). So the star of each active food lives in the player's own
  `m_customData` (saved with the character) under our key, one entry per food prefab.
- **Stat bonus:** a postfix on `Player.GetTotalFoodValue` adds each food's star share of `m_health`, `m_stamina` and
  `m_eitr`.
  - This scales whatever the game computed, so it composes with FeastMaster's per-food values and its no-degrade
    switch rather than fighting them.
  - `UpdateFood` recomputes the per-food values from the base every second, so they are never written directly.
- **Duration bonus:** at eating, `m_time = m_foodBurnTime * (1 + bonus)`. Vanilla's curve clamps
  `m_time / m_foodBurnTime` to 1, so the dish stays at full strength for its bonus time, then declines as usual.
  **To check:** the HUD's food timers and FeastMaster's timer display with `m_time` above the burn time.
- **Re-eating:** vanilla blocks the same food until it is mostly used up. When it is eaten again, the new dish's
  stars replace the old ones.
- **Messages and tooltips:** the eat message and the item tooltip show the boosted values.

## Kitchen perks (defaults at level 100, linear from 0, synced and lockable)

These are in addition to what the game already gives (shorter craft time):

- Cooking speed on cooking stations and oven: +50%.
- Burn window: burns at 3x cook time instead of 2x.
- Fermenting speed: +30%.
- Extra food at every kitchen: 50% chance of +1, replacing the game's 25%.
- Kitchen crafting, ingredient saved: 10% chance to refund one consumed ingredient.

## Experience

- **Sources: the game's own.**
  - 0.4 for placing raw food and 0.6 for taking a dish off, for the player at the station;
  - 1 per item crafted at a kitchen crafting station.
- **Additions:**
  - A trashed dish credits the cook with the take-off share (see "Cooking stations and oven").
  - Tapping a fermenter gives the tapper a small amount; the game gives nothing.
- **Scaled by tier:** a scoped multiplier on `RaiseSkill` while the station interaction or `DoCrafting` runs. XP
  grows with the dish's total food value (health + stamina + eitr). Items without food value (bases, dough, unbaked
  pies) use the value of what they become. Late-biome food trains the skill much faster than cooked meat, while a
  Meadows dish stays close to vanilla.
- **Discovery bonus:** the first time a character makes each dish, it earns triple XP (tracked in player custom
  data).
- **All rates are synced settings,** with a global multiplier. Setting tier scaling and discovery to off gives
  exactly the game's own XP.

## Skill loss on death (every skill)

- **The game's rule:** `Player.OnDeath` calls `Skills.OnDeath` on the dying player's own client, unless the "no skill
  drain" buff from a recent death is active or the world resets skills (`Skills.Clear`, left alone). It calls
  `LowerAllSkills(m_DeathLowerFactor * Game.m_skillReductionRate)`: every level loses that share (0.05 in the Player
  prefab) and every skill's progress (`m_accumulator`) goes to 0.
- **The change (`Skills/DeathPenalty.cs`):**
  - For that one call the factor becomes "Skill Loss On Death". The world's death penalty modifier still scales it.
  - With "Lose Progress On Death" off, each skill's progress is put back after `LowerAllSkills`.
  - With 0% loss and progress kept, nothing runs and no "skills lowered" message shows.
- **Hot reload:** both values are read at the moment of death, so a hot-reloaded .cfg (or a server push) applies to
  the next death.

## Multiplayer

- **Required on the server and on every client.** Settings sync through Charter, and any client may own a station.
- **Persistent state lives where the game replicates it:**
  - slot and fermenter keys, and the filter, in the piece's ZDO;
  - stars in the item's quality;
  - active-food stars in the player's custom data.
- **Cook level:** sent with the add RPC. Skills are client-side in vanilla too, so this trusts the client no less
  than the game does.
- **Transient RPCs:** add-with-stars, set-filter and the XP credit.

## Settings

- **Synced and lockable (gameplay):** star bonuses, odds table, ingredient bonus per star, kitchen perk values,
  XP rates and multiplier, discovery bonus, filter on/off, skill loss on death and whether progress is lost.
- **Per player (unsynced):** star icons on/off, and ingredient order (lowest or highest first). The filter uses the
  game's own alternative interact; the icon position is fixed.

## Compatibility

- **FeastMaster:** it sets base values; GrindstoneSkills scales on top at `GetTotalFoodValue`. Test both together,
  including the no-degrade switch and auto-eat.
- **OpenKeep:** quick stack, sort, top-up and craft-from-containers must keep star counts apart and follow the
  ingredient order. Quality-based stacking should give this for free; verify in game.
- **Mods that change the game's Cooking skill** (XP rates, bonus yield) stack with GrindstoneSkills. Mods that add a
  cooking skill of their own run beside it without clashing, since GrindstoneSkills adds no cooking skill.
- **Mods that show item quality:** they may show a quality number on food. Acceptable; note it in the README if seen.

## Own names

- Plugin GUID `com.GrindstoneSkills`.
- ZDO keys, RPC names and custom-data keys prefixed `grindstone_`.
- Localization keys `$grindstone_*`; the Sailing skill's name is the game's own pattern, `$skill_<type number>`.
- Sailing's `SkillType` number is the stable hash of `grindstone_sailing`, masked positive.

## Layout (standard mod layout, copied from ShipConfig)

Planned units, each within the size limits:

- `Stars/`: quality mapping, odds, display.
- `Kitchen/`: stations, cauldron, fermenter and perks, each separate.
- `Filter/`: ZDO key, key handling, hover text.
- `Eating/`: custom data, total-value postfix, duration.
- `Experience/`: tier multiplier, trash credit, fermenter XP, discovery.
- `Sailing/`: skill registration and cheats, the three perks, experience; `Sailing/Lookout/` and `Sailing/WindCall/` the two actives.
- `Fishing/`: `Core` (angler, float scope, catch), `Fight`, `Bites`, `BigFish`, `Records`, `Rewards`, `Experience`.

## Later

- **Farming** (106) and **Crafting** (107) are game skills too, and the next modules, each with its own enable
  switch. Neither uses its skill much in the code yet.
- **Party synergy:** a feast bonus when party members eat the same cook's 3-star dish.
- **Anti-grind:** diminishing XP for the same dish within a day, if discovery plus tier scaling are not enough.

## Test checklist (LocalTesting profile, then a dedicated server with two clients)

- [ ] A character's existing Cooking level drives the odds; the game's own XP and bonus yield still work.
- [ ] Station: stars rolled at done time; a second player owning the station changes nothing; filter trashes below
      the minimum for everyone; hover chance matches the odds table.
- [ ] Cauldron, mead cauldron, prep table: multi-craft and the bonus item split by star; trashed items vanish; ingredient order setting honoured; ingredient stars
      raise the odds.
- [ ] Fermenter: all meads from a base share its star; the star survives the owner leaving mid-ferment.
- [ ] Stars stack apart in inventory, chests, OpenKeep quick stack and on the ground; survive relog and item drop.
- [ ] Eating: the bonus shows in max health, stamina and eitr; the duration bonus holds full strength, then declines;
      both survive relog.
- [ ] XP credited to the cook, not the station owner; offline cook loses it quietly; discovery bonus once per dish.
- [ ] Death: 0% loss with progress kept changes nothing and shows no message; 5% and on matches the game; an edited
      .cfg applies to the next death without restarting.
- [ ] FeastMaster together: no-degrade switch and auto-eat still work, values compose.

## Decisions made while building (2026-09-27)

- **Discovery bonus:** applies to one dish per craft, not the whole batch. A first multi-craft of n earns
  n + (D - 1) dishes' worth, not n x D.
- **Ingredient-save perk:** rolls once per batch, so a x5 multi-craft rolls five times.
- **Raw fish carries stars.** Cleaning fish at the prep table (`Recipe_Fish1`) is a kitchen recipe, so cleaned fish
  rolls stars, and those feed the cooking station as input stars. Settled with the Fishing module (user, 2026-09-27):
  whole fish never carry stars, since their quality is their size (level); the fish's level adds effective levels to
  its fillets' roll instead (see "Fishing").
- **Feasts** (prefabs with a `Feast` or `Piece` component) never carry stars. Placing one would lose them anyway.
- **Icon star position is fixed:** a column down the slot's left edge. There is no per-player corner setting, only
  on/off.
- **The ★ glyph:** the game's Averia and Norse fonts lack U+2605, but their TMP fallbacks (Noto Sans/Serif JP) have
  it. Text uses ★; icons use the game's creature-star sprite (`EnemyHud` `m_baseHud`, `level_2/star`).
- **Filter key:** the game's own alternative interact. At the cauldron, mead cauldron and prep table, Shift+E used to
  open the crafting screen like E; now it cycles the filter.
- **Messages are plain English** ("Trashed:", "Saved:", filter text). No localization keys yet.
- **Known gaps (nothing is lost):**
  - `Tombstone.EasyFitInInventory` and OpenKeep's `ReachPull` room estimate ignore quality.
  - With a full inventory whose only room is in starred stacks of the output, a craft is refused.
  - FeastMaster's "count food stamina only" regen option sums `food.m_stamina` directly, so it misses the star
    bonus (a FeastMaster change, off by default).

## Sailing

### The request (user, 2026-09-27)

A Sailing skill that increases the health of ships built by you, the sailing speed of ships commanded by you and
your exploration radius while on a ship. A milestone at level 50: a hotkey sends a pulse 100 m out from the ship in
a circle and reveals the name tags of the enemies near. Installed on the server, it enforces the configuration
(Charter already does that for every GrindstoneSkills setting).

### The skill: our own

- **The game has no sailing skill** (`Skills.SkillType` stops at Ride 110), so Sailing is GrindstoneSkills' own. Its
  type number is the stable hash of `grindstone_sailing`, masked positive: far from the game's numbers and from
  small numbers other mods pick.
- **Registration (shared by every skill of our own since Defense: `Skills/CustomSkill.cs`, `Skills/CustomSkills.cs`),
  read from the game code 2026-09-27:**
  - `Skills.GetSkill` creates a skill for any type, with the definition `GetSkillDef` finds in `m_skills`, or null.
    A null definition breaks the level-up message, so `Skills.Awake` adds Sailing's definition to every `Skills`.
  - `Skills.Load` keeps only types the enum defines (`IsSkillValid`), so without a patch a saved Sailing level is
    dropped at load and lost at the next save.
  - The skills panel and the level-up message name a skill `$skill_` + the type's name in lower case: the number
    here. The word goes to the game's localization in `Localization.SetupLanguage`.
  - The icon is the Karve's piece icon, read when `ZNetScene` wakes.
  - The death penalty (`LowerAllSkills`), the skills panel and the world's skill-gain modifier work unchanged.
- **Console:** `raiseskill` and `resetskill` match the enum's names, so "sailing" is handled by a patch and "all"
  includes Sailing (`Skills/CustomSkillCheats.cs`, for every skill of our own).
- **Level on the ZDO:** every skill of our own is published to the player's ZDO once a second when it changed
  (`Skills/CustomSkillLevels.cs`); Sailing's key is `grindstone_sailing_level`.
- **Removing GrindstoneSkills loses the Sailing level** at the next save, as for any mod-added skill: the game's
  loader skips a type it does not know.

### Perks (defaults at level 100, linear from 0, synced and lockable)

| Perk | Whose level | Default at 100 |
| --- | --- | --- |
| Ship health | the builder's, fixed when the ship is placed | +50% |
| Ship speed, sail and oars | the helmsman's | +20% top speed |
| Map exploration radius | each player's own, while aboard | +100% (100 m to 200 m) |

- **Health (`ShipwrightHealth`):**
  - `Player.PlacePiece` instantiates the ship on the builder's client, which owns it, then calls `Piece.SetCreator`.
    A postfix stores the builder's level on the ship's ZDO (`grindstone_shipwright`) and raises the new ship's max
    health (its `Awake` ran before the level was stored).
  - Every client that loads a ship raises `WearNTear.m_health` in a prefix on `WearNTear.Awake`, before the game
    adds the world level bonus and computes the health share, so every machine agrees on the max.
  - The current health in the ZDO is absolute and untouched; a new ship has none stored and reads as full.
- **Speed (`HelmSpeed`):**
  - The game moves a ship in `Ship.CustomFixedUpdate` on the ZDO owner only, and the owner is not always the
    helmsman: `Ship.UpdateOwner` moves ownership only when the owner is no longer aboard.
  - So every client publishes its own Sailing level on its player ZDO (`grindstone_sailing_level`, written when it
    changes, checked once a second). The owner finds the helmsman (`ShipControlls.GetUser` among `Ship.m_players`)
    and reads that level, or its own skill when it steers itself.
  - For that one call `m_sailForceFactor` and `m_backwardForce` (the oars) are raised, then put back, so ShipConfig's
    per-ship values stay the base.
  - The forward drag is quadratic in speed (`v² * m_dampingForward` per step), so top speed grows with the square
    root of the force: a +20% speed setting raises the force by 1.2² = 1.44.
- **Exploration (`SeaExploration`):** `Minimap.UpdateExplore` uncovers the map around the local player every two
  seconds with `m_exploreRadius`. For that call the radius grows with the local player's level while
  `Ship.GetLocalShip()` is set (they are inside a ship's deck volume, standing or at the helm).

### Experience

- **At the helm:** once a second the distance the ship moved over the water (flat, so bobbing earns nothing) earns
  40 per kilometre by default. A jump of more than 60 m in a second (a teleport, a ship that just loaded) earns
  nothing.
- **Crew:** everyone else aboard earns 25% of that while someone steers. A ship nobody steers earns nothing.
- **Pacing at 40 per km** (the game's curve, `0.5 * (level + 1)^1.5 + 0.5` per level, step 1): level 10 after
  about 2 km, level 50 after about 90 km (4 to 5 hours at the helm at 20 to 25 km/h), level 100 after about 510 km.
  The world's skill-gain modifier scales it as for every skill.
- **No experience for building ships:** deconstructing refunds every material, so building would be free experience.

### Lookout (the level 50 milestone)

- **Key:** O by default, each player's own (unbound in the game and in every mod in this workspace). It works while
  the game takes the player's input (`Player.TakeInput`: no chat, console, text field, menu, inventory or map).
- **Conditions:** Sailing on, the player's level at least `Lookout Level` (50), aboard a ship, and the player's own
  cooldown (60 s) over. Each refusal says why. With Sailing off or `Lookout Level` above 100 the key does nothing.
- **The pulse (`LookoutPulse`):** an RPC on the ship's `ZNetView` to everybody, registered in `Ship.Awake`, so every
  machine that has the ship loaded handles it once, the sender included.
  - Every client with a player draws a ring on the sea from the ship out to the radius (100 m) over 2 seconds, and
    plays the Wishbone's ping there, both as local-only copies.
  - The players aboard that ship also get the reveal and a message with the count.
- **The reveal (`LookoutReveal`):** every enemy (`BaseAI.IsEnemy`, so tamed creatures are not) within the radius,
  measured flat from the ship, keeps its name tag for 30 s.
  - The game's `EnemyHud` shows a creature's tag only within 10 m (`TestShow`) and only for a minute after the player
    looked at it (the hud's hover timer). For a revealed creature `TestShow` says yes at any distance and the hover
    timer is held at 0; afterwards the game drops tags beyond 10 m on its own.
  - A snapshot: a creature that was inside the radius keeps its tag wherever it goes. Bosses are left out, since
    their tag is the boss bar at the top of the screen.

### Wind Call (the level 25 active, requested by the user 2026-09-30)

The request: "active ability for sailing: changes the wind direction level 25. 3 minute cooldown".

- **What the game does with wind:** every machine works the wind out on its own from the world time
  (`EnvMan.UpdateWind`, each physics step) and eases into a new target over 5 s (`SetTargetWind`). A ship's sail force
  uses the wind on the machine that owns the ship (`Ship.GetSailForce`), and the game hands a ship to a player aboard
  (`Ship.UpdateOwner`). Moder's power is the precedent: every client whose player is aboard (`Ship.GetLocalShip`)
  turns its wind to the ship's bow while anyone aboard has the power.
- **Key:** K by default, each player's own (unbound in the game and in every mod in this workspace), read through the
  Hotkeys library (it fires while W is held). The lookout key moved onto Hotkeys with it.
- **Conditions:** Sailing on, the player's level at least `Wind Call Level` (25), steering a ship, and the player's own
  cooldown (180 s) over. Each refusal says why. Away from a helm, with Sailing off or the level above 100 the key does
  nothing.
- **The call (`WindCallInput`, `WindCallReceive`):** an RPC on the ship's `ZNetView` to everybody with the flat
  direction the caller's camera looks and the caller's player ID. The ship's owner writes the direction and the end
  time (world ticks) into the ship's ZDO (`grindstone_wind_dir`, `grindstone_wind_until`); every client aboard gets
  "You turn the wind toward the north-east for 60 s" (or the caller's name).
- **The wind (`WindCallWind`):** a prefix on `EnvMan.SetTargetWind` replaces the target direction with the ship's
  called wind on every client aboard while it blows; the game's intensity and its 5 s easing stay. The debug wind
  (`wind` command) and the world-edge push-back wind are left alone. Players ashore or on other ships keep the
  weather's wind, as with Moder.

### Settings

- **8 - Sailing (synced):** Sailing Enabled, Ship Health At 100, Ship Speed At 100, Exploration Radius At 100, Helm
  Experience Per Kilometre, Crew Experience Share.
- **9 - Lookout:** Lookout Level, Lookout Radius, Lookout Duration, Lookout Cooldown (synced); Lookout Key (each
  player's own).
- **35 - Wind Call:** Wind Call Level (25), Wind Call Duration (60 s), Wind Call Cooldown (180 s) (synced); Wind Call
  Key (each player's own). Section 35 because 10 to 34 were taken when it came.

### Decisions made while building (2026-09-27), for the user to confirm

- **Ship health is fixed by the builder's level when the ship is placed.** Ships built before GrindstoneSkills keep
  the game's health; repairing does not refit a ship to the builder's current level.
- **Speed covers the oars as well as the sail**, with one setting.
- **The reveal goes to everyone aboard the pulsing ship**; players nearby who are not aboard see the ring and hear
  the ping but get no tags.
- **Experience from distance only**, crew at 25%, nothing for building.
- **The lookout cooldown is per player and in memory**; a relog resets it.
- **One switch turns all of Sailing off** (perks, experience, lookout); levels are kept.

### Decisions made while building Wind Call (2026-09-30), for the user to confirm

- **The wind blows the way the caller looks** (the camera's heading), fixed in the world while it blows: looking
  ahead gives a wind at the back, and any other point can be picked. It does not follow the bow as Moder's does.
- **It lasts 60 s** (the request gave only the cooldown), then the weather's wind returns over the game's 5 s easing.
- **Only the direction changes**; the weather keeps deciding how strong the wind is.
- **It is the ship's wind, not the world's:** everyone aboard that ship gets it, nobody else, as with Moder. A
  world-wide change would let one sailor turn every other ship's wind.
- **It wins over Moder's tailwind** while it blows, since it is the more recent choice of someone aboard.
- **The cooldown is per player and in memory**, like the lookout's; several sailors aboard can call one after another,
  and a new call replaces the one blowing.

### Known gaps

- ShipConfig writes a loaded ship's max health when its own settings hot reload, which drops the Sailing bonus on
  ships already loaded until they load again (leave the area or relog).
- A changed `Ship Health At 100` or `Sailing Enabled` applies to ships as they load, not to ships already loaded.

### Test checklist

- [ ] Skills panel: Sailing with the Karve icon and its description; the level survives relog; the death penalty
      lowers it.
- [ ] Console with devcommands: `raiseskill sailing 50`, `resetskill sailing`; `raiseskill all 10` includes Sailing.
- [ ] Experience: about 40 per km at the helm, 25% of that for crew, none when nobody steers or the ship is beached.
- [ ] Health: a Karve built at level 100 takes 50% more damage to break than one built at 0; a second client agrees
      (worn and broken looks match); still so after relog.
- [ ] Speed: at level 100 a ship is about 20% faster under sail and at the oars; still so when a passenger owns the
      ship (the passenger boards first, the helmsman second).
- [ ] Exploration: the uncovered circle on the map is wider aboard, normal ashore.
- [ ] Lookout: refused below level 50, ashore and on cooldown, with a message; the ring shows on a second client near
      the ship; the crew see tags on serpents or drakes up to 100 m away for 30 s; no tags on bosses or tamed
      animals; nothing breaks on a dedicated server.
- [ ] Wind Call: refused below level 25, ashore and on cooldown, with a message; K aboard turns the sail and the
      ship HUD's wind arrow to the way you looked within a few seconds (up to 10 s: a running 5 s easing finishes
      first), the message names the compass point; after 60 s
      the weather's wind returns; K fires while W is held and not while typing in chat.
- [ ] Wind Call in multiplayer: when a passenger calls it and the helmsman owns the ship (and the other way round),
      the ship sails with the called wind and both see the same HUD arrow; a player boarding mid-call gets it; a
      player on the shore nearby keeps the weather's wind; a dedicated server shows nothing wrong in its log.

## Woodcutting

### The request (user, 2026-09-27)

Deeper Woodcutting on the game's own skill, without a grading system: Timber! (aim, callout, log safety), Domino
felling, perks (stamina refund, axe wear, Clean fell, Replanting), experience, Clean splits, Finds and Old growth,
all working on a dedicated server. Starred logs were proposed and turned down.

### What the game's Woodcutting already does

Read from the decompiled assembly and the prefab bundles (UnityPy), 2026-09-27:

- **Skill 13, "Wood Cutting"**, described as "Axe damage when hitting trees", increase step 1.
- **Who trains it:** axes and battleaxes are Axes weapons (`m_skillType` 7), but their attacks set
  `m_specialHitSkill` WoodCutting for `m_specialHitType` Tree. Tree-type targets are `TreeBase`, `TreeLog` and about 40
  `Destructible`s of type Tree (stumps, saplings, small trees, bushes, branches, old logs).
- **Damage:** a wood hit rolls its damage factor from Woodcutting (`GetRandomSkillFactor`: lerp(0.4, 1, level/100)
  ± 0.15, clamped): level 0 gives 25-55% of the axe's chop damage, level 100 85-100%. The same factor scales push force.
- **Experience:** +1 per swing that hits any wood (not per target), ×1.5 if the swing also hit a creature, Rested +50%.
  Level 100 takes about 20,300 such swings.
- **Not affected:** stamina (the Axes skill: −33% at 100), axe wear, drops, tool tiers, falling-log damage (an impact
  hit has no attacker). No food, mead, set or Forsaken power changes Woodcutting.
- **Quirk, left as it is:** the Thunderblood Axe and Greataxe have no tree special hit, so on wood they roll from Axes
  and train nothing.
- **Where things happen:** a tree's damage, fall, log spawn and canopy drops run on the tree's ZDO owner; a log's
  damage, break and drops on the log's owner; `ImpactEffect` (a falling log hitting things) on the log's owner. The
  hit's own `m_skillLevel` is the weapon skill's level (Axes).
- **A falling log's hit:** every log and log half's `ImpactEffect` has hit type **Tree** (13), not Impact (the
  component's default): blunt 50 and chop 30 (Ashlands blunt 110, chop 40, fire 20), tool tier 2, full damage from
  5 m/s, one impact per 0.25 s. Wood is immune to everything but chop, so an impact lands at most 30 on a tree.

### Foundation (`Woodcutting/Core`)

- **The hit carries the woodcutter (`WoodHit`).** Before a hit on wood is sent to its owner, fields HitData already
  sends and wood never reads are filled: `m_skill` WoodCutting, `m_skillLevel` the woodcutter's level, `m_attacker` the
  woodcutter's player, and on an impact `m_skillRaiseAmount` the chain depth. `Woodcutter.FromHit` reads it on the owner.
- **Logs remember their woodcutter.** When a tree falls, its owner writes the woodcutter (player ID, level, chain) to the
  new log's ZDO (`Felling`, `LogSpawns`); halves inherit it and the clean mark (`LogBreaking`).
- **Scopes:** `SwingScope` (the local player's melee swing; the Woodcutting raise in it is "this swing hit wood"),
  `ImpactScope` (a felled log's impact, on its owner), `Felling.Open` (SpawnLog), `LogBreaking.Open` (TreeLog.Destroy).
- **Dispatch:** the foundation calls each feature's hook, each guarded (`HookGuard`), so a failing feature never stops a
  tree from falling. Fell order: Old growth (stores the tree's size on the log), Timber, Clean fell, Replanting,
  Finds, experience.
- **Impacts:** a hit counts as a falling log's when its type is Tree or Impact (`WoodHit.IsImpact`) while the log's
  `ImpactScope` is open; a log's hit on itself stays the game's (defensive: every game log has `m_damageToSelf` off).
- **Yield:** Old growth and Clean splits each return a bonus; they add up and scale the log half's drop count.
- **Credit (`WoodCredit`):** fell and split experience goes to the woodcutter by routed RPC, like the cook credit; an
  offline woodcutter loses it. **Callouts (`WoodCallout`):** a routed RPC; every client within 40 m draws the text with
  the game's floating damage text, if its own "Show Callouts" is on.

### Features (defaults at level 100, linear from 0, synced and lockable)

| Feature | Default | Whose level, where it runs |
| --- | --- | --- |
| Fall push (Timber!) | +900% of the game's push (×10) | the feller's, tree owner |
| Log safety (Timber!) | 100% less damage from logs of your own trees | the feller's, log owner |
| Domino impact | +200% impact damage on other wood (×3), chains up to 5 trees | the feller's, log owner |
| Stamina refund | 30% of a swing's stamina when it hits wood | your own, your client |
| Axe wear | 50% less wear for swings that hit wood | your own, your client |
| Clean fell | 100% chance the stump comes out and drops its wood | the feller's, tree owner |
| Replanting | 50% chance a sapling of the same kind takes root; free | the feller's, tree owner |
| Clean split | 20% of hits on a log split it at once; +50% wood | the hitter's, log owner |
| Old growth | up to +100% wood from the biggest trees of each kind | the breaker's (else the feller's), log owner |
| Finds | 2% chance at level 0 to 15% at 100, per fell | the feller's, tree owner |

- **Timber!:** the log gets the game's own push again, times the level share, along the felling hit. "Timber!" floats
  above every tree that falls to a woodcutter.
- **Domino:** a felled log's impacts on other trees, logs and stumps are multiplied; a tree it fells counts as the
  woodcutter's (its log carries the chain on), and the woodcutter sees "Chain ×N!". Players, creatures and buildings
  take the game's own log damage.
- **Replanting** takes the stump out too. The sapling is found from the game's saplings (`Plant.m_grownPrefabs`), so
  trees without one (swamp, Mistlands, Ashlands) are never replanted.
- **Finds:** a YAML file (`GrindstoneSkills.Finds*.yml`, synced, hot reloaded) lists per biome, or per tree, what a
  tree can hide: named finds of existing items with weights. No creatures (the ambush idea is left out for now).
- **Clean split:** the hit is raised to finish the log (tool tier still applies); a whole log's halves keep the mark,
  so their wood gets the bonus; "Clean split!" floats above it.
- **Old growth is relative to the tree's kind.** Sizes differ by kind (beech 0.8 to 1.5, birch 0.5 to 1.0, firs 1.5
  to 3.0), so one absolute scale fits no forest. When a tree falls its size within its kind's range (world generator
  and sapling ranges, built at runtime) is stored on the log (`grindstone_wood_size`, 0..1) and passed to the
  halves; the bonus grows from nothing at 50% of the range to all of it at the top.
  - The range is the tree prefab's own (so Meadows birches, 0.5 to 1.0, are not judged against the Plains heath's
    1.0 to 1.5), else its log kind's. Sources: `ZoneSystem.m_vegetation` (filled on every machine in
    `SetupLocations`), saplings' `Plant.m_minScale..m_maxScale`, and a prefab's root scale when it is not 1.
  - Known gap: FirTree is one prefab for the Black Forest (2.0 to 2.5), the mountains (1.5 to 3.0) and saplings
    (1.0 to 2.5), so Black Forest firs reach at most half the bonus. A per-biome range would fix it.

### Experience (section 10, synced)

- The game's +1 per swing stays, scaled by the hardest wood the swing hit: +50% per tool tier it needs (birch and oak
  ×2, Yggdrasil ×3), and 25% for wood with less than 30 health (saplings, small trees), so they are no XP farm.
- **Fell:** 5 per tree, times its tier, to the feller, chain fells included. **Split:** 2 per log half broken into
  wood, times its tier, to the breaker.
- **Discovery:** the first fell of each kind of tree (its log prefab) ×3, recorded in player custom data.
- An Experience Multiplier over all of it. Credits never run the swing perks or the swing scaling again.

### Decisions made while building (2026-09-27), for the user to confirm

- Log safety covers the feller only (the party could come later through Party's API). It covers log halves too.
- Replanting is free (no seed taken) and always takes the stump out.
- Finds carry items only; the Greyling ambush is not in.
- The Thunderblood axes keep the game's quirk.
- **Timber!:** the extra push is the flat part of the felling direction (a tilt would only lift the log or press it
  into the stump); nearly vertical hits get none. It applies to chain fells too.
- **Domino:** every damage type is scaled (only chop lands on wood, and the drop conversions keep their majority
  type). A boosted impact takes the log's own tool tier when it is higher, so Yggdrasil logs can knock over the next
  shoot (tier 4). The chain message shows for every chain fell, including ones past Max Chain. The boost also
  applies to the log's hits on its own tree's stump, so at high levels a hard landing can knock the stump out even
  when Clean fell did not roll.
- **Clean splits:** a hit that would break the log anyway can still be a clean split (late axes break halves in one
  hit). The bonus is flat, not scaled by level; the chance is. The bonus follows the mark, whoever breaks the log.
- **Replanting:** a sapling is planted only where it could grow (biome, roof, grow space ignoring the fell's own
  tree, log and stump), so dense spots get fewer. A replanted sapling (`grindstone_replanted`) waits instead of dying
  while a log lies within its grow radius. Autumn birches get birch saplings (lookup by tree, then by log kind).
- **Experience:** wood too hard for the axe gets no tier bonus (it would be an endless farm). A felled log that
  breaks a log half credits the split to its feller. Discovery is not used up while the credit is 0.
- **Finds:** a tree listed in the file replaces its biome's table. The world's resource rate does not scale finds.
- **Chain credit needs the woodcutter nearby:** a domino fell is credited only if the woodcutter's player is loaded
  on the log's owner; otherwise the chain carries on anonymously.

### Known gaps

- Axe swings can cut a replanted sapling standing at the log's base, as with any sapling (1 health).
- Credits reach the woodcutter as one raise; the game gives at most one level per raise, so a large first-fell credit
  at a low level loses its excess.
- The woodcutter's player ID is read on the target's owner from the attacker's player ZDO. For a chain, the struck
  tree's owner may not have it (the woodcutter far away); that fell is then anonymous (no credit, no chain message).
  HitData has no other 64-bit field that is always sent.
- A tagged impact carries the woodcutter as attacker, so the game counts tree stats for them and a Destructible with
  `m_triggerPrivateArea` (Dvergr areas) reacts as if they had hit it. Check in the Mistlands.

### Test checklist

- [ ] Level 0 with every feature on plays like vanilla apart from finds (2%), callouts and experience.
- [ ] Timber!: at 100 the tree falls away from you, faster; "Timber!" for players within 40 m; your own log does not
      hurt you, another player's does.
- [ ] Domino: at 100 a beech felled into a beech knocks it over; "Chain ×2!"; Max Chain 1 stops the boost at the
      second tree; players and buildings take vanilla damage.
- [ ] Stamina and wear: an iron axe at 100 refunds 3 of 10 stamina per hit on wood; 10 swings wear 5.
- [ ] Clean fell and Replanting: no stump, its wood drops; a sapling where there is room, none in swamp, Mistlands,
      Ashlands; the sapling survives a log lying on it and grows once it is cleared.
- [ ] Clean splits: Chance 100 splits a log in one hit, the halves drop about 15 instead of 10, the damage number is
      the log's remaining health.
- [ ] Old growth: the biggest beech of an area gives more wood than a small one; logs felled earlier give none.
- [ ] Finds: Chance 100 drops a find per fell, per biome and for Oak1; the YAML hot reloads; a bad biome is refused.
- [ ] Experience: birch twice beech, saplings a quarter, the first fell of a kind triple, splits credited.
- [ ] Dedicated server with two clients, the trees owned by the other client: every credit, callout and bonus above.

## Pickaxes

### The request (user, 2026-09-27)

Deeper Pickaxes on the game's own skill, themed "the miner learns to read stone": Seams (active mining), Rich veins
(deposit stars), splash damage to touching chunks, perks, milestones (Read the rock, Echo, Unbroken), finds and
experience, all working on a dedicated server. The user decided: seams are personal to each miner; the vein bonus goes
to everyone who mines the deposit; finds are the game's own valuables only; splash is 15 damage at level 100 **shared**
across the touching chunks (not 15 each); Shatter (a level 75 milestone) is dropped because splash does its job.

### What the game's Pickaxes already does

Read from the decompiled assembly and the prefab bundles (UnityPy), 2026-09-27:

- **Skill 12, Pickaxes.** A pickaxe swing rolls its damage factor from it (`GetRandomSkillFactor`: 25-55% of the
  pickaxe's damage at level 0, 85-100% at 100), knockback included; swing stamina −33% at 100 (`GetAttackStamina`).
- **Experience:** +1 per swing that hits rock (`Attack.m_raiseSkillAmount`, raised once per swing after the hits),
  ×1.5 if the swing also hit a creature. Digging the ground (a Heightmap hit) gives nothing.
- **Not affected:** tool tier (the pickaxe's), drops (fixed tables × the world's resource rate), wear, digging.
- **Rocks take only pickaxe damage:** every rock's damage modifiers make it immune to blunt, slash, pierce, chop, fire,
  frost, lightning, poison and spirit. Pickaxe damage: Antler 18, Bronze 25, Iron 33, Black metal 49 (Stone 15), before
  upgrades.
- **Chunk health:** copper and silver 50, boulders 30-50, mud piles 5; tin and obsidian are single pieces
  (Destructible, 30). Ashlands flametal is `LeviathanLava`, a MineRock (pieces of 100, tool tier 3); the
  `FlametalRockstand` formations (chunks of 70) drop only coal.
- **Where things happen:**
  - The hit is built on the miner's client (`Attack.DoMeleeAttack`), and `MineRock5.Damage`, `MineRock.Damage` and
    `Destructible.Damage` run there. They find the chunk (area index) and send the hit to the rock's ZDO owner.
  - **One swing, many hits:** a pickaxe attack (`m_pickaxeSpecial`) sends one full hit per chunk collider its swing
    touches; the skill is raised once, after all of them.
  - The owner applies the damage (`MineRock5.RPC_Damage` → `DamageArea`, `MineRock.RPC_Hit`, `Destructible.RPC_Damage`)
    and spawns the drops (`m_dropItems.GetDropList()` per broken chunk; `DropOnDestroyed` for a Destructible).
  - The hit already sends `m_skill` (Pickaxes), `m_skillLevel` (the weapon skill's level, so the miner's Pickaxes
    level), `m_toolTier` and `m_attacker` (the miner's player). **Owner-side features read the miner from the hit; no
    tagging is needed** (only the splash mark, below).
- **Intact deposits:** `rock4_copper`, `silvervein`, `rock3_silver`, `FlametalRockstand`, `mudpile`, `mudpile2`,
  `mudpile_beacon` and the big boulders are 1-health Destructibles without drops of their own. Their first hit replaces
  them with their `_frac` MineRock5 (`m_spawnWhenDestroyed`), instantiated at the same position and rotation, and the
  owner damages it at once with the same hit. Some share another prefab's fractured form: `mudpile_beacon` becomes
  `mudpile_frac`, `rock1_mistlands` becomes `rock1_mountain_frac`, `BigRock` becomes `rock4_bigrock_frac`.
- **Buried deposits:** `silvervein` carries a `Beacon` (the Wishbone's target) on a child object, `mudpile_beacon` on
  itself. `rock3_silver`, `mudpile`, `mudpile2` and every fractured form carry none, so a vein is no longer buried once
  its first hit breaks it open.

### Foundation (`Pickaxes/Core`)

- **Rock (`Rock`, `RockCatalog`, `RockPieces`):** a MineRock5, MineRock or Destructible whose damage modifiers make it
  immune (or Ignore) to chop and blunt but not to pickaxe. That is discovered, not listed, so modded rocks count, and
  dungeon gates, iron walls and bar stacks (MineRocks without such modifiers) do not.
  - **Kind:** its prefab name without `_frac`; an intact deposit takes its fractured form's kind, so the two are one
    kind (`mudpile_beacon` is `mudpile`).
  - **Name:** the game's (`m_name`, a Destructible's HoverText, or its fractured form's). A rock without one (the ice
    rocks, the flametal rockstand) goes by its first ore item's name ("Ice", "Coal"). Echo and every callout use it.
  - **Ore deposit:** its drop table (a Destructible: its `DropOnDestroyed`, or its `m_spawnWhenDestroyed` prefab's table)
    holds any item not in "Plain Stone Items" (default `Stone, Grausten`). Other rocks are plain stone.
  - **Also known per rock:** biome at its position, tool tier, one chunk's health in the prefab, whether it carries a
    `Beacon` (on itself or a child), whether it has chunks (MineRock5 or MineRock).
- **The hit carries the miner (`Miner.FromHit`):** on the owner, a hit with `m_skill` Pickaxes gives the miner's player
  ID (from the attacker's ZDO), level (`m_skillLevel`) and ZDOID. A hit without Pickaxes is left to the game.
- **Swing scope (`MineSwing`):** the local player's melee swing with a Pickaxes weapon, like `SwingScope`. It records
  the rocks the swing hit. The Pickaxes raise inside the scope is "this swing hit rock": experience scaling, wear
  reduction and Echo run there, once per swing.
- **Local hit (`MineHit`):** the local player's pickaxe hit on a rock, before the game sends it: recorded in the swing,
  handed to discovery and to Seams (which may raise its damage), and marked for splash (`SplashOnce`): every hit on a
  MineRock5 or an intact deposit after the swing's first carries `m_skillRaiseAmount` −1. HitData sends that field and
  nothing on a rock's path reads it; the game raises skills from `Attack.m_raiseSkillAmount` on the attacker's client.
- **Owner hit (`OwnerHit`):** after the owner applied a pickaxe hit to a MineRock5 chunk, splash runs, unless the hit
  carries the mark, or another hit of the same handler already splashed (an intact deposit's owner re-sends its first
  hit to every new chunk within 5 cm of the hit point).
- **Break (`MineBreak`):** on the owner, when a pickaxe hit (or splash) breaks a chunk of a rock, or destroys a
  single-piece rock (`ChunkBreaks`, `PieceBreaks`). The context (`RockBreak`) gives the rock, chunk centre, drop
  table, miner and cheated flag. An intact deposit turning into its fractured form is no break.
  - Features add **extra rolls** of the chunk's own drop table to the context (vein bonus, extra ore, clean strike).
  - The foundation spawns them once, as the game spawns drops. The world's resource rate applies as usual.
  - Finds spawn their own items.
- **Clean strike marks (`CleanStrikeMarks`):** an RPC on the rock's ZNetView to its owner (`grindstone_CleanStrike`,
  int area, ZDOID miner). It is sent before the hit, so it arrives first. The owner keeps the mark in memory for 30 s;
  the break of that chunk by that miner takes it.
- **Callouts (`MineCallout`):** floating text, as the Woodcutting callouts.
  - Shown locally for the miner's own events.
  - Broadcast to players within 40 m for finds.
  - Each receiver's own Pickaxes "Show Callouts" decides.
- **Dispatch:** every feature hook is called through the shared hook guard (`HookGuard`), so a failing feature never
  stops a rock from breaking.
- **Shared with the other modules (`Core`):** `HookGuard` (was Woodcutting's `WoodGuard`), `FloatingText` (the game's
  floating damage text, both modules' callouts), `PlayerIds`, `ToolWear` (both wear perks) and `WishbonePing` (the
  Wishbone's ping as a local copy, a 3D sound: the Sailing lookout and Echo).

### Features (defaults, synced and lockable)

| Feature | Default | Whose level, where it runs |
| --- | --- | --- |
| Seams | 10% of swings at level 0 to 40% at 100 open a seam; window 2.6 s to 5.6 s | your own, your client |
| Clean strike | ×2 damage, +1 drop roll on ore deposits, +1 experience, next seam at once | your own; ore on the owner |
| Unbroken (level 100) | each chained clean strike +20% damage, up to 5 links | your own, your client |
| Splash | 15 damage per swing at 100, shared across the chunks touching its first chunk; grows every 10 levels (1.5 per step) | the miner's, rock owner |
| Rich veins | 0★ 60%, 1★ 25%, 2★ 11%, 3★ 4% per ore deposit; +25% drop rolls per star | anyone's, rock owner |
| Read the rock (level 25) | hover shows the vein's stars and chunks left | your own, your client |
| Extra ore | 30% chance of +1 drop roll per broken chunk of an ore deposit | the miner's, rock owner |
| Pickaxe wear | 50% less wear for swings that hit rock | your own, your client |
| Echo (level 50) | a swing on rock pings the nearest ore deposit within 40 m, every 10 s | your own, your client |
| Finds | 0.2% at level 0 to 1% at 100 per broken chunk or rock, × its health / 50 (at most 1) | the miner's, rock owner |

- **Seams (multi-chunk rocks: MineRock5):**
  - A seam is a chunk that stands after the swing (the chunk being mined included, unless the swing broke it),
    within the pickaxe's reach (+0.3 m) of where the miner stands, and visible from the miner's eye (a chunk the
    swing touched counts as visible). A machine that does not own the rock subtracts the swing's damage from the
    health the owner last saved in the ZDO to know whether the chunk survives. Marked by a local-only glow (gold
    light, halo and the game's glint star) and a soft clink. Each miner sees only their own seams.
  - It opens once the swing is over, when the swing's one roll of the chance succeeds on a rock with no open seam.
  - A hit on the seam chunk inside the window is a clean strike: the hit's damage is multiplied before it is sent,
    "Clean strike!" floats up, and the next seam opens straight away with a full window (a chain). One per rock and
    swing.
  - A swing that hits the rock elsewhere, or a window running out, ends the chain.
  - Plain stone rocks get seams too (damage and experience), but no extra roll.
- **Unbroken:** from "Unbroken Level" (100), each clean strike after the first in a chain adds +20% of the base to
  the multiplier, up to 5 links (×2, 2.4, 2.8, 3.2, 3.6, 4). The callout counts the links at any level ("Clean strike ×3!").
- **Splash:**
  - On the owner, after the swing's first hit on a MineRock5 chunk that passed the tool tier check. A swing sends one
    hit per chunk it touches; the later ones carry the splash mark and do not splash (see the foundation).
  - Touching chunks are the intact ones found by an overlap box around the hit chunk (the game's own support test).
    They share the level's amount equally. The boxes are filled before the first hit, while its chunk is whole.
  - The damage goes through the game's chunk damage with its chip sound, damage numbers and noise muted; the
    crumble of a chunk it breaks stays.
  - A chunk it breaks drops, counts for every break feature and triggers the game's support check.
  - Splash never splashes again and gives no experience. Seams don't multiply it.
- **Rich veins:**
  - Stars come from a stable hash of the world seed and the deposit's position (to 0.5 m), so every machine agrees
    with nothing stored, and the intact deposit and its fractured form agree.
  - Ore deposits only.
  - The bonus is extra drop rolls on every broken chunk: whole rolls, plus a chance for the fraction.
- **Read the rock:** from "Read The Rock Level" (25), the hover of an ore deposit shows "Rich vein ★★" or "Plain
  vein". A multi-chunk rock also shows "N of M chunks left". Below that level nothing shows, but the bonus still applies.
- **Echo:**
  - From "Echo Level" (50), a swing that hits rock finds the nearest loaded ore deposit within 40 m. The one just hit,
    fully mined ones and any carrying a `Beacon` are skipped, so buried silver stays the Wishbone's job.
  - It plays the Wishbone's ping 4 m towards the deposit (a 3D sound, so it comes from that direction) and floats
    "<name>, 32 m" there, for the miner only.
  - Per-player cooldown of 10 s. Silent when nothing is near.
- **Finds:** GrindstoneSkills.MineFinds*.yml (synced, hot reloaded), per biome or per deposit kind, the same format as
  the Woodcutting finds. Only the game's own valuables by default: Amber, Amber pearl, Ruby. The item drops at the
  chunk and "Found <name>!" floats there for players near.
  - The chance is scaled by the chunk's health in the prefab over 50, at most 1, so rocks of tiny chunks are no find
    farm: copper and silver chunks and bigger 100%, tin, obsidian and small boulders (30) 60%, mud piles (5) 10%,
    Ashlands floor pieces (1) 2%.

### Experience (section 14, synced)

- **Swings:** the game's +1 per swing that hits rock stays, scaled by the hardest rock the swing hit:
  - +25% per biome step of the rock's biome: Meadows ×1, Black Forest ×1.25, Swamp ×1.5, Mountain ×1.75, Plains ×2,
    Mistlands ×2.25, Ashlands and Deep North ×2.5;
  - ×1.5 on ore deposits;
  - no scaling when the rock is too hard for the pickaxe.
- **Clean strike:** +1 × the rock's scale, credited at once on the miner's client.
- **Discovery:** the first hit on each ore deposit gives +10 × its scale. Deposits are told apart by name, so kinds
  that read the same count once (mudpile, mudpile2 and mudpile_old; both giant helmets; the ice rocks). It is recorded
  in player custom data (`grindstone_mined`).
- An Experience Multiplier applies over all of it.

### Settings

- **14 - Pickaxes:** Pickaxes Enabled, Show Callouts (each player's own), Plain Stone Items, Experience Multiplier,
  Experience Per Biome Step, Ore Experience Bonus, Clean Strike Experience, Discovery Experience.
- **15 - Seams:** Seam Chance At 0, Seam Chance At 100, Seam Window At 0, Seam Window At 100, Clean Strike Damage, Unbroken
  Level, Unbroken Bonus Per Link, Unbroken Max Links.
- **16 - Veins:** Vein Chance 1 Star, Vein Chance 2 Stars, Vein Chance 3 Stars, Vein Bonus Per Star, Read The Rock Level,
  Echo Level, Echo Radius, Echo Cooldown.
- **17 - Pickaxe Perks:** Extra Ore Chance At 100, Pickaxe Wear Reduction At 100, Splash Damage At 100.
- **18 - Mine Finds:** Find Chance At 0, Find Chance At 100 (the tables are in GrindstoneSkills.MineFinds*.yml).

### Decisions made while building (2026-09-27), for the user to confirm

- Seams and splash work on every multi-chunk rock, boulders included. Extra rolls from clean strikes, extra ore and
  veins are for ore deposits only, so stone doesn't pile up.
- Every drop bonus is an extra roll of the chunk's own table, not a count of one ore item.
- Experience scales by biome rather than tool tier: the game's rock tiers are uneven (copper and flametal chunks need
  tier 0, silver and obsidian 2). Ocean, no biome and modded biomes count as Meadows. "Too hard" is the game's own
  tool check, the world-level tool lock included.
- Discovery is on the first hit of a deposit, on the miner's own client, so it needs no credit RPC. A deposit too
  hard for the pickaxe is not recorded yet, nor is one while its credit is 0.
- The Experience Multiplier also scales Pickaxes raises from swings that hit only creatures.
- Chunks that fall when the chunks under them are mined away (the game's support check) count as broken by the miner
  whose hit or splash took their support: they get the vein bonus, extra ore and finds too.
- **Seams:** a seam opens after the swing (the whole swing is known first); the chance is rolled once per swing, and
  only for the first seam: each clean strike opens the next at once, at any level. Only chunks the miner can see. A
  pickaxe too weak for the rock neither opens nor strikes seams, and does not end a chain.
- **Clean strike roll:** a clean strike that does not break its chunk still pays when that miner breaks the chunk
  within 30 s, however it breaks.
- **Splash** is once per swing, around the swing's first chunk hit: a swing touching three chunks splashes 15, not 45.
- **Clean strike balance (user, 2026-09-27):** Clean Strike Damage ×2, not ×2.5, so at level 100 a bronze pickaxe
  (21-25 per hit) needs Unbroken's second link (×2.4) to break a 50-health copper or silver chunk in one hit, while
  iron already does; Clean Strike Experience 1, not 2, so a kept chain earns about twice a swing's experience, not
  three times.
- **Splash grows in steps of 10 levels (user, 2026-09-27):** nothing below level 10, then a tenth of Splash Damage At
  100 per step (1.5, 3, ... 15 at level 100), instead of growing smoothly.
- **Rich veins:** a vein without stars reads "Plain vein", so a reader can tell it from no hover.
- **Extra ore** has no callout.
- **Pickaxe wear** is reduced on every swing that hits rock: plain stone and rock too hard for the pickaxe too.
- **Echo:** the cooldown starts only on a ping (a silent swing leaves it ready). The text ignores Show Callouts: it is
  the feature, not a callout. The radius is 3D. Ore is what the foundation counts as ore, so Leviathans, giant bones
  and armour, ice rocks, flametal rockstands (coal) and mud piles without a beacon are pinged.
- **Finds:** cheated breaks find nothing; the Ocean has a table; Deep North copies the Ashlands; the per-deposit
  example is commented out; items pop out 0.4 m towards the miner. The health share uses the prefab's health, before
  the world level, so the 50 stays one yardstick (confirmed by the user, 2026-09-27). The world's resource rate does not scale finds.
- **Names:** deposits without a name of their own are called by their ore ("Ice", "Coal").

### Known gaps

- Clean strike marks live in the owner's memory: an owner change between the clean strike and the break loses the
  extra roll.
- Credits reach the miner as one raise; the game gives at most one level per raise. Discovery is credited before the
  hit is sent.
- Echo sees only loaded objects. Non-owners' chunk healths lag up to 10 s (the game reloads them), which Echo and
  "chunks left" follow; broken chunks reach everybody at once.
- When the miner owns the rock, chunks broken by splash count in their Mine Hits and Mines stats. Splash does not
  trigger a Dvergr area (`m_triggerPrivateArea`) as a hit does.
- A swing whose first rock hit does not land (its chunk already broken on the owner, lag) does not splash.
- The intact flametal rockstand has no hover (the game gives it none); its fragments show the vein lines alone. All
  texts are English.
- Seams: the glow and sound constants (top of `SeamGlow`, `SeamSound`) are untested; the sight test aims at the
  chunk's centre; lag can still pick a chunk that then vanishes (the seam closes quietly), since the survival
  estimate ignores other mods' damage changes; a seam opens one frame after its swing.

### Test checklist

- [ ] Pickaxes Enabled off: rocks, drops, hover, sounds and experience exactly vanilla; no seams, no Echo.
- [ ] Experience: neutral settings give vanilla; Meadows boulder ×1, Black Forest copper ×1.875, Mountain stone ×1.75,
      Mountain silver ×2.625 with an iron pickaxe and ×1 with bronze.
- [ ] Discovery: the first hit on copper floats "Discovered Copper deposit!" and gives 18.75 once, also after relog;
      none on stone; it waits for the tool tier; mudpile and mudpile2 are one discovery; an ice rock reads "Ice";
      Show Callouts off hides the text but credits; Discovery 0 records nothing.
- [ ] Seams at level 0: about 1 swing in 10 on a multi-chunk rock opens a glint on the chunk being mined or one
      within pickaxe reach, gone after 2.6 s; readable in daylight and at night; Seam Chance 0: none.
- [ ] Clean strike: "Clean strike!", the ×2 damage number, 1.875 experience on Black Forest copper, a new seam at
      once with a higher clink, "Clean strike ×2!"; a miss or expiry ends the chain.
- [ ] Clean strike roll: an ore chunk it breaks drops a second roll, a boulder chunk does not.
- [ ] Unbroken at 100: a chain goes ×2, 2.4 … 4 and stays; below Unbroken Level ×2 flat; 101 turns it off.
- [ ] A pickaxe too weak for the rock opens no seams and does not end a chain.
- [ ] Seams close without errors when Pickaxes is turned off, the rock unloads, its last chunk breaks, the player
      dies or logs out.
- [ ] Splash at 100 on copper: 15 in total per swing however many chunks it touches, shared by the chunks touching
      the first; no numbers or chip sound on them; the first hit on a never-checked rock splashes too.
- [ ] Splash breaks: the chunk crumbles and drops with vein bonus, extra ore and finds; unsupported chunks fall and
      count; the last chunk by splash destroys the rock cleanly.
- [ ] Splash: nothing below level 10, at setting 0 or below the tool tier; 1.5 at level 10 to 19, 7.5 at 50 to 59, 15
      at 100; no experience; no re-splash; a clean strike does not raise it; boulders and mud piles work.
- [ ] Rich veins: two players see the same stars on a deposit, intact and broken open, and after a restart; 3★ at
      bonus 100 gives about 4× ore at any level, splash and collapsed chunks and the last chunk included.
- [ ] Read the rock: nothing below 25; from 25 "Rich vein ★★" or "Plain vein" and "N of M chunks left" counting
      down; tin and obsidian stars without the chunks line; boulders never; synced chance changes show within a second.
- [ ] Extra ore at setting 100: a second roll on copper, tin and silver, none on stone, splash and collapses included.
- [ ] Wear at 100: half the wear (none at setting 100), never above max; swings on creatures or the ground wear as
      before.
- [ ] Echo: nothing below Echo Level; at 50 the ping comes from the deposit's direction with "Copper deposit, N m";
      the cooldown; buried silver and beacon mud piles never, opened veins and plain mud piles yes; fully mined never,
      partly mined yes; Show Callouts off still shows; Echo Level 101 off, 0 everyone; no hitch near a large base.
- [ ] Echo on the first hit of an intact copper deposit names another deposit, never the one hit.
- [ ] Finds at chance 100/100: a copper chunk always drops a valuable towards the miner, "Found a lump of amber!" for
      players within 40 m; a mud pile chunk about 1 in 10; the silvervein example works uncommented; the YAML hot
      reloads, a bad biome is refused, a misspelled item is logged once; a cheated miner finds nothing.
- [ ] Woodcutting and Sailing unchanged: axe wear refund as before; the lookout ping sounds as before.
- [ ] **Dedicated server with two clients** (A mines, B stands by), rocks owned by the server, then by B:
  - [ ] A's seams, Echo and discovery texts are invisible to B; B breaking A's seam chunk closes the seam.
  - [ ] A's clean strike damage and roll, splash, veins, extra ore and finds work at A's level.
  - [ ] Drops and "Found …!" show for both; no splash damage numbers anywhere; nothing in the server log.
  - [ ] The first hit on an intact deposit splashes once, and names another deposit for Echo.

## Foraging

### The request (user, 2026-09-27)

A Foraging skill for picking berries, mushrooms, flowers, thistles and the like. From the ideas offered, the user took
the first set: stars on picks that feed Cooking, experience and extra yield through the game's own pick hook,
first-pick experience, better odds at each plant's best time, and sweep picking. Answers: flint, stones and branches
count, but never carry stars; stars are rolled on pick (not stored on the plant); Foraging is the mod's own skill,
and the user makes its icon.

### What the game already does with wild picks

Read from the prefab bundles (UnityPy) and the decompiled assembly, 2026-09-27:

- **The game trains Farming with wild plants.** `Pickable.m_pickRaiseSkill` is Farming (106) on raspberry,
  blueberry, cloudberry and lingonberry bushes, red, yellow and blue mushrooms, smoke puffs, fiddleheads, thistle,
  dandelion, wild barley and wild flax, and on every crop. Flint, stone, branches, royal jelly and the rest have None.
- **Its pick hook:** `Pickable.Interact` runs on the picker's client. With a skill set it raises it by 1 (not again
  while the pick is in flight, `m_pickedLocal`) and rolls one extra item with chance
  `skillFactor * m_maxLevelBonusChance` (0.25 everywhere), showing "+1". It then sends `RPC_Pick(bonus)` to the
  plant's owner, which spawns the items (`Drop`: instantiate, `ItemDrop.OnCreateNew`) and the plant's extra drops.
- **Crops:** a crop is a pickable some `Plant.m_grownPrefabs` lists. Several are the same prefab wild and planted
  (Jotun puffs, magecap, `Pickable_Seed*`, `VineAsh`); `Plant.Grow` does not mark what it grows.
- **Environment:** `EnvMan.IsDay`, `IsNight` and `IsWet` are the local player's.

### The split with Farming (agreed with the Farming module, 2026-09-27)

- **Crops are Farming's, wild or planted:** a pickable whose prefab name is in any `Plant.m_grownPrefabs`
  (`Crops.IsCrop`, shared in Core since Farming merged; it replaced Foraging's private `ForageCrops`). So wild Jotun
  puffs, magecap, seed carrots, turnips and onions, and vineberries train Farming.
- **Forage is everything else whose item is on the Forage list.** Wild barley and flax (`Pickable_Barley_Wild`,
  `Pickable_Flax_Wild`) are not crop prefabs, so they are forage.

### Design (`Foraging/`)

- **The skill:** a `CustomSkill` (`ForagingSkill`, identity `grindstone_foraging`, level published as
  `grindstone_foraging_level`). Icon: `assets/skill_foraging.png` embedded when present, else the Raspberry icon.
- **The list (`GrindstoneSkills.Forage*.yml`, synced, hot reloaded):** one entry per item prefab with `stars`, `best`
  (day, night, wet, dry; all listed must hold) and `experience` (a factor). Default: berries, mushrooms, smoke puffs,
  fiddleheads, thistle, dandelion and royal jelly with stars; flint, stone, grausten, wood, frostwood (×0.5
  experience), wild barley and flax without.
- **Starred forage carries stars like dishes:** items with `stars` join `Kitchen`'s items (`ForageStarItems`, via
  `Kitchen.AddItem`), so stacking, icons, tooltips, recipe counting, the ingredient average and eating work unchanged.
  Items join from every file applied in the session and never leave while the game runs.
- **The pick (`ForagePick`, picker's client):** for forage, the plant's `m_pickRaiseSkill` becomes Foraging and
  `m_maxLevelBonusChance` the Extra Yield Chance for the one call, then both are put back. The game's own code then
  raises Foraging and rolls the extra item from the Foraging level.
- **Stars (`ForageMarks`, `ForageSpawn`):** before the game's `RPC_Pick`, the picker sends `grindstone_ForageMark`
  (float effective level: Foraging level, plus Best Time Levels at the plant's best) to the owner, who keeps it per
  plant and sender for 10 s. Routed RPCs from one peer arrive in order. While `RPC_Pick` runs with that sender's mark,
  every new item whose entry has stars rolls its own stars from the odds table at that level and is saved at once.
- **Experience (`ForageXp`):** the game's raise, scaled inside the pick's scope to Experience Per Pick × the item's
  factor × (1 + Experience Per Biome Step × the Pickaxes biome step of the plant's spot), × Discovery Multiplier the
  first time the character picks the item (player custom data `grindstone_foraged`, callout "Discovered X!").
- **Sweep (`ForageSweep`):** after a top-level pick, every plant of the same prefab within level / 100 × Sweep Radius
  At 100 (from Sweep Level) that can be picked is picked with the game's `Interact`, each a full forage pick.
- **Hint (`ForageHover`):** a starred plant with a best time adds "Best picked at night" or "At its best now" to its
  hover text.

### Settings

- **33 - Foraging:** Foraging Enabled, Show Callouts and Show Hints (each player's own), Experience Per Pick (3),
  Experience Per Biome Step (25%), Discovery Multiplier (3).
- **34 - Forage Perks:** Extra Yield Chance At 100 (50%), Best Time Levels (20), Sweep Level (25), Sweep Radius At
  100 (4 m).
- The star odds are the Cooking odds table (section 3), read at the forager's effective level.

### Decisions made while building (2026-09-27), for the user to confirm

- **Foraging takes wild picks away from Farming** while it is on; with it off, picks are the game's again.
- **Each item rolls its own stars**, so one pick of royal jelly (5 items) can give several stacks.
- **Pacing:** 3 per pick in the Meadows up to 7.5 in the Ashlands (the skill's step is 1, the game's curve), so
  level 50 takes about 1,200 picks of Meadows forage or 500 of Ashlands forage, and level 100 about 6,800 or 2,700.
  Flint, stones and branches count half.
- **Default best times:** berries a dry day, red, yellow mushrooms and smoke puffs rain, blue mushrooms and thistle
  night, dandelion, fiddlehead and royal jelly day.
- **Sweep** needs no key: it happens on every pick from Sweep Level, never on a held (repeat) interact. Swept picks
  earn experience and roll stars like any pick.
- **Berries and mushrooms now count in the ingredient average** (they used to be left out as items without stars),
  so a 0★ mushroom lowers a dish's odds a little where it used to count for nothing.

### Known gaps

- Turning `stars` off for an item after starred copies exist: after the next restart recipes no longer count those
  copies (they are not star items any more). The file says so.
- A lost mark (the plant's owner changing between the mark and the pick) gives a plain pick.
- A vanilla client's picks are plain and train Farming, as without the mod.
- All texts are English.

### Test checklist

- [ ] Skills panel: Foraging with its icon (Raspberry until the PNG is in); `raiseskill foraging 50`; the level
      survives relog and the death penalty lowers it.
- [ ] Picking a raspberry at level 0 trains Foraging, not Farming; "Discovered Raspberries!" and triple experience
      once; a planted carrot still trains Farming; wild Jotun puffs train Farming.
- [ ] Stars: at `raiseskill foraging 100` most berries come out starred, stacks stay apart, the icon shows the stars,
      eating a 3★ raspberry gives more; flint and branches never starred.
- [ ] Best time: a thistle at night hovers "At its best now", by day "Best picked at night"; Show Hints off hides it.
- [ ] Extra yield: about half of picks at 100 show "+1".
- [ ] Sweep: nothing below 25; at 100 one pick clears the same kind within 4 m, not other kinds, not crops.
- [ ] Cooking: Queen's jam from 3★ berries rolls better than from 0★.
- [ ] Foraging Enabled off: picks as in the game (Farming, 25%, no stars, no hints, no sweep).
- [ ] Dedicated server with two clients, the bushes owned by the other client: stars, extra yield, sweep and
      experience at the picker's level; nothing in the server log.

## Defense

### The request (user, 2026-09-27)

A Defense skill: more max health (25 at most), a little out-of-combat regeneration, +1% health from food per 10
levels (+10% at 100), 10% less damage taken at 100, better blocking and staggering, 10% cheaper dodges at 100; trained
mostly by blocking with a shield and a little by every hit taken; plus more ideas. The user then asked for everything
proposed, with the best decision wherever the request was vague.

### The skill: our own

- **The game has no Defense skill**, so it is a `CustomSkill` (`Defense/Core/DefenseSkill.cs`), type number the stable
  hash of `grindstone_defense`, level published under `grindstone_defense_level`. The game's Blocking skill (+1
  experience per block, +2 per parry, up to +50% block power) is untouched: Blocking is the shield, Defense the body.
- **Icon:** the user's helmet-and-shield picture, `assets/skill_defense.png` (64x64), else the iron helmet's icon.
- **Everything runs on the defending player's own client**, which owns their character. Read from the game code
  2026-09-27: `Character.RPC_Damage` (difficulty, block, armour), `Humanoid.BlockAttack`, `Character.ApplyDamage`,
  `Player.GetTotalFoodValue` (via `UpdateFood`), stamina use and stagger all run on the owner. Only Shield Wall reads
  another player's state: their blocking flag and left-hand item (both in their ZDO) and their published level.

### Core perks (defaults at level 100, linear from 0, synced and lockable)

| Perk | Default at 100 | How |
| --- | --- | --- |
| Max health | +25 | added after the foods in a `GetTotalFoodValue` postfix (`Vitality`) |
| Food health | +10% | of the food part of that total, after the cooking stars' postfix (lower priority) |
| Damage reduction | -10% | every source, in an `ApplyDamage` prefix: after armour, before the world's damage-taken modifier |
| Regeneration | 1% of max health per 10 s | after 10 s out of combat, scaled by `SEMan.ModifyHealthRegen` (`Recovery`) |
| Poise | +25% | `Character.GetStaggerTreshold` for the local player; a stagger during a block breaks the guard |
| Parry window | +0.1 s | the game's 0.25 s is a constant in `BlockAttack`; a timer inside the wider window is scaled into it for the call |
| Block stamina | -10% | `SEMan.ModifyBlockStaminaUsage` only while `BlockAttack` runs (the equipment readout calls it too) |
| Dodge stamina | -10% | `Player.GetDodgeStaminaUse`, after the game's Dodge skill (-50% at 100, so -55% with both) |

- **Out of combat** means no attack started (`Humanoid.m_lastCombatTimer`), no hit with an attacker and no damage
  taken for `Out Of Combat Delay` seconds.
- **The hit scope (`IncomingHit`):** a prefix on `RPC_Damage` records the hit as it arrived (raw damage, attacker,
  whether it trains); the block hooks (`BlockHooks`, `BlockState`) and `DamageIntake` record what happened; the
  finalizer credits experience and Hardened. A dodged hit leaves `RPC_Damage` early and gives nothing.

### Experience

- **Only hits that train:** hit type `EnemyHit` from a non-player, or `PlayerHit` from another player while `Player
  Hits Train` is on (off by default). Falls, drowning, fire, poison ticks, lava and anything without an attacker give
  nothing.
- **Amounts:** a shield block 1, a parry ×2, a block or parry with a weapon ×0.5, a hit that hurt without being
  blocked 0.5; the first block against each kind of creature ×3 (name token such as `$enemy_troll`, kept in player
  custom data `grindstone_blocked`, "First block: Troll" floats over it).
- **Hit size:** √(raw damage ÷ 10), between 0.5 and 4: a hit of 10 is size 1, 40 is size 2, 160 or more size 4.
  Raw damage is before difficulty, blocking and armour, so armour does not slow training.
- **Cooldown:** 0.5 s between two credited hits. The step is 1 (the game's curve, as Sailing). An Experience
  Multiplier applies over all of it; the world's skill-gain modifier applies as to every skill.

### Milestones (synced; a level above 100 turns one off)

- **Riposte (25):** a held parry arms it for 2 s; the first attack started in that time is the riposte; its melee hits
  deal +25%. "Riposte!" floats at the first hit. No stagger of its own (the user's call, 2026-09-28): it used to set
  the hit's `m_staggerMultiplier` to 100, which makes the target's owner force a stagger in `RPC_Damage`; now the
  multiplier stays as the attack set it, so a riposte staggers only through the stagger damage of its (bigger) hit,
  like any hit.
- **Shield Wall (50):** worked out on the sheltered player's client in `DamageIntake`: another player who is blocking,
  shows a shield in the left hand (their `VisEquipment`), has Defense 50 (published level), and stands within 4 m in
  front of the local player (the local player is behind them, flat, against their facing) takes 10% off. Blockers do
  not add up.
- **Hardened (75):** each training hit that hurts without being blocked adds a stack, up to 5; each takes 3% off for 8
  s after the last stack. The hit that adds a stack is not reduced by it.
- **Last Stand (100):** damage from anything that would take the last health is scaled to leave 1; then 2 s of no
  damage at all (the `ApplyDamage` prefix skips it) and a 10-minute cooldown. "Last Stand!" in the middle of the screen.
  Not in god mode.

### Guard perks (defaults at level 100, linear from 0, synced)

- **Reflex (10%):** a blockable hit from a character, from the front, while not blocking with a shield in the left
  hand: on a roll `m_blocking` is set for that one `RPC_Damage`, so the game blocks it by all its own rules (not while
  attacking, dodging or staggered; never a parry, since the block timer is not running). "Reflex!" when it held.
- **Shield Bash (15%):** an ordinary (not parry) held shield block staggers a non-player attacker the game staggers on
  a parry (`m_staggerWhenBlocked`), with the game's `Stagger`. "Bash!".
- **Thorns (10%):** a held block of a melee hit sends that share of the blockable damage the block took off back as
  pierce damage from the blocker: unblockable, undodgeable, PvP rules apply, trains nothing.
- **Adrenaline (25%):** during a hit, adrenaline gains grow by it and the game's loss for an unblocked hit shrinks by it.
- **Shield wear (50%):** that share of the durability a block took from the blocker (shield or weapon) is given back.
- **Knockback (50%):** a blocked hit's push force shrinks by it, after the game's own reduction.
- **Desperation:** below 25% health the core damage reduction is doubled (it follows that perk, so nothing at level 0).
- All reductions multiply (`Reductions`), each capped at 90%, so nothing reaches immunity.

### Display

- **Status icons (`DefenseEffects`, `DefenseStatus`):** Riposte (seconds), Shield Wall, Hardened (stacks), Last Stand
  (seconds), Last Stand recovering (cooldown style) and Desperation, as status effects added to the local player while
  their state holds and gone when it ends; item icons (iron sword, wood shield, iron chest, drake helmet, blood bag).
  Status effects stay on the owner's client, so nothing is sent.
- **Skills panel page (`Skills/Pages/DefensePage`):** clicking Defense in the skills panel's info pane (see "## Skill
  book") shows every bonus at the current level, the guard perks and the milestones with the level each needs; hovering
  a milestone's name, or a gold word such as Poise or Reflex, shows what it does. It replaced, on 2026-09-28, a hover
  tooltip on the Defense entry (`SkillTooltips`, `DefenseSkillTip`, `DefenseSummary`), which itself had replaced the
  Defense plate in the inventory's stat column. PlateColumn stays merged for `EmbeddedSprite` (the skill icon) and the
  word tips (`LinkTips`).
- **Callouts:** "Riposte!", "Reflex!", "Bash!", "First block: ..." with the game's floating text, local, `Show
  Callouts` (each player's own).

### Settings

- **19 - Defense:** Defense Enabled, Show Callouts (each player's own), Max Health, Food Health, Damage
  Reduction, Regeneration At 100, Regeneration Interval, Out Of Combat Delay, Poise, Parry Window, Block Stamina
  Reduction, Dodge Stamina Reduction.
- **20 - Defense Experience:** Experience Multiplier, Block Experience, Parry Multiplier, Weapon Block Share, Hit Taken
  Experience, Hit Size Damage, Min Hit Size, Max Hit Size, Experience Cooldown, First Block Bonus, Player Hits Train.
- **21 - Defense Milestones:** each milestone's level and values.
- **22 - Defense Guard:** Reflex, Shield Bash, Thorns, Adrenaline, Shield Wear, Knockback, Desperation Health and
  Multiplier.

### Decisions made while building (2026-09-27), for the user to confirm

- **Dodge stamina kept as asked** (-10%, on top of the game's Dodge skill), and block stamina -10% added.
- **Damage reduction covers every source**, falls and poison included; Last Stand too.
- **Food health counts the cooking stars** (and anything another mod added to food health before it).
- **Regeneration is a share of max health**, so it keeps up late; resting and meads scale it.
- **PvP hits do not train** unless the server turns it on; dodged hits give nothing.
- **Shield Wall needs a shield** and works for any player behind the blocker, party or not.
- **Last Stand's cooldown and Hardened's stacks live in memory:** a relog resets them.
- **Riposte covers every melee hit of the one attack**, so a sweep that hits three creatures empowers all three.

### Known gaps

- Shield Wall judges "behind" by the blocker's facing only; a hit coming from behind the sheltered player is reduced
  too.
- The plate shows the core reduction (with Desperation); Hardened and Shield Wall show as icons instead.
- A reflex block that broke the guard shows no "Reflex!".

### Test checklist

- [ ] Skills panel: Defense with the helmet-and-shield icon; `raiseskill defense 100`, `resetskill defense`,
      `raiseskill all 10` includes it; survives relog; the death penalty lowers it. Sailing still listed and working.
- [ ] Level 100: no food 50 health; with food the food part +10%; stars and FeastMaster still compose.
- [ ] Damage: a known hit does 10% less; falls too; a second player sees the same health bar.
- [ ] Regeneration: nothing for 10 s after attacking, blocking or getting hurt; then about 1% every 10 s; faster
      rested.
- [ ] Poise: more hits before staggering; guard breaks later. Parry: a block raised about 0.3 s before the hit parries.
- [ ] Stamina: blocks and dodges cost about 10% less at 100; the inventory's equipment readout is unchanged.
- [ ] Experience: shield block ~1 per greydwarf hit, more on a troll; parry double; weapon half; hits taken half;
      "First block: Greydwarf" once, also after relog; nothing from falls, fire or poison ticks.
- [ ] Riposte: at 25, the Riposte icon after a parry; the next swing does 25% more to a greydwarf and says
      "Riposte!" without staggering it (a light weapon's first hit on a fresh greydwarf); the same on a dedicated
      server with the greydwarf owned by the server or by another player.
- [ ] Shield Wall with two clients: B behind A (blocking with a shield, Defense 50) takes 10% less and sees the icon;
      not in front of A, not when A blocks with a weapon.
- [ ] Hardened: stacks to 5x with unblocked hits, gone 8 s later. Last Stand: survive a killing blow at 1 health,
      2 s untouchable, cooldown icon counts 10 minutes, the next killing blow within it kills.
- [ ] Reflex, Bash, Thorns: callouts show; thorns damage numbers on the attacker; bash staggers.
- [ ] Skills panel: hovering Defense shows its description, then every bonus at the current level and the
      milestones; no Defense box in the inventory's stat column.
- [ ] Defense Enabled off: vanilla health, damage, stamina, parry; no icons, no bonuses in the tooltip, no experience.
- [ ] Dedicated server with two clients: all of the above at each player's own level; nothing in the server log.

### Skill book

### The request (user, 2026-09-28)

"Make the Skills larger, where it houses them. When you click a skill it shows that info next to the list of skills.
Then if you hover certain words, like in Defense Riposte, Shield Wall, Hardened, Last Stand, it will describe each of
those." Earlier the same day: hovering a skill should give very short and concise information on that skill, with
numbers for what it does for you, in the same box as the inventory's world tier, weight and armour tips.

### Design (`Skills/Book`, `Skills/Pages`)

- **Room (`BookLayout`):** once per skills window, the frame (458 x 657) grows by the pane's width plus a gap (420 +
  24) and by 140 in height; the list moves left by half the added width and grows by the added height, so it shows
  about four more skills. The frame stays centred; the title and Close button follow its edges.
- **Pane (`PaneBuilder`, `PaneScrollbar`, `BookPane`):** a dark box like the list's on its right, as tall as the list:
  the skill's icon, its name in the entries' gold font, a level line (level, a food or mead bonus in green, progress to
  the next level), a rule, then the page text in a scroll view with its own scrollbar (built new, not copied, so it
  carries none of the list bar's wiring). The shown skill's entry is tinted gold.
- **Choosing (`SkillBook`):** postfixes on `SkillsDialog.Setup` (every opening: empties the entries' own tooltips and
  shows the skill shown last, else the first), `SkillClicked` (the entry's Button) and `Update` (with a gamepad the page
  follows `m_selectionIndex`).
- **Pages (`SkillPage`, `SkillPages`, `PageText`):** a writer per skill fills a `SkillPage` for the local player at
  the moment it is shown: `About` (one sentence), `Line`s with the numbers at the player's level (bonuses included,
  as the game floors them), optional `Heading`s, and `Perk`s (name, level that unlocks it, hover text; a level above
  100 means turned off and it is not listed). `Line(text, term, tip)` explains one word of a line. Every game skill has
  a writer (the game's own effects, read from the game code, plus what this mod adds); a skill with no writer
  (another mod's) shows its description. A writer that throws is logged and its page keeps what it wrote.
- **Word tips (PlateColumn `LinkTips`):** perk names and explained words are TMP `<link>`s in gold; hovering one for
  0.3 s shows PlateColumn's tip box (the stat boxes' box: the game's item tooltip with the gold border) beside the
  word, until the pointer leaves it. Pointer events only, so no gamepad access to word tips.

### Decisions made while building (2026-09-28), for the user to confirm

- The entries' hover tooltips are gone: the pane shows the same and more on a click.
- 420 wide and 140 taller; the pane is as tall as the list.
- Level-gated perks say "at level N" or "unlocked"; perks that are always on have no label.
- The last skill shown is remembered for the session (not saved).

### Test checklist

- Open the skills panel: wider and taller, list on the left, pane on the right, the timber/brown background still
  covers the whole frame, Close button under both.
- Click several skills: icon, name, level line and page change; the clicked entry is tinted; a long page scrolls with
  the wheel and shows a scrollbar, a short one none.
- Hover a gold word (Riposte on Defense): the bordered box shows beside it after a moment and goes when the pointer
  leaves; it is not clipped by the pane.
- Close and reopen: the same skill's page shows. With a gamepad: moving the selection changes the page.
- A mead or food skill bonus shows in green on the level line.

## Status (2026-09-27)

Written in one session, after the shared `CustomSkills` registry was split out of Sailing's registration (Sailing,
Foraging and Husbandry use it too). Compiles clean; every Harmony target and parameter checked offline against the
game's assemblies. Released as 0.6.0 before any in-game test; the test checklist above is next.

## Fishing

### The request (user, 2026-09-27)

Ideas proposed from the game's fishing code, all accepted ("do it all"): a real fight on the line (line tension, strike
timing, tiring fish, grace at 0 stamina), bigger fish (big ones on the hook, legendary fish, bite rate, the angler's
senses), records and an angler's log, treasure (bonus items, snags), bait and tackle (bait saver, cast and line, chum),
conditions (time of day and weather), a link to Cooking (a fish's level improves its fillets' stars) and honest
experience (no experience for reeling an empty line).

### What the game's Fishing already does

Read from the decompiled assembly and the prefab bundles (UnityPy), 2026-09-27:

- **Skill 104, Fishing**, increase step 0.25. Only `FishingFloat` reads it: reel speed 2 to 6 m/s (halved while the
  fish thrashes); reeling costs the fish's pull times its level per second (Perch 3, Northern salmon 20, 3x while
  thrashing), a fifth of that at 100; holding a fish costs 1 stamina per second, 0.2 at 100. Nothing else: not bites,
  hooking, size, bait, drops or cast distance.
- **Experience:** one raise per second of reeling, two with a fish on. An empty line trains too and costs no stamina
  (the float prefab's pull cost is 0), so cast-and-reel is the game's fastest way to train.
- **Casting:** the rod throws a projectile (15 m/s, drawn like a bow); where it lands its spawn on hit becomes the
  float, and `FishingFloat.Setup` runs on the caster's client, which owns the float and runs its `FixedUpdate`. The line
  snaps when the float is 10 m past the line's length or 30 m from the rod.
- **Bites:** a fish that picks a new place to swim (on its owner) goes for each float in the water within 50 m with its
  base hook chance (10% for every fish), swims to it and nibbles (an RPC to the float's owner). Each fish takes one bait
  at 100%; the wrong bait says so and that fish ignores the float. Reeling within 0.5 s of a nibble sets the hook. The
  bait is used up on the hook and given back when a cast is reeled in empty.
- **The fight:** `Fish.OnHooked` claims the fish for the angler. `Fish.Escape` starts a thrash on the hook and then
  after a pause of 1.5 to 5 s (1.25 to 4 s for later fish): 0.5 to 3 s (1 to 4 s for later fish) plus 1.5 s per level
  (1 s for the Trollfish). Holding a fish drains stamina every
  step, so stamina never comes back during a fight, and at 0 the fish is lost.
- **Levels:** 1 to 5, rolled at spawn (SpawnSystem, 20% per step, 15% in the Deep North and Ashlands), stored in the
  item's quality: +40% size and +2 kg per level, pull times the level, longer thrashes, and fish above level 2 never
  jump (above 4 for Tuna and Coral cod). Cleaning (`Recipe_Fish1`, prep table, one fish of any kind) gives 1 raw fish plus 3 per level above 1, plus 1
  (Tuna, Giant herring, Grouper, Coral cod) or 2 (Anglerfish, Northern salmon, Magmafish, Pufferfish).
- **Bonus items:** every fish has an extra-drop table rolled into the inventory on 20% of catches, one item: Perch
  stone or amber, Pike flint or an amber pearl, Tuna tin ore or a ruby, Tetra obsidian or 1 to 15 coins, Trollfish troll
  hide or copper ore, Giant herring iron ore or chain, Grouper black metal scrap or barley, Coral cod chitin or onion
  seeds, Anglerfish soft tissue or blue jute, Northern salmon carrot seeds or silver, Magmafish flametal ore, a Surtling
  core or grausten, Pufferfish sap or ooze.
- **Also:** the fishing hat gives +20 Fishing (+20 Swim); fish stack by ten; item stands take fish; the game's stats
  count catches per level up to 6. Baits are crafted at the prep table, a kitchen, so they already rolled Cooking stars
  before this module.

### Foundation (`Fishing/Core`)

- **The angler (`Angler`, `FloatSetup`):** the `Setup` postfix, on the caster's client, stamps the angler's level and
  the bait's stars on the float's ZDO (`grindstone_angler_level`, `grindstone_bait_stars`) for the fish owners, adds the
  cast's state (`FloatFight`, a component on the float, only on the angler's client) and lengthens the line.
- **The float's step (`FloatScope`):** a prefix and finalizer on `FishingFloat.FixedUpdate` for the local player's
  floats. Before the game's step: the senses when the float lands, snags without a fish, and with a fish the grace or
  line tension, either of which can skip the game's step; reel speed changes for a spent fish or a snag, put back by the
  finalizer, which also lands a snag. A prefix on `Skills.RaiseSkill` scales the game's reeling raise inside the step;
  credits inside the step go through `RaiseUnscoped`.
- **The catch (`CatchHook`, `CatchInfo`):** `FishingFloat.Catch`'s prefix captures the fish before the pickup destroys it
  and raises its bonus-item odds for the one roll; the postfix credits experience, writes the log and records, may give
  the bait back, announces a legendary fish and adds the weight to the game's message; the finalizer restores the table.
- **Callouts (`FishCallout`):** local text, text for everyone within 40 m, and a top-left announcement for the server.
- **Icon:** the user's art (2026-09-27), trimmed, centred on a square and scaled to 64x64 as
  `assets/skill_fishing.png`, is Fishing's entry in `Skills/GameSkillIcons` (shared with Cooking and Farming), which
  puts it on the game's Fishing skill definition when a player's Skills wake. The skills panel and the level-up message
  show it; a machine without graphics skips it.

### Features (defaults, synced and lockable)

| Feature | Default | Whose level, where it runs |
| --- | --- | --- |
| Line tension | builds 60% of the line per second at 0 (30% at 100) while reeling a thrashing fish, eases 50%/s (10%/s while reeling a calm fish), snaps at 100% | the angler's, angler's client |
| Strike window | 0.5 s at 0 to 1 s at 100; a perfect strike (reel within 0.2 s) skips the hook's thrash | the angler's, angler's client |
| Tiring | each thrash 15% shorter per thrash before it (never below 20%); spent after 4 thrashes (legendary 8): no more, reel +50% | angler's client (it owns the fish) |
| Grace (level 75) | 4 s at 0 stamina, once per fish; ends early at 25% stamina | the angler's |
| Bite chance | +100% at 100; +50% at the height of dawn and dusk; +25% in rain; +20% per bait star; +100% near chum | the float's angler (from its ZDO), the fish's owner |
| Big one | 25% at 100, per level, x1.5 at night, +5 points per bait star; up to level 5 | the angler's, angler's client |
| Legendary fish | 0.5% of spawned fish; only anglers from level 50 hook them; announced | the spawner; the fish's owner |
| Senses | species 25, size 50, water 75 | the angler's |
| Bonus item | the fish's own 20% at 0 to 40% at 100; two items from 50; legendary always | the angler's |
| Bait saver | 30% at 100 | the angler's |
| Snags | 2% at 0 to 6% at 100, once per cast after 8 s in the water | the angler's |
| Cast and line | +30% distance, +50% line at 100 | the angler's |
| Fillets | +10 effective Cooking levels per level of the fish above 1 | the crafter's client |

- **Line tension (`Fight/Tension`, `Fight/TensionBar`):** reeling while the fish thrashes (`Fish.IsEscaping`) builds it,
  from 0.75 s after the hook (the hook starts a thrash while the angler still reels); at 1 the game's own line break
  runs (message, fish released, float gone). A bar under the crosshair under the HUD root shows the tension and the
  state ("Thrashing! Ease off", "Spent! Reel it in", the grace's countdown).
- **Strikes (`Fight/Strike`):** `TryToHook` is replaced by the same code with the angler's window. A perfect strike
  needs the angler reeling, not already reeling when the fish nibbled, within the window. `RPC_Nibble`'s postfix
  records whether the angler was reeling and runs the nibble sense; a snagged hook takes no nibbles.
- **Tiring (`Fight/Tiring`):** a postfix on `Fish.Escape` on the fish's owner; a cancelled thrash also sets the game's
  next pause. The hook's own thrash counts as the first.
- **Grace (`Fight/Grace`):** the game's step is skipped (so nothing drains and stamina comes back after the game's 1 s
  pause) while the fish takes 1.5 m of line per second, up to the line's length.
- **Bites (`Bites/BiteChance`, `FishingConditions`, `Chum`):** `Fish.FindFloat` is replaced by the same look with the
  chance multiplied. Day time and weather are the same on every machine. Chum is any item of Chum Items floating in the
  water (listed once a second from the game's list of dropped items); its owner stamps when it started floating
  (`grindstone_chum_since`) and removes it after Chum Duration, checked in `ItemDrop.SlowUpdate` (every 10 s).
- **Senses (`Bites/Sense`):** the nibble message (centre), the conditions and the reach readout when the cast lands
  (top left), and a fish's level in its hover in the water from Size Sense Level. Legendary fish read "Legendary" to
  everyone.
- **Tackle (`Bites/Tackle`):** the rod attack's launch speeds times the square root of 1 + the share (a throw's range
  grows with the square of its speed), put back afterwards; the float's `m_maxDistance` when it lands.
- **Starred bait (`Bites/StarredBait`):** `ReturnBait` gives starred bait back with its stars; the bait saver uses the
  same.
- **Big ones (`BigFish/BigOne`):** after the hook, on the angler's client which now owns the fish: `SetQuality` and the
  item data saved to the fish's ZDO. `FishMark` (every client with a screen) reloads hooked fish it does not own twice a
  second, so the growth shows everywhere, and every fish once 2 s after it loads.
- **Legendary fish (`BigFish/LegendarySpawn`, `LegendaryItems`, `LegendaryGlow`, `LegendaryCatch`):** a scope around
  `SpawnSystem.Spawn` notes the fish it instantiates (`Fish.Awake`) and makes each legendary with Legendary Chance (level
  6: three times the size). Dropped fish are never rolled. Every fish item's `m_maxQuality` is raised to 6 when the item
  database wakes, so recipes count a legendary fish (the game counts ingredients only up to an item's maximum). The
  glow is a local light and halo on every client with a screen.
- **Records (`Records/`):** the weight is the game's weight for the level ±15%, in messages, the log and records only
  (fish stack by ten, so per-fish data on items would be lost). The log (`grindstone_fish_log`: levels per species) and
  records (`grindstone_fish_records`) are in player custom data. The fish tooltip, and `fishlog` (not a cheat, so
  `/fishlog` works in chat) show them.
- **Snags (`Rewards/Snags`, `SnagFile`, `SnagModel`, `SnagLoot`):** `GrindstoneSkills.Snags*.yml` per biome, read like the
  finds files. A snagged line reels at half speed, costs 6 stamina per second of reeling and drags the float down. It
  lands when the game's step destroys the empty float at 0.5 m of line; the find goes into the inventory (at the
  angler's feet when full). No experience.
- **Fillets (`Rewards/Fillets`):** `CraftStars.Roll` adds the levels of the biggest fish the craft used up
  (`CraftRecord` notes it, with its quality).

### Experience (section 50, synced)

- **Reeling:** the game's raise, times Empty Reel Experience (0; the game's is 100) or Fight Experience (100).
- **Catch:** Catch Experience (10) x the species (the square root of its pull over a Perch's: Pike 1.3, Northern salmon
  2.6) x 1 + Size Experience Bonus (50%) per level above 1.
- **Discovery:** Discovery Experience (30) x the species for a new species, New Size Experience (10) x the species for a
  new level of a known one.
- An Experience Multiplier over all of it. The game gives 0.25 of a point per raise: a 10 s Perch fight is about 20
  raises plus 10 for the catch, about 7.5 points, against the game's 5 plus whatever empty reeling added.

### Settings

- **50 - Fishing:** Fishing Enabled, Show Callouts (each player's own), Experience Multiplier, Empty Reel Experience,
  Fight Experience, Catch Experience, Size Experience Bonus, Discovery Experience, New Size Experience.
- **51 - Fishing Fight:** Line Tension, Tension Build At 0, Tension Build At 100, Tension Ease, Strike Window At 0,
  Strike Window At 100, Perfect Strike Window, Tiring Per Thrash, Thrashes To Tire, Spent Reel Speed, Grace Level,
  Grace Seconds.
- **52 - Bites:** Bite Chance At 100, Dawn And Dusk Bite Bonus, Rain Bite Bonus, Bait Bite Bonus Per Star, Chum Items,
  Chum Bite Bonus, Chum Radius, Chum Duration, Species Sense Level, Size Sense Level, Water Sense Level.
- **53 - Big Fish:** Big One Chance At 100, Night Big One Bonus, Bait Big One Bonus Per Star, Legendary Chance,
  Legendary Level, Legendary Thrashes, Announce Legendary Catches.
- **54 - Catch And Tackle:** Bonus Item Chance At 100, Double Bonus Level, Bait Saver At 100, Fillet Levels Per Fish
  Level, Snag Chance At 0, Snag Chance At 100, Snag Wait, Cast Distance At 100, Line Length At 100.

### Decisions made while building (2026-09-27), for the user to confirm

- **Sections 50 to 54:** the other modules written at the same time were sharing out 19 to 49.
- **Tension** builds only while reeling a thrashing fish, and not in the first 0.75 s after the hook.
- **Perfect strike:** reeling, started within 0.2 s of the nibble, and not already reeling when it nibbled; the game's
  hook from a fast-moving float is never perfect.
- **Tiring:** the hook's thrash counts as the first; a spent fish never thrashes again.
- **Grace:** once per fish; the game's step is skipped during it, except when the angler attacks or draws a bow (the
  game's step then lets the fish go and removes the float, as always).
- **The Fishing icon** shows whatever Fishing Enabled says, as Cooking's and Farming's do: it is the mod's look, not
  gameplay.
- **Fishing Enabled off** turns every feature off, except that fish items keep a maximum quality of 6, so a legendary
  fish caught earlier can still be cleaned.
- **A catch counts only when the fish was taken:** the game lets a fish go when the inventory has no room for it, and
  then no experience, log entry, record, bait saver or announcement follows.
- **Big ones** grow on the hook, not at spawn, up to level 5, and change the fish's real level: size, pull and fillets.
- **Legendary fish** are rolled only for fish the spawn system makes; the level 50 gate uses the angler's level at the
  cast (a fishing hat put on afterwards counts from the next cast); the glow is sea-green.
- **Starred bait:** +20% bites and +5 big-one points per star; it comes back with its stars.
- **Weights and records are the character's own;** there are no server-wide records.
- **The angler's log** is shown on fish tooltips and by `fishlog`; there is no panel.
- **Snags** give no experience and block nibbles; one roll per cast.
- **Chum** is the game's entrails and blood bags, no new item; one dropped stack is one piece of chum.
- **Fillets:** the biggest fish among the used-up items counts, for any kitchen recipe (only cleaning uses fish today).
- **Item stands** take fish as in the game; nothing is shown there.

### Known gaps

- Other clients see a big one grow up to half a second late (`FishMark` polls), and a fish whose data reached them after
  it loaded is fixed 2 s later.
- A snag is landed when the float is destroyed at 0.5 m of line or less; attacking at that very moment lands it too.
- Credits reach the angler as one raise; the game gives at most one level per raise.
- The tension bar's position, size and colours, the glow's light and halo, and every message are untested. All texts
  are English.
- With every bite bonus stacked the chance per fish caps at 100%.

### Test checklist

- [ ] Fishing Enabled off: reeling, bites, hooking, experience and messages exactly vanilla; no bar, no glow.
- [ ] Icon: the skills panel and the "Fishing increased" message show the new fish-and-rod icon, sharp at 64x64.
- [ ] Tension at level 0: keep reeling through a Perch's thrash and the bar fills red and the line snaps in about two
      seconds; easing off during thrashes lands it; Line Tension off never snaps.
- [ ] Strike: at level 100 the hook sets up to 1 s after the nibble; reeling right on the nibble floats "Perfect
      strike!" and the fish does not thrash at once; holding block when it nibbles is never perfect.
- [ ] Tiring: each thrash shorter; "Spent!" after the fourth and the line comes in faster; eight for a legendary.
- [ ] Grace at 75: out of stamina, "It takes line - catch your breath!", the countdown, stamina returns and the fight
      goes on; out of stamina again, the fish is lost; below 75 lost at once.
- [ ] Bites: more at 100 than at 0, at dusk, in rain, near floating entrails; the entrails vanish after 60 to 70 s.
- [ ] Big one: with Big One Chance At 100 at 100 a hooked fish grows, "It's a big one!", and a second client sees it
      grow; level 5 fish never grow.
- [ ] Legendary: with Legendary Chance 100 new fish spawn legendary, three times the size, glowing on both clients; a
      level 0 float is ignored; landing one announces it to both; cleaning it gives at least 16 raw fish; the tooltip
      says Legendary.
- [ ] Senses at 25, 50 and 75: the nibble messages, the conditions line, the reach line and "Something big lurks".
- [ ] Log: the first Perch says "New in your angler's log: Perch (1 of 12 species)" and credits; a new size says so; a
      heavier catch says "new record!"; tooltips; `/fishlog` in chat; all of it after relog.
- [ ] Snags: with Snag Chance At 0 at 100, 8 s in the water gives "Snagged something heavy!", a slow reel, and the
      biome's items; no nibbles meanwhile; sea casts use the Ocean table; the YAML hot reloads.
- [ ] Bonus items and bait saver at 100: more bonus items, sometimes two; "Bait saved"; starred bait comes back
      starred, after an empty reel too.
- [ ] Tackle: casts go further and the line snaps later at 100.
- [ ] Fillets: at Cooking 0, cleaning level 5 fish gives better stars than cleaning level 1 fish.
- [ ] Experience: an empty reel gives none; a catch and discoveries credit; Empty Reel 100, Fight 100 and the three
      credits at 0 give the game's own.
- [ ] **Dedicated server with two clients** (A fishes, B stands by, the fish owned by B or the server): A's level and
      bait still decide bites; legendary fish spawn wherever spawning runs and glow for both; growth, callouts and
      announcements reach B; nothing in the server log.

### Status (2026-09-27)

Written 2026-09-27 in the same tree as the Defense, Husbandry and Farming modules, while 0.5.0 to 0.8.2 were
released without it. Released as 0.9.0 to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.9.0), before any in-game
test. A review against the decompiled game fixed a calmed fish
that kept swimming (the game's rest is an escape time below 0), the grace skipping the game's cleanup when the angler
attacks, catches counted when the inventory had no room, snag loot doubled at a nearly full inventory, the snag biome
read at the rod, a long strike window stealing a fish hooked elsewhere, and starred bait lost to a full inventory.
The build is clean (0 warnings) and the offline patch check passes (225 patch classes). The Fishing decisions above
wait for the user's confirmation.

## Husbandry

### The request (user, 2026-09-27)

A Husbandry skill: the higher the level, the more yield from tamed creatures; from some level, creatures that are being
tamed no longer fear or attack you; a chance for offspring to be a higher level than their parents. The user then
asked for every idea from the planning discussion to be built, with the best decision for anything vague: faster
taming, longer fed time, faster breeding, bigger herds, twins, faster growing up, animal lore, produce from living
animals, petting and contentment, pack leader, an animal feeder, optional taming level gates, extra honey, starred eggs
for Cooking, prime cuts, and the hand-off to Elite Creatures Reborn's stars. The skill icon is the user's own art
(`assets/skill_husbandry.png`, 64x64, from `Desktop/valheim_icons/husbendry_icon.png`).

### What the game already does

Read from the decompiled assembly and the prefab bundles (UnityPy), 2026-09-27:

- **No such skill.** `Skills.SkillType` has Ride (110, "Riding": mount speed and stamina) but nothing for animals.
- **Tameable creatures:** Boar, Wolf (the only commandable one: follow/stay), Lox, Asksvin, Moose (tameable, saddle,
  breeds, calves), Hen (always tame, lays eggs), and the Mysterious Rock joke. The spirit-caller summons and friendly
  skeletons start tamed and do not breed.
- **Taming (`Tameable.TamingUpdate`, on the owner, every 3 s):** while fed (`m_fedDuration` 600 s after a meal) and not
  alerted, 3 s come off `m_tamingTime` (1800 s), doubled per nearby player (60 m) with the taming boost. "Frightened"
  in the hover is `MonsterAI.IsAlerted`, and it stops taming.
- **Fear and aggression:** Boar and Hen flee their target until alerted (`m_fleeIfNotAlerted`) and fear fire; Wolf,
  Lox, Asksvin and Moose attack. Every creature picks targets through `BaseAI.IsEnemy(a, b)`.
- **Breeding (`Procreation.Procreate`, on the owner, every 30 s):** a fed, calm, tamed animal with a partner within
  3 m (8 m lox) gains a love point on 67% of checks (`m_pregnancyChance` 0.33 is the chance to skip); 3 points (4 lox)
  make it pregnant; it gives birth after `m_pregnancyDuration` (60 s, 120 s lox). No breeding while `m_maxCreatures`
  (4 wolf and lox, 5 boar and moose, 10 hen and asksvin) of the kind, young included, stand within 10 m (20 m lox).
  **The newborn takes exactly the pregnant parent's level** (the partner's is ignored), so a line never gains stars.
  Hens and asksvin lay an egg instead, its quality set to the hen's level.
- **Growing up:** eggs (`EggGrow`) hatch after 1800 s warm (fire near, roof for chicken eggs) at their quality's level;
  young (`Growup`: piglet, cub, calf, chick, hatchling) grow up after 3000 s (6000 s lox calf), keeping their level.
- **Drops double per level:** `CharacterDrop` multiplies level-scaled drops by 2^(level-1).
- **Elite Creatures Reborn** keeps the game level at 1, stores stars in its own traits, and rolls a newborn's stars
  evenly from 0 to the stronger parent's; an egg's quality is its stars + 1, the same convention as Cooking.

### The skill: our own

- Registered through `Skills/CustomSkills` (the Defense session's generic registration): identity
  `grindstone_husbandry`, level published on the player's ZDO as `grindstone_husbandry_level` once a second,
  `raiseskill husbandry 50` and `resetskill husbandry` with devcommands, "all" included. Removing the mod loses the
  level at the next save, as for every mod skill.
- **Whose level counts (`Keeper`):** creature code runs on the creature's owner, often not the keeper's client. The
  owner reads the best published Husbandry level among living players within Keeper Range (30 m): fed time, breeding,
  twins, better offspring, growth, produce. Taming speed uses the game's own taming range (60 m). Butchering uses the
  killer's level, extra honey the harvester's, pack leader the followed player's, lore and the feeder gate the local
  player's.

### Taming

- **Taming speed:** a prefix on `Tameable.DecreaseRemainingTime` (the 3 s step) multiplies the step by 1 + the Taming
  Speed share (100% at 100: half the time), stacking with the game's boost.
- **Taming Levels (off by default):** "Lox:30, Asksvin:50, Moose:60" makes a creature need a keeper of that level
  within the taming range; otherwise the step is 0. The hover says so to everyone.
- **Fed longer:** a postfix on `Tameable.OnConsumedItem` moves the feeding stamp into the future by m_fedDuration x the
  Fed Duration share (100% at 100: 20 min instead of 10). Every reader of `IsHungry` (taming, breeding, healing, the
  hover) agrees without patches of its own.
- **Calm (level 50):** a postfix on the static `BaseAI.IsEnemy(a, b)` answers "not an enemy" when `a` is a creature
  being tamed (tameness above 0%) and `b` a player at Calm Level. The creature never targets that player, so it neither
  attacks nor flees from them and is not alerted by them, and taming goes on beside them. Only that direction changes:
  the player can still hit it. Hurting it (`MonsterAI.OnDamaged`, on the owner) writes the player and the time to the
  creature's ZDO, and it defends itself against that player for Calm Break Time (120 s). Other players still scare it.

### Breeding (`Husbandry/Breeding`)

Everything runs on the parent's (or young animal's, or egg's) ZDO owner, at the best keeper's level within Keeper Range.

- **One breeding check (`BreedingCheck`):** a prefix on `Procreation.Procreate` with `Priority.First`, so Elite
  Creatures Reborn's own prefix (which decides "birth now" with `IsDue`) sees the same shortened pregnancy and the
  star-up key; a postfix books the birth; a finalizer puts the game's values back and closes the birth scope even if
  the game's code threw. State travels in a `BreedingCall` (`__state`).
- **Pace (`BreedingPace`):** for the one check the pregnancy duration and the skip chance are divided by
  f = 1 + Breeding Speed's share + Content Breeding Bonus while the animal is content (never while Content Duration is
  0), and Herd Size's share of the level, rounded half up, is added to the crowding limit. `SpeedFactor`,
  `ExtraRoom` and `IsContent` are shared with the lore and petting.
- **Births (`BirthRolls`):** a check is a birth when the game's own `IsPregnant` and `IsDue` say so after pacing.
  Twins and Better offspring roll first; after the game's code, only if it really ended the pregnancy, the parent's
  birth counter (`grindstone_births`) goes up, a twin's mark is cleared, and "Strong offspring!" / "Twins!" float
  above the parent (`HerdCallout`, drawn by clients near it).
- **Better offspring (`OffspringStar`, `OffspringLevel`):**
  - Without ECR, a birth scope raises the value the game passes on (`Max(m_minOffspringLevel, parent level)`) in
    `Character.SetLevel` (a newborn) or `ItemDrop.SetQuality` (a laid egg, whose quality becomes the chick's level) to
    that level + 1, never above Max Offspring Level (3 = two stars). Only a value equal to the passed-on level is raised,
    so the egg's own Awake call cannot compound it; a parent at the cap gets no star and no callout.
  - With ECR (`Chainloader.PluginInfos` has `gglasgow.elitecreaturesreborn`), the parent carries `grindstone_star_up`
    = 1 for the birth. ECR's `Breeding/KeeperBonus.cs` (added for this) gives the newborn one star more than its
    inheritance roll, up to one above the stronger parent, and writes 0 when that cap leaves no room, so the callout
    only shows for a real star. The finalizer sets the key back to 0. Max Offspring Level does not apply there.
- **Twins (`TwinPregnancy`):** after the birth the parent is made pregnant again through the game's own
  `MakePregnant` (ECR patches it to remember the partner), then `s_pregnant` is moved back past the game's own duration
  and the pregnancy is marked a twin's (`grindstone_twin`). The next check, 30 s later, gives the second birth or egg
  (a pregnant animal gives birth whether hungry, alerted or crowded). A twin never has twins; it rolls Better offspring
  on its own and counts as a birth.
- **Growing up (`GrowingUp`):** `Growup.GrowUpdate` (tamed young only) and `EggGrow.GrowUpdate` (every egg) divide
  m_growTime by 1 + Growth Speed's share for the one call. The game grows by age, so a young left alone catches up as
  soon as a keeper returns.

### Animal yield (`Husbandry/Yield`)

- **The death scope (`Butchering`):** tamed animals usually die through a ragdoll: inside `Character.OnDeath`,
  `Ragdoll.Setup` generates the drop list, stores it on the ragdoll's ZDO and turns the creature's own drops off; the
  ragdoll spawns the loot seconds later (`Ragdoll.SpawnLoot`, on its owner then). So the scope wraps
  `Character.OnDeath` on the creature's owner, which covers both paths. The killer is the attacker of the game's last
  hit (`m_lastHit`, recorded on the owner for every hit on a living creature). A player killer gets Butchering
  experience through `HusbandryCredit` (routed RPC, only the killer's client acts).
- **Butcher yield (`ButcherYield`):** a `CharacterDrop.GenerateDropList` postfix inside the scope with a player killer
  multiplies every non-trophy entry by 1 + the killer's share; the fraction is a chance of one more; the game's cap of
  100 per entry holds. Each list is scaled once (the ragdoll turns the creature's own drop path off).
- **Prime Cuts (`PrimeCutDrops`, off by default):** meat is discovered (`YieldCatalog`): kitchen cooking-station inputs
  that some Tameable prefab drops (raw, wolf, lox, chicken, moose meat). While Prime Cuts is on, every machine makes that
  meat a star item (`YieldStarItems`, `Kitchen.AddItem`). Meat from a starred tamed animal gets stars = level - 1, at
  most 3, when it spawns: in the death scope directly, or through `grindstone_prime_stars` on the ragdoll's ZDO, read
  back in `Ragdoll.SpawnLoot` (an `ItemDrop.Awake` postfix sets the quality and saves). The killer does not matter.
- **Produce (`Produce`, `ProduceTables`):** a `Tameable.TamingUpdate` postfix (every 3 s) on the owner of a fed tamed
  animal keeps the world time of its last roll (`grindstone_produce_last`). The first tick starts the clock; once
  Produce Interval has passed and a keeper is in range, it rolls the keeper's share of Produce Chance and drops one item
  beside the animal, with the world's resource rate. The table is the creature's own drops minus trophies, food and
  cooking inputs, weighted by drop chance: hens feathers, boars leather scraps, wolves pelts and fangs, lox pelts,
  moose hides and sinew. A due roll waits for a keeper (an area loads 60 m out, beyond Keeper Range) and never stacks.
- **Extra honey (`ExtraHoney`):** on the harvester's client, a prefix on `Beehive.Extract` (which the game calls only
  after its own ward and honey checks) records the honey count; after `Beehive.Interact`, Husbandry rises by Honey
  Experience per honey and each honey rolls the player's share for one more, spawned at the hive as a networked item.
  OpenKeep's hive settings only change how much honey there is, so they add up.
- **Starred eggs (`YieldStarItems`):** every item with `EggGrow` becomes a star item while Husbandry is on (the game
  already stores the laying hen's level in the egg's quality): eggs from starred hens show their star, stack apart,
  count in recipes (the game's own recipe count ignores quality-2 eggs) and raise a dish's odds.

### Companions (`Husbandry/Companions`)

- **Pack leader (`PackLeader`):** a tamed creature following a player (wolves: `MonsterAI.GetFollowTarget`, kept only
  on the creature's owner) is stronger by the followed player's level. A `Character.Damage` prefix on the attacker's
  owner multiplies its hit by 1 + Pack Damage's share before the RPC goes; a postfix hands the caller its HitData back,
  so a hit reused for several targets is never scaled twice. A `Character.RPC_Damage` prefix on the victim's owner
  multiplies a follower's incoming hit by 1 - Pack Toughness's share (at most 90%). Blood Magic summons (anything with
  `m_levelUpOwnerSkill`, an unsummon distance or logout timer) are left out.
- **Petting (`Petting`):** a use on a tamed animal the game does not let you command pets it (`Tameable.Interact`
  stamps m_lastPetTime). On the petter's client: if it was not content yet, Petting Experience x tier; every pet sends
  `grindstone_Pet` to the owner, who writes `grindstone_content_until` = now + Content Duration. Breeding and lore read
  it. Content Duration 0: no contentment, no petting experience.
- **Animal Feeder (`FeederPrefab`, `FeederRecipe`, `FeederGate`, `Feeders`, `FeederEating`):**
  - A copy of the game's barrel (`piece_chest_barrel`) named Animal Feeder (`grindstone_feeder`): a 4 x 2 container
    in the hammer's Misc tab, at a workbench, costing Feeder Recipe (Wood 10, Leather scraps 4; unknown items skipped,
    fallback Wood 10; follows the setting live; everything returned when taken down). Made once per process on every
    machine under an inactive DontDestroyOnLoad holder (no Awake, no ZDO), registered in `ZNetScene` after every
    `ZNetScene.Awake`, added to the Hammer once the item database exists, whether Husbandry is on or not, so built
    feeders always load.
  - Building needs Feeder Level (25): `Player.HaveRequirements(Piece, mode)` says no for the feeder below it (the game
    neither learns nor places it), and a `PieceTable.UpdateAvailable` postfix takes it out of the menu for a player who
    learned it and then fell below. Built feeders work for everyone.
  - Eating, on the creature's owner: when the game's `MonsterAI.UpdateConsumeItem` found no food on the ground at its
    search moment, a hungry animal that is tamed or being tamed picks the nearest loaded feeder within Feeder Range
    holding something it eats (its own food order) with a path to the feeder's near side, walks there (returning true
    like the game), turns and eats one: the feeder's owner removes the item (directly or by `grindstone_FeederTake`),
    `m_onConsumedItem` runs with that food's prefab (so fed time, Fed Duration and feeding experience follow), and the
    consume effect and animation play. It gives up after 30 s or when the feeder empties. Loaded feeders are a list
    filled in `Container.Awake`; non-owners read a feeder's inventory from its ZDO.

### Animal lore (level 20)

The crosshair text of an animal or egg gets timer lines, rebuilt at most every 0.25 s like the vein hover:
"Tamed in about 12 min while fed and calm", "Fed for 14 min", "Content for 7 min", "Love 2 of 3" or "Pregnant, due in
40 s", "Herd 5 of 8" or "Herd full (8 of 8 within 10 m)", a young animal's "Grows up in 20 min" (young have no
Tameable, so the game shows nothing for them; lore adds the name) and a warm egg's "Hatches in 12 min". The timers use
the best keeper near the animal now.

### Experience (section 28)

- **Watched, not sent (`HerdWatch`):** every 2 s each keeper's client snapshots the tameable creatures within Keeper
  Range from their replicated ZDOs and credits what changed since the last look: taming progress (20 per whole taming),
  becoming tame (10, x3 for the character's first of each kind, recorded in `grindstone_tamed`), eating (1), births (3,
  from the parent's `grindstone_births` counter, so twins and eggs count). A creature seen for the first time earns
  nothing. Everyone near earns.
- **Credited:** butchering a tamed animal (5, routed RPC from the creature's owner to the killer), petting an animal
  that is not content yet (1), each honey harvested (0.5, no tier).
- **Tier:** every amount but honey is multiplied by 1 + half a step per doubling of the creature's health over 10,
  1 to 5: boar and hen 1, wolf 2.5, asksvin 4.2, lox and moose 4.3.

### Settings

- **23 - Husbandry:** Husbandry Enabled, Keeper Range, Animal Lore Level (synced); Show Callouts (each player's own).
- **24 - Taming:** Taming Speed At 100, Fed Duration At 100, Taming Levels, Calm Level, Calm Break Time.
- **25 - Breeding:** Breeding Speed At 100, Herd Size At 100, Growth Speed At 100, Better Offspring At 100, Max Offspring
  Level, Twins At 100, Content Duration, Content Breeding Bonus.
- **26 - Animal Yield:** Butcher Yield At 100, Prime Cuts, Produce Chance At 100, Produce Interval, Extra Honey At 100.
- **27 - Companions:** Pack Damage At 100, Pack Toughness At 100, Feeder Level, Feeder Range, Feeder Recipe.
- **28 - Husbandry Experience:** Experience Multiplier, Taming, Tamed, Discovery Multiplier, Feeding, Birth, Petting,
  Butchering and Honey Experience.

### Decisions made while building (2026-09-27), for the user to confirm

The user asked for every idea with the best decision for anything vague. These are the calls made:

- **The keeper is the best level within 30 m**, not the tamer or the last feeder: animals only simulate while someone
  is near, and it needs nothing stored per animal. A visiting friend's higher level helps your farm.
- **Calm needs taming under way** (tameness above 0%), is per player, and only stops the creature treating you as an
  enemy: you can still hit it, and then it defends itself against you for 120 s. Calm Level 50.
- **Taming Levels is off by default**; the example in its description is Lox 30, Asksvin 50, Moose 60.
- **Better offspring is one star over the parent, 25% at level 100, up to two stars** (the game's own maximum and the
  last one it draws); drops double per level, so higher would multiply meat eightfold. Under ECR: one star over ECR's
  roll, up to one above the stronger parent.
- **Twins: 25% at level 100**, a second birth 30 s later, never chained.
- **Yield means four things:** butchering (killer's level, +50%, never trophies), produce from the living animal
  (feathers, scraps, pelts, hides: 50% per 20 minutes at 100), extra honey (beekeeping is husbandry too: +50% at 100)
  and more animals (twins, herd size, better offspring).
- **Prime Cuts is off by default:** raw meat becoming a star item changes Cooking's ingredient average even at 0
  stars, and splits meat stacks by star. Starred eggs are on, because the game already gives eggs a quality.
- **Petting refreshes contentment** on every pet but pays experience only when the animal was not content; contentment
  adds +50% breeding speed for 10 minutes.
- **Pack leader** covers followers only (wolves), not summons; its toughness also softens the leader's own hits.
- **The feeder is a barrel copy** with the barrel's look and icon, 4 x 2, Wood 10 and Leather scraps 4 at a workbench,
  buildable from level 25; animals eat ground food first; tamed animals and animals being tamed use it.
- **Experience is watched, not sent:** everyone within Keeper Range earns from taming, tames, meals and births; the
  creature's tier (from its health) scales it; the first tame of each kind is worth triple.
- **Lore from level 20**, one level for every line. The Taming Levels hint shows to everyone.

### Known gaps

- Elite Creatures Reborn keeps the game level at 1, so Prime Cuts gives no stars there, and Better offspring needs
  Elite Creatures Reborn 3.10.0 or later (`KeeperBonus`). Eggs above 3 stars under ECR show 3 in GrindstoneSkills' icons.
- Prime Cuts meat and eggs stay star items until a restart after their switch is turned off.
- Two animals eating a feeder's last item at once may both be fed; a feeder changing owner while the take RPC is in
  flight keeps its item.
- A pet whose contentment takes more than a second to come back from the owner may pay experience twice.
- Calm remembers only the last player who hurt the creature.
- Killing a tamed summon with the butcher knife gives Butchering experience (it has no drops).
- Lore timers are estimates for the keeper near now; the game's taming boost is not in the taming estimate.
- Credits reach the player as one raise; the game gives at most one level per raise.
- The skill is the mod's own: removing GrindstoneSkills loses the level at the next save.

### Test checklist

- [ ] Skills panel: Husbandry with the fence-and-animals icon and its description; survives relog; `raiseskill
      husbandry 50`, `resetskill husbandry`, `raiseskill all 10` include it; the death penalty lowers it.
- [ ] Husbandry Enabled off: taming, breeding, drops, produce, honey, wolves and hovers exactly vanilla; no feeder in
      the hammer; built feeders still load.
- [ ] Taming speed at 100: a boar tames in about 15 minutes instead of 30 while you stay within 60 m; with the taming
      boost as well, faster still. Taming Levels "Boar:50" at level 40: no progress, and the hover says why.
- [ ] Calm at 50: a wild boar at 10% tameness no longer flees from you and keeps taming while you stand beside it; a
      lox no longer attacks you; a second player below 50 still spooks it; hitting it makes it fight you for 120 s.
- [ ] Fed longer at 100: after a meal the hover counts 20 minutes, and the animal stays calm and breeds that long.
- [ ] Breeding at 100: pregnancy about 30 s instead of 60; a petted animal faster still; boars keep breeding up to 4
      over the crowding limit. Better offspring 100: a 0-star piglet grows into a 1-star boar, "Strong offspring!"; a
      2-star parent gives no star. Twins 100: "Twins!", a second piglet or egg 30 s later, never a third.
- [ ] With ECR: the newborn gets ECR's traits plus a star; no callout when it is already one above the stronger parent.
- [ ] Growth at 100: a tamed piglet grows up and a warm egg hatches in half the time; a wild calf does not.
- [ ] Butchering at 100: a tamed boar killed with the butcher knife drops about 1.5x meat and scraps, never two trophies;
      the killer gets experience, also on a dedicated server; a wolf's or a fall's kill gives plain drops.
- [ ] Prime Cuts on: a two-star boar's meat has 1 star, also when you walk away before the body vanishes; off: none.
- [ ] Produce (interval 60, chance 100, level 100): a fed hen drops feathers about once a minute; not when hungry; not
      with no keeper near; a second client sees the item.
- [ ] Extra honey at 100 (chance 100): a full hive gives twice the honey and 0.5 experience per honey; none in someone
      else's ward.
- [ ] Starred eggs: a one-star hen's egg shows 1 star, stacks apart, counts in a recipe and hatches a one-star chick.
- [ ] Pack leader at 100: a following wolf deals 50% more and takes 33% less; told to stay, vanilla; skeletons vanilla.
- [ ] Petting: experience once per 10 minutes per animal, the hover says "Content for ...", wolves get commands.
- [ ] Feeder: hidden below level 25; at 25 under Misc for Wood 10 and Leather scraps 4; a hungry tamed boar within 10 m
      walks to it and eats a carrot (one fewer, animation, hover fed); ground food first; an animal being tamed uses it
      too; a feeder open by another player loses the item there.
- [ ] Lore at 20: taming time, fed time, love or pregnancy, herd room, content time, grows up in, hatches in.
- [ ] Experience: taming a boar near you pays about 20 over the taming plus 30 for the first tame; a lox about four
      times that; a birth near you 3 x tier; loading into a farm pays nothing.
- [ ] **Dedicated server with two clients**, the animals owned by the other client: keeper levels, calm, breeding
      rolls, callouts on both clients, butchering credit, feeder takes and contentment all work.

### Status (2026-09-27)

0.7.0 released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.7.0), together with Elite Creatures Reborn
3.10.0 (its `Breeding/KeeperBonus.cs` reads the star-up key), before any in-game test. Written as a foundation (skill,
keeper, settings, taming, calm, lore, experience, the ECR hand-off) in the main session, then Breeding, Yield and
Companions by three agents in parallel, then a review. The build is clean (0 warnings) and the offline patch check
passes every patch class of the mod, the 35 Husbandry ones included. The decisions above wait for the user's
confirmation; the test checklist above is next.

## Farming

### The request (user, 2026-09-27)

Ideas for a Farming skill were given in chat (starred crops, heirloom seeds, giant crops, companion planting, rain,
an almanac hover, daily tending, level perks, experience, mill star pass-through, a compost bin). The user asked for
all of it, with the best decision taken for anything left vague. The decisions below are those calls, for the user
to confirm.

### What the game's Farming already does

Read from the decompiled assembly and the prefab bundles (UnityPy), 2026-09-27:

- **Skill 106, Farming.** The cultivator's piece table (`_CultivatorPieceTable.m_skill`) is Farming.
  - Every placement raises it by 1, paying off the remove debt first.
  - Build stamina drops by up to 50% with the level (`GetBuildStamina`).
  - Every crop pickable has `m_pickRaiseSkill` Farming, so picking raises it by 1.
  - Picking gives a bonus of `m_bonusYieldAmount` (1) with chance `skillFactor * m_maxLevelBonusChance` (0.25), rolled
    on the picker's client.
  - The scythe's harvest radius grows with the level (`Attack` and `Piece.OnPlaced` harvest).
- **Crop plants** (`sapling_*`): grow 4000 to 5000 s (seeded per plant), need cultivated ground, grow radius 0.5
  (magecap 0.8), scale 0.9 to 1.1, die when they cannot grow.
  - Carrot, onion, kale, oat, poteitr and the seed variants grow in the Meadows, Black Forest, Plains and Ashlands.
    Turnips add the Swamp and Mistlands. Barley and flax grow only in the Plains, jotun puffs and magecap only in the
    Mistlands. No crop grows in the Mountain or Deep North.
  - The Ashlands and cold biomes also need `m_tolerateHeat` / `m_tolerateCold` or a shield generator.
- **What each plant uses and gives:**

  | Plant | Uses | Gives |
  | --- | --- | --- |
  | carrot | carrot seeds | 1 carrot |
  | seed carrot | 1 carrot | 3 carrot seeds |
  | turnip, onion, kale, and their seed variants | the same pattern | kale gives 3, seed plants give 3 seeds |
  | barley, flax | barley, flax | 2 each |
  | jotun puffs, magecap | the mushroom | 3 |
  | oat | oat seeds | 3 oat seeds |
  | poteitr | poteitr seeds | 3 poteitr and 1 to 2 poteitr seeds |

- **Growth:** `Plant.SUpdate` checks on every slow-update pass. Its 10 s gate (`!(time > m_updateTime)`, then
  `m_updateTime = time + 10`) never closes, so in practice that is several times a second (IL checked, 2026-09-27).
  When the time since `s_plantTime` passes `GetGrowTime()`, the plant's owner calls `Grow()`, which instantiates the
  grown prefab (the owner owns the new ZDO), scales it and destroys the plant. `UpdateHealth` (status) and the
  half-grown look run on every client.
- **Picking:**
  - `Pickable.Interact` runs on the picker's client: it rolls the bonus, raises the skill, then sends
    `RPC_Pick(bonus)`.
  - The owner runs `RPC_Pick`: it drops `m_amount` (scaled by the world's resource rate) plus the bonus, then the extra
    drops, each through `Drop` → `Instantiate` + `ItemDrop.OnCreateNew`.
  - A crop has no respawn, so the owner destroys it.
- **Windmill** (a Smelter without fuel, spawns stacks): barley → barley flour, oat seeds → oats, oats → oat flour.
  - The queue is item names in the windmill's ZDO (`item0..n`).
  - The owner processes it (`UpdateSmelter` → `RemoveOneOre` + `QueueProcessed` → `Spawn`).
  - Players add through `OnAddOre` → `RPC_AddOre(name, cheated)`.
- **Spinning wheel:** flax → linen thread. **Vines** (`VineAsh`, `VineGreen`) grow segments as networked objects of
  their own, their berries respawn and give no Farming experience.

### Whose level counts

- **The planter's**, written on the plant at placement: star odds, growth speed, grow space, heat and cold tolerance,
  giant crops. The row width is the placing player's.
- **The picker's**, on their client: bonus yield, seed return, auto-replant, discovery.
- **Anyone's:** tending, rain and compost are the same for everyone.

### Which items carry stars

- **A crop plant** is a Plant whose grown prefab has a Pickable and is not a tree (`TreeBase`) or a vine (`Vine`). Its
  seed is its piece's first resource; its crops are its pickable's item and extra drop items. `Crops.IsCrop` /
  `Crops.IsCropPrefab` (Core) answer "a crop pickable", for the Foraging module too.
- **Kitchen relevant:** an item that is edible, an ingredient of a kitchen recipe, an input of a kitchen station or
  fermenter, or milled (a Smelter conversion) into something kitchen relevant.
- **A crop plant whose seed or crop is kitchen relevant** makes its seed and all its crops star items
  (`Kitchen.AddStarItem`). Then every Smelter conversion whose input carries stars gives its output stars too
  (barley flour, oats, oat flour).
- **Flax** is none of these, so it has no stars and the spinning wheel is left alone: starred linen would do nothing.
- **Effect on Cooking:** crops now count in a dish's ingredient average, so starred crops raise its odds and 0★ crops
  pull it down (before, vegetables never counted). Starred raw crops that are eaten (carrots, onions, mushrooms, kale,
  oats, poteitr) get the star food bonus as any dish does.

### Stars at ripening

- **Rolled by the plant's owner in `Grow`,** with the same odds table as dishes (section 3). The effective level is
  the sum of:
  - the planter's level;
  - Seed Levels Per Star (10) × the stars of the seed planted (heirloom);
  - Companion Levels (5) for each other crop kind growing or ripe within 2 m, up to 3 kinds;
  - Compost Star Levels (10) when fertilized.
- **Kind:** a seed and its crop are one kind (carrot and seed carrot), so companions must be real neighbours.
- **Stored on the grown pickable's ZDO,** with the plant it grew from (for auto-replant). Starred crops stand 6% taller
  per star.
- **Size:** the owner sets it with `ZNetView.SetLocalScale`, which reaches other clients only for prefabs that sync
  their scale (most crops, not magecap). For the others every client applies it when the crop loads (`CropLook`).
- **No roll:** wild crops, crops that ripened before Farming, and every crop while Farming or Crop Stars is off have
  0 stars.
- **Giant crops:** planter's share of 2% at level 100. A giant is always 3★, 2.5× the size and gives 6× the crop
  (scaled by the world's resource rate, in full stacks). The picker sees "Giant turnip!" and earns 5× the picking
  experience. Giants also grow while Crop Stars is off and from crops without stars (flax), then with 0 stars.

### Picking

- **Owner (`RPC_Pick`):** every dropped item that carries stars gets the crop's stars (extra drops too); a giant adds
  its extra stack.
- **Picker's client (`Interact`)** does the rest:
  - Bonus yield: 50% chance at level 100 instead of the game's 25%, via the instance's `m_maxLevelBonusChance`
    during the call.
  - Seed return: 30% at 100. One seed of the plant it grew from, with the crop's stars, pops out at the crop.
  - Auto-replant from level 50, each player's own switch: the same plant goes back in the same spot, paid with a seed
    from the inventory.
    - The seed's stars count, as in manual planting. It costs no stamina and earns planting experience.
    - Nothing happens without a seed or where the spot is not valid.
- **The scythe** picks through `Interact`, so all of this applies to every crop it cuts.

### Planting

- **Planter keys:** when the local player places a plant, its ZDO gets the planter's ID and Farming level (the
  placing client owns the new ZDO).
- **Heirloom seeds:** the stars of the seed paid for it are recorded while the placement pays (`ConsumeResources`,
  through `CraftRecord`, so the player's Ingredient Order decides which stack is used). Each placement takes the
  next plant waiting for its seed.
- **Row planting:** from level 25 a row of 3, from 50 a row of 5, each player's own switch. Hold the game's
  alternative place key (Shift) to plant one.
  - The row runs across the view, snapped to the nearest world axis. Spacing is twice the planter's grow radius plus
    0.1 m.
  - Each extra plant pays its own seed and needs a valid spot (cultivated ground where needed, no ward or no-build
    zone, room to grow). A spot that fails is skipped.
  - No extra stamina or wear, planting experience for each.
  - EarthWright's seed grid snaps the centre plant; its default spacing matches below level 1.

### Growing

- **Growth speed:** the planter's 40% at level 100, plus 25% when fertilized. `GetGrowTime` is divided by it on
  every client, from the ZDO, so the half-grown look agrees everywhere.
- **Rain:** at most once per 10 s of game time per plant (a mark in memory), the owner credits half the time since
  the last mark while it is wet at the owner's client (+50%), by moving `s_plantTime` back. Dry time moves the mark
  without credit; one credit is at most 30 s.
- **Tending:** E (the game's use key) on a growing plant tends it and every growing plant within 2.5 m that the
  player may access (ward).
  - Each gains 10% of its grow time, once per in-game day, through an RPC to its owner.
  - 0.25 experience per plant. The client remembers the plants it sent a tend to today, so pressing again before
    the owner's record comes back pays nothing.
- **Vines** get no planter, so none of the growth perks.
- **Grow space:** the planter's grow radius shrinks by up to 40% at level 100, set on each instance on every client.
- **Heat tolerance** from level 75: crops grow in the Ashlands without a shield. **Cold tolerance** from 100: crops
  that grow in the Meadows also grow in the Mountain and Deep North.
- **Almanac (hover):** a growing plant shows "Ripe in 12 min", tending ("[E] Tend" or "Tended today") and
  "Fertilized". From level 20 (the viewer's), it also shows the star odds of its roll, companions counted at hover.
- **Ripe crops** show their stars and "Giant" in the hover.

### Windmill

- **Adding:** `OnAddOre` sends our own RPC carrying the item's stars instead of the game's `RPC_AddOre`. The owner runs
  the game's add, then appends the stars to a parallel queue (one digit per item, `grindstone_mill_stars`).
- **Processing:** `RemoveOneOre` pops the front digit; `QueueProcessed` spawns the pending stack first when its stars
  differ (`grindstone_mill_spawn`). The spawned output gets the stars.
- **Breaking the windmill** drops queued items with their stars. Adds from other mods count as 0★.
- **Always on,** whatever Farming Enabled says: the stars belong to the items, like their stacking, and a queue whose
  digits stopped being kept would give later items the wrong stars.

### Compost bin

- **The piece:** a new piece in the cultivator's menu, a copy of the game's barrel (`piece_chest_barrel`: same model,
  container and workbench need). It is registered on every machine from code, so the prefab name matches everywhere.
  Cost: 10 wood, 4 stone.
- **Filling:** players put scraps in through the normal chest window, so quick stack works.
- **Composting:** every 30 s the bin's owner composts one unit into 1 point (up to 100). Compostable:
  - anything with food value;
  - anything that carries stars;
  - the Compost Items list (default Entrails, BoneFragments).
- **Feeding:** every 10 s the owner spends 1 point per growing crop within 12 m that is not fertilized, at most 20 per
  round, and marks it (directly, or by RPC to the plant's owner).
- **Fertilized** crops grow 25% faster and roll 10 levels better.
- **Kitchen trash:** a dish the trash filter throws away within 20 m of a bin adds 1 point to the nearest one.
- **Hover:** "Compost 23 / 100".

### Experience (section 31, synced)

- **The game's own:** 1 per planting, 1 per crop picked.
- **Scaling:** both are multiplied by Experience Multiplier and the crop's tier.
  - The tier is its value over Tier Reference Value (30), 1 to 3.
  - The value is the best food value among the plant's seed and crops, or what they are milled into, or the best
    kitchen dish they go into.
- **Bonuses:** Discovery ×3 on the first pick of each crop kind (player custom data). Giant crops ×5. Tending 0.25
  per plant.

### Icon

The icon the user supplied (2026-09-27), trimmed, centred on a square and scaled to 64x64 as
`assets/skill_farming.png`. It replaces the game's icon on the Farming skill definition when a player's Skills wake
(`Skills/GameSkillIcons.cs`, which does the same for Cooking with `assets/skill_cooking.png`), so the skills panel
and the level-up messages show it. Not loaded on a machine without graphics (`Core/EmbeddedIcon.cs`). Sailing's icon
is `assets/skill_sailing.png` since 0.8.2, the Karve's only as a fallback.

### Settings

- **29 - Farming:** Farming Enabled, Crop Stars, Seed Levels Per Star, Companion Levels, Companion Radius, Companion
  Kinds, Giant Crop Chance At 100, Giant Crop Yield, Giant Crop Size, and each player's own Show Callouts, Row
  Planting, Auto Replant.
- **30 - Farming Perks:** Growth Speed At 100, Grow Space Reduction At 100, Bonus Yield Chance At 100, Seed Return
  Chance At 100, Auto Replant Level, Row Of Three Level, Row Of Five Level, Heat Tolerance Level, Cold Tolerance Level,
  Rain Growth Bonus, Tending Bonus, Tending Radius, Almanac Level.
- **31 - Farming Experience:** Experience Multiplier, Tier Scaling, Tier Reference Value, Tier Maximum, Discovery
  Multiplier, Giant Crop Multiplier, Tending Experience.
- **32 - Compost:** Compost Enabled, Compost Time, Compost Capacity, Compost Radius, Compost Growth Speed, Compost Star
  Levels, Kitchen Trash Compost, Compost Items.

### Decisions made while building (2026-09-27), for the user to confirm

- **Seeds carry stars** (heirloom breeding) and only kitchen-relevant crops do. Flax does not, so neither does linen.
- **No auto-pickup filter:** in a base, items left on the ground never despawn. The compost bin is where spare crops
  go.
- **Vines stay vanilla:** their berries live on segment objects the vine spawns, and they give no Farming experience.
- **Shared odds table:** crops use the dishes' odds (section 3), so one table decides every star.
- **Row planting belongs to Farming,** not EarthWright: it is a level perk. EarthWright's grid only snaps the ghost.
- **Hardy crops** extend where generalist crops grow; barley, flax and the Mistlands mushrooms keep their biomes.

### Known gaps

- Rain follows the weather at the plant owner's client (the biome that player stands in).
- Items added to the windmill by other mods, and crops placed by other mods, carry no stars or planter.
- A plant placed close to a lower-level planter's crop can take its room, as in the game.
- The hover counts companions when it is shown. Neighbours can change before the crop ripens.
- The compost bin's feeding RPC can be lost if the plant unloads first; the point is then spent.
- All texts are English.

### Test checklist

- [ ] Farming Enabled off: planting, growing, picking and experience exactly vanilla; bins stop; the windmill still
      keeps the stars items already have.
- [ ] The log reads "Farming: N crop plants, M carry stars; star mills: windmill." (flax the only one without).
- [ ] Planting stores planter and seed stars; row of 3 at 25 and 5 at 50, skipping bad spots; Shift plants one;
      each extra pays its seed.
- [ ] Growth: 40% faster at level 100; rain shortens it; tending once per day in radius; almanac time matches.
- [ ] Grow space at 100 lets crops stand closer; heat tolerance in the Ashlands at 75; cold at 100 in the Mountain.
- [ ] Ripening rolls stars from the planter's level plus seed, companions and compost; giants appear at chance 100.
- [ ] Picking: drops carry the stars; bonus yield 50% at 100; seed return; auto-replant at 50 with a seed; scythe.
- [ ] Starred crops stack apart, raise dish odds, starred carrots eaten give the bonus.
- [ ] Windmill: 1★ and 3★ barley give 1★ and 3★ flour in separate stacks; breaking it returns starred barley.
- [ ] Compost bin: builds from the cultivator, composts food, feeds crops in range, kitchen trash adds points.
- [ ] Experience: tier, discovery once per crop kind, giant ×5, tending.
- [ ] **Dedicated server with two clients:** A plants, B picks. Stars follow A's level, bonus and seed return follow
      B's. The windmill and bin work when owned by the other player. Hovers agree on both clients.

## Status (2026-09-27)

0.1.0 released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.1.0), before any in-game test. Every feature is
written and the full build is clean (0 warnings). It is installed in the LocalTesting profile.
Nothing has been tested in game yet; the test checklist above is the next step. The store icon is in and `pack.ps1`
passes. Open: the raw-fish question.

0.2.0 (Sailing) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.2.0), before any in-game test: the
build is clean (0 warnings). The Sailing decisions above wait for the user's confirmation.

0.3.0 (Woodcutting) released to Thunderstore 2026-09-27 (commit e087c78, tag GrindstoneSkills-v0.3.0), with a new
store icon, before any in-game test: written as a foundation, then eight features in parallel, then a review; the
build is clean (0 warnings). The Woodcutting decisions above wait for the user's confirmation.

0.4.0 (Pickaxes) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.4.0), before any in-game test: written
as a foundation, then seven features in parallel, then a review and a docs pass; the build is clean (0 warnings). The
user set splash in 10-level steps, Clean Strike Damage ×2 and Clean Strike Experience 1; the other Pickaxes decisions
above wait for the user's confirmation.

0.5.0 (Foraging) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.5.0), before any in-game test; the
build is clean (0 warnings). It also moves Sailing onto the shared custom-skill registration (`Skills/CustomSkill*.cs`,
written by the Defense module). Built from HEAD plus Foraging while the Defense, Husbandry, Fishing and Farming
modules were being written in the same tree, so none of them is in it. Farming's `Core/Crops.cs` is to replace
`ForageCrops`. The Foraging decisions above wait for the user's confirmation.

0.6.0 (Defense) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.6.0), before any in-game test; the build
is clean. Built from HEAD plus Defense in a separate worktree while Husbandry, Fishing and Farming were being written
in the main tree, so none of them is in it. It also merges the PlateColumn library (the Defense plate). The Defense
decisions above wait for the user's confirmation.

0.7.0 (Husbandry) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.7.0) with Elite Creatures Reborn
3.10.0, before any in-game test; the build is clean. Built from HEAD plus Husbandry in a separate worktree while Fishing
and Farming were being written in the main tree, so neither is in it. The Husbandry decisions above wait for the
user's confirmation.

0.8.0 (Farming) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.8.0), before any in-game test; the build
is clean (0 warnings) and every Harmony target and parameter was checked offline against the game's assemblies. Written
in a separate worktree, reviewed by an agent (five fixes), released from HEAD plus Farming while Fishing was being
written in the main tree, so Fishing is not in it. It also moves Foraging onto the shared `Crops.IsCrop` and Husbandry
and Foraging onto `Kitchen.AddStarItem`. The Farming decisions above wait for the user's confirmation. 0.8.1 (tag
GrindstoneSkills-v0.8.1) adds the Farming skill icon the user supplied; 0.8.2 (tag GrindstoneSkills-v0.8.2) the
Cooking and Sailing icons.

0.9.0 (Fishing) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.9.0), before any in-game test: written as a
foundation and features in one session, reviewed by an agent against the decompiled game (seven fixes), offline patch
check clean, with the Fishing icon the user supplied. The Fishing decisions above wait for the user's confirmation.

0.9.1 (tag GrindstoneSkills-v0.9.1) adds the Woodcutting and Pickaxes skill icons the user supplied
(`assets/skill_woodcutting.png`, `assets/skill_pickaxes.png`, 64x64, entries in `Skills/GameSkillIcons`).

Compile check without touching dist/ or the test profile, and safe to run several at once (libraries built first):
`dotnet build GrindstoneSkills/GrindstoneSkills/GrindstoneSkills.csproj -c Release --no-restore --no-dependencies -p:SkipRepack=true -p:CheckDir=<name>`.

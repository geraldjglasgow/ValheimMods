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
- `Sailing/`: skill registration and cheats, the three perks, experience; `Sailing/Lookout/` the milestone.

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
  rolls stars, and those feed the cooking station as input stars. Fish caught directly is 0 stars. Undecided; ask
  the user.
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
- **Registration (`Sailing/SkillRegistration.cs`), read from the game code 2026-09-27:**
  - `Skills.GetSkill` creates a skill for any type, with the definition `GetSkillDef` finds in `m_skills`, or null.
    A null definition breaks the level-up message, so `Skills.Awake` adds Sailing's definition to every `Skills`.
  - `Skills.Load` keeps only types the enum defines (`IsSkillValid`), so without a patch a saved Sailing level is
    dropped at load and lost at the next save.
  - The skills panel and the level-up message name a skill `$skill_` + the type's name in lower case: the number
    here. The word goes to the game's localization in `Localization.SetupLanguage`.
  - The icon is the Karve's piece icon, read when `ZNetScene` wakes.
  - The death penalty (`LowerAllSkills`), the skills panel and the world's skill-gain modifier work unchanged.
- **Console:** `raiseskill` and `resetskill` match the enum's names, so "sailing" is handled by a patch and "all"
  includes Sailing (`Sailing/SkillCheats.cs`).
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

### Settings

- **8 - Sailing (synced):** Sailing Enabled, Ship Health At 100, Ship Speed At 100, Exploration Radius At 100, Helm
  Experience Per Kilometre, Crew Experience Share.
- **9 - Lookout:** Lookout Level, Lookout Radius, Lookout Duration, Lookout Cooldown (synced); Lookout Key (each
  player's own).

### Decisions made while building (2026-09-27), for the user to confirm

- **Ship health is fixed by the builder's level when the ship is placed.** Ships built before GrindstoneSkills keep
  the game's health; repairing does not refit a ship to the builder's current level.
- **Speed covers the oars as well as the sail**, with one setting.
- **The reveal goes to everyone aboard the pulsing ship**; players nearby who are not aboard see the ring and hear
  the ping but get no tags.
- **Experience from distance only**, crew at 25%, nothing for building.
- **The lookout cooldown is per player and in memory**; a relog resets it.
- **One switch turns all of Sailing off** (perks, experience, lookout); levels are kept.

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

## Status (2026-09-27)

0.1.0 released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.1.0), before any in-game test. Every feature is
written and the full build is clean (0 warnings). It is installed in the LocalTesting profile.
Nothing has been tested in game yet; the test checklist above is the next step. The store icon is in and `pack.ps1`
passes. Open: the raw-fish question.

0.2.0 (Sailing) released to Thunderstore 2026-09-27 (tag GrindstoneSkills-v0.2.0), before any in-game test: the
build is clean (0 warnings). The Sailing decisions above wait for the user's confirmation.

Compile check without touching dist/ or the test profile, and safe to run several at once (libraries built first):
`dotnet build GrindstoneSkills/GrindstoneSkills/GrindstoneSkills.csproj -c Release --no-restore --no-dependencies -p:SkipRepack=true -p:CheckDir=<name>`.

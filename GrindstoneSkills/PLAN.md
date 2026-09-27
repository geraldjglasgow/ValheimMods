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
| Seams | 10% of swings at level 0 to 40% at 100 open a seam; window 2 s to 5 s | your own, your client |
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
  - A seam is a chunk other than the ones the swing touched, intact, within 2 m of the hit point and visible from
    the miner's eye, marked by a local-only glow (gold light, halo and the game's glint star) and a soft clink. Each
    miner sees only their own seams.
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
  chunk's centre; lag can pick a chunk that then vanishes (the seam closes quietly); a seam opens one frame after its
  swing.

### Test checklist

- [ ] Pickaxes Enabled off: rocks, drops, hover, sounds and experience exactly vanilla; no seams, no Echo.
- [ ] Experience: neutral settings give vanilla; Meadows boulder ×1, Black Forest copper ×1.875, Mountain stone ×1.75,
      Mountain silver ×2.625 with an iron pickaxe and ×1 with bronze.
- [ ] Discovery: the first hit on copper floats "Discovered Copper deposit!" and gives 18.75 once, also after relog;
      none on stone; it waits for the tool tier; mudpile and mudpile2 are one discovery; an ice rock reads "Ice";
      Show Callouts off hides the text but credits; Discovery 0 records nothing.
- [ ] Seams at level 0: about 1 swing in 10 on a multi-chunk rock opens a glint on a nearby visible chunk, never one
      just hit, gone after 2 s; readable in daylight and at night; Seam Chance 0: none.
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

Compile check without touching dist/ or the test profile, and safe to run several at once (libraries built first):
`dotnet build GrindstoneSkills/GrindstoneSkills/GrindstoneSkills.csproj -c Release --no-restore --no-dependencies -p:SkipRepack=true -p:CheckDir=<name>`.

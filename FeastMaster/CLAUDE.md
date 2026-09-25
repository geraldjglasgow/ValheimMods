# CLAUDE.md - FeastMaster

Food, mead, stamina and eitr tuning for Valheim. The 3.x core (the food and mead sections, the degradation switch,
the global multipliers and the config migration) is the author's own original code from the earlier
ValheimFoodConfig mod; everything from 4.0.0 onwards was written black-box from a behaviour specification (`SPEC.md`
while it exists) and the game code alone; the rules are under "Developing mods" in `../CLAUDE.md`. This file is
the code map, the decisions the spec left open, and the in-game test checklist.

Rules that still apply to every change:

- Never read, grep, decompile or quote the source, DLL, config or save data of any other mod.
  The mod has its own GUID, config keys, section names, localization keys and vocabulary
  (Vigor, Eitr Vigor, Continuous Food Healing, Degradation Curve, Eat Again At); no compatibility layer for other
  mods. Migrating FeastMaster's own renamed entries is allowed (`ConfigMigration`).
- Verify every patched signature against a fresh `ilspycmd` decompile of the game's own assemblies
  (`assembly_valheim.dll`; `Localization` lives in `assembly_guiutils.dll`) in the scratchpad, never in a repository.
- Functions 5 to 20 lines, one responsibility per class, classes under 300 lines. Prefix, postfix and finalizer
  patches only; no transpilers. Every patch class is applied on its own in `PatchEverything`, so one failure is
  logged and the others still apply.
- Settings are read at use time (`ConfigEntry.Value`), never cached, so edits, file reloads and server pushes apply
  at once. Every gameplay setting is bound synced; only section `8. Display` is bound with `synced: false`.
- Game fields are never changed permanently: a drain, regen or delay field is scaled or swapped in a prefix for
  one call and restored in a finalizer. Item shared data and mead status effect assets are the exception, written
  from the configured values on load and on every change.
- Zero compiler warnings from the mod's own code. Build from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build FeastMaster/FeastMaster/FeastMaster.csproj -c Release`.

## Layout

```
FeastMaster/FeastMaster/FeastMasterCore/
  FeastMaster.cs          plugin entry: settings, config hooks, patches per class, Synced.Finish, Guard.Install last
  FeastMasterData.cs      section 0 (multipliers, Disable Food Degradation, Degradation Curve, Eat Again At, Lock
                          Configuration), one section per food (Health, Stamina, Duration, HealthRegen, Eitr, Vigor,
                          EitrVigor) and per mead (nine entries), bound from the item database on
                          ObjectDB.Awake / CopyOtherDB, with migration of the 3.3.x names
  MeadEffectConfig.cs     the nine entries of a mead
  ConfigMigration.cs      reads BepInEx's private OrphanedEntries to carry a value over to a renamed section or key
  Settings.cs             sections 1 to 3: Health Regeneration, Stamina Regeneration, Eitr Regeneration
  SettingsMore.cs         sections 4 to 8: Stamina Costs, Base Values, Skills, World Rates, Display (per client);
                          the FoodTimers enum
  SettingsKitchen.cs      section 9: Kitchen (fermenter, cook time multiplier, burning, feast servings; carries over
                          4.2.0's 9. Fermenter)
  ScenePrefabs.cs         ZNetScene.Awake pass: binds the station sections and every feast's food section, file
                          written and item values applied once
  CookTimes.cs            one section per cooking station prefab, one entry per recipe (raw item)
  SettingsEating.cs       Allow Auto Eat (section 0, synced) and Auto Eat (section 8, per player)
  AutoEat.cs              Auto Eat (Player.UpdateFood prefix/postfix, local player)
  Rested.cs               Rested entries in section 2, bound from the item database with the effect asset's values
                          as defaults; written into the asset and the local player's running effect by ItemValues
  FeastServings.cs        Feast Servings (Feast.GetStack, GetStackPercentige capped at 1, GetHoverText)
  Cooking.cs              cook times and burning (CookingStation.UpdateCooking, on the station's owner)
  ItemValues.cs           writes the configured values into the items' shared data (the prefab and every live
                          copy, through ItemCopies; new copies via Copies.HookSpawns) and the mead status effect
                          assets on load and on every SettingChanged / ConfigReloaded, then forces the player's
                          food update and refreshes the mead effect the local player is under; Vigor and Eitr
                          Vigor per item
  Patches.cs              ObjectDB load hooks, consume-time safety nets (Player.EatFood, SEMan.AddStatusEffect)
  FoodDegradation.cs      Degradation Curve and Disable Food Degradation (Player.GetTotalFoodValue prefix)
  BaseValues.cs           Base Health / Base Stamina (GetTotalFoodValue prefix) and stamina from skills (postfix)
  HealthRegen.cs          Continuous Food Healing (Player.UpdateFood prefix, heals per frame, holds the tick timer at 0)
  StaminaRegen.cs         SEMan.ModifyStaminaRegen postfix (Vigor, extra stamina, sneak bonus, curve, blocking
                          factor), RegenCurve (shared with eitr), Player.UpdateStats postfix (encumbered and
                          swimming regen), OnSwimming stroke timer
  RegenBasics.cs          Stamina Regen Multiplier, Low Stamina Regen Bonus, Eitr Regen Multiplier (UpdateStats
                          prefix/finalizer), Stamina Regen Delay (RPC_UseStamina), Eitr Regen Delay (RPC_UseEitr)
  EitrRegen.cs            SEMan.ModifyEitrRegen postfix (Eitr Vigor, eitr curve, blocking eitr factor)
  CostRules.cs            out of combat, free sneaking, skill discount, and the scale/swap/restore helpers
  StaminaCostsMovement.cs run, jump, dodge, sneak, swim, encumbered costs
  StaminaCostsActions.cs  block, attack, tool, fishing, harpoon costs
  GameplayRules.cs        Eat Again At (Food.CanEatAgain), skill gain (Skills.RaiseSkill), Drowning Damage
  WorldRates.cs           world rate overrides (Game.UpdateWorldRates postfix, refresh on setting change)
  Fermenting.cs           Fermentation Time (Fermenter.GetStatus) and Batch Yield (DelayedTap)
  Display.cs              hidden HUD numbers, Food Timers, the Vigor / Eitr Vigor tooltip lines, the localization words
```

Startup order in `Awake`: `FeastMasterData.Initialize`, `Settings.Initialize`, `ItemValues.HookConfig`,
`WorldRates.HookConfig`, every patch class on its own, `Synced.Finish`, the `Loading [FeastMaster x.y.z]` line,
`Guard.Install` last.

## Patched game methods

Postfix: `Attack.GetAttackStamina`, `Feast.GetStackPercentige` (capped at 1), `ZNetScene.Awake` (`Priority.Last`, binds the station and feast food sections), `Fish.GetStaminaUse`, `Game.UpdateWorldRates`, `Hud.UpdateHealth`,
`Hud.UpdateStamina`, `Hud.UpdateEitr`, `Hud.UpdateFood`, `ItemDrop.ItemData.GetTooltip` (static, six
parameters), `Localization.SetupLanguage`, `ObjectDB.Awake`, `ObjectDB.CopyOtherDB`, `Player.Food.CanEatAgain`,
`Player.GetBuildStamina`, `Player.GetDodgeStaminaUse`, `Player.GetTotalFoodValue` (`ref float stamina`),
`Player.OnSwimming` (stroke timer), `Player.UpdateStats(float)` (encumbered and swimming regen),
`SEMan.ModifyStaminaRegen`, `SEMan.ModifyEitrRegen`.
Prefix and postfix: `Player.UpdateFood` (Auto Eat; the expiring food in the prefix, the refill in the postfix).
Prefix: `Character.Damage` (drowning), `Player.EatFood`, `Player.GetTotalFoodValue` (degradation, base values),
`Player.UpdateFood`, `SEMan.AddStatusEffect(StatusEffect, ...)` (target picked by first parameter),
`Skills.RaiseSkill` (`ref float factor`).
Prefix and finalizer (field scaled or swapped for one call): `Character.Jump`, `CookingStation.UpdateCooking`, `Feast.GetStack`, `Feast.GetStackPercentige`, `Feast.GetHoverText`, `Fermenter.GetStatus`,
`Fermenter.DelayedTap`, `FishingFloat.FixedUpdate`,
`Humanoid.BlockAttack`, `Player.CheckRun`, `Player.OnSneaking`, `Player.OnSwimming`, `Player.RPC_UseEitr`,
`Player.RPC_UseStamina`, `Player.UpdateStats(float)` (encumbered cost and the regen basics), `SE_Harpooned.UpdateStatusEffect`.

Helpers that return a cost (`GetDodgeStaminaUse`, `GetAttackStamina`, `GetBuildStamina`, `Fish.GetStaminaUse`) get a
postfix on `__result`. The scratchpad tool `patchcheck` (a .NET 8 console program) resolves every patch class
against the game assemblies and checks the injected parameter names; run it after touching a patch.

## Config sections

`0. Global Settings`, `1. Health Regeneration`, `2. Stamina Regeneration`, `3. Eitr Regeneration`,
`4. Stamina Costs`, `5. Base Values`, `6. Skills`, `7. World Rates`, `8. Display` (unsynced), `9. Kitchen`, then one section per
food prefab (seven entries), one per mead prefab (nine entries) and one per cooking station prefab (one entry per
recipe, keyed by the raw item's prefab name). BepInEx writes sections sorted by name; digits
sort before letters, so the numbered sections come first and the items follow alphabetically. Keys and defaults
are listed in `README.md`. Renames from 3.3.x with migration: `General/Lock Configuration` to `0. Global Settings`,
`0Meads_<prefab>` to `<prefab>`. Renames from 4.0.0 with migration (same section, new key; the three settings named
in the 4.1.0 changelog): `Continuous Food Healing`, `Count Food Stamina Only` and `Regen Per Extra Stamina Point`,
whose former per-10-points value is divided by 10. Renamed in 4.3.0 with migration: section `9. Fermenter` to
`9. Kitchen` (same keys). Localization keys: `$fm_vigor` ("Vigor"), `$fm_vigor_regen` ("stamina regen"),
`$fm_eitr_vigor` ("Eitr Vigor"), `$fm_eitr_vigor_regen` ("eitr regen").

## Decisions where the spec was silent

- Jump cost is scaled around `Character.Jump` rather than `Player.OnJump`: `Jump` first checks
  `HaveStamina(m_jumpStaminaUsage)` and then calls `OnJump`, which drains the same field, so a discounted jump is
  never refused for lack of stamina.
- Attacks of home items (`Attack.m_isHomeItem`: hammer, hoe, cultivator swings) take `Tool Cost`, like their
  placement through `GetBuildStamina`; `Attack Cost` covers every other attack.
- The fish's own stamina use (`Fish.GetStaminaUse`, added to the pull drain) is scaled by `Fishing Pull Cost` too.
- "Sneaking and not moving" for `Sneak Skill Regen Bonus` is `IsCrouching() && !IsSneaking()`: the game's
  `IsSneaking` is crouching while moving.
- `Free Sneaking Without Enemies` also requires not sensed and not targeted, because `BaseAI.InStealthRange` is
  false both when no enemy is near and when a near enemy is already alerted.
- The regen rules combine in this order: Vigor, extra stamina and sneak bonus are added to the game's multiplier,
  then the bar-level curve factor and the blocking factor (`configured / 0.8`, which turns the game's 0.8 into the
  configured value) multiply the result. Eitr: Eitr Vigor added, then the eitr curve and blocking factor; the game
  adds its equipment eitr regen modifier after this hook, so the eitr curve does not scale that modifier.
- Encumbered and swimming regen keep the game's other zero conditions (attacking, dodging, wall running) and the
  blocking factor; when both states apply the encumbered fraction is used.
- Vigor and `Regen Per Extra Stamina Point` both reward food stamina; with both on it counts twice. Allowed, the
  README recommends one of the two. Extra stamina is measured above the configured `Base Stamina` and excludes the
  skill stamina.
- `Stamina Regen Delay` and `Eitr Regen Delay` are applied around `Player.RPC_UseStamina` / `Player.RPC_UseEitr`,
  where the game sets the regen timers (the public `UseStamina` / `UseEitr` only route to them).
- `Food Timers`: the game always shows the timer under an active food, so `Vanilla` and `Always` behave the same
  today; `Never` hides it.
- World rate overrides: a change in section `7. World Rates` (or a file reload) calls the game's own
  `ZoneSystem.UpdateWorldRates`, which recomputes the rates from the current global keys; the postfix then applies
  the overrides above 0. Nothing is remembered, so 0 restores the world's value exactly. Nothing happens outside a
  world (no `ZoneSystem`).
- Migration: `ConfigMigration.TakeOrphan` (moved section) and `TakeRenamed` (new key) remove the old entry from
  BepInEx's orphans and return its text unless the file already has the new entry, in which case the file's new
  value wins and the old entry is dropped. `ApplyDivided` converts a number whose unit changed.
- Configured values are written into the shared item data (and the mead status effect assets) on load and on every
  setting change. Every world item, picked-up item and eaten food holds its own copy of the shared data (the game
  re-links it to the prefab only in the editor), so the writes go through the ItemCopies library: the prefab, every
  live copy, and new copies from `ItemDrop.Awake` and `Inventory.AddItem`. After a write `Player.UpdateFood(0, true)`
  is forced so max health follows at once. The consume-time patches remain as a safety net.
- A mead's status effect is cloned onto the player by `SEMan.AddStatusEffect`, so the asset write alone never
  reaches a running effect. After writing a mead's asset, `ItemValues` walks `Player.m_localPlayer.GetSEMan()
  .GetStatusEffects()` and writes the same values into every `SE_Stats` with that mead's `NameHash`. The clone
  counts `m_time` up from 0 and ends when it passes `m_ttl`, so a changed `Duration` keeps the same fraction of
  the effect remaining: `m_time` is rescaled by new ttl / old ttl rather than reset. Nothing happens without a
  local player; the `AddStatusEffect` prefix still writes the values into a freshly cloned effect.
- `SettingChanged` is ignored while the item database is being bound (BepInEx raises it for every entry read from
  the file); one `ApplyAll` runs at the end of the load instead. Foods and meads are both keyed by prefab name,
  which is also their section name, so a change is applied to that one item.
- `Localization.AddWord` is private and the dictionary is cleared on every language setup, so the words are added
  in a postfix of `SetupLanguage` (assembly_guiutils, publicized).
- Fermenter settings are one global value each, not per mead or per barrel: `Fermentation Time` is absolute seconds
  (0 = the barrel's own, so modded barrels keep theirs), `Batch Yield` replaces every recipe's count (0 = the
  recipe's own). The barrel keeps only its start time in the ZDO and GetStatus compares at read time, so a changed
  time applies to barrels already brewing: shorter makes a long-brewing barrel ready at once, longer can turn a
  ready, untapped barrel back to brewing. Every peer evaluates GetStatus (hover, visuals, interact) and the ZDO
  owner rechecks it before a tap and spawns the batch, so the synced value keeps them in agreement.
- Cook times: one entry per station and recipe, absolute seconds defaulting to the game's time, times the global
  `Cook Time Multiplier` (the same shape as the food values and their global modifiers). Per station rather than
  in the cooked food's section, because the same food can cook on more than one station. The cauldron is a
  crafting station (instant recipes), not a CookingStation, so it has no cook time to configure.
  `Food Can Burn` off clears the station's `m_canOvercookItems` for the tick; on keeps each station's own flag.
  Cooking runs only on the station's ZDO owner and the cooked time lives in the ZDO, so the values are applied
  only there; an edit reaches food already on the fire at the next one-second tick. Food that is already burnt
  stays burnt, and food already done stays done when the time is raised.
- Feast servings: one global count, not per feast (0 = each feast's own). The ZDO holds the servings left only once
  someone has eaten, so untouched feasts follow a change and started ones keep what they have left; their share
  is capped at 1, because the deconstruct refund multiplies the feast's resources by it and a lowered count would
  otherwise refund more feasts than were placed. The visual stages redraw on the next serving or load.
- Feast food: the feast's `m_foodItem` (or the feast prefab's own ItemDrop) is bound as a food from the ZNetScene
  pass whatever its item type, so its values and the global modifiers apply like any food's. Placed feasts get
  the values through the ItemDrop.Awake copy hook and `Player.EatFood`.
- Mead cooldown: the game has none of its own. A mead cannot be drunk while its own effect or one of the same
  category runs (`Player.CanConsumeItem`), so `Duration` is the cooldown; decided with the user to document that
  rather than add a separate entry.
- Auto Eat: the server permits (`Allow Auto Eat`, synced, default off) and each player opts out (`Auto Eat` in
  section 8, default on), decided with the user. The replacement is the same food by shared name, eaten through
  `Humanoid.UseItem` with `fromInventoryGui: true` (otherwise it would be used on the hovered interactable, a
  cooking station for example). No food of the same kind: nothing is eaten.
- Rested lives in `2. Stamina Regeneration`: a new numbered section would sort `10.` before `2.`. Its entries are
  absolute values with the effect asset's as defaults, like the food and mead sections. A changed duration applies
  from the next rest (the game computes the running effect's time once, at the start); regeneration follows at once.

## Test checklist (LocalTesting profile)

Launch through the r2modman profile `LocalTesting` (the build copies the DLL there). Never start or kill the game
from a script.

1. Fresh config: only the new sections and the per-food `Vigor` and `EitrVigor` lines are new; no vanilla behaviour changes.
2. Tooltip of an uneaten food shows configured Health/Stamina/Duration after editing its section and saving.
3. `Degradation Curve` 1: food strength falls linearly (HUD max health drops steadily); 0 keeps full strength.
4. `Continuous Food Healing` on: health rises smoothly at the tick's rate; no floating numbers; off restores ticks.
5. `Vigor Per Stamina Point` 0.2 on a fresh config: the tooltip shows Vigor for stamina foods; regen visibly
   faster with three stamina foods; `Vigor Multiplier` 0 removes it.
6. `Regen Curve Strength` 2, `Regen Curve Pivot` 0.4: the bar refills at twice the rate when empty, at the normal
   rate at 40%, at half the rate when full.
7. `Regen Per Extra Stamina Point` 0.2 with 150 food stamina: about +30% regen; `Count Food Stamina Only` toggles
   gear stamina out.
8. `Encumbered Regen Fraction` 0.5: bar regenerates slowly while encumbered and still drains when walking.
9. `Swimming Regen Fraction` 0.5, delay 3: tread water, after 3 s the bar starts refilling.
10. Each cost multiplier at 0 makes the action free; at 2 doubles the drain (check run, jump, dodge, block,
    attack, sneak, swim, tool, fishing, harpoon).
11. Out of combat run 0.5: running away from a spawn point with nothing around drains half; a targeting greydwarf
    restores full drain.
12. `Free Sneaking Without Enemies` on: sneaking in an empty meadow drains nothing.
13. `Run Skill Stamina` 30 at run skill 50: max stamina +15 shown on the HUD.
14. `Hide Stamina Number` hides the number for this client only and is not written by the server.
15. Server: every gameplay setting listed above is overwritten by the server's value on a client with
    Lock Configuration on; `Hide Stamina Number` is not.
16. Config file: every numbered section is above the first food section; no `0Meads_` or `General` section
    remains after the first save; an old file's mead Duration and Lock Configuration values survive the rename.
17. `Stamina Regen Delay` 3: after a jump the bar waits 3 s before refilling. `Stamina Regen Multiplier` 2 refills
    twice as fast. `Blocking Regen Factor` 1: no slowdown while blocking.
18. `Eitr Vigor Per Eitr Point` 0.5 with an eitr food: tooltip shows Eitr Vigor and eitr refills faster.
19. `Base Health` 50: the HUD base bar doubles with no food. `Base Stamina` 100: max stamina 100 with no food.
20. `Eat Again At` 0.9: food can be re-eaten almost at once; 0: never until it runs out.
21. `Run Skill Gain` 5: Run skill rises visibly faster.
22. `Drowning Damage` 0: no damage when out of stamina in water.
23. `Food Rate` 2: food timers count down twice as fast; back to 0 restores the world's rate.
24. Mead `StaminaRegenMultiplier` 2 on Minor Stamina Mead: regen doubles while it lasts.
25. Drink a mead, then change its `StaminaRegenMultiplier` (or `StaminaOverTime`) in the file and save: the
    running effect follows without re-drinking; changing its `Duration` keeps the same share of the timer left.
26. `Hide Health Number`, `Hide Eitr Number`, `Food Timers` Never: the HUD follows, per client.
27. `Fermentation Time` 60: a new batch shows ready after one minute of world time; a barrel already brewing for
    over a minute turns ready within 2 s of saving the file, with no restart. Back to 0: 2400 s again.
28. `Batch Yield` 10: tapping spawns 10 meads; 0 restores the recipe's count.
29. Dedicated server, both settings changed in the server's file while a client stands at a barrel: the client's
    hover text and the barrel's lid follow the server's time, and the tap (spawned by the ZDO owner) gives the
    server's yield, whichever client owns the barrel.
30. `Cook Time Multiplier` 0.1 with raw meat on the cooking station: done in a few seconds instead of the game's
    time; the station's own section (e.g. `RawMeat`) set to 60 with multiplier 1: done after a minute.
31. `Food Can Burn` off: cooked meat left on the fire stays cooked for minutes; on again: it burns at the next tick
    if it has been on for over twice its cook time.
32. The oven: `Cook Time Multiplier` applies to bread and pies too; the file has a section per station with one
    entry per recipe, and a 4.2.0 file's `9. Fermenter` values appear under `9. Kitchen` after the first start.
33. Dedicated server, change `Cook Time Multiplier` in the server's file while a client cooks: the food follows
    the server's value whichever player owns the station, without a restart.
34. `Feast Servings` 10: a freshly placed feast shows `10/10` on hover and serves 10 times; the visual stages step
    down with it. A feast eaten to 3 left under 5 still has 3 left after the change.
35. A feast's own section (its food prefab) exists in the file; its `Health` changed there is what eating from the
    placed feast gives.
36. Lower `Feast Servings` below a started feast's remaining servings, then deconstruct it: no more feasts come back
    than were placed.
37. `Allow Auto Eat` on, two cooked meat in the inventory, `Food Rate` 50: when the eaten meat runs out another is
    eaten with the eat sound, and the stack drops by one; with none left nothing is eaten. `Auto Eat` off in
    section 8: nothing is eaten for this player only. `Allow Auto Eat` off on the server: nobody auto-eats.
38. Auto Eat while looking at a cooking station: the meat is eaten, not put on the station.
39. `Rested Duration` 60 and `Rested Duration Per Comfort` 0: after sleeping, Rested shows one minute. `Rested
    Stamina Regen` 3 while rested: stamina refills visibly faster at once, without resting again.
40. A mead's description in the file mentions the cooldown; `Duration` 30 on Minor Healing Mead lets the next one be
    drunk after 30 s.

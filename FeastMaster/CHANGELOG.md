# Changelog

## 4.6.0

- Food Slots: how many foods a player can have active at once, 1 to 5 (the game's 3 by default). The HUD shows the
  extra foods. Lowering it keeps extra foods until they run out.

## 4.5.0

- Auto Eat: a food that runs out is replaced by another of the same from the inventory. The server allows it
  (`Allow Auto Eat`, off by default); each player can turn it off (`Auto Eat` in `8. Display`).
- Rested: `Rested Duration`, `Rested Duration Per Comfort` and the rested stamina, health and eitr regeneration, in
  `2. Stamina Regeneration`. Defaults are the game's values.
- A mead's `Duration` is also its cooldown; its description now says so.

## 4.4.0

- Feasts: `Feast Servings` in `9. Kitchen` sets how many servings a placed feast holds. A feast already eaten from
  keeps the servings it has left.
- Every feast's food gets a food section, even when the game does not list it as a consumable.
- Faster world loading: the cooking station sections no longer re-apply every food value once per entry.

## 4.3.0

- Cooking: every cooking station and oven gets its own section with the cook time of each recipe, and `9. Kitchen`
  gets `Cook Time Multiplier` for all of them and `Food Can Burn`. Defaults are the game's values; edits reach food
  already on the fire.
- Section `9. Fermenter` is now `9. Kitchen`. Its values carry over.

## 4.2.0

- New `9. Fermenter`: `Fermentation Time` and `Batch Yield`. Both default to the game's values, sync from the server
  and apply to barrels already brewing.

## 4.1.0

- Config edits now reach items already picked up, foods already eaten and a mead currently running.
- Server sync moved to Charter, our own library. Existing config files work unchanged.
- Version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter`: `status`, `diff`, `versions`.
- Renamed: `Steady Regeneration` → `Continuous Food Healing`, `Extra Stamina From Food Only` → `Count Food
  Stamina Only`, `Regen Per 10 Extra Stamina` → `Regen Per Extra Stamina Point` (a former 1 becomes 0.1). Old
  values carry over automatically.

## 4.0.0

- Configured values apply at load and on every change, so tooltips show them before the food is eaten.
- Config reorganised into numbered global sections above per-item sections. Old values carry over.
- New `Degradation Curve` and `Eat Again At`.
- New `1. Health Regeneration` — continuous food healing.
- Vigor: every food gives a stamina regen bonus, shown on the tooltip. Defaults to vanilla.
- New `2. Stamina Regeneration` — multipliers, delay, bar-level curve, extra-stamina regen, sneak bonus,
  encumbered and swimming regen.
- New `3. Eitr Regeneration` — Eitr Vigor, multiplier, delay, curve, blocking factor.
- New `4. Stamina Costs` — a multiplier per drain, out-of-combat multipliers, skill discount, drowning damage.
- New `5. Base Values`, `6. Skills`, `7. World Rates`.
- New `8. Display`, per player: hide health/stamina/eitr numbers, food timers.
- Per mead: health, stamina and eitr regen multipliers, run and jump stamina modifiers.
- Every new setting defaults to vanilla, syncs from the server, locks and hot reloads.

## 3.3.5

- New store icon.

## 3.3.4

- Store page links to the GitHub repository.

## 3.3.3

- Rebuilt on the rewritten shared configuration libraries. No behaviour change.
- MIT licence added.

## 3.3.2

- Fixed a frame rate loss from the shared library's exception-tagging finalizer.

## 3.3.1

- Fixed a dedicated server hanging in an endless config-reload loop.
- Config entries written once instead of once per entry, removing a long menu freeze with many modded consumables.

## 3.3.0

- Updated for the current Valheim version.
- Jotunn no longer required.
- `Lock Configuration` now actually locks clients.
- Meads detected automatically and matched by status effect.
- Disable Food Degradation no longer rewrites game code.

## 3.2.0

- Fixed disable food degradation.

## 3.1.1

- Fixed icon.

## 3.1.0

- Config changes apply without a restart.
- Values applied at consumption time, for multiplayer.

## 3.0.0

- Renamed from ValheimFoodConfig to FeastMaster.

## 2.3.1

- Mead values configurable.

## 2.3.0

- Option to remove food degradation.

## 2.2.0

- Global food config modifiers.

## 2.1.0

- Fixed incompatibility with a new Valheim version.

## 2.0.0

- New config file format.

## 1.1.1

- Eitr food values configurable.

## 1.1.0

- New config file format.

## 1.0.0

- Food values configurable.

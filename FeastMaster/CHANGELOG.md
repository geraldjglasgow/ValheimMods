# Changelog

## 4.7.2

- Faster loading: the config file is written once instead of once per setting.

## 4.7.1

- Less work every frame keeping the server's settings in sync.

## 4.7.0

- Settings at their defaults no longer touch the game: FeastMaster only patches what a changed setting needs, so
  other mods' fishing, movement, skill, base value and regeneration changes keep working alongside it.
- Setting a value back to its default removes its patch and gives the game (or the other mod) its value back.
- Fixed: fishing costs, the regen multipliers and base health and stamina were rewritten even at their defaults.
- Foods, meads and Rested keep their own (or another mod's) values until one of theirs is changed.
- The log names the patches that changed settings installed.

## 4.6.1

- Shorter store page and changelog; nothing changes in game.

## 4.6.0

- New `Food Slots`: 1 to 5 foods active at once (the game's 3 by default); the HUD shows that many slots.
- Lowering it keeps extra foods until they run out.

## 4.5.0

- Auto Eat: a food that runs out is replaced by another of the same from the inventory.
- The server allows it (`Allow Auto Eat`, off by default); each player can turn it off (`Auto Eat` in `8. Display`).
- Rested: `Rested Duration`, `Rested Duration Per Comfort` and the rested regeneration, in `2. Stamina Regeneration`.
- A mead's `Duration` is also its cooldown; its description now says so.

## 4.4.0

- `Feast Servings` in `9. Kitchen`: how many servings a placed feast holds; started feasts keep what they have left.
- Every feast's food gets a food section.
- Faster world loading.

## 4.3.0

- Cooking: a section per cooking station and oven with each recipe's cook time; edits reach food already on the fire.
- New `Cook Time Multiplier` and `Food Can Burn` in `9. Kitchen`.
- Section `9. Fermenter` is now `9. Kitchen`; its values carry over.

## 4.2.0

- New `9. Fermenter`: `Fermentation Time` and `Batch Yield`, applying to barrels already brewing.

## 4.1.0

- Config edits reach items already picked up, foods already eaten and a running mead.
- Server sync moved to Charter, our own library; config files work unchanged.
- A version mismatch refuses the join with a named reason.
- New console command `charter` (`status`, `diff`, `versions`).
- Renamed `Steady Regeneration` to `Continuous Food Healing` and `Extra Stamina From Food Only` to `Count Food
  Stamina Only`.
- Renamed `Regen Per 10 Extra Stamina` to `Regen Per Extra Stamina Point` (a former 1 becomes 0.1). Old values carry
  over.

## 4.0.0

- Config reorganised into numbered sections above the per-item ones; old values carry over.
- Configured values show on tooltips before eating and apply on every change.
- New `Degradation Curve` and `Eat Again At`.
- Vigor: every food gives a stamina regen bonus, shown on the tooltip.
- New `1. Health Regeneration` (continuous food healing), `2. Stamina Regeneration`, `3. Eitr Regeneration` (Eitr
  Vigor).
- New `4. Stamina Costs`, `5. Base Values`, `6. Skills`, `7. World Rates` and per-player `8. Display`.
- Per mead: regen multipliers and run and jump stamina modifiers.
- Every new setting defaults to vanilla, is synced and hot reloads.

## 3.3.5

- New store icon.

## 3.3.4

- Store page links to the GitHub repository.

## 3.3.3

- Rebuilt on the rewritten shared configuration libraries; no behaviour change.
- MIT licence added.

## 3.3.2

- Fixed: a frame rate loss.

## 3.3.1

- Fixed: a dedicated server hung in an endless config reload.
- Fixed: a long menu freeze with many modded consumables.

## 3.3.0

- Updated for the current Valheim version.
- Jotunn no longer required.
- Fixed: `Lock Configuration` now locks clients.
- Meads detected automatically and matched by status effect.
- Disable Food Degradation no longer rewrites game code.

## 3.2.0

- Fixed: disable food degradation.

## 3.1.1

- Fixed: the icon.

## 3.1.0

- Config changes apply without a restart.
- Values applied when consumed, for multiplayer.

## 3.0.0

- Renamed from ValheimFoodConfig to FeastMaster.

## 2.3.1

- Mead values configurable.

## 2.3.0

- Option to remove food degradation.

## 2.2.0

- Global food config modifiers.

## 2.1.0

- Fixed: incompatibility with a new Valheim version.

## 2.0.0

- New config file format.

## 1.1.1

- Eitr food values configurable.

## 1.1.0

- New config file format.

## 1.0.0

- Food values configurable.

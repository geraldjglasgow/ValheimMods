# Public API

User decision 2026-10-05: other mods integrate with EliteCrafting through a supported API, modelled on the shape of
Epic Loot's (one public static class, JSON in, version and endpoint checks, providers, filters, listeners); our own
mods (PackPanel, Elite Creatures Pack) use it for compatibility. Names and behaviour are ours.

## 1. Shape

- `EliteCrafting.Api.EliteCraftingApi`, a `public static class` in `EliteCrafting.dll` (ILRepack internalizes only the
  merged libraries, so it stays public).
- **No reference needed.** Other mods bind by reflection through `ValheimModLibs/EliteCraftingLink`, a library that our
  mods merge with ILRepack (and other authors may bundle the same way): typed wrappers that find the API on first use and
  do nothing (return false / null / 0) when EliteCrafting is absent or older. It also keeps the workspace rule that our
  mods never reference each other.
- **Versioning**: `const int ApiVersion = 1`, `GetApiVersion()`, `GetPluginVersion()`, `HasEndpoint(name)`,
  `GetEndpointNames()`. A new endpoint is added, never changed; a removed one stays as a no-op.
- **Definitions are JSON strings** (parsed with the merged YamlDotNet: JSON is YAML), using the same field names as the
  YAML format 2 (classes-and-tiers.md), so a mod can share one definition between a file and the API.
- **Every callback runs guarded**: an exception in another mod's classifier, provider, filter or listener is logged once
  per source with that mod's id and treated as "no answer"; it never breaks EliteCrafting.
- **Multiplayer**: registrations are code: every peer runs the same mods and registers the same things. Registered
  classes and inscriptions sit under the YAML layers, so a server's synced files still override them.

## 2. Item classes and levels

| Endpoint | What it does |
|---|---|
| `bool RegisterItemClass(string json)` | Adds or replaces a class (fields as the YAML `classes` entries: id, group, name, rolls, damage_scale, drop_weight, match, items). |
| `bool ClaimItems(string classId, string[] prefabs)` | Puts these prefabs in the class (step 1 of classification). |
| `bool RegisterClassifier(string id, Func<ItemDrop.ItemData, string> classify)` / `UnregisterClassifier(id)` | A callback that names a class id for items it knows, null for the rest (step 2). |
| `bool SetItemLevel(string prefab, int level)` | The item level (1-8) of a prefab, above the material derivation (like `item_tiers.items`). |
| `string GetItemClass(ItemDrop.ItemData item)` / `int GetItemLevel(item)` | The resolved class id (null: none) and level. |

## 3. Inscriptions and effects

| Endpoint | What it does |
|---|---|
| `bool RegisterInscription(string json)` | Adds an inscription (YAML format 2 fields: id, name, effect, param, value, affix, family, classes, tiers, ...). Its effect must be registered. |
| `bool AddToPool(string classId, string inscriptionId, bool bestFit)` | Lets an existing inscription roll on a class (best fit or allowed). |
| `bool RegisterExternalEffect(string json)` | An effect EliteCrafting rolls, displays and sums but does not apply: fields `id`, `scope` (player / item), `value` (percent / flat / flag, or a list of them), `polarity` (raise / lower), `cap`, `param` (none or a kind), optional `description`. The registering mod applies it by reading totals. |
| `float GetPlayerTotal(Player player, string effect, string param = null)` | The capped total of a player-global channel on that player (local player: live; others: 0 unless the effect publishes to the ZDO). |
| `float GetItemTotal(ItemDrop.ItemData item, string effect, string param = null)` | An item's own sum for an item-local effect. |

Registrations made after the rules loaded rebuild the rule set once at the end of the frame.

## 4. Items

| Endpoint | What it does |
|---|---|
| `bool IsMagic(item)`, `string GetRarity(item)`, `string GetRarityColor(item)` | Rarity id (`normal`, `magic`, `rare`) and its `#RRGGBB`. |
| `string GetInscriptionsJson(item)` | `[{ id, tier, value, affix, active }]`, tier as shown (1 strongest). |
| `string GetDecoratedName(item)` | The item name as the tooltip shows it, rarity colour included. |
| `bool CanBeMagic(item)` | A magic base under the current rules and filters. |
| `bool RollMagic(item, string rarity)` | A fresh roll at that rarity (the item must be a magic base); written through the item-state writer. |
| `bool Cleanse(item)` | Back to Normal. |

## 5. Hooks

| Endpoint | What it does |
|---|---|
| `bool RegisterEquipmentProvider(string id, Func<Player, List<ItemDrop.ItemData>> extraEquipped)` / `Unregister...` | Items a player wears outside the game's equipment slots (PackPanel's backpack slot). Their inscriptions count like equipped items. |
| `void InvalidatePlayer(Player player)` | Rebuild that player's effect totals (call after the provider's answer changed): the local player's on the next frame, at most once per frame, like every other rebuild trigger; other players have no totals on this peer. |
| `bool RegisterMagicBaseFilter(string id, Func<ItemDrop.ItemData, bool> allow)` / `Unregister...` | A veto: false keeps an item from ever becoming magic (drops, runes, the API). |
| `bool AddItemChangedListener(Action<ItemDrop.ItemData, string> listener)` / `Remove...` | After every write of our item state; reason `rune:<id>`, `drop`, `command`, `api`, `migration`. |
| `bool AddLootGeneratedListener(Action<ItemDrop.ItemData> listener)` / `Remove...` | After a pre-rolled magic item is made for a drop. |
| `bool SetCreatureLoot(string json)` | A creature's loot profile, the same fields as `drops.creatures` and `drops.bosses` entries (prefab, tier, multiplier, rune_multiplier, rune_rolls, gear_rolls, bonus, boss), for modded creatures. |

## 6. Our mods

- **PackPanel** registers the class `backpack` (its eight packs, claimed by prefab, item levels by material: Deerhide 1 ...
  Moosehide 8), an equipment provider for the worn pack, its own inscriptions through external effects (extra slots),
  pools for the pack class (carry weight and the pack's own lines), and draws our rarity colour behind magic items in
  its slots.
- **Elite Creatures Pack** sets the item levels of its weapons and shield and loot profiles for its creatures and bosses,
  so they drop runes and rolled gear at the right tier.

## 7. As built (2026-10-05)

The API is `Api/` (code map in `CLAUDE.md`, "API") and the link is `ValheimModLibs/EliteCraftingLink`, whose wrappers
are split by section: `CraftingLink` (Present, versions, endpoints, `Guid`), `CraftingClasses`, `CraftingInscriptions`,
`CraftingItems`, `CraftingHooks`. Where the build settles what the tables above leave open, or differs from them:

- **Load order.** A caller must load after EliteCrafting (`[BepInDependency("com.EliteCrafting", SoftDependency)]`)
  and register in its Awake. The first rules load (in our Awake) reads everything registered before it; anything
  registered after rebuilds once at the end of that frame. A user file that tunes a registered inscription is rejected at
  our first load (the inscription is not there yet) and applied by that rebuild, which builds the family again from the
  texts it was last given (this machine's files, or the server's on a bound player).
- **Inscriptions.** The JSON must be a complete, valid entry on its own (the YAML parser checks it, its effect must be
  registered). It joins a layer under the built-in defaults and the files: a file entry with the same id changes only the
  fields it names. An id the YAML already defines is accepted with a warning (the YAML's fields win); use an id of your
  own (`packpanel_...`). A later registration with the same id replaces the earlier one.
- **Pools.** `AddToPool` is applied after the merge, so it holds whatever the files say about the inscription's classes
  (best wins over allowed); a file can still disable the inscription or set its weight to 0, not remove the class. An
  inscription id that does not exist (yet) is a warning at each build and is ignored.
- **External effects** may be registered at any time, also after the rules loaded (the effect registry otherwise closes
  then); re-registering an external id replaces it, one of EliteCrafting's own ids is refused. `scope` also takes
  `player_global` / `item_local`; `cap` absent or null means uncapped. They are no-ops in our aggregate and item hooks.
- **Totals.** `GetPlayerTotal` is 0 for an item-scope effect. For another player only the effects that publish on the
  player's ZDO answer (the loot-find effects and the shared stats of effects-runtime.md section 7), and only without a
  param; external effects never publish. `GetItemTotal` works for any effect (the item's own active inscriptions of that
  effect and param, each channel capped on the item); a health-critical channel counts only while the local player is
  critical. Both are 0 while `Affix effects` is off.
- **Item levels and loot** sit under the YAML whole, not field by field: an `item_tiers.items` entry for the prefab wins
  over `SetItemLevel` (`ecraft tiers` shows the API's levels as source `api`), and a `drops.bosses` or `drops.creatures`
  entry for the prefab wins over that kind of `SetCreatureLoot` entry. `SetCreatureLoot` also takes `gear_multiplier`;
  a boss (`"boss": true`) gets a boss entry from `tier`, `rune_rolls`, `gear_rolls`, `bonus` and, when a multiplier is
  given, a creature entry with only the multipliers; anything else gets a creature entry (`rune_rolls`/`gear_rolls`
  warned and ignored). Bonus runes must be built-in rune ids. A later call for the prefab replaces both entries.
- **Items.** `GetRarity` is `normal` for a plain item and an unknown stored id as stored; `GetRarityColor` is white for
  an unknown rarity. `GetInscriptionsJson` gives an orphaned inscription (id no longer defined) tier 0 and affix null.
  `GetDecoratedName` follows the viewing player's `Colored item names` setting, like the tooltip. `RollMagic` and
  `Cleanse` refuse sealed items and items written by a newer EliteCrafting; `Cleanse` of a plain item answers true and
  writes nothing. Both write on the caller's peer: call them where the item is owned, as a rune would be.
- **Listeners.** The reason `other` is given for a write outside every reason scope (none today). `migration` is
  reserved: format migrations happen in memory and are stored by the next write of another reason. The workbench
  upgrade copies an item's data onto the new item object without a write, so it raises nothing. Loot-generated
  listeners run for creature drops and world containers, on the peer that rolls them, before the item reaches the world.
- **Providers and filters.** Providers are asked for the local player on every effects rebuild and every item-state
  write, so their answer must be cheap; their items also count for the loot-find stats. Filters are asked wherever
  "magic base" is decided (drops ask with the prefab's item data, so decide from the item type); registering or
  removing one refreshes the rules at the end of the frame so the gear pool follows.
- **Endpoint names.** `HasEndpoint` and `GetEndpointNames` read the API class's public static methods (34 in version 1).

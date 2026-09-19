# CLAUDE.md - OpenKeep

Storage and inventory for Valheim, version 1.1.1: crafting, building and station feeding from nearby containers
(Reach), stow, top up, sort, junk, trash, routing, chest cycling and ground pickup (Stow), a Salvage tab, stack
sizes and weights (Stacks), container sizes and hover contents (Capacity), carts as workbenches (Carts), several
players in one chest (Shared), and a contents sign above every player-built container (Signs). Written black-box
from `SPEC.md` (deleted after the in-game verification) and the game code alone, by module agents following
`PLAN.md`; the rules are under "Developing mods" in `../CLAUDE.md`.
This file is the code map, the patched methods, the decisions the spec left open, and the in-game test checklist.

Rules that still apply to every change:

- Clean room. No other mod's source, DLL, config or documentation is read; only this workspace, the shared
  libraries and the game's own decompiled assembly. No web search for how another mod does it. Own names
  everywhere: GUID `milkyteam.openkeep`, config `milkyteam.openkeep.cfg`, YAML `OpenKeep.<Module>.yml`, words
  `$ok_*`, command `openkeep`, custom data `OpenKeep.<name>`, ZDO keys and RPC names `OpenKeep.<name>` /
  `OpenKeep_<Name>`, Charter article names `openkeep_<module>`. No compatibility layer for any other mod's files.
- Verify every patched signature against a fresh `ilspycmd` decompile of the game's own assemblies
  (`assembly_valheim.dll`, `assembly_utils.dll`, `assembly_guiutils.dll`) in the scratchpad, never in a repository,
  and read the method body so the patch fits ownership, RPCs and save paths.
- Functions at most 24 lines from brace to brace (lambdas and local functions count), classes at most 300 lines,
  one responsibility per class, one class per file unless a patch class belongs to a small helper. Prefix, postfix
  and finalizer patches only; no transpilers. Every patch class is applied on its own in `Plugin.PatchEverything`,
  so one failure is logged and the others still apply.
- Settings are read at use time (`ConfigEntry.Value`), never cached. Every gameplay setting is bound synced and
  covered by `Lock Configuration`; hotkeys, display formats, confirmations, sort orders and colours are bound with
  `synced: false`.
- Container access is the game's: `ContainerScan.IsUsable` runs the privacy check, the ward check, the in-use rule
  and the section 0 switches for every candidate; nothing changes a container's inventory without
  `ContainerScan.Claim` before and `ContainerScan.Save` after, and a chest another player is using is changed only
  through the Shared module's requests to its owner (`ChestWriter`), never locally.
- Nullable is off. Zero compiler warnings from the mod's own code. Build from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build OpenKeep/OpenKeep/OpenKeep.csproj -c Release`; the build copies the
  merged DLL to `dist/` and to the r2modman profile `LocalTesting`. Never launch or kill the game from a script.

## Layout

```
OpenKeep/OpenKeep/src/
  Plugin.cs                 entry: Lock Configuration, module Initialize calls, patches per class, Synced.Finish,
                            Guard.Install last; the YAML editor's Update and OnGUI
  Core/                     shared by every module
    CoreModule.cs, CoreSettings.cs, SharedMode.cs   section 0. Containers (Ships, Carts, Player Chests, Honour Wards,
                            Shared Chests: Off, View, Full)
    ContainerScan.cs        loaded containers (Container.Awake postfix), Nearby, IsUsable, IsShared, InUseByAnother,
                            Claim, Save, PrefabName, IsShip, IsCart, IsPrivateChest
    ContainerRules.cs       per prefab enabled table, filled by Reach's YAML, read by every module
    ContainerUse.cs         Reach or Stow (both share the section 0 rules today)
    ItemMatcher.cs, ItemMatchSet.cs, ItemGroups.cs   the item vocabulary and the groups: map of a YAML file
    ItemNames.cs            prefab name, display name, stacking identity of an item
    CharacterData.cs        string sets and flags in Player.m_customData under OpenKeep.<key>
    Keys.cs                 KeyboardShortcut helpers, InventoryOpen, TextInputActive
    RenamedKeys.cs          Carry: a renamed cfg key takes the old line's value from BepInEx's orphaned entries
    Messages.cs, Language.cs   centre and top-left messages; $ok_ words (Localization.SetupLanguage postfix)
    Command.cs              the openkeep console command (Terminal.InitTerminal postfix)
  Reach/                    section 1
    ReachModule.cs, ReachSettings.cs, ReachMode.cs, RequirementDisplay.cs
    ReachModel.cs, ContainerRule.cs, ReachRules.cs   OpenKeep.Reach*.yml, per prefab rule, gate and ranges
    ReachCount.cs           reachable containers (one list per frame), counting per stack with allow/deny
    ReachPayment.cs         the payment window (ConsumeResources, DoCrafting) and the RemoveItem prefix
    ReachPull.cs            moving items from containers into the inventory, never in two places
    Requirements.cs         the game's requirement filter (upgrader resources, missing items)
    RecipeRequirementPatch.cs, PieceRequirementPatch.cs, FirstRequiredItemPatch.cs   counting
    RequirementRows.cs, ReachFlash.cs   the requirement rows (SetupRequirement postfix) and the flash after a pull
    CraftPullPatch.cs       Pull modifier + Craft
    StationFeed.cs, StationAccepts.cs, StationHover.cs   borrow one unit, Fill, Pull, "From storage" hover lines
    SmelterOrePatch.cs, SmelterFuelPatch.cs, CookingFoodPatch.cs, CookingFuelPatch.cs, FireplacePatch.cs,
    FermenterPatch.cs       one prefix/postfix pair per station entry point
    ReachKeys.cs            Toggle Key and Link Key (Player.Update postfix)
    ReachLinks.cs           link lines (LineRenderer) and the PlacePiece postfix
  Stow/                     section 2
    StowModule.cs, StowSettings.cs, StowWords.cs, SortOrder.cs
    StowModel.cs, StowRule.cs, StowRules.cs   OpenKeep.Stow*.yml: groups, pickup, accept, refuse
    StowTargets.cs          the open container and the nearby ones: usable now or shared (Full mode)
    ChestBatch.cs           one action's writes to one container through Shared.ChestWriter, with the summary
    StackMover.cs           stacks with a put under way (never moved twice), the end-of-action message
    StowActions.cs          quick stack, stow all, dump
    TopUp.cs, Sorting.cs, Trash.cs, Routing.cs, Finder.cs, Cycling.cs
    Favourites.cs, Movable.cs   favourite items, favourite slots, junk marks; what may move
    StowHotkeys.cs          InventoryGui.Update postfix: the hotkeys and cycling
    PanelButtons.cs         InventoryGui.Awake postfix: the button row and the container Sort button
    HoveredItem.cs          the slot under the pointer (or the gamepad selection)
    ClickRouting.cs         InventoryGui.OnSelectedItem prefix: Route Modifier + click
    DumpKeyPatch.cs         Player.Update postfix: Dump Key outside the inventory
    AutoSortPatch.cs        InventoryGui.Show prefix/postfix
    FavouriteOverlay.cs, SlotMarks.cs, StowSprites.cs   star, cross, border marks on the grid elements
    LinkMarker.cs           Find Key: a line from the player and a floating count
    GroundPickup.cs         Container.CheckForChanges postfix
  Salvage/                  section 3
    SalvageModule.cs, SalvageSettings.cs, SalvageWords.cs, RoundingMode.cs
    SalvageModel.cs, FractionOverride.cs, SalvageRules.cs   OpenKeep.Salvage*.yml, recipe lookup, blockers
    SalvageReturns.cs, SalvageReturn.cs   what a stack returns
    SalvageInventory.cs     the exact fit simulation and the add with rollback
    SalvageActions.cs       the public face: CanSalvage, WhyNot, Returns, Salvage, Confirm
    SalvageTab.cs, SalvageList.cs, SalvageRow.cs, SalvagePanel.cs, SalvageGuiPatches.cs   the third tab
    SalvageHotkey.cs        InventoryGui.Update postfix: Salvage Key
  Stacks/                   section 4
    StacksModule.cs, StacksSettings.cs, StacksModel.cs, StackRule.cs, ItemValue.cs
    StackValues.cs          writes stack and weight into the prefabs and every live item (ItemCopies for the world
                            drops, inventory and open container, and new copies via Copies.HookSpawns)
    VanillaValues.cs, LiveItems.cs (the other loaded containers), PerItemEntries.cs
    DatabaseReady.cs        ObjectDB.Awake / CopyOtherDB postfixes
    MergeIntoChests.cs      InventoryGrid.DropItem prefix (skips a viewed or shared chest)
    TeleportPatch.cs        Inventory.IsTeleportable prefix
    Documentation.cs        OpenKeep.Items.txt and OpenKeep.Containers.txt (ZNetScene.Awake postfix)
  Capacity/                 section 5
    CapacityModule.cs, CapacitySettings.cs, HoverFill.cs, ContainersModel.cs, ContainerSize.cs
    ContainerPrefabs.cs, VanillaSizes.cs, ContainerSizes.cs   prefab discovery, vanilla sizes, apply and resize
    ContainerTemplate.cs    fills a still-default OpenKeep.Containers.yml with every container prefab
    SceneReady.cs           ZNetScene.Awake and Container.Awake postfixes
    HoverText.cs            Container.GetHoverText postfix
  Carts/                    section 6
    CartsModule.cs, CartsSettings.cs
    CartStation.cs          the CraftingStation component on every loaded cart (Vagon.Awake postfix)
    CartLevelPatch.cs, CartInteractPatch.cs, CartHoverPatch.cs
  Signs/                    section 7 (SPEC-Signs.md section 10)
    SignsModule.cs, SignsSettings.cs   section 7. Signs, the OpenKeep.Signs*.yml set, the $ok_signs_ words
    SignsModel.cs, SignRule.cs, SignRules.cs   per prefab enabled, offset, rotation; Allows (module, prefab, no
                            ship or cart, placed by a player)
    SignLinks.cs            the ZDO keys OpenKeep.sign, OpenKeep.signOf, OpenKeep.autoText, OpenKeep.noSign; Claim
    SignText.cs             the words: localized names, counts, Max Items, Max Characters, the ellipsis, Empty Text
    SignWriter.cs           text, empty author, autoText into the sign's ZDO; MayWrite (a player's words win)
    SignPose.cs, SignPlacement.cs   the sign's place from the container's renderer bounds, Height, Rotation, YAML
    SignPlacer.cs           Object.Instantiate of the sign prefab, creator, link, first text; Replace keeps the words
    SignRemover.cs          ZNetScene.Destroy when loaded, ZDOMan.DestroyZDO when not
    SignState.cs, SignStates.cs   per container throttle, revision and rules generation
    SignRefresh.cs          Container.CheckForChanges postfix: the owner's tick; RewriteAll, RulesChanged
    SignOptOut.cs           WearNTear.Remove postfix: OpenKeep.noSign on the container (the hammer)
    SignWear.cs             m_noSupportWear and m_noRoofWear cleared on every automatic sign instance
    ContainerGonePatch.cs   Container.OnDestroyed postfix: the owner removes the sign
    SignOrphanCheck.cs      Sign.Awake postfix adds the component and switches wear off; a sign without its
                            container is destroyed
    SignsCommand.cs         openkeep signs, signs reset, signs rewrite (called from Core's Command by reflection)
  Shared/                   section 9
    SharedModule.cs, SharedSettings.cs, SharedWords.cs   section 9. Shared, the $ok_shared_ words
    SharedState.cs          the viewed container, the mode, the user name (ZDO OpenKeep.user), the panel title
    UserNamePatch.cs        Container.SetInUse postfix: the owner writes and clears OpenKeep.user
    ViewOpenPatch.cs        Container.Interact prefix (the read-only open), Container.RPC_OpenResponse prefix
    ViewerPanel.cs          InventoryGui.UpdateContainer prefix (keeps the viewer's panel alive, re-requests the
                            game's open), InventoryGui.Hide and CloseContainer postfixes (end of viewing)
    PanelRouting.cs         the game's panel actions on a viewed chest: OnSelectedItem, OnRightClickItem,
                            InventoryGrid.DropItem, OnTakeAll, OnStackAll prefixes (refuse in View, request in Full)
    ChestWriter.cs          the one writer of 9.3: Take, Put, Move, TakeAll, StackAll; CanWriteNow, IsShared
    ChestSync.cs, ChestOps.cs, InventoryFit.cs   the synchronous path; the inventory operations both sides use;
                            the dry run of Inventory.AddItem
    ChestRequests.cs        the RPC names, headers, reasons, registration (Container.Awake postfix), Send, Reply
    ChestRequester.cs       pending requests, replies, the one retry, timeouts (Player.Update postfix)
    ChestAsk.cs             builds each request and applies the owner's answer on the requester
    ChestOwnerHandler.cs    the referee: validates, applies with the game's methods, saves, replies
    ItemPacket.cs           one item on the wire (prefab name + the game's ItemData.Save fields)
    Touches.cs              OpenKeep_Touch: InventoryGrid.OnLeftDown, InventoryGui.Update, InventoryGrid.UpdateGui
OpenKeep/OpenKeep/config/   embedded default YAML files: OpenKeep.Reach.yml, OpenKeep.Stow.yml,
                            OpenKeep.Salvage.yml, OpenKeep.Stacks.yml, OpenKeep.Containers.yml, OpenKeep.Signs.yml
```

Startup order in `Plugin.Awake`: `Synced.BindLocking` (General / Lock Configuration), then
`CoreModule.Initialize`, `ReachModule.Initialize`, `StowModule.Initialize`, `SalvageModule.Initialize`,
`StacksModule.Initialize`, `CapacityModule.Initialize`, `CartsModule.Initialize`, `SignsModule.Initialize`,
`SharedModule.Initialize` last (the spec's order; each binds its settings, registers its YAML set and its words), every patch class on its
own, `Synced.Finish`, the `Loading [OpenKeep 1.1.1]` line, `Guard.Install` last.

Cross-module uses that are allowed: Stow's `Trash` calls `Salvage.SalvageActions` (Trash Uses Salvage), Stacks'
`Documentation` calls `Capacity.ContainerPrefabs` and `Capacity.VanillaSizes` (OpenKeep.Containers.txt), Stow's
`Finder` reads Reach's `Link Seconds` through `ConfigDefinition("1. Reach", "Link Seconds")`, Core's `Command`
reaches `Stacks.Documentation.Write` and `Signs.SignsCommand.Run` by reflection. Stow's `ChestBatch`, `Routing`, `Trash`, `Sorting` and
`StowTargets` and Stacks' `MergeIntoChests` call `Shared.ChestWriter`, `Shared.SharedState` and
`Shared.SharedWords`; Shared's `PanelRouting` reads Stow's `Enabled` and `Route Modifier` through
`ConfigDefinition("2. Stow", ...)`. Everything else goes through `Core`.

## Patched game methods

Postfix: `Container.Awake` (Core tracking; Capacity sizes; Shared RPC registration), `Container.CheckForChanges`
(ground pickup; the sign refresh tick), `Container.GetHoverText`, `Container.OnDestroyed` (the owner removes the
container's sign), `Container.SetInUse(bool)` (the user name), `CookingStation.GetHoverText`,
`CraftingStation.GetLevel(bool)`, `Fermenter.GetHoverText`, `Fireplace.GetHoverText`,
`InventoryGrid.OnLeftDown(UIInputHandler)` (touches), `InventoryGrid.UpdateGui(Player, ItemData)` (marks; touch
tint), `InventoryGui.Awake` (Stow buttons; Salvage tab), `InventoryGui.CloseContainer` and `InventoryGui.Hide` (end
of viewing), `InventoryGui.SetupRequirement` (static, six parameters), `InventoryGui.Update` (Stow hotkeys; Salvage
Key; touch end), `InventoryGui.UpdateRecipe(Player, float)`, `Localization.SetupLanguage`, `ObjectDB.Awake`, `Sign.Awake` (the
orphan check component on automatic signs; their `WearNTear` wear switched off),
`ObjectDB.CopyOtherDB` (both `Priority.Low`), `Player.GetFirstRequiredItem`, `Player.HaveRequirementItems`,
`Player.HaveRequirements(Piece, RequirementMode)`, `Player.PlacePiece`, `Player.Update` (Reach keys; Dump Key;
request timeouts), `Switch.GetHoverText`, `Terminal.InitTerminal`, `Vagon.Awake`, `Vagon.GetHoverText`, `WearNTear.Remove(bool)`
(the hammer opt-out on the removing player's client), `ZNetScene.Awake` (documentation; container list and sizes,
`Priority.Low`).
Prefix: `Container.Interact(Humanoid, bool, bool)` (the read-only open), `Container.RPC_OpenResponse(long, bool)`
(a refusal is silent while viewing), `InventoryGrid.DropItem(Inventory, ItemData, int, Vector2i)` (Shared,
`Priority.First`, zeroes the amount for a viewed chest; Merge Into Chests), `InventoryGui.OnCraftPressed` (Pull
modifier; Salvage tab), `InventoryGui.OnRightClickItem(InventoryGrid, ItemData)` (refused on a viewed chest),
`InventoryGui.OnSelectedItem` (Shared, `Priority.First`; Stow's Route Modifier), `InventoryGui.OnStackAll`,
`InventoryGui.OnTakeAll`, `InventoryGui.OnTabCraftPressed`, `InventoryGui.OnTabUpgradePressed`,
`InventoryGui.UpdateContainer(Player)` (the viewer's panel), `InventoryGui.UpdateRecipeGamepadInput`,
`Inventory.IsTeleportable(bool)`, `Inventory.RemoveItem(string, int, int, bool)` (the payment hook),
`Vagon.Interact`.
Prefix and postfix: `CookingStation.OnAddFuelSwitch`, `CookingStation.OnInteract(Humanoid)`,
`Fermenter.Interact`, `Fireplace.Interact`, `InventoryGui.Show(Container, int)` (auto sort),
`InventoryGui.UpdateCraftingPanel(bool)`, `Smelter.OnAddFuel`, `Smelter.OnAddOre`.
Prefix and finalizer (the payment window): `InventoryGui.DoCrafting`, `Player.ConsumeResources`.

## Config sections and keys

`General` (`Lock Configuration`), `0. Containers` (`Ships`, `Carts`, `Player Chests`, `Honour Wards`, `Shared
Chests`: the enum `Off`, `View`, `Full`, default `Off`), `1. Reach` (`Enabled`, `Range`, `Crafting`, `Building`,
`Upgrading`, `Feed Stations`; unsynced `Fill Modifier`, `Pull Modifier`, `Toggle Key`, `Show Links`, `Link Key`,
`Link Seconds`, `Requirement Display`, `Storage Colour`, `Flash On Pull`), `2. Stow` (`Enabled`, `Quick Stack
Nearby`, `Nearby Range`, `Ground Pickup`, `Pickup Range`, `Pickup Interval`, `Pickup Delay`, `Pickup Only Held
Items`; unsynced every key, `Sort Order`, `Auto Sort Containers`, `Auto Sort Inventory`, `Confirm Trash`, `Trash
Uses Salvage`, `Cycle With Wheel`, `Show Favourites`, `Button Row Offset`), `3. Salvage` (`Enabled`, `Return Fraction`, `Rounding`, `At
Least One`, `Upgrade Materials`, `Require Known Recipe`, `Require Station`; unsynced `Salvage Key`), `4. Stacks`
(`Enabled`, `Stack Multiplier`, `Weight Multiplier`, `Ignore Teleport Restriction`, `Merge Into Chests`, `Per Item
Config Entries`, `Write Documentation`), `4a. Item Stacks` and `4b. Item Weights` (`<prefab>.Stack`,
`<prefab>.Weight`, only with Per Item Config Entries), `5. Capacity` (`Enabled`; unsynced `Hover Contents`, `Hover
Lines`, `Hover Fill`), `6. Carts` (`Cart Workbench`, `Cart Station Level`, `Cart Station Range`), `7. Signs`
(`Enabled` false, `Show Counts` false, `Max Items` 4, `Max Characters` 50, `Update Seconds` 2, `Height` 0.1,
`Rotation` 0, `Empty Text` empty; all synced), `9. Shared` (`Request Timeout` 2 s, `Touch Seconds` 5 s, both
synced; unsynced `Show Touches` true, `Touch Colour` `#ffb347`).
Keys, defaults and meanings are in `README.md`. Every setting of the spec is bound with the spec's section, key,
default and sync flag; the one addition is `2. Stow / Enabled` (synced, true), so every module has a master switch.

## Network and file names

- ZDO keys: `OpenKeep.user` (string): the name of the player using a container, written by the owning client in
  the `Container.SetInUse` postfix when the container is taken into use and cleared when it is released (compared
  before writing, so the ZDO changes only when the name changes). Read only: the game's `InUse`, `spawntime`,
  `fuel`. `OpenKeep.cartOffset` is reserved for the unbuilt cart extension piece. Signs: on a container
  `OpenKeep.sign` (ZDOID, its sign) and `OpenKeep.noSign` (bool, the hammer opt-out); on a sign `OpenKeep.signOf`
  (ZDOID, its container) and `OpenKeep.autoText` (string, what the mod last wrote); the sign's text goes into the
  game's own `text` key with `author` and `authorPlatformDisplayName` set to empty strings, and the piece's
  `creator` long is the local player's id.
- RPCs, registered on every container's net view in a `Container.Awake` postfix (once per view), every payload one
  `ZPackage`. A request starts with a header: request id (long), the requester's player id (long) and name
  (string); it goes to the ZDO's owner at send time (`ZNetView.InvokeRPC(name, pkg)`).
  `OpenKeep_Take` (slot `Vector2i`, expected item name, expected stack, amount); `OpenKeep_Put` (item packet,
  has-slot bool, slot); `OpenKeep_Move` (from slot, to slot, amount, expected item name); `OpenKeep_TakeAll`
  (count, then per entry slot, item name, amount); `OpenKeep_StackAll` (count, then per entry index, item packet).
  `OpenKeep_Reply` goes back to the requesting peer: request id, ok (bool), reason word (`not owner`, `denied`,
  `chestfull`, `nothing`, `bad`; `timeout` is local), a nested package with the result: Take an item packet, Put
  the accepted count, Move nothing, TakeAll count plus item packets, StackAll count plus (index, accepted) pairs.
  `OpenKeep_Touch` (slot, player name, on bool, forwarded bool): sent to the owner, which forwards it to everybody
  with `forwarded` set.
- Item packet: the prefab name (empty when the item has no prefab; the reader then returns null), followed by the
  game's own `ItemDrop.ItemData.Save` fields: durability x100, grid x, grid y, world level, a flag byte (picked
  up, equipped, quality, stack, variant, crafter, prefab, custom data), then only the flagged ones: quality,
  stack, variant, crafter id and name, prefab hash, custom data, and the cheated flag. The reader clones the
  prefab's item and runs `ItemDrop.ItemData.Load` on it, as `Inventory.Load` does.
- Player custom data: `OpenKeep.reachOff` (flag), `OpenKeep.favouriteItems`, `OpenKeep.favouriteSlots` (`x:y`),
  `OpenKeep.junk` (comma separated sets, keyed by prefab name).
- Charter article names: `openkeep_reach`, `openkeep_stow`, `openkeep_salvage`, `openkeep_stacks`, `openkeep_containers`,
  `openkeep_signs` (the YAML sets) plus the cfg sync of the shared libraries. Container ownership goes through the game's
  `ZNetView.ClaimOwnership` and `ZDOMan.ForceSendZDO`; the game's own `RPC_RequestOpen` is re-sent by a viewer.
- GameObjects created: `OpenKeep.ReachLink` (link lines), `OpenKeep_link` (Find marker), `OpenKeep_SalvageTab`,
  `OpenKeep_<word>` and `OpenKeep_trash` (panel buttons), `OpenKeep_border`, `OpenKeep_star`, `OpenKeep_cross`
  (slot marks), sprites named `OpenKeep_sprite`. The cart's station is a `CraftingStation` component on the cart
  instance, no new prefab. Shared creates none: touches recolour the grid's icons. Signs instantiates the game's
  own `sign` prefab (a normal piece, no new prefab) and adds a `SignOrphanCheck` component to loaded automatic signs.
- Files next to the cfg: the six YAML files, `OpenKeep.Items.txt`, `OpenKeep.Containers.txt`.
- Localization keys: `$ok_*` (Reach: `ok_fromstorage`, `ok_reach`, `ok_on`, `ok_off`, `ok_pulled`,
  `ok_nothingtopull`, `ok_nofit`; Stow: `ok_stow_*`, including `ok_stow_moved_to` and `ok_stow_toppedup_from` for
  a shared chest's reply; Salvage: `ok_salvage*`; Capacity: `ok_slots`, `ok_full`, `ok_and`, `ok_more`; Carts:
  `ok_cartcraft`; Shared: `ok_shared_inuse`, `ok_shared_moving`, `ok_shared_noanswer`, `ok_shared_denied`,
  `ok_shared_readonly`, `ok_shared_someone`, `ok_shared_nofit`, `ok_shared_chestfull`, `ok_shared_unavailable`;
  Signs: `ok_signs_sign`, `ok_signs_playertext`, `ok_signs_nosign`, `ok_signs_optedout`, `ok_signs_reset`,
  `ok_signs_rewritten`, console output only; the sign text itself is plain text).
- Console: `openkeep reload`, `openkeep containers`, `openkeep write docs`, `openkeep signs`, `openkeep signs reset`,
  `openkeep signs rewrite`.

## Decisions where the spec was silent

### Core

- Containers are found by component, never by a name list: every object with the game's `Container` component
  whose net view has a ZDO is tracked in the `Container.Awake` postfix (the game creates the inventory in exactly
  that case). The net view is the one the game uses: `m_rootObjectOverride` when set (ships and carts keep their
  storage on a child object and the ZNetView on the root), else the container's own. `PrefabName` is the net
  view object's prefab name, so ship storage is `VikingShip`, cart storage `Cart`, and a modded ship or cart is
  addressed by its own prefab name; `IsShip` is a `Ship` component on the object or a parent, `IsCart` the
  container's `m_wagon` or a `Vagon` on a parent. The only hard-coded prefab names are `piece_chest_private`
  (the Player Chests switch) and `piece_workbench` (the cart station template).
- `Container` has no `OnDestroy`; destroyed containers are pruned lazily in `All()`.
- "Inventory read from the ZDO at least once" is the game's `m_lastRevision != uint.MaxValue`.
- The in-use rule mirrors `RPC_RequestOpen`: the container the local client owns is usable (only the owner can
  have it open), another client's container is refused while its ZDO `InUse` is 1 or its cart is in use
  (`InUseByAnother`). A ship that someone sails is not in use; only an open hold is. The owner is the referee
  (section 9): `IsUsable` refuses a chest another player is using in every `Shared Chests` mode, so every
  synchronous path (Reach's payment and station feeding, ground pickup, `openkeep containers`, the sort) never
  sees such a chest and nothing is ever applied speculatively; `Claim` refuses a chest in use as well, since the
  game never hands such a chest over. `IsShared` is the other side: `Shared Chests` is `Full`, another player uses
  the chest, and every other usability rule passes; only `Shared.ChestWriter` may change it, by request.
- `Claim` claims ownership, force-sends the ZDO to the previous owner as the game does after a granted open, then
  calls `Container.Load` so the latest data is read (a no-op when the data revision has not changed; ownership
  changes only the owner revision). `Save` calls `Inventory.Changed()`, the game's own path to the ZDO, and logs a
  warning when the client does not own the container.
- A private chest without a `Piece` is unusable (the game's privacy check would need the piece's creator).
- The ward check never flashes the ward. `ContainerUse` is accepted but does not distinguish rules yet.
- Vocabulary: an unknown keyword (`foo:`) or an unknown `type:` name is an invalid entry with `Problem` set; it
  matches nothing and the YAML model warns. `ItemGroups` warns about invalid members and members naming unknown
  groups; cycles are tolerated through a visited set.
- `Keys`: a shortcut with modifiers uses BepInEx's `IsDown`; a single key fires only with no Shift, Ctrl or Alt
  held, so `F` and `LeftShift + F` never fire together. `TextInputActive` covers chat, console, the sign text input,
  any selected input field and the YAML editor. `CharacterData` strips commas from set members.
- `openkeep reload` is allowed with no `ZNet` or when the local player is admin or host; it runs
  `Config.Reload`, `Yaml.LoadAll`, `Yaml.ApplyAll`. `openkeep containers` lists within 20 m.

### Reach

- Payment: both game pay paths end in `Inventory.RemoveItem(name, amount, quality, worldLevel)` on the player
  inventory (`Player.ConsumeResources`, and `InventoryGui.DoCrafting` directly for single ingredient recipes), and
  both run after the crafted item or the piece exists. `ConsumeResources` and `DoCrafting` open a depth-counted
  payment window (mode Building for quality 0, Crafting for 1, Upgrading above; `DoCrafting` sets Crafting or
  Upgrading from `m_craftUpgradeItem`); inside it the `RemoveItem` prefix computes the shortfall against the same
  name, quality and world-level filter the game removes with, takes exactly that from reachable containers nearest
  first (claim, remove per stack honouring allow/deny, save), and the game then removes what the inventory has.
  Finalizers close the window even after an exception. A container that vanished since counting leaves the
  shortfall paid short and logged; the player gains, never loses. The build panel passes quality 0 to
  `SetupRequirement`, so its rows use the Building switch. Reachable means `ContainerScan.IsUsable`: a chest another
  player is using is neither counted nor paid from, in every `Shared Chests` mode.
- Single ingredient recipes: `GetFirstRequiredItem` returns the inventory's stack, else the container's stack;
  the game reads only its name and quality (`Recipe.GetAmount`, `DoCrafting`).
- Rows: Split shows `min(have, need) + storage part`, Total shows the need in the storage colour, rows stay red
  when inventory plus storage is short. The flash after a pull is a 0.6 s sine pulse read by the row postfix,
  because the game rebuilds the rows every frame.
- Stations: a prefix on each add entry point borrows one unit from the nearest container into the inventory when
  the hand is empty and nothing acceptable is carried; the game's own method consumes it (caps, messages, RPCs);
  the postfix returns the unit to a container if the game did not consume it. Fill claims the station's ZDO and
  re-invokes the game's method up to the cap; a nested Fill is prevented by a flag. Pull moves up to one stack.
  A fire that can be turned off toggles on plain Use, so Fill only repeats there through the game's alt use
  (Shift by default, the same key as the default Fill Modifier); with a rebound Fill Modifier such a fire is not
  filled rather than toggled repeatedly.
- `Fermenter` feeding requires the ward check and the game's roof and shelter conditions before borrowing.
- Links go to every `CraftingStation`, `Smelter`, `CookingStation`, `Fermenter` and `Fireplace` (torches included)
  within the container's range, lifted 0.6 m, width 0.05, material `Sprites/Default` with fallbacks to
  `Particles/Standard Unlit` and the cart's rope material. `PlacePiece` matches the placed container within 1.5 m.
- YAML: `range:` is shipped commented out; a container entry with no value warns; `containers:` keys are trimmed,
  case-insensitive.
- Toggling with the Toggle Key applies at once (the flag is read at use time).

### Stow

- `Enabled` (synced, true) was added as the module's master switch.
- Favourite slots are stored as `x:y` because the sets are comma separated.
- Sorting never touches the hotbar row (row 0) of the player inventory; favourite slots, equipped items and
  stacks with a put under way are pinned; containers pin nothing. Stacks of the same item, quality and world level
  merge while sorting.
- Dump Key requires `Quick Stack Nearby`; with it off the centre message says so.
- Favourite items are refused by Store one and Route; Top up still refills them.
- Trash from the container grid is allowed (the container is claimed and saved). `Trash Uses Salvage` applies only
  to a whole stack of the player inventory.
- Route Modifier + click on the player grid replaces the game's own modifier click (which moves the stack to the
  open container); the open container leads the candidates, so when it holds the item the stack still goes there.
  Clicks with a dragged item, on the container grid, or with the game's drop modifier stay the game's.
- Cycling is limited to `min(Nearby Range, InventoryGui.m_autoCloseDistance)` (4 m) because the panel closes any
  container farther away; the ring is sorted by `atan2` around the player; the wheel has a 0.25 s cooldown and
  only counts over the container grid. Opening goes through `Container.Interact` (the game's RPC path).
- Ground pickup runs in the `Container.CheckForChanges` postfix (once a second per container) on the owner client,
  staggered per container, skips containers in use, containers not usable by the local player and placed pieces
  (`ItemDrop.IsPiece`); the age comes from the drop's `spawntime` ZDO value against `ZNet.GetTime`; rule order is
  refuse, accept, then the only-held rule; the drop is claimed, loaded, added with `AddItem`, then destroyed
  through `ZNetScene` or reduced and saved. Unchanged by the Shared module: owner only.
- Buttons are clones of `m_takeAllButton`: Quick stack, Stow all, Top up, Sort and a trash can, 78x26 px,
  anchored bottom-centre of `m_player` with the pivot at the bottom; one Sort centred at the bottom of
  `m_container`. Both hang below their panel's bottom edge: `anchoredPosition.y` is `-(26 + 4)`, so the top of a
  button sits 4 px under the edge, plus the per player `Button Row Offset` (default 0, positive up). Inside the
  panels nothing is free: `InventoryGui.SetInventorySize` grows `m_player` by exactly one grid row per inventory
  row, so the last item row sits on the panel's bottom edge (a row 6 px up covered it), and `m_container` ends
  with the take-all line. The prefab's spacing between the two panels is not in the game code; if the container
  panel abuts the player panel, the row overlaps its title and the offset is the remedy. `PanelButtons` keeps the
  placed rects and `Reposition` (from `StowModule`, on `SettingChanged`) re-places them without a restart. The
  hovered item mirrors `InventoryGrid.UpdateGui`'s tooltip choice (gamepad selection, else the hovered element).
- Shared chests (SPEC 9.3): every container write of Stow goes through `Shared.ChestWriter`, one `Put` or `Take`
  per stack, whether the chest answers at once (the local client owns it or may claim it: claim, the game's
  inventory methods, save, all inside the writer) or by request (`Full` mode, another player using it). Sorting
  is the one exception: it rewrites every cell at once, which no request carries, so a chest the player views or
  shares is refused ("Viewing only" in View mode, "The chest cannot be changed right now" in Full mode; auto sort
  skips it quietly). A Stow target (`StowTargets`) is a container that is usable now or shared; the nearby list
  holds both, nearest first, the open one first, no duplicates. A chest the player only views (View mode) is no
  target: quick stack to the open container, Stow all and Store one say "Viewing only" for it; quick stack
  nearby, Dump, Top up and Route skip it silently and use the other chests.
- Summary messages stay honest: what moved at once is reported at the end of the action ("Moved n stacks",
  "Topped up n items"); every shared chest reports itself once its last reply is in ("Moved n stacks to <chest>",
  "Topped up n items from <chest>", only when something moved; a refusal is the writer's own message, nothing is
  said twice). When nothing moved at once and replies are still to come, nothing is said yet. A put credits one
  stack (moved at least partly), a take credits its amount (the owner grants exactly the amount or denies).
- A stack of the player inventory with a put under way is registered (`StackMover.IsPending`) until the reply:
  every later target of the same action, every later action and the sort leave it alone, so the stack can never
  be moved twice (on yes the chest keeps the copy and the writer removes the original). The game's own moves of
  such a stack during the round trip are not blocked; the writer then logs a warning and the chest keeps what it
  accepted. Requests to one chest are sent all at once, one per stack (no batch RPC was added); the owner applies
  them in order and a chest that fills up denies the rest with "No room in the chest".
- Top up counts the room per kind (name, quality, world level) once and spends it as takes are sent, so a shared
  chest is never asked for more than fits; one take per chest stack; the reply merges into the partial stacks the
  way the game's `AddItem` does. A chest that answers at once is claimed before its stacks are picked, because a
  claim may reload the inventory's item objects.
- Trash from a shared chest: there is no destroy request, so the stack is taken into the inventory and the units
  that arrived (the count of that kind after the reply against before, at most the amount) are destroyed there;
  what did not fit stays on the floor or in the chest, the player never loses more than the trashed stack. A viewed
  chest refuses with "Viewing only" before the confirmation. The trash can accepts a stack dragged from a shared
  chest the same way.
- Route and Store one are one put each; a chest that answers at once and takes nothing says "Nothing to move", a
  shared chest's answer is the message ("Sent x to chest" on yes, the writer's refusal on no).
- Cycling in View and Full modes includes chests another player is using when the Shared module would open them
  read-only: in use by another, the ward and privacy checks of `Container.Interact`, the prefab enabled and the
  section 0 switches (the rules are repeated in `Cycling.Viewable` because `ContainerScan` has no viewable query;
  moving it into Core would be cleaner). In `Off` the ring is the usable containers, as before.
- Find marks shared chests too (they are targets), so the player sees where a quick stack would go.
- Vocabulary since 1.1.0: the chest-side button that stores your matching items is `Stow all` (`Stow All Key`, was
  `Store All Key`), the button that refills your stacks from the chests is `Top up` (`Top Up Key`, was `Restock
  Key`), an item marked for destruction is junk (`Junk Key`, was `Trash Flag Key`; `Destroy Junk Key`, was `Trash
  Flagged Key`; custom data `OpenKeep.junk`, was `OpenKeep.trashFlags`, old marks are not read). Destroying an item
  stays "trash" (`Trash Key`, `Confirm Trash`, `Trash Uses Salvage`). A cfg from 1.0.0 keeps its bindings through
  `Core.RenamedKeys.Carry`, which reads the old line from BepInEx's orphaned entries before the new key is bound.
- The defaults `G` (Stow all) and `V` (Store one) share keys with the game's radial menu (`OpenEmote`) and auto
  pickup toggle (`AutoPickup`), which is safe: both are read in `Player.Update` inside `if (TakeInput())`
  (`HandleRadialInput` and `ZInput.GetButtonDown("AutoPickup")` sit in the branch reached only when `flag2 =
  TakeInput()` is true), and `Player.TakeInput` returns false while `InventoryGui.IsVisible()`. `InventoryGui.Update`
  itself reads no letter keys, and the radial's own close checks run only while it is open, which it cannot be with
  the panel up. The mod's hotkeys fire only with the panel open (`Keys.InventoryOpen`), so neither key ever does
  two things at once.
- Reach is untouched: it counts and pays through `ContainerScan.Nearby(..., ContainerUse.Reach)`, which never
  returns a shared chest, and nothing in Stow feeds Reach.

### Salvage

- Recipes with `m_requireOnlyOneIngredient`, recipes with no resources and recipes that need the item itself are
  not salvageable; `m_upgraderResource` requirements are ignored. The first enabled matching recipe wins; the
  lookup is cached per object database and recipe count.
- Returns: fraction, rounding (Round is half up), cap at `ceil(full cost)`, then At Least One; materials with the
  same name are merged before rounding. The fraction override order is exact name, first matching pattern, cfg.
- Favourites come from Stow's `OpenKeep.favouriteItems`; prefab and shared name both count.
- Require Station is satisfied by the current station or any station of that name within build range.
- The fit check is an exact simulation of `Inventory.AddItem`: partial stacks of quality 1 at the current world
  level fill first, the rest needs empty slots, the salvaged stack's own slot counts as free. The stack is removed
  first, then the returns are added; if an add still fails everything added is removed and the stack goes back
  into its slot.
- No progress bar; the tab selection persists like the game's tabs; the list shows the quality number and `x<n>`;
  success prints a top-left message. The Salvage Key reads the hovered stack of the player grid at the pointer,
  else the gamepad selection, and ignores presses while an item is dragged or a popup shows.

### Stacks and Capacity

- Every world item, picked-up item and loaded item carries its own copy of `SharedData` (the game re-links it to
  the prefab only in the editor), so values are written into the prefab and into every live item: through the
  ItemCopies library for `ItemDrop.s_instances`, the local player's inventory and the open container, and for new
  copies (`ItemDrop.Awake`, `Inventory.AddItem`); `LiveItems` adds the other tracked containers. Cached weights
  are refreshed. The load path clamps stacks to the
  prefab's maximum, so lowering a multiplier or removing the mod loses items above the new maximum when an
  inventory loads (README warning).
- Rule order: vanilla times multiplier, then the per item cfg entry, then YAML patterns in file order, then YAML
  exact names. YAML multipliers replace the cfg multipliers when present. `Enabled` off restores vanilla.
- Per Item Config Entries: defaults are vanilla; 0 or the vanilla value counts as not set; bound with
  `SaveOnConfigSet` off and the apply suspended, one save at the end.
- Merge Into Chests only on drag and drop into the open container from another inventory (the click move already
  merges in the game), and only into a container the local client owns: a viewed or shared chest is left to the
  Shared module's own `DropItem` prefix (an explicit early return, not only the zeroed amount). Ignore Teleport
  Restriction returns true unconditionally.
- Hover contents are summed by shared name, most first, shown only for a valid container the player could open
  (guard stone and privacy checks). The read-only grid on hover is not built (no setting for it).
- A container entry that lost its indentation when uncommented (players delete the whole `#  `, landing the key
  at the file's root) is read anyway with a warning naming the fix; a non-map root key warns as unknown. Generated
  and default files comment entries as `  # entry` - indentation first - so removing the `#` alone also works.
  `ContainerTemplate.Normalize` treats the 1.1.0 style (`#  entry`) as still-default so updating players keep
  getting the generated list.
- The container list is generated at `ZNetScene.Awake` on the author while the file still equals the
  embedded default, through `Yaml.Replace(saveToDisk: true)`. Sizes are set on the prefab's `m_width` /
  `m_height` and on every loaded instance's `Container` and `Inventory` fields, then `Changed()`; shrinking below
  an occupied cell is refused with a warning; widths above 8 warn. The documentation files are written at scene
  load and after every Stacks YAML apply, tab separated.

### Carts

- The station is added per cart instance in `Vagon.Awake` and when the settings change (the prefab is untouched,
  so switching off removes it at once): a `CraftingStation` copied from the workbench prefab's station, same name,
  no roof or fire requirement, `m_useDistance` at least 4 m, `m_upgrader` false. Shift + Use (the game's alt use)
  opens it; plain Use attaches the cart. `GetLevel` adds `Cart Station Level - 1` on top of the game's 1 plus
  extensions. The cart extension piece is not built (no setting for it).

### Shared

- The owner is the referee and nothing is applied speculatively. A Take is granted when the slot still holds an
  item of the same shared name and its stack is at least the amount; the owner takes exactly the amount. A Put is
  granted for what `ChestOps.Put` accepts: into the named slot only when it is empty or holds the same kind with
  room, without a slot anywhere; zero accepted is "No room in the chest". A Move within the chest checks the name
  at the source slot and behaves like the game's drag and drop (empty slot, merge into the same kind, or a swap of
  whole stacks). Take all grants every entry whose slot still holds the named item, at most its stack; Stack all
  grants every sent stack whose kind the chest holds, through the game's `AddItem`. A client that no longer owns
  the ZDO answers `not owner`; the requester resolves the owner again and retries once.
- No stack swap through a Put: a drop from the player grid onto a different item in a viewed chest is denied, the
  game's swap would need the chest's item back in one call.
- The Take side checks that the amount fits first (`InventoryFit`, a dry run of `AddItem` over a snapshot that
  books what it returns), asks for what fits, and on yes adds the item; what no longer fits is dropped at the
  player's feet with the game's "dropped" message. Take all sends only the entries that fit.
- Right click on a stack of a viewed chest (use or equip from the chest) is refused in both modes. The game's
  Move-modifier click and its Drop-modifier click on a viewed chest's stack are both routed as a Take of the whole
  stack in Full mode (the game's drop would throw a copy on the ground while the chest keeps the stack); a plain
  click starts the game's drag. Ctrl+click (the game's move click) on the player grid with a viewed chest yields
  to Stow's Route Modifier when Stow is enabled and the modifier is held.
- A viewer re-sends the game's own open request 1 s after the read-only open, then every 5 s while the chest stays
  in use, and within 1 s once the in-use flag clears; a refused response is silent while viewing, a granted one
  turns the panel live through the game's `Show`. The auto-close distance is applied by the viewer's own
  `UpdateContainer` prefix. In View mode the Take all and Stack all buttons are disabled and a drag from the chest
  is cleared; in Full mode both buttons send their batch.
- A slot with a request under way ignores further clicks on it (no second request, no message) until the reply or
  the timeout. Requests without a reply after `Request Timeout` are denied locally ("The chest did not answer").
- Touches: mouse-down on a stack of the open chest's grid sends a touch (Full mode); the sent touch ends when the
  button is up with nothing dragged, when the panel leaves the chest, or after `Touch Seconds`; received touches
  expire after `Touch Seconds`; a player's own touch is not shown to them. The owner forwards touches to everybody.
- The `DropItem` prefix runs with `Priority.First` and zeroes `amount` so later prefixes on the same method see
  nothing to merge (Harmony runs every prefix); the split dialog ends in the same `DropItem`.
- A cart another player pulls is in use (`m_wagon.InUse()`), so interacting with its storage opens it as a viewer
  instead of the game's refusal.
- Extra words beyond the spec: `ok_shared_someone` ("another player", when the ZDO carries no name),
  `ok_shared_nofit` ("Not enough room in your inventory"), `ok_shared_chestfull` ("No room in the chest"),
  `ok_shared_unavailable` ("The chest cannot be changed right now": neither writable now nor shared).
- Items travel as `ItemPacket`: the prefab name plus the game's own `ItemData.Save` fields, rebuilt from the
  prefab on the receiving side, so quality, variant, durability, crafter and custom data survive; an item without
  a prefab cannot travel and is refused with "The chest cannot be changed right now".

### Signs

- Who acts: only the client that owns the container's ZDO places, moves, rewrites or removes its sign, in the
  `Container.CheckForChanges` postfix (once a second, after the game's own `Load`), never while the inventory has
  not been read (`m_lastRevision`). A dedicated server instantiates only objects near the world origin (its
  reference position is zero), so in practice the nearest player's game does the work; the server assigns
  ownership of unowned pieces to the nearest peer every two seconds.
- Placed by a player means `Piece.IsPlacedByPlayer()` on the container's own `Piece` (creator not 0); ships and
  carts are `ContainerScan.IsShip` / `IsCart`. The private chest is treated like any chest.
- Creation is `UnityEngine.Object.Instantiate(prefab, position, rotation)` of `ZNetScene.GetPrefab("sign")`, which
  gets its persistent ZDO in `ZNetView.Awake` exactly as `Player.PlacePiece` does; `WearNTear.OnPlaced` is called as
  the game does. The creator is written as the `creator` ZDO long and `Piece.m_creator` (the local player's id, only
  when there is a local player); `Piece.SetCreator` itself is not called because its `PlatformUserID` parameter
  lives in the `Splatform` assembly the csproj does not reference, so the creator's platform index stays -1. No
  cost, no effect, no station, no noise.
- Place: the world AABB of the container net object's active `MeshRenderer` and `SkinnedMeshRenderer` components
  (particle renderers and inactive damage-state meshes excluded): centre x and z, `max.y + Height`, plus the YAML
  offset rotated by the container's rotation; yaw = container yaw + `Rotation` + YAML rotation, the sign upright.
  The pose is the sign's bottom: after instantiation the sign's own renderer bounds are measured and the sign is
  shifted (transform and `ZDO.SetPosition`) until its renderer bottom sits on the pose; the pivot-to-bottom distance
  is remembered so later signs are created in the right place at once.
- Re-placing: when the cfg (Height, Rotation, the text settings) or the YAML changes, and once per container after
  it loads, the owner compares a loaded sign's renderer bottom and yaw with the computed pose (2 cm, 1 degree). A
  sign that is off is placed anew with its `text`, `author`, display name and `autoText` copied, and the old one
  destroyed through the scene, so every client sees the move at once (static pieces do not follow ZDO position
  changes while loaded). A player's words survive the move.
- Text: `text` with an empty `author` and an empty `authorPlatformDisplayName`: `Sign.UpdateText` turns an empty
  author into `PlatformUserID.None`, which is not valid, so the view permission is granted on every platform
  without a relations check. The loaded sign's `UpdateText` is called after a write so the board changes at once
  instead of at its next two-second check. Item kinds are keyed by localized display name (what the board shows);
  ties sort by name. `Max Characters` counts the ellipsis (U+2026) as one character: an item is added only when
  it and, if more kinds remain, the ellipsis fit.
- Change detection: the container ZDO's `DataRevision` (every `Container.Save` moves it) is compared with the
  revision of the last look; only then is the text rebuilt, and written only when it differs from what the sign
  shows. A sign whose text differs from `OpenKeep.autoText` and is not empty is a player's; the mod leaves it alone
  until the text is empty again and something changes or the container reloads. A container without a sign gets
  one on every tick regardless of the revision (so `openkeep signs reset` brings the sign back within a second).
- Throttle: after any write (placement, move, text) the container's next write waits `Update Seconds`; the tick
  keeps looking every second, so a change during the wait is applied by the first tick after it. The console's
  `rewrite` ignores the throttle and rewrites the text even when unchanged.
- No wear: the game's `WearNTear` fields are named backwards (`m_noSupportWear = true` means "take 100 damage per
  wear tick without support", see `UpdateWear`; `m_noRoofWear = true` means "get wet and wear without a roof").
  Both are set to false on every automatic sign instance, right after `Instantiate` in `SignPlacer` and in the
  `Sign.Awake` postfix for signs that load with `OpenKeep.signOf`, so a sign floating above a chest never crumbles
  or rots. The prefab is untouched (player-placed signs keep vanilla wear); the instance field matters only on the
  owner, and every owner runs the mod. `WearNTear.OnPlaced` does not touch these fields.
- Opt-out: only `WearNTear.Remove` (the removing player's client, the hammer) sets `OpenKeep.noSign` on the
  container, after claiming its ZDO the way `ContainerScan.Claim` does; a container another player has open is
  not claimed and keeps its right to a new sign (logged as a warning). A sign destroyed by damage (a troll) is
  placed again on the container's next tick. The mod's own removals go through `ZNetScene.Destroy` (nothing
  drops).
- Removal: the container's owner destroys the linked sign (loaded: `ZNetScene.Destroy` after claiming; not loaded:
  claim, then `ZDOMan.DestroyZDO`) and clears `OpenKeep.sign` when the container may not have a sign (module off,
  prefab off, ship or cart, opted out) and in `Container.OnDestroyed` (the game's own hook the owner runs when the
  container is destroyed by the hammer or damage). Signs a player edited are removed the same way.
- Orphans: `Sign.Awake` adds a `SignOrphanCheck` component to every sign carrying `OpenKeep.signOf`; ten seconds
  later, and every five seconds while the sign is not owned here, it checks that the container ZDO exists and
  points back at this sign, and the owner destroys a sign that fails. The delay exists because a client receives
  an area's ZDOs over several frames and the container's may arrive after the sign's. A sign whose container
  points at another sign is a stray from a re-placement and goes the same way.
- YAML: `offset` must be a list of three numbers and `rotation` a number; anything else is an error, which
  rejects the file set (the previous rules stay) as in every other module. `enabled` alone, or an empty entry
  (warned), is accepted. Prefab keys are trimmed and case-insensitive.
- The console `signs` sub-command lists every loaded container (not only reachable ones) with its distance,
  state (`sign`, `player text`, `no sign`, `opted out`) and `(owned elsewhere)` when another client owns it;
  `reset` needs admin or host on a server and skips containers another player has open; `rewrite` counts the
  containers where something was placed, moved, written or removed.

## Not yet implemented

- Capacity: the read-only container grid on hover (SPEC section 5's stretch goal); no setting is bound for it.
- Carts: the cart extension piece parented to a cart (SPEC section 6's stretch goal); no setting is bound for it,
  `OpenKeep.cartOffset` stays reserved.

## Test checklist (LocalTesting profile)

Launch through the r2modman profile `LocalTesting` (the build copies the DLL there). Never start or kill the game
from a script.

1. Log shows `Loading [OpenKeep 1.1.1]` without failed patches; `milkyteam.openkeep.cfg` and the six YAML files
   appear in `BepInEx/config`; after a world loads `OpenKeep.Items.txt` and `OpenKeep.Containers.txt` are written
   and `OpenKeep.Containers.yml` lists every container prefab commented out (chests, `VikingShip`, `Cart`).
2. Reach: with wood only in a chest 10 m away, the hammer shows the campfire requirement as `0 + 5` in the
   storage colour and lets you place it; the wood leaves the chest. Craft a club the same way. Park a Karve with
   wood in its hold next to a workbench: the wood counts and is taken from the hold; with `Ships` off it is not;
   `openkeep containers` lists `VikingShip`. Toggle key turns it off and the requirement goes red. A chest 30 m
   away is not counted. A chest inside a stranger's ward is not counted. Requirement Display Total and Vanilla,
   the flash after crafting, Alt + L draws lines from chests to stations, placing a chest draws them too.
3. Stations: interact with a smelter holding no ore; one ore leaves the chest; hold Fill (Shift) and it fills;
   hold Pull (Alt) and the ore lands in the inventory. Same with a campfire's wood (plain E and Shift + E), a
   kiln, a cooking station's meat, an oven's fuel switch and a fermenter's mead base. Hover texts show
   `From storage: n`. Rebind Fill Modifier to LeftControl and check a brazier or hearth (a fire that can be turned
   off) is not toggled repeatedly.
4. Stow: check the button row below the player panel and the Sort button below the container panel's Take all
   line overlap no item slot and no game text (with and without a chest open; note what `Button Row Offset` a
   clean layout needs, and that a changed offset moves the buttons at once); quick stack, stow all, top up, sort by each order,
   favourite item and slot (star, border, cross drawn), trash with confirmation, destroy junk, route with
   Ctrl click (with and without a chest open), store one with V, find (line and floating count), dump
   outside the inventory, cycle with arrows and wheel between three chests within 4 m. Favourites survive a relog.
   Gamepad: the buttons are selectable, the popup confirms and cancels.
5. Ground pickup on with `pickup: true` for the chest's prefab in `OpenKeep.Stow.yml`: drop copper ore near a
   chest holding copper; after the delay it is inside the chest. An item inside a stranger's ward is not taken.
6. Salvage tab appears after Upgrade (position with and without a station), lists an iron sword, returns the
   rounded fraction; with a full inventory the button is disabled and the hotkey says so. Backspace on a hovered
   stack asks first. `Trash Uses Salvage` on: Delete salvages instead. Gamepad steps through the list.
7. Stacks: `Stack Multiplier = 2` doubles wood stacks in the tooltip and inventory; YAML `Wood: {stack: 200}`
   wins; `Ignore Teleport Restriction` lets ore through a portal; dragging a stack onto a chest with a partial
   stack merges first; `Per Item Config Entries` on adds sections 4a and 4b after the item database loads.
8. Capacity: YAML `piece_chest_wood: {width: 8, height: 4}` resizes new and existing chests; hover shows the fill
   line and contents; shrinking a chest with items in the extra rows is refused with a log warning.
9. Carts: `Cart Workbench = On`, craft a club with Shift + E next to a cart in the wild and place a wall next to
   it; plain E still attaches the cart; a workbench extension near the cart raises the level shown.
10. Two clients: server-locked settings cannot be changed on the client; a YAML edit on the server reaches the
    client within seconds; a chest another player has open is skipped; a chest the other player closes becomes
    reachable again.
11. Two clients, `Shared Chests = View`: A opens a chest, B interacts: B sees the contents and the title
    `<chest> (in use by A)`, cannot move anything (click, drag, right click, Take all and Stack all say `Viewing
    only`, the buttons are greyed), B's Y and Stow all say `Viewing only`, B's Ctrl click routes elsewhere; A
    closes, B's panel turns live without closing (the title loses the suffix and B can move items). B's log shows
    `viewing piece_chest_wood used by A`, every few seconds `viewer asks to open ... (free: False)`, then
    `no longer viewing ...` when the panel turns live or closes. A pulled cart's storage opens the same way.
12. `Shared Chests = Full`: both open the chest; B takes a stack, A sees it go within a second (B's log:
    `request 1 OpenKeep_Take for piece_chest_wood sent to owner <id>`, A's log: `request 1 from B: take ...`,
    `reply 1 to B: yes`, B's log: `request 1 OpenKeep_Take answered by peer <id>: yes`); both click the same stack
    at once, exactly one gets it and the other sees `Someone else got there first` (`reply <id> to <name>: no,
    denied`); B drags an item in, A sees it; B drops a stack onto a different item in A's chest: `No room in the
    chest`, nothing swapped; B presses Take all and Stack all; A sees B's mouse-down tint and `B is moving this`
    in the tooltip; B quick stacks (Q, and Alt + D from outside) into the chest A is using: B's message is
    `Moved n stacks to Chest` when the replies are in, the stacks appear on A's side, and B's inventory keeps
    nothing that A's chest accepted; B tops up from it (`Topped up n items from Chest`); B sorts it: `The chest
    cannot be changed right now`; B trashes a stack of it from the chest grid (it is taken and destroyed, `Destroyed
    ...`); A crafts while B holds the chest: the chest is not counted for A (the requirement row shows only A's
    inventory). Stop A's client while B has a request pending: B sees `The chest did not answer` after 2 s and
    nothing moved on B's side.
13. `openkeep reload` reloads the cfg and every YAML file; the Configuration Manager `Edit YAML` entries open the
    editor for each set.
14. Signs, `7. Signs / Enabled = true`; place a wooden chest: a blank sign appears above it within 2 s, facing the
    chest's front (if it faces backwards, note it: the `Rotation` default changes). It does not crumble, in rain
    or in the open (its wear is switched off). Put wood and stone in: the sign reads `Wood, Stone` within 2 s;
    `Show Counts = true` and the next change shows `Wood 10, Stone 5`. Fill six item kinds: four names and the
    ellipsis `…`, which must render on the sign's font; if it shows as a box the ellipsis becomes `...` (three
    characters, `SignText.Ellipsis`). Empty it: blank (or `Empty Text`).
15. Edit the sign with `E`, write `Ores`; change the chest contents: the sign keeps `Ores`. Clear the sign text:
    the next change writes the contents again.
16. Remove the sign with the hammer: it does not come back after adding items or after logging out and in (one
    wood drops, the vanilla sign's resource). `openkeep signs reset`, then change the contents: the sign is back.
17. Remove the chest with the hammer: the sign goes with it. A chest destroyed by a troll: its sign is gone when
    the area is next loaded (the owner removes it at once when it is loaded at the time). A troll smashing the
    sign alone: a new sign appears on the next tick, no opt-out.
18. A cart and a moored Karve get no sign; a dungeon chest gets none; the private chest gets one.
19. `piece_chest_wood: { enabled: false }` in `OpenKeep.Signs.yml`: existing wooden chest signs disappear, iron
    chest signs stay; `offset: [0, 0.5, 0]` raises them by half a metre after the file reloads (the sign is placed
    anew, its words kept).
20. `Enabled = false`: every sign disappears; on again: they return on the next change or reload.
21. Two clients: the sign placed by A's game is visible to B; B fills the chest, the sign updates (B owns the
    chest after opening it); A's `Lock Configuration` keeps B from changing `Show Counts`.
22. Old world without signs: walking to a base places signs on every chest as they load, once each. Placement
    height and facing on a wooden chest and on a reinforced chest; `openkeep signs` lists them with their state.

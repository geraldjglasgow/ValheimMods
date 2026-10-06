# CLAUDE.md - OpenKeep

Storage and inventory for Valheim, version 1.7.0: crafting, building and station feeding from nearby containers
(Reach), stow, top up, sort, junk, trash, routing, chest cycling and ground pickup (Stow), a Salvage tab, stack
sizes and weights (Stacks), container sizes, station capacities and hover contents (Capacity), carts as
workbenches (Carts), several
players in one chest (Shared), a contents sign above every player-built container (Signs), a - amount + stepper
beside the Craft button and a craft speed (Batch), a recipe search, favourite recipes and list or grid views in the
crafting panel (Recipe List), recipes pinned to the HUD with their materials (Recipe Tracker), and base tweaks outside
storage: respawn at the nearest owned bed, pieces on wooden floors, honey per day, fires refuelling from nearby
containers, torches lit only at night, smelters and kilns feeding themselves from the containers beside them and gear
repaired when a crafting station opens (Homestead), and a build camera detached from the player near a crafting
station (Build Camera). The player's own inventory (a bigger grid, labelled slots, a key
ring, backpacks and the look) is the separate mod PackPanel, which this one works with (see "PackPanel" under the decisions). Written black-box
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
  through the Shared module's requests to its owner (`ChestWriter`), never locally. `Claim` takes only a container
  nobody owns; one another client owns is asked for (`HandOver`) and used once it arrives, ship and cart storage
  only while the local client owns the vehicle.
- Nullable is off. Zero compiler warnings from the mod's own code. Build from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build OpenKeep/OpenKeep/OpenKeep.csproj -c Release`; the build copies the
  merged DLL to `dist/` and to the r2modman profile `LocalTesting`. Never launch or kill the game from a script.

## Layout

```
OpenKeep/OpenKeep/src/
  Plugin.cs                 entry: Lock Configuration, module Initialize calls, patches per class, Synced.Finish,
                            Guard.Install last (the YAML editor draws from SyncedConfig's host, no OnGUI here)
  HotPatches.cs             one patch per hot game method several features hook (Player.Update, InventoryGui.Update and
                            UpdateRecipe postfixes, Container.CheckForChanges), asking each feature in turn
  Core/                     shared by every module
    CoreModule.cs, CoreSettings.cs, SharedMode.cs   section 0. Containers (Ships, Carts, Player Chests, Honour Wards,
                            Shared Chests: Off, View, Full)
    ContainerScan.cs        loaded containers (Container.Awake postfix, Registered count), Nearby, Allowed, IsUsable
                            (= IsReady + IsAllowed), IsShared, InUseByAnother, Claim, Save, PrefabName, IsShip, IsCart,
                            IsPrivateChest
    ContainerFacts.cs       a tracked container's fixed facts (prefab name, ship, cart, private chest), found on first use
    ContainerRules.cs       per prefab enabled table, filled by Reach's YAML, read by every module
    ContainerUse.cs         Reach or Stow (both share the section 0 rules today)
    ItemMatcher.cs, ItemMatchSet.cs, ItemGroups.cs   the item vocabulary and the groups: map of a YAML file
    ItemNames.cs            prefab name, display name, stacking identity of an item
    CharacterData.cs        string sets and flags in Player.m_customData under OpenKeep.<key>
    Keys.cs                 the hotkeys through the Hotkeys library (main key looked at first), InventoryOpen,
                            TextInputActive (Typing.Active; the YAML editor registered as a window)
    InventoryChanges.cs     the count of every Inventory.Changed (prefix); the per-frame caches key on it
    SaveHolds.cs            one save per container per operation: Container.OnContainerChanged prefix skips the
                            game's save while held, Claim loads once per hold, the end of the hold saves once
    ContainerClaim.cs       who may become a container's owner: a free one is claimed, another client's is asked
                            for, vehicle storage never; CanClaimNow, OwnedElsewhere, VehicleOfAnother
    HandOver.cs             RPC OpenKeep_HandOver (Container.Awake postfix): the owner hands a chest nobody uses over,
                            latest data first; never ship or cart storage; asked at most every 1 s (actions) or 30 s
    PruneMark.cs            when a per-object table is pruned: once it doubled since the last prune
    SceneSafe.cs            ZNetScene.Awake postfix work that logs and swallows a throw (a throw there stops the world loading)
    RenamedKeys.cs          Carry: a renamed cfg key takes the old line's value from BepInEx's orphaned entries
    Messages.cs, Language.cs   centre and top-left messages; $ok_ words (Localization.SetupLanguage postfix)
    Command.cs              the openkeep console command (Terminal.InitTerminal postfix)
    PackPanelLink.cs        PackPanel present (GUID in the chainloader), its config entries, LaysOutInventory
    PackPanelGrid.cs        PackPanel.mainGrid from the character's custom data: rows, blocked cells, InSlot
  Reach/                    section 1
    ReachModule.cs, ReachSettings.cs, ReachMode.cs, RequirementDisplay.cs
    ReachModel.cs, ContainerRule.cs, StationRule.cs, ReachRules.cs   OpenKeep.Reach*.yml, per prefab container
                            and station rules, gate and ranges
    ReachChests.cs          reachable containers: candidates by the section 0 rules every 0.25 s (sooner on a woken
                            container, a rules apply, a new player body, 4 m moved, a wider range), narrowed every frame
                            by IsReady and each prefab's range; one list instance while its members stay the same
    StorageIndex.cs         what the reachable containers hold per name and per name + quality, walked again only when
                            the list instance, any inventory (InventoryChanges) or the world level changed
    ReachCount.cs           the reach list, requirement counts from StorageIndex, live per-container counts and lookups
    ReachPayment.cs         the payment window (ConsumeResources, DoCrafting) and the RemoveItem prefix
    ReachReady.cs           before a craft (DoCrafting prefix) or a placement (TryPlacePiece prefix): storage can pay
                            now, else the chests are asked for and it waits; Craft pressed asks at once
    EpicLootLink.cs         Epic Loot's RegisterInventoryProvider (reflection, from Plugin.Start): its table pays from chests
    ReachPull.cs            moving items from containers into the inventory, never in two places
    Requirements.cs         the game's requirement filter (upgrader resources, missing items)
    RecipeRequirementPatch.cs, PieceRequirementPatch.cs, FirstRequiredItemPatch.cs   counting
    RequirementRows.cs, ReachFlash.cs   the requirement rows (SetupRequirement postfix) and the flash after a pull
    CraftPullPatch.cs       Pull modifier + Craft
    StationFeed.cs, StationAccepts.cs, StationHover.cs   borrow one unit, Fill, Pull, "From storage" hover lines
    SmelterOrePatch.cs, SmelterFuelPatch.cs, CookingFoodPatch.cs, CookingFuelPatch.cs, FireplacePatch.cs,
    FermenterPatch.cs       one prefix/postfix pair per station entry point
    ReachKeys.cs            Toggle Key and Link Key (from HotPatches' Player.Update postfix)
    ReachLinks.cs           link lines (LineRenderer) and the PlacePiece postfix
  Stow/                     section 2
    StowModule.cs, StowSettings.cs, StowWords.cs, SortOrder.cs
    StowModel.cs, StowRule.cs, StowRules.cs   OpenKeep.Stow*.yml: groups, pickup, accept, refuse
    StowTargets.cs          the open container and the nearby ones: usable now or shared (Full mode)
    ChestBatch.cs           one action's writes to one container through Shared.ChestWriter, with the summary
    StackMover.cs           stacks with a put under way (never moved twice), the end-of-action message
    StowActions.cs          quick stack, store all, dump, the Take all key (the game's OnTakeAll, so Shared routes it); SpillOver: a shared chest's leftover goes on to later holders
    Overflow.cs             one stack through a line of containers: the chosen one, then every later one holding the
                            item, nearest first, until placed; one put per step, a shared chest's answer continues it
    TopUp.cs, Sorting.cs, Trash.cs, Routing.cs, Finder.cs, Cycling.cs
    Favourites.cs, Movable.cs   favourite items, favourite slots, junk marks; what may move
    StowHotkeys.cs          after InventoryGui.Update (HotPatches): the hotkeys and cycling
    PanelButtons.cs         InventoryGui.Awake postfix: the button row and the container Sort button; Follow puts
                            the row into PackPanel's strip while it is shown
    TrashPlate.cs           the trash can's own plate in the game's style, a copy of the game's armour plate
                            named Trash, 78 under it (the spot trash can mods use); the game's plates only read
    HoveredItem.cs          the slot under the pointer (or the gamepad selection)
    ClickRouting.cs         InventoryGui.OnSelectedItem prefix: Route Modifier + click
    DumpKeyPatch.cs         after Player.Update (HotPatches): Dump Key outside the inventory
    AutoSortPatch.cs        InventoryGui.Show prefix/postfix
    FavouriteOverlay.cs, GridMarks.cs, SlotMarks.cs, StowSprites.cs   star, cross, border marks on the grid elements,
                            set again only when the grid, an inventory or the favourites changed (GridMarks); the trash
                            icon from assets/trash.png
    LinkMarker.cs           Find Key: a line from the player and a floating count
    GroundPickup.cs         Container.CheckForChanges postfix
    PickupOrder.cs          which pickup chest takes a drop: holders before acceptors, nearest to the drop first,
                            a full one passed over
    TidySchedule.cs         Auto Tidy's looks: after Container.CheckForChanges (HotPatches), first sight, change, close,
                            retries with backoff for chests whose strays wait, wakes near a change, one per frame
    TidyChanges.cs          Container.OnContainerChanged postfix: a closed owned chest changed (not by Auto Tidy)
    TidyHands.cs            Container.SetInUse prefix/postfix: contents at open against release = put in by hand
    TidySweep.cs            one look: memory update, strays to the nearest home with room, 8 stacks at most
    TidyChests.cs           which chests take part, ready now, the homes of a stray (nearest first, 15 m)
    TidyMemory.cs           ZDO OpenKeep.tidy: per prefab first seen, fading peak, last amount, hand, kept, sent away
    TidyProfile.cs, TidyProfiles.cs   a chest's weights, shares, random-items score; kept per revision
    TidyThemes.cs, TidyLabels.cs, TidyFamilies.cs   what is alike: learned pairs, YAML groups, type themes,
                            smelting families from the game's stations and recipes
    TidyCommand.cs          openkeep tidy
  Salvage/                  section 3
    SalvageModule.cs, SalvageSettings.cs, SalvageWords.cs, RoundingMode.cs
    SalvageModel.cs, FractionOverride.cs, SalvageRules.cs   OpenKeep.Salvage*.yml, recipe lookup, blockers
    ModDataCheck.cs         blocker: item custom data keys under a Mod Data Prefix (another mod's state)
    SalvageReturns.cs, SalvageReturn.cs   what a stack returns
    SalvageInventory.cs     the exact fit simulation and the add with rollback
    SalvageActions.cs       the public face: CanSalvage, WhyNot, Returns, Salvage, Confirm
    SalvageTab.cs, SalvageList.cs, SalvageRow.cs, SalvagePanel.cs, SalvageGuiPatches.cs   the third tab
    SalvageRarity.cs        Epic Loot's rarity background behind the tab's icons, through its published API
    SalvageHotkey.cs        after InventoryGui.Update (HotPatches): Salvage Key
  Stacks/                   section 4
    StacksModule.cs, StacksSettings.cs, StacksModel.cs, StackRule.cs, ItemValue.cs
    StackValues.cs          writes stack and weight into the prefabs and every live item (ItemCopies for the world
                            drops, inventory and open container, and new copies via Copies.HookSpawns)
    VanillaValues.cs, LiveItems.cs (the other loaded containers), PerItemEntries.cs
    DatabaseReady.cs        ObjectDB.Awake / CopyOtherDB postfixes
    PackPanelKeys.cs        PackPanel's Key Stack for the prefabs in its Key Items, as their starting stack
    MergeIntoChests.cs      InventoryGrid.DropItem prefix (skips a viewed or shared chest)
    TeleportPatch.cs        Inventory.IsTeleportable prefix
    Documentation.cs        OpenKeep.Items.txt and OpenKeep.Containers.txt (ZNetScene.Awake postfix)
    DocFile.cs              a documentation file is written only when its text changed (the console command always)
  Capacity/                 section 5
    CapacityModule.cs, CapacitySettings.cs, HoverFill.cs, ContainersModel.cs, ContainerSize.cs
    ContainerPrefabs.cs, VanillaSizes.cs, ContainerSizes.cs   prefab discovery, vanilla sizes, apply and resize
    ContainerTemplate.cs    fills a still-default OpenKeep.Containers.yml with every container prefab
    SceneReady.cs           ZNetScene.Awake and Container.Awake postfixes
    HoverText.cs            Container.GetHoverText postfix
    StationsFile.cs         the OpenKeep.Stations*.yml set (registered from CapacityModule) and its Enabled hook
    StationsModel.cs, StationCaps.cs   prefab to items (m_maxOre) and fuel (m_maxFuel) caps, 1 to 1000
    StationPrefabs.cs, VanillaCaps.cs, StationCapacities.cs   Smelter prefab discovery, vanilla caps, apply to the
                            prefab and every loaded station, restore
    StationTemplate.cs      fills a still-default OpenKeep.Stations.yml with every station prefab
    StationSceneReady.cs    ZNetScene.Awake and Smelter.Awake postfixes
    StationDocumentation.cs OpenKeep.Stations.txt (called from Stacks' Documentation.Write)
    ScenePrefabs.cs         one walk over the scene's prefabs for both ContainerPrefabs and StationPrefabs, kept while
                            the scene and its prefab count stay the same
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
    SignRefresh.cs          after Container.CheckForChanges (HotPatches): the owner's tick; RewriteAll, RulesChanged
    SignOptOut.cs           WearNTear.Remove postfix: OpenKeep.noSign on the container (the hammer)
    SignWear.cs             m_noSupportWear and m_noRoofWear cleared on every automatic sign instance
    ContainerGonePatch.cs   Container.OnDestroyed postfix: the owner removes the sign
    SignOrphanCheck.cs      Sign.Awake postfix adds the component and switches wear off; a sign without its
                            container is destroyed
    SignsCommand.cs         openkeep signs, signs reset, signs rewrite (called from Core's Command by reflection)
  Homestead/                section 8: independent features, each with its own Feature and Settings class
    HomesteadModule.cs      Section, and the Feature.Initialize calls
    BedFeature.cs, BedSettings.cs   Nearest Bed Respawn, Bed Choice Seconds, Quick Respawn (Range, Seconds),
                            Beds On Map; the two words of the choice
    BedPoints.cs            x:y:z text, same bed (1 m, the game's IsCurrent tolerance), map distance, nearest first
    BedStore.cs, BedList.cs   OpenKeep.beds.<world uid> in the character's custom data; changes without a local
                            player wait with their scope (player id @ world uid) until Player.OnSpawned
    BedRespawn.cs           the nearest bed at death, Prefer (a clicked bed), the fallback list, TryNext
    BedWait.cs              Quick Respawn: the death delay (a new RequestRespawn) and the load speed from the distance
    BedLoadPatch.cs         Game.FindSpawnPoint prefix: m_respawnWait runs LoadSpeed times as fast after a death
    BedChoice.cs            the choice of bed after death: open, click, time out, confirm, close
    BedChoiceMap.cs         the large map while dead: the game's map update, zoom to fit, map key/Escape one frame
                            late
    BedChoiceLabel.cs       the countdown in the large map's upper left corner (seconds in large figures)
    BedChoiceMapPatch.cs, BedChoiceClickPatch.cs, BedChoiceScreenPatch.cs,
    BedChoiceEndPatch.cs    Minimap.Update, OnMapLeftClick, Hud.UpdateBlackScreen, Game._RequestRespawn
    BedPins.cs, BedPinsPatch.cs   the other known beds as unsaved bed pins (Minimap.UpdateProfilePins postfix)
    BedPinLook.cs, BedPinLookPatch.cs   bed icons yellow (Minimap.UpdatePins postfix); twice the size and pulsing
                            while choosing
    BedStandPatch.cs        Player.OnSpawned postfix: after a death, the game's getting-up animation is skipped
    (AreaLoading library)   the distance-to-wait line (QuickWait), the server settle (AreaSettle) and Quick Area
                            Loading for the respawn (AreaLoader.When(BedWait.LoadingLand)); portal speed is Wayfare's
    BedChecks.cs            IsLive, IsUnclaimed, IsLocal (ZDO owner vs profile id), IsSpawnPoint
    BedDeathPatch.cs, BedRespawnPatch.cs, BedSpawnedPatch.cs, BedSeenPatch.cs, BedUsePatch.cs, BedHoverPatch.cs,
    BedGonePatch.cs         one patch class per game method
    FireFeature.cs, FireSettings.cs   Build On Wood and its change handler
    FirePrefabs.cs          the listed names as piece prefabs (exact, then ignoring case); unknown names warned
    FirePlacement.cs        Piece.m_notOnWood cleared on the listed prefabs and the local placement ghost, the
                            original value back when a name leaves the list
    FireSceneReady.cs       ZNetScene.Awake postfix (Priority.Low)
    HiveFeature.cs, HiveSettings.cs   Honey Per Day, Honey Per Player Online
    HiveRate.cs             seconds per honey: the settings, EnvMan.m_dayLengthSec, players online (ZNet's list);
                            honey hives only; the prefab's m_secPerUnit
    HiveProgress.cs         ZDO OpenKeep.hiveSecPerUnit: progress rescaled on a rate change, the dropped remainder kept
    HivePatch.cs            Beehive.UpdateBees prefix/postfix on the hive's owner
    FuelFeature.cs, FuelSettings.cs   Auto Fuel, Auto Fuel Range
    FuelFires.cs            the fires both fire features act on (ZDO owned here, a piece a player built); state on/off
    FuelRefill.cs           the refill rule on the fire's owner: whole units that fit, one Fireplace.AddFuel per refill
    NearbyTake.cs           Fuel and Feed: units out of the given containers (the caller's ContainerScan.Nearby) with
                            the station's predicate, the container's allow/deny and the world level rule; claim,
                            remove, save
    TakeRetry.cs            Fuel and Feed: 10 s wait for a fire or station that found nothing
    FuelHolders.cs          which loaded containers hold an item (by shared name), one index for every fire; after an
                            inventory change, at most every 2 s, only containers whose ZDO revision moved are walked
                            again; Auto Fuel looks only at those near the fire
    FuelPatch.cs            Fireplace.UpdateFireplace postfix (after TorchPatch)
    TorchFeature.cs, TorchSettings.cs   Torches Night Only, Torch Pieces, Torch Margin, Torch Switch Key (unsynced),
                            the $ok_torch_ words
    TorchPrefabs.cs         Torch Pieces as prefab hashes, split again when the text or the scene changes
    TorchNight.cs           the game's night thresholds (smoothed day fraction 0.25 / 0.75) widened by Torch Margin,
                            once the game's day fraction has caught up with the clock
    TorchSwitch.cs          ZDO OpenKeep.torchPhase, RPC_ToggleOn at nightfall and daybreak, relight when a torch
                            leaves the schedule (setting off, unlisted, kept lit)
    TorchKeep.cs            ZDO OpenKeep.torchKeepLit, RPC OpenKeep_TorchKeepLit: the request (ward check, claim an
                            unowned fire) and the owner's write
    TorchPatch.cs           Fireplace.UpdateFireplace postfix (Priority.High)
    TorchHoverPatch.cs      Fireplace.GetHoverText postfix: "Lights at nightfall", "[O] Keep lit" / "Light at night only"
    TorchKeyPatch.cs        after Player.Update (HotPatches): Torch Switch Key on the hovered fire
    TorchRpcPatch.cs        Fireplace.Awake postfix: registers OpenKeep_TorchKeepLit on every fire
    FeedFeature.cs, FeedSettings.cs   Auto Feed Stations, Auto Feed Range, Auto Feed Skip, Auto Feed Leave
    FeedStations.cs         the stations (Smelter, ZDO owned here, a piece a player built) and their outline (the
                            box around their solid colliders)
    FeedSkip.cs             Auto Feed Skip as an ItemMatchSet, parsed again when the text changes
    FeedTick.cs             the feeding rule on the station's owner: one item (RPC_AddOre) and one fuel (RPC_AddFuel)
                            per tick while there is room
    FeedPatch.cs            Smelter.UpdateSmelter postfix
    PetFeature.cs, PetSettings.cs   Pets Eat From Chests, Pet Chest Range
    PetEating.cs            MonsterAI.UpdateConsumeItem postfix on the creature's owner: at the game's search moment a
                            hungry tame picks the nearest container holding its food, walks there and eats one
    PetFood.cs              what a container offers a creature (its m_consumeItems by shared name, the world level
                            rule, the containers: rule) and the spot beside the container where it stands to eat
    SaveFeature.cs, SaveSettings.cs   Quick World Save
    SaveValueCopy.cs        ZDOExtraData.PrepareSave prefix: the stored values of the saved objects only, the seven
                            tables on worker threads from 20000 objects
    SaveObjectCopy.cs       ZDOMan.AddObjectsPerChunk prefix (private): the changed chunks' object clones, chunks on
                            worker threads from 20000 objects, appended in the game's order
    SaveChunkCopy.cs        one chunk's clones exactly as the game makes them, reading only the game's tables
    RestFeature.cs, RestSettings.cs   Rested Delay
    RestDelay.cs            the setting at use time for the game's Resting only (name hash); the log line with the
                            asset's own delay
    RestPatch.cs            SE_Cozy.UpdateStatusEffect prefix: the SEMan's copy of Resting gets m_delay every tick
    RepairFeature.cs, RepairSettings.cs   Area Repair, the $ok_repair_one and $ok_repair_many words
    RepairPatch.cs          Player.Repair prefix/postfix: the hovered piece's WearNTear.m_lastRepair moved = the game
                            repaired it
    RepairNeighbours.cs     the touching pieces: the game's SetupColliders boxes (+0.3 m) over the active, enabled
                            colliders, OverlapBox on piece, piece_nonsolid, Default, static_solid, Default_small;
                            triggers and rigidbodies skipped; nearest first
    RepairChecks.cs         the hand repair's checks per neighbour, silent: station in build range, ward access
    RepairArea.cs, RepairOutcome.cs, RepairTally.cs   the run: checks, WearNTear.Repair, place effect, cap 64,
                            message, log line
    StationRepairFeature.cs, StationRepairSettings.cs   Auto Repair, the $ok_autorepair_one and $ok_autorepair_many
                            words
    StationRepair.cs        the run: the game's HaveRepairableItems and m_canRepair rule, one RepairOneItem per item
                            its CanRepair accepts, the repair effect once, one message
    StationRepairPatch.cs   CraftingStation.Interact postfix: the station is the local player's current one
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
    ChestRequester.cs       pending requests, replies, the one retry, timeouts and late answers (Player.Update postfix)
    ChestAsk.cs             builds each request and applies the owner's answer on the requester
    Escrow.cs               a put's units held out of the inventory until the answer; the rest comes back
    ChestOwnerHandler.cs    the referee: validates, applies with the game's methods, saves, replies
    ItemPacket.cs           one item on the wire (prefab name + the game's ItemData.Save fields)
    Touches.cs              OpenKeep_Touch: InventoryGrid.OnLeftDown, InventoryGui.Update, InventoryGrid.UpdateGui
  Batch/                    section 10
    BatchModule.cs, BatchSettings.cs   section 10. Batch Crafting (no words of its own)
    BatchCheck.cs           CanMake's materials and room answers kept per amount until an inventory, the recipe or
                            the station changes, at most 0.25 s
    BatchAmount.cs          the amount: where it applies, back to 1 on another recipe, CanMake, Limit (binary
                            search), the - and + steps with Shift and Ctrl, the gamepad's fast repeat, a typed
                            amount, NextCraft for Reach's Pull modifier, Current (its recipe, for the tracker)
    BatchDrive.cs           writes the amount into the game's multi-craft fields, puts the game's back, keeps a
                            started craft's amount for DoCrafting
    BatchStepper.cs         the UI: - amount + left of the Craft button, which gives up the width
    BatchField.cs           the amount as the game's GuiInputField: digits typed, set on Enter or a click
                            elsewhere; Steam's keyboard in Big Picture
    BatchWheel.cs           the mouse wheel on the buttons and the amount
    CraftSpeed.cs           Craft Speed: the game's craft durations remembered at Awake, divided before UpdateRecipe
    BatchPatches.cs         InventoryGui.Awake, UpdateRecipe, OnCraftPressed, DoCrafting
  Recipes/                  section 12, the local player's client only (no ZDO key, RPC or file)
    RecipeListModule.cs, RecipeListSettings.cs, RecipeWords.cs   section 12. Recipe List (all unsynced), ok_recipe_*
    RecipeView.cs           List, CompactList, SmallGrid, MediumGrid, LargeGrid
    RecipeKeys.cs           a recipe's key in saved lists (the recipe asset's name) and the lookup back
    RecipeFavourites.cs     favourite recipes and favourites only in the character's custom data; Rebuild (the
                            game's own UpdateCraftingPanel)
    RecipeQuery.cs          the search text as a filter: words, @material, -not
    RecipeNeeds.cs          which materials count: the game's upgrader rule (also the tracker's rows)
    RecipeFilter.cs         after UpdateRecipeList: drops hidden rows, favourites first, lays out, decorates
    RecipeLayout.cs, RecipeTile.cs   row and tile places for the view; a row reshaped into a tile or a compact row
    RecipeRows.cs           per row: the star, middle click (favourite), right click (track), the hover for F
    RecipeStar.cs           the build menu's favourite star (sprite and colour), OpenKeep's drawn star before it
    RecipeTips.cs           the game's tooltips (a copy of the inventory slots' prefab) on OpenKeep's parts and on tiles
    RecipeSprites.cs        the view button's drawn icons
    PanelButton.cs          copies of the crafting panel's buttons without their gamepad key and hint
    SearchBar.cs            the row above the list: a copy of the build menu's search field; the list gives up
                            the row's height; debounced rebuild; cleared on close
    SearchButtons.cs        favourites only star (Shift: clear all) and view button at the row's end
    RecipeActions.cs        Track and favourite buttons beside the Style button under the recipe's name
    RecipeInput.cs          Search Key, Stow's Favourite Item Key over a row, the search's debounce tick
    RecipeGamepad.cs        right stick shortcuts (once per push) and grid stepping, from UpdateRecipeGamepadInput
    TypingGuard.cs, TypingWatch.cs   E, Tab and Escape do not close the inventory while a panel field is focused
    RecipeListPatches.cs    InventoryGui.Awake, UpdateCraftingPanel (prefix first, postfix), UpdateRecipeList,
                            UpdateRecipeGamepadInput, Update (prefix first, postfix), Hide, UpdateRecipe
  Tracker/                  section 13, the local player's client only (no ZDO key, RPC or file)
    TrackerModule.cs, TrackerSettings.cs, TrackerWords.cs, TrackerFont.cs   section 13. Recipe Tracker (all
                            unsynced), ok_tracker_*
    TrackedRecipe.cs        key, quality, amount, upgrader; saved as key|quality|amount (|u at an upgrader)
    TrackerList.cs          the tracked list in the character's custom data, toggle, remove, step, crafted
    TrackerCounts.cs        need (requirement x amount) and have (inventory, and Reach's containers)
    TrackerHud.cs           on the HUD's object: build, show or hide, rebuild on change, count twice a second
    TrackerPanel.cs         the column under hudroot, its own canvas above the inventory's, background, title, fit
    TrackerEntry.cs         one recipe: header (X, icon, name, - x +), station line, material rows; controls
    TrackerUi.cs, TrackerStyle.cs   texts, icons, rows, text buttons; fonts and colours from the settings
    TrackerDrag.cs, TrackerWheel.cs   drag by the title (Position written), the wheel on a header
    TrackerVisibility.cs    on, something tracked, alive, no large map, no threat for 5 s; the free cursor
    CraftWatch.cs, TrackerPatches.cs   Hud.Awake (adds TrackerHud), DoCrafting prefix (Priority.Low) and postfix
  BuildCamera/              section 11, the local player's client only (no ZDO key, RPC or file)
    CameraModule.cs, CameraSettings.cs, CameraPrefs.cs   section 11. Build Camera (synced gameplay keys; unsynced
                            keys, speed, head light, panel) and the ok_cam_* words
    CameraState.cs          out or not, position, yaw and pitch; out only while its player is the local player
    CameraToggle.cs         after Player.Update (HotPatches): Toggle Key / Gamepad Toggle in and out, every end condition
    PadToggle.cs            Gamepad Toggle: ZInput button names joined with +
    CameraArea.cs           where the camera may be: station build range x Range Multiplier, flat and as high; while
                            AroundPlayer (set by Blueprints) a ball of 50 m round the player's body (the user's cap)
    CameraNeeds.cs          Resting and comfort conditions, read where the player stands
    CameraMotion.cs         one frame of flying: movement keys and left stick flat, Jump/Crouch and the triggers up
                            and down, Run faster; held in the area, then swept
    CameraCollision.cs      sphere sweep and slide against terrain and static_solid
    CameraViewPatches.cs    GameCamera.GetCameraPosition (the pose), UpdateListner (sound on the camera),
                            LightLod.GetLightReferencePoint (light fading from the camera), GameCamera.LateUpdate
                            (head light, panel)
    CameraInputPatches.cs   Player.SetMouseLook (look turns the camera), Player.SetControls (the body at rest)
    CameraReach.cs, CameraReachPatches.cs   the game's four build rays measured from the camera: PieceRayTest,
                            RemovePiece, CopyPiece, UpdateWearNTearHover (prefix + finalizer around
                            m_maxPlaceDistance)
    CameraPickup.cs         Player.FixedUpdate postfix: the game's auto pickup around the camera
    PickupPanel.cs, PickupPanelLook.cs   the HUD panel saying why camera pickup holds back, in the build menu's look
    CircletSource.cs, CircletLight.cs   the light worn on the head, copied onto the camera
OpenKeep/OpenKeep/config/   embedded default YAML files: OpenKeep.Reach.yml, OpenKeep.Stow.yml,
                            OpenKeep.Salvage.yml, OpenKeep.Stacks.yml, OpenKeep.Containers.yml, OpenKeep.Signs.yml,
                            OpenKeep.Stations.yml
OpenKeep/OpenKeep/assets/   embedded UI images: trash.png, the trash can's icon (128 px, scaled down from the
                            author's 1254 px drawing, which is not in the repository); the Blueprints tab's icons
                            (128 px, drawn with Python and PIL at 4x in the style of the first two): blueprint.png,
                            fixground.png, planner.png, copy.png, ghostsshown.png, ghostshidden.png, folder.png,
                            folderup.png, foldernew.png; every PNG here is embedded
```

Startup order in `Plugin.Awake`: `Synced.BindLocking` (General / Lock Configuration), then
`CoreModule.Initialize`, `ReachModule.Initialize`, `StowModule.Initialize`, `SalvageModule.Initialize`,
`StacksModule.Initialize`, `CapacityModule.Initialize`, `CartsModule.Initialize`, `SignsModule.Initialize`,
`HomesteadModule.Initialize`, `SharedModule.Initialize` (the spec's order), `BatchModule.Initialize`,
`CameraModule.Initialize`, `RecipeListModule.Initialize`, `TrackerModule.Initialize` (each binds its settings, registers its YAML set and its words), every patch class on its own, `Synced.Finish`, the `Loading [OpenKeep 2.4.0]` line, `Guard.Install` last.
  Blueprints/               section 14 (moved from EarthWright 2026-10-05; design in ../SPEC-Blueprints.md), off by default
    BlueprintsModule.cs     binds the settings, words, the Sites, Planner and Copy modules, adds BlueprintRunner
    BlueprintSettings.cs    14. Blueprints / Enabled and Build Without Materials, and BlueprintRules (numbers, fixed keys)
    BlueprintTab.cs, BlueprintPieceList.cs, BlueprintTabPatches.cs   the "Blueprints" tab of the game's build menu
                            (BuildUi): a copy of its last tab with its own piece list, shown while on and the hammer
                            is the tool; the other four lists never show the entries; folder clicks; no favourites
    HammerTable.cs          the game's hammer table (ObjectDB "Hammer"): the entries appended and taken out again, in a
                            spare category (first of DeepNorth, Feasts, Food, Meads unused there, logged) listed as
                            "Blueprints" while in
    BlueprintMenu.cs        the tab's view (tools, then the folder's blueprints) and the identity checks (Owns,
                            IsFix, IsPlanner, IsCopy, IsGhosts, IsOurs, NameOf, InHand); refills the table when the switch, the
                            folder, its files or the player changes
    BlueprintEntries.cs, BlueprintIcons.cs   the entries as empty pieces (one per tool and blueprint; names and
                            descriptions, prefab names without spaces) and the nine icons (folder.png, folderup.png
                            and foldernew.png only on the folder panel)
    GhostSwitch.cs          the Construction ghosts switch: this player's choice to draw site ghosts (Visible = shown
                            or the Site planner in hand), flipped by a click on its entry, kept in TabMemory
    BlueprintSelection.cs   keeps the hammer's selection on the chosen entry across refills (and renames)
    BlueprintFolders.cs, BlueprintRename.cs, BlueprintFiles.cs   folders opened, New folder (the panel's button), F2
                            and right-click rename/move through NamePrompt (+ the ConnectPanel F2 guard), the file
                            operations and name rules
    Tab/                    the tab's own UI on the game's piece buttons (namespace OpenKeep.Blueprints.Tab):
                            TabButtons (a BuildUiPieceButton.Setup postfix sets everything below per piece), TabLabel
                            (the name band), TabMark (pick frame), TabPicks (the multi-selection),
                            TabClicks (Ctrl / Shift + click, right click), TabBox (selection box on empty space),
                            TabDrag + TabGhost + TabMoves (drag onto a panel folder or breadcrumb part, the ghost, the moves), TabLook
                            (the menu's font, outlined label material, frames); FolderPanel + FolderRows (the folder
                            panel in the menu's tag column, rows copied from its tag button), NewFolderBadge (the New
                            folder button on its first row, a copy of the menu's key badge), Breadcrumb +
                            BreadcrumbParts (the folder chain above the list), FolderTarget (a panel row's or
                            breadcrumb part's folder: click, drop, right click), TabMemory (folder, tab and the
                            ghosts switch kept on this machine in OpenKeep.BlueprintsTab.txt)
    Blueprint.cs, GroundPaint.cs, BlueprintReader.cs, BlueprintWriter.cs, BlueprintLibrary.cs   DevBridge's blueprint
                            JSON in BepInEx/config/OpenKeep.Blueprints and its subfolders (plain_wood_house
                            embedded), plus "relief"; paths like "houses/barn", CurrentFolder, listings, SaveNew
    BuildFrame.cs, SiteArea.cs, WaterRule.cs, GroundMask.cs   the frame, pad and paint, sea level, dirt under pieces
    GroundGrid.cs, GroundFit.cs   the ground work for any IGroundTarget: pad heights, 45° cuts and ~35° fills (rounded by
                            smoothing passes for Fix ground), within 8 m of the generated ground; stone 0.5 per m³
    GroundUndo.cs           the last build's or fix's heights, put back by Alt+Z (Fix ground) or openkeep blueprint undo
    GroundFixBuilding.cs, GroundFixInside.cs, GroundFixTarget.cs, GroundFixPlan.cs, GroundFixSession.cs, GroundOutline.cs   Fix ground: the
                            building under the crosshair (touching drawn boxes), which pieces touch the ground (floors
                            cut or fill, the rest only fill), the plan and checks, pin/apply/undo, the yellow/blue outline
    HammerZoom.cs           the camera zooms out to 80 m (faster when far) while an entry of the tab is selected
    BlueprintCamera.cs      with an entry of the tab the build camera (section 11, Toggle Key B as in normal building)
                            flies within 50 m of the player (CameraArea.AroundPlayer) instead of near a station
    GroundWriter.cs         RPC OpenKeep_BlueprintGround on each touched terrain compiler; its owner writes the heights
                            (as the game's LevelTerrain) and paint, saves and redraws
    SitePlan.cs, SiteProtection.cs, MaterialBill.cs   the check: in the way, interiors, no-build, wards, learned, materials
    SiteObjects.cs, SiteClearing.cs   trees, logs, stumps, shrubs, rocks, pickables (never ore, crops, offerings)
    PiecePlacer.cs, BuildJob.cs, BuildUndo.cs   one piece as Player.PlacePiece without OnPlaced; a site's all-at-once
                            build (a 2 ms budget a frame, at most 20 pieces); undo takes down the player's last site and its pieces
    PieceShapes.cs, GhostView.cs, SiteOutline.cs, BlueprintHud.cs   the preview, outline lines and HUD (IMGUI)
    BlueprintTool.cs, BlueprintSession.cs, BlueprintKeys.cs   a blueprint entry's click (pin, build), aim/turn/height
                            and the fixed keys
    BlueprintPatches.cs     TerrainComp.Awake (ground RPC), Player.TryPlacePiece (the click), Player.AddKnownPiece
                            (no unlock message), Player.RemovePiece (no removal while an entry is selected); the
                            BlueprintRunner MonoBehaviour (menu, tab, rename, session, build job; disabled on a
                            dedicated server, idle once settled while off unless a site is loaded or a build runs)
    BlueprintGui.cs         the IMGUI HUD and the planner panel, enabled only while one of them has something to draw
    BlueprintCapture.cs, BlueprintCommands.cs, BlueprintWords.cs, BlueprintSafe.cs   capture of real pieces (by radius
                            for openkeep blueprint save, or an explicit list for Copy), the command, ok_bp_* words
    NamePrompt.cs           the game's text box (TextInput) asking for a name: saving, New folder, F2 and right-click rename
    Sites/                  construction sites ("ghost mode"): SiteState (the ZDO contract), SiteCodec (the blueprint
                            as bytes), SiteMarker (post: hover, E deliver, Shift+E take down), SitePrefab (OpenKeep_Site
                            from wood_pole2), SitePlacement, SiteGhost + GhostPick + GhostLook (everyone's ghost, see-through look, glow, picking; hidden and unpickable
                            while this player's Construction ghosts switch is off, unless the Site planner is in hand),
                            SiteBuilder / SitePieces / SiteGround / SiteOrder / SiteNeeds / SiteCosts / SiteStore (the
                            owner builds as materials come in or all at once), SiteDelivery + SiteDeliveries +
                            SiteParcel (materials held until the post's owner answers), SiteTakeOver (a server-owned
                            site handed by the server to the first machine that asks), SiteTakeDown, SiteRights,
                            SiteNetwork, SiteHover, SiteRun, SiteSettings (Build As Resources Come In), SiteWords,
                            SiteHooks (the seams), SmartSelect + Smart*.cs (one enclosed house from a click;
                            SmartSameType: the joined pieces of one prefab), SiteSupport (what holds each piece up:
                            cheapest path to the ground over touching boxes; a queued selection brings its supports)
    Planner/                the Site planner entry: PlannerSession, PlannerKeys (+ a Chat.Update prefix so Enter does
                            not open chat), PlannerClicks, PlannerSelection, PlannerAim, PlannerHouse, PlannerPieces,
                            PlannerBill, PlannerGlow, PlannerHud, PlannerGroup (G: same type), QueueEdits / QueueRpc / QueueCheck (the queue, owner
                            writes), PlannerPanel / PanelModel / PanelStyles (the side panel, WindowInput), PlannerWords
    Copy/                   the Copy building entry: CopyModule, CopySession, CopyKeys, CopyAim, CopyClicks, CopyHover,
                            CopySelection (the chosen real pieces, this machine only), CopyBuildings (a building =
                            GroundFixBuilding.From), CopySameType + CopyGrid + CopyTouch (G: joined pieces of one
                            prefab), CopyFootprint, CopyHud, CopyGlow + CopyGlowPatches (the game's piece tint through
                            MaterialMan, held), CopySave (Enter: name, BlueprintCapture.Of, BlueprintLibrary.SaveNew),
                            ChatEnter (one Chat.Update prefix any tool claims Enter through), CopyWords

Cross-module uses that are allowed: Stow's `Trash` calls `Salvage.SalvageActions` (Trash Uses Salvage), Stacks'
`Documentation` calls `Capacity.ContainerPrefabs` and `Capacity.VanillaSizes` (OpenKeep.Containers.txt) and
`Capacity.StationDocumentation` (OpenKeep.Stations.txt), Stow's
Reach's `CraftPullPatch` asks `Batch.BatchAmount.NextCraft` how many crafts to pull materials for, Stow's
`Finder` reads Reach's `Link Seconds` through `ConfigDefinition("1. Reach", "Link Seconds")`, Core's `Command`
reaches `Stacks.Documentation.Write` and `Signs.SignsCommand.Run` by reflection. Stow's `ChestBatch`, `Routing`, `Trash`, `Sorting` and
`StowTargets` and Stacks' `MergeIntoChests` call `Shared.ChestWriter`, `Shared.SharedState` and
`Shared.SharedWords`; Shared's `PanelRouting` reads Stow's `Enabled` and `Route Modifier` through
`ConfigDefinition("2. Stow", ...)`. Homestead's `FuelRefill`, `FeedTick` and `NearbyTake` use Reach's
`ReachRules.StationRuleFor`, `ReachRules.RuleFor`, `StationAccepts.Fuel`, `StationAccepts.SmelterOre`,
`ReachCount.CountIn` and `ContainerRule`, so the Reach YAML's `stations:` and `containers:` rules apply to auto fuel
and auto feed; `TorchPrefabs` uses `FirePrefabs.Find`. Stow's `MainGrid` and `Sorting` and Shared's `ChestAsk` read
PackPanel's main grid through `Core.PackPanelGrid`, Stacks' `PackPanelKeys` its Key Stack through `Core.PackPanelLink`,
and Stow's `TrashPlate` sits at rank 120 so the column reads armour, trash, weight, world tier. Blueprints'
`BlueprintCamera` sets Build Camera's `CameraArea.AroundPlayer`. Everything else goes through `Core`.

## Patched game methods

Postfix: `Bed.Awake` (remember own beds, forget others at a known point), `Bed.GetHoverText` (Sleep on every own
bed), `Container.Awake` (Core tracking; Capacity sizes; Shared RPC registration; Core's hand-over RPC),
`Container.CheckForChanges` (ground pickup; the sign refresh tick; Auto Tidy's tick, with a prefix noting the loaded
revision), `Container.OnContainerChanged()` (private: Auto Tidy's change trigger), `Container.GetHoverText`, `Container.OnDestroyed` (the owner removes the
container's sign), `Container.SetInUse(bool)` (the user name), `CookingStation.GetHoverText`,
`CraftingStation.GetLevel(bool)`, `CraftingStation.Interact(Humanoid, bool, bool)` (Homestead's auto repair on the
local player's client once the game made the station current; carts with a workbench open theirs through it too),
`Fermenter.GetHoverText`, `Fireplace.Awake` (Homestead registers
`OpenKeep_TorchKeepLit` on every fire's net view), `Fireplace.GetHoverText` (Reach's From storage line; Homestead's
Lights at nightfall and the Torch Switch Key line), `Fireplace.UpdateFireplace()` (private, every 2 s on every client with the fire
loaded; on the ZDO owner only: Homestead's torch switch with `Priority.High`, then auto fuel),
`Smelter.Awake` (Capacity: a station made from another copy of its prefab gets the caps),
`Smelter.UpdateSmelter()` (private, every second on every client with the station loaded; on the ZDO owner only:
Homestead's auto feed),
`Game.RemoveCustomSpawnPoint(Vector3)` (forget a destroyed bed), `Minimap.UpdateProfilePins()` (private: the other
known beds as bed pins),
`InventoryGrid.OnLeftDown(UIInputHandler)` (touches), `InventoryGrid.UpdateGui(Player, ItemData)` (marks; touch
tint), `InventoryGui.Awake` (Stow buttons; Salvage tab; Batch stepper), `InventoryGui.CloseContainer` and `InventoryGui.Hide` (end
of viewing), `InventoryGui.SetupRequirement` (static, six parameters), `InventoryGui.Update` (Stow hotkeys; Salvage
Key; touch end), `InventoryGui.UpdateRecipe(Player, float)` (Salvage panel; Batch stepper layout), `Localization.SetupLanguage`, `MonsterAI.UpdateConsumeItem(Humanoid,
float)` (private, in the AI update on the creature's owner: Homestead's pets eat from containers), `ObjectDB.Awake`, `Sign.Awake` (the
orphan check component on automatic signs; their `WearNTear` wear switched off),
`ObjectDB.CopyOtherDB` (both `Priority.Low`), `Player.GetFirstRequiredItem`, `Player.HaveRequirementItems`,
`Player.HaveRequirements(Piece, RequirementMode)`, `Player.OnDeath` (the nearest own bed becomes the spawn point;
the choice of bed opens, or Quick Respawn moves the respawn request),
`ZoneSystem.Update()` and `ZNetScene.CreateDestroyObjects()` (private: Quick Area Loading for the respawn, installed
by name by the AreaLoading library, `AreaLoader.Install` in `Plugin.Awake`),
`Player.OnSpawned(bool)` (waiting bed changes written, the fallback dropped; after a death, no getting-up
animation, a class of its own), `Player.PlacePiece`, `Player.Update` (Reach keys; Dump Key;
request timeouts; Torch Switch Key), `Switch.GetHoverText`, `Terminal.InitTerminal`, `Vagon.Awake`, `Vagon.GetHoverText`, `WearNTear.Remove(bool)`
(the hammer opt-out on the removing player's client), `ZNetScene.Awake` (documentation; container list and sizes;
station list and caps; Homestead's Build On Wood; all `Priority.Low`, none throws: `SceneSafe`), `ZNetScene.OnDestroy()`
(private: Core forgets the tracked containers when the world is left).
Prefix: `ZDOExtraData.PrepareSave()` and `ZDOMan.AddObjectsPerChunk(int, byte, int[], ref List<...>)` (private; both on
the machine that saves the world, Quick World Save, fall back to the game on a throw), `Container.Interact(Humanoid, bool, bool)` (the read-only open), `Container.OnContainerChanged()` (private,
`Priority.First`: a held container's save waits for the end of the hold, `SaveHolds`), `Inventory.Changed()` (private:
the inventory change count), `Game._RequestRespawn()` (private: the
choice of bed ends), `Game.FindSpawnPoint(out Vector3, out bool, float)` (Quick Respawn's load speed, a class of its
own beside the prefix and postfix below), `Hud.UpdateBlackScreen(Player, float)` (private: no black screen during
the choice of bed), `Minimap.OnMapLeftClick()` (during the choice of bed a click picks a bed, `Priority.First`; the
MapClicks library's `IconClick.Install` in `Plugin.Awake` adds, by name, a `Minimap.OnMapDblClick()` prefix and a
`Minimap.Update()` postfix that hold the pick through the double click window), `Minimap.Update()` (private: the map during the choice of bed, skipping the game's update),
`Container.RPC_OpenResponse(long, bool)`
(a refusal is silent while viewing), `InventoryGrid.DropItem(Inventory, ItemData, int, Vector2i)` (Shared,
`Priority.First`, zeroes the amount for a viewed chest; Merge Into Chests), `InventoryGui.OnCraftPressed` (Pull
modifier; Salvage tab; Batch, with a postfix too; a postfix asks for the chests a started craft pays from), `InventoryGui.UpdateRecipe(Player, float)` (Batch drives the
game's multi-craft fields), `InventoryGui.OnRightClickItem(InventoryGrid, ItemData)` (refused on a viewed chest),
`InventoryGui.OnSelectedItem` (Shared, `Priority.First`; Stow's Route Modifier), `InventoryGui.OnStackAll`,
`InventoryGui.OnTakeAll`, `InventoryGui.OnTabCraftPressed`, `InventoryGui.OnTabUpgradePressed`,
`InventoryGui.DoCrafting(Player)` (private: the payment window; refused while storage cannot pay now, `ReachReady`),
`Player.TryPlacePiece(Piece)` (`Priority.Low`, after the Blueprints tab's: waits while storage cannot pay now),
`InventoryGui.UpdateContainer(Player)` (the viewer's panel), `InventoryGui.UpdateRecipeGamepadInput`,
`Inventory.IsTeleportable(bool)`, `Inventory.RemoveItem(string, int, int, bool)` (the payment hook),
`SE_Cozy.UpdateStatusEffect(float)` (the Resting effect's tick on the player's own client: `Rested Delay` into the
copy's `m_delay`), `Vagon.Interact`.
Prefix and postfix: `Container.SetInUse(bool)` (Auto Tidy: the contents at open and at release), `Bed.Interact(Humanoid, bool, bool)` (an own bed becomes the spawn point, then the game's sleep
path; remember), `Beehive.UpdateBees()` (honey rate and progress on the hive's ZDO owner),
`CookingStation.OnAddFuelSwitch`, `CookingStation.OnInteract(Humanoid)`, `Fermenter.Interact`, `Fireplace.Interact`,
`Game.FindSpawnPoint(out Vector3, out bool, float)` (a cleared point is forgotten and the next nearest bed set),
`InventoryGui.Show(Container, int)` (auto sort),
`InventoryGui.UpdateCraftingPanel(bool)`, `Player.Repair(ItemDrop.ItemData, Piece)` (private; area repair on the
repairing player's client once the game's own repair went out), `Smelter.OnAddFuel`, `Smelter.OnAddOre`.
Build Camera: postfix `Player.Update` (toggle, end conditions), `Player.FixedUpdate` (private: camera pickup),
`GameCamera.LateUpdate` (private: head light, panel), `GameCamera.UpdateListner` (private: the listener stays on the
camera); prefix `GameCamera.GetCameraPosition(float, out Vector3, out Quaternion)` (private: skipped while the camera
is out), `LightLod.GetLightReferencePoint()` (private static: the camera while out), `Player.SetMouseLook(Vector2)`,
`Player.SetControls(...)` (twelve parameters); prefix and finalizer `Player.PieceRayTest(out Vector3, out Vector3, out
Piece, out Heightmap, out Collider, bool)`, `Player.RemovePiece()`, `Player.CopyPiece()`,
`Player.UpdateWearNTearHover()` (all private: `m_maxPlaceDistance` swapped for the one call, put back in the finalizer).
Recipe List: postfix `InventoryGui.Awake` (the typing watch; Track and favourite buttons), `InventoryGui.UpdateRecipeList(List<Recipe>)`
(private: filter, favourites first, layout, row hooks), `InventoryGui.Update` (Search Key, F over a row, the search's
debounce), `InventoryGui.Hide` (Clear Search On Close), `InventoryGui.UpdateRecipe(Player, float)` (the Track and
favourite buttons); prefix `InventoryGui.UpdateCraftingPanel(bool)` (`Priority.First`, and a postfix: the search row
and the list's height), `InventoryGui.UpdateRecipeGamepadInput()` (private: right stick shortcuts; grid stepping,
skipping the game's), `InventoryGui.Update()` (`Priority.First`: while a panel field is focused, the Use and Inventory
presses dropped and a frame with Escape skipped). Batch's `UpdateRecipe` prefix also sets the craft durations (Craft
Speed). Recipe Tracker: postfix `Hud.Awake()` (private: adds the tracker's driver); prefix (`Priority.Low`, after
Batch has put the started amount back) and postfix `InventoryGui.DoCrafting(Player)` (a tracked recipe made).
Prefix and finalizer (the payment window): `InventoryGui.DoCrafting`, `Player.ConsumeResources`. Batch has its own
prefix (`Priority.First`, the started craft's amount) and finalizer on `InventoryGui.DoCrafting`.
Blueprints: postfix `TerrainComp.Awake()` (private: the ground RPC); prefix
`Player.TryPlacePiece(Piece)` (`Priority.First`: a blueprint entry's click), `Player.AddKnownPiece(Piece)` (private:
the entries known without a message) and `Player.RemovePiece()` (private, `Priority.First`: no removal for the local
player while an entry of the tab is selected; HarmonyX still runs Build Camera's prefix and finalizer there); postfix
`BuildUi.Awake()` (private: the Blueprints tab); prefix and postfix `BuildUi.OpenBuildMenu()` (the tab shown or hidden);
prefix `BuildUi.OnSelectPiece(Piece)` (Ctrl / Shift + click picks and the menu stays open; nothing while a name box
is up), `BuildUi.Update()` (private: skipped while a name box is up over the menu;
otherwise a right click on a blueprint of the grid or a folder of the panel or the breadcrumb renames it and skips that
frame of the menu), `TabHandler.Update()` (private: the build menu's own tab keys held while a name box is up) and
`BuildUi.PressedFavoriteButton(BuildUiPieceButton)` (no favourites for the entries); postfix
`BuildUi.UpdateTagButtons(bool)` (private: the folder panel and breadcrumb follow the game's tag column); postfix
`BuildUiPieceButton.Setup(Piece, BuildUi)` (name band, pick mark and drag handle follow the piece); prefix
`Player.SetControls(...)` (`TabCtrlPatch`: a Ctrl + click in the Blueprints tab does not toggle sneaking); postfix
`GetAvailablePiecesWithTag(int, PieceTable, IList<Piece>)` of `ByUsagePieceList`, `ByMaterialPieceList`,
`RecentPieceList` and `FavoritePieceList` (the entries only in their own tab); prefix and postfix
`ConnectPanel.Update()` (private: an F2 that renames leaves the connection panel as it was); postfix `ZNetScene.Awake()` again for the site post and `ZNet.Awake()` (the site
RPCs every machine answers); prefix `Chat.Update()` (skipped for the one frame Enter queues a planner selection);
prefix `Chat.Update()` again (`ChatEnterPatch`: skipped for a frame a Copy save takes Enter); prefix
`WearNTear.Highlight()` (the game's hover tint held back while Copy building is selected) and postfix
`WearNTear.ResetHighlight()` (private: a piece the Copy tool lights gets its glow back);
WindowInput's own patches (its Harmony id `milkyteam.openkeep.planner`) while the planner's panel is open.

## Config sections and keys

`General` (`Lock Configuration`), `0. Containers` (`Ships`, `Carts`, `Player Chests`, `Honour Wards`, `Shared
Chests`: the enum `Off`, `View`, `Full`, default `Off`), `1. Reach` (`Enabled`, `Range`, `Crafting`, `Building`,
`Upgrading`, `Feed Stations`, and the YAML `stations:` map; unsynced `Fill Modifier`, `Pull Modifier`, `Toggle Key`, `Show Links`, `Link Key`,
`Link Seconds`, `Requirement Display`, `Storage Colour`, `Flash On Pull`), `2. Stow` (`Enabled`, `Quick Stack
Nearby`, `Nearby Range`, `Ground Pickup`, `Pickup Range`, `Pickup Interval`, `Pickup Delay`, `Pickup Only Held
Items`, `Auto Tidy` false; unsynced every key, `Sort Order`, `Sort Favourite Items`, `Auto Sort Containers`, `Auto Sort Inventory`, `Confirm Trash`, `Trash
Uses Salvage`, `Cycle With Wheel`, `Show Favourites`, `Button Row Offset`, `Trash Can On Stat Column`), `3. Salvage` (`Enabled`, `Return Fraction`, `Rounding`, `At
Least One`, `Upgrade Materials`, `Require Known Recipe`, `Require Station`, `Skip Items With Mod Data`, `Mod Data
Prefixes`; unsynced `Salvage Key`), `4. Stacks`
(`Enabled`, `Stack Multiplier`, `Weight Multiplier`, `Ignore Teleport Restriction`, `Merge Into Chests`, `Per Item
Config Entries`, `Write Documentation`), `4a. Item Stacks` and `4b. Item Weights` (`<prefab>.Stack`,
`<prefab>.Weight`, only with Per Item Config Entries), `5. Capacity` (`Enabled`; unsynced `Hover Contents`, `Hover
Lines`, `Hover Fill`), `6. Carts` (`Cart Workbench`, `Cart Station Level`, `Cart Station Range`), `7. Signs`
(`Enabled` false, `Show Counts` false, `Max Items` 4, `Max Characters` 50, `Update Seconds` 2, `Height` 0.1,
`Rotation` 0, `Empty Text` empty; all synced), `8. Homestead` (`Nearest Bed Respawn` true, `Bed Choice Seconds` 30 (0 to 60), `Quick
Respawn` true, `Quick Respawn Range` 1000 (10 to 20000), `Quick Respawn Seconds` 1 (0 to 18), `Stand Up On
Respawn` true, `Build On Wood`
`fire_pit`, `Honey Per Day` 0, `Honey Per Player Online` false, `Auto Fuel` true, `Auto Fuel Range` 20, `Torches Night Only` true,
`Torch Pieces` `piece_groundtorch_wood, piece_groundtorch, piece_groundtorch_green, piece_groundtorch_blue,
piece_walltorch`, `Torch Margin` 1 (in-game hours, 0 to 4), `Auto Feed Stations` true, `Auto Feed Range` 4, `Auto Feed Skip` `FineWood, RoundLog`, `Auto Feed Leave` 1 (0 to 1000), `Rested Delay` 5 (seconds, 0 to 60), `Area Repair` true, `Auto Repair` true, `Pets Eat From Chests` true, `Pet Chest Range` 10 (1 to 30), `Quick World Save` true; all synced; unsynced `Beds On Map` true, `Quick Area Loading` true (the respawn only),
`Torch Switch Key` O),
`9. Shared` (`Request Timeout` 2 s, `Touch Seconds` 5 s, both
synced; unsynced `Show Touches` true, `Touch Colour` `#ffb347`), `10. Batch Crafting` (`Enabled` true, `Max Amount`
100 (1 to 1000), `Craft Speed` 1 (0.1 to 10); all synced), `11. Build Camera` (`Enabled` true, `Range Multiplier` 1 (0.25 to 5), `Extra Reach` 5 m
(0 to 45), `Camera Pickup` true, `Entry Needs Resting` false, `Entry Min Comfort` 0, `Pickup Needs Resting` false,
`Pickup Min Comfort` 0 (comfort 0 to 50); all synced; unsynced `Toggle Key` B, `Gamepad Toggle` `JoyAltKeys +
JoyRStick`, `Speed` 10, `Run Multiplier` 3, `Circlet Light` true, `Circlet Intensity`, `Circlet Range`, `Circlet Spot
Angle` (0: the circlet's own), `Pickup Panel` true, `Pickup Panel Position` (0, -120)), `12. Recipe List` (`Search`
true, `Search Key` LeftControl + F, `Clear Search On Close` true, `Favourites` true, `Favourites First` true, `Recipe
View` `List` (`List`, `CompactList`, `SmallGrid`, `MediumGrid`, `LargeGrid`), `Gamepad Controls` true; all unsynced),
`13. Recipe Tracker` (`Enabled` true, `Max Tracked` 6 (1 to 12), `Count Nearby Chests` true, `Untrack When Crafted`
true, `Hide In Combat` true, `Hide With Map` true, `Scale` 1 (0.5 to 2), `Font` `Sans` (`Sans`, `Serif`, `Norse`),
`Font Size` 16 (10 to 28), `Have Colour` `#FFFFFF`, `Missing Colour` `#FF6A5A`, `Ready Colour` `#FFB65C`, `Background
Opacity` 0.56, `Position` empty; all unsynced), `14. Blueprints` (`Enabled` false, `Build Without Materials` false,
`Build As Resources Come In` true; all synced; the keys are fixed).
Keys, defaults and meanings are in each entry's description in the .cfg (bound in the modules' `*Settings.cs`; the
README only names the features). Every setting of the spec is bound with the spec's section, key,
default and sync flag; the one addition is `2. Stow / Enabled` (synced, true), so every module has a master switch.

## Network and file names

- ZDO keys: `OpenKeep.user` (string): the name of the player using a container, written by the owning client in
  the `Container.SetInUse` postfix when the container is taken into use and cleared when it is released (compared
  before writing, so the ZDO changes only when the name changes). Read only: the game's `InUse`, `spawntime`
  (and `fuel` for Reach; Homestead's auto fuel adds fuel through the game). `OpenKeep.cartOffset` is reserved for the unbuilt cart extension piece. Signs: on a container
  `OpenKeep.sign` (ZDOID, its sign) and `OpenKeep.noSign` (bool, the hammer opt-out); on a sign `OpenKeep.signOf`
  (ZDOID, its container) and `OpenKeep.autoText` (string, what the mod last wrote); the sign's text goes into the
  game's own `text` key with `author` and `authorPlatformDisplayName` set to empty strings, and the piece's
  `creator` long is the local player's id. Beehives: `OpenKeep.hiveSecPerUnit` (float), the seconds per honey the
  hive's `product` was counted at, written by the hive's owner once the mod's rate has run; the game's `product` is
  written, `lastTime` and `level` only read. Beds write no ZDO key: the game's own `owner` is the only shared state. Fires: `OpenKeep.torchPhase` (int: 0 never
  switched, 1 put out for the day, 2 lit for the night), written by the fire's ZDO owner at nightfall and daybreak and
  set to 0 when `Torches Night Only` is off, the prefab leaves `Torch Pieces` or the torch is kept lit;
  `OpenKeep.torchKeepLit` (bool): a player keeps the torch lit day and night, written only by the fire's ZDO owner
  when it handles `OpenKeep_TorchKeepLit`; written as false rather than removed, because `ZDO.RemoveInt` does not
  raise the data revision and a removal would never reach the other clients; left alone when the setting is off or
  the prefab unlisted. The game's `fuel` is written only through `Fireplace.AddFuel` (`RPC_AddFuelAmount`), `state`
  only through `RPC_ToggleOn`.
- Auto Tidy: `OpenKeep.tidy` (byte array) on a container, written only by its ZDO owner at a look when something
  changed: a `ZPackage` of version (int, 2), count (int), then per prefab its name (string), first seen (double, world
  seconds), peak stacks (float) and its time (double), stacks at the last look (float), flags (byte: 1 put in by hand,
  2 kept) and the time Auto Tidy last sent it away (double).
- Hand-over: `OpenKeep_HandOver` (no payload) is registered on every container's net view and sent to the ZDO owner,
  which, when nobody uses the chest and it is no ship or cart storage, force sends the ZDO to the asker and sets it as
  owner (the game's own grant of an open). It replaced `OpenKeep_TidyHandOver` (2026-10-06).
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
- `OpenKeep_TorchKeepLit` (bool: keep lit, or back on the schedule), registered on every fire's net view in a
  `Fireplace.Awake` postfix (once per view, guarded by `m_functions`), sent by the player's client to the fire's ZDO
  owner with `ZNetView.InvokeRPC` after claiming an unowned fire as `Fireplace.Interact` does; the wanted state, not
  a toggle. Area repair and Auto Feed send only the game's own RPCs (`RPC_Repair`, `RPC_AddOre`, `RPC_AddFuel`);
  Auto Repair sends nothing (durability and skills live in the player's own inventory and profile, as for the game's
  repair button).
- Item packet: the prefab name (empty when the item has no prefab; the reader then returns null), followed by the
  game's own `ItemDrop.ItemData.Save` fields: durability x100, grid x, grid y, world level, a flag byte (picked
  up, equipped, quality, stack, variant, crafter, prefab, custom data), then only the flagged ones: quality,
  stack, variant, crafter id and name, prefab hash, custom data, and the cheated flag. The reader clones the
  prefab's item and runs `ItemDrop.ItemData.Load` on it, as `Inventory.Load` does.
- Player custom data: `OpenKeep.reachOff` (flag), `OpenKeep.favouriteItems`, `OpenKeep.favouriteSlots` (`x:y`),
  `OpenKeep.junk` (comma separated sets, keyed by prefab name), `OpenKeep.beds.<world uid>` (comma separated set of
  bed spawn points `x:y:z`, invariant culture, two decimals; the world uid is `ZNet.GetWorldUID`, the key of the
  profile's own per-world spawn point), `OpenKeep.favouriteRecipes` (recipe keys: the recipe asset's name, else its
  item's prefab name), `OpenKeep.favouriteRecipesOnly` (flag), `OpenKeep.trackedRecipes` (entries `key|quality|amount`,
  `|u` added for one tracked at an upgrader station, in tracking order). Read only: PackPanel's `PackPanel.mainGrid` (see "PackPanel").
- Charter article names: `openkeep_reach`, `openkeep_stow`, `openkeep_salvage`, `openkeep_stacks`, `openkeep_containers`,
  `openkeep_signs`, `openkeep_stations` (the YAML sets) plus the cfg sync of the shared libraries. Container ownership goes through the game's
  `ZNetView.ClaimOwnership` and `ZDOMan.ForceSendZDO`; the game's own `RPC_RequestOpen` is re-sent by a viewer.
- GameObjects created: `OpenKeep.ReachLink` (link lines), `OpenKeep_link` (Find marker), `OpenKeep_SalvageTab`,
  `OpenKeep_<word>` and `OpenKeep_trash` (panel buttons), `OpenKeep_border`, `OpenKeep_star`, `OpenKeep_cross`
  (slot marks), sprites named `OpenKeep_sprite`, `OpenKeep_trashcan` (the trash can in the button row, with
  PackPanel), `OpenKeep_trashcursor` (trash mode's pointer), and `Trash` (the trash can's
  plate, a direct child of the player panel, without PackPanel). Read only: PackPanel's `PackPanel_buttonstrip`. Batch: `OpenKeep_BatchStepper` with `OpenKeep_BatchLess`,
  `OpenKeep_BatchAmount` and `OpenKeep_BatchMore` beside the Craft button. Build Camera: `OpenKeep_CameraCirclet` (a
  Light under the game camera), `OpenKeep_CameraPickup` with `OpenKeep_CameraPickupText` (under the HUD root). Recipe
  List: `OpenKeep_RecipeSearch` (beside `RecipeList` in the crafting panel) with `OpenKeep_RecipeSearchField`,
  `OpenKeep_RecipeOnly` and `OpenKeep_RecipeView`; `OpenKeep_TrackButton` and `OpenKeep_FavouriteButton` beside the
  Style button; `OpenKeep_star` on favourite rows; `OpenKeep_icon` on its buttons; a `TypingWatch` component on the
  inventory's object. Recipe Tracker: `OpenKeep_Tracker` under the HUD root with `OpenKeep_TrackerTitle` and one
  `OpenKeep_TrackerEntry` per recipe; a `TrackerHud` component on the HUD's object. The cart's station is a `CraftingStation` component on the cart
  instance, no new prefab. Shared creates none: touches recolour the grid's icons. Signs instantiates the game's
  own `sign` prefab (a normal piece, no new prefab) and adds a `SignOrphanCheck` component to loaded automatic signs.
- Files next to the cfg: the seven YAML files, `OpenKeep.Items.txt`, `OpenKeep.Containers.txt`,
  `OpenKeep.Stations.txt`.
- Localization keys: `$ok_*` (Reach: `ok_fromstorage`, `ok_reach`, `ok_on`, `ok_off`, `ok_pulled`,
  `ok_nothingtopull`, `ok_nofit`; Stow: `ok_stow_*`, including `ok_stow_moved_to` and `ok_stow_toppedup_from` for
  a shared chest's reply and `ok_stow_routed_more` for a stack that went on to further containers; Salvage: `ok_salvage*`; Capacity: `ok_slots`, `ok_full`, `ok_and`, `ok_more`; Carts:
  `ok_cartcraft`; Shared: `ok_shared_inuse`, `ok_shared_moving`, `ok_shared_noanswer`, `ok_shared_denied`,
  `ok_shared_readonly`, `ok_shared_someone`, `ok_shared_nofit`, `ok_shared_chestfull`, `ok_shared_unavailable`;
  Signs: `ok_signs_sign`, `ok_signs_playertext`, `ok_signs_nosign`, `ok_signs_optedout`, `ok_signs_reset`,
  `ok_signs_rewritten`, console output only; the sign text itself is plain text); Homestead: `ok_bedchoice_nearest`, `ok_bedchoice_click`, `ok_bedchoice_keys`, `ok_torch_nightfall`, `ok_torch_keeplit`,
  `ok_torch_nightonly`, `ok_torch_kept`, `ok_torch_scheduled`, `ok_repair_one`, `ok_repair_many`,
  `ok_autorepair_one`, `ok_autorepair_many`; Build Camera: `ok_cam_nostation`, `ok_cam_needs`, `ok_cam_pickupneeds`,
  `ok_cam_resting`, `ok_cam_comfort`; Recipe List: `ok_recipe_*`; Recipe Tracker: `ok_tracker_*`.
- Console: `openkeep reload`, `openkeep containers`, `openkeep write docs`, `openkeep signs`, `openkeep signs reset`,
  `openkeep signs rewrite`, `openkeep tidy`, `openkeep blueprint list | save <name> [radius] [all] [replace] | undo`.
- Blueprints: RPC `OpenKeep_BlueprintGround` (on each terrain compiler's own view, to its owner); local menu entries
  (never networked or placed) `OpenKeep_Blueprint_<path, all but letters and digits as _>_<hash>`,
  `OpenKeep_FixGround`, `OpenKeep_SitePlanner`, `OpenKeep_Copy`, `OpenKeep_GhostSwitch` (their `$ok_bp_entry_*` and tool names land in the
  player's known recipes);
  folder `BepInEx/config/OpenKeep.Blueprints/**/*.json` (its subfolders are the tab's folders), embedded defaults
  `OpenKeep.blueprints.<file>`; words `ok_bp_*` (the tab `ok_bp_tab`), `ok_fix*`, `ok_site_*`, `ok_planner*`, `ok_copy*`;
  this machine's tab memory `BepInEx/config/OpenKeep.BlueprintsTab.txt` (`folder=`, `tab=`, `ghosts=shown|hidden`,
  never synced).
- Construction sites: prefab `OpenKeep_Site` (networked post); its ZDO keys `OpenKeep.site_bp`, `site_name`,
  `site_origin`, `site_yaw`, `site_built`, `site_queue`, `site_store`, `site_ground`, `site_groundStone`,
  `site_creator`, `site_creatorName` (all `OpenKeep.`); RPCs on the post `OpenKeep_SiteDeliver` (request id, then the
  amounts), its answer `OpenKeep_SiteDelivered` (id, yes or no, reason), routed `OpenKeep_SiteTakeOver` (to the
  server: a site it owns, by ZDO id),
  `OpenKeep_SiteTakeDown`, `OpenKeep_SiteQueue`; routed `OpenKeep_SiteTakeDownAsk` (to the server) and
  `OpenKeep_SiteBuilt` (to everyone, shown within 40 m).

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
- `Claim` (`ContainerClaim`): a container the local client owns is used at once; one nobody owns is claimed with
  `ZNetView.ClaimOwnership` (the game's way for a free object); one another client owns is never taken locally: its
  owner is asked (`OpenKeep_HandOver`, `HandOver.Ask`, at most every 1 s for a player's action, 30 s for Auto Fuel,
  Auto Feed, pets and Auto Tidy) to hand it over with its latest data, and `Claim` returns false until it is ours, so
  our older copy never overwrites the last owner's change. Ship and cart storage is never claimed or asked for: it is
  usable (`IsReady`) only while the local client owns the vehicle, and otherwise written only by request to its owner
  (Stow, through `ChestWriter`). Then `Container.Load` reads the latest data (a no-op when the data revision has not
  changed). `Save` calls `Inventory.Changed()`, the game's own path to the ZDO, and logs a
  warning when the client does not own the container.
- Save holds: a quick stack, store all, top up, sort, a ground pickup sweep, every `ChestSync` call and every
  container Auto Fuel, Auto Feed and pets take from hold the container's saves (`SaveHolds`, in a `using`): the
  game's save after each change is skipped, `Claim` loads only on the first claim of the hold (the held inventory is
  newer than the ZDO), and the end of the outermost hold saves once. Only synchronous work is held, so no other client's change can arrive meanwhile.
  Auto Tidy's moves are not held (their saves must happen while its `Moving` flag is set).
- A private chest without a `Piece` is unusable (the game's privacy check would need the piece's creator).
- The ward check never flashes the ward. `ContainerUse` is accepted but does not distinguish rules yet.
- Vocabulary: an unknown keyword (`foo:`) or an unknown `type:` name is an invalid entry with `Problem` set; it
  matches nothing and the YAML model warns. `ItemGroups` warns about invalid members and members naming unknown
  groups; cycles are tolerated through a visited set.
- `Keys`: a single key fires only with no Shift, Ctrl or Alt held, so `F` and `LeftShift + F` never fire
  together. `TextInputActive` covers chat, console, the sign text input,
  any selected input field and the YAML editor. `CharacterData` strips commas from set members.
- `openkeep reload` is allowed with no `ZNet` or when the local player is admin or host; it runs
  `Config.Reload`, `Yaml.LoadAll`, `Yaml.ApplyAll`. `openkeep containers` lists within 20 m.
- Keys (Core, 1.8.0): a shortcut with modifiers is now its main key down with all its modifiers held and no other
  Shift, Ctrl or Alt, as EarthWright does; BepInEx's `IsDown` refused it while any other key (W) was held. After
  2.2.1 `Keys` passes every read to the workspace's Hotkeys library (`Hotkey.Pressed`, `Hotkey.Held`,
  `Typing.Active`): the same rules, modifiers read through the game's `ZInput` (one let go in another window is not
  stuck), the main key tested first.

### Reach

- Payment: both game pay paths end in `Inventory.RemoveItem(name, amount, quality, worldLevel)` on the player
  inventory (`Player.ConsumeResources`, and `InventoryGui.DoCrafting` directly for single ingredient recipes), and
  both run after the crafted item or the piece exists. `ConsumeResources` and `DoCrafting` open a depth-counted
  payment window (mode Building for quality 0, Crafting for 1, Upgrading above; `DoCrafting` sets Crafting or
  Upgrading from `m_craftUpgradeItem`); inside it the `RemoveItem` prefix computes the shortfall against the same
  name, quality and world-level filter the game removes with, takes exactly that from reachable containers nearest
  first (claim, remove per stack honouring allow/deny, save), and the game then removes what the inventory has.
  Finalizers close the window even after an exception. Before a craft (`DoCrafting` prefix) and a placement
  (`TryPlacePiece` prefix) `ReachReady` checks in the same frame that the inventory plus the containers the client may
  change now (owned, or owned by nobody) cover every shortfall; if not, every other reachable container holding the
  item is asked for (`HandOver`) and the craft or placement is refused with "Fetching the materials from storage, try
  again" (the game's missing-requirement message when nothing holds it any more). Pressing Craft asks at once, so the
  hand-over usually arrives within the craft's progress bar. A payment from another mod's path that falls short is
  logged. Each paying container is held (one save). The build panel passes quality 0 to
  `SetupRequirement`, so its rows use the Building switch. Reachable means `ContainerScan.IsUsable`: a chest another
  player is using is neither counted nor paid from, in every `Shared Chests` mode.
- Counting: the reach list (`ReachChests`) re-runs the section 0 rules (switches, prefab table, privacy, the ward
  scan) over its candidates every 0.25 s and at once on a woken container, `ReachRules.Apply`, a new local player,
  4 m moved or a wider range; loaded, in use by another player, still there and range run every frame, so leaving
  range or another player opening a chest counts at once, while a ward toggle, a changed permitted list or a section 0
  switch reaches Reach within 0.25 s. Requirement counts come from `StorageIndex`: one walk of the reachable
  containers per change (a new list instance, which includes the 0.25 s refresh as a safety net for changes that
  bypass `Inventory.Changed`; any `Inventory.Changed`, so payments, pulls, drags and another player's change loaded
  from the ZDO; a new world level). Payment, Pull, Borrow and Fill read the containers live; the station hover's
  "From storage" line is counted live but kept while the hovered object, the reach list, the inventories and the
  world level stay the same (`StationHover.Line`). A
  requirement row counts storage only when the inventory alone is short.
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
- The `stations:` map (1.2.0) narrows feeding only: `StationAccepts` composes the rule's allow/deny into every
  accepts predicate (so Borrow, Fill, Pull and the hover count all honour it), `StationFeed.Wanted` and
  `StationHover.Shows` gate `enabled` per station. The station's prefab name is its net view object's, as for
  containers. Crafting, building and upgrading counts are deliberately untouched — the request behind it was
  "don't cook my meats", not "hide them from recipes".
- Toggling with the Toggle Key applies at once (the flag is read at use time).

### Stow

- `Enabled` (synced, true) was added as the module's master switch.
- Favourite slots are stored as `x:y` because the sets are comma separated.
- Sorting never touches the hotbar row (row 0) of the player inventory; favourite slots, equipped items and
  stacks with a put under way are pinned; containers pin nothing. Stacks of the same item, quality and world level
  merge while sorting. With `Sort Favourite Items` off (1.2.0, default on) favourite items are pinned too, for
  extra cells a mod keeps inside the game's rows.
- Hotbar (1.4.2, `MainGrid.FirstRow`): quick stack, store all and dump never take from the hotbar row (row 0) of the
  player inventory either, the same row the sort has always left alone (the user's call, 2026-09-25). Store one,
  Route, Trash and top up still act on it.
- Main grid (1.4.0, `MainGrid`): quick stack, store all, dump and the player sort work only on the rows the game
  gives the player: its unique key `invrows` (`Player.InventoryRowsKey`, set by `Player.SetInventorySize`, 4 until
  set, more once rows are bought from the trader). Rows below belong to other mods: nothing is taken from them and
  the sort places nothing there. Top up is deliberately not limited, so food and ammo slots are refilled (the
  user's call, 2026-09-24). Store one, Route, Trash, Destroy Junk and Salvage act on an item the player chose and
  are not limited either. `Main Inventory Rows` (per player, 0 = the game's rows, 1-20 a fixed count) covers mods
  that add ordinary rows without the key. Observed with ExtraSlots in Ravenholt: the character's `invrows` is 4 and
  its equipped helmet, chest and legs sit in cells (0,6), (1,6) and (2,6) of the player `Inventory`, read from the
  character save's inventory block (the game's own format), not from the mod.
- Dump Key requires `Quick Stack Nearby`; with it off the centre message says so.
- Trash mode (1.8.0, the user's idea): Shift + click on the trash can (with nothing dragged) turns the pointer into
  the can; each click on a stack of either grid destroys the whole stack through `Trash.Destroy` with the trash's
  refusals (favourites, worn items, a viewed chest; `Trash Uses Salvage` salvages) and no confirmation, the held Shift
  being the confirmation. An `InventoryGui.OnSelectedItem` prefix at `Priority.First + 1` takes the click (with Shift
  held the game would open its split dialog); Shared's and Stow's other click prefixes leave split clicks alone.
  Letting go of Shift, closing the inventory, a popup or a drag ends it (`StowHotkeys` ticks it first every frame).
  The pointer: the game never sets a cursor image, it only shows the system arrow every frame (`ZCursor.SetVisible`),
  so hiding the arrow does not stick; the arrow is swapped for an 8x8 transparent cursor (`Cursor.SetCursor`) and
  restored with `SetCursor(null)`, and the can is a 56 unit image child of the inventory screen placed at
  `ZInput.pointerPosition` every frame, as the game places a dragged item.
- Favourite items are refused by Store one and Route; Top up still refills them.
- Trash from the container grid is allowed (the container is claimed and saved). `Trash Uses Salvage` applies only
  to a whole stack of the player inventory.
- Route Modifier + click on the player grid replaces the game's own modifier click (which moves the stack to the
  open container); the open container leads the candidates, so when it holds the item the stack still goes there,
  and when no nearby container holds the item it takes the stack before any group or `accept` match.
  Clicks with a dragged item, on the container grid, or with the game's drop modifier stay the game's.
- Cycling is limited to `min(Nearby Range, InventoryGui.m_autoCloseDistance)` (4 m) because the panel closes any
  container farther away; the ring is sorted by `atan2` around the player; the wheel has a 0.25 s cooldown and
  only counts over the container grid, and not while EliteCrafting scrolls a long tooltip (its `ecf_tooltip_bar`
  shows in `UITooltip.m_tooltip`, found by name). Opening goes through `Container.Interact` (the game's RPC path).
- Ground pickup runs in the `Container.CheckForChanges` postfix (once a second per container) on the owner client,
  staggered per container, skips containers in use, containers not usable by the local player and placed pieces
  (`ItemDrop.IsPiece`); the age comes from the drop's `spawntime` ZDO value against `ZNet.GetTime`; rule order is
  refuse, accept, then the only-held rule; a drop this client owns (or nobody owns, then claimed) is loaded, added
  with `AddItem`, then destroyed through `ZNetScene` or reduced and saved; a drop another client owns is asked for
  with the game's `ItemDrop.RequestOwn` (its own backoff) and taken on a later sweep. Unchanged by the Shared module.
- Buttons are clones of `m_takeAllButton`: Quick stack, Store all, Top up and Sort, 78x26 px,
  anchored bottom-centre of `m_player` with the pivot at the bottom; one Sort centred at the bottom of
  `m_container`. Both hang below their panel's bottom edge: `anchoredPosition.y` is `-(26 + 4)`, so the top of a
  button sits 4 px under the edge, plus the per player `Button Row Offset` (default 0, positive up). Inside the
  panels nothing is free: `InventoryGui.SetInventorySize` grows `m_player` by exactly one grid row per inventory
  row, so the last item row sits on the panel's bottom edge (a row 6 px up covered it), and `m_container` ends
  with the take-all line. The game's scene makes `m_container` a child of `m_player`, anchored to its bottom-left
  corner at (0, -30): the row fills that gap with 4 px to spare above it, so with a chest open a negative offset
  overlaps the container panel and one above 4 overlaps the player panel. `PanelButtons` keeps the
  placed rects and `Reposition` (from `StowModule`, on `SettingChanged`) re-places them without a restart. The
  hovered item mirrors `InventoryGrid.UpdateGui`'s tooltip choice (gamepad selection, else the hovered element).
- The stat plates are the game's `Player/Armor` and `Player/Weight` (read from the game's scene with UnityPy on
  2026-09-24), the direct children of `m_player` holding `m_armor` and `m_weight`. Each is 80x64 with three
  children: the wood (`bkg`, sprite `woodpanel_flik`, the largest Image child), a 32 px icon poking 14 px above the
  plate (`ac_bkg_large`, 64 px source; `weight_icon_32`, 32 px source) and the text. The game only writes the texts
  (`UpdateCharacterStats`, `UpdateInventoryWeight`); nothing positions the plates. The prefab anchors armour to the
  panel's right edge at mid-height and weight to its bottom-right corner, so rows bought from the trader
  (`SetInventorySize`, up to 9) pulled them apart. Since 2026-09-26 the column is the shared `PlateColumn` library
  (`ValheimModLibs/CLAUDE.md`), which Elite Creatures Reborn also merges for its world tier plate: it pins both game
  plates to the top-right corner where they are (armour at 32, -71.5; weight at 32, -227), draws each icon 48 px
  behind its number, gives each a tooltip (a bordered box pinned right of the plate, not following the mouse), and spaces every plate evenly over that span, centred on its middle, at
  least 8 px apart. The trash can (`TrashPlate`) is `Column.Add` with rank 200 (armour 100, weight 300): a copy of
  the armour plate keeping only the wood and the icon, which shows `StowSprites.Bin` in its own colours
  (`assets/trash.png`, embedded and decoded by the library's `EmbeddedSprite`), with the tooltip "Trash" / the drag
  hint. Alone it sits at the midpoint (32, -149.25; 13.75 px between plates); with Elite Creatures' world tier plate
  (rank 400) under the weight the four sit at -41.25, -113.25, -185.25 and -257.25, 8 px apart. When a game plate
  is missing the can joins the row instead. The container panel's own weight plate (`Container/Weight`) is left as
  the game draws it.
- Shared chests (SPEC 9.3): every container write of Stow goes through `Shared.ChestWriter`, one `Put` or `Take`
  per stack, whether the chest answers at once (the local client owns it or may claim it: claim, the game's
  inventory methods, save, all inside the writer) or by request (`Full` mode, another player using it). Sorting
  is the one exception: it rewrites every cell at once, which no request carries, so a chest the player views or
  shares is refused ("Viewing only" in View mode, "The chest cannot be changed right now" in Full mode; auto sort
  skips it quietly). A Stow target (`StowTargets`) is a container that is usable now or shared; the nearby list
  holds both, nearest first, the open one first, no duplicates. A chest the player only views (View mode) is no
  target: quick stack to the open container, Store all and Store one say "Viewing only" for it; quick stack
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
- Route and Store one start with one put into the chosen container; what it does not take goes on (see the next
  point). The message comes when the last put is answered: "Sent x to chest" (the first container that took some),
  "Sent x to chest and n more" when further containers took the rest, "Stored one x"; when nothing moved, "Nothing to
  move", unless the last container asked was a shared chest whose refusal the writer has just said.
- Overflow (asked for on 2026-09-28: "if a chest is full, then auto store looks for the next closest chest with the
  item in it to fill up"; no setting, it is the obvious behaviour): a stack sent to a container that fills up goes on
  to the next container holding the item, nearest first from the player, until it is placed or none is left.
  `Overflow` runs one stack through a line: the chosen container first (Route's target, Store one's open or nearest
  holding container, taken whether or not it holds the item, as before), then every later container of the same
  `StowTargets.Nearby` list (the open one first, then by distance, within `Nearby Range`) that holds the item by name
  when its turn comes. Each step re-checks what could have changed while a shared chest answered: still a target
  (usable now or shared: section 0 switches, privacy, ward, in-use rule), not refusing the item, still holding it;
  the stack still in the inventory and still movable (favourites, equipped, a put under way). Every step is one
  `ChestWriter.Put` with the stack reserved in `StackMover` until its answer, so a chest the client can change at
  once is claimed, changed and saved as before, and a shared chest gets its request and the line goes on when the
  answer arrives. The paths:
  - Quick stack (with `Quick Stack Nearby`, or no container open) and Dump already did this and are unchanged for
    every chest the client changes at once: the targets are visited nearest first, each takes every movable stack it
    holds as far as it fits, and what a full container left is still in the inventory when the next holder comes.
    The gap was a shared chest: its stack is reserved until the answer and later containers skip it, so the leftover
    stayed. Now `StowActions.SpillOver` runs after each of its answers and sends the leftover on to the later
    containers of the same action that hold the item (`Overflow` without a chosen container); when anything moved a
    top-left line says "Sent x to chest", so the chest's own centre summary ("Moved n stacks to chest") stays.
  - Route: after the first container, only containers holding the exact item follow. When the target was chosen for
    its group or its `accept` list, no nearby container held the item (the exact holder always wins), so a full group
    or accept target keeps the rest in the inventory, as before; the next group or accept container is not tried.
  - Store one: the open container when one is open (refusing it still says "No nearby container takes x"), else the
    nearest holder (a refusing nearest holder too, as before); when it is full the item goes to the next nearest
    holder instead of "Nothing to move".
  - Unchanged because they never pick one container per item: Store all and quick stack without `Quick Stack Nearby`
    (the open container only, by definition), the game's move click and Place stacks (the open chest), Merge Into
    Chests (a drag onto the open chest).
  - Ground pickup and Reach's `ReturnOne`: see "Closest first" below.
- Closest first (the user's rule, 2026-09-29, for "any type of auto store": "if there are multiple chests with the items
  in it, it should try to store in the chest closest to it. if that chest is full then the next closest"; their
  examples: a chest full of stone beside one holding a single stone, ground pickup, quick stack, a kiln's coal). "It" is
  where the item comes from: the player for quick stack, dump, route and store one (above: nearest first from the
  player, the open container first as the player's own pick); the drop for ground pickup, which is also how a kiln's
  or smelter's output reaches chests (the game drops it at the station's output point; OpenKeep has no station-to-chest
  path). Ground pickup was each chest sweeping whatever lay in its range, so whichever swept first took the drop.
  `PickupOrder.IsFirst` now leaves a drop to the pickup chest that ranks first among those whose `Pickup Range`
  reaches it and could take it now (a pickup chest, not in use, `Wanted`, room for one): a chest holding the item
  before one that only accepts it (YAML `accept`, or any chest with `Pickup Only Held Items` off), then the nearest to
  the drop. A full chest has no room, so the next one takes the drop on its own sweep, and a chest that fills halfway
  leaves the rest for the next. Every chest still sweeps on its own ZDO owner's client from replicated contents, so no
  RPC is added; a drop a better chest will take waits for that chest's next sweep (`Pickup Interval`). Ranges stay:
  a chest farther than `Pickup Range` from the drop never takes it. Reach's `ReturnOne` (a unit a station borrowed,
  put back) now tries the containers holding the item first, nearest first, then the other accepting ones.

- Cycling in View and Full modes includes chests another player is using when the Shared module would open them
  read-only: in use by another, the ward and privacy checks of `Container.Interact`, the prefab enabled and the
  section 0 switches (the rules are repeated in `Cycling.Viewable` because `ContainerScan` has no viewable query;
  moving it into Core would be cleaner). In `Off` the ring is the usable containers, as before.
- Find marks shared chests too (they are targets), so the player sees where a quick stack would go.
- Auto Tidy (2026-10-05, the user's design: chests sort themselves; "some players have junk chests they throw everything
  into, and want the items to flow to the proper chests"; weigh how long an item has been in a chest and the chest's
  random items against like items; an emptied chest keeps its items; no lag. Of the automatic options offered they
  chose: looks only on a change, learning from players' hands, learned themes; no manual marks. One setting only, on
  or off: range, memory length and every threshold are fixed):
  - Scoring (`TidyProfile`): per prefab, stacks held or remembered (the larger) x settled (15 % on arrival, all after
    one in-game day) x origin (1 by hand, 0.5 arrived on its own). An item's share is the weight alike to it over the
    total; the random-items score is one over the weighted mean share (n for n unrelated kinds). Score 5 or more: a
    junk chest, home to nothing, everything in it a stray. Elsewhere a stray has under 25 %. A home is no junk chest
    with at least 25 % and twice the source's share (no back and forth), or a chest that keeps the item. Homes within
    15 m are tried nearest first (the closest-first rule), the next when one is full or refuses it (YAML `refuse`).
  - Alike (`TidyThemes`): the same prefab, a shared label (YAML group; a type theme for non-materials: food, trophies,
    fish, ammo, armour, weapons and tools, utility; a smelting family: inputs and outputs of every fuelled `Smelter`
    keyed by its fuel, plus materials crafted only from one family, so bars and bronze are one theme), or a learned
    pair: two prefabs put in by hand and settled together in 2 or more chests of at most 8 such prefabs.
  - Memory (`TidyMemory`): written in the chest's ZDO by its owner. A look happens on every change of an owned chest,
    so an item held at the last look and gone now left with this change: its peak restarts fading then, over three
    in-game days. The first look at a chest (no key yet) counts its contents as settled and put in by hand: they were
    there before Auto Tidy.
  - Hands (`TidyHands`): the game opens a chest only on its owner, so the owner notes the contents when it is taken
    into use and compares at release; prefabs that grew were put in by hand (Shared Full requests too). An item put
    back by hand within 15 minutes of world time after Auto Tidy sent it away becomes kept there: never a stray, always
    a home, until it fades out after leaving.
  - Looks (`TidySchedule`): never a timer over every chest. First sight of an owned chest (spread over 30 s), a change
    while closed (`OnContainerChanged`, not Auto Tidy's own moves, not while loading), a release (3 s later), and for a
    chest whose strays found no ready home with room: after 1, 2, 4, 8, then every 10 minutes, and soon after any chest
    within 15 m changes (another client's change shows as a new loaded revision). One look per frame, a chest at most
    every 10 s, 8 stacks per look (a look that used them all comes back in 3 s). Profiles are kept per data revision,
    loaded revision, rules and learned themes for up to 5 minutes, and the random-items score (every pair) is worked
    out only for a chest that already passed the cheap share test. Offline with .NET 8 against the built DLL
    (2026-10-05): 200 chests of 20 items scored for one stray with nothing cached took 1.2 ms.
  - Multiplayer: items move only between two chests the looking client owns (no other client can write either), with
    the game's inventory methods and save path, both chests held (one save each per home). A home owned by another
    client that has room for the item is asked to hand over (`HandOver`, at most once per 30 s per chest); the owner grants it as the game grants an open (free chest, ZDO force sent, then
    the owner set), and the source looks again 3 s later. A chest in use is never a source or a home.
  - Taking part: a piece placed by a player, not a ship, cart or private chest, and usable as Stow uses containers
    (section 0, prefab table, ward, privacy for the local player).
- Vocabulary: the chest-side button that stores your matching items is `Store all` (`Store All Key`; 1.1.0 to 1.2.0
  called it `Stow all` / `Stow All Key`, carried over; 1.0.0 already used `Store All Key`), the button that refills your stacks from the chests is `Top up` (`Top Up Key`, was `Restock
  Key`), an item marked for destruction is junk (`Junk Key`, was `Trash Flag Key`; `Destroy Junk Key`, was `Trash
  Flagged Key`; custom data `OpenKeep.junk`, was `OpenKeep.trashFlags`, old marks are not read). Destroying an item
  stays "trash" (`Trash Key`, `Confirm Trash`, `Trash Uses Salvage`). A cfg from 1.0.0 keeps its bindings through
  `Core.RenamedKeys.Carry`, which reads the old line from BepInEx's orphaned entries before the new key is bound.
- The defaults `G` (Store all) and `V` (Store one) share keys with the game's radial menu (`OpenEmote`) and auto
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
- Blocker order: disabled, trophy or quest item, no recipe, favourite, deny list, mod data, equipped, unknown
  recipe, station. Mod data (user decision 2026-09-24, EliteCrafting DECISIONS.md SAL-14): with `Skip Items With
  Mod Data` on, an item whose `m_customData` has a key starting with one of `Mod Data Prefixes` (comma separated,
  ordinal case-sensitive match, default `ecf_` = EliteCrafting's magic items) is not listed and not salvaged
  (`$ok_salvage_moddata`). Only keys are compared; nothing of the other mod is referenced. The prefix list is
  re-split only when the setting's text changes, and an empty `m_customData` is skipped before any enumeration.
  With `Trash Uses Salvage` on, such an item is trashed by the Trash Key (after `Confirm Trash`) like any other
  item salvage refuses.
- Require Station is satisfied by the current station or any station of that name within build range.
- The fit check is an exact simulation of `Inventory.AddItem`: partial stacks of quality 1 at the current world
  level fill first, the rest needs empty slots, the salvaged stack's own slot counts as free. The stack is removed
  first, then the returns are added; if an add still fails everything added is removed and the stack goes back
  into its slot.
- No progress bar; the tab selection persists like the game's tabs; the list shows the quality number and `x<n>`;
  success prints a top-left message. The Salvage Key reads the hovered stack of the player grid at the pointer,
  else the gamepad selection, and ignores presses while an item is dragged or a popup shows.
- The list leaves out the hotbar (the first eight cells of the top row) and PackPanel's slots (rows below its
  published main grid); the Salvage Key still works on them (user decision 2026-10-05).
- With Epic Loot, a magic item's row icon and the selected stack's icon get its rarity background, the one its
  inventory grid shows: the rows are OpenKeep's, so Epic Loot's own `AddRecipeToList` patch never sees them, and they
  go through `EpicLoot.API.ApplyMagicItemBackgroundToIcon` (reflection, nothing referenced). Epic Loot's client
  setting `Interface / Show Rarity In Recipe List` decides. The panel postfix runs `HarmonyAfter` Epic Loot, whose
  `UpdateRecipe` postfix hides the background when no recipe is selected.

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
- Station capacities (1.7.0; the user asked for how much each workstation holds to be configurable per station):
  stations are found by component, every ZNetScene prefab with the game's `Smelter` on it or a child; the name is the
  net view object's prefab name (`m_nview`, own object or parent). `items` is `m_maxOre`, `fuel` is `m_maxFuel`. A key
  for a cap the station does not use (vanilla 0: the hot tub and frost kiln take no items; the charcoal kiln,
  spinning wheel, windmill and battering ram take no fuel) is warned about when applying and ignored, because
  `UpdateSmelter` and `IsActive` read 0 as "runs without it". Caps are written to the prefab and to every loaded
  station (`FindObjectsByType<Smelter>`) at `ZNetScene.Awake`, on every YAML apply and on every `Enabled` change; new
  stations copy the prefab (`ZNetScene.CreateObject` and `Player.PlacePiece` instantiate it), and the `Smelter.Awake`
  postfix only guards a station made from another copy of the prefab. Only listed caps are touched, per cap; a
  removed cap and every cap with `Enabled` off go back to the value remembered when the prefab was first seen. The
  caps are enforced only by the interacting client (`OnAddOre`, `OnAddFuel`, `CanUseItems`); the owner's `RPC_AddOre`
  and `RPC_AddFuel` check no cap, so the synced file keeps every client on the same caps (Auto Feed checks them on
  the station's owner before its RPC). Lowering a cap loses nothing: `UpdateSmelter` and `DropAllItems` work from the
  ZDO's `queued`/`item<n>`/`fuel` without reading the caps, and the hover shows e.g. `(15/10)` until the station is
  below. Hover texts and Reach's feeding read the instance fields live. Large caps cost: one ZDO string per queued
  item, the whole queue rewritten per finished product (about 15 KB at 1000), and the game catches up at most 3600 s
  of work when a station's area loads again. The template, validation and indentation rescue follow the container
  file; unknown prefab names are accepted silently (a mod that is not loaded).

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
  the timeout. Requests without a reply after `Request Timeout` are denied locally ("The chest did not answer") but
  wait 30 s more for a late answer, which is still applied (a take still arrives, a put keeps what the chest took);
  a late no, or none, gives back what was held. A put or stack-all holds its units out of the inventory until the
  answer (`Escrow`): the whole stack leaves (and comes back to its own slot when that is free), a part is taken off
  the stack. A chest another client owns and nobody uses is written the same way, by request to its owner, in every
  `Shared Chests` mode (`ChestWriter.IsShared` = `ContainerScan.IsShared` or `IsRemote`).
- Touches: mouse-down on a stack of the open chest's grid sends a touch (Full mode); the sent touch ends when the
  button is up with nothing dragged, when the panel leaves the chest, or after `Touch Seconds`; received touches
  expire after `Touch Seconds`; a player's own touch is not shown to them. The owner forwards touches to the machines of the players within 10 m of the chest.
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
  claim, then `ZDOMan.DestroyZDO`) and clears `OpenKeep.sign` when the container may not have a sign (module off:
  looked at once after the container loads and once after the switch changes, not every second; prefab off, ship or
  cart, opted out) and in `Container.OnDestroyed` (the game's own hook the owner runs when the
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

### Homestead

Section 8 (1.6.0, more in 1.7.0 and 1.8.0) holds requests from the user's server that are not storage; each feature is a set
of classes with one prefix (`Bed*`, `Fire*`, `Hive*`, `Fuel*`, `Torch*`, `Feed*`, `Rest*`, `Repair*`, `StationRepair*`, `Pet*`) and its own settings class, and
`HomesteadModule` only calls them. Fuel and Feed share `NearbyTake` and `TakeRetry`; Pet takes through `NearbyTake` too.

Beds:
- Every part runs on the player's own client: the list in the character's custom data, the choice, and the
  profile's custom spawn point. The dedicated server does nothing (`BedStore.Scope` is null there).
- The game already lets a player own several beds (the bed ZDO keeps `owner` after another is claimed); only the
  profile's one custom spawn point per world counts. At death (`Player.OnDeath` postfix, while the player still
  exists) the known beds plus the profile's current point (a bed claimed before the mod) are ordered by map distance
  (x/z: a dungeon sits about 5000 m above its entrance); the nearest becomes the custom spawn point, which the game
  reads in `FindSpawnPoint` after 10 s and in `SaveLogoutPoint` when the player logs out dead. The rest are the
  fallback.
- A gone bed: once the area is ready the game looks for a bed of this player within reach of the point
  (`FindBedNearby`, `IsCurrent`) and clears the point when there is none. The `FindSpawnPoint` postfix forgets it
  and sets the next nearest; the game waits its own load time (8 s) for each. The world start comes only when no bed
  is left. No fallback on the first spawn of a session (the character's data is not loaded yet); the bed is still
  forgotten.
- Remembered when a bed becomes the spawn point in `Bed.Interact` (a claim that passed the roof check, a use of an
  own bed) and when an own bed loads (`Bed.Awake`, owner = profile id). Forgotten when another player's or an
  unclaimed bed loads within 1 m of a known point, when the local client destroys a bed (`RemoveCustomSpawnPoint`),
  and when the respawn finds no bed there. Tracking runs with the setting off too (custom data only), so beds
  claimed then count when it is turned on. `resetspawn` clears only the profile's point.
- Custom data is written only while a local player exists. Between `_RequestRespawn` (which saves the data and
  destroys the player) and the next spawn, changes wait tagged with player id and world uid and are written in
  `Player.OnSpawned` after `LoadPlayerData`; changes for another character or world are dropped.
- Using an own bed that is not the spawn point makes it the spawn point quietly before the game's `Interact` runs,
  so the game's sleep path and all its checks apply; the profile's point is therefore the last bed claimed, used or
  woken in. `Bed.IsCurrent` is not patched, so `FindBedNearby` stays exact; `BedChecks.IsSpawnPoint` also requires
  `HaveCustomSpawnPoint`, because `IsCurrent` matches the stale point the game leaves after clearing one. The hover
  swaps the localized "Set spawn point" for the game's "Sleep" inside the game's string.
- Choice of bed (1.12.0, the user's request of 2026-09-30): at death, with two candidates or more, the large map opens
  for `Bed Choice Seconds`; the nearest is already the spawn point, so doing nothing is the old behaviour. A click
  within the game's pin click radius (`PinInteractRadius`, it grows with zoom) of a candidate makes it the spawn point
  (`BedRespawn.Prefer`; the rest stay the fallback, nearest first) and ends the choice; the map key or Escape ends it
  with the nearest. The game's `Minimap.Update` sets the map to None for a dead player, so its prefix runs the game's
  own pieces instead (`UpdateMap`, `UpdateDynamicPins`, `UpdatePins`, `UpdateBiome`) and skips the original; the
  cursor follows `Minimap.IsOpen` as usual. Closing sets `m_hiddenFrames` to 3: the game's counter stops while the
  player is dead, and `IsOpen` would stay true (cursor shown, Escape menu blocked) until the next spawn. Escape and the
  map key close one frame after the press, so the menu (`Menu.Update` checks `IsOpen`) never sees that press with the
  map already shut. The black screen (`Hud.UpdateBlackScreen`) is held off during the choice: it would cover the map
  and its clicks. The respawn the game asked for (10 s) is moved to the end of the choice as a backstop, so a broken
  map (no generated texture, a mod closing it) still wakes the player at the nearest bed; `Game._RequestRespawn` ends
  a choice still open. Not in `nomap` worlds (`Game.m_noMap`). The choice is "active" only while the same `Game`
  and the same dead local player exist, so quitting or being revived needs no clean-up call. The bed filter of the
  map is forced on during the choice and restored after. The countdown (30 s by default, the user's call of
  2026-09-30) sits in the upper left corner of the map image (moved from the upper right at the user's request the
  same day), a child of `m_mapImageLarge` anchored top left and drawn after its other children, in the biome name's
  font, material and colour, left aligned: the seconds in large figures, then "seconds until you wake in the nearest
  bed", how to choose, and the keys. During the choice every bed icon (OpenKeep's and the game's spawn pin) has the
  game's `m_doubleSize` and `m_animate`, as its event pins: 1.2 to 2 times the normal size, pulsing (the user asked
  for twice the size and pulsing, 2026-09-30). The game sizes an icon only when it makes it and while it pulses, so
  at the end the icons are destroyed (`DestroyPinMarker`) and made again at the normal size. The chosen (nearest)
  bed no longer stands out from the others.
- Quick Respawn (1.12.0): the game's wait is `RequestRespawn(10f)` from `Player.OnDeath` plus `m_respawnLoadDuration`
  (8 s) in `FindSpawnPoint` before it looks for the bed. Both shrink by one share: `Quick Respawn Seconds` at 0 m,
  the full 18 s at `Quick Respawn Range`, linear between, from the map distance between the death point and the
  profile's spawn point (or the `StartTemple` location icon without one). The death part is a new `RequestRespawn`
  counted from the death (the time spent choosing counts); the load part is `m_respawnWait` advanced faster in a
  `FindSpawnPoint` prefix, only after a death and only with a custom spawn point (the world start path has no timer).
  The game's own `IsAreaReady` check stays, so a far bed still waits for its area. Works with `Nearest Bed Respawn`
  off too (then the game's one bed). A fallback bed after a gone one is timed from its own distance.
- Beds On Map (1.12.0, per player): the other known beds as `PinType.Bed` pins with `m_save` false, added to
  `m_pins` directly (`AddPin` would turn the bed filter back on each time) and never saved; the game draws the spawn
  point's own pin, so that bed is left out. Checked once a second and at once when the spawn point moves. During the
  choice the candidates are drawn even with the setting off. The bed icons are yellow (1, 0.85, 0.1; the user's call
  of 2026-09-30, to stand out), OpenKeep's and the game's spawn pin alike, on both maps, whenever OpenKeep shows the
  beds (`BedPins.Showing`: the choice, or Nearest Bed Respawn with Beds On Map); the game sets every icon white in
  `UpdatePins`, so a postfix tints them after it. Otherwise the game's white icon. During the choice every bed icon
  is also drawn on top of every other icon (asked 2026-10-04, "so it's easier to click"): the game remakes an icon
  that leaves its pin root and Wayfare keeps its portal layer last under the map, so instead each bed icon gets its
  own override-sorting canvas at the HUD canvas's order + 1 (401; the inventory is 600), gone when the icons are
  made again at the end. The click prefix runs `Priority.First`, so a portal icon lying over a bed (Wayfare's prefix
  takes clicks on its icons) never takes the click. Each bed icon carries a click area (`BedBubble`, asked the same
  day; first a visible 140 unit disc, then, at the user's word, invisible and no larger than the icon at its largest:
  an empty rect of twice `m_pinSizeLarge`, the doubled icon at the top of its pulse); a click inside one picks that bed
  (the nearest when areas overlap; the radius is the area's screen size turned into map metres at the current zoom;
  the game's pin radius only before the first area exists). The pick waits out the game's double click window (0.3 s,
  the MapClicks library's `IconClick.Hold`), so the game's pins keep working under a bed (asked 2026-10-04): a double
  click there places a pin instead of waking, and a right click removes a pin as anywhere else (the bed icons are
  unsaved pins, which the game's removal skips). The nearest bed is pinged when the map opens (`BedPing`, asked the
  same day: the game's ping marker as a local unsaved pin, built directly like the bed pins, twice the size, pulsing,
  tinted gold after every pin update), under the bed's icon and bubble; it goes when the choice ends.

Portals (moved out 2026-10-04):
- Quick Portals, Quick Portal Range, Quick Portal Seconds, Portal Screen Only When Loading and the jump half of Quick
  Area Loading went to Wayfare on 2026-10-04 (the user: "Wayfare is the teleportation mod"), with their key names, in
  its section `Jump Speed`; the decisions and measurements made here (1.12.0, 2026-09-30) are in `../Wayfare/PLAN.md`
  under "Jump speed". The cfg keys of section 8 that went are left as orphans in old cfgs (BepInEx keeps them unused),
  a MAJOR change by the workspace's version rules. Old OpenKeep and new Wayfare together would hurry a jump twice;
  release both together.
- What stayed is the respawn: the distance-scaled wait (`QuickWait`), the bed search held on a server's client until
  the bed's objects arrived (`AreaSettle`: the ZDOs in the bed's 3x3 sectors unchanged for 0.5 s, never past the
  game's 8 s; without it a quick respawn could look for the bed before its ZDO arrived, and the game would clear the
  bed and wake the player elsewhere) and `Quick Area Loading` (per player, now the respawn only: the land loads at 20
  ms of `CreateLocalZones` per frame and the listed objects at 15 ms per `CreateDestroyObjects` run, zone by zone,
  while the dead player waits to respawn). All three are the AreaLoading library's (`../ValheimModLibs/AreaLoading`),
  which Wayfare uses for jumps; each mod's merged copy hurries only for its own registered reason.
- Stand Up On Respawn (1.12.0, the user's request of 2026-09-30): `Player.Awake` reads the player ZDO's `wakeup`
  flag (true when unset) and plays the getting-up animation, a state tagged `cutscene` (no movement until it ends).
  `Game.SpawnPlayer` runs `Awake` and `OnSpawned` in one frame, before the animator updates, so the `OnSpawned`
  postfix clears the ZDO flag, the animator bool and `m_wakeupTimer`; the ZDO flag goes out with the first sync, so
  other clients' copies stand as well. Only after a death (`Game.m_respawnAfterDeath`); logging in keeps it.

Fires:
- `Piece.m_notOnWood` is the only thing keeping the campfire off wood: `Player.UpdatePlacementGhost` reads it from
  the placement ghost and refuses on a WearNTear of material Wood or HardWood (vanilla already allows stone, iron,
  marble, Timberwood). Read from the assets with UnityPy on 2026-09-27: `fire_pit` has `m_notOnWood` 1 and no other
  ground-only flag; the same flag is set on `bonfire`, `smelter`, `charcoal_kiln`, `blastfurnace`, `eitrrefinery`,
  `piece_FrostKiln` and `windmill`, hence a prefab list (default `fire_pit`) rather than a bool.
- Applied to the prefab at `ZNetScene.Awake` and on every `SettingChanged`, and to the local ghost so a change
  applies with the piece in hand; the original value is remembered once per prefab name and restored when the name
  leaves the list. No placement patch, no RPC, no ZDO key: the builder's client decides, placed fires are ordinary
  pieces.
- Embers: `fire_pit` and `bonfire` are the only fires with a `CinderSpawner`, which spawns only in the Ashlands or
  with `GlobalKeys.Fire`; there its embers land on a dry burnable wood floor and start a `HouseFire`. That is why the
  game forbids it. Nothing prevents it (warned in the cfg and README); the smallest fix, if wanted, is a
  `CinderSpawner.SpawnCinder` prefix skipping a listed fire's own spawner when it stands on a burnable piece. The
  direct ignite capsule (0.5 to 1.12 m, radius 0.45) stays 5 cm clear of the floor. Smoke (mask `smoke` only), the
  roof and wet checks (Cover rays go sideways and up) and support behave as on the ground.

Beehives:
- Vanilla, read from the main scene bundle with UnityPy on 2026-09-27: `piece_beehive` `m_secPerUnit` 1200,
  `m_maxHoney` 4; `EnvMan.m_dayLengthSec` 1800 (the class defaults 10 and 1200 are not what the game uses): 1.5
  honey a day, about 1.49 in practice with the 10 s tick.
- Only hives whose `m_honeyItem` is the `Honey` prefab follow the settings; `piece_birdnest` (a `Beehive` making
  Feathers) keeps the game's rate. The rate is `m_dayLengthSec / honey per day`, set on the owner's instance in the
  `UpdateBees` prefix every tick (the only reader of `m_secPerUnit`), or reset to the prefab's value with both
  settings off; the prefab is never changed.
- Players online is `ZNet.GetNrOfPlayers()`, at least 1: the server's list on a client (every player, sent every
  2 s), the local player plus peers on a host.
- The remainder the game drops after a finished honey is kept (recomputed from `product` and `lastTime` before and
  after), so fast rates give the full amount. A rate change scales `product` by new over old rate
  (`OpenKeep.hiveSecPerUnit`), so the fraction of a honey carries over; a hive the mod never ran counted at the
  prefab rate, and a hive it never touched gets no ZDO write. `m_maxHoney` is not changed: at high rates the cap of
  4 is the limit.

Fires feeding themselves (Auto Fuel):
- Hook: the private `Fireplace.UpdateFireplace` (`InvokeRepeating` every 2 s from `Awake` on every client with the
  fire loaded; the ZDO owner burns `fuel` for the time since `lastTime` while `state` is 1). The postfix runs on the
  owner after the burn; every other machine returns after the ownership check.
- Fires: every `Fireplace` whose own `Piece` a player built (creator not 0), with `m_canRefill`, not `m_infiniteFuel`,
  a fuel item and `m_maxFuel` of at least 1; found by component, so modded fires count. Location fires (Fuling camps,
  Haldor, Hildir, Morkhalla) and the resin candle (`m_canRefill` 0) are not fed; a fire switched off takes nothing.
- Rule: room = floor(`m_maxFuel`) - ceil(fuel), the game's own refusal (`Interact` refuses at `CeilToInt(fuel) >=
  m_maxFuel`). At room 1 or more every whole unit that fits is taken and added with one `Fireplace.AddFuel(n)` (the
  game's `RPC_AddFuelAmount`, handled at once on the owner: clamp, ZDO write, one fuel-added effect, `UpdateState`).
  Steady state is one unit and one container write per unit burned (read with UnityPy 2026-09-27: wood fires 5000 s
  per unit, most torches and braziers 20000 s, the wood torch 10000 s); a fire that burned out unloaded (the game
  burns the whole absence in its first tick) refills in one go. The fuel is compared before and after (warning).
- Containers: the loaded containers holding the fuel (`FuelHolders`, one shared walk) within Auto Fuel Range, through
  `ContainerScan.Nearby(holders, fire position, Auto Fuel Range, Reach)`, nearest first; `StationAccepts.Fuel`
  (fuel item by shared name, the fire prefab's `stations:` allow/deny), the world level rule (1.7.0: an item's
  `m_worldLevel` below `Game.m_worldLevel` is skipped, as `Inventory.HaveItem` does for a fire fed by hand) and per
  stack the container prefab's allow/deny; claim, `RemoveItem`, save (`NearbyTake`). `stations:` `enabled: false` stops it. The containers' `range:` (metres from
  the player) is not used; Reach's `Enabled`, `Feed Stations` and the per-player toggle do not gate it.
- Retry: a fire with room that found nothing, or whose station entry is disabled, looks again after 10 s (per
  machine). Range default 20 m, Reach's default; `Nearby` checks distance first, so the range does not cost more.
- Multiplayer: only the fire's ZDO owner acts (the nearest player's client: the server hands unowned persistent ZDOs
  in a peer's active area to that peer). `ContainerScan.IsUsable` needs a local player, so the owner's access applies
  (chests another player has open skipped, a private chest feeds only its owner's fires, a player without access to a
  ward feeds nothing from its chests). A dedicated server simulates only the area around the world origin (reference
  position zero): fires there are not auto-fuelled. A fire nobody is near is not loaded anywhere; it catches up when
  someone comes.

Torches at night:
- Vanilla (UnityPy, all SoftRef bundles, 2026-09-27): only `Candle_resin` has `m_canTurnOff`. The switch is the
  `state` ZDO int (1 or unset on, 2 off), read by `IsBurning` on every client: an off fire hides `m_enabledObject`
  (torches: the point light, flames, the fire loop sound and the Fire effect area that fire-shy creatures avoid and
  that holds back the Mistlands mist), burns nothing and spreads no embers; `m_playerBaseObject` (the PlayerBase area)
  stays. `RPC_ToggleOn` is registered on every fire and does not check `m_canTurnOff` (only `Interact` does), so the
  owner switches vanilla torches with the game's own RPC and no prefab change. `m_canTurnOff` is deliberately not
  set: plain Use would toggle instead of adding fuel under a hover that says Use Resin, and fires with rain objects
  (braziers) would go out in rain through `UpdateState`. Price: Use never switches a vanilla torch (Torch Switch Key
  keeps one lit instead, 1.7.0), and a torch left out when the mod is removed stays dark (README warning). Torches
  have no toggle effects (silent).
- Night: the game's own thresholds widened by `Torch Margin`. `EnvMan.IsNight()` is the smoothed day fraction
  (`GetDayFraction`, `m_smoothDayFraction`) at or below 0.25 or at or above 0.75; `RescaleDayFraction` maps the clock's
  15 % (daybreak, also `SkipToMorning`'s target) and 85 % (nightfall) onto those values, so the game's night is 30 % of
  the day, 9 of 30 minutes. The margin moves both clock points by hours / 24 and rescales them the same way, so 0 is
  exactly `IsNight`; 1 (default) lights at 0.7202 and puts out at 0.2798 of the smoothed fraction (lit 11.5 minutes,
  out 18.5); 4, the maximum, is lit 19 of 30 minutes. An in-game hour is a 24th of `m_dayLengthSec` (1800 s in the
  game's scene): 75 s. `IsDaylight` was rejected: its `m_alwaysDark` weathers are per biome where each owner stands,
  so owners at a biome border would disagree and an owner change could flip a torch. The game's day fraction starts
  at midnight when a world loads and trails the clock, so `TorchNight` answers only within 10 degrees of the clock
  (or with `tod` set): no flicker after loading, nothing during a sleep's time skip (torches go out Torch Margin
  after waking).
- Phase memory: `OpenKeep.torchPhase`. The owner switches a scheduled torch (listed, setting on, not kept lit) only
  when the phase differs from the stored one, then stores it, so an owner change, a reload and a hand switch in
  between stay. A torch never switched takes the current phase on its first tick. Off the schedule (setting off,
  removed from the list, or kept lit): a torch the mod put out is lit again and the key cleared; one it lit is left as
  it is. Runs on the dedicated server too for the fires it owns (no local player needed). `TorchPatch` runs before
  `FuelPatch`, so a torch put out this tick takes no fuel. The hover line shows on any client from the replicated
  phase and state.
- Keep lit (1.7.0; the user asked for players to keep a torch on, and off again to return it to the schedule): per
  torch `OpenKeep.torchKeepLit`, switched with `Torch Switch Key` (default O) while looking at the torch. Plain Use is
  unchanged (E adds fuel; `m_canTurnOff` stays unset for the reasons above). O is in none of the game's default
  bindings and no OpenKeep key; `L` was rejected because `debugmode` reads L as `removedrops` in `Player.Update`, and
  `LeftAlt + L` is Reach's Link Key. Polled in a `Player.Update` postfix only when `Player.TakeInput()` is true and no
  radial is open, on the game's hover object; it never calls `Interact`, so it cannot add fuel. The client claims an
  unowned fire (as `Fireplace.Interact` does) and sends the wanted state to the owner (`OpenKeep_TorchKeepLit`); the
  owner writes the flag and applies it at once: kept lit, the fire is lit (`RPC_ToggleOn` when off) and leaves the
  schedule like an unlisted fire; back on the schedule, the phase is set to 0 and the tick runs at once, so by day it
  goes out immediately and at night it is lit. A request reaching a client that is no longer the owner is dropped, as
  the game's fire RPCs are. A resin candle kept lit and then put out by hand stays out (only a torch the mod put out
  is relit).
- The key and the hover act only on a player-built fire in `Torch Pieces` while `Torches Night Only` is on; otherwise
  every torch burns anyway, the hover shows no switch line and the key does nothing. The flag is never cleared by the
  setting or the list, so it counts again when either comes back.
- Ward: the game's `Fireplace.Interact` has no ward check (anyone refuels a warded fire), so the switch follows
  `Door` and `Sign`: the hover omits the line without access (`CheckAccess(pos, 0, false)`), the key flashes the ward
  and sends nothing (`CheckAccess(pos)`). The owner does not re-check, as the game's RPCs don't.
- Hover: `[O] Keep lit` on a scheduled torch, `[O] Light at night only` on one kept lit (modifiers first); `Lights at nightfall` stays for a scheduled torch out for the day. The presser gets a centre message
  (`<name>: kept lit day and night` / `<name>: lit at night only`); the owner logs `... kept lit day and night (asked
  by peer N)` / `... back on the night schedule`.
- `piece_bathtub` is a `Smelter` with a wood switch, not a `Fireplace`: neither fire feature touches it (Reach feeds
  it through `SmelterFuelPatch`, Auto Feed through `RPC_AddFuel`).

Stations feeding themselves (Auto Feed, 1.7.0; the user asked for smelters and kilns pulling ore, wood and coal from
chests about 2 m away):
- Hook: the private `Smelter.UpdateSmelter` (`InvokeRepeating` every 1 s from `Awake` on every client with the
  station loaded; the ZDO owner works the queue for the time since `startTime`, capped at 3600 s). The postfix runs
  on the owner after the work; every other machine returns after the ownership check.
- Stations: every `Smelter` whose net view's `Piece` a player built, found by component. Vanilla, read from the
  prefabs with UnityPy on 2026-09-27 (item cap / fuel cap, fuel): `smelter` 10/20 Coal, `blastfurnace` 10/20 Coal,
  `charcoal_kiln` 25/0 (Wood, FineWood, RoundLog to Coal), `eitrrefinery` 20/20 Sap (Softtissue), `piece_spinningwheel`
  40/0 (Flax, needs a roof), `windmill` 50/0 (Barley, OatSeeds, Oat), `piece_bathtub` 0/10 Wood, `piece_FrostKiln` 0/25
  Ice (makes FrozenFuel from its fuel). `BatteringRam` 25/0 (Wood, FineWood, RoundLog, Blackwood, makes nothing) is
  skipped: its `SiegeMachine.m_engine` is the `Smelter`, which rams while `IsActive` and burns a wood every 20 s, so a
  ram parked by a chest would ram and eat its wood. Cooking stations, ovens and fermenters are other classes.
- Rule per tick: room for an item is `queued < m_maxOre` (the `OnAddOre` cap), room for fuel is `fuel <= m_maxFuel - 1`
  (the `OnAddFuel` cap). With room, one unit is taken (`NearbyTake`, predicate `StationAccepts.SmelterOre` or
  `StationAccepts.Fuel` minus `Auto Feed Skip`) and handed to the game's `RPC_AddOre(prefab name, cheated)` or
  `RPC_AddFuel`. `ZNetView.InvokeRPC` targets the ZDO owner, this machine, and `ZRoutedRpc` handles a call to itself at
  once, so the queue or fuel is checked right after (warning if it did not go up by one; a debug line otherwise, as
  a station takes a unit every 15 s or more in steady state). One unit of each per tick rather than all that fit:
  each goes through the game's RPC with its own added effect, as by hand, and nothing is played ten times in one
  frame. The fastest vanilla station works one item in 10 s, so a stocked station never waits.
- `Auto Feed Skip`: the kiln turns FineWood and RoundLog (core wood) into coal like Wood, so the default skips them;
  the setting uses the item vocabulary without groups (it lives in the cfg). A station fills its fuel whether or not
  it has anything queued: the game burns fuel only while it works, so nothing is wasted.
- `Auto Feed Leave` (1.8.0; the user asked for stations to leave one ore or wood in the chest): `NearbyTake` keeps
  that many units of each shared name in every container, counted over the stacks it may take (world level and
  `containers:` rules applied), so a chest keeps its last unit and quick stack, which sends an item only to a chest
  that holds it, still stocks it. Ore and fuel alike. Auto Fuel and pets pass 0; hand feeding (Reach) is not limited.
- Range: `ContainerScan.Nearby(Bounds, range)`: from the station's outline (the world box of its enabled non-trigger
  colliders; the PlayerBase and warmth triggers are 5 to 20 m wide) to the container's position, 0 inside. A centre
  measure would not reach a chest touching the kiln (its door switch alone sits 2.2 m from its centre). The box of a
  rotated station is a little larger than the station; the windmill's includes its blades. 4 m by default since 1.8.0 (2 m
  before; the user asked for 4).
- Retry, access, multiplayer and the dedicated server as for Auto Fuel: `TakeRetry` after a tick with room that fed
  nothing or whose `stations:` entry is disabled; `ContainerScan.IsUsable` with the owner's local player; only the
  owner acts, a dedicated server feeds nothing. Reach's `Enabled`, `Feed Stations` and the per-player toggle do not
  gate it.

Pets eating from containers (1.8.0; the user asked for tamed animals eating from chests within 10 m):
- Hook: the private `MonsterAI.UpdateConsumeItem(Humanoid, float)`, called from the AI update on the creature's owner
  only. The game's own search comes first (every `m_consumeSearchInterval`, 10 s: food on the ground within
  `m_consumeSearchRange`, 5 m). When it found nothing (`__result` false) at the search moment (`m_consumeSearchTimer`
  just set to 0), the postfix picks the nearest usable container within `Pet Chest Range`
  (`ContainerScan.Nearby(position)`) holding one of the creature's `m_consumeItems` (shared name, the world level
  rule, the `containers:` rule) that the creature has a path to (`HavePath` to the spot beside it; at most 3
  containers tried), then walks it there, returning true while it walks as the game does for ground food.
- Eating: one unit through `NearbyTake` (claim, remove, save), then `m_onConsumedItem` with the food's prefab
  `ItemDrop` (Tameable plays its soothe effect and resets `TameLastFeeding` in the creature's ZDO), the creature's
  `m_consumeItemEffects` and the `consume` trigger on its synced animator. Tamed creatures only (`IsTamed`); ones still
  being tamed eat from the ground as in the game. A target is dropped after 30 s, when the container empties, unloads
  or is refused at the take, or when the creature is fed or finds ground food.
- Access, multiplayer and the dedicated server as for Auto Fuel: the owner's local player's ward and privacy access
  and the section 0 switches; a dedicated server running a creature (no player near it) feeds nothing.
  GrindstoneSkills' Animal Feeder has the same walk for its feeder piece under Husbandry. The feeder is a container,
  so with both mods the first postfix to find food takes the creature and the other sees `__result` true and stands
  down.

Quick World Save (2.4.0; the user asked for world saves without the 1 to 2 s freeze):
- Vanilla (game 2026-10): `ZNet.SaveWorld` runs `ZDOMan.PrepareSave` on the main thread, then writes on a thread.
  PrepareSave clones the persistent objects of the chunks changed since the last save (`GetSaveClonePerChunk`, four
  `AddObjectsPerChunk` calls), then `ZDOExtraData.PrepareSave` clones every stored value of every object in the world
  into `s_save*`, though the only reader (`ZDO.Save` through `GetSaveData`, from `SaveChunk`) asks only for the
  changed chunks' objects. The "World saved (X+Y s)" message: X is the freeze, Y the thread.
- The values prefix copies the entries of the objects in `m_saveData.m_objectsByChunk` alone (portal chunk included),
  with the game's own `Clone` (`BinarySearchDictionary.Clone` is a shallow `MemberwiseClone`, as in the game), and
  keeps `RegenerateConnectionHashData` and the full `s_connectionsHashData` copy. From 20000 objects the seven
  tables are copied side by side (`Parallel.Invoke`), and the changed chunks' object clones chunk by chunk
  (`Parallel.For`, results appended in the game's order): the main thread waits inside the call, so nothing writes
  the tables meanwhile, and the workers only read them. Below 20000 the copy runs on the main thread.
- Nothing differs on disk. A throw logs a warning and runs the game's own copy; the object copy appends only after
  every chunk is done, so the game's rerun starts clean. Only the machine that saves (server, host, single player)
  acts; clients run nothing.
- Not done: spreading the snapshot over frames (change tracking, transpilers). Also not addressed: every client's
  own character save at the same moment (`Game.SavePlayerProfile` from the server's `SavePlayerProfile` RPC packs the
  8 MB explored map byte by byte, compresses and writes it on the main thread).

Rested sooner (1.7.0; the user asked for the comfort buff after 5 s):
- Vanilla (UnityPy, bundle c4210710, 2026-09-27): `Resting` is an `SE_Cozy` with `m_delay` 20 (the class default 10
  is not what the game uses), `m_ttl` 0 and `m_statusEffect` `Rested`; `Rested` is an `SE_Rested` with `m_baseTTL`
  480 and `m_TTLPerComfortLevel` 60. `Player.UpdateEnvStatusEffects` keeps Resting while the player is near a fire
  (a Heat area within 0.25 s), sitting or in shelter, not sensed, not cold or freezing, not wet (unless in a warm
  cozy area) and not burning, and removes it otherwise, so leaving restarts the wait. Past `m_delay` the effect adds
  Rested with resetTime every tick, and `SE_Rested.ResetTime` renews it to 480 + 60 x (comfort - 1) s.
- A prefix on `SE_Cozy.UpdateStatusEffect` writes the setting into `m_delay` of the SEMan's copy every tick (the SEMan
  clones the item database's asset when Resting starts; the asset is never changed), so a new value applies to a
  rest in progress. Only the effect with the name hash `SEMan.s_statusEffectResting` follows it; the game has no other
  `SE_Cozy`, and a modded one keeps its delay. One value rather than a switch: 20 is the game's wait, 0 is at once.
  Duration, comfort and the resting conditions are not touched.
- Multiplayer: status effects run only on the player's ZDO owner, the player's own client (`Player.FixedUpdate` for
  the local player; `SEMan.Update` only when the ZDO is owned), each with the server's synced value. A dedicated
  server does nothing here; no RPC, no ZDO key, and neither effect sets `m_attributes`.
- Display: the game shows no countdown. The Resting icon shows `Comfort N` (`SE_Cozy.GetIconText`), the Active
  effects page a fixed tooltip and regen lines, and the messages name no time, so no text changes. Below about 2 s
  the "You feel rested (Comfort: n)" message can show a comfort level up to 2 s old (the game recounts comfort every
  2 s); the duration follows the current comfort through the renewal.

Area repair (1.7.0; the user asked for the hammer to repair every piece touching the one repaired, on by default):
- Hook: the private `Player.Repair` (the hammer's repair mode, local player only, after the game's stamina check).
  `WearNTear.Repair` stamps `m_lastRepair` only when it sends the repair, so the prefix/postfix pair compares the
  hovered piece's stamp: moved means the game repaired it and charged the swing (`GetBuildStamina`, `m_attackEitr`,
  `m_useDurabilityDrain` x `Game.m_durabilityRate`; no materials, no skill). A piece that needs no repair spreads
  nothing. The neighbours are free: a charge per piece would empty the stamina bar and the hammer in one swing, and a
  repair costs no materials in the game.
- Touching is the game's own connection rule (`SetupColliders` boxes grown by 0.3 m, 0.15 m a side, overlapped as in
  `UpdateSupport`, which uses it to tell the pieces around a new one to clear their support cache), so no tolerance
  setting. The cached support lists (`m_supportColliders`) were not used: they hold only the supporting pieces below
  the centre of mass, and only on the owner. Deviations: boxes measured fresh from active, enabled colliders (a
  disabled collider's bounds lie at the world origin, the cached boxes never follow a moved piece), `piece_nonsolid`
  added (the repair ray reaches it), terrain dropped. Colliders on a rigidbody are skipped as in the game: ships and
  carts are never neighbours and repairing one spreads nowhere. Direct neighbours only, nearest first, at most 64.
- Checks per neighbour mirror `Player.Repair`, without its messages and ward flash: a `Piece` with a net view;
  `CheckCanRemovePiece` (the piece's station within build range of the player, waived by no placement cost or the
  `NoWorkbench` key); `PrivateArea.CheckAccess` at the piece (radius 0); then `WearNTear.Repair` itself (damaged, not
  repaired in the last second, valid view). Like the hand repair, no creator, `m_canBeRemoved`, no-build or reach
  check.
- `WearNTear.Repair` sends `RPC_Repair` to the ZDO owner (at once when local, through the server otherwise); the
  owner writes full health and sends `RPC_HealthChanged` to everybody, so every client sees the worn look go. No ZDO
  key and no RPC of our own. A piece at full health plays no effect in the game (`SetHealthVisual` fires the switch
  effect only on the way down), so each repaired neighbour plays its own `m_placeEffect` on the repairing client, as
  the game does for the hovered piece. One top-left line with the count follows the game's `$msg_repaired`; one log
  line per swing.

Repair on opening a station (`Auto Repair`, asked for on 2026-09-28 as "auto repair workbench"; on by default):
- Hook: a `CraftingStation.Interact(Humanoid, bool, bool)` postfix. The game's `Interact` (not on `repeat`, only for
  the local player, within `InUseDistance`, with `CheckUsable` passing its roof and fire rules) calls
  `Player.SetCraftingStation(this)` and `InventoryGui.Show(null, 3)` and returns false; that is the only place the
  game opens a station. The postfix acts when the station is the local player's current one afterwards, so a press
  another mod's prefix took (GrindstoneSkills' alt press on a kitchen station cycles its filter and skips the game's)
  or a refused open repairs nothing. OpenKeep's cart workbench opens through `station.Interact` too
  (`CartInteractPatch`), so a cart repairs as a workbench of `Cart Station Level` (its `GetLevel` postfix) with the
  workbench's `m_canRepair` and repair effect (copied in `CartStation`).
- The decision is the game's repair button's: `InventoryGui.UpdateRepair` shows the button only at a station with
  `m_canRepair` (checked here, since `RepairOneItem` itself does not), `HaveRepairableItems` (station usable here,
  a worn item `CanRepair` accepts) must be true, and the items are the worn ones (`Inventory.GetWornItems`) that
  `CanRepair` accepts: `m_canBeReparied`, a recipe whose crafting or repair station has this station's name or an item
  from a lower world level, and `Mathf.Min(GetLevel(), 4)` at least the recipe's `m_minStationLevel`; everything under
  `NoCostCheat`. The repair is `InventoryGui.RepairOneItem` called once per such item (the button's own method, each
  call repairs the first item `CanRepair` accepts: Crafting skill raised by `1 - durability / max`, durability set to
  the maximum), stopping at the first call that repairs nothing (a mod's patch refusing it), so whatever another mod
  adds to the button applies per item too. Then `UpdateCraftingPanel()` as the button does, because the upgrade list's
  durability bars are drawn when the list is built.
- One effect, one message: `RepairOneItem` plays the station's `m_repairItemDoneEffects` and shows the centre
  `$msg_repaired <item>` per call. The first call's effect plays; for the rest the station instance's field holds an
  empty `EffectList` (put back in a `finally`; the cart's station shares the workbench prefab's list object, so the
  field is swapped, never the list changed). The game's centre messages are not logged (`Player.Message` passes
  `log: false`) and the last centre text wins, so the mod's "Repaired n items" / "Repaired 1 item", shown after the
  calls in the same frame, is the one on screen; a mod's refusal message stays when nothing was repaired. Nothing is
  said when nothing was due (the game's "No more item to repair" comes only from a call with nothing due, which is
  never made). One log line per opening that repaired something.
- Multiplayer: repair is the local player's own business in the game: `RepairOneItem` changes `m_durability` in the
  player's inventory and raises a skill in the profile, sends no RPC and writes no ZDO, so every player on a dedicated
  server gets it on their own client with the server's synced setting; the server does nothing. The effect is the
  game's `EffectList.Create`, exactly as the button creates it.
- Items only in the player's inventory, as the button: equipped gear is in it; containers are not touched.

### Batch

- Asked for on 2026-09-28: "for workbenches, and any crafting in openkeep, - and + buttons with a number in the
  center, which is the amount to craft, then a crafting button". It is its own module and section (10) because it
  belongs to no storage module; it works with Reach off and without any container.
- The stepper crafts nothing itself: it drives the game's multi-craft (Shift + Craft makes `m_multiCraftAmount`, 5).
  `m_multiCraftAmount` is set to the amount and `m_touchMultiCrafting`, the flag a touch long press sets, to amount
  > 1, before the game's `UpdateRecipe` and `OnCraftPressed` read them. So the requirement rows (x amount, flashing
  red when short), the recipe name (`x<total>`), `HaveRequirements`, the room check, `ConsumeResources` with the
  multiplier (Reach's payment window pays the shortfall from containers as for one craft), the bonus rolls per craft,
  `RaiseSkill` and the statistics are the game's. GrindstoneSkills reads `m_multiCrafting ? m_multiCraftAmount : 1`
  in its DoCrafting prefixes and sees the amount.
- The amount counts crafts, not items: a recipe making 20 arrows makes 20 per step. Items that do not stack (weapons,
  armour) come out as separate items; the game's `Inventory.AddItem` splits by the max stack and `CanAddItem` checks
  the free slots first.
- Where: the Craft tab of every station and of crafting by hand (`InventoryGui.InCraftTab()`), never on the Upgrade
  tab (the game has no multi-upgrade), the Salvage tab or an upgrader station. Hidden while the craft bar fills; the
  Craft button gets its full width back whenever the stepper hides.
- The amount goes back to 1 when another recipe is selected and drops, every frame, to the most that can be made
  when the current amount no longer can (so after a batch that used the last materials it reads 1 or what is left).
  `+` greys out at the limit; `-` at 1. The limit is `Max Amount`, the materials (`Player.HaveRequirements(recipe,
  false, 1, n)`, so the station level and, with Reach, the containers count; skipped under `NoCostCheat` or the
  `NoCraftCost` world key) and room for `recipe.m_amount * n` items (`Inventory.CanAddItem`; single-ingredient
  recipes' quality bonus is not counted, the game still refuses a batch that does not fit). The largest n is a
  binary search, run on a click and when the amount must drop; a frame costs at most two checks (n and n + 1).
- Typing: click the amount (the whole number is selected) and type; the game's own `GuiInputField` (a
  `TMP_InputField`; in Steam's Big Picture and on the Steam Deck a click opens Steam's keyboard, whose text arrives
  through the field's `OnInputSubmit`, taken only while the field is not focused, since Enter raises it too) with
  4 characters. The game's field adds a validator of its own when it starts, and a validator replaces TMP's `Digit`
  rule, so the stepper puts a digits-only validator back every frame it shows. Escape in the field does not close the
  inventory (Recipe List's `TypingGuard`). `onEndEdit` (Enter, a click elsewhere, or Escape, which restores the
  old text) sets the amount through `BatchAmount.Set`, clamped to 1 and the limit, and shows what was kept; an empty
  field keeps the old amount. The field is not overwritten while focused. After Enter the field would stay the
  EventSystem's selection, and `Core/Keys.TextInputActive` silences every OpenKeep hotkey while an input field is
  selected, so `onEndEdit` deselects it (skipped while `EventSystem.alreadySelecting`, when a click is already moving
  the selection; the field has already stopped taking input, so the deselect does not end the edit twice). Typing
  is safe with the inventory open: `Player.TakeInput` is false there, so digits never use the hotbar, and `Chat`
  opens on Enter only while `InventoryGui` is hidden. The caret is white (TMP's default is dark grey), the text a
  fixed 26 (the caret sits wrong on auto-sized text), no colour transition, navigation off.
- No tooltips on the stepper (the user asked for none, 2026-09-28).
- Clicks: +/- 1; Shift to the next multiple of ten (1, 10, 20 ...; down from 25 is 20, from 10 is 1); Ctrl to 1 or
  to the limit. Keys are read with `Input.GetKey`, both Shift and both Ctrl, not configurable. Gamepad: the D-pad's
  left and right (`JoyDPadLeft`/`Right`, which the game repeats after 0.3 s held) through the clones' `UIGamePad`,
  only while the crafting panel is the active group; the clones' stick hint is removed (a `$KEY_` text needs the
  game's `Localize` component, which does not see clones), so there is no gamepad glyph. A held D-pad steps by tens
  (the Shift rule) after 8 repeats less than 0.35 s apart, only while the gamepad is the active input.
- Mouse wheel (asked for 2026-10-04, "scrolling"): over either button or the amount, up is +, down is -, with the
  clicks' Shift and Ctrl rules. `BatchWheel` sits on each part, not on the row: a single-line TMP field passes the
  wheel to a handler on its parent, which would then step twice.
- Time: the game's, times `Craft Speed` (2026-10-04, synced, 1 = the game's): the game's `m_craftDuration` (2 s),
  `m_multiCraftDuration` (6 s), `m_upgraderDuration` and `m_upgraderDurationPerLevel` are read at `InventoryGui.Awake`
  and divided by the speed in the `UpdateRecipe` prefix, the only place the game reads them; the crafting skill
  shortens them as usual, and GrindstoneSkills' skill book, which reads the fields, shows the sped-up times. It
  applies with `Enabled` off too. One craft takes `m_craftDuration`, any batch `m_multiCraftDuration`, both shortened
  by the crafting skill, as for the game's Shift + Craft. A held Shift no longer turns a single craft into a 6 s
  "x 1" multi-craft: the `OnCraftPressed` postfix sets `m_multiCrafting` from the amount alone, and Craft's label is
  plain `Craft` (the game appends ` x 5` while Shift is held).
- A started craft's amount is kept apart (`BatchDrive.Started`) and written back in a `Priority.First` DoCrafting
  prefix, since selecting another recipe or tab while the bar fills resets or releases the fields.
- `Enabled = false` (or the stepper not applying) writes the game's own `m_multiCraftAmount` back (read at
  `InventoryGui.Awake`) and clears the touch flag, so Shift + Craft makes 5 again.
- Reach's Pull modifier + Craft pulls materials for the stepper's amount (`BatchAmount.NextCraft`), else for the
  game's Shift rule as before.
- Layout (checked on the live panel and the offline UI dump): the Craft button's row `craft_button_panel` is 334 x 70,
  the button fills it less 5 above and below (offsets (0, 5) and (0, -5)). The stepper is 158 wide at its left end
  (44 button, 4, 62 field, 4, 44 button), full button height; the Craft button's left offset grows by 164. The
  buttons are clones of `m_qualityLevelDown` / `m_qualityLevelUp` (`Decription/UpgradePanel/LevelDown`, `LevelUp`, 40
  x 40, sprite `button`, inactive in the game), their label in the Craft label's font at 32; the field is the
  requirement slot's `item_background` image with a copy of the Craft label, white, inside a `RectMask2D` text area
  inset by 4.
- Multiplayer: crafting is the crafting player's client alone (the game's `InventoryGui`); the containers it pays
  from go through Reach's claim and save path as for one craft. All three settings are synced from the server and
  locked with `Lock Configuration`, so a server decides the batch size and the craft speed for everyone.

### Recipe List

- Asked for on 2026-10-04 with the tracker below, as a list of features: a text search over the recipes, prefixes to
  search by material, favourites with a favourites only view and a way to clear them all, grid views of several sizes
  beside the classic list, configurable sizing, type and colours, the on-screen keyboard and gamepad navigation. The
  crafting panel keeps the game's look (the user's rule since 2026-09-29: custom looks belong to PackPanel, every
  other mod uses the game's own UI): every new part is a copy of a game element (the build menu's search field, the quality and Style buttons, the build menu's favourite
  star, the inventory's tooltip). The "sizing, type and colours" part went to the tracker (Scale, Font, Font Size,
  colours, background) and to the grid sizes; the crafting panel itself is not restyled.
- Section 12 is all unsynced: it changes how the player's own panel shows and reacts, never what a craft does.
  Hidden recipes cannot be crafted while hidden (the game only crafts the selected row), so nothing needs a server.
- Where the search goes: a 30 unit row on top of the recipe list (the list's panel moves down and shrinks by 34;
  `m_recipeListBaseSize` with it), holding the field (stretched across the row, less two 30 unit buttons and gaps
  anchored at its right end) and the favourites only and view buttons. Nothing about the list is remembered: each
  panel update takes the list's current size as its full size, unless it is still exactly what this left it at, so
  PackPanel's larger crafting panel (Crafting Panel Width/Height, set at `InventoryGui.Show`) widens the row and the
  field with it. It shows on the Craft and Upgrade tabs while `Search` is on; on the Salvage tab
  the list is the game's height again (Salvage builds its own list in the same place). `Search = false` hides the row
  and with it the two buttons; the gamepad and the cfg still reach favourites only and the view.
- The field is a copy of the game's build menu search field (`BuildUi.m_searchField`, a `GuiInputField`): the same
  sprite, fonts, Ctrl + Backspace, and Steam's keyboard in Big Picture and on the Steam Deck. The copy gets fresh
  events (the build menu's own listeners are runtime ones and stay behind), loses its layout element and its key hint
  (F and the left stick are the build menu's keys), a smaller text (16) and a wider text area for the lower row, and
  OpenKeep's placeholder. It is copied at the first panel update after the HUD exists (the HUD's build menu is the
  source); when the field cannot be found the row stays off with a warning and everything else works.
- Search rules: case-insensitive substrings, against the item's name in the game's language and its prefab name (so
  `sword` and `SwordIron` both work in any language). `@word` matches a material the crafting panel lists for
  the recipe (at the listed quality, so an upgrade's materials, not the craft's; and by the game's upgrader rule,
  `RecipeNeeds`: at an upgrader station only upgrader materials, elsewhere only the others); `-` before either kind excludes. All words must hold. Spaces
  separate words; there is no quoting. The list rebuilds 0.12 s after the last key (the game's own
  `UpdateCraftingPanel`, so every other hook on it runs), scrolled to the top. Escape in the field clears it (TMP's
  cancel), Enter keeps the text; both let go of the field so the hotkeys work again. `Clear Search On Close` (on)
  empties it when the inventory closes. `Search Key` LeftControl + F (F alone is Stow's Favourite Item Key) puts the
  cursor in; the build menu uses F for its search, which would collide here.
- Typing guard: the game closes the inventory on Use (E), Inventory (Tab) and Escape with no regard for a focused
  field. While the search or the batch amount is focused, or was at the end of the last frame (TMP may have handled
  Escape and let go earlier in this frame), a `Priority.First` prefix on `InventoryGui.Update` resets the Use and
  Inventory button states and skips the game's update for a frame with Escape, so Escape only leaves the field.
  Skipping (rather than refusing `Hide`) keeps the inventory open without any Hide postfix (Shared's end of viewing)
  running for a close that never happened. The game opens no menu on Escape while the inventory shows. A gamepad's B
  or Y likewise leaves the field (presses dropped, the frame skipped): a pad without Steam's keyboard cannot type, and
  right stick up would otherwise strand it in the field.
- Copies of the game's buttons made at `InventoryGui.Awake` get their texts emptied at once: the scene's `Localize`
  starts later and caches every text it changes (`$inventory_style` on the Style button), writing it back on every
  language or input device change; an empty text is never cached. The Track label is also rewritten whenever it
  differs from what OpenKeep last set.
- Filtering: after the game's `UpdateRecipeList` (Craft and Upgrade tabs; it already sorted with the `sortcraft`
  setting) the hidden rows are destroyed and dropped from `m_availableRecipes`; the game then selects from what is
  left (`GetSelectedRecipeIndex` gives the first row when the selected recipe went), so only listed recipes can be
  crafted. Favourites first is a stable sort, so each group keeps the game's order.
- Favourites: per character in its custom data (like Stow's favourite items), keyed by the recipe asset's name, which
  is stable across sessions and languages; a recipe of a missing mod keeps its key. Middle click is the game's own
  favourite click in the build menu; Stow's Favourite Item Key (F) over a recipe row toggles too (over a slot it
  keeps its Stow meaning: Stow acts only on a hovered slot), and so does the star button under the recipe's name.
  The mark is the build menu's own favourite star (`BuildUiPieceButton.m_favoriteStar`: sprite `craft_icon_32`, the
  game's orange) on the icon's corner; OpenKeep's drawn star stands in until the HUD exists. Favourites only with no
  favourite is refused with a hint; removing the last favourite turns it off. Clear all is Shift + click on the
  favourites only star, behind the game's yes/no popup.
- Views: `List` is the game's (30 apart); `CompactList` packs rows 22 apart, the icon and marks scaled with the row;
  the grids use the list's width (187) split into 5, 4 or 3 square tiles (37, 46, 62). A tile hides the name and
  shows it in the game's tooltip (with x amount when a craft makes more than one), puts the quality level in the top
  left corner and scales the durability bar (the game's bar keeps the width it woke with). The game hides an
  uncraftable recipe's icon in its list (alpha 0; the grey name says it); a tile shows it greyed. The view button
  cycles forward (Shift: back) and writes the cfg; a change of `Recipe View`, `Search`, `Favourites` or `Favourites
  First` rebuilds an open panel at once.
- Tooltips (2026-10-05, the user: the tile's hover text was "so far away from item"): Epic Loot postfixes
  `UITooltip.OnHoverStart` and turns every tooltip with a `Topic` child and no `Scroll View` child into a scroll box
  of its item tooltip size (350 tall at least) set half its width beside the hovered element, which suits an item's
  long text, not a tile's name. `RecipeTips` therefore gives every OpenKeep tip a copy of the item slots' prefab, made
  once under an inactive holder, carrying an empty, inactive `Scroll View` child: Epic Loot passes it by and the tip
  shows as the game shows an item's, below and right of the pointer. Found by decompiling Epic Loot 0.14.13 into the
  scratch folder (no `CLEANROOM.md`); nothing of it is referenced.
- Track and favourite buttons: copies of the game's Style button (`m_variantButton`) under the recipe's name, from
  where the Style button starts (right of it while the game shows it). Hidden on the Salvage tab and with no recipe.
- Gamepad (`Gamepad Controls`): read in a prefix of `UpdateRecipeGamepadInput`, which the game calls only while the
  crafting panel is the active group. The game's free inputs there are the right stick's four directions (the game
  uses them only to scroll chat) and the left stick's left and right (only the hidden quality buttons have them);
  the face buttons, triggers, bumpers, D-pad and both stick clicks are taken (X craft, A style, B/Y close, LT/RT tabs,
  LB/RB groups, D-pad list and stepper, R3 repair, L3 the game's multi-craft). Right stick up searches (the field's
  `OpenKeyboard`: Steam's keyboard where available, else the caret), down tracks, left favourites, right favourites
  only; each acts once per push (the right stick directions repeat in ZInput). There is no glyph: the game's
  `$KEY_` lookup throws for stick directions (`rightStick_up` is not in its sprite map). In a grid the stick and D-pad
  up and down step a row and the left stick a tile; the D-pad's left and right step a tile only while the batch
  stepper (which owns them) is hidden.
- Multiplayer: nothing leaves the client; favourites and the view are the player's own.

### Recipe Tracker

- Asked for on 2026-10-04: pin recipes and their material lists on screen to watch them while gathering, the needs
  multiplied by the wanted amount, a draggable window with one-click removal, hidden in combat and with the full map.
- Section 13 is all unsynced: it shows what the player has and changes nothing in the world.
- Tracking: right click a recipe row (the game uses only left and middle there), its Track button, or right stick
  down. An entry is the recipe key, the quality it makes (1, or the next level from the Upgrade tab) and an amount
  in crafts, plus whether it was tracked at an upgrader station (the game lists other materials there, so that
  is an entry of its own); a new entry starts at the batch stepper's amount when that recipe is the selected one, else 1. The batch
  stepper cannot stand for the target (it stops at what can be made now, and a tracked recipe usually cannot be), so
  each entry has its own amount: - and + (Shift: to the next ten) and the mouse wheel over its header. `Max Tracked` 6.
- Counting, twice a second while shown: need = the game's requirement amount at that quality x amount; have = what
  the player carries (the game's `CountItems`, any quality, current world level) plus, with `Count Nearby Chests` and
  Reach's matching switch on, Reach's own count of the containers within reach. Rows and the recipe's name colour by
  Have / Missing / Ready. A recipe that takes any one material says so and is ready once one row is. The station and
  level the recipe needs show under the name.
- No untracking when ready: asked for on 2026-10-05 and dropped the same day, since an entry the player could
  already make once would leave before + could raise it. A ready entry stays, its name in the Ready colour.
- `Untrack When Crafted`: a DoCrafting prefix (`Priority.Low`, after Batch put the started amount back) notes how
  many of the item at that quality the player holds; the postfix compares: more means the craft happened (refusals,
  an upgrader's failure or break leave it as it was). The crafts (the multi-craft amount, 1 for an upgrade) count the
  entry down; at zero it leaves with a top-left message.
- Place: a column under `Hud.m_rootObject` (`hudroot`, full screen), so it hides with the HUD (the game moves the
  root off screen); top left anchored, 20 right and 330 down from the screen's top left by default, left of
  everything the HUD draws there. `Position` holds x right and y down (written at a drag's end); a place off the
  screen (another resolution, a hand edit) is pulled back on when applied, and the tracker is kept on screen after
  every rebuild. The title, the drag handle, is built once, so a rebuild (a tracked craft finishing) never ends a
  drag. It has a canvas
  of its own sorted at the inventory screen's order + 50 (600 + 50; the HUD canvas is 400, the store, chat, centre
  messages, menu and popups 700 and up), so it shows above the open inventory and its buttons get the pointer before
  the inventory's full-screen drop area. Only the title (the drag handle), the headers and their buttons catch the
  pointer, and only while the game has freed the cursor (`ZCursor`), so clicks on material rows reach the inventory
  below and an attack with a locked cursor never clicks it. Dragging keeps it on screen and writes `Position`.
- Look: the game's materials only: the crafting panel's dark item background at `Background Opacity`, the game's
  three fonts by name (`Font`), its orange for hover and Ready. `Scale`, `Font Size` and the colours are the "sizing,
  type and colours" the request asked for.
- Compact (asked for on 2026-10-05: tighter, same text and icon sizes): the panel is as wide as its widest row (a
  horizontal `ContentSizeFitter`); names are as wide as their words up to 11 points (header) or 9 (material) and end
  in ... past that, counts are at least 3 points wide so a digit more barely moves them. With the cursor locked an
  entry shows only its icon, name, the amount when more than one, and its rows; once the cursor is free (the
  inventory open) each header gains an X at its left (removes it; asked for as "an X in the upper left of the box")
  and - and + round the amount, and the panel widens for them. The notes' indent follows the X. Texts are never
  given a height below their line: with Ellipsis overflow TextMeshPro then draws nothing (2.0.0's - x1 + X were
  invisible that way, 1.2 and 1 point high boxes); the buttons' labels, the amount and the counts overflow instead.
- Hidden when: off, nothing tracked, dead, the large map open (`Hide With Map`), or a creature targeted the player
  in the last 5 s (`Hide In Combat`; the game's `Player.IsTargeted`, true for 1 s after a creature has the player as
  its target). The inventory being open does not hide it: that is when it can be dragged and edited.
- Multiplayer: the list lives in the character's custom data and the panel on the player's own HUD; nothing is sent.

### PackPanel

- The player's own inventory (a bigger grid, labelled slots, a key ring, backpacks, the stat panels and the brown or
  timber look) was built as this mod's section `10. Inventory` (`src/Pack/`) on 2026-09-28 and moved into its own mod,
  PackPanel (`../PackPanel/`), the same day, before any release: the user was told the UI and inventory work had to be
  separate from the storage mod. Storage and its own UI stayed here (the button row, the trash can and trash mode, the
  Salvage tab, marks, touches, hover texts). Its design, decisions and checklist are in `../PackPanel/CLAUDE.md`.
- Neither mod references the other. `Core/PackPanelLink` finds PackPanel by its GUID `milkyteam.packpanel` in BepInEx's
  chainloader on first use and reads its config entries through its plugin's `Config` (`1. Inventory / Enabled`,
  `3. Key Ring / Key Items` and `Key Stack`; the server's values while connected). What PackPanel publishes:
  - `PackPanel.mainGrid` in the character's custom data, `width|rows|blocked` (`Core/PackPanelGrid`, trusted only
    while PackPanel is loaded and enabled, since the key is saved with the character): Stow's `MainGrid` takes its
    rows, `Sorting` its blocked cells (a backpack's partly used last row), Shared's `ChestAsk` skips items below its
    rows for a shared chest's stack all.
  - `PackPanel_buttonstrip`, an empty child of the player panel, active while PackPanel keeps a 30 unit strip at the
    panel's bottom for the button row: `PanelButtons.Follow` (every frame, from `StowHotkeys`' `InventoryGui.Update`
    postfix) moves the row inside it, 2 units above the edge, and back below the panel when it goes. At
    `InventoryGui.Awake` the trash can is its own plate in the game's style under the armour unless PackPanel is
    enabled, when it is a button right of Sort (PackPanel's stats panel holds the column). OpenKeep builds no
    PlateColumn column of its own (the user's call, 2026-09-29: the brown boxes are PackPanel's, OpenKeep leaves the
    inventory's look alone).
  - Key Stack: one mod writes stack sizes. With OpenKeep present PackPanel leaves the keys to `Stacks/PackPanelKeys`,
    which raises every prefab in Key Items to at least Key Stack as its starting value (so per item entries and the
    YAML still win), also with the Stacks module off, and applies the values again when either entry changes (watched
    from the first database ready, when every plugin has loaded).
- PackPanel counts OpenKeep only from 1.8.0 (older versions know nothing of it): below that it keeps no strip and
  writes Key Stack itself.

### Build Camera

Section 11 (the user's request of 2026-10-04, from a feature list for a build camera; built from that list and the
game code alone). Everything runs on the player's own client: no ZDO key, RPC or file; pieces are placed, removed and
repaired through the game's own paths, so a dedicated server and the other players need nothing new.

- Entry: Toggle Key (B) or Gamepad Toggle while the game takes input and no build or radial menu is open, with the
  game's place mode on (`Player.InPlaceMode`: the hammer, hoe, cultivator or any modded build tool), standing inside a
  station's camera area. The camera starts where the game camera is (pulled into the area if it sits outside).
  Gamepad: the list named no gamepad toggle; the default, hold the left trigger and click the right stick, is free in
  the game's default layout (with `JoyAltKeys` held the click hides nothing). Pressed without a build tool: nothing.
- End: the key again, or at once when place mode ends (Hide, R, unequips the tool; so does the inventory), the player
  dies, teleports, sits at a seat with its own camera (a ship's helm), the game's free fly starts, `Enabled` goes
  off, or no station's area holds the player any more (the station was destroyed). The comfort conditions are checked
  on entry only, not while the camera is out.
- The area: every loaded crafting station with a build range (`m_allStations`; placement ghosts never register),
  `GetStationBuildRange` (with extensions) times `Range Multiplier`, measured flat as `HaveBuildStationInRange` does,
  and as far above and below the station (the game's build area is an endless column; the camera's is capped). The
  union of all stations counts. A move that leaves it is pulled back to the nearest point of the nearest area. Nothing
  of the station is changed: `m_rangeBuild`, the area marker and the `PlayerBase` effect area stay the game's, so raids,
  comfort and where pieces may be placed (`NoBuildStation` at the ghost) keep the real range.
- Flying: the movement keys and left stick pan flat along the view (not along the pitch, so W never dives), Jump and
  the right trigger raise, Crouch and the left trigger lower, Run (and the gamepad's run) multiplies `Speed` by `Run
  Multiplier`. The list asked for the triggers; in the game's default gamepad layout they are also place (RT) and
  rotate (LT), so a gamepad player moves the camera a little while placing. Look is the game's own look input
  (`SetMouseLook`, sensitivity and inversion applied), consumed so the player's body does not turn; the body's
  movement, jump, crouch, run, autorun, dodge and block are zeroed in `SetControls` (attack stays: place mode turns it
  into building; a seated player stays seated). No input while the game takes none (inventory, chat, map, menus).
- Collision: one sphere sweep (0.3 m, a little under the game camera's 0.35 m probe) with one slide, against
  `terrain` and `static_solid` only. Building pieces, trees and creatures do not block, so the camera enters houses
  through walls. Cave floors, walls and ceilings built as terrain (the list named the HearthBelow mod's caves) block the
  same way; there is no height clamp to the world's ground, which would break caves. Water does not block.
- Reach: the game's four build rays start at the game camera but compare the hit with the player's eyes. While the
  camera is out a prefix casts the same ray (origin, direction, 50 m, mask) and lets the game's check pass only when
  the hit lies within the player's own `m_maxPlaceDistance` (5 m, or what another mod sets) plus `Extra Reach` of the
  camera (plus the piece's `m_extraPlacementDistance` for placing), by setting `m_maxPlaceDistance` to +100000 or
  -100000 for that one call; the finalizer puts the original back, so EarthWright's and EliteCrafting's own writes to
  the field are untouched. `CheckCanRemovePiece` still asks for the piece's station at the player's position, and the
  hoe's level ground still levels to the ground height where the player stands (`UpdatePlacementGhost`): both the
  game's rules. EarthWright measures its brush aim from the eyes (`GhostAim.InReach`), so with EarthWright terrain
  edits reach only the game's distance from the player, not from the camera.
- View: `GetCameraPosition` is replaced while out (the near clip plane at the game's minimum, as the game uses beside
  walls); the sound listener stays on the camera (the game puts it on the eyes), so you hear what you see; light
  fading (`LightLod`) counts from the camera as in the game's own free fly, so torches by the camera stay lit.
- Camera Pickup (the list's "camera item pickup"): items within the player's auto pickup range of the camera fly to
  it and land in the inventory by `Player.AutoPickup`'s rules (the auto pickup key, the item's flag, not a piece, not a
  unique item already had, not in tar, room, carry weight, ownership first) and `Humanoid.Pickup`. The body's own auto
  pickup goes on as usual. With `Pickup Needs Resting` or `Pickup Min Comfort` unmet the items stay and the panel shows.
- Comfort conditions: "coziness" is the game's Resting effect (`SE_Cozy`, `s_statusEffectResting`: a fire, a roof, no
  enemy near), comfort is `Player.GetComfortLevel` (recounted every 2 s); both read where the player stands, not at
  the camera. Entry refused: a centre message naming what is missing.
- Pickup panel: under `Hud.m_rootObject` (hidden with the HUD), top centre plus `Pickup Panel Position`, the largest
  sprite image of the piece selection window as background and `m_buildSelection`'s font, material and colour at 80 %
  size; shown while blocked items lie by the camera, gone 1 s after or when the camera goes back.
- Head light: any light under the head bone (the `Head` transform above `VisEquipment.m_helmet`, else its parent),
  so the Dvergr circlet in the helmet slot and a circlet another mod (CircletExtended, RaziCirclet) hangs on the head
  from an extra slot are found without knowing those mods; torches in hand or on the back are not. The strongest
  lit one wins (looked up twice a second). The copy is a Light under the game camera, pointing where it looks, given
  every setting each frame (the range from `LightLod.m_baseRange` while the game fades it), so a recolour or switch
  by another mod follows; `Circlet Intensity`, `Range` and `Spot Angle` above 0 replace the worn light's own.

### Blueprints

- The user's request (2026-10-05): place builds like DevBridge's blueprints in game and shape the ground to fit (flatten
  a hill, dig water below sea level), with the ground under a home painted dirt. Built in EarthWright first, switched
  off for everyone the same day ("keep all code for now, remove the config option, do not allow anyone to use
  this"), moved here ("move this code for the building stuff into openkeep"), then given its switch back and its own
  tool ("make it a config option to turn this off or on. make a new hammer (use existing asset) that is called
  OpenKeep, and it will provide the building interface/hotkeys for this feature"). `14. Blueprints / Enabled` is
  synced and off by default. Later the same day: Fix ground, Ctrl + arrows for small turns (Alt since: Ctrl and Shift move the build camera), zooming out for large
  structures, "Build Without Materials" (off), stone for raised and lowered ground, construction sites ("ghost
  mode") with "Build As Resources Come In", the Site planner's queue and side panel with hover glow, and smart select
  (one house at a time); built by the lead and three agents from SPEC-Blueprints.md. Then, still the same day, the
  OpenKeep hammer was removed again at the user's request: the same tools moved into a new Blueprints tab of the
  regular hammer, with folders, naming and renaming, and a distinct icon per tool. Never run in game.
- Without EarthWright, OpenKeep writes the ground itself: one package per terrain compiler to its owner, which adds
  the change to the level delta as the game's own level operation does, so the game's limit holds (8 m from the
  generated ground; the HUD counts points that stop short). Protection is the game's sender-side check, as for a hoe.
- The interface is a "Blueprints" tab in the game's own hammer build menu, after By Usage, By Material, Recent and
  Favorites. The visible menu is `BuildUi` (BuildUIV2); the old `Hud.m_pieceCategoryTabs` window is switched off in
  `Hud.Awake` and never shown, so the tab is a copy of BuildUi's last tab under the same parent (a
  HorizontalLayoutGroup places it; read offline from the game's scene bundle), with its own piece list (it asks for
  the tag column but has no tags: the column, with the game's search field, holds the folder panel) and a place in the
  menu's TabHandler, so Q / E (TabLeft / TabRight) and the gamepad reach it. It shows
  only while `Enabled` is on and the hammer is the tool; hidden, its handler entry has no button (the tab keys skip
  it) and a menu that showed it goes back to the first tab. The entries live in the hammer's own table only while on,
  appended after the game's pieces, in one category the hammer does not use (the first of DeepNorth, Feasts, Food,
  Meads unused by its categories and pieces, logged at the first fill; Misc if all four are taken), so the hammer's
  own lists and selections never shift; that category is also added to the table's categories with the label
  "Blueprints" while the entries are in. The four game lists filter the entries out (unless the tab could not be
  made, then they stay in "All"); favourites refuse them.
- Selecting an entry previews it; the first click pins, the second builds; arrows turn, Home faces you again,
  Alt + Left / Right turn a degree (repeating), PageUp / PageDown (Alt: 2 m) move the floor, End puts it back,
  Backspace lets go; the wheel zooms (to 80 m). Fixed keys. The zoom comes out only while an entry of the tab is
  selected, never while building normally; the build camera comes out with B as always (no longer by itself, the
  user's rule of 2026-10-05) and then flies within 50 m of the player. The hammer's Remove button (the middle button by
  default) removes nothing while an entry is selected (asked by the lead: aiming a tool at a building must never take
  a piece down). Off: no tab, no entries, commands refused.
- The tab's order: Fix ground, Site planner, Copy building, Construction ghosts, then the blueprints of the folder
  shown. Folders are
  subfolders of `BepInEx/config/OpenKeep.Blueprints`; a blueprint is named by its path ("houses/plain_wood_house"); the
  folder shown is kept (back to the top when it disappears). Folders are only on the folder panel and the breadcrumb:
  the grid lost its Up and New folder entries ("get rid of the up icon where the blueprints go ... get rid of the
  icon for add folder where the blueprints go") and then its folder entries ("also remove the child folder icon from
  where the blueprints are. We can navigate via the left file organizer and top bar"), all on 2026-10-05. Opening,
  dropping into and renaming folders happen there; a folder itself cannot be dragged (the panel rows are not drag
  sources), so it moves by a rename to a path ("houses/old"). Entries are named by the file name made readable; a blueprint's name in game (HUD, site) is its file name, whatever
  the JSON says. The blueprint the player chose stays selected while other folders are shown (kept in the table; the
  game stores a selection as a place in a category's list, so it is put back after every refill).
- F2 renames: with the menu open the blueprint under the mouse, with it closed the selected blueprint; the
  text box starts with its name. A plain name renames it in its folder; a name with "/" is a path from the top
  folder ("houses/barn" moves it into houses, made when missing; "/barn" moves it to the top); "." and ".." parts,
  names that are not valid file names, taken names and a folder into itself are refused with a message. Only the
  file's name changes, never its content; a pinned or selected blueprint follows its file. The game's own F2 (the
  connection panel) is put back for a press that renames. A right click on a blueprint in the menu, or on a folder
  of the panel or the breadcrumb, renames it the same way (the user, 2026-10-05: "lets make right clicking a blueprint
  or folder change the name"). The
  game's piece buttons have no right click of their own; the right button is the game's Build Menu key, which closes
  the menu in `BuildUi.NavigationUpdate`, so the press is read from the mouse in a `BuildUi.Update` prefix that skips
  that frame of the menu. Right click on a folder of the panel or the breadcrumb (not the top) renames it too.
- The name box over the open menu (the user, 2026-10-05: "when right clicking on an icon don't close the blueprints,
  just have the pop up show up"): right click, F2 and the New folder button open the game's text box over the build menu, which
  stays open on its tab, folder and scroll. The game draws the box above the HUD (its canvas sorts at 1100, the HUD at
  400) and focuses its field. While a box is up (`NamePrompt.Showing`: the game's own test, true for the frame its Esc
  or Enter closes it, or the box's panel shown) the menu's `Update` is skipped (F would focus its search field, Esc and
  the Build Menu key would close it, gamepad buttons act), its tab handler's Q / E are held, and clicks, drags and the
  selection box in the tab do nothing; the player's own keys (hotbar, movement) are the game's, already held by the
  box. Esc closes only the box; after OK the tab is filled again in place.
- The folder panel (the user, 2026-10-05: "have kind of a file manager to the left ... the parent folder has a up
  arrow and name, then all the folder names inside the parent"): the build menu's own left column, where the other tabs
  list their tags under the search field, shows folders while the Blueprints tab is shown (inside the menu's frame
  rather than beside it: the game's own look and scrolling, on screen at any resolution, and the search field now works
  on this tab too). First the folder above with the up icon (a click goes up; at the top this row is "Blueprints" itself,
  highlighted, inert), then every folder in it with the shown one highlighted and its own subfolders indented under it;
  at the top, the top's folders. At the right end of the first row sits the New folder button (the user, 2026-10-05:
  "PUT THE NEW FOLDER icon to the right of the parent folder ... kinda like the F and Q things"): a copy of the build
  menu's own key badge (the "Q" beside its tabs, `TabContainer/InputHelp/MK hints/Left`: its key_base sprite and grey)
  holding foldernew.png, brighter under the mouse, with the game's tooltip (the prefab its piece icons use) naming
  the folder the new one goes into. It is a Button of its own on the row, so its click never reaches the row (no going
  up); it asks for a name over the open menu and makes the folder inside the folder shown. The rows are copies of the
  game's tag button (its hover, highlight and gamepad
  navigation) with a small folder icon, OpenKeep's own, after the game's buttons in the column; the game's "All" row is
  hidden on this tab and given back on the others. Rebuilt when the folder or the files change; the wheel scrolls it.
- The breadcrumb ("across the top it shows the folder chain you're in"): a strip across the top of the piece list
  (the list moves down by its 30 px while the tab is shown), "Blueprints > houses > nordic", the shown folder bold and
  inert, every other part a button (underlined under the mouse). Too long a chain loses parts from the left behind
  "...", which opens the folder above the first part shown; laid out again when the folder or the strip's width changes.
- Panel rows, the panel's up row and breadcrumb parts are the drop targets of the drag and drop (green frame); the
  folder shown never is.
- Remembering the folder ("remember what folder their in so they don't have to keep going back to same place when
  opening and closing hammer"). Found in the decompile: closing the menu or putting the hammer away keeps its tab
  (`BuildUi.Close` keeps `m_currentPieceList` and `m_currentBuildTool`), but `OpenBuildMenu` starts on the first tab
  whenever the build tool changed since it was last open (after the hoe or the cultivator) and a new menu (another
  world, a restart) starts on its first tab; the folder lived only as long as the game. Now the tab and the folder are
  kept on this machine in `BepInEx/config/OpenKeep.BlueprintsTab.txt` (`folder=houses/nordic`, `tab=true`; a file, not
  PlayerPrefs, so it can be read and deleted; never synced), read when the hammer's table is first filled (a folder that
  is gone falls back to the top) and written when either changes. A menu opened with the hammer goes back to the
  Blueprints tab when that was the last tab shown with the hammer.
- Names on icons (the user, 2026-10-05: "The foldername should be visible on the icon", "each blueprint you create
  should have the name on the icon"): every blueprint entry shows its name on a dark band along
  the bottom of its icon, in the menu's own font (taken from its tab labels) with a black outline, at most two lines,
  cut with an ellipsis. Text children on the game's buttons rather than names baked into textures: the buttons are
  64 px cells (read from the scene bundle) and the menu scales them, so live text stays sharp, needs no texture per
  entry and follows a rename at once; the menu reuses its buttons, so a `BuildUiPieceButton.Setup` postfix sets the
  band (and hides it for any other piece) every time a button is given a piece. The tools keep plain icons.
- Several at once, as in a file explorer (the user: "holding click and drag to select multiple", "clicking 1 then
  holding shift and clicking another will select all between those 2, and holding CTRL and clicking will select
  multiple one at a time"): only blueprints can be picked. Ctrl + click toggles one (it becomes the
  anchor); Shift + click picks everything from the anchor to it in the tab's order (Ctrl + Shift adds the range to the
  picks); holding the left button on empty space of the list and dragging draws a gold box that picks what it touches
  (with Ctrl added to the picks), and a plain click on empty space clears them. Picks have a gold frame. The game's
  sneak key is Ctrl and it still reads the player's keys with the menu open, so a Ctrl + click there does not toggle
  sneaking. A plain click
  on an entry clears the picks and does what it always did (it is selected and the menu closes). The picks clear when the menu closes, it shows another tab or another folder. The box is an invisible area
  behind the buttons inside the list's viewport, active only on this tab: as a child of the list it takes the drag
  the list would otherwise scroll by (dragging empty space scrolled the list before), while the wheel is not a drag
  and still scrolls; the box is kept in the list's space, so scrolling while drawing keeps its start.
- Drag and drop (the user: "I should be able to drag and drop the building blueprints ive created into folders"):
  a press on a blueprint that moves past the event system's drag threshold drags it, or every pick when it is one
  of them; a short press stays a click. A drag handle on the game's button is enabled only while the button shows a
  blueprint (Unity sends drag events only to enabled behaviours), so every other button's drag
  still reaches the list's scrolling; the drag marks the press as no click. A see-through ghost of the icon with a
  count follows the mouse, the folder under it gets a green frame (a folder panel row - its first row for the folder
  above - or a breadcrumb part), and letting go there moves the blueprints into that folder one by one through the
  rename path (a path from the top folder): a name already taken there refuses that one at the top left and the rest
  still move; then the picks clear and the tab is filled again. Anywhere else nothing happens. A drag the event system loses
  (the menu closed) ends by itself.
- A site's ghost is one faint white film you see through (the user, 2026-10-05: "that clearish ghost like color",
  then "barely visible ... can play the game normally and see through it"). A solid blue tint came first, then
  see-through faces on Sprites/Default, whose layers stacked into a glowing white block. Now every ghost renderer
  draws twice after all else in the frame (queues 3990/3991, so water and smoke are never hidden): depth only
  (Unity's Hidden/Internal-Colored), then a child renderer on the same mesh draws white at 12 % where that
  depth is the nearest, so only one layer shows; no shadows. Then ("ghost like ... you can still make out the
  details") the film became the piece's own texture on Sprites/Default, lightened and cooled (colour 1.01, 1.34, 2.4: the wood's
  yellow read green) and 4 % opaque, tuned live with the user, still
  one layer thanks to the depth pass; GhostLook.Tune(alpha, brightness, texture) changes it live through DevBridge
  eval. Planner glows colour it 40 % opaque.
- Copy building (the user's request, 2026-10-05: "a way to copy existing buildings and allowing selecting multiple
  builds (if someone wants to copy a compound), have those same kinda controls"): with the entry selected and the menu
  closed, a click selects the piece under the crosshair (no Ctrl + click: Ctrl lowers the build camera), Shift + click
  its whole building: every piece joined through touching drawn boxes, as Fix ground finds it, plus everything inside
  it (a piece whose centre lies within a building piece's footprint and between its lowest bottom and highest top,
  and what touches that); at most 4,000 pieces within 90 m, so a huge compound takes several clicks; or lets it go
  when all of it was selected (the user's choice: holding Shift highlights the whole building); G + click
  the joined pieces of the same prefab; holding Shift or G previews what a click
  takes; Backspace clears; Enter asks for a name ("Building N", the first free) and saves the selection into the
  folder the tab shows. Several buildings make one blueprint. The frame faces the camera (yaw snapped to a quarter
  turn), the ground is the most common ground height under the pieces, and relief and water are kept as the console
  save does. Only player-built pieces, never ships, carts or site posts; anyone's pieces may be copied (copying
  changes nothing). The selection and its glow are this machine's only: gold selected, pale blue what a click adds,
  red what it lets go, through the game's own piece tint held in MaterialMan (at most 200 pieces set a frame, nothing
  while the selection stands still); the game's hover tint is held back while the entry is selected.
- Construction ghosts (the user, 2026-10-05: "can we have some option in the hammer to turn off unbuilt building
  outlines?"): a switch entry after Copy building. A click on it in the menu flips it and the menu stays open (it is
  never selected for building); its icon (an eye, crossed out in red while hidden), its band ("Ghosts shown" /
  "Ghosts hidden") and its description show the state, and a top-left message says the new one. It is this player's
  only (client-only, never synced), kept as `ghosts=` in `OpenKeep.BlueprintsTab.txt`. Hidden: every site ghost on this
  machine has its root switched off, ghosts loaded later start hidden, no copies are made meanwhile and
  `SiteGhost.Pick` finds nothing; while the Site planner entry is in hand the ghosts show anyway, since the planner
  works on them. The site posts, their hover text, delivery and building are not touched.
- Each tool has its own icon, 128 px, drawn in the style of blueprint.png and fixground.png (thick ink outline, warm
  wood and straw, the grass-and-earth slab): the Site planner a ghost house and a ticked clipboard, Copy building a
  house with its pale copy and an arrow, Construction ghosts a big eye in front of a ghost house (ghostsshown.png),
  crossed out by a red bar while hidden (ghostshidden.png). The folder panel's rows show a small wooden folder with a blueprint sheet
  (folder.png), its up row the folder with a big yellow arrow (folderup.png), and its New folder button the folder
  with a green plus (foldernew.png).
- The second click places a construction site (SPEC-Blueprints.md section 1): a post 2 m in front holding the state,
  a ghost every player sees, materials handed over with E, Shift+E (creator or admin, checked by the server) takes it
  down and drops what was handed over. The post's owner builds: with "Build As Resources Come In" one piece every
  0.125 s (ground first, then the queue, then the rest; a site whose looks change nothing backs off to every 2 s
  until its ZDO changes, e.g. a delivery), off all at once when everything is there; "Build Without
  Materials" or no-cost mode builds at once. A site near the world centre owned by a dedicated server is taken over
  by a client that has it loaded.
- Players pay every piece's materials (into the site) and must have learned every piece; no-cost mode pays nothing,
  "Build Without Materials" pays nothing but still needs learned pieces. Raised ground costs 0.5 Stone per m³ and
  lowered ground gives as much back; only the difference counts. Any player-built piece in the way refuses the site. Water is the world's sea level: a blueprint with water sets its floor from the
  sea. Clearing removes without drops and never takes ore. Pieces skip `WearNTear.OnPlaced`, so support is first
  checked 30 s later, when the whole build and its ground stand.
- Stability (asked 2026-10-05): a queued selection is built together with the unbuilt pieces that hold it up, lowest
  first (`SiteSupport`: from every piece on the ground, support spreads over touching boxes, resting on a piece below
  costs 1, beside 4, hanging 6; each piece keeps its cheapest path). Checked offline: a house's roof alone brings
  its walls and posts (plain_wood_house 42 roofs + 46 supports, mead_hall 144 + 81), 4-9 ms a house, 118 ms the compound.
- Site planner G (hold): the hover glow shows the joined pieces of the same type (a wall run, a roof slope); G + click
  selects or lets them go. Shift + click stays the whole house.
- Putting the hammer away with a blueprint pinned places it as a construction site (as the second click): its
  unbuilt pieces stay as a ghost. Another piece picked in the hammer's menu only lets the pin go; death never places.
- `openkeep blueprint list | save <name> [radius] [all] [replace] | undo` (refused while off); `list` shows every folder
  and blueprint as paths and the folder the tab shows; `save` writes into that folder, keeps a 1 m relief of the
  ground and water where it lay below sea level, and the new file shows in the tab within seconds.

## Not yet implemented

- Capacity: the read-only container grid on hover (SPEC section 5's stretch goal); no setting is bound for it.
- Carts: the cart extension piece parented to a cart (SPEC section 6's stretch goal); no setting is bound for it,
  `OpenKeep.cartOffset` stays reserved.
## Test checklist (LocalTesting profile)

Launch through the r2modman profile `LocalTesting` (the build copies the DLL there). Never start or kill the game
from a script.

1. Log shows `Loading [OpenKeep 2.4.0]` without failed patches; `milkyteam.openkeep.cfg` and the seven YAML files
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
   off) is not toggled repeatedly. Add `stations: { piece_cookingstation: { deny: [NeckTail] } }` to the Reach
   YAML: neck tails in a chest are no longer borrowed, filled or pulled onto the cooking station, its hover count
   drops, and crafting still counts them; `charcoal_kiln: { enabled: false }` removes the kiln's feeding and hover
   line entirely (verify the prefab names against the log's `openkeep containers` style output or the sign; they
   were written from memory, not the assets).
4. Stow: check the button row below the player panel and the Sort button below the container panel's Take all
   line overlap no item slot and no game text (with and without a chest open; note what `Button Row Offset` a
   clean layout needs, and that a changed offset moves the buttons at once); quick stack, store all, take all with
   Shift+G (and in a shared chest in Full and View mode), top up, sort by each order (Amount: most held first),
   favourite item and slot (star, border, cross drawn), trash with confirmation, destroy junk, route with
   Ctrl click (with and without a chest open), store one with V, find (line and floating count), dump
   outside the inventory, cycle with arrows and wheel between three chests within 4 m. Favourites survive a relog.
   `Sort Favourite Items` off: sorting the inventory leaves a favourite item's stack in its cell (and still merges
   and sorts the rest); on (default): it is sorted as before.
   Gamepad: the buttons are selectable, the popup confirms and cancels.
   Stat plates: on the player panel's right the armour plate is on top, the trash plate in the middle and the
   weight plate at the bottom, evenly spaced (with Elite Creatures Reborn and world tiers on, its globe plate joins
   under the weight and all four move up, evenly spaced); hovering each shows, after half a second, a dark box with a gold border just right of the plate that names it and stays put while the pointer moves over the plate; the shield and the weight fill most of their wood, centred, with the
   number readable on top (the weight flashes red when over the limit); the grey metal bin sits on its own wood,
   turns red on hover, and a stack dragged onto it is destroyed; the button row is four buttons with no can. The log
   shows `OpenKeep.assets.trash.png: 128x128, 8 mip levels`
   (fewer levels means the game dropped the mipmaps: the bin may shimmer when small) and `trash can on its own
   plate`. With `devcommands`,
   `inventorysize 6` grows the panel downward and the three plates stay where they were (use a test character:
   the size is saved with it, and going back with `inventorysize 4` drops what sits in the removed rows).
5. Ground pickup on with `pickup: true` for the chest's prefab in `OpenKeep.Stow.yml`: drop copper ore near a
   chest holding copper; after the delay it is inside the chest. An item inside a stranger's ward is not taken.
6. Salvage tab appears after Upgrade (position with and without a station), lists an iron sword, returns the
   rounded fraction; with a full inventory the button is disabled and the hotkey says so. Backspace on a hovered
   stack asks first. `Trash Uses Salvage` on: Delete salvages instead. Gamepad steps through the list. An item
   with an `ecf_` custom data key (an EliteCrafting magic item) is not listed and Backspace on it says "Another mod
   keeps data on this item"; `Skip Items With Mod Data = false` lists it again.
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
    only`, the buttons are greyed), B's Y and Store all say `Viewing only`, B's Ctrl click routes elsewhere; A
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
    inventory). Stop A's client while B has a request pending: B sees `The chest did not answer` after 2 s, and
    a stack B was putting comes back to B's inventory within 30 s (nothing is in two places).
    Hand-over (2026-10-06): A opens and closes a chest, walks off but stays near; B crafts from it: the first craft
    may say `Fetching the materials from storage, try again` (log on A: `handed ... over to peer`), the next press
    crafts and pays from the chest. B quick stacks into a chest A owns and nobody uses: it goes by request (A's log
    shows `request ... put`). A sails a ship with wood in its hold; B crafts at a workbench near it: the hold is not
    counted for B, and A's ship is never taken over. Site delivery to a post another machine owns: `Handed over` comes
    with the answer; with that machine gone, `The site did not answer yet` and the materials return within 30 s.
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
23. Beds, single player: claim bed A under a roof (log: `bed at x:y:z remembered, 1 known in this world`), claim bed
    B a few hundred metres away (2 known). Hover A: `[E] Sleep`; at night E on A sleeps with the game's checks. With
    `devcommands`, `die` near A wakes you in A, near B in B, in a dungeon below A's area in A (log: `died at ...;
    waking in the bed at ...`). Die, then log out within 10 s: logging in puts you at the nearest bed.
24. Beds: remove B with the hammer (log: forgotten). Break a bed with damage while elsewhere, then die next to it:
    log `no bed of yours at ...; trying the bed at ...`, and after the load wait you wake in the next nearest. With
    every bed gone you wake at the world start. `Nearest Bed Respawn = false`: an own non-current bed shows "Set
    spawn point" and death returns you to the last bed set. An old character's bed claimed without the mod is
    remembered once you walk to it.
25. Beds, dedicated server, clients A and B: each log lists only its own beds; B hovering A's bed sees no action;
    A destroys B's bed, B dies near it: B wakes at another bed of B's or the world start. The server log shows
    nothing from the bed feature; Lock Configuration keeps B from changing the setting.
26. Fires: the log shows `fire_pit may be built on wood (the game's m_notOnWood: True)`; a campfire places, burns,
    smokes and gives comfort on a wood floor; a bonfire over wood stays red; a steep roof or a wall side stays red;
    a stranger's ward refuses. With the ghost over a wood floor, empty `Build On Wood`: the ghost turns red without
    reselecting, built fires stay. `fire_pti` logs a warning. Removing the floor under a fire breaks it. Hazard
    (test world): Ashlands or `setkey Fire`, embers start small fires on a dry wood floor within a minute or two.
27. Fires, dedicated server: server `fire_pit`, A's own cfg empty: A may build on wood after joining (the server's
    value); clearing the server's value turns A's held ghost red within seconds.
28. Beehives, single player at a hive: the log shows `make honey at the game's own rate`. `Honey Per Day = 170`
    (about 10.6 s per honey): an emptied hive has 4 within about 50 s. `Honey Per Day = 10`, two minutes after a
    honey take it and set 100: the next tick adds at most one, then one about every 20 s. `Honey Per Player Online`
    in single player: `1 honey per day, one every 1800 s`. A bird nest keeps about 20 minutes per feather.
29. Beehives, dedicated server, `Honey Per Player Online` on, A at a hive and B far away: A's log shows `2 honey per
    day`; B disconnects: `1 honey per day` within 10 s. A leaves and B walks to the hive: B's log shows the rate and
    the hive's count carries on.
30. Stow ground pickup with a fresh cfg: `Pickup Range = 2`; an item dropped 3 m from the chest stays on the ground.
31. Auto Fuel, single player: a chest with 20 wood within 20 m of a new bonfire (0/10): within 2 s it shows 10/10,
    the chest holds 10, one fuel-added puff, debug log `bonfire refilled itself with 10 Wood from containers
    near it (10/10)`. With devcommands, `skiptime 5000`: the campfire drops a unit and is full again within 2 s. A sconce
    15 m from a resin chest does the same after `skiptime 20000`. A chest 25 m away gives nothing.
    `stations: { hearth: { enabled: false } }` in OpenKeep.Reach.yml: the hearth no longer refills;
    `containers: { piece_chest_wood: { deny: [Resin] } }`: torches take resin only from other chests. The resin candle
    is never refilled. `Auto Fuel = false`: nothing refills.
32. Torches, single player, `Torch Margin = 1`: `tod 0.5`: every standing torch and sconce goes dark within 2 s,
    silently, the hover says `Lights at nightfall` and `[O] Keep lit`, its fuel stays; a campfire stays lit and shows
    no O line. A torch built now goes out within 2 s. `tod 0.29`: still out; `tod 0.27`: lit (the margin after
    daybreak); `tod 0.71`: out; `tod 0.73`: lit (the margin before nightfall); `tod 0.9`: lit. `Torch Margin = 0`:
    `tod 0.27` and `tod 0.73` are out. `tod -1` and sleep: out about 75 s after waking, nothing flickers during the
    skip. Log out and in at noon: torches stay out, no flash of light while loading. `Candle_resin` in Torch Pieces:
    at night E puts it out and it stays out until daybreak. `Torches Night Only = false` by day: every torch lights
    within 2 s.
33. Fires, dedicated server, clients A and B at one base: both see every torch go out Torch Margin after daybreak and
    light Torch Margin before nightfall together; A walks away (B owns everything): no torch flips. A comes back and
    presses O on a torch B owns: it lights on both screens within a second, B's log shows `kept lit day and night
    (asked by peer <A's id>)`; A leaves again and B logs out: when A returns alone the torch is still kept lit (the
    flag lives in the ZDO); B presses O on it by day: it goes out for both. A and B press O on the same dark torch at
    once: it ends up kept lit. B holds the resin chest open: A's torches take no resin from it until B closes it.
    Lock Configuration keeps B from changing Auto Fuel and Torch Margin, while B can rebind Torch Switch Key.
    The server log shows nothing from these features for a base far from the world centre.
34. Auto Feed, single player: a new smelter with a chest touching its side holding 30 copper ore and 30 coal: within
    about 10 s the hover shows 10/10 ore and 10 coal and rising to 20, one added sound per unit, copper comes out;
    the chest's ore drops by one about every 30 s until one copper ore and one coal are left, which stay (`Auto Feed
    Leave = 0`: the chest empties). A chest 5 m from the smelter's side gives nothing; `Auto Feed Range = 6`: it does. A charcoal kiln beside a chest with wood, fine wood and core wood takes only the wood (the log
    at debug level: `charcoal_kiln fed itself one Wood`); empty `Auto Feed Skip` and it takes the others too. A
    spinning wheel with flax, a windmill with barley, an eitr refinery with soft tissue and sap, a hot tub with wood.
    `stations: { smelter: { enabled: false } }`: the smelter stops taking; `containers: { piece_chest_wood: { deny:
    [Coal] } }`: it takes coal only from other chests. `Auto Feed Stations = false`: nothing is fed.
35. Auto Feed, dedicated server, clients A and B at one base: the station is fed once, by whoever's game owns it
    (the other log shows nothing); B holds the ore chest open: nothing is taken from it until B closes it. Lock
    Configuration keeps B from changing Auto Feed Range.
36. Rested sooner, single player: a fresh cfg has `Rested Delay = 5`. Sit by a campfire (or stand by a fire under a
    roof): `Resting...`, then about 5 s later `You feel rested (Comfort: n)`; the first rest logs `Rested comes after
    5 s of resting (the game's own delay is 20 s)`. The Rested icon's time is the game's (8:00 at comfort 1, a minute
    more per comfort level). `Rested Delay = 20`: about 20 s, as in the game; `0`: Rested together with Resting. Set 60,
    rest 10 s (no Rested), set 5: Rested at once. Step away from the fire before the wait ends: no Rested, and the
    wait starts over on return. Wet from rain outdoors: no Resting, as in the game.
37. Rested sooner, dedicated server with clients A and B, server `Rested Delay = 5`, A's own cfg 20: A and B each rest
    at a fire and get Rested about 5 s after Resting; both logs show the 5 s line. Change the server's value to 30:
    the next rest of each takes about 30 s and both logs show the new line. Lock Configuration keeps B from changing
    it. The server log shows nothing from this feature.
38. Area repair, single player: hit a 3x3 wooden floor and the walls standing on it with a club until they look
    worn. Repair the middle tile with the hammer: it and every touching damaged piece (the eight tiles around it, the
    walls on it) turn new at once, each with the build puff; top-left shows the game's repaired line and `Also
    repaired n pieces touching it`; the log `OpenKeep: area repair around wood_floor: n of m touching pieces
    repaired, ...`. A worn tile two tiles away stays worn. Stamina drops by one swing, the hammer by one use. Hitting
    a whole piece says the game's "does not need repair" and repairs nothing around it. `Area Repair = false`: only
    the hit piece.
39. Area repair checks: a damaged stone wall touching the repaired floor with no stonecutter near stays worn (log `1
    without their station in range`); with a stonecutter it is repaired. A damaged dock piece next to a moored,
    damaged Karve: the dock is repaired, the ship not. A damaged chest standing on the floor is repaired with it.
40. Area repair, dedicated server, A and B: B reaches the base first (B's game owns the pieces), then A repairs a
    floor tile: the touching pieces turn new on both screens. B places a ward A is not permitted on, reaching just
    over one tile's edge; A repairs a tile just outside it: its damaged neighbour inside the ward stays worn (A's log:
    `1 in a ward`), no ward flash. Lock Configuration keeps B from changing Area Repair; the server log shows nothing
    from the feature.
41. Keep lit, single player: at noon look at a dark torch and press O: it lights at once, message `Standing torch:
    kept lit day and night`, hover `[O] Light at night only`, log `... kept lit day and night (asked by peer ...)`;
    `tod 0.3` to `0.6`: it stays lit while the others stay out; it keeps refuelling from a chest (Auto Fuel). Press O
    again by day: it goes out at once, message `... lit at night only`, hover `Lights at nightfall` and `[O] Keep lit`;
    at night (`tod 0.9`) press O twice: it stays lit both times. Log out and in: a kept torch is still lit by day.
    `Torches Night Only = false`: the hover shows no O line and O does nothing; on again: the kept torch stays lit,
    the others go out. Take it out of `Torch Pieces` and back: still kept. Rebind `Torch Switch Key` to
    `LeftAlt + O`: the hover shows `[LeftAlt + O]`. O with the inventory, chat or map open does nothing; O while
    holding the hammer does nothing (no hover object).
42. Keep lit and wards: A places a ward over a torch, B (not permitted) hovers it: no O line; B presses O: the ward
    flashes and nothing changes (no log line on the owner). A permits B: the O line appears and works.
43. Station capacity, single player: after a world loads the log shows `listed every station prefab in
    ...OpenKeep.Stations.yml` and `station capacities applied, 0 prefabs`; the file lists `BatteringRam`,
    `blastfurnace`, `charcoal_kiln`, `eitrrefinery`, `piece_bathtub`, `piece_FrostKiln`, `piece_spinningwheel`,
    `smelter` and `windmill` commented out with only the caps each uses (kiln `{ items: 25 }`, hot tub `{ fuel: 10 }`);
    `openkeep write docs` writes `OpenKeep.Stations.txt`. `smelter: { items: 30, fuel: 60 }`: within seconds `station
    capacities applied, 1 prefabs`, an existing smelter's hover reads `(0/30)` and `Coal 0/60`, it takes 30 ore and 60
    coal, a new smelter and a relog show the same; Shift + E with ore only in a chest fills to 30, and Auto Feed fills
    it to 30 too. With 30 queued lower to `items: 10`: the hover shows `(30/10)`, adding is refused, all 30 are
    smelted and it takes ore again below 10. Destroy a smelter holding 25: all 25 ore and the coal drop.
44. Station capacity checks: `charcoal_kiln: { items: 50, fuel: 10 }` holds 50 wood and warns `charcoal_kiln takes no
    fuel ... ignored`, still running without fuel; `piece_bathtub: { items: 5 }` warns and the tub runs on wood alone.
    `items: 0` or `items: 2000` logs a YAML error and the previous caps stay. Removing the entry brings back 10/20 on
    existing and new smelters. `5. Capacity / Enabled = false` restores every vanilla cap; true restores the file's.
    "Edit station capacities" opens in the Configuration Manager.
45. Station capacity, dedicated server with A and B: the server's log lists the stations on its first world load and
    the clients' files are not rewritten. Server `smelter: { items: 30 }`, A's own file different: A and B both see
    `/30` and add up to 30; the server edits it to 40 and both see `/40` within seconds. Lock Configuration keeps A
    from changing it. B owns the smelter (standing there first), A adds ore to the cap: B's hover counts it. A station
    over its cap after the server lowers it keeps working on both clients and loses nothing.
46. Hotkeys with modifiers: holding W, `LeftAlt + D` (Dump Key) and `LeftAlt + R` (Reach toggle) fire.
47. Pets, single player: a hungry tamed wolf, no meat on the ground, a chest 8 m away with raw meat: within 10 s the
    wolf walks to the chest and eats one (the chest has one fewer; the debug log line `Wolf ate one RawMeat from
    piece_chest_wood`); it eats again only when hungry. Meat on the ground 3 m away is eaten first. A chest 12 m away
    is left alone, and so is everything with `Pets Eat From Chests = false`. A wolf still being tamed does not eat
    from the chest.
48. Pets, dedicated server, A and B: B reached the base first (B's game runs the wolf): the chest loses one meat on
    both clients and A sees the wolf eat. A opens the chest while the wolf walks to it: the wolf gives up and tries
    again at a later search.
49. With PackPanel installed (see `../PackPanel/CLAUDE.md`, its items 2 and 3): the button row and the trash can sit
    inside the inventory panel and follow PackPanel's `Enabled` at once; sort keeps out of a backpack's closed cells;
    quick stack takes nothing from the slots; PackPanel's Key Stack holds unless `OpenKeep.Stacks.yml` names the key.
    Without PackPanel: the row hangs below the panel, the trash can is a plate in the game's style under the armour.
50. Batch crafting, single player, at a workbench with 30 wood and 10 resin in the inventory: the Craft tab shows
    `- 1 +` left of a narrower Craft button; select Torch (1 wood, 1 resin), `+` five times:
    the field reads 6, the requirement rows read x6, Craft makes 6 torches in one bar (about 6 s) and uses 6 of each.
    Ctrl + `+` jumps to the most the materials allow and `+` greys out there; Shift + `+` goes 1, 10, 20; Ctrl + `-`
    is 1. Click the number, type 12, Enter: 12 (or the most the materials allow); type 0: 1; Escape: the old amount;
    after Enter the Stow hotkeys (Q, R, T) work at once. No tooltip over -, the number or +. Select another
    recipe: 1. Craft the whole batch: the field drops by itself to what is left (or 1). The Upgrade and Salvage
    tabs show the full-width button and no stepper; so does the bar while it fills. Shift +
    Craft at 1 makes one torch in about 2 s. Wood arrows at 5: the name reads `x100` and 100 arrows arrive. Five
    swords: five separate swords. A full inventory: `+` stops at what fits.
51. Batch with Reach: wood only in a chest in range: the stepper goes as high as the chest allows, Craft takes the
    wood from the chest; LeftAlt + Craft at 4 pulls exactly 4 crafts' materials into the inventory first. Reach off
    (LeftAlt + R): the limit is the inventory alone.
52. Batch with GrindstoneSkills at a cauldron: 3 of a dish give 3 dishes, each rolling stars, and Cooking gains 3
    crafts' experience. `10. Batch Crafting / Enabled = false`: no stepper, Shift + Craft makes 5 again. Gamepad: in
    the crafting panel the D-pad left and right step the amount, held they repeat. Dedicated server with A: the
    server's `Max Amount = 3` stops `+` at 3 on A; Lock Configuration keeps A from changing it.
53. Overflow, single player: chest A 3 m away and chest B 8 m away both hold wood, A has room for only 10 more (fill
    the rest with stone), chest C holds no wood. With 50 wood in the inventory and no chest open, Ctrl + click the
    wood: 10 go to A, 40 to B, message `Sent Wood to Chest and 1 more`, C gets none. Empty B's room too: the wood
    that fits nowhere stays in the inventory. V on wood with no chest open and A full: one wood lands in B, `Stored
    one Wood`; open A (full) and press V: the wood goes to B as well. A and B full: `Nothing to move`. Quick stack (Q)
    and Dump (Alt + D) with A full: the rest lands in B (as before). A chest holding wood beyond `Nearby Range`, one in
    a stranger's ward and one with `refuse: [Wood]` in `OpenKeep.Stow.yml` get none; a favourite wood stack is not
    routed. Ctrl + click copper ore when only a chest of tin (same group) is near: it goes there; with that chest full
    it stays (no other group chest is tried). Open chest C (no wood) while A holds wood: Ctrl + click wood goes to A.
    Open C with no chest holding the item (a stack of resin): Ctrl + click sends it to C, even with a group chest near.
54. Overflow, two clients, `Shared Chests = Full`: A has chest X open (holds wood, room for 10), chest Y near B holds
    wood too. B quick stacks (Q): B's centre message is `Moved n stacks to Chest` when X answers, X gains 10 wood, the
    rest arrives in Y with a top-left `Sent Wood to Chest`, and B's inventory keeps nothing either chest took (log:
    B's request to X, then no warning). B Ctrl + clicks a wood stack with X full: `No room in the chest`, then `Sent
    Wood to Chest` as Y takes it. `Shared Chests = Off`: X is skipped and everything goes to Y at once.
55. Auto Repair, single player: wear down an axe, a bronze sword (a forge recipe) and the armour you wear, then open a
    workbench (E): the axe and the armour made at the workbench are full again at once, one repair sound, the centre
    message `Repaired 2 items` (or the count), the Crafting skill rises as with the button; the sword stays worn. Open
    a forge: `Repaired 1 item`. An item whose recipe needs workbench level 3 stays worn at a level 1 workbench (the
    game's button refuses it too); add extensions and open again: repaired. Nothing worn: no message, no sound. A
    workbench without a roof does not open and repairs nothing. The Upgrade tab's durability bars show full right
    after opening. `Cart Workbench = On`: Shift + E on a cart repairs workbench items. With GrindstoneSkills, Shift + E
    on a cauldron (its trash filter) repairs nothing. `Auto Repair = false`: opening repairs nothing and the game's
    repair button works as before. The log shows `OpenKeep: opening Workbench repaired n items` once per opening.
56. Auto Repair, dedicated server with A and B: each repairs their own worn gear on opening a station, on their own
    screen only; the server log shows nothing. Server `Auto Repair = false` with A's own cfg true: A's opening repairs
    nothing. Lock Configuration keeps B from changing it.
57. Closest first, ground pickup: `Ground Pickup = true`, `Pickup Range = 4`, two chests with `pickup: true` 3 m apart,
    chest A full of stone, chest B holding one stone. Drop 10 stone next to A: after `Pickup Delay` they go into B.
    Empty some of A: stone dropped next to A goes into A, stone dropped next to B into B. A chest C with no stone nearer
    to the drop than B: the stone still goes to B (holders first). A kiln with its output point between two pickup
    chests holding coal: the coal goes into the nearer one, and into the other once that one is full. Dedicated server
    with the chests owned by different players: the same.
58. Bed choice, single player, beds A (near) and B (a few hundred metres away): `die` near A. The map opens centred
    on the death point, zoomed out to show both, both bed icons yellow, twice the size and pulsing, the countdown in
    the map's upper left corner counting
    down from 30. Wait: at 0 you wake in A. Die again and click B: the map closes, you wake in B after B's share of the wait (log
    `chose the bed at ...`, `waking in ... s after death`). Die and press M (and once Escape): the map closes at once,
    you wake in A, the game menu does not open, and the cursor hides again. Die, double click on B: the pin name field
    opens there and you do not wake; name the pin, right click it on B: the pin goes, B stays; click B: you wake in B.
    Hide the bed icons in the map filter first: they show during the choice and are hidden again after. After the
    choice, open the map: the bed icons are back to the normal size and still. With a portal, a map pin and the death
    marker beside a bed (and Wayfare's portal icons shown): the bed icon is drawn over all of them during the choice,
    and a click on it wakes you there, not at the portal; after the choice the icons stack as the game draws them.
59. Bed choice, edge cases: one bed only, no map (wake at once with Quick Respawn); `Bed Choice Seconds = 0`: no map;
    a `nomap` world: no map; log out during the choice and back in: you are at the nearest bed; destroy B while
    choosing it (second client): the log says `no bed of yours at ...; trying the bed at ...` and you wake in A.
60. Quick Respawn: `die` right beside a bed: awake in about a second. 500 m away with the defaults: about 9.5 s. 2 km
    away: the game's 18 s. No bed: the same by the distance to the start stones. `Quick Respawn = false`: 18 s again,
    with the bed choice too (the choice then only picks the bed).
61. Beds On Map: all own beds show with the bed icon in yellow on the minimap and the large map, the spawn bed once (the game's icon, no second one on top);
    sleeping in another bed swaps them within a second; a destroyed bed's icon goes. `Beds On Map = false`: only the
    game's icon. Dedicated server with A and B: each sees only their own beds.
62. (Quick Portals moved to Wayfare, 2026-10-04: its PLAN.md checklist.) With OpenKeep alone, two portals 20 m
    apart take the game's 8 s again and the log has no `portal jump` line.
63. (Portal screen moved to Wayfare.) With OpenKeep alone every jump shows the game's teleport screen.
64. Stand Up On Respawn: `die` beside a bed: you appear standing and can walk at once; log out and in: the game's
    getting-up animation. Dedicated server, B watching A die and respawn: B sees A standing, not lying.
65. Quick Area Loading (world with simulation distance 4), now the respawn only: dying far from a bed, the respawn
    waits only for the timer, not ~6 s of land loading (log `OpenKeep: loaded the land around you in ...`).
    `Quick Area Loading = false`: the game's pace. A portal jump with OpenKeep alone: the game's pace (Wayfare's now).
66. Server settle, dedicated server with a big base 1 km from where you die: dying far from your bed wakes you in
    that bed, not the next one (log has no `no bed of yours at`).
67. Epic Loot link, with Epic Loot installed: the log says `Epic Loot's enchanting table pays materials from nearby
    containers`. Put the dust and runestones an enchant needs in a chest beside the enchanting table, none in the
    inventory: the enchant tab shows them as available, enchanting takes them from the chest. Half in the inventory:
    the inventory pays first. Gear in the chest never shows in the enchant or sacrifice lists. `Crafting = false`:
    chest materials no longer count. Dedicated server, chest open by another player: it is skipped. Without Epic
    Loot: no log line, no warning.
68. Build Camera: hammer in hand by a workbench, B: the camera detaches where it was; WASD pans flat, mouse turns it,
    Space up, Ctrl down, Shift faster; the body stands still (not walking, jumping or crouching). Place a wall 10 m
    from the camera and 25 m from the player: it builds, the wood leaves the inventory; middle click removes it, the
    resources fly to the camera and into the inventory; repair works. Fly down: the camera stops on the ground and
    slides along it; it passes through a wall into the house. Fly away: it stops at the workbench's 20 m edge
    (`Range Multiplier = 2`: 40 m; a piece placed beyond 20 m still says it needs the workbench). B or R brings it
    back; so does unequipping the hammer. Away from any station B says "The build camera works only near a crafting
    station". Hoe and cultivator: the same. `Entry Min Comfort = 5` in an empty hut: refused with "comfort 5 (you
    have 1)"; `Pickup Needs Resting` outside: removing a piece shows the panel at the top, in the build menu's look,
    and the items stay. Dvergr circlet on: the camera carries its light; `Circlet Intensity = 3` brightens only the
    copy. Gamepad: hold LT, click R3; sticks pan and turn, triggers up and down. Torches by the camera 50 m from the
    player stay lit. Dedicated server, B watching A: A stands still while pieces appear; A's pickups land in A's
    inventory.
69. Recipe search: open a workbench. A search row sits on top of the recipe list, in the build menu's field look,
    with a star and a view button at its end; the list starts below it. Type `club`: only clubs stay, 0.1 s after the
    last key, scrolled to the top. `@resin`: only recipes needing resin (torch, ...). `@wood -@stone`: wood but no
    stone. A Polish or German game language: the local name and `SwordIron` both find the sword. Escape: the text goes,
    the inventory stays open; Escape again closes it. While typing, E and Tab type letters (the inventory stays), and
    OpenKeep's Q/G/T hotkeys do nothing. Enter keeps the text; reopening the inventory clears it (`Clear Search On
    Close = false`: it stays). Ctrl + F puts the cursor in. Upgrade tab: the search filters the upgradable items too.
    Salvage tab: no row, the list is the game's height. `Search = false`: no row. Steam Deck / Big Picture: clicking
    the field opens Steam's keyboard and the list follows what was typed.
70. Favourite recipes: middle click a recipe: a small orange star on its icon, centre message, it moves to the top
    (`Favourites First = false`: it stays in place). F over a recipe row toggles it; F over an inventory slot still
    favourites the item. The star button under the recipe's name toggles it and shows orange while a favourite. The
    star above the list: only favourites (message), again: all. With none: refused with a hint. Shift + click that
    star: the game's yes/no popup, yes clears them all. Log out and in: favourites are still there; another character
    has its own.
71. Views: the view button cycles List, Compact list, Small grid (5 a row), Medium grid (4), Large grid (3), Shift +
    click goes back; the cfg's `Recipe View` follows. In a grid: icons fill the tiles, uncraftable ones greyed, the
    selected tile orange, quality levels small top left on the Upgrade tab, worn items show a durability bar; hover a
    tile: the game's tooltip with the name (arrows: x20), just below and right of the pointer, with Epic Loot
    installed too (also the search row's and buttons' tips). Scrolling works; the selected recipe stays selected
    across a view change. Compact list: lower rows, icons and texts still fit.
72. Gamepad in the crafting panel: right stick up opens the search (Big Picture: Steam's keyboard), down tracks the
    selected recipe, left favourites it, right switches favourites only; holding a direction acts once. In a grid:
    D-pad up and down move a row, left stick left and right a tile; D-pad left and right still change the batch
    amount. Holding D-pad right: +1 steps, then tens after about a second.
73. Batch extras: the mouse wheel over -, the amount or + changes it by one (Shift: tens, Ctrl: 1 or the most).
    `Craft Speed = 2`: a single craft's bar fills in half the time, a batch too, and an upgrade; `0.5`: twice as long.
    Dedicated server with `Craft Speed = 3` and Lock Configuration on: a client's own value is ignored, the server's
    applies at once on a cfg edit (hot reload).
74. Tracker: right click a recipe you cannot make yet: "Tracking ...", a narrow panel at the left edge shows its icon, name,
    the station and level it needs, and one row per material with have/need (red while short, white once enough; the
    name turns orange when all are there). Set the stepper to 5 first and track the selected recipe: x5 and every
    need times 5. Pick up materials: the counts follow within half a second. Stand by a chest with Reach on: its
    materials count too (`Count Nearby Chests = false`: only the inventory). The Upgrade tab tracks the next level
    ("..., level 3") with the upgrade's materials.
75. Tracker editing, inventory open: each header shows an X at its left and - x1 + at its right, the panel wider;
    closed, only icon, name (xN when more than one) and rows. - and + change the amount (Shift: tens), the mouse wheel
    over a header too; X removes it. Drag the "Tracked recipes" title: it moves, stays on screen, and is there again after a restart
    (`Position` written). Clicking a material row clicks what is under it (an inventory slot). Inventory closed:
    attacking never clicks the tracker. More than `Max Tracked`: refused with a message.
76. Tracker visibility: open the large map: it hides (`Hide With Map = false`: stays). A greyling attacks: it hides
    and comes back 5 s after the fight. Hide the HUD (the game's key): it goes with it. Dead: hidden.
77. Untrack When Crafted: track a recipe x3, craft 2 (stepper 2): x1 left; craft 1 more: it leaves with "... made,
    off the tracker". A refused craft (no room) changes nothing. `Untrack When Crafted = false`: it stays.
78. Tracker look: `Scale = 1.5`, `Font = Norse`, `Font Size = 20`, the colours and `Background Opacity = 0` apply
    within half a second of saving the cfg; the panel widens with the font and stays as narrow as its rows.
79. Auto Tidy on (`Auto Tidy = true`), a test world: a wood chest (15 stacks) and a stone chest a few metres apart, a
    junk chest with ten kinds including wood and stone. Within 30 s the wood and stone leave the junk chest for their
    chests (8 stacks per look, the rest 3 s later); `openkeep tidy` shows the junk chest `JUNK`, the wood chest
    `Wood 100%`, the looks and their times (the slowest well under 1 ms in a small base). Put one stack of stone into
    the wood chest and close it: within about 5 s it is in the stone chest. Take it back and put it into the wood chest
    again: it stays (`kept: Stone`). Empty the wood chest, put wood in the junk chest: it goes to the empty wood chest.
    A chest of six kinds of bar is not junk (the `smelted on Coal` family). A chest left alone is not looked at again
    (`Looks` stays put). Two clients: A owns the junk chest, B the wood chest (B opened it last): B's log shows `tidy
    handed piece_chest_wood over to peer <A>` and the wood moves within seconds.
80. Quick World Save, a world with a large base: save with the console `save` (or wait for the autosave). The log shows
    the game's `GetSaveClonePerChunk ... [N ms]` and `ZDOExtraData.PrepareSave done [N ms]` lower than with `Quick
    World Save = false`, and the first number of "World saved (X+Y s)" smaller. Change a chest, save, quit, load: the
    chest is as left; build a piece and destroy another, save, reload: both stay that way; a portal pair still links.
    No `quick world save fell back` warning. Dedicated server: the same on the server's log; clients log nothing.

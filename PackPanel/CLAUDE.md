# CLAUDE.md - PackPanel

The player's own inventory for Valheim, version 0.1.0: a bigger grid, labelled slots for worn gear, a backpack slot (and
PackPanel's eight backpacks, one per biome, worn on the back, adding slots and carry weight), up to five worn utilities, a trinket,
food, mead and ammo, a coin purse, a key ring, a tacklebox slot (and four crafted boxes whose cells hold bait), the stat
boxes (in their own panel and beside the minimap) and the brown or timber look. Built on 2026-09-28 as OpenKeep's section
`10. Inventory` (`src/Pack/`, never released, never committed there) and moved into its own mod the same day at the
user's request: they were told the UI and inventory work had to be separate from the storage mod. OpenKeep keeps
storage; the two work alone and fit together when both are installed (below). This file is the code map, the patched
methods, the names, the decisions and the in-game test checklist.

Rules that apply to every change:

- Clean room as in `../CLAUDE.md`. Own names everywhere: GUID `milkyteam.packpanel`, config `milkyteam.packpanel.cfg`,
  YAML `PackPanel.Backpacks.yml` and `PackPanel.Tackleboxes.yml`, words `$packpanel_*`, custom data and ZDO keys
  `PackPanel.<name>`, prefabs and GameObjects `PackPanel_<Name>`, Charter articles `packpanel_backpacks` and
  `packpanel_tackleboxes`, the model bundles `packpanel_backpacks` and `packpanel_tackleboxes` with the ValheimAssets
  assets `packpanel_<pack>` and `packpanel_<box>_tacklebox`.
- Verify every patched signature against a fresh `ilspycmd` decompile of the game's own assemblies in the scratchpad,
  never in a repository, and read the method body so the patch fits ownership, RPCs and save paths.
- Functions at most 24 lines from brace to brace (lambdas and local functions count), classes at most 300 lines, one
  responsibility per class. Prefix, postfix and finalizer patches only; no transpilers. Every patch class is applied
  on its own in `Plugin.PatchEverything`, so one failure is logged and the others still apply.
- Settings are read at use time (`ConfigEntry.Value`). Every gameplay setting is bound synced and covered by
  `Lock Configuration`; the look (`5. Look`) is bound with `synced: false`.
- Nullable is off. Zero compiler warnings. Build from the workspace root with
  `"/c/Program Files/dotnet/dotnet" build PackPanel/PackPanel/PackPanel.csproj -c Release`; the build copies the merged
  DLL to `dist/` and to the r2modman profile `LocalTesting`. Never launch or kill the game from a script.

## With OpenKeep

Neither mod references the other, needs the other or reflects on the other. Each detects the other by plugin GUID in
BepInEx's chainloader, on first use, cached (`Core/OpenKeepLink`: OpenKeep 1.8.0 or later; older OpenKeep knows
nothing of PackPanel and counts as absent). What they share is data, documented on both sides:

- The main grid: PackPanel writes the character's custom data key `PackPanel.mainGrid` = `width|rows|blocked`
  (`Layout/GridContract`) with every layout applied while it is on, and removes it when switched off. OpenKeep
  (`Core/PackPanelGrid`) trusts it only while PackPanel is loaded and its `1. Inventory / Enabled` is on, and uses it for
  quick stack, store all, dump, sort (the rows and a backpack's blocked cells) and a shared chest's stack all (items
  below the main rows are in slots). Elite Creatures Reborn reads the same key through the shared library
  `ValheimModLibs/PlayerGrid` (the same reader and a `GridContract` writer, which this mod and OpenKeep are to move
  onto): its Thieving mutation never takes from below the main rows, and takes nothing while PackPanel is on and the
  key cannot be read. Keep every slot kind below the main rows and the format as it is, or change all three together.
- The button strip: while PackPanel lays the player panel out and OpenKeep is present, the panel is 30 units taller
  and an empty `PackPanel_buttonstrip` (a child of `InventoryGui.m_player`, stretched along its bottom) is active
  (`Panels/ButtonStrip`). OpenKeep's `PanelButtons.Follow` watches it every frame and puts its row inside (2 units
  above the edge) or back below the panel. OpenKeep decides at `InventoryGui.Awake` whether its trash can is a box in
  PlateColumn's column or a button in the row, from PackPanel's `Enabled` read through its config.
- Key Stack: one mod writes the keys' stack size. With OpenKeep present, OpenKeep's `Stacks/PackPanelKeys` reads
  `3. Key Ring / Key Items` and `Key Stack` from PackPanel's config file (`Chainloader.PluginInfos[guid].Instance.Config`,
  the server's values while connected) and applies them as the keys' starting value, so its YAML still wins, and
  re-applies when either changes. Without OpenKeep, `Ring/KeyStacks` writes them itself through ItemCopies.
- PlateColumn is a library both merge; its copies cooperate through GameObject names (`PlateColumn_boxes`), as with
  Elite Creatures Reborn's world tier box. PackPanel moves the column into its stats panel; OpenKeep alone keeps its
  trash plate in the column.

## With BiomeLords

BiomeLords' Featherweight blessing (Faller Valkyrie lord, setting `FallerValkyrieExtraRows`, default 2) adds inventory
rows while active; it has no duration, survives death (BiomeLords re-adds it at spawn) and ends only when the player
switches blessing. BiomeLords sizes the inventory itself (`FeatherweightInventory.SetHeight`, writing `m_height`) to
max(4, `invrows`) plus its rows at every spawn, grant, switch and before `DropInvalidItems`, and moves everything below
into CargoCrates; it only stands back for ExtraSlots and AzuExtendedPlayerInventory. Found in game 2026-10-03: every
PackPanel slot item landed in crates at login. `Layout/BiomeLordsLink` + `BiomeLordsPatches` (by reflection, patched
only with BiomeLords present, soft dependency so it loads first): while PackPanel lays out the inventory, `SetHeight`
becomes `LayoutApply.Apply`, the blessing's rows are main rows (`LayoutBuilder` adds them after Inventory Rows and
bought rows, before a backpack's cells), and when the blessing goes their items move to free main cells, the rest into
BiomeLords' own crate (`SpillToCrate`). `EnsureExpanded` and `FindExtraRowSlot` (free cells under its base: PackPanel's
slots) do nothing. At switch time BiomeLords still has the buff on, so the requested height decides: at or under its
base takes the rows away until the next grant.

## Layout

```
PackPanel/PackPanel/src/
  Plugin.cs                 entry: Lock Configuration, InventoryModule.Initialize, patches per class, the backpacks'
                            ZNetScene hook, Synced.Finish, Guard.Install; Update runs the YAML editor and the theme
  Core/
    InventoryModule.cs      binds everything, the layout keys' change handler (applied once, next frame, in
                            PlayerTick's Player.Update postfix)
    InventorySettings.cs    sections 1. Inventory, 2. Slots, 5. Look (the key ring, backpacks and tacklebox bind 3., 4.
                            and 6.)
    InventoryState.cs       the local player and its layout; Manages(inventory), SlotAt, CellsOf, ItemIn, BlockedCells
    Words.cs                the $packpanel_ words: slot captions, the key ring's words, the messages
    PlayerTick.cs           Player.Update postfix: the waiting layout change, the worn backpack's slots, the
                            tacklebox's cells, pruning extra utilities
    OpenKeepLink.cs         OpenKeep 1.8.0 or later present (GUID in the chainloader)
    EpicLootLink.cs         Epic Loot's active effects, totals and display texts through its public API, by reflection
    Language.cs, Messages.cs, ItemNames.cs   words to the game's localization, HUD messages, prefab names (read
                            once per drop prefab)
    EditorHost.cs           the YAML editor's window drawn by a component of its own, enabled only while the window is
                            open, so the plugin has no OnGUI
  Layout/
    InventoryLayout.cs      width, main rows (a worn pack's included), backpack slots and blocked cells, the ordered
                            slots; slot k is cell (k % width, mainRows + k / width); the record 1|W|R|ids
    LayoutBuilder.cs        the wanted layout from the settings and invrows (the tacklebox slot and its cells last);
                            GameLayout = 8 x invrows, no slots
    LayoutRecord.cs         Player.m_customData["PackPanel.inventoryLayout"]
    GridContract.cs         Player.m_customData["PackPanel.mainGrid"] for OpenKeep
    BiomeLordsLink.cs, BiomeLordsPatches.cs   Featherweight's rows as main rows; BiomeLords never resizes (see With BiomeLords)
    LayoutMigration.cs      where every item goes: slots by id, main cells kept, keys and bait into ring and box cells
                            a layout adds, the rest to free ring, box, then main cells, overflow
    LayoutApply.cs          BeforeLoad (width 32, provisional state), AfterLoad, Apply: move, size, record, contract,
                            drop, settle worn items, panel size
    LayoutPatches.cs        Player.Load prefix/postfix/finalizer; Player.SetInventorySize replaced
    MainCells.cs, FreeCellPatches.cs   FindEmptySlot, GetEmptySlots, HaveEmptySlot, CanAddItem over the main grid (plus
                            an empty purse, ring cell or box cell for what goes there);
                            GetBoundItems and GetHotbar stop at x 8
    CarryWeight.cs          Base Carry Weight and the worn pack's carry: Player.GetMaxCarryWeight postfix
  Consume/                  the Food Key and the Mead Key (section 2. Slots, per player) and the Food and Mead bar
                            (5. Look, per player)
    ConsumeSettings.cs, ConsumeWords.cs   the two keys (Z, B; the YAML editor registered with the Hotkeys library's
                            Typing), the "nothing to eat / drink" words
    ConsumeKeys.cs          PlayerTick: a press outside the inventory (Player.TakeInput) and build mode
                            (Player.InPlaceMode) eats or drinks from its slots
    ConsumeBar.cs, ConsumeBarCell.cs   Hud.Update postfix: PackPanel_consumebar under the health panel, a food square
                            and a mead square (copies of the HUD's food square) with each key (Hotkeys'
                            KeyNames.Short) over its top-left corner
    SlotMeals.cs            left to right, every item that can be taken now: the game's checks without messages,
                            then Humanoid.UseItem(inventory, item, fromInventoryGui: true)
  Slots/
    SlotKind.cs, Slot.cs, SlotRules.cs   the slot kinds (Head..Back, Utility worn; Backpack, Food, Mead, Ammo, Purse
                            carried; Retired for an old record's quick slots; Key, the ring cells; Tacklebox and
                            Tackle, the box and its cells; Trinket, worn, last), ids like food2, which item each takes (a ring cell:
                            its own key, Accepts(Slot, item))
    SlotCounts.cs           every group's count (Slots Per Group over the group keys)
    AmmoSearch.cs           Inventory.GetAmmoItem prefix: the tacklebox's bait, then the Ammo slots left to right,
                            then the game's own search
    FoodCount.cs            foods a player can eat: FeastMaster's Food Slots entry through BepInEx's Chainloader, else 3
    CoinPurse.cs            Inventory.AddItem(ItemData) prefix: coins into the purse first
    AmmoRouting.cs          Inventory.AddItem(ItemData) prefix: arrows and bolts (equipable ammo) into the Ammo slots
                            first, crafted or picked up
    SlotFill.cs             an item into a group of slot cells: onto a same stack, then an empty cell (ammo, bait)
    SlotDrop.cs, SlotAddPatches.cs   the slot rules on InventoryGrid.DropItem and the positional adds
    DragSettle.cs           InventoryGui.OnSelectedItem: refuse a bad drop first, settle worn slots after; stands down
                            when an earlier prefix already refused the click (__runOriginal)
    TakeAllRouting.cs       Inventory.MoveAll prefix/postfix: keys, bait and arrows a take all put in main cells move to
                            their ring cell, into the tacklebox and into the Ammo slots
    StackAllGuard.cs        the game's Place stacks (Inventory.StackAll) leaves the slot cells alone
    KeptOnDeath.cs          Keep Slots On Death: slot items out of the inventory while the tombstone is made
    GravePatches.cs, GraveWidth.cs   CreateTombStone suspends worn moves; MoveAll from the own grave re-equips, from
                            anything else keeps out of the slots; a grave loads as wide as its items
    GraveFit.cs             TombStone.EasyFitInInventory for the own grave: Use takes all when the take all would fit
                            (the waiting pack's slots and carry, slot items back in their own slots)
  Worn/
    WornPlacement.cs        worn item into its slot on equip (swap with the old piece), out on unequip; suspended in
                            drags, loads and the tombstone
    ExtraUtilities.cs, ExtraEffects.cs   the worn utilities beyond the game's one and their status effects
    EquipPatches.cs         EquipItem, UnequipItem, IsItemEquiped, IsItemTypeEquiped, UnequipAllItems, UnequipDeathDropItems
    UtilityEffectPatches.cs UpdateEquipmentStatusEffects, GetSetCount, GetEquipmentEitrRegenModifier, UpdateModifiers,
                            GetEquipmentWeight, UpdateEquipment count the extra utilities
  Ring/
    KeyRing.cs, KeyRingSettings.cs   Key Items parsed (ring number = list place), a key's ring cell, counts per key by
                            cached shared name; section 3. Key Ring
    KeyRouting.cs           Inventory.AddItem(ItemData) prefix: keys into their ring cell first (a take all:
                            Slots/TakeAllRouting)
    KeyStacks.cs            Key Stack into the keys' stack size (ObjectDB.Awake/CopyOtherDB postfixes, settings
                            changes), only without OpenKeep
    KeyRingButton.cs        PackPanel_keyring_button: a copy of the grid's element prefab on the purse's row; icon,
                            caption, the number of different keys held, the list as its tooltip, gamepad highlight
    KeyRingPopup.cs, KeyRingCircle.cs, KeyRingCells.cs   PackPanel_keyring: the small round pop-up under the slot
                            panel, the circle's geometry, the ring cells' elements (reparented, scaled), counts
    KeyRingState.cs, KeyRingClicks.cs   open or shut (InventoryGui.Show/Hide close it); the button's click, a dragged
                            key sent to its own cell (OnSelectedItem prefix, Priority.First), A/X on the shut ring
    KeyRingNews.cs, KeyRingNotice.cs   keys never carried before (PackPanel.keysSeen, PlayerTick); the button's gold
                            breathing and the PackPanel_keynotice box under it until the ring is opened
    KeyRingGamepad.cs       InventoryGrid.UpdateGamepad: moves skip hidden cells (the shut tacklebox's cells too), the
                            shut ring stands in as its first cell; InventoryGui.Update prefix: B shuts either pop-up
                            first
  Backpacks/
    BackpackKind.cs, BackpackStats.cs, BackpackCatalog.cs   the eight backpacks (CraftedKind, CraftStats): default
                            and current stats (slots, carry, portal), the worn model, the ZDO hash; lookups by shared
                            name, id and ZDO hash
    BackpacksModel.cs, BackpacksFile.cs   PackPanel.Backpacks*.yml (article packpanel_backpacks): per pack overrides;
                            applying sets the stats, the recipes and the words
    Backpack.cs, BackpackSettings.cs, BackpackWords.cs   the worn pack (Backpack slot of a layout; the local player's;
                            the slots for the next layout from the recorded one), the ZDO key; section 4. Backpacks;
                            names, descriptions (what the pack gives, rewritten on change), message
    BackpackPrefab.cs       ZNetScene.Awake (BundlePrefabs.NetPrefabs): each model of the bundle, the item
                            (Crafting/CraftedItem) and the worn copy; registered in ZNetScene and ObjectDB (ItemPrefabs)
    BackpackWear.cs         the local player's frame: re-layout when the worn pack or its slots changed, the ZDO key
    BackpackEquip.cs        a pack is equipment (a Utility item): EquipItem wears it in the Backpack slot, never as the
                            game's utility; the pack in the slot carries m_equipped, every frame (Sync)
    BackpackUse.cs          Humanoid.UseItem prefix: right click wears a pack (EquipItem) or takes it off
    BackpackGrave.cs        before a take all from a grave the worn pack (found at the grave's Backpack slot cell) goes
                            on first; the rows a waiting pack adds (RowsAdded: the layout's own rule)
    BackpackMount.cs        VisEquipment.UpdateEquipmentVisuals postfix: hangs, swaps or takes down the model on Spine2
    CapeHold.cs             on the mount: the worn cape's MagicaCloth gets a max distance that holds its top under the
                            pack; the cape's own settings back when the pack comes down or the cape changes
    BackpackPortal.cs       Inventory.IsTeleportable prefix: Backpack Portal Pass for the worn pack's slots
  Crafting/                 what the backpacks and the tackleboxes share
    CraftedKind.cs, CraftStats.cs   a crafted item: prefab id, group and word (its words, icon, bundle asset), English
                            name and look, weight, the item; station, level and cost
    CraftRecipes.cs, CostText.cs   ObjectDB.Awake and CopyOtherDB postfixes: one Recipe per crafted item in every
                            database, refreshed on every change; the cost text as requirements (own items from their
                            catalogs, so a recipe that takes the one before holds)
    CraftedItem.cs, CraftedModels.cs   the item as a copy of TrollHide (model, collider, LOD, shared data, icon); a
                            model from an embedded bundle dressed in the troll hide cape's material
    RecipeYaml.cs           station, level, cost and range checks for the item files
    InPlaceUpgrade.cs       a recipe that takes the pack in the Backpack slot (the box in the Tacklebox slot) crafts the
                            new one into that slot: the old one set aside for the game's craft, the slot its free cell
  Tackle/
    TackleboxKind.cs, TackleboxStats.cs, TackleboxCatalog.cs   the four boxes: default and current stats (cells);
                            lookups by shared name and id
    TackleboxesModel.cs, TackleboxesFile.cs   PackPanel.Tackleboxes*.yml (article packpanel_tackleboxes): per box
                            overrides; applying sets the stats, the recipes and the words
    Tacklebox.cs, TackleboxSettings.cs, TackleboxWords.cs   the box in the Tacklebox slot of a layout, the cells for the
                            next layout from the recorded one; section 6. Tacklebox; names, descriptions, captions,
                            message
    TackleboxPrefab.cs      ZNetScene.Awake: each box's item from the bundle packpanel_tackleboxes
    TackleboxWear.cs        the local player's frame: re-layout when the box or its cells changed
    TackleRules.cs          what a box takes: bait (ammo of the FishingRod's ammo type) and Tackle Items
    TackleRouting.cs        Inventory.AddItem(ItemData) prefix: bait into the box first (stacks, then an empty cell)
    TackleboxUse.cs         Humanoid.UseItem prefix: right click a box into its slot, on its slot opens the pop-up;
                            right click a bait in the box equips it (the game's ammo), again unequips
    TackleClicks.cs         InventoryGui.OnSelectedItem prefix (Priority.First): bait let go on the slot goes into the
                            box
    TackleState.cs, TackleGamepad.cs   open or shut (Show/Hide close it; opening shuts the key ring's); the gamepad's
                            selection follows
    TacklePopup.cs, TackleCells.cs   PackPanel_tacklebox: the pop-up under the slot panel, its cells (reparented,
                            scaled 0.8, rows of 4, captions, counts)
    TackleboxSlot.cs        the slot's corner: cells used of cells
    TackleboxGrave.cs       before a take all from a grave the box that was carried goes back into its slot first
  Panels/
    PanelSize.cs            InventoryGui.SetInventorySize replaced (width, main rows, frame pad, OpenKeep's strip);
                            UpdateContainer postfix widens the container panel for inventories wider than 8
    CraftingPanel.cs, CraftingParts.cs, CraftingPanelPatch.cs, RectMemo.cs   the larger crafting panel: InventoryGui.Show
                            prefix sets the panel, list, row template, description, name and text from the game's
                            remembered values plus Crafting Panel Width/Height, clamped to the free screen; the
                            name/skills/trophies/PvP panel above (m_infoPanel) gets the same width
    (Look) ArtWarmup.cs     FejdStartup.Start postfix: SkinArt.Prewarm decodes every panel image at the main menu
                            (the 1254 px wallpaper and 1983 x 793 button stalled the first inventory open)
    ButtonStrip.cs          PackPanel_buttonstrip, the mark OpenKeep's button row follows
    PanelDress.cs           where the side panels go, and the library's box column moved into the stats panel (every frame)
    StatsPanel.cs           PackPanel_stats between the inventory and the slot panel, behind the box column
    StatIconFrames.cs       vanilla gray backplates for armor, weight and world-tier icons; original strip layout
    StatIconLayout.cs       48-unit squares, centered icons and inset 8-11 point number overlays; restores on disable
    WeightDisplay.cs        current weight / dash / capacity on three lines in the inventory and HUD; overload still flashes
    HudStats.cs             matching full-size squares in a column right of the minimap; armor, weight, world tier by rank
    HudRoom.cs              moves the minimap and the status effects left while that column shows a box
    HudWeight.cs            Weight Under Minimap: Hud.Update postfix writing a box in PlateColumn's HUD column
    SlotPanel.cs, SlotPanelLayout.cs   PackPanel_slots right of the stats panel: the tab buttons across the top, the
                            shown tab's cells (Gear: gear column left, utilities and the trinket right; Consumables: a row each of
                            food, mead, ammo), the purse, the key ring's button and the Tacklebox slot last under both
    SlotTab.cs, SlotTabs.cs the tabs; the shown one (session only), which kind goes where, the gamepad turning tabs
    TabButtons.cs           the two tab buttons, copies of the game's take-all button, stripped of its gamepad key,
                            colour scripts and localizer
    GearStats.cs, SheetScroll.cs   PackPanel_gearstats: the Gear tab's stat sheet between the columns, a rounded inset
                            with a wheel-scrolled list, worked out 4 times a second
    StatRows.cs, StatSheet.cs   the sheet drawn as rows (label wraps, value right, headings), restyled before every
                            write; the sheet as lines with sections and a change key
    SheetLines.cs, GearStatLines.cs, EffectGroups.cs   everything it lists; the game's numbers (its getters and words);
                            Epic Loot's effects grouped by words in their type names
    StatTips.cs, GearTips.cs, TipText.cs, StatTipWords.cs   each line's breakdown for its hover tooltip: health,
                            stamina, eitr, armour, carry weight; movement, durability, resistances, modifiers
    HeldStatLines.cs, HeldItems.cs   a section per weapon or shield in hand (sheathed ones too): damage, attack costs,
                            knockback, backstab; the blocker's block armour, block force, parry, block modifiers
    HeldDamage.cs, HeldTips.cs   a hit's damage range per type (weapon + ammo, attack, effects, skill roll); the costs,
                            block and parry worked out as the game does, and their breakdowns
    BlockedCell.cs          a worn pack's unopened cells: dimmed, a grey cross, deaf to the pointer
    SlotElements.cs, ElementPlacer.cs   InventoryGrid.UpdateGui postfix: slot elements into the panel, unused cells
                            hidden, hotbar numbers stop at 8, root kept to the main rows, skins, the purse's count
    HoveredCell.cs          InventoryGrid.GetHoveredElement postfix: a hidden cell is never the hovered one
    SlotLabels.cs, SlotIcons.cs   the captions (PackPanel_hint), shown while a slot is empty; the icon each shows
    Divider.cs              the bronze line over the purse's row
    ContainerDrop.cs        the container panel lowered to keep the gap
    PopupPlace.cs           where a pop-up under the slot panel hangs (the key ring's, the tacklebox's)
    BackpackPanelHeight.cs  the side panels keep the height they have without a worn pack's rows
  Look/
    RoundedFill.cs, CrossMark.cs   sprites painted in code: a rounded rectangle (the stat sheet), a cross (blocked cells)
    SkinArt.cs              the embedded pictures as sprites (panel 9-sliced, button states, icons, timber)
    GridSkin.cs             Brown Style panels (default material, 12 unit reach) and cells
    NightShade.cs           the panels darker with the light around you (Night Shade), grey only; never the cells
    GamePanelTheme.cs       Panel Theme over the game's wood panels (a rescan per scene, Image.OnEnable postfix)
    TimberBackground.cs, TimberFrame.cs, TimberCoordinates.cs   the timber wallpaper aligned to the screen and the rim
PackPanel/PackPanel/config/ the embedded default PackPanel.Backpacks.yml and PackPanel.Tackleboxes.yml
PackPanel/PackPanel/assets/ embedded images: panel.png, cell.png, button*.png, icon_<slot>.png, ring_*.png (painted by
                            skin_art.py next to them: `python skin_art.py <out folder>`, then copy the PNGs here; never
                            write preview.png into this folder since every PNG here is embedded), timber_*.png
                            (timber-art.md), backpack_<word>.png and tacklebox_<word>.png (rendered from the models);
                            bundles/ the models, packpanel_backpacks.windows and .linux (ValheimAssets packpanel_*;
                            rebuilt with `../ValheimAssets/build.ps1 -Asset <the eight packpanel_* pack folders> -Bundle
                            packpanel_backpacks`, then both files copied here from ../ValheimAssets/out/bundles) and
                            packpanel_tackleboxes.windows and .linux (the four packpanel_*_tacklebox folders, the same
                            way)
PackPanel/tests/            TimberCoordinates.Check: `dotnet run` checks the wallpaper's screen mapping
PackPanel/artwork/          background studies for the timber theme (not embedded)
```

## Patched game methods

All on the local player's own inventory only unless said: prefix `Player.Load` (and postfix and finalizer),
`Player.SetInventorySize` (replaced), `InventoryGui.SetInventorySize` (replaced), `InventoryGui.Show` (prefix: the crafting panel's size), `Inventory.FindEmptySlot`,
`Inventory.GetEmptySlots`, `Inventory.HaveEmptySlot`, `Inventory.CanAddItem(ItemData, int)`, `Inventory.AddItem(ItemData)`
(the purse, and the key ring's, the tacklebox's and the Ammo slots' own prefixes), `Inventory.AddItem(ItemData, int, int, int, bool)` and
`Inventory.AddItem(ItemData, Vector2i)` (slot rules), `InventoryGrid.DropItem` (`Priority.High`),
`InventoryGui.OnSelectedItem` (prefix, postfix, finalizer), `Humanoid.EquipItem` (prefix, postfix, finalizer; the
prefix also wears PackPanel's backpacks, on any character), `Player.CreateTombStone` (prefix, finalizer), `Inventory.StackAll` (prefix, finalizer: the slot cells' unworn items are
out of the list for the call), `Inventory.MoveAll` (prefix, postfix, finalizer; the key ring has its own prefix and
postfix), `Container.Load` (prefix and postfix, graves only, any player's); the key ring: `InventoryGui.OnSelectedItem`
(a second prefix, `Priority.First`), `InventoryGui.OnRightClickItem` (prefix), `InventoryGrid.UpdateGamepad` (private;
prefix and postfix), `InventoryGui.Update` (private; prefix), postfix `InventoryGui.Show` and `InventoryGui.Hide`;
postfix `Inventory.GetBoundItems`, `Inventory.GetHotbar`, `Humanoid.UnequipItem`, `Humanoid.IsItemEquiped`,
`Humanoid.IsItemTypeEquiped`, `Humanoid.UnequipAllItems`, `Player.UnequipDeathDropItems`,
`Humanoid.UpdateEquipmentStatusEffects`, `Humanoid.GetSetCount`, `Player.GetEquipmentEitrRegenModifier`,
`Player.UpdateModifiers`, `Humanoid.GetEquipmentWeight`, `Humanoid.UpdateEquipment`, `InventoryGrid.UpdateGui`,
`InventoryGui.UpdateContainer`, `Player.Update`, `Player.GetMaxCarryWeight` (Base Carry Weight; any player, only the
local one's matters), `Hud.Update` (private; Weight Under Minimap, and a second postfix for the Food and Mead bar), `InventoryGui.UpdateInventoryWeight` (postfix: the weight box's
text stacked as weight over capacity, `WeightDisplay`), `Localization.SetupLanguage` (the words),
`UnityEngine.UI.Image.OnEnable` (Panel Theme: a newly shown wood panel is themed on the next frame). The backpacks:
prefix `Humanoid.UseItem` (local player, from the inventory screen), `Inventory.IsTeleportable` (the local player's
inventory, Backpack Portal Pass only); postfix
`VisEquipment.UpdateEquipmentVisuals` (private, every player's character on every client), `ObjectDB.Awake` and
`ObjectDB.CopyOtherDB` (the recipes; `Priority.Low` for Key Stack without OpenKeep), and through BundlePrefabs
`ZNetScene.Awake` (the prefabs) and again `ObjectDB.Awake`/`CopyOtherDB` (the items). The tacklebox: prefix
`Inventory.AddItem(ItemData)` (bait into the box, its own prefix), `Inventory.GetAmmoItem` (the box's bait first, then the Ammo slots left to right; `Slots/AmmoSearch`),
`Humanoid.UseItem` (a second prefix: boxes and bait in the box), `InventoryGui.OnSelectedItem` (a third prefix,
`Priority.First`: bait let go on the slot), postfix
`InventoryGui.Show` and `InventoryGui.Hide`; the recipes and prefabs through the same patches as the backpacks'.
Both, upgraded in their slot (`Crafting/InPlaceUpgrade`): prefix and finalizer `InventoryGui.OnCraftPressed` and
`InventoryGui.DoCrafting` (both private), read by the `Inventory.FindEmptySlot` and `Inventory.CanAddItem` prefixes.
`Inventory.MoveAll`'s key ring prefix and postfix became `TakeAllRouting`, for keys and bait.

## Config sections and keys

`General` (`Lock Configuration`), `1. Inventory` (`Enabled` true, `Inventory Width` 8 (8-12), `Inventory Rows` 5 (0-10; 0 = the two hand cells),
`Base Carry Weight` 300 (50-10000), `Keep Slots On Death` false), `2. Slots` (`Equipment Slots` true, `Utility Slots` 3
(0-5), `Trinket Slot` true, `Backpack Slot` true, `Backpack Items` empty, `Slots Per Group` 0 (0-5), `Food Slots` 3, `Food Slots Follow
Eating` true, `Mead Slots` 3, `Ammo Slots` 3 (0-5 each), `Coin Purse` true; per player `Food Key` Z, `Mead Key` B), `3. Key Ring` (`Key Ring` true, `Key Items`
`HildirKey_forestcrypt,CryptKey,HildirKey_mountaincave,HildirKey_plainsfortress,DvergrKey,BloodGoldKey`, `Key Stack` 10
(1-100)), `4. Backpacks` (`Backpacks` true, `Backpack Portal Pass` false, and `Show Worn Backpack` true, the one key there not
synced), `6. Tacklebox` (`Tacklebox` true, `Tackle
Items` empty), all synced; `5. Look` unsynced (`Slot Labels` true, `Brown Style` true, `Weight Under Minimap` true, `Food And Mead Bar` true,
`Panel Theme` Timber (Brown, Timber), `Timber Border Width` 5.5 (3-8), `Timber Border Jaggedness` 1.5 (0-2.5), `Night Shade` 0.65 (0.3-1),
`Crafting Panel Width` 100 (0-600), `Crafting Panel Height` 90 (0-400); first 160 and 120, slimmed the same day at the
user's word). The keys
kept the names they had in OpenKeep's section 10 (never released); only the sections are new. Each key's meaning is
its description in the .cfg (bound in `src/Core/InventorySettings.cs`, `InventoryModule.cs`, `Consume/ConsumeSettings.cs`,
`Ring/KeyRingSettings.cs`, `Backpacks/BackpackSettings.cs` and `Tackle/TackleboxSettings.cs`); the README is only a
short store page. The YAML files and the default recipes are below.

## Backpack and tacklebox YAML files

The reference for players and server admins (moved unchanged from the README, which links here).

Slots and carry weight take turns, and each pack's recipe takes the one before it, so you upgrade rather than collect.
Crafting the next pack while the one it takes is in the Backpack slot upgrades it there: no free cell needed, the pack's
rows and what they hold stay (the tackleboxes the same in the Tacklebox slot):

| Biome | Backpack | Crafted at | Cost | Slots | Carry weight |
| --- | --- | --- | --- | --- | --- |
| Meadows | Deerhide Satchel | Workbench, level 2 | 10 Deer hide, 8 Leather scraps | +4 | |
| Black Forest | Trollhide Backpack | Forge, level 1 | Deerhide Satchel, 20 Troll hide, 2 Bronze | +4 | +50 |
| Swamp | Rootbound Pack | Forge, level 2 | Trollhide Backpack, 5 Iron, 10 Root, 4 Guck | +8 | +50 |
| Mountains | Wolfpelt Pack | Forge, level 3 | Rootbound Pack, 12 Wolf pelt, 6 Silver | +8 | +100 |
| Plains | Lox Hauler | Forge, level 4 | Wolfpelt Pack, 8 Lox pelt, 8 Black metal, 12 Linen thread | +12 | +100 |
| Mistlands | Carapace Pack | Black forge, level 1 | Lox Hauler, 12 Carapace, 8 Scale hide, 6 Blue jute | +12 | +150 |
| Ashlands | Asksvin Pack | Black forge, level 2 | Carapace Pack, 10 Asksvin hide, 6 Flametal, 4 Morgen sinew | +16 | +150 |
| Deep North | Moosehide Pack | Black forge, level 4 | Asksvin Pack, 10 Moose hide, 4 Moose sinew, 5 Gold | +16 | +200 |

Every pack's station, level, cost, slots and carry weight can be changed in `PackPanel.Backpacks.yml` (below).

| Biome | Tacklebox | Crafted at | Cost | Cells |
| --- | --- | --- | --- | --- |
| Meadows | Driftwood Tacklebox | Workbench, level 2 | 10 Wood, 10 Leather scraps, 5 Deer hide | 1 |
| Black Forest | Finewood Tacklebox | Workbench, level 3 | Driftwood Tacklebox, 10 Fine wood, 10 Troll hide, 2 Bronze | 2 |
| Mistlands | Carapace Tacklebox | Black forge, level 1 | Finewood Tacklebox, 10 Carapace, 10 Yggdrasil wood | 6 |
| Ashlands | Flametal Tacklebox | Black forge, level 3 | Carapace Tacklebox, 5 Flametal, 10 Asksvin hide | 8 |

Every box's station, level, cost and cells can be changed in `PackPanel.Tackleboxes.yml` (below).

`PackPanel.Backpacks.yml`, next to the cfg, created on first start, hot reloaded a few seconds after a save, synced
from the server, and editable in game through the Configuration Manager entry `Edit backpacks`. Extra files named
`PackPanel.Backpacks<anything>.yml` are merged in. Every key is optional; one left out keeps the default (the table
above). `station` is a crafting station's prefab name (`piece_workbench`, `forge`, `blackforge`, ...), `level` 1 to 10,
`cost` prefab:amount pairs, `slots` 0 to 40, `carry` 0 to 1000, `portal` true or false (only with `Backpack Portal
Pass` on).
```yaml
backpacks:
  PackPanel_DeerhideSatchel: { slots: 8 }
  PackPanel_TrollhideBackpack: { station: forge, level: 1, cost: "PackPanel_DeerhideSatchel:1, TrollHide:20, Bronze:2" }
  PackPanel_MoosehidePack: { carry: 250, portal: true }
```

`PackPanel.Tackleboxes.yml` works the same way for the tackleboxes (`Edit tackleboxes`, extra files
`PackPanel.Tackleboxes<anything>.yml`): `station`, `level` and `cost` as above, and `cells` 0 to 20.
```yaml
tackleboxes:
  PackPanel_DriftwoodTacklebox: { cells: 2 }
  PackPanel_FlametalTacklebox: { cells: 12 }
```

## Network and file names

- Player custom data: `PackPanel.inventoryLayout` (the layout the items were last arranged in, `1|W|R|head1,chest1,...`,
  written on every layout apply, removed when `Enabled` is off), `PackPanel.mainGrid` (`width|rows|blocked`, for
  OpenKeep, written and removed with it), `PackPanel.keysSeen` (the key prefab names this character has ever carried,
  comma separated, for the new-key notice; kept when `Enabled` is off).
- Player ZDO: `PackPanel.backpack` (int), the stable hash of the worn pack's prefab name, 0 for none; written by the
  player's own client when it changes, read by every client to hang that pack's model on the player's back.
- Prefabs: `PackPanel_DeerhideSatchel`, `PackPanel_TrollhideBackpack`, `PackPanel_RootboundPack`,
  `PackPanel_WolfpeltPack`, `PackPanel_LoxHauler`, `PackPanel_CarapacePack`, `PackPanel_AsksvinPack`,
  `PackPanel_MoosehidePack` (items, in ZNetScene and ObjectDB on every peer; each built from the game's `TrollHide` with
  the model of the embedded bundle `packpanel_backpacks`, asset `packpanel_<snake case>`, e.g. `packpanel_lox_hauler`),
  their recipes `Recipe_<prefab>` (ObjectDB only). Icons `assets/backpack_<word>.png`. The tackleboxes
  `PackPanel_DriftwoodTacklebox`, `PackPanel_FinewoodTacklebox`, `PackPanel_CarapaceTacklebox` and
  `PackPanel_FlametalTacklebox` the same way from the bundle `packpanel_tackleboxes` (asset
  `packpanel_driftwood_tacklebox` ...), icons `assets/tacklebox_<word>.png`.
- GameObjects created: `PackPanel_slots` (the slot panel, a child of the player panel), `PackPanel_stats` (the stat
  boxes' panel, a child of the player panel), `PackPanel_buttonstrip` (OpenKeep's strip mark), `PackPanel_keyring` (the
  key ring's pop-up, a child of the player panel, with `wire`, `hub` and `caption`), `PackPanel_keynotice` (the new-key note under the ring button), `PackPanel_consumebar` (the Food and Mead bar, a child of the HUD's `hudroot` after `healthpanel`, with the squares `PackPanel_food` and `PackPanel_mead`), `PackPanel_keyring_button` (in the
  slot panel), `PackPanel_tacklebox` (the tacklebox's pop-up, a child of the player panel), `PackPanel_tab_gear` and
  `PackPanel_tab_consumables` (the tab buttons, in the slot panel), `PackPanel_gearstats` (the Gear tab's sheet, in the
  slot panel, with `viewport/content/row/label` and `value`, and `scrollbar/handle`), `PackPanel_blocked` (the cross on
  a blocked cell's element), `PackPanel_hint` (a caption on a slot element), `PackPanel_divider`,
  `PackPanel_timberwood` and `PackPanel_frame` (the timber theme on a panel), `PackPanel_stat_icon_frame`,
  `PackPanel_backpack` (the mount on a wearer's Spine2 bone, with the model under it, named after the pack's prefab),
  `PackPanel_worn_<word>` (each worn model's template on BundlePrefabs' inactive bench); sprites `PackPanel_<picture>`;
  PlateColumn boxes `..._packpanel_armor` (the HUD copy of the armour box) and `..._packpanel_weight`.
- Files next to the cfg: `PackPanel.Backpacks.yml`, `PackPanel.Tackleboxes.yml`.
- Charter articles: `packpanel_backpacks`, `packpanel_tackleboxes`, plus the cfg sync of the shared libraries.
- Localization keys: `$packpanel_head`, `_chest`, `_legs`, `_back`, `_backpack`, `_utility`, `_trinket`, `_food`, `_mead`, `_ammo`,
  `_coins`, `_wrongslot`, `_dropped`, the tabs `_tab_gear`, `_tab_consumables`, the stat sheet's headings
  `_stat_resistances`, `_stat_gear`, `_stat_epicloot`, `_stat_offence`, `_stat_defence`, `_stat_resources`,
  `_stat_movement`, `_stat_skills`, `_stat_other`, and for the key ring `_keys`,
  `_keyring`, `_nokeysheld`, `_notakey`, `_keynew`, `_keysnew`, for the consume keys `_nothingtoeat`, `_nothingtodrink`, for the stat breakdowns `_tip_base`, `_tip_other`,
  `_tip_nothing`, `_tip_effect`, `_tip_carry`, `_tip_world`, `_tip_heaviest`, `_tip_missinghealth`, `_tip_parryarmor`, `_tip_skill` (the stat sheet uses the game's own `$item_`, `$inventory_` and `$se_` words); backpacks
  `$packpanel_backpack_<word>` and `$packpanel_backpack_<word>_description` for deerhide, trollhide, rootbound,
  wolfpelt, lox, carapace, asksvin and moosehide, and `$packpanel_backpack_noroom`; the tacklebox `$packpanel_tackle`
  (the slot's caption), `$packpanel_bait` (a cell's), `$packpanel_tacklebox_full`, and
  `$packpanel_tacklebox_<word>` and `$packpanel_tacklebox_<word>_description` for driftwood, finewood, carapace and
  flametal.
- No RPC, no console command.

## Decisions

The history before the move (2026-09-28): the user asked for OpenKeep to make the player inventory wider and taller,
and for extra slots like another mod's, but always visible and labelled: Head, Chest, Legs, Back, Backpack, three
utilities all worn, and Food, Mead, Ammo and Quick up to 5 each, food following the foods a player can eat; a brown
framed look with the stat readouts as small square boxes, and a coin purse among them; rings later; default 8 x 6,
made 8 x 5 at the user's request later the same day. Revised after seeing it in game the same day (through DevBridge):
no quick slots (their keys, the HUD bar and the settings are gone; an old record's quick slots read as Retired, so the
cells after them keep their places and their items move into the grid), the purse a slot at the bottom left of the slot
panel instead of a box, the button row inside the panel, even gaps around the box column, opaque brown, bigger captions
without an outline, and GrindstoneSkills' Defense box gone (its summary is now the Defense tooltip in the skills
panel). The Timber panel theme came from the user's Codex agent. The same day the whole section moved out of OpenKeep
into PackPanel (below); its settings kept their keys under new sections, its names became PackPanel's (nothing had
shipped, so nothing migrates: a test character's old `OpenKeep.inventoryLayout` record is ignored and its items are
re-placed from the game's layout once, and test copies of `OpenKeep_*` backpacks vanish at load).

- The split (2026-09-28): the user was told the UI and inventory work had to be a separate mod from the storage mod.
  Moved: the whole inventory section, backpacks, key ring, stat panels, HUD boxes and the look. Stayed in OpenKeep:
  the storage features and their own UI (the button row, the trash can and trash mode, the Salvage tab, marks,
  touches, hover texts). GrindstoneSkills' skill tooltips and Elite Creatures Reborn's world tier stayed in their mods.
  The two mods share no code and no reference; see "With OpenKeep".
- Storage: every slot is a cell of the game's own player `Inventory`, in the rows under the main grid (`InventoryLayout`),
  so the game saves, weighs and entombs slot items with no data of ours besides the layout record. The alternative (a
  second inventory in custom data) would need its own save, weight and tombstone handling. The grid element of each
  slot cell is moved into the slot panel (`SlotElements`); the game's `InventoryGrid` still owns it (index-based
  `m_elements`, clicks by GameObject, hover by rect), so drag and drop, split, tooltips, right click and the gamepad
  are the game's. The gamepad walks the logical grid, so from the last main row "down" reaches the slots in their
  cell order, not the panel's picture.
- Layout record: `PackPanel.inventoryLayout` keeps W, main rows and the slot ids in cell order. Every apply plans from
  the recorded layout to the wanted one (`LayoutMigration`): a slot item keeps its slot id when the new layout has it
  and it still fits the rule; a main item keeps its cell when that cell is still main; everything else goes to free
  main cells bottom row first (the hotbar row last, as the game's own non-weapon placement); what finds no cell is
  dropped with the game's `DropItem` and one message. A character without a record is taken as the game's 8 x
  `invrows` with no slots, so anything below (another inventory mod's rows) is re-placed into the new grid.
- Load: `Player.Load` accepts no column beyond the inventory's width (it drops the item) but any row, so the prefix
  widens the inventory to 32 first; the postfix applies the layout without dropping (the player is not in the world
  yet: overflow is parked below the layout, out of bounds but kept), and `Player.OnSpawned`'s `SetInventorySize`
  applies it again with dropping. `Player.SetInventorySize` (spawn, the trader's row, `inventorysize`) is replaced while
  PackPanel is on or a record exists: the game's version drops every item outside its rows (`DropInvalidItems`), which
  would throw the slots on the ground. With `Enabled` off and no record nothing is patched in behaviour, so another
  inventory mod keeps its rows. Bought rows (`invrows` above 4) add to `Inventory Rows`.
- Fewer rows than the game's (2026-10-02, a player who keeps a one-row inventory, then the user's call): `Inventory
  Rows` goes down to 0. 0 leaves two main cells, the hands, at the top left (`InventorySettings.HandCells`; hotbar
  keys 1 and 2), the rest of row 0 blocked. The layout counts the main cells that stay without a backpack
  (`InventoryLayout.BaseCells`: whole rows, or the hands) and a worn backpack's cells follow them in reading order, so
  a pack first fills the rest of the hands row (keys 3 to 8), then rows below; the spare right end of the bottom row
  is blocked as before, so the published grid (`width|rows|blocked`) keeps its format and OpenKeep and Elite
  Creatures Reborn need no change. A pack's cells are `IsPackCell` (reading order from `BaseCells`), not its rows;
  for whole rows that is the same set as before. The main grid always has at least the hands row, so the slots
  never reach row 0 and no hotbar key uses a slot. The player panel follows its rows however few (the user's call,
  2026-10-02: OpenKeep's buttons right under the hand cells), and a chest's panel moves up under it; the stats and
  slot panels hang from its top at the height a 4-row grid gives (`BackpackPanelHeight`), so they keep their size. A
  chest wider than 8 columns may then reach under the side panels. The game's HUD hotbar still draws 8 frames; keys
  3 to 8 find nothing in blocked cells. Lowering the setting drops what no longer fits.
- Room: `FindEmptySlot`, `GetEmptySlots`, `HaveEmptySlot` and `CanAddItem` count the main grid only, so pickups,
  crafting, purchases and the tombstone's easy-fit check never land in a slot (the bottom-first search would fill the
  slot rows first). `FindFreeStackItem` is left alone: arrows, food and coins stack onto a slot's stack, as with any
  partial stack. Coins go into the purse first (`CoinPurse`), and an empty purse counts as room for coins.
- Ammo first (the user's request, 2026-09-28: "when crafting arrows, or picking them up the arrow slots should be
  prioritized"): `AmmoRouting`, a prefix on `Inventory.AddItem(ItemData)` like the purse's, sends equipable ammo
  (`ItemType.Ammo`: arrows, bolts, missiles) onto a same stack in the Ammo slots left to right, then into an empty Ammo
  slot, before the game's add (which would stack onto whichever stack it finds first, often one in the grid). The
  game's crafting reaches it (`InventoryGui.DoCrafting` adds by name with no cell, which ends in `AddItem(ItemData)`), as
  do pickups, traders and click moves; `CanAddItem` counts empty Ammo slots as room (`MainCells.AmmoRoom`), so a full
  grid still crafts and picks up arrows. A take all moves arrows that landed in main cells into the Ammo slots after it
  (`TakeAllRouting`), a take all from the own grave too (arrows that lay in the grid top up the Ammo slots). Bait (also
  `ItemType.Ammo`, of the rod's ammo type) is left to the tacklebox. The stacking is shared with the tacklebox (`SlotFill`). No setting:
  0 Ammo slots routes nothing.
- Slot rules: the dropped item must suit its cell, and in a swap the displaced item must suit the cell the dropped one
  came from. Checked in the `OnSelectedItem` prefix before the game starts, because after a refused `DropItem` the game
  would equip whatever lies under the pointer (it re-equips assuming the drop happened), and again in `DropItem`
  (other callers) and in the positional `AddItem`s. Cells after the last slot (the last slot row's remainder) refuse
  everything and are hidden. The load (`skipValidPositionCheck`) is never refused.
- Worn slots show what is worn: equipping moves the item into its slot, swapping with an unworn item there (the
  game's EquipItem takes the old piece off first; `WornPlacement.BeginEquip` keeps it in the slot until the new one
  arrives, so it lands where the new piece was); unequipping moves it to a free main cell, or leaves it when the grid
  is full. The game unequips both items of a drag before `DropItem` and re-equips them after, so moves are suspended
  for the whole `OnSelectedItem` and settled afterwards (in a worn slot: put on; worn but outside the worn slots: taken
  off). Suspended too while loading (the layout places worn items) and while the tombstone is made (the grave keeps
  the armour in its slots; `MoveAll` from the own grave puts it back on). With `Equipment Slots` off, armour is worn
  from the grid as in the game.
- Worn utilities (three by default, up to five): the game has one `m_utilityItem`. The others are a list on the local
  player (`ExtraUtilities`): `EquipItem`'s prefix wears the item beside the game's one while there is room and no item
  of the same name is worn (with the game's own guards: in the inventory, not attacking or dodging, not swimming,
  durability, world level), else the game replaces its utility as before. `IsItemEquiped` and `IsItemTypeEquiped`
  answer yes for them, so the game's `UnequipItem`, `ToggleEquipped` and drag paths treat them like its own;
  `UnequipItem`'s postfix forgets them. Their equip and set status effects are kept in a set of our own next to the
  game's `m_equipmentStatusEffects` (never removed while the game grants the same effect); set counts, eitr regen, the
  equipment modifiers (`UpdateModifiers` sums `s_equipmentModifierSourceFields`), equipment weight and durability count
  them. Only the game's utility shows on the character (`SetupVisEquipment`). A utility deleted without an unequip
  (trash, salvage) is pruned every frame. The list is rebuilt on every load from the saved `m_equipped` flags: the load
  prefix sets a provisional layout so the game's `EquipInventoryItems` already wears up to three.
- Trinket slot (the user's request, 2026-09-28: "a trinket slot under utility"): the game has one worn trinket
  (`Humanoid.m_trinketItem`, `ItemType.Trinket`, the adrenaline trinkets such as `TrinketBronzeHealth`) and equips,
  replaces, saves, entombs and shows it like its utility, so the slot is a worn kind like Head (`SlotKind.Trinket`,
  `Trinket Slot` on by default) and needs no patch of its own: `WornPlacement`, `DragSettle`, the graves and Keep Slots
  On Death handle it through `SlotRules.IsWorn`/`WornKindOf`. One slot, not a group: wearing several would need an
  `ExtraUtilities` for trinkets (the adrenaline bar reads the one field). In the layout it follows the utilities, so an
  existing record's food, mead and ammo cells move one cell on (by slot id, `LayoutMigration`); the enum value is
  appended last, as the record stores ids. Drawn under the utilities in the Gear tab's right column; with five
  utilities that column is six rows, and both tabs grow to six (`SlotPanelLayout.ContentRows`), the sheet with them.
  Its empty-slot icon (`assets/icon_trinket.png`, a rune pendant) is drawn by
  `artwork/slot-icons-silhouette-concept/draw.py` with the approved set.
- Graves: `MoveInventoryToGrave` copies the width, but a grave loading again (another client, the area reloading) gets
  the tombstone prefab's width and its load drops items beyond it, so `Container.Load` of a tombstone widens the
  inventory to 32 and then to its widest item, on every client. A take all from anything that is not a grave keeps
  out of the slots (`GravePatches.ForeignTakeAll`), so a chest's items do not land in slots because their chest cell
  maps onto one; the grave is recognised by finding the `TombStone` whose container holds the inventory (only on a
  take all).
- Food slots: `Food Slots Follow Eating` reads FeastMaster's own config entry (`com.FeastMaster`, `0. Global
  Settings`, `Food Slots`) through BepInEx's `Chainloader.PluginInfos` on first use, and relays out when it changes;
  without FeastMaster it is the game's 3. Food is a consumable with a food value; mead any other consumable.
- UI: the player panel is widened one element step (70) per column beyond 8 and sized to the main rows (the game's
  `m_invGridHeight` per row), plus, with OpenKeep, a 30 unit strip at the bottom for its button row (`ButtonStrip`;
  OpenKeep places the player panel's row 2 units above the edge while the strip is shown, the container panel's Sort
  button still hangs below its panel). Without OpenKeep there are no buttons and no strip. The grid root is kept to
  the main rows every frame (the game sizes it for every row, and its invisible image would take clicks below the
  panel). The game's panel background (`Bkg`) reaches 10 units past the panel's rect on every side, so the side
  panels sit 4 units past that edge, 4 apart (`PanelDress.SideGap`; 12 until the user asked for them
  closer, 2026-09-28; the pop-ups under the slot panel keep 12, `Gap`). The stat boxes have their own panel (`PackPanel_stats`, `StatsPanel`)
  between the inventory panel and the slot panel, as tall as its boxes (the inventory panel's margin above the first and
  below the last; it was as tall as the inventory panel's background until 2026-10-02) and one box wide with 10
  units each side (the inventory panel's 18 until the user asked for a narrower column, 2026-09-28; the top keeps 18 so
  the first box stays level with the grid's first row); the library's column of boxes (armour, weight, world tier and any other mod's box, by rank) is
  moved into it (`PanelDress`). History: the user first had the boxes as a row in the slot panel's last row beside the
  purse (tried in game 2026-09-28, a HorizontalLayoutGroup swapped in by a `BoxShape` class, now gone), then asked the
  same night for them back in a column in a brown panel of their own between the two panels. The box container belongs
  to the PlateColumn library and stays a child of the player panel, so every copy still finds it and adds its boxes;
  the library places it only when it makes it, so the move holds. PackPanel makes the column itself (`Column.Boxes`)
  when no other mod has. With `Enabled` off the column goes back to the library's place. The side panels are drawn
  before the player panel's background (the library keeps the container right after that background), so the boxes
  draw over the stats panel; no backgrounds overlap. The slot panel (`PackPanel_slots`) has the same top as the
  inventory panel's background, the tab buttons across its top, then the shown tab's cells (below), 5 cells wide, the
  purse's row last under a divider (no row without a purse or ring); both side panels' backgrounds take clicks so a
  drop between them is not a drop on the ground. OpenKeep's trash can is a button in its row, right of Sort, while
  PackPanel is on (OpenKeep decides at `InventoryGui.Awake`); its object is `OpenKeep_trashcan`, because the library
  adopts anything named `OpenKeep_trash` (the pre-library trash plate) into its boxes. Brown Style swaps sprites and
  clears the game's `litpanel` material on the panels (it darkened the brown to near black; the slot panel with the
  default material looked brown next to it); the frame adds 4 units above the first row. The skin's fills are opaque
  (PlateColumn's `SkinPalette`): the game renders in linear colour space, where a 0.94 alpha dark fill over grass
  looked half transparent. A container wider than 8 widens the container panel and moves its scrollbar with the right
  edge.
- Stat breakdowns (the user's request, 2026-09-28: "when you hover a stat, something that says the breakdown of what
  is giving you those stats"): every line of the game's numbers carries a breakdown (`StatSheet.Line.Tip`) that its row
  shows in the game's `UITooltip` (it follows the pointer), dressed in the stat boxes' bordered box through PlateColumn's
  `Column.DressTooltip` (the craft button's tooltip prefab if that fails); each row takes the pointer with a clear
  image, and the viewport's `RectMask2D` keeps rows scrolled out of view from answering. Summed as the game sums
  (`StatTips`, `GearTips`, `TipText`): health and stamina = `m_baseHP` / `m_baseStamina` + each `m_foods` entry's
  current `m_health` / `m_stamina` (eitr: foods only); armour = each worn helmet, chest, legs and cape's `GetArmor()`
  (what `GetBodyArmor` adds before `ApplyArmorMods`); carry weight = `m_maxCarryWeight` + each `SE_Stats`'
  `m_addMaxCarryWeight`, times `Game.m_carryWeightRate` (shown when not 1), then the five heaviest stacks; movement =
  each worn item's `m_movementModifier`, then each effect's `m_speedModifier`; durability = every equipped item that
  wears out, most worn first; a resistance = each worn armour piece's `m_damageModifiers` (helmet, chest, legs, cape:
  `ApplyArmorDamageMods`; a shield's only count in a block and show in its own section) and each effect's `m_mods` pair
  for that type; the other modifiers = each worn item's value of `Player.s_equipmentModifierSourceFields[i]`. Whatever the
  named parts do not explain (Epic Loot, other mods, the game's armour effects) is one "Other effects" line, so a
  breakdown always adds up to its line; a line nothing gives says "Nothing you wear or carry gives this". "Worn" is every
  equipped item in the inventory, which includes the hands and PackPanel's extra utilities. Epic Loot's lines and the
  headings have no breakdown. The tips are part of the sheet's change key, so they are written again when they change.
- Weapons and shields in hand (the user's request, 2026-09-28: "any equipped weapons/shields should have their stats
  included"): after the core lines, a section per held item headed by its name, the right hand's then the left's
  (`HeldItems`; while sheathed the game unequips both and keeps them as `m_hiddenRightItem` / `m_hiddenLeftItem`, and
  the sheet keeps showing them, since they come back on the next draw). Nothing for bare hands. Numbers are what the
  game's attack and block use, not the item tooltip's raw values, so they change with skill, meads and gear: a damage
  type's line is the range one primary hit does (`GetDamage` plus the ammo's for a weapon that shoots, the ammo
  `Attack.UseAmmo` would take; times the attack's `m_damageMultiplier`, level and missing-health multipliers; times each
  status effect's `ModifyAttack` for the weapon's skill, pure in the game and in our mods; times
  `GetRandomSkillRange`, the tooltip's yellow numbers), at full draw for a bow and without a combo's doubled last hit.
  Chop and pickaxe damage are left out, as the game's tooltip does. Attack stamina follows `Attack.GetAttackStamina`
  (gear, `ModifyAttackStaminaUsage`, a third off at skill 100, the stamina back per missing health), eitr
  `GetAttackEitr`, health `GetAttackHealth`, stamina held while drawing `Player`'s bow draw (before its halving at full
  draw). Knockback is the weapon's `m_attackForce` plus the ammo's (raw, as the tooltip), backstab its multiplier. Block
  lines belong only to the item the game blocks with (`GetCurrentBlocker`: the left hand's item, else the weapon), so a
  sword beside a shield shows no block armour: `GetBlockPower` at the Blocking skill, `GetDeflectionForce`, the parry
  bonus times every effect's `ModifyTimedBlockBonus` (its breakdown ends with the block armour a parry makes), parry
  adrenaline, and the item's damage modifiers, which the game applies to a blocked hit only. Labels are the game's
  `$item_` and `$inventory_` words; breakdowns name the item, the ammo, each effect and the skill with its level.
- Slot tabs and the stat sheet (the user's request, 2026-09-28, replacing one row per group): two buttons across the
  slot panel's top, Gear and Consumables. Gear has Head, Chest, Legs, Back ("cloak" in the request) and Backpack in a
  column on the left, the utilities in a column on the right (the request said "3 utility on left" for both columns;
  read as right, since the stats were to be in the centre) and a stat sheet between them; Consumables has a row each of
  food, mead and ammo. The purse's row with the ring button stays at the bottom under both (coins and keys are
  neither), and the panel is the same size in both tabs (5 rows above the purse's, 6 with five utilities and the
  trinket), so nothing jumps
  when turning. Only elements are hidden: the hidden tab's slots are still cells of the inventory, so their items
  weigh, stay worn and go to the grave as before. The two tabs' cells lie on the same spots and the game's
  `GetHoveredElement` takes the first element under the pointer, hidden or not (the Gear cells come first), so
  `HoveredCell` skips hidden ones; without it the Consumables tab's tooltips went to the hidden Gear cells (an arrow
  in an Ammo slot showed none; food showed text left from before), as did equip-hovered and the touch drop. The tab is static for the session (Gear first) and a change is
  applied through `SlotElements.Invalidate`, the same re-placing a settings change uses. Consumables is not clickable
  without consumable slots. The buttons copy the game's take-all button like OpenKeep's row, so they keep the vanilla
  look in every theme; the copy loses its `UIGamePad` (it would answer the take-all key), and `ButtonTextColor`,
  `ButtonImageColor` and `Localize`, which reset colours every frame and the caption on a language change. Gamepad:
  it walks cells, so a move onto a slot of the hidden tab turns to that tab (`SlotTabs.FollowGamepad`); the gear row
  and the consumables rows lie in different cell rows, so down from the gear reaches food. The sheet (the user listed
  health, armour, movement speed, frost resist, durability and weight, "not sure what else") lists the game's own
  numbers from its own getters in its own words (`$item_food_health` ... `$item_durability`, `$inventory_<type>`,
  `$inventory_<modifier>`, `Player.s_equipmentModifierTooltips`): max health, stamina and eitr (eitr only above 0),
  `GetBodyArmor`, total weight over `GetMaxCarryWeight`, movement as the game walks ((1 + gear) x (1 + the sum of
  `SE_Stats.m_speedModifier`) - 1), the lowest durability of equipped items that wear out (weapons in hand included;
  lowest rather than an average, as it says when to repair), the non-normal damage modifiers from `GetDamageModifiers`
  (blunt, slash, pierce and the five elements; chop and pickaxe left out), and every non-zero equipment modifier after
  movement through `GetEquipmentModifierPlusSE` (effects included where the game's tooltip includes them, flat from
  index 10 as the game prints it). The armour and weight boxes in the stats panel stay; the sheet repeats them.
- The scrolling stat sheet (the user's calls the same day, after Epic Loot was installed for a test: "EPIC loot will
  have a lot of stats"). Four Mythic armour pieces carried 32 Epic Loot effects of 23 types, and the centre fits about
  15 lines at the first size. Offered a third tab, a scrolling centre, a flyout and a hover tooltip, the user first
  picked a tab, then a panel beside the slot panel toggled by a Stats button (built, never seen in game), then asked
  for neither: the centre scrolls and holds everything, in smaller text (12, headings 10.5; it was auto-sized up to
  15), in an inset with round corners (`RoundedFill`, 6 units). It scrolls with the mouse wheel at the game's recipe
  list's speed, a thin bronze bar in the right margin showing only when needed. The lines are rows (`StatRows`): the
  value at the right at its own width, the label wrapping under itself in the rest, since the centre is narrow and
  Epic Loot's sentences are long; the two-column texts of the first build could not wrap without going out of step.
  Every text is restyled before it is written: made under a hidden panel, TextMeshPro only set the first build's texts
  up when first shown and put its defaults back (word wrap on, size 18), which wrapped "Attack stamina usage" onto two
  lines out of step with its value in the first in-game look.
- Epic Loot (tested with 0.14.13): read through the public API it publishes for other mods, `EpicLoot.API` (API
  version 1), by reflection on first use (`EpicLootLink`): `GetAllActiveMagicEffects(Player, string)` (equipped magic
  items' effects and set bonuses, JSON), `GetTotalPlayerActiveMagicEffectValue(Player, string, float, ItemData)` per
  type, `GetMagicItemEffectDefinition(string)` for its `DisplayText` token, localized and formatted with the total
  ("Frost Resistance {0:0.#}%"); a missing word ("[mod_epicloot_...]") falls back to the type name spaced out. Epic Loot
  has no categories, so `EffectGroups` groups by words in the type name (skills, defence, offence, movement, health and
  stamina, other; first match wins). Its effects that also feed the game's numbers (IncreaseHealth, ModifyArmor,
  AddCarryWeight, ModifyMovementSpeed) are listed as well: the core lines are totals, these say what the magic adds.
  Epic Loot finds equipped items through `Inventory.GetEquippedItems`, so PackPanel's extra utilities (marked
  `m_equipped`) count without its `RegisterEquipmentProvider`. Without Epic Loot, or with an API missing a method, the
  sheet shows the game's stats only and one warning is logged.
- Night shade (the user's request, 2026-10-02: the game's UI darkens when it gets dark outside; implement that for
  PackPanel). The game's wooden panels draw with the `litpanel` material (shader `Custom/LitGui`), which reads the sun
  and ambient colours `EnvMan` sets every frame (`_SunColor` = the sun light's colour x intensity, `_AmbientColor`).
  PackPanel's skins clear that material (it took the brown near black, and it tints with the weather: purple around
  the Elder, Moder and Yagluth), so they never darkened. `NightShade` reads the same two colours each frame, takes
  their luminance (ambient + 0.25 x sun) and maps a clear Meadows noon (0.90, from the game's weather data) to 1 and a
  clear Meadows midnight (0.42, read in game at day fraction 0.78: ambient 0.357/0.368/0.485, sun 0.191/0.202/0.255)
  to `Night Shade` (0.65), clamped, moving at most 0.5 a second. Dark weather lands in between or at the floor (a
  Swamp rain noon about 0.47 reads as night). Grey only. Applied as each graphic's `CanvasRenderer` colour, which
  multiplies its own colour without touching it, to every panel `GridSkin.Panel` skins and every Timber wallpaper (`TimberBackground`, the game menus Panel Theme covers included), and only while the
  graphic uses the default UI material, so one still drawn with the game's lit material is not shaded twice. 1 with
  Brown Style off (the game's panels shade themselves), outside a world (no `EnvMan`) and with `Night Shade = 1`.
  Icons, numbers, captions and inventory cells are not shaded (cells: the user's request, 2026-10-02; the cell's
  Button hover tint writes the same `CanvasRenderer` colour, so a shaded cell went light on hover and could stay so).
- Captions (the user asked for them to be much easier to see, and said an item may cover them): bold, bright
  (#FAE6B8), auto-sized 10 to 20 over the whole cell (3 unit inset), drawn under the icon and shown only while the slot
  is empty (`InventoryElement.m_used`, which the game sets every frame). No outline: with the game's font material a
  TMP outline swallowed the letters and left them dark with a light rim (seen in game).
- The purse's element shows its count alone ("51") rather than the game's "51/999".
- The look follows the user's mockup (2026-09-28), refined in game over several rounds: plain dark brown panels with a
  bronze frame and small rivets (`SkinArt.Panel`; a tiled wood-plank version read as thin stripes at this scale and was
  dropped, so was a knotwork cell), dark recessed cells, and in every empty slot a bronze icon of PackPanel's own
  (`icon_<slot>.png`; the game's item icons tinted bronze read as dull blobs) above the slot's name in the game's serif
  (the take-all button's font, Averia Serif). The coin purse shows the game's coins, tinted. OpenKeep's buttons,
  including the row's trash can, clone the game's take-all button and keep its vanilla sprites, text colours and
  interaction states in every panel theme; PackPanel does not skin them. The purse's row sits 12 units lower under a
  bronze divider.
- Weight Under Minimap (per player, on; the user asked for the world tier and weight under the minimap as well,
  2026-09-28): PlateColumn's `HudRow` holds copies of the column's box in a column right of the minimap's small root
  (they hide with it: big map open, no-map worlds, HUD off); `HudWeight` writes the weight there from a `Hud.Update`
  postfix (the game writes its own weight only while the inventory is open), in the game's format, at the weight rank
  so it reads armour, weight, world tier top to bottom like the column; `HudStats` adds the armour box and shows the
  column at full size (the stats panel's 48 unit squares; at 65% under the map they were too small, the user said
  2026-09-30, and asked for them right of the map, top to bottom). The game leaves the map 40 units from the screen's
  right edge, so `HudRoom` moves the map left by what the column needs (gap 6, box 48, 10 to the screen edge: 24
  units) and the game's status effect row with it (its first icon sits 26 units left of the map), both remembered and
  put back when no box shows, the map is off or PackPanel is off. Elite Creatures Reborn adds its world tier box the
  same way. Off with the master switch, like every key. The key keeps its old name (`Weight Under Minimap`).
- Keep Slots On Death (off; the user asked for it): the `Player.CreateTombStone` prefix takes the items of every slot
  but the purse out of the inventory list (so neither the grave nor a world modifier that deletes items at death sees
  them) with whether each was worn, and the finalizer puts them back, worn ones marked `m_equipped`. The game saves the
  character in `Game._RequestRespawn`, after the tombstone, and `Player.Load`'s `EquipInventoryItems` wears what is
  marked, so the armour and the three utilities are on again after the respawn; the dead body in between has them in
  the inventory but not on.
- Base Carry Weight (300; the user asked for it on 2026-09-28): the game's limit is `m_maxCarryWeight` plus what status
  effects add, times `Game.m_carryWeightRate` (`Player.GetMaxCarryWeight`); the effects only add (Megingjord +150), so
  the postfix adds (setting - `m_maxCarryWeight`) x the rate and the result is exactly the game's formula with the new
  base. At 300, or with the master switch off, it changes nothing, so a mod that sets `m_maxCarryWeight` keeps working.
- Slots Per Group (0; the user asked on 2026-09-28 for one number for utility, food, mead and ammo, 1 to 5, beside the
  individual settings): `SlotCounts` answers every group's count for the layout and the worn-utility limit. 1 to 5
  wins over each group's own key and over Food Slots Follow Eating; 0 hands back to them. Utility Slots went from 0-3
  to 0-5 at the same time: the worn list has no fixed size, and a group row of five fits the slot panel's five columns.
- Backpacks (the user's requests, 2026-09-28: first a Trollhide Backpack giving 8 more inventory slots when equipped,
  20 TrollHide and 2 Ruby, then a pack for every biome from Meadows to Deep North, slots and carry weight taking turns;
  the user chose own models worn on the back and seen by every player, overflow dropped when a pack comes off, the
  troll leather armour as the Trollhide's look, and accepted the plan's table: +4 slots, then +50 carry weight, then
  +4 slots, ... up to +16 slots and +200, each recipe taking the pack before it, Backpack Portal Pass optional and
  off). A pack is worn by lying in the Backpack slot: the game has no equip slot for it (capes use Shoulder, utilities
  are already three), and the slot already existed. It was a Misc item until 2026-10-04 and is equipment since (below,
  "Backpacks are equipment"). Worn, it adds its slots to the bottom of the
  main grid: as many rows as the slots need, the cells of a partly used last row beyond them blocked (`InventoryLayout`
  `BackpackSlots`, `BlockedCells`, `IsBlocked`; `IsMain` excludes them, so every room question, the migration and the
  slot rules keep out, and OpenKeep's sort through `PackPanel.mainGrid`; their elements are shown dimmed with a grey
  cross and take no clicks, `BlockedCell`, the user's request of 2026-09-28; they were hidden before). Slots rather than
  rows, so a pack gives the same count at any grid width (the user first asked for 8). The layout plans from where the
  items lie, so `LayoutBuilder` looks for the pack in the recorded layout's Backpack slot; `BackpackSlots` is not in
  the record, and the frame check in `BackpackWear` compares the layout in use with the worn pack's slots and applies
  the layout again with dropping, as a settings change does. Pickups fill the pack's cells first (they are the bottom
  rows, and the game places bottom first). Taken off, its cells' items go to free cells and the rest drops (the user's
  choice); right click takes it off only into a main cell that is not one of its own, else says there is no room. Carry weight is
  added in the `GetMaxCarryWeight` postfix with Base Carry Weight, scaled by the world modifier. Keep Slots On Death
  takes the pack out only inside `CreateTombStone`, which no frame sees, and the frame check skips a dead player, so
  death never shrinks the grid mid-way. A grave made while a pack was worn has its cells as main cells and the slots
  lower down; the take all from a grave (`Inventory.MoveAll`, after the game's take-all reply made this client the
  grave's owner) first moves the pack that lay in the grave's Backpack slot cell (a spare pack in the grid is not taken
  for it) into the empty slot and applies the layout, so every cell lines up again. The rows a pack adds are the
  layout's own (`BackpackGrave.RowsAdded`: its cells continue after the base cells), not its slots over the width:
  with the two hand cells of Inventory Rows 0 a 4 slot pack shares their row and a 12 slot pack adds one, which the
  old rounding missed, so the pack was not found in the grave (fixed 2026-10-04).
- Use on the own grave (asked 2026-10-04: "if you die with a backpack and your whole inventory full ... it needs to
  allow pickup all ... the backpack first, then all other items"): the game takes all on Use only when
  `TombStone.EasyFitInInventory` says yes, else it opens the grave, and it counted every grave item against the free
  grid cells and the grave's weight against the carry of a player who woke with no pack; with Inventory Rows 0 the
  pack is nearly the whole grid, so a full one never fit (seen in game: 20 items against 18 cells, 498 kg against
  300). `Slots/GraveFit` answers for the local player's own grave as the take all will run: the waiting pack goes to
  its slot and brings its slots and carry weight (`Carry x Game.m_carryWeightRate`), an item from a slot row of the
  grave goes back into its own slot when that cell is free, one that stacks onto what the player carries needs no
  cell, the rest need free grid cells. The weight test stays the game's, with the pack's carry added. The take all
  itself was already right (seen in game the same day: pack on first, then all 20 items, armour worn again); it
  replaced the backpack's and the tacklebox's partial count prefixes (`ExtraRoom`). Another player's grave keeps
  the game's check. Stats come from `BackpackCatalog` (the defaults) and `PackPanel.Backpacks.yml` (any key of any
  pack; unknown packs warned, out of range values rejected); the recipe objects are shared by every ObjectDB and
  refreshed on every change (station by prefab name through the game's recipes, falling back to the pack's default,
  no station found means no recipe since the game would let it be made by hand; cost as `prefab:amount` pairs,
  PackPanel's packs from the catalog so the chain holds before ObjectDB has them, unknown names skipped with a
  warning, nothing usable falls back to the default). Each item is a copy of the game's `TrollHide` (net view, sync,
  rigidbody, ground sparkle) with its model swapped in and its collider fitted to the mesh, one per stack. The models
  (ValheimAssets `packpanel_*`, procedural textures of our own at 256 px to keep the DLL small; the pivot is the Spine2
  bone's rest position in the player's frame) are dressed in a copy of the game's troll hide cape material
  (`GameMaterials.Dress`), so the game's lighting and rain apply; worn, a model hangs on Spine2 under a mount turned
  upright (Spine2 rests about 5.7 degrees back), made in the world and parented keeping its world size as the game
  attaches items. It sits where a sheathed shield or weapon also hangs; they overlap. Only the inventory panel grows
  with a pack's rows: the stats panel and the slot panel keep the height they have without them
  (`BackpackPanelHeight`, the user's call). Backpack Portal Pass replaces `Inventory.IsTeleportable` for the local
  player's inventory while the worn pack says `portal: true`: its cells may hold what portals refuse, everything else
  follows the game's rule, and items of tool tier 1000 or more never pass.
- Backpacks are equipment (the user's question, 2026-10-04: "can it be an equipment? and can we make it work with epic
  loot?", after asking whether Epic Loot could roll stats on backpacks). Epic Loot makes an item magic only when the
  game counts it equipable (`CanBeMagicItem`: `ItemData.IsEquipable`), picks effects by item type name (none allows
  Misc) and applies only equipped items' effects (`Inventory.GetEquippedItems`, the `m_equipped` flags), so a Misc pack
  got nothing. A pack is now a Utility item (`BackpackKind.ItemType`; the tackleboxes stay Misc) that PackPanel wears,
  never as the game's utility: `Humanoid.EquipItem` on any PackPanel pack, on any character, is `BackpackEquip.Wear`
  (false where no Backpack slot is laid out, so neither the main menu nor PackPanel off makes it the game's utility),
  which takes the pack worn before off and sets `m_equipped`; `WornPlacement` moves it into the Backpack slot
  (`SlotRules.WornKindOf` answers Backpack for a pack and the Backpack slot is worn; another mod's backpack only lies
  there). `IsItemEquiped` answers yes for a flagged pack in the local inventory, so the game's own unequip, drag, drop,
  chest and unequip-all paths take it off. Where it lies stays the truth for the layout: every frame `BackpackEquip.Sync`
  wears the pack in the slot and takes any other off, through EquipItem and UnequipItem so Epic Loot hears of it; a worn
  pack that left without an unequip (crafted away in place, trashed) loses the flag and an UnequipItem call tells Epic
  Loot. None of the game's equip guards (attacking, swimming, world level): the pack in the slot gives its slots, so it
  is worn. Unequip all (the tombstone) takes it off, so it goes into the grave like armour (`MoveInventoryToGrave` keeps
  equipped items with the player); with the keep-equipment world modifier it stays, like armour. The grid shows the
  game's equipped mark on it. Epic Loot needs nothing more: its enchanting table lists packs, rolls Utility effects
  (carry weight, movement, stamina, health and the like) and applies them while the pack is worn, and the stat sheet
  lists them. EliteCrafting's runes follow with Epic Loot installed (they ask Epic Loot); its own inscriptions leave
  packs out (`ItemSlots`, by the `$packpanel_backpack_` name: its effects read only the game's utility field). Right
  click from the slot keeps the old rule (a cell outside its own rows, else "no room"); a hotbar key wears it through
  the game's toggle. Upgrading a magic pack makes a plain new pack (the game crafts a new item); carrying the magic over
  was asked about and is not decided.
- Capes under the pack (the user's request, 2026-09-28: "if player has backpacks showing, make it so capes do not
  glitch through the backpack when running"). The game's capes are MagicaCloth 2 mesh cloths (`VisEquipment.SetupCloth`)
  hanging from the shoulders to the ankles (their meshes: tops 1.69-1.72, hems 0.0-0.21 in the player's frame); at rest
  their top lies inside the pack's volume, and running swings it backwards out through the pack. A pack is worn over the
  cape, so the pack holds the cape's top (`CapeHold`): while a pack hangs, every `MagicaCloth` of the shoulder item's
  instances gets MagicaCloth's max distance (`motionConstraint.useMaxDistance`, solved after collisions): 5 cm from the
  body's skinned pose where the pack covers the cape and 35 cm of cape below the pack's bottom (`Below`; the user asked
  for the hold lower down after the first build, which held only what the pack covers), then 1.5 m more per metre of
  cape further down (room for a swing of about 95 degrees), so the hem still swings (0.9 to 1.2 m). How much the pack covers comes from the pack model's lowest
  point (`ModelBounds`, 0.20 m below Spine2 for the Deerhide Satchel to 0.445 m for the Trollhide Backpack) over a
  nominal cape (top 1.70, 1.58 m long), as a fraction of MagicaCloth's depth (0 at the fixed top row, 1 at the hem),
  written as 16 keys at the steps where the solver reads the curve (at depth squared). The cape's own `useMaxDistance`
  and `maxDistance` (none of the game's capes use it) come back when the pack comes down (taken off, `Show Worn
  Backpack` off) or the cape changes (`m_shoulderItemInstances` is a new list per cape). A cloth still building reads the
  values when its build syncs its parameters. Rejected: a collider shaped like the pack (the cape would drape over it
  and hide the pack; a collider between the cape and the pack squeezes the cloth against the body's colliders, since
  the cape's particles are 8.5 cm in radius) and hiding the cape. Hair and the Hildir dresses (also MagicaCloth) are
  not held.
- Key ring (the user's request, 2026-09-28; the round pop-up was their idea): one ring cell per `Key Items` entry,
  ordinary slot cells after the purse with ids `key1`.. in list order; `SlotKind.Key` is last in the enum, and the
  record stores ids, so older records read and an older build reads `keyN` as retired. A cell takes only its own key,
  matched by prefab name. The pop-up shows only the cells holding a key (the user's call after the first in-game look:
  a key shows up when the player gets it; the brief had kept every key ever found, by the game's known list); a key
  being dragged stays in its cell until it lands, so its cell stays meanwhile. Per-frame counts compare shared names
  cached from ObjectDB, so no prefab name string is made per item.
- Key routing: an `Inventory.AddItem(ItemData)` prefix like the purse's: an empty ring cell takes the item itself, a key
  of the same world level (`IsSameType`) takes what fits up to the stack size, the rest goes on through the game (other
  stacks, then the grid). A take all puts each item at its old cell first, so `TakeAllRouting` moves the keys that came
  in and sit in main cells once it is done; from the own grave they are back in their cells already. `CanAddItem` counts
  an empty ring cell as room for its key, so auto pickup takes a key with the grid full. Layout changes: displaced keys
  try their ring cell before the grid, and keys kept in main cells move into a ring cell the new layout adds (its id not
  in the old record), so the first install and turning the ring off and on bring them back; a key a player left in the
  grid under the same ring stays there.
- Key Stack (the user chose a setting, 10, over the game's 1 on 2026-09-28): a stack size lives on the item's shared
  data, and a load cuts a stack to the prefab's maximum, so a ring-only size would lose keys at the next load. It is
  written as max(stack, Key Stack) whatever `Key Ring` and `Enabled` say, so switching those never cuts a stack, and it
  never lowers a stack: a non-key named in Key Items keeps its own. Written by OpenKeep's Stacks when OpenKeep is
  installed (as a key's starting value, before its per item entries and YAML, also with its Stacks module off), by
  `KeyStacks` otherwise (over the value each prefab had when first seen, into the prefab and every live copy).
- The pop-up: `PackPanel_keyring`, a child of the player panel hanging under the slot panel (a gap below it), centred
  under the ring button but not left of the slot panel. First placed right of the slot panel, where the crafting panel
  drew over it (seen in game 2026-09-28); under it the grid is clear, and an open chest's panel (under the player
  panel) is cleared every frame by moving the pop-up right of it when a chest wider than the grid reaches under it.
  With the brown look it is a disc (`ring_panel.png`), otherwise the game's square panel. Its cells are drawn at 0.65
  of the grid's (`KeyRingCircle.CellScale`, element `localScale`; the user asked for a much smaller ring) and sit on a
  circle (first at the top, clockwise, neighbours 1.12 scaled steps apart, radius at least 0.95) with a bronze wire
  (`ring_line.png`) through their centres and the ring's icon faint in the middle; with no key carried the wire and the
  icon stay, an empty ring. Laid out again when the grid is placed or the carried set changes. The cells are the grid's
  own elements, reparented like the slot panel's; a key shows its count alone, above one. Its background takes clicks,
  so a dragged item let go on it is not dropped.
- The ring button: an instance of the grid's element prefab in the slot panel, not in the grid's `m_elements`, its
  `UIInputHandler.m_onLeftDown` wired to `KeyRingClicks`: a toggle, or with a dragged key a call to `OnSelectedItem` on
  that key's cell (a non-key is refused with a message). (For a short while on 2026-09-28 a right click opened it;
  the user asked for the left click back the same day.) The `OnSelectedItem` prefix (`Priority.First`, before
  `DragSettle`'s slot rules) sends a dragged key let go on any ring cell to its own cell. The empty-slot hint (icon and
  "Keys") shows with Slot Labels on, the element's icon without; the game's quality text in the corner is the number
  of different keys held anywhere in the inventory; the tooltip lists them with counts, written only when it changes.
  The open state is static and never saved; `InventoryGui.Show` and `Hide` postfixes close it.
- Gamepad (the game moves by inventory cell, not by what is drawn): an `InventoryGrid.UpdateGamepad` postfix carries a
  move that lands on a hidden cell (the shut ring, a key not carried, a backpack's blocked cell, after the last slot) on
  the same way to the next shown cell, or back. With the ring shut every ring cell stands for the first, whose
  highlight is drawn on the button; A or X there (`OnSelectedItem`, `OnRightClickItem` prefixes) toggles instead of
  taking the hidden key, and opening selects the first key. B (`InventoryGui.Update` prefix, no dialog open) shuts the
  ring before the game would close the inventory, and the button is selected again.
- New keys (the user's request, 2026-09-28: "the first time you pick up a key that you didn't have before, the key
  ring's icon flashes slowly and a tooltip under it says the key went onto the key ring"). "Didn't have before" is
  read as never carried by this character: `KeyRingNews` keeps the set of key prefab names ever carried in the custom
  data `PackPanel.keysSeen` (comma separated), so keys taken back from a grave or found again after using the last one
  say nothing. A character without the record starts silently with the keys it carries; the set is also kept in memory
  for the character played (by `PlayerProfile`), so a new body after death whose data predates the last save keeps it.
  It looks at the whole inventory in the local player's frame while the ring is active, so every way in counts
  (pickup, purchase, crafting, a chest). `KeyRingNotice` then, while anything waits and the button is drawn: both key
  icons (the button's own with Slot Labels off, the hint's with them on) breathe from their hint colour to gold and
  back every 1.6 s (`Time.unscaledTime`), and `PackPanel_keynotice`, a small box in PlateColumn's `Skin.Box` with one
  line in the label colour, hangs 6 units under the button, centred, sized to its text, taking no raycasts: "{key}
  went onto your key ring", or "{n} new keys went onto your key ring". Opening the ring clears it; it is never saved,
  so a key found on the road is announced when the inventory next opens.
- Death: ring cells follow the game's rules like the purse, into the grave, also with Keep Slots On Death (the
  request said keys go to the grave with the slots). The game's own "Place stacks" button leaves the ring's cells
  alone like every slot cell's (`StackAllGuard`).
- Tacklebox (the user's request, 2026-09-28: "a tacklebox mod that performs similarly to the keyring, where slots show
  up below", crafted like the backpacks; the plan was talked through in chat the same day). The user set the boxes
  and their cells: Driftwood 1 (Wood 10, LeatherScraps 10, DeerHide 5, "a wood box covered in furs"), Finewood 2
  (FineWood 10, TrollHide 10, Bronze 2), Carapace 6 (Carapace 10, YggdrasilWood 10, "the Yggdrasil wood box coated in
  the carapace"), Flametal 8 (FlametalNew 5, AskHide 10, "flametal with the askhide covering it"); a Serpentscale box
  between them was dropped at their request. Chosen by the plan and accepted: each recipe takes the box before it (as
  for the backpacks), the Finewood box at the workbench (no metal needs a forge), bait only by default with `Tackle
  Items` for more (with one or two cells, fish would crowd the bait out), no carry-weight perk.
- The box gives cells, it stores nothing itself: the cells are ordinary cells of the player's inventory after every
  other slot (`SlotKind.Tackle`, ids `tackle1`..), as the ring cells and a backpack's slots are, so the game saves,
  weighs and entombs them and the rod finds its bait there with no patch (`Inventory.GetAmmoItem` searches the whole
  inventory). A box put in a chest keeps nothing. The box lies in the Tacklebox slot (`SlotKind.Tacklebox`), on the
  purse's row right of the ring's button (the user's call: "the tacklebox slot should go next to the key slot"). The
  count of cells comes from the box in the recorded layout's slot, as a backpack's slots do, so the two kinds are last
  in the layout: a box change moves no other slot. `TackleboxWear` re-applies the layout when the box or its cells
  change: taking the box out moves its bait into free cells and drops what does not fit (the backpacks' rule).
- Upgrade in place (the user's request, 2026-10-04: "when you upgrade a backpack, have it upgrade in place if it's
  equipped in the PackPanel slot, so you don't have to empty the inventory"). The game asks for a free main cell before
  a craft (at the button press, "Inventory full", and again when the craft finishes), puts the new item there and then
  consumes the cost, so the old pack left its slot, its rows closed and what lay in them moved or dropped. Now, when the
  recipe of a PackPanel backpack takes the item in the Backpack slot (a tacklebox's: the Tacklebox slot), the craft fits
  without a free cell and the new item goes into the slot: for the length of `DoCrafting` the old one lies off the grid
  at (-1, -1) (still counted for the cost, which the game then consumes), the emptied slot is the cell `FindEmptySlot`
  answers. A new pack with the same slots changes nothing else; more slots open more cells (a YAML pack with fewer
  closes some as a swap does). If the old one is still there afterwards (the craft stopped early, or the game consumed
  a spare copy from the grid first), it goes back into the slot, or into the freed cell when the new one is there.
  Left to the game: multi-crafting (several packs, several old ones) and crafting without cost (nothing consumed).
- The slot is a real cell, so a left click picks the box up, as on any slot: a right click (the game's use; X on a
  gamepad) opens and shuts the pop-up instead of a button, and a right click on a box in the grid puts it in the slot.
  A bait dragged onto the slot goes into the box (`TackleClicks`, like a key let go on the ring's button); with the box
  full it says so, and `DragSettle` stands down (`__runOriginal`) so no second "does not go in that slot" follows.
- Routing: bait added without a cell (pickups, a purchase, a click move) goes onto a stack of the same bait in the box,
  then into an empty cell (`TackleRouting`, like the ring's); an empty box cell counts as room (`MainCells`), so a full
  grid still picks bait up; a take all moves the bait that landed in main cells into the box afterwards
  (`TakeAllRouting`, which replaced `KeyTakeAll`). A layout that adds box cells (a box put in, a bigger box) gathers
  bait kept in the main grid into them (`LayoutMigration.GatherTackle`), so a new box fills with the bait carried.
- Which bait the rod uses: the game uses its equipped ammo, and when none is equipped takes the first bait by cell
  index, which would be stray bait in the grid. `AmmoSearch` (was `TackleAmmo`) makes the box's cells come first. A right click on a bait
  in the box equips it through the game's own ammo slot (`Humanoid.EquipItem`; the game's baits are
  `ItemType.Ammo`, not `AmmoNonEquipable`), so the rod uses it and the grid shows the game's equipped mark; the
  game counts any ammo of the same name as equipped, so the old one comes off first (a starred stack of the same bait).
- The pop-up (`PackPanel_tacklebox`) hangs where the ring's does (`PopupPlace`, shared), so opening one shuts the
  other. Every cell shows, empty ones with a "Bait" caption and a hook icon, at 0.8 of the grid's size (the ring's are
  0.65 at the user's request; bait counts need the room) in rows of four; with the slot panel's panel look.
- Death: the box follows the backpack (Keep Slots On Death keeps it, and its cells then still exist for the bait that
  went to the grave), its bait the ring cells (always to the grave). Before a take all from the own grave the box
  that was carried goes back into its slot first (`TackleboxGrave`, after `BackpackGrave`, whose waiting pack pushes the
  grave's slot rows down); Use on the grave counts the box and its bait as going back to their own cells (`GraveFit`).
- Crafting shared with the backpacks (2026-09-28, with the tackleboxes): recipes, the cost text, the item prefab and
  the bundle models moved from `Backpacks/` into `Crafting/` (`CraftedKind`, `CraftStats`, `CraftRecipes`, `CostText`,
  `CraftedItem`, `CraftedModels`, `RecipeYaml`) instead of being copied; behaviour unchanged, except the backpacks'
  icon sprites are named `PackPanel_backpack_<word>` now.
- Multiplayer: everything is on the local player's own client (inventory, equipment, status effects run there); the
  settings and the backpacks' and tackleboxes' files are synced so every client lays out the same; graves are widened
  on every client.
  No RPC. One ZDO key, `PackPanel.backpack` on the player (the worn pack's hash), written by its own client, read by
  every client to draw the pack; the packs' and boxes' prefabs are registered on every peer (a dedicated server loads
  the Linux bundles), so PackPanel is required on the server and every client (Charter's join check, `mandatory`).

### Ammo order and the food and mead keys

- Asked for on 2026-09-28: "arrows should auto equip from left to right, unless the player equips a different arrow",
  and "two hotkeys, one for food and one for mead: eat/drink anything possible from the consumables".
- Ammo: the game already equips ammo by itself; it searches only when none is equipped or the equipped stack is gone
  or does not fit the weapon (`Attack.StartDraw`/`Start`: `HaveAmmo`, `EquipAmmoItem`; `UseAmmo`), so ammo the player
  equipped is kept until it runs out. Its search takes the lowest cell index, grid cells before slot cells.
  `AmmoSearch` answers for the player's own inventory: the tacklebox's cells (bait), then the Ammo slots in layout
  order, which is left to right on the Consumables tab (`SlotPanelLayout`), then the game's search (a stray stack in
  the grid). One prefix for both, since Harmony gives no firm order between two prefixes where the first returns
  false. No setting.
- Keys: `2. Slots / Food Key` (Z) and `Mead Key` (B), per player. The game binds neither (Z flies and B builds for free
  only in its `debugmode`); the only other Z in the workspace is OpenKeep's Find Key, inventory only. They work where
  the hotbar keys do (`Player.TakeInput`), so never inside the inventory, and never with a hammer, hoe or cultivator in
  hand (`Player.InPlaceMode`; the user's request, 2026-10-04: "with the hammer equipped for building B shouldn't drink
  meads"; there B is OpenKeep's build camera and EarthWright's Select Value Key, Z EarthWright's Snap Hold Key, so both
  keys stand down), and are read with the `Hotkeys` library
  (`../ValheimModLibs/Hotkeys`, made for this from OpenKeep's `Core/Keys` rules). A press goes through the Food (or
  Mead) slots left to right and uses every item that passes the game's `CanConsumeItem(item, checkWorldLevel: true)`
  rules checked without messages (consumable, world level, `CanEat(item, false)`, no running effect of the same name
  or category), through `Humanoid.UseItem(inventory, item, fromInventoryGui: true)`: the hotbar's path (animation,
  sound, effect, food) minus feeding what the player looks at. Each check sees what the earlier items gave, so two of
  the same food eat one, and meads of one category (health, stamina) drink one. Nothing taken: a centre message.
- Food and Mead bar (the user's request, 2026-09-28: "the hotkeys for food displayed in the bottom left of the screen for
  the food and meads"): one row in the empty strip under the game's health panel (`hudroot/healthpanel` is anchored to
  the bottom-left corner at x 49.5, its bottom at y 58, read offline from the main scene; the Forsaken power sits right
  of it at y 86-150), from (50, 6) in HUD units. Since 2026-09-29 two squares and nothing else (the user: "I don't
  want 8 squares down there, I want 2"): a food square with the Food Key over it, 6 units, a mead square with the Mead
  Key over it, 42-unit copies of the game's own `food0` square (dark square, icon, corner text) showing the slot
  kind's own icon (`SlotIcons.For`) at full strength, the key in the key-hint yellow at the top-left corner where the
  hotbar shows its numbers. No items, counts or dimming. A group with no slots or no key (None) is left out. Checked
  ten times a second, rebuilt only when a group or a key changes; hidden while dead, with `Enabled` off or with the HUD
  hidden (a child of `hudroot`). Per player, `5. Look / Food And Mead Bar`.
  Eating is the local player's own action, as a hotbar key's is; nothing is sent.

## Not yet implemented

- A Ring slot (the user wants it later, compatible with other mods' rings); gamepad moves that follow the slot panel's
  picture; a worn backpack on the main menu's character (it has no ZDO) and on the ragdoll.
- The store images (header, gallery) and a Nexus page.
- The Rootbound Pack's model is not bit-for-bit reproducible: two builds of the same `model.py` differ by a few
  millimetres and sometimes one five-sided tube segment (10 triangles), since its roots and guck drips follow ray
  casts whose float results vary between runs. The other seven rebuilt with the same size, triangle count and textures. Harmless for the look, but
  a rebuild changes that pack's bundle bytes.

## Test checklist (LocalTesting profile)

Nothing here has been played through in game yet; before the move the section was only looked at through DevBridge
screenshots. Items 1 to 3 are new with the split; the rest came from OpenKeep's list (its items 46 to 78).

1. Log shows `Loading [PackPanel 0.8.1]` without failed patches, eight `... ready` lines for the backpacks, and
   `milkyteam.packpanel.cfg` with the sections `1. Inventory` to `5. Look` and `PackPanel.Backpacks.yml` are written.
   OpenKeep's own log line shows no failed patches either, and OpenKeep's cfg has no `10. Inventory` section any more.
2. Without OpenKeep (disable it in r2modman): the player panel ends just under the grid (no empty strip), no buttons;
   the stat boxes, slots, key ring and backpacks work; `Key Stack = 10`: two Swamp Keys share a cell (PackPanel writes
   the stack size itself), and `Key Stack = 1` again leaves a stack of one.
3. With OpenKeep 1.8.0: the Quick stack, Store all, Top up and Sort buttons and the trash can sit inside the inventory
   panel under the grid; `1. Inventory / Enabled = false` moves the row below the panel at once, `true` brings it back
   in. Wearing the Deerhide Satchel (a half row), `T` (sort) never puts an item into the four closed cells; `Q` at a
   chest takes nothing from the slots. `Key Stack = 10` with an `OpenKeep.Stacks.yml` entry `CryptKey: { stack: 3 }`:
   the Swamp Key stacks to 3 (OpenKeep's YAML wins); without the entry, to 10.
4. Inventory, single player, an existing character: the log shows `inventory laid out 8 x 5 with 18 slots: n items
   moved`; the grid has one more row, every item is where it was, the worn armour sits in Head, Chest, Legs and Back,
   the belt in Utility. The panels are solid brown (no grass showing through). The slot panel right of the stats
   panel opens on Gear (Head, Chest, Legs, Back, Backpack down the left, Utility x 3 down the right, the stat sheet
   between) with Coins at the bottom; Consumables shows Food x 3, Mead x 3 and Ammo x 3 in rows. Every caption is in
   large bright letters that are easy to read, and disappears when an item goes into its slot. With OpenKeep, its
   buttons use the game's vanilla appearance and hover/press states in every theme; every
   tooltip works; the trash can still destroys a dragged stack. A character from an earlier build with items in quick
   slots finds them in the grid.
5. Worn slots: drag a helmet from the grid onto Head: it is worn, the old one lands where the new one was. Drag the
   worn helmet into the grid: it comes off. Right click a worn chest piece: it comes off into a free grid cell. Right
   click a leg piece in the grid: it moves into Legs. A sword dropped on Head is refused with `That does not go in
   that slot` and stays in hand; a helmet dragged onto a sword in the grid from Head is refused too.
6. Utilities: wear the Megingjord, the wishbone and the wisplight in the three Utility slots: carry weight +150,
   the wishbone pings, the wisp follows. Take one off: its effect goes. A second Megingjord dragged onto Utility
   swaps with the worn one rather than stacking. Relog: all three are still worn with their effects.
7. Food, mead, ammo: cooked meat goes into Food, not into Mead; a mead into Mead; arrows into Ammo (picking up
   arrows tops up that stack). With FeastMaster at Food Slots 5 the food row has 5 slots; set FeastMaster to 4: within
   a frame 4, and the fifth slot's food moved into the grid.
8. Keep Slots On Death on: die wearing armour and three utilities with food and arrows in their slots: the grave
   holds only the grid's items (and the purse); after the respawn the armour and the utilities are worn, the food and
   arrows are in their slots, the log shows `kept n slot items through death (m worn)`. Off: all of it is in the grave.
   Purse: the Coins slot at the bottom left of the slot panel. Pick up coins: they land in it (the caption goes, the
   count shows "51"); sell at Haldor: the coins join the purse; buy: they leave it. With the grid full and the purse
   empty, coins can still be picked up.
9. Grid rules: with the grid full, picked up wood does not go into a slot ("inventory full"). With OpenKeep, quick
   stack and sort leave the slots alone; top up refills the Ammo slot's arrows from a chest.
10. Settings: `Inventory Width = 10`: the panel widens, the boxes and the slot panel move right, row 0's cells 9 and
    10 show no number and the hotbar still shows 8. `Inventory Rows = 4` with items in rows 5 and 6: they move to free
    cells, or drop at your feet with `No room for n items`. `Mead Slots = 0`: the Mead row goes, its meads move into
    the grid. `Enabled = false`: the game's 8 x 4 (plus bought rows), every slot item in the grid or at your feet, the
    armour still worn, two of the three utilities taken off, the game's wood panels and cells even with Brown Style
    on. On again: slots back, armour into them, brown again. (With OpenKeep its trash can returns to the stat column
    only at the next world load: OpenKeep decides that at `InventoryGui.Awake`.)
11. Death: die wearing armour and three utilities with food in the Food slots. The grave shows them in their rows.
    Take all (E on the grave): everything is back where it was and the armour and the three utilities are worn again.
    Log out before recovering and back in, with `Inventory Width = 10` and items in columns 9 and 10: the grave still
    has them. Take all from a chest whose items sit in rows 7 and 8: they land in the grid, not in slots.
12. Trader: buy an inventory row from Haldor: the grid gets a row, the slot items stay in their slots (the log line
    shows items moved), nothing drops.
13. Brown Style off: the game's wood panels and cells come back at once; on: brown again. `Slot Labels = false` hides
    the captions. `Panel Theme = Brown` and back to `Timber`: the inventory, crafting and character panels change at
    once.
14. Two clients on a dedicated server: server `Inventory Rows = 8`, client file 5: the client gets 8 rows after
    joining; server `Utility Slots = 1`: the client's second and third utilities come off and move into the grid.
    A dies with `Inventory Width = 10`; B, standing at A's grave when A logs out, loads it: all items are there when A
    comes back. Lock Configuration keeps B from changing the slot counts; B can switch Brown Style. A client without
    PackPanel is refused at the join with a screen naming the mod.
15. `Base Carry Weight = 500`: the weight box reads n/500, n/650 with the Megingjord worn; above 500 you are
    over-encumbered. 300 or `Enabled = false`: the game's 300. On a dedicated server the server's value reaches the
    client and is locked.
16. `Slots Per Group = 5`: the slot panel shows rows of five Utility, Food, Mead and Ammo slots whatever the group
    settings say; five different utilities (Megingjord, wishbone, wisplight and two modded ones) are all worn. Back to
    0: each group's own count returns, the utilities beyond `Utility Slots` come off and move into the grid.
    `Utility Slots = 5` with `Slots Per Group = 0` also wears five.
17. Backpacks, crafting: with the materials known, each pack shows at its station and level (Deerhide Satchel at the
    workbench level 2, Trollhide Backpack at the forge level 1, ..., Moosehide Pack at the black forge level 4) and
    takes exactly its cost, the pack before it included. `PackPanel.Backpacks.yml` `PackPanel_TrollhideBackpack:
    { cost: "TrollHide:5, Wood:3" }` changes the list at once; a typo like `Trolhide:5` is skipped with a warning in the
    log; `Backpacks = false` hides every recipe. Each item shows its own icon, name and description (what it gives).
18. Wearing: drag the Deerhide Satchel onto the Backpack slot: the grid gets a sixth row with its left 4 cells open and
    the right 4 dimmed with a grey cross (a click or a dropped item there does nothing), the slot panel's rows follow, nothing moves out of its slot; the pack shows on your back in
    third person, upright, snug, not through the chest, from the front, side and back, running, jumping, swimming and
    sitting. Swap it for the Trollhide Backpack (drop it on the slot): still 4 cells, carry weight +50 (the weight box
    reads n/350); the Rootbound Pack: the whole row open. Right click a pack in the grid: same. Pick up stones: they
    fill the pack's cells first; OpenKeep's sort never puts anything in the crossed cells. Look at every pack on the
    back. `Show Worn Backpack = false`: the pack leaves your back at once, for you and for a second player watching, and
    the grid keeps its rows and the weight box its carry weight; `true`: it hangs again.
19. Taking it off with its cells full and the grid full: drag it out to the grid: its items drop at your feet with the
    "No room for N items" message; with room, they move into free cells. Right click it in the slot with the grid
    full: "No room in your inventory to take the backpack off", nothing changes. Trash it from the slot (OpenKeep):
    the same as dragging it out.
20. Two clients on a dedicated server (A wears, B watches): B sees the pack on A's back at once, the new model when A
    swaps packs, and none as soon as A takes it off; after B relogs and after A relogs, still right. The server's log
    shows eight `... ready` lines (the Linux bundle loaded). A drops a pack on the ground: B sees it, picks it up, it
    keeps its icon.
21. Death wearing the Lox Hauler (12 slots, a half row), its cells and the slots full, a spare Trollhide Backpack in
    the grid: the grave holds everything; E on the grave (take all) puts the Lox Hauler on first (not the spare) and
    everything back in its cell and slot, armour worn again. With the grid otherwise full the easy fit still succeeds
    when the pack's cells make the room. With `Keep Slots On Death = true` the pack stays on, the grid does not shrink,
    its cells' items are in the grave.
22. Changes while worn: `slots: 16` for the worn pack in the YAML: the rows grow at once; back down: the bottom cells'
    items move up or drop. `Backpack Slot = false` or `Enabled = false`: the pack's cells go (items move or drop), the
    pack leaves the back, the carry weight returns. `Backpack Portal Pass = true` wearing the Moosehide Pack with
    copper ore in its cells: the portal lets you through; ore anywhere else in the grid: refused; the Asksvin Pack with
    the same ore: refused.
23. Stats panel: the inventory shows three panels left to right, a gap apart, tops and bottoms level: the grid, a
    narrow panel with armour, weight and (with Elite Creatures Reborn) the world tier boxes, the first box level with
    the first grid row, then the slot panel with the purse alone in its last row. Hover each box: its tooltip. Drop a
    dragged item on the stats panel: nothing falls on the ground. Without Elite Creatures Reborn the armour and weight
    are still boxes in the panel. `Enabled = false`: no stats panel, the column stands right of the grid.
24. Beside the minimap: closed inventory, a column of boxes right of the minimap, its top level with the map's:
    armour, weight ("26/300", red flashing over the limit) and, with Elite Creatures Reborn, the world tier ("0/7"),
    each as big as a stats panel box, the column's right edge a little in from the screen's; the map sits a little
    further left than without PackPanel, and a status effect (get wet) shows left of the map, not under it. Pick up
    stones: the weight changes at once. Open the big map: the boxes go with the minimap; close it: nothing jumps.
    `Weight Under Minimap = false`: the armour and weight boxes go, the world tier moves to the top. Without Elite
    Creatures Reborn and with the setting off, or with `Enabled = false`: the map and the status effects are back where
    the game has them.
25. Key ring, single player: `devcommands`, then `spawn CryptKey`, `spawn HildirKey_forestcrypt`, `spawn BloodGoldKey 2`
    and pick them up. The ring button right of the purse reads 3; hovering it lists the three keys, the Intricate Key
    at 2. Click it: a small round pop-up under the slot panel, below the ring button, three small cells on a bronze
    ring (the Intricate Key showing "2"), no cell for the Silver Key, the Bronze Key or the Sealbreaker. With no key
    carried it shows an empty ring.
26. Toggle: click the button again: the pop-up shuts. Open it and close the inventory; open the inventory: the ring is
    shut. Open a chest, then the ring: it covers neither the grid nor the chest panel.
27. Drag the Swamp Key out of the pop-up into the grid; shut the ring; drop the key on the button: it is back in its
    cell. Drop the Swamp Key on the Intricate Key's cell: it goes to its own. Drop a stone on the button or a ring
    cell: refused with "Only keys go on the key ring" / "That does not go in that slot".
28. Doors: with the Swamp Key on the ring, `eval $player.GetInventory().HaveItem("$item_cryptkey")` (the shared name,
    read from the item's tooltip if the token differs) is true, and the sunken crypt gate opens.
29. Routing: a chest holding a Swamp Key, take all: the key lands on the ring, not in the grid. Fill the grid, drop a
    key on the ground: it is picked up into its empty ring cell. Use up the last Intricate Key: its cell goes.
30. Death with keys on the ring (and with Keep Slots On Death on): the keys are in the grave; take all from your own
    grave: the ring holds them again.
31. Gamepad: with the ring shut, the D-pad never stops on a hidden cell (the ring cells, the cells after the last slot,
    a backpack's blocked cells); moving onto the ring's cells highlights the button; A opens it and selects the first
    key; B shuts it with the button selected; B again closes the inventory.
32. Settings: `Key Ring = false`: the keys move into the grid (what does not fit is dropped); `true` again: they are
    back on the ring. `Key Stack = 10`: two Swamp Keys share a cell and a chest cell. Dedicated server with two
    clients: change `Key Items` on the server: both clients' inventories are laid out again with the new ring.
33. Place stacks (the game's button at a chest): with cooked meat in a Food slot and in the grid, arrows in an Ammo slot
    and coins in the purse, and a chest already holding meat, arrows and coins: only the grid's meat goes in; the
    slots, the purse and the key ring keep theirs, and the weight box is right afterwards. The same with a chest another
    player has open (OpenKeep's Shared Chests = Full).
34. Tabs: the two buttons sit across the slot panel's top, level with each other, captions "Gear" and "Consumables"
    readable, the shown one lit and the other dimmed; hover and click sound as the game's buttons. Click Consumables:
    the gear, utilities and sheet go, three rows of food, mead and ammo show from the top, the purse row does not move
    and the panel keeps its size; the key ring's pop-up still hangs under the panel. Back to Gear. With an item picked
    up, click the other tab and drop it in a slot there: it lands (nothing falls on the ground). The gamepad's B, Y and
    X do nothing to the tabs (the take-all key is not on them). `Food Slots`, `Mead Slots` and `Ammo Slots` all 0:
    Consumables is greyed and the panel stays on Gear. Change the game's language: the captions stay the tabs' words.
35. Stat sheet: a dark inset with round corners, small text (12), no value out of line with its label. Health, Stamina
    and Armour match the HUD bars and the armour box; eat an eitr
    food: Eitr appears.
    Weight matches the weight box and turns red when over. Wear the wolf armour chest or drink a frost resistance mead:
    "Frost  Resistant" in green; the troll leather set or the root armour: their modifiers show. Wear the Ashlands
    armour or a mead that changes stamina use: its line with the percentage the item tooltip calls its total.
    Movement speed shows the gear's minus (e.g. -5% with an iron chest), and is +0% with no gear. Durability shows the
    most worn equipped item's percentage, red under 25%, and "-" with nothing equipped that wears out. Headings
    "Resistances" and "Gear bonuses" appear only with lines under them. A second and third worn utility change the
    modifiers they give.
36. Gamepad: from the bottom grid row, down reaches Head (Gear) and down again Food: the panel turns to Consumables and
    Food is highlighted; up again turns back to Gear. The purse and the ring button are reachable on either tab.
37. Tacklebox, crafting: the Driftwood Tacklebox at the workbench level 2 for 10 Wood, 10 Leather scraps and 5 Deer
    hide; the Finewood (workbench level 3) takes the Driftwood box, 10 Fine wood, 10 Troll hide and 2 Bronze; the
    Carapace (black forge 1) the Finewood box, 10 Carapace, 10 Yggdrasil wood; the Flametal (black forge 3) the Carapace
    box, 5 Flametal, 10 Asksvin hide. Each has its own icon, name and a description giving its cells. The log shows four
    `... Tacklebox ready` lines. `Tacklebox = false` hides every recipe and the slot.
38. The slot: the purse's row reads Coins, the key ring's button, then an empty cell captioned "Tackle" with a bronze
    box icon. Drop the Driftwood box on it: its corner reads 0/1. Right click it: a small panel under the slot panel,
    centred under the box, one empty cell captioned "Bait" with a hook. Right click again, or close the inventory:
    shut. Open the key ring: the tacklebox pop-up shuts, and the other way round. Right click a Finewood box in the
    grid: it swaps with the Driftwood box; the pop-up has two cells. With a chest open, the pop-up covers neither
    panel. A stone dropped on the slot: "That does not go in that slot".
39. Bait: `spawn FishingBait 20`, `spawn FishingBaitForest 20` and pick them up: each lands in a box cell (the corner
    reads 2/2), not the grid; a third kind with the box full goes to the grid. With the grid full, bait still goes into
    a free box cell. Drag a bait from the grid onto the Tacklebox slot with the pop-up shut: it joins its stack in the
    box; with the box full and no stack of it: "The tacklebox is full" once, and the bait stays in hand. A chest holding
    bait, take all: the bait goes into the box.
40. Fishing: with bait in the grid and other bait in the box, cast: the box's first bait is used (its count drops).
    Right click the second bait in the box: it shows the game's equipped mark; cast: that bait is used until it runs
    out, then the box's first again. Right click the marked bait: the mark goes. Two stacks of the same bait with
    different stars (GrindstoneSkills): right click the other stack: the mark moves to it.
41. Taking it out: with both cells full and the grid full, drag the box into the grid: its bait drops at your feet with
    "No room for 2 items"; with room, the bait moves into the grid. Put the box back: the grid's bait moves back into
    its cells. Swap the Finewood box for the Carapace box with bait in the grid: the four new cells fill with it.
42. Death with the Carapace box and bait in its cells: the grave holds the box and the bait; take all (E on the grave)
    puts the box back first and every bait into its cell. The same wearing the Lox Hauler: box and bait line up too.
    With Keep Slots On Death on: the box stays in its slot, the bait is in the grave, and take all puts it back into
    the cells.
43. Gamepad: with the pop-up shut the D-pad never stops on a box cell; X on the box opens it and selects the first cell;
    B shuts it and selects the box again, before it would close the inventory.
44. Settings and files: `Tackle Items = Entrails` (GrindstoneSkills' chum by default): picked up entrails go into the
    box too. `PackPanel.Tackleboxes.yml` `PackPanel_DriftwoodTacklebox: { cells: 3 }` with that box in the slot: three
    cells at once; back to 1: the bait of cells 2 and 3 moves to the grid or drops. `Tacklebox = false`: the slot and
    its cells go, the box and the bait move into the grid. Two clients on a dedicated server: B sees A's dropped box
    with its model and can pick it up; the server's `cells` reach the client; the server's log shows four `... ready`
    lines.
45. Stat sheet with Epic Loot 0.14.13 and magic gear worn: under the game's lines, headings "Epic Loot · Defence",
    "· Offence" and so on, each effect once in Epic Loot's words with the total over all worn pieces (four Mythic Deep
    North pieces showed 23 effect types: e.g. Frost Resistance 22%, Discovery Radius +13%), matching the sum of the
    items' tooltips; a long one ("Feint Increased by 18% (Health Critical)") wraps onto a second line under itself.
    Unequip a piece: its lines drop or shrink within a second. A magic utility in the second or third utility slot
    counts. The list scrolls with the mouse wheel over it, a thin bronze bar shows when it is longer than the inset.
    With an item picked up, click in the list: nothing drops. Without Epic Loot: no Epic Loot headings, no warning;
    the log shows no error either way. The stat box column between the grid and the slot panel is narrower than before
    (10 units each side of the boxes).

46. Ammo order: arrows of three kinds in the Ammo slots (Wood, Flint, Bronzehead left to right) and a stack of Fire
    arrows in the grid; nothing equipped. Draw the bow: the Wood arrows are equipped and used; when they run out the
    Flint ones are equipped, then the Bronzehead, and the grid's Fire arrows only after the slots are empty. Right
    click the Bronzehead arrows while Wood is equipped: Bronzehead is used until it runs out, then the leftmost left.
    A crossbow with bolts in the slots equips the bolts. With a tacklebox, the rod still takes the box's bait first,
    then bait in an Ammo slot.
47. Food and Mead keys, single player: three different foods and a second of the first in the Food slots, hungry:
    press Z outside the inventory: three foods are eaten (the eat animation, three food icons), the duplicate stays.
    Press Z again at once: "Nothing in your food slots can be eaten now". A health mead, a stamina mead, a second
    health mead and a frost resistance mead: B drinks health, stamina and frost, the second health mead stays. Z inside
    the inventory eats nothing (OpenKeep's Find Key works there); in chat, the console or the map nothing happens.
    Looking at a tame wolf while pressing Z: you eat, the wolf is not fed.
48. Keys and settings: `Food Key = LeftShift + Z`: Z alone does nothing, Shift + Z eats while walking with W; with the
    YAML editor open the keys do nothing. With the hammer, hoe or cultivator in hand, B and Z eat and drink nothing (B
    toggles OpenKeep's build camera only); put it away and they work again. Dedicated server with A and B: each eats from their own slots, the other sees
    the food effects and the eat animation.
53. Food and Mead bar: with the inventory shut, the bottom-left corner under the health bar shows two squares and
    nothing else, matching the game's food squares above in look: the food icon with a yellow "Z" over its top-left
    corner, then the mead icon with a yellow "B". Putting food or meads in the slots changes nothing on the bar.
    `Food Key = LeftShift + Z`: the label reads "Shift+Z"; `Mead Key = None`: the mead square goes. `Mead
    Slots = 0`: only the food group. `Food And Mead Bar = false`: gone. Hide the HUD (Ctrl+F3): gone with it. Die: gone
    until the respawn. Build mode with the hammer: the bar does not cover the key hints.
54. Ammo first: 20 Wood arrows in the first Ammo slot and 30 more in the grid. Craft 20 Wood arrows at the workbench:
    the slot's stack reads 40, the grid's stays 30. Craft Flint arrows: they go into the second Ammo slot, not the
    grid. `spawn ArrowFire 20` and pick them up: third Ammo slot. With all three Ammo slots holding other arrows, pick up
    bone arrows: they go to the grid. Fill the grid completely with the Ammo slots empty: crafting Wood arrows still
    works (no "inventory full") and they land in the first Ammo slot. Take all from a chest holding bronze arrows: they
    end up in an Ammo slot. Bait picked up still goes into the tacklebox, not the Ammo slots.

49. New key notice: a character that carries a Swamp Key (first load with this build): nothing glows. `spawn
    HildirKey_forestcrypt` and pick it up with the inventory shut; open the inventory: the ring button's key breathes
    slowly between its colours and gold, and a box under it reads "Smoke-Filled Cave key went onto your key ring" (the
    game's name for it). Pick up the Bronze Key too: "2 new keys went onto your key ring". Click the button: the ring
    opens, the glow and the box stop. Drop both keys and pick them up again: nothing. Relog: nothing. Die with the keys
    and take them back from the grave: nothing. A new character picking up its first key: it glows.

50. Stat breakdowns: with three foods eaten, hover Health on the Gear tab's sheet: a bordered tooltip "Health" lists
    Base 25 and each food with its current health, adding up to the line; Stamina the same with the game's base.
    Armour: each worn piece's armour. Weight: Carry weight Base 300, Megingjord +150 when worn, then the five heaviest
    stacks. Movement: an iron chest's -5% and any speed effect. Frost after a frost resistance mead: the mead and any
    wolf armour, each with its word. Scroll the sheet: rows scrolled out of view show no tooltip. Headings and Epic
    Loot's lines show none.

51. Weapons and shields: hold an iron sword and a banded shield: under the core lines an "IRON SWORD" section with
    Slash as a range matching the sword tooltip's yellow numbers, the stamina use (the tooltip's cost less the Swords
    skill's cut), Knockback and Backstab, but no block lines; a "BANDED SHIELD" section with Block armor matching the
    shield tooltip's yellow number, Block force and Parry bonus. Hover Slash: Iron sword, Swords <level> x0.xx-0.xx;
    hover Block armor: the shield, Blocking <level> +x%. Drop the shield: the sword's section gains Block armor and
    Parry bonus. Sheathe (R): both sections stay. A bow with wood arrows: Pierce includes the arrows' damage, the arrows
    appear in its breakdown, the draw's stamina shows per second; switch to fire arrows: Fire appears. A frost staff: Eitr
    use. Drink a mead or eat food that raises damage for the skill: its line in the breakdown and the range grows. Bare
    hands or a hammer: no section. The Resistances breakdown no longer names the shield.
52. Trinket slot: the Gear tab's right column reads Utility x 3, then an empty cell captioned "Trinket" with the
    pendant icon. `spawn TrinketBronzeHealth` and `spawn TrinketBronzeStamina`: both land in the grid. Drop the health
    trinket on Trinket: it is worn (the cell shows it equipped, the adrenaline bar appears); drop the stamina trinket on it:
    that one is worn and the health trinket lands where the stamina one was. Right click the worn one: it comes off into
    the grid. Right click one in the grid: it moves into Trinket. A belt dropped on Trinket, or a trinket on Utility, is
    refused with `That does not go in that slot`. Relog: still worn, still in the slot. Die with Keep Slots On Death
    off: the grave holds it in its slot; take all: it is worn again. `Utility Slots = 5`: the right column is six cells,
    the trinket last, and both tabs are one row taller; `Trinket Slot = false`: it moves into the grid and stays worn.
54. Capes under the pack: wear the Trollhide Backpack and the troll hide cape, third person. Standing: the cape comes out
    below the pack as before. Run and sprint forward, turn sharply, jump, stop: the cape's top never shows through the
    pack's back, top or sides, the cape stays against the legs for about 35 cm below the pack, and the rest still trails
    and swings. Sit, crouch
    and swim: no cape stuck out or jittering around the pack. The same with the Deerhide Satchel (the shortest, more cape
    swings) and the Lox Hauler, and with the wolf, lox, feather, linen, Asksvin and Deep North capes. Take the pack off
    (or `Show Worn Backpack = false`) while running: the cape flaps freely again at once. Change capes with the pack on:
    the new cape is held too. A second player watching sees the same.
55. Small inventory: `Inventory Rows = 1`: one row, the hotbar, the inventory panel one row tall with OpenKeep's
    buttons right under it, the stats and slot panels beside it at their usual height, a chest's panel just under it; what was in other rows moves to free cells or drops (`No room for n items`). `0`:
    two cells in row 0 (keys 1 and 2), the other six crossed out; wood picked up with both hands full says
    inventory full; keys 3 to 8 do nothing. Wear the Deerhide Satchel (+4): row 0's cells 3 to 6 open (keys 3 to 6
    work), 7 and 8 still crossed out; the Rootbound Pack (+8): all of row 0 and two cells of a second row, the rest
    of that row crossed out; take it off: its cells go and their items drop or move to the hands. With OpenKeep, sort and quick stack never put anything in a crossed cell.
    Dedicated server: server `Inventory Rows = 0`, client file 5: the client gets the two hand cells.
56. Night shade: at noon in clear Meadows the panels look as before. `tod 0` (midnight): within about a second the
    inventory, stats and slot panels, their cells and the Timber wallpaper of the crafting panel go darker (about two
    thirds), the item icons, numbers and captions stay bright, no purple or blue tint. `tod 0.27`: they lighten again
    as the sun comes up. `Night Shade = 1`: no change at night; `0.4`: much darker. Brown Style off: the game's own
    panels darken as the game does and PackPanel adds nothing. Swamp in rain at noon: about as dark as night.
57. BiomeLords: with BiomeLords installed and slots filled (food, mead, ammo, armour), log out and in: no CargoCrate
    appears, every slot keeps its item, the log has `inventory laid out` with the usual rows. Take the Faller Valkyrie
    blessing: the main grid grows by two rows (8 x 7 with Inventory Rows 5), the slots stay. Fill those rows, then switch
    to another blessing: their items move to free main cells, what does not fit lands in a CargoCrate at your feet with
    BiomeLords' message, the slots keep theirs. Die with Featherweight on: after respawn the two rows are still there.
    With the grid full and Featherweight on, pick up an item: it never lands in a slot it does not belong in.
58. Upgrade in place: wear the Trollhide Backpack, the whole grid and its 4 cells full (the Rootbound Pack's materials
    among them), at the forge level 2: Craft is not refused ("Inventory full" before); the
    Rootbound Pack is in the Backpack slot, the Trollhide is gone, its 4 cells keep their items and 4 more open, nothing
    dropped, the new pack on your back. The same with a spare Trollhide in the grid: one Trollhide is left, in the grid.
    Not wearing a pack: the game's own craft (needs a free cell). The tacklebox: the Finewood box over the Driftwood box
    in the Tacklebox slot, its bait kept and a second cell added. Multi-craft (hold the alt place key): the game's way.
59. Grave with a full backpack (Inventory Rows 0, the Moosehide Pack worn, every cell full, near the carry limit,
    armour worn): die, wake, press Use on the grave: everything comes back at once (no grave window), the pack on
    your back first, the armour worn again, the grave gone. The same with the Deerhide Satchel and the Lox Hauler (4
    and 12 slots, whose rows Rows 0 rounds differently). Something picked up into a slot's cell since waking: Use still
    takes all when the grid has room, else opens the grave. Another player's grave: the game's rule.
60. Crafting panel (Crafting Panel Width 160, Height 120, 16:9 screen): open a workbench: the panel reaches further
    left, stopping clear of the slot panel (about 140 more at 3840x2160 with the default inventory, the log-free clamp)
    and lower, clear of the key hints; the tabs sit above the list's left edge, their line spans the panel; the list
    and its rows are wider (names longer before they shrink, quality numbers and durability bars still on the icons,
    grid tiles bigger); the description wider and taller, its text starting below OpenKeep's Track and star buttons;
    requirements centred and the Craft row (with OpenKeep's stepper) full width. Both at 0, or PackPanel's Enabled off:
    the game's panel exactly. Change either in the cfg with the inventory open: the panel follows within seconds.
    A 16:10 window: the width shrinks to what fits.
61. Backpack as equipment (Megingjord worn throughout: it stays worn and carry weight keeps its +150, so no pack ever
    becomes the game's utility): the worn Trollhide Backpack shows the game's equipped mark in the Backpack slot;
    `GetEquippedItems` lists it. Right click it: it comes off into a cell above its rows, no mark; again: on, marked.
    Drag the Deerhide Satchel onto the slot: the satchel is marked, the Trollhide lies unmarked where the satchel was.
    Drag the worn pack into a chest: it arrives unmarked, the rows go. A pack on the hotbar, its key: after the equip
    bar it is in the Backpack slot, marked. Relog: still marked. Die: the grave holds it unmarked; take all: marked
    again. `Backpacks = false`: the mark goes, carry weight back; `true`: marked again. With Epic Loot: the enchanting
    table lists the pack, worn or not; enchant it: utility effects (carry weight, movement speed, ...); worn, the stat
    sheet's Epic Loot lines show them, taken off they go. An EliteCrafting rune on it works with Epic Loot; without
    Epic Loot a rune is refused ("not a magic base") and no pack drops as magic gear.

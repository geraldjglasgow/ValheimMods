# Changelog

## 0.2.0

- **A Trinket slot.** `2. Slots / Trinket Slot` (on): a slot under the utilities in the Gear tab for the one trinket
  the game lets you wear (the adrenaline trinkets). Drop a trinket on it to wear it, drag it out or right click it to
  take it off; equipping a trinket from the grid puts it in the slot and the old one where the new one was. A trinket
  you already wear moves into the slot on the first start, and an existing character's food, mead and ammo slots keep
  their items. `Keep Slots On Death` keeps it too, and taking all from your grave puts it back on. With five utilities
  both tabs are one row taller.
- **A Food and Mead bar.** `5. Look / Food And Mead Bar` (on, per player): in the bottom-left corner of the screen,
  under your health, the Food Key in yellow followed by what is in your Food slots, then the Mead Key followed by your
  meads, each with its count, in the look of the game's own food squares. What a press would not eat or drink right
  now (a food you already ate, a mead whose effect is running) is dimmed, and an empty slot shows its faint icon. A
  group without slots or without a key is left out; the bar hides with the HUD and while you are dead.
- **Arrows go into the Ammo slots first.** Arrows and bolts you craft, pick up, buy or take from a chest go onto a stack
  of the same arrows in an Ammo slot, then into an empty Ammo slot, and only then into the grid; a full grid still
  crafts and picks them up while an Ammo slot is free. Bait still goes into the tacklebox. With `Ammo Slots` at 0
  nothing changes.
- **Capes stay under the pack.** A backpack worn over a cape now holds the cape's top against your back and a little
  way below the pack, so running, turning and jumping no longer flap the cape out through the pack; its lower part
  still swings. The cape moves freely again as soon as the pack comes off or `Show Worn Backpack` is off, and every
  player sees the same.
- **A new Mead slot icon**: a mead bottle instead of a drinking horn.

## 0.1.0

- **First release: the player's own inventory, in a mod of its own.** A bigger grid, labelled slots for worn gear,
  utilities, food, meads and ammo, a coin purse, a key ring, a tacklebox, eight craftable backpacks, a stat sheet, stat
  boxes beside the grid and under the minimap, and a brown or timber look. It works on its own and fits together with
  OpenKeep 1.8.0 or later, which keeps the storage features. Install it on the server and on every client.
- **A bigger grid.** `1. Inventory`: 8 x 5 by default, `Inventory Width` 8 to 12 and `Inventory Rows` 4 to 10, with
  rows bought from the trader on top; the hotbar stays the first 8 cells of the top row. An existing character keeps
  every item where it was and gets the new rows. `Enabled` is a master switch that brings back the game's own
  inventory, grid and look, moving the slots' items into the grid.
- **Carry more, and keep your slots through death if you like.** `Base Carry Weight` (300, the game's) sets how much a
  player carries before Megingjord and meads add to it. `Keep Slots On Death` (off) keeps what is in the gear,
  backpack, utility, food, mead and ammo slots when you die, and what you wore is worn again when you wake.
- **Labelled slots, always on screen, on two tabs.** `2. Slots`: a panel right of the grid with Gear and Consumables
  buttons across its top. Gear has Head, Chest, Legs, Back and Backpack down the left, where the armour you wear sits
  (drop a piece on its slot to wear it, drag it out to take it off), and the Utility slots down the right. Consumables
  has a row each of Food, Mead and Ammo. An empty slot shows a bronze icon and its name in large letters.
- **Up to five utilities worn at once.** `Utility Slots` (3, up to 5): a belt, the wishbone and the wisplight together,
  each with its effects, set bonus, eitr regen and modifiers. Two of the same item do not stack.
- **Food, mead and ammo slots.** `Food Slots`, `Mead Slots` and `Ammo Slots`, 0 to 5 each. The food slots follow how
  many foods you can eat (`Food Slots Follow Eating`: FeastMaster's Food Slots, or the game's 3), and `Slots Per Group`
  sets utilities, food, mead and ammo to one number. A slot takes only its kind of item.
- **Ammo comes from the slots, left to right.** With no arrows equipped, the bow takes the leftmost Ammo slot's arrows,
  then the next slot's when they run out, before any arrows in the grid; arrows you equip yourself are used until they
  run out. Bolts and missiles work the same way.
- **Eat or drink with one key.** `Food Key` (Z) and `Mead Key` (B), per player: outside the inventory, eat every food in
  the Food slots you can eat right now, or drink every mead in the Mead slots you can drink (one of each kind), left
  to right.
- **A stat sheet beside your gear.** Between the Gear tab's columns: max health, stamina and eitr, armour, weight out
  of carry weight, movement speed, the lowest durability of what you have equipped, every damage type you resist or
  are weak to, every other gear modifier that is not zero, and a section for each weapon or shield in hand (damage per
  type as the range one hit does at your skill, attack costs, knockback, backstab, block armour, block force, parry
  bonus). With Epic Loot installed it also lists every magic effect you wear, totalled, grouped and in Epic Loot's own
  words. It scrolls with the mouse wheel, and hovering a line shows what makes it up: each food, armour piece, belt,
  effect, item or skill.
- **A coin purse.** `Coin Purse` (on): a Coins slot at the bottom of the slot panel, under both tabs. Coins you pick
  up, get from a trader or take from a chest go into it first; traders take coins from it as from anywhere in the
  inventory.
- **A key ring.** `3. Key Ring`: a button right of the purse shows how many different keys you carry, with the list
  and counts on hover. A click opens a small round pop-up under the slot panel with a cell for every key you carry.
  Keys you pick up, make or take from a chest, grave or trader go onto the ring first, and doors still find them there.
  A key you have never carried before makes the button glow, with a note under it, until you open the ring. `Key
  Items` sets which keys it holds (the game's six by default) and `Key Stack` (10) how many of one key stack, on the
  ring and in chests.
- **A tacklebox for your bait.** `6. Tacklebox`: a Tacklebox slot right of the key ring's button, and four craftable
  boxes, each recipe taking the one before: Driftwood (1 cell, workbench), Finewood (2, workbench), Carapace (6, black
  forge) and Flametal (8, black forge), each with its own model and icon. Right click the box in its slot for a pop-up
  of its cells; the slot's corner shows how many are in use. Bait you pick up, buy or take from a chest goes into the
  box first, bait dropped on the slot goes in too, and the fishing rod takes its bait from the box first; right click a
  bait in the box to fish with that one. Taking the box out moves its bait into the grid and drops what does not fit.
  `Tackle Items` adds other items a box takes (chum, lures), and `PackPanel.Tackleboxes.yml` sets each box's station,
  level, cost and cells.
- **Eight craftable backpacks, worn on your back.** `4. Backpacks`: one per biome, from the Deerhide Satchel (Meadows)
  to the Moosehide Pack (Deep North), each with its own model and icon, each recipe taking the pack before it. In the
  Backpack slot a pack hangs on your back for every player to see, adds slots to the bottom of your grid and adds
  carry weight; slots and carry weight take turns, from +4 slots up to +16 slots and +200 carry weight, and the cells a
  half row does not open are crossed out. Taking a pack off moves what its slots hold into free cells and drops what
  does not fit. `PackPanel.Backpacks.yml` sets each pack's station, level, cost, slots and carry weight; `Backpack
  Portal Pass` (off) lets what lies in the Moosehide Pack's slots through portals; `Show Worn Backpack` (per player)
  hides your pack from view. The Backpack slot also holds other mods' backpacks (`Backpack Items`), without wearing
  them.
- **Your stats in boxes, also under the minimap.** Armour, weight and Elite Creatures Reborn's world tier sit in small
  boxes in a panel of their own between the grid and the slot panel, and armour and weight also show under the minimap
  (`Weight Under Minimap`, per player). Weight reads what you carry over what you can carry, flashing red when you are
  over.
- **A brown or timber look.** `5. Look`, per player: `Brown Style` gives the inventory, chest and slot panels plain
  dark brown with bronze frames, recessed cells and bronze slot icons (`Slot Labels` shows the slot names). `Panel
  Theme` Timber, the default, gives the inventory and the game's other wood panels timber borders over one painted
  background (`Timber Border Width`, `Timber Border Jaggedness`); Brown keeps the plain brown panels.
- **The slots stay out of the way.** Pickups, crafting and purchases fill only the grid; the game's own Place stacks
  at a chest leaves the slots alone; taking all from a chest never fills a slot; taking all from your own grave puts
  your pack and tacklebox back first, then everything where it was, armour worn, keys on the ring and bait in the box.
  A grave keeps items from columns beyond 8 when it loads again, and a chest wider than 8 widens the chest panel.
- **With OpenKeep 1.8.0 or later.** Its button row and trash can sit inside the inventory panel in Valheim's own button
  look; quick stack, store all, dump, sort and a shared chest's stack all work on the whole grid, a backpack's rows
  included, and never touch the slots, the key ring or the tacklebox. OpenKeep writes `Key Stack` with its own stack
  sizes, so an `OpenKeep.Stacks.yml` entry for a key wins.
- **Server synced.** Every gameplay setting and both YAML files come from the server and are locked by `Lock
  Configuration`; the look, the Food and Mead keys and `Show Worn Backpack` are each player's own. The YAML files hot
  reload and can be edited in game through Configuration Manager.

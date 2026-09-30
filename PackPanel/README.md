# PackPanel

A bigger, better organised player inventory for Valheim: a wider and taller grid (8 x 5 by default), labelled slots for
your armour, a backpack, three worn utilities, a trinket, food, meads and ammo on two tabs (Gear, with a sheet of your stats, and
Consumables), Epic Loot's magic effects in that sheet when it is installed, a coin purse, a key ring, a tacklebox
slot whose four craftable boxes hold your fishing bait, eight craftable backpacks (one per biome) that you wear on your
back, your stats in their own boxes and beside the minimap, and a plain brown or timber look for the inventory and the
game's other wood panels. Every gameplay setting is server synced and lockable; the look is each player's own. Works on
its own, and fits together with OpenKeep (storage: craft from chests, quick stack, sort, trash, salvage) when both are
installed.

### The inventory
Section `1. Inventory`. `Enabled` is the master switch: off, you get the game's own inventory back (its 8 x 4 grid
plus bought rows, no slots, no purse, no key ring, no backpacks, the game's wood look), every other PackPanel setting is
ignored, and the slots' items move into the grid or drop at your feet.

On the first start an existing character keeps every item where it was and gets the new rows; the armour, utility and
trinket you wear move into their slots. Items another inventory mod kept below the game's rows are moved into the new grid (see
Warnings).

- A bigger grid: `Inventory Width` (8, up to 12) columns and `Inventory Rows` (5, from 4 up to 10) rows. Rows bought
  from the trader come on top. The hotbar stays the first 8 cells of the top row, keys 1 to 8.
- Carry weight (`Base Carry Weight`, 300): how much you carry before you are over-encumbered. Megingjord and meads
  still add to it, and the world's carry weight modifier still scales it. 300 is the game's.
- Keep Slots On Death (`Keep Slots On Death`, off): what is in the gear, backpack, utility, trinket, food, mead and ammo
  slots stays with you when you die instead of going into your grave, and the armour, utilities and trinket you wore
  are worn again when you wake. The coin purse, the key ring and the grid follow the game's rules: keys go to your grave.

### Slots
Section `2. Slots`. The slots sit in a panel right of the stat boxes, on two tabs you pick with the buttons across its
top. **Gear**: Head, Chest, Legs, Back and Backpack down the left, the utilities and under them the trinket down the
right, and a sheet of your stats between them. **Consumables**: a row each of food, mead and ammo. The coin purse and
the key ring's button stay at the bottom under both tabs. An empty slot shows its name in large letters, an item in it
covers the name. With a gamepad, moving onto a slot of the other tab turns to that tab.

- Ammo: with no arrows equipped, drawing the bow takes the arrows in the Ammo slots, leftmost first; when they run out
  the next slot's are equipped, and arrows loose in the grid only once the slots are empty. Arrows you equip yourself
  (right click) are used until they run out, then the slots take over again. Bolts and bait the same way.
- `Food Key` (Z) and `Mead Key` (B), per player: outside the inventory, eat every food in your Food slots that you can
  eat right now, or drink every mead in your Mead slots that you can drink (one of each kind: a health mead and a
  stamina mead, not two health meads), left to right. The game's own rules decide, as for a hotbar key; when nothing
  can be taken the centre message says so.
- Food and Mead bar (`5. Look / Food And Mead Bar`, on, per player): in the bottom-left corner of the screen, under
  your health, two squares: a food icon with the Food Key over it and a mead icon with the Mead Key over it, in the
  yellow of the game's key hints. A group without slots or without a key is left out, and the bar hides with the HUD
  and while you are dead.

- Stat sheet (Gear tab), your totals in the game's own words: max health, stamina and eitr (once you have any),
  armour, weight carried out of what you can carry (red when over), movement speed, and the lowest durability of
  anything you have equipped (red under 25%). Below that, every damage type you resist (green) or are weak to (red),
  from armour and from meads or other effects, such as Frost: Resistant; every other modifier your gear gives that is
  not zero (stamina use for running, jumping, attacking, blocking, dodging, swimming and sneaking, heat resistance,
  adrenaline); for each weapon or shield in your hands (sheathed too), the damage one hit does by type as a range at
  your skill (arrows and meads included), what an attack costs in stamina, eitr or health, knockback and backstab,
  and for what you block with its block armour at your Blocking skill, block force, parry bonus and what it resists
  while blocking; and with Epic Loot installed every magic effect you wear, once each with its total, in Epic Loot's own
  words, grouped into offence, defence, health and stamina, movement, skills and other. Scroll it with the mouse
  wheel. Every worn utility counts. Hover a line to see what makes it up: health and stamina by base and each food,
  armour by piece, carry weight by base, belt and effects (and your heaviest stacks), movement, durability,
  resistances and the other modifiers by item and effect, a weapon's damage by weapon, arrows, effects and skill; what other mods add shows as "Other effects".

- Head, Chest, Legs, Back (`Equipment Slots`, on): the armour you wear sits in its slot. Drop a piece on its slot to
  put it on, drag it out (or right click it) to take it off. Equipping a piece from the grid puts it in its slot and
  the old piece where the new one was.
- Backpack (`Backpack Slot`, on): wears one of PackPanel's backpacks (below), or holds a backpack from another mod:
  any item whose prefab name contains "backpack", or one listed in `Backpack Items`. It does not equip those.
- Utility x 3 (`Utility Slots`, 0 to 5): every utility item in a slot is worn at the same time (a belt, the wishbone
  and the wisplight together), with its effects, set bonus, eitr regen and movement changes. Two of the same item do
  not stack. Only the first shows on your character.
- Trinket (`Trinket Slot`, on): under the utilities. The game wears one trinket at a time, and this is it: drop a
  trinket on the slot to wear it, drag it out (or right click it) to take it off; equipping another trinket from the
  grid puts it in the slot and the old one where the new one was.
- Food (`Food Slots Follow Eating`, on: as many as the foods you can eat at once, FeastMaster's `Food Slots` when it is
  installed, 3 otherwise; off: `Food Slots`), Mead (`Mead Slots`, 3) and Ammo (`Ammo Slots`, 3, arrows, bolts, missiles
  and bait), each from 0 to 5. A slot takes only its kind of item.
- One number for all four (`Slots Per Group`, 0): 1 to 5 gives Utility, Food, Mead and Ammo that many slots each, over
  their own settings above; 0 lets each group use its own.
- Coins (`Coin Purse`, on): the purse, at the bottom of the slot panel under both tabs. Coins you pick up, get from a
  trader or take from a chest go into it first. Traders take your coins from it like from anywhere in the inventory.

Picked up items, crafted items and purchases go into the grid, coins into the purse, keys onto the ring and arrows and
bolts into the Ammo slots first (onto a stack of the same arrows, then an empty Ammo slot, so a full grid still takes
them); food and coins stack onto what a slot already holds. The game's own Place stacks at a chest works on the grid
only, never the slots or the ring. Death: your armour stays in its slots in the grave. Taking all from your own grave
puts everything back where it was and your armour back on, keys back on the ring. Taking all from a chest fills the
grid, never the other slots; its keys go onto the ring and its arrows into the Ammo slots. A grave keeps items from
columns beyond 8 when it loads again, and a chest wider than 8 columns widens the chest panel to match. Changing a setting lays the inventory out again at once:
items keep their slot, and whatever no longer has a place moves into a free cell of the grid, or drops at your feet
when the grid is full.

### Key ring
Section `3. Key Ring`. `Key Ring` (on): a button right of the purse with a key ring on it and, in its corner, how many
different keys you carry. Hover it to list them and how many of each. Click it for a small round pop-up under the slot
panel: a ring with a small cell for every key you carry hung on it, or an empty ring while you carry none; click again,
or close the inventory, to shut it. Keys you pick up, make, or take from a chest, grave or trader go onto the ring
first, into their own cell, and doors find them there as anywhere else in the inventory. Drag a key out like from any
cell; drop a key on the ring button or on any ring cell and it goes back into its own. A key's cell goes when you use
or move away its last one. With a gamepad, move onto the ring button and press A to open it; B shuts it again. A key you have never carried before makes the
ring button's key glow slowly, with a small note under it ("Swamp Key went onto your key ring") until you open the ring;
keys you had before, such as the ones you take back from your grave, say nothing.

`Key Items` lists what the ring takes, one cell each, in order: the game's six keys by default (Hildir's Brass Key, the
Swamp Key, Hildir's Silver and Bronze Keys, the Sealbreaker and the Intricate Key). `Key Stack` (10, up to 100; the game
has 1) is how many of one key stack, on the ring and in chests; keys of different world levels never stack, a second
world level goes to the grid.

### Backpacks
Section `4. Backpacks`. `Backpacks` (on): eight packs, one per biome. Put one in the Backpack slot, by dragging it there
or right clicking it: it hangs on your back, seen by every player, adds its slots to the bottom of your grid and adds
its carry weight. Worn over a cape, the pack holds the cape's top against your back, so running no longer flaps it out
through the pack; the cape is held a little way below the pack too, and its lower part still swings. Slots fill the
bottom row from the left; a half row keeps its other cells closed. Right click it in the slot, or drag it out, to take
it off: what its slots hold moves into free cells of the grid, and what does not fit drops at your feet. If you die
wearing one, taking all from your grave puts it back on first, so everything goes back where it was. Needs
`2. Slots / Backpack Slot`.

Slots and carry weight take turns, and each pack's recipe takes the one before it, so you upgrade rather than collect:

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
`Backpack Portal Pass` (off): what lies in the Moosehide Pack's slots (any pack marked `portal: true` in the file) goes
through portals, ores and metals included; the rest of your inventory still follows the game's rule.
`Show Worn Backpack` (on, your own choice, not synced): off, your pack is invisible on your back, to you and to every
other player, and still gives its slots and carry weight. A pack that does not fill its last row shows the cells it
does not open dimmed, with a grey cross.

### Tacklebox
Section `6. Tacklebox`. `Tacklebox` (on): a Tacklebox slot on the purse's row, right of the key ring's button. Put a
tacklebox in it, by dragging it there or right clicking it in your grid, then right click it in the slot for a small
pop-up under the slot panel with the box's cells; right click it again, or close the inventory, to shut it. The slot's
corner shows how many of the box's cells are in use. Fishing bait you pick up, buy or take from a chest goes into the
box first, and a bait dropped on the Tacklebox slot goes into the box too, open or shut. The fishing rod takes its bait
from the box first, in cell order; right click a bait in the box to fish with that one (the game marks it as equipped),
and again to let the rod choose. Take the box out of its slot and what its cells hold moves into free cells of the
grid, and what does not fit drops at your feet. If you die with one, taking all from your grave puts it back first, so
the bait goes back into its cells. With a gamepad, press X on the box to open it and B to shut it.

`Tackle Items` (empty): prefab names of anything else a box should take, comma separated (chum, another mod's lures).

Each box's recipe takes the one before it, and each is bigger than the one before, with its own model: a small
driftwood chest with a rounded lid under a deer pelt, a fine wood box on turned feet, a yggdrasil box plated with
carapace on chitin claws, and a flametal chest on heavy glowing feet.

| Biome | Tacklebox | Crafted at | Cost | Cells |
| --- | --- | --- | --- | --- |
| Meadows | Driftwood Tacklebox | Workbench, level 2 | 10 Wood, 10 Leather scraps, 5 Deer hide | 1 |
| Black Forest | Finewood Tacklebox | Workbench, level 3 | Driftwood Tacklebox, 10 Fine wood, 10 Troll hide, 2 Bronze | 2 |
| Mistlands | Carapace Tacklebox | Black forge, level 1 | Finewood Tacklebox, 10 Carapace, 10 Yggdrasil wood | 6 |
| Ashlands | Flametal Tacklebox | Black forge, level 3 | Carapace Tacklebox, 5 Flametal, 10 Asksvin hide | 8 |

Every box's station, level, cost and cells can be changed in `PackPanel.Tackleboxes.yml` (below).

### Stat boxes and the look
Section `5. Look`, each player's own.

- Stat boxes: armour, weight and Elite Creatures Reborn's world tier (and any other mod's box in the game's stat
  column) sit as small boxes in a narrow panel of their own between the inventory and the slot panel, each with its
  tooltip. Weight reads what you carry over what you can carry, here and beside the minimap, flashing red when you are
  over.
- Beside the minimap (`Weight Under Minimap`, on): your armour and weight in boxes in a column right of the minimap,
  above Elite Creatures Reborn's world tier when it is installed, so you see them without opening the inventory. The
  minimap and the status effect icons move a little to the left to make room.
- Food and Mead bar (`Food And Mead Bar`, on): a food square with the Food Key over it and a mead square with the Mead
  Key over it, in the bottom-left corner under your health (see Slots above).
- Brown look (`Brown Style`, on): the inventory, chest and slot panels in plain dark brown with a bronze frame, dark
  recessed cells and bronze icons with the slot's name in every empty slot. Off: the game's wood. `Slot Labels` (on)
  shows the slot names.
- Panel Theme (`Panel Theme`, `Timber` or `Brown`; Timber by default): Timber gives the inventory, the key ring, the
  crafting panel, the character dialogs, the settings and the game's other wood panels chopped timber borders over one
  painted background that stays aligned across the screen, so a larger panel shows more of it rather than stretching
  it; their controls stay the game's. `Brown` keeps the plain brown panels above. `Timber Border Width` (5.5, 3 to 8)
  and `Timber Border Jaggedness` (1.5, 0 to 2.5) tune the wood edge and apply at once. With `Brown Style` off the game's
  own wood comes back whatever the theme.

### With OpenKeep
OpenKeep (storage: craft from chests, quick stack, sort, trash, salvage, bigger chests) is a separate mod; PackPanel
needs none of it. With both installed (OpenKeep 1.8.0 or later) they fit together by themselves:

- OpenKeep's buttons (Quick stack, Store all, Top up, Sort and the trash can) sit inside the inventory panel, under the
  grid, in Valheim's own button look whatever the theme.
- Quick stack, store all, dump, sort and a shared chest's stack all work on the whole grid, a backpack's rows included,
  and never touch the slots, the key ring or the tacklebox; top up still refills the food and ammo in the slots.
- `Key Stack` is written by OpenKeep's stack sizes like any other, so an `OpenKeep.Stacks.yml` entry for a key wins.

An older OpenKeep knows nothing of PackPanel: its buttons stay under the panel, and its quick stack and sort use only
the game's four rows (plus bought ones), so they leave the slots alone but also skip PackPanel's extra rows.

### With other mods
- **Elite Creatures Reborn**: its world tier box joins the stats panel and the column beside the minimap. From version 3.12.0
  on, its Thieving mutation takes only from your grid, never from PackPanel's slots, key ring or tacklebox.
- **FeastMaster**: with `Food Slots Follow Eating` on, the food slots follow FeastMaster's `Food Slots` and change with
  it.
- **Epic Loot**: the stat sheet lists every magic effect you wear, in Epic Loot's own words, read through the API it
  publishes for other mods (tested with 0.14.13). Magic utilities in the second to fifth Utility slots count.
- **GrindstoneSkills**: starred bait sits in the tacklebox like any bait, and right clicking the other stack moves the
  rod to it. For chum, add its items to `Tackle Items` (such as `Entrails`).
- **Configuration Manager** (optional): change settings in game and open the YAML editor (`Edit backpacks`, `Edit
  tackleboxes`).
- **Other inventory mods** that change the inventory's size or add equipment slots do not work together with PackPanel
  (see Warnings).

### Hotkeys (per player, not synced)

| Setting | Default | Does |
| --- | --- | --- |
| `2. Slots / Food Key` | Z | outside the inventory: eat every food in the Food slots you can eat now, left to right |
| `2. Slots / Mead Key` | B | outside the inventory: drink every mead in the Mead slots you can drink now, one of each kind |

They work where the hotbar keys do: never with the inventory, chat, the console, the map or the YAML editor open. A key
with a modifier (`LeftShift + Z`) fires while you walk; other modifiers held stop it. The game uses neither key
outside its debug mode, and OpenKeep's Find Key (also Z) works only inside the inventory, so the two never clash. The
Food and Mead bar (`5. Look / Food And Mead Bar`) shows both keys on screen, as you set them.

### Config file
`BepInEx/config/milkyteam.packpanel.cfg`, written on first start. Sections: `1. Inventory` (`Enabled`, `Inventory
Width`, `Inventory Rows`, `Base Carry Weight`, `Keep Slots On Death`), `2. Slots` (`Equipment Slots`, `Backpack Slot`,
`Backpack Items`, `Slots Per Group`, `Utility Slots`, `Trinket Slot`, `Food Slots`, `Food Slots Follow Eating`, `Mead
Slots`, `Ammo Slots`, `Coin Purse`; per player `Food Key`, `Mead Key`), `3. Key Ring` (`Key Ring`, `Key Items`, `Key
Stack`), `4. Backpacks` (`Backpacks`, `Backpack Portal Pass`; per player `Show Worn Backpack`), `5. Look` (per player:
`Slot Labels`, `Brown Style`, `Weight Under Minimap`, `Food And Mead Bar`, `Panel Theme`, `Timber Border Width`,
`Timber Border Jaggedness`), `6. Tacklebox` (`Tacklebox`, `Tackle Items`), and `General / Lock Configuration`. Every entry has a description in the
file. Gameplay settings are synced from the server and locked; the look, the two keys and `Show Worn Backpack` are
yours.

### YAML files
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

### Server settings
Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses their
own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff` lists
where the server's values differ from your own file, and `charter versions` lists the mods on both sides. Joining with
a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a refusal code that
is also written to the server's and your own log.

### Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod. On a server, install it on the server and on every client: the backpacks and tackleboxes are
   items every peer must know, and a client without the mod is refused at the join with a screen naming it.

For manual install, drop `PackPanel.dll` into `BepInEx/plugins`. The config file and the YAML files are created on the
first run. A server pushes its gameplay settings and YAML files to every client; with `Lock Configuration` on (the
default) clients cannot change them while connected.

### Warnings
- The slots and the extra rows are part of your inventory. Without PackPanel the game drops every item outside its own
  rows at your feet when you log in, and items in columns beyond 8 are lost; backpacks and tackleboxes you carry or
  store are gone. Before removing the mod, or lowering `Inventory Width`, empty those columns; turning
  `1. Inventory / Enabled` off moves the slots' items into the grid.
- Taking off a backpack or a tacklebox, or lowering a count (`Inventory Rows`, a slot group, a pack's `slots` or a
  box's `cells`), drops at your feet whatever no longer finds a free cell.
- Other mods that change the player inventory's size or add equipment slots (ExtraSlots, Equipment and Quick Slots,
  Extended Player Inventory and the like) do not work together with PackPanel; turn one of them off.
- Lowering `Key Stack`, or removing the mod, loses the part of a stack of keys above the new maximum when the inventory
  that holds it loads. Split large stacks first.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` folder next to this one.
`pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build PackPanel/PackPanel.csproj -c Release
```

### Bugs and feature requests
The source is open, at the repository linked from the store page (`website_url` in `thunderstore/manifest.json`):
https://github.com/geraldjglasgow/ValheimMods. Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.

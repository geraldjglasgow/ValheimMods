# Configuration

Settings are in `BepInEx/config/milkyteam.packpanel.cfg`. Backpack and tacklebox recipes and sizes are in `PackPanel.Backpacks.yml` and `PackPanel.Tackleboxes.yml` beside it. Changes apply without a restart.

## On a server

- With `Lock Configuration` on (the default), every player uses the server's settings and YAML files.
- Admins and the host can change the server's values from their own game, for example with a configuration manager.
- Settings marked "per player" stay each player's own.

## Settings

### General

| Setting | Default | Effect |
| --- | --- | --- |
| `Lock Configuration` | true | Server only: players use the server's settings. |

### 1. Inventory

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | true | Master switch; off brings back the game's own inventory. |
| `Inventory Width` | 8 (8-12) | Columns of the grid. |
| `Inventory Rows` | 5 (0-10) | Rows of the grid; 0 leaves only your two hand cells. |
| `Base Carry Weight` | 300 (50-10000) | Carry weight before Megingjord, effects and backpacks. |
| `Keep Slots On Death` | false | Slot items, your backpack and your tacklebox stay with you when you die. |

### 2. Slots

| Setting | Default | Effect |
| --- | --- | --- |
| `Equipment Slots` | true | Head, Chest, Legs and Back slots; off, armour is worn from the grid. |
| `Utility Slots` | 3 (0-5) | Utility slots, all worn at once; 0 wears one utility, as in the game. |
| `Trinket Slot` | true | A slot for your worn trinket. |
| `Backpack Slot` | true | The Backpack slot; PackPanel's backpacks need it. |
| `Backpack Items` | empty | Other mods' items the Backpack slot accepts, as prefab names. |
| `Slots Per Group` | 0 (0-5) | Gives Utility, Food, Mead and Ammo this many slots each; 0 uses their own settings. |
| `Food Slots` | 3 (0-5) | Food slots when `Food Slots Follow Eating` is off. |
| `Food Slots Follow Eating` | true | As many Food slots as foods you can eat at once (FeastMaster aware). |
| `Mead Slots` | 3 (0-5) | Slots for meads and potions. |
| `Ammo Slots` | 3 (0-5) | Slots for arrows, bolts and bait. |
| `Coin Purse` | true | A purse slot for coins. |
| `Food Key` | Z | Per player: eats from the Food slots; not with a hammer, hoe or cultivator in hand. |
| `Mead Key` | B | Per player: drinks from the Mead slots; not with a hammer, hoe or cultivator in hand. |
| `Mead Slot 1 Key` to `Mead Slot 5 Key` | `Alpha1 + LeftAlt` to `Alpha5 + LeftAlt` | Per player: drinks the mead in that Mead slot (left to right); not with a hammer, hoe or cultivator in hand. A key for a slot you do not have does nothing. |

### 3. Key Ring

| Setting | Default | Effect |
| --- | --- | --- |
| `Key Ring` | true | The key ring for your keys. |
| `Key Items` | the game's six keys | Keys the ring holds, as prefab names. |
| `Key Stack` | 10 (1-100) | How many of one key stack together; lowering it loses keys in bigger stacks. |

The default `Key Items`: `HildirKey_forestcrypt,CryptKey,HildirKey_mountaincave,HildirKey_plainsfortress,DvergrKey,BloodGoldKey`.

### 4. Backpacks

| Setting | Default | Effect |
| --- | --- | --- |
| `Backpacks` | true | PackPanel's eight craftable backpacks. |
| `Backpack Portal Pass` | false | Items in the Moosehide Pack go through portals. |
| `Show Worn Backpack` | true | Per player: shows your backpack on your back. |

### 5. Look (all per player)

| Setting | Default | Effect |
| --- | --- | --- |
| `Slot Labels` | true | Icon and name on each empty slot. |
| `Brown Style` | true | PackPanel's panel look instead of the game's wood. |
| `Panel Theme` | Timber | Timber or Brown panels. |
| `Timber Border Width` | 5.5 (3-8) | Width of the Timber edge. |
| `Timber Border Jaggedness` | 1.5 (0-2.5) | How rough the Timber edge is. |
| `Night Shade` | 0.65 (0.3-1) | Panel brightness at midnight; 1 never darkens. |
| `Weight Under Minimap` | true | Armour and weight boxes beside the minimap. |
| `Food And Mead Bar` | true | Shows the Food and Mead keys under your health bar, then each Mead slot's mead with its Mead Slot key. |
| `Crafting Panel Width` | 100 (0-600) | Extra width for the crafting panel and the panel above it (the game's are 570); 0 is the game's width. |
| `Crafting Panel Height` | 90 (0-400) | Extra height for the crafting panel (the game's is 650); 0 is the game's height. |

### 6. Tacklebox

| Setting | Default | Effect |
| --- | --- | --- |
| `Tacklebox` | true | The Tacklebox slot and the four craftable tackleboxes. |
| `Tackle Items` | empty | Items a tacklebox holds besides bait, as prefab names. |

## The YAML files

Each entry changes one backpack or tacklebox. Every key is optional; one left out keeps its default.

| Key | For | Values | Effect |
| --- | --- | --- | --- |
| `station` | both | `piece_workbench`, `forge`, `blackforge`... | Where it is crafted. |
| `level` | both | 1-10 | Station level needed. |
| `cost` | both | `prefab:amount` pairs | What it costs. |
| `slots` | backpacks | 0-40 | Cells it adds. |
| `carry` | backpacks | 0-1000 | Carry weight it adds. |
| `portal` | backpacks | true/false | Its items pass portals while `Backpack Portal Pass` is on. |
| `cells` | tackleboxes | 0-20 | Bait cells it gives. |

Item names: `PackPanel_DeerhideSatchel`, `PackPanel_TrollhideBackpack`, `PackPanel_RootboundPack`, `PackPanel_WolfpeltPack`, `PackPanel_LoxHauler`, `PackPanel_CarapacePack`, `PackPanel_AsksvinPack`, `PackPanel_MoosehidePack`; `PackPanel_DriftwoodTacklebox`, `PackPanel_FinewoodTacklebox`, `PackPanel_CarapaceTacklebox`, `PackPanel_FlametalTacklebox`.

```yaml
backpacks:
  PackPanel_DeerhideSatchel: { slots: 8 }
  PackPanel_MoosehidePack: { carry: 250 }
```

## Console command

PackPanel shares the `charter` command with the other MilkyTeam mods.

| Command | Shows |
| --- | --- |
| `charter` or `charter status` | Whether the server's settings apply to you, for each mod. |
| `charter diff [mod]` | Settings where your value differs from the server's. |
| `charter versions` | Mod versions on your side and the server's. |

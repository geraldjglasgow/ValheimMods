# Store and Sort

Section `2. Store`: buttons and keys to move items between you and chests, sort, trash and pick up drops. Buttons (`Quick stack`, `Store all`, `Top up`, `Sort`) sit under the inventory; the trash can sits under the armour readout. With PackPanel, both sit inside its panel.

## Moving items

"Nearby" means within `Nearby Range` of you. Keys work with the inventory open, except Dump.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Master switch for this section. |
| `Nearby Range` | 20 m | Server | Reach of quick stack, top up, dump, route and find (1 to 100). |
| `Quick Stack Nearby` | on | Server | Quick stack also fills nearby chests, not only the open one. Needed for Dump. |
| `Quick Stack Key` | Q | Player | Move every stack to chests that already hold that item. |
| `Store All Key` | G | Player | Move everything that may move into the open chest. |
| `Take All Key` | Shift + G | Player | Take everything from the open chest. |
| `Top Up Key` | R | Player | Fill your partial stacks from the open and nearby chests. |
| `Dump Key` | Left Alt + D | Player | Quick stack to nearby chests without opening the inventory. |
| `Route Modifier` | Left Ctrl | Player | Ctrl + click a stack: send it to the nearest chest holding that item with room for it, else the open chest, else a chest with the same group or that accepts it. Full chests are passed over. |
| `Store One Key` | V | Player | Send one of the hovered item to the open chest, or the nearest chest holding it. |
| `Find Key` | Z | Player | Point to every nearby chest holding the hovered item. |
| `Button Row Offset` | 0 | Player | Move the button row up or down, in pixels. |
| `Trash Can On Stat Column` | on | Player | Off: the trash can joins the button row. |
| `Main Inventory Rows` | 0 | Player | Rows these actions use. 0: the game's rows. Set it for mods that add ordinary rows. |

When a chest is full, the rest goes to the next nearest chest holding the item; with Ctrl + click, whatever is still left then goes into the open chest. Quick stack, store all and dump leave the hotbar and other mods' slots alone; equipped and favourite items never move.

## Favourites, junk and trash

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Favourite Item Key` | F | Player | Mark the hovered kind of item as a favourite: it never moves, is never trashed or salvaged. Over a recipe in the crafting list it makes a favourite recipe (see [Crafting and Salvage](wiki:Crafting and Salvage)). |
| `Favourite Slot Key` | Shift + F | Player | Mark the hovered slot as a favourite: its content stays put. |
| `Junk Key` | J | Player | Mark the hovered kind of item as junk. |
| `Destroy Junk Key` | Shift + Delete | Player | Destroy every junk stack you carry. |
| `Show Favourites` | on | Player | Star on favourites, border on favourite slots, cross on junk. |
| `Trash Key` | Delete | Player | Destroy the hovered stack. |
| `Confirm Trash` | off | Player | Ask before destroying. |
| `Trash Uses Salvage` | off | Player | Trashing a stack salvages it instead when it can. |

Drag a stack onto the trash can to destroy it. `Shift` + click the can for trash mode: every stack you click is destroyed until you let go of Shift.

## Sorting and cycling

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Sort Inventory Key` | T | Player | Sort your inventory. |
| `Sort Container Key` | Y | Player | Sort the open chest. |
| `Sort Order` | Category | Player | `Category`, `Name`, `Weight`, `Value` or `Amount` (most held first). |
| `Sort Favourite Items` | on | Player | Off: favourite items keep their cells. |
| `Auto Sort Containers` | off | Player | Sort a chest when you open it. |
| `Auto Sort Inventory` | off | Player | Sort your inventory when it opens. |
| `Cycle Previous Key` | Left Arrow | Player | Open the previous chest around you (within about 4 m). |
| `Cycle Next Key` | Right Arrow | Player | Open the next chest around you. |
| `Cycle With Wheel` | on | Player | The mouse wheel over the chest cycles too. |

Sorting merges stacks and leaves the hotbar, favourite slots and equipped items in place.

## Ground pickup

Chests pick up items dropped around them. Needs `Ground Pickup` on and `pickup: true` for the chest in `OpenKeep.Store.yml`. A chest already holding the item gets it first, then the one nearest the drop, so a kiln's coal lands in the nearest chest of coal.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Ground Pickup` | off | Server | Master switch. |
| `Pickup Range` | 2 m | Server | Distance around the chest (1 to 50). |
| `Pickup Interval` | 5 s | Server | Time between sweeps (1 to 120). |
| `Pickup Delay` | 30 s | Server | How long an item lies before it is taken (0 to 3600). |
| `Pickup Only Held Items` | on | Server | Take only items the chest already holds, plus its `accept:` list. |

## OpenKeep.Store.yml

| Key | Meaning |
| --- | --- |
| `groups:` | Named item lists; Route sends an item to a chest holding another item of its group. |
| `containers:` `<prefab>:` `pickup: true` | This chest picks up drops. |
| `containers:` `<prefab>:` `accept:` | Items picked up and routed here even if it does not hold them yet. |
| `containers:` `<prefab>:` `refuse:` | Items never stored or picked up here. |

```yaml
groups:
  Metals: [Copper, Tin, Bronze, Iron, Silver, BlackMetal, Flametal]
containers:
  piece_chest_blackmetal:
    pickup: true
    accept: [group:Metals]
    refuse: [type:Trophy]
```

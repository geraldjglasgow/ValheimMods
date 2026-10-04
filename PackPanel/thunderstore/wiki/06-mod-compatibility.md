# Mod Compatibility

## Not compatible

- Mods that resize the player inventory or add equipment slots, such as ExtraSlots.
- When you switch from one, items in its extra rows move into PackPanel's grid.

## Works with

| Mod | With PackPanel |
| --- | --- |
| OpenKeep 1.8.0+ | Its buttons sit under the grid, and quick stack and sort never touch your slots. An `OpenKeep.Stacks.yml` entry for a key overrides `Key Stack`. |
| FeastMaster | The Food slots follow its `Food Slots` setting. |
| Elite Creatures Reborn | Its world tier box joins the stat boxes, and Thieving never steals from your slots. |
| Epic Loot | The stat sheet lists your active magic effects, and utilities in the extra slots count. |
| BiomeLords | The Featherweight blessing's extra rows join your grid. |
| Jewelcrafting, TrashItems, Quick Stack Store Sort Trash Restock, CurrencyPocket, OttoPay | Their boxes join PackPanel's stat column. |

Keep OpenKeep and Elite Creatures Reborn up to date. OpenKeep 1.8.0 to 1.9.0 and Elite Creatures Reborn 3.12.0 to 3.13.0 carry an older stat box column.

## Other mods' items

- Backpacks: the Backpack slot holds items with "backpack" in their name, or listed in `Backpack Items`, but does not wear them.
- Keys: add them to `Key Items` to put them on the ring.
- Lures and chum: add them to `Tackle Items` so the tacklebox holds them.

## Language

PackPanel's own words are in English. Stat names on the stat sheet follow the game's language.

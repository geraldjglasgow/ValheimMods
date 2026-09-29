# PlayerGrid

Which cells of the local player's inventory are the player's own grid and which are PackPanel's slots, so a mod that
moves, sorts or takes items can leave the slots alone without referencing PackPanel.

PackPanel keeps every slot (worn armour, utilities, the backpack, food, mead, ammo, the coin purse, the key ring, the
tacklebox) as an ordinary cell of the game's player `Inventory`, in the rows under the main grid. It publishes where
the main grid ends in the character's custom data, `PackPanel.mainGrid` = `width|rows|blocked`:

- the first `rows` rows are the main grid (a worn backpack's rows included), `width` wide;
- the last `blocked` cells of the bottom main row hold nothing (a backpack's partly used last row);
- every row below is a slot.

The key is saved with the character, so it outlives the mod: a reader trusts it only while PackPanel is loaded (its
plugin GUID in BepInEx's chainloader) and its `1. Inventory / Enabled` is on.

```csharp
// PackPanel, on every layout applied / when switched off
GridContract.Write(player, width, mainRows, blockedCells);
GridContract.Clear(player);

// a consumer
if (PackPanelGrid.InSlot(player, inventory, item)) continue;           // a slot: leave it
foreach (Vector2i cell in PackPanelGrid.BlockedCells(player, inventory)) ...
if (!PackPanelGrid.TryMainRows(player, out int mainRows)) return;      // PackPanel on, grid unreadable: hold back
bool own = item.m_gridPos.y < mainRows;                                 // int.MaxValue without PackPanel
ConfigEntry<int> stack = PackPanelLink.Entry<int>("3. Key Ring", "Key Stack");  // null without PackPanel
```

`TryMainRows` is for callers where touching a slot is worse than doing nothing (a theft): it answers every row without
PackPanel, PackPanel's main rows with it, and false when PackPanel lays the inventory out but the key cannot be read.
`InSlot` and `BlockedCells` answer "not a slot" and "none" in that case, for callers that only need to stay out of the
way.

Consumers: Elite Creatures Reborn (Thieving never takes from a slot). PackPanel's own `Layout/GridContract` and
OpenKeep's `Core/PackPanelGrid` and `Core/PackPanelLink` are the same code and move onto this library later.

Merge `PlayerGrid.dll` into your plugin with ILRepack like the other libraries. Depends on BepInEx and the game.

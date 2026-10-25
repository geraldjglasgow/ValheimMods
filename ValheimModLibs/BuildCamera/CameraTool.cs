using System;
using System.Collections.Generic;

namespace BuildCamera;

/// <summary>The build tool the camera builds with, taken in hand before the camera comes out.</summary>
public static class CameraTool
{
	/// <summary>
	/// True when the player holds a build tool the mod accepts (its piece table, a hammer say) and is in the game's place
	/// mode, equipping the first accepted one from the inventory when the hand holds none (the game's own equip: not
	/// mid-attack or dodge, not swimming, not broken). False when there is none to hold.
	/// </summary>
	public static bool Hold(Player player, Func<ItemDrop.ItemData, bool> accepts)
	{
		List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();
		foreach (ItemDrop.ItemData item in items)
		{
			if (IsTool(item, accepts) && player.IsItemEquiped(item))
			{
				return player.InPlaceMode();
			}
		}
		foreach (ItemDrop.ItemData item in items)
		{
			if (IsTool(item, accepts) && player.EquipItem(item))
			{
				return player.InPlaceMode();
			}
		}
		return false;
	}

	private static bool IsTool(ItemDrop.ItemData item, Func<ItemDrop.ItemData, bool> accepts) =>
		item.m_shared.m_buildPieces != null && accepts(item);
}

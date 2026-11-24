namespace HaloRing.Pointing;

/// <summary>A game screen that keeps a ring shut, or closes an open one: inventory, map, console, chat, text input, the
/// game menu, or the local player dead. A handful of static reads.</summary>
internal static class BlockingUi
{
	public static bool IsBlocked()
	{
		if (InventoryGui.IsVisible() || Minimap.IsOpen() || global::Console.IsVisible())
			return true;
		if (TextInput.IsVisible() || Menu.IsVisible())
			return true;
		if (Chat.instance != null && Chat.instance.HasFocus())
			return true;
		return Player.m_localPlayer != null && Player.m_localPlayer.IsDead();
	}
}

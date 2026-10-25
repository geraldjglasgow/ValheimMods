using System.Reflection;
using HarmonyLib;

namespace BlueprintShelf;

/// <summary>
/// Server side: who sent a request, and whether they may change a player's shared blueprints - the player who shared them
/// (the same character), an admin on the server's admin list, or the hosting player. The character is read from the
/// server's own copy of the sender's player object, never from what the request says. The admin list and its test are
/// the game's private <c>ZNet.m_adminList</c> and <c>ListContainsId</c>, reached by name.
/// </summary>
internal static class ShelfRights
{
	private static readonly FieldInfo? AdminList = AccessTools.Field(typeof(ZNet), "m_adminList");
	private static readonly MethodInfo? ListContainsId = AccessTools.Method(typeof(ZNet), "ListContainsId");

	/// <summary>The sender's character id and name; 0 when the server does not know their character (yet).</summary>
	public static long Who(long sender, out string name)
	{
		if (sender == ZDOMan.GetSessionID())
		{
			Player local = Player.m_localPlayer;
			name = local != null ? local.GetPlayerName() : "";
			return local != null ? local.GetPlayerID() : 0L;
		}
		ZNetPeer? peer = ZNet.instance.GetPeer(sender);
		name = peer?.m_playerName ?? "";
		ZDO? character = peer == null || peer.m_characterID.IsNone() ? null : ZDOMan.instance.GetZDO(peer.m_characterID);
		return character != null ? character.GetLong(ZDOVars.s_playerID) : 0L;
	}

	public static bool IsAdmin(long sender)
	{
		if (sender == ZDOMan.GetSessionID())
		{
			return true;
		}
		ZNetPeer? peer = ZNet.instance.GetPeer(sender);
		if (peer?.m_socket == null || AdminList == null || ListContainsId == null)
		{
			return false;
		}
		object list = AdminList.GetValue(ZNet.instance);
		return list != null && (bool)ListContainsId.Invoke(ZNet.instance, new[] { list, peer.m_socket.GetHostName() });
	}

	public static bool MayChange(long sender, long owner) => owner != 0L && (Who(sender, out _) == owner || IsAdmin(sender));
}

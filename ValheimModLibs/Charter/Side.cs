using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Charter;

/// <summary>Which side of the connection this process is, read from the game each time it is asked.</summary>
internal static class Side
{
	/// <summary>The game's peer list as a set, read once per frame (and again when its length changes).</summary>
	private static readonly HashSet<ZNetPeer> present = new();
	private static int presentFrame = -1;
	private static int presentCount = -1;

	/// <summary>True on a dedicated server, for the hosting player, in single player and outside any game.</summary>
	public static bool IsServer => ZNet.instance == null || ZNet.instance.IsServer();

	/// <summary>The connected peers that passed the game's own join checks.</summary>
	public static List<ZNetPeer> ReadyPeers()
	{
		List<ZNetPeer> ready = new();
		ReadyPeers(ready);
		return ready;
	}

	/// <summary>The same, into a list the caller keeps (cleared first), for a timer that asks every second.</summary>
	public static void ReadyPeers(List<ZNetPeer> ready)
	{
		ready.Clear();
		if (ZNet.instance == null)
		{
			return;
		}
		foreach (ZNetPeer peer in ZNet.instance.GetPeers())
		{
			if (peer.IsReady() && peer.m_rpc != null && peer.m_rpc.IsConnected())
			{
				ready.Add(peer);
			}
		}
	}

	public static ZNetPeer? PeerOf(ZRpc rpc)
	{
		return ZNet.instance == null ? null : ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc);
	}

	/// <summary>
	/// True while the peer is still in the game's peer list and its socket is open. Every lane of every charter asks
	/// every frame, so the list is looked up in a set made once per frame; a peer missing from it (one that just left,
	/// or one that joined since the set was made) is looked for in the list itself.
	/// </summary>
	public static bool IsPresent(ZNetPeer peer)
	{
		if (ZNet.instance == null || peer.m_rpc == null || !peer.m_rpc.IsConnected())
		{
			return false;
		}
		List<ZNetPeer> peers = ZNet.instance.GetPeers();
		Refresh(peers);
		return present.Contains(peer) || peers.Contains(peer);
	}

	private static void Refresh(List<ZNetPeer> peers)
	{
		int frame = Time.frameCount;
		if (frame == presentFrame && peers.Count == presentCount)
		{
			return;
		}
		presentFrame = frame;
		presentCount = peers.Count;
		present.Clear();
		foreach (ZNetPeer peer in peers)
		{
			present.Add(peer);
		}
	}

	public static string NameOf(ZNetPeer peer) => string.IsNullOrEmpty(peer.m_playerName) ? HostOf(peer) : peer.m_playerName;

	public static string HostOf(ZNetPeer peer) => peer.m_socket?.GetHostName() ?? "?";
}

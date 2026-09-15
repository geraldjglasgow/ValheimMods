using System;
using System.Collections.Generic;
using System.Linq;

namespace Charter;

/// <summary>
/// The join check of the lead copy: the server sends its family registry to a new peer, the player answers with
/// its own, both compare. The server refuses a mismatching peer with a refusal code and disconnects it a second
/// later; the player refuses itself when the server lacks a mod it requires; a peer that never answers is
/// refused after five seconds when any of the server's mods is mandatory.
/// </summary>
internal static class FamilyRpc
{
	private const string FamilyName = "Charter_Family";
	private const string RefuseName = "Charter_Refuse";
	private const float Patience = 5f;
	private const float Grace = 1f;

	private static readonly Dictionary<ZNetPeer, float> awaiting = new();
	private static readonly Dictionary<ZNetPeer, float> leaving = new();
	private static float now;

	public static Journal? Journal { get; set; }

	/// <summary>The server's registry as received at join, on the player; null when not connected.</summary>
	public static List<FamilyEntry>? ServerRegistry { get; private set; }

	public static void Attach(ZNetPeer peer, bool server)
	{
		if (server)
		{
			peer.m_rpc.Register<ZPackage>(FamilyName, OnClientRegistry);
			return;
		}
		ServerRegistry = null;
		peer.m_rpc.Register<ZPackage>(FamilyName, OnServerRegistry);
		peer.m_rpc.Register<ZPackage>(RefuseName, OnRefused);
	}

	/// <summary>Server: sends the registry to a peer the game just accepted and starts waiting for the answer.</summary>
	public static void Greet(ZNetPeer peer)
	{
		ZPackage pkg = new();
		FamilyEntry.Write(pkg, Family.Entries());
		peer.m_rpc.Invoke(FamilyName, pkg);
		awaiting[peer] = now + Patience;
	}

	public static void Tick(float time)
	{
		now = time;
		foreach (ZNetPeer peer in awaiting.Where(p => p.Value <= now).Select(p => p.Key).ToList())
		{
			awaiting.Remove(peer);
			List<FamilyEntry> mine = Family.Entries();
			if (Side.IsPresent(peer) && mine.Any(e => e.Mandatory))
			{
				Refuse(peer, FamilyCheck.Compare(mine, new List<FamilyEntry>()));
			}
		}
		foreach (ZNetPeer peer in leaving.Where(p => p.Value <= now).Select(p => p.Key).ToList())
		{
			leaving.Remove(peer);
			if (Side.IsPresent(peer))
			{
				ZNet.instance.Disconnect(peer);
			}
		}
	}

	private static void OnClientRegistry(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer? peer = Side.PeerOf(rpc);
		List<FamilyEntry>? theirs = FamilyEntry.Read(pkg);
		if (peer == null || theirs == null)
		{
			return;
		}
		awaiting.Remove(peer);
		List<string> lines = FamilyCheck.Compare(Family.Entries(), theirs);
		if (lines.Count > 0)
		{
			Refuse(peer, lines);
			return;
		}
		Journal?.Trace($"family check passed for {Side.NameOf(peer)}: {theirs.Count} mod(s)");
	}

	private static void OnServerRegistry(ZRpc rpc, ZPackage pkg)
	{
		List<FamilyEntry>? theirs = FamilyEntry.Read(pkg);
		if (theirs == null)
		{
			return;
		}
		ServerRegistry = theirs;
		List<FamilyEntry> mine = Family.Entries();
		ZPackage answer = new();
		FamilyEntry.Write(answer, mine);
		rpc.Invoke(FamilyName, answer);
		if (FamilyCheck.ServerLacksMandatory(theirs, mine))
		{
			Leave(rpc, FamilyCheck.Compare(theirs, mine));
		}
	}

	private static void OnRefused(ZRpc rpc, ZPackage pkg)
	{
		string id = pkg.ReadString();
		string text = pkg.ReadString();
		foreach (string line in text.Split('\n'))
		{
			Journal?.Error(line);
		}
		Journal?.Error($"refused by the server, refusal code {id}");
		RefusalScreen.Pending = text;
	}

	private static void Refuse(ZNetPeer peer, List<string> lines)
	{
		string id = FamilyCheck.NewRefusalCode();
		foreach (string line in lines)
		{
			Journal?.Error(line);
		}
		Journal?.Error($"refusing {Side.NameOf(peer)} ({Side.HostOf(peer)}), refusal code {id}");
		ZPackage pkg = new();
		pkg.Write(id);
		pkg.Write(FamilyCheck.Message(lines, id));
		peer.m_rpc.Invoke(RefuseName, pkg);
		leaving[peer] = now + Grace;
	}

	private static void Leave(ZRpc rpc, List<string> lines)
	{
		string id = FamilyCheck.NewRefusalCode();
		foreach (string line in lines)
		{
			Journal?.Error(line);
		}
		Journal?.Error($"leaving the server, refusal code {id}");
		RefusalScreen.Pending = FamilyCheck.Message(lines, id);
		rpc.GetSocket().Close();
	}
}

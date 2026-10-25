using System;
using System.Reflection;
using HarmonyLib;

namespace BlueprintShelf;

/// <summary>
/// A shelf's routed RPCs, each the host's prefix and its own name. Requests go to the server (<see cref="ShelfServer"/>),
/// which keeps the pool and answers the asking machine only; a change to the pool is announced to everybody
/// (<see cref="Changed"/>), so open windows list it again. A machine takes answers only from the server (the game's
/// private <c>ZRoutedRpc.GetServerPeerID</c>, reached by name). Registered on every machine with every network
/// (<see cref="ShelfHooks"/>); the pool needs the host mod on the server.
/// </summary>
internal sealed class ShelfRpc
{
	private static readonly MethodInfo? ServerPeer = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID");

	private readonly Shelf shelf;
	private ZRoutedRpc? registered;

	public ShelfRpc(Shelf shelf)
	{
		this.shelf = shelf;
		string prefix = shelf.Settings.RpcPrefix;
		List = prefix + "List";
		Listing = prefix + "Listing";
		Put = prefix + "Put";
		PutDone = prefix + "PutDone";
		Get = prefix + "Get";
		File = prefix + "File";
		Remove = prefix + "Remove";
		Move = prefix + "Move";
		MakeFolder = prefix + "MakeFolder";
		Say = prefix + "Say";
		Changed = prefix + "Changed";
	}

	public string List { get; }
	public string Listing { get; }
	public string Put { get; }
	public string PutDone { get; }
	public string Get { get; }
	public string File { get; }
	public string Remove { get; }
	public string Move { get; }
	public string MakeFolder { get; }
	public string Say { get; }
	public string Changed { get; }

	/// <summary>Registers the RPCs on a network's table, once per table.</summary>
	public void Register(ZRoutedRpc rpc)
	{
		if (rpc == registered)
		{
			return;
		}
		registered = rpc;
		ToServer(rpc, shelf.Server);
		ToPlayers(rpc);
	}

	private void ToServer(ZRoutedRpc rpc, ShelfServer server)
	{
		rpc.Register(List, sender => Guard("list", () => server.OnList(sender)));
		rpc.Register<ZPackage>(Put, (sender, part) => Guard("share", () => server.OnPut(sender, part)));
		rpc.Register<long, string>(Get, (sender, owner, path) => Guard("take", () => server.OnGet(sender, owner, path)));
		rpc.Register<long, string, bool>(Remove,
			(sender, owner, path, folder) => Guard("remove", () => server.OnRemove(sender, owner, path, folder)));
		rpc.Register<long, string, string, bool>(Move,
			(sender, owner, from, to, folder) => Guard("move", () => server.OnMove(sender, owner, from, to, folder)));
		rpc.Register<long, string>(MakeFolder, (sender, owner, path) => Guard("folder", () => server.OnMakeFolder(sender, owner, path)));
	}

	private void ToPlayers(ZRoutedRpc rpc)
	{
		rpc.Register<ZPackage>(Listing, (sender, pool) => Guard("listing", () => shelf.Pool.OnListing(sender, pool)));
		rpc.Register<string, string>(PutDone, (sender, path, word) => Guard("shared", () => shelf.Share.OnDone(sender, path, word)));
		rpc.Register<ZPackage>(File, (sender, part) => Guard("file", () => shelf.Take.OnFile(sender, part)));
		rpc.Register<string, string, string>(Say, (sender, word, a, b) => Guard("message", () => OnSay(sender, word, a, b)));
		rpc.Register(Changed, sender => shelf.Pool.MarkStale());
	}

	/// <summary>The answer came from the server (the only machine that answers shelf requests).</summary>
	public static bool FromServer(long sender) =>
		ZRoutedRpc.instance != null && ServerPeer != null && sender == (long)ServerPeer.Invoke(ZRoutedRpc.instance, null);

	/// <summary>Server: a message for the asking player, a word (sent as its token) with up to two values, shown in their language.</summary>
	public void Tell(long target, string word, string? a = "", string? b = "") =>
		ZRoutedRpc.instance.InvokeRoutedRPC(target, Say, shelf.Words.Token(word), a ?? "", b ?? "");

	private void OnSay(long sender, string word, string a, string b)
	{
		if (FromServer(sender) && shelf.Words.Read(word, a, b) is string text)
		{
			shelf.Say(text);
		}
	}

	private void Guard(string what, Action action) => shelf.Run(what, action);
}

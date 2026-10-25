using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BlueprintShelf;

/// <summary>
/// Server side of a shelf: lists the pool, stores a shared file once all its parts are in, sends a file back in parts,
/// removes and moves. Every request is checked here whatever the asking machine checked: the shelf on, a known character,
/// valid paths, the size and count limits, the host's check of the file, and for a change the sharer or an admin
/// (<see cref="ShelfRights"/>). Answers go to the asking machine; a change is announced to everybody.
/// </summary>
internal sealed class ShelfServer
{
	private readonly Shelf shelf;
	private readonly ShelfInbox inbox;

	public ShelfServer(Shelf shelf)
	{
		this.shelf = shelf;
		inbox = new ShelfInbox(shelf.Limits);
	}

	private ShelfStore Store => shelf.Store;

	private bool Serving(long sender)
	{
		if (ZNet.instance == null || !ZNet.instance.IsServer())
		{
			return false;
		}
		bool on = shelf.Settings.Enabled();
		if (!on)
		{
			shelf.Rpc.Tell(sender, ShelfWord.Disabled);
		}
		return on;
	}

	public void OnList(long sender)
	{
		if (Serving(sender))
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, shelf.Rpc.Listing, Store.Listing);
		}
	}

	/// <summary>One part of a shared file: path, part, parts, bytes. The last part stores it.</summary>
	public void OnPut(long sender, ZPackage package)
	{
		if (!Serving(sender))
		{
			return;
		}
		string path = package.ReadString();
		int part = package.ReadInt();
		int parts = package.ReadInt();
		byte[]? packed = inbox.Add(sender + "|" + path, part, parts, package.ReadByteArray());
		if (packed != null)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, shelf.Rpc.PutDone, path, Answer(Keep(sender, path, packed)));
		}
	}

	/// <summary>A stored file's answer: nothing, or the reason's word as its token, read in the player's language.</summary>
	private string Answer(string? refused) => refused != null ? shelf.Words.Token(refused) : "";

	/// <summary>Stores a whole shared file; null when stored, else the reason's word.</summary>
	private string? Keep(long sender, string path, byte[] packed)
	{
		long id = ShelfRights.Who(sender, out string name);
		byte[]? data = id == 0L || shelf.Clean(path) == null ? null : ShelfZip.Unpack(packed, shelf.Limits.MaxFileBytes);
		string? refused = Refusal(id, path, data);
		if (refused != null || data == null)
		{
			return refused;
		}
		if (!Disk(() => Store.Save(id, name, path, data)))
		{
			return ShelfWord.Failed;
		}
		shelf.Info($"{name} shared {path} ({data.Length} bytes)");
		Announce();
		return null;
	}

	/// <summary>Why a received file is not stored (unpacked, null when it was too big or not read): the reason's word, or null.</summary>
	private string? Refusal(long id, string path, byte[]? data)
	{
		if (id == 0L)
		{
			return ShelfWord.Unknown;
		}
		if (shelf.Clean(path) == null)
		{
			return ShelfWord.BadName;
		}
		if (data == null)
		{
			return ShelfWord.TooBig;
		}
		if (!shelf.Settings.Accepts(data))
		{
			return ShelfWord.NotReadable;
		}
		bool full = !Store.Has(id, path) && (Store.Find(id)?.Paths.Count ?? 0) >= shelf.Limits.MaxPerPlayer;
		return full ? ShelfWord.Full : null;
	}

	/// <summary>Sends one shared file in parts: owner, owner's name, path, part, parts, bytes; no parts when it is gone.</summary>
	public void OnGet(long sender, long owner, string path)
	{
		if (!Serving(sender))
		{
			return;
		}
		byte[]? data = null;
		if (shelf.Clean(path) != null)
		{
			Disk(() => data = Store.Read(owner, path));
		}
		string name = Store.Find(owner)?.Name ?? "";
		List<byte[]> parts = data != null ? ShelfZip.Split(ShelfZip.Pack(data), shelf.Limits.PartBytes) : new List<byte[]>();
		for (int i = 0; i < parts.Count; i++)
		{
			SendPart(sender, owner, name, path, i, parts.Count, parts[i]);
		}
		if (parts.Count == 0)
		{
			SendPart(sender, owner, name, path, 0, 0, new byte[0]);
		}
	}

	private void SendPart(long target, long owner, string name, string path, int part, int parts, byte[] bytes)
	{
		ZPackage package = new ZPackage();
		package.Write(owner);
		package.Write(name);
		package.Write(path);
		package.Write(part);
		package.Write(parts);
		package.Write(bytes);
		ZRoutedRpc.instance.InvokeRoutedRPC(target, shelf.Rpc.File, package);
	}

	public void OnRemove(long sender, long owner, string path, bool folder)
	{
		if (!Serving(sender) || !Allowed(sender, owner, path, folder))
		{
			return;
		}
		string shown = Shown(owner, path);
		int removed = -1;
		if (!Disk(() => removed = Store.Delete(owner, path, folder)))
		{
			shelf.Rpc.Tell(sender, ShelfWord.Failed, shown);
		}
		else
		{
			shelf.Rpc.Tell(sender, removed >= 0 ? ShelfWord.Removed : ShelfWord.Gone, shown, removed.ToString(CultureInfo.InvariantCulture));
		}
		if (removed >= 0)
		{
			Announce();
		}
	}

	/// <summary>An empty folder in a player's part; the sender's own part is made when they have none yet.</summary>
	public void OnMakeFolder(long sender, long owner, string path)
	{
		if (!Serving(sender) || !Allowed(sender, owner, path, false))
		{
			return;
		}
		long self = ShelfRights.Who(sender, out string name);
		string? refused = null;
		if (!Disk(() => refused = Store.MakeFolder(owner, self == owner ? name : null, path)))
		{
			shelf.Rpc.Tell(sender, ShelfWord.Failed, path);
		}
		else
		{
			shelf.Rpc.Tell(sender, refused ?? ShelfWord.FolderMade, path);
		}
		if (refused == null)
		{
			Announce();
		}
	}

	public void OnMove(long sender, long owner, string from, string to, bool folder)
	{
		if (!Serving(sender) || !Allowed(sender, owner, from, folder))
		{
			return;
		}
		string? refused = null;
		if (shelf.Clean(to) == null)
		{
			shelf.Rpc.Tell(sender, ShelfWord.BadName, to);
		}
		else if (!Disk(() => refused = Store.Move(owner, from, to, folder)))
		{
			shelf.Rpc.Tell(sender, ShelfWord.Failed, Shown(owner, from));
		}
		else
		{
			shelf.Rpc.Tell(sender, refused ?? ShelfWord.Renamed, refused == ShelfWord.Exists ? to : from, to);
			if (refused == null)
			{
				Announce();
			}
		}
	}

	/// <summary>A valid path (the top only for a whole folder) of an owner the sender may change; says why not otherwise.</summary>
	private bool Allowed(long sender, long owner, string path, bool folder)
	{
		if (shelf.Clean(path, folder) == null)
		{
			shelf.Rpc.Tell(sender, ShelfWord.BadName, path);
			return false;
		}
		if (ShelfRights.MayChange(sender, owner))
		{
			return true;
		}
		shelf.Rpc.Tell(sender, ShelfWord.NotYours, Shown(owner, path));
		return false;
	}

	/// <summary>"Name/path" as players see it in the pool.</summary>
	private string Shown(long owner, string path)
	{
		string name = Store.Find(owner)?.Name ?? "?";
		return path.Length == 0 ? name : name + "/" + path;
	}

	/// <summary>Runs a file operation; false (logged) when the disk refused it.</summary>
	private bool Disk(Action work)
	{
		try
		{
			work();
			return true;
		}
		catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
		{
			shelf.Warn("file operation failed: " + e.Message);
			return false;
		}
	}

	private void Announce() => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, shelf.Rpc.Changed);
}

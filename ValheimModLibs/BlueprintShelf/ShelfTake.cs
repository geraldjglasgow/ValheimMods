using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// A player's machine taking copies from the pool: the files are asked for a few at a time, arrive packed in parts and are
/// written into the player's own files at the path the window chose (the folder the player's side shows, a taken folder
/// keeping what is inside it); a name already taken there gets " (2)", " (3)" and so on, so nothing of the player's is
/// ever overwritten. One message sums up the batch. A file the server stops sending is given up after the timeout.
/// </summary>
internal sealed class ShelfTake
{
	private readonly Shelf shelf;
	private readonly Queue<(long Owner, string Path, string Dest)> waiting = new();
	private readonly Dictionary<string, float> asked = new();
	private readonly Dictionary<string, string> destinations = new();
	private readonly ShelfInbox inbox;
	private int took;
	private int failed;
	private string into = "";

	public ShelfTake(Shelf shelf)
	{
		this.shelf = shelf;
		inbox = new ShelfInbox(shelf.Limits);
	}

	public bool Busy => waiting.Count > 0 || asked.Count > 0;

	/// <summary>Files still to come, for the window's status line.</summary>
	public int Left => waiting.Count + asked.Count;

	/// <summary>Queues pool files of one player (Path) for paths of the player's own (Dest); <paramref name="folder"/> names the batch.</summary>
	public void Take(long owner, IEnumerable<(string Path, string Dest)> files, string folder)
	{
		foreach ((string path, string dest) in files)
		{
			if (!asked.ContainsKey(Key(owner, path)) && !waiting.Any(w => w.Owner == owner && w.Path == path))
			{
				waiting.Enqueue((owner, path, dest));
			}
		}
		into = folder;
	}

	public void Tick()
	{
		if (!Busy)
		{
			return;
		}
		while (asked.Count < shelf.Limits.TakesInFlight && waiting.Count > 0)
		{
			(long owner, string path, string dest) = waiting.Dequeue();
			asked[Key(owner, path)] = Time.unscaledTime;
			destinations[Key(owner, path)] = dest;
			ZRoutedRpc.instance?.InvokeRoutedRPC(shelf.Rpc.Get, owner, path);
		}
		GiveUpLate();
	}

	private static string Key(long owner, string path) => owner + "|" + path;

	private void GiveUpLate()
	{
		float now = Time.unscaledTime;
		List<string> late = asked.Where(a => now - a.Value > shelf.Limits.Timeout).Select(a => a.Key).ToList();
		foreach (string key in late)
		{
			asked.Remove(key);
		}
		failed += late.Count;
		if (late.Count > 0 && !Busy)
		{
			Report();
		}
	}

	/// <summary>One part from the server: owner, owner's name, path, part, parts (none: the file is gone), bytes.</summary>
	public void OnFile(long sender, ZPackage package)
	{
		if (!ShelfRpc.FromServer(sender))
		{
			return;
		}
		long owner = package.ReadLong();
		package.ReadString();
		string path = package.ReadString();
		int part = package.ReadInt();
		int parts = package.ReadInt();
		byte[] bytes = package.ReadByteArray();
		string key = Key(owner, path);
		if (!asked.ContainsKey(key))
		{
			return;
		}
		asked[key] = Time.unscaledTime;
		byte[]? packed = parts > 0 ? inbox.Add(key, part, parts, bytes) : null;
		if (parts <= 0 || packed != null)
		{
			Arrived(key, packed);
		}
	}

	/// <summary>A file is in (or gone, <paramref name="packed"/> null): written, or counted as not taken.</summary>
	private void Arrived(string key, byte[]? packed)
	{
		asked.Remove(key);
		if (packed != null && destinations.TryGetValue(key, out string dest) && Save(dest, packed))
		{
			took++;
		}
		else
		{
			failed++;
		}
		if (!Busy)
		{
			Report();
		}
	}

	/// <summary>Writes a taken copy into the player's files at a free name; false when it cannot be read or written.</summary>
	private bool Save(string dest, byte[] packed)
	{
		byte[]? data = ShelfZip.Unpack(packed, shelf.Limits.MaxFileBytes);
		if (data == null || shelf.Clean(dest) == null)
		{
			return false;
		}
		try
		{
			string file = shelf.Files.FileOf(Free(dest));
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			File.WriteAllBytes(file, data);
		}
		catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
		{
			shelf.Warn($"cannot write {dest} taken from the pool: {e.Message}");
			return false;
		}
		shelf.Files.Changed();
		return true;
	}

	/// <summary>The path itself when no file has it, else "name (2)", "name (3)" ... in the same folder.</summary>
	private string Free(string path)
	{
		if (!File.Exists(shelf.Files.FileOf(path)))
		{
			return path;
		}
		for (int n = 2; ; n++)
		{
			string candidate = path + " (" + n + ")";
			if (!File.Exists(shelf.Files.FileOf(candidate)))
			{
				return candidate;
			}
		}
	}

	private void Report()
	{
		List<string> lines = new List<string>();
		if (took > 0)
		{
			lines.Add(shelf.Words.Format(ShelfWord.Took, took, shelf.Shown(into)));
		}
		if (failed > 0)
		{
			lines.Add(shelf.Words.Format(ShelfWord.NotTaken, failed));
		}
		shelf.Say(string.Join("\n", lines));
		took = failed = 0;
		destinations.Clear();
	}
}

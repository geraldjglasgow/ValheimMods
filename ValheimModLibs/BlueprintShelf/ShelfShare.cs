using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// A player's machine sharing files: each file of the player's own is read (it must be one the host can read), packed and
/// sent to the server in parts, a few a frame, to the path in the player's part of the pool the window chose for it;
/// sharing to a path again replaces it. The server answers every file; once all are answered one message sums it up. A
/// server that stops answering is given up after the limits' timeout.
/// </summary>
internal sealed class ShelfShare
{
	private readonly Shelf shelf;
	private readonly Queue<(string Local, string Dest)> waiting = new();
	private readonly Queue<ZPackage> parts = new();
	private int sent;
	private int answered;
	private int shared;
	private string? firstRefusal;
	private float lastHeard;

	public ShelfShare(Shelf shelf) => this.shelf = shelf;

	private ShelfLimits Limits => shelf.Limits;

	public bool Busy => waiting.Count > 0 || parts.Count > 0 || answered < sent;

	/// <summary>Files not yet answered, for the window's status line.</summary>
	public int Left => waiting.Count + sent - answered;

	/// <summary>Queues the player's files (Local) for the pool paths they go to in the player's part (Dest).</summary>
	public void Send(IEnumerable<(string Local, string Dest)> files)
	{
		foreach ((string Local, string Dest) file in files)
		{
			if (!waiting.Contains(file))
			{
				waiting.Enqueue(file);
			}
		}
		lastHeard = Time.unscaledTime;
	}

	/// <summary>Every file of the player's own, each to the same path in their part (Share all).</summary>
	public List<(string, string)> AllOwn() => shelf.Files.Everything().Select(p => (p, p)).ToList();

	public void Tick()
	{
		if (!Busy)
		{
			return;
		}
		if (parts.Count == 0 && waiting.Count > 0)
		{
			Prepare(waiting.Dequeue());
		}
		for (int i = 0; i < Limits.PartsPerFrame && parts.Count > 0; i++)
		{
			ZRoutedRpc.instance?.InvokeRoutedRPC(shelf.Rpc.Put, parts.Dequeue());
		}
		if (waiting.Count == 0 && parts.Count == 0 && answered < sent && Time.unscaledTime - lastHeard > Limits.Timeout)
		{
			GiveUp();
		}
	}

	private void Prepare((string Local, string Dest) share)
	{
		sent++;
		lastHeard = Time.unscaledTime;
		byte[]? packed = Pack(share.Local, share.Dest, out string? refused);
		if (packed == null)
		{
			Answer(share.Local, shelf.Words.Token(refused ?? ShelfWord.NotReadable));
			return;
		}
		List<byte[]> pieces = ShelfZip.Split(packed, Limits.PartBytes);
		for (int i = 0; i < pieces.Count; i++)
		{
			parts.Enqueue(Part(share.Dest, i, pieces.Count, pieces[i]));
		}
	}

	/// <summary>The file packed for sending, or null with the reason's word.</summary>
	private byte[]? Pack(string path, string dest, out string? refused)
	{
		FileInfo file = new FileInfo(shelf.Files.FileOf(path));
		refused = shelf.Clean(dest) == null ? ShelfWord.BadName
			: !file.Exists ? ShelfWord.NotReadable
			: file.Length > Limits.MaxFileBytes ? ShelfWord.TooBig
			: !shelf.Files.Readable(path) ? ShelfWord.NotReadable : null;
		if (refused != null)
		{
			return null;
		}
		byte[] packed = ShelfZip.Pack(File.ReadAllBytes(file.FullName));
		if (packed.Length <= Limits.MaxParts * Limits.PartBytes)
		{
			return packed;
		}
		refused = ShelfWord.TooBig;
		return null;
	}

	private static ZPackage Part(string path, int part, int count, byte[] bytes)
	{
		ZPackage package = new ZPackage();
		package.Write(path);
		package.Write(part);
		package.Write(count);
		package.Write(bytes);
		return package;
	}

	/// <summary>The server's answer for one file: no word when it was stored.</summary>
	public void OnDone(long sender, string path, string word)
	{
		if (ShelfRpc.FromServer(sender) && answered < sent)
		{
			Answer(path, string.IsNullOrEmpty(word) ? null : word);
		}
	}

	/// <summary>One file answered: stored, or refused with the reason's token (its second value, the share limit, is for the Full word).</summary>
	private void Answer(string path, string? refused)
	{
		answered++;
		lastHeard = Time.unscaledTime;
		if (refused == null)
		{
			shared++;
		}
		else if (firstRefusal == null)
		{
			firstRefusal = shelf.Words.Read(refused, path, Limits.MaxPerPlayer) ?? refused;
		}
		if (!Busy)
		{
			Report();
		}
	}

	private void GiveUp()
	{
		waiting.Clear();
		parts.Clear();
		firstRefusal = shelf.Words.Get(ShelfWord.NoAnswer);
		answered = sent;
		Report();
	}

	/// <summary>One message for the whole batch: how many were shared, the first refusal and how many more there were.</summary>
	private void Report()
	{
		int refusals = sent - shared;
		List<string> lines = new List<string>();
		if (shared > 0)
		{
			lines.Add(shelf.Words.Format(ShelfWord.Shared, shared));
		}
		if (firstRefusal != null)
		{
			lines.Add(firstRefusal);
		}
		if (refusals > 1)
		{
			lines.Add(shelf.Words.Format(ShelfWord.MoreRefused, refusals - 1));
		}
		shelf.Say(string.Join("\n", lines));
		sent = answered = shared = 0;
		firstRefusal = null;
	}
}

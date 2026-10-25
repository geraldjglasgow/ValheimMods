using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// A player's machine: the shared pool as the server last listed it. Asked for when the window opens and again (at most
/// once a second) while it is open and the server announced a change.
/// </summary>
internal sealed class ShelfPool
{
	private const float AskInterval = 1f;
	private const float AnswerWait = 5f;

	private readonly Shelf shelf;
	private bool stale;
	private float nextAsk;
	private float openedAt;

	public ShelfPool(Shelf shelf) => this.shelf = shelf;

	public List<ShelfOwner> Owners { get; private set; } = new();

	/// <summary>A listing arrived since the window opened.</summary>
	public bool Loaded { get; private set; }

	/// <summary>No listing came within a few seconds of opening: the server does not answer this shelf's requests.</summary>
	public bool Silent => !Loaded && Time.unscaledTime - openedAt > AnswerWait;

	/// <summary>Goes up with every listing.</summary>
	public int Version { get; private set; }

	public ShelfOwner? Find(long id) => Owners.FirstOrDefault(o => o.Id == id);

	/// <summary>The window opened: forget the old listing and ask for the pool.</summary>
	public void Open()
	{
		Loaded = false;
		openedAt = Time.unscaledTime;
		Ask();
	}

	private void Ask()
	{
		stale = false;
		nextAsk = Time.unscaledTime + AskInterval;
		ZRoutedRpc.instance?.InvokeRoutedRPC(shelf.Rpc.List);
	}

	public void MarkStale() => stale = true;

	public void Tick()
	{
		if (stale && shelf.IsOpen && Time.unscaledTime >= nextAsk)
		{
			Ask();
		}
	}

	public void OnListing(long sender, ZPackage package)
	{
		if (!ShelfRpc.FromServer(sender))
		{
			return;
		}
		Owners = ShelfListing.Read(package, shelf.Limits.MaxFileBytes);
		Loaded = true;
		Version++;
	}
}

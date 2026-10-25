using System.Collections.Generic;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// Changes to the shared pool, asked of the server: a new folder in the part shown, a rename (the typed name checked here
/// first, then by the server), a remove after the game's yes / no box, and moves into a folder of the same part. The server
/// answers with a message and, when the pool changed, tells everybody to list it again.
/// </summary>
internal sealed class PoolEdits
{
	private readonly Shelf shelf;
	private readonly PoolPane pane;

	public PoolEdits(Shelf shelf, PoolPane pane)
	{
		this.shelf = shelf;
		this.pane = pane;
	}

	private ShelfRpc Rpc => shelf.Rpc;

	public void NewFolder()
	{
		long owner = pane.Owner;
		string at = pane.Folder;
		ShelfPrompt.Ask(shelf.Words.Get(ShelfWord.NewFolderTopic), "", shelf.Limits.NameLimit, name => shelf.Run("pool folder", () =>
		{
			string? path = shelf.Files.Target(name, at, out string? error);
			if (path == null)
			{
				shelf.Say(error ?? "");
			}
			else
			{
				ZRoutedRpc.instance?.InvokeRoutedRPC(Rpc.MakeFolder, owner, path);
			}
		}));
	}

	public void Rename(ShelfLine entry, long owner)
	{
		ShelfPrompt.Ask(shelf.Words.Get(ShelfWord.RenameTopic), ShelfPaths.Leaf(entry.Path), shelf.Limits.NameLimit,
			typed => shelf.Run("pool rename", () => Renamed(entry, owner, typed)));
	}

	private void Renamed(ShelfLine entry, long owner, string typed)
	{
		string? to = shelf.Files.Target(typed, ShelfPaths.Parent(entry.Path), out string? error);
		if (to == null)
		{
			shelf.Say(error ?? "");
		}
		else if (to != entry.Path)
		{
			ZRoutedRpc.instance?.InvokeRoutedRPC(Rpc.Move, owner, entry.Path, to, entry.Kind == ShelfKind.Folder);
		}
	}

	public void Remove(List<ShelfLine> picked)
	{
		if (picked.Count == 0)
		{
			return;
		}
		shelf.Confirm(shelf.Words.Get(ShelfWord.Remove), shelf.Words.Format(ShelfWord.RemoveAsk, ShelfConfirm.Names(picked)), () =>
		{
			foreach (ShelfLine entry in picked)
			{
				ZRoutedRpc.instance?.InvokeRoutedRPC(Rpc.Remove, pane.OwnerOf(entry), entry.Path, entry.Kind != ShelfKind.File);
			}
			pane.Selection.Clear();
		});
	}

	/// <summary>Asks the server to move each line into a folder of the same part; lines already there are left.</summary>
	public void MoveInto(List<ShelfLine> dragged, long owner, string into)
	{
		foreach (ShelfLine entry in dragged.Where(e => e.Kind == ShelfKind.Folder || e.Kind == ShelfKind.File))
		{
			if (!ShelfPaths.Same(ShelfPaths.Parent(entry.Path), into) && !ShelfPaths.Within(into, entry.Path))
			{
				ZRoutedRpc.instance?.InvokeRoutedRPC(Rpc.Move, owner, entry.Path,
					ShelfPaths.Join(into, ShelfPaths.Leaf(entry.Path)), entry.Kind == ShelfKind.Folder);
			}
		}
		pane.Selection.Clear();
	}
}

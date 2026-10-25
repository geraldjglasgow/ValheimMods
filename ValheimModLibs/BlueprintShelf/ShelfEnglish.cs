using System.Collections.Generic;

namespace BlueprintShelf;

/// <summary>
/// The library's own English for every <see cref="ShelfWord"/>, the default <see cref="ShelfWords.Text"/>. A key it does
/// not know reads as itself, so a host button may carry its words as they are.
/// </summary>
public static class ShelfEnglish
{
	private static readonly Dictionary<string, string> Words = new()
	{
		[ShelfWord.Title] = "Blueprints",
		[ShelfWord.Yours] = "Your blueprints",
		[ShelfWord.Pool] = "Shared in this world",
		[ShelfWord.Players] = "Everyone who shared",
		[ShelfWord.You] = "(you)",
		[ShelfWord.Home] = "Blueprints",
		[ShelfWord.Top] = "the top folder",
		[ShelfWord.NewFolder] = "New folder",
		[ShelfWord.NewFolderTopic] = "Name of the new folder",
		[ShelfWord.RenameTopic] = "New name (folder/name moves it, from the top folder)",
		[ShelfWord.Share] = "Share",
		[ShelfWord.ShareAll] = "Share all",
		[ShelfWord.Take] = "Take a copy",
		[ShelfWord.Rename] = "Rename",
		[ShelfWord.Delete] = "Delete",
		[ShelfWord.Remove] = "Remove",
		[ShelfWord.Close] = "Close",
		[ShelfWord.Asking] = "Asking the server...",
		[ShelfWord.Hint] = "Drag blueprints and folders across to share them or take a copy, onto a folder to move them. Double click opens, right click or F2 renames, Ctrl / Shift + click picks several.",
		[ShelfWord.Sharing] = "Sharing: $1 to go",
		[ShelfWord.Taking] = "Taking copies: $1 to go",
		[ShelfWord.DeleteAsk] = "Delete $1 from your blueprints? Folders go with everything in them.",
		[ShelfWord.RemoveAsk] = "Remove $1 from the shared ones? Folders go with everything in them.",
		[ShelfWord.Deleted] = "Deleted $1",
		[ShelfWord.OnlyOwn] = "Blueprints are shared into your own part",
		[ShelfWord.Nothing] = "No blueprints in what you picked",
		[ShelfWord.FolderMade] = "Folder $1 made",
		[ShelfWord.Renamed] = "$1 is now $2",
		[ShelfWord.Moved] = "Moved $1 into $2",
		[ShelfWord.MoveRefused] = "$1 not moved: $2",
		[ShelfWord.Shared] = "Shared $1 blueprints",
		[ShelfWord.MoreRefused] = "$1 more not shared",
		[ShelfWord.Took] = "Took $1 blueprints into $2",
		[ShelfWord.NotTaken] = "$1 blueprints could not be taken",
		[ShelfWord.TooBig] = "$1 is too big to share",
		[ShelfWord.NotReadable] = "$1 is not a readable blueprint",
		[ShelfWord.Full] = "$1 not shared: a player can share at most $2 blueprints",
		[ShelfWord.NotYours] = "Only the player who shared it or an admin can change $1",
		[ShelfWord.Gone] = "$1 is no longer shared",
		[ShelfWord.Removed] = "Removed $1 from the shared ones ($2 blueprints)",
		[ShelfWord.Unknown] = "$1 not shared: the server does not know your character yet",
		[ShelfWord.Failed] = "$1: the server could not change its files",
		[ShelfWord.NoAnswer] = "The server did not answer (it needs the same mod)",
		[ShelfWord.BadName] = "'$1' cannot be a file or folder name",
		[ShelfWord.Exists] = "$1 exists already",
		[ShelfWord.IntoItself] = "The folder $1 cannot move into itself",
		[ShelfWord.Disabled] = "This is switched off on the server",
	};

	public static string Get(string key) => Words.TryGetValue(key, out string text) ? text : key;
}

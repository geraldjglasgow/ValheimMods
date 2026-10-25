namespace BlueprintShelf;

/// <summary>
/// The words of a shelf, by name. A host's <see cref="ShelfWords.Key"/> turns each into its own key; the names are the
/// ones OpenKeep's bench keys end with (<c>ok_bench_share</c>, <c>ok_bp_newfolder</c>), so it maps them in one line.
/// Texts with values carry $1, $2.
/// </summary>
public static class ShelfWord
{
	/// <summary>The window's title.</summary>
	public const string Title = "title";
	/// <summary>The headers of the two sides, and the pool's top list of players.</summary>
	public const string Yours = "yours";
	public const string Pool = "pool";
	public const string Players = "players";
	/// <summary>After the local player's name in that list.</summary>
	public const string You = "you";
	/// <summary>The top of the player's own files, first in their folder trail.</summary>
	public const string Home = "home";
	/// <summary>"the top folder", in messages.</summary>
	public const string Top = "top";

	public const string NewFolder = "newfolder";
	public const string NewFolderTopic = "newfolder_topic";
	public const string RenameTopic = "rename_topic";
	public const string Share = "share";
	public const string ShareAll = "shareall";
	public const string Take = "take";
	public const string Rename = "rename";
	public const string Delete = "delete";
	public const string Remove = "remove";
	public const string Close = "close";

	public const string Asking = "asking";
	public const string Hint = "hint";
	public const string Sharing = "sharing";
	public const string Taking = "taking";
	public const string DeleteAsk = "deleteask";
	public const string RemoveAsk = "removeask";
	public const string Deleted = "deleted";
	public const string OnlyOwn = "onlyown";
	public const string Nothing = "nothing";
	public const string FolderMade = "foldermade";
	public const string Renamed = "renamed";
	public const string Moved = "moved";
	public const string MoveRefused = "moverefused";

	public const string Shared = "shared";
	public const string MoreRefused = "morerefused";
	public const string Took = "took";
	public const string NotTaken = "nottaken";
	public const string TooBig = "toobig";
	public const string NotReadable = "notblueprint";
	public const string Full = "full";
	public const string NotYours = "notyours";
	public const string Gone = "gone";
	public const string Removed = "removed";
	public const string Unknown = "unknown";
	public const string Failed = "failed";
	public const string NoAnswer = "noanswer";
	public const string BadName = "badname";
	public const string Exists = "exists";
	public const string IntoItself = "intoitself";
	public const string Disabled = "disabled";

	/// <summary>Every word, for the check of words a server sends (<see cref="ShelfWords.Read"/>).</summary>
	public static readonly string[] All =
	{
		Title, Yours, Pool, Players, You, Home, Top, NewFolder, NewFolderTopic, RenameTopic, Share, ShareAll, Take, Rename,
		Delete, Remove, Close, Asking, Hint, Sharing, Taking, DeleteAsk, RemoveAsk, Deleted, OnlyOwn, Nothing, FolderMade,
		Renamed, Moved, MoveRefused, Shared, MoreRefused, Took, NotTaken, TooBig, NotReadable, Full, NotYours, Gone, Removed,
		Unknown, Failed, NoAnswer, BadName, Exists, IntoItself, Disabled,
	};
}

using System.Collections.Generic;

namespace BlueprintShelf;

/// <summary>
/// The player's own blueprints on this machine, the left side of the window. A blueprint or folder is named by its path
/// from the top with "/" ("houses/barn", no extension). Every refusal comes back as a message for the player. A host whose
/// own code keeps a cache of the same files implements this over it, so both follow every change; others use
/// <see cref="ShelfFolder"/>.
/// </summary>
public interface IShelfFiles
{
	/// <summary>Goes up whenever a listing changed or something was made, renamed, moved, deleted or written.</summary>
	int Version { get; }

	bool Exists(string folder);

	/// <summary>The folders right inside a folder ("" the top), sorted.</summary>
	List<string> Folders(string folder);

	/// <summary>The blueprints right inside a folder, sorted.</summary>
	List<string> Files(string folder);

	/// <summary>Every blueprint below the top, sorted.</summary>
	List<string> Everything();

	/// <summary>A blueprint's file on disk.</summary>
	string FileOf(string path);

	/// <summary>The blueprint can be read; only readable ones are shared.</summary>
	bool Readable(string path);

	/// <summary>The path a typed name means in a folder (with "/", a path from the top); null with why not.</summary>
	string? Target(string typed, string folder, out string? error);

	bool CreateFolder(string parent, string typed, out string path, out string? error);

	/// <summary>Renames or moves a blueprint or folder to what was typed; false with why (no reason: the name is unchanged).</summary>
	bool Rename(string from, bool folder, string typed, out string to, out string? error);

	/// <summary>Deletes a blueprint, or a folder with everything in it.</summary>
	bool Delete(string path, bool folder, out string? error);

	/// <summary>A file was written from outside (a copy taken from the pool): list again.</summary>
	void Changed();
}

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace BlueprintShelf;

/// <summary>
/// Everything of the host's in a shelf: its names (log, objects, RPCs, the server's pool folder), its files, words, icons,
/// limits and switch, and the row of its own buttons under the two sides. Every peer makes the shelf with the same
/// settings; the server reads <see cref="RpcPrefix"/>, <see cref="PoolFolder"/>, <see cref="Extension"/>,
/// <see cref="Enabled"/>, <see cref="Accepts"/> and <see cref="Limits"/>.
/// </summary>
public sealed class ShelfSettings
{
	/// <summary>The shelf in the log ("OpenKeep blueprint bench").</summary>
	public string Name = "Blueprint shelf";

	/// <summary>The window's canvas object, under the game's GUI.</summary>
	public string CanvasName = "BlueprintShelf";

	/// <summary>Every routed RPC is this and its own name ("OpenKeep_Bench": OpenKeep_BenchList ...); one per mod.</summary>
	public string RpcPrefix = "BlueprintShelf_";

	/// <summary>The server's pool: BepInEx/config/&lt;PoolFolder&gt;/&lt;world&gt;/&lt;player&gt;_&lt;character id&gt;/...</summary>
	public string PoolFolder = "BlueprintShelf.Shared";

	/// <summary>A blueprint file's extension, with the dot, here and in the pool.</summary>
	public string Extension = ".json";

	/// <summary>The player's own blueprints (<see cref="ShelfFolder"/> for a plain folder of files).</summary>
	public IShelfFiles Files = null!;

	public ShelfWords Words = new();

	public ShelfLimits Limits = new();

	public ManualLogSource? Log;

	/// <summary>The shelf is on: the server refuses every request while it is off, and an open window closes.</summary>
	public Func<bool> Enabled = () => true;

	/// <summary>A line's shown name from its file or folder name.</summary>
	public Func<string, string> Title = name => name;

	/// <summary>A kind of line's icon; null (or no host function) for the library's own.</summary>
	public Func<ShelfKind, Sprite?>? Icon;

	/// <summary>Server: whether a received file may be stored (by default a JSON object).</summary>
	public Func<byte[], bool> Accepts = JsonObject;

	/// <summary>The host's own buttons, in a row under the two sides; none, no row.</summary>
	public List<ShelfButton> Buttons = new();

	/// <summary>Renames the one line picked in the side last used.</summary>
	public KeyCode RenameKey = KeyCode.F2;

	/// <summary>The window closes when the player is farther than this from the station, metres.</summary>
	public float Reach = 6f;

	/// <summary>A JSON object: its first character that is not white space (or a byte order mark) is "{".</summary>
	public static bool JsonObject(byte[] data)
	{
		foreach (byte b in data)
		{
			if (b == '{')
			{
				return true;
			}
			if (b != ' ' && b != '\t' && b != '\r' && b != '\n' && b != 0xEF && b != 0xBB && b != 0xBF)
			{
				return false;
			}
		}
		return false;
	}
}

/// <summary>A shelf's fixed limits; the defaults are OpenKeep's bench's.</summary>
public sealed class ShelfLimits
{
	/// <summary>One network part of a packed file, bytes.</summary>
	public int PartBytes = 32 * 1024;

	/// <summary>The largest file that can be shared, bytes.</summary>
	public int MaxFileBytes = 4 * 1024 * 1024;

	/// <summary>The most parts a packed file may come in.</summary>
	public int MaxParts = 64;

	/// <summary>The most blueprints one player may have shared in a world.</summary>
	public int MaxPerPlayer = 250;

	/// <summary>Parts sent per frame while sharing, and files asked for at once while taking.</summary>
	public int PartsPerFrame = 2;
	public int TakesInFlight = 3;

	/// <summary>A transfer the server stops answering is given up after this many seconds.</summary>
	public float Timeout = 30f;

	/// <summary>The longest name or path the name box and the pool take.</summary>
	public int NameLimit = 120;
}

/// <summary>A button: its word, when it can be pressed (asked every frame the window shows) and what it does.</summary>
public sealed class ShelfButton
{
	public ShelfButton(string word, Func<bool> enabled, Action run)
	{
		Word = word;
		Enabled = enabled;
		Run = run;
	}

	/// <summary>A host button's label, read through <see cref="ShelfWords.Text"/> (a key, or the text itself).</summary>
	public string Word { get; }
	public Func<bool> Enabled { get; }
	public Action Run { get; }
}

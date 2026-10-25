using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BlueprintShelf;

/// <summary>
/// Where a shelf's words come from. <see cref="Key"/> turns a <see cref="ShelfWord"/> into the host's own key (a
/// Translations key, say) and <see cref="Text"/> reads a key in the player's language; by default the key is the word and
/// the text is the library's English (<see cref="ShelfEnglish"/>), so a host sets only what it words its own way. A word
/// the server answers with travels as its token, "$" and the key (<see cref="Token"/>), and is read on the receiving
/// machine in its own language (<see cref="Read"/>). Set both before the shelf is made.
/// </summary>
public sealed class ShelfWords
{
	private HashSet<string>? keys;

	/// <summary>A word's key in the host's word table.</summary>
	public Func<string, string> Key = word => word;

	/// <summary>A key's text in the player's language (a host button's word goes through it as it is).</summary>
	public Func<string, string> Text = ShelfEnglish.Get;

	/// <summary>The library's English with the host's own texts (by word) where it has them.</summary>
	public static ShelfWords English(IDictionary<string, string> own) =>
		new ShelfWords { Text = key => own.TryGetValue(key, out string text) ? text : ShelfEnglish.Get(key) };

	public string Get(string word) => Text(Key(word));

	public string Format(string word, params object?[] values) => Fill(Get(word), values);

	/// <summary>The word as it travels to another machine.</summary>
	public string Token(string word) => "$" + Key(word);

	/// <summary>A token a server sent, read here with its values; null when it is no word of this shelf.</summary>
	public string? Read(string token, params object?[] values)
	{
		if (string.IsNullOrEmpty(token) || token[0] != '$')
		{
			return null;
		}
		keys ??= new HashSet<string>(ShelfWord.All.Select(Key), StringComparer.Ordinal);
		string key = token.Substring(1);
		return keys.Contains(key) ? Fill(Text(key), values) : null;
	}

	/// <summary>Replaces $1, $2 ... in a text, the highest first so $1 never eats $10.</summary>
	public static string Fill(string text, params object?[] values)
	{
		for (int i = values.Length; i >= 1; i--)
		{
			text = text.Replace("$" + i, Convert.ToString(values[i - 1], CultureInfo.CurrentCulture) ?? "");
		}
		return text;
	}
}

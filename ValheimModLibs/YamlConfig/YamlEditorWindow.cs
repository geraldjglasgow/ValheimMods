using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YamlConfig;

/// <summary>
/// The in-game IMGUI editor for one <see cref="YamlFileSet"/>: a text area per file, live validation, and save
/// or apply through the hub. Call <see cref="Update"/> from your plugin's Update and LateUpdate (it keeps the
/// cursor free), <see cref="OnGUI"/> from OnGUI, and <see cref="DrawButtons"/> from a ConfigurationManager custom
/// drawer to offer one open button per set.
/// </summary>
public sealed class YamlEditorWindow
{
	private const float ValidationHeight = 100f;

	private readonly YamlFileHub hub;
	private readonly string windowTitle;
	private readonly List<string> paths = new();
	private readonly List<string> texts = new();
	private readonly List<bool> expanded = new();
	private readonly List<Vector2> scrolls = new();
	private readonly List<string> errors = new();
	private readonly List<string> warnings = new();
	private Vector2 validationScroll;
	private YamlFileSet? set;
	private Texture2D? background;
	private GUIStyle? errorStyle;

	/// <param name="hub">The hub whose sets are edited.</param>
	/// <param name="windowTitle">Heading of the window, passed through <see cref="Translate"/>.</param>
	public YamlEditorWindow(YamlFileHub hub, string windowTitle)
	{
		this.hub = hub;
		this.windowTitle = windowTitle;
	}

	/// <summary>Caption lookup for the window title, button captions and messages; identity by default.</summary>
	public Func<string, string> Translate { get; set; } = caption => caption;

	/// <summary>True while a set is being edited.</summary>
	public bool IsOpen => set is not null;

	/// <summary>Opens a set for editing with its current file contents.</summary>
	public void Open(YamlFileSet set)
	{
		this.set = set;
		paths.Clear();
		texts.Clear();
		expanded.Clear();
		scrolls.Clear();
		foreach (KeyValuePair<string, string> file in set.Files)
		{
			paths.Add(file.Key);
			texts.Add(file.Value);
			expanded.Add(true);
			scrolls.Add(Vector2.zero);
		}
		Validate();
	}

	/// <summary>Closes the window without applying.</summary>
	public void Close() => set = null;

	/// <summary>One button per registered set that has files, each opening that set. For a ConfigurationManager drawer.</summary>
	public void DrawButtons()
	{
		foreach (YamlFileSet candidate in hub.Sets.Where(s => s.Files.Count > 0))
		{
			if (GUILayout.Button(Translate(candidate.EditorLabel()), GUILayout.ExpandWidth(true)))
			{
				Open(candidate);
			}
		}
	}

	/// <summary>Keeps the cursor unlocked and visible while the window is open.</summary>
	public void Update()
	{
		if (IsOpen)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	/// <summary>Draws the window while open.</summary>
	public void OnGUI()
	{
		if (set is null)
		{
			return;
		}
		HandleEditingKeys();
		GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Background);
		GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
		GUILayout.Label(Translate(windowTitle));
		DrawTopRow();
		for (int i = 0; i < texts.Count; i++)
		{
			DrawFile(i);
		}
		DrawValidation();
		GUILayout.EndArea();
	}

	private void DrawTopRow()
	{
		GUILayout.BeginHorizontal();
		bool canApply = hub.IsAuthor && errors.Count == 0;
		GUI.enabled = canApply;
		if (GUILayout.Button(Translate("Save and apply")))
		{
			Submit(saveToDisk: true);
		}
		if (GUILayout.Button(Translate("Apply without saving")))
		{
			Submit(saveToDisk: false);
		}
		GUI.enabled = true;
		if (GUILayout.Button(Translate("Discard")))
		{
			Close();
		}
		GUILayout.EndHorizontal();
	}

	private void Submit(bool saveToDisk)
	{
		if (set is null)
		{
			return;
		}
		Dictionary<string, string> files = new();
		for (int i = 0; i < paths.Count; i++)
		{
			files[paths[i]] = texts[i];
		}
		hub.Replace(set, files, saveToDisk);
		Close();
	}

	private void DrawFile(int index)
	{
		if (texts.Count > 1)
		{
			expanded[index] = GUILayout.Toggle(expanded[index], System.IO.Path.GetFileName(paths[index]));
			if (!expanded[index])
			{
				return;
			}
		}
		scrolls[index] = GUILayout.BeginScrollView(scrolls[index], GUILayout.ExpandHeight(true));
		GUI.SetNextControlName(ControlName(index));
		string edited = GUILayout.TextArea(texts[index], GUILayout.ExpandHeight(true));
		GUILayout.EndScrollView();
		if (edited != texts[index])
		{
			texts[index] = edited;
			Validate();
		}
	}

	private void DrawValidation()
	{
		validationScroll = GUILayout.BeginScrollView(validationScroll, GUILayout.Height(ValidationHeight));
		if (errors.Count == 0 && warnings.Count == 0)
		{
			GUILayout.Label(Translate("Configuration is valid."));
		}
		foreach (string error in errors)
		{
			GUILayout.Label(error, ErrorStyle);
		}
		foreach (string warning in warnings)
		{
			GUILayout.Label(warning);
		}
		GUILayout.EndScrollView();
	}

	private void Validate()
	{
		errors.Clear();
		warnings.Clear();
		if (set is null)
		{
			return;
		}
		YamlModel model = set.CreateModel();
		model.LoadAll(paths.Select((path, i) => new KeyValuePair<string, string>(path, texts[i])));
		errors.AddRange(model.Errors);
		warnings.AddRange(model.Warnings);
	}

	/// <summary>Tab inserts two spaces; Enter inserts a newline plus the indentation of the current line.</summary>
	private void HandleEditingKeys()
	{
		Event current = Event.current;
		if (current.type != EventType.KeyDown)
		{
			return;
		}
		bool tab = current.keyCode == KeyCode.Tab || current.character == '\t';
		bool enter = current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter || current.character == '\n';
		int index = FocusedFile();
		if (index < 0 || !(tab || enter))
		{
			return;
		}
		TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
		editor.ReplaceSelection(tab ? "  " : "\n" + IndentationOfLineAt(editor.text, editor.cursorIndex));
		texts[index] = editor.text;
		current.Use();
		Validate();
	}

	private int FocusedFile()
	{
		string focused = GUI.GetNameOfFocusedControl();
		for (int i = 0; i < texts.Count; i++)
		{
			if (focused == ControlName(i))
			{
				return i;
			}
		}
		return -1;
	}

	private static string IndentationOfLineAt(string text, int position)
	{
		int lineStart = Math.Max(0, Math.Min(position, text.Length));
		while (lineStart > 0 && text[lineStart - 1] != '\n')
		{
			lineStart--;
		}
		int end = lineStart;
		while (end < text.Length && text[end] == ' ')
		{
			end++;
		}
		return text.Substring(lineStart, end - lineStart);
	}

	private static string ControlName(int index) => "YamlEditorWindow.File" + index;

	private Texture2D Background
	{
		get
		{
			if (background == null)
			{
				background = new Texture2D(1, 1);
				background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.9f));
				background.Apply();
			}
			return background;
		}
	}

	private GUIStyle ErrorStyle => errorStyle ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
}

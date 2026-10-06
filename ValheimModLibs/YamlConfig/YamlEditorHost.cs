using UnityEngine;

namespace YamlConfig;

/// <summary>
/// Draws a <see cref="YamlEditorWindow"/> from a component of its own that is enabled only while the window is open.
/// Unity runs its IMGUI pass every frame for every enabled behaviour that has an <c>OnGUI</c>, even one that draws
/// nothing, so a plugin should have no always-on <c>OnGUI</c> for an editor that is almost always closed. Opening the
/// window enables its host, closing it disables it again; while open the host also keeps the cursor free (the window's
/// <see cref="YamlEditorWindow.Update"/> from Update and LateUpdate). The SyncedConfig facade adds one for its editor to
/// the plugin's object, so a mod built on it needs no <c>OnGUI</c> or <c>Update</c> for the editor at all.
/// </summary>
public sealed class YamlEditorHost : MonoBehaviour
{
	private YamlEditorWindow? window;
	private bool laidOut;

	/// <summary>Adds a host for the window to an object (the plugin's own); from then on the window draws only through it.</summary>
	public static YamlEditorHost Add(GameObject on, YamlEditorWindow window)
	{
		YamlEditorHost host = on.AddComponent<YamlEditorHost>();
		host.window = window;
		window.Host = host;
		host.enabled = window.IsOpen;
		return host;
	}

	private void Update() => window?.Update();

	private void LateUpdate() => window?.Update();

	private void OnDisable() => laidOut = false;

	/// <summary>
	/// The window opens from a button in another behaviour's OnGUI (the configuration manager), so the host can be
	/// enabled halfway through an event's passes; it draws from the next Layout event on, never an event whose layout
	/// pass it missed.
	/// </summary>
	private void OnGUI()
	{
		if (!laidOut && Event.current.type != EventType.Layout)
		{
			return;
		}
		laidOut = true;
		window?.Draw();
	}
}

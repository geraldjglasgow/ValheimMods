using UnityEngine;

namespace HaloRing.Pointing;

/// <summary>The game's own UI scale (screen size and the UI Scale setting together): the scale factor of the Hud's
/// root canvas, or the screen height over 1080 without a Hud (the main menu).</summary>
internal static class GameUiScale
{
	public static float Factor()
	{
		Canvas? hudCanvas = Hud.instance != null && Hud.instance.m_rootObject != null
			? Hud.instance.m_rootObject.GetComponentInParent<Canvas>()
			: null;
		return hudCanvas != null ? hudCanvas.scaleFactor : Mathf.Max(Screen.height / 1080f, 0.01f);
	}
}

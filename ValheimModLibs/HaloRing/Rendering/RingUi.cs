using UnityEngine;
using UnityEngine.UI;

namespace HaloRing.Rendering;

/// <summary>The ring's small UI parts: centred rects and the centre texts.</summary>
internal static class RingUi
{
	public static RectTransform NewRect(string name, Transform parent, Vector2 anchoredPosition)
	{
		var go = new GameObject(name, typeof(RectTransform));
		var rect = (RectTransform)go.transform;
		rect.SetParent(parent, worldPositionStays: false);
		rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = Vector2.zero;
		return rect;
	}

	/// <summary>A centred line of text, white or the given colour, that takes no clicks.</summary>
	public static Text NewText(string name, Transform parent, Vector2 at, Vector2 size, int fontSize, Color color)
	{
		RectTransform rect = NewRect(name, parent, at);
		rect.sizeDelta = size;
		Text text = rect.gameObject.AddComponent<Text>();
		text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		text.alignment = TextAnchor.MiddleCenter;
		text.fontSize = fontSize;
		text.color = color;
		text.raycastTarget = false;
		return text;
	}
}

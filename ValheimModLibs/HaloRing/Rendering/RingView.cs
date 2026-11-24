using System.Collections.Generic;
using HaloRing.Pointing;
using HaloRing.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace HaloRing.Rendering;

/// <summary>
/// One ring on screen: its own overlay Canvas, named after the calling mod and the ring (<c>"&lt;owner&gt; Ring
/// [&lt;name&gt;]"</c>, so two mods' copies never share an object), a pooled row of <see cref="SegmentView"/>, the
/// centre label and the line under it. Built once (the segments again only when their count changes) and shown or
/// hidden on open and close.
/// </summary>
internal sealed class RingView
{
	private const int SortingOrder = 100;
	private static readonly Color SublabelColor = new Color(0.8f, 0.85f, 1f, 1f);

	private readonly GameObject canvasObject;
	private readonly RectTransform root;
	private readonly Text centerLabel;
	private readonly Text centerSublabel;
	private readonly List<SegmentView> segments = new();
	private string? shownLabel;
	private string? shownSublabel;
	private bool labelShown;

	public int Count => segments.Count;

	public RingView(string objectName)
	{
		canvasObject = new GameObject(objectName);
		Object.DontDestroyOnLoad(canvasObject);
		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SortingOrder;
		CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
		scaler.scaleFactor = 1f;
		root = (RectTransform)canvasObject.transform;
		centerLabel = RingUi.NewText("CenterLabel", root, Vector2.zero, new Vector2(300f, 60f), 20, Color.white);
		centerSublabel = RingUi.NewText("CenterSublabel", root, new Vector2(0f, -24f), new Vector2(300f, 30f), 15, SublabelColor);
		centerSublabel.gameObject.SetActive(false);
		canvasObject.SetActive(false);
	}

	public void Show() => canvasObject.SetActive(true);

	public void Hide() => canvasObject.SetActive(false);

	public void Rebuild(int segmentCount)
	{
		foreach (SegmentView old in segments)
			Object.Destroy(old.gameObject);
		segments.Clear();
		for (int i = 0; i < segmentCount; i++)
			segments.Add(SegmentView.Create(root));
	}

	public void ApplyLayout(RingLayout layout)
	{
		for (int i = 0; i < segments.Count && i < layout.Segments.Length; i++)
			segments[i].SetGeometry(layout.Segments[i], layout.InnerRadius, layout.OuterRadius);
	}

	public void ApplySlots(RingSlots slots)
	{
		for (int i = 0; i < segments.Count; i++)
			segments[i].SetContent(slots.At(i)?.Icon, slots.IsFilled(i));
	}

	public void Tick(int? highlightedIndex, RingSlots slots, HoverLook look, bool showLabel, float deltaTime)
	{
		for (int i = 0; i < segments.Count; i++)
		{
			bool highlighted = highlightedIndex == i && slots.IsFilled(i);
			segments[i].Tick(highlighted, slots.IsEnabledAt(i), look, deltaTime);
		}
		HaloRingItem? item = showLabel && highlightedIndex.HasValue ? slots.At(highlightedIndex.Value) : null;
		UpdateCenter(item?.Label, item?.Sublabel);
	}

	public void ShakeSlot(int index)
	{
		if (index >= 0 && index < segments.Count)
			segments[index].TriggerShake();
	}

	/// <summary>The centre texts, written only when they change, not every frame the ring is open.</summary>
	private void UpdateCenter(string? label, string? sublabel)
	{
		if (labelShown && label == shownLabel && sublabel == shownSublabel)
			return;
		labelShown = true;
		shownLabel = label;
		shownSublabel = sublabel;
		centerLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
		centerLabel.text = label ?? string.Empty;
		centerSublabel.gameObject.SetActive(!string.IsNullOrEmpty(sublabel));
		centerSublabel.text = sublabel ?? string.Empty;
	}
}

using System;
using System.Collections.Generic;
using HaloRing.Layout;
using HaloRing.Pointing;
using HaloRing.Rendering;
using HarmonyLib;
using UnityEngine;

namespace HaloRing;

/// <summary>
/// One radial ring a mod drives itself: the caller decides when it opens (<see cref="Open"/>, from a held key or
/// button of its own), ticks it once a frame while open (<see cref="Tick"/>), and asks for the pick when its button is
/// let go (<see cref="Release"/>) or cancels it. The direction of the mouse from the screen centre, or of the gamepad
/// stick, picks a segment, even past the rim; the centre (<see cref="HaloRingStyle.DeadZone"/>) picks nothing. While
/// open the cursor is free and this copy's WindowInput holds the game's input like a window (no attacks, hotbar keys
/// or mouse look; walking stays), Esc cancels, and a game screen opening (inventory, map, menu, chat) cancels it. Only
/// one ring is open at a time, across every mod's copy. No config and no keys: the caller owns both. Client only.
/// </summary>
public sealed class HaloRingMenu
{
	private readonly RingView view;
	private readonly HysteresisSelector selector = new();
	private readonly Action<string>? report;
	private HaloRingStyle style = new();
	private HoverLook look;
	private RingLayout? layout;
	private RingSlots? slots;
	private bool layoutDirty = true;
	private float lastGameUiScale = -1f;
	private int? highlighted;

	/// <param name="owner">The calling mod's name; with <paramref name="name"/> it names the ring's canvas
	/// (<c>"&lt;owner&gt; Ring [&lt;name&gt;]"</c>), so two mods never share an object.</param>
	/// <param name="name">The ring's name within the mod.</param>
	/// <param name="report">Where a throwing <see cref="HaloRingItem.IsEnabled"/> is reported (the mod's log).</param>
	public HaloRingMenu(string owner, string name, Action<string>? report = null)
	{
		view = new RingView($"{owner} Ring [{name}]");
		this.report = report;
		look = new HoverLook(style);
	}

	/// <summary>Once in the mod's Awake, with the mod's own Harmony: the input hold-back (WindowInput) of this copy.</summary>
	public static void Install(Harmony harmony) => OpenRings.Install(harmony);

	/// <summary>A game screen is up or the local player is dead: a ring does not open now.</summary>
	public static bool Blocked => BlockingUi.IsBlocked();

	/// <summary>Keeps the game's input held back until that mouse button is let go (a click that closed a ring).</summary>
	public static void HoldUntilReleased(int mouseButton) => OpenRings.HoldUntilReleased(mouseButton);

	/// <summary>Size and look; the layout follows on the next open, the hover look at once.</summary>
	public HaloRingStyle Style
	{
		get => style;
		set
		{
			style = value ?? new HaloRingStyle();
			look = new HoverLook(style);
			layoutDirty = true;
		}
	}

	public bool IsOpen { get; private set; }

	/// <summary>The hovered segment (only a filled one), or null.</summary>
	public int? HighlightedIndex => highlighted;

	public HaloRingItem? Highlighted => highlighted.HasValue ? slots?.At(highlighted.Value) : null;

	/// <summary>A filled segment became the hovered one.</summary>
	public event Action<int>? HighlightChanged;

	/// <summary>The ring closed without a pick: <see cref="Cancel"/>, Esc, a game screen, another ring opening, or a
	/// release in the centre.</summary>
	public event Action? Cancelled;

	/// <summary>Cancels another open ring of this mod, as opening one does.</summary>
	public void CloseOthers() => OpenRings.CloseOthers(this);

	/// <summary>
	/// Opens the ring now with these items in segment order (segment 0 at <see cref="HaloRingStyle.StartAngle"/>, then
	/// counter-clockwise; a null leaves a segment empty; items past <paramref name="segmentCount"/> are left out).
	/// False when a game screen is up, the count is below 1, or another mod's ring is open.
	/// </summary>
	public bool Open(IReadOnlyList<HaloRingItem?> items, int segmentCount)
	{
		if (IsOpen)
			return true;
		if (segmentCount < 1 || BlockingUi.IsBlocked() || !OpenRings.Claim(this))
			return false;
		IsOpen = true;
		EnsureLayout(segmentCount);
		slots = RingSlots.Build(items, segmentCount, report);
		selector.Reset();
		highlighted = null;
		CursorHold.Suspend();
		view.ApplySlots(slots);
		view.Show();
		return true;
	}

	/// <summary>Once a frame while open: closes it on a game screen or Esc (false then), else follows the pointer.</summary>
	public bool Tick(float deltaTime)
	{
		if (!IsOpen || CloseIfBlocked() || CloseOnEscape())
			return false;
		Track(deltaTime);
		return true;
	}

	/// <summary>Cancels an open ring while a game screen is up; true when it did.</summary>
	public bool CloseIfBlocked()
	{
		if (!IsOpen || !BlockingUi.IsBlocked())
			return false;
		Cancel();
		return true;
	}

	/// <summary>Esc without the game's menu (the start screen; in game Esc reaches the ring through the menu).</summary>
	public bool CloseOnEscape()
	{
		if (!IsOpen || Menu.instance != null || !UnityEngine.Input.GetKeyDown(KeyCode.Escape))
			return false;
		Cancel();
		return true;
	}

	/// <summary>Follows the pointer and draws the hover look; nothing while closed.</summary>
	public void Track(float deltaTime)
	{
		if (!IsOpen || slots == null)
			return;
		UpdateSelection(slots);
		view.Tick(highlighted, slots, look, style.ShowLabel, deltaTime);
	}

	/// <summary>
	/// The pick: closes on the hovered item (<see cref="RingOutcome.Picked"/>); a disabled one shakes and the ring stays
	/// open (<see cref="RingOutcome.Refused"/>) unless <paramref name="refuseDisabled"/> is false; nothing hovered
	/// cancels (<see cref="RingOutcome.Cancelled"/>, and <see cref="Cancelled"/> is raised).
	/// </summary>
	public HaloRingPick Release(bool refuseDisabled = true)
	{
		HaloRingItem? item = IsOpen ? Highlighted : null;
		int index = highlighted ?? -1;
		if (item == null)
		{
			Cancel();
			return HaloRingPick.None;
		}
		if (refuseDisabled && slots != null && !slots.IsEnabledAt(index))
		{
			view.ShakeSlot(index);
			return new HaloRingPick(RingOutcome.Refused, item, index);
		}
		Shut();
		return new HaloRingPick(RingOutcome.Picked, item, index);
	}

	/// <summary>Closes the ring without a pick and raises <see cref="Cancelled"/>; nothing when closed.</summary>
	public void Cancel()
	{
		if (!IsOpen)
			return;
		Shut();
		Cancelled?.Invoke();
	}

	private void Shut()
	{
		IsOpen = false;
		view.Hide();
		CursorHold.Restore();
		OpenRings.Closed(this);
	}

	/// <summary>The layout again only after a style change, a new segment count or a new screen scale.</summary>
	private void EnsureLayout(int segmentCount)
	{
		float gameUiScale = GameUiScale.Factor();
		bool scaleChanged = !Mathf.Approximately(gameUiScale, lastGameUiScale);
		if (!layoutDirty && !scaleChanged && layout != null && layout.SegmentCount == segmentCount)
			return;
		layoutDirty = false;
		lastGameUiScale = gameUiScale;
		layout = RingLayout.Build(style, segmentCount, style.UiScale * gameUiScale);
		if (view.Count != segmentCount)
			view.Rebuild(segmentCount);
		view.ApplyLayout(layout);
	}

	private void UpdateSelection(RingSlots filled)
	{
		RingLayout shape = layout!;
		Vector2 offset = RingPointer.Offset(style.Gamepad, style.Stick, shape.OuterRadius);
		int? raw = selector.Update(offset, shape.SegmentCount, shape.StartAngleOffset, shape.DeadZoneRadius, RingGeometry.HysteresisDegrees);
		int? next = raw.HasValue && filled.IsFilled(raw.Value) ? raw : null;
		if (next == highlighted)
			return;
		highlighted = next;
		if (highlighted.HasValue)
			HighlightChanged?.Invoke(highlighted.Value);
	}
}

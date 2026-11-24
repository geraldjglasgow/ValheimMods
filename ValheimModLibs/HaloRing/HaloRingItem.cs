using System;
using UnityEngine;

namespace HaloRing;

/// <summary>
/// One choice on a ring: an id the caller knows it by, the name shown in the middle while it is hovered, its icon, an
/// optional second line under the name (a cost, a count), and whether it can be picked. <see cref="IsEnabled"/>, when
/// set, is asked every frame the ring is open (a throw counts as enabled); <see cref="Enabled"/> is used without it. A
/// disabled choice is drawn dim and can still be hovered. <see cref="Tag"/> is the caller's own object, handed back
/// untouched. The ring never loads, copies or frees the sprite.
/// </summary>
public sealed class HaloRingItem
{
	public string Id;
	public string Label;
	public Sprite? Icon;
	public string? Sublabel;
	public bool Enabled = true;
	public Func<bool>? IsEnabled;
	public object? Tag;

	public HaloRingItem(string id, string label, Sprite? icon)
	{
		Id = id;
		Label = label;
		Icon = icon;
	}
}

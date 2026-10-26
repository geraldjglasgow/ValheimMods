using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace MannedWeapons;

/// <summary>
/// A placed weapon a player mans with the use key, aims with the view and fires with attack, on every peer. A mod
/// derives its weapon's component from this, puts it on the piece (beside its ZNetView) and gives it its
/// <see cref="Design"/>, its parts (<see cref="FindFrame"/>) and its shot (<see cref="Shoot"/>); the rest are hooks with
/// plain defaults. Call <see cref="Install"/> once in the mod's Awake.
/// <para>
/// The use key asks the weapon's owner for it, as the game's ship helm does (<see cref="WeaponHold"/>): granted when no
/// operator holds it, the operator written to the ZDO and the ZDO handed to him, so from then on his own machine aims,
/// reloads and fires and writes what every peer draws. He controls it through the game's doodad control (a helm, a
/// saddle): his movement keys do nothing, the view aims (within the arcs, <see cref="ViewLimit"/>), attack fires, the use
/// key, jump or dodge let go, and so does going too far or dying; his hand items are put away meanwhile and his body
/// passes through the weapon. His feet walk to the stand point with the game's own walk (<see cref="OperatorFeet"/>) and
/// his hands go to the grips (<see cref="OperatorHands"/>), on every screen.
/// </para>
/// </summary>
public abstract class MannedWeapon : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	private static readonly bool Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

	private readonly OperatorHands hands = new();
	private float shownYaw, shownPitch;
	internal bool holding;

	public ZNetView Net { get; private set; } = null!;

	/// <summary>The parts the library turns and reads; null when the weapon's model lacks them (it then does nothing).</summary>
	public WeaponFrame? Frame { get; private set; }

	/// <summary>The operator's own aim, on his machine.</summary>
	public WeaponAim Aim { get; } = new();

	/// <summary>The operator's feet, on his machine.</summary>
	public OperatorFeet Feet { get; } = new();

	public abstract WeaponDesign Design { get; }

	/// <summary>A fresh read of the ZDO; only on a valid object.</summary>
	public WeaponState State => WeaponState.Read(Net.GetZDO(), Design);

	/// <summary>
	/// Installs the operator's patches (his buttons, feet, facing and view), once per merged copy, by name. Returns the
	/// game members it could not find; then nothing is patched and no weapon can be manned.
	/// </summary>
	public static IReadOnlyList<string> Install(Harmony harmony) => WeaponPatches.Install(harmony);

	/// <summary>Esc lets go of a weapon too (before it would open the game menu); optional, once per merged copy.</summary>
	public static IReadOnlyList<string> InstallEscape(Harmony harmony) => WeaponEscape.Install(harmony);

	/// <summary>Whether `player` mans this weapon on this machine.</summary>
	public bool HeldBy(Player player) => holding && player == Player.m_localPlayer && player.GetDoodadController() == (IDoodadController)this;

	/// <summary>Whether someone mans it now, on any peer: the operator its ZDO names is in the world, alive and within <see cref="WeaponDesign.HolderRange"/>.</summary>
	public bool Manned => Net.IsValid() && WeaponHold.ValidUser(this, State.User);

	/// <summary>The body under the weapon (a ship's, or its own carriage's), whose motion its operator and its aim discount; null on solid ground.</summary>
	public Rigidbody? Carrier => GetComponentInParent<Rigidbody>();

	/// <summary>The way the operator faces once at his spot: where the weapon points, turned by <see cref="OperatorBody.Facing"/>.</summary>
	public Vector3 Ahead
	{
		get
		{
			if (Frame == null)
			{
				return transform.forward;
			}
			float facing = Design.Body.Facing;
			return facing == 0f ? Frame.Yaw.forward : Quaternion.AngleAxis(-facing, Frame.Yaw.up) * Frame.Yaw.forward;
		}
	}

	/// <summary>The weapon's parts, once, as it wakes (a placement ghost too); null when its model lacks them.</summary>
	protected abstract WeaponFrame? FindFrame();

	/// <summary>Where the operator's feet go now; by default the stand point.</summary>
	public virtual Vector3 StandPoint(in WeaponState s) => Frame!.Stand.position;

	/// <summary>The point the weapon's pitch is measured from when it lays itself on the crosshair; by default the yaw pivot.</summary>
	public virtual Vector3 AimPivot => Frame!.Yaw.position;

	/// <summary>The operator's machine fires the loaded weapon (spawns its shot, <see cref="WeaponShot"/>); false when it could not, and it stays loaded.</summary>
	protected internal abstract bool Shoot(Player shooter);

	/// <summary>Degrees the pitch part is thrown up now on top of the aim (a shot's kick); none by default.</summary>
	protected internal virtual float Kick(in WeaponState s) => 0f;

	/// <summary>The weapon's own look each frame on a peer that draws, after the turn and before the operator's hands.</summary>
	protected virtual void Draw(in WeaponState s)
	{
	}

	/// <summary>A placement ghost's look, once (it has no ZDO and is then left still).</summary>
	protected virtual void DrawAtRest()
	{
	}

	/// <summary>The operator's lean and his fists' targets now; by default upright, both on the grips.</summary>
	protected internal virtual OperatorPose Pose(in WeaponState s, OperatorRig rig, Transform body) =>
		new(0f, Frame?.GripLeft?.position, Frame?.GripRight?.position);

	/// <summary>After the operator's hands are posed (something carried in a fist follows it here).</summary>
	protected internal virtual void Gripped(in WeaponState s, OperatorRig rig, Transform body)
	{
	}

	/// <summary>No operator's hands are on the weapon this frame.</summary>
	protected internal virtual void Released(in WeaponState s)
	{
	}

	/// <summary>What the operator would load: by default an item of the reload's ammunition type in his inventory (none without one).</summary>
	protected internal virtual ItemDrop.ItemData? FindAmmo(Player player) =>
		Design.Reload.AmmoType.Length > 0 ? player.GetInventory().GetAmmoItem(Design.Reload.AmmoType) : null;

	/// <summary>A reload step begins: by default one item is taken as step <see cref="ReloadCycle.TakeAt"/> begins.</summary>
	protected internal virtual void TakeAmmo(Player player, ItemDrop.ItemData ammo, int step)
	{
		if (step == Design.Reload.TakeAt)
		{
			player.GetInventory().RemoveItem(ammo, 1);
		}
	}

	/// <summary>On the owner: whether the weapon may be taken at all now (false while it is being moved, say).</summary>
	protected internal virtual bool Free => true;

	/// <summary>On the machine of a player at the stand point, before asking the owner: why he may not take it now, said to him; null (by default): he may ask.</summary>
	protected internal virtual string? Refusal(Player player) => null;

	/// <summary>The operator's attack buttons this frame; by default a press of attack pulls the trigger (<see cref="Fire"/>).</summary>
	protected internal virtual void Buttons(Player player, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold)
	{
		if (attack)
		{
			Fire(player);
		}
	}

	/// <summary>The trigger, on the operator's machine: fires when loaded; with nothing loaded and nothing to load, says so.</summary>
	public void Fire(Player player) => WeaponReload.Trigger(this, player);

	/// <summary>The hover: name, loaded or empty, and the use key's take hold or let go when the player may.</summary>
	protected virtual string HoverText(in WeaponState s, bool mine, bool takeable)
	{
		WeaponWords words = Design.Words;
		StringBuilder text = new(words.Name);
		text.Append(s.Loaded ? " (" + words.Loaded + ")" : " (" + words.Empty + ")");
		if (takeable)
		{
			text.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ").Append(mine ? words.LetGo : words.Hold);
		}
		return text.ToString();
	}

	protected virtual void Awake()
	{
		Net = GetComponent<ZNetView>();
		Frame = FindFrame();
		if (Net.GetZDO() == null)
		{
			return;   // a placement ghost
		}
		WeaponHold.Register(this);
	}

	/// <summary>A placement ghost (no ZDO) is drawn at rest once and left still.</summary>
	protected virtual void Start()
	{
		if (Headless || (Net != null && Net.GetZDO() != null))
		{
			return;
		}
		if (Frame != null)
		{
			DrawAtRest();
		}
		enabled = false;
	}

	protected virtual void FixedUpdate()
	{
		if (holding && Net.IsValid() && Player.m_localPlayer is Player player && player.GetDoodadController() == (IDoodadController)this)
		{
			Safe.Run(Design.HoldContext, static (weapon, holder) => WeaponReload.Tick(weapon, holder), this, player);
		}
	}

	/// <summary>On every peer that draws: the turn, the weapon's own look, the operator's hands.</summary>
	protected virtual void LateUpdate()
	{
		if (Headless || Frame == null || !Net.IsValid())
		{
			return;
		}
		WeaponState s = State;
		Turn(Frame, s);
		Draw(s);
		hands.Pose(this, s, Time.deltaTime);
	}

	protected virtual void OnDestroy()
	{
		if (holding && Player.m_localPlayer != null)
		{
			GameAccess.ShowHandItems(Player.m_localPlayer);
		}
	}

	/// <summary>The operator's own aim, worked out here each frame; anyone else's eased toward the ZDO's; the kick on top.</summary>
	private void Turn(WeaponFrame frame, in WeaponState s)
	{
		if (Player.m_localPlayer != null && HeldBy(Player.m_localPlayer))
		{
			Aim.Step(this, Player.m_localPlayer, Time.deltaTime);
			(shownYaw, shownPitch) = (Aim.Yaw, Aim.Pitch);
		}
		else
		{
			float ease = 1f - Mathf.Exp(-Design.Aim.Follow * Time.deltaTime);
			(shownYaw, shownPitch) = (Mathf.LerpAngle(shownYaw, s.Yaw, ease), Mathf.LerpAngle(shownPitch, s.Pitch, ease));
		}
		float kick = Kick(s);
		frame.Yaw.localRotation = Quaternion.Euler(0f, shownYaw, 0f);
		frame.Pitch.localRotation = Quaternion.Euler(-(shownPitch + kick), 0f, 0f);
	}

	/// <summary>The granted player takes hold, on his own machine.</summary>
	internal void Take(Player player)
	{
		Aim.Begin(State);
		holding = true;
		Feet.Start();
		player.StartDoodadControl(this);
		player.HideHandItems(false, false);
		WeaponHold.Collide(this, player, false);
	}

	public bool Interact(Humanoid user, bool hold, bool alt) => WeaponHold.Interact(this, user, hold);

	public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

	public void OnUseStop(Player player)
	{
		holding = false;
		GameAccess.ShowHandItems(player);
		WeaponHold.Collide(this, player, true);
		if (!Net.IsValid())
		{
			return;
		}
		if (Net.IsOwner())
		{
			WeaponReload.LetGo(this);
		}
		else
		{
			Net.InvokeRPC(Design.Keys.Release, player.GetPlayerID());
		}
	}

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
	}

	public Component GetControlledComponent() => Frame != null ? Frame.Yaw : transform;

	public Vector3 GetPosition() => transform.position;

	public bool IsValid() => this != null && Net != null && Net.IsValid();

	public string GetHoverName() => Localization.instance.Localize(Design.Words.Name);

	public float GetHoverOffset() => 0f;

	public string GetHoverText()
	{
		if (!Net.IsValid())
		{
			return "";
		}
		WeaponState state = State;
		bool mine = Player.m_localPlayer != null && HeldBy(Player.m_localPlayer);
		return Localization.instance.Localize(HoverText(state, mine, mine || !WeaponHold.ValidUser(this, state.User)));
	}
}

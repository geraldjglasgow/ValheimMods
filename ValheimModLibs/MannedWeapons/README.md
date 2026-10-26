# MannedWeapons

A placed weapon a player mans with the use key, aims with the view and fires with attack: the Bone Ballista's way of
holding, aiming and walking round a weapon, made shareable (2026-10-26) and used by the AssetLab cannons the same day. The
weapon's model, its look and its particular motions stay in the mod; everything a manned weapon has in common is here.

- **Manning.** E asks the weapon's owner for it, as the game's ship helm does: granted when no valid operator holds it
  (one in the world, alive and within `HolderRange`) and the weapon is `Free`; the operator is written to the ZDO and the
  ZDO handed to him (`ForceSendZDO`, `SetOwner`, answer), so his own machine aims, reloads and fires from then on and
  writes what everyone draws. One operator at a time ("in use"); from within `UseRange` of the stand point, and from
  behind the yaw pivot when `Behind` is set. He controls it through the game's doodad control: movement keys do nothing,
  E, jump or dodge let go, and so does going past the game's 5 m or dying; Esc too with `InstallEscape`. Hand items put
  away meanwhile, his body passing through the weapon. Nobody takes one while sitting, steering or manning something
  else, and the weapon can say why not first (`Refusal`, said before asking); `Manned` tells any peer whether someone
  holds it.
- **Aim** (`WeaponArcs`). Yaw follows the view's yaw, pitch lays the weapon on what the crosshair is on (from
  `AimPivot`, out to `Reach`, passing through the body the weapon stands on, short of the sea with `StopAtSea`; measured
  from the plane the weapon stands on, so a base tilted on a slope or a rolling deck is taken off), within
  `YawLimit` each way of where it was placed (180 or more: all round) and `PitchDown`..`PitchUp`, at `TurnRate` and
  `TiltRate`. While manned the view itself stops at the arc's end, as the weapon does (no looking behind one that cannot
  turn there), turns no faster than `ViewRate` (0: free) and tilts within `LookUp`/`LookDown`. The aim goes to the ZDO
  when it moves more than `WriteStep`; every other peer eases toward it at `Follow`.
- **The operator's body** (`OperatorBody`). His feet walk to `StandPoint` with the game's own walk, steered by
  `m_moveDir` (a stand point on the yaw part swings round as the weapon turns, so turning it walks him round it); he
  stops near it and starts again only once it has moved on, gives up on a spot he cannot reach, and faces the way the
  weapon points (turned `Facing` degrees), so a turn plays as a side step. Kept in the weapon's own space and measured
  against the body it stands on, so a moving deck changes nothing. His hands go to the targets `Pose` gives (by default
  the frame's grips) by two-bone IK (LimbIk) after the animator, with the lean `Pose` gives, over the last stride up;
  elbows `Beside` (the ballista's) or `Square` (never flips as a fist comes back to the hip).
- **Reload and fire** (`ReloadCycle`). Steps that run one after another (`Steps`, seconds each), each begun when the one
  before has run and every peer drawing them from the moment in the ZDO; the cycle starts by itself when the operator
  carries ammunition (`AmmoType`, one item taken as step `TakeAt` begins) and the weapon has rested `Rest` after a shot.
  Attack fires a loaded weapon: `Shoot` spawns the shot, then the ZDO says empty and counts the shot. Letting go mid-step
  finishes the step at once (`FinishOnLetGo`, the default), or leaves it to run out by itself (false: a long reload is
  not cut short by letting go and taking hold again). `WeaponShot.Launch` spawns a networked projectile from the operator as the game's ballista
  does (`WeaponShot.Hit` makes the hit from an ammo item); with `InstallPassThrough` it passes through the body or the
  weapon it was fired from.

## A weapon

```csharp
public sealed class MyGun : MannedWeapon            // on the piece, beside its ZNetView
{
	public override WeaponDesign Design => MyDesigns.Gun;
	protected override WeaponFrame? FindFrame() =>    // once, as it wakes (a placement ghost too)
		new WeaponFrame(Find("yaw"), Find("pitch"), Find("stand"), Find("grip_l"), Find("grip_r"));
	protected override bool Shoot(Player shooter) { /* WeaponShot.Launch(...) */ return true; }
}

public static readonly WeaponDesign Gun = new("my gun", new WeaponKeys("my_gun"))
{
	Aim = new WeaponArcs { YawLimit = 25f, PitchDown = -8f, PitchUp = 20f, TurnRate = 35f, TiltRate = 30f, ViewRate = 35f },
	Reload = new ReloadCycle(2.5f) { AmmoType = "$ammo_my_ball", Rest = 0.25f },
	Words = new WeaponWords { Name = "$piece_my_gun", Loaded = "...", Empty = "...", Hold = "...", LetGo = "...", NoAmmo = "..." },
};

MannedWeapon.Install(harmony);                       // once in Awake; returns the game members it could not find
```

Hooks with plain defaults: `StandPoint(state)` (where the feet go now: the stand point), `AimPivot` (the yaw pivot),
`Kick(state)` (degrees thrown up on top of the pitch), `Draw(state)` (the weapon's own look each frame, after the turn and
before the hands), `DrawAtRest()` (a placement ghost), `Pose(state, rig, body)` (lean, fist targets, how fully he holds:
`OperatorPose`), `Gripped`/`Released` (after the hands are posed, or with nobody's on it), `FindAmmo`/`TakeAmmo`,
`Free` (the owner may grant it now), `Refusal(player)` (why he may not ask now, said to him; null by default),
`Buttons(player, attack, attackHold, secondaryAttack, secondaryAttackHold)` (by default a press of attack calls `Fire`),
`HoverText(state, mine, takeable)`. `State` reads the ZDO (`WeaponState`:
`User`, `Yaw`, `Pitch`, `Stage` (steps done), `Step` (under way, from 1), `StepTime`, `Shots`, `ShotAge`, `Settled`,
`Busy`, `Loaded`). `WeaponKeys(prefix)`: ZDO keys `prefix_user`, `_yaw`, `_pitch`, `_stage`, `_step`, `_step_at`,
`_shots`, `_shot_at` (the three reload ones nameable, so a weapon keeps the keys it already saved), RPCs
`prefix_request`, `_release`, `_answer`.

Patches, by hand and by name, once per merged copy, each acting only on this copy's weapons: `Player.SetControls`
prefix (attack buttons to `Buttons`, nothing else lets go but jump and dodge) and postfix (the feet), `Player.SetMouseLook`
postfix (the view held to the arc), `Character.UpdateRotation` (private) prefix (the facing); optional
`Menu.Update` prefix (`InstallEscape`) and `Projectile.OnHit` prefix (`WeaponShot.InstallPassThrough`). Private game
members by `AccessTools` (`GameAccess`), checked first (`GameTargets`). Nothing is drawn on a dedicated server.

## The cannons

The AssetLab cannons (`AssetLab/AssetLab/Cannons`, `CANNONS.md`) moved onto the library on 2026-10-26. Their arm framework
(`KwArm*`) had the owner keep the ZDO while the gunner sent his aim, shots and reloads to it by RPC, with a prediction of
his own round while the ZDO caught up; here the gunner owns the ZDO while he mans the cannon, so those calls, the
prediction and the cannons' own aim, feet, facing and arms went (`KwArmCrew`, `KwArmLoad`, `KwArmAim`, `KwArmFeet`,
`KwArmFacing`, `KwArmBody`/`KwArmLimb`, `KwArmGunner`, `KwFiredFrom`, `KwArmControls`' manning part). `KwArm` is the
`MannedWeapon`; `KwArmDesigns` makes each kind's design from its definition.

- **Field cannon** (wheels, 25 degrees each way) and **ship cannon** (four trucks, levered 20 degrees each way):
  `YawLimit` 25 / 20, `PitchDown`/`PitchUp` its elevation, `TurnRate` its turn speed and `ViewRate` the same (the
  ballista's feel: view, barrel and feet stop together at the arc's end), `TiltRate` its tilt speed, `StopAtSea` on,
  `AimPivot` the pitch pivot. The field cannon's stand point is on the piece beside the trail (only his hands follow the
  breech); the ship cannon's is on the yaw part, so he shuffles after the breech as the carriage is levered round, and
  `StandPoint` keeps it from rolling back with the carriage and adds the recoil's step aside (`KwShipRecoil`). `Facing` 20
  for the ship cannon. `Free` is false while the cannon is towed (`KwArmTow`) and `Refusal` says so ("Put it down first",
  "Someone is pulling it"); the tow's owner refuses a puller while `Manned`.
- **Mounted cannon** (on a mount, all round): `YawLimit` 180 and `ViewRate` 0, so the view turns freely and the barrel
  follows at `TurnRate` across the back (no stop). `AimPivot` the pitch pivot too.
- **All**: `Elbows = Square`; `Pose` from the grips (`Hands`, eased onto `ReloadHands` through a reload) with the lean from
  the grips' `Lean`, `MostLean` and `TiltLean` (the drawn barrel's tilt) and `Hold` falling while a ship cannon's carriage
  recoils. `ReloadCycle(shot.Reload - FireTime)` (one step: breech, ball, powder) with `Rest = FireTime` and
  `FinishOnLetGo` false; `FindAmmo`/`TakeAmmo` for the balls and powder (`KwShot.Charge`) of the trigger last pulled,
  which trigger the load is for and the ball kept in ZDO keys of the cannon's own (`al_arm_load_kind`, `al_arm_ammo`);
  `Buttons` for the secondary attack and held triggers, with the cannon's own words; `Shoot` writes which trigger fired
  (`al_arm_kind`) and holds the shot until `FireDelay` into the lanyard's pull (`KwArmLaunch`), when
  `WeaponShot.Launch(..., body: carrier, weapon: the cannon)` flies it with `InstallPassThrough`, the ship's speed added.
  The motion tracks, cords, recoil and muzzle effects are its `Draw`, read from `State` (`ShotAge`, `Busy`, `StepTime`);
  its keys are `WeaponKeys("al_arm", stage: "loaded", step: "step", stepAt: "load_at")`, so a loaded cannon stays loaded.

Offline (2026-10-26, a .NET 8 harness on a fake game, the cannons' real numbers read from the built AssetLab.dll), for all
nine: view and barrel held within the arc and stopping together at its end, the view no faster than the gun, mounted ones
free and following round the back; the stand points turning with the gun (a field cannon's staying put) and the feet
walking after them; the hands bone for bone as the old cannon code posed them; the shot the same as before from the muzzle,
passing through what it stands on; a shot every `Reload` seconds; the hand-off between two players over a server.

## Fixed on 2026-10-26

- **The aim on a tilted base.** The pitch was measured in the world and set on the pitch part, which tilts on the
  weapon's base, so a weapon whose base is tilted (a field cannon resting on a slope, anything on a rolling deck) laid
  its barrel off by the base's own tilt: offline, a cannon on a base pitched 6 degrees shot 6 degrees low. It is now
  measured from the base's own plane (the old cannon code's way): off by under 0.1 degree pitched 5 and rolled 5. On
  level ground nothing changes but float rounding (the ballista's ZDO aim by at most 0.000004 degrees).
- **The lean.** Until then the lean was skipped when the animator's pose was within about 0.16 degrees of the last
  leaned pose (`OperatorRig.Lean` took that as "the animator did not pose this frame", through `Quaternion.Angle`'s
  rounding), so while a lean came in from almost nothing (the hands easing on, a ship cannon's gunner taking hold again
  after the recoil) a frame could lose it entirely. It now compares the pose bit for bit. Checked against the ballista's
  scenarios: 9 frames of 3320 differ, each where the lean applied was about a degree or less; every other number is the
  same but for the aim's float rounding.
- **Taking hold while busy.** A player sitting, steering a ship or manning another weapon could take one (two doodad
  controls at once); he is now refused.

Depends on LimbIk and PatchGuard (merge both). Consumers: Elite Creatures Pack (the Bone Ballista, `Ballista/`) and the
AssetLab cannons (`AssetLab/AssetLab/Cannons/Arms`, keys `al_arm_*`).

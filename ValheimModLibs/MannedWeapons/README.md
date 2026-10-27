# MannedWeapons

A placed weapon a player mans with the use key, aims with the view and fires with attack: the Bone Ballista's way of
holding, aiming and walking round a weapon, made shareable (2026-10-26) and used by the AssetLab cannons the same day (Broadside's ship cannons since they graduated). The
weapon's model, its look and its particular motions stay in the mod; everything a manned weapon has in common is here.

- **Manning.** E asks the weapon's owner for it, as the game's ship helm does: granted when no valid operator holds it
  (one in the world, alive and within `HolderRange` of the stand point) and the weapon is `Free`; the operator is written
  to the ZDO and the ZDO handed to him (`ForceSendZDO`, `SetOwner`, answer), so his own machine aims, reloads and fires
  from then on and writes what everyone draws. One operator at a time ("in use"). Generous: from anywhere within
  `UseRange` of the stand point (at most `WeaponDesign.MostUse`, 4.5 m), in front of the weapon too, and the hover offers
  E exactly where pressing it works (`WeaponHover`; nothing of E when he is too far or busy); he is then brought to the
  stand point in about a third of a second, round the weapon rather than through it (`QuickStep`), and only what truly
  stops him refuses him, on the hover and on E alike: swimming, a floor at the stand point more than 1.2 m above his
  feet (or 3 m below), or something solid on his way round at knee or chest height (`OperatorReach`; `Words.Unreachable`). The game's
  own 5 m reach of a doodad control is measured from the stand point (`GetPosition`), as `UseRange` and `HolderRange`
  (never less than `UseRange` + 0.5) are. He controls it through the game's doodad control: movement keys do nothing,
  E, jump or dodge let go, and so does going past the game's 5 m or dying; Esc too with `InstallEscape`. Hand items put
  away meanwhile, his body passing through the weapon. Nobody takes one while sitting, steering or manning something
  else, the weapon can say why not first (`Refusal`, said before asking and shown on the hover) or use the key for
  something of its own (`OwnUse`: a knocked over cannon is righted); `Manned` tells any peer whether someone holds it.
- **Aim** (`WeaponArcs`, `WeaponLay`). The weapon is laid so its shot lands on what the crosshair is on (`WeaponSight`:
  from just past `AimPivot`, out to `Reach`, passing through the weapon itself and the body it stands on, short of the sea
  with `StopAtSea`): from where the shot leaves (`Flight`, a `ShotFlight`: its point on the pitch part, its speed, its
  gravity, whether it carries on with the ship under it), toward the target, along a straight line or the shot's low
  arc (the game's projectile step counted: it adds a step's gravity before each move, so it falls a little more), a moving
  ship's motion led off; never parallel to the view, whose ray runs beside the weapon's line (the game's camera looks
  over the operator's right shoulder, `m_3rdOffset` 0.3 m right and 0.3 m up of his eye). With nothing on the crosshair
  (open sky, or sea past the reach) the shot keeps the elevation the ray has from the muzzle, turned onto the ray where
  it comes down to the sea. Measured in the frame the yaw part turns in, so a base tilted on a slope or a rolling deck is
  taken off. Within `YawLimit` each way of where it was placed (180 or more: all round) and `PitchDown`..`PitchUp`, at
  `TurnRate` and `TiltRate`; past them, the closest it can do (the pitch held at its stop and the yaw keeping the shot's
  bearing). While manned the view itself stops where the weapon does, as the weapon does (no looking behind one that
  cannot turn there): its yaw no further than the weapon can turn to lay its shot on the crosshair (`ViewTurn`, a degree
  or so short of the arc's end on the right, where the ray runs beside the weapon's line), no faster than `ViewRate` (0:
  free); with `ViewTiltStops` (the default) it tilts no faster than `TiltRate` and no further than the weapon can lay
  its shot on what the crosshair is on (`ViewTilt`: the view's ray followed to what it is on, and held where it meets
  the shot's path at the stop, a few steps a frame), so view and weapon stop together and the crosshair is never on what
  the weapon cannot reach; always within `LookUp`/`LookDown`. Taking hold, the view starts where the weapon points.
  The trigger takes what the crosshair is on (`Aim.Mark`); while `Steady` (a cannon from its lanyard's pull until the
  ball has left) the weapon stays laid on that mark rather than the live crosshair, so the operator stepping aside never
  moves it and a deck rolling or turning meanwhile is followed. The aim goes to the ZDO when it moves more than
  `WriteStep`; every other peer eases toward it at `Follow`.
- **The operator's body** (`OperatorBody`). Brought to `StandPoint` as he takes hold (`QuickStep`, above), his feet then
  walk after it with the game's own walk, steered by `m_moveDir` (a stand point on the yaw part swings round as the
  weapon turns, so turning it walks him round it); he
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
  weapon it was fired from. `WeaponShot.Way` is the way to launch it, worked out as it leaves: onto what the crosshair
  was on as the trigger was pulled (`Aim.Mark`; without one, what it is on now), laid as the aim lays it from where it
  leaves now, so a deck that rolled or turned since, or the operator stepping aside, does not carry it off; within the
  arcs and at most `MostTurn` (5 degrees) from the weapon as drawn; for anyone not manning it (a called shot), where the
  weapon points. `WeaponShot.Scatter` scatters it evenly within so many degrees round that way
  (uniform over the cone, so shots centre on the target, never to a side); both run on the firing machine only, whose
  projectile every peer sees fly, so every screen shows the same flight.
- **A shot without an operator** (`CallShot(player)`, `WeaponCall`): a player who does not man it (a ship's captain at
  the helm) asks the owner (`prefix_call`); when nobody valid mans it, it is `Free` and loaded and not mid-step
  (`Callable` tells the asker's machine beforehand), the owner hands its ZDO to him (sent first) and answers
  (`prefix_called`); once the ZDO is his (up to 2 s), his machine fires it where it points (`ShootCalled`, by default
  `Shoot`) and writes it empty and counted, every peer's cue for its look and sound. Nothing reloads it (Broadside's
  cannons reload a called shot themselves, from the caller's pack: `KwBroadsideReload`).
- **A shot ordered whoever mans it** (`OrderShot(player)`, `Orderable`, `prefix_order`; 2026-10-26, for the cannons'
  broadside: "the fire cannons hotkey should fire all cannons"): the order goes to the owner, which decides. A weapon its
  operator holds there (his machine owns it while he does) is fired by his own trigger, on his crosshair, where he has
  it laid (`WeaponReload.Ordered`, the hook `ShootOrdered`, by default `Shoot`), and reloads as his always does; any
  other is a called shot, as above. One machine decides and the shot writes it empty at once, so it fires once however
  the order crosses his own pull, his letting go or a late copy of the order.
- **Rest lay** (`WeaponDesign.Rest`, a `WeaponRest(yaw, pitch)`; `RestLay`; 2026-10-26, for the cannons' broadside: "the
  cannon isn't left in whatever crappy trajectory it was left in"). With one, whenever no valid operator holds the
  weapon (he let go, walked off, died or left the game) its owner, whoever that is then (a dedicated server too), turns
  its aim back to the rest lay (held within the arcs) at `TurnRate` and `TiltRate` each physics step and writes it to
  the ZDO, every peer easing after it as after an operator's aim; at rest it writes nothing and looks again every half
  second. A weapon never aimed reads the rest lay as its lay (`WeaponState`). A called shot fires where the weapon
  points and a reload leaves the aim alone. Null (the default, the Bone Ballista): it stays where it was left.

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
	Rest = new WeaponRest(0f, 1f),                    // optional: unmanned, back to the middle of its arc, 1 degree up
	Words = new WeaponWords { Name = "$piece_my_gun", Loaded = "...", Empty = "...", Hold = "...", LetGo = "...", NoAmmo = "...", Unreachable = "..." },
};

MannedWeapon.Install(harmony);                       // once in Awake; returns the game members it could not find
```

Hooks with plain defaults: `StandPoint(state)` (where the feet go now: the stand point), `AimPivot` (the yaw pivot: the
crosshair's target is looked for from a metre past it), `Flight` (how the shot flies and from where, `ShotFlight.At(frame,
point, speed, gravity, carried)`: by default a straight line from `AimPivot`; give a weapon's real launch point, speed and
gravity so it lands on the crosshair),
`Kick(state)` (degrees thrown up on top of the pitch), `Steady(state)` (the shot on its way: the aim kept on the trigger's
mark, `Aim.Mark`), `Draw(state)` (the
weapon's own look each frame, after the turn and before the hands), `DrawAtRest()` (a placement ghost),
`Pose(state, rig, body)` (lean, fist targets, how fully he holds: `OperatorPose`), `Gripped`/`Released` (after the hands
are posed, or with nobody's on it), `FindAmmo`/`TakeAmmo`, `Free` (the owner may grant it, or a called shot, now),
`Refusal(player)` (why he may not ask now, said to him and shown on the hover; null by default), `OwnUse(player)` (the
use key taken for something of the weapon's own first; false by default), `AltUse(player)` (the game's alternate use,
Shift+E, taken first for something of the weapon's own: a ship cannon hauled; false by default, and it works as the use
key), `ShootCalled(player)` (a called shot; by
default `Shoot`), `ShootOrdered(holder)` (an ordered shot on its operator's machine; by default `Shoot`), `Buttons(player, attack, attackHold, secondaryAttack, secondaryAttackHold)` (by default a press of
attack calls `Fire`), `HoverText(state, mine, takeable, barred)` (`barred`: why the local player may not take it from
where he is; null he may, empty too far or busy). `CallShot(player)` and `Callable`, `OrderShot(player)` and
`Orderable` (above). `QuickStep` and
`OperatorReach` are public, for a mod's own "bring him there" (the AssetLab cannons' tow). `State` reads the ZDO (`WeaponState`:
`User`, `Yaw`, `Pitch`, `Stage` (steps done), `Step` (under way, from 1), `StepTime`, `Shots`, `ShotAge`, `Settled`,
`Busy`, `Loaded`). `WeaponKeys(prefix)`: ZDO keys `prefix_user`, `_yaw`, `_pitch`, `_stage`, `_step`, `_step_at`,
`_shots`, `_shot_at` (the three reload ones nameable, so a weapon keeps the keys it already saved), RPCs
`prefix_request`, `_release`, `_answer`, `_call`, `_called`.

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
  `AimPivot` the pitch pivot, `Flight` the ball's from the muzzle (its speed and gravity, carried on by the ship). The
  field cannon's stand point is on the piece beside the trail (only his hands follow the
  breech); the ship cannon's is on the yaw part, so he shuffles after the breech as the carriage is levered round, and
  `StandPoint` keeps it from rolling back with the carriage and adds the recoil's step aside (`KwShipRecoil`). `Facing` 20
  for the ship cannon. `Free` is false while the cannon is towed (`KwArmTow`) and `Refusal` says so ("Put it down first",
  "Someone is pulling it"); the tow's owner refuses a puller while `Manned`.
- **Mounted cannon** (on a mount, all round): `YawLimit` 180 and `ViewRate` 0, so the view turns freely and the barrel
  follows at `TurnRate` across the back (no stop). `AimPivot` and `Flight` as above.
- **Elevation** (2026-10-26, "the cannon should be able to tilt down a little and up a little"): mounted and field
  cannons -8 to +20 degrees (the large ones were -6 to +18; their breech still clears the trail and the stool at +20),
  ship cannons -4 to +14 (the quoin). `ViewTiltStops` on: the view tilts with the barrel. `UseRange` the library's
  4.5 m, `HolderRange` 5 m. `Steady` from the lanyard's pull until the ball has left. `OwnUse`/`Free`: a knocked over
  field or ship cannon is righted with E, not manned (`KwRighting`). `ShootCalled` fires whatever the load is for: the
  captain's broadside at a launched Broadside ship's helm (`KwBroadside`, key N) calls every loaded, unmanned cannon
  aboard. The tow (a field cannon's handles, a ship cannon's rear) takes the same generous reach with `QuickStep` and
  `OperatorReach`. `AltUse`: Shift+E anywhere on a ship cannon hauls it (on land, or about a launched ship's deck),
  E still mans it.
- **All**: `Elbows = Square`; `Pose` from the grips (`Hands`, eased onto `ReloadHands` through a reload) with the lean from
  the grips' `Lean`, `MostLean` and `TiltLean` (the drawn barrel's tilt) and `Hold` falling while a ship cannon's carriage
  recoils. `ReloadCycle(shot.Reload - FireTime)` (one step: breech, ball, powder) with `Rest = FireTime` and
  `FinishOnLetGo` false; `FindAmmo`/`TakeAmmo` for the balls and powder (`KwShot.Charge`) of the trigger last pulled,
  which trigger the load is for and the ball kept in ZDO keys of the cannon's own (`al_arm_load_kind`, `al_arm_ammo`);
  `Buttons` for the secondary attack and held triggers, with the cannon's own words; `Shoot` writes which trigger fired
  (`al_arm_kind`) and holds the shot until `FireDelay` into the lanyard's pull (`KwArmLaunch`; the barrel meanwhile on
  `Aim.Mark`), when `WeaponShot.Way` lays it onto the mark from the muzzle and `WeaponShot.Scatter`
  spreads it (the shot's `Spread`: 1, 0.8 and 0.6 degrees for the small iron, small and large cannon), and
  `WeaponShot.Launch(..., body: carrier, weapon: the cannon)` flies it with `InstallPassThrough`, the ship's speed added;
  a called shot leaves where the barrel points.
  The motion tracks, cords, recoil and muzzle effects are its `Draw`, read from `State` (`ShotAge`, `Busy`, `StepTime`);
  its keys are `WeaponKeys("al_arm", stage: "loaded", step: "step", stepAt: "load_at")`, so a loaded cannon stays loaded.

Offline (2026-10-26, a .NET 8 harness on a fake game, the cannons' real numbers read from the built AssetLab.dll), for all
nine: view and barrel held within the arc and stopping together at its end, the view no faster than the gun, mounted ones
free and following round the back; the stand points turning with the gun (a field cannon's staying put) and the feet
walking after them; the hands bone for bone as the old cannon code posed them; the shot the same as before from the muzzle,
passing through what it stands on; a shot every `Reload` seconds; the hand-off between two players over a server.

## Changed on 2026-10-26, later

- **The shot lands on the crosshair** (`WeaponLay`, `ShotFlight`, `Flight`, `ViewTurn`, `Aim.Mark`, `WeaponShot.Way`/`Scatter`;
  the user: "it consistently shoots the ball to the left of my cursor"). The cause, read from the game's camera live
  (`GameCamera.m_3rdOffset` (0.3, 0.3, 0), the eye 1.808 m over the feet) and measured offline with that camera: the
  yaw followed the view's, so the barrel lay parallel to the crosshair's ray, and that ray runs 0.3 m right of the
  operator's eye, who stands right of the barrel's line too (0.1 m on a mount, 0.32 m at a field cannon, 0.14 m at a
  ship cannon, 0.6 m more just after its recoil); every ball passed 0.3 to 0.8 m left of the crosshair at every range.
  The pitch was laid on a straight line, so the ball also fell short: 1.1 to 1.8 m low at 40 m and 7 to 11 m low at
  100 m on a wall, and on flat ground it landed 16 to 21 m short of a mark 40 m off; on a ship sailing 4 m/s the ship's
  speed the ball carries was not led off (2 to 5 m beside). Now the weapon is laid from where the shot leaves onto the
  crosshair's target along the shot's arc (the game's projectile step counted), the ship's motion led off; the target
  is taken as the trigger is pulled, the weapon kept on it until the shot leaves and the shot laid onto it as it leaves
  (a rolling or turning deck, or the gunner's step aside, no longer carries it off); past the arcs the closest the
  weapon can do (the pitch held, the bearing kept); and the view's yaw, like its tilt, stops where the weapon can still
  lay its shot on the crosshair. Offline, nine
  cannons and a straight bolt at 15, 40 and 100 m, left, ahead and right, on level ground, a wall, from a platform, on
  10 degree slopes and a deck sailing, turning and rolling, with the game's camera: every unscattered shot lands on the
  crosshair's point (0.00 m beside, 0.00 m long or short, but the shots whose target was out of reach or left the arc
  during the pull); scattered (120 shots a mark), the mean on a board standing at the crosshair is within 0.04 m of it
  at 15 m, 0.06 m at 40 m and 0.17 m at 100 m, the side bias over about 2,000 shots a gun at most 0.02 m. Both pitch
  stops and both yaw stops land on the crosshair within 0.05 m. The Bone Ballista lays its bolt the same way (a
  straight flight from its nose in the groove).
- **The view's tilt held to the weapon's** (`ViewTiltStops`, `ViewTilt`): before, the view tilted freely (89 degrees down)
  while the weapon stopped at its pitch arc, so a cannon's crosshair on the ground near the gunner left the barrel at
  its stop, its ball flying out nearly level well past the crosshair ("it just shoots straight ahead"). The shot itself
  always left along the barrel. Offline, nine cannons on level ground, 10 degree slopes three ways and a rolling base,
  the mouse dragged hard down then up: the crosshair at most 0.11 degree past what the barrel reaches, view and barrel
  stopping together at both ends, the view never faster than `TiltRate`; 16 shots a gun across its elevation, each from
  the muzzle along its axis (0.00 degrees) and through what the crosshair was on (within 0.1 m above or below).
- **The yaw on a tilted base** (then `Bearing`, since folded into `WeaponLay`): a barrel raised 20 degrees on a 10 degree
  cross slope swung 3 degrees to the side of the view; it was then kept in the view's upright plane.
- **The aim's ray passes through the weapon itself** (`WeaponSight`): looking down past a weapon on solid ground, the ray
  could stop on its own frame.
- **Generous taking** (`UseRange` 4.5 m by default, `QuickStep`, `OperatorReach`, `WeaponHover`, `GetPosition` the stand
  point, `HolderRange` from the stand point): the hover offered E where E then did nothing ("not close enough"), and the
  ballista refused anyone in front of it (`Behind` and `Words.Behind` are gone). Offline, 84 places round each cannon:
  hover and E agree everywhere, a taker is there within 0.36 s, never through the weapon; a wall, swimming and a 1.5 m
  step refused on both.
- **A shot without an operator** (`CallShot`, `WeaponCall`, `ShootCalled`, `Callable`, keys `_call`/`_called`), for the
  cannons' broadside. Offline over a server: called twice, fired once from the caller's machine; empty or manned, refused.
- **A shot ordered whoever mans it** (`OrderShot`, `Orderable`, `ShootOrdered`, `WeaponReload.Ordered`, key `_order`),
  so the broadside fires manned cannons too. Offline over a server (the AssetLab harness `rr`): a manned cannon fires
  once, on its gunner's machine along his barrel, his reload from his pack; let go before its turn, it is the caller's
  called shot; his own pull in the same frame, or a late order: never a second shot.
- **The Bone Ballista**, before against after through its own scenarios: the yaw identical in every frame; the view's
  pitch identical until the old view would have looked past the arc (it reached 25 degrees up while the bolt's line
  needed 27.6, past its 25); the view now stops at 23.3 up, where the bolt reaches 25.1; its holder is brought in at
  once rather than walking up, and from in front too.

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

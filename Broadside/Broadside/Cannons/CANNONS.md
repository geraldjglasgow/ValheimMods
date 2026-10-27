# Ship cannons

Broadside's ship cannons, graduated from the AssetLab Cannons bench on 2026-10-26 (the user's words: "we need to move the
cannons into Broadside ... we need black powder and iron cannonballs as well. We also need to not allow building with the
vanilla Valheim pieces on the ship, but you should be allowed to build cannons on your ship"). Code in
`Broadside/Broadside/Cannons/` (`Arms/` the manned-weapon framework, the broadside, the shots and the reload lines;
`Guns/` the guns, breeches, balls and powder; `Ship/` the truck carriage, its recoil and its haul about a deck), namespace
`Broadside`. No settings and no bundle: every mesh is made in code from the game's own materials at startup. Broadside's
`CLAUDE.md` ("Cannons") has the summary; this file keeps the behaviour in full and how it was measured.

Only the ship cannons came over. The bench's seven weapon mounts, the three mounted cannons on them and the three field
cannons on wheels (towed on land) were left out with their code; the bench was removed from AssetLab. Measurements and
offline checks below that name mounted or field cannons were made on all nine of the bench's cannons and are kept as
they were, as history. The ship cannons are built only on a launched Broadside ship's deck, never on land, so a ship
cannon has no physics body of its own any more (the bench's could be hauled on land by a joint and knocked over and
righted): it rides its ship (`KwShipRider`) and is hauled about the deck by its hauler's game.

**Meadows and Black Forest only, for now** (the user, 2026-10-26: "right now we should only be supporting the Meadows and
Black Forest ... We should not have any of the higher tier stuff yet"; and "Re-tier the small cannon to Black Forest: made
of bronze and fine wood, firing stone shot; black powder from coal and resin only. The iron, Plains and Ashlands cannons
are hidden until a later release."): the bronze ship cannon is the small iron cannon's design (`KwShipCannons.Bronze`:
the same carriage, gun, arcs, turn speeds, recoil, reload, broadside and haul; `KwSmallIronCannonShape` cast in the
carriage's metal) in the Black Forest rudder's two looks, the game's bronze (`bar_bronze_stack`'s `bronze`) for its gun
and fittings and fine wood (`piece_table`'s `Table_Mat`) for its carriage, and it fires stone shot. `KwTiers.Offered`
(the Black Forest) decides what is offered: a cannon of a higher tier stays out of the hammer (`KwCannonHammer`) and an
item of one has no recipe (`KwItems`), but every prefab and item is still made and registered on every peer, so cannons
and stacks a world already has still load, work and give their cost back. Raising it offers the others again.

## Identity

Every name starts with `BS_`/`bs_` (`Broadside_` for UI objects), never `KW_`/`kw_`: Broadside treats every `KW_` prefab
as a ship part (blueprints, snap rules, undo, `kwship clear`).

| What | Names |
| --- | --- |
| Ship cannons (four trucks, on a launched ship's deck) | `BS_cannon_bronze` (Black Forest, the only one in the hammer for now), `BS_cannon_small_iron` (Swamp), `BS_cannon_small` (Plains), `BS_cannon_large` (Ashlands); ids `cannon_bronze`, `cannon_small_iron`, `cannon_small`, `cannon_large` |
| Items | `BS_CannonballStone` (the bronze cannon), `BS_CannonballIron` (the two small cannons), `BS_CannonballFlametal` (the large cannon), ammo type `$bs_ammo_cannonball`; `BS_BlackPowder`. The stone ball wears the game's `Stone` item's material (`stone`, `KwStone`), as does the ball the bronze cannon lays in at its breech |
| Shots, bursts | `BS_shot_cannon_bronze`, `_small_iron`, `_small`, `_large`; `BS_burst_<same>` |
| ZDO keys | the manned weapon's (MannedWeapons, "Manning" below): `bs_arm_user`, `bs_arm_yaw`, `bs_arm_pitch`, `bs_arm_loaded` (1 once loaded), `bs_arm_step`, `bs_arm_load_at`, `bs_arm_shots`, `bs_arm_shot_at`; the cannon's own: `bs_arm_kind` (the trigger that fired), `bs_arm_load_kind` (the trigger a load is for), `bs_arm_ammo` (the ball loaded), `bs_arm_tower` (who hauls it), `bs_arm_untied` (1: its breeching left unmade where it was hauled to, nothing there to make it fast to); and Broadside's rider record (`kw_rider`, `kw_rider_pos`, `kw_rider_rot`: `KwShipRider`), its pose written as it is hauled |
| RPCs (on the cannon's own view) | the manned weapon's, named from its key prefix by the library: `bs_arm_request`, `bs_arm_release`, `bs_arm_answer`, `bs_arm_call`, `bs_arm_called` (a shot without an operator), `bs_arm_order` (a shot ordered whoever mans it: the captain's broadside); the cannon's own: `BS_Arm_Load` (the captain asks for an empty cannon to load it), `BS_Arm_Tow`, `BS_Arm_TowDenied` (haul it) |
| Patches | the cannons': `Player.SetControls` (the hauler's controls, `KwArmControls`), `Player.UpdatePlacementGhost` and `Player.TryPlacePiece` (only on a launched ship's deck, `KwArmPlace`), `Projectile.OnHit` (the harder hit on structures and the energy under water, `KwShotHits`), `ObjectDB.Awake`/`CopyOtherDB` (the items and recipes, `KwItems`); the MannedWeapons library's, by hand (`KwCannonPrefabs.Install`): `Player.SetControls`, `Player.SetMouseLook`, `Character.UpdateRotation`, `Projectile.OnHit` (the pass-through); each acts only on the cannons' own pieces, gunners and shots. No patch reads the captain's keys (U, J): a hidden ticker object does (`Broadside_CannonsTicker`, `KwBroadside`, the Hotkeys library) |
| UI and drawn objects (the captain's screen only) | `Broadside_FireBroadside` and `Broadside_ShotArcs`, the cannons' two lines on Broadside's helm key list (`Broadside_HelmKeys`, `KwHelmKeys`); `Broadside_ShotArcLines`, the shot arcs' lines under the ship steered; the reload lines, rows `cannons.port` and `cannons.starboard` of Broadside's own ship panel (`KwShipPanel`) |

## Recipes

| What | Cost | Where |
| --- | --- | --- |
| Bronze ship cannon | 10 bronze, 20 fine wood | the hammer, a workbench in range (the wood floor's station), on a launched ship's deck |
| Stone cannonball, 4 | 2 stone | workbench, level 1 |
| Black powder, 4 | 2 coal, 1 resin (no guck since 2026-10-26) | forge, level 1 |

Held back for a later release (`KwTiers.Offered`), not in the hammer or the crafting lists:

| What | Cost | Where |
| --- | --- | --- |
| Small iron ship cannon | 14 iron, 22 ancient bark | the hammer, as the bronze one |
| Small ship cannon | 14 black metal, 8 tar, 24 wood, 4 linen thread | the same |
| Large ship cannon | 18 flametal, 6 surtling cores, 26 blackwood, 6 linen thread | the same |
| Iron cannonball, 4 | 1 iron | forge, level 2 |
| Flametal cannonball, 4 | 1 flametal, 2 grausten | black forge, level 2 |

A shot takes one ball and one, one, two or three black powder (bronze, small iron, small, large). The bronze cannon fires
only stone shot, the two small ones iron balls, the large one only the flametal ball.

## How it plays

- **Where they are built** (asked for 2026-10-26: "build things like cannons and the weapon mounts on the ship after it is
  built, and not while it is being built on the keel guide with keel blocks"; and on graduating: "you should be allowed to
  build cannons on your ship"): only on a launched Broadside ship's deck (`KwArmPlace`: the local player's placement ray
  on the ship this frame, `KwShipRiderAim`), where each then rides the ship (`KwShipRider`). On a Broadside dock's design
  parts "Build cannons on the ship once it is launched"; anywhere else (the ground, the game's floors) "Ship cannons are
  built on a launched ship's deck"; the game's own ships never take a ghost. On a launched ship nothing else is built
  (Broadside's `KwShipRiderRules`: "Only ship cannons are built on a launched ship"); pieces already aboard stay.
- **Ship cannon**: built on a launched Broadside ship's deck. A breech-loader with a long barrel (each shape's `Long` stretch, `Guns/KwStretch.cs`:
  the reinforce drawn out behind the trunnions and the chase ahead of them, rings, hoops, breech and muzzle unchanged) on
  a long, low naval truck carriage made in code to the proportions of a photographed naval gun (below): two brackets
  (the gun's own cheeks across), each a long head at the trunnions (the trunnions bedded a little into it, its front
  corner taken off) and four steps down to the rear, on an underside cut up in an arch between the axletrees; a front
  and a rear transom, a front and a rear axletree whose arms carry four solid wooden trucks out near the brackets' ends
  (the front pair a little larger), iron cap-squares over the trunnions, eyebolts (breeching, side tackles, train
  tackle), a stool bed with a quoin on it, a ring on each side of the breech, and the breeching rope from each ring
  through the bracket's eyebolt to a ringbolt on a deck plate ahead of the carriage (the piece's model). In the tier's
  wood and metal: bark and iron, tarred dark wood and black metal, ashwood and flametal. The whole carriage is levered
  round 20 degrees each way (22, 22 and 16 degrees a second) and the gun elevates in it
  from -4 to +14 degrees, the quoin sliding along the bed under the breech (`KwQuoin`; it bears on the breech's base
  ring and is kept under its back edge, door and T-handle). **Firing**: manned, aimed, yanked, reloaded and saved as
  before, the view stopping at the arc's end with the carriage and turning no faster than it is levered round (below,
  "Manning"); the gunner stands close behind the low breech and a little to its right, turned 20 degrees toward it, bent
  well over it (50 degrees to aim, 55 to reload, further as the gun is raised, at most 65: `KwArmDef.MostLean` and
  `TiltLean`), and his stand point turns round with the carriage as it is levered round, so he shuffles after the
  breech; as he yanks the lanyard he steps out to the right beside the carriage; the carriage rolls back on its trucks
  0.7, 0.8 and 0.95 m (fast, slowing as the breeching takes it, the rope drawn taut and straight), holds a quarter
  second and runs out again over 2 s, its trucks turning; his hands let go while it recoils and he steps back in as it
  finishes. **Rolling** (asked for on 2026-10-26: "players should be allowed to wheel cannons around for ship cannons, E
  is to mount but some other key should be to move it"): E anywhere on it, its rear too, mans it; **Shift+E** anywhere on
  it (the game's alternate use: the AltPlace key with use, a gamepad's alternate keys too; the MannedWeapons library's
  `AltUse`, `KwArm`) hauls it by its rear (low behind the brackets; from anywhere its hover offers it, within 4.5 m, he
  is brought to it at once, round the carriage), and Shift+E again lets go. Its hover says both: "[E] Man the small iron
  ship cannon", "[Shift+E] Haul it (slowly)" (or "Let go", or greyed why not). One player at a walk or less (1 m/s),
  all four trucks kept down, turning on the spot about its middle no faster than 20 degrees a second. It is hauled about its ship's deck (below, "Hauling a ship cannon about a deck"). Not while
  manned; never manned while hauled. It is manned, aimed, fired and reloaded as anywhere: everything is
  measured in the weapon's own space, so the moving deck changes nothing; its recoil is drawn on the piece, and its trucks
  turn by their travel in the ship's frame, so they stand still as the ship sails (`KwWheels`). While it is hauled its
  breeching (both ropes and the ringbolts on their deck plates) is hidden; let go, it is made fast again where it stands
  (`KwBreechingTie`, below).

## Manning

Asked for on 2026-10-26 ("apply a lot of the bone crossbow's aspects to the weapon systems: how you aim it, and how you
can't aim past a certain point because the weapon just doesn't go that far ... the mounted weapons are fine turning 360.
I also really liked the bone crossbow's animation for the character when he walks around it"): the cannons are manned
weapons of the MannedWeapons library (`ValheimModLibs/MannedWeapons`), the Bone Ballista's way. `KwArm` is the
library's `MannedWeapon`; `KwArmDesigns` makes each kind's design from its definition.

- **Taking hold** (asked for on 2026-10-26: "be generous ... just put the character up there quickly"): E asks the
  cannon's owner, as the game's ship helm does; granted when nobody valid mans it and nobody pulls it, the gunner is
  written to the ZDO and the ZDO handed to him, so his own game aims, reloads and fires from then on. The hover offers
  E exactly where pressing it works: anywhere within 4.5 m of the stand point (the library's `UseRange`, inside the
  game's own 5 m reach of a doodad control, which the library measures from the stand point too, as it does the
  owner's 5 m holder range), in front of the gun too; he is then brought to the stand point in about a third of a
  second (0.15 to 0.45 s by distance), round the gun rather than through it (the library's `QuickStep`). Only what
  truly stops him refuses him, on the hover (greyed) and on E alike: swimming, a stand point more than 1.2 m above his
  feet or 3 m below them, or something solid on his way round at knee or chest height ("You cannot reach it from here"; `OperatorReach`);
  someone manning it or hauling it. Never while he sits, steers or mans
  something else; while it is hauled he is told so without asking. E, jump or dodge let go, and so do going past the
  game's 5 m or dying; his hand items are put away meanwhile. E on its rear mans it too, and Shift+E anywhere on it hauls it
  instead (the library's `AltUse`, taken before anything else). A player still holding Shift (running) as he presses E hauls rather than mans: the game's own convention.
- **Aim and view** (the library's `WeaponLay`, since 2026-10-26 evening: the user, "it consistently shoots the ball to
  the left of my cursor ... if it's inaccurate, it should be scattered around where the cursor was aimed"): the barrel is
  laid so the ball lands on what the crosshair is on (out to 250 m, through the ship it stands on and the cannon itself,
  never under the sea): from the muzzle (`KwArm.Flight`: the loaded shot's speed and drop, carried on by the ship or
  carriage under it), toward the target, raised by the ball's drop (the low arc at 48, 56 and 62 m/s, falling 5, 4.5 and
  5.5 m/s squared, and the half step more the game's projectile falls), a moving ship's speed led off; measured from the
  plane the cannon stands on, so a field cannon resting on a slope or a cannon on a rolling deck still lands on the
  crosshair. With nothing on the crosshair (the sky, or sea past 250 m) the barrel takes the view's own elevation,
  turned onto the crosshair's line where the ball comes down to the sea. Past its arcs it does the closest it can (the
  pitch held at its stop, the bearing kept). A field cannon (25 degrees each way) and a ship cannon (20) hold the view
  itself to the gun's turn speed (35, 35, 25 and 22, 22, 16 degrees a second) and stop it where the gun can still turn
  to lay the ball on the crosshair (the library's `ViewTurn`: about a degree short of the arc's end on the right, where
  the crosshair's ray runs beside the barrel), so view, barrel and feet stop together; a mounted cannon leaves the view
  free and its barrel follows it round the back at its turn speed. Every cannon holds the view's tilt to the barrel's
  (asked for on 2026-10-26: "you should not be able to move your mouse all the way down and look at the ground if the
  cannon can't aim at the ground"; the library's `ViewTiltStops`): the view tilts no faster than the barrel (30, 30, 22
  degrees a second) and no further than the barrel can lay the ball on what the crosshair is on, so dragging the mouse
  on past either end, the crosshair stops where the ball, laid at the stop, lands (a field cannon's, 8 degrees down
  from 0.86 m: about 6 m out) or on the sky, and view and barrel stop together. Elevation: mounted and field cannons 8
  degrees down to 20 up, ship cannons 4 down to 14 up (the quoin). Pulling the lanyard takes what the crosshair is on
  (the library's `Aim.Mark`); until the ball has left (`KwArm.Steady`) the barrel stays laid on that, not on the live
  crosshair, so a ship cannon's gunner stepping aside from the recoil never moves it, while a deck rolling or turning
  meanwhile is followed; the ball then leaves onto it.
- **Why it "shot straight ahead"** (the user, 2026-10-26: "I can point my cursor at the ground and the cannon just
  shoots straight ahead"): the view was free to tilt to 89 degrees down while the barrel stops at 8 down, so with the
  view dragged down onto the ground near the gunner the barrel lay at its stop and the ball flew out nearly level,
  meeting the ground 5.4 m out from a mounted small iron cannon's trunnions at the nearest (offline; 6.1 and 7 m for the
  field cannons, 9.7 and 11.4 m for the ship cannons), well past the crosshair; and at middle range the camera, above
  and behind the gunner, could put the crosshair below anything the barrel could reach. The shot itself
  always left along the barrel: offline, every shot at every elevation (16 a gun, three bases) left the muzzle along
  its axis (0.00 degrees off) at the aim's pitch.
- **Why it "shot to the left"** (the user, 2026-10-26 evening; measured offline with the game's own camera, read live:
  `GameCamera.m_3rdOffset` (0.3, 0.3, 0), the eye 1.808 m over the feet): until then the yaw followed the view's, so
  the barrel lay parallel to the crosshair's ray, and that ray runs 0.3 m right of the gunner's eye, who himself stands
  right of the barrel's line (0.1 m at a mounted cannon, 0.32 m at a field cannon, 0.14 m at a ship cannon and 0.6 m more
  just after its recoil). Every ball passed 0.3 to 0.8 m left of the crosshair, at every range and on every base (on a
  wall 15 to 100 m off: 0.32 to 0.46 m mounted, 0.45 to 0.77 m field, 0.30 to 0.58 m ship). The pitch was also laid on a
  straight line, so the ball fell short: 1.1 to 1.8 m low at 40 m and 7 to 11 m low at 100 m on a wall; on flat ground it
  landed 16 to 21 m short of a mark 40 m off; and on a ship sailing 4 m/s the ship's speed the ball carries was not led
  off (2 to 5 m beside).
- **Walk-round**: the gunner's feet walk to the stand point with the game's own walk, stop a few centimetres from it,
  start again only once it has moved 15 cm, and face the way the gun points (a ship cannon's turned 20 degrees toward the
  breech). A mounted cannon's stand point is on its turntable and a ship cannon's on its carriage, so turning the gun walks
  him round after the breech; a field cannon's stays beside the trail and only his hands follow. A ship cannon's never
  rolls back with the carriage at a shot, and steps aside out of the recoil and back (`KwShipRecoil`).
- **Hands**: both fists on the grips (the left on the breech, the right on the lanyard's handle), eased onto the door's
  handle through a reload, elbows out square to his arms, leaning in as each grip says and further as the barrel is
  raised (`KwArmHands`); at a ship cannon he lets go while the carriage recoils.
- **Reload**: one step, the shot's reload less its motion (6.8, 5.8 and 8.8 s), begun once the shot's motion (1.2 s) has
  run, taking the powder and a ball as it begins (`KwArmReload`, from a store: the gunner's pack, `KwAmmoStore`), so a
  cannon fires every 8, 7 and 10 s; letting go mid-reload leaves it to run out (taking hold again never cuts it short).
  A cannon the captain's broadside fired reloads the same way from the captain's pack (below).
- **Rest lay** (asked for on 2026-10-26: "when a player is done using a cannon, the aim ... needs to have some set
  position it reverts back to ... so the captain knows where his cannons are likely to fire"): whenever nobody mans a
  cannon (its gunner let go, walked off, died or left the game), its owner turns it back to the middle of its arc
  (straight out of a ship cannon's port, a mount's placed facing, a field cannon along its trail) at 1 degree up
  (`KwArmDesigns.RestPitch`), at the cannon's own turn and tilt speeds (0.6 to 1.5 s back from the far ends offline),
  written to its ZDO so every peer's copy eases after it; a cannon never aimed reads it. The bearing is centred as well
  as the elevation, so a broadside fires where the captain expects. The library's `WeaponDesign.Rest` (`RestLay`, on the
  owner, a dedicated server too); the Bone Ballista has none and stays where it was left. A called shot and a reload
  never move it. One degree up: from a 1.5 m deck the ball is still about 2 m over a calm sea 30 m out and meets it 53
  to 55 m out (small iron) or 68 to 72 m (the others).
- **Firing**: attack pulls the lanyard of a loaded cannon (`KwArmTrigger`; the secondary attack a cannon's other shot,
  when one has one); the shot leaves 0.32 s into the pull (`KwArmLaunch`), from the muzzle, carried on by what the cannon
  stands on and passing through it and the cannon (`KwShooter`, the library's `WeaponShot`); its way is worked out as it
  leaves, onto what the crosshair was on at the pull (the library's `WeaponShot.Way`: within 5 degrees of the barrel as
  drawn), then scattered evenly within 1, 0.8 and 0.6 degrees of it for the small iron, small and large gun (the shot's
  `Spread`, was 0.8, 0.6 and 0.5: uniform over the cone, so shots centre on the crosshair and never lean to a side; a
  standard spread of about 0.35, 0.28 and 0.21 m either way at 40 m). The draw and the flight are the gunner's game's;
  every other screen sees that projectile. A shot the captain calls leaves where its barrel points, scattered the same.
  The muzzle's effects play then on every screen that sees the cannon, from the ZDO's shot count.
- **Checked offline** (2026-10-26, a .NET 8 harness on a fake game, the real new cannon code and library with all nine
  cannons' numbers read from the built AssetLab bench's DLL): the arcs and view stops as above; the stand points and the feet
  walking after them; the hands, bone for bone, as the old code posed them (with the library's lean fix, which the old
  code shared: without it 3 frames of one gun differ); the shot as before from the muzzle, on the ground, its own body and
  a moving ship; a shot every reload; the hand-off between two players over a server (in use, let go, taken, refused
  while pulled, taken from a gunner who died there). Later that day, all nine on level ground, 10 degree slopes nose up,
  nose down and across, and a base rolling 8 and pitching 4 degrees, the mouse dragged hard down for 3 s and up for 4:
  the crosshair never on what the barrel cannot reach (at most 0.1 degree past the stop), view and barrel stopping
  together at both ends, the view never tilting faster than the barrel, the crosshair held down as far out as the
  barrel's line meets the ground (within 0.25 m); 16 shots a gun across its whole elevation on three bases, each from
  the muzzle along the barrel's axis and passing what the crosshair was on within 0.1 m above or below (beside it only
  by the gunner's own place off the barrel's line: 0.1 m mounted, 0.32 m field, 0.15 m ship); E from 84 places round
  each gun, hover and E agreeing at every one, the taker brought in within 0.36 s and never nearer the pivot than
  where he started; a wall, swimming and a 1.5 m step refused on the hover and on E alike; the called shot over a
  server (below, "The captain's broadside"). In the evening, the ball flown down as the game flies it (its gravity added
  before each 50th-of-a-second move) with the game's camera, all nine and a straight bolt, the crosshair kept on marks
  15, 40 and 100 m off, 15 degrees left, ahead and right, on level ground, on a wall (up), from a 1.5 m platform (down),
  on 10 degree slopes nose up and across, and on a deck sailing 4 m/s, turning 3 degrees a second, rolling 5 and
  pitching 3: every unscattered shot lands on the crosshair's point (0.00 m beside, 0.00 m long or short), but those
  whose mark was out of the gun's reach or left its arc during the pull (a ship cannon's 4 degrees down cannot reach the
  sea 15 m off a rolling deck), which do the closest the gun can; scattered (120 shots a mark), the mean on a board
  standing at the crosshair's point is within 0.04 m of it at 15 m, 0.06 m at 40 m and 0.17 m at 100 m, and the side
  bias over about 2,000 shots a gun at most 0.02 m (before: 0.3 to 0.8 m left, and short or low as above). Dragged
  hard to either pitch stop or either yaw stop and fired there, the ball lands within 0.05 m of the crosshair.

## The captain's broadside

Asked for on 2026-10-26 ("a captain hotkey to fire all cannons on the ship. If a single player is playing, they need
this"). While the local player steers a launched Broadside ship (the game's ship control, `GetControlledShip`, on a
ship with Broadside's `KwShipBody`), **U** fires every loaded cannon on the side he looks toward (the
cannons riding on the ship, its children; below, "One side at a time"), manned ones too since
the evening of 2026-10-26 (below) (`KwBroadside`, read
through the Hotkeys library: never while he types, only when the game takes his input). U, the user's pick of a free key (2026-10-26; it was N, which is also
GrindstoneSkills' Fog Lift aboard; F is the game's Forsaken Power, which works at a helm; K is GrindstoneSkills' Wind
Call): the game binds E, R, Q, X, C, F, V, G, T, M and Tab, Broadside H (nudge) and B (bail, held, at a flooding
ship's helm), and EarthWright's U and I work only with a hoe or cultivator in hand.
Each fires where it points now, with its own shot, one after another 0.15 s apart from bow to stern, so the broadside
rolls down the ship. Each is ordered of its owner through the MannedWeapons library's ordered shot (`OrderShot`,
`bs_arm_order`), and the owner decides. One nobody mans goes the called shot's way (`bs_arm_call`/`bs_arm_called`):
the owner, when it is free and loaded and not mid-reload, hands its ZDO to the captain's game (sent first) and answers;
the captain's game fires it once the ZDO is its own as its gunner's lanyard would (`KwArm.ShootCalled`: the shot the
load is for), so the ball, the muzzle's blast and sound and a ship cannon's recoil follow on every screen from the ZDO,
and the captain is the shot's attacker. One still reloading or empty is skipped. Every cannon aboard says the key on its
hover. An unmanned cannon lies at its rest lay (above, "Manning"), so a broadside fires where the captain expects.

**One side at a time** (the user, the evening of 2026-10-26: "where the captain is looking [is] the halfway point. So if
you are looking more to the port side, then pressing U fires the port side ... you can only fire one set at a time ...
if you look to the right and the starboard fires and you immediately look to the left and the port side fires, it's fine
if they kind of fire at the same time"): as U is pressed his view (the game's camera, laid level) is compared with the
ship's bow-stern line (`KwBroadside.LooksPort`): left of it fires the port cannons, right of it (or straight along it) the
starboard ones. A cannon's side is the way it was placed facing, as the reload lines count it (`KwReloadCount.Port`:
facing left of the ship's heading is port). Each press ripples its side bow to stern; pressed again toward the other side
while the first ripple runs, the second runs beside it (the queue is looked through whole, not only its head), so both
sides can be firing at once. Decided on the captain's game, then ordered of each cannon's owner exactly as before, so
nothing new is sent. His top line names the side ("Port broadside: 3 of 3 cannons fired, 3 reloading."; "Starboard
broadside: no cannons on the starboard side."), and the helm key list's line reads "Fire the broadside you look toward
(loads from your pack) [U]". The shot arcs still show every cannon aboard, both sides.

**When U "does nothing"** (the user, the same evening: "it says that all of my port and starboard side guns are ready,
but I'm pressing [U] and nothing is happening"; later "it might not fire when you're working on stuff"). Checked live
with read-only DevBridge calls on two ships armed by script (`Instantiate`, `SetCreator`, Broadside's
`KwShipRider.Place`, no hammer): nothing a hammer placement sets up is missing; each cannon is the ship's child, its
ZDO owned by the captain's game, its calls registered (now `bs_arm_order`, `bs_arm_call`, `bs_arm_called`, `BS_Arm_Load`),
free, loaded by `KwBroadsideLoad`, and U fired them in the same session (12 of 12 twice, 6 of 6 three times), the log
clean. A press that reaches the game always prints the top line, even "0 of 6"; no line means the key never reached
`KwBroadside.Tick`'s order: the game window did not have the keyboard (DevBridge's status showed it unfocused at the
helm seconds before a press that worked: another window, a build or a tool, had it), the game not taking input
(`Player.TakeInput`: inventory, map, menu or console open, or the free camera on, which DevBridge's `/frame` turns on),
the Hotkeys library's typing check (a text box left selected, which DevBridge's `/type` or `/click` can leave), or
Shift, Ctrl or Alt held. DevBridge's reads (`/eval`, `/log`, `/nearby`, `/ui`, `/status`) touch none of these, and its
`/key` and `/mouse` feed only the game's Input System, while the key is read through Unity's own input.

**Manned cannons fire too** (the user, the evening of 2026-10-26: "also the fire cannons hotkey should fire all
cannons"). A cannon a gunner mans is his game's while he holds it, so the order reaches his game, which fires it as his
own pull of the lanyard would (`WeaponReload.Ordered`, `KwArm.ShootOrdered`: the shot the load is for, on what his
crosshair is on, where he has it laid), the gunner the shot's attacker; he reloads it from his own pack as always, and
the captain's pack gives nothing for it. One machine decides each shot and writes the cannon empty at once, so it fires
once however the order crosses his own pull (whichever comes first fires, the other finds it empty), his letting go (let
go before its turn in the ripple, it is the captain's called shot, reloaded from his pack) or a late copy of the order
(the owner finds it empty). The ripple stays bow to stern across all of them.

Later the same day ("When the captain is firing using the hotkeys, we should actually have the hotkeys listed ... the
cannons should auto reload if the captain is firing ... have the reloading materials come out of the captain"; and
"a hot key to see the cannon trajectories ... You tap it, you see them, you tap it, you turn them off"):

- **Reloads** (`KwBroadsideReload`): every cannon the broadside fired reloads by itself afterwards, a gunner's reload
  in every way, its balls and powder taken from the captain's own pack. His game, which owns the cannon's ZDO from the
  called shot on, queues it as it fires; once it has rested after the shot (the shot's motion, 1.2 s) it takes the
  load for the shot it fired, else its other shot's (`KwArmReload.Load`, from `KwBroadsideReload.Store`: the captain's
  pack, `KwAmmoStore`; a ship's chest aboard would go there, asked of the chest's own owner first) and writes the load
  and the reload step's start to the cannon's ZDO as a gunner's reload does, so every peer, a dedicated server too,
  draws the breech's reload motion (the only reload look a cannon has; it plays no sound) and counts it loaded once the
  step has run (6.8, 5.8, 8.8 s), with no further write. It takes only while his game still owns the cannon, nobody
  mans it and it is empty and not loading: one a gunner took meanwhile is the gunner's (his own reload follows from his
  pack), one broken, gone or whose ZDO went elsewhere takes nothing; one queued reload a shot, dropped once tried, so
  nothing is taken twice. **A gunner who takes hold mid-reload** finds it loading and loaded when the step has run:
  the reload finishes rather than being cancelled (the simpler of the two, and correct: the step is in the ZDO with its
  start, the library never cuts a step short, and nothing taken is lost). Short of a load it stays empty.
- **Empty cannons load too** (`KwBroadsideLoad`; a bug the user met that evening, 12 small iron ship cannons: "guns
  are loading on port side but not the starboard side": the starboard six had never been fired, so they were never
  loaded and U skipped them as empty). While he steers, every 0.2 s, bow to stern, the first cannon aboard that is
  empty and not loading, that nobody mans or hauls, upright, with no broadside reload queued for it, has its load begun
  from his pack by the same reload step (`KwBroadsideReload.Begin`: the same rest after a shot, the items taken and
  the step written in one frame, so nothing is taken twice), one a count, so the loads roll bow to stern. One his game
  does not own is asked of its owner (`BS_Arm_Load`, once every 2 s a cannon at most), who hands it over the way the
  called shot does (the ZDO sent first) while it is still empty and free; his game begins the load once it owns it. A
  gunner who takes it meanwhile wins, as for the reloads. Short of a load it stays empty, the reload lines show it red
  "empty", and it is tried again every 2 s (it loads once he picks up the items). While a broadside reload is queued
  nothing new is begun, so the reloads its message promised come first. U then fires whatever is loaded.
- **His top line**, as U is pressed (the reloads forecast from his pack, bow to stern, the order they reload in; a
  manned cannon counts among those fired, but its gunner reloads it, so it is in neither the reloads nor the short):
  "Port broadside: 4 of 6 cannons fired, 3 reloading, 1 short of Iron cannonball (1 still reloading, 1 not loaded)"
  (that side's cannons only). U again while some reload fires only the loaded ones and counts the rest: "still
  reloading" (its reload under way or queued), "not loaded" (empty, nothing coming).
- **The helm key list**: Broadside lists a helm's keys at the bottom right of the screen while a launched ship is
  steered (its `KwHelmKeys`: the strip where the game's own key hints sit, which the game hides at a helm, in their
  look). The cannons add two lines to it (`KwHelmRows`: the list is `Broadside_HelmKeys` under the game's key hints,
  each line a copy of the game's own hint; the cannons copy the first line and give it their words): "Fire the broadside you look toward (loads from your pack) [U]" (was
  "Fire broadside (loads from your pack)" until U fired one side) and "Shot arcs: on/off [J]", hidden without cannons
  aboard. The old once-a-helm hint on the top line went with it.
- **Shot arcs** (`KwShotArcs`, `KwShotPath`, `KwBall`, `KwShotArcLook`): **J** at the helm turns them on and off
  (kept for the session, also between helms, never saved; not while the inventory or the map is open). J because it
  sits beside H and under U, and nothing reads it at a helm: the game's keys are E, R, Q, X, C, F, V, G, T, M, Tab, W, A, S,
  D, 1 to 8, Space, Shift, Ctrl; Broadside's H and B; GrindstoneSkills' K (Wind Call), N (Fog Lift) and O (lookout)
  aboard; EarthWright's J, I, L, P, U, Y only with a hoe or cultivator; OpenKeep's J marks junk only in the inventory.
  While on, each cannon aboard a broadside would fire (manned ones too: a manned barrel is laid on its gunner's
  crosshair, where his shot goes) shows where its ball would go if fired now, on the captain's screen only (nothing is
  sent): the real called shot's flight, from the muzzle along the barrel as drawn, at the shot's speed plus the ship's motion there, each physics step its gravity added and then the move, each
  step's path tested from half a step behind on the layers the game's projectile hits, passing through its own ship and
  cannon and the characters it spares (friends while PvP is off), and into the sea on under as `KwShotWater` flies it,
  so a hull struck under the waterline counts. Drawn as a faint white dotted line (one dot a segment, the path's points
  about 2 m apart) ending where it strikes, with no marker there; a cannon not loaded dimmed. Until the evening of
  2026-10-26 a ring lay where it struck, as wide as the shot's scatter there, green on what it would hurt and pale on the
  sea or ground; the user had them taken off ("remove the red and green circles ... not arrows, the little white lines,
  those are just fine"), so only the lines are drawn (`KwShotArcLook` has no ring pool, colours or material any more).
  Worked out 20 times a second and drawn under the ship (between two looks they ride with it), the game's sprite shader,
  unlit and thin, widths growing with the distance so they look as thin far off as near. Cheap: the path is tested 5 steps at a time along their chord (a copy of the ball flown ahead; the arc
  within millimetres of it, under the water within centimetres) and only a span with something on it step by step;
  about 26 to 29 rays a cannon at its rest lay over open sea.
- **Checked offline** (2026-10-26, the AIM2 harness's fake game, the real new code on the real library, a server owning
  three cannons' ZDOs, a gunner A and the captain B on their own machines, three guns: small iron ship, large mounted,
  small field): U with three loaded and two balls in his pack fires all three from B's game, the two bow ones reload
  (the step in their ZDOs, two balls and two powder each taken once), the stern one stays empty, his line "3 of 3
  cannons fired, 2 reloading, 1 short of ..."; every machine reads them reloading, then loaded, the server's and A's
  copies drawing the reload motion; the aim never written. U again: "2 of 3 cannons fired, 1 reloading, 1 short (1 not
  loaded)", then "0 of 3 (2 still reloading, 1 not loaded)", then "(1 still reloading, 2 not loaded)". A taking a
  cannon mid-reload: it finishes loaded in his hands, nothing taken from him, nothing more from B; his own shot then
  reloads from his pack. A taking one between its shot and its reload: B's game takes nothing for it, A's reload loads
  it. One broken before its reload: nothing taken for it. The large cannon with only iron balls: "3 of 3 cannons fired,
  3 short of Flametal cannonball". The shot arcs, all nine cannons, the prediction made at the moment the real ball
  leaves against the ball flown from its launch: open sea, an enemy hull 30 m out, a hull met under the waterline, a
  friend (passed) and a creature (struck) in the way, the ship's own hull in front of the muzzle (passed), land, and a
  deck sailing 4 m/s, turning, rolling 5 and pitching 3 degrees: the same strike, 0.000 m apart, of the same kind,
  every time. The arc as drawn when U is pressed is 1 to 2.4 m from where the ball lands 110 to 160 m out on that
  rolling deck: the deck moves during the lanyard's pull (0.32 s).
- **The reload lines** (the user, the evening of 2026-10-26: "some like reload timer on cannons for the captain, maybe
  like under the ship health like a reload countdown"; `KwReloadRows`, `KwReloadCount`): while he steers with cannons aboard, one line for each side that has cannons,
  "Port guns 3/3 ready" and "Starboard guns 1/3, 4.2 s, 1 empty": how many are loaded of all, the seconds until the last
  reload under way on that side is done (a side is fired at once, so the moment the whole side is loaded is what he
  waits for; cannons fired together finish within a fraction of a second, so it is also nearly always the next one),
  and how many are empty with nothing coming. A cannon is port when it was placed facing left of the ship's heading
  (its rest lay), else starboard. Coloured as the ship panel's own lines: all loaded "3/3 ready" in the game's orange
  (the panel's Ready for an ability), the seconds in its cooldown grey, the empty in its red. Manned cannons count like
  the rest (U fires them). Counted from each cannon's ZDO alone (its load, its reload step and that step's start, its
  last shot, on the network clock), so every peer would count the same whoever owns the cannons and nothing is sent or
  saved: loaded; reloading (a step under way, or resting after a shot, the rest and the step still to run, until a
  second past the rest, when a reload must have begun if one was coming); else empty. Ten times a second over the
  cannons aboard (the broadside's list, looked up again only when the ship's children change, else every 5 s), each
  line written only when its numbers change. **Where**: on ShipConfig's ship panel (under the minimap while aboard),
  under the ship's health, when ShipConfig 1.6.0 or later is there: neither mod references the other; ShipConfig
  publishes a container `ShipConfig_ShipPanel/ShipConfig_ShipPanelRows` under the HUD root with an inactive line
  `Template` in the panel's look (texts `label` and `value`, the game's tooltip); each line is a copy of it
  (`Broadside_PortGuns`, `Broadside_StarboardGuns`, tooltips saying what the numbers are), which the panel stacks under
  the health and closes up over when hidden, and which hide with the panel (with the large map open, say). **Without
  ShipConfig**, or while its panel stays hidden as he steers with the map closed (three counts, 0.3 s: its Ship Panel
  setting off, or the game not counting a high helm aboard; the panel coming back as the map closes takes a frame), the
  lines draw on their own in a small frame just under the minimap (under the ship
  HUD's wind indicator while that shows), in the minimap's own look, the game's (its dark frame and its font, nothing of
  ours), hidden while the large map is open: a captain without ShipConfig still sees his guns, and it costs one small
  class. Hidden off the helm and without cannons.
- **Checked offline** (the same harness, `rr`, the real new code, ShipConfig's real panel code beside it): U with A
  manning the bow cannon laid 10 degrees (a ship cannon; 64 on the mounted one) off its rest lay fires all three, his
  once from his game along his barrel (0.02 degrees off), still his and manned; his reload from his pack, B's two from
  B's; "3 of 3 cannons fired, 2 reloading". A letting go of the stern cannon 0.1 s into the ripple: it fires once, B's
  called shot, from B's pack. A's own pull in the frame U fires his cannon, or just before: one shot, U counting it "not
  loaded". Orders sent again late, manned and unmanned: one shot each. The count on the server, A's and B's machines
  alike every time: all ready; 0.5 s after U every cannon reloading, the seconds the rest and the step less 0.5; the
  short one empty a second past its rest; the seconds falling ten tenths a second; after the step "1/2, 1 empty"; a
  manned cannon counted with its gunner's reload; a gone one not at all. On screen: without ShipConfig a frame 200 wide
  6 px under the minimap, 36 high with two lines, 22 with one, hidden with the large map open and off the helm; with
  ShipConfig's panel (64 high alone) the two lines under the health (92 high), one line (78), none (64), their own frame
  hidden; the map open and closing, never their own frame; the panel hidden while steering, their own frame after 0.3 s
  and back on the panel when it shows. ShipConfig's panel alone with another mod's lines: one 78, two 92 in order, the
  first hidden 78, with two Sailing abilities under the divider (115, 101 without the lines). The loads of empty
  cannons (`rr loads`, a ship cannon and the large mounted one): three never fired, the server's, one to port and two
  to starboard: each asked of the server once, handed to B's game, loaded from B's pack 0.2 s apart bow to stern, the
  items taken once each, loaded on every machine, nothing more taken or asked as he steers on, then U "3 of 3 cannons
  fired, 3 reloading". One ball: the bow one loads, the two others stay empty ("0/2, 2 empty" in red), asked once
  only; given two balls they load within the 2 s retry. One owned by A's game unmanned: handed over and loaded; one A
  mans: never asked for nor charged, until he lets go, then loaded from B's pack. The earlier checks all still pass
  with it running.

- **Where the reload lines go**: rows `cannons.port` and `cannons.starboard` of Broadside's own ship panel (`KwShipPanel.Row`, `KwReloadRows`), written only when a side's count changed.
- **Keys**: Shift+E anywhere on it (its rear too) takes it up, Shift+E again lets go; E still mans it. Only from its own
  ship's deck ("Stand on its deck to haul it"; a moment in the air is fine). He is brought to its rear at once
  (`QuickStep`), walks (1 m/s at most, no running or jumping), and his fists hold its brackets' rear ends on every screen
  (`KwTowHands`).
- **How it rolls** (`KwDeckMath`): as a cart (`KwCartMath.Follow`): its middle rolls only along the way
  it points, never sideways, its rear at the handle's reach (1.07 m behind its middle, 1.19 m for the large gun) from
  his feet, so walking away pulls it straight, walking off to a side swings it round about its middle and then it
  follows, and walking toward its muzzle pushes it (his walk steered as a man steers a pushed cart, `KwCartMath.Pushed`).
  No faster than 1.2 m/s, never round faster than 20 degrees a second (it lags him then), and at most 5 cm and 3 degrees
  a frame. Its trucks turn by their travel on every screen (`KwWheels`, in the ship's frame).
- **On the deck** (`KwDeckFloor`, the ship's own colliders where its body's physics has them, never the cannon's own):
  each truck stands on the highest walkable face of the ship's under it (its decks, cabin roofs, a shut hatch's lid,
  another rider's top) within 6 cm over where it stands now, so it rolls up and down a slab's small step but never up
  onto a hatch's lid 18 cm proud nor down a step, an opening, a hatch or off the deck's edge. A ray slips down the
  seams between a deck's slabs (up to 6 cm wide on bubba where one is laid half a degree askew), so a truck's tread is
  looked at 6 cm ahead and behind and 5 cm to each side where its foot finds nothing. The carriage stands on its four
  trucks' feet, tilted along and across as the deck under them is (never more than 20 degrees).
- **What stops it**: anything of the ship's (its body's colliders: hull and bulwarks, railings, cabin walls, the mast and
  its spars' hit shapes, a hatch's lid, ladders, the helm, another cannon, a mount, a chest, any rider) in the carriage's
  room (from 6 cm over the deck to over its breech, out to its trucks' outer faces, from its rear to its front trucks) or
  the barrel's (round it, ahead of the brackets, out to the muzzle). A move that would put something there is not made;
  the turn alone, then the roll alone, are tried, and failing both it stays put and he is held back from walking on more
  than 0.25 m from its rear (`KwDeckHaul.Leash`). What it already stands in never holds it, room by room (one built with
  its barrel through a railing is hauled back out, its carriage never in). Players do not stop it (it nudges them).
- **Letting go**: Shift+E, walking off (more than 2.2 m from its rear), stepping off its ship, dying, logging out,
  dodging, swimming, being given other controls. It stays where it was left, at its rest lay relative to its new facing
  (the aim is the carriage's own), and the broadside, the shot arcs and the reload rows read it there (they read the
  cannons aboard each time). Nobody mans it while it is hauled, nor hauls it while it is manned, and the broadside
  neither fires nor reloads a hauled cannon (it is not free).
- **Its breeching** (`KwBreechingTie`): while someone hauls it (its ZDO names him) both
  ropes and the ringbolts on their deck plates are hidden on every screen; let go, its owner looks for something solid
  of the ship's (deck, railing, side) within 12 cm of each ringbolt's plate, ahead of its
  brackets, and makes them fast there as built; where either has none (over an opening) it writes `bs_arm_untied` and
  they stay hidden until it is hauled where they can be.

Checked offline (2026-10-26, `scratchpad/deckhaul`: the real `KwDeckMath`, `KwDeckFloor`, `KwDeckHaul`, `KwRiderPose` (now Broadside's `KwShipRider` keys),
`KwBreechingTie`, `KwShipSpec`, `KwCart` and Broadside's `KwShipRiderMove` and `KwShipRiderPose` on a fake engine with
exact box, sphere and capsule queries, on bubba's 1122 colliders read from the running game; the cannons' numbers checked
against the game's): every frame of every haul all four trucks on the deck and nothing of the ship's in the cannon. About
the open deck 6 m, turned 150 degrees, its pose written 9 times a second; pushed into the railing, the muzzle stops 1 to
1.5 cm short of it, calm and rough (heel 12, roll 6, pitch 3 degrees, 5 m/s, turning 3 degrees a second; the two stops
5 mm apart); hauled sideways into it, the carriage 4 cm off; into the mast 0.3 to 1.5 cm off; into another cannon, its
barrel against that one's carriage; into the cabin's front wall; at the hatch, shut (stopped by its lid) and open (no
truck more than its tread's 6 cm over the hole); on a deck without railings or sides, its front trucks at the deck's
edge; the large gun into the port railing; one built through the railing hauled back out; the hauler held within
0.27 m of its rear. Two peers on a rough sea: the other peer's cannon at most 0.17 m behind (never more than 2.4 cm a
frame), the same pose to the millimetre once let go, both peers' Broadside records holding it, and a world loaded again
from that record putting it there. Standing still on a rolling ship: nothing moved or written. Its breeching made fast on
the deck, left unmade over the open hatch, hidden while hauled.

## Damage

Asked for on 2026-10-26 ("the cannons need to do a lot more damage to ships ... maybe six shots from an iron field cannon
could sink a longboat"), then the same day "lower cannon damage by 25%", and that evening "cannon balls need to do 25%
less damage": every ball and burst does three quarters of what it did each time (the burst's push kept), so ten iron
hits now sink the game's longship, and the other guns keep their ranks.

- **The game's ships** (their `WearNTear`, measured in game): raft 300 health, karve 500, longship (`VikingShip`, and the
  `Trailership`) 1000, drakkar (`VikingShip_Ashlands`) 3000. All take blunt, slash and lightning in full, pierce and frost
  at half, fire at 1.5 times (the drakkar at a quarter), nothing from chop, pickaxe, poison or spirit; tool tier 0.
- **How a shot reaches one**: the ball (the game's projectile, flown and tested on the gunner's game) hits the ship's wear
  once (`Projectile.OnHit`, `WearNTear.Damage`, `RPC_Damage` on the ship's owner, who takes off the resistances and the
  health and breaks the ship at 0, as for any hit), then its burst (the siege bomb's area effect, spawned where the ball
  stands, 1 m up) hits it once more: it always reaches the ship the ball struck, since the ball stands at most a step and a
  half past its hit point. No skill or random factor, and the ship adds nothing of its own (only its upside-down,
  water-impact and Ashlands damage). The world level raises every piece's health, GrindstoneSkills' Sailing raises a
  skilled builder's ship's, and ShipConfig's Damage Taken scales hits on ships (1 in LocalTesting).
- **The ball hits a piece or a ship 1.6 times as hard** (`KwShot.Structures`, `KwCannons.OnStructures`): `KwShotHits` raises
  the projectile's damage for its one hit on anything with a `WearNTear` and puts it back after, before the hit is sent.
  Creatures, players and the burst take the plain numbers (a direct hit on a creature: about 73, 141 and 298, plus
  burning from the large gun).

| Gun | Ball, powder | Ball on a creature; on a piece or ship | Burst | A hit on a ship |
| --- | --- | --- | --- | --- |
| Bronze | stone, 1 | 50; 80 blunt | 15 blunt, 2.5 m | 95 |
| Small iron | iron, 1 | 50.6; 81 blunt | 22.5 blunt, 2.5 m | 104 |
| Small | iron, 2 | 95.6; 153 blunt | 45 blunt, 3 m | 198 |
| Large | flametal, 3 | 185.6; 297 blunt | 78.75 blunt and 33.75 fire, 4 m | 426, drakkar 384 |

Hits to sink, before the second quarter off in brackets:

| Gun | Raft (300) | Karve (500) | Longship (1000) | Drakkar (3000) |
| --- | --- | --- | --- | --- |
| Bronze | 4 | 6 | 11 | 32 |
| Small iron | 3 (3) | 5 (4) | 10 (8) | 29 (22) |
| Small | 2 (2) | 3 (2) | 6 (4) | 16 (12) |
| Large | 1 (1) | 2 (1) | 3 (2) | 8 (6) |

**Stone shot** (2026-10-26, the bronze cannon's): a little under the small iron ball, about 92% of its hit on a ship. Its
ball keeps 50 blunt, the least a heavy shot is (`KwShipHitEnergy.HeavyBlunt`, read before the 1.6 is put on), so it still
holes a Broadside hull and goes through it as the iron ball does; the difference is in its burst (15 blunt, not 22.5).
About 22 stone shots sink what 20 iron ones do.

**Broadside ships** (`KW_ship`) take the same hits (the karve's resistances, fire better with better materials), and
Broadside holes the hull part nearest each hit (real see-through holes since 2026-10-26). Their health is on the game's
scale since 2026-10-26 (500 x the square root of (weight / 26 t) x (1 + 0.3 x materials tier)): one of the longship's size
(16.8 by 5.95 m, 104 t, Meadows wood) has about 1000, so ten iron hits sink it too.

## Into the sea

Asked for on 2026-10-26 ("let the cannonball travel through the water. It should slow down quite a bit once it hits the
water. It should travel at about full speed for at least a metre into the water, then start slowing down"). The balls
(`KwShot.Dives`, every cannon's) are no longer stopped at the surface by the game's projectile (its `m_canHitWater` off);
`KwShotWater`, on the shot's prefab, flies them on under, on the gunner's game only (the game's projectile moves only
there; every other screen sees it by its ZDO, so nothing more is sent):

- **Going in**: where the ball crosses the surface (the game's liquid level, waves and all) the shot's splash plays (its
  water hit effects, the raft's water impact, as the game plays them there), and its burst and hit effects are taken off:
  it bursts only above water.
- **Under**: about its full speed for the first metre of its path (`KwWaterFlight.Keep`), then the water's drag takes it
  off with the path, its speed times e^(-0.46 a metre): a fifth over the next 3.5 m, as an iron ball a hand across slows
  (water's drag on it against its weight, 3 x 1000 x 0.47 / (8 x 7800 x 0.06 m), about 0.4 a metre). Gravity, less an
  eighth for buoyancy, sinks it at about 3 m/s at most. It moves about a metre a physics step, so its speed is set for the
  middle of each coming step; what it strikes in the very step it meets the water (within that metre of the surface) is
  struck as above water.
- **Hits**: the game's projectile still tests its path, so what it meets under water takes the hit: a ship's hull below
  the waterline, a creature, a piece. Its damage goes with the energy it has left (its speed squared against what it went
  in with, `KwShotHits`), but on a Broadside ship, whose hit router weighs a heavy shot by its speed at impact itself
  (the projectile's own speed, as it reads for any hit; known by its component's name, the mod never referenced).
- **The end**: gone once slower than 2 m/s or 3 m under where it went in; no burst.

Offline (the game's projectile step in the harness, the sea at 0, both orders of the two components' physics steps): a
48 m/s ball going in 5, 15 and 30 degrees down keeps 48 m/s over its first metre, then 40 to 43 at 1.5 m, 20 to 23 at
3 m, 13 at 4 m, a fifth (9.6 m/s) at 4.5 to 4.7 m of path, 5 to 6 at 6 m, and is gone after 6 to 9 m, 3 m down, with no
burst. Fired from a cannon 40 m from a hull's side (1.5 m out of the water, 2.5 m under): into the sea 2 m short of it, it
strikes the hull 0.13 m under the waterline at 39 m/s (a game ship's hull takes 1.06 of the ball's damage, its structure
1.6 times the energy share 0.66; a creature 0.66); 4 m short, 0.31 m under at 14 m/s (0.14 and 0.09); 0.5 m short, it
strikes in the step it meets the water, 3 cm above the surface, as above water (full damage, its burst); a Broadside
hull takes the full damage each time and the speed at impact (39, 14 m/s) for its own reckoning.

## Multiplayer

- The cannon's state is in its own ZDO. Its gunner's game owns it while he mans it (the library hands it to him on E, as
  the game's helm does) and writes the aim, the reload and the shots; every other peer draws from it. Who may take it is
  decided by whoever owns it then.
- A captain's called shot (the broadside, a cannon nobody mans) hands the cannon's ZDO to the captain's game the way
  taking hold does, and his game writes the shot and flies it; a manned cannon's ordered shot is fired by its gunner's
  game, which owns it while he holds it, so one machine decides each shot and it never fires twice.
- The broadside's reload runs where the cannon's state is decided after the called shot, the captain's game (its ZDO's
  owner then), which also owns his inventory: the load is taken from his pack and the step written to the ZDO in the
  same frame, only while his game still owns the cannon and nobody mans it, so nothing is taken for a cannon another
  peer has, nothing twice, and nothing is lost (a step in the ZDO always finishes). Every peer draws the reload and
  counts it loaded from the ZDO alone. An empty cannon another peer owns is asked of it (`BS_Arm_Load`, one ask a
  cannon every 2 s at most) and handed to the captain's game the called shot's way before it loads it.
- The rest lay is decided by the cannon's owner, whoever that is when nobody mans it (the gunner's game as he lets go,
  the server or another player once he has left), and kept in the ZDO's aim; every peer eases after it.
- The helm key list, the shot arcs and the reload lines are the captain's own screen: nothing of them is sent or saved;
  the reload lines read only the cannons' ZDOs, so they are right whoever owns each cannon.
- A shot is flown and its hits tested on the gunner's game, which sends each hit to the owner of what it struck (the
  game's own `RPC_Damage`); the harder hit on a piece or ship is set before it is sent, so nothing new travels.
- A ship cannon on a launched Broadside ship's deck is hauled by its hauler's game, which owns its ZDO from Shift+E on
  (asked of the owner with `BS_Arm_Tow`, the game's cart's way: unless it is manned or hauled, the owner hands over its ZDO): each frame it puts the piece at its new pose under the ship
  (`KwDeckHaul`) and ten times a second while it moves, and as he lets go, writes that pose to the Broadside rider record
  (`kw_rider_pos`, `kw_rider_rot`, `KwShipRider`'s keys). Every other peer, a dedicated server too, sees
  the ZDO change and eases the piece there over 0.12 s (its `KwShipRiderMove`; the owner's own game takes the record
  without moving it), so everyone sees it roll smoothly, about a tenth of a second behind; its trucks turn by its travel
  on every screen. A saved world keeps the record, so it loads where it was left. Should the hauler leave the game, the
  game gives the ZDO to another peer, whose game clears his name and settles the cannon where his last write left it.
- A hauled ship cannon's breeching is hidden on every peer from the ZDO's hauler (`bs_arm_tower`); whether it is made
  fast where it was let go is decided once by its owner then and kept in `bs_arm_untied`.
- Its recoil is drawn, not simulated: each peer, from the age of the
  ZDO's last shot, rolls its own copy of the carriage (the yaw part) back and out, slides the quoin,
  lays the ropes and moves the gunner's stand point aside and round with the carriage's turn (`KwShipRecoil`, from the
  synced yaw); the piece, its body and every collider (all on the piece, none on the carriage) stay where they are, so
  nothing is sent and nothing can desync. The gunner's own game walks him aside and after the breech, and everyone sees
  him by his own synced position. Nothing of it runs on a dedicated server.

## Ship cannon proportions

Asked for on 2026-10-25 ("a little too tall, and the wheels are way too close together ... make it look more like the
cannon in that image"): the carriages were reshaped to a side-on photograph of a naval gun on its truck carriage in front
of HMS Victory. Measured in pixels on the near side, taken as orthographic (the gun is turned about 13 degrees, so the
far side shows a little), in the front truck's diameter D (128 px):

| What | Photo | The ship cannons |
| --- | --- | --- |
| Rear truck | 0.95 D (nearer the camera; about 0.92 D) | 0.92 D |
| Axles apart | 2.9 D | 2.75 D |
| Brackets, front to rear end | 4.0 D (4.3 D with the bed and quoin behind) | 3.77 D |
| Front of the brackets ahead of the front axle; rear end behind the rear axle | 0.42 D; 0.68 D | 0.42 D; 0.6 D |
| Trunnions behind the front axle; behind the brackets' front | 0.22 D; 0.64 D | 0.3 D; 0.72 D |
| Bore's axis over the ground at the trunnions | 1.85 to 1.9 D | 1.94 D |
| Head: top over the ground; length behind the trunnions | 1.71 D (0.2 D under the axis); 1.15 D | 1.84 D (0.1 D under it); 1.15 D |
| Steps below the head | four, tops falling 0.22, 0.17, 0.17, 0.16 D, 0.4 to 0.67 D long | four, even |
| Rear step: top over the ground; depth | 1.11 D; 0.6 D | 1.1 D; 0.54 D |
| Underside | on the axletrees (0.52 D at the rear), arched up to 0.68 D between them | the same, the arch 0.13 D |
| Barrel | 6.4 D cascabel to muzzle, 6.0 D base ring to muzzle | 5.95, 6.15 and 6.05 D, back of the T-handle to muzzle |
| Breech and muzzle diameters | 1.1 D and 0.77 D | 0.94 and 0.72 D (small iron), 1.1 and 0.82 D (large) |
| Trunnions along the barrel, from the back | 45 to 49 % of the base ring to the muzzle | 45, 44 and 46 % of the back face to the muzzle |
| Muzzle past the brackets' front | 2.4 D (2.6 D allowing for the turn) | 2.39, 2.59 and 2.43 D |

Each carriage is the one design in proportion to its front trucks (`KwShipSpec.Size`): 0.35 m for the two small guns,
0.41 m for the large one. The trunnions stand 0.68 and 0.8 m over the deck (0.74 and 0.86 before), the axles 0.96 and
1.13 m apart (0.30 to 0.43 before), the brackets 1.32 and 1.55 m long (0.64 to 0.88 before), the barrels with door and
T-handle 2.08, 2.15 and 2.48 m (1.2, 1.4 and 1.65 before). Where it is not the photo's, the gunner decided it: a breech
far behind the trunnions drops as the gun is raised, so the breech was not drawn back as far as the photo's cascabel
(the T-handle sits 6 to 8 cm inside the brackets' rear end, the brackets and axles a little shorter to match), the
trunnions stand a little higher, the elevation is -4 to +14 degrees (-8 to +20, -6 to +18 before; at more depression
the quoin's thick end would stand up behind the breech door) and the gunner bends further. With the harness's arm
(0.682 m) both fists reach their grips at every elevation up to level and at full elevation fall as short as they did
before at 20 degrees (0.80 m small, 0.73 m large, to the T-handle), and his stand point now turns with the carriage, so
a traverse no longer takes the breech out of his reach. At high elevation the quoin's thick end stands behind and above
the low breech end, as a real one does, and the door may brush it as it swings open for a reload there.

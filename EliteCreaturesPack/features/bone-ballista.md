# Elite Creatures Pack - specification: The Bone Ballista

One feature of the mod, specified on its own. The other feature files sit beside it.

Asked for by the user on 2026-10-08: "look at the ballista, will you make a much smaller version made of bone. the arms
should be made of spine. this will need to be placeable like the ballista, but this one should be player controlled. it
takes bone missiles ... When player presses E on it they will hold the ballista, when fired the bow should spring
forward and bolt leaves and shoots out. it should only have like a 90 degree radius to turn, your character should
shuffle back and forth when rotating ballista. when reloading, the character uses 2 hands to pull the string back, then
loads a bolt down into the center channel, then its ready to fire. This will be for Elite Creatures Pack."

**Status: built, not tested in game.** Code in `EliteCreaturesPack/Ballista/`; model, missile and icons in
`../ValheimAssets/Assets/Props/BoneBallista` (bundle `ecp_boneballista`, contract in its `BRIEF.md`). Builds, the
offline patch check passes.

## What the player sees

- **Building it.** Hammer, Misc tab, right after the game's Ballista, at a workbench: 3 Spines and 25 bone fragments
  (the user, 2026-10-09). Placed like the game's ballista (it is a copy of it: placement, wear, player base area),
  200 health (the game's is 400; the user, 2026-10-09), wood hit and break effects. About half the game ballista's size: it turns at 1.10 m.
- **Bone Missiles.** Workbench level 2: 5 bone fragments and 2 feathers make 20 (the game's Wooden Missile is 5 core wood
  and 2 feathers for 20 at the artisan table). 50 blunt (the user, 2026-10-09), knockback 40, 45 m/s, no drop; the Crossbows skill rises with
  hits. Their own ammo type: only the Bone Ballista takes them, and it takes nothing else.
- **Holding it.** The use key on it (standing within 3.5 m of its back): the player walks to the spot behind it, hand
  items put away, both fists on its handles; their body passes through the ballista while holding it (the player's
  capsule is wider than the room behind the post; rubbing on it slowed every side-step). Movement keys do nothing; the use key again, jump or dodge let go (hand
  items come back). One holder at a time ("in use" otherwise).
- **Aiming.** It follows the view, and the view turns no faster than 240 degrees a second while holding it (what
  the holder's jog round its back keeps up with), so view, ballista and feet stop together when the mouse stops; at most 45 degrees either side of where it was placed (90 in all), 20 down to 25 up (the user, 2026-10-09).
  The feet stop a few centimetres from their spot and move again only once it has moved 15 cm (no walking in place). The camera is held to the same turn while holding it (no looking behind; the user,
  2026-10-09). The holder keeps facing the way it points while their feet walk the arc
  behind it: turning it makes them side-step back and forth.
- **Reloading, by itself while the holder carries Bone Missiles.** Pull (1.4 s): step in and lean, both hands take the
  string either side of the groove, haul it back to the latch with the body, from bent forward to leaning back, while stepping back (the spine arms
  bend back), click. The grab: a jog up from up to 3 m facing the way they go, a turn into place, the hands on in
  about an eighth of a second, hand items put away without the sheathing animation.
  Load (1.2 s): step in, the right hand takes a missile from the hip (taken from the inventory then), carries it over the
  groove and lays it down into it, hand back to the handle. Ready.
- **Shooting.** Attack: the string snaps forward, the arms spring past rest and ring, the nose kicks up, the missile
  leaves along the groove. Half a second later the next pull starts. Attack with nothing laid and no missiles: "No bone
  missiles".
- **Hover.** "Bone Ballista (loaded/empty)", "[E] Hold" (or "Let go").

## Multiplayer

- The use key sends a request to the ballista's owner, which grants it when nobody valid holds it (a holder in the
  world, alive, within 6 m), writes the holder into the ZDO and hands the ZDO to them (the chest pattern:
  `ForceSendZDO`, `SetOwner`, answer). The holder's machine then aims, reloads and shoots and writes everything.
- ZDO keys (written by the owner): `ecp_bal_user` (long, holder's player ID), `ecp_bal_yaw`, `ecp_bal_pitch` (floats,
  degrees), `ecp_bal_spring` (0 slack, 1 drawn, 2 loaded), `ecp_bal_act` (0 none, 1 pulling, 2 loading),
  `ecp_bal_act_at` (network ticks), `ecp_bal_shots` (count), `ecp_bal_shot_at` (ticks). RPCs `ecp_bal_request`,
  `ecp_bal_release` (long player ID), `ecp_bal_answer` (bool).
- Every peer that draws runs `BallistaLook` from those keys and the timeline (`BallistaTimeline`): the turn (eased for
  non-holders), arms, string, laid missile, sounds, the holder's arms and lean. Nothing is sent per frame but the aim
  (written when it moves more than 0.1 degree). The holder's feet move with the game's own movement (synced as any
  player's). The shot is a networked projectile owned by the holder.
- Letting go mid-action finishes the action at once (a pull leaves it drawn, a load leaves it loaded: the missile was
  already taken). A holder who leaves or dies stops counting as one.

## In the mod

| File | What |
| --- | --- |
| `BallistaPrefabs` | builds once, registers on every scene wake, effects (game: `fx_turret_fire`, `sfx_reload_start`, `sfx_reload_done`, `fx_turret_addammo`) |
| `BallistaPiece` | `ECP_BoneBallista`: copy of `piece_turret`, `Turret` removed, children but `PlayerBase` stripped, bundle model hung into its frame, two missile copies, dressed in `Turret_mat` |
| `BallistaFrame`, `BallistaParts` | the flat bundle hung into yaw/pitch/arm chains; one instance's parts by name |
| `BallistaMissile` | `ECP_BoneMissile` (copy of the game's unused `TurretBoltBone`) and `ECP_BoneMissile_projectile` (copy of `Turret_projectilebone`, trail kept) |
| `BallistaCrafting` | hammer entry after `piece_turret`, piece cost, missile recipe; ObjectDB.Awake/CopyOtherDB postfixes, settings change |
| `BallistaControl` | the piece's component: use, hover, doodad control, request/answer/release RPCs |
| `BallistaOperator`, `BallistaShot` | the holder's aim, reload, trigger, steering, the shot |
| `BallistaTimeline`, `BallistaState` | the reload's moments and the shot's spring; the ZDO keys and one read of them |
| `BallistaLook`, `BallistaHands`, `BallistaRig` | the drawing; the holder's arms (two-bone IK) and lean |
| `BallistaPatches` | `Player.SetControls` prefix (attack shoots, nothing else lets go) and postfix (steer), `Character.UpdateRotation` prefix (holder keeps facing the ballista), `Player.SetMouseLook` postfix (the view held to the ballista's turn) |
| `BallistaWords`, `BallistaSettings` | words; section `28 - Bone Ballista`, one switch `Enabled` |

## Decisions made without the user (overturn freely)

- Reload runs by itself whenever the holder has missiles (like the Arbalest), not on a key; attack only shoots.
- Damage 50 (blunt by the user; Wooden Missile 75 pierce, Black Metal 120): 2.6 s reload.
- The missile recipe; Misc tab after the game's ballista.
- The hands and lean are code (two-bone IK on the game's animation), not new player clips; the walk is the game's.
- Ammo type of its own, so the game's ballista never takes bone missiles and ours takes nothing else.

## Test checklist

- [ ] It shows in the hammer (Misc, after Ballista) with its icon and cost; places like the game's; hover text.
- [ ] Bone Missiles at workbench 2, 20 per craft, icon, on the ground.
- [ ] E holds: walks to the spot, hands on the handles, hand items hidden; E/jump/dodge let go, items back.
- [ ] Aim follows the camera within 45 degrees either way, turn speed; the holder side-steps on the arc and faces forward.
- [ ] The arms bend back as the string draws and spring forward and ring on a shot; strings follow the tips.
- [ ] Pull and load poses: both hands on the string, the missile from the hip into the groove; sounds at each moment.
- [ ] The shot leaves along the groove, hits, raises Crossbows; "No bone missiles" with none.
- [ ] Second player: sees the turn, the hands, the reload and the shot; "in use" when they try to hold it.
- [ ] Dedicated server: request and ownership hand-off; letting go mid-reload; holder logging out.

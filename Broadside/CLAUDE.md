# CLAUDE.md - Broadside

Ship building: a dry dock, hull tools that make every keel, rib, plank run, deck, railing, ladder and mast to size,
fittings (post caps, hatches, rudders, a captain's wheel) and a blueprint table that saves, builds back and shares designs; and every saved
design in the hammer, built on the water as a real ship that floats on its own hull, sails, and is steered by a
helmsman at its rudder's tiller or at its captain's wheel; and ship cannons, the only pieces built on a launched ship's
deck, manned, fired, hauled about the deck and fired as a broadside from the helm (`Cannons/`, "Cannons" below). Version 0.1.0, unreleased, never run in game as its own mod. GUID
`milkyteam.broadside`, namespace `Broadside`, no .cfg (no settings). Required on the server and every client: the
pieces and the ship are networked prefabs and the server keeps the shared ships.

Built as the Broadside prototype ("fit kit") inside the dev-only AssetLab mod and moved out on 2026-10-24 with the
ship building only (the user's words: "Only have the custom ship building in it. Do not add any of the storage,
hammock, weapons in there at all." and "Let's make the keel block shipbuilding thing just for designing ships for
now. When it's destroyed nothing happens."). The same day the user asked "why can't I build my custom ships using
the hammer?", and the hammer's saved-ship entries and launched ships came back from the prototype (`Ships/`, `Helm/`,
the `KwBp*` entry files), with what they cost, the helmsman at a tiller and the prototype's later fixes. The prototype
stays on disk, gitignored and no longer compiled, in `AssetLab/AssetLab/Keelwright/` as the archive of what is still
left out: the weapon mounts and the other ship weapons (`Arms/`, with `ANIMATION.md`; its cannons came back as the ship
cannons on 2026-10-26, from the AssetLab Cannons bench that had tried them out), storage (and with it a ship's hold), hammocks,
lanterns, seats and benches, paddle ports, ship pushing and the table's Launch button. Anything from there comes
back only as a new, deliberate change: the captain's wheel came back on 2026-10-25 (the user's words: "We also need
the steering wheel back. We also need a way that the rudder connects to the steering wheel, so maybe on the underside
of the top decking where the rudder is, we have a system that connects to the wheel."), with a steering gear under the
deck in place of the deck tiller. The ship formulas are in
`AssetLab/AssetLab/Keelwright/FORMULAS.md` and in `KwShipStats`.

`PROBLEMS.md` keeps the problems met so far and how they were solved.

## Rules

- **Design mode on the dock.** Every Broadside piece costs nothing to build (`Piece.m_resources` empty, set in
  `KwPrefabs.Configure` for every prefab), and removing or breaking one gives nothing back and drops nothing. On the
  dock a biome tier is a look only. Destroying a dock piece or the
  blueprint table only removes that piece. A docked rudder's blade stands amidships, unless a captain's wheel steers it through its steering gear: then it follows
  that wheel's helm, so the gear is seen working; a docked sail still moves in the
  game's wind and can be raised and lowered.
- **Meadows and Black Forest only, for now.** New work is offered only what the first two biomes give (2026-10-26, the
  user's words: "right now we should only be supporting the Meadows and Black Forest textures and sails and everything
  like that. We should not have any of the higher tier stuff yet, only meadows and Black Forest compatible items."):
  every "Material" choice, in every tool and edit window (keel, rib, planking, deck, railing, roof railing, cabin, a
  railing added at sea), offers tiers `meadows` and `blackforest` (`KwBiomeTiers.Offered`, `KwBiomeField`); the hammer
  has only `KW_rudder_meadows` and `KW_rudder_blackforest` (`KwRudderTiers.Offered`, `KwHammer`); sails, jibs and new
  sails added to a mast are offered raw hide only, cloth `rawhide` (`KwSailCloth.Offered`, `KwClothField`; linen needs
  the Plains' flax, scale hide the Mistlands'); a pennant is offered none or "As the sails" (the dyed colours are linen,
  `KwPennantLook`). Nothing saved changes: the five other tiers, the five other rudders (registered, out of the hammer),
  linen and scale hide sails and dyed pennants all still load, draw, steer and cost as before, so an older blueprint
  builds on the dock and from the hammer as it was (at its own tiers' cost). An edit window on such a part shows its own
  tier, cloth or colour after the offered ones, picked, and keeps it unless the player picks an offered one; a
  sail added to a linen or scale hide mast is raw hide. Post caps, the anchor windlass and a cap at sea take the look
  of the tier they stand on; nothing else a player picks needs a higher-tier item (a frame's "Iron-banded" costs the
  tier's hull fittings, resin or bronze nails, no iron). The ship cannons and their shot and powder are the
  cannons' own (`Cannons/`, "Cannons" below), not limited here.
- **Real ships from the hammer.** A saved design (`KW_bp_<path>`) is built on the water like the game's own ships: a
  workbench in range (the game's own check, the karve's station), and its cost, what its parts would have cost built by
  hand (`KwBpCost`): each planking, deck, keel or rib part its tier's two materials by its size (`KwBiomeCost`); each
  railing by its length along the ship (half that for one side only; a roof railing by its railed edges' length) in the planking's materials of its
  own tier (`KwRailLooks`; one built before railings had a tier: of the tier of the planking it stands on, the blueprint's
  planking from the same clicks: same place, way and line; Meadows' wood and resin with none), 1 bulk per 2 m and 1
  fittings per 4 m; each ladder 1 wood per metre of its height (at least 1); each mast 1
  wood per metre of its height (at least 2), ropes and rigging included, and each of its sails 1 of its cloth's item per
  2 m² (at least 1 a sail: raw hide `DeerHide`, linen `LinenThread`, scale hide `ScaleHide`, `KwSailCloth.Item`), a
  crow's nest 6 wood, a fighting top 8, a pennant 1 of its cloth's item; each bowsprit 1 wood per metre (at least 2)
  and 1 a jib's club, each jib 1 of its cloth's item per 2 m² (cut to the blueprint's foremast; none without one), all in
  `KwRigCost`; each cabin its tier's hull materials by the area of its walls and raised sides and its deck
  materials by its roof's, at the planking's rates, and 1 wood a corner post (`KwHouseCost`); each opening
  (`KwOpeningCost`, hooked in `KwBpCost`): a frame one of the tier's hull bulk (two for a heavy one, iron one hull
  fitting more), a cover one of the tier's deck bulk, a door 2 wood (a double door 4); each
  rudder its biome's recipe (`KwRudderTier.Recipe`), and with a saved steering gear its recipe's first item a metre per 2
  of shaft (at least 2) and its second a metre per 5 (at least 1, `KwRudderGear.Cost`); each Post cap tool cap one wood on the standard post, its size squared times its
  length rounded (`KwCapSpec.Cost`; a carved head the same, its lean and facing free); each old hammer cap, hatch, name board or captain's wheel one wood (painted bands and
  hung shields nothing). All from the
  saved part texts (a mast's sails as placed to fit it). The piece shows as many requirements as the build menu has
  slots beside the workbench; the rest are checked and taken at the click. The launched ship (`KW_ship`) broken up with
  the hammer's remove gives all of it back, in full, in the game's floating cargo crates (`KwShipBreak`, below); wrecked
  or cleared with `kwship clear` it gives nothing; its boarding ladder (the karve's own, remade) is not a saved part and costs nothing.
- **Built aboard once launched: ship cannons only.** The dock designs the whole ship; the only pieces built with the
  hammer on a launched ship are Broadside's ship cannons (`BS_cannon_*`, `KwShipRiderRules`), which ride it
  (`KwShipRider`) at their own cost and with the game's own removal (2026-10-26, the user's words: "We also need to not
  allow building with the vanilla Valheim pieces on the ship, but you should be allowed to build cannons on your ship").
  The game's pieces and every other mod's are refused there ("Only ship cannons are built on a launched ship"); pieces
  already riding a ship stay. Ship cannons are built nowhere else (not on the dock's design parts, not on land:
  `KwArmPlace`). Broadside's own dock pieces are never built there (its tools edit a launched ship their own way), and
  blueprints never carry riders.
- **Own code only.** Nothing from another mod is compiled in (the prototype used Elite Creatures Pack's `Core`);
  logging is the mod's own `Log`.
- **Assets ship with the mod.** The textures and tool icons in `Broadside/assets` (copied from
  `ValheimAssets/Assets/Ships/Keelwright`) are embedded as `Broadside.assets.<file>`. A repainted texture is copied
  in again by hand. The planking's are long (`kw_<tier>_hull.png`, 2048 by 512 px, from `tier_planking/make.py` there):
  the albedo as 256 colours, its relief as 256 greys (`_h.png`, the normal map made from it on load, `KwRelief`), its
  metal mask and glow map; 5.7 MB for all seven tiers. The blueprint table's model is a bundle per platform in `Broadside/assets/bundles`
  (`kw_bptable.windows` and `.linux`, the dedicated server's; built in `ValheimAssets/Assets/Ships/Keelwright/BlueprintTable`
  with `build.ps1 -SkipBlender -Bundle` and checked with its `verify_bundle.py`), embedded as
  `Broadside.assets.kw_bptable.<platform>`; a rebuilt bundle is copied in again by hand, both files together. The
  Post cap tool's carved heads are the bundle `kw_postheads` (`.windows` and `.linux`, built in
  `ValheimAssets/Assets/Ships/Keelwright/PostHeads` with its `build.ps1` and `ph_split.py`, checked with its
  `verify_bundle.py`; embedded as `Broadside.assets.kw_postheads.<platform>`, copied in by hand the same way): prefabs
  `kw_head_dragon`, `_serpent`, `_wolf`, `_raven`, `_horse`, each a `model` mesh on the shared material `kw_postheads`
  (256 px atlas and normal map) and an empty `head_start` at the neck's top, the foot 0.40 by 0.30 m, +y up, looking
  along +z. The meshes of the 2026-10-25 build sit where the workshop laid the five out side by side, not at their
  prefab's origin; `KwCapHead` centres each on its foot's underside, so a fixed build needs no change. Only loaded where
  there is a screen. The Wooden Repair Kit's model, icon and hull patch are the bundle `kw_repairkit` (`.windows` and
  `.linux`, built in ValheimAssets, embedded as `Broadside.assets.kw_repairkit.<platform>` once copied in with its csproj
  line; `KwRepairKitLook`, loaded on every peer, the server's Linux one too); without it the kit wears fine wood's look and
  the patches are generated boards.
- The workspace rules hold: methods at most 24 lines brace to brace (lambdas count), classes at most 300 lines,
  prefix/postfix patches only, multiplayer when first built.

## Multiplayer

- Every prefab is made once on each peer when the network scene first wakes and registered with `NetPrefabs.Register`
  (`KwPrefabs`), the dedicated server included (icons are rendered only where there is a graphics device).
- A tool's part is spawned on the clicking player's game as an ordinary networked piece; what it is made from (its
  span, sizes, mast and tier) lives in its ZDO and every peer builds the same mesh from it (`KwGenerated`).
- Editing a part (`KwEdit`): the old part is removed the hammer's way (its owner destroys it) and the new one spawned on
  the editing player's game in its place, with the old one's other ZDO values; a part builds its mesh from its text
  once, so a rewritten text would never show on other peers. Nothing new is sent. While the edit's window is open the
  parts are hidden only on the editing player's screen (`KwEditHide`: their renderers' `forceRenderingOff`, colliders
  and ZDO untouched) and its preview is a local object (`KwEditPreview`); other players see the parts until they are replaced.
  A cabin edit carries the cabins on its roof and what stands on them the same way (`KwCabinFollow`): each removed by its
  owner and spawned again, raised or moved, on the editing player's game in the same frame; nothing new is sent.
- Hatches and masts keep their state in their ZDO, written only by the owner, asked by routed RPC (`KW_HatchToggle`,
  `KW_SailHoist`). A rudder's lay, and the ship's waterline on it, are measured by its owner and saved in its ZDO; every peer draws what the ZDO holds.
- Captain's wheel and steering gear: a dock wheel's helm is the prototype's (`KwWheelHelm`): E asks the wheel's owner
  (`KW_Helm`, answered with `KW_HelmReply`), who keeps the helmsman in its ZDO (`kw_helmsman`) and the helm the
  helmsman sends ten times a second while turning (`KW_HelmTurn`) in `kw_helm`; every peer turns the wheel, and the
  rudder linked to it (the blade following the wheel as drawn, `KwWheelHelm.Shown`), from that ZDO. The steering gear
  is measured by the rudder's owner with its lay, from the dock's pieces round it (`KwRudderGearDock`), again whenever
  their signature changes (a wheel, deck, planking, hatch, ladder, mast, keel, cabin or windlass placed, removed or
  loaded, a doorway or window cut, a cabin's text changed; looked at every 2 s), and saved in the rudder's ZDO (`kw_rudder_gear`, beside `kw_rudder_lay`), so it travels in a
  blueprint; every peer draws the shafts from it, each turned by the rudder's angle (`KwGearTrain.Spin`). On a launched
  ship it comes from the blueprint (laid out there from the blueprint when saved without one, the same on every peer),
  the wheel, the blade and the shafts turn from the ship's synced rudder value (the game's sculling wiggle left out),
  and the helmsman stands behind the wheel on his own feet, carried by his own sync. Only drawn: a dedicated server
  builds no wheel or gear.
- Blueprints: each player's ships are files on their own machine; the shared ships live on the server and travel in
  parts through the BlueprintShelf library's RPCs (`KW_Bench*`). Building a blueprint spawns ordinary pieces on the
  acting player's game, a few a frame; clearing the dock or removing a whole ship goes through the game's
  `WearNTear.Remove`, warded pieces kept. The hammer's saved-ship entries are client-side prefabs, never networked.
- Launched ships: one networked object each (`KW_ship`, a copy of the karve, registered on every peer), spawned on the
  building player's game; its ZDO carries its blueprint (`kw_ship`, gzipped JSON in the ship's own frame), and every
  peer builds the same ship from it (`KwShipBody`). The game moves, owns, syncs and saves it as its own ships; its
  physics run on the owner (`KwShipDrive`: the game's controls and pushes, the hull's own buoyancy, `KwShipHydro`).
  Its hatches keep their state in the ship's ZDO (`kw_hatch<n>_open`), flipped by the owner on `KW_Hatch<n>_Toggle`.
  Its holes, its parts' wear and the water in its hold are in its ZDO too (`kw_holes`, `kw_hull_wear`, `kw_flood`, and
  each compartment's water in `kw_flood_parts`; a ship with `kw_flood` alone has it spread at one level), written by
  the owner, where the game's wear takes the hits, the sea is let in and the water's weight pressed in the physics
  step; bailing (`KW_Bail`, the bucket and the bailer's compartment) and mending a hole (`KW_HoleMend`, by its number) are calls to
  it; every peer cuts the same holes from the ZDO (each hole's ragged outline seeded by its own number,
  `KwHullHole.Id`): through the hull's look where there is a screen (`KwHoleLooks`) and through the ship's boxes on every
  peer, a dedicated server too (`KwHoleBoxes`), and works out each compartment's water surface from the ZDO; the
  compartments come from the blueprint, the same on every peer. A shot that bursts is given the ship's hit at the
  burst's own place and the shot's way on the shooter's game, before the hit is sent to the owner (`KwHullBurst`; an
  area burst's, the game's `Aoe`, its middle).
  Heavy shots (2026-10-26; a projectile of 50 blunt or 300 pierce, a cannonball, never an arrow): on the shooter's game,
  where the game's projectiles hit, the game's hit on the ship's wear is not sent; `KW_ShipShot` (one `ZPackage`: the
  game's `HitData`, where it struck and its flight in the ship's frame as the shooter's game sees the ship's body, its
  speed, the range flown, its projectile prefab, the share a body takes) goes to the owner instead, passed on once if
  ownership moved, and the projectile's burst is held back. The owner traces the path through its ship's colliders as
  its body stands (`Physics.RaycastAll`) and runs each kind's taker in order (`KwShipHitRouter`): holes in its ZDO's
  `kw_holes`, broken railings in `kw_rails_broken`, the pieces built aboard and the characters hit the game's way
  (`WearNTear.Damage`, `Character.Damage`, to their owners), the ship's health once through the game's own damage path;
  then the burst where the shot stopped, or the projectile flying on past the far side, both networked objects the owner
  spawns. Every peer draws holes and broken railings from the ZDO and takes the railings' boxes away there (a dedicated
  server too). Mending a railing is `KW_RailMend(piece, part)` to the owner, from the hammer's repair over it.
  Repair kits and the hammer (2026-10-26, the user's words: "Create an item ... a ship repair kit ... the whole idea is to
  stop the water seeping in and sinking the ship further. Then, when a ship gets back to port and they start repairing it
  with the hammer, these wooden repair kits should just go away"): the Wooden Repair Kit (`KW_RepairKit_Wood`, a networked
  item prefab registered on every peer, its recipe in every ObjectDB). Patching: the player's game finds the hole aimed at
  (plain maths on the ZDO's holes, `KwHoleAim`; a ray through a hole meets no collider) and asks the ship's owner
  (`KW_HolePatch(id)`); the owner adds the hole's number and radius to the ship's `kw_hull_patches` and answers the asker
  (`KW_HolePatched(id)`), whose game only then takes one kit. Every peer reads the patches, lets no sea in through a patched
  hole (the owner's flood step takes `KwHullHoles.Leaking`) and draws the patch, hammered on with the game's sound as it
  appears; a patch whose hole is mended or widened past it is off at once on every peer (it breaks away and falls) and the
  owner then takes it out of the ZDO. The hammer: each click on a launched ship (the game's `WearNTear.Repair`, a workbench
  in range) mends one thing, chosen on the repairing player's game from the ship's ZDO as it reads it (what is aimed at,
  else the next step in order), asked of the owner by that thing's own call (`KW_HoleMend`, `KW_RailMend`, `KW_RigMend`,
  `KW_RigRaise`, a fallen piece's cost taken on `KW_RigRaised`); a step asked is skipped for 1.5 s so quick clicks go on to
  the next. What every peer sees (a patch falling, a mast growing back) is played from the ZDO's change.
  The helmsman stays on his own feet and the player's own sync carries him; his pose is worked out on every screen from
  synced state (`KwHelmsman`). What is only drawn (the ship's look, shadow, materials, water mask, its rudder, sail
  cloth, the ladder over the side's look, the helmsman's pose) is never built on a dedicated server (`KwShipBody.Drawn`); the
  hatches are, as the ship's owner answers their calls. The ladder over the side and its climb's place are worked out
  from the blueprint on every peer, a dedicated server too (`KwShipLadderFind`, plain maths, nothing sent or saved).
- Looks: a planking part's painted bands are the last segment of its span text (`paint=3-3:red,6-7:blue`), drawn on
  every screen. A name board's name is in its ZDO (`kw_name`); on a launched ship in the ship's (`kw_name`, set by the
  owner on `KW_ShipName`, also copied to the pin's `kw_ship_name`). Hung shields are whole items in one ZDO text
  (`kw_shields`): a dock railing's are changed the item stand's way (the acting game owns the railing, asked with
  `KW_ShieldOwn`, then writes the ZDO and moves the item itself); a launched ship's (its owner runs its physics, so it is
  never handed over) by the owner's confirmation: `KW_ShieldHang` / `KW_ShieldHung` (the item leaves the inventory only
  then, else `KW_ShieldUndo`), `KW_ShieldTake` / `KW_ShieldGiven`. A blueprint keeps `kw_shields` as data only, never
  given back to a built piece; a destroyed railing or ship drops its shields on its owner.
- Openings: each part's text carries an `open=id,wall,shape,u,sill,w,h,frame,cover;...` segment, so they travel with the
  ZDO, the blueprint and a launched ship's packed blueprint. In a planking part's text it sits before `|paint=` (the
  paint reader takes everything after it); in a cabin's it comes last. On the dock each opening's leaf state (one for all its leaves: a double door's two, a pair of shutters) is the int
  `kw_open<id>` in the part's ZDO, changed by the RPC `KW_OpeningUse(int id, int way)` to the part's owner; the game's
  wood_door sound plays on a change. On a launched ship the covered openings are numbered in blueprint order
  (`KwShipOpeningPlan`); state is `kw_ship_open<n>` in the ship's ZDO (0 shut, 1 or -1 open), written only by the ship's
  owner on `KW_ShipOpeningUse(int n, int way, int revision)`; every peer polls it every 0.1 s while the ship's
  `kw_ship_rev` is the revision its openings were numbered in. Leaves are drawn only where the ship is
  drawn; their colliders, the RPC and the leaks exist everywhere, a dedicated server included. Doors and windows cut,
  changed or filled in at sea (2026-10-26, `Sea/KwSeaOpening*`) are a sea edit of the planking parts' and cabins' texts:
  the owner checks only their openings changed, moves every covered opening's state to its new number in the same frame
  (a new one shut, numbers no longer used cleared; a call numbered in an older revision is ignored), drops the holes in
  the openings' places (with their patches; the part's other holes and wear stay); every peer then numbers the leaves
  again (`KwShipOpenings.Replan`: changed parts' leaves hung anew, the others renumbered) and draws and boxes only the
  changed parts again.
- Rig (bowsprit, jibs, tops, pennants): nothing new is sent or saved besides the parts' own texts. A bowsprit's jibs are
  cut on every peer to the stay found from the masts' texts (`KwJibStay`), set with the foremast's ZDO hoist on the dock
  and by the ship's synced speed afloat; the platforms are in the mast's mesh (its collider on the dock) and in a
  launched ship's boxes on every peer; climbing moves only the climber, whose own sync carries them; a pennant's shape
  comes from the game's wind and `ZNet`'s clock, the same on every screen. Jibs, pennants and climbs are only drawn.
  Where every rope ties on, and its fitting, is worked out on each screen from the pieces' saved shapes (`KwRigTies`, plain
  maths, no physics), so every peer draws the same; a dedicated server works out nothing.
- Sail patterns: in the parts' own texts only (a sail's fifth field, a bowsprit's sixth), so they travel with the ZDO,
  the blueprint and a launched ship's packed blueprint. Every screen paints its own textures from them (never a
  dedicated server, which keeps the plain cloth); a player's own picture is named by its file
  (`img/<name>.png` in `BepInEx/config/Broadside.Sails/`) and painted only where that file is.
- Sailing (`KwShipSailing`): the anchor's state is in the ship's ZDO (`kw_anchor_down`, `kw_anchor_at`), flipped by the
  owner on `KW_AnchorToggle`, held by the owner's physics step; the windlasses and the drawn anchor and cable are
  screens only. Where the cable leaves the ship (the first windlass's fairlead, `KwAnchorPlan`) is worked out from the
  blueprint on every peer, the server too, plain maths only, so the owner lets go and rides to the anchor there; on the
  dock each screen works it out from the dock's pieces (`KwAnchorDock`); nothing is sent or saved for it. Cargo: each
  player's own game reports its inventory weight to the ship's owner (`KW_ShipLoad`, every 3 s while aboard); the owner adds the carts on deck, changes the body's mass and the drag by what it added before,
  and keeps the sum in `kw_ship_load` for every peer's hover. Map pins: who launched a ship (`kw_ship_by`, written at
  launch) and who steered it last (`kw_ship_helm`, the owner copying the game's `s_user`) are in its ZDO; each player's
  game asks the server every 5 s (`KW_ShipWhere`), which looks the ships up among its own ZDOs (found once a session,
  plus `KW_ShipLaunched` from the launching game) and answers with places and names (`KW_ShipsAre`); pins are local
  and never saved. A ship with no anchor and nobody aboard keeps the game's behaviour: `KwShipDrive.Drag`, as the
  game's step, takes 90% of its level speed every physics step, so it stops at once and only rocks (the hull's lift is
  straight up), and with no player near it is not simulated at all.
- Sails by kind (2026-10-26, the user's words: "The normal square sails, like vanilla, need the wind behind them to give
  any speed. But the sails that start with G (gaff) ... can catch the wind too. Perhaps those sails could also move back
  and forth"): nothing new is sent or saved. The sails' push is worked out on the ship's owner in its physics step
  (`KwShipWind.Push`, from the game's wind and the body's own speed); every screen swings the fore-and-aft sails from
  the same game wind and the ship's speed as it sees the ship move (`KwBoomSwing`, `KwJibSwing`, never on a dedicated
  server); the ship's sails by kind (`KwSailMix`) come from its blueprint on every peer.
- Breaking up a launched ship (`KwShipBreak`, 2026-10-26, the user's words: "You should be allowed to break ships with
  the middle mouse button (the hammer's remove). All resources go in floating crates on the water."): the game's own
  removal, nothing new sent or saved. The remover's game checks as the game does (ward, no-build place, the karve's
  workbench in range) and refuses with "Everyone must leave the ship first" while any player is aboard (the onboard
  trigger, or over the hull's plan up to 25 m over its sheer: a cabin's roof, a mast), the remover included; the game's
  `RPC_Remove` goes to the ship's owner, who asks again whether anyone is aboard as it sees them (then the ship stays and
  the remover is told by the game's routed `ShowMessage`), else, before the game destroys the ship and its ZDO, makes
  the crates (networked `CargoCrate`s, the game's, so every peer sees the same) with the whole cost and the hung shields
  (the hooks emptied in the ZDO first, so the drop on destroy finds none), and the wood breaking effects along the hull
  (networked effects, made on the owner as the game makes them). A dedicated server owner does the same; nothing drawn.
  The pieces built on it (riders, below) are taken over by the owner and destroyed the game's way first, and every item
  they drop (resources, a chest's contents, an item stand's item) goes into the same crates (`KwShipRiderBreak.Gather`).
- Pieces built on a launched ship (riders, 2026-10-26, the user's words: "build things like cannons and the weapon mounts
  on the ship after it is built, and not while it is being built on the keel guide with keel blocks. I really want the
  keel guide and keel blocks to create the entirety of the ship"): since the ship cannons came in the same day, only
  those (`KwShipRiderRules`; any other piece aimed at a launched ship is taken by the placement ray too, so its ghost
  stands on the deck refused, and placing it says why, `KwShipRiderGhost`, `KwShipRiderRefused`; pieces already riding
  stay and work as before). The game drops every placement hit on a collider with a
  body (`Player.PieceRayTest`), so nothing could be built aboard; for the local player a hit on a launched ship is taken
  (`KwShipRiderAim`), carried from the body's physics pose to its drawn one (the game's ships' bodies interpolate), and the
  piece the game then places in that frame is marked in its own ZDO (`KwShipRider.Place`): the ship's own id
  (`kw_rider`, the ship's `kw_ship_uid`, a random long the ship's owner writes once, since the game renumbers ZDOs each
  time the world loads) and its pose in the ship's frame (`kw_rider_pos`, `kw_rider_rot`), written by the placing game.
  The piece's owner may write a new pose at any time, as the game's synced transform works: it moves the piece itself
  (its local pose under the ship) and writes where it is; every other peer, seeing the ZDO's data revision change, takes
  the new pose as the record and eases the piece there from where it is drawn over 0.12 s (`KwShipRiderMove`; a ship cannon
  hauled about a deck writes its own ten times a second, `KwDeckHaul`). Every peer makes the piece the ship's child at that pose as soon as both are in its scene (`KwShipRiderWake`,
  `KwShipRiders.Take`): it follows the ship exactly, its colliders are part of the ship's body (players on it are carried;
  the body's mass, centre of mass and turning are put back as they were, so the hull's buoyancy never sees it), its own
  body, if it has one, is destroyed and its synced transform off, its support wear off. Its owner keeps its ZDO's
  position and rotation with the ship once a second (1 m or 5 degrees), from the piece, or, with no ship object in its
  scene, from the ship's ZDO found by its id in the nine sectors round it (`KwShipRiderFind`), so it loads and unloads with
  the ship and its ownership moves with the players near it. The game lets go of a ship's object in `ZNetScene.Destroy`,
  `OnZDODestroyed` and `RemoveObjects`; its riders are let go first (`KwShipRiderScene`), parked where they are, and come
  aboard again when the ship is loaded. A ship destroyed by its wear (sunk, wrecked) has each rider removed by its owner
  the game's way, drops where it was (`KwShipRiderBreak.Wreck`, from the ship's `m_onDestroyed` on its owner); a ship gone
  any other way (`kwship clear`, or no ship of its id near for a minute with the area loaded) leaves its riders as ordinary
  pieces, their owner clearing `kw_rider` and the game's support rules taking them back (`KwShipRider.Release`). The hover
  ray picks a body's own object for a collider of a body (`Player.FindHoverObject`), so on a launched ship it is cast once
  more and a rider's collider taken (`KwShipRiderHover`): chests open, cannons are manned at sea. Nothing new is sent; a
  dedicated server only keeps the ZDOs. Blueprints never carry riders: a ship built from one starts bare.
- Rig damage (2026-10-26, the user's words: "Holes through sails. If you hit wood, damage the mast that holds the sail,
  and the sail then droops ... If a cannon has a really lucky direct shot on the base of a post cap or the base of a mast
  ... the sail literally falls over"; and the rudder: "Very slight steering should still be allowed if basically the
  whole rudder is gone"): the marks (notches in spars, holes in sails, chips off caps, chunks off a rudder's blade, pieces
  fallen) are one text in the ship's ZDO, `kw_rig_damage` (`KwRigMarks`; never in a blueprint), written only by the
  owner: by its shot router takers (`mast`, `yard`, `sail`, `cap`, `bowsprit`, `rudder`, `KwRigDamageTakers`), by
  `KW_SailShot` (a hole a heavy projectile tore on the shooter's game, which flies on through the cloth: the sail's box
  is an `IHitProjectile` that says no, `KwSailDamagePierce`; the same hole from the router and the shooter counted once),
  `KW_RigMend(piece, part, type)` and `KW_RigRaise(piece)` from the hammer's repair (answered `KW_RigRaised` to the asker,
  who then pays). Every peer, a dedicated server too, polls the text (0.25 s), works out what it means
  (`KwRigCondition`: fastenings broken, droop, drive left, blade left), sizes its rig hit colliders and sets its
  steering; screens draw it. A fall seen arriving topples there (`KwMastFall`); one already in the ZDO when the ship
  loads is just gone. The sails' push is the owner's (`KwShipWind.Push` through `KwRigDamageDrive.Push`). The rig's hit
  colliders are on every peer under `ship/rig_hits`, on the "hitbox" layer, which collides with nothing (players walk
  through, the camera, building, removing and using rays pass them; the game's projectiles, melee and the router's trace
  meet them), tagged for the router (`KwShipHitRouter.Tag`), the body's mass, centre and inertia put back after each change.
- Ship cannons (2026-10-26, from the AssetLab Cannons bench; `Cannons/CANNONS.md`, "Multiplayer", has it in full): each
  cannon's state is in its own ZDO (`bs_arm_*`). E asks its owner, who hands the ZDO to the gunner's game (the
  MannedWeapons library), which aims, reloads and fires from then on; every other peer draws the aim, the reload, the
  recoil and the muzzle's effects from the ZDO. The ball is flown and its hits tested on the gunner's game, and each hit
  goes to the owner of what it struck (a launched ship's `KW_ShipShot`, as for any heavy shot). Shift+E asks the owner
  for the cannon to haul it (`BS_Arm_Tow`, the cart's way: the ZDO handed over unless it is manned or hauled); the
  hauler's game moves it about the deck in the ship's frame and writes its rider record's pose ten times a second, which
  every other peer eases to (`KwShipRiderMove`). The captain's broadside (U) is ordered of each cannon's owner through
  the library's ordered and called shots (`bs_arm_order`, `bs_arm_call`, `bs_arm_called`): one nobody mans is handed to
  the captain's game, fired there and reloaded from his pack in the same frame its ZDO is his; a manned one is fired on
  its gunner's game. An empty cannon another peer owns is asked for (`BS_Arm_Load`) before the captain's pack loads it.
  The rest lay is its owner's, a dedicated server too. The helm key lines, the shot arcs and the reload lines are the
  captain's screen only, read from the cannons' ZDOs; nothing of them is sent or saved.
- Logging off aboard (2026-10-27, `KwShipLogout`): each save of the local player aboard a launched ship keeps the
  ship's id and his place on it in his own save, per world; spawning there again he is put back on that spot once the
  ship is built. Local only: nothing is sent, his own sync carries him.
- Sea edits (2026-10-26, the user's words: "I might want to edit the railing to add another cannon, so I want to remove some
  of my railing so there's a gap for the cannon on the deck. Or I might want to add in a porthole window": railings, doors
  and windows, ladders, masts and sails, post caps and the bowsprit, with the dock's tools and windows; nothing else, and
  never hauled back onto a dock): the editing player's game checks the game's building rules (reach, ward, no-build place,
  the karve's workbench in range, a rider's counting) and the cost on a copy, then asks the ship's owner
  (`KW_SeaEdit(ZPackage edit, int token)`: the kind, the revision it was planned on, whole pieces in the ship's frame). The
  owner refuses a stale revision, a gone entry or a piece no sea kind owns, else changes the blueprint the ship was built
  from in place: a change replaces its entry, an addition is appended, a removal leaves a tombstone (`KW_sea_gone`, no data)
  so no index ever shifts and every mark keyed by index stays on its part; the changed or removed indices' own marks are
  dropped (new wood), each kind keeps its own numbering right (shield hooks, openings' leaves), and `kw_ship` and
  `kw_ship_rev` are written together; it answers (`KW_SeaEdited(token, ok, words, ZPackage cost)`) with the cost, the
  whole blueprint's after less before (`KwBpCost`), which the asker then pays or is given back once (none with the world's
  free build key). Every peer reads `kw_ship_rev` five times a second; when it moves, the ZDO's blueprint is read exactly as
  packed (`KwBpFile.Exact`: no piece left out, none turned round), copied entry by entry into the live one and each kind
  redraws only its own changed parts; then the ship's figures at once, its ladder over the side and its shadow boxes a frame
  later each. A peer loading the ship later builds the edited ship. Ghosts and hiding are this screen's only.

## Identity

| What | Names |
| --- | --- |
| Dock | `KW_dockguide` (keel block guide), `KW_keelcradle` (keel block) |
| Tools (hammer) | `KW_tool_keel`, `_rib`, `_planking`, `_deck`, `_rail`, `_ladder`, `_sail`, `_bowsprit`, `_cabin` (the Cabin tool: the castle tool renamed 2026-10-25), `_doorway`, `_window` (cut openings into planking and cabins; no part of their own), `_cap` (the Post cap tool) |
| Generated parts (never in the hammer) | `KW_gen_keel`, `_rib`, `_planking`, `_deck`, `_rail`, `_ladder`, `_sail`, `_bowsprit`, `_cabin`, `_cap` (a post cap, "cap|style|width|depth|length|lean|facing", lean in degrees, facing "out" or "in"; an older text without the last two reads as lean 0, out); `_castle` (castles built before the rename, kept so they load; they build as cabins) |
| Fittings | `KW_cap_scroll`, `_dragon`, `_tail`, `_finial`, `_plain` (out of the hammer since the Post cap tool, kept so built caps load; its edit rebuilds one as a `KW_gen_cap`; the old dragon wears the carved head); post cap styles `dragon`, `serpent`, `wolf`, `raven`, `horse` (carved heads, bundle prefabs `kw_head_<style>`), `scroll`, `tail`, `finial`, `plain`; `KW_hatch_small`, `_medium`, `_large`; `KW_rudder_<biome>` (meadows, blackforest, swamp, mountains, plains, mistlands, ashlands; only meadows and blackforest in the hammer, the other five registered so built and saved ones load); `KW_anchor` (anchor windlass); `KW_nameboard`; `KW_helm` (captain's wheel) |
| Blueprint table | `KW_bpstation` |
| Saved ships (hammer, client-side) | `KW_bp_<path>` (the file's path, "/" as "_"), named "Ship: <path>" |
| Launched ship | `KW_ship` (a copy of the game's `Karve`, never in the hammer); broken up into the game's `CargoCrate`s; `KW_sea_gone` (a removed part's tombstone in its blueprint, no prefab) |
| Ship cannons (hammer, Misc tab, after the dock pieces; only on a launched ship's deck) | `BS_cannon_bronze` ("Bronze ship cannon", Black Forest: 10 bronze, 20 fine wood; the small iron cannon's design in bronze on fine wood, firing stone shot) and `BS_cannon_small_iron` ("Small iron ship cannon", Swamp: 14 iron, 22 ancient bark; released 2026-10-28), the two in the hammer for now (`KwTiers.Offered`, the Swamp); held back for a later release, out of the hammer but registered so built ones load: `BS_cannon_small` ("Small ship cannon", Plains: 14 black metal, 8 tar, 24 wood, 4 linen thread), `BS_cannon_large` ("Large ship cannon", Ashlands: 18 flametal, 6 surtling cores, 26 blackwood, 6 linen thread); each a copy of `wood_floor` (a workbench in range), its cost refunded on removal; shots `BS_shot_cannon_bronze`, `_small_iron`, `_small`, `_large` and their bursts `BS_burst_<same>` |
| Cannon items | `BS_CannonballStone` ("Stone cannonball", the bronze cannon: 2 stone make 4 at the workbench; the game's `Stone` item's material), `BS_CannonballIron` ("Iron cannonball", the two small cannons: 1 iron makes 4 at the forge, level 2), `BS_CannonballFlametal` ("Flametal cannonball", the large cannon: 1 flametal and 2 grausten would make 4 at the black forge, level 2), all of ammo type `$bs_ammo_cannonball` (copies of `Catapult_ammo`); the flametal ball has no recipe for now (registered, so stacks load); `BS_BlackPowder` ("Black powder": 2 coal and 1 resin make 4 at the forge, level 1; one, two or three a shot); recipes `Recipe_<name>` in every ObjectDB |
| Items | `KW_RepairKit_Wood` ("Wooden Repair Kit", a copy of the game's `FineWood`; 4 `Wood` and 2 `Resin` at a workbench of level 1, stacks of 10, weight 2; recipe `Recipe_KW_RepairKit_Wood`); its look the bundle `kw_repairkit` when embedded (resources `Broadside.assets.kw_repairkit.windows` and `.linux`: prefabs `kw_repairkit_wood`, `kw_hullpatch_wood`, sprite `kw_repairkit_wood_icon`) |
| ZDO keys | `kw_span` (a part's saved text), `kw_tier` (biome tier id: `meadows`, `blackforest`, `swamp`, `mountains`, `plains`, `mistlands`, `ashlands`; new work only the first two), `kw_rudder_lay`, `kw_rudder_gear` (a rudder's steering gear to a captain's wheel, or why not), `kw_rudder_water` (the design waterline over a dock rudder's point, a float), `kw_helm` and `kw_helmsman` (a dock wheel's helm and who holds it), `kw_hatch_open`, `kw_sail_hoist`, `kw_ship` (on a piece: the build id of a ship built from a blueprint on the dock; on a `KW_ship`: its packed blueprint), `kw_hatch<n>_open` (a launched ship's hatch n), `kw_open<id>` (a dock opening's leaf, an int on its part); on a `KW_ship` also `kw_ship_open<n>` (a covered opening's leaf: 0 shut, 1 or -1 open), `kw_anchor_down`, `kw_anchor_at`, `kw_ship_load`, `kw_ship_by`, `kw_ship_helm`, `kw_ship_name`, `kw_holes`, `kw_hull_wear`, `kw_flood`, `kw_flood_parts` (its holes, its parts' wear, the water in its hold and in each of its compartments; never in a blueprint), `kw_rails_broken` (its broken railing stretches, "piece:part;..." with part = run × 1000 + stretch; never in a blueprint), `kw_hull_patches` (its patched holes, "id,radius;...", the hole's number and its radius when patched; never in a blueprint), `kw_rig_damage` (its rig's damage marks, "type,piece,part,a,b,c,d,seed;...", `KwRigMark`; never in a blueprint); `kw_name` (a name board's, and a `KW_ship`'s name), `kw_shields` (a railing's or a `KW_ship`'s hung shields; in a blueprint as data only); `kw_ship_uid` (a `KW_ship`'s own id, a random long its owner writes once); `kw_ship_rev` (a `KW_ship`'s sea edits' revision, written with `kw_ship`); on any piece built on a launched ship `kw_rider` (its ship's `kw_ship_uid`; 0 once it is an ordinary piece again), `kw_rider_pos`, `kw_rider_rot` (its pose in the ship's frame); on a ship cannon `bs_arm_user`, `bs_arm_yaw`, `bs_arm_pitch`, `bs_arm_loaded`, `bs_arm_step`, `bs_arm_load_at`, `bs_arm_shots`, `bs_arm_shot_at` (the MannedWeapons library's, from the prefix `bs_arm`), `bs_arm_kind`, `bs_arm_load_kind`, `bs_arm_ammo`, `bs_arm_tower` (who hauls it), `bs_arm_untied` (its breeching left unmade) |
| RPCs | `KW_HatchToggle`, `KW_SailHoist`, `KW_Helm`, `KW_HelmTurn`, `KW_HelmReply` (a dock wheel's helm), `KW_Hatch<n>_Toggle`, `KW_OpeningUse` (a dock opening's leaf), `KW_ShipOpeningUse` (a launched ship's), `KW_AnchorToggle`, `KW_ShipLoad`, `KW_Bail`, `KW_HoleMend` (one hole, by its number), `KW_HolePatch`, `KW_HolePatched` (a repair kit's patch, asked and answered), `KW_ShipShot` (a heavy shot, to the owner), `KW_RailMend`, `KW_SailShot`, `KW_RigMend`, `KW_RigRaise`, `KW_RigRaised` (the rig's damage), `KW_ShipName`, `KW_ShieldHang`, `KW_ShieldHung`, `KW_ShieldUndo`, `KW_ShieldTake`, `KW_ShieldGiven` (on a `KW_ship`), `KW_ShieldOwn` (a dock railing), `KW_ShipWhere`, `KW_ShipsAre`, `KW_ShipLaunched` (routed, the ship pins), `KW_SeaEdit`, `KW_SeaEdited` (a sea edit asked of a `KW_ship`'s owner and answered), `KW_Bench*` (BlueprintShelf: `List`, `Listing`, `Put`, ...); on a ship cannon `bs_arm_request`, `bs_arm_release`, `bs_arm_answer`, `bs_arm_call`, `bs_arm_called`, `bs_arm_order` (the MannedWeapons library's, named from its key prefix), `BS_Arm_Tow`, `BS_Arm_TowDenied`, `BS_Arm_Load` |
| UI objects (the captain's screen) | `Broadside_HelmKeys` (the helm key list) with the cannons' `Broadside_FireBroadside` and `Broadside_ShotArcs` lines; `Broadside_CannonsTicker` (the broadside's keys and ticks); `Broadside_ShotArcLines`; the reload lines: rows `cannons.port` and `cannons.starboard` of Broadside's own ship panel (`KwShipPanel`) |
| Folders | `BepInEx/config/Broadside.Ships/` (the player's ships, `.json`, folders kept); the server's `BepInEx/config/Broadside.SharedShips/<world>/`; `BepInEx/config/Broadside.Sails/` (the player's PNG pictures for sail emblems); `BepInEx/cache/Broadside.Icons/` (rendered hammer and item icons, `KwIconCache`; the carried ones are `Broadside.icons.<name>@<hash>.icon`) |
| Player save | `broadside_aboard_<worldUID>` in the player's custom data: the launched ship he was saved aboard and his place on it (`KwShipLogout`) |
| Console | `kwship clear` (every `KW_` piece within 40 m, launched ships too); a cheat |
| Harmony ids | `milkyteam.broadside`, `.shelf` (BlueprintShelf's patches), `.form` (WindowInput's), `.camera` (BuildCamera's) |

## Code map

All in `Broadside/Broadside/`, one namespace. `Broadside.cs` is the plugin (patch classes applied one by one, then
`KwPrefabs.Install`, `Guard.Install` last); `Log.cs` the logger.

- `Pieces/`: making the prefabs and putting them in the hammer. `KwPrefabs` (copies of `wood_floor` with a generated
  mesh, the longship's hull material and the floor's wood, free; and the launched ship's prefab), `KwCatalogue` (every
  `KwDef`: dock, tools and parts, fittings), `KwFeatures` (caps, rudders, hatches, windlass, table), `KwHammer` (Misc tab, build
  order, the saved ships last), `KwSnaps`/`KwSnapRules` (snap points and which may meet), `KwIcons` (rendered hammer
  icons), `KwPaintedIcons` (painted tool and piece icons, `assets/kw_*.png`), `KwIcons` with `KwIconRig`, `KwIconSubject`, `KwIconLight`, `KwIconPicture` (rendered icons, isolated light), `KwTextures` (embedded textures; one over 256 px compressed on the
  graphics card and let go from memory, `Settle`), `KwRelief` (a normal map from an embedded relief, the workshop's
  way), `KwBuildView` (zoom out, pan, turn while
  building).
- `Dock/`: `KwDock` (the keel block guide and the keel block), `KwSupport` (keel blocks fitting the keel above
  them), `KwSolid` (timber solids), `KwDockCommand` (`kwship clear`), `KwDockWaterline` + `KwWaterlineLevel` +
  `KwWaterlineShape` (the waterline drawn on the dock's ship while designing or at the open table: where the launched
  ship will float, its trim included, worked out as `KwShipHydro` floats it; local, screens only; its weight and the
  weight's centre from `KwShipWeight`, as the launched ship's and the table's figures).
- `Tools/`: `KwTools` (the click tools: planking, deck, railing; spawning parts), `KwToolPieces` (the seven tools and
  their parts), `KwShapeTool` + `KwShapeSites` + `KwKeelPick` (keel and rib tools), `KwKeel`/`KwKeelSpec`/`KwKeelBody`
  (keels, their curve and thickness), `KwRibs`/`KwRibRow` (ribs, even rows), `KwSpans` + `KwSpanKeels` +
  `KwSpanEnds` + `KwStemEnd` (what a part between two clicks spans), `KwKeelRun` + `KwRunPieces` (keel end to keel
  end), `KwSpanForm` (the planking and deck window; `KwBandField`, its painted bands), `KwPreview` (see-through previews). Undo on the dock: `KwUndo`
  (the local player's last 30 actions this session, Ctrl + Z and Ctrl + Y; recorded in `KwTools.Spawn`, the parts
  spawned in one frame one action, and for ship pieces from the hammer), `KwUndoStep` (an action and its pieces' ZDO
  ids), `KwUndoWork` (removing the hammer's way, building again from the saved ZDO values), `KwUndoKeys` (the keys,
  and Z kept from the game's debug fly), `KwUndoHooks` (`KwUndoPlaced`, `KwUndoHammer`). Editing parts already built
  (2026-10-25: a tool's click on a part it makes, Shift + click every one of its kind on the dock; offered first in
  `KwTools.Click`, its preview first in `KwPreview`; before it the tool switch, the user's request 2026-10-25: a click on a
  part the tool in hand does not take selects that part's tool in the hammer, `Player.SetSelectedPiece` as the game's copy
  pick does, and opens its edit, `KwEdit.Open`, whatever that tool keeps its own way near it; a doorway or window on the
  part aimed at is the Doorway or Window tool's even where the held tool would edit the wall; fittings, the dock and a
  waiting first click left alone: `KwToolSwitch` + `KwSwitchTarget`, `KwSwitchRule` (the part's tool, the choice, the
  hint; plain data), `KwToolActs` (each tool's own aim and site logic asked without acting), `KwToolSwitchHint` (the
  hover's "Click: change this mast (switches to the Sail tool)" once on the hint line; every tool's last panel line
  `KwToolText.OtherParts`)): `KwEdit` (which clicks, the dock's parts, wards), `KwEditKind` +
  `KwEditKinds` (a kind of part: its values from its text and tier and back, refused past its tool's limits, its window,
  the note on what does not follow, `Over`: the part a click on something else stands for; a new tool's edit is one more
  kind in `KwEditKinds.All`), `KwPartValues` (values by the window's keys, the keys changed, the merge), `KwEditSession`
  (the open window: each part planned from its own values with only the changed keys, the note line, the preview of them
  all, a refused part shown as last planned so none vanishes; `KwEditKind.HoldsRefused`, the cabin's, keeps OK waiting while
  every part changed is refused, through `KwForm.Open`'s accept), `KwEditHide` (the user's request, 2026-10-25: while the window is open the parts are hidden on this screen, every
  renderer under them by `forceRenderingOff`, so only the new version shows; shown again on Cancel and every other end, the
  tool put away, no edit open, a throw; on OK the replaced parts stay hidden until gone, at most 3 s; ticked by
  `KwEditHideTicker`), `KwEditPreview` (the planned parts in their real looks, solid, gathered by `KwBpMeshSet` at their own
  places: bands, hold floors, openings, a cabin's three looks; the sail and bowsprit tools draw their own), `KwEditWork`
  (removed and spawned again in place through `KwTools.Spawn`, one undo step whose `KwUndoStep.Replaced` keeps the old
  parts; undo and redo swap them, `KwUndoWork.Swap`; the parts taken away told back for the hiding); the kinds `KwKeelEdit` (a click within 0.75 m along of a "Keel joint" is `KwKeelPick`'s),
  `KwRibEdit`, `KwPlankingEdit` (material, bands), `KwDeckEdit` (height, material; near its ends the tool's click),
  `KwLadderEdit` (length), and in `Sails/` `KwMastEdit` and `KwBowspritEdit` (the sail and bowsprit windows themselves,
  `KwSailForm.OpenOn`, `KwBowspritForm.OpenOn`, through `KwSailTool.Edit` and `KwBowspritTool.Edit`), `KwRailEdit` (post
  spacing, material, a hull railing's sides as port and starboard, read with the part's place, `KwEditKind.Read(piece, ...)`;
  a roof railing's edges and its ladder gaps; `KwEditKind.Read(saved, tier, tiered)` leaves the tier out
  for a railing built before railings had one; `Over`, the user's request 2026-10-25: a click on a roof railed already, or
  on the planking, a rib or a keel end under a hull railing's stretch on a side it has, `KwRailOver`, edits that railing;
  a rib or keel end at a railing's end with no railing past it still starts a new one). Railings (2026-10-25): the railing tool's window is `KwSpanForm`'s with
  `KwRailField` (post spacing, Sides: both, port or starboard, the roof's edges); `KwRailBow` (port and starboard to a part's
  own left and right by the way it runs to the bow, `KwCabinBow`'s bow, else the player's look); a click on a cabin is
  `KwRoofRailTool`'s (window, preview, build on the roof with gaps at the ladders there). The Cabin tool (2026-10-25: the castle tool renamed and reworked, the deckhouse
  cabin tool removed): `KwCabinTool` (two clicks on deck sections, or a cabin's roof twice; the hover preview, the window,
  the build), `KwCabinPick` (a deck part or a cabin roof's top under the crosshair), `KwCabinHull` (the decks and planking
  along the first section's keel line, read once; the range between two sections on one level deck without a gap, the
  hull's sections over it in the cabin's frame, its ends at the hull's end or open; `Over`, a cabin on a cabin's roof),
  `KwCabinRange` (`KwCabinSizes`, `KwDeckRun`, `KwCabinSite`, `KwCabinRange`), `KwCabinBow` (the bow from a rudder or the
  masts on the line, the cabin's front; else the way the player faces), `KwCabinForm` (the window's rows: height, front,
  back, left and right insets, material; its note), `KwCabinRails` (the hull railing a side at the hull would stand in:
  each part taken the hammer's way and built again on its other side only, cut at the ribs round the cabin's ends when it
  reaches past them, `KwRailCut`, all in the same undo step, `KwUndo.Taken`), `KwCabinRiders` (what stands on a cabin goes
  with it, `KwCabinLift.Removal`: in `KwUndoWork`'s undo and redo, kept in the step, nothing for a cabin swapped for another
  version of itself; `KwCabinRemoval`, with the hammer, the cabin and all it took one undo step, `KwUndo.Removed`,
  `KwUndoStep.Removal`), `KwCabinEdit` (the edit kind: a click on a cabin's
  wall, not its roof's top; `KwEditKind.Edits` takes old castles too, `KwEditKind.Rebuild` takes the railing after the
  parts are replaced), `KwHouseTools` (the dispatch from `KwTools` and `KwPreview`). Stacked cabins (the user's report,
  2026-10-25: a cabin raised grew into the cabin on its roof): `KwCabinFollow` (`KwEditKind.Follow`, the cabin edit's:
  the stack read, carried and given back as plans, an edited cabin on a changed one planned again in place, every other
  piece a `KwEditPlan.Follows` plan with its `Shift` or `Take`, built by `KwEditWork` in the same frame, shown by the
  preview, hidden by `KwEditHide.Follow`; `KwEditFollow` holds them; `KwStackWords` the note and message),
  `KwCabinLift` (bottom up: a cabin on a changed cabin set on its new roof, gone with nothing buildable there;
  `KwStackMove`; `Removal`), `KwCabinRebase` (an upper cabin on its lower one's new roof, `KwCabinHull.Over`: each wall
  kept where it stood, one on the lower's wall kept on it, a typed inset kept, drawn in onto the new edge, fitted;
  openings' sills raised, their places kept), `KwStackRiders` (roof railings moved or laid again on the new outline,
  pieces on a roof raised or taken off it, ladders keeping their foot and lengthened to the new top or taken),
  `KwCabinStack` (what stands on a cabin: `Stands`, `Railing`, `Standing`, a ladder's `TopOn`/`FootOn`; never the hull's
  parts or the posts' fittings), `KwStackPart` (a piece as read, `Near`), `KwStackFrame` + `KwCabinAt` (a level frame; a
  cabin where it stands, `OnRoof`, `FromEdge`, `Edges`). All pure numbers but `Near`.
- `Hull/`: the meshes. `KwMesh` (`KwDef`, `MeshBuilder`, decks and skins), `KwShapes`/`KwJoints` (stations, the joint
  table), `KwPlanks` (clinker boards, the transom board in level rows), `KwStrakes` + `KwGirth` + `KwClip` (strakes one
  texture row wide, cut from the stations' measure: level on the hull's sides, one every 0.5 m over the level keel line,
  `KwDef.Level` from `KwSpan.Level`; round the bottom by girth, blended at the turn of the bilge; numbered from the keel
  line, the bottom's below 0), `KwDeckBoards`/`KwDeckLayout`/`KwDeckHoles` (boards, hatch openings),
  `KwRailing` (a hull railing; posts at its stations, or evenly by `KwRailText`, its span's last segment "posts=0.9",
  `KwSpan.Rail`, with "sides=l" or "sides=r" for one side only, the part's own left or right; none: both),
  `KwRailCut` (a hull railing cut where a cabin stands: the sides left over its length, whole past the ribs round it), `KwRailLooks` (every railing wears the plank wood its planar UVs are made for; its tier is its cost only), `KwRailOver` (the hull railing a railing tool's click on the hull under it stands for), roof railings
  (`KwRoofRail`, its own `KW_gen_rail` text "roofrail|posts|sides|edges|gaps", never a span; `KwRoofRailShape` + `KwRoofRun`
  its mesh, rails cut and posts either side at each gap; `KwRoofSite` + `KwRoofFrame`, a cabin's roof from
  `KwCabinRoofs`), `KwFrame`, `KwRound`, `KwSpan` (a part's saved text), `KwGenerated` (a part in the world),
  `KwKeelLines` + `KwSeat` (a part seated on its ship's keels each time it builds, never saved: the dock's loaded keels or a
  blueprint's; ribs' heights count from the level keel line, so a rib a little up a keel end meets every part at one
  height, and a part between ribs follows the keel under it; `KwSeatAt` where a part stands),
  `KwSole`/`KwSoleDock` (hold floors). Cabins: `KwCabinSpec` (its saved text "cabin|c2|floor|top|ref|back|front|left|
  right|seed|sections": the hull's sections under its deck sections in its own frame, so it builds without the hull round
  it; `KwCabinEnd`, open or at the hull's pointed or flat end, and its inset; `KwCabinSide`, the edge it stands on and its
  inset; an old castle's "castle|c1|" text reads as a cabin; never a `KwSpan`, so the hull's measures, floors, slices and
  water mask leave it out), `KwCabinOutline` (the hull anywhere along it carried up to the roof, each side's reach, how
  wide it is across, which ends the hull closes, the roof's outline), `KwCabinFit` (the user's report, 2026-10-25: every
  spec read or made is the cabin asked fitted to its hull: height 1 to 3.5 m, insets 0 or more and leaving 0.5 m of deck,
  an end with a bulkhead moved on in to where the cabin is 0.3 m across, the sides limited, larger first, only where no
  place fits; what changed in `Fitted` for the note; the text keeps what was asked, `Asked`; a cabin nothing fits,
  `Unbuilt`, is a plain frame and an unreadable text wears the tool's mark, so no cabin part is ever invisible), `KwCabinWall` (one wall from its outline: its line across the plan
  and its outline in (along, up), cut in strake rows and straight segments for the look and a ship's boxes, so openings
  can be cut from the outline), `KwCabinWalls` (a bulkhead at each end the hull does not close, a wall along each
  drawn-in side), `KwCabinWallMesh` (both faces planked in level rows, the rim), `KwCabinRoof` (a raised deck over its
  footprint in its tier's boards, `KwDeckBoards.Lay` over pieces), `KwCabinPosts` (0.24 m corner posts where a bulkhead
  meets a side, centred on the bulkhead, from the deck to the roof's top; against the hull flush on the planking's inside,
  with an ear over a raised side's end out past the hull's outside; a cabin on a roof stands its posts on that roof's
  top, the same section, so stacked posts read as one; the plank wood, drawn only) + `KwCabinPostMesh` (a post's solid
  from its rings, the grain up it by height, caps inside its ends), `KwCabinShape` (the hull's sides raised to the roof on each side at the hull,
  a flat stern closed, a pointed end the hull closes boarded across over its post from the deck to the roof, the walls,
  roof and posts), `KwCabinRoofs` (a piece's cabin, a point on its roof's top, the roof's
  edges in world space for the railing and ladder tools). `KwHouses` (built in the world from the ZDO, one submesh a look;
  `KwHouseMeshes`), `KwHouseLayers` (hull, deck and plank-wood looks and what is walked on), `KwHouseCost` (by area in the
  tier's hull and deck materials, from `KwBiomeCost.Of`, and its posts).
- `Looks/`: `KwBiomeTiers` (the seven tiers and each part's two materials; `Offered`, the two new work may pick), `KwBiomeCost` (a tiered part's or a railing's
  cost by its size), `KwBiomeLooks` (their materials, each dressed in its textures when first worn, never on a dedicated
  server), `KwBiomeField` (the "Material" choice: the offered tiers, and an edited part's own after them; read back by
  name), `KwBiomeDress` (a part wearing its
  tier), `KwTierMaterials` (the game materials rudders wear), `KwUv`/`KwKeelUv`/`KwPlankShift` (texture mapping: the
  planking's long texture, sixteen strakes outside and sixteen inside, 64 m along; `KwPlankShift` lays each part's
  planks along its ship from its start rib's place on the ship's axis, the part's heading folded to one half-turn and
  rounded to 2.25 degrees, run backwards for a part laid the other way, and starts each strake at its own place in its
  row, `KwUv.RowStart`, from the strake's number and its side on the ship, carried per vertex in the mesh's third UV
  channel, `MeshBuilder.Row`, and let go after; `KwGirth` brings a part's strakes out at its rib ends where the
  neighbour's start, `KwDef.RibStart`, `RibEnd`).
  Painted bands: `KwPaint` (the palette, the span text's paint segment), `KwBandMesh` (thin skins 6 mm off the painted
  strakes, cut as `KwStrakes` cuts them), `KwPaintLooks` (the tier's plank texture painted over), `KwBandsShown` (on the
  dock), `KwPartLooks` (from `KwGenerated.Build`: bands, and a railing's shield hooks); the ship's in `KwBpMeshSet`.
- `Fittings/`: post caps (`KwCaps` the nine styles in the wheel's order and the old hammer pieces). The carved heads
  (2026-10-25, the user's "create a few different options for post heads and remake the dragon more like the
  longship's, not as blocky"; the code's blocky dragon removed): `KwCapHeads` (the `kw_postheads` bundle read once on a
  screen: each head's mesh, head_start, the shared material; a piece's `model` wearing one, the old hammer dragon and the
  tool's placeholder), `KwCapHead` (one head as plain data, centred on its foot; its neck's middle line, measured from
  the corners at the neck's sides band by band), `KwCapWarp` + `KwCapJet` (fitted to the post: the neck stretched and
  bent by `KwCapBend`, the head above moved whole, the foot's underside scaled flush to the post's section, the rest by
  the cap's size, turned half round to face inboard; normals by the Jacobian's cofactors, tangents by the Jacobian),
  `KwCapMesh` (the cap's mesh anywhere: a head's warped corners kept by the cap's text, else the code's),
  `KwHeadLooks` (the plank wood's piece material in the bundle's atlas and normal map, tinted per keel tier as the doors
  are: Meadows light, Ashlands nearly black; a separate material group in `KwBpMeshSet`). `KwCapBend` (a neck's lean
  and stretch as a warp by its height as carved: the middle line stretched and turned by an angle easing in and out
  over a fifth at each end, `Share`, so no crease at the foot or the head; each point keeps its offset from the middle
  line, so the neck bends about its own middle; above the top everything moves whole, `Whole`). The code's caps:
  `KwCapCurls`, `KwCapSmall`, `KwCapSweep` their shapes, drawn for the standard post through `KwCapPen` + `KwCapForm`:
  the foot to the post's section, the carving scaled evenly by the base's size, a stem (scroll, tail) made longer and
  its line bent by the lean (`KwCapPen.Bend`) with its curl moved whole, the finial and plain cap stretched, all turned
  half round to face inboard; `KwTierCap` the look (`Look`: a head's tinted look, else the keel tier's) and the hover;
  the Post cap tool, 2026-10-25: `KwCapTool` the click and the post top aimed at, the wheel (style; Shift: length;
  Ctrl: lean, 5° a notch), `KwCapGhost` the cap shown solid in place, `KwCapSpec` the saved text and cost, `KwCapEdit`
  the edit kind (style, length, size, lean, facing), old hammer caps rebuilt as tool caps; the wheel
  through `Tools/KwToolNotches`: a stepping tool turns, so the game counts the notches into its placement rotation,
  taken back off before the ghost is placed), hatches
  (`KwHatch`, `KwHatchCover`, `KwHatchPlace`), ladders (`KwLadder`, `KwLadderClimb`; `KwLadderTool`, one click:
  `KwLadderAim` what is aimed at, `KwLadderSpots` the five spots an edge of a hatch's opening and the 0.25 m snap along an
  open edge, `KwLadderEdges` a deck's ends no other deck carries on and a cabin roof's sides, each a `KwLadderEdge` (a
  run of stretches, a side along the hull's line joined whole, its ends named hull side or corner), `KwLadderDrop` its
  length and what stands round an open edge, `KwLadderGaps` a roof railing's gap at its top, built with it in one undo
  step, the ragged step in `KwUndoWork.Swap`; centring on an open edge (2026-10-25): `KwLadderNear` the edge's bounds,
  worked out once an edge, from `KwLadderBelow` (looked under every 0.1 m: a solid in the way by name, no floor) and
  `KwLadderFinds` (doorways and windows with their frames in a wall right under the edge and along it, from
  `KwOpeningSurfaces`; cabins' corner posts; ladders on the edge), `KwLadderBound`, and `KwLadderSpans` (free spans a
  ladder fits with 5 cm each side; a span's middle snaps within 0.4 m, a span of 2.4 m or more its quarter points within
  0.2 m, else the grid; refused in front of a doorway, another ladder or a solid; "In front of a window" allowed). The
  hover says where it snapped, or why not, in the middle of the screen (kept out of the log) when that changes; the click's
  message says it too. Nothing new is saved: placement only; on a launched ship, 2026-10-26, the same click from the
  ship's blueprint, `Sea/KwSeaLadder*`: `KwSeaLadders` the sea kind (owns `KW_gen_ladder`, draws each ladder alone, the
  owner's check), `KwSeaLadderTool` (hover ghost, click, the length window with Remove, the hammer's pick, the edit with a
  roof railing's gap), `KwSeaLadderPlanner` (the aim: a hatch crossed or hit, a cabin, a deck's top, a ladder's box),
  `KwSeaLadderShip` (the ship as plain maths: decks, cabins, hatches, ladders, roof railings, cut planking, what is walked
  on, the hull's sides, fittings as `KwAnchorFixtures` solids by piece), `KwSeaLadderEdges` (deck ends and roof sides as
  `KwLadderEdge`s, `Part` their blueprint index), `KwSeaLadderNear` (the bounds, `KwLadderBelow.Runs`), `KwSeaLadderDrop`
  (`KwSeaLadderFoot`: the way down to a floor or a solid, the open edge's checks), `KwSeaLadderSolids`, `KwSeaLadderLook`
  (each ladder's climb box with its look as its child under `ship/fittings`, `KwLadderClimb.Piece`; made at build by
  `KwShipFittings.Climb`, again by the redraw)), the anchor
  windlass (`KwAnchor`, `KwAnchorShapes` (a flanged drum), `KwAnchorCoil` (the cable wound on it as rope: uneven turns,
  riding turns, one crossing over, about 2,600 triangles), `KwAnchorPlace`: on decks only; its lead overboard,
  2026-10-25: `KwAnchorPlan` (the clear place with the least cost: cable, 0.6 m a deck-lead turn, 0.3 per metre to
  the side of the drum, so straight out from the drum wins), `KwAnchorShip` (the ship round the windlass in its frame from blueprint
  pieces: `KwAnchorHull` planking girth in and out, `KwAnchorRail`, `KwAnchorDeck` + `KwAnchorCabin`, `KwAnchorFixtures`
  and `KwAnchorSolids`), `KwAnchorSpots` + `KwAnchorSpot` (chocks along the sheer, railing caps and transoms, snug
  beside each post; hawse pipes), `KwAnchorRoute` (the cable on deck off the drum within 45 degrees of square to it,
  else a deck lead), `KwAnchorHang` (the weighed anchor plumb, leaning on the hull or on an arm; the cable let go clear
  of the hull down to the keel), `KwAnchorLead` (the result), `KwAnchorLeadShapes` + `KwAnchorLook` (drawn), `KwAnchorDock`
  (on the dock, its hover)), the name board (`KwNameBoard`, its mesh and
  frame; `KwNameText`, the game sign's TextMeshPro copied onto both faces; `KwNameBoardPiece`, E on the dock), shields
  on the railing (`KwShieldSlots`, a hook every 1.2 m a side; `KwShieldRecord`, the hung items as ZDO text;
  `KwShieldShow`, the item's model as the wall item stand shows it; `KwShieldRack` + `KwShieldHook`, the hooks;
  `KwRailShields`, a dock railing's, the item stand's ownership way); the captain's wheel (`KwWheel`: the piece, its
  turning "wheel" child, where the helmsman stands, its turn at full helm the steering gear's ratio times the rudder's;
  `KwWheelShapes`: the stand with its closed pedestal, where the gear's short shaft comes up out of sight, and the
  wheel; `KwWheelHelm`: its helm on the dock, the prototype's, E, A and D, the hover; on a deck or a cabin roof's top
  (`KwCabinRoofs.OnRoof`) only, by `KwAnchorPlace`).
- `Rudders/`: `KwRudder` (the seven, built to their post), `KwRudderTiers` (the seven in order; `Offered`, the two in
  the hammer, and `Held`, a registered rudder kept out of it), `KwRudderTier*` (each design), `KwRudderFit`/`KwRudderDrop`/
  `KwRudderSpan` (sizing: the blade from just over the keel bottom up to the ship's design waterline, 96% of its area
  under it, the stock alone above; `KwRudderWater` that waterline over the rudder point, from the blueprint on a launched
  ship, measured on the dock by the rudder's owner and kept in `kw_rudder_water`), `KwRudderLay` + `KwRudderLayFind` + `KwRudderLayTests` + `KwRudderSweep` (how it links to
  the deck), `KwRudderLink` + `KwRudderLinkShapes` + `KwRudderLinkage` (the deck tiller and links, and where the
  helmsman's hand holds them: `HandAt`, `BarAt`, `Turn`), `KwRudderKit`, `KwRudderShapes`, `KwRudderWindows`,
  `KwRudderPlace` (onto a post), `KwRudderBlade` (a rudder in the world; on the dock it follows the wheel it is linked
  to; its owner measures again when the pieces round it change). The steering gear to a captain's wheel (2026-10-25,
  a shaft and gears in place of the first tiller-and-ropes gear): `KwGearTrain` (sizes and how every part turns: a
  toothed quadrant on the stock facing aft over its head, outside the hull, a pinion rolling on its face 0.4 m aft
  and to port of the stock, so the line shaft passes the stock and goes forward through a bore in the post, 0.24 m
  under the deck; bevel pairs one to one; pinion to quadrant 8 to 1, so the wheel turns 280° at full helm; every shaft
  turns 8 times the rudder's angle about its way from the wheel), `KwRudderGear` (its saved text "shaft|wheel|corners|
  hangers", "|brackets" after it when it climbs to a roof, or why not, `KwGearWhy`; old "gear|" rope texts read as
  none; its cost by its whole length, climbs too), `KwGearShip` (the ship's pieces
  in the rudder's kit frame, plain maths only: `KwGearPose`, `KwGearDeck` the decks' tops and hatch openings,
  `KwGearHull` the hull's inside, masts and ladders as columns, keels, posts and cabins as solids, `KwGearSolids`; the
  wheels), `KwRudderGearFind` (the wheel, its deck, the stern's deck over the bore, the `KwRudderHelm.Gear` lay; the
  blade's top cut under the quadrant, refused under 60% of it), `KwRudderGearRoute` + `KwGearLine` (one straight run
  when the wheel is within 5° of the bore's line, the checks; the hangers), `KwGearGrid` + `KwGearSearch` (else the
  fewest bends: Dijkstra over places along and lanes across, a bend's cost, boxes where they fit), `KwRudderGearDock`
  (the dock's pieces read for it, their signature), `KwGearParts` (the shafts as turning parts, the hangers and bush
  as fixed ones), `KwGearShapes` (the quadrant, the pinion, bevel wheels, couplings; tooth places as functions for
  offline checks), `KwGearFittings` (the shaft, hangers, the post's bearings, the bush under the wheel's deck),
  `KwRudderGearLook` (drawn on the rudder, the shafts turned each frame the angle changes; the wheel's hover words).
  The quadrant is drawn on the blade by `KwRudderLinkShapes.Yoked`, the post's bearings by `KwRudderLinkShapes.Deck`.
  A wheel on a cabin's roof (the user's request, 2026-10-25): `KwGearClimb` (under the deck the lowest cabin stands on
  to a climb up its back wall, under its roof to the wheel, a cabin on a cabin climbed in turn, one straight climb
  where their backs meet; the best climbs tried first, refused only when none fits: `KwGearWhy.Climb`, `.Ceiling`,
  `.Route`), `KwGearRooms` (the cabins under the wheel, lowest first; what a climbing shaft must pass: rooms, openings'
  rooms, masts, ladders, windlasses; the first climb's foot box inside the hull), `KwGearRoom` (a cabin as a room in the
  rudder's frame: inside its walls, raised sides and hull less its posts, its roof over a place, its after end),
  `KwGearKeepOut` (a doorway's or window's room: across it and its frame, in as far as a door swings), `KwGearRisers` +
  `KwGearRiser` (the climbs best first: the back wall hugged in every clear lane, the shaft's own lane first, straight on
  up from the cabin below, else a side wall near the back; the brackets, each squared onto the nearest face, a pointed
  stern's slanting planking too), `KwGearCeiling` (`IKwGearWay` under a roof: an L forward first, else `KwGearGrid`,
  which takes any `IKwGearWay`, `KwGearWay`), `KwGearBracket` (a bracket in the gear's text, "x,y,z,dx,dy", its fifth
  part, only when it climbs).
- `Sails/`: `KwSailTool` + `KwSailForm` + `KwSailDraft` (the sail tool and window), `KwSailMast`/`KwSailSpec`/
  `KwSailFit`/`KwSailLayout` (a mast's sails), `KwSailShapes`/`KwSailCanvas`/`KwSailDrape`/`KwSailDress` (meshes),
  `KwSailRigging` (ropes drawn) + `KwRigRopes` (which ropes, where; `KwRigPlan` keeps each rope's tie by name, its feet
  at least 0.25 m apart), where they tie on (2026-10-25, the user's "ropes go straight through the deck floor"):
  `KwRigShip` (the ship round a rigged piece, in its kit, from blueprint pieces: the anchor's `KwAnchorShip` hull, rails
  and decks, `KwRigCabin`/`KwRigRoofRail`/`KwRigSails` solids, the cabin it stands on), `KwRigEdges` + `KwRigEdge` (tie
  places every 0.1 m along the sheer and each roof's outline, railed or not, none under a cabin or a higher planking
  part), `KwRigTies` (the rules: its side's sheer at the aim's place on a deck; its roof's edge on a roof, the hull past
  it when the roof's edge is out less than a quarter of the rope's height; the nearest clear and free place when
  blocked; a rope down the middle to the deck under it) + `KwRigClear` (sampled every 0.1 m with its sag against the
  solids), `KwRigTie` (the fitting's place), `KwTieShapes` + `KwTieLook` (deadeyes on a chainplate, cleats, eyebolts;
  drawn only, no colliders), `KwRigSource` + `KwRigState` (the pieces: a launched ship's `KwShipBody.Blueprint`, the
  dock's; on the dock re-tied when a piece round the mast comes or goes), `KwRigGhost` (ropes and fittings as meshes in
  a saved ship's ghost and the sail tool's preview), `KwSailAnchor.On` (any other rope, the jibs' sheets, by the same
  rules), `KwSailCloth` (the three cloths and the items they cost; `Offered`, raw hide, the one new work may pick, and
  `Default`, the cloth a new sail or jib starts in), `KwSailHide` (the raw hide cloth: the game's hide
  from the raft's sail, `sail_hide` and its normal map, copied at runtime and never shipped: its middle inside the
  outline and lacing holes, opaque, drawn out to 256 texels; tinted linen on a dedicated server or without the raft),
  `KwSailWear` + `KwWearTears` + `KwWearSewn` (raw hide worn, painted per sail shape and proportions by `KwSailLooks`
  over any pattern: holes with frayed rims, patches and a sewn tear, stains, the free edges torn (a square sail's sides
  and foot, any other sail's leech), laced edges and corners whole; fixed seed; a worn sail has no bolt rope,
  `KwSailRigging.Bolt`), `KwSailRig` (a mast in the world, hoist),
  `KwSailMotion` + `KwSailWind` + `KwSailNow` + `KwSailPoints` + `KwSailWeld` (the cloth in the wind), `KwSailClear` (a
  square sail's cloth and bolt rope kept off its mast after the motion, each frame), `KwSailSwing`
  (the cloth swinging after a launched ship). Sails by kind (2026-10-26): `KwSailPolar` (`KwSailKind`: square, gaff,
  triangle, jib; each kind's push ahead and to leeward by the apparent wind's angle off the bow, a table per kind: square
  0.70 dead astern, the game's own push there, nothing closer than 70°; gaff from 35°, triangle and jib from 30°, best
  on a reach, the jib weakest running), `KwApparentWind` (the game's wind, taken as 22 m/s in its strongest and slower by
  the square root of its push in a lighter one, less the ship's speed; the push's strength the game's, up to 1.25 times
  more when the apparent wind blows harder, never less), `KwSailMix` (a blueprint's sails by kind: areas, power at full
  and at half sail as `KwShipSail.Share` sets them, the drive as each kind's by its share), `KwSailSpeeds` (steady speed
  by the true wind's angle where push meets drag, the best and the closest a third of it reaches; the figures' and the
  offline checks'), `KwSailTrim` (the side to leeward with 6° of give across the bow and stern, a boom's angle, a jib
  foot's angle and its turn about its stay), `KwBoomSwing` (a launched ship's sail along the ship, its gaff, its boom with
  its lacing (left out of the ship's look and the lashings, `KwSailShapes.LooseBooms`, `Apart`), its halyards and lift
  turned about the mast, its boom sheet redrawn to its foot), `KwJibSwing` + `KwJibHung` (a launched ship's jibs turned
  about their stays, the leeward sheet taut, the windward slack), `KwSailWords` (the figures' Sails and Winds lines,
  the helm card's wind line). The window's second page, what a mast carries besides its sails, saved
  after them in its text: `KwMastTops` (crow's nest, fighting top, pennant; their parts "nest,h", "top,square,h",
  "pennant,colour"), `KwSailTopsForm` (the page), `KwMastTopFit` (heights clear of the sails, the window's note),
  `KwMastTopShapes` (their meshes and the climbing pegs, in the mast's mesh), `KwMastTopColliders` (a launched ship's
  boxes for them), `KwMastClimb` (E / Shift + E up and down the pegs), `KwPennant` + `KwPennantLook` (the streaming
  pennant, from the game's wind and the network clock alone; offered none or the sails' cloth, a dyed one only on a
  mast saved with it), `KwClothField` (the windows' cloth choice: the offered cloths, and a sail's own after them).
  Bowsprit and
  jibs: `KwBowspritTool` + `KwBowspritForm` (click a stem post's top; length, rise, jibs, cloth), `KwBowsprit`
  ("bowsprit|length|rise|jibs|cloth", the spar), `KwJibStay` (the stay's upper end, found from the ship's pieces when
  the jibs are built: the loaded masts on the dock, the blueprint's on a ship), `KwJibPlan` (the jibs' cuts, sized by
  the bowsprit's length and held clear of the foremast's sails, and area, their drive in `KwBpBow.Pull`), `KwJibShear` (a triangle sail's square-cornered cloth set into a jib's shape on three
  nested transforms), `KwJibUv` (the jib's own triangle in its cloth's texture, at one scale both ways, its foot on
  the texture's foot, as much cloth a metre as its foremast's lowest sail where it fits: the shear undone, nothing
  stretched), `KwJibDress` (jibs in the world), `KwJibRig` (on the dock: set with the foremast's hoist),
  `KwShipJibs` (on a launched ship: by its speed). Sail patterns (a sail's ",pattern" and a bowsprit's "|pattern":
  "vstripes:red:3", "border:black", "emblem:white:piece_banner02", "emblem:red:img/raven.png"; none for plain, so old
  texts read plain): `KwSailPattern` + `KwSailDye` (the six patterns, the seven colours), `KwPatternField` (the windows'
  rows: pattern, colour, stripes, emblem; the note about pictures), `KwSailEmblems` (the game's eleven banners drawn
  whole from their textures, and PNG pictures in `BepInEx/config/Broadside.Sails/`), `KwSailFrame` (where the cloth
  lies in its texture and its proportions: a jib's triangle placed in it, stripes spread over the cloth's reach), `KwSailPaint` (the pattern dyed into the
  cloth's texture at the cloth's own lightness), `KwSailPixels` (PNG decoding, textures read back from the graphics
  card), `KwSailLooks` (the patterned materials, made once per cloth, pattern and proportions and shared; the plain cloth
  on a dedicated server), `KwSailPreview` (the cloths in the tools' see-through previews), `KwSailGhost` (the sails and
  jibs in a saved ship's hammer ghost). `KwSailCanvas` keeps a reefed sail's texture on its cloth (a gaff's head on its
  gaff, a patterned triangle's after edge on its diagonal), so a pattern lies right set, reefed or furled.
  On a launched ship (2026-10-26, the user's "the sails, the post cap and that big long pole that goes out the front of
  the ship"), `Sea/KwSeaRig*`: `KwSeaRig` the sea kind (owns `KW_gen_sail`, `KW_gen_bowsprit`, every `KwCaps.Is` cap;
  serves the sail, bowsprit and Post cap tools; a fallen piece removed gives nothing of its own back, `Cost`),
  `KwSeaRigTools` (each tool's click: its own part aimed at first, by its rig hit box, else a deck's or roof's top for a
  mast, a post's top for a bowsprit or cap; any other rig piece opens its own window; the cap tool's ghost on a free post),
  `KwSeaRigWindows` + `KwSeaRigSession` (the dock's sail and bowsprit windows through `KwSailTool.Elsewhere`,
  `KwBowspritTool.Elsewhere`, `KwBowspritForm.StayElsewhere` (the stay to the ship's own foremast) and the forms' `Extra`
  row (Remove); the old piece hidden, `KwSeaHide.Until` on OK), `KwSeaRigCapWindow` (`KwCapEdit`'s rows, the post measured
  from the blueprint; an old hammer cap built again as `KW_gen_cap`), `KwSeaRigGhost` (spars through `KwSeaGhost`, cloths,
  ropes and jibs through `KwSailPreview.ShowOn` under `ship/visual`), `KwSeaRigSites` (`KwSeaPost`: each keel's post top
  from its spec as the snap point is placed; the post a ray passes within 0.6 m of; the cap or bowsprit on a post),
  `KwSeaRigPick` (the hammer's remove: the rig hit box met, else the nearest spar's surface, a fallen one's stump, for the
  0.2/0.75 m rule), `KwSeaRigCheck` (the owner: texts written again by the dock's edit kinds, one cap and one bowsprit a
  post, a change in place, a fallen piece only removed; the refund), `KwSeaRigRedraw` + `KwSeaRigLayers` (every peer: a
  changed index's platform boxes (`KwMastTopColliders`, each box marked `KwMastTopOf`) and fitting
  (`KwShipFittings.Sail`, `KwShipJibs`) made again, every bowsprit's jibs when a mast changed, then
  `KwRigDamage.Refit`: the rig read again, those indices' hit sets and looks remade (`KwRigDamageHits.Refit`,
  `KwRigDamageLooks.Refit`), the others kept with their piece as read now, the ZDO's marks read at once; `Dress`: ship
  materials, no shadow), `KwSeaRigRetie` (every other mast's ropes tied on again from the blueprint as it is now, a mast a
  step in `KwSeaShip.Later`, after a mast or a railing changed), `KwSeaRigWords`.
- `Forms/`: `KwForm` and its parts (`KwField`, `KwFormRow`, `KwFormBoxes`, `KwFormButtons`, `KwFormValues`,
  `KwFormView`, `KwFormCopies`, `KwFormTyping`): the small sizes window in the game's look.
- `Blueprints/`: `KwBlueprintStation` (the table piece; its generated table only without the bundle), `KwTableModel`
  (the table's workshop model from the `kw_bptable` bundle: its `model` and `col_box_*` boxes under the piece on every
  peer, dressed on screens in `piece_workbench`'s `high` body material with the bundle's maps and metal mask, gloss
  0.62; a `KwDef.Model` that `KwPrefabs.Make` uses before the generated mesh), `KwBpStation` (hover, Use), `KwBpStationRule` (stands only by
  a dock), `KwBpStationText`, `KwBpShelf` (the BlueprintShelf window), `KwBpStationActions` (its buttons),
  `KwBlueprint` + `KwBpFile` + `KwBpJson` + `KwBpData` (a design and its JSON), `KwBpLibrary` (the player's files),
  `KwBpDock` (which dock the table serves), `KwBpStrays` + `KwBpHull` (which pieces belong to the ship; the hull's
  measures for a launched ship), `KwBpBow` (the bow from the rudder or the sails' pull), `KwBpJobs` + `KwBpRunner`
  (building and removing over frames), `KwBpShipRemoval` (Alt + remove takes the whole ship), `KwBpCamera` (the table's
  Build ship camera, on the BuildCamera library). The figures panel beside the window: `KwBpFiguresPanel` (opened from
  `KwBpStation.Use`; `KwBpBuildBack`, the shaped build), `KwBpFiguresBuild` (its objects), `KwBpFiguresView` (its
  refresh: the dock's pieces' signature twice a second), `KwBpFiguresText` (its lines), `KwBpFiguresTyping` (its boxes
  take the keys); the figures, from the launched ship's own code: `KwBpFigures` (hull, slices, waterline, weight,
  `KwShipStats`, cost), `KwBpHandling` (top speed, the closest to the wind, rowing and the turning circle from `KwShipDrive`'s step, the sails by kind, `KwSailSpeeds`),
  `KwBpStability` (GM = KB + BM - KG, the word from the roll period). Building back longer, wider or scaled:
  `KwBpReshape` (`KwBpShape`, the order), `KwBpStretch` + `KwBpStretchZone` (amidships: the longest stretch only
  straight parts cover, spread, ribs added or thinned), `KwBpResize` + `KwBpPosts` (wider; scaled, caps and rudders
  onto their posts), `KwBpPartText` + `KwBpSpanText` + `KwBpHouseText` (the saved texts made again, cabins
  included, refused past each tool's limits).
  The saved ships in the hammer: `KwBpEntries` (the client-side
  `KW_bp_` prefabs), `KwBpHammer` (putting them in), `KwBpCost` + `KwRigCost` (what one costs; the ladders', masts'
  and sails' part), `KwBpGhost` + `KwBpMeshSet` (the
  ghost's look, the whole ship combined, its icon rendered by `KwIcons.RenderAll`), `KwBpWater` (the ghost on the
  water, too shallow refused), `KwBpPlace` (the click: the ship launched, the cost taken). Load (2026-10-27, the user's
  hammer lag: all fourteen saved ships' looks and icons made on the first hammer of every launch, about 1.4 s of 50 to
  400 ms frames): the entries are read one a frame from the local player's spawn (`KwBpWarm`); a look is made only
  when the hammer holds that ship or it has no icon yet (`KwBpGhost.Want`); icons, the pieces' and items' too, are kept
  by `KwIconCache` (a saved ship's by a hash of its file's text, so a player's own ship renders again only when it is
  saved again at the table, whatever the mod's version) and rendered only when new.
  Generated part meshes (a part's strakes, frames, cabin) are kept for the session by `KwPartMeshes` (keyed by tool,
  text, tier, covers shut and the part's seat in its own frame; each use a copy), so a previewed design builds, loads
  and repeats without generating them again.
  The mod carries the icons that are the same for every player (`assets/icons`, gzipped: the 11 ready-made ships, the
  rendered pieces and items), so no player renders them. **Before a release** that changes how ships, the ready-made
  ships or a rendered piece or item look: delete `assets/icons/*.icon` (carried icons are found first, so they are
  never rendered while carried), build, launch LocalTesting, take up the hammer once, run
  `python Broadside/tools/bake_icons.py` and build again (it refuses while an icon is missing from the cache).
- `Ships/`: a launched ship. `KwShipPrefab` (the karve copy), `KwShipLaunch` (spawning it), `KwShipData` (its blueprint
  in its ZDO), `KwShipBody` (building it on every peer; where there is a screen its steps run over a few frames, about
  4 ms a frame, its colliders and tuning always on the first, `Built` once all have run: `KwShipFittings.Steps` a piece's
  fitting a step, `KwSeaShip.Steps` and `KwSeaKind.Building`, `KwSeaOpeningLooks.Building` a part's look a step; its body's
  centre of mass and inertia off automatic before a box is added, and the colliders made switched off and switched on
  once, 2026-10-27: a Little Gull from 906 ms in one frame to a few frames), `KwShipParts` (a blueprint piece's sizes, prefab copies, the
  rudder's post), `KwShipTuning` + `KwShipHydro` + `KwHullSlices` + `KwSectionBlend` + `KwSectionMath` (how it floats:
  the hull in slices, each shaped as the hull is at its middle, Archimedes), `KwShipWeight` (its weight, the water
  displaced to the design waterline, and the weight's centre straight over that water's middle, 35% up to the
  waterline, so it floats level; the one reckoning for the ship, the dock waterline and the table's figures), `KwShipDrive` (its physics step; its sails' push from `KwShipWind`), `KwShipStats` (speed, sail push, steering, health, its sails by kind), `KwShipWind` (the ship's sails by kind and apparent wind on every peer; the owner's sail push by kind, eased as the game's), `KwShipLook`
  (combined look), `KwShipColliders` (decks, hold floors, keels as boxes; the planking's sides and keel strip from
  `KwHullSkin`, boxes lying on the drawn strakes' face, named "hull": `KwSkinSheet` + `KwSkinQuad` (the face as the strakes
  are drawn, carried past rib ends and under raised sides), `KwSkinFit` + `KwSkinBox` + `KwSkinQuad2` (a box over a stretch,
  laid along the strakes, upright or along the sheer), `KwSkinPatch` (a stretch cut in two where no box fits), `KwSkinRuns`
  (one box over a straight run of gaps), `KwSkinOpenings` (cut round the openings' bores), `KwSkinCache` (once a shape);
  the ends `KwShipTransomWall`: transoms, keel ends, cabins' raised ends), `KwShipShadow`, `KwShipMaterials` (no
  strobing), `KwShipWaterMask` + `KwMaskShape` + `KwMaskSections` (the game's water mask as a thin slab round the sea's
  real surface in the hull, drawn in past the planking, over the hold's water, none where the sea has taken over; it
  must never show from outside: wherever it shows the sea is not drawn) + `KwShipSeaInside` (on every screen, every
  0.1 s: the sea's line across each section, and per compartment whether the sea is its water, `KwFloodHandover`),
  `KwShipCamera` + `KwShipCameraSwap` + `KwShipCameraCover` (in the open the camera passes through the ship and is lifted out of holds and cabins; under a deck or roof the hull blocks it), `KwShipSteady` (the camera's height lags the heave), `KwShipFittings` (rudder, hatches,
  sails, ladders), `KwShipHatch` + `KwShipDeckHoles` + `KwHoverForward` (hatches on a ship), `KwShipLadderLanding` (after the game's ladder climb on a ship, the player moved to the nearest spot their capsule fits, the fall reset), `KwShipLadder` (climbing
  aboard: the karve's ladder placed, dressed and its climb's place set, from `KwShipLadderFind`: a stretch of the hull's
  side with no cabin at the hull, near the widest section, port before starboard, clear of openings, shield hooks and
  posts, the anchor's lead, a rudder and fittings, with a deck inside the rail; else a doorway in a side; else the lowest
  cabin roof's edge over a raised side; the ship as it sees it `KwShipLadderShip` (the rope tie places of `KwRigShip`,
  decks and hold floors, fittings, `KwShipLadderHoles` openings, the anchor's lead), `KwShipLadderRoom` (what is in the
  ladder's way, where a person stands), `KwShipLadderSides` (the hull's outside), the result `KwShipLadderPlan`),
  `KwShipSail` (sails set by speed, mast by mast), `KwShipHelm` + `KwShipTiller` (where the helm is),
  `KwShipWheel` (a captain's wheel turned by the ship's helm; the helm at it when its gear reaches the rudder,
  `TakesHelm`) + `KwShipGear` (the rudder built with its steering gear, saved or laid out from the blueprint).
  `KwShipDress` (from `KwShipFittings.Fit`): `KwShipShields` (every railing's shield hooks, the owner's confirmation),
  `KwShipNameBoard` + `KwShipName` (the boards' name in the ship's ZDO; `KwShipNameHover`, the helm's hover starts with it).
  Breaking up (2026-10-26): `KwShipBreak` (the remove refused with anyone aboard; `KwShipBreakOwner`, the owner's
  `RPC_Remove`: asked again, then the crates and the look), `KwShipBreakRules` (the game's checks in its order, `Aboard`:
  the onboard trigger or `Over` the hull's plan up to 25 m over the sheer), `KwShipBreakSpill` (the cost from the packed
  blueprint, `KwBpCost.Of`, and the hung shields into `CargoCrate`s at the water's surface, or the keel's height on land),
  `KwShipBreakPlan` (plain numbers: stacks, crates, places in rows along the hull 1.4 m apart, centred across),
  `KwShipBreakLook` (wood_floor's destroyed effects along the deck, sounds at the ends and middle; `Kept`, the prefab's
  destroyed effect without the karve's broken hull), `KwShipBreakHover` (the hammer-mode hover line or why not).
  Pieces built on a launched ship (riders, 2026-10-26; Multiplayer above): `KwShipRiders` (on the ship: its id, its
  riders as children, `Take`/`Drop` keeping the body's mass with `KwBodyMass`, `LetGo`, `Shown`: a hit carried to the
  drawn pose), `KwShipRider` (on the piece: its record, aboard, parked, the owner's upkeep, `Release`, a pose written
  again taken), `KwShipRiderMove` (that pose eased to on every peer but its owner's), `KwShipRiderRules`
  (which pieces ride: the ship cannons only; which are refused aboard; when a parked one is let go; plain data), `KwShipRiderPose` (the pose maths, plain arithmetic),
  `KwShipRiderPlace` (`KwShipRiderAim` the placement ray, `KwShipRiderGhost` the rules on the ghost, `KwShipRiderPlaced`
  the piece placed, `KwShipRiderRefused` why a piece was refused aboard), `KwShipRiderScene` (`KwShipRiderWake`, and the riders let go before the game destroys the ship's
  object), `KwShipRiderFind` (a ship's ZDO by its id near a point), `KwShipRiderBreak` (broken up into the crates,
  wrecked with drops where they were), `KwShipRiderHover` (the hover on a rider).
  `KwShipHouses` (cabins' boxes, from `KwShipColliders.Add`: the roof's slabs named "cabin_roof", the raised sides on
  their strakes' face (`KwHullSkin.Raised`), the raised ends and each wall's boxes cut from its outline named "deck"; `KwShipEyes` puts the view at the eyes under a roof within 1.2 m
  of them). Sailing, all added by `KwShipSailing`: `KwShipAnchor` + `KwMooring` (the anchor's state and the ride to it) and
  `KwShipWindlass` (E at a windlass; the anchor and cable drawn through the lead's fairlead), `KwShipCargo` + `KwShipLoad` (the load aboard, the
  most a hull floats, the drag), `KwShipKeys` + `KwShipFinder` + `KwShipPins` (whose ship, the server's lookup, the map
  pins), `KwShipPerks` (by the hull's tier: the Ashlands sea's burning and the fire modifier, set from
  `KwShipStats.Apply`) and `KwShipCard` (the helm's hover: tier, health, fire, Ashlands, cargo, anchor; holes and the
  hold's flooding when there are any).
  Damage and flooding, all added by `KwShipDamage`: `KwHullHole` + `KwHullHoleText` (a hole; the holes and the parts'
  wear as ZDO text), `KwHullPartMap` + `KwHullPartCabins` (which planking, transom, deck or cabin face a hit strikes,
  from the seated spans and cabin texts: the nearest face that faces the shot best, the hole put on the outside line and
  square out of the face there on planking and raised sides, as an opening's tunnel is), `KwHullDamage` (the
  rule: wear to a threshold, then a hole, widened by more hits; at most 32 holes), `KwHullHoles` (a ship's holes on every peer, made on the
  owner, mended by RPC), `KwHullDamagePatch` (the game's `WearNTear.ApplyDamage` and `Repair` joined to them;
  `KwHullBurst`, a burst's hit given its own place and the shot's way),
  `KwShipFlood` (the water in the hold on the ship: let in by the holes, its weight in the buoyancy step, sinking, the
  ZDO), `KwFloodHolds` (the compartments' water: each its own flat surface, level across and a fifth of the pitch
  along, the sea into the holed one, passed slowly through the bulkheads), `KwBulkheads` + `KwFloodWall` (where the
  bulkheads go: the planking parts' ends, 1.2 to 4 m apart; seeping through under the waterline top, pouring over
  it), `KwFloodLevel` (a run of slices' level surface for a volume), `KwShipBail` (B in the flooded hold, the
  compartment stood in), `KwFloodSurface` + `KwFloodFaces` + `KwFloodWater` (each compartment's water drawn, its top
  in a still copy of the sea's own material, its underside opaque, its end against a bulkhead where the two sides
  differ; not drawn where the sea has taken over, its water within 5 cm of the sea or the ship sinking: the sea is the
  water there).
  Real holes (2026-10-26, the user's words: "Instead of a black spot there, can you literally have a hole? Make it look
  like there's a hole in the ship, and let objects pass through it"): `KwHoleShape` (a hole's frame square to its face and
  its ragged outline, 13 to 16 corners from its seed: rough round, split along the grain, notches with splinters; its
  inner radius), `KwHoleLooks` (on the look's holder: the hull's own looks, kept apart by `KwBpMeshSet.Apart`, packed by
  `KwHoleChunks` into a few meshes a material of at most 32000 vertices along the ship (decks; planking parts and cabins
  are drawn alone by `KwSeaOpeningLooks` and handed in by index, `Own`, so a sea edit replaces one part); a chunk a hole
  comes near is cut again), `KwHolePart` (a chunk's renderer, whole mesh and bounds) + `KwHolePatches` (its holes cut a group at a time, the
  holes whose triangles meet together, each group's patch kept by its holes' keys, joined onto the whole mesh), `KwHoleCut`
  (`Patch`: the triangles within a hole's depths, 8 cm out of the face to 15 cm in, less its outline, `KwHoleClip`: the
  wedges outside the star, three cuts each; `Join`; `KwHoleIndex`, the triangles' bounding spheres; `KwHolePatch`),
  `KwHoleRim` + `KwRimProbe` + `KwHoleSplinter` (the broken board end through the thickness in the part's own wood, in 3.5 cm
  steps where the part's faces are found along the normal; splinters bent in through the hull at the notches),
  `KwHoleMesh` + `KwHoleMeshes` (a mesh as lists, read from and written to Unity's), `KwHoleBoxes` + `KwHoleGaps` (every
  box a hole's wall is, its thin side within 60 degrees of the hole and its middle plane within 0.25 m, stood in for by
  boxes round the hole's passage: three rectangles inside the outline seen on the box, carried along the normal through
  the box no deeper than the hole's cut; each hole's boxes worked out once; the body's mass kept).
  The black mark (`KwHoleLook`) is gone.
  Patches and repairs (2026-10-26; Multiplayer above), added by `KwShipDamage`: `KwHullPatches` (on the ship: the patches
  from its ZDO, `Leaking` for the flood, the patch calls, the owner's tidying) + `KwHullPatchText` (the ZDO text; which
  holes are patched, which leak, plain), `KwHullPatchSpot` (the hole the local player aims at as a hover and use: E, or the
  kit from the hotbar; `KwHullPatchHover`, `Player.FindHoverObject` given it when it found nothing of its own) +
  `KwHoleAim` (a ray meeting a hole, plain), `KwHullPatchLooks` (drawn: a patch on or off) + `KwHullPatchBoard` (one
  patch on the hull's outside: the workshop's, or `KwHullPatchMesh`'s lapped boards) + `KwHullPatchFall` (hammered on,
  broken away and falling); the hammer: `KwShipRepair` (the one `WearNTear.Repair` postfix on a launched ship) +
  `KwShipRepairWork` (one click: the aim, the step, the owner's call, the corner message) + `KwShipRepairPlan`
  (`KwRepairStep`; every step in order: patched holes, holes, railings, rig marks, fallen pieces, nearest the aim first
  within each; plain) + `KwShipRepairAsked` (steps just asked); `KwRigRise` (drawn: a piece put back, or a rudder blade
  mended, growing back in about a second). In `Items/`: `KwRepairKit` (the kit's prefab, on every ZNetScene wake),
  `KwRepairKitRecipe`, `KwRepairKitLook` (the bundle `kw_repairkit` when the mod embeds it: the kit's model with a box
  collider, its icon, the patch model; else fine wood's look and the generated patch; dressed in a game material).
  Shots through the ship (2026-10-26, the user's words: "If the cannon is close enough to a ship, can the cannonball
  shoot through the ship: a hole in one side, through the ship, and a hole out the other side? ... Can it also do damage
  to railings? With a small iron cannon, maybe about 20 shots in total should sink a fairly large ship."): `KwShipShot`
  (the shooter's game: `Projectile.OnHit` first of all its patches, a heavy shot on a launched ship sent instead of the
  game's hit, its burst held back) + `KwShipShotData` (the `KW_ShipShot` package), `KwShipHitRouter` (on every ship:
  the takers by kind, `Register`, `Tag`, `Health`; the route on the owner; `Routing` keeps the wear's own hole rule out
  and the takers' hits from being sent as shots again) + `KwShipHitWalk` (each thing to its taker, plain), `KwShipHit` +
  `KwShipHitPart` (a tag on a collider's holder: kind, piece, part; the rig's are DMB's `KwRigDamage*`) + `KwShipHitMeet`,
  `KwShipHitTrace` + `KwShipHitClass` (the ray through the ship as its body stands, classed: a character, a tag, a rider,
  a box by the hull map facing as the struck face does, a planking box's normal taken in the section's plane as the
  map's are, the rudder's blade, a fitting), `KwShipHitEnergy` (heavy, the
  energy from damage, speed and range, what a wall takes), `KwShipHitTakers` + `KwShipWallHit` (walls through or stopped,
  fittings, riders, bodies), `KwShipHitAfter` (the burst where it stopped, or the projectile flying on), `KwHullDamage.Pierce`
  (a hole at once) and `KwHullHoles.Pierce`. Railings: `KwShipRails` (each stretch between two posts a box under
  `colliders/rails`, tagged; broken ones in `kw_rails_broken`, their boxes off; the taker; mending), `KwRailStretches` +
  `KwRailStretch` (where the stretches lie: hull railings seated as their look, roof railings' runs), `KwRailBroken` (a
  railing's look broken: top rail gone with splintered stubs, posts snapped where both sides are gone), `KwShipRailLooks`
  (railings drawn apart, `KwHoleLooks.Takes`, one renderer each, redrawn when broken). Numbers: a small iron ball (108 on a
  structure, 48 m/s) carries 96 at 5 m, 69 at 20 m, 28 at 60 m; Meadows planking takes 35 (5% more a tier, more at a
  slant, at most 3×), a deck 22, a cabin wall 25, a railing 12 and a tenth; a pass-through costs the ship 1.6 shots'
  health, a far wall stopping it 1.4 and its burst inside.
  Rig damage (2026-10-26; Multiplayer above), all added by `KwShipFittings.Add` (`KwRigDamage.Add`): `KwRigDamage` (on the
  ship: its marks from the ZDO, `Edit` on the owner, the RPCs, what changed handed on), `KwRigMark` + `KwRigMarks` (a mark,
  the text), `KwRigParts` + `KwRigPiece` + `KwRigPieceRead` (the rig from the blueprint: masts with their sails, bowsprits
  with their foremast and jib power, post caps, rudders; each sail's lashings, hoops and lacing along its spars),
  `KwRigSpar` (a spar in its piece's kit axes as `KwRound` draws it: radius, ways across, a shot's place, side and offset),
  `KwRigSection` (plain: a ball's band of a section, a notch's chord and scoop, the section lost, `Breaks` at half),
  `KwRigCondition` + `KwSailHurt` + `KwSailHurtRead` (what the marks mean: broken fastenings, broken yard arms, booms and
  gaffs, droop and holes' shares, the drive left), `KwRigDamageTakers` + `KwRigDamageCuts` (the owner's takers and the
  marks they write: notches deepened when they overlap, a fall past half the section, holes counted once, chips, chunks;
  `Bite` a ball's half-width, `Wood` 1190 energy a square metre of section), `KwRigDamageHits` + `KwRigHitSet` +
  `KwRigDamageBoxes` + `KwRigHitFrame` + `KwRigHitFind` (the hit colliders: capsules on spars, boxes on sails, caps and
  blades; sails sized to their hoist, booms' frames turned with the drawn booms, stumps, a fallen mast's platforms off),
  `KwRigDamageDrive` + `KwRigPower` + `KwRigSteering` (the push by the sails still working, whole exactly the mix's; the
  steering by the blade left, `Floor` 0.12), `KwRigDamageMend` + `KwRigMendAsk` (the hammer: its hover on the rig, and what a repair click asks, `KwShipRepair`),
  `KwRigBlade` (a rudder blade's box, area left, where a chunk broke it, ragged board by board). Drawn only:
  `KwRigDamageLooks` (on the visual holder), `KwRigLook` + `KwRigLookPlan` + `KwRigSparDraw` + `KwRigLookKey` (each mast,
  bowsprit and cap drawn on its own, `KwShipFittings.Moves`, cut by its plan: spars that go, broken hoops, spars drawn
  again, a fallen piece's high part), `KwRigDamageSpar` (a notched spar's mesh, splintered ends), `KwRigDamageMesh`
  (triangles kept, craters, joins), `KwMastFall` + `KwRigSound` (the fall, 6 to 9 s, the ropes' whip, the game's sounds made local, scaled in volume and pitch; the heavy gate's creak found in `darkwood_gate`'s door effects) + `KwMastCreak` (the wood creaking and splintering as it leans, most when about halfway over; a ship's timber crash where it lands) + `KwMastFallSplit` (only what is above the break goes),
  `KwRigDamageRudder` + `KwRigBladeCut` (the blade sliced by height and clipped, broken boards' ends); in `Sails/`
  `KwSailDamageCloth` (a mast's sails: holes, droop, rings), `KwSailDamageHoles` (the texture place, the cut, the holed
  material), `KwSailDamageDroop` + `KwSailDroopAlong` (through `KwSailMotion.Bend`, `IKwClothBend`), `KwSailDamageRings`
  (lashings and lacing without the broken rings, booms and gaffs notched or broken), `KwSailDamagePierce` (a shot through a
  sail). Numbers: a small iron ball's bite 0.1 m each side; dead centre at 5 m it fells a 13 m mast (0.20 m at its foot),
  a 16 to 20 m mast takes two at 20 m; 0.14 m off the axis it does not; a yard arm goes in one; a hole's radius 1.6 bites
  and 4 cm.
- `Helm/`: the helmsman at a tiller or a captain's wheel. `KwHelmsman` (who holds it, his hands and feet; a ship's helm
  or a dock wheel's), `KwTillerGrip` (the peg, one or both hands, where he stands), `KwWheelGrip` (hand over hand on
  the wheel's handles, where he stands behind it), `KwHelmHold` (one frame's pose, and `IKwHelmGrip`, what he holds), `KwHelmPose` (his upper body turned and leaned),
  `KwHelmArms` (his arms by two-bone IK, the LimbIk library), `KwHelmFeet` (walking to the stand), `KwHelmPatches`
  (taking the helm on his feet, walking, facing). `KwHelmKeys` (2026-10-26, the helm key list: while a launched ship is
  steered, H push off and B bail listed at the bottom right in the strip where the game shows its key hints, which it
  hides at a helm; each line a copy of the game's own attack hint, laid out as its row; hidden off the helm and while
  the inventory, map, radial or game menu, chat or a pause is up; installed from `Broadside.Awake`, nothing on a
  dedicated server). Other mods add lines by name only: the list is `Broadside_HelmKeys` under the game's `KeyHints`,
  every child a copy of the game's hint ("Text" beside "key_bkg"/"Key"); a mod copies the first line, renames it and
  sets its words (the ship cannons' U broadside and J shot arcs, `KwHelmRows`).
- `Cannons/` (2026-10-26; `CANNONS.md` there has the behaviour in full): the four ship cannons, three graduated from the
  AssetLab Cannons bench (its mounts, mounted cannons and field cannons left out) and the bronze one made from the small
  iron one's design the same day; only the bronze and small iron cannons, stone and iron shot and black powder offered for
  now (`Arms/KwTiers` `Offered`, the Swamp since 2026-10-28: the others out of the hammer and without recipes, still registered). `KwCannonPrefabs` (the prefabs, after
  Broadside's own, copies of `wood_floor` stripped with `KwPrefabs.Strip`; the MannedWeapons library's patches and the
  broadside's ticker installed) and `KwCannonHammer` (their costs, added by `KwHammer` after Broadside's pieces).
  `Arms/`: the manned-weapon framework on the MannedWeapons library (`KwArm` the cannon, `KwArmDef`/`KwArmory`/
  `KwArmBuild`/`KwArmDesigns` its definition and piece, `KwArmMotion*`/`KwArmMover`/`KwTrack` its moving parts,
  `KwArmHands`, `KwArmReload`/`KwAmmoStore`, `KwArmTrigger`/`KwArmLaunch`, `KwArmLook`, `KwArmLink` its keys and calls,
  `KwArmWords` every word, `KwArmControls` the hauler's controls, `KwArmPlace` only on a launched ship's deck), the shots
  (`KwShot`, `KwShots`, `KwShooter`, `KwShotLook`, `KwShotHits`, `KwShotWater`), the captain's broadside (`KwBroadside`
  U, `KwBroadsideReload`, `KwBroadsideLoad`), the shot arcs (`KwShotArcs` J, `KwShotPath`, `KwBall`, `KwShotArcLook`), the
  helm key lines (`KwHelmRows`) and the reload lines (`KwReloadRows`, `KwReloadCount`: rows `cannons.port`/`cannons.starboard` of Broadside's own
  ship panel, `KwShipPanel.Row`). `Guns/`:
  the guns' shapes (`KwSmallIronCannonShape`, also cast in bronze for the bronze cannon, `KwSmallCannonShape`,
  `KwLargeCannonShape`, drawn long by `KwStretch`), `KwStone` (the game's stone on the stone ball and the bronze cannon's
  ball at the breech), `KwCannonBreech` (`KwBreech`, `KwCannonWork`), `KwCannonFiring`, `KwCannonParts`, `KwArmMeshes`, the
  shots (`KwCannons`) and items (`KwCannonballs`, `KwBlackPowder`, `KwItemDef`, `KwItemModel`, `KwItems`). `Ship/`: the
  ship cannons (`KwShipCannons`), the truck carriage (`KwShipSpec` with `KwCheeks`, `KwShipCarriage`, `KwShipIrons`,
  `KwShipRig`, `KwQuoin`, `KwBreeching`, `KwBreechingTie`, `KwCannonBody`: colliders and rear, no body of its own), its
  recoil (`KwShipRecoil`), its trucks (`KwWheels`) and its haul about a deck (`KwArmTow`, `KwTowHitch`, `KwTowHands`,
  `KwDeckHaul`, `KwDeckMath`, `KwDeckFloor`, `KwCart`), writing `KwShipRider`'s pose keys.
- `Openings/`: doorways, windows and gunports cut into walls (2026-10-25), one system for a planking part's sides, a
  cabin's own walls and its raised hull sides. `KwOpening` (one opening: its wall, shape, place along, sill, sizes, frame,
  cover), `KwOpeningText` (the part's text's "open=" segment: before the paint in a span, `KwSpan.Openings`; last in a
  cabin's, `KwCabinSpec.Openings`), `KwOpeningShape` (outlines: doorway, round, square, rectangle, arched top; limits;
  which covers each takes), `KwOpeningCut` (a convex piece less convex outlines, as convex pieces), `KwOpeningSkin` (the
  planking's and raised sides' strakes and painted bands cut, through `KwGirth.Holes` (the openings themselves), by
  `KwOpeningRing`: each cut straight through the board along its tunnel, its corners the lining's own (`KwFrameRing.Steps`)
  on the face line, a ring of quads round it turning back to the wall's own depth (`KwRingBand`: on the face line under
  the frame, then fading; worked out once an opening), outside it `KwOpeningCut`; points put
  on the piece's own two faces by `KwPieceFan`, every corner on its neighbours' edges by `KwRingSeal`; a segment scaled
  for a reshape), `KwOpeningTunnel` (2026-10-25, "almost a diagonal drill through the ship": an opening's straight bore,
  square to the face at its middle, `Way`; a depth goes along it as far as the wall's own depth reaches the same face, so
  faces, stand-offs and clearances keep their measures; its `Ring` and `Strength`, sized by `KwOpeningRings`: wide enough
  for the inside face's corners to slide, off the part's ends and other rings),
  `KwOpeningSurface` + `KwPlankSurface` + `KwWallSurface` (a wall as a surface: a point at (along, up, depth), its ways,
  thickness, reach; a planking's depth along the sections' normals, as its strakes stand off; true metres on the face:
  `Fit` an opening's own outline point onto the wall, up its middle line from the sill (`Upright`: on planking plumb, so
  sides stand plumb where the hull flares and turns at once) then along at that height, `Fibre` the planking's own depth,
  `Rise`, `Run`, `Metres`), `KwOpeningSurfaces` (a part's walls with the openings that fit them, each fitted to its wall,
  `KwOpening.On`/`Surface`, so its `Outline` is its true size on the face, cached; one left behind by an edit is
  dropped; the saved text unchanged, u the middle's place), `KwOpeningFrames` (lining boards through the cut, frames on
  both faces, an iron band; a doorway's open at the floor; stopped at a wall's top or foot; `Front`/`Back` the faces at
  least 1.2 cm past the wall's furthest face) + `KwFrameRing` (the cut and the outer edge in 8 cm steps on the wall's own
  face, each step's board: a straight side or a curve) + `KwFramePen`/`KwFrameGrain` (faces, rims sunk 1.5 cm into the
  wall, lining, the grain along each board), `KwPlankGrain` (the plank wood's density, a board's start in the wood),
  `KwOpeningLeaves` + `KwLeafPlan` (where each cover's or door's leaves hang and
  swing, `Pivot`: about the hinge edge of the face they swing toward; a cover lifted clear of a frame's face; a door's leaf lies flat from its jamb to the far jamb, a double door's (cover `door2`, `KwOpening.DoubleDoor`)
  two from their jambs to the middle, one state for both), `KwOpeningLeafMesh` (a cover's boards, `KwLeafBoards`: boards along the hinge on a backing, grooved, and iron straps; a door
  hands over to `KwDoorMesh`: upright bevelled boards, ledges and braces inside, and `KwDoorIron`: knuckles, strap hinges,
  straps or studded bands, ring pulls, by `KwDoorStyle`; drawn in `KwDoorSpace`), `KwDoorLooks` (the game's `door_wood`
  copied and tinted per tier; the style per tier: Meadows plain, Black Forest and Swamp strapped, Mountains and up banded),
  `KwOpeningLooks` (the frames as meshes in the railings' plank wood whatever the tier (2026-10-25: "so they don't look
  like the deck texture was reused") and the game's iron; leaves shut for a ghost, `KwBpMeshSet.Shut`, through `Dressed`;
  `Placed(leaf, stage)` a leaf swung as on the dock), `KwOpeningPart` + `KwOpeningLeaf` (on the dock:
  frames and leaves under the part's "kw_openings" child, from `KwPartLooks` and `KwHouses.Build`), `KwLeafSwing` (a leaf's
  object, pose, way, sound, hover, shared with a launched ship's), `KwOpeningCost`, `KwTurns` (LookRotation and
  AngleAxis in plain arithmetic, for offline checks), `KwOpeningBoxes` (a planking part's fitted openings; a slab less
  rectangles, `Apart`; a launched ship's hull boxes are cut round each opening's bounding rectangle with all its bore
  sweeps through the wall's thickness seen on the box, `KwSkinOpenings`, so a round port is square in the collider;
  `KwShipShadow` takes its shadows from the same boxes),
  `KwOpeningBands` (painted bands cut round openings). The cut faces get no rim of their own: the frame's lining boards
  cover the cut, which avoids z-fighting. Cabin walls are cut in `KwCabinWall.Pieces()`, raised sides in
  `KwCabinShape.Sides`, planking strakes by `KwOpeningRing` through `KwGirth.Holes`; frames, lining, leaves and hinges go
  by the opening's own surface (`KwOpening.Surface`: its tunnel). Walls are named `p`/`s` (planking),
  `back`/`front`/`left`/`right` (cabin walls), `hl`/`hr` (raised sides). Old texts read with no openings; blueprint
  reshapes carry them.
- `Tools/` (openings): the Doorway and Window tools (2026-10-25 reworked: one click places, the real ghost, the wheel; then
  rows on Shift, the E fill and Shift's free placing gone), `KwDoorwayTool` + `KwWindowTool` (a click on a bare wall
  places, Shift + click its row; on an opening its edit window, Shift + click its row's, `KwOpeningRowEdit`). Placing: `KwOpeningTypes` + `KwOpeningCurrent` (the fixed types, the player's own from the last edit, the current
  one per tool for the session; the wheel's notches from `KwToolNotches`), `KwOpeningPlace` (`KwOpeningContext`: what the
  world is asked, `KwOpeningPlacing`: the spot and its plan; a window's middle at the aim, a doorway from the deck inside,
  as high as the wall's room), `KwOpeningSnap` (sill level with the side's or the type's height over the deck; along,
  whole spacings from the nearest of the side's openings of the tool's kind or a free stretch's middle within 0.4 m, the
  nearer wins, the spacing when the two are within 0.15 m; both in the aimed part's section, a spot on a rib moved off
  it), `KwOpeningSpans` (free stretches between the
  wall's ends, openings and ladders, `KwOpeningLadders`), `KwOpeningLine` (the side's parts and openings in the aimed part's
  measures, `Held` the nearest place off a rib; `Section`: the planking from rib to rib is the middle, each part from a rib
  to a keel end, or from keel end to keel end, a section of its own; `KwOpeningSpacing`: the last two placed, else the
  nearest's neighbour, else the type's). Rows (Shift held): `KwOpeningRun` (a side's free stretches by section: the
  middle's joined across its ribs into one stretch from the first rib to the last, each end part's its own between its rib
  and keel end; a row, its edit's Number and the snaps keep to the aimed part's section, 2026-10-25), `KwOpeningRow` +
  `KwOpeningRowPlan` (N evenly spread, one in the middle of each equal share, as many as fit,
  the count the wheel set, else the side's spacing; a place on a rib moved off it within its share), `KwOpeningFill` (the
  type cut at given places, refused ones skipped, one plan), `KwOpeningRowShown` (the row the ghost shows, worked out
  again when its stretch, count, sill or type change, at most every 0.15 s), `KwOpeningRowSet` (the row an opening clicked
  with Shift is in: its kind, in line, sill within 5 cm, in the free stretch round it; its edit filled in, or laid out
  again evenly with the answers and the Number). In the world: `KwOpeningHover` (every frame from `KwHouseTools`:
  placeholder hidden by its renderers, the wheel (types, or with Shift the row's count), the ghost (one, or the row),
  message; `Unless` from `KwPreview` drops it with another tool), `KwOpeningAimed` (`KwOpeningSight`: one ray, an opening seen through), `KwOpeningReads` (each part's walls
  read once per text, the ship looked up at most once a second, lines, partners and decks inside kept while its pieces
  stay), `KwOpeningGhost` + `KwOpeningGhostLooks` (each changed part stood in for by its body built from its new text, its
  bands, frames and shut leaves in their materials; its own model hidden meanwhile; the edit window's plan too, its leaves
  hung part open so a cover's style shows, the openings as they are while nothing changes),
  `KwOpeningWords` (the placement message in the middle of the screen). The rest as before: `KwOpeningSession` (the edit
  window, its still plan), `KwOpeningForm` (only the rows that change the opening, by shape: a porthole's diameter, a
  square's size, a rectangle's width and height, the covers its shape takes; built again by `Reshaper` for another shape,
  door, frame or fill-in), `KwOpeningSite` + `KwOpeningAim` + `KwOpeningRays`, `KwOpeningHull`
  (partners cut, changed, filled in and undone together), `KwOpeningRules` (a door 0.8 to 2 m wide; an open doorway any
  size its wall takes), `KwOpeningPlan` + `KwDoorwayPlan` + `KwWindowPlan`, `KwOpeningAsk` + `KwOpeningForm`,
  `KwOpeningNotes`, `KwOpeningWater`, `KwOpeningWork` (cut, change and fill in as one undo step). Free on the dock,
  wards respected; on a launched ship at the openings' cost, below.
- `Ships/` (openings): `KwShipOpenings` (a launched ship's covered openings, on every ship, one with none too; `Replan`
  after a sea edit), `KwShipOpeningLeaf` (a leaf's pose and collider; `Renumber`), `KwShipOpeningPlan` (the openings
  numbered in blueprint order, the same on every peer; `Renumber`/`States` between two plans; each part's openings fitted
  once by its piece, `KwShipOpeningFit` + `KwShipOpeningCache`, so a sea edit fits only its parts again). Open openings in the
  hull's own sides, from below the waterline up to 1 m above it, let the sea in through `KwShipFlood`; a shut cover is
  watertight, and a cabin's own walls never flood.
- `Sea/KwSeaOpening*` (doors and windows on a launched ship, 2026-10-26, the user's "add in a porthole window to put in
  another cannon"): `KwSeaOpenings` the sea kind (owns planking and cabins, serves the Doorway and Window tools, draws
  each planking part and cabin alone, `NewWood` false; its redraw: the parts' looks, their boxes, the covers numbered
  again, timed in the log over 4 ms). The tools run their dock code at sea: `KwDoorwayTool.Clicked`,
  `KwWindowTool.Clicked`, `KwOpeningHover.Frame(player, tool, sight)` with the sight `KwSeaOpeningAim` makes (a ray in
  the ship's frame: an opening passed through before the box met, else the nearest wall of the parts the point is on),
  the context of `KwSeaOpeningShip` (`KwOpeningReads.Use`: the ship's reads from its blueprint, made again each redraw:
  `KwSeaOpeningPart` each planking part and cabin with its walls seated on the ship's keels and every site's `Of` the
  part; `KwSeaOpeningMaps` the planking in line and a doorway's partners through `KwOpeningHull.Mapped`;
  `KwSeaOpeningFloor` + `KwSeaOpeningDeck` the deck inside, decks an opening crosses and the waterline as plain maths;
  the ladders as `KwLadderPose`s for `KwOpeningLadders.Against`). OK and a click send the plan through
  `KwSeaOpeningSend` (each changed part's piece with its new text, `KwSeaPiece.With`, at the plan's revision, the rules
  asked at the opening's middle); `KwSeaOpeningOwner` the owner (only the openings changed, `Rest`; the states moved;
  `Cut`: the holes whose middle lies in an opening cut, changed or filled in, outline and frame, on its wall, dropped with
  their patches); `KwSeaOpeningPick` the hammer's remove (a leaf or an opening the ray passes through at 0, else the
  nearest opening's frame on the wall met; the fill-in edit, a doorway's partners with it). Drawn: `KwSeaOpeningLooks`
  (each part's look through `KwBpMeshSet`, one renderer a material under `ship/visual/parts`, the ship's materials, no
  shadow, handed to `KwHoleLooks.Own` so holes cut it and an edit replaces only it), `KwSeaOpeningGhost` (the tools'
  ghost on the ship: the part as it would be drawn under `ship/visual`, the leaves shown, its own look hidden through
  `KwOpeningGhost`; its meshes kept by `KwSeaOpeningPrepared` and taken by the redraw on the editing player's game, which
  so builds nothing when the edit lands). Boxes: `KwSeaOpeningBoxes` (each part's boxes its openings cut, kept by index from
  `KwShipColliders.Sides` and `KwShipHouses.Opened`, made again alone; `KwHoleBoxes.Rebox` told which went and came).
- `Sea/`: editing a launched ship where it floats (Multiplayer above). The core: `KwSeaShip` (on every `KW_ship`, made as the
  last step of `KwShipBody.Build`: the RPCs, the revision polled, `KwSeaDiff`, the redraw by kind, `Later` one queued step
  a frame), `KwSeaEdit` + `KwSeaPart` + `KwSeaPiece` (the edit and its wire format; pieces made, changed, compared),
  `KwSeaApply` (applied in place, tombstones, `Back`/`Yaw`/`OnShip` in plain arithmetic), `KwSeaChanges`, `KwSeaDiff`,
  `KwSeaMarks` (the new wood's holes, patches, wear, broken stretches and rig marks dropped), `KwSeaCost` (the whole
  blueprint's difference, each kind's `Cost` correction; given into the inventory or at the feet), `KwSeaOwner` (the
  owner's checks, apply, write, answer), `KwSeaAsk` (the asker: rules, cost tried, one edit in flight, paid once),
  `KwSeaRules` (reach, no-build, ward, workbench), `KwSeaKind` + `KwSeaKinds` (a kind of part: owns, serves a tool, draws
  its own, click, hover, pick, removal, check, new wood, owner, cost, redraw, after; `All` one line a kind), `KwSeaAim`
  (the camera's ray in the ship's frame: its box, name and tag, and the rig's hit box on the "hitbox" layer), `KwSeaTools`
  (from `KwTools.Click` and `KwPreview`), `KwSeaRemove` (the hammer's remove: a part within 0.2 m, a near miss within
  0.75 m refused, else the break-up; `KwShipBreak` leaves a click it took alone), `KwSeaGhost` + `KwSeaHide` (the ghost
  under `ship/visual`; hidden parts, kept hidden until the redraw, `Until`), `KwSeaWindow` (a `KwEditKind`'s window on a
  blueprint piece, `RemoveRow`, `Hold`), `KwSeaRedraw` (stats at once; the ladder over the side and the shadow later),
  `KwSeaWords`. Railings: `KwSeaRails` (the kind), `KwSeaRailSite` (`KwSeaSheer`: the planking's top edge nearest the aim;
  the railing over a planking part; a new one's text), `KwSeaRoofSite` (a cabin's roof from the blueprint, its railing),
  `KwSeaRailAdd` (the new railing's windows and hover), `KwSeaRailCut` (the hammer's removal: a side, a roof edge, the
  part), `KwSeaRailDraw` (the redraw: `KwShipRails.Refit`, `KwShipRailLooks.Swap`, `KwShipShields.Relay`), `KwSeaShields`
  (the owner's shields renumbered, the hookless dropped; `KwShieldSlots.Places`). Openings `KwSeaOpening*`, ladders
  `KwSeaLadder*`, the rig `KwSeaRig*` (their own entries above).
- `Panel/`: the ship panel (2026-10-26, the user's words: "the speed, speed multiplier, explore radius, ship health, the
  cannon load/reload stuff ... all of that stuff needs to be in the new mod"), on the HUD while the local player is aboard
  a launched ship, in ShipConfig's panel's place and look (the minimap's frame and font, the game's tooltip box), local
  only, refreshed four times a second, writing only what changed. `KwShipPanelHud` (on the HUD from `Hud.Awake`, run in
  LateUpdate: shown, hover each frame, refresh), `KwShipPanelView` (the `Broadside_ShipPanel` object under the HUD root,
  its rows, layout, place under the wind indicator or the minimap), `KwShipPanelLines` (what they say: speed,
  `KwSpeedometer`; speed multiplier, `KwShipPace`: the ship's top speed against a karve's as the table's figures work it
  out, under oars its hull's speed factor, worked out once a ship and sea edit, times the helmsman's Sailing factor;
  explore radius; ship health from its wear; other code's lines; GrindstoneSkills' Sailing abilities and cooldowns),
  `KwGrindstoneLink` (GrindstoneSkills' `SailingApi` by reflection, ShipConfig's way), `KwPanelRow` + `KwPanelLook` +
  `KwPanelHover` (a line on screen, the look, the tooltips past the inventory's backdrop), `KwShipConfigPanel` (aboard a
  Broadside ship ShipConfig's `ShipConfig_ShipPanel` under the HUD root gets its Canvas and GraphicRaycaster switched off,
  on again when the player leaves; looked for only with `com.ShipConfig` installed and when the root's children change).
  Other code's lines: `KwShipPanel.Row(id)` gives a `KwPanelLine` (ask once, keep it): `Set(label, value, topic, tip)`
  or `Keep()` at least once a second while it should show, `SetBar(0..1)` a progress bar, `Hide()`; it hides 2 s
  (`KwPanelLine.Linger`) after its last set or keep. Lines show under the ship health in the order first asked for.

### The table's buttons

The row under the window's two sides is `KwBpStationActions.Buttons()`: one `ShelfButton(word, enabled, run)` each,
left to right (Save the dock's ship, Build on the dock, Clear the dock, Build ship). A new button is one more line
there, with its action as a method beside the others (`Act` gives it the local player and the table's position; `Dock`
finds the dock or says why not). The library makes the window 54 units taller for the row and checks each button's
`enabled` every frame the window shows (Build ship looks for its dock only twice a second, `KwBpCamera.Serves`).
Build on the dock builds the picked ship as the figures panel's three boxes shape it (`KwBpBuildBack.Shaped`: percent
longer amidships, percent wider, scale; refused with the part and its limits, or when it would reach past the dock).
The panel is a child of the library's window (`KW_ShipTable/Window`, found by name), which moves left to make room.

### The build camera

Build ship (the user's request, 2026-10-24: "When the user clicks build ship they go into free camera with a range of
30 m from the keel blocks, where they can build the ship."), on the BuildCamera library (`ValheimModLibs/BuildCamera`):

- Enabled while the table serves a dock, or while the camera is out (then it brings the view back). Pressed: the hammer
  is taken in hand from the inventory if it is not there (any tool whose piece table holds `KW_bpstation`; none: "You
  need a hammer to build the ship"), the window closes and the view flies free.
- Where: at most 30 m from the dock's keel line, the segment from its aft piece's origin to the end of its fore piece
  (`SegmentBounds`), so the whole dock is covered; swept against the ground and rock, held above the terrain and the
  water. It passes through pieces, so it can look inside the hull.
- Keys, the game's bindings: movement keys fly flat along the view, Jump up, Crouch down, Run three times as fast (10
  m/s, 30), mouse look turns it. Building, removing, repairing, the build menu and turning pieces work as usual; the
  build view's pan (`KwBuildView`) is off while it is out.
- Reach: the game's placement ghost, removal, copy and repair rays start at the camera; the library lifts the place
  distance for each of those calls when the hit is within 50 m of the camera (the whole ray), and puts the game's value
  back after the call. The kit's own clicks (`KwTools.Aim`, the ladder and keel tools) already cast from the game
  camera. `KwBpShipRemoval` reads the place distance inside `RemovePiece`, after the library's prefix (run first).
- Back: Build ship again (E at the table works from the camera when it looks at the table within reach of the body),
  Esc (only when it would open the game menu), death, a teleport, a seat camera, the game's free fly, the hammer put
  away ("The hammer is away"), or the body taking damage ("You were hurt"). The game camera's zoom is put back.
- The body stands at the table at rest (no walking, jumping, crouching, running, blocking or attacking); other players
  see it there. Local only, nothing on the server.

## Patched

`Player.TryPlacePiece` (prefix: the click tools; prefix: the table's dock check; prefix: a saved ship built,
`KwBpPlace`), `Player.UpdatePlacementGhost` (postfixes: the table's dock rule, hatch, windlass and rudder placement, previews, a
saved ship's ghost on the water), `Player.SetPlaceMode` (prefix: the saved ships into the hammer; postfix: the undo
keys' object), `Player.PlacePiece` (postfix: a ship piece from the hammer recorded for undo), `Player.Update` (prefix
and finalizer: no debug fly on Ctrl + Z),
`Player.FindClosestSnapPoints` (prefix: snap rules), `Player.RemovePiece` (prefix and postfix: Alt removes the whole
ship; prefix and postfix: what stands on a cabin goes with it, one undo step; prefix, low priority: a launched ship with
anyone aboard refused, `KwShipBreak`), `WearNTear.RPC_Remove` (prefix: the owner breaks up a launched ship into crates),
`Hud.UpdateCrosshair` (postfix: a launched ship's hover in hammer mode), `Hud.Awake` (postfix: the ship panel's
driver, `KwShipPanelHud`), `ObjectDB.Awake` (postfix: hammer), `ZNetScene.Awake` (BundlePrefabs' `OnSceneAwake`: pieces and the ship),
`ZNet.Awake` (postfixes: the table's window and its RPCs; the ship pins' calls), `ShipControlls.GetHoverText`
(postfixes: the ship's card; the ship's name first), `Terminal.InitTerminal` (postfix: `kwship`),
`Player.OnSpawned` (postfix: the dock waterline's drawer),
`WearNTear.ApplyDamage` (postfix: a hit holes a launched ship's hull part), `WearNTear.Repair` (postfix: one thing on a
launched ship mended a click, `KwShipRepair`), `ObjectDB.Awake` and `ObjectDB.CopyOtherDB` (postfixes: the repair kit's
recipe; BundlePrefabs' `ItemPrefabs`: the kit in the item list), `Player.UpdateWearNTearHover` (postfix: the hammer's repair finds a launched ship by its rig's
hit boxes, 12 m), `Projectile.DoAOE` and `Aoe.CheckHits` (prefix and finalizer: the
burst being made) and `WearNTear.Damage` (prefix: a heavy shot on a launched ship sent to its owner as `KW_ShipShot`
instead, skipping the game's; else a launched ship hit by a burst given the burst's place and the shot's way),
`Projectile.OnHit` (prefix at `Priority.First` and finalizer: a heavy shot striking a launched ship, its burst held back,
`KwShipShot`), `Ship.CustomFixedUpdate` (prefix: a launched ship's physics step), `Ship.UpdateSailSize` (prefix: skipped on a
launched ship), `ShipControlls.RPC_RequestRespons` (prefix: the helm taken on foot), `Player.SetControls` (postfix:
the helmsman walked to the tiller), `Character.UpdateRotation` (prefix: the helmsman facing the bow),
`Player.PieceRayTest` (postfix: a hit on a launched ship taken, for a piece that rides or is refused there), `Player.UpdatePlacementGhost` and `Player.PlacePiece`
(postfixes: the rules on a ghost aboard, the piece placed rides the ship), `Player.TryPlacePiece` (postfix: why a piece was
refused aboard, `KwShipRiderRefused`; why a cannon was refused off a ship, `KwArmPlaceMessage`), the ship cannons'
(`Cannons/CANNONS.md`, Identity: `Player.SetControls` prefix and postfix, the hauler's; `Player.UpdatePlacementGhost`
postfix, only on a launched ship's deck; `Projectile.OnHit` prefix and postfix, a ball's hit on structures and under
water; `ObjectDB.Awake` and `ObjectDB.CopyOtherDB` postfixes, the cannon items and recipes; and the MannedWeapons
library's `Player.SetControls`, `Player.SetMouseLook`, `Character.UpdateRotation`, `Projectile.OnHit`), `Player.FindHoverObject` (postfix: a rider
hovered, not its ship; postfix, low priority: the hole aimed at with nothing else hovered, `KwHullPatchHover`), `Piece.Awake` (postfix: a rider rides), `ZNetScene.Destroy(GameObject)` and
`ZNetScene.OnZDODestroyed` (prefixes) and `ZNetScene.RemoveObjects` (postfix): a ship's riders let go before its object goes,
`ZSyncAnimation.SetBool(string, bool)` (prefix: no empty pose name), `GameCamera.UpdateCamera` (prefix: the helm
view's zoom and the build view's; postfix: the eyes below decks, the steady height, the build view's pan),
`GameCamera.GetCameraPosition` (prefix, postfix and finalizer: the camera on a launched ship), `PlayerController.InInventoryEtc`,
`PlayerController.TakeInput`, `Player.CanRotatePiece` (the build view), `TextInput.IsVisible` and `InventoryGui.Update`
(the sizes window and the figures panel's boxes take the keys); BlueprintShelf, WindowInput and BuildCamera add their own (BuildCamera's:
`GameCamera.GetCameraPosition`, `GameCamera.UpdateListner`, `LightLod.GetLightReferencePoint`, `Player.SetMouseLook`,
`Player.SetControls`, `Player.Update`, `Player.PieceRayTest`, `Player.RemovePiece`, `Player.CopyPiece`,
`Player.UpdateWearNTearHover`, `Menu.Update`).

## Not checked yet

The bronze ship cannon and stone shot (2026-10-26) are compiled only, not yet seen in game: the bronze and fine wood
looks, the stone material on the ball, its icon and the shot in flight, the hammer showing it alone. The ship cannons (2026-10-26) were tried in game as the AssetLab Cannons bench (`AL_` names, built anywhere) and
checked offline there (`Cannons/CANNONS.md`); as Broadside's (`BS_` names, built only on a launched ship's deck, no body
of their own, the building rule refusing every other piece aboard) they are compiled only, not yet run in game.

Never run in game as its own mod (built 2026-10-24); the prototype's pieces were tested in AssetLab, most of them not
yet in game. The Build ship camera and the BuildCamera library (2026-10-24) are built and untested. The saved ships
in the hammer, their cost and launched ships came back from the prototype the same day, untested as Broadside: the
entries, the ghost on the water, the cost split between the build menu and the click, the ship afloat and sailing,
the helmsman at a tiller with the LimbIk library, hatches on a ship, and a dedicated server skipping the drawn parts.
Railings, ladders, masts and sails priced (2026-10-25), built and not yet seen in game. Undo and redo on the dock
(2026-10-25) built, not yet tried in game. The table's workshop model
(2026-10-25) is built in and not yet seen in game: its look in the workbench material, placing it, removing it with the
hammer and breaking it (`m_fragmentRoots` null, every wear state the model). The dock waterline (2026-10-25) is
checked offline only (its line against a heave and pitch run of `KwShipHydro`'s force law, its band against the
stations), not yet seen in game. Sailing (2026-10-25: the anchor windlass and mooring, the ship pins, cargo weight,
the tier perks and the helm's card) is built and checked offline only (the mooring law in a 2D ride, the most load of
the saved Medium and Pirate_ship hulls, the perk tables, the patch targets), not yet seen in game. The anchor led
overboard (2026-10-25) is checked offline only (the saved Pirate_ship with a windlass at five places, Medium at three
and the user's bubba at the place it was saved with and three more, inside its castle too: the lead by the mod's own code, then the
cable every 0.02 m and the anchor's mesh against every part's own mesh; renders from outside and from the deck), not
yet seen in game: the dock's re-plan as pieces are added, the fairlead's looks, the hover, a launched ship lying to a
fairlead off the bow. Its lead straight out from the drum and the rope coil (2026-10-25, the user's report) are
checked the same way on the user's live dock (read through DevBridge), Pirate_ship (eight places), Medium (four) and
bubba: the place chosen, no deck lead where a straight one is clear, the cable clear of every part and of the coil, the
let-go cable plumb past the keel, the coil 9 mm or more from the flanges and frame; not yet seen in game. Holes, flooding,
bailing and the hold's water (2026-10-25) are checked offline only (the part map, the damage rule, the hole text, the
flood level, sinking times and the roll with the water sloshing on the saved Medium and Pirate_ship hulls; the
compartments the same day: trim with end and midships holes, flooded evenly, list, slosh, sinking times), not yet
seen in game: the hole and water looks, its ends at the bulkheads, B in the hold, mending with the hammer. The mask at
the sea's real surface, the hold's water in the sea's material and the sea taking over a flooded or sinking ship
(2026-10-26) are checked offline only (`ov_SK_harness` in the session's scratchpad, PROBLEMS.md "A sinking ship looked
dry below the sea"), not yet seen in game. Real holes (2026-10-26) are checked offline only
(`ov_DH_harness` in the session's scratchpad, the user's bubba and live Pirate_ship): see-through, nothing else opened,
mending, the hit mapping and the time a hit takes, in PROBLEMS.md; not yet seen in game: the cut look, the boxes'
gaps against the game's projectiles, a hammer's mend aimed at a hole's edge. Hull boxes on the planking's face
(2026-10-26) are checked offline only (`ov_HC_harness`, PROBLEMS.md "Shots between two holes passed through"); not yet
seen in game: walking the holds and cabins, the camera inside, the physics' cost of the larger box count. Ships floating level (2026-10-25: the weight over the centre of buoyancy, slices
shaped at their middle) are checked offline only (heave and pitch runs of the saved Medium, Pirate_ship and shared
Medium_Ship hulls and two synthetic ones, both ways round), not yet seen in game. The
table's figures panel and building back longer, wider or scaled (2026-10-25) are checked offline only (the figures
and every reshape of the saved Medium, Pirate_ship and shared Medium_Ship blueprints and a hull laid keel end to keel
end: parts read again, limits refused, caps and rudders on their posts), not yet seen in game: the panel's place and
look beside the window, its boxes taking the keys, and a reshaped ship built on the dock. The Cabin tool's rework
(2026-10-25) is checked offline only (on the saved Pirate_ship: a midships cabin over two sections, the same drawn in,
a bow cabin the hull closes, a cabin on the flat stern's poop deck, a cabin on a cabin's roof, an old castle's text;
texts read back, refusals, edits, reshapes, costs, the railings each takes, the walls' boxes; the corner post rendered at
a cabin's front; the thicker posts of 2026-10-25 on the user's two stacked cabins: flush with the planking at both ends,
the ear over the raised side's end, the upper posts meeting the lower ring to ring, a shorter or drawn-in upper cabin, a
cabin inside the hull's flare; the fit of the user's report, 2026-10-25, on the user's stern cabin read live: left, right
and back insets of 0.3, 0.5 and 1 m, both sides, a cabin on a walkway cabin, midships, odd and unbuildable texts, the edit's
asked values, the old texts' meshes unchanged), not yet seen in game: the two clicks and the hover preview, the window, the railing taken and given
back by undo, the edit, the eyes under a low roof, the ship's boxes. Stacked cabins carried by an edit (2026-10-25) are
checked offline only (the user's two stacked stern cabins read live with their roof railings, and a third tier with a mast
and two ladders made on them: the lower raised and lowered, its left, right, front and back insets, a Shift edit, the
round trip back, a material-only edit, the removal set; every tier on its lower roof, nothing of one in the other, no wall
past the roof; rendered before and after), not yet seen in game: the preview of the moved stack and the hidden originals,
the parts replaced with two players, undo and redo of the edit and of a cabin removed with the hammer. The bowsprit and jibs, crow's
nests, fighting tops and pennants (2026-10-25) are checked offline only (mast texts old and new, the jib shear through
Unity's own quaternion maths, the jib cuts on many bowsprits, the foremast's pick, the platforms' meshes, the pennant's
shape, the pull and the costs), not yet seen in game: the sheared jib cloth and its bolt rope under a stretched parent,
the jibs' sheets tying on, the window's two pages, climbing the pegs (the boxes on `piece_nonsolid`), standing on a
launched ship's platforms and the pennant's colours (the bowsprit tool has its painted icon since the sail patterns).
Sail patterns (2026-10-25) are checked offline only (every sail and bowsprit text old and new, reshaped; the patterns
painted on the three cloths for square, triangle, gaff and a 55° jib, laid on the reefed cloths' geometry; a banner
from the game's own texture), not yet seen in game: reading the game's linen and banner textures back from the
graphics card, the colours under the game's light, the window's rows, the preview, the ghost and a player's picture. Editing parts (2026-10-25) is checked offline only (every kind's
values read from saved texts and written back unchanged, the windows filled from them, the changed-fields-only merge on
ribs, keels, decks, planking, ladders, masts and bowsprits, limits refused, the keel tool's open end against its body and
the deck tool's end against its middle), not yet seen in game: the windows on a part, the see-through preview of several
parts, the parts replaced with two players and on a dedicated server, a ward leaving parts out, undo and redo of an edit.
The painted
bands, the name board and the railing's shields (2026-10-25) are checked offline only (paint texts old and new, every
strake of the saved Pirate_ship's planking painted: faces out, 6 mm off the strake; the hooks along its railings; the
board's frame), not yet seen in game: the paint's colours over each tier's planks, the sign's text on the board (its
side and size), the shield's facing on its hook (read from the game's wall item stand, its +z taken as its front), and
hanging and taking shields on the dock and on a ship with two players. The level side strakes (2026-10-25) are checked
offline only (the saved Pirate_ship and Medium rendered before and after, their seams, a painted band, a castle's sides
and rays from inside flat sterns and the pirate ship's hold), not yet seen in game. The long planking textures and the
planks laid along the ship (2026-10-25) are checked offline only (the saved Pirate_ship and Medium: u at every shared
rib, in the blueprint's frame and laid out as on a dock far from the world's middle with headings off by a hair; the
painted bands' u on their strakes; side and bow quarter renders before and after), not yet seen in game: the textures
compressed on the card, the normal maps made from the reliefs, the paint looks read from the files again, a tier's
hitch when first worn, the cabins' walls on the long texture. The captain's
wheel and its shaft-and-gears steering gear (2026-10-25) are checked offline only (laid out by the mod's own code on
the user's ship, the saved bubba with its rudder on the stern keel's rudder point: one straight 23.3 m shaft, its wheel
at the place the live rudder had saved; the saved Pirate_ship with a wheel at ten places, bends round its main deck's
hatch and ladder, refused with the reason too low, off the hull and 36 m ahead; the Medium at three; the teeth meshing
within 0.3 mm at every helm, the texts read back; a cabin over the user's stern closing on the post with nothing of the
rudder inside it; renders from under the deck and from aft at helm 0 and full helm), not yet seen in game: the wheel's
look and hammer icon, its helm on the dock with the rudder and the shafts following it, the gear under a real deck,
the re-measure when a piece is placed or removed, the helmsman walked behind the wheel and his hands on its handles
on a launched ship, and two players. The wheel on a cabin's roof (2026-10-25) is checked offline only (the user's
live dock, two cabins stacked over the pointed stern: one straight climb through both up the stern's inside, a wheel on
either roof, off the centre line, at the back, refused at the roof's edge; a bulkhead back with its corner posts, a door
where the shaft went, doors and windows across it, a mast, a ladder and windlasses in the way, the back filled; every
gear vertex clear of the walls, hull, roofs, posts, frames and door leaves swung both ways at three helms, the teeth
meshing within 0.7 mm, the same gear in any piece order and on a launched ship, 653 deck wheels' texts unchanged; a
render inside the cabins), not yet seen in game: the ghost on a roof, the climb and brackets in the game's light, the
helmsman on a roof. One-click ladders,
railing post spacing and roof railings (2026-10-25) are checked offline only (a hatch's twenty spots in three sizes, the
0.25 m snap, the saved Pirate_ship's deck ends open or carried on, old and new railing texts with their posts and cost, the
railing edit, a roof railing on the Cabin tool's own roof outline with a ladder's gap, rendered once; the centring on the
saved bubba's castle with a doorway in its bulkhead, a second ladder, a window, a drawn-in left wall and a turned front
bulkhead: the spans, centre and quarter snaps, the refusals, no aim covering a doorway), not yet seen in
game: the aim's spot on an open hatch, the open-edge checks against real walls and floors, climbing an edge ladder, the
roof railing window and its preview, the gap and the ladder undone together. One-sided
railings and cabins taking only the side at the hull (2026-10-25) are checked offline only (the saved Pirate_ship's railing
texts old and one-sided, meshes, hooks, costs, reshapes, the edit's port and starboard, walkway cabins midships and on the
poop deck, a railing cut at a rib), not yet seen in game: the Sides row, the cut parts and their undo, a roof railing
going with its cabin. The Doorway and Window tools (2026-10-25) are built and compile, not yet seen in game: the
tools' aim rays and preview, partner doorways as one undo step, the leaves and E on the dock and on a moving ship (the
swing and its sound), flooding through an open port, walking through a cut doorway, and two players or a dedicated
server. Checked offline: round trips of 17 openings on the saved Pirate_ship and the closure ray check in PROBLEMS.md.
Their one-click rework (2026-10-25) is checked offline on the saved Pirate_ship (the type cycle, a gunport's sill and
spot, equal spacing from the type's, the last two placed and the nearest's neighbour, Shift, refusals at a rib and a deck,
the E fill along the port run with the ribs skipped, door widths and heights, an open doorway 3 m wide, centring beside a
door and a ladder, the doorway window's rows), not yet seen in game: the real ghost (its
materials, the part's own model hidden and shown again, its cost on a long hull), the wheel through `KwToolNotches` in
the build camera, the placement message. Rows on Shift and the row edit (2026-10-25) are checked offline on the saved
Pirate_ship and the user's bubba (rows of 1 to 5 gunports along each port side, moved off the ribs, and of doors, windows
and portholes on the cabins' walls: even spread, centring, as many as fit, the stretch aimed at beside a door, gaps through
a deck, the ghost's openings the ones built; a row's members, laid out again with another number, a wider type or filled
in) and the edit window's rows by shape, each row's change seen in the preview's meshes (a square made a rectangle on a
low cabin wall is refused, said in the note); not yet seen in game: Shift's row ghost on a long hull (its rebuild cost),
the wheel with Shift held, the Number row's window rebuilt, the leaves hung part open.
The openings' plank wood look, their true size on a curving hull, frames clear of the wall and leaves turning about
their pivots (2026-10-25) are checked offline only (PROBLEMS.md, "Wall through the frames, windows stretched on a
curving hull"), not yet seen in game: the plank wood under the game's light, a leaf's swing on its pivot (a door's hinge
knuckles ride with the door) and a launched ship's openings.
Railings back in the plank wood (2026-10-25) are checked offline (the user's hull and roof railings rendered in both
looks); the parts hidden while an edit's window is open, its real-look preview, and the railing tool's click on a railed
roof or on the hull under a railing (the stretch and side check run offline on the user's railing) are built and
compile, not yet seen in game. The Post cap tool (2026-10-25) is checked offline only (each style on the standard, the
saved bubba and Pirate_ship, a thin and a wide post; lengths 0.5, 1 and 2; texts, edits, cost, scaling; the
Pirate_ship's old dragon read as its new form; a render), not yet seen in game: the wheel's steps in the hammer and the
build camera, the solid ghost, the edit window on old and new caps, two players. The carved heads, lean and facing
(2026-10-25) are checked offline only (the bundle's own meshes read with UnityPy, warped by the mod's code on the standard,
bubba, Pirate_ship, Medium and a wide post: feet flush, lengths 0.5 to 2 moving the head whole, leans of ±35° turning the
head and its normals whole with the warp never folding, inboard turned half round; the scroll's and tail's curls moved
whole; texts old and new, the window's rows, Shift's changed keys, cost, scaling; a render with the tier tints), not yet
seen in game: the bundle loading, the tinted piece material and its normal map under the game's light, Ctrl + wheel in
the hammer and the build camera, a launched ship's heads. At length 0.5 with the full lean the long edge strips of a
mane or ridge down the neck's back shade up to about 30° off. The tool switch (2026-10-25) is checked
offline only (every prefab's own tool, the choice, the hints and panel lines; each tool against each part kind of the saved
bubba, Pirate_ship and Medium with the tools' rules as data), not yet seen in game: the hammer's tab, selection, ghost and
panel after a switch, the hint line, the sail and bowsprit windows opened from another tool. Breaking up a launched
ship (2026-10-26) is checked offline only (the saved bubba and Pirate_ship: the cost from the packed blueprint equal to
the paid one, every item in stacks within the game's sizes, 9 and 5 crates of the game's 2 by 2 `CargoCrate`, read from
the running game, laid along the hull 3.4 and 3.1 m apart, 40 crates in rows 1.4 m apart; the aboard check at ten
places, the onboard trigger as sized; the patch targets), not yet seen in game: the hover line and its key name, the
crates at the water's surface and on land, the wood breaking along the hull, the refusal, two players. Sails by kind
(2026-10-26) are checked offline only (each kind's table; the saved bubba, Pirate_ship and Medium's steady speeds at every
angle to the true wind, against the game's push, in full and half wind: square sails nothing within 60°, every run no
faster than before, 93 to 96% of it; each kind alone on the pirate's hull; the launched ship's step run in the plane,
its speed and leeway; the boom's and a jib's turns, the jib's luff kept on its stay; a render of the pirate's gaff swung
on a beam reach and close-hauled), not yet seen in game: the feel at the helm, the boom, lacing and sheet on a moving
ship, a gybe, the jibs' clews rising as they are sheeted out, the figures panel's smaller words on a long body, the card's
wind line, two players. Pieces built on a launched ship (2026-10-26) are checked offline only (`ov_SP_harness` in the
session's scratchpad: the pose maths round trip over 20,000 random ships and pieces, a piece kept on its deck point under
a ship sailing, turning, heeling and pitching for 10 s, a hit on the interpolated ship carried to where it is drawn, the
write thresholds, which pieces ride, the release rule; the patch targets of both mods; the game's ships' bodies read
from its bundle: `Interpolate`), not yet seen in game: the ghost on a moving deck, a piece placed under way, players
standing on riders, a chest opened and a cannon manned at sea, the ship reloaded and the world restarted with riders,
two players and a dedicated server, breaking up and sinking with riders aboard. Rig damage (2026-10-26) is checked
offline only (`ov_DMB_harness` in the session's scratchpad, 71 checks on the saved bubba and Pirate_ship: the rig read,
the marks' text, the section maths, lucky shots and how many a mast and a cap take, fastenings and droop on the cloth's
own points, holes' texture places against the cloth's UVs and the cut, the drive whole equal to the sail mix and less as
damaged, the rudder's blade left and its mesh cut, the look plan against the mod's own rig mesh, notched rings' areas,
mending and putting back, the fall's turn; renders `out/DMB_droop.png` and `out/DMB_fall.png` there), not yet seen in
game: the colliders against real cannonballs, the hole textures read back from the cloth, the droop in the wind, the
fall, its sounds and the ropes' whip, the cut looks in the ship materials, the hammer's hover and repair on the rig, a
reloaded ship with a mast down, two players and a dedicated server. The Wooden Repair Kit and the hammer's step-by-step
repair (2026-10-26) are checked offline only (`ov_RK_harness` in the session's scratchpad, on the saved bubba and
Pirate_ship rebuilt as launched ships: holes on planking above and below the waterline, a deck and a cabin's wall made by
the owner's rule; every ZDO text round trip; each hole aimed at through its middle from outside and inside and at its
rim, not past the margin or along the face; the holes below the waterline patched let in nothing in 20 s against 0.72 m³
unpatched, one patch stopping just its own hole's water; a patched hole widened by a hit losing its patch; the plan in
order and 14 and 12 clicks mending everything one thing each, the patches falling first, the fallen mast back for 20
wood and 62 deer hide, and 19 wood and 40 scale hide; the generated boards wound outward and covering every hole's
ragged outline from 0.12 to 0.45 m), not yet seen in game: the kit, its recipe and icon, the hover and E, the patch's
look and sounds, the patch falling, the regrowth, the bundle's model once embedded, two players and a dedicated server.
Ladders on a launched ship (2026-10-26) are checked offline only (`ov_LAD_harness` in the session's scratchpad: every
dock ladder of bubba and Pirate_ship planned again from the blueprint at the same top and length, the hatch ladders
exactly; a hatch and an edge ladder added, lengthened and removed through the packed blueprint on bubba, Pirate_ship and
Medium, a second peer's blueprint the same, only the ladder (and a roof railing's gap) redrawn, its foot on the floor, the
cost and refund netting to nothing; the ladder over the side moving off a ladder added where it hung), not yet seen in
game: the see-through ghost riding the ship, the hover's words, the length window and its Remove, the hammer's remove,
climbing a ladder added at sea, two players and a dedicated server.
Masts, bowsprits and post caps on a launched ship (2026-10-26) are checked offline only (`ov_RIG_harness` in the session's
scratchpad, 92 checks on the saved bubba, Pirate_ship and medium: a mast with a sail added on a deck and removed, a course
re-specced, the bowsprit shortened, lengthened, turned up and turned inboard, a cap removed and put back, Pirate_ship's old
dragon cap built again as the tool's, all through the edit's package and the packed blueprint; the second mast's notches
and hole kept on it, and the hammer's repair finding them, after the first is removed; a fallen foremast refused a change
and removed for only its jib; the sails' pull, sails by kind and jib power following; cost and refund netting to nothing; a
second peer's blueprint and every mast's rope ties the same; a removed mast's tombstone reading as no piece to the ties,
cost, sails and rig; 0.4 to 5 ms of plain work a mast edit, most of it tying its ropes), not yet seen in game: the windows
and ghosts on a moving ship,
the rig remade (fittings, looks, hit boxes, platforms) and the other masts re-tied a frame apart, the hammer's remove on a
mast, two players and a dedicated server.
Sea edits' core and railings at sea (2026-10-26) checked offline only (`ov_SEA_harness` in the session's scratchpad, 93
checks on bubba, Pirate_ship and medium: the railing a click on the planking would add is the one the dock laid there,
roof sites match their railings, the edit's package, an owner and a second peer through three railing edits with every
mark, shield and cost checked, a break-up after them, the unpacking keeping every index; an owner's railing edit 1 to 5 ms
of plain work on bubba, a peer's 0.3 to 0.6 ms, the ladder over the side 11 to 27 ms a frame later); not yet seen in game:
the hover ghost and windows on a moving ship, a railing's look, boxes and shield hooks redrawn, the shadow made again, the
hammer's remove and its near miss, two players and a dedicated server.
Doors and windows on a launched ship (2026-10-26) are checked offline only (`ov_OPN_harness` in the session's scratchpad,
277 checks on bubba, Pirate_ship and medium: aimed from outside in the ship's frame, a gunport, a Shift row of portholes,
a door in a cabin's wall or the planking and a doorway through a cabin's raised side placed, edited, Shift-edited and
filled in through the edit's package, the owner's check (moved, reshaped or removed parts refused) and a second peer
numbering and flooding the same; rays through new openings pass the part's boxes, through filled-in ones are stopped;
states kept with their openings; holes in an opening's place dropped with their patches, the others kept; the cost the
frames', covers' and doors'; plain work a part 0.5 to 11 ms of boxes, 3 to 36 ms of look data in .NET 8 (more with more
openings on the part), 1 to 8 ms numbering the openings); not yet seen in game: the parts drawn alone and their ghost
hiding one, a part's look and boxes replaced and its holes cut again, the leaves renumbered, the hammer's remove on a
window, two players and a dedicated server.
No `thunderstore/icon.png` yet, and no wiki pages. The words are English in the code: the Translations library is not
used yet.

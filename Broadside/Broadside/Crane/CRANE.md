# Cargo crane

A land-built cargo crane and the crates it lifts (asked for 2026-10-25: players need a way to unload cargo from
Broadside ships without carrying every item off). Made as an Asset Lab bench and put into Broadside on 2026-11-02 (the
user's words: "put the crates you just created into broadside, and the crane too"), unchanged but for its registration
(`NetPrefabs.Register`) and its install in `Broadside.cs`. The same day the user, finding a "Large slatted crate" on a
launched ship's deck would not hook, decided the crane lifts every Broadside storage crate, in a rope sling ("When you
hook onto a cargo crate, rope should appear around the crate with a like loop on top that the crane hook is hooked
into"), and lifts them off ships and onto them. Broadside's `CLAUDE.md` rules hold (methods at most 24 lines, classes at
most 300, prefix/postfix only, multiplayer first, no settings, English words in the code, `KwCraneText`). Folder
`Broadside/Broadside/Crane/`; its bundles `kw_cargocrane` and `kw_cargocrate` are embedded from `Broadside/assets/bundles`.

## Identity

The prefabs start with `KWC_`, not `KW_`: Broadside treats every `KW_` piece as a ship part (blueprints, snap rules,
undo, the dock waterline), and these are land pieces.

| What | Names |
| --- | --- |
| Pieces (hammer, Misc tab, at a workbench) | `KWC_cargo_crane` (30 wood, 10 stone, 4 deer hide, 6 leather scraps; a copy of `piece_workbench` without its crafting station), `KWC_cargo_crate` (10 wood, 4 leather scraps; a copy of `piece_chest_wood`, 6 x 4) |
| Crates it lifts | `KWC_cargo_crate` and the four storage crates `BS_crate_small`, `BS_crate_large`, `BS_crate_slat_small`, `BS_crate_slat_large` (`Storage/`, when their bundles are embedded): each has `KwCrate`, `KwCrateLook` and `KwCrateLid`. No other chest, nor the ammunition piles |
| Bundles (ValheimAssets) | `kw_cargocrane` (`Assets/Ships/Keelwright/CargoCrane`), `kw_cargocrate` (`Assets/Ships/Keelwright/CargoCrate`), embedded by `LabBundle` lines |
| Crane model contract | parts `turn` (turret, about Y), `boom` (slides along the turret's +Z, 0 to 8.5 m: the hook 3.5 to 12 m out from the mast; 4.5 m until 2026-11-02, when the user asked for 50% more reach), `drum` (windlass, spins about X; winding in, the head going up, is +X), `head` (hangs under the tip); points `operator`, `mast_pulley`, `tip`, `drum_rope`, `hook` (the bowl's top, where a rope rests; the hook lies in the turret's Y-Z plane); boxes `col_box_base`, `turn/col_box_mast`. The works (2026-11-03, the workshop's `HANDOFF.md` has pivots and ratios): controls `ctl_slew`, `ctl_boom`, `ctl_hook` (levers and latch, about local X, pushed forward +X, clear to ±30°), `ctl_hoist` (the crank, about X), gears `gear_drum`, `gear_crank` (about X), `gear_slew` (about Y), `gear_boom` (about X); points `grip_slew`, `grip_boom`, `grip_hoist`, `grip_hook` (where the fists go). `KwCraneRig.Arrange` hangs boom, drum, the works and the points under `turn`, `tip` under `boom`, `hook` under `head`, each grip under its control (all keeping their world pose) |
| Crate model contract | box `col_box_body` (needed: the crate's box, its sling, its hooking area); part `lid` (opens `Euler(-100, 0, 0)` about its hinge); point `lift` (optional: where the hook holds it; without it, over the middle of the box's top, `KwCrateSling.LiftOver`). The cargo crate's `sling_0`..`sling_3` corner loops are model only: no code reads them since the sling came (2026-11-02) |
| Crane ZDO (its owner writes) | `kwc_turn`, `kwc_slide`, `kwc_drop` (the pose), `kwc_operator` (the operator's character), `kwc_crate` (the crate on the hook) |
| Crate ZDO (its owner writes) | `kwc_crane` (the crane it hangs from), `kwc_hang_yaw` (its turn against the turret), `kwc_afloat`; aboard a launched ship the rider's own `kw_rider`, `kw_rider_pos`, `kw_rider_rot` (`Ships/KwShipRider`); on the game's cart `kwc_cart` (the cart's own id; 0 when on none), `kwc_cart_slot` (its bed cells, bits), `kwc_cart_yaw` (its turn on the bed); `kwc_break` (falling to break where its ZDO has it); its items are the game chest's own `items` |
| Cart ZDO (the game's `Cart`, its owner writes once) | `kwc_cart_uid` (the cart's own id, a random long; the game renumbers ZDOs each time the world loads) |
| Drop ZDO (a stack spilled from a broken crate, written by the crate's owner) | `kwc_heap` (the drop shows its stack as a heap) |
| RPCs | on the crane: `KWC_Operate` (take or leave), `KWC_OperateReply`, `KWC_Hooked` (the crate's answer); on the crate: `KWC_Hook`, `KWC_Release`, `KWC_Unload` (to its owner: off its cart and down at a spot beside it, `Vector3` and turn) |
| Patches | `Player.SetControls` (prefix: the crane takes Jump, Crouch, Attack, Block while worked), `Container.Interact` (prefix: a hooked or breaking crate is shut; a crate on a cart unloaded with the alternate use; an empty cart's storage hands the use to the cart), `Container.RPC_RequestOpen`, `RPC_RequestStack`, `RPC_RequestTakeAll` (prefixes: a hooked or breaking crate is shut), `Container.GetHoverText` (postfix: a crate on a cart's unload line; the cart's storage), `Piece.CanBeRemoved` (postfix: nor removed), `Vagon.Awake` (postfix: the cart's bed), `Vagon.UpdateMass` (postfix: its crates' contents in its mass), `ZNetScene.Destroy(GameObject)`, `ZNetScene.OnZDODestroyed` (prefixes) and `ZNetScene.RemoveObjects` (postfix: a cart's crates let go first), `ItemDrop.Awake` (postfix: a spilled stack's heap), `Player.FindHoverObject` (through `KwShipRiderHover`'s postfix: a crate on a cart hovered), `Inventory.AddItem`, `CanAddItem`, `InventoryGrid.DropItem` (through `KwPileFilter`'s prefixes: nothing into a cart's storage), `ObjectDB.Awake` (postfix: the hammer); the pieces on `ZNetScene.Awake` through BundlePrefabs |

## How it plays

- E at the crane works it like a ship's helm, one operator at a time; the operator stands on the turret and turns with
  it. Game bindings: Left/Right turn (30 degrees a second), Forward/Backward slide the boom out and in (1.2 m/s), Jump
  raises and Crouch lowers the head (1.5 m/s, 0.6 to 12 m under the tip), Run doubles them, Attack picks up, Block sets
  down, Use (E) leaves. While working it, a box at the bottom of the screen lists these keys (`KwCraneKeys`: the game's
  own boxed tooltip, under the HUD, centred, its bottom a tenth of the screen up).
- Pick up: with the hook lowered onto a crate's top, anywhere over it (its footprint and 0.25 m round it; the top from
  0.45 m over the hook to 1.1 m under it; of several, the top nearest the hook's height, then the nearest middle), the
  crate is hooked, the head draws its sling tight and lifts it 0.3 m. Normal chests and the ammunition piles are never
  picked up ("Lower the hook onto a crate").
- The sling (`KwCrateSling`), on every screen while the crate hangs and only then: two rope bands round its body like a
  parcel's string, one along it and one across, each under its bottom (the one across 3.5 cm under the one along, so
  they cross) and up two opposite faces, lying on them over whatever stands out within 6 cm of the band (a latch, the
  lid's lip, a rope handle, a lashing: the taut path over the model's own triangles, at most 12 cm past the box,
  `KwCrateSlingFit`, measured once per crate prefab); from the four top edges, legs up to an eye (7 cm radius to the
  rope's middle) whose top rests in the hook's bowl, standing across the hook's plane so the hook threads it. The eye's
  bottom stands over the top 0.9 times the crate's longer half side (0.3 to 1 m) on a storage crate (0.63 m on the large
  ones, 0.41 on the small, 0.56 and 0.34 slatted), 0.68 m on the cargo crate (its model's `lift`). Rope 3.5 cm thick, the
  game's (the longship's rope material, as the crane's own rope).
- Set down: the crate goes straight down onto what is under it (ground, floor, a dock, a deck, another piece), or
  floats where that would be under water (its bottom 0.35 m under the calm surface, riding the waves on every screen).
  A floating crate can be hooked again. Lowering stops where the hook, or the crate's bottom, meets something.
- Ships: a crate riding a launched Broadside ship (built on its deck, or set down there) can be hooked; it leaves the
  ship and hangs as any crate. Set down on a launched Broadside ship (its deck, cabins, or a piece riding it), it rides
  that ship as a crate built there does (`KwShipRider`): at that place on the deck as drawn, turned with the deck,
  keeping its heading. On a dock, land or a floor it is an ordinary piece. Over the game's own ships (or a Broadside
  ship that has no id yet), which carry no crates, it goes down beside the ship, clear of its hull: set on the deck it
  stood fixed in the world inside the hull and held the ship dead (a player's report, 2026-11-04). While it hangs its
  boxes are triggers, so a hanging crate pushes no ship or player. The cargo crate rides the same way, though the hammer does not build it aboard (the rider rule
  for building is unchanged).
- A hooked crate cannot be opened, stacked into, emptied or removed with the hammer.
- A free crate (cargo or storage) with nothing left under it comes down on its own (below), so a storage crate no longer
  wears away for want of support (`m_noSupportWear` off, as the cargo crate's).
- Dropped from too high (2026-11-03, the user's words: "if a crate is dropped from a crane from higher than 2 meters,
  then it breaks on impact and the contents go all over. Stackable items should show as stacks"; `KwCrateBreak`): let go
  from a crane (Block, the operator leaving, the crane gone, the crate not named for three seconds) with its bottom more
  than 2 m over where it comes to rest (the ground, a floor, a deck, a cart's bed), it falls there under gravity on every
  screen and breaks as it lands: each inventory slot dropped as one pickup with its whole stack (the game's
  `ItemDrop.DropItem`: quality, durability, variant, crafter and custom data kept) within 1.5 m of the landing, on the
  surface there, in sight of it (not through a wall), clear of anything solid, floating where that is water; then the
  crate is broken the game's way (its destroyed effect, sound and fragments, its build resources dropped as any broken
  piece's). 2 m or less sets it down as before. Onto water it never breaks: it floats, from any height. While it falls
  nobody opens, hooks or removes it ("The crate is falling"). Only the crane's let-go breaks a crate: one settling after
  the floor under it went, unloaded off a cart or dropped by a cart that went, never does.
- A stack spilled from a broken crate shows as a stack (`KwCrateHeap`): the drop wears copies of its own model beside and
  on it, one copy for 2 or 3 items, two for 4 to 7, three for 8 to 15, four for 16 or more, laid across the model's
  narrow way and then on top, each turned a few degrees by the drop's id; the heap shrinks as the stack is picked up.
  Cosmetic: no colliders, nothing networked; every other drop in the world is left as the game draws it.
- Carts (2026-11-03, the user's words: "we also need to allow lowering a crate with the crane onto a cart, and the cart
  accepts the crate as cargo ... The cart wouldn't have storage anymore, just crates. Players need to be able to drop
  crates off from the cart, or use the crane again to remove them"; the user decided to change the game's own `Cart`;
  `Cart/`): a crate is lowered onto the cart's load space (the hook stops on it, never inside the bed) and, set down,
  drops into a place on the bed, squared to the cart, and rides it as it is pulled, on every screen. The bed (measured
  from the game's cart: 1.41 m across, 1.94 m along between the walls, the floor 0.52 m up) is four cells, two across and
  two along; a crate takes the smallest of a cell, a row across, a column along or the whole bed that its box fits in
  (2 cm to spare), turned square to the cart the way nearest how it hung, at the free place nearest where it hung. What
  fits: a small storage crate (0.90 x 0.62 m) or small slatted crate (0.75 x 0.60) a cell, along the cart; a large
  storage crate or the cargo crate (1.39 x 0.91) or a large slatted crate (1.25 x 0.95) a row, across the cart: four
  small, two large, or one large and two small. Block over a cart with no room says "The cart has no room for this crate"
  (or "This crate is too big for the cart", "The cart is not ready yet") and keeps it on the hook; let go any other way
  over such a cart, the crate goes down beside it (as unloading does), or as a last resort straight under it, the cart
  pushed aside.
- A crate on the cart: E opens it and items go in and out as with any crate (the game's chest, its items in its own ZDO);
  the game's alternate use, Shift+E (a gamepad's alternate keys with use on the newer layouts), unloads it: it slides off
  to the right, left or back of the cart (never the front, where it is pulled), the side nearest the player first, 0.3 m
  clear of the walls, keeping its turn, onto the ground, a floor or a dock at most 2 m under the bed, or afloat on water;
  never onto another cart or a ship, nor where anything stands in its way there or between ("No room beside the cart to
  set the crate down"). The crane hooks it off the cart as off the ground. The hammer removes it as any crate (not while
  hooked or falling); it leaves the cart. A cart destroyed or removed lets its crates go where they are and they come
  down; a cart floats on water as the game's does, its crates with it. Its mass, every 5 s as the game sets it, counts
  what its crates hold at the game's factor (base 50 plus a tenth of the weight), as it counted its storage.
- The cart's own storage takes nothing in any more, by any route (drag, shift-click, stack all, another mod's quick
  stack; "The cart carries crates now: nothing more goes in here"). A cart that holds goods keeps them: E opens it to take
  them out, its hover saying so. Once empty, E on its load space takes hold of the cart, as on its frame, and the hover is
  the cart's with "Carries crates: lower them onto it with a cargo crane". Nothing a player owns is deleted.

## The works and the operator's hands

The user (2026-11-03): "we need the crane to be cooler. We need like gears, poles, ropes, and controls, so when you do
different things to the crane your character accurately is grabbing and moving controls around to control the crane."
The workshop model got a console with two levers and a hook latch, a hoist crank driving the drum through a wheel and a
pinion, a slew pinion rolling round a toothed hoop on the footing, a pinion over a rack on the boom, an A-frame, rails and
guy ropes. Everything moving is drawn on a screen from what every peer already has (the pose `kwc_turn`, `kwc_slide`,
`kwc_drop` as the screen eases toward it, `kwc_crate`, `kwc_operator`): nothing new is sent, and a dedicated server
neither turns the works nor poses anyone (`KwCraneHands` is added only where the game draws; within 50 m of the camera).

- The gears (`KwCraneWorks`), angles from rest about each part's own axis: the drum turns with the rope paid out
  (`KwCraneRig.DrumAngle`: the slide plus the drop over its 0.15 m radius, winding in +X; it turned the other way until
  2026-11-03, against the model's rope, which leaves over the drum's front), `gear_drum` (the drum's 7-tooth iron
  pinion) with it, `gear_crank` (the crank's 21-tooth wooden wheel) and the crank `ctl_hoist` a third of it the other way
  (-7/21), `gear_slew` +5 times the turret's turn (40:8 against the hoop, measured on the turret), `gear_boom` -286.48°
  per metre the boom slides out (its 0.20 m pinion rolling on the rack). The hoist is a step-up since 2026-11-03 (the
  user: "my guy doesn't have to turn extremely fast; ... there is a gear there that is turning extremely fast"; it was
  a 7-tooth pinion on the crank driving a 21-tooth wheel on the drum, the crank at -3 times the drum, a blur under his
  hand): at the hoist's 1.5 m/s the crank turns about 0.53 times a second (1.06 with Run) while the drum's pinion whirls
  at 1.6 (3.2). The drum also turns while only the boom slides (the rope between the pulley and the tip lengthens), so
  the crank turns with it then (0.42 a second at the boom's 1.2 m/s).
- The levers lean from how the pose changes, eased (about a tenth of a second), up to 20° at full speed: `ctl_slew`
  pushed forward while the turret turns right (the Right key) and pulled back while it turns left, `ctl_boom` forward
  while the boom slides out (Forward) and back while it comes in; upright when it stops. A step too big for one frame
  (a pose taken at once, the sling drawn tight) counts as still.
- The hook latch `ctl_hook` flicks when `kwc_crate` changes: forward when a crate is asked or hooked, back when it is
  let go (or refused). 0.2 s for the hand to reach it, over in 0.15 s, held 0.15 s, back in 0.15 s (25°).
- The operator (`KwCraneHands`, `KwCraneHand`): the one `kwc_operator` names, looked up four times a second (on his own
  game, himself while he works it), stands where the crane already puts him (its `operator` point, facing along the
  boom; his feet unchanged). By two-bone IK (the helmsman's `KwHelmPose` and `KwHelmArm`, the LimbIk library; elbows out
  to his sides, back and down) his left fist holds `grip_slew` and his right `grip_boom`, each moving with its lever.
  While the head goes up or down (the drop changing faster than 0.05 m/s, and 0.35 s after), the left fist moves over to
  `grip_hoist` and follows the crank's handle round; while the latch is worked, the right fist goes to `grip_hook`,
  rides the flick and comes back. Every move over takes 0.2 s, worked out in the turret's frame, lifting 6 cm on the way.
  His upper body leans over the console, eased: 8° at the levers, 15° at the latch, 45° bowed over the crank (its
  handle circles 0.75 to 1.05 m over his feet, at the bottom still a few centimetres past his reach). His hands take
  hold over 0.2 s once he stands within 0.3 m of the point and let go as he leaves.

## Multiplayer

- E asks the crane's owner, who allows one operator (a gone one counts as none), names them in the ZDO and hands the
  crane's ownership to them, the way a chest is handed to whoever opens it. The operator's game moves the crane and
  writes its pose ten times a second; every other screen eases toward the ZDO's pose. The owner frees the crane from an
  operator gone three checks running.
- A crate hangs only while both name each other: the crane its crate (the crane's owner writes it first, then calls the
  crate), the crate its crane (the crate's owner writes it, unless the crate is open or on a crane still holding it).
  Every screen draws it under that crane's hook from the crane's pose, in its sling; the crate's owner keeps the crate's
  ZDO place up with it, so it loads where it is. The sling is drawn from the hanging state alone (nothing sent; nothing
  drawn on a dedicated server, which only places the crate for its casts).
- Letting go: Block, the operator leaving (E, a jump out, death, logging out), the crane changing owner, or the crane
  being destroyed. The crane forgets the crate and calls it; the crate's owner sets it down under where it hung. The
  crate's owner also sets it down by itself when the crane has not named it for three seconds (the crane let go, was
  destroyed, or never got the answer), and a free crate with nothing left under it settles again every two seconds
  (never one riding a ship, afloat, or where the area round it is not loaded yet).
- The ship hand-off is the rider's own record in the crate's ZDO, so every peer and a player joining later agree.
  Hooked: the crate's owner releases its rider (`KwShipRider.Release`: the record cleared, the crate no longer the ship's
  child) and names the crane in the same ZDO write, so no peer ever sees it ride and hang at once; every other peer, seeing
  the ZDO change, releases its own rider at once (`KwCrateLook`), before drawing it under the hook (its rider's own check
  would within a second). Set down on a launched ship: the crate's owner finds the ship from the collider its box comes
  down on (its body's `KwShipRiders`, the ship given its own id), puts the crate there as the ship is drawn and writes the
  rider's record (`KwShipRider.Place`, `KwCrateRest.Board`); every other peer, seeing the record with no rider, adds one
  (`KwCrateLook`), and the rider boards the ship there; a later joiner's rider comes from the record as the piece wakes
  (`KwShipRiderWake`). While it rides, neither its settle check (`KwCrate.Settle`) nor its free drawing
  (`KwCrateLook.Free`) runs: the rider alone keeps its place, on the deck or parked while the ship is away, and its
  owner's upkeep of the ZDO's place is never fought. A rider let go for good (its ship destroyed, or missing a minute)
  is a free crate again: it settles, falling or floating.
- No items are ever moved but by a break: the crate is the game's chest with its items in its own ZDO, never destroyed or
  rebuilt by the crane, so a lift can neither lose nor double anything.
- A break is the crate's owner's alone: at the let-go it measures the fall to where it comes to rest and, over 2 m, writes
  the landing and `kwc_break` in one ZDO write; every screen draws the fall from that (`KwCrateLook`). On the owner, once
  its drawn crate has landed (or the fall's time and half a second more have passed), once (its own flag, and a crate
  already broken or removed has nothing left to break it): the crate's colliders off here, every slot dropped as a
  networked drop (`kwc_heap` set on a stack of more than one), the inventory emptied and saved, and only then the game's
  `WearNTear.Remove` (its effects and fragments sent to every peer the game's way, the ZDO destroyed). An owner gone
  mid-fall: the new owner's check sees `kwc_break` with nothing armed and breaks it where its ZDO has it. Every peer
  draws a marked drop's heap from the drop's own synced stack (the game's item data in its ZDO); nothing else is sent.
- Carts: the link is the crate's own record in its ZDO (`kwc_cart`, the cart's own id `kwc_cart_uid`, which the cart's
  owner writes once; `kwc_cart_slot`, `kwc_cart_yaw`), so every peer and a player joining later agree. Set down over a
  cart: the crate's owner finds the cart from the collider its box comes down on (the body's `KwCartBed`), chooses the
  place from the crates its game sees on that cart, and writes the place in the world, the link and the crane's name
  cleared in one ZDO write (`KwCartLoad.Board`); every peer, seeing the link, adds the crate's rider (`KwCartRider.Agree`
  from `KwCrateLook`), which makes it the cart's child at its place, eased there from where it was drawn (falling from the
  hook). The cart's owner moves the cart (the game's own physics and `ZSyncTransform`), and each screen draws the crates
  on it as its children. The crate's owner keeps the crate's ZDO place with the cart (at once when the cart stands still,
  every 8 m while it is pulled: each write sends the crate's whole ZDO), so it loads where it is and with the cart's area.
  Hooked off a cart or unloaded: the link cleared in the same write as the crane's name or the spot, every peer letting
  go of its rider at once. Unloading is the one call: the asking game finds the spot and sends it to the crate's owner
  (`KWC_Unload`), who checks the crate is still on a cart, not open and not hooked, and writes it; nothing comes back
  (refusals are said on the asking game before it asks). Two crates set down on one place at the same moment by two
  games: the later one (by ZDO id) goes down beside the cart, by its owner. The cart's crates are let go before the game
  destroys its object (`ZNetScene.Destroy`, `OnZDODestroyed`, `RemoveObjects`), parked where they are; a cart destroyed,
  or no ZDO of it near for 30 s with the area loaded, has each crate's owner clear the link and the crate settles where it
  is. The cart's mass is its owner's, from the crates' inventories as that game has them. A dedicated server only keeps
  the ZDOs and places the crates.

## Left for later

- A hanging crate passes through walls when swung sideways (only lowering stops at what is under it).
- A crate set down on a launched ship from high up appears on the deck at once on every screen (only a crate set down
  elsewhere falls under gravity).
- A crate stays on a cart that tips over (it is the cart's child until unloaded or hooked).

## Not checked yet

Built 2026-10-25 and checked offline only (compiled with no warnings; every patch target and parameter checked against
the game's DLL). Not yet seen in game: the models, materials and icons, working the crane (keys, the operator on the
turret, the rope), hooking and setting down, floating, two players and a dedicated server, the crane removed mid-lift.
The storage crates on the hook, the sling and the ship hand-off (2026-11-02) are compiled only: the sling's look on all
five crates (the bands clear of the latch, lid and handles, the eye threading the hook), hooking a large crate near its
edge, hooking a crate off a launched ship's deck and setting one down on a deck (the rider taking it, on a second
player's screen too, and for a player joining after), sailing off with it, setting one on a vanilla ship, and the
settle check leaving built storage crates alone in a base (on floors, beside walls, on slopes).

Carts, breaking and heaps (2026-11-03) are compiled only (no warnings); the cart prefab's colliders, its storage box,
Vagon's mass numbers and the crates' boxes were read from the running game (DevBridge, read only), the game's Vagon,
Container, ItemDrop, WearNTear, Inventory and InventoryGrid decompiled for the patch targets. Not yet seen in game: the bed
measured at runtime against the numbers above, lowering onto a cart (the hook stopping on the load space), the drop onto
the bed and the crate riding a pulled cart on two screens (and a player joining after), the cart's pull and tipping with
two large crates (its centre of mass kept, its turning from the colliders), hover and E on a crate on the cart through
the storage box, Shift+E unloading on each side and against a wall, the crane lifting a crate off a cart, the cart
removed or broken with crates on it, a cart unloaded out of range and back, the old storage (taking goods out, every
route of putting in refused, E on the empty load space pulling the cart), the mass, two crates set down at once, a
dedicated server. The break: the fall and the landing on the owner and a second screen, the destroyed effect and
fragments, the spill's spots (walls, slopes, a deck, a cart's bed, water), the heaps' look for logs, ore, food and
arrows (the narrow way, copies sunk on a slope), the heap shrinking as a stack is partly picked up, and an owner logging
out mid-fall.

The works and the operator's hands (2026-11-03) are compiled only; the model was checked in the workshop (every gear
train meshing through its travel with the ratios above and colliding the other way, the levers swinging ±30° clear) and
both bundles read back against the contract, but nothing was seen in Unity or in game: the model in the game's light
(the iron matte and dark), each gear turning with its teeth meshing (the drum's new sense against its rope, the slew
pinion round the hoop, the boom's pinion along the rack), the levers' lean and its signs, the latch's flick, the hands
on the grips (the reach to the crank's low handle, the 45° bow, elbows, the hand-overs), the crank at about half a
turn a second (the step-up of 2026-11-03) with a hand on it and the drum's pinion whirling, the left hand's elbow beside
the crank's wheel, a second player watching, and the hands letting go when the operator leaves.

# Trawling

Trawling from a launched ship, asked for on 2026-11-03 (the user's words: "we need to have some kind of mechanism for
casting out a net on a boat while sailing to trawl, then some mechanism to hoist the net up onto the deck and the fish
drop out onto the deck for players to pick up", and "the net winch or hoist needs to be placeable on a ship"). A net
winch built on the ship casts a towed trawl net astern, the net catches the water's own fish while the ship moves
slowly, and hauling it in swings the bag over the deck and empties it there. Folder `Broadside/Broadside/Trawl/`;
Broadside's `CLAUDE.md` rules hold (methods at most 24 lines, classes at most 300, prefix/postfix only, multiplayer
first, English words in the code).

## How it plays

- **The winch.** "Trawl net winch" in the hammer's Misc tab (Broadside's build menu entry), after the ammunition piles,
  at a workbench: 10 `Wood`, 6 `FineWood`, 2 `Bronze`, 12 `LeatherScraps` (Meadows and Black Forest items), given back
  when removed. Built on a launched ship's deck, which it then rides like a cannon or a crate (on land it can be built
  but does nothing). Built at the stern or a side rail facing out: its davit's arm reaches 1.5 m out from its post
  (2.4 m high), and the net is cast only when nothing of the ship is under the arm's sheave. A frame of two sleepers
  and two cheeks carries a drum with flanges and a crank (the anchor windlass's sizes), the net rolled on the drum; the
  rope runs from the drum over a lead block on the post to the sheave at the arm's end.
- **E** at the winch: stowed, it casts the net (refused, with a word, off a launched ship, with the arm over the deck,
  or with less than 3 m of water astern); out, it hauls the net in; otherwise "The net winch is busy". The hover says
  what E does, how full the net is ("Net: 4 of 12 fish") and, while the net is out, how it fares at the ship's speed.
- **Casting** (5 s): the rope pays out to 14 m, the drum and crank turning, the roll on the drum shrinking; the net
  drops from the sheave, unfurls and streams astern. **Towing**: the net trails behind and below the ship, its mouth
  toward the ship, the rope from the sheave to its tow eye. Slow, it sinks to 3.5 m under the surface and hangs nearer the
  stern with the rope sagging; at 5 m/s and over it rises to 1.2 m and streams out to the rope's reach. It keeps its
  place in the water and is pulled along, so it swings wide of a turn; every move is eased.
- **Drag**: each net in the water adds 0.4 of the ship's own drag along it (x1.4), about 15% off its top speed (the
  speed goes as the drag's square root): a karve-like hull rowed at 4.0 m/s rows at about 3.4 with its net out.
- **Catch**: a fish every 30 s of trawling in the band, up to 12 in the net. The band: nothing under 0.5 m/s (stopped,
  at anchor), rising to the full rate at 1.5 m/s, full to 4.5 m/s, then the net lifts and catches less, down to a
  quarter of the rate at 8 m/s and over. Rowing or a half sail in a light wind is trawling; full sail before a strong
  wind is not. Each fish is the game's own fish item for the biome of the water under the net, by chance (`KwTrawlFish`):
  Meadows perch 60%, pike 40%; Black Forest pike 40%, perch 30%, trollfish 30%; Swamp giant herring; Plains grouper 50%,
  pike 30%, perch 20%; Ocean tuna 40%, coral cod 40%, pufferfish 20%; Mistlands anglerfish 70%, pufferfish 30%; Deep
  North northern salmon; Ashlands magmafish; none in Mountain water ("No fish in these waters"). The fish's prefab
  names (`Fish1` to `Fish12`, `Fish4_cave` the cave tetra, left out) and their biomes were read in the game on
  2026-11-03 (DevBridge `/eval`, the game's spawn lists).
- **Snags**: the sea floor is looked at four times a second under the net's tow eye and its bag. The net is kept 0.3 m
  clear of it; while the floor holds it up by more than 0.3 m it is snagged: it catches nothing, the hover says so, and
  each player aboard is told once on the screen ("The trawl net is snagged on the sea floor"). A full net tells them
  too.
- **Hauling in** (6 s): the rope winds in, the drum and crank turning the other way; the net comes up and in to the
  sheave, bunching up and hanging bag down. **Landing** (6.6 s): the davit swings in over the deck (2 s), the bag hangs
  there, opens and shakes (the fish fall out at 2.4 s), is wound onto the drum (to 5 s) and the davit swings back out:
  stowed. The fish lie on the deck under the bag (spread 0.45 m round it), each the game's own item, picked up with E
  or the game's auto pickup, and they stay where they lie however the ship sails (below). They are dead (2026-11-04, the
  user: "the fish need to be dead coming out of the net"; `KwTrawlDead`, the ZDO key `bs_catch_dead`): the game's fish
  behaviour and animation off, always out of the water for the pickup. Their colliders stay on, passing through their
  own ship's (they were off in 0.6.0, and a player found the catch could not be picked up: "they seem decorative").

## Contract (the net's model)

The workshop's boat-towed trawl net, `ValheimAssets/Assets/Props/TrawlNet` v001 (never edited from here): the bundle
files `Exports/Bundles/va_trawlnet_v001.windows` and `.linux` copied in as `Broadside/assets/bundles/bs_trawlnet.windows`
and `.linux`, embedded as `Broadside.assets.bs_trawlnet.<platform>` (the file renamed; the bundle's prefab keeps its
contract name `va_trawlnet_v001`, which only a rebuild could change). 7,940 triangles; 5.73 x 2.67 x 11.41 m; mouth 4.3 x
2.28 m; bag 6.4 m; origin at the mouth's middle, +Z into the bag, -Z toward the boat; the tow eye `tow_attach` at
(0, 0.45, -4.5), read from the model on load; meshes `frame`, `netting`, `ropes`, `tow_ropes`, `weights`; no colliders.
Dressed as its README says (`KwTrawlNet`): `frame` wears the game's `logwall` (`wood_wall_log`, `Custom/Piece`); every
other part a copy of it wearing the part's own hemp or stone albedo and normal map, white, metal cleared, gloss 0.08
(`GameMaterials.Plain(GameMaterials.Dress(logwall, placeholder), 0.08)`); then the ship's copies of those materials
(`KwShipMaterials`: no value noise, no ripple, moveable), as it moves with a ship. Loaded once where there is a screen,
into an inactive template every net is a copy of; a dedicated server loads nothing. Without the bundle no net is drawn
(the winch still works and catches) and the drum's roll wears plank wood. The net is a static shape: bunched, it is
the same model scaled down (0.18 x 0.2 x 0.12 hanging at the davit), whole once 6 m of rope is out.

## Identity

| What | Names |
| --- | --- |
| Piece (hammer, Misc tab, after the ammunition piles; on land or a launched ship's deck) | `BS_trawl_winch` ("Trawl net winch"; 10 `Wood`, 6 `FineWood`, 2 `Bronze`, 12 `LeatherScraps`), a copy of `wood_floor` stripped, its parts made in code (`KwTrawlShapes`), at a workbench, its recipe given back when removed; children `model` (frame, `irons`, `heel`, the boxes and the placing box `kw_place`), `drum` (`barrel`, `crank`, `roll`), `davit` (`arm`, `sheave/wheel`); rope lines `bs_trawl_lead`, `bs_trawl_tow` (screens only) |
| Net (drawn only, never networked) | `bs_trawlnet` (a copy of the bundle's `va_trawlnet_v001`, no parent) |
| Winch ZDO (its owner writes) | `bs_trawl_state` (0 stowed, 1 casting, 2 out, 3 hauling, 4 landing), `bs_trawl_at` (when the step began, the network clock's ticks), `bs_trawl_catch` (the fish's prefab names, comma separated), `bs_trawl_part` (the way to the next fish, 0 to 1), `bs_trawl_fare` (0 none, 1 slow, 2 trawling, 3 lifting, 4 snagged, 5 full, 6 no fish); and the rider's own `kw_rider`, `kw_rider_pos`, `kw_rider_rot` |
| Fish ZDO (written once by the game that landed it) | `bs_catch_ship` (its ship's `kw_ship_uid`; 0 once let go), `bs_catch_pos`, `bs_catch_rot` (its place and turn in the ship's frame), `bs_catch_fall` (metres it falls out of the bag), `bs_catch_at` (when it began falling) |
| RPC | `BS_Trawl_Use(int)` on the winch, routed to its owner: 1 cast, 2 haul |
| Patches | `ItemDrop.Awake` (postfix: a fish whose ZDO says it rides a ship rides it, Broadside's one `ItemDrop.Awake` patch, `Pieces/KwItemDropWake`); `ObjectDB.Awake` through `KwHammer` (the hammer); `ZNetScene.Awake` through BundlePrefabs (the piece) |

## Multiplayer

- **One game decides: the winch's owner** (the game that owns the winch's ZDO: its placer, then whoever the game hands
  the piece to, a player near it or a server running the area). Only it writes the winch's ZDO: it begins a cast or a
  haul when asked (`BS_Trawl_Use`, routed to it from the pressing player's game, which checks first and shows the
  words), moves the steps on by the network clock, fishes once a second, and spawns the catch. Chosen over the ship's
  owner because the state is the winch's own (the game's rule: only a ZDO's owner writes it, no cross-object writes),
  and because the winch's owner is always near its ship. The fish are spawned on that game as ordinary networked items
  (each made once; the net is emptied in the ZDO in the same frame, so an owner that changes mid-landing never lands
  them twice).
- **Every peer draws the same** from the ZDO: the step and the time in it give the rope out, the drum's roll, the
  davit's swing and the net's size (`KwTrawlTune`); the net's place in the water is worked out on each peer from the
  sheave as drawn and the ship's speed as it sees the ship move (`KwTrawlTow`, `KwTrawlSpeed`), which agree near enough
  on every screen. A late joiner reads the step and sees the net already out, or the fish already on the deck. Nothing
  is sent every frame; the owner writes the fare when it changes and the way to the next fish once a second.
- **Drag** is applied on the ship's owner only, where its physics run (`KwTrawlDrag`, a component on the ship added by
  the first winch riding it on every peer, counting each riding winch's rope out from its ZDO's step).
- **Fish on the deck ride the ship on every peer** (`KwTrawlDeck`): the game's items slide off a moving ship (and
  Broadside's riders are pieces), so each landed fish's ZDO records its ship's own id, its place and turn in the ship's
  frame and its fall, written once by the game that landed it; on every peer its body is kinematic and out of the
  collisions (`detectCollisions` off: the hover ray and the auto pickup still find it, and it never pushes the ship),
  its synced transform off, and it is put where the ship is drawn each frame, falling out of the bag at first. Moved
  by the game (drawn toward a player picking it up) it keeps where it was moved to. Its owner keeps its ZDO's place
  with the ship once a second, so it loads and unloads with the ship; with the ship not in its scene it follows the
  ship's ZDO found near; with the ship gone for good (destroyed, or nowhere near for a minute with the area loaded, the
  riders' rule) it is let go as an ordinary item. Picked up, it is the game's own fish.
- **Dedicated server**: the winch is made there too (the piece, its ZDO, its RPC, the owner's side if it owns the
  winch: the tow worked out without drawing, the catch, the spawning), nothing drawn and no net loaded.
- **Logging out with the net out** keeps it out: the step is in the winch's ZDO. The catch counts only while the
  winch's owner has the winch's ship in its scene (someone is near the ship); with nobody near, the ship and winch are
  not loaded anywhere and nothing counts. The ship's drag is back as soon as the ship is loaded, from the same ZDO.
- **A winch no longer riding a ship** (its ship gone, `kw_rider` cleared) has its net wound in at once by its owner;
  what was in the net stays there until it is next landed. A winch removed or broken with fish in its net spills them
  where it stood, riding its ship (on its owner, `WearNTear.m_onDestroyed`); a ship broken up gathers them into its
  cargo crates with the winch's cost.

## Numbers

| What | Value |
| --- | --- |
| Cast, haul, land | 5 s, 6 s, 6.6 s (swung in 2 s, fish fall at 2.4 s, emptied 3.4 s, wound 5 s) |
| Rope | 14 m out; 0.3 m with the net at the davit; whole net from 6 m |
| Depth of the tow eye | 3.5 m slow, 1.2 m at 5 m/s and over (eased over 1.5 s); reach 45% of the rope's slow, all of it streaming; the bag drooping 20 degrees slow, 5 streaming |
| Band | 0 under 0.5 m/s, full 1.5 to 4.5 m/s, a quarter at 8 m/s and over |
| Catch | a fish every 30 s in the band; 12 fish a net |
| Drag | x1.4 the ship's drag along it per net (about 15% off the top speed) |
| Water to cast | 3 m astern; the net kept 0.3 m off the floor, snagged when lifted 0.3 m more. Water the net needs under it: about 7 m at 1.5 m/s, 4.7 m at 3 m/s, 3.5 m at 4.5 m/s |
| Winch | 1.04 x 1.06 m frame, post 2.4 m, arm 1.5 m out, drum on a 0.55 m axle, roll 0.11 to 0.23 m |

## Not checked yet

Compiled only, never run in game (2026-11-03): the winch's look and icon (the generated frame, drum, crank, davit and
sheave, the roll of net in the bundle's hemp, the irons), placing it at a stern and at a side rail (the placing box, the
rider), the arm-over-water check, the cast, the net's look under water (the bundle's first use in gameplay; its
dressed materials, its scale bunched and whole, its turn eased), the tow rope's sag, the net trailing in turns and
drooping when slow, the drag on a real ship (about 15%), the speed band and the catch rate against real sailing,
the snag over shallows and the floor checks, the haul, the davit's swing and the fish falling onto the deck (the
landing ray, a deck under the bag, a cabin roof, the winch's own frame left out), the fish lying on their side, riding
a sailing ship on two screens, the auto pickup drawing them in and E picking them up, a late joiner, a dedicated
server owning the winch, a logout and login with the net out, the drum's spin direction, and the spill when the
winch is removed with fish in its net. No sounds yet (the game's winch or splash sounds would be borrowed).

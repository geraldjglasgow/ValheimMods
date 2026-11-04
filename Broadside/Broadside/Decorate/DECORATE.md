# Decorate - the Auto-decorate tool

Built 2026-11-03 from the user's words: "We also need to consider an auto decorate tool for a shipyard, where you kinda
have an area selector like with the EarthWright area thing and it auto fills in decorations, maybe allowing the player
to easily use the scroll wheel to have more or fewer decorations. This would need to be really neat and be super
compatible: walkways and docks and player-made docks with vanilla pieces would all need to be decorated without
interfering with the purpose of the area. Also need to make sure decorations are not placed in water (unless they are
supposed to be)."

Reworked the same day after the user tried it: "after choosing the size of the area with the alt scroll and then
clicking, the alt scroll should then allow to add or remove the decoration density ... because you need the regular
scroll wheel to actually zoom in and out to see the whole area. ... We need it to look like an actual functioning
shipyard, and we also need the ability for this area to be way larger and also to work on docks. ... a sawhorse next
to a scaffolding, and there is a shed next to both of those pieces that is pointing away, and then there is an anchor
next to it; that makes no sense. ... The pieces need to make sense and be grouped. And you don't have to use all of
the pieces."

It reads an area, then composes it from **scenes** (small hand-made arrangements of decorations that belong together:
a carpentry yard, a ship slip, rope works...) and **quay furniture** along the docks, shows them as see-through
previews, and places them as ordinary pieces through the game's own placement and pay path. Code in this folder,
classes `KwDecor*`; the tool's prefab `BS_tool_decorate`.

## How it plays

- **The tool.** Hammer, Misc tab (and Broadside's build menu entry under Transportation, and the game's Decoration
  filter): **Auto-decorate**, right after the dockside decorations. Free, needs no station itself; the decorations it
  places need their workbench, looked for where the player stands, as the game does.
- **The plain mouse wheel is never the tool's**: the tool does not turn, so the game gives the wheel to the camera and
  the view zooms as usual, to see a large area whole.
- **Aiming.** A square is drawn on what is there (ground, floor or the water's surface) round the aimed point, within
  30 m of the player's eyes and half the area's size more: pale. It turns to the floor aimed at (a dock section's own
  way, else the piece's turn), else the camera's heading to 15 degrees. **Alt + wheel** or **[ ]** size it: **6 to 64 m
  across**, 2 m a notch, 24 m at first, kept this session. The lines at the top: "Auto-decorate: an area 24 m across /
  Click reads it · Alt + wheel or [ ]: its size (6 to 64 m) · the wheel zooms as usual".
- **Click: read the area.** The square is locked where it is (its cells laid out from the floor aimed at, or the nearest
  building piece, so floors' edges fall on cells' sides) and read over frames (the square goes warm; the lines count the
  reading's share), then laid out: the previews stand where each decoration would go (a few made a frame) and the square
  goes green (red when nothing fits).
- **Alt + wheel or [ ] with an area locked: more or fewer.** Five densities, 3 at first, kept this session: **a few
  touches, a light yard, a working yard, a busy yard, a bustling yard**. Each density shows the scenes and fillers that
  first come in at it and every one before (below), so turning it adds or takes away whole scenes and their fillers and
  never moves what stays. To change the size of a locked area, Esc and size it again.
- **The lines once laid out** say how busy, which scenes and how many pieces, the total cost against what the player
  has (what lacks in red), whether a workbench is in range, and the keys: "Auto-decorate: A busy yard (4 of 5): a
  carpentry yard, steam bending, a ship slip, rope works and quay furniture, 31 pieces / Cost: ... / Click places them ·
  Alt + wheel or [ ]: more or fewer · N: another layout · Esc lets it go". At a density below the top with nothing more
  that fits it says "nothing more fits".
- **N: another layout** for the same area (another roll of the seed). The same area, size and roll always give the same
  layout.
- **Click inside the square: place.** All of them when the player can pay for all. If not, nothing is placed and the
  lines say what lacks; **Shift + click** places the ones he can pay for (the layout in order, each kept while the total
  is still paid for). Clicking outside the square reads a new area there.
- **Esc** lets the area go (instead of opening the game menu); so does putting the tool away.
- **Ctrl + Z** with the tool in hand takes the last auto-decoration back (below).

## Reading the area

The area is a square of 0.5 m cells (`KwDecorGrid`), turned to the frame: 12 by 12 cells at 6 m, 128 by 128 (16,384)
at 64 m. The reading runs a pass at a time over frames, at most 4 ms a frame (`KwDecorRead`): the rays down 128 cells a
step, the wall rays 128 cells a step, the floors, the ground and water, the pieces round the area 48 a step, the zones
kept clear, and the fields, each its own step.

- **Rays** (`KwDecorRays`): one down through each cell's middle from 4 m over the aim's height to 8 m under it (a large
  area's band grows with it: 35% of its half-size over, 50% under, so 11 m over and 16 m under at 64 m, for a slope far
  off), against the world, pieces and terrain (never water, ghosts, characters or vehicles), every surface listed top
  down (up to 32). The surface taken is the one nearest the aim's height among those with 2 m clear over them, one more
  than 1.5 m under the aim counted 3 m farther (a roof over a dock leaves the dock; a chest on a dock is what stands
  there; a high dock beside the water aimed at is the dock, not the sea floor under it). The water over it comes from the
  game's own water level (`Floating.GetLiquidLevel`, calm), never from a ray.
- **What a cell is** (`KwDecorSurface`): terrain, dry (ground) or under water (water, with its depth); a **floor**: a
  Broadside dock section's deck (`KwPierDeck`, `Docks/DOCKS.md` "Reading a dock"), or any piece's top that is flat
  (normal 0.9 up or more), of a slab at most 0.8 m thick, supporting (`WearNTear.m_supports`), no furniture and no
  comfort, working at nothing (no chest, station, extension, fire, oven, smelter, fermenter, beehive, bed, door, portal,
  sign, item stand, ladder, ship or cart), not a decoration and not the dry dock's; anything else is **blocked** (a
  chest, a table, a rock, a wall's top). A blocked cell with a floor within 2.5 m under it is **covered**: it counts for
  the floor's width. A floor under water is blocked.
- **Floors** (`KwDecorDecks`): floor cells joined across steps of 0.35 m or less are one floor; one under 3 m² is
  something standing (a table's top). Round each floor: a **drop** (water, nothing, or lower by more than 0.6 m), a
  **step** (ground or another floor within 0.6 m: where it is walked onto), or something **neutral** (a wall, higher
  ground). A floor with no drop, or with more steps than drops round it, is **open** (flush with the ground, or walled
  in): decorated as ground, but a strip 4.5 m across or less and at least 2.5 times as long is a **boardwalk**, kept
  clear. Any other floor is a **dock** or **walkway**, read across its width (the shorter of the two runs of its cells
  through each cell):
  - its **lane**: the middle, at least 1.6 m (`width / 2 - 0.8` from each side), or all but the outer 0.5 m rows when
    narrow: kept clear (a piece set against the dock's edge may reach into it only as far as the dock stays 1.6 m clear
    across with room for a post opposite, "The quay");
  - its **edge band**: the rest, up to 1.75 m in from each side: where deck pieces stand, their backs to the side;
  - a wide dock's (8 m or more both ways) middle past the bands is **open**, decorated as ground;
  - its **end**: the last 1.5 m before a drop along its length (a pier's end) is band, not lane; a cell as near its end
    as its side points to the end (**tip**), so a pier head's edge is its whole width, corners included;
  - its **access**: within 1.5 m of a step (where it is walked onto from the land or another floor): kept clear, the
    ground beside it too; the middle of each floor's access is its **entry** (a path end, the quay's root);
  - each band cell points to its nearer side (or end); the outer row at a drop or a neutral side is an **edge**, at
    water a **water edge**, which is also the shore.
- **Ground** (`KwDecorGround`): ground painted dirt or paved (the hoe's path, a road) in a strip 4.5 m across or less is
  a **path**, kept clear; a wider painted yard is open ground. Every cell's distance to water (up to 16 m) and the way to
  it; dry ground within 1.5 m of water and the waterline itself (water 0.4 m deep or less beside dry ground) is the
  **shore**. Steeper than 20 degrees is never stood on (the cell's normal, 0.94 up).
- **Walls** (`KwDecorWalls`): from walkable cells every metre, a ray 1.2 m up along each of the frame's four ways, 0.9 m
  long, against building pieces: an upright face of a wearing building piece, working at nothing, its collider 1.4 m
  tall or more, is a wall face facing that open cell.
- **Kept clear** (`KwDecorKeepClear`, `KwDecorClearRules`, then `KwDecorClear`), from the pieces round the area (the
  game's own list, `Piece.GetAllPiecesInRadius`, once a reading, read 48 a step): a door's front and back (2 m, as wide as
  the door and 0.3 m more); a ladder or stairs (a name with "stair" or "ladder", or a `Ladder`) and 1.5 m round; a
  crafting station 2 m round and a 2.4 by 3 m patch in front of it; a station extension 1 m round; a fire, oven, smelter,
  fermenter or beehive 1.6 m round; a chest 0.5 m round and its front; a bed and 1 m round; a portal's front and back (3 m
  each side); a sign's front; an item stand 0.6 m round; a ship or cart and 1.5 m round; the dry dock (its keel block
  guides and keel blocks and 4.5 m round, the ship designed on it and its fittings and the blueprint table and 2 m round,
  as one box, also kept as the area's dry dock); and a 1.6 m way between each door side, ladder or stairs, station front,
  portal side or dock entry and its two nearest others within 18 m (the paths between them). Each zone is drawn only
  into the cells it can reach. The dockside decorations already standing keep their spacing.
- **Fields** (`KwDecorFields`), worked out last: every cell's steps to the nearest path or path end (12 m) and the way
  there, and to the nearest wall face (6 m); the yard's **heart** (the dry dock, else a ship or workbench, else the open
  ground's middle); its **entrances** (dock entries and painted paths crossing the area's side; else the path ends); the
  walkable heights' range; and four summed tables (`KwDecorTable`: any rectangle's failing cells in four lookups) of
  where a standing piece may stand (dry level ground or a wide floor's open middle, nothing kept clear), the same on the
  land alone, where people walk, and where people walk or the water is.

## Scenes

A scene (`KwDecorScene`, the list in `KwDecorScenes`) is a small hand-made arrangement of decorations that belong
together, in its own frame: its front +Z (the side people work from), x to its right, metres from its middle, each piece
at a place and a turn in it. The gaps written there are the real gaps between the pieces' footprints, sized for a
Viking to work in. A scene is placed **whole or not at all**: its core pieces all fit, or nothing of it is placed; its
**fillers** (pieces that come in one or two densities later) are left out where they do not fit, and every filler stands
against a core piece, so none is ever stranded. A piece not in the game yet (the pit saw, the figurehead bench) is left
out of its scene until it is; a scene missing a core piece is never placed.

**Finding its place** (`KwSiteSearch`, a row of the area a step): every metre a middle is tried, its front turned by the
scene's rule (`KwDecorFacing`) and square to the area's frame, mirrored and not (a piece whose two ends differ, the steam
box's kettle or the rope walk's jack, is turned round in a mirrored scene instead of mirrored). A place goes on only
when the scene's needs are met (`KwDecorLikes.Meets`), every core piece's footprint is on standable cells, its working
spaces (aprons: the space in front where people work, a kettle's end) are walkable (or water, for a scene facing it), and
nothing of it reaches into another scene's room (its footprint grown by its margin, 1 to 2 m, and its aprons); all in
table lookups. The 16 best by what it likes (with a little chance, and never two within 2 m facing the same way) are then
tried whole, one a step (`KwDecorStage`): each piece where the frame puts it, on the surface there, a degree or so off at
random, through every check below; a wall piece on the nearest building wall within 7 m that faces the scene; the core
within 1.2 m of height (never across a terrace). The first that fits is taken.

**Facing rules**: **Open**, toward the most open walkable ground round it (a 6 m square 4.5 m out each way, the fewest
cells nobody walks on, the area's outside counted as such; ties toward the heart); **Water**, toward the nearest water;
**Path**, toward the nearest path or path end (else Open); **Heart**, toward the yard's heart; **Slip**, toward the water
within 8 m, else toward the dry dock.

**Needs** (yes or no, `KwNeed`): **WaterFront**, its front within 1.5 m of the water; **Slip**, its front within 3 m
of the water or 6 m of the dry dock; **NearWater**, its middle within 14 m of the water; **Quay**, within 7 m of a dock's
entry; **PathFront**, its front within 2.5 m of a path or path end; **Land**, its core on the land itself, never on a
floor. **Likes** (scores, `KwLike`): **Yard** (+3 with the dry dock within 12 m or a ship or workbench within 14),
**BackToWall** (+1.5 with a wall face within 3 m behind), **Dry** (-3 within 4 m of the water, -1 within 7), **Central**
(-0.06 a metre from the heart), **Outskirts** (+0.08 a metre from the heart, +2 within 5 m of the area's side), **High**
(up to +2, the higher the better), **Entrance** (up to +3 near a way into the yard), **Path** (+1.5 within 4 m of a
path), **Quay** (up to +3 near a dock's entry), **Wet** (+1 within 8 m of the water), **Beside** another scene (up to
+2.5 within 14 m of it); plus up to 1.5 at random. A second copy stands at least its scene's apart from the first; a
scene of one piece (a vane, a heap, an anchor) never within 16 m of the same piece placed by another scene.

### The scenes

In the order they are placed (the pickiest and largest first); the density each first shows at in brackets, its fillers
one or two later.

| Scene | Pieces (frame metres; turn) | Where | Facing |
| --- | --- | --- | --- |
| **A carpentry yard** (1; a second at 5, 20 m from the first, large areas) | lean-to (0, -1.6), its open front to the yard; wood shavings and sawhorses on them (-0.5, 2), 1.5 m in front of the eave; fillers: timber drying rack (3.55, -1.6; 90°) 0.8 m off its side, nail barrels (-3, -0.3) at its front corner, the tool wall on a building wall within 7 m (behind the shed when its back is to a building); slots for the pit saw (-5.3, -1.6; 90°) and the figurehead bench under its roof (0, -2.55), not in the game yet | Land; likes the yard (dry dock, workbench), a building at its back, back from the water, near the heart; working space 4.8 by 2.5 m in front | Open |
| **A fishing corner** (1; a second at 4, 16 m off) | net drying rack (0, -1), lobster pots (-1.1, 0.6); fillers: the stone anchor (1.1, 0.7) and a mooring post (2.5, 1.9) at the water, the hanging net on a building wall, a second stack of pots (-2.7, -0.7; 90°) | WaterFront; likes a dock's entry near | Water |
| **A ship slip** (3) | hull frames (0, 0.4; 90°) lying along the water or the dry dock, the scaffold (0.4, -2.45) on the land side 0.45 m off the hull, its toe board to it; fillers: shavings and sawhorses (-3.7, -4.3) behind the scaffold, nail barrels (3.3, -4) at its far end | Slip, Land; likes the yard; its launching side may be water | Slip |
| **Steam bending** (2) | steam box (0, 0), its kettle's end kept clear (a 1.9 by 1.6 m space), the spar rack (0.2, -2) behind, the tar pot (4, 1) 2.35 m off its open end | Land; likes the yard, dry ground, near the carpentry yard | Heart (the dry dock) |
| **Rope works** (3) | rope walk (0, 0), its jack's end to the rope spool (-3.4, 0; 90°, facing along it), rope coils (3.2, 0.1) at its sledge's end; filler: spare coils (-3.3, -1.6) behind the spool; people walk along its front | Land; likes a path beside, dry ground | Path |
| **A ship gear store** (2) | oar rack (-3.4, 0), shield rack (-1, -0.05) and spar rack (2.6, 0.15) in a row, backs in line; fillers: the ship's anchor (-5.6, 0.35; 90°) at the oar rack's end, nail barrels (5.6, 0.15) at the spar rack's | NearWater; likes a dock's entry, a path | Path |
| **A sail loft** (4) | sail frame (0, 0) facing its working side, sail bolts (2.45, 0.2) beside it; filler: the pennant line (0.8, -1.5) strung behind | ground or a wide dock's open middle; likes a wall at its back, near the rope works | Open |
| **A notice board** (2) | the notice board | PathFront; likes a way into the yard (a dock's entry, a path at the area's side) | Path |
| **A weather vane** (3) | the vane, 2 m clear round it | Land; likes high ground | Heart |
| **A salvage heap** (4) | the salvage heap | Land; likes the outskirts, near the water | Heart |
| **A ship's anchor by the quay** (4) | the ship's anchor | Quay, Land | Water |

The gangplank is not in any scene: its top (1.5 m) does not reach the scaffold's platform (2.0 m), and the scaffold has
its own ladder; it is placed by hand against a dock or a ship's side.

## The quay

Along the docks' edges (`KwDecorEdges`: each dock's outer band row at a drop or a wall cut into straight **runs**, by
floor, by the way out and by row, split at a gap, an access or a step; a run mostly facing the water is a **water run**,
one mostly pointing to the dock's end a **pier head**; each run's usual width across, the middle of its cells' widths so
a branch joining it is not counted). Laid out before the scenes (`KwDecorQuay`, `KwDecorHeads`), every piece set
against its edge with its back to it and facing in (the line found to 2 cm by rays, a dock's curb counting as the end,
`KwDecorWorld.Line`), each searching half a metre either way, then a metre, when its spot is taken:

- **Pier heads** (a dock's end over the water at least 2.4 m wide), ranked by how far they are from where their dock is
  stepped onto: the **harbour beacon** (2) at the outermost one's far corner; the **signal bell** (3) at the next pier
  head's corner, or the outermost one's other corner when it is 4.6 m wide or more; a narrower outermost end (a
  walkway's) gets a **lantern pole** (2) over a corner's piling instead (as below); **pilings** (4) in the shallows 1.8 m off the
  outermost head's corners. The middle of every pier head stays clear.
- **Fishing gear on the pier** (3), on a dock 4 m wide or more: lobster pots and the stone anchor along a water side,
  nearest its outer end, placed together or not at all, the net drying rack beside them (4).
- **Mooring posts** (1) every 7 m along the water sides, and **fenders** (3) hung on the dock's side between them,
  staggered: one side's posts start 1.5 m along, the other side's half a stride on, so one side's post faces the other's
  fender. On a dock section each post stands over the free piling station nearest its place in the rhythm, as one placed
  by hand on a dock (`KwDecorMoorings`, `Dockside/KwDockMooring`, `Docks/DOCKS.md` "Mooring posts on a dock"): over the
  piling, moved in from its axis until the post's back is flush with the deck's side (nothing of it hangs past the edge),
  square to the edge, its rope coil in over the deck, the curb cut round it once placed (so its solid
  test passes over the dock's own curb and timbers, and its spacing is kept from the others); on a game floor dock it is
  set against the edge as before. Fenders need the water deep enough under them (their 1.2 m); nothing stands in the
  water alongside, where a ship moors.
- **Lantern poles** (2) every 11 m along the water sides, alternating sides (one side from 4.5 m along, the other half
  a stride on), each over the free piling station nearest its place in the rhythm, as one placed by hand on a dock
  (`KwDecorLights`, `Dockside/KwDockMooring`, `Docks/DOCKS.md` "Mooring posts on a dock"): its post on the piling's axis,
  so the dock's own piling reads as rising through the deck into it, turned so its lamp (the model's -X) hangs in over
  the deck toward the dock's middle, without its stone cairn (`Dockside/KwDockTrim`), the curb cut round it once placed.
  Its lamp is clear glass with a small torch flame lit inside from dusk to dawn (`Dockside/KwDockLantern`). A station a branch, a ladder, a mooring post or another lantern holds (or is planned at) is passed over. A game floor
  dock has no pilings and gets none. The same day these were lantern poles risen from the sea floor beside the dock and
  then dock lanterns on posts of their own beside it; the user: "it literally needs to attach to the existing docks pole.
  And not create a brand new pole."

A piece set against a dock's edge may reach into its lane (`KwSlot.Reach`) as far as leaves the dock 1.6 m clear across
with 0.5 m more for a post opposite: a 2 m walkway takes only its posts (they fit in its outer row). A fender hung over
the water and a piece standing on the deck are either side of the edge, so they keep no spacing between them; a mooring
post or lantern pole over a piling (right over where a fender would hang) keeps its spacing from both. The ends where a dock is stepped onto (its access) are never used.

### Wood tiers

The decorations made of a dock's wood come in its three tiers (2026-11-03, `Dockside/KwDockTiers`: the mooring post,
stone anchor, net drying rack, lobster pots, harbour beacon, fenders and signal bell, each with a core wood and a fine
wood copy, `BS_dock_<id>_core` and `_fine`, of the same footprint and metadata). The layout's last step
(`KwDecorTiers`) swaps each for the tier going with where it stands: on or beside a dock section (its deck within
1.5 m), that dock's material's (plain wood plain, core wood core, fine wood fine); on a game floor (the floor read under
it), core wood when the floor is clearly core wood (hard wood, or a darkwood or log piece by name), else plain; on the
shore (in the water, or within 4 m of it) within 12 m of a dock section, the nearest one's; anywhere else plain. The
previews, cost and placed pieces are the tier's. The lantern pole has no tiers.

### Support

The game holds a piece up (`WearNTear.UpdateSupport`, on its owner) by the pieces its boxes touch, each giving its own
support less a loss for the distance between the two pieces' origins (or centres of mass): wood loses 20% of it a metre
sideways (12.5% straight down) and falls under 10. A dock section's origin is at its start, up to 12 m away, so a piece
standing on its deck had full support only near the section's start and none past about 4.4 m along it (7 m straight
down): that is why half the lantern poles the tool stood on docks broke. Since 2026-11-03 a dock section holds anything
resting on its deck at that piece's full support, wherever along it it stands (mooring posts, the beacon and bell at a
pier head, fishing gear, fenders hung on its side, the mooring posts and lantern poles over its pilings, a player's own
pieces), and holds its ladders (`Docks/DOCKS.md`, "Support").

## What a decoration needs

`KwDecorFitCheck`, cheapest first:

1. Its kind of place is one of its piece's (`KwDecorFit.Places`).
2. Every point of its footprint (every 0.25 m, 5 cm in from its sides; the 0.2 m strip along the back of a piece set
   against an edge or wall is left to that line and the solid test) on a cell of its kind of place, never kept clear,
   and not in a path, lane or access unless it may lie in one (`InPath`), it hangs, or it is a piece set against a dock's
   edge within its reach into the lane: **deck**, that floor's band at the slot's height (15 cm); **ground**, ground no
   steeper than 20 degrees within 0.3 m of its height, or an open floor; **shore**, the same or the dock's band, and for a
   piece made for water the waterline (0.4 m deep at most); **shallows**, water between 0.15 m and its `MaxDepth`;
   **edge**, off the dock and clear down to its height under the deck; **wall**,
   nothing standing under it.
3. Apart from every planned decoration of another scene and every one standing, by the larger of the two spacings
   (pieces of one scene are where the scene puts them; a hung edge piece and a deck piece keep none; a mooring post or
   lantern pole over a piling keeps it from both).
4. The game's own rules (`KwDecorRules`, as the placement ghost's): no no-build location
   (`Location.IsInsideNoBuildLocation`), a ward the player may build in (`PrivateArea.CheckAccess`), not in a dungeon
   unless the piece or the world allows, its biome when it has one, a fire the game keeps off wood (`m_notOnWood`) off a
   wooden floor, and nothing in water whose piece refuses water (`m_noInWater`).
5. Nothing solid in the way (`KwDecorSolids`): each of the piece's own colliders' boxes at its pose (the placing box left
   out), 4 cm smaller all round and lifted 6 cm (a hung piece moved 4 cm off its wall), against the world, pieces,
   vehicles and characters (`Physics.OverlapBoxNonAlloc`); a piece without boxes uses its footprint to its height.

Rules 4 and 5 and the edge's line are the only parts of the layout that ask the world (`KwDecorWorld`); the rest is
plain arithmetic on the reading.

## Densities

Every decoration carries the density that first shows it (its scene's, a second copy's later, a filler's one or two
later, the quay's by piece), worked out once for the whole layout; a density shows those up to it, in the order they
were laid out (`KwDecorLayout.Shown`). When nothing found room at the first densities, all are shifted down so the first
always shows something. In short: **1** a carpentry yard, a fishing corner and mooring posts; **2** steam bending, a ship
gear store, a notice board, the beacon, lantern poles, the carpentry yard's fillers; **3** a ship slip, rope works, a
weather vane, the bell, fenders, fishing gear on the pier; **4** a sail loft, a salvage heap, a ship's anchor by the
quay, pilings, a second fishing corner; **5** a second carpentry yard and the last fillers. A small area holds only what
fits: a 16 m area usually one or two scenes.

## Placing

`KwDecorPlacer`: two a frame, each through `Player.PlacePiece` (the game's own placement: a networked piece with the
player as its creator, its placing effect and sound, the same as one placed by hand), then paid with `KwGamePay.Take`
(`Player.ConsumeResources`, the game's). Before each, the game's rules, the solid test and its cost are asked again;
one that no longer fits (something moved in) or cannot be paid is left out and counted. A scene's pieces are placed in
its own order (the shavings before the sawhorses on them). It pauses while the player's placement ghost stands on a
launched ship (the rider rule would take a piece placed then as built aboard). Free with the game's no-cost cheat or
the world's no build cost. The player swings once. A mooring post or lantern pole over a dock's piling is tested past
the dock's curb it will cut. A
decoration is then an ordinary piece: removable with the hammer (its recipe given back), worn, warded, saved and
replicated as any.

## Undo

`KwDecorUndo`: the pieces placed in one go are a batch (their ZDO ids). **Ctrl + Z** with the tool in hand removes each
of the last batch still standing the hammer's way (`WearNTear.Remove`: its owner removes it and drops its recipe, the
usual refund), each checked as the hammer checks: this player made it, it may be removed now, no no-build place, a ward
he may build in, the workbench in range. Only the last batch, once, this session.

## Cost of a large area

At 64 m (16,384 cells) the reading casts 16,384 rays down (each through a band 27 m deep) and at most 16,384 wall rays
(four from each walkable cell every metre), 128 cells a step; at a few microseconds a ray that is about a tenth of a
second of rays, spread over some 30 frames at 4 ms (not measured in game). Its passes after the rays, measured offline
on a 64 m shore with a dock (best of six after a warm-up, 2026-11-03): floors 1.5 ms, ground and water 2 ms, kept-clear
marking 0.2 ms, fields 1.2 ms, each its own step. The layout: about 21 ms (the median of 20 runs; up to 90 ms on a busy
machine) in some 880 steps of 0.1 ms or less each (a row of places looked at, or a scene tried whole), within the same
4 ms a frame, so a frame's work is never much over 4 ms. The previews are stood 6 new or 24 from the pool a frame, and a
density change only stands or puts back the difference. The square's outline has 16 points a side, up to 32 on a large
area, laid again only when it moves.

## Decoration metadata

Each dockside decoration the tool may place has a `KwDecorFit` in its `KwDockDef.Decor` (in `Dockside/KwDockside.cs`,
`KwDockYard.cs` and `KwDockQuay.cs`, together `KwDockside.Defs`); a def without one (the gangplank) is never placed by the
tool. Where it goes and with what is the scenes' and the quay's business; the metadata is the piece's size and rules.

```csharp
new KwDockDef("ropecoils", "Rope coils", "...", 0f, ("Wood", 2), ("LeatherScraps", 6))
{
    Decor = new KwDecorFit(1.4f, 1.2f, 0.4f, KwDecorPlace.Deck | KwDecorPlace.Ground | KwDecorPlace.Shore) { Spacing = 0.5f },
},
```

`new KwDecorFit(width, depth, height, places)`; then any of:

| Field | Meaning | Default |
| --- | --- | --- |
| `width`, `depth` | the footprint on the ground along the piece's own x and z (the BRIEF's W and D, not its H): what it covers visually, including a coil or offcuts lying round it | |
| `height` | its height (the BRIEF's H): a wall piece needs a wall this tall over its lift; an edge piece hangs this far under the deck's top | |
| `places` | any of `Ground` (open ground, open floors), `Deck` (a dock's edge band, back to its edge or a wall), `Edge` (hung on a dock's side over a drop), `Wall` (hung on a wall), `Shore` (the water's edge: ground within 1.5 m, the waterline, a dock's water edge), `Shallows` (standing in water) | |
| `Spacing` | metres kept clear round its footprint from decorations of other scenes and standing ones | 0.6 |
| `InPath` | may lie in a path, lane or access (a flat floor scatter) | false |
| `Wet` | made for the water: may stand at the waterline and in shallow water. **Also clears the piece's `m_noInWater`** (`KwDockside.Configure`), so it is placed there by hand too | false |
| `MaxDepth` | a `Shallows` piece's deepest water | 1.6 |
| `Lift` | a `Wall` piece's origin this high over the floor | 0.4 |

Conventions (the BRIEF's): a standing piece's origin is on the ground at its footprint's centre, its front +Z; a piece
that hangs (`Wall` or `Edge` only) has its origin at the foot of the face behind it, its back on that face (z = 0), the
piece in front. A piece is either standing or hanging: never give one both a standing place and `Wall`/`Edge`. Its
colliders are read from the piece itself for the solid test (its `col_*` boxes), so no collider sizes go here. The
models' own ends, read from their HANDOFFs and used by the scenes: the lean-to open at +Z; the steam box's kettle at -X,
its open end at +X; the rope walk's jack at -X, its sledge at +X; the scaffold's toe board at +Z, its ladder at +X; the
hull frames' bow at +Z; the sail frame's patched face and bench at +Z; the lantern pole's lamp hanging toward -X
(over a dock's piling, -X toward the dock's middle and the cairn left off, `KwDockTrim`); the dock lantern's origin where
its post's axis meets the deck's top, its arm and lamp toward +Z (in over the deck); the mooring post's rope coil at +Z.

| Decoration | Footprint W x D, H | Places | Spacing, other |
| --- | --- | --- | --- |
| `BS_dock_mooringpost` | 0.45 x 0.45, 1.05 | Shore | 0.8, Wet |
| `BS_dock_anchor` (stone) | 1.6 x 0.9, 0.45 | Shore | 0.8, Wet |
| `BS_dock_oarrack` | 2.0 x 0.7, 2.9 | Deck, Ground | 0.8 |
| `BS_dock_shieldrack` | 2.0 x 0.6, 1.3 | Deck, Ground | 0.8 |
| `BS_dock_netrack` | 3.4 x 0.8, 2.3 | Ground, Deck, Shore | 1 |
| `BS_dock_creels` | 1.3 x 0.8, 0.9 | Deck, Ground, Shore | 0.6 |
| `BS_dock_beacon` | 1.2 x 1.2, 4.6 | Deck, Ground, Shore | 1.5 |
| `BS_dock_vane` | 1.0 x 1.0, 4.2 | Ground, Deck, Shore | 1.5 |
| `BS_dock_sawhorses` | 2.4 x 0.8, 0.95 | Ground, Deck | 0.8 |
| `BS_dock_dryingrack` | 3.0 x 1.2, 2.2 | Ground, Deck | 1 |
| `BS_dock_steambox` | 3.3 x 0.8, 1.4 | Ground | 1.2 (its small fire keeps it off wooden floors, `m_notOnWood`) |
| `BS_dock_scaffold` | 4.0 x 1.2, 2.2 | Ground | 1 |
| `BS_dock_hullribs` | 3.6 x 10.0, 3.0 | Ground | 1.5 |
| `BS_dock_leanto` | 4.3 x 3.3, 3.2 | Ground | 1.2 |
| `BS_dock_toolwall` | 2.0 x 0.25, 1.6 | Wall | 0.4, lift 0.6 |
| `BS_dock_shavings` | 2.0 x 2.0, 0.05 | Ground, Deck | 0.2, InPath (its flat 2 cm box on `piece_nonsolid`, for the hammer, is its solid test) |
| `BS_dock_ropecoils` | 1.4 x 1.2, 0.4 | Deck, Ground, Shore | 0.5 |
| `BS_dock_ropespool` | 1.2 x 1.05, 1.3 | Deck, Ground | 0.6 |
| `BS_dock_ropewalk` | 4.0 x 1.0, 1.6 | Ground | 1 |
| `BS_dock_sparrack` | 4.5 x 1.0, 2.0 | Ground, Deck | 1 |
| `BS_dock_sailbolts` | 1.6 x 1.1, 0.7 | Deck, Ground | 0.6 |
| `BS_dock_sailframe` | 2.6 x 0.9, 2.6 | Ground, Deck | 1 |
| `BS_dock_tarpot` | 1.2 x 1.2, 1.3 | Ground | 1 (its small fire keeps it off wooden floors) |
| `BS_dock_nailbarrels` | 1.5 x 1.0, 1.0 | Ground, Deck | 0.6 |
| `BS_dock_ironanchor` ("Ship's anchor") | 2.0 x 1.4, 0.6 | Shore, Ground | 0.8, Wet |
| `BS_dock_fenders` | 2.0 x 0.5, 1.2 | Edge, Wall | 0.8, Wet, lift 0.3 |
| `BS_dock_pilings` | 1.0 x 1.0, 3.0 | Shallows, Shore | 1, Wet, max depth 2 |
| `BS_dock_gangplank` | none: placed by hand against a dock or a ship's side | | |
| `BS_dock_salvage` | 4.0 x 2.8, 1.9 | Ground, Shore | 1.2 |
| `BS_dock_hangnet` | 2.5 x 0.3, 2.2 | Wall | 0.4, lift 0.3 |
| `BS_dock_lanternpole` | 1.0 x 0.8, 2.8 | Ground, Deck | 1 (on a dock near a side seated over a piling, `KwDecorLights`) |
| the wood tiers `BS_dock_<id>_core`, `_fine` (mooring post, stone anchor, net drying rack, lobster pots, beacon, fenders, bell) | as their plain piece | | the plain one's (`KwDecorTiers` picks the tier) |
| `BS_dock_bell` | 1.4 x 0.8, 2.6 | Ground, Deck, Shore | 1 (its bell and rope's box on `piece_nonsolid` counts in the solid test) |
| `BS_dock_pennants` | 4.0 x 0.5, 3.0 | Ground, Deck | 1 |
| `BS_dock_noticeboard` | 1.6 x 0.4, 2.2 | Ground, Deck | 1.5 |

Still to come, once their bundles are built (`ValheimAssets/Assets/Ships/Keelwright/Dockside/BRIEF.md`; check each
against its HANDOFF's real size before adding); their places in the carpentry yard are already in its scene:

| Prefab | `new KwDecorFit(...)` | Initializer |
| --- | --- | --- |
| `pitsaw` | `(4.0f, 2.0f, 2.0f, KwDecorPlace.Ground)` | `Spacing = 1.2f` |
| `figurebench` | `(2.2f, 1.0f, 1.6f, KwDecorPlace.Ground \| KwDecorPlace.Deck)` | `Spacing = 1f` |

## Adding a decoration or a scene

- **A decoration**: add its `KwDockDef` to the list it belongs with (`KwDockYard.Defs` for the yard's work, rope and sail,
  `KwDockQuay.Defs` for ship gear, light and signal), with `Decor = new KwDecorFit(...)` from its BRIEF size (W, D, H in
  that order) and its kinds of place; `Wet = true` for anything made for the water's edge or shallow water. Then put it
  in a scene (or the quay): the tool places nothing that no scene names. A scene may already name it (the pit saw, the
  figurehead bench): it then shows as soon as the piece is built.
- **A scene**: one more `KwDecorScene` in `KwDecorScenes.All`, where in the list says how early it is placed (pickiest
  first): an id, the player's words for it ("a ..."), the density it first shows at, a facing rule, its needs and likes,
  its pieces with `M(piece id, x, z, turn, extra densities, place, ends differ)` in its own frame (front +Z), measured
  from the pieces' footprints so the gaps are real, every filler against a core piece; its aprons (where people work,
  in its frame), `ApronWet` when it faces the water, its margin, and `Copies`/`Later`/`Apart` for a second one in large
  areas. List a piece that must stand on another after it (the sawhorses after their shavings): the placer places in the
  scene's order. Check it in the offline harness (below) on a shore, open ground and a dock before trying it in game.
- **Quay furniture** is code, not data (`KwDecorQuay`, `KwDecorHeads`): a rhythm along the runs or a pier head's corner.

## Docks

The Dock tool's sections (`Docks/`, `BS_pier_*`) are read through `KwPierDeck` (`Docks/DOCKS.md`, "Reading a dock"): a
section's deck is always a floor whatever its deck box's depth, the area turns to its `Forward`, and its cells are laid
out from its right edge (`Start + Right * Width / 2`) so both sides fall on cells' sides at every width the tool makes.
Its curbs are found by the edge line's rays: what stands on the deck goes inside them, what hangs goes on the outer
side. Sections joined end to end or by a branch are one floor (no edge at the joint). The game's floors and any other
mod's are read by the same floor rules: a 4 m dock of the game's 2 m floors gets the same quay as a Broadside one.

## Multiplayer

Everything before the click to place is on the player's own game: the aim, the reading (rays and the game's piece
list), the layout, the previews (on the ghost layer, never networked), the square and the lines. Nothing new is sent or
saved: each decoration is placed through `Player.PlacePiece` and paid through `Player.ConsumeResources`, so every peer
and a dedicated server see an ordinary piece placed by that player; the undo removes through `WearNTear.Remove`, the
hammer's own call to each piece's owner. A dedicated server never makes the driver (it is made when the tool is first
taken in hand).

## Code map

| File | What |
| --- | --- |
| `KwDecorFit` | the metadata: `KwDecorPlace`, `KwDecorFit` |
| `KwDecorTool` | the hammer piece (not turning), `Held`, `AddTo` (from `KwHammer`), `Made` (for `KwBuildMenu`) |
| `KwDecorInput` | `KwDecorClick` (the click), `KwToolWheel` (Alt + wheel owned), `KwDecorHold` (the driver made), `KwDecorEscape` (Esc) |
| `KwDecorDriver` | the per-frame driver: keys, work budget, aim, square, previews, lines, the placing queue |
| `KwDecorSession` | size, density, the locked area's reading and layout, the roll and seed, `Turn`, `Version` |
| `KwDecorAim` | the aim ray, the frame and the cells' anchor |
| `KwDecorGrid`, `KwDecorCell` | the cells and what each is |
| `KwDecorRead` | one reading over frames, a pass a step |
| `KwDecorArea` | what a reading found (`KwWallSpot` too) |
| `KwDecorRays`, `KwDecorSurface` | each cell's surface; floors, obstacles, water, paint |
| `KwDecorWalls` | wall faces |
| `KwDecorDecks` | floors: docks, open floors, lanes, bands, ends and tips, edges, access |
| `KwDecorGround` | paths, distance and way to water, the shore, walls' ways |
| `KwDecorRuns` | runs along rows and columns, the breadth-first spread |
| `KwDecorKeepClear`, `KwDecorClearRules`, `KwDecorClear` | the pieces round the area a batch at a time, what each keeps clear; the ways between and the marking |
| `KwDecorFields`, `KwDecorTable` | path, wall and heart fields, entrances, the summed tables |
| `KwDecorBox` | a turned rectangle on the ground: containment, overlap, distance |
| `KwDecorKind` | the decorations with metadata (`Find` by id), their solid boxes |
| `KwDecorScene`, `KwDecorScenes` | a scene's data (`KwSceneMember`, `KwFacing`, `KwNeed`, `KwLike`) and the list |
| `KwDecorShape` | `KwRect`, `KwScenePiece`, `KwSceneShape` (a scene as this game has it), `KwSiteFrame` (a scene at a place) |
| `KwDecorSites` | `KwSiteSearch` (a scene's search), `KwDecorFacing` |
| `KwDecorLikes` | needs and likes |
| `KwDecorStage` | a scene tried whole at a place |
| `KwDecorEdges` | the docks' edges as runs (`KwEdgeRun`) |
| `KwDecorQuay`, `KwDecorHeads` | the quay's furniture, the pier heads and fishing gear on a pier; `KwRunTask`, `KwOnceTask` |
| `KwDecorLights` | the lantern poles over the docks' piling stations (`KwDockMooring.Over`), facing the dock's middle |
| `KwDecorMoorings` | the quay's mooring posts: over a dock section's piling stations, else against the edge; their solid test past the dock's curb |
| `KwDecorTiers` | the layout's last step: each decoration of a dock's wood in the tier going with where it stands |
| `KwDecorLayout` | the layout's tasks over frames, densities, scene names |
| `KwDecorPlan` | `KwSlot`, a planned decoration, `KwDecorFoot` (footprint, test points), `KwDecorPose` |
| `KwDecorFitCheck` | the cell test and spacing |
| `KwDecorWorld` | the world's part: `KwDecorRules`, `KwDecorSolids`, the edge's line |
| `KwDecorGhosts`, `KwDecorOutline`, `KwDecorHud` | previews, the square, the lines |
| `KwDecorCost` | sum, can pay, the part that can be paid, the station, the cost line |
| `KwDecorPlacer`, `KwDecorUndo` | placing over frames, the last batch taken back |
| `KwDecorWords` | every word |

## Patched

`Player.TryPlacePiece` (prefix: the tool's click; the tool is never placed), `ZInput.GetMouseScrollWheel` (postfix: 0
for the game while Alt + wheel is the tool's, read past it by the tool; the Dock tool's `KwToolWheel` is a second
postfix on the same method, each owning the wheel only while its own tool is in hand), `Player.UpdatePlacementGhost`
(prefix: the driver made when the tool is first in hand), `Menu.Update` (prefix: Esc lets a locked area go). All four
targets checked offline (the scratchpad's patch check, 2026-11-03). Shared lines elsewhere: `KwHammer` (the tool after
the decorations), `KwBuildMenu.Kit` (in Broadside's entry), `KwToolHint.Takes` and `KwToolPanel.IsTool` (its messages
on the tools' hint line, its description in the tools' taller box), `Broadside.InstallPieces` (`KwDecorTool.Install`
after the decorations), `KwDockDef.Decor` and `KwDockside.Configure` (`m_noInWater` cleared for a `Wet` piece).

## Checked offline

An offline harness (the session's scratchpad `decorharness`, 2026-11-03) compiles this folder's plain-arithmetic files
(the grid, cells, floors, ground, keep-clear marking, fields, tables, scenes, sites, stage, edges, quay, layout, fit
check) against small stand-ins for Unity's vectors and the world (`KwDecorWorld` with no wards and the scenario's solids;
the decorations' metadata read from the Dockside files), reads four synthetic places the way the rays would (a 64 m
shore with the dry dock, a Broadside dock 3 m wide with a branch, a path, a longhouse, a shed and a workbench; 48 m of
open ground with houses, a shed, paths, a workbench and rocks; a 40 m harbour with a 16 by 21 m dock of the game's
floors; a 40 m beach with a 2 m walkway pier 32 m long), lays them out with two seeds and renders each top down at
densities 2, 3 and 5 (cells by kind, lanes, access and kept-clear zones, paths, the dry dock, every piece's footprint
coloured by scene with an arrow to its front). What the images showed and changed: posts placed first crowded the
beacon off its pier head (pier heads now go first); a 3 m dock's half-metre bands took no lantern (the measured reach
into the lane); a pier head's corners went to its sides (a corner now points to the end); a salvage heap stood on a
wide dock's deck (scenes that belong on the land need it); the gear store's anchor stood alone where its spar rack did
not fit (every filler now stands against a core piece); the notice board went to a workbench (entrances are now dock
entries and paths at the area's side); a fender kept a lantern pole's spacing across the edge (hung and standing pieces
keep none). The timing above is from the same harness.

## Not checked yet

Compiled and checked offline only (2026-11-03), never run in game: the tool in the hammer and its icon; the plain wheel
zooming with the tool in hand and Alt + wheel neither zooming nor turning (beside the Dock tool's own wheel postfix); the
square on ground, docks and water up to 64 m; [ ], N, Esc and Ctrl + Z; the reading's real time on a 64 m area (the rays
and the piece list, estimated above) and whether a frame ever hitches; the reading itself on the game's floors, the Dock
tool's docks (curbs, joints, branches as the Dock tool now opens them), walls, a house, a slope, a beach, a painted path,
a wide yard; the keep-clear zones' sides (whether a station's and a chest's working side is the piece's +Z, as assumed);
every scene's look with the real models (the gaps written from the footprints; the lean-to's roof over the nail barrels,
the tool wall inside the shed's back against a building, the hull frames along the water, the steam box turned round in
a mirrored scene), the quay's pieces against a real dock's curbs, the lantern poles over the piling stations (the
post on the piling's axis, no cairn in the preview or placed, the lamp over the deck toward the middle, the curb cut
round it), the mooring posts over the piling stations (flush with the deck's side, their curb cut once placed, their
solid test past it), the wood tiers picked on each dock
material, on a game floor and on the shore near a dock (and their previews and cost), none broken far along a section
(the support fix, `Docks/DOCKS.md` "Support"), the fenders over shallow water, the pilings off a pier head; the previews' see-through look and their spread over frames;
the lines' length with many scenes; the cost line's colours; placing two a frame in a scene's order (the sawhorses on
their shavings), the partial Shift + click, refunds on undo; a dedicated server and a second player seeing the pieces.
The mooring post and stone anchor placed by hand at the waterline and in shallow water (`m_noInWater` cleared) are not
checked either. Aiming the tool at a launched ship says the ship refuses it (the rider rule's message): not tried.
Placing pauses while the player's placement ghost stands on a launched ship: not tried.

## Decisions to confirm

- Alt + wheel and [ ] size the area before the click and set the density after it; resizing a locked area is Esc and
  size again.
- The area 6 to 64 m across, 2 m a notch, 24 m at first; 0.5 m cells at every size (spread over frames, no coarser cells).
- Five densities named a few touches, a light, working, busy and bustling yard.
- The scenes, their pieces, gaps, densities and facing rules above; a second carpentry yard (density 5) and fishing
  corner (4) only in large areas; the gangplank in no scene; steam bending facing the dry dock (or the yard's heart).
- Quay rhythms: posts every 7 m staggered, fenders between, lantern poles every 11 m alternating; the beacon at the
  outermost pier head, the bell at the next; fishing gear only on docks 4 m wide or more; a lantern at a narrow pier's end.
- Lantern poles over the docks' own piling stations (2026-11-03, the user's words: "it literally needs to attach to the
  existing docks pole. And not create a brand new pole."): the post on the piling's axis, the cairn left off, the lamp
  toward the dock's middle; a station holds one mooring post or lantern; game floor docks get none. The dock lantern
  (`BS_dock_lantern`, its own post beside the dock) is placed by hand only.
- Wood tiers (2026-11-03, "when auto decorating it should automatically select the items that fit wood-wise with the
  dock"): a deck within 1.5 m decides; on a game floor core wood only for hard wood, darkwood or log pieces; on the shore
  (in the water or within 4 m of it) the nearest dock section within 12 m; anywhere else plain.
- Mooring posts on a dock section over its piling stations, the free one nearest each place in the rhythm (a station
  with a branch, ladder or post passed over).
- A piece against a dock's edge may reach into the lane while 1.6 m plus 0.5 m stays clear across.
- Spacings lowered for the quay's rhythm (mooring post 2 to 0.8 m, beacon 4 to 1.5, vane 3 to 1.5, lantern pole 3 to 1,
  bell 2 to 1, pilings 1.5 to 1); the old themes, "most an area" and weights removed with the old scatter.

# Docks - the Dock tool

Built 2026-11-03 from the user's words: "We also need some kind of dock tool that can be used to build docks, maybe you
have a start point and an end point and use ALT and scroll to adjust width, or something kinda like the mod for making
roads, but for docks we need the ability to make a dock starting point an existing dock. Also needs to be able to be
made from wood, core wood, fine wood."

Real docks on land and water, built with the hammer at a workbench and paid for, unlike the dry dock's free design
pieces. Code in this folder; names `BS_` (not ship parts), classes `KwPier*` ("dock" names are taken by the dry dock,
`Dock/`, and the dockside decorations, `Dockside/`).

## How it plays

- **The tool.** Hammer, Misc tab (and Broadside's build menu entry under Transportation): **Dock tool**, after the
  dockside decorations. It needs a workbench in range, as any piece built there (the game's own check before every
  click; the build panel shows the workbench).
- **Material: the mouse wheel** steps wood, core wood, fine wood (round). **Width: Alt + wheel**, 1.5 to 6 m in 0.5 m
  steps (3 m at first). Both are kept for the next dock this session, and every dock is built at them, one started on
  a dock too. While the tool is in hand the wheel is the tool's: the view does not zoom and nothing turns (Alt + wheel
  included). The build panel's lines say the material and width, the keys, and what the next click does or why not.
- **Aiming before the first click** shows a 2 m stub where the dock would start, at the tool's width and in its
  material, see-through (red where it may not start); a turn of the wheel changes it at once. On the shore it runs the
  way the camera looks; on a dock, the way it would carry on.
- **The preview** (the stub, then the whole dock) is the dock as it will be built: every section, board, joist, cap
  and piling at the width, length and material set, in that material's own texture as tiled on the built dock,
  see-through (only its nearest faces show, over everything behind), made again on every change of the aim, width
  or material.
- **First click: the start.**
  - On the ground or the shore: the deck is level 0.1 m over the ground there. Within the game's building reach (8 m,
    the tool 5 m more), and at least 0.5 m over the sea.
  - On a dock, within 1.5 m of an end no other section carries on from: the new dock **continues it straight on**
    from that end, at its deck height.
  - Anywhere else on a dock: the new dock **branches off square** from the side aimed at, from the deck's edge, its
    centre line at the nearest half-metre along (at least 0.75 m from the section's ends).
  - A start on a dock is at that dock's deck height; its width and material are the tool's (the wheel's), as the stub
    shows, before and after the click.
  - Never on the water, a ship or another piece (the message says so).
- **Then the whole dock follows the aim**, see-through, red with the reason while it may not be built, and the build
  panel shows its cost as the requirement (the game's have/need colours). Its length is the level distance from the
  start to where the aim meets the water or the ground (on a dock: along the way that dock fixes), to the half-metre,
  2 to 40 m (an aim past 40 m builds 40 m, "the longest" said). A free start runs toward the aim.
- **Other docks on the way are joined, never in the way** (user, 2026-11-03: "it should just try to attach it to it";
  "if ... it goes through another dock, still allow it ... we should allow docks to connect"; `KwPierMeet`): an end
  aimed on another dock, or up to 1 m short of it (its posts, fenders and lanterns included), ends exactly at its near
  edge, not rounded; a dock whose line crosses another is laid in runs, up to its near edge and on again from its far
  edge, each run starting joined; the preview shows the runs, the panel "across a dock". The new deck's centre line and
  both its sides are measured, so an angled crossing leaves a small wedge rather than lying on the other deck. Only docks
  within 1 m of the new deck's height count; a run under 1 m is left out (the docks already meet there).
- **Second click: the end**, on the water or the ground (aimed over everything, where the aim crosses the deck's
  height). The dock is built and paid; "Wood dock built, 18.5 m long: 39 Wood".
- **Cancel**: Esc (the game menu does not open) or a right click (the build menu does not open) forget the first
  click; so does putting the tool away. A start on a dock removed meanwhile is cancelled.

### Limits (each refused with its message)

| Rule | Message |
| --- | --- |
| at least 2 m long | "Too short: a dock is at least 2 m long" |
| deck at least 0.5 m over the sea | "Too low: the deck would be 0.2 m over the sea (0.5 m at least)" |
| deck at most 8 m over the water or ground at any station | "Too high: the deck would stand 9.4 m over the water 12 m out (8 m at most)" |
| pilings at most 20 m (deck top to the ground) | "Too deep for pilings 30 m out (20 m at most)" |
| no ground above the deck past its first station | "The ground rises through the dock 6 m out" |
| no other dock section inside its deck beside its line (one it runs into or crosses is joined instead) | "Another dock is in the way" |
| no dock ladder hanging where its deck would be | "A dock ladder is in the way (the hammer takes it down)" |
| no no-build place, no ward the player may not use, at every station | the game's `$msg_nobuildzone`, `$msg_privatezone` (the ward flashes on the click) |
| start within reach, on land or a dock | "Too far away to start a dock", "Start the dock on the shore or on a dock, ...", "Docks are built on the shore, not on a ship" |
| the material in hand | the game's "$msg_missingrequirement" before the click; "This dock needs 12 Fine wood" at it |

## What a dock looks like

A core wood or fine wood section (frame: x across, y up from the deck's top, z along; plain wood is the rough tier,
below):

- **Deck boards** across the full width, a row every board's width plus a gap, each cut into lengths (butt joints
  staggered row by row, from the section's seed), each board a hair higher or lower than the next and with its own
  place in the texture.
- **Joists** along under the boards, one about every 0.9 m across (at least two).
- **A cap beam** across under the joists at each station; stations evenly spaced, at most 2 m apart, both ends
  included (none at a joined start: the dock it meets has its own there).
- **A pair of pilings** at each station under the deck's edges, from the cap beam down to the ground found by a cast
  when built, sunk 0.3 m into it (none where the ground is at the cap beam already).
- **A low curb** along each side edge on the deck, **left out where another dock meets that side** or a dock ladder
  hangs from it (see "Joints"); none across the ends. Its runs end square, or capped logs (core wood).

### The rough wood dock

Plain wood is the cheapest tier and looks knocked together (2026-11-03, the user's words: "The normal wood docks need to
look more... rugged, maybe with some smaller pieces too, more un-uniform pieces. It's the crappiest tier of dock.";
`KwPierRough`, `KwPierRoughFrame`, `KwPierRoughPole`). Core wood and fine wood stay as above, the better tiers.

- **Boards** in rows across the deck, each row its own width (0.14 to 0.30 m) with uneven gaps between rows (6 to 20 mm,
  one row in eight 25 to 50 mm), each row cut into short boards of their own lengths (0.45 to 1.7 m, many butt joints),
  5 to 6 cm thick. Every board a little askew (up to half a degree), tilted along and across, wide ones cupped, each at
  its own height (about a centimetre either way), its ends ragged past the deck's sides (3 cm short to 5 cm over). One
  row in fourteen has its end board at one side broken short (0.1 to 0.35 m, the end jagged), over the joist there and
  only at the outer edge. A few short offcuts (one per 9 m² or so, 0.35 to 0.7 m) nailed across the boards, well in from
  the sides. The first and last rows lie square, so sections carried on meet edge to edge.
- **Joists** of square timbers along under the boards, each its own width (8 to 12 cm) and a little off its line, cut
  into runs of 2.5 to 4 m at slightly different heights.
- **Cap beams** of square timbers, each its own width (14 to 18 cm), a little askew, its ends past the sides by odd
  amounts (2 to 18 cm).
- **Pilings**: crooked round poles with patches of bark left on, each its own thickness (0.8 to 1.3 of the material's
  0.2 m), a little thicker at the foot, the top just under the cap beam a little off its mark, leaning 1 to 3.5 degrees
  inward or along the dock (never out past the deck's edge, where ladders and moored ships come), bent twice on the way
  down to its saved foot. Nearly half are lashed to their cap beam with rope (three turns round the top, two loops over
  the beam).
- **The curb**: pieces of odd lengths (0.6 to 2.4 m), heights (8 to 12 cm) and widths (9 to 13 cm), set a little in from
  the edge and askew, butted or with small gaps (2 to 12 cm) between.

Where another dock meets a side, or a ladder hangs from it, the boards there end flush and whole, the caps end at the
edge and the curb is left out (`KwPierGaps.Openings`). Everything comes from the section's seed, its draws the same
whatever meets it (a branch or a ladder built later changes only the boards' ends, the caps' ends and the curb at it;
checked offline over five seeds), so every peer and every load builds the same, and wood docks built before take the new
look on load. Only the mesh is uneven, by a few centimetres: the colliders are the same flat deck, curb runs and straight
piling boxes as before. A 10 m by 3 m section is about 7,200 vertices (core wood's and fine wood's about half that); a
40 m by 6 m preview about 44,000, built in about 9 ms offline.

### Joints

Where a branch leaves a dock's side, the dock's curb is cut away across the branch's whole width (and its collider with
it), so the two decks run into each other flush, with no lip; the branch has no curb or cap beam across its start, and
its own curbs (and joists) start at the dock's edge, capped. The same where any two sections meet edge to edge at the
same deck height, facing each other: a dock carried on from an end, a branch that straddles two sections of the dock
it leaves (each cuts its own part), two docks built side by side. A run of curb left shorter than 0.15 m between two
openings is left out too.

Nothing about a joint is saved or sent: every peer works out each section's openings (`KwPierGaps`) from the sections
loaded there, from their saved shape (`bs_pier`) and where they stand, when the section builds, and again the frame
after a section meeting it is built or goes (removed, broken or unloaded), when its curbs and timbers are made again if
a side's openings changed. So docks built before joints were cut, late joiners and a dedicated server all build the
same. A branch out of a peer's loaded area (only at its edge, 100 m and more away) leaves that peer's curb across it
until it loads.

A dock ladder ("Dock ladders" below) opens its side's curb the same way, 0.35 m either side of it (`KwPierGaps.Ladders`,
worked out the same way from the ladders loaded, again when one is built or goes), so a player climbing up steps off
flush; it is not a join (the edge stays open and over the water there). A mooring post standing over a side's piling
("Mooring posts on a dock" below) opens it 0.24 m either side of its axis, so the curb ends against the post, the same
way (`KwPierGaps.PostSpan`, from the posts loaded, `KwDockMooring.Loaded`).

| Material | Item | Boards (width, gap, thickness, longest) | Timbers | Look (its own atlas, `KwPierAtlas`) | Health, support |
| --- | --- | --- | --- | --- | --- |
| Wood | `Wood` | the rough tier: 0.14 to 0.30 m, 6 to 50 mm, 5 to 6 cm, 0.45 to 1.7 m (above) | square joists and caps, round pole pilings 0.16 to 0.26, an odd curb (above); colliders as before: joists 0.10 x 0.18, caps 0.16 x 0.18, posts 0.20, curb 0.12 x 0.10 | weathered plain planks in the wood floor's brown, half of them grey, each its own (knots and knot holes, nail holes every 0.6 m with rust runs, long splits, stains, darker wet edges): 64 narrow and 64 wide board strips, 40 square-timber strips, 16 pole strips (bare weathered wood with bark patches), 4 rope strips | 1000, wood |
| Core wood | `RoundLog` | 0.30 m, 15 mm, 8 cm, 2 m | round logs: joists 0.20, caps 0.24, pilings 0.30, curb 0.16 (diameters) | heavier, darker split-pine boards with resin runs and big knots; dark furrowed pine bark round the logs (branch stubs, scraped patches); log ends with rings and a bark rim: 104 board strips, 24 bark strips | 1500, hard wood |
| Fine wood | `FineWood` | 0.18 m, 6 mm, 5 cm, 2 m | square: joists 0.10 x 0.16, caps 0.14 x 0.16, posts 0.20, curb 0.10 x 0.09 | light, clean, pale planed boards (cream to light honey, fine tight grain, few small knots, calm relief), timbers and posts in the same pale wood, no bark: 188 board strips, 64 timber strips | 1200, wood |

Each material wears its own 1024 px atlas, painted in the asset workshop (`ValheimAssets/Assets/Ships/Keelwright/
PierPlanks`, 2026-11-03, after the user found the fine wood dock looked like bark): `bs_pier_<material>.png` and its
normal map `_n.png`, embedded, point filtered, on a copy of the kit's plank wood material (the game's Custom/Piece,
drawn white; `KwPierLooks`). The atlas is long strips with the grain along u, 512 px (11.9 m) at 43 px a metre: a row
of 64 end-grain tiles, then the timber (or bark) strips, then the board strips (`KwPierAtlas`, mirroring the
workshop's `atlas.py`); wood's (repainted rougher 2026-11-03) has between them the pole strips (24 px round, periodic),
the rope strips (8 px round) and the narrow board strips (8 px), its wide boards 12 px (0.28 m). Every board, timber,
log length, pole and rope (timbers, logs and poles cut every 4 m) takes a strip of its kind chosen from the section's
seed in the order the parts are made, never one of the last few of its kind chosen (eight boards, four timbers), at a
random place along it that leaves it room; a rough board takes the narrow or wide strips nearer its own width and its
width across the whole strip, so both its edges show; its ends take an end-grain tile (`KwPierUv`). The same on every
peer and every load; a dock built before keeps its seed and is redrawn in the new look.

## Cost

All in the material's one item, rounded up per section:

- 1 per 2 m² of deck,
- 1 per 4 m of length (joists, caps, curbs),
- 1 per 4 m of pilings.

A 10 m by 2 m wood dock 1 m over 2 m of water: about 23 Wood. A 20 m by 3 m one over 2.5 m of water: about 55. The
same in core wood or fine wood costs the same count of `RoundLog` or `FineWood`. Built at a workbench in range (the
player's, as any piece). The whole cost is given back when a section is removed with the hammer, and dropped when one
breaks (the game's own drop: each section's requirements are its own cost, `m_recover`). With the world's no-build-cost
key nothing is taken and nothing given back, as the game's pieces.

## Dock ladders

The ladder tool on a dock (2026-11-03, the user's words: "We also need to allow the ladder tool to be used on the dock to
wear a ladder: if you place it kind of on the side of a dock, it will get a length that goes down to the water.";
`KwPierLadder*`):

- **Selecting it**: the kit's own **ladder tool** (hammer, Misc tab, or Broadside's build menu entry under
  Transportation: the same tool that makes the dry dock's ship ladders). There is no separate entry. Aimed at a dock
  built with the Dock tool, or at a game floor's edge over the water, it hangs a dock ladder; aimed at anything else it
  is the ship design tool as before (a hatch seen through a game floor stays the hatch's).
- **Where**: on the edge of the deck nearest the aim (within 1.2 m of it): a side, at the nearest 0.25 m along it, kept
  0.35 m in from the section's ends; or an end or start no other dock carries on from, at the nearest 0.25 m across
  it. Its top stands on the edge at the deck's top, turned square to the edge, facing out over the water. On a game floor
  (a piece with "floor" in its name and a level box collider, the game's or another mod's): the nearest side of its box
  the same way, when no other piece carries on past it; the ladder in wood (core wood on a hard wood floor).
- **Its length**: straight down from the deck's top to 0.4 m under the water's surface there (`Floating.GetLiquidLevel`
  where it hangs, the waves as they are at the click, never above 0.4 m under the calm sea), or to the sea floor where
  that is shallower, to 5 cm; 0.6 to 10 m.
- **Its look**: in the dock's material and atlas (`KwPierLadderShape`): two rails standing just off the deck's edge from
  the foot to a little over the deck, rungs every 0.3 m from the foot up (the top one 0.4 m or less under the deck), a
  hook from each rail lying back 0.3 m over the deck's edge. Wood: crooked bark-patched pole rails, slats of odd lengths
  nailed across their front, each rail lashed under its hook with rope; core wood: logs, round rungs, log hooks; fine
  wood: square planed rails, dowel rungs, square hooks. The curb under it is left out ("Joints").
- **The hover**: the ladder see-through where it would hang, in its material, and in the middle of the screen when it
  changes "Dock ladder, down into the water: 3 Wood" (or "down to the sea floor"), or why not.
- **The click** builds it, as any piece: the game's workbench check before it (the tool's station), then the cost
  checked as the game counts it (a mod building from chests counts too, `KwBpCost.Missing`), the piece placed the game's
  way (`Player.PlacePiece`: the hammer's swing, the place effect) and the cost taken (`KwGamePay`): "Wood dock ladder
  built, 1.6 m down into the water: 2 Wood".
- **Cost**: one of the dock's material's item a metre of ladder, rounded up (a 1.6 m ladder: 2 Wood, `RoundLog` or
  `FineWood`), all given back when it is removed with the hammer and dropped when it breaks (its requirements, set from
  its saved length on every peer). None with the world's no-build-cost key.
- **Climbing**: as the kit's ladders (`KwLadderClimb`): E within reach of it, from the water (swimming beside it) puts the
  player on the deck behind its top, where they fit standing; from the deck, at its foot in the water, facing the ladder
  to climb back up.
- **Held up by its dock**: while a dock section loaded on the ladder's owner holds its top (or one near it is still
  loading) it has its material's full support; with none (the section removed or broken) it has none and the game
  breaks it, dropping what it cost (`KwPierLadderSupport`, a `WearNTear.UpdateSupport` prefix: the game's own check
  measures support by the distance to a section's start, up to 12 m away, and would drop a ladder far along a
  section). A ladder on a game floor keeps the game's own support check. Nothing is built against it (`m_supports` off).
- **The Dock tool** refuses a dock (a branch or an extension) whose deck would cover a dock ladder: "A dock ladder is in
  the way (the hammer takes it down)".

| Refused | Message |
| --- | --- |
| aimed at the deck away from its edges | "Aim at the edge of the dock where the ladder should hang" |
| an end another dock carries on from | "Another dock carries on from there" |
| a branch within 0.35 m along the side | "A dock branches off there" |
| another dock ladder within 0.8 m | "Another ladder hangs there" |
| aimed at a dock ladder | "A dock ladder hangs here already (the hammer takes it down)" |
| no water under the edge (land, or the sea floor above the water) | "No water under this edge for a ladder" |
| shorter than 0.6 m, longer than 10 m | "The water is too close under this edge for a ladder", "The water is too far down here (10 m of ladder at most)" |
| out of the game's building reach | "Too far away" |
| a no-build place, a ward the player may not use | the game's `$msg_nobuildzone`, `$msg_privatezone` (the ward flashes on the click) |
| a game floor: away from its edge, another piece past it | "Aim at the floor's edge where the ladder should hang", "Another piece carries on past this edge" |
| the dock section not built yet on this game | "This dock has not finished building here yet" |
| the material in hand | "This ladder needs 2 Wood" |

## Dock lanterns

A lantern on a post of the dock's own wood beside the dock (2026-11-03, the user's words: "the lights on the dock need to
not have the stone at the bottom; they should look like they're just attached to the side of the dock ... sit on top of a
post that goes down into the water, off to the side, and automatically have the post pointing toward the inside of the
dock so we have a uniform placement"; `Dockside/KwDockLantern*`):

- **The piece**: hammer, Misc tab (and the Decoration and Lighting filters), **Dock lantern** (`BS_dock_lantern`), right
  after the lantern pole. The workshop's lantern head (`bs_dock_lanternhead`: the lantern pole's glass lantern and iron
  arm on an iron cap, no post, no cairn) on top of a post made in code. At a workbench.
- **Where**: aimed at a dock section's deck within 1.2 m of an edge (`KwDockLanternEdge.Dock`): on a side, halfway between
  the two piling stations nearest the aim (clear of the pilings and the rough wood's cap beam ends); on an end or start no
  other dock carries on from, at the nearest 0.25 m across it, 0.3 m in from its corners. Aimed at a game floor's edge
  (a level box with "floor" in its name, as the dock ladders read one) over the water: the nearest side of its box, wood
  (core wood on a hard wood floor). The post stands just outside the edge (3 cm off the deck's side), square to it, and
  the head's +Z (its arm and lantern) points in over the deck: every dock lantern stands the same way. Aimed anywhere
  else it is refused and says so ("A dock lantern stands beside a dock: aim at a dock's side (the lantern pole stands
  anywhere)"), the ghost red.
- **The post**: from 1.8 m over the deck's top (the head's foot; its lantern hangs from about 1.7 m to 2.2 m over the
  deck, 0.41 m in from the post's axis) straight down to the sea floor or the ground under it (one cast when placed,
  sunk 0.3 m, to 5 cm; saved), in the dock's material and atlas (`KwDockLanternShape`, `KwPierLooks`, `KwPierUv`): wood a
  crooked bark-patched pole 0.26 m thick (a little thicker at its foot, bent twice, lashed to the deck's side with two
  loops of rope), core wood a 0.30 m round log, fine wood a 0.26 m planed square post; all fit the head's 0.306 m cap
  skirt unscaled. Its box is the post from the head down to its foot, on the piece's layer; the head keeps its
  `col_box_lantern`.
- **Cost**: the dock's material's item, one per 2 m of the whole post (at least 2), and the lantern's own 2 Bronze and
  2 Resin; the build panel shows it while aimed at a dock (the piece's requirements are the aimed site's cost; at rest a
  short wood post's: 2 Wood, 2 Bronze, 2 Resin). The game takes it at the click as any piece's; all of it is given back
  when it is removed (its requirements set from its saved post on every peer).
- **Lit** from dusk to dawn as the lantern pole (`KwDockLantern`: a small torch flame in its oil cup, a copy of the game's
  wall torch flames at a fifth of their size, and a warm point light at its `light` point, the world's clock, screens
  only); behind clear glass (the bundle's own see-through material, the flame sorted after it so it shows whole from
  every side), the wick's ember glowing by day.
- **Held up by its dock**: beside a dock section, that section holds it at its material's full support wherever along
  it it stands, and with the section gone it falls (dropping what it cost), even though its post stands on the sea floor
  (`KwPierLadderSupport`, `KwDockLanternPiece.Held`). Beside a game floor the game's own support holds it.

| Refused | Message |
| --- | --- |
| not a dock or a game floor's edge | "A dock lantern stands beside a dock: aim at a dock's side (the lantern pole stands anywhere)" |
| the deck away from its edges | "Aim at the dock's side where the lantern should stand" |
| an end another dock carries on from, a branch within 0.3 m | "Another dock carries on from there", "A dock branches off there" |
| another dock lantern within 1 m, a dock ladder within 0.6 m | "Another dock lantern stands there", "A dock ladder hangs there" |
| aimed at a dock lantern | "A dock lantern stands here already (the hammer takes it down)" |
| a game floor not over the water | "A dock lantern stands beside a dock, or a floor over the water" |
| the ground at or over the deck there; nothing within 20 m down | "The ground rises past the deck here", "Too deep here for the lantern's post (20 m at most)" |
| reach, no-build place, ward | "Too far away", the game's `$msg_nobuildzone`, `$msg_privatezone` (the ward flashes on the click) |

The lantern pole (`BS_dock_lanternpole`) is the light the Auto-decorate tool puts along docks: on a dock it is seated on
one of the dock's own pilings ("Mooring posts on a dock"), and this dock lantern, on a post of its own beside the dock, is
placed by hand only (the user, 2026-11-03: "it literally needs to attach to the existing docks pole. And not create a
brand new pole.").

## Mooring posts on a dock

A mooring post (any wood tier) or the lantern pole placed on a dock section's deck within 0.8 m of a side edge snaps over
the nearest piling station on that side (within 1.6 m along it) that has a piling (2026-11-03, the user's words: "have
those also sit directly on the posts that go into the water, but on top of the dock, so where the post goes in the water
the mooring post is above that, but it needs to look natural", then "the mooring posts need to look more seamless ...
there's like a piece that's kind of hanging off the side", and for the lights "it literally needs to attach to the
existing docks pole. And not create a brand new pole."; `Dockside/KwDockMooring`), so it reads as the piling carried on
up through the deck's edge, square to the edge:

- **the lantern pole** on the piling's axis (`KwPierDeck.PilingIn` in from the edge, at `KwPierDeck.Pilings`), its hewn
  post (0.21 m, the wood piling 0.20 m) rising from the deck as the piling's top, turned so its lamp (the model's -X)
  hangs in over the deck toward the dock's middle, without its stone cairn: every triangle with a corner under 0.45 m and
  more than 0.13 m from the post's axis left out of a trimmed copy of its mesh (made once), its cairn's box switched off
  (`Dockside/KwDockTrim`), decided on every peer from where it and the sections stand (looked for every second for 20 s
  while they load; nothing saved), the hammer's ghost and the tool's previews drawn so;
- **a mooring post**, whose bollard (0.34 to 0.40 m) is thicker than the piling, moved in from the piling's axis until
  its back is 0.19 m in, flush with the deck's side, still over the piling, so nothing of it hangs past the edge; its
  rope coil (the model's +Z) in over the deck.

A station where another dock branches off (within 0.35 m), a dock ladder hangs or another mooring post or lantern pole
stands is passed over for the next one. The side's curb is cut round it, 0.24 m either side of where it stands ("Joints":
the curb ends against the post), on every peer from where the pieces and sections stand, made again when one is placed or
goes. Anywhere else (the shore, a game floor, the middle of a deck) it places as before (a lantern pole with its cairn).
The player's chosen piece is kept: a plain mooring post stays plain on a core wood dock. It rests on the deck, and the
deck holds it at full support ("Support").

## Support

The game holds a piece up (`WearNTear.UpdateSupport`, on its owner) by the pieces its boxes touch, each giving its own
support less a loss for the distance between the two pieces' origins (or centres of mass). A dock section's origin is at
its start, up to 12 m away, so a piece built on a deck more than about 7 m along a 12 m section (9 m in core wood) found
too little and broke (found when the dock ladders were made; half the lantern poles the Auto-decorate tool stood on docks
broke so). Since 2026-11-03 the one prefix on that method (`KwPierLadderSupport`) decides it for the docks:

- a dock ladder hanging from a dock section, and a dock lantern standing beside one, are held by that section (also while
  a section near it is still loading) at their material's full support, and with none they have none and fall;
- any other piece resting on a section's deck (`KwPierRest`: one of its boxes as the game reads them, `WearNTear.m_bounds`,
  each collider's box grown 0.15 m, meets the deck's box from the cap beams' underside to the curb's top) has its own
  material's full support, as on the ground, wherever along the section it stands: the game's pieces, the decorations,
  mooring posts over the pilings, fenders hung on its side;
- a dock section itself, a ladder or lantern beside a game floor and every other piece keep the game's own check.

Only pieces near a loaded section's deck are looked at (a level distance from its middle first, no engine call, then the
boxes); with the section removed, what rested on it has the game's own check again.

## Sections

A dock longer than 12 m is laid as equal sections of at most 12 m (a 30 m dock: three of 10 m), each its own piece:
removed, repaired and broken on its own, each refunding its own share. Every section after the first is joined to the
one before (no pilings or cap at its start; the boards, joists and curbs run on), so the dock looks continuous. A dock
continued or branched later is joined the same way. Each section is a piece of its own so a ship ramming one or a
player removing one takes only that part, and no single piece's mesh or collider list grows unbounded.

## Contract

| What | Names |
| --- | --- |
| Tool (hammer) | `BS_tool_pier` ("Dock tool"), a copy of `wood_floor`, its look a 1.4 by 1.6 m wood section, never placed itself |
| Sections (never in the hammer) | `BS_pier_wood`, `BS_pier_corewood`, `BS_pier_finewood` ("Dock (wood)", ...), copies of `wood_floor`: Piece, WearNTear (health and material type by material, support wear on, no flying fragments, the wear states the model), ZNetView; at a workbench (for removal) |
| ZDO key | `bs_pier` (string): `pier1|length|width|joined|seed|l,r;l,r;...`, metres to the centimetre: the section's length and deck width, 1 when its start is joined, its boards' seed, and each station's left and right piling foot below the deck's top (0: none). Stations: `ceil(length / 2) + 1`, evenly from start to end. |
| Where it stands | the piece at its start, on the centre line, at the deck's top, facing along it (`transform.forward`); its material is its prefab |
| Colliders (every peer, a dedicated server too) | children `bs_pier_deck` (the deck from its top to the cap beams' underside), `bs_pier_curb` (one per run of curb: none across a joint, made again when the joints change), `bs_pier_piling` (one per piling); all on the piece's layer (`piece`) |
| Joints | derived on every peer from the loaded sections, never saved or sent (`KwPierGaps`, "Joints" above) |
| Snap points | inactive children tagged `snappoint` at the deck's top: its four corners, its centre line's two ends, and both edges at every inner station |
| Requirements | each section's own cost, recovered (set on every peer from its text) |
| Dock ladders (never in the hammer; the kit's ladder tool builds them) | `BS_pier_ladder_wood`, `BS_pier_ladder_corewood`, `BS_pier_ladder_finewood` ("Dock ladder (wood)", ...), copies of `wood_floor`: Piece (at a workbench), WearNTear (0.4 of its material's section health, its support rules, supporting nothing), ZNetView, `KwLadderClimb`, `KwPierLadder` |
| Its ZDO key | `bs_pier_ladder` (string): `ladder1|length|seed|dock` (or `floor`): its length in metres to the centimetre from the deck's top down to its foot, its look's seed, and what it hangs from (a dock section: held by it, its curb cut; a game floor) |
| Where it stands | the piece at its top, on the deck's edge at the deck's top, facing out over the water (`transform.forward`); its material is its prefab |
| Its collider | child `bs_pier_ladder_climb`: one box round its rails and rungs, 0.62 m wide, from 2 cm off the edge to 13 cm, from its foot to 0.1 m over the deck's top, on the piece's layer (nothing over the deck) |
| Its requirements | one of its material's item a metre, rounded up, recovered (set on every peer from its text) |
| Dock lantern (hammer) | `BS_dock_lantern` ("Dock lantern"), a copy of `piece_table` (as the dockside decorations) wearing the `bs_dock_lanternhead` bundle lifted 1.8 m, a post made in code (child `bs_lantern_post` of `dock`) and its box (`bs_lantern_post_box`), `KwDockLantern`, `KwDockLanternPiece`; at a workbench; `m_noInWater` cleared |
| Its ZDO key | `bs_dock_lantern` (string): `lantern1|length|material|seed|dock` (or `floor`): the post's length in metres to the centimetre from the deck's top down to its foot, the dock's material id, its pole's seed, and what it stands beside (a dock section: held by it; a game floor) |
| Where it stands | the piece where the post's axis meets the deck's top, just outside the edge, facing in over the deck (`transform.forward`) |
| Its requirements | its post's material's item one per 2 m of post (at least 2), 2 `Bronze`, 2 `Resin`, recovered (set on every peer from its text) |

## Reading a dock

For other code (the Decorate tool, `Decorate/`), read-only, on any peer with the section loaded:

- `KwPier.Loaded`: every dock section loaded here (`KwPier.Spec` is null until it has built).
- `KwPierDeck.Of(Piece)` or `KwPierDeck.Of(KwPier)`: the section's deck, or null for any other piece or one not built.
- `KwPierDeck.Near(Vector3 point, float radius)`: every built section whose deck comes within the level distance.
- On a `KwPierDeck`:
  - `Top` (the deck's walking height, level all along), `Start`, `End` (the centre line's ends at the deck's top, world),
    `Forward`, `Right` (level unit vectors), `Width`, `Length`, `Material` ("wood", "corewood", "finewood"),
    `JoinedAtStart`, `Piece`.
  - `CurbWidth`, `CurbHeight`: the curb on each side edge, on the deck (clear walking width: `Width - 2 * CurbWidth`),
    except across its edge's `Joins`.
  - `At(across, along)`: a point on the deck's top (across: metres right of the centre line).
  - `Distance(point)`: level distance to the deck.
  - `ContinuedAtEnd()`, `ContinuedAtStart()`: another loaded section carries on from that end.
  - `Edges`: four `KwPierEdge`s, "left", "right", "start", "end", each with `A`, `B` (its ends at the deck's top, a side's on the
    curb's outer face), `Outward` (level, out of the deck), `Water` (over the water: the ground under its pilings below
    the sea's level at half or more of its stations; an end only when open and its pilings are), `Joins` (where other
    loaded sections meet it: spans `(from, to)` in metres along it from `A` toward `B`, in order; open into the other
    deck there, no curb, not water; a side's are its branches), `Open` (no other section meets it anywhere: `Joins`
    empty, and for an end or start none continues it there).
- The pilings stand under the side edges at each station, `KwPierShape.PostX(spec, kind)` either side of the centre
  line and `KwPierShape.PostZ(spec, kind, i)` along, from the cap beam (`KwPierKind.CapFoot` below the top) down (the
  rough wood's poles are drawn a little off that, inside the edge; their colliders are there).
- `PilingIn` (how far in from a side edge the pilings' axes stand), `CapFoot` (the deck box's underside below its top),
  `Pilings(bool right)` (the stations along one side, metres from the start, where that side has a piling).
- Dock ladders: `KwPierLadder.Loaded` (each with its `Spec`: `Length`, `OnDock`; null until built), each standing at a
  deck edge at the deck's top, facing out. A side's curb is open 0.35 m either side of each (`KwPierGaps.Ladders`, and
  with the joins `KwPierGaps.Openings`); they are not in an edge's `Joins` (it stays `Open` and over the water there).
  So is it round each mooring post over a side's piling (`KwDockMooring.Loaded`, 0.24 m either side).
- Dock lanterns: `KwDockLanternPiece.Loaded` (each with its `Spec`: `Length`, `Kind`, `OnDock`; null until built),
  standing just outside an edge, facing in.

## Multiplayer

- The tool's clicks, aim, preview, panel lines, wheel and cancel run on the placing player's game only; nothing of them
  is sent.
- Built through the game's own placement: each section is `Player.PlacePiece` on the placing player's game (a
  networked piece, creator set, the game's place effect and statistics, the hammer's swing for the first), and in the
  same frame its shape is written to its new ZDO and built (`KwPier.Take`), so the game's support check never sees it
  without its pilings. The cost is taken by the game after the click (the tool's requirements set to it), as any
  piece's; a mod that builds from nearby chests counts and pays there too (`KwBpCost.Missing`, `KwGamePay`).
- Every peer builds each section from its ZDO when it appears (`KwPier`): colliders and snap points everywhere, a
  dedicated server included (the owner's support check needs the pilings), the mesh only where there is a screen. The
  piling feet were cast once, by the builder, and saved, so every peer builds the same; nothing else is sent or saved.
- The joints (curbs cut where sections meet) are worked out on each peer from the sections it has loaded, when one
  builds and again when one meeting it comes or goes (`KwPierGaps`); no peer writes another's ZDO for them.
- Removal and breaking are the game's own (`WearNTear.Remove` to the owner, `Piece.DropResources` there), each
  section's requirements its own cost on every peer, so whoever owns it gives the right refund.
- A dock ladder is placed the same way (`Player.PlacePiece` on the clicking player's game, its shape written to its new
  ZDO and built in the same frame, `KwPierLadder.Take`); every peer builds its climb box (a dedicated server too) and its
  look where there is a screen, its requirements from its saved length. Its length was found once, by the builder, from
  the water and the sea floor there, and saved. Each peer cuts the curb round it itself (`KwPierGaps`, when it or the
  section builds and again when it goes). Its support is checked on its owner, from the sections loaded there. Climbing
  is the climbing player's own (the game sends the player's position as always). Nothing else is sent.
- A dock lantern is placed through the game's own click (`Player.TryPlacePiece`, the game taking its cost as any
  piece's); in the same frame its post (length, material, seed, dock or floor, found by the placing player) is written to
  its new ZDO and built (`KwDockLanternPiece.Take`); every peer builds its box (a dedicated server too) and its look where
  there is a screen, its requirements from its text. Its light is each screen's own from the world's clock. Its support
  is checked on its owner. A mooring post is an ordinary piece; where it stands over a dock's piling each peer cuts the
  curb round it itself (`KwPierGaps`). Nothing else is sent.

## Patched

`WearNTear.UpdateSupport` (prefix: a dock ladder or dock lantern held by its section, and a piece resting on a deck at
full support, `KwPierLadderSupport` with `KwPierRest`; every other piece the game's own), `Player.UpdatePlacementGhost`
(postfix: the dock lantern's ghost at its site and its shown cost, a mooring post snapped over a piling,
`Dockside/KwDockPlace`), `Player.TryPlacePiece` (prefix and postfix: a dock lantern refused with its reason, its post
written once placed, `Dockside/KwDockLanternClick`; prefix: the tool's clicks, `KwPierTool`; the ladder tool's click on a dock goes
through the kit's own prefix, `KwTools`, to `KwLadderTool.Click`), `Player.UpdatePlacementGhost` (postfix: the wheel,
the game's ghost hidden, the preview, the panel lines and shown cost, `KwPierPreview`; the ladder tool's dock hover
through the kit's `KwPreview`), `ZInput.GetMouseScrollWheel`
(postfix: 0 for the game while the tool owns the wheel, `KwToolWheel`), `Menu.Update` (prefix: Esc cancels a first
click), `Player.UpdateBuildGuiInput` (prefix: a right click cancels it, `KwPierCancel`), and `ZNetScene.Awake`
(BundlePrefabs' `OnSceneAwake`: the prefabs). All targets and parameters checked offline (the scratchpad's patch
check, 2026-11-03, again with the ladder's support prefix the same day, and again with the dock lantern's and mooring
post's patches: 104 patch classes, no problems).

## Tried in game

The user built a dock with branches on 2026-11-03 (FinePointPen, fine wood, 3 m): the docks build, sections join and
branch, and the plain wheel reaches the tool (its material read live through DevBridge after the user's turns). They
found, and these were fixed the same day (compiled, not yet run):

- "There is still the edge of the first dock where the second dock comes off of it": the curb ran on across every
  branch. Now cut across it on every peer ("Joints"); the user's existing docks were checked offline against the new
  rule (every branch, one straddling two sections, found).
- The wheel's material and Alt + wheel's width did not show in the preview: aimed at a dock, the stub was drawn at
  that dock's width and material whatever the wheel said, and the first click on a dock set the tool's width and
  material back to that dock's, so a turn of the wheel before it was lost (every dock built that day is 3 m wide; the
  panel's line did change). Now the tool's width and material are used everywhere. The preview's look also hid the
  material: the sprite shader drew the texture without the material's tiling (the planks' `-0.56 x 0.12`, a squashed
  stripe), at a third of its opacity, every face behind another blended in; now the tiling is in its UVs, it is
  drawn over a depth pass (nearest faces only) at 55 %.

## Not checked yet

The dock lantern, mooring posts over the pilings and the support fix (2026-11-03) are compiled (no warnings) and their
patches checked offline (104 patch classes), and the posts and heads checked offline only: the mod's own mesh code built
a 6 m section of each material with a lantern post halfway between stations 1 and 2 and its curb opened round a mooring
post over station 1, rendered in Blender with the atlases, the workshop's lantern head and mooring post FBX at the
mod's poses (the session's scratchpad, `lanternharness/out`): the head's cap sits on each post, its arm in over the deck,
the mooring posts stand over the pilings with the curb ending against them. Not seen in game yet: the hammer's ghost
snapping beside a dock and its post's length, the refusals' words, the shown and taken cost (and with a chest mod), the
light at night, a lantern beside a game floor, a lantern loading before its dock, its fall with its section; a mooring
post snapping on a deck (and a tier's), the curb opened and closed again when one is placed and removed, a post loading
before its dock; the support fix on a long section (a game piece, a decoration, a fender far along a 12 m section
staying up, and falling once the section is removed), its cost on a large base by docks; two players, a late joiner and
a dedicated server. The lanterns' clear glass and torch flame (2026-11-03, the dock lantern's and the lantern pole's) are
seen in Blender only: in game, the glass drawing see-through with its sheen, the flame inside the glass and seen through
it from every side, on at dusk and out at dawn on every screen.

The rough wood look and the dock ladders (2026-11-03) are compiled and checked offline only: their meshes built by the
mod's own code outside the game and rendered in Blender with the atlases, point sampled
(`ValheimAssets/Assets/Ships/Keelwright/PierPlanks/out/review/rough/`: `rough_dock_overview.png`, `rough_dock_deck.png`,
`rough_dock_ladder.png`, `dock_ladder_wood.png`, `_corewood.png`, `_finewood.png`), a side opened later moving only the
boards, caps and curb at it, and the support prefix's target checked. Not seen in game yet:

- the rough look under the game's light: the new wood atlas (greyer, splits, rust runs, poles, rope), the board ends,
  gaps and broken ends at a low angle, the rope lashings, the curb pieces; a wood dock built before redrawn in it; the
  tool's icon; the preview of a long wide wood dock (about 9 ms to build offline at 40 by 6 m);
- the dock ladder: the hover and its words over a dock, a dock's end and a game floor (wood floors have a level box
  collider; other mods' floors may not); the click, the cost taken (and with a chest mod), the length at a calm sea and
  in waves, a ladder to the sea floor in shallow water; climbing it from the water and from the deck (swimming players
  pressing E, the landing behind its top with the curb cut, facing the ladder in the water); the curb cut and closed again
  when it is built and removed; its support while its section stands and its fall (with the refund dropped) when the
  section is removed; a ladder loading before its dock; the hammer removing it; the Dock tool refusing a branch over it;
  two players, a late joiner and a dedicated server owning it;
- the kit's ladder tool on the dry dock unchanged (hatches, open edges, ladders on cabins, at sea).

Compiled and its patches checked offline only (2026-11-03):

- the three atlas looks under the game's light (checked only in the workshop's lineup beside `wood_floor`,
  `wood_pole_log` and `wood_fine_stack`): fine wood's brightness in the Meadows sun, the point filtered board edges
  and strip seams at a low angle (Broadside's ship textures flickered point filtered), the end tiles on cut ends,
  the bark's join round a log, the tool's rendered icon;
- the preview's new look (the depth pass and the tiled texture at 55 %, the red tint) and the panel's two lines (the
  game's box may shrink them);
- the joints in game: the curb cut across each branch on a screen and in the colliders (walking across with no step),
  made again when a branch is built or removed, a late joiner and a dedicated server seeing the same, core wood's capped
  log ends at a cut;
- the wheel owned (no zoom, no turn; a touchpad), Alt + wheel changing the width (read the same way as the material's
  wheel, Alt through the Hotkeys library; not seen in game yet) and with another mod's Alt + wheel (EarthWright's is the hoe's);
- Esc and right click cancelling without opening the menus;
- the starts: the reach, a sloping beach (the first station's ground above the deck), a dock's end, start and side,
  a dock section's end already continued (offered as a side branch);
- the piling casts over rocks, bushes and steep sea floors; the support (every section held up by its pilings in the
  ground, a piece built on the deck held by it: by the game's rule only within about 4.4 m of its section's start, where
  the section's origin is, Decorate/DECORATE.md "Support"); a ship or boat bumping the pilings;
- the cost shown and taken, a mod building from chests, the refund on removal and the drop when broken;
- the game's snapping of floors and walls to the deck's snap points;
- two players (a second screen building the same mesh), a late joiner, a dedicated server owning a section.

## Decisions to confirm

- The rough wood is plain wood only, and only its look changed: its cost, sizes for the colliders, health and support
  are as before. Its numbers (board widths and lengths, gaps, broken ends one row in fourteen, offcuts one per 9 m²,
  nearly half the pilings lashed, leans up to 3.5 degrees) are my choice from the user's words.
- Dock ladders are made with the kit's ladder tool (no new hammer entry), on a dock's sides and its open ends (the user
  said "on the side of a dock"; a pier's end is where a ladder is wanted most), and on a game floor's edge over the water.
- A dock ladder's length is fixed when it is built: 0.4 m under the water's surface as it is at the click, never above
  0.4 m under the calm sea, so a ladder built at a wave's crest still reaches the calm water. It does not follow the
  waves or a terrain change later.
- A dock ladder costs one of the dock's material's item a metre (as the kit's ship ladders cost a wood a metre); core
  wood ladders cost `RoundLog`, fine wood `FineWood`; a floor's ladder wood (core wood on a hard wood floor).
- It is held only by the dock section under its top; with the section gone it falls even if its foot stands on the sea
  floor.
- The support found with the dock ladders (a piece far along a section breaking) is fixed since 2026-11-03 ("Support"):
  anything resting on a deck has its own material's full support, as on the ground; a piece touching only a piling or
  the underside of the deck box counts too (its boxes meet the deck's box), one touching only a piling below it does not.
- Dock lanterns (2026-11-03): one prefab whose material is the dock's (saved in its ZDO, as the ladders' length), not one
  per material; the post's top 1.8 m over the deck; its wood one per 2 m of post (at least 2) and the lantern's own 2
  Bronze and 2 Resin (the lantern pole's 2 Bronze and 2 Resin, without its post's wood and cairn's stone); on a side
  halfway between two piling stations, on an open end too; beside a game floor only over the water; the post 3 cm off the
  deck's side, the rough wood's lashed to it with rope, core and fine wood's not; posts of 0.26 m (wood, fine wood) and
  0.30 m (core wood), all under the head unscaled; its hammer icon rendered from the piece (the head on a short wood post),
  not the bundle's head alone.
- Mooring posts on a dock (2026-11-03): snapped within 0.8 m of a side and 1.6 m along it, only to stations with a piling
  on that side; the curb cut 0.24 m either side; a plain post stays plain on any dock (the player's choice, the user's
  words).

- A dock started on a dock is built at the tool's width and material, not the met dock's (until 2026-11-03 it took the
  met dock's, which hid the wheel's changes from the preview); the tool keeps both for the session, so branches match
  their dock unless the wheel is turned.
- Pilings stand under the deck's edges (not outside them), so a branch meets a dock's edge flush; the curb is cut
  across every joint (the user, 2026-11-03), also between two docks built side by side.
- The preview shows only the new dock: the curb it will cut on the dock it leaves goes when it is built.
- Cost rates (deck 0.5 a m², length 0.25 a m, pilings 0.25 a m), the same count for all three materials.
- Limits: 2 to 40 m long, 12 m sections, deck 0.5 to 8 m over the water or ground, pilings 20 m, 1.5 to 6 m wide.
- The material on the plain wheel (as the Post cap tool's styles), so the view does not zoom with the tool in hand;
  no gamepad control for the width or material yet.

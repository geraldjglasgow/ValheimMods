# DevBridge agent notes

Practical notes for an agent building, editing the world or taking pictures through DevBridge. They were learned
while building a castle and shooting piece screenshots in the `LocalTesting` profile. `REFERENCE.md` has every
endpoint; this file covers what it doesn't: working recipes, piece measurements and the traps.

`examples/castle/` holds the Python scripts that built that castle (1,900 pieces in about two minutes). Copy them
rather than starting from scratch. The centre, ground height and player id are set at the top of `cs.py`:
- `cs.py`: the HTTP helpers.
- `castle_lib.py`: generators for walls, boxes and floors, and support-order placement.
- `stage1.py`: walls, towers and spiral stairs.
- `stage2.py`: the keep and its gable roof.
- `stage3.py`: buildings with props.

`examples/blueprints/` holds saved builds the user can ask for by name ("build the house"): one JSON per build
(pieces in a frame whose door faces -z, in placement order) and `blueprint.py` to use them:
- `pieces.json` is the piece catalogue: name, material, build cost, snap points, size at yaw 0, pivot height and a
  note on the traps (which way a roof rises, how a shutter hinges, what hangs). Read it before designing; add pieces
  with `python measure_pieces.py <prefab...>` (`--all` for every Hammer piece; needs the game running).
- `kit.py` has the building blocks (stone runs, curtain wall with walk, towers, 45 degree roofs, gables, timber
  frame buildings, fences); `compound.py` and `plain_wood_house.py` are designs written with it. Run a design
  script to write its JSON again.
- `python preview.py <name> [--views oblique,top,front,east|cam=x,y,z:tx,ty,tz]` renders a blueprint offline in
  headless Blender from the catalogue's shapes: check layout, heights and overlaps before anything goes in game.
  `previews/` keeps renders of the saved designs.
- `python blueprint.py cost <name>` adds up the materials.
- `python blueprint.py list` names them.
- `python blueprint.py build <name>` builds where the player stands with the door toward where they look
  (`--facing west`, `--at x,z`, `--ground y`; `--dry` checks only). It refuses when built pieces are already in the
  footprint (`--force`), then clears trees, rocks and shrubs, levels a square with EarthWright's admin
  `ew terrain level`, moves the player out in front of the door, places every piece and counts them.
- `python blueprint.py save <name> --center x,z --ground y --facing <door direction>` captures what the player built
  there (`--anyone` for every builder); the area must be loaded.
- `plain_wood_house` (444 pieces): the two-storey 10 x 10 m wood house with campfire and three cooking stations,
  workbench, tanning rack, chopping block, portal upstairs and a black banner. `plain_wood_house.py` is its design
  source and writes the JSON again after a change.
- `compound` (4,414 pieces, never built in game): an 82 x 60 m walled compound with a boat canal out through the
  east wall, wall walk, corner/gate/water-gate towers, great hall, two longhouses, two plain_wood_houses, smithy,
  barn, fenced field. Its site digs the canal, so it needs ground 0.5-2.5 m above sea level (30) with open water
  east of it: `build compound --ground 31.5 --facing <gate side>`. `great_hall`, `longhouse`, `smithy` and `barn`
  are its buildings on their own.
- A blueprint's `site`: `clear_radius`, `stand` (where the player ends up), and either `level_half` (one square) or
  `terrain` steps `{"op": "level"|"paint", "at": [x, z], "half": m, "y": m | "paint": kind}`, run with the
  player moved to each centre (EarthWright levels around the player); `water` makes the build check sea level.

## Ground rules

- The user starts and closes the game. Never launch or kill it from a script. A rebuilt mod DLL loads only after
  the user restarts (`/reload` aside).
- Turn god mode on for any automated session, and put it back afterwards. `/status` reports it as a JSON bool:
  `if not status()["player"]["god"]: console("god")`.
- Run `console("killall")` every ~80 placements and before screenshots. Creatures wander in and kill the player.
- **`/frame` carries the player's character up with the free camera.** When it switches back with `off=1`, the
  player falls from wherever the camera was, which can kill them. If a person is playing, don't use `/frame`. Ask,
  or let them look themselves. Otherwise always call `/frame?off=1` in a `finally`, then teleport the player back to
  the ground.
- Close menus before every screenshot, then check that `status()["screens"]` is empty:
  `InventoryGui.instance.Hide()`, `Minimap.instance.SetMapMode(Minimap.MapMode.Small)`, `Menu.instance.Hide()`.

## /eval

- One expression per call. The helpers are:
  - `$player`, the local player;
  - `$prefab("name")`, a ZNetScene prefab;
  - `$v3(x,y,z)`;
  - `$zdo("id")`, a ZDOID from `/nearby`;
  - `$hover`, `$last`, `$type`.

  Private members work.
- The reply is `Type = value`. Strip it, split on the first `" = "` and strip the quotes. Errors come back as HTTP
  400 or 500 with the message in the body.
- **Not supported:** generic methods like `GetComponent<T>()`. `GetComponentInChildren(Type)` fails too.
- **What does work:**
  - Component shortcuts: `$prefab("x").Chair.m_attachPoint`, `.Ladder`, `.BoxCollider.isTrigger`, `.Piece`.
  - Walking children: `.transform.childCount`, `.transform.GetChild(i).name / .localPosition / .gameObject.tag`.
- Raycasts: `Physics.RaycastAll($v3(..), $v3(dir), dist)`, read with `.Length`, `[i].point.y`, `[i].distance` and
  `[i].collider.transform.root.name`. The hits are unsorted and include terrain (`_Zone(Clone)`).

## Placing real pieces

```
$player.PlacePiece($prefab("stone_wall_4x2").Piece, $v3(x,y,z), Quaternion.Euler(0,yaw,0), false)
```

- What you get: a real networked piece with the creator set. It saves with the world and syncs to other players.
- What it skips: build checks, costs and the station requirement.
- Speed: about 30 ms a call.
- `/lineup` and `/place` copies are different: they are local, still copies, never saved.

Support:
- Place the lowest pieces first.
- **Props that hang or attach** (banners, ceiling braziers, the forge tool rack, hanging food) go in a second pass,
  after the walls and beams they hang from. Placed before them, they break.
- **Props must touch what holds them up.** A prop whose collider sits a few cm above the floor has no support and
  breaks. Put the mesh bottom on the surface, or sink it 0.05.

Checking:
- Wait about 6–8 s, then compare `/nearby` counts by prefab with what you placed.
- Item drops (`/nearby` entries with `item`) mean something broke. The drop names the piece's recipe, so Bronze and
  Chain mean a hanging brazier. Remove the drops afterwards.

Removing without drops:
`ZNetScene.instance.Destroy(ZNetScene.instance.FindInstance($zdo("<id>")).gameObject)`. To touch only your own
pieces, filter `/nearby` on `creator` (the player id).

Structural rules:
- These come from the server, so check them before designing tall or wide builds.
- Vanilla values:

  | Material | Max support | Min support | Vertical loss | Horizontal loss |
  | --- | --- | --- | --- | --- |
  | Stone | 1000 | 100 | 0.125 | 1.0 |
  | Wood | 100 | 10 | 0.125 | 0.2 |

- Cornerstone (`com.orianaventure.mod.Cornerstone.cfg`) overrides these per material. In `LocalTesting` every
  loss is 0.001, so almost anything holds there.

## Measuring a piece

- **Snap points are the most reliable footprint.** Read the children tagged `snappoint` and their `localPosition`.
- **`/lineup?prefabs=X&new=1`**, one prefab per call (several in one call stack in the same spot), then `/clear`:
  - `size` is the bounding box after the lineup's own yaw, so x and z come out inflated.
  - `position.y - row.origin.y` is the pivot height above the mesh bottom.
- **Seats:** a seat's facing is `Chair.m_attachPoint.forward`. Every chair, bench and throne checked faces local +z.

Vanilla pieces (yaw 0, x east, z north):

| Piece | Size and pivot | Orientation |
| --- | --- | --- |
| `stone_wall_4x2`, `stone_wall_2x1`, `stone_wall_1x1` | 4×2, 2×1, 1×1, all 1 m thick; pivot at the centre | runs along x; yaw 90 runs it along z |
| `stone_floor_2x2` | 2×1×2, pivot at the centre | top = pivot + 0.5 |
| `stone_stair` | 1 m rise, pivot at the bottom | |
| `stone_pillar` | 2 m tall, pivot at the centre | |
| `wood_floor` | 2×2, top at the pivot | |
| `woodwall`, `wood_door` | 2×2, pivot at the centre | in the x-y plane |
| `wood_wall_half` | 2×1 | |
| `wood_gate` | 2 wide, 3 tall, bottom 1 below the pivot | |
| `wood_roof_45` | 2×2×2, pivot at the centre | rises toward local −z; yaw 270 rises to +x, 90 to −x, 180 to +z |
| `wood_roof_top_45` | peak 1 above the pivot, eaves at ±1 | ridge along x; yaw 90 runs it along z |
| `wood_wall_roof_45` | right-triangle gable, x −1..1, y 0..2, pivot at the bottom centre | rises toward +x; yaw 180 mirrors |
| `wood_stepladder` | 1 wide, 2 up per 2 along, pivot at the bottom centre | rises toward local −z; walkable; yaw 180 rises to +z |
| `wood_stair` | 2×2, rises 1 | |
| `wood_beam` | 2 m long, pivot at the centre | along x |
| `wood_pole2` | 2 m, pivot at the centre | |
| `wood_fence` | 2 m, spans x −1.1..0.9 | along x; at yaw 0 the pivot is 0.1 east of the centre |
| `piece_table` | 2.5×1.25, top 0.83 | |
| `piece_bench01` | 2.5×0.62 | |
| `hearth` | x ±2, z ±1.5 | leave a ridge gap above it for smoke |
| `jute_carpet`, rugs | about 4×4 | |

Pivot height above the mesh bottom:

| Piece | Pivot height |
| --- | --- |
| `piece_banner01` | +2.93 (hangs below the pivot) |
| `piece_brazierfloor01` | +1.06 |
| `piece_brazierceiling01` | −0.03 (pivot at the bottom; it rises 1.95, so hang it pivot = beam − 2.05) |
| `piece_groundtorch_wood` | +0.65 |
| `piece_walltorch` | +0.33 |
| `wood_fence` | +0.16 |
| `forge`, `smelter`, `piece_oven`, `fermenter`, `piece_workbench`, `piece_preptable` | ≈ 0 |
| `forge_ext6` | +0.69 (wall-mounted) |
| `piece_MeadCauldron` | +0.16 |
| `piece_throne01` | ≈ 0 |

Other mods' pieces:
- **VALKEA** sizes and pivots for all 329 pieces are in `examples/castle/pieces_dump2.tsv` (`rb=` size, `rbc=`
  centre relative to the pivot). `prop()` in `stage3.py` uses it to sit a prop on a surface.
- **`piece_SprialStairsStoneV2`** (VALKEA): half a turn per piece, 3.55 m rise, 2.4 m radius.
  - Steps run from local +x through +z to −x, with a landing at −x. The pivot sits 0.46 above the mesh bottom.
  - Stack them with yaw +180 and +3.55 m.
  - The outer rail closes every side, so the stair can only be left at the top. To get off midway, end a run and
    put a floor over the open half, then start a new run on it.

## Terrain (EarthWright console)

These commands work around the **player**, so teleport there first:
- `ew forestry R`, with a hoe equipped;
- `ew debris R`, with a hoe equipped;
- `ew terrain level R H`;
- `ew terrain paint R paint=paved|dirt|grass|cultivated`.

Levelling is asynchronous: wait a few seconds, then sample `ZoneSystem.instance.GetGroundHeight($v3(x,0,z))`.
Clear everything else in an area with `/nearby` and Destroy, skipping creatures and your own pieces.

Teleporting: `$player.TeleportTo($v3(x,y,z), Quaternion.Euler(0,0,0), true)`. Then poll `/status` until the state
is `ingame` and the position is close, and wait 3–4 s more for zones to load.

## Pictures

- `/screenshot?out=` needs an absolute path; a relative one lands in the game folder.
- `/light?tod=0.38&env=Clear&wind=0,0` keeps the time of day the same across shots. `/light?reset=1` afterwards.
- **Hide the ravens:** `GameObject.Find("Hugin").transform.localScale = $v3(0,0,0)`, and the same for `Munin`.
- **Grass:** `ClutterSystem.instance.ClearAll()` and `ClutterSystem.instance.enabled = false`. Never deactivate
  `ClutterSystem.instance.gameObject`: it is `_GameMain`, and the game freezes.
- **Glare:** outdoors, sun glare washes out close shots. A closed building with a wood floor and fixed light makes a
  steady photo studio.
- **Distance:** `/frame` `radius` sets it. A whole building needs about 1.4× its size. Remember `/frame` moves the
  player (see Ground rules).

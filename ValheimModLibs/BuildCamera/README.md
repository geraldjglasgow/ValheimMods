# BuildCamera

A free build camera for the local player: the view leaves the body and flies inside bounds the mod gives, the body
stays where it stood at rest, and the game's own placement ghost, build, remove, copy and repair aim along the camera's
view and reach from it. Local only: nothing is written to a ZDO or sent; pieces go through the game's own networked
placement, so a dedicated server and the other players need nothing (they see the builder standing still).

- `FreeCamera.Install(harmony)`: once in the mod's `Awake`. Patches by hand, by name, once per merged copy; nothing on
  a dedicated server. Returns the game members it could not find (then nothing is patched and `Enter` refuses).
- `FreeCamera.Enter(player, flight)`: out for the local player, starting where the game camera is (pulled into the
  bounds). False when dead, teleporting, seated with a seat camera, in the game's free fly, or another mod's copy has
  its camera out (an empty `BuildCamera_out` child under the game camera says so).
- `FreeCamera.Leave()`, `IsOut`, `IsOutFor(player)`, `Flight`, `Position`, `Rotation`.
- `CameraFlight(bounds)`: one trip out. `Speed` (10 m/s), `RunMultiplier` (3), `WheelSpeed` (10) `WheelMoves` (when the mouse wheel moves the camera along its view like the game's zoom instead of turning the piece in hand; null, never), `FliesWhile` (when it still flies on the movement keys though a window holds the player's input; null, never), `Reach` (metres from the camera along the
  game's 50 m build ray; default 50), `Collide` (sphere sweep against `terrain` and `static_solid`, on), `AboveGround`
  (off, for caves), `AboveWater` (on), `EscapeLeaves` (on), `LeaveWhenHurt` (on), `Stays` (the mod's own rule, asked
  after every player update), `Ended` (why it came back: `CameraEnd`).
- `ICameraBounds.Clamp(point)`; `BallBounds(centre, radius)`, `SegmentBounds(from, to, radius)` (a capsule round a line).
- `CameraTool.Hold(player, accepts)`: a build tool the mod accepts in hand, equipped from the inventory if need be.

While out:

- Flying: the game's bindings through `ZInput`: movement keys and the left stick pan flat along the view, Jump and the
  right trigger up, Crouch and the left trigger down, Run faster; only while the game takes the player's input and no
  build or radial menu is open. Look is the game's own look input (`Player.SetMouseLook`), so sensitivity and
  inversion apply and the body does not turn.
- The body: `Player.SetControls` holds it at rest (no walking, jumping, crouching, running, autorun, dodging, blocking
  or attacking). The game's place mode reads its build keys itself, so building, the build menu and turning pieces work
  as usual.
- Reach: `Player.PieceRayTest`, `RemovePiece`, `CopyPiece` and `UpdateWearNTearHover` start their ray at the camera but
  measure from the player's eyes; a prefix (run first) casts the same ray and lifts `m_maxPlaceDistance` for that one
  call when the hit is within `Reach` of the camera, a finalizer puts the game's value back. Nothing to restore after.
- View: `GameCamera.GetCameraPosition` returns the camera's pose, the sound listener stays on the camera, light fading
  (`LightLod`) counts from the camera.
- Back: `Leave`, Esc when it would open the game menu (not when it closes something else, which keeps WindowInput's
  windows first), death, a teleport, a cutscene, a seat camera, the game's free fly, damage (`LeaveWhenHurt`), the
  mod's `Stays`, or a logout (the trip belongs to the player who was local). The game camera's zoom is put back.

```csharp
FreeCamera.Install(new Harmony(PluginGuid + ".camera"));
if (CameraTool.Hold(player, item => item.m_shared.m_name == "$item_hammer"))
	FreeCamera.Enter(player, new CameraFlight(new BallBounds(table.position, 30f)) { Stays = p => p.InPlaceMode() });
```

Merged into the consuming mod with ILRepack like every library here. Consumer: Broadside (the blueprint table's
Build ship camera round the dock). OpenKeep's `BuildCamera/` section is the same rules and is to move onto it.

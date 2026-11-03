# Cargo crane

A land-built cargo crane and the cargo crate it lifts (asked for 2026-10-25: players need a way to unload cargo from
Broadside ships without carrying every item off). Made as an Asset Lab bench and put into Broadside on 2026-11-02 (the
user's words: "put the crates you just created into broadside, and the crane too"), unchanged but for its registration
(`NetPrefabs.Register`) and its install in `Broadside.cs`. Broadside's `CLAUDE.md` rules hold (methods at most 24 lines,
classes at most 300, prefix/postfix only, multiplayer first, no settings, English words in the code). Folder
`Broadside/Broadside/Crane/`; its bundles `kw_cargocrane` and `kw_cargocrate` are embedded from `Broadside/assets/bundles`.

## Identity

The prefabs start with `KWC_`, not `KW_`: Broadside treats every `KW_` piece as a ship part (blueprints, snap rules,
undo, the dock waterline), and these are land pieces.

| What | Names |
| --- | --- |
| Pieces (hammer, Misc tab, at a workbench) | `KWC_cargo_crane` (30 wood, 10 stone, 4 deer hide, 6 leather scraps; a copy of `piece_workbench` without its crafting station), `KWC_cargo_crate` (10 wood, 4 leather scraps; a copy of `piece_chest_wood`, 6 x 4) |
| Bundles (ValheimAssets) | `kw_cargocrane` (`Assets/Ships/Keelwright/CargoCrane`), `kw_cargocrate` (`Assets/Ships/Keelwright/CargoCrate`), embedded by `LabBundle` lines |
| Crane model contract | parts `turn` (turret, about Y), `boom` (slides along the turret's +Z, 0 to 8.5 m: the hook 3.5 to 12 m out from the mast; 4.5 m until 2026-11-02, when the user asked for 50% more reach), `drum` (windlass, spins about X), `head` (hangs under the tip); points `operator`, `mast_pulley`, `tip`, `drum_rope`, `hook`; boxes `col_box_base`, `turn/col_box_mast`. `KwCraneRig.Arrange` hangs boom, drum and the points under `turn`, `tip` under `boom`, `hook` under `head` |
| Crate model contract | part `lid` (opens `Euler(-100, 0, 0)` about its hinge), points `sling_0`..`sling_3`, `lift`; box `col_box_body` |
| Crane ZDO (its owner writes) | `kwc_turn`, `kwc_slide`, `kwc_drop` (the pose), `kwc_operator` (the operator's character), `kwc_crate` (the crate on the hook) |
| Crate ZDO (its owner writes) | `kwc_crane` (the crane it hangs from), `kwc_hang_yaw` (its turn against the turret), `kwc_afloat`; its items are the game chest's own `items` |
| RPCs | on the crane: `KWC_Operate` (take or leave), `KWC_OperateReply`, `KWC_Hooked` (the crate's answer); on the crate: `KWC_Hook`, `KWC_Release` |
| Patches | `Player.SetControls` (prefix: the crane takes Jump, Crouch, Attack, Block while worked), `Container.Interact`, `Container.RPC_RequestOpen`, `RPC_RequestStack`, `RPC_RequestTakeAll` (prefixes: a hooked crate is shut), `Piece.CanBeRemoved` (postfix: nor removed), `ObjectDB.Awake` (postfix: the hammer); the pieces on `ZNetScene.Awake` through BundlePrefabs |

## How it plays

- E at the crane works it like a ship's helm, one operator at a time; the operator stands on the turret and turns with
  it. Game bindings: Left/Right turn (30 degrees a second), Forward/Backward slide the boom out and in (1.2 m/s), Jump
  raises and Crouch lowers the head (1.5 m/s, 0.6 to 12 m under the tip), Run doubles them, Attack picks up, Block sets
  down, Use (E) leaves. While working it, a box at the bottom of the screen lists these keys (`KwCraneKeys`: the game's
  own boxed tooltip, under the HUD, centred, its bottom a tenth of the screen up).
- Pick up: with the hook lowered onto a crate's top (within 0.75 m across), the crate is hooked, the head draws its
  sling tight and lifts it 0.3 m; its four sling ropes run from its corner loops to the hook. Normal chests are never
  picked up.
- Set down: the crate goes straight down onto what is under it (ground, floor, a deck, another piece), or floats
  where that would be under water (its bottom 0.35 m under the calm surface, riding the waves on every screen). A
  floating crate can be hooked again. Lowering stops where the hook, or the crate's bottom, meets something.
- A hooked crate cannot be opened, stacked into, emptied or removed with the hammer.

## Multiplayer

- E asks the crane's owner, who allows one operator (a gone one counts as none), names them in the ZDO and hands the
  crane's ownership to them, the way a chest is handed to whoever opens it. The operator's game moves the crane and
  writes its pose ten times a second; every other screen eases toward the ZDO's pose. The owner frees the crane from an
  operator gone three checks running.
- A crate hangs only while both name each other: the crane its crate (the crane's owner writes it first, then calls the
  crate), the crate its crane (the crate's owner writes it, unless the crate is open or on a crane still holding it).
  Every screen draws it under that crane's hook from the crane's pose; the crate's owner keeps the crate's ZDO place up
  with it, so it loads where it is.
- Letting go: Block, the operator leaving (E, a jump out, death, logging out), the crane changing owner, or the crane
  being destroyed. The crane forgets the crate and calls it; the crate's owner sets it down under where it hung. The
  crate's owner also sets it down by itself when the crane has not named it for three seconds (the crane let go, was
  destroyed, or never got the answer), and a free crate with nothing left under it settles again every two seconds.
- No items are ever moved: the crate is the game's chest with its items in its own ZDO, never destroyed or rebuilt by
  the crane, so a lift can neither lose nor double anything.

## Left for later

- Crates aboard Broadside ships (the user: "the crate is a placeable object, but don't worry about it now"). Until
  then a crate set on a ship's deck stays where it was set in the world; when the ship moves off, the settle check
  lets it fall or float.
- A hanging crate passes through walls when swung sideways (only lowering stops at what is under it).

## Not checked yet

Built 2026-10-25 and checked offline only (compiled with no warnings; every patch target and parameter checked against
the game's DLL). Not yet seen in game: the models, materials and icons, working the crane (keys, the operator on the
turret, the rope), hooking and setting down, floating, two players and a dedicated server, the crane removed mid-lift.

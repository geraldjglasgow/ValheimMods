# CLAUDE.md - EliteBuildingPieces

New building pieces for the hammer, each a copy of the closest game piece wearing its own model from `../ValheimAssets`.
Version 0.1.0, unreleased. Required on the server and every client (the pieces are networked prefabs).

## Core wood (section `1. Core Wood`, switch `Core Wood Pieces`, on)

| Prefab (save key) | Copies | Bundle (workshop source) | Cost | Health |
| --- | --- | --- | --- | --- |
| `EBP_CoreWoodWall` | `stake_wall` | `va_corewoodwall_v002` (`Assets/Props/CoreWoodWall/v002`) | 4 RoundLog | stakewall's (1000) |
| `EBP_CoreWoodWallLarge` | `stake_wall` | `va_corewoodwall_double_v003` (`CoreWoodWall/Variants/DoubleWidth_v003`) | 8 RoundLog | 2x stakewall |
| `EBP_CoreWoodDoor` | `wood_gate` | `va_corewoodgate_single_v002` (`Assets/Props/CoreWoodGate/v002/Single`) | 6 RoundLog | wood gate's (400) |
| `EBP_CoreWoodDoubleDoor` | `wood_gate` | `va_corewoodgate_double_v002` (`CoreWoodGate/v002/Double`) | 12 RoundLog | 2x wood gate |

All four sit in the hammer's Building tab right after the wood gate, at the workbench (the base pieces' station).

- **Model** (`CoreWood/CoreWoodModel.cs`): the base piece's children, root renderers, colliders and LOD group are
  stripped; the bundle prefab goes in as child `corewood`; every renderer wears the game's `logwall` material from
  `wood_wall_log` whole (the models are UV-mapped onto the game's core wood atlas). `WearNTear.m_new/m_worn/m_broken`
  point at the model, `m_wet` and `m_fragmentRoots` are cleared (the stakewall's fragment roots were its stripped
  models). The bundle's own Animator is destroyed. The walls' `col_box_*` come mirrored from Blender (negative scale,
  which BoxCollider ignores: the walls found no ground and broke a second after placing); `Unmirror` makes the scale
  positive and mirrors the centre. Icons: the bundles' `<bundle>_icon` sprites, rendered by
  `ValheimAssets/Assets/Props/CoreWoodWall/icons.py` (the first in-game renders were empty).
- **Snap points** (`CoreWood/CoreWoodSnaps.cs`): the bundle anchors `snap_*` move to the piece root (the game reads
  only direct children), tagged `snappoint`, inactive, named `$hud_snappoint_bottom N` / `$hud_snappoint_mid N` like
  the stakewall's. 2 m pieces: x = -1, 1 at y = 0 and 2 (the stakewall's); 4 m pieces also the middle. The walls'
  `col_box_wall_01` (the stakewall's slanted brace collider) is tagged `leaky` like the game's.
- **Doors** (`CoreWood/GateSwing.cs`): the game's `Door` and the wood gate's root Animator (`door_animator`) stay, so
  use, keys, ward check, sounds, the ZDO state and the use timing are the game's. The animator binds nothing on our
  model; a `Door.SetState` postfix hands the state to `GateSwing`, which turns `door_left` (-1) and `door_right` (+1)
  to state x 100 degrees about Y in 0.7 s: state 1 opens towards +Z, away from a user behind the door. Runs on every
  peer, the dedicated server included (the leaves carry colliders); off between swings; snaps on first state.

- **One wall line** (`CoreWood/WallLine.cs`): the 4 m pieces are exactly two 2 m walls side by side (end posts at
  x = -1.806 and 1.803 like the walls'; the double door's leaves meet at 0). A `Player.IsOverlappingOtherPiece`
  postfix treats `stake_wall` and the four pieces as one family by footprint: none snaps onto a family piece's line and
  course overlapping it along the line (the game alone only stops the same prefab at the same spot, so a stakewall
  snapped into a core wood wall).

Words: `$ebp_corewood_wall`, `_wall_large`, `_door`, `_door_double`, each with `_desc` (`CoreWood/CoreWoodModule.cs`).
Patched: `ZNetScene.Awake` (prefix: build and add prefabs; postfix: hammer), `ObjectDB.Awake` (postfix, last: hammer),
`Door.SetState` (postfix), `Player.IsOverlappingOtherPiece` (postfix), `Localization.SetupLanguage` (postfix: words).

## Not checked yet

Never run in game (2026-10-08): placement and snapping against stakewalls and each other, collisions, the leaves'
swing direction, the double door's centre, hover text, wear/destruction fragments, a dedicated server. No
`thunderstore/icon.png` yet.

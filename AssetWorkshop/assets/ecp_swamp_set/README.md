# Five swamp creatures

Original accessory meshes fitted to the local vanilla reference export. Runtime
implementation: `EliteCreaturesPack/EliteCreaturesPack/Swamp`; gameplay and testing
checklist: `EliteCreaturesPack/features/swamp-creatures.md`.

| Creature | Kit | Vanilla animated body |
| --- | --- | --- |
| Mire Jarl | ecp_mire_jarl + ecp_mire_crown | Draugr_Elite |
| Reed Stalker | ecp_reed_stalker | Draugr |
| Bog Maw | ecp_bog_maw | Blob |
| Fen Crawler | ecp_fen_crawler | Neck |
| Drowned Shade | ecp_drowned_shade | Wraith |

The kit meshes and texture atlases are original. Existing bodies, animations,
effects and sounds are reused from the running game. These are five new creature
prefabs built from vanilla creatures, not five new skeletal rigs.

## Visual direction

Studied local vanilla Draugr Elite, Draugr, Blob and Wraith, plus Neck as the
small-creature rig reference. `reference_review.py` renders those actual assets.
Keep dark olive, peat brown, dirty bone and black iron; use faceted silhouettes,
small albedo atlases and sparse detail that reads under swamp lighting.
The Jarl wears broken iron lamellar and a low jagged crown. The stalker has a
ragged reed mantle and wicker eel trap. Bog Maw has a dead-root crown and bone
mouth. Fen Crawler has overlapping bark plates. The shade bears a burial yoke,
chains and a cracked bell. None introduces broad neon glow or glossy armour.

## Build

From the workspace root in PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -Command "& './AssetWorkshop/build.ps1' -Asset ecp_mire_jarl,ecp_mire_crown,ecp_reed_stalker,ecp_bog_maw,ecp_fen_crawler,ecp_drowned_shade -Bundle ecp_swamp"
Copy-Item AssetWorkshop/out/bundles/ecp_swamp.windows,AssetWorkshop/out/bundles/ecp_swamp.linux EliteCreaturesPack/EliteCreaturesPack/assets/bundles
```

`review.py` loads each baked kit onto the local vanilla reference and writes
`out/swamp_lineup.png` and one editable fitted `.blend` per creature. Run using
Blender `--background --factory-startup --python review.py`. These fitted review
files contain vanilla reference geometry and stay local; never bundle or ship
them. The standalone kit `.blend`, FBX and atlases are in each asset's `out/`.

The lineup shows rest-pose fittings with independent framing, not relative size
or an in-game screenshot. Runtime applies creature size settings after fitting.
The separately equipped stalker spear is absent from the rest-pose board.

Validated: six kit exports, local fitting previews, Unity size checks and both
platform bundles. In-game animation fitting, combat balance and dedicated-server
play testing remain necessary before release.

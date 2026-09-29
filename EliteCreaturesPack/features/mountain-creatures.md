# Mountain creatures

Five encounters for the Mountain biome, built from vanilla animated bodies with original low-poly
accessories. Their surfaces follow the swamp reference study: broad worn shapes, coarse textures,
dark earth and stone colors, and limited pale accents. Existing vanilla creatures remain available.

| Creature | Prefab | Base health | Role |
| --- | --- | ---: | --- |
| Frostfang | `ECP_Frostfang` | 1400 | Rare oversized wolf miniboss |
| Rimeback | `ECP_Rimeback` | 650 | Compact heavy grazer with a slate carapace |
| Scree Wing | `ECP_ScreeWing` | 240 | Horned flying drake hunter |
| Cairn Wight | `ECP_CairnWight` | 450 | Bone-masked nocturnal Fenring ambusher |
| Ice Crawler | `ECP_IceCrawler` | 180 | Shale-crested ground predator |

Frostfang's bite adds frost damage. At half health it permanently enters rage: its coat pales,
run speed rises 25%, and it stops circling its target. The zone owner records this phase in
`ecp_frostfang_rage`, so late joiners and ownership changes keep the same phase. It grants no
Forsaken progression key. Rimeback pushes harder with its attacks; Scree Wing divides its
hail damage between blunt and frost; Cairn Wight retains the Fenring leap; Ice Crawler adds
frost to its bite. All five share the MountainMonsters faction, resist frost completely,
and are weak to fire. They cannot be tamed or bred.

Death uses private scaled and tinted vanilla ragdolls. The added mane, plates and mask currently
disappear on death; they are not fitted to the separate ragdoll skeletons.

Settings live in synced sections 15–19 of `com.EliteCreaturesPack.cfg`: Enabled, Health,
Damage Factor, Spawn Chance and Spawn Interval. Disabling wild spawns leaves already-spawned
creatures in the world. Health settings apply to new creatures.

Models are built in `AssetWorkshop/assets/ecp_mountain_set/build.ps1`; its README describes the
asset and local reference preview paths. Windows and Linux bundles are embedded in the mod.
Game-owned bodies, animation clips, textures and sounds are resolved from the running game.

## In-game validation checklist

After rebuilding, restart the LocalTesting profile. In a disposable test world, enable
`devcommands` and spawn each prefab above. Run these checks on a host and a dedicated server
with a second client, both without and with Elite Creatures Reborn:

- Check body and accessory color under daylight and blizzard lighting.
- Walk, run, turn, attack, stagger and kill each creature; inspect attachment movement and corpse behavior.
- Confirm the wolf's miniboss behavior and dodge/block windows.
- Check flying attacks, terrain navigation and collision on steep Mountain terrain.
- Verify normal and starred creatures, loot and localization.
- Allow natural spawns: all five stay within Mountains; Cairn Wight spawns at night.
- Change synced damage and spawn settings; ensure neither base-game creatures nor other clients diverge.
- Check a late join, ownership transfer, world save/reload and log errors.

Build and preview checks do not replace these runtime checks.

## Validation status

Windows and Linux bundles built successfully with five prefabs and matching Blender/Unity
dimensions. The Release mod build passed with zero warnings and errors. Rest-pose fitted
previews and the combined lineup were inspected. DevBridge was unavailable, so the in-game
checklist above remains outstanding; combat balance and animation fit are not yet play-tested.

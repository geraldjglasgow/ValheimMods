# Swamp creatures

Five additional wild swamp encounters, using the game's original bodies, rigs, sounds, hit shapes and combat ownership, with original low-poly accessories made in AssetWorkshop. They belong to the Draugr faction and do not replace the biome's original creatures, crypt inhabitants or spawners. All are available through `spawn <prefab>`.

| Creature | Prefab | Vanilla base | Role and appearance | Base health |
| --- | --- | --- | --- | --- |
| Mire Jarl | `ECP_MireJarl` | Draugr Elite | Rare 1.35x miniboss; corroded crown, burial armour, slower heavy blows and stronger knockback | 900 |
| Reed Stalker | `ECP_ReedStalker` | Draugr | Reed mantle and wicker trap; fast circling spear hunter with a 2.7m piercing thrust | 160 |
| Bog Maw | `ECP_BogMaw` | Blob | Root crust and tusks on a 1.25x poison ambusher; sleeps until approached within 7m or disturbed | 200 |
| Fen Crawler | `ECP_FenCrawler` | Neck | 1.5x armored scavenger with swamp growth; quick bites, pierce resistance, blunt weakness, poison immunity | 100 |
| Drowned Shade | `ECP_DrownedShade` | Wraith | Night-only revenant with a drowned bell and chain harness; flying melee with stronger knockback | 220 |

The Jarl is a miniboss encounter, not a progression boss: it sets no boss defeat key or Forsaken power and has no altar. Natural spawns cannot star it. Other creatures may naturally reach one star (`m_maxLevel=2`); manually spawned higher levels retain vanilla level handling.

## Encounters and configuration

Sections 10–14 each expose Enabled, Health, Damage Factor, Spawn Chance and Spawn Interval. Values sync from the server and are lockable. Enabled controls new natural encounters; existing creatures remain. Health changes apply to newly spawned creatures; attack damage and spawn settings update immediately on reload.

| Creature | Chance / interval | Local cap | Minimum same-creature spacing |
| --- | --- | --- | --- |
| Mire Jarl | 4% / 900 seconds | 1 | 180m |
| Reed Stalker | 12% / 360 seconds | 2 | 45m |
| Bog Maw | 10% / 420 seconds | 2 | 45m |
| Fen Crawler | 18% / 300 seconds | 2 | 45m |
| Drowned Shade | 10% / 480 seconds, night only | 2 | 45m |

All spawn singly in the interior of the swamp through normal zone-owner SpawnSystem entries. These percentages are per eligible attempt, not a percentage of existing creature populations. Drops use existing items: Jarl entrails/chains, Stalker entrails/wood, Maw ooze/bone fragments, Crawler neck tails/bone fragments, Shade chains/coal.

## Implementation and assets

`SwampKind` is the identity table. `SwampPrefabs` builds and registers creatures and private attack items identically on all peers. `SwampAttacks` retains vanilla animation-event attacks; `SwampSpear` replaces the Draugr melee clip with the player's humanoid `Javelin Stab`, using the same capped-hit-event approach as the skeleton arsenal. No custom damage RPC or client damage loop is added.

The `ecp_swamp.windows` and `.linux` bundles contain six original models: `ecp_mire_jarl`, `ecp_mire_crown`, `ecp_reed_stalker`, `ecp_bog_maw`, `ecp_fen_crawler`, `ecp_drowned_shade`. Models are authored at the unscaled vanilla prefab root in rest pose. Runtime instances them there, then reparents with world-position preservation to Spine1 (Draugr), Head (crown), Bone (Blob), Spine (Neck), or spine1 (Wraith), and finally scales the whole creature. Materials use the Draugr creature shader with the kit's point-filtered pixel atlas and low gloss, so solid accessories do not inherit a Blob's special surface effect. Local vanilla reference exports are used only to fit and preview originals; no vanilla meshes or textures are distributed.

Accessories follow their single attachment bone and retain the game's original death effects/ragdolls. They are not separately skinned and do not persist as decorated corpse pieces.

## Validation

- [x] Checked base attack slots and attachment bone names against local vanilla prefab exports.
- [x] Complete Release build: 0 warnings, 0 errors; both `ecp_swamp.linux` and `ecp_swamp.windows` confirmed embedded in the merged DLL.
- [ ] Restart LocalTesting and spawn all five; check material lighting, accessory clipping, spear grip/thrust and hit timing while moving.
- [ ] Verify Bog Maw wakes on approach and damage, Shade flight/night behavior, Crawler resistances and Jarl stagger/knockback.
- [ ] Observe natural swamp spawn caps and chance settings; disable each and confirm existing creatures remain.
- [ ] Dedicated-server plus two-client check: same kits/animations, one attack result, ownership transfer and save/reload.
- [ ] Repeat with Elite Creatures Reborn: normal stars/mutations, no progression boss state added.

No live DevBridge game was available during implementation (`127.0.0.1:7780/status` refused the connection); animation fit and multiplayer behavior require the in-game checks above. The successful Release build writes `dist/EliteCreaturesPack.dll` and copies it into the existing LocalTesting plugin directory.

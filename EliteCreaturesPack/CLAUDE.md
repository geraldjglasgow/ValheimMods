# CLAUDE.md - EliteCreaturesPack

New creatures for Valheim: the crypt mimic, the Greydwarf Slinger, the Rime Giant and the Kraken. The first three were
split out of Elite Creatures Reborn on 2026-09-28, before any of them was released (the Kraken was built here the same
day), so the creatures ship and update on their own and Elite
Creatures Reborn stays a mod that changes the game's creatures. Read the workspace `../CLAUDE.md` first (layout, small
units, multiplayer first, releasing). The behaviour of each creature is in `features/`; the README is the store page.

**Status: 0.1.0, builds clean, offline patch check passes (33 patch classes); never run in game as this mod.** The mimic
was seen in game once while it lived in Elite Creatures Reborn (the ambush worked; the lunge was fixed after). The
slinger, the giant and the kraken have never run in game.

## Identity

| | |
| --- | --- |
| GUID / name / version | `com.EliteCreaturesPack`, `Elite Creatures Pack`, `PluginVersion` in `EliteCreaturesPack.cs` |
| Config | `com.EliteCreaturesPack.cfg` through SyncedConfig: `1 - General` (Lock Configuration), `2 - Crypt Mimic`, `3 - Greydwarf Slinger`, `4 - Rime Giant`, `5 - Kraken`; every entry synced and lockable |
| Prefabs | `ECP_CryptMimic` (+ `_bite`, `_ragdoll`), `ECP_GreydwarfSlinger` (+ `_shot`, `_stone`), `ECP_RimeGiant` (+ `_boulder`, `_ragdoll`, `_sweep`, `_slam`, `_throw`), `ECP_Kraken` (+ `_corpse`); the kraken's loot items `ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken` |
| Status effects | `ECP_KrakenInk` (the ink's screen splats; kept in every ObjectDB by `Kraken/Ink/InkStatus`) |
| ZDO / RPC / global keys | `ecp_mimic_swap`, `ecp_mimic_chest`, `ecp_mimic_open`, `ecp_disguised`, `ecp_rime_plates`, `ecp_rime_wave`, global `ecp_rimegiant_<region>`; kraken ZDO `ecp_kraken_phase`, `ecp_kraken_ship`, `ecp_kraken_anchor`, `ecp_kraken_side`, `ecp_kraken_along`, RPCs `ecp_kraken_slam`, `ecp_kraken_bite`, `ecp_kraken_ink`, `ecp_kraken_flinch` |
| Words | `enemy_ecp_*`, `item_ecp_*` (the kraken's loot: `item_ecp_krakenbeak`, `_krakenmeat`, `_krakenmeatcooked`, `_shieldkraken`), `se_ecp_*`, added in each creature's `*Words` |
| Asset bundles | `ecr_cryptmimic`, `ecr_slinger`, `ecr_rimegiant`, `ecp_kraken`, `ecp_kraken_loot` (four item models and their icon sprites) in `EliteCreaturesPack/assets/bundles`, one per platform. The older bundle and asset names (`ecr_sling_*`, `ecr_rime_plate_N`, ...) keep the `ecr_` prefix they were built with: nobody sees them and renaming means rebuilding in AssetWorkshop |

Prefab names are hashed into saved worlds: never rename one after a release.

## Layout

```
EliteCreaturesPack/
  EliteCreaturesPack.cs   plugin entry: settings, PatchAll, each creature's Prefabs.Install, Synced.Finish, Guard
  Core/                   Settings (General + the Changed event, root namespace), Log, SafeCall, NameList
  Mimic/                  the crypt mimic: prefabs, body, bite, disguise, leap, crypt swap, loot, patches, settings
  Slinger/                the Greydwarf Slinger: prefabs, kit, sling rig, shot, stone, aim, spawns, patches, settings
  RimeGiant/              the Rime Giant: prefabs, kit, look, armour, slumber, attacks, avalanche, boulder, mountains, spawns, patches, settings
  Kraken/                 the Kraken: prefabs, settings, state, spawns, words, patches, and
    Motion/               pure Unity maths, no game types: tentacle poses (curl, deck run, swim, limp), the bone rigs, easing
    Ships/                a ship's hull, deck scan, the ship scene it is drawn in, holding the ship
    Body/                 every machine's drawing: KrakenBody, head and tentacle motion, strike and head-attack timelines, impacts
    Fight/                the owner's brain (hunt, fight, targets), the attack RPCs, hit tests, ink jet, effects, death
    Build/                the prefab: serpent copy, model from the bundle, materials, hit boxes, corpse
    Ink/                  the ink's status effect and the screen splats
    Loot/                 its items: beak, raw and cooked tentacle, the Kraken shield (copies of game items wearing the
                          loot bundle's models), the cooking, the shield's recipe, the parry bite (Humanoid.BlockAttack)
  assets/bundles/         the embedded bundles
```

The kraken has no animation clips: everything it does is posed in code each frame. `Kraken/Motion` depends on nothing
but UnityEngine, so it can be dropped into the AssetWorkshop Unity project to preview poses. Its AI is the serpent's
MonsterAI with `UpdateAI` replaced by `KrakenBrain.Think` (a prefix returning false); while it holds a ship its body is
kinematic and placed by the owner, and `Character.UpdateMotion` is skipped. The head's and tentacles' hit boxes are on
the game's `hitbox` layer (hit by attacks, collide with nothing) under a kinematic body of their own.

Each creature builds its prefabs on every ZNetScene wake (`BundlePrefabs.NetPrefabs.OnSceneAwake`) and listens to
`Settings.Changed` to put new numbers on its prefabs and on the loaded creatures. Local ice and frost effects go
through the `LocalEffects` library.

## Elite Creatures Reborn

Optional, both ways. Only key names, a prefab name and a version cross (`Mimic/EliteHandOff.cs`,
`Kraken/Build/KrakenHandOff.cs`, ECR's `Runtime/Disguise.cs` and `Rules/AspectDefaults.cs`):

- `ecp_disguised` (bool, set here on every mimic by its owner): while a creature carrying it sleeps, ECR applies its
  numbers but holds back size, star look, mutation behaviours and the decorated hover name, and dresses it on waking.
- `ecr_gen` (int, ECR's Splintering generation): read here so a split-off mimic is born awake.
- The kraken is an ECR boss: boss stars and an aspect, but ECR's own rules (3.12.0 on) leave Twin and Phantom out of
  `ECP_Kraken`'s rotation (a built-in `per boss` entry, also written into a new `creature_rules.yml`; the first test
  drew Twin: two krakens on one Karve). Against an older ECR (its version read from the chainloader) the kraken's
  owner sets `ecr_resolved` (bool, ECR's "traits rolled" mark) in the brain's `Awake`, before ECR's
  `EliteController.Start` rolls, so it gets nothing at all.
- The Rime Giant's corpse colour patch replaces only a colour with no shift at all, so it works with the game's level
  looks and ECR's star looks alike without asking either.

## Assets

Built in `../AssetWorkshop`: `assets/crypt_mimic` (the mimic's rig and clips), `assets/ecr_slinger`,
`assets/ecr_rimegiant` and `assets/ecp_kraken` (`build.ps1 [-Preview] [-Install]`; `-Install` copies the bundles into this mod's
`assets/bundles`). The kraken's loot is the bundle `ecp_kraken_loot`: `.\build.ps1 -Asset ecp_kraken_beak,ecp_kraken_beak_shield,
ecp_kraken_meat,ecp_kraken_meat_cooked -Bundle ecp_kraken_loot -SkipBlender` in the workshop (their Blender side is
`assets/kraken_beak_set` and `assets/kraken_food`, run on their own), then copy `out/bundles/ecp_kraken_loot.*` here.
Each feature file's work log records what was built.

## Testing

Build, then the user restarts the LocalTesting profile (never launch or kill the game). With DevBridge
(`curl -s http://127.0.0.1:7780/help`): `devcommands`, then `spawn ECP_CryptMimic`, `spawn ECP_GreydwarfSlinger`,
`spawn ECP_RimeGiant` (by day it comes asleep), `spawn ECP_Kraken` (at sea beside a ship with someone aboard), and
the kraken's loot `spawn ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`. Each
feature file ends with its checklist. Test once without Elite
Creatures Reborn and once with it (stars and mutations, the mimic's hold-back).

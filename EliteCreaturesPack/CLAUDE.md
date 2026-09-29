# CLAUDE.md - EliteCreaturesPack

New creatures for Valheim: the crypt mimic, the Greydwarf Slinger, the Rime Giant, the Kraken, the Skeleton
Crossbowman and the skeleton arsenal (seven skeletons with bone weapons, and those weapons for players). The first
three were split out of Elite Creatures Reborn on 2026-09-28, before any of them was released (the Kraken, the
crossbowman and the arsenal were built here), so the creatures ship and update on their own and Elite Creatures
Reborn stays a mod that changes the game's creatures. Read the workspace `../CLAUDE.md` first (layout, small
units, multiplayer first, releasing). The behaviour of each creature is in `features/`; the README is the store page.

**Status: 0.1.0, builds clean, offline patch check passes (33 patch classes); never run in game as this mod.** The mimic
was seen in game once while it lived in Elite Creatures Reborn (the ambush worked; the lunge was fixed after). The
slinger, the giant and the kraken have never run in game. The Skeleton Crossbowman (built 2026-09-28, unreleased; since
2026-09-29 one per archer skeleton kind, plus the players' Bone Crossbow) has never run in game either, nor has the
skeleton arsenal (built 2026-09-29, unreleased; `features/skeleton-arsenal.md`). Patch check since: 44 classes, no problems.

## Identity

| | |
| --- | --- |
| GUID / name / version | `com.EliteCreaturesPack`, `Elite Creatures Pack`, `PluginVersion` in `EliteCreaturesPack.cs` |
| Config | `com.EliteCreaturesPack.cfg` through SyncedConfig: `1 - General` (Lock Configuration), `2 - Crypt Mimic`, `3 - Greydwarf Slinger`, `4 - Rime Giant`, `5 - Kraken`, `6 - Skeleton Crossbowman`, `7 - Bone Crossbow`, `8 - Skeleton Arsenal` (only a master `Enabled` and a spawn switch per arsenal skeleton, by
the user's wish; its numbers are fixed in code), `25 - Crypt Executioner`, `26 - Executioner's Greataxe`; every entry synced and lockable |
| Prefabs | `ECP_CryptMimic` (+ `_bite`, `_ragdoll`), `ECP_GreydwarfSlinger` (+ `_shot`, `_stone`), `ECP_RimeGiant` (+ `_boulder`, `_ragdoll`, `_sweep`, `_slam`, `_throw`), `ECP_Kraken` (+ `_corpse`), `ECP_SkeletonCrossbowman` (+ `_shot`) and its `ECP_SkeletonCrossbowman_bolt`; the players' Blunted Bone Bolts `ECP_BoltBoneBlunt` (+ `_projectile`); the kraken's loot items `ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`; the players' `ECP_BoneCrossbow`; the arsenal skeletons `ECP_Skeleton<Title>` for Cutthroat, Swordsman, Axeman, Bonebreaker, Spearman, Halberdier, Bowman (each + `_attack`; the Black Forest's kind only since 0.4.0), `ECP_SkeletonArrow_projectile`; the players' `ECP_BoneDagger`, `ECP_BoneSword`, `ECP_BoneAxe`, `ECP_BoneMace`, `ECP_BoneSpear`, `ECP_BoneAtgeir`, `ECP_BoneBow`, `ECP_Vertebra`, `ECP_ArrowBone` (+ `_projectile`); the Crypt Executioner `ECP_Headsman` (+ `_ragdoll`, `_spawner`, `_shatter`, `_axe_hurl`, `_axe_disc`, attacks `ECP_Headsman_slam`, `_scrape`, `_spin`, `_hurl`, `_spinthrow`, `_rear`), its raised `ECP_HeadsmanSkeleton`, the players' `ECP_ExecutionerGreataxe` and `ECP_ExecutionerAxehead`, the greataxe's swing sounds `ECP_Greataxe_sfx_slash`, `_spin`, `_overhead` |
| Status effects | `ECP_KrakenInk` (the ink's screen splats; kept in every ObjectDB by `Kraken/Ink/InkStatus`) |
| ZDO / RPC / global keys | `ecp_mimic_swap`, `ecp_mimic_chest`, `ecp_mimic_open`, `ecp_disguised`, `ecp_rime_plates`, `ecp_rime_wave`, global `ecp_rimegiant_<region>`; kraken ZDO `ecp_kraken_phase`, `ecp_kraken_ship`, `ecp_kraken_anchor`, `ecp_kraken_side`, `ecp_kraken_along`, RPCs `ecp_kraken_slam`, `ecp_kraken_bite`, `ecp_kraken_ink`, `ecp_kraken_flinch`; the Executioner's `ecp_hs_hit`, `ecp_hs_summon`, `ecp_hs_raised` (the shatter), `ecp_hs_rise` (the raised skeleton), `ecp_hs_chamber` (a burial chamber rolled), `ecp_hs_cue` (a swing sound's moment), global key `ecp_headsman_off` (never set: blocks the spawners while it is off) |
| Words | `enemy_ecp_*`, `item_ecp_*` (the kraken's loot: `item_ecp_krakenbeak`, `_krakenmeat`, `_krakenmeatcooked`, `_shieldkraken`; the crossbowman's `item_ecp_skeletoncrossbow`; the players' `item_ecp_bonecrossbow` and `_description`; the arsenal's `enemy_ecp_skeleton<title>`, `item_ecp_bone<weapon>`, `item_ecp_vertebra`, `item_ecp_arrowbone`, each item with `_description`; the Executioner's `enemy_ecp_headsman`, `item_ecp_headsman_<attack>`, `item_ecp_executionergreataxe`, `item_ecp_executioneraxehead` and their `_description`), `se_ecp_*`, added in each creature's `*Words` |
| Asset bundles | `ecr_cryptmimic`, `ecr_slinger`, `ecr_rimegiant`, `ecp_kraken`, `ecp_kraken_loot` (four item models and their icon sprites), `ecp_crossbowman` (the crossbowman's kit, its bolt and five clips; the Bone Crossbow's unloaded, loaded and rigged models (`ecp_xbow_item_rig`: string halves and markers, moved by `XbowPlayerRig`), its players' reload clips `ecp_xbow_player_reload` and `_done`, and icon sprite), `ecp_skel_arsenal` (the seven bone weapons, the bow a second time in the player's hold, the arrow, the vertebra, nine icon sprites, and the Bone Atgeir's player attacks `ecp_atgeir_player_attack0`..`2` from AssetWorkshop `Assets/BundleExtras`), `ecp_headsman` (the Executioner's kit and animator, the axe in 29 pieces, the players' held greataxe and axehead, two icon sprites) in `EliteCreaturesPack/assets/bundles`, one per platform. The older bundle and asset names (`ecr_sling_*`, `ecr_rime_plate_N`, ...) keep the `ecr_` prefix they were built with: nobody sees them and renaming means rebuilding in AssetWorkshop |

Prefab names are hashed into saved worlds: never rename one after a release.

## Layout

```
EliteCreaturesPack/
  EliteCreaturesPack.cs   plugin entry: settings, PatchAll, each creature's Prefabs.Install, Synced.Finish, Guard
  Core/                   Settings (General + the Changed event, root namespace), Log, SafeCall, NameList
  Mimic/                  the crypt mimic: prefabs, body, bite, disguise, leap, crypt swap, loot, patches, settings
  Slinger/                the Greydwarf Slinger: prefabs, kit, sling rig, shot, stone, aim, spawns, patches, settings
  RimeGiant/              the Rime Giant: prefabs, kit, look, armour, slumber, attacks, avalanche, boulder, mountains, spawns, patches, settings
  Crossbow/               the Skeleton Crossbowman: kinds, prefabs, creature, kit, shot, bolt, rig, spawns, patches, settings, words;
                          the Bone Crossbow item, its settings and workbench recipe
  Arsenal/                the skeleton arsenal: weapons and kinds (tables), prefabs, creature, its weapon (ArsenalAttack),
                          single-blow clips, look (models, colliders, glow, bowstring), players' items and vertebra,
                          bone arrow, recipes, spawns, patches, words, settings (section 8: master and spawn switches only)
  Kraken/                 the Kraken: prefabs, settings, state, spawns, words, patches, and
    Motion/               pure Unity maths, no game types: tentacle poses (curl, deck run, swim, limp), the bone rigs, easing
    Ships/                a ship's hull, deck scan, the ship scene it is drawn in, holding the ship
    Body/                 every machine's drawing: KrakenBody, head and tentacle motion, strike and head-attack timelines, impacts
    Fight/                the owner's brain (hunt, fight, targets), the attack RPCs, hit tests, ink jet, effects, death
    Build/                the prefab: serpent copy, model from the bundle, materials, hit boxes, corpse
    Ink/                  the ink's status effect and the screen splats
    Loot/                 its items: beak, raw and cooked tentacle, the Kraken shield (copies of game items wearing the
                          loot bundle's models), the cooking, the shield's recipe, the parry bite (Humanoid.BlockAttack)
  Headsman/               the Crypt Executioner (features/headsman.md): prefabs, settings, words, burial chambers, and
    Motion/               the moves' clip times and cues, the animator and world clocks, the axe's shape, two-bone IK
    Build/                the creature (Skeleton copy), kit, six attacks, thrown axes
    Fight/                the owner's brain, the shockwave, the AI patches
    Look/                 every peer's drawing: rig, forming axe, rocks, shatter and its pieces, the raised skeleton
    Sound/                cues from the game's clips by the generated HeadsmanSoundTable (sfx_table.py writes it)
    Greataxe/             the players' axehead and greataxe: items, recipe, swing sounds, animator override, left-hand grip
  assets/bundles/         the embedded bundles
```

The kraken has no animation clips: everything it does is posed in code each frame. `Kraken/Motion` depends on nothing
but UnityEngine, so it can be dropped into the AssetWorkshop Unity project to preview poses. Its AI is the serpent's
MonsterAI with `UpdateAI` replaced by `KrakenBrain.Think` (a prefix returning false); while it holds a ship its body is
kinematic and placed by the owner, and `Character.UpdateMotion` is skipped. The head's and tentacles' hit boxes are on
the game's `hitbox` layer (hit by attacks, collide with nothing) under a kinematic body of their own.

The crossbowman (`Crossbow/XbowKind`: the Black Forest's archer `Skeleton` only since 0.4.0; the table once held all four) is
a copy of that skeleton, its bow's damage turned blunt, spawning in its place from every spawner (`XbowSpawns`). It plays five clips of the bundle in place of the Skeleton controller's own
(`Crossbow/XbowCreature` names them): its idle, walk and run carry the crossbow at the low ready, and the archer's two
bow states (`bow_idle`, then `attack_bow`) play the raise-and-aim and the fire-and-reload clips. `XbowRig` times the
string and the bolts by the `attack_bow` state's normalized time against the fire clip's seconds (the constants mirror
AssetWorkshop `Crossbow/XbowClips`; change both together). The Bone Crossbow (`XbowItem`) is a copy of the game's
Arbalest with the bundle's two models in its `attach/Unloaded` and `attach/Loaded` slots (the game's WeaponLoadState
swaps them), 1.25 times the crossbowmen's size; `XbowRecipe` puts it on the workbench with the cost from the config.

The arsenal (`Arsenal/`, `features/skeleton-arsenal.md`) is two tables: `ArsenalWeapon` (seven weapons: the skeleton's
title, its `Swing` - state, player clip, blow, factor, reach - and the bronze-age game item the players' copy is made
after) and `ArsenalKind` (the four skeletons it copies, each with its no-archer version, sword and bow). Every
creature is a copy of its kind's archer skeleton with one weapon (`ArsenalAttack`: a copy of the kind's sword or bow
wearing the bone model) and, for the dagger, axe, spear and atgeir, an animator override that plays the player's first
blow (`ArsenalClips`: a copy of the clip with its hit no later than 75%) in place of the Skeleton's sword swing
(`Standing Melee Attack Horizontal`). The bundle's models are built in the attach frame (`ArsenalLook.Wear` puts them
at the slot's origin); the bowstrings are a cube between the tips in `ArsenalLook` (numbers from the workshop's
`out/ecp_skel_bow*_points.json`). Its wild-spawn prefix and the crossbowman's each stand aside when `__runOriginal` is
already false, so one skeleton spawn never becomes two.

Players' weapons that need more than the game's clips carry a component on every player (added in `Player.Awake`,
active only while the item is in hand, on every peer): `Headsman/Greataxe/GreataxeHold` (combo override, left hand on
the haft), `Arsenal/ArsenalAtgeirHold` (the Bone Atgeir's own attacks, left hand on the haft), `Crossbow/XbowHold` and
`XbowPlayerRig`/`XbowPlayerHand` (the Bone Crossbow's reload clip, right hand on the stock and string, the string and
bolts moved by the reload clip's time and the game's synced loaded flag). The override swap and the haft grip are
shared: `Core/AnimatorSwap`, `Core/HaftGrip`, `Core/TwoBoneIk`. The workshop's player preview
(`AssetWorkshop/unity/Assets/Editor/SkelArsenal/ArsenalPlayerBake` + `assets/ecp_skel_arsenal/player_scene.py`) plays
the same with copies of that code.

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
`assets/ecr_rimegiant`, `assets/ecp_kraken` and `assets/ecp_crossbowman` (`build.ps1 [-Preview] [-Install]`; `-Install` copies the bundles into this mod's
`assets/bundles`). The arsenal: `assets/ecp_skel_arsenal/build.ps1 -Install -SkipBake` (models, icons, bundle, install;
without `-SkipBake` it also bakes the Blender showcase). The kraken's loot is the bundle `ecp_kraken_loot`: `.\build.ps1 -Asset ecp_kraken_beak,ecp_kraken_beak_shield,
ecp_kraken_meat,ecp_kraken_meat_cooked -Bundle ecp_kraken_loot -SkipBlender` in the workshop (their Blender side is
`assets/kraken_beak_set` and `assets/kraken_food`, run on their own), then copy `out/bundles/ecp_kraken_loot.*` here.
The Crypt Executioner: `assets/ecp_headsman/build.ps1 -Bundle -Install` (icons, bundle, install; it also rewrites
`Headsman/Sound/HeadsmanSoundTable.cs` from the sound recipes). Each feature file's work log records what was built.

## Testing

Build, then the user restarts the LocalTesting profile (never launch or kill the game). With DevBridge
(`curl -s http://127.0.0.1:7780/help`): `devcommands`, then `spawn ECP_CryptMimic`, `spawn ECP_GreydwarfSlinger`,
`spawn ECP_RimeGiant` (by day it comes asleep), `spawn ECP_Kraken` (at sea beside a ship with someone aboard),
`spawn ECP_SkeletonCrossbowman`, `spawn ECP_BoneCrossbow`, `spawn ECP_BoltBoneBlunt 20`, the arsenal's
`spawn ECP_SkeletonSwordsman` (Cutthroat, Axeman, Bonebreaker, Spearman, Halberdier, Bowman; and the kinds),
`spawn ECP_BoneSword` (Dagger, Axe, Mace, Spear, Atgeir, Bow), `spawn ECP_Vertebra`, `spawn ECP_ArrowBone 20`, `spawn ECP_Headsman`, `spawn ECP_ExecutionerGreataxe`, `spawn ECP_ExecutionerAxehead`, and
the kraken's loot `spawn ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`. Each
feature file ends with its checklist. Test once without Elite
Creatures Reborn and once with it (stars and mutations, the mimic's hold-back).

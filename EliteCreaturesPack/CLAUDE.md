# CLAUDE.md - EliteCreaturesPack

New creatures for Valheim: the crypt mimic, the Greydwarf Slinger, the Rime Giant, the Kraken, the Skeleton
Crossbowman, the arsenal skeletons, the Crypt Executioner, and the skeleton arsenal (every bone weapon, see "The
skeleton arsenal" below). The first
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
| Config | `com.EliteCreaturesPack.cfg` through SyncedConfig: `1 - General` (Lock Configuration), `2 - Crypt Mimic`, `3 - Greydwarf Slinger`, `4 - Rime Giant`, `5 - Kraken`, `6 - Skeleton Crossbowman` (its shot's numbers only), `7 - Bone Crossbow`, `8 - Skeleton Arsenal` (only a master `Enabled` and a spawn switch per arsenal skeleton, the crossbowman's among them, by
the user's wish; its numbers are fixed in code), `25 - Crypt Executioner`, `26 - Executioner Greataxe`, `27 - Bear Claws` (the game's Paws of the Bear, `BearClaws/`: one switch, `Triple Strike`), `28 - Bone Ballista` (one switch, `Enabled`: the piece in the hammer and its missiles' recipe), `29 - Custom Creatures` (one switch, `Enabled`; everything else in the YAML files below) (no apostrophe: BepInEx refuses `= \n \t \ " ' [ ]` in section and key names and the whole Awake stops); every entry synced and lockable |
| Prefabs | `ECP_CryptMimic` (+ `_bite`, `_ragdoll`), `ECP_GreydwarfSlinger` (+ `_shot`, `_stone`), `ECP_RimeGiant` (+ `_boulder`, `_ragdoll`, `_sweep`, `_slam`, `_throw`), `ECP_Kraken` (+ `_corpse`), `ECP_SkeletonCrossbowman` (+ `_shot`) and its `ECP_SkeletonCrossbowman_bolt`; the players' Blunted Bone Bolts `ECP_BoltBoneBlunt` (+ `_projectile`); the kraken's loot items `ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`; the players' `ECP_BoneCrossbow`; the arsenal skeletons `ECP_Skeleton<Title>` for Cutthroat, Swordsman, Axeman, Bonebreaker, Spearman, Halberdier, Bowman (each + `_attack`; the Black Forest's kind only since 0.4.0), `ECP_SkeletonArrow_projectile`; the players' `ECP_BoneDagger`, `ECP_BoneSword`, `ECP_BoneAxe`, `ECP_BoneMace`, `ECP_BoneSpear`, `ECP_BoneAtgeir`, `ECP_BoneBow`, `ECP_Spine`, `ECP_ArrowBone` (+ `_projectile`); the Crypt Executioner `ECP_Headsman` (+ `_spawner`, `_shatter`, `_axe_hurl`, `_axe_disc`, attacks `ECP_Headsman_slam`, `_scrape`, `_spin`, `_hurl`, `_spinthrow`, `_rear`), its raised `ECP_HeadsmanSkeleton`, the players' `ECP_ExecutionerGreataxe` and `ECP_ExecutionerAxehead`, the greataxe's swing sounds `ECP_Greataxe_sfx_slash`, `_spin`, `_overhead`; the Bone Ballista piece `ECP_BoneBallista`, its `ECP_BoneMissile` (+ `_projectile`) |
| Status effects | `ECP_KrakenInk` (the ink's screen splats; kept in every ObjectDB by `Kraken/Ink/InkStatus`) |
| ZDO / RPC / global keys | `ecp_mimic_swap`, `ecp_mimic_chest`, `ecp_mimic_open`, `ecp_disguised`, `ecp_rime_plates`, `ecp_rime_wave`, global `ecp_rimegiant_<region>`; kraken ZDO `ecp_kraken_phase`, `ecp_kraken_ship`, `ecp_kraken_anchor`, `ecp_kraken_side`, `ecp_kraken_along`, RPCs `ecp_kraken_slam`, `ecp_kraken_bite`, `ecp_kraken_ink`, `ecp_kraken_flinch`; the Executioner's `ecp_hs_hit`, `ecp_hs_summon`, `ecp_hs_raised` (the shatter), `ecp_hs_rise` (the raised skeleton), `ecp_hs_chamber` (a burial chamber rolled), `ecp_hs_cue` (a swing sound's moment), global key `ecp_headsman_off` (never set: blocks the spawners while it is off); the Bone Ballista's ZDO `ecp_bal_user`, `_yaw`, `_pitch`, `_spring`, `_act`, `_act_at`, `_shots`, `_shot_at`, RPCs `ecp_bal_request`, `_release`, `_answer`; custom creatures' ZDO `ecp_custom` (string, the creature's prefab name, written once by its owner: how a creature whose definition is gone is recognised and parked), `ecp_spawn_told` (bool, its spawn message dealt with), `ecp_human_look` (bool, a human's look rolled into the game's own look keys), RPCs `ecp_admin_ask`, `ecp_admin_answer` (the `ecp` command's admin check) |
| Words | `enemy_ecp_*`, `item_ecp_*` (the kraken's loot: `item_ecp_krakenbeak`, `_krakenmeat`, `_krakenmeatcooked`, `_shieldkraken`; the crossbowman's `item_ecp_skeletoncrossbow`; the players' `item_ecp_bonecrossbow` and `_description`; the arsenal's `enemy_ecp_skeleton<title>`, `item_ecp_bone<weapon>`, `item_ecp_spine`, `item_ecp_arrowbone`, each item with `_description`; the Executioner's `enemy_ecp_headsman`, `item_ecp_headsman_<attack>`, `item_ecp_executionergreataxe`, `item_ecp_executioneraxehead` and their `_description`), `se_ecp_*`; the Bone Ballista's `piece_ecp_boneballista` (+ `_description`), `item_ecp_bonemissile` (+ `_description`), `hud_ecp_bal_hold`, `_letgo`, `_loaded`, `_empty`, `msg_ecp_bal_nomissiles`; added in each creature's `*Words` |
| Custom creatures | Defined by server admins in `EliteCreaturesPack.Creatures.yml` and any `EliteCreaturesPack.Creatures*.yml` in the config folder and its subfolders (YamlConfig set, sync key `ecp.creatures`; a new main file is written from `Custom/Ready/creatures.yml`, the ready-made creatures, all off); the files the server built from travel in the standing Charter article `ecp.creatures.built`. Each becomes a prefab named as its `name`, with parts `<name>_<item>` (its own item copies), `<name>_ragdoll`, `<name>_young`, `<name>_overlay` (a number added when taken). `ecp export` writes `EliteCreaturesPack.Export.<prefab>.yml` (never loaded) |
| Console command | `ecp` (admins only, checked by the server): `ecp export <prefab>`, `ecp export human` |
| Asset bundles | `ecr_cryptmimic`, `ecr_slinger`, `ecr_rimegiant`, `ecp_kraken`, `ecp_kraken_loot` (four item models and their icon sprites), `ecp_crossbowman` (the crossbowman's kit, its bolt and five clips; the Bone Crossbow's unloaded, loaded and rigged models (`ecp_xbow_item_rig`: string halves and markers, moved by `XbowPlayerRig`), its players' reload clips `ecp_xbow_player_reload` and `_done`, and icon sprite), `ecp_skel_arsenal` (the seven bone weapons, the bow a second time in the player's hold, the arrow, the spine, nine icon sprites, and the Bone Atgeir's player attacks `ecp_atgeir_player_attack0`..`2` from ValheimAssets `Assets/BundleExtras`), `ecp_headsman` (the Executioner's kit and animator, the axe in 29 pieces, the players' held greataxe and axehead, two icon sprites), `ecp_boneballista` (the Bone Ballista, flat: mount, yaw, pitch, spine-arm vertebrae, string halves, points; its Bone Missile; two icon sprites) in `EliteCreaturesPack/assets/bundles`, one per platform. The older bundle and asset names (`ecr_sling_*`, `ecr_rime_plate_N`, ...) keep the `ecr_` prefix they were built with: nobody sees them and renaming means rebuilding in ValheimAssets |

Prefab names are hashed into saved worlds: never rename one after a release.

## Layout

```
EliteCreaturesPack/
  EliteCreaturesPack.cs   plugin entry: settings, PatchAll, each creature's Prefabs.Install, Synced.Finish, the Elite
                          Crafting hand-off, Guard
  Core/                   Settings (General + the Changed event, root namespace), Log, SafeCall, NameList, StandIns
  Mimic/                  the crypt mimic: prefabs, body, bite, disguise, leap, crypt swap, loot, patches, settings
  Slinger/                the Greydwarf Slinger: prefabs, kit, sling rig, shot, stone, aim, spawns, patches, settings
  RimeGiant/              the Rime Giant: prefabs, kit, look, armour, slumber, attacks, avalanche, boulder, mountains, spawns, patches, settings
  Crossbow/               the Skeleton Crossbowman: kinds, prefabs, creature, kit, shot, bolt, rig, settings, words;
                          the Bone Crossbow item and its Blunted Bone Bolts (skeleton arsenal), settings, workbench recipe
  Arsenal/                the skeleton arsenal's own folder and its skeletons: weapons and kinds (tables), prefabs,
                          creature, its weapon (ArsenalAttack),
                          single-blow clips, look (models, colliders, glow, bowstring), players' items and spine,
                          bone arrow, recipes, words, settings (section 8: master and spawn switches only)
  Skeletons/              the one even draw for a Black Forest skeleton's spawn: plain, arsenal or crossbowman, and its patches
  BearClaws/              the game's bear claws (FistBjornClaw): a punch is a flurry of three swipes (the game's own combo,
                          left/right alternating) at 6.2x animator speed, ~0.75 s for all three (synced by ZSyncAnimation), each 0.35 of the
                          punch's damage (~52 DPS), a third of its skill and adrenaline; swipes 2-3 cost no stamina or durability, hits freeze 1/3
                          as long, root motion (the forward step) cut to 1/3, the player's own attack starts wait while it runs (owner only)
  Ballista/               the Bone Ballista (features/bone-ballista.md): a player-held copy of the game's ballista, half its
                          size, of bone, spine arms; piece, missile, crafting, hold (doodad control, ownership hand-off),
                          the holder's aim/reload/shot, drawing (turn, bending arms, string, sounds, the holder's arms by IK)
  Crafting/               Elite Crafting, optional: the gear's item classes and levels, the creatures' rune and gear loot
  Kraken/                 the Kraken: prefabs, settings, state, spawns, words, patches, and
    Motion/               pure Unity maths, no game types: tentacle poses (curl, deck run, swim, limp), the bone rigs, easing
    Ships/                a ship's hull, deck scan, the ship scene it is drawn in, holding the ship
    Body/                 every machine's drawing: KrakenBody, head and tentacle motion, strike and head-attack timelines, impacts
    Fight/                the owner's brain (hunt, fight, targets), the attack RPCs, hit tests, ink jet, effects, death and
                          where its loot lands (other mods' drops in its death too: KrakenDropSpot)
    Build/                the prefab: serpent copy, model from the bundle, materials, hit boxes, corpse
    Ink/                  the ink's status effect and the screen splats
    Loot/                 its items: beak, raw and cooked tentacle, the Kraken shield (copies of game items wearing the
                          loot bundle's models), the cooking, the shield's recipe, the parry bite (Humanoid.BlockAttack)
  Headsman/               the Crypt Executioner (features/headsman.md): prefabs, settings, words, burial chambers, and
    Motion/               the moves' clip times and cues, the animator and world clocks, the axe's shape, two-bone IK
    Build/                the creature (Skeleton copy), kit, six attacks, thrown axes
    Fight/                the owner's brain, the axe head's hits, the AI patches
    Look/                 every peer's drawing: rig, forming axe, rocks, shatter and its pieces, the raised skeleton
    Sound/                cues from the game's clips by the generated HeadsmanSoundTable (sfx_table.py writes it)
    Greataxe/             the players' axehead and greataxe (skeleton arsenal): items, recipe, swing sounds, animator
                          override, left-hand grip
  Custom/                 custom creatures (features/custom-creatures.md), see "Custom creatures" below:
    Definitions/          the definition model, plain data, one block per area of the spec
    Files/                the YAML set and its readers (one entry left out per mistake, file and line), the server's built files
    Build/                when and how prefabs are built: timing, pass, base chains, shells, step runner, the step contract,
                          lookups, the world's custom prefabs
    Nature/ Combat/ Look/ Humans/ Elite/   the five steps (character; drops, gear, damage, attacks; effects and looks; the
                          human body; Elite Creatures Reborn's lines) and their runtime parts
    Saves/                the `ecp_custom` mark and the parking of creatures whose definition is gone
    Commands/ Export/     the `ecp` command (admin check) and `ecp export`
    Ready/                creatures.yml, the default main file
  assets/bundles/         the embedded bundles
```

The kraken has no animation clips: everything it does is posed in code each frame. `Kraken/Motion` depends on nothing
but UnityEngine, so it can be dropped into the ValheimAssets Unity project to preview poses. Its AI is the serpent's
MonsterAI with `UpdateAI` replaced by `KrakenBrain.Think` (a prefix returning false); while it holds a ship its body is
kinematic and placed by the owner, and `Character.UpdateMotion` is skipped. The head's and tentacles' hit boxes are on
the game's `hitbox` layer (hit by attacks, collide with nothing) under a kinematic body of their own.

The crossbowman (`Crossbow/XbowKind`: the Black Forest's archer `Skeleton` only since 0.4.0; the table once held all four) is
a copy of that skeleton, its bow's damage turned blunt, spawning in its place from every spawner (`Skeletons/SkeletonDraw`).
Since 2026-09-30 it counts as an arsenal skeleton (the user): switched in section 8, and it drops a spine as they do
(`ArsenalCreature.AddSpine`). It plays five clips of the bundle in place of the Skeleton controller's own
(`Crossbow/XbowCreature` names them): its idle, walk and run carry the crossbow at the low ready, and the archer's two
bow states (`bow_idle`, then `attack_bow`) play the raise-and-aim and the fire-and-reload clips. `XbowRig` times the
string and the bolts by the `attack_bow` state's normalized time against the fire clip's seconds (the constants mirror
ValheimAssets `Crossbow/XbowClips`; change both together). The Bone Crossbow (`XbowItem`) is a copy of the game's
Arbalest with the bundle's two models in its `attach/Unloaded` and `attach/Loaded` slots (the game's WeaponLoadState
swaps them), 1.25 times the crossbowmen's size; `XbowRecipe` puts it on the workbench with the cost from the config.

Each creature builds its prefabs on the first ZNetScene wake and registers them on every one
(`BundlePrefabs.NetPrefabs.OnSceneAwake`), and listens to `Settings.Changed` (raised once, the frame after any number of
settings changed) to put new numbers on its prefabs and on the loaded creatures. Local ice and frost effects go
through the `LocalEffects` library.

## The skeleton arsenal

**The skeleton arsenal is every bone weapon of the mod** (the user, 2026-09-30): the bone dagger, sword, axe, mace,
spear, atgeir, the players' bow and the skeletons' bow, the bone arrow, the spine, the Bone Crossbow and its Blunted
Bone Bolts, and the Executioner's Greataxe. "The arsenal" means all of them. It ships in three bundles:
`ecp_skel_arsenal` (built in `Arsenal/`), `ecp_crossbowman` (the Bone Crossbow and its bolts, built beside the
crossbowman in `Crossbow/XbowItem`, `XbowBolts`, `XbowRecipe`) and `ecp_headsman` (the greataxe, built beside the
Executioner in `Headsman/Greataxe/`); the crossbow and the greataxe share their bundles with the creatures that carry
them, which is why their code lives beside those creatures. In ValheimAssets, `Assets/Weapons/SkeletonArsenal/ecp_skel_arsenal/build.ps1`
builds all three bundles. The **arsenal skeletons** are the eight skeleton units that carry its weapons (Cutthroat,
Swordsman, Axeman, Bonebreaker, Spearman, Halberdier, Bowman, Crossbowman), switched in section 8, which keeps the
config name `8 - Skeleton Arsenal`; the Executioner carries the greataxe but is a boss of its own (section 25).

The arsenal's own folder (`Arsenal/`, `features/skeleton-arsenal.md`) is two tables: `ArsenalWeapon` (seven weapons: the skeleton's
title, its `Swing` - state, player clip, blow, factor, reach - and the bronze-age game item the players' copy is made
after) and `ArsenalKind` (the four skeletons it copies, each with its no-archer version, sword and bow). Every
creature is a copy of its kind's archer skeleton with one weapon (`ArsenalAttack`: a copy of the kind's sword or bow
wearing the bone model) and, for the dagger, axe, spear and atgeir, an animator override that plays the player's first
blow (`ArsenalClips`: a copy of the clip with its hit no later than 75%) in place of the Skeleton's sword swing
(`Standing Melee Attack Horizontal`). The bundle's models are built in the attach frame (`ArsenalLook.Wear` puts them
at the slot's origin); the bowstrings are a cube between the tips in `ArsenalLook` (numbers from the workshop's
`out/ecp_skel_bow*_points.json`). Which skeleton a Black Forest skeleton's spawn becomes is one even draw among the
plain skeleton, the arsenal skeletons and the crossbowman (`Skeletons/SkeletonDraw`, its three spawn patches in
`SkeletonPatches`; the user, 2026-09-30). Every creature that spawns in another's place registers in `Core/StandIns`, so
bone piles, nests and wild spawn caps count it as the creature it replaced (`Core/StandInPatches`).

Players' weapons that need more than the game's clips carry a component on every player (added by the one
`Player.Awake` patch in `Core/PlayerHolds`, active only while the item is in hand, on every peer that draws; a dedicated
server gets none): `Headsman/Greataxe/GreataxeHold` (combo override, left hand on
the haft), `Arsenal/ArsenalAtgeirHold` (the Bone Atgeir's own attacks, left hand on the haft), `Crossbow/XbowHold` and
`XbowPlayerRig`/`XbowPlayerHand` (the Bone Crossbow's reload clip, right hand on the stock and string, the string and
bolts moved by the reload clip's time and the game's synced loaded flag). The override swap and the haft grip are
shared: `Core/AnimatorSwap`, `Core/HaftGrip`, `Core/TwoBoneIk`. The workshop's player preview
(`../ValheimAssets/Tools/Unity/Assets/Editor/SkelArsenal/ArsenalPlayerBake` + `Assets/Weapons/SkeletonArsenal/ecp_skel_arsenal/player_scene.py`) plays
the same with copies of that code.

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

## Elite Crafting

Optional, one way (added 2026-10-05, untested in game; the user: "custom mods in elite creatures pack can drop our rune
currency", "all the bone items need to be classified and have tiers"). With Elite Crafting installed (a soft
`BepInDependency` on `com.EliteCrafting`, so it loads first), `Crafting/CraftingHandOff` registers in the plugin's Awake,
on every peer, through Elite Crafting's public API (`ValheimModLibs/EliteCraftingLink`, merged, bound by reflection: no
reference, nothing happens without it; `EliteCrafting/features/api.md`). A server's Elite Crafting files win over each
registration by prefab name (a class's `items`, `item_tiers.items`, `drops.bosses`, `drops.creatures`); `ecraft tiers`
shows these levels as source `api`.

**Gear** (`CraftingGear`): every equippable is claimed for its class (each already meets the default file's rule for its
game item; the claim pins it whatever order a server puts the rules in) and given its item level by where it comes
from, since Elite Crafting knows none of the mod's materials (spine, axehead, beak). Ammo (`ECP_ArrowBone`,
`ECP_BoltBoneBlunt`) and materials and food (`ECP_Spine`, `ECP_ExecutionerAxehead`, `ECP_KrakenBeak`, the kraken's meat)
stack, so Elite Crafting never makes them magic; they are left alone.

| Prefab | Class | Level | Why |
| --- | --- | --- | --- |
| `ECP_BoneDagger` | knife | 2 | Copper knife copy; bone fragments and the Black Forest skeletons' spine, workbench 2 (like the bronze kit) |
| `ECP_BoneSword`, `ECP_BoneAxe`, `ECP_BoneMace` | sword_1h, axe_1h, mace_1h | 2 | Bronze sword, axe and mace copies, a little weaker; same materials |
| `ECP_BoneSpear`, `ECP_BoneAtgeir` | spear, atgeir | 2 | Bronze spear and atgeir copies; same materials |
| `ECP_BoneBow` | bow | 2 | Finewood bow copy; same materials |
| `ECP_BoneCrossbow` | crossbow | 2 | Arbalest copy with its own 30 blunt; spine, bones, wood, leather at workbench 2 (the crossbowmen are Black Forest skeletons) |
| `ECP_ExecutionerGreataxe` | battleaxe | 2 | Battleaxe copy hitting like a fully upgraded bronze axe; the Black Forest Crypt Executioner's axehead |
| `ECP_ShieldKraken` | shield | 4 | Silver shield copy, better than the serpent scale shield; the Kraken's beak and silver (the ocean is level 4) |

**Creature loot** (`CraftingLoot`): runes and rolled gear from Elite Crafting's own roll, beside the creature's drops.
Elite Crafting reads a level from the game's spawn lists, which none of these is in, so each has one here. Bosses and
mini-bosses get boss entries (guaranteed runes and gear, a bonus rune, the boss rarity row) scaled against the game's
bosses in Elite Crafting's file (Elder: level 2, 2 runes, 1 gear, Ascension 100%; Moder: level 4, 3 runes, 1 gear,
Ascension 100%). Elite Crafting 0.8.0 removed the Consecrated Rune (an entry naming it is refused whole) and drops no gear
for now, so the gear counts below roll nothing until it does again.

| Prefab | Level | Entry | Why |
| --- | --- | --- | --- |
| `ECP_Kraken` | 4 | boss: 3 runes, 1 gear, Sealed Rune 100% | A boss of the open sea (game boss flag, 3000 health): Moder's at her level, the sea's rune for her Ascension |
| `ECP_RimeGiant` | 4 | boss: 3 runes, 1 gear, Ascension 50% | Mini-boss, one per mountain ever, plates only fire breaks; level of the lowest of its `Biomes` (Mountain), resent when they change |
| `ECP_Headsman` | 2 | boss: 2 runes, 1 gear, Ascension 25% | The burial chambers' mini-boss (900 health), back every 3 days: a little under the Elder |
| `ECP_HeadsmanSkeleton` | - | multiplier 0 | The Executioner's raised skeletons drop nothing: no rune farm beside a boss |
| `ECP_CryptMimic` | where it dies | rune x6, gear x8 | It is a crypt chest: a chest's chances (Elite Crafting's chests 30% and 10%, a level 2 creature 5% and 1.25%); the chest it replaced was rolled and removed. Forest crypt 2, sunken crypt 3 |
| `ECP_GreydwarfSlinger` | 2 | creature | Lowest of its `Biomes` (Black Forest), resent when they change |
| `ECP_Skeleton<Title>` (7), `ECP_SkeletonCrossbowman` | 2 | creature | They take the Black Forest Skeleton's spawns |

Nothing in Elite Crafting kept these creatures from dropping (none is tamed or player-faction, all are hit by players,
deaths without a ragdoll drop at once), but the Kraken's runes and gear would have dropped where its body was, which
may be deep under the ship it holds: Elite Crafting drops at the dying creature's middle. So its death prefix now runs
first (`HarmonyPriority.First`: it is moved to where its head is seen before any other mod looks), and from then until
its finalizer, which runs last, `KrakenDropSpot` sends every `ItemDrop.DropItem` to the spot its own loot is aimed at
(the held ship's deck, or just over the water).

## Custom creatures

Server admins define new creatures and human enemies in YAML (`features/custom-creatures.md`, its build checklist and
work log). Each is a real prefab, a copy of a creature that exists, so the game's `spawn`, other mods' spawners and raids
and other creatures' death effects make it by name. Everything works without Elite Creatures Reborn.

- **When.** The server (host, single player) builds at the end of `ZoneSystem.Start` (`Priority.Last`: every Awake has
  run, ObjectDB is the world's, no object is made yet) from its own files, then assigns the standing Charter article
  `ecp.creatures.built` with exactly those files. A joining client never builds from its own files: it builds when that
  article arrives with Charter's first push (sent in the server's `RPC_PeerInfo` postfix, ahead of the ZDOs); a custom ZDO
  that arrives first is only retried (a client never destroys an unknown prefab's ZDO). Each world load builds from
  scratch (`ZNetScene.Awake` resets; the last world's prefabs and their parts are destroyed when the next is built).
  A file changed during play applies the next time a world is loaded (the log says so).
- **How** (`Build/BuildPass`). Names that are prefabs already are refused; each definition's bases are followed through
  the other definitions (a loop refuses the creatures in it) to a game or mod creature, or `Human`; every shell is made
  first (`PrefabBench.Copy` of that root, or `HumanBody.Build`), so definitions can name each other; then for each
  definition of the chain, base-most first, the steps in order `Human`, `Character`, `Combat`, `Look`, `Elite`
  (`Build/CreatureSteps`), each guarded (a throw fails only that creature); a creature that needs a failed one fails
  too; the rest are registered with ZNetScene (with their networked parts) and their item copies put in ObjectDB
  (`Combat/PartItems`); then ECR gets the elite lines (`Elite/EliteRegistrar`) or one warning says they do nothing.
- **Chain rules.** Each pass applies only what its definition sets; relative values stack (`speed scale`, `size`); the
  look is put on once, at the last pass, from the whole chain (`Look/LookPlan`); anything registered (a ZDO mark, an ECR
  registration, part names) is for the creature, never for a base definition.
- **Parts and origins.** A step never changes a game prefab: it changes the shell, and any item, ragdoll, young or
  overlay it needs is the creature's own copy (`CreatureBuild.CopyPart`, named `<name>_<suffix>`). The build keeps each
  part's origin (`CreatureBuild.OriginOf`, `CustomCreature.PartOrigins`), the one record the combat step, the elite step
  (portal attacks named by the game's item) and `ecp export` read. Item copies get their prefab name as shared name.
- **Humans.** `base: Human` is the player's body as a hostile Humanoid (`Humans/`): faction Boss, the Draugr's mind,
  a bare kit (club and rags) that the human step takes off once when any definition of the chain gives gear, the
  player's weapons fitted for an AI as it is armed, bows drawn and crossbows reloaded on the owner, endless ammunition,
  a look rolled on the owner into the game's own look keys.
- **Runtime parts** (on the prefab, every peer; decisions on the owner, state in the ZDO): `CustomTag`,
  `CreatureMessages`, `EatHeal`, `IdleSpawns`, `HumanAppearance`, `HumanRanged`, `ItemTint`, `BodyOverlay` (drawing only,
  nothing on a dedicated server).
- **Removed definitions** (`Saves/ParkedCreatures`): just before each `ZNetScene.CreateObjects`, a ZDO whose prefab is
  unknown and which carries `ecp_custom` is marked as made (memory only), so the server never deletes it and nobody makes
  it; one warning per name per world; it comes back when its definition does.
- **Errors.** A mistake in an entry (unknown key, value out of range, wrong word) leaves out that creature only, logged
  with the file, the creature and the line (YamlConfig's `TrackLines`, `CollectErrors`, `ErrorUnknownKeys`); an unknown
  prefab, item, effect or status effect fails the creature at build, at the field's path and line.

## Assets

Built in `../../ValheimAssets`: `Assets/Creatures/CryptMimic/crypt_mimic` (the mimic's rig and clips), `Assets/Creatures/Slinger/ecr_slinger`,
`assets/ecr_rimegiant`, `assets/ecp_kraken` and `assets/ecp_crossbowman` (`build.ps1 [-Preview] [-Install]`; `-Install` copies the bundles into this mod's
`assets/bundles`). The skeleton arsenal: `assets/ecp_skel_arsenal/build.ps1 -Install -SkipBake` (models, icons, its
bundle, then the crossbow's `ecp_crossbowman` and the greataxe's `ecp_headsman` through their own builds, install;
`-SkipBlender` builds from the models already built; without `-SkipBake` it also bakes the Blender showcase). The kraken's loot is the bundle `ecp_kraken_loot`: `.\build.ps1 -Asset ecp_kraken_beak,ecp_kraken_beak_shield,
ecp_kraken_meat,ecp_kraken_meat_cooked -Bundle ecp_kraken_loot -SkipBlender` in the workshop (their Blender side is
`assets/kraken_beak_set` and `assets/kraken_food`, run on their own), then copy `out/bundles/ecp_kraken_loot.*` here.
The Crypt Executioner: `assets/ecp_headsman/build.ps1 -Bundle -Install` (icons, bundle, install; it also rewrites
`Headsman/Sound/HeadsmanSoundTable.cs` from the sound recipes). The Bone Ballista: `Assets/Props/BoneBallista/build.ps1` (models, icons, review, lineups, bundle `ecp_boneballista`, check;
`-SkipBlender` rebuilds only the bundle), then copy `out/bundles/ecp_boneballista.*` here. Each feature file's work log records what was built.

## Testing

Build, then the user restarts the LocalTesting profile (never launch or kill the game). With DevBridge
(`curl -s http://127.0.0.1:7780/help`): `devcommands`, then `spawn ECP_CryptMimic`, `spawn ECP_GreydwarfSlinger`,
`spawn ECP_RimeGiant` (by day it comes asleep), `spawn ECP_Kraken` (at sea beside a ship with someone aboard),
`spawn ECP_SkeletonCrossbowman`, `spawn ECP_BoneCrossbow`, `spawn ECP_BoltBoneBlunt 20`, the arsenal's
`spawn ECP_SkeletonSwordsman` (Cutthroat, Axeman, Bonebreaker, Spearman, Halberdier, Bowman; and the kinds),
`spawn ECP_BoneSword` (Dagger, Axe, Mace, Spear, Atgeir, Bow), `spawn ECP_Spine`, `spawn ECP_ArrowBone 20`, `spawn ECP_Headsman`, `spawn ECP_ExecutionerGreataxe`, `spawn ECP_BoneMissile 20` (then build the Bone Ballista from the hammer's Misc tab), `spawn ECP_ExecutionerAxehead`, and
the kraken's loot `spawn ECP_KrakenBeak`, `ECP_KrakenMeat`, `ECP_KrakenMeatCooked`, `ECP_ShieldKraken`. Each
feature file ends with its checklist. Custom creatures: switch a ready-made one on in
`EliteCreaturesPack.Creatures.yml` (`enabled: true`), load a world, `spawn ECP_TrollBrute` (or `ECP_Outlaw`...),
and `ecp export Troll` / `ecp export human`. Test once without Elite
Creatures Reborn and once with it (stars and mutations, the mimic's hold-back).

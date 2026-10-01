# Elite Creatures Pack - specification: The Skeleton Arsenal

One feature of the mod, specified on its own. The other feature files sit beside it.

**The skeleton arsenal is every bone weapon of the mod** (the user, 2026-09-30): the bone dagger, sword, axe, mace,
spear, atgeir, the players' bow and the skeletons' bow, the bone arrow, the spine, the Bone Crossbow and its Blunted
Bone Bolts, and the Executioner's Greataxe, in three bundles (`ecp_skel_arsenal`, `ecp_crossbowman`,
`ecp_headsman`). The Bone Crossbow and its bolts are specified in `skeleton-crossbowman.md` (section 6a), the
greataxe in `headsman.md`, beside the creatures that carry them; the rest, and the skeletons that carry them (the
arsenal skeletons), are specified here.

This file covers the skeleton arsenal's own part: seven new skeleton units, each carrying one weapon of bone with vertebrae worked
in (a dagger, sword, axe, mace, spear, atgeir or bow), the spine they drop, the players' bone weapons made from it,
and bone arrows. The skeletons' bodies, AI and sounds are the game's own; the weapons, the arrow and the spine come
from the workspace's `AssetWorkshop` (`assets/ecp_skel_arsenal` and `assets/ecp_spine`, v2: the vertebra set; v1 is archived in
`assets/skeleton_weapons_v1`) and ship in the plugin as an embedded asset bundle.

Asked for by the user on 2026-09-29: "all these will now be new mobs in the game. they will not do any combo swings,
just single attacks against targets. They will be treated like vanilla skeleton units in terms of where and when they
spawn, what they are classified as and what biome they live in. Also these weapons should now be crafting recipies.
level 3 workbench. They all require a vertabrae and bones to craft, except the dagger just requires bones. all these
weapons need to be useable by players. We also need a vertabrae item that can be dropped by skeletons with these
vertabrae weapons. these skeletons should drop what normals skeletons drop, but have a 1 in 10 chance of the vertabrae
drop. you should be able to craft bone arrows using bones. better than wood, but worse than flint", and "the weapons
stats should be like the bronze variaents, but weaker". The rest was decided in building it and is listed under
section 7.

**Status: built, not tested in game.** The mod builds, the patches verify offline against the game's assemblies (44
patch classes, no problems), the bundle holds all ten models and nine icons; nothing has run in a game yet.

---

# 1. The skeletons

Seven arsenal skeletons, each in four kinds (the game's four skeletons, section 2): 28 creatures.

| Weapon | Skeleton | Prefab (+ `_Meadows`, `_Swamps`, `_Mountains`) | Its blow | Damage | Reach | Blows at most every | Shield |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Dagger | Skeleton Cutthroat | `ECP_SkeletonCutthroat` | the player's first knife slash | 0.8 of the sword's, half slash, half pierce | 1.5 m | 2 s | yes |
| Sword | Skeleton Swordsman | `ECP_SkeletonSwordsman` | the Skeleton's own sword swing | the sword's, slash | 1.8 m | 3 s | yes |
| Axe | Skeleton Axeman | `ECP_SkeletonAxeman` | the player's first axe swing | 1.1 of the sword's, slash | 1.9 m | 3 s | yes |
| Mace | Skeleton Bonebreaker | `ECP_SkeletonBonebreaker` | the poison skeleton's mace blow | 1.15 of the sword's, blunt | 2.2 m | 3.5 s | yes |
| Spear | Skeleton Spearman | `ECP_SkeletonSpearman` | the player's spear stab | the sword's, pierce | 2.3 m | 3 s | yes |
| Atgeir | Skeleton Halberdier | `ECP_SkeletonHalberdier` | the player's first atgeir blow | 1.2 of the sword's, pierce | 2.8 m | 4 s | no (two hands) |
| Bow | Skeleton Bowman | `ECP_SkeletonBowman` | the skeleton archer's shot | the archer's bow's, pierce | 20 m | 4 s | no |

- **One blow at a time.** Every melee skeleton swings through the Skeleton's own "attack" state (or the mace's
  "attack_mace"), which plays one clip and hands back to idle: no combos. The dagger, axe, spear and atgeir play the
  first blow of the player's own attack for that weapon in place of the Skeleton's sword swing (humanoid clips, which
  play on the Skeleton as they do on the player), through an override of the creature's animator. The player's clip
  carries its own hit; the copy used lands it no later than three quarters of the way through, so the spear's quick
  stab (whose hit comes at 84%) lands before the state starts handing back to idle.
- **Damage** starts from the sword of the skeleton it replaces (Black Forest 25 slash, Meadows 15, Swamps 48 and 10 chop,
  Mountains 60 and 15 chop), turned into the weapon's blow and factor; the chop stays, so the Swamps' and Mountains'
  arsenal skeletons still hack at buildings. The bowman's bow is its skeleton's bow (20, 15, 55, 60 pierce):

  | | Black Forest | Meadows | Swamps | Mountains |
  | --- | --- | --- | --- | --- |
  | Dagger | 20 | 12 | 38.4 | 48 |
  | Sword | 25 | 15 | 48 | 60 |
  | Axe | 27.5 | 16.5 | 52.8 | 66 |
  | Mace | 28.75 | 17.25 | 55.2 | 69 |
  | Spear | 25 | 15 | 48 | 60 |
  | Atgeir | 30 | 18 | 57.6 | 72 |
  | Bow | 20 | 15 | 55 | 60 |

- **The bowman** shoots exactly as the skeleton archer does (its bow is a copy of the archer's): the same draw, aim,
  range and arrow flight. Its arrow wears the bone arrow; its bow is the bone bow, strung with a string in the colour
  of the archer's between its tips.

# 2. Where they come from

A skeleton unit like the game's others: wherever and whenever the game spawns one of its skeletons, that spawn is an
arsenal skeleton instead one time in four (**25%**, fixed), each of that skeleton's kind: the Black Forest's
`Skeleton` (40 health), the Meadows' `Skeleton_Meadows` (30), the Swamps' `Skeleton_Swamps` (60) and the Mountains'
`Skeleton_Mountains` (75), and their no-archer versions (`Skeleton_NoArcher`, `Skeleton_*_noarcher`). That covers the
wild spawns (the Black Forest's skeletons at night once Bonemass is dead, the Swamps'), the fixed spawners of burial
chambers, graves, tower ruins, cabins and the Meadows' ruins, and bone piles.

- Which weapon it carries is drawn evenly. The bow comes only in place of a skeleton that could have been an
  archer; a no-archer skeleton's spawn draws among the six melee weapons.
- Each skeleton has its own on/off switch (section 8). One switched off drops out of the draw; the rest, the plain
  skeleton among them, stay equally likely.
- The spawn keeps its levels (stars). The spawner itself never changes: a fixed spawner's creature is swapped for one
  spawn and put back.
- With the Skeleton Crossbowman on too, an archer skeleton's spawn goes to whichever swap wins first; both never
  happen at once (each wild-spawn swap stands aside when another has already spawned in the skeleton's place).
- The poison, Hildir, Deep North and summoned skeletons are left alone.

# 3. Stats, look and loot

A copy of its kind's archer skeleton: its health, resistances, faction (the undead), sounds, senses, AI, star looks
and drops (a bone fragment, a 10% skeleton trophy). It carries the arsenal weapon in place of the Skeleton's random
sword or bow, and keeps the Skeleton's random shield (wood or bronze buckler) beside a one-handed weapon. On top of
the skeleton's drops, **one spine at 10%**, whatever its stars. Its name is the
table's ("Skeleton Cutthroat").

# 4. The players' items

**Bone weapons.** Each is a copy of the bronze-age weapon of its class, so it keeps that weapon's handling,
animations, combos, skill, sounds, trail, durability, weight, block, quality levels and upgrades, wearing the bone
model in place of the game's. Its damage, and what each upgrade adds, is that weapon's times **0.85** (like bronze,
a little weaker). Made at the **workbench, level 2** (level 3 until 0.4.0; the user, 2026-09-29).

| Item | Prefab | Made after | Damage (at 0.85) | Per upgrade | Default recipe |
| --- | --- | --- | --- | --- | --- |
| Bone Dagger | `ECP_BoneDagger` | copper knife (`KnifeCopper`; the game has no bronze knife) | 10.2 slash, 10.2 pierce | 0.85 each | BoneFragments 8 (+4 an upgrade) |
| Bone Sword | `ECP_BoneSword` | `SwordBronze` | 29.75 slash | 5.1 | ECP_Spine 1, BoneFragments 10 (+5) |
| Bone Axe | `ECP_BoneAxe` | `AxeBronze` | 34 slash, 34 chop | 4.25, 2.55 | ECP_Spine 1, BoneFragments 10 (+5) |
| Bone Mace | `ECP_BoneMace` | `MaceBronze` | 29.75 blunt | 5.1 | ECP_Spine 1, BoneFragments 12 (+6) |
| Bone Spear | `ECP_BoneSpear` | `SpearBronze` | 29.75 pierce | 5.1 | ECP_Spine 1, BoneFragments 10 (+5) |
| Bone Atgeir | `ECP_BoneAtgeir` | `AtgeirBronze` | 38.25 pierce | 5.1 | ECP_Spine 2, BoneFragments 16 (+8) |
| Bone Bow | `ECP_BoneBow` | fine bow (`BowFineWood`) | 27.2 pierce | 2.55 | ECP_Spine 1, BoneFragments 12 (+6) |

The dropped item lies with a box collider fitted to the bone model; the game's upgrade glow (brighter with each
quality) is reshaped to a box round it. The bow sits in the hand as the game's bows do (`ecp_skel_bow_player`, the
same bow turned into the player's hold) with a string between its tips.

**Spine** (`ECP_Spine`): a copy of the game's bone fragments (a material: weight, stack of 50, sounds) wearing
the bundle's spine: eight vertebrae still joined, 0.88 m long. Only the arsenal skeletons drop it. It replaced the
single vertebra (`ECP_Vertebra`) on 2026-09-29, at the user's request ("I kinda actually want a spine item instead, it
makes more sense"); uses, drop and counts are the vertebra's.

**Bone Arrow** (`ECP_ArrowBone`): a copy of the game's wood arrows with a bone shaft and a vertebra head. **24 pierce**
(wood 22, flint 27), otherwise a wood arrow (its flight, the bow's draw). 20 for **8 bone fragments** at
the **workbench, level 2** (wood arrows: level 1, flint arrows: level 2). Its projectile is a copy of the wood arrow's
(`ECP_ArrowBone_projectile`); the bowmen's is a copy of the skeleton archer's (`ECP_SkeletonArrow_projectile`).

# 5. Multiplayer

- Every prefab is built identically on the server and every client when ZNetScene wakes, and registered in ZNetScene
  and ObjectDB, so every hash that travels resolves to the same object everywhere. The mod must be on the server and
  every client (as for every creature here).
- Spawning is decided where the game decides every spawn (the machine running the spawner).
- The skeletons' attacks, animations and hits are the game's own (item, trigger, animation event), synced as for any
  skeleton; the animator override is part of the prefab on every machine.
- The spawn switches are synced and lockable, and read at each spawn, so a change takes effect at once.

# 6. Configuration

`8 - Skeleton Arsenal`: a master switch, `Enabled` (on: off stops all of them, whatever their own switches say), and
one on/off switch per skeleton, whether it spawns (`Skeleton Cutthroat`, `Skeleton Swordsman`, `Skeleton Axeman`,
`Skeleton Bonebreaker`, `Skeleton Spearman`, `Skeleton Halberdier`, `Skeleton Bowman`; all on).
Nothing else is a setting (the user, 2026-09-29: "I kinda just want individual toggles on/off for spawning. limit
config"): the share, the drop chance, damage, recipes, levels and the arrows are fixed as described above.

# 7. Decisions made in building it

Not in the request; decided here, change freely:

1. **Names**: Cutthroat, Swordsman, Axeman, Bonebreaker, Spearman, Halberdier, Bowman; items "Bone <weapon>",
   "Spine", "Bone Arrow". Prefab names are hashed into worlds: settle them before a release.
2. **One even draw** (the user, 2026-09-30; was 25% for the arsenal together): the plain skeleton, the seven arsenal
   skeletons and the Skeleton Crossbowman are equally likely, one in nine each (one in seven among no-archer spawns:
   no bowman, no crossbowman).
3. **Creature damage** from the replaced skeleton's sword by the factors above; the dagger strikes more often, the
   mace and atgeir less.
4. **The spine is paid once**: upgrades cost bones only (the atgeir needs two spines). One spine per kill at
   10%, never more with stars.
5. **Bone arrows at workbench level 2**, 8 bone fragments for 20, 24 pierce.
6. **Only damage is weaker** than bronze. Durability, weight, block, speed and the axe's tool tier (it chops what the
   bronze axe chops) are bronze's.
7. **The dagger is made after the copper knife**: the game has no bronze knife.
8. **The upgrade glow** is kept and reshaped to a box round the bone model rather than removed.

# Build checklist

In the LocalTesting profile with DevBridge, `devcommands` on.

## Spawning
- [ ] `spawn ECP_SkeletonSwordsman` (and each other title, and `_Meadows`, `_Swamps`, `_Mountains`) makes a skeleton
      holding the right bone weapon in the right hand (the bow in the left), named "Skeleton <title>".
- [ ] Black Forest burial chamber: about one skeleton in four comes armed from the arsenal; a no-archer spawner never
      gives a bowman.
- [ ] Bone pile and a swamp crypt: the Swamps' kind comes (60 health).
- [ ] A skeleton's switch off: no new ones of it; the others as often as before.
- [ ] `Enabled` off: no new arsenal skeletons at all; back on: they come again as their own switches say.

## Fight
- [ ] Each melee skeleton strikes once, returns to idle, strikes again: no combos. The dagger, axe, spear and atgeir
      play the player's blow; the hit lands with the blow (the spear's stab especially).
- [ ] Damage roughly as the table (Black Forest: sword 25 slash, atgeir 30 pierce); a two-star skeleton hits harder.
- [ ] The bowman draws, aims and shoots like the archer; the arrow looks like the bone arrow and flies like the
      archer's; the string sits between the bow's tips.
- [ ] One-handed skeletons carry a shield; the halberdier and bowman do not.

## Death and loot
- [ ] Bone fragments and the 10% trophy as a skeleton; a spine about one kill in ten.

## Items
- [ ] `spawn ECP_Spine`, `ECP_BoneDagger` ... `ECP_BoneBow`, `ECP_ArrowBone`: each shows its icon and name, lies on
      the ground on its collider, and each weapon sits in the hand like the bronze one (the spear point forward, the
      atgeir's blade out, the bow's string toward the player).
- [ ] Workbench level 2 lists the six spine weapons, the dagger and the bone arrows; the costs as above.
- [ ] Damage in the tooltip is 0.85 of bronze's; upgrades add 0.85 of bronze's per level; the upgrade glow sits on
      the bone model.
- [ ] Bone arrows: 24 pierce in the tooltip, fly from any bow, look like bone arrows in flight.

## Multiplayer
- [ ] On a dedicated server with two clients: both see the same skeleton with the same weapon and the same blows;
      a spine dropped by one is picked up by the other; recipes follow the server's settings.

## Work log

- 2026-09-29: models v1 (all bone) archived; v2 built around vertebrae (fewer, bigger pieces, curved spines, half long
  bones and half vertebrae in the spear and atgeir shafts), the bow in the player's hold, icons, bundle
  `ecp_skel_arsenal` installed. Mod side built: `Arsenal/` (kinds, weapons, creatures, weapons of the skeletons,
  clips, items, arrow, recipes, spawns, words, settings sections 8 and 9). The crossbowman's wild-spawn swap now stands
  aside when another swap already spawned (`__runOriginal`).
- 2026-09-29: config cut to seven spawn switches (section 8) at the user's request; section 9 removed, its values
  fixed in code. Then a master `Enabled` switch over them (user: "a master 'skeleton config' to turn them all on or
  off. default on").
- 2026-09-30: the Skeleton Crossbowman counts as an arsenal skeleton (the user): its switch `Skeleton Crossbowman`
  in section 8 under the master `Enabled` (section 6's `Enabled` removed), and the spine drop.
- 2026-09-30: at the user's request ("all the non-executioner skeletons ... an equal chance to be the mob spawned"),
  the arsenal's 25% and the crossbowman's `Share` became one even draw with the plain skeleton (`Skeletons/`); a
  switched-off skeleton now drops out of the draw instead of leaving its spawns plain. The greataxe's recipe finds the
  spine before the database lists it (it had fallen back to bone fragments) and takes 1 (was 8); the Bone Crossbow's
  takes 1 too. Built, not run in game.
- 2026-09-29: the vertebra item became the spine (`ECP_Spine`, `item_ecp_spine`, AssetWorkshop `assets/ecp_spine`),
  a visual and name change only: same drop, recipes and counts. The greataxe's `Vertebrae` setting is `Spines`.

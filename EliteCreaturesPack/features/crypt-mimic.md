# Elite Creatures Pack - specification: The crypt mimic

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers the crypt mimic: a new creature that poses as a crypt chest, bites whoever opens it, then hops after
them and lunges. Its model, rig and animations come from the workspace's `AssetWorkshop` (Blender, then Unity) and
ship in the plugin as an embedded asset bundle; the chest itself and every texture are the game's own, borrowed at
runtime. The design was settled with the user on 2026-09-27; the animations were reviewed in Blender and Unity first.

**Status: built, not tested in game.** Everything below is built and the patches verify offline against the game's
assemblies; nothing has been seen in a running game yet.

---

# 1. Where mimics come from

When a crypt first fills its chests (on the machine generating the crypt, which owns the chests), each chest listed
in `Chests` has `Chance` percent (default **15%**) to be a mimic instead. That chest is left empty and, a frame
later, removed; a mimic stands exactly where it stood, facing the same way. The mimic remembers which chest it was.

- Listed by default: `TreasureChest_forestcrypt` (burial chambers) and `TreasureChest_sunkencrypt` (sunken crypts).
- Deliberately not listed: `TreasureChest_forestcrypt_hildir` (it holds Hildir's quest key) and `TreasureChest_fCrypt`
  (the same chest also stands in the Ashlands' Charred Fortress, fortress ruins and troll caves).
- Only crypts generated after the feature is on get mimics; a chest already filled never changes.
- The game's own `spawn ECP_CryptMimic` (or, with Elite Creatures Reborn, `elite spawn ECP_CryptMimic <stars>`) makes
  one by hand, dormant.

# 2. Dormant

A dormant mimic **is** the crypt chest to anyone looking:

- The game's own chest meshes and material, on the mimic's rig; teeth, tongue and eyes are folded away inside.
- Its hover text and name are the chest's, word for word ("[E] Open" and the stack-all hint).
- No name plate or health bar. With Elite Creatures Reborn installed, no star row, size change, star look or mutation
  effect either: this mod marks every mimic with the ZDO bool `ecp_disguised`, and while a creature carrying it sleeps
  Elite Creatures Reborn applies its health at once (so the first hit is already scaled and waking never refills it)
  but holds everything visible until it wakes.
- It never wakes because someone walks near or makes noise. Only two things wake it: being **opened** (E) or being
  **hit**.

# 3. The ambush

Pressing E on a dormant mimic wakes it and bites the player who pressed it: the lid bursts open and snaps shut on them.
The ambush bite cannot be dodged or blocked - they walked into it. It deals the lunge bite's damage (below), scaled like
any hit from this creature. The mimic then targets that player. A mimic woken by a hit skips the ambush and fights.

# 4. Awake

- **The lunge** is its only attack: a wind-up with the lid gaping (0.73 s), a leap of about 2.5 m, and the snap. The
  animation plays the leap on the spot and `MimicLeap` moves the creature along the same curve through the game's
  root-motion hook, on the owner (Unity's root motion from the exported rig came out backwards, then downwards). **Dodgeable, not blockable**: a dodge's invincibility frames make it miss; a raised shield does not.
- After each bite it needs `Bite Cooldown` (default **3 s**) before it can bite again. Meanwhile it has no attack
  to use, so it bounds after its target at `Run Speed` (default **5**; the skeleton runs at 4); it wanders at
  half that. The hop blends into a longer, higher bound as it speeds up.
- After the snap it spends about 1.8 s recovering, lid hanging open and tongue out: the safe moment to hit it. It takes
  normal damage at all times.
- On death its lid flops open and the eyes go out; the corpse fades after 6 s and spills the loot.

# 5. Stats

It is a copy of the Black Forest crypt skeleton (`Skeleton`), so it has that skeleton's:

| | |
| --- | --- |
| Health | 40 (80 at one star, 120 at two, as for any creature) |
| Bite | 20 slash (`Bite Damage`; the skeleton's sword does 25), used for the lunge and the ambush |
| Weak to | blunt, fire |
| Resists | pierce, frost |
| Immune to | poison; chopping and mining do nothing |
| Faction | Undead: the crypt's skeletons and draugr leave it alone |

Its biome, for Elite Creatures Reborn's rules, is the Black Forest for forest crypts (a crypt interior resolves to the surface biome above
it). With Elite Creatures Reborn, stars and mutations roll like any creature's and show once it wakes; it has no aspects
(those are bosses'). Without it, a mimic stays at the game's level 1: crypt chests are not spawners that roll levels.

# 6. Loot

It drops the loot of the chest it replaced: that chest's own loot table, rolled at death into its drop list, which then
goes the way every creature's does (to its corpse, spilling when the corpse fades). Elite Creatures Reborn's loot rules and any loot mod act
on the drop list as for any creature; the chest's rows are added last and left as rolled.

Epic Loot finds tables by object name and has none for this creature, so it adds nothing until its loot table has an
entry for `ECP_CryptMimic` (for example one that refers to the crypt chest's or the skeleton's table).

# 7. Multiplayer

- The prefabs (creature, bite, corpse) are built and registered identically on the server and every client when the
  game's network scene wakes, so the prefab hash in a ZDO means the same thing everywhere. The plugin embeds a bundle
  per platform (Windows, Linux) because a bundle only loads on the platform it was built for; a dedicated server runs
  the Linux player.
- Which chests become mimics is decided once, by the chest's owner, and written to the chest's ZDO; the swap itself is
  done by whoever owns the chest when it next wakes.
- Dormant or awake is the game's own sleep state, carried to every client by the ZDO. Every client hides the health bar
  and shows the chest's text from it.
- Opening routes to the mimic's owner (`ecp_mimic_open`, the opener's ZDOID); only the owner wakes it and bites. The
  ambush animation reaches every client through the animator sync, the damage through the game's own damage RPC.
- Copies of an awake mimic split off by Elite Creatures Reborn's Splintering are born awake (that mod gives them the
  ZDO int `ecr_gen`, which this mod reads).

# 8. Configuration

Section `2 - Crypt Mimic` of `com.EliteCreaturesPack.cfg`, synced from the server and locked while `Lock Configuration`
is on there:

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | true | off: no new mimics; ones already in the world stay |
| `Chance` | 15 | percent of the listed chests that are mimics |
| `Chests` | TreasureChest_forestcrypt, TreasureChest_sunkencrypt | chest prefabs that may be mimics, separated by commas |
| `Bite Cooldown` | 3 | seconds between lunges |
| `Bite Damage` | 20 | slash, before stars (the crypt skeleton's sword: 25) |
| `Run Speed` | 5 | how fast it bounds after you; it wanders at half this |

A change reaches the bite of every loaded mimic at once.

# 9. Open decisions

- Whether an awake mimic that loses its target should settle back into a chest (the game can put it back to sleep)
  instead of roaming the crypt.
- Translations: the name ("Crypt Mimic") and the bite's are English in every language for now.
- A macOS bundle (macOS players load the Windows bundle and may not see the mimic).

# Build checklist

## Spawning
- [ ] Listed crypt chests become mimics at the configured chance when a crypt is generated; others fill as normal
- [ ] Hildir's crypt chest and the fortress/troll-cave chest are never replaced
- [ ] `spawn ECP_CryptMimic` makes a dormant mimic

## Dormant
- [ ] Looks exactly like the crypt chest (meshes, material, size), with no health bar, star row or effect
- [ ] Hover text and name are the chest's
- [ ] Walking near or making noise does not wake it

## Fight
- [ ] E wakes it and bites the opener, undodgeable and unblockable
- [ ] A hit wakes it without the ambush
- [ ] The lunge misses a dodging player and hits through a raised shield
- [ ] It waits the cooldown between lunges and hops after its target meanwhile
- [ ] With Elite Creatures Reborn: stars and mutations show once it wakes, nothing of them while dormant

## Death and loot
- [ ] The corpse plays the death pose, fades, and spills the chest's loot
- [ ] Epic Loot adds magic items once given an entry for ECP_CryptMimic

## Multiplayer
- [ ] On a dedicated server (Linux): prefabs register, a second player sees the ambush, the lunge and the death
- [ ] Opening from a client that does not own the mimic wakes it and bites that client

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-27 | Built: prefabs from the embedded bundle (BundlePrefabs library), disguise, ambush, lunge, crypt swap, loot, `mimics:` rules; ECR dresses a disguised creature only once it wakes. Patches verified offline. | |
| 2026-09-28 | Bite cooldown 4 s -> 3 s and run speed 3 -> 5 (wandering 2.5) at the user's request; a run (bound) clip added. | |
| 2026-09-28 | Bite damage 25 -> 12 -> 20 slash at the user's request, as the new `mimics: bite damage` (a reload reaches live mimics); the `added creatures:` master switch over mimics and slingers. | |
| 2026-09-28 | First in-game test: the ambush worked; the lunge bit on the spot and then slid 2.5 m backwards (the clip's root motion was reversed in Unity). The leap now comes from `MimicLeap`; the clips carry no travel. | |
| 2026-09-28 | Moved from Elite Creatures Reborn into its own mod, Elite Creatures Pack, at the user's request: prefabs `ECR_` -> `ECP_`, ZDO/RPC/global keys `ecr_` -> `ecp_`, the rule-file block became a section of the mod's .cfg (shares in percent, biomes as a flag list); Elite Creatures Reborn is optional and linked by key names only. Asset bundle and asset names kept (`ecr_*`). | |

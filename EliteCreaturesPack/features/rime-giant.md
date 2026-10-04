# Elite Creatures Pack - specification: The Rime Giant

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers the Rime Giant (the Hrímþurs of the old stories): a very rare giant of the Mountains, a troll
crusted in plates of ice that sleeps through the day looking like a snowy outcrop, shrugs off weapons until fire
breaks its plates, and sends avalanches down the slope at you. Its body is the game's own forest Troll, grown and
frosted; the plates of rime, the snow crust it wears asleep and the ice boulder it throws come from the workspace's
`ValheimAssets` (`Assets/Creatures/RimeGiant/ecr_rimegiant`) and ship in the plugin as an embedded asset bundle. The idea was pitched and
accepted on 2026-09-28 ("make it, it should be a very rare spawn"); the numbers below are judgement calls made then,
to be tuned in play.

**Status: built, not tested in game.** Everything below is built and the patches verify offline against the game's
assemblies; nothing has been seen in a running game yet.

---

# 1. Where giants come from

**At most one giant per mountain, ever**, on 3 mountains in 5 (decided with the user, 2026-09-28: "3/5 chance for 1
per mountain").

- **A mountain** is one of the game's own biome regions: the game floods the world into connected stretches of one
  biome (its biome sectors, built from the world seed, the same on every machine). A region of one of the
  `Biomes` (default: the **Mountains**) counts when it is at least 40 of the game's 12 m cells round its
  edge, about 150 m across; the snowy hilltops smaller than that do not.
- **Which mountains hold one** is fixed by the world seed and the region: `Mountains` percent of them (default **60%**). The
  world tested on 2026-09-28 (seed -3891240) has 123 Mountain regions, 75 big enough to count, and 45 of those hold a
  giant.
- **When it comes**: the giant has its own entry in every zone's spawn system (the game's wild spawning, run by
  whoever owns the zone). Each zone of a listed biome that a player is in rolls `Chance` percent (default **25%**) once
  every `Interval` seconds (default **3600**); the spawn goes ahead only on a mountain that holds a giant which has not
  come yet. So roaming a mountain that holds one, it shows up within the first few zones you cross.
- **Once it has come**, the mountain is claimed for good (a global key on the server, `ecp_rimegiant_<region>`, saved
  with the world): it never gets another, even after its giant dies.
- A giant comes alone, somewhere 40 to 80 metres from a player, away from the biome's edges, on ground no steeper
  than 35 degrees, never while another giant is loaded within a kilometre, and **asleep**, whatever the time of day
  (it wakes at once if it is night or storming, see 2).

A giant never despawns; it stays where it is until killed. `spawn ECP_RimeGiant` (or, with Elite Creatures Reborn,
`elite spawn ECP_RimeGiant <stars>`) makes one by hand, whatever the mountain.

# 2. Asleep and awake

- **Asleep** it is frozen in the troll's own sleeping pose, wearing its plates and a crust of snow and rime over its
  back, shoulders and head, and shows no name, health bar or hover text: a snowy outcrop on the slope. Walking right
  past does not wake it, and walking into it does not move it: asleep, its body is fixed in place (the game's own
  switch for sleeping creatures), and set down on the solid ground under it.
- It **wakes** at night, in a snowstorm (the game's `SnowStorm` weather), or the moment anything hits it, with a burst
  of shattering ice and the troll's alert roar. The crust is gone while it is awake.
- It **settles back to sleep** where it stands once it is day, the storm has passed and it has had nothing to fight
  (no target, not alerted, not attacking) for 20 seconds.

So by day a careful party can find one asleep and choose when to start the fight; at night it hunts like any troll
(it hears as far as a troll does).

# 3. The rime armour

- It wears up to `Plates` (default **8**) plates of ice: chest, back, both shoulders, both forearms, both thighs.
- **The plates cut the physical part of every hit** (blunt, slash, pierce, chop, pickaxe), the more plates the more.
  With every plate on only `Armoured Damage` (default **15%**) gets through; each plate lost lets an equal part more
  through (with the default eight: 15%, 26%, 36%, 47%, 58%, 68%, 79%, 89%, then the whole hit with none left). Fire,
  frost, lightning, poison and spirit are not cut.
- **Fire breaks the plates.** Every `Fire Per Plate` (default **30**) of fire damage it takes knocks one plate off.
  The game turns all fire into burning, so fire arrows, a torch, a fire staff and the burn afterwards all count. It
  is **weak to fire** (the game's Weak, 1.5 times), so a fire arrow (22 fire, 33 against it) takes off about one
  plate and a torch's hit (15 fire, 22 against it) three quarters of one.
- **Standing near a fire** (within about two metres of a fire's warmth - a campfire, hearth or brazier) counts as 10
  fire a second: a plate every three seconds. Trolls do not avoid fire, so a campfire can be a trap.
- **At `Shatter At` of its health** (default **40%**) whatever plates are left all break off at once, so the last
  part of the fight is always against the bare giant.
- Each plate that breaks falls off and tumbles away with the sound and sparkle of shattering ice, from the chest plate
  last. **When the last one falls, it staggers.**
- **Plates never grow back during a fight.** Only once it has no target, is no longer alerted and has gone
  `Regrow Delay` seconds (default **12**) without being hit or burnt, and only while its health is above `Shatter At`,
  do they grow back, one every `Regrow Interval` (default **5**), with a glitter of frost. A giant that gave up the
  fight rearms; one still below 40% stays bare until its health (the game's slow creature healing, all of it back in
  an hour) climbs past it. (Decided with the user, 2026-09-28.)

The fight's rhythm: burn plates off to open it up, or grind through them with weapons; either way it is bare by 40%.

# 4. Attacks

Copies of the forest troll's own, so the animations, timing and hit shapes are the game's; every reach is grown with
the body.

- **Sweep** (the troll's punch): `Sweep Damage` (75) blunt with more than twice the troll's knockback (220 against
  100). Up close, it throws you.
- **Slam** (the troll's two-fisted ground slam): `Slam Damage` (60) blunt where the fists land, and it sends an
  **avalanche** rolling from its feet towards its target. It uses the slam from up to 12 metres, every 12 seconds at
  most.
- **Avalanche**: a wave of snow and ice that rolls along the ground in 2 m steps (about as fast as a sprint). It runs
  `Avalanche Length` (24) metres on flat ground; every metre it climbs costs it three metres of that and every metre
  it drops gives half a metre back, so **below the giant it runs far (up to 50 m), above it it dies within a few
  steps**. It stops at water. Anyone hostile it rolls over takes `Avalanche Damage` (45) blunt plus half as much frost,
  once per wave, and is knocked on in the direction it rolls - downhill. It can be dodged and blocked like any hit.
  Flank it along the ridge or get above it.
- **Ice boulder** (the troll's rock throw): from 11 to 22 metres, every 10 seconds at most, it hurls a boulder of ice
  that bursts where it lands: everyone within 3.5 metres takes `Boulder Damage` (40) frost and half as much blunt (the
  game's frost, which chills and slows).

# 5. Stats, look and loot

- A copy of the forest troll grown to **1.4 times** its size, with `Health` (default **1500**; a troll has 600) before
  stars. The troll's resistances (resists blunt, weak to pierce, ignores chop and pickaxe, immune to spirit), plus
  **immune to frost** and **weak to fire**. A mountain creature: wolves, drakes, golems and fenrings leave it be.
- Its skin is the troll's, drained of blue and lightened to the grey-white of old ice. One star is a colder blue, two
  a paler, brighter white; the corpse keeps the colour.
- It rolls stars and mutations like any creature. Its hits are the Gammeltroll's icy ones.
- **Loot**: 4-8 Crystal, 3-6 Freeze gland, 3-6 Silver ore, no troll hide (with Elite Creatures Reborn, scaled by its loot
  rules like any creature's, which can also change it with a `creatures:` entry for `ECP_RimeGiant`).

# 6. Multiplayer

- The prefabs (creature, boulder, corpse, attacks) are built once and registered identically on the server and every
  client whenever the game's network scene wakes, so the prefab hash in a ZDO means the same thing everywhere. The
  plugin embeds a bundle per platform (Windows, Linux) because a bundle only loads on the platform it was built for;
  a dedicated server runs the Linux player.
- Spawning is decided by whoever owns the zone's spawn system, as for every wild creature.
- **Sleep** is the game's own sleep state, carried to every client in the ZDO; the owner wakes it and puts it back to
  sleep. Every client draws the crust and hides the name and health bar from that state alone.
- **Plates** are an int in the creature's ZDO (`ecp_rime_plates`). Only the owner changes it: the physical cut and the
  fire counting run where damage is applied (the owner), heat and regrowth in the owner's update. Every client draws
  the plates from the number, and a plate lost while a client watches falls off there as a local, cosmetic copy.
- **Avalanche**: the owner traces the path (at the slam's hit frame, where the game runs attacks) and deals the
  damage through the game's damage RPC; it sends the path once to every client (one RPC, a list of points), which
  draw the wave step by step with local effects.
- **Boulder**: the game's own networked projectile, launched on the owner; its burst is the game's projectile area hit.
- The settings come from the server when it binds its players. A change reaches
  the spawn entry at once, the attack damage of loaded giants as well as new ones, and the health of new ones.

# 7. Configuration

Section `4 - Rime Giant` of `com.EliteCreaturesPack.cfg`, synced from the server and locked while `Lock Configuration`
is on there:

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | true | off: no new giants; ones already in the world stay |
| `Mountains` | 60 | percent of mountains that hold a giant |
| `Chance` | 25 | percent per zone roll while its giant has not come yet |
| `Interval` | 3600 | seconds between a zone's rolls |
| `Biomes` | Mountain | the biomes whose regions count; several separated by commas |
| `Health` | 1500 | before stars (a forest troll: 600) |
| `Plates` | 8 | 0 to 8 |
| `Armoured Damage` | 15 | percent of a physical hit that gets through with every plate on |
| `Shatter At` | 40 | percent of its health at which the plates left all break off |
| `Fire Per Plate` | 30 | fire damage that breaks one plate (a fire arrow: about 33) |
| `Regrow Delay` | 12 | seconds unhurt, out of the fight, before the plates grow back |
| `Regrow Interval` | 5 | seconds per plate as they grow back |
| `Sweep Damage` | 75 | blunt, heavy knockback (a troll's punch: 60) |
| `Slam Damage` | 60 | blunt, where the fists land |
| `Avalanche Damage` | 45 | blunt, plus half as much frost, where the wave rolls over you |
| `Avalanche Length` | 24 | metres the wave rolls on flat ground; 0 turns it off |
| `Boulder Damage` | 40 | frost in the burst where it lands, plus half as much blunt |

# 8. Decisions

- **Name**: "Rime Giant" in game (the pitch's Hrímþurs is kept for the lore; the game's fonts may not have þ).
- **No frost breath**: the pitch had one, but the troll's rig has no breath animation; the troll's own rock throw
  became the ice boulder with a frost burst instead.
- **Loot from the game's items** (crystal, freeze glands, silver ore; no troll hide, the user's call) rather than a new "frozen heart"
  material, which would need an icon, a model and something to craft from it.

# 9. Open decisions

- A trophy and a crafting material of its own (the pitch's "frozen heart": a frost weapon or frost-resistant armour).
- Whether it should wake when a player comes very close by day (now only night, storm or a hit wake it).
- Translations: its name and attacks are English in every language for now.
- A macOS bundle (macOS players load the Windows bundle and may not see the plates).

# Build checklist

## Spawning
- [ ] At most one per mountain, on about 3 mountains in 5, asleep; a mountain whose giant came never gets another
- [ ] A sleeping giant cannot be pushed, and sits on the ground
- [ ] `spawn ECP_RimeGiant` makes one; `Enabled = false` stops new ones

## Sleep
- [ ] By day it sleeps in the troll's pose under its crust, with no name, health bar or hover text
- [ ] It wakes at night, in a snowstorm and when hit; it settles back to sleep on a clear day after 20 quiet seconds

## Armour
- [ ] Eight plates sit on the body through idle, walk, run and every attack, lit like the troll
- [ ] Physical hits do 15% with every plate on, more with each lost; fire arrows, a torch and a nearby campfire break
  plates one by one; the rest shatter at 40% health
- [ ] No plate grows back while it fights; after it gives up they return one by one (not below 40% health)
- [ ] A broken plate falls off and tumbles away; the last one staggers it

## Attacks
- [ ] The sweep throws a player back; the slam's avalanche runs far downhill, dies uphill, knocks you downhill
- [ ] The boulder arcs in, bursts in ice and frost, and slows whoever it catches

## Death and loot
- [ ] It falls as a frosted troll corpse at its size; crystal, freeze glands and silver ore drop (no troll hide)
- [ ] Stars and mutations show and scale it like any creature

## Multiplayer
- [ ] On a dedicated server (Linux): prefabs register, a second player sees the sleep, the plates falling and the wave
- [ ] A settings change reaches the attack damage of giants already in the world

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-28 | Built: the creature on the forest troll, the rime armour, sleep, avalanche, ice boulder, corpse, spawn entry and `rime giants:` rules; the plates, crust and boulder in ValheimAssets (`Assets/Creatures/RimeGiant/ecr_rimegiant`). Shared `YamlRead.Number`/`Biomes` for the added-creature blocks. Patches verified offline (77 classes). | |
| 2026-09-28 | Assets: 8 plates (Spine2 chest/back, Left/RightArm shoulders, Left/RightForeArm, Left/RightUpLeg), 7 crust pieces fitted to the troll's Sleeping pose, the boulder (1.2 x 1.1 x 1.4 m), measured on the game's Troll at its armature scale 130; muted cyan-grey hand-painted ice after the game's own textures; 256 px per part; bundles 2.1 MB (Windows) and 2.3 MB (Linux). Known clipping (clearance check): the sleeping jaw into the chest plate, the sleeping left forearm into the left thigh plate, the thumb into the right thigh plate at the end of Wakeup, shoulder skin into the shoulder plates in the punch; the forearm plates go below ground in the slam, as the troll's fists do. | |
| 2026-09-28 | At the user's request: plates never regrow in a fight (only after it gives up and goes 12 s unhurt, and not below `shatter at`); whatever is left shatters at 40% health (`shatter at`, new); the cut scales with the plates left; no troll hide in the loot. | |
| 2026-09-28 | At the user's request: at most one giant per mountain, on 3 mountains in 5 (`mountains`, new; the game's biome sectors, 40+ edge cells, seed-hashed; claimed by global key), `chance` 0.2 -> 25; a sleeping giant's body is fixed in place (`m_disableWhileSleeping`) and set down on the ground by its owner. Verified on the live world through DevBridge: 75 mountains, 45 hold a giant. 78 patch classes verified. | |
| 2026-09-28 | Moved from Elite Creatures Reborn into its own mod, Elite Creatures Pack, at the user's request: prefabs `ECR_` -> `ECP_`, ZDO/RPC/global keys `ecr_` -> `ecp_`, the rule-file block became a section of the mod's .cfg (shares in percent, biomes as a flag list); Elite Creatures Reborn is optional and linked by key names only. Asset bundle and asset names kept (`ecr_*`). | |
